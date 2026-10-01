using System.Collections.Generic;
using GalacticRupture.Sim;
using UnityEngine;

namespace GalacticRupture.Game
{
    // The ship is the health bar (IDEAS.md §2.2): a plan-view schematic of the hull, drawn from its
    // real hardpoint layout and the damage it has taken. Hull sections tint from steel to ember as
    // they are hit and crack open when breached; every gun shows its state; the shield is a ring
    // that drains from the stern. Rebuilt only when something visible changes.
    public sealed class HullSchematic
    {
        sealed class Entry
        {
            public Texture2D Tex;
            public int Signature = int.MinValue;
        }

        public const int W = 400, H = 200;

        readonly Dictionary<int, Entry> _cache = new Dictionary<int, Entry>();
        readonly Color32[] _px = new Color32[W * H];

        // Half-width along the keel, stern (0) to bow (1), in thirteenths of the beam.
        static readonly float[,] Profile =
        {
            { 0f, 5f }, { 0.08f, 9f }, { 0.24f, 12f }, { 0.55f, 13f }, { 0.78f, 11f }, { 0.92f, 7f }, { 1f, 0f },
        };

        static float HalfWidth(float u)
        {
            if (u <= 0f || u >= 1f) return u <= 0f ? Profile[0, 1] : 0f;
            for (int i = 0; i < Profile.GetLength(0) - 1; i++)
                if (u <= Profile[i + 1, 0])
                {
                    float t = (u - Profile[i, 0]) / (Profile[i + 1, 0] - Profile[i, 0]);
                    return Mathf.Lerp(Profile[i, 1], Profile[i + 1, 1], t);
                }
            return 0f;
        }

        static float ClassScale(HullClass c) => c switch
        {
            HullClass.Battleship => 1f,
            HullClass.Cruiser => 0.86f,
            HullClass.Destroyer => 0.7f,
            HullClass.Frigate => 0.56f,
            HullClass.Corvette => 0.42f,
            HullClass.Transport => 0.8f,
            _ => 0.62f,
        };

        public Texture2D Get(Ship ship)
        {
            if (!_cache.TryGetValue(ship.Id, out var e))
            {
                e = new Entry { Tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear } };
                _cache[ship.Id] = e;
            }
            int sig = Signature(ship);
            if (sig != e.Signature)
            {
                e.Signature = sig;
                Paint(ship);
                e.Tex.SetPixels32(_px);
                e.Tex.Apply(false);
            }
            return e.Tex;
        }

        public void Forget(HashSet<int> alive)
        {
            List<int> gone = null;
            foreach (var k in _cache.Keys) if (!alive.Contains(k)) (gone ??= new List<int>()).Add(k);
            if (gone == null) return;
            foreach (int k in gone)
            {
                Object.Destroy(_cache[k].Tex);
                _cache.Remove(k);
            }
        }

        static int Signature(Ship s)
        {
            unchecked
            {
                int h = 17;
                for (int i = 0; i < Ship.Sections; i++) h = h * 31 + (int)(s.SectionHeat(i) * 10f);
                h = h * 31 + (int)(s.Shield / Mathf.Max(1f, s.Def.ShieldHp) * 24f);
                foreach (var m in s.Mounts) h = h * 31 + (m.Alive ? 1 + (int)(Mathf.Clamp01(m.Hp / m.Def.Hp) * 2.99f) : 0);
                return h;
            }
        }

        // --- Painting ----------------------------------------------------------------------------

        static readonly Color32 Clear = new Color32(0, 0, 0, 0);

        static Color HeatColour(float v)
        {
            var steel = new Color(0.30f, 0.34f, 0.39f);
            var scorch = new Color(0.42f, 0.36f, 0.30f);
            var ember = new Color(0.62f, 0.36f, 0.20f);
            var open = new Color(0.46f, 0.15f, 0.10f);
            if (v < 0.25f) return Color.Lerp(steel, scorch, v / 0.25f);
            if (v < 0.6f) return Color.Lerp(scorch, ember, (v - 0.25f) / 0.35f);
            return Color.Lerp(ember, open, Mathf.Clamp01((v - 0.6f) / 0.4f));
        }

        void Set(int x, int y, Color c, float a = 1f)
        {
            if (x < 0 || y < 0 || x >= W || y >= H) return;
            int i = y * W + x;
            var old = _px[i];
            float oa = old.a / 255f;
            float na = a + oa * (1f - a);
            if (na <= 0f) return;
            float r = (c.r * a + old.r / 255f * oa * (1f - a)) / na;
            float g = (c.g * a + old.g / 255f * oa * (1f - a)) / na;
            float b = (c.b * a + old.b / 255f * oa * (1f - a)) / na;
            _px[i] = new Color32((byte)(r * 255), (byte)(g * 255), (byte)(b * 255), (byte)(na * 255));
        }

        void Disc(float cx, float cy, float r, Color c)
        {
            int x0 = Mathf.FloorToInt(cx - r - 1), x1 = Mathf.CeilToInt(cx + r + 1);
            int y0 = Mathf.FloorToInt(cy - r - 1), y1 = Mathf.CeilToInt(cy + r + 1);
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                float a = Mathf.Clamp01(r - d + 0.5f);
                if (a > 0f) Set(x, y, c, a);
            }
        }

        void Ring(float cx, float cy, float r, float thick, Color c)
        {
            int x0 = Mathf.FloorToInt(cx - r - thick - 1), x1 = Mathf.CeilToInt(cx + r + thick + 1);
            int y0 = Mathf.FloorToInt(cy - r - thick - 1), y1 = Mathf.CeilToInt(cy + r + thick + 1);
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                float a = Mathf.Clamp01(thick * 0.5f - Mathf.Abs(d - r) + 0.5f);
                if (a > 0f) Set(x, y, c, a);
            }
        }

        void Line(float x0, float y0, float x1, float y1, float thick, Color c)
        {
            float dx = x1 - x0, dy = y1 - y0;
            int steps = Mathf.CeilToInt(Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) * 1.5f);
            for (int i = 0; i <= steps; i++)
            {
                float t = steps == 0 ? 0f : i / (float)steps;
                Disc(x0 + dx * t, y0 + dy * t, thick * 0.5f, c);
            }
        }

        static float Hash(int x, int y)
        {
            unchecked
            {
                uint h = (uint)(x * 73856093) ^ (uint)(y * 19349663);
                h ^= h >> 13; h *= 0x5bd1e995; h ^= h >> 15;
                return (h & 0xffff) / 65535f;
            }
        }

        void Paint(Ship ship)
        {
            for (int i = 0; i < _px.Length; i++) _px[i] = Clear;
            var def = ship.Def;
            float scale = ClassScale(def.Class);
            float length = W * 0.76f * scale;
            float x0 = (W - length) * 0.5f, cy = H * 0.5f;
            float beam = Mathf.Clamp(length * (def.Radius / Mathf.Max(1f, def.HalfLength)) * 0.62f, H * 0.12f, H * 0.34f);
            float unit = beam / 13f;
            float shieldFrac = def.ShieldHp > 0f ? Mathf.Clamp01(ship.Shield / def.ShieldHp) : 0f;

            // Shield ring around the hull: drains from the stern forward, so what is left is the bow.
            if (def.ShieldHp > 0f)
            {
                float rx = length * 0.5f + 16f, ry = beam + 14f;
                for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float nx = (x - (x0 + length * 0.5f)) / rx, ny = (y - cy) / ry;
                    float d = Mathf.Sqrt(nx * nx + ny * ny);
                    float edge = Mathf.Abs(d - 1f) * Mathf.Min(rx, ry);
                    if (edge > 2.2f) continue;
                    float ang = Mathf.Abs(Mathf.Atan2(ny, nx)) / Mathf.PI;     // 0 at the bow, 1 at the stern
                    bool lit = ang <= shieldFrac;
                    float a = Mathf.Clamp01(1.6f - edge * 0.75f);
                    var col = lit ? UiKit.Shield : new Color(0.17f, 0.19f, 0.22f);
                    Set(x, y, col, lit ? a * 0.95f : a * (((x + y) / 4) % 2 == 0 ? 0.9f : 0f));
                }
            }

            // Hull, six slices of the keel.
            for (int x = Mathf.FloorToInt(x0); x <= Mathf.CeilToInt(x0 + length); x++)
            {
                float u = (x - x0) / length;
                if (u < 0f || u > 1f) continue;
                float hw = HalfWidth(u) * unit;
                int sec = Mathf.Min(Ship.Sections - 1, (int)(u * Ship.Sections));
                float heat = ship.SectionHeat(sec);
                var baseCol = HeatColour(heat);
                float secPos = u * Ship.Sections - sec;
                for (int y = Mathf.FloorToInt(cy - hw - 1); y <= Mathf.CeilToInt(cy + hw + 1); y++)
                {
                    float dy = Mathf.Abs(y - cy);
                    float cover = Mathf.Clamp01(hw - dy + 0.5f);
                    if (cover <= 0f) continue;
                    var c = baseCol * (0.88f + 0.12f * Mathf.Cos(dy / Mathf.Max(1f, hw) * 1.4f));
                    if (secPos < 0.03f || secPos > 0.97f) c *= 0.55f;               // the seam between slices
                    if (hw - dy < 1.6f) c = Color.Lerp(c, new Color(0.68f, 0.72f, 0.78f), 0.55f); // lit edge
                    if (Mathf.Abs(dy) < 0.7f) c *= 0.8f;                              // the spine
                    if (heat > 0.7f)
                    {
                        // Breached: a ragged opening, and embers in it.
                        float n = Hash(x / 2, y / 2);
                        float crack = Mathf.Abs(Mathf.Sin((x * 0.31f) + Mathf.Sin(y * 0.23f) * 2f));
                        if (crack < 0.16f) c = Color.Lerp(c, new Color(0.03f, 0.02f, 0.02f), 0.9f);
                        else if (n > 0.93f) c = Color.Lerp(c, new Color(1f, 0.55f, 0.2f), 0.9f);
                    }
                    c.a = 1f;
                    Set(x, y, c, cover);
                }
            }

            // Engines at the stern; a ship that cannot move has none.
            if (def.MaxSpeed > 0f)
            {
                int engines = def.Class >= HullClass.Cruiser ? 3 : def.Class >= HullClass.Frigate ? 2 : 1;
                for (int i = 0; i < engines; i++)
                {
                    float ey = cy + (i - (engines - 1) * 0.5f) * beam * 0.5f;
                    for (int ex = 0; ex < 7; ex++)
                        for (int yy = -3; yy <= 3; yy++)
                            Set(Mathf.RoundToInt(x0) - 6 + ex, Mathf.RoundToInt(ey) + yy, new Color(1f, 0.6f, 0.25f), 0.9f - ex * 0.1f);
                }
            }

            // Guns, from the hardpoint layout: z forward to the right, starboard down.
            foreach (var m in ship.Mounts)
            {
                var hp = m.Hardpoint;
                float u = hp.LocalPosition.Z / (def.HalfLength * 2f) + 0.5f;
                float v = hp.LocalPosition.X / Mathf.Max(1f, def.Radius);
                float mx = x0 + Mathf.Clamp01(u) * length, my = cy + Mathf.Clamp(v, -1f, 1f) * beam * 0.7f;
                float r = (m.Def.Size == MountSize.Large ? 6.5f : m.Def.Size == MountSize.Medium ? 5f : 3.6f) * (0.7f + 0.3f * scale);
                float hpFrac = Mathf.Clamp01(m.Hp / Mathf.Max(1f, m.Def.Hp));
                var col = !m.Alive ? UiKit.Red : hpFrac < 0.5f ? UiKit.Warn : UiKit.Ok;
                Disc(mx, my, r + 1.5f, new Color(0.03f, 0.04f, 0.05f));
                Ring(mx, my, r, 2.2f, col);
                if (m.Alive)
                {
                    float a = hp.BaseYawDeg * Mathf.Deg2Rad;
                    Line(mx, my, mx + Mathf.Cos(a) * (r + 6f), my + Mathf.Sin(a) * (r + 6f), 2.2f, col);
                }
                else
                {
                    Line(mx - r * 0.7f, my - r * 0.7f, mx + r * 0.7f, my + r * 0.7f, 2f, UiKit.Red);
                    Line(mx + r * 0.7f, my - r * 0.7f, mx - r * 0.7f, my + r * 0.7f, 2f, UiKit.Red);
                }
            }
        }
    }
}
