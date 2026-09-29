using System.Collections.Generic;
using GalacticRupture.Sim;
using UnityEngine;
using UnityEngine.Rendering;

namespace GalacticRupture.Game
{
    // Draws every round in flight as an instanced tracer streak, grouped by look.
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
        readonly List<Matrix4x4>[] _buckets = new List<Matrix4x4>[4];
        Look[] _looks;
        readonly Matrix4x4[] _scratch = new Matrix4x4[Batch];

        public void Init(GameArt art)
        {
            _art = art;
            _looks = new[]
            {
                new Look { Material = art.TracerBallistic, Length = 70f, Width = 2.2f },
                new Look { Material = art.TracerEnergy, Length = 34f, Width = 1.3f },
                new Look { Material = art.TracerPointDefence, Length = 16f, Width = 0.5f },
                new Look { Material = art.TracerEnemy, Length = 26f, Width = 1.1f },
            };
            for (int i = 0; i < _buckets.Length; i++) _buckets[i] = new List<Matrix4x4>(4096);
        }

        static int LookFor(in Projectile p)
        {
            if (p.Side != TurretToy.PlayerSide) return 3;
            return p.Class switch
            {
                MountClass.Line => 0,
                MountClass.PointDefence => 2,
                _ => 1,
            };
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

            for (int b = 0; b < _buckets.Length; b++)
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
