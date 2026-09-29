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
            public bool Action;     // frame the ship most recently hit on its shields
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
            new Shot { Time = 52f, Action = true, RelYaw = 70f, Pitch = 16f, Name = "03_action" },
            new Shot { Time = 60f, RelYaw = 115f, Pitch = 18f, Distance = 170f, Offset = new Vector3(0f, 24f, 72f), Name = "04_turret" },
            new Shot { Time = 68f, Enemy = true, RelYaw = 150f, Pitch = 10f, Distance = 150f, Offset = Vector3.zero, Name = "05_corvette" },
            new Shot { Time = 80f, RelYaw = 205f, Pitch = 9f, Distance = 720f, Offset = new Vector3(0f, 10f, -60f), Name = "06_stern" },
            new Shot { Time = 88f, Action = true, RelYaw = -60f, Pitch = 12f, Name = "07_action" },
            new Shot { Time = 100f, RelYaw = -80f, Pitch = 30f, Distance = 1600f, Offset = Vector3.zero, Name = "08_battle_wide" },
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
            if (!_framed && t >= shot.Time - (shot.Action ? 0.6f : 2.5f))
            {
                int ship = shot.Enemy ? FirstEnemy() : Runner.Toy.Flagship;
                float distance = shot.Distance;
                var hit = Runner.World.ShipById(Runner.Presenter.LastShieldHit);
                if (shot.Action && hit != null && hit.Alive)
                    ship = hit.Id;
                var view = Runner.Presenter.ViewOf(ship);
                if (shot.Action && view != null) distance = view.Def.HalfLength * 3.2f + 60f;
                float heading = view != null ? view.transform.eulerAngles.y : 0f;
                Rig.SnapTo(ship, heading + shot.RelYaw, shot.Pitch, distance, shot.Offset);
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
