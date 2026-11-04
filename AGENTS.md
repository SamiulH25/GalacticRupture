# Galactic Rupture: instructions for agents

`IDEAS.md` is the single design source. Read §1, §1.3, §1.4 and §7 before any design, model
or campaign work. Sections marked `LOCKED` were decided with the user. Build on them, and
change them only when the user asks. Don't re-propose alternatives on your own.

## Locked direction (as of 2026-11-04)

- **The game is a free company** (`IDEAS.md` §1, §1.4). The player runs a mercenary fleet in a
  war that AI powers fight on the campaign sim. It is not an Empire at War-style conquest game
  where the player is a power. Endings: commission (admiralty), found a state, or the legend.
- **The salvage fleet** (§1.3) is the core loop: named, scarred ships, salvage and refit.
- **The world** (§7.1): the Meridian, lanes held open by keystones, the Rupture as the master
  keystone cracking, heat and radiators, battles on a plane, scarce crews.
- **The five powers** (§7.2): Sovereign Fleet, Foundry Combine, March Wardens, Keystone Synod,
  Charter Houses. Each is modelled on a real navy at war, and that precedent is the design
  brief.
- **Ship design** (§7.4): every hull is explainable to a shipwright and derived from what a
  real faction builds in wartime. Shared anatomy, per-faction hardpoint layouts, hull classes
  set by lane rating. No ventral guns, short heavy barrels, built live in Blender. Present ship
  concepts as plan-view sketches with callouts.
- **Lane ratings and keel fabs** (§7.5): adopted in their smallest form. Extensions
  (keystone raids, degrading lanes) are deferred.
- **Combat**: slow, deliberate RTS the player commands directly (not an autobattler). Fleets
  move as a unit, and ships stay level.

## Working rules

- Keep `IDEAS.md` current in the same task as any change to behaviour, plan status or
  decisions, and add a change-log entry (§11).
- The sim (`Assets/Scripts/Sim`) has no engine references and must stay deterministic
  (`IDEAS.md` §8.5, §8.7).
- The code still uses the old faction names. The slot mapping to the new powers is in §7.2.
