using System.Collections.Generic;
using GalacticRupture.Sim;
using UnityEngine;

namespace GalacticRupture.Game
{
    // The visible ship: hull, independently animated turrets, engine plumes, fires.
    // Copies pose from the sim each frame; never decides anything.
    public sealed class ShipView : MonoBehaviour
    {
        sealed class MountView
        {
            public Transform Yaw;
            public Transform Pitch;
            public Transform Barrels;
            public MeshRenderer House;
            public MeshRenderer BarrelRenderer;
            public Transform[] Muzzles;
            public float RecoilDistance;
            public float BaseYawDeg;
            public float Recoil;
            public bool Dead;
        }

        sealed class Scorch
        {
            public MeshRenderer Renderer;
            public float Heat;
        }

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        static readonly int HeatId = Shader.PropertyToID("_Heat");
        const int MaxScorches = 48;

        public ShieldView Shield { get; private set; }
        readonly List<Scorch> _scorches = new List<Scorch>();
        int _nextScorch;
        int _paintIndex = -1, _windowIndex = -1;
        Color _paintColor, _windowColor;
        float _shownDamage = -1f;
        MaterialPropertyBlock _block;

        public int ShipId { get; private set; }
        public HullDef Def { get; private set; }
        public bool Wrecked { get; private set; }

        readonly List<MountView> _mounts = new List<MountView>();
        readonly List<Transform> _plumes = new List<Transform>();
        readonly List<ParticleSystem> _fires = new List<ParticleSystem>();
        MeshRenderer _hull;
        GameArt _art;
        float _roll, _pitch;
        Vector3 _wreckSpin;
        Vector3 _wreckDrift;
        float _wreckAge;

        public static ShipView Create(Ship ship, GameArt art, Transform parent)
        {
            var go = new GameObject($"{ship.Def.Id} #{ship.Id}");
            go.transform.SetParent(parent, false);
            var view = go.AddComponent<ShipView>();
            view.Build(ship, art);
            return view;
        }

        void Build(Ship ship, GameArt art)
        {
            ShipId = ship.Id;
            Def = ship.Def;
            _art = art;
            var engines = new List<ShipModels.EnginePort>();

            var hullPrefab = art.HullModel(ship.Def.Id);
            if (hullPrefab != null)
            {
                var hull = Instantiate(hullPrefab, transform, false);
                hull.name = "Hull";
                _hull = hull.GetComponent<MeshRenderer>();
                art.ApplyFaction(_hull, ship.Side);
                foreach (var marker in Children(hull.transform, "Engine_"))
                {
                    int r = marker.name.IndexOf("_r", System.StringComparison.Ordinal);
                    float radius = r >= 0 && float.TryParse(marker.name.Substring(r + 2),
                        System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 2f;
                    engines.Add(new ShipModels.EnginePort { Position = marker.localPosition, Radius = radius });
                    Destroy(marker.gameObject);
                }
            }
            else
            {
                var model = ShipModels.Hull(ship.Def);
                var hullGo = new GameObject("Hull");
                hullGo.transform.SetParent(transform, false);
                hullGo.AddComponent<MeshFilter>().sharedMesh = model.Mesh;
                _hull = hullGo.AddComponent<MeshRenderer>();
                _hull.sharedMaterials = Slot.Materials(art, ship.Side);
                engines.AddRange(model.Engines);
            }

            var hullMats = _hull.sharedMaterials;
            for (int i = 0; i < hullMats.Length; i++)
            {
                if (hullMats[i] == art.HullPaint[ship.Side]) _paintIndex = i;
                if (hullMats[i] == art.Windows) _windowIndex = i;
            }
            _paintColor = art.HullPaint[ship.Side].GetColor(BaseColorId);
            _windowColor = art.Windows.GetColor(EmissionId);
            _block = new MaterialPropertyBlock();
            Shield = ShieldView.Create(transform, art.ShieldField, _hull.GetComponent<MeshFilter>().sharedMesh.bounds);

            foreach (var mount in ship.Mounts)
            {
                var hp = mount.Hardpoint;
                var mv = art.TurretModel(hp.Mount.Id) is GameObject tp
                    ? ModelMount(tp, hp, ship.Side)
                    : ProceduralMount(hp, ship.Side);
                mv.Yaw.name = $"Mount {mount.Index}";
                mv.Yaw.localPosition = hp.LocalPosition.ToUnity();
                mv.Yaw.localRotation = Quaternion.Euler(0f, hp.BaseYawDeg, 0f);
                mv.BaseYawDeg = hp.BaseYawDeg;
                mv.RecoilDistance = hp.Mount.Class switch
                {
                    MountClass.Line => 2.4f,
                    MountClass.PointDefence => 0.5f,
                    _ => hp.Mount.MuzzleLength * 0.1f,
                };
                _mounts.Add(mv);
            }

            var plumeMesh = Meshes.Plume;
            foreach (var e in engines)
            {
                var p = new GameObject("Plume").transform;
                p.SetParent(transform, false);
                p.localPosition = e.Position;
                p.localRotation = Quaternion.LookRotation(Vector3.back);
                p.localScale = new Vector3(e.Radius * 1.9f, e.Radius * 1.9f, e.Radius * 9f);
                p.gameObject.AddComponent<MeshFilter>().sharedMesh = plumeMesh;
                var r = p.gameObject.AddComponent<MeshRenderer>();
                r.sharedMaterial = art.Plume[ship.Side];
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _plumes.Add(p);
            }
        }

        static List<Transform> Children(Transform t, string prefix)
        {
            var list = new List<Transform>();
            foreach (Transform c in t)
                if (c.name.StartsWith(prefix, System.StringComparison.Ordinal)) list.Add(c);
            list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return list;
        }

        // Blender turret: House root (yaw pivot), *_Barrels child at the pitch pivot, Muzzle_N
        // markers under House at rest pose. Adds a Pitch pivot so barrels can recoil inside it.
        MountView ModelMount(GameObject prefab, HardpointDef hp, int side)
        {
            var yaw = Instantiate(prefab, transform, false).transform;
            var house = yaw.GetComponent<MeshRenderer>();
            _art.ApplyFaction(house, side);

            Transform barrels = null;
            foreach (Transform c in yaw)
                if (c.name.Contains("_Barrels")) barrels = c;
            var pitch = new GameObject("Pitch").transform;
            pitch.SetParent(yaw, false);
            pitch.localPosition = barrels != null ? barrels.localPosition : new Vector3(0f, hp.Mount.PivotHeight, 0f);
            var muzzles = Children(yaw, "Muzzle_");
            if (barrels != null)
            {
                barrels.SetParent(pitch, false);
                barrels.localPosition = Vector3.zero;
                barrels.localRotation = Quaternion.identity;
            }
            var br = barrels != null ? barrels.GetComponent<MeshRenderer>() : null;
            if (br != null) _art.ApplyFaction(br, side);
            foreach (var m in muzzles)
            {
                m.SetParent(barrels != null ? barrels : pitch, true);
                Destroy(m.GetComponent<MeshRenderer>());
                Destroy(m.GetComponent<MeshFilter>());
            }
            if (muzzles.Count == 0) muzzles.Add(pitch);

            return new MountView { Yaw = yaw, Pitch = pitch, Barrels = barrels, House = house, BarrelRenderer = br, Muzzles = muzzles.ToArray() };
        }

        MountView ProceduralMount(HardpointDef hp, int side)
        {
            var tm = ShipModels.Turret(hp.Mount);
            var materials = Slot.Materials(_art, side);
            var yaw = new GameObject("Mount").transform;
            yaw.SetParent(transform, false);
            var house = yaw.gameObject.AddComponent<MeshRenderer>();
            yaw.gameObject.AddComponent<MeshFilter>().sharedMesh = tm.Gunhouse;
            house.sharedMaterials = materials;

            var pitch = new GameObject("Pitch").transform;
            pitch.SetParent(yaw, false);
            pitch.localPosition = new Vector3(0f, hp.Mount.PivotHeight, 0f);

            var barrels = new GameObject("Barrels").transform;
            barrels.SetParent(pitch, false);
            barrels.gameObject.AddComponent<MeshFilter>().sharedMesh = tm.Barrels;
            var br = barrels.gameObject.AddComponent<MeshRenderer>();
            br.sharedMaterials = materials;

            var muzzles = new Transform[tm.Muzzles.Length];
            for (int i = 0; i < muzzles.Length; i++)
            {
                muzzles[i] = new GameObject($"Muzzle_{i}").transform;
                muzzles[i].SetParent(barrels, false);
                muzzles[i].localPosition = tm.Muzzles[i];
            }
            return new MountView { Yaw = yaw, Pitch = pitch, Barrels = barrels, House = house, BarrelRenderer = br, Muzzles = muzzles };
        }

        public void Sync(Ship ship, float alpha, float dt)
        {
            if (Wrecked)
            {
                _wreckAge += dt;
                // Wrecks burn out.
                if (_wreckAge > 12f && _fires.Count > 0)
                {
                    foreach (var f in _fires) f.Stop(false, ParticleSystemStopBehavior.StopEmitting);
                    _fires.Clear();
                }
                transform.position += _wreckDrift * dt;
                transform.rotation = Quaternion.Euler(_wreckSpin * dt) * transform.rotation;
                return;
            }

            Vector3 pos = System.Numerics.Vector3.Lerp(ship.PrevPosition, ship.Position, alpha).ToUnity();
            float yaw = Mathf.LerpAngle(ship.PrevYaw * Mathf.Rad2Deg, ship.Yaw * Mathf.Rad2Deg, alpha);

            float targetRoll = Mathf.Clamp(-ship.TurnRate * Mathf.Rad2Deg, -50f, 50f);
            float targetPitch = ship.Speed > 1f ? -Mathf.Atan2(ship.Velocity.Y, ship.Speed) * Mathf.Rad2Deg * 0.6f : 0f;
            float k = 1f - Mathf.Exp(-dt * 2.5f);
            _roll = Mathf.Lerp(_roll, targetRoll, k);
            _pitch = Mathf.Lerp(_pitch, targetPitch, k);

            transform.SetPositionAndRotation(pos, Quaternion.Euler(_pitch, yaw, _roll));

            for (int i = 0; i < _mounts.Count; i++)
            {
                var mv = _mounts[i];
                if (mv.Dead) continue;
                var m = ship.Mounts[i];
                float my = Mathf.LerpAngle(m.PrevYaw * Mathf.Rad2Deg, m.Yaw * Mathf.Rad2Deg, alpha);
                float mp = Mathf.Lerp(m.PrevPitch, m.Pitch, alpha) * Mathf.Rad2Deg;
                mv.Yaw.localRotation = Quaternion.Euler(0f, mv.BaseYawDeg + my, 0f);
                mv.Pitch.localRotation = Quaternion.Euler(-mp, 0f, 0f);
                mv.Recoil = Mathf.MoveTowards(mv.Recoil, 0f, dt * mv.RecoilDistance * 1.4f);
                if (mv.Barrels != null) mv.Barrels.localPosition = new Vector3(0f, 0f, -mv.Recoil);
            }

            Shield.Tick(dt, ship.Def.ShieldHp > 0f ? ship.Shield / ship.Def.ShieldHp : 0f);
            ShowDamage(1f - ship.Hull / ship.Def.HullHp);
            CoolScorches(dt);

            float thrust = ship.Def.MaxSpeed > 0f ? Mathf.Clamp01(ship.Speed / ship.Def.MaxSpeed) : 0f;
            float flicker = 0.92f + 0.08f * Mathf.PerlinNoise(Time.time * 9f, ShipId);
            foreach (var p in _plumes)
            {
                var s = p.localScale;
                float baseLen = s.x / 1.9f * 9f;
                p.localScale = new Vector3(s.x, s.y, Mathf.Max(0.05f, baseLen * (0.25f + 0.75f * thrust) * flicker));
            }
        }

        // The ship is the health bar: paint darkens with damage, and past 70% the lights flicker.
        void ShowDamage(float damage)
        {
            bool critical = damage > 0.7f;
            if (!critical && Mathf.Abs(damage - _shownDamage) < 0.02f) return;
            _shownDamage = damage;
            if (_paintIndex >= 0)
            {
                _block.Clear();
                _block.SetColor(BaseColorId, _paintColor * Mathf.Lerp(1f, 0.4f, damage * damage));
                _hull.SetPropertyBlock(_block, _paintIndex);
            }
            if (_windowIndex >= 0)
            {
                float lit = critical ? (Mathf.PerlinNoise(Time.time * 6f, ShipId * 0.37f) > 0.45f ? 0.8f : 0.05f) : 1f - damage * 0.4f;
                _block.Clear();
                _block.SetColor(EmissionId, _windowColor * lit);
                _hull.SetPropertyBlock(_block, _windowIndex);
            }
        }

        // A burn mark where a round got through. Old marks are reused past the cap.
        public void AddScorch(Vector3 worldPoint, Vector3 worldNormal, float size)
        {
            Scorch sc;
            if (_scorches.Count < MaxScorches)
            {
                var go = new GameObject("Scorch");
                go.transform.SetParent(_hull.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = Meshes.Quad;
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = _art.Scorch;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                sc = new Scorch { Renderer = r };
                _scorches.Add(sc);
            }
            else
            {
                sc = _scorches[_nextScorch];
                _nextScorch = (_nextScorch + 1) % MaxScorches;
            }
            var t = sc.Renderer.transform;
            t.position = worldPoint + worldNormal * 0.25f;
            t.rotation = Quaternion.LookRotation(-worldNormal) * Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            t.localScale = Vector3.one * size;
            sc.Heat = 1f;
        }

        void CoolScorches(float dt)
        {
            foreach (var sc in _scorches)
            {
                if (sc.Heat <= 0f) continue;
                sc.Heat = Mathf.Max(0f, sc.Heat - dt * 0.35f);
                _block.Clear();
                _block.SetFloat(HeatId, sc.Heat);
                sc.Renderer.SetPropertyBlock(_block);
            }
        }

        public void OnFired(int mount, int barrel)
        {
            var mv = _mounts[mount];
            mv.Recoil = mv.RecoilDistance;
        }

        public Vector3 MuzzleWorld(int mount, int barrel)
        {
            var muzzles = _mounts[mount].Muzzles;
            return muzzles[barrel % muzzles.Length].position;
        }

        public Vector3 BarrelForward(int mount) => _mounts[mount].Pitch.forward;

        public void OnMountDestroyed(int mount)
        {
            var mv = _mounts[mount];
            mv.Dead = true;
            if (mv.BarrelRenderer != null) mv.BarrelRenderer.enabled = false;
            SetAll(mv.House, _art.Wreck);
            mv.Yaw.localRotation *= Quaternion.Euler(Random.Range(-12f, 12f), Random.Range(-30f, 30f), Random.Range(-15f, 15f));
        }

        // Starts a fire at a world point on the hull; the ship is the health bar.
        public void Ignite(Vector3 worldPoint, float size, int maxFires)
        {
            if (_fires.Count >= maxFires) return;
            var go = new GameObject("Fire");
            go.transform.SetParent(transform, false);
            go.transform.position = worldPoint;
            _fires.Add(Fx.MakeHullFire(go, _art, size));
        }

        public void BecomeWreck(Vector3 velocity)
        {
            Wrecked = true;
            SetAll(_hull, _art.Wreck);
            foreach (var mv in _mounts)
            {
                SetAll(mv.House, _art.Wreck);
                SetAll(mv.BarrelRenderer, _art.Wreck);
            }
            foreach (var p in _plumes) p.gameObject.SetActive(false);
            Shield.gameObject.SetActive(false);
            _wreckDrift = velocity * 0.6f;
            float size = Mathf.Max(Def.HalfLength, 10f);
            _wreckSpin = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-2f, 2f)) * (120f / size);
        }

        public float WreckAge => _wreckAge;

        static void SetAll(Renderer r, Material m)
        {
            if (r == null) return;
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) mats[i] = m;
            r.sharedMaterials = mats;
        }
    }
}
