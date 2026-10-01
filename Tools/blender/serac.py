"""Serac Combine hulls: mineral tan, basalt and hazard red, quarried not built.

The Serac are a mining combine that bought a war fleet second-hand, so nothing here
is aerodynamic. Grammar across the line: a flat roof carried between hard chamfered
shoulders, slab sides stacked in visible plate courses, contrasting strake along
every chamfer, and the propulsion hung *outboard* on short pylons -- paired nacelle
pods that read as cargo haulers bolted to a barge. Decks are wide and flat because
that is where the combine bolts its mining plant.

The line ship goes further and splits into three lobes: a central hull with a
nacelle pod on each beam, tied together by transverse sponsons, leaving open slots
down the flanks that you can see straight through from above.

Five classes against fleet.CONTRACT: the deck plane and mount stations come from the
sim, so the faction reads through outline, superstructure and surface, not layout.
"""
import math
import random

from mathutils import Vector

import fleet as f
import grkit as k

# Proportions of fleet.faceted_section, restated here because the surface treatment
# bolts to the same roof edge, slab side and keel line the hull is lofted from.
ROOF = 0.86  # flat roof half-width, as a fraction of the beam
CHAMFER = 0.08  # half the section's shoulder chamfer
SIDE_LO = 0.35  # where the slab side turns into the keel chamfer
# How far a seam line stands proud of the plating. `f.panel_lines` centres its box on
# the surface, so a negative `proud` buries the whole seam inside the hull where
# nothing can see it; a few centimetres proud reads as a panel gap and self-shadows.
SEAM = 0.03
SUPERFIRE = f.SUPERFIRE
DECK_BS = f.CONTRACT["battleship"]["deck"]
DECK_DD = f.CONTRACT["destroyer"]["deck"]
DECK_TR = f.CONTRACT["transport"]["deck"]
DECK_FR = f.CONTRACT["frigate"]["deck"]
DECK_CV = f.CONTRACT["corvette"]["deck"]


# --------------------------------------------------------------------------- grammar
def side_span(w, top, bottom):
    """The vertical slab side of a `faceted_section`: (z_low, z_high)."""
    return SIDE_LO * bottom, top - CHAMFER * 2 * (top - bottom)


def slab(part, stations, x0=0.0, slot="Paint", **kw):
    """Lofts a station list into faceted hull, offset to the side by `x0`."""
    return part.loft([(y, [(x0 + x, z) for x, z in f.faceted_section(w, t, b)])
                      for y, w, t, b in stations], slot, **kw)


def strake(part, stations, x0=0.0, s=1, slot="Trim", width=0.9, proud=0.25,
           y0=None, y1=None):
    """A trim band let into the shoulder chamfer, lofted through the hull's own sheer.

    The one piece of trim in the fleet that sat on the hull rather than near it, so the
    approach is kept: the band is built from `stations`, which is why it cannot sail
    off the bow the way a separately-run rail does. What changes is the section -- a
    ribbon with real width and a proud outer face instead of a chain of 0.4 m square
    cylinders that read as a wire.

    The sheer is resampled at a fixed pitch first. Lofted straight through the hull's
    own stations, which are 20-50 m apart on the big hulls, the band beads at every one
    of them; stepped at 7 m it is a smooth ribbon.
    """
    lo = stations[-1][0] if y0 is None else max(stations[-1][0], y0)
    hi = stations[0][0] if y1 is None else min(stations[0][0], y1)
    if hi <= lo:
        return
    n = max(2, int((hi - lo) / 7.0))
    rings = []
    for i in range(n + 1):
        y = lo + (hi - lo) * i / n
        w, top, bottom = f.interp(stations, f.clamp_y(stations, y))
        h = top - bottom
        # The shoulder chamfer, from `fleet.faceted_section`: roof edge down to the
        # slab side, and the normal that points outboard from it.
        ax, az = ROOF * w, top
        dx, dz = w - ROOF * w, -(1.0 - 2.0 * CHAMFER) * h
        span = math.hypot(dx, dz)
        nx, nz = -dz, dx  # rotate the chamfer direction 90 degrees, outboard
        half = min(width, span * 0.92) * 0.5
        t0 = max(0.0, 0.5 - half / span)
        t1 = min(1.0, 0.5 + half / span)
        # Die the band away at the bow and stern instead of stopping on a hard edge.
        o = proud * (0.55 if i in (0, n) else 1.0)
        p0 = (ax + dx * t0, az + dz * t0)
        p1 = (ax + dx * t1, az + dz * t1)
        quad = [(p0[0], p0[1]), (p1[0], p1[1]),
                (p1[0] + nx * o / span, p1[1] + nz * o / span),
                (p0[0] + nx * o / span, p0[1] + nz * o / span)]
        rings.append((y, [(x0 + s * x, z) for x, z in quad]))
    part.loft(rings, slot)


def courses(part, stations, x0, y_from, y_to, rows, rnd, length=(9.0, 15.0),
            proud=0.35, dark_p=0.26, band=0.46, bolts=0):
    """Stacked plate courses down the top of a slab side, some of them replaced in
    `Dark` where the hull has been cut back and re-laid over.

    Two things keep this from reading as tile. The courses only occupy the top
    `band` of the side, so the flank is a proud plate row over a recessed band rather
    than one unbroken wall of identical rectangles; and each plate stands off by its
    own amount, with the odd bolt row on the long ones, so the rhythm is staggered.
    """
    y = y_from
    while y > y_to:
        ly = rnd.uniform(*length)
        yc = f.clamp_y(stations, y - ly * 0.5)
        w, top, bottom = f.interp(stations, yc)
        zlo, zhi = side_span(w, top, bottom)
        zlo += (zhi - zlo) * 0.06
        span = zhi - zlo
        # Courses occupy the top of the side, the part that reads from a three-quarter
        # view; the flank under them is left as a recessed band for the seam lines.
        zb = zhi - span * band
        row = (zhi - zb) / rows
        p = proud * rnd.uniform(0.62, 1.3)
        slot = "Dark" if rnd.random() < dark_p else "Paint"
        for r in range(rows):
            z = zb + row * (r + 0.5)
            for s in (-1, 1):
                x = x0 + s * (w + p * 0.5)
                part.box((x, yc, z), (p, ly - 0.9, row * 0.82), slot, chamfer=p * 0.35)
                # A bolt row along the top course only: on every course it turns the
                # flank into speckle at review distance instead of a plate run.
                if bolts and r == rows - 1:
                    for b in range(bolts):
                        by = yc + (b - (bolts - 1) * 0.5) * (ly - 2.4) / max(1, bolts)
                        part.box((x + s * p * 0.5, by, z + row * 0.24),
                                 (0.4, 0.9, 0.9), "Dark")
        y -= ly


def buttress(part, stations, x0, s, y_from, y_to, pitch, rnd, proud=1.1, dark_p=0.3):
    """Vertical stiffener ribs standing proud of the plate courses: the hull is
    bolted together, not cast, and the ribs say so in silhouette."""
    y = y_from
    while y >= y_to:
        w, top, bottom = f.interp(stations, f.clamp_y(stations, y))
        zlo, zhi = side_span(w, top, bottom)
        zlo += (zhi - zlo) * 0.05
        p = proud * rnd.uniform(0.7, 1.2)
        part.box((x0 + s * (w + p * 0.5), y, (zlo + zhi) * 0.5),
                 (p, 1.5 + (zhi - zlo) * 0.08, (zhi - zlo) * 0.8),
                 "Dark" if rnd.random() < dark_p else "Paint", chamfer=0.3)
        y -= pitch


def handrail(part, stations, x0, s, y_from, y_to, height=1.1, post=6.0, r=0.15,
             slot="Trim"):
    """Posts and a top rail along a deck edge: the combine's crews walk its roofs.

    Kept as the one hand-holding piece of trim on these hulls, so the runs are short
    and stand off far enough to read as a rail rather than a painted line."""
    y = y_from
    prev = None
    while y >= y_to:
        w, top, _ = f.interp(stations, f.clamp_y(stations, y))
        x = x0 + s * (ROOF * w - 0.8)
        p = Vector((x, y, top + height))
        part.cyl((x, y, top), p, r, seg=6, slot=slot)
        if prev is not None and (p - prev).length > 0.05:
            part.cyl(prev, p, r * 0.75, seg=4, slot=slot)
        prev = p
        y -= post


def walkway(part, stations, x, y_from, y_to, width_frac=0.6, thick=0.5, pitch=6.0,
            slot="Paint"):
    """Broken walkway plating down a pod roof. Kept in pieces with gaps and the odd
    replaced plate, because one long contrasting panel reads as a flat billboard
    from every angle -- the exact thing the nacelle roofs must not look like. Painted
    rather than trimmed: a yellow slab this size is a plank dropped on the roof."""
    y = y_from
    i = 0
    while y > y_to:
        yy = f.clamp_y(stations, y - pitch * 0.45)
        w, top, _ = f.interp(stations, yy)
        part.box((x, yy, top + thick * 0.5),
                 (ROOF * w * width_frac, pitch * 0.7, thick),
                 "Dark" if i % 4 == 3 else slot, chamfer=0.12)
        y -= pitch
        i += 1


def pod(part, x, stations, bells, r, engines, y0, zs, hull_stations=None, slot="Paint"):
    """An outboard nacelle pod: faceted casing, own bells, and a faired collar on the
    inboard face.

    The pods used to intersect the hull with nothing between them, and the join reads
    as a modelling error even when the seam is buried. The collar is a run of tapered
    plates that follow the parent hull's sheer, so the pod looks bolted on.

    Only the outboard shoulder is straked, and only over the after part of the pod. A
    band down both shoulders of every pod for its whole length outlines the ship four
    times over, which is exactly what the trim cut is meant to stop.
    """
    slab(part, stations, x, slot)
    lo, hi = stations[0][0], stations[-1][0]
    # The outboard side only: the inboard one faces into the slot and is barely seen.
    strake(part, stations, x, f.k_sign(x), width=max(0.4, r * 0.55), proud=0.15,
           y0=hi - (hi - lo) * 0.4, y1=lo + (hi - lo) * 0.12)
    for z in zs:
        f.nozzle(part, x, z, r, y0, engines, seg=20, ribs=2, trim=False)
    if hull_stations is not None:
        collar(part, x, stations, hull_stations, slot)


def seams(part, stations, x0, y_from, y_to, count, z_lo, z_hi, width=0.4, slot="Dark",
          skip=()):
    """Recessed seam lines across a slab flank on an offset centreline.

    `f.panel_lines` does this for a hull on the centreline. The pods sit out at `x0`
    and it carries no offset, so the same geometry is rebuilt here for them."""
    step = (y_from - y_to) / count
    for i in range(count):
        y = y_from - step * (i + 0.5)
        if any(abs(y - sk) < 6.0 for sk in skip):
            continue
        w, _top, _bottom = f.interp(stations, f.clamp_y(stations, y))
        for s in (-1, 1):
            part.box((x0 + s * (w + SEAM), y, (z_lo + z_hi) * 0.5),
                     (0.09, width, z_hi - z_lo), slot)


def collar(part, pod_x, stations, hull_stations, slot="Paint"):
    """A faired transition from the hull shoulder out to an outboard pod.

    Sized to fair the joint, not to fill the slot: a collar as deep and tall as the
    pod itself closes the gap the lobes are supposed to read through."""
    s = f.k_sign(pod_x)
    lo, hi = stations[0][0], stations[-1][0]
    n = max(2, int((hi - lo) / 26.0))
    for i in range(n):
        t = (i + 0.5) / n
        y = hi - (hi - lo) * t
        yc = f.clamp_y(stations, y)
        w, top, bottom = f.interp(stations, yc)
        hull_w = f.interp(hull_stations, f.clamp_y(hull_stations, yc))[0]
        out = pod_x - s * w
        inn = s * (hull_w + 0.5)
        z = bottom + (top - bottom) * 0.34
        # Tall where it meets the pod, shallow at the hull: the taper is the fairing.
        part.box(((out + inn) * 0.5, yc, z), (abs(out - inn), w * 1.15, (top - bottom) * 0.62),
                 slot, chamfer=min(0.9, w * 0.5), taper=0.6, taper_y=0.6)


def pylon(part, x0, x1, y, z, h, depth, slot="Paint"):
    """A short pylon tying an outboard pod to the hull it is bolted to: a deep web at
    the pod tapering back into a shallow root on the hull, with a keel under it."""
    mid = (x0 + x1) * 0.5
    part.box((mid, y, z), (abs(x1 - x0), depth, h), slot, chamfer=min(0.6, h * 0.3))
    part.box((mid, y, z + h * 0.5), (abs(x1 - x0) * 0.72, depth * 0.7, h * 0.34),
             slot, chamfer=0.3, taper=0.7, taper_y=0.8)
    part.box((mid, y, z - h * 0.5), (abs(x1 - x0) * 0.6, depth * 0.8, h * 0.26),
             "Dark", chamfer=0.2)


def ring(part, x, y, z, r, count=10, seg=0.42, h=0.3, slot="Trim"):
    """A segmented trim ring around a barbette.

    A solid `Trim` cylinder of the barbette's own radius is mostly its two flat end
    caps: 500 m^2 of flat colour lying on the deck per mount, which is the plate-on-a-
    plate problem all over again. This is the same ring, marked in segments."""
    for i in range(count):
        a = 2.0 * math.pi * i / count
        part.box((x + r * math.cos(a), y + r * math.sin(a), z),
                 (r * 2.0 * math.pi / count * seg, r * 0.5, h), slot, chamfer=0.04)


def feed(part, x, y, z, height, r=0.26, dy=None, slot="Dark"):
    """Plumbing from the heat exchanger up into a radiator bank, so the comb is fed
    rather than sitting on the hull with nothing behind it."""
    dy = height * 0.36 if dy is None else dy
    for d in (-1, 1):
        part.cyl((x, y + d * dy, z - height * 0.5), (x, y + d * dy, z + height * 0.42),
                 r, r * 0.8, seg=8, slot=slot)
    part.box((x, y, z - height * 0.5), (r * 5.0, r * 5.0, r * 3.0), slot, chamfer=0.1)


def bank(part, x, y, z, height, slats, length, tilt_deg=28.0):
    """A flank radiator: a dark louvred comb in a plenum.

    `fleet.radiator` lays a full-length emissive strip on the top of every slat, and
    four or five of those in a row read as neon tubing run along the flank rather than
    as a heat exchanger. The flank banks are dark slats in a dark plenum, read by the
    depth between them; the short banks over the engine bays keep the glow, where it
    is a highlight on a hot compartment rather than a stripe down the ship."""
    f.radiator(part, x, y, z, height=height, slats=slats, length=length,
               tilt_deg=tilt_deg, face="Dark")


def windows(part, stations, x0, y_from, y_to, z, span=1.6, h=0.7, pitch=3.2, s=1,
            frame=0.12):
    """A run of slit windows along a slab side, at whatever height the hull allows.

    The slit is set *into* a `Dark` surround that stands proud of the plating, so it
    reads as a lit slot in a recess rather than a white rectangle stuck on the hull."""
    y = y_from
    while y > y_to:
        yy = f.clamp_y(stations, y)
        w, _top, _bottom = f.interp(stations, yy)
        part.box((x0 + s * (w + frame * 0.5), yy, z), (frame, span + 0.6, h + 0.5),
                 "Dark", chamfer=0.04)
        part.box((x0 + s * (w + frame * 0.3), yy, z), (frame * 0.6, span, h),
                 "Windows", chamfer=0.02)
        y -= pitch


def hatches(part, stations, y_from, y_to, n, rnd, wfrac=0.8):
    """Deck furniture: coamings, hatches and stacked mine crates on the flat roof."""
    for _ in range(n):
        y = rnd.uniform(y_to, y_from)
        w, top, _ = f.interp(stations, y)
        room = ROOF * w * wfrac
        x = rnd.uniform(-room, room)
        sx = min(rnd.uniform(2.0, 6.0), 2.0 * max(room - abs(x), 0.5))
        sz = rnd.uniform(0.3, 1.1)
        if rnd.random() < 0.35:
            sz += 1.2
        part.box((x, y, top + sz * 0.5), (sx, rnd.uniform(2.0, 5.5), sz),
                 "Dark" if rnd.random() < 0.4 else "Paint", chamfer=0.12)


# --------------------------------------------------------------------------- battleship
# y station, half-beam, deck height, keel depth. 354 m bow to stern, centre hull.
STATIONS_BS = [
    (182.0, 5.0, 9.0, 0.0),
    (172.0, 13.0, 11.6, -7.0),
    (158.0, 18.0, 12.0, -13.0),
    (126.0, 21.0, DECK_BS, -18.0),
    (86.0, 17.0, DECK_BS, -19.0),
    (30.0, 14.5, DECK_BS, -20.0),
    (-50.0, 14.5, DECK_BS, -20.0),
    (-100.0, 17.0, DECK_BS, -19.0),
    (-140.0, 20.0, 12.2, -16.0),
    (-164.0, 19.0, 11.6, -11.0),
    (-172.0, 16.0, 10.6, -7.0),
]
# Outboard nacelle pods, half-beam relative to the pod centreline at x = +-POD_X.
POD_X = 37.0
STATIONS_POD = [
    (134.0, 2.0, 9.0, 4.0),
    (124.0, 6.0, 10.6, -1.0),
    (106.0, 8.4, 11.6, -5.0),
    (56.0, 9.0, 12.0, -7.0),
    (-56.0, 9.0, 12.0, -7.0),
    (-116.0, 8.4, 11.6, -5.6),
    (-146.0, 6.4, 10.2, -3.0),
]


def battleship(col):
    """Three lobes: a central hull with a nacelle pod outboard on each beam, lashed
    together by transverse sponsons so the flanks stay open. A and Y on the main deck,
    B and X in raised superfiring houses; six beam secondaries in the sponson tops,
    six point-defence tubs, and a command island that bridges the two slots."""
    rnd = random.Random(3)
    hull = k.Part("Hull", col)
    slab(hull, STATIONS_BS)
    parts = [hull.build(bevel=0.3, smooth_angle=18)]
    d = k.Part("Detail", col)

    engines = []

    # Outboard nacelle pods, on short pylons, each with a pair of bells. The lobes
    # only separate in silhouette if the slot between them is genuinely deep, so the
    # pylons are slung low under a dark trough rather than filling the gap.
    for s in (-1, 1):
        pod(d, s * POD_X, STATIONS_POD, None, 3.4, engines, -148.0, (5.0, -2.0),
            hull_stations=STATIONS_BS)
        for y in (118.0, 40.0, 2.0, -36.0, -120.0):
            pylon(d, s * 12.0, s * (POD_X - 6.0), y, 2.0, 6.0, 9.0)
        # Slot floor: a dark keel bridging hull to pod, so the gap reads as a
        # shadowed channel with the pods lifted above it, not a hole in a slab.
        d.box((s * 22.0, -6.0, -3.6), (16.0, 214.0, 4.4), "Dark", chamfer=0.7, taper=0.9)
        # Strake and plating courses down the pod flank, seams under them.
        courses(d, STATIONS_POD, s * POD_X, 104.0, -120.0, 2, rnd, length=(12.0, 20.0),
                proud=0.34, band=0.5, bolts=2)
        seams(d, STATIONS_POD, s * POD_X, 104.0, -118.0, 8, -1.2, 2.4, width=0.6)
        windows(d, STATIONS_POD, s * POD_X, 96.0, -110.0, 8.4, span=2.0, h=0.8, pitch=4.4)
        # Hazard marks on the pod bow, where the paint has gone.
        d.box((s * (POD_X + 9.1), 96.0, 6.0), (0.3, 5.0, 2.2), "Heat", chamfer=0.1)
        d.box((s * (POD_X + 9.1), 88.0, 6.0), (0.3, 2.0, 2.2), "Heat", chamfer=0.1)
        # Pod cap gantry and container rack on the pod roof.
        walkway(d, STATIONS_POD, s * POD_X, 58.0, -100.0, width_frac=0.7, thick=0.7,
                pitch=9.0, slot="Paint")
        for i in range(4):
            d.box((s * POD_X, 22.0 - i * 12.0, 14.0), (8.0, 9.0, 2.8),
                  "Dark" if i % 2 else "Paint", chamfer=0.5)
        feed(d, s * (POD_X + 8.4), -74.0, 3.0, 12.0, r=0.34)
        bank(d, s * (POD_X + 8.4), -74.0, 3.0, height=12.0, slats=5,
             length=26.0, tilt_deg=30.0)

    # Central hull: strake, plate courses over a seamed band, slit windows. This is
    # a 250 m flank, so the seams are what stop it reading as one moulded wall.
    for s in (-1, 1):
        strake(d, STATIONS_BS, 0.0, s, width=2.4, proud=0.5)
    courses(d, STATIONS_BS, 0.0, 160.0, -160.0, 3, rnd, length=(16.0, 24.0), proud=0.7,
            band=0.46, bolts=3)
    f.panel_lines(d, STATIONS_BS, 1.0, -5.6, 0.6, 14, y0=-100.0, y1=126.0, width=0.9,
                  proud=SEAM, skip=(40.0, 2.0, -36.0))
    for s in (-1, 1):
        windows(d, STATIONS_BS, 0.0, 150.0, -152.0, 8.2, span=2.2, h=0.9, pitch=4.6, s=s)
        buttress(d, STATIONS_BS, 0.0, s, 150.0, -152.0, 13.0, rnd, proud=1.3, dark_p=0.3)

    # Superfiring houses. A and Y stand on the main deck; B and X on raised decks.
    for yc in (64.0, -60.0):
        d.box((0.0, yc, 16.0), (22.0, 48.0, 8.0), "Paint", chamfer=1.6, taper=0.94)
        # One short bar on the roof, not a field of them: at this size a trim plate
        # is 100 m^2 of colour per copy and the roof is not the thing to accent.
        d.box((0.0, yc - 15.0, 20.05), (13.0, 2.4, 0.4), "Trim", chamfer=0.1)
        for s in (-1, 1):
            d.box((s * 11.1, yc, 17.5), (0.3, 40.0, 1.4), "Windows", chamfer=0.05)
        for i in range(3):
            d.box((0.0, yc - 16.0 + i * 16.0, 21.2), (9.0, 7.0, 1.4), "Dark", chamfer=0.3)
    for yb in (104.0, -98.0):
        f.barbette(d, 0.0, DECK_BS, yb, 8.9, height=1.6, seg=32)
        ring(d, 0.0, yb, 12.4, 9.1, count=12)
    for yb in (72.0, -66.0):
        f.barbette(d, 0.0, SUPERFIRE, yb, 9.6, height=1.5, seg=32, slot="Paint")
        ring(d, 0.0, yb, SUPERFIRE + 0.4, 9.7, count=12)

    # Beam secondaries in sponson tops, on the sponson that also carries the pylon.
    for z in (40.0, 2.0, -36.0):
        for s in (-1, 1):
            d.box((s * 20.5, z, 8.4), (17.0, 16.0, 5.2), "Paint", chamfer=1.0)
            # Sponson deck marked out in short bars and an outboard edge, not one
            # large plate: a 15 m square of flat colour reads as a billboard.
            for n in range(4):
                d.box((s * (14.0 + n * 4.0), z, 11.05), (2.6, 9.0, 0.3),
                      "Trim" if n == 0 else "Dark")
            d.box((s * 27.4, z, 8.4), (0.4, 15.0, 4.4), "Dark", chamfer=0.1)
            f.barbette(d, s * 19.0, 11.0, z, 2.7, height=1.3, seg=16)

    # Crossing beam, its braces, and the command island that bridges the two slots.
    d.box((0.0, 16.0, 15.5), (60.0, 16.0, 7.0), "Paint", chamfer=1.0)
    for yy in (11.0, 21.0):
        d.box((0.0, yy, 19.1), (40.0, 2.0, 0.3), "Trim")
    for s in (-1, 1):
        # Diagonal braces: the beam is a bridge between the pods, and it shows it.
        for yy, xa, xb in ((9.0, 11.0, 26.0), (23.0, 11.0, 26.0)):
            d.cyl((s * xa, yy, 7.0), (s * xb, yy, 12.2), 1.0, 0.8, seg=8, slot="Dark")
        d.box((s * 22.0, 16.0, 8.0), (20.0, 3.0, 2.0), "Paint", chamfer=0.4)
    d.box((0.0, 16.0, 22.5), (38.0, 34.0, 7.0), "Paint", chamfer=1.4, taper=0.9, taper_y=0.9)
    d.box((0.0, 33.2, 23.2), (30.0, 0.3, 1.4), "Windows")
    for s in (-1, 1):
        d.box((s * 19.1, 16.0, 23.2), (0.3, 26.0, 1.4), "Windows")
        for yy in range(4, 30, 4):
            if rnd.random() < 0.65:
                d.box((s * 17.6, yy, 20.0), (0.2, 1.8, 0.8), "Windows")

    # Bridge cap, funnel block and masts, all on the island.
    d.box((0.0, 30.0, 28.4), (30.0, 9.0, 4.8), "Paint", chamfer=1.0, taper=0.86)
    d.box((0.0, 34.6, 28.6), (24.0, 0.3, 1.3), "Windows")
    for s in (-1, 1):
        d.box((s * 14.4, 30.0, 28.6), (0.3, 6.0, 1.3), "Windows")
    d.box((0.0, 28.0, 30.9), (24.0, 8.0, 0.4), "Dark")
    d.box((0.0, -18.0, 20.0), (24.0, 18.0, 9.0), "Paint", chamfer=1.4, taper=0.82)
    d.box((0.0, -18.0, 24.6), (17.0, 13.0, 0.6), "Heat")
    d.box((0.0, -18.0, 24.7), (19.0, 15.0, 0.4), "Dark")
    for yd, zd in ((26.0, 32.0), (-4.0, 26.2)):
        d.box((0.0, yd, zd), (7.0, 5.0, 3.4), "Paint", chamfer=0.9, taper=0.8)
        d.box((0.0, yd - 1.0, zd + 0.7), (15.0, 1.2, 1.2), "Dark", chamfer=0.3)
        for s in (-1, 1):
            d.cyl((s * 8.0, yd - 1.0, zd + 0.7), (s * 8.7, yd - 1.0, zd + 0.7), 0.95,
                  seg=10, slot="Dark")
    apex = Vector((0.0, 26.0, 58.0))
    for b in (Vector((0.0, 31.0, 31.0)), Vector((-5.0, 23.0, 31.0)), Vector((5.0, 23.0, 31.0))):
        d.cyl(b, apex, 0.9, 0.5, seg=8, slot="Dark")
    d.cyl(apex, apex + Vector((0, 0, 9.0)), 0.5, 0.2, seg=6, slot="Dark")
    for zz, span in ((48.0, 18.0), (54.0, 11.0)):
        d.box((0.0, 26.3 - (zz - 48.0) * 0.12, zz), (span, 0.5, 0.5), "Dark")
    d.box((0.0, 30.0, 67.4), (0.6, 0.6, 0.6), "Heat")

    # Point defence: two on the foredeck, two aft, a pair up on the island.
    for x, y_up, z in ((14.0, DECK_BS, 132.0), (14.0, DECK_BS, -128.0)):
        for s in (-1, 1):
            d.box((s * 12.0, z, DECK_BS + 0.6), (7.0, 8.0, 1.2), "Paint", chamfer=0.4)
            f.barbette(d, s * x, y_up, z, 2.9, height=1.5, seg=16)
    for s in (-1, 1):
        f.barbette(d, s * 16.0, 26.0, 14.0, 3.0, height=1.5, seg=16, slot="Paint")
        d.box((s * 16.0, 14.0, 25.4), (7.0, 8.0, 1.2), "Paint", chamfer=0.4)

    # Mining plant bolted to the flat roof: gantries, silos, crate stacks.
    hatches(d, STATIONS_BS, 162.0, -160.0, 170, rnd, wfrac=0.66)
    for s in (-1, 1):
        # Silo banks, gantry rails and a bow dock, so the flat roof is working
        # deck rather than an empty apron.
        for i in range(3):
            yy = 120.0 - i * 13.0
            for j in range(3):
                d.cyl((s * (4.0 + j * 4.4), yy, DECK_BS), (s * (4.0 + j * 4.4), yy, DECK_BS + 7.0),
                      1.9, 1.7, seg=12, slot="Paint" if (i + j) % 2 else "Dark")
            d.box((s * 8.8, yy, DECK_BS + 7.4), (9.0, 1.8, 0.4), "Trim", chamfer=0.1)
        for i in range(2):
            yy = -100.0 - i * 15.0
            d.box((s * 7.0, yy, DECK_BS + 2.4), (11.0, 12.0, 4.8), "Paint", chamfer=0.8)
            d.box((s * 7.0, yy, DECK_BS + 5.0), (6.0, 6.0, 0.4), "Trim", chamfer=0.1)
        d.box((s * 5.0, 168.0, 8.6), (9.0, 14.0, 4.4), "Paint", chamfer=0.7, taper=0.7)
        d.box((s * 5.0, 168.0, 10.9), (6.0, 11.0, 0.4), "Heat")
        d.box((s * 6.0, 150.0, 14.6), (7.0, 34.0, 5.2), "Paint", chamfer=0.9)
        d.box((s * 6.0, 150.0, 17.4), (5.0, 30.0, 0.5), "Dark")
        for yy in range(140, 162, 8):
            d.cyl((s * 6.0, yy, 17.6), (s * 6.0, yy, 19.2), 1.5, 1.3, seg=12, slot="Dark")
        feed(d, s * 16.0, -104.0, 15.0, 9.0, r=0.3)
        bank(d, s * 16.0, -104.0, 15.0, height=9.0, slats=4, length=18.0, tilt_deg=-26.0)
        for yy in (168.0, -164.0):
            w, _, _ = f.interp(STATIONS_BS, yy)
            d.box((s * (w * 0.78), yy, 9.4), (4.0, 6.0, 4.0), "Dark", chamfer=0.5)

    # Bow shield, keel spine and the stern engine block.
    d.box((0.0, 176.0, 6.0), (12.0, 6.0, 8.0), "Dark", chamfer=1.2)
    d.box((0.0, -10.0, -20.6), (6.0, 260.0, 2.2), "Dark", chamfer=0.6)
    d.cyl((0.0, 128.0, -16.0), (0.0, 128.0, -18.2), 5.2, 3.6, seg=20, slot="Dark")
    d.box((0.0, -176.0, -2.0), (34.0, 8.0, 22.0), "Dark", chamfer=2.6, taper=0.92)
    d.box((0.0, -180.4, -2.0), (30.0, 0.6, 16.0), "Dark")
    d.box((0.0, -180.6, 4.4), (22.0, 0.4, 1.6), "Trim")
    # Bells graded rather than repeated: five identical circles read as a mistake.
    for x, z, r, y0 in ((0.0, -4.0, 6.4, -180.0), (-10.5, -4.0, 4.6, -179.5),
                        (10.5, -4.0, 4.6, -179.5), (-5.2, 6.5, 3.0, -179.5),
                        (5.2, 6.5, 3.0, -179.5)):
        f.nozzle(d, x, z, r, y0, engines, seg=24, ribs=3, trim=False)

    parts.append(d.build(smooth_angle=26))
    return f.finish(parts, "Battleship", col, engines)


# --------------------------------------------------------------------------- destroyer
# y station, half-beam, deck height, keel depth. 148 m.
STATIONS_DD = [
    (76.0, 2.5, 5.0, 0.0),
    (70.0, 5.5, 6.0, -2.5),
    (58.0, 9.0, 6.0, -5.0),
    (40.0, 11.0, DECK_DD, -6.5),
    (8.0, 12.0, DECK_DD, -7.0),
    (-36.0, 12.0, DECK_DD, -7.0),
    (-56.0, 11.0, 6.0, -6.0),
    (-66.0, 9.5, 5.6, -4.5),
    (-72.0, 8.0, 5.0, -2.5),
]
POD_DD = 16.5
STATIONS_DD_POD = [
    (34.0, 1.6, 4.4, 0.4),
    (28.0, 2.8, 4.8, -1.6),
    (10.0, 3.2, 5.0, -2.4),
    (-52.0, 3.2, 5.0, -2.4),
    (-64.0, 2.6, 4.6, -1.4),
    (-69.0, 1.6, 4.2, 0.0),
]


def destroyer(col):
    """A wide flat-roofed barge with a full-width working deck: two centreline guns
    fore and aft, point defence in the waist, and a pair of fat outboard nacelles on
    short pylons. Radiator slats are raked out over the shoulders."""
    rnd = random.Random(13)
    hull = k.Part("Hull", col)
    slab(hull, STATIONS_DD)
    parts = [hull.build(bevel=0.18, smooth_angle=18)]
    d = k.Part("Detail", col)
    engines = []

    for s in (-1, 1):
        pod(d, s * POD_DD, STATIONS_DD_POD, None, 2.5, engines, -69.0, (2.6, -1.4),
            hull_stations=STATIONS_DD)
        for y in (26.0, -12.0, -50.0):
            pylon(d, s * 10.0, s * (POD_DD - 2.0), y, 2.2, 5.0, 6.0)
        courses(d, STATIONS_DD_POD, s * POD_DD, 26.0, -58.0, 2, rnd, length=(8.0, 13.0),
                proud=0.26, band=0.5)
        seams(d, STATIONS_DD_POD, s * POD_DD, 26.0, -58.0, 7, -0.55, 1.1, width=0.4)
        walkway(d, STATIONS_DD_POD, s * POD_DD, 24.0, -58.0, width_frac=0.62, thick=0.6,
                pitch=5.5)
        feed(d, s * (POD_DD + 3.4), -30.0, 1.4, 7.0, r=0.26)
        bank(d, s * (POD_DD + 3.4), -30.0, 1.4, height=7.0, slats=4,
             length=16.0, tilt_deg=28.0)

    for s in (-1, 1):
        strake(d, STATIONS_DD, 0.0, s, width=1.1, proud=0.28)
    courses(d, STATIONS_DD, 0.0, 60.0, -66.0, 3, rnd, length=(13.0, 20.0), proud=0.34,
            band=0.5, bolts=2)
    f.panel_lines(d, STATIONS_DD, 1.0, -1.9, 0.3, 8, y0=40.0, y1=-40.0, width=0.55,
                  proud=SEAM, skip=(40.0, -36.0, 8.0))
    for s in (-1, 1):
        windows(d, STATIONS_DD, 0.0, 56.0, -62.0, 3.6, span=1.5, h=0.6, pitch=3.0, s=s)

    for z in (40.0, -36.0):
        f.barbette(d, 0.0, DECK_DD, z, 4.4, height=1.4, seg=24)
        ring(d, 0.0, z, DECK_DD + 0.4, 4.6, count=8, h=0.22)
    for s in (-1, 1):
        d.box((s * 7.0, 8.0, DECK_DD + 0.5), (5.0, 6.0, 1.0), "Paint", chamfer=0.3)
        f.barbette(d, s * 7.0, 7.0, 8.0, 2.1, height=1.2, seg=14)

    # A squat, heavily chamfered wheelhouse set well forward; the deck behind is
    # left clear for the combine's crane.
    d.box((0.0, -4.0, 8.4), (14.0, 30.0, 5.0), "Paint", chamfer=1.4, taper=0.84)
    d.box((0.0, 6.0, 12.2), (9.0, 13.0, 3.8), "Paint", chamfer=1.0, taper=0.78, taper_y=0.82)
    d.box((0.0, 7.8, 14.1), (7.0, 0.25, 0.9), "Windows")
    for s in (-1, 1):
        d.box((s * 4.45, 6.0, 14.1), (0.25, 8.0, 0.9), "Windows")
        for yy in range(-16, 4, 3):
            if rnd.random() < 0.7:
                d.box((s * 6.6, yy, 8.4), (0.2, 1.4, 0.6), "Windows")
    d.box((0.0, 2.0, 16.6), (5.0, 6.0, 1.6), "Paint", chamfer=0.4)
    d.cyl((0.0, 2.0, 17.4), (0.0, 2.0, 24.0), 0.42, 0.18, seg=6, slot="Dark")
    d.box((0.0, 2.0, 21.4), (4.6, 0.2, 0.2), "Dark")
    d.box((0.0, 2.0, 24.1), (0.22, 0.22, 0.22), "Heat")
    for yd, zd in ((14.0, 14.0), (-12.0, 10.4)):
        d.box((0.0, yd, zd), (3.6, 3.0, 1.9), "Paint", chamfer=0.5, taper=0.8)
        d.box((0.0, yd - 0.6, zd + 0.4), (7.8, 0.7, 0.7), "Dark", chamfer=0.2)

    # Crane gantry over the working deck, and a raked slat stack over the engine bay.
    for s in (-1, 1):
        d.cyl((s * 6.0, -24.0, DECK_DD), (s * 6.0, -24.0, 16.0), 0.55, 0.45, seg=8, slot="Trim")
        d.cyl((s * 6.0, -24.0, 16.0), (s * 6.0, -2.0, 15.4), 0.35, 0.35, seg=6, slot="Dark")
    d.box((0.0, -4.0, 15.1), (13.0, 1.0, 1.0), "Trim", chamfer=0.2)
    feed(d, 0.0, -56.0, 9.0, 8.0, r=0.28)
    f.radiator(d, 0.0, -56.0, 9.0, height=8.0, slats=3, length=13.0, tilt_deg=34.0)

    hatches(d, STATIONS_DD, 64.0, -66.0, 44, rnd, wfrac=0.68)
    # The vent stacks were isolated cones on bare deck; a machinery bank around them
    # gives the foredeck a reason to be there.
    for s in (-1, 1):
        for yy in (66.0, 58.0):
            d.box((s * 6.0, yy, DECK_DD + 1.1), (7.0, 9.0, 2.2), "Dark", chamfer=0.4)
            d.box((s * 6.0, yy - 5.4, DECK_DD + 0.7), (5.0, 3.0, 1.4), "Paint", chamfer=0.3)
        d.cyl((s * 2.4, 52.0, DECK_DD), (s * 2.4, 52.0, DECK_DD + 5.2), 0.5, 0.42,
              seg=8, slot="Dark")

    for s in (-1, 1):
        d.box((s * 8.0, 66.0, 5.2), (3.0, 5.0, 3.0), "Dark", chamfer=0.5)
        d.box((s * 8.0, -66.0, 4.6), (3.0, 5.0, 3.0), "Dark", chamfer=0.5)
    d.box((0.0, -10.0, -7.4), (3.0, 100.0, 1.6), "Dark", chamfer=0.4)
    d.cyl((0.0, 68.0, -4.4), (0.0, 68.0, -6.0), 2.6, 1.9, seg=14, slot="Dark")
    d.box((0.0, -76.0, 0.0), (17.0, 6.0, 9.0), "Dark", chamfer=1.2)
    d.box((0.0, -79.2, 0.0), (14.0, 0.4, 6.0), "Trim")
    for x, z, r in ((-4.0, 0.5, 2.8), (4.0, 0.5, 2.8), (0.0, -3.0, 2.0)):
        f.nozzle(d, x, z, r, -79.0, engines, seg=22, ribs=2, trim=False)
    # Ribs standing proud of the plate courses, and a walkable rail round the roof.
    for s in (-1, 1):
        buttress(d, STATIONS_DD, 0.0, s, 62.0, -68.0, 7.0, rnd)
        handrail(d, STATIONS_DD, 0.0, s, 34.0, -30.0, post=6.0, r=0.16)
        buttress(d, STATIONS_DD_POD, s * POD_DD, s, 30.0, -62.0, 6.0, rnd, proud=0.7,
                 dark_p=0.2)
        for i in range(3):
            d.cyl((s * POD_DD, 24.0 - i * 14.0, 5.4), (s * POD_DD, 24.0 - i * 14.0, 8.4),
                  1.9, 1.7, seg=12, slot="Dark")
        d.cyl((s * POD_DD, -40.0, 5.2), (s * POD_DD, -40.0, 9.2), 2.3, 1.8, seg=12, slot="Dark")
        d.cyl((s * POD_DD, -46.0, 5.2), (s * POD_DD, -46.0, 8.0), 1.7, 1.5, seg=12, slot="Paint")
        for yy in (66.0, 58.0):
            w, _, _ = f.interp(STATIONS_DD, yy)
            d.cyl((s * (w * 0.8), yy, 5.6), (s * (w * 0.8), yy, 6.6), 0.55, 0.45, seg=8,
                  slot="Trim")
        d.box((s * 9.5, 72.0, 4.0), (2.6, 5.0, 4.4), "Dark", chamfer=0.6)

    parts.append(d.build(smooth_angle=26))
    return f.finish(parts, "Destroyer", col, engines)


# --------------------------------------------------------------------------- transport
# y station, half-beam, deck height, keel depth. 117 m.
STATIONS_TR = [
    (59.0, 6.0, 6.6, 0.0),
    (52.0, 11.0, 7.4, -4.0),
    (38.0, 14.0, DECK_TR, -6.5),
    (0.0, 15.0, DECK_TR, -7.0),
    (-38.0, 14.0, DECK_TR, -6.5),
    (-52.0, 11.0, 7.4, -4.0),
    (-58.0, 8.0, 6.6, -2.0),
]
POD_TR = 15.5
STATIONS_TR_POD = [
    (-4.0, 2.4, 1.6, -2.4),
    (-16.0, 3.2, 1.8, -3.2),
    (-44.0, 3.2, 1.8, -3.2),
    (-54.0, 2.4, 1.6, -2.0),
]


def transport(col):
    """A slab-sided lighter with no guns to speak of: the roof is the point, stacked
    with the combine's plant in painted containers. Two low outboard nacelles slung
    well aft, four main bells, and point defence in raised tubs fore and aft."""
    rnd = random.Random(41)
    hull = k.Part("Hull", col)
    slab(hull, STATIONS_TR)
    parts = [hull.build(bevel=0.22, smooth_angle=18)]
    d = k.Part("Detail", col)
    engines = []

    for s in (-1, 1):
        pod(d, s * POD_TR, STATIONS_TR_POD, None, 2.4, engines, -54.0, (0.0,),
            hull_stations=STATIONS_TR)
        for y in (-10.0, -40.0):
            pylon(d, s * 12.0, s * (POD_TR - 2.0), y, 0.0, 5.0, 6.0)
        courses(d, STATIONS_TR_POD, s * POD_TR, -8.0, -48.0, 2, rnd, length=(9.0, 14.0),
                proud=0.26, band=0.5)
        seams(d, STATIONS_TR_POD, s * POD_TR, -8.0, -48.0, 6, -2.1, -0.6, width=0.45)
        walkway(d, STATIONS_TR_POD, s * POD_TR, -6.0, -50.0, width_frac=0.62, thick=0.5,
                pitch=4.5)
        feed(d, s * (POD_TR + 3.4), -28.0, -1.0, 6.5, r=0.24)
        bank(d, s * (POD_TR + 3.4), -28.0, -1.0, height=6.5, slats=4,
             length=14.0, tilt_deg=30.0)

    for s in (-1, 1):
        strake(d, STATIONS_TR, 0.0, s, width=1.0, proud=0.26)
    courses(d, STATIONS_TR, 0.0, 48.0, -50.0, 3, rnd, length=(14.0, 21.0), proud=0.4,
            band=0.5, bolts=2)
    f.panel_lines(d, STATIONS_TR, 1.0, -1.9, 1.2, 8, y0=38.0, y1=-38.0, width=0.6,
                  proud=SEAM, skip=(34.0, -30.0))
    for s in (-1, 1):
        windows(d, STATIONS_TR, 0.0, 44.0, -46.0, 4.4, span=1.8, h=0.7, pitch=3.6, s=s)

    # Container roof, laid out as a stowage plan rather than a checkerboard: a centre
    # aisle you can read down, four rows of cells either side of it, uneven stacks in
    # two footprints, a fifth of the cells deliberately left open, corner guides and
    # lashing bars on every stack, and the plant grouped at the two ends.
    deck = DECK_TR
    bays = (27.0, 17.2, 7.4, -2.4, -12.2, -22.0)
    for xs in (-9.2, -4.8, 4.8, 9.2):
        for yb in bays:
            d.box((xs, yb, deck + 0.06), (4.4, 8.6, 0.12), "Paint")
            for cx in (-1, 1):
                for cy in (-1, 1):
                    d.box((xs + cx * 2.1, yb + cy * 4.1, deck + 0.5), (0.4, 0.4, 1.0), "Dark")
            r = rnd.random()
            if r < 0.2:
                continue  # open cell: guides and floor showing, the deck reading through
            ly = 7.8 if rnd.random() < 0.62 else 4.0
            # Mostly one or two high: a field of three-high stacks is a wall, not a
            # stowage plan, and it hides the bay rhythm from above.
            stack = 1 + int(rnd.random() * 2.4)
            base = "Trim" if rnd.random() < 0.1 else ("Dark" if rnd.random() < 0.4 else "Paint")
            z = deck + 0.12
            for lvl in range(stack):
                h = 2.6 if rnd.random() < 0.74 else 3.3
                d.box((xs, yb, z + h * 0.5), (4.0, ly, h),
                      base if rnd.random() < 0.6 else "Paint", chamfer=0.3)
                z += h
            # Lashing: straps down the stack sides and a bar over each end.
            for cx in (-1, 1):
                d.box((xs + cx * 2.05, yb, z - (z - deck - 0.12) * 0.5), (0.22, ly + 0.2, z - deck),
                      "Dark")
            for cy in (-1, 1):
                d.box((xs, yb + cy * (ly * 0.5 - 0.3), z + 0.12), (4.3, 0.4, 0.24), "Dark")
    # Centre aisle: a marked lane with cross members, not a glowing yellow panel.
    d.box((0.0, 2.0, deck + 0.05), (5.6, 58.0, 0.1), "Dark")
    for y in range(30, -27, -4):
        d.box((0.0, y, deck + 0.14), (5.2, 0.5, 0.12), "Paint")
    for y in (30.0, -26.0):
        d.box((0.0, y, deck + 0.16), (4.0, 1.0, 0.2), "Heat")
    # Cell guide rails mark the outboard edge of each bay. That is all the trim the
    # deck gets: the containers themselves are mostly hull paint.
    for s in (-1, 1):
        for yb in bays:
            d.box((s * 11.7, yb, deck + 0.12), (0.8, 8.0, 0.24), "Trim", chamfer=0.06)

    # Bridge block forward, standing clear of the stacks, and a gantry crane.
    d.box((0.0, 44.0, 11.4), (13.0, 13.0, 7.0), "Paint", chamfer=1.2, taper=0.8, taper_y=0.8)
    d.box((0.0, 48.4, 11.8), (9.4, 0.3, 1.4), "Windows")
    for s in (-1, 1):
        d.box((s * 6.4, 44.0, 11.8), (0.3, 8.0, 1.4), "Windows")
    d.cyl((0.0, 41.0, 14.9), (0.0, 41.0, 24.0), 0.5, 0.22, seg=6, slot="Dark")
    d.box((0.0, 41.0, 20.4), (6.0, 0.4, 0.4), "Dark")
    d.box((0.0, 41.0, 24.1), (0.24, 0.24, 0.24), "Heat")
    for s in (-1, 1):
        d.cyl((s * 8.0, 30.0, DECK_TR), (s * 8.0, 30.0, 15.0), 0.6, 0.5, seg=8, slot="Trim")
        d.cyl((s * 8.0, 30.0, 15.0), (s * 8.0, 4.0, 14.2), 0.4, 0.4, seg=6, slot="Dark")
    d.box((0.0, 30.0, 14.6), (17.0, 1.0, 1.0), "Trim", chamfer=0.2)

    # Point defence in raised tubs, one each end of the working deck. The tub top is
    # marked with two short bars, not a full plate of colour.
    for z in (34.0, -30.0):
        d.box((0.0, z, 8.8), (7.0, 7.0, 1.6), "Paint", chamfer=0.4)
        for s in (-1, 1):
            d.box((s * 2.2, z, 9.6), (1.4, 5.4, 0.3), "Trim")
        f.barbette(d, 0.0, 9.0, z, 2.3, height=1.3, seg=14)

    # Machinery is grouped at the two ends of the cargo deck, leaving the stowage
    # plan in the middle to be read on its own.
    hatches(d, STATIONS_TR, 52.0, 35.0, 10, rnd, wfrac=0.5)
    hatches(d, STATIONS_TR, -29.0, -48.0, 10, rnd, wfrac=0.5)
    for s in (-1, 1):
        d.cyl((s * 5.0, 50.0, 7.0), (s * 5.0, 50.0, 8.8), 1.7, 1.5, seg=12, slot="Dark")
        d.box((s * 5.0, 50.0, 7.1), (7.0, 2.2, 2.2), "Trim", chamfer=0.4)
        d.cyl((s * 9.0, -50.0, 7.0), (s * 9.0, -50.0, 10.4), 2.6, 1.6, seg=12, slot="Paint")
        d.cyl((s * 9.0, -50.0, 10.5), (s * 9.0, -50.0, 10.9), 1.7, seg=12, slot="Dark")
        d.box((s * 9.0, -42.0, 9.6), (7.0, 9.0, 3.2), "Paint", chamfer=0.5)
        # A recessed hatch in the hopper lid, not a flat emissive panel: 5 x 7 m of
        # Heat clipped to a solid yellow rectangle and read as a sticker.
        d.box((s * 9.0, -42.0, 11.25), (5.6, 7.6, 0.35), "Dark")
        d.box((s * 9.0, -42.0, 11.35), (3.4, 5.0, 0.2), "Heat")
        # Settling tanks and a pump house: the plant that feeds the hoppers.
        d.cyl((s * 3.4, -34.5, deck), (s * 3.4, -34.5, deck + 4.4), 2.1, 1.8, seg=12,
              slot="Paint")
        d.cyl((s * 3.4, -34.5, deck + 4.4), (s * 3.4, -34.5, deck + 4.8), 1.9, seg=12,
              slot="Dark")
        d.box((s * 7.0, -30.0, deck + 1.3), (5.0, 7.0, 2.6), "Dark", chamfer=0.4)
        d.box((s * 8.0, 52.0, 5.4), (3.0, 5.0, 3.0), "Dark", chamfer=0.5)
        d.box((s * 8.0, -52.0, 5.4), (3.0, 5.0, 3.0), "Dark", chamfer=0.5)
    # A dorsal bank over the engine bay: this hull had no heat rejection of its own.
    feed(d, 0.0, -50.0, deck + 5.6, 6.0, r=0.26)
    f.radiator(d, 0.0, -50.0, deck + 5.6, height=6.0, slats=3, length=13.0, tilt_deg=30.0)
    d.box((0.0, 56.0, 7.2), (9.0, 5.0, 2.0), "Paint", chamfer=0.5, taper=0.7)
    for s in (-1, 1):
        # Lander cradles stowed flat along the flanks, in Trim so they read.
        for y in (18.0, -14.0):
            d.box((s * 12.4, y, 1.0), (3.4, 13.0, 5.0), "Dark", chamfer=0.5)
            d.box((s * 14.4, y, 1.6), (0.9, 7.0, 2.0), "Trim", chamfer=0.3)
        d.box((s * 14.6, 2.0, 5.2), (0.3, 40.0, 0.5), "Dark")

    d.box((0.0, -6.0, -7.4), (4.0, 100.0, 1.6), "Dark", chamfer=0.5)
    d.box((0.0, -60.0, 0.0), (24.0, 6.0, 13.0), "Dark", chamfer=1.4)
    d.box((0.0, -63.2, 0.0), (20.0, 0.4, 9.0), "Dark")
    d.box((0.0, -63.4, 2.6), (14.0, 0.3, 1.4), "Trim")
    for x, z, r in ((-6.6, 2.5, 3.5), (6.6, 2.5, 3.5), (-3.0, -4.0, 2.4),
                    (3.0, -4.0, 2.0)):
        f.nozzle(d, x, z, r, -63.0, engines, seg=22, ribs=2, trim=False)

    # Ribs and a short rail run: the lashing gear now lives in the stowage plan.
    for s in (-1, 1):
        buttress(d, STATIONS_TR, 0.0, s, 50.0, -52.0, 7.5, rnd)
        handrail(d, STATIONS_TR, 0.0, s, 40.0, -36.0, post=5.5, r=0.16)
        buttress(d, STATIONS_TR_POD, s * POD_TR, s, -8.0, -50.0, 6.0, rnd, proud=0.7,
                 dark_p=0.2)
        d.cyl((s * POD_TR, -20.0, 1.8), (s * POD_TR, -20.0, 4.6), 1.7, 1.5, seg=12, slot="Dark")
        d.cyl((s * POD_TR, -36.0, 1.8), (s * POD_TR, -36.0, 4.0), 1.4, 1.2, seg=12, slot="Dark")
        for yy in (46.0, -46.0):
            w, _, _ = f.interp(STATIONS_TR, yy)
            d.cyl((s * (w * 0.86), yy, 7.0), (s * (w * 0.86), yy, 8.2), 0.6, 0.5, seg=8,
                  slot="Trim")


    parts.append(d.build(smooth_angle=26))
    return f.finish(parts, "Transport", col, engines)


# --------------------------------------------------------------------------- corvette
# y station, half-beam, deck height, keel depth. 47 m, one gun forward.
STATIONS_CV = [
    (25.0, 1.2, 3.0, 0.0),
    (21.0, 3.0, 3.4, -1.5),
    (14.0, 4.2, DECK_CV, -2.6),
    (4.0, 4.6, DECK_CV, -3.0),
    (-10.0, 4.6, DECK_CV, -3.0),
    (-18.0, 4.0, 3.4, -2.4),
    (-22.0, 3.0, 3.2, -1.6),
]
POD_CV = 6.4
STATIONS_CV_POD = [
    (8.0, 1.0, 1.4, 0.2),
    (3.0, 1.5, 1.6, -0.8),
    (-13.0, 1.5, 1.6, -0.8),
    (-18.0, 1.0, 1.4, 0.0),
]


def corvette(col):
    """A tug: a stubby chamfered block barely longer than it is wide, two stubby
    outboard nacelles slung on short pylons, and one gun on a wide flat foredeck.
    The whole thing is a tug with a gun bolted on the front of it."""
    rnd = random.Random(53)
    hull = k.Part("Hull", col)
    slab(hull, STATIONS_CV)
    parts = [hull.build(bevel=0.1, smooth_angle=18)]
    d = k.Part("Detail", col)
    engines = []

    for s in (-1, 1):
        pod(d, s * POD_CV, STATIONS_CV_POD, None, 1.35, engines, -18.5, (0.4,),
            hull_stations=STATIONS_CV)
        for y in (2.0, -10.0):
            pylon(d, s * 3.6, s * (POD_CV - 1.0), y, 0.0, 2.6, 3.4)
        courses(d, STATIONS_CV_POD, s * POD_CV, 4.0, -16.0, 1, rnd, length=(4.0, 6.0),
                proud=0.16, band=0.6)
        walkway(d, STATIONS_CV_POD, s * POD_CV, 4.0, -16.0, width_frac=0.6, thick=0.35,
                pitch=3.0)
        # Heat goes on the pod roof, not out on the flank: a 2.4 m bank hung off a
        # 47 m tug's side reads as three rods sticking out of it.
        feed(d, s * POD_CV, -8.0, 1.6, 2.0, r=0.14)
        bank(d, s * POD_CV, -8.0, 1.6, height=2.0, slats=3, length=5.0, tilt_deg=26.0)

    # The hull was a smooth chamfered blob: a strake at the shoulder, two plate
    # courses and a seamed band under them give the flank something to read.
    for s in (-1, 1):
        strake(d, STATIONS_CV, 0.0, s, width=0.85, proud=0.24)
    courses(d, STATIONS_CV, 0.0, 16.0, -18.0, 2, rnd, length=(4.0, 6.5), proud=0.22,
            band=0.55)
    f.panel_lines(d, STATIONS_CV, 1.0, -0.7, 0.5, 6, y0=14.0, y1=-16.0, width=0.3,
                  proud=SEAM, skip=(8.0,))
    for s in (-1, 1):
        windows(d, STATIONS_CV, 0.0, 14.0, -16.0, 2.4, span=0.9, h=0.4, pitch=1.7, s=s)
        windows(d, STATIONS_CV, 0.0, 13.0, -15.0, 0.9, span=0.7, h=0.32, pitch=2.3, s=s)

    f.barbette(d, 0.0, DECK_CV, 8.0, 1.5, height=0.9, seg=14)
    d.cyl((0, 8.0, 3.7), (0, 8.0, 3.85), 1.6, 1.6, seg=14, slot="Trim")

    # A single wheelhouse, hard against the back of the gun pad. Its forward face is
    # the second-largest blank plate on the hull, so it gets a window band, a brow and
    # seam lines; the old window strip was buried inside the casing at y=1.6.
    d.box((0.0, 0.0, 4.6), (5.4, 7.0, 2.4), "Paint", chamfer=0.5, taper=0.82)
    d.box((0.0, 3.62, 4.75), (3.9, 0.2, 0.8), "Dark", chamfer=0.03)
    d.box((0.0, 3.68, 4.75), (3.3, 0.2, 0.5), "Windows", chamfer=0.02)
    d.box((0.0, 3.45, 5.72), (4.6, 0.5, 0.24), "Dark", chamfer=0.05)
    for sx in (-1, 1):
        d.box((sx * 1.1, 3.55, 3.95), (0.2, 0.16, 2.0), "Dark", chamfer=0.02)
    d.box((0.0, -3.55, 4.2), (3.2, 0.2, 0.6), "Dark", chamfer=0.03)
    d.box((0.0, -3.62, 4.2), (2.6, 0.2, 0.36), "Windows", chamfer=0.02)
    for s in (-1, 1):
        d.box((s * 2.25, 0.0, 5.1), (0.2, 4.0, 0.6), "Windows")
    d.box((0.0, 0.0, 5.9), (4.0, 5.0, 0.3), "Dark", chamfer=0.1)
    # The wheelhouse roof was the largest single blank plate on the hull. It gets a
    # coamed hatch, a director, a vent cluster, seam lines and a ladder.
    d.box((0.0, -1.7, 6.15), (2.8, 2.2, 0.5), "Paint", chamfer=0.12)
    d.box((0.0, -1.7, 6.45), (2.2, 1.6, 0.25), "Dark", chamfer=0.08)
    d.box((0.0, -1.7, 6.6), (0.9, 0.3, 0.1), "Trim", chamfer=0.03)
    for yy in (-2.9, -0.5, 1.9):
        d.box((0.0, yy, 6.06), (3.9, 0.22, 0.16), "Paint", chamfer=0.03)
    d.cyl((1.3, 1.5, 6.05), (1.3, 1.5, 6.9), 0.16, 0.13, seg=8, slot="Dark")
    d.box((1.3, 1.5, 7.1), (1.0, 0.34, 0.3), "Dark", chamfer=0.06)
    for i in range(3):
        d.cyl((-1.5, 1.9 - i * 0.9, 6.05), (-1.5, 1.9 - i * 0.9, 6.75), 0.24, 0.2,
              seg=8, slot="Dark")
    d.box((1.9, -2.6, 6.1), (0.5, 0.14, 0.7), "Dark", chamfer=0.03)
    d.box((1.9, -2.6, 5.55), (0.5, 0.14, 0.5), "Dark", chamfer=0.03)
    d.cyl((0.0, 0.0, 6.0), (0.0, 0.0, 9.6), 0.22, 0.1, seg=6, slot="Dark")
    d.box((0.0, 0.0, 7.8), (2.4, 0.2, 0.2), "Dark")
    d.box((0.0, 0.0, 9.7), (0.16, 0.16, 0.16), "Heat")
    # Raked slat stack over the engine bay, and a working winch on the aft deck.
    feed(d, 0.0, -13.0, 4.6, 3.0, r=0.2)
    f.radiator(d, 0.0, -13.0, 4.6, height=3.0, slats=3, length=5.4, tilt_deg=32.0)
    d.box((0.0, -17.0, 3.8), (3.0, 3.0, 1.6), "Dark", chamfer=0.3)
    d.cyl((0.0, -17.0, 4.6), (0.0, -15.6, 4.6), 0.7, seg=10, slot="Trim")
    # A hatch with a raised coaming, not a yellow slab lying on the deck.
    d.box((0.0, -9.0, 4.4), (4.6, 6.0, 1.8), "Paint", chamfer=0.35)
    d.box((0.0, -9.0, 5.3), (3.8, 5.0, 0.9), "Dark", chamfer=0.2)
    for sy in (-1, 1):
        for sx in (-1, 1):
            d.box((sx * 1.7, -9.0 + sy * 2.4, 5.5), (1.2, 0.5, 0.4), "Trim", chamfer=0.1)
    d.box((0.0, -6.4, 6.0), (2.2, 2.0, 1.0), "Dark", chamfer=0.2)

    hatches(d, STATIONS_CV, 14.0, -18.0, 28, rnd, wfrac=0.6)
    # Foredeck working gear forward of the gun pad: a capstan, a liferack and a
    # fender belt round the blunt bow.
    d.cyl((0.0, 15.0, 3.4), (0.0, 15.0, 4.6), 1.0, 0.8, seg=12, slot="Dark")
    d.cyl((0.0, 15.0, 4.6), (0.0, 15.0, 4.8), 1.4, seg=12, slot="Trim")
    for s in (-1, 1):
        d.box((s * 2.6, 16.0, 4.2), (0.3, 3.4, 1.8), "Trim", chamfer=0.08)
        for yy in (20.0, 23.0):
            d.box((s * 2.4, yy, 1.6), (1.6, 2.0, 2.6), "Dark", chamfer=0.35)
        # Bow chine and floodlights: the nose was a blank box.
        d.box((s * 2.6, 20.5, 1.2), (1.0, 6.0, 0.9), "Dark", chamfer=0.2)
        d.cyl((s * 1.5, 19.0, 3.6), (s * 1.5, 19.0, 4.4), 0.42, 0.36, seg=8, slot="Dark")
        d.cyl((s * 1.5, 19.0, 4.4), (s * 1.5, 19.3, 4.5), 0.34, seg=8, slot="Heat")
    d.box((0.0, 18.0, 3.9), (5.2, 1.0, 0.9), "Trim", chamfer=0.15)
    # Roof plant: a vent cluster and a lashed crate stack, so the wheelhouse is not
    # sitting on an empty apron.
    for s in (-1, 1):
        for i in range(3):
            d.cyl((s * 2.4, -4.0 - i * 2.6, 3.4), (s * 2.4, -4.0 - i * 2.6, 4.6),
                  0.7, 0.6, seg=10, slot="Dark")
        d.box((s * 2.2, -11.0, 4.3), (3.0, 4.4, 1.8), "Paint", chamfer=0.3)
        d.box((s * 2.2, -11.0, 5.35), (2.6, 3.8, 0.7), "Dark", chamfer=0.2)
    d.box((0.0, -6.0, -3.3), (2.0, 22.0, 1.0), "Dark", chamfer=0.3)
    d.cyl((0.0, 18.0, -1.4), (0.0, 18.0, -2.6), 1.8, 1.3, seg=12, slot="Dark")
    d.box((0.0, -24.0, 0.4), (7.0, 4.0, 4.6), "Dark", chamfer=0.6)
    d.box((0.0, -26.2, 0.4), (5.6, 0.3, 3.0), "Trim")
    f.nozzle(d, 0.0, 0.0, 1.3, -26.0, engines, seg=18, ribs=2, trim=False)

    # Tug furniture: ribs, rails, a bow plough and the bollards a hull this blunt
    # needs to be pushed around with.
    for s in (-1, 1):
        buttress(d, STATIONS_CV, 0.0, s, 16.0, -20.0, 3.4, rnd, proud=0.55, dark_p=0.34)
        handrail(d, STATIONS_CV, 0.0, s, 8.0, -10.0, height=0.8, post=3.0, r=0.11)
        buttress(d, STATIONS_CV_POD, s * POD_CV, s, 6.0, -16.0, 3.0, rnd, proud=0.45,
                 dark_p=0.2)
        d.cyl((s * POD_CV, -2.0, 1.6), (s * POD_CV, -2.0, 3.2), 1.1, 0.9, seg=10, slot="Dark")
        for yy in (18.0, 10.0, -12.0):
            w, _, _ = f.interp(STATIONS_CV, yy)
            d.cyl((s * (w * 0.72), yy, 3.4), (s * (w * 0.72), yy, 4.2), 0.4, 0.32, seg=8,
                  slot="Trim")
        d.box((s * 4.0, 22.0, 2.0), (2.0, 3.0, 2.4), "Dark", chamfer=0.35)
    d.box((0.0, 22.5, 1.2), (6.0, 3.0, 1.6), "Dark", chamfer=0.4)
    d.box((0.0, 23.6, 1.2), (4.6, 0.3, 1.0), "Trim")
    d.cyl((0.0, -4.0, 3.6), (0.0, -4.0, 4.4), 1.3, seg=12, slot="Trim")
    d.cyl((0.0, 4.0, 3.6), (0.0, 4.0, 4.2), 0.9, seg=10, slot="Dark")

    parts.append(d.build(smooth_angle=26))
    return f.finish(parts, "Corvette", col, engines)


# --------------------------------------------------------------------------- frigate
# y station, half-beam, deck height, keel depth. 90 m, guns fore and aft.
STATIONS_FR = [
    (47.0, 2.0, 4.0, 0.0),
    (42.0, 4.5, 4.4, -2.0),
    (30.0, 6.2, DECK_FR, -3.5),
    (10.0, 6.8, DECK_FR, -4.2),
    (-16.0, 6.8, DECK_FR, -4.2),
    (-32.0, 6.0, DECK_FR, -3.5),
    (-40.0, 4.4, 4.2, -2.2),
    (-43.0, 3.4, 4.0, -1.4),
]
POD_FR = 9.4
STATIONS_FR_POD = [
    (24.0, 1.4, 3.0, 1.2),
    (18.0, 2.0, 3.4, 0.0),
    (-28.0, 2.0, 3.4, 0.0),
    (-38.0, 1.4, 3.0, 1.0),
]


def frigate(col):
    """A long, narrow hopper. The nacelles are slung high on the shoulders rather than
    low on the flanks, which is what separates it from the destroyer at a glance:
    a step change in the outline just aft of the bridge. Guns fore and aft, and a
    raked slat radiator standing over the engine bay."""
    rnd = random.Random(67)
    hull = k.Part("Hull", col)
    slab(hull, STATIONS_FR)
    parts = [hull.build(bevel=0.12, smooth_angle=18)]
    d = k.Part("Detail", col)
    engines = []

    for s in (-1, 1):
        pod(d, s * POD_FR, STATIONS_FR_POD, None, 1.9, engines, -38.5, (2.2,),
            hull_stations=STATIONS_FR)
        for y in (18.0, -22.0):
            pylon(d, s * 5.2, s * (POD_FR - 1.4), y, 2.6, 4.0, 4.6)
        courses(d, STATIONS_FR_POD, s * POD_FR, 20.0, -36.0, 1, rnd, length=(6.0, 9.0),
                proud=0.2, band=0.55)
        seams(d, STATIONS_FR_POD, s * POD_FR, 20.0, -34.0, 6, -0.85, 0.35, width=0.4)
        walkway(d, STATIONS_FR_POD, s * POD_FR, 20.0, -36.0, width_frac=0.6, thick=0.5,
                pitch=5.0)
        feed(d, s * (POD_FR + 2.2), -14.0, 2.0, 5.5, r=0.22)
        bank(d, s * (POD_FR + 2.2), -14.0, 2.0, height=5.5, slats=4,
             length=14.0, tilt_deg=28.0)

    for s in (-1, 1):
        strake(d, STATIONS_FR, 0.0, s, width=0.9, proud=0.24)
    courses(d, STATIONS_FR, 0.0, 36.0, -38.0, 3, rnd, length=(9.5, 14.0), proud=0.28,
            band=0.5, bolts=2)
    f.panel_lines(d, STATIONS_FR, 1.0, -1.0, 1.3, 8, y0=30.0, y1=-30.0, width=0.45,
                  proud=SEAM, skip=(22.0, -18.0))
    for s in (-1, 1):
        windows(d, STATIONS_FR, 0.0, 34.0, -36.0, 2.8, span=1.2, h=0.5, pitch=2.6, s=s)

    for z in (22.0, -18.0):
        f.barbette(d, 0.0, DECK_FR, z, 2.0, height=1.1, seg=16)
        ring(d, 0.0, z, 4.8, 2.1, count=8, h=0.18)

    # Bridge set just aft of the forward gun, so the barrels clear the roofline.
    d.box((0.0, 10.0, 6.0), (5.2, 10.0, 3.0), "Paint", chamfer=0.8, taper=0.8, taper_y=0.82)
    d.box((0.0, 7.2, 6.1), (3.6, 0.2, 0.7), "Windows")
    for s in (-1, 1):
        d.box((s * 2.55, 10.0, 6.1), (0.2, 5.4, 0.7), "Windows")
        for yy in range(7, 14, 2):
            if rnd.random() < 0.7:
                d.box((s * 2.5, yy, 5.0), (0.2, 1.1, 0.5), "Windows")
    d.box((0.0, 10.0, 7.6), (4.0, 6.0, 0.3), "Dark", chamfer=0.1)
    d.cyl((0.0, 10.0, 7.7), (0.0, 10.0, 13.0), 0.3, 0.13, seg=6, slot="Dark")
    d.box((0.0, 10.0, 10.4), (3.6, 0.2, 0.2), "Dark")
    d.box((0.0, 10.0, 13.1), (0.18, 0.18, 0.18), "Heat")
    for yd, zd in ((16.0, 5.0), (2.0, 5.0)):
        d.box((0.0, yd, zd), (2.8, 2.4, 1.6), "Paint", chamfer=0.4, taper=0.8)
        d.box((0.0, yd - 0.5, zd + 0.35), (6.0, 0.6, 0.6), "Dark", chamfer=0.15)

    # Dorsal slat stack over the engine bay, raked aft and set between the pods.
    feed(d, 0.0, -30.0, 6.4, 6.0, r=0.24)
    f.radiator(d, 0.0, -30.0, 6.4, height=6.0, slats=3, length=8.0, tilt_deg=32.0)

    # The deck ran 98 m with almost nothing on it. Two open hopper holds break it
    # into fore and aft working areas, which is what a rock hauler is carrying.
    for yh, lh in ((-6.0, 26.0), (-34.0, 14.0)):
        for s in (-1, 1):
            d.box((s * 3.4, yh, DECK_FR + 0.9), (0.7, lh, 1.8), "Paint", chamfer=0.15)
        d.box((0.0, yh, DECK_FR + 0.3), (6.0, lh, 0.6), "Dark")
        # Heaped rock, not a flat lid: a tapered load with ribs over it.
        d.box((0.0, yh, DECK_FR + 1.2), (5.0, lh - 2.0, 0.9), "Dark", chamfer=0.2,
              taper=0.78, taper_y=0.9)
        for yb in range(int(yh - lh * 0.4), int(yh + lh * 0.4) + 1, 4):
            d.box((0.0, yb, DECK_FR + 1.75), (4.6, 0.6, 0.35), "Paint", chamfer=0.08)
        for s in (-1, 1):
            d.box((s * 3.9, yh, DECK_FR + 1.9), (1.4, 2.0, 0.5), "Trim", chamfer=0.1)
    hatches(d, STATIONS_FR, 34.0, -34.0, 34, rnd, wfrac=0.5)
    for s in (-1, 1):
        d.box((s * 5.0, 40.0, 3.0), (2.4, 4.0, 2.6), "Dark", chamfer=0.4)
        d.box((s * 4.0, -38.0, 2.8), (2.4, 4.0, 2.6), "Dark", chamfer=0.4)
    d.box((0.0, -6.0, -4.6), (2.6, 62.0, 1.2), "Dark", chamfer=0.35)
    d.cyl((0.0, 40.0, -2.8), (0.0, 40.0, -4.0), 1.9, 1.4, seg=12, slot="Dark")
    d.box((0.0, -46.0, 0.4), (10.0, 5.0, 8.0), "Dark", chamfer=0.8)
    d.box((0.0, -48.7, 0.4), (8.0, 0.3, 5.5), "Trim")
    for x, z, r in ((-2.6, 2.4, 1.9), (2.6, 2.4, 1.9), (0.0, -1.4, 1.5)):
        f.nozzle(d, x, z, r, -48.5, engines, seg=20, ribs=2, trim=False)

    # Ribs, rails and hopper gear: this hull exists to move rock, so the fittings
    # are all cargo handling, not weapons.
    for s in (-1, 1):
        buttress(d, STATIONS_FR, 0.0, s, 36.0, -40.0, 5.0, rnd, proud=0.8)
        handrail(d, STATIONS_FR, 0.0, s, 24.0, -20.0, post=4.4, r=0.14)
        buttress(d, STATIONS_FR_POD, s * POD_FR, s, 22.0, -36.0, 4.0, rnd, proud=0.6,
                 dark_p=0.2)
        d.cyl((s * POD_FR, 8.0, 3.4), (s * POD_FR, 8.0, 5.6), 1.4, 1.2, seg=12, slot="Dark")
        d.cyl((s * POD_FR, -4.0, 3.4), (s * POD_FR, -4.0, 6.2), 1.5, 1.1, seg=12, slot="Dark")
        for yy in (36.0, 26.0, -30.0):
            w, _, _ = f.interp(STATIONS_FR, yy)
            d.cyl((s * (w * 0.78), yy, 4.2), (s * (w * 0.78), yy, 5.2), 0.45, 0.36, seg=8,
                  slot="Trim")
        d.box((s * 3.0, 44.0, 2.6), (2.2, 3.4, 2.8), "Dark", chamfer=0.4)
    d.box((0.0, -23.0, 5.2), (6.0, 8.0, 1.4), "Dark", chamfer=0.4)
    for i, yy in enumerate((-21.0, -25.0)):
        d.box((0.0, yy, 6.6), (4.4, 3.2, 1.6), "Trim" if i == 1 else "Paint", chamfer=0.3)

    parts.append(d.build(smooth_angle=26))
    return f.finish(parts, "Frigate", col, engines)


BUILDERS = {
    "battleship": battleship,
    "destroyer": destroyer,
    "transport": transport,
    "corvette": corvette,
    "frigate": frigate,
}
