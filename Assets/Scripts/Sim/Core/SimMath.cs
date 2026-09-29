using System;
using System.Numerics;

namespace GalacticRupture.Sim
{
    // Every transcendental the sim uses goes through here (IDEAS.md §8.7), so a table-based
    // version can replace MathF if bit-identical results across platforms ever matter.
    public static class SimMath
    {
        public const float Pi = MathF.PI;
        public const float TwoPi = MathF.PI * 2f;
        public const float Deg2Rad = MathF.PI / 180f;

        public static float Sin(float x) => MathF.Sin(x);
        public static float Cos(float x) => MathF.Cos(x);
        public static float Atan2(float y, float x) => MathF.Atan2(y, x);
        public static float Sqrt(float x) => MathF.Sqrt(x);

        public static float Clamp(float v, float min, float max) => v < min ? min : v > max ? max : v;

        // Wraps an angle into (-π, π].
        public static float WrapAngle(float a)
        {
            while (a > Pi) a -= TwoPi;
            while (a <= -Pi) a += TwoPi;
            return a;
        }

        // Moves `current` toward `target` by at most `maxDelta`.
        public static float MoveTowards(float current, float target, float maxDelta)
        {
            float d = target - current;
            if (d > maxDelta) return current + maxDelta;
            if (d < -maxDelta) return current - maxDelta;
            return target;
        }

        // Moves an angle toward a target angle the short way round.
        public static float MoveTowardsAngle(float current, float target, float maxDelta)
        {
            float d = WrapAngle(target - current);
            if (d > maxDelta) d = maxDelta;
            else if (d < -maxDelta) d = -maxDelta;
            return WrapAngle(current + d);
        }

        // Heading in the XZ plane, 0 = +Z (forward), positive turns toward +X (starboard).
        public static Vector3 HeadingToDir(float yaw) => new Vector3(Sin(yaw), 0f, Cos(yaw));

        // Rotates a ship-local vector (x starboard, y up, z forward) into world space by yaw.
        public static Vector3 RotateYaw(Vector3 v, float yaw)
        {
            float s = Sin(yaw), c = Cos(yaw);
            return new Vector3(v.X * c + v.Z * s, v.Y, -v.X * s + v.Z * c);
        }

        public static Vector3 InverseRotateYaw(Vector3 v, float yaw) => RotateYaw(v, -yaw);

        // Direction from a yaw (about +Y, from +Z) and a pitch (up positive).
        public static Vector3 YawPitchToDir(float yaw, float pitch)
        {
            float cp = Cos(pitch);
            return new Vector3(Sin(yaw) * cp, Sin(pitch), Cos(yaw) * cp);
        }

        // Time for a projectile of `speed` from the origin to meet a target at `relPos` moving
        // at `relVel`. Returns false if it can never catch up.
        public static bool InterceptTime(Vector3 relPos, Vector3 relVel, float speed, out float t)
        {
            float a = Vector3.Dot(relVel, relVel) - speed * speed;
            float b = 2f * Vector3.Dot(relPos, relVel);
            float c = Vector3.Dot(relPos, relPos);
            t = 0f;
            if (MathF.Abs(a) < 1e-6f)
            {
                if (MathF.Abs(b) < 1e-6f) return false;
                t = -c / b;
                return t > 0f;
            }
            float disc = b * b - 4f * a * c;
            if (disc < 0f) return false;
            float sq = Sqrt(disc);
            float t1 = (-b - sq) / (2f * a);
            float t2 = (-b + sq) / (2f * a);
            t = t1 > 0f && (t1 < t2 || t2 <= 0f) ? t1 : t2;
            return t > 0f;
        }

        // Squared distance between segments p1-q1 and p2-q2, with the closest point on the first.
        public static float SegmentSegmentDistSq(Vector3 p1, Vector3 q1, Vector3 p2, Vector3 q2, out Vector3 c1)
        {
            Vector3 d1 = q1 - p1, d2 = q2 - p2, r = p1 - p2;
            float a = Vector3.Dot(d1, d1), e = Vector3.Dot(d2, d2), f = Vector3.Dot(d2, r);
            float s, t;
            if (a <= 1e-9f && e <= 1e-9f)
            {
                c1 = p1;
                return Vector3.Dot(r, r);
            }
            if (a <= 1e-9f)
            {
                s = 0f;
                t = Clamp(f / e, 0f, 1f);
            }
            else
            {
                float c = Vector3.Dot(d1, r);
                if (e <= 1e-9f)
                {
                    t = 0f;
                    s = Clamp(-c / a, 0f, 1f);
                }
                else
                {
                    float b = Vector3.Dot(d1, d2);
                    float denom = a * e - b * b;
                    s = denom > 1e-9f ? Clamp((b * f - c * e) / denom, 0f, 1f) : 0f;
                    t = (b * s + f) / e;
                    if (t < 0f) { t = 0f; s = Clamp(-c / a, 0f, 1f); }
                    else if (t > 1f) { t = 1f; s = Clamp((b - c) / a, 0f, 1f); }
                }
            }
            c1 = p1 + d1 * s;
            Vector3 c2 = p2 + d2 * t;
            return Vector3.DistanceSquared(c1, c2);
        }
    }
}
