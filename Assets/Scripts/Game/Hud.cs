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

        static readonly Color Panel = UiKit.Panel;
        static readonly Color Rule = UiKit.Line;
        static readonly Color Edge = new Color(UiKit.Amber.r, UiKit.Amber.g, UiKit.Amber.b, 0.75f);
        static readonly Color Ink = UiKit.Ink;
        static readonly Color Muted = UiKit.Mute;
        static readonly Color HullColor = new Color(0.9f, 0.76f, 0.46f);
        static readonly Color ShieldColor = UiKit.Shield;
        static readonly Color Friend = new Color(0.4f, 0.78f, 1f);
        static readonly Color Foe = new Color(1f, 0.32f, 0.28f);

        World W => Runner.World;

        void Styles()
        {
            if (_white != null) return;
            UiKit.Init(Runner.Art);
            _white = Texture2D.whiteTexture;
            _disc = Disc(64, false);
            _ring = Disc(256, true);
            _tri = Triangle(32);
            var art = Runner.Art;
            var font = art != null && art.UiFont != null ? art.UiFont : GUI.skin.font;
            var bold = art != null && art.UiFontBold != null ? art.UiFontBold : font;
            _text = new GUIStyle(GUI.skin.label) { font = bold, fontSize = 15, richText = true, normal = { textColor = Ink } };
            _small = new GUIStyle(_text) { font = UiKit.Mono, fontSize = 12, normal = { textColor = Muted } };
            _tiny = new GUIStyle(_small) { fontSize = 10 };
            _caps = new GUIStyle(_text) { font = UiKit.Mono, fontSize = 12, normal = { textColor = UiKit.Amber } };
            _banner = new GUIStyle(_text) { font = UiKit.Stencil, fontSize = 52, alignment = TextAnchor.MiddleCenter };
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
                    // The plotting board: dark green face, range rings, bearing ticks every ten degrees.
                    a = d > 1f ? 0f : 0.82f;
                    float rim = Mathf.Abs(d - 0.985f) < 0.012f ? 1f : 0f;
                    float rings = (Mathf.Abs(d - 0.33f) < 0.004f || Mathf.Abs(d - 0.66f) < 0.004f) ? 0.45f : 0f;
                    float cross = (Mathf.Abs(x - c) < 0.6f || Mathf.Abs(y - c) < 0.6f) && d < 0.98f ? 0.2f : 0f;
                    float ang = Mathf.Atan2(y - c, x - c) * Mathf.Rad2Deg;
                    float step = Mathf.Abs(Mathf.DeltaAngle(ang, Mathf.Round(ang / 10f) * 10f));
                    float tick = d > 0.94f && d < 0.985f && step * d * 3.14159f / 180f * c < 0.9f ? 0.8f : 0f;
                    float line = Mathf.Max(Mathf.Max(rim, tick), Mathf.Max(rings, cross));
                    var col = Color.Lerp(new Color(0.035f, 0.055f, 0.045f), new Color(0.42f, 0.66f, 0.5f), line);
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
                GameAudio.Ui("click");
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
            DirectiveSlip();
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
            string time = $"×{Runner.TimeScale:0.##}";
            GUI.Label(new Rect(20, 12, 700, 24), $"{time}     FLEET {own}     CONTACTS {enemy}", _caps);
            if (res != null)
            {
                int cap = res.Cap[Input.PlayerSide], used = res.Used(W, Input.PlayerSide);
                float px = 20f;
                GUI.Label(new Rect(px, 34, 90, 16), "COMMAND", _tiny);
                for (int i = 0; i < Mathf.Min(cap, 40); i++)
                    UiKit.Fill(new Rect(px + 62 + i * 8, 36, 5, 11), i < used ? UiKit.Amber : new Color(0.17f, 0.15f, 0.1f));
            }
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

        // The after-action ledger: what the engagement cost, in the figures the campaign keeps.
        void Banner()
        {
            if (Runner.State == BattleState.Fighting) return;
            bool won = Runner.State == BattleState.Victory;
            bool broke = Runner.State == BattleState.Withdrawn;
            int me = Input.PlayerSide;

            int ours = 0, lost = 0, oursCost = 0, lostCost = 0, gunsLost = 0, gunsTotal = 0;
            int theirs = 0, killed = 0, killedCost = 0, fled = 0;
            float hullSum = 0f;
            var survivors = new List<Ship>();
            var losses = new List<Ship>();
            string station = null;
            foreach (var sh in W.Ships)
            {
                if (sh.Def.Class == HullClass.Station)
                {
                    if (sh.Side == me) station = sh.Alive ? $"Orbital station standing, hull {sh.Hull / sh.Def.HullHp * 100f:0}%" : "Orbital station destroyed";
                    continue;
                }
                bool own = sh.Side == me;
                if (own)
                {
                    ours++;
                    oursCost += sh.Def.Cost;
                    foreach (var m in sh.Mounts) { gunsTotal++; if (!m.Alive) gunsLost++; }
                    if (sh.Alive) { survivors.Add(sh); hullSum += sh.Hull / sh.Def.HullHp; }
                    else { lost++; lostCost += sh.Def.Cost; losses.Add(sh); }
                }
                else
                {
                    theirs++;
                    if (sh.Alive) fled++;
                    else { killed++; killedCost += sh.Def.Cost; }
                }
            }

            UiKit.Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.55f));
            _building.Add(new Rect(0, 0, Screen.width, Screen.height));
            float w = Mathf.Min(880f, Screen.width - 40f), h = Mathf.Min(520f, Screen.height - 40f);
            var r = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            UiKit.Fill(r, UiKit.Bg);
            UiKit.Outline(r, UiKit.AmberDim, 2f);

            string verdict = won ? "VICTORY" : broke ? "DISENGAGED" : "FLEET LOST";
            var vc = won ? UiKit.Ok : broke ? UiKit.Warn : UiKit.Red;
            UiKit.Text(new Rect(r.x + 24, r.y + 14, r.width - 260, 36), "AFTER-ACTION LEDGER", UiKit.Face.Stencil, 30, UiKit.Amber);
            UiKit.Text(new Rect(r.x + 26, r.y + 52, r.width - 260, 18), $"ENGAGEMENT CLOSED  ·  {(int)(W.Time / 60f):00}:{(int)(W.Time % 60f):00}", UiKit.Face.Mono, 12, UiKit.Mute);
            UiKit.Stamp(new Rect(r.xMax - 220, r.y + 20, 190, 36), verdict, vc, -2f, 20);
            UiKit.Fill(new Rect(r.x + 24, r.y + 78, r.width - 48, 2), UiKit.AmberDim);

            float colW = (r.width - 48 - 20) / 2f, y0 = r.y + 94;
            var left = new Rect(r.x + 24, y0, colW, 190);
            var right = new Rect(left.xMax + 20, y0, colW, 190);
            UiKit.Plate(left, "Our line");
            UiKit.Plate(right, "The enemy");
            float y = left.y + 38;
            LedgerRow(left, ref y, "Hulls committed", ours.ToString());
            LedgerRow(left, ref y, "Hulls lost", lost.ToString(), lost > 0 ? UiKit.Red : UiKit.Ok);
            LedgerRow(left, ref y, "Value lost", $"{lostCost} cr", lostCost > 0 ? UiKit.Warn : UiKit.Mute);
            LedgerRow(left, ref y, "Guns shot away", $"{gunsLost} of {gunsTotal}", gunsLost > 0 ? UiKit.Warn : UiKit.Mute);
            LedgerRow(left, ref y, "Survivor hull", survivors.Count > 0 ? $"{hullSum / survivors.Count * 100f:0}% mean" : "none");
            y = right.y + 38;
            LedgerRow(right, ref y, "Hulls fielded", theirs.ToString());
            LedgerRow(right, ref y, "Hulls destroyed", killed.ToString(), killed > 0 ? UiKit.Ok : UiKit.Mute);
            LedgerRow(right, ref y, "Value destroyed", $"{killedCost} cr", killedCost > 0 ? UiKit.Ok : UiKit.Mute);
            LedgerRow(right, ref y, "Left on the field", fled.ToString(), fled > 0 ? UiKit.Warn : UiKit.Mute);
            if (station != null) LedgerRow(right, ref y, "Our station", station.Replace("Orbital station ", ""));

            // Survivors, as they are: damage stays until a yard repairs it.
            var strip = new Rect(r.x + 24, left.yMax + 14, r.width - 48, r.height - 94 - 190 - 14 - 76);
            UiKit.Plate(strip, survivors.Count > 0 ? "Survivors — damage stays until a yard repairs it" : "No survivors");
            float cw = Mathf.Min(150f, (strip.width - 24f) / Mathf.Max(1, Mathf.Min(survivors.Count, 6)) - 6f);
            int shown = Mathf.Min(survivors.Count, 6);
            for (int i = 0; i < shown; i++)
            {
                var sh = survivors[i];
                var cell = new Rect(strip.x + 12 + i * (cw + 6), strip.y + 34, cw, strip.height - 44);
                UiKit.Outline(cell, UiKit.Line);
                float picH = cw * HullSchematic.H / HullSchematic.W;
                UiKit.Tex(new Rect(cell.x, cell.y + 2, cw, picH), _schematic.Get(sh), Color.white);
                var (state, tone) = Condition(sh, 1, sh.Hull / sh.Def.HullHp);
                UiKit.Text(new Rect(cell.x + 4, cell.y + picH + 4, cw - 8, 14), ClassName(sh.Def).Split('·')[0].Trim().ToUpperInvariant(), UiKit.Face.Bold, 12, UiKit.Ink);
                UiKit.Text(new Rect(cell.x + 4, cell.y + picH + 18, cw - 8, 14), state, UiKit.Face.Mono, 9, tone);
            }
            if (survivors.Count > shown) UiKit.Text(new Rect(strip.xMax - 90, strip.y + 6, 80, 16), $"+{survivors.Count - shown} more", UiKit.Face.Mono, 10, UiKit.Mute, TextAnchor.MiddleRight);
            if (losses.Count > 0)
            {
                var names = new List<string>();
                foreach (var sh in losses) names.Add(ClassName(sh.Def).Split('·')[0].Trim());
                UiKit.Text(new Rect(strip.x + 14, strip.yMax - 22, strip.width - 28, 16), "<color=#d9533d>LOST ✕</color>  " + string.Join(", ", names), UiKit.Face.Mono, 10, UiKit.Mute);
            }

            // The way out.
            var btn = new Rect(r.xMax - 24 - 260, r.yMax - 52, 260, 36);
            string label = Runner.CampaignBattle != null && Director != null ? "Return to the galaxy" : "New engagement (Ctrl+R)";
            bool hover = btn.Contains(Event.current.mousePosition);
            UiKit.Fill(btn, hover ? UiKit.Amber : new Color(0.082f, 0.071f, 0.039f));
            UiKit.Outline(btn, UiKit.AmberDim);
            UiKit.Text(btn, UiKit.Spaced(label), UiKit.Face.Mono, 12, hover ? new Color(0.05f, 0.04f, 0.02f) : UiKit.Amber, TextAnchor.MiddleCenter);
            _building.Add(btn);
            if (Runner.CampaignBattle != null && Director != null && Clicked(btn)) Director.ReturnToMap();
        }

        void LedgerRow(Rect plate, ref float y, string label, string value, Color? tone = null)
        {
            UiKit.Text(new Rect(plate.x + 20, y, plate.width - 40, 22), label, UiKit.Face.Body, 16, UiKit.Ink, TextAnchor.MiddleLeft);
            UiKit.Text(new Rect(plate.x + 20, y, plate.width - 40, 22), value, UiKit.Face.Mono, 14, tone ?? UiKit.Ink, TextAnchor.MiddleRight);
            for (float x = plate.x + 170; x < plate.xMax - 120; x += 6) UiKit.Fill(new Rect(x, y + 17, 2, 1), UiKit.Line2);
            y += 28;
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
            GUI.Label(new Rect(r.x + 42, r.y + 8, w - 50, 22), $"REINFORCEMENTS  <color=#b99a55><size=12>{pool.Count} waiting · command {used}/{res.Cap[side]}</size></color>", _text);
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

        readonly HullSchematic _schematic = new HullSchematic();
        readonly HashSet<int> _liveIds = new HashSet<int>();

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
            const float cw = 224f, ch = 146f, gap = 6f;
            int fit = Mathf.Max(1, Mathf.FloorToInt((Screen.width - 540f) / (cw + gap)));
            int n = Mathf.Min(_groups.Count, Mathf.Min(fit, 9));
            float total = n * cw + (n - 1) * gap;
            float x0 = Screen.width * 0.5f - total * 0.5f, y = Screen.height - ch - 30f;
            _liveIds.Clear();
            foreach (var s in W.Ships) if (s.Alive) _liveIds.Add(s.Id);
            _schematic.Forget(_liveIds);
            for (int k = 0; k < n; k++)
            {
                var (def, ships) = _groups[k];
                var lead = ships[0];
                var r = new Rect(x0 + k * (cw + gap), y, cw, ch);
                UiKit.Fill(r, UiKit.Panel);
                UiKit.Outline(r, UiKit.Line);
                _building.Add(r);
                int guns = 0, mounts = 0;
                foreach (var s in ships)
                    foreach (var m in s.Mounts) { mounts++; if (m.Alive) guns++; }

                // Head: key, class, count.
                var keyR = new Rect(r.x + 6, r.y + 5, 16, 16);
                UiKit.Fill(keyR, new Color(1f, 1f, 1f, 0.07f));
                UiKit.Text(keyR, (k + 1).ToString(), UiKit.Face.Mono, 11, UiKit.Amber, TextAnchor.MiddleCenter);
                string title = ClassName(def).Split('·')[0].Trim();
                if (ships.Count > 1) title += $" ×{ships.Count}";
                UiKit.Text(new Rect(r.x + 28, r.y + 3, cw - 130, 20), title.ToUpperInvariant(), UiKit.Face.Bold, 14, UiKit.Ink, TextAnchor.MiddleLeft);
                UiKit.Fill(new Rect(r.x, r.y + 25, cw, 1), UiKit.Line);

                // The hull.
                var pic = new Rect(r.x + 2, r.y + 26, cw - 4, (cw - 4) * HullSchematic.H / HullSchematic.W);
                UiKit.Tex(pic, _schematic.Get(lead), Color.white);

                // Foot: what it is doing, and what it has left.
                float hull = 0f;
                foreach (var s in ships) hull += s.Hull / s.Def.HullHp;
                hull /= ships.Count;
                var (state, tone) = Condition(lead, ships.Count, hull);
                string order = ships.Count > 1 ? "" : OrderText(lead);
                if (lead.ActiveAbility >= 0 && ships.Count == 1) order = AbilityShort((Ability)lead.ActiveAbility);
                UiKit.Text(new Rect(r.xMax - 100, r.y + 4, 92, 18), order.ToUpperInvariant(), UiKit.Face.Mono, 9, UiKit.Mute, TextAnchor.MiddleRight);
                UiKit.Text(new Rect(r.x + 8, r.yMax - 19, cw - 84, 16), state, UiKit.Face.Mono, 10, tone, TextAnchor.MiddleLeft);
                UiKit.Text(new Rect(r.xMax - 76, r.yMax - 19, 68, 16), $"{guns}/{mounts} GUNS", UiKit.Face.Mono, 10, guns < mounts ? UiKit.Warn : UiKit.Mute, TextAnchor.MiddleRight);
                if (lead.InNebula) UiKit.Text(new Rect(r.x + 8, r.y + 28, 90, 14), "IN NEBULA", UiKit.Face.Mono, 9, new Color(0.77f, 0.6f, 1f));

                if (Clicked(r))
                {
                    Input.Selected.Clear();
                    foreach (var s in ships) Input.Selected.Add(s.Id);
                }
            }
            if (_groups.Count > n)
                GUI.Label(new Rect(x0 + total + 8, y + ch - 20, 80, 18), $"+{_groups.Count - n}", _small);
        }

        // The words under the hull picture: the damage said plainly, worst first.
        static (string, Color) Condition(Ship s, int count, float hull)
        {
            if (count > 1) return hull > 0.9f ? ("SOUND", UiKit.Ok) : hull > 0.5f ? ("DAMAGED", UiKit.Warn) : ("MAULED", UiKit.Red);
            int dead = 0, total = 0, deadSide = 0;
            foreach (var m in s.Mounts)
            {
                total++;
                if (m.Alive) continue;
                dead++;
                deadSide += m.Hardpoint.LocalPosition.X >= 0f ? 1 : -1;
            }
            if (hull < 0.25f) return ("CRITICAL", UiKit.Red);
            if (s.Shield <= 0f && s.Def.ShieldHp > 0f && hull < 0.6f) return ("HULL OPEN", UiKit.Red);
            if (dead * 2 >= total && total > 0) return ("BATTERIES LOST", UiKit.Red);
            if (dead > 0) return (deadSide > 0 ? "STARBOARD BANK LOST" : deadSide < 0 ? "PORT BANK LOST" : "GUNS LOST", UiKit.Warn);
            if (s.Def.ShieldHp > 0f && s.Shield <= 0f) return ("SHIELDS DOWN", UiKit.Warn);
            return hull > 0.9f ? ("SOUND", UiKit.Ok) : ("DAMAGED", UiKit.Warn);
        }

        // The last order as a slip of paper, with what the ships said back.
        void DirectiveSlip()
        {
            float age = Time.unscaledTime - Input.DirectiveTime;
            if (age > 14f || Input.DirectiveNumber == 0) return;
            float a = Mathf.Clamp01((14f - age) / 2f);
            var r = new Rect(16, Screen.height - 220 - 34 - 128, 330, 118);
            if (Event.current.type != EventType.Repaint) return;
            var old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, a);
            UiKit.Paper(r, -0.8f);
            var m = GUI.matrix;
            GUIUtility.RotateAroundPivot(-0.8f, r.center);
            UiKit.Text(new Rect(r.x + 12, r.y + 7, r.width - 24, 20), $"DIRECTIVE {Input.DirectiveNumber} — {Input.DirectiveTitle}", UiKit.Face.Stencil, 15, UiKit.PaperInk);
            UiKit.Text(new Rect(r.x + 12, r.y + 29, r.width - 24, 34), Input.DirectiveBody, UiKit.Face.Mono, 12, UiKit.PaperInk, TextAnchor.UpperLeft, true);
            UiKit.Text(new Rect(r.x + 12, r.y + 72, r.width - 24, 40), "readback: " + Input.DirectiveReadback, UiKit.Face.Mono, 11, new Color(0.23f, 0.2f, 0.15f), TextAnchor.UpperLeft, true);
            GUI.matrix = m;
            GUI.color = old;
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
