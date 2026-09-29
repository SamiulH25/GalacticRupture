using System.Collections.Generic;
using System.Numerics;

namespace GalacticRupture.Sim
{
    // Uniform grid over the XZ plane, rebuilt each tick. Queries visit cells in a fixed order
    // and each cell lists ships in id order, so results are deterministic.
    public sealed class SpatialGrid
    {
        public readonly float CellSize;
        readonly Dictionary<long, List<int>> _cells = new Dictionary<long, List<int>>();
        readonly List<List<int>> _used = new List<List<int>>();
        readonly Stack<List<int>> _pool = new Stack<List<int>>();

        public SpatialGrid(float cellSize) => CellSize = cellSize;

        static long Key(int x, int z) => ((long)x << 32) ^ (uint)z;

        int Cell(float v) => (int)System.MathF.Floor(v / CellSize);

        public void Rebuild(List<Ship> ships)
        {
            foreach (var list in _used)
            {
                list.Clear();
                _pool.Push(list);
            }
            _used.Clear();
            _cells.Clear();
            foreach (var s in ships)
            {
                if (!s.Alive) continue;
                long key = Key(Cell(s.Position.X), Cell(s.Position.Z));
                if (!_cells.TryGetValue(key, out var list))
                {
                    list = _pool.Count > 0 ? _pool.Pop() : new List<int>();
                    _cells[key] = list;
                    _used.Add(list);
                }
                list.Add(s.Id);
            }
        }

        // Ship ids whose position lies within `radius` of `p` on the XZ plane (cell-accurate:
        // callers do their own exact checks).
        public void Query(Vector3 p, float radius, List<int> results)
        {
            int x0 = Cell(p.X - radius), x1 = Cell(p.X + radius);
            int z0 = Cell(p.Z - radius), z1 = Cell(p.Z + radius);
            for (int x = x0; x <= x1; x++)
            for (int z = z0; z <= z1; z++)
                if (_cells.TryGetValue(Key(x, z), out var list))
                    results.AddRange(list);
        }
    }
}
