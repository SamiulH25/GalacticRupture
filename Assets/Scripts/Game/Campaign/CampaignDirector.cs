using System.Collections.Generic;
using GalacticRupture.Sim;
using GalacticRupture.Sim.Strategic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GalacticRupture.Game
{
    // Runs the campaign: strategic time on the galaxy map, and the hand-off into and out of
    // tactical battles. When the player's fleets meet a hostile force, time stops and the player
    // chooses to take command or leave it to the fleet; both run the same battle sim (IDEAS.md §3).
    // Council sessions also wait for the player; offers from other powers do not (IDEAS.md §1.1).
    // A day takes about twenty seconds at normal speed (§1.2). F5 saves, F9 loads.
    public sealed class CampaignDirector : MonoBehaviour
    {
        public SimRunner Runner;
        public CameraRig TacticalCamera;
        public MapCamera MapCamera;
        public MapView Map;
        public ulong Seed = 20260929;
        public float HoursPerSecond = 1.2f;
        public const float AutosaveDays = 30f;

        public Campaign Campaign { get; private set; }
        public bool InBattle { get; private set; }
        public bool Paused;
        public float Speed = 1f;
        public readonly List<string> Log = new List<string>();

        public static bool Active { get; private set; }

        Engagement _engagement;
        CampaignBattle _battle;
        float _hours;

        static readonly float[] Speeds = { 0.5f, 1f, 2f, 4f, 8f, 16f };
        int _lastAutosaveDay;

        public static string SaveDir => System.IO.Path.Combine(Application.persistentDataPath, "saves");
        public static string QuickSave => System.IO.Path.Combine(SaveDir, "quicksave.grsave");
        public static string AutoSave => System.IO.Path.Combine(SaveDir, "autosave.grsave");

        void Start()
        {
            Active = LaunchMode.Current == Launch.Campaign;
            if (!Active)
            {
                Map.gameObject.SetActive(false);
                MapCamera.enabled = false;
                enabled = false;
                return;
            }
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-grSeed" && ulong.TryParse(args[i + 1], out ulong seed)) Seed = seed;
            bool load = System.Array.IndexOf(args, "-grContinue") >= 0 && System.IO.File.Exists(QuickSave);
            if (load && TryLoad(QuickSave)) return;
            Campaign = GalaxyGen.Generate(Seed);
            Opened();
            var capital = Campaign.Galaxy.Systems[Campaign.Factions[Campaign.PlayerFaction].Capital];
            var throne = Campaign.Galaxy.Systems[Campaign.OldCapital];
            Say($"Day 1. The old empire is broken. The Continuance holds {capital.Name} and three worlds around it.");
            Say($"Ruins lie in every direction: send a fleet to one and reclaim it. The old capital, {throne.Name}, waits at the centre.");
        }

        // A campaign has been made or loaded: build the map and look at home.
        void Opened()
        {
            Map.Build(Campaign);
            var capital = Campaign.Galaxy.Systems[Campaign.Factions[Campaign.PlayerFaction].Capital];
            MapCamera.Snap(MapView.ToMap(capital.Position));
            _lastAutosaveDay = Campaign.Day;
            EnterMap();
        }

        public bool TrySave(string path)
        {
            if (Campaign == null || !Campaign.CanSave) return false;
            try
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
                string tmp = path + ".tmp";
                using (var fs = System.IO.File.Create(tmp)) Campaign.Save(fs);
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                System.IO.File.Move(tmp, path);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[save] {e.Message}");
                return false;
            }
        }

        public bool TryLoad(string path)
        {
            try
            {
                using var fs = System.IO.File.OpenRead(path);
                Campaign = Campaign.Load(fs);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[load] {e.Message}");
                Say($"Could not load the save: {e.Message}");
                return false;
            }
            Log.Clear();
            Map.SelectedFleet = Map.SelectedSystem = -1;
            Opened();
            Say($"{Date}: campaign loaded.");
            return true;
        }

        public string Date
        {
            get
            {
                int h = (int)Campaign.Hours;
                return $"Day {h / 24 + 1}, {h % 24:00}:00";
            }
        }

        // Only battles and council votes stop the clock (IDEAS.md §1.1).
        public bool Waiting => Campaign.Pending != null || Campaign.Council != null;

        public void Say(string line)
        {
            Log.Insert(0, line);
            if (Log.Count > 60) Log.RemoveAt(Log.Count - 1);
        }

        void EnterMap()
        {
            InBattle = false;
            Runner.Unload();
            TacticalCamera.enabled = false;
            MapCamera.enabled = true;
            Map.gameObject.SetActive(true);
        }

        void Update()
        {
            if (InBattle)
            {
                Map.gameObject.SetActive(false);
                return;
            }
            var kb = Keyboard.current;
            if (kb != null && !Waiting)
            {
                if (kb.spaceKey.wasPressedThisFrame) Paused = !Paused;
                if (kb.equalsKey.wasPressedThisFrame || kb.numpadPlusKey.wasPressedThisFrame) StepSpeed(1);
                if (kb.minusKey.wasPressedThisFrame || kb.numpadMinusKey.wasPressedThisFrame) StepSpeed(-1);
                if (kb.f5Key.wasPressedThisFrame) Say(TrySave(QuickSave) ? $"{Date}: saved." : "Could not save.");
                if (kb.f9Key.wasPressedThisFrame && System.IO.File.Exists(QuickSave)) TryLoad(QuickSave);
            }

            if (!Paused && !Waiting && Campaign.State == CampaignState.Ongoing)
            {
                _hours += Time.deltaTime * HoursPerSecond * Speed;
                while (_hours >= 1f && !Waiting)
                {
                    _hours -= 1f;
                    Campaign.Tick();
                    ReadEvents();
                }
                if (Waiting) _hours = 0f;
                if (Campaign.Day >= _lastAutosaveDay + AutosaveDays && Campaign.CanSave)
                {
                    _lastAutosaveDay = Campaign.Day;
                    TrySave(AutoSave);
                }
            }
            Map.Refresh(MapCamera.GetComponent<Camera>(), Time.deltaTime);
        }

        void StepSpeed(int dir)
        {
            int i = System.Array.IndexOf(Speeds, Speed);
            if (i < 0) i = 1;
            Speed = Speeds[Mathf.Clamp(i + dir, 0, Speeds.Length - 1)];
        }

        public string SysName(int id) => id >= 0 && id < Campaign.Galaxy.Systems.Count ? Campaign.Galaxy.Systems[id].Name : "?";
        public string FacName(int id) => id >= 0 && id < Campaign.Factions.Count ? Campaign.Factions[id].Name : "no one";
        public string Tag(int faction) => $"<color=#{FactionStyle.Hex(faction)}>{FacName(faction)}</color>";

        public string Where(Engagement e) => e == null ? "?" : e.InLane
            ? $"the lane between {SysName(e.LaneA)} and {SysName(e.LaneB)}"
            : SysName(e.System);

        void ReadEvents()
        {
            var intel = Campaign.Intel[Campaign.PlayerFaction];
            foreach (var e in Campaign.Events)
            {
                bool ours = e.Faction == Campaign.PlayerFaction;
                bool seen = e.System < 0 || e.System >= intel.SystemVisible.Length || intel.SystemVisible[e.System];
                switch (e.Kind)
                {
                    case CampaignEventKind.FleetArrived when ours:
                        Say($"{Date}: {Campaign.FleetById(e.Fleet)?.Name} arrived at {SysName(e.System)}.");
                        break;
                    case CampaignEventKind.ShipBuilt when ours:
                        Say($"{Date}: {e.Text} commissioned at {SysName(e.System)}.");
                        break;
                    case CampaignEventKind.ArmyRaised when ours:
                        Say($"{Date}: the {e.Text} is raised at {SysName(e.System)}.");
                        break;
                    case CampaignEventKind.LandingStarted when ours || seen:
                        Say(ours ? $"{Date}: our armies are down on {SysName(e.System)}."
                                 : $"{Date}: {Tag(e.Faction)} armies have landed on {SysName(e.System)}.");
                        break;
                    case CampaignEventKind.LandingFailed when ours || seen:
                        Say($"{Date}: the landing on {SysName(e.System)} has been thrown back.");
                        break;
                    case CampaignEventKind.SystemCaptured when ours || e.Other == Campaign.PlayerFaction || seen:
                        Say(ours ? $"{Date}: {SysName(e.System)} is ours."
                            : $"{Date}: {SysName(e.System)} has fallen to the {Tag(e.Faction)}.");
                        break;
                    case CampaignEventKind.BattleStarted:
                        Say($"{Date}: contact at {Where(Campaign.Pending)}.");
                        break;
                    case CampaignEventKind.Interception when e.Faction == Campaign.PlayerFaction || e.Other == Campaign.PlayerFaction || seen:
                        Say($"{Date}: fleets meet in the lane toward {e.Text}.");
                        break;
                    case CampaignEventKind.WarDeclared:
                        Say($"{Date}: the {Tag(e.Faction)} declares war on the {Tag(e.Other)}.");
                        break;
                    case CampaignEventKind.TreatySigned:
                        Say($"{Date}: the {Tag(e.Faction)} and the {Tag(e.Other)} sign {Treaty(e.Text)}.");
                        break;
                    case CampaignEventKind.TreatyBroken:
                        Say($"{Date}: the {Tag(e.Faction)} breaks its treaty with the {Tag(e.Other)}. Every court takes note.");
                        break;
                    case CampaignEventKind.CouncilSession:
                        Say($"{Date}: the council sits. On the floor: {e.Text}.");
                        break;
                    case CampaignEventKind.CouncilResult:
                        var r = Campaign.LastCouncil;
                        Say($"{Date}: the council {(r.Passed ? "passes" : "rejects")} the motion, {r.YesSeats} seats to {r.NoSeats}.");
                        break;
                    case CampaignEventKind.FactionEliminated:
                        Say($"{Date}: the {Tag(e.Faction)} is gone from the map.");
                        break;
                    case CampaignEventKind.BuildingCompleted when ours:
                        Say($"{Date}: {Building(e.Text)} completed at {SysName(e.System)}.");
                        break;
                    case CampaignEventKind.GovernorAppointed when ours:
                        Say($"{Date}: {e.Text} takes up the governorship of {SysName(e.System)}.");
                        break;
                    case CampaignEventKind.GovernorDefected when ours || seen:
                        Say(ours ? $"{Date}: <color=#ff8a70>{e.Text} has taken {SysName(e.System)} out of the war.</color>"
                                 : $"{Date}: the governor of {SysName(e.System)} breaks from the {Tag(e.Faction)}.");
                        break;
                    case CampaignEventKind.NotableOffered when ours:
                        Say($"{Date}: {e.Text} of {SysName(e.System)} offers to serve as a governor.");
                        break;
                    case CampaignEventKind.ReclamationStarted when ours:
                        Say($"{Date}: the expedition begins work on {SysName(e.System)}.");
                        break;
                    case CampaignEventKind.SystemReclaimed when ours || seen:
                        Say(ours ? $"{Date}: {SysName(e.System)} is reclaimed and ours."
                                 : $"{Date}: the {Tag(e.Faction)} has reclaimed {SysName(e.System)}.");
                        break;
                    case CampaignEventKind.CourtingStarted when ours:
                        Say($"{Date}: our envoys go down to {SysName(e.System)}.");
                        break;
                    case CampaignEventKind.HoldoutJoined when ours || seen:
                        Say(ours ? $"{Date}: {SysName(e.System)} joins us."
                                 : $"{Date}: {SysName(e.System)} throws in with the {Tag(e.Faction)}.");
                        break;
                    case CampaignEventKind.ProposalReceived:
                        Say($"{Date}: an envoy from the {Tag(e.Faction)} waits with an offer.");
                        break;
                    case CampaignEventKind.ActBegan:
                        Say(e.Text == "2"
                            ? $"{Date}: <b>The ruins are claimed. Blocs are forming, and the council is splitting.</b>"
                            : $"{Date}: <b>Total war. Whoever holds {SysName(Campaign.OldCapital)} for {Campaign.ThroneDays:0} days wins.</b>");
                        break;
                }
            }
            Campaign.Events.Clear();
            if (Campaign.State == CampaignState.Victory) Say("The war is won.");
            if (Campaign.State == CampaignState.Defeat) Say("The war is lost.");
        }

        static string Building(string kind) => kind switch
        {
            "Fab" => "a fab",
            "Yard" => "a yard",
            "Refinery" => "a refinery",
            "Garrison" => "a garrison",
            "Admin" => "an administration",
            "Habitat" => "a habitat",
            "Depot" => "a supply depot",
            _ => kind,
        };

        static string Treaty(string stance) => stance switch
        {
            "Alliance" => "an alliance",
            "NonAggression" => "a non-aggression pact",
            "Peace" => "a peace",
            _ => stance,
        };

        // Called when the player acts on something that changes the world outside the tick
        // (a vote, a treaty): read what it caused.
        public void Flush() => ReadEvents();

        // --- Battle hand-off -------------------------------------------------------------------

        public void Attend()
        {
            _engagement = Campaign.Pending;
            if (_engagement == null) return;
            var world = BattleSetup.Build(Campaign, _engagement, true, out _battle);
            InBattle = true;
            Map.gameObject.SetActive(false);
            MapCamera.enabled = false;
            Runner.Load(world);
            TacticalCamera.enabled = true;
            // From behind our line, looking across at theirs.
            TacticalCamera.Frame(new Vector3(0f, 0f, -900f), 0f, 30f, 5200f);
            Say($"{Date}: taking command at {Where(_engagement)}.");
        }

        public void LeaveToFleet()
        {
            var e = Campaign.Pending;
            if (e == null) return;
            var result = BattleSetup.AutoResolve(Campaign);
            Report(e, result, null);
            ReadEvents();
        }

        // Called from the tactical HUD once the battle is over (or the player withdraws).
        public void ReturnToMap()
        {
            if (!InBattle) return;
            BattleSetup.Apply(Campaign, _engagement, Runner.World, _battle);
            Report(_engagement, _battle.State, _battle);
            ReadEvents();
            _battle = null;
            _engagement = null;
            EnterMap();
        }

        public void Withdraw() => _battle?.Withdraw(0);

        void Report(Engagement e, BattleState state, CampaignBattle battle)
        {
            string where = Where(e);
            Say(state switch
            {
                BattleState.Victory => $"{Date}: victory at {where}.",
                BattleState.Defeat => $"{Date}: our ships at {where} were lost or driven off.",
                _ => $"{Date}: the battle at {where} broke off; {(battle?.Withdrawing == 0 || e.Attacker == Campaign.PlayerFaction ? "we fell back" : "they fell back")}.",
            });
        }

        public float Forecast(int side)
        {
            var e = Campaign.Pending;
            float s = 0f;
            if (e == null) return s;
            foreach (int id in e.Fleets)
            {
                var f = Campaign.FleetById(id);
                if (f != null && e.SideOf(f.Owner) == side) s += f.Strength;
            }
            return s;
        }
    }
}
