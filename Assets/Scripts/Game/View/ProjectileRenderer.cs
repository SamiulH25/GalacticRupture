using System.Collections.Generic;
using GalacticRupture.Sim;
using UnityEngine;
using UnityEngine.Rendering;

namespace GalacticRupture.Game
{
    // Draws every round in flight as an instanced tracer streak, grouped by look: each weapon
    // family and size has its own streak (WeaponLook), and energy bolts take the colour of the
    // side that fired them.
    public sealed class ProjectileRenderer : MonoBehaviour
    {
        struct Look
        {
            public Material Material;
            public float Length;
            public float Width;
        }

        const int Batch = 1023;

        GameArt _art;
        readonly List<Look> _looks = new List<Look>();
        readonly List<List<Matrix4x4>> _buckets = new List<List<Matrix4x4>>();
        readonly Dictionary<int, int> _index = new Dictionary<int, int>();
        readonly Matrix4x4[] _scratch = new Matrix4x4[Batch];

        public void Init(GameArt art) => _art = art;

        int LookFor(in Projectile p)
        {
            var wl = WeaponLook.Of(p.Family, p.Size);
            bool player = p.Side == TurretToy.PlayerSide;
            int key = ((int)p.Family * 3 + (int)p.Size) * 2 + (wl.SideColoured && !player ? 1 : 0);
            if (_index.TryGetValue(key, out int i)) return i;

            var source = wl.SideColoured ? _art.TracerEnergy : _art.TracerBallistic;
            var m = new Material(source) { name = $"Tracer_{p.Family}_{p.Size}", enableInstancing = true };
            Color c = wl.SideColoured ? (player ? WeaponLook.PlayerBolt : WeaponLook.EnemyBolt) : wl.Color;
            m.SetColor("_Color", c);
            m.SetFloat("_Intensity", wl.Intensity);
            if (p.Family == WeaponFamily.Ion) m.SetFloat("_Softness", 4f);
            i = _looks.Count;
            _looks.Add(new Look { Material = m, Length = wl.Length, Width = wl.Width });
            _buckets.Add(new List<Matrix4x4>(1024));
            _index[key] = i;
            return i;
        }

        public void Draw(World world, float alpha)
        {
            foreach (var b in _buckets) b.Clear();
            var list = world.Projectiles;
            for (int i = 0; i < list.Count; i++)
            {
                var p = list[i];
                int look = LookFor(p);
                var l = _looks[look];
                Vector3 head = System.Numerics.Vector3.Lerp(p.PrevPosition, p.Position, alpha).ToUnity();
                Vector3 vel = p.Velocity.ToUnity();
                float speed = vel.magnitude;
                if (speed < 1e-3f) continue;
                Vector3 dir = vel / speed;
                _buckets[look].Add(Matrix4x4.TRS(head - dir * l.Length, Quaternion.LookRotation(dir), new Vector3(l.Width, l.Width, l.Length)));
            }

            for (int b = 0; b < _buckets.Count; b++)
            {
                var bucket = _buckets[b];
                if (bucket.Count == 0) continue;
                var rp = new RenderParams(_looks[b].Material)
                {
                    shadowCastingMode = ShadowCastingMode.Off,
                    receiveShadows = false,
                    worldBounds = new Bounds(Vector3.zero, Vector3.one * 200000f),
                };
                for (int start = 0; start < bucket.Count; start += Batch)
                {
                    int n = Mathf.Min(Batch, bucket.Count - start);
                    bucket.CopyTo(start, _scratch, 0, n);
                    Graphics.RenderMeshInstanced(rp, Meshes.Tracer, 0, _scratch, n);
                }
            }
        }
    }
}
