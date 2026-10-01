using System.Numerics;

namespace GalacticRupture.Sim
{
    public enum BattleState
    {
        Fighting,
        Victory,
        Defeat,
        Withdrawn, // both sides still standing: someone pulled out, or time ran out
    }

    // The player's fleet and station against a Remnant squadron, in an asteroid field beside a
    // nebula. Player ships do only what they are told; their guns pick targets on their own
    // (IDEAS.md §2.1). The Remnant attack the station, run by the tactical AI (FleetAi), and both
    // sides have ships in reserve behind the command cap.
    public sealed class Skirmish : IDirector
    {
        public const int PlayerSide = 0;
        public const int EnemySide = 1;

        public BattleState State { get; private set; }
        public int Flagship { get; private set; } = -1;
        public int Station { get; private set; } = -1;
        public readonly Reinforcements Reinforcements = new Reinforcements();
        public TerrainSpec Terrain = new TerrainSpec { Asteroids = 2, Nebula = true };

        float _aiTimer;
        readonly FleetAi _ai = new FleetAi(EnemySide);

        // `arsenal`: every ship carries a different mix of weapon families, to see them side by side.
        public static World Create(ulong seed, bool arsenal = false)
        {
            var world = new World(seed);
            var s = new Skirmish();
            world.Director = s;
            Sim.Terrain.Populate(world, s.Terrain, seed);

            var station = world.Spawn(Hulls.Station(2), PlayerSide, new Vector3(0f, 0f, -3900f), 0f);
            station.Speed = 0f;
            s.Station = station.Id;
            s.Reinforcements.Station[PlayerSide] = station.Id;

            var bs = world.Spawn(arsenal ? ArsenalBattleship.Built : Hulls.Battleship, PlayerSide, new Vector3(0f, 0f, -2500f), 0f);
            bs.Speed = 0f;
            s.Flagship = bs.Id;
            int k = 0;
            foreach (var x in new[] { -340f, 340f, 0f })
                world.Spawn(arsenal ? ArsenalDestroyers[k++].Built : Hulls.Destroyer, PlayerSide, new Vector3(x, x == 0f ? -60f : 20f, x == 0f ? -2250f : -2650f), 0f).Speed = 0f;

            ref var rng = ref world.Rng;
            float yaw = SimMath.Pi;
            for (int i = 0; i < 2; i++)
                world.Spawn(arsenal ? ArsenalDestroyers[i].Built : Hulls.Destroyer, EnemySide, new Vector3((i - 0.5f) * 500f, 0f, 3000f), yaw);
            for (int i = 0; i < 4; i++)
                world.Spawn(Hulls.Frigate, EnemySide, new Vector3((i - 1.5f) * 380f, rng.Range(-100f, 100f), 3400f), yaw);
            for (int i = 0; i < 10; i++)
                world.Spawn(Hulls.Corvette, EnemySide, new Vector3((i - 4.5f) * 160f, rng.Range(-200f, 200f), 3900f), yaw);

            // Held back behind the cap: the player calls these in; the Remnant bring theirs as room opens.
            void Reserve(int side, HullDef def, int n)
            {
                for (int i = 0; i < n; i++)
                    s.Reinforcements.Pool[side].Add(new ReserveShip { Def = def, Faction = side, Hull = def.HullHp, Name = $"{def.Id} {i + 1}" });
            }
            Reserve(PlayerSide, Hulls.Destroyer, 2);
            Reserve(PlayerSide, Hulls.Frigate, 3);
            Reserve(PlayerSide, Hulls.Corvette, 4);
            Reserve(EnemySide, Hulls.Battleship, 1);
            Reserve(EnemySide, Hulls.Destroyer, 2);
            Reserve(EnemySide, Hulls.Frigate, 3);
            s.Reinforcements.Cap[EnemySide] = 24;
            return world;
        }

        static ShipDesign Loadout(string name, HullDef hull, params MountDef[] mounts)
        {
            var d = new ShipDesign(name, hull);
            for (int i = 0; i < mounts.Length; i++) d.Set(i, mounts[i]);
            return d;
        }

        static readonly ShipDesign ArsenalBattleship = Loadout("Arsenal", Hulls.Battleship,
            Hulls.HeavyTurbolaser, Hulls.HeavyRailgun, Hulls.LanceBattery, Hulls.MassAccelerator,
            Hulls.Railgun, Hulls.Railgun, Hulls.IonBattery, Hulls.IonBattery, Hulls.FlakBattery, Hulls.FlakBattery,
            Hulls.PulseLaser, Hulls.PulseLaser, Hulls.RotaryCannon, Hulls.RotaryCannon, Hulls.LightIon, Hulls.LightIon);

        static readonly ShipDesign[] ArsenalDestroyers =
        {
            Loadout("Rotary", Hulls.Destroyer, Hulls.RotaryDriver, Hulls.RotaryDriver, Hulls.AutoCannon, Hulls.AutoCannon),
            Loadout("Burner", Hulls.Destroyer, Hulls.ShieldBurner, Hulls.ShieldBurner, Hulls.PulseLaser, Hulls.PulseLaser),
            Loadout("Driver", Hulls.Destroyer, Hulls.MassDriver, Hulls.FlakBattery, Hulls.RotaryCannon, Hulls.LightIon),
        };

        public void Tick(World world)
        {
            Reinforcements.Tick(world, false, true);
            bool player = false, enemy = false;
            foreach (var s in world.Ships)
            {
                if (!s.Alive || s.Def.Class == HullClass.Transport) continue;
                if (s.Side == PlayerSide) player = true;
                else enemy = true;
            }
            if (!player && Reinforcements.HasWarships(PlayerSide)) { Reinforcements.Force(world, PlayerSide); player = true; }
            if (!enemy && Reinforcements.HasWarships(EnemySide)) { Reinforcements.Force(world, EnemySide); enemy = true; }
            if (State == BattleState.Fighting)
            {
                if (!enemy) State = BattleState.Victory;
                else if (!player) State = BattleState.Defeat;
            }

            _aiTimer -= World.Dt;
            if (_aiTimer > 0f) return;
            _aiTimer = 2f;
            _ai.Think(world, 2f);
        }
    }
}
