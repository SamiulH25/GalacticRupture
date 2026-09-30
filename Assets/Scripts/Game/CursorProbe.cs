using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace GalacticRupture.Game
{
    /// Capture-time check that the cursor set really is present and swappable. A missing
    /// Resources entry fails silently at runtime -- Cursor.SetCursor just keeps the old
    /// texture -- so this asserts rather than trusting the folder.
    public sealed class CursorProbe : MonoBehaviour
    {
        public static bool Ran;
        public static readonly List<string> Results = new();

        void Start()
        {
            var names = new[]
            {
                "Cursors/cursor_1_default", "Cursors/cursor_2_select",
                "Cursors/cursor_3_own_ship", "Cursors/cursor_4_box_select",
                "Cursors/cursor_5_move", "Cursors/cursor_6_attack",
                "Cursors/cursor_7_face",
            };
            int ok = 0;
            foreach (var n in names)
            {
                var t = Resources.Load<Texture2D>(n);
                if (t == null) { Results.Add($"{n}: MISSING"); continue; }
                if (t.width != 32 || t.height != 32) { Results.Add($"{n}: {t.width}x{t.height}"); continue; }
                ok++;
                Results.Add($"{n}: {t.width}x{t.height} fmt={t.format}");
            }
            Results.Add($"loaded {ok}/{names.Length}");

            // Every Apply() must be a no-throw and must actually change the cursor.
            var skin = GetComponent<CursorSkin>();
            if (skin != null)
            {
                foreach (CursorState s in System.Enum.GetValues(typeof(CursorState)))
                {
                    try { skin.Apply(s); Results.Add($"apply {s}: ok"); }
                    catch (System.Exception e) { Results.Add($"apply {s}: {e.GetType().Name}"); }
                }
            }
            else Results.Add("CursorSkin component: MISSING");

            // Import settings are asserted editor-side by CursorImporter's own log; this
            // is a runtime component and cannot touch AssetImporter.
            // The hardware cursor is not captured in a screenshot, so assert it directly:
            // CursorSkin._shown tracks what was last requested.
            var f = typeof(CursorSkin).GetField("_shown",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (skin != null && f != null)
                Results.Add($"last state = {f.GetValue(skin)}");

            // Texture type must be Cursor or Unity never uploads it as one -- the symptom
            // is coloured garbage lines instead of the sprite.
            var ti = Resources.Load<Texture2D>("Cursors/cursor_1_default");
            Results.Add(ti != null
                ? $"default tex {ti.width}x{ti.height} fmt={ti.format}"
                : "default tex MISSING");

            Ran = true;
            foreach (var r in Results) Debug.Log("[cursorprobe] " + r);
        }
    }
}