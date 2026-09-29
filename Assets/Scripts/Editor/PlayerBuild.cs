using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace GalacticRupture.EditorTools
{
    public static class PlayerBuild
    {
        public const string LinuxPath = "Builds/Linux/GalacticRupture.x86_64";

        [MenuItem("Galactic Rupture/Build Linux Player")]
        public static void Linux()
        {
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/TurretToy.unity" },
                locationPathName = LinuxPath,
                target = BuildTarget.StandaloneLinux64,
                options = BuildOptions.None,
            });
            Debug.Log($"[build] {report.summary.result} {report.summary.totalSize / (1024 * 1024)} MB in {report.summary.totalTime}");
            if (report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }

        // Regenerate everything, then build: one batch call.
        public static void BuildAllAndLinux()
        {
            ProjectBuilder.BuildAll();
            Linux();
        }
    }
}
