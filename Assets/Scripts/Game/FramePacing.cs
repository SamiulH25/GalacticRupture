using UnityEngine;

namespace GalacticRupture.Game
{
    // Keeps the game from rendering frames nobody sees. Vsync caps it to the display; if the
    // driver ignores vsync (measured frame rate well above the refresh rate) it falls back to a
    // frame-rate cap. Out of focus it drops to 30 fps. The benchmark turns all of this off.
    public sealed class FramePacing : MonoBehaviour
    {
        public static bool Uncapped;   // BenchMode

        const float Background = 30f;
        float _refresh;
        float _window;
        int _frames;
        bool _fallback;
        bool _focused = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            var go = new GameObject("Frame Pacing");
            DontDestroyOnLoad(go);
            go.AddComponent<FramePacing>();
        }

        void Awake()
        {
            double hz = Screen.currentResolution.refreshRateRatio.value;
            _refresh = hz > 20.0 ? (float)hz : 60f;
            Application.focusChanged += f =>
            {
                _focused = f;
                Apply();
            };
            Apply();
        }

        void Apply()
        {
            if (Uncapped)
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = -1;
                return;
            }
            if (!_focused)
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = (int)Background;
                return;
            }
            if (_fallback)
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = Mathf.RoundToInt(Mathf.Min(_refresh, 144f));
            }
            else
            {
                QualitySettings.vSyncCount = 1;
                Application.targetFrameRate = -1;
            }
        }

        void Update()
        {
            if (Uncapped || _fallback || !_focused) return;
            _window += Time.unscaledDeltaTime;
            _frames++;
            if (_window < 3f) return;
            float fps = _frames / _window;
            _window = 0f;
            _frames = 0;
            if (fps > _refresh * 1.3f)
            {
                _fallback = true;
                Debug.Log($"[pacing] vsync ignored ({fps:0} fps on a {_refresh:0} Hz display); capping at {Mathf.Min(_refresh, 144f):0}");
                Apply();
            }
        }
    }
}
