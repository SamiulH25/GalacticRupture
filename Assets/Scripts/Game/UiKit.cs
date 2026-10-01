using System.Collections.Generic;
using UnityEngine;

namespace GalacticRupture.Game
{
    // The look of every screen: old-empire terminals seized and repurposed (IDEAS.md §7). Near-black
    // plates with screw heads, amber caps in a mono face, stamps in a stencil face, and the paper
    // of the Secretariat. Panels, stamps and fonts live here so the screens share one language.
    public static class UiKit
    {
        public static readonly Color Bg = new Color(0.031f, 0.035f, 0.043f);
        public static readonly Color Panel = new Color(0.063f, 0.071f, 0.086f, 0.94f);
        public static readonly Color Panel2 = new Color(0.082f, 0.094f, 0.114f, 0.96f);
        public static readonly Color Line = new Color(0.165f, 0.184f, 0.216f);
        public static readonly Color Line2 = new Color(0.231f, 0.259f, 0.302f);
        public static readonly Color Amber = new Color(0.906f, 0.667f, 0.271f);
        public static readonly Color AmberDim = new Color(0.541f, 0.416f, 0.18f);
        public static readonly Color Phos = new Color(0.561f, 0.827f, 0.659f);
        public static readonly Color Ink = new Color(0.851f, 0.831f, 0.78f);
        public static readonly Color Mute = new Color(0.49f, 0.502f, 0.537f);
        public static readonly Color Red = new Color(0.851f, 0.325f, 0.239f);
        public static readonly Color Warn = new Color(0.941f, 0.753f, 0.29f);
        public static readonly Color Ok = new Color(0.435f, 0.725f, 0.541f);
        public static readonly Color Shield = new Color(0.345f, 0.651f, 1f);
        public static readonly Color PaperTone = new Color(0.847f, 0.812f, 0.718f);
        public static readonly Color PaperInk = new Color(0.106f, 0.09f, 0.063f);
        public static readonly Color PaperRed = new Color(0.643f, 0.184f, 0.118f);

        public static Font Body, BodyBold, Stencil, Mono;
        public static Texture2D White { get; private set; }

        static bool _ready;
        static readonly Dictionary<long, GUIStyle> _styles = new Dictionary<long, GUIStyle>();

        public static void Init(GameArt art)
        {
            if (_ready) return;
            _ready = true;
            White = Texture2D.whiteTexture;
            Body = art != null && art.UiFont != null ? art.UiFont : GUI.skin.font;
            BodyBold = art != null && art.UiFontBold != null ? art.UiFontBold : Body;
            Stencil = Resources.Load<Font>("Fonts/StardosStencil-Bold") ?? BodyBold;
            Mono = Resources.Load<Font>("Fonts/ShareTechMono-Regular") ?? Body;
        }

        public enum Face { Body, Bold, Stencil, Mono }

        public static GUIStyle Style(Face face, int size, Color color, TextAnchor anchor = TextAnchor.UpperLeft, bool wrap = false)
        {
            long key = ((long)face << 40) ^ ((long)size << 32) ^ ((long)anchor << 24) ^ (wrap ? 1L << 23 : 0)
                       ^ ((long)(color.r * 255) << 16) ^ ((long)(color.g * 255) << 8) ^ (long)(color.b * 255);
            if (_styles.TryGetValue(key, out var s)) return s;
            var font = face switch { Face.Bold => BodyBold, Face.Stencil => Stencil, Face.Mono => Mono, _ => Body };
            s = new GUIStyle(GUI.skin.label)
            {
                font = font, fontSize = size, alignment = anchor, wordWrap = wrap, richText = true, clipping = TextClipping.Overflow,
                normal = { textColor = color },
            };
            _styles[key] = s;
            return s;
        }

        public static void Text(Rect r, string text, Face face, int size, Color color, TextAnchor anchor = TextAnchor.UpperLeft, bool wrap = false)
            => GUI.Label(r, text, Style(face, size, color, anchor, wrap));

        // Spaced capitals, the plate-heading look.
        public static string Spaced(string s) => string.Join(" ", s.ToUpperInvariant().ToCharArray());

        public static void Fill(Rect r, Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, White);
            GUI.color = old;
        }

        public static void Outline(Rect r, Color c, float w = 1f)
        {
            Fill(new Rect(r.x, r.y, r.width, w), c);
            Fill(new Rect(r.x, r.yMax - w, r.width, w), c);
            Fill(new Rect(r.x, r.y, w, r.height), c);
            Fill(new Rect(r.xMax - w, r.y, w, r.height), c);
        }

        public static void Tex(Rect r, Texture t, Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, t);
            GUI.color = old;
        }

        // A riveted plate with an optional amber caption bar.
        public static void Plate(Rect r, string title = null, Color? tint = null)
        {
            Fill(r, tint ?? Panel);
            Outline(r, Line);
            Screw(new Vector2(r.x + 7, r.y + 7));
            Screw(new Vector2(r.xMax - 7, r.y + 7));
            if (title == null) return;
            Fill(new Rect(r.x, r.y + 26, r.width, 1), Line);
            Text(new Rect(r.x + 22, r.y + 5, r.width - 44, 18), Spaced(title), Face.Mono, 12, Amber, TextAnchor.MiddleLeft);
        }

        static void Screw(Vector2 c)
        {
            Fill(new Rect(c.x - 3, c.y - 3, 6, 6), new Color(0.13f, 0.15f, 0.17f));
            Outline(new Rect(c.x - 3, c.y - 3, 6, 6), Line2);
        }

        // A stamped label, a little off true.
        public static void Stamp(Rect r, string text, Color color, float angle = -2f, int size = 15)
        {
            var m = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, r.center);
            var c = new Color(color.r, color.g, color.b, 0.92f);
            Outline(r, c, 2f);
            Text(r, text.ToUpperInvariant(), Face.Stencil, size, c, TextAnchor.MiddleCenter);
            GUI.matrix = m;
        }

        // The Secretariat's paper: warm, ruled lightly, with a shadow.
        public static void Paper(Rect r, float angle = 0.3f)
        {
            var m = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, r.center);
            Fill(new Rect(r.x + 3, r.y + 5, r.width, r.height), new Color(0f, 0f, 0f, 0.45f));
            Fill(r, PaperTone);
            for (float y = r.y + 4; y < r.yMax; y += 4) Fill(new Rect(r.x, y, r.width, 1), new Color(0f, 0f, 0f, 0.035f));
            GUI.matrix = m;
        }

        // A struck-through run of text, drawn over the paper.
        public static void Strike(Rect r, Color c) => Fill(new Rect(r.x, r.center.y + 1, r.width, 2), c);
    }
}
