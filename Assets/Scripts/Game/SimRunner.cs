using GalacticRupture.Sim;
using GalacticRupture.Sim.Strategic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GalacticRupture.Game
{
    // Owns the tactical sim. Steps it at its fixed tick, feeds events to the presenter, and
    // hands the interpolation alpha to everything that draws. Idle (no World) on the galaxy map.
    [DefaultExecutionOrder(-100)]
    public sealed class SimRunner : MonoBehaviour
    {
        public GameArt Art;
        public ulong Seed = 20260929;
        public float TimeScale = 1f;
        public bool Paused;

        public World World { get; private set; }
        public TurretToy Toy { get; private set; }
        public Skirmish Battle { get; private set; }
        public CampaignBattle CampaignBattle { get; private set; }
        public FleetPresenter Presenter { get; private set; }
        public float Alpha { get; private set; }

        ProjectileRenderer _projectiles;
        Fx _fx;
        float _accumulator;

        // Wall-clock cost of the last frame's sim steps, for the benchmark.
        public double LastStepMs { get; private set; }
        readonly System.Diagnostics.Stopwatch _watch = new System.Diagnostics.Stopwatch();

        // The ship the camera starts on.
        public int Flagship => Toy != null ? Toy.Flagship
            : Battle != null ? Battle.Flagship
            : CampaignBattle != null ? CampaignBattle.Flagship
            : 0;

        // Ships waiting behind the command cap, if this battle has any.
        public Reinforcements Reinforcements => Battle != null ? Battle.Reinforcements
            : CampaignBattle != null ? CampaignBattle.Reinforcements
            : null;

        public TerrainSpec Terrain => Battle != null ? Battle.Terrain
            : CampaignBattle != null ? CampaignBattle.Terrain
            : default;

        public BattleState State => Battle != null ? Battle.State
            : CampaignBattle != null ? CampaignBattle.State
            : BattleState.Fighting;

        public static readonly float[] Speeds = { 0.5f, 1f, 2f, 4f };

        void Awake()
        {
            _fx = new GameObject("Fx").AddComponent<Fx>();
            _fx.transform.SetParent(transform, false);
            _fx.Init(Art);

            _projectiles = gameObject.AddComponent<ProjectileRenderer>();
            _projectiles.Init(Art);

            switch (LaunchMode.Current)
            {
                case Launch.Toy:
                    Load(TurretToy.Create(Seed));
                    break;
                case Launch.Skirmish:
                case Launch.RtsCapture:
                    Load(Skirmish.Create(Seed, System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-grArsenal") >= 0));
                    break;
            }
        }

        // Replaces the running battle, views and all.
        public void Load(World world)
        {
            Unload();
            Presenter = new GameObject("Fleet").AddComponent<FleetPresenter>();
            Presenter.transform.SetParent(transform, false);
            World = world;
            Toy = world.Director as TurretToy;
            Battle = world.Director as Skirmish;
            CampaignBattle = world.Director as CampaignBattle;
            Presenter.Init(Art, _fx, World);
            _accumulator = 0f;
            Paused = false;
            Dispatch();
        }

        public void Unload()
        {
            if (Presenter != null) Destroy(Presenter.gameObject);
            Presenter = null;
            World = null;
            Toy = null;
            Battle = null;
            CampaignBattle = null;
            _fx.Clear();
        }

        void Update()
        {
            if (World == null) return;
            HandleKeys();
            if (!Paused)
            {
                _accumulator += Time.deltaTime * TimeScale;
                int steps = 0;
                _watch.Restart();
                while (_accumulator >= World.Dt && steps < 12)
                {
                    World.Step();
                    Dispatch();
                    _accumulator -= World.Dt;
                    steps++;
                }
                if (steps == 12) _accumulator = 0f;
                LastStepMs = _watch.Elapsed.TotalMilliseconds;
            }
            Alpha = Mathf.Clamp01(_accumulator / World.Dt);
        }

        void LateUpdate()
        {
            if (World == null) return;
            Presenter.Sync(Alpha, Paused ? 0f : Time.deltaTime * TimeScale);
            _projectiles.Draw(World, Alpha);
        }

        void Dispatch()
        {
            var events = World.Events;
            for (int i = 0; i < events.Count; i++)
            {
                Presenter.Handle(events[i]);
                GameAudio.OnEvent(events[i], World);
            }
            events.Clear();
        }

        void HandleKeys()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.spaceKey.wasPressedThisFrame) Paused = !Paused;
            if (kb.equalsKey.wasPressedThisFrame || kb.numpadPlusKey.wasPressedThisFrame) StepSpeed(1);
            if (kb.minusKey.wasPressedThisFrame || kb.numpadMinusKey.wasPressedThisFrame) StepSpeed(-1);
            if (kb.ctrlKey.isPressed && kb.rKey.wasPressedThisFrame && CampaignBattle == null)
                Load(Toy != null ? TurretToy.Create(Seed) : Skirmish.Create(Seed + (ulong)Time.frameCount));
        }

        void StepSpeed(int dir)
        {
            int i = System.Array.IndexOf(Speeds, TimeScale);
            if (i < 0) i = 1;
            TimeScale = Speeds[Mathf.Clamp(i + dir, 0, Speeds.Length - 1)];
        }
    }
}
