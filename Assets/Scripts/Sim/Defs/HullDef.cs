using System.Collections.Generic;
using System.Numerics;

namespace GalacticRupture.Sim
{
    // Where a gun sits on a hull and which way it can look.
    public sealed class HardpointDef
    {
        public MountDef Mount;
        public Vector3 LocalPosition;     // x starboard, y up, z forward, metres
        public float BaseYawDeg;          // the direction the gun rests in, 0 = forward
        public float YawHalfArcDeg = 180f; // 180 = all round
    }

    public enum HullClass
    {
        Corvette,
        Frigate,
        Destroyer,
        Cruiser,
        Battleship,
    }

    public sealed class HullDef
    {
        public string Id = "";
        public HullClass Class;

        // Collision is a capsule along the keel: from -HalfLength to +HalfLength on z.
        public float HalfLength = 100f;
        public float Radius = 20f;

        public float MaxSpeed = 60f;       // m/s
        public float Acceleration = 8f;
        public float TurnRateDeg = 6f;     // degrees per second at full speed
        public float ClimbRate = 10f;

        public float HullHp = 4000f;
        public float ShieldHp = 1500f;
        public float ShieldRegen = 40f;    // per second
        public float ShieldRegenDelay = 6f; // seconds after the last hit

        // Campaign: what it costs, how long a yard takes, how fast it crosses lanes.
        public int Cost = 100;
        public float BuildDays = 3f;
        public float LyPerDay = 5f;

        public List<HardpointDef> Hardpoints = new List<HardpointDef>();
    }
}
