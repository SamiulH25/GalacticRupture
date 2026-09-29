using System.Numerics;
using GalacticRupture.Sim;
using NUnit.Framework;

namespace GalacticRupture.Tests
{
    public class TurretTests
    {
        static HullDef Barge(MountDef gun, float baseYaw, float halfArc)
        {
            var h = new HullDef { Id = "barge", HalfLength = 50f, Radius = 10f, MaxSpeed = 0f, HullHp = 1000f, ShieldHp = 0f };
            h.Hardpoints.Add(new HardpointDef { Mount = gun, LocalPosition = new Vector3(0f, 5f, 0f), BaseYawDeg = baseYaw, YawHalfArcDeg = halfArc });
            return h;
        }

        static MountDef SlowGun() => new MountDef
        {
            Id = "slow", YawRateDeg = 10f, PitchRateDeg = 10f, Range = 3000f, Reload = 1f,
            ProjectileSpeed = 1000f, Damage = 10f, SpreadDeg = 0f, PitchMinDeg = -30f,
        };

        static HullDef Target() => new HullDef { Id = "target", HalfLength = 20f, Radius = 8f, MaxSpeed = 0f, HullHp = 100000f, ShieldHp = 0f };

        [Test]
        public void TraverseIsRateLimited()
        {
            var w = new World(1);
            var shooter = w.Spawn(Barge(SlowGun(), 0f, 180f), 0, Vector3.Zero, 0f);
            w.Spawn(Target(), 1, new Vector3(1000f, 0f, 0f), 0f); // 90° to starboard
            var m = shooter.Mounts[0];

            float maxStep = 10f * SimMath.Deg2Rad * World.Dt + 1e-5f;
            for (int i = 0; i < World.TicksPerSecond * 4; i++)
            {
                float before = m.Yaw;
                w.Step();
                Assert.LessOrEqual(System.MathF.Abs(SimMath.WrapAngle(m.Yaw - before)), maxStep);
            }
            // After 4 s at 10°/s the gun has swung 40° of the 90° it needs, and has not fired.
            Assert.AreEqual(40f, m.Yaw / SimMath.Deg2Rad, 0.5f);
            Assert.IsFalse(m.OnTarget);
        }

        [Test]
        public void GunConvergesThenHits()
        {
            var w = new World(2);
            var shooter = w.Spawn(Barge(SlowGun(), 0f, 180f), 0, Vector3.Zero, 0f);
            var target = w.Spawn(Target(), 1, new Vector3(1000f, 0f, 0f), 0f);

            for (int i = 0; i < World.TicksPerSecond * 15; i++) w.Step();
            Assert.Less(target.Hull, target.Def.HullHp, "the gun should have swung round and hit");
        }

        [Test]
        public void TargetOutsideArcIsIgnored()
        {
            var w = new World(3);
            var shooter = w.Spawn(Barge(SlowGun(), 0f, 45f), 0, Vector3.Zero, 0f); // forward-only gun
            var target = w.Spawn(Target(), 1, new Vector3(0f, 0f, -1000f), 0f);   // dead astern

            for (int i = 0; i < World.TicksPerSecond * 10; i++) w.Step();
            Assert.AreEqual(-1, shooter.Mounts[0].Target);
            Assert.AreEqual(target.Def.HullHp, target.Hull);
        }

        [Test]
        public void LimitedArcGunNeverCrossesTheDeadZone()
        {
            var w = new World(4);
            var shooter = w.Spawn(Barge(SlowGun(), 0f, 150f), 0, Vector3.Zero, 0f);
            w.Spawn(Target(), 1, new Vector3(-600f, 0f, -900f), 0f); // aft port quarter, inside the arc
            float half = 150f * SimMath.Deg2Rad + 1e-5f;
            for (int i = 0; i < World.TicksPerSecond * 20; i++)
            {
                w.Step();
                Assert.LessOrEqual(System.MathF.Abs(shooter.Mounts[0].Yaw), half);
            }
        }

        [Test]
        public void InterceptLeadsAMovingTarget()
        {
            Assert.IsTrue(SimMath.InterceptTime(new Vector3(0f, 0f, 1000f), new Vector3(100f, 0f, 0f), 1000f, out float t));
            Vector3 aim = new Vector3(0f, 0f, 1000f) + new Vector3(100f, 0f, 0f) * t;
            Assert.AreEqual(1000f * t, aim.Length(), 0.01f);
        }
    }

    public class DeterminismTests
    {
        [Test]
        public void SameSeedSameOutcome()
        {
            var a = TurretToy.Create(42);
            var b = TurretToy.Create(42);
            for (int i = 0; i < World.TicksPerSecond * 90; i++)
            {
                a.Step();
                b.Step();
                a.Events.Clear();
                b.Events.Clear();
            }
            Assert.AreEqual(a.Hash(), b.Hash());
            Assert.Greater(a.Ships.Count, 3, "a wave should have spawned");
        }

        [Test]
        public void TurretToyProducesAFight()
        {
            var w = TurretToy.Create(7);
            int fired = 0, destroyed = 0;
            for (int i = 0; i < World.TicksPerSecond * 120; i++)
            {
                w.Step();
                foreach (var e in w.Events)
                {
                    if (e.Kind == SimEventKind.Fired) fired++;
                    if (e.Kind == SimEventKind.ShipDestroyed) destroyed++;
                }
                w.Events.Clear();
            }
            TestContext.WriteLine($"fired {fired}, destroyed {destroyed}");
            Assert.Greater(fired, 100);
            Assert.Greater(destroyed, 0);
        }
    }
}
