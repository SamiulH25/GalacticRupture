"""Renders review sheets: every hull of a faction, turrets dressed at the sim's hardpoints.

    blender -b -P render.py -- continuance                 # one faction, three views
    blender -b -P render.py -- serac --classes corvette,frigate

Output lands in output/ship-reviews/<faction>/. The lighting is deliberately flat and
studio-like: these are construction checks, not beauty shots.
"""
import importlib
import math
import os
import sys

import bpy
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import assemble  # noqa: E402
import fleet as f  # noqa: E402
import grkit as k  # noqa: E402
import turrets  # noqa: E402

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "output", "ship-reviews")

# yaw, pitch, in degrees. Bow-on three-quarter, then plan and profile.
VIEWS = (("three-quarter", -34.0, 22.0), ("top", 0.0, 84.0), ("side", 90.0, 4.0))


def reset():
    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    for col in list(bpy.data.collections):
        bpy.data.collections.remove(col)


def studio(span):
    """A key/fill/rim rig scaled to the ship, plus a flat world so nothing is pure black."""
    scene = bpy.context.scene
    world = scene.world or bpy.data.worlds.new("World")
    scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes["Background"]
    bg.inputs["Color"].default_value = (0.035, 0.04, 0.05, 1.0)
    bg.inputs["Strength"].default_value = 1.0

    for name, energy, offset in (("Key", 9.0, (-1.1, -0.7, 1.5)), ("Fill", 3.0, (1.4, 0.6, 0.5)),
                                ("Rim", 5.0, (0.3, 1.6, 1.1))):
        data = bpy.data.lights.new(name, type="AREA")
        data.energy = energy * span * span * 12.0
        data.size = span * 1.6
        ob = bpy.data.objects.new(name, data)
        bpy.context.collection.objects.link(ob)
        ob.location = Vector(offset) * span
        ob.rotation_euler = (math.radians(58.0), 0.0, math.radians(-38.0))


def camera():
    data = bpy.data.cameras.new("Camera")
    data.lens = 62.0
    data.sensor_width = 36.0
    ob = bpy.data.objects.new("Camera", data)
    bpy.context.collection.objects.link(ob)
    bpy.context.scene.camera = ob
    return ob


def orbit(yaw_deg, pitch_deg):
    """Unit vector from the ship out to the camera. yaw 0 sits ahead of the bow."""
    yaw, pitch = math.radians(yaw_deg), math.radians(pitch_deg)
    return Vector((math.sin(yaw) * math.cos(pitch), -math.cos(yaw) * math.cos(pitch), math.sin(pitch)))


def half_fov():
    cam = bpy.context.scene.camera.data
    return math.atan(cam.sensor_width / (2.0 * cam.lens))


def fit_distance(outward, centre, verts, margin=1.06):
    """Closest camera distance along `outward` that keeps every vertex inside the frame.

    A single distance for every view crops long ships in profile, so the distance is
    solved per view: for a vertex at offset `rel` from the centre, being in frame means
    |rel.right| <= t * (D + rel.forward) and likewise for up, giving a bound on D.
    """
    forward = -outward.normalized()
    right = Vector((0.0, 0.0, 1.0)).cross(forward)
    if right.length < 1e-6:
        right = Vector((1.0, 0.0, 0.0))
    right.normalize()
    up = forward.cross(right).normalized()
    t = math.tan(half_fov()) * margin
    need = 1.0
    for v in verts:
        rel = v - centre
        # Depth from the camera plane is D + rel.dot(forward); the bounds below solve for D.
        need = max(need, abs(rel.dot(right)) / t - rel.dot(forward), abs(rel.dot(up)) / t - rel.dot(forward))
    return need


def shoot(cam, target, outward, distance):
    cam.location = Vector(target) + outward * distance
    cam.rotation_euler = (Vector(target) - cam.location).to_track_quat("-Z", "Y").to_euler()


def configure(res=1100):
    scene = bpy.context.scene
    scene.render.resolution_x = res
    scene.render.resolution_y = res
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = False
    scene.render.image_settings.file_format = "PNG"
    scene.view_settings.view_transform = "Standard"
    scene.view_settings.look = "None"
    for engine in ("BLENDER_EEVEE_NEXT", "BLENDER_EEVEE", "CYCLES"):
        try:
            scene.render.engine = engine
            break
        except TypeError:
            continue
    if scene.render.engine == "CYCLES":
        scene.cycles.samples = 24


def render_faction(faction, classes, outdir):
    mod = importlib.import_module(faction)
    os.makedirs(outdir, exist_ok=True)
    sheets = []
    for cls in classes:
        reset()
        # The kit holds live objects, so it is rebuilt after every reset.
        kit = turrets.build_all(k.collection("Turrets"))
        for house in kit.values():
            house.location = (0, 0, 0)
        col = k.collection(f"Hull_{cls}_{faction}")
        obj = mod.BUILDERS[cls](col)
        f.check(cls, obj)
        assemble.dress(k.collection("Dressed"), kit, cls)

        co = [v.co for v in obj.data.vertices]
        centre = Vector((0.0, sum(v.y for v in co) / len(co), sum(v.z for v in co) / len(co)))
        span = max(max(v.y for v in co) - min(v.y for v in co),
                   max(v.x for v in co) - min(v.x for v in co))
        configure()
        studio(span)
        cam = camera()

        views = []
        for tag, yaw, pitch in VIEWS:
            outward = orbit(yaw, pitch)
            shoot(cam, centre, outward, fit_distance(outward, centre, co))
            path = os.path.join(outdir, f"{cls}-{tag}.png")
            bpy.context.scene.render.filepath = path
            bpy.ops.render.render(write_still=True)
            views.append(path)
        sheets.append((cls, views))
        print(f"[render] {faction}/{cls} tris={f.tri_budget(obj)}")
    return sheets


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []

    classes = None
    if "--classes" in argv:
        i = argv.index("--classes")
        if i + 1 >= len(argv):
            raise SystemExit("--classes needs a comma-separated list")
        classes = [c.strip() for c in argv[i + 1].split(",") if c.strip()]
        argv = argv[:i] + argv[i + 2:]

    factions = [a for a in argv if a in f.FACTIONS] or list(f.FACTIONS)
    unknown = [a for a in argv if a not in f.FACTIONS]
    if unknown:
        raise SystemExit(f"unknown faction(s) {unknown}; known: {list(f.FACTIONS)}")
    bad = [c for c in classes or [] if c not in f.CLASSES]
    if bad:
        raise SystemExit(f"unknown class(es) {bad}; known: {list(f.CLASSES)}")

    for faction in factions:
        outdir = os.path.join(OUT, faction)
        render_faction(faction, classes or list(f.CLASSES), outdir)
        print(f"[render] {faction} -> {outdir}")


main()
