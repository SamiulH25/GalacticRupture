# Galactic Rupture — Design Ideas

The single design document for the game. Everything else in the repo is implementation.
Updated as we go.

**Status key:** `LOCKED` decided · `PROPOSED` suggested, not yet confirmed · `OPEN` needs discussion

---

## 1. The Vision

`LOCKED` 2026-11-04 — **Galactic Rupture is a mercenary fleet game set inside a living galactic
war.** The player runs a **free company**: a contract fleet of named, scarred ships, hired by
the successor powers that are fighting over the dead empire. The powers run the war; the player
takes jobs, picks sides, keeps the salvage and builds a name. Contracts tip the fronts. By the
last act the company is strong enough to choose its own ending. Full design in §1.4.

**Why the change.** By November 2026 the game was heading toward an Empire at War-style
galactic conquest game with 4x depth: you are a power and paint the map. The user liked that
path but did not want it as the end goal. Three routes were proposed (an exodus fleet, a free
company, an admiralty) and the free company was chosen. The admiralty survives as one of its
endings (§1.4).

**What carries over.** Everything built stays. The tactical RTS, independent turrets, persistent
damage, the designer and the salvage fleet (§1.3) become the player's tools. The generated
galaxy, system economy, governors, diplomacy, council, tech, ground war and campaign AI (§4.2)
become the **living war** the company works in, run by the AI powers. The player gets the
power-level systems back only in the *found a state* ending.

**Superseded by this change** (older text stays below for the record, read through §1.4):
- "4x depth in the overworld, with EAW's galactic conquest constraints" (below). The player
  is not a power and does not paint the map.
- §1.1 "All majors are playable". All majors are AI. The player is the company.
- §1.1 "Four strategy layers" and "Session rhythm … 3–4 systems gained". The company has its
  own layers and rhythm (§1.4).
- §1.1 "What stops the clock: … council votes". Only council votes that concern the company
  stop it (§1.4).
- §1.1 economy, governors and landings describe the AI powers. They are not player
  activities, except after founding a state.

---

*The original vision (2026-09-29), kept for the record:*

A 4x space opera in the lineage of *Star Wars: Empire at War*, with the land combat
removed and the naval combat elevated to be the centrepiece.

The appeal of EAW was never really its land battles. It was:
- A **galaxy map where a single wrong decision feels catastrophic** (the supply line)
- **Real-time fleet combat** that was fast, decisive and cinematic
- An **artificial clock** that made a 20-minute campaign tense

We are keeping the galaxy map's tension and the combat's feel, and building 4x depth
around them.

~~`LOCKED` — 4x depth in the overworld, with EAW's galactic conquest constraints.~~ Superseded
2026-11-04 by the free company (above).
~~`LOCKED` — Combat is the star. Everything else serves it.~~ Superseded 2026-09-29 by §1.1:
strategy is where the time goes; combat is the reward.
`LOCKED` — Operatic space opera, original IP.
`LOCKED` — Full 3D.
`LOCKED` — Ground combat is **simulated**. Armies are transported through space for
assaults, in the manner of Stellaris.
`LOCKED` — Major and minor powers (2–3 majors as mirrors, 2–3 minors as pawns).

### 1.1 Scope and Game Shape

Decided 2026-09-29. Where this conflicts with older text below, this wins. Since 2026-11-04,
§1 and §1.4 win over this section. The long campaign, per-engagement command, galaxy size,
three-act arc and reclamation still hold, but as the war the company lives in.

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

### 1.3 The Signature: the Salvage Fleet

Proposed 2026-10-03. `PROPOSED` — everything in this section awaits sign-off. If confirmed, it
becomes the game's identity and the lens for every later decision.

Since 2026-11-04 this is the **core loop of the free company** (§1.4). Where the text says "a
power" or "the player's power", read "the company" for the player. AI powers keep their own
stock and refit as written.

**The problem it answers.** By 2026-10-03 the game had every Empire at War and Stellaris
feature (galaxy map, economy, council, tactical RTS) and no signature. Nothing on screen could
not have been in either of those games. In the tactical view ships were specks in a void, so
the §2.1 "swarm of barrels" never showed; the map was territory blobs. The three ideas this
document holds that nobody else has (independent turrets, persistent damage, reclaiming a dead
empire) were all in the design and none was visible.

**The hook, in one sentence.**

> You rebuild a dead empire's war machine from its wreckage, and every ship you fly is a named,
> scarred survivor you can't bear to lose.

**The test.** After a few sittings a player can describe a ship in their fleet from memory: its
name, the battle that cost it the dorsal battery, the enemy railgun bolted on in its place.
If they can't, the signature has failed, whatever else works.

**Why it fits this game and not another.** The campaign is long (§1.1): 15–30 hours. A long
campaign needs something the player is afraid to lose and glad to recover. XCOM, FTL and
Highfleet get that from persistent, scarred units. A 4x with it is rare, and ours has the
setting for it: the old empire's fleets died in the Rupture and their hulks are everywhere.

#### The loop, as a player sees it

1. A fleet fights over a system and wins, but the flagship loses its dorsal battery.
2. The wrecks of the enemy line drift in the field. The fleet holds the field and claims them:
   two railguns, a hull section and a damaged reactor core come back as salvage.
3. At a yard, the lost battery is not free any more (§4.2 today replaces it at no cost). The
   player refits the stump with a salvaged railgun instead of buying a new mass driver.
4. The next battle, the flagship is visibly different: a scorched stump where the old gunhouse
   was, a railgun in the enemy's colours, patched plating. The enemy sees its own gun on your hull.
5. The flagship earns a name in the ledger. Eventually it dies, the ledger records it, and the
   next hull to carry the name inherits a weight the player feels.

That loop gives the game a reason to fight (parts and glory, not just tiles), a reason not to
(scars are permanent), and a reason to care about the ships (history).

#### Part A: Ships are characters

- **Identity.** Every hull has a permanent id, a name, a build date and a service record:
  battles fought, hulls killed, damage taken, systems fought over, the scars it carries. The sim
  already persists `ShipRecord` (name, hull, which mounts live); this extends it.
- **Scars are permanent.** A destroyed mount leaves a **stump**: that hardpoint is dead until
  refitted with a part from stock. Hull damage is repaired at yards as today, but hull plating
  is a salvage and industry cost (see Part B), not free time. Three authored damage states stay
  (§2.2) and stumps and patches are drawn on top of them.
- **Function, not availability** (§2.2) stays locked. A scarred ship fights worse and still
  fights. The scars have to hurt through lost arcs, not by sidelining the ship.
- **Crew experience.** Four tiers (green, seasoned, veteran, hardened) from battles survived.
  Effects stay small (reload, traverse rate, nerve) and cap at a few percent so veterans never
  decide a battle alone. A destroyed ship takes its crew with it, which is the cost.
- **Legacy.** Names retire on loss and can be reused by a later hull, so the roster has
  lineages ("the Third *Vigil*"). The after-action ledger (built) lists the dead and the
  saved. No death spiral: a loss never makes the next fight unwinnable by rule.
- **The roster.** A fleet screen lists every hull as a card with its record, scars and
  refit state. This is the game's emotional centre and gets real design effort, not a table.

#### Part B: Salvage as the economy of war

- **Sources.**
  - *Wreck fields.* Ships destroyed in battle leave wrecks in the system's field (and in a lane)
    for a number of days. Whoever holds the field after the fight, with a fleet on station and
    no hostile warships, claims them over time. The loser can scuttle to deny them.
  - *Ruins.* Reclaiming a ruin (§4.2) can yield old-empire parts and, rarely, a **relic**.
  - *Derelict hulls.* Some old-empire hulls can be restored to service at a yard for a fraction
    of a new hull's cost, mismatched and strange.
- **What comes back.** Mounts (any family, from any faction), hull plating (repair currency),
  reactor cores (rare; restore a derelict or refit a capital), and **relics**: unique mounts
  with a name and a story, one of each per campaign, with a distinct look and a distinct trade-off.
- **Stock and refit.** Each power keeps a salvage stock. The designer's hardpoint choice shows
  *stock* guns (bought, new) and *salvaged* ones (free, finite, any family). Refitting a stump
  takes yard days and consumes the part.
- **Salvage is a supplement, not a replacement.** Yards have a refit throughput, parts
  degrade (salvaged mounts start at 60–90% condition), and plating shortfalls cap how fast a
  fleet heals. The economy still needs credits and industry; salvage bends it.
- **The mismatch is the point.** The twelve weapon families already have distinct silhouettes
  and rounds (§4.2). A fleet carrying captured guns reads at a glance, and a fleet that has
  been looted by an enemy sees its own guns come back at it.
- **Doctrine and politics.** The salvage doctrine technology becomes a branch (recovery speed,
  scuttle charges, refit throughput). The Oriel Salvage Charter and Hadran Yard Cooperative
  (§7.1) gain real leverage over wreck law and refit contracts. The council can rule on salvage
  rights over contested fields.

#### Part C: Combat is a duel of arcs, seen up close

- **Fewer, bigger ships on screen.** The signature image (§2.1) needs a camera close enough to
  see guns. A battle centres on 4–12 capitals with escorts, not a few hundred specks (§8.8's
  "few hundred ships" remains the technical ceiling, not the design target). The command cap
  (§5.5) is retuned so one battleship fills the frame.
- **Capital cam.** The default tactical camera sits low and close on the selected capital,
  with an establishing pass when a battle opens (§8.9). A zoomed-out tactical map stays for
  orders, but it is the secondary view.
- **Dead arcs.** Every ship has a coverage map built from its live mounts' arcs and traverse
  limits, always one key away and drawn on the hull in the damage schematic (which exists).
  A stump removes arc coverage permanently; fighting is about getting to the side that a
  previous fight left bare.
- **Subsystem orders.** A new order, *target the battery*, directs the fleet to break a named
  group of mounts (dorsal, port, forward) rather than hull. Guns still pick targets on their
  own (§2.1), but the player decides what the fleet is trying to break. Disruptive weapons
  (§5.1) become the dedicated answer.
- **Telegraphed commitment** (§2.1). Converging a battery narrows your own coverage and shows
  as a visible swing; the target can break it by turning. Shells have travel time.
- **Tactical AI exploits arcs.** `FleetAi` already stages assaults and defends stations. It gains
  arc awareness: protect its own dead arcs by turning, seek yours, focus a battery.
- **Pace.** Stays slow and deliberate with speed buttons (as built, `Pace.cs`), fleets
  moving as a unit, ships level. This part makes slowness an asset: there is time to read arcs.

#### Part D: Making it visible

- **Ship models.** A hull's authored mesh gets stump variants per hardpoint and a patched-plate
  material layer. Mount models already swap by mount id (`WeaponModels`), so a salvaged mount
  needs no new art, only a faction-paint rule so it keeps its original colours.
- **Scar rendering.** Scorch, vented hull, fires, listing (§8.9), carried between battles and
  accumulating with the ship's record, not reset.
- **Ship record card.** Name, lineage, crew tier, battles, kills, scar diagram, refit parts
  list, the one-line story the sim writes ("lost her dorsal battery at Alois, refit with a
  Remnant railgun").
- **After-action ledger** (built) extended: wrecks claimed, parts recovered, names lost.
- **Audio.** A short, sober beat when a named ship dies; the roster screen has its own ambience.

#### What it entails for every layer of the game

| Layer | What changes |
|---|---|
| **Sim: defs** | `ShipRecord` gains id, history, per-hardpoint mounted gun and condition, stump state, crew tier. Designs stay templates; a built ship drifts from its design as it is refit. |
| **Sim: campaign** | A salvage stock per power; wrecks and claims in systems and lanes; yard refit queue and throughput; `Repair()` stops replacing mounts free and instead consumes stock; derelict hull restoration; relic table seeded at galaxy generation. |
| **Sim: tactical** | Wrecks persist as field objects after a battle; scuttle action; subsystem-group targeting; arc coverage query; `FleetAi` arc awareness; battle size retune. |
| **Designer** | Stock vs salvaged mounts per hardpoint; refit of a built ship vs design of a new one; relics shown with their stories. |
| **Economy** | Hull plating and reactor cores as a new, small salvage stock; upkeep and costs retuned so salvage matters without replacing credits and industry. |
| **Campaign AI** | Powers claim wrecks, scuttle when losing a field, refit with what they hold, and name ships. The AI redesign pass (§4.2) becomes a refit pass. |
| **Diplomacy/council** | Salvage rights and wreck law as council items; Oriel and Hadran as leverage minors; an embargo can cut a power's refit parts. |
| **Setting** | The Rupture left graveyards: every system holds the old empire's dead. Act 1 is a scavenger's era (free wrecks), act 2 contests fields, act 3 pays out the old capital's relic fleet. |
| **Presentation** | Capital cam, scar variants, stump and patch rendering, ship record card, roster screen, extended ledger. |
| **Models** | Stump variants and patch layers for 4 hull classes × factions, built in Blender to the existing quality bar (no ventral guns, short chunky barrels). |
| **Saves** | Save version 4: ship ids and histories, salvage stock, wrecks, relics. Reads version 3 by giving existing ships fresh records. |
| **Tests** | Determinism (§8.7): salvage rolls go through the seeded RNG. Sim tests for refit consumption, stump persistence across a battle and a save round-trip, and a 900-day AI-vs-AI run to check that salvage does not snowball. |

#### Risks and guards

- **Snowball.** Veterans and relics stacking would make a winner unbeatable. Guards: crew
  bonuses cap small, relics are one each and tradeable only by capture, salvaged mounts start
  worn, refit throughput is limited.
- **Bookkeeping over thirty hours.** Refitting fifty ships by hand is chores. Guards: a yard
  refit policy ("fill stumps with best stock") and governors with a military focus doing it
  for the player, as they auto-build today.
- **Locks it must respect.** No faction rock-paper-scissors (§5.2): salvage gives no exclusive
  weapon. Function over availability (§2.2). Damage persists. The RTS is the player's (§1.1).
- **Pace of the campaign.** Salvage must not add a second clock-stopper. Claims and refits
  run in the strategic clock and notify (§1.1).
- **Art load.** Stump and patch variants multiply model work. Start with the battleship and
  destroyer and prove the look before extending.

#### Build order

1. **The duel.** One battle on capital cam: two battleships, visible arcs, turrets dying,
   dead arcs exploited, subsystem order. The "whoa" test for the combat.
2. **Scars that last.** Per-hardpoint stumps persisted by `ShipRecord`, rendered; ship record
   card; `Repair()` no longer refills turrets free.
3. **Salvage and refit.** Wrecks persist and are claimed; stock; refit in the designer and yards;
   mismatched mounts drawn in original colours.
4. **Relics and derelict hulls.** Reclamation yields them; restoration at yards.
5. **Campaign hooks.** AI salvage and refit; council wreck law; minors' leverage; governors'
   refit policy; roster screen; ledger extension; save version 4.
6. **Legacy and polish.** Lineages, memorial in the ledger, audio, establishing shots.

Steps 1 and 2 are the vertical slice: if a battle on capital cam does not feel strong, and a
scar carried into the next fight does not feel earned, stop and rework before building 3–6.

#### Existing text this changes

- §2.2 "Repair at friendly stations": hull repair stays; lost turrets now cost stock and yard time.
- §4.2 "Repair at friendly yards… lost turrets replaced one at a time" and `Repair()`
  in `Campaign.cs` replace free turret regrowth.
- §5.5 command cap and §8.8 "working size: a few hundred ships": the design target moves to a
  handful of capitals; the technical ceiling stays.
- §4.2 designer: "design cost is hull plus mounts" gains salvaged and relic mounts at no cost.

#### Open questions

- How permanent is permanent? Can a stump ever be healed without a part (an old-empire
  restoration facility, a rare tech), or is a missing gun always a gun you must find?
- Wreck claims: automatic by holding the field, or an order (a salvage posture) that exposes
  the fleet to counterattack? Leaning toward an order, since that is a decision.
- Does the roster have a cap, so a power cannot hoard hundreds of named ships?
- How many relics, and do they appear on the map before they are found (rumour) to give act 1 a
  treasure hunt?

### 1.4 The Free Company

Chosen 2026-11-04. The direction is `LOCKED`, as are three answers given that day: **branching
endings**, **a company that grows into detachments**, and **a mobile tender plus leased yards**.
Everything else here is `PROPOSED`.

**The hook, in one sentence.**

> The successor powers have more fronts than fleets, so they hire yours. Every job pays in
> credits, salvage and enemies, and every ship you lose was one you named.

**The test.** A player can explain why they took the last contract, what it cost, and how it
changed the war. "The Hierarchate paid double, but taking it meant the Free Yards closed their
docks to us, and the *Vigil* needed a refit."

**Setting.** After the Rupture the old navy's crews and hulks are everywhere and no power has
enough of either. The council, which controls almost nothing (§7), still issues **letters of
service** that make a free company legal. That is part of its theatre: a power can pay for a
war without declaring one. Companies are licensed, hired, used and disowned.

#### The loop, as a player sees it

1. **The board.** Contracts arrive from the powers and minors, each with terms and a deadline.
2. **The choice.** Taking one helps its employer and angers its target. Pay, salvage rights
   and risk are weighed against the state of the fleet.
3. **The job.** The fleet travels the lanes. The battle is fought in the RTS or left to the
   detachment's captain, through the same sim (§3).
4. **The aftermath.** You get paid (or not), claim wrecks (§1.3), and the after-action ledger
   records the dead.
5. **The refit.** Field repairs at the tender, or a big refit at a leased yard whose price
   depends on your standing with its owner.
6. **The war moves.** A system you held stays held, a power you weakened loses a front, and the
   next board reflects it.

#### Part A: Contracts

- **Generated from the war's real needs.** The campaign AI posts a contract when it has a gap:
  a front without enough fleet, a convoy that needs escort, a station under siege, a rival's
  yard it wants raided. Contracts are never random flavour. Finishing one changes the state
  that created it.
- **Kinds** (first set): *hold* a system or station for N days · *escort* a convoy or landing
  · *raid* a lane or a yard · *break* a blockade · *siege support* for a power's assault ·
  *expedition* to an old-empire ruin (salvage and relics) · *bounty* on a named enemy ship.
- **Terms.** Pay up front and on completion, a **salvage clause** (you keep the wrecks, they
  do, or a split), a loss indemnity (the employer pays part of the cost of destroyed hulls),
  exclusivity (no work for its enemies for N days), and the deadline. Better standing gets
  better terms, and terms can be haggled once.
- **Failure.** Failing a contract costs pay and standing. Abandoning one costs more. Turning
  on an employer mid-contract is possible, and every power hears about it.

#### Part B: Standing and word

- **Standing** with each power and minor, from hostile to trusted. It sets which contracts
  you are offered, their terms, yard prices and access, and whether their fleets treat you as
  a target.
- **Word**, a single reputation for reliability. Every power sees it. Kept contracts raise
  it, and betrayals and abandoned jobs cut it hard. High word gets you the best contracts. Low
  word gets you only desperate employers.
- **The council** can renew or revoke your letters of service, rule on contested salvage and
  declare a company outlaw. A council vote about the company stops the clock. Other votes do
  not.

#### Part C: The company

- **Finances.** Credits in, payroll and upkeep out. Each hull has a crew wage, and veteran
  crews (§1.3) cost more. Fuel and supplies are spent per jump. Yard fees and repairs are paid
  to the yards. Debt is allowed up to a limit. Past it, crews desert and creditors seize hulls.
  Bankruptcy or the loss of every warship ends the campaign.
- **Ships.** New hulls are bought from a power's or minor's yard at its price and standing.
  Derelicts are restored (§1.3). Enemy hulls are captured when a contract's salvage clause
  allows it. The designer (§4.2) becomes the **refit designer** for the company's ships, and
  stock guns are bought from whoever will sell them.
- **The roster** (§1.3 Part A) is the company's heart: named ships, crews, scars and lineages.
- **No research.** The company does not run labs. Its technology is whatever it can buy,
  salvage or capture. The powers' tech race (§4.2) is visible in what they field and sell.

#### Part D: Detachments and captains

- The company starts as **one fleet**. As it grows it hires **captains** (the existing
  admirals: stats plus a trait) to lead **detachments** on separate contracts.
- The player commands any battle they choose. The rest are fought by their captain through
  the same sim.
- Captains take a share of their detachment's pay and have **loyalty**. A neglected or
  underpaid captain can be poached by a power, or walk off with the detachment.
- Detachments are how the company works more than one contract at once. They are not a way
  to paint fronts.

#### Part E: The tender and leased yards

- **The tender** is a slow, lightly armed yard ship that travels with a fleet. It repairs
  hull plating and refits stumps from the salvage stock at limited throughput, and stores the
  salvage. It cannot build hulls. It must be guarded: a raid on the tender is a classic
  contract against you, and losing it is a crisis, not the end.
- **Leased yards** belong to powers and minors (the Hadran Yard Cooperative and Free Yards
  above all). Big refits, new hulls and derelict restoration happen there. Price and access
  follow standing, and an embargo or a hostile power can shut a yard to you mid-war. This is
  how politics reaches the fleet.

#### Part F: The living war

- The five majors and the minors run the war themselves on the existing campaign sim and AI
  (§4.2), through the same three acts (§1.1): Rupture, Blocs, Total war.
- The player sees the war as a **war feed** (battles, systems changing hands, council rulings,
  powers in trouble) and a map showing fronts and standing. Intel is fogged (§4.1), and
  accurate intel can itself be bought or be part of a contract.
- **Guard: the war must be legible.** If a player can't see what their last contract
  changed, the game fails its test. The feed and the after-action ledger call out the
  company's effect directly ("Hierarchate assault on Alois failed; the Ilium line held, with
  your detachment").
- **Guard: the war must not finish itself.** The 900-day AI-vs-AI diagnostic already exists.
  It has to show a war that stays contested into act 3 without the player.

#### Part G: Endings

In act 3, once the company's strength, standing and word pass thresholds, endings open up.
Which ones open depends on how the company was played. Choosing one starts the final stretch.

- **The commission (the admiralty).** A trusted power offers to make the company its navy
  and the player its admiral. You get fronts, a budget and political masters who can overrule
  you. You win when that power takes the old capital or the throne (§4.2).
- **Found a state.** Take and hold systems, through contracts paid in territory, derelicts
  reclaimed or outright seizure, then declare. The power-level systems open to the player:
  economy, governors, a council seat and the ground war. You win by surviving to recognition
  while the others try to strangle the new state.
- **The legend.** Stay free. You win by being the company still standing when the war ends,
  with renown past a threshold and, optionally, the old capital's relic fleet destroyed or
  taken.
- **Losses:** bankruptcy, every warship lost, or outlawed with no employer left.

#### Rhythm and scope

- A campaign stays long, 15–30 hours (§1.1).
- A 1–2 hour sitting gives 2–4 contracts, a couple of battles, a refit decision and one
  political choice that costs something. This replaces "3–4 systems gained".
- Battles and council votes about the company stop the clock. Contract offers and their
  deadlines notify.

#### What happens to what is built

| System | Fate |
|---|---|
| Tactical sim, terrain, stations, `FleetAi`, abilities, pace | **Stays** as is. Every contract is a battle setup (§1.2 "one sim, several setups"). |
| Galaxy gen, economy, buildings, governors, reclamation, diplomacy, council, tech, ground, `CampaignAi` | **Stays** as the AI's world. The player sees them through the war feed. They open to the player in the *found a state* ending. |
| Campaign HUD panels (strategy, research, council, deploy) | **Retired from the default view.** Replaced by company screens (contract board, roster, finances and ledger, war feed, standing). Kept for the state ending. |
| Designer | **Becomes the refit designer** for company ships (stock vs salvaged, §1.3). |
| Admirals | **Become captains** with loyalty and a pay share. |
| Ground war (`Campaign.Ground`) | **Stays** for the powers. Company marines use it for station seizure and landings. Boarding is new tactical work. |
| Player faction | The campaign currently assumes the player is a power with systems. The company needs its own actor: fleets and finances, no systems. Gotcha: much code reads "player faction index". |
| Saves | New version: company, contracts, standing, word, captains. |

#### Build order

1. **The war runs without you.** Run the 900-day diagnostic and fix stalls and runaway
   winners. Add a first war feed.
2. **One contract end to end.** Company creation (origin, name, heraldry), the company actor,
   a contract board, a single *hold* contract generated from an AI need, travel, battle, pay,
   wreck claim, ledger.
3. **The company's books.** Payroll, debt, buying hulls at a yard, the roster screen. Merges
   with §1.3 steps 2–3 (scars, salvage and refit).
4. **Standing, word and the tender.** Per-power standing, yard access and prices, the tender's
   field refits.
5. **The full board.** All contract kinds, terms and haggling, betrayal, council letters of
   service.
6. **Marines.** Hiring, boarding in the tactical sim, station seizure and landings.
7. **Rival companies.** The company AI, competition for the board, hire-against-you, poaching
   and buyouts.
8. **Detachments.** Captains, loyalty, parallel contracts.
9. **Endings.** Commission, founding a state, the legend.

The vertical slice is §1.3 steps 1–2 (the duel and scars that last) plus steps 1–2 here. Take
a contract, fight it on capital cam with named ships, come home scarred and paid, and see the
war move. If that doesn't land, stop and rework before building the rest.

#### Part H: Origins

`LOCKED` 2026-11-04 — **The player chooses the starting company.** `PROPOSED` — three origins.
Each sets the starting fleet, credits and debt, standing, one captain and one origin trait:

- **Old navy deserters.** One worn old-empire capital, scarred and short of parts, with a few
  escorts. High word and good crews. Every successor that claims the old navy's legacy sees
  them as deserters. *Trait:* old-navy hulls restore cheaper.
- **Salvage guild.** A strong tender, a large salvage stock and light warships. Good standing
  with the Oriel Salvage Charter. *Trait:* faster wreck claims and better salvaged-part
  condition.
- **Disbanded minor navy.** A balanced squadron of matched ships from a minor that was
  absorbed. Trusted by that minor's people and hostile to the power that absorbed it.
  *Trait:* cheaper marines recruited from its old garrisons.

#### Part I: Marines

`LOCKED` 2026-11-04 — **The company hires marines.** `PROPOSED`:

- **Hiring.** Marine companies are recruited at systems: ex-garrisons, minors and old-navy
  holdouts. They have wages, experience and casualties, like crews. They ride in transports or
  troop-capable hulls, which become part of fleet design.
- **Boarding** (tactical). A disabled enemy ship (engines out or hull below a threshold) can
  be boarded by an adjacent ship carrying marines. Boarding takes time, is contested by the
  defending crew and can be broken by killing the boarding ship. A successful boarding takes
  the **whole hull**, scars included. It is the best salvage there is, and the hardest.
- **Station seizure and landings** (strategic). Marines take stations and join or escort a
  power's landings under contract. Both resolve through the existing simulated ground war
  (§6, `Campaign.Ground`).
- **The state ending.** Marines are how a company holds systems before it declares (Part G).

#### Part J: Rival companies

`LOCKED` 2026-11-04 — **There are rival AI free companies.** `PROPOSED`:

- 3–5 at the start, each with a name, heraldry, an origin and a temperament: *reliable*,
  *cutthroat* or *glory-hunting*. They run on the same company actor as the player, driven
  by a company AI, and they have standing and word too.
- They compete for the board: a contract can be taken before you accept it. Powers hire them
  against you, so a raid on your tender may be another company's job.
- They can be fought, out-bid, allied with on joint contracts, poached (their captains and
  marines), or bought out and absorbed when they are broken. Companies can die, and new
  ones can form in later acts.
- **Guard:** rivals must not empty the board. Contract supply grows with the war's needs, and
  rivals take a share, not everything.

#### Part K: Heraldry

`LOCKED` 2026-11-04 — **The player picks the company's heraldry.** `PROPOSED`: a name, two
colours and an emblem from a set (with simple charges and divisions), chosen at company
creation. It is painted on every company hull. Captured and bought hulls are repainted at a
yard, but salvaged mounts keep their original colours (§1.3 Part D), so the mismatch still
shows. Rival companies get generated heraldry.

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
  Remnant, Hierarchate, Free Yards, Lantern Synod — placeholder names; the settled powers and
  their slot mapping are in §7.2),
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
  - *Energy:* heavy plasma cannon → pulse laser → beam lance → shield burner.
  - *Disruptive:* arc cannon → light arc cannon → heavy arc cannon → arc doctrine (reload −10%).
  - *Defence:* flak → heavy flak; shield lattice (shields +20%), layered armour (hull +15%) →
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
| Plasma | light plasma cannon, plasma cannon, heavy plasma cannon | dome with finned emitters and hot tips; bolts in the side's colour |
| Pulse | pulse laser | small dome, 2×2 emitters; short rapid bolts |
| Lance | beam lance | long house, one square emitter with a glowing slit; a long heavy bolt |
| Shield burner | shield burner | dome, emitters ringed with glowing coils; magenta bolts |
| Arc | light arc, arc cannon, heavy arc | drum house, pronged fork with a coil; fat soft blue bolts |
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
- **Disruptive** — EMP, arc, neural. **No hull damage.** Kills subsystems. The only
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

### 5.5 The Tactical Layer (as built)

`PROPOSED` — the Empire at War verbs, as of 2026-09-30. Numbers are first tuning from headless
runs, not from play.

- **Terrain.** Every system has a fixed battlefield, derived from its name: open space (35%),
  scattered rock, an asteroid field, or a belt across the field with two or three gaps (12%);
  28% also hold a nebula to one flank. Ruins lie in their own wreckage (more rock). Lanes are
  open space, sometimes with rock or a nebula. Rock keeps to the middle and the flanks, never
  the deployment lines.
  - *Asteroids* are walls and cover: ships steer round them, rounds stop on them, and a ship
    behind one cannot be targeted. Ships with an attack order move on until the line is clear.
  - *Nebulae* strip shields (35% of capacity a second, no regeneration) and hide ships: a ship
    inside is seen only within 1.2 km.
- **Stations.** Levels 1–3 (350 / 600 / 950 cr, 8 / 12 / 16 days, 1% of their value a day in
  upkeep), built from the system panel alongside the slot buildings. 10 / 16 / 24 guns, all on
  the deck or rim. Capitals start with one (majors level 2, minors level 1). A station fights
  in every battle over its system, stops any hostile fleet there (passing through included),
  keeps its damage between battles (repaired 5% a day), and never withdraws. Destroyed, it is
  gone. The AI fortifies its capital first, then developed worlds on a hostile border, out of
  savings; it adds station strength when judging an invasion.
- **Command cap.** 30 points a side on the field: corvette 1, frigate 2, destroyer 3,
  battleship 6, transport 1, station 0. The heaviest ships deploy first; the rest wait in
  reserve and arrive at the side's edge of the field (or under its station) when called in
  from the reinforcement panel (R). An AI side brings in the heaviest ship that fits. A side
  with no warships left on the field brings one in automatically.
- **Abilities.** Boost engines (×1.7 speed, 10 s, 35 s cooldown), power to shields (×3 regen
  and no delay, 10 s, 40 s), power to weapons (reload ×0.6 and no shield regen, 10 s, 45 s);
  the first two slow the guns (reload ×1.4). One at a time. Z / X / V or the buttons.
- **Tactical AI.** Defending a station, the fleet holds under its guns unless it outguns the
  attacker 1.6:1. Assaulting one, it gathers out of range and goes in together (when 75% are
  in place, when the defenders come out, or after 45 s); escorts hunt escorts, the line goes
  for the station once two or fewer defenders cover it. Badly hurt ships (<22% hull) fall back
  to a fixed point at their side's edge while healthy ships hold the line. Shield-heavy fleets
  stay out of nebulae; a fleet outgunned 0.6:1 with nothing to take hides in one for up to a
  minute. Transports wait at the back and run when the escort is gone; unescorted transports
  are fair game until they get clear.

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

`LOCKED` 2026-11-04 — **The world, the five powers and the ship design doctrine in §7.1–§7.5**
were proposed, revised once at the user's request (ships based on real wartime navies, and a
full case for lane ratings and keel fabs) and accepted. They are the setting. Future work
builds on them and does not reinvent them. Change them only when the user asks. The
illustrated proposal, with plan-view sketches of every faction's ship, is at
https://claude.ai/artifact/L7Lw6FrAUAhHv3wReU5gqx. This section is the authoritative text.

### 7.1 The World: the Broken Meridian

The old empire was **the Meridian**. It held together through one network, the lanes.

1. **Lanes need keystones.** Stars are joined by lanes. A ship can cross one only while a
   **keystone**, an old-empire anchor station, holds it open. The Meridian ran every keystone
   from the **master keystone** at its capital. Eleven years before the campaign the master
   keystone cracked. **That is the Rupture.** Half the lanes collapsed within a week, fleets in
   transit never came out, and the empire broke apart into whoever held what. The endgame
   object is the master keystone at Meridian. Whoever repairs and holds it controls travel,
   and that is what "the throne" (§4.2) means.
2. **Lane ratings set ship sizes.** A lane passes only so much mass. Each lane has a rating
   (I, II or III), and every hull is built to one. Rating III trunk lanes pass battleships,
   rating II lanes pass cruisers and smaller, and rating I lanes pass only frigates and
   corvettes. (Details and the case for it: §7.5.)
3. **Keels are scarce.** A rating III hull needs a **keel fab** to build. Three survive: two
   Foundry Combine, one damaged and Sovereign. Escorts can be built at any yard. (§7.5.)
4. **Heat is the limit.** Drives and energy weapons make heat that only radiators shed.
   Radiators are big and fragile, fold in for combat and can be shot off. A ship that loses
   radiators overheats and fires slower. Radiators are a subsystem target.
5. **Battles happen on a plane.** Lanes deliver ships onto a system's ecliptic, and fleets
   fight across it. Guns go on the dorsal deck and the beam. Armour goes on the bow and the
   belt. The belly carries boats and magazine access, **never guns**. This is the in-world
   reason for the no-ventral-guns rule and for ships staying level.
6. **Crews are scarcer than hulls.** The Rupture killed most of the trained navy in transit.
   Veteran crews and marines are hired, poached and fought over, which is why free companies
   (§1.4) exist.

The premise from the title still holds: **the galaxy broke, and the institutions of the old
order still pretend to function.**

- **Major powers** — successor states, whoever seized the big pieces of the old empire's
  infrastructure
- **Minor powers** — what's left over, still flying colours that don't mean anything
- **The council** — exists, has seats, passes resolutions, controls almost nothing. The
  pre-rupture order performing legitimacy while actual power sits with whoever took the
  fabs

This makes galactic diplomacy *about a lie*, which is far more interesting than peace
treaties — every council session is theatre, and the interesting play is deciding when to
stop pretending.

### 7.2 The Five Powers

Each power claims the right to rule on a different basis, so every council session is an
argument about what legitimacy is. Each holds something another needs, which is the war, and
it is why the contract board (§1.4) never runs dry. Each is modelled on how a real navy behaved
under the same pressure. The precedent is the design brief.

| Power | Claims rule by | Holds | Lacks | As an employer |
|---|---|---|---|---|
| **Sovereign Fleet** | Inheritance: "we are the Meridian's navy, so we are the Meridian" | Most surviving capitals, the old fleet base, one damaged keel fab | Industry, money, replacements | Proud, slow to pay; hires companies so its battleships stay out of danger |
| **Foundry Combine** | Production: "whoever builds the future owns it" | Two keel fabs, the foundry worlds, mass production | Lane access, old-empire tech, trained crews | Cash on time, strict contracts, a bonus per hull destroyed |
| **March Wardens** | Protection: "whoever keeps people alive has the right to govern them" | The frontier, its people, the evacuation networks | Money, and big ships (their lanes are rated I–II) | Poor; pays in salvage rights and land |
| **Keystone Synod** | Knowledge: "whoever keeps the lanes keeps the galaxy" | The keystone engineers, lane secrets, the best old-empire tech | A fleet of any size | Secretive; pays in lane access; prefers companies it can deny hiring |
| **Charter Houses** | Contract: "whoever pays, rules" | Trade, credit, freight, courier routes | Warships | The biggest employer of free companies; pays best, haggles hardest |

Who needs whom: the Sovereign Fleet wants the Combine's keel fabs. The Combine wants the
Synod's lanes and tech. The Synod fears a Sovereign restoration that would put the keystones
back under a throne. The Wardens need Charter money. The Charter Houses need open lanes and
hire everyone to keep them open.

**Sovereign Fleet** — a navy that outlived its state.
- *Precedent:* Royal Navy First World War battleships rebuilt between the wars and refitted
  with bulges and dozens of anti-aircraft guns; Tirpitz kept in Norway as a "fleet in being";
  Wrangel's White Russian fleet, which sailed on after its state was gone.
- *Ships:* builds nothing new above a cruiser and rebuilds old Meridian battleships. Superfiring
  triple turrets on the centreline, an armoured conning tower, refit bulges on the old belt,
  point defence crowded onto every free surface, patches cannibalised from other wrecks, small
  radiators (mass drivers run cool). The Meridian crest on the bow is the only ornament,
  because it is their claim to rule.
- *Fights:* as a battle line, bow on, heavy guns at long range. Capitals are committed only
  when the odds are certain. *Weakness:* every loss is permanent; slow; short of escorts.
- *Signature:* coordinated volley (§5.4 T3). *Palette:* fleet slate, brass, rust patches.

**Foundry Combine** — wartime mass production.
- *Precedent:* American Second World War production: Liberty ships welded from prefabricated
  sections, 175 Fletcher-class destroyers, escort carriers launched almost weekly, one 5-inch
  gun on almost everything. The Type XXI U-boat, built in eight sections at different yards,
  with the quality problems that brought.
- *Ships:* identical prefabricated hull blocks with the seams showing (a destroyer is 3 blocks,
  a cruiser 5, a battleship 8), flat plates and no curves, one standard twin turret on every
  class, missile cells in standard blocks, a flat slab bow plate. Primer paint, hazard
  markings, stencilled hull numbers and never names.
- *Fights:* missile salvos and volume of fire, numbers over quality, losses replaced in weeks.
  *Weakness:* thin armour, weld defects, green crews.
- *Signature:* block swap (a tender replaces a wrecked block in a day). *Palette:* primer
  grey, hazard ochre.

**March Wardens** — a small navy on a long frontier.
- *Precedent:* Type VII U-boat saddle fuel tanks; the raider Atlantis (more than 600 days at sea
  without a home port); Norwegian and Finnish torpedo boats in fjords and skerries; US warships
  alternating engine and boiler rooms so one hit can't stop them; the Dunkirk little ships and
  armed trawlers.
- *Ships:* compact, low hulls with saddle fuel tanks, twin separate drive pods, torpedo tubes,
  few turrets, a sensor mast. Frigates are converted ore haulers and tugs. A cruiser of about
  400 m is the biggest thing they fly, because their lanes pass nothing larger.
- *Fights:* ambush from nebulae and asteroid fields, a torpedo run, then gone. They evacuate a
  system rather than lose its people. (This absorbs the old Hearthward League idea.)
  *Weakness:* no battle line; can't hold against capitals in the open.
- *Signature:* dark running (hidden in terrain until they fire). *Palette:* olive drab, bone.

**Keystone Synod** — a military order holding the choke points.
- *Precedent:* the Knights Hospitaller, who held Malta and its sea lanes with a handful of
  galleys, funded by every power that needed the passage open; interwar Japan, limited by
  treaty to fewer ships, building each one better and keeping the Long Lance torpedo secret.
- *Ships:* few and superb: old Meridian hulls kept immaculate, huge radiator wings for energy
  lances, oversized lane coils, an arc emitter aft. Flagship type: the "lane cutter" cruiser,
  about 540 m.
- *Fights:* energy lances, and arc weapons that shut ships down. They choose where battles
  happen. *Weakness:* the radiator wings (shoot them and the guns choke); a tiny fleet.
- *Signature:* lane interdiction (pull a fleet out of a lane early). *Palette:* ceramic white,
  lane blue.

**Charter Houses** — a trading company that arms its freighters and hires the rest.
- *Precedent:* the East India companies, who armed their merchantmen and hired armies (at Pulo
  Aura in 1804 a convoy of Indiamen bluffed a French squadron into retreat); First World War
  Q-ships with hidden guns; armed merchant cruisers such as Jervis Bay; dazzle-painted convoys.
- *Ships:* armed merchants and convoy cruisers on cargo spines that still haul freight. Batteries
  are hidden behind cargo panels, with point defence everywhere, shield emitters to protect the
  cargo, dazzle paint, and each house's own livery.
- *Fights:* defensively: protect the convoy, outlast the raid, call in hired companies.
  *Weakness:* weak armour, few real warships, dependence on mercenaries.
- *Signature:* false colours (a convoy cruiser reads as a freighter on sensors until it fires).
  *Palette:* house teal and bone dazzle, lacquer red, gold.

**Free companies** (the player and rivals, §1.4) have no design language of their own. A
company fleet is a patchwork of bought, restored and captured hulls under one heraldry, with
salvaged guns in their old colours. *Precedent:* the condottieri, privateers under letters
of marque (the council's letters of service), and the Hessian regiments hired out by their
prince.

**Code mapping** (the renames are not yet made in code). The new powers take over the existing
faction slots: 0 Continuance → Sovereign Fleet, 1 Remnant → March Wardens, 2 Hierarchate →
Charter Houses, 3 Free Yards → Foundry Combine, 4 Lantern Synod → Keystone Synod. The AI
doctrines in §4.2 are retuned to match: Sovereign is ballistic and defence; Wardens logistics
and light ballistic/torpedo; Charter defence (point defence and shields) and economy; Combine
ballistic and missiles and industry; Synod energy and disruptive. The older roster proposal
(Serac and Ilium promoted to majors, plus a Hearthward League) is superseded. Serac and Ilium
stay minors in the test map.

### 7.3 Minor powers

These eight minors stay as proposed. Each holds a specific piece of leverage that makes its
council seat and its fate worth contesting. None needs a unique weapon or exclusive hull
class, and each flies converted hulls in its own colours.

| Minor power | What it holds | Pressure point |
|---|---|---|
| **Morrow Passage Authority** | Pilots, traffic records and service stations at the Morrow routes. | Can delay a fleet without defeating it; claims it is enforcing neutral passage rules. |
| **Thule Archive Enclave** | Pre-rupture registries, survey records and competing copies of imperial law. | Every major wants its records authenticated; the Enclave survives by releasing only enough truth to stay useful. |
| **Oriel Salvage Charter** | Wreck fields, salvage rights and the crews who can recover old-empire matériel. | Its claims keep wreckage from becoming free loot, but a blockade can starve its people of replacement parts. |
| **Hadran Yard Cooperative** | Skilled repair crews and a small, badly damaged orbital yard. | Can keep a battered fleet in service, but must choose whose contracts get priority. |
| **Silex Prospectors' Union** | Remote ore and isotope claims worked by independent extraction ships. | Can shut down exports to resist absorption; its scattered crews are hard to protect from raiders. |
| **Edda Habitat Trust** | Life-support expertise and several densely populated habitat stations. | It can keep a system habitable through a siege, but its residents fear becoming a strategic asset owned by a major. |
| **Vesper Courier Guild** (renamed from Vesper Courier Houses to avoid a clash with the Charter Houses) | Couriers and relay caches carrying messages along routes the old network no longer serves reliably. | They sell speed and privacy to every side, until a bloc demands exclusive service. |
| **Nadir Watch** | A chain of remote observatories and warning posts on an exposed frontier. | Its early warning buys neighbours time; no one agrees who should pay to keep its crews supplied. |

The minor powers should have their own reasons for bargaining, resisting absorption or
changing sides. Their value is political and strategic leverage, not a set of disposable
colour-coded planets. Their final system locations, names and council-seat rules remain
`OPEN` until the long-campaign map is designed.

### 7.4 Ship Design Doctrine

`LOCKED` 2026-11-04. Every ship must be explainable to a shipwright. These rules apply to every
new hull and model.

**Why the old models were rejected.** One hardpoint layout per class was shared by every
faction, so factions differed only in outline and paint. The hulls were flat boats with
nothing in the world explaining their shape, and no working parts (drive, radiators, armour,
magazines) were visible.

**Wartime design principles.** Each faction's ships follow from these, with real precedents
in §7.2:
- Build what your yards can make fast: simple hulls, flat plates, prefabricated sections.
- Standardise: one class in hundreds, one gun on every ship.
- Convert what you have: freighters, trawlers, half-built hulls.
- Protect what you can't replace: rebuild old capitals and keep them out of danger.
- Bolt on what the last fight taught you: refit point defence and bulges onto old hulls.
- Where you can't match numbers, make quality count: few ships, a secret weapon.
- The look is wartime: primer, camouflage, weld seams, stencils. The only decoration allowed
  is the kind that identifies a ship.

**Shared anatomy** (plan view, bow forward). Every faction builds around the same skeleton:
- *Drive section* aft (fusion torches and reaction mass, about a third of the length; hit it
  and the ship slows).
- *Radiators* (folded for combat, deployed for cruising; their size shows how much energy a
  ship burns).
- *Lane spine and coils*: old Meridian coils on every hull, because nobody can make new ones.
  This is the one visual element all factions share.
- *Citadel*: an armoured box for the reactor, magazines and CIC.
- *Turrets* on the dorsal deck and beam, superfiring on the centreline.
- *Armour* heaviest on the bow and on the belt over the citadel.
- *Belly*: boats and magazine access, and no guns.

The standing model rules (§8.9 and the user's feedback of 2026-09-28) still apply: no ventral guns, short
heavy barrels sized to the gunhouse, silhouettes checked at game camera distance, built live
in Blender.

**Hull classes** (sizes are set by lane ratings, so every power builds the same classes):

| Class | Lane rating | Length | Job | Who can build it |
|---|---|---|---|---|
| Corvette | I | 80–120 m | Picket, raider, boarding cutter | Any yard |
| Frigate | I | 140–200 m | Escort, point defence, convoy | Any yard |
| Destroyer | II | 220–300 m | Screen, torpedo attack | Any yard |
| **Cruiser** (new) | II | 340–560 m | Independent operations; the backbone of a free company | Any yard |
| Battleship | III | 560–700 m | The battle line | Keel fabs only |
| **Dreadnought** (relic) | III | 900 m+ | Old Meridian capitals, found only as derelicts and relics | Nobody since the Rupture |
| **Tender** (new) | II | 300–400 m | Field repair and refit, salvage storage | Any yard |
| Transport | I–II | 150–350 m | Marines, armies, freight | Any yard |

**Per-faction hardpoints.** Each faction has its own hardpoint layout for each class.
`HullDef` gains a faction dimension, and the Blender contract (`Tools/blender/README.md`,
`check_contract.py`) checks each faction's layout separately. This reverses the old rule that "a
faction expresses itself … never by moving a gun". Moving guns is now exactly how a faction
expresses its doctrine.

**Radiators** become a subsystem with a folded or deployed state and a simple heat model.
Losing radiators slows the guns.

**First model to build:** `OPEN`. One cruiser per faction comes first, as the new quality bar.
The user picks which faction.

### 7.5 Lane Ratings and Keel Fabs: the Case and the Scope

`LOCKED` 2026-11-04 in their **smallest form**. The user was unsure, asked for a proper case
and accepted it.

**Lane ratings.**
- *Precedent:* Germany widened the Kiel Canal (1907–14) because dreadnoughts no longer fit.
  US battleships were held to the Panama Canal locks' beam for decades, and Japan built the
  Yamato class wider, betting the US wouldn't. The 1936 Montreux Convention kept the Black
  Sea's capitals bottled up.
- *What it gives:* geography with teeth (fronts have shapes, and choke points are real); the
  free company's niche (cruisers reach where battle lines can't, which is why powers hire
  you); asymmetry from circumstance (§5.3: the Wardens fly small ships because of their lanes);
  targets for contracts.
- *Example:* the Combine's battleships reach a keystone system at the end of the trunk lanes and
  no further, so it hires a cruiser company to strike the Warden haven beyond it.
- *Without it:* battleships go everywhere and the biggest stack wins, which drifts back toward
  Empire at War.
- *Smallest form, to build first:* three ratings, fixed at galaxy generation and shown on the
  map. Route-finding and the AI respect them. A fleet that can't reach a target says so before
  the order is given.
- *Deferred until the fixed version plays well:* keystones as raidable stations, lanes
  degrading without upkeep, and a raided keystone dropping a lane's rating.

**Keel fabs.**
- *Precedent:* wartime navies almost never replaced lost battleships. A battleship took three
  to five years on one of a handful of slipways. The Royal Navy lost five capitals in the
  Second World War and finished one new class. Japan's third Yamato hull was finished as the
  carrier Shinano. The Washington Treaty capped capitals, and cruisers became the workhorse.
- *What it gives:* precious, named capitals (the core of §1.3); a crown-jewel target for
  contracts; a reason derelicts matter; no battleship spam; a ceiling for the company, which
  can't build capitals and has to buy, restore or capture them.
- *Smallest form:* a keel fab building, three in the galaxy at the start (two Combine, one
  damaged Sovereign). Rating III hulls need one. A new fab can be built from act 2, at huge
  cost and over a long time, so the late game doesn't run out of capitals.

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

**GPU budget (2026-09-30).** The game used to render uncapped, and two full-screen shaders did
per-pixel work every frame: the sky (about twenty octaves of 3D noise) and the galaxy map's
territory plate (a loop over every system). Now: vsync (falling back to a frame cap if the
driver ignores it) and 30 fps when the window is out of focus; the sky's nebula is baked once
to a lat-long texture; the plate's territory and fog are baked only when what the player knows
changes; 32-bit HDR instead of 64-bit; no depth or opaque texture copies; a 2048 shadow map;
no post AA on top of 4× MSAA; particle size capped to limit overdraw; turrets stop animating
when the hull is a few pixels long.

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

Items 1, 2 and 4–10 have assumed answers in §1.2. The free company's own open questions are in
§1.4. Still open:

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
- **2026-09-30 — Tactical layer and performance.** §5.5: terrain (asteroids, nebulae),
  orbital stations built on the galaxy map, the command cap and reinforcements, power
  abilities, a tactical AI that stages assaults and defends stations. The tactical HUD follows
  Empire at War (radar, reinforcement panel, unit cards, ability buttons, health bars). GPU:
  frame pacing (vsync, 30 fps unfocused), the sky's nebula and the map's territory baked to
  textures instead of computed per pixel every frame, lighter pipeline settings (§8.8).
- **2026-10-03 — The signature (proposed).** Added §1.3, the Salvage Fleet: named, scarred
  ships with permanent stumps, a salvage economy of wrecks, ruins and relics, and combat as a
  duel of arcs on a close capital camera. Includes the per-layer impact, risks, build order
  and the existing text it would change. Awaiting sign-off.
- **2026-11-04 — New direction: the free company.** The user did not want an Empire at
  War-style conquest game as the end goal. Of three routes proposed (exodus fleet, free company,
  admiralty), the free company was chosen. §1 rewritten and §1.4 added: the player runs a
  mercenary fleet in a war the AI powers fight. It covers contracts generated from the war's
  needs, per-power standing and a single reputation ("word"), finances, detachments under hired
  captains, a mobile tender plus leased yards, and branching endings (the commission/admiralty,
  founding a state, the legend). §1.3 becomes the company's core loop. The built
  power-level systems become the AI's world. Build order and open questions are in §1.4.
- **2026-11-04 — Free company open questions settled.** The player chooses the starting
  company (three proposed origins, Part H). The company hires marines for boarding, station
  seizure and landings (Part I). Rival AI companies compete, can be hired against you and can
  be absorbed (Part J). The player picks the heraldry (Part K). Build order grew to nine steps.
- **2026-11-04 — World, powers and ship doctrine locked.** §7 rewritten. The world (§7.1): the
  Meridian, lanes held open by keystones, the Rupture as the master keystone cracking, plus
  rules on lane ratings, keel fabs, heat and radiators, the battle plane and scarce crews. Five
  new powers (§7.2), each modelled on a real wartime navy: Sovereign Fleet, Foundry Combine,
  March Wardens, Keystone Synod, Charter Houses. They map onto the existing faction slots, and
  the old Serac/Ilium/Hearthward roster proposal is superseded. Ship design doctrine (§7.4):
  wartime principles, shared anatomy, the hull class table (cruiser and tender new, dreadnought
  as a relic) and per-faction hardpoints, which reverses the shared-contract rule. Lane ratings
  and keel fabs (§7.5) are adopted in their smallest form, with the case recorded. Minor
  renamed: Vesper Courier Guild.

