namespace GalacticRupture.Sim.Strategic
{
    // Starting maps. Positions in light-years: x across, y height (small), z along the map.
    public static class Maps
    {
        public static Campaign Rupture(ulong seed)
        {
            var c = new Campaign(seed);
            c.Factions[0] = new Faction { Id = 0, Name = "Continuance", Credits = 700f, ShipPrefix = "CNS" };
            c.Factions[1] = new Faction { Id = 1, Name = "Remnant", Credits = 700f, ShipPrefix = "RSV" };
            var g = c.Galaxy;

            var aurel = g.Add("Aurel", -28f, 2f, 0f, 3, 0, capital: true);
            var tessary = g.Add("Tessary", -20f, -1f, 9f, 2, 0);
            var halden = g.Add("Halden", -21f, 1f, -10f, 1, 0);
            var morrow = g.Add("Morrow's Gate", -9f, 0f, 4f, 2, -1);
            var ilium = g.Add("Ilium Drift", -8f, -2f, -13f, 1, -1);
            var serac = g.Add("Serac", -6f, 2f, 16f, 1, -1);
            var oriel = g.Add("Oriel", 3f, 1f, -3f, 2, -1);
            var kestrel = g.Add("Kestrel Deep", 6f, -1f, 12f, 1, -1);
            var vantor = g.Add("Vantor", 12f, 1f, -14f, 1, 1);
            var ashfall = g.Add("Ashfall", 16f, -2f, 6f, 2, 1);
            var nadir = g.Add("Nadir", 20f, 0f, -6f, 1, 1);
            var veyl = g.Add("Veyl", 28f, 1f, 1f, 3, 1, capital: true);

            foreach (var (a, b) in new[]
            {
                (aurel, tessary), (aurel, halden), (tessary, morrow), (halden, morrow), (tessary, serac),
                (halden, ilium), (morrow, oriel), (ilium, oriel), (serac, kestrel), (morrow, serac),
                (oriel, kestrel), (oriel, vantor), (ilium, vantor), (kestrel, ashfall), (oriel, nadir),
                (vantor, nadir), (ashfall, nadir), (ashfall, veyl), (nadir, veyl),
            })
                g.Connect(a.Id, b.Id);

            var home = c.NewFleet(0, aurel.Id, "Home Fleet");
            home.Ships.Add(c.NewShip(0, Hulls.Battleship));
            home.Ships.Add(c.NewShip(0, Hulls.Destroyer));
            home.Ships.Add(c.NewShip(0, Hulls.Destroyer));
            var screen = c.NewFleet(0, tessary.Id, "Tessary Screen");
            screen.Ships.Add(c.NewShip(0, Hulls.Destroyer));
            screen.Ships.Add(c.NewShip(0, Hulls.Frigate));
            screen.Ships.Add(c.NewShip(0, Hulls.Frigate));

            var main = c.NewFleet(1, veyl.Id, "Veyl Line");
            for (int i = 0; i < 2; i++) main.Ships.Add(c.NewShip(1, Hulls.Destroyer));
            for (int i = 0; i < 4; i++) main.Ships.Add(c.NewShip(1, Hulls.Frigate));
            var raiders = c.NewFleet(1, ashfall.Id, "Ashfall Raiders");
            raiders.Ships.Add(c.NewShip(1, Hulls.Frigate));
            for (int i = 0; i < 6; i++) raiders.Ships.Add(c.NewShip(1, Hulls.Corvette));
            var pickets = c.NewFleet(1, vantor.Id, "Vantor Pickets");
            for (int i = 0; i < 4; i++) pickets.Ships.Add(c.NewShip(1, Hulls.Corvette));
            return c;
        }
    }
}
