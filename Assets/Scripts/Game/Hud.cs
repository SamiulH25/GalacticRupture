using GalacticRupture.Sim;
using UnityEngine;

namespace GalacticRupture.Game
{
    // Overlay until the UI Toolkit pass: selection cards with hull and shield bars, battle state,
    // time control, the drag box, and the controls line.
    public sealed class Hud : MonoBehaviour
    {
        public SimRunner Runner;
        public CameraRig Rig;
        public CommandInput Input;

        GUIStyle _label, _small, _banner;
        Texture2D _white;

        static readonly Color Panel = new Color(0.03f, 0.05f, 0.08f, 0.72f);
        static readonly Color Edge = new Color(0.45f, 0.85f, 1f, 0.5f);
        static readonly Color HullColor = new Color(0.85f, 0.72f, 0.45f);
        static readonly Color ShieldColor = new Color(0.4f, 0.8f, 1f);

        void Styles()
        {
            if (_white != null) return;
            _white = Texture2D.whiteTexture;
            _label = new GUIStyle(GUI.skin.label) { fontSize = 14, normal = { textColor = new Color(0.85f, 0.9f, 0.96f) } };
            _small = new GUIStyle(_label) { fontSize = 11, normal = { textColor = new Color(0.6f, 0.68f, 0.78f) } };
            _banner = new GUIStyle(_label) { fontSize = 38, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        }

        void Fill(Rect r, Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, _white);
            GUI.color = old;
        }

        void Bar(Rect r, float v, Color c)
        {
            Fill(r, new Color(0f, 0f, 0f, 0.5f));
            Fill(new Rect(r.x, r.y, r.width * Mathf.Clamp01(v), r.height), c);
        }

        void OnGUI()
        {
            if (CaptureMode.Active || BenchMode.Active || Runner == null || Runner.World == null) return;
            Styles();
            var world = Runner.World;

            // Drag box (screen y is flipped relative to GUI).
            if (Input != null && Input.Dragging)
            {
                var r = Input.DragRect;
                var g = new Rect(r.x, Screen.height - r.yMax, r.width, r.height);
                Fill(g, new Color(0.45f, 0.85f, 1f, 0.08f));
                Fill(new Rect(g.x, g.y, g.width, 1), Edge);
                Fill(new Rect(g.x, g.yMax - 1, g.width, 1), Edge);
                Fill(new Rect(g.x, g.y, 1, g.height), Edge);
                Fill(new Rect(g.xMax - 1, g.y, 1, g.height), Edge);
            }

            // Top: time and battle state.
            string time = Runner.Paused ? "PAUSED" : $"×{Runner.TimeScale:0.##}";
            int own = 0, enemy = 0;
            foreach (var s in world.Ships)
                if (s.Alive) { if (s.Side == 0) own++; else enemy++; }
            GUI.Label(new Rect(24, 16, 600, 24), $"{time}     FLEET {own}     CONTACTS {enemy}", _label);
            if (Runner.Battle != null && Runner.Battle.State != BattleState.Fighting)
            {
                bool won = Runner.Battle.State == BattleState.Victory;
                _banner.normal.textColor = won ? new Color(0.6f, 0.95f, 1f) : new Color(1f, 0.45f, 0.4f);
                GUI.Label(new Rect(0, Screen.height * 0.3f, Screen.width, 60), won ? "VICTORY" : "FLEET LOST", _banner);
                GUI.Label(new Rect(0, Screen.height * 0.3f + 56, Screen.width, 24), "Ctrl+R for a new engagement",
                    new GUIStyle(_small) { alignment = TextAnchor.MiddleCenter });
            }

            // Bottom left: selection cards.
            if (Input != null && Input.Selected.Count > 0)
            {
                const float w = 210f, h = 54f;
                int cols = Mathf.Max(1, Mathf.Min(Input.Selected.Count, (int)((Screen.width * 0.6f) / (w + 6))));
                for (int i = 0; i < Input.Selected.Count; i++)
                {
                    var s = world.ShipById(Input.Selected[i]);
                    if (s == null) continue;
                    int row = i / cols, col = i % cols;
                    var r = new Rect(24 + col * (w + 6), Screen.height - 104 - row * (h + 6), w, h);
                    Fill(r, Panel);
                    Fill(new Rect(r.x, r.y, 2, r.height), Edge);
                    string order = s.Order.Kind switch
                    {
                        OrderKind.MoveTo => "moving",
                        OrderKind.Attack => "attacking",
                        OrderKind.Face => "turning",
                        _ => "holding",
                    };
                    int guns = 0;
                    foreach (var m in s.Mounts) if (m.Alive) guns++;
                    GUI.Label(new Rect(r.x + 10, r.y + 3, w - 20, 18), $"{s.Def.Id.ToUpperInvariant()} {s.Id}", _label);
                    GUI.Label(new Rect(r.x + 10, r.y + 20, w - 20, 16), $"{order} · {guns}/{s.Mounts.Count} guns", _small);
                    Bar(new Rect(r.x + 10, r.y + 38, w - 20, 4), s.Shield / Mathf.Max(1f, s.Def.ShieldHp), ShieldColor);
                    Bar(new Rect(r.x + 10, r.y + 45, w - 20, 4), s.Hull / s.Def.HullHp, HullColor);
                }
            }

            GUI.Label(new Rect(24, Screen.height - 32, Screen.width - 48, 20),
                "left-click/drag select · right-click move · right-click enemy attack · Ctrl+right-click face · H hold · Ctrl+1–9 groups · " +
                "WASD pan · Q/E turn · middle-drag orbit · wheel zoom · F follow · Space pause · +/- speed",
                _small);
        }
    }
}
