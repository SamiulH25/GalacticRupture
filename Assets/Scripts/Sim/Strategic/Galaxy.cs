using System.Collections.Generic;
using System.Numerics;

namespace GalacticRupture.Sim.Strategic
{
    public sealed class StarSystem
    {
        public int Id;
        public string Name = "";
        public Vector3 Position;       // light-years
        public int Owner = -1;         // faction, -1 unclaimed
        public int Tier = 1;           // 1..3: income, and whether it can build
        public bool Capital;
        public float Siege;            // 0..1: the landed attackers' share of the ground war
        public int Besieger = -1;      // faction with armies landed here, -1 if none
        public float Militia;          // local defence force, rebuilds slowly
        public float Unrest;           // hours of lost production after a change of hands

        public bool Shipyard => Tier >= 2;
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
            var s = new StarSystem { Id = Systems.Count, Name = name, Position = new Vector3(x, y, z), Tier = tier, Owner = owner, Capital = capital };
            Systems.Add(s);
            _adjacent[s.Id] = new List<int>();
            return s;
        }

        public void Connect(int a, int b)
        {
            int index = Lanes.Count;
            Lanes.Add(new Lane(a, b, Vector3.Distance(Systems[a].Position, Systems[b].Position)));
            _adjacent[a].Add(index);
            _adjacent[b].Add(index);
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
