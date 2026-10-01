"""Guards `fleet.CONTRACT` against drift from the sim.

`fleet.py` mirrors the hardpoints in `Assets/Scripts/Sim/Scenarios/Hulls.cs` so hull
builders can check themselves. That mirror is the one thing in this pipeline that can rot
silently: move a gun in C#, forget the models, and every faction's turret hangs in the air
or sits inside the hull with no build error. This parses the `Hp(...)` calls back out of
Hulls.cs and compares them, and `build.py` runs it before any geometry is built.
"""
import os
import re

import fleet as f

HULLS_CS = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..",
                        "Assets", "Scripts", "Sim", "Scenarios", "Hulls.cs")

BUILDERS = {"Battleship": "battleship", "Destroyer": "destroyer", "Frigate": "frigate",
            "Corvette": "corvette", "Transport": "transport"}

# Locals the hardpoint calls use: Hulls.BattleshipDeck / Hulls.BattleshipSuperfire.
ALIAS = {"d": f.CONTRACT["battleship"]["deck"], "s": f.SUPERFIRE}


def _value(expr, sign=1):
    """Evaluates the small C# numeric expressions the Hp() arguments use."""
    e = re.sub(r"(\d)f\b", r"\1", expr.strip().replace(" ", ""))
    for name, v in ALIAS.items():
        e = e.replace(name, repr(v))
    e = e.replace("--", "+ -")
    e = re.sub(r"(?<![\d.])-", "+-", e)
    return sign * float(eval(e))


def _mount(name):
    """Hulls.cs spells mounts MainBattery; fleet.py spells them main_battery."""
    return name.lower().replace("_", "")


def parse(src):
    """[(class, [(mount, x, y_up, z_fwd, base_yaw)])] straight from Hulls.cs."""
    out = {}
    for cls_name, body in re.findall(r"static HullDef Build(\w+)\(\)\s*\n?\s*\{(.*?)\n        \}",
                                     src, re.S):
        found = []

        # `for (int i = 0; i < N; i++) { float z = <expr>; ... Hp(Mount, s * x, y, z, s * yaw, ...) }`
        for loop in re.finditer(r"for \(int i = 0; i < (\d+); i\+\+\)\s*\{(.*?)\n            \}",
                                body, re.S):
            inner = loop.group(2)
            zexpr = re.search(r"float z = ([^;]+);", inner).group(1)
            m = re.search(r"Hp\((\w+),\s*([^,]+),\s*([^,]+),\s*z,\s*([^,]+),", inner)
            mount, xexpr, yexpr, yawexpr = m.groups()
            for k in range(int(loop.group(1))):
                z = _value(zexpr.replace("i", str(k)))
                for sign in (1, -1):
                    found.append((_mount(mount), _value(xexpr, sign), _value(yexpr), z,
                                  _value(yawexpr, sign)))

        for m in re.finditer(r"Hp\((\w+),\s*([^,]+),\s*([^,]+),\s*([^,]+),\s*([^,]+),", body):
            mount, x, y, z, yaw = (g.strip() for g in m.groups())
            if z == "z":      # the loop form, already collected above
                continue
            sign = -1 if x.startswith("-s") else 1
            found.append((_mount(mount), _value(x, sign), _value(y), _value(z), _value(yaw, sign)))

        out[BUILDERS[cls_name]] = sorted(found)
    return out


def run():
    """Raises AssertionError describing any drift between Hulls.cs and fleet.CONTRACT."""
    with open(HULLS_CS) as fh:
        parsed = parse(fh.read())

    problems = []
    for cls in f.CLASSES:
        if cls not in parsed:
            problems.append(f"{cls}: no Build{cls.capitalize()}() found in Hulls.cs")
            continue
        contract = sorted((_mount(m), x, y, z, yaw)
                          for m, x, y, z, yaw in f.CONTRACT[cls]["mounts"])
        if contract != parsed[cls]:
            problems.append(f"{cls}: hull builders disagree with Hulls.cs")
            for entry in contract:
                if entry not in parsed[cls]:
                    problems.append(f"    only in fleet.CONTRACT: {entry}")
            for entry in parsed[cls]:
                if entry not in contract:
                    problems.append(f"    only in Hulls.cs      : {entry}")

    if problems:
        raise AssertionError("CONTRACT HAS DRIFTED FROM Hulls.cs:\n" + "\n".join(problems))
    total = sum(len(v) for v in parsed.values())
    print(f"[contract] {total} hardpoints across {len(parsed)} classes match Hulls.cs")


if __name__ == "__main__":
    run()
