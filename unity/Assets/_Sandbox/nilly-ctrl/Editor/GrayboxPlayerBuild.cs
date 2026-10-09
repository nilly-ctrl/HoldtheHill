using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Builds the graybox scene as a Windows game for teammates to try without Unity.
    /// Menu: Tools > Hold the Hill > Build Graybox Player (Windows).
    /// </summary>
    /// <remarks>
    /// The level scenes are passed to the build directly, so the project's Build Settings scene list is
    /// left alone. Output goes outside the repository; pass <c>-hthBuildDir &lt;folder&gt;</c> on the
    /// command line to choose where, otherwise it is <c>Builds/Graybox</c> beside the project.
    /// </remarks>
    public static class GrayboxPlayerBuild
    {
        private const string ExeName = "HoldTheHill-Graybox.exe";

        [MenuItem("Tools/Hold the Hill/Build Graybox Player (Windows)")]
        public static void Build()
        {
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds", "Graybox"));
            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-hthBuildDir") folder = args[i + 1];
            }

            // The first scene is the one the game opens on; the rest are the other levels Home offers.
            var scenes = new System.Collections.Generic.List<string>();
            foreach (GrayboxLevels.Level level in GrayboxLevels.Available()) scenes.Add(level.ScenePath);

            var options = new BuildPlayerOptions
            {
                scenes = scenes.ToArray(),
                locationPathName = Path.Combine(folder, ExeName),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[Graybox] Player built: {summary.outputPath} ({summary.totalSize / (1024 * 1024)} MB)");
            }
            else
            {
                Debug.LogError($"[Graybox] Player build {summary.result}: {summary.totalErrors} errors.");
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }
    }
}
