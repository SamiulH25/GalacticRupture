using System.Collections.Generic;

namespace GalacticRupture.Sim.Strategic
{
    // The enemy's strategy, every few hours: keep the yards busy, gather strength, defend what
    // is under siege, and strike the weakest target it can reach and outweigh.
    public static class CampaignAi
    {
        public static void Think(Campaign c, int faction)
        {
            Build(c, faction);
            MergeIdle(c, faction);
            Defend(c, faction);
            Attack(c, faction);
        }

        static void Build(Campaign c, int faction)
        {
            foreach (var s in c.Galaxy.Systems)
            {
                if (s.Owner != faction || !s.Shipyard) continue;
                if (c.Yards.TryGetValue(s.Id, out var q) && q.Count > 0) continue;
                // A light, fast navy: mostly escorts, destroyers for weight.
                float roll = c.Rng.NextFloat();
                HullDef pick = roll < 0.3f ? Hulls.Destroyer : roll < 0.7f ? Hulls.Frigate : Hulls.Corvette;
                if (!c.QueueBuild(faction, s.Id, pick)) c.QueueBuild(faction, s.Id, Hulls.Corvette);
            }
        }

        static void MergeIdle(Campaign c, int faction)
        {
            var fleets = new List<Fleet>(c.Fleets);
            foreach (var a in fleets)
            {
                if (!a.Alive || a.Owner != faction || a.InTransit) continue;
                foreach (var b in fleets)
                    if (b != a && b.Alive && b.Owner == faction && !b.InTransit && b.System == a.System)
                        c.Merge(a.Id, b.Id);
            }
        }

        static void Defend(Campaign c, int faction)
        {
            foreach (var s in c.Galaxy.Systems)
            {
                if (s.Owner != faction || s.Besieger < 0) continue;
                Fleet best = null;
                float bestLen = float.MaxValue;
                foreach (var f in c.Fleets)
                {
                    if (!f.Alive || f.Owner != faction || f.InTransit) continue;
                    var route = c.Galaxy.Route(f.System, s.Id);
                    if (route.Count > 0 && route.Count < bestLen)
                    {
                        bestLen = route.Count;
                        best = f;
                    }
                }
                if (best != null && best.Strength > c.Strength(s.Besieger, s.Id) * 0.8f) c.MoveFleet(best.Id, s.Id);
            }
        }

        static void Attack(Campaign c, int faction)
        {
            foreach (var f in new List<Fleet>(c.Fleets))
            {
                if (!f.Alive || f.Owner != faction || f.InTransit || f.Route.Count > 0) continue;
                if (c.Galaxy.Systems[f.System].Owner != faction) continue; // it is taking this one: stay
                if (f.Strength < 700f) continue; // too small to go out alone; wait for reinforcements

                StarSystem best = null;
                float bestScore = float.MaxValue;
                foreach (var s in c.Galaxy.Systems)
                {
                    if (s.Owner == faction) continue;
                    var route = c.Galaxy.Route(f.System, s.Id);
                    if (route.Count == 0 || route.Count > 3) continue;
                    float defence = 0f;
                    foreach (var o in c.Fleets)
                        if (o.Alive && o.Owner != faction && !o.InTransit && o.System == s.Id) defence += o.Strength;
                    float needed = s.Capital ? 1.8f : 1.25f;
                    if (f.Strength < defence * needed) continue;
                    float score = route.Count * 10f + defence * 0.01f - s.Tier * 4f - (s.Owner < 0 ? 5f : 0f);
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
