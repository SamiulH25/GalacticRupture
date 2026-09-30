using System.Collections.Generic;
using System.Numerics;

namespace GalacticRupture.Sim.Strategic
{
    // The long campaign's galaxy (IDEAS.md §1.1, §1.2): about a hundred systems joined by lanes,
    // five major powers on the rim, ten minors between them, the old capital at the centre and
    // the ruins of the old empire everywhere else. Everything follows from the seed.
    public static class GalaxyGen
    {
        public sealed class Options
        {
            public int Systems = 100;
            public float RadiusX = 85f;      // light-years
            public float RadiusZ = 60f;
            public float MinSpacing = 8f;
            public float MaxLane = 17f;
            public int Majors = 5;
            public int Minors = 10;
        }

        static readonly (string Name, string Prefix)[] MajorNames =
        {
            ("Continuance", "CNS"),
            ("Remnant", "RSV"),
            ("Hierarchate", "HRS"),
            ("Free Yards", "FYS"),
            ("Lantern Synod", "LSV"),
        };

        static readonly (string Name, string Prefix)[] MinorNames =
        {
            ("Serac Compact", "SCV"),
            ("Ilium Directorate", "IDS"),
            ("Thule Freeholds", "TFS"),
            ("Orrery League", "OLS"),
            ("Kestrel Syndics", "KSV"),
            ("Varne Protectorate", "VPS"),
            ("Halcyon Mandate", "HMS"),
            ("Corva Assembly", "CAS"),
            ("Mireth Concord", "MCS"),
            ("Ashgrove Charter", "ACS"),
        };

        static readonly string[] Onsets = { "Ae", "Al", "Ar", "As", "Bel", "Cor", "Cy", "Dar", "Del", "Ed", "Fal", "Gal", "Hal", "Hes", "Il", "Ith", "Kae", "Kal", "Kes", "Lor", "Lum", "Mar", "Mer", "Nad", "Nor", "Or", "Pel", "Quel", "Ren", "Sab", "Ser", "Sol", "Tal", "Tes", "Thu", "Ul", "Val", "Ven", "Vor", "Wyr", "Yss", "Zar" };
        static readonly string[] Middles = { "", "", "", "a", "e", "i", "o", "an", "en", "ar", "or", "is", "ith", "ev" };
        static readonly string[] Codas = { "a", "on", "is", "ar", "el", "ium", "ora", "une", "yx", "eth", "ane", "ost", "ire", "ade", "orn", "is Deep", "'s Reach", " Gate", " Drift", " Prime" };

        public static Campaign Generate(ulong seed, Options o = null)
        {
            o ??= new Options();
            var c = new Campaign(seed);
            var rng = new Rng(seed ^ 0x6A09E667F3BCC908UL);
            var g = c.Galaxy;

            // Positions: rejection sampling in an ellipse, denser toward the core.
            var pts = new List<Vector3>();
            int tries = 0;
            while (pts.Count < o.Systems && tries++ < 200000)
            {
                float a = rng.Range(0f, 6.2831853f);
                float r = System.MathF.Pow(rng.NextFloat(), 0.62f);
                var p = new Vector3(System.MathF.Cos(a) * r * o.RadiusX, rng.Range(-2.5f, 2.5f), System.MathF.Sin(a) * r * o.RadiusZ);
                float spacing = o.MinSpacing * (0.85f + 0.3f * r);
                bool ok = true;
                foreach (var q in pts)
                    if (Vector3.DistanceSquared(new Vector3(p.X, 0f, p.Z), new Vector3(q.X, 0f, q.Z)) < spacing * spacing) { ok = false; break; }
                if (ok) pts.Add(p);
            }

            // Names.
            var used = new HashSet<string>();
            foreach (var p in pts)
            {
                string name;
                int guard = 0;
                do
                {
                    name = Onsets[rng.Range(0, Onsets.Length)] + Middles[rng.Range(0, Middles.Length)] + Codas[rng.Range(0, Codas.Length)];
                } while (!used.Add(name) && guard++ < 50);
                var s = g.Add(name, p.X, p.Y, p.Z, 1, -1);
                s.Buildings.Clear();
            }

            Lanes(g, o, ref rng);

            // The old capital: the system nearest the centre.
            int throne = 0;
            for (int i = 1; i < g.Systems.Count; i++)
                if (Flat(g.Systems[i].Position).Length() < Flat(g.Systems[throne].Position).Length()) throne = i;

            var taken = new bool[g.Systems.Count];
            taken[throne] = true;

            // Major powers on the rim, spread as far apart as the map allows.
            var capitals = new List<int>();
            int first = -1;
            float bestR = -1f;
            float startAngle = rng.Range(0f, 6.2831853f);
            var dir = new Vector3(System.MathF.Cos(startAngle), 0f, System.MathF.Sin(startAngle));
            for (int i = 0; i < g.Systems.Count; i++)
            {
                var p = Flat(g.Systems[i].Position);
                float score = Vector3.Dot(p, dir) / o.RadiusX;
                if (score > bestR) { bestR = score; first = i; }
            }
            capitals.Add(first);
            while (capitals.Count < o.Majors)
            {
                int best = -1;
                float bestScore = -1f;
                for (int i = 0; i < g.Systems.Count; i++)
                {
                    if (capitals.Contains(i) || i == throne) continue;
                    var p = Flat(g.Systems[i].Position);
                    float rim = p.Length() / o.RadiusX;
                    if (rim < 0.55f) continue;
                    float near = float.MaxValue;
                    foreach (int k in capitals) near = System.MathF.Min(near, g.Distance(i, k));
                    float score = near * (0.6f + rim);
                    if (score > bestScore) { bestScore = score; best = i; }
                }
                capitals.Add(best);
            }

            for (int m = 0; m < o.Majors; m++)
            {
                var (name, prefix) = MajorNames[m % MajorNames.Length];
                var f = c.AddFaction(name, true, prefix, 800f);
                int cap = capitals[m];
                Develop(g.Systems[cap], 3, f.Id, ref rng);
                g.Systems[cap].Capital = true;
                f.Capital = cap;
                taken[cap] = true;
                // The core worlds: the nearest few by lane.
                foreach (int s in Nearest(g, cap, 3, taken))
                {
                    Develop(g.Systems[s], rng.NextFloat() < 0.4f ? 2 : 1, f.Id, ref rng);
                    taken[s] = true;
                }
            }

            // Minor powers in the gaps: as far from everyone as possible.
            for (int m = 0; m < o.Minors; m++)
            {
                int best = -1;
                float bestScore = -1f;
                for (int i = 0; i < g.Systems.Count; i++)
                {
                    if (taken[i]) continue;
                    float near = float.MaxValue;
                    for (int k = 0; k < g.Systems.Count; k++)
                        if (taken[k] && k != throne) near = System.MathF.Min(near, g.Distance(i, k));
                    near = System.MathF.Min(near, g.Distance(i, throne) * 1.5f);
                    if (near > bestScore) { bestScore = near; best = i; }
                }
                if (best < 0) break;
                var (name, prefix) = MinorNames[m % MinorNames.Length];
                var f = c.AddFaction(name, false, prefix, 350f);
                Develop(g.Systems[best], 2, f.Id, ref rng);
                g.Systems[best].Capital = true;
                f.Capital = best;
                taken[best] = true;
                int extra = rng.Range(0, 3);
                foreach (int s in Nearest(g, best, extra, taken))
                {
                    Develop(g.Systems[s], 1, f.Id, ref rng);
                    taken[s] = true;
                }
            }

            // Everything else is the old empire's wreckage.
            for (int i = 0; i < g.Systems.Count; i++)
            {
                if (taken[i]) continue;
                var s = g.Systems[i];
                if (rng.NextFloat() < 0.3f) Holdout(s, ref rng);
                else Ruin(s, ref rng);
            }
            Throne(g.Systems[throne], ref rng);

            c.Begin();

            // Old grudges: the two largest successors start cold, and near neighbours wary.
            c.ChangeTrust(0, 1, -30f);
            for (int a = 0; a < c.Factions.Count; a++)
            for (int b = a + 1; b < c.Factions.Count; b++)
            {
                float d = g.Distance(c.Factions[a].Capital, c.Factions[b].Capital);
                if (d < 35f) c.ChangeTrust(a, b, -8f);
                c.ChangeTrust(a, b, rng.Range(-6f, 6f));
            }

            Forces(c, ref rng);
            return c;
        }

        static Vector3 Flat(Vector3 p) => new Vector3(p.X, 0f, p.Z);

        // Lanes: the relative neighbourhood graph (sparse, readable, with chokepoints) plus a share
        // of the longer Gabriel-graph edges for loops, and bridges wherever a piece is cut off.
        static void Lanes(Galaxy g, Options o, ref Rng rng)
        {
            int n = g.Systems.Count;
            var P = new Vector3[n];
            for (int i = 0; i < n; i++) P[i] = Flat(g.Systems[i].Position);
            for (int i = 0; i < n; i++)
            for (int j = i + 1; j < n; j++)
            {
                float dij = Vector3.DistanceSquared(P[i], P[j]);
                if (dij > o.MaxLane * o.MaxLane) continue;
                bool rngEdge = true, gabriel = true;
                var mid = (P[i] + P[j]) * 0.5f;
                for (int k = 0; k < n && (rngEdge || gabriel); k++)
                {
                    if (k == i || k == j) continue;
                    float dik = Vector3.DistanceSquared(P[i], P[k]), djk = Vector3.DistanceSquared(P[j], P[k]);
                    if (System.MathF.Max(dik, djk) < dij) rngEdge = false;
                    if (Vector3.DistanceSquared(mid, P[k]) < dij * 0.25f) gabriel = false;
                }
                if (rngEdge || (gabriel && rng.NextFloat() < 0.35f)) g.Connect(i, j);
            }

            // Join any separate pieces by their shortest gap.
            while (true)
            {
                var comp = new int[n];
                for (int i = 0; i < n; i++) comp[i] = -1;
                int pieces = 0;
                for (int i = 0; i < n; i++)
                {
                    if (comp[i] >= 0) continue;
                    var stack = new Stack<int>();
                    stack.Push(i);
                    comp[i] = pieces;
                    while (stack.Count > 0)
                    {
                        int u = stack.Pop();
                        foreach (var l in g.LanesFrom(u))
                        {
                            int v = l.Other(u);
                            if (comp[v] >= 0) continue;
                            comp[v] = pieces;
                            stack.Push(v);
                        }
                    }
                    pieces++;
                }
                if (pieces <= 1) break;
                int ba = -1, bb = -1;
                float bd = float.MaxValue;
                for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                {
                    if (comp[i] != 0 || comp[j] == 0) continue;
                    float d = Vector3.DistanceSquared(P[i], P[j]);
                    if (d < bd) { bd = d; ba = i; bb = j; }
                }
                g.Connect(ba, bb);
            }
        }

        static List<int> Nearest(Galaxy g, int from, int count, bool[] taken)
        {
            var order = new List<int>();
            for (int i = 0; i < g.Systems.Count; i++) if (!taken[i] && i != from) order.Add(i);
            order.Sort((a, b) =>
            {
                int c = g.Distance(from, a).CompareTo(g.Distance(from, b));
                return c != 0 ? c : a.CompareTo(b);
            });
            if (order.Count > count) order.RemoveRange(count, order.Count - count);
            return order;
        }

        static readonly BuildingKind[] Kinds = { BuildingKind.Fab, BuildingKind.Yard, BuildingKind.Refinery, BuildingKind.Garrison, BuildingKind.Admin, BuildingKind.Habitat, BuildingKind.Depot };

        static void Develop(StarSystem s, int tier, int owner, ref Rng rng)
        {
            s.Owner = owner;
            s.Derelict = Derelict.None;
            s.Buildings.Clear();
            s.Habitability = tier switch { 3 => 12f, 2 => rng.Range(6f, 9f), _ => rng.Range(3.5f, 6.5f) };
            s.Population = s.Habitability * (tier >= 3 ? 0.9f : rng.Range(0.55f, 0.8f));
            s.Slots = tier switch { 3 => 7, 2 => 5, _ => rng.Range(3, 5) };
            if (tier >= 3)
            {
                s.Buildings.Add(new Building(BuildingKind.Yard));
                s.Buildings.Add(new Building(BuildingKind.Yard));
                s.Buildings.Add(new Building(BuildingKind.Fab));
                s.Buildings.Add(new Building(BuildingKind.Admin));
                s.Buildings.Add(new Building(BuildingKind.Garrison));
            }
            else if (tier == 2)
            {
                s.Buildings.Add(new Building(BuildingKind.Yard));
                s.Buildings.Add(new Building(BuildingKind.Garrison));
            }
            else if (rng.NextFloat() < 0.5f) s.Buildings.Add(new Building(BuildingKind.Refinery));
        }

        static void Ruin(StarSystem s, ref Rng rng)
        {
            s.Owner = -1;
            s.Derelict = Derelict.Ruin;
            s.Population = 0f;
            s.Habitability = rng.Range(3f, 10f);
            s.Slots = rng.Range(2, 6);
            int dormant = System.Math.Min(s.Slots, rng.Range(1, 4));
            for (int i = 0; i < dormant; i++)
                s.Buildings.Add(new Building(Kinds[rng.Range(0, Kinds.Length)]) { Dormant = true });
        }

        static void Holdout(StarSystem s, ref Rng rng)
        {
            s.Owner = -1;
            s.Derelict = Derelict.Holdout;
            s.Habitability = rng.Range(4f, 9f);
            s.Population = s.Habitability * rng.Range(0.4f, 0.7f);
            s.Slots = rng.Range(3, 6);
            s.Stability = 50f;
            if (rng.NextFloat() < 0.6f) s.Buildings.Add(new Building(rng.NextFloat() < 0.5f ? BuildingKind.Garrison : BuildingKind.Refinery));
        }

        // The seat of the old empire: rich, walled, and taken only by force.
        static void Throne(StarSystem s, ref Rng rng)
        {
            s.Owner = -1;
            s.Derelict = Derelict.Holdout;
            s.OldCapital = true;
            s.Habitability = 16f;
            s.Population = 11f;
            s.Slots = 8;
            s.Stability = 60f;
            s.Buildings.Clear();
            s.Buildings.Add(new Building(BuildingKind.Yard));
            s.Buildings.Add(new Building(BuildingKind.Yard));
            s.Buildings.Add(new Building(BuildingKind.Fab));
            s.Buildings.Add(new Building(BuildingKind.Admin));
            s.Buildings.Add(new Building(BuildingKind.Garrison));
            s.Buildings.Add(new Building(BuildingKind.Refinery));
        }

        static void Forces(Campaign c, ref Rng rng)
        {
            foreach (var f in c.Factions)
            {
                int home = f.Capital;
                if (f.Major)
                {
                    var fleet = c.NewFleet(f.Id, home, "Home Fleet");
                    fleet.Ships.Add(c.NewShip(f.Id, Hulls.Destroyer));
                    fleet.Ships.Add(c.NewShip(f.Id, Hulls.Destroyer));
                    for (int i = 0; i < 3; i++) fleet.Ships.Add(c.NewShip(f.Id, Hulls.Frigate));
                    fleet.Ships.Add(c.NewShip(f.Id, Hulls.Transport));
                    var picket = c.NewFleet(f.Id, home, "First Expedition");
                    picket.Expedition = true;
                    picket.Ships.Add(c.NewShip(f.Id, Hulls.Frigate));
                    picket.Ships.Add(c.NewShip(f.Id, Hulls.Corvette));
                    var second = c.NewFleet(f.Id, home, "Second Expedition");
                    second.Expedition = true;
                    second.Ships.Add(c.NewShip(f.Id, Hulls.Corvette));
                    second.Ships.Add(c.NewShip(f.Id, Hulls.Corvette));
                    c.NewArmy(f.Id, home);
                    c.NewArmy(f.Id, home);
                }
                else
                {
                    var guard = c.NewFleet(f.Id, home, $"{f.Name} Guard");
                    guard.Ships.Add(c.NewShip(f.Id, Hulls.Destroyer));
                    for (int i = 0; i < 2; i++) guard.Ships.Add(c.NewShip(f.Id, Hulls.Corvette));
                    c.NewArmy(f.Id, home);
                }
            }
        }
    }
}
