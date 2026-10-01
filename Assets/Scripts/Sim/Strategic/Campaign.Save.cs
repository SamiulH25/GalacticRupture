using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace GalacticRupture.Sim.Strategic
{
    // Saves: a campaign runs for weeks of play (IDEAS.md §1.1). The whole strategic state goes
    // to a versioned binary stream; loading it and ticking on gives the same game as never
    // having stopped. Saving waits until no battle or council vote is open.
    public sealed partial class Campaign
    {
        const int SaveMagic = 0x56535247;   // "GRSV"
        const int SaveVersion = 3;   // 3: stations

        public bool CanSave => Pending == null && Council == null;

        public void Save(Stream stream)
        {
            if (!CanSave) throw new System.InvalidOperationException("finish the battle or the vote first");
            using var w = new BinaryWriter(stream, System.Text.Encoding.UTF8, true);
            w.Write(SaveMagic);
            w.Write(SaveVersion);

            w.Write(_rng.State);
            w.Write(Hours);
            w.Write((int)State);
            w.Write(AiEnabled);
            w.Write(_nextFleetId);
            w.Write(_nextArmyId);
            w.Write(Act);
            w.Write(ThroneHolder);
            w.Write(ThroneSince);
            w.Write(_derelictsAtStart);
            w.Write(_act2Hour);

            // Galaxy.
            w.Write(Galaxy.Systems.Count);
            foreach (var s in Galaxy.Systems)
            {
                w.Write(s.Name);
                WriteV(w, s.Position);
                w.Write(s.Owner);
                w.Write(s.Capital);
                w.Write(s.OldCapital);
                w.Write(s.Siege);
                w.Write(s.Besieger);
                w.Write(s.Militia);
                w.Write(s.Unrest);
                w.Write(s.Population);
                w.Write(s.Habitability);
                w.Write(s.Stability);
                w.Write(s.Slots);
                w.Write((int)s.Derelict);
                w.Write(s.Reclaim);
                w.Write(s.Reclaimer);
                w.Write(s.Courting);
                w.Write(s.Courter);
                w.Write(s.Buildings.Count);
                foreach (var b in s.Buildings)
                {
                    w.Write((int)b.Kind);
                    w.Write(b.HoursLeft);
                    w.Write(b.Dormant);
                }
                w.Write(s.Governor != null);
                if (s.Governor != null) WriteGovernor(w, s.Governor);
                w.Write(s.Station);
                w.Write(s.StationHull);
                w.Write(s.StationHours);
            }
            w.Write(Galaxy.Lanes.Count);
            foreach (var l in Galaxy.Lanes)
            {
                w.Write(l.A);
                w.Write(l.B);
            }

            // Factions.
            w.Write(Factions.Count);
            foreach (var f in Factions)
            {
                w.Write(f.Name);
                w.Write(f.Major);
                w.Write(f.Credits);
                w.Write(f.Influence);
                w.Write(f.SupplyRange);
                w.Write(f.ShipPrefix);
                w.Write(f.ShipsBuilt);
                w.Write(f.Capital);
                w.Write(f.Eliminated);
                w.Write(f.EmbargoHours);
                w.Write(f.Designs.Count);
                foreach (var d in f.Designs)
                {
                    w.Write(d.Name);
                    w.Write(d.Hull.Id);
                    w.Write(d.Mounts.Count);
                    foreach (var m in d.Mounts) w.Write(m.Id);
                }
                var pool = Pool(f.Id);
                w.Write(pool.Count);
                foreach (var g in pool) WriteGovernor(w, g);
                var known = new List<string>(f.Known);
                known.Sort(System.StringComparer.Ordinal);
                w.Write(known.Count);
                foreach (string k in known) w.Write(k);
                w.Write(f.ResearchQueue.Count);
                foreach (string k in f.ResearchQueue) w.Write(k);
                w.Write(f.ResearchBank);
                w.Write(f.TechVersion);
            }

            // Fleets and ships.
            w.Write(Fleets.Count);
            foreach (var f in Fleets)
            {
                w.Write(f.Id);
                w.Write(f.Owner);
                w.Write(f.Name);
                w.Write(f.System);
                w.Write(f.Next);
                w.Write(f.Progress);
                w.Write(f.Route.Count);
                foreach (int r in f.Route) w.Write(r);
                w.Write(f.ArrivedHour);
                w.Write(f.Held);
                w.Write(f.Expedition);
                w.Write(f.Admiral != null);
                if (f.Admiral != null)
                {
                    w.Write(f.Admiral.Name);
                    w.Write(f.Admiral.Tactics);
                    w.Write(f.Admiral.Nerve);
                }
                w.Write(f.Ships.Count);
                foreach (var r in f.Ships)
                {
                    WriteDef(w, r.Def);
                    w.Write(r.Name);
                    w.Write(r.Hull);
                    w.Write(r.MountAlive.Length);
                    foreach (bool a in r.MountAlive) w.Write(a);
                }
            }

            // Armies.
            w.Write(Armies.Count);
            foreach (var a in Armies)
            {
                w.Write(a.Id);
                w.Write(a.Owner);
                w.Write(a.Name);
                w.Write(a.Commander.Name);
                w.Write((int)a.Commander.Trait);
                w.Write(a.Commander.Victories);
                w.Write(a.Strength);
                w.Write(a.System);
                w.Write(a.Fleet);
            }

            // Yards, in system order so the stream is deterministic.
            var yards = new List<int>(Yards.Keys);
            yards.Sort();
            w.Write(yards.Count);
            foreach (int sys in yards)
            {
                w.Write(sys);
                var q = Yards[sys];
                w.Write(q.Count);
                foreach (var item in q)
                {
                    w.Write(item.Def != null);
                    if (item.Def != null) WriteDef(w, item.Def);
                    w.Write(item.HoursLeft);
                    w.Write(item.Owner);
                }
            }

            // Diplomacy and the council.
            int n = Factions.Count;
            for (int a = 0; a < n; a++)
            for (int b = 0; b < n; b++)
            {
                w.Write((int)_stance[a, b]);
                w.Write(_trust[a, b]);
                w.Write(_since[a, b]);
            }
            for (int a = 0; a < n; a++) w.Write(_aggression[a]);
            w.Write(CeasefireHours);
            w.Write(_tabled.HasValue ? (int)_tabled.Value : -1);
            w.Write(_tabledTarget);
            w.Write(_tabledBy);
            w.Write(Incoming.Count);
            foreach (var p in Incoming)
            {
                w.Write(p.From);
                w.Write(p.To);
                w.Write((int)p.Treaty);
                w.Write(p.Expires);
            }
            w.Write(LastCouncil != null);
            if (LastCouncil != null)
            {
                w.Write((int)LastCouncil.Kind);
                w.Write(LastCouncil.Target);
                w.Write(LastCouncil.TabledBy);
                w.Write(LastCouncil.Passed);
                w.Write(LastCouncil.YesSeats);
                w.Write(LastCouncil.NoSeats);
            }

            // What everyone knows.
            foreach (var intel in Intel)
            {
                for (int s = 0; s < Galaxy.Systems.Count; s++)
                {
                    w.Write(intel.SystemVisible[s]);
                    w.Write(intel.OwnerSeen[s]);
                    w.Write(intel.SeenHour[s]);
                    w.Write(intel.GarrisonSeen[s]);
                }
                var ids = new List<int>(intel.Fleets.Keys);
                ids.Sort();
                w.Write(ids.Count);
                foreach (int id in ids)
                {
                    var f = intel.Fleets[id];
                    w.Write(f.Fleet);
                    w.Write(f.Owner);
                    w.Write(f.System);
                    w.Write(f.Next);
                    w.Write(f.Progress);
                    w.Write(f.Strength);
                    w.Write(f.Ships);
                    w.Write(f.Troops);
                    w.Write(f.Hour);
                }
                var visible = new List<int>(intel.VisibleFleets);
                visible.Sort();
                w.Write(visible.Count);
                foreach (int id in visible) w.Write(id);
            }
        }

        public static Campaign Load(Stream stream)
        {
            using var r = new BinaryReader(stream, System.Text.Encoding.UTF8, true);
            if (r.ReadInt32() != SaveMagic) throw new InvalidDataException("not a Galactic Rupture save");
            int version = r.ReadInt32();
            if (version != SaveVersion && version != 2) throw new InvalidDataException($"save version {version} is not supported");

            var c = new Campaign(r.ReadUInt64());
            c.Hours = r.ReadDouble();
            c.State = (CampaignState)r.ReadInt32();
            c.AiEnabled = r.ReadBoolean();
            c._nextFleetId = r.ReadInt32();
            c._nextArmyId = r.ReadInt32();
            c.Act = r.ReadInt32();
            c.ThroneHolder = r.ReadInt32();
            c.ThroneSince = r.ReadDouble();
            c._derelictsAtStart = r.ReadInt32();
            c._act2Hour = r.ReadDouble();

            int systems = r.ReadInt32();
            for (int i = 0; i < systems; i++)
            {
                string name = r.ReadString();
                var p = ReadV(r);
                var s = c.Galaxy.Add(name, p.X, p.Y, p.Z, 1, -1);
                s.Buildings.Clear();
                s.Owner = r.ReadInt32();
                s.Capital = r.ReadBoolean();
                s.OldCapital = r.ReadBoolean();
                s.Siege = r.ReadSingle();
                s.Besieger = r.ReadInt32();
                s.Militia = r.ReadSingle();
                s.Unrest = r.ReadSingle();
                s.Population = r.ReadSingle();
                s.Habitability = r.ReadSingle();
                s.Stability = r.ReadSingle();
                s.Slots = r.ReadInt32();
                s.Derelict = (Derelict)r.ReadInt32();
                s.Reclaim = r.ReadSingle();
                s.Reclaimer = r.ReadInt32();
                s.Courting = r.ReadSingle();
                s.Courter = r.ReadInt32();
                int buildings = r.ReadInt32();
                for (int b = 0; b < buildings; b++)
                    s.Buildings.Add(new Building((BuildingKind)r.ReadInt32()) { HoursLeft = r.ReadSingle(), Dormant = r.ReadBoolean() });
                if (r.ReadBoolean()) s.Governor = ReadGovernor(r);
                if (version >= 3)
                {
                    s.Station = r.ReadInt32();
                    s.StationHull = r.ReadSingle();
                    s.StationHours = r.ReadSingle();
                }
            }
            int lanes = r.ReadInt32();
            for (int i = 0; i < lanes; i++) c.Galaxy.Connect(r.ReadInt32(), r.ReadInt32());

            int factions = r.ReadInt32();
            for (int i = 0; i < factions; i++)
            {
                var f = c.AddFaction(r.ReadString(), r.ReadBoolean(), "", 0f);
                f.Credits = r.ReadSingle();
                f.Influence = r.ReadSingle();
                f.SupplyRange = r.ReadSingle();
                f.ShipPrefix = r.ReadString();
                f.ShipsBuilt = r.ReadInt32();
                f.Capital = r.ReadInt32();
                f.Eliminated = r.ReadBoolean();
                f.EmbargoHours = r.ReadSingle();
                f.Designs.Clear();
                int designs = r.ReadInt32();
                for (int d = 0; d < designs; d++)
                {
                    string dname = r.ReadString();
                    var design = new ShipDesign(dname, Hulls.ById(r.ReadString()));
                    int mounts = r.ReadInt32();
                    for (int m = 0; m < mounts; m++) design.Set(m, Hulls.MountById(r.ReadString()));
                    f.Designs.Add(design);
                }
                var pool = c.Pool(f.Id);
                int pooled = r.ReadInt32();
                for (int g = 0; g < pooled; g++) pool.Add(ReadGovernor(r));
                int known = r.ReadInt32();
                for (int k = 0; k < known; k++) f.Known.Add(r.ReadString());
                int queued = r.ReadInt32();
                for (int k = 0; k < queued; k++) f.ResearchQueue.Add(r.ReadString());
                f.ResearchBank = r.ReadSingle();
                f.TechVersion = r.ReadInt32();
                c.ApplyKnown(f);
            }

            int fleets = r.ReadInt32();
            for (int i = 0; i < fleets; i++)
            {
                var f = new Fleet
                {
                    Id = r.ReadInt32(),
                    Owner = r.ReadInt32(),
                    Name = r.ReadString(),
                    System = r.ReadInt32(),
                    Next = r.ReadInt32(),
                    Progress = r.ReadSingle(),
                };
                int route = r.ReadInt32();
                for (int k = 0; k < route; k++) f.Route.Add(r.ReadInt32());
                f.ArrivedHour = r.ReadDouble();
                f.Held = r.ReadBoolean();
                f.Expedition = r.ReadBoolean();
                if (r.ReadBoolean()) f.Admiral = new Admiral { Name = r.ReadString(), Tactics = r.ReadInt32(), Nerve = r.ReadInt32() };
                int ships = r.ReadInt32();
                for (int k = 0; k < ships; k++)
                {
                    var def = ReadDef(r);
                    var rec = new ShipRecord(def, r.ReadString()) { Hull = r.ReadSingle() };
                    int mounts = r.ReadInt32();
                    for (int m = 0; m < mounts; m++)
                    {
                        bool alive = r.ReadBoolean();
                        if (m < rec.MountAlive.Length) rec.MountAlive[m] = alive;
                    }
                    f.Ships.Add(rec);
                }
                c.Fleets.Add(f);
            }

            int armies = r.ReadInt32();
            for (int i = 0; i < armies; i++)
            {
                c.Armies.Add(new Army
                {
                    Id = r.ReadInt32(),
                    Owner = r.ReadInt32(),
                    Name = r.ReadString(),
                    Commander = new Commander { Name = r.ReadString(), Trait = (CommanderTrait)r.ReadInt32(), Victories = r.ReadInt32() },
                    Strength = r.ReadSingle(),
                    System = r.ReadInt32(),
                    Fleet = r.ReadInt32(),
                });
            }

            int yards = r.ReadInt32();
            for (int i = 0; i < yards; i++)
            {
                int sys = r.ReadInt32();
                var q = c.Queue(sys);
                int items = r.ReadInt32();
                for (int k = 0; k < items; k++)
                {
                    var def = r.ReadBoolean() ? ReadDef(r) : null;
                    q.Add(new BuildItem { Def = def, HoursLeft = r.ReadSingle(), Owner = r.ReadInt32() });
                }
            }

            c.InitDiplomacy();
            int n = c.Factions.Count;
            for (int a = 0; a < n; a++)
            for (int b = 0; b < n; b++)
            {
                c._stance[a, b] = (DiplomaticStance)r.ReadInt32();
                c._trust[a, b] = r.ReadSingle();
                c._since[a, b] = r.ReadDouble();
            }
            for (int a = 0; a < n; a++) c._aggression[a] = r.ReadSingle();
            c.CeasefireHours = r.ReadSingle();
            int tabled = r.ReadInt32();
            c._tabled = tabled < 0 ? (ResolutionKind?)null : (ResolutionKind)tabled;
            c._tabledTarget = r.ReadInt32();
            c._tabledBy = r.ReadInt32();
            int incoming = r.ReadInt32();
            for (int i = 0; i < incoming; i++)
                c.Incoming.Add(new Proposal { From = r.ReadInt32(), To = r.ReadInt32(), Treaty = (DiplomaticStance)r.ReadInt32(), Expires = r.ReadDouble() });
            if (r.ReadBoolean())
            {
                var last = new CouncilSession
                {
                    Kind = (ResolutionKind)r.ReadInt32(),
                    Target = r.ReadInt32(),
                    TabledBy = r.ReadInt32(),
                    Passed = r.ReadBoolean(),
                    YesSeats = r.ReadInt32(),
                    NoSeats = r.ReadInt32(),
                };
                c.LastCouncil = last;
            }

            c.InitIntel();
            foreach (var intel in c.Intel)
            {
                for (int s = 0; s < systems; s++)
                {
                    intel.SystemVisible[s] = r.ReadBoolean();
                    intel.OwnerSeen[s] = r.ReadInt32();
                    intel.SeenHour[s] = r.ReadDouble();
                    intel.GarrisonSeen[s] = r.ReadSingle();
                }
                int sightings = r.ReadInt32();
                for (int i = 0; i < sightings; i++)
                {
                    var f = new FleetSighting
                    {
                        Fleet = r.ReadInt32(),
                        Owner = r.ReadInt32(),
                        System = r.ReadInt32(),
                        Next = r.ReadInt32(),
                        Progress = r.ReadSingle(),
                        Strength = r.ReadSingle(),
                        Ships = r.ReadInt32(),
                        Troops = r.ReadInt32(),
                        Hour = r.ReadDouble(),
                    };
                    intel.Fleets[f.Fleet] = f;
                }
                int visible = r.ReadInt32();
                for (int i = 0; i < visible; i++) intel.VisibleFleets.Add(r.ReadInt32());
            }
            return c;
        }

        // A hull as built: the stock hull, the design's name and the gun in every hardpoint.
        static void WriteDef(BinaryWriter w, HullDef d)
        {
            w.Write(d.Id);
            w.Write(d.Design ?? "");
            w.Write(d.Hardpoints.Count);
            foreach (var hp in d.Hardpoints) w.Write(hp.Mount.Id);
            // Research improves hulls as they are built; the numbers travel with the ship.
            w.Write(d.HullHp);
            w.Write(d.ShieldHp);
            w.Write(d.ShieldRegen);
            w.Write(d.LyPerDay);
        }

        static HullDef ReadDef(BinaryReader r)
        {
            var stock = Hulls.ById(r.ReadString());
            string design = r.ReadString();
            int n = r.ReadInt32();
            var mounts = new MountDef[n];
            bool same = design.Length == 0 && n == stock.Hardpoints.Count;
            for (int i = 0; i < n; i++)
            {
                mounts[i] = Hulls.MountById(r.ReadString());
                if (i >= stock.Hardpoints.Count || mounts[i] != stock.Hardpoints[i].Mount) same = false;
            }
            float hull = r.ReadSingle(), shield = r.ReadSingle(), regen = r.ReadSingle(), speed = r.ReadSingle();
            same &= hull == stock.HullHp && shield == stock.ShieldHp && regen == stock.ShieldRegen && speed == stock.LyPerDay;
            if (same) return stock;
            var def = stock.WithMounts(design, mounts);
            def.HullHp = hull;
            def.ShieldHp = shield;
            def.ShieldRegen = regen;
            def.LyPerDay = speed;
            return def;
        }

        static void WriteGovernor(BinaryWriter w, Governor g)
        {
            w.Write(g.Name);
            w.Write(g.Administration);
            w.Write(g.Industry);
            w.Write(g.Loyalty);
            w.Write((int)g.Trait);
            w.Write((int)g.Focus);
            w.Write((int)g.Origin);
            w.Write(g.Home);
        }

        static Governor ReadGovernor(BinaryReader r) => new Governor
        {
            Name = r.ReadString(),
            Administration = r.ReadInt32(),
            Industry = r.ReadInt32(),
            Loyalty = r.ReadInt32(),
            Trait = (GovernorTrait)r.ReadInt32(),
            Focus = (GovernorFocus)r.ReadInt32(),
            Origin = (GovernorOrigin)r.ReadInt32(),
            Home = r.ReadString(),
        };

        static void WriteV(BinaryWriter w, Vector3 v)
        {
            w.Write(v.X);
            w.Write(v.Y);
            w.Write(v.Z);
        }

        static Vector3 ReadV(BinaryReader r) => new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
    }
}
