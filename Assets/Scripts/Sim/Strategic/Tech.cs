using System.Collections.Generic;

namespace GalacticRupture.Sim.Strategic
{
    public enum TechBranch
    {
        Ballistic,
        Energy,
        Disruptive,
        Defence,
        Industry,
        Logistics,
    }

    // What a faction's research has changed. Rebuilt from the list of finished technologies,
    // so a save only needs the list.
    public sealed class TechMods
    {
        public float FabIndustry;          // added to each fab's +8
        public float RefineryCredits;      // added to each refinery's +20
        public float LabResearch;          // added to each lab's +5
        public float HabitatPop;           // added to each habitat's +3
        public float YardRate = 1f;
        public float ReclaimFactor = 1f;   // cost and time of reclamation
        public float SupplyRange;
        public float Speed = 1f;           // light-years a day
        public float AdminStability;
        public float AdminInfluence;
        public float ArmyAttack = 1f;
        public float Militia = 1f;
        public float Hull = 1f;
        public float Shields = 1f;
        public float ShieldRegen = 1f;
        public float Reload = 1f;
    }

    public sealed class TechDef
    {
        public string Id = "";
        public string Name = "";
        public TechBranch Branch;
        public int Tier;                   // 1..4
        public string[] Requires = new string[0];
        public string Text = "";           // what it does, in a line
        public System.Action<TechMods> Apply;

        public int Cost => Tier switch { 1 => 300, 2 => 1200, 3 => 5000, _ => 14000 };
    }

    // The tree (IDEAS.md §5.4): tiers are behavioural, not numbers. Weapon branches unlock new
    // kinds of gun; the rest change how the economy, logistics and ships work.
    public static class Techs
    {
        static TechDef T(string id, string name, TechBranch b, int tier, string text, System.Action<TechMods> apply = null, params string[] requires) =>
            new TechDef { Id = id, Name = name, Branch = b, Tier = tier, Text = text, Apply = apply, Requires = requires };

        public static readonly TechDef[] All =
        {
            T("rotary", "Rotary mechanisms", TechBranch.Ballistic, 1, "Rotary cannon (small): a hail of light rounds"),
            T("rotary_driver", "Rotary drivers", TechBranch.Ballistic, 2, "Rotary driver (medium)", null, "rotary"),
            T("rails", "Rail theory", TechBranch.Ballistic, 2, "Railgun (medium): long reach, fast, accurate", null, "rotary"),
            T("capital_rails", "Capital rails", TechBranch.Ballistic, 3, "Heavy railgun (large)", null, "rails"),
            T("acceleration", "Mass acceleration", TechBranch.Ballistic, 4, "Mass accelerator (large): the round goes on through its target", null, "capital_rails"),

            T("heavy_plasma", "Heavy plasma cannons", TechBranch.Energy, 1, "Heavy plasma cannon (large)"),
            T("pulse", "Pulse emitters", TechBranch.Energy, 2, "Pulse laser (small): quad emitters, rapid bolts", null, "heavy_plasma"),
            T("lances", "Beam lances", TechBranch.Energy, 3, "Beam lance (large): one slow, crushing bolt", null, "pulse"),
            T("shield_burners", "Shield burners", TechBranch.Energy, 4, "Shield burner (medium): more than twice as hard on shields", null, "lances"),

            T("arc", "Arc theory", TechBranch.Disruptive, 1, "Arc cannon (medium): kills guns, not hulls"),
            T("compact_arc", "Compact arc coils", TechBranch.Disruptive, 2, "Light arc cannon (small)", null, "arc"),
            T("heavy_arc", "Heavy arc cannon", TechBranch.Disruptive, 3, "Heavy arc cannon (large)", null, "compact_arc"),
            T("arc_doctrine", "Arc doctrine", TechBranch.Disruptive, 4, "Every gun reloads 10% faster", m => m.Reload *= 0.9f, "heavy_arc"),

            T("flak", "Flak batteries", TechBranch.Defence, 1, "Flak battery (medium): screens small craft"),
            T("heavy_flak", "Heavy flak", TechBranch.Defence, 2, "Heavy flak (large)", null, "flak"),
            T("shield_lattice", "Shield lattice", TechBranch.Defence, 2, "Shields +20% on new hulls", m => m.Shields *= 1.2f),
            T("armour", "Layered armour", TechBranch.Defence, 2, "Hull +15% on new hulls", m => m.Hull *= 1.15f),
            T("reactive_shields", "Reactive shields", TechBranch.Defence, 3, "Shield regeneration +40% on new hulls", m => m.ShieldRegen *= 1.4f, "shield_lattice"),
            T("fortress_worlds", "Fortress worlds", TechBranch.Defence, 3, "Militia +50%", m => m.Militia *= 1.5f, "armour"),

            T("automated_fabs", "Automated fabs", TechBranch.Industry, 1, "Fabs +4 industry", m => m.FabIndustry += 4f),
            T("orbital_refining", "Orbital refining", TechBranch.Industry, 1, "Refineries +10 credits", m => m.RefineryCredits += 10f),
            T("institutes", "Research institutes", TechBranch.Industry, 1, "Laboratories +3 research", m => m.LabResearch += 3f),
            T("arcologies", "Arcologies", TechBranch.Industry, 2, "Habitats +2 population ceiling", m => m.HabitatPop += 2f, "orbital_refining"),
            T("slipways", "Slipway automation", TechBranch.Industry, 3, "Yards work 25% faster", m => m.YardRate *= 1.25f, "automated_fabs"),

            T("salvage", "Salvage doctrine", TechBranch.Logistics, 1, "Reclamation costs and takes 30% less", m => m.ReclaimFactor *= 0.7f),
            T("forward_depots", "Forward depots", TechBranch.Logistics, 1, "Supply range +4 ly", m => m.SupplyRange += 4f),
            T("lane_drives", "Lane drives", TechBranch.Logistics, 2, "Fleets cross lanes 15% faster (new hulls)", m => m.Speed *= 1.15f, "forward_depots"),
            T("civics", "Civic administration", TechBranch.Logistics, 2, "Administration +5 stability, +0.5 influence", m => { m.AdminStability += 5f; m.AdminInfluence += 0.5f; }, "salvage"),
            T("drop_doctrine", "Drop doctrine", TechBranch.Logistics, 2, "Armies attack 25% harder", m => m.ArmyAttack *= 1.25f),
            T("staff_college", "Staff college", TechBranch.Logistics, 3, "Every gun reloads 5% faster", m => m.Reload *= 0.95f, "drop_doctrine"),
        };

        public static TechDef ById(string id)
        {
            foreach (var t in All) if (t.Id == id) return t;
            return null;
        }

        public static IEnumerable<MountDef> Unlocks(TechDef t)
        {
            foreach (var m in Hulls.Mounts) if (m.Tech == t.Id) yield return m;
        }
    }

    public sealed partial class Campaign
    {
        public const float LabResearch = 3f;

        public bool Knows(int faction, string tech) => tech.Length == 0 || Factions[faction].Known.Contains(tech);

        public bool CanUse(int faction, MountDef m) => Knows(faction, m.Tech);

        public bool CanResearch(int faction, TechDef t)
        {
            var f = Factions[faction];
            if (f.Known.Contains(t.Id)) return false;
            foreach (string r in t.Requires) if (!f.Known.Contains(r)) return false;
            return true;
        }

        public float ResearchPerDay(int faction)
        {
            float r = 0f;
            var mods = Factions[faction].Mods;
            foreach (var s in Galaxy.Systems)
            {
                if (s.Owner != faction || s.Unrest > 0f) continue;
                float here = 0.05f * s.Population + (LabResearch + mods.LabResearch) * s.Count(BuildingKind.Lab);
                if (s.Governor != null) here *= 1f + 0.05f * (s.Governor.Industry - 3);
                r += here;
            }
            return r;
        }

        // Set what a faction researches next; the rest of the queue follows it.
        public bool Research(int faction, string tech)
        {
            var t = Techs.ById(tech);
            if (t == null || !CanResearch(faction, t)) return false;
            var q = Factions[faction].ResearchQueue;
            q.Remove(tech);
            q.Insert(0, tech);
            return true;
        }

        public void Enqueue(int faction, string tech)
        {
            var t = Techs.ById(tech);
            var f = Factions[faction];
            if (t == null || f.Known.Contains(tech) || f.ResearchQueue.Contains(tech)) return;
            // Bring the prerequisites along, first.
            foreach (string r in t.Requires) Enqueue(faction, r);
            f.ResearchQueue.Add(tech);
        }

        public void Dequeue(int faction, string tech) => Factions[faction].ResearchQueue.Remove(tech);

        public TechDef Current(int faction)
        {
            var f = Factions[faction];
            foreach (string id in f.ResearchQueue)
            {
                var t = Techs.ById(id);
                if (t != null && CanResearch(faction, t)) return t;
            }
            return null;
        }

        void ResearchTick()
        {
            foreach (var f in Factions)
            {
                if (f.Eliminated) continue;
                f.ResearchBank += ResearchPerDay(f.Id) / 24f;
                f.ResearchQueue.RemoveAll(id => f.Known.Contains(id));
                var t = Current(f.Id);
                if (t == null && f.Id != PlayerFaction && AiEnabled) t = CampaignAi.PickResearch(this, f.Id);
                if (t == null || f.ResearchBank < t.Cost) continue;
                f.ResearchBank -= t.Cost;
                Learn(f.Id, t);
            }
        }

        public void Learn(int faction, TechDef t)
        {
            var f = Factions[faction];
            if (!f.Known.Add(t.Id)) return;
            f.ResearchQueue.Remove(t.Id);
            t.Apply?.Invoke(f.Mods);
            f.TechVersion++;
            Emit(CampaignEventKind.TechLearned, faction: faction, text: t.Id);
            if (faction != PlayerFaction) CampaignAi.Redesign(this, faction);
        }

        // Rebuild modifiers after loading.
        void ApplyKnown(Faction f)
        {
            f.Mods = new TechMods();
            foreach (var t in Techs.All)
                if (f.Known.Contains(t.Id)) t.Apply?.Invoke(f.Mods);
        }

        // What the yards build for this faction: the design, with research applied to the hull.
        public HullDef Buildable(int faction, ShipDesign d)
        {
            var f = Factions[faction];
            if (_buildable.TryGetValue(d, out var e) && e.Version == f.TechVersion && e.Source == d.Built) return e.Def;
            var def = d.Built.WithMounts(d.Name, d.Mounts);
            var m = f.Mods;
            def.HullHp *= m.Hull;
            def.ShieldHp *= m.Shields;
            def.ShieldRegen *= m.ShieldRegen;
            def.LyPerDay *= m.Speed;
            _buildable[d] = (f.TechVersion, d.Built, def);
            return def;
        }

        readonly Dictionary<ShipDesign, (int Version, HullDef Source, HullDef Def)> _buildable = new Dictionary<ShipDesign, (int, HullDef, HullDef)>();
    }
}
