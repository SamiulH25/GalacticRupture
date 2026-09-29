using System.IO;
using UnityEngine;

namespace GalacticRupture.Game
{
    // `-grCapture <dir>` on the player command line: frames a set of shots at fixed sim times,
    // saves each as a PNG and quits. Used to review the look without a person at the screen.
    public sealed class CaptureMode : MonoBehaviour
    {
        struct Shot
        {
            public float Time;
            public bool Enemy;      // frame the first living enemy instead of the flagship
            public float RelYaw;    // relative to the ship's heading
            public float Pitch;
            public float Distance;
            public Vector3 Offset;  // ship-heading frame
            public string Name;
        }

        public SimRunner Runner;
        public CameraRig Rig;

        public static bool Active { get; private set; }

        string _dir;
        int _next;
        bool _framed;
        float _quitAt = -1f;

        static readonly Shot[] Shots =
        {
            new Shot { Time = 36f, RelYaw = -35f, Pitch = 11f, Distance = 640f, Offset = new Vector3(0f, 14f, 10f), Name = "01_quarter" },
            new Shot { Time = 44f, RelYaw = 18f, Pitch = 4f, Distance = 380f, Offset = new Vector3(0f, 20f, 70f), Name = "02_bow_low" },
            new Shot { Time = 52f, RelYaw = 60f, Pitch = 24f, Distance = 2400f, Offset = Vector3.zero, Name = "03_wide" },
            new Shot { Time = 60f, RelYaw = 115f, Pitch = 18f, Distance = 170f, Offset = new Vector3(0f, 24f, 72f), Name = "04_turret" },
            new Shot { Time = 68f, Enemy = true, RelYaw = 150f, Pitch = 10f, Distance = 150f, Offset = Vector3.zero, Name = "05_corvette" },
            new Shot { Time = 80f, RelYaw = 205f, Pitch = 9f, Distance = 720f, Offset = new Vector3(0f, 10f, -60f), Name = "06_stern" },
            new Shot { Time = 95f, RelYaw = -80f, Pitch = 30f, Distance = 900f, Offset = Vector3.zero, Name = "07_broadside_top" },
        };

        void Awake()
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-grCapture") _dir = args[i + 1];
            Active = _dir != null;
            if (!Active)
            {
                enabled = false;
                return;
            }
            Directory.CreateDirectory(_dir);
            Rig.Cinematic = false;
        }

        void Update()
        {
            if (_quitAt > 0f)
            {
                if (Time.unscaledTime > _quitAt) Application.Quit();
                return;
            }
            if (_next >= Shots.Length)
            {
                _quitAt = Time.unscaledTime + 1.5f;
                return;
            }

            var shot = Shots[_next];
            float t = Runner.World.Time;
            if (!_framed && t >= shot.Time - 2.5f)
            {
                int ship = shot.Enemy ? FirstEnemy() : Runner.Toy.Flagship;
                var view = Runner.Presenter.ViewOf(ship);
                float heading = view != null ? view.transform.eulerAngles.y : 0f;
                Rig.SnapTo(ship, heading + shot.RelYaw, shot.Pitch, shot.Distance, shot.Offset);
                _framed = true;
            }
            if (_framed && t >= shot.Time)
            {
                Capture(Path.Combine(_dir, shot.Name + ".png"));
                Debug.Log($"[capture] {shot.Name} at t={t:F1}");
                _next++;
                _framed = false;
            }
        }

        // Renders the main camera offscreen at a fixed size, whatever the window manager did to the window.
        void Capture(string path)
        {
            var cam = Rig.GetComponent<Camera>();
            var rt = RenderTexture.GetTemporary(new RenderTextureDescriptor(1920, 1080, RenderTextureFormat.ARGB32, 24) { msaaSamples = 1, sRGB = true });
            var prev = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = prev;
            var active = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            tex.Apply();
            RenderTexture.active = active;
            RenderTexture.ReleaseTemporary(rt);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Destroy(tex);
        }

        int FirstEnemy()
        {
            foreach (var s in Runner.World.Ships)
                if (s.Alive && s.Side != Sim.TurretToy.PlayerSide && s.Def.Class == Sim.HullClass.Corvette) return s.Id;
            return Runner.Toy.Flagship;
        }
    }
}
