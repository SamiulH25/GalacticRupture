using UnityEngine;

namespace GalacticRupture.Game
{
    // Small shared meshes for effects.
    public static class Meshes
    {
        static Mesh _tracer, _plume, _sphere, _quad;

        // UV sphere, radius 0.5.
        public static Mesh Sphere => _sphere ??= BuildSphere(48, 24);

        // Unit quad in XY facing -Z, uv 0..1.
        public static Mesh Quad => _quad ??= new Mesh
        {
            name = "quad",
            vertices = new[] { new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0), new Vector3(0.5f, 0.5f, 0), new Vector3(-0.5f, 0.5f, 0) },
            uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) },
            normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back },
            triangles = new[] { 0, 2, 1, 0, 3, 2 },
        };

        static Mesh BuildSphere(int lon, int lat)
        {
            var v = new Vector3[(lon + 1) * (lat + 1)];
            var n = new Vector3[v.Length];
            var uv = new Vector2[v.Length];
            for (int y = 0; y <= lat; y++)
            for (int x = 0; x <= lon; x++)
            {
                float a = x / (float)lon * Mathf.PI * 2f, b = y / (float)lat * Mathf.PI;
                var d = new Vector3(Mathf.Sin(b) * Mathf.Cos(a), Mathf.Cos(b), Mathf.Sin(b) * Mathf.Sin(a));
                int i = y * (lon + 1) + x;
                v[i] = d * 0.5f;
                n[i] = d;
                uv[i] = new Vector2(x / (float)lon, y / (float)lat);
            }
            var t = new int[lon * lat * 6];
            int k = 0;
            for (int y = 0; y < lat; y++)
            for (int x = 0; x < lon; x++)
            {
                int i0 = y * (lon + 1) + x, i1 = i0 + 1, i2 = i0 + lon + 1, i3 = i2 + 1;
                t[k++] = i0; t[k++] = i1; t[k++] = i2;
                t[k++] = i1; t[k++] = i3; t[k++] = i2;
            }
            var m = new Mesh { name = "sphere", vertices = v, normals = n, uv = uv, triangles = t };
            m.RecalculateBounds();
            return m;
        }

        // Three crossed quads along +Z from 0 (tail) to 1 (head); u across, v along.
        public static Mesh Tracer => _tracer ??= CrossedQuads("tracer", 0.5f, 0f, 1f, false);

        // Engine plume: crossed quads along +Z, bright at the nozzle (z = 0) fading to z = 1.
        public static Mesh Plume => _plume ??= CrossedQuads("plume", 0.5f, 0f, 1f, true);

        static Mesh CrossedQuads(string name, float halfWidth, float z0, float z1, bool reverseV)
        {
            var v = new Vector3[12];
            var uv = new Vector2[12];
            var tris = new int[18];
            for (int q = 0; q < 3; q++)
            {
                float a = q * Mathf.PI / 3f;
                var side = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * halfWidth;
                int i = q * 4;
                v[i] = -side + new Vector3(0, 0, z0);
                v[i + 1] = side + new Vector3(0, 0, z0);
                v[i + 2] = side + new Vector3(0, 0, z1);
                v[i + 3] = -side + new Vector3(0, 0, z1);
                float vt = reverseV ? 1f : 0f, vh = reverseV ? 0f : 1f;
                uv[i] = new Vector2(0, vt);
                uv[i + 1] = new Vector2(1, vt);
                uv[i + 2] = new Vector2(1, vh);
                uv[i + 3] = new Vector2(0, vh);
                int t = q * 6;
                tris[t] = i; tris[t + 1] = i + 1; tris[t + 2] = i + 2;
                tris[t + 3] = i; tris[t + 4] = i + 2; tris[t + 5] = i + 3;
            }
            var colors = new Color[12];
            for (int i = 0; i < colors.Length; i++) colors[i] = Color.white;
            var m = new Mesh { name = name, vertices = v, uv = uv, colors = colors, triangles = tris };
            m.bounds = new Bounds(new Vector3(0, 0, 0.5f), new Vector3(2f, 2f, 2f));
            return m;
        }
    }

    public static class VectorExt
    {
        public static Vector3 ToUnity(this System.Numerics.Vector3 v) => new Vector3(v.X, v.Y, v.Z);
        public static System.Numerics.Vector3 ToSim(this Vector3 v) => new System.Numerics.Vector3(v.x, v.y, v.z);
    }
}
