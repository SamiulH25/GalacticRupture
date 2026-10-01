using System.Collections.Generic;
using System.Numerics;

namespace GalacticRupture.Sim.Strategic
{
    // One simulation, two resolutions (IDEAS.md §3): an engagement becomes an ordinary tactical
    // World whether the player commands it or leaves it to the fleet. Only who gives the orders
    // differs. Side 0 is the player's coalition whenever the player is involved.
    public sealed class CampaignBattle : IDirector
    {
        public const float MaxSeconds = 1500f;

        public readonly bool PlayerCommands;
        public BattleState State { get; private set; }
        public int Withdrawing { get; private set; } = -1;
        public int Flagship = -1;
        public int PlayerSide = -1;
        public int Station = -1;          // tactical id of the system's station, -1 none
        public int StationSystem = -1;
        public TerrainSpec Terrain;

        // Tactical ship id -> (fleet id, index in that fleet).
        public readonly List<(int Ship, int Fleet, int Index)> Map = new List<(int, int, int)>();

        // Ships beyond each side's command cap wait off the field (Empire at War's population cap).
        public readonly Reinforcements Reinforcements = new Reinforcements();

        float _aiTimer;
        readonly FleetAi[] _ai = { new FleetAi(0), new FleetAi(1) };

        // Unattended sides are commanded by their admiral: the one who breaks off a losing fight.
        public readonly Admiral[] Admirals = new Admiral[2];
        public readonly float[] StartStrength = new float[2];

        public CampaignBattle(bool playerCommands)
        {
            PlayerCommands = playerCommands;
            Reinforcements.Deployed = (ship, r) => { if (r.Fleet >= 0) Map.Add((ship.Id, r.Fleet, r.Index)); };
        }

        public static float SideStrength(World world, int side)
        {
            float s = 0f;
            foreach (var ship in world.Ships)
                if (ship.Alive && ship.Side == side && ship.Def.Class != HullClass.Transport) s += ship.Def.Cost * (ship.Hull / ship.Def.HullHp);
            return s;
        }

        // Strength on the field plus what waits in reserve, for the admiral's nerve.
        float TotalStrength(World world, int side)
        {
            float s = SideStrength(world, side);
            foreach (var r in Reinforcements.Pool[side])
                if (r.Def.Class != HullClass.Transport) s += r.Def.Cost * (r.Hull / r.Def.HullHp);
            return s;
        }

        // Troop ships of `side` still within reach of an enemy warship.
        public static bool Cornered(World world, int side)
        {
            foreach (var t in world.Ships)
            {
                if (!t.Alive || t.Side != side || t.Def.Class != HullClass.Transport) continue;
                foreach (var e in world.Ships)
                    if (e.Alive && e.Side != side && e.Def.Class != HullClass.Transport
                        && System.Numerics.Vector3.DistanceSquared(e.Position, t.Position) < 4500f * 4500f) return true;
            }
            return false;
        }

        public void Withdraw(int side)
        {
            if (State == BattleState.Fighting) Withdrawing = side;
        }

        bool AiSide(int side) => !(side == 0 && PlayerCommands);

        public void Tick(World world)
        {
            if (State != BattleState.Fighting) return;
            Reinforcements.Tick(world, AiSide(0), AiSide(1));

            bool side0 = false, side1 = false;
            foreach (var s in world.Ships)
            {
                if (!s.Alive || s.Def.Class == HullClass.Transport) continue;
                if (s.Side == 0) side0 = true;
                else side1 = true;
            }
            // Nothing left on the field but ships in reserve: they come in, whoever commands.
            if (!side0 && Reinforcements.HasWarships(0)) { Reinforcements.Force(world, 0); side0 = true; }
            if (!side1 && Reinforcements.HasWarships(1)) { Reinforcements.Force(world, 1); side1 = true; }
            // A side with only troop ships left has lost the fight, once those have got clear
            // (or died): unescorted transports are fair game while the enemy's guns can reach them.
            if (!side1 && !Cornered(world, 1)) State = BattleState.Victory;
            else if (!side0 && !Cornered(world, 0)) State = BattleState.Defeat;
            else if (Withdrawing >= 0 || world.Time >= MaxSeconds) State = BattleState.Withdrawn;
            if (State != BattleState.Fighting) return;
            for (int side = 0; side < 2; side++)
            {
                if (!AiSide(side)) continue;
                // A station doesn't run: the side holding one fights to the end.
                if (world.ShipById(Station) is Ship st && st.Alive && st.Side == side) continue;
                float breakAt = Admirals[side]?.BreakAt ?? 0.3f;
                if (StartStrength[side] > 0f && TotalStrength(world, side) < StartStrength[side] * breakAt)
                {
                    Withdrawing = side;
                    State = BattleState.Withdrawn;
                    return;
                }
            }

            _aiTimer -= World.Dt;
            if (_aiTimer > 0f) return;
            _aiTimer = 2f;
            _ai[1].Think(world, 2f);
            if (AiSide(0)) _ai[0].Think(world, 2f);
        }
    }

    public static class BattleSetup
    {
        // The battlefield of an engagement: the system's own terrain, or open lane space with
        // now and then some drifting rock.
        public static TerrainSpec TerrainOf(Campaign c, Engagement e)
        {
            if (e.System >= 0) return c.Galaxy.Systems[e.System].Terrain;
            uint h = (uint)(e.LaneA * 7919 + e.LaneB * 104729) * 2654435761u;
            h ^= h >> 16;
            return new TerrainSpec { Asteroids = (h & 3) == 0 ? 1 : 0, Nebula = ((h >> 4) & 7) == 0 };
        }

        // `vanguard` (fleet id, index in fleet): the player's ships that start on the field; the
        // rest of side 0 waits in reserve, to be called in near them. Null: the line deploys
        // heaviest first up to the cap, as the AI's does.
        public static World Build(Campaign c, Engagement e, bool playerCommands, out CampaignBattle battle,
            ICollection<(int Fleet, int Index)> vanguard = null)
        {
            var world = new World(c.Rng.NextULong());
            battle = new CampaignBattle(playerCommands) { PlayerSide = e.SideOf(Campaign.PlayerFaction) };
            world.Director = battle;
            battle.Terrain = TerrainOf(c, e);
            Terrain.Populate(world, battle.Terrain, (ulong)(e.System >= 0 ? e.System : e.LaneA * 1000 + e.LaneB) * 0x9E3779B97F4A7C15UL + 17UL);

            // The system's station, if its owner is in the fight: at the back of its side's field.
            if (e.System >= 0)
            {
                var sys = c.Galaxy.Systems[e.System];
                int side = sys.Owner >= 0 ? e.SideOf(sys.Owner) : -1;
                if (sys.Station > 0 && sys.StationHull > 0f && side >= 0)
                {
                    var def = Hulls.Station(sys.Station);
                    float dir = side == 0 ? -1f : 1f;
                    var st = world.Spawn(def, side, new Vector3(0f, 0f, dir * 3900f), side == 0 ? 0f : SimMath.Pi);
                    st.Faction = sys.Owner;
                    st.Speed = 0f;
                    st.Hull = def.HullHp * sys.StationHull;
                    battle.Station = st.Id;
                    battle.StationSystem = sys.Id;
                    battle.Reinforcements.Station[side] = st.Id;
                }
            }

            // Every ship present, heaviest first: the line deploys, the rest wait in reserve.
            var queued = new List<ReserveShip>[] { new List<ReserveShip>(), new List<ReserveShip>() };
            foreach (int id in e.Fleets)
            {
                var f = c.FleetById(id);
                if (f == null) continue;
                int side = e.SideOf(f.Owner);
                if (side < 0) continue;
                // The side's admiral is the best one present.
                if (f.Admiral != null && (battle.Admirals[side] == null || f.Admiral.Tactics > battle.Admirals[side].Tactics))
                    battle.Admirals[side] = f.Admiral;
                for (int i = 0; i < f.Ships.Count; i++)
                {
                    var r = f.Ships[i];
                    float reload = f.Admiral != null && !(side == 0 && playerCommands) ? f.Admiral.ReloadScale : 1f;
                    queued[side].Add(new ReserveShip
                    {
                        Def = r.Def, Name = r.Name, Faction = f.Owner, Hull = r.Hull,
                        MountAlive = (bool[])r.MountAlive.Clone(),
                        ReloadScale = reload * c.Factions[f.Owner].Mods.Reload,
                        Fleet = f.Id, Index = i,
                    });
                }
            }

            var placed = new int[2];
            float bestCost = -1f;
            for (int side = 0; side < 2; side++)
            {
                var list = queued[side];
                // Stable: heaviest warships first, troop ships last, fleet order otherwise.
                var order = new List<int>();
                for (int i = 0; i < list.Count; i++) order.Add(i);
                order.Sort((a, b) =>
                {
                    float ka = list[a].Def.Class == HullClass.Transport ? -1f : list[a].Def.Cost;
                    float kb = list[b].Def.Class == HullClass.Transport ? -1f : list[b].Def.Cost;
                    return ka != kb ? kb.CompareTo(ka) : a.CompareTo(b);
                });
                int used = 0;
                float dir = side == 0 ? -1f : 1f;
                bool chosen = side == 0 && playerCommands && vanguard != null && HasWarship(list, vanguard);
                foreach (int i in order)
                {
                    var r = list[i];
                    int pts = Reinforcements.Points(r.Def.Class);
                    if (used + pts > battle.Reinforcements.Cap[side] || (chosen && !vanguard.Contains((r.Fleet, r.Index))))
                    {
                        battle.Reinforcements.Pool[side].Add(r);
                        continue;
                    }
                    used += pts;
                    int k = placed[side]++;
                    // Line abreast, heavy ships in front, escorts behind, troop ships at the back.
                    int col = (k % 2 == 0 ? 1 : -1) * ((k + 1) / 2);
                    int row = r.Def.Class == HullClass.Transport ? 2 : r.Def.Class >= HullClass.Destroyer ? 0 : 1;
                    var p = new Vector3(col * 380f, world.Rng.Range(-80f, 80f), dir * (2600f + row * 500f));
                    var ship = world.Spawn(r.Def, side, p, side == 0 ? 0f : SimMath.Pi);
                    ship.Faction = r.Faction;
                    ship.ReloadScale = r.ReloadScale;
                    ship.Speed = ship.TopSpeed * 0.3f;
                    ship.Hull = r.Hull;
                    for (int m = 0; m < ship.Mounts.Count && m < r.MountAlive.Length; m++)
                        ship.Mounts[m].Alive = r.MountAlive[m];
                    battle.Map.Add((ship.Id, r.Fleet, r.Index));
                    if (side == 0 && r.Def.Class != HullClass.Transport && r.Def.Cost > bestCost)
                    {
                        bestCost = r.Def.Cost;
                        battle.Flagship = ship.Id;
                    }
                }
            }
            if (battle.Flagship < 0 && battle.Station >= 0 && world.ShipById(battle.Station).Side == 0) battle.Flagship = battle.Station;
            battle.StartStrength[0] = CampaignBattle.SideStrength(world, 0);
            battle.StartStrength[1] = CampaignBattle.SideStrength(world, 1);
            foreach (var r in battle.Reinforcements.Pool[0]) if (r.Def.Class != HullClass.Transport) battle.StartStrength[0] += r.Def.Cost * r.Hull / r.Def.HullHp;
            foreach (var r in battle.Reinforcements.Pool[1]) if (r.Def.Class != HullClass.Transport) battle.StartStrength[1] += r.Def.Cost * r.Hull / r.Def.HullHp;
            return world;
        }

        // A vanguard with no warship in it would leave nothing to call reserves in on.
        static bool HasWarship(List<ReserveShip> list, ICollection<(int Fleet, int Index)> vanguard)
        {
            foreach (var r in list)
                if (r.Def.Class != HullClass.Transport && vanguard.Contains((r.Fleet, r.Index))) return true;
            return false;
        }

        // The player's side of an engagement, ship by ship, for choosing the vanguard.
        public static List<(int Fleet, int Index, ShipRecord Ship)> PlayerShips(Campaign c, Engagement e)
        {
            var list = new List<(int, int, ShipRecord)>();
            foreach (int id in e.Fleets)
            {
                var f = c.FleetById(id);
                if (f == null || e.SideOf(f.Owner) != 0) continue;
                for (int i = 0; i < f.Ships.Count; i++) list.Add((f.Id, i, f.Ships[i]));
            }
            return list;
        }

        // Damage, losses and lost turrets go back into the fleets; the loser, or whoever
        // withdrew, falls back.
        // Every battle written back, attended or not: for tests and tools.
        public static System.Action<Campaign, Engagement, World, CampaignBattle> Resolved;

        public static void Apply(Campaign c, Engagement e, World world, CampaignBattle battle)
        {
            Resolved?.Invoke(c, e, world, battle);
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

            // The station keeps its damage; destroyed, it is gone and must be built again.
            if (battle.StationSystem >= 0 && world.ShipById(battle.Station) is Ship st)
            {
                var sys = c.Galaxy.Systems[battle.StationSystem];
                if (st.Alive) sys.StationHull = st.Hull / st.Def.HullHp;
                else
                {
                    sys.Station = 0;
                    sys.StationHull = 0f;
                    sys.StationHours = 0f;
                }
            }

            // Victory means side 1 has no warships left: its survivors (troop ships) run.
            int retreat = battle.State switch
            {
                BattleState.Victory => 1,
                BattleState.Defeat => 0,
                _ => battle.Withdrawing >= 0 ? battle.Withdrawing : e.SideOf(e.Attacker) < 0 ? 1 : e.SideOf(e.Attacker),
            };
            c.EndEngagement(e, retreat);
        }

        // Leave it to the fleet: the same battle, unattended.
        public static BattleState AutoResolve(Campaign c, Engagement e)
        {
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

        public static BattleState AutoResolve(Campaign c) => AutoResolve(c, c.Pending);
    }
}
