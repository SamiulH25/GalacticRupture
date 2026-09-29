namespace GalacticRupture.Sim.Strategic
{
    // Starting maps. Positions in light-years: x across, y height (small), z along the map.
    public static class Maps
    {
        public static Campaign Rupture(ulong seed)
        {
            var c = new Campaign(seed);
            var cont = c.AddFaction("Continuance", true, "CNS", 700f);
            var rem = c.AddFaction("Remnant", true, "RSV", 700f);
            var serac = c.AddFaction("Serac Compact", false, "SCV", 350f);
            var ilium = c.AddFaction("Ilium Directorate", false, "IDS", 350f);
            var g = c.Galaxy;

            var aurel = g.Add("Aurel", -28f, 2f, 0f, 3, cont.Id, capital: true);
            var tessary = g.Add("Tessary", -20f, -1f, 9f, 2, cont.Id);
            var halden = g.Add("Halden", -21f, 1f, -10f, 1, cont.Id);
            var morrow = g.Add("Morrow's Gate", -9f, 0f, 4f, 2, -1);
            var iliumDrift = g.Add("Ilium Drift", -8f, -2f, -15f, 2, ilium.Id, capital: true);
            var sable = g.Add("Sable", -1f, 1f, -24f, 1, ilium.Id);
            var harrow = g.Add("Harrow", 8f, -1f, -22f, 1, ilium.Id);
            var seracSys = g.Add("Serac", -6f, 2f, 17f, 2, serac.Id, capital: true);
            var lumen = g.Add("Lumen", 3f, -2f, 25f, 1, serac.Id);
            var kestrel = g.Add("Kestrel Deep", 7f, -1f, 13f, 1, serac.Id);
            var oriel = g.Add("Oriel", 2f, 1f, -3f, 2, -1);
            var thule = g.Add("Thule", -16f, 1f, 22f, 1, -1);
            var vantor = g.Add("Vantor", 15f, 1f, -13f, 1, rem.Id);
            var ashfall = g.Add("Ashfall", 16f, -2f, 7f, 2, rem.Id);
            var nadir = g.Add("Nadir", 21f, 0f, -4f, 1, rem.Id);
            var veyl = g.Add("Veyl", 29f, 1f, 2f, 3, rem.Id, capital: true);

            cont.Capital = aurel.Id;
            rem.Capital = veyl.Id;
            serac.Capital = seracSys.Id;
            ilium.Capital = iliumDrift.Id;

            foreach (var (a, b) in new[]
            {
                (aurel, tessary), (aurel, halden), (tessary, morrow), (halden, morrow), (tessary, thule),
                (thule, seracSys), (tessary, seracSys), (halden, iliumDrift), (morrow, oriel), (morrow, seracSys),
                (iliumDrift, oriel), (iliumDrift, sable), (sable, harrow), (harrow, vantor), (oriel, harrow),
                (seracSys, lumen), (lumen, kestrel), (seracSys, kestrel), (kestrel, oriel), (kestrel, ashfall),
                (oriel, nadir), (oriel, vantor), (vantor, nadir), (ashfall, nadir), (ashfall, veyl), (nadir, veyl),
            })
                g.Connect(a.Id, b.Id);

            c.Begin();
            c.SetStance(cont.Id, rem.Id, DiplomaticStance.War);
            c.ChangeTrust(cont.Id, rem.Id, -40f);
            c.ChangeTrust(serac.Id, ilium.Id, -10f);
            c.ChangeTrust(cont.Id, serac.Id, 5f);
            c.ChangeTrust(cont.Id, ilium.Id, 5f);

            var home = c.NewFleet(cont.Id, aurel.Id, "Home Fleet");
            home.Ships.Add(c.NewShip(cont.Id, Hulls.Battleship));
            home.Ships.Add(c.NewShip(cont.Id, Hulls.Destroyer));
            home.Ships.Add(c.NewShip(cont.Id, Hulls.Destroyer));
            home.Ships.Add(c.NewShip(cont.Id, Hulls.Transport));
            var screen = c.NewFleet(cont.Id, tessary.Id, "Tessary Screen");
            screen.Ships.Add(c.NewShip(cont.Id, Hulls.Destroyer));
            screen.Ships.Add(c.NewShip(cont.Id, Hulls.Frigate));
            screen.Ships.Add(c.NewShip(cont.Id, Hulls.Frigate));
            c.NewArmy(cont.Id, aurel.Id);
            c.NewArmy(cont.Id, aurel.Id);
            c.NewArmy(cont.Id, tessary.Id);

            var main = c.NewFleet(rem.Id, veyl.Id, "Veyl Line");
            for (int i = 0; i < 2; i++) main.Ships.Add(c.NewShip(rem.Id, Hulls.Destroyer));
            for (int i = 0; i < 4; i++) main.Ships.Add(c.NewShip(rem.Id, Hulls.Frigate));
            for (int i = 0; i < 2; i++) main.Ships.Add(c.NewShip(rem.Id, Hulls.Transport));
            var raiders = c.NewFleet(rem.Id, ashfall.Id, "Ashfall Raiders");
            raiders.Ships.Add(c.NewShip(rem.Id, Hulls.Frigate));
            for (int i = 0; i < 6; i++) raiders.Ships.Add(c.NewShip(rem.Id, Hulls.Corvette));
            var pickets = c.NewFleet(rem.Id, vantor.Id, "Vantor Pickets");
            for (int i = 0; i < 4; i++) pickets.Ships.Add(c.NewShip(rem.Id, Hulls.Corvette));
            for (int i = 0; i < 3; i++) c.NewArmy(rem.Id, veyl.Id);

            var sguard = c.NewFleet(serac.Id, seracSys.Id, "Compact Guard");
            sguard.Ships.Add(c.NewShip(serac.Id, Hulls.Destroyer));
            for (int i = 0; i < 2; i++) sguard.Ships.Add(c.NewShip(serac.Id, Hulls.Frigate));
            sguard.Ships.Add(c.NewShip(serac.Id, Hulls.Transport));
            c.NewArmy(serac.Id, seracSys.Id);
            c.NewArmy(serac.Id, seracSys.Id);

            var iguard = c.NewFleet(ilium.Id, iliumDrift.Id, "Directorate Guard");
            iguard.Ships.Add(c.NewShip(ilium.Id, Hulls.Destroyer));
            for (int i = 0; i < 3; i++) iguard.Ships.Add(c.NewShip(ilium.Id, Hulls.Corvette));
            iguard.Ships.Add(c.NewShip(ilium.Id, Hulls.Transport));
            c.NewArmy(ilium.Id, iliumDrift.Id);
            c.NewArmy(ilium.Id, iliumDrift.Id);
            return c;
        }
    }
}
