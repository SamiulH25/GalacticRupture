using System.Collections.Generic;
using GalacticRupture.Sim;
using UnityEngine;
using UnityEngine.Rendering;

namespace GalacticRupture.Game
{
    // The battlefield's terrain: the sim's asteroids as tumbling rocks with a scatter of small
    // debris around them (instanced, no gameplay), nebulae as banks of soft glowing cloud that
    // thin out as the camera enters them, and a planet hanging off the field for scale.
    public sealed class TerrainView : MonoBehaviour
    {
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int SmoothId = Shader.PropertyToID("_Smoothness");

        sealed class Rock
        {
            public Transform T;
            public Vector3 Spin;
        }

        sealed class Puff
        {
            public Transform T;
            public MeshRenderer R;
            public Color Colour;
            public float Size;
        }

        readonly List<Rock> _rocks = new List<Rock>();
        readonly List<Puff> _puffs = new List<Puff>();
        readonly List<Matrix4x4>[] _debris = { new List<Matrix4x4>(), new List<Matrix4x4>(), new List<Matrix4x4>(), new List<Matrix4x4>() };
        readonly Matrix4x4[] _scratch = new Matrix4x4[1023];
        Material _rockMat, _cloudMat, _planetMat;
        MaterialPropertyBlock _block;
        Camera _cam;

        static Mesh[] _rockMeshes;

        public static TerrainView Create(Transform parent, GameArt art, World world, int seed)
        {
            var go = new GameObject("Terrain");
            go.transform.SetParent(parent, false);
            var v = go.AddComponent<TerrainView>();
            v.Build(art, world, seed);
            return v;
        }

        void Build(GameArt art, World world, int seed)
        {
            _block = new MaterialPropertyBlock();
            _rockMeshes ??= new[] { RockMesh(11), RockMesh(23), RockMesh(37), RockMesh(59) };
            _rockMat = new Material(art.Planet) { name = "Rock", enableInstancing = true };
            _rockMat.SetColor(BaseColorId, new Color(0.3f, 0.27f, 0.24f));
            _rockMat.SetFloat(SmoothId, 0.12f);
            _cloudMat = new Material(art.Flash) { name = "Nebula" };

            var rng = new System.Random(seed);
            foreach (var o in world.Obstacles)
            {
                if (o.Kind == ObstacleKind.Asteroid) AddRock(o, rng);
                else AddNebula(o, rng);
            }
            AddPlanet(art, rng);
        }

        void AddRock(in Obstacle o, System.Random rng)
        {
            var go = new GameObject("Asteroid");
            go.transform.SetParent(transform, false);
            go.transform.position = o.Position.ToUnity();
            go.transform.rotation = Random3(rng);
            // The mesh is a unit-radius lump; stretch it a little so no two read the same.
            go.transform.localScale = new Vector3(o.Radius * Rand(rng, 0.9f, 1.15f), o.Radius * Rand(rng, 0.8f, 1f), o.Radius * Rand(rng, 0.9f, 1.15f));
            go.AddComponent<MeshFilter>().sharedMesh = _rockMeshes[o.Seed & 3];
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = _rockMat;
            r.shadowCastingMode = ShadowCastingMode.On;
            _rocks.Add(new Rock { T = go.transform, Spin = new Vector3(Rand(rng, -1f, 1f), Rand(rng, -1f, 1f), Rand(rng, -1f, 1f)) * (40f / o.Radius) });

            // Gravel and boulders around it: scenery only.
            int n = Mathf.Clamp((int)(o.Radius / 10f), 8, 30);
            for (int i = 0; i < n; i++)
            {
                Vector3 d = Random.onUnitSphere;
                d.y *= 0.35f;
                var p = o.Position.ToUnity() + d.normalized * o.Radius * Rand(rng, 1.25f, 2.6f);
                float size = Rand(rng, 2f, 1f + o.Radius * 0.08f);
                _debris[rng.Next(4)].Add(Matrix4x4.TRS(p, Random3(rng), Vector3.one * size));
            }
        }

        // A bank of cloud: overlapping soft puffs, denser toward the middle.
        void AddNebula(in Obstacle o, System.Random rng)
        {
            var baseA = new Color(0.55f, 0.25f, 0.85f);
            var baseB = new Color(0.2f, 0.55f, 0.9f);
            int n = 26;
            for (int i = 0; i < n; i++)
            {
                Vector3 d = Random.insideUnitSphere;
                d.y *= 0.4f;
                var p = o.Position.ToUnity() + d * o.Radius * 0.85f;
                float size = o.Radius * Rand(rng, 0.7f, 1.4f);
                var go = new GameObject("Cloud");
                go.transform.SetParent(transform, false);
                go.transform.position = p;
                go.transform.localScale = Vector3.one * size;
                go.AddComponent<MeshFilter>().sharedMesh = Meshes.Quad;
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = _cloudMat;
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
                var c = Color.Lerp(baseA, baseB, (float)rng.NextDouble()) * Rand(rng, 0.05f, 0.1f);
                _puffs.Add(new Puff { T = go.transform, R = r, Colour = c, Size = size });
            }
            // A few sparks of lightning-lit gas deep inside.
            for (int i = 0; i < 4; i++)
            {
                var go = new GameObject("Cloud core");
                go.transform.SetParent(transform, false);
                go.transform.position = o.Position.ToUnity() + Random.insideUnitSphere * o.Radius * 0.4f;
                float size = o.Radius * 0.5f;
                go.transform.localScale = Vector3.one * size;
                go.AddComponent<MeshFilter>().sharedMesh = Meshes.Quad;
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = _cloudMat;
                r.shadowCastingMode = ShadowCastingMode.Off;
                _puffs.Add(new Puff { T = go.transform, R = r, Colour = new Color(0.8f, 0.6f, 1f) * 0.12f, Size = size });
            }
        }

        void AddPlanet(GameArt art, System.Random rng)
        {
            _planetMat = new Material(art.Planet) { name = "Backdrop planet" };
            var palette = new[]
            {
                new Color(0.42f, 0.36f, 0.3f), new Color(0.25f, 0.35f, 0.5f), new Color(0.5f, 0.3f, 0.22f),
                new Color(0.3f, 0.42f, 0.34f), new Color(0.55f, 0.5f, 0.42f),
            };
            _planetMat.SetColor(BaseColorId, palette[rng.Next(palette.Length)]);
            _planetMat.SetFloat(SmoothId, 0.2f);
            var go = new GameObject("Planet");
            go.transform.SetParent(transform, false);
            float side = rng.Next(2) == 0 ? -1f : 1f;
            go.transform.position = new Vector3(side * Rand(rng, 18000f, 30000f), Rand(rng, -9000f, -4000f), Rand(rng, 30000f, 45000f));
            go.transform.localScale = Vector3.one * Rand(rng, 16000f, 26000f);
            go.AddComponent<MeshFilter>().sharedMesh = Meshes.Sphere;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = _planetMat;
            r.shadowCastingMode = ShadowCastingMode.Off;
        }

        void LateUpdate()
        {
            if (_cam == null) _cam = Camera.main;
            float dt = Time.deltaTime;
            foreach (var r in _rocks) r.T.Rotate(r.Spin * dt, Space.Self);

            if (_cam != null && _puffs.Count > 0)
            {
                var face = _cam.transform.rotation;
                Vector3 cp = _cam.transform.position;
                foreach (var p in _puffs)
                {
                    p.T.rotation = face;
                    // Thin out as the camera comes into a puff, so flying through a nebula doesn't
                    // fill the screen with glow (or the GPU with overdraw).
                    float d = Vector3.Distance(cp, p.T.position);
                    float fade = Mathf.Clamp01((d - p.Size * 0.3f) / (p.Size * 0.9f));
                    p.R.enabled = fade > 0.02f;
                    if (!p.R.enabled) continue;
                    _block.SetColor(ColorId, p.Colour * fade);
                    p.R.SetPropertyBlock(_block);
                }
            }

            var rp = new RenderParams(_rockMat) { shadowCastingMode = ShadowCastingMode.Off, receiveShadows = true, worldBounds = new Bounds(Vector3.zero, Vector3.one * 60000f) };
            for (int k = 0; k < _debris.Length; k++)
            {
                var list = _debris[k];
                for (int start = 0; start < list.Count; start += _scratch.Length)
                {
                    int n = Mathf.Min(_scratch.Length, list.Count - start);
                    list.CopyTo(start, _scratch, 0, n);
                    Graphics.RenderMeshInstanced(rp, _rockMeshes[k], 0, _scratch, n);
                }
            }
        }

        void OnDestroy()
        {
            if (_rockMat != null) Destroy(_rockMat);
            if (_cloudMat != null) Destroy(_cloudMat);
            if (_planetMat != null) Destroy(_planetMat);
        }

        static float Rand(System.Random rng, float a, float b) => a + (b - a) * (float)rng.NextDouble();

        static Quaternion Random3(System.Random rng) => Quaternion.Euler(Rand(rng, 0f, 360f), Rand(rng, 0f, 360f), Rand(rng, 0f, 360f));

        // A lumpy unit-radius rock: a subdivided icosahedron pushed about by layered noise, flat
        // shaded so the facets catch the light.
        static Mesh RockMesh(int seed)
        {
            var verts = new List<Vector3>();
            var tris = new List<int>();
            float t = (1f + Mathf.Sqrt(5f)) / 2f;
            foreach (var v in new[]
            {
                new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
                new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
                new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1),
            }) verts.Add(v.normalized);
            tris.AddRange(new[]
            {
                0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
                3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1,
            });
            var cache = new Dictionary<long, int>();
            for (int level = 0; level < 3; level++)
            {
                var next = new List<int>();
                for (int i = 0; i < tris.Count; i += 3)
                {
                    int a = tris[i], b = tris[i + 1], c = tris[i + 2];
                    int ab = Mid(verts, cache, a, b), bc = Mid(verts, cache, b, c), ca = Mid(verts, cache, c, a);
                    next.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
                }
                tris = next;
            }
            var off = new Vector3(seed * 1.7f, seed * 3.1f, seed * 0.7f);
            for (int i = 0; i < verts.Count; i++)
            {
                var v = verts[i];
                float n = Noise(v * 1.3f + off) * 0.35f + Noise(v * 3.1f + off) * 0.15f + Noise(v * 7f + off) * 0.06f;
                // A crater or two.
                n -= Mathf.Max(0f, Vector3.Dot(v, new Vector3(Mathf.Sin(seed), 0.3f, Mathf.Cos(seed)).normalized) - 0.88f) * 1.6f;
                verts[i] = v * (0.82f + n);
            }
            // Flat shading: every triangle its own vertices, wound clockwise for Unity (the
            // icosahedron above is listed counter-clockwise).
            var fv = new Vector3[tris.Count];
            var ft = new int[tris.Count];
            for (int i = 0; i < tris.Count; i += 3)
            {
                fv[i] = verts[tris[i]];
                fv[i + 1] = verts[tris[i + 2]];
                fv[i + 2] = verts[tris[i + 1]];
                ft[i] = i;
                ft[i + 1] = i + 1;
                ft[i + 2] = i + 2;
            }
            var mesh = new Mesh { name = $"rock_{seed}", vertices = fv, triangles = ft };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static int Mid(List<Vector3> verts, Dictionary<long, int> cache, int a, int b)
        {
            long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
            if (cache.TryGetValue(key, out int i)) return i;
            verts.Add(((verts[a] + verts[b]) * 0.5f).normalized);
            cache[key] = verts.Count - 1;
            return verts.Count - 1;
        }

        static float Noise(Vector3 p)
        {
            float xy = Mathf.PerlinNoise(p.x, p.y), yz = Mathf.PerlinNoise(p.y + 31.7f, p.z), zx = Mathf.PerlinNoise(p.z + 71.3f, p.x);
            return (xy + yz + zx) / 3f - 0.5f;
        }
    }
}
