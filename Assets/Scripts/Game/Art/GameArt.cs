using UnityEngine;

namespace GalacticRupture.Game
{
    // Every material the runtime needs, created and saved by the editor builder so they ship
    // in the player. Index by side where a faction look applies.
    [System.Serializable]
    public struct ModelEntry
    {
        public string Id;
        public GameObject Prefab;
    }

    [CreateAssetMenu(menuName = "Galactic Rupture/Game Art")]
    public sealed class GameArt : ScriptableObject
    {
        [Header("Models (Tools/blender), by hull and mount id")]
        public ModelEntry[] HullModels = new ModelEntry[0];
        public ModelEntry[] TurretModels = new ModelEntry[0];

        public GameObject HullModel(string id) => Find(HullModels, id);
        public GameObject TurretModel(string id) => Find(TurretModels, id);

        static GameObject Find(ModelEntry[] entries, string id)
        {
            foreach (var e in entries)
                if (e.Id == id) return e.Prefab;
            return null;
        }

        // Blender material slots are named after these; anything else keeps its imported material.
        public Material ForSlot(string slotName, int side) => slotName switch
        {
            "Paint" => HullPaint[side],
            "Dark" => DarkMetal,
            "Trim" => Trim[side],
            "Windows" => Windows,
            "Engine" => EngineGlow[side],
            "Heat" => Heat,
            _ => null,
        };

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

        [Header("Hull (per side)")]
        public Material[] HullPaint = new Material[2];
        public Material[] Trim = new Material[2];
        public Material[] EngineGlow = new Material[2];
        public Material[] Plume = new Material[2];
        public Material DarkMetal;
        public Material Windows;
        public Material Heat;
        public Material Wreck;

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
            art.HullPaint[side], art.DarkMetal, art.Trim[side], art.Windows, art.EngineGlow[side], art.Heat,
        };
    }
}
