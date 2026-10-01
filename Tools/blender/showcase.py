"""Loads every built hull into the current Blender session as a comparison fleet.

Run from Blender's text editor (or `blender -P Tools/blender/showcase.py`) after building
the models. One column per faction, left to right, classes stacked front to back, so a
given class can be compared straight across all four fleets.

    clear(); lay_out(); frame()
    frame(pitch_deg=55)        # more plan-like, for reading deck layouts
    focus("ilium_battleship")  # frame one ship
"""
import math
import os

import bpy
import mathutils

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
MODELS = os.path.join(ROOT, "Assets", "Art", "Models")
CLASSES = ["battleship", "destroyer", "transport", "frigate", "corvette"]
FACTIONS = ["continuance", "remnant", "serac", "ilium"]
GAP = 60.0


def ship_collection():
    col = bpy.data.collections.get("Ships")
    if col is None:
        col = bpy.data.collections.new("Ships")
        bpy.context.scene.collection.children.link(col)
    return col


def import_hull(faction, cls):
    """Imports one FBX and returns its top-level root, or None if it is not built yet."""
    path = os.path.join(MODELS, f"hull_{cls}_{faction}.fbx")
    if not os.path.exists(path):
        return None

    before = set(bpy.data.objects)
    bpy.ops.wm.fbx_import(filepath=path)
    new = [o for o in bpy.data.objects if o not in before]
    if not any(o.type == "MESH" for o in new):
        for o in new:
            bpy.data.objects.remove(o, do_unlink=True)
        return None

    tops = [o for o in new if o.parent is None]
    root = tops[0] if tops else new[0]
    root.name = f"{faction}_{cls}"

    col = ship_collection()
    for o in new:
        for c in list(o.users_collection):
            c.objects.unlink(o)
        col.objects.link(o)
    return root


def world_bounds(root):
    """Axis-aligned world bounds over a root and everything under it."""
    xs, ys, zs = [], [], []
    stack = [root]
    while stack:
        o = stack.pop()
        if o.type == "MESH":
            for corner in o.bound_box:
                w = o.matrix_world @ mathutils.Vector(corner)
                xs.append(w.x)
                ys.append(w.y)
                zs.append(w.z)
        stack.extend(o.children)
    return (min(xs), max(xs), min(ys), max(ys), min(zs), max(zs))


def clear():
    for ob in list(bpy.data.objects):
        bpy.data.objects.remove(ob, do_unlink=True)
    for col in list(bpy.data.collections):
        bpy.data.collections.remove(col)


def lay_out():
    """Builds the fleet and reports what loaded, so a missing model is obvious."""
    report = []
    x_cursor = 0.0
    for faction in FACTIONS:
        band = []
        for cls in CLASSES:
            root = import_hull(faction, cls)
            if root is not None:
                band.append((cls, root, world_bounds(root)))
        if not band:
            continue

        y = 0.0
        for cls, root, b in band:
            cy = (b[2] + b[3]) * 0.5
            root.location.y += (y + (b[3] - b[2]) * 0.5) - cy
            y += (b[3] - b[2]) + GAP

        x0 = min(b[0] + root.location.x for cls, root, b in band)
        x1 = max(b[1] + root.location.x for cls, root, b in band)
        for cls, root, b in band:
            root.location.x += x_cursor - (x0 + x1) * 0.5
        x_cursor += (x1 - x0) + GAP

        report.append(f"{faction}: " + ", ".join(
            f"{cls} {b[1] - b[0]:.0f}x{b[3] - b[2]:.0f}" for cls, root, b in band))

    for line in report:
        print("  ", line)
    return report


def _studio():
    scene = bpy.context.scene
    world = bpy.data.worlds.get("Showcase") or bpy.data.worlds.new("Showcase")
    scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes["Background"]
    bg.inputs["Color"].default_value = (0.028, 0.032, 0.040, 1.0)
    bg.inputs["Strength"].default_value = 1.0


def frame(yaw_deg=0.0, pitch_deg=24.0):
    """Aims the viewport at the layout; a no-op when there is no UI, as under -b."""
    _studio()
    screen = bpy.context.screen
    if screen is None:
        print("no UI available; skipping viewport framing")
        return

    for area in screen.areas:
        if area.type != "VIEW_3D":
            continue
        sp = area.spaces.active
        sp.shading.type = "MATERIAL"
        sp.clip_start = 0.1
        sp.clip_end = 100000.0
        sp.lens = 50.0
        for flag in ("show_floor", "show_axis_x", "show_axis_y", "show_extras", "show_cursor"):
            setattr(sp.overlay, flag, False)
        sp.region_3d.view_perspective = "PERSP"
        sp.region_3d.view_rotation = mathutils.Euler(
            (math.radians(90.0) - math.radians(pitch_deg), 0.0,
             math.radians(180.0 + yaw_deg)), "XYZ").to_quaternion()
        region = next(r for r in area.regions if r.type == "WINDOW")
        with bpy.context.temp_override(window=bpy.context.window, area=area, region=region):
            bpy.ops.view3d.view_all(center=True)
    for o in bpy.context.selected_objects:
        o.select_set(False)


def focus(root_name):
    """Frames one ship by name, e.g. focus('ilium_battleship')."""
    root = bpy.data.objects.get(root_name)
    screen = bpy.context.screen
    if root is None or screen is None:
        print("cannot focus:", root_name)
        return
    for area in screen.areas:
        if area.type != "VIEW_3D":
            continue
        region = next(r for r in area.regions if r.type == "WINDOW")
        for o in bpy.context.view_layer.objects:
            o.select_set(o.name.startswith(root_name))
        with bpy.context.temp_override(window=bpy.context.window, area=area, region=region):
            bpy.ops.view3d.view_selected()
    for o in bpy.context.selected_objects:
        o.select_set(False)
