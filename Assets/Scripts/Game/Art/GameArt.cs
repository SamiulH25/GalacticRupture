using UnityEngine;

namespace GalacticRupture.Game
{
    // Every material the runtime needs, created and saved by the editor builder so they ship
    // in the player. Index by side where a faction look applies.
    [CreateAssetMenu(menuName = "Galactic Rupture/Game Art")]
    public sealed class GameArt : ScriptableObject
    {
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
