using System.Collections.Generic;
using GalacticRupture.Sim;
using GalacticRupture.Sim.Strategic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GalacticRupture.Game
{
    // Runs the campaign: strategic time on the galaxy map, and the hand-off into and out of
    // tactical battles. When fleets meet, time stops and the player chooses to take command
    // or leave it to the fleet; both run the same battle sim (IDEAS.md §3).
    public sealed class CampaignDirector : MonoBehaviour
    {
        public SimRunner Runner;
        public CameraRig TacticalCamera;
        public MapCamera MapCamera;
        public MapView Map;
        public ulong Seed = 20260929;
        public float HoursPerSecond = 3f;

        public Campaign Campaign { get; private set; }
        public bool InBattle { get; private set; }
        public bool Paused;
        public float Speed = 1f;
        public readonly List<string> Log = new List<string>();

        public static bool Active { get; private set; }

        Engagement _engagement;
        CampaignBattle _battle;
        float _hours;

        static readonly float[] Speeds = { 0.5f, 1f, 2f, 4f, 8f };

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
            Campaign = Maps.Rupture(Seed);
            Map.Build(Campaign);
            var capital = Campaign.Galaxy.Systems.Find(s => s.Capital && s.Owner == Campaign.PlayerFaction);
            MapCamera.Snap(MapView.ToMap(capital.Position) + new Vector3(1500f, 0f, 0f));
            Say($"Day 1. The Continuance holds {capital.Name}. The Remnant holds Veyl.");
            EnterMap();
        }

        public string Date
        {
            get
            {
                int h = (int)Campaign.Hours;
                return $"Day {h / 24 + 1}, {h % 24:00}:00";
            }
        }

        public void Say(string line)
        {
            Log.Insert(0, line);
            if (Log.Count > 40) Log.RemoveAt(Log.Count - 1);
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
            if (kb != null && Campaign.Pending == null)
            {
                if (kb.spaceKey.wasPressedThisFrame) Paused = !Paused;
                if (kb.equalsKey.wasPressedThisFrame || kb.numpadPlusKey.wasPressedThisFrame) StepSpeed(1);
                if (kb.minusKey.wasPressedThisFrame || kb.numpadMinusKey.wasPressedThisFrame) StepSpeed(-1);
            }

            if (!Paused && Campaign.Pending == null && Campaign.State == CampaignState.Ongoing)
            {
                _hours += Time.deltaTime * HoursPerSecond * Speed;
                while (_hours >= 1f && Campaign.Pending == null)
                {
                    _hours -= 1f;
                    Campaign.Tick();
                    ReadEvents();
                }
                if (Campaign.Pending != null) _hours = 0f;
            }
            Map.Refresh(MapCamera.GetComponent<Camera>(), Time.deltaTime);
        }

        void StepSpeed(int dir)
        {
            int i = System.Array.IndexOf(Speeds, Speed);
            if (i < 0) i = 1;
            Speed = Speeds[Mathf.Clamp(i + dir, 0, Speeds.Length - 1)];
        }

        string SysName(int id) => id >= 0 && id < Campaign.Galaxy.Systems.Count ? Campaign.Galaxy.Systems[id].Name : "?";

        void ReadEvents()
        {
            foreach (var e in Campaign.Events)
            {
                bool ours = e.Faction == Campaign.PlayerFaction;
                switch (e.Kind)
                {
                    case CampaignEventKind.FleetArrived when ours:
                        Say($"{Date}: {Campaign.FleetById(e.Fleet)?.Name} arrived at {SysName(e.System)}.");
                        break;
                    case CampaignEventKind.ShipBuilt when ours:
                        Say($"{Date}: {e.Text} commissioned at {SysName(e.System)}.");
                        break;
                    case CampaignEventKind.SiegeStarted:
                        Say(ours ? $"{Date}: our ships are taking {SysName(e.System)}."
                                 : $"{Date}: the Remnant is moving on {SysName(e.System)}.");
                        break;
                    case CampaignEventKind.SystemCaptured:
                        Say(ours ? $"{Date}: {SysName(e.System)} is ours." : $"{Date}: {SysName(e.System)} has fallen to the Remnant.");
                        break;
                    case CampaignEventKind.BattleStarted:
                        Say($"{Date}: contact at {SysName(e.System)}.");
                        break;
                }
            }
            Campaign.Events.Clear();
            if (Campaign.State == CampaignState.Victory) Say("Veyl has fallen. The war is won.");
            if (Campaign.State == CampaignState.Defeat) Say("Aurel has fallen. The war is lost.");
        }

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
            Say($"{Date}: taking command at {SysName(_engagement.System)}.");
        }

        public void LeaveToFleet()
        {
            var e = Campaign.Pending;
            if (e == null) return;
            var result = BattleSetup.AutoResolve(Campaign);
            Report(e, result);
            ReadEvents();
        }

        // Called from the tactical HUD once the battle is over (or the player withdraws).
        public void ReturnToMap()
        {
            if (!InBattle) return;
            BattleSetup.Apply(Campaign, _engagement, Runner.World, _battle);
            Report(_engagement, _battle.State);
            ReadEvents();
            _battle = null;
            _engagement = null;
            EnterMap();
        }

        public void Withdraw()
        {
            _battle?.Withdraw(Campaign.PlayerFaction);
        }

        void Report(Engagement e, BattleState state)
        {
            string where = SysName(e.System);
            Say(state switch
            {
                BattleState.Victory => $"{Date}: victory at {where}.",
                BattleState.Defeat => $"{Date}: our ships at {where} were lost.",
                _ => $"{Date}: the battle at {where} broke off; {(e.Attacker == Campaign.PlayerFaction || _battle?.Withdrawing == 0 ? "we fell back" : "the Remnant fell back")}.",
            });
        }

        public float Forecast(int faction)
        {
            var e = Campaign.Pending;
            float s = 0f;
            if (e == null) return s;
            foreach (int id in e.Fleets)
            {
                var f = Campaign.FleetById(id);
                if (f != null && f.Owner == faction) s += f.Strength;
            }
            return s;
        }
    }
}
