using System.Collections.Generic;
using GalacticRupture.Sim;
using UnityEngine;

namespace GalacticRupture.Game
{
    // Turns sim state and events into things on screen.
    public sealed class FleetPresenter : MonoBehaviour
    {
        GameArt _art;
        Fx _fx;
        World _world;
        readonly Dictionary<int, ShipView> _views = new Dictionary<int, ShipView>();
        readonly List<int> _expired = new List<int>();

        public IReadOnlyDictionary<int, ShipView> Views => _views;

        // The most recent ship to take a shield hit: where the action is, for cameras.
        public int LastShieldHit { get; private set; } = -1;

        public void Init(GameArt art, Fx fx, World world)
        {
            _art = art;
            _fx = fx;
            _world = world;
        }

        public ShipView ViewOf(int ship) => _views.TryGetValue(ship, out var v) ? v : null;

        public void Handle(in SimEvent e)
        {
            var ship = _world.ShipById(e.Ship);
            switch (e.Kind)
            {
                case SimEventKind.ShipSpawned:
                    _views[e.Ship] = ShipView.Create(ship, _art, transform);
                    _views[e.Ship].Sync(ship, 1f, 0f);
                    break;

                case SimEventKind.Fired:
                {
                    var view = ViewOf(e.Ship);
                    if (view == null) break;
                    view.OnFired(e.Mount, e.Barrel);
                    var def = ship.Mounts[e.Mount].Def;
                    float scale = def.Class switch
                    {
                        MountClass.Line => 4.5f,
                        MountClass.PointDefence => 0.6f,
                        _ => def.Id == "corvette_gun" ? 0.9f : 1.8f,
                    };
                    Color tint = ship.Side != TurretToy.PlayerSide ? new Color(1f, 0.35f, 0.4f)
                        : def.Channel == DamageChannel.Energy ? new Color(0.5f, 0.85f, 1f)
                        : new Color(1f, 0.75f, 0.45f);
                    _fx.MuzzleFlash(view.MuzzleWorld(e.Mount, e.Barrel), view.BarrelForward(e.Mount), ship.Velocity.ToUnity(), scale, tint);
                    break;
                }

                case SimEventKind.ShieldHit:
                {
                    var view = ViewOf(e.Ship);
                    if (view == null) break;
                    Vector3 p = view.Shield.SurfacePoint(e.Position.ToUnity());
                    Vector3 n = (p - view.transform.position).normalized;
                    view.Shield.Hit(p);
                    LastShieldHit = e.Ship;
                    _fx.ShieldImpact(p, n, Mathf.Clamp(e.Value / 60f, 0.3f, 1.6f));
                    break;
                }

                case SimEventKind.ShieldsDown:
                {
                    var view = ViewOf(e.Ship);
                    if (view == null) break;
                    Vector3 p = view.Shield.SurfacePoint(e.Position.ToUnity());
                    view.Shield.Collapse(p);
                    _fx.ShieldImpact(p, (p - view.transform.position).normalized, 3f);
                    break;
                }

                case SimEventKind.HullHit:
                {
                    Vector3 p = e.Position.ToUnity();
                    Vector3 n = (p - ship.Position.ToUnity()).normalized;
                    _fx.HullImpact(p, n, Mathf.Clamp(e.Value / 50f, 0.4f, 3f));
                    var view = ViewOf(e.Ship);
                    if (view != null && Random.value < 0.35f + e.Value / 200f)
                        view.AddScorch(p, KeelNormal(ship, p), Mathf.Clamp(3f + e.Value / 15f, 3f, 16f) * (ship.Def.Radius > 15f ? 1f : 0.5f));
                    float damaged = 1f - ship.Hull / ship.Def.HullHp;
                    if (view != null && damaged > 0.35f && Random.value < 0.08f + e.Value / 1500f)
                        view.Ignite(p, Mathf.Clamp(ship.Def.Radius * 0.35f, 1.5f, 9f), 2 + (int)(damaged * 8f));
                    break;
                }

                case SimEventKind.MountDestroyed:
                    ViewOf(e.Ship)?.OnMountDestroyed(e.Mount);
                    _fx.Explosion(e.Position.ToUnity(), ship.Velocity.ToUnity(), 4f);
                    break;

                case SimEventKind.ShipDestroyed:
                {
                    var view = ViewOf(e.Ship);
                    _fx.Explosion(e.Position.ToUnity(), ship.Velocity.ToUnity(), ship.Def.Radius * 1.4f);
                    if (view != null)
                    {
                        view.BecomeWreck(ship.Velocity.ToUnity());
                        for (int i = 0; i < 3; i++)
                            view.Ignite(ship.LocalToWorld(new System.Numerics.Vector3(0f, 0f, Random.Range(-1f, 1f) * ship.Def.HalfLength)).ToUnity(),
                                ship.Def.Radius * 0.5f, 6);
                    }
                    break;
                }
            }
        }

        // Outward normal at a hull point, taken from the nearest point on the keel.
        static Vector3 KeelNormal(Ship ship, Vector3 p)
        {
            ship.KeelSegment(out var bow, out var stern);
            Vector3 a = stern.ToUnity(), b = bow.ToUnity();
            Vector3 ab = b - a;
            float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude);
            Vector3 n = p - (a + ab * t);
            return n.sqrMagnitude > 1e-4f ? n.normalized : Vector3.up;
        }

        public void Sync(float alpha, float dt)
        {
            _expired.Clear();
            foreach (var kv in _views)
            {
                var ship = _world.ShipById(kv.Key);
                kv.Value.Sync(ship, alpha, dt);
                if (kv.Value.Wrecked && kv.Value.WreckAge > 40f) _expired.Add(kv.Key);
            }
            foreach (int id in _expired)
            {
                Destroy(_views[id].gameObject);
                _views.Remove(id);
            }
        }
    }
}
