using System.Collections.Generic;
using System.Numerics;

namespace GalacticRupture.Sim
{
    public sealed partial class World
    {
        // Ships flying one order together (ShipOrder.Group) keep together: each tick, the time
        // the slowest of them needs to get where it is going, and everyone else eases off to
        // arrive with it.
        readonly Dictionary<int, float> _groupEta = new Dictionary<int, float>();
        int _groups;

        public int NewGroup() => ++_groups;

        void GroupPace()
        {
            _groupEta.Clear();
            foreach (var ship in Ships)
            {
                if (!ship.Alive || ship.Order.Group < 0 || ship.Def.MaxSpeed <= 0f) continue;
                float eta = Approach(ship) / EffectiveSpeed(ship);
                int g = ship.Order.Group;
                if (!_groupEta.TryGetValue(g, out float e) || eta > e) _groupEta[g] = eta;
            }
        }

        float EffectiveSpeed(Ship ship) => ship.TopSpeed * (ship.Using(Ability.Engines) ? 1.7f : 1f);

        // How far the ship still has to go before its order has it where it wants to be: the
        // part of the job a group does together. Zero once there.
        float Approach(Ship ship)
        {
            var o = ship.Order;
            switch (o.Kind)
            {
                case OrderKind.MoveTo:
                    return Flat(o.Point - ship.Position).Length();
                case OrderKind.Attack:
                case OrderKind.AttackRun:
                {
                    var t = ShipById(o.OtherShip);
                    if (t == null || !t.Alive || (o.Kind == OrderKind.AttackRun && ship.RunOutbound)) return 0f;
                    float d = Flat(t.Position - ship.Position).Length();
                    // Escorts hold with the line until the target is nearly in reach, then go in.
                    float until = o.Kind == OrderKind.Attack ? EngageRange(ship) : ship.MaxGunRange * 1.4f;
                    return System.MathF.Max(0f, d - until);
                }
            }
            return 0f;
        }

        // A ship ahead of the rest of its group slows to let the others come up.
        float GroupSpeed(Ship ship, float desired)
        {
            if (ship.Order.Group < 0 || !_groupEta.TryGetValue(ship.Order.Group, out float eta) || eta < 1f) return desired;
            float r = Approach(ship);
            if (r <= 0f) return desired;
            return System.MathF.Min(desired, System.MathF.Max(r / eta, ship.TopSpeed * 0.3f));
        }

        static float EngageRange(Ship ship) => System.MathF.Max(ship.MaxGunRange * 0.8f, 300f);

        void Pilot(Ship ship)
        {
            var def = ship.Def;
            if (def.MaxSpeed <= 0f)
            {
                ship.Speed = 0f;
                ship.Velocity = Vector3.Zero;
                ship.TurnRate = 0f;
                return;
            }
            bool boost = ship.Using(Ability.Engines);
            float maxSpeed = EffectiveSpeed(ship);
            float accel = def.Acceleration * Pace.Movement * (boost ? 2f : 1f);
            Vector3 goal = ship.Position + ship.Forward * 100f;
            float desiredSpeed = maxSpeed;

            var order = ship.Order;
            switch (order.Kind)
            {
                case OrderKind.Hold:
                    desiredSpeed = 0f;
                    break;

                case OrderKind.MoveTo:
                {
                    goal = order.Point;
                    ship.TargetAltitude = order.Point.Y;
                    float d = Flat(order.Point - ship.Position).Length();
                    float stopping = ship.Speed * ship.Speed / (2f * accel) + 20f;
                    if (d < stopping)
                    {
                        desiredSpeed = 0f;
                        // Arrived: come round to the heading the formation was given.
                        if (!float.IsNaN(order.FacingYaw)) goal = ship.Position + SimMath.HeadingToDir(order.FacingYaw) * 500f;
                    }
                    break;
                }

                case OrderKind.Orbit:
                {
                    Vector3 rel = Flat(ship.Position - order.Point);
                    float theta = SimMath.Atan2(rel.X, rel.Z);
                    float lead = 300f / System.MathF.Max(order.Radius, 1f);
                    theta += order.Clockwise ? lead : -lead;
                    goal = order.Point + new Vector3(SimMath.Sin(theta), 0f, SimMath.Cos(theta)) * order.Radius;
                    ship.TargetAltitude = order.Point.Y;
                    break;
                }

                case OrderKind.Escort:
                {
                    var leader = ShipById(order.OtherShip);
                    if (leader == null || !leader.Alive)
                    {
                        ship.Order = ShipOrder.Hold();
                        break;
                    }
                    Vector3 station = leader.LocalToWorld(order.Offset);
                    Vector3 lf = leader.Forward;
                    goal = station + lf * 400f;
                    float along = Vector3.Dot(station - ship.Position, lf);
                    desiredSpeed = SimMath.Clamp(leader.Speed + along * 0.15f, 0f, maxSpeed);
                    ship.TargetAltitude = station.Y;
                    break;
                }

                case OrderKind.AttackRun:
                    goal = AttackRunGoal(ship, order, ref desiredSpeed);
                    break;

                case OrderKind.Attack:
                    goal = AttackGoal(ship, order, ref desiredSpeed);
                    break;

                case OrderKind.Face:
                    goal = order.Point;
                    desiredSpeed = 0f;
                    break;
            }
            desiredSpeed = GroupSpeed(ship, desiredSpeed);

            if (order.Kind != OrderKind.Face && order.Kind != OrderKind.Hold) goal = AvoidRocks(ship, goal);
            Vector3 to = Flat(goal - ship.Position);
            float prevYaw = ship.Yaw;
            if (to.LengthSquared() > 1f)
            {
                float desiredYaw = SimMath.Atan2(to.X, to.Z);
                float maxTurn = def.TurnRateDeg * Pace.Turn * (boost ? 1.3f : 1f) * SimMath.Deg2Rad * Dt;
                ship.Yaw = SimMath.MoveTowardsAngle(ship.Yaw, desiredYaw, maxTurn);
            }
            ship.TurnRate = SimMath.WrapAngle(ship.Yaw - prevYaw) / Dt;

            ship.Speed = SimMath.MoveTowards(ship.Speed, System.MathF.Min(desiredSpeed, maxSpeed), accel * Dt);

            // Level flight: ships change height only when an order sends them to another plane.
            Vector3 p = ship.Position + ship.Forward * (ship.Speed * Dt);
            p.Y = SimMath.MoveTowards(p.Y, ship.TargetAltitude, def.ClimbRate * Pace.Movement * Dt);
            ship.Velocity = (p - ship.Position) / Dt;
            ship.Position = p;
        }

        // Inbound at the target, offset to one side; past it, break away to a point far out,
        // then come round for another pass.
        Vector3 AttackRunGoal(Ship ship, ShipOrder order, ref float desiredSpeed)
        {
            var target = ShipById(order.OtherShip);
            if (target == null || !target.Alive)
            {
                ship.Order = ShipOrder.Hold();
                return ship.Position;
            }

            Vector3 toTarget = Flat(target.Position - ship.Position);
            float dist = toTarget.Length();

            if (!ship.RunOutbound)
            {
                if (dist < order.PassDistance + target.Def.HalfLength * 0.5f)
                {
                    ship.RunOutbound = true;
                    Vector3 side = new Vector3(ship.Forward.Z, 0f, -ship.Forward.X) * (_rng.NextFloat() < 0.5f ? -1f : 1f);
                    ship.RunExitPoint = ship.Position + ship.Forward * order.RunOutDistance + side * _rng.Range(0f, 900f);
                }
                // Aim to pass abeam of the target, not ram it.
                Vector3 dir = dist > 1f ? toTarget / dist : ship.Forward;
                Vector3 perp = new Vector3(dir.Z, 0f, -dir.X) * ((ship.Id & 1) == 0 ? 1f : -1f);
                float offset = order.PassDistance + target.Def.Radius;
                return target.Position + perp * offset;
            }

            if (dist > order.RunOutDistance || Flat(ship.RunExitPoint - ship.Position).Length() < 150f)
                ship.RunOutbound = false;
            return ship.RunExitPoint;
        }

        // Close to four fifths of gun range, then turn the beam to the target so every turret
        // with an arc on that side bears, and ease off to a slow crawl.
        Vector3 AttackGoal(Ship ship, ShipOrder order, ref float desiredSpeed)
        {
            var target = ShipById(order.OtherShip);
            if (target == null || !target.Alive)
            {
                ship.Order = ShipOrder.Hold();
                ship.FocusTarget = -1;
                return ship.Position;
            }
            Vector3 to = Flat(target.Position - ship.Position);
            float dist = to.Length();
            float engage = EngageRange(ship);
            // Inside the enemy's reach, close at battle speed rather than flat out.
            if (dist > engage || dist < 1f)
            {
                if (dist < ship.MaxGunRange * 1.05f) desiredSpeed = ship.TopSpeed * 0.7f;
                return target.Position;
            }
            // Rock in the way: keep moving (the helm steers round it) until the guns can see.
            if (Obstacles.Count > 0 && Occluded(ship.Position, target.Position))
            {
                desiredSpeed = ship.TopSpeed * 0.6f;
                return target.Position;
            }

            Vector3 dir = to / dist;
            Vector3 port = new Vector3(-dir.Z, 0f, dir.X);
            // Pick whichever beam heading needs less turning.
            Vector3 beam = Vector3.Dot(port, ship.Forward) >= 0f ? port : -port;
            desiredSpeed = ship.TopSpeed * 0.25f;
            return ship.Position + beam * 500f;
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.X, 0f, v.Z);
    }
}
