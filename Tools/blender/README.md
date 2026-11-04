# Ship model pipeline

Every ship mesh in the game is generated here and exported to `Assets/Art/Models/*.fbx`.
Nothing is hand-modelled in the Unity project.

> **Superseded design (2026-11-04).** The models and the shared contract described below
> were rejected. `IDEAS.md` §7.4 now requires **per-faction hardpoint layouts**: `HullDef`
> gains a faction dimension, and the contract check runs per faction. New hulls follow the
> wartime ship design doctrine and the shared anatomy in §7.4, and each faction's real-world
> precedent in §7.2. The text below describes the current code until that rework lands. Keep
> the safety checks (C#↔model mirror, no floating turrets). Drop the "never move a gun" rule.

## The contract

`Assets/Scripts/Sim/Scenarios/Hulls.cs` defines one `HullDef` per class and **no faction
dimension**, so every faction's hull for a given class must present the same hardpoints at
the same places. `fleet.py` transcribes those hardpoints, deck heights and hull extents into
`CONTRACT`, and `fleet.check()` runs on every build.

`check_contract.py` guards that mirror against drift: it parses the `Hp(...)` calls back out
of `Hulls.cs` and fails the build if they no longer agree. `build.py` runs it before any
geometry, so moving a gun in C# without moving it in the models is a build error, not a
fleet of turrets floating in space.

`check()` raycasts straight down through each mount's station and fails the build if the
hull's own surface is below the mount's yaw pivot. A turret can therefore never end up
floating over a gap or standing outside the beam.

**A faction expresses itself through outline, superstructure, propulsion and surface detail
— never by moving a gun or changing a deck height.** If the contract looks wrong, fix
`Hulls.cs` first and re-transcribe; do not edit a hull to dodge it.

## Layout

| File | Role |
|---|---|
| `fleet.py` | The hardpoint contract, section grammars, and shared hull helpers |
| `grkit.py` | Low-level kit: `Part` (loft/box/cyl), materials, FBX export |
| `turrets.py` | The four shared turret models, one per `MountDef` |
| `continuance.py` `remnant.py` `serac.py` `ilium.py` | One module per faction, five hull builders each |
| `export_all.py` | Builds every model and exports FBX |
| `build.py` | Headless entry point |
| `render.py` | Renders review sheets |
| `assemble.py` | Places turrets on a hull at the contract's hardpoints, for review renders |
| `showcase.py` | Loads the whole fleet into an interactive session for side-by-side comparison |

Each faction module exposes `BUILDERS = {"corvette": …, "frigate": …, "destroyer": …,
"transport": …, "battleship": …}`, each taking a collection and returning the joined hull.

### Section grammars

One per faction, all in `fleet.py`, so plating courses and strakes have a single hull edge
to bolt to:

- `heavy_section` — flared warship, wide belt under the deck (Continuance)
- `blade_section` — hard chines, narrow deck (Remnant)
- `faceted_section` — flat roof, chamfered shoulder, slab sides (Serac)
- `ring_section` — blunt ogive, narrow deck, deep keel (Ilium)
- `boxy_section` — chamfered hold

## Commands

Run from the repo root.

    # Build everything (20 hulls + 4 turrets)
    blender -b --factory-startup -P Tools/blender/build.py

    # One faction, skipping the shared turrets
    blender -b --factory-startup -P Tools/blender/build.py -- continuance --no-turrets

    # One class across every faction
    blender -b --factory-startup -P Tools/blender/build.py -- --classes corvette,frigate

    # Review renders: three views per hull into output/ship-reviews/<faction>/
    blender -b --factory-startup -P Tools/blender/render.py -- remnant

Pass `--no-turrets` when working on one faction: turrets are shared, and a bare build
rewrites everyone's. Use `build.py`, not the Blender GUI, so the FBX is reproducible.

## Output contract

Hull files are `hull_<class>_<faction>.fbx`. `ProjectBuilder` splits that name into a hull
class and a faction index (`GameArt.FactionNames`), and `GameArt.HullModel(id, faction)`
returns the faction's own mesh, falling back to any faction-agnostic entry and then to the
procedural placeholder mesh.

Each hull is one joined object, origin at midships on the centreline, +Y forward, +Z up,
in metres, with `Engine_<i>_r<radius>` marker children where the plumes belong.

Material slots are named and must stay exactly: `Paint`, `Dark`, `Trim`, `Windows`,
`Engine`, `Heat`. `GameArt.ApplyFaction` swaps them per faction at runtime by name, so a
hull that paints everything `Paint` loses the faction entirely.

## Conventions learned the hard way

- **Never a flat panel for a radiator.** One plate with painted stripes reads as a billboard
  from every angle. Use `fleet.radiator`, which lays a comb of individually raked slats.
- **`Part.box` takes a local centre when given a matrix.** Passing world coordinates into a
  matrix that already translates double-offsets the box.
- **`Matrix.Rotation` alone rotates about the world origin.** Compose with
  `Matrix.Translation(pos) @ rot` so a part pivots about its own seat.
- **Lay plates on a flank with `atan2(-nx, nz)`**, not `-atan2(uz, ux)`, or the plate's
  thin axis ends up along the hull normal and the plate vanishes.
- **`blade_section` has no deck out at 0.8 × beam.** Greebles placed there hang in mid-air.
