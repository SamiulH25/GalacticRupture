using System.IO;
using GalacticRupture.Sim;
using GalacticRupture.Sim.Strategic;
using NUnit.Framework;

namespace GalacticRupture.Tests
{
    // The long campaign's strategy layers (IDEAS.md §1.1, §1.2): the system economy, governors,
    // reclamation, automatic landings, the designer, the generated galaxy and saves.
    public class StrategicTests
    {
        static void RunHours(Campaign c, int hours)
        {
            for (int i = 0; i < hours; i++)
            {
                if (c.Pending != null) BattleSetup.AutoResolve(c);
                if (c.Council != null) c.CastPlayerVote(Vote.Abstain);
                c.Tick();
                c.Events.Clear();
            }
        }

        // A (ours, a core world) - B (a ruin) - C (a holdout) - D (theirs).
        static Campaign Line()
        {
            var c = new Campaign(5);
            c.AddFaction("Us", true, "US", 3000f);
            c.AddFaction("Them", true, "TH", 3000f);
            var a = c.Galaxy.Add("A", 0f, 0f, 0f, 3, 0, capital: true);
            var b = c.Galaxy.Add("B", 9f, 0f, 0f, 1, -1);
            var cc = c.Galaxy.Add("C", 18f, 0f, 0f, 1, -1);
            var d = c.Galaxy.Add("D", 27f, 0f, 0f, 3, 1, capital: true);
            b.Buildings.Clear();
            b.Derelict = Derelict.Ruin;
            b.Population = 0f;
            b.Buildings.Add(new Building(BuildingKind.Refinery) { Dormant = true });
            cc.Buildings.Clear();
            cc.Derelict = Derelict.Holdout;
            cc.Population = 2f;
            c.Factions[0].Capital = a.Id;
            c.Factions[1].Capital = d.Id;
            c.Galaxy.Connect(a.Id, b.Id);
            c.Galaxy.Connect(b.Id, cc.Id);
            c.Galaxy.Connect(cc.Id, d.Id);
            c.Begin();
            c.AiEnabled = false;
            return c;
        }

        static Fleet Squadron(Campaign c, int owner, int system, params HullDef[] hulls)
        {
            var f = c.NewFleet(owner, system, $"Squadron {owner}");
            foreach (var h in hulls) f.Ships.Add(c.NewShip(owner, h));
            return f;
        }

        [Test]
        public void BuildingsTakeSlotsTimeAndMoney()
        {
            var c = Line();
            var a = c.Galaxy.Systems[0];
            float before = c.Output(a).Credits;
            int free = a.FreeSlots;
            float credits = c.Factions[0].Credits;
            Assert.IsTrue(c.Construct(0, 0, BuildingKind.Refinery));
            Assert.AreEqual(free - 1, a.FreeSlots, "under construction, it already holds the slot");
            Assert.Less(c.Factions[0].Credits, credits);
            Assert.AreEqual(before, c.Output(a).Credits, 0.01f, "not producing yet");
            RunHours(c, (int)(Campaign.BuildingDays(BuildingKind.Refinery) * 24f) + 1);
            Assert.Greater(c.Output(a).Credits, before, "a finished refinery pays");
        }

        [Test]
        public void AGovernorBuildsAndTakesACut()
        {
            var c = Line();
            var a = c.Galaxy.Systems[0];
            c.Factions[0].Influence = 50f;
            float gross = c.Output(a).Credits;
            Assert.IsTrue(c.Appoint(0, 0, 0, GovernorFocus.Economy));
            var o = c.Output(a);
            Assert.Greater(o.Cut, 0f);
            Assert.GreaterOrEqual(a.Governor.Cut, 0.03f);
            Assert.LessOrEqual(a.Governor.Cut, 0.08f);
            int buildings = a.Buildings.Count;
            RunHours(c, 24 * 2);
            Assert.Greater(a.Buildings.Count, buildings, "the governor starts building on their own");
            TestContext.WriteLine($"gross {gross:F1}, net {o.Credits:F1}, cut {o.Cut:F1}");
        }

        [Test]
        public void ReclamationNeedsAFleetOnStation()
        {
            var c = Line();
            var b = c.Galaxy.Systems[1];
            Assert.IsFalse(c.CanReclaim(0, 1), "no fleet there");
            var f = Squadron(c, 0, 0, Hulls.Corvette);
            c.MoveFleet(f.Id, 1);
            RunHours(c, 48);
            Assert.IsTrue(c.CanReclaim(0, 1));
            Assert.IsTrue(c.StartReclaim(0, 1));
            RunHours(c, 24 * 5);
            float progress = b.Reclaim;
            Assert.Greater(progress, 0f);
            c.MoveFleet(f.Id, 0);
            RunHours(c, 24 * 5);
            Assert.AreEqual(-1, b.Owner);
            c.MoveFleet(f.Id, 1);
            RunHours(c, (int)(c.ReclaimDays(b) * 24f) + 48);
            Assert.AreEqual(0, b.Owner, "restored once the expedition finishes the work");
            Assert.AreEqual(Derelict.None, b.Derelict);
            Assert.IsTrue(b.Has(BuildingKind.Refinery), "the old plant comes back online");
        }

        [Test]
        public void RuinsCannotBeTakenByLandingButHoldoutsCan()
        {
            var c = Line();
            var f = Squadron(c, 0, 0, Hulls.Destroyer, Hulls.Transport, Hulls.Transport);
            c.Embark(f.Id, c.NewArmy(0, 0).Id);
            c.Embark(f.Id, c.NewArmy(0, 0).Id);
            c.MoveFleet(f.Id, 1);
            RunHours(c, 24 * 3);
            Assert.AreEqual(2, c.CountIn(f), "nothing to fight on a ruin: the armies stay aboard");
            c.MoveFleet(f.Id, 2);
            RunHours(c, 24 * 3);
            Assert.AreEqual(0, c.CountIn(f), "they land on the holdout by themselves");
            RunHours(c, 24 * 10);
            Assert.AreEqual(0, c.Galaxy.Systems[2].Owner);
        }

        [Test]
        public void DesignsChangeGunsCostAndBuildTime()
        {
            var d = new ShipDesign("Ion Destroyer", Hulls.Destroyer);
            Assert.IsFalse(d.Set(0, Hulls.MainBattery), "a large gun does not fit a medium hardpoint");
            Assert.IsTrue(d.Set(0, Hulls.IonBattery));
            Assert.IsTrue(d.Set(2, Hulls.AutoCannon));
            var built = d.Built;
            Assert.AreEqual("destroyer", built.Id, "same hull, same model");
            Assert.AreEqual(Hulls.IonBattery, built.Hardpoints[0].Mount);
            Assert.AreEqual(Hulls.Destroyer.Cost + Hulls.IonBattery.Cost - Hulls.Secondary.Cost + Hulls.AutoCannon.Cost - Hulls.PointDefence.Cost, built.Cost);
            Assert.AreEqual(Hulls.Secondary, Hulls.Destroyer.Hardpoints[0].Mount, "the stock hull is untouched");

            var world = new World(1);
            var ship = world.Spawn(built, 0, System.Numerics.Vector3.Zero, 0f);
            Assert.AreEqual(built.Hardpoints.Count, ship.Mounts.Count);
        }

        [Test]
        public void OffersDoNotStopTheClock()
        {
            var c = Line();
            c.Incoming.Add(new Proposal { From = 1, To = 0, Treaty = DiplomaticStance.NonAggression, Expires = 1000 });
            double h = c.Hours;
            c.Tick();
            Assert.Greater(c.Hours, h);
        }

        [Test]
        public void TheGeneratedGalaxyHasTheAgreedShape()
        {
            var c = GalaxyGen.Generate(3);
            var g = c.Galaxy;
            Assert.That(g.Systems.Count, Is.InRange(90, 120));
            int majors = 0, minors = 0;
            foreach (var f in c.Factions) if (f.Major) majors++; else minors++;
            Assert.AreEqual(5, majors);
            Assert.AreEqual(10, minors);
            Assert.GreaterOrEqual(c.OldCapital, 0);
            Assert.Greater(c.DerelictsLeft, g.Systems.Count / 3, "most of the galaxy is ruins at the start");
            for (int i = 1; i < g.Systems.Count; i++)
                Assert.Less(g.Hops(0, i), int.MaxValue, "every system is reachable");
            foreach (var f in c.Factions) Assert.AreEqual(f.Id, g.Systems[f.Capital].Owner);
        }

        [Test]
        public void ASavedCampaignPlaysOnIdentically()
        {
            var a = GalaxyGen.Generate(4);
            RunHours(a, 24 * 40);
            while (!a.CanSave) RunHours(a, 1);
            var ms = new MemoryStream();
            a.Save(ms);
            var b = Campaign.Load(new MemoryStream(ms.ToArray()));
            RunHours(a, 24 * 20);
            RunHours(b, 24 * 20);
            while (!a.CanSave || !b.CanSave)
            {
                if (!a.CanSave) RunHours(a, 1);
                if (!b.CanSave) RunHours(b, 1);
            }
            var sa = new MemoryStream();
            var sb = new MemoryStream();
            a.Save(sa);
            b.Save(sb);
            CollectionAssert.AreEqual(sa.ToArray(), sb.ToArray());
        }

        [Test]
        public void TheLongCampaignMovesWithoutThePlayer()
        {
            var c = GalaxyGen.Generate(7);
            int derelicts = c.DerelictsLeft;
            RunHours(c, 24 * 120);
            Assert.Less(c.DerelictsLeft, derelicts, "the other powers reclaim the ruins");
            Assert.AreEqual(1, c.Act, "act 1 is still running after four months");
        }
    }
}
