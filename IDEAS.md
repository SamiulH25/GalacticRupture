# Galactic Rupture — Design Ideas

Living brainstorm document. Updated as we go.

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

`LOCKED` — **No game engine.** Godot/Unity/Unreal are off the table. But frameworks and
libraries are fine and will be used. The requirement is *no lock-in*, not *no code reuse*.

### 8.1 The Lock-In Principle

`LOCKED` — What "tied down" actually means, precisely:

An **engine** owns your scene format, asset pipeline, editor and architecture — you fight
it for control. A **graphics library** owns the draw call, nothing else. bgfx's own tagline
is *"Bring Your Own Engine/Framework"*; it explicitly declines to dictate pipelines,
meshes, cameras, lights or materials. That is almost a word-for-word description of the
requirement.

**The operative rule is in §8.6 — never let a library's file format become your save
format.** Freedom lives in the data model, not the library choice.

### 8.2 Language: C# / .NET

`LOCKED` — **C# / .NET.** All implementation work done with Claude.

**Why C# won:**

1. **Hot reload turns design iteration from a compile-wait loop into a save-and-reload
   loop.** This is the decisive reason. When you're tuning whether persistent damage
   snowballs, or whether turret traverse rates feel fair, that's the difference between
   testing 40 variants this afternoon and 3 this week. *We are design-iterating
   constantly; that is the dominant workload.*
2. **Headless simulation is trivial**, which makes AI-vs-AI balance testing a first-class
   tool rather than a project.
3. **The 3D ecosystem is now sufficient.** NeoVeldrid + Silk.NET cover rendering, input,
   audio and asset loading. There is no longer a reason to leave C# for C++.

**Rejected alternatives:**

| Option | Why not |
|---|---|
| C++ / bgfx | Most prior art, best raw perf — but slow rebuilds, manual memory, a real learning curve on the GPU layer. Iteration speed is the bottleneck for us, not frame time. |
| Rust / wgpu | Philosophically the purest no-lock-in choice, and genuinely good. Rejected on weaker AI-assist ergonomics and a thinner 3D ecosystem for what we need. |
| Zig | wgpu available; rejected on ecosystem and tooling. |

**Honest caveat:** Silk.NET is maintained by volunteers and its 2.x line is in maintenance
mode with 3.0 in progress. NeoVeldrid is pinned to Silk.NET 2.x. This is a real, accepted
risk — the mitigation is that it's a *bindings* library, and the abstraction it provides
(Silk.NET 3.0) is unlikely to change. Never let it become our save format and we're safe.

### 8.3 Rendering Framework: NeoVeldrid

`LOCKED` — **NeoVeldrid** (maintained fork of Veldrid, MIT, targets .NET 10, ImGui
integration built in, pure C# shader cross-compilation).

> **Warning for future reference:** the *original* **Veldrid is dead.** Its author stepped
> back in Feb 2023 and the repo is abandoned. If you go looking for "Veldrid" you will find
> the dead repo first. **NeoVeldrid** is the live fork (v1.2.0, July 2026).

**Alternatives considered:**

| Option | Why not |
|---|---|
| bgfx | Excellent and very much alive — the best-in-class C++ choice. Rejected *only* because we're in C# and it has no first-class .NET story. Would be the recommendation in a C++ project. |
| Raw Vulkan | Maximum freedom, maximum cost. We don't need to own the GPU layer for the requirements in §8.1. |

**Rendering scope** — deliberately narrow, so the renderer does exactly one job:
- Fixed camera angles (no free orbit)
- Hard content cap: a few dozen hulls, one material model
- Single shadow cascade, tightly fitted to camera focus
- Instanced draws: one draw call per ship type per LOD
- Depth prepass on opaque geometry
- LOD swaps at distance (barrel animation culled at fleet scale)

### 8.4 Full Stack

**Stack:**

| Layer | Choice |
|---|---|
| Language | C# / .NET 10 |
| Rendering | NeoVeldrid (maintained fork of Veldrid, MIT, .NET 10) |
| Window / input | Silk.NET.SDL |
| Math | System.Numerics |
| Audio | miniaudio via Silk.NET |
| UI | ImGui.NET (dev tooling; skin or replace for ship) |
| Text | MSDF atlas — lock in early |
| Data | TOML — hot-reloadable, schema-validatable, diffable |
| Physics | **None.** We need sphere overlap, not rigid bodies |

### 8.5 Architecture Notes

- `Game.Sim` has **zero** rendering references. Deterministic, headless-runnable.
- Everything scriptable lives in hot-reloadable C# — scenarios, events, AI, balance
  constants. This is what actually buys the iteration speed in §8.2.
- Build the map and system editors **inside the game client**, not as a separate tool —
  same camera, selection and gizmo code, one UI instead of two. Pair with a plain text
  view for scripts and data; hot reload closes the loop.

### 8.6 Format Discipline — The One Rule That Buys Freedom

> Never let a library's file format become your save format.

- **Meshes: glTF.** Be compatible with the art ecosystem; purity here is a mistake — we'd
  be reimplementing an importer for no gain.
- **Scenes: our own `.grscene`.** Composition, LOD, turret mount points and arcs,
  damage-state mesh swaps, metadata.
- Turret mount points live in *our* format, not the mesh, so hulls can be re-exported from
  a new DCC tool without breaking weapon layout.

This is the difference between "we use Godot" and "we use NeoVeldrid." Both mean depending
on someone else's code. Only one means depending on someone's *data model*. If the editor
writes a format only our own code reads, swapping the renderer is a month of work rather
than a rewrite.

### 8.7 Determinism in C#

**The actual gotcha:**
- `+ - * /` and comparisons on `float` are IEEE-754 and safe
- **Transcendentals are not** — `MathF.Sin` etc. delegate to platform libm and are not
  bit-identical across platforms. This is the real killer and it looks like ordinary math.
- Sim uses no transcendentals. Rotations via quaternions or angle tables.
- Positions as `double` in sim, `float` at render.

Scope note: if single-player on one platform, plain `float` is fine. Buy determinism by
*structure* (separate assembly, fixed tick, no libm in the sim), not by contorting the code.

### 8.8 The Number That Gates Everything Else

How many ships can we draw at 60fps, with shadows, at target camera distance? EAW had ~30
on screen. Stellaris has 10,000. **That is a different game** — different tactical layer,
camera, selection model, and AI cost model. We cannot design the combat layer until we know
which number we're near. Benchmark a throwaway stress scene first: 10,000 instanced ships,
one shadow cascade, depth prepass, no gameplay.

---

## 9. Open Questions

1. **One engagement type or several?** Fleet action, planetary siege, escort-intercept as
   three different games, or one universal brawl?
2. **Gun taxonomy** — how many mount classes, and bound to hulls or to hardpoints?
3. **The three majors** — mirrors with one real ideological difference each, plus their
   doctrine differences and default loadouts.
4. **Minors** — do they fall out as the powers that picked a side?
5. **Tick rates** at strategic and tactical resolution are open.
6. **Does the two-resolution system get confirmed?** It is the structural spine and nothing
   above is fully designed without it.

---

## 10. Change Log

- Initial document created from the opening brainstorm. Captures: vision, four pillars,
  two-resolution architecture proposal, map philosophy, damage model and hardpoint
  principle, army layer, setting premise, tech stack, and open questions.
- Expanded §8 into a proper record of the technical decisions: added the lock-in principle,
  the C# rationale with rejected alternatives, the NeoVeldrid rationale (with a warning
  that the original Veldrid is dead), rendering scope, format discipline, and the
  benchmark that gates combat design. Previously these were recorded as bare choices with
  no reasoning, which would have made them impossible to revisit or defend later.
