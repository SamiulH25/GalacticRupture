using System.Collections.Generic;
using System.Numerics;

namespace GalacticRupture.Sim
{
    public enum ObstacleKind
    {
        Asteroid,   // solid rock: ships steer round it, rounds stop on it, it hides what is behind it
        Nebula,     // charged gas: shields fail inside, and a ship inside can only be seen up close
    }

    public struct Obstacle
    {
        public ObstacleKind Kind;
        public Vector3 Position;
        public float Radius;
        public int Seed;            // for the view: which rock, how it tumbles
    }

    // The terrain of a battle (IDEAS.md §4: the depth inside the node). Asteroids are cover and
    // walls; a nebula strips shields and hides ships.
    public sealed partial class World
    {
        public readonly List<Obstacle> Obstacles = new List<Obstacle>();

        public const float NebulaSightRange = 1200f;   // a ship in a nebula is seen only this close
        public const float NebulaShieldDrain = 0.35f;  // share of shield capacity lost per second inside

        public void AddObstacle(ObstacleKind kind, Vector3 position, float radius, int seed = 0) =>
            Obstacles.Add(new Obstacle { Kind = kind, Position = position, Radius = radius, Seed = seed });

        public bool InNebula(Vector3 p)
        {
            foreach (var o in Obstacles)
                if (o.Kind == ObstacleKind.Nebula && Vector3.DistanceSquared(o.Position, p) < o.Radius * o.Radius) return true;
            return false;
        }

        // Is the straight line from a to b blocked by rock? `shrink` lets lines that only graze
        // the edge of a rock through.
        public bool Occluded(Vector3 a, Vector3 b, float shrink = 0.85f)
        {
            foreach (var o in Obstacles)
            {
                if (o.Kind != ObstacleKind.Asteroid) continue;
                float r = o.Radius * shrink;
                if (SegmentPointDistSq(a, b, o.Position) < r * r) return true;
            }
            return false;
        }

        // Rock doesn't move: bucket it once into a coarse grid, so a round only tests the rocks
        // near it. Rebuilt if obstacles are added.
        const float RockCell = 600f;
        readonly Dictionary<long, List<int>> _rockCells = new Dictionary<long, List<int>>();
        int _rockCellsFor = -1;

        static long RockKey(int x, int z) => ((long)x << 32) ^ (uint)z;

        void BuildRockGrid()
        {
            _rockCells.Clear();
            _rockCellsFor = Obstacles.Count;
            for (int i = 0; i < Obstacles.Count; i++)
            {
                var o = Obstacles[i];
                if (o.Kind != ObstacleKind.Asteroid) continue;
                int x0 = (int)System.MathF.Floor((o.Position.X - o.Radius) / RockCell), x1 = (int)System.MathF.Floor((o.Position.X + o.Radius) / RockCell);
                int z0 = (int)System.MathF.Floor((o.Position.Z - o.Radius) / RockCell), z1 = (int)System.MathF.Floor((o.Position.Z + o.Radius) / RockCell);
                for (int x = x0; x <= x1; x++)
                for (int z = z0; z <= z1; z++)
                {
                    long k = RockKey(x, z);
                    if (!_rockCells.TryGetValue(k, out var list)) _rockCells[k] = list = new List<int>();
                    list.Add(i);
                }
            }
        }

        // The first rock a segment enters, and where. For rounds, whose steps are shorter than a cell.
        bool HitsRock(Vector3 a, Vector3 b, out Vector3 point)
        {
            point = default;
            if (_rockCellsFor != Obstacles.Count) BuildRockGrid();
            if (_rockCells.Count == 0) return false;
            if (_rockCells.TryGetValue(RockKey((int)System.MathF.Floor(b.X / RockCell), (int)System.MathF.Floor(b.Z / RockCell)), out var cell)
                && HitsRockIn(cell, a, b, out point)) return true;
            if (_rockCells.TryGetValue(RockKey((int)System.MathF.Floor(a.X / RockCell), (int)System.MathF.Floor(a.Z / RockCell)), out cell)
                && HitsRockIn(cell, a, b, out point)) return true;
            return false;
        }

        bool HitsRockIn(List<int> cell, Vector3 a, Vector3 b, out Vector3 point)
        {
            point = default;
            foreach (int i in cell)
            {
                var o = Obstacles[i];
                if (SegmentPointDistSq(a, b, o.Position) >= o.Radius * o.Radius) continue;
                Vector3 d = b - a;
                float len = d.Length();
                if (len < 1e-4f) { point = a; return true; }
                d /= len;
                Vector3 m = a - o.Position;
                float bq = Vector3.Dot(m, d);
                float c = Vector3.Dot(m, m) - o.Radius * o.Radius;
                float disc = bq * bq - c;
                float t = disc > 0f ? -bq - SimMath.Sqrt(disc) : 0f;
                point = a + d * SimMath.Clamp(t, 0f, len);
                return true;
            }
            return false;
        }

        static float SegmentPointDistSq(Vector3 a, Vector3 b, Vector3 p)
        {
            Vector3 ab = b - a;
            float len2 = ab.LengthSquared();
            float t = len2 > 1e-6f ? SimMath.Clamp(Vector3.Dot(p - a, ab) / len2, 0f, 1f) : 0f;
            return Vector3.DistanceSquared(a + ab * t, p);
        }

        // Steering round rock: if the way to `goal` runs through an asteroid, aim for a point
        // beside it instead, on whichever side is nearer the goal. Flat (XZ) like the helm.
        Vector3 AvoidRocks(Ship ship, Vector3 goal)
        {
            if (Obstacles.Count == 0) return goal;
            Vector3 pos = ship.Position;
            Vector3 to = Flat(goal - pos);
            float dist = to.Length();
            if (dist < 1f) return goal;
            Vector3 dir = to / dist;
            float clearance = ship.Def.Radius + ship.Def.HalfLength * 0.5f + 60f;

            int block = -1;
            float blockAlong = float.MaxValue;
            for (int i = 0; i < Obstacles.Count; i++)
            {
                var o = Obstacles[i];
                if (o.Kind != ObstacleKind.Asteroid) continue;
                Vector3 rel = Flat(o.Position - pos);
                float along = Vector3.Dot(rel, dir);
                float r = o.Radius + clearance;
                if (along < -r || along > dist + r) continue;
                Vector3 across = rel - dir * along;
                if (across.LengthSquared() >= r * r) continue;
                if (along < blockAlong)
                {
                    blockAlong = along;
                    block = i;
                }
            }
            if (block < 0) return goal;

            var rock = Obstacles[block];
            Vector3 c = Flat(rock.Position - pos);
            float rr = rock.Radius + clearance;
            Vector3 side = new Vector3(dir.Z, 0f, -dir.X);
            // Go round on the side the rock's centre is not on; ties by ship id, so a group splits.
            float s = Vector3.Dot(c, side);
            float sign = System.MathF.Abs(s) > 1f ? -System.MathF.Sign(s) : ((ship.Id & 1) == 0 ? 1f : -1f);
            Vector3 waypoint = rock.Position + side * sign * rr * 1.15f;
            waypoint.Y = goal.Y;
            return waypoint;
        }

        // Nothing passes through rock: a ship that has drifted into one is pushed back out.
        void KeepOutOfRocks(Ship ship)
        {
            foreach (var o in Obstacles)
            {
                if (o.Kind != ObstacleKind.Asteroid) continue;
                Vector3 rel = ship.Position - o.Position;
                float r = o.Radius + ship.Def.Radius;
                float d2 = rel.LengthSquared();
                if (d2 >= r * r) continue;
                float d = SimMath.Sqrt(d2);
                Vector3 n = d > 1e-3f ? rel / d : Vector3.UnitX;
                ship.Position = o.Position + n * r;
            }
        }

        // Shields fail in a nebula.
        void NebulaEffects(Ship ship)
        {
            ship.InNebula = InNebula(ship.Position);
            if (ship.InNebula && ship.Shield > 0f)
            {
                ship.Shield = System.MathF.Max(0f, ship.Shield - ship.Def.ShieldHp * NebulaShieldDrain * Dt);
                ship.ShieldDelay = System.MathF.Max(ship.ShieldDelay, 1f);
            }
        }
    }
}
