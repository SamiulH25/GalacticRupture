using System.Numerics;
using GalacticRupture.Sim;
using NUnit.Framework;

namespace GalacticRupture.Tests
{
    public class OrderTests
    {
        static HullDef Target() => new HullDef { Id = "target", HalfLength = 20f, Radius = 8f, MaxSpeed = 0f, HullHp = 100000f, ShieldHp = 0f };

        [Test]
        public void CommandsApplyOnTheNextTick()
        {
            var w = new World(1);
            var s = w.Spawn(Hulls.Destroyer, 0, Vector3.Zero, 0f);
            w.Issue(s.Id, ShipOrder.MoveTo(new Vector3(0f, 0f, 1000f)));
            Assert.AreEqual(OrderKind.Hold, s.Order.Kind);
            w.Step();
            Assert.AreEqual(OrderKind.MoveTo, s.Order.Kind);
        }

        [Test]
        public void MoveToArrivesAndStops()
        {
            var w = new World(2);
            var s = w.Spawn(Hulls.Destroyer, 0, Vector3.Zero, 0f);
            w.Issue(s.Id, ShipOrder.MoveTo(new Vector3(800f, 40f, 1500f)));
            for (int i = 0; i < World.TicksPerSecond * 90; i++) w.Step();
            Assert.Less(Vector3.Distance(s.Position, new Vector3(800f, 40f, 1500f)), 120f);
            Assert.Less(s.Speed, 5f);
        }

        [Test]
        public void AttackClosesToRangeAndFocusesFire()
        {
            var w = new World(3);
            var s = w.Spawn(Hulls.Battleship, 0, Vector3.Zero, 0f);
            var decoy = w.Spawn(Target(), 1, new Vector3(-1500f, 0f, 1500f), 0f);
            var target = w.Spawn(Target(), 1, new Vector3(0f, 0f, 7000f), 0f);
            w.Issue(s.Id, ShipOrder.Attack(target.Id));
            for (int i = 0; i < World.TicksPerSecond * 120; i++) w.Step();

            float dist = Vector3.Distance(s.Position, target.Position);
            Assert.Less(dist, s.MaxGunRange, "closed to gun range");
            Assert.Less(target.Hull, target.Def.HullHp, "focus target is being hit");
            // Guns that can reach the focus target are on it; shorter-ranged guns are free to
            // engage whatever they can reach (here, the decoy).
            int canReach = 0, onFocus = 0;
            foreach (var m in s.Mounts)
            {
                if (!m.Alive || m.Def.Range < dist) continue;
                canReach++;
                if (m.Target == target.Id) onFocus++;
            }
            Assert.Greater(canReach, 0);
            Assert.AreEqual(canReach, onFocus, "every gun that reaches the focus target is on it");
        }

        [Test]
        public void FaceTurnsToThePointAndStops()
        {
            var w = new World(4);
            var s = w.Spawn(Hulls.Destroyer, 0, Vector3.Zero, 0f);
            s.Speed = 0f;
            w.Issue(s.Id, ShipOrder.Face(new Vector3(1000f, 0f, 0f)));
            for (int i = 0; i < World.TicksPerSecond * 30; i++) w.Step();
            Assert.AreEqual(90f, s.Yaw / SimMath.Deg2Rad, 2f);
        }

        [Test]
        public void SkirmishEndsWithAResult()
        {
            var w = Skirmish.Create(9);
            var sk = (Skirmish)w.Director;
            // Player fleet holds and only its guns act: the enemy still has to come to it.
            for (int i = 0; i < World.TicksPerSecond * 400 && sk.State == BattleState.Fighting; i++)
            {
                w.Step();
                w.Events.Clear();
            }
            Assert.AreNotEqual(BattleState.Fighting, sk.State);
        }
    }
}
