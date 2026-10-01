using System.Collections.Generic;
using GalacticRupture.Sim;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GalacticRupture.Game
{
    // The player's hands on the fleet.
    //   Left click / drag       select (Shift adds, Ctrl+A all, double-click selects that class)
    //   Right click space       move there in formation: the line in front, escorts behind,
    //                           facing the way it travelled; everyone keeps the slowest's pace
    //   Right drag              the same, facing the way you dragged
    //   Alt + right click       move there keeping the group's present shape
    //   Right click enemy       attack together: close to gun range, turn broadside, focus fire
    //   Ctrl + right click      face that point (turn the broadside you want)
    //   H                       hold position
    //   F                       follow the selection
    //   Ctrl+1..9 / 1..9        set / recall a control group
    //   With reserves picked in the reinforcements panel: left click inside the zone around
    //   your ships brings them in there; right click or Esc puts them back.
    public sealed class CommandInput : MonoBehaviour
    {
        public SimRunner Runner;
        public CameraRig Rig;
        public int PlayerSide = 0;
        // Cosmetic only; the sim never reads it.
        public CursorSkin Skin;

        public readonly List<int> Selected = new List<int>();
        readonly Dictionary<int, List<int>> _groups = new Dictionary<int, List<int>>();

        public bool Dragging { get; private set; }
        public Rect DragRect { get; private set; }
        public int Hovered { get; private set; } = -1;

        // Feedback for the order markers: where the last order went, and when.
        public Vector3 LastOrderPoint { get; private set; }
        public float LastOrderTime { get; private set; } = -10f;
        public bool LastOrderWasAttack { get; private set; }

        // The order as a slip of paper: what was ordered, and what the ships say back (HUD only).
        public string DirectiveTitle { get; private set; } = "";
        public string DirectiveBody { get; private set; } = "";
        public string DirectiveReadback { get; private set; } = "";
        public int DirectiveNumber { get; private set; }
        public float DirectiveTime { get; private set; } = -100f;

        static string Called(Ship s)
        {
            string id = string.IsNullOrEmpty(s.Def.Design) ? s.Def.Id : s.Def.Design;
            return string.IsNullOrEmpty(id) ? "ship" : char.ToUpperInvariant(id[0]) + id.Substring(1) + " " + s.Id;
        }

        void Note(string title, string body)
        {
            int dead = 0, ships = 0;
            Ship lead = null;
            foreach (int id in Selected)
            {
                var s = Runner.World.ShipById(id);
                if (!Own(s)) continue;
                lead ??= s;
                ships++;
                foreach (var m in s.Mounts) if (!m.Alive) dead++;
            }
            if (ships == 0) return;
            string who = ships == 1 ? Called(lead) : $"{ships} ships";
            DirectiveNumber++;
            DirectiveTitle = title;
            DirectiveBody = body;
            DirectiveReadback = dead > 0 ? $"{who}: acknowledged. {dead} gun{(dead > 1 ? "s" : "")} cannot bear." : $"{who}: acknowledged. All batteries answering.";
            DirectiveTime = Time.unscaledTime;
            GameAudio.Ui("confirmation", 0.35f);
        }

        // Reserves picked in the HUD, waiting for the player to say where they arrive.
        public readonly List<ReserveShip> Placing = new List<ReserveShip>();
        public Vector3 PlacePoint { get; private set; }
        public bool PlaceValid { get; private set; }
        public bool PlaceOnField { get; private set; }

        // A right-button order in progress: where it went down, and the drag that sets facing.
        public bool Planning { get; private set; }
        public bool PlanDragged { get; private set; }
        public Vector3 PlanPoint { get; private set; }
        public float PlanYaw { get; private set; }
        Vector2 _rightStart;
        int _rightEnemy = -1;

        Vector2 _dragStart;
        bool _pressOnUi;
        float _lastClickTime;
        int _lastClickShip = -1;

        static readonly Key[] Digits =
        {
            Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9,
        };

        void Update()
        {
            if (CaptureMode.Active || BenchMode.Active || RtsCapture.Active || CampaignCapture.Active || Runner.World == null) return;
            var mouse = Mouse.current;
            var kb = Keyboard.current;
            if (mouse == null || kb == null) return;
            Prune();

            Vector2 mp = mouse.position.ReadValue();
            Hovered = PickShip(mp, -1);
            UpdateCursor(mp, mouse, kb);

            // Clicks on the HUD are the HUD's.
            bool overUi = Hud.Over(mp);
            if (Placing.Count > 0 && Place(mp, mouse, kb, overUi)) return;
            if (mouse.leftButton.wasPressedThisFrame)
            {
                _dragStart = mp;
                Dragging = false;
                _pressOnUi = overUi;
            }
            if (_pressOnUi)
            {
                if (mouse.leftButton.wasReleasedThisFrame) _pressOnUi = false;
                mouse = null;
            }
            if (mouse != null && mouse.leftButton.isPressed && (mp - _dragStart).sqrMagnitude > 64f)
            {
                Dragging = true;
                DragRect = Rect.MinMaxRect(Mathf.Min(mp.x, _dragStart.x), Mathf.Min(mp.y, _dragStart.y),
                    Mathf.Max(mp.x, _dragStart.x), Mathf.Max(mp.y, _dragStart.y));
            }
            if (mouse != null && mouse.leftButton.wasReleasedThisFrame)
            {
                bool add = kb.shiftKey.isPressed;
                if (Dragging) BoxSelect(DragRect, add);
                else ClickSelect(mp, add);
                Dragging = false;
            }

            RightButton(mp, Mouse.current, kb, overUi);

            if (kb.zKey.wasPressedThisFrame) UseAbility(Ability.Engines);
            if (kb.xKey.wasPressedThisFrame) UseAbility(Ability.Shields);
            if (kb.vKey.wasPressedThisFrame) UseAbility(Ability.Weapons);

            if (kb.hKey.wasPressedThisFrame)
            {
                IssueAll(_ => ShipOrder.Hold());
                Note("HOLD POSITION", "Cease manoeuvre. Guns free.");
            }
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

        // Returns true while placement owns the mouse.
        bool Place(Vector2 mp, Mouse mouse, Keyboard kb, bool overUi)
        {
            var res = Runner.Reinforcements;
            if (res != null) Placing.RemoveAll(r => !res.Pool[PlayerSide].Contains(r));
            if (res == null || Placing.Count == 0)
            {
                Placing.Clear();
                return false;
            }
            var plane = new Plane(Vector3.up, Vector3.zero);
            var ray = Rig.Camera.ScreenPointToRay(mp);
            PlaceOnField = false;
            if (!overUi && plane.Raycast(ray, out float t) && t < 60000f)
            {
                PlaceOnField = true;
                PlacePoint = ray.GetPoint(t);
            }
            PlaceValid = PlaceOnField && Reinforcements.InZone(Runner.World, PlayerSide, PlacePoint.ToSim());

            if (kb.escapeKey.wasPressedThisFrame || (mouse.rightButton.wasPressedThisFrame && !overUi))
            {
                Placing.Clear();
                return true;
            }
            if (mouse.leftButton.wasPressedThisFrame && !overUi)
            {
                // The release that follows is the placement's, not a selection click.
                _pressOnUi = true;
                if (!PlaceValid) return true;
                res.RequestAt(PlayerSide, Placing, PlacePoint.ToSim());
                LastOrderPoint = PlacePoint;
                LastOrderWasAttack = false;
                LastOrderTime = Time.unscaledTime;
                Placing.Clear();
                return true;
            }
            return !overUi;
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
            GameAudio.Ui("select", 0.4f);

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

        // Every selected ship that can, diverts power (Empire at War's ship abilities).
        public void UseAbility(Ability a)
        {
            foreach (int id in Selected)
            {
                var ship = Runner.World.ShipById(id);
                if (Own(ship) && ship.CanUse(a)) Runner.World.Use(id, a);
            }
        }

        // The same orders the mouse gives, for scripts and tests.
        public void OrderAttack(int enemy)
        {
            int group = Selected.Count > 1 ? Runner.World.NewGroup() : -1;
            IssueAll(_ =>
            {
                var o = ShipOrder.Attack(enemy);
                o.Group = group;
                return o;
            });
            var target = Runner.World.ShipById(enemy);
            string tn = target == null ? "the hostile" : Called(target);
            Note("CONCENTRATE FIRE", $"All batteries, hostile {tn}. Hold converging arcs.");
        }

        public void OrderMove(Vector3 point) => OrderMove(point, DefaultYaw(point));

        public void OrderMove(Vector3 point, float yaw)
        {
            var ships = OwnSelected();
            if (ships.Count == 0) return;
            Formation.Move(Runner.World, ships, point.ToSim(), yaw);
            Note("PROCEED", ships.Count == 1 ? "Proceed to the marked position." : "Proceed to the marked position, keeping station on each other.");
            LastOrderPoint = point;
            LastOrderWasAttack = false;
            LastOrderTime = Time.unscaledTime;
        }

        // Where the ships of a planned move would end up, for the overlay.
        public System.Numerics.Vector3[] PlanSlots() => Formation.Slots(OwnSelected(), PlanPoint.ToSim(), PlanYaw);

        List<Ship> OwnSelected()
        {
            var list = new List<Ship>();
            foreach (int id in Selected)
            {
                var s = Runner.World.ShipById(id);
                if (Own(s) && s.Def.MaxSpeed > 0f) list.Add(s);
            }
            return list;
        }

        // Facing the way the group travels; for a short hop, the way it already faces.
        float DefaultYaw(Vector3 point)
        {
            Vector3 d = point - SelectionCentre();
            d.y = 0f;
            if (d.magnitude > 400f) return Mathf.Atan2(d.x, d.z);
            float sx = 0f, sz = 0f;
            foreach (var s in OwnSelected())
            {
                sx += Mathf.Sin(s.Yaw);
                sz += Mathf.Cos(s.Yaw);
            }
            return Mathf.Atan2(sx, sz);
        }

        /// The cursor promises the order that `RightClick` would actually give. That mapping is
        /// not free: `RightClick` picks attack before move, and treats Ctrl as face
        /// regardless of what is under the pointer, so the cursor has to test in the same
        /// order or it will show amber over an enemy that a plain click would attack.
        void UpdateCursor(Vector2 mp, Mouse mouse, Keyboard kb)
        {
            if (Skin == null) return;
            if (Hud.Over(mp)) { Skin.Apply(CursorState.Default); return; }

            // A marquee is a drag in progress, whatever is underneath it.
            if (Dragging) { Skin.Apply(CursorState.BoxSelect); return; }

            // Right button held: show the order that releasing here would produce.
            if (Selected.Count > 0 && mouse.rightButton.isPressed)
            {
                if (kb.ctrlKey.isPressed) { Skin.Apply(CursorState.Face); return; }
                Skin.Apply(PickShip(mp, 1 - PlayerSide) >= 0 ? CursorState.Attack : CursorState.Move);
                return;
            }

            int hover = PickShip(mp, -1);
            if (hover < 0) { Skin.Apply(CursorState.Default); return; }
            Skin.Apply(Own(Runner.World.ShipById(hover)) ? CursorState.OwnShip : CursorState.Select);
        }

        // Orders go on release, so a drag can set the formation's facing first.
        void RightButton(Vector2 mp, Mouse mouse, Keyboard kb, bool overUi)
        {
            if (mouse.rightButton.wasPressedThisFrame)
            {
                Planning = PlanDragged = false;
                if (Selected.Count == 0 || overUi) return;
                _rightStart = mp;
                _rightEnemy = PickShip(mp, 1 - PlayerSide);
                if (!GroundPoint(mp, out Vector3 p)) return;
                PlanPoint = p;
                PlanYaw = DefaultYaw(p);
                Planning = true;
                return;
            }
            if (!Planning) return;
            bool dragged = PlanDragged = (mp - _rightStart).sqrMagnitude > 400f;
            if (dragged && GroundPoint(mp, out Vector3 to))
            {
                Vector3 d = to - PlanPoint;
                if (new Vector2(d.x, d.z).magnitude > 30f) PlanYaw = Mathf.Atan2(d.x, d.z);
            }
            if (!mouse.rightButton.wasReleasedThisFrame) return;
            Planning = false;
            if (Selected.Count == 0) return;
            if (dragged) OrderMove(PlanPoint, PlanYaw);
            else RightClick(_rightStart, kb.ctrlKey.isPressed, kb.altKey.isPressed);
        }

        void RightClick(Vector2 mp, bool face, bool keepShape)
        {
            int enemy = _rightEnemy;
            if (enemy >= 0 && !face && Runner.World.ShipById(enemy) is Ship e && e.Alive)
            {
                OrderAttack(enemy);
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
                Note("PRESENT BROADSIDE", "Turn to the marked bearing. Hold.");
                return;
            }
            if (!keepShape)
            {
                OrderMove(point, PlanYaw);
                return;
            }

            // The group keeps its shape around the click, squeezed if it was spread wide.
            Vector3 centre = SelectionCentre();
            float spread = 0f;
            foreach (int id in Selected)
                spread = Mathf.Max(spread, (Runner.World.ShipById(id).Position.ToUnity() - centre).magnitude);
            float squeeze = spread > 1500f ? 1500f / spread : 1f;
            int group = Selected.Count > 1 ? Runner.World.NewGroup() : -1;
            IssueAll(ship =>
            {
                Vector3 offset = (ship.Position.ToUnity() - centre) * squeeze;
                var o = ShipOrder.MoveTo((point + offset).ToSim());
                o.Group = group;
                return o;
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
