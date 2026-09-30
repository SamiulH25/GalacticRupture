"""Remnant hulls: oxide red over worn steel, hazard yellow trim.

Remnant: keeled, one unbroken line bow to stern. A narrow hard-chined blade hull; the
dorsal spine runs the full length and rises into a fin that carries the radiators and
sensors; a ventral keel blade stiffens the hull (no guns under it). Built light and fast
by a navy that lost its yards.

The navy lost its yards, so nothing here is thick. Warship classes are daggers: a long
fine bow, a narrow flat deck, hard chines run as a proud flange the whole length, a spine
that never breaks, and a keel under it carrying nothing but structure and sensors. The
transport is the exception that proves the rule — a slab-sided hold, because you cannot
fly an army through a knife edge. Armour is applied as plates laid along the chine;
radiators are always segmented and angled so they read as machinery, never as billboards.

Hardpoints follow Hulls.cs (BuildCorvette, BuildFrigate, BuildDestroyer, BuildTransport,
BuildBattleship); the shared fleet contract owns the deck heights and mount stations.
"""
import math
import random

from mathutils import Matrix, Vector

import fleet as f
import grkit as k

# Barbette radius per shared mount id, so a tub always reads at the scale of its gun.
RADII = {"main_battery": 9.0, "secondary": 4.4, "point_defence": 2.7, "corvette_gun": 1.8}


# --------------------------------------------------------------------------- sections
def blade_flank(w, top, bottom=0.0):
    return [(0.32 * w, top), (0.82 * w, top * 0.45), (w, 0.0)]


def slab_flank(w, top, bottom):
    """Starboard flank of the slab section: a flat side to hang lander cradles off."""
    h = top - bottom
    return [(0.70 * w, top), (w, top - 0.16 * h), (w, bottom + 0.22 * h)]


def slab_section(w, top, bottom):
    """Transport hull: a slab-sided hold, hard chined top and bottom, narrow keel."""
    return f.mirror(slab_flank(w, top, bottom) + [(0.78 * w, bottom), (0.20 * w, bottom)])


def _chord(pts, t):
    """Point, outward normal and lay angle along a flank, `t` running 0 at the deck edge
    to 1 at the chine. The lay angle rotates a plate's thin axis onto the normal and its
    tall axis along the flank, so applied armour follows the hull instead of floating."""
    seg = 0 if t < 0.5 else 1
    u = t * 2 - seg
    ax, az = pts[seg]
    bx, bz = pts[seg + 1]
    dx, dz = bx - ax, bz - az
    ln = math.hypot(dx, dz) or 1.0
    ux, uz = dx / ln, dz / ln
    return ax + dx * u, az + dz * u, -uz, ux, math.atan2(-ux, -uz)


# --------------------------------------------------------------------------- shared grammar
def _belt(d, st, y0, y1, flank, t=0.45, seed=1, out=0.18, gap=0.8, length=(9.0, 15.0),
          thick=0.8, height=(1.0, 5.0)):
    """Armour plates laid along the flank, a fifth of them in `Dark`: the yards are long
    gone, so the field repairs show.

    Standoff, gap and thickness all vary plate to plate, and a `Dark` replacement sits
    deeper than the plate it replaced. A constant standoff reads as tiles stuck to a
    wall, and a repair only reads as a repair if it differs in depth as well as colour.
    """
    rnd = random.Random(seed)
    y = y0
    while y > y1:
        ln = rnd.uniform(*length)
        yc = f.clamp_y(st, y - ln * 0.5)
        w, top, bottom = f.interp(st, yc)
        x, z, nx, nz, ang = _chord(flank(w, top, bottom), t)
        h = min(max(top * 0.34, height[0]), height[1]) * (0.8 + 0.4 * rnd.random())
        for s in (-1, 1):
            dark = rnd.random() < 0.2
            o = out * (0.4 + 1.0 * rnd.random()) * (0.5 if dark else 1.0)
            m = (Matrix.Translation((s * (x + nx * o), yc, z + nz * o))
                 @ Matrix.Rotation(s * ang, 4, "Y"))
            th = thick * (0.75 if dark else 1.0)
            d.box((0, 0, 0), (th, max(0.4, ln - gap * (0.6 + 0.8 * rnd.random())), h),
                  "Dark" if dark else "Paint", matrix=m, chamfer=th * 0.4)
        y -= ln


def _courses(d, st, y0, y1, flank, t, n, out=-0.07, thick=0.22, slot="Dark", skip=()):
    """A recessed seam running fore and aft down the flank at chord position `t`: the
    break between two plate courses.

    Each box is laid on the facet's own lay angle and set `out` into it, so the line
    follows the section instead of cutting a straight chord across a curved flank and
    floating off the upper half of it.
    """
    lo, hi = min(y0, y1), max(y0, y1)
    ln = (hi - lo) / n
    for i in range(n):
        y = f.clamp_y(st, lo + ln * (i + 0.5))
        if any(abs(y - sk) < 7.0 for sk in skip):
            continue
        w, top, bottom = f.interp(st, y)
        x, z, nx, nz, ang = _chord(flank(w, top, bottom), t)
        for s in (-1, 1):
            m = (Matrix.Translation((s * (x + nx * out), y, z + nz * out))
                 @ Matrix.Rotation(s * ang, 4, "Y"))
            d.box((0, 0, 0), (thick, ln * 1.25, thick * 3.2), slot, matrix=m)


def _panels(d, st, y0, y1, flank, n, out=-0.07, thick=0.16, width=0.5,
            t_span=(0.14, 0.9), slot="Dark", skip=()):
    """Transverse recessed panel lines up the flank. Two boxes per station, because a
    flank is faceted: one line drawn straight across a knuckle leaves the hull halfway
    through the stroke. `skip` keeps stations clear so a seam does not cut a turret.
    """
    lo, hi = min(y0, y1), max(y0, y1)
    for i in range(n):
        y = f.clamp_y(st, lo + (hi - lo) * (i + 0.5) / n)
        if any(abs(y - sk) < 7.0 for sk in skip):
            continue
        w, top, bottom = f.interp(st, y)
        pts = flank(w, top, bottom)
        for ta, tb in ((t_span[0], 0.5), (0.5, t_span[1])):
            xa, za, nx, nz, _ = _chord(pts, ta)
            xb, zb, _, _, _ = _chord(pts, tb)
            dx, dz = xb - xa, zb - za
            ln = math.hypot(dx, dz) or 1.0
            ang = math.atan2(dx, dz)
            if math.cos(ang) < 0.0:
                ang += math.pi
            for s in (-1, 1):
                m = (Matrix.Translation((s * ((xa + xb) * 0.5 + nx * out), y,
                                          (za + zb) * 0.5 + nz * out))
                     @ Matrix.Rotation(s * ang, 4, "Y"))
                d.box((0, 0, 0), (thick, width, ln * 1.02), slot, matrix=m)


def _trim_edge(d, st, y0, y1, edge=0.28, width=0.9, proud=0.18):
    """Deck-edge trim band, the waist only.

    This replaces a 0.1-0.3 m tube running bow to stern on posts. At ship scale that is
    a hairline: it read as a painted racing stripe, and on the destroyer as scaffolding
    over the working deck. A band with real width casts a shadow line and stops after
    the waist, so trim marks the edge instead of outlining the ship.
    """
    f.strake(d, st, edge, width * 0.35, f.clamp_y(st, y0), f.clamp_y(st, y1),
             width=width, proud=proud, slot="Trim", seg=6)


def _hatch(d, st, y, x_frac=0.0, w=3.0, length=3.4, slot="Trim", rim="Dark"):
    """A marked hatch: a recessed rim with a trim coaming around it. Trim budget goes on
    edges and hatches, so this is where most of it now goes."""
    w_st, top, _ = f.interp(st, y)
    d.box((x_frac * w_st, y, top + 0.12), (w, length, 0.4), rim, chamfer=0.12)
    for s in (-1, 1):
        d.box((x_frac * w_st + s * w * 0.5, y, top + 0.3), (0.35, length * 1.05, 0.5),
              slot, chamfer=0.1)
        d.box((x_frac * w_st, y + s * length * 0.5, top + 0.3), (w * 1.05, 0.35, 0.5),
              slot, chamfer=0.1)


def _slot(d, centre, w, h, face="y", depth=0.26, frame=0.22):
    """A lit window set into a dark raised frame.

    Applied `Windows` boxes sat proud of the surface and read as white rectangles
    painted on the hull. Backing each one with four thin dark bars and pulling the glass
    in behind them gives it a reveal, so it reads as glazing in a recess.
    """
    x, y, z = centre
    d.box((x, y, z), (w if face == "y" else depth, depth, h), "Windows")
    for s in (-1, 1):
        if face == "y":
            d.box((x, y + s * (h * 0.5 + frame * 0.5), z),
                  (w + frame * 2.0, depth * 1.4, frame), "Dark", chamfer=0.06)
        else:
            d.box((x + s * (w * 0.5 + frame * 0.5), y, z),
                  (frame, depth * 1.4, h), "Dark", chamfer=0.06)
        d.box((x, y, z + s * (h * 0.5 + frame * 0.5)),
              (w + frame * 2.0 if face == "y" else w, depth * 1.4, frame), "Dark",
              chamfer=0.06)


def _cargo(d, yc, crest):
    """One cargo module on the convoy spine.

    These were plain grey boxes with two painted yellow bars down the roof, which read
    as scenery. Corner castings, recessed door leaves with hinge barrels and a lock bar,
    and lashing bars down to the spine are what make a container read as a container.
    """
    roof = crest + 4.0
    d.box((0, yc, crest + 2.0), (9.4, 18.0, 4.0), "Paint", chamfer=1.0, taper=0.94)
    d.box((0, yc, roof - 0.1), (7.6, 14.6, 0.4), "Dark", chamfer=0.12)
    for sx in (-1, 1):
        for sy in (-1, 1):
            d.box((sx * 4.55, yc + sy * 8.5, roof - 0.1), (1.5, 1.5, 0.9), "Dark",
                  chamfer=0.25)
    for s in (-1, 1):
        for k in (-1, 1):
            d.box((s * 4.72, yc + k * 4.5, crest + 2.0), (0.34, 8.0, 3.2), "Dark",
                  chamfer=0.22)
            for h in (-1, 1):
                d.cyl((s * 4.95, yc + k * 4.5 + h * 3.2, crest + 0.85),
                      (s * 4.95, yc + k * 4.5 + h * 3.2, crest + 3.15),
                      0.26, seg=8, slot="Trim" if h > 0 else "Dark")
        d.cyl((s * 5.02, yc - 8.4, crest + 1.4), (s * 5.02, yc + 8.4, crest + 1.4),
              0.2, seg=6, slot="Dark")
    for k in (-1, 0, 1):
        d.box((0, yc + k * 6.2, roof + 0.45), (10.4, 0.5, 0.5), "Dark", chamfer=0.14)
        for sx in (-1, 1):
            d.box((sx * 5.0, yc + k * 6.2, crest + 3.25), (0.45, 0.45, 2.4), "Dark")
    for sy in (-1, 1):
        d.box((0, yc + sy * 7.4, roof + 0.06), (8.4, 1.0, 0.18), "Trim")


def _strake(d, st, y0, y1, out, thick, n=18, slot="Paint"):
    """The shoulder chine run as a proud flange: an unbroken line from stem to stern that
    gives the blade hull its knife edge in every view."""
    ys = []
    for i in range(n + 1):
        y = y0 + (y1 - y0) * i / n
        w, _, _ = f.interp(st, y)
        x = w * 0.99  # the widest point of the section is its chine, on every Remnant hull
        ys.append((y, f.mirror([(x, thick), (x + out, 0.0), (x + out * 0.5, -thick * 1.5),
                                (x, -thick)])))
    d.loft(ys, slot)


def _dorsal(d, st, prof, base=0.45, n=20, slot="Paint"):
    """The spine. `prof` is a (y, half, h, fin_half) control list: the structure stands
    `h` metres above the deck at that station, with a narrow fin above the shoulder.

    Returns the sampled rings so the caller can break the crest up. The old crest rail
    was a 0.1-0.3 m Trim tube running the whole length of the fin, which is exactly the
    wire trim this pass is removing, so it is gone rather than thinned.
    """
    ys = []
    for i in range(n + 1):
        y = f.clamp_y(st, prof[0][0] + (prof[-1][0] - prof[0][0]) * i / n)
        _, top, _ = f.interp(st, y)
        half, h, fh = f.interp(prof, y)
        z0 = top - 0.4
        z1 = top + base + h * 0.66
        z2 = top + base + h
        ys.append((y, f.mirror([(half, z0), (half * 0.9, z1), (min(fh, half * 0.85), z2)])))
    d.loft(ys, slot)
    return ys


def _seam_angles(dx, dz):
    """Lay angle for a box on a flank facet, plus the facet's outward normal.

    The box's thin axis goes onto the normal and its tall axis along the facet, so a
    seam follows the section instead of cutting a straight chord across it and floating
    off the upper half. The half-pi nudge keeps the normal pointing outboard whichever
    way the facet was wound.
    """
    ang = math.atan2(dx, dz)
    if math.cos(ang) < 0.0:
        ang += math.pi
    return ang, math.cos(ang), -math.sin(ang)


def _spine_seams(d, ys, every=2, out=-0.08, thick=0.24, slot="Dark"):
    """Recessed seams across the dorsal spine, from the rings `_dorsal` sampled.

    The spine is the dominant form on an escort and the dominant band on a warship, and
    a smooth lofted tube reads as a plastic pipe however good the section is. Each seam
    is a thin dark band bedded into the side and shoulder facets, so the crest reads as
    plating with visible joints rather than a moulded extrusion.
    """
    for i in range(0, len(ys) - 1, every):
        (ya, pa), (yb, _) = ys[i], ys[i + 1]
        seg = abs(yb - ya) * 1.1
        ym = (ya + yb) * 0.5
        for facet in (1, 2):  # the starboard side and shoulder facets of the ring
            xa, za = pa[facet]
            xb, zb = pa[facet + 1]
            ang, nx, nz = _seam_angles(xb - xa, zb - za)
            for s in (-1, 1):
                m = (Matrix.Translation((s * ((xa + xb) * 0.5 + nx * out), ym,
                                          (za + zb) * 0.5 + nz * out))
                     @ Matrix.Rotation(s * ang, 4, "Y"))
                d.box((0, 0, 0), (thick, seg, thick * 2.4), slot, matrix=m)


def _dorsal_top(st, prof, y, base=0.45):
    """Height of the spine crest at station `y`, so detail can sit on it."""
    _, top, _ = f.interp(st, f.clamp_y(st, y))
    return top + base + f.interp(prof, y)[1]


def _keel(d, st, y0, y1, depth, half, n=16, slot="Dark"):
    """Ventral keel blade: what stops a light hull folding under its own guns, and where
    the sensor suite lives. Nothing is mounted under it."""
    ys = []
    for i in range(n + 1):
        y = f.clamp_y(st, y0 + (y1 - y0) * i / n)
        _, _, bottom = f.interp(st, y)
        dep = depth * f.sin_ramp(i, n)
        ys.append((y, [(-half, bottom + 0.5), (half, bottom + 0.5),
                       (0.42 * half, bottom - dep), (-0.42 * half, bottom - dep)]))
    d.loft(ys, slot)


def _radiators(d, y0, y1, z, span, vanes, rake=34.0, slats=3, root=True, seg_y=None):
    """A radiator bank laid flat against the spine and raked aft.

    These used to stand on end and cant outboard at a dihedral, which read as a crown
    of knife blades -- decorative fins, not heat exchangers. Lying the plates flat in a
    stepped plenum, each raked aft so the comb shadows itself, reads as machinery from
    every angle and stops fighting the spine's line for the silhouette.
    """
    lo, hi = min(y0, y1), max(y0, y1)
    pitch = (hi - lo) / vanes
    sy = seg_y or pitch * 1.05
    rk = math.radians(rake)
    step = 0.62
    if root:
        # The bank is bedded into a plenum rather than balanced on the spine, and the
        # risers show where the coolant comes from.
        d.box((0, (lo + hi) * 0.5, z - 0.7), (span * 1.02, abs(hi - lo) + pitch, 1.8),
              "Dark", chamfer=0.5)
        for yy in (lo + pitch * 0.35, hi - pitch * 0.35):
            d.cyl((0, yy, z - 3.4), (0, yy, z - 0.9), span * 0.11, span * 0.09,
                  seg=8, slot="Dark")
    for i in range(vanes):
        yc = lo + pitch * (i + 0.5)
        for j in range(slats):
            m = (Matrix.Translation((0, yc, z + step * (j + 0.6)))
                 @ Matrix.Rotation(-rk, 4, "X"))
            d.box((0, 0, 0), (span, sy, 0.34), "Dark", matrix=m, chamfer=0.1)
            d.box((0, 0, 0.24), (span - 0.55, sy - 0.6, 0.14), "Heat", matrix=m)
    # One trim edge on the after end of the comb: a marked fitting, not an outlined one.
    d.box((0, hi - pitch * 0.5, z + step * (slats + 0.6)), (span * 0.98, 0.3, 0.2), "Trim")



def _greebles(d, st, y0, y1, n, rnd, avoid=(), scale=1.0, flank=blade_flank, deck=0.3):
    """Fittings laid on the real surface: hatches and vents on the deck, condensers and
    winches down the flank. Nothing floats clear of the hull."""
    for _ in range(n):
        y = f.clamp_y(st, rnd.uniform(y0, y1))
        if any(abs(y - a) < 8.0 for a in avoid):
            continue
        w, top, bottom = f.interp(st, y)
        if rnd.random() < 0.5:
            hw = rnd.uniform(0.12, 0.26) * w
            d.box((rnd.uniform(-(deck - hw / w), deck - hw / w) * w, y, top + 0.04),
                  (hw, rnd.uniform(1.4, 4.6) * scale, rnd.uniform(0.2, 0.6)),
                  "Dark" if rnd.random() < 0.4 else "Paint", chamfer=0.06)
        else:
            x, z, nx, nz, ang = _chord(flank(w, top, bottom), rnd.uniform(0.2, 0.85))
            s = rnd.choice((-1, 1))
            m = (Matrix.Translation((s * (x + nx * 0.06), y, z + nz * 0.06))
                 @ Matrix.Rotation(s * ang, 4, "Y"))
            d.box((0, 0, 0), (0.4, rnd.uniform(1.2, 3.6) * scale, rnd.uniform(0.5, 1.7)),
                  "Dark" if rnd.random() < 0.4 else "Paint", matrix=m, chamfer=0.1)


def _lights(d, st, y0, y1, n, scale=1.0):
    """Hull lights: a run of slits down each flank, low on the chine."""
    for i in range(n + 1):
        y = f.clamp_y(st, y0 + (y1 - y0) * i / n)
        w, top, _ = f.interp(st, y)
        if w < 1.2:
            continue
        x, z, _, _, _ = _chord(blade_flank(w, top), 0.86)
        for s in (-1, 1):
            d.box((s * (x + 0.06), y, z + 0.1), (0.1 * scale, 0.8 * scale, 0.3 * scale),
                  "Windows")


def _hardpoints(d, cls, st, roof=None, slot="Paint"):
    """An armoured tub under every hardpoint the contract defines, plus a plinth wherever
    a mount stands proud of the surface beneath it: superfiring main guns, tubs on the
    bridge wing, sponsons out on the flanks."""
    for mount, x, y_up, z, _yaw in f.CONTRACT[cls]["mounts"]:
        r = RADII[mount]
        base = (roof or {}).get(z, f.interp(st, z)[1])
        if y_up - base > 0.5:
            d.cyl((x, z, base - 1.0), (x, z, y_up - 0.8), r * 1.14, r * 1.08, seg=28, slot=slot)
        f.barbette(d, x, y_up, z, r, height=1.0 + max(0.0, y_up - base) * 0.9, seg=24)


# --------------------------------------------------------------------------- escorts
# Keeled blade hull with a full-length spine, fin, ventral keel, chin-mounted guns.
def remnant_blade(col, name, st, spine_peak, spine_y, fin_len, gun_ys, keel_depth,
                  engines_spec, scale, cls):
    hull = k.Part("Hull", col)
    hull.loft([(y, f.blade_section(w, t, b)) for y, w, t, b in st], "Paint")
    parts = [hull.build(bevel=0.08 * scale, bevel_segments=1, smooth_angle=15)]
    d = k.Part("Detail", col)

    bow, stern = st[1][0], st[-1][0]
    _strake(d, st, bow, stern, 0.55 * scale, 0.2 * scale, n=12)
    _belt(d, st, bow, stern, blade_flank, t=0.42, seed=len(gun_ys) * 7 + 3, out=0.1 * scale,
          gap=0.3 * scale, length=(3.0 * scale, 5.5 * scale), thick=0.5 * scale,
          height=(0.5 * scale, 1.8 * scale))
    # Plate breakup: a course seam low on the flank and transverse panel lines above it,
    # so the smooth blade side stops reading as an extruded profile.
    _courses(d, st, bow - 2 * scale, stern + 2 * scale, blade_flank, 0.8,
             max(6, int(56 / scale)), thick=0.16 * scale)
    _panels(d, st, bow - 2 * scale, stern + 2 * scale, blade_flank, max(5, int(34 / scale)),
            thick=0.12 * scale, width=0.42 * scale, skip=gun_ys)
    # Trim: one band along the deck edge over the waist plus two marked hatches. The old
    # bow-to-stern rail on posts was a hairline at this scale and read as scaffolding.
    _trim_edge(d, st, bow * 0.46, stern * 0.72, edge=0.28, width=0.8 * scale,
               proud=0.14 * scale)
    _hatch(d, st, bow * 0.16, w=2.2 * scale, length=2.6 * scale)
    _hatch(d, st, stern * 0.55, w=1.8 * scale, length=2.2 * scale)

    # The spine: one unbroken line, low over the guns, rising to the fin.
    def crest(y):
        """Fin crest height above the deck at `y`: the fin profile in one place."""
        rise = max(0.0, 1.0 - abs(y - spine_y) / fin_len)
        return 0.6 * scale + spine_peak * rise ** 1.6
    stations = []
    n = 14
    for i in range(n + 1):
        y = f.clamp_y(st, bow + (stern - bow) * i / n)
        _, top, _ = f.interp(st, y)
        rise = max(0.0, 1.0 - abs(y - spine_y) / fin_len)
        height = crest(y)
        half = 0.35 * scale * (1.0 - 0.4 * rise)
        stations.append((y, [(-half, top - 0.2), (half, top - 0.2), (half * 0.4, top + height),
                             (-half * 0.4, top + height)]))
    d.loft(stations, "Paint")
    # The spine is the escort's dominant form and a smooth tube read as a plastic pipe, so
    # it gets the same recessed seams as the flank. The crest rail is gone rather than
    # thinned: 0.12 m of tube along the whole fin is a wire, not a strake.
    _spine_seams(d, stations, every=2, out=-0.05 * scale, thick=0.15 * scale)

    # Radiators lie flat on the fin crest, raked aft and bedded in a plenum. They used to
    # stand on end and cant outboard, which read as a crown of knife blades rather than
    # heat exchangers, and they fought the spine for the silhouette.
    ry = spine_y - fin_len * 0.45
    _radiators(d, ry, ry + fin_len * 0.9,
               f.interp(st, spine_y)[1] + crest(spine_y) - 0.7 * scale,
               0.95 * scale, 4, rake=30.0, slats=3)

    # Ventral keel blade: stiffener and sensor array, no guns.
    _keel(d, st, st[2][0], st[-2][0], keel_depth, 0.3 * scale, n=14)

    _hardpoints(d, cls, st)

    # Bridge slit and flank lights.
    _, top, _ = f.interp(st, gun_ys[0] - 6 * scale)
    d.box((0, gun_ys[0] - 5 * scale, top + 0.1), (1.6 * scale, 0.2, 0.5 * scale), "Windows")
    _lights(d, st, st[2][0], st[-2][0], 8, scale)

    rnd = random.Random(11 + len(gun_ys))
    _greebles(d, st, bow - 3 * scale, stern + 2 * scale, int(12 * scale), rnd, gun_ys, scale)

    engines = []
    y_end = st[-1][0]
    d.box((0, y_end - 0.6, 0.0),
          (st[-1][1] * 1.6, 1.4, (st[-1][2] - st[-1][3]) * 0.9), "Dark", chamfer=0.4 * scale)
    for x, z, r in engines_spec:
        f.nozzle(d, x, z, r, y_end - 1.2, engines, ribs=1)
    parts.append(d.build(bevel=0.05 * scale, bevel_segments=1, smooth_angle=30))
    return f.finish(parts, name, col, engines)


# y station, half-beam, deck height, keel depth. 90 m, guns fore and aft.
ST_FR = [(47.0, 0.3, 2.0, -1.0), (38.0, 3.5, 4.2, -4.0), (12.0, 7.0, 4.5, -8.0),
         (-20.0, 6.5, 4.5, -8.0), (-38.0, 5.0, 4.0, -5.0), (-43.0, 4.6, 3.6, -4.2)]


def frigate(col):
    """A raider, not a warship: two chin guns on a bare spine, radiators laid flat along
    the fin crest, and nothing under the keel at all."""
    return remnant_blade(col, "Frigate", ST_FR, spine_peak=9.0, spine_y=-4.0, fin_len=16.0,
                         gun_ys=(22.0, -18.0), keel_depth=6.0,
                         engines_spec=((0.0, -0.6, 3.0), (-4.0, 1.0, 1.7), (4.0, 1.0, 1.7)),
                         scale=1.0, cls="frigate")


# y station, half-beam, deck height, keel depth. 47 m, one gun forward.
ST_CV = [(25.0, 0.2, 1.4, -0.5), (18.0, 2.4, 2.8, -2.4), (2.0, 4.4, 3.2, -4.0),
         (-15.0, 4.1, 3.0, -3.5), (-21.0, 3.7, 2.6, -3.0), (-22.0, 3.5, 2.4, -2.8)]


def corvette(col):
    """Half a frigate with everything non-structural taken off it."""
    return remnant_blade(col, "Corvette", ST_CV, spine_peak=5.0, spine_y=-9.0, fin_len=10.0,
                         gun_ys=(8.0,), keel_depth=3.2,
                         engines_spec=((-2.3, 0.0, 1.7), (2.3, 0.0, 1.7)),
                         scale=0.55, cls="corvette")


# --------------------------------------------------------------------------- destroyer
# y station, half-beam, deck height, keel depth. 148 m, one long forebody.
ST_DD = [(76.0, 0.35, 3.2, -0.6), (70.0, 2.2, 4.6, -3.0), (58.0, 5.2, 5.6, -5.6),
         (40.0, 8.0, 6.0, -7.0), (4.0, 9.4, 6.0, -7.4), (-40.0, 8.8, 6.0, -7.0),
         (-60.0, 7.4, 5.6, -6.0), (-72.0, 6.2, 5.2, -5.0)]

# (y, spine half-width, height above deck, fin half-width).
PROF_DD = [(70.0, 0.9, 0.6, 0.5), (52.0, 1.9, 1.6, 0.9), (34.0, 2.6, 3.2, 1.5),
           (4.0, 3.0, 8.6, 1.9), (-26.0, 3.0, 8.6, 1.9), (-48.0, 2.6, 3.4, 1.4),
           (-66.0, 1.6, 1.4, 0.8)]


def destroyer(col):
    """The frigate's idea at 148 m: one unbroken spine with a tall radiator fin standing
    over the waist, a keel blade the whole length, and a pair of flank blisters carrying
    the point defence. Chin guns fore and aft; four bells and two thrusters in the stern
    block, so she accelerates hard and turns inside her own length."""
    rnd = random.Random(23)
    hull = k.Part("Hull", col)
    hull.loft([(y, f.blade_section(w, t, b)) for y, w, t, b in ST_DD], "Paint")
    parts = [hull.build(bevel=0.16, bevel_segments=1, smooth_angle=15)]
    d = k.Part("Detail", col)

    _strake(d, ST_DD, 70.0, -66.0, 1.1, 0.5, n=18)
    _belt(d, ST_DD, 66.0, -66.0, blade_flank, t=0.45, seed=5, out=0.2, gap=0.7,
          length=(6.0, 10.0), thick=0.8, height=(1.2, 3.2))
    # Trim: one deck-edge band over the waist and two marked hatches. The old pair of
    # 0.13 m rails on 0.16 m posts ran the whole 138 m and read as painted scaffolding
    # over the working deck; the spine crest rail was a third wire on top of that.
    _trim_edge(d, ST_DD, 34.0, -58.0, edge=0.28, width=1.3, proud=0.26)
    _hatch(d, ST_DD, 52.0, w=4.2, length=5.0)
    _hatch(d, ST_DD, -52.0, w=3.4, length=4.0)

    # Plate breakup: a course seam low on the flank and transverse panel lines above,
    # skipping the three turret stations so no seam cuts a barbette.
    _courses(d, ST_DD, 66.0, -66.0, blade_flank, 0.82, 22, thick=0.3,
             skip=(40.0, 8.0, -36.0))
    _panels(d, ST_DD, 66.0, -66.0, blade_flank, 18, thick=0.22, width=0.7,
            skip=(40.0, 8.0, -36.0))

    _spine_seams(d, _dorsal(d, ST_DD, PROF_DD, base=0.5, n=22), every=2, thick=0.3)

    _keel(d, ST_DD, 58.0, -60.0, 6.4, 0.7, n=18)

    # Radiator bank: flat plates raked aft over the waist, bedded in a plenum on the fin
    # rather than splayed off it, so it reads as heat exchange and not as decoration.
    _radiators(d, -8.0, 20.0, _dorsal_top(ST_DD, PROF_DD, 6.0) - 2.2, 4.6, 5, rake=32.0)

    # Flank blisters under the point defence, raked back and canted outboard.
    for s in (-1, 1):
        m = (Matrix.Translation((s * 8.2, 8.0, 5.0))
             @ Matrix.Rotation(-s * math.radians(16), 4, "Y"))
        d.box((0, 0, 0), (5.0, 13.0, 4.4), "Paint", matrix=m, chamfer=1.1, taper=0.82)
        d.box((0, 0, 2.4), (4.0, 11.0, 0.6), "Dark", matrix=m, chamfer=0.2)
        d.box((s * 10.4, 1.4, 4.2), (1.2, 5.0, 0.5), "Trim", chamfer=0.1)
        for j in range(3):
            d.box((s * 9.0, 20.0 - j * 5.0, 1.1), (1.6, 3.4, 1.4), "Dark", chamfer=0.25)

    # Bridge: a low, hard-shouldered block on the forward spine, slotted for a look-out.
    d.box((0, 26.0, 7.4), (7.2, 15.0, 4.6), "Paint", chamfer=1.3, taper=0.72, taper_y=0.8)
    _slot(d, (0, 19.75, 8.4), 5.2, 0.85, face="y", depth=0.3, frame=0.26)
    for yy in (23.0, 26.5, 30.0):
        for s in (-1, 1):
            _slot(d, (s * 2.72, yy, 9.2), 1.8, 0.55, face="x", depth=0.3, frame=0.16)
    d.box((0, 30.5, 8.2), (3.4, 4.0, 2.2), "Dark", chamfer=0.6)
    d.cyl((0, 30.0, 9.3), (0, 30.0, 13.0), 0.16, 0.1, seg=6, slot="Dark")
    d.box((0, 30.0, 13.2), (0.3, 0.3, 0.3), "Heat")
    # Mast and sensor yard on the fin.
    for zz, span in ((12.0, 5.0), (15.0, 3.2)):
        d.box((0, -6.0, zz), (span, 0.4, 0.4), "Dark")
    d.box((0, -6.0, 16.6), (2.4, 3.0, 2.2), "Paint", chamfer=0.5)

    # Bow anchors, ventral intakes and a bow sensor blister.
    d.box((0, 52.0, -6.4), (2.4, 22.0, 1.4), "Dark", chamfer=0.3)
    for s in (-1, 1):
        d.cyl((s * 3.4, 58.0, 0.6), (s * 5.6, 50.0, 0.6), 0.7, 0.7, seg=8, slot="Dark")
    d.cyl((0, 46.0, -1.0), (0, 46.0, -3.4), 3.0, 1.6, seg=16, slot="Dark")

    _hardpoints(d, "destroyer", ST_DD)
    _lights(d, ST_DD, 58.0, -60.0, 14)
    _greebles(d, ST_DD, 64.0, -66.0, 26, rnd, (40.0, 8.0, -36.0))

    engines = []
    d.box((0, -74.0, -0.6), (13.0, 4.0, 12.0), "Dark", chamfer=1.0, taper=0.92)
    for x, z, r in ((0.0, -1.0, 3.6), (-5.0, 1.4, 2.6), (5.0, 1.4, 2.6),
                    (-2.6, -5.0, 1.7), (2.6, -5.0, 1.7)):
        f.nozzle(d, x, z, r, -76.0, engines, ribs=2)
    parts.append(d.build(bevel=0.1, bevel_segments=1, smooth_angle=30))
    return f.finish(parts, "Destroyer", col, engines)


# --------------------------------------------------------------------------- transport
# y station, half-beam, deck height, keel depth. 117 m, a blunt bow that takes a ramp.
ST_TR = [(59.0, 1.6, 4.6, -1.0), (54.0, 6.5, 7.2, -5.0), (46.0, 12.5, 8.0, -8.0),
         (10.0, 14.0, 8.0, -10.0), (-40.0, 14.0, 8.0, -10.0), (-52.0, 12.5, 7.6, -9.0),
         (-58.0, 10.5, 7.0, -7.5)]
DECK_TR = f.CONTRACT["transport"]["deck"]

# (y, spine half-width, height above deck, fin half-width). Dips to nothing at the two
# point-defence stations so the tubs stand clear of the convoy deck.
PROF_TR = [(54.0, 2.2, 1.2, 0.8), (46.0, 4.0, 2.0, 1.2), (36.0, 5.2, 0.3, 2.2),
           (22.0, 5.4, 1.0, 2.6), (4.0, 5.6, 2.4, 2.6), (-16.0, 5.6, 2.4, 2.6),
           (-30.0, 5.2, 0.3, 2.2), (-42.0, 4.2, 1.6, 1.4), (-52.0, 2.8, 1.0, 0.9)]


def transport(col):
    """The one Remnant hull that is not a knife. A slab-sided hold on a hard chine with a
    keel blade deep enough to swallow the loading ramp, landers racked in three bays down
    each flank, and the army riding the convoy deck amidships where the spine carries it.
    Blunt, deep and unmistakable in plan against the other four."""
    rnd = random.Random(31)
    hull = k.Part("Hull", col)
    hull.loft([(y, slab_section(w, t, b)) for y, w, t, b in ST_TR], "Paint")
    parts = [hull.build(bevel=0.2, bevel_segments=1, smooth_angle=15)]
    d = k.Part("Detail", col)

    _belt(d, ST_TR, 50.0, -54.0, slab_flank, t=0.5, seed=9, out=0.24, gap=1.0,
          length=(7.0, 12.0), thick=1.0, height=(1.6, 4.0))
    # Trim: one deck-edge band over the waist and a marked bow and stern hatch. The old
    # 0.16 m rail on 0.2 m posts ran the full 104 m like a trawler handrail.
    _trim_edge(d, ST_TR, 34.0, -40.0, edge=0.66, width=1.6, proud=0.3)
    _hatch(d, ST_TR, 48.0, w=5.0, length=5.6)
    _hatch(d, ST_TR, -46.0, w=4.4, length=4.6)

    # Plate breakup on the slab side, which is the one truly flat wall in the faction.
    _courses(d, ST_TR, 50.0, -54.0, slab_flank, 0.62, 16, thick=0.34,
             skip=(34.0, -30.0, 42.0))
    _panels(d, ST_TR, 50.0, -54.0, slab_flank, 20, thick=0.26, width=0.8,
            skip=(34.0, -30.0, 42.0))
    # Chine flange, run just under the deck edge where the slab turns over.
    _strake(d, ST_TR, 52.0, -54.0, 1.4, 0.7, n=18)

    _spine_seams(d, _dorsal(d, ST_TR, PROF_TR, base=0.3, n=24), every=2, thick=0.3)
    for yc in (16.0, 0.0, -18.0):
        _cargo(d, yc, _dorsal_top(ST_TR, PROF_TR, yc, base=0.3))

    # Convoy deck: three cargo modules on the spine, clear of both gun stations.
    # Lander cradles down both flanks, three bays a side, slung off the slab.
    for s in (-1, 1):
        for yc in (18.0, -4.0, -26.0):
            d.box((s * 14.2, yc, 3.4), (6.0, 20.0, 8.4), "Paint", chamfer=1.6, taper=0.84)
            d.box((s * 16.6, yc, 3.4), (2.4, 17.0, 6.4), "Dark", chamfer=0.9)
            for q in (-1, 1):
                d.box((s * 14.2 + q * 1.7, yc, 7.8), (0.5, 18.0, 0.45), "Trim")
            for q in range(3):
                d.box((s * 13.4, yc - 6.0 + q * 6.0, 4.4), (1.0, 3.6, 4.4), "Dark", chamfer=0.3)
            d.box((s * 12.6, yc, 0.2), (0.8, 19.0, 0.8), "Trim")
            d.box((s * 17.8, yc, 4.6), (0.5, 4.0, 1.2), "Windows")
    # Radiators: two banks laid flat on the spine and raked aft, bedded in plenum boxes.
    # They used to stand on end and splay off the fin, which read as blades.
    _radiators(d, -12.0, 16.0, DECK_TR + 2.0, 4.4, 5, rake=30.0)
    _radiators(d, -38.0, -26.0, DECK_TR + 2.0, 4.4, 3, rake=30.0, root=False)

    # Bridge: a low armoured block forward, on the hull deck clear of the spine.
    d.box((0, 42.0, DECK_TR + 2.4), (12.0, 13.0, 5.2), "Paint", chamfer=1.6, taper=0.76)
    _slot(d, (0, 35.85, DECK_TR + 3.4), 8.6, 0.95, face="y", depth=0.34, frame=0.28)
    for yy in (38.0, 42.0, 46.0):
        for s in (-1, 1):
            _slot(d, (s * 4.5, yy, DECK_TR + 3.2), 2.2, 0.7, face="x", depth=0.34,
                  frame=0.18)
    d.box((0, 48.0, DECK_TR + 2.0), (6.0, 4.0, 3.0), "Dark", chamfer=0.7)
    d.cyl((0, 48.0, DECK_TR + 3.5), (0, 48.0, DECK_TR + 7.5), 0.2, 0.12, seg=6, slot="Dark")
    d.box((0, 48.0, DECK_TR + 7.7), (0.35, 0.35, 0.35), "Heat")

    # Loading ramp: a slotted plate raked down the bow, with a keel housing under it.
    # The tilt composes after the placement, so the plate pivots about its own seat.
    ramp = Matrix.Rotation(math.radians(-26), 4, "X")
    d.box((0, 0, 0), (9.0, 14.0, 0.9), "Trim",
          matrix=Matrix.Translation((0.0, 51.0, DECK_TR - 1.0)) @ ramp, chamfer=0.25)
    for s in (-1, 1):
        d.box((0, 0, 0), (0.6, 14.0, 1.6), "Dark",
              matrix=Matrix.Translation((s * 3.2, 51.0, DECK_TR - 1.6)) @ ramp, chamfer=0.2)
    d.box((0, 48.0, DECK_TR - 3.4), (7.0, 6.0, 2.6), "Dark", chamfer=0.5, taper_y=0.5)

    # Keel blade: deeper and broader than a warship's, because it is the ramp housing.
    _keel(d, ST_TR, 44.0, -50.0, 8.0, 1.6, n=20)
    d.box((0, 26.0, -12.0), (6.0, 34.0, 2.0), "Dark", chamfer=0.5)
    d.cyl((0, 44.0, -6.0), (0, 44.0, -9.4), 3.4, 2.0, seg=16, slot="Dark")

    _hardpoints(d, "transport", ST_TR)
    _lights(d, ST_TR, 44.0, -50.0, 14)
    _greebles(d, ST_TR, 50.0, -54.0, 26, rnd, (34.0, -30.0, 42.0), flank=slab_flank, deck=0.6)

    engines = []
    d.box((0, -60.0, -0.5), (22.0, 4.0, 16.0), "Dark", chamfer=1.2, taper=0.94)
    for x, z, r in ((-6.0, 2.0, 3.2), (6.0, 2.0, 3.2), (-6.0, -5.0, 3.2), (6.0, -5.0, 3.2),
                    (0.0, 3.4, 2.0)):
        f.nozzle(d, x, z, r, -62.0, engines, ribs=2)
    parts.append(d.build(bevel=0.12, bevel_segments=1, smooth_angle=30))
    return f.finish(parts, "Transport", col, engines)


# --------------------------------------------------------------------------- battleship
# y station, half-beam, deck height, keel depth. 354 m: a dagger, all forebody.
ST_BS = [(182.0, 0.5, 6.0, -0.5), (176.0, 3.2, 8.5, -4.0), (162.0, 7.0, 10.6, -8.5),
         (140.0, 12.0, 11.8, -13.0), (112.0, 18.0, 12.0, -15.5), (40.0, 23.5, 12.0, -16.5),
         (-40.0, 24.0, 12.0, -16.5), (-110.0, 23.0, 12.0, -15.5), (-150.0, 20.5, 11.8, -14.0),
         (-172.0, 17.0, 11.0, -12.0)]
DECK_BS = f.CONTRACT["battleship"]["deck"]

# (y, spine half-width, height above deck, fin half-width). Narrow, so the deck still
# reads as a blade; it flares only at the two superfiring gun bays, and stands tall fore
# and aft where the fin carries the masts.
PROF_BS = [(172.0, 1.0, 0.8, 0.5), (150.0, 2.2, 1.6, 0.9), (128.0, 3.4, 3.0, 1.4),
           (110.0, 4.4, 4.6, 1.8), (90.0, 5.4, 4.8, 2.0), (72.0, 8.6, 3.4, 2.6),
           (40.0, 6.6, 3.4, 3.0), (-30.0, 6.6, 3.4, 3.0), (-66.0, 8.6, 3.4, 2.6),
           (-98.0, 5.4, 4.8, 2.0), (-130.0, 4.0, 3.4, 1.6), (-160.0, 2.4, 1.8, 0.9)]


def battleship(col):
    """A very long knife with guns on it. Four main turrets in superfiring pairs along a
    spine that never breaks, six secondary tubs on outrigger sponsons, a bridge wing and
    citadel amidships, three bells and two manoeuvring in a tight stern block, and a
    ventro-fin under the waist to keep her from rolling. Built light: the answer to the
    slug that nearly killed her, and the whole reason she exists."""
    rnd = random.Random(7)
    hull = k.Part("Hull", col)
    hull.loft([(y, f.blade_section(w, t, b)) for y, w, t, b in ST_BS], "Paint")
    parts = [hull.build(bevel=0.3, bevel_segments=1, smooth_angle=15)]
    d = k.Part("Detail", col)

    _strake(d, ST_BS, 176.0, -170.0, 2.0, 1.0, n=26)
    _belt(d, ST_BS, 172.0, -170.0, blade_flank, t=0.5, seed=17, out=0.45, gap=1.4,
          length=(14.0, 21.0), thick=1.6, height=(2.4, 6.5))
    # Waist only, like every other Remnant hull: trim marks the edge, it does not outline
    # the ship. Wider than the escorts' -- a 350 m hull wants a strake you can read at range.
    _trim_edge(d, ST_BS, 46.0, -72.0, edge=0.28, width=2.2, proud=0.3)

    _spine_seams(d, _dorsal(d, ST_BS, PROF_BS, base=0.8, n=32), every=2, thick=0.34)
    _keel(d, ST_BS, 160.0, -150.0, 12.0, 1.6, n=28)

    # Ventro-fin: a deep, raked wing either side of the keel. Nothing under it.
    for s in (-1, 1):
        m = (Matrix.Translation((0, -30.0, -13.0))
             @ Matrix.Rotation(s * math.radians(22), 4, "Z")
             @ Matrix.Translation((s * 9.0, 0, 0)))
        d.box((0, 0, 0), (16.0, 90.0, 1.4), "Dark", matrix=m, chamfer=0.4, taper=0.8)
        d.box((s * 12.0, -30.0, -12.0), (2.0, 60.0, 5.0), "Dark", chamfer=0.6, taper=0.7)

    # Secondary sponsons: outrigger pods slung off the shoulder, raked and canted.
    for z in (40.0, 2.0, -36.0):
        for s in (-1, 1):
            m = (Matrix.Translation((s * 20.5, z, 5.6))
                 @ Matrix.Rotation(-s * math.radians(14), 4, "Y"))
            d.box((0, 0, 0), (11.0, 16.0, 7.0), "Paint", matrix=m, chamfer=1.6, taper=0.88)
            d.box((0, 0, 3.7), (9.0, 13.0, 0.7), "Dark", matrix=m, chamfer=0.2)
            # Local offsets: `matrix` already carries the pod out to the shoulder.
            d.box((s * 3.5, 0.0, -2.6), (3.0, 12.0, 4.0), "Dark", matrix=m, chamfer=0.8)
            d.box((0.0, 8.4, 0.2), (9.0, 0.5, 0.5), "Trim", matrix=m)
            for q in range(3):
                d.box((s * 2.1, -5.0 + q * 5.0, -2.4), (0.6, 3.2, 2.4), "Dark",
                      matrix=m, chamfer=0.2)

    # Citadel: two levels growing out of the spine, angular and hard-shouldered.
    d.box((0, 4.0, 16.5), (22.0, 86.0, 9.0), "Paint", chamfer=2.4, taper=0.9, taper_y=0.95)
    d.box((0, 4.0, 21.2), (19.0, 78.0, 0.5), "Dark")
    for yy in range(-34, 44, 6):
        for s in (-1, 1):
            if rnd.random() < 0.8:
                d.box((s * 10.5, yy, 17.6), (0.2, 2.6, 1.0), "Windows")
            if rnd.random() < 0.4:
                d.box((s * 10.0, yy, 20.4), (0.2, 2.6, 0.8), "Windows")
    d.box((0, 2.0, 22.0), (26.0, 40.0, 6.0), "Paint", chamfer=2.0, taper=0.82)
    for yy in range(-16, 20, 6):
        for s in (-1, 1):
            if rnd.random() < 0.7:
                d.box((s * 12.2, yy, 22.4), (0.2, 2.4, 0.9), "Windows")

    # Bridge wing: the contract puts two point-defence tubs out here, so the structure
    # that carries them has to reach, on pylons off the citadel.
    d.box((0, 14.0, 22.0), (38.0, 26.0, 6.0), "Paint", chamfer=1.6, taper=0.96)
    d.box((0, 14.0, 25.2), (34.0, 22.0, 0.5), "Dark")
    for s in (-1, 1):
        d.box((s * 13.5, 14.0, 19.0), (3.0, 22.0, 8.0), "Paint", chamfer=0.8, taper=0.9)
    for yy in (5.0, 11.0, 17.0, 23.0):
        for s in (-1, 1):
            d.box((s * 18.0, yy, 23.0), (0.3, 2.4, 0.9), "Windows")
    # Tower above the wing, and its directors.
    d.box((0, 13.0, 28.0), (18.0, 20.0, 6.0), "Paint", chamfer=1.6, taper=0.72, taper_y=0.86)
    d.box((0, 3.2, 29.0), (13.0, 0.3, 1.3), "Windows")
    for s in (-1, 1):
        d.box((s * 6.4, 13.0, 29.0), (0.3, 15.0, 1.3), "Windows")
    d.box((0, 5.0, 31.4), (24.0, 12.0, 2.8), "Paint", chamfer=0.8, taper=0.95)
    d.box((0, -0.5, 31.5), (20.0, 0.3, 1.0), "Windows")
    for yd, zd in ((8.0, 34.0), (-16.0, 33.0)):
        d.box((0, yd, zd), (7.0, 6.0, 4.0), "Paint", chamfer=0.9, taper=0.8)
        d.box((0, yd - 1.2, zd + 0.7), (16.0, 1.4, 1.4), "Dark", chamfer=0.3)
        for s in (-1, 1):
            d.cyl((s * 7.8, yd - 1.2, zd + 0.7), (s * 8.4, yd - 1.2, zd + 0.7), 1.0,
                  seg=10, slot="Dark")
    # Tripod mast.
    apex = Vector((0, 18.0, 56.0))
    for b in (Vector((0, 25.0, 31.0)), Vector((-5.5, 12.0, 31.0)), Vector((5.5, 12.0, 31.0))):
        d.cyl(b, apex, 1.1, 0.6, seg=8, slot="Dark")
    d.cyl(apex, apex + Vector((0, 0, 11.0)), 0.6, 0.2, seg=6, slot="Dark")
    for zz, span in ((46.0, 20.0), (52.0, 12.0)):
        d.box((0, 18.0 - (zz - 46.0) * 0.12, zz), (span, 0.6, 0.6), "Dark")
    d.box((0, 18.8, 41.0), (9.0, 1.2, 5.0), "Dark", chamfer=0.5)
    d.box((0, 18.0, 67.3), (0.7, 0.7, 0.7), "Heat")

    # Radiator banks, laid flat against the fin and raked aft either side of the gun bays.
    _radiators(d, 32.0, 58.0, _dorsal_top(ST_BS, PROF_BS, 45.0, base=0.8) - 1.4, 5.4, 5,
               rake=32.0)
    _radiators(d, -60.0, -34.0, _dorsal_top(ST_BS, PROF_BS, -47.0, base=0.8) - 1.4, 5.4, 5,
               rake=32.0)

    # Debris deflector and anchors forward of A.
    for s in (-1, 1):
        m = Matrix.Translation((s * 5.0, 126.0, DECK_BS + 1.4)) @ Matrix.Rotation(
            s * math.radians(-30), 4, "Z")
        d.box((0, 0, 0), (11.0, 1.4, 2.8), "Paint", matrix=m, chamfer=0.4, taper=0.85)
        d.cyl((s * 4.0, 150.0, 1.0), (s * 7.0, 138.0, 1.0), 0.9, 0.9, seg=8, slot="Dark")
    d.cyl((0, 134.0, -8.0), (0, 134.0, -11.0), 5.0, 3.2, seg=20, slot="Dark")

    # Vertical launch cells and boat bays aft of Y.
    for i in range(5):
        for j in range(3):
            d.box(((j - 1) * 3.4, -110.0 - i * 3.4, DECK_BS + 0.15), (2.8, 2.8, 0.35),
                  "Dark", chamfer=0.15)
    d.box((0, -117.0, DECK_BS + 0.05), (12.0, 19.0, 0.2), "Trim")
    for s in (-1, 1):
        d.box((s * 15.0, -128.0, 3.0), (6.0, 16.0, 7.0), "Dark", chamfer=1.4, taper=0.85)
        d.box((s * 17.4, -128.0, 4.0), (0.6, 12.0, 4.0), "Trim")

    _hardpoints(d, "battleship", ST_BS, roof={14.0: 25.0, 40.0: 9.0, 2.0: 9.0, -36.0: 9.0})
    _lights(d, ST_BS, 160.0, -150.0, 30)
    _greebles(d, ST_BS, 170.0, -166.0, 40, rnd,
              (104.0, 72.0, 14.0, -66.0, -98.0, 132.0, -128.0))

    engines = []
    d.box((0, -176.0, -1.0), (38.0, 8.0, 24.0), "Dark", chamfer=3.0, taper=0.92)
    d.box((0, -181.0, 4.0), (26.0, 4.0, 14.0), "Paint", chamfer=1.4, taper=0.9)
    for x, z, r in ((0.0, 1.0, 6.4), (-13.0, 1.0, 5.2), (13.0, 1.0, 5.2),
                    (-7.0, -7.0, 3.0), (7.0, -7.0, 3.0)):
        f.nozzle(d, x, z, r, -180.0, engines, seg=24, ribs=3)
    parts.append(d.build(bevel=0.16, bevel_segments=1, smooth_angle=30))
    return f.finish(parts, "Battleship", col, engines)


BUILDERS = {"corvette": corvette, "frigate": frigate, "destroyer": destroyer,
            "transport": transport, "battleship": battleship}