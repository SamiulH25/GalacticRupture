using System.Collections.Generic;
using GalacticRupture.Sim;
using UnityEngine;

namespace GalacticRupture.Game
{
    // Placeholder hard-surface ships built from code, standing in until Blender models land.
    // Deck heights and hardpoint positions come from the sim's hull definitions.
    public static class ShipModels
    {
        public struct EnginePort
        {
            public Vector3 Position;
            public float Radius;
        }

        public sealed class HullModel
        {
            public Mesh Mesh;
            public List<EnginePort> Engines = new List<EnginePort>();
        }

        public sealed class TurretModel
        {
            public Mesh Gunhouse;
            public Mesh Barrels;
            public Vector3[] Muzzles;   // pitch-pivot local
            public float Recoil;        // metres the barrels run back
        }

        static readonly Dictionary<string, HullModel> HullCache = new Dictionary<string, HullModel>();
        static readonly Dictionary<string, TurretModel> TurretCache = new Dictionary<string, TurretModel>();

        public static HullModel Hull(HullDef def)
        {
            if (HullCache.TryGetValue(def.Id, out var m)) return m;
            m = def.Id switch
            {
                "battleship" => Battleship(def),
                "destroyer" => Destroyer(def),
                "frigate" => Frigate(def),
                _ when def.Class == HullClass.Station => Station(def),
                _ => Corvette(def),
            };
            HullCache[def.Id] = m;
            return m;
        }

        public static TurretModel Turret(MountDef def)
        {
            if (TurretCache.TryGetValue(def.Id, out var t)) return t;
            // The first four guns keep their original look (and have Blender models); every
            // other gun is built by family (WeaponModels).
            bool legacy = def.Id == "main_battery" || def.Id == "secondary" || def.Id == "point_defence" || def.Id == "corvette_gun";
            t = !legacy ? WeaponModels.Build(def) : def.Class switch
            {
                MountClass.Line => BuildTurret(def, width: 13f, back: -9f, front: 6f, height: 7.5f, radius: 0.85f, spacing: 2.6f, recoil: 2.4f, rangefinder: true),
                MountClass.PointDefence => BuildTurret(def, 3.2f, -1.6f, 1.4f, 2.8f, 0.22f, 0.7f, 0.5f, false),
                _ when def.Id == "corvette_gun" => BuildTurret(def, 2.6f, -1.6f, 1.3f, 2.2f, 0.2f, 0.6f, 0.4f, false),
                _ => BuildTurret(def, 6.5f, -4.5f, 3f, 4.2f, 0.45f, 1.4f, 1.0f, false),
            };
            TurretCache[def.Id] = t;
            return t;
        }

        // Hull cross-section: flat deck, chamfered shoulders, full beam just above the waterline
        // equivalent, narrowing keel.
        static Vector2[] Section(float w, float top, float bottom)
        {
            float h = top - bottom, mid = (top + bottom) * 0.5f;
            return new[]
            {
                new Vector2(0.80f * w, top),
                new Vector2(w, mid + 0.25f * h),
                new Vector2(w, mid - 0.15f * h),
                new Vector2(0.55f * w, bottom),
                new Vector2(-0.55f * w, bottom),
                new Vector2(-w, mid - 0.15f * h),
                new Vector2(-w, mid + 0.25f * h),
                new Vector2(-0.80f * w, top),
            };
        }

        static void HullLoft(MeshBuilder b, float[,] stations)
        {
            var rings = new List<Vector2[]>();
            var zs = new List<float>();
            // Stations are listed bow first; loft runs stern to bow.
            for (int i = stations.GetLength(0) - 1; i >= 0; i--)
            {
                zs.Add(stations[i, 0]);
                rings.Add(Section(stations[i, 1], stations[i, 2], stations[i, 3]));
            }
            b.Loft(rings, zs, Slot.Paint);
        }

        static void Nozzle(MeshBuilder b, HullModel m, Vector3 at, float r, float length)
        {
            b.Cylinder(at, at + new Vector3(0f, 0f, -length), r * 0.85f, r * 1.1f, 16, Slot.Dark);
            b.Cylinder(at + new Vector3(0f, 0f, -length * 0.35f), at + new Vector3(0f, 0f, -length * 0.36f), r * 0.8f, r * 0.8f, 16, Slot.Engine);
            m.Engines.Add(new EnginePort { Position = at + new Vector3(0f, 0f, -length), Radius = r });
        }

        // Rows of lit ports along both sides: the crew are home.
        static void PortLights(MeshBuilder b, float zFrom, float zTo, float y, System.Func<float, float> halfBeamAt, int seed)
        {
            var rng = new System.Random(seed);
            for (float z = zFrom; z < zTo; z += 5f)
            {
                if (rng.NextDouble() < 0.35) continue;
                float x = halfBeamAt(z) + 0.08f;
                b.Box(new Vector3(x, y, z), new Vector3(0.1f, 0.7f, 1.6f), Slot.Windows);
                if (rng.NextDouble() < 0.7)
                    b.Box(new Vector3(-x, y, z), new Vector3(0.1f, 0.7f, 1.6f), Slot.Windows);
            }
        }

        // An orbital fortress: a gun deck on a stepped drum, a citadel carrying the heavy guns,
        // docking arms with lit berths, radiators and a sensor spire. Every gun rides on top or
        // on the rim ledge (Hulls.BuildStation); the structure below is hull, not guns.
        static HullModel Station(HullDef def)
        {
            var m = new HullModel();
            var b = new MeshBuilder(Slot.Count);
            float r = def.Radius, deck = Sim.Hulls.StationDeck;
            const int seg = 40;

            // Deck, rim ledge, mid deck, citadel.
            b.Cylinder(new Vector3(0f, deck - 8f, 0f), new Vector3(0f, deck, 0f), r * 0.85f, r * 0.85f, seg, Slot.Paint);
            b.Cylinder(new Vector3(0f, deck - 10f, 0f), new Vector3(0f, deck - 4f, 0f), r * 1.1f, r * 1.1f, seg, Slot.Dark);
            b.Cylinder(new Vector3(0f, deck - 0.2f, 0f), new Vector3(0f, deck + 6f, 0f), r * 0.55f, r * 0.55f, seg, Slot.Paint);
            b.Cylinder(new Vector3(0f, deck + 5.8f, 0f), new Vector3(0f, deck + 12f, 0f), r * 0.4f, r * 0.38f, seg, Slot.Dark);
            // Deck-edge trim and a band of lit ports under the gun deck.
            b.Cylinder(new Vector3(0f, deck - 0.4f, 0f), new Vector3(0f, deck + 0.3f, 0f), r * 0.86f, r * 0.86f, seg, Slot.Trim);
            for (int i = 0; i < 72; i++)
            {
                if ((i * 7) % 5 == 0) continue;
                float a = i * Mathf.PI * 2f / 72f;
                var dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                var saved = b.Transform;
                b.Transform = Matrix4x4.TRS(dir * (r * 0.85f + 0.05f) + Vector3.up * (deck - 5f), Quaternion.LookRotation(dir), Vector3.one);
                b.Box(Vector3.zero, new Vector3(2.2f, 0.9f, 0.12f), Slot.Windows);
                b.Transform = saved;
            }

            // The drum below: tapering hull down to the reactor and a sensor spire.
            b.Cylinder(new Vector3(0f, deck - 10f, 0f), new Vector3(0f, -r * 0.45f, 0f), r * 0.7f, r * 0.42f, seg, Slot.Paint);
            b.Cylinder(new Vector3(0f, -r * 0.45f, 0f), new Vector3(0f, -r * 0.8f, 0f), r * 0.42f, r * 0.2f, seg, Slot.Dark);
            b.Cylinder(new Vector3(0f, -r * 0.8f, 0f), new Vector3(0f, -r * 1.35f, 0f), r * 0.06f, r * 0.02f, 8, Slot.Dark);
            b.Cylinder(new Vector3(0f, -r * 0.62f, 0f), new Vector3(0f, -r * 0.64f, 0f), r * 0.36f, r * 0.36f, seg, Slot.Heat);

            // Command block on the citadel, between the heavy guns.
            b.ChamferBox(new Vector3(0f, deck + 16f, 0f), new Vector3(r * 0.16f, 8f, r * 0.16f), 1.2f, Slot.Paint, 0.85f);
            b.Box(new Vector3(0f, deck + 17f, r * 0.08f + 0.05f), new Vector3(r * 0.12f, 1.2f, 0.12f), Slot.Windows);
            b.Cylinder(new Vector3(0f, deck + 20f, 0f), new Vector3(0f, deck + 38f, 0f), 0.9f, 0.4f, 8, Slot.Dark);
            b.Box(new Vector3(0f, deck + 38.4f, 0f), new Vector3(0.7f, 0.7f, 0.7f), Slot.Heat);

            // Docking arms on the diagonals, below the guns' arcs: a spar, a berth, lit windows.
            for (int k = 0; k < 4; k++)
            {
                float yaw = 45f + k * 90f;
                var saved = b.Transform;
                b.Transform = Matrix4x4.TRS(Vector3.up * (deck - 22f), Quaternion.Euler(0f, yaw, 0f), Vector3.one);
                float from = r * 0.6f, to = r * 1.45f;
                b.ChamferBox(new Vector3(0f, 0f, (from + to) * 0.5f), new Vector3(9f, 7f, to - from), 1.2f, Slot.Paint);
                b.ChamferBox(new Vector3(0f, -1f, to + 6f), new Vector3(22f, 12f, 14f), 1.5f, Slot.Dark);
                b.Box(new Vector3(0f, 6f, to + 6f), new Vector3(16f, 0.4f, 10f), Slot.Trim);
                for (float z = from + 4f; z < to - 2f; z += 6f)
                {
                    b.Box(new Vector3(4.56f, 0.5f, z), new Vector3(0.1f, 1.1f, 2.6f), Slot.Windows);
                    b.Box(new Vector3(-4.56f, 0.5f, z), new Vector3(0.1f, 1.1f, 2.6f), Slot.Windows);
                }
                b.Transform = saved;
            }

            // Radiators between the arms: canted panels with hot stripes.
            for (int k = 0; k < 4; k++)
            {
                var saved = b.Transform;
                b.Transform = Matrix4x4.TRS(Vector3.up * (-r * 0.2f), Quaternion.Euler(0f, k * 90f, 0f), Vector3.one)
                    * Matrix4x4.TRS(new Vector3(0f, 0f, r * 0.62f), Quaternion.Euler(-20f, 0f, 0f), Vector3.one);
                b.Box(new Vector3(0f, 0f, r * 0.25f), new Vector3(r * 0.35f, 0.8f, r * 0.5f), Slot.Dark);
                for (int j = -2; j <= 2; j++)
                    b.Box(new Vector3(j * r * 0.06f, 0.45f, r * 0.25f), new Vector3(0.4f, 0.1f, r * 0.46f), Slot.Heat);
                b.Transform = saved;
            }

            // Gun tubs under the mounts on the rim ledge and the mid deck.
            foreach (var hp in def.Hardpoints)
            {
                var p = hp.LocalPosition.ToUnity();
                if (hp.Mount.Class == MountClass.PointDefence || p.y < deck - 1f)
                    b.Cylinder(new Vector3(p.x, p.y - 1.4f, p.z), new Vector3(p.x, p.y + 0.1f, p.z), 3.4f, 3f, 12, Slot.Dark);
            }

            m.Mesh = b.ToMesh(def.Id);
            return m;
        }

        static HullModel Battleship(HullDef def)
        {
            var m = new HullModel();
            var b = new MeshBuilder(Slot.Count);
            float deck = Sim.Hulls.BattleshipDeck;
            HullLoft(b, new float[,]
            {
                { 180f, 1.2f, 10f, -1f },
                { 162f, 9f, deck, -9f },
                { 132f, 17f, deck, -15f },
                { 92f, 22f, deck, -18f },
                { -110f, 23f, deck, -18f },
                { -150f, 21f, deck, -15f },
                { -172f, 19f, deck, -12f },
            });

            // Armour belt and deck-edge trim.
            foreach (float s in new[] { -1f, 1f })
            {
                b.Box(new Vector3(s * 23.1f, 1.5f, -8f), new Vector3(1.4f, 7f, 190f), Slot.Dark);
                b.Box(new Vector3(s * 18.1f, deck + 0.25f, -10f), new Vector3(0.7f, 0.5f, 196f), Slot.Trim);
            }

            // Barbettes: superfiring B and X, low rings for A and Y.
            b.Cylinder(new Vector3(0f, deck - 1f, 72f), new Vector3(0f, Sim.Hulls.BattleshipSuperfire, 72f), 9.5f, 9f, 24, Slot.Paint);
            b.Cylinder(new Vector3(0f, deck - 1f, -66f), new Vector3(0f, Sim.Hulls.BattleshipSuperfire, -66f), 9.5f, 9f, 24, Slot.Paint);
            b.Cylinder(new Vector3(0f, deck - 0.5f, 104f), new Vector3(0f, deck + 0.8f, 104f), 8.8f, 8.6f, 24, Slot.Dark);
            b.Cylinder(new Vector3(0f, deck - 0.5f, -98f), new Vector3(0f, deck + 0.8f, -98f), 8.8f, 8.6f, 24, Slot.Dark);

            // Citadel, bridge tower, mast.
            b.ChamferBox(new Vector3(0f, 19f, -8f), new Vector3(28f, 14f, 86f), 3f, Slot.Paint, 0.85f);
            b.Box(new Vector3(0f, 26.2f, -8f), new Vector3(20f, 0.4f, 78f), Slot.Dark);
            b.ChamferBox(new Vector3(0f, 32f, 22f), new Vector3(18f, 12f, 22f), 2f, Slot.Paint, 0.8f);
            b.ChamferBox(new Vector3(0f, 39.5f, 25f), new Vector3(24f, 3.5f, 11f), 1f, Slot.Paint);
            b.Box(new Vector3(0f, 39.6f, 30.6f), new Vector3(20f, 1.1f, 0.3f), Slot.Windows);
            foreach (float s in new[] { -1f, 1f })
                b.Box(new Vector3(s * 12.05f, 39.6f, 25f), new Vector3(0.2f, 1.1f, 8f), Slot.Windows);
            b.Cylinder(new Vector3(0f, 41f, 21f), new Vector3(0f, 66f, 21f), 1.3f, 0.5f, 8, Slot.Dark);
            b.Box(new Vector3(0f, 58f, 21f), new Vector3(16f, 0.6f, 0.6f), Slot.Dark);
            b.Box(new Vector3(0f, 50f, 21f), new Vector3(10f, 0.5f, 0.5f), Slot.Dark);
            b.Box(new Vector3(0f, 66.4f, 21f), new Vector3(0.6f, 0.6f, 0.6f), Slot.Heat);

            // Aft stack: radiator housing with two canted radiator panels.
            b.ChamferBox(new Vector3(0f, 29f, -40f), new Vector3(16f, 8f, 30f), 2f, Slot.Paint, 0.8f);
            foreach (float s in new[] { -1f, 1f })
            {
                var saved = b.Transform;
                b.Transform = Matrix4x4.TRS(new Vector3(s * 5f, 33f, -40f), Quaternion.Euler(0f, 0f, -s * 22f), Vector3.one)
                    * Matrix4x4.Translate(new Vector3(s * 4f, 6f, 0f));
                b.Box(Vector3.zero, new Vector3(0.8f, 14f, 28f), Slot.Dark);
                for (int k = -2; k <= 2; k++)
                    b.Box(new Vector3(s * 0.45f, k * 2.6f, 0f), new Vector3(0.1f, 0.35f, 26f), Slot.Heat);
                b.Transform = saved;
            }

            // Secondary sponsons and point-defence tubs.
            foreach (var hp in def.Hardpoints)
            {
                var p = hp.LocalPosition.ToUnity();
                if (hp.Mount.Class == MountClass.Secondary)
                    b.ChamferBox(new Vector3(Mathf.Sign(p.x) * 21f, p.y - 1.6f, p.z), new Vector3(8f, 3.2f, 13f), 1f, Slot.Paint);
                else if (hp.Mount.Class == MountClass.PointDefence)
                    b.Cylinder(new Vector3(p.x, p.y - 1.2f, p.z), new Vector3(p.x, p.y + 0.2f, p.z), 2.6f, 2.3f, 12, Slot.Dark);
            }

            PortLights(b, -140f, 120f, 3.5f, z => Mathf.Lerp(22f, 23f, Mathf.InverseLerp(92f, -110f, z)), 11);
            PortLights(b, -48f, 32f, 16f, z => 14f * 0.95f, 12);

            // Engines: three mains, two manoeuvring.
            Nozzle(b, m, new Vector3(0f, -2f, -172f), 7f, 10f);
            Nozzle(b, m, new Vector3(-12.5f, -2f, -171f), 6f, 9f);
            Nozzle(b, m, new Vector3(12.5f, -2f, -171f), 6f, 9f);
            Nozzle(b, m, new Vector3(-9f, 7.5f, -172f), 3f, 5f);
            Nozzle(b, m, new Vector3(9f, 7.5f, -172f), 3f, 5f);

            m.Mesh = b.ToMesh("battleship");
            return m;
        }

        static HullModel Destroyer(HullDef def)
        {
            var m = new HullModel();
            var b = new MeshBuilder(Slot.Count);
            HullLoft(b, new float[,]
            {
                { 74f, 1f, 5f, -1f },
                { 62f, 5f, 6f, -5f },
                { 40f, 9f, 6f, -8f },
                { -50f, 10f, 6f, -8f },
                { -66f, 9f, 6f, -6f },
                { -72f, 8f, 6f, -5f },
            });
            foreach (float s in new[] { -1f, 1f })
                b.Box(new Vector3(s * 8.05f, 6.15f, -6f), new Vector3(0.4f, 0.3f, 100f), Slot.Trim);
            b.Cylinder(new Vector3(0f, 5.5f, 40f), new Vector3(0f, 6.6f, 40f), 4.4f, 4.2f, 16, Slot.Dark);
            b.Cylinder(new Vector3(0f, 5.5f, -36f), new Vector3(0f, 6.6f, -36f), 4.4f, 4.2f, 16, Slot.Dark);
            b.ChamferBox(new Vector3(0f, 10.5f, 0f), new Vector3(11f, 9f, 34f), 1.5f, Slot.Paint, 0.85f);
            b.ChamferBox(new Vector3(0f, 17f, 8f), new Vector3(9f, 4f, 9f), 1f, Slot.Paint, 0.85f);
            b.Box(new Vector3(0f, 17.3f, 12.6f), new Vector3(7f, 0.8f, 0.2f), Slot.Windows);
            b.Cylinder(new Vector3(0f, 19f, 6f), new Vector3(0f, 30f, 6f), 0.6f, 0.25f, 6, Slot.Dark);
            b.Box(new Vector3(0f, 26f, 6f), new Vector3(7f, 0.3f, 0.3f), Slot.Dark);
            foreach (var hp in def.Hardpoints)
                if (hp.Mount.Class == MountClass.PointDefence)
                    b.Cylinder(hp.LocalPosition.ToUnity() + new Vector3(0f, -1.4f, 0f), hp.LocalPosition.ToUnity() + new Vector3(0f, 0.2f, 0f), 1.8f, 1.6f, 10, Slot.Dark);
            PortLights(b, -60f, 50f, 1.5f, z => 10f, 21);
            Nozzle(b, m, new Vector3(-4f, -0.5f, -72f), 3.6f, 5f);
            Nozzle(b, m, new Vector3(4f, -0.5f, -72f), 3.6f, 5f);
            m.Mesh = b.ToMesh("destroyer");
            return m;
        }

        // Remnant: keeled, one unbroken line bow to stern.
        static HullModel Frigate(HullDef def)
        {
            var m = new HullModel();
            var b = new MeshBuilder(Slot.Count);
            HullLoft(b, new float[,]
            {
                { 46f, 0.6f, 3f, -1.5f },
                { 30f, 5f, 4.5f, -6f },
                { 0f, 7f, 4.5f, -9f },
                { -30f, 6f, 4.5f, -8f },
                { -42f, 5f, 4f, -5f },
            });
            b.ChamferBox(new Vector3(0f, 9f, -6f), new Vector3(3.2f, 9f, 34f), 1f, Slot.Paint, 0.35f);
            b.Box(new Vector3(0f, -12.5f, -2f), new Vector3(1.4f, 7f, 58f), Slot.Dark);
            b.Box(new Vector3(0f, 13.6f, -6f), new Vector3(0.3f, 0.3f, 30f), Slot.Trim);
            b.Box(new Vector3(0f, 7f, 12f), new Vector3(3.4f, 0.7f, 0.2f), Slot.Windows);
            PortLights(b, -34f, 30f, 0.5f, z => 6.8f, 31);
            Nozzle(b, m, new Vector3(0f, -1f, -42f), 3.2f, 4f);
            Nozzle(b, m, new Vector3(-4.2f, 1.2f, -41f), 1.8f, 3f);
            Nozzle(b, m, new Vector3(4.2f, 1.2f, -41f), 1.8f, 3f);
            m.Mesh = b.ToMesh("frigate");
            return m;
        }

        static HullModel Corvette(HullDef def)
        {
            var m = new HullModel();
            var b = new MeshBuilder(Slot.Count);
            HullLoft(b, new float[,]
            {
                { 24f, 0.5f, 2f, -1f },
                { 12f, 4f, 3f, -3f },
                { -10f, 5f, 3f, -4f },
                { -22f, 4.5f, 2.5f, -3f },
            });
            b.ChamferBox(new Vector3(0f, 5.2f, -8f), new Vector3(2f, 5f, 16f), 0.6f, Slot.Paint, 0.35f);
            b.Box(new Vector3(0f, -6f, -4f), new Vector3(1f, 4f, 26f), Slot.Dark);
            b.Box(new Vector3(0f, 3.2f, 2f), new Vector3(2.4f, 0.5f, 0.2f), Slot.Windows);
            Nozzle(b, m, new Vector3(-2.4f, 0f, -22f), 1.8f, 2.5f);
            Nozzle(b, m, new Vector3(2.4f, 0f, -22f), 1.8f, 2.5f);
            m.Mesh = b.ToMesh("corvette");
            return m;
        }

        // Gunhouse on the yaw pivot; barrels and mantlet on the pitch pivot at PivotHeight.
        static TurretModel BuildTurret(MountDef def, float width, float back, float front, float height,
            float radius, float spacing, float recoil, bool rangefinder)
        {
            float pivotY = def.PivotHeight;
            var g = new MeshBuilder(Slot.Count);
            float hw = width * 0.5f;
            float c = Mathf.Min(width, height) * 0.14f;
            Vector2[] Ring(float top, float halfW) => new[]
            {
                new Vector2(halfW - c, top), new Vector2(halfW, top - c), new Vector2(halfW * 1.04f, 0.3f), new Vector2(-halfW * 1.04f, 0.3f),
                new Vector2(-halfW, top - c), new Vector2(-halfW + c, top),
            };
            float roofFront = Mathf.Max(height * 0.62f, pivotY + radius * 2.5f);
            g.Loft(new[] { Ring(height, hw * 0.96f), Ring(height, hw), Ring(roofFront, hw * 0.9f) },
                new[] { back, back + (front - back) * 0.55f, front }, Slot.Paint);
            if (rangefinder)
            {
                g.Box(new Vector3(0f, height - 1.2f, back + 2f), new Vector3(width * 1.35f, 1.1f, 1.4f), Slot.Dark);
                g.Box(new Vector3(0f, height + 0.6f, back + 3f), new Vector3(2.4f, 1.2f, 3f), Slot.Paint);
                g.Box(new Vector3(0f, height + 0.05f, back + 1f), new Vector3(width * 0.8f, 0.1f, 0.3f), Slot.Trim);
            }
            g.Cylinder(new Vector3(0f, -0.4f, 0f), new Vector3(0f, 0.6f, 0f), hw * 1.02f, hw * 1.0f, 20, Slot.Dark);

            var bar = new MeshBuilder(Slot.Count);
            float L = def.MuzzleLength;
            int n = Mathf.Max(def.Barrels, 1);
            var muzzles = new Vector3[n];
            bar.ChamferBox(new Vector3(0f, 0f, front - 0.6f), new Vector3(spacing * n + radius * 3f, radius * 4.2f, 1.8f), radius, Slot.Dark);
            for (int i = 0; i < n; i++)
            {
                float x = (i - (n - 1) * 0.5f) * spacing;
                bar.Cylinder(new Vector3(x, 0f, front), new Vector3(x, 0f, front + L * 0.25f), radius * 1.7f, radius * 1.45f, 12, Slot.Dark);
                bar.Cylinder(new Vector3(x, 0f, front), new Vector3(x, 0f, L - radius * 1.6f), radius * 1.15f, radius, 12, Slot.Dark);
                bar.Cylinder(new Vector3(x, 0f, L - radius * 1.6f), new Vector3(x, 0f, L), radius * 1.45f, radius * 1.4f, 12, Slot.Dark);
                muzzles[i] = new Vector3(x, 0f, L);
            }

            // Barrels hang from the pitch pivot, which sits PivotHeight up the gunhouse.
            var mesh = bar.ToMesh(def.Id + "_barrels");
            return new TurretModel
            {
                Gunhouse = g.ToMesh(def.Id + "_house"),
                Barrels = mesh,
                Muzzles = muzzles,
                Recoil = recoil,
            };
        }
    }
}
