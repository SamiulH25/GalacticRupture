using System.Collections.Generic;
using UnityEngine;

namespace GalacticRupture.Game
{
    /// The seven fleet cursors, as one texture per state.
    ///
    /// These are deliberately small hand-placed sprites rather than the OS arrow: the
    /// game is played over black space and a bright planet, and a stock cursor is
    /// legible against neither. Every state shares the same arrow body and the same 1px
    /// ink outline so the set reads as one family; only the colour and the small
    /// marker change. Cyan is the "select" family, amber is "your orders", red is
    /// "hostile", matching the HUD.
    public enum CursorState
    {
        Default,
        Select,
        OwnShip,
        BoxSelect,
        Move,
        Attack,
        Face,
    }

    /// Swaps `Cursor.Set` as the player's intent changes. Purely cosmetic: nothing here
    /// feeds back into the sim, so a missing texture degrades to the OS cursor rather
    /// than breaking input.
    public sealed class CursorSkin : MonoBehaviour
    {
        static readonly string[] Files =
        {
            "cursor_1_default",
            "cursor_2_select",
            "cursor_3_own_ship",
            "cursor_4_box_select",
            "cursor_5_move",
            "cursor_6_attack",
            "cursor_7_face",
        };

        readonly Dictionary<CursorState, Texture2D> _tex = new();
        CursorState _shown = (CursorState)(-1);

        void Awake()
        {
            for (var i = 0; i < Files.Length; i++)
            {
                // Loaded through the normal importer, so point filtering is whatever the
                // texture's import settings say. Pixel art must be point-filtered or the
                // cursor goes soft at 1:1 -- see the note in ProjectBuilder.
                var t = Resources.Load<Texture2D>("Cursors/" + Files[i]);
                if (t != null) _tex[(CursorState)i] = t;
            }
            Apply(CursorState.Default);
        }

        // The arrow's tip is at (3, 3) in the 32x32 sprite, not at (0, 0). Without this the
        // cursor draws offset from the actual click point, which is very noticeable on a
        // selection click and reads as the game ignoring your aim.
        static readonly Vector2 Hotspot = new(3f, 3f);

        public void Apply(CursorState state)
        {
            if (state == _shown) return;
            _shown = state;
            // ForceSoftware, not Auto: Auto lets the platform decide whether to honour a custom
            // cursor, and where it does not (some Linux compositors, some remote
            // desktops) the sprite is silently ignored and the player gets a stray
            // hardware cursor instead. Forcing software guarantees the set is shown.
            if (_tex.TryGetValue(state, out var t))
                UnityEngine.Cursor.SetCursor(t, Hotspot, UnityEngine.CursorMode.ForceSoftware);
            else
                UnityEngine.Cursor.SetCursor(null, Vector2.zero, UnityEngine.CursorMode.ForceSoftware);
        }
    }
}