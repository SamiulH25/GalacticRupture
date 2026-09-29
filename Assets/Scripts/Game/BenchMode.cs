using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GalacticRupture.Sim;
using UnityEngine;

namespace GalacticRupture.Game
{
    // `-grBench <dir>`: the unit-count benchmark (IDEAS.md §8.8). For each fleet size, frames
    // the middle of the fleet at three camera distances, measures frame time with vsync off,
    // writes bench.csv and a screenshot per size, and quits.
    public sealed class BenchMode : MonoBehaviour
    {
        public SimRunner Runner;
        public CameraRig Rig;

        public static bool Active { get; private set; }

        static readonly int[] Sizes = { 50, 100, 250, 500, 1000, 2000 };
        static readonly (string Name, float Distance, float Pitch)[] Views =
        {
            ("close", 700f, 12f),
            ("mid", 3500f, 22f),
            ("far", 14000f, 35f),
        };

        const float Warmup = 1.5f;
        const float Measure = 3f;

        string _dir;

        void Awake()
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-grBench") _dir = args[i + 1];
            Active = _dir != null;
            if (!Active)
            {
                enabled = false;
                return;
            }
            Directory.CreateDirectory(_dir);
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            Rig.Cinematic = false;
        }

        IEnumerator Start()
        {
            var csv = new StringBuilder("ships,view,distance,avg_ms,p95_ms,fps,sim_ms,gpu_name\n");
            foreach (int n in Sizes)
            {
                Runner.Load(FleetBench.Create(20260929, n));
                yield return null;
                yield return null;
                int centre = CentreShip(Runner.World);

                foreach (var view in Views)
                {
                    Rig.SnapTo(centre, 30f, view.Pitch, view.Distance, Vector3.zero);
                    yield return new WaitForSecondsRealtime(Warmup);
                    var frames = new List<float>();
                    double sim = 0;
                    float end = Time.realtimeSinceStartup + Measure;
                    while (Time.realtimeSinceStartup < end)
                    {
                        yield return null;
                        frames.Add(Time.unscaledDeltaTime * 1000f);
                        sim += Runner.LastStepMs;
                    }
                    frames.Sort();
                    float avg = 0f;
                    foreach (var f in frames) avg += f;
                    avg /= frames.Count;
                    float p95 = frames[Mathf.Min(frames.Count - 1, (int)(frames.Count * 0.95f))];
                    csv.AppendLine($"{n},{view.Name},{view.Distance},{avg:F2},{p95:F2},{1000f / avg:F1},{sim / frames.Count:F2},{SystemInfo.graphicsDeviceName}");
                    Debug.Log($"[bench] {n} ships, {view.Name}: {avg:F2} ms avg, {p95:F2} ms p95, sim {sim / frames.Count:F2} ms");
                    if (view.Name == "mid") ScreenCapture.CaptureScreenshot(Path.Combine(_dir, $"bench_{n}.png"));
                }
            }
            File.WriteAllText(Path.Combine(_dir, "bench.csv"), csv.ToString());
            Application.Quit();
        }

        static int CentreShip(World world)
        {
            int best = 0;
            float bestD = float.MaxValue;
            foreach (var s in world.Ships)
            {
                if (s.Side != 0) continue;
                float d = s.Position.LengthSquared();
                if (d < bestD)
                {
                    bestD = d;
                    best = s.Id;
                }
            }
            return best;
        }
    }
}
