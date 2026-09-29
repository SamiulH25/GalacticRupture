"""Places turret instances on a hull at the sim's hardpoints, for review renders only."""
import math

import bpy

import grkit as k

BATTLESHIP = (
    # mount, (x, y_up, z_fwd) in Unity axes, base yaw
    ("main_battery", (0, 12, 104), 0), ("main_battery", (0, 20, 72), 0),
    ("main_battery", (0, 20, -66), 180), ("main_battery", (0, 12, -98), 180),
    *[("secondary", (s * 19, 11, z), s * 90) for z in (40, 2, -36) for s in (1, -1)],
    *[("point_defence", (s * x, y, z), s * yaw) for x, y, z, yaw in ((14, 12, 132, 45), (16, 26, 14, 90), (14, 12, -128, 135)) for s in (1, -1)],
)


def dress(col, turrets, layout, yaw_offsets=None, pitch=0.0):
    k.collection(col.name)  # clear
    for i, (name, pos, base_yaw) in enumerate(layout):
        src = turrets[name]
        h = bpy.data.objects.new(f"{name}_{i}", src.data)
        col.objects.link(h)
        h.location = k.hp(*pos)
        extra = (yaw_offsets or {}).get(i, 0.0)
        h.rotation_euler = (0, 0, -math.radians(base_yaw + extra))
        for child in src.children:
            if child.type != "MESH":
                continue
            c = bpy.data.objects.new(child.name + f"_{i}", child.data)
            col.objects.link(c)
            c.parent = h
            c.location = child.location
            c.rotation_euler = (math.radians(pitch), 0, 0)
