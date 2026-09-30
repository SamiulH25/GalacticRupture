using UnityEngine;

namespace GalacticRupture.Game
{
    // One look per faction, used by the map, the UI and the ships' paint. Majors first, in the
    // generated galaxy's order (Sim GalaxyGen), then the minors in softer, distinct hues.
    //   Continuance: cold institutional steel with gold.
    //   Remnant: deep crimson and bone.
    //   Hierarchate: sea-green and cream, standardised.
    //   Free Yards: ochre and dark bronze.
    //   Lantern Synod: violet and pale gold.
    public static class FactionStyle
    {
        public static readonly Color[] Primary =
        {
            new Color(0.48f, 0.72f, 0.95f),
            new Color(0.93f, 0.30f, 0.26f),
            new Color(0.30f, 0.82f, 0.66f),
            new Color(0.95f, 0.68f, 0.26f),
            new Color(0.70f, 0.52f, 0.96f),
            new Color(0.88f, 0.56f, 0.76f),
            new Color(0.62f, 0.80f, 0.36f),
            new Color(0.95f, 0.88f, 0.48f),
            new Color(0.45f, 0.87f, 0.96f),
            new Color(0.78f, 0.62f, 0.46f),
            new Color(0.58f, 0.68f, 0.52f),
            new Color(0.96f, 0.58f, 0.44f),
            new Color(0.66f, 0.72f, 0.88f),
            new Color(0.86f, 0.78f, 0.96f),
            new Color(0.50f, 0.86f, 0.56f),
        };

        public static readonly Color[] Accent =
        {
            new Color(0.95f, 0.78f, 0.42f),
            new Color(0.90f, 0.85f, 0.74f),
            new Color(0.92f, 0.90f, 0.80f),
            new Color(0.55f, 0.38f, 0.22f),
            new Color(0.96f, 0.86f, 0.55f),
            new Color(0.95f, 0.92f, 0.90f),
            new Color(0.30f, 0.36f, 0.20f),
            new Color(0.40f, 0.34f, 0.16f),
            new Color(0.90f, 0.96f, 0.98f),
            new Color(0.35f, 0.26f, 0.18f),
            new Color(0.90f, 0.92f, 0.86f),
            new Color(0.40f, 0.20f, 0.16f),
            new Color(0.25f, 0.30f, 0.42f),
            new Color(0.45f, 0.36f, 0.58f),
            new Color(0.18f, 0.36f, 0.22f),
        };

        public static readonly Color Neutral = new Color(0.58f, 0.6f, 0.68f);

        public static Color Of(int faction) => faction >= 0 && faction < Primary.Length ? Primary[faction] : Neutral;

        public static string Hex(int faction) => ColorUtility.ToHtmlStringRGB(Of(faction));
    }
}
