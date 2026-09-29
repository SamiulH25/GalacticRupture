using System.Collections.Generic;
using GalacticRupture.Sim;
using GalacticRupture.Sim.Strategic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GalacticRupture.Game
{
    // The strategic screen: date and treasury, system and fleet panels, the yard, the log,
    // the battle prompt. Also the map's mouse: left-click selects a fleet or system,
    // right-click a system sends the selected fleet there.
    public sealed class CampaignHud : MonoBehaviour
    {
        public CampaignDirector Director;
        public MapView Map;
        public Camera Camera;

        readonly List<Rect> _panels = new List<Rect>();
        GUIStyle _label, _small, _title, _button, _banner;
        Texture2D _white;

        static readonly Color Panel = new Color(0.03f, 0.05f, 0.08f, 0.8f);
        static readonly Color Edge = new Color(0.45f, 0.85f, 1f, 0.5f);
        static readonly Color HullColor = new Color(0.85f, 0.72f, 0.45f);

        Campaign C => Director.Campaign;

        void Update()
        {
            if (!CampaignDirector.Active || Director.InBattle || C == null) return;
            var mouse = Mouse.current;
            if (mouse == null) return;
            Vector2 mp = mouse.position.ReadValue();
            var gui = new Vector2(mp.x, Screen.height - mp.y);
            foreach (var r in _panels)
                if (r.Contains(gui)) return;
            if (C.Pending != null) return;

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
            float bestD = 26f;
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
            float bestD = 40f;
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

        // --- Drawing ---------------------------------------------------------------------------

        void Styles()
        {
            if (_white != null) return;
            _white = Texture2D.whiteTexture;
            _label = new GUIStyle(GUI.skin.label) { fontSize = 14, richText = true, normal = { textColor = new Color(0.85f, 0.9f, 0.96f) } };
            _small = new GUIStyle(_label) { fontSize = 12, normal = { textColor = new Color(0.62f, 0.7f, 0.8f) } };
            _title = new GUIStyle(_label) { fontSize = 18, fontStyle = FontStyle.Bold };
            _banner = new GUIStyle(_label) { fontSize = 40, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _button = new GUIStyle(GUI.skin.button) { fontSize = 13, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(10, 8, 4, 4) };
        }

        void Fill(Rect r, Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, _white);
            GUI.color = old;
        }

        Rect Box(float x, float y, float w, float h)
        {
            var r = new Rect(x, y, w, h);
            Fill(r, Panel);
            Fill(new Rect(r.x, r.y, 2, r.height), Edge);
            _panels.Add(r);
            return r;
        }

        void Bar(Rect r, float v, Color c)
        {
            Fill(r, new Color(0f, 0f, 0f, 0.5f));
            Fill(new Rect(r.x, r.y, r.width * Mathf.Clamp01(v), r.height), c);
        }

        void OnGUI()
        {
            if (!CampaignDirector.Active || Director.InBattle || C == null) return;
            if (Event.current.type == EventType.Layout) _panels.Clear();
            Styles();

            SystemLabels();
            TopBar();
            FleetPanel();
            SystemPanel();
            LogPanel();
            BattlePrompt();
            EndBanner();
        }

        void SystemLabels()
        {
            foreach (var s in C.Galaxy.Systems)
            {
                Vector3 p = Camera.WorldToScreenPoint(Map.SystemPosition(s.Id));
                if (p.z <= 0f) continue;
                string pips = new string('•', s.Tier);
                Color fc = MapView.FactionColour(s.Owner);
                string hex = ColorUtility.ToHtmlStringRGB(fc);
                string text = $"<b>{s.Name}</b>  <color=#{hex}>{pips}</color>{(s.Capital ? "  capital" : s.Shipyard ? "  yard" : "")}";
                var r = new Rect(p.x - 100, Screen.height - p.y + 26, 200, 20);
                GUI.Label(r, text, new GUIStyle(_small) { alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(0.85f, 0.88f, 0.95f, 0.9f) } });
            }
        }

        void TopBar()
        {
            var r = Box(0, 0, Screen.width, 38);
            var fac = C.Factions[Campaign.PlayerFaction];
            float income = 0f;
            foreach (var s in C.Galaxy.Systems)
                if (s.Owner == Campaign.PlayerFaction && s.Unrest <= 0f) income += Campaign.IncomePerDay(s);
            int owned = 0;
            foreach (var s in C.Galaxy.Systems) if (s.Owner == Campaign.PlayerFaction) owned++;
            string time = Director.Paused ? "PAUSED" : $"×{Director.Speed:0.#}";
            GUI.Label(new Rect(20, 9, 1400, 22),
                $"<b>{Director.Date}</b>     {time}     <b>{fac.Credits:0}</b> credits  (+{income:0}/day)     {owned} systems",
                _label);
            GUI.Label(new Rect(Screen.width - 620, 11, 600, 20),
                "left-click select · right-click system: move fleet · WASD / Q E / wheel / middle-drag · Space pause · +/- speed",
                new GUIStyle(_small) { alignment = TextAnchor.UpperRight });
        }

        void FleetPanel()
        {
            var f = C.FleetById(Map.SelectedFleet);
            if (f == null || !f.Alive) return;
            float h = 116 + f.Ships.Count * 34;
            var r = Box(16, 52, 330, h);
            float y = r.y + 10;
            GUI.Label(new Rect(r.x + 14, y, 300, 24), f.Name, _title);
            y += 26;
            string where = f.InTransit
                ? $"en route to {C.Galaxy.Systems[f.Destination].Name}"
                : $"at {C.Galaxy.Systems[f.System].Name}";
            bool supplied = C.InSupply(f);
            string owner = f.Owner == Campaign.PlayerFaction ? "Continuance" : "Remnant";
            GUI.Label(new Rect(r.x + 14, y, 300, 20), $"{owner} · {where}", _small);
            y += 18;
            GUI.Label(new Rect(r.x + 14, y, 300, 20),
                $"strength {f.Strength:0} · {f.LyPerDay:0.#} ly/day · {(supplied ? "in supply" : "<color=#ff7a60>OUT OF SUPPLY</color>")}", _small);
            y += 26;
            foreach (var s in f.Ships)
            {
                GUI.Label(new Rect(r.x + 14, y, 200, 18), $"{s.Name}  <color=#8a9bb0>{s.Def.Id}</color>", _label);
                GUI.Label(new Rect(r.x + 200, y, 116, 18), $"{s.MountsAlive}/{s.MountAlive.Length} guns", new GUIStyle(_small) { alignment = TextAnchor.UpperRight });
                Bar(new Rect(r.x + 14, y + 19, 302, 4), s.HullFraction, HullColor);
                y += 34;
            }

            if (f.Owner != Campaign.PlayerFaction || f.InTransit) return;
            // Merge with another of our fleets in the same system.
            foreach (var o in C.Fleets)
            {
                if (o == f || !o.Alive || o.Owner != f.Owner || o.InTransit || o.System != f.System) continue;
                if (GUI.Button(new Rect(r.x + 14, y, 302, 24), $"Merge {o.Name} into this fleet", _button))
                {
                    C.Merge(f.Id, o.Id);
                    Director.Say($"{Director.Date}: {o.Name} joined {f.Name}.");
                }
                _panels.Add(new Rect(r.x + 14, y, 302, 24));
                y += 28;
                break;
            }
            if (f.Ships.Count > 1 && GUI.Button(new Rect(r.x + 14, y, 302, 24), "Detach the most damaged ship", _button))
            {
                int worst = 0;
                for (int i = 1; i < f.Ships.Count; i++)
                    if (f.Ships[i].HullFraction < f.Ships[worst].HullFraction) worst = i;
                var nf = C.Split(f.Id, new[] { worst }, $"{f.Ships[worst].Name} (detached)");
                if (nf != null) Director.Say($"{Director.Date}: {nf.Name} detached.");
            }
            _panels.Add(new Rect(r.x + 14, y, 302, 24));
        }

        void SystemPanel()
        {
            if (Map.SelectedSystem < 0) return;
            var s = C.Galaxy.Systems[Map.SelectedSystem];
            bool ours = s.Owner == Campaign.PlayerFaction;
            C.Yards.TryGetValue(s.Id, out var queue);
            float h = 132 + (ours && s.Shipyard ? 34 * Hulls.All.Length + 24 + (queue?.Count ?? 0) * 20 : 0);
            var r = Box(Screen.width - 346, 52, 330, h);
            float y = r.y + 10;
            GUI.Label(new Rect(r.x + 14, y, 300, 24), s.Name + (s.Capital ? "  (capital)" : ""), _title);
            y += 26;
            string owner = s.Owner < 0 ? "unclaimed" : s.Owner == Campaign.PlayerFaction ? "Continuance" : "Remnant";
            GUI.Label(new Rect(r.x + 14, y, 300, 20), $"{owner} · tier {s.Tier} · {Campaign.IncomePerDay(s):0} credits/day", _small);
            y += 18;
            GUI.Label(new Rect(r.x + 14, y, 300, 20),
                $"{(s.Shipyard ? "shipyard" : "no shipyard")}{(s.Unrest > 0f ? $" · unrest {s.Unrest / 24f:0.0} days" : "")}", _small);
            y += 22;
            if (s.Besieger >= 0)
            {
                GUI.Label(new Rect(r.x + 14, y, 300, 18), s.Besieger == Campaign.PlayerFaction ? "we are taking it" : "under siege", _small);
                Bar(new Rect(r.x + 14, y + 18, 302, 5), s.Siege, MapView.FactionColour(s.Besieger));
                y += 30;
            }
            float own = C.Strength(Campaign.PlayerFaction, s.Id), foe = C.Strength(Campaign.EnemyFaction, s.Id);
            GUI.Label(new Rect(r.x + 14, y, 300, 20), $"our ships here {own:0} · theirs {foe:0}", _small);
            y += 26;

            if (!ours || !s.Shipyard) return;
            if (queue != null && queue.Count > 0)
            {
                foreach (var item in queue)
                {
                    GUI.Label(new Rect(r.x + 14, y, 300, 18), $"building {item.Def.Id} · {item.HoursLeft / 24f:0.0} days", _small);
                    y += 20;
                }
            }
            GUI.Label(new Rect(r.x + 14, y, 300, 20), "Order from the yard:", _label);
            y += 22;
            foreach (var hull in Hulls.All)
            {
                bool can = C.CanBuild(Campaign.PlayerFaction, s.Id, hull);
                GUI.enabled = can;
                var b = new Rect(r.x + 14, y, 302, 28);
                if (GUI.Button(b, $"{hull.Id}   <color=#c9a560>{hull.Cost} cr</color>   {hull.BuildDays:0.#} days", new GUIStyle(_button) { richText = true }))
                {
                    if (C.QueueBuild(Campaign.PlayerFaction, s.Id, hull))
                        Director.Say($"{Director.Date}: {hull.Id} laid down at {s.Name}.");
                }
                GUI.enabled = true;
                _panels.Add(b);
                y += 34;
            }
        }

        void LogPanel()
        {
            int lines = Mathf.Min(8, Director.Log.Count);
            if (lines == 0) return;
            var r = Box(Screen.width - 486, Screen.height - 24 - lines * 20 - 16, 470, lines * 20 + 16);
            for (int i = 0; i < lines; i++)
            {
                var style = i == 0 ? _label : _small;
                GUI.Label(new Rect(r.x + 14, r.y + 8 + i * 20, 450, 20), Director.Log[i], style);
            }
        }

        void BattlePrompt()
        {
            var e = C.Pending;
            if (e == null) return;
            var s = C.Galaxy.Systems[e.System];
            var r = Box(Screen.width * 0.5f - 230, Screen.height * 0.5f - 130, 460, 250);
            GUI.Label(new Rect(r.x, r.y + 16, r.width, 30), $"BATTLE AT {s.Name.ToUpperInvariant()}",
                new GUIStyle(_title) { alignment = TextAnchor.MiddleCenter, fontSize = 22 });
            float ours = Director.Forecast(Campaign.PlayerFaction), theirs = Director.Forecast(Campaign.EnemyFaction);
            GUI.Label(new Rect(r.x + 24, r.y + 60, 412, 22), $"Our fleet: {Describe(e, Campaign.PlayerFaction)}", _label);
            GUI.Label(new Rect(r.x + 24, r.y + 84, 412, 22), $"Theirs: {Describe(e, Campaign.EnemyFaction)}", _label);
            string odds = ours > theirs * 1.5f ? "We have the weight." : theirs > ours * 1.5f ? "We are outmatched." : "It could go either way.";
            GUI.Label(new Rect(r.x + 24, r.y + 112, 412, 22), odds, _small);
            if (GUI.Button(new Rect(r.x + 24, r.y + 150, 412, 34), "Take command", new GUIStyle(_button) { alignment = TextAnchor.MiddleCenter, fontSize = 15 }))
                Director.Attend();
            if (GUI.Button(new Rect(r.x + 24, r.y + 192, 412, 34), "Leave it to the fleet", new GUIStyle(_button) { alignment = TextAnchor.MiddleCenter, fontSize = 15 }))
                Director.LeaveToFleet();
        }

        string Describe(Engagement e, int faction)
        {
            var counts = new Dictionary<string, int>();
            foreach (int id in e.Fleets)
            {
                var f = C.FleetById(id);
                if (f == null || f.Owner != faction) continue;
                foreach (var s in f.Ships)
                {
                    counts.TryGetValue(s.Def.Id, out int n);
                    counts[s.Def.Id] = n + 1;
                }
            }
            var parts = new List<string>();
            foreach (var kv in counts) parts.Add($"{kv.Value} {kv.Key}{(kv.Value > 1 ? "s" : "")}");
            return parts.Count == 0 ? "none" : string.Join(", ", parts);
        }

        void EndBanner()
        {
            if (C.State == CampaignState.Ongoing) return;
            bool won = C.State == CampaignState.Victory;
            _banner.normal.textColor = won ? new Color(0.6f, 0.95f, 1f) : new Color(1f, 0.45f, 0.4f);
            GUI.Label(new Rect(0, Screen.height * 0.25f, Screen.width, 60), won ? "THE WAR IS WON" : "THE WAR IS LOST", _banner);
        }
    }
}
