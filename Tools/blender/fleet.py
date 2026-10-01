"""Fleet-wide model contract and shared hull grammar.

The sim (Assets/Scripts/Sim/Scenarios/Hulls.cs) defines one `HullDef` per class and no
faction dimension, so every faction flies the same hardpoints, deck heights and
dimensions. A faction therefore expresses itself through outline, superstructure,
propulsion and surface detail -- never by moving a gun or rescaling a deck. Anything
that would move a mount belongs in the paint, not the mesh.

`CONTRACT` transcribes the hardpoints and extents from Hulls.cs so a hull builder can
check itself against the sim. `check()` asserts a built mesh actually covers them.

Axes: Blender +Y forward, +Z up, metres. `Hulls.cs` is Unity (+Z forward, +Y up);
`k.hp(x, y_up, z_fwd)` converts.
"""
import math

from mathutils import Matrix, Vector

import grkit as k

FACTIONS = ("continuance", "remnant", "serac", "ilium")
CLASSES = ("corvette", "frigate", "destroyer", "transport", "battleship")

# mount_id -> (MountDef.PivotHeight, MountDef.MuzzleLength, barrels), from Hulls.cs.
MOUNTS = {
    "main_battery": (3.8, 17.0, 2),
    "secondary": (2.2, 9.0, 2),
    "point_defence": (1.8, 5.0, 2),
    "corvette_gun": (1.5, 4.5, 2),
}

# class -> geometry the hull must present, mirrored from Hulls.cs.
#   bow/stern: mesh extent along +Y, generous past HalfLength (which is the collision half-length).
#   deck: deck plane in Z that mounts stand on.
#   mounts: (mount_id, x, y_up, z_fwd, base_yaw) hardpoints the mesh must clear, transcribed
#   from the same `Hp(...)` calls the sim reads, so a review render dresses a ship exactly
#   as the game will.
CONTRACT = {
    "corvette": dict(
        bow=25.0, stern=-22.0, deck=3.5, half_length=22.0,
        mounts=[("corvette_gun", 0.0, 3.5, 8.0, 0.0)],
    ),
    "frigate": dict(
        bow=47.0, stern=-43.0, deck=4.5, half_length=42.0,
        mounts=[("corvette_gun", 0.0, 4.5, 22.0, 0.0), ("corvette_gun", 0.0, 4.5, -18.0, 180.0)],
    ),
    "destroyer": dict(
        bow=76.0, stern=-72.0, deck=6.0, half_length=70.0,
        mounts=[("secondary", 0.0, 6.0, 40.0, 0.0), ("secondary", 0.0, 6.0, -36.0, 180.0),
                ("point_defence", 7.0, 7.0, 8.0, 90.0), ("point_defence", -7.0, 7.0, 8.0, -90.0)],
    ),
    "transport": dict(
        bow=59.0, stern=-58.0, deck=8.0, half_length=55.0,
        mounts=[("point_defence", 0.0, 9.0, 34.0, 0.0), ("point_defence", 0.0, 9.0, -30.0, 180.0)],
    ),
    "battleship": dict(
        bow=182.0, stern=-172.0, deck=12.0, half_length=170.0,
        mounts=[("main_battery", 0.0, 12.0, 104.0, 0.0), ("main_battery", 0.0, 20.0, 72.0, 0.0),
                ("main_battery", 0.0, 20.0, -66.0, 180.0), ("main_battery", 0.0, 12.0, -98.0, 180.0),
                *[("secondary", s * 19.0, 11.0, z, s * 90.0) for z in (40.0, 2.0, -36.0) for s in (1, -1)],
                *[("point_defence", s * x, y, z, s * yaw)
                  for x, y, z, yaw in ((14.0, 12.0, 132.0, 45.0), (16.0, 26.0, 14.0, 90.0),
                                      (14.0, 12.0, -128.0, 135.0)) for s in (1, -1)]],
    ),
}

SUPERFIRE = 20.0  # battleship raised deck, Hulls.BattleshipSuperfire


def interp(stations, y):
    """Half-beam, deck height and keel depth at station `y`, for a (y, w, top, bottom) list.

    Raises outside the station range. This used to clamp to the nearest end station, which
    let detail runs called past the bow or stern keep drawing at constant beam and sail off
    the hull -- trim strakes were visibly floating in space because of it. Fail loudly and
    fix the caller instead.
    """
    lo, hi = stations[-1][0], stations[0][0]
    if y < lo - 1e-6 or y > hi + 1e-6:
        raise ValueError(f"station y={y:.2f} outside hull range {lo:.2f}..{hi:.2f}")
    for a, b in zip(stations, stations[1:]):
        if b[0] <= y <= a[0]:
            t = (a[0] - y) / (a[0] - b[0])
            return tuple(a[i] + (b[i] - a[i]) * t for i in range(1, 4))
    return stations[-1][1:]


def clamp_y(stations, y):
    """Clamps `y` into the station range.

    Detail runs walk `y` in steps, so the last step routinely overshoots the hull. Clamp
    before interpolating rather than letting interp raise -- `interp` stays strict on
    purpose, and these callers are the ones that needed fixing.
    """
    return max(stations[-1][0], min(stations[0][0], y))


def mirror(half):
    """Closes a right-hand section outline into a full ring."""
    return half + [(-x, z) for x, z in reversed(half)]


def heavy_section(w, top, bottom):
    """Flared warship section: wide belt just under the deck, narrowing keel."""
    h = top - bottom
    mid = (top + bottom) * 0.5
    return mirror([(0.78 * w, top), (0.95 * w, top - 0.16 * h), (w, mid + 0.12 * h),
                   (w, mid - 0.2 * h), (0.8 * w, bottom + 0.12 * h), (0.36 * w, bottom)])


def blade_section(w, top, bottom):
    """Hard-chined diamond: narrow deck, hard shoulder at the waterline."""
    return mirror([(0.32 * w, top), (0.82 * w, top * 0.45), (w, 0.0),
                   (0.7 * w, bottom * 0.45), (0.18 * w, bottom)])


def boxy_section(w, top, bottom):
    """Boxy hold section with chamfered corners."""
    c = min(w, top - bottom) * 0.18
    return mirror([(w - c, top), (w, top - c), (w, bottom + c), (w - c, bottom)])


def nozzle(part, x, z, r, y0, engines, seg=20, trim=True, ribs=0, throat=True):
    """One engine bell at station `y0`; records the plume anchor for the game.

    The bell flares outward, with a lip ring at the mouth and a conical throat recessed
    inside it. Without the recess the far cap sat flush with the mouth and read as a flat
    disc -- which is what every nozzle in the first pass looked like.
    """
    length = r * 1.4
    depth = r * 0.75
    if trim:
        part.cyl((x, y0 + 0.6, z), (x, y0, z), r * 1.12, r * 1.12, seg=seg, slot="Trim")
    part.cyl((x, y0, z), (x, y0 - length, z), r * 0.85, r * 1.1, seg=seg, slot="Dark")
    # A slightly proud lip, so the mouth has a silhouette instead of a hard disc edge.
    part.cyl((x, y0 - length, z), (x, y0 - length - 0.22 * r, z), r * 1.16, r * 1.16, seg=seg, slot="Dark")
    if throat:
        # Recessed cone in Dark, so you look down the bell rather than into a light.
        part.cyl((x, y0 - length - depth, z), (x, y0 - length - 0.06, z), r * 0.18, r * 0.92,
                 seg=seg, slot="Dark")
        part.cyl((x, y0 - length - 0.02, z), (x, y0 - length - 0.3, z), r * 0.34, r * 0.34,
                 seg=max(8, seg // 2), slot="Engine")
    else:
        part.cyl((x, y0 - length, z), (x, y0 - length - 0.04, z), r * 0.98, seg=seg, slot="Engine")
    for i in range(ribs):
        yy = y0 - length * (0.25 + i * 0.25)
        rr = r * (0.85 + 0.25 * (0.25 + i * 0.25)) + 0.12
        part.cyl((x, yy, z), (x, yy - 0.35, z), rr, rr, seg=seg, slot="Dark")
    engines.append((Vector((x, y0 - length - 0.04, z)), r))



def faceted_section(w, top, bottom, shoulder=0.16):
    """Wide-shouldered slab: flat roof, hard chamfer at the shoulder, slab sides and a
    tucked keel. Serac bolts most of its mass onto a wide roof."""
    h = top - bottom
    return mirror([(0.86 * w, top), (w, top - shoulder * h), (w, bottom * 0.35), (0.52 * w, bottom)])


def ring_section(w, top, bottom, shoulder=0.62):
    """Blunt ogive: rounded beam, narrow deck, deep keel. Ilium."""
    h = top - bottom
    return mirror([(0.42 * w, top), (0.86 * w, top - 0.1 * h), (w, (top + bottom) * shoulder),
                   (0.74 * w, bottom + 0.18 * h), (0.22 * w, bottom)])


def barbette(part, x, y_up, z_fwd, r, height=1.2, seg=16, slot="Dark"):
    """Armoured tub under a mount, so a turret does not appear to float on the deck."""
    p = k.hp(x, y_up, z_fwd)
    part.cyl(p - Vector((0, 0, height)), p + Vector((0, 0, 0.25)), r, r * 0.9, seg=seg, slot=slot)



def radiator(part, x, y, z, height, slats, length, tilt_deg=22.0, spine="Dark", face="Heat",
             plenum=True, thick=1.1):
    """A radiator bank standing on the ship's centreline or flank.

    A comb of individually raked slats, deep enough to self-shadow at gameplay distance,
    seated in a plenum so the bank sits *in* the hull rather than on it. A single plate
    with painted stripes reads as a billboard from every angle.
    """
    pitch = height / slats
    tilt = Matrix.Rotation(math.radians(tilt_deg), 4, "Y")
    if plenum:
        # Root fairing the bank grows out of, so it is not sitting on the surface.
        part.box((x, y, z - height * 0.5 - thick * 0.6), (thick * 1.5, length, thick * 1.3),
                 spine, chamfer=0.12)
    for i in range(slats):
        zz = z + (i - (slats - 1) * 0.5) * pitch
        m = Matrix.Translation((x, y, zz)) @ tilt
        part.box((0, 0, 0), (thick, length, pitch * 0.46), spine, matrix=m, chamfer=0.08)
        part.box((0, 0, pitch * 0.24), (thick + 0.1, length - 0.6, 0.14), face, matrix=m)


def strake(part, stations, x_frac, z_off, y0=None, y1=None, width=0.9, proud=0.22,
           slot="Trim", seg=4, taper_ends=True):
    """A trim band running along the hull, following the section rather than cutting across it.

    `x_frac` is the half-beam fraction the band sits at and `z_off` the height offset from
    the deck. Both are interpolated per sample, so the band hugs the hull through the
    sheer instead of bowing away from it. Sample count is derived from the run so a long
    band gets enough segments to track a curved hull.

    Replaces the hairline cylinder rails that read as painted stripes: at ship scale a
    0.2 m tube is a wire, so the band has real width and stands proud enough to catch
    a shadow.
    """
    lo = stations[-1][0] if y0 is None else y0
    hi = stations[0][0] if y1 is None else y1
    if lo > hi:
        lo, hi = hi, lo
    steps = max(4, int((hi - lo) / 12.0))
    pts = []
    for i in range(steps + 1):
        y = lo + (hi - lo) * i / steps
        w, top, _ = interp(stations, y)
        # Taper the band away at both ends so it does not stop on a hard edge.
        k = 1.0
        if taper_ends:
            k = min(1.0, min(y - lo, hi - y) / max(1e-3, (hi - lo) * 0.12))
        pts.append((Vector((x_frac * w, y, top + z_off)), max(0.05, width * k)))
    for s in (-1, 1):
        for (a, wa), (b, wb) in zip(pts, pts[1:]):
            pa, pb = Vector((s * a.x, a.y, a.z)), Vector((s * b.x, b.y, b.z))
            part.cyl(pa, pb, max(wa, wb) * 0.5, seg=seg, slot=slot)
    # A proud edge line under the band gives it a shadow line to read against.
    for s in (-1, 1):
        for (a, wa), (b, wb) in zip(pts, pts[1:]):
            pa = Vector((s * (a.x + proud), a.y, a.z - proud * 0.9))
            pb = Vector((s * (b.x + proud), b.y, b.z - proud * 0.9))
            part.cyl(pa, pb, max(wa, wb) * 0.22, seg=seg, slot="Trim")


def panel_lines(part, stations, x_frac, z_lo, z_hi, count, y0=None, y1=None,
                width=0.34, proud=-0.04, slot="Dark", skip=()):
    """Recessed seam lines across a large flank facet.

    The highest-value detail in the refinement pass: thin `Dark` lines set slightly *into*
    the surface break up an unbroken plate wall for almost no geometry. `proud` is negative
    so the line sits inside the hull and reads as a gap rather than a stuck-on strip.
    `skip` takes station values to leave clear, so seams do not cut through a turret.
    """
    lo = stations[-1][0] if y0 is None else y0
    hi = stations[0][0] if y1 is None else y1
    if lo > hi:
        lo, hi = hi, lo
    for i in range(count):
        y = lo + (hi - lo) * (i + 0.5) / count
        if any(abs(y - s) < 8.0 for s in skip):
            continue
        w, top, _ = interp(stations, y)
        h = (z_hi - z_lo)
        for s in (-1, 1):
            part.box((s * (w * x_frac + proud), y, z_lo + h * 0.5),
                     (abs(proud) + 0.02, width, h), slot)


def nacelle(part, stations, x, y0, y1, z=0.0, r=1.6, seg=18, ribs=4, slot="Paint",
            fairing=0.9, rib_slot="Trim"):
    """A ribbed outboard pod with a faired inboard transition.

    Pods previously just intersected the hull, and the intersection seam reads as a
    modelling error even when it is buried. The fairing tapers the pod's inboard end into
    the hull shoulder so the join is intentional.
    """
    def w_at(y):
        w, _, _ = interp(stations, y)
        return w
    if fairing > 0:
        # Tapered collar bridging the hull shoulder out to the pod.
        mid = (y0 + y1) * 0.5
        span = (y1 - y0) * 0.5
        part.cyl((k_sign(x) * (w_at(mid) * 0.92), mid, z),
                 (x - math.copysign(r * 0.2, x), mid, z), fairing, r * 0.8, seg=seg, slot=slot)
    part.cyl((x, y0, z), (x, y1, z), r, r * 0.94, seg=seg, slot=slot)
    for i in range(ribs):
        t = (i + 0.5) / ribs
        yy = y0 + (y1 - y0) * t
        part.cyl((x, yy, z), (x, yy - (y1 - y0) * 0.03, z), r * 1.09, r * 1.09, seg=seg, slot=rib_slot)
    return Vector((x, y1, z))


def k_sign(x):
    return 1.0 if x >= 0 else -1.0


def finish(parts, name, col, engines):
    """Joins the parts, then hangs an `Engine_N_rR` marker off each plume point."""
    obj = k.join(parts, name)
    for i, (p, r) in enumerate(engines):
        k.marker(f"Engine_{i}_r{r:.2f}", col, p, parent=obj)
    return obj


def check(cls, obj):
    """Fails the build unless `obj` actually supports its class's hardpoints.

    For each mount this raycasts straight down through the turret's station and requires
    the hull's own surface to reach up to the mount's yaw pivot, so a turret can never end
    up floating over a gap or standing outside the beam. Rays run in the hull's local
    space, which is how the builders leave it: origin at midships on the centreline.
    """
    c = CONTRACT[cls]
    co = [v.co for v in obj.data.vertices]
    hi_x = max(v.x for v in co)
    hi_y = max(v.y for v in co)
    lo_y = min(v.y for v in co)
    hi_z = max(v.z for v in co)
    errs = []
    if hi_y < c["bow"] - 6.0:
        errs.append(f"bow {hi_y:.1f} short of {c['bow']:.1f}")
    if lo_y > c["stern"] + 6.0:
        errs.append(f"stern {lo_y:.1f} short of {c['stern']:.1f}")
    if min(v.z for v in co) > 0.0:
        errs.append(f"keel {min(v.z for v in co):.1f} above the centreline")

    down = Vector((0.0, 0.0, -1.0))
    for mount, x, y_up, z, _yaw in c["mounts"]:
        if abs(x) > hi_x:
            errs.append(f"{mount} at x={x} outside beam {hi_x:.1f}")
            continue
        hit, loc, _, _ = obj.ray_cast(Vector((x, z, hi_z + 10.0)), down)
        if not hit:
            errs.append(f"{mount} at ({x:g},{z:g}) has no hull beneath it")
        elif loc.z < y_up - 0.75:
            errs.append(f"{mount} at ({x:g},{z:g}) deck {loc.z:.1f} below pivot {y_up}")
    if errs:
        raise AssertionError(f"{cls}: " + "; ".join(errs))
    return len(co)


def tri_budget(obj):
    """Triangle count, logged so a runaway builder is visible in the build output."""
    return sum(len(p.vertices) - 2 for p in obj.data.polygons)


def fan(a, b, steps):
    """Even steps across an inclusive range; used for plate and window runs."""
    return [a + (b - a) * i / steps for i in range(steps + 1)]


def sin_ramp(i, n):
    """0..1..0 ramp over `n` intervals, for hull features that taper fore and aft."""
    return math.sin(math.pi * i / n) ** 0.6
