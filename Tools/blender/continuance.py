"""Continuance League hulls: charcoal and hazard amber, armoured and repairable.

The League keeps old hulls in service, so every class shows replacement plates in a
different finish and a generous amount of bolted-on structure. Grammar across the line:
a broad flared hull, guns on the centreline or in beam sponsons, a compact citadel set
low, and redundant propulsion -- a ship that loses an engine still gets home.

Five classes against fleet.CONTRACT: the deck plane and mount stations are shared with
the sim, so the faction reads through outline, superstructure and surface, not layout.
"""
import math
import random

from mathutils import Matrix, Vector

import fleet as f
import grkit as k

DECK = 12.0
SUPERFIRE = f.SUPERFIRE


def taper_x(half, z, z0, h, taper):
    """Outboard face x of a `Part.box` side wall at height `z`.

    `box` tapers linearly in x from `half` at the box bottom to `half * taper` at the top,
    so anything applied to that wall -- a window band, a seam -- has to follow it or it
    floats clear of the surface instead of sitting in it.
    """
    return half * (1.0 + (taper - 1.0) * (z - z0) / h)


def flank_x(stations, y, z, section=None):
    """Outboard surface x of the hull at station `y`, height `z`.

    Taken from the shared section outline rather than a guessed beam fraction: on
    `heavy_section` the flank is only truly vertical between the two shoulder points, so a
    fixed `0.82 * w` sits most of a metre *inside* the shell and any detail placed there
    is buried and invisible.
    """
    w, top, bottom = f.interp(stations, f.clamp_y(stations, y))
    z = max(bottom, min(top, z))
    pts = (section or f.heavy_section)(w, top, bottom)
    for (x0, z0), (x1, z1) in zip(pts, pts[1:]):
        if min(z0, z1) <= z <= max(z0, z1) and abs(z1 - z0) > 1e-6:
            return abs(x0 + (x1 - x0) * (z - z0) / (z1 - z0))
    return w


def lit_slot(part, x, y, z, size, axis="x"):
    """A window set into a surface.

    A dark coaming stands proud *around* the pane -- four bars, not a solid block, so the
    pane is not sealed behind it -- and the emissive slot sits a little below the coaming
    face. That is what makes it read as a lit window instead of a floating white box.
    """
    sx, sy, sz = size
    p = Vector((x, y, z))
    u, v = (sy, sz) if axis == "x" else (sx, sz)
    r = max(0.07, min(0.3, 0.35 * min(u, v)))
    out = Vector((math.copysign(r * 0.4, x), 0.0, 0.0)) if axis == "x" else Vector((0.0, math.copysign(r * 0.4, y), 0.0))
    wide = (sx + r, u + 2 * r) if axis == "x" else (u + 2 * r, sx + r)
    for s2 in (-1, 1):
        part.box(p + out + Vector((0.0, 0.0, s2 * (v + r) * 0.5)), (wide[0], wide[1], r), "Dark", chamfer=r * 0.3)
        part.box(p + out + Vector((0.0, s2 * (u + r) * 0.5, 0.0)), (wide[0], r, v), "Dark", chamfer=r * 0.3)
    pane_out = out * 0.625
    part.box(p + pane_out, (sx + 0.05, u, v) if axis == "x" else (u, sx + 0.05, v),
             "Windows", chamfer=0.02)


def flank_seam(part, stations, z_frac, y0, y1, step=10.0, width=0.55, proud=-0.16,
               slot="Dark", y_gap=(), section=None):
    """A longitudinal seam down both flanks, following the sheer and the section.

    `z_frac` is the height as a fraction of the local section depth measured up from the
    keel, so the run tracks the hull as it narrows instead of cutting straight across it.
    `y_gap` leaves stations clear so a seam does not run through a turret or sponson.
    """
    for s in (-1, 1):
        y, prev = y0, None
        while y >= y1:
            yc = f.clamp_y(stations, y)
            w, top, bottom = f.interp(stations, yc)
            z = bottom + z_frac * (top - bottom)
            p = Vector((s * (flank_x(stations, yc, z, section) + proud), yc, z))
            if prev is not None and not any(abs((p.y + prev.y) * 0.5 - g) < g_h for g, g_h in y_gap):
                part.cyl(prev, p, width * 0.5, seg=4, slot=slot)
            prev = p
            y -= step


def bank_frame(part, x, y, z, height, length, tilt_deg, posts, span=3.4):
    """Dark frame members across a radiator bank, so the hot faces read as segmented
    exchanger panels rather than one continuous glowing bar per slat."""
    tilt = Matrix.Rotation(math.radians(tilt_deg), 4, "Y")
    for i in range(posts):
        yy = y + length * (i / max(1, posts - 1.0) - 0.5) * 0.92
        m = Matrix.Translation((x, yy, z)) @ tilt
        part.box((0.0, 0.0, 0.0), (span, 0.5, height * 1.06), "Dark", matrix=m, chamfer=0.08)


# --------------------------------------------------------------------------- battleship
# y station, half-beam, deck height, keel depth. 354 m bow to stern.
STATIONS_BS = [
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


def battleship(col):
    """Armoured prow, A/B/X/Y centreline with B and X superfiring, armour belt over the
    citadel, missile cells aft of Y, radiators aft, three mains plus two manoeuvring."""
    rnd = random.Random(7)
    hull = k.Part("Hull", col)
    hull.loft([(y, f.heavy_section(w, t, b)) for y, w, t, b in STATIONS_BS], "Paint")
    parts = [hull.build(bevel=0.25, smooth_angle=20)]
    det = k.Part("Detail", col)

    # Stations that flank detail must keep clear of: the four main barbettes and the six
    # secondary sponsons.
    TURRET_Y = (104.0, 72.0, -66.0, -98.0, 40.0, 2.0, -36.0)

    # Armour belt: two plate courses either side of a joint strip. Standoff varies plate to
    # plate, the gaps are staggered and the lower course is offset half a plate against the
    # upper one, so the flank stops reading as a row of identical tiles.
    y = 128.0
    while y > -158.0:
        length = rnd.uniform(13.0, 20.0)
        yc = f.clamp_y(STATIONS_BS, y - length * 0.5)
        w, top, bottom = f.interp(STATIONS_BS, yc)
        h = top - bottom
        mid = (top + bottom) * 0.5
        repaired = rnd.random() < 0.18
        for course, (zc, zh, lead) in enumerate(((mid + 0.04 * h, 0.17 * h, 0.0),
                                                (mid - 0.17 * h, 0.09 * h, 0.55 * length))):
            yy = f.clamp_y(STATIONS_BS, yc - lead)
            thick = rnd.uniform(0.28, 0.64)
            zz = zc + rnd.uniform(-0.25, 0.25)
            for s in (-1, 1):
                det.box((s * (w + thick * 0.5 - 0.06), yy, zz),
                        (thick, length - rnd.uniform(0.7, 1.5), zh),
                        "Dark" if repaired else "Paint", chamfer=0.22)
                if course == 0 and not repaired and length > 15.0:
                    for b in range(4):
                        by = yy - length * 0.31 + b * (length * 0.62 / 3.0)
                        det.cyl((s * (w + thick - 0.06), by, zz - zh * 0.5 + 0.5),
                                (s * (w + thick + 0.1), by, zz - zh * 0.5 + 0.5), 0.24, seg=6, slot="Dark")
        y -= length

    # Plate joints across the strip between the two courses, and longitudinal seams up the
    # shoulder and down the turn of the bilge. Without these the flank is one 250 m
    # unbroken wall.
    f.panel_lines(det, STATIONS_BS, 1.0, -4.35, -6.75, count=24, y0=-156.0, y1=152.0,
                  width=1.1, proud=0.05, skip=TURRET_Y)
    flank_seam(det, STATIONS_BS, 0.90, 152.0, -156.0, step=11.0, width=0.9, proud=-0.26,
               y_gap=tuple((t, 11.0) for t in TURRET_Y))
    flank_seam(det, STATIONS_BS, 0.20, 152.0, -156.0, step=11.0, width=0.9, proud=-0.26)

    # Deck-edge trim. The band sits on the top of the vertical flank -- at `0.38 * h` below
    # the deck it follows the sheer exactly, rather than lying on the deck as a painted
    # stripe -- and runs fore and aft in two pieces, not right around the ship.
    f.strake(det, STATIONS_BS, 1.0, -11.4, y0=62.0, y1=152.0, width=2.0, proud=0.34)
    f.strake(det, STATIONS_BS, 1.0, -11.4, y0=-152.0, y1=-62.0, width=2.0, proud=0.34)

    # Debris deflector ahead of A turret.
    for s in (-1, 1):
        m = Matrix.Translation((s * 4.2, 121.0, DECK + 1.2)) @ Matrix.Rotation(s * math.radians(-28), 4, "Z")
        det.box((0, 0, 0), (9.5, 1.2, 2.4), "Paint", matrix=m, chamfer=0.3, taper=0.85)

    # Barbettes. A and Y low; B and X raised so they bear over the pair ahead.
    for yb in (104.0, -98.0):
        det.cyl((0, yb, DECK - 0.8), (0, yb, DECK + 0.9), 8.9, 8.6, seg=32, slot="Dark")
    for yb in (72.0, -66.0):
        det.cyl((0, yb, DECK - 1.0), (0, yb, SUPERFIRE - 0.6), 9.8, 9.3, seg=32, slot="Paint")
        det.cyl((0, yb, SUPERFIRE - 0.6), (0, yb, SUPERFIRE + 0.8), 9.0, 8.8, seg=32, slot="Dark")
        for zb in (DECK + 2.5, DECK + 5.5):
            det.cyl((0, yb, zb), (0, yb, zb + 0.4), 9.95, 9.95, seg=32, slot="Trim")

    # Citadel, level 1: from behind B barbette to ahead of X.
    det.box((0, -7.0, 19.0), (28.0, 88.0, 14.0), "Paint", chamfer=2.6, taper=0.86)
    det.box((0, -7.0, 26.15), (21.0, 82.0, 0.3), "Dark")
    for yy in range(-46, 34, 4):
        if rnd.random() < 0.3:
            continue
        for s in (-1, 1):
            if rnd.random() < 0.8:
                lit_slot(det, s * taper_x(14.0, 17.0, 12.0, 14.0, 0.86), yy, 17.0, (0.12, 1.8, 0.8))
            if rnd.random() < 0.5:
                lit_slot(det, s * taper_x(14.0, 21.5, 12.0, 14.0, 0.86), yy, 21.5, (0.12, 1.8, 0.8))

    det.box((0, -10.0, 29.0), (20.0, 50.0, 6.0), "Paint", chamfer=1.4, taper=0.86)
    for yy in range(-32, 14, 3):
        for s in (-1, 1):
            if rnd.random() < 0.55:
                lit_slot(det, s * taper_x(10.0, 29.0, 26.0, 6.0, 0.86), yy, 29.0, (0.12, 1.6, 0.7))

    # Bridge tower, directors, tripod mast.
    det.box((0, 22.0, 35.0), (16.0, 18.0, 18.0), "Paint", chamfer=1.8, taper=0.8, taper_y=0.84)
    det.box((0, 25.0, 46.4), (26.0, 11.0, 4.8), "Paint", chamfer=1.0, taper=0.95)
    lit_slot(det, 0.0, 30.5, 46.6, (21.0, 0.12, 1.3), axis="y")
    for s in (-1, 1):
        lit_slot(det, s * taper_x(13.0, 46.6, 44.0, 4.8, 0.95), 25.0, 46.6, (0.12, 8.5, 1.3))
        for zz in (33.0, 38.0):
            lit_slot(det, s * taper_x(8.0, zz, 26.0, 18.0, 0.8), 22.0, zz, (0.12, 12.0, 0.7))
    det.box((0, 23.0, 48.9), (22.0, 9.0, 0.3), "Dark")
    for yd, zd in ((26.0, 50.8), (6.0, 33.8)):
        det.box((0, yd, zd), (6.0, 5.0, 3.2), "Paint", chamfer=0.8, taper=0.8)
        det.box((0, yd - 1.0, zd + 0.6), (14.0, 1.2, 1.2), "Dark", chamfer=0.3)
        for s in (-1, 1):
            det.cyl((s * 7.0, yd - 1.0, zd + 0.6), (s * 7.6, yd - 1.0, zd + 0.6), 0.9, seg=10, slot="Dark")
    apex = Vector((0, 20.5, 74.0))
    for b in (Vector((0, 27.0, 49.0)), Vector((-4.5, 18.0, 49.0)), Vector((4.5, 18.0, 49.0))):
        det.cyl(b, apex, 0.9, 0.5, seg=8, slot="Dark")
    det.cyl(apex, apex + Vector((0, 0, 9.0)), 0.5, 0.2, seg=6, slot="Dark")
    for zz, span in ((64.0, 18.0), (70.0, 11.0)):
        det.box((0, 20.8 - (zz - 64.0) * 0.1, zz), (span, 0.5, 0.5), "Dark")
    det.box((0, 21.4, 59.0), (7.0, 1.0, 4.0), "Dark", chamfer=0.4)
    det.box((0, 20.5, 83.3), (0.6, 0.6, 0.6), "Heat")
    for x, y in ((-6.0, -2.0), (6.0, -2.0)):
        det.cyl((x, y, 32.0), (x, y, 33.2), 2.6, 2.2, seg=16, slot="Dark")
        det.cyl((x, y, 33.2), (x, y, 34.4), 2.2, 1.0, seg=16, slot="Paint")

    # Aft heat-exchange stack feeding the radiator arrays. The banks used to sit parked on
    # the roof; the manifolds and risers below run out to each comb so they are fed.
    det.box((0, -42.0, 34.0), (16.0, 26.0, 16.0), "Paint", chamfer=1.6, taper=0.78, taper_y=0.9)
    det.box((0, -42.0, 42.3), (9.0, 18.0, 0.6), "Heat")
    det.box((0, -42.0, 42.2), (11.0, 21.0, 0.4), "Dark")
    det.cyl((0, -55.0, 29.0), (0, -29.0, 29.0), 0.95, seg=12, slot="Dark")
    for s in (-1, 1):
        f.radiator(det, s * 5.2, -42.0, 38.0, height=20.0, slats=7, length=24.0,
                   tilt_deg=26.0 * s, thick=1.35)
        det.cyl((s * 8.6, -54.0, 30.0), (s * 8.6, -30.0, 30.0), 1.15, seg=12, slot="Dark")
        for yb in (-49.0, -42.0, -35.0):
            det.cyl((s * 8.6, yb, 30.0), (s * 5.6, yb, 32.5), 0.65, seg=10, slot="Dark")
    # Missile-cell deck aft, varied rather than a regular tile grid.
    for i in range(6):
        for j in range(3):
            det.box(((j - 1) * 3.2 + rnd.uniform(-0.5, 0.5), -114.0 - i * 3.0 + rnd.uniform(-0.4, 0.4),
                     DECK + 0.15),
                    (rnd.uniform(1.8, 3.0), rnd.uniform(1.8, 3.2), rnd.uniform(0.3, 0.55)),
                    "Dark", chamfer=0.15)
    det.box((0, -121.5, DECK + 0.05), (10.4, 19.6, 0.2), "Trim")

    # Secondary sponsons with casemate armour beneath.
    for z in (40.0, 2.0, -36.0):
        for s in (-1, 1):
            det.box((s * 21.2, z, 9.4), (8.5, 13.5, 3.2), "Paint", chamfer=0.9)
            det.box((s * 22.0, z, 6.2), (5.0, 11.0, 3.6), "Paint", chamfer=0.9, taper=1.3)

    for x, y_up, z in ((14, 12, 132), (16, 26, 14), (14, 12, -128)):
        for s in (-1, 1):
            f.barbette(det, s * x, y_up, z, 2.8, height=1.4, seg=16)

    for _ in range(60):
        yy = rnd.uniform(-160.0, 150.0)
        if any(abs(yy - yb) < 12 for yb in (104, 72, -66, -98)) or -52 < yy < 38:
            continue
        w, top, _ = f.interp(STATIONS_BS, yy)
        det.box((rnd.uniform(-0.6, 0.6) * w, yy, top + 0.12),
                (rnd.uniform(1.5, 4.0), rnd.uniform(1.5, 5.0), 0.3), "Dark", chamfer=0.1)

    # Keel spine and a bow sensor blister. No weapons underneath.
    det.box((0, -10.0, -18.6), (5.5, 250.0, 2.2), "Dark", chamfer=0.6)
    det.cyl((0, 128.0, -15.0), (0, 128.0, -17.2), 5.0, 3.6, seg=20, slot="Dark")

    for yy in (150.0, -150.0):
        w, _, _ = f.interp(STATIONS_BS, yy)
        for s in (-1, 1):
            det.box((s * (w + 0.2), yy, 3.0), (2.4, 5.0, 3.0), "Dark", chamfer=0.4)

    # Three mains in a row, two manoeuvring above.
    det.box((0, -175.5, -1.5), (40.0, 7.0, 23.0), "Dark", chamfer=3.0, taper=0.9)
    engines = []
    for x, z, r, y0 in ((0.0, -3.0, 6.5, -179.0), (-14.0, -3.0, 5.5, -178.5), (14.0, -3.0, 5.5, -178.5),
                        (-7.0, 7.5, 2.6, -178.5), (7.0, 7.5, 2.6, -178.5)):
        f.nozzle(det, x, z, r, y0, engines, seg=24, ribs=3)

    parts.append(det.build(bevel=0.12, smooth_angle=30))
    return f.finish(parts, "Battleship", col, engines)


# --------------------------------------------------------------------------- destroyer
# y station, half-beam, deck height, keel depth. 148 m.
STATIONS_DD = [(76.0, 0.6, 4.5, 0.0), (72.0, 2.6, 5.5, -2.0), (62.0, 5.6, 6.3, -5.0), (46.0, 9.0, 6.2, -8.0),
               (-50.0, 10.0, 6.0, -8.0), (-64.0, 9.2, 6.0, -7.0), (-72.0, 8.5, 5.8, -6.0)]


def destroyer(col):
    """The battleship's grammar at escort scale: two centreline gun mounts fore and aft,
    point defence amidships, compact citadel, redundant engines."""
    hull = k.Part("Hull", col)
    hull.loft([(y, f.heavy_section(w, t, b)) for y, w, t, b in STATIONS_DD], "Paint")
    parts = [hull.build(bevel=0.15, smooth_angle=20)]
    d = k.Part("Detail", col)

    # Armour belt: two plate courses either side of a joint strip, with the standoff varied
    # plate to plate and the gaps staggered -- the line ship's grammar at escort scale.
    rnd = random.Random(11)
    y = 52.0
    i = 0
    while y > -64.0:
        length = 11.0 + (i % 3) * 1.5
        yc = f.clamp_y(STATIONS_DD, y - length * 0.5)
        w, top, bottom = f.interp(STATIONS_DD, yc)
        h = top - bottom
        mid = (top + bottom) * 0.5
        repaired = rnd.random() < 0.2
        for course, (zc, zh, lead) in enumerate(((mid + 0.025 * h, 0.17 * h, 0.0),
                                                (mid - 0.16 * h, 0.10 * h, 0.55 * length))):
            yy = f.clamp_y(STATIONS_DD, yc - lead)
            thick = rnd.uniform(0.22, 0.5)
            for s in (-1, 1):
                d.box((s * (w + thick * 0.5 - 0.04), yy, zc + rnd.uniform(-0.12, 0.12)),
                      (thick, length - rnd.uniform(0.5, 1.0), zh),
                      "Dark" if repaired else "Paint", chamfer=0.18)
                if course == 0 and not repaired and length > 12.5:
                    for b in range(3):
                        by = yy - length * 0.28 + b * (length * 0.56 / 2.0)
                        d.cyl((s * (w + thick - 0.04), by, zc - zh * 0.5 + 0.3),
                              (s * (w + thick + 0.08), by, zc - zh * 0.5 + 0.3), 0.16, seg=6, slot="Dark")
        y -= length
        i += 1

    # Plate joints across the strip between the two courses, plus shoulder seams.
    f.panel_lines(d, STATIONS_DD, 1.0, -2.45, -1.8, count=14, y0=-62.0, y1=68.0,
                  width=0.3, proud=0.03)
    flank_seam(d, STATIONS_DD, 0.62, 68.0, -62.0, step=7.0, width=0.4, proud=-0.12)
    flank_seam(d, STATIONS_DD, 0.90, 68.0, -62.0, step=7.0, width=0.34, proud=-0.1,
               y_gap=((40.0, 9.0), (-36.0, 9.0), (8.0, 9.0)))

    # Deck-edge trim: two strakes fore and aft, not a wire rail round the whole ship.
    f.strake(d, STATIONS_DD, 0.80, -0.15, y0=44.0, y1=70.0, width=1.0, proud=0.18)
    f.strake(d, STATIONS_DD, 0.80, -0.15, y0=-42.0, y1=-68.0, width=1.0, proud=0.18)

    # Gun barbettes at the sim's hardpoints.
    for z in (40.0, -36.0):
        f.barbette(d, 0.0, 6.0, z, 4.4, height=1.3, seg=24)
    for s in (-1, 1):
        f.barbette(d, s * 7.0, 7.0, 8.0, 2.1, height=1.2, seg=14)

    # Citadel: a broad deckhouse filling the waist, a second level, bridge forward and
    # directors on top. The ship is long and the deck is otherwise bare, so the citadel
    # carries most of the silhouette between the two gun barbettes.
    d.box((0, -8.0, 8.8), (16.5, 34.0, 5.8), "Paint", chamfer=1.4, taper=0.86)
    d.box((0, -8.0, 11.8), (14.0, 32.0, 0.35), "Dark", chamfer=0.3)
    d.box((0, -10.0, 13.4), (11.5, 24.0, 3.4), "Paint", chamfer=1.0, taper=0.84)
    for yy in range(-21, 4, 3):
        for s in (-1, 1):
            if rnd.random() < 0.72:
                lit_slot(d, s * taper_x(8.25, 8.8, 5.9, 5.8, 0.86), yy, 8.8, (0.12, 1.5, 0.7))
            if rnd.random() < 0.4:
                lit_slot(d, s * taper_x(5.75, 13.4, 11.7, 3.4, 0.84), yy, 13.4, (0.12, 1.3, 0.6))
    # Bridge, raked forward over the bow.
    d.box((0, 8.0, 12.4), (9.5, 12.0, 4.2), "Paint", chamfer=1.0, taper=0.76, taper_y=0.8)
    lit_slot(d, 0.0, 14.0 - (4.1 / 4.2) * 2.16, 14.4, (7.2, 0.12, 1.0), axis="y")
    for s in (-1, 1):
        lit_slot(d, s * taper_x(4.75, 14.4, 10.3, 4.2, 0.76), 8.0, 14.4, (0.12, 7.0, 1.0))
    for yd, zd in ((6.0, 17.0), (-13.0, 15.2)):
        d.box((0, yd, zd), (3.4, 2.8, 1.8), "Paint", chamfer=0.5, taper=0.8)
        d.box((0, yd - 0.6, zd + 0.4), (7.4, 0.7, 0.7), "Dark", chamfer=0.2)
    mast = Vector((0, 8.0, 17.0))
    d.cyl(mast, mast + Vector((0, 0, 7.0)), 0.45, 0.2, seg=6, slot="Dark")
    d.box((0, 8.0, 19.6), (0.35, 0.35, 0.35), "Heat")

    # Radiator bank aft of the citadel, deeper slats in a root fairing and fed by a
    # manifold off the hull rather than parked on the deck.
    d.cyl((0, -36.0, 6.4), (0, -24.0, 6.4), 0.5, seg=10, slot="Dark")
    f.radiator(d, 0.0, -30.0, 10.9, height=8.0, slats=5, length=13.0, tilt_deg=20.0, thick=1.25)

    # Deck furniture and RCS blocks, kept clear of the citadel and the gun barbettes.
    for _ in range(26):
        yy = rnd.uniform(-66.0, 62.0)
        if any(abs(yy - yb) < 7 for yb in (40, -36)) or -28 < yy < 17:
            continue
        w, top, _ = f.interp(STATIONS_DD, yy)
        d.box((rnd.uniform(-0.5, 0.5) * w, yy, top + 0.1),
              (rnd.uniform(0.9, 2.2), rnd.uniform(0.9, 2.8), 0.22), "Dark", chamfer=0.08)
    for yy in (66.0, -66.0):
        w, _, _ = f.interp(STATIONS_DD, yy)
        for s in (-1, 1):
            d.box((s * (w + 0.1), yy, 2.0), (1.5, 3.0, 2.0), "Dark", chamfer=0.3)

    d.box((0, -10.0, -8.4), (2.6, 96.0, 1.4), "Dark", chamfer=0.4)
    d.cyl((0, 70.0, -6.0), (0, 70.0, -7.6), 2.4, 1.8, seg=14, slot="Dark")

    engines = []
    f.nozzle(d, -4.0, -1.5, 3.4, -75.0, engines)
    f.nozzle(d, 4.0, -1.5, 3.4, -75.0, engines)
    f.nozzle(d, 0.0, 3.2, 1.6, -74.8, engines)
    parts.append(d.build(bevel=0.08, smooth_angle=30))
    return f.finish(parts, "Destroyer", col, engines)


# --------------------------------------------------------------------------- transport
# y station, half-beam, deck height, keel depth. 118 m.
STATIONS_TR = [(59.0, 3.0, 6.0, -3.0), (54.0, 8.0, 7.5, -7.0), (44.0, 11.0, f.CONTRACT["transport"]["deck"], -9.0),
               (-44.0, 11.0, f.CONTRACT["transport"]["deck"], -9.0), (-54.0, 9.5, 7.5, -8.0),
               (-58.0, 9.0, 7.0, -7.5)]
DECK_TR = f.CONTRACT["transport"]["deck"]


def transport(col):
    """A long armoured box: the army lives inside the citadel. Landers racked along both
    flanks so a landing is a broadside release. Bridge forward and high. No guns."""
    hull = k.Part("Hull", col)
    hull.loft([(y, f.boxy_section(w, t, b)) for y, w, t, b in STATIONS_TR], "Paint")
    parts = [hull.build(bevel=0.2, smooth_angle=20)]
    d = k.Part("Detail", col)

    # Cargo rows on the citadel roof: varied footprints and a clear centre lane, so the
    # deck is not a checkerboard of identical grey boxes.
    cargo = random.Random(41)
    for i in range(8):
        y = 36.0 - i * 9.0
        w, top, _ = f.interp(STATIONS_TR, y)
        c = min(w, top - -9.0) * 0.18
        lane = 3.0 if i % 2 == 0 else 0.0
        for s in (-1, 1):
            if cargo.random() < 0.12:
                continue
            sx = min(cargo.uniform(3.0, 6.2), max(1.5, w - c - lane - 0.8))
            d.box((s * (lane + sx * 0.5), y + cargo.uniform(-0.8, 0.8), DECK_TR + 0.3),
                  (sx, cargo.uniform(5.5, 8.2), cargo.uniform(0.5, 0.95)), "Paint", chamfer=0.25)
            d.box((s * (lane + sx * 0.5), y + cargo.uniform(-0.8, 0.8), DECK_TR + 0.85),
                  (sx * 0.7, 0.4, 0.15), "Dark")

    # Lander cradles along both flanks.
    for s in (-1, 1):
        for i in range(5):
            y = 30.0 - i * 14.0
            d.box((s * 12.6, y, -1.0), (2.2, 12.0, 12.0), "Dark", chamfer=0.4)
            d.box((s * 15.2, y, -1.0), (3.2, 9.5, 5.0), "Paint", chamfer=0.9, taper=0.8)
            lit_slot(d, s * 16.59, y, -0.2, (0.12, 6.0, 1.0))
            d.cyl((s * 15.2, y - 5.0, -1.0), (s * 15.2, y - 5.8, -1.0), 1.2, 1.4, seg=10, slot="Engine")

    # Flank seams fore and aft of the cradle line: the box flank is otherwise blank.
    flank_seam(d, STATIONS_TR, 0.0, 52.0, -52.0, step=9.0, width=0.5, proud=-0.14,
               y_gap=tuple((30.0 - i * 14.0, 7.0) for i in range(5)))
    # Deck-edge trim: two short strakes at the ends of the cargo run instead of an 88 m
    # plank down both flanks.
    f.strake(d, STATIONS_TR, 1.0, -1.95, y0=40.0, y1=52.0, width=0.9, proud=0.16)
    f.strake(d, STATIONS_TR, 1.0, -1.95, y0=-40.0, y1=-54.0, width=0.9, proud=0.16)

    d.box((0, 40.0, DECK_TR + 4.0), (12.0, 10.0, 8.0), "Paint", chamfer=1.2, taper=0.8, taper_y=0.8)
    lit_slot(d, 0.0, 43.65, DECK_TR + 6.0, (9.0, 0.12, 1.2), axis="y")
    for s in (-1, 1):
        lit_slot(d, s * taper_x(6.0, DECK_TR + 6.0, DECK_TR, 8.0, 0.8), 40.0, DECK_TR + 6.0, (0.12, 4.0, 1.2))

    d.cyl((0, 38.0, DECK_TR + 8.0), (0, 38.0, DECK_TR + 18.0), 0.5, 0.25, seg=6, slot="Dark")
    d.box((0, 38.0, DECK_TR + 14.0), (6.0, 0.4, 0.4), "Dark")

    for z in (34.0, -30.0):
        f.barbette(d, 0.0, 9.0, z, 2.2, height=1.2, seg=14)

    # Radiator: was a thin fin. A wide, deep comb in a root housing, fed by ducts off the
    # citadel roof, so it reads as a heat exchanger rather than a plate.
    d.box((0, -43.0, DECK_TR + 1.4), (9.0, 18.0, 2.8), "Paint", chamfer=0.6)
    for s in (-1, 1):
        for yb in (-36.0, -50.0):
            d.cyl((s * 4.0, yb, DECK_TR + 0.6), (s * 4.0, yb, DECK_TR + 2.6), 0.7, seg=10, slot="Dark")
    f.radiator(d, 0.0, -43.0, DECK_TR + 5.6, height=7.4, slats=6, length=20.0,
               tilt_deg=22.0, thick=1.3)
    d.box((0, -60.0, -0.5), (20.0, 4.0, 14.0), "Dark", chamfer=1.2)
    engines = []
    for x, z, r in ((-5.0, -1.5, 3.8), (5.0, -1.5, 3.8), (0.0, 4.5, 2.0), (0.0, -6.5, 2.0)):
        f.nozzle(d, x, z, r, -62.0, engines, seg=20)

    parts.append(d.build(bevel=0.08, smooth_angle=30))
    return f.finish(parts, "Transport", col, engines)


# --------------------------------------------------------------------------- corvette
# y station, half-beam, deck height, keel depth. 47 m, one gun forward.
STATIONS_CV = [(25.0, 0.3, 1.5, -0.7), (21.0, 1.5, 2.5, -1.9), (14.0, 2.7, 3.2, -2.9),
               (2.0, 3.4, f.CONTRACT["corvette"]["deck"], -3.4), (-12.0, 3.2, 3.1, -3.1),
               (-19.0, 2.5, 2.5, -2.3), (-22.0, 2.1, 2.1, -1.8)]


def corvette(col):
    """A crew-and-engine craft: a short armoured spine with two outboard drive pods. One
    forward gun. Nothing to lose and nothing worth losing."""
    rnd = random.Random(23)
    hull = k.Part("Hull", col)
    hull.loft([(y, f.heavy_section(w, t, b)) for y, w, t, b in STATIONS_CV], "Paint")
    parts = [hull.build(bevel=0.07, smooth_angle=20)]
    d = k.Part("Detail", col)

    # Drive pods on short pylons. `nacelle` fairs the inboard end into the hull shoulder,
    # so the join reads as a designed transition instead of a cylinder poking through.
    for s in (-1, 1):
        d.box((s * 2.6, -4.0, -0.4), (1.6, 16.0, 1.4), "Paint", chamfer=0.3)
        f.nacelle(d, STATIONS_CV, s * 3.9, 3.0, -19.0, z=-0.6, r=1.75, seg=18, ribs=3,
                  slot="Paint", fairing=0.95, rib_slot="Dark")
        d.cyl((s * 3.9, -19.0, -0.6), (s * 3.9, -20.6, -0.6), 1.8, 1.5, seg=18, slot="Dark")
        for yy in (-0.5, -10.5):
            d.cyl((s * 3.9, yy, -0.6), (s * 3.9, yy - 0.24, -0.6), 1.86, seg=18, slot="Trim")

    # Armour belt: two plate courses either side of a joint strip, standoff varied.
    y = 14.0
    while y > -16.0:
        length = rnd.uniform(4.0, 6.0)
        yc = f.clamp_y(STATIONS_CV, y - length * 0.5)
        w, top, bottom = f.interp(STATIONS_CV, yc)
        h = top - bottom
        mid = (top + bottom) * 0.5
        repaired = rnd.random() < 0.25
        for course, (zc, zh, lead) in enumerate(((mid + 0.025 * h, 0.17 * h, 0.0),
                                                (mid - 0.16 * h, 0.10 * h, 0.55 * length))):
            yy = f.clamp_y(STATIONS_CV, yc - lead)
            thick = rnd.uniform(0.16, 0.34)
            for s in (-1, 1):
                d.box((s * (w + thick * 0.5 - 0.03), yy, zc + rnd.uniform(-0.06, 0.06)),
                      (thick, length - rnd.uniform(0.3, 0.7), zh),
                      "Dark" if repaired else "Paint", chamfer=0.1)
        y -= length

    # Plate joints across the strip between the courses, plus a shoulder seam.
    f.panel_lines(d, STATIONS_CV, 1.0, -0.58, -0.28, count=7, y0=-14.0, y1=15.0,
                  width=0.2, proud=0.02)
    flank_seam(d, STATIONS_CV, 0.90, 15.0, -15.0, step=5.0, width=0.22, proud=-0.07)

    # Deck-edge trim: short strakes fore and aft, not a wire rail round the whole spine.
    f.strake(d, STATIONS_CV, 0.80, -0.1, y0=9.0, y1=20.0, width=0.55, proud=0.12)
    f.strake(d, STATIONS_CV, 0.80, -0.1, y0=-9.0, y1=-20.0, width=0.55, proud=0.12)

    # Gun pad, bridge slit, sensor mast, hull lights.
    f.barbette(d, 0.0, 3.5, 8.0, 1.5, height=0.9, seg=14)
    d.box((0, 2.5, 3.9), (1.5, 2.6, 1.0), "Paint", chamfer=0.25, taper=0.7)
    lit_slot(d, 0.0, 3.8, 4.1, (1.1, 0.12, 0.4), axis="y")
    d.cyl((0, 2.5, 4.4), (0, 2.5, 8.2), 0.2, 0.1, seg=6, slot="Dark")
    d.box((0, 2.5, 6.6), (2.2, 0.2, 0.2), "Dark")
    d.box((0, 2.5, 8.3), (0.22, 0.22, 0.22), "Heat")
    for i in range(5):
        y = 8.0 - i * 5.0
        w, top, bottom = f.interp(STATIONS_CV, y)
        mid, h = (top + bottom) * 0.5, top - bottom
        # Sit the slits just above the armour belt: the old `0.82 * w` put them most of a
        # metre inside the shell, where they could never be seen.
        z = mid + 0.17 * h
        for s in (-1, 1):
            lit_slot(d, s * flank_x(STATIONS_CV, y, z), y, z, (0.1, 0.9, 0.28))

    d.box((0, -8.0, -3.7), (1.6, 22.0, 0.9), "Dark", chamfer=0.25)
    d.box((0, 18.0, -1.6), (1.4, 5.0, 1.0), "Dark", chamfer=0.25)

    engines = []
    for s in (-1, 1):
        f.nozzle(d, s * 3.9, -0.6, 1.5, -20.6, engines, seg=18)
    parts.append(d.build(bevel=0.05, smooth_angle=30))
    return f.finish(parts, "Corvette", col, engines)


# --------------------------------------------------------------------------- frigate
# y station, half-beam, deck height, keel depth. 90 m, guns fore and aft.
STATIONS_FR = [(47.0, 0.4, 2.2, -1.1), (38.0, 2.4, 3.6, -3.0), (22.0, 4.6, f.CONTRACT["frigate"]["deck"], -5.0),
               (2.0, 5.2, f.CONTRACT["frigate"]["deck"], -5.6), (-18.0, 4.6, f.CONTRACT["frigate"]["deck"], -5.0),
               (-34.0, 3.8, 3.8, -4.0), (-43.0, 3.2, 3.0, -2.8)]


def frigate(col):
    """A lean raider: long forebody, outboard drive pods, a dorsal radiator fin over the
    engine bay. Two guns, forward and aft, and no belly armour worth the mass."""
    rnd = random.Random(31)
    hull = k.Part("Hull", col)
    hull.loft([(y, f.heavy_section(w, t, b)) for y, w, t, b in STATIONS_FR], "Paint")
    parts = [hull.build(bevel=0.1, smooth_angle=20)]
    d = k.Part("Detail", col)

    # Outboard drive pods on pylons clear of the hull flank. `nacelle` fairs the inboard end
    # into the shoulder so the join is a transition, not a cylinder intersecting a shell.
    for s in (-1, 1):
        d.box((s * 4.2, -14.0, -0.8), (2.0, 24.0, 1.6), "Paint", chamfer=0.4)
        f.nacelle(d, STATIONS_FR, s * 5.4, -6.0, -39.0, z=-1.0, r=2.15, seg=20, ribs=4,
                  slot="Paint", fairing=1.15, rib_slot="Dark")
        d.cyl((s * 5.4, -39.0, -1.0), (s * 5.4, -41.2, -1.0), 2.2, 1.9, seg=20, slot="Dark")
        for yy in (-11.0, -30.0):
            d.cyl((s * 5.4, yy, -1.0), (s * 5.4, yy - 0.3, -1.0), 2.26, seg=20, slot="Trim")

    # Armour belt: two plate courses either side of a joint strip, standoff varied.
    y = 34.0
    while y > -36.0:
        length = rnd.uniform(6.0, 9.0)
        yc = f.clamp_y(STATIONS_FR, y - length * 0.5)
        w, top, bottom = f.interp(STATIONS_FR, yc)
        h = top - bottom
        mid = (top + bottom) * 0.5
        repaired = rnd.random() < 0.22
        for course, (zc, zh, lead) in enumerate(((mid + 0.025 * h, 0.17 * h, 0.0),
                                                (mid - 0.16 * h, 0.10 * h, 0.55 * length))):
            yy = f.clamp_y(STATIONS_FR, yc - lead)
            thick = rnd.uniform(0.18, 0.4)
            for s in (-1, 1):
                d.box((s * (w + thick * 0.5 - 0.03), yy, zc + rnd.uniform(-0.1, 0.1)),
                      (thick, length - rnd.uniform(0.4, 0.9), zh),
                      "Dark" if repaired else "Paint", chamfer=0.14)
        y -= length

    # Plate joints across the strip between the courses, plus a shoulder seam.
    f.panel_lines(d, STATIONS_FR, 1.0, -1.6, -1.2, count=10, y0=-34.0, y1=34.0,
                  width=0.25, proud=0.025)
    flank_seam(d, STATIONS_FR, 0.90, 34.0, -34.0, step=6.0, width=0.3, proud=-0.1,
               y_gap=((22.0, 8.0), (-18.0, 8.0)))

    # Deck-edge trim: two strakes fore and aft, not a wire rail round the whole hull.
    f.strake(d, STATIONS_FR, 0.80, -0.15, y0=26.0, y1=40.0, width=0.7, proud=0.15)
    f.strake(d, STATIONS_FR, 0.80, -0.15, y0=-24.0, y1=-40.0, width=0.7, proud=0.15)

    for z in (22.0, -18.0):
        f.barbette(d, 0.0, 4.5, z, 2.0, height=1.1, seg=16)

    # Low bridge forward of the main gun, directors between the two mounts.
    d.box((0, 12.0, 6.4), (4.4, 8.0, 3.6), "Paint", chamfer=0.7, taper=0.78, taper_y=0.8)
    lit_slot(d, 0.0, 16.0 - (1.9 / 3.6) * 0.8, 6.5, (3.2, 0.12, 0.7), axis="y")
    for s in (-1, 1):
        lit_slot(d, s * taper_x(2.2, 6.5, 4.6, 3.6, 0.78), 12.0, 6.5, (0.12, 4.5, 0.7))
    for yd in (26.0, -14.0):
        d.box((0, yd, 6.4), (2.6, 2.2, 1.5), "Paint", chamfer=0.4, taper=0.8)
        d.box((0, yd - 0.5, 6.6), (5.6, 0.6, 0.6), "Dark", chamfer=0.15)
    d.cyl((0, 12.0, 8.0), (0, 12.0, 13.5), 0.32, 0.14, seg=6, slot="Dark")
    d.box((0, 12.0, 11.0), (4.0, 0.2, 0.2), "Dark")
    d.box((0, 12.0, 13.6), (0.18, 0.18, 0.18), "Heat")

    # Dorsal radiator fin over the engine bay.
    f.radiator(d, 0.0, -30.0, 6.2, height=7.4, slats=4, length=15.0, tilt_deg=20.0)

    for i in range(7):
        y = 26.0 - i * 6.0
        w, top, bottom = f.interp(STATIONS_FR, y)
        mid, h = (top + bottom) * 0.5, top - bottom
        # The old slits sat at a fixed `0.82 * w`, which is inside the shell on this
        # section: they were buried and never appeared in a render.
        z = mid + 0.17 * h
        for s in (-1, 1):
            lit_slot(d, s * flank_x(STATIONS_FR, y, z), y, z, (0.1, 1.1, 0.34))

    d.box((0, -8.0, -5.9), (2.2, 62.0, 1.1), "Dark", chamfer=0.3)
    d.cyl((0, 42.0, -3.4), (0, 42.0, -4.8), 1.8, 1.3, seg=14, slot="Dark")
    for yy in (42.0, -38.0):
        w, _, _ = f.interp(STATIONS_FR, yy)
        for s in (-1, 1):
            d.box((s * (w + 0.1), yy, 1.6), (1.2, 2.6, 1.6), "Dark", chamfer=0.25)

    engines = []
    for s in (-1, 1):
        f.nozzle(d, s * 5.4, -1.0, 2.0, -41.2, engines, seg=20)
    f.nozzle(d, 0.0, -1.4, 2.4, -43.0, engines, seg=24)
    parts.append(d.build(bevel=0.07, smooth_angle=30))
    return f.finish(parts, "Frigate", col, engines)


BUILDERS = {
    "battleship": battleship,
    "destroyer": destroyer,
    "transport": transport,
    "corvette": corvette,
    "frigate": frigate,
}
