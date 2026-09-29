using UnityEngine;

namespace GalacticRupture.Game
{
    // Small shared meshes for effects.
    public static class Meshes
    {
        static Mesh _tracer, _plume;

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
