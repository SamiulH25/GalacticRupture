using System.Numerics;

namespace GalacticRupture.Sim
{
    // Placeholder hull and gun roster until data authoring lands (IDEAS.md §8.5).
    // Hull deck heights here must match the placeholder meshes in the Game layer.
    public static class Hulls
    {
        public static readonly MountDef MainBattery = new MountDef
        {
            Id = "main_battery", Name = "Heavy mass driver", Class = MountClass.Line, Channel = DamageChannel.Ballistic,
            Size = MountSize.Large, Cost = 90, Family = WeaponFamily.MassDriver,
            YawRateDeg = 14f, PitchRateDeg = 10f, PitchMinDeg = -4f, PitchMaxDeg = 40f,
            Range = 4800f, Reload = 5.5f, Barrels = 2, BarrelStagger = 0.18f,
            ProjectileSpeed = 1500f, Damage = 150f, SpreadDeg = 0.2f, AimToleranceDeg = 1.2f,
            MuzzleLength = 17f, PivotHeight = 3.8f, Hp = 600f,
        };

        public static readonly MountDef Secondary = new MountDef
        {
            Id = "secondary", Name = "Turbolaser", Class = MountClass.Secondary, Channel = DamageChannel.Energy,
            Size = MountSize.Medium, Cost = 30, Family = WeaponFamily.Turbolaser,
            YawRateDeg = 34f, PitchRateDeg = 28f, PitchMinDeg = -6f, PitchMaxDeg = 70f,
            Range = 3000f, Reload = 1.8f, Barrels = 2, BarrelStagger = 0.1f,
            ProjectileSpeed = 1700f, Damage = 38f, SpreadDeg = 0.35f, AimToleranceDeg = 2f,
            MuzzleLength = 9f, PivotHeight = 2.2f, Hp = 220f,
        };

        public static readonly MountDef PointDefence = new MountDef
        {
            Id = "point_defence", Name = "Point defence", Class = MountClass.PointDefence, Channel = DamageChannel.Ballistic,
            Size = MountSize.Small, Cost = 10, Family = WeaponFamily.PointDefence,
            YawRateDeg = 110f, PitchRateDeg = 90f, PitchMinDeg = -8f, PitchMaxDeg = 85f,
            Range = 1500f, Reload = 0.22f, Barrels = 2, BarrelStagger = 0.05f,
            ProjectileSpeed = 2000f, Damage = 7f, SpreadDeg = 0.9f, AimToleranceDeg = 4f,
            MuzzleLength = 5f, PivotHeight = 1.8f, Hp = 90f,
        };

        public static readonly MountDef CorvetteGun = new MountDef
        {
            Id = "corvette_gun", Name = "Light turbolaser", Class = MountClass.Secondary, Channel = DamageChannel.Energy,
            Size = MountSize.Small, Cost = 12, Family = WeaponFamily.Turbolaser,
            YawRateDeg = 60f, PitchRateDeg = 50f, PitchMinDeg = -10f, PitchMaxDeg = 60f,
            Range = 1800f, Reload = 1.4f, Barrels = 2, BarrelStagger = 0.08f,
            ProjectileSpeed = 1600f, Damage = 16f, SpreadDeg = 0.6f, AimToleranceDeg = 3f,
            MuzzleLength = 4.5f, PivotHeight = 1.5f, Hp = 60f,
        };

        // --- Known from the start ------------------------------------------------------------

        // Small, ballistic: a light rapid gun that grinds hull through shields.
        public static readonly MountDef AutoCannon = new MountDef
        {
            Id = "autocannon", Name = "Autocannon", Class = MountClass.Secondary, Channel = DamageChannel.Ballistic,
            Size = MountSize.Small, Cost = 12, Family = WeaponFamily.Autocannon,
            YawRateDeg = 55f, PitchRateDeg = 45f, PitchMinDeg = -10f, PitchMaxDeg = 60f,
            Range = 1600f, Reload = 0.8f, Barrels = 1, BarrelStagger = 0.06f,
            ProjectileSpeed = 1500f, Damage = 20f, SpreadDeg = 0.7f, AimToleranceDeg = 3f,
            MuzzleLength = 5.5f, PivotHeight = 1.5f, Hp = 60f,
        };

        // Medium, ballistic: the escort's shell gun.
        public static readonly MountDef MassDriver = new MountDef
        {
            Id = "mass_driver", Name = "Mass driver", Class = MountClass.Secondary, Channel = DamageChannel.Ballistic,
            Size = MountSize.Medium, Cost = 28, Family = WeaponFamily.MassDriver,
            YawRateDeg = 26f, PitchRateDeg = 20f, PitchMinDeg = -6f, PitchMaxDeg = 60f,
            Range = 3400f, Reload = 3.2f, Barrels = 2, BarrelStagger = 0.14f,
            ProjectileSpeed = 1300f, Damage = 60f, SpreadDeg = 0.3f, AimToleranceDeg = 1.8f,
            MuzzleLength = 10f, PivotHeight = 2.2f, Hp = 240f,
        };

        // --- Unlocked by research (Sim/Strategic/Tech.cs) ------------------------------------

        // Ballistic, tier 2: rapid fire. Damage per round down hard, volume up.
        public static readonly MountDef RotaryCannon = new MountDef
        {
            Id = "rotary_cannon", Name = "Rotary cannon", Class = MountClass.Secondary, Channel = DamageChannel.Ballistic,
            Size = MountSize.Small, Cost = 18, Family = WeaponFamily.Rotary, Tech = "rotary",
            YawRateDeg = 70f, PitchRateDeg = 60f, PitchMinDeg = -10f, PitchMaxDeg = 70f,
            Range = 1400f, Reload = 0.5f, Barrels = 4, BarrelStagger = 0.03f,
            ProjectileSpeed = 1400f, Damage = 6f, SpreadDeg = 1f, AimToleranceDeg = 4f,
            MuzzleLength = 5f, PivotHeight = 1.5f, Hp = 70f,
        };

        public static readonly MountDef RotaryDriver = new MountDef
        {
            Id = "rotary_driver", Name = "Rotary driver", Class = MountClass.Secondary, Channel = DamageChannel.Ballistic,
            Size = MountSize.Medium, Cost = 42, Family = WeaponFamily.Rotary, Tech = "rotary_driver",
            YawRateDeg = 40f, PitchRateDeg = 34f, PitchMinDeg = -6f, PitchMaxDeg = 65f,
            Range = 2400f, Reload = 0.9f, Barrels = 3, BarrelStagger = 0.05f,
            ProjectileSpeed = 1400f, Damage = 20f, SpreadDeg = 0.6f, AimToleranceDeg = 2.5f,
            MuzzleLength = 9f, PivotHeight = 2.2f, Hp = 240f,
        };

        // Ballistic, tier 3: rails. Long reach, very fast, very accurate.
        public static readonly MountDef Railgun = new MountDef
        {
            Id = "railgun", Name = "Railgun", Class = MountClass.Secondary, Channel = DamageChannel.Ballistic,
            Size = MountSize.Medium, Cost = 50, Family = WeaponFamily.Railgun, Tech = "rails",
            YawRateDeg = 22f, PitchRateDeg = 18f, PitchMinDeg = -6f, PitchMaxDeg = 55f,
            Range = 5000f, Reload = 3.5f, Barrels = 1, BarrelStagger = 0.1f,
            ProjectileSpeed = 4000f, Damage = 140f, SpreadDeg = 0.05f, AimToleranceDeg = 0.8f,
            MuzzleLength = 13f, PivotHeight = 2.2f, Hp = 220f,
        };

        public static readonly MountDef HeavyRailgun = new MountDef
        {
            Id = "heavy_railgun", Name = "Heavy railgun", Class = MountClass.Line, Channel = DamageChannel.Ballistic,
            Size = MountSize.Large, Cost = 150, Family = WeaponFamily.Railgun, Tech = "capital_rails",
            YawRateDeg = 11f, PitchRateDeg = 9f, PitchMinDeg = -4f, PitchMaxDeg = 40f,
            Range = 6000f, Reload = 7f, Barrels = 2, BarrelStagger = 0.2f,
            ProjectileSpeed = 4500f, Damage = 320f, SpreadDeg = 0.04f, AimToleranceDeg = 0.7f,
            MuzzleLength = 22f, PivotHeight = 3.8f, Hp = 620f,
        };

        // Ballistic, tier 4: one enormous coil gun whose round goes on through its target.
        public static readonly MountDef MassAccelerator = new MountDef
        {
            Id = "mass_accelerator", Name = "Mass accelerator", Class = MountClass.Line, Channel = DamageChannel.Ballistic,
            Size = MountSize.Large, Cost = 190, Family = WeaponFamily.Accelerator, Tech = "acceleration", Pierce = 2,
            YawRateDeg = 7f, PitchRateDeg = 6f, PitchMinDeg = -3f, PitchMaxDeg = 30f,
            Range = 6500f, Reload = 10f, Barrels = 1, BarrelStagger = 0.1f,
            ProjectileSpeed = 5000f, Damage = 700f, SpreadDeg = 0.03f, AimToleranceDeg = 0.6f,
            MuzzleLength = 26f, PivotHeight = 3.8f, Hp = 700f,
        };

        // Energy, tier 1: the capital's turbolaser.
        public static readonly MountDef HeavyTurbolaser = new MountDef
        {
            Id = "heavy_turbolaser", Name = "Heavy turbolaser", Class = MountClass.Line, Channel = DamageChannel.Energy,
            Size = MountSize.Large, Cost = 100, Family = WeaponFamily.Turbolaser, Tech = "heavy_turbolasers",
            YawRateDeg = 16f, PitchRateDeg = 12f, PitchMinDeg = -4f, PitchMaxDeg = 45f,
            Range = 4000f, Reload = 4f, Barrels = 2, BarrelStagger = 0.2f,
            ProjectileSpeed = 1900f, Damage = 120f, SpreadDeg = 0.2f, AimToleranceDeg = 1.3f,
            MuzzleLength = 13f, PivotHeight = 3.8f, Hp = 580f,
        };

        // Energy, tier 2: quad emitters, rapid short bolts.
        public static readonly MountDef PulseLaser = new MountDef
        {
            Id = "pulse_laser", Name = "Pulse laser", Class = MountClass.Secondary, Channel = DamageChannel.Energy,
            Size = MountSize.Small, Cost = 20, Family = WeaponFamily.Pulse, Tech = "pulse",
            YawRateDeg = 65f, PitchRateDeg = 55f, PitchMinDeg = -10f, PitchMaxDeg = 70f,
            Range = 1600f, Reload = 0.6f, Barrels = 4, BarrelStagger = 0.04f,
            ProjectileSpeed = 2000f, Damage = 9f, SpreadDeg = 0.5f, AimToleranceDeg = 3f,
            MuzzleLength = 4f, PivotHeight = 1.5f, Hp = 70f,
        };

        // Energy, tier 3: a slow, heavy bolt from one long emitter.
        public static readonly MountDef LanceBattery = new MountDef
        {
            Id = "lance_battery", Name = "Beam lance", Class = MountClass.Line, Channel = DamageChannel.Energy,
            Size = MountSize.Large, Cost = 110, Family = WeaponFamily.Lance, Tech = "lances",
            YawRateDeg = 9f, PitchRateDeg = 8f, PitchMinDeg = -4f, PitchMaxDeg = 35f,
            Range = 4200f, Reload = 8f, Barrels = 1, BarrelStagger = 0.25f,
            ProjectileSpeed = 2200f, Damage = 520f, SpreadDeg = 0.15f, AimToleranceDeg = 1f,
            MuzzleLength = 20f, PivotHeight = 3.8f, Hp = 560f,
        };

        // Energy, tier 4: tuned to tear shields down.
        public static readonly MountDef ShieldBurner = new MountDef
        {
            Id = "shield_burner", Name = "Shield burner", Class = MountClass.Secondary, Channel = DamageChannel.Energy,
            Size = MountSize.Medium, Cost = 60, Family = WeaponFamily.ShieldBurner, Tech = "shield_burners", ShieldFactor = 2.2f,
            YawRateDeg = 30f, PitchRateDeg = 24f, PitchMinDeg = -6f, PitchMaxDeg = 65f,
            Range = 3000f, Reload = 2f, Barrels = 2, BarrelStagger = 0.1f,
            ProjectileSpeed = 1800f, Damage = 50f, SpreadDeg = 0.3f, AimToleranceDeg = 2f,
            MuzzleLength = 8f, PivotHeight = 2.2f, Hp = 220f,
        };

        // Disruptive: no hull damage to speak of, but it kills guns (IDEAS.md §5.1).
        public static readonly MountDef IonBattery = new MountDef
        {
            Id = "ion_battery", Name = "Ion cannon", Class = MountClass.Secondary, Channel = DamageChannel.Disruptive,
            Size = MountSize.Medium, Cost = 36, Family = WeaponFamily.Ion, Tech = "ion",
            YawRateDeg = 30f, PitchRateDeg = 24f, PitchMinDeg = -6f, PitchMaxDeg = 70f,
            Range = 2600f, Reload = 2.4f, Barrels = 2, BarrelStagger = 0.1f,
            ProjectileSpeed = 1500f, Damage = 45f, SpreadDeg = 0.35f, AimToleranceDeg = 2f,
            MuzzleLength = 9f, PivotHeight = 2.2f, Hp = 220f,
        };

        public static readonly MountDef LightIon = new MountDef
        {
            Id = "light_ion", Name = "Light ion cannon", Class = MountClass.Secondary, Channel = DamageChannel.Disruptive,
            Size = MountSize.Small, Cost = 16, Family = WeaponFamily.Ion, Tech = "compact_ion",
            YawRateDeg = 55f, PitchRateDeg = 45f, PitchMinDeg = -10f, PitchMaxDeg = 60f,
            Range = 1800f, Reload = 1.6f, Barrels = 1, BarrelStagger = 0.1f,
            ProjectileSpeed = 1500f, Damage = 36f, SpreadDeg = 0.5f, AimToleranceDeg = 3f,
            MuzzleLength = 5f, PivotHeight = 1.5f, Hp = 60f,
        };

        public static readonly MountDef HeavyIon = new MountDef
        {
            Id = "heavy_ion", Name = "Heavy ion cannon", Class = MountClass.Line, Channel = DamageChannel.Disruptive,
            Size = MountSize.Large, Cost = 95, Family = WeaponFamily.Ion, Tech = "heavy_ion",
            YawRateDeg = 14f, PitchRateDeg = 11f, PitchMinDeg = -4f, PitchMaxDeg = 40f,
            Range = 3600f, Reload = 4f, Barrels = 2, BarrelStagger = 0.2f,
            ProjectileSpeed = 1500f, Damage = 110f, SpreadDeg = 0.2f, AimToleranceDeg = 1.3f,
            MuzzleLength = 14f, PivotHeight = 3.8f, Hp = 580f,
        };

        // Flak: bursts that screen small craft.
        public static readonly MountDef FlakBattery = new MountDef
        {
            Id = "flak_battery", Name = "Flak battery", Class = MountClass.PointDefence, Channel = DamageChannel.Ballistic,
            Size = MountSize.Medium, Cost = 24, Family = WeaponFamily.Flak, Tech = "flak",
            YawRateDeg = 70f, PitchRateDeg = 60f, PitchMinDeg = -8f, PitchMaxDeg = 85f,
            Range = 1900f, Reload = 0.5f, Barrels = 4, BarrelStagger = 0.05f,
            ProjectileSpeed = 1800f, Damage = 6f, SpreadDeg = 0.8f, AimToleranceDeg = 3.5f,
            MuzzleLength = 5f, PivotHeight = 2.2f, Hp = 200f,
        };

        public static readonly MountDef HeavyFlak = new MountDef
        {
            Id = "heavy_flak", Name = "Heavy flak", Class = MountClass.PointDefence, Channel = DamageChannel.Ballistic,
            Size = MountSize.Large, Cost = 60, Family = WeaponFamily.Flak, Tech = "heavy_flak",
            YawRateDeg = 45f, PitchRateDeg = 40f, PitchMinDeg = -6f, PitchMaxDeg = 85f,
            Range = 2200f, Reload = 0.6f, Barrels = 4, BarrelStagger = 0.06f,
            ProjectileSpeed = 1700f, Damage = 20f, SpreadDeg = 0.9f, AimToleranceDeg = 3.5f,
            MuzzleLength = 8f, PivotHeight = 3.8f, Hp = 520f,
        };

        public static readonly MountDef[] Mounts =
        {
            MainBattery, HeavyTurbolaser, HeavyRailgun, MassAccelerator, LanceBattery, HeavyIon, HeavyFlak,
            MassDriver, Secondary, RotaryDriver, Railgun, ShieldBurner, IonBattery, FlakBattery,
            AutoCannon, CorvetteGun, RotaryCannon, PulseLaser, LightIon, PointDefence,
        };

        public static MountDef MountById(string id)
        {
            foreach (var m in Mounts)
                if (m.Id == id) return m;
            return null;
        }

        public const float BattleshipDeck = 12f;
        public const float BattleshipSuperfire = 20f;

        public static readonly HullDef Battleship = BuildBattleship();
        public static readonly HullDef Destroyer = BuildDestroyer();
        public static readonly HullDef Frigate = BuildFrigate();
        public static readonly HullDef Corvette = BuildCorvette();
        public static readonly HullDef Transport = BuildTransport();

        public static readonly HullDef[] All = { Battleship, Destroyer, Frigate, Corvette, Transport };

        public static HullDef ById(string id)
        {
            foreach (var h in All)
                if (h.Id == id) return h;
            return null;
        }

        static HardpointDef Hp(MountDef m, float x, float y, float z, float baseYaw, float halfArc) =>
            new HardpointDef { Mount = m, Size = m.Size, LocalPosition = new Vector3(x, y, z), BaseYawDeg = baseYaw, YawHalfArcDeg = halfArc };

        // Every faction starts with the stock loadouts; the designer copies and changes them.
        public static ShipDesign[] StockDesigns() => new[]
        {
            new ShipDesign("Line Battleship", Battleship),
            new ShipDesign("Escort Destroyer", Destroyer),
            new ShipDesign("Patrol Frigate", Frigate),
            new ShipDesign("Picket Corvette", Corvette),
            new ShipDesign("Troop Transport", Transport),
        };

        static HullDef BuildBattleship()
        {
            var h = new HullDef
            {
                Id = "battleship", Class = HullClass.Battleship,
                Cost = 1200, BuildDays = 16f, LyPerDay = 3.5f,
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
                Cost = 340, BuildDays = 6f, LyPerDay = 5f,
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
                Cost = 150, BuildDays = 3.5f, LyPerDay = 6f,
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
                Cost = 60, BuildDays = 1.5f, LyPerDay = 8f,
                HalfLength = 22f, Radius = 6f,
                MaxSpeed = 170f, Acceleration = 30f, TurnRateDeg = 45f, ClimbRate = 40f,
                HullHp = 420f, ShieldHp = 220f, ShieldRegen = 12f, ShieldRegenDelay = 5f,
            };
            h.Hardpoints.Add(Hp(CorvetteGun, 0f, 3.5f, 8f, 0f, 70f));
            return h;
        }

        // A troop carrier: slow, well shielded, point defence only. Carries one army.
        static HullDef BuildTransport()
        {
            var h = new HullDef
            {
                Id = "transport", Class = HullClass.Transport,
                Cost = 110, BuildDays = 3f, LyPerDay = 5f, TroopCapacity = 1,
                HalfLength = 55f, Radius = 13f,
                MaxSpeed = 45f, Acceleration = 5f, TurnRateDeg = 7f, ClimbRate = 8f,
                HullHp = 2800f, ShieldHp = 1100f, ShieldRegen = 35f, ShieldRegenDelay = 6f,
            };
            h.Hardpoints.Add(Hp(PointDefence, 0f, 9f, 34f, 0f, 150f));
            h.Hardpoints.Add(Hp(PointDefence, 0f, 9f, -30f, 180f, 150f));
            return h;
        }
    }
}
