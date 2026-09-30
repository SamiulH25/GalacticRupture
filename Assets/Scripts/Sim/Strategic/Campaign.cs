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

        // Rough fighting value, for the AI and the battle forecast. Transports count for little.
        public float Strength => (Def.Class == HullClass.Transport ? 0.2f : 1f) * Def.Cost
            * (0.35f + 0.65f * HullFraction) * (0.3f + 0.7f * MountsAlive / (float)System.Math.Max(1, MountAlive.Length));
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
        public bool Held;               // stopped in a lane by an engagement
        public Admiral Admiral;         // commands when the battle is left to the fleet
        public bool Expedition;         // AI role: reclaims and courts, doesn't join the line

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

        public int TroopCapacity
        {
            get
            {
                int n = 0;
                foreach (var r in Ships) n += r.Def.TroopCapacity;
                return n;
            }
        }
    }

    public sealed class Faction
    {
        public int Id;
        public string Name = "";
        public bool Major;
        public float Credits;
        public float Influence;
        public float SupplyRange = 14f; // light-years from the nearest owned system
        public string ShipPrefix = "";
        public int ShipsBuilt;
        public int Capital = -1;
        public bool Eliminated;
        public float EmbargoHours;      // council sanction: income cut while > 0
        public readonly List<ShipDesign> Designs = new List<ShipDesign>();

        public ShipDesign Design(string name)
        {
            foreach (var d in Designs) if (d.Name == name) return d;
            return null;
        }
    }

    public sealed class BuildItem
    {
        public HullDef Def;             // null: an army
        public float HoursLeft;
        public int Owner;
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
        ArmyRaised,
        LandingStarted,
        LandingFailed,
        WarDeclared,
        TreatySigned,
        TreatyBroken,
        CouncilSession,
        CouncilResult,
        FactionEliminated,
        Interception,
        BuildingCompleted,
        GovernorAppointed,
        GovernorDefected,
        NotableOffered,
        ReclamationStarted,
        SystemReclaimed,
        CourtingStarted,
        HoldoutJoined,
        ProposalReceived,
        ActBegan,
    }

    public struct CampaignEvent
    {
        public CampaignEventKind Kind;
        public int System;
        public int Fleet;
        public int Faction;
        public int Other;
        public string Text;
    }

    // The strategic layer: an hourly tick over systems, fleets, armies, yards, supply,
    // intelligence and diplomacy. When the player's fleets meet a hostile force, time stops
    // until the battle is fought (attended or not) and written back; battles elsewhere resolve
    // unattended through the same tactical sim.
    public sealed partial class Campaign
    {
        public const int PlayerFaction = 0;
        public const int EnemyFaction = 1;

        public readonly Galaxy Galaxy = new Galaxy();
        public readonly List<Faction> Factions = new List<Faction>();
        public readonly List<Fleet> Fleets = new List<Fleet>();
        public readonly SortedDictionary<int, List<BuildItem>> Yards = new SortedDictionary<int, List<BuildItem>>(); // sorted: build order is deterministic
        public readonly List<CampaignEvent> Events = new List<CampaignEvent>();

        public double Hours { get; private set; }
        public Engagement Pending { get; private set; }
        public CampaignState State { get; private set; }
        public bool AiEnabled = true;       // off in tests that drive every faction by hand

        Rng _rng;
        int _nextFleetId;

        public Campaign(ulong seed) => _rng = new Rng(seed);

        public ref Rng Rng => ref _rng;

        public int Day => (int)(Hours / 24.0);

        public Faction AddFaction(string name, bool major, string prefix, float credits)
        {
            var f = new Faction { Id = Factions.Count, Name = name, Major = major, ShipPrefix = prefix, Credits = credits, Influence = 20f };
            foreach (var d in Hulls.StockDesigns()) f.Designs.Add(d);
            Factions.Add(f);
            return f;
        }

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

        public ShipRecord NewShip(int owner, ShipDesign design) => NewShip(owner, design.Built);

        // A faction's current design for a hull class: the first one listed.
        public ShipDesign DesignFor(int owner, HullClass cls)
        {
            foreach (var d in Factions[owner].Designs) if (d.Hull.Class == cls) return d;
            return null;
        }

        void Emit(CampaignEventKind kind, int system = -1, int faction = -1, int other = -1, int fleet = -1, string text = null) =>
            Events.Add(new CampaignEvent { Kind = kind, System = system, Faction = faction, Other = other, Fleet = fleet, Text = text });

        // Call once after building the map.
        public void Begin()
        {
            InitDiplomacy();
            InitIntel();
            foreach (var s in Galaxy.Systems)
            {
                s.Militia = MaxMilitia(s);
                if (s.Owner >= 0) s.Stability = StabilityTarget(s);
            }
            GovernorPools();
            UpdateIntel();
        }

        // One hour.
        public void Tick()
        {
            if (Pending != null || Council != null || State != CampaignState.Ongoing) return;
            Hours += 1.0;
            MoveFleets();
            if (Pending != null) return;
            Economy();
            BuildQueue();
            Repair();
            Reclamation();
            AutoLand();
            GroundWar();
            Acts();
            UpdateIntel();
            DetectEngagements();
            if (Pending != null) return;
            DiplomacyTick();
            if (!AiEnabled) return;
            int h = (int)Hours;
            for (int f = 1; f < Factions.Count; f++)
                if (!Factions[f].Eliminated && (h + f * 2) % 6 == 0) CampaignAi.Think(this, f);
        }

        // --- Orders ---------------------------------------------------------------------------

        public bool MoveFleet(int fleetId, int to)
        {
            var f = FleetById(fleetId);
            if (f == null || !f.Alive || f.Held) return false;
            f.Route.Clear();
            if (f.InTransit)
            {
                if (to == f.System || Galaxy.Route(f.System, to).Count < Galaxy.Route(f.Next, to).Count)
                {
                    // Turn back the way it came.
                    float len = Galaxy.LaneLength(f.System, f.Next);
                    (f.System, f.Next) = (f.Next, f.System);
                    f.Progress = System.MathF.Max(0f, len - f.Progress);
                    if (f.Next != to) f.Route.AddRange(Galaxy.Route(f.Next, to));
                    return true;
                }
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
            return s.Owner == owner && s.Shipyard && s.Unrest <= 0f && Factions[owner].Credits >= def.Cost;
        }

        public bool QueueBuild(int owner, int system, HullDef def)
        {
            if (!CanBuild(owner, system, def)) return false;
            Factions[owner].Credits -= def.Cost;
            Queue(system).Add(new BuildItem { Def = def, HoursLeft = def.BuildDays * 24f, Owner = owner });
            return true;
        }

        List<BuildItem> Queue(int system)
        {
            if (!Yards.TryGetValue(system, out var q)) Yards[system] = q = new List<BuildItem>();
            return q;
        }

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
            DropExcessTroops(f);
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
            foreach (var a in Armies)
                if (a.Fleet == from.Id) a.Fleet = into.Id;
            Fleets.Remove(from);
            return true;
        }

        // --- Hourly systems ---------------------------------------------------------------------

        void MoveFleets()
        {
            foreach (var f in Fleets)
            {
                if (!f.InTransit || !f.Alive || f.Held) continue;
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
                    Emit(CampaignEventKind.FleetArrived, f.System, f.Owner, fleet: f.Id);
                }
            }
            DetectInterceptions();
        }

        void BuildQueue()
        {
            foreach (var kv in Yards)
            {
                var sys = Galaxy.Systems[kv.Key];
                var q = kv.Value;
                if (q.Count == 0 || sys.Unrest > 0f) continue;
                int lanes = System.Math.Max(1, sys.Count(BuildingKind.Yard));
                float rate = YardRate(sys);
                for (int i = 0; i < System.Math.Min(lanes, q.Count); i++) q[i].HoursLeft -= q[i].Def == null ? 1f : rate;
                for (int i = q.Count - 1; i >= 0; i--)
                {
                    if (q[i].HoursLeft > 0f) continue;
                    var item = q[i];
                    q.RemoveAt(i);
                    if (item.Owner != sys.Owner) continue;
                    if (item.Def == null)
                    {
                        var army = NewArmy(item.Owner, sys.Id);
                        Emit(CampaignEventKind.ArmyRaised, sys.Id, item.Owner, text: army.Name);
                        continue;
                    }
                    var fleet = HomeFleet(item.Owner, sys.Id) ?? NewFleet(item.Owner, sys.Id, $"{sys.Name} Squadron");
                    var ship = NewShip(item.Owner, item.Def);
                    fleet.Ships.Add(ship);
                    Emit(CampaignEventKind.ShipBuilt, sys.Id, item.Owner, fleet: fleet.Id, text: ship.Name);
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
            foreach (var s in Galaxy.Systems)
                if ((s.Owner == f.Owner || (s.Owner >= 0 && Stance(s.Owner, f.Owner) == DiplomaticStance.Alliance))
                    && Vector3.Distance(s.Position, p) <= SupplyRangeOf(s)) return true;
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

        public float Strength(int faction, int system)
        {
            float s = 0f;
            foreach (var f in Fleets)
                if (f.Alive && f.Owner == faction && !f.InTransit && f.System == system) s += f.Strength;
            return s;
        }
    }
}
