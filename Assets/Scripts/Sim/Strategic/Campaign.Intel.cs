using System.Collections.Generic;
using System.Numerics;

namespace GalacticRupture.Sim.Strategic
{
    public sealed class FleetSighting
    {
        public int Fleet;
        public int Owner;
        public int System;
        public int Next;
        public float Progress;
        public float Strength;
        public int Ships;
        public int Troops;
        public double Hour;
    }

    // What one faction knows, and when it learned it (IDEAS.md §5, intel). Allies share.
    public sealed class Intel
    {
        public bool[] SystemVisible;
        public int[] OwnerSeen;
        public double[] SeenHour;
        public float[] GarrisonSeen;
        public readonly Dictionary<int, FleetSighting> Fleets = new Dictionary<int, FleetSighting>();
        public readonly HashSet<int> VisibleFleets = new HashSet<int>();

        public float HostileStrengthAt(Campaign c, int faction, int system)
        {
            float s = 0f;
            foreach (var kv in Fleets)
            {
                var f = kv.Value;
                if (f.System == system && f.Next < 0 && f.Owner != faction && c.Stance(faction, f.Owner) == DiplomaticStance.War)
                    s += f.Strength;
            }
            return s;
        }
    }

    public sealed partial class Campaign
    {
        public const float SystemSensorRange = 7f;
        public const float FleetSensorRange = 5f;

        public readonly List<Intel> Intel = new List<Intel>();

        void InitIntel()
        {
            int n = Galaxy.Systems.Count;
            foreach (var _ in Factions)
            {
                var i = new Intel
                {
                    SystemVisible = new bool[n],
                    OwnerSeen = new int[n],
                    SeenHour = new double[n],
                    GarrisonSeen = new float[n],
                };
                for (int s = 0; s < n; s++)
                {
                    i.OwnerSeen[s] = Galaxy.Systems[s].Owner;
                    i.SeenHour[s] = -1;
                }
                Intel.Add(i);
            }
        }

        bool SharesIntel(int a, int b) => a == b || Stance(a, b) == DiplomaticStance.Alliance;

        public float Garrison(int system)
        {
            var s = Galaxy.Systems[system];
            float g = s.Militia;
            foreach (var a in Armies)
                if (a.System == system && a.Fleet < 0 && a.Owner == s.Owner) g += a.Strength;
            return g;
        }

        void UpdateIntel()
        {
            var sources = new List<(Vector3 Pos, float Range)>();
            for (int f = 0; f < Factions.Count; f++)
            {
                var intel = Intel[f];
                sources.Clear();
                foreach (var s in Galaxy.Systems)
                    if (s.Owner >= 0 && SharesIntel(f, s.Owner)) sources.Add((s.Position, SystemSensorRange));
                foreach (var fl in Fleets)
                    if (fl.Alive && SharesIntel(f, fl.Owner)) sources.Add((FleetPosition(fl), FleetSensorRange));

                foreach (var s in Galaxy.Systems)
                {
                    bool seen = s.Owner >= 0 && SharesIntel(f, s.Owner);
                    for (int k = 0; k < sources.Count && !seen; k++)
                        seen = Vector3.DistanceSquared(sources[k].Pos, s.Position) <= sources[k].Range * sources[k].Range;
                    intel.SystemVisible[s.Id] = seen;
                    if (!seen) continue;
                    intel.OwnerSeen[s.Id] = s.Owner;
                    intel.SeenHour[s.Id] = Hours;
                    intel.GarrisonSeen[s.Id] = Garrison(s.Id);
                }

                intel.VisibleFleets.Clear();
                foreach (var fl in Fleets)
                {
                    if (!fl.Alive) continue;
                    Vector3 p = FleetPosition(fl);
                    bool seen = SharesIntel(f, fl.Owner);
                    for (int k = 0; k < sources.Count && !seen; k++)
                        seen = Vector3.DistanceSquared(sources[k].Pos, p) <= sources[k].Range * sources[k].Range;
                    if (!seen) continue;
                    intel.VisibleFleets.Add(fl.Id);
                    if (!intel.Fleets.TryGetValue(fl.Id, out var sight)) intel.Fleets[fl.Id] = sight = new FleetSighting { Fleet = fl.Id };
                    sight.Owner = fl.Owner;
                    sight.System = fl.System;
                    sight.Next = fl.Next;
                    sight.Progress = fl.Progress;
                    sight.Strength = fl.Strength;
                    sight.Ships = fl.Ships.Count;
                    sight.Troops = CountIn(fl);
                    sight.Hour = Hours;
                }

                // Forget sightings that are now disproved: the place is watched and the fleet is not there,
                // or the fleet no longer exists and we saw it go.
                var stale = new List<int>();
                foreach (var kv in intel.Fleets)
                {
                    if (intel.VisibleFleets.Contains(kv.Key)) continue;
                    var sight = kv.Value;
                    bool placeWatched = sight.Next < 0 && intel.SystemVisible[sight.System];
                    if (placeWatched || FleetById(kv.Key) == null && Hours - sight.Hour > 24 * 10) stale.Add(kv.Key);
                }
                foreach (int id in stale) intel.Fleets.Remove(id);
            }
        }
    }
}
