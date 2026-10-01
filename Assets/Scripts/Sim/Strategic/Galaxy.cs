using System.Collections.Generic;
using System.Numerics;

namespace GalacticRupture.Sim.Strategic
{
    // What a derelict old-empire system is (IDEAS.md §1.1: expansion is reclamation).
    public enum Derelict
    {
        None,
        Ruin,       // empty: dead fabs and drifting stations, reclaimed by an expedition
        Holdout,    // cut off but alive: taken by landing, or brought in with influence
    }

    public sealed class StarSystem
    {
        public int Id;
        public string Name = "";
        public Vector3 Position;       // light-years
        public int Owner = -1;         // faction, -1 unclaimed
        public bool Capital;
        public bool OldCapital;        // the seat of the old empire: the prize of act 3
        public float Siege;            // 0..1: the landed attackers' share of the ground war
        public int Besieger = -1;      // faction with armies landed here, -1 if none
        public float Militia;          // local defence force, rebuilds slowly
        public float Unrest;           // hours of lost production after a change of hands

        // The economy (IDEAS.md §1.1: specialisation, not pops).
        public float Population;       // grows toward Habitability plus habitats
        public float Habitability = 6f;
        public float Stability = 60f;  // 0..100
        public int Slots = 3;          // building slots
        public readonly List<Building> Buildings = new List<Building>();
        public Derelict Derelict;
        public Governor Governor;
        public float Reclaim;          // 0..1: an expedition's progress
        public int Reclaimer = -1;     // faction whose expedition is working here
        public float Courting;         // 0..1: influence spent bringing a holdout in
        public int Courter = -1;

        // The orbital defence station: fights in every battle over the system (IDEAS.md §4.2).
        public int Station;            // level 0 (none) to 3
        public float StationHull;      // 0..1 of the current level's hull
        public float StationHours;     // > 0 while the next level is being built

        // The battlefield: fixed for the system, derived from its name, so it never needs saving.
        public TerrainSpec Terrain
        {
            get
            {
                uint h = 2166136261u;
                foreach (char ch in Name) h = (h ^ ch) * 16777619u;
                h ^= (uint)Id * 2654435761u;
                h ^= h >> 15;
                h *= 2246822519u;
                h ^= h >> 13;
                float a = (h & 0xffff) / 65536f;
                float n = ((h >> 16) & 0xff) / 256f;
                // Ruins lie in their own wreckage.
                if (Derelict != Derelict.None) a = System.MathF.Min(0.999f, a + 0.2f);
                return new TerrainSpec
                {
                    Asteroids = a < 0.35f ? 0 : a < 0.65f ? 1 : a < 0.88f ? 2 : 3,
                    Nebula = n < 0.28f,
                };
            }
        }

        public int Count(BuildingKind k)
        {
            int n = 0;
            foreach (var b in Buildings) if (b.Kind == k && b.Online) n++;
            return n;
        }

        public bool Has(BuildingKind k) => Count(k) > 0;
        public bool Shipyard => Has(BuildingKind.Yard);
        public int FreeSlots => Slots - Buildings.Count;
        public float MaxPopulation => Habitability + 3f * Count(BuildingKind.Habitat);

        // A rough size, for the map and the AI's sense of value: 1 small, 2 developed, 3 a core world.
        public int Tier => Population >= 9f || Buildings.Count >= 5 ? 3 : Population >= 4.5f || Buildings.Count >= 3 ? 2 : 1;
    }

    public readonly struct Lane
    {
        public readonly int A, B;
        public readonly float Length;

        public Lane(int a, int b, float length)
        {
            A = a;
            B = b;
            Length = length;
        }

        public int Other(int s) => s == A ? B : A;
    }

    // The map: systems joined by FTL lanes. Travel only runs along lanes.
    public sealed class Galaxy
    {
        public readonly List<StarSystem> Systems = new List<StarSystem>();
        public readonly List<Lane> Lanes = new List<Lane>();
        readonly Dictionary<int, List<int>> _adjacent = new Dictionary<int, List<int>>();

        public StarSystem Add(string name, float x, float y, float z, int tier, int owner, bool capital = false)
        {
            var s = new StarSystem { Id = Systems.Count, Name = name, Position = new Vector3(x, y, z), Owner = owner, Capital = capital };
            // A developed world of the given size: population, and the buildings that go with it.
            s.Habitability = tier switch { 3 => 12f, 2 => 7f, _ => 4f };
            s.Population = s.Habitability * (tier >= 3 ? 0.9f : 0.75f);
            s.Slots = tier switch { 3 => 6, 2 => 4, _ => 3 };
            if (tier >= 2) s.Buildings.Add(new Building(BuildingKind.Yard));
            if (tier >= 3)
            {
                s.Buildings.Add(new Building(BuildingKind.Yard));
                s.Buildings.Add(new Building(BuildingKind.Fab));
                s.Buildings.Add(new Building(BuildingKind.Admin));
            }
            if (tier >= 2) s.Buildings.Add(new Building(BuildingKind.Garrison));
            Systems.Add(s);
            _adjacent[s.Id] = new List<int>();
            _dist = null;
            return s;
        }

        public void Connect(int a, int b)
        {
            int index = Lanes.Count;
            Lanes.Add(new Lane(a, b, Vector3.Distance(Systems[a].Position, Systems[b].Position)));
            _adjacent[a].Add(index);
            _adjacent[b].Add(index);
            _dist = null;
        }

        public IEnumerable<Lane> LanesFrom(int system)
        {
            foreach (int i in _adjacent[system]) yield return Lanes[i];
        }

        public float LaneLength(int a, int b)
        {
            foreach (var l in LanesFrom(a))
                if (l.Other(a) == b) return l.Length;
            return -1f;
        }

        float[] _dist;   // all-pairs shortest lane distance, rebuilt when the map changes
        int[] _hops;

        void BuildDistances()
        {
            int n = Systems.Count;
            _dist = new float[n * n];
            _hops = new int[n * n];
            for (int i = 0; i < n * n; i++)
            {
                _dist[i] = float.MaxValue;
                _hops[i] = int.MaxValue;
            }
            for (int i = 0; i < n; i++)
            {
                _dist[i * n + i] = 0f;
                _hops[i * n + i] = 0;
            }
            foreach (var l in Lanes)
            {
                _dist[l.A * n + l.B] = _dist[l.B * n + l.A] = l.Length;
                _hops[l.A * n + l.B] = _hops[l.B * n + l.A] = 1;
            }
            for (int k = 0; k < n; k++)
            for (int i = 0; i < n; i++)
            {
                float dik = _dist[i * n + k];
                int hik = _hops[i * n + k];
                if (dik == float.MaxValue && hik == int.MaxValue) continue;
                for (int j = 0; j < n; j++)
                {
                    float d = dik + _dist[k * n + j];
                    if (d < _dist[i * n + j]) _dist[i * n + j] = d;
                    if (hik != int.MaxValue && _hops[k * n + j] != int.MaxValue && hik + _hops[k * n + j] < _hops[i * n + j])
                        _hops[i * n + j] = hik + _hops[k * n + j];
                }
            }
        }

        // Lane distance in light-years along the shortest route; MaxValue if unreachable.
        public float Distance(int a, int b)
        {
            if (_dist == null) BuildDistances();
            return _dist[a * Systems.Count + b];
        }

        // Jumps on the fewest-jump route; int.MaxValue if unreachable.
        public int Hops(int a, int b)
        {
            if (_dist == null) BuildDistances();
            return _hops[a * Systems.Count + b];
        }

        // Shortest route by lane length (Dijkstra; ties broken by system id, so deterministic).
        public List<int> Route(int from, int to)
        {
            int n = Systems.Count;
            var dist = new float[n];
            var prev = new int[n];
            var done = new bool[n];
            for (int i = 0; i < n; i++)
            {
                dist[i] = float.MaxValue;
                prev[i] = -1;
            }
            dist[from] = 0f;
            for (int step = 0; step < n; step++)
            {
                int u = -1;
                for (int i = 0; i < n; i++)
                    if (!done[i] && (u < 0 || dist[i] < dist[u])) u = i;
                if (u < 0 || dist[u] == float.MaxValue) break;
                done[u] = true;
                if (u == to) break;
                foreach (var l in LanesFrom(u))
                {
                    int v = l.Other(u);
                    float d = dist[u] + l.Length;
                    if (d < dist[v])
                    {
                        dist[v] = d;
                        prev[v] = u;
                    }
                }
            }
            var route = new List<int>();
            if (from != to && prev[to] < 0) return route;
            for (int v = to; v != from; v = prev[v]) route.Add(v);
            route.Reverse();
            return route;
        }
    }
}
