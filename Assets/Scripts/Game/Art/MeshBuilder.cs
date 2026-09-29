using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GalacticRupture.Game
{
    // Flat-shaded hard-surface mesh assembly: lofts, chamfered boxes, cylinders. Faces are
    // oriented outward automatically and get box-projected UVs in metres / UvScale, so panel
    // textures stay a constant size across every part.
    public sealed class MeshBuilder
    {
        public const float UvScale = 24f;

        readonly List<Vector3> _v = new List<Vector3>();
        readonly List<Vector3> _n = new List<Vector3>();
        readonly List<Vector2> _uv = new List<Vector2>();
        readonly List<Color> _c = new List<Color>();
        readonly List<int>[] _subs;

        public Matrix4x4 Transform = Matrix4x4.identity;
        public Color VertexColor = Color.white;

        public MeshBuilder(int submeshes)
        {
            _subs = new List<int>[submeshes];
            for (int i = 0; i < submeshes; i++) _subs[i] = new List<int>();
        }

        public void Tri(Vector3 a, Vector3 b, Vector3 c, int sub)
        {
            a = Transform.MultiplyPoint3x4(a);
            b = Transform.MultiplyPoint3x4(b);
            c = Transform.MultiplyPoint3x4(c);
            Vector3 n = Vector3.Cross(b - a, c - a).normalized;
            int i = _v.Count;
            Add(a, n); Add(b, n); Add(c, n);
            _subs[sub].Add(i); _subs[sub].Add(i + 1); _subs[sub].Add(i + 2);
        }

        // Quad a-b-c-d, flipped if needed so its normal points along `outward` (local space).
        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 outward, int sub)
        {
            Vector3 n = Vector3.Cross(b - a, c - a);
            if (n.sqrMagnitude < 1e-10f) n = Vector3.Cross(c - a, d - a);
            if (Vector3.Dot(n, outward) < 0f)
            {
                (b, d) = (d, b);
            }
            Tri(a, b, c, sub);
            Tri(a, c, d, sub);
        }

        void Add(Vector3 p, Vector3 n)
        {
            _v.Add(p);
            _n.Add(n);
            _c.Add(VertexColor);
            float ax = Mathf.Abs(n.x), ay = Mathf.Abs(n.y), az = Mathf.Abs(n.z);
            Vector2 uv = ax >= ay && ax >= az ? new Vector2(p.z, p.y)
                : ay >= az ? new Vector2(p.x, p.z)
                : new Vector2(p.x, p.y);
            _uv.Add(uv / UvScale);
        }

        // Lofts rings of equal point count placed at successive z stations along local +Z.
        // Rings are (x, y) outlines, convex, in any winding.
        public void Loft(IList<Vector2[]> rings, IList<float> zs, int sub, bool capStart = true, bool capEnd = true)
        {
            for (int r = 0; r + 1 < rings.Count; r++)
            {
                var a = rings[r];
                var b = rings[r + 1];
                Vector2 ca = Centroid(a), cb = Centroid(b);
                for (int i = 0; i < a.Length; i++)
                {
                    int j = (i + 1) % a.Length;
                    var p0 = new Vector3(a[i].x, a[i].y, zs[r]);
                    var p1 = new Vector3(a[j].x, a[j].y, zs[r]);
                    var p2 = new Vector3(b[j].x, b[j].y, zs[r + 1]);
                    var p3 = new Vector3(b[i].x, b[i].y, zs[r + 1]);
                    Vector2 mid = (a[i] + a[j] + b[i] + b[j]) * 0.25f - (ca + cb) * 0.5f;
                    Quad(p0, p1, p2, p3, new Vector3(mid.x, mid.y, 0f), sub);
                }
            }
            if (capStart) Cap(rings[0], zs[0], -1f, sub);
            if (capEnd) Cap(rings[rings.Count - 1], zs[zs.Count - 1], 1f, sub);
        }

        void Cap(Vector2[] ring, float z, float dir, int sub)
        {
            Vector2 c = Centroid(ring);
            var centre = new Vector3(c.x, c.y, z);
            for (int i = 0; i < ring.Length; i++)
            {
                int j = (i + 1) % ring.Length;
                var a = new Vector3(ring[i].x, ring[i].y, z);
                var b = new Vector3(ring[j].x, ring[j].y, z);
                Vector3 n = Vector3.Cross(a - centre, b - centre);
                if (n.sqrMagnitude < 1e-10f) continue;
                if (n.z * dir < 0f) Tri(centre, b, a, sub);
                else Tri(centre, a, b, sub);
            }
        }

        static Vector2 Centroid(Vector2[] ring)
        {
            Vector2 s = Vector2.zero;
            foreach (var p in ring) s += p;
            return s / ring.Length;
        }

        // An axis-aligned box with its vertical edges chamfered, from zMin to zMax.
        public void ChamferBox(Vector3 centre, Vector3 size, float chamfer, int sub, float topTaper = 1f)
        {
            float hx = size.x * 0.5f, hy = size.y * 0.5f, hz = size.z * 0.5f;
            float cx = Mathf.Min(chamfer, hx * 0.49f), cy = Mathf.Min(chamfer, hy * 0.49f);
            float tx = hx * topTaper;
            var ring = new[]
            {
                new Vector2(centre.x + tx - cx, centre.y + hy),
                new Vector2(centre.x + hx, centre.y + hy - cy),
                new Vector2(centre.x + hx, centre.y - hy + cy),
                new Vector2(centre.x + hx - cx, centre.y - hy),
                new Vector2(centre.x - hx + cx, centre.y - hy),
                new Vector2(centre.x - hx, centre.y - hy + cy),
                new Vector2(centre.x - hx, centre.y + hy - cy),
                new Vector2(centre.x - tx + cx, centre.y + hy),
            };
            Loft(new[] { ring, ring }, new[] { centre.z - hz, centre.z + hz }, sub);
        }

        public void Box(Vector3 centre, Vector3 size, int sub) => ChamferBox(centre, size, 0.001f, sub);

        // A cylinder (or cone) between two points.
        public void Cylinder(Vector3 from, Vector3 to, float r0, float r1, int segments, int sub, bool caps = true)
        {
            Vector3 axis = to - from;
            float len = axis.magnitude;
            if (len < 1e-5f) return;
            var saved = Transform;
            Transform = saved * Matrix4x4.TRS(from, Quaternion.LookRotation(axis / len,
                Mathf.Abs(Vector3.Dot(axis / len, Vector3.up)) > 0.99f ? Vector3.forward : Vector3.up), Vector3.one);
            var a = new Vector2[segments];
            var b = new Vector2[segments];
            for (int i = 0; i < segments; i++)
            {
                float t = (i + 0.5f) / segments * Mathf.PI * 2f;
                var d = new Vector2(Mathf.Cos(t), Mathf.Sin(t));
                a[i] = d * r0;
                b[i] = d * r1;
            }
            Loft(new[] { a, b }, new[] { 0f, len }, sub, caps, caps);
            Transform = saved;
        }

        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh { name = name };
            if (_v.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(_v);
            mesh.SetNormals(_n);
            mesh.SetUVs(0, _uv);
            mesh.SetColors(_c);
            mesh.subMeshCount = _subs.Length;
            for (int i = 0; i < _subs.Length; i++) mesh.SetTriangles(_subs[i], i);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }
    }
}
