using System.Collections.Generic;
using GalacticRupture.Sim;
using GalacticRupture.Sim.Strategic;
using UnityEngine;

namespace GalacticRupture.Game
{
    // Before taking command: choose the vanguard, the ships that start on the field. Everything
    // else waits in reserve and is called in during the battle, near the ships already fighting.
    public sealed partial class CampaignHud
    {
        bool _deploying;
        Engagement _deployFor;
        readonly HashSet<(int Fleet, int Index)> _vanguard = new HashSet<(int, int)>();
        Vector2 _deployScroll;

        public bool Deploying => _deploying;

        public void OpenDeployment()
        {
            _deploying = true;
            _deployFor = C.Pending;
            _vanguard.Clear();
            _deployScroll = Vector2.zero;
            // By default the heaviest warship leads alone.
            (int, int) lead = (-1, -1);
            int best = -1;
            foreach (var (fleet, index, ship) in BattleSetup.PlayerShips(C, C.Pending))
                if (ship.Def.Class != HullClass.Transport && ship.Def.Cost > best)
                {
                    best = ship.Def.Cost;
                    lead = (fleet, index);
                }
            if (best >= 0) _vanguard.Add(lead);
        }

        void DeployWindow()
        {
            if (_deployFor != C.Pending)
            {
                _deploying = false;
                return;
            }
            var ships = BattleSetup.PlayerShips(C, C.Pending);
            const float rowH = 40f;
            float listH = Mathf.Min(ships.Count * rowH + 8f, Screen.height * 0.5f);
            var r = Modal(620, 190 + listH, new Color(0.45f, 0.85f, 1f));
            float y = r.y + 18;
            GUI.Label(new Rect(r.x, y, r.width, 22), Caps("Vanguard"), new GUIStyle(_caps) { alignment = TextAnchor.MiddleCenter });
            GUI.Label(new Rect(r.x + 28, y + 24, r.width - 56, 40),
                "These ships open the battle. The rest wait in reserve: call them in from the reinforcements panel, anywhere near the ships already fighting.",
                new GUIStyle(_small) { wordWrap = true, alignment = TextAnchor.UpperCenter });
            y += 74;

            int cap = Reinforcements.DefaultCap, used = 0;
            bool warship = false;
            foreach (var (fleet, index, ship) in ships)
                if (_vanguard.Contains((fleet, index)))
                {
                    used += Reinforcements.Points(ship.Def.Class);
                    warship |= ship.Def.Class != HullClass.Transport;
                }

            var list = new Rect(r.x + 20, y, r.width - 40, listH);
            _deployScroll = GUI.BeginScrollView(list, _deployScroll, new Rect(0, 0, list.width - 20, ships.Count * rowH), false, false);
            for (int i = 0; i < ships.Count; i++)
            {
                var (fleet, index, ship) = ships[i];
                var key = (fleet, index);
                bool on = _vanguard.Contains(key);
                int pts = Reinforcements.Points(ship.Def.Class);
                bool fits = on || used + pts <= cap;
                var row = new Rect(0, i * rowH, list.width - 20, rowH - 4);
                bool hover = row.Contains(Event.current.mousePosition);
                Fill(row, on ? new Color(0.15f, 0.35f, 0.5f, 0.6f) : new Color(1f, 1f, 1f, hover && fits ? 0.08f : 0.03f));
                var tick = new Rect(row.x + 10, row.y + 10, 16, 16);
                Fill(tick, new Color(1f, 1f, 1f, 0.12f));
                if (on) Fill(new Rect(tick.x + 4, tick.y + 4, 8, 8), Ink);
                string name = string.IsNullOrEmpty(ship.Name) ? ship.Def.Id : ship.Name;
                var tone = fits ? Ink : Muted;
                GUI.Label(new Rect(row.x + 36, row.y, 300, 20), $"<color=#{ColorUtility.ToHtmlStringRGB(tone)}>{name}</color>", _label);
                GUI.Label(new Rect(row.x + 36, row.y + 17, 300, 20), ship.Def.Class.ToString().ToLowerInvariant() + (fits ? "" : " · no room"), _tiny);
                Bar(new Rect(row.x + 340, row.y + 16, 140, 4), ship.HullFraction, HullBar);
                GUI.Label(new Rect(row.xMax - 40, row.y + 8, 30, 20), pts.ToString(), new GUIStyle(_label) { alignment = TextAnchor.MiddleRight });
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && hover && fits)
                {
                    if (on) _vanguard.Remove(key);
                    else _vanguard.Add(key);
                    Event.current.Use();
                }
            }
            GUI.EndScrollView();

            y = r.yMax - 88;
            GUI.Label(new Rect(r.x + 28, y - 26, 300, 20), $"command {used}/{cap}" + (warship ? "" : "  <color=#ff9a80>pick at least one warship</color>"), _small);
            float bw = (r.width - 56 - 24) / 3f;
            if (Button(new Rect(r.x + 28, y, bw, 30), "LEAD SHIP ONLY")) OpenDeployment();
            if (Button(new Rect(r.x + 28 + bw + 12, y, bw, 30), "FILL THE LINE"))
            {
                // Heaviest first, as far as the command cap goes.
                _vanguard.Clear();
                used = 0;
                var order = new List<(int, int, ShipRecord)>(ships);
                order.Sort((a, b) => Weight(b.Item3).CompareTo(Weight(a.Item3)));
                foreach (var (fleet, index, ship) in order)
                {
                    int pts = Reinforcements.Points(ship.Def.Class);
                    if (used + pts > cap) continue;
                    used += pts;
                    _vanguard.Add((fleet, index));
                }
            }
            if (Button(new Rect(r.x + 28 + (bw + 12) * 2, y, bw, 30), "BACK")) _deploying = false;
            if (Button(new Rect(r.x + 28, r.yMax - 48, r.width - 56, 34), "BEGIN THE BATTLE", warship, true)) BeginBattle();
        }

        public void BeginBattle()
        {
            _deploying = false;
            Director.Attend(new HashSet<(int, int)>(_vanguard));
        }

        static float Weight(ShipRecord s) => s.Def.Class == HullClass.Transport ? -1f : s.Def.Cost;
    }
}
