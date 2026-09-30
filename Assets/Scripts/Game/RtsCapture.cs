using System.IO;
using GalacticRupture.Sim;
using UnityEngine;

namespace GalacticRupture.Game
{
    // `-grCaptureRts <dir>`: plays the skirmish through the same command API the mouse uses
    // (select, move, attack), screenshots the full screen with UI at set times, and quits.
    public sealed class RtsCapture : MonoBehaviour
    {
        public SimRunner Runner;
        public CameraRig Rig;
        public CommandInput Input;

        public static bool Active { get; private set; }

        string _dir;
        int _step;
        float _quitAt = -1f;

        void Awake()
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-grCaptureRts") _dir = args[i + 1];
            Active = _dir != null;
            if (!Active)
            {
                enabled = false;
                return;
            }
            Directory.CreateDirectory(_dir);
        }

        void Shot(string name)
        {
            ScreenCapture.CaptureScreenshot(Path.Combine(_dir, name + ".png"));
            Debug.Log($"[rts] {name} at t={Runner.World.Time:F1}");
        }

        void Update()
        {
            if (_quitAt > 0f)
            {
                if (Time.unscaledTime > _quitAt) Application.Quit();
                return;
            }
            var w = Runner.World;
            float t = w.Time;
            // -grArsenal: close-ups of the gun families first, in real time with the battle paused.
            if (_closeup < Closeups.Length && System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-grArsenal") >= 0)
            {
                Closeup(w);
                return;
            }
            switch (_step)
            {
                case 0 when t > 0.5f:
                    Input.SelectAll();
                    Rig.SnapTo(Runner.Flagship, 200f, 38f, 2200f, Vector3.zero);
                    _step++;
                    break;
                case 1 when t > 1.5f:
                    Input.OrderMove(new Vector3(0f, 0f, -600f));
                    _step++;
                    break;
                case 2 when t > 5f:
                    Shot("rts_01_move");
                    _step++;
                    break;
                case 3 when t > 18f:
                    int target = NearestEnemy(w, Destroyer: true);
                    if (target >= 0) Input.OrderAttack(target);
                    Rig.SnapTo(Runner.Flagship, 240f, 26f, 1600f, Vector3.zero);
                    _step++;
                    break;
                case 4 when t > 21f:
                    Shot("rts_02_attack");
                    _step++;
                    break;
                case 5 when t > 40f:
                    Rig.SnapTo(Runner.Flagship, 120f, 18f, 900f, Vector3.zero);
                    _step++;
                    break;
                case 6 when t > 42f:
                    Shot("rts_03_engaged");
                    _step++;
                    break;
                case 7 when t > 70f:
                    if (NearestEnemy(w, Destroyer: false) is int e && e >= 0) Input.OrderAttack(e);
                    Rig.SnapTo(Runner.Flagship, 60f, 30f, 2600f, Vector3.zero);
                    _step++;
                    break;
                case 8 when t > 74f:
                    Shot("rts_04_battle");
                    _step++;
                    break;
                case 9 when t > 110f || (Runner.Battle != null && Runner.Battle.State != BattleState.Fighting && t > 80f):
                    Shot("rts_05_end");
                    Debug.Log($"[rts] state {Runner.Battle?.State}");
                    _quitAt = Time.unscaledTime + 1.5f;
                    _step++;
                    break;
            }
        }

        int _closeup;
        float _closeupAt;

        // Ship index on the player's side, yaw, pitch, distance, and a local offset to look at.
        static readonly (int Ship, float Yaw, float Pitch, float Distance, Vector3 Offset)[] Closeups =
        {
            (0, 150f, 22f, 230f, new Vector3(0f, 10f, 70f)),
            (0, 30f, 22f, 230f, new Vector3(0f, 10f, -60f)),
            (0, 90f, 30f, 150f, new Vector3(10f, 10f, 20f)),
            (1, 140f, 25f, 120f, new Vector3(0f, 5f, 0f)),
            (2, 140f, 25f, 120f, new Vector3(0f, 5f, 0f)),
            (3, 140f, 25f, 120f, new Vector3(0f, 5f, 0f)),
        };

        void Closeup(World w)
        {
            Runner.TimeScale = 0f;
            if (Time.unscaledTime < _closeupAt) return;
            var c = Closeups[_closeup];
            int id = -1, n = 0;
            foreach (var s in w.Ships)
                if (s.Side == 0 && n++ == c.Ship) id = s.Id;
            if (_phase == 0)
            {
                Rig.SnapTo(id, c.Yaw, c.Pitch, c.Distance, c.Offset);
                _phase = 1;
                _closeupAt = Time.unscaledTime + 1.5f;
                return;
            }
            ScreenCapture.CaptureScreenshot(Path.Combine(_dir, $"arsenal_{_closeup}.png"));
            Debug.Log($"[rts] arsenal close-up {_closeup}");
            _closeup++;
            _phase = 0;
            _closeupAt = Time.unscaledTime + 0.3f;
            if (_closeup >= Closeups.Length) Runner.TimeScale = 1f;
        }

        int _phase;

        int NearestEnemy(World w, bool Destroyer)
        {
            var flag = w.ShipById(Runner.Flagship);
            if (flag == null) return -1;
            int best = -1;
            float bestD = float.MaxValue;
            foreach (var s in w.Ships)
            {
                if (!s.Alive || s.Side == 0) continue;
                if (Destroyer && s.Def.Class != HullClass.Destroyer) continue;
                float d = System.Numerics.Vector3.DistanceSquared(s.Position, flag.Position);
                if (d < bestD) { bestD = d; best = s.Id; }
            }
            return best;
        }
    }
}
