using System.Collections.Generic;

namespace GalacticRupture.Sim.Strategic
{
    public enum DiplomaticStance
    {
        War,
        Peace,
        NonAggression,
        Alliance,
    }

    public sealed class Proposal
    {
        public int From;
        public int To;
        public DiplomaticStance Treaty;
        public double Expires;
    }

    public enum ResolutionKind
    {
        Censure,     // embargo the aggressor for a season
        Ceasefire,   // every war stops for a few days
        Recognise,   // holdings taken by force are legitimised: unrest ends
        Embargo,     // tabled by a member against a named faction
    }

    public enum Vote
    {
        Abstain,
        Yes,
        No,
    }

    // The council: seats, votes, rules, almost no power (IDEAS.md §2.3, §7). It passes
    // resolutions that bite at the edges: an embargo, a ceasefire, legitimacy for a conquest.
    public sealed class CouncilSession
    {
        public ResolutionKind Kind;
        public int Target = -1;
        public int TabledBy = -1;
        public readonly Dictionary<int, Vote> Votes = new Dictionary<int, Vote>();
        public bool Passed;
        public int YesSeats, NoSeats;
    }

    public sealed partial class Campaign
    {
        public const float CouncilIntervalDays = 20f;
        public const int PeaceCost = 15, NonAggressionCost = 20, AllianceCost = 40, TableCost = 40;

        DiplomaticStance[,] _stance;
        float[,] _trust;
        double[,] _since;                       // hour the current stance began
        readonly float[] _aggression = new float[8]; // recent conquests and declarations, decays

        public readonly List<Proposal> Incoming = new List<Proposal>(); // proposals awaiting the player
        public CouncilSession Council { get; private set; }             // session awaiting the player's vote
        public CouncilSession LastCouncil { get; private set; }
        public float CeasefireHours { get; private set; }
        ResolutionKind? _tabled;
        int _tabledTarget = -1;
        int _tabledBy = -1;

        void InitDiplomacy()
        {
            int n = Factions.Count;
            _stance = new DiplomaticStance[n, n];
            _trust = new float[n, n];
            _since = new double[n, n];
            for (int a = 0; a < n; a++)
            for (int b = 0; b < n; b++)
                _stance[a, b] = a == b ? DiplomaticStance.Alliance : DiplomaticStance.Peace;
        }

        public DiplomaticStance Stance(int a, int b)
        {
            if (a == b) return DiplomaticStance.Alliance;
            if (a < 0 || b < 0) return DiplomaticStance.War;
            return _stance[a, b];
        }

        public bool Hostile(int a, int b) => a != b && Stance(a, b) == DiplomaticStance.War && CeasefireHours <= 0f;

        public float Trust(int a, int b) => a == b ? 100f : _trust[a, b];

        public float DaysAtStance(int a, int b) => (float)((Hours - _since[a, b]) / 24.0);

        public void ChangeTrust(int a, int b, float delta)
        {
            if (a < 0 || b < 0 || a == b) return;
            float v = System.MathF.Max(-100f, System.MathF.Min(100f, _trust[a, b] + delta));
            _trust[a, b] = _trust[b, a] = v;
        }

        public void SetStance(int a, int b, DiplomaticStance s)
        {
            _stance[a, b] = _stance[b, a] = s;
            _since[a, b] = _since[b, a] = Hours;
        }

        public void DeclareWar(int a, int b)
        {
            if (a == b || Stance(a, b) == DiplomaticStance.War) return;
            var broken = Stance(a, b);
            if (broken == DiplomaticStance.NonAggression || broken == DiplomaticStance.Alliance)
            {
                // Reputation is global: breaking a treaty is priced by everyone at once.
                foreach (var f in Factions)
                    if (f.Id != a) ChangeTrust(a, f.Id, -15f);
                Emit(CampaignEventKind.TreatyBroken, faction: a, other: b);
            }
            SetStance(a, b, DiplomaticStance.War);
            ChangeTrust(a, b, -30f);
            _aggression[a] += 1f;
            Emit(CampaignEventKind.WarDeclared, faction: a, other: b);
            // Alliances carry obligations: b's allies join in.
            foreach (var f in Factions)
            {
                if (f.Id == a || f.Id == b || f.Eliminated) continue;
                if (Stance(f.Id, b) == DiplomaticStance.Alliance && Stance(f.Id, a) != DiplomaticStance.Alliance && Stance(f.Id, a) != DiplomaticStance.War)
                {
                    SetStance(f.Id, a, DiplomaticStance.War);
                    Emit(CampaignEventKind.WarDeclared, faction: f.Id, other: a);
                }
            }
        }

        public static int Cost(DiplomaticStance treaty) => treaty switch
        {
            DiplomaticStance.Peace => PeaceCost,
            DiplomaticStance.NonAggression => NonAggressionCost,
            DiplomaticStance.Alliance => AllianceCost,
            _ => 0,
        };

        public bool CanPropose(int from, int to, DiplomaticStance treaty)
        {
            if (from == to || Factions[to].Eliminated || Factions[from].Influence < Cost(treaty)) return false;
            var s = Stance(from, to);
            return treaty switch
            {
                DiplomaticStance.Peace => s == DiplomaticStance.War,
                DiplomaticStance.NonAggression => s == DiplomaticStance.Peace,
                DiplomaticStance.Alliance => s == DiplomaticStance.Peace || s == DiplomaticStance.NonAggression,
                _ => false,
            };
        }

        // An offer to an AI faction: answered at once. Returns whether it was accepted.
        public bool Propose(int from, int to, DiplomaticStance treaty)
        {
            if (!CanPropose(from, to, treaty)) return false;
            Factions[from].Influence -= Cost(treaty);
            bool yes = WillAccept(to, from, treaty);
            if (yes) Sign(from, to, treaty);
            else ChangeTrust(from, to, -2f);
            return yes;
        }

        void Sign(int a, int b, DiplomaticStance treaty)
        {
            SetStance(a, b, treaty);
            ChangeTrust(a, b, treaty == DiplomaticStance.Alliance ? 15f : 8f);
            Emit(CampaignEventKind.TreatySigned, faction: a, other: b, text: treaty.ToString());
            if (treaty != DiplomaticStance.Alliance) return;
            // An alliance brings you into your ally's wars.
            foreach (var f in Factions)
            {
                if (f.Eliminated || f.Id == a || f.Id == b) continue;
                if (Stance(b, f.Id) == DiplomaticStance.War && Stance(a, f.Id) == DiplomaticStance.Peace) DeclareWar(a, f.Id);
                if (Stance(a, f.Id) == DiplomaticStance.War && Stance(b, f.Id) == DiplomaticStance.Peace) DeclareWar(b, f.Id);
            }
        }

        public float NavyStrength(int faction)
        {
            float s = 0f;
            foreach (var f in Fleets) if (f.Alive && f.Owner == faction) s += f.Strength;
            return s;
        }

        bool SharedEnemy(int a, int b)
        {
            foreach (var f in Factions)
                if (!f.Eliminated && f.Id != a && f.Id != b && Stance(a, f.Id) == DiplomaticStance.War && Stance(b, f.Id) == DiplomaticStance.War)
                    return true;
            return false;
        }

        // How an AI weighs an offer: trust, relative strength, a shared enemy, war weariness.
        public bool WillAccept(int who, int from, DiplomaticStance treaty)
        {
            float trust = Trust(who, from);
            float mine = NavyStrength(who) + 1f, theirs = NavyStrength(from) + 1f;
            switch (treaty)
            {
                case DiplomaticStance.Peace:
                {
                    float score = trust * 0.5f + (theirs / mine - 1f) * 40f + DaysAtStance(who, from) * 0.4f;
                    return score > 20f;
                }
                case DiplomaticStance.NonAggression:
                    return trust + (SharedEnemy(who, from) ? 20f : 0f) + (theirs > mine ? 10f : 0f) > 5f;
                case DiplomaticStance.Alliance:
                    return trust + (SharedEnemy(who, from) ? 30f : 0f) > 45f;
            }
            return false;
        }

        public void Gift(int from, int to, float credits)
        {
            if (Factions[from].Credits < credits || from == to) return;
            Factions[from].Credits -= credits;
            Factions[to].Credits += credits;
            ChangeTrust(from, to, credits / 12f);
        }

        public void Respond(Proposal p, bool accept)
        {
            Incoming.Remove(p);
            if (accept && Stance(p.From, p.To) != DiplomaticStance.Alliance) Sign(p.From, p.To, p.Treaty);
            else ChangeTrust(p.From, p.To, -3f);
        }

        void DiplomacyTick()
        {
            if (CeasefireHours > 0f) CeasefireHours -= 1f;
            Incoming.RemoveAll(p => p.Expires < Hours);
            for (int i = 0; i < _aggression.Length; i++) _aggression[i] *= 0.9993f;

            int h = (int)Hours;
            if (h % 24 == 0) DailyTrust();
            if (h % 120 == 60 && AiEnabled) AiDiplomacy();
            if (h > 24 * 5 && h % (int)(CouncilIntervalDays * 24f) == 0) OpenCouncil();
        }

        void DailyTrust()
        {
            int n = Factions.Count;
            for (int a = 0; a < n; a++)
            for (int b = a + 1; b < n; b++)
            {
                if (Factions[a].Eliminated || Factions[b].Eliminated) continue;
                var s = Stance(a, b);
                float d = 0f;
                if (s == DiplomaticStance.War) d -= 1f;
                if (s == DiplomaticStance.Alliance) d += 0.5f;
                if (SharedEnemy(a, b)) d += 1f;
                if (s != DiplomaticStance.Alliance && Borders(a, b))
                {
                    // An aggressive neighbour is a worse neighbour.
                    d -= 0.2f + 0.45f * (System.MathF.Min(_aggression[a], 4f) + System.MathF.Min(_aggression[b], 4f));
                }
                // A common threat: both border someone who has been taking systems.
                for (int t = 0; t < n; t++)
                    if (t != a && t != b && !Factions[t].Eliminated && _aggression[t] > 0.8f && Borders(a, t) && Borders(b, t))
                    {
                        d += 0.8f;
                        break;
                    }
                ChangeTrust(a, b, d);
            }
        }

        bool Borders(int a, int b)
        {
            foreach (var l in Galaxy.Lanes)
            {
                int oa = Galaxy.Systems[l.A].Owner, ob = Galaxy.Systems[l.B].Owner;
                if ((oa == a && ob == b) || (oa == b && ob == a)) return true;
            }
            return false;
        }

        // AI factions court, threaten and settle with each other and with the player.
        void AiDiplomacy()
        {
            for (int who = 1; who < Factions.Count; who++)
            {
                if (Factions[who].Eliminated) continue;
                float mine = NavyStrength(who) + 1f;
                for (int other = 0; other < Factions.Count; other++)
                {
                    if (other == who || Factions[other].Eliminated) continue;
                    var s = Stance(who, other);
                    float theirs = NavyStrength(other) + 1f;
                    float trust = Trust(who, other);

                    DiplomaticStance? offer = null;
                    if (s == DiplomaticStance.War && (mine < theirs * 0.6f || DaysAtStance(who, other) > 80f) && trust > -60f)
                        offer = DiplomaticStance.Peace;
                    else if (s == DiplomaticStance.Peace && (trust > 10f || SharedEnemy(who, other)))
                        offer = DiplomaticStance.NonAggression;
                    else if ((s == DiplomaticStance.NonAggression || s == DiplomaticStance.Peace) && trust > 35f)
                        offer = DiplomaticStance.Alliance;
                    else if (s == DiplomaticStance.Peace && trust < -25f && mine > theirs * 1.6f && Borders(who, other))
                    {
                        DeclareWar(who, other);
                        continue;
                    }

                    if (offer == null) continue;
                    if (other == PlayerFaction)
                    {
                        if (!Incoming.Exists(p => p.From == who))
                            Incoming.Add(new Proposal { From = who, To = PlayerFaction, Treaty = offer.Value, Expires = Hours + 72 });
                    }
                    else if (WillAccept(other, who, offer.Value))
                    {
                        Sign(who, other, offer.Value);
                    }
                }
            }
        }

        // --- Council ---------------------------------------------------------------------------

        public int Seats(int faction)
        {
            int n = 0;
            foreach (var s in Galaxy.Systems) if (s.Owner == faction) n++;
            return n;
        }

        public bool CanTable(int faction) => Factions[faction].Influence >= TableCost && _tabled == null;

        // A member tables an embargo for the next session.
        public bool TableEmbargo(int faction, int target)
        {
            if (!CanTable(faction) || target == faction || Factions[target].Eliminated) return false;
            Factions[faction].Influence -= TableCost;
            _tabled = ResolutionKind.Embargo;
            _tabledTarget = target;
            _tabledBy = faction;
            return true;
        }

        void OpenCouncil()
        {
            var session = new CouncilSession();
            if (_tabled != null)
            {
                session.Kind = _tabled.Value;
                session.Target = _tabledTarget;
                session.TabledBy = _tabledBy;
                _tabled = null;
            }
            else
            {
                int aggressor = -1;
                float worst = 1.5f;
                for (int i = 0; i < Factions.Count; i++)
                    if (!Factions[i].Eliminated && _aggression[i] > worst)
                    {
                        worst = _aggression[i];
                        aggressor = i;
                    }
                float roll = _rng.NextFloat();
                if (aggressor >= 0 && roll < 0.5f)
                {
                    session.Kind = ResolutionKind.Censure;
                    session.Target = aggressor;
                }
                else session.Kind = roll < 0.8f ? ResolutionKind.Ceasefire : ResolutionKind.Recognise;
            }
            foreach (var f in Factions)
                if (!f.Eliminated && f.Id != PlayerFaction) session.Votes[f.Id] = AiVote(f.Id, session);
            Council = session;
            Emit(CampaignEventKind.CouncilSession, faction: session.Target, text: Describe(session));
        }

        public string Describe(CouncilSession s) => s.Kind switch
        {
            ResolutionKind.Censure => $"Censure the {Factions[s.Target].Name} for aggression: an embargo of twenty days",
            ResolutionKind.Embargo => $"Embargo the {Factions[s.Target].Name}, tabled by the {Factions[s.TabledBy].Name}: twenty days",
            ResolutionKind.Ceasefire => "A general ceasefire of eight days",
            _ => "Recognise holdings taken by force: unrest ends in every captured system",
        };

        Vote AiVote(int who, CouncilSession s)
        {
            switch (s.Kind)
            {
                case ResolutionKind.Censure:
                case ResolutionKind.Embargo:
                    if (s.Target == who || Stance(who, s.Target) == DiplomaticStance.Alliance) return Vote.No;
                    if (Stance(who, s.Target) == DiplomaticStance.War || Trust(who, s.Target) < -10f) return Vote.Yes;
                    return Trust(who, s.Target) > 20f ? Vote.No : Vote.Abstain;
                case ResolutionKind.Ceasefire:
                {
                    float mine = NavyStrength(who) + 1f, foes = 1f;
                    bool atWar = false;
                    foreach (var f in Factions)
                        if (!f.Eliminated && Stance(who, f.Id) == DiplomaticStance.War) { atWar = true; foes += NavyStrength(f.Id); }
                    if (!atWar) return Vote.Yes;
                    return mine < foes ? Vote.Yes : Vote.No;
                }
                default:
                {
                    int held = 0;
                    foreach (var sys in Galaxy.Systems) if (sys.Owner == who && sys.Unrest > 0f) held++;
                    return held > 0 ? Vote.Yes : Vote.Abstain;
                }
            }
        }

        public void CastPlayerVote(Vote v)
        {
            var s = Council;
            if (s == null) return;
            s.Votes[PlayerFaction] = v;
            foreach (var kv in s.Votes)
            {
                int seats = System.Math.Max(1, Seats(kv.Key));
                if (Factions[kv.Key].Eliminated) continue;
                if (kv.Value == Vote.Yes) s.YesSeats += seats;
                else if (kv.Value == Vote.No) s.NoSeats += seats;
            }
            s.Passed = s.YesSeats > s.NoSeats;
            if (s.Passed)
            {
                switch (s.Kind)
                {
                    case ResolutionKind.Censure:
                    case ResolutionKind.Embargo:
                        Factions[s.Target].EmbargoHours = 20 * 24;
                        _aggression[s.Target] = 0f;
                        break;
                    case ResolutionKind.Ceasefire:
                        CeasefireHours = 8 * 24;
                        break;
                    case ResolutionKind.Recognise:
                        foreach (var sys in Galaxy.Systems) sys.Unrest = 0f;
                        break;
                }
            }
            // Votes are remembered.
            if (s.Target >= 0)
                foreach (var kv in s.Votes)
                    if (kv.Value == Vote.Yes) ChangeTrust(kv.Key, s.Target, -6f);
                    else if (kv.Value == Vote.No) ChangeTrust(kv.Key, s.Target, 4f);
            LastCouncil = s;
            Council = null;
            Emit(CampaignEventKind.CouncilResult, faction: s.Target, text: Describe(s));
        }
    }
}
