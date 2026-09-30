namespace GalacticRupture.Sim.Strategic
{
    // Expansion is reclamation, not colonisation (IDEAS.md §1.1). Ruins are restored by an
    // expedition: a fleet that stays on station while the work is paid for and done. Holdouts
    // are alive and armed; they are brought in with influence while a fleet shows the flag, or
    // taken by landing an army.
    public sealed partial class Campaign
    {
        public int ReclaimCost(int faction, StarSystem s)
        {
            int dormant = 0;
            foreach (var b in s.Buildings) if (b.Dormant) dormant++;
            return (int)((150 + 60 * dormant) * Factions[faction].Mods.ReclaimFactor);
        }

        public float ReclaimDays(int faction, StarSystem s)
        {
            int dormant = 0;
            foreach (var b in s.Buildings) if (b.Dormant) dormant++;
            return (30f + 8f * dormant) * Factions[faction].Mods.ReclaimFactor;
        }

        public int CourtCost(StarSystem s) => 30 + (int)(6f * s.Population);
        public const float CourtDays = 30f;

        // Has a fleet on station in the system, with no hostile warships present.
        public bool OnStation(int faction, int system)
        {
            bool ours = false;
            foreach (var f in Fleets)
            {
                if (!f.Alive || f.InTransit || f.System != system) continue;
                if (f.Owner == faction) ours = true;
                else if (Hostile(f.Owner, faction)) return false;
            }
            return ours;
        }

        public bool CanReclaim(int faction, int system)
        {
            var s = Galaxy.Systems[system];
            if (s.Owner >= 0 || s.Derelict != Derelict.Ruin) return false;
            if (s.Reclaimer >= 0 && s.Reclaimer != faction && OnStation(s.Reclaimer, system)) return false;
            if (s.Reclaimer == faction) return false;
            return OnStation(faction, system) && Factions[faction].Credits >= ReclaimCost(faction, s);
        }

        public bool StartReclaim(int faction, int system)
        {
            if (!CanReclaim(faction, system)) return false;
            var s = Galaxy.Systems[system];
            Factions[faction].Credits -= ReclaimCost(faction, s);
            s.Reclaimer = faction;
            s.Reclaim = 0f;
            Emit(CampaignEventKind.ReclamationStarted, system, faction);
            return true;
        }

        public bool CanCourt(int faction, int system)
        {
            var s = Galaxy.Systems[system];
            if (s.Owner >= 0 || s.Derelict != Derelict.Holdout || s.Besieger >= 0 || s.OldCapital) return false;
            if (s.Courter >= 0 && s.Courter != faction && OnStation(s.Courter, system)) return false;
            if (s.Courter == faction) return false;
            return OnStation(faction, system) && Factions[faction].Influence >= CourtCost(s);
        }

        public bool StartCourting(int faction, int system)
        {
            if (!CanCourt(faction, system)) return false;
            var s = Galaxy.Systems[system];
            Factions[faction].Influence -= CourtCost(s);
            s.Courter = faction;
            s.Courting = 0f;
            Emit(CampaignEventKind.CourtingStarted, system, faction);
            return true;
        }

        // Work goes on only while the expedition's fleet holds station.
        void Reclamation()
        {
            foreach (var s in Galaxy.Systems)
            {
                if (s.Owner >= 0) continue;
                if (s.Reclaimer >= 0 && OnStation(s.Reclaimer, s.Id))
                {
                    s.Reclaim += 1f / (ReclaimDays(s.Reclaimer, s) * 24f);
                    if (s.Reclaim >= 1f) Restore(s, s.Reclaimer);
                }
                else if (s.Courter >= 0 && OnStation(s.Courter, s.Id))
                {
                    s.Courting += 1f / (CourtDays * 24f);
                    if (s.Courting >= 1f) Restore(s, s.Courter);
                }
            }
        }

        void Restore(StarSystem s, int faction)
        {
            bool ruin = s.Derelict == Derelict.Ruin;
            s.Owner = faction;
            s.Derelict = Derelict.None;
            s.Reclaimer = s.Courter = -1;
            s.Reclaim = s.Courting = 0f;
            s.Unrest = 0f;
            if (ruin)
            {
                // The expedition brings the first settlers, and the old plant comes back online.
                s.Population = System.MathF.Max(s.Population, 1f);
                foreach (var b in s.Buildings) b.Dormant = false;
                s.Stability = 50f;
            }
            else s.Stability = 45f;
            s.Militia = MilitiaCap(s) * 0.5f;
            Emit(ruin ? CampaignEventKind.SystemReclaimed : CampaignEventKind.HoldoutJoined, s.Id, faction);
            OfferNotable(faction, s);
        }
    }
}
