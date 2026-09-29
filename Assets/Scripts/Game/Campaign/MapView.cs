using System.Collections.Generic;
using GalacticRupture.Sim.Strategic;
using UnityEngine;

namespace GalacticRupture.Game
{
    // The galaxy map in 3D, drawn from what the player knows:
    //   a plotting plate with territory, frontiers and unexplored space (GR/MapPlate),
    //   stars with churning surfaces, their planets on orbits, stations at shipyards,
    //   lanes as flowing light in the colours of the powers at each end,
    //   fleets as small models of their lead ship; last-known enemy positions as ghosts.
    public sealed class MapView : MonoBehaviour
    {
        public const float Scale = 100f; // map units per light-year
        public const int Viewer = Campaign.PlayerFaction;

        public GameArt Art;
        public Material LineMaterial;

        public static Color FactionColour(int faction) => FactionStyle.Of(faction);

        sealed class SystemVis
        {
            public Transform Star, Corona;
            public MeshRenderer StarR, CoronaR;
            public readonly List<(Transform T, float Radius, float Speed, float Phase)> Planets = new List<(Transform, float, float, float)>();
            public Transform Station;
            public LineRenderer Ring, Siege, Battle;
            public Color StarColour;
        }

        sealed class FleetVis
        {
            public Transform Root, Model;
            public LineRenderer Drop, Base, Route;
            public Vector3 Shown;
            public bool Placed;
            public string HullId;
        }

        Campaign _c;
        readonly List<SystemVis> _systems = new List<SystemVis>();
        readonly List<(LineRenderer Line, int A, int B)> _lanes = new List<(LineRenderer, int, int)>();
        readonly Dictionary<int, FleetVis> _fleets = new Dictionary<int, FleetVis>();
        readonly Dictionary<int, Transform> _ghosts = new Dictionary<int, Transform>();
        readonly List<int> _gone = new List<int>();
        LineRenderer _selFleet, _selSystem;
        MaterialPropertyBlock _block;
        MeshRenderer _plate;
        Mesh _stationMesh;
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        readonly Vector4[] _sysData = new Vector4[32];
        readonly Vector4[] _sysFlags = new Vector4[32];
        readonly Vector4[] _colours = new Vector4[8];

        public int SelectedFleet = -1;
        public int SelectedSystem = -1;
        public bool ShowSupply;

        public static Vector3 ToMap(System.Numerics.Vector3 ly) => new Vector3(ly.X, ly.Y, ly.Z) * Scale;

        public Vector3 SystemPosition(int id) => ToMap(_c.Galaxy.Systems[id].Position);

        public Vector3 FleetPosition(int fleetId) =>
            _fleets.TryGetValue(fleetId, out var v) ? v.Shown : Vector3.zero;

        public IEnumerable<int> FleetIds => _fleets.Keys;

        public IEnumerable<KeyValuePair<int, Transform>> Ghosts => _ghosts;

        public void Build(Campaign c)
        {
            _c = c;
            _block = new MaterialPropertyBlock();
            _stationMesh = StationMesh();
            foreach (Transform t in transform) Destroy(t.gameObject);
            _systems.Clear();
            _fleets.Clear();
            _ghosts.Clear();
            _lanes.Clear();

            // The plate spans the map with a margin.
            Vector3 min = Vector3.one * 1e9f, max = -Vector3.one * 1e9f;
            foreach (var s in c.Galaxy.Systems)
            {
                min = Vector3.Min(min, ToMap(s.Position));
                max = Vector3.Max(max, ToMap(s.Position));
            }
            Vector3 centre = (min + max) * 0.5f;
            float span = Mathf.Max(max.x - min.x, max.z - min.z) + 5000f;
            var plate = new GameObject("Plate");
            plate.transform.SetParent(transform, false);
            plate.transform.position = new Vector3(centre.x, -140f, centre.z);
            plate.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            plate.transform.localScale = new Vector3(span, span, 1f);
            plate.AddComponent<MeshFilter>().sharedMesh = Meshes.Quad;
            _plate = plate.AddComponent<MeshRenderer>();
            _plate.sharedMaterial = Art.MapPlate;
            _plate.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Shader.SetGlobalVector("_GR_MapCentre", new Vector4(centre.x, centre.z, 0f, span * 0.5f));
            _plate.sharedMaterial.SetFloat("_SupplyRange", c.Factions[Viewer].SupplyRange * Scale);

            foreach (var lane in c.Galaxy.Lanes)
            {
                var lr = Line("Lane", 2, 14f, Color.white, Art.Lane);
                lr.textureMode = LineTextureMode.Tile;
                lr.SetPosition(0, SystemPosition(lane.A));
                lr.SetPosition(1, SystemPosition(lane.B));
                _lanes.Add((lr, lane.A, lane.B));
            }

            var rng = new System.Random(7);
            foreach (var s in c.Galaxy.Systems)
            {
                var v = new SystemVis();
                Vector3 p = SystemPosition(s.Id);
                v.StarColour = StarColour(s.Name);
                float starR = 26f + s.Tier * 8f;
                v.StarR = Mesh($"{s.Name}", p, Vector3.one * starR * 2f, Meshes.Sphere, Art.Star);
                v.Star = v.StarR.transform;
                v.CoronaR = Mesh($"{s.Name} corona", p, Vector3.one * starR * 9f, Meshes.Quad, Art.Flash);
                v.Corona = v.CoronaR.transform;

                // Planets: a few on orbits, more where the system matters.
                int planets = 1 + s.Tier + rng.Next(0, 2);
                for (int i = 0; i < planets; i++)
                {
                    float orbit = starR * 2.2f + 34f + i * 34f + (float)rng.NextDouble() * 12f;
                    var orb = Circle(p, orbit, 1.6f, new Color(0.6f, 0.7f, 0.9f, 0.12f));
                    orb.name = "Orbit";
                    float size = 7f + (float)rng.NextDouble() * (i == planets - 1 ? 16f : 9f);
                    var pr = Mesh("Planet", p, Vector3.one * size, Meshes.Sphere, Art.Planet);
                    SetColour(pr, BaseColorId, PlanetColour(rng));
                    v.Planets.Add((pr.transform, orbit, 6f + (float)rng.NextDouble() * 10f, (float)rng.NextDouble() * 360f));
                }
                if (s.Shipyard)
                {
                    var st = new GameObject("Station");
                    st.transform.SetParent(transform, false);
                    st.transform.position = p + new Vector3(starR * 2.2f + 20f, 18f, 0f);
                    st.transform.localScale = Vector3.one * (s.Tier >= 3 ? 1.4f : 1f);
                    st.AddComponent<MeshFilter>().sharedMesh = _stationMesh;
                    st.AddComponent<MeshRenderer>().sharedMaterials = new[] { Art.DarkMetal, Art.Windows };
                    v.Station = st.transform;
                }
                v.Ring = Circle(p, 210f, 7f, FactionStyle.Neutral);
                v.Siege = Line("Ground war", 2, 18f, Color.red, LineMaterial);
                v.Battle = Circle(p, 250f, 10f, new Color(1f, 0.35f, 0.25f));
                _systems.Add(v);
            }

            _selFleet = Circle(Vector3.zero, 90f, 6f, Color.white);
            _selSystem = Circle(Vector3.zero, 300f, 8f, new Color(1f, 1f, 1f, 0.8f));
        }

        static Color PlanetColour(System.Random rng)
        {
            Color[] kinds =
            {
                new Color(0.45f, 0.38f, 0.32f), new Color(0.62f, 0.52f, 0.38f), new Color(0.35f, 0.45f, 0.55f),
                new Color(0.55f, 0.6f, 0.68f), new Color(0.72f, 0.55f, 0.4f), new Color(0.3f, 0.42f, 0.3f),
            };
            return kinds[rng.Next(kinds.Length)];
        }

        static Color StarColour(string name)
        {
            int h = 0;
            foreach (char ch in name) h = h * 31 + ch;
            float t = (h & 0xffff) / 65535f;
            if (t < 0.4f) return new Color(1f, 0.5f, 0.3f);
            if (t < 0.72f) return new Color(1f, 0.82f, 0.6f);
            if (t < 0.9f) return new Color(0.98f, 0.96f, 1f);
            return new Color(0.62f, 0.76f, 1f);
        }

        // A station: a ring on spokes around a hub, with lit windows.
        static Mesh StationMesh()
        {
            var b = new MeshBuilder(2);
            b.Cylinder(new Vector3(0, -3, 0), new Vector3(0, 3, 0), 3.5f, 3.5f, 10, 0);
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI * 2f / 12f, a2 = (i + 1) * Mathf.PI * 2f / 12f;
                var p0 = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 16f;
                var p1 = new Vector3(Mathf.Cos(a2), 0, Mathf.Sin(a2)) * 16f;
                b.Cylinder(p0, p1, 1.6f, 1.6f, 6, 0);
                if (i % 3 == 0) b.Cylinder(Vector3.zero, p0, 0.6f, 0.6f, 5, 0);
                if (i % 2 == 0) b.Box((p0 + p1) * 0.5f + Vector3.up * 1.7f, new Vector3(1.2f, 0.3f, 1.2f), 1);
            }
            return b.ToMesh("station");
        }

        MeshRenderer Mesh(string name, Vector3 pos, Vector3 scale, Mesh mesh, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return r;
        }

        void SetColour(Renderer r, int id, Color c, float intensity = -1f)
        {
            _block.Clear();
            _block.SetColor(id, c);
            if (intensity >= 0f) _block.SetFloat(IntensityId, intensity);
            r.SetPropertyBlock(_block);
        }

        LineRenderer Line(string name, int points, float width, Color colour, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = mat;
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
            var lr = Line("Ring", 64, width, colour, LineMaterial);
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

        public void Refresh(Camera cam, float dt)
        {
            if (_c == null) return;
            var intel = _c.Intel[Viewer];
            Quaternion face = cam.transform.rotation;
            float t = Time.time;

            // Plate data: what the player knows of who holds what, and what is unseen.
            int n = Mathf.Min(32, _c.Galaxy.Systems.Count);
            for (int i = 0; i < n; i++)
            {
                var s = _c.Galaxy.Systems[i];
                Vector3 p = SystemPosition(i);
                int owner = intel.SeenHour[i] >= 0 ? intel.OwnerSeen[i] : -1;
                _sysData[i] = new Vector4(p.x, p.z, owner, intel.SystemVisible[i] ? 1f : 0f);
                bool source = s.Owner == Viewer || (s.Owner >= 0 && _c.Stance(Viewer, s.Owner) == DiplomaticStance.Alliance);
                _sysFlags[i] = new Vector4(source ? 1f : 0f, s.Capital ? 1f : 0f, 0f, 0f);
            }
            for (int f = 0; f < 8; f++) _colours[f] = FactionStyle.Of(f);
            Shader.SetGlobalVectorArray("_GR_Systems", _sysData);
            Shader.SetGlobalVectorArray("_GR_SysFlags", _sysFlags);
            Shader.SetGlobalVectorArray("_GR_Colors", _colours);
            Shader.SetGlobalFloat("_GR_SystemCount", n);
            Shader.SetGlobalFloat("_GR_ShowSupply", ShowSupply ? 1f : 0f);

            for (int i = 0; i < _systems.Count; i++)
            {
                var s = _c.Galaxy.Systems[i];
                var v = _systems[i];
                bool seen = intel.SystemVisible[i];
                int owner = intel.SeenHour[i] >= 0 ? intel.OwnerSeen[i] : -1;
                Vector3 p = SystemPosition(i);

                SetColour(v.StarR, ColorId, v.StarColour, seen ? 3f : 1.2f);
                v.Corona.rotation = face;
                SetColour(v.CoronaR, ColorId, v.StarColour * (seen ? 0.5f : 0.18f));
                v.Star.rotation = Quaternion.Euler(0f, t * 4f, 0f);
                foreach (var (pt, radius, speed, phase) in v.Planets)
                {
                    float a = (phase + t * speed) * Mathf.Deg2Rad;
                    pt.position = p + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius;
                }
                if (v.Station != null) v.Station.Rotate(0f, dt * 20f, 0f, Space.World);

                Color oc = FactionStyle.Of(owner);
                v.Ring.startColor = v.Ring.endColor = new Color(oc.r, oc.g, oc.b, owner < 0 ? 0.18f : seen ? 0.7f : 0.3f);

                // Ground war: an arc in the attacker's colour filling as they win.
                if (seen && s.Besieger >= 0)
                {
                    int m = Mathf.Max(2, Mathf.CeilToInt(64 * Mathf.Clamp01(s.Siege)));
                    v.Siege.enabled = true;
                    v.Siege.positionCount = m;
                    Color bc = FactionStyle.Of(s.Besieger);
                    float pulse = 0.7f + 0.3f * Mathf.Sin(t * 5f);
                    v.Siege.startColor = v.Siege.endColor = bc * pulse;
                    for (int k = 0; k < m; k++)
                    {
                        float a = Mathf.PI * 0.5f - k * Mathf.PI * 2f * Mathf.Clamp01(s.Siege) / (m - 1);
                        v.Siege.SetPosition(k, p + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 235f);
                    }
                }
                else v.Siege.enabled = false;

                bool battle = _c.Pending != null && _c.Pending.System == i;
                v.Battle.enabled = battle;
                if (battle)
                {
                    float r = 250f + 20f * Mathf.Sin(t * 6f);
                    SetCircle(v.Battle, p, r);
                }
            }

            foreach (var (line, a, b) in _lanes)
            {
                int oa = intel.SeenHour[a] >= 0 ? intel.OwnerSeen[a] : -1;
                int ob = intel.SeenHour[b] >= 0 ? intel.OwnerSeen[b] : -1;
                Color ca = oa >= 0 ? FactionStyle.Of(oa) : new Color(0.55f, 0.62f, 0.8f);
                Color cb = ob >= 0 ? FactionStyle.Of(ob) : new Color(0.55f, 0.62f, 0.8f);
                float dim = intel.SystemVisible[a] || intel.SystemVisible[b] ? 0.55f : 0.25f;
                line.startColor = ca * dim;
                line.endColor = cb * dim;
            }

            RefreshFleets(intel, dt, t);

            _selFleet.enabled = SelectedFleet >= 0 && _fleets.ContainsKey(SelectedFleet);
            if (_selFleet.enabled) SetCircle(_selFleet, FleetPosition(SelectedFleet), 90f);
            _selSystem.enabled = SelectedSystem >= 0;
            if (_selSystem.enabled) SetCircle(_selSystem, SystemPosition(SelectedSystem), 300f);
        }

        void RefreshFleets(Intel intel, float dt, float t)
        {
            var stack = new Dictionary<int, int>();
            foreach (var f in _c.Fleets)
            {
                if (!f.Alive || !intel.VisibleFleets.Contains(f.Id)) continue;
                if (!_fleets.TryGetValue(f.Id, out var fv)) _fleets[f.Id] = fv = NewFleetVis(f);
                string lead = LeadHull(f);
                if (fv.HullId != lead) SetModel(fv, f, lead);

                Vector3 target = ToMap(_c.FleetPosition(f));
                if (!f.InTransit || f.Held)
                {
                    stack.TryGetValue(f.System, out int k);
                    stack[f.System] = k + 1;
                    float a = (k * 55f + 20f) * Mathf.Deg2Rad;
                    if (!f.InTransit) target += new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 150f;
                }
                target.y += 70f;
                fv.Shown = fv.Placed ? Vector3.Lerp(fv.Shown, target, 1f - Mathf.Exp(-dt * 6f)) : target;
                fv.Placed = true;
                fv.Root.position = fv.Shown;

                // Face the way it is going.
                Vector3 heading = f.InTransit
                    ? ToMap(_c.Galaxy.Systems[f.Next].Position) - ToMap(_c.Galaxy.Systems[f.System].Position)
                    : new Vector3(Mathf.Sin(t * 0.2f + f.Id), 0f, Mathf.Cos(t * 0.2f + f.Id));
                heading.y = 0f;
                if (heading.sqrMagnitude > 1f) fv.Root.rotation = Quaternion.Slerp(fv.Root.rotation, Quaternion.LookRotation(heading), 1f - Mathf.Exp(-dt * 3f));

                fv.Drop.SetPosition(0, fv.Shown);
                fv.Drop.SetPosition(1, new Vector3(fv.Shown.x, -130f, fv.Shown.z));
                SetCircle(fv.Base, new Vector3(fv.Shown.x, -128f, fv.Shown.z), 40f + Mathf.Sqrt(f.Strength) * 0.8f);

                if (f.InTransit && f.Owner == Viewer || f.InTransit && _c.Stance(Viewer, f.Owner) == DiplomaticStance.Alliance)
                {
                    fv.Route.enabled = true;
                    fv.Route.positionCount = 2 + f.Route.Count;
                    fv.Route.SetPosition(0, fv.Shown);
                    fv.Route.SetPosition(1, SystemPosition(f.Next) + Vector3.up * 70f);
                    for (int i = 0; i < f.Route.Count; i++) fv.Route.SetPosition(2 + i, SystemPosition(f.Route[i]) + Vector3.up * 70f);
                }
                else fv.Route.enabled = false;
            }

            _gone.Clear();
            foreach (var kv in _fleets)
            {
                var f = _c.FleetById(kv.Key);
                if (f == null || !f.Alive || !intel.VisibleFleets.Contains(kv.Key)) _gone.Add(kv.Key);
            }
            foreach (int id in _gone)
            {
                Destroy(_fleets[id].Root.gameObject);
                Destroy(_fleets[id].Drop.gameObject);
                Destroy(_fleets[id].Base.gameObject);
                Destroy(_fleets[id].Route.gameObject);
                _fleets.Remove(id);
            }

            // Ghosts: where hostile fleets were last seen.
            _gone.Clear();
            foreach (var kv in intel.Fleets)
            {
                if (intel.VisibleFleets.Contains(kv.Key) || kv.Value.Owner == Viewer) continue;
                var sight = kv.Value;
                if (!_ghosts.TryGetValue(kv.Key, out var g))
                {
                    var r = Mesh("Ghost", Vector3.zero, Vector3.one * 150f, Meshes.Quad, Art.Flash);
                    SetColour(r, ColorId, FactionStyle.Of(sight.Owner) * 0.6f);
                    _ghosts[kv.Key] = g = r.transform;
                }
                Vector3 a = SystemPosition(sight.System);
                Vector3 pos = a;
                if (sight.Next >= 0)
                {
                    Vector3 b = SystemPosition(sight.Next);
                    pos = Vector3.Lerp(a, b, Mathf.Clamp01(sight.Progress * Scale / Mathf.Max(1f, Vector3.Distance(a, b))));
                }
                else pos += new Vector3(-120f, 0f, 120f);
                g.position = pos + Vector3.up * 70f;
                g.rotation = Camera.main != null ? Camera.main.transform.rotation : Quaternion.identity;
                g.localScale = Vector3.one * (120f + 30f * Mathf.Sin(t * 2f + kv.Key));
            }
            foreach (var kv in _ghosts)
                if (!intel.Fleets.ContainsKey(kv.Key) || intel.VisibleFleets.Contains(kv.Key)) _gone.Add(kv.Key);
            foreach (int id in _gone)
            {
                Destroy(_ghosts[id].gameObject);
                _ghosts.Remove(id);
            }
        }

        static string LeadHull(Fleet f)
        {
            GalacticRupture.Sim.HullDef best = null;
            foreach (var r in f.Ships)
                if (best == null || r.Def.Cost > best.Cost) best = r.Def;
            return best?.Id ?? "corvette";
        }

        FleetVis NewFleetVis(Fleet f)
        {
            var fv = new FleetVis();
            var root = new GameObject($"Fleet {f.Name}");
            root.transform.SetParent(transform, false);
            fv.Root = root.transform;
            Color fc = FactionStyle.Of(f.Owner);
            fv.Drop = Line("Drop", 2, 3f, new Color(fc.r, fc.g, fc.b, 0.35f), LineMaterial);
            fv.Base = Circle(Vector3.zero, 50f, 4f, new Color(fc.r, fc.g, fc.b, 0.6f));
            fv.Route = Line("Route", 2, 9f, new Color(fc.r, fc.g, fc.b, 0.7f), LineMaterial);
            return fv;
        }

        // The fleet is shown as its heaviest hull, scaled to a readable size, in its faction's paint.
        void SetModel(FleetVis fv, Fleet f, string hullId)
        {
            if (fv.Model != null) Destroy(fv.Model.gameObject);
            fv.HullId = hullId;
            var prefab = Art.HullModel(hullId);
            if (prefab == null) return;
            var go = Instantiate(prefab, fv.Root, false);
            foreach (Transform c in go.transform) Destroy(c.gameObject); // engine markers
            var r = go.GetComponent<MeshRenderer>();
            Art.ApplyFaction(r, Mathf.Clamp(f.Owner, 0, 3));
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            float length = r.localBounds.size.z;
            float scale = Mathf.Clamp(150f / Mathf.Max(1f, length), 0.3f, 3f);
            go.transform.localScale = Vector3.one * scale;
            fv.Model = go.transform;
        }
    }
}
