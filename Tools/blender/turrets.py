"""Turret parts kit. Each turret exports as House (origin at the yaw pivot on the deck) with a
Barrels child (origin at the pitch pivot, PivotHeight up) and Muzzle_N markers at the barrel
tips (children of House at rest pose; the game reparents them under Barrels). Dimensions match MountDef.MuzzleLength / PivotHeight in Hulls.cs.

Short, chunky barrels in proportion to the gunhouse; armoured, faceted houses.
"""
from mathutils import Vector

import grkit as k


def house_ring(w, top, chamfer):
    return [(-w, 0.6), (w, 0.6), (w, top - chamfer), (w - chamfer * 1.2, top),
            (-w + chamfer * 1.2, top), (-w, top - chamfer)]


def build_turret(col, name, width, back, front, height, pivot, spacing, radius, muzzle, barrels=2,
                 rangefinder=False, bustle=True, hoods=True):
    w = width * 0.5
    c = height * 0.22
    house = k.Part(f"{name}_House", col)
    stations = []
    if bustle:
        stations.append((back, house_ring(w * 0.82, height * 0.84, c)))
        stations.append((back + (front - back) * 0.2, house_ring(w, height, c)))
    else:
        stations.append((back, house_ring(w, height, c)))
    stations.append((back + (front - back) * 0.72, house_ring(w, height, c)))
    front_top = max(height * 0.6, pivot + radius * 2.2)
    stations.append((front, house_ring(w * 0.86, front_top, c * 0.8)))
    house.loft(stations, "Paint")
    house.cyl((0, 0, -0.6), (0, 0, 0.8), w * 0.98, w * 0.95, seg=32, slot="Dark")
    house.cyl((0, 0, 0.55), (0, 0, 0.8), w * 1.0, w * 1.0, seg=32, slot="Trim")
    if hoods:
        for s in (-1, 1):
            house.box((s * w * 0.45, back + (front - back) * 0.45, height + 0.35 * c), (w * 0.3, (front - back) * 0.16, 0.7 * c),
                      "Paint", chamfer=0.2 * c, taper=0.8)
    if rangefinder:
        yb = back + (front - back) * 0.3
        house.box((0, yb, height - 0.9 * c), (width * 1.18, c * 1.2, c * 1.1), "Dark", chamfer=0.25 * c)
        for s in (-1, 1):
            house.cyl((s * width * 0.59, yb, height - 0.9 * c), (s * width * 0.63, yb, height - 0.9 * c), c * 0.75, seg=12, slot="Dark")
        house.box((0, back + 1.0, height * 0.55), (width * 0.6, 0.3, 0.4), "Trim")
    h = house.build(bevel=min(0.15, height * 0.03), smooth_angle=30)

    bar = k.Part(f"{name}_Barrels", col)
    mantlet_w = spacing * (barrels - 1) + radius * 4.0
    bar.box((0, front - 0.4, pivot), (mantlet_w, 1.8, radius * 3.8), "Dark", chamfer=radius * 0.8)
    muzzles = []
    for i in range(barrels):
        x = (i - (barrels - 1) * 0.5) * spacing
        L = muzzle
        bar.cyl((x, front, pivot), (x, front + L * 0.22, pivot), radius * 1.75, radius * 1.55, seg=16, slot="Dark")
        bar.cyl((x, front + L * 0.22, pivot), (x, L - radius * 1.7, pivot), radius * 1.12, radius * 0.98, seg=16, slot="Dark")
        bar.cyl((x, L - radius * 1.7, pivot), (x, L, pivot), radius * 1.42, radius * 1.36, seg=16, slot="Dark")
        # Recessed bore: a dark cone set back up the barrel with a short glowing throat,
        # rather than a flat Heat disc capping the muzzle.
        bar.cyl((x, L - radius * 2.6, pivot), (x, L - radius * 0.15, pivot), radius * 0.16,
                radius * 0.62, seg=14, slot="Dark")
        bar.cyl((x, L - radius * 0.5, pivot), (x, L - radius * 0.12, pivot), radius * 0.42,
                radius * 0.5, seg=10, slot="Heat")
        muzzles.append(Vector((x, L, pivot)))
    b = bar.build(bevel=min(0.08, radius * 0.1), smooth_angle=40, origin=(0, 0, pivot))
    b.parent = h
    for i, p in enumerate(muzzles):
        k.marker(f"Muzzle_{i}", col, p, parent=h)  # direct child of House: FBX baking breaks grandchildren
    return h


# name, width, back, front, height, pivot, spacing, radius, muzzle length, rangefinder
TURRETS = {
    "main_battery": dict(width=13.0, back=-9.5, front=6.0, height=7.2, pivot=3.8, spacing=2.7, radius=0.85, muzzle=17.0, rangefinder=True),
    "secondary": dict(width=6.6, back=-4.6, front=3.0, height=4.0, pivot=2.2, spacing=1.45, radius=0.45, muzzle=9.0, rangefinder=False),
    "point_defence": dict(width=3.2, back=-1.7, front=1.4, height=2.7, pivot=1.8, spacing=0.75, radius=0.22, muzzle=5.0, bustle=False, hoods=False),
    "corvette_gun": dict(width=2.6, back=-1.6, front=1.3, height=2.2, pivot=1.5, spacing=0.62, radius=0.2, muzzle=4.5, bustle=False, hoods=False),
}


def build_all(col):
    out = {}
    x = 0.0
    for name, spec in TURRETS.items():
        h = build_turret(col, name, **spec)
        h.location = (x, 0, 0)
        x += spec["width"] * 2.5
        out[name] = h
    return out
