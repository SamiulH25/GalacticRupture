using System.Numerics;

namespace GalacticRupture.Sim
{
    // What a battlefield holds, before it is laid out.
    public struct TerrainSpec
    {
        public int Asteroids;   // 0 open space, 1 scattered rock, 2 an asteroid field, 3 a belt across the field
        public bool Nebula;

        public string Describe()
        {
            string rock = Asteroids switch
            {
                1 => "scattered asteroids",
                2 => "an asteroid field",
                3 => "a dense asteroid belt",
                _ => "open space",
            };
            return Nebula ? $"{rock}, a nebula" : rock;
        }
    }

    // Lays out the terrain of a battle. Fleets arrive along the z axis (side 0 from -z, side 1
    // from +z) and deploy beyond |z| = 2200, so rock keeps to the middle of the field and to the
    // flanks: it shapes the fight without walling in either deployment.
    public static class Terrain
    {
        public const float DeployClear = 2200f;

        public static void Populate(World world, TerrainSpec spec, ulong seed)
        {
            var rng = new Rng(seed ^ 0x7E44A1B5C0FFEEUL);
            int rockSeed = 1;

            void Rock(Vector3 p, float r)
            {
                if (System.MathF.Abs(p.Z) + r > DeployClear + 400f) return;
                foreach (var o in world.Obstacles)
                    if (o.Kind == ObstacleKind.Asteroid && Vector3.Distance(o.Position, p) < o.Radius + r + 40f) return;
                world.AddObstacle(ObstacleKind.Asteroid, p, r, rockSeed++);
            }

            void Cluster(Vector3 centre, int count, float spread, float maxR)
            {
                for (int i = 0; i < count; i++)
                {
                    float a = rng.Range(0f, SimMath.Pi * 2f);
                    float d = spread * SimMath.Sqrt(rng.NextFloat());
                    var p = centre + new Vector3(SimMath.Sin(a) * d, rng.Range(-120f, 120f), SimMath.Cos(a) * d);
                    // Mostly boulders, a few mountains.
                    float r = rng.NextFloat() < 0.2f ? rng.Range(maxR * 0.6f, maxR) : rng.Range(50f, maxR * 0.45f);
                    Rock(p, r);
                }
            }

            switch (spec.Asteroids)
            {
                case 1:
                    for (int i = 0; i < 3; i++)
                        Cluster(new Vector3(rng.Range(-4200f, 4200f), 0f, rng.Range(-1500f, 1500f)), 4, 500f, 220f);
                    break;
                case 2:
                    for (int i = 0; i < 3; i++)
                        Cluster(new Vector3(rng.Range(-3800f, 3800f), 0f, rng.Range(-1400f, 1400f)), 9, 800f, 320f);
                    break;
                case 3:
                {
                    // A belt across the field with a few gaps: the gaps are the fight.
                    float z = rng.Range(-500f, 500f);
                    float tilt = rng.Range(-0.18f, 0.18f);
                    int gaps = 2 + rng.Range(0, 2);
                    var gapAt = new float[gaps];
                    for (int g = 0; g < gaps; g++) gapAt[g] = rng.Range(-4000f, 4000f);
                    for (float x = -6500f; x <= 6500f; x += rng.Range(150f, 320f))
                    {
                        bool gap = false;
                        foreach (float g in gapAt) gap |= System.MathF.Abs(x - g) < 450f;
                        if (gap) continue;
                        Rock(new Vector3(x, rng.Range(-100f, 100f), z + x * tilt + rng.Range(-260f, 260f)), rng.Range(90f, 300f));
                    }
                    Cluster(new Vector3(rng.Range(-3000f, 3000f), 0f, z + rng.Range(-1100f, 1100f)), 6, 600f, 260f);
                    break;
                }
            }

            if (spec.Nebula)
            {
                float side = rng.NextFloat() < 0.5f ? -1f : 1f;
                var centre = new Vector3(side * rng.Range(1800f, 3800f), rng.Range(-150f, 150f), rng.Range(-1100f, 1100f));
                world.AddObstacle(ObstacleKind.Nebula, centre, rng.Range(1300f, 1900f), (int)(seed & 0xffff));
            }
        }
    }
}
