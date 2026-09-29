using GalacticRupture.Sim;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GalacticRupture.Game
{
    // Owns the sim. Steps it at its fixed tick, feeds events to the presenter, and hands the
    // interpolation alpha to everything that draws.
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
        public FleetPresenter Presenter { get; private set; }
        public float Alpha { get; private set; }

        ProjectileRenderer _projectiles;
        Fx _fx;
        float _accumulator;

        // Wall-clock cost of the last frame's sim steps, for the benchmark.
        public double LastStepMs { get; private set; }
        readonly System.Diagnostics.Stopwatch _watch = new System.Diagnostics.Stopwatch();

        // The ship the camera starts on.
        public int Flagship => Toy != null ? Toy.Flagship : Battle != null ? Battle.Flagship : 0;

        static readonly float[] Speeds = { 0.25f, 1f, 2f, 4f };

        void Awake()
        {
            _fx = new GameObject("Fx").AddComponent<Fx>();
            _fx.transform.SetParent(transform, false);
            _fx.Init(Art);

            _projectiles = gameObject.AddComponent<ProjectileRenderer>();
            _projectiles.Init(Art);

            bool toy = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-grToy") >= 0 || CaptureMode.Requested;
            // (RtsCapture uses the skirmish, like a player would.)
            Load(toy ? TurretToy.Create(Seed) : Skirmish.Create(Seed));
        }

        // Replaces the running battle, views and all.
        public void Load(World world)
        {
            if (Presenter != null) Destroy(Presenter.gameObject);
            Presenter = new GameObject("Fleet").AddComponent<FleetPresenter>();
            Presenter.transform.SetParent(transform, false);
            World = world;
            Toy = world.Director as TurretToy;
            Battle = world.Director as Skirmish;
            Presenter.Init(Art, _fx, World);
            _accumulator = 0f;
            Dispatch();
        }

        void Update()
        {
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
            Presenter.Sync(Alpha, Paused ? 0f : Time.deltaTime * TimeScale);
            _projectiles.Draw(World, Alpha);
        }

        void Dispatch()
        {
            var events = World.Events;
            for (int i = 0; i < events.Count; i++) Presenter.Handle(events[i]);
            events.Clear();
        }

        void HandleKeys()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.spaceKey.wasPressedThisFrame) Paused = !Paused;
            if (kb.equalsKey.wasPressedThisFrame || kb.numpadPlusKey.wasPressedThisFrame) StepSpeed(1);
            if (kb.minusKey.wasPressedThisFrame || kb.numpadMinusKey.wasPressedThisFrame) StepSpeed(-1);
            if (kb.ctrlKey.isPressed && kb.rKey.wasPressedThisFrame)
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
