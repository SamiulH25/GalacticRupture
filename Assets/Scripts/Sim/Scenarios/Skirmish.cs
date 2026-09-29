using System.Numerics;

namespace GalacticRupture.Sim
{
    public enum BattleState
    {
        Fighting,
        Victory,
        Defeat,
    }

    // The player's fleet against a Remnant squadron. Player ships do only what they are told;
    // their guns pick targets on their own (IDEAS.md §2.1). The enemy is run by a simple AI.
    public sealed class Skirmish : IDirector
    {
        public const int PlayerSide = 0;
        public const int EnemySide = 1;

        public BattleState State { get; private set; }
        public int Flagship { get; private set; } = -1;

        float _aiTimer;

        public static World Create(ulong seed)
        {
            var world = new World(seed);
            var s = new Skirmish();
            world.Director = s;

            var bs = world.Spawn(Hulls.Battleship, PlayerSide, new Vector3(0f, 0f, -2500f), 0f);
            bs.Speed = 0f;
            s.Flagship = bs.Id;
            foreach (var x in new[] { -340f, 340f, 0f })
                world.Spawn(Hulls.Destroyer, PlayerSide, new Vector3(x, x == 0f ? -60f : 20f, x == 0f ? -2250f : -2650f), 0f).Speed = 0f;

            ref var rng = ref world.Rng;
            float yaw = SimMath.Pi;
            for (int i = 0; i < 2; i++)
                world.Spawn(Hulls.Destroyer, EnemySide, new Vector3((i - 0.5f) * 500f, 0f, 3000f), yaw);
            for (int i = 0; i < 4; i++)
                world.Spawn(Hulls.Frigate, EnemySide, new Vector3((i - 1.5f) * 380f, rng.Range(-100f, 100f), 3400f), yaw);
            for (int i = 0; i < 10; i++)
                world.Spawn(Hulls.Corvette, EnemySide, new Vector3((i - 4.5f) * 160f, rng.Range(-200f, 200f), 3900f), yaw);
            return world;
        }

        public void Tick(World world)
        {
            bool player = false, enemy = false;
            foreach (var s in world.Ships)
            {
                if (!s.Alive) continue;
                if (s.Side == PlayerSide) player = true;
                else enemy = true;
            }
            if (State == BattleState.Fighting)
            {
                if (!enemy) State = BattleState.Victory;
                else if (!player) State = BattleState.Defeat;
            }

            _aiTimer -= World.Dt;
            if (_aiTimer > 0f) return;
            _aiTimer = 2f;
            EnemyAi(world);
        }

        // Idle enemies pick the nearest player ship: escorts make attack runs, destroyers slug it out.
        static void EnemyAi(World world)
        {
            foreach (var s in world.Ships)
            {
                if (!s.Alive || s.Side != EnemySide) continue;
                var current = world.ShipById(s.Order.OtherShip);
                bool busy = (s.Order.Kind == OrderKind.Attack || s.Order.Kind == OrderKind.AttackRun) && current != null && current.Alive;
                if (busy) continue;
                var target = Nearest(world, s, PlayerSide);
                if (target == null) continue;
                if (s.Def.Class <= HullClass.Frigate)
                {
                    var o = ShipOrder.AttackRun(target.Id);
                    o.PassDistance = s.Def.Class == HullClass.Corvette ? world.Rng.Range(150f, 350f) : world.Rng.Range(400f, 700f);
                    o.RunOutDistance = world.Rng.Range(1400f, 2200f);
                    world.Issue(s.Id, o);
                }
                else
                {
                    world.Issue(s.Id, ShipOrder.Attack(target.Id));
                }
            }
        }

        static Ship Nearest(World world, Ship from, int side)
        {
            Ship best = null;
            float bestD = float.MaxValue;
            foreach (var s in world.Ships)
            {
                if (!s.Alive || s.Side != side) continue;
                float d = Vector3.DistanceSquared(s.Position, from.Position);
                if (d < bestD)
                {
                    bestD = d;
                    best = s;
                }
            }
            return best;
        }
    }
}
