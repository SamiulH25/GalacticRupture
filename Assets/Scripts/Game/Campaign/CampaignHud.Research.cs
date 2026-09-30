using System.Linq;
using GalacticRupture.Sim;
using GalacticRupture.Sim.Strategic;
using UnityEngine;

namespace GalacticRupture.Game
{
    // The research window: six branches across, four tiers down. Click a technology to queue it
    // (its prerequisites come with it); click a queued one to take it off; the first in the queue
    // that can be studied is worked on. Hover to see what it does and which guns it unlocks.
    public sealed partial class CampaignHud
    {
        bool _showResearch;
        string _hoverTech;

        public bool ShowResearch { get => _showResearch; set => _showResearch = value; }

        static readonly Color Known = new Color(0.45f, 0.8f, 0.55f);
        static readonly Color Working = new Color(0.55f, 0.8f, 1f);

        static string BranchName(TechBranch b) => b switch
        {
            TechBranch.Ballistic => "Ballistic",
            TechBranch.Energy => "Energy",
            TechBranch.Disruptive => "Disruptive",
            TechBranch.Defence => "Defence",
            TechBranch.Industry => "Industry",
            _ => "Logistics & command",
        };

        static Color BranchColour(TechBranch b) => b switch
        {
            TechBranch.Ballistic => new Color(1f, 0.62f, 0.3f),
            TechBranch.Energy => new Color(0.4f, 0.8f, 1f),
            TechBranch.Disruptive => new Color(0.6f, 0.62f, 1f),
            TechBranch.Defence => new Color(0.9f, 0.85f, 0.45f),
            TechBranch.Industry => new Color(0.55f, 0.85f, 0.55f),
            _ => new Color(0.85f, 0.65f, 0.9f),
        };

        void ResearchWindow()
        {
            const int me = Campaign.PlayerFaction;
            var fac = C.Factions[me];
            float w = Mathf.Min(1480, Screen.width - 60), h = Mathf.Min(820, Screen.height - 110);
            var r = Box(Screen.width * 0.5f - w * 0.5f, 66, w, h, Working, solid: true);
            GUI.Label(new Rect(r.x + 20, r.y + 14, 400, 30), "Research", _title);
            var current = C.Current(me);
            float perDay = C.ResearchPerDay(me);
            string working = current == null ? "<color=#ffb070>nothing queued: research is banked</color>"
                : $"studying <b>{current.Name}</b> · {Mathf.Min(fac.ResearchBank, current.Cost):0} / {current.Cost}" +
                  (perDay > 0f ? $" · {Mathf.Max(0f, current.Cost - fac.ResearchBank) / perDay:0} days" : "");
            GUI.Label(new Rect(r.x + 20, r.y + 44, w - 200, 20),
                $"<b>{perDay:0.#}</b> research a day from population and laboratories · banked {fac.ResearchBank:0} · {working}", _small);
            if (Button(new Rect(r.xMax - 120, r.y + 14, 100, 28), "CLOSE")) _showResearch = false;

            var branches = (TechBranch[])System.Enum.GetValues(typeof(TechBranch));
            float top = r.y + 80, left = r.x + 20;
            float colW = (w - 40 - 12 * (branches.Length - 1)) / branches.Length;
            float rowH = Mathf.Min(150, (h - 80 - 30 - 118) / 4f);
            _hoverTech = null;

            for (int b = 0; b < branches.Length; b++)
            {
                float x = left + b * (colW + 12);
                Fill(new Rect(x, top, colW, 3), BranchColour(branches[b]));
                GUI.Label(new Rect(x, top + 6, colW, 20), Caps(BranchName(branches[b])), new GUIStyle(_caps) { normal = { textColor = BranchColour(branches[b]) } });
                for (int tier = 1; tier <= 4; tier++)
                {
                    var techs = Techs.All.Where(t => t.Branch == branches[b] && t.Tier == tier).ToList();
                    float y = top + 30 + (tier - 1) * rowH;
                    if (b == 0) GUI.Label(new Rect(r.x + 2, y + 4, 18, 20), $"{tier}", _tiny);
                    float cardH = techs.Count > 1 ? (rowH - 10) / techs.Count - 4 : rowH - 14;
                    for (int i = 0; i < techs.Count; i++)
                        TechCard(new Rect(x, y + i * (cardH + 4), colW, cardH), techs[i], branches[b]);
                }
            }

            // What the hovered technology does.
            float by = r.yMax - 100;
            Divider(r, by - 6);
            var hover = _hoverTech != null ? Techs.ById(_hoverTech) : current;
            if (hover != null)
            {
                GUI.Label(new Rect(left, by, w - 40, 24), $"<b>{hover.Name}</b>  <color=#8aa0b8>tier {hover.Tier} · {hover.Cost} research" +
                    (hover.Requires.Length > 0 ? $" · needs {string.Join(", ", hover.Requires.Select(q => Techs.ById(q).Name))}" : "") + "</color>", _text);
                GUI.Label(new Rect(left, by + 26, w - 40, 20), hover.Text, _label);
                var guns = Techs.Unlocks(hover).ToList();
                if (guns.Count > 0)
                    GUI.Label(new Rect(left, by + 50, w - 40, 20), "unlocks " + string.Join(" · ", guns.Select(m =>
                        $"<b>{m.Name}</b> <color=#8aa0b8>({m.Size.ToString().ToLowerInvariant()} {m.Channel.ToString().ToLowerInvariant()}, {m.Damage * m.Barrels / m.Reload:0} dps, {m.Range / 1000f:0.#} km)</color>")), _small);
            }
            else GUI.Label(new Rect(left, by, w - 40, 24), "Hover a technology to see what it does. Build laboratories, or give governors a research focus, to study faster.", _small);
        }

        void TechCard(Rect c, TechDef t, TechBranch b)
        {
            const int me = Campaign.PlayerFaction;
            var fac = C.Factions[me];
            bool known = fac.Known.Contains(t.Id);
            int queued = fac.ResearchQueue.IndexOf(t.Id);
            bool current = C.Current(me) == t;
            bool open = C.CanResearch(me, t);
            _panels.Add(c);

            Color edge = known ? Known : current ? Working : queued >= 0 ? new Color(0.8f, 0.8f, 0.5f) : open ? BranchColour(b) * 0.8f : new Color(0.3f, 0.34f, 0.4f);
            Fill(c, new Color(0.06f, 0.09f, 0.13f, known ? 0.7f : 0.95f));
            Fill(new Rect(c.x, c.y, 3, c.height), edge);
            bool hover = c.Contains(Event.current.mousePosition);
            if (hover)
            {
                Fill(c, new Color(1f, 1f, 1f, 0.05f));
                _hoverTech = t.Id;
            }

            var name = new GUIStyle(_label) { wordWrap = true, fontSize = 14, normal = { textColor = known ? Known : open || queued >= 0 ? Ink : Muted } };
            GUI.Label(new Rect(c.x + 10, c.y + 4, c.width - 16, 36), t.Name, name);
            string state = known ? "known" : current ? $"{Mathf.Min(fac.ResearchBank, t.Cost) / t.Cost:P0}" : queued >= 0 ? $"queued #{queued + 1}" : $"{t.Cost}";
            GUI.Label(new Rect(c.x + 10, c.yMax - 20, c.width - 16, 18), state, _tiny);
            var guns = Techs.Unlocks(t).ToList();
            if (guns.Count > 0 && c.height > 58)
                GUI.Label(new Rect(c.x + 10, c.yMax - 38, c.width - 16, 18), string.Join(", ", guns.Select(m => m.Name.ToLowerInvariant())), new GUIStyle(_tiny) { normal = { textColor = BranchColour(b) * 0.9f } });
            if (current) Bar(new Rect(c.x + 3, c.yMax - 3, c.width - 3, 3), fac.ResearchBank / t.Cost, Working);

            if (known || Event.current.type != EventType.MouseDown || !hover) return;
            if (queued >= 0) C.Dequeue(me, t.Id);
            else C.Enqueue(me, t.Id);
            Event.current.Use();
        }
    }
}
