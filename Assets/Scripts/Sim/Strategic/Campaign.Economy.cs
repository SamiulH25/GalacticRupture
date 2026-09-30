using System.Collections.Generic;

namespace GalacticRupture.Sim.Strategic
{
    // What a building slot can hold. Each has exactly one job (IDEAS.md §1.1, §2.4).
    public enum BuildingKind
    {
        Fab,        // industry: how fast the yards and builders here work
        Yard,       // builds hulls; a second yard builds two at once
        Refinery,   // credits
        Garrison,   // militia and stability; raises armies
        Admin,      // influence and stability
        Habitat,    // room for more people
        Depot,      // stretches supply range from this system
    }

    public sealed class Building
    {
        public BuildingKind Kind;
        public float HoursLeft;        // > 0 while under construction
        public bool Dormant;           // a dead old-empire building, waiting for reclamation

        public Building(BuildingKind kind) => Kind = kind;

        public bool Online => HoursLeft <= 0f && !Dormant;
    }

    public struct SystemOutput
    {
        public float Credits, Industry, Influence;   // per day, after the governor's cut
        public float Cut;                            // credits the governor keeps, per day
    }

    public sealed partial class Campaign
    {
        public const float ReserveForGovernors = 150f;   // governors leave this much in the treasury
        public const float ShipUpkeep = 0.015f;          // of a hull's cost, per day
        public const float ArmyUpkeep = 1f;              // per army, per day

        public static int BuildingCost(BuildingKind k) => k switch
        {
            BuildingKind.Yard => 260,
            BuildingKind.Admin => 180,
            BuildingKind.Habitat => 160,
            BuildingKind.Fab => 150,
            BuildingKind.Refinery => 150,
            BuildingKind.Depot => 140,
            _ => 120,
        };

        public static float BuildingDays(BuildingKind k) => k switch
        {
            BuildingKind.Yard => 12f,
            BuildingKind.Admin => 8f,
            BuildingKind.Habitat => 8f,
            BuildingKind.Garrison => 5f,
            _ => 6f,
        };

        public static string Describe(BuildingKind k) => k switch
        {
            BuildingKind.Fab => "+8 industry: yards and builders here work faster",
            BuildingKind.Yard => "builds hulls; a second yard builds two at once",
            BuildingKind.Refinery => "+20 credits a day",
            BuildingKind.Garrison => "+40 militia, +5 stability, raises armies",
            BuildingKind.Admin => "+1 influence a day, +10 stability",
            BuildingKind.Habitat => "+3 population ceiling",
            BuildingKind.Depot => "+8 ly supply range from here",
            _ => "",
        };

        // --- Output ------------------------------------------------------------------------------

        public SystemOutput Output(StarSystem s)
        {
            var o = new SystemOutput();
            if (s.Owner < 0) return o;
            o.Influence = 0.2f + s.Count(BuildingKind.Admin) + (s.Capital ? 2f : 0f);
            if (s.Unrest <= 0f)
            {
                float stab = s.Stability >= 50f ? 1f : 0.4f + 0.6f * s.Stability / 50f;
                float gov = s.Governor != null ? 1f + 0.05f * (s.Governor.Industry - 3) : 1f;
                o.Credits = (3f * s.Population + 20f * s.Count(BuildingKind.Refinery)) * stab * gov;
                o.Industry = (0.5f * s.Population + 8f * s.Count(BuildingKind.Fab)) * stab * gov;
                if (Factions[s.Owner].EmbargoHours > 0f) o.Credits *= 0.7f;
            }
            if (s.Governor != null)
            {
                float cut = s.Governor.Cut;
                o.Cut = o.Credits * cut;
                o.Credits -= o.Cut;
                o.Influence *= 1f - cut;
            }
            return o;
        }

        // How fast the yard and the builders here work: 1 is standard.
        public float YardRate(StarSystem s) => System.MathF.Min(3f, System.MathF.Max(0.5f, 0.5f + Output(s).Industry / 12f));

        public float BuilderRate(StarSystem s)
        {
            float r = System.MathF.Min(2.5f, System.MathF.Max(0.6f, 0.6f + Output(s).Industry / 15f));
            if (s.Governor != null) r *= 1f + 0.08f * (s.Governor.Administration - 3) + (s.Governor.Trait == GovernorTrait.Builder ? 0.2f : 0f);
            return r;
        }

        public float Upkeep(int faction)
        {
            float u = 0f;
            foreach (var f in Fleets)
                if (f.Alive && f.Owner == faction)
                    foreach (var r in f.Ships) u += r.Def.Cost * ShipUpkeep;
            foreach (var a in Armies) if (a.Owner == faction) u += ArmyUpkeep;
            return u;
        }

        public SystemOutput Totals(int faction)
        {
            var t = new SystemOutput();
            foreach (var s in Galaxy.Systems)
            {
                if (s.Owner != faction) continue;
                var o = Output(s);
                t.Credits += o.Credits;
                t.Industry += o.Industry;
                t.Influence += o.Influence;
                t.Cut += o.Cut;
            }
            return t;
        }

        public float StabilityTarget(StarSystem s)
        {
            if (s.Owner < 0) return s.Stability;
            float t = 55f + 10f * s.Count(BuildingKind.Admin) + 5f * s.Count(BuildingKind.Garrison) + (s.Capital ? 15f : 0f);
            if (s.Governor != null)
                t += 4f * (s.Governor.Loyalty - 3) + (s.Governor.Trait == GovernorTrait.Populist ? 10f : 0f);
            if (s.Unrest > 0f) t -= 30f;
            if (s.Besieger >= 0) t -= 20f;
            if (Factions[s.Owner].EmbargoHours > 0f) t -= 10f;
            // Distance from the seat of power wears on loyalty; admin buildings are the answer.
            int capital = Factions[s.Owner].Capital;
            if (capital >= 0 && capital != s.Id && Galaxy.Systems[capital].Owner == s.Owner)
            {
                int hops = Galaxy.Hops(capital, s.Id);
                t -= hops == int.MaxValue ? 20f : System.MathF.Min(20f, 2.5f * hops);
            }
            else if (capital != s.Id) t -= 15f;
            return System.MathF.Max(0f, System.MathF.Min(100f, t));
        }

        public static float MaxMilitia(StarSystem s)
        {
            if (s.Derelict == Derelict.Ruin && s.Owner < 0) return 0f;
            if (s.Owner < 0) return 20f * s.Population + 30f + (s.OldCapital ? 250f : 0f);
            return 6f * s.Population + 40f * s.Count(BuildingKind.Garrison) + (s.Capital ? 60f : 0f) + (s.OldCapital ? 80f : 0f);
        }

        public float SupplyRangeOf(StarSystem s) => Factions[s.Owner].SupplyRange + 8f * s.Count(BuildingKind.Depot);

        // --- Hourly and daily ---------------------------------------------------------------------

        void Economy()
        {
            foreach (var fac in Factions) if (fac.EmbargoHours > 0f) fac.EmbargoHours -= 1f;
            foreach (var s in Galaxy.Systems)
            {
                if (s.Owner < 0) continue;
                var fac = Factions[s.Owner];
                var o = Output(s);
                fac.Influence += o.Influence / 24f;
                fac.Credits += o.Credits / 24f;
                if (s.Unrest > 0f) s.Unrest -= 1f;

                // Construction.
                float rate = BuilderRate(s);
                foreach (var b in s.Buildings)
                {
                    if (b.HoursLeft <= 0f || b.Dormant) continue;
                    b.HoursLeft -= rate;
                    if (b.HoursLeft <= 0f) Emit(CampaignEventKind.BuildingCompleted, s.Id, s.Owner, text: b.Kind.ToString());
                    break;  // one building at a time
                }
            }
            foreach (var fac in Factions)
                if (!fac.Eliminated) fac.Credits -= Upkeep(fac.Id) / 24f;

            if ((int)Hours % 24 == 0) Daily();
        }

        void Daily()
        {
            foreach (var s in Galaxy.Systems)
            {
                // People: growth toward the ceiling while the place is calm; decline above it.
                float max = s.MaxPopulation;
                if (s.Owner >= 0 || s.Derelict == Derelict.Holdout)
                {
                    if (s.Population > max) s.Population = System.MathF.Max(max, s.Population - 0.1f);
                    else if (s.Stability >= 30f && s.Population > 0f)
                        s.Population = System.MathF.Min(max, s.Population + 0.006f * s.Population * (1f - s.Population / max) + 0.005f);
                }
                if (s.Owner < 0) continue;

                float target = StabilityTarget(s);
                s.Stability += System.MathF.Max(-2f, System.MathF.Min(2f, target - s.Stability));

                if (s.Governor != null) GovernorDay(s);
            }
            GovernorPools();
        }

        // --- Player (and AI) orders ---------------------------------------------------------------

        public bool Constructing(StarSystem s)
        {
            foreach (var b in s.Buildings) if (b.HoursLeft > 0f && !b.Dormant) return true;
            return false;
        }

        public int CostHere(StarSystem s, BuildingKind k)
        {
            float c = BuildingCost(k);
            if (s.Governor != null) c *= 1f - 0.05f * (s.Governor.Administration - 3);
            return (int)c;
        }

        public bool CanConstruct(int owner, int system, BuildingKind k)
        {
            var s = Galaxy.Systems[system];
            if (s.Owner != owner || s.FreeSlots <= 0 || s.Unrest > 0f) return false;
            if (k == BuildingKind.Yard && s.Count(BuildingKind.Yard) + UnderConstruction(s, BuildingKind.Yard) >= 2) return false;
            if (k == BuildingKind.Yard && s.Population < 3f) return false;
            return Factions[owner].Credits >= CostHere(s, k);
        }

        static int UnderConstruction(StarSystem s, BuildingKind k)
        {
            int n = 0;
            foreach (var b in s.Buildings) if (b.Kind == k && b.HoursLeft > 0f) n++;
            return n;
        }

        // Buildings queue in the slots; one is worked on at a time.
        public bool Construct(int owner, int system, BuildingKind k)
        {
            if (!CanConstruct(owner, system, k)) return false;
            var s = Galaxy.Systems[system];
            Factions[owner].Credits -= CostHere(s, k);
            s.Buildings.Add(new Building(k) { HoursLeft = BuildingDays(k) * 24f });
            return true;
        }

        // Pulls a building down to free its slot; nothing is refunded.
        public bool Demolish(int owner, int system, int index)
        {
            var s = Galaxy.Systems[system];
            if (s.Owner != owner || index < 0 || index >= s.Buildings.Count) return false;
            s.Buildings.RemoveAt(index);
            return true;
        }

        // What a governor, or an AI power, builds next in a system for a given focus. -1: nothing.
        public int Advise(StarSystem s, GovernorFocus focus)
        {
            if (s.FreeSlots <= 0 || Constructing(s)) return -1;
            bool crowded = s.Population >= s.MaxPopulation * 0.85f;
            if (s.Stability < 40f && s.Count(BuildingKind.Admin) < 2) return (int)BuildingKind.Admin;
            if (crowded && s.FreeSlots > 1 && s.Count(BuildingKind.Habitat) < 2) return (int)BuildingKind.Habitat;
            bool yardOk = s.Population >= 3f && s.Count(BuildingKind.Yard) < 2;
            BuildingKind[] order = focus switch
            {
                GovernorFocus.Economy => new[] { BuildingKind.Refinery, BuildingKind.Habitat, BuildingKind.Refinery, BuildingKind.Admin },
                GovernorFocus.Industry => new[] { BuildingKind.Fab, BuildingKind.Yard, BuildingKind.Fab, BuildingKind.Refinery },
                GovernorFocus.Military => new[] { BuildingKind.Yard, BuildingKind.Garrison, BuildingKind.Fab, BuildingKind.Depot, BuildingKind.Yard },
                _ => new[] { BuildingKind.Refinery, BuildingKind.Garrison, BuildingKind.Fab, BuildingKind.Yard, BuildingKind.Admin, BuildingKind.Habitat },
            };
            foreach (var k in order)
            {
                if (k == BuildingKind.Yard && !yardOk) continue;
                if ((k == BuildingKind.Garrison || k == BuildingKind.Depot) && s.Has(k)) continue;
                if (k == BuildingKind.Admin && s.Count(BuildingKind.Admin) >= 2) continue;
                if (k == BuildingKind.Habitat && s.Count(BuildingKind.Habitat) >= 2) continue;
                // A repeated entry means "a second one of these".
                int wanted = 0;
                foreach (var o in order) if (o == k) wanted++;
                if (s.Count(k) + UnderConstruction(s, k) >= wanted) continue;
                return (int)k;
            }
            return -1;
        }
    }
}
