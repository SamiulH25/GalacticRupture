using UnityEngine;
using UnityEngine.InputSystem;

namespace GalacticRupture.Game
{
    // Galaxy map camera: WASD pans, Q/E turns, middle-drag tilts and turns, wheel zooms.
    public sealed class MapCamera : MonoBehaviour
    {
        public Vector3 Focus;
        public float Yaw;
        public float Pitch = 58f;
        public float Distance = 6500f;
        public float MinDistance = 800f;
        public float MaxDistance = 30000f;

        Vector3 _focus;
        float _distance;
        bool _init;

        public void Snap(Vector3 focus)
        {
            Focus = focus;
            _init = false;
        }

        void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (mouse != null)
            {
                if (mouse.middleButton.isPressed)
                {
                    Vector2 d = mouse.delta.ReadValue();
                    Yaw += d.x * 0.2f;
                    Pitch = Mathf.Clamp(Pitch - d.y * 0.2f, 15f, 88f);
                }
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                    Distance = Mathf.Clamp(Distance * Mathf.Pow(0.87f, Mathf.Sign(scroll)), MinDistance, MaxDistance);
            }
            if (kb != null)
            {
                Vector2 pan = Vector2.zero;
                if (kb.wKey.isPressed) pan.y += 1f;
                if (kb.sKey.isPressed) pan.y -= 1f;
                if (kb.dKey.isPressed) pan.x += 1f;
                if (kb.aKey.isPressed) pan.x -= 1f;
                Focus += Quaternion.Euler(0f, Yaw, 0f) * new Vector3(pan.x, 0f, pan.y) * Distance * 0.8f * dt;
                if (kb.qKey.isPressed) Yaw -= 70f * dt;
                if (kb.eKey.isPressed) Yaw += 70f * dt;
            }

            if (!_init)
            {
                _focus = Focus;
                _distance = Distance;
                _init = true;
            }
            _focus = Vector3.Lerp(_focus, Focus, 1f - Mathf.Exp(-dt * 8f));
            _distance = Mathf.Lerp(_distance, Distance, 1f - Mathf.Exp(-dt * 6f));
            var rot = Quaternion.Euler(Pitch, Yaw, 0f);
            transform.SetPositionAndRotation(_focus - rot * Vector3.forward * _distance, rot);
        }
    }
}
