using System.Collections.Generic;
using GalacticRupture.Sim;
using UnityEngine;

namespace GalacticRupture.Game
{
    // The tactical screen, laid out after Empire at War: time, fleet and command points top
    // left; a radar bottom left (click to look, right-click to send the selection); the
    // reinforcement panel on the right (R); the selection as cards along the bottom; the power
    // abilities bottom right (Z engines, X shields, V weapons). Over the field, every ship in
    // view carries a small shield and hull bar, and distant ships a marker in their side's colour.
    public sealed class Hud : MonoBehaviour
    {
        public SimRunner Runner;
        public CameraRig Rig;
        public CommandInput Input;
        public CampaignDirector Director;

        // Screen areas the HUD owns this frame (GUI coordinates): clicks there are not orders.
        static readonly List<Rect> Owned = new List<Rect>();
        static readonly List<Rect> _building = new List<Rect>();

        // Input System position (y up) over a HUD panel?
        public static bool Over(Vector2 screen)
        {
            var p = new Vector2(screen.x, Screen.height - screen.y);
            foreach (var r in Owned) if (r.Contains(p)) return true;
            return false;
        }

        public bool ShowReserves = true;

        GUIStyle _text, _small, _tiny, _caps, _banner, _key, _center;
        Texture2D _white, _disc, _ring, _tri;
        float _radarRange = 7000f;

        static readonly Color Panel = new Color(0.02f, 0.035f, 0.06f, 0.82f);
        static readonly Color Rule = new Color(0.45f, 0.62f, 0.8f, 0.22f);
        static readonly Color Edge = new Color(0.45f, 0.85f, 1f, 0.55f);
        static readonly Color Ink = new Color(0.86f, 0.91f, 0.97f);
        static readonly Color Muted = new Color(0.56f, 0.64f, 0.74f);
        static readonly Color HullColor = new Color(0.9f, 0.76f, 0.46f);
        static readonly Color ShieldColor = new Color(0.4f, 0.8f, 1f);
        static readonly Color Friend = new Color(0.4f, 0.78f, 1f);
        static readonly Color Foe = new Color(1f, 0.32f, 0.28f);

        World W => Runner.World;

        void Styles()
        {
            if (_white != null) return;
            _white = Texture2D.whiteTexture;
            _disc = Disc(64, false);
            _ring = Disc(256, true);
            _tri = Triangle(32);
            var art = Runner.Art;
            var font = art != null && art.UiFont != null ? art.UiFont : GUI.skin.font;
            var bold = art != null && art.UiFontBold != null ? art.UiFontBold : font;
            _text = new GUIStyle(GUI.skin.label) { font = bold, fontSize = 15, richText = true, normal = { textColor = Ink } };
            _small = new GUIStyle(_text) { font = font, fontSize = 13, normal = { textColor = Muted } };
            _tiny = new GUIStyle(_small) { fontSize = 11 };
            _caps = new GUIStyle(_text) { fontSize = 13, normal = { textColor = new Color(0.62f, 0.74f, 0.88f) } };
            _banner = new GUIStyle(_text) { fontSize = 52, alignment = TextAnchor.MiddleCenter };
            _key = new GUIStyle(_text) { fontSize = 12, alignment = TextAnchor.MiddleCenter };
            _center = new GUIStyle(_text) { fontSize = 13, alignment = TextAnchor.MiddleCenter };
        }

        static Texture2D Disc(int size, bool ring)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp };
            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                float a;
                if (!ring) a = Mathf.Clamp01((1f - d) * size * 0.25f);
                else
                {
                    // The radar face: dark disc, range rings and a bright rim.
                    a = d > 1f ? 0f : 0.55f;
                    float rim = Mathf.Abs(d - 0.985f) < 0.012f ? 1f : 0f;
                    float rings = (Mathf.Abs(d - 0.33f) < 0.004f || Mathf.Abs(d - 0.66f) < 0.004f) ? 0.5f : 0f;
                    float cross = (Mathf.Abs(x - c) < 0.6f || Mathf.Abs(y - c) < 0.6f) && d < 0.98f ? 0.25f : 0f;
                    float line = Mathf.Max(rim, Mathf.Max(rings, cross));
                    var col = Color.Lerp(new Color(0.02f, 0.05f, 0.09f), new Color(0.4f, 0.7f, 0.95f), line);
                    t.SetPixel(x, y, new Color(col.r, col.g, col.b, Mathf.Max(a, line * 0.9f)));
                    continue;
                }
                t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            t.Apply();
            return t;
        }

        // Pointing up (toward -y in GUI space).
        static Texture2D Triangle(int size)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float v = y / (float)(size - 1);           // texture row 0 draws at the bottom
                float u = Mathf.Abs(x / (float)(size - 1) - 0.5f);
                float a = u <= 0.5f * (1f - v) ? 1f : 0f;  // wide at the bottom, a point at the top
                t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            t.Apply();
            return t;
        }

        void Fill(Rect r, Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, _white);
            GUI.color = old;
        }

        void Draw(Rect r, Texture t, Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, t);
            GUI.color = old;
        }

        void Bar(Rect r, float v, Color c)
        {
            Fill(r, new Color(0f, 0f, 0f, 0.55f));
            Fill(new Rect(r.x, r.y, r.width * Mathf.Clamp01(v), r.height), c);
        }

        Rect PanelBox(Rect r, Color accent)
        {
            Fill(r, Panel);
            Fill(new Rect(r.x, r.y, r.width, 1), accent);
            Fill(new Rect(r.x, r.yMax - 1, r.width, 1), Rule);
            _building.Add(r);
            return r;
        }

        bool Clicked(Rect r, int button = 0)
        {
            var e = Event.current;
            if (e.type == EventType.MouseDown && e.button == button && r.Contains(e.mousePosition))
            {
                e.Use();
                return true;
            }
            return false;
        }

        void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && Runner != null && Runner.World != null && kb.rKey.wasPressedThisFrame && !kb.ctrlKey.isPressed)
                ShowReserves = !ShowReserves;
        }

        void OnGUI()
        {
            if (CaptureMode.Active || BenchMode.Active || Runner == null || W == null) return;
            Styles();
            var ev = Event.current;
            if (ev.type == EventType.Layout) _building.Clear();
            bool repaint = ev.type == EventType.Repaint;

            if (repaint) FieldMarkers();
            DragBox(repaint);
            TopBar();
            Radar();
            Reserves();
            Cards();
            AbilityBar();
            Banner();

            if (repaint)
            {
                GUI.Label(new Rect(16, Screen.height - 22, Screen.width - 300, 18),
                    "click/drag select · right-click move or attack · right-drag set facing · Alt keep shape · Ctrl face · H hold · Z/X/V abilities · R reinforcements · Ctrl+1–9 groups · WASD pan · Q/E turn · wheel zoom · F follow · Space pause · +/- speed",
                    _tiny);
                Owned.Clear();
                Owned.AddRange(_building);
            }
        }

        // --- Over the field ------------------------------------------------------------------

        void FieldMarkers()
        {
            var cam = Rig.Camera;
            Vector3 cp = cam.transform.position;
            foreach (var kv in Runner.Presenter.Views)
            {
                var ship = W.ShipById(kv.Key);
                if (ship == null || !ship.Alive) continue;
                var view = kv.Value;
                Vector3 wp = view.transform.position;
                Vector3 sp = cam.WorldToScreenPoint(wp);
                if (sp.z <= 0f || sp.x < -50 || sp.x > Screen.width + 50 || sp.y < -50 || sp.y > Screen.height + 50) continue;
                float gy = Screen.height - sp.y;
                bool own = ship.Side == Input.PlayerSide;
                bool selected = Input.Selected.Contains(ship.Id);
                bool hovered = Input.Hovered == ship.Id;
                var side = own ? Friend : Foe;

                // Size of the hull on screen.
                float px = ship.Def.HalfLength * 2f / Mathf.Max(1f, sp.z) * (Screen.height / (2f * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad)));
                if (px < 10f)
                {
                    // A contact marker: triangle for warships, square for stations, dot for troop ships.
                    float s = ship.Def.Class == HullClass.Station ? 12f : ship.Def.Class >= HullClass.Destroyer ? 10f : 7f;
                    var c = new Color(side.r, side.g, side.b, selected ? 1f : 0.8f);
                    if (ship.Def.Class == HullClass.Station) Fill(new Rect(sp.x - s * 0.5f, gy - s * 0.5f, s, s), c);
                    else if (ship.Def.Class == HullClass.Transport) Draw(new Rect(sp.x - 4, gy - 4, 8, 8), _disc, c);
                    else
                    {
                        var m = GUI.matrix;
                        GUIUtility.RotateAroundPivot(HeadingOnScreen(cam, ship, view), new Vector2(sp.x, gy));
                        Draw(new Rect(sp.x - s * 0.5f, gy - s * 0.5f, s, s), _tri, c);
                        GUI.matrix = m;
                    }
                }

                // Health: always for the selection and the hovered ship; for others when hurt or near.
                float hull = ship.Hull / ship.Def.HullHp;
                float shield = ship.Def.ShieldHp > 0f ? ship.Shield / ship.Def.ShieldHp : 0f;
                bool near = (wp - cp).sqrMagnitude < (ship.Def.HalfLength * 60f) * (ship.Def.HalfLength * 60f);
                if (!(selected || hovered || near || hull < 0.999f)) continue;
                float w = Mathf.Clamp(px * 0.6f, 26f, ship.Def.Class == HullClass.Station ? 110f : 80f);
                float top = gy - Mathf.Max(px * 0.35f, 10f) - 10f;
                float alpha = selected || hovered ? 1f : 0.75f;
                Fill(new Rect(sp.x - w * 0.5f - 1, top - 1, w + 2, 8), new Color(0f, 0f, 0f, 0.5f * alpha));
                Fill(new Rect(sp.x - w * 0.5f, top, w * shield, 3), new Color(ShieldColor.r, ShieldColor.g, ShieldColor.b, alpha));
                Fill(new Rect(sp.x - w * 0.5f, top + 4, w * hull, 3), new Color(side.r, side.g, side.b, alpha));
                if (ship.ActiveAbility >= 0)
                    GUI.Label(new Rect(sp.x - 40, top - 16, 80, 14), AbilityShort((Ability)ship.ActiveAbility), new GUIStyle(_key) { normal = { textColor = side } });
            }
        }

        // The ship's heading as a GUI rotation, taken from where its bow lands on screen.
        static float HeadingOnScreen(Camera cam, Ship ship, ShipView view)
        {
            Vector3 a = cam.WorldToScreenPoint(view.transform.position);
            Vector3 b = cam.WorldToScreenPoint(view.transform.position + view.transform.forward * 100f);
            Vector2 d = new Vector2(b.x - a.x, -(b.y - a.y));
            return Mathf.Atan2(d.x, -d.y) * Mathf.Rad2Deg;
        }

        void DragBox(bool repaint)
        {
            if (!repaint || Input == null || !Input.Dragging) return;
            var r = Input.DragRect;
            var g = new Rect(r.x, Screen.height - r.yMax, r.width, r.height);
            Fill(g, new Color(0.45f, 0.85f, 1f, 0.08f));
            Fill(new Rect(g.x, g.y, g.width, 1), Edge);
            Fill(new Rect(g.x, g.yMax - 1, g.width, 1), Edge);
            Fill(new Rect(g.x, g.y, 1, g.height), Edge);
            Fill(new Rect(g.xMax - 1, g.y, 1, g.height), Edge);
        }

        // --- Top ------------------------------------------------------------------------------

        void TopBar()
        {
            int own = 0, enemy = 0;
            foreach (var s in W.Ships)
                if (s.Alive) { if (s.Side == Input.PlayerSide) own++; else enemy++; }
            var res = Runner.Reinforcements;
            string cmd = res != null ? $"     COMMAND {res.Used(W, Input.PlayerSide)}/{res.Cap[Input.PlayerSide]}" : "";
            string time = $"×{Runner.TimeScale:0.##}";
            GUI.Label(new Rect(20, 12, 700, 24), $"{time}     FLEET {own}     CONTACTS {enemy}{cmd}", _text);
            string terrain = Runner.Terrain.Describe();
            GUI.Label(new Rect(Screen.width * 0.5f - 200, 12, 400, 24), terrain.ToUpperInvariant(), new GUIStyle(_caps) { alignment = TextAnchor.MiddleCenter });

            SpeedBar();
            if (Runner.CampaignBattle != null && Director != null && Runner.State == BattleState.Fighting)
            {
                var wr = new Rect(Screen.width - 210, 44, 192, 28);
                _building.Add(wr);
                if (GUI.Button(wr, "Withdraw from the system")) Director.Withdraw();
            }
        }

        // Pause and battle speed, top right (Space, - and = do the same).
        void SpeedBar()
        {
            const float w = 34f, h = 24f, gap = 4f;
            var speeds = SimRunner.Speeds;
            float x = Screen.width - 18f - (w + gap) * (speeds.Length + 1) + gap, y = 12f;
            if (Runner.Paused) GUI.Label(new Rect(x - 80f, y, 72f, h), "PAUSED", new GUIStyle(_text) { alignment = TextAnchor.MiddleRight });
            for (int i = -1; i < speeds.Length; i++)
            {
                var r = new Rect(x + (i + 1) * (w + gap), y, w, h);
                _building.Add(r);
                bool on = i < 0 ? Runner.Paused : !Runner.Paused && Mathf.Approximately(Runner.TimeScale, speeds[i]);
                bool hover = r.Contains(Event.current.mousePosition);
                Fill(r, on ? new Color(0.15f, 0.35f, 0.5f, 0.95f) : Panel);
                Fill(new Rect(r.x, r.y, r.width, 1), on ? Edge : hover ? Ink : Rule);
                if (i < 0)
                {
                    Fill(new Rect(r.center.x - 5, r.y + 6, 3, 12), Ink);
                    Fill(new Rect(r.center.x + 2, r.y + 6, 3, 12), Ink);
                }
                else
                {
                    string label = speeds[i] < 1f ? "½" : $"×{speeds[i]:0}";
                    GUI.Label(r, label, _center);
                }
                if (!Clicked(r)) continue;
                if (i < 0) Runner.Paused = !Runner.Paused;
                else
                {
                    Runner.TimeScale = speeds[i];
                    Runner.Paused = false;
                }
            }
        }

        void Banner()
        {
            if (Runner.State == BattleState.Fighting) return;
            bool won = Runner.State == BattleState.Victory;
            bool broke = Runner.State == BattleState.Withdrawn;
            _banner.normal.textColor = won ? new Color(0.6f, 0.95f, 1f) : broke ? new Color(0.9f, 0.85f, 0.6f) : new Color(1f, 0.45f, 0.4f);
            GUI.Label(new Rect(0, Screen.height * 0.3f, Screen.width, 60), won ? "VICTORY" : broke ? "DISENGAGED" : "FLEET LOST", _banner);
            if (Runner.CampaignBattle != null && Director != null)
            {
                var r = new Rect(Screen.width * 0.5f - 140, Screen.height * 0.3f + 70, 280, 34);
                _building.Add(r);
                if (GUI.Button(r, "Return to the galaxy")) Director.ReturnToMap();
            }
            else GUI.Label(new Rect(0, Screen.height * 0.3f + 56, Screen.width, 24), "Ctrl+R for a new engagement", new GUIStyle(_small) { alignment = TextAnchor.MiddleCenter });
        }

        // --- Radar ----------------------------------------------------------------------------

        void Radar()
        {
            const float size = 220f;
            var r = new Rect(14, Screen.height - size - 34, size, size);
            _building.Add(r);
            Vector2 c = r.center;
            float rad = size * 0.5f - 4f;

            // The whole field: everything alive and every obstacle, eased so it doesn't jump.
            float want = 6000f;
            foreach (var s in W.Ships)
                if (s.Alive) want = Mathf.Max(want, new Vector2(s.Position.X, s.Position.Z).magnitude * 1.08f);
            foreach (var o in W.Obstacles)
                want = Mathf.Max(want, (new Vector2(o.Position.X, o.Position.Z).magnitude + o.Radius) * 1.02f);
            _radarRange = Mathf.Lerp(_radarRange, Mathf.Min(want, 40000f), 1f - Mathf.Exp(-Time.unscaledDeltaTime * 2f));
            Vector2 ToRadar(float x, float z) => c + new Vector2(x, -z) / _radarRange * rad;

            var e = Event.current;
            if (e.type == EventType.MouseDown && r.Contains(e.mousePosition) && Vector2.Distance(e.mousePosition, c) <= rad)
            {
                Vector2 d = (e.mousePosition - c) / rad * _radarRange;
                var world = new Vector3(d.x, Input.SelectionCentre().y, -d.y);
                if (e.button == 0) Rig.LookAt(world);
                else if (e.button == 1 && Input.Selected.Count > 0) Input.OrderMove(world);
                e.Use();
            }
            if (e.type != EventType.Repaint) return;

            Draw(r, _ring, Color.white);
            foreach (var o in W.Obstacles)
            {
                Vector2 p = ToRadar(o.Position.X, o.Position.Z);
                float s = Mathf.Max(3f, o.Radius / _radarRange * rad * 2f);
                var col = o.Kind == ObstacleKind.Nebula ? new Color(0.6f, 0.35f, 0.95f, 0.28f) : new Color(0.6f, 0.58f, 0.55f, 0.7f);
                Draw(new Rect(p.x - s * 0.5f, p.y - s * 0.5f, s, s), _disc, col);
            }
            foreach (var s in W.Ships)
            {
                if (!s.Alive) continue;
                bool own = s.Side == Input.PlayerSide;
                // A ship hidden in a nebula doesn't show on the enemy's scope unless someone is close.
                if (!own && s.InNebula && !Spotted(s)) continue;
                Vector2 p = ToRadar(s.Position.X, s.Position.Z);
                var col = own ? Friend : Foe;
                if (own && Input.Selected.Contains(s.Id)) col = Color.white;
                if (s.Def.Class == HullClass.Station) { Fill(new Rect(p.x - 4, p.y - 4, 8, 8), col); continue; }
                if (s.Def.Class == HullClass.Transport) { Draw(new Rect(p.x - 3, p.y - 3, 6, 6), _disc, col); continue; }
                float sz = s.Def.Class >= HullClass.Destroyer ? 9f : 6f;
                var m = GUI.matrix;
                GUIUtility.RotateAroundPivot(s.Yaw * Mathf.Rad2Deg, p);
                Draw(new Rect(p.x - sz * 0.5f, p.y - sz * 0.5f, sz, sz), _tri, col);
                GUI.matrix = m;
            }
            // The camera: where it looks, and which way.
            Vector2 f = ToRadar(Rig.FocusPoint.x, Rig.FocusPoint.z);
            var mm = GUI.matrix;
            GUIUtility.RotateAroundPivot(Rig.Yaw, f);
            Draw(new Rect(f.x - 7, f.y - 7, 14, 14), _tri, new Color(1f, 1f, 1f, 0.9f));
            GUI.matrix = mm;
            GUI.Label(new Rect(r.x, r.yMax + 2, size, 16), $"{_radarRange / 1000f:0} km", new GUIStyle(_tiny) { alignment = TextAnchor.MiddleCenter });
        }

        bool Spotted(Ship s)
        {
            foreach (var o in W.Ships)
                if (o.Alive && o.Side == Input.PlayerSide && System.Numerics.Vector3.DistanceSquared(o.Position, s.Position) < World.NebulaSightRange * World.NebulaSightRange) return true;
            return false;
        }

        // --- Reinforcements --------------------------------------------------------------------

        void Reserves()
        {
            var res = Runner.Reinforcements;
            if (res == null) return;
            int side = Input.PlayerSide;
            var pool = res.Pool[side];
            const float w = 300f, rowH = 46f;
            int shown = ShowReserves ? Mathf.Min(pool.Count, 10) : 0;
            bool placing = Input.Placing.Count > 0;
            float h = 40f + shown * rowH + (pool.Count > shown && ShowReserves ? 20f : 0f) + (ShowReserves && pool.Count == 0 ? 24f : 0f)
                      + (ShowReserves && pool.Count > 0 ? 40f : 0f);
            var r = PanelBox(new Rect(Screen.width - w - 14, 120, w, h), Edge);
            if (Clicked(new Rect(r.x, r.y, r.width, 36))) ShowReserves = !ShowReserves;

            var keyR = new Rect(r.x + 12, r.y + 9, 20, 20);
            Fill(keyR, new Color(1f, 1f, 1f, 0.08f));
            GUI.Label(keyR, "R", _key);
            int used = res.Used(W, side);
            int picked = 0;
            foreach (var p in Input.Placing) picked += Reinforcements.Points(p.Def.Class);
            GUI.Label(new Rect(r.x + 42, r.y + 8, w - 50, 22), $"REINFORCEMENTS  <color=#8aa0b8><size=12>{pool.Count} waiting · command {used}/{res.Cap[side]}</size></color>", _text);
            if (!ShowReserves) return;
            if (pool.Count == 0)
            {
                GUI.Label(new Rect(r.x + 14, r.y + 38, w - 20, 20), "every ship is on the field", _small);
                return;
            }
            for (int i = 0; i < shown; i++)
            {
                var ship = pool[i];
                int pts = Reinforcements.Points(ship.Def.Class);
                bool on = Input.Placing.Contains(ship);
                bool fits = on || used + picked + pts <= res.Cap[side];
                var row = new Rect(r.x + 8, r.y + 38 + i * rowH, w - 16, rowH - 4);
                bool hover = row.Contains(Event.current.mousePosition);
                Fill(row, on ? new Color(0.15f, 0.35f, 0.5f, 0.9f) : new Color(1f, 1f, 1f, hover && fits ? 0.09f : 0.035f));
                var tone = fits ? Ink : Muted;
                string name = string.IsNullOrEmpty(ship.Name) ? ship.Def.Id : ship.Name;
                GUI.Label(new Rect(row.x + 10, row.y, row.width - 60, 20), $"<color=#{ColorUtility.ToHtmlStringRGB(tone)}>{name}</color>", _text);
                GUI.Label(new Rect(row.x + 10, row.y + 18, row.width - 60, 20), ClassName(ship.Def) + (fits ? "" : "  <color=#ff9a80>no room</color>"), _small);
                Bar(new Rect(row.x + 10, row.yMax - 5, row.width - 70, 2), ship.Hull / ship.Def.HullHp, HullColor);
                var badge = new Rect(row.xMax - 38, row.y + 8, 28, 24);
                Fill(badge, new Color(1f, 1f, 1f, 0.06f));
                GUI.Label(badge, pts.ToString(), _key);
                // Pick the ships (Shift adds), then click the field where they should arrive.
                if (Clicked(row))
                {
                    bool add = Event.current.shift;
                    if (on) Input.Placing.Remove(ship);
                    else if (add ? fits : used + pts <= res.Cap[side])
                    {
                        if (!add) Input.Placing.Clear();
                        Input.Placing.Add(ship);
                    }
                }
            }
            if (pool.Count > shown)
                GUI.Label(new Rect(r.x + 14, r.y + 38 + shown * rowH, w - 20, 18), $"and {pool.Count - shown} more", _tiny);

            // Footer: pick everything that fits, or what to do with what is picked.
            var foot = new Rect(r.x + 8, r.yMax - 38, w - 16, 30);
            if (placing)
            {
                string where = !Input.PlaceOnField ? "" : Input.PlaceValid ? "  <color=#8fe3a8>in the zone</color>" : "  <color=#ff9a80>too far from our ships</color>";
                GUI.Label(new Rect(foot.x + 4, foot.y, foot.width, 16), $"{Input.Placing.Count} picked · click the field to call them in{where}", _tiny);
                GUI.Label(new Rect(foot.x + 4, foot.y + 14, foot.width, 16), "Shift adds · right click or Esc cancels", _tiny);
            }
            else
            {
                bool hover = foot.Contains(Event.current.mousePosition);
                Fill(foot, new Color(1f, 1f, 1f, hover ? 0.1f : 0.05f));
                GUI.Label(foot, "PICK ALL THAT FIT", _center);
                if (Clicked(foot))
                {
                    int room = res.Cap[side] - used;
                    foreach (var ship in pool)
                    {
                        int pts = Reinforcements.Points(ship.Def.Class);
                        if (pts > room) continue;
                        room -= pts;
                        Input.Placing.Add(ship);
                    }
                }
            }
        }

        static string ClassName(HullDef d)
        {
            string cls = d.Class switch
            {
                HullClass.Battleship => "Battleship",
                HullClass.Cruiser => "Cruiser",
                HullClass.Destroyer => "Destroyer",
                HullClass.Frigate => "Frigate",
                HullClass.Corvette => "Corvette",
                HullClass.Transport => "Troop transport",
                _ => "Station",
            };
            return string.IsNullOrEmpty(d.Design) ? cls : $"{cls} · {d.Design}";
        }

        // --- Selection cards --------------------------------------------------------------------

        readonly List<(HullDef Def, List<Ship> Ships)> _groups = new List<(HullDef, List<Ship>)>();

        void Cards()
        {
            if (Input.Selected.Count == 0) return;
            // One card per ship, or per kind of ship once the selection is large.
            _groups.Clear();
            bool grouped = Input.Selected.Count > 8;
            foreach (int id in Input.Selected)
            {
                var s = W.ShipById(id);
                if (s == null || !s.Alive) continue;
                int g = -1;
                if (grouped)
                    for (int i = 0; i < _groups.Count; i++)
                        if (_groups[i].Def == s.Def) g = i;
                if (g < 0)
                {
                    _groups.Add((s.Def, new List<Ship>()));
                    g = _groups.Count - 1;
                }
                _groups[g].Ships.Add(s);
            }
            const float cw = 150f, ch = 72f, gap = 6f;
            int n = Mathf.Min(_groups.Count, 9);
            float total = n * cw + (n - 1) * gap;
            float x0 = Screen.width * 0.5f - total * 0.5f, y = Screen.height - ch - 30f;
            for (int k = 0; k < n; k++)
            {
                var (def, ships) = _groups[k];
                var r = PanelBox(new Rect(x0 + k * (cw + gap), y, cw, ch), Edge);
                float hull = 0f, shield = 0f;
                int guns = 0, mounts = 0;
                foreach (var s in ships)
                {
                    hull += s.Hull / s.Def.HullHp;
                    shield += s.Def.ShieldHp > 0f ? s.Shield / s.Def.ShieldHp : 0f;
                    foreach (var m in s.Mounts) { mounts++; if (m.Alive) guns++; }
                }
                hull /= ships.Count;
                shield /= ships.Count;
                var keyR = new Rect(r.x + 6, r.y + 6, 18, 18);
                Fill(keyR, new Color(1f, 1f, 1f, 0.08f));
                GUI.Label(keyR, (k + 1).ToString(), _key);
                string title = ships.Count > 1 ? $"{ClassName(def).Split('·')[0].Trim()} ×{ships.Count}" : ClassName(def).Split('·')[0].Trim();
                GUI.Label(new Rect(r.x + 30, r.y + 4, cw - 34, 20), title, _text);
                var lead = ships[0];
                string status = ships.Count > 1 ? $"{guns}/{mounts} guns" : $"{OrderText(lead)} · {guns}/{mounts} guns";
                if (lead.ActiveAbility >= 0 && ships.Count == 1) status = AbilityShort((Ability)lead.ActiveAbility) + " · " + status;
                if (lead.InNebula) status += " · <color=#c49aff>nebula</color>";
                GUI.Label(new Rect(r.x + 8, r.y + 26, cw - 12, 18), status, _tiny);
                Bar(new Rect(r.x + 8, r.y + 50, cw - 16, 4), shield, ShieldColor);
                Bar(new Rect(r.x + 8, r.y + 58, cw - 16, 4), hull, HullColor);
                if (Clicked(r))
                {
                    Input.Selected.Clear();
                    foreach (var s in ships) Input.Selected.Add(s.Id);
                }
            }
            if (_groups.Count > n)
                GUI.Label(new Rect(x0 + total + 8, y + ch - 20, 80, 18), $"+{_groups.Count - n}", _small);
        }

        static string OrderText(Ship s) => s.Order.Kind switch
        {
            OrderKind.MoveTo => "moving",
            OrderKind.Attack => "attacking",
            OrderKind.AttackRun => "attack run",
            OrderKind.Face => "turning",
            _ => s.Def.MaxSpeed <= 0f ? "on station" : "holding",
        };

        static string AbilityShort(Ability a) => a switch
        {
            Ability.Engines => "ENGINES",
            Ability.Shields => "SHIELDS",
            _ => "WEAPONS",
        };

        // --- Abilities ------------------------------------------------------------------------

        static readonly (Ability A, string Key, string Label)[] AbilityKeys =
        {
            (Ability.Engines, "Z", "ENG"),
            (Ability.Shields, "X", "SHD"),
            (Ability.Weapons, "V", "WPN"),
        };

        void AbilityBar()
        {
            const float s = 58f, gap = 8f;
            float x = Screen.width - (s + gap) * AbilityKeys.Length - 8f, y = Screen.height - s - 30f;
            bool any = Input.Selected.Count > 0;
            for (int i = 0; i < AbilityKeys.Length; i++)
            {
                var (a, key, label) = AbilityKeys[i];
                var r = new Rect(x + i * (s + gap), y, s, s);
                _building.Add(r);
                // Ready if any selected ship can use it; otherwise show the shortest cooldown.
                bool ready = false, active = false, has = false;
                float cd = float.MaxValue, cdMax = Abilities.Cooldown(a);
                foreach (int id in Input.Selected)
                {
                    var ship = W.ShipById(id);
                    if (ship == null || !ship.Alive || !Abilities.Has(ship, a)) continue;
                    has = true;
                    if (ship.Using(a)) active = true;
                    if (ship.CanUse(a)) ready = true;
                    cd = Mathf.Min(cd, Mathf.Max(0f, ship.AbilityCooldown[(int)a]));
                }
                Fill(r, active ? new Color(0.15f, 0.35f, 0.5f, 0.95f) : Panel);
                Fill(new Rect(r.x, r.y, r.width, 1), ready ? Edge : Rule);
                if (has && !ready && cd < float.MaxValue && cd > 0f)
                    Fill(new Rect(r.x, r.y, r.width, r.height * Mathf.Clamp01(cd / cdMax)), new Color(0f, 0f, 0f, 0.5f));
                var tone = !any || !has ? new Color(1f, 1f, 1f, 0.25f) : ready || active ? Ink : Muted;
                GUI.Label(new Rect(r.x, r.y + 12, s, 22), $"<color=#{ColorUtility.ToHtmlStringRGBA(tone)}>{label}</color>", _center);
                GUI.Label(new Rect(r.x + 3, r.y + 2, 14, 14), key, new GUIStyle(_key) { fontSize = 10 });
                if (has && !ready && !active && cd < float.MaxValue && cd > 0f)
                    GUI.Label(new Rect(r.x, r.y + 34, s, 16), $"{cd:0}s", new GUIStyle(_tiny) { alignment = TextAnchor.MiddleCenter });
                if (r.Contains(Event.current.mousePosition) && Event.current.type == EventType.Repaint)
                    GUI.Label(new Rect(r.x - 120, r.y - 22, s + 120, 18), Abilities.Name(a), new GUIStyle(_small) { alignment = TextAnchor.MiddleRight });
                if (Clicked(r)) Input.UseAbility(a);
            }
        }
    }
}
