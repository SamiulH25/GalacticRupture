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
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-grLineup") >= 0)
            {
                Lineup();
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
                    // Call in two ships from reserve.
                    Runner.Reinforcements?.Request(0, 0);
                    Runner.Reinforcements?.Request(0, 0);
                    _step++;
                    break;
                case 2 when t > 9f:
                    Rig.Frame(w.ShipById(Runner.Flagship).Position.ToUnity(), 200f, 55f, 4200f);
                    Shot("rts_01_move");
                    _step++;
                    break;
                case 3 when t > 12f:
                    // The whole field from above: rock, cloud, station, both lines.
                    Rig.Frame(new Vector3(0f, 0f, 0f), 0f, 62f, 15000f);
                    _step++;
                    break;
                case 4 when t > 14f:
                    Shot("rts_01b_field");
                    _step++;
                    break;
                case 5 when t > 14.5f:
                    var station = Runner.Battle != null ? w.ShipById(Runner.Battle.Station) : null;
                    if (station != null) Rig.SnapTo(station.Id, 200f, 22f, 700f, new Vector3(0f, 20f, 0f));
                    _step++;
                    break;
                case 6 when t > 16f:
                    Shot("rts_01c_station");
                    _step++;
                    break;
                case 7 when t > 18f:
                    int target = NearestEnemy(w, Destroyer: true);
                    if (target >= 0) Input.OrderAttack(target);
                    Input.UseAbility(Ability.Engines);
                    Rig.SnapTo(Runner.Flagship, 240f, 26f, 1600f, Vector3.zero);
                    _step++;
                    break;
                case 8 when t > 30f:
                    Shot("rts_02_attack");
                    // The rest at ×4, the speed buttons' top setting.
                    Runner.TimeScale = 4f;
                    _step++;
                    break;
                case 9 when t > 110f:
                    Rig.SnapTo(Runner.Flagship, 120f, 18f, 900f, Vector3.zero);
                    _step++;
                    break;
                case 10 when t > 114f:
                    Input.UseAbility(Ability.Weapons);
                    Shot("rts_03_engaged");
                    _step++;
                    break;
                case 11 when t > 170f:
                    if (NearestEnemy(w, Destroyer: false) is int e && e >= 0) Input.OrderAttack(e);
                    Rig.SnapTo(Runner.Flagship, 60f, 30f, 2600f, Vector3.zero);
                    _step++;
                    break;
                case 12 when t > 178f:
                    Shot("rts_04_battle");
                    _step++;
                    break;
                case 13 when t > 300f || (Runner.Battle != null && Runner.Battle.State != BattleState.Fighting && t > 180f):
                    Shot("rts_05_end");
                    Debug.Log($"[rts] state {Runner.Battle?.State}");
                    _quitAt = Time.unscaledTime + 1.5f;
                    _step++;
                    break;
            }
        }

        // -grLineup: every faction's five hulls in rows, held still, shot from a few angles, to
        // check the Blender models in the engine.
        int _lineup = -1;
        float _lineupAt;
        static readonly int[] LineupFactions = { 0, 1, 5, 6 };   // Continuance, Remnant, Serac, Ilium
        static readonly (float Yaw, float Pitch, float Distance, Vector3 At)[] LineupViews =
        {
            (210f, 24f, 2600f, new Vector3(0f, 0f, 0f)),
            (150f, 12f, 1300f, new Vector3(-700f, 0f, 0f)),
            (150f, 12f, 1300f, new Vector3(700f, 0f, 0f)),
            (300f, 35f, 900f, new Vector3(-1050f, 0f, 0f)),
            (300f, 35f, 900f, new Vector3(-350f, 0f, 0f)),
            (300f, 35f, 900f, new Vector3(350f, 0f, 0f)),
            (300f, 35f, 900f, new Vector3(1050f, 0f, 0f)),
        };

        void Lineup()
        {
            if (Time.unscaledTime < _lineupAt) return;
            if (_lineup < 0)
            {
                var w = new World(1);
                var classes = new[] { Hulls.Battleship, Hulls.Destroyer, Hulls.Transport, Hulls.Frigate, Hulls.Corvette };
                for (int f = 0; f < LineupFactions.Length; f++)
                {
                    float x = (f - 1.5f) * 700f;
                    float z = 900f;
                    foreach (var h in classes)
                    {
                        var ship = w.Spawn(h, f % 2, new System.Numerics.Vector3(x, 0f, z), 0f);
                        ship.Faction = LineupFactions[f];
                        ship.Speed = 0f;
                        z -= h.HalfLength * 2f + 110f;
                    }
                }
                var st = w.Spawn(Hulls.Station(3), 0, new System.Numerics.Vector3(0f, 0f, -1400f), 0f);
                st.Speed = 0f;
                Runner.Load(w);
                Runner.Paused = true;
                _lineup = 0;
                _phase = 0;
                _lineupAt = Time.unscaledTime + 1f;
                return;
            }
            if (_lineup >= LineupViews.Length)
            {
                Application.Quit();
                return;
            }
            var v = LineupViews[_lineup];
            if (_phase == 0)
            {
                Rig.Frame(v.At, v.Yaw, v.Pitch, v.Distance);
                _phase = 1;
                _lineupAt = Time.unscaledTime + 1.2f;
                return;
            }
            ScreenCapture.CaptureScreenshot(Path.Combine(_dir, $"lineup_{_lineup}.png"));
            Debug.Log($"[rts] lineup {_lineup}");
            _lineup++;
            _phase = 0;
            _lineupAt = Time.unscaledTime + 0.3f;
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
                if (s.Side == 0 && s.Def.Class != HullClass.Station && n++ == c.Ship) id = s.Id;
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
