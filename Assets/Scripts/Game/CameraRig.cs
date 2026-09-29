using UnityEngine;
using UnityEngine.InputSystem;

namespace GalacticRupture.Game
{
    // Tactical camera: follows a ship, orbits inside a restricted arc (IDEAS.md §8.9).
    // Right-drag orbits, wheel zooms, Tab cycles ships, F returns to the flagship,
    // C toggles a slow cinematic drift. FocusOffset is in the focused ship's heading frame.
    [DefaultExecutionOrder(100)]
    public sealed class CameraRig : MonoBehaviour
    {
        public SimRunner Runner;
        public float Yaw = 145f;
        public float Pitch = 14f;
        public float Distance = 720f;
        public float MinPitch = -18f;
        public float MaxPitch = 70f;
        public float MinDistance = 40f;
        public float MaxDistance = 9000f;
        public bool Cinematic = true;
        public Vector3 FocusOffset = new Vector3(0f, 12f, 0f);

        public int FocusShip = -1;
        Vector3 _focus;
        float _smoothDistance;
        bool _initialised;
        float _idle;

        void LateUpdate()
        {
            if (Runner == null || Runner.World == null) return;
            HandleInput();

            if (FocusShip < 0 || Runner.Presenter.ViewOf(FocusShip) == null) FocusShip = Runner.Toy.Flagship;
            var view = Runner.Presenter.ViewOf(FocusShip);
            Vector3 target = view != null
                ? view.transform.position + Quaternion.Euler(0f, view.transform.eulerAngles.y, 0f) * FocusOffset
                : _focus;

            if (!_initialised)
            {
                _focus = target;
                _smoothDistance = Distance;
                _initialised = true;
            }

            float dt = Time.unscaledDeltaTime;
            _focus = Vector3.Lerp(_focus, target, 1f - Mathf.Exp(-dt * 6f));
            _smoothDistance = Mathf.Lerp(_smoothDistance, Distance, 1f - Mathf.Exp(-dt * 5f));
            if (Cinematic && _idle > 4f) Yaw += dt * 2.2f;

            var rot = Quaternion.Euler(Pitch, Yaw, 0f);
            transform.SetPositionAndRotation(_focus - rot * Vector3.forward * _smoothDistance, rot);
        }

        void HandleInput()
        {
            _idle += Time.unscaledDeltaTime;
            var mouse = Mouse.current;
            var kb = Keyboard.current;
            if (mouse != null)
            {
                if (mouse.rightButton.isPressed)
                {
                    Vector2 d = mouse.delta.ReadValue();
                    Yaw += d.x * 0.18f;
                    Pitch = Mathf.Clamp(Pitch - d.y * 0.18f, MinPitch, MaxPitch);
                    _idle = 0f;
                }
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    Distance = Mathf.Clamp(Distance * Mathf.Pow(0.88f, Mathf.Sign(scroll)), MinDistance, MaxDistance);
                    _idle = 0f;
                }
            }
            if (kb != null)
            {
                if (kb.tabKey.wasPressedThisFrame) CycleFocus(kb.shiftKey.isPressed ? -1 : 1);
                if (kb.fKey.wasPressedThisFrame) FocusShip = Runner.Toy.Flagship;
                if (kb.cKey.wasPressedThisFrame) Cinematic = !Cinematic;
            }
        }

        void CycleFocus(int dir)
        {
            var ships = Runner.World.Ships;
            int n = ships.Count;
            int start = FocusShip < 0 ? 0 : FocusShip;
            for (int k = 1; k <= n; k++)
            {
                int id = ((start + dir * k) % n + n) % n;
                if (ships[id].Alive)
                {
                    FocusShip = id;
                    float size = ships[id].Def.HalfLength;
                    Distance = Mathf.Clamp(size * 4.2f, MinDistance, MaxDistance);
                    FocusOffset = new Vector3(0f, ships[id].Def.Radius * 0.5f, 0f);
                    return;
                }
            }
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
