using System.Collections.Generic;
using System.Numerics;

namespace GalacticRupture.Sim.Strategic
{
    // One simulation, two resolutions (IDEAS.md §3): an engagement becomes an ordinary tactical
    // World whether the player commands it or leaves it to the fleet. Only who gives the orders
    // differs.
    public sealed class CampaignBattle : IDirector
    {
        public const float MaxSeconds = 600f;

        public readonly bool PlayerCommands;
        public BattleState State { get; private set; }
        public int Withdrawing { get; private set; } = -1;
        public int Flagship = -1;

        // Tactical ship id -> (fleet id, index in that fleet).
        public readonly List<(int Ship, int Fleet, int Index)> Map = new List<(int, int, int)>();

        float _aiTimer;

        public CampaignBattle(bool playerCommands) => PlayerCommands = playerCommands;

        public void Withdraw(int side)
        {
            if (State == BattleState.Fighting) Withdrawing = side;
        }

        public void Tick(World world)
        {
            if (State != BattleState.Fighting) return;
            bool player = false, enemy = false;
            foreach (var s in world.Ships)
            {
                if (!s.Alive) continue;
                if (s.Side == Campaign.PlayerFaction) player = true;
                else enemy = true;
            }
            if (!enemy) State = BattleState.Victory;
            else if (!player) State = BattleState.Defeat;
            else if (Withdrawing >= 0 || world.Time >= MaxSeconds) State = BattleState.Withdrawn;
            if (State != BattleState.Fighting) return;

            _aiTimer -= World.Dt;
            if (_aiTimer > 0f) return;
            _aiTimer = 2f;
            FleetAi.Command(world, Campaign.EnemyFaction);
            if (!PlayerCommands) FleetAi.Command(world, Campaign.PlayerFaction);
        }
    }

    public static class BattleSetup
    {
        public static World Build(Campaign c, Engagement e, bool playerCommands, out CampaignBattle battle)
        {
            var world = new World(c.Rng.NextULong());
            battle = new CampaignBattle(playerCommands);
            world.Director = battle;

            var placed = new int[2];
            float bestCost = -1f;
            foreach (int id in e.Fleets)
            {
                var f = c.FleetById(id);
                if (f == null) continue;
                int side = f.Owner == Campaign.PlayerFaction ? 0 : 1;
                float dir = side == 0 ? -1f : 1f;
                for (int i = 0; i < f.Ships.Count; i++)
                {
                    var r = f.Ships[i];
                    int k = placed[side]++;
                    // Line abreast, heavier ships toward the centre, rows by weight class.
                    int col = (k % 2 == 0 ? 1 : -1) * ((k + 1) / 2);
                    int row = r.Def.Class >= HullClass.Destroyer ? 0 : 1;
                    var p = new Vector3(col * 380f, world.Rng.Range(-80f, 80f), dir * (2600f + row * 500f));
                    var ship = world.Spawn(r.Def, side, p, side == 0 ? 0f : SimMath.Pi);
                    ship.Speed = r.Def.MaxSpeed * 0.3f;
                    ship.Hull = r.Hull;
                    for (int m = 0; m < ship.Mounts.Count && m < r.MountAlive.Length; m++)
                        ship.Mounts[m].Alive = r.MountAlive[m];
                    battle.Map.Add((ship.Id, f.Id, i));
                    if (side == 0 && r.Def.Cost > bestCost)
                    {
                        bestCost = r.Def.Cost;
                        battle.Flagship = ship.Id;
                    }
                }
            }
            return world;
        }

        // Damage, losses and lost turrets go back into the fleets; the loser, or whoever
        // withdrew, falls back a system.
        public static void Apply(Campaign c, Engagement e, World world, CampaignBattle battle)
        {
            var dead = new List<(int Fleet, int Index)>();
            foreach (var (shipId, fleetId, index) in battle.Map)
            {
                var ship = world.ShipById(shipId);
                var fleet = c.FleetById(fleetId);
                if (fleet == null || index >= fleet.Ships.Count) continue;
                var r = fleet.Ships[index];
                if (!ship.Alive)
                {
                    dead.Add((fleetId, index));
                    continue;
                }
                r.Hull = ship.Hull;
                for (int m = 0; m < ship.Mounts.Count && m < r.MountAlive.Length; m++)
                    r.MountAlive[m] = ship.Mounts[m].Alive;
            }
            dead.Sort((a, b) => a.Fleet != b.Fleet ? a.Fleet.CompareTo(b.Fleet) : b.Index.CompareTo(a.Index));
            foreach (var (fleetId, index) in dead) c.FleetById(fleetId).Ships.RemoveAt(index);

            int retreat = battle.State switch
            {
                BattleState.Victory => Campaign.EnemyFaction,
                BattleState.Defeat => Campaign.PlayerFaction,
                _ => battle.Withdrawing >= 0 ? battle.Withdrawing : e.Attacker,
            };
            c.EndEngagement(retreat);
        }

        // Leave it to the fleet: the same battle, unattended.
        public static BattleState AutoResolve(Campaign c)
        {
            var e = c.Pending;
            var world = Build(c, e, false, out var battle);
            int maxTicks = (int)(CampaignBattle.MaxSeconds * World.TicksPerSecond) + 1;
            for (int i = 0; i < maxTicks && battle.State == BattleState.Fighting; i++)
            {
                world.Step();
                world.Events.Clear();
            }
            Apply(c, e, world, battle);
            return battle.State;
        }
    }
}
