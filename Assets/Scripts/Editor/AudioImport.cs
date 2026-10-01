using UnityEditor;
using UnityEngine;

namespace GalacticRupture.EditorTools
{
    // Music streams from disk; effects decompress on load. Set by folder so new clips follow the rule.
    public sealed class AudioImport : AssetPostprocessor
    {
        void OnPreprocessAudio()
        {
            var importer = (AudioImporter)assetImporter;
            if (!assetPath.StartsWith("Assets/Resources/Audio/")) return;
            var s = importer.defaultSampleSettings;
            bool music = assetPath.Contains("/Music/");
            s.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = music ? 0.6f : 0.7f;
            importer.defaultSampleSettings = s;
            importer.forceToMono = !music;
        }
    }
}
