using System.Numerics;

namespace GalacticRupture.Sim
{
    public sealed partial class World
    {
        void Pilot(Ship ship)
        {
            var def = ship.Def;
            float maxSpeed = def.MaxSpeed * ship.CruiseSpeedFraction;
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
                    float stopping = ship.Speed * ship.Speed / (2f * def.Acceleration) + 20f;
                    if (d < stopping) desiredSpeed = 0f;
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
                    desiredSpeed = SimMath.Clamp(leader.Speed + along * 0.15f, 0f, def.MaxSpeed);
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

            Vector3 to = Flat(goal - ship.Position);
            float prevYaw = ship.Yaw;
            if (to.LengthSquared() > 1f)
            {
                float desiredYaw = SimMath.Atan2(to.X, to.Z);
                float maxTurn = def.TurnRateDeg * SimMath.Deg2Rad * Dt;
                ship.Yaw = SimMath.MoveTowardsAngle(ship.Yaw, desiredYaw, maxTurn);
            }
            ship.TurnRate = SimMath.WrapAngle(ship.Yaw - prevYaw) / Dt;

            ship.Speed = SimMath.MoveTowards(ship.Speed, desiredSpeed, def.Acceleration * Dt);

            Vector3 p = ship.Position + ship.Forward * (ship.Speed * Dt);
            p.Y = SimMath.MoveTowards(p.Y, ship.TargetAltitude, def.ClimbRate * Dt);
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
                    ship.TargetAltitude = target.Position.Y + _rng.Range(-250f, 250f);
                }
                // Aim to pass abeam of the target, not ram it.
                Vector3 dir = dist > 1f ? toTarget / dist : ship.Forward;
                Vector3 perp = new Vector3(dir.Z, 0f, -dir.X) * ((ship.Id & 1) == 0 ? 1f : -1f);
                float offset = order.PassDistance + target.Def.Radius;
                return target.Position + perp * offset;
            }

            if (dist > order.RunOutDistance || Flat(ship.RunExitPoint - ship.Position).Length() < 150f)
            {
                ship.RunOutbound = false;
                ship.TargetAltitude = target.Position.Y + _rng.Range(-200f, 200f);
            }
            desiredSpeed = ship.Def.MaxSpeed * ship.CruiseSpeedFraction;
            return ship.RunExitPoint;
        }

        // Close to three quarters of gun range, then turn the beam to the target so every turret
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
            ship.TargetAltitude = target.Position.Y;
            Vector3 to = Flat(target.Position - ship.Position);
            float dist = to.Length();
            float engage = System.MathF.Max(ship.MaxGunRange * 0.75f, 300f);
            if (dist > engage || dist < 1f) return target.Position;

            Vector3 dir = to / dist;
            Vector3 port = new Vector3(-dir.Z, 0f, dir.X);
            // Pick whichever beam heading needs less turning.
            Vector3 beam = Vector3.Dot(port, ship.Forward) >= 0f ? port : -port;
            desiredSpeed = ship.Def.MaxSpeed * 0.25f;
            return ship.Position + beam * 500f;
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.X, 0f, v.Z);
    }
}
