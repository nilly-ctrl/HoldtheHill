using HoldTheHill.Features.Enemies;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl.Editor
{
    /// <summary>Runs the analytic balance sim with adjustable settings and shows the report.</summary>
    public class GrayboxBalanceSimWindow : EditorWindow
    {
        private readonly BalanceSettings _settings = new BalanceSettings();
        private GrayboxTowerCatalog _catalog;
        private MapWaveDataSO _waves;
        private string _harnessLog = string.Empty;
        private string _report = "Press Run. This is an estimate from a simple model, not the real combat.";
        private Vector2 _scroll;
        private Vector2 _logScroll;

        public static void Open()
        {
            var window = GetWindow<GrayboxBalanceSimWindow>("HtH Balance Sim");
            window.minSize = new Vector2(480f, 480f);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Analytic estimate. It models towers hitting the front-most enemies along a single line of route " +
                "and leaves out slows, statuses, healers and upgrades. The real combat is measured by the in-scene harness.",
                MessageType.Info);

            _catalog = (GrayboxTowerCatalog)EditorGUILayout.ObjectField("Tower catalog (default: graybox)",
                _catalog, typeof(GrayboxTowerCatalog), false);
            _waves = (MapWaveDataSO)EditorGUILayout.ObjectField("Wave asset (default: graybox)",
                _waves, typeof(MapWaveDataSO), false);

            _settings.PathLength = EditorGUILayout.FloatField("Route length (units)", _settings.PathLength);
            _settings.StartingFood = EditorGUILayout.IntField("Starting gold", _settings.StartingFood);
            _settings.MaxTowers = EditorGUILayout.IntField("Max towers", _settings.MaxTowers);
            _settings.Strategy = (BalanceBuyStrategy)EditorGUILayout.EnumPopup("Buying", _settings.Strategy);
            _settings.PathOffset = EditorGUILayout.FloatField("Tower distance from road", _settings.PathOffset);
            _settings.DamageMultiplier = EditorGUILayout.FloatField("Damage multiplier", _settings.DamageMultiplier);
            _settings.BountyMultiplier = EditorGUILayout.FloatField("Bounty multiplier", _settings.BountyMultiplier);
            _settings.WaveClearBonus = EditorGUILayout.IntField("Gold per cleared wave", _settings.WaveClearBonus);
            _settings.HillHealth = EditorGUILayout.FloatField("Hill health", _settings.HillHealth);

            EditorGUILayout.LabelField("Harness console output (optional, to compare):");
            _logScroll = EditorGUILayout.BeginScrollView(_logScroll, GUILayout.Height(48f));
            _harnessLog = EditorGUILayout.TextArea(_harnessLog, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Run estimate", GUILayout.Height(28f)))
                {
                    _report = GrayboxBalanceSim.RunAndWrite(_catalog, _waves, _settings, _harnessLog);
                }

                if (GUILayout.Button("Open output folder", GUILayout.Height(28f)))
                {
                    Directory.CreateDirectory(GrayboxBalanceSim.OutputFolder);
                    EditorUtility.RevealInFinder(GrayboxBalanceSim.CsvPath);
                }

                if (GUILayout.Button("Run real harness (Play Mode)", GUILayout.Height(28f)))
                {
                    // The existing dashboard entry; it opens the scene and enters Play Mode.
                    EditorApplication.ExecuteMenuItem("Tools/Hold the Hill/Run Graybox Balance Simulation");
                }
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.SelectableLabel(_report, EditorStyles.textArea, GUILayout.ExpandHeight(true), GUILayout.MinHeight(300f));
            EditorGUILayout.EndScrollView();
        }
    }
}
