namespace GalacticRupture.Sim.Strategic
{
    // The campaign's three acts (IDEAS.md §1.1): rupture, blocs, total war. Acts turn on what
    // the galaxy has become, with a date as a backstop so a quiet galaxy still moves on.
    public sealed partial class Campaign
    {
        public const float Act2Day = 540f;        // a year and a half
        public const float Act3Day = 1460f;       // four years
        public const float ThroneDays = 90f;      // hold the old capital this long in act 3 to win

        public int Act { get; private set; } = 1;
        public int ThroneHolder { get; private set; } = -1;
        public double ThroneSince { get; private set; }
        int _derelictsAtStart = -1;
        double _act2Hour;

        public int OldCapital
        {
            get
            {
                foreach (var s in Galaxy.Systems) if (s.OldCapital) return s.Id;
                return -1;
            }
        }

        public int DerelictsLeft
        {
            get
            {
                int n = 0;
                foreach (var s in Galaxy.Systems) if (s.Derelict != Derelict.None && s.Owner < 0) n++;
                return n;
            }
        }

        // The share of all systems held by a faction and its allies.
        public float BlocShare(int faction)
        {
            int held = 0;
            foreach (var s in Galaxy.Systems)
                if (s.Owner >= 0 && (s.Owner == faction || Stance(s.Owner, faction) == DiplomaticStance.Alliance)) held++;
            return held / (float)System.Math.Max(1, Galaxy.Systems.Count);
        }

        void Acts()
        {
            if (_derelictsAtStart < 0) _derelictsAtStart = DerelictsLeft;
            if (Act == 1 && (Day >= Act2Day || (_derelictsAtStart > 0 && DerelictsLeft <= _derelictsAtStart * 0.15f)))
            {
                Act = 2;
                _act2Hour = Hours;
                Emit(CampaignEventKind.ActBegan, text: "2");
            }
            if (Act == 2)
            {
                bool bloc = false;
                foreach (var f in Factions) if (f.Major && !f.Eliminated && BlocShare(f.Id) >= 0.5f) bloc = true;
                bloc &= Hours - _act2Hour >= 365 * 24;
                if (Day >= Act3Day || bloc)
                {
                    Act = 3;
                    Emit(CampaignEventKind.ActBegan, text: "3");
                }
            }

            int throne = OldCapital;
            if (throne >= 0)
            {
                int holder = Galaxy.Systems[throne].Owner;
                if (holder != ThroneHolder)
                {
                    ThroneHolder = holder;
                    ThroneSince = Hours;
                }
                if (Act == 3 && holder >= 0 && ThroneHeldDays >= ThroneDays)
                    State = holder == PlayerFaction || Stance(holder, PlayerFaction) == DiplomaticStance.Alliance
                        ? CampaignState.Victory : CampaignState.Defeat;
            }
        }

        public float ThroneHeldDays => ThroneHolder < 0 ? 0f : (float)((Hours - ThroneSince) / 24.0);

        void CheckEnd()
        {
            foreach (var fac in Factions)
            {
                if (fac.Eliminated) continue;
                bool any = false;
                foreach (var s in Galaxy.Systems) if (s.Owner == fac.Id) { any = true; break; }
                if (any) continue;
                fac.Eliminated = true;
                Emit(CampaignEventKind.FactionEliminated, faction: fac.Id);
            }
            var player = Factions[PlayerFaction];
            if (player.Eliminated)
            {
                State = CampaignState.Defeat;
                return;
            }
            if (OldCapital < 0)
            {
                // The small map: the war with one enemy is decided at the two capitals.
                if (Galaxy.Systems[player.Capital].Owner != PlayerFaction) State = CampaignState.Defeat;
                else if (Galaxy.Systems[Factions[EnemyFaction].Capital].Owner == PlayerFaction) State = CampaignState.Victory;
                return;
            }
            // The last major power standing has won, whatever act it is.
            bool rival = false;
            foreach (var f in Factions) if (f.Major && !f.Eliminated && f.Id != PlayerFaction) rival = true;
            if (!rival) State = CampaignState.Victory;
        }
    }
}
