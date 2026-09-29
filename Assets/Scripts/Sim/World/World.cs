using System.Collections.Generic;
using System.Numerics;

namespace GalacticRupture.Sim
{
    public struct Projectile
    {
        public Vector3 Position;
        public Vector3 PrevPosition;
        public Vector3 Velocity;
        public float Life;
        public float Damage;
        public int Side;
        public int Shooter;
        public DamageChannel Channel;
        public MountClass Class;
    }

    // Anything that drives a scenario forward: spawning waves, scripted events.
    public interface IDirector
    {
        void Tick(World world);
    }

    // The whole tactical simulation. Fixed tick, no engine, deterministic from its seed.
    public sealed partial class World
    {
        public const int TicksPerSecond = 30;
        public const float Dt = 1f / TicksPerSecond;

        public readonly List<Ship> Ships = new List<Ship>();
        public readonly List<Projectile> Projectiles = new List<Projectile>();
        public readonly List<SimEvent> Events = new List<SimEvent>();
        public IDirector Director;
        public readonly SpatialGrid Grid = new SpatialGrid(1000f);
        readonly List<int> _query = new List<int>();
        const float ScanInterval = 0.5f;

        // Orders from the player (or an AI) wait here and apply at the start of the next tick,
        // so the same command stream always gives the same battle.
        readonly List<(int Ship, ShipOrder Order)> _commands = new List<(int, ShipOrder)>();

        public void Issue(int ship, ShipOrder order) => _commands.Add((ship, order));

        void ApplyCommands()
        {
            foreach (var (id, order) in _commands)
            {
                var ship = ShipById(id);
                if (ship == null || !ship.Alive) continue;
                ship.Order = order;
                ship.RunOutbound = false;
                ship.FocusTarget = order.Kind == OrderKind.Attack ? order.OtherShip : -1;
            }
            _commands.Clear();
        }

        public int Tick { get; private set; }
        public float Time => Tick * Dt;

        Rng _rng;

        public World(ulong seed) => _rng = new Rng(seed);

        public ref Rng Rng => ref _rng;

        public Ship Spawn(HullDef def, int side, Vector3 position, float yaw)
        {
            var ship = new Ship(Ships.Count, side, def, position, yaw);
            ship.Speed = def.MaxSpeed * 0.5f;
            Ships.Add(ship);
            Events.Add(new SimEvent { Kind = SimEventKind.ShipSpawned, Ship = ship.Id, Position = position });
            return ship;
        }

        // Events accumulate until the caller clears them, so a frame that runs several ticks sees all.
        public void Step()
        {
            Tick++;
            ApplyCommands();
            Director?.Tick(this);

            foreach (var ship in Ships)
            {
                ship.PrevPosition = ship.Position;
                ship.PrevYaw = ship.Yaw;
                foreach (var m in ship.Mounts)
                {
                    m.PrevYaw = m.Yaw;
                    m.PrevPitch = m.Pitch;
                }
            }

            foreach (var ship in Ships)
                if (ship.Alive) Pilot(ship);

            Grid.Rebuild(Ships);
            foreach (var ship in Ships)
                if (ship.Alive) ScanForEnemies(ship);

            foreach (var ship in Ships)
                if (ship.Alive) UpdateShields(ship);

            foreach (var ship in Ships)
                if (ship.Alive)
                    foreach (var m in ship.Mounts)
                        if (m.Alive) UpdateMount(ship, m);

            UpdateProjectiles();
        }

        public ulong Hash()
        {
            var h = StateHash.Create();
            h.Add(Tick);
            foreach (var s in Ships)
            {
                h.Add(s.Position);
                h.Add(s.Yaw);
                h.Add(s.Hull);
                h.Add(s.Shield);
                foreach (var m in s.Mounts)
                {
                    h.Add(m.Yaw);
                    h.Add(m.Pitch);
                    h.Add(m.Target);
                }
            }
            h.Add(Projectiles.Count);
            return h.Value;
        }

        public Ship ShipById(int id) => id >= 0 && id < Ships.Count ? Ships[id] : null;

        void ScanForEnemies(Ship ship)
        {
            ship.ScanTimer -= Dt;
            if (ship.ScanTimer > 0f) return;
            ship.ScanTimer = ScanInterval;
            ship.Enemies.Clear();
            if (ship.MaxGunRange <= 0f) return;
            _query.Clear();
            float reach = ship.MaxGunRange + 400f;
            Grid.Query(ship.Position, reach, _query);
            foreach (int id in _query)
            {
                var other = Ships[id];
                if (other.Side == ship.Side || !other.Alive) continue;
                if (Vector3.DistanceSquared(other.Position, ship.Position) > reach * reach) continue;
                ship.Enemies.Add(id);
            }
        }

        void UpdateShields(Ship ship)
        {
            if (ship.ShieldDelay > 0f)
            {
                ship.ShieldDelay -= Dt;
                return;
            }
            if (ship.Shield < ship.Def.ShieldHp)
                ship.Shield = System.MathF.Min(ship.Def.ShieldHp, ship.Shield + ship.Def.ShieldRegen * Dt);
        }
    }
}
