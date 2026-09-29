using UnityEngine;

namespace GalacticRupture.Game
{
    // Points the skybox's star at the key light, so the sun you see is the one lighting the hulls.
    [ExecuteAlways]
    public sealed class SkyController : MonoBehaviour
    {
        public Light Sun;
        static readonly int SunDir = Shader.PropertyToID("_GR_SunDir");

        void Update()
        {
            if (Sun != null) Shader.SetGlobalVector(SunDir, -Sun.transform.forward);
        }
    }
}
