# Model refinement pass

The 20 hulls are structurally correct — every hardpoint is supported, every class reads
distinctly, the fleet matches the reference art's silhouettes. What they are not yet is
*finished*. They read as blockouts: correct massing, placeholder surface.

This is a punch list, ordered by return on effort. Every item is observed from the renders
in `output/ship-reviews/`, not guessed.

---

## Priority

| | Fix | Why it matters |
|---|---|---|
| **P0** | Trim strakes (§1) | The single most damaging defect. It appears on all 20 hulls and is the first thing the eye catches. |
| **P0** | Engine bells (§4) | Pure white discs, in renders *and* in game. Reads as broken, not as thrust. |
| **P1** | Panel breakup (§3) | Large flat facets are why everything looks like grey plastic. Biggest overall silhouette gain. |
| **P1** | Radiators (§2) | Still flat fins on every faction after the first pass. |
| **P2** | Armour plate depth (§5) | Applied armour currently reads as stickers. |
| **P2** | Superstructure detail (§6) | Every ship has one extruded box where the crew spaces are. |
| **P2** | Nacelle fairings (§8) | Pods intersect the hull with no transition. |
| **P3** | Bow detail (§7) | Forward 20% of most hulls is bare. |
| **P3** | Deck hierarchy (§9) | Either empty grey or an undifferentiated brick pile. |
| **P1** | Per-faction second colour (§11) | Serac and Remnant are two-tone area schemes with no slot for the second colour. Small change to `GameArt`, unlocks half of each faction's identity. |
| **P1** | Texture the Trim slot (§10) | Trim is the only material slot with no map at all. Untextured flat colour is a large part of why it reads as plastic planks. |
| **P2** | Surface variation maps (§10) | Albedo is pure greyscale, so no faction can have weathering character or markings. |
| **P3** | Per-class texture scale (§10) | One `UV_METRES` for every class; the small hulls get coarse plating and the battleship gets fine. |

Do P0 and P1 first — that is roughly a third of the perceived roughness for a fifth of
the work.

---

## 1. Trim strakes — P0, all 20 hulls

**Observed.** Three failure modes, in descending order of how badly they damage the read:

- *Floating trim.* **An actual bug, on Remnant and Ilium.** `remnant_destroyer` and
  `ilium_corvette` have trim lines that lift clear of the hull and run out past the bow into
  empty space. The cause is `fleet.interp`: for any `y` outside a hull's station range it
  returns the end station's half-beam and deck height instead of failing, so a trim run
  called with `y0`/`y1` beyond the stations keeps drawing at constant beam and sails off the
  hull. The rails are also unsupported 0.2 m tubes floating 0.3 m above the deck, so even
  where they do sit on the hull they read as wires.
  Serac's `strake` walks the real station list and does not have this fault — verified by
  rendering the Trim slot alone. Its problem is only that there is too much of it and it is
  too thin.
- *Wire trim.* A 0.16–0.22 m radius cylinder along the sheer (`continuance.py` deck-edge
  rails, `remnant.py` `_rails`). At ship scale this is a hairline: a painted racing stripe,
  not a strake.
- *Plank trim.* A flat slab laid on a surface. On `serac_corvette` and `remnant_transport`
  these read as planks dropped on the hull.

**Fix.**
1. Clamp every trim run to the hull's actual station range, or terminate it with a turn-down
   and a post at each end. Never let a run extend past `STATIONS[0][0]` or `STATIONS[-1][0]`.
   Alternatively, make `interp` raise or return `None` outside range so callers cannot
   silently overshoot.
2. Replace the wire rails with a **strake**: a chamfered band following the section's
   shoulder, sampled from `fleet.interp()` at fine stations so it hugs the hull. Give it
   real width (0.8–1.5 m on a destroyer, 2–3 m on a battleship) and enough proudness to
   cast a shadow line.
3. Where a hairline is genuinely wanted, cut a **recessed groove** — a thin `Dark` box set
   slightly *into* the surface. A dark line reads as a panel gap; a light cylinder reads as
   a pipe.
4. **Cut the trim budget by roughly 60%.** `remnant_destroyer` is covered in yellow bars and
   posts that read as scaffolding; `ilium_transport` has rails running the full 122 m like
   trawler handrails. Trim should mark edges and hatches, not outline the whole ship.

## 2. Radiators — P1, all 20 hulls

The first pass replaced flat plates with fin banks (`fleet.radiator`), which was the right
call, but none of them read as radiators yet:

- `remnant_corvette` / `remnant_frigate` — the vanes splay forward off the dorsal spine like
  a crown of knife blades. They look like decorative fins, not heat exchangers.
- `ilium_destroyer` — the flank banks are flat dark trapezoids standing proud of the curved
  shell; the large one on the dorsal house is a **flat black decal with no depth**.
- `continuance_battleship` — much improved, but the banks sit above the citadel roof rather
  than being fed from it, and there is no visible pipe run.
- `serac_*` — barely present; the transport has none at all.

**Fix.** Slats need enough depth to self-shadow: at gameplay distance a 0.5 m fin is
invisible. Widen the comb, deepen each slat, and recess the whole bank into a plenum so it
sits *in* the hull rather than on it. Cut visible plumbing from the heat exchanger to the
bank. Kill the splayed crown on the Remnant escorts — lay the vanes flat against the spine
and rake them aft instead of forward.

## 3. Panel breakup — P1, all 20 hulls

**Observed.** Every large facet is a single uninterrupted plane. The Continuity battleship's
flank is one 250 m smooth wall. The Ilium transport's deck is one blank grey field. The
`panels_albedo` / `panels_normal` textures exist in Unity, but at the distances these ships
are actually seen (700 m–14 km per the bench in IDEAS.md) the texture cannot carry it. The
geometry has to.

**Fix.**
1. Break every hull side into **two or three plate courses** with a visible seam: a proud
   plate row over a recessed band, or vice versa.
2. Add recessed panel lines as thin `Dark` boxes set 2–4 cm into the surface. These are the
   highest-value detail in the whole document — they cost almost nothing and they are what
   makes a surface stop looking like plastic.
3. Vary greeble density. Currently the pattern is either uniform or absent. Real ships
   cluster: dense around machinery and hatches, bare on open deck.

## 4. Engine bells — P0, all 20 hulls

**Observed.** Every nozzle ends in a **flat white disc**. `grkit.material` sets the `Engine`
slot to emission strength 3.0, and the cap geometry is a flat disc facing aft, so it clips
to white and the bell has no interior at all. This survives into the game — the engine
exhausts in the in-game capture are white circles with no throat.

**Fix.**
1. **Recess the throat.** Currently the cap sits flush with the bell mouth. Push it 0.6–0.9
   radii *into* the nozzle and line the recess with `Dark`, so you see down a dark cone.
2. Add a **nozzle lip ring** — a thin torus at the mouth, slightly larger radius — so the
   bell has a silhouette instead of ending in a hard disc.
3. Drop `Engine` emission to ~1.2–1.5 and let the game's `EngineGlow` material (strength 5)
   do the glow. The mesh should read as *machinery with a hot interior*, not as a light.
4. Vary bell sizes within a cluster — the Serac and Ilium battleships have several equal
   bells in a row, which reads as a mistake.

## 5. Armour plate depth — P2

**Observed.** Applied armour is uniform in thickness, uniform in gap, and sits at a constant
standoff, so it reads as flat rectangles stuck on. `continuance_battleship` is the clearest
case: a row of identical grey rectangles down both flanks with visible seams between them
like tiles.

**Fix.** Vary standoff plate to plate (0.15–0.6 m), stagger the gaps rather than leaving a
regular rhythm, and add a bolt or rivet row on the larger plates. The `Dark` replacement
plates are a good idea and should stay — but they should sit at a *different* depth, so a
replaced plate visibly stands proud or sits recessed.

## 6. Superstructure detail — P2

**Observed.** Every ship has essentially one deckhouse: an extruded box with chamfers and
flat white rectangles for windows. `continuance_battleship` is the best of them and still
reads as a box with a box on top.

**Fix.**
- **Recess the windows.** Currently `Windows` boxes are applied proud of the surface, so they
  float. Set them into the hull so they read as lit slots.
- Add **bridge wings** that overhang, with a support strut.
- Add ladders, vent stacks, and hatch coamings on the citadel sides.
- Break the silhouette with one asymmetric element per ship — a mast, a director, a gantry.
  Every hull currently has a symmetric box.

## 7. Bow detail — P3

**Observed.** The forward 20% of nearly every hull is blank. `remnant_transport`,
`serac_transport`, `ilium_destroyer` and `remnant_corvette` all have a large empty rounded
or faceted nose with nothing on it. Bow clutter is cheap: anchors, floodlights, a bow plane
or lidar dome, chine strakes.

## 8. Nacelle fairings — P2

**Observed.** Outboard pods intersect the hull with no transition —
`ilium_destroyer`'s nacelles are smooth cylinders that pass straight through the shell,
`serac_corvette`'s are stubby boxes that just sit there. The eye reads the intersection
seam as a modelling error even when it is hidden.

**Fix.** Add a faired transition: a tapered collar or a strut that visually carries the pod,
and make the pod's inboard end blend into the hull shoulder. Vary the rib count and make
ribs cast a shadow.

## 9. Deck hierarchy — P3

**Observed.** Two opposite failures. Most ships have a large empty grey deck.
`serac_transport` has the opposite problem: the deck is a checkerboard of identical grey and
yellow boxes with no hierarchy, reading as a brick pile.

**Fix.** Give the deck a reading order — a clear path fore-and-aft, machinery grouped at
the ends, cargo or equipment in bays with a consistent footprint. Break the Serac transport's
container grid into varied sizes with visible lashing and a few open slots.

## 10. Texturing and surface detail

**What is already right — do not change this.** The UV setup is good and worth protecting:

- `grkit.box_uv()` is a true triplanar-style projection. It picks the dominant face normal
  and projects the other two axes, so vertical flanks get `(y, z)` rather than a smeared
  top-down projection. This is correct.
- `UV_METRES = 24.0` means one texture tile spans 24 m of hull. With the recursive split in
  `Textures.Panels` (`min = 56` px on 1024) that gives panels of roughly 1.3 m **in world
  space, consistently across every class**. A corvette and a battleship get the same physical
  plate size, which is right.
- `panels_normal.png` is imported correctly (`textureType: 1`, `sRGBTexture: 0`), so it gets
  proper normal-map compression. Aniso 8 and mipmaps are on.
- The albedo is greyscale by design (`a[i] = new Color(t, t, t, 1)`) and tinted per faction
  through `_BaseColor`. One texture serving four fleets is the right call.

**What is wrong.**

- **The `Trim` slot has no texture at all.** `Trim_Continuance.mat` and its siblings have
  `_BaseMap: {fileID: 0}`. Every other lit material — `Hull_*`, `DarkMetal`, `Wreck` — gets
  the panel maps. Trim is untextured flat colour, and trim is used heavily on most of the
  fleet. This is a large part of why it reads as plastic planks and wires (§1), and why the
  fix there is not just geometry.
- **`Windows`, `Engine`, `Heat` and `Plume` are untextured emissive.** Correct for `Engine`
  and `Heat`. Wrong for `Windows`: they are flat emissive rectangles with no frame, no
  interior, no variation. See §6.
- **No metallic or smoothness variation.** Only `_BaseMap` and `_BumpMap` are bound, so
  `_Metallic` and `_Smoothness` are uniform across every square metre of hull. The panel
  texture varies tone in the albedo but not in reflectance, so plating reads as one flat
  finish.
- **No faction weathering character.** Because the albedo is greyscale luminance, the only
  channel available to a faction is *brightness*. Remnant's "worn steel", Continuance's
  "replacement plates in a different finish" and Ilium's "pale ceramic" cannot differ by
  anything except how light or dark they are.
- **No markings.** Hazard striping, unit insignia, hull numbers, faction glyphs and
  squadron flashes are impossible with the current setup. The reference sheets all carry
  insignia.

**Fix.**
1. Bind the panel maps to the `Trim_*` materials too, with a lower `_BumpScale` (~0.25) and
   higher `_Metallic`. Trim is machined or rolled metal, not painted plastic.
2. Add a **metallic/smoothness variation** derived from the same panel height field —
   `Textures.Panels` already computes `height[]`; writing it into a third map costs almost
   nothing and stops every plate reflecting identically. Rough up the seams, polish the
   plate centres.
3. Add a **detail/AO or grime map** multiplied into the albedo. Grime collecting along seams
   is already computed as `tone`; it just needs somewhere to go besides luminance.
4. To give factions character without four 1024² textures, add a second UV-independent
   channel: a low-cost approach is to vary the *panel size* per faction by scaling
   `UV_METRES` per faction (Serac wants big slab plates, Remnant wants small riveted ones),
   plus a per-faction grime tint.
5. Markings: add a decal pass — a separate `Markings` texture sampled by a second UV set, or
   simply place faction insignia geometry on the hull in the `Trim` slot. Geometry is
   cheaper to author here than a UV2 pipeline, and at these camera distances it reads better.

**Per-class texture scale (P3).** `UV_METRES` is a single global. With one value the
battleship gets ~15 tiles along its length and the corvette gets ~2. World panel size is
constant, which is correct, but the *tiling frequency* is not, and the coarse end is where
the 1024² map runs out of detail. Consider a second scale for the small classes — coarser
plates on corvettes and frigates would also match how they should read as lighter craft.

## 11. Faction colouring

**How it works.** `GameArt.ApplyFaction` swaps whole materials by slot name, so a faction is
defined by exactly four materials: `HullPaint[faction]`, `Trim[faction]`, `EngineGlow[faction]`
and `Plume[faction]`. `Dark`, `Windows`, `Heat` and `Wreck` are **shared across all four
factions**. That is a deliberate, reasonable design — but it means a faction's identity rests
entirely on two colours, and both need to be right.

**Observed — the palettes are broadly right, but the art wants a third colour the slots
cannot carry.**

Checked against the four class sheets in `output/ship-references/final-fleet/`:

| Faction | `Hull_*` now | Art shows | verdict |
|---|---|---|---|
| Continuance | `(0.27, 0.30, 0.35)` slate blue-grey | blue-grey hull, amber trim | matches |
| Remnant | `(0.38, 0.06, 0.07)` oxide red | red hull with **large bone panel blocks** | base matches; the bone is area, not trim |
| Serac | `(0.14, 0.30, 0.27)` dark green | **cream and dark green in large blocks**, roughly even | one of two; the cream is lost |
| Ilium | `(0.42, 0.31, 0.16)` tan / khaki | warm tan hull, **neutral dark grey** structure | hull matches; trim is warm brown where the art is neutral |

So there is no gross "wrong faction" error — that was my first reading and it was wrong. The
real problem is structural: **Serac and Remnant are two-tone area schemes**, where a second
colour covers large blocks of hull. The `Trim` slot is used for thin strakes and rails on
most hulls, so putting a block colour there fights the geometry, and the block colour has
nowhere else to go.

Only one concrete mismatch: `Trim_Ilium` is a warm brown `(0.33, 0.22, 0.13)` where the art's
dark structure is neutral grey. Small, but it makes Ilium's accents look muddy rather than
ceramic.

**Fix.**
1. **Add a per-faction second area colour.** The cleanest route that does not disturb the
   hardpoint work: make `Dark` per-faction. Every one of the twenty hulls already uses the
   `Dark` slot for armour plates, recesses and structure — in Serac that is where the green
   blocks belong, and in Remnant where the bone belongs. Extend `GameArt` with
   `DarkMetal[]` indexed by faction alongside `HullPaint[]`, give each power its own dark
   tone, and let the models paint their area blocks into `Dark` rather than `Trim`. Right now
   all four share `DarkMetal (0.12, 0.125, 0.135)`, which is why the applied armour reads as
   the same black on all twenty hulls.
2. **Neutralise `Trim_Ilium`** toward grey. One line.
3. **Vary the metallics.** `Hull_*` is `_Metallic 0.12, _Smoothness 0.5` for all four — a
   painted hull. Serac's "basalt" and Remnant's "worn steel" want to be noticeably less
   metallic and rougher than Ilium's ceramic. Metallicity is a bigger identity lever than
   hue at gameplay distance.
4. **Check lightness, not just hue.** Per the bench in IDEAS.md these ships are read at
   700 m–14 km. At that range a hue difference between two dark values is invisible while a
   *value* difference is not. Continuance and Serac are both dark and cool; they will read as
   the same ship at 14 km. Push their lightness apart, or accept that the far camera
   identifies factions by silhouette and massing rather than by colour.
5. **Do not add markings until the panel texture carries hue** (§10). Faction insignia on a
   greyscale-luminance texture will read as grey smudges.

## Reference palette, for whoever fixes §11

Read off `output/ship-references/final-fleet/*.png` by eye — check against the plates before
committing to specific values.

| | hull (current → art) | second colour in the art | where it should live |
|---|---|---|---|
| Continuance | slate blue-grey → matches | amber, thin | `Trim` ✓ |

| Remnant | oxide red → matches | **bone, large blocks** | `Dark`, made per-faction |
| Serac | dark green → **one of two** | **cream, large blocks** | `Dark`, made per-faction |
| Ilium | tan / khaki → matches | **neutral dark grey** | `Dark` + neutralise `Trim_Ilium` |

All four `Hull_*` colours are defensible against their sheets. The gap is that two of the
factions need a large-block second colour and there is no slot for it.

---


## Per model

Ordered worst first. Items already covered in the sections above are referenced, not
repeated.

### Serac

**Whole faction — half the scheme is missing.** The Serac art is a two-tone cream-and-green
in large blocks, roughly even. `Hull_Serac` takes the green and the cream has nowhere to go,
so every hull reads darker than its concept. Nothing is miscoloured; it is incomplete.
Painting the cream blocks into `Dark`, once `Dark` becomes per-faction, recovers it (§11).

**`serac_corvette`** — Trim is applied as thick flat slabs lying on the pod roofs and deck;
they read as dropped planks (§1). The whole hull is a smooth chamfered blob with no panel
work. Nacelles are stubby boxes with no fairing (§8). Bow is a blank yellow-topped box.

**`serac_frigate`** — Trim is correctly placed on the hull — the only faction that gets this
right — but it is far too thin and far too plentiful (§1). The deck is almost entirely empty
across 98 m. The grey armour plate run down the port flank is a single flat band with a
regular tile rhythm (§5).

**`serac_destroyer`** — Reads well in plan; the outboard pods and wide barge hull work.
Needs: the Trim is heavy along both sheer lines and on the sponson tops, competing with the
hull shape. Vent stacks are isolated cones sitting on bare deck with nothing around them.

**`serac_transport`** — Worst deck in the fleet: a flat checkerboard of identical grey and
yellow boxes with no hierarchy (§9). Two emissive yellow deck panels blow out to flat
rectangles. Flank is covered in small identical rectangles (§5). The bow deckhouse is
under-scaled against a 127 m cargo deck.

**`serac_battleship`** — Tri-lobed plan reads, but from three-quarter it collapses into one
broad slab; the slots and cross-beams need more depth so the lobes separate in silhouette
too. Trim is used heavily on every edge and flattens the form. Nine bells in a row of
near-identical size (§4).

### Remnant

**Whole faction — the bone blocks have nowhere to live.** The Remnant art is a strong
red-against-bone split with the bone covering large panel areas. `Trim_Remnant` is bone
`(0.72, 0.68, 0.58)`, which is right as a colour, but `Trim` is used for thin strakes on these
hulls — so a block colour in the trim slot fights the geometry, and the bone never appears at
the area scale the art uses. Paint the big bone panels into `Dark` once `Dark` is per-faction,
and keep `Trim` for the small safety-yellow accents (§11).

**`remnant_corvette`** — The dorsal spine is a **large smooth featureless tube**; it is the
dominant form and it is completely blank. The radiator vanes splay forward off the spine
like a crown of blades (§2). The ventral keel blade is a huge smooth dark teardrop — a whale
belly with no detail. Trim is a hairline wire (§1). Armour plates are flat grey rectangles
(§5).

**`remnant_frigate`** — Same smooth-blank spine, made worse because it runs the full 95 m.
The splayed radiator crown forward of the fin is the most obviously wrong feature on any
escort (§2). Hull is a smooth teardrop with no panel breakup at all (§3).

**`remnant_destroyer`** — **Worst trim offender in the fleet.** Four long yellow lines run the
full length of the ship; the forward pair lifts clear of the hull and runs out past the bow
into space — the `interp` clamping bug in §1, confirmed by rendering the Trim slot alone.
Yellow bars and posts then cover the deck and both sheer lines, reading as scaffolding.
Bow is bare, radiator vanes are thin flat blades, and the dorsal mast is a single thin pole
with nothing on it.

**`remnant_transport`** — Cargo containers are **plain grey boxes with painted yellow
stripes**; they read as untextured shipping crates and need door detail, corner castings and
lashing. The stern ventral blades read as **black spiky claws** — they should be a keel, not
claws. Trim is a thick flat plank down the deck edge (§1). Bow is a blank rounded mass.

**`remnant_battleship`** — Strongest silhouette of the four. The narrow spine with
superfiring islands works. Needs: the bridge wing on pylons is a thin gantry that reads as
scaffolding; trim along the full strake is a wire; the aft hull runs very long and empty
between the last turret and the engines.

### Ilium

**Whole faction — one small mismatch, one missing colour.** `Hull_Ilium` is a warm tan
`(0.42, 0.31, 0.16)` and the art is a warm tan — that one is fine. Two things are not:
`Trim_Ilium` is a warm brown where the art's dark structure is **neutral grey**, and the
art's large dark grey blocks have nowhere to live because `Dark` is shared across all four
factions. Fix both with §11.

**`ilium_corvette`** — **Both trim rails lift clear of the hull** and run out past the nose
into space — the `interp` clamping bug in §1, confirmed by rendering the Trim slot alone.
A **flat black rectangle with a yellow border sits proud on the deck**; it reads as a sticker
rather than a recessed hatch. Dark grey flat slabs hang below the stern deck edge. Nacelles
are smooth unsegmented cylinders (§8). Bow is a blank rounded mass.

**`ilium_destroyer`** — The big **dorsal grille is a flat black trapezoid with no depth** —
it reads as a painted-on decal, not a radiator (§2). Flank banks are flat angular slabs
standing proud of a curved shell. The bow dome is a large blank rounded mass (§7). Nacelles
are smooth pipes with a few ribs and no fairing (§8).

**`ilium_transport`** — **Trim is overwhelming**: long yellow rails run the full 122 m like
trawler handrails, and the two deck radiator banks have bright yellow caps (§1). Dark box
structures on the port flank are flat rectangles with a single white window decal. Stern is a
flat black plate carrying four white discs (§4).

**`ilium_battleship`** — Best of the Ilium set. Needs: the flank radiator banks are flat
dark plates rather than louvred volumes (§2); the sponsor sponsons are plain boxes; the aft
transom plate is flat black with four white discs (§4).

### Continuance

**`continuance_corvette`** — Reads well and holds up best at small scale. The outboard drive
pods are the strongest feature in the fleet. Needs: hull sides are one smooth flare (§3);
the pylon between pod and spine is a bare box; the bow is blank.

**`continuance_frigate`** — The reworked radiator bank is a real improvement and is the
model to copy. Needs: the hull is a smooth lozenge with no panel work (§3); the deck is
almost entirely empty between the two guns; trim is a hairline wire (§1).

**`continuance_destroyer`** — Citadel was strengthened and now reads properly. Needs: the
flank is still one long unbroken slab (§3); the deck fore and aft of the citadel is bare;
trim is a hairline wire along the sheer; the armour belt plates are a regular tile rhythm
(§5).

**`continuance_transport`** — Reads well — the lander cradles are the best storytelling
detail in the fleet. Needs: the radiator fin is a thin flat plate where the other classes
now have fin banks (§2); the eight hold plates on the deck roof are identical with a regular
rhythm; each lander is a plain box with a single white window strip.

**`continuance_battleship`** — The most complete ship, and the one the rest should be
brought up to. Needs: **the flank is a 250 m unbroken wall** — the single worst case of §3;
the armour belt reads as regular tiles (§5); the VLS cell grid on the deck is a flat outlined
grid (§9); the citadel sides are one box with window rows.

---

## Shared work worth doing once

These are new helpers for `fleet.py`, not per-hull edits:

- `strake(part, stations, ...)` — a trim band that follows the section (§1)
- `panel_lines(part, ...)` — recessed seam lines across a large facet (§3)
- `nozzle(..., throat=True)` — a recessed, lipped bell (§4)
- `nacelle(part, ...)` — a ribbed pod with a faired inboard transition (§8)

Each replaces the same inline loop in four modules. That is the argument for putting them in
`fleet.py`: right now every faction has independently reinvented the same four techniques,
and three of the four got them wrong in different ways.
