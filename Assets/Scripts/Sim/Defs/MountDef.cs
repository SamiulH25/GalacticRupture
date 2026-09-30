namespace GalacticRupture.Sim
{
    public enum MountClass
    {
        Line,
        Secondary,
        PointDefence,
    }

    // How big a gun a hardpoint takes. A mount fits any hardpoint of its size or larger.
    public enum MountSize
    {
        Small,
        Medium,
        Large,
    }

    // What kind of gun it is, beyond its size: sets its look and what it's good at (IDEAS.md §5.4).
    public enum WeaponFamily
    {
        MassDriver,     // ballistic shells from a heavy gunhouse
        Autocannon,     // light ballistic, rapid
        Rotary,         // many spinning barrels, a hail of small rounds
        Railgun,        // long thin rails, very fast slugs, long reach
        Accelerator,    // one huge coil gun; the round goes through ships
        Turbolaser,     // energy bolts from short, hot emitters
        Pulse,          // clustered quad emitters, rapid short bolts
        Lance,          // one long emitter, a slow heavy bolt
        ShieldBurner,   // energy tuned to tear shields down
        Ion,            // disruptive: kills guns, not hulls
        Flak,           // bursts to screen small craft
        PointDefence,   // tiny rapid guns
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
        public string Name = "";
        public MountClass Class;
        public DamageChannel Channel;
        public MountSize Size;
        public int Cost = 10;               // credits, on top of the hull
        public WeaponFamily Family;
        public string Tech = "";            // the technology that unlocks it; empty: known from the start
        public float ShieldFactor = 1f;     // multiplier on what it does to shields
        public int Pierce;                  // ships a round can pass through

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
