"""Hard-surface helpers for Galactic Rupture ship models.

Blender axes: +X starboard, +Y forward (bow), +Z up. Units are metres.
The sim's hull definitions use Unity axes (x starboard, y up, z forward); `hp()` converts.

Materials are named after the Unity submesh slots (Paint, Dark, Trim, Windows, Engine, Heat);
the game maps them by name.
"""
import math

import bmesh
import bpy
from mathutils import Matrix, Vector

SLOTS = ("Paint", "Dark", "Trim", "Windows", "Engine", "Heat")


COLOURS = {
    "Paint": (0.36, 0.40, 0.46, 1), "Dark": (0.10, 0.11, 0.12, 1), "Trim": (0.85, 0.63, 0.28, 1),
    "Windows": (1.0, 0.8, 0.55, 1), "Engine": (0.45, 0.75, 1.0, 1), "Heat": (1.0, 0.32, 0.08, 1),
}


def material(slot):
    """Preview material for a slot; the game replaces these by name."""
    m = bpy.data.materials.get(slot)
    if m is None:
        m = bpy.data.materials.new(slot)
        m.use_fake_user = True
        m.use_nodes = True
        bsdf = next(n for n in m.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
        c = COLOURS[slot]
        bsdf.inputs["Base Color"].default_value = c
        bsdf.inputs["Metallic"].default_value = 0.5 if slot in ("Paint", "Dark", "Trim") else 0.0
        bsdf.inputs["Roughness"].default_value = 0.5
        if slot in ("Windows", "Engine", "Heat"):
            bsdf.inputs["Emission Color"].default_value = c
            # Per slot: a nozzle bell should read as hot machinery, not as a light fitting.
            # The game's EngineGlow material supplies the actual glow.
            bsdf.inputs["Emission Strength"].default_value = {
                "Windows": 2.2, "Engine": 1.2, "Heat": 2.6}[slot]
    return m


def hp(x, y_up, z_fwd):
    """Unity (x, y up, z forward) -> Blender (x, y forward, z up)."""
    return Vector((x, z_fwd, y_up))


def collection(name, clear=True):
    col = bpy.data.collections.get(name)
    if col is None:
        col = bpy.data.collections.new(name)
        bpy.context.scene.collection.children.link(col)
    elif clear:
        for o in list(col.objects):
            bpy.data.objects.remove(o, do_unlink=True)
    return col


class Part:
    """Accumulates geometry for one object via bmesh, with per-face material slots."""

    def __init__(self, name, col):
        self.name = name
        self.col = col
        self.bm = bmesh.new()
        self.mats = []

    def _mat(self, slot):
        if slot not in self.mats:
            self.mats.append(slot)
        return self.mats.index(slot)

    def _faces(self, faces, slot):
        idx = self._mat(slot)
        for f in faces:
            f.material_index = idx

    # A convex loft: `rings` are lists of (x, z) points at successive y stations.
    def loft(self, stations, slot="Paint", cap_start=True, cap_end=True, matrix=None):
        bm = self.bm
        m = matrix or Matrix()
        rings = []
        for y, pts in stations:
            rings.append([bm.verts.new(m @ Vector((x, y, z))) for x, z in pts])
        faces = []
        for a, b in zip(rings, rings[1:]):
            n = len(a)
            for i in range(n):
                j = (i + 1) % n
                faces.append(bm.faces.new((a[i], a[j], b[j], b[i])))
        if cap_start:
            faces.append(bm.faces.new(list(reversed(rings[0]))))
        if cap_end:
            faces.append(bm.faces.new(rings[-1]))
        self._faces(faces, slot)
        return faces

    def box(self, centre, size, slot="Paint", matrix=None, chamfer=0.0, taper=1.0, taper_y=1.0):
        """Box along Y; `chamfer` cuts the four long edges, `taper` narrows the top (x),
        `taper_y` shortens the top face along y (sloped front and back)."""
        cx, cy, cz = centre
        sx, sy, sz = (s * 0.5 for s in size)
        c = min(chamfer, sx * 0.45, sz * 0.45)

        def ring(y, top_scale):
            tx = sx * top_scale
            if c <= 0:
                return [(cx - sx, cz - sz), (cx + sx, cz - sz), (cx + tx, cz + sz), (cx - tx, cz + sz)]
            return [(cx - sx + c, cz - sz), (cx + sx - c, cz - sz), (cx + sx, cz - sz + c), (cx + tx, cz + sz - c),
                    (cx + tx - c, cz + sz), (cx - tx + c, cz + sz), (cx - tx, cz + sz - c), (cx - sx, cz - sz + c)]

        if taper_y >= 0.999:
            return self.loft([(cy - sy, ring(cy - sy, taper)), (cy + sy, ring(cy + sy, taper))], slot, matrix=matrix)
        # Sloped ends: build as a loft through three stations with the top pulled in.
        bm = self.bm
        m = matrix or Matrix()
        verts = {}

        def v(x, y, z):
            return bm.verts.new(m @ Vector((x, y, z)))

        ty = sy * taper_y
        bottom = [v(cx - sx, cy - sy, cz - sz), v(cx + sx, cy - sy, cz - sz), v(cx + sx, cy + sy, cz - sz), v(cx - sx, cy + sy, cz - sz)]
        top = [v(cx - sx * taper, cy - ty, cz + sz), v(cx + sx * taper, cy - ty, cz + sz), v(cx + sx * taper, cy + ty, cz + sz), v(cx - sx * taper, cy + ty, cz + sz)]
        faces = [bm.faces.new(list(reversed(bottom))), bm.faces.new(top)]
        for i in range(4):
            j = (i + 1) % 4
            faces.append(bm.faces.new((bottom[i], bottom[j], top[j], top[i])))
        self._faces(faces, slot)
        return faces

    def cyl(self, p0, p1, r0, r1=None, seg=24, slot="Dark", cap=True):
        r1 = r0 if r1 is None else r1
        p0, p1 = Vector(p0), Vector(p1)
        axis = p1 - p0
        length = axis.length
        rot = axis.normalized().to_track_quat("Y", "Z").to_matrix().to_4x4()
        m = Matrix.Translation(p0) @ rot
        ring0 = [(math.cos(t) * r0, math.sin(t) * r0) for t in (2 * math.pi * (i + 0.5) / seg for i in range(seg))]
        ring1 = [(math.cos(t) * r1, math.sin(t) * r1) for t in (2 * math.pi * (i + 0.5) / seg for i in range(seg))]
        return self.loft([(0.0, ring0), (length, ring1)], slot, cap, cap, matrix=m)

    def mirror_x(self):
        """Duplicates all geometry mirrored across X (for symmetric detail)."""
        bm = self.bm
        geom = bm.verts[:] + bm.edges[:] + bm.faces[:]
        out = bmesh.ops.duplicate(bm, geom=geom)
        verts = [e for e in out["geom"] if isinstance(e, bmesh.types.BMVert)]
        bmesh.ops.scale(bm, vec=(-1, 1, 1), verts=verts)
        faces = [e for e in out["geom"] if isinstance(e, bmesh.types.BMFace)]
        bmesh.ops.reverse_faces(bm, faces=faces)

    def build(self, bevel=0.0, bevel_segments=2, smooth_angle=35.0, origin=None):
        bm = self.bm
        bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-4)
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        bm.normal_update()
        box_uv(bm)
        mesh = bpy.data.meshes.new(self.name)
        bm.to_mesh(mesh)
        bm.free()
        for slot in self.mats:
            mesh.materials.append(material(slot))
        obj = bpy.data.objects.new(self.name, mesh)
        self.col.objects.link(obj)
        if origin is not None:
            obj.location = origin
            mesh.transform(Matrix.Translation(-Vector(origin)))
        mesh.shade_smooth()
        mesh.set_sharp_from_angle(angle=math.radians(smooth_angle))
        if bevel > 0:
            mod = obj.modifiers.new("Bevel", "BEVEL")
            mod.width = bevel
            mod.segments = bevel_segments
            mod.limit_method = "ANGLE"
            mod.angle_limit = math.radians(30)
            mod.harden_normals = True
            mod.miter_outer = "MITER_ARC"
            wn = obj.modifiers.new("WeightedNormal", "WEIGHTED_NORMAL")
            wn.keep_sharp = True
        return obj


UV_METRES = 24.0


def box_uv(bm):
    """Box projection in metres / UV_METRES, matching the game's panel texture scale."""
    uv = bm.loops.layers.uv.verify()
    for f in bm.faces:
        n = f.normal
        ax, ay, az = abs(n.x), abs(n.y), abs(n.z)
        for loop in f.loops:
            co = loop.vert.co
            if ax >= ay and ax >= az:
                loop[uv].uv = (co.y / UV_METRES, co.z / UV_METRES)
            elif ay >= az:
                loop[uv].uv = (co.x / UV_METRES, co.z / UV_METRES)
            else:
                loop[uv].uv = (co.x / UV_METRES, co.y / UV_METRES)


def empty(name, col, location, parent=None, size=1.0):
    """An empty; with a parent, `location` is in the parent's local space."""
    e = bpy.data.objects.new(name, None)
    e.empty_display_type = "PLAIN_AXES"
    e.empty_display_size = size
    col.objects.link(e)
    if parent is not None:
        e.parent = parent
    e.location = location
    return e


def marker(name, col, location, parent):
    """A one-triangle mesh used as a named point. FBX bake-space export mishandles empties
    under offset parents; meshes come through correctly. The game hides these."""
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata([(0, 0, 0), (0.01, 0, 0), (0, 0.01, 0)], [], [(0, 1, 2)])
    o = bpy.data.objects.new(name, mesh)
    col.objects.link(o)
    o.parent = parent
    o.location = location
    return o


def apply_modifiers(obj):
    dg = bpy.context.evaluated_depsgraph_get()
    ev = obj.evaluated_get(dg)
    mesh = bpy.data.meshes.new_from_object(ev)
    old = obj.data
    obj.modifiers.clear()
    obj.data = mesh
    bpy.data.meshes.remove(old)


def join(objs, name):
    """Applies modifiers and joins into one object named `name`."""
    for o in objs:
        apply_modifiers(o)
    ctx = bpy.context
    for o in ctx.selected_objects:
        o.select_set(False)
    for o in objs:
        o.select_set(True)
    ctx.view_layer.objects.active = objs[0]
    bpy.ops.object.join()
    obj = ctx.view_layer.objects.active
    obj.name = name
    obj.data.name = name
    return obj


def export_fbx(path, objects):
    ctx = bpy.context
    for o in ctx.selected_objects:
        o.select_set(False)
    for o in objects:
        o.select_set(True)
        for c in o.children_recursive:
            c.select_set(True)
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, object_types={"MESH", "EMPTY"},
        axis_forward="Z", axis_up="Y", apply_unit_scale=True, apply_scale_options="FBX_SCALE_UNITS",
        bake_space_transform=True, use_mesh_modifiers=True, mesh_smooth_type="OFF", use_tspace=False,
        add_leaf_bones=False, bake_anim=False)


def tri_count(obj):
    return sum(len(p.vertices) - 2 for p in obj.data.polygons)
