# Galactic Rupture — Design Ideas

The single design document for the game. Everything else in the repo is implementation.
Updated as we go.

**Status key:** `LOCKED` decided · `PROPOSED` suggested, not yet confirmed · `OPEN` needs discussion

---

## 1. The Vision

A 4x space opera in the lineage of *Star Wars: Empire at War*, with the land combat
removed and the naval combat elevated to be the centrepiece.

The appeal of EAW was never really its land battles. It was:
- A **galaxy map where a single wrong decision feels catastrophic** (the supply line)
- **Real-time fleet combat** that was fast, decisive and cinematic
- An **artificial clock** that made a 20-minute campaign tense

We are keeping the galaxy map's tension and the combat's feel, and building 4x depth
around them.

`LOCKED` — 4x depth in the overworld, with EAW's galactic conquest constraints.
`LOCKED` — Combat is the star. Everything else serves it.
`LOCKED` — Operatic space opera, original IP.
`LOCKED` — Full 3D.
`LOCKED` — Ground combat is **simulated**. Armies are transported through space for
assaults, in the manner of Stellaris.
`LOCKED` — Major and minor powers (2–3 majors as mirrors, 2–3 minors as pawns).

---

## 2. The Four Pillars

These were named directly as the things that must be in the game.

### 2.1 Independent Turrets
> *"The guns on ships were independent and would rotate on their own."*

Each gun mount is a physical object with its own position, rotation, elevation, target
and cooldown. A capital with sixty mounts has sixty independently tracking barrels.

**Why it matters:** a capital is a **mobile zone of exclusion**, not a DPS platform.
Converging a battery on one target *narrows* your defensive coverage — attention creates
vulnerability, emergently. Flanking becomes literal geometry, not a modifier.

**Visual:** the swarm-of-barrels-converging moment is the signature image of the game.

**The rule that keeps it fair:**
> Guns traverse at a finite rate. A converged battery takes time to swing onto a new
> target.

This turns an unfair alpha strike into a **telegraphed commitment** — a visible wind-up
the target can break by manoeuvring. It costs nothing to implement, because it falls
straight out of "guns rotate on their own."

**Requirements it creates:**
- Gun mounts must be separately authored objects, rigidly parented, animatable
- Turrets must be individually visible, nameable and targetable
- A ship inspector UI showing which turrets are alive
- LOD plan — barrels must not animate at fleet scale

`OPEN` — Gun taxonomy: 3–4 mount classes (point defence / line / flak / spinal) proposed.

### 2.2 Persistent Damage
Damage persists across engagements and is visible on the model.

**The rule that makes it safe:**
> Damage degrades **function, not availability**.

A battered ship fights worse but still fights. It stays in your order of battle. This
preserves the payoff (the fleet you take into the next fight is visibly the one that fought
the last) without the death spiral (losing a fight making the next one unwinnable).

**Damage → function mapping:**
- Destroyed turrets — permanent. An arc that never closes again.
- Weakened shields / engines / accuracy
- Three authored damage states (pristine / damaged / critical), swapped at thresholds.
  Three meshes, not thirty.

**Combined with 2.1, the ship is the health bar.** You read the state of a battle by looking
at the ships, at fleet scale, with no UI. Damage and turrets are the same pillar.

**Economy hooks:**
- Repair at friendly stations — meaningfully cheaper than rebuilding, not free
- Wreck salvage for destroyed hulls (EAW let you tug corpses out for income)

`LOCKED` — Persistent damage is in.
`PROPOSED` — Damage degrades function, not availability. (needs confirm)

### 2.3 Galactic Conquest with Diplomacy
The galaxy is divided between factions that must actually negotiate.

**`LOCKED` — Milestone-based branching endgame:** thresholds unlock branches for
everyone, plus one high threshold that ends the game.

**`LOCKED` — Real-time with pause**, at both the strategic and tactical layers.

#### The design tension
Deep diplomacy *and* combat as the star are in conflict. If diplomacy works well, players
avoid combat and the game we designed never happens.

**Resolution: diplomacy is an amplifier, not a substitute.**

- A **galactic council**, not a peace-treaty menu. Seats, votes, rules. The
  Republic-to-Empire transition.
- **Alliances concentrate fleets** → blocs → a front line. Major battles only occur on
  fronts. Systems behind a front are safe.
- This gives the galaxy *shape* and manufactures the set-piece moments. A galaxy of
  diplomatic hermits produces thirty skirmishes and no drama.
- Alliances carry **obligations** — join their wars, contribute production
- **Trust is an observable number**, so deception is a playable strategy
- **Reputation is global** — betrayal is priced across every faction at once

`PROPOSED` — Council-with-seats as the diplomacy system.
`PROPOSED` — Alliances concentrate fleets, creating fronts.

### 2.4 Economy Management
`PROPOSED` — Three resources, each with exactly one job:
- **Credits** — spend
- **Industry** — build capacity
- **Influence** — diplomacy and faction actions

Plus a fourth that is *ours* rather than Stellaris':
- **Per-fleet supply range.** A fleet draws resupply from friendly infrastructure and has
  a hard operational radius.

**Why supply range matters most:** it generates most of the strategic texture a 4x needs.
Expansion costs logistics, so your empire's *shape* is set by your infrastructure rather
than your neighbours' borders. Overextension is punished naturally, without an artificial
penalty rule. Supply routes become targets, giving static infrastructure the value ground
combat used to give static territory. It makes the space *between* systems matter.

Without it, 4x tends toward "expand everywhere, lose a fleet, rebuild, expand everywhere."
With it you get a real dilemma: *this system is worth taking and I can never hold it.*

---

## 3. Architecture: One Simulation, Two Resolutions

`PROPOSED` — **This is the structural spine of the game and the biggest open design
decision.**

The galaxy is continuous 3D space. Always. No battle screens, no loading, no second game.

But the game runs at two time resolutions, and switching between them is a core verb:

- **Strategic time** — fleets move, production ticks, diplomacy, army transports.
  Real-time with pause, at galactic pace. This is the 4x.
- **Tactical time** — when fleets actually meet, the game *dilates* and the camera drops
  in. Same rules, slower clock, individual hulls, turret arcs, damage states.

**Critical property: both resolutions run the identical simulation.** There is no separate
auto-resolve ruleset. The only difference is whether the player is present to watch and
make micro-decisions.

**Why this is the right answer:**
- EAW solved the two-layer problem by cutting between two separate games, which makes the
  loading screen where the game actually happens
- Stellaris solved it by never having a battle at all
- Neither gives us 4x depth *and* moment-to-moment

**The gift it produces:** a genuine strategic dilemma about **where your attention goes.**
You can't be everywhere. Front A is collapsing. Front B is where your transports are.
Front C is a four-hour siege. Choose.

**The discipline it imposes (a feature, not a bug):** requiring a battle to be fairly
summarisable at low fidelity constrains combat design for us. If a fight needs forty
micro-decisions to win fairly, it can't be abstracted, so it's un-attendable, so it's a
wall of text every time. **We physically cannot design intricate tactics for content that
doesn't matter.** The system prunes our own scope.

**Bonus:** the independent-turret feature is a real-time-only pleasure. At strategic
resolution barrel animation is meaningless noise. The dream mechanic is the strongest
argument for having two resolutions at all.

`OPEN` — One engagement type, or several? EAW had several distinct battle verbs (send
fleet / invade planet / defend). Ours could be fleet action, planetary siege, and
escort-intercept as three genuinely different games.

---

## 4. The Map

`LOCKED` — Small node count. EAW's galaxy is ~10 systems and every one is adjacent to
something that matters. It's a *board*, not a *space*. That's why it's tense in 20 minutes.

**The tension:** 4x depth and EAW's scale pull in opposite directions. Depth comes from
systems interlocking; EAW's scale comes from density. A 200-system galaxy becomes admin.

`PROPOSED` — **Keep the node count small, push the depth inside the node.** A system isn't
a dot — it's a sub-graph of orbits, stations, production and garrison with internal state
that changes. 12 systems with real interiors beats 150 dots.

`PROPOSED` — **Transit is a combat space, not a loading screen.** FTL with real travel
time. The space *between* systems is where intercepts and escort fights happen. Gives
Stellaris' transport-raid tension and EAW' cinematic battles without building two games.

---

## 5. Combat Design

### 5.1 Damage Model — Three Layers
- **Shields** — regenerate, absorb, effectiveness varies by weapon channel
- **Hull** — permanent, persistent damage, never regenerates
- **Subsystems** — engines, shields, weapons, and *every individual turret*. Temporary or
  permanent disable.

Subsystems are what make non-physical ammo interesting. An EMP doing zero hull damage but
killing six turrets for thirty seconds is worth more than a torpedo doing hull damage to
one, because it opens an arc that never closes.

`PROPOSED` — Three damage channels, available to **every** faction:
- **Ballistic** — mass drivers, rails, physical ammo. Low damage, **partial shield
  penetration**. Grinds hull. The answer when shields are holding.
- **Energy** — beams, plasma, shield-burners. High alpha, high efficiency. Loses to
  shields that regen faster than you burn.
- **Disruptive** — EMP, ion, neural. **No hull damage.** Kills subsystems. The only
  reliable answer to a capital's turrets.

`PROPOSED` — Disruption and persistent damage are the same system aimed at different
layers. Turrets destroyed = permanent. Engines and shields disrupted = temporary. Same
targeting code, two clocks, one grammar the player learns once.

### 5.2 Hardpoints, Not Locks

`LOCKED` — **No faction rock-paper-scissors.** Exclusive doctrines create deterministic
matchups, and a 4x with a settled meta is a dead 4x. Every faction has every weapon style.
Capitals carry missiles, lasers and more.

**The principle:** identity comes from **configuration, not access.**

A capital is a set of **hardpoints**. Filling them is a real decision with real costs:
- More line batteries — more damage, more power draw
- More flak — survives missile spam, does nothing else
- Spinal mount — huge alpha, fixed, cannot traverse
- Missile racks — flexible answer to everything, slow enough to be shot down

`PROPOSED` — **Missiles are the keystone.** They track, retarget mid-flight, and ignore
turret arcs, so a missile-armed ship threatens anything. The counterweight is that they're
slow and **flak shoots them down**:

```
missiles  →  flexible, hits anything, must be intercepted
flak      →  kills missiles, useless against everything else
line guns →  reliable, but must converge, and guns are aim-limited
```

A small cycle that resolves **at the engagement, decided by loadout** — not at the faction
level. And because every capital needs flak, flak is a permanent tax on production, which
means **logistics decides how much flak you can afford.** The economy pillar generates a
combat constraint directly.

### 5.3 Asymmetry From Circumstance, Not Capability

`LOCKED` — No faction should be almost undefeatable. Don't build deterministic dominance
into the roster — build it into the situation.

- A faction at war on two fronts fields fewer flak because it can't afford them
- A faction that lost a supply route can't replace damaged line batteries
- A faction that turtled has all missiles and no line

Two factions with identical tech can fight with completely different fleets, and neither is
stronger — they're solving different problems. Variation is **emergent, not authored.**

### 5.4 Tiers

`PROPOSED` — **Tiers must be behavioural, not numerical.** If tiers are numeric the
doctrine triangle collapses and whoever maxes damage-per-shot first wins.

Ballistic example:
- **T1** — single slow shot, minimal penetration
- **T2** — rapid fire: damage down hard, volume way up
- **T3** — **coordinated volley.** Whole battery fires on one convergence point
  simultaneously, with a concentration bonus. *This tier exists only because of the
  independent-turret system* — it's the moment a capital feels like a battery.
- **T4** — accelerator: charges, pierces through a target into the one behind it

`PROPOSED` — What makes factions distinguishable:
1. **The shape of their tech tree** — T4 energy / T2 ballistic fights visibly differently
   from the reverse. Since tech progress is a 4x variable, a faction's identity *changes
   during the game.*
2. **Default loadout doctrine** — what their shipyards build by default. Not a restriction.
3. **One signature tech each** — a single high-tier capability unique to them. Not
   stronger, just different.

---

## 6. Ground Combat & Armies

`LOCKED` — Ground combat is simulated. Armies are transported through space for assaults,
Stellaris-style.

The transport layer plugs the hole ground combat left: **armies are the space-between-systems
risk**, because a transport is the single fattest target in the game.

Two rules to keep it from becoming sterile:
- **Ground combat simulates, but ground war has faces.** Armies get named commanders and a
  visible combat forecast. Two strength numbers auto-resolving is death in a space opera.
- **Lose the ground war, degrade — never raze.** A captured-then-lost system drops a
  production tier and needs a garrison rebuilt. Razing gives full-Stellaris reset pacing
  and makes the galaxy fragile in a boring way.

`PROPOSED` — The landing decision is the most tense moment in the game and it's pure space
play: you have a fleet, an army, and a target. What goes in first, what screens the
landing, what happens if the transport dies over the enemy.

---

## 7. The Setting

`LOCKED` — Original IP, operatic space opera, major and minor powers.

`PROPOSED` — The working premise, from the title: **the galaxy broke, and the institutions
of the old order still pretend to function.**

- **Major powers** — successor states, whoever seized the big pieces of the old empire's
  infrastructure
- **Minor powers** — what's left over, still flying colours that don't mean anything
- **The council** — exists, has seats, passes resolutions, controls almost nothing. The
  pre-rupture order performing legitimacy while actual power sits with whoever took the
  fabs

This makes galactic diplomacy *about a lie*, which is far more interesting than peace
treaties — every council session is theatre, and the interesting play is deciding when to
stop pretending.

---

## 8. Technology

`LOCKED` — **Unity 6 (6000.6.3f1), Universal Render Pipeline, C#.** All implementation work
done with Claude, driven from the terminal through the `unity` CLI.

This replaces the original "no game engine" decision (NeoVeldrid + Silk.NET). The engine-free
stack cost too much time on plumbing — windowing, a renderer, an asset loader, UI — before any
of it made the game look good. The goal is now a *stunning* game, and the visual ceiling of a
hand-rolled renderer was the bottleneck.

### 8.1 The Lock-In Principle, Restated for an Engine

We now depend on an engine's scene format and editor. The principle that survives is the one
that always mattered:

> **The rules of the game do not live in the engine.**

- `GalacticRupture.Sim` is a separate assembly with **no `UnityEngine` reference**
  (`noEngineReferences: true`, enforced by a test). Deterministic, fixed tick, runnable
  headless from a test or a batch job.
- Unity owns presentation: meshes, materials, lights, VFX, cameras, UI, input.
- The Game layer reads sim state and draws it; it never decides an outcome.

If Unity ever had to go, the sim and its tests leave with us intact.

### 8.2 Render Pipeline: URP

`LOCKED` — **URP, not HDRP** (user decision, 2026-09-29).

| | Why |
|---|---|
| Scale | URP is lighter per frame and scales to much larger fleets (§8.8). HDRP would cap us near 50–150 capitals |
| Hardware | Development machine is a GTX 1080 Ti: no hardware ray tracing, so HDRP's high end is unavailable anyway |
| Look | Space is mostly black with one key light (the star). The look comes from HDR bloom, emissives, VFX and good models, all of which URP does well |

The cost: volumetrics and lighting fidelity take hand-built shader work instead of arriving in
a settings panel.

### 8.3 Project Layout

| Assembly | Folder | Role |
|---|---|---|
| `GalacticRupture.Sim` | `Assets/Scripts/Sim` | Rules. No engine. Fixed tick, deterministic |
| `GalacticRupture.Game` | `Assets/Scripts/Game` | Unity side: sim runner, views, camera, input, UI, VFX hooks |
| `GalacticRupture.Tests.EditMode` | `Assets/Tests/EditMode` | NUnit tests against the sim |

Art under `Assets/Art/{Models,Materials,Shaders,VFX}`, prefabs under `Assets/Prefabs`, data
under `Assets/Data`.

### 8.4 Stack

| Layer | Choice |
|---|---|
| Engine | Unity 6000.6.3f1 |
| Rendering | URP 17.6, Shader Graph, VFX Graph |
| Camera | Cinemachine |
| Input | Input System |
| Performance | Burst, Collections, Mathematics; GPU instancing for fleets |
| UI | UI Toolkit (uGUI only where a package needs it) |
| Tests | Unity Test Framework (NUnit), EditMode, via `unity test` |
| Models | Blender through the Blender MCP, exported to glTF/FBX |
| Sim math | `System.Numerics` — available without an engine reference |
| Physics | **None in the sim.** Sphere overlap and ray-vs-sphere, written by us |

### 8.5 Architecture Notes

- The sim steps at a **fixed tick**; the Game layer interpolates between the last two ticks
  for rendering. Pausing and time dilation (§3) are changes to how many ticks run per frame,
  never to the rules.
- Game objects are **views**: a ship view holds a reference to a sim ship id and copies its
  pose each frame. Views can be destroyed and rebuilt from sim state at any time.
- `PROPOSED` — **Data is authored as ScriptableObjects** in the Game layer (edited live in the
  Inspector, which gives hot tuning for free) and converted to plain C# definitions the sim
  consumes. The sim never sees a Unity type.
- Build tools (scene builders, benchmark scenes) are editor scripts that construct scenes
  from code, so a scene can always be regenerated and nothing is hand-edited YAML.

### 8.6 Format Discipline

> The meshes belong to the art tool. The ship's *layout* belongs to us.

- **Meshes:** glTF or FBX from Blender.
- **Turret mount points, arcs, hardpoint classes and damage-state swaps live in our hull
  definitions,** not in the mesh, so a hull can be re-exported or remodelled without breaking
  its weapon layout.
- Every turret is a **separate mesh with its own pivot** (yaw on the base, pitch on the
  barrels), instanced from a shared parts kit.

### 8.7 Determinism

Buy determinism by **structure**, not by contorting the code:

- Separate assembly, fixed tick, all randomness from a seeded sim RNG (never
  `UnityEngine.Random`, never `System.Random` shared across systems).
- No iteration over unordered collections where order affects the outcome.
- Transcendentals (`Atan2`, `Sin`, `Sqrt`) go through one `SimMath` class. `+ - * /` are
  IEEE-754 and safe; libm is not bit-identical across platforms. Single-player on one platform
  makes plain `MathF` fine today, and the wrapper means a table-based version can drop in if
  lockstep multiplayer or cross-platform replays ever matter.
- A **state hash** each tick makes "same seed, same outcome, attended or not" testable (§3).

### 8.8 The Number That Gates Everything Else

How many ships can we draw at 60fps, with shadows, at target camera distance, on a GTX 1080 Ti?
EAW had ~30 on screen. Stellaris has 10,000. **That is a different game** — different tactical
layer, camera, selection model, and AI cost. The combat layer cannot be sized until we know
which number we're near.

A benchmark scene answers it: N instanced hulls with turrets, one shadow cascade, bloom on, no
gameplay. Measure at three camera distances. Re-run when real models land.

**First result (2026-09-29, `Tools/bench.sh`, GTX 1080 Ti, 1080p, Blender models, mixed hulls):**

| Ships | Close (700 m) | Mid (3.5 km) | Far (14 km) |
|---|---|---|---|
| 100 | 252 fps | 192 fps | 204 fps |
| 250 | 235 fps | 153 fps | 100 fps |
| 500 | 148 fps | 105 fps | 53 fps |
| 1000 | 87 fps | 69 fps | 29 fps |
| 2000 | 47 fps | 40 fps | 19 fps |

The sim costs under 3 ms at 2000 ships once targeting uses a spatial grid. Rendering is the
limit, and it falls off at the far view, where every ship and turret is on screen with full
detail; the far-distance LOD in §2.1 (merged mounts, no barrel animation) is the fix.
**Working size: a few hundred ships per battle** — EAW's scale ×10, well short of Stellaris.
Combat design is sized for that.

### 8.9 The Visual Bar

`LOCKED` — **Stunning is a requirement, not polish.** The game is judged by feel and look.

`PROPOSED` — What stunning means for us, concretely:

- **One star lights the battle.** A strong directional key light with the star's colour, a
  faint cool fill, deep blacks. Ships read by silhouette and rim light, not ambient mush.
- **HDR everywhere.** Engines, muzzle flashes, beams, shield impacts and hull fires are
  emissive and bloom. Tonemapped (ACES or Neutral), with exposure tuned per system.
- **A sky worth looking at.** A procedural starfield and nebula backdrop per system, plus the
  system's star and planets as real objects at a distance.
- **Guns you can watch.** Barrels traverse visibly, recoil, flash; tracers and beams are bright
  and readable at fleet scale. The converging battery is the signature image (§2.1).
- **Damage you can read.** Scorching, venting, fires, debris, listing — the ship is the health
  bar (§2.2).
- **Cinematic camera.** Close, low, restricted orbit in tactical view; an establishing pass
  when an engagement opens.
- **Models at a fixed quality bar.** Practical warships whose shapes follow from what a power
  at war needs (armoured bows, centreline superfiring guns, radiators, redundant engines), not
  art objects. No ventral guns; short, chunky barrels in proportion to the gunhouse.

### 8.10 Toolchain

```sh
unity run . --editor-version 6000.6.3f1 -- -executeMethod <Class.Method>   # batch jobs, scene builders
unity test . --editor-version 6000.6.3f1 --mode EditMode --output <file>   # sim tests
unity build . --target StandaloneLinux64 --execute-method <Class.Method>   # player builds
unity status / unity command ...                                           # drive a live Editor
```

---

## 9. Build Order

`PROPOSED` — Playable early, playable always. Each step ends with something to run and judge.

1. **Turret toy.** A capital in a lit space scene, its turrets tracking targets independently
   with finite traverse, firing tracers. Proves §2.1 and sets the visual bar.
2. **Fleet benchmark.** §8.8. Sizes everything after it.
3. **Ship duel.** Shields / hull / subsystems, three damage channels, movement, orders,
   facing. 2v2 where facing and convergence decide the outcome.
4. **Fleet engagement.** 10–50 ships, doctrine AI, end states, the unattended resolution and
   the report.
5. **Mini campaign.** A handful of systems, two factions, supply range, production, persistent
   damage carried between battles.
6. **Deepen**, one layer at a time: economy, armies and transports, council and diplomacy.

---

## 10. Open Questions

1. **One engagement type or several?** Fleet action, planetary siege, escort-intercept as
   three different games, or one universal brawl?
2. **Gun taxonomy** — how many mount classes, and bound to hulls or to hardpoints?
3. **The three majors** — mirrors with one real ideological difference each, plus their
   doctrine differences and default loadouts.
4. **Minors** — do they fall out as the powers that picked a side?
5. **Tick rates** at strategic and tactical resolution.
6. **Does the two-resolution system get confirmed?** It is the structural spine and nothing
   above is fully designed without it.
7. **Map size and travel** — 12 systems or more, and FTL transit (§4) versus fixed gates.

---

## 11. Change Log

- Initial document created from the opening brainstorm: vision, four pillars, two-resolution
  architecture, map, damage model and hardpoints, armies, setting, tech stack, open questions.
- Expanded §8 into a record of the engine-free technical decisions (C#, NeoVeldrid).
- **2026-09-29 — Fresh start.** The previous implementation, plans and design docs were
  retired; this file is the only design source carried forward. §8 rewritten for Unity 6 +
  URP (engine-free stack dropped, lock-in principle restated as "the rules don't live in the
  engine"), added the visual bar (§8.9) and build order (§9).
