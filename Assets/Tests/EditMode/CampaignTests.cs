using GalacticRupture.Sim;
using GalacticRupture.Sim.Strategic;
using NUnit.Framework;

namespace GalacticRupture.Tests
{
    public class CampaignTests
    {
        static StarSystem Sys(Campaign c, string name) => c.Galaxy.Systems.Find(s => s.Name == name);

        static void RunHours(Campaign c, int hours, bool autoResolve = true)
        {
            for (int i = 0; i < hours; i++)
            {
                if (c.Pending != null)
                {
                    if (!autoResolve) return;
                    BattleSetup.AutoResolve(c);
                }
                c.Tick();
                c.Events.Clear();
            }
        }

        [Test]
        public void RoutesFollowLanes()
        {
            var c = Maps.Rupture(1);
            var route = c.Galaxy.Route(Sys(c, "Aurel").Id, Sys(c, "Veyl").Id);
            Assert.Greater(route.Count, 2);
            Assert.AreEqual(Sys(c, "Veyl").Id, route[route.Count - 1]);
            int prev = Sys(c, "Aurel").Id;
            foreach (int s in route)
            {
                Assert.Greater(c.Galaxy.LaneLength(prev, s), 0f, "each hop is a lane");
                prev = s;
            }
        }

        [Test]
        public void FleetsTravelAtTheirSlowestShip()
        {
            var c = Maps.Rupture(2);
            var home = c.Fleets.Find(f => f.Name == "Home Fleet");
            int to = Sys(c, "Tessary").Id;
            float len = c.Galaxy.LaneLength(home.System, to);
            Assert.IsTrue(c.MoveFleet(home.Id, to));
            float hours = len / Hulls.Battleship.LyPerDay * 24f;
            RunHours(c, (int)hours - 1);
            Assert.IsTrue(home.InTransit, "not there yet");
            RunHours(c, 2);
            Assert.IsFalse(home.InTransit);
            Assert.AreEqual(to, home.System);
        }

        [Test]
        public void YardsBuildShipsIntoAFleet()
        {
            var c = Maps.Rupture(3);
            int aurel = Sys(c, "Aurel").Id;
            float before = c.Factions[0].Credits;
            Assert.IsTrue(c.QueueBuild(0, aurel, Hulls.Destroyer));
            Assert.AreEqual(before - Hulls.Destroyer.Cost, c.Factions[0].Credits, 0.01f);
            var home = c.Fleets.Find(f => f.Name == "Home Fleet");
            int ships = home.Ships.Count;
            RunHours(c, (int)(Hulls.Destroyer.BuildDays * 24f) + 1, autoResolve: false);
            Assert.AreEqual(ships + 1, home.Ships.Count);
        }

        [Test]
        public void AnUndefendedNeutralSystemIsTaken()
        {
            var c = Maps.Rupture(4);
            var screen = c.Fleets.Find(f => f.Name == "Tessary Screen");
            var serac = Sys(c, "Serac");
            c.MoveFleet(screen.Id, serac.Id);
            RunHours(c, 24 * 6, autoResolve: false);
            if (c.Pending == null) Assert.AreEqual(Campaign.PlayerFaction, serac.Owner);
        }

        [Test]
        public void OverwhelmingForceWinsAndTheLoserIsGone()
        {
            var c = Maps.Rupture(5);
            var home = c.Fleets.Find(f => f.Name == "Home Fleet");
            var pickets = c.Fleets.Find(f => f.Name == "Vantor Pickets");
            // Put them in the same place and let the campaign notice.
            pickets.System = home.System;
            c.Tick();
            Assert.IsNotNull(c.Pending);
            var state = BattleSetup.AutoResolve(c);
            Assert.IsNull(c.Pending);
            Assert.AreEqual(BattleState.Victory, state, "a battleship group beats four corvettes");
            Assert.IsFalse(c.Fleets.Contains(pickets), "the destroyed fleet is gone");
        }

        [Test]
        public void BattlesWriteDamageBack()
        {
            // An even fight: whoever survives carries damage, and losses leave the fleets.
            var c = Maps.Rupture(5);
            var screen = c.Fleets.Find(f => f.Name == "Tessary Screen");
            var line = c.Fleets.Find(f => f.Name == "Veyl Line");
            int before = screen.Ships.Count + line.Ships.Count;
            line.System = screen.System;
            c.Tick();
            Assert.IsNotNull(c.Pending);
            BattleSetup.AutoResolve(c);
            int after = 0;
            bool damaged = false;
            foreach (var f in new[] { screen, line })
            {
                if (!c.Fleets.Contains(f)) continue;
                after += f.Ships.Count;
                foreach (var r in f.Ships) damaged |= r.HullFraction < 0.999f || r.MountsAlive < r.MountAlive.Length;
            }
            Assert.IsTrue(after < before || damaged, "the fight left a mark");
        }

        [Test]
        public void YardsRepairDamage()
        {
            var c = Maps.Rupture(6);
            var home = c.Fleets.Find(f => f.Name == "Home Fleet");
            var r = home.Ships[0];
            r.Hull = r.Def.HullHp * 0.5f;
            r.MountAlive[0] = false;
            RunHours(c, 24 * 5, autoResolve: false);
            Assert.Greater(r.HullFraction, 0.9f);
            Assert.IsTrue(r.MountAlive[0], "the lost turret is replaced");
        }

        [Test]
        public void TheCampaignMovesWithoutThePlayer()
        {
            // The player does nothing: the enemy should build, advance and fight on its own.
            var c = Maps.Rupture(7);
            int enemyBefore = 0;
            foreach (var s in c.Galaxy.Systems) if (s.Owner == Campaign.EnemyFaction) enemyBefore++;
            RunHours(c, 24 * 40);
            int enemyAfter = 0;
            foreach (var s in c.Galaxy.Systems) if (s.Owner == Campaign.EnemyFaction) enemyAfter++;
            TestContext.WriteLine($"day {c.Hours / 24:F0}: enemy systems {enemyBefore} -> {enemyAfter}, state {c.State}");
            Assert.Greater(enemyAfter, enemyBefore, "the enemy expands");
        }

        [Test]
        public void SameSeedSameCampaign()
        {
            var a = Maps.Rupture(8);
            var b = Maps.Rupture(8);
            RunHours(a, 24 * 20);
            RunHours(b, 24 * 20);
            for (int i = 0; i < a.Galaxy.Systems.Count; i++)
                Assert.AreEqual(a.Galaxy.Systems[i].Owner, b.Galaxy.Systems[i].Owner);
            Assert.AreEqual(a.Fleets.Count, b.Fleets.Count);
            Assert.AreEqual(a.Factions[1].Credits, b.Factions[1].Credits, 0.001f);
        }
    }
}
