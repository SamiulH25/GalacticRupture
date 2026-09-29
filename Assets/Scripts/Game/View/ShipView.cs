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
            public ShipModels.TurretModel Model;
            public float BaseYawDeg;
            public float Recoil;
            public bool Dead;
        }

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
            var model = ShipModels.Hull(ship.Def);
            var materials = Slot.Materials(art, ship.Side);

            var hullGo = new GameObject("Hull");
            hullGo.transform.SetParent(transform, false);
            hullGo.AddComponent<MeshFilter>().sharedMesh = model.Mesh;
            _hull = hullGo.AddComponent<MeshRenderer>();
            _hull.sharedMaterials = materials;

            foreach (var mount in ship.Mounts)
            {
                var hp = mount.Hardpoint;
                var tm = ShipModels.Turret(hp.Mount);
                var yaw = new GameObject($"Mount {mount.Index}").transform;
                yaw.SetParent(transform, false);
                yaw.localPosition = hp.LocalPosition.ToUnity();
                yaw.localRotation = Quaternion.Euler(0f, hp.BaseYawDeg, 0f);

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

                _mounts.Add(new MountView
                {
                    Yaw = yaw, Pitch = pitch, Barrels = barrels, House = house, BarrelRenderer = br,
                    Model = tm, BaseYawDeg = hp.BaseYawDeg,
                });
            }

            var plumeMesh = Meshes.Plume;
            foreach (var e in model.Engines)
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

        public void Sync(Ship ship, float alpha, float dt)
        {
            if (Wrecked)
            {
                _wreckAge += dt;
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
                mv.Recoil = Mathf.MoveTowards(mv.Recoil, 0f, dt * mv.Model.Recoil * 1.4f);
                mv.Barrels.localPosition = new Vector3(0f, 0f, -mv.Recoil);
            }

            float thrust = ship.Def.MaxSpeed > 0f ? Mathf.Clamp01(ship.Speed / ship.Def.MaxSpeed) : 0f;
            float flicker = 0.92f + 0.08f * Mathf.PerlinNoise(Time.time * 9f, ShipId);
            foreach (var p in _plumes)
            {
                var s = p.localScale;
                float baseLen = s.x / 1.9f * 9f;
                p.localScale = new Vector3(s.x, s.y, Mathf.Max(0.05f, baseLen * (0.25f + 0.75f * thrust) * flicker));
            }
        }

        public void OnFired(int mount, int barrel)
        {
            var mv = _mounts[mount];
            mv.Recoil = mv.Model.Recoil;
        }

        public Vector3 MuzzleWorld(int mount, int barrel)
        {
            var mv = _mounts[mount];
            var muzzles = mv.Model.Muzzles;
            return mv.Pitch.TransformPoint(muzzles[barrel % muzzles.Length]);
        }

        public Vector3 BarrelForward(int mount) => _mounts[mount].Pitch.forward;

        public void OnMountDestroyed(int mount)
        {
            var mv = _mounts[mount];
            mv.Dead = true;
            mv.BarrelRenderer.enabled = false;
            mv.House.sharedMaterial = _art.Wreck;
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
            var mats = _hull.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) mats[i] = _art.Wreck;
            _hull.sharedMaterials = mats;
            foreach (var mv in _mounts)
            {
                mv.House.sharedMaterial = _art.Wreck;
                mv.BarrelRenderer.sharedMaterial = _art.Wreck;
            }
            foreach (var p in _plumes) p.gameObject.SetActive(false);
            _wreckDrift = velocity * 0.6f;
            float size = Mathf.Max(Def.HalfLength, 10f);
            _wreckSpin = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-2f, 2f)) * (120f / size);
        }

        public float WreckAge => _wreckAge;
    }
}
