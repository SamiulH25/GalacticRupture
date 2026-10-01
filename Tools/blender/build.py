"""Headless entry point: build and export every model.

    blender -b -P build.py --                       # everything
    blender -b -P build.py -- continuance           # one faction
    blender -b -P build.py -- --classes corvette    # one class, every faction
    blender -b -P build.py -- remnant --no-turrets
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import check_contract  # noqa: E402
import export_all  # noqa: E402

import fleet as f  # noqa: E402


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []

    with_turrets = "--no-turrets" not in argv
    argv = [a for a in argv if a != "--no-turrets"]

    classes = None
    if "--classes" in argv:
        i = argv.index("--classes")
        if i + 1 >= len(argv):
            raise SystemExit("--classes needs a comma-separated list")
        classes = [c.strip() for c in argv[i + 1].split(",") if c.strip()]
        argv = argv[:i] + argv[i + 2:]

    unknown = [a for a in argv if a not in f.FACTIONS]
    if unknown:
        raise SystemExit(f"unknown faction(s) {unknown}; known: {list(f.FACTIONS)}")
    bad = [c for c in classes or [] if c not in f.CLASSES]
    if bad:
        raise SystemExit(f"unknown class(es) {bad}; known: {list(f.CLASSES)}")

    # The contract is a mirror of Hulls.cs; check it before spending time on geometry.
    check_contract.run()

    written = export_all.run(factions=argv or None, classes=classes, with_turrets=with_turrets)
    print(f"[build] {len(written)} model(s) written")

main()
