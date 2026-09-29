using System.Collections.Generic;
using System.Numerics;

namespace GalacticRupture.Sim
{
    public enum OrderKind
    {
        Hold,
        MoveTo,
        Orbit,     // circle a point at a radius and altitude
        Escort,    // keep station on another ship at a local offset
        AttackRun, // repeated passes at a target: inbound, break away, come round again
        Attack,    // close to gun range, then hold the broadside and focus fire
        Face,      // stop and turn to face a point
    }

    public sealed class ShipOrder
    {
        public OrderKind Kind;
        public Vector3 Point;
        public float Radius;
        public bool Clockwise;
        public int OtherShip = -1;
        public Vector3 Offset;       // Escort: leader-local offset
        public float PassDistance = 300f;
        public float RunOutDistance = 2200f;

        public static ShipOrder Hold() => new ShipOrder { Kind = OrderKind.Hold };
        public static ShipOrder MoveTo(Vector3 p) => new ShipOrder { Kind = OrderKind.MoveTo, Point = p };

        public static ShipOrder Orbit(Vector3 centre, float radius, bool clockwise) =>
            new ShipOrder { Kind = OrderKind.Orbit, Point = centre, Radius = radius, Clockwise = clockwise };

        public static ShipOrder Escort(int leader, Vector3 offset) =>
            new ShipOrder { Kind = OrderKind.Escort, OtherShip = leader, Offset = offset };

        public static ShipOrder AttackRun(int target) =>
            new ShipOrder { Kind = OrderKind.AttackRun, OtherShip = target };

        public static ShipOrder Attack(int target) =>
            new ShipOrder { Kind = OrderKind.Attack, OtherShip = target };

        public static ShipOrder Face(Vector3 point) =>
            new ShipOrder { Kind = OrderKind.Face, Point = point };
    }

    public sealed class Mount
    {
        public readonly HardpointDef Hardpoint;
        public readonly int Index;

        public float Yaw;            // radians, offset from the hardpoint's rest direction
        public float Pitch;          // radians, up positive
        public float PrevYaw, PrevPitch;
        public float Hp;
        public bool Alive = true;

        public int Target = -1;
        public float Cooldown;
        public float RetargetTimer;
        public bool OnTarget;        // barrels within tolerance of the firing solution

        internal int PendingRounds;
        internal float StaggerTimer;
        internal int NextBarrel;

        public Mount(HardpointDef hardpoint, int index)
        {
            Hardpoint = hardpoint;
            Index = index;
            Hp = hardpoint.Mount.Hp;
        }

        public MountDef Def => Hardpoint.Mount;
    }

    public sealed class Ship
    {
        public readonly int Id;
        public readonly int Side;
        public readonly HullDef Def;
        public readonly List<Mount> Mounts = new List<Mount>();

        public Vector3 Position;
        public float Yaw;            // radians, 0 = +Z
        public float Speed;
        public float TargetAltitude;
        public float TurnRate;       // current yaw rate, radians/s; the view banks from it
        public Vector3 Velocity;

        public Vector3 PrevPosition;
        public float PrevYaw;

        public float Hull;
        public float Shield;
        public float ShieldDelay;
        public bool Alive = true;

        public ShipOrder Order = ShipOrder.Hold();
        public int FocusTarget = -1;     // guns prefer this ship when it bears
        public float CruiseSpeedFraction = 1f;

        internal bool RunOutbound;
        internal Vector3 RunExitPoint;

        // Enemies near enough for any of this ship's guns, refreshed a few times a second.
        internal readonly List<int> Enemies = new List<int>();
        internal float ScanTimer;
        public readonly float MaxGunRange;

        public Ship(int id, int side, HullDef def, Vector3 position, float yaw)
        {
            Id = id;
            Side = side;
            Def = def;
            Position = PrevPosition = position;
            Yaw = PrevYaw = yaw;
            TargetAltitude = position.Y;
            Hull = def.HullHp;
            Shield = def.ShieldHp;
            for (int i = 0; i < def.Hardpoints.Count; i++)
            {
                var m = new Mount(def.Hardpoints[i], i);
                // Spread retargeting across ticks so a big fleet doesn't all think at once.
                m.RetargetTimer = ((id * 7 + i * 3) % 15) / 30f;
                Mounts.Add(m);
                MaxGunRange = System.MathF.Max(MaxGunRange, def.Hardpoints[i].Mount.Range);
            }
            ScanTimer = (id % 15) / 30f;
        }

        public Vector3 Forward => SimMath.HeadingToDir(Yaw);

        public Vector3 LocalToWorld(Vector3 local) => Position + SimMath.RotateYaw(local, Yaw);

        public void KeelSegment(out Vector3 bow, out Vector3 stern)
        {
            Vector3 f = Forward * Def.HalfLength;
            bow = Position + f;
            stern = Position - f;
        }
    }
}
