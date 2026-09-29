"""Builds every model and exports FBX into the Unity project."""
import os

import bpy

import battleship
import grkit as k
import turrets

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Assets", "Art", "Models")


def run(ships=None):
    os.makedirs(OUT, exist_ok=True)
    written = []
    tcol = k.collection("Turrets")
    kit = turrets.build_all(tcol)
    for name, house in kit.items():
        house.location = (0, 0, 0)
        path = os.path.join(OUT, f"turret_{name}.fbx")
        k.export_fbx(path, [house])
        written.append(path)
    builders = {"battleship": battleship.build}
    import escorts
    import transport
    builders.update(escorts.BUILDERS)
    builders["transport"] = transport.build
    x = 0.0
    for name, build in builders.items():
        if ships and name not in ships:
            continue
        col = k.collection(f"Hull_{name}")
        obj = build(col)
        path = os.path.join(OUT, f"hull_{name}.fbx")
        k.export_fbx(path, [obj])
        written.append(path)
        obj.location.x = x
        x += 120.0
    return written
