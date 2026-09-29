"""Troop transport, 118 m. Carries one army and the landers to put it on the ground.

Design reasons:
- A long armoured box: the army lives inside the citadel, the part of the ship that must survive.
- Landers racked along both flanks in cradles, so a landing is a broadside release, fast.
- Bridge forward and high, over the bow; point defence fore and aft (Hulls.cs BuildTransport).
- Two main engines and two auxiliaries: a transport that loses an engine must still get out.
- No guns under it, nothing that competes with the escorts' job.
"""
import math

from mathutils import Matrix, Vector

import grkit as k

DECK = 8.0


def section(w, top, bottom):
    # Boxy with chamfered corners: a hold, not a warship's fine lines.
    c = min(w, top - bottom) * 0.18
    half = [(w - c, top), (w, top - c), (w, bottom + c), (w - c, bottom)]
    return half + [(-x, z) for x, z in reversed(half)]


STATIONS = [
    (59.0, 3.0, 6.0, -3.0),
    (54.0, 8.0, 7.5, -7.0),
    (44.0, 11.0, DECK, -9.0),
    (-44.0, 11.0, DECK, -9.0),
    (-54.0, 9.5, 7.5, -8.0),
    (-58.0, 9.0, 7.0, -7.5),
]


def build(col):
    hull = k.Part("Hull", col)
    hull.loft([(y, section(w, t, b)) for y, w, t, b in STATIONS], "Paint")
    parts = [hull.build(bevel=0.2, smooth_angle=20)]
    d = k.Part("Detail", col)

    # Armoured hold plates and hatch lines along the top.
    for i in range(8):
        y = 36.0 - i * 10.0
        d.box((0, y, DECK + 0.25), (18.0, 8.6, 0.5), "Paint", chamfer=0.3)
        d.box((0, y, DECK + 0.55), (14.0, 0.4, 0.15), "Dark")

    # Lander cradles along both flanks: landers in racks, ready to drop.
    for s in (-1, 1):
        for i in range(5):
            y = 30.0 - i * 14.0
            d.box((s * 12.6, y, -1.0), (2.2, 12.0, 12.0), "Dark", chamfer=0.4)
            m = Matrix.Translation((s * 15.2, y, -1.0))
            d.box((0, 0, 0), (3.2, 9.5, 5.0), "Paint", matrix=m, chamfer=0.9, taper=0.8)
            d.box((s * 1.65, 0, 0.8), (0.1, 6.0, 1.0), "Windows", matrix=m)
            d.cyl((s * 15.2, y - 5.0, -1.0), (s * 15.2, y - 5.8, -1.0), 1.2, 1.4, seg=10, slot="Engine")
        d.box((s * 11.2, -6.0, DECK - 1.0), (0.4, 88.0, 0.5), "Trim")

    # Bridge forward and high.
    d.box((0, 40.0, DECK + 4.0), (12.0, 10.0, 8.0), "Paint", chamfer=1.2, taper=0.8, taper_y=0.8)
    d.box((0, 44.3, DECK + 6.0), (9.0, 0.2, 1.2), "Windows")
    d.cyl((0, 38.0, DECK + 8.0), (0, 38.0, DECK + 18.0), 0.5, 0.25, seg=6, slot="Dark")
    d.box((0, 38.0, DECK + 14.0), (6.0, 0.4, 0.4), "Dark")

    # Point-defence tubs at the sim's hardpoints.
    for (x, yu, z) in ((0, 9, 34), (0, 9, -30)):
        p = k.hp(x, yu, z)
        d.cyl(p - Vector((0, 0, 1.2)), p + Vector((0, 0, 0.3)), 2.2, 2.0, seg=14, slot="Dark")

    # Radiator fin aft and the engine block.
    d.box((0, -40.0, DECK + 4.5), (0.8, 14.0, 9.0), "Dark", chamfer=0.2, taper=0.4)
    for j in range(-1, 2):
        d.box((0.45, -40.0, DECK + 4.5 + j * 2.4), (0.06, 11.0, 0.3), "Heat")
        d.box((-0.45, -40.0, DECK + 4.5 + j * 2.4), (0.06, 11.0, 0.3), "Heat")
    d.box((0, -60.0, -0.5), (20.0, 4.0, 14.0), "Dark", chamfer=1.2)
    engines = []
    for x, z, r in ((-5.0, -1.5, 3.8), (5.0, -1.5, 3.8), (0.0, 4.5, 2.0), (0.0, -6.5, 2.0)):
        y0 = -62.0
        length = r * 1.4
        d.cyl((x, y0 + 0.6, z), (x, y0, z), r * 1.12, seg=20, slot="Trim")
        d.cyl((x, y0, z), (x, y0 - length, z), r * 0.85, r * 1.1, seg=20, slot="Dark")
        d.cyl((x, y0 - length, z), (x, y0 - length - 0.04, z), r * 0.98, seg=20, slot="Engine")
        engines.append((Vector((x, y0 - length - 0.04, z)), r))

    parts.append(d.build(bevel=0.08, smooth_angle=30))
    obj = k.join(parts, "Transport")
    for i, (p, r) in enumerate(engines):
        k.marker(f"Engine_{i}_r{r:.2f}", col, p, parent=obj)
    return obj
