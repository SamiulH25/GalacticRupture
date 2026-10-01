using System.Collections.Generic;

namespace GalacticRupture.Sim.Strategic
{
    public sealed class Engagement
    {
        public int System = -1;           // -1: in a lane, between LaneA and LaneB
        public int LaneA = -1, LaneB = -1;
        public float LanePosition;        // light-years from LaneA
        public int Attacker = -1;         // the faction that arrived last
        public readonly List<int> Fleets = new List<int>();
        public readonly Dictionary<int, int> Sides = new Dictionary<int, int>(); // faction -> 0 or 1

        public bool InLane => System < 0;
        public bool Involves(int faction) => Sides.ContainsKey(faction);
        public int SideOf(int faction) => Sides.TryGetValue(faction, out int s) ? s : -1;
    }

    public sealed partial class Campaign
    {
        // Two coalitions from the factions present: the anchor and its allies, against everyone
        // present who is at war with them (and their allies). Neutrals sit it out.
        Engagement BuildSides(List<int> present)
        {
            int anchor = -1;
            foreach (int a in present)
            {
                bool hostile = false;
                foreach (int b in present) hostile |= Hostile(a, b);
                if (!hostile) continue;
                if (a == PlayerFaction) { anchor = a; break; }
                if (anchor < 0) anchor = a;
            }
            if (anchor < 0) return null;

            var e = new Engagement();
            e.Sides[anchor] = 0;
            foreach (int a in present)
                if (a != anchor && Stance(a, anchor) == DiplomaticStance.Alliance) e.Sides[a] = 0;
            foreach (int a in present)
            {
                if (e.Sides.ContainsKey(a)) continue;
                foreach (var kv in e.Sides)
                    if (kv.Value == 0 && Hostile(a, kv.Key)) { e.Sides[a] = 1; break; }
            }
            foreach (int a in present)
            {
                if (e.Sides.ContainsKey(a)) continue;
                foreach (var kv in new Dictionary<int, int>(e.Sides))
                    if (kv.Value == 1 && Stance(a, kv.Key) == DiplomaticStance.Alliance) { e.Sides[a] = 1; break; }
            }
            bool two = false;
            foreach (var kv in e.Sides) two |= kv.Value == 1;
            return two ? e : null;
        }

        // In the system, or passing through it this hour.
        bool At(Fleet f, int system) => f.System == system && (!f.InTransit || (f.Progress <= 0f && f.ArrivedHour >= Hours));

        void DetectEngagements()
        {
            if (CeasefireHours > 0f) return;
            var present = new List<int>();
            foreach (var s in Galaxy.Systems)
            {
                present.Clear();
                foreach (var f in Fleets)
                    if (f.Alive && At(f, s.Id) && !present.Contains(f.Owner)) present.Add(f.Owner);
                if (present.Count == 0) continue;
                // A station fights for its system: hostile fleets there, even passing through, must
                // deal with it before they can go on or land.
                if (s.Station > 0 && s.StationHull > 0f && s.Owner >= 0 && !present.Contains(s.Owner)) present.Add(s.Owner);
                if (present.Count < 2) continue;
                var e = BuildSides(present);
                if (e == null) continue;
                e.System = s.Id;
                double latest = -1;
                foreach (var f in Fleets)
                {
                    if (!f.Alive || !At(f, s.Id) || !e.Involves(f.Owner)) continue;
                    e.Fleets.Add(f.Id);
                    // Nobody slips past a battle, including fleets only passing through.
                    f.Route.Clear();
                    f.Next = -1;
                    f.Progress = 0f;
                    if (f.ArrivedHour > latest)
                    {
                        latest = f.ArrivedHour;
                        e.Attacker = f.Owner;
                    }
                }
                if (Begin(e)) return;
            }
        }

        // Hostile fleets on the same lane meet in deep space (IDEAS.md §4: transit is a combat space).
        void DetectInterceptions()
        {
            if (CeasefireHours > 0f) return;
            for (int i = 0; i < Fleets.Count; i++)
            {
                var a = Fleets[i];
                if (!a.Alive || !a.InTransit || a.Held) continue;
                for (int j = i + 1; j < Fleets.Count; j++)
                {
                    var b = Fleets[j];
                    if (!b.Alive || !b.InTransit || b.Held || !Hostile(a.Owner, b.Owner)) continue;
                    bool sameLane = (a.System == b.System && a.Next == b.Next) || (a.System == b.Next && a.Next == b.System);
                    if (!sameLane) continue;
                    float len = Galaxy.LaneLength(a.System, a.Next);
                    int laneA = System.Math.Min(a.System, a.Next), laneB = System.Math.Max(a.System, a.Next);
                    float pa = a.System == laneA ? a.Progress : len - a.Progress;
                    float pb = b.System == laneA ? b.Progress : len - b.Progress;
                    bool opposite = a.System != b.System;
                    float reach = opposite ? (a.LyPerDay + b.LyPerDay) / 24f + 0.1f : 0.25f;
                    if (System.MathF.Abs(pa - pb) > reach) continue;

                    var present = new List<int> { a.Owner, b.Owner };
                    var e = BuildSides(present);
                    if (e == null) continue;
                    e.LaneA = laneA;
                    e.LaneB = laneB;
                    e.LanePosition = (pa + pb) * 0.5f;
                    e.Attacker = a.Owner;
                    // Anyone else on this lane nearby and involved joins in.
                    foreach (var f in Fleets)
                    {
                        if (!f.Alive || !f.InTransit || !e.Involves(f.Owner)) continue;
                        bool on = (f.System == laneA && f.Next == laneB) || (f.System == laneB && f.Next == laneA);
                        if (!on) continue;
                        float pf = f.System == laneA ? f.Progress : len - f.Progress;
                        if (System.MathF.Abs(pf - e.LanePosition) > 0.6f) continue;
                        e.Fleets.Add(f.Id);
                        f.Held = true;
                    }
                    Emit(CampaignEventKind.Interception, laneA, a.Owner, b.Owner, text: Galaxy.Systems[laneB].Name);
                    if (Begin(e)) return;
                }
            }
        }

        // Battles the player is part of wait for the player; the rest are fought unattended now.
        bool Begin(Engagement e)
        {
            if (e.Involves(PlayerFaction))
            {
                Pending = e;
                Emit(CampaignEventKind.BattleStarted, e.System >= 0 ? e.System : e.LaneA, e.Attacker, other: e.LaneB);
                return true;
            }
            BattleSetup.AutoResolve(this, e);
            return false;
        }

        // Called once a battle has been written back into the fleets.
        public void EndEngagement(Engagement e, int retreatingSide)
        {
            foreach (int id in e.Fleets)
            {
                var f = FleetById(id);
                if (f != null) DropExcessTroops(f);
            }
            Fleets.RemoveAll(f => !f.Alive);

            foreach (int id in e.Fleets)
            {
                var f = FleetById(id);
                if (f == null) continue;
                f.Held = false;
                if (retreatingSide < 0 || e.SideOf(f.Owner) != retreatingSide) continue;
                if (e.InLane)
                {
                    // Turn back the way it came.
                    float len = Galaxy.LaneLength(f.System, f.Next);
                    (f.System, f.Next) = (f.Next, f.System);
                    f.Progress = System.MathF.Max(0f, len - f.Progress);
                    f.Route.Clear();
                }
                else
                {
                    int to = RetreatTarget(f);
                    if (to >= 0)
                    {
                        f.Next = to;
                        f.Progress = 0f;
                        f.Route.Clear();
                    }
                }
            }
            if (Pending == e) Pending = null;
            Emit(CampaignEventKind.BattleResolved, e.System >= 0 ? e.System : e.LaneA, other: retreatingSide);
        }

        // Back along the lanes to the nearest friendly system, or any neighbour.
        int RetreatTarget(Fleet f)
        {
            int best = -1;
            float bestLen = float.MaxValue;
            foreach (var l in Galaxy.LanesFrom(f.System))
            {
                int n = l.Other(f.System);
                int owner = Galaxy.Systems[n].Owner;
                float score = l.Length + (owner == f.Owner ? 0f : owner >= 0 && Hostile(owner, f.Owner) ? 200f : 100f);
                if (score < bestLen)
                {
                    bestLen = score;
                    best = n;
                }
            }
            return best;
        }
    }
}
