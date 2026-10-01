using System.Collections.Generic;
using System.Linq;
using GalacticRupture.Sim.Strategic;
using UnityEngine;

namespace GalacticRupture.Game
{
    // The council as theatre (IDEAS.md §7): a chamber with most seats empty, the resolution as the
    // Secretariat records it, and beside it what will actually happen. The mandate bars show the
    // gap between who the old order recognises and who holds the fabs.
    public sealed partial class CampaignHud
    {
        int _councilSeat = -2;      // faction picked in the chamber; -1 none
        Texture2D _seatDisc;

        sealed class Brief
        {
            public string Official, Cross, Hand, Reality;
        }

        Brief BriefFor(CouncilSession s)
        {
            string T(int f) => f >= 0 && f < C.Factions.Count ? C.Factions[f].Name : "no one";
            switch (s.Kind)
            {
                case ResolutionKind.Ceasefire:
                {
                    int wars = 0;
                    for (int a = 0; a < C.Factions.Count; a++)
                    for (int b = a + 1; b < C.Factions.Count; b++)
                        if (!C.Factions[a].Eliminated && !C.Factions[b].Eliminated && C.Stance(a, b) == DiplomaticStance.War) wars++;
                    return new Brief
                    {
                        Official = "All signatories shall cease hostilities for eight days in the interest of the Succession.",
                        Cross = "cease hostilities", Hand = "rearm and reposition",
                        Reality = $"<b>{wars}</b> war{(wars == 1 ? " is" : "s are")} running. The weaker side of each votes yes to buy time. Eight days of quiet repairs fleets and moves them; it settles nothing.",
                    };
                }
                case ResolutionKind.Recognise:
                {
                    int unrest = 0, ours = 0;
                    foreach (var sys in C.Galaxy.Systems)
                        if (sys.Unrest > 0f && sys.Owner >= 0)
                        {
                            unrest++;
                            if (sys.Owner == Campaign.PlayerFaction) ours++;
                        }
                    return new Brief
                    {
                        Official = "Holdings acquired by successor administrations are recognised as lawful and settled.",
                        Cross = "lawful and settled", Hand = "held, for now",
                        Reality = $"<b>{unrest}</b> captured system{(unrest == 1 ? "" : "s")} are in unrest ({ours} of them ours). Passing it ends the unrest in all of them, and tells every power that conquest is rewarded.",
                    };
                }
                default:
                {
                    bool tabled = s.Kind == ResolutionKind.Embargo;
                    string who = T(s.Target);
                    int atWar = 0;
                    foreach (var kv in s.Votes)
                        if (kv.Key != s.Target && C.Stance(kv.Key, s.Target) == DiplomaticStance.War) atWar++;
                    string origin = tabled ? $" Tabled by the {T(s.TabledBy)}, who paid {Campaign.TableCost:0} influence to make it a motion." : "";
                    return new Brief
                    {
                        Official = $"The Council finds that the {who} has acted against the peace of the Succession, and declares an embargo upon it for twenty days.",
                        Cross = "acted against the peace of the Succession", Hand = "has grown too strong",
                        Reality = $"<b>{atWar}</b> of the voters are already at war with the {who}. The embargo cuts their income for twenty days and stops no ship. Voting for costs trust with them (−6); against earns it (+4).{origin}",
                    };
                }
            }
        }

        static string Plain(string rich) => rich.Replace("<b>", "").Replace("</b>", "");

        bool PlateButton(Rect r, string text, bool primary, bool enabled = true)
        {
            var e = Event.current;
            bool hover = enabled && r.Contains(e.mousePosition);
            var edge = primary ? UiKit.AmberDim : UiKit.Line2;
            UiKit.Fill(r, hover ? (primary ? UiKit.Amber : UiKit.Panel2) : (primary ? new Color(0.082f, 0.071f, 0.039f) : new Color(0f, 0f, 0f, 0.15f)));
            UiKit.Outline(r, edge);
            var tone = !enabled ? UiKit.Line2 : hover && primary ? new Color(0.05f, 0.04f, 0.02f) : primary ? UiKit.Amber : UiKit.Mute;
            UiKit.Text(r, UiKit.Spaced(text), UiKit.Face.Mono, 12, tone, TextAnchor.MiddleCenter);
            if (enabled && e.type == EventType.MouseDown && e.button == 0 && r.Contains(e.mousePosition))
            {
                e.Use();
                GameAudio.Ui(primary ? "confirmation" : "switch");
                return true;
            }
            return false;
        }

        void CouncilWindow()
        {
            UiKit.Init(Art);
            if (_seatDisc == null) _seatDisc = MakeDisc();
            var s = C.Council;
            var brief = BriefFor(s);

            float w = Mathf.Min(1180f, Screen.width - 40f), h = Mathf.Min(660f, Screen.height - 40f);
            UiKit.Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.62f));
            _panels.Add(new Rect(0, 0, Screen.width, Screen.height));
            var r = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            UiKit.Fill(r, UiKit.Bg);
            UiKit.Outline(r, UiKit.AmberDim, 2f);

            // Heading.
            int day = C.Day;
            var voters = C.Factions.Where(f => !f.Eliminated).ToList();
            int totalSeats = voters.Sum(f => Mathf.Max(1, C.Seats(f.Id)));
            int votingSeats = s.Votes.Keys.Sum(f => Mathf.Max(1, C.Seats(f)));
            bool quorum = votingSeats * 2 >= totalSeats;
            UiKit.Text(new Rect(r.x + 24, r.y + 14, r.width - 300, 36), "COUNCIL OF THE SUCCESSION", UiKit.Face.Stencil, 30, UiKit.Amber);
            UiKit.Text(new Rect(r.x + 26, r.y + 52, r.width - 300, 18),
                $"SESSION {day / (int)Campaign.CouncilIntervalDays + 1}  ·  DAY {day}  ·  QUORUM {(quorum ? "<color=#6fb98a>MET</color>" : "<color=#d9533d>NOT MET — PROCEEDING REGARDLESS</color>")}",
                UiKit.Face.Mono, 12, UiKit.Mute);
            UiKit.Stamp(new Rect(r.xMax - 190, r.y + 20, 160, 32), "Provisional", UiKit.Red, -2f, 16);
            UiKit.Fill(new Rect(r.x + 24, r.y + 78, r.width - 48, 2), UiKit.AmberDim);

            float top = r.y + 94, bottom = r.yMax - 16;
            float leftW = Mathf.Min(520f, r.width * 0.46f);
            var left = new Rect(r.x + 24, top, leftW, bottom - top);
            var right = new Rect(left.xMax + 16, top, r.xMax - 24 - left.xMax - 16, bottom - top);

            Chamber(new Rect(left.x, left.y, left.width, left.height * 0.56f), s);
            Mandate(new Rect(left.x, left.y + left.height * 0.56f + 10, left.width, left.height * 0.44f - 10), voters);
            Dossier(right, s, brief);
        }

        Texture2D MakeDisc()
        {
            const int n = 32;
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Bilinear };
            float c = (n - 1) * 0.5f;
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                t.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01((1f - d) * n * 0.3f)));
            }
            t.Apply();
            return t;
        }

        // The chamber: seats on a semicircle, the empty ones dashed, the Chair on its dais.
        void Chamber(Rect r, CouncilSession s)
        {
            var occupied = C.Factions.Where(f => !f.Eliminated).Select(f => f.Id).ToList();
            int seatsTotal = Mathf.Max(13, occupied.Count + 3);
            UiKit.Plate(r, $"The chamber — {seatsTotal} seats, {occupied.Count} occupied");
            float cx = r.center.x, cy = r.yMax - 52, rx = r.width * 0.40f, ry = r.height - 118f;
            UiKit.Fill(new Rect(cx - 52, cy - 14, 104, 30), UiKit.Panel2);
            UiKit.Outline(new Rect(cx - 52, cy - 14, 104, 30), UiKit.Line2);
            UiKit.Text(new Rect(cx - 52, cy - 14, 104, 30), "THE CHAIR", UiKit.Face.Stencil, 12, UiKit.Amber, TextAnchor.MiddleCenter);
            UiKit.Text(new Rect(cx - 120, cy + 20, 240, 14), "SPEAKER ABSENT SINCE DAY 0", UiKit.Face.Mono, 9, UiKit.Mute, TextAnchor.MiddleCenter);

            for (int i = 0; i < seatsTotal; i++)
            {
                float a = Mathf.PI * (0.08f + 0.84f * i / (seatsTotal - 1));
                var pos = new Vector2(cx - Mathf.Cos(a) * rx, cy - Mathf.Sin(a) * ry);
                int fid = i < occupied.Count ? occupied[i] : -1;
                float sz = Mathf.Clamp(rx * 2.1f / seatsTotal * 1.3f, 26f, 44f);
                var seat = new Rect(pos.x - sz * 0.5f, pos.y - sz * 0.39f, sz, sz * 0.78f);
                var m = GUI.matrix;
                GUIUtility.RotateAroundPivot((a - Mathf.PI * 0.5f) * Mathf.Rad2Deg, pos);
                if (fid < 0)
                {
                    UiKit.Outline(seat, UiKit.Line, 1f);
                    GUI.matrix = m;
                    continue;
                }
                var col = FactionStyle.Of(fid);
                bool picked = _councilSeat == fid;
                UiKit.Fill(seat, new Color(0.075f, 0.09f, 0.11f));
                UiKit.Outline(seat, picked ? UiKit.Amber : col, picked ? 2f : C.Factions[fid].Major ? 2f : 1f);
                UiKit.Tex(new Rect(pos.x - sz * 0.18f, pos.y - sz * 0.22f, sz * 0.36f, sz * 0.36f), _seatDisc, col);
                bool voted = s.Votes.TryGetValue(fid, out var vote);
                bool you = fid == Campaign.PlayerFaction;
                var vc = !voted ? UiKit.Line2 : vote == Vote.Yes ? UiKit.Ok : vote == Vote.No ? UiKit.Red : UiKit.Mute;
                UiKit.Fill(new Rect(pos.x - sz * 0.18f, pos.y + sz * 0.17f, sz * 0.36f, 4), you ? UiKit.Amber : vc);
                GUI.matrix = m;
                string name = C.Factions[fid].Name.Split(' ')[0].ToUpperInvariant();
                var outward = new Vector2(-Mathf.Cos(a), -Mathf.Sin(a));
                var lp = pos + outward * (sz * 0.5f + 12f);
                UiKit.Text(new Rect(lp.x - 40, lp.y - 6, 80, 12), you ? "YOU" : name, UiKit.Face.Mono, 8, you ? UiKit.Amber : UiKit.Mute, TextAnchor.MiddleCenter);
                var hit = new Rect(pos.x - 26, pos.y - 20, 52, 42);
                var e = Event.current;
                if (e.type == EventType.MouseDown && e.button == 0 && hit.Contains(e.mousePosition))
                {
                    _councilSeat = picked ? -2 : fid;
                    e.Use();
                }
            }
            UiKit.Text(new Rect(r.x + 14, r.yMax - 18, r.width - 28, 14),
                "aye ▪ green    nay ▪ red    abstain ▪ grey    ringed: a major power", UiKit.Face.Mono, 9, UiKit.Mute);
        }

        // Recognition against control: the gap is the lie.
        void Mandate(Rect r, List<Faction> voters)
        {
            UiKit.Plate(r, "Mandate — recognition vs. control");
            float seats = Mathf.Max(1, voters.Sum(f => C.Seats(f.Id)));
            float navy = Mathf.Max(1f, voters.Sum(f => C.NavyStrength(f.Id)));
            var rows = voters.OrderByDescending(f => C.Seats(f.Id)).Take(Mathf.Max(3, Mathf.FloorToInt((r.height - 56f) / 26f))).ToList();
            float y = r.y + 36;
            float labelW = 120f, barX = r.x + 18 + labelW, barW = r.width - labelW - 80f;
            float scale = 1f / Mathf.Max(0.2f, rows.Max(f => Mathf.Max(C.Seats(f.Id) / seats, C.NavyStrength(f.Id) / navy)));
            foreach (var f in rows)
            {
                float official = C.Seats(f.Id) / seats * scale, eff = C.NavyStrength(f.Id) / navy * scale;
                var col = FactionStyle.Of(f.Id);
                UiKit.Text(new Rect(r.x + 18, y, labelW, 20), f.Name, UiKit.Face.Bold, 14, col, TextAnchor.MiddleLeft);
                var bar = new Rect(barX, y + 1, barW, 18);
                UiKit.Fill(bar, new Color(0.04f, 0.045f, 0.05f));
                UiKit.Outline(bar, UiKit.Line);
                UiKit.Fill(new Rect(bar.x, bar.y + 2, bar.width * official, 5), UiKit.AmberDim);
                UiKit.Fill(new Rect(bar.x, bar.y + 10, bar.width * eff, 5), col);
                float lo = Mathf.Min(official, eff), hi = Mathf.Max(official, eff);
                if (hi - lo > 0.04f)
                {
                    var gap = new Rect(bar.x + bar.width * lo, bar.y, bar.width * (hi - lo), bar.height);
                    UiKit.Fill(gap, new Color(0.85f, 0.33f, 0.24f, 0.22f));
                    UiKit.Fill(new Rect(gap.x, gap.y, 1, gap.height), UiKit.Red);
                    UiKit.Fill(new Rect(gap.xMax - 1, gap.y, 1, gap.height), UiKit.Red);
                }
                float diff = (eff - official) / scale * 100f;
                UiKit.Text(new Rect(bar.xMax + 6, y, 52, 20), $"{(diff >= 0 ? "+" : "−")}{Mathf.Abs(diff):0}", UiKit.Face.Mono, 11,
                    Mathf.Abs(diff) > 12f ? UiKit.Red : Mathf.Abs(diff) > 5f ? UiKit.Warn : UiKit.Mute, TextAnchor.MiddleRight);
                y += 26;
            }
            UiKit.Text(new Rect(r.x + 18, r.yMax - 18, r.width - 36, 14), "amber: council seats   coloured: fleet strength   hatched: the gap", UiKit.Face.Mono, 9, UiKit.Mute);
        }

        // The resolution file: the paper, the truth, who voted how, and the three choices.
        void Dossier(Rect r, CouncilSession s, Brief brief)
        {
            UiKit.Plate(r, "Resolution file");
            var paper = new Rect(r.x + 16, r.y + 40, r.width - 32, 150);
            UiKit.Paper(paper, 0.3f);
            var m = GUI.matrix;
            GUIUtility.RotateAroundPivot(0.3f, paper.center);
            UiKit.Text(new Rect(paper.x + 14, paper.y + 8, paper.width - 28, 20), $"RES. {C.Day / (int)Campaign.CouncilIntervalDays + 1:00}-{(char)('A' + (int)s.Kind)} — {Plain(C.Describe(s)).Split(':')[0].ToUpperInvariant()}",
                UiKit.Face.Stencil, 14, UiKit.PaperInk, TextAnchor.UpperLeft, true);
            // The official text with the lie struck out and amended by hand.
            StruckText(new Rect(paper.x + 14, paper.y + 34, paper.width - 28, 64), brief.Official, brief.Cross);
            UiKit.Text(new Rect(paper.x + 14, paper.y + 98, paper.width - 28, 26), $"<b>{brief.Hand}</b>", UiKit.Face.Bold, 19, UiKit.PaperRed, TextAnchor.UpperLeft, true);
            UiKit.Text(new Rect(paper.x + 14, paper.yMax - 22, paper.width - 28, 14), "Entered by the Secretariat of the Succession", UiKit.Face.Mono, 10, new Color(0.23f, 0.2f, 0.15f));
            GUI.matrix = m;

            // What actually happens.
            var real = new Rect(r.x + 16, paper.yMax + 22, r.width - 32, 92);
            UiKit.Fill(real, new Color(0.055f, 0.078f, 0.063f));
            UiKit.Outline(real, new Color(0.12f, 0.23f, 0.165f));
            UiKit.Fill(new Rect(real.x + 10, real.y - 8, 190, 14), UiKit.Bg);
            UiKit.Text(new Rect(real.x + 14, real.y - 9, 190, 14), UiKit.Spaced("what actually happens"), UiKit.Face.Mono, 10, UiKit.Phos);
            UiKit.Text(new Rect(real.x + 12, real.y + 10, real.width - 24, real.height - 14), brief.Reality, UiKit.Face.Mono, 12, UiKit.Phos, TextAnchor.UpperLeft, true);

            // Votes.
            float y = real.yMax + 12;
            float x = r.x + 16;
            foreach (var kv in s.Votes)
            {
                var f = C.Factions[kv.Key];
                string name = f.Name.Split(' ')[0];
                string vt = kv.Value == Vote.Yes ? "AYE" : kv.Value == Vote.No ? "NAY" : "ABST";
                var tone = kv.Value == Vote.Yes ? UiKit.Ok : kv.Value == Vote.No ? UiKit.Red : UiKit.Mute;
                var chip = new Rect(x, y, 128, 20);
                if (chip.xMax > r.xMax - 16) { x = r.x + 16; y += 24; chip.x = x; chip.y = y; }
                UiKit.Outline(chip, kv.Key == _councilSeat ? UiKit.Amber : new Color(tone.r, tone.g, tone.b, 0.45f));
                UiKit.Text(chip, $"{name} · {C.Seats(kv.Key)} · {vt}", UiKit.Face.Mono, 10, tone, TextAnchor.MiddleCenter);
                x += 134;
            }
            UiKit.Text(new Rect(r.x + 16, y + 26, r.width - 32, 16), $"your seats: {C.Seats(Campaign.PlayerFaction)}   ·   chips: power · seats · vote", UiKit.Face.Mono, 10, UiKit.Mute);

            // The choices.
            float by = r.yMax - 52, bw = (r.width - 32 - 20) / 3f;
            if (PlateButton(new Rect(r.x + 16, by, bw, 34), "Vote for", true)) { C.CastPlayerVote(Vote.Yes); Director.Flush(); }
            if (PlateButton(new Rect(r.x + 26 + bw, by, bw, 34), "Vote against", false)) { C.CastPlayerVote(Vote.No); Director.Flush(); }
            if (PlateButton(new Rect(r.x + 36 + bw * 2, by, bw, 34), "Abstain", false)) { C.CastPlayerVote(Vote.Abstain); Director.Flush(); }
        }

        GUIStyle _paperText;

        // Plain text with one phrase struck through in red: the line is laid over the glyphs, one
        // stroke per character so it follows the wrapping.
        void StruckText(Rect r, string text, string phrase)
        {
            _paperText ??= new GUIStyle(UiKit.Style(UiKit.Face.Mono, 12, UiKit.PaperInk, TextAnchor.UpperLeft, true)) { richText = false };
            GUI.Label(r, text, _paperText);
            int at = text.IndexOf(phrase, System.StringComparison.Ordinal);
            if (at < 0 || Event.current.type != EventType.Repaint) return;
            var content = new GUIContent(text);
            for (int i = at; i < at + phrase.Length; i++)
            {
                Vector2 a = _paperText.GetCursorPixelPosition(r, content, i);
                Vector2 b = _paperText.GetCursorPixelPosition(r, content, i + 1);
                if (Mathf.Abs(a.y - b.y) > 2f) continue;   // the stroke that wraps to the next line
                UiKit.Fill(new Rect(a.x, a.y + _paperText.lineHeight * 0.55f, Mathf.Max(1f, b.x - a.x), 2f), UiKit.PaperRed);
            }
        }
    }
}
