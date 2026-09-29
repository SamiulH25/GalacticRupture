using UnityEditor;
using UnityEngine;

namespace GalacticRupture.EditorTools
{
    // Import settings for the ship models exported by Tools/blender: metres, Y up, no
    // animation, no cameras or lights. Material slots keep their Blender names (Paint, Dark,
    // Trim, Windows, Engine, Heat) so the game can swap in the faction materials by name.
    public sealed class ModelImport : AssetPostprocessor
    {
        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith("Assets/Art/Models/")) return;
            var imp = (ModelImporter)assetImporter;
            imp.globalScale = 1f;
            imp.useFileScale = true;
            imp.bakeAxisConversion = true;
            imp.importAnimation = false;
            imp.importCameras = false;
            imp.importLights = false;
            imp.importBlendShapes = false;
            imp.animationType = ModelImporterAnimationType.None;
            imp.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            imp.materialLocation = ModelImporterMaterialLocation.InPrefab;
            imp.importNormals = ModelImporterNormals.Import;
            imp.importTangents = ModelImporterTangents.CalculateMikk;
            imp.meshCompression = ModelImporterMeshCompression.Off;
            imp.isReadable = false;
            imp.sortHierarchyByName = false;
        }
    }

    public static class ModelCheck
    {
        [MenuItem("Galactic Rupture/Report Models")]
        public static void Report()
        {
            AssetDatabase.Refresh();
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { "Assets/Art/Models" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Debug.Log($"[models] {path}");
                Dump(go.transform, 1);
            }
        }

        static void Dump(Transform t, int depth)
        {
            string pad = new string(' ', depth * 2);
            var mf = t.GetComponent<MeshFilter>();
            var mr = t.GetComponent<MeshRenderer>();
            string mesh = mf != null && mf.sharedMesh != null
                ? $" mesh bounds c={mf.sharedMesh.bounds.center:F1} s={mf.sharedMesh.bounds.size:F1} tris={mf.sharedMesh.triangles.Length / 3} subs={mf.sharedMesh.subMeshCount}"
                : "";
            string mats = mr != null ? " mats=" + string.Join(",", System.Array.ConvertAll(mr.sharedMaterials, m => m ? m.name : "null")) : "";
            Debug.Log($"[models] {pad}{t.name} pos={t.localPosition:F2} rot={t.localEulerAngles:F1} scale={t.localScale:F2}{mesh}{mats}");
            foreach (Transform c in t) Dump(c, depth + 1);
        }
    }
}
