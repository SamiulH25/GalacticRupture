"""Places turret instances on a hull at the sim's hardpoints, for review renders only.

The layout comes from fleet.CONTRACT, which is transcribed from the same `Hp(...)` calls
the sim reads, so a dressed review ship is posed exactly as the game will pose it.
"""
import math

import bpy

import fleet as f
import grkit as k


def layout(cls):
    """[(mount_id, (x, y_up, z_fwd), base_yaw)] for a class."""
    return [(mount, (x, y_up, z), yaw) for mount, x, y_up, z, yaw in f.CONTRACT[cls]["mounts"]]


def dress(col, turrets, cls, yaw_offsets=None, pitch=0.0):
    k.collection(col.name)  # clear
    for i, (name, pos, base_yaw) in enumerate(layout(cls)):
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
