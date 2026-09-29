using System.Collections.Generic;

namespace GalacticRupture.Sim.Strategic
{
    // Strategy for every faction the player doesn't run, every few hours: keep the yards busy,
    // raise armies and the transports to carry them, gather strength, and invade what it can
    // take, judged on what its intelligence says, not on the truth.
    public static class CampaignAi
    {
        public static void Think(Campaign c, int faction)
        {
            Build(c, faction);
            MergeIdle(c, faction);
            Embark(c, faction);
            LandWhereArrived(c, faction);
            Reinforce(c, faction);
            Muster(c, faction);
            Invade(c, faction);
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

            foreach (var s in c.Galaxy.Systems)
            {
                if (s.Owner != faction || !s.Shipyard) continue;
                int queued = c.Yards.TryGetValue(s.Id, out var q) ? q.Count : 0;
                if (queued >= (fac.Credits > 900f ? 3 : 1)) continue;
                if (armies < wantArmies && c.RaiseArmy(faction, s.Id))
                {
                    armies++;
                    continue;
                }
                if (transports < armies && c.QueueBuild(faction, s.Id, Hulls.Transport))
                {
                    transports++;
                    continue;
                }
                float roll = c.Rng.NextFloat();
                HullDef pick = fac.Major
                    ? (roll < 0.3f ? Hulls.Destroyer : roll < 0.7f ? Hulls.Frigate : Hulls.Corvette)
                    : (roll < 0.15f ? Hulls.Destroyer : roll < 0.6f ? Hulls.Frigate : Hulls.Corvette);
                if (!c.QueueBuild(faction, s.Id, pick)) c.QueueBuild(faction, s.Id, Hulls.Corvette);
            }
        }

        static void MergeIdle(Campaign c, int faction)
        {
            var fleets = new List<Fleet>(c.Fleets);
            foreach (var a in fleets)
            {
                if (!a.Alive || a.Owner != faction || a.InTransit || a.Route.Count > 0) continue;
                if (c.Galaxy.Systems[a.System].Owner != faction) continue;
                foreach (var b in fleets)
                    if (b != a && b.Alive && b.Owner == faction && !b.InTransit && b.Route.Count == 0 && b.System == a.System)
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

        static void LandWhereArrived(Campaign c, int faction)
        {
            foreach (var f in c.Fleets)
                if (f.Alive && f.Owner == faction && !f.InTransit && c.CanLand(f)) c.Land(f.Id);
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
                    if (c.CountIn(f) > 0 && c.Galaxy.Route(f.System, s.Id).Count <= 3)
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
                if (!f.Alive || f.Owner != faction || f.InTransit || f.Route.Count > 0) continue;
                if (c.CountIn(f) >= f.TroopCapacity || f.TroopCapacity == 0) continue;
                int best = -1, bestLen = int.MaxValue;
                foreach (var s in c.Galaxy.Systems)
                {
                    if (s.Owner != faction || s.Id == f.System) continue;
                    int spare = CountHome(c, faction, s.Id) - (s.Id == capital ? 1 : 0);
                    if (spare <= 0) continue;
                    int len = c.Galaxy.Route(f.System, s.Id).Count;
                    if (len > 0 && len < bestLen)
                    {
                        bestLen = len;
                        best = s.Id;
                    }
                }
                if (best >= 0 && (c.CountIn(f) == 0 || bestLen <= 2)) c.MoveFleet(f.Id, best);
            }
        }

        static void Invade(Campaign c, int faction)
        {
            var fac = c.Factions[faction];
            var intel = c.Intel[faction];
            foreach (var f in new List<Fleet>(c.Fleets))
            {
                if (!f.Alive || f.Owner != faction || f.InTransit || f.Route.Count > 0) continue;
                if (c.Galaxy.Systems[f.System].Owner != faction) continue;
                int troops = c.CountIn(f);
                if (troops == 0) continue;
                if (f.Strength < (fac.Major ? 500f : 300f)) continue;

                StarSystem best = null;
                float bestScore = float.MaxValue;
                foreach (var s in c.Galaxy.Systems)
                {
                    int owner = intel.OwnerSeen[s.Id];
                    if (owner == faction) continue;
                    bool open = owner < 0 || c.Stance(faction, owner) == DiplomaticStance.War;
                    if (!open) continue;
                    var route = c.Galaxy.Route(f.System, s.Id);
                    if (route.Count == 0 || route.Count > (fac.Major ? 4 : 2)) continue;

                    // Judge by what we know; unknown places are assumed moderately held.
                    float fleets = intel.HostileStrengthAt(c, faction, s.Id);
                    float garrison = intel.SeenHour[s.Id] >= 0 ? intel.GarrisonSeen[s.Id] : 60f * s.Tier;
                    if (f.Strength < fleets * (s.Capital ? 1.8f : 1.3f)) continue;
                    if (troops * 100f < garrison * (s.Capital ? 1.8f : 1.35f)) continue;

                    float score = route.Count * 10f + fleets * 0.01f + garrison * 0.05f - s.Tier * 6f
                                  - (owner < 0 ? 4f : 0f) - (s.Capital ? 12f : 0f);
                    if (score < bestScore)
                    {
                        bestScore = score;
                        best = s;
                    }
                }
                if (best != null) c.MoveFleet(f.Id, best.Id);
            }
        }
    }
}
