using System.Collections.Generic;
using System.Numerics;

namespace GalacticRupture.Sim
{
    // Where a gun sits on a hull and which way it can look.
    public sealed class HardpointDef
    {
        public MountDef Mount;
        public MountSize Size;            // the largest mount it takes
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
        Transport,  // carries armies; the fattest target in the game (IDEAS.md §6)
    }

    public sealed class HullDef
    {
        public string Id = "";            // the hull: picks the model
        public string Design = "";        // the loadout's name, empty for the stock hull
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
        public int TroopCapacity;         // armies carried

        public List<HardpointDef> Hardpoints = new List<HardpointDef>();

        // The same hull with other guns in its hardpoints.
        public HullDef WithMounts(string design, IReadOnlyList<MountDef> mounts)
        {
            var h = (HullDef)MemberwiseClone();
            h.Design = design;
            h.Hardpoints = new List<HardpointDef>();
            int delta = 0;
            for (int i = 0; i < Hardpoints.Count; i++)
            {
                var hp = Hardpoints[i];
                var m = i < mounts.Count && mounts[i] != null ? mounts[i] : hp.Mount;
                delta += m.Cost - hp.Mount.Cost;
                h.Hardpoints.Add(new HardpointDef
                {
                    Mount = m, Size = hp.Size, LocalPosition = hp.LocalPosition,
                    BaseYawDeg = hp.BaseYawDeg, YawHalfArcDeg = hp.YawHalfArcDeg,
                });
            }
            h.Cost = System.Math.Max(1, Cost + delta);
            h.BuildDays = BuildDays * System.MathF.Sqrt(h.Cost / (float)Cost);
            return h;
        }
    }
}
