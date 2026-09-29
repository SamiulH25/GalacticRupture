using UnityEngine;
using UnityEngine.InputSystem;

namespace GalacticRupture.Game
{
    // RTS camera over a free focus point: WASD pans, Q/E turn, middle-drag orbits inside a
    // restricted arc (IDEAS.md §8.9), wheel zooms, F follows the selection (any pan breaks
    // follow), C toggles a slow cinematic drift. FocusOffset is in the followed ship's heading frame.
    [DefaultExecutionOrder(100)]
    public sealed class CameraRig : MonoBehaviour
    {
        public SimRunner Runner;
        public float Yaw = 150f;
        public float Pitch = 32f;
        public float Distance = 1800f;
        public float MinPitch = -15f;
        public float MaxPitch = 80f;
        public float MinDistance = 40f;
        public float MaxDistance = 16000f;
        public bool Cinematic;
        public Vector3 FocusOffset;

        // Ship being followed, or -1 for a free focus point.
        public int FocusShip = -1;
        public Vector3 FocusPoint;

        Vector3 _focus;
        float _smoothDistance;
        bool _initialised;
        float _idle;

        public Camera Camera { get; private set; }

        void Awake() => Camera = GetComponent<Camera>();

        void LateUpdate()
        {
            if (Runner == null || Runner.World == null) return;
            if (!_initialised && FocusShip < 0)
            {
                var flag = Runner.Presenter.ViewOf(Runner.Flagship);
                if (flag != null) FocusPoint = flag.transform.position;
            }
            HandleInput();

            if (FocusShip >= 0)
            {
                var view = Runner.Presenter.ViewOf(FocusShip);
                if (view != null && !view.Wrecked)
                    FocusPoint = view.transform.position + Quaternion.Euler(0f, view.transform.eulerAngles.y, 0f) * FocusOffset;
                else
                    FocusShip = -1;
            }

            if (!_initialised)
            {
                _focus = FocusPoint;
                _smoothDistance = Distance;
                _initialised = true;
            }

            float dt = Time.unscaledDeltaTime;
            _focus = Vector3.Lerp(_focus, FocusPoint, 1f - Mathf.Exp(-dt * 8f));
            _smoothDistance = Mathf.Lerp(_smoothDistance, Distance, 1f - Mathf.Exp(-dt * 6f));
            if (Cinematic && _idle > 4f) Yaw += dt * 2.2f;

            var rot = Quaternion.Euler(Pitch, Yaw, 0f);
            transform.SetPositionAndRotation(_focus - rot * Vector3.forward * _smoothDistance, rot);
        }

        void HandleInput()
        {
            if (CaptureMode.Active || BenchMode.Active || RtsCapture.Active) return;
            float dt = Time.unscaledDeltaTime;
            _idle += dt;
            var mouse = Mouse.current;
            var kb = Keyboard.current;
            if (mouse != null)
            {
                if (mouse.middleButton.isPressed)
                {
                    Vector2 d = mouse.delta.ReadValue();
                    Yaw += d.x * 0.2f;
                    Pitch = Mathf.Clamp(Pitch - d.y * 0.2f, MinPitch, MaxPitch);
                    _idle = 0f;
                }
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    Distance = Mathf.Clamp(Distance * Mathf.Pow(0.87f, Mathf.Sign(scroll)), MinDistance, MaxDistance);
                    _idle = 0f;
                }
            }
            if (kb == null) return;

            Vector2 pan = Vector2.zero;
            if (kb.wKey.isPressed) pan.y += 1f;
            if (kb.sKey.isPressed) pan.y -= 1f;
            if (kb.dKey.isPressed) pan.x += 1f;
            if (kb.aKey.isPressed && !kb.ctrlKey.isPressed) pan.x -= 1f;
            if (pan != Vector2.zero)
            {
                var flat = Quaternion.Euler(0f, Yaw, 0f);
                float speed = Distance * 0.9f * (kb.shiftKey.isPressed ? 2.5f : 1f);
                FocusPoint += flat * new Vector3(pan.x, 0f, pan.y) * speed * dt;
                FocusShip = -1;
                _idle = 0f;
            }
            if (kb.qKey.isPressed) Yaw -= 70f * dt;
            if (kb.eKey.isPressed) Yaw += 70f * dt;
            if (kb.cKey.wasPressedThisFrame) Cinematic = !Cinematic;
        }

        public void Follow(int ship)
        {
            var s = Runner.World.ShipById(ship);
            if (s == null) return;
            FocusShip = ship;
            FocusOffset = Vector3.zero;
            Distance = Mathf.Clamp(s.Def.HalfLength * 5f, MinDistance, MaxDistance);
        }

        public void SnapTo(int ship, float yaw, float pitch, float distance, Vector3 offset)
        {
            FocusShip = ship;
            Yaw = yaw;
            Pitch = pitch;
            Distance = distance;
            FocusOffset = offset;
            _initialised = false;
        }
    }
}
