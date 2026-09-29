"""Continuance line battleship, 354 m.

Built around the hardpoints in Assets/Scripts/Sim/Scenarios/Hulls.cs (BuildBattleship).
Design reasons, bow to stern:
- Armoured prow and debris deflector: the bow takes fire on the approach.
- A/B and X/Y main turrets on the centreline, B and X superfiring: every main gun bears
  on either beam, and the pairs cover bow and stern.
- Armour belt over the citadel; replacement plates in a different finish (the Continuance
  keeps old hulls in service and it shows).
- Missile cells aft of Y turret.
- Citadel superstructure with the bridge forward and fire-control directors on top.
- Radiator panels aft of the citadel: a warship in vacuum dumps heat or cooks.
- Three main engines plus two manoeuvring engines: redundancy, no single point of failure.
- No ventral guns.
"""
import math
import random

from mathutils import Matrix, Vector

import grkit as k

DECK = 12.0
SUPERFIRE = 20.0

# y station, half-beam, deck height, keel depth
STATIONS = [
    (182.0, 1.0, 9.0, 0.0),
    (176.0, 4.5, 11.0, -4.0),
    (160.0, 10.5, 12.6, -11.0),
    (135.0, 17.0, 12.4, -16.0),
    (100.0, 22.0, DECK, -18.0),
    (-100.0, 23.0, DECK, -18.0),
    (-140.0, 21.5, DECK, -16.0),
    (-165.0, 19.5, DECK, -13.0),
    (-172.0, 18.5, 11.6, -12.0),
]


def section(w, top, bottom):
    h = top - bottom
    mid = (top + bottom) * 0.5
    half = [
        (0.78 * w, top),
        (0.95 * w, top - 0.16 * h),
        (w, mid + 0.12 * h),
        (w, mid - 0.2 * h),
        (0.8 * w, bottom + 0.12 * h),
        (0.36 * w, bottom),
    ]
    return half + [(-x, z) for x, z in reversed(half)]


def station_at(y):
    for (y0, w0, t0, b0), (y1, w1, t1, b1) in zip(STATIONS, STATIONS[1:]):
        if y1 <= y <= y0:
            t = (y0 - y) / (y0 - y1)
            return w0 + (w1 - w0) * t, t0 + (t1 - t0) * t, b0 + (b1 - b0) * t
    return STATIONS[-1][1:]


def build(col):
    rnd = random.Random(7)
    hull = k.Part("Hull", col)
    hull.loft([(y, section(w, t, b)) for y, w, t, b in STATIONS], "Paint")
    parts = [hull.build(bevel=0.25, smooth_angle=20)]

    det = k.Part("Detail", col)

    # Armour belt: plates slightly proud of the hull, gaps between them; some replaced.
    y = 120.0
    while y > -150.0:
        length = rnd.uniform(14.0, 19.0)
        yc = y - length * 0.5
        w, top, bottom = station_at(yc)
        h = top - bottom
        mid = (top + bottom) * 0.5
        z0, z1 = mid - 0.2 * h, mid + 0.12 * h
        repaired = rnd.random() < 0.18
        for s in (-1, 1):
            det.box((s * (w + 0.45), yc, (z0 + z1) * 0.5), (0.9, length - 0.7, (z1 - z0) + 0.8),
                    "Dark" if repaired else "Paint", chamfer=0.3)
        y -= length

    # Deck-edge rails in trim, following the sheer.
    for (y0, w0, t0, _), (y1, w1, t1, _) in zip(STATIONS[1:], STATIONS[2:]):
        for s in (-1, 1):
            a = Vector((s * 0.78 * w0, y0, t0 + 0.35))
            b = Vector((s * 0.78 * w1, y1, t1 + 0.35))
            det.cyl(a, b, 0.22, seg=6, slot="Trim")

    # Debris deflector ahead of A turret: a shallow V of armour.
    for s in (-1, 1):
        m = Matrix.Translation((s * 4.2, 121.0, DECK + 1.2)) @ Matrix.Rotation(s * math.radians(-28), 4, "Z")
        det.box((0, 0, 0), (9.5, 1.2, 2.4), "Paint", matrix=m, chamfer=0.3, taper=0.85)

    # Barbettes. A and Y: low armour rings; B and X: raised for superfiring.
    for yb in (104.0, -98.0):
        det.cyl((0, yb, DECK - 0.8), (0, yb, DECK + 0.9), 8.9, 8.6, seg=32, slot="Dark")
    for yb in (72.0, -66.0):
        det.cyl((0, yb, DECK - 1.0), (0, yb, SUPERFIRE - 0.6), 9.8, 9.3, seg=32, slot="Paint")
        det.cyl((0, yb, SUPERFIRE - 0.6), (0, yb, SUPERFIRE + 0.8), 9.0, 8.8, seg=32, slot="Dark")
        for zb in (DECK + 2.5, DECK + 5.5):
            det.cyl((0, yb, zb), (0, yb, zb + 0.4), 9.95, 9.95, seg=32, slot="Trim")

    # Citadel: level 1, running from behind B barbette to ahead of X barbette.
    det.box((0, -7.0, 19.0), (28.0, 88.0, 14.0), "Paint", chamfer=2.6, taper=0.86)
    det.box((0, -7.0, 26.15), (21.0, 82.0, 0.3), "Dark")
    # Window rows on the citadel sides.
    for yy in range(-46, 34, 4):
        if rnd.random() < 0.3:
            continue
        for s in (-1, 1):
            if rnd.random() < 0.8:
                det.box((s * 13.4, yy, 17.0), (0.15, 1.8, 0.8), "Windows")
            if rnd.random() < 0.5:
                det.box((s * 12.9, yy, 21.5), (0.15, 1.8, 0.8), "Windows")

    # Level 2 deckhouse along the citadel top.
    det.box((0, -10.0, 29.0), (20.0, 50.0, 6.0), "Paint", chamfer=1.4, taper=0.86)
    for yy in range(-32, 14, 3):
        for s in (-1, 1):
            if rnd.random() < 0.55:
                det.box((s * 9.2, yy, 29.0), (0.15, 1.6, 0.7), "Windows")

    # Bridge tower, bridge, directors, tripod mast.
    det.box((0, 22.0, 35.0), (16.0, 18.0, 18.0), "Paint", chamfer=1.8, taper=0.8, taper_y=0.84)
    det.box((0, 25.0, 46.4), (26.0, 11.0, 4.8), "Paint", chamfer=1.0, taper=0.95)
    det.box((0, 30.6, 46.6), (21.0, 0.25, 1.3), "Windows")
    for s in (-1, 1):
        det.box((s * 13.1, 25.0, 46.6), (0.25, 8.5, 1.3), "Windows")
        for zz in (33.0, 38.0):
            det.box((s * 7.2, 22.0, zz), (0.15, 12.0, 0.7), "Windows")
    det.box((0, 23.0, 48.9), (22.0, 9.0, 0.3), "Dark")
    for yd, zd in ((26.0, 50.8), (6.0, 33.8)):
        det.box((0, yd, zd), (6.0, 5.0, 3.2), "Paint", chamfer=0.8, taper=0.8)
        det.box((0, yd - 1.0, zd + 0.6), (14.0, 1.2, 1.2), "Dark", chamfer=0.3)
        for s in (-1, 1):
            det.cyl((s * 7.0, yd - 1.0, zd + 0.6), (s * 7.6, yd - 1.0, zd + 0.6), 0.9, seg=10, slot="Dark")
    base = [Vector((0, 27.0, 49.0)), Vector((-4.5, 18.0, 49.0)), Vector((4.5, 18.0, 49.0))]
    apex = Vector((0, 20.5, 74.0))
    for b in base:
        det.cyl(b, apex, 0.9, 0.5, seg=8, slot="Dark")
    det.cyl(apex, apex + Vector((0, 0, 9.0)), 0.5, 0.2, seg=6, slot="Dark")
    for zz, span in ((64.0, 18.0), (70.0, 11.0)):
        det.box((0, 20.8 - (zz - 64.0) * 0.1, zz), (span, 0.5, 0.5), "Dark")
    det.box((0, 21.4, 59.0), (7.0, 1.0, 4.0), "Dark", chamfer=0.4)  # search radar
    det.box((0, 20.5, 83.3), (0.6, 0.6, 0.6), "Heat")
    # Sensor domes and comms on the deckhouse.
    for x, y in ((-6.0, -2.0), (6.0, -2.0)):
        det.cyl((x, y, 32.0), (x, y, 33.2), 2.6, 2.2, seg=16, slot="Dark")
        det.cyl((x, y, 33.2), (x, y, 34.4), 2.2, 1.0, seg=16, slot="Paint")

    # Aft heat-exchange stack feeding the radiator arrays.
    det.box((0, -42.0, 34.0), (16.0, 26.0, 16.0), "Paint", chamfer=1.6, taper=0.78, taper_y=0.9)
    det.box((0, -42.0, 42.3), (9.0, 18.0, 0.6), "Heat")
    det.box((0, -42.0, 42.2), (11.0, 21.0, 0.4), "Dark")
    for s in (-1, 1):
        m = (Matrix.Translation((s * 5.2, -42.0, 38.0)) @ Matrix.Rotation(s * math.radians(26), 4, "Y")
             @ Matrix.Translation((0, 0, 10.0)))
        det.box((0, 0, 0), (0.9, 24.0, 20.0), "Dark", matrix=m, chamfer=0.2)
        for i in range(-3, 4):
            det.box((s * 0.47, 0, i * 2.6), (0.08, 22.5, 0.35), "Heat", matrix=m)
        det.box((0, 0, 10.3), (1.3, 24.4, 0.6), "Paint", matrix=m)
        det.cyl((s * 5.2, -54.0, 38.0), (s * 5.2, -30.0, 38.0), 1.1, seg=10, slot="Dark")

    # Vertical launch cells aft of Y turret (missile racks come later; the hatches are armour).
    for i in range(6):
        for j in range(3):
            det.box(((j - 1) * 3.0, -114.0 - i * 3.0, DECK + 0.15), (2.4, 2.4, 0.35), "Dark", chamfer=0.15)
    det.box((0, -121.5, DECK + 0.05), (10.4, 19.6, 0.2), "Trim")

    # Secondary sponsons with casemate armour under them.
    for z in (40.0, 2.0, -36.0):
        for s in (-1, 1):
            det.box((s * 21.2, z, 9.4), (8.5, 13.5, 3.2), "Paint", chamfer=0.9)
            det.box((s * 22.0, z, 6.2), (5.0, 11.0, 3.6), "Paint", chamfer=0.9, taper=1.3)

    # Point-defence tubs.
    for x, y_up, z in ((14, 12, 132), (16, 26, 14), (14, 12, -128)):
        for s in (-1, 1):
            p = k.hp(s * x, y_up, z)
            det.cyl(p - Vector((0, 0, 1.4)), p + Vector((0, 0, 0.2)), 2.8, 2.4, seg=16, slot="Dark")

    # Deck furniture: hatches and access plates.
    for _ in range(60):
        yy = rnd.uniform(-160.0, 150.0)
        if any(abs(yy - yb) < 12 for yb in (104, 72, -66, -98)) or -52 < yy < 38:
            continue
        w, top, _ = station_at(yy)
        xx = rnd.uniform(-0.6, 0.6) * w
        det.box((xx, yy, top + 0.12), (rnd.uniform(1.5, 4.0), rnd.uniform(1.5, 5.0), 0.3), "Dark", chamfer=0.1)

    # Keel spine and a sensor blister under the bow. No weapons underneath.
    det.box((0, -10.0, -18.6), (5.5, 250.0, 2.2), "Dark", chamfer=0.6)
    det.cyl((0, 128.0, -15.0), (0, 128.0, -17.2), 5.0, 3.6, seg=20, slot="Dark")

    # RCS thruster blocks at the corners.
    for yy in (150.0, -150.0):
        w, top, bottom = station_at(yy)
        for s in (-1, 1):
            det.box((s * (w + 0.2), yy, 3.0), (2.4, 5.0, 3.0), "Dark", chamfer=0.4)

    # Engine block and nozzles: three mains in a row, two manoeuvring engines above.
    det.box((0, -175.5, -1.5), (40.0, 7.0, 23.0), "Dark", chamfer=3.0, taper=0.9)
    engines = []
    for x, z, r, y0 in ((0.0, -3.0, 6.5, -179.0), (-14.0, -3.0, 5.5, -178.5), (14.0, -3.0, 5.5, -178.5),
                        (-7.0, 7.5, 2.6, -178.5), (7.0, 7.5, 2.6, -178.5)):
        length = r * 1.4
        det.cyl((x, y0 + 1.0, z), (x, y0, z), r * 1.12, r * 1.12, seg=24, slot="Trim")
        det.cyl((x, y0, z), (x, y0 - length, z), r * 0.85, r * 1.1, seg=24, slot="Dark")
        det.cyl((x, y0 - length, z), (x, y0 - length - 0.05, z), r * 0.98, r * 0.98, seg=24, slot="Engine")
        for i in range(3):
            yy = y0 - length * (0.25 + i * 0.25)
            rr = r * (0.85 + 0.25 * (0.25 + i * 0.25)) + 0.12
            det.cyl((x, yy, z), (x, yy - 0.35, z), rr, rr, seg=24, slot="Dark")
        engines.append((Vector((x, y0 - length - 0.05, z)), r))

    parts.append(det.build(bevel=0.12, smooth_angle=30))
    obj = k.join(parts, "Battleship")

    for i, (p, r) in enumerate(engines):
        k.marker(f"Engine_{i}_r{r:.2f}", col, p, parent=obj)
    return obj
