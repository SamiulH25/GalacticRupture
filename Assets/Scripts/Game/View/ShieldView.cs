using UnityEngine;

namespace GalacticRupture.Game
{
    // The shield field around one ship: hidden at rest, it shows where it is struck and
    // collapses visibly when it fails. Hits are kept in the shield's own space so they ride
    // with the ship.
    public sealed class ShieldView : MonoBehaviour
    {
        const int MaxHits = 8;
        static readonly int HitsId = Shader.PropertyToID("_Hits");
        static readonly int StrengthId = Shader.PropertyToID("_Strength");
        static readonly int ActivityId = Shader.PropertyToID("_Activity");

        readonly Vector4[] _hits = new Vector4[MaxHits];
        MeshRenderer _renderer;
        MaterialPropertyBlock _block;
        int _next;
        float _activity;

        public static ShieldView Create(Transform ship, Material material, Bounds hullBounds)
        {
            var go = new GameObject("Shield");
            go.transform.SetParent(ship, false);
            go.transform.localPosition = hullBounds.center;
            // Sphere mesh has radius 0.5: scale to an ellipsoid a little outside the hull.
            go.transform.localScale = hullBounds.size * 1.12f + Vector3.one * 8f;
            go.AddComponent<MeshFilter>().sharedMesh = Meshes.Sphere;
            var view = go.AddComponent<ShieldView>();
            view._renderer = go.AddComponent<MeshRenderer>();
            view._renderer.sharedMaterial = material;
            view._renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            view._renderer.receiveShadows = false;
            view._renderer.enabled = false;
            view._block = new MaterialPropertyBlock();
            for (int i = 0; i < MaxHits; i++) view._hits[i] = new Vector4(0f, 0f, 0f, -1f);
            return view;
        }

        // Where a hit at `worldPoint` meets the shell.
        public Vector3 SurfacePoint(Vector3 worldPoint)
        {
            Vector3 local = transform.InverseTransformPoint(worldPoint);
            if (local.sqrMagnitude < 1e-6f) local = Vector3.forward;
            return transform.TransformPoint(local.normalized * 0.5f);
        }

        public void Hit(Vector3 worldPoint)
        {
            Vector3 local = transform.InverseTransformPoint(worldPoint);
            if (local.sqrMagnitude < 1e-6f) local = Vector3.forward;
            local = local.normalized * 0.5f;
            _hits[_next] = new Vector4(local.x, local.y, local.z, 0f);
            _next = (_next + 1) % MaxHits;
            _activity = 1f;
            _renderer.enabled = true;
        }

        // Shield failure: the whole shell lights and ripples out from the breach.
        public void Collapse(Vector3 worldPoint)
        {
            Hit(worldPoint);
            for (int i = 0; i < 5; i++)
                Hit(transform.TransformPoint(Random.onUnitSphere * 0.5f));
            _activity = 2.5f;
        }

        public void Tick(float dt, float strength)
        {
            if (!_renderer.enabled) return;
            bool any = false;
            for (int i = 0; i < MaxHits; i++)
            {
                if (_hits[i].w < 0f) continue;
                _hits[i].w += dt;
                if (_hits[i].w > 1.2f) _hits[i].w = -1f;
                else any = true;
            }
            _activity = Mathf.Max(0f, _activity - dt * 1.5f);
            if (!any && _activity <= 0f)
            {
                _renderer.enabled = false;
                return;
            }
            _block.SetVectorArray(HitsId, _hits);
            _block.SetFloat(StrengthId, strength);
            _block.SetFloat(ActivityId, Mathf.Min(_activity, 1.5f));
            _renderer.SetPropertyBlock(_block);
        }
    }
}
