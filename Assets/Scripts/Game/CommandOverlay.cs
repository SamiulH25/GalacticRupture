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

            // Contact rings: at a distance every ship gets a faint marker in its side's colour, so
            // the whole battle reads even when hulls are a few pixels long.
            foreach (var kv in Runner.Presenter.Views)
            {
                var ship = Runner.World.ShipById(kv.Key);
                if (ship == null || !ship.Alive || Input.Selected.Contains(ship.Id)) continue;
                Vector3 p = kv.Value.transform.position;
                float d = Vector3.Distance(cam.transform.position, p);
                if (d < 1800f) continue;
                float fade = Mathf.Clamp01((d - 1800f) / 1500f);
                var c = ship.Side == Input.PlayerSide ? Own : Attack;
                c.a *= 0.55f * fade;
                Ring(p, Mathf.Max(ship.Def.HalfLength * 1.2f, d * 0.012f), d * 0.0012f, c);
            }

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
