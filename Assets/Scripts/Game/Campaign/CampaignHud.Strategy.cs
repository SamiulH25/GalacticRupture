using System.Collections.Generic;
using System.Linq;
using GalacticRupture.Sim;
using GalacticRupture.Sim.Strategic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GalacticRupture.Game
{
    // The long campaign's panels (IDEAS.md §1.1, §1.2): a system's economy, buildings and
    // governor; reclaiming ruins and courting holdouts; envoys waiting with offers; the landing
    // forecast under the cursor; and the ship designer.
    public sealed partial class CampaignHud
    {
        bool _showDesigner;
        Vector2 _sysScroll, _designScroll;
        float _sysContentH = 600f;
        int _designIndex;

        public bool ShowDesigner { get => _showDesigner; set => _showDesigner = value; }

        static readonly BuildingKind[] Kinds =
        {
            BuildingKind.Refinery, BuildingKind.Fab, BuildingKind.Yard, BuildingKind.Garrison,
            BuildingKind.Admin, BuildingKind.Habitat, BuildingKind.Depot,
        };

        static string Name(BuildingKind k) => k switch
        {
            BuildingKind.Admin => "Administration",
            BuildingKind.Depot => "Supply depot",
            _ => k.ToString(),
        };

        static string FocusName(GovernorFocus f) => f.ToString().ToUpperInvariant();

        // --- System --------------------------------------------------------------------------

        void SystemPanel()
        {
            if (Map.SelectedSystem < 0) return;
            var s = C.Galaxy.Systems[Map.SelectedSystem];
            bool seen = Intel.SystemVisible[s.Id];
            int owner = Intel.SeenHour[s.Id] >= 0 ? Intel.OwnerSeen[s.Id] : -1;
            bool ours = s.Owner == Campaign.PlayerFaction;
            const float w = 400;
            float maxH = Screen.height - 66 - 260;
            var r = Box(Screen.width - w - 16, 66, w, Mathf.Min(_sysContentH + 16, maxH), FactionStyle.Of(owner));
            var view = new Rect(0, 0, w - 18, _sysContentH);
            _sysScroll = GUI.BeginScrollView(new Rect(r.x, r.y + 4, w, r.height - 8), _sysScroll, view, false, false);
            float x = 16, y = 8, cw = w - 50;

            string status = s.OldCapital ? "the old capital" : owner >= 0 ? C.Factions[owner].Name
                : s.Derelict == Derelict.Ruin ? "ruin of the old empire" : s.Derelict == Derelict.Holdout ? "holdout" : "unclaimed";
            GUI.Label(new Rect(x, y, cw, 20), Caps(status), new GUIStyle(_caps) { normal = { textColor = FactionStyle.Of(owner) } });
            y += 18;
            GUI.Label(new Rect(x, y, cw, 30), s.Name + (s.Capital ? "   <size=15><color=#c9b27a>capital</color></size>" : ""), _title);
            y += 34;

            if (!seen)
            {
                GUI.Label(new Rect(x, y, cw, 40), Intel.SeenHour[s.Id] < 0 ? "never seen: send a fleet"
                    : $"last seen {(C.Hours - Intel.SeenHour[s.Id]) / 24:0} days ago; garrison then ~{Intel.GarrisonSeen[s.Id]:0}", _small);
                y += 26;
                Finish(y);
                return;
            }

            GUI.Label(new Rect(x, y, cw, 20),
                $"population {s.Population:0.0} / {s.MaxPopulation:0} · {s.Slots} slots" + (s.Owner >= 0 ? $" · stability {s.Stability:0} <color=#8aa0b8>(→{C.StabilityTarget(s):0})</color>" : ""), _small);
            y += 20;
            if (s.Unrest > 0f)
            {
                GUI.Label(new Rect(x, y, cw, 20), $"<color=#ffb070>unrest: {s.Unrest / 24f:0.0} days without output</color>", _small);
                y += 20;
            }
            if (s.Owner >= 0)
            {
                var o = C.Output(s);
                GUI.Label(new Rect(x, y, cw, 20),
                    $"<b>{o.Credits:0.#}</b> cr · <b>{o.Industry:0.#}</b> ind · <b>{o.Influence:0.#}</b> inf a day" + (o.Cut > 0f ? $" <color=#8aa0b8>(governor takes {o.Cut:0.#} cr)</color>" : ""), _small);
                y += 20;
            }
            y += 8;
            Divider(new Rect(0, 0, w, 0), y - 4);

            y = DerelictSection(s, x, y, cw);
            y = BuildingSection(s, ours, x, y, cw);
            if (ours) y = GovernorSection(s, x, y, cw);
            y = GarrisonSection(s, x, y, cw);
            if (ours && s.Shipyard) y = YardSection(s, x, y, cw);
            Finish(y);
        }

        void Finish(float y)
        {
            GUI.EndScrollView();
            if (Event.current.type == EventType.Repaint) _sysContentH = y + 8;
        }

        float DerelictSection(StarSystem s, float x, float y, float cw)
        {
            if (s.Owner >= 0 || s.Derelict == Derelict.None) return y;
            const int me = Campaign.PlayerFaction;
            if (s.Derelict == Derelict.Ruin)
            {
                GUI.Label(new Rect(x, y, cw, 40), "Dead fabs and drifting stations. An expedition (any fleet of ours on station) can restore it.", new GUIStyle(_small) { wordWrap = true });
                y += 46;
                if (s.Reclaimer >= 0)
                {
                    GUI.Label(new Rect(x, y, cw, 20), $"{(s.Reclaimer == me ? "our" : $"the {Director.Tag(s.Reclaimer)}")} expedition: {s.Reclaim:P0}", _label);
                    Bar(new Rect(x, y + 21, cw, 3), s.Reclaim, ArmyBar);
                    y += 30;
                }
                if (s.Reclaimer != me)
                {
                    bool can = C.CanReclaim(me, s.Id);
                    if (Button(new Rect(x, y, cw, 30), $"RECLAIM   <color=#d9b870>{C.ReclaimCost(s)} cr</color>   <color=#8aa0b8>{C.ReclaimDays(s):0} days on station</color>", can, can))
                        C.StartReclaim(me, s.Id);
                    y += 34;
                    if (!can && !C.OnStation(me, s.Id))
                    {
                        GUI.Label(new Rect(x, y, cw, 18), "needs a fleet of ours here, and no hostile warships", _tiny);
                        y += 20;
                    }
                }
            }
            else
            {
                GUI.Label(new Rect(x, y, cw, 36), s.OldCapital ? "The seat of the old empire, walled and garrisoned. It can only be taken by force."
                    : "Cut off but alive. Bring it in with influence while a fleet shows the flag, or land an army.", new GUIStyle(_small) { wordWrap = true });
                y += 46;
                if (s.Courter >= 0)
                {
                    GUI.Label(new Rect(x, y, cw, 20), $"{(s.Courter == me ? "our" : $"the {Director.Tag(s.Courter)}'s")} envoys: {s.Courting:P0}", _label);
                    Bar(new Rect(x, y + 21, cw, 3), s.Courting, ArmyBar);
                    y += 30;
                }
                if (!s.OldCapital && s.Courter != me)
                {
                    bool can = C.CanCourt(me, s.Id);
                    if (Button(new Rect(x, y, cw, 30), $"COURT IT   <color=#d9b870>{C.CourtCost(s)} inf</color>   <color=#8aa0b8>{Campaign.CourtDays:0} days on station</color>", can, can))
                        C.StartCourting(me, s.Id);
                    y += 34;
                }
            }
            Divider(new Rect(0, 0, cw + 50, 0), y + 2);
            return y + 10;
        }

        float BuildingSection(StarSystem s, bool ours, float x, float y, float cw)
        {
            GUI.Label(new Rect(x, y, cw, 20), Caps($"Buildings {s.Buildings.Count} / {s.Slots}"), _caps);
            y += 22;
            for (int i = 0; i < s.Buildings.Count; i++)
            {
                var b = s.Buildings[i];
                string state = b.Dormant ? "<color=#8a8f9a>dormant</color>" : b.HoursLeft > 0f ? $"<color=#d9b870>building, {b.HoursLeft / 24f / Mathf.Max(0.1f, C.BuilderRate(s)):0.#} days</color>" : "";
                GUI.Label(new Rect(x, y, cw - 30, 20), $"{Name(b.Kind)}  {state}", _label);
                if (ours && Button(new Rect(x + cw - 24, y, 24, 20), "x"))
                {
                    C.Demolish(Campaign.PlayerFaction, s.Id, i);
                    break;
                }
                y += 22;
            }
            if (ours && s.FreeSlots > 0)
            {
                y += 4;
                if (C.Constructing(s))
                {
                    GUI.Label(new Rect(x, y, cw, 18), "one building at a time; the next can be queued", _tiny);
                    y += 18;
                }
                float bw = (cw - 6) / 2f;
                for (int i = 0; i < Kinds.Length; i++)
                {
                    var k = Kinds[i];
                    var rect = new Rect(x + (i % 2) * (bw + 6), y + (i / 2) * 30, bw, 26);
                    if (Button(rect, $"{Name(k)}  <color=#d9b870>{C.CostHere(s, k)}</color>", C.CanConstruct(Campaign.PlayerFaction, s.Id, k)))
                        C.Construct(Campaign.PlayerFaction, s.Id, k);
                    if (rect.Contains(Event.current.mousePosition)) _hoverText = Campaign.Describe(k);
                }
                y += (Kinds.Length + 1) / 2 * 30;
                GUI.Label(new Rect(x, y, cw, 18), _hoverText ?? "hover a building to see what it does", _tiny);
                y += 20;
            }
            y += 6;
            Divider(new Rect(0, 0, cw + 50, 0), y - 2);
            return y + 8;
        }

        string _hoverText;

        float GovernorSection(StarSystem s, float x, float y, float cw)
        {
            const int me = Campaign.PlayerFaction;
            GUI.Label(new Rect(x, y, cw, 20), Caps("Governor"), _caps);
            y += 22;
            var g = s.Governor;
            if (g != null)
            {
                GUI.Label(new Rect(x, y, cw, 20), $"{g.Name}  <color=#8aa0b8>{g.TraitText}</color>", _label);
                y += 20;
                GUI.Label(new Rect(x, y, cw, 20), $"administration {g.Administration} · industry {g.Industry} · loyalty {g.Loyalty} · takes {g.Cut:P0}", _tiny);
                y += 22;
                float fw = (cw - 18) / 4f;
                for (int i = 0; i < 4; i++)
                {
                    var f = (GovernorFocus)i;
                    if (Button(new Rect(x + i * (fw + 6), y, fw, 24), FocusName(f), true, g.Focus == f)) C.SetFocus(me, s.Id, f);
                }
                y += 30;
                if (Button(new Rect(x, y, cw, 24), "DISMISS  <color=#8aa0b8>stability suffers</color>")) C.Dismiss(me, s.Id);
                y += 30;
            }
            else
            {
                GUI.Label(new Rect(x, y, cw, 34), $"A governor builds the system for you, for a cut of what it makes. Appointing costs {Campaign.AppointCost} influence.", new GUIStyle(_tiny) { wordWrap = true });
                y += 36;
                var pool = C.Pool(me);
                for (int i = 0; i < pool.Count; i++)
                {
                    var c = pool[i];
                    string origin = c.Origin == GovernorOrigin.Notable ? $" · of {c.Home}" : "";
                    GUI.Label(new Rect(x, y, cw - 110, 20), $"{c.Name}{origin}", _label);
                    GUI.Label(new Rect(x, y + 18, cw - 110, 18), $"A{c.Administration} I{c.Industry} L{c.Loyalty} · {c.TraitText} · {c.Cut:P0}", _tiny);
                    if (Button(new Rect(x + cw - 100, y + 4, 100, 28), "APPOINT", C.CanAppoint(me, s.Id)))
                    {
                        var focus = s.Capital ? GovernorFocus.Industry : GovernorFocus.Balanced;
                        if (C.Appoint(me, s.Id, i, focus)) Director.Flush();
                        break;
                    }
                    y += 40;
                }
                foreach (var a in C.ArmiesAt(s.Id).Where(a => a.Owner == me).Take(1))
                {
                    if (Button(new Rect(x, y, cw, 26), $"Retire {a.Commander.Name} to govern here"))
                    {
                        C.RetireCommander(a.Id, s.Id, GovernorFocus.Military);
                        Director.Flush();
                    }
                    y += 30;
                }
            }
            Divider(new Rect(0, 0, cw + 50, 0), y + 2);
            return y + 10;
        }

        float GarrisonSection(StarSystem s, float x, float y, float cw)
        {
            GUI.Label(new Rect(x, y, cw, 20), Caps("Garrison"), _caps);
            y += 22;
            GUI.Label(new Rect(x, y, 200, 20), $"militia {s.Militia:0}", _label);
            Bar(new Rect(x + cw - 114, y + 8, 114, 4), s.Militia / Mathf.Max(1f, Campaign.MaxMilitia(s)), ArmyBar);
            y += 24;
            foreach (var a in C.ArmiesAt(s.Id))
            {
                bool attacker = a.Owner != s.Owner;
                GUI.Label(new Rect(x, y, cw, 20), $"{(attacker ? $"<color=#{FactionStyle.Hex(a.Owner)}>" : "")}{a.Name}{(attacker ? "</color>" : "")}", _label);
                GUI.Label(new Rect(x, y + 17, cw, 18), $"{a.Commander.Name}, {a.Commander.TraitText}", _tiny);
                Bar(new Rect(x + cw - 114, y + 8, 114, 4), a.Strength / Army.MaxStrength, attacker ? FactionStyle.Of(a.Owner) : ArmyBar);
                y += 40;
            }
            if (s.Besieger >= 0)
            {
                GUI.Label(new Rect(x, y, cw, 20), $"ground war: the {Director.Tag(s.Besieger)} holds {s.Siege:P0} of the fighting strength", _small);
                y += 22;
            }
            return y + 6;
        }

        float YardSection(StarSystem s, float x, float y, float cw)
        {
            const int me = Campaign.PlayerFaction;
            Divider(new Rect(0, 0, cw + 50, 0), y);
            y += 8;
            float rate = C.YardRate(s);
            GUI.Label(new Rect(x, y, cw, 20), Caps($"Yard ×{s.Count(BuildingKind.Yard)}") + $"   <color=#8aa0b8>works at {rate:0.0}× from industry</color>", _caps);
            y += 22;
            if (C.Yards.TryGetValue(s.Id, out var queue))
                foreach (var item in queue)
                {
                    string what = item.Def == null ? "army" : item.Def.Design.Length > 0 ? item.Def.Design : item.Def.Id;
                    GUI.Label(new Rect(x, y, cw, 18), $"{what} · {item.HoursLeft / 24f / (item.Def == null ? 1f : rate):0.0} days", _tiny);
                    y += 20;
                }
            foreach (var d in C.Factions[me].Designs)
            {
                var hull = d.Built;
                bool can = C.CanBuild(me, s.Id, hull);
                if (Button(new Rect(x, y, cw, 28), $"{d.Name.ToUpperInvariant()}   <color=#d9b870>{hull.Cost} cr</color>   <color=#8aa0b8>{hull.BuildDays / rate:0.#} days</color>", can)
                    && C.QueueBuild(me, s.Id, hull))
                    Director.Say($"{Director.Date}: a {d.Name} laid down at {s.Name}.");
                y += 31;
            }
            bool canArmy = C.CanRaiseArmy(me, s.Id);
            if (Button(new Rect(x, y, cw, 28), $"RAISE AN ARMY   <color=#d9b870>{Campaign.ArmyCost} cr</color>   <color=#8aa0b8>{Campaign.ArmyDays:0} days</color>", canArmy)
                && C.RaiseArmy(me, s.Id))
                Director.Say($"{Director.Date}: an army is being raised at {s.Name}.");
            return y + 34;
        }

        // Shown in the fleet panel when the fleet sits on a ruin or a holdout.
        float ExpeditionButtons(Rect r, float y, Fleet f, StarSystem here)
        {
            const int me = Campaign.PlayerFaction;
            if (here.Owner >= 0 || here.Derelict == Derelict.None) return y;
            if (here.Derelict == Derelict.Ruin)
            {
                if (here.Reclaimer == me)
                {
                    GUI.Label(new Rect(r.x + 16, y, 348, 20), $"reclaiming {here.Name}: {here.Reclaim:P0}", _small);
                    Bar(new Rect(r.x + 16, y + 21, 348, 3), here.Reclaim, ArmyBar);
                    return y + 30;
                }
                if (Button(new Rect(r.x + 16, y, 348, 30), $"RECLAIM {here.Name.ToUpperInvariant()}   <color=#d9b870>{C.ReclaimCost(here)} cr</color>", C.CanReclaim(me, here.Id), true))
                    C.StartReclaim(me, here.Id);
                return y + 36;
            }
            if (here.OldCapital) return y;
            if (here.Courter == me)
            {
                GUI.Label(new Rect(r.x + 16, y, 348, 20), $"courting {here.Name}: {here.Courting:P0}", _small);
                Bar(new Rect(r.x + 16, y + 21, 348, 3), here.Courting, ArmyBar);
                return y + 30;
            }
            if (Button(new Rect(r.x + 16, y, 348, 30), $"COURT {here.Name.ToUpperInvariant()}   <color=#d9b870>{C.CourtCost(here)} inf</color>", C.CanCourt(me, here.Id), true))
                C.StartCourting(me, here.Id);
            return y + 36;
        }

        // --- Envoys ----------------------------------------------------------------------------

        void OffersPanel()
        {
            if (C.Incoming.Count == 0) return;
            int n = Mathf.Min(3, C.Incoming.Count);
            float w = 470, h = 34 + n * 58;
            var r = Box(16, Screen.height - 40 - h, w, h, new Color(0.85f, 0.75f, 0.5f));
            GUI.Label(new Rect(r.x + 16, r.y + 8, w - 32, 20), Caps($"Envoys waiting {C.Incoming.Count}"), _caps);
            float y = r.y + 32;
            foreach (var p in C.Incoming.Take(n).ToList())
            {
                string what = p.Treaty switch
                {
                    DiplomaticStance.Peace => "offers peace",
                    DiplomaticStance.NonAggression => "proposes non-aggression",
                    _ => "proposes an alliance",
                };
                GUI.Label(new Rect(r.x + 16, y, w - 200, 20), $"The {Director.Tag(p.From)} {what}", _label);
                GUI.Label(new Rect(r.x + 16, y + 19, w - 200, 18), $"trust {C.Trust(p.From, Campaign.PlayerFaction):+0;-0;0} · leaves in {(p.Expires - C.Hours) / 24:0} days", _tiny);
                if (Button(new Rect(r.xMax - 176, y + 4, 80, 28), "ACCEPT", true, true)) { C.Respond(p, true); Director.Flush(); }
                if (Button(new Rect(r.xMax - 90, y + 4, 76, 28), "DECLINE")) C.Respond(p, false);
                y += 58;
            }
        }

        // --- The landing forecast under the cursor ----------------------------------------------

        void LandingForecast()
        {
            var f = C.FleetById(Map.SelectedFleet);
            if (f == null || f.Owner != Campaign.PlayerFaction || C.CountIn(f) == 0) return;
            var mouse = Mouse.current;
            if (mouse == null) return;
            Vector2 mp = mouse.position.ReadValue();
            int sys = PickSystem(mp);
            if (sys < 0) return;
            var s = C.Galaxy.Systems[sys];
            if (s.Owner == f.Owner) return;
            string text;
            if (s.Owner < 0 && s.Derelict == Derelict.Ruin) text = "a ruin: nobody to fight. Reclaim it instead.";
            else if (s.Owner >= 0 && C.Stance(f.Owner, s.Owner) != DiplomaticStance.War) text = $"at peace with the {C.Factions[s.Owner].Name}: no landing";
            else
            {
                var fc = C.Forecast(C.ArmiesIn(f), s);
                text = fc.AttackerWins ? $"<color=#9fe39a>landing: likely to take it in about {fc.Hours / 24f:0.#} days</color>" : "<color=#ff8a70>landing: likely to be thrown back</color>";
                if (!Intel.SystemVisible[s.Id]) text += " <color=#8aa0b8>(old intelligence)</color>";
            }
            var r = new Rect(mp.x + 18, Screen.height - mp.y + 18, 380, 26);
            Fill(r, new Color(0f, 0f, 0f, 0.75f));
            GUI.Label(new Rect(r.x + 8, r.y + 3, r.width - 12, 20), text, _small);
        }

        // --- Designer --------------------------------------------------------------------------

        void DesignerWindow()
        {
            const int me = Campaign.PlayerFaction;
            var designs = C.Factions[me].Designs;
            if (designs.Count == 0) return;
            _designIndex = Mathf.Clamp(_designIndex, 0, designs.Count - 1);
            var d = designs[_designIndex];

            float w = 1000, h = Mathf.Min(760, Screen.height - 120);
            var r = Box(Screen.width * 0.5f - w * 0.5f, 70, w, h, FactionStyle.Of(me), solid: true);
            GUI.Label(new Rect(r.x + 20, r.y + 14, 400, 30), "Ship designs", _title);
            GUI.Label(new Rect(r.x + 20, r.y + 44, 600, 20), "Every hull's hardpoints take any gun of their size or smaller. Yards build what is listed here.", _small);
            if (Button(new Rect(r.xMax - 120, r.y + 14, 100, 28), "CLOSE")) _showDesigner = false;

            // The list.
            float lx = r.x + 20, ly = r.y + 80;
            GUI.Label(new Rect(lx, ly, 260, 20), Caps("Designs"), _caps);
            ly += 22;
            for (int i = 0; i < designs.Count; i++)
            {
                var o = designs[i];
                if (Button(new Rect(lx, ly, 280, 28), $"{o.Name}  <color=#8aa0b8>{o.Hull.Class.ToString().ToLowerInvariant()}</color>", true, i == _designIndex)) _designIndex = i;
                ly += 31;
            }
            ly += 8;
            GUI.Label(new Rect(lx, ly, 260, 20), Caps("New from a hull"), _caps);
            ly += 22;
            foreach (var hull in Hulls.All)
            {
                if (Button(new Rect(lx, ly, 280, 26), hull.Class.ToString().ToUpperInvariant()))
                {
                    designs.Add(new ShipDesign(UniqueName($"New {hull.Class}"), hull));
                    _designIndex = designs.Count - 1;
                }
                ly += 29;
            }

            // The design.
            float dx = r.x + 330, dy = r.y + 80, dw = w - 350;
            var nameStyle = new GUIStyle(GUI.skin.textField) { font = Art.UiFontBold, fontSize = 20, normal = { textColor = Ink } };
            string renamed = GUI.TextField(new Rect(dx, dy, 360, 32), d.Name, 32, nameStyle);
            if (renamed != d.Name && renamed.Trim().Length > 0 && designs.All(o => o == d || o.Name != renamed)) d.Name = renamed;
            _panels.Add(new Rect(dx, dy, 360, 32));
            if (Button(new Rect(dx + 372, dy + 2, 80, 28), "COPY"))
            {
                designs.Add(d.Copy(UniqueName(d.Name + " II")));
                _designIndex = designs.Count - 1;
                return;
            }
            bool lastOfClass = designs.Count(o => o.Hull.Class == d.Hull.Class) <= 1;
            if (Button(new Rect(dx + 460, dy + 2, 90, 28), "DELETE", !lastOfClass))
            {
                designs.RemoveAt(_designIndex);
                return;
            }
            if (Button(new Rect(dx + 558, dy + 2, 100, 28), "FIRST", _designIndex > 0))
            {
                designs.RemoveAt(_designIndex);
                designs.Insert(0, d);
                _designIndex = 0;
                return;
            }
            dy += 42;

            var built = d.Built;
            GUI.Label(new Rect(dx, dy, dw, 20),
                $"{d.Hull.Class} hull · <b>{built.Cost}</b> cr · {built.BuildDays:0.#} days · upkeep {built.Cost * Campaign.ShipUpkeep:0.#} cr/day", _label);
            dy += 22;
            GUI.Label(new Rect(dx, dy, dw, 20),
                $"hull {built.HullHp:0} · shields {built.ShieldHp:0} (+{built.ShieldRegen:0}/s) · {built.MaxSpeed:0} m/s · {built.LyPerDay:0.#} ly/day" + (built.TroopCapacity > 0 ? $" · carries {built.TroopCapacity} army" : ""), _small);
            dy += 22;
            GUI.Label(new Rect(dx, dy, dw, 20), Summary(built), _small);
            dy += 30;
            Divider(new Rect(dx - 16, 0, dw + 32, 0), dy - 6);

            var list = new Rect(dx, dy, dw, r.yMax - dy - 16);
            float rowH = 38;
            var view = new Rect(0, 0, dw - 20, d.Mounts.Count * rowH);
            _designScroll = GUI.BeginScrollView(list, _designScroll, view, false, false);
            for (int i = 0; i < d.Mounts.Count; i++)
            {
                var hp = d.Hull.Hardpoints[i];
                var m = d.Mounts[i];
                float yy = i * rowH;
                string where = hp.LocalPosition.Z > 0f ? "fore" : "aft";
                string side = hp.LocalPosition.X > 1f ? " starboard" : hp.LocalPosition.X < -1f ? " port" : " centreline";
                GUI.Label(new Rect(0, yy + 6, 150, 20), $"{hp.Size.ToString().ToLowerInvariant()} · {where}{side}", _tiny);
                if (GUI.Button(new Rect(150, yy, 30, 30), "<", _button)) Cycle(d, i, -1);
                GUI.Label(new Rect(188, yy + 4, 150, 22), m.Name, _label);
                if (GUI.Button(new Rect(336, yy, 30, 30), ">", _button)) Cycle(d, i, 1);
                GUI.Label(new Rect(378, yy + 6, dw - 400, 20),
                    $"{m.Channel.ToString().ToLowerInvariant()} · {m.Damage * m.Barrels / m.Reload:0} dps · {m.Range / 1000f:0.#} km · {m.Cost} cr", _tiny);
            }
            GUI.EndScrollView();
        }

        static void Cycle(ShipDesign d, int hardpoint, int dir)
        {
            var hp = d.Hull.Hardpoints[hardpoint];
            var fits = Hulls.Mounts.Where(m => ShipDesign.Fits(m, hp)).ToList();
            int i = fits.IndexOf(d.Mounts[hardpoint]);
            i = ((i + dir) % fits.Count + fits.Count) % fits.Count;
            d.Set(hardpoint, fits[i]);
        }

        static string Summary(HullDef h)
        {
            float bal = 0f, en = 0f, dis = 0f, pd = 0f;
            foreach (var hp in h.Hardpoints)
            {
                var m = hp.Mount;
                float dps = m.Damage * m.Barrels / m.Reload;
                if (m.Class == MountClass.PointDefence) pd += dps;
                else if (m.Channel == DamageChannel.Ballistic) bal += dps;
                else if (m.Channel == DamageChannel.Energy) en += dps;
                else dis += dps;
            }
            return $"broadside: ballistic {bal:0} · energy {en:0} · disruptive {dis:0} · point defence {pd:0} dps";
        }

        string UniqueName(string name)
        {
            var designs = C.Factions[Campaign.PlayerFaction].Designs;
            string n = name;
            for (int i = 2; designs.Any(d => d.Name == n); i++) n = $"{name} {i}";
            return n;
        }
    }
}
