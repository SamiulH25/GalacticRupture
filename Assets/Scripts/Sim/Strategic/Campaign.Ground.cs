using System.Collections.Generic;

namespace GalacticRupture.Sim.Strategic
{
    public enum CommanderTrait
    {
        Steady,      // holds well
        Bold,        // hits hard, bleeds for it
        Methodical,  // digs in
        Veteran,     // better at everything
    }

    public sealed class Commander
    {
        public string Name = "";
        public CommanderTrait Trait;
        public int Victories;

        public float Attack => Trait switch { CommanderTrait.Bold => 1.25f, CommanderTrait.Veteran => 1.15f, CommanderTrait.Methodical => 0.9f, _ => 1f };
        public float Defence => Trait switch { CommanderTrait.Methodical => 1.25f, CommanderTrait.Veteran => 1.15f, CommanderTrait.Bold => 0.9f, CommanderTrait.Steady => 1.1f, _ => 1f };

        public string TraitText => Trait switch
        {
            CommanderTrait.Bold => "bold: hits hard, bleeds for it",
            CommanderTrait.Methodical => "methodical: digs in",
            CommanderTrait.Veteran => "veteran",
            _ => "steady",
        };
    }

    // Commands a fleet's battles when the player leaves them to the fleet (IDEAS.md §1.2).
    public sealed class Admiral
    {
        public string Name = "";
        public int Tactics = 3;   // 1..5: how well the guns are handled
        public int Nerve = 3;     // 1..5: how long the fleet stays in a losing fight

        public float ReloadScale => 1f - 0.05f * (Tactics - 3);
        public float BreakAt => 0.35f - 0.07f * (Nerve - 3);   // withdraw below this share of the starting strength
    }

    // Ground war is simulated, but it has faces (IDEAS.md §6): every army has a named commander.
    public sealed class Army
    {
        public int Id;
        public int Owner;
        public string Name = "";
        public Commander Commander = new Commander();
        public float Strength = 100f;
        public const float MaxStrength = 100f;
        public int System = -1;  // on the ground here (garrison, or landed as attackers)
        public int Fleet = -1;   // embarked in this fleet
    }

    public struct GroundForecast
    {
        public bool AttackerWins;
        public float Hours;
    }

    public sealed partial class Campaign
    {
        public const int ArmyCost = 120;
        public const float ArmyDays = 3f;

        public readonly List<Army> Armies = new List<Army>();
        int _nextArmyId;

        static readonly string[] Given = { "Ines", "Tomas", "Oyelaran", "Mireille", "Kasimir", "Adaeze", "Joren", "Halvard", "Sunniva", "Teodor", "Priya", "Dalia", "Emeric", "Wen", "Rasmus", "Ottilie", "Bastien", "Nadira", "Corwin", "Ysolde" };
        static readonly string[] Family = { "Varga", "Achterberg", "Okafor", "Delacroix-Hume", "Strand", "Imre", "Castellan", "Rook", "Abernethy", "Szabo", "Lindqvist", "Morrow", "Kaur", "Vasquez", "Thorne", "Adeyemi", "Kessler", "Nakamura", "Fairweather", "Orlov" };
        static readonly string[] Ordinal = { "1st", "2nd", "3rd", "4th", "5th", "6th", "7th", "8th", "9th", "10th", "11th", "12th" };

        public Army NewArmy(int owner, int system)
        {
            int n = 0;
            foreach (var a in Armies) if (a.Owner == owner) n++;
            var army = new Army
            {
                Id = _nextArmyId++,
                Owner = owner,
                System = system,
                Name = $"{(n < Ordinal.Length ? Ordinal[n] : (n + 1) + "th")} {Factions[owner].Name} Army",
                Commander = new Commander
                {
                    Name = $"{Given[_rng.Range(0, Given.Length)]} {Family[_rng.Range(0, Family.Length)]}",
                    Trait = (CommanderTrait)_rng.Range(0, 4),
                },
            };
            Armies.Add(army);
            return army;
        }

        public Army ArmyById(int id)
        {
            foreach (var a in Armies) if (a.Id == id) return a;
            return null;
        }

        public IEnumerable<Army> ArmiesIn(Fleet f)
        {
            foreach (var a in Armies) if (a.Fleet == f.Id) yield return a;
        }

        public IEnumerable<Army> ArmiesAt(int system)
        {
            foreach (var a in Armies) if (a.System == system && a.Fleet < 0) yield return a;
        }

        public int CountIn(Fleet f)
        {
            int n = 0;
            foreach (var a in Armies) if (a.Fleet == f.Id) n++;
            return n;
        }

        public const int AdmiralCost = 8;   // influence

        public bool CanAppointAdmiral(int fleetId)
        {
            var f = FleetById(fleetId);
            return f != null && f.Alive && f.Admiral == null && Factions[f.Owner].Influence >= AdmiralCost;
        }

        public Admiral AppointAdmiral(int fleetId)
        {
            if (!CanAppointAdmiral(fleetId)) return null;
            var f = FleetById(fleetId);
            Factions[f.Owner].Influence -= AdmiralCost;
            f.Admiral = new Admiral
            {
                Name = $"{Given[_rng.Range(0, Given.Length)]} {Family[_rng.Range(0, Family.Length)]}",
                Tactics = 1 + _rng.Range(0, 3) + _rng.Range(0, 3),
                Nerve = 1 + _rng.Range(0, 3) + _rng.Range(0, 3),
            };
            return f.Admiral;
        }

        public bool CanRaiseArmy(int owner, int system)
        {
            var s = Galaxy.Systems[system];
            return s.Owner == owner && s.Has(BuildingKind.Garrison) && s.Unrest <= 0f && Factions[owner].Credits >= ArmyCost;
        }

        public bool RaiseArmy(int owner, int system)
        {
            if (!CanRaiseArmy(owner, system)) return false;
            Factions[owner].Credits -= ArmyCost;
            Queue(system).Add(new BuildItem { Def = null, HoursLeft = ArmyDays * 24f, Owner = owner });
            return true;
        }

        // A power that loses its capital moves the court to its most populous remaining world.
        void Relocate(int faction, StarSystem lost)
        {
            lost.Capital = false;
            StarSystem best = null;
            foreach (var o in Galaxy.Systems)
                if (o.Owner == faction && o != lost && (best == null || o.Population > best.Population)) best = o;
            Factions[faction].Capital = best?.Id ?? -1;
            if (best != null) best.Capital = true;
        }

        public bool Embark(int fleetId, int armyId)
        {
            var f = FleetById(fleetId);
            var a = ArmyById(armyId);
            if (f == null || a == null || f.InTransit || a.Fleet >= 0 || a.Owner != f.Owner) return false;
            if (a.System != f.System || Galaxy.Systems[f.System].Owner != f.Owner) return false;
            if (CountIn(f) >= f.TroopCapacity) return false;
            a.Fleet = f.Id;
            a.System = -1;
            return true;
        }

        public bool Disembark(int armyId)
        {
            var a = ArmyById(armyId);
            if (a == null || a.Fleet < 0) return false;
            var f = FleetById(a.Fleet);
            if (f == null || f.InTransit || Galaxy.Systems[f.System].Owner != f.Owner) return false;
            a.Fleet = -1;
            a.System = f.System;
            return true;
        }

        public bool CanLand(Fleet f)
        {
            if (f == null || f.InTransit || CountIn(f) == 0) return false;
            var s = Galaxy.Systems[f.System];
            if (s.Owner == f.Owner) return false;
            if (s.Owner < 0 && s.Derelict == Derelict.Ruin) return false;   // nobody to fight: send an expedition
            if (s.Owner >= 0 && Stance(f.Owner, s.Owner) != DiplomaticStance.War) return false;
            if (s.Station > 0 && s.StationHull > 0f) return false;   // the station first
            foreach (var o in Fleets)
                if (o.Alive && !o.InTransit && o.System == s.Id && o.Owner != f.Owner && o.Owner >= 0
                    && Stance(o.Owner, f.Owner) == DiplomaticStance.War) return false;
            return true;
        }

        // Landings are automatic (IDEAS.md §1.1): a fleet carrying armies that stops over a
        // system it may land on puts them down. The decision was made when the fleet was sent.
        void AutoLand()
        {
            foreach (var f in Fleets)
                if (f.Alive && !f.InTransit && !f.Held && CountIn(f) > 0 && CanLand(f)) Land(f.Id);
        }

        // Everything aboard goes down to fight for the system.
        public int Land(int fleetId)
        {
            var f = FleetById(fleetId);
            if (!CanLand(f)) return 0;
            int n = 0;
            foreach (var a in Armies)
            {
                if (a.Fleet != f.Id) continue;
                a.Fleet = -1;
                a.System = f.System;
                n++;
            }
            Emit(CampaignEventKind.LandingStarted, f.System, f.Owner, Galaxy.Systems[f.System].Owner);
            return n;
        }

        // Transports lost in battle take their armies with them.
        internal void DropExcessTroops(Fleet f)
        {
            int cap = f.Alive ? f.TroopCapacity : 0;
            int n = 0;
            for (int i = Armies.Count - 1; i >= 0; i--)
            {
                if (Armies[i].Fleet != f.Id) continue;
                if (++n > cap) Armies.RemoveAt(i);
            }
        }

        public GroundForecast Forecast(IEnumerable<Army> attackers, StarSystem s)
        {
            var atk = new List<float>();
            var mult = new List<float>();
            foreach (var a in attackers)
            {
                atk.Add(a.Strength);
                mult.Add(a.Commander.Attack * Factions[a.Owner].Mods.ArmyAttack);
            }
            var def = new List<float>();
            var dmult = new List<float>();
            foreach (var a in ArmiesAt(s.Id))
                if (a.Owner == s.Owner)
                {
                    def.Add(a.Strength);
                    dmult.Add(a.Commander.Defence);
                }
            def.Add(s.Militia);
            dmult.Add(1f);
            for (int h = 0; h < 2000; h++)
            {
                if (Sum(def) <= 1f) return new GroundForecast { AttackerWins = true, Hours = h };
                if (Sum(atk) <= 1f) return new GroundForecast { AttackerWins = false, Hours = h };
                GroundRound(atk, mult, def, dmult, Fortification(s));
            }
            return new GroundForecast { AttackerWins = false, Hours = 2000 };
        }

        static float Sum(List<float> l)
        {
            float s = 0f;
            foreach (float v in l) s += v;
            return s;
        }

        static float Fortification(StarSystem s) => s.Capital ? 1.5f : 1.25f;

        // One hour of fighting: each side's losses are proportional to the other's weighted strength.
        static void GroundRound(List<float> atk, List<float> amult, List<float> def, List<float> dmult, float fort)
        {
            float ap = 0f, dp = 0f;
            for (int i = 0; i < atk.Count; i++) ap += atk[i] * amult[i];
            for (int i = 0; i < def.Count; i++) dp += def[i] * dmult[i];
            dp *= fort;
            float toDef = ap * 0.025f, toAtk = dp * 0.025f;
            Spread(def, toDef);
            Spread(atk, toAtk);
        }

        static void Spread(List<float> l, float damage)
        {
            float total = Sum(l);
            if (total <= 0f) return;
            for (int i = 0; i < l.Count; i++) l[i] = System.MathF.Max(0f, l[i] - damage * l[i] / total);
        }

        void GroundWar()
        {
            bool changed = false;
            foreach (var s in Galaxy.Systems)
            {
                // Who has armies on the ground here, other than the owner?
                int attacker = -1;
                foreach (var a in Armies)
                    if (a.System == s.Id && a.Fleet < 0 && a.Owner != s.Owner
                        && (s.Owner < 0 || Stance(a.Owner, s.Owner) == DiplomaticStance.War)
                        && (attacker < 0 || a.Owner < attacker)) attacker = a.Owner;

                if (attacker < 0)
                {
                    s.Besieger = -1;
                    s.Siege = 0f;
                    float max = MilitiaCap(s);
                    s.Militia = System.MathF.Min(max, s.Militia + max / 96f);
                    foreach (var a in Armies)
                        if (a.System == s.Id && a.Fleet < 0 && a.Owner == s.Owner)
                            a.Strength = System.MathF.Min(Army.MaxStrength, a.Strength + 0.5f);
                    continue;
                }

                s.Besieger = attacker;
                var atkArmies = new List<Army>();
                var defArmies = new List<Army>();
                foreach (var a in Armies)
                {
                    if (a.System != s.Id || a.Fleet >= 0) continue;
                    if (a.Owner == attacker) atkArmies.Add(a);
                    else if (a.Owner == s.Owner) defArmies.Add(a);
                }
                var atk = new List<float>();
                var amult = new List<float>();
                foreach (var a in atkArmies)
                {
                    atk.Add(a.Strength);
                    amult.Add(a.Commander.Attack * Factions[a.Owner].Mods.ArmyAttack);
                }
                var def = new List<float>();
                var dmult = new List<float>();
                foreach (var a in defArmies)
                {
                    def.Add(a.Strength);
                    dmult.Add(a.Commander.Defence);
                }
                def.Add(s.Militia);
                dmult.Add(1f);

                GroundRound(atk, amult, def, dmult, Fortification(s));
                for (int i = 0; i < atkArmies.Count; i++) atkArmies[i].Strength = atk[i];
                for (int i = 0; i < defArmies.Count; i++) defArmies[i].Strength = def[i];
                s.Militia = def[def.Count - 1];
                float a0 = Sum(atk), d0 = Sum(def);
                s.Siege = a0 / System.MathF.Max(1f, a0 + d0);

                Armies.RemoveAll(a => a.Strength <= 1f);

                if (d0 <= 1f)
                {
                    int loser = s.Owner;
                    s.Owner = attacker;
                    s.Derelict = Derelict.None;
                    s.Courter = s.Reclaimer = -1;
                    s.Courting = s.Reclaim = 0f;
                    s.Governor = null;
                    s.Stability = System.MathF.Min(s.Stability, 25f);
                    s.Unrest = 72f;
                    s.Militia = 0f;
                    s.Besieger = -1;
                    s.Siege = 0f;
                    // Half-built hulls in a captured yard are lost; captured systems degrade, never raze.
                    if (Yards.TryGetValue(s.Id, out var q)) q.Clear();
                    foreach (var a in atkArmies) if (a.Strength > 1f) a.Commander.Victories++;
                    Emit(CampaignEventKind.SystemCaptured, s.Id, attacker, loser);
                    if (loser >= 0 && Factions[loser].Capital == s.Id && OldCapital >= 0) Relocate(loser, s);
                    if (loser >= 0)
                    {
                        ChangeTrust(loser, attacker, -25f);
                        _aggression[attacker] += 1f;
                    }
                    changed = true;
                }
                else if (a0 <= 1f)
                {
                    s.Besieger = -1;
                    s.Siege = 0f;
                    Emit(CampaignEventKind.LandingFailed, s.Id, attacker, s.Owner);
                }
            }
            if (changed) CheckEnd();
        }
    }
}
