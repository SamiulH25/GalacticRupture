using System.Collections.Generic;
using GalacticRupture.Sim;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GalacticRupture.Game
{
    // The player's hands on the fleet.
    //   Left click / drag       select (Shift adds, Ctrl+A all, double-click selects that class)
    //   Right click space       move there, the group keeping its shape
    //   Right click enemy       attack: close to gun range, turn broadside, focus fire
    //   Ctrl + right click      face that point (turn the broadside you want)
    //   H                       hold position
    //   F                       follow the selection
    //   Ctrl+1..9 / 1..9        set / recall a control group
    public sealed class CommandInput : MonoBehaviour
    {
        public SimRunner Runner;
        public CameraRig Rig;
        public int PlayerSide = 0;

        public readonly List<int> Selected = new List<int>();
        readonly Dictionary<int, List<int>> _groups = new Dictionary<int, List<int>>();

        public bool Dragging { get; private set; }
        public Rect DragRect { get; private set; }
        public int Hovered { get; private set; } = -1;

        // Feedback for the order markers: where the last order went, and when.
        public Vector3 LastOrderPoint { get; private set; }
        public float LastOrderTime { get; private set; } = -10f;
        public bool LastOrderWasAttack { get; private set; }

        Vector2 _dragStart;
        float _lastClickTime;
        int _lastClickShip = -1;

        static readonly Key[] Digits =
        {
            Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9,
        };

        void Update()
        {
            if (CaptureMode.Active || BenchMode.Active || RtsCapture.Active || Runner.World == null) return;
            var mouse = Mouse.current;
            var kb = Keyboard.current;
            if (mouse == null || kb == null) return;
            Prune();

            Vector2 mp = mouse.position.ReadValue();
            Hovered = PickShip(mp, -1);

            if (mouse.leftButton.wasPressedThisFrame)
            {
                _dragStart = mp;
                Dragging = false;
            }
            if (mouse.leftButton.isPressed && (mp - _dragStart).sqrMagnitude > 64f)
            {
                Dragging = true;
                DragRect = Rect.MinMaxRect(Mathf.Min(mp.x, _dragStart.x), Mathf.Min(mp.y, _dragStart.y),
                    Mathf.Max(mp.x, _dragStart.x), Mathf.Max(mp.y, _dragStart.y));
            }
            if (mouse.leftButton.wasReleasedThisFrame)
            {
                bool add = kb.shiftKey.isPressed;
                if (Dragging) BoxSelect(DragRect, add);
                else ClickSelect(mp, add);
                Dragging = false;
            }

            if (mouse.rightButton.wasPressedThisFrame && Selected.Count > 0)
                RightClick(mp, kb.ctrlKey.isPressed);

            if (kb.hKey.wasPressedThisFrame) IssueAll(_ => ShipOrder.Hold());
            if (kb.fKey.wasPressedThisFrame && Selected.Count > 0) Rig.Follow(Selected[0]);
            if (kb.ctrlKey.isPressed && kb.aKey.wasPressedThisFrame) SelectAll();

            for (int i = 0; i < Digits.Length; i++)
            {
                if (!kb[Digits[i]].wasPressedThisFrame) continue;
                if (kb.ctrlKey.isPressed) _groups[i] = new List<int>(Selected);
                else if (_groups.TryGetValue(i, out var g))
                {
                    Selected.Clear();
                    Selected.AddRange(g);
                    Prune();
                }
            }
        }

        void Prune()
        {
            Selected.RemoveAll(id =>
            {
                var s = Runner.World.ShipById(id);
                return s == null || !s.Alive;
            });
        }

        bool Own(Ship s) => s != null && s.Alive && s.Side == PlayerSide;

        // Nearest ship whose on-screen hull covers the cursor. Side -1 = any.
        int PickShip(Vector2 screen, int side)
        {
            var cam = Rig.Camera;
            int best = -1;
            float bestD = float.MaxValue;
            foreach (var kv in Runner.Presenter.Views)
            {
                var ship = Runner.World.ShipById(kv.Key);
                if (ship == null || !ship.Alive || (side >= 0 && ship.Side != side)) continue;
                var view = kv.Value;
                Vector3 c = cam.WorldToScreenPoint(view.transform.position);
                if (c.z <= 0f) continue;
                Vector3 edge = cam.WorldToScreenPoint(view.transform.position + cam.transform.right * ship.Def.HalfLength);
                float r = Mathf.Max(14f, Vector2.Distance(c, edge) * 0.7f);
                float d = Vector2.Distance(c, screen);
                if (d < r && c.z < bestD)
                {
                    bestD = c.z;
                    best = ship.Id;
                }
            }
            return best;
        }

        void ClickSelect(Vector2 mp, bool add)
        {
            int id = PickShip(mp, PlayerSide);
            if (!add) Selected.Clear();
            if (id < 0) return;

            bool doubleClick = id == _lastClickShip && Time.unscaledTime - _lastClickTime < 0.35f;
            _lastClickShip = id;
            _lastClickTime = Time.unscaledTime;
            if (doubleClick)
            {
                var cls = Runner.World.ShipById(id).Def;
                foreach (var s in Runner.World.Ships)
                    if (Own(s) && s.Def == cls && OnScreen(s) && !Selected.Contains(s.Id)) Selected.Add(s.Id);
                return;
            }
            if (add && Selected.Contains(id)) Selected.Remove(id);
            else if (!Selected.Contains(id)) Selected.Add(id);
        }

        bool OnScreen(Ship s)
        {
            var view = Runner.Presenter.ViewOf(s.Id);
            if (view == null) return false;
            Vector3 p = Rig.Camera.WorldToViewportPoint(view.transform.position);
            return p.z > 0f && p.x >= 0f && p.x <= 1f && p.y >= 0f && p.y <= 1f;
        }

        void BoxSelect(Rect r, bool add)
        {
            if (!add) Selected.Clear();
            foreach (var kv in Runner.Presenter.Views)
            {
                var ship = Runner.World.ShipById(kv.Key);
                if (!Own(ship)) continue;
                Vector3 c = Rig.Camera.WorldToScreenPoint(kv.Value.transform.position);
                if (c.z > 0f && r.Contains(c) && !Selected.Contains(ship.Id)) Selected.Add(ship.Id);
            }
        }

        public void SelectAll()
        {
            Selected.Clear();
            foreach (var s in Runner.World.Ships)
                if (Own(s)) Selected.Add(s.Id);
        }

        // The same orders the mouse gives, for scripts and tests.
        public void OrderAttack(int enemy) => IssueAll(_ => ShipOrder.Attack(enemy));

        public void OrderMove(Vector3 point)
        {
            Vector3 centre = SelectionCentre();
            IssueAll(ship => ShipOrder.MoveTo((point + (ship.Position.ToUnity() - centre)).ToSim()));
            LastOrderPoint = point;
            LastOrderWasAttack = false;
            LastOrderTime = Time.unscaledTime;
        }

        void RightClick(Vector2 mp, bool face)
        {
            int enemy = PickShip(mp, 1 - PlayerSide);
            if (enemy >= 0 && !face)
            {
                IssueAll(_ => ShipOrder.Attack(enemy));
                LastOrderPoint = Runner.Presenter.ViewOf(enemy).transform.position;
                LastOrderWasAttack = true;
                LastOrderTime = Time.unscaledTime;
                return;
            }

            if (!GroundPoint(mp, out Vector3 point)) return;
            LastOrderPoint = point;
            LastOrderWasAttack = false;
            LastOrderTime = Time.unscaledTime;

            if (face)
            {
                IssueAll(_ => ShipOrder.Face(point.ToSim()));
                return;
            }

            // The group keeps its shape around the click, squeezed if it was spread wide.
            Vector3 centre = SelectionCentre();
            float spread = 0f;
            foreach (int id in Selected)
                spread = Mathf.Max(spread, (Runner.World.ShipById(id).Position.ToUnity() - centre).magnitude);
            float squeeze = spread > 1500f ? 1500f / spread : 1f;
            IssueAll(ship =>
            {
                Vector3 offset = (ship.Position.ToUnity() - centre) * squeeze;
                return ShipOrder.MoveTo((point + offset).ToSim());
            });
        }

        // The click on the plane at the selection's mean height.
        bool GroundPoint(Vector2 mp, out Vector3 point)
        {
            var plane = new Plane(Vector3.up, new Vector3(0f, SelectionCentre().y, 0f));
            var ray = Rig.Camera.ScreenPointToRay(mp);
            if (plane.Raycast(ray, out float t) && t < 60000f)
            {
                point = ray.GetPoint(t);
                return true;
            }
            point = default;
            return false;
        }

        public Vector3 SelectionCentre()
        {
            if (Selected.Count == 0) return Rig.FocusPoint;
            Vector3 c = Vector3.zero;
            foreach (int id in Selected) c += Runner.World.ShipById(id).Position.ToUnity();
            return c / Selected.Count;
        }

        void IssueAll(System.Func<Ship, ShipOrder> order)
        {
            foreach (int id in Selected)
            {
                var ship = Runner.World.ShipById(id);
                if (Own(ship)) Runner.World.Issue(id, order(ship));
            }
        }
    }
}
