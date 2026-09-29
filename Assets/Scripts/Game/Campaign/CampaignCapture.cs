using System.IO;
using System.Linq;
using GalacticRupture.Sim;
using GalacticRupture.Sim.Strategic;
using UnityEngine;

namespace GalacticRupture.Game
{
    // `-grCaptureCampaign <dir>`: plays the opening the way a player would: embark an army, send
    // the home fleet to Morrow's Gate, fight what it meets, land and take the system; accepts
    // offers and abstains in council; screenshots each stage with UI, and quits.
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
        bool _foughtOne;
        bool _shotModal;
        int _shots;

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
            ScreenCapture.CaptureScreenshot(Path.Combine(_dir, $"camp_{++_shots:00}_{name}.png"));
            Debug.Log($"[campaign] {name}: {Director.Date}");
        }

        Fleet Home => Director.Campaign.Fleets.Find(f => f.Name == "Home Fleet");
        StarSystem Sys(string n) => Director.Campaign.Galaxy.Systems.Find(s => s.Name == n);

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

            // Battles in the tactical view: keep the fleet on the nearest enemy, then go back.
            if (Director.InBattle)
            {
                if (Runner.State == BattleState.Fighting && Runner.World.Time < 200f)
                {
                    if (Input.Selected.Count == 0) Input.SelectAll();
                    foreach (var s in Runner.World.Ships)
                        if (s.Alive && s.Side != 0) { Input.OrderAttack(s.Id); break; }
                    Runner.TimeScale = 4f;
                    _wait = 1f;
                    if (Runner.World.Time > 12f && !_foughtOne) { Shot("battle"); _foughtOne = true; }
                    return;
                }
                if (Runner.State == BattleState.Fighting) Director.Withdraw();
                _wait = 1f;
                Director.ReturnToMap();
                return;
            }

            // Screenshots land at the end of the frame, so each is taken a frame before acting.
            if (c.Pending != null)
            {
                if (!_shotModal) { Shot("contact"); _shotModal = true; _wait = 0.3f; return; }
                _shotModal = false;
                if (!_foughtOne) Director.Attend(); else Director.LeaveToFleet();
                _wait = 1f;
                return;
            }
            if (c.Council != null)
            {
                if (!_shotModal) { Shot("council"); _shotModal = true; _wait = 0.3f; return; }
                _shotModal = false;
                c.CastPlayerVote(Vote.Abstain);
                Director.Flush();
                _wait = 0.5f;
                return;
            }
            if (c.Incoming.Count > 0)
            {
                if (!_shotModal) { Shot("envoy"); _shotModal = true; _wait = 0.3f; return; }
                _shotModal = false;
                c.Respond(c.Incoming[0], true);
                Director.Flush();
                _wait = 0.5f;
                return;
            }

            switch (_step)
            {
                case 0:
                    _wait = 2f;
                    _step++;
                    break;
                case 1:
                    Map.SelectedSystem = Sys("Aurel").Id;
                    Shot("map");
                    _wait = 0.5f;
                    _step = 20;
                    break;
                case 20:
                    Map.SelectedSystem = -1;
                    Director.MapCamera.Snap(MapView.ToMap(Sys("Aurel").Position));
                    Director.MapCamera.Distance = 1500f;
                    Director.MapCamera.Pitch = 38f;
                    _wait = 2.5f;
                    _step = 21;
                    break;
                case 21:
                    Shot("closeup");
                    _wait = 0.3f;
                    _step = 22;
                    break;
                case 22:
                    Director.MapCamera.Distance = 6500f;
                    Director.MapCamera.Pitch = 58f;
                    _wait = 1.5f;
                    _step = 2;
                    break;
                case 2:
                {
                    var home = Home;
                    var army = c.ArmiesAt(home.System).First(a => a.Owner == Campaign.PlayerFaction);
                    c.Embark(home.Id, army.Id);
                    c.RaiseArmy(Campaign.PlayerFaction, Sys("Aurel").Id);
                    c.QueueBuild(Campaign.PlayerFaction, Sys("Aurel").Id, Hulls.Transport);
                    Map.SelectedFleet = home.Id;
                    Map.SelectedSystem = -1;
                    c.MoveFleet(home.Id, Sys("Morrow's Gate").Id);
                    Director.Speed = 4f;
                    _wait = 3f;
                    _step++;
                    break;
                }
                case 3:
                    Shot("fleet_underway");
                    _step++;
                    break;
                case 4:
                {
                    var home = Home;
                    if (home == null) { _step = 7; break; }
                    if (home.InTransit && c.Hours < 24 * 20) return;
                    if (c.CanLand(home))
                    {
                        c.Land(home.Id);
                        Director.Flush();
                        Map.SelectedSystem = home.System;
                        Map.SelectedFleet = -1;
                        Director.Speed = 1f;
                        _wait = 4f;
                        _step++;
                    }
                    else _step = 6;
                    break;
                }
                case 5:
                    Shot("landing");
                    Director.Speed = 4f;
                    _wait = 6f;
                    _step++;
                    break;
                case 6:
                    Hud.ShowDiplomacy = true;
                    Director.Paused = true;
                    _wait = 0.8f;
                    _step++;
                    break;
                case 7:
                    Shot("diplomacy");
                    _wait = 0.3f;
                    _step++;
                    break;
                case 8:
                    Hud.ShowDiplomacy = false;
                    Map.ShowSupply = true;
                    _wait = 0.8f;
                    _step++;
                    break;
                case 9:
                    Shot("supply");
                    _wait = 0.3f;
                    _step++;
                    break;
                case 10:
                    Map.ShowSupply = false;
                    Director.Paused = false;
                    Director.Speed = 8f;
                    _wait = 25f;
                    _step++;
                    break;
                case 11:
                    Shot("later");
                    _quitAt = Time.unscaledTime + 1f;
                    _step++;
                    break;
            }
        }
    }
}
