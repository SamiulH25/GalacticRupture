using System.Numerics;
using GalacticRupture.Sim;
using GalacticRupture.Sim.Strategic;
using NUnit.Framework;

namespace GalacticRupture.Tests
{
    // Asteroids, nebulae, stations, abilities and the command cap.
    public class TerrainTests
    {
        [Test]
        public void RockBlocksLineOfSightAndRounds()
        {
            var w = new World(1);
            var a = w.Spawn(Hulls.Destroyer, 0, new Vector3(0f, 0f, 0f), 0f);
            var b = w.Spawn(Hulls.Destroyer, 1, new Vector3(0f, 0f, 2000f), SimMath.Pi);
            a.Speed = b.Speed = 0f;
            w.AddObstacle(ObstacleKind.Asteroid, new Vector3(0f, 0f, 1000f), 250f);
            for (int i = 0; i < World.TicksPerSecond * 20; i++) w.Step();
            Assert.AreEqual(b.Def.HullHp, b.Hull, "nothing gets through the rock");
            Assert.AreEqual(a.Def.HullHp, a.Hull);
        }

        [Test]
        public void ShipsSteerRoundRock()
        {
            var w = new World(2);
            var s = w.Spawn(Hulls.Destroyer, 0, new Vector3(0f, 0f, 0f), 0f);
            w.AddObstacle(ObstacleKind.Asteroid, new Vector3(0f, 0f, 1500f), 300f);
            w.Issue(s.Id, ShipOrder.MoveTo(new Vector3(0f, 0f, 3200f)));
            float closest = float.MaxValue;
            for (int i = 0; i < World.TicksPerSecond * 90; i++)
            {
                w.Step();
                closest = System.MathF.Min(closest, Vector3.Distance(s.Position, new Vector3(0f, s.Position.Y, 1500f)));
            }
            Assert.Greater(closest, 300f, "never inside the rock");
            Assert.Less(Vector3.Distance(s.Position, new Vector3(0f, s.Position.Y, 3200f)), 300f, "and still gets there");
        }

        [Test]
        public void NebulaStripsShields()
        {
            var w = new World(3);
            var s = w.Spawn(Hulls.Destroyer, 0, Vector3.Zero, 0f);
            s.Speed = 0f;
            w.AddObstacle(ObstacleKind.Nebula, Vector3.Zero, 1500f);
            for (int i = 0; i < World.TicksPerSecond * 5; i++) w.Step();
            Assert.AreEqual(0f, s.Shield);
        }

        [Test]
        public void PowerToWeaponsReloadsFaster()
        {
            var w = new World(4);
            var s = w.Spawn(Hulls.Destroyer, 0, Vector3.Zero, 0f);
            w.Use(s.Id, Ability.Weapons);
            w.Step();
            Assert.IsTrue(s.Using(Ability.Weapons));
            Assert.Less(s.ReloadFactor, 1f);
            Assert.IsFalse(s.CanUse(Ability.Shields), "one at a time");
            for (int i = 0; i < World.TicksPerSecond * 11; i++) w.Step();
            Assert.AreEqual(-1, s.ActiveAbility);
            Assert.Greater(s.AbilityCooldown[(int)Ability.Weapons], 0f);
        }

        [Test]
        public void ReinforcementsRespectTheCap()
        {
            var w = new World(5);
            var r = new Reinforcements();
            r.Cap[0] = 7;
            w.Spawn(Hulls.Battleship, 0, Vector3.Zero, 0f);   // 6 points
            r.Pool[0].Add(new ReserveShip { Def = Hulls.Destroyer, Hull = Hulls.Destroyer.HullHp });
            r.Pool[0].Add(new ReserveShip { Def = Hulls.Corvette, Hull = Hulls.Corvette.HullHp });
            r.Request(0, 0);   // a destroyer (3) does not fit
            r.Tick(w, false, false);
            Assert.AreEqual(1, w.Ships.Count);
            r.Tick(w, true, false);   // the AI brings in what fits: the corvette
            Assert.AreEqual(2, w.Ships.Count);
            Assert.AreEqual(HullClass.Corvette, w.Ships[1].Def.Class);
        }

        [Test]
        public void StationFightsForItsSystemAndKeepsItsDamage()
        {
            var c = GalaxyGen.Generate(11);
            // An enemy war fleet arrives at a major's capital, which starts with a station.
            int capital = c.Factions[1].Capital;
            var sys = c.Galaxy.Systems[capital];
            Assert.Greater(sys.Station, 0);
            c.SetStance(0, 1, DiplomaticStance.War);
            var f = c.NewFleet(0, capital, "Raid");
            for (int i = 0; i < 3; i++) f.Ships.Add(new ShipRecord(Hulls.Corvette, $"c{i}"));
            c.Tick();
            Assert.IsNotNull(c.Pending, "a station stops a hostile fleet");
            var state = BattleSetup.AutoResolve(c);
            Assert.AreNotEqual(BattleState.Fighting, state);
            Assert.Greater(sys.Station, 0, "three corvettes don't kill a station");
        }
    }
}
