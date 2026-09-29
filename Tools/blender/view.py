"""Viewport framing for review screenshots."""
import math

import bpy
from mathutils import Euler, Vector


def look(target, yaw_deg, pitch_deg, distance, lens=50):
    """Orbit the viewport around `target`: yaw 0 looks from the bow toward the stern."""
    for area in bpy.context.screen.areas:
        if area.type != "VIEW_3D":
            continue
        space = area.spaces.active
        space.shading.type = "MATERIAL"
        space.clip_end = 20000
        space.lens = lens
        space.overlay.show_floor = False
        space.overlay.show_axis_x = False
        space.overlay.show_axis_y = False
        space.overlay.show_extras = False
        space.overlay.show_outline_selected = False
        r3d = space.region_3d
        r3d.view_perspective = "PERSP"
        r3d.view_location = Vector(target)
        r3d.view_distance = distance
        # View rotation: camera looks along its -Z. Start looking toward -Y (from the bow), then yaw/pitch.
        rot = Euler((math.radians(90 - pitch_deg), 0, math.radians(180 + yaw_deg)), "XYZ")
        r3d.view_rotation = rot.to_quaternion()
    for o in bpy.context.selected_objects:
        o.select_set(False)
