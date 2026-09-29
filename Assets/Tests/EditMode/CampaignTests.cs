using System.Numerics;
using GalacticRupture.Sim;
using GalacticRupture.Sim.Strategic;
using NUnit.Framework;

namespace GalacticRupture.Tests
{
    public class CampaignTests
    {
        static StarSystem Sys(Campaign c, string name) => c.Galaxy.Systems.Find(s => s.Name == name);

        // Runs the clock, fighting any battle unattended, abstaining in council, declining offers.
        static void RunHours(Campaign c, int hours, bool autoResolve = true)
        {
            for (int i = 0; i < hours; i++)
            {
                if (c.Pending != null)
                {
                    if (!autoResolve) return;
                    BattleSetup.AutoResolve(c);
                }
                if (c.Council != null) c.CastPlayerVote(Vote.Abstain);
                foreach (var p in c.Incoming.ToArray()) c.Respond(p, false);
                c.Tick();
                c.Events.Clear();
            }
        }

        // A small line of systems for isolated tests: A (player capital) - B (unclaimed) - C (enemy capital).
        static Campaign Line(bool war = true)
        {
            var c = new Campaign(3);
            c.AddFaction("Us", true, "US", 2000f);
            c.AddFaction("Them", true, "TH", 2000f);
            c.AddFaction("Others", false, "OT", 500f);
            var a = c.Galaxy.Add("A", 0f, 0f, 0f, 3, 0, capital: true);
            var b = c.Galaxy.Add("B", 10f, 0f, 0f, 2, -1);
            var cc = c.Galaxy.Add("C", 20f, 0f, 0f, 3, 1, capital: true);
            var d = c.Galaxy.Add("D", 10f, 0f, 12f, 2, 2, capital: true);
            c.Factions[0].Capital = a.Id;
            c.Factions[1].Capital = cc.Id;
            c.Factions[2].Capital = d.Id;
            c.Galaxy.Connect(a.Id, b.Id);
            c.Galaxy.Connect(b.Id, cc.Id);
            c.Galaxy.Connect(b.Id, d.Id);
            c.Begin();
            c.AiEnabled = false;
            if (war) c.SetStance(0, 1, DiplomaticStance.War);
            return c;
        }

        static Fleet Squadron(Campaign c, int owner, int system, params HullDef[] hulls)
        {
            var f = c.NewFleet(owner, system, $"Squadron {owner}");
            foreach (var h in hulls) f.Ships.Add(c.NewShip(owner, h));
            return f;
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
            var c = Line(war: false);
            var f = Squadron(c, 0, 0, Hulls.Battleship, Hulls.Corvette);
            float hours = 10f / Hulls.Battleship.LyPerDay * 24f;
            Assert.IsTrue(c.MoveFleet(f.Id, 1));
            RunHours(c, (int)hours - 1);
            Assert.IsTrue(f.InTransit, "not there yet");
            RunHours(c, 2);
            Assert.IsFalse(f.InTransit);
            Assert.AreEqual(1, f.System);
        }

        [Test]
        public void YardsBuildShipsAndRaiseArmies()
        {
            var c = Line(war: false);
            var home = Squadron(c, 0, 0, Hulls.Destroyer);
            Assert.IsTrue(c.QueueBuild(0, 0, Hulls.Destroyer));
            Assert.IsTrue(c.RaiseArmy(0, 0));
            int armies = c.Armies.Count;
            RunHours(c, (int)(Hulls.Destroyer.BuildDays * 24f) + 2);
            Assert.AreEqual(2, home.Ships.Count, "the new hull joins the fleet in the yard");
            Assert.AreEqual(armies + 1, c.Armies.Count);
        }

        [Test]
        public void ALandingTakesASystem()
        {
            var c = Line(war: false);
            var f = Squadron(c, 0, 0, Hulls.Destroyer, Hulls.Transport, Hulls.Transport);
            var a1 = c.NewArmy(0, 0);
            var a2 = c.NewArmy(0, 0);
            Assert.IsTrue(c.Embark(f.Id, a1.Id));
            Assert.IsTrue(c.Embark(f.Id, a2.Id));
            Assert.IsFalse(c.Embark(f.Id, c.NewArmy(0, 0).Id), "two transports carry two armies");
            c.MoveFleet(f.Id, 1);
            RunHours(c, 24 * 3);
            Assert.AreEqual(1, f.System);
            var forecast = c.Forecast(c.ArmiesIn(f), c.Galaxy.Systems[1]);
            Assert.IsTrue(forecast.AttackerWins);
            Assert.Greater(c.Land(f.Id), 0);
            RunHours(c, (int)forecast.Hours + 4);
            Assert.AreEqual(0, c.Galaxy.Systems[1].Owner, "the system is ours");
        }

        [Test]
        public void ParkingAFleetNoLongerTakesASystem()
        {
            var c = Line(war: false);
            var f = Squadron(c, 0, 0, Hulls.Battleship);
            c.MoveFleet(f.Id, 1);
            RunHours(c, 24 * 12);
            Assert.AreEqual(-1, c.Galaxy.Systems[1].Owner, "armies take systems, not fleets");
        }

        [Test]
        public void TroopsLiveOrDieWithTheirTransport()
        {
            var c = Line();
            var ours = Squadron(c, 0, 1, Hulls.Transport, Hulls.Frigate);
            var army = c.NewArmy(0, 0);
            army.System = -1;
            army.Fleet = ours.Id;
            Squadron(c, 1, 1, Hulls.Battleship, Hulls.Destroyer, Hulls.Destroyer);
            c.Tick();
            Assert.IsNotNull(c.Pending);
            var state = BattleSetup.AutoResolve(c);
            Assert.AreEqual(BattleState.Defeat, state);
            bool transportAfloat = c.Fleets.Contains(ours) && ours.TroopCapacity > 0;
            Assert.AreEqual(transportAfloat, c.ArmyById(army.Id) != null, "the army lives or dies with its transport");
        }

        [Test]
        public void FleetsMeetingInALaneFightThere()
        {
            var c = Line();
            // Head on along the A-B lane.
            var ours = Squadron(c, 0, 0, Hulls.Destroyer);
            var theirs = Squadron(c, 1, 1, Hulls.Destroyer);
            c.MoveFleet(ours.Id, 1);
            c.MoveFleet(theirs.Id, 0);
            for (int i = 0; i < 24 * 6 && c.Pending == null; i++) c.Tick();
            Assert.IsNotNull(c.Pending, "they meet");
            Assert.IsTrue(c.Pending.InLane, "between systems, not at one");
        }

        [Test]
        public void FogHidesDistantFleetsAndRemembersSightings()
        {
            var c = Line();
            var theirs = Squadron(c, 1, 2, Hulls.Destroyer);
            c.Tick();
            var intel = c.Intel[0];
            Assert.IsFalse(intel.VisibleFleets.Contains(theirs.Id), "20 ly away, unseen");
            var scout = Squadron(c, 0, 0, Hulls.Corvette);
            c.MoveFleet(scout.Id, 2);
            RunHours(c, 46);
            Assert.IsTrue(intel.VisibleFleets.Contains(theirs.Id), "the scout, 5 ly out, sees them");
            double seen = intel.Fleets[theirs.Id].Hour;
            c.MoveFleet(scout.Id, 1); // turn back
            RunHours(c, 60);
            Assert.IsFalse(intel.VisibleFleets.Contains(theirs.Id));
            Assert.IsTrue(intel.Fleets.ContainsKey(theirs.Id), "a last known position remains");
            Assert.Less(intel.Fleets[theirs.Id].Hour, c.Hours);
            Assert.GreaterOrEqual(intel.Fleets[theirs.Id].Hour, seen);
        }

        [Test]
        public void BreakingATreatyCostsTrustEverywhere()
        {
            var c = Line(war: false);
            c.ChangeTrust(0, 2, 40f);
            c.Factions[0].Influence = 100f;
            Assert.IsTrue(c.Propose(0, 2, DiplomaticStance.NonAggression));
            Assert.AreEqual(DiplomaticStance.NonAggression, c.Stance(0, 2));
            float before = c.Trust(0, 1);
            c.DeclareWar(0, 2);
            Assert.AreEqual(DiplomaticStance.War, c.Stance(0, 2));
            Assert.Less(c.Trust(0, 1), before, "a third party thinks less of us");
        }

        [Test]
        public void AlliesAreDrawnIntoWars()
        {
            var c = Line(war: false);
            c.ChangeTrust(0, 2, 90f);
            c.Factions[0].Influence = 100f;
            Assert.IsTrue(c.Propose(0, 2, DiplomaticStance.Alliance));
            c.DeclareWar(1, 0);
            Assert.AreEqual(DiplomaticStance.War, c.Stance(2, 1), "our ally joins in");
        }

        [Test]
        public void TheCouncilMeetsAndItsEmbargoBites()
        {
            var c = Line(war: false);
            c.Factions[0].Influence = 100f;
            Assert.IsTrue(c.TableEmbargo(0, 1));
            for (int i = 0; i < 24 * 25 && c.Council == null; i++) c.Tick();
            Assert.IsNotNull(c.Council, "a session opens");
            Assert.AreEqual(ResolutionKind.Embargo, c.Council.Kind);
            c.CastPlayerVote(Vote.Yes);
            if (c.LastCouncil.Passed) Assert.Greater(c.Factions[1].EmbargoHours, 0f);
            Assert.IsNull(c.Council);
        }

        [Test]
        public void TheCampaignMovesWithoutThePlayer()
        {
            var c = Maps.Rupture(7);
            int before = 0;
            foreach (var s in c.Galaxy.Systems) if (s.Owner == Campaign.EnemyFaction) before++;
            RunHours(c, 24 * 40);
            int after = 0;
            foreach (var s in c.Galaxy.Systems) if (s.Owner == Campaign.EnemyFaction) after++;
            TestContext.WriteLine($"day {c.Hours / 24:F0}: enemy systems {before} -> {after}, state {c.State}");
            Assert.Greater(after, before, "the enemy expands");
        }

        [Test]
        public void SameSeedSameCampaign()
        {
            var a = Maps.Rupture(8);
            var b = Maps.Rupture(8);
            RunHours(a, 24 * 30);
            RunHours(b, 24 * 30);
            for (int i = 0; i < a.Galaxy.Systems.Count; i++)
                Assert.AreEqual(a.Galaxy.Systems[i].Owner, b.Galaxy.Systems[i].Owner);
            Assert.AreEqual(a.Fleets.Count, b.Fleets.Count);
            Assert.AreEqual(a.Factions[1].Credits, b.Factions[1].Credits, 0.001f);
            Assert.AreEqual(a.Trust(1, 2), b.Trust(1, 2), 0.001f);
        }
    }
}
