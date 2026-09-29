using System.Numerics;

namespace GalacticRupture.Sim
{
    // Build-order step 1 (IDEAS.md §9): a battleship and two escorts holding off endless waves
    // of corvettes and frigates making attack runs. Exists to watch the guns.
    public sealed class TurretToy : IDirector
    {
        public const int PlayerSide = 0;
        public const int EnemySide = 1;

        public int Flagship { get; private set; } = -1;
        public int Wave { get; private set; }

        float _waveTimer = 3f;
        float _flagshipTimer;

        public static World Create(ulong seed)
        {
            var world = new World(seed);
            var toy = new TurretToy();
            world.Director = toy;
            toy.SpawnFlagshipGroup(world, Vector3.Zero, 0f);
            return world;
        }

        public void Tick(World world)
        {
            var flagship = world.ShipById(Flagship);
            if (flagship == null || !flagship.Alive)
            {
                _flagshipTimer += World.Dt;
                if (_flagshipTimer > 10f)
                {
                    _flagshipTimer = 0f;
                    SpawnFlagshipGroup(world, Vector3.Zero, 0f);
                }
                return;
            }

            bool enemiesLeft = false;
            foreach (var s in world.Ships)
                if (s.Alive && s.Side == EnemySide) { enemiesLeft = true; break; }

            if (enemiesLeft) return;
            _waveTimer -= World.Dt;
            if (_waveTimer > 0f) return;
            _waveTimer = 6f;
            SpawnWave(world, flagship);
        }

        void SpawnFlagshipGroup(World world, Vector3 at, float yaw)
        {
            var bs = world.Spawn(Hulls.Battleship, PlayerSide, at, yaw);
            bs.Order = ShipOrder.Orbit(at + new Vector3(3500f, 0f, 0f), 3500f, false);
            bs.CruiseSpeedFraction = 0.55f;
            Flagship = bs.Id;

            var port = world.Spawn(Hulls.Destroyer, PlayerSide, bs.LocalToWorld(new Vector3(-240f, 30f, -80f)), yaw);
            port.Order = ShipOrder.Escort(bs.Id, new Vector3(-240f, 30f, -80f));
            var stbd = world.Spawn(Hulls.Destroyer, PlayerSide, bs.LocalToWorld(new Vector3(240f, -20f, -140f)), yaw);
            stbd.Order = ShipOrder.Escort(bs.Id, new Vector3(240f, -20f, -140f));
        }

        void SpawnWave(World world, Ship flagship)
        {
            Wave++;
            ref var rng = ref world.Rng;
            float bearing = rng.Range(0f, SimMath.TwoPi);
            Vector3 dir = new Vector3(SimMath.Sin(bearing), 0f, SimMath.Cos(bearing));
            Vector3 centre = flagship.Position + dir * 5200f;
            float yaw = bearing + SimMath.Pi;
            Vector3 right = new Vector3(dir.Z, 0f, -dir.X);

            int corvettes = 8 + System.Math.Min(Wave * 2, 10);
            for (int i = 0; i < corvettes; i++)
            {
                Vector3 p = centre + right * ((i - corvettes * 0.5f) * 120f) + dir * rng.Range(-150f, 150f);
                p.Y = rng.Range(-300f, 300f);
                var c = world.Spawn(Hulls.Corvette, EnemySide, p, yaw);
                c.Order = ShipOrder.AttackRun(PickTarget(world, flagship, ref rng));
                c.Order.PassDistance = rng.Range(120f, 320f);
                c.Order.RunOutDistance = rng.Range(1200f, 2000f);
            }

            int frigates = 2 + System.Math.Min(Wave / 2, 4);
            for (int i = 0; i < frigates; i++)
            {
                Vector3 p = centre - dir * 600f + right * ((i - frigates * 0.5f) * 300f);
                p.Y = rng.Range(-150f, 150f);
                var f = world.Spawn(Hulls.Frigate, EnemySide, p, yaw);
                f.Order = ShipOrder.AttackRun(flagship.Id);
                f.Order.PassDistance = rng.Range(350f, 650f);
                f.Order.RunOutDistance = 2000f;
            }
        }

        static int PickTarget(World world, Ship flagship, ref Rng rng)
        {
            if (rng.NextFloat() < 0.65f) return flagship.Id;
            foreach (var s in world.Ships)
                if (s.Alive && s.Side == PlayerSide && s != flagship && rng.NextFloat() < 0.5f)
                    return s.Id;
            return flagship.Id;
        }
    }
}
