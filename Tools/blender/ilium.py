"""Ilium hulls: pale ceramic and sand over dark grey, built as pressure vessels.

    blender -b -P build.py -- ilium

Every hull is a blunt ogive (fleet.ring_section) closed by a narrow top deck and a
deep tucked keel, dressed in soft chamfers. The faction reads through its round
cylindrical drive nacelles, braced clear of the flank on faired pylons, and its
louvred radiator banks seated in the shell just under the deck edge -- nothing
sharp, nothing bare, everything enclosed.

Refinement pass. Three things the first pass got wrong on a round hull:

*   Trim was a 0.2 m cylinder rail lifted clear of the deck and run the whole length
    of the ship, which at hull scale is a painted wire. It is now one chamfered sheer
    strake per side over a *fraction* of the length -- an edge mark, not an outline --
    clamped to the hull's own station range so it cannot run off into space.
*   Panel seams were laid at a constant half-beam. On an ogive that floats off the
    shell near the deck and buries itself at the waterline, so seams are cut facet by
    facet around the section instead.
*   Radiator banks were stacks of plates piled along the panel normal, which from any
    angle is just a flat plate. The blades are now stacked across the run and raked
    about the normal, so you look *between* them, and the whole bank sits in a plenum
    that follows the hull's curve.
"""
import math
import random

from mathutils import Matrix, Vector

import fleet as f
import grkit as k


# How blunt each class's section reads: low is a soft round body, high is a rounded slab.
SHOULDER = {"corvette": 0.62, "frigate": 0.58, "destroyer": 0.6, "transport": 0.78, "battleship": 0.68}

# The sheer strake, per class: (y_from, y_to, width, z_frac). Trim is an accent, so the
# run covers well under half the hull and the rest of the deck edge is carried by the
# recessed seam instead. Previously every class ran a rail bow to stern.
STRAKE = {
    "corvette": (10.0, -11.0, 0.62, 0.985),
    "frigate": (24.0, -24.0, 0.85, 0.985),
    "destroyer": (38.0, -38.0, 1.10, 0.985),
    "transport": (28.0, -28.0, 1.30, 0.988),
    "battleship": (66.0, -66.0, 2.00, 0.988),
}


# ----------------------------------------------------------------- shared grammar

def section_x(stations, y, z, shoulder):
    """Half-beam of the hull's ring section at station `y`, height `z`. Radiator banks
    and seams use it to sit tangent to a flank that curves away, instead of standing off
    it like a fin or a flat decal."""
    w, top, bot = f.interp(stations, f.clamp_y(stations, y))
    h = top - bot
    z = max(bot + h * 0.03, min(top - h * 0.03, z))
    pts = f.ring_section(w, top, bot, shoulder)
    for (x0, z0), (x1, z1) in zip(pts, pts[1:]):
        if min(z0, z1) <= z <= max(z0, z1):
            t = 0.0 if abs(z1 - z0) < 1e-6 else (z - z0) / (z1 - z0)
            return x0 + (x1 - x0) * t
    return 0.0


def facet(stations, cls, y, z):
    """(half-beam, facet angle) of the flank at (y, z). 90 degrees is a vertical flank;
    less leans the facet up toward the deck, which is where an Ilium hull spends most
    of its length. Every panel bolted to the side -- seam, armour plate, radiator bank --
    takes its orientation from here so it lies on the facet it belongs to."""
    x = section_x(stations, y, z, SHOULDER[cls])
    d = 1.0
    slope = (section_x(stations, y, z + d, SHOULDER[cls]) - x) / d
    return x, math.atan2(1.0, -slope)


def flank_matrix(cls, stations, y, z, proud=0.0, x_face=None):
    """Matrix placing a part flat on the flank at (y, z): local X runs up the facet,
    local Y fore and aft, local Z out of the hull."""
    x, ang = facet(stations, cls, y, z)
    x = (x + proud) if x_face is None else (x_face + proud)
    return Matrix.Translation((x, y, z)) @ Matrix.Rotation(ang, 4, "Y"), ang


def seam(part, stations, cls, y, z_lo, z_hi, slot="Dark", proud=-0.02, width=0.55):
    """One panel seam running across the flank at station `y`.

    A seam laid at a constant half-beam floats off the shell near the deck and
    disappears into it at the waterline, which is what made the first pass's seam pass
    invisible on half the hull. This cuts the seam facet by facet along the ring
    outline instead. Most of the seam's thickness is buried and a few centimetres stand
    proud, so it reads as a gap in the plating and still catches a shadow line.
    """
    w, top, bot = f.interp(stations, f.clamp_y(stations, y))
    pts = f.ring_section(w, top, bot, SHOULDER[cls])
    half = pts[: len(pts) // 2 + 1]
    thick = abs(proud) + 0.12
    for (x0, z0), (x1, z1) in zip(half, half[1:]):
        dz = z1 - z0
        if abs(dz) < 1e-6:
            continue
        ta = max(0.0, min(1.0, (max(z0, z1) - z0) / dz))
        tb = max(0.0, min(1.0, (min(z0, z1) - z0) / dz))
        if tb - ta < 0.05:
            continue
        tm = 0.5 * (ta + tb)
        dx = x1 - x0
        run = math.hypot(dx, dz) * (tb - ta)
        nx, nz = -dz / math.hypot(dx, dz), dx / math.hypot(dx, dz)
        for s in (-1, 1):
            nxs = s * nx
            m = Matrix.Translation((s * (x0 + dx * tm + nxs * proud),
                                    y, z0 + dz * tm + nz * proud))
            part.box((0, 0, 0), (run, width, thick), slot,
                     matrix=m @ Matrix.Rotation(math.atan2(nxs, nz), 4, "Y"))


def seams(part, stations, cls, y_from, y_to, count, z_frac=(0.10, 0.70), skip=(), **kw):
    """`count` transverse seams down a flank, evenly spaced and clamped to the hull.
    `skip` leaves stations clear so a seam does not cut through a turret or a bank."""
    for i in range(count):
        y = f.clamp_y(stations, y_from + (y_to - y_from) * (i + 0.5) / count)
        if any(abs(y - c) < r for s in skip
               for c, r in ((s,) if isinstance(s, tuple) else ((s, 9.0),))):
            continue
        w, top, bot = f.interp(stations, y)
        h = top - bot
        seam(part, stations, cls, y, bot + h * z_frac[0], bot + h * z_frac[1], **kw)


def sheer_seam(part, stations, cls, z_frac, y_from, y_to, slot="Dark", proud=-0.02,
               width=0.6, pitch=9.0):
    """A longitudinal seam at a fixed fraction of hull depth, following the sheer.

    Cheaper than a second course of transverse seams and it stops the flank reading as
    one tall unbroken panel between them."""
    steps = max(2, int(round(abs(y_to - y_from) / pitch)))
    for i in range(steps):
        ya = f.clamp_y(stations, y_from + (y_to - y_from) * i / steps)
        yb = f.clamp_y(stations, y_from + (y_to - y_from) * (i + 1) / steps)
        if abs(yb - ya) < 1e-6:
            continue
        for yy in (ya, yb):
            w, top, bot = f.interp(stations, yy)
            h = top - bot
            seam(part, stations, cls, yy, bot + h * z_frac, bot + h * z_frac + width, slot,
                 proud=proud, width=abs(yb - ya) * 1.05)


def sheer_strake(part, stations, cls, y_from, y_to, width=1.0, z_frac=0.985,
                 slot="Trim", pitch=5.0, sink=0.0):
    """The deck-edge trim: one chamfered strake per side, following the sheer.

    Replaces a 0.16-0.32 m cylinder rail hovering a third of a metre above the deck.
    At hull scale that is a painted wire, and it ran the full length of every ship like
    a trawler handrail. This is a half-round moulding of real diameter laid on the
    upper chine, tapered away at both ends and terminated with a turn-down post, and it
    is clamped to the hull's own station range so no run can sail off past the bow.
    """
    lo = f.clamp_y(stations, min(y_from, y_to))
    hi = f.clamp_y(stations, max(y_from, y_to))
    steps = max(2, int(round((hi - lo) / pitch)))
    taper = max(1e-3, (hi - lo) * 0.16)
    pts = []
    for i in range(steps + 1):
        y = lo + (hi - lo) * i / steps
        w, top, bot = f.interp(stations, y)
        h = top - bot
        z = bot + h * z_frac
        x, ang = facet(stations, cls, y, z)
        nx, nz = math.sin(ang), math.cos(ang)
        # Half-sunk in the facet, so it is a moulding grown out of the shell.
        off = width * 0.22 - sink
        k = min(1.0, min(y - lo, hi - y) / taper)
        pts.append((Vector((x + nx * off, y, z + nz * off)), max(0.04, width * k * 0.5)))
    for s in (-1, 1):
        for (pa, ra), (pb, rb) in zip(pts, pts[1:]):
            part.cyl(Vector((s * pa.x, pa.y, pa.z)), Vector((s * pb.x, pb.y, pb.z)),
                     ra, rb, seg=8, slot=slot)
    # Turn-down posts, so the strake is let into the hull rather than stopping in air.
    for p, r in (pts[0], pts[-1]):
        w, top, bot = f.interp(stations, p.y)
        drop = top - p.z + r * 2.0
        for s in (-1, 1):
            part.cyl((s * p.x, p.y, p.z - r * 0.7), (s * p.x, p.y, p.z - r * 0.7 - drop),
                     r * 0.85, r * 0.55, seg=8, slot=slot)


def grille(part, centre, w, h, slats, depth=2.0, tilt=32.0, frame="Paint", blade="Dark",
           face=None, proud=0.45, matrix=None):
    """A louvred radiator bank with real depth.

    Local X is the span, local Y the run, local Z out of the panel. The blades are
    stacked across the *run* and raked about the normal, which is what lets you see
    between them. Stacked along the normal instead -- which is what this did first --
    piles the plates in depth and reads as one flat plate from every angle, which is
    exactly the defect it replaces.

    A dark plenum fills the volume behind the comb so the gaps read as depth rather
    than as the hull showing through, and a painted lip stands proud of the opening so
    the recess has an edge. `face` puts a hot strip on the outer end of each blade.
    """
    m = (matrix or Matrix()) @ Matrix.Translation(Vector(centre))
    part.box((0, 0, -depth * 0.5 + 0.1), (w, h, depth), "Dark", matrix=m)
    rake = Matrix.Rotation(math.radians(tilt), 4, "Z")
    pitch = h / slats
    for i in range(slats):
        by = -h * 0.5 + pitch * (i + 0.5)
        mm = m @ Matrix.Translation((0, by, proud * 0.5)) @ rake
        part.box((0, 0, 0), (w, pitch * 0.42, depth), blade, matrix=mm,
                 chamfer=min(0.12, depth * 0.09))
        if face:
            part.box((0, 0, depth * 0.5 - 0.18), (w * 0.96, pitch * 0.34, 0.32), face,
                     matrix=mm)
    for sy in (-1, 1):
        part.box((0, sy * (h * 0.5 + 0.34), proud * 0.3), (w + 1.3, 0.7, proud * 1.3),
                 frame, matrix=m, chamfer=0.16)
    for sx in (-1, 1):
        part.box((sx * (w * 0.5 + 0.34), 0, proud * 0.3), (0.7, h + 1.3, proud * 1.3),
                 frame, matrix=m, chamfer=0.16)


def flank_plenum(part, stations, cls, y, w, h, z_c, depth, proud=0.24, steps=4):
    """The recess a flank radiator bank is built into.

    The back of the well follows the hull and the face is the local tangent plane, taken
    from the *narrowest* station in the run so the shell can never break through it.
    That is what seats the bank in a curved hull instead of standing it proud on a
    curved shell as a flat angular slab."""
    xs = []
    for i in range(steps + 1):
        yy = f.clamp_y(stations, y + h * 0.5 * (2 * i / steps - 1))
        xs.append(section_x(stations, yy, z_c, SHOULDER[cls]))
    x_face = min(xs) + proud
    rings = []
    for i in range(steps + 1):
        yy = f.clamp_y(stations, y + h * 0.5 * (2 * i / steps - 1))
        xt, at = facet(stations, cls, yy, z_c + w * 0.5)
        xb, ab = facet(stations, cls, yy, z_c - w * 0.5)
        nts, ntc = math.sin(at), math.cos(at)
        nbs, nbc = math.sin(ab), math.cos(ab)
        rings.append((yy, [
            (x_face, z_c + w * 0.5),
            (x_face, z_c - w * 0.5),
            (xb - nbs * depth, z_c - w * 0.5 - nbc * depth),
            (xt - nts * depth, z_c + w * 0.5 - ntc * depth),
        ]))
    part.loft(rings, "Dark")
    _, ang = facet(stations, cls, y, z_c)
    return x_face, ang


def flank_grille(part, cls, stations, s, y, w, h, slats, drop=0.2, depth=1.8, tilt=32.0,
                 face=None, proud=0.45, steps=4):
    """A louvred bank let into one flank, `drop` hull-depths below the sheer."""
    sh = SHOULDER[cls]
    _, top, bot = f.interp(stations, y)
    hgt = top - bot
    z_c = top - hgt * drop
    x_face, ang = flank_plenum(part, stations, cls, y, w, h, z_c, depth, steps=steps)
    m = Matrix.Translation((s * x_face, y, z_c)) @ Matrix.Rotation(s * ang, 4, "Y")
    grille(part, (0, 0, 0), w, h, slats, depth, tilt, blade="Dark", face=face,
           proud=proud, matrix=m)


def ventral_grille(part, stations, x, y, w, h, slats, depth=1.8, matrix=None, face=None):
    """A louvred bank slung under the waist, raked outboard so it is visible from above."""
    _, _, bot = f.interp(stations, y)
    m = Matrix.Translation((x, y, bot + 0.4)) @ (matrix or Matrix())
    grille(part, (0, 0, 0), w, h, slats, depth, frame="Dark", blade="Dark", face=face,
           matrix=m)


def nacelle(part, stations, x, z, r, y0, y1, engines, seg=20, ribs=3, pylon=None,
            trim=True, fairing=True):
    """One round Ilium drive pod: a ceramic barrel domed shut at `y0` and ending in an
    `f.nozzle` bell at `y1`, carried clear of the hull by a faired root and a pylon.

    The pod used to be a smooth cylinder driven straight through the shell with a plain
    box between the two, which reads as a modelling error along the whole intersection.
    It now grows out of a faired root whose inboard end tapers into the hull shoulder,
    carries a root ring and three longitudinal seams so the barrel is not one unbroken
    tube, and its ribs alternate depth so they cast shadow.
    """
    s = f.k_sign(x)
    if fairing:
        # Faired root: a tapered collar swept from the hull shoulder out to the casing,
        # so the pod's inboard end blends into the hull instead of cutting it.
        span = y1 - y0
        for yf, t in ((y0 + span * 0.10, 0.34), (y0 + span * 0.52, 1.0)):
            yf = f.clamp_y(stations, yf)
            hw, top, bot = f.interp(stations, yf)
            xh = hw * 0.97
            xo = x - s * r * 0.55
            part.box((s * (xh + xo) * 0.5, yf, z), (abs(xo - xh), r * 2.1 * t, r * 2.0 * t),
                     "Paint", chamfer=min(r * 0.8, r * 1.1 * t), taper=0.62)
        yf = f.clamp_y(stations, y0 + span * 0.10)
        part.cyl((x, yf - r * 0.28, z), (x, yf + r * 0.28, z), r * 1.14, r * 1.14,
                 seg=seg, slot="Paint")
    if pylon is not None:
        yb = f.clamp_y(stations, y0 + (y1 - y0) * 0.42)
        part.box(((x + pylon) * 0.5, yb + r * 0.5, z * 0.5),
                 (abs(x - pylon) + r * 0.7, r * 1.6, abs(z) * 0.5 + r * 1.0),
                 "Paint", chamfer=r * 0.5, taper=0.72)
    part.cyl((x, y0, z), (x, y1, z), r, r * 0.94, seg=seg, slot="Paint")
    part.cyl((x, y0 + r * 0.55, z), (x, y0, z), r * 0.66, r, seg=seg, slot="Paint")
    part.cyl((x, y0 - r * 0.1, z), (x, y0 - r * 0.36, z), r * 0.68, r * 0.66, seg=seg, slot="Dark")
    # Longitudinal seams: three dark strips the length of the casing, so the barrel is
    # a built assembly and not one smooth tube.
    span = y1 - y0
    for a in (38.0, 90.0, 142.0):
        ar = math.radians(a)
        off = Vector((math.cos(ar), 0.0, math.sin(ar))) * r * 0.99
        part.cyl((x + off.x, y0 + span * 0.14, z + off.z),
                 (x + off.x, y1 - span * 0.02, z + off.z), r * 0.07, r * 0.07,
                 seg=4, slot="Dark")
    for i in range(ribs):
        yy = y0 + span * (0.20 + 0.62 * i / max(1, ribs - 1))
        rr = r * (1.10 if i % 2 else 1.04)
        part.cyl((x, yy, z), (x, yy - r * 0.2, z), rr, rr, seg=seg,
                 slot="Dark" if i % 2 else "Paint")
    if trim:
        part.box((x, y0 + span * 0.34, z + r * 0.86), (r * 0.5, span * 0.34, r * 0.16),
                 "Trim", chamfer=r * 0.1)
    f.nozzle(part, x, z, r * 0.78, y1, engines, seg=seg, ribs=1)


def mount_pad(part, x, y_up, z_fwd, r, depth=1.8, slot="Paint", seg=20, trim=True):
    """Soft cylindrical coaming that carries a mount standing on a narrow top deck."""
    p = k.hp(x, y_up, z_fwd)
    part.cyl(p - Vector((0, 0, depth)), p - Vector((0, 0, 0.15)), r * 1.24, r * 1.1, seg=seg, slot=slot)
    if trim:
        part.cyl(p - Vector((0, 0, 0.3)), p - Vector((0, 0, 0.02)), r * 1.2, r * 1.02,
                 seg=seg, slot="Trim")
    else:
        part.cyl(p - Vector((0, 0, 0.3)), p - Vector((0, 0, 0.02)), r * 1.2, r * 1.02,
                 seg=seg, slot="Dark")


def belt(part, stations, rnd, y_from, y_to, cls, lo=3.0, hi=7.0, dark=0.24, proud=0.4):
    """Applied armour down both flanks.

    Every plate at one standoff and one gap reads as a row of identical tiles stuck on,
    so the standoff is drawn per plate, the replaced `Dark` plates are pushed further
    out than the `Paint` ones so a repair visibly stands proud of what it replaced,
    and the larger plates carry a bolt row.
    """
    y = y_from
    while y > y_to:
        length = rnd.uniform(lo, hi)
        yc = f.clamp_y(stations, y - length * 0.5)
        w, top, bottom = f.interp(stations, yc)
        h = top - bottom
        zc = bottom + h * 0.46
        x, ang = facet(stations, cls, yc, zc)
        nx, nz = math.sin(ang), math.cos(ang)
        for s in (-1, 1):
            is_dark = rnd.random() < dark
            st = proud * (rnd.uniform(1.0, 1.8) if is_dark else rnd.uniform(0.4, 1.0))
            m = Matrix.Translation((s * (x + nx * st * 0.5), yc, zc + nz * st * 0.5)) \
                @ Matrix.Rotation(s * ang, 4, "Y")
            part.box((0, 0, 0), (h * 0.38, length - 0.8, st),
                     "Dark" if is_dark else "Paint", matrix=m, chamfer=min(0.3, st * 0.42))
            if length > (lo + hi) * 0.5:
                nb = max(2, int((length - 2.0) / 3.2))
                for i in range(nb):
                    by = -length * 0.5 + 1.5 + i * 3.2
                    part.box((0, by, 0), (st * 0.7, 0.46, 0.46), "Dark",
                             matrix=m @ Matrix.Translation((st * 0.32, 0, 0)), chamfer=0.13)
        y -= length


def hatches(part, stations, rnd, y_from, y_to, n, w_min=1.6, w_max=4.0, skip=(),
            trim_p=0.3, deck=None):
    """Deck furniture: a raised coaming with the lid set into it.

    The first pass laid a `Trim` frame with a `Dark` lid on top of the deck, which on a
    small hull read as a flat black rectangle with a yellow border stuck on the plating.
    This is a low coaming with the lid down inside it and a couple of dog bars, so it
    reads as a closed hatch with height; only a few carry a grab rail in Trim.
    """
    for _ in range(n):
        y = rnd.uniform(y_from, y_to)
        if any(abs(y - s) < h for s, h in skip):
            continue
        w, top, _ = f.interp(stations, f.clamp_y(stations, y))
        if deck is not None:
            top, span = deck
        else:
            span = w * 0.42
        x = rnd.uniform(-0.8, 0.8) * span
        sx, sy = rnd.uniform(w_min, w_max), rnd.uniform(1.8, 5.0)
        part.box((x, y, top + 0.06), (sx + 0.9, sy + 0.9, 0.34), "Paint", chamfer=0.14)
        part.box((x, y, top + 0.17), (sx, sy, 0.2), "Dark", chamfer=0.06)
        for dx in (-0.26, 0.26):
            part.box((x + dx * sx, y, top + 0.29), (0.16, sy * 0.86, 0.1), "Paint")
        if rnd.random() < trim_p:
            part.box((x, y + sy * 0.5, top + 0.34), (sx * 0.5, 0.16, 0.1), "Trim")


def panel_greebles(part, stations, rnd, y_from, y_to, n, cls, skip=()):
    """Flush hull plating patches and recessed service ports on the flanks, each lying
    on the facet at its own height rather than on a flat plane."""
    for _ in range(n):
        y = rnd.uniform(y_from, y_to)
        if any(abs(y - s) < h for s, h in skip):
            continue
        w, top, bot = f.interp(stations, f.clamp_y(stations, y))
        hgt = top - bot
        s = rnd.choice((-1, 1))
        z = bot + hgt * rnd.uniform(0.2, 0.62)
        x, ang = facet(stations, cls, y, z)
        nx, nz = math.sin(ang), math.cos(ang)
        st = rnd.uniform(0.12, 0.4)
        m = Matrix.Translation((s * (x + nx * st * 0.5), y, z + nz * st * 0.5)) \
            @ Matrix.Rotation(s * ang, 4, "Y")
        part.box((0, 0, 0), (rnd.uniform(1.6, 3.4), rnd.uniform(1.4, 3.2), st + 0.1),
                 "Dark", matrix=m, chamfer=min(0.16, st * 0.4))
        if rnd.random() < 0.3:
            part.box((0, 0, 0), (rnd.uniform(0.9, 1.4), rnd.uniform(1.2, 2.2), st + 0.24),
                     "Trim", matrix=m, chamfer=0.1)


def keel(part, stations, y0, y1, w=4.0, h=1.6, lift=0.5):
    """The tucked keel's external spine, the one hard edge an Ilium hull admits. Half sunk
    into the deepest point of the keel so it grows out of the shell, not hangs off it."""
    z = min(f.interp(stations, f.clamp_y(stations, y))[2] for y in f.fan(y1, y0, 8))
    part.box((0, (y0 + y1) * 0.5, z - lift), (w, abs(y0 - y1), h + lift * 2.0),
             "Dark", chamfer=0.5)


def mast(part, x, y, z0, height, spread=1.0):
    """A short sensor mast with a yard and a Heat bead on top."""
    part.cyl((x, y, z0), (x, y, z0 + height), 0.42 * spread, 0.16 * spread, seg=8, slot="Dark")
    part.box((x, y, z0 + height * 0.62), (4.4 * spread, 0.22, 0.22), "Dark")
    part.box((x, y, z0 + height + 0.12), (0.28 * spread, 0.28 * spread, 0.28 * spread), "Heat")


def warm(part, cls, stations, y_from, y_to, count, skip=(), z_frac=(0.10, 0.70), **kw):
    """Panel breakup: transverse seams down the flank plus a longitudinal one.

    Together these are what stop a rounded Ilium flank reading as one unbroken ceramic
    wall. On the battleship the side is a 250 m rounded shell with nothing on it, the
    largest flat surface in the fleet."""
    seams(part, stations, cls, y_from, y_to, count, z_frac=z_frac, skip=skip, **kw)
    sheer_seam(part, stations, cls, 0.42, y_from, y_to)


# ----------------------------------------------------------------- battleship

DECK = f.CONTRACT["battleship"]["deck"]
SUPERFIRE = f.SUPERFIRE

# y station, half-beam, deck height, keel depth. 354 m bow to stern, a broad blunt mass.
STATIONS_BS = [
    (182.0, 3.0, 9.2, -3.0),
    (176.0, 11.0, 11.4, -8.0),
    (166.0, 18.0, 12.2, -13.0),
    (150.0, 24.0, DECK, -17.0),
    (124.0, 28.5, DECK, -19.5),
    (92.0, 30.6, DECK, -20.0),
    (20.0, 31.0, DECK, -20.0),
    (-58.0, 30.4, DECK, -20.0),
    (-108.0, 28.8, DECK, -19.0),
    (-144.0, 24.6, DECK, -16.5),
    (-164.0, 18.4, 11.6, -12.5),
    (-172.0, 11.0, 10.0, -8.0),
]


def battleship(col):
    """A soft ceramic leviathan: blunt ogive hull far broader than it is tall, a low dorsal
    house amidships, louvred radiator banks slung under the deck edge, six secondary
    sponsons, and a cluster of round bells on a broad transom."""
    rnd = random.Random(7)
    hull = k.Part("Hull", col)
    hull.loft([(y, f.ring_section(w, t, b, SHOULDER["battleship"])) for y, w, t, b in STATIONS_BS], "Paint")

    parts = [hull.build(bevel=0.5, smooth_angle=38)]
    d = k.Part("Detail", col)

    ys, ye, sw, sz = STRAKE["battleship"]
    belt(d, STATIONS_BS, rnd, 160.0, -156.0, "battleship", lo=12.0, hi=16.0, proud=0.9, dark=0.2)
    sheer_strake(d, STATIONS_BS, "battleship", ys, ye, width=sw, z_frac=sz, pitch=7.0)
    keel(d, STATIONS_BS, -150.0, 156.0, w=6.5, h=2.4, lift=0.6)

    # The flank is a 250 m rounded wall. Transverse seams on a 12 m pitch plus two
    # longitudinal seams is what stops it reading as one extruded shell.
    warm(d, "battleship", STATIONS_BS, 152.0, -152.0, 26,
         skip=((104, 16), (72, 16), (-66, 16), (-98, 16), (14, 20), (-14, 20), (40, 16), (-36, 16)))

    # Bow: a blunt ceramic cap with a sensor blister and two anchor lights.
    d.cyl((0, 177.0, 4.0), (0, 186.0, 4.0), 6.0, 3.2, seg=24, slot="Paint")
    d.box((0, 178.0, -3.6), (5.0, 9.0, 2.0), "Dark", chamfer=0.6)
    for s in (-1, 1):
        d.box((s * 6.6, 175.0, 4.0), (1.6, 4.0, 2.2), "Trim", chamfer=0.5)

    # Superfiring islands: the hull steps up to carry the second and third main turrets.
    for y0, y1 in ((92.0, 54.0), (-52.0, -90.0)):
        d.box((0, (y0 + y1) * 0.5, 15.6), (23.0, abs(y0 - y1), 8.0), "Paint", chamfer=2.6, taper=0.9)
        d.box((0, (y0 + y1) * 0.5, 19.4), (18.0, abs(y0 - y1) - 5.0, 0.4), "Dark", chamfer=0.15)
        for s in (-1, 1):
            for yy in f.fan(y1 + 6.0, y0 - 6.0, 6):
                d.box((s * 11.4, yy, 14.0), (0.2, 2.4, 0.9), "Windows")
        # Plate seams down the island sides, so the step is not one blank wall.
        for yy in f.fan(y1 + 6.0, y0 - 6.0, 7):
            d.box((s * 11.6, yy, 15.0), (0.24, 0.5, 6.6), "Dark")

    # Dorsal house: long, low and rounded, carrying the high point-defence tubs.
    d.box((0, 6.0, 19.2), (38.0, 96.0, 6.6), "Paint", chamfer=3.2, taper=0.9)
    d.box((0, 6.0, 24.0), (27.0, 84.0, 7.2), "Paint", chamfer=2.6, taper=0.82)
    d.box((0, 6.0, 27.4), (20.0, 78.0, 0.4), "Dark", chamfer=0.15)
    for yy in f.fan(-34.0, 44.0, 16):
        for s in (-1, 1):
            if rnd.random() < 0.22:
                continue
            d.box((s * 13.6, yy, 22.4), (0.2, 2.2, 1.0), "Windows")
            if rnd.random() < 0.5:
                d.box((s * 10.2, yy, 26.0), (0.2, 2.0, 0.8), "Windows")
        # Recessed seams across the house sides: the citadel is plated, not cast.
        if rnd.random() < 0.7:
            for s in (-1, 1):
                d.box((s * 19.4, yy, 20.0), (0.3, 0.7, 5.0), "Dark")
                d.box((s * 13.8, yy, 25.0), (0.3, 0.7, 5.4), "Dark")

    # Bridge blister, domed, with a stub sensor mast.
    d.box((0, 36.0, 30.0), (19.0, 22.0, 6.4), "Paint", chamfer=2.4, taper=0.78, taper_y=0.86)
    d.box((0, 46.0, 30.4), (13.0, 0.2, 1.4), "Windows")
    for s in (-1, 1):
        d.box((s * 9.3, 36.0, 30.4), (0.2, 15.0, 1.4), "Windows")
    d.box((0, 33.0, 33.6), (10.0, 8.0, 2.0), "Dark", chamfer=0.8, taper=0.7)
    mast(d, 0, 30.0, 33.4, 8.0, spread=1.6)
    for s in (-1, 1):
        d.cyl((s * 6.4, 30.0, 30.0), (s * 6.4, 30.0, 33.2), 0.9, seg=12, slot="Dark")

    # Main turrets: A and Y on the hull deck, B and X on the superfiring islands.
    for yb, base in ((104.0, DECK), (-98.0, DECK), (72.0, SUPERFIRE), (-66.0, SUPERFIRE)):
        d.cyl((0, yb, base - 1.6), (0, yb, base + 0.9), 10.6, 10.0, seg=22, slot="Dark")
        d.cyl((0, yb, base + 0.9), (0, yb, base + 1.3), 10.3, 10.3, seg=22, slot="Trim")
    for yb in (72.0, -66.0):
        d.cyl((0, yb, SUPERFIRE - 1.0), (0, yb, SUPERFIRE + 1.0), 11.6, 11.0, seg=22, slot="Paint")
        for zb in (SUPERFIRE + 2.6, SUPERFIRE + 5.6):
            d.cyl((0, yb, zb), (0, yb, zb + 0.4), 11.8, 11.8, seg=22, slot="Trim")

    # Secondary sponsons: rounded bays carried clear of the flank, with a casemate under.
    for z in (40.0, 2.0, -36.0):
        for s in (-1, 1):
            d.box((s * 22.0, z, 8.6), (12.0, 17.0, 6.0), "Paint", chamfer=2.0, taper=0.82)
            d.box((s * 23.4, z, 4.0), (7.0, 13.0, 5.0), "Paint", chamfer=1.6, taper=1.25)
            d.box((s * 20.0, z, 11.5), (4.0, 12.0, 0.5), "Dark", chamfer=0.15)
            mount_pad(d, s * 19.0, 11.0, z, 3.4, depth=2.2)
            for yy in f.fan(z + 5.0, z - 5.0, 4):
                d.box((s * 15.0, yy, 9.6), (0.2, 1.6, 0.8), "Windows")
            for yy in f.fan(z + 6.5, z - 6.5, 3):
                d.box((s * 27.9, yy, 7.6), (0.3, 0.6, 3.4), "Dark")

    # Louvred radiator banks: a tall bank over the waist on each flank, a short one
    # between the sponsons, and one slung under the belly. Every bank is set into a
    # plenum that follows the shell, so it sits in the hull instead of on it.
    for s in (-1, 1):
        flank_grille(d, "battleship", STATIONS_BS, s, 72.0, 12.0, 30.0, 7, drop=0.16,
                     depth=2.6, tilt=34.0, face="Heat")
        flank_grille(d, "battleship", STATIONS_BS, s, -72.0, 12.0, 30.0, 7, drop=0.16,
                     depth=2.6, tilt=34.0, face="Heat")
        flank_grille(d, "battleship", STATIONS_BS, s, 20.0, 7.0, 12.0, 4, drop=0.11, depth=1.8)
        flank_grille(d, "battleship", STATIONS_BS, s, -20.0, 7.0, 12.0, 4, drop=0.11, depth=1.8)
        ventral_grille(d, STATIONS_BS, s * 12.0, -58.0, 22.0, 11.0, 6, depth=2.2,
                       matrix=Matrix.Rotation(s * math.radians(104.0), 4, "Y"))
        # Feed plumbing: visible pipes from the hull into each bank, so the exchanger
        # is connected to something instead of appearing out of nowhere.
        for z in (72.0, -72.0, 20.0, -20.0):
            hw, top, _ = f.interp(STATIONS_BS, z)
            d.cyl((s * (hw - 2.0), z, top - 9.0), (s * (hw - 0.4), z, top - 3.0), 1.1, 1.1,
                  seg=12, slot="Dark")

    # Point-defence tubs.
    for x, y_up, z in ((14.0, DECK, 132.0), (16.0, 26.0, 14.0), (14.0, DECK, -128.0)):
        for s in (-1, 1):
            mount_pad(d, s * x, y_up, z, 3.0, depth=2.0, trim=False)
            f.barbette(d, s * x, y_up, z, 2.8, height=1.4, seg=14)

    # Two round drive pods on the aft quarters, faired into the flank.
    engines = []
    for s in (-1, 1):
        nacelle(d, STATIONS_BS, s * 22.0, 4.0, 5.0, -58.0, -118.0, engines, seg=16, ribs=3,
                pylon=s * 16.0)

    hatches(d, STATIONS_BS, rnd, 152.0, -150.0, 14, w_min=2.0, w_max=5.0,
            skip=((104, 13), (72, 13), (-66, 13), (-98, 13), (14, 42), (0, 40)))
    panel_greebles(d, STATIONS_BS, rnd, -158.0, 158.0, 22, "battleship",
                    skip=((104, 14), (72, 14), (-66, 14), (-98, 14), (6, 44), (72, 20), (-72, 20)))

    # Laser farm blisters on the dorsal house roof.
    for s in (-1, 1):
        for z in (20.0, -6.0):
            d.box((s * 7.0, z, 30.0), (4.4, 6.0, 3.0), "Paint", chamfer=0.9, taper=0.72)
            d.box((s * 7.0, z - 1.6, 30.2), (5.0, 1.2, 1.0), "Dark", chamfer=0.2)

    # Transom: a broad rounded block carrying a cluster of round bells. The aft face was
    # a flat black plate; it is now plated with recessed seams, a pair of louvred
    # exhaust vents and a short Trim marker over the centreline.
    d.box((0, -158.0, 0.0), (46.0, 44.0, 22.0), "Paint", chamfer=5.0, taper=0.86, taper_y=0.9)
    for xx in f.fan(-18.0, 18.0, 7):
        d.box((xx, -179.4, -1.0), (0.6, 1.2, 17.0), "Dark")
    for zz in f.fan(-9.0, 8.0, 4):
        d.box((0.0, -179.4, zz), (34.0, 1.2, 0.6), "Dark")
    for s in (-1, 1):
        grille(d, (s * 15.0, -179.0, 1.0), 6.0, 11.0, 4, depth=1.6, tilt=28.0,
               frame="Paint", blade="Dark", face="Heat",
               matrix=Matrix.Rotation(math.radians(90.0), 4, "X"))
    # The transom roof is plated and vented rather than a blank shelf.
    d.box((0.0, -152.0, 10.4), (30.0, 30.0, 0.9), "Paint", chamfer=0.4)
    for yy in f.fan(-140.0, -164.0, 6):
        d.box((0.0, yy, 10.95), (26.0, 0.6, 0.4), "Dark")
    for xx in f.fan(-9.0, 9.0, 4):
        d.box((xx, -152.0, 10.95), (0.6, 26.0, 0.4), "Dark")
    for s in (-1, 1):
        d.cyl((s * 6.0, -146.0, 10.9), (s * 6.0, -158.0, 10.9), 2.2, 2.2, seg=14, slot="Dark")
        d.cyl((s * 6.0, -158.0, 10.9), (s * 6.0, -160.0, 10.9), 2.2, 1.8, seg=14, slot="Paint")
    d.box((0.0, -179.6, -9.0), (14.0, 1.0, 1.2), "Trim", chamfer=0.2)
    for x, z, r in ((-7.5, 5.0, 5.4), (7.5, 5.0, 5.4), (-7.5, -5.6, 5.4), (7.5, -5.6, 5.4)):
        f.nozzle(d, x, z, r, -181.0, engines, seg=24, ribs=2)
    for s in (-1, 1):
        d.box((s * 16.0, -132.0, 8.0), (8.0, 26.0, 4.0), "Paint", chamfer=1.4)
        d.box((s * 19.6, -132.0, 8.0), (0.5, 20.0, 2.4), "Dark")

    # Several hundred hull elements cannot afford the second bevel segment; every box
    # already carries an explicit chamfer, so one facet per edge reads the same at this size.
    parts.append(d.build(bevel=0.12, bevel_segments=1, smooth_angle=32))
    return f.finish(parts, "Battleship", col, engines)


# ----------------------------------------------------------------- destroyer

# y station, half-beam, deck height, keel depth. 148 m.
STATIONS_DD = [
    (76.0, 2.2, 4.6, -2.0),
    (72.0, 5.2, 5.4, -4.4),
    (63.0, 7.8, 5.9, -6.8),
    (48.0, 9.8, 6.0, -8.6),
    (20.0, 10.8, 6.0, -9.2),
    (-20.0, 10.8, 6.0, -9.2),
    (-45.0, 10.2, 6.0, -8.8),
    (-62.0, 8.8, 5.8, -7.4),
    (-72.0, 6.4, 5.2, -5.4),
]


def destroyer(col):
    """The escort that carries the faction's signature: two big round nacelles slung high
    and outboard on faired pylons, with louvred radiator banks between them."""
    rnd = random.Random(11)
    hull = k.Part("Hull", col)
    hull.loft([(y, f.ring_section(w, t, b, SHOULDER["destroyer"])) for y, w, t, b in STATIONS_DD], "Paint")
    parts = [hull.build(bevel=0.3, smooth_angle=38)]
    d = k.Part("Detail", col)

    ys, ye, sw, sz = STRAKE["destroyer"]
    belt(d, STATIONS_DD, rnd, 63.0, -60.0, "destroyer", lo=5.5, hi=8.0, proud=0.55, dark=0.24)
    sheer_strake(d, STATIONS_DD, "destroyer", ys, ye, width=sw, z_frac=sz, pitch=5.0)
    keel(d, STATIONS_DD, -64.0, 62.0, w=3.4, h=1.5, lift=0.4)
    warm(d, "destroyer", STATIONS_DD, 62.0, -62.0, 14,
         skip=((40, 12), (-36, 12), (8, 10), (32, 13), (-14, 13)))

    # Rounded bow cap and the forward sonar dome.
    d.cyl((0, 71.0, 2.0), (0, 78.0, 2.0), 3.4, 2.0, seg=20, slot="Paint")
    d.cyl((0, 69.0, -3.4), (0, 69.0, -5.2), 3.6, 2.4, seg=18, slot="Dark")

    # Dorsal house, low and long, with a window band and a squat bridge dome.
    d.box((0, 6.0, 8.2), (11.0, 34.0, 4.4), "Paint", chamfer=1.8, taper=0.86)
    d.box((0, 16.0, 11.2), (7.4, 12.0, 2.6), "Paint", chamfer=1.2, taper=0.74, taper_y=0.85)
    d.box((0, 22.2, 11.4), (5.2, 0.2, 0.9), "Windows")
    for s in (-1, 1):
        d.box((s * 3.6, 16.0, 11.4), (0.2, 8.4, 0.9), "Windows")
        for yy in f.fan(-8.0, 20.0, 6):
            if rnd.random() < 0.3:
                continue
            d.box((s * 5.4, yy, 8.2), (0.2, 1.8, 0.7), "Windows")
            # Plating seams down the house side: it is panelled, not a plain block.
            d.box((s * 5.7, yy + 1.6, 8.2), (0.3, 0.5, 3.6), "Dark")
    d.box((0, 6.0, 10.5), (8.4, 30.0, 0.3), "Dark", chamfer=0.1)
    mast(d, 0, 12.0, 12.4, 6.6, spread=1.0)
    for s in (-1, 1):
        d.cyl((s * 3.4, 12.0, 11.0), (s * 3.4, 12.0, 13.0), 0.5, seg=10, slot="Dark")

    # Main turrets fore and aft.
    for z in (40.0, -36.0):
        mount_pad(d, 0.0, 6.0, z, 3.6, depth=1.6, seg=22)
        f.barbette(d, 0.0, 6.0, z, 3.3, height=1.3, seg=18)

    # Point-defence tubs on small wing roots.
    for s in (-1, 1):
        d.box((s * 7.4, 8.0, 5.0), (4.4, 11.0, 2.6), "Paint", chamfer=1.0, taper=0.8)
        mount_pad(d, s * 7.0, 7.0, 8.0, 2.4, depth=2.0, seg=16, trim=False)
        f.barbette(d, s * 7.0, 7.0, 8.0, 2.2, height=1.1, seg=14)

    # Louvred radiator banks between the nacelles and forward and aft of them. Each is
    # seated in a plenum cut to the hull's curve, so the shell closes around it instead
    # of a flat slab standing proud of a curved side.
    for s in (-1, 1):
        flank_grille(d, "destroyer", STATIONS_DD, s, 32.0, 6.4, 17.0, 6, drop=0.17, depth=2.2)
        flank_grille(d, "destroyer", STATIONS_DD, s, 54.0, 5.0, 9.0, 4, drop=0.17, depth=1.6)
        flank_grille(d, "destroyer", STATIONS_DD, s, -14.0, 6.4, 17.0, 6, drop=0.17, depth=2.2)
        flank_grille(d, "destroyer", STATIONS_DD, s, -60.0, 5.0, 9.0, 4, drop=0.17, depth=1.6)
        for y in (32.0, -14.0):
            hw, top, _ = f.interp(STATIONS_DD, y)
            d.cyl((s * (hw - 1.2), y, top - 4.2), (s * (hw + 0.4), y, top - 1.2), 0.9, 0.9,
                  seg=10, slot="Dark")
    ventral_grille(d, STATIONS_DD, 0.0, -30.0, 8.0, 16.0, 6, depth=1.8,
                   matrix=Matrix.Rotation(math.radians(180.0), 4, "X"))

    # Aft dorsal bank: a raised housing with a deep louvred face let into its roof. This
    # used to be a flat black decal standing on the deck with no depth at all.
    _, dtop, _ = f.interp(STATIONS_DD, -44.0)
    d.box((0, -44.0, dtop + 0.5), (10.6, 17.0, 1.5), "Paint", chamfer=0.7, taper=0.84)
    for xx in f.fan(-3.6, 3.6, 3):
        d.box((xx, -44.0, dtop + 1.32), (0.4, 15.0, 0.3), "Dark")
    grille(d, (0, -44.0, dtop + 1.5), 7.6, 13.4, 7, depth=2.4, proud=0.45, tilt=30.0,
           face="Heat", matrix=Matrix.Rotation(math.radians(-18.0), 4, "X"))
    for s in (-1, 1):
        d.cyl((s * 6.6, -37.0, dtop - 0.8), (s * 4.4, -40.5, dtop + 1.3), 0.7, 0.7,
              seg=10, slot="Dark")

    # The two big round nacelles, slung high and clear of the flank on faired roots.
    engines = []
    for s in (-1, 1):
        nacelle(d, STATIONS_DD, s * 13.2, 4.8, 3.8, 0.0, -52.0, engines, seg=22, ribs=4,
                pylon=s * 9.0)
        # small ventral manoeuvring pod
        d.cyl((s * 4.6, -50.0, -7.0), (s * 4.6, -62.0, -7.0), 1.5, 1.3, seg=14, slot="Paint")
        d.cyl((s * 4.6, -62.0, -7.0), (s * 4.6, -63.4, -7.0), 1.5, 1.2, seg=14, slot="Dark")

    hatches(d, STATIONS_DD, rnd, 58.0, -62.0, 10, w_min=1.4, w_max=3.4,
            skip=((40, 9), (-36, 9), (8, 8), (0, 22)))
    panel_greebles(d, STATIONS_DD, rnd, -64.0, 62.0, 14, "destroyer",
                    skip=((40, 10), (-36, 10), (8, 9), (0, 24), (32, 12), (-14, 12)))

    parts.append(d.build(bevel=0.08, smooth_angle=32))
    return f.finish(parts, "Destroyer", col, engines)


# ----------------------------------------------------------------- transport

# y station, half-beam, deck height, keel depth. 118 m, short and very full.
STATIONS_TR = [
    (59.0, 4.0, 7.2, -4.0),
    (55.0, 9.0, 8.0, -7.0),
    (46.0, 12.4, 8.0, -9.0),
    (20.0, 13.0, 8.0, -9.5),
    (-20.0, 13.0, 8.0, -9.5),
    (-44.0, 12.4, 8.0, -9.0),
    (-54.0, 9.4, 7.6, -7.0),
    (-58.0, 4.6, 6.4, -4.0),
]

DECK_TR = 9.0  # top of the container deck plate


def transport(col):
    """A rounded nose and a flat container deck: two tidy bays of stacked pods under a
    window-banded bridge, with four round nacelles on the aft quarters."""
    rnd = random.Random(17)
    hull = k.Part("Hull", col)
    hull.loft([(y, f.ring_section(w, t, b, SHOULDER["transport"])) for y, w, t, b in STATIONS_TR], "Paint")
    parts = [hull.build(bevel=0.36, smooth_angle=36)]
    d = k.Part("Detail", col)

    ys, ye, sw, sz = STRAKE["transport"]
    belt(d, STATIONS_TR, rnd, 50.0, -48.0, "transport", lo=6.0, hi=9.0, proud=0.5, dark=0.2)
    sheer_strake(d, STATIONS_TR, "transport", ys, ye, width=sw, z_frac=sz, pitch=5.5)
    keel(d, STATIONS_TR, -50.0, 48.0, w=4.0, h=1.6, lift=0.4)
    warm(d, "transport", STATIONS_TR, 50.0, -50.0, 13,
         skip=((34, 12), (-30, 12), (-14, 13), (20, 13)))

    # Rounded nose: a blunt dome with a docking collar and lamps.
    d.cyl((0, 55.0, 2.0), (0, 63.0, 2.0), 5.0, 2.6, seg=24, slot="Paint")
    d.cyl((0, 62.0, 2.0), (0, 64.0, 2.0), 2.6, 1.6, seg=20, slot="Dark")
    d.cyl((0, 63.0, 2.0), (0, 63.6, 2.0), 1.3, seg=16, slot="Trim")
    for s in (-1, 1):
        d.box((s * 4.6, 57.0, 2.0), (1.2, 3.0, 1.6), "Trim", chamfer=0.4)

    # Container deck: a flat plate stepped onto the hull top. The kerb was a 136 m run of
    # bright Trim and read as the ship's dominant feature; it is now a dark recessed
    # gutter with the plating seams carrying the edge instead.
    d.box((0, -10.0, 8.4), (21.0, 68.0, 1.2), "Paint", chamfer=0.3)
    for s in (-1, 1):
        d.box((s * 10.2, -10.0, 8.85), (1.1, 68.0, 1.3), "Dark", chamfer=0.25)
        d.box((s * 10.75, -10.0, 9.2), (0.5, 68.0, 0.8), "Paint", chamfer=0.15)
    for yy in f.fan(-42.0, 22.0, 8):
        d.box((0.0, yy, DECK_TR + 0.02), (19.0, 0.5, 0.24), "Dark")

    # Two bays of stacked pods. Heights vary and one slot is left open, so the deck is a
    # loaded hold rather than a regular brick pile.
    for bx in (-5.4, 5.4):
        for i in range(4):
            yy = -30.0 + i * 9.4
            levels = 3 if (i + (bx > 0)) % 3 else 2
            if i == 2 and bx > 0:
                levels = 1
            sx = 8.4 if i % 2 == 0 else 7.8
            sy = 8.2 if i % 3 else 7.4
            for lvl in range(levels):
                z0 = 9.2 + lvl * 3.3
                d.box((bx, yy, z0 + 1.55), (sx, sy, 3.1),
                      "Dark" if rnd.random() < 0.28 else "Paint", chamfer=0.4)
                d.box((bx, yy - sy * 0.5 - 0.2, z0 + 1.55), (sx * 0.84, 0.3, 0.6),
                      "Dark", chamfer=0.08)
                if rnd.random() < 0.5:
                    d.box((bx, yy, z0 + 3.2), (sx * 0.7, 0.5, 0.4), "Dark", chamfer=0.1)
            for dx in (-2.6, 2.6):
                d.cyl((bx + dx, yy - sy * 0.5 - 0.2, 9.3),
                      (bx + dx, yy - sy * 0.5 - 0.2, 9.2 + levels * 3.3), 0.18, seg=6, slot="Dark")
    for s in (-1, 1):
        d.box((s * 9.6, -10.0, 10.0), (0.6, 60.0, 3.0), "Paint", chamfer=0.2)
        # Lashing rail down each bay, so the stacks are held down rather than resting.
        d.box((s * 0.5, -19.0, 12.4), (0.5, 32.0, 0.4), "Dark", chamfer=0.1)

    # Bridge: a stepped block forward with a long window band, and a second tier.
    d.box((0, 38.0, 11.8), (15.0, 20.0, 6.2), "Paint", chamfer=1.8, taper=0.9)
    d.box((0, 36.0, 16.2), (10.0, 12.0, 3.4), "Paint", chamfer=1.4, taper=0.8, taper_y=0.86)
    d.box((0, 30.2, 16.4), (7.6, 0.2, 1.1), "Windows")
    for s in (-1, 1):
        d.box((s * 4.9, 36.0, 16.4), (0.2, 9.0, 1.1), "Windows")
        for yy in f.fan(30.0, 46.0, 5):
            d.box((s * 7.4, yy, 12.2), (0.2, 2.4, 0.9), "Windows")
            d.box((s * 7.6, yy + 1.6, 12.2), (0.3, 0.5, 4.6), "Dark")
    d.box((0, 38.0, 15.1), (12.0, 18.0, 0.3), "Dark", chamfer=0.1)
    mast(d, 0, 40.0, 18.0, 6.4, spread=1.1)
    for s in (-1, 1):
        d.cyl((s * 4.0, 40.0, 16.6), (s * 4.0, 40.0, 19.0), 0.6, seg=10, slot="Dark")

    # Point-defence tubs: one on the bridge front, one on the aft deck house.
    mount_pad(d, 0.0, 9.0, 34.0, 2.6, depth=2.0, seg=18)
    f.barbette(d, 0.0, 9.0, 34.0, 2.4, height=1.2, seg=14)
    d.box((0, -34.0, 9.2), (9.0, 12.0, 2.0), "Paint", chamfer=0.8, taper=0.88)
    d.box((0, -34.0, 10.4), (7.0, 9.0, 0.3), "Dark", chamfer=0.1)
    mount_pad(d, 0.0, 9.0, -30.0, 2.6, depth=2.0, seg=18, trim=False)
    f.barbette(d, 0.0, 9.0, -30.0, 2.4, height=1.2, seg=14)
    for s in (-1, 1):
        d.box((s * 4.3, -34.0, 9.8), (0.2, 6.0, 0.7), "Windows")

    # Louvre banks on the hold flanks and under the quarters, each in a plenum cut to
    # the shell. Dark blades, painted lip: nothing on a heat exchanger is Trim yellow.
    for s in (-1, 1):
        flank_grille(d, "transport", STATIONS_TR, s, -14.0, 5.4, 12.0, 5, drop=0.18, depth=1.9)
        flank_grille(d, "transport", STATIONS_TR, s, 20.0, 4.6, 8.0, 4, drop=0.18, depth=1.5)
        ventral_grille(d, STATIONS_TR, s * 8.0, -38.0, 16.0, 8.0, 5, depth=1.9, face="Heat",
                       matrix=Matrix.Rotation(s * math.radians(104.0), 4, "Y"))

    # Four round nacelles on the aft quarters, bells aft, faired into the flank.
    engines = []
    for s in (-1, 1):
        nacelle(d, STATIONS_TR, s * 14.6, 1.4, 4.2, -14.0, -52.0, engines, seg=22, ribs=3,
                pylon=s * 10.5)
        d.cyl((s * 5.0, -46.0, -6.4), (s * 5.0, -55.0, -6.4), 1.4, 1.2, seg=14, slot="Paint")
        d.cyl((s * 5.0, -55.0, -6.4), (s * 5.0, -56.2, -6.4), 1.4, 1.1, seg=14, slot="Dark")

    hatches(d, STATIONS_TR, rnd, 52.0, -50.0, 7, w_min=1.2, w_max=2.8,
            skip=((34, 8), (-30, 9), (0, 46)), deck=(DECK_TR, 9.4))
    panel_greebles(d, STATIONS_TR, rnd, -52.0, 52.0, 12, "transport",
                    skip=((34, 9), (-30, 10), (0, 44), (-14, 10), (20, 10)))

    parts.append(d.build(bevel=0.07, smooth_angle=32))
    return f.finish(parts, "Transport", col, engines)


# ----------------------------------------------------------------- corvette

# y station, half-beam, deck height, keel depth. 47 m, a stubby rounded pod.
STATIONS_CV = [
    (25.0, 1.6, 2.4, -1.6),
    (22.0, 3.2, 3.2, -2.8),
    (16.0, 4.6, 3.5, -3.6),
    (2.0, 5.0, 3.5, -3.8),
    (-12.0, 4.6, 3.4, -3.4),
    (-18.0, 3.4, 3.0, -2.6),
    (-22.0, 1.8, 2.4, -1.8),
]


def corvette(col):
    """A pressure-vessel pod with a domed nose, a single forward gun, and two round
    nacelles braced clear of the flank so the little hull reads as a stubby body between
    its own engines."""
    rnd = random.Random(23)
    hull = k.Part("Hull", col)
    hull.loft([(y, f.ring_section(w, t, b, SHOULDER["corvette"])) for y, w, t, b in STATIONS_CV], "Paint")
    parts = [hull.build(bevel=0.18, smooth_angle=40)]
    d = k.Part("Detail", col)

    ys, ye, sw, sz = STRAKE["corvette"]
    belt(d, STATIONS_CV, rnd, 20.0, -18.5, "corvette", lo=3.0, hi=4.0, proud=0.3, dark=0.24)
    sheer_strake(d, STATIONS_CV, "corvette", ys, ye, width=sw, z_frac=sz, pitch=3.0)
    keel(d, STATIONS_CV, -18.0, 18.0, w=1.6, h=0.8, lift=0.25)
    warm(d, "corvette", STATIONS_CV, 20.0, -18.0, 7, skip=((8, 7), (0, 6)), width=0.3)

    # Domed nose and a wraparound window band.
    d.cyl((0, 21.0, 0.6), (0, 27.0, 0.6), 2.6, 1.2, seg=20, slot="Paint")
    d.cyl((0, 18.0, -2.4), (0, 18.0, -3.6), 1.9, 1.2, seg=16, slot="Dark")

    d.box((0, 2.0, 3.9), (2.4, 8.0, 1.2), "Paint", chamfer=0.4, taper=0.8)
    d.box((0, 4.4, 4.1), (1.8, 0.2, 0.5), "Windows")
    for s in (-1, 1):
        d.box((s * 1.15, 2.0, 4.1), (0.2, 5.4, 0.5), "Windows")
    d.box((0, 2.0, 4.6), (2.0, 7.0, 0.2), "Dark", chamfer=0.08)
    for yy in f.fan(-1.0, 5.0, 4):
        d.box((0.0, yy, 4.56), (2.1, 0.16, 0.16), "Dark")
    mast(d, 0, 2.0, 4.6, 4.4, spread=0.5)

    mount_pad(d, 0.0, 3.5, 8.0, 1.7, depth=1.2, seg=14)
    f.barbette(d, 0.0, 3.5, 8.0, 1.5, height=0.9, seg=14)

    # Louvred banks amidships and a ventral grille, each in a plenum cut to the hull.
    for s in (-1, 1):
        flank_grille(d, "corvette", STATIONS_CV, s, -4.0, 2.2, 5.0, 4, drop=0.22, depth=1.1,
                     tilt=30.0)
        flank_grille(d, "corvette", STATIONS_CV, s, 12.0, 1.8, 4.0, 3, drop=0.22, depth=0.9,
                     tilt=30.0)
    ventral_grille(d, STATIONS_CV, 0.0, -13.0, 3.0, 5.0, 4, depth=1.1,
                   matrix=Matrix.Rotation(math.radians(180.0), 4, "X"))

    # Two round nacelles braced clear of the pod, faired into the flank so the casing
    # does not simply pass through the shell.
    engines = []
    for s in (-1, 1):
        nacelle(d, STATIONS_CV, s * 6.4, 0.6, 1.7, 4.0, -16.0, engines, seg=16, ribs=3,
                pylon=s * 4.0)

    # Hatches sit on the raised deck plate, not on the bare ridge, and only carry a grab
    # rail in Trim. They were a flat black rectangle with a yellow border on the deck.
    hatches(d, STATIONS_CV, rnd, 16.0, -18.0, 5, w_min=0.7, w_max=1.5, skip=((8, 4), (0, 6)),
            trim_p=0.25, deck=(4.5, 1.15))
    panel_greebles(d, STATIONS_CV, rnd, -18.0, 16.0, 7, "corvette", skip=((8, 5), (0, 8)))

    parts.append(d.build(bevel=0.045, smooth_angle=32))
    return f.finish(parts, "Corvette", col, engines)


# ----------------------------------------------------------------- frigate

# y station, half-beam, deck height, keel depth. 90 m, long and low.
STATIONS_FR = [
    (47.0, 2.0, 3.0, -2.0),
    (43.0, 3.8, 4.0, -3.2),
    (32.0, 5.6, 4.5, -4.6),
    (10.0, 6.2, 4.5, -5.2),
    (-14.0, 6.0, 4.5, -5.0),
    (-32.0, 5.0, 4.3, -4.2),
    (-43.0, 3.2, 3.6, -2.6),
]


def frigate(col):
    """A long low hull slung between two large round nacelles, with a shallow dorsal
    bridge and louvres tucked under the deck edge. Guns fore and aft."""
    rnd = random.Random(31)
    hull = k.Part("Hull", col)
    hull.loft([(y, f.ring_section(w, t, b, SHOULDER["frigate"])) for y, w, t, b in STATIONS_FR], "Paint")
    parts = [hull.build(bevel=0.24, smooth_angle=38)]
    d = k.Part("Detail", col)

    ys, ye, sw, sz = STRAKE["frigate"]
    belt(d, STATIONS_FR, rnd, 40.0, -37.0, "frigate", lo=4.5, hi=6.5, proud=0.4, dark=0.24)
    sheer_strake(d, STATIONS_FR, "frigate", ys, ye, width=sw, z_frac=sz, pitch=4.0)
    keel(d, STATIONS_FR, -38.0, 38.0, w=2.6, h=1.2, lift=0.3)
    warm(d, "frigate", STATIONS_FR, 40.0, -37.0, 11,
         skip=((22, 9), (-18, 9), (0, 18), (28, 8), (-2, 8)), width=0.4)

    d.cyl((0, 43.0, 0.4), (0, 49.0, 0.4), 3.0, 1.4, seg=20, slot="Paint")
    d.cyl((0, 39.0, -3.0), (0, 39.0, -4.4), 2.1, 1.3, seg=16, slot="Dark")

    # Shallow bridge house with a wraparound window band.
    d.box((0, 2.0, 5.7), (6.6, 26.0, 2.4), "Paint", chamfer=1.2, taper=0.88)
    d.box((0, 8.0, 7.6), (4.6, 9.0, 1.9), "Paint", chamfer=0.9, taper=0.76, taper_y=0.84)
    d.box((0, 12.2, 7.7), (3.6, 0.2, 0.7), "Windows")
    for s in (-1, 1):
        d.box((s * 2.2, 8.0, 7.7), (0.2, 6.0, 0.7), "Windows")
        for yy in f.fan(-8.0, 12.0, 5):
            if rnd.random() < 0.35:
                continue
            d.box((s * 3.2, yy, 5.7), (0.2, 1.6, 0.6), "Windows")
            d.box((s * 3.5, yy + 1.2, 5.7), (0.3, 0.4, 2.0), "Dark")
    d.box((0, 2.0, 7.0), (5.2, 22.0, 0.25), "Dark", chamfer=0.08)
    mast(d, 0, 6.0, 8.8, 5.0, spread=0.7)

    for z in (22.0, -18.0):
        mount_pad(d, 0.0, 4.5, z, 2.4, depth=1.4, seg=16)
        f.barbette(d, 0.0, 4.5, z, 2.2, height=1.1, seg=16)

    # Louvred radiator banks under the deck edge, forward of and behind the nacelles.
    for s in (-1, 1):
        flank_grille(d, "frigate", STATIONS_FR, s, 28.0, 3.0, 8.0, 5, drop=0.18, depth=1.3)
        flank_grille(d, "frigate", STATIONS_FR, s, -2.0, 3.0, 8.0, 5, drop=0.18, depth=1.3)
        flank_grille(d, "frigate", STATIONS_FR, s, -30.0, 2.8, 6.0, 4, drop=0.18, depth=1.1)
        for y in (28.0, -2.0):
            hw, top, _ = f.interp(STATIONS_FR, y)
            d.cyl((s * (hw - 0.9), y, top - 2.6), (s * (hw + 0.5), y, top - 0.9), 0.7, 0.7,
                  seg=10, slot="Dark")

    # Two big round nacelles on faired pylons, the frigate's whole character.
    engines = []
    for s in (-1, 1):
        nacelle(d, STATIONS_FR, s * 8.4, -0.4, 2.8, 4.0, -38.0, engines, seg=20, ribs=4,
                pylon=s * 5.2)
        d.cyl((s * 3.2, -34.0, -4.0), (s * 3.2, -41.0, -4.0), 1.1, 0.9, seg=12, slot="Paint")
        d.cyl((s * 3.2, -41.0, -4.0), (s * 3.2, -42.0, -4.0), 1.1, 0.8, seg=12, slot="Dark")

    hatches(d, STATIONS_FR, rnd, 38.0, -36.0, 8, w_min=1.0, w_max=2.6, skip=((22, 7), (-18, 7), (0, 16)))
    panel_greebles(d, STATIONS_FR, rnd, -38.0, 38.0, 10, "frigate",
                    skip=((22, 8), (-18, 8), (0, 18), (28, 6), (-2, 6)))

    parts.append(d.build(bevel=0.055, smooth_angle=32))
    return f.finish(parts, "Frigate", col, engines)


BUILDERS = {
    "corvette": corvette,
    "frigate": frigate,
    "destroyer": destroyer,
    "transport": transport,
    "battleship": battleship,
}
