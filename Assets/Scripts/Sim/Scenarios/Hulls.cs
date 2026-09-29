using System.Numerics;

namespace GalacticRupture.Sim
{
    // Placeholder hull and gun roster until data authoring lands (IDEAS.md §8.5).
    // Hull deck heights here must match the placeholder meshes in the Game layer.
    public static class Hulls
    {
        public static readonly MountDef MainBattery = new MountDef
        {
            Id = "main_battery", Class = MountClass.Line, Channel = DamageChannel.Ballistic,
            YawRateDeg = 14f, PitchRateDeg = 10f, PitchMinDeg = -4f, PitchMaxDeg = 40f,
            Range = 4800f, Reload = 5.5f, Barrels = 2, BarrelStagger = 0.18f,
            ProjectileSpeed = 1500f, Damage = 150f, SpreadDeg = 0.2f, AimToleranceDeg = 1.2f,
            MuzzleLength = 17f, PivotHeight = 5f, Hp = 600f,
        };

        public static readonly MountDef Secondary = new MountDef
        {
            Id = "secondary", Class = MountClass.Secondary, Channel = DamageChannel.Energy,
            YawRateDeg = 34f, PitchRateDeg = 28f, PitchMinDeg = -6f, PitchMaxDeg = 70f,
            Range = 3000f, Reload = 1.8f, Barrels = 2, BarrelStagger = 0.1f,
            ProjectileSpeed = 1700f, Damage = 38f, SpreadDeg = 0.35f, AimToleranceDeg = 2f,
            MuzzleLength = 9f, PivotHeight = 3f, Hp = 220f,
        };

        public static readonly MountDef PointDefence = new MountDef
        {
            Id = "point_defence", Class = MountClass.PointDefence, Channel = DamageChannel.Ballistic,
            YawRateDeg = 110f, PitchRateDeg = 90f, PitchMinDeg = -8f, PitchMaxDeg = 85f,
            Range = 1500f, Reload = 0.22f, Barrels = 2, BarrelStagger = 0.05f,
            ProjectileSpeed = 2000f, Damage = 7f, SpreadDeg = 0.9f, AimToleranceDeg = 4f,
            MuzzleLength = 5f, PivotHeight = 1.8f, Hp = 90f,
        };

        public static readonly MountDef CorvetteGun = new MountDef
        {
            Id = "corvette_gun", Class = MountClass.Secondary, Channel = DamageChannel.Energy,
            YawRateDeg = 60f, PitchRateDeg = 50f, PitchMinDeg = -10f, PitchMaxDeg = 60f,
            Range = 1800f, Reload = 1.4f, Barrels = 2, BarrelStagger = 0.08f,
            ProjectileSpeed = 1600f, Damage = 16f, SpreadDeg = 0.6f, AimToleranceDeg = 3f,
            MuzzleLength = 4.5f, PivotHeight = 1.5f, Hp = 60f,
        };

        public const float BattleshipDeck = 12f;
        public const float BattleshipSuperfire = 20f;

        public static readonly HullDef Battleship = BuildBattleship();
        public static readonly HullDef Destroyer = BuildDestroyer();
        public static readonly HullDef Frigate = BuildFrigate();
        public static readonly HullDef Corvette = BuildCorvette();

        static HardpointDef Hp(MountDef m, float x, float y, float z, float baseYaw, float halfArc) =>
            new HardpointDef { Mount = m, LocalPosition = new Vector3(x, y, z), BaseYawDeg = baseYaw, YawHalfArcDeg = halfArc };

        static HullDef BuildBattleship()
        {
            var h = new HullDef
            {
                Id = "battleship", Class = HullClass.Battleship,
                HalfLength = 170f, Radius = 24f,
                MaxSpeed = 40f, Acceleration = 3f, TurnRateDeg = 3f, ClimbRate = 6f,
                HullHp = 26000f, ShieldHp = 7000f, ShieldRegen = 140f, ShieldRegenDelay = 5f,
            };
            float d = BattleshipDeck, s = BattleshipSuperfire;
            // Main battery: two forward, one aft, centreline. B and X superfire over A and Y.
            h.Hardpoints.Add(Hp(MainBattery, 0f, d, 104f, 0f, 150f));
            h.Hardpoints.Add(Hp(MainBattery, 0f, s, 72f, 0f, 155f));
            h.Hardpoints.Add(Hp(MainBattery, 0f, s, -66f, 180f, 155f));
            h.Hardpoints.Add(Hp(MainBattery, 0f, d, -98f, 180f, 150f));
            // Secondaries: three a side on the beam sponsons.
            for (int i = 0; i < 3; i++)
            {
                float z = 40f - i * 38f;
                h.Hardpoints.Add(Hp(Secondary, 19f, d - 1f, z, 90f, 95f));
                h.Hardpoints.Add(Hp(Secondary, -19f, d - 1f, z, -90f, 95f));
            }
            // Point defence at the corners of the upper deck.
            h.Hardpoints.Add(Hp(PointDefence, 14f, d, 132f, 45f, 150f));
            h.Hardpoints.Add(Hp(PointDefence, -14f, d, 132f, -45f, 150f));
            h.Hardpoints.Add(Hp(PointDefence, 16f, s + 6f, 14f, 90f, 150f));
            h.Hardpoints.Add(Hp(PointDefence, -16f, s + 6f, 14f, -90f, 150f));
            h.Hardpoints.Add(Hp(PointDefence, 14f, d, -128f, 135f, 150f));
            h.Hardpoints.Add(Hp(PointDefence, -14f, d, -128f, -135f, 150f));
            return h;
        }

        static HullDef BuildDestroyer()
        {
            var h = new HullDef
            {
                Id = "destroyer", Class = HullClass.Destroyer,
                HalfLength = 70f, Radius = 11f,
                MaxSpeed = 70f, Acceleration = 7f, TurnRateDeg = 8f, ClimbRate = 12f,
                HullHp = 5200f, ShieldHp = 1800f, ShieldRegen = 50f, ShieldRegenDelay = 5f,
            };
            h.Hardpoints.Add(Hp(Secondary, 0f, 6f, 40f, 0f, 150f));
            h.Hardpoints.Add(Hp(Secondary, 0f, 6f, -36f, 180f, 150f));
            h.Hardpoints.Add(Hp(PointDefence, 7f, 7f, 8f, 90f, 150f));
            h.Hardpoints.Add(Hp(PointDefence, -7f, 7f, 8f, -90f, 150f));
            return h;
        }

        static HullDef BuildFrigate()
        {
            var h = new HullDef
            {
                Id = "frigate", Class = HullClass.Frigate,
                HalfLength = 42f, Radius = 8f,
                MaxSpeed = 110f, Acceleration = 14f, TurnRateDeg = 22f, ClimbRate = 25f,
                HullHp = 1500f, ShieldHp = 700f, ShieldRegen = 25f, ShieldRegenDelay = 5f,
            };
            h.Hardpoints.Add(Hp(CorvetteGun, 0f, 4.5f, 22f, 0f, 140f));
            h.Hardpoints.Add(Hp(CorvetteGun, 0f, 4.5f, -18f, 180f, 140f));
            return h;
        }

        static HullDef BuildCorvette()
        {
            var h = new HullDef
            {
                Id = "corvette", Class = HullClass.Corvette,
                HalfLength = 22f, Radius = 6f,
                MaxSpeed = 170f, Acceleration = 30f, TurnRateDeg = 45f, ClimbRate = 40f,
                HullHp = 420f, ShieldHp = 220f, ShieldRegen = 12f, ShieldRegenDelay = 5f,
            };
            h.Hardpoints.Add(Hp(CorvetteGun, 0f, 3.5f, 8f, 0f, 70f));
            return h;
        }
    }
}
