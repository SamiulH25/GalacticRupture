using UnityEngine;

namespace GalacticRupture.Game
{
    // Every material the runtime needs, created and saved by the editor builder so they ship
    // in the player. Index by side where a faction look applies.
    [System.Serializable]
    public struct ModelEntry
    {
        // Hull class, or mount id for a turret.
        public string Id;
        // Faction this mesh belongs to, or -1 when it stands in for every faction.
        public int Faction;
        public GameObject Prefab;
    }

    [CreateAssetMenu(menuName = "Galactic Rupture/Game Art")]
    public sealed class GameArt : ScriptableObject
    {
        // Order matters: it is the faction index every per-faction array is indexed by.
        public static readonly string[] FactionNames =
            { "continuance", "remnant", "serac", "ilium" };

        public static int FactionIndex(string id)
        {
            for (int i = 0; i < FactionNames.Length; i++)
                if (FactionNames[i] == id) return i;
            return -1;
        }

        // Which of the four model and paint sets a campaign faction flies (Sim GalaxyGen order:
        // majors Continuance, Remnant, Hierarchate, Free Yards, Lantern Synod, then minors
        // starting with Serac Compact and Ilium Directorate). Powers without their own set
        // borrow the nearest in look: the Hierarchate's standardised sea-green hulls are
        // Serac's, the Free Yards' bronze is Ilium's, the Lantern Synod flies Continuance
        // steel; the other minors cycle through the four.
        static readonly int[] ArtOf = { 0, 1, 2, 3, 0, 2, 3 };

        public static int ArtFaction(int faction) =>
            faction < 0 ? 0 : faction < ArtOf.Length ? ArtOf[faction] : faction % FactionNames.Length;

        [Header("Models (Tools/blender), by hull and mount id")]
        public ModelEntry[] HullModels = new ModelEntry[0];
        public ModelEntry[] TurretModels = new ModelEntry[0];

        // A faction's own hull wins; a faction-agnostic entry is the fallback, and null
        // lets the caller fall back to the procedural placeholder mesh.
        public GameObject HullModel(string id, int faction) => Find(HullModels, id, faction);
        public GameObject TurretModel(string id) => Find(TurretModels, id, -1);

        static GameObject Find(ModelEntry[] entries, string id, int faction)
        {
            GameObject fallback = null;
            foreach (var e in entries)
            {
                if (e.Id != id) continue;
                if (e.Faction == faction) return e.Prefab;
                if (e.Faction < 0) fallback = e.Prefab;
            }
            return fallback;
        }

        // Blender material slots are named after these; anything else keeps its imported
        // material. `Dark` is per-faction: the reference art gives each power its own
        // secondary tone, and Serac and Remnant use it for large area blocks rather than
        // thin trim, so a single shared near-black flattened all four fleets.
        public Material ForSlot(string slotName, int side) => slotName switch
        {
            "Paint" => Pick(HullPaint, side),
            "Dark" => Pick(Structure, side),
            "Trim" => Pick(Trim, side),
            "Windows" => Windows,
            "Engine" => Pick(EngineGlow, side),
            "Heat" => Heat,
            _ => null,
        };

        // Per-faction arrays are authored at four entries by ProjectBuilder; guard anyway
        // so an older asset degrades to the faction-agnostic material instead of throwing.
        static Material Pick(Material[] set, int side)
        {
            if (set == null || set.Length == 0) return null;
            if (side >= 0 && side < set.Length && set[side] != null) return set[side];
            for (int i = 0; i < set.Length; i++)
                if (set[i] != null) return set[i];
            return null;
        }

        public void ApplyFaction(Renderer r, int side)
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null) continue;
                var m = ForSlot(mats[i].name, side);
                if (m != null) mats[i] = m;
            }
            r.sharedMaterials = mats;
        }

        [Header("Hull (per faction: Continuance, Remnant, Serac Compact, Ilium Directorate)")]
        public Material[] HullPaint = new Material[2];
        // Structure is the `Dark` slot: armour, recesses, and each faction's secondary
        // tone. Per-faction because Serac and Remnant use it for large area blocks, not
        // just thin trim.
        public Material[] Structure = new Material[2];
        public Material[] Trim = new Material[2];
        public Material[] EngineGlow = new Material[2];
        public Material[] Plume = new Material[2];
        public Material Windows;
        public Material Heat;
        public Material Wreck;

        // Faction-neutral structure, for anything with no owner (map stations).
        public Material DarkMetal => Pick(Structure, 0);

        [Header("Weapons")]
        public Material TracerBallistic;
        public Material TracerEnergy;
        public Material TracerPointDefence;
        public Material TracerEnemy;

        [Header("Effects")]
        public Material Flash;
        public Material Spark;
        public Material Fire;
        public Material Shield;
        public Material ShieldField;
        public Material Scorch;

        [Header("Galaxy map")]
        public Material MapPlate;
        public Material Star;
        public Material Lane;
        public Material Planet;

        [Header("Interface")]
        public Font UiFont;
        public Font UiFontBold;
    }

    // Submesh slots shared by every generated ship mesh.
    public static class Slot
    {
        public const int Paint = 0;
        public const int Dark = 1;
        public const int Trim = 2;
        public const int Windows = 3;
        public const int Engine = 4;
        public const int Heat = 5;
        public const int Count = 6;

        public static Material[] Materials(GameArt art, int side) => new[]
        {
            art.HullPaint[side], art.ForSlot("Dark", side), art.Trim[side], art.Windows,
            art.EngineGlow[side], art.Heat,
        };
    }
}
