using System.Numerics;

namespace GalacticRupture.Sim
{
    // The unit-count benchmark (IDEAS.md §8.8): two fleets of mixed hulls in loose formation,
    // closing slowly, far enough apart that the fight starts partway through a run.
    public static class FleetBench
    {
        public static World Create(ulong seed, int ships)
        {
            var world = new World(seed);
            int perSide = System.Math.Max(1, ships / 2);
            for (int side = 0; side < 2; side++)
            {
                float dir = side == 0 ? 1f : -1f;
                int cols = (int)System.MathF.Ceiling(System.MathF.Sqrt(perSide * 2f));
                for (int i = 0; i < perSide; i++)
                {
                    HullDef def = (i % 10) switch
                    {
                        0 => Hulls.Battleship,
                        1 or 2 => Hulls.Destroyer,
                        3 or 4 or 5 => Hulls.Frigate,
                        _ => Hulls.Corvette,
                    };
                    int row = i / cols, col = i % cols;
                    var p = new Vector3((col - cols * 0.5f) * 420f, world.Rng.Range(-150f, 150f), -dir * (3000f + row * 520f));
                    var ship = world.Spawn(def, side, p, side == 0 ? 0f : SimMath.Pi);
                    ship.Order = ShipOrder.MoveTo(new Vector3(p.X, p.Y, 0f));
                    ship.CruiseSpeedFraction = 0.35f;
                }
            }
            return world;
        }
    }
}
