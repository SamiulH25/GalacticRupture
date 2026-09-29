using System.Collections.Generic;
using System.Numerics;

namespace GalacticRupture.Sim.Strategic
{
    // A ship that persists between battles: damage stays until a yard repairs it (IDEAS.md §2.2).
    public sealed class ShipRecord
    {
        public readonly HullDef Def;
        public readonly string Name;
        public float Hull;
        public readonly bool[] MountAlive;

        public ShipRecord(HullDef def, string name)
        {
            Def = def;
            Name = name;
            Hull = def.HullHp;
            MountAlive = new bool[def.Hardpoints.Count];
            for (int i = 0; i < MountAlive.Length; i++) MountAlive[i] = true;
        }

        public float HullFraction => Hull / Def.HullHp;

        public int MountsAlive
        {
            get
            {
                int n = 0;
                foreach (bool a in MountAlive) if (a) n++;
                return n;
            }
        }

        // Rough fighting value, for the AI and the battle forecast.
        public float Strength => Def.Cost * (0.35f + 0.65f * HullFraction) * (0.3f + 0.7f * MountsAlive / (float)System.Math.Max(1, MountAlive.Length));
    }

    public sealed class Fleet
    {
        public int Id;
        public int Owner;
        public string Name = "";
        public readonly List<ShipRecord> Ships = new List<ShipRecord>();

        public int System;              // where it is, or the system it left
        public int Next = -1;           // the system it is flying to, -1 when stationary
        public float Progress;          // light-years flown along the current lane
        public readonly List<int> Route = new List<int>(); // systems after Next
        public double ArrivedHour;

        public bool InTransit => Next >= 0;
        public bool Alive => Ships.Count > 0;
        public int Destination => Route.Count > 0 ? Route[Route.Count - 1] : Next >= 0 ? Next : System;

        public float LyPerDay
        {
            get
            {
                float v = float.MaxValue;
                foreach (var s in Ships) v = global::System.MathF.Min(v, s.Def.LyPerDay);
                return v == float.MaxValue ? 0f : v;
            }
        }

        public float Strength
        {
            get
            {
                float s = 0f;
                foreach (var r in Ships) s += r.Strength;
                return s;
            }
        }
    }

    public sealed class Faction
    {
        public int Id;
        public string Name = "";
        public float Credits;
        public float SupplyRange = 14f; // light-years from the nearest owned system
        public string ShipPrefix = "";
        public int ShipsBuilt;
    }

    public sealed class BuildItem
    {
        public HullDef Def;
        public float HoursLeft;
        public int Owner;
    }

    public sealed class Engagement
    {
        public int System;
        public int Attacker;            // the faction that arrived last
        public readonly List<int> Fleets = new List<int>();
    }

    public enum CampaignState
    {
        Ongoing,
        Victory,
        Defeat,
    }

    public enum CampaignEventKind
    {
        FleetArrived,
        BattleStarted,
        BattleResolved,
        SystemCaptured,
        ShipBuilt,
        SiegeStarted,
    }

    public struct CampaignEvent
    {
        public CampaignEventKind Kind;
        public int System;
        public int Fleet;
        public int Faction;
        public string Text;
    }

    // The strategic layer: an hourly tick over systems, fleets, yards and supply. When hostile
    // fleets meet, time stops until the battle is fought (attended or not) and written back.
    public sealed partial class Campaign
    {
        public const int PlayerFaction = 0;
        public const int EnemyFaction = 1;

        public readonly Galaxy Galaxy = new Galaxy();
        public readonly Faction[] Factions = new Faction[2];
        public readonly List<Fleet> Fleets = new List<Fleet>();
        public readonly Dictionary<int, List<BuildItem>> Yards = new Dictionary<int, List<BuildItem>>();
        public readonly List<CampaignEvent> Events = new List<CampaignEvent>();

        public double Hours { get; private set; }
        public Engagement Pending { get; private set; }
        public CampaignState State { get; private set; }

        Rng _rng;
        int _nextFleetId;

        public Campaign(ulong seed) => _rng = new Rng(seed);

        public ref Rng Rng => ref _rng;

        public static float IncomePerDay(StarSystem s) => s.Tier switch { 3 => 110f, 2 => 50f, _ => 22f };

        public Fleet FleetById(int id)
        {
            foreach (var f in Fleets)
                if (f.Id == id) return f;
            return null;
        }

        public Fleet NewFleet(int owner, int system, string name)
        {
            var f = new Fleet { Id = _nextFleetId++, Owner = owner, System = system, Name = name, ArrivedHour = Hours };
            Fleets.Add(f);
            return f;
        }

        public ShipRecord NewShip(int owner, HullDef def)
        {
            var fac = Factions[owner];
            fac.ShipsBuilt++;
            return new ShipRecord(def, $"{fac.ShipPrefix} {100 + fac.ShipsBuilt}");
        }

        // One hour.
        public void Tick()
        {
            if (Pending != null || State != CampaignState.Ongoing) return;
            Hours += 1.0;
            MoveFleets();
            Economy();
            BuildShips();
            Repair();
            Sieges();
            DetectEngagement();
            if (((int)Hours) % 6 == 0) CampaignAi.Think(this, EnemyFaction);
        }

        // --- Orders ---------------------------------------------------------------------------

        public bool MoveFleet(int fleetId, int to)
        {
            var f = FleetById(fleetId);
            if (f == null || !f.Alive) return false;
            f.Route.Clear();
            if (f.InTransit)
            {
                // Finish the current jump; the rest of the route starts from there.
                if (f.Next != to) f.Route.AddRange(Galaxy.Route(f.Next, to));
                return true;
            }
            if (to == f.System) return true;
            var route = Galaxy.Route(f.System, to);
            if (route.Count == 0) return false;
            f.Next = route[0];
            f.Progress = 0f;
            route.RemoveAt(0);
            f.Route.AddRange(route);
            return true;
        }

        public bool CanBuild(int owner, int system, HullDef def)
        {
            var s = Galaxy.Systems[system];
            return s.Owner == owner && s.Shipyard && Factions[owner].Credits >= def.Cost;
        }

        public bool QueueBuild(int owner, int system, HullDef def)
        {
            if (!CanBuild(owner, system, def)) return false;
            Factions[owner].Credits -= def.Cost;
            if (!Yards.TryGetValue(system, out var q)) Yards[system] = q = new List<BuildItem>();
            q.Add(new BuildItem { Def = def, HoursLeft = def.BuildDays * 24f, Owner = owner });
            return true;
        }

        // Moves ships between two fleets at the same system (merging when `ships` covers all).
        public Fleet Split(int fleetId, IList<int> shipIndices, string name)
        {
            var f = FleetById(fleetId);
            if (f == null || f.InTransit || shipIndices.Count == 0 || shipIndices.Count >= f.Ships.Count) return null;
            var nf = NewFleet(f.Owner, f.System, name);
            var sorted = new List<int>(shipIndices);
            sorted.Sort();
            for (int i = sorted.Count - 1; i >= 0; i--)
            {
                nf.Ships.Insert(0, f.Ships[sorted[i]]);
                f.Ships.RemoveAt(sorted[i]);
            }
            return nf;
        }

        public bool Merge(int intoId, int fromId)
        {
            var into = FleetById(intoId);
            var from = FleetById(fromId);
            if (into == null || from == null || into == from || into.Owner != from.Owner) return false;
            if (into.InTransit || from.InTransit || into.System != from.System) return false;
            into.Ships.AddRange(from.Ships);
            from.Ships.Clear();
            Fleets.Remove(from);
            return true;
        }

        // --- Hourly systems ---------------------------------------------------------------------

        void MoveFleets()
        {
            foreach (var f in Fleets)
            {
                if (!f.InTransit || !f.Alive) continue;
                float len = Galaxy.LaneLength(f.System, f.Next);
                f.Progress += f.LyPerDay / 24f;
                if (f.Progress < len) continue;

                f.System = f.Next;
                f.Progress = 0f;
                f.ArrivedHour = Hours;
                if (f.Route.Count > 0)
                {
                    f.Next = f.Route[0];
                    f.Route.RemoveAt(0);
                }
                else
                {
                    f.Next = -1;
                    Events.Add(new CampaignEvent { Kind = CampaignEventKind.FleetArrived, Fleet = f.Id, System = f.System, Faction = f.Owner });
                }
            }
        }

        void Economy()
        {
            foreach (var s in Galaxy.Systems)
            {
                if (s.Owner < 0) continue;
                if (s.Unrest > 0f)
                {
                    s.Unrest -= 1f;
                    continue;
                }
                Factions[s.Owner].Credits += IncomePerDay(s) / 24f;
            }
        }

        void BuildShips()
        {
            foreach (var kv in Yards)
            {
                var sys = Galaxy.Systems[kv.Key];
                var q = kv.Value;
                if (q.Count == 0 || sys.Unrest > 0f) continue;
                int lanes = sys.Tier >= 3 ? 2 : 1;
                for (int i = 0; i < System.Math.Min(lanes, q.Count); i++) q[i].HoursLeft -= 1f;
                for (int i = q.Count - 1; i >= 0; i--)
                {
                    if (q[i].HoursLeft > 0f) continue;
                    var item = q[i];
                    q.RemoveAt(i);
                    var fleet = HomeFleet(item.Owner, sys.Id) ?? NewFleet(item.Owner, sys.Id, $"{sys.Name} Squadron");
                    var ship = NewShip(item.Owner, item.Def);
                    fleet.Ships.Add(ship);
                    Events.Add(new CampaignEvent { Kind = CampaignEventKind.ShipBuilt, System = sys.Id, Fleet = fleet.Id, Faction = item.Owner, Text = ship.Name });
                }
            }
        }

        Fleet HomeFleet(int owner, int system)
        {
            foreach (var f in Fleets)
                if (f.Owner == owner && f.Alive && !f.InTransit && f.System == system && f.Route.Count == 0) return f;
            return null;
        }

        public bool InSupply(Fleet f)
        {
            Vector3 p = FleetPosition(f);
            float range = Factions[f.Owner].SupplyRange;
            foreach (var s in Galaxy.Systems)
                if (s.Owner == f.Owner && Vector3.Distance(s.Position, p) <= range) return true;
            return false;
        }

        public Vector3 FleetPosition(Fleet f)
        {
            var a = Galaxy.Systems[f.System].Position;
            if (!f.InTransit) return a;
            var b = Galaxy.Systems[f.Next].Position;
            float len = System.MathF.Max(Vector3.Distance(a, b), 0.001f);
            return Vector3.Lerp(a, b, System.MathF.Min(f.Progress / len, 1f));
        }

        // Yards repair hulls and replace lost turrets; out of supply, nothing is repaired.
        void Repair()
        {
            foreach (var f in Fleets)
            {
                if (f.InTransit || !f.Alive) continue;
                var s = Galaxy.Systems[f.System];
                if (s.Owner != f.Owner || !s.Shipyard || s.Unrest > 0f) continue;
                foreach (var r in f.Ships)
                {
                    r.Hull = System.MathF.Min(r.Def.HullHp, r.Hull + r.Def.HullHp * 0.006f);
                    if (r.HullFraction >= 0.6f && ((int)Hours) % 12 == 0)
                    {
                        for (int i = 0; i < r.MountAlive.Length; i++)
                        {
                            if (r.MountAlive[i]) continue;
                            r.MountAlive[i] = true;
                            break;
                        }
                    }
                }
            }
        }

        // An enemy fleet sitting in a system with no defenders takes it over time.
        void Sieges()
        {
            foreach (var s in Galaxy.Systems)
            {
                int besieger = -1;
                bool defended = false;
                foreach (var f in Fleets)
                {
                    if (!f.Alive || f.InTransit || f.System != s.Id) continue;
                    if (f.Owner == s.Owner) defended = true;
                    else besieger = f.Owner;
                }
                if (besieger < 0 || defended)
                {
                    s.Siege = System.MathF.Max(0f, s.Siege - 1f / 24f);
                    if (s.Siege <= 0f) s.Besieger = -1;
                    continue;
                }
                if (s.Besieger != besieger)
                {
                    s.Besieger = besieger;
                    s.Siege = 0f;
                    Events.Add(new CampaignEvent { Kind = CampaignEventKind.SiegeStarted, System = s.Id, Faction = besieger });
                }
                float hoursToTake = s.Owner < 0 ? 18f : s.Capital ? 96f : 36f + 12f * s.Tier;
                s.Siege += 1f / hoursToTake;
                if (s.Siege < 1f) continue;

                int loser = s.Owner;
                s.Owner = besieger;
                s.Siege = 0f;
                s.Besieger = -1;
                s.Unrest = 72f;
                // Half-built hulls in a captured yard are lost; captured systems degrade, never raze.
                if (Yards.TryGetValue(s.Id, out var q)) q.Clear();
                Events.Add(new CampaignEvent { Kind = CampaignEventKind.SystemCaptured, System = s.Id, Faction = besieger });
                if (s.Capital && loser >= 0)
                    State = loser == PlayerFaction ? CampaignState.Defeat : CampaignState.Victory;
            }
        }

        void DetectEngagement()
        {
            foreach (var s in Galaxy.Systems)
            {
                bool player = false, enemy = false;
                foreach (var f in Fleets)
                {
                    if (!f.Alive || f.InTransit || f.System != s.Id) continue;
                    if (f.Owner == PlayerFaction) player = true;
                    else enemy = true;
                }
                if (!player || !enemy) continue;

                var e = new Engagement { System = s.Id };
                double latest = -1;
                foreach (var f in Fleets)
                {
                    if (!f.Alive || f.InTransit || f.System != s.Id) continue;
                    e.Fleets.Add(f.Id);
                    // Stop there: nobody slips past a battle.
                    f.Route.Clear();
                    if (f.ArrivedHour > latest)
                    {
                        latest = f.ArrivedHour;
                        e.Attacker = f.Owner;
                    }
                }
                Pending = e;
                Events.Add(new CampaignEvent { Kind = CampaignEventKind.BattleStarted, System = s.Id, Faction = e.Attacker });
                return;
            }
        }

        // Called once a battle is over and written back into the fleets.
        public void EndEngagement(int retreatingFaction)
        {
            var e = Pending;
            if (e == null) return;
            Fleets.RemoveAll(f => !f.Alive);
            if (retreatingFaction >= 0)
            {
                foreach (int id in e.Fleets)
                {
                    var f = FleetById(id);
                    if (f == null || f.Owner != retreatingFaction) continue;
                    int to = RetreatTarget(f);
                    if (to >= 0)
                    {
                        f.Next = to;
                        f.Progress = 0f;
                        f.Route.Clear();
                    }
                }
            }
            Pending = null;
            Events.Add(new CampaignEvent { Kind = CampaignEventKind.BattleResolved, System = e.System, Faction = retreatingFaction });
        }

        // Back along the lanes to the nearest friendly system, or any neighbour.
        int RetreatTarget(Fleet f)
        {
            int best = -1;
            float bestLen = float.MaxValue;
            foreach (var l in Galaxy.LanesFrom(f.System))
            {
                int n = l.Other(f.System);
                float score = l.Length + (Galaxy.Systems[n].Owner == f.Owner ? 0f : 100f);
                if (score < bestLen)
                {
                    bestLen = score;
                    best = n;
                }
            }
            return best;
        }

        public float Strength(int faction, int system)
        {
            float s = 0f;
            foreach (var f in Fleets)
                if (f.Alive && f.Owner == faction && !f.InTransit && f.System == system) s += f.Strength;
            return s;
        }
    }
}
