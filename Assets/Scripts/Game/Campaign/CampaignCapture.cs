using System.IO;
using GalacticRupture.Sim;
using GalacticRupture.Sim.Strategic;
using UnityEngine;

namespace GalacticRupture.Game
{
    // `-grCaptureCampaign <dir>`: plays the opening of the campaign the way a player would
    // (select the home fleet, send it forward, take command of the first battle, return to the
    // map), screenshots each stage with UI, and quits.
    public sealed class CampaignCapture : MonoBehaviour
    {
        public CampaignDirector Director;
        public CampaignHud Hud;
        public MapView Map;
        public CommandInput Input;
        public SimRunner Runner;

        public static bool Active { get; private set; }

        string _dir;
        int _step;
        float _wait;
        float _quitAt = -1f;

        void Awake()
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-grCaptureCampaign") _dir = args[i + 1];
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
            Debug.Log($"[campaign] {name}: {Director.Date}, pending {Director.Campaign.Pending != null}, battle {Director.InBattle}");
        }

        void Update()
        {
            if (_quitAt > 0f)
            {
                if (Time.unscaledTime > _quitAt) Application.Quit();
                return;
            }
            _wait -= Time.unscaledDeltaTime;
            if (_wait > 0f) return;
            var c = Director.Campaign;
            if (c == null) return;

            switch (_step)
            {
                case 0:
                    _wait = 2f;
                    _step++;
                    break;
                case 1:
                {
                    var home = c.Fleets.Find(f => f.Name == "Home Fleet");
                    Map.SelectedFleet = home.Id;
                    var aurel = c.Galaxy.Systems.Find(s => s.Name == "Aurel");
                    Map.SelectedSystem = -1;
                    Shot("camp_01_map");
                    _wait = 1f;
                    _step++;
                    break;
                }
                case 2:
                {
                    // Send everything forward: home fleet to Oriel, the screen to Morrow's Gate.
                    var home = c.Fleets.Find(f => f.Name == "Home Fleet");
                    var screen = c.Fleets.Find(f => f.Name == "Tessary Screen");
                    c.MoveFleet(home.Id, c.Galaxy.Systems.Find(s => s.Name == "Oriel").Id);
                    c.MoveFleet(screen.Id, c.Galaxy.Systems.Find(s => s.Name == "Morrow's Gate").Id);
                    c.QueueBuild(Campaign.PlayerFaction, c.Galaxy.Systems.Find(s => s.Name == "Aurel").Id, Hulls.Destroyer);
                    Map.SelectedSystem = c.Galaxy.Systems.Find(s => s.Name == "Aurel").Id;
                    Director.Speed = 4f;
                    _wait = 4f;
                    _step++;
                    break;
                }
                case 3:
                    Shot("camp_02_moving");
                    _step++;
                    break;
                case 4:
                    if (c.Pending == null && c.Hours < 24 * 30) return;
                    Director.Speed = 1f;
                    _wait = 1f;
                    _step++;
                    break;
                case 5:
                    Shot("camp_03_prompt");
                    if (c.Pending == null) _quitAt = Time.unscaledTime + 1f;
                    _wait = 0.5f;
                    _step++;
                    break;
                case 6:
                    Director.Attend();
                    _wait = 1f;
                    _step = 60;
                    break;
                case 60:
                    Input.SelectAll();
                    foreach (var s in Runner.World.Ships)
                        if (s.Alive && s.Side != 0)
                        {
                            Input.OrderAttack(s.Id);
                            break;
                        }
                    _wait = 14f;
                    _step = 7;
                    break;
                case 7:
                    Shot("camp_04_battle");
                    Runner.TimeScale = 4f;
                    _step++;
                    break;
                case 8:
                    if (Runner.State == BattleState.Fighting && Runner.World.Time < 240f)
                    {
                        // Keep the fleet on the nearest enemy, as a player would.
                        foreach (int id in Input.Selected)
                        {
                            var ship = Runner.World.ShipById(id);
                            if (ship != null && ship.Order.Kind != OrderKind.Attack)
                                foreach (var s in Runner.World.Ships)
                                    if (s.Alive && s.Side != 0) { Input.OrderAttack(s.Id); break; }
                        }
                        _wait = 1f;
                        return;
                    }
                    if (Runner.State == BattleState.Fighting) Director.Withdraw();
                    _wait = 1.5f;
                    _step++;
                    break;
                case 9:
                    Shot("camp_05_battle_end");
                    Director.ReturnToMap();
                    _wait = 2f;
                    _step++;
                    break;
                case 10:
                    Shot("camp_06_after");
                    _quitAt = Time.unscaledTime + 1f;
                    _step++;
                    break;
            }
        }
    }
}
