using UnityEditor;
using UnityEngine;

namespace GalacticRupture.EditorTools
{
    /// Enforces the import settings the fleet cursors need.
    ///
    /// These are 32x32 sprites drawn to be seen at 1:1, so the defaults Unity applies are
    /// wrong in three ways that all show up as a visibly worse cursor: bilinear filtering
    /// blurs the hard pixels, mipmaps fade the outline at distance, and DXT compression
    /// puts colour fringes on the 1px ink edge against a transparent background. Hand-
    /// editing the .meta would work until someone reimported, so it is set here instead.
    public sealed class CursorImporter : AssetPostprocessor
    {
        const string Dir = "Assets/Resources/Cursors/";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Dir)) return;
            var imp = (TextureImporter)assetImporter;

            // This is the one that actually makes the cursor work. Unity uploads a
            // hardware cursor from a texture typed Cursor; anything else is uploaded as
            // ordinary streaming data and the OS gets an unreadable buffer, which shows up
            // as coloured garbage lines instead of the sprite.
            imp.textureType = TextureImporterType.Cursor;

            // Point filter: nearest neighbour, so a pixel stays a pixel.
            imp.filterMode = FilterMode.Point;

            // Keeps the edge pixels from bleeding their colour into the transparent ones,
            // which is what makes a hard 1px outline look ragged against a dark sky.
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.anisoLevel = 0;

            // Uncompressed. Alpha is load-bearing here -- it is what makes the cursor's
            // shape read against whatever it is over -- and DXT artifacts along that edge
            // are very visible at this size.
            imp.textureCompression = TextureImporterCompression.Uncompressed;

            // Alpha is data, not colour: sRGB would lighten the transparent pixels' edges
            // and shift the ink outline's apparent value.
            imp.sRGBTexture = false;

            // Clamped so the cursor is never sampled outside its own 32x32.
            imp.wrapMode = TextureWrapMode.Clamp;

            // Keep the 1:1 pixel grid at any UI scale; scaling is the UI's business.
            imp.npotScale = TextureImporterNPOTScale.None;
        }

        void OnPostprocessAllAssets(string[] imported, string[] _, string[] __, bool ___)
        {
            // Point filtering is the whole reason these read as pixel art, so assert it on
            // every import rather than trusting the folder to still hold the settings.
            foreach (var p in imported)
            {
                if (!p.StartsWith(Dir)) continue;
                if (AssetImporter.GetAtPath(p) is not TextureImporter ti) continue;
                bool ok = ti.textureType == TextureImporterType.Cursor
                          && ti.filterMode == FilterMode.Point
                          && !ti.mipmapEnabled
                          && ti.textureCompression == TextureImporterCompression.Uncompressed
                          && !ti.sRGBTexture;
                if (!ok)
                    Debug.LogWarning($"[cursor-import] {p} lost pixel-art settings"
                                     + $" (type={ti.textureType} filter={ti.filterMode}"
                                     + $" mip={ti.mipmapEnabled}"
                                     + $" srgb={ti.sRGBTexture} compress={ti.textureCompression})");
            }
        }
    }
}