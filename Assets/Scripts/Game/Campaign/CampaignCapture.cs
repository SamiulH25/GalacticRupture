using System.IO;
using System.Linq;
using GalacticRupture.Sim;
using GalacticRupture.Sim.Strategic;
using UnityEngine;

namespace GalacticRupture.Game
{
    // `-grCaptureCampaign <dir>`: plays the opening of the long campaign the way a player would:
    // looks over the galaxy and home, appoints a governor, drafts a ship design, sends an
    // expedition to reclaim the nearest ruin and the home fleet with an army to the nearest
    // holdout, fights the first battle in command, saves and reloads, then lets the months run.
    // Screenshots each stage with UI, and quits.
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

        Fleet Named(string n) => Director.Campaign.Fleets.Find(f => f.Name == n && f.Owner == Campaign.PlayerFaction);
        Fleet Home => Named("Home Fleet");
        int _ruin = -1, _holdout = -1;
        bool _shotEnvoy;

        int Capital => Director.Campaign.Factions[Campaign.PlayerFaction].Capital;

        int Nearest(System.Func<StarSystem, bool> want)
        {
            var c = Director.Campaign;
            int best = -1;
            float bestD = float.MaxValue;
            foreach (var s in c.Galaxy.Systems)
            {
                if (!want(s)) continue;
                float d = c.Galaxy.Distance(Capital, s.Id);
                if (d < bestD) { bestD = d; best = s.Id; }
            }
            return best;
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
                // Envoys no longer stop the clock: photograph the first, then answer.
                if (!_shotEnvoy) { Shot("envoy"); _shotEnvoy = true; _wait = 0.3f; return; }
                c.Respond(c.Incoming[0], true);
                Director.Flush();
            }

            switch (_step)
            {
                case 0:
                    Director.Paused = true;
                    Director.MapCamera.Snap(MapView.ToMap(c.Galaxy.Systems[c.OldCapital].Position));
                    Director.MapCamera.Distance = 26000f;
                    Director.MapCamera.Pitch = 70f;
                    _wait = 2.5f;
                    _step++;
                    break;
                case 1:
                    Shot("galaxy");
                    _wait = 0.3f;
                    _step++;
                    break;
                case 2:
                    Director.MapCamera.Snap(MapView.ToMap(c.Galaxy.Systems[Capital].Position));
                    Director.MapCamera.Distance = 7000f;
                    Director.MapCamera.Pitch = 58f;
                    Map.SelectedSystem = Capital;
                    _wait = 2f;
                    _step++;
                    break;
                case 3:
                    Shot("home");
                    _wait = 0.3f;
                    _step++;
                    break;
                case 4:
                {
                    // A governor for the largest core world that isn't the capital.
                    var core = c.Galaxy.Systems.Where(s => s.Owner == Campaign.PlayerFaction && !s.Capital).OrderByDescending(s => s.Population).First();
                    c.Factions[Campaign.PlayerFaction].Influence += 20f;
                    c.Appoint(Campaign.PlayerFaction, core.Id, 0, GovernorFocus.Industry);
                    Director.Flush();
                    Map.SelectedSystem = core.Id;
                    _wait = 0.8f;
                    _step++;
                    break;
                }
                case 5:
                    Shot("governor");
                    _wait = 0.3f;
                    _step++;
                    break;
                case 6:
                {
                    // Draft an ion-armed destroyer.
                    var designs = c.Factions[Campaign.PlayerFaction].Designs;
                    var d = designs.First(x => x.Hull.Class == HullClass.Destroyer).Copy("Ion Destroyer");
                    d.Set(0, Hulls.IonBattery);
                    d.Set(1, Hulls.IonBattery);
                    designs.Add(d);
                    Hud.ShowDesigner = true;
                    _wait = 0.8f;
                    _step++;
                    break;
                }
                case 7:
                    Shot("designer");
                    _wait = 0.3f;
                    _step++;
                    break;
                case 8:
                {
                    Hud.ShowDesigner = false;
                    _ruin = Nearest(s => s.Owner < 0 && s.Derelict == Derelict.Ruin);
                    _holdout = Nearest(s => s.Owner < 0 && s.Derelict == Derelict.Holdout && !s.OldCapital);
                    var exp = Named("First Expedition");
                    c.MoveFleet(exp.Id, _ruin);
                    var home = Home;
                    var army = c.ArmiesAt(home.System).First(a => a.Owner == Campaign.PlayerFaction);
                    c.Embark(home.Id, army.Id);
                    c.MoveFleet(home.Id, _holdout);
                    c.QueueBuild(Campaign.PlayerFaction, Capital, Hulls.Transport);
                    Map.SelectedFleet = home.Id;
                    Map.SelectedSystem = -1;
                    Director.Paused = false;
                    Director.Speed = 4f;
                    _wait = 2f;
                    _step++;
                    break;
                }
                case 9:
                    Shot("fleets_underway");
                    Director.Speed = 16f;
                    _step++;
                    break;
                case 10:
                {
                    var exp = Named("First Expedition");
                    if (exp == null) { _step = 12; break; }
                    if (exp.InTransit && c.Hours < 24 * 60) return;
                    if (c.StartReclaim(Campaign.PlayerFaction, _ruin)) Director.Flush();
                    Map.SelectedSystem = _ruin;
                    Map.SelectedFleet = -1;
                    Director.MapCamera.Snap(MapView.ToMap(c.Galaxy.Systems[_ruin].Position));
                    Director.Speed = 1f;
                    _wait = 2f;
                    _step++;
                    break;
                }
                case 11:
                    Shot("reclaim");
                    Director.Speed = 16f;
                    _step++;
                    break;
                case 12:
                {
                    var s = c.Galaxy.Systems[_holdout];
                    if (s.Besieger != Campaign.PlayerFaction && s.Owner != Campaign.PlayerFaction && c.Hours < 24 * 90) return;
                    Map.SelectedSystem = _holdout;
                    Director.MapCamera.Snap(MapView.ToMap(s.Position));
                    Director.Speed = 1f;
                    _wait = 2f;
                    _step++;
                    break;
                }
                case 13:
                    Shot("landing");
                    _wait = 0.3f;
                    _step = 30;
                    break;
                case 30:
                    Hud.ShowDiplomacy = true;
                    Director.Paused = true;
                    _wait = 0.8f;
                    _step = 14;
                    break;
                case 14:
                    Shot("diplomacy");
                    _wait = 0.3f;
                    _step = 31;
                    break;
                case 31:
                    Hud.ShowDiplomacy = false;
                    Map.ShowSupply = true;
                    Director.MapCamera.Snap(MapView.ToMap(c.Galaxy.Systems[Capital].Position));
                    Director.MapCamera.Distance = 14000f;
                    _wait = 1f;
                    _step = 15;
                    break;
                case 15:
                    Shot("supply");
                    _wait = 0.3f;
                    _step = 32;
                    break;
                case 32:
                    Map.ShowSupply = false;
                    Debug.Log($"[campaign] save {(Director.TrySave(CampaignDirector.QuickSave) ? "ok" : "failed")} at {Director.Date}");
                    Debug.Log($"[campaign] load {(Director.TryLoad(CampaignDirector.QuickSave) ? "ok" : "failed")} at {Director.Date}");
                    Director.Paused = false;
                    Director.Speed = 16f;
                    _wait = 40f;
                    _step = 16;
                    break;
                case 16:
                {
                    Director.Paused = true;
                    var me = c.Factions[Campaign.PlayerFaction];
                    int held = c.Galaxy.Systems.Count(s => s.Owner == Campaign.PlayerFaction);
                    Debug.Log($"[campaign] {Director.Date}: act {c.Act}, we hold {held}, {me.Credits:0} cr, derelicts left {c.DerelictsLeft}");
                    Map.SelectedSystem = _ruin;
                    _wait = 1f;
                    _step++;
                    break;
                }
                case 17:
                    Shot("later");
                    _quitAt = Time.unscaledTime + 1f;
                    _step++;
                    break;
            }
        }
    }
}
