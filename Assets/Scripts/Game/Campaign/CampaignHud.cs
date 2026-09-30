using System.Collections.Generic;
using System.Linq;
using GalacticRupture.Sim;
using GalacticRupture.Sim.Strategic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GalacticRupture.Game
{
    // The strategic screen: treasury and time, the fleet and system panels (ships, armies,
    // garrisons, the yard), the diplomacy window, the council, offers from other powers, the
    // battle prompt, and the map's mouse (left-click selects, right-click sends a fleet).
    public sealed partial class CampaignHud : MonoBehaviour
    {
        public CampaignDirector Director;
        public MapView Map;
        public Camera Camera;
        public GameArt Art;

        readonly List<Rect> _panels = new List<Rect>();
        bool _showDiplomacy;
        Vector2 _dipScroll;

        public bool ShowDiplomacy { get => _showDiplomacy; set => _showDiplomacy = value; }
        Vector2 _logScroll;

        GUIStyle _text, _small, _title, _caps, _button, _buttonHot, _banner, _label, _tiny;
        Texture2D _white;

        static readonly Color Panel = new Color(0.018f, 0.03f, 0.05f, 0.88f);
        static readonly Color Rule = new Color(0.5f, 0.7f, 0.95f, 0.18f);
        static readonly Color Ink = new Color(0.86f, 0.9f, 0.96f);
        static readonly Color Muted = new Color(0.55f, 0.62f, 0.72f);
        static readonly Color HullBar = new Color(0.9f, 0.74f, 0.44f);
        static readonly Color ArmyBar = new Color(0.7f, 0.85f, 0.55f);

        Campaign C => Director.Campaign;
        Intel Intel => C.Intel[Campaign.PlayerFaction];

        // --- Map input -------------------------------------------------------------------------

        void Update()
        {
            if (!CampaignDirector.Active || Director.InBattle || C == null) return;
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.dKey.wasPressedThisFrame && !kb.ctrlKey.isPressed) _showDiplomacy = !_showDiplomacy;
                if (kb.tabKey.wasPressedThisFrame) Map.ShowSupply = !Map.ShowSupply;
                if (kb.bKey.wasPressedThisFrame) _showDesigner = !_showDesigner;
                if (kb.escapeKey.wasPressedThisFrame) _showDiplomacy = _showDesigner = false;
            }
            var mouse = Mouse.current;
            if (mouse == null) return;
            Vector2 mp = mouse.position.ReadValue();
            var gui = new Vector2(mp.x, Screen.height - mp.y);
            foreach (var r in _panels)
                if (r.Contains(gui)) return;
            if (Director.Waiting) return;

            if (mouse.leftButton.wasPressedThisFrame)
            {
                int fleet = PickFleet(mp);
                if (fleet >= 0)
                {
                    Map.SelectedFleet = fleet;
                    Map.SelectedSystem = -1;
                }
                else
                {
                    Map.SelectedSystem = PickSystem(mp);
                    if (Map.SelectedSystem < 0) Map.SelectedFleet = -1;
                }
            }
            if (mouse.rightButton.wasPressedThisFrame && Map.SelectedFleet >= 0)
            {
                var f = C.FleetById(Map.SelectedFleet);
                int to = PickSystem(mp);
                if (f != null && f.Owner == Campaign.PlayerFaction && to >= 0 && C.MoveFleet(f.Id, to))
                    Director.Say($"{Director.Date}: {f.Name} ordered to {C.Galaxy.Systems[to].Name}.");
            }
        }

        int PickFleet(Vector2 mp)
        {
            int best = -1;
            float bestD = 30f;
            foreach (int id in Map.FleetIds)
            {
                Vector3 s = Camera.WorldToScreenPoint(Map.FleetPosition(id));
                if (s.z <= 0f) continue;
                float d = Vector2.Distance(s, mp);
                if (d < bestD)
                {
                    bestD = d;
                    best = id;
                }
            }
            return best;
        }

        int PickSystem(Vector2 mp)
        {
            int best = -1;
            float bestD = 44f;
            foreach (var s in C.Galaxy.Systems)
            {
                Vector3 p = Camera.WorldToScreenPoint(Map.SystemPosition(s.Id));
                if (p.z <= 0f) continue;
                float d = Vector2.Distance(p, mp);
                if (d < bestD)
                {
                    bestD = d;
                    best = s.Id;
                }
            }
            return best;
        }

        // --- Styles ----------------------------------------------------------------------------

        void Styles()
        {
            if (_white != null) return;
            _white = Texture2D.whiteTexture;
            var font = Art != null && Art.UiFont != null ? Art.UiFont : GUI.skin.font;
            var bold = Art != null && Art.UiFontBold != null ? Art.UiFontBold : font;
            _text = new GUIStyle(GUI.skin.label) { font = font, fontSize = 16, richText = true, wordWrap = false, normal = { textColor = Ink } };
            _label = new GUIStyle(_text) { fontSize = 15 };
            _small = new GUIStyle(_text) { fontSize = 14, normal = { textColor = Muted } };
            _tiny = new GUIStyle(_text) { fontSize = 12, normal = { textColor = Muted } };
            _title = new GUIStyle(_text) { font = bold, fontSize = 24 };
            _caps = new GUIStyle(_text) { font = bold, fontSize = 13, normal = { textColor = new Color(0.62f, 0.72f, 0.86f) } };
            _banner = new GUIStyle(_text) { font = bold, fontSize = 52, alignment = TextAnchor.MiddleCenter };
            _button = new GUIStyle(GUI.skin.button)
            {
                font = bold, fontSize = 14, alignment = TextAnchor.MiddleLeft, richText = true,
                padding = new RectOffset(12, 10, 4, 4),
                normal = { background = Tex(new Color(0.08f, 0.12f, 0.18f, 0.95f)), textColor = Ink },
                hover = { background = Tex(new Color(0.13f, 0.2f, 0.3f, 0.98f)), textColor = Color.white },
                active = { background = Tex(new Color(0.2f, 0.32f, 0.46f, 1f)), textColor = Color.white },
            };
            _buttonHot = new GUIStyle(_button)
            {
                normal = { background = Tex(new Color(0.16f, 0.28f, 0.42f, 0.98f)), textColor = Color.white },
            };
        }

        static Texture2D Tex(Color c)
        {
            var t = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            t.SetPixel(0, 0, c);
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

        Rect Box(float x, float y, float w, float h, Color accent, bool solid = false)
        {
            var r = new Rect(x, y, w, h);
            Fill(r, solid ? new Color(Panel.r, Panel.g, Panel.b, 0.985f) : Panel);
            Fill(new Rect(r.x, r.y, r.width, 2), accent);
            Fill(new Rect(r.x, r.yMax - 1, r.width, 1), Rule);
            _panels.Add(r);
            return r;
        }

        void Bar(Rect r, float v, Color c)
        {
            Fill(r, new Color(1f, 1f, 1f, 0.06f));
            Fill(new Rect(r.x, r.y, r.width * Mathf.Clamp01(v), r.height), c);
        }

        void Divider(Rect panel, float y) => Fill(new Rect(panel.x + 16, y, panel.width - 32, 1), Rule);

        bool Button(Rect r, string text, bool enabled = true, bool hot = false)
        {
            _panels.Add(r);
            GUI.enabled = enabled;
            bool clicked = GUI.Button(r, text, hot ? _buttonHot : _button);
            GUI.enabled = true;
            return clicked;
        }

        static string Caps(string s) => string.Join(" ", s.ToUpperInvariant().ToCharArray());

        // --- Drawing ---------------------------------------------------------------------------

        void OnGUI()
        {
            if (!CampaignDirector.Active || Director.InBattle || C == null) return;
            if (Event.current.type == EventType.Layout) _panels.Clear();
            Styles();

            bool modal = _showDiplomacy || _showDesigner || C.Pending != null || C.Council != null;
            MapLabels();
            TopBar();
            if (!modal)
            {
                FleetPanel();
                SystemPanel();
                OffersPanel();
                LandingForecast();
            }
            LogPanel();
            Hints();
            if (_showDiplomacy) DiplomacyWindow();
            if (_showDesigner) DesignerWindow();
            if (C.Pending != null) BattlePrompt();
            else if (C.Council != null) CouncilWindow();
            EndBanner();
        }

        void MapLabels()
        {
            var nameStyle = new GUIStyle(_text) { font = Art.UiFontBold, fontSize = 15, alignment = TextAnchor.UpperCenter };
            var sub = new GUIStyle(_tiny) { alignment = TextAnchor.UpperCenter };
            // Fewer words the further out the camera is: at galaxy scale, only what matters.
            float dist = Director.MapCamera.Distance;
            bool far = dist > 11000f, mid = dist > 7000f;
            foreach (var s in C.Galaxy.Systems)
            {
                Vector3 p = Camera.WorldToScreenPoint(Map.SystemPosition(s.Id));
                if (p.z <= 0f) continue;
                bool seen = Intel.SystemVisible[s.Id];
                int owner = Intel.SeenHour[s.Id] >= 0 ? Intel.OwnerSeen[s.Id] : -1;
                bool key = s.Capital || s.OldCapital || owner == Campaign.PlayerFaction || s.Id == Map.SelectedSystem;
                if (far && !key) continue;
                Color fc = FactionStyle.Of(owner);
                var r = new Rect(p.x - 110, Screen.height - p.y + 30, 220, 22);
                nameStyle.normal.textColor = seen ? new Color(0.93f, 0.95f, 1f) : new Color(0.6f, 0.62f, 0.7f);
                GUI.Label(r, s.Name.ToUpperInvariant(), nameStyle);
                string tier = new string('•', s.Tier);
                if (mid && !key && !seen) continue;
                string info = owner >= 0 ? C.Factions[owner].Name
                    : s.OldCapital ? "the old capital" : s.Derelict == Derelict.Ruin ? "ruin" : s.Derelict == Derelict.Holdout ? "holdout" : "unclaimed";
                if (!seen) info = Intel.SeenHour[s.Id] < 0 ? "unexplored" : $"{info} · seen {(C.Hours - Intel.SeenHour[s.Id]) / 24:0}d ago";
                sub.normal.textColor = seen ? fc : new Color(0.5f, 0.52f, 0.6f);
                GUI.Label(new Rect(r.x, r.y + 19, r.width, 18), $"{tier}  {info}{(s.Besieger >= 0 && seen ? "  ·  ground war" : "")}", sub);
            }

            var fleetStyle = new GUIStyle(_tiny) { alignment = TextAnchor.MiddleLeft };
            foreach (int id in Map.FleetIds)
            {
                var f = C.FleetById(id);
                if (f == null) continue;
                Vector3 p = Camera.WorldToScreenPoint(Map.FleetPosition(id));
                if (p.z <= 0f) continue;
                fleetStyle.normal.textColor = FactionStyle.Of(f.Owner);
                int troops = C.CountIn(f);
                GUI.Label(new Rect(p.x + 18, Screen.height - p.y - 9, 220, 18),
                    $"{f.Name} · {f.Ships.Count}{(troops > 0 ? $" · {troops} army" : "")}", fleetStyle);
            }
            foreach (var kv in Map.Ghosts)
            {
                if (!Intel.Fleets.TryGetValue(kv.Key, out var sight)) continue;
                Vector3 p = Camera.WorldToScreenPoint(kv.Value.position);
                if (p.z <= 0f) continue;
                fleetStyle.normal.textColor = FactionStyle.Of(sight.Owner) * 0.8f;
                GUI.Label(new Rect(p.x + 14, Screen.height - p.y - 9, 240, 18),
                    $"? {C.Factions[sight.Owner].Name} · {sight.Ships} ships · {(C.Hours - sight.Hour) / 24:0.#}d ago", fleetStyle);
            }
        }

        void TopBar()
        {
            var r = Box(0, 0, Screen.width, 52, FactionStyle.Of(Campaign.PlayerFaction));
            var fac = C.Factions[Campaign.PlayerFaction];
            var t = C.Totals(Campaign.PlayerFaction);
            float net = t.Credits - C.Upkeep(Campaign.PlayerFaction);
            Fill(new Rect(18, 16, 20, 20), FactionStyle.Of(Campaign.PlayerFaction));
            Fill(new Rect(22, 20, 12, 12), FactionStyle.Accent[0]);
            bool wide = Screen.width >= 1500;
            GUI.Label(new Rect(48, 13, 200, 28), wide ? Caps(fac.Name) : fac.Name.ToUpperInvariant(), new GUIStyle(_caps) { fontSize = 16, normal = { textColor = Ink } });
            string time = Director.Waiting ? "HELD" : Director.Paused ? "PAUSED" : $"×{Director.Speed:0.#}";
            float next = Campaign.CouncilIntervalDays * 24f - (float)(C.Hours % (Campaign.CouncilIntervalDays * 24f));
            string act = C.Act switch { 1 => "act I: rupture", 2 => "act II: blocs", _ => "act III: total war" };
            string throne = C.Act >= 3 && C.ThroneHolder >= 0 ? $"     <color=#{FactionStyle.Hex(C.ThroneHolder)}>{C.Factions[C.ThroneHolder].Name} holds the throne {C.ThroneHeldDays:0}/{Campaign.ThroneDays:0}d</color>" : "";
            GUI.Label(new Rect(wide ? 260 : 210, 13, Screen.width - (wide ? 700 : 640), 28),
                $"<b>{Director.Date}</b>  <color=#8aa0b8>{time} · {act}</color>     " +
                $"<b>{fac.Credits:0}</b> <color=#8aa0b8>cr ({(net >= 0 ? "+" : "")}{net:0})</color>     " +
                $"<b>{t.Industry:0}</b> <color=#8aa0b8>ind</color>     " +
                $"<b>{fac.Influence:0}</b> <color=#8aa0b8>inf (+{t.Influence:0.#})</color>" +
                (wide ? $"     <color=#8aa0b8>council in {next / 24f:0}d</color>" : "") + throne +
                (fac.EmbargoHours > 0f ? $"     <color=#ff8a70>EMBARGOED {fac.EmbargoHours / 24f:0}d</color>" : ""),
                _text);
            float bx = Screen.width - 16;
            bx -= 90; if (Button(new Rect(bx, 10, 82, 32), "SAVE", C.CanSave)) Director.Say(Director.TrySave(CampaignDirector.QuickSave) ? $"{Director.Date}: saved." : "Could not save.");
            bx -= 110; if (Button(new Rect(bx, 10, 102, 32), "SUPPLY", true, Map.ShowSupply)) Map.ShowSupply = !Map.ShowSupply;
            bx -= 132; if (Button(new Rect(bx, 10, 124, 32), "DIPLOMACY", true, _showDiplomacy)) { _showDiplomacy = !_showDiplomacy; _showDesigner = false; }
            bx -= 126; if (Button(new Rect(bx, 10, 118, 32), "DESIGNS", true, _showDesigner)) { _showDesigner = !_showDesigner; _showDiplomacy = false; }
        }

        // --- Fleet ---------------------------------------------------------------------------

        void FleetPanel()
        {
            var f = C.FleetById(Map.SelectedFleet);
            if (f == null || !f.Alive || !Intel.VisibleFleets.Contains(f.Id))
            {
                Map.SelectedFleet = -1;
                return;
            }
            bool ours = f.Owner == Campaign.PlayerFaction;
            var armies = C.ArmiesIn(f).ToList();
            int shown = Mathf.Min(f.Ships.Count, 9);
            float h = 150 + shown * 30 + (ours ? 60 + armies.Count * 40 + 230 : 0);
            var r = Box(16, 66, 380, h, FactionStyle.Of(f.Owner));
            float y = r.y + 12;
            GUI.Label(new Rect(r.x + 16, y, 350, 20), Caps(C.Factions[f.Owner].Name), _caps);
            y += 18;
            GUI.Label(new Rect(r.x + 16, y, 350, 30), f.Name, _title);
            y += 32;
            string where = f.Held ? "engaged in the lane"
                : f.InTransit ? $"to {C.Galaxy.Systems[f.Destination].Name}, {Eta(f):0.#} days"
                : $"at {C.Galaxy.Systems[f.System].Name}";
            GUI.Label(new Rect(r.x + 16, y, 350, 20), where, _small);
            y += 20;
            if (ours)
                GUI.Label(new Rect(r.x + 16, y, 350, 20),
                    $"strength {f.Strength:0} · {f.LyPerDay:0.#} ly/day · {(C.InSupply(f) ? "<color=#7fd9ff>in supply</color>" : "<color=#ff8a70>out of supply: no repairs</color>")}", _small);
            else
                GUI.Label(new Rect(r.x + 16, y, 350, 20), $"estimated strength {f.Strength:0}", _small);
            y += 22;
            if (ours)
            {
                if (f.Admiral != null)
                    GUI.Label(new Rect(r.x + 16, y, 350, 20), $"admiral {f.Admiral.Name} · tactics {f.Admiral.Tactics} · nerve {f.Admiral.Nerve}", _small);
                else if (Button(new Rect(r.x + 16, y - 2, 348, 24), $"APPOINT AN ADMIRAL   <color=#8aa0b8>{Campaign.AdmiralCost} inf</color>", C.CanAppointAdmiral(f.Id)))
                {
                    var adm = C.AppointAdmiral(f.Id);
                    if (adm != null) Director.Say($"{Director.Date}: {adm.Name} takes command of {f.Name}.");
                }
                y += 26;
            }
            y += 6;
            Divider(r, y - 4);

            for (int i = 0; i < shown; i++)
            {
                var s = f.Ships[i];
                GUI.Label(new Rect(r.x + 16, y, 220, 20), $"{s.Name}  <color=#8a9bb0>{s.Def.Id}</color>", _label);
                if (ours) GUI.Label(new Rect(r.x + 230, y, 134, 20), $"{s.MountsAlive}/{s.MountAlive.Length} guns", new GUIStyle(_tiny) { alignment = TextAnchor.UpperRight });
                if (ours) Bar(new Rect(r.x + 16, y + 21, 348, 3), s.HullFraction, HullBar);
                y += 30;
            }
            if (f.Ships.Count > shown) GUI.Label(new Rect(r.x + 16, y - 6, 300, 18), $"and {f.Ships.Count - shown} more", _tiny);
            if (!ours) return;

            Divider(r, y + 2);
            y += 10;
            GUI.Label(new Rect(r.x + 16, y, 350, 20), Caps($"Armies aboard {armies.Count} / {f.TroopCapacity}"), _caps);
            y += 22;
            foreach (var a in armies)
            {
                GUI.Label(new Rect(r.x + 16, y, 350, 20), $"{a.Name}", _label);
                GUI.Label(new Rect(r.x + 16, y + 17, 350, 18), $"{a.Commander.Name}, {a.Commander.TraitText}", _tiny);
                Bar(new Rect(r.x + 250, y + 8, 114, 4), a.Strength / Army.MaxStrength, ArmyBar);
                y += 40;
            }

            if (f.InTransit) return;
            var here = C.Galaxy.Systems[f.System];
            y += 6;
            if (armies.Count > 0 && here.Owner == f.Owner)
            {
                GUI.Label(new Rect(r.x + 16, y, 350, 20), "armies land by themselves where the fleet stops", _tiny);
                y += 20;
            }
            y = ExpeditionButtons(r, y, f, here);
            if (here.Owner == f.Owner)
            {
                foreach (var a in C.ArmiesAt(here.Id).ToList())
                {
                    if (a.Owner != f.Owner || armies.Count >= f.TroopCapacity) break;
                    if (Button(new Rect(r.x + 16, y, 348, 28), $"Embark the {a.Name}"))
                        C.Embark(f.Id, a.Id);
                    y += 32;
                    break;
                }
                if (armies.Count > 0 && Button(new Rect(r.x + 16, y, 348, 28), "Put the armies ashore here"))
                    foreach (var a in armies) C.Disembark(a.Id);
                if (armies.Count > 0) y += 32;
            }
            foreach (var o in C.Fleets)
            {
                if (o == f || !o.Alive || o.Owner != f.Owner || o.InTransit || o.System != f.System) continue;
                if (Button(new Rect(r.x + 16, y, 348, 28), $"Merge {o.Name} into this fleet"))
                {
                    C.Merge(f.Id, o.Id);
                    Director.Say($"{Director.Date}: {o.Name} joined {f.Name}.");
                }
                y += 32;
                break;
            }
            if (f.Ships.Count > 1 && Button(new Rect(r.x + 16, y, 348, 28), "Detach the most damaged ship"))
            {
                int worst = 0;
                for (int i = 1; i < f.Ships.Count; i++)
                    if (f.Ships[i].HullFraction < f.Ships[worst].HullFraction) worst = i;
                var nf = C.Split(f.Id, new[] { worst }, $"{f.Ships[worst].Name} (detached)");
                if (nf != null) Director.Say($"{Director.Date}: {nf.Name} detached.");
            }
        }

        float Eta(Fleet f)
        {
            float ly = C.Galaxy.LaneLength(f.System, f.Next) - f.Progress;
            int prev = f.Next;
            foreach (int s in f.Route)
            {
                ly += C.Galaxy.LaneLength(prev, s);
                prev = s;
            }
            return ly / Mathf.Max(0.1f, f.LyPerDay);
        }

        // --- Log and hints ---------------------------------------------------------------------

        void LogPanel()
        {
            int lines = Mathf.Min(9, Director.Log.Count);
            if (lines == 0) return;
            float w = 560;
            var r = Box(Screen.width - w - 16, Screen.height - 40 - lines * 21 - 20, w, lines * 21 + 20, Rule);
            for (int i = 0; i < lines; i++)
            {
                var style = i == 0 ? _label : new GUIStyle(_small) { fontSize = 14 };
                GUI.Label(new Rect(r.x + 16, r.y + 10 + i * 21, w - 28, 21), Director.Log[i], style);
            }
        }

        void Hints()
        {
            GUI.Label(new Rect(18, Screen.height - 30, Screen.width - 620, 22),
                "left-click select · right-click a system: send the fleet · WASD pan · Q/E turn · wheel zoom · Space pause · +/- speed · D diplomacy · B designs · Tab supply · F5 save · F9 load",
                _tiny);
        }

        // --- Diplomacy -------------------------------------------------------------------------

        void DiplomacyWindow()
        {
            float w = 900, rowH = 92;
            int rows = C.Factions.Count - 1;
            float h = Mathf.Min(120 + rows * rowH + 110, Screen.height - 150);
            var r = Box(Screen.width * 0.5f - w * 0.5f, 70, w, h, FactionStyle.Of(Campaign.PlayerFaction), solid: true);
            float y = r.y + 14;
            GUI.Label(new Rect(r.x + 20, y, 400, 30), "Diplomacy", _title);
            var me = C.Factions[Campaign.PlayerFaction];
            GUI.Label(new Rect(r.x + 20, y + 30, w - 40, 20),
                $"{me.Influence:0} influence to spend · council seats: ours {C.Seats(Campaign.PlayerFaction)}" +
                "  ·  breaking a treaty costs trust with everyone", _small);
            if (Button(new Rect(r.xMax - 120, y, 100, 28), "CLOSE")) _showDiplomacy = false;
            y += 70;

            // The powers scroll; the council stays at the bottom.
            var list = new Rect(r.x, y - 8, w, r.yMax - y - 100);
            float top = y - 8;
            _dipScroll = GUI.BeginScrollView(list, _dipScroll, new Rect(r.x, top, w - 20, rows * rowH + 8), false, false);
            foreach (var fac in C.Factions.OrderBy(f => f.Eliminated).ThenBy(f => !f.Major).ThenBy(f => f.Id))
            {
                if (fac.Id == Campaign.PlayerFaction) continue;
                Divider(r, y - 6);
                Color fc = FactionStyle.Of(fac.Id);
                Fill(new Rect(r.x + 20, y + 6, 6, rowH - 24), fc);
                var stance = C.Stance(Campaign.PlayerFaction, fac.Id);
                string stanceText = stance switch
                {
                    DiplomaticStance.War => "<color=#ff7a66>AT WAR</color>",
                    DiplomaticStance.Alliance => "<color=#7fe0a0>ALLIED</color>",
                    DiplomaticStance.NonAggression => "<color=#9fc8ff>NON-AGGRESSION PACT</color>",
                    _ => "<color=#b8c2d0>AT PEACE</color>",
                };
                GUI.Label(new Rect(r.x + 38, y, 330, 26), $"{fac.Name}{(fac.Eliminated ? " (gone)" : "")}", new GUIStyle(_text) { font = Art.UiFontBold, fontSize = 19, normal = { textColor = fc } });
                GUI.Label(new Rect(r.x + 38, y + 24, 330, 20), $"{(fac.Major ? "major power" : "minor power")} · {stanceText} · {C.DaysAtStance(Campaign.PlayerFaction, fac.Id):0} days", _small);
                var wars = C.Factions.Where(o => o.Id != fac.Id && !o.Eliminated && C.Stance(fac.Id, o.Id) == DiplomaticStance.War).Select(o => o.Name).ToList();
                GUI.Label(new Rect(r.x + 38, y + 44, 330, 20), wars.Count > 0 ? $"at war with {string.Join(", ", wars)}" : "at war with no one", _tiny);

                float trust = C.Trust(Campaign.PlayerFaction, fac.Id);
                var tb = new Rect(r.x + 380, y + 12, 160, 6);
                Fill(tb, new Color(1f, 1f, 1f, 0.06f));
                Fill(new Rect(tb.center.x, tb.y - 3, 1, 12), Rule);
                float half = tb.width * 0.5f * Mathf.Abs(trust) / 100f;
                Fill(trust >= 0 ? new Rect(tb.center.x, tb.y, half, tb.height) : new Rect(tb.center.x - half, tb.y, half, tb.height),
                    trust >= 0 ? new Color(0.5f, 0.9f, 0.6f) : new Color(1f, 0.45f, 0.35f));
                GUI.Label(new Rect(tb.x, y + 22, 160, 18), $"trust {trust:+0;-0;0}", new GUIStyle(_tiny) { alignment = TextAnchor.UpperCenter });
                GUI.Label(new Rect(tb.x, y + 40, 160, 18), $"{C.Seats(fac.Id)} council seats", new GUIStyle(_tiny) { alignment = TextAnchor.UpperCenter });

                if (fac.Eliminated) { y += rowH; continue; }
                float bx = r.x + 560, bw = 280;
                float by = y;
                void Offer(DiplomaticStance treaty, string label)
                {
                    if (!C.CanPropose(Campaign.PlayerFaction, fac.Id, treaty) && Campaign.Cost(treaty) <= me.Influence) return;
                    bool can = C.CanPropose(Campaign.PlayerFaction, fac.Id, treaty);
                    bool likely = C.WillAccept(fac.Id, Campaign.PlayerFaction, treaty);
                    if (Button(new Rect(bx, by, bw, 26), $"{label}  <color=#8aa0b8>{Campaign.Cost(treaty)} inf · {(likely ? "likely" : "unlikely")}</color>", can))
                    {
                        bool ok = C.Propose(Campaign.PlayerFaction, fac.Id, treaty);
                        Director.Say(ok ? $"{Director.Date}: the {Director.Tag(fac.Id)} accepts." : $"{Director.Date}: the {Director.Tag(fac.Id)} refuses.");
                        Director.Flush();
                    }
                    by += 29;
                }
                if (stance == DiplomaticStance.War) Offer(DiplomaticStance.Peace, "OFFER PEACE");
                if (stance == DiplomaticStance.Peace) Offer(DiplomaticStance.NonAggression, "PROPOSE NON-AGGRESSION");
                if (stance == DiplomaticStance.Peace || stance == DiplomaticStance.NonAggression) Offer(DiplomaticStance.Alliance, "PROPOSE AN ALLIANCE");
                if (stance != DiplomaticStance.War && by < y + rowH - 28 && Button(new Rect(bx, by, 134, 26), "GIFT 150 cr", me.Credits >= 150f))
                {
                    C.Gift(Campaign.PlayerFaction, fac.Id, 150f);
                    Director.Say($"{Director.Date}: a gift to the {Director.Tag(fac.Id)}.");
                }
                if (stance != DiplomaticStance.War && Button(new Rect(bx + 146, by, 134, 26), "<color=#ff8a70>DECLARE WAR</color>"))
                {
                    C.DeclareWar(Campaign.PlayerFaction, fac.Id);
                    Director.Flush();
                }
                y += rowH;
            }

            GUI.EndScrollView();
            y = r.yMax - 96;
            Divider(r, y - 6);
            GUI.Label(new Rect(r.x + 20, y + 4, 500, 22), Caps("The council"), _caps);
            var last = C.LastCouncil;
            GUI.Label(new Rect(r.x + 20, y + 26, w - 40, 20),
                last == null ? "has not yet sat" : $"last session: {C.Describe(last)} — {(last.Passed ? "passed" : "rejected")} {last.YesSeats} to {last.NoSeats}", _small);
            GUI.Label(new Rect(r.x + 20, y + 50, 300, 20), $"table an embargo ({Campaign.TableCost} influence):", _small);
            float bx2 = r.x + 280;
            foreach (var fac in C.Factions)
            {
                if (fac.Id == Campaign.PlayerFaction || fac.Eliminated || !fac.Major) continue;
                if (Button(new Rect(bx2, y + 46, 140, 26), $"<color=#{FactionStyle.Hex(fac.Id)}>{fac.Name}</color>", C.CanTable(Campaign.PlayerFaction)))
                {
                    C.TableEmbargo(Campaign.PlayerFaction, fac.Id);
                    Director.Say($"{Director.Date}: we table an embargo on the {Director.Tag(fac.Id)} for the next session.");
                }
                bx2 += 146;
            }
        }

        // --- Modals ----------------------------------------------------------------------------

        Rect Modal(float w, float h, Color accent)
        {
            Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.35f));
            _panels.Add(new Rect(0, 0, Screen.width, Screen.height));
            return Box(Screen.width * 0.5f - w * 0.5f, Screen.height * 0.5f - h * 0.5f, w, h, accent, solid: true);
        }

        void BattlePrompt()
        {
            var e = C.Pending;
            var r = Modal(560, 300, new Color(1f, 0.4f, 0.3f));
            float y = r.y + 18;
            GUI.Label(new Rect(r.x, y, r.width, 22), Caps(e.InLane ? "Contact in the lanes" : "Contact"), new GUIStyle(_caps) { alignment = TextAnchor.MiddleCenter });
            GUI.Label(new Rect(r.x, y + 22, r.width, 36), Director.Where(e), new GUIStyle(_title) { alignment = TextAnchor.MiddleCenter, fontSize = 28 });
            y += 76;
            GUI.Label(new Rect(r.x + 28, y, 504, 22), $"<b>Ours:</b> {Describe(e, 0)}", _label);
            GUI.Label(new Rect(r.x + 28, y + 24, 504, 22), $"<b>Theirs:</b> {Describe(e, 1)}", _label);
            float ours = Director.Forecast(0), theirs = Director.Forecast(1);
            string odds = ours > theirs * 1.5f ? "We have the weight." : theirs > ours * 1.5f ? "We are outmatched." : "It could go either way.";
            GUI.Label(new Rect(r.x + 28, y + 52, 504, 22), odds, _small);
            if (Button(new Rect(r.x + 28, r.yMax - 88, 504, 34), "TAKE COMMAND", true, true)) Director.Attend();
            if (Button(new Rect(r.x + 28, r.yMax - 48, 504, 34), "LEAVE IT TO THE FLEET")) Director.LeaveToFleet();
        }

        string Describe(Engagement e, int side)
        {
            var counts = new Dictionary<string, int>();
            var factions = new List<string>();
            foreach (int id in e.Fleets)
            {
                var f = C.FleetById(id);
                if (f == null || e.SideOf(f.Owner) != side) continue;
                string tag = Director.Tag(f.Owner);
                if (!factions.Contains(tag)) factions.Add(tag);
                foreach (var s in f.Ships)
                {
                    counts.TryGetValue(s.Def.Id, out int n);
                    counts[s.Def.Id] = n + 1;
                }
            }
            var parts = counts.Select(kv => $"{kv.Value} {kv.Key}{(kv.Value > 1 ? "s" : "")}");
            return $"{string.Join(" and ", factions)}: {string.Join(", ", parts)}";
        }

        void CouncilWindow()
        {
            var s = C.Council;
            int voters = s.Votes.Count;
            var r = Modal(820, 200 + (voters + 1) / 2 * 24 + 40, new Color(0.85f, 0.75f, 0.5f));
            float y = r.y + 18;
            GUI.Label(new Rect(r.x, y, r.width, 22), Caps("The council sits"), new GUIStyle(_caps) { alignment = TextAnchor.MiddleCenter });
            GUI.Label(new Rect(r.x + 30, y + 26, r.width - 60, 60), C.Describe(s), new GUIStyle(_title) { fontSize = 22, wordWrap = true, alignment = TextAnchor.UpperCenter });
            y += 98;
            int k = 0;
            foreach (var kv in s.Votes)
            {
                string v = kv.Value switch { Vote.Yes => "<color=#9fe39a>for</color>", Vote.No => "<color=#ff8a70>against</color>", _ => "abstains" };
                GUI.Label(new Rect(r.x + 40 + (k % 2) * 380, y + (k / 2) * 24, 370, 22), $"{Director.Tag(kv.Key)} · {C.Seats(kv.Key)} seats · {v}", _label);
                k++;
            }
            y += (k + 1) / 2 * 24;
            GUI.Label(new Rect(r.x + 40, y + 4, 540, 22), $"our seats: {C.Seats(Campaign.PlayerFaction)}", _small);
            float bw = 176;
            if (Button(new Rect(r.x + 30, r.yMax - 50, bw, 34), "VOTE FOR", true, true)) { C.CastPlayerVote(Vote.Yes); Director.Flush(); }
            if (Button(new Rect(r.x + 30 + bw + 12, r.yMax - 50, bw, 34), "VOTE AGAINST")) { C.CastPlayerVote(Vote.No); Director.Flush(); }
            if (Button(new Rect(r.x + 30 + (bw + 12) * 2, r.yMax - 50, bw, 34), "ABSTAIN")) { C.CastPlayerVote(Vote.Abstain); Director.Flush(); }
        }

        void EndBanner()
        {
            if (C.State == CampaignState.Ongoing) return;
            bool won = C.State == CampaignState.Victory;
            _banner.normal.textColor = won ? new Color(0.6f, 0.95f, 1f) : new Color(1f, 0.45f, 0.4f);
            GUI.Label(new Rect(0, Screen.height * 0.25f, Screen.width, 70), won ? "THE WAR IS WON" : "THE WAR IS LOST", _banner);
        }
    }
}
