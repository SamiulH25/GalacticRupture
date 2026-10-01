using System.Collections.Generic;

namespace GalacticRupture.Sim.Strategic
{
    // Strategy for every faction the player doesn't run, every few hours: develop the systems,
    // appoint governors, send expeditions into the ruins, keep the yards busy, raise armies and
    // the transports to carry them, gather strength, and invade what it can take, judged on what
    // its intelligence says, not on the truth. Minor powers never expand (IDEAS.md §1.2).
    public static class CampaignAi
    {
        public static void Think(Campaign c, int faction)
        {
            Develop(c, faction);
            Fortify(c, faction);
            Govern(c, faction);
            Build(c, faction);
            MergeIdle(c, faction);
            Expeditions(c, faction);
            Embark(c, faction);
            Reinforce(c, faction);
            Muster(c, faction);
            Invade(c, faction);
        }

        // Systems without a governor are built up by the court directly, keeping a reserve for the yards.
        static void Develop(Campaign c, int faction)
        {
            var fac = c.Factions[faction];
            float reserve = fac.Major ? 350f : 200f;
            foreach (var s in c.Galaxy.Systems)
            {
                if (s.Owner != faction || s.Governor != null) continue;
                var focus = FocusFor(c, s);
                int k = c.Advise(s, focus);
                if (k < 0) continue;
                if (fac.Credits < c.CostHere(s, (BuildingKind)k) + reserve) return;
                c.Construct(faction, s.Id, (BuildingKind)k);
            }
        }

        // Stations where they earn their upkeep: the capital first, then developed worlds on a
        // hostile border, and only out of savings, so the yards are never starved for them.
        static void Fortify(Campaign c, int faction)
        {
            var fac = c.Factions[faction];
            if (fac.Credits < (fac.Major ? 1100f : 700f)) return;
            if (c.Upkeep(faction) > c.Totals(faction).Credits * 0.8f) return;
            StarSystem best = null;
            float bestScore = 0f;
            foreach (var s in c.Galaxy.Systems)
            {
                if (s.Owner != faction || !c.CanBuildStation(faction, s.Id)) continue;
                bool threatened = ThreatenedBorder(c, s);
                int want = s.Capital ? (fac.Major ? 3 : 2) : threatened && s.Tier >= 2 ? 2 : threatened || s.Shipyard ? 1 : 0;
                if (s.Station >= want) continue;
                float score = (s.Capital ? 10f : 0f) + (threatened ? 6f : 0f) + s.Tier * 2f + (s.Shipyard ? 2f : 0f) - s.Station * 3f;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = s;
                }
            }
            if (best != null && fac.Credits >= Campaign.StationCost(best.Station + 1) + (fac.Major ? 700f : 400f))
                c.BuildStation(faction, best.Id);
        }

        // Next to a power we're at war with, or one that doesn't trust us.
        static bool ThreatenedBorder(Campaign c, StarSystem s)
        {
            foreach (var l in c.Galaxy.LanesFrom(s.Id))
            {
                int o = c.Galaxy.Systems[l.Other(s.Id)].Owner;
                if (o >= 0 && o != s.Owner && c.Stance(s.Owner, o) == DiplomaticStance.War) return true;
            }
            return false;
        }

        // The capital builds industry, the border builds for war, and every third quiet world
        // becomes a research world.
        static GovernorFocus FocusFor(Campaign c, StarSystem s) =>
            s.Capital ? GovernorFocus.Industry : Frontier(c, s) ? GovernorFocus.Military : s.Id % 3 == 0 ? GovernorFocus.Research : GovernorFocus.Balanced;

        static bool Frontier(Campaign c, StarSystem s)
        {
            foreach (var l in c.Galaxy.LanesFrom(s.Id))
            {
                int o = c.Galaxy.Systems[l.Other(s.Id)].Owner;
                if (o >= 0 && o != s.Owner) return true;
            }
            return false;
        }

        // Governors for the most populous ungoverned systems, while influence allows.
        static void Govern(Campaign c, int faction)
        {
            var fac = c.Factions[faction];
            if (fac.Influence < Campaign.AppointCost + 30f) return;
            var pool = c.Pool(faction);
            if (pool.Count == 0) return;
            StarSystem best = null;
            foreach (var s in c.Galaxy.Systems)
                if (s.Owner == faction && s.Governor == null && !s.Capital && (best == null || s.Population > best.Population)) best = s;
            if (best == null || best.Population < 4f) return;
            int pick = 0;
            for (int i = 1; i < pool.Count; i++)
                if (pool[i].Loyalty + pool[i].Industry + pool[i].Administration > pool[pick].Loyalty + pool[pick].Industry + pool[pick].Administration) pick = i;
            c.Appoint(faction, best.Id, pick, FocusFor(c, best));
        }

        static bool Working(Campaign c, Fleet f)
        {
            if (f.InTransit) return false;
            var s = c.Galaxy.Systems[f.System];
            return s.Owner < 0 && (s.Reclaimer == f.Owner || s.Courter == f.Owner);
        }

        // Expedition fleets go to the nearest ruin or holdout nobody is working, and stay until it's done.
        static void Expeditions(Campaign c, int faction)
        {
            var fac = c.Factions[faction];
            if (!fac.Major) return;
            var intel = c.Intel[faction];
            int want = 1;
            int have = 0;
            foreach (var f in c.Fleets) if (f.Alive && f.Owner == faction && f.Expedition) have++;
            // Keep enough expeditions: detach a light ship from an idle fleet at home when short.
            if (have < want)
                foreach (var f in new List<Fleet>(c.Fleets))
                {
                    if (!f.Alive || f.Owner != faction || f.Expedition || f.InTransit || f.Ships.Count < 3) continue;
                    if (c.Galaxy.Systems[f.System].Owner != faction || c.CountIn(f) > 0) continue;
                    int light = -1;
                    for (int i = 0; i < f.Ships.Count; i++)
                        if (f.Ships[i].Def.Class <= HullClass.Frigate && (light < 0 || f.Ships[i].Def.Cost < f.Ships[light].Def.Cost)) light = i;
                    if (light < 0) continue;
                    var nf = c.Split(f.Id, new[] { light }, $"Expedition {c.Fleets.Count}");
                    if (nf != null) nf.Expedition = true;
                    break;
                }

            var claimed = new HashSet<int>();
            foreach (var f in c.Fleets)
                if (f.Alive && f.Owner == faction && f.Expedition && f.Destination != f.System) claimed.Add(f.Destination);

            foreach (var f in new List<Fleet>(c.Fleets))
            {
                if (!f.Alive || f.Owner != faction || !f.Expedition || f.InTransit) continue;
                var here = c.Galaxy.Systems[f.System];
                if (here.Owner < 0 && here.Derelict == Derelict.Ruin && c.CanReclaim(faction, here.Id))
                {
                    c.StartReclaim(faction, here.Id);
                    continue;
                }
                if (here.Owner < 0 && here.Derelict == Derelict.Holdout && fac.Influence >= c.CourtCost(here) + 15f && c.CanCourt(faction, here.Id))
                {
                    c.StartCourting(faction, here.Id);
                    continue;
                }
                if (Working(c, f)) continue;

                int best = -1;
                float bestScore = float.MaxValue;
                foreach (var s in c.Galaxy.Systems)
                {
                    if (s.Owner >= 0 || s.Derelict == Derelict.None || s.OldCapital || claimed.Contains(s.Id)) continue;
                    if (intel.OwnerSeen[s.Id] >= 0) continue;
                    if (s.Reclaimer >= 0 && s.Reclaimer != faction || s.Courter >= 0 && s.Courter != faction) continue;
                    if (s.Derelict == Derelict.Holdout && fac.Influence < c.CourtCost(s)) continue;
                    if (intel.HostileStrengthAt(c, faction, s.Id) > 0f) continue;
                    int hops = c.Galaxy.Hops(f.System, s.Id);
                    if (hops == int.MaxValue || hops > 6) continue;
                    // Close to home, and worth having.
                    float near = float.MaxValue;
                    foreach (var o in c.Galaxy.Systems) if (o.Owner == faction) near = System.MathF.Min(near, c.Galaxy.Distance(o.Id, s.Id));
                    float score = near + hops * 4f - s.Habitability - s.Slots * 2f;
                    if (score < bestScore) { bestScore = score; best = s.Id; }
                }
                if (best >= 0)
                {
                    claimed.Add(best);
                    c.MoveFleet(f.Id, best);
                }
            }
        }

        static int Owned(Campaign c, int faction)
        {
            int n = 0;
            foreach (var s in c.Galaxy.Systems) if (s.Owner == faction) n++;
            return n;
        }

        static void Build(Campaign c, int faction)
        {
            var fac = c.Factions[faction];
            int armies = 0, transports = 0;
            foreach (var a in c.Armies) if (a.Owner == faction) armies++;
            foreach (var f in c.Fleets)
                if (f.Alive && f.Owner == faction)
                    foreach (var r in f.Ships) if (r.Def.Class == HullClass.Transport) transports++;
            int wantArmies = fac.Major ? 4 + Owned(c, faction) / 2 : 1 + Owned(c, faction) / 3;

            HullDef D(HullClass cls, HullDef stock)
            {
                var d = c.DesignFor(faction, cls);
                return d != null ? c.Buildable(faction, d) : stock;
            }
            foreach (var s in c.Galaxy.Systems)
            {
                if (s.Owner != faction || !s.Shipyard || s.Unrest > 0f) continue;
                int queued = c.Yards.TryGetValue(s.Id, out var q) ? q.Count : 0;
                if (queued >= (fac.Credits > 900f ? 3 : 1)) continue;
                if (armies < wantArmies && c.RaiseArmy(faction, s.Id))
                {
                    armies++;
                    continue;
                }
                if (transports < armies && c.QueueBuild(faction, s.Id, D(HullClass.Transport, Hulls.Transport)))
                {
                    transports++;
                    continue;
                }
                float roll = c.Rng.NextFloat();
                bool rich = fac.Credits > 2500f && s.Count(BuildingKind.Yard) >= 2;
                HullDef pick = fac.Major
                    ? (rich && roll < 0.15f ? D(HullClass.Battleship, Hulls.Battleship) : roll < 0.4f ? D(HullClass.Destroyer, Hulls.Destroyer) : roll < 0.75f ? D(HullClass.Frigate, Hulls.Frigate) : D(HullClass.Corvette, Hulls.Corvette))
                    : (roll < 0.15f ? D(HullClass.Destroyer, Hulls.Destroyer) : roll < 0.6f ? D(HullClass.Frigate, Hulls.Frigate) : D(HullClass.Corvette, Hulls.Corvette));
                // Don't build a navy the treasury can't keep, unless there are savings to burn.
                if (c.Upkeep(faction) > c.Totals(faction).Credits * (fac.Credits > 4000f ? 1.2f : 0.6f)) continue;
                if (!c.QueueBuild(faction, s.Id, pick)) c.QueueBuild(faction, s.Id, D(HullClass.Corvette, Hulls.Corvette));
            }
        }

        static void MergeIdle(Campaign c, int faction)
        {
            var fleets = new List<Fleet>(c.Fleets);
            foreach (var a in fleets)
            {
                if (!a.Alive || a.Owner != faction || a.InTransit || a.Route.Count > 0 || a.Expedition) continue;
                if (c.Galaxy.Systems[a.System].Owner != faction) continue;
                foreach (var b in fleets)
                    if (b != a && b.Alive && b.Owner == faction && !b.InTransit && b.Route.Count == 0 && b.System == a.System && !b.Expedition)
                        c.Merge(a.Id, b.Id);
            }
        }

        // Armies in a system with a fleet that has room go aboard, keeping one at the capital.
        static void Embark(Campaign c, int faction)
        {
            int capital = c.Factions[faction].Capital;
            foreach (var f in c.Fleets)
            {
                if (!f.Alive || f.Owner != faction || f.InTransit) continue;
                foreach (var a in new List<Army>(c.ArmiesAt(f.System)))
                {
                    if (a.Owner != faction) continue;
                    if (f.System == capital && CountHome(c, faction, capital) <= 1) break;
                    if (!c.Embark(f.Id, a.Id)) break;
                }
            }
        }

        static int CountHome(Campaign c, int faction, int system)
        {
            int n = 0;
            foreach (var a in c.ArmiesAt(system)) if (a.Owner == faction) n++;
            return n;
        }

        // Our own system has enemy armies on it: send a fleet with troops to put them down there.
        static void Reinforce(Campaign c, int faction)
        {
            foreach (var s in c.Galaxy.Systems)
            {
                if (s.Owner != faction || s.Besieger < 0) continue;
                foreach (var f in c.Fleets)
                {
                    if (!f.Alive || f.Owner != faction || f.InTransit) continue;
                    if (f.System == s.Id)
                    {
                        foreach (var a in new List<Army>(c.ArmiesIn(f))) c.Disembark(a.Id);
                        continue;
                    }
                    if (c.CountIn(f) > 0 && c.Galaxy.Hops(f.System, s.Id) <= 3)
                    {
                        c.MoveFleet(f.Id, s.Id);
                        break;
                    }
                }
            }
        }

        // Empty troop ships go and fetch armies waiting in our systems.
        static void Muster(Campaign c, int faction)
        {
            int capital = c.Factions[faction].Capital;
            foreach (var f in c.Fleets)
            {
                if (!f.Alive || f.Owner != faction || f.InTransit || f.Route.Count > 0 || f.Expedition) continue;
                if (c.CountIn(f) >= f.TroopCapacity || f.TroopCapacity == 0) continue;
                int best = -1, bestLen = int.MaxValue;
                foreach (var s in c.Galaxy.Systems)
                {
                    if (s.Owner != faction || s.Id == f.System) continue;
                    int spare = CountHome(c, faction, s.Id) - (s.Id == capital ? 1 : 0);
                    if (spare <= 0) continue;
                    int len = c.Galaxy.Hops(f.System, s.Id);
                    if (len > 0 && len < bestLen)
                    {
                        bestLen = len;
                        best = s.Id;
                    }
                }
                if (best >= 0 && c.CountIn(f) == 0) c.MoveFleet(f.Id, best);
            }
        }

        static void Invade(Campaign c, int faction)
        {
            var fac = c.Factions[faction];
            var intel = c.Intel[faction];
            foreach (var f in new List<Fleet>(c.Fleets))
            {
                if (!f.Alive || f.Owner != faction || f.InTransit || f.Route.Count > 0 || f.Expedition) continue;
                if (c.Galaxy.Systems[f.System].Besieger == faction) continue;   // covering a landing
                int troops = c.CountIn(f);
                if (troops == 0) continue;
                if (f.Strength < (fac.Major ? 500f : 300f)) continue;

                StarSystem best = null;
                float bestScore = float.MaxValue;
                foreach (var s in c.Galaxy.Systems)
                {
                    int owner = intel.OwnerSeen[s.Id];
                    if (owner == faction) continue;
                    // Majors take holdouts by force as well as enemies; minors fight only their wars.
                    bool open = owner < 0 ? fac.Major && s.Derelict == Derelict.Holdout && (!s.OldCapital || c.Act >= 3) && (c.Act >= 2 || c.Galaxy.Hops(f.System, s.Id) <= 2)
                                          : c.Stance(faction, owner) == DiplomaticStance.War;
                    if (!open || s.Courter >= 0) continue;
                    int hops = c.Galaxy.Hops(f.System, s.Id);
                    if (hops == 0 || hops > (fac.Major ? 4 : 2)) continue;

                    // Judge by what we know; unknown places are assumed moderately held.
                    // A station is seen from afar and fights with the fleets: bring enough for both.
                    float fleets = intel.HostileStrengthAt(c, faction, s.Id)
                                   + (intel.SeenHour[s.Id] >= 0 ? Campaign.StationStrength(s) * 1.2f : 0f);
                    float garrison = intel.SeenHour[s.Id] >= 0 ? intel.GarrisonSeen[s.Id] : Campaign.MaxMilitia(s);
                    if (f.Strength < fleets * (s.Capital ? 1.8f : 1.3f)) continue;
                    if (troops * 100f < garrison * (s.Capital ? 1.8f : 1.35f)) continue;

                    float score = hops * 10f + fleets * 0.01f + garrison * 0.05f - s.Tier * 6f
                                  - (owner < 0 ? 4f : 0f) - (s.Capital ? 12f : 0f);
                    if (score < bestScore)
                    {
                        bestScore = score;
                        best = s;
                    }
                }
                if (best != null)
                {
                    c.MoveFleet(f.Id, best.Id);
                    continue;
                }
                Stage(c, faction, f);
            }
        }

        // Nothing in reach: move up to the friendly system nearest the closest enemy, and wait there.
        static void Stage(Campaign c, int faction, Fleet f)
        {
            var intel = c.Intel[faction];
            int enemy = -1, enemyHops = int.MaxValue;
            foreach (var s in c.Galaxy.Systems)
            {
                int owner = intel.OwnerSeen[s.Id];
                if (owner < 0 || owner == faction || c.Stance(faction, owner) != DiplomaticStance.War) continue;
                int h = c.Galaxy.Hops(f.System, s.Id);
                if (h < enemyHops) { enemyHops = h; enemy = s.Id; }
            }
            if (enemy < 0)
            {
                // At peace with everyone in sight: go home.
                int home = c.Factions[faction].Capital;
                if (home >= 0 && c.Galaxy.Systems[f.System].Owner != faction) c.MoveFleet(f.Id, home);
                return;
            }
            int stage = -1, stageHops = int.MaxValue;
            foreach (var s in c.Galaxy.Systems)
            {
                if (s.Owner != faction) continue;
                int h = c.Galaxy.Hops(s.Id, enemy);
                if (h < stageHops || h == stageHops && stage >= 0 && c.Galaxy.Hops(f.System, s.Id) < c.Galaxy.Hops(f.System, stage)) { stageHops = h; stage = s.Id; }
            }
            if (stage >= 0 && stage != f.System) c.MoveFleet(f.Id, stage);
        }

        // --- Research and designs ---------------------------------------------------------------

        // Each major leans toward two branches: the shape of its tech tree is its identity
        // (IDEAS.md §5.4). Minors look to their defences and their economies.
        static TechBranch[] Doctrine(Campaign c, int faction) => c.Factions[faction].Name switch
        {
            "Continuance" => new[] { TechBranch.Energy, TechBranch.Defence },
            "Remnant" => new[] { TechBranch.Ballistic, TechBranch.Logistics },
            "Hierarchate" => new[] { TechBranch.Disruptive, TechBranch.Industry },
            "Free Yards" => new[] { TechBranch.Ballistic, TechBranch.Industry },
            "Lantern Synod" => new[] { TechBranch.Energy, TechBranch.Disruptive },
            _ => new[] { TechBranch.Defence, TechBranch.Industry },
        };

        public static TechDef PickResearch(Campaign c, int faction)
        {
            var doctrine = Doctrine(c, faction);
            TechDef best = null;
            float bestScore = float.MinValue;
            foreach (var t in Techs.All)
            {
                if (!c.CanResearch(faction, t)) continue;
                float score = 1000f / t.Cost * (System.Array.IndexOf(doctrine, t.Branch) >= 0 ? 3f : 1f) * (0.7f + 0.6f * c.Rng.NextFloat());
                if (score > bestScore) { bestScore = score; best = t; }
            }
            if (best != null) c.Enqueue(faction, best.Id);
            return best;
        }

        // New guns: draw up a fresh design for every hull and build that from now on.
        public static void Redesign(Campaign c, int faction)
        {
            var fac = c.Factions[faction];
            var doctrine = Doctrine(c, faction);
            foreach (var hull in Hulls.All)
            {
                var stock = hull;
                var d = new ShipDesign($"{hull.Class} Mk {fac.TechVersion + 1}", hull);
                for (int i = 0; i < hull.Hardpoints.Count; i++)
                {
                    var hp = hull.Hardpoints[i];
                    bool screen = hp.Mount.Class == MountClass.PointDefence;
                    MountDef pick = hp.Mount;
                    float best = float.MinValue;
                    foreach (var m in Hulls.Mounts)
                    {
                        if (!ShipDesign.Fits(m, hp) || !c.CanUse(faction, m)) continue;
                        // Screening slots stay screens; the rest carry the heaviest gun that fits.
                        if (screen != (m.Class == MountClass.PointDefence)) continue;
                        float dps = m.Damage * m.Barrels / m.Reload * m.ShieldFactor * (1f + m.Pierce * 0.3f);
                        float score = dps * System.MathF.Sqrt(m.Range / 3000f) * (m.Size == hp.Size ? 1.3f : 1f);
                        if (m.Channel == DamageChannel.Disruptive) score *= System.Array.IndexOf(doctrine, TechBranch.Disruptive) >= 0 ? 1.1f : 0.6f;
                        if (m.Channel == DamageChannel.Energy && System.Array.IndexOf(doctrine, TechBranch.Energy) >= 0) score *= 1.2f;
                        if (m.Channel == DamageChannel.Ballistic && System.Array.IndexOf(doctrine, TechBranch.Ballistic) >= 0) score *= 1.2f;
                        if (score > best) { best = score; pick = m; }
                    }
                    d.Set(i, pick);
                }
                // Replace the last generated design of this class, keep hand-made ones.
                fac.Designs.RemoveAll(o => o.Hull == stock && o.Name.StartsWith($"{hull.Class} Mk "));
                fac.Designs.Insert(0, d);
            }
        }
    }
}
