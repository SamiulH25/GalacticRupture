using System.Collections.Generic;
using GalacticRupture.Sim;
using UnityEngine;

namespace GalacticRupture.Game
{
    // In-world command feedback: selection rings, drop lines to the order plane, order lines to
    // destinations and targets, and a marker where the last order landed.
    [DefaultExecutionOrder(200)]
    public sealed class CommandOverlay : MonoBehaviour
    {
        public SimRunner Runner;
        public CommandInput Input;
        public Material LineMaterial;

        static readonly Color Own = new Color(0.45f, 0.85f, 1f, 0.7f);
        static readonly Color Move = new Color(0.35f, 0.9f, 0.55f, 0.35f);
        static readonly Color Attack = new Color(1f, 0.35f, 0.3f, 0.9f);
        static readonly Color Hover = new Color(1f, 1f, 1f, 0.5f);
        static readonly Color GunBig = new Color(0.91f, 0.67f, 0.27f, 0.3f);
        static readonly Color GunSmall = new Color(0.91f, 0.67f, 0.27f, 0.12f);
        static readonly Color GunLaid = new Color(1f, 0.82f, 0.45f, 0.5f);
        static readonly Color GunDead = new Color(0.85f, 0.33f, 0.24f, 0.9f);

        readonly List<LineRenderer> _pool = new List<LineRenderer>();
        int _used;

        LineRenderer Next(int points, float width, Color color, bool loop)
        {
            LineRenderer lr;
            if (_used < _pool.Count) lr = _pool[_used];
            else
            {
                var go = new GameObject("Line");
                go.transform.SetParent(transform, false);
                lr = go.AddComponent<LineRenderer>();
                lr.sharedMaterial = LineMaterial;
                lr.useWorldSpace = true;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lr.receiveShadows = false;
                lr.numCapVertices = 2;
                _pool.Add(lr);
            }
            _used++;
            lr.enabled = true;
            lr.positionCount = points;
            lr.loop = loop;
            lr.widthMultiplier = width;
            lr.startColor = lr.endColor = color;
            return lr;
        }

        void Ring(Vector3 centre, float radius, float width, Color color)
        {
            const int n = 48;
            var lr = Next(n, width, color, true);
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2f / n;
                lr.SetPosition(i, centre + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius);
            }
        }

        void Line(Vector3 a, Vector3 b, float width, Color color)
        {
            var lr = Next(2, width, color, false);
            lr.SetPosition(0, a);
            lr.SetPosition(1, b);
        }

        // The batteries as geometry (IDEAS.md §2.1): each live gun's firing arc as a thin fan, a red
        // mark where one has been shot away, and a line to the target for each gun that is laid on.
        void GunArcs(Ship ship, Vector3 shipPos, bool fans)
        {
            var world = Runner.World;
            float w = Mathf.Max(0.5f, Vector3.Distance(Input.Rig.Camera.transform.position, shipPos) * 0.0009f);
            foreach (var m in ship.Mounts)
            {
                Vector3 pivot = world.MountPivot(ship, m).ToUnity();
                var hp = m.Hardpoint;
                if (!m.Alive)
                {
                    Ring(pivot, ship.Def.Radius * 0.25f + 3f, w * 1.6f, GunDead);
                    continue;
                }
                bool big = m.Def.Class == MountClass.Line;
                var col = big ? GunBig : GunSmall;
                if (fans && m.Def.Class != MountClass.PointDefence && hp.YawHalfArcDeg < 178f)
                {
                    float range = m.Def.Range;
                    float centre = ship.Yaw + hp.BaseYawDeg * Mathf.Deg2Rad;
                    float half = hp.YawHalfArcDeg * Mathf.Deg2Rad;
                    bool all = half >= Mathf.PI * 0.98f;
                    const int n = 14;
                    var lr = Next(all ? n * 2 : n + 1, w, col, all);
                    for (int i = 0; i < lr.positionCount; i++)
                    {
                        float a = all ? i * Mathf.PI * 2f / lr.positionCount : centre - half + i * 2f * half / n;
                        lr.SetPosition(i, pivot + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * range);
                    }
                    if (!all)
                    {
                        var edge = new Color(col.r, col.g, col.b, col.a * 0.7f);
                        Line(pivot, pivot + new Vector3(Mathf.Sin(centre - half), 0f, Mathf.Cos(centre - half)) * range, w * 0.7f, edge);
                        Line(pivot, pivot + new Vector3(Mathf.Sin(centre + half), 0f, Mathf.Cos(centre + half)) * range, w * 0.7f, edge);
                    }
                }
                if (m.Target >= 0 && m.OnTarget && world.ShipById(m.Target) is Ship t && t.Alive)
                    Line(pivot, t.Position.ToUnity(), w * 1.1f, GunLaid);
            }
        }

        void LateUpdate()
        {
            _used = 0;
            if (Runner.World != null && !CaptureMode.Active && !BenchMode.Active) Draw();
            for (int i = _used; i < _pool.Count; i++) _pool[i].enabled = false;
        }

        void Draw()
        {
            var cam = Input.Rig.Camera;
            float planeY = Input.SelectionCentre().y;

            foreach (int id in Input.Selected)
            {
                var ship = Runner.World.ShipById(id);
                var view = Runner.Presenter.ViewOf(id);
                if (ship == null || view == null) continue;
                Vector3 p = view.transform.position;
                float scale = Vector3.Distance(cam.transform.position, p) * 0.0012f;
                float r = ship.Def.HalfLength * 1.15f + 10f;
                Ring(new Vector3(p.x, p.y, p.z), r, Mathf.Max(0.6f, scale), Own);

                // Drop line to the order plane, so height reads in 3D.
                if (Mathf.Abs(p.y - planeY) > 5f)
                    Line(p, new Vector3(p.x, planeY, p.z), Mathf.Max(0.3f, scale * 0.5f), new Color(Own.r, Own.g, Own.b, 0.35f));

                if (ship.Mounts.Count > 0 && ship.Side == Input.PlayerSide) GunArcs(ship, p, Input.Selected.Count <= 3 || id == Input.Selected[0]);

                var o = ship.Order;
                if (o.Kind == OrderKind.MoveTo)
                {
                    Vector3 dest = o.Point.ToUnity();
                    Line(p, dest, Mathf.Max(0.4f, scale * 0.6f), Move);
                    Ring(dest, ship.Def.Radius + 8f, Mathf.Max(0.4f, scale * 0.6f), Move);
                }
                else if (o.Kind == OrderKind.Attack && Runner.Presenter.ViewOf(o.OtherShip) is ShipView tv)
                {
                    Line(p, tv.transform.position, Mathf.Max(0.4f, scale * 0.6f), Attack);
                }
                else if (o.Kind == OrderKind.Face)
                {
                    Line(p, o.Point.ToUnity(), Mathf.Max(0.4f, scale * 0.6f), new Color(1f, 0.85f, 0.4f, 0.8f));
                }
            }

            // Distant contacts are marked by the HUD (screen-space icons), not by line rings: a
            // ring per ship per frame was the overlay's biggest cost at fleet scale.

            if (Input.Hovered >= 0 && !Input.Selected.Contains(Input.Hovered))
            {
                var ship = Runner.World.ShipById(Input.Hovered);
                var view = Runner.Presenter.ViewOf(Input.Hovered);
                if (ship != null && view != null)
                {
                    float scale = Vector3.Distance(cam.transform.position, view.transform.position) * 0.002f;
                    Ring(view.transform.position, ship.Def.HalfLength * 1.15f + 10f, Mathf.Max(0.5f, scale),
                        ship.Side == Input.PlayerSide ? Hover : Attack);
                }
            }

            // Reserves waiting to be placed: the zone around our ships, and where they would arrive.
            if (Input.Placing.Count > 0)
            {
                var zone = new Color(0.45f, 0.85f, 1f, 0.22f);
                foreach (var ship in Runner.World.Ships)
                {
                    if (!ship.Alive || ship.Side != Input.PlayerSide || ship.Def.Class == HullClass.Transport) continue;
                    Vector3 c = ship.Position.ToUnity();
                    c.y = 0f;
                    float w = Mathf.Max(1f, Vector3.Distance(cam.transform.position, c) * 0.0015f);
                    Ring(c, Reinforcements.ZoneRadius, w, zone);
                }
                if (Input.PlaceOnField)
                {
                    var tone = Input.PlaceValid ? Move : Attack;
                    tone.a = 0.9f;
                    float d = Vector3.Distance(cam.transform.position, Input.PlacePoint);
                    float w = Mathf.Max(0.8f, d * 0.0015f);
                    Ring(Input.PlacePoint, 250f, w, tone);
                    Ring(Input.PlacePoint, 60f, w, tone);
                }
            }

            // A right-drag in progress: where each ship will stand, and the way they will face.
            if (Input.Planning && Input.PlanDragged)
            {
                var slots = Input.PlanSlots();
                float d = Vector3.Distance(cam.transform.position, Input.PlanPoint);
                float w = Mathf.Max(0.5f, d * 0.0012f);
                Vector3 fwd = new Vector3(Mathf.Sin(Input.PlanYaw), 0f, Mathf.Cos(Input.PlanYaw));
                int k = 0;
                foreach (int id in Input.Selected)
                {
                    var ship = Runner.World.ShipById(id);
                    if (ship == null || !ship.Alive || ship.Side != Input.PlayerSide || ship.Def.MaxSpeed <= 0f) continue;
                    if (k >= slots.Length) break;
                    Vector3 p = slots[k++].ToUnity();
                    float half = ship.Def.HalfLength;
                    Line(p - fwd * half, p + fwd * half, w * 1.4f, Move);
                    Ring(p, ship.Def.Radius + 10f, w, Move);
                }
                Vector3 c = Input.PlanPoint;
                Line(c, c + fwd * d * 0.08f, w * 1.6f, Own);
            }

            float age = Time.unscaledTime - Input.LastOrderTime;
            if (age < 0.6f)
            {
                float t = age / 0.6f;
                var c = Input.LastOrderWasAttack ? Attack : Move;
                c.a *= 1f - t;
                float d = Vector3.Distance(cam.transform.position, Input.LastOrderPoint);
                Ring(Input.LastOrderPoint, d * (0.01f + 0.03f * t), d * 0.002f, c);
            }
        }
    }
}
