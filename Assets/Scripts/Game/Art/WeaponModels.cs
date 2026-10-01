using System.Collections.Generic;
using GalacticRupture.Sim;
using UnityEngine;

namespace GalacticRupture.Game
{
    // One silhouette per weapon family (IDEAS.md §2.1: the guns are the signature image), so a
    // flak mount, a line battery and an arc cannon read apart at a glance and a heavy plasma cannon
    // reads as the big brother of a light one. Built from code until Blender models replace them;
    // a model named for the mount id (GameArt.TurretModels) always wins.
    //
    // Yaw-local gunhouse: y up from the ring, z forward. Barrels are pitch-local: the trunnion
    // at the origin, the muzzles out along +z at the mount's MuzzleLength.
    public static class WeaponModels
    {
        public static float Scale(MountSize s) => s switch { MountSize.Small => 0.45f, MountSize.Medium => 1f, _ => 2f };

        public static ShipModels.TurretModel Build(MountDef def)
        {
            var house = new MeshBuilder(Slot.Count);
            var bar = new MeshBuilder(Slot.Count);
            var muzzles = new List<Vector3>();
            float k = Scale(def.Size);
            float p = def.PivotHeight;
            float L = def.MuzzleLength;
            float recoil = 0.6f * k;

            switch (def.Family)
            {
                case WeaponFamily.MassDriver:
                {
                    // Angular armoured gunhouse, thick barrels with muzzle brakes.
                    Gunhouse(house, 6.5f * k, -4.5f * k, 3f * k, p * 1.9f, angular: true);
                    if (def.Size == MountSize.Large) house.Box(new Vector3(0f, p * 1.9f + 0.4f * k, -3f * k), new Vector3(9f * k, 0.9f * k, 1.2f * k), Slot.Dark);
                    Barrels(bar, muzzles, def.Barrels, 1.3f * k, 0.42f * k, 3f * k, L, brake: true);
                    recoil = 1.2f * k;
                    break;
                }
                case WeaponFamily.Autocannon:
                {
                    Gunhouse(house, 2.8f, -1.8f, 1.2f, p * 1.6f, angular: true);
                    Barrels(bar, muzzles, 1, 0f, 0.22f, 1.2f, L, brake: true);
                    // The ammunition drum on the side.
                    bar.Cylinder(new Vector3(0.9f, -0.2f, 0.2f), new Vector3(1.5f, -0.2f, 0.2f), 0.7f, 0.7f, 14, Slot.Dark);
                    recoil = 0.5f;
                    break;
                }
                case WeaponFamily.Rotary:
                {
                    // A round drum turret; a ring of barrels spinning round the axis.
                    Dome(house, 3.2f * k, p * 1.7f, Slot.Paint);
                    float ring = 0.55f * k * (def.Barrels >= 4 ? 1.1f : 1f);
                    bar.Cylinder(new Vector3(0f, 0f, 1.2f * k), new Vector3(0f, 0f, 2.6f * k), ring * 1.9f, ring * 1.7f, 16, Slot.Dark);
                    for (int i = 0; i < def.Barrels; i++)
                    {
                        float a = i * Mathf.PI * 2f / def.Barrels;
                        var o = new Vector3(Mathf.Cos(a) * ring, Mathf.Sin(a) * ring, 0f);
                        bar.Cylinder(o + new Vector3(0f, 0f, 2.6f * k), o + new Vector3(0f, 0f, L), 0.18f * k, 0.16f * k, 8, Slot.Dark);
                        muzzles.Add(o + new Vector3(0f, 0f, L));
                    }
                    bar.Cylinder(new Vector3(0f, 0f, L * 0.62f), new Vector3(0f, 0f, L * 0.66f), ring * 1.5f, ring * 1.5f, 16, Slot.Trim);
                    recoil = 0.1f;
                    break;
                }
                case WeaponFamily.Railgun:
                {
                    // Low flat wedge; two long flat rails with a glowing gap between them.
                    Gunhouse(house, 6f * k, -4f * k, 3.5f * k, p * 1.45f, angular: true, taper: 0.75f);
                    float gap = 0.5f * k;
                    float rails = def.Barrels;
                    for (int r = 0; r < rails; r++)
                    {
                        float x = (r - (rails - 1) * 0.5f) * 2.4f * k;
                        bar.Box(new Vector3(x, 0f, 2f * k), new Vector3(2.2f * k, 1.6f * k, 3f * k), Slot.Dark);
                        bar.Box(new Vector3(x - gap, 0f, L * 0.5f + 1.5f * k), new Vector3(0.35f * k, 0.9f * k, L - 3f * k), Slot.Paint);
                        bar.Box(new Vector3(x + gap, 0f, L * 0.5f + 1.5f * k), new Vector3(0.35f * k, 0.9f * k, L - 3f * k), Slot.Paint);
                        bar.Box(new Vector3(x, 0f, L * 0.5f + 1.5f * k), new Vector3(0.25f * k, 0.3f * k, L - 3.5f * k), Slot.Engine);
                        muzzles.Add(new Vector3(x, 0f, L));
                    }
                    recoil = 0.4f * k;
                    break;
                }
                case WeaponFamily.Accelerator:
                {
                    // A squat heavy base under one enormous barrel wound with glowing coils.
                    Gunhouse(house, 7.5f * k, -5f * k, 3.5f * k, p * 1.7f, angular: true);
                    bar.ChamferBox(new Vector3(0f, 0f, 1.5f * k), new Vector3(4f * k, 3.4f * k, 4f * k), 0.5f * k, Slot.Dark);
                    bar.Cylinder(new Vector3(0f, 0f, 3f * k), new Vector3(0f, 0f, L), 0.95f * k, 0.8f * k, 18, Slot.Dark);
                    for (int i = 0; i < 6; i++)
                    {
                        float z = 4.5f * k + i * (L - 7f * k) / 6f;
                        bar.Cylinder(new Vector3(0f, 0f, z), new Vector3(0f, 0f, z + 0.9f * k), 1.45f * k, 1.45f * k, 18, Slot.Paint);
                        bar.Cylinder(new Vector3(0f, 0f, z + 0.9f * k), new Vector3(0f, 0f, z + 1.2f * k), 1.3f * k, 1.3f * k, 18, Slot.Engine);
                    }
                    muzzles.Add(new Vector3(0f, 0f, L));
                    recoil = 2f * k;
                    break;
                }
                case WeaponFamily.Plasma:
                {
                    // A rounded dome; short thick emitters with cooling fins and hot tips.
                    Dome(house, (def.Size == MountSize.Small ? 2.8f : 3.6f) * k, p * 1.75f, Slot.Paint);
                    float spacing = def.Barrels > 1 ? 1.5f * k : 0f;
                    for (int i = 0; i < def.Barrels; i++)
                    {
                        float x = (i - (def.Barrels - 1) * 0.5f) * spacing;
                        bar.Cylinder(new Vector3(x, 0f, 1f * k), new Vector3(x, 0f, L * 0.8f), 0.5f * k, 0.44f * k, 12, Slot.Dark);
                        for (int f = 0; f < 3; f++)
                        {
                            float z = 2f * k + f * L * 0.18f;
                            bar.Box(new Vector3(x, 0f, z), new Vector3(1.5f * k, 0.12f * k, 0.8f * k), Slot.Dark);
                        }
                        bar.Cylinder(new Vector3(x, 0f, L * 0.8f), new Vector3(x, 0f, L * 0.95f), 0.62f * k, 0.62f * k, 12, Slot.Trim);
                        bar.Cylinder(new Vector3(x, 0f, L * 0.95f), new Vector3(x, 0f, L), 0.4f * k, 0.3f * k, 12, Slot.Heat);
                        muzzles.Add(new Vector3(x, 0f, L));
                    }
                    if (def.Size == MountSize.Large) house.Box(new Vector3(0f, p * 1.75f + 0.3f * k, -1.5f * k), new Vector3(2f * k, 0.8f * k, 2.5f * k), Slot.Dark);
                    recoil = 0.35f * k;
                    break;
                }
                case WeaponFamily.Pulse:
                {
                    // A small dome with four emitters in a square.
                    Dome(house, 3f * k, p * 1.6f, Slot.Paint);
                    bar.ChamferBox(new Vector3(0f, 0f, 0.8f * k), new Vector3(2.6f * k, 2.6f * k, 1.6f * k), 0.4f * k, Slot.Dark);
                    for (int i = 0; i < 4; i++)
                    {
                        var o = new Vector3((i % 2 - 0.5f) * 1.2f * k, (i / 2 - 0.5f) * 1.2f * k, 0f);
                        bar.Cylinder(o + new Vector3(0f, 0f, 1.6f * k), o + new Vector3(0f, 0f, L - 0.5f * k), 0.28f * k, 0.25f * k, 10, Slot.Dark);
                        bar.Cylinder(o + new Vector3(0f, 0f, L - 0.5f * k), o + new Vector3(0f, 0f, L), 0.3f * k, 0.2f * k, 10, Slot.Heat);
                        muzzles.Add(o + new Vector3(0f, 0f, L));
                    }
                    recoil = 0.15f * k;
                    break;
                }
                case WeaponFamily.Lance:
                {
                    // A long armoured housing; one square emitter with a glowing slit, a prism at the tip.
                    Gunhouse(house, 5.5f * k, -6f * k, 3f * k, p * 1.8f, angular: true, taper: 0.85f);
                    bar.ChamferBox(new Vector3(0f, 0f, L * 0.5f), new Vector3(1.8f * k, 1.8f * k, L), 0.35f * k, Slot.Paint);
                    bar.Box(new Vector3(0f, 0.92f * k, L * 0.5f), new Vector3(0.35f * k, 0.1f * k, L * 0.9f), Slot.Heat);
                    bar.ChamferBox(new Vector3(0f, 0f, L + 0.6f * k), new Vector3(1.2f * k, 1.2f * k, 1.2f * k), 0.5f * k, Slot.Heat);
                    muzzles.Add(new Vector3(0f, 0f, L + 1.2f * k));
                    recoil = 0.3f * k;
                    break;
                }
                case WeaponFamily.ShieldBurner:
                {
                    Dome(house, 3.4f * k, p * 1.7f, Slot.Paint);
                    for (int i = 0; i < def.Barrels; i++)
                    {
                        float x = (i - (def.Barrels - 1) * 0.5f) * 1.6f * k;
                        bar.Cylinder(new Vector3(x, 0f, 1f * k), new Vector3(x, 0f, L), 0.4f * k, 0.55f * k, 12, Slot.Dark);
                        for (int r = 0; r < 3; r++)
                        {
                            float z = L * (0.45f + r * 0.18f);
                            bar.Cylinder(new Vector3(x, 0f, z), new Vector3(x, 0f, z + 0.4f * k), 0.75f * k, 0.75f * k, 12, Slot.Engine);
                        }
                        muzzles.Add(new Vector3(x, 0f, L));
                    }
                    recoil = 0.2f * k;
                    break;
                }
                case WeaponFamily.Arc:
                {
                    // A drum housing and a pronged fork with a glowing coil between the prongs.
                    house.Cylinder(new Vector3(0f, 0f, 0f), new Vector3(0f, p * 1.6f, 0f), 3.2f * k, 2.8f * k, 20, Slot.Paint);
                    house.Cylinder(new Vector3(0f, p * 1.6f, 0f), new Vector3(0f, p * 1.6f + 0.3f * k, 0f), 2.8f * k, 2.2f * k, 20, Slot.Dark);
                    for (int i = 0; i < def.Barrels; i++)
                    {
                        float x = (i - (def.Barrels - 1) * 0.5f) * 2.2f * k;
                        bar.ChamferBox(new Vector3(x, 0f, 1.2f * k), new Vector3(1.8f * k, 1.8f * k, 2.4f * k), 0.3f * k, Slot.Dark);
                        bar.Box(new Vector3(x - 0.6f * k, 0f, L * 0.55f), new Vector3(0.3f * k, 0.9f * k, L * 0.8f), Slot.Paint);
                        bar.Box(new Vector3(x + 0.6f * k, 0f, L * 0.55f), new Vector3(0.3f * k, 0.9f * k, L * 0.8f), Slot.Paint);
                        bar.Cylinder(new Vector3(x, 0f, 2.4f * k), new Vector3(x, 0f, L * 0.85f), 0.35f * k, 0.35f * k, 12, Slot.Engine);
                        bar.Cylinder(new Vector3(x, 0f, L * 0.85f), new Vector3(x, 0f, L), 0.5f * k, 0.15f * k, 12, Slot.Engine);
                        muzzles.Add(new Vector3(x, 0f, L));
                    }
                    recoil = 0.2f * k;
                    break;
                }
                case WeaponFamily.Flak:
                {
                    // A boxy mount behind a shield plate; four stubby barrels in two pairs.
                    Gunhouse(house, 4f * k, -2.6f * k, 1.6f * k, p * 1.5f, angular: true);
                    bar.Box(new Vector3(0f, 0f, 0.6f * k), new Vector3(3.6f * k, 2.8f * k, 0.4f * k), Slot.Paint);
                    for (int i = 0; i < 4; i++)
                    {
                        var o = new Vector3((i % 2 - 0.5f) * 1.3f * k, (i / 2 - 0.5f) * 0.9f * k, 0f);
                        bar.Cylinder(o + new Vector3(0f, 0f, 0.8f * k), o + new Vector3(0f, 0f, L), 0.22f * k, 0.22f * k, 8, Slot.Dark);
                        bar.Cylinder(o + new Vector3(0f, 0f, L - 0.6f * k), o + new Vector3(0f, 0f, L), 0.3f * k, 0.3f * k, 8, Slot.Trim);
                        muzzles.Add(o + new Vector3(0f, 0f, L));
                    }
                    recoil = 0.3f * k;
                    break;
                }
                default:
                {
                    // Point defence: a ball mount with twin needles.
                    house.Cylinder(Vector3.zero, new Vector3(0f, p * 1.3f, 0f), 1.3f, 1.1f, 12, Slot.Paint);
                    bar.Cylinder(new Vector3(0f, 0f, -0.6f), new Vector3(0f, 0f, 0.8f), 0.8f, 0.8f, 12, Slot.Dark);
                    Barrels(bar, muzzles, 2, 0.5f, 0.1f, 0.8f, L, brake: false);
                    recoil = 0.15f;
                    break;
                }
            }

            return new ShipModels.TurretModel
            {
                Gunhouse = house.ToMesh(def.Id + "_house"),
                Barrels = bar.ToMesh(def.Id + "_barrels"),
                Muzzles = muzzles.ToArray(),
                Recoil = recoil,
            };
        }

        // An armoured box gunhouse, sloped front, on a dark ring.
        static void Gunhouse(MeshBuilder g, float width, float back, float front, float height, bool angular, float taper = 0.9f)
        {
            float hw = width * 0.5f;
            float c = Mathf.Min(width, height) * (angular ? 0.14f : 0.3f);
            Vector2[] Ring(float top, float halfW) => new[]
            {
                new Vector2(halfW - c, top), new Vector2(halfW, top - c), new Vector2(halfW * 1.04f, 0.3f), new Vector2(-halfW * 1.04f, 0.3f),
                new Vector2(-halfW, top - c), new Vector2(-halfW + c, top),
            };
            g.Loft(new[] { Ring(height, hw * 0.96f), Ring(height, hw), Ring(height * 0.62f, hw * taper) },
                new[] { back, back + (front - back) * 0.55f, front }, Slot.Paint);
            g.Cylinder(new Vector3(0f, -0.4f, 0f), new Vector3(0f, 0.6f, 0f), hw * 1.02f, hw, 20, Slot.Dark);
        }

        // A rounded dome gunhouse: stacked rings narrowing to the crown.
        static void Dome(MeshBuilder g, float radius, float height, int sub)
        {
            g.Cylinder(new Vector3(0f, -0.4f, 0f), new Vector3(0f, 0.4f, 0f), radius * 1.05f, radius * 1.05f, 24, Slot.Dark);
            float prevY = 0.4f, prevR = radius;
            for (int i = 1; i <= 4; i++)
            {
                float t = i / 4f;
                float y = 0.4f + height * Mathf.Sin(t * Mathf.PI * 0.5f);
                float r = radius * Mathf.Cos(t * Mathf.PI * 0.42f);
                g.Cylinder(new Vector3(0f, prevY, 0f), new Vector3(0f, y, 0f), prevR, r, 24, sub, caps: i == 4);
                prevY = y;
                prevR = r;
            }
        }

        static void Barrels(MeshBuilder b, List<Vector3> muzzles, int n, float spacing, float radius, float front, float L, bool brake)
        {
            n = Mathf.Max(1, n);
            b.ChamferBox(new Vector3(0f, 0f, front - 0.6f * radius / 0.42f), new Vector3(spacing * n + radius * 3f, radius * 4.2f, 1.8f * radius / 0.42f), radius, Slot.Dark);
            for (int i = 0; i < n; i++)
            {
                float x = (i - (n - 1) * 0.5f) * spacing;
                b.Cylinder(new Vector3(x, 0f, front), new Vector3(x, 0f, front + L * 0.25f), radius * 1.7f, radius * 1.45f, 12, Slot.Dark);
                b.Cylinder(new Vector3(x, 0f, front), new Vector3(x, 0f, L - radius * 1.6f), radius * 1.15f, radius, 12, Slot.Dark);
                if (brake) b.Cylinder(new Vector3(x, 0f, L - radius * 1.6f), new Vector3(x, 0f, L), radius * 1.5f, radius * 1.45f, 12, Slot.Dark);
                muzzles.Add(new Vector3(x, 0f, L));
            }
        }
    }

    // How each family's rounds and flashes look in flight.
    public struct WeaponLook
    {
        public float Length, Width;      // tracer streak, metres
        public Color Color;
        public float Intensity;
        public bool SideColoured;        // energy bolts take the firing side's colour
        public float Muzzle;             // muzzle flash scale

        public static WeaponLook Of(WeaponFamily f, MountSize s)
        {
            float k = WeaponModels.Scale(s);
            switch (f)
            {
                case WeaponFamily.MassDriver: return new WeaponLook { Length = 40f + 20f * k, Width = 1.2f + 0.7f * k, Color = new Color(1f, 0.6f, 0.28f), Intensity = 14f, Muzzle = 2.2f * k };
                case WeaponFamily.Autocannon: return new WeaponLook { Length = 24f, Width = 0.8f, Color = new Color(1f, 0.7f, 0.35f), Intensity = 12f, Muzzle = 0.8f };
                case WeaponFamily.Rotary: return new WeaponLook { Length = 16f, Width = 0.55f * (0.6f + k * 0.4f), Color = new Color(1f, 0.8f, 0.42f), Intensity = 11f, Muzzle = 0.5f * k };
                case WeaponFamily.Railgun: return new WeaponLook { Length = 150f * Mathf.Sqrt(k), Width = 0.8f * Mathf.Sqrt(k), Color = new Color(0.78f, 0.9f, 1f), Intensity = 20f, Muzzle = 1.2f * k };
                case WeaponFamily.Accelerator: return new WeaponLook { Length = 320f, Width = 4f, Color = new Color(0.85f, 0.72f, 1f), Intensity = 26f, Muzzle = 6f };
                case WeaponFamily.Plasma: return new WeaponLook { Length = 20f + 26f * k, Width = 0.9f + 1f * k, Intensity = 14f, SideColoured = true, Muzzle = 1.1f * k };
                case WeaponFamily.Pulse: return new WeaponLook { Length = 11f, Width = 0.9f, Intensity = 13f, SideColoured = true, Muzzle = 0.5f };
                case WeaponFamily.Lance: return new WeaponLook { Length = 150f, Width = 3.6f, Intensity = 22f, SideColoured = true, Muzzle = 5f };
                case WeaponFamily.ShieldBurner: return new WeaponLook { Length = 48f, Width = 2.2f, Color = new Color(1f, 0.38f, 0.95f), Intensity = 16f, Muzzle = 1.6f };
                case WeaponFamily.Arc: return new WeaponLook { Length = 30f + 12f * k, Width = 2f + 1f * k, Color = new Color(0.45f, 0.62f, 1f), Intensity = 16f, Muzzle = 1.4f * k };
                case WeaponFamily.Flak: return new WeaponLook { Length = 8f + 3f * k, Width = 0.8f + 0.3f * k, Color = new Color(1f, 0.86f, 0.4f), Intensity = 12f, Muzzle = 0.7f * k };
                default: return new WeaponLook { Length = 16f, Width = 0.5f, Color = new Color(1f, 0.88f, 0.6f), Intensity = 9f, Muzzle = 0.6f };
            }
        }

        public static readonly Color PlayerBolt = new Color(0.35f, 0.82f, 1f);
        public static readonly Color EnemyBolt = new Color(1f, 0.24f, 0.34f);
    }
}
