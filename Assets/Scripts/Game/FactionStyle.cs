using UnityEngine;

namespace GalacticRupture.Game
{
    // One look per faction, used by the map, the UI and the ships' paint.
    //   Continuance: cold institutional steel with gold.
    //   Remnant: deep crimson and bone.
    //   Serac Compact: sea-green and cream, standardised.
    //   Ilium Directorate: ochre and dark bronze.
    public static class FactionStyle
    {
        public static readonly Color[] Primary =
        {
            new Color(0.48f, 0.72f, 0.95f),
            new Color(0.93f, 0.30f, 0.26f),
            new Color(0.30f, 0.82f, 0.66f),
            new Color(0.95f, 0.68f, 0.26f),
        };

        public static readonly Color[] Accent =
        {
            new Color(0.95f, 0.78f, 0.42f),
            new Color(0.90f, 0.85f, 0.74f),
            new Color(0.92f, 0.90f, 0.80f),
            new Color(0.55f, 0.38f, 0.22f),
        };

        public static readonly Color Neutral = new Color(0.58f, 0.6f, 0.68f);

        public static Color Of(int faction) => faction >= 0 && faction < Primary.Length ? Primary[faction] : Neutral;

        public static string Hex(int faction) => ColorUtility.ToHtmlStringRGB(Of(faction));
    }
}
