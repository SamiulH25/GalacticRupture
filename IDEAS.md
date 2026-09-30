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
~~`LOCKED` — Combat is the star. Everything else serves it.~~ Superseded 2026-09-29 by §1.1:
strategy is where the time goes; combat is the reward.
`LOCKED` — Operatic space opera, original IP.
`LOCKED` — Full 3D.
`LOCKED` — Ground combat is **simulated**. Armies are transported through space for
assaults, in the manner of Stellaris.
`LOCKED` — Major and minor powers (2–3 majors as mirrors, 2–3 minors as pawns).

### 1.1 Scope and Game Shape

Decided 2026-09-29. Where this conflicts with older text below, this wins.

`LOCKED` — **A long campaign.** One campaign is played over about two weeks, 1–2 hours a day
(roughly 15–30 hours). Saves, a mid game and a late game are therefore required, not extras.

`LOCKED` — **Strategy is the activity; combat is the reward.** Most of the player's time goes
into building the economy, designing and building fleets, preparing fronts and choosing whom
to fight. A battle is the payoff for that preparation: where the plan is tested and the fleet
you built either holds or doesn't.

`LOCKED` — **The player chooses, per engagement, whether to command it** in the RTS or leave
it to the fleet. Both go through the same tactical sim (§3).

`LOCKED` — **Full vision, rough first.** The first complete version has every layer (economy,
logistics, diplomacy/council, tech, designer, armies, RTS combat) working together at rough
quality, then each is deepened. No layer is cut to polish another.

`LOCKED` — **The galaxy is 60–120 systems**, shared by **4–5 major powers and 8–12 minors**.
All majors are playable (they are mirrors; §5.4 gives their differences).

`LOCKED` — **The campaign has a three-act arc:**
1. **Rupture** — the old order has just broken. Powers reclaim derelict old-empire systems,
   court or absorb minors, and position against each other.
2. **Blocs** — alliances harden into blocs, the council fractures, fronts form between them.
3. **Total war** — bloc against bloc for the old capital and the endgame thresholds (§2.3).

`LOCKED` — **Expansion is reclamation, not colonisation.** There is no empty space to settle.
Much of the galaxy is derelict old-empire territory (dead fabs, drifting stations, cut-off
holdouts); powers send expeditions to reclaim and restore it. When the ruins run out, growth
comes only from diplomacy or conquest, which pushes the game from act 1 into act 2.

`LOCKED` — **Four strategy layers, all core:** economy & planets, logistics & fronts,
diplomacy & council, tech & doctrine.

`LOCKED` — **System economy is specialisation, not pops.** Each system has population,
stability and a small number of building slots (fab, yard, refinery, garrison, research…).
Deep enough to plan with, light enough to manage thirty of them.

`LOCKED` — **Full hardpoint ship designer.** The player fills each hull's hardpoints
(§5.2); the configuration is the faction's identity.

`LOCKED` — **Governors.** The player can appoint a governor to a system. Governors have stats,
and a governor auto-builds the system. In return they take a cut of what the system produces
(`PROPOSED` about 5%, possibly varying with the governor's stats). This is the main tool for
managing scale: hand off the systems you don't want to micromanage, and pay for it.

`LOCKED` — **Session rhythm.** A 1–2 hour sitting should give a couple of battles and at least
3–4 systems gained (reclaimed, absorbed or taken). A campaign is a sequence of these sittings.

`LOCKED` — **What stops the clock:** battles and council votes. Nothing else does; everything
else notifies.

`LOCKED` — **Landings are automatic.** A fleet carrying an army that is over an enemy planet
lands it without asking. The decision is made earlier, when you send the fleet. Armies can
also be stationed on your own planets as a defensive garrison.

### 1.2 Assumed Defaults

Filled in by Claude on 2026-09-29 when the user said "just assume the rest". All `PROPOSED`:
they stand until changed, and none needs sign-off before building.

- **Map: about 100 systems** (the top of §1.1's range). At 3–4 systems a sitting over ~14
  sittings the player gains 40–55, so the map must be big enough for that, and systems are
  expected to change hands back and forth in acts 2–3. Generated from a seed, not hand-placed.
- **Travel: FTL lanes with real travel time**, as built. Fleets move only along lanes, and a
  lane is a combat space where fleets can be intercepted (§4). No gates, no free travel
  through open space. Lanes are what make choke points, fronts and supply lines readable.
- **Governors:**
  - Three stats, 1–5: **Administration** (build speed and cost), **Industry** (bonus to the
    system's output), **Loyalty** (stability; a disloyal governor skims more and can defect
    with the system if the power is losing). Plus one trait.
  - Cut is 5% of the system's output, from 3% for a loyal governor to 8% for a greedy one.
  - The player sets a **focus** (industry, military, research, balanced); the governor
    decides what to build within it.
  - They come from a small candidate pool per power that refreshes over time. Reclaimed
    systems sometimes offer a local notable, and retired army commanders can be appointed.
- **Admirals, not front AIs.** A fleet can have an admiral with stats that apply when its
  battles are left to the fleet. There is no automated front commander yet; positioning
  fleets on fronts is the strategy the player came for.
- **One tactical sim, several setups** (§10.1). Battles are the same RTS in different
  situations: open lane (intercept/escort), a system with stations and defence platforms
  (siege), and a system fight between fleets. The setup changes the map, not the rules.
- **Gun mounts are bound to hardpoints, not hulls** (§10.2). A hardpoint has a size (S/M/L) and
  a type (turret, spinal, missile, utility). Mount classes: point defence (anti-missile and
  anti-small craft), line batteries (S/M/L), missile racks, spinal. The designer fills
  hardpoints from these.
- **Minors** are pawns. They never expand. A major absorbs one by holding enough trust and
  paying influence over time, or conquers it. A minor aligned with a bloc lends its seats.
- **Time:**
  - The tactical sim runs at a fixed 30 ticks a second. The strategic sim ticks every in-game
    hour.
  - At normal speed a day takes about 20 s, so an hour of strategic play is about six in-game
    months and a campaign is 6–8 in-game years.
  - The council sits **every 60 days** instead of 20, so a sitting has a few votes rather than
    a dozen clock stops.
- **Two resolutions, confirmed as built** (§3, §10.6). A battle stops the strategic clock and
  opens the tactical view on the same sim, attended or not. A seamless camera zoom from the
  galaxy into the battle is deferred as presentation, not a rule change.
- **The majors' identities** (names, ideologies, signature techs) are setting work for a
  later session. The campaign's four existing powers stand in until then.

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

~~`LOCKED` — Small node count. EAW's galaxy is ~10 systems and every one is adjacent to
something that matters. It's a *board*, not a *space*. That's why it's tense in 20 minutes.~~
Superseded 2026-09-29 by §1.1: 60–120 systems for a long campaign. The goal that survives:
every system is named and matters, and the tension comes from fronts and supply rather than a
small board.

**The tension:** 4x depth and EAW's scale pull in opposite directions. Depth comes from
systems interlocking; EAW's scale comes from density. A 200-system galaxy becomes admin.

`PROPOSED` — **Keep the node count small, push the depth inside the node.** A system isn't
a dot — it's a sub-graph of orbits, stations, production and garrison with internal state
that changes. 12 systems with real interiors beats 150 dots.

`PROPOSED` — **Transit is a combat space, not a loading screen.** FTL with real travel
time. The space *between* systems is where intercepts and escort fights happen. Gives
Stellaris' transport-raid tension and EAW' cinematic battles without building two games.

### 4.1 The First Campaign Map (as built; now the test map)

`PROPOSED` — the rules the campaign runs on today, to confirm or change:

- **16 systems, joined by FTL lanes; four powers.** The Continuance (player) and the Remnant are
  at war; two minors, the Serac Compact and the Ilium Directorate, start at peace with everyone.
  Travel only along lanes, at the speed of the slowest hull. Fleets can turn back mid-lane.
- **Tiers 1–3.** Income 22 / 50 / 110 credits and 0.5 / 1 / 3 influence a day; tier 2+ has a
  shipyard, a capital yard builds two hulls at once.
- **Supply range 14 ly** from any owned or allied system. Out of supply, nothing repairs.
- **Repair at friendly yards:** hull back over days, lost turrets replaced one at a time.
- **Fog of war:** systems see 7 ly, fleets 5 ly; allies share. Everything else is last known,
  with its age. Everyone starts with the charts (who held what), not with fleet positions.
- **Armies take systems, not fleets** (§6). Armies (named commanders, four traits) are raised at
  yards and carried by transports, one each. Landing is automatic when the fleet is over the target (§1.1); the ground
  war runs hour by hour against garrisons and militia, fortified at capitals. Troops die with
  their transport. A captured system sits in unrest for 3 days; half-built hulls are lost.
- **Transit is a combat space** (§4): hostile fleets meeting on a lane fight there; fleets
  passing through a contested system are caught.
- **Battles stop the clock** for the player: take command or leave it to the fleet — the same
  tactical sim either way, with real damage carried out (§2.2, §3). Battles between AI powers
  resolve unattended through the same sim. The loser, or whoever withdraws, falls back.
- **Diplomacy** (§2.3): war, peace, non-aggression, alliance. Trust moves with shared enemies,
  common threats, border friction and aggression; breaking a treaty costs trust with everyone.
  Allies are drawn into each other's wars. Treaties cost influence; AI powers make offers.
- **The council** sits every 20 days (60 in the long campaign, §1.2): censure of the aggressor (an embargo), a general
  ceasefire, recognition of conquests, or an embargo a member tabled with influence. Seats are
  systems held. It stops the clock for the player's vote.
- **Win** by taking Veyl; **lose** by losing Aurel.

### 4.2 The Long Campaign (as built)

`PROPOSED` — the rules the long campaign (§1.1, §1.2) runs on as of 2026-09-30. Numbers are first
tuning from headless runs of the AI playing itself, not from play.

- **Galaxy.** Generated from a seed: ~100 systems in an ellipse ~170 × 120 ly, lanes from the
  relative-neighbourhood graph plus some longer loops. Five majors on the rim (Continuance,
  Remnant, Hierarchate, Free Yards, Lantern Synod — placeholder names until the setting pass),
  each with a capital and three core worlds; ten minors of 1–3 systems in the gaps; the old
  capital at the centre; everything else (~60 systems) derelict: 70% ruins, 30% holdouts.
- **Systems.** Population grows slowly toward habitability (+3 per habitat); stability 0–100
  drifts 2/day toward a target set by admin (+10), garrison (+5), capital (+15), governor loyalty,
  and distance from the capital (−2.5 per jump, up to −20); unrest, sieges and embargoes lower it.
  Below 50 stability, output falls.
- **Buildings,** one per slot (2–8 slots), one under construction at a time: fab (+8 industry),
  yard (builds hulls; two yards build two at once), refinery (+20 cr), garrison (+40 militia,
  +5 stability, raises armies), administration (+1 influence, +10 stability), habitat (+3
  population ceiling), supply depot (+8 ly supply range from that system).
- **Output a day:** credits 3 × population + refineries; industry 0.5 × population + fabs;
  influence 0.2 + admin (+2 at the capital). Industry speeds the yard (0.5×–3×) and construction.
- **Upkeep:** 1.5% of a hull's cost a day, 1 cr per army. This is what caps navies.
- **Governors:** as §1.2, with five focuses: balanced, economy, industry, military, research.
  Appointing costs 10 influence. Candidate pool of three per power, one replaced every 30 days.
- **Reclamation:** a ruin costs 150 + 60 per dormant building and takes 30 + 8 days per dormant
  building with a fleet on station and no hostile warships; work pauses when the fleet leaves.
  Restored ruins come back with their old buildings online. A holdout is courted for 30 + 6 ×
  population influence over 30 days on station, or taken by landing (militia 20 × population + 30).
  The old capital can only be taken by force.
- **Admirals:** 8 influence, tactics and nerve 1–5. Unattended, a side withdraws when it falls
  below ~35% of its starting strength (earlier with low nerve); tactics shortens reloads up to 10%.
- **Designer:** hardpoints are Small / Medium / Large; a mount fits its size or larger. A
  design's cost is the hull plus its mounts; build time scales with it. Only researched guns can
  be fitted. Missiles and spinal mounts wait on tactical support.
- **Research:** 0.05 × population plus 3 per laboratory a day (a capital starts with one). Tier
  costs 300 / 1,200 / 5,000 / 14,000. The player queues technologies (prerequisites come along);
  what is not spent is banked. Research improves hulls **as they are built** (shields, armour,
  speed); guns are fitted in the designer. 29 technologies in six branches:
  - *Ballistic:* rotary cannon → rotary driver / railgun → heavy railgun → mass accelerator.
  - *Energy:* heavy turbolaser → pulse laser → beam lance → shield burner.
  - *Disruptive:* ion cannon → light ion cannon → heavy ion cannon → ion doctrine (reload −10%).
  - *Defence:* flak → heavy flak; deflector lattice (shields +20%), layered armour (hull +15%) →
    reactive shields, fortress worlds.
  - *Industry:* automated fabs, orbital refining, research institutes → arcologies, slipway automation.
  - *Logistics & command:* salvage doctrine, forward depots → lane drives, civic administration,
    drop doctrine → staff college.
- **AI doctrine** (IDEAS §5.4, the shape of the tree is identity): Continuance energy and
  defence; Remnant ballistic and logistics; Hierarchate disruptive and industry; Free Yards
  ballistic and industry; Lantern Synod energy and disruptive. The AI redraws its designs with
  the best guns it knows whenever it learns a technology.
- **Debt:** below −300 credits, a power loses its cheapest hull a day to desertion and its
  worlds' stability slips. Eliminated powers' fleets disband.

`PROPOSED` — **Weapon families.** Every gun belongs to a family with its own silhouette, round
and flash, and comes in sizes (light / standard / heavy):

| Family | Guns | Look |
|---|---|---|
| Mass driver | mass driver, heavy mass driver | armoured gunhouse, thick barrels with brakes; amber shells |
| Autocannon | autocannon | small house, one long barrel, side drum; short amber rounds |
| Rotary | rotary cannon, rotary driver | round drum, ring of barrels; a hail of tiny tracers |
| Railgun | railgun, heavy railgun | low wedge, flat twin rails with a glowing gap; long white streaks |
| Accelerator | mass accelerator | one huge coil-wound barrel; a violet-white bar that passes through ships |
| Turbolaser | light turbolaser, turbolaser, heavy turbolaser | dome with finned emitters and hot tips; bolts in the side's colour |
| Pulse | pulse laser | small dome, 2×2 emitters; short rapid bolts |
| Lance | beam lance | long house, one square emitter with a glowing slit; a long heavy bolt |
| Shield burner | shield burner | dome, emitters ringed with glowing coils; magenta bolts |
| Ion | light ion, ion cannon, heavy ion | drum house, pronged fork with a coil; fat soft blue bolts |
| Flak | flak battery, heavy flak | box behind a shield plate, four stubby barrels; short yellow dashes |
| Point defence | point defence | ball mount, twin needles; tiny white tracers |

The looks are code-built placeholders; a Blender model named for the mount id replaces one.

- **Acts:** act 2 begins when 85% of the ruins are taken or at day 540; act 3 when a bloc holds
  half the galaxy after a year of act 2, or at day 1460. In act 3, holding the old capital for 90
  days wins (or loses, if a rival does). The last major standing wins at any time.
- **Time:** ~20 s a day at ×1, speeds ×0.5–×16; autosave every 30 days; F5/F9 quicksave/load.
- **AI:** develops systems within a cash reserve, appoints governors, keeps one expedition
  working the ruins, builds within its upkeep, stages fleets toward its wars. War declarations
  need trust below −45 in act 1, −25 in act 2, −10 in act 3.

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
play: you have a fleet, an army, and a target. Since landings are automatic (§1.1), the
decision is made when you *send* the fleet, so the forecast belongs on the move order. What goes in first, what screens the
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

### 7.1 Proposed Long-Campaign Faction Roster

`PROPOSED` — The long campaign has **five major powers and eight minor powers**. This
resolves the roster target in §1.1 while preserving the identities already present in the
prototype. Serac Compact and Ilium Directorate are currently marked minor in the 16-system
prototype map; this proposal promotes them to major powers in the full campaign. That is a
lore and campaign-scope decision, not a change to the prototype map.

#### Fifth major: the Hearthward League

The Hearthward League claims that a state earns legitimacy by keeping its people alive,
not by inheriting a capital, a council seat or an old imperial seal. It grew around the
emergency habitat, life-support and evacuation networks that kept isolated populations
breathing after the rupture. Those networks are now its territory, its bargaining power and
the reason its neighbours cannot ignore it.

Hearthward is a major power because it can sustain and move populations across several
systems, not because it owns the largest battle fleet. Its navy is built around convoy
protection, repair under fire and holding an evacuation corridor long enough for transports
to clear. In war it will abandon a system before sacrificing its residents, then return when
it can defend them. Its blind spot is paternalism: it calls forced relocation temporary
protection, and treats local refusal as a danger it is entitled to override.

At the council it demands recognition and protection for displaced populations. This puts
it at odds with powers that treat a system's industry, strategic position or formal title as
the first claim to its future. Its visual identity should read as a working relief fleet
hardened by repeated wars: charcoal hulls, warm ivory panels and hazard amber, with broad,
protected transport and service sections. That gives its ships a practical, unmistakable
profile distinct from the existing spearhead-like warships.

#### Minor powers

These are eight new minor powers for the full campaign. Each holds a specific piece of
leverage that makes its council seat and its fate worth contesting; none needs a unique
weapon or exclusive hull class.

| Minor power | What it holds | Pressure point |
|---|---|---|
| **Morrow Passage Authority** | Pilots, traffic records and service stations at the Morrow routes. | Can delay a fleet without defeating it; claims it is enforcing neutral passage rules. |
| **Thule Archive Enclave** | Pre-rupture registries, survey records and competing copies of imperial law. | Every major wants its records authenticated; the Enclave survives by releasing only enough truth to stay useful. |
| **Oriel Salvage Charter** | Wreck fields, salvage rights and the crews who can recover old-empire matériel. | Its claims keep wreckage from becoming free loot, but a blockade can starve its people of replacement parts. |
| **Hadran Yard Cooperative** | Skilled repair crews and a small, badly damaged orbital yard. | Can keep a battered fleet in service, but must choose whose contracts get priority. |
| **Silex Prospectors' Union** | Remote ore and isotope claims worked by independent extraction ships. | Can shut down exports to resist absorption; its scattered crews are hard to protect from raiders. |
| **Edda Habitat Trust** | Life-support expertise and several densely populated habitat stations. | It can keep a system habitable through a siege, but its residents fear becoming a strategic asset owned by a major. |
| **Vesper Courier Houses** | Couriers and relay caches carrying messages along routes the old network no longer serves reliably. | They sell speed and privacy to every side, until a bloc demands exclusive service. |
| **Nadir Watch** | A chain of remote observatories and warning posts on an exposed frontier. | Its early warning buys neighbours time; no one agrees who should pay to keep its crews supplied. |

The minor powers should have their own reasons for bargaining, resisting absorption or
changing sides. Their value is political and strategic leverage, not a set of disposable
colour-coded planets. Their final system locations, names and council-seat rules remain
`OPEN` until the long-campaign map is designed.

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

**`LOCKED` — Main ship reference images (user approved, 2026-09-29):**
[output/ship-references/main/](output/ship-references/main/README.md). Four battleship plates
and four smaller-class comparison sheets cover the twenty current faction/class combinations.
Classes must have distinct role-specific hull structures, not a battleship resized with fewer
guns. Carry faction identity through construction, materials, components and paint. Keep clear,
modest command/deck areas and practical wartime engineering. Earlier image folders are iteration
history; use `main/` for future ship art and modelling. This approves the visual direction, not
the final faction lineup or changes to simulation hull definitions.

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

Items 1, 2 and 4–10 have assumed answers in §1.2. Still open:

3. **The majors** — 4–5 of them: names, one real ideological difference each, tech-tree shape,
   signature tech, default loadouts.
11. **The endgame thresholds** (§2.3) — what they are, and what the old capital does in act 3.
12. **Tech tree** — size, and how research is produced (a building slot, per §1.1).

---

## 11. Change Log

- Initial document created from the opening brainstorm: vision, four pillars, two-resolution
  architecture, map, damage model and hardpoints, armies, setting, tech stack, open questions.
- Expanded §8 into a record of the engine-free technical decisions (C#, NeoVeldrid).
- **2026-09-29 — Fresh start.** The previous implementation, plans and design docs were
  retired; this file is the only design source carried forward. §8 rewritten for Unity 6 +
  URP (engine-free stack dropped, lock-in principle restated as "the rules don't live in the
  engine"), added the visual bar (§8.9) and build order (§9).
- **2026-09-29 — First campaign.** Recorded the rules the mini campaign runs on (§4.1).
- **2026-09-29 — Campaign depth.** Four powers, fog of war, armies and landings, lane
  interception, diplomacy and the council (§4.1).
- **2026-09-29 — Scope.** Added §1.1: a long campaign (~2 weeks of play), strategy as the
  activity and combat as the reward, 60–120 systems, 4–5 playable majors and 8–12 minors,
  a three-act arc (rupture → blocs → total war), reclamation instead of colonisation, system
  specialisation, a full hardpoint designer. Superseded "combat is the star" (§1) and "small
  node count" (§4).
- **2026-09-29 — Scale and rhythm.** Governors with stats that auto-build a system for a cut
  of its output; a sitting gives a couple of battles and 3–4 systems; only battles and council
  votes stop the clock; landings are automatic and armies can garrison planets (§1.1).
- **2026-09-29 — Assumed defaults.** Filled in the remaining open questions as §1.2 (map ~100,
  lanes, governor stats and pool, admirals, one sim with several setups, hardpoint mounts,
  minors, time scale, council every 60 days, two resolutions confirmed).
- **2026-09-30 — Long campaign built.** Recorded its rules as §4.2: generated galaxy, system
  economy and buildings, governors, reclamation and courting, admirals, the designer, acts and
  the throne, upkeep, saves. Governor focus "research" became "economy" until tech exists.
- **2026-09-30 — Research and weapon families.** Tech tree (29 technologies, six branches),
  laboratories, research governors, AI doctrines and redesigns, research-improved hulls, debt.
  Twelve weapon families with their own turret, tracer and flash; 20 guns in all (§4.2).
