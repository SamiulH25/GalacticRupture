using System.Collections.Generic;
using GalacticRupture.Sim.Strategic;
using UnityEngine;

namespace GalacticRupture.Game
{
    // The galaxy map, drawn in 3D: stars as glows, territory as coloured haze under each
    // owned system, lanes as faint lines, fleets as bright markers on the lanes.
    public sealed class MapView : MonoBehaviour
    {
        public const float Scale = 100f; // map units per light-year

        public GameArt Art;
        public Material LineMaterial;

        public static readonly Color PlayerColour = new Color(0.35f, 0.75f, 1f);
        public static readonly Color EnemyColour = new Color(1f, 0.3f, 0.26f);
        public static readonly Color NeutralColour = new Color(0.62f, 0.62f, 0.68f);

        public static Color FactionColour(int faction) =>
            faction == Campaign.PlayerFaction ? PlayerColour : faction == Campaign.EnemyFaction ? EnemyColour : NeutralColour;

        sealed class SystemVis
        {
            public Transform Glow, Core;
            public MeshRenderer GlowR, CoreR, Territory;
            public LineRenderer Ring, Siege;
            public int ShownOwner = -2;
        }

        sealed class FleetVis
        {
            public Transform Marker;
            public LineRenderer Route;
            public Vector3 Shown;
            public bool Placed;
        }

        Campaign _c;
        readonly List<SystemVis> _systems = new List<SystemVis>();
        readonly Dictionary<int, FleetVis> _fleets = new Dictionary<int, FleetVis>();
        readonly List<int> _gone = new List<int>();
        LineRenderer _selFleet, _selSystem;
        MaterialPropertyBlock _block;
        static readonly int ColorId = Shader.PropertyToID("_Color");
        Mesh _marker;

        public int SelectedFleet = -1;
        public int SelectedSystem = -1;

        public static Vector3 ToMap(System.Numerics.Vector3 ly) => new Vector3(ly.X, ly.Y, ly.Z) * Scale;

        public Vector3 SystemPosition(int id) => ToMap(_c.Galaxy.Systems[id].Position);

        public Vector3 FleetPosition(int fleetId) =>
            _fleets.TryGetValue(fleetId, out var v) ? v.Shown : Vector3.zero;

        public IEnumerable<int> FleetIds => _fleets.Keys;

        public void Build(Campaign c)
        {
            _c = c;
            _block = new MaterialPropertyBlock();
            _marker = Octahedron();
            foreach (Transform t in transform) Destroy(t.gameObject);
            _systems.Clear();
            _fleets.Clear();

            foreach (var lane in c.Galaxy.Lanes)
            {
                var lr = Line("Lane", 2, 16f, new Color(0.45f, 0.55f, 0.8f, 0.28f));
                lr.SetPosition(0, SystemPosition(lane.A));
                lr.SetPosition(1, SystemPosition(lane.B));
            }

            foreach (var s in c.Galaxy.Systems)
            {
                var v = new SystemVis();
                Vector3 p = SystemPosition(s.Id);
                Color star = StarColour(s.Name);
                v.GlowR = Quad($"{s.Name} glow", p, 260f + s.Tier * 70f, Art.Flash, star * 0.55f);
                v.Glow = v.GlowR.transform;
                v.CoreR = Quad($"{s.Name} core", p, 70f + s.Tier * 15f, Art.Flash, Color.white * 1.6f);
                v.Core = v.CoreR.transform;
                v.Territory = Quad($"{s.Name} territory", p + Vector3.down * 60f, 2600f, Art.Flash, Color.black);
                v.Territory.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                v.Ring = Circle(p, 280f, 14f, NeutralColour);
                v.Siege = Line("Siege", 2, 26f, EnemyColour);
                _systems.Add(v);
            }

            _selFleet = Circle(Vector3.zero, 120f, 10f, Color.white);
            _selSystem = Circle(Vector3.zero, 380f, 12f, Color.white);
        }

        static Color StarColour(string name)
        {
            // Stable per name: red dwarfs are common, a few bright white and blue stars.
            int h = 0;
            foreach (char ch in name) h = h * 31 + ch;
            float t = ((h & 0xffff) / 65535f);
            if (t < 0.45f) return new Color(1f, 0.55f, 0.35f);
            if (t < 0.75f) return new Color(1f, 0.85f, 0.65f);
            if (t < 0.92f) return new Color(0.95f, 0.95f, 1f);
            return new Color(0.6f, 0.75f, 1f);
        }

        MeshRenderer Quad(string name, Vector3 pos, float size, Material mat, Color colour)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * size;
            go.AddComponent<MeshFilter>().sharedMesh = Meshes.Quad;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            SetColour(r, colour);
            return r;
        }

        void SetColour(Renderer r, Color c)
        {
            _block.Clear();
            _block.SetColor(ColorId, c);
            r.SetPropertyBlock(_block);
        }

        LineRenderer Line(string name, int points, float width, Color colour)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = LineMaterial;
            lr.useWorldSpace = true;
            lr.positionCount = points;
            lr.widthMultiplier = width;
            lr.startColor = lr.endColor = colour;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.numCapVertices = 2;
            return lr;
        }

        LineRenderer Circle(Vector3 centre, float radius, float width, Color colour)
        {
            const int n = 48;
            var lr = Line("Ring", n, width, colour);
            lr.loop = true;
            SetCircle(lr, centre, radius);
            return lr;
        }

        static void SetCircle(LineRenderer lr, Vector3 centre, float radius)
        {
            int n = lr.positionCount;
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2f / n;
                lr.SetPosition(i, centre + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius);
            }
        }

        static Mesh Octahedron()
        {
            var v = new[]
            {
                new Vector3(0, 1, 0), new Vector3(1, 0, 0), new Vector3(0, 0, 1),
                new Vector3(-1, 0, 0), new Vector3(0, 0, -1), new Vector3(0, -1, 0),
            };
            var t = new[] { 0, 2, 1, 0, 3, 2, 0, 4, 3, 0, 1, 4, 5, 1, 2, 5, 2, 3, 5, 3, 4, 5, 4, 1 };
            var m = new Mesh { name = "marker", vertices = v, triangles = t };
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        public void Refresh(Camera cam, float dt)
        {
            if (_c == null) return;
            Quaternion face = cam.transform.rotation;

            for (int i = 0; i < _systems.Count; i++)
            {
                var s = _c.Galaxy.Systems[i];
                var v = _systems[i];
                v.Glow.rotation = face;
                v.Core.rotation = face;
                if (v.ShownOwner != s.Owner)
                {
                    v.ShownOwner = s.Owner;
                    Color fc = FactionColour(s.Owner);
                    SetColour(v.Territory, s.Owner < 0 ? Color.black : fc * 0.3f);
                    v.Ring.startColor = v.Ring.endColor = new Color(fc.r, fc.g, fc.b, s.Owner < 0 ? 0.35f : 0.85f);
                }
                // Siege: an arc filling around the ring in the besieger's colour.
                if (s.Besieger >= 0 && s.Siege > 0f)
                {
                    int n = Mathf.Max(2, Mathf.CeilToInt(48 * s.Siege));
                    v.Siege.enabled = true;
                    v.Siege.positionCount = n;
                    Color bc = FactionColour(s.Besieger);
                    v.Siege.startColor = v.Siege.endColor = bc;
                    Vector3 c = SystemPosition(i);
                    for (int k = 0; k < n; k++)
                    {
                        float a = Mathf.PI * 0.5f - k * Mathf.PI * 2f * s.Siege / (n - 1);
                        v.Siege.SetPosition(k, c + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 330f);
                    }
                }
                else v.Siege.enabled = false;
            }

            // Fleets: markers stacked above their system, gliding along lanes.
            var stack = new Dictionary<int, int>();
            foreach (var f in _c.Fleets)
            {
                if (!f.Alive) continue;
                if (!_fleets.TryGetValue(f.Id, out var fv))
                {
                    fv = new FleetVis();
                    var go = new GameObject($"Fleet {f.Name}");
                    go.transform.SetParent(transform, false);
                    go.AddComponent<MeshFilter>().sharedMesh = _marker;
                    var r = go.AddComponent<MeshRenderer>();
                    r.sharedMaterial = Art.EngineGlow[f.Owner == Campaign.PlayerFaction ? 0 : 1];
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    fv.Marker = go.transform;
                    fv.Route = Line("Route", 2, 12f, FactionColour(f.Owner) * new Color(1, 1, 1, 0.6f));
                    _fleets[f.Id] = fv;
                }
                Vector3 target = ToMap(_c.FleetPosition(f));
                if (!f.InTransit)
                {
                    stack.TryGetValue(f.System, out int k);
                    stack[f.System] = k + 1;
                    target += new Vector3((k - 0.5f) * 160f, 170f, 0f);
                }
                else target += Vector3.up * 120f;
                fv.Shown = fv.Placed ? Vector3.Lerp(fv.Shown, target, 1f - Mathf.Exp(-dt * 6f)) : target;
                fv.Placed = true;
                fv.Marker.position = fv.Shown;
                fv.Marker.rotation = Quaternion.Euler(0f, Time.time * 40f, 0f);
                float size = 45f + Mathf.Sqrt(f.Strength) * 1.4f;
                fv.Marker.localScale = new Vector3(size * 0.7f, size, size * 0.7f);

                // Route: from here through every remaining system.
                if (f.InTransit)
                {
                    fv.Route.enabled = true;
                    fv.Route.positionCount = 2 + f.Route.Count;
                    fv.Route.SetPosition(0, fv.Shown);
                    fv.Route.SetPosition(1, SystemPosition(f.Next) + Vector3.up * 120f);
                    for (int i = 0; i < f.Route.Count; i++) fv.Route.SetPosition(2 + i, SystemPosition(f.Route[i]) + Vector3.up * 120f);
                }
                else fv.Route.enabled = false;
            }

            _gone.Clear();
            foreach (var kv in _fleets)
            {
                var f = _c.FleetById(kv.Key);
                if (f == null || !f.Alive) _gone.Add(kv.Key);
            }
            foreach (int id in _gone)
            {
                Destroy(_fleets[id].Marker.gameObject);
                Destroy(_fleets[id].Route.gameObject);
                _fleets.Remove(id);
            }

            _selFleet.enabled = SelectedFleet >= 0 && _fleets.ContainsKey(SelectedFleet);
            if (_selFleet.enabled) SetCircle(_selFleet, FleetPosition(SelectedFleet), 110f);
            _selSystem.enabled = SelectedSystem >= 0;
            if (_selSystem.enabled) SetCircle(_selSystem, SystemPosition(SelectedSystem), 380f);
        }
    }
}
