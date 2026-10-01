using UnityEngine;

namespace GalacticRupture.Game
{
    // Points the skybox's star at the key light, so the sun you see is the one lighting the hulls.
    // In play it bakes the sky's nebula into a lat-long texture once, so the sky costs a texture
    // read per pixel rather than twenty octaves of noise (Hidden/GR/SkyBake, GR_SKY_BAKED).
    [ExecuteAlways]
    public sealed class SkyController : MonoBehaviour
    {
        public Light Sun;
        public Shader BakeShader;   // Hidden/GR/SkyBake
        static readonly int SunDir = Shader.PropertyToID("_GR_SunDir");
        static readonly int NebulaTex = Shader.PropertyToID("_GR_SkyNebula");
        const string BakedKeyword = "GR_SKY_BAKED";

        RenderTexture _baked;

        void OnEnable()
        {
            if (Application.isPlaying) Bake();
        }

        void OnDisable()
        {
            Shader.DisableKeyword(BakedKeyword);
            if (_baked != null)
            {
                _baked.Release();
                Destroy(_baked);
                _baked = null;
            }
        }

        // Call again after changing the sky material's nebula settings.
        public void Bake()
        {
            var sky = RenderSettings.skybox;
            if (sky == null || BakeShader == null) return;
            if (_baked == null)
            {
                _baked = new RenderTexture(2048, 1024, 0, RenderTextureFormat.ARGBHalf)
                {
                    name = "Sky Nebula",
                    wrapModeU = TextureWrapMode.Repeat,
                    wrapModeV = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                    useMipMap = false,
                };
                _baked.Create();
            }
            var bake = new Material(BakeShader);
            bake.CopyPropertiesFromMaterial(sky);
            Graphics.Blit(null, _baked, bake, 0);
            Destroy(bake);
            Shader.SetGlobalTexture(NebulaTex, _baked);
            Shader.EnableKeyword(BakedKeyword);
            // Reflections were captured from the sky before it was baked: take them again.
            foreach (var probe in FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None))
                if (probe.mode == UnityEngine.Rendering.ReflectionProbeMode.Realtime) probe.RenderProbe();
        }

        void Update()
        {
            if (Sun != null) Shader.SetGlobalVector(SunDir, -Sun.transform.forward);
        }
    }
}
