using System.Numerics;

namespace GalacticRupture.Sim
{
    public sealed partial class World
    {
        const float RetargetInterval = 0.5f;
        const float SubsystemHitRadius = 18f;

        struct Solution
        {
            public float Yaw;     // offset from the hardpoint's rest direction, clamped to the arc
            public float Pitch;   // clamped to the elevation limits
            public float Distance;
            public bool Bears;    // inside arc, elevation and range
        }

        public Vector3 MountPivot(Ship ship, Mount m) =>
            ship.LocalToWorld(m.Hardpoint.LocalPosition + new Vector3(0f, m.Def.PivotHeight, 0f));

        public Vector3 BarrelDirection(Ship ship, Mount m) =>
            SimMath.YawPitchToDir(ship.Yaw + m.Hardpoint.BaseYawDeg * SimMath.Deg2Rad + m.Yaw, m.Pitch);

        Solution Solve(Ship ship, Mount m, Ship target)
        {
            var def = m.Def;
            var hp = m.Hardpoint;
            Vector3 pivot = MountPivot(ship, m);
            Vector3 relPos = target.Position - pivot;
            Vector3 relVel = target.Velocity - ship.Velocity;
            Vector3 aim = SimMath.InterceptTime(relPos, relVel, def.ProjectileSpeed, out float t)
                ? relPos + relVel * t
                : relPos;

            Vector3 local = SimMath.InverseRotateYaw(aim, ship.Yaw);
            float yaw = SimMath.WrapAngle(SimMath.Atan2(local.X, local.Z) - hp.BaseYawDeg * SimMath.Deg2Rad);
            float pitch = SimMath.Atan2(local.Y, SimMath.Sqrt(local.X * local.X + local.Z * local.Z));

            float half = hp.YawHalfArcDeg * SimMath.Deg2Rad;
            float pMin = def.PitchMinDeg * SimMath.Deg2Rad, pMax = def.PitchMaxDeg * SimMath.Deg2Rad;
            float dist = aim.Length();

            var s = new Solution
            {
                Distance = dist,
                Bears = System.MathF.Abs(yaw) <= half && pitch >= pMin && pitch <= pMax && dist <= def.Range,
                Yaw = hp.YawHalfArcDeg >= 180f ? yaw : SimMath.Clamp(yaw, -half, half),
                Pitch = SimMath.Clamp(pitch, pMin, pMax),
            };
            return s;
        }

        void UpdateMount(Ship ship, Mount m)
        {
            var def = m.Def;
            m.Cooldown -= Dt;
            m.RetargetTimer -= Dt;

            FireQueued(ship, m);

            Ship target = ShipById(m.Target);
            if (target != null && (!target.Alive || !Solve(ship, m, target).Bears))
            {
                target = null;
                m.Target = -1;
                m.RetargetTimer = 0f;
            }
            if (m.RetargetTimer <= 0f)
            {
                m.RetargetTimer = RetargetInterval;
                target = ChooseTarget(ship, m, target);
                m.Target = target?.Id ?? -1;
            }

            float wantYaw = 0f, wantPitch = 0f;
            bool bears = false;
            if (target != null)
            {
                var sol = Solve(ship, m, target);
                wantYaw = sol.Yaw;
                wantPitch = sol.Pitch;
                bears = sol.Bears;
            }

            float yawStep = def.YawRateDeg * SimMath.Deg2Rad * Dt;
            m.Yaw = m.Hardpoint.YawHalfArcDeg >= 180f
                ? SimMath.MoveTowardsAngle(m.Yaw, wantYaw, yawStep)
                : SimMath.MoveTowards(m.Yaw, wantYaw, yawStep);
            m.Pitch = SimMath.MoveTowards(m.Pitch, wantPitch, def.PitchRateDeg * SimMath.Deg2Rad * Dt);

            float dYaw = SimMath.WrapAngle(wantYaw - m.Yaw) * SimMath.Cos(m.Pitch);
            float dPitch = wantPitch - m.Pitch;
            float err = SimMath.Sqrt(dYaw * dYaw + dPitch * dPitch);
            m.OnTarget = bears && err <= def.AimToleranceDeg * SimMath.Deg2Rad;

            if (m.OnTarget && m.Cooldown <= 0f && m.PendingRounds == 0)
            {
                m.Cooldown = def.Reload * ship.ReloadScale;
                m.PendingRounds = def.Barrels;
                m.StaggerTimer = 0f;
                FireQueued(ship, m);
            }
        }

        // Prefers targets the guns can reach soon and the mount class is meant for; keeps its
        // current target unless another is clearly better.
        Ship ChooseTarget(Ship ship, Mount m, Ship current)
        {
            Ship best = null;
            float bestScore = float.MaxValue;
            Vector3 pivot = MountPivot(ship, m);
            foreach (int id in ship.Enemies)
            {
                var other = Ships[id];
                if (!other.Alive) continue;
                float reach = m.Def.Range + other.Def.HalfLength;
                if (Vector3.DistanceSquared(pivot, other.Position) > reach * reach) continue;
                var sol = Solve(ship, m, other);
                if (!sol.Bears) continue;

                float swing = System.MathF.Abs(SimMath.WrapAngle(sol.Yaw - m.Yaw)) + System.MathF.Abs(sol.Pitch - m.Pitch);
                float score = sol.Distance / m.Def.Range + swing / SimMath.Pi;
                bool small = other.Def.Class <= HullClass.Frigate;
                if (m.Def.Class == MountClass.PointDefence && small) score *= 0.5f;
                if (m.Def.Class == MountClass.Line && !small) score *= 0.6f;
                if (other == current) score *= 0.8f;
                if (other.Id == ship.FocusTarget) score *= 0.05f;

                if (score < bestScore)
                {
                    bestScore = score;
                    best = other;
                }
            }
            return best;
        }

        void FireQueued(Ship ship, Mount m)
        {
            if (m.PendingRounds <= 0) return;
            m.StaggerTimer -= Dt;
            if (m.StaggerTimer > 0f) return;

            var def = m.Def;
            m.StaggerTimer = def.BarrelStagger;
            m.PendingRounds--;

            float spread = def.SpreadDeg * SimMath.Deg2Rad;
            float yaw = ship.Yaw + m.Hardpoint.BaseYawDeg * SimMath.Deg2Rad + m.Yaw + _rng.Range(-spread, spread);
            float pitch = m.Pitch + _rng.Range(-spread, spread);
            Vector3 dir = SimMath.YawPitchToDir(yaw, pitch);
            Vector3 muzzle = MountPivot(ship, m) + BarrelDirection(ship, m) * def.MuzzleLength;

            Projectiles.Add(new Projectile
            {
                Position = muzzle,
                PrevPosition = muzzle,
                Velocity = dir * def.ProjectileSpeed + ship.Velocity,
                Life = def.Range * 1.15f / def.ProjectileSpeed,
                Damage = def.Damage,
                Side = ship.Side,
                Shooter = ship.Id,
                Channel = def.Channel,
                Class = def.Class,
            });

            int barrel = m.NextBarrel;
            m.NextBarrel = (m.NextBarrel + 1) % System.Math.Max(def.Barrels, 1);
            Events.Add(new SimEvent
            {
                Kind = SimEventKind.Fired, Ship = ship.Id, Mount = m.Index, Barrel = barrel,
                Position = muzzle, Direction = dir,
            });
        }

        void UpdateProjectiles()
        {
            int write = 0;
            for (int i = 0; i < Projectiles.Count; i++)
            {
                var p = Projectiles[i];
                p.PrevPosition = p.Position;
                p.Position += p.Velocity * Dt;
                p.Life -= Dt;

                bool spent = p.Life <= 0f;
                if (!spent)
                {
                    Vector3 mid = (p.PrevPosition + p.Position) * 0.5f;
                    float segHalf = (p.Position - p.PrevPosition).Length() * 0.5f;
                    _query.Clear();
                    Grid.Query(mid, 200f + segHalf, _query);
                    foreach (int id in _query)
                    {
                        var ship = Ships[id];
                        if (!ship.Alive || ship.Side == p.Side) continue;
                        float reach = ship.Def.HalfLength + ship.Def.Radius + segHalf;
                        if (Vector3.DistanceSquared(mid, ship.Position) > reach * reach) continue;

                        ship.KeelSegment(out var bow, out var stern);
                        float r = ship.Def.Radius;
                        if (SimMath.SegmentSegmentDistSq(p.PrevPosition, p.Position, bow, stern, out var hit) <= r * r)
                        {
                            ApplyHit(ship, hit, p.Damage, p.Channel);
                            spent = true;
                            break;
                        }
                    }
                }

                if (!spent) Projectiles[write++] = p;
            }
            Projectiles.RemoveRange(write, Projectiles.Count - write);
        }

        public void ApplyHit(Ship ship, Vector3 point, float damage, DamageChannel channel)
        {
            var def = ship.Def;
            ship.ShieldDelay = def.ShieldRegenDelay;
            float hullDamage = damage;

            if (ship.Shield > 0f && channel != DamageChannel.Disruptive)
            {
                // Energy burns shields efficiently; ballistic partly punches through (IDEAS.md §5.1).
                float toShield = channel == DamageChannel.Energy ? damage * 1.25f : damage * 0.8f;
                float bleed = channel == DamageChannel.Ballistic ? damage * 0.2f : 0f;
                float absorbed = System.MathF.Min(ship.Shield, toShield);
                ship.Shield -= absorbed;
                hullDamage = bleed + (toShield - absorbed);
                Events.Add(new SimEvent { Kind = SimEventKind.ShieldHit, Ship = ship.Id, Position = point, Value = absorbed });
                if (ship.Shield <= 0f)
                    Events.Add(new SimEvent { Kind = SimEventKind.ShieldsDown, Ship = ship.Id, Position = point });
            }

            if (hullDamage <= 0f) return;

            ship.Hull -= hullDamage;
            Events.Add(new SimEvent { Kind = SimEventKind.HullHit, Ship = ship.Id, Position = point, Value = hullDamage });

            Mount nearest = null;
            float nearestSq = SubsystemHitRadius * SubsystemHitRadius;
            foreach (var m in ship.Mounts)
            {
                if (!m.Alive) continue;
                float d = Vector3.DistanceSquared(MountPivot(ship, m), point);
                if (d < nearestSq)
                {
                    nearestSq = d;
                    nearest = m;
                }
            }
            if (nearest != null)
            {
                nearest.Hp -= hullDamage;
                if (nearest.Hp <= 0f)
                {
                    nearest.Alive = false;
                    nearest.Target = -1;
                    nearest.PendingRounds = 0;
                    Events.Add(new SimEvent
                    {
                        Kind = SimEventKind.MountDestroyed, Ship = ship.Id, Mount = nearest.Index,
                        Position = MountPivot(ship, nearest),
                    });
                }
            }

            if (ship.Hull <= 0f)
            {
                ship.Hull = 0f;
                ship.Alive = false;
                Events.Add(new SimEvent { Kind = SimEventKind.ShipDestroyed, Ship = ship.Id, Position = ship.Position });
            }
        }
    }
}
