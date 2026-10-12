using HoldTheHill.Features.Enemies;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl.Editor
{
    /// <summary>
    /// Entry points for the analytic balance sim: loads the gray-box tower catalog and wave asset,
    /// runs <see cref="BalanceSimulator"/>, and writes the text report and CSV.
    /// </summary>
    /// <remarks>
    /// This wraps the real-combat harness (GrayboxBalanceHarness, GrayboxDashboard) rather than
    /// replacing it. The harness plays the scene and measures; this estimates without one, in
    /// well under a second, so tuning changes can be checked before a scene run.
    /// </remarks>
    public static class GrayboxBalanceSim
    {
        public const string WavesPath = GrayboxProjectValidator.SandboxRoot + "/Graybox/GrayboxWaves.asset";
        public const string CatalogPath = GrayboxProjectValidator.SandboxRoot + "/Graybox/Data/GrayboxTowerCatalog.asset";

        /// <summary>Folder (ignored by Unity because of the tilde) the reports are written to.</summary>
        public static string OutputFolder =>
            Path.Combine(Application.dataPath, "_Sandbox", "nilly-ctrl", "Balance~");

        public static string CsvPath => Path.Combine(OutputFolder, "sim-report.csv");

        public static string TextPath => Path.Combine(OutputFolder, "sim-report.txt");

        [MenuItem("Tools/Hold the Hill/Validation/Run Balance Sim (estimate)", false, 110)]
        public static void RunFromMenu()
        {
            string text = RunAndWrite(null, null, new BalanceSettings(), null);
            Debug.Log("[Balance sim]\n" + text);
        }

        [MenuItem("Tools/Hold the Hill/Validation/Open Balance Sim Window", false, 111)]
        public static void OpenWindow()
        {
            GrayboxBalanceSimWindow.Open();
        }

        /// <summary>
        /// Batch entry: <c>-executeMethod HoldTheHill.Sandbox.NillyCtrl.Editor.GrayboxBalanceSim.RunBatch</c>.
        /// Writes the report files, logs the text and exits 0 (or 1 if the inputs could not be loaded).
        /// Automated runs must go through the Unity session lock.
        /// </summary>
        public static void RunBatch()
        {
            int exitCode = 0;
            try
            {
                string text = RunAndWrite(null, null, new BalanceSettings(), null);
                Debug.Log("[Balance sim]\n" + text);
            }
            catch (Exception exception)
            {
                Debug.LogError("[Balance sim] failed: " + exception);
                exitCode = 1;
            }

            EditorApplication.Exit(exitCode);
        }

        /// <summary>
        /// Builds the inputs from the assets (the defaults when null), runs the model, writes both
        /// files and returns the text report. <paramref name="harnessLog"/> is optional console
        /// text from a harness run, used to show measured numbers beside the estimate.
        /// </summary>
        public static string RunAndWrite(GrayboxTowerCatalog catalog, MapWaveDataSO waves,
            BalanceSettings settings, string harnessLog)
        {
            BalanceSimResult result = Run(catalog, waves, settings, harnessLog);

            Directory.CreateDirectory(OutputFolder);
            File.WriteAllText(CsvPath, BalanceReportWriter.ToCsv(result));
            string text = BalanceReportWriter.ToText(result);
            File.WriteAllText(TextPath, text);
            return text;
        }

        public static BalanceSimResult Run(GrayboxTowerCatalog catalog, MapWaveDataSO waves,
            BalanceSettings settings, string harnessLog)
        {
            catalog = catalog != null ? catalog : AssetDatabase.LoadAssetAtPath<GrayboxTowerCatalog>(CatalogPath);
            waves = waves != null ? waves : AssetDatabase.LoadAssetAtPath<MapWaveDataSO>(WavesPath);

            var notes = new List<string>();
            List<BalanceTower> towers = BalanceInputBuilder.BuildTowers(catalog, notes);
            List<BalanceWave> waveList = BalanceInputBuilder.BuildWaves(waves, notes);

            BalanceSimResult result = BalanceSimulator.Run(towers, waveList, settings ?? new BalanceSettings());
            result.Notes.InsertRange(0, notes);

            if (!string.IsNullOrEmpty(harnessLog))
            {
                HarnessLogParser.Apply(HarnessLogParser.Parse(harnessLog), result);
            }

            return result;
        }
    }
}
