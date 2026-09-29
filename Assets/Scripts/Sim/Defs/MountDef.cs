namespace GalacticRupture.Sim
{
    public enum MountClass
    {
        Line,
        Secondary,
        PointDefence,
    }

    public enum DamageChannel
    {
        Ballistic,
        Energy,
        Disruptive,
    }

    // A gun: how it moves and what it throws. Shared by every hardpoint that carries it.
    public sealed class MountDef
    {
        public string Id = "";
        public MountClass Class;
        public DamageChannel Channel;

        public float YawRateDeg = 20f;      // traverse, degrees per second
        public float PitchRateDeg = 15f;
        public float PitchMinDeg = -5f;
        public float PitchMaxDeg = 60f;

        public float Range = 3000f;
        public float Reload = 4f;           // seconds between shots
        public int Barrels = 2;             // one round per barrel per shot
        public float BarrelStagger = 0.12f; // seconds between barrels in one shot
        public float ProjectileSpeed = 1200f;
        public float Damage = 60f;
        public float SpreadDeg = 0.25f;
        public float AimToleranceDeg = 1.5f; // fires only when the barrels are this close to the solution
        public float MuzzleLength = 10f;     // pivot to muzzle, metres
        public float PivotHeight = 3f;       // trunnion height above the hardpoint

        public float Hp = 120f;
    }
}
