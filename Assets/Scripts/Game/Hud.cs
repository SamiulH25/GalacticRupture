using GalacticRupture.Sim;
using UnityEngine;

namespace GalacticRupture.Game
{
    // Minimal overlay until the UI pass: state line and controls.
    public sealed class Hud : MonoBehaviour
    {
        public SimRunner Runner;
        public CameraRig Rig;
        GUIStyle _style, _hint;

        void OnGUI()
        {
            if (CaptureMode.Active || BenchMode.Active || Runner == null || Runner.World == null) return;
            _style ??= new GUIStyle(GUI.skin.label) { fontSize = 15, richText = true, normal = { textColor = new Color(0.82f, 0.88f, 0.95f, 0.9f) } };
            _hint ??= new GUIStyle(_style) { fontSize = 12, normal = { textColor = new Color(0.6f, 0.68f, 0.78f, 0.7f) } };

            var focus = Runner.World.ShipById(Rig.FocusShip);
            string focusText = focus == null ? "" :
                $"   ·   {focus.Def.Id.ToUpperInvariant()} #{focus.Id}   hull {focus.Hull / focus.Def.HullHp:P0}   shields {focus.Shield / System.MathF.Max(focus.Def.ShieldHp, 1f):P0}";
            string time = Runner.Paused ? "PAUSED" : $"×{Runner.TimeScale:0.##}";
            GUI.Label(new Rect(24, Screen.height - 64, 1400, 24), $"{(Runner.Toy != null ? $"WAVE {Runner.Toy.Wave}" : $"{Runner.World.Ships.Count} SHIPS")}   ·   {time}{focusText}", _style);
            GUI.Label(new Rect(24, Screen.height - 36, 1400, 20),
                "right-drag orbit · wheel zoom · Tab / Shift+Tab ships · F flagship · C cinematic · Space pause · 1–4 speed", _hint);
        }
    }
}
