"""Builds every model and exports FBX into the Unity project.

`blender -b -P build.py -- [faction ...]` filters by faction; `--classes corvette,frigate`
filters by class. With no arguments everything is rebuilt.

Hull files are named `hull_<class>_<faction>.fbx`; turret files stay shared by mount id,
since the sim defines one MountDef per class with no faction dimension.
"""
import importlib
import os

import bpy

import fleet as f
import grkit as k
import turrets

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Assets", "Art", "Models")


def builders(faction):
    """The five hull builders a faction module exposes, keyed by class."""
    mod = importlib.import_module(faction)
    return {cls: mod.BUILDERS[cls] for cls in f.CLASSES}


def run(factions=None, classes=None, with_turrets=True):
    os.makedirs(OUT, exist_ok=True)
    written = []
    if with_turrets:
        tcol = k.collection("Turrets")
        for name, house in turrets.build_all(tcol).items():
            house.location = (0, 0, 0)
            path = os.path.join(OUT, f"turret_{name}.fbx")
            k.export_fbx(path, [house])
            written.append(path)

    for faction in (factions or f.FACTIONS):
        built = builders(faction)
        for cls in classes or f.CLASSES:
            col = k.collection(f"Hull_{cls}_{faction}")
            obj = built[cls](col)
            # The contract is the sim's: fail the build rather than ship a hull whose
            # turrets would hang in the air or sit outside the beam.
            verts = f.check(cls, obj)
            path = os.path.join(OUT, f"hull_{cls}_{faction}.fbx")
            k.export_fbx(path, [obj])
            written.append(path)
            print(f"[build] {os.path.basename(path)} verts={verts} tris={f.tri_budget(obj)}")
    return written
