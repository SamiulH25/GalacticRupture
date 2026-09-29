"""Destroyer (Continuance) and the Remnant frigate and corvette.

Hardpoints follow Hulls.cs (BuildDestroyer, BuildFrigate, BuildCorvette).

Continuance destroyer: the battleship's grammar at escort scale. Two gun mounts fore and aft on
the centreline, point defence amidships, a compact citadel, redundant engines.

Remnant: keeled, one unbroken line bow to stern. A narrow hard-chined blade hull; the dorsal
spine runs the full length and rises into a fin that carries the radiators and sensors; a
ventral keel blade stiffens the hull (no guns under it). Built light and fast by a navy that
lost its yards.
"""
import math

from mathutils import Matrix, Vector

import grkit as k


def interp(stations, y):
    for a, b in zip(stations, stations[1:]):
        if b[0] <= y <= a[0]:
            t = (a[0] - y) / (a[0] - b[0])
            return tuple(a[i] + (b[i] - a[i]) * t for i in range(1, 4))
    return stations[-1][1:]


def continuance_section(w, top, bottom):
    h = top - bottom
    mid = (top + bottom) * 0.5
    half = [(0.78 * w, top), (0.95 * w, top - 0.16 * h), (w, mid + 0.12 * h), (w, mid - 0.2 * h),
            (0.8 * w, bottom + 0.12 * h), (0.36 * w, bottom)]
    return half + [(-x, z) for x, z in reversed(half)]


def remnant_section(w, top, bottom):
    # Hard chines: a flattened diamond with a narrow deck.
    half = [(0.32 * w, top), (0.82 * w, top * 0.45), (w, 0.0), (0.7 * w, bottom * 0.45), (0.18 * w, bottom)]
    return half + [(-x, z) for x, z in reversed(half)]


def nozzle(part, x, z, r, y0, engines):
    length = r * 1.4
    part.cyl((x, y0 + 0.6, z), (x, y0, z), r * 1.12, r * 1.12, seg=20, slot="Trim")
    part.cyl((x, y0, z), (x, y0 - length, z), r * 0.85, r * 1.1, seg=20, slot="Dark")
    part.cyl((x, y0 - length, z), (x, y0 - length - 0.04, z), r * 0.98, seg=20, slot="Engine")
    engines.append((Vector((x, y0 - length - 0.04, z)), r))


def finish(parts, name, col, engines):
    obj = k.join(parts, name)
    for i, (p, r) in enumerate(engines):
        k.marker(f"Engine_{i}_r{r:.2f}", col, p, parent=obj)
    return obj


def destroyer(col):
    st = [(76.0, 0.6, 4.5, 0.0), (72.0, 2.6, 5.5, -2.0), (62.0, 5.6, 6.3, -5.0), (46.0, 9.0, 6.2, -8.0),
          (-50.0, 10.0, 6.0, -8.0), (-64.0, 9.2, 6.0, -7.0), (-72.0, 8.5, 5.8, -6.0)]
    hull = k.Part("Hull", col)
    hull.loft([(y, continuance_section(w, t, b)) for y, w, t, b in st], "Paint")
    parts = [hull.build(bevel=0.15, smooth_angle=20)]
    d = k.Part("Detail", col)

    y = 52.0
    i = 0
    while y > -64.0:
        length = 11.0 + (i % 3) * 1.5
        yc = y - length * 0.5
        w, top, bottom = interp(st, yc)
        h = top - bottom
        mid = (top + bottom) * 0.5
        for s in (-1, 1):
            d.box((s * (w + 0.3), yc, mid - 0.04 * h), (0.6, length - 0.5, 0.36 * h), "Dark" if i % 5 == 3 else "Paint", chamfer=0.2)
        y -= length
        i += 1
    for (y0, w0, t0, _), (y1, w1, t1, _) in zip(st[1:], st[2:]):
        for s in (-1, 1):
            d.cyl((s * 0.78 * w0, y0, t0 + 0.2), (s * 0.78 * w1, y1, t1 + 0.2), 0.12, seg=6, slot="Trim")

    for yb in (40.0, -36.0):
        d.cyl((0, yb, 5.4), (0, yb, 6.6), 4.4, 4.2, seg=24, slot="Dark")
    d.box((0, 0.0, 10.5), (11.0, 40.0, 9.0), "Paint", chamfer=1.4, taper=0.85)
    d.box((0, 12.0, 18.0), (8.0, 9.0, 6.0), "Paint", chamfer=0.9, taper=0.82, taper_y=0.8)
    d.box((0, 14.0, 21.8), (12.0, 5.0, 2.4), "Paint", chamfer=0.5)
    d.box((0, 16.55, 22.0), (9.5, 0.15, 0.7), "Windows")
    for yy in range(-16, 18, 3):
        for s in (-1, 1):
            d.box((s * 5.1, yy, 9.5), (0.12, 1.2, 0.5), "Windows")
    for b in (Vector((0, 15.0, 23.0)), Vector((-2.2, 11.0, 23.0)), Vector((2.2, 11.0, 23.0))):
        d.cyl(b, Vector((0, 12.5, 36.0)), 0.4, 0.22, seg=6, slot="Dark")
    d.box((0, 12.5, 31.0), (7.0, 0.3, 0.3), "Dark")
    d.box((0, 12.5, 36.2), (0.3, 0.3, 0.3), "Heat")
    d.box((0, -13.0, 17.0), (8.0, 12.0, 4.0), "Paint", chamfer=0.8, taper=0.8)
    for s in (-1, 1):
        m = Matrix.Translation((s * 3.0, -13.0, 18.5)) @ Matrix.Rotation(s * math.radians(28), 4, "Y") @ Matrix.Translation((0, 0, 4.0))
        d.box((0, 0, 0), (0.4, 11.0, 8.0), "Dark", matrix=m, chamfer=0.1)
        for j in range(-2, 3):
            d.box((s * 0.22, 0, j * 1.5), (0.05, 10.2, 0.2), "Heat", matrix=m)
    for s in (-1, 1):
        p = k.hp(s * 7, 7, 8)
        d.cyl(p - Vector((0, 0, 1.4)), p + Vector((0, 0, 0.2)), 1.9, 1.7, seg=14, slot="Dark")
    for j in range(4):
        d.box(((j - 1.5) * 1.8, -52.0, 6.1), (1.5, 1.5, 0.3), "Dark", chamfer=0.1)
    d.box((0, -5.0, -8.6), (2.5, 90.0, 1.4), "Dark", chamfer=0.3)

    d.box((0, -73.5, -1.0), (15.0, 3.0, 10.0), "Dark", chamfer=1.2, taper=0.9)
    engines = []
    nozzle(d, -4.0, -1.5, 3.4, -75.0, engines)
    nozzle(d, 4.0, -1.5, 3.4, -75.0, engines)
    nozzle(d, 0.0, 3.2, 1.6, -74.8, engines)
    parts.append(d.build(bevel=0.08, smooth_angle=30))
    return finish(parts, "Destroyer", col, engines)


def remnant_blade(col, name, st, spine_peak, spine_y, fin_len, gun_ys, keel_depth, engines_spec, scale):
    hull = k.Part("Hull", col)
    hull.loft([(y, remnant_section(w, t, b)) for y, w, t, b in st], "Paint")
    parts = [hull.build(bevel=0.08 * scale, smooth_angle=15)]
    d = k.Part("Detail", col)

    # The spine: one unbroken line, low over the guns, rising to the fin.
    bow, stern = st[1][0], st[-1][0]
    stations = []
    n = 14
    for i in range(n + 1):
        y = bow + (stern - bow) * i / n
        _, top, _ = interp(st, y)
        rise = max(0.0, 1.0 - abs(y - spine_y) / fin_len)
        height = 0.6 * scale + spine_peak * rise ** 1.6
        half = 0.35 * scale * (1.0 - 0.4 * rise)
        stations.append((y, [(-half, top - 0.2), (half, top - 0.2), (half * 0.4, top + height), (-half * 0.4, top + height)]))
    d.loft(stations, "Paint")
    for i in range(len(stations) - 1):
        (ya, pa), (yb, pb) = stations[i], stations[i + 1]
        d.cyl((0, ya, pa[2][1] + 0.05), (0, yb, pb[2][1] + 0.05), 0.12 * scale, seg=6, slot="Trim")

    # Radiator vanes in the fin.
    for j in range(5):
        yy = spine_y + (j - 2) * fin_len * 0.22
        rise = max(0.0, 1.0 - abs(yy - spine_y) / fin_len)
        _, top, _ = interp(st, yy)
        hgt = spine_peak * rise ** 1.6 * 0.8
        if hgt > 0.5:
            for s in (-1, 1):
                d.box((s * 0.3 * scale, yy, top + hgt * 0.5), (0.1, fin_len * 0.12, hgt), "Heat")

    # Ventral keel blade: stiffener and sensor array, no guns.
    keel = []
    for i in range(n + 1):
        y = st[2][0] + (st[-2][0] - st[2][0]) * i / n
        _, _, bottom = interp(st, y)
        depth = keel_depth * math.sin(math.pi * i / n) ** 0.6
        keel.append((y, [(-0.3 * scale, bottom + 0.3), (0.3 * scale, bottom + 0.3), (0.12 * scale, bottom - depth), (-0.12 * scale, bottom - depth)]))
    d.loft(keel, "Dark")

    # Gun pads, bridge slit, hull lights.
    for gy in gun_ys:
        _, top, _ = interp(st, gy)
        d.cyl((0, gy, top - 0.3), (0, gy, top + 0.35), 1.6 * scale, 1.5 * scale, seg=18, slot="Dark")
    _, top, _ = interp(st, gun_ys[0] - 6 * scale)
    d.box((0, gun_ys[0] - 5 * scale, top + 0.1), (1.6 * scale, 0.2, 0.5 * scale), "Windows")
    for i in range(8):
        y = st[2][0] - (i + 1) * (st[2][0] - st[-2][0]) / 9
        w, top, bottom = interp(st, y)
        for s in (-1, 1):
            d.box((s * (w * 0.82 + 0.02), y, top * 0.45 - 0.2), (0.08, 0.9 * scale, 0.35 * scale), "Windows")

    engines = []
    y_end = st[-1][0]
    d.box((0, y_end - 0.6, 0.0), (st[-1][1] * 1.6, 1.4, (st[-1][2] - st[-1][3]) * 0.9), "Dark", chamfer=0.4 * scale)
    for x, z, r in engines_spec:
        nozzle(d, x, z, r, y_end - 1.2, engines)
    parts.append(d.build(bevel=0.05 * scale, smooth_angle=30))
    return finish(parts, name, col, engines)


def frigate(col):
    st = [(47.0, 0.3, 2.0, -1.0), (38.0, 3.5, 4.2, -4.0), (12.0, 7.0, 4.5, -8.0), (-20.0, 6.5, 4.5, -8.0),
          (-38.0, 5.0, 4.0, -5.0), (-43.0, 4.6, 3.6, -4.2)]
    return remnant_blade(col, "Frigate", st, spine_peak=9.0, spine_y=-4.0, fin_len=16.0, gun_ys=(22.0, -18.0),
                         keel_depth=6.0, engines_spec=((0.0, -0.6, 3.0), (-4.0, 1.0, 1.7), (4.0, 1.0, 1.7)), scale=1.0)


def corvette(col):
    st = [(25.0, 0.2, 1.4, -0.5), (18.0, 2.4, 2.8, -2.4), (2.0, 4.4, 3.2, -4.0), (-15.0, 4.1, 3.0, -3.5),
          (-21.0, 3.7, 2.6, -3.0), (-22.0, 3.5, 2.4, -2.8)]
    return remnant_blade(col, "Corvette", st, spine_peak=5.0, spine_y=-9.0, fin_len=10.0, gun_ys=(8.0,),
                         keel_depth=3.2, engines_spec=((-2.3, 0.0, 1.7), (2.3, 0.0, 1.7)), scale=0.55)


BUILDERS = {"destroyer": destroyer, "frigate": frigate, "corvette": corvette}
