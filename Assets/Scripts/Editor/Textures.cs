using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GalacticRupture.EditorTools
{
    // Generated textures: hull panelling (albedo + normal) and soft sprites for effects.
    public static class Textures
    {
        public sealed class Set
        {
            public Texture2D Albedo;
            public Texture2D Normal;
        }

        public sealed class Result
        {
            public Set Panels;
            public Texture2D Glow;
            public Texture2D Ring;
        }

        public static Result Build(string dir)
        {
            var r = new Result
            {
                Glow = Save(dir, "fx_glow", Glow(256), false, false),
                Ring = Save(dir, "fx_ring", Ring(256), false, false),
            };
            Panels(1024, 7, out var albedo, out var normal);
            r.Panels = new Set
            {
                Albedo = Save(dir, "panels_albedo", albedo, true, false),
                Normal = Save(dir, "panels_normal", normal, false, true),
            };
            return r;
        }

        static Texture2D Save(string dir, string name, Texture2D tex, bool srgb, bool normal)
        {
            string path = $"{dir}/{name}.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            imp.sRGBTexture = srgb;
            imp.wrapMode = normal || srgb ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            imp.mipmapEnabled = true;
            imp.anisoLevel = 8;
            imp.textureCompression = TextureImporterCompression.CompressedHQ;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Texture2D Glow(int size)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (x + 0.5f) / size * 2f - 1f, v = (y + 0.5f) / size * 2f - 1f;
                float r2 = u * u + v * v;
                float g = Mathf.Exp(-r2 * 7f) * 0.85f + Mathf.Exp(-r2 * 40f) * 0.15f;
                g *= Mathf.Clamp01((1f - Mathf.Sqrt(r2)) * 4f);
                px[y * size + x] = new Color(g, g, g, g);
            }
            t.SetPixels(px);
            t.Apply();
            return t;
        }

        static Texture2D Ring(int size)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (x + 0.5f) / size * 2f - 1f, v = (y + 0.5f) / size * 2f - 1f;
                float r = Mathf.Sqrt(u * u + v * v);
                float ring = Mathf.Exp(-Mathf.Pow((r - 0.72f) / 0.1f, 2f));
                float core = Mathf.Exp(-r * r * 9f) * 0.35f;
                // Faint hex cells inside the ring: shields read as a field, not a bubble of light.
                float hex = Mathf.Abs(Mathf.Sin(u * 22f) * Mathf.Sin((u * 0.5f + v * 0.866f) * 22f) * Mathf.Sin((u * 0.5f - v * 0.866f) * 22f));
                float cells = (1f - Mathf.SmoothStep(0.0f, 0.12f, hex)) * 0.35f * Mathf.Clamp01(1f - r);
                float g = Mathf.Clamp01(ring + core + cells) * Mathf.Clamp01((1f - r) * 5f);
                px[y * size + x] = new Color(g, g, g, g);
            }
            t.SetPixels(px);
            t.Apply();
            return t;
        }

        struct Rect
        {
            public int X, Y, W, H;
        }

        // Recursive panel split. Seams are recessed; panels vary slightly in tone and height,
        // some carry rivet lines, and grime collects along the seams.
        static void Panels(int size, int seed, out Texture2D albedo, out Texture2D normal)
        {
            var rng = new System.Random(seed);
            var height = new float[size * size];
            var tone = new float[size * size];
            var rects = new List<Rect>();
            Split(new Rect { X = 0, Y = 0, W = size, H = size }, rng, rects, 0);

            const int seam = 3;
            foreach (var r in rects)
            {
                float h = 1f - (float)rng.NextDouble() * 0.06f;
                float tn = 0.84f + (float)rng.NextDouble() * 0.16f;
                bool rivets = rng.NextDouble() < 0.35;
                bool hatch = rng.NextDouble() < 0.12;
                for (int y = r.Y; y < r.Y + r.H; y++)
                for (int x = r.X; x < r.X + r.W; x++)
                {
                    int i = y * size + x;
                    int dx = Mathf.Min(x - r.X, r.X + r.W - 1 - x);
                    int dy = Mathf.Min(y - r.Y, r.Y + r.H - 1 - y);
                    int d = Mathf.Min(dx, dy);
                    float hh = d < seam ? 0.55f + 0.1f * d : h;
                    float t = tn * (d < seam ? 0.55f : 1f) * Mathf.Lerp(0.88f, 1f, Mathf.Clamp01(d / 14f));
                    if (rivets && d >= 6 && d <= 8 && ((x + y) % 14 < 3) && (dx == 7 || dy == 7))
                        hh += 0.05f;
                    if (hatch && d > 10 && ((x - y) & 15) < 2)
                    {
                        hh -= 0.03f;
                        t *= 0.93f;
                    }
                    height[i] = hh;
                    tone[i] = t;
                }
            }

            // Fine surface noise so large panels don't read as flat plastic.
            for (int i = 0; i < height.Length; i++)
            {
                int x = i % size, y = i / size;
                float n = Mathf.PerlinNoise(x * 0.02f + seed, y * 0.02f) * 0.6f + Mathf.PerlinNoise(x * 0.11f, y * 0.11f + seed) * 0.4f;
                tone[i] *= 0.9f + n * 0.14f;
                height[i] += (n - 0.5f) * 0.012f;
            }

            albedo = new Texture2D(size, size, TextureFormat.RGBA32, true, false);
            normal = new Texture2D(size, size, TextureFormat.RGBA32, true, true);
            var a = new Color[size * size];
            var nrm = new Color[size * size];
            const float strength = 6f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int i = y * size + x;
                float t = Mathf.Clamp01(tone[i]);
                a[i] = new Color(t, t, t, 1f);
                float hl = height[y * size + (x + size - 1) % size], hr = height[y * size + (x + 1) % size];
                float hd = height[((y + size - 1) % size) * size + x], hu = height[((y + 1) % size) * size + x];
                var n = new Vector3((hl - hr) * strength, (hd - hu) * strength, 1f).normalized;
                nrm[i] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
            }
            albedo.SetPixels(a);
            albedo.Apply();
            normal.SetPixels(nrm);
            normal.Apply();
        }

        static void Split(Rect r, System.Random rng, List<Rect> output, int depth)
        {
            const int min = 56;
            bool canV = r.W >= min * 2, canH = r.H >= min * 2;
            bool stop = depth > 2 && rng.NextDouble() < 0.18 + depth * 0.06;
            if ((!canV && !canH) || stop)
            {
                output.Add(r);
                return;
            }
            bool vertical = canV && (!canH || (r.W > r.H ? rng.NextDouble() < 0.75 : rng.NextDouble() < 0.25));
            if (vertical)
            {
                int cut = min + rng.Next(r.W - min * 2 + 1);
                cut = cut / 8 * 8;
                Split(new Rect { X = r.X, Y = r.Y, W = cut, H = r.H }, rng, output, depth + 1);
                Split(new Rect { X = r.X + cut, Y = r.Y, W = r.W - cut, H = r.H }, rng, output, depth + 1);
            }
            else
            {
                int cut = min + rng.Next(r.H - min * 2 + 1);
                cut = cut / 8 * 8;
                Split(new Rect { X = r.X, Y = r.Y, W = r.W, H = cut }, rng, output, depth + 1);
                Split(new Rect { X = r.X, Y = r.Y + cut, W = r.W, H = r.H - cut }, rng, output, depth + 1);
            }
        }
    }
}
