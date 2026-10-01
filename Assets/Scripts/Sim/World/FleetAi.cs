using System.Collections.Generic;
using System.Numerics;

namespace GalacticRupture.Sim
{
    // Tactical doctrine for a side nobody is commanding. Every couple of seconds it reads the
    // field as a whole and gives each ship a job:
    //   - Defending a station, the fleet holds under the station's guns and only goes out to
    //     meet an enemy that comes close, unless it clearly outguns the attacker.
    //   - Attacking a station, the fleet gathers out of the station's reach and goes in together,
    //     escorts first at the escorts, the line at the station once the defenders thin out.
    //   - Heavy ships close and fight broadside; escorts make attack runs on escorts, then on
    //     whatever the line is fighting.
    //   - Badly hurt ships fall back behind the line (under the station, if there is one).
    //   - Clearly outgunned, a fleet near a nebula goes into it: no shields for anyone in there,
    //     and nobody can shoot what they cannot see.
    //   - Shield-heavy fleets keep out of nebulae; everyone steers round rock (World.AvoidRocks)
    //     and moves on to get a clear line past it (World.AttackGoal).
    //   - Abilities: shields when the shields are failing under fire, engines to close or to
    //     get out, weapons when in range with shields to spare.
    public sealed class FleetAi
    {
        public readonly int Side;
        float _stagedFor;          // seconds spent gathering before an assault
        bool _committed;           // the assault is on
        float _hidden;             // seconds spent waiting in a nebula
        bool _hideOver;            // waited long enough: nobody came
        int _group = -1;           // the pace group the fighting ships move on (World.GroupPace)
        readonly List<Ship> _own = new List<Ship>();
        readonly List<Ship> _enemy = new List<Ship>();

        public FleetAi(int side) => Side = side;

        // Stateless doctrine for one tick (scenarios that keep no AI object).
        public static void Command(World world, int side) => new FleetAi(side).Think(world, 2f);

        public void Think(World world, float dt)
        {
            _own.Clear();
            _enemy.Clear();
            Ship station = null, enemyStation = null;
            float ownStrength = 0f, enemyStrength = 0f;
            foreach (var s in world.Ships)
            {
                if (!s.Alive) continue;
                bool ours = s.Side == Side;
                if (s.Def.Class == HullClass.Station)
                {
                    if (ours) station = s;
                    else enemyStation = s;
                }
                else (ours ? _own : _enemy).Add(s);
                if (s.Def.Class == HullClass.Transport) continue;
                float v = Value(s);
                if (ours) ownStrength += v;
                else enemyStrength += v;
            }
            if (_own.Count == 0) return;
            if (_enemy.Count == 0 && enemyStation == null) return;
            if (_group < 0) _group = world.NewGroup();

            Vector3 ownCentre = Centre(_own), enemyCentre = _enemy.Count > 0 ? Centre(_enemy) : enemyStation.Position;
            float ratio = ownStrength / System.MathF.Max(1f, enemyStrength);

            // An assault on a station: gather first, then go in together.
            Vector3 rally = default;
            bool staging = false;
            if (enemyStation != null && !_committed)
            {
                float reach = enemyStation.MaxGunRange + 900f;
                Vector3 away = Flat(ownCentre - enemyStation.Position);
                away = away.LengthSquared() > 1f ? Vector3.Normalize(away) : Vector3.UnitZ * (Side == 0 ? -1f : 1f);
                rally = enemyStation.Position + away * reach;
                rally.Y = 0f;
                int gathered = 0, fleet = 0;
                foreach (var s in _own)
                {
                    if (s.Def.Class == HullClass.Transport) continue;
                    fleet++;
                    if (Vector3.Distance(Flat(s.Position), Flat(rally)) < 1400f) gathered++;
                }
                _stagedFor += dt;
                // Defenders coming out to meet us, or everyone in place (or waited long enough): go.
                bool pressed = false;
                foreach (var e in _enemy)
                    if (Vector3.Distance(e.Position, ownCentre) < 3500f && e.Def.Class != HullClass.Transport) pressed = true;
                if (pressed || gathered >= fleet * 0.75f || _stagedFor > 45f) _committed = true;
                else staging = true;
            }

            int healthy = 0;
            foreach (var s in _own) if (s.Def.Class != HullClass.Transport && !Hurt(s)) healthy++;
            bool avoidNebula = ShieldHeavy(_own) && !AnyIn(world, _enemy);
            Vector3 hideIn = default;
            // Only a side with nothing to take hides: an assault on a station has to go in.
            bool hide = !_hideOver && ratio < 0.6f && station == null && enemyStation == null
                        && NearestNebula(world, ownCentre, enemyCentre, out hideIn);
            if (hide)
            {
                _hidden += dt;
                bool contact = false;
                foreach (var e in _enemy)
                    if (Vector3.Distance(e.Position, ownCentre) < 2500f) contact = true;
                if (contact) _hidden = 0f;
                if (_hidden > 60f) _hideOver = hide = false;
            }

            foreach (var s in _own)
            {
                if (s.Def.Class == HullClass.Transport)
                {
                    Transport(world, s, station, enemyCentre, ownStrength > 0f);
                    continue;
                }
                UseAbilities(world, s);

                // Badly hurt: fall back behind the line (under the station if there is one), as
                // long as healthy ships remain to hold it. The fallback point is fixed, at the
                // side's own edge of the field, so a retreat never turns into a chase.
                if (Hurt(s) && ratio < 2.5f && healthy > 0)
                {
                    Vector3 refuge = station != null ? station.Position + Rear * 0.15f : Rear;
                    if (Vector3.Distance(Flat(s.Position), Flat(refuge)) > 450f) Move(world, s, refuge);
                    else
                    {
                        // In cover: turn the broadside to the enemy and let the guns work.
                        Vector3 back = Vector3.Normalize(Flat(Rear) + new Vector3(0.001f, 0f, 0f));
                        if (s.Order.Kind != OrderKind.Face) world.Issue(s.Id, ShipOrder.Face(s.Position + new Vector3(back.Z, 0f, -back.X) * 500f));
                    }
                    continue;
                }

                if (staging)
                {
                    // Escorts screen ahead of the rally point; the line forms on it.
                    Vector3 slot = rally + Offset(s, 420f);
                    if (Vector3.Distance(Flat(s.Position), Flat(slot)) > 350f) Move(world, s, slot, _group);
                    else if (s.Order.Kind != OrderKind.Face) world.Issue(s.Id, ShipOrder.Face(enemyStation.Position));
                    continue;
                }

                if (hide)
                {
                    if (!world.InNebula(s.Position))
                    {
                        Move(world, s, hideIn + Offset(s, 300f), _group);
                        continue;
                    }
                    // In the cloud: fight whatever comes close enough to see.
                    var close = Nearest(_enemy, s, 1500f);
                    if (close != null) Engage(world, s, close, _group);
                    else if (s.Order.Kind != OrderKind.Hold) world.Issue(s.Id, ShipOrder.Hold());
                    continue;
                }

                var target = PickTarget(world, s, station, enemyStation, ratio, avoidNebula);
                if (target == null)
                {
                    if (station != null && Vector3.Distance(s.Position, station.Position) > 1200f)
                        Move(world, s, station.Position + Offset(s, 700f));
                    continue;
                }
                Engage(world, s, target, _group);
            }
        }

        static bool Hurt(Ship s) => s.Hull < s.Def.HullHp * 0.22f;

        // This side's own edge of the field (side 0 arrives from -z).
        Vector3 Rear => new Vector3(0f, 0f, Side == 0 ? -4300f : 4300f);

        // Rough fighting value on the field: cost, scaled by what is left of it.
        static float Value(Ship s) => s.Def.Cost * (0.35f + 0.65f * s.Hull / s.Def.HullHp);

        Ship PickTarget(World world, Ship s, Ship station, Ship enemyStation, float ratio, bool avoidNebula)
        {
            bool escort = s.Def.Class <= HullClass.Frigate;
            // Defending: stay under the station's guns unless the enemy is much weaker.
            float leash = station != null && ratio < 1.6f ? station.MaxGunRange + 700f : float.MaxValue;

            Ship best = null;
            float bestScore = float.MaxValue;
            void Consider(Ship e)
            {
                if (station != null && Vector3.Distance(e.Position, station.Position) > leash) return;
                if (avoidNebula && e.InNebula) return;
                float d = Vector3.Distance(s.Position, e.Position);
                float score = d / System.MathF.Max(600f, s.MaxGunRange);
                bool small = e.Def.Class <= HullClass.Frigate;
                if (escort && small) score *= 0.45f;             // escorts screen escorts
                if (!escort && !small) score *= 0.6f;            // the line fights the line
                if (e.Def.Class == HullClass.Transport) score *= 0.5f; // troop ships are the prize
                if (e.Def.Class == HullClass.Station)
                {
                    // Go for the station once its escorts are thin, or when nothing else is near.
                    int defenders = 0;
                    foreach (var o in _enemy)
                        if (o.Def.Class != HullClass.Transport && Vector3.Distance(o.Position, e.Position) < e.MaxGunRange) defenders++;
                    score *= defenders <= 2 ? (escort ? 1.2f : 0.5f) : 2.5f;
                }
                score *= 0.6f + 0.4f * e.Hull / e.Def.HullHp; // finish off the hurt
                if (e.Id == s.Order.OtherShip) score *= 0.75f;   // don't flip-flop
                if (score < bestScore)
                {
                    bestScore = score;
                    best = e;
                }
            }
            foreach (var e in _enemy) Consider(e);
            if (enemyStation != null) Consider(enemyStation);
            return best;
        }

        // `group`: the fleet goes in together, the fastest waiting for the slowest.
        static void Engage(World world, Ship s, Ship target, int group)
        {
            if (s.Order.OtherShip == target.Id && (s.Order.Kind == OrderKind.Attack || s.Order.Kind == OrderKind.AttackRun)) return;
            if (s.Def.Class <= HullClass.Frigate && target.Def.Class != HullClass.Station)
            {
                var o = ShipOrder.AttackRun(target.Id);
                o.PassDistance = s.Def.Class == HullClass.Corvette ? world.Rng.Range(150f, 350f) : world.Rng.Range(400f, 700f);
                o.RunOutDistance = world.Rng.Range(1400f, 2200f);
                o.Group = group;
                world.Issue(s.Id, o);
            }
            else
            {
                var o = ShipOrder.Attack(target.Id);
                o.Group = group;
                world.Issue(s.Id, o);
            }
        }

        static void Move(World world, Ship s, Vector3 to, int group = -1)
        {
            if (s.Order.Kind == OrderKind.MoveTo && s.Order.Group == group && Vector3.Distance(s.Order.Point, to) < 250f) return;
            var o = ShipOrder.MoveTo(to);
            o.Group = group;
            world.Issue(s.Id, o);
        }

        // Troop ships keep out of it: under the station, or at the back of the field; with no
        // warships left to cover them, they run for the edge.
        void Transport(World world, Ship s, Ship station, Vector3 enemyCentre, bool covered)
        {
            if (station != null)
            {
                Vector3 behind = station.Position + Flat(station.Position - enemyCentre) * 0.15f;
                if (Vector3.Distance(s.Position, behind) > 900f) Move(world, s, behind);
                return;
            }
            Vector3 to = covered ? Rear + Offset(s, 200f) : Rear * 3f + Offset(s, 300f);
            if (Vector3.Distance(Flat(s.Position), Flat(to)) > 400f) Move(world, s, to);
        }

        static void UseAbilities(World world, Ship s)
        {
            if (s.ActiveAbility >= 0) return;
            float shield = s.Def.ShieldHp > 0f ? s.Shield / s.Def.ShieldHp : 0f;
            bool underFire = s.ShieldDelay > s.Def.ShieldRegenDelay - 1.5f;
            if (underFire && shield < 0.3f && !s.InNebula && s.CanUse(Ability.Shields))
            {
                world.Use(s.Id, Ability.Shields);
                return;
            }
            var target = world.ShipById(s.Order.OtherShip);
            bool hurt = Hurt(s);
            if (s.CanUse(Ability.Engines))
            {
                bool closing = target != null && target.Alive && Vector3.Distance(s.Position, target.Position) > s.MaxGunRange * 1.6f;
                bool fleeing = hurt && s.Order.Kind == OrderKind.MoveTo;
                if (closing || fleeing)
                {
                    world.Use(s.Id, Ability.Engines);
                    return;
                }
            }
            if (target != null && target.Alive && shield > 0.6f && !hurt && s.CanUse(Ability.Weapons)
                && Vector3.Distance(s.Position, target.Position) < s.MaxGunRange * 0.9f)
                world.Use(s.Id, Ability.Weapons);
        }

        static bool ShieldHeavy(List<Ship> ships)
        {
            float shield = 0f, hull = 0f;
            foreach (var s in ships)
            {
                shield += s.Def.ShieldHp;
                hull += s.Def.HullHp;
            }
            return shield > hull * 0.3f;
        }

        static bool AnyIn(World world, List<Ship> ships)
        {
            foreach (var s in ships) if (s.InNebula) return true;
            return false;
        }

        // A nebula nearer to us than to them.
        static bool NearestNebula(World world, Vector3 own, Vector3 enemy, out Vector3 centre)
        {
            centre = default;
            float best = float.MaxValue;
            foreach (var o in world.Obstacles)
            {
                if (o.Kind != ObstacleKind.Nebula) continue;
                float dOwn = Vector3.Distance(o.Position, own), dEnemy = Vector3.Distance(o.Position, enemy);
                if (dOwn > dEnemy || dOwn > 6000f || dOwn >= best) continue;
                best = dOwn;
                centre = o.Position;
            }
            return best < float.MaxValue;
        }

        static Ship Nearest(List<Ship> ships, Ship from, float within)
        {
            Ship best = null;
            float bestD = within * within;
            foreach (var s in ships)
            {
                float d = Vector3.DistanceSquared(s.Position, from.Position);
                if (d < bestD) { bestD = d; best = s; }
            }
            return best;
        }

        public static Ship Nearest(World world, Ship from)
        {
            Ship best = null;
            float bestD = float.MaxValue;
            foreach (var s in world.Ships)
            {
                if (!s.Alive || s.Side == from.Side) continue;
                float d = Vector3.DistanceSquared(s.Position, from.Position);
                if (d < bestD)
                {
                    bestD = d;
                    best = s;
                }
            }
            return best;
        }

        // A fixed slot per ship around a point, so a group spreads instead of stacking.
        static Vector3 Offset(Ship s, float spacing)
        {
            int k = s.Id % 12;
            float a = k * 2.39996f;   // golden angle
            float r = spacing * SimMath.Sqrt(k + 1);
            return new Vector3(SimMath.Sin(a) * r, 0f, SimMath.Cos(a) * r);
        }

        static Vector3 Centre(List<Ship> ships)
        {
            Vector3 c = Vector3.Zero;
            foreach (var s in ships) c += s.Position;
            return ships.Count > 0 ? c / ships.Count : c;
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.X, 0f, v.Z);
    }
}
