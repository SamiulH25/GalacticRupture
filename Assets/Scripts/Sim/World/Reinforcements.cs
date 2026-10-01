using System.Collections.Generic;
using System.Numerics;

namespace GalacticRupture.Sim
{
    // A ship waiting off the field for room in the line.
    public sealed class ReserveShip
    {
        public HullDef Def;
        public string Name = "";
        public int Faction;
        public float Hull;
        public bool[] MountAlive;
        public float ReloadScale = 1f;
        public int Fleet = -1, Index = -1;   // campaign: where it came from
    }

    // Empire at War's population cap: each side has so many command points on the field, and
    // everything beyond waits in reserve until a ship is lost and there is room to bring another
    // in. Stations cost nothing. Requests apply at the next tick, so the battle stays deterministic.
    // A commander calls ships in near the ships already fighting: anywhere within ZoneRadius of
    // one of them (or of the station). The AI's arrive at its edge of the field.
    public sealed class Reinforcements
    {
        public const int DefaultCap = 30;
        public const float ZoneRadius = 2500f;
        public readonly int[] Cap = { DefaultCap, DefaultCap };
        public readonly List<ReserveShip>[] Pool = { new List<ReserveShip>(), new List<ReserveShip>() };
        public readonly float[] EntryZ = { -3400f, 3400f };   // where each side arrives
        public readonly int[] Station = { -1, -1 };          // arrive under your station if you have one

        struct Req
        {
            public int Side, Index;
            public ReserveShip Ship;   // by reference when given, since indices shift as ships leave
            public bool At;
            public Vector3 Point;
        }

        readonly List<Req> _requests = new List<Req>();

        // Called with every ship brought onto the field, and the reserve entry it came from.
        public System.Action<Ship, ReserveShip> Deployed;

        public static int Points(HullClass c) => c switch
        {
            HullClass.Corvette => 1,
            HullClass.Frigate => 2,
            HullClass.Destroyer => 3,
            HullClass.Cruiser => 4,
            HullClass.Battleship => 6,
            HullClass.Transport => 1,
            _ => 0,
        };

        public int Used(World world, int side)
        {
            int n = 0;
            foreach (var s in world.Ships)
                if (s.Alive && s.Side == side) n += Points(s.Def.Class);
            return n;
        }

        public bool Fits(World world, int side, ReserveShip r) => Used(world, side) + Points(r.Def.Class) <= Cap[side];

        public bool HasWarships(int side)
        {
            foreach (var r in Pool[side]) if (r.Def.Class != HullClass.Transport) return true;
            return false;
        }

        public void Request(int side, int index) => _requests.Add(new Req { Side = side, Index = index });

        // Ships a side has on the field that reserves can form up on.
        static bool Anchor(Ship s, int side) => s.Alive && s.Side == side && s.Def.Class != HullClass.Transport;

        public static bool InZone(World world, int side, Vector3 p)
        {
            foreach (var s in world.Ships)
            {
                if (!Anchor(s, side)) continue;
                float dx = s.Position.X - p.X, dz = s.Position.Z - p.Z;
                if (dx * dx + dz * dz <= ZoneRadius * ZoneRadius) return true;
            }
            return false;
        }

        // Call these reserves in at a point, formed up abreast facing the enemy's side of the
        // field, heavy ships in front. Dropped if the point is not in the zone when it applies.
        public void RequestAt(int side, IReadOnlyList<ReserveShip> ships, Vector3 point)
        {
            var list = new List<ReserveShip>(ships);
            list.Sort((a, b) => b.Def.Cost.CompareTo(a.Def.Cost));
            float dir = side == 0 ? 1f : -1f;   // towards the enemy
            const int PerRow = 6;
            const float Gap = 120f;
            float back = 0f;
            for (int start = 0; start < list.Count; start += PerRow)
            {
                int end = System.Math.Min(list.Count, start + PerRow);
                float width = 0f, depth = 0f;
                for (int i = start; i < end; i++)
                {
                    width += list[i].Def.HalfLength * 2f + (i > start ? Gap : 0f);
                    depth = System.MathF.Max(depth, list[i].Def.HalfLength);
                }
                if (back > 0f) back += depth;
                float x = -width * 0.5f;
                for (int i = start; i < end; i++)
                {
                    float half = list[i].Def.HalfLength;
                    x += half;
                    _requests.Add(new Req { Side = side, Ship = list[i], At = true, Point = point + new Vector3(x, 0f, -dir * back) });
                    x += half + Gap;
                }
                back += depth + Gap * 2f;
            }
        }

        // Room permitting, the request is honoured now (at deploy time) or dropped.
        public void Tick(World world, bool autoSide0, bool autoSide1)
        {
            foreach (var q in _requests)
            {
                var pool = Pool[q.Side];
                int index = q.Ship != null ? pool.IndexOf(q.Ship) : q.Index;
                if (index < 0 || index >= pool.Count) continue;
                if (!Fits(world, q.Side, pool[index])) continue;
                if (q.At && !InZone(world, q.Side, q.Point)) continue;
                Deploy(world, q.Side, index, q.At ? q.Point : (Vector3?)null);
            }
            _requests.Clear();
            if (autoSide0) Auto(world, 0);
            if (autoSide1) Auto(world, 1);
        }

        // An AI side brings in the heaviest warship that fits; troop ships come last.
        void Auto(World world, int side)
        {
            var pool = Pool[side];
            while (pool.Count > 0)
            {
                int best = -1;
                int room = Cap[side] - Used(world, side);
                for (int i = 0; i < pool.Count; i++)
                {
                    int pts = Points(pool[i].Def.Class);
                    if (pts > room) continue;
                    if (best < 0 || Rank(pool[i]) > Rank(pool[best])) best = i;
                }
                if (best < 0) return;
                Deploy(world, side, best);
            }
        }

        static float Rank(ReserveShip r) => r.Def.Class == HullClass.Transport ? -1f : r.Def.Cost * r.Hull / r.Def.HullHp;

        // A side with no warships left on the field brings one in regardless of whose side it is,
        // so a battle never ends with ships still waiting.
        public void Force(World world, int side)
        {
            var pool = Pool[side];
            for (int i = 0; i < pool.Count; i++)
                if (pool[i].Def.Class != HullClass.Transport)
                {
                    Deploy(world, side, i);
                    return;
                }
        }

        public Ship Deploy(World world, int side, int index, Vector3? at = null)
        {
            var r = Pool[side][index];
            Pool[side].RemoveAt(index);
            Vector3 p = at ?? ArrivalPoint(world, side, r);
            var ship = world.Spawn(r.Def, side, p, side == 0 ? 0f : SimMath.Pi, arriving: true);
            ship.Faction = r.Faction;
            ship.Hull = r.Hull;
            ship.ReloadScale = r.ReloadScale;
            ship.Speed = ship.TopSpeed * 0.6f;
            if (r.MountAlive != null)
                for (int m = 0; m < ship.Mounts.Count && m < r.MountAlive.Length; m++)
                    ship.Mounts[m].Alive = r.MountAlive[m];
            Deployed?.Invoke(ship, r);
            return ship;
        }

        Vector3 ArrivalPoint(World world, int side, ReserveShip r)
        {
            var station = world.ShipById(Station[side]);
            float dir = side == 0 ? -1f : 1f;
            ref var rng = ref world.Rng;
            if (station != null && station.Alive)
                return station.Position + new Vector3(rng.Range(-900f, 900f), rng.Range(-80f, 80f), -dir * rng.Range(300f, 700f));
            return new Vector3(rng.Range(-1600f, 1600f), rng.Range(-100f, 100f), EntryZ[side] + dir * rng.Range(0f, 400f));
        }
    }
}
