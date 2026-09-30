using System.Collections.Generic;

namespace GalacticRupture.Sim.Strategic
{
    public enum GovernorTrait
    {
        Frugal,     // takes less
        Builder,    // builds faster
        Populist,   // keeps the peace
        Greedy,     // takes more
        Martial,    // keeps a larger militia
    }

    // What a governor builds toward. The player sets it; the governor decides the rest.
    public enum GovernorFocus
    {
        Balanced,
        Economy,
        Industry,
        Military,
    }

    public enum GovernorOrigin
    {
        Appointee,     // from the court's candidate pool
        Notable,       // a local figure from a reclaimed or courted system
        Commander,     // a retired army commander
    }

    // A governor runs a system for a cut of what it produces (IDEAS.md §1.1, §1.2).
    public sealed class Governor
    {
        public string Name = "";
        public int Administration = 3;   // 1..5: build speed and cost
        public int Industry = 3;         // 1..5: output
        public int Loyalty = 3;          // 1..5: stability; a disloyal governor skims and may defect
        public GovernorTrait Trait;
        public GovernorFocus Focus;
        public GovernorOrigin Origin;
        public string Home = "";         // a notable's home system

        // Share of the system's credits and influence the governor keeps: 3% to 8%.
        public float Cut
        {
            get
            {
                float c = 0.05f + 0.01f * (3 - Loyalty) + (Trait == GovernorTrait.Greedy ? 0.02f : Trait == GovernorTrait.Frugal ? -0.01f : 0f);
                return System.MathF.Max(0.03f, System.MathF.Min(0.08f, c));
            }
        }

        public string TraitText => Trait switch
        {
            GovernorTrait.Frugal => "frugal: takes less",
            GovernorTrait.Builder => "builder: works fast",
            GovernorTrait.Populist => "populist: keeps the peace",
            GovernorTrait.Greedy => "greedy: takes more",
            _ => "martial: a larger militia",
        };
    }

    public sealed partial class Campaign
    {
        public const int AppointCost = 10;        // influence
        public const int PoolSize = 3;
        public const float PoolRefreshDays = 30f;

        public readonly Dictionary<int, List<Governor>> Candidates = new Dictionary<int, List<Governor>>();

        public List<Governor> Pool(int faction)
        {
            if (!Candidates.TryGetValue(faction, out var l)) Candidates[faction] = l = new List<Governor>();
            return l;
        }

        Governor RandomGovernor(GovernorOrigin origin)
        {
            int Stat() => 1 + _rng.Range(0, 3) + _rng.Range(0, 3);  // 1..5, bunched at 3
            return new Governor
            {
                Name = $"{Given[_rng.Range(0, Given.Length)]} {Family[_rng.Range(0, Family.Length)]}",
                Administration = Stat(),
                Industry = Stat(),
                Loyalty = Stat(),
                Trait = (GovernorTrait)_rng.Range(0, 5),
                Origin = origin,
            };
        }

        void GovernorPools()
        {
            int day = Day;
            foreach (var fac in Factions)
            {
                if (fac.Eliminated) continue;
                var pool = Pool(fac.Id);
                // The court turns over: one candidate leaves each month, and the pool refills.
                if (day > 0 && day % (int)PoolRefreshDays == 0 && pool.Count > 0)
                {
                    for (int i = 0; i < pool.Count; i++)
                        if (pool[i].Origin == GovernorOrigin.Appointee) { pool.RemoveAt(i); break; }
                }
                while (pool.Count < PoolSize) pool.Add(RandomGovernor(GovernorOrigin.Appointee));
            }
        }

        public bool CanAppoint(int faction, int system) =>
            Galaxy.Systems[system].Owner == faction && Factions[faction].Influence >= AppointCost;

        public bool Appoint(int faction, int system, int candidate, GovernorFocus focus)
        {
            var pool = Pool(faction);
            if (!CanAppoint(faction, system) || candidate < 0 || candidate >= pool.Count) return false;
            var s = Galaxy.Systems[system];
            if (s.Governor != null) Dismiss(faction, system);
            Factions[faction].Influence -= AppointCost;
            s.Governor = pool[candidate];
            s.Governor.Focus = focus;
            pool.RemoveAt(candidate);
            Emit(CampaignEventKind.GovernorAppointed, system, faction, text: s.Governor.Name);
            return true;
        }

        // Dismissed governors go home, and the system takes it badly for a while.
        public bool Dismiss(int faction, int system)
        {
            var s = Galaxy.Systems[system];
            if (s.Owner != faction || s.Governor == null) return false;
            s.Governor = null;
            s.Stability = System.MathF.Max(0f, s.Stability - 5f);
            return true;
        }

        // A commander hangs up the uniform: the army is disbanded, and its commander governs.
        public bool RetireCommander(int armyId, int system, GovernorFocus focus)
        {
            var a = ArmyById(armyId);
            var s = Galaxy.Systems[system];
            if (a == null || a.Fleet >= 0 || s.Owner != a.Owner || a.System != system) return false;
            var c = a.Commander;
            var g = new Governor
            {
                Name = c.Name,
                Administration = c.Trait == CommanderTrait.Methodical ? 4 : 2 + System.Math.Min(2, c.Victories),
                Industry = 2,
                Loyalty = c.Trait == CommanderTrait.Veteran ? 5 : 4,
                Trait = c.Trait == CommanderTrait.Bold ? GovernorTrait.Greedy : GovernorTrait.Martial,
                Origin = GovernorOrigin.Commander,
                Focus = focus,
            };
            Armies.Remove(a);
            s.Governor = g;
            Emit(CampaignEventKind.GovernorAppointed, system, s.Owner, text: g.Name);
            return true;
        }

        public void SetFocus(int faction, int system, GovernorFocus focus)
        {
            var s = Galaxy.Systems[system];
            if (s.Owner == faction && s.Governor != null) s.Governor.Focus = focus;
        }

        // A reclaimed or courted system sometimes offers one of its own to govern it.
        void OfferNotable(int faction, StarSystem s)
        {
            if (_rng.NextFloat() > 0.35f) return;
            var g = RandomGovernor(GovernorOrigin.Notable);
            g.Loyalty = System.Math.Max(g.Loyalty, 3);
            g.Home = s.Name;
            Pool(faction).Add(g);
            Emit(CampaignEventKind.NotableOffered, s.Id, faction, text: g.Name);
        }

        void GovernorDay(StarSystem s)
        {
            var g = s.Governor;
            if (g.Trait == GovernorTrait.Martial) s.Militia = System.MathF.Min(MaxMilitia(s) * 1.25f, s.Militia + 2f);

            // A disloyal governor of a miserable border system can take it out of the war.
            if (g.Loyalty <= 2 && s.Stability < 20f && !s.Capital && BordersEnemy(s) && _rng.NextFloat() < 0.015f * (3 - g.Loyalty))
            {
                int was = s.Owner;
                s.Owner = -1;
                s.Derelict = Derelict.Holdout;
                s.Besieger = -1;
                Emit(CampaignEventKind.GovernorDefected, s.Id, was, text: g.Name);
                CheckEnd();
                return;
            }

            // Build within the focus, out of the treasury, keeping a reserve.
            int k = Advise(s, g.Focus);
            if (k < 0) return;
            var kind = (BuildingKind)k;
            if (Factions[s.Owner].Credits < CostHere(s, kind) + ReserveForGovernors) return;
            Construct(s.Owner, s.Id, kind);
        }

        bool BordersEnemy(StarSystem s)
        {
            foreach (var l in Galaxy.LanesFrom(s.Id))
            {
                int o = Galaxy.Systems[l.Other(s.Id)].Owner;
                if (o >= 0 && o != s.Owner && Stance(o, s.Owner) == DiplomaticStance.War) return true;
            }
            return false;
        }
    }
}
