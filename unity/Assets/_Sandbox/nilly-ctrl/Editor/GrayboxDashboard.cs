using System.Collections.Generic;
using System.Reflection;
using HoldTheHill.Features.Enemies;
using HoldTheHill.Features.Towers;
using HoldTheHill.Sandbox.NillyCtrl;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl.Editor
{
    /// <summary>
    /// Editor Window Dashboard for managing, running, and inspecting the Hold the Hill combat grey-box environment.
    /// </summary>
    public class GrayboxDashboard : EditorWindow
    {
        private const string ScenePath = "Assets/_Sandbox/nilly-ctrl/Graybox/GrayboxCombatTest.unity";
        private const string WaveAssetPath = "Assets/_Sandbox/nilly-ctrl/Graybox/GrayboxWaves.asset";

        private Vector2 _scrollPos;
        private string _lastTestReport = "No balance test run yet. Click 'Run Balance Simulation' to execute.";
        private bool _isTestRunning;

        [MenuItem("Tools/Hold the Hill/Graybox Dashboard", false, 1)]
        public static void ShowWindow()
        {
            var window = GetWindow<GrayboxDashboard>("Combat Greybox");
            window.minSize = new Vector2(380, 520);
            window.Show();
        }

        [MenuItem("Tools/Hold the Hill/Run Graybox Balance Simulation", false, 10)]
        public static void RunBalanceSimulationFromMenu()
        {
            ShowWindow();
            var window = GetWindow<GrayboxDashboard>("Combat Greybox");
            window.RunBalanceSimulation();
        }

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Hold the Hill — Combat Greybox Dashboard", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Control, build, test, and tune the tower & combat mechanics grey box environment.", MessageType.Info);
            EditorGUILayout.Space(8);

            DrawSceneSection();
            EditorGUILayout.Space(10);

            DrawVisualizationSection();
            EditorGUILayout.Space(10);

            DrawBalanceTestSection();
            EditorGUILayout.Space(10);

            DrawCustomHordeSection();
            EditorGUILayout.Space(10);

            DrawEconomySection();
            EditorGUILayout.Space(10);

            DrawSkillTreeSection();
            EditorGUILayout.Space(10);

            DrawWaveAssetSummarySection();

            EditorGUILayout.EndScrollView();
        }

        private void DrawSkillTreeSection()
        {
            EditorGUILayout.LabelField("6. Commander Skill Tree Dev Controls", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var skillTree = FindAnyObjectByType<GrayboxSkillTree>();
                int currentSP = skillTree != null ? skillTree.SkillPoints : 0;
                EditorGUILayout.LabelField($"Current Skill Points: {currentSP} SP", EditorStyles.boldLabel);

                EditorGUI.BeginDisabledGroup(!Application.isPlaying || skillTree == null);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("+ 5 SP")) skillTree?.AddSkillPoints(5);
                    if (GUILayout.Button("+ 10 SP")) skillTree?.AddSkillPoints(10);
                    if (GUILayout.Button("Reset Skills")) skillTree?.ResetSkills();
                }
                EditorGUI.EndDisabledGroup();

                if (!Application.isPlaying)
                {
                    EditorGUILayout.HelpBox("Press Play to grant Skill Points and test skill tree upgrades.", MessageType.Info);
                }
            }
        }

        private void DrawSceneSection()
        {
            EditorGUILayout.LabelField("1. Scene Management", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"Target Scene: {ScenePath}", EditorStyles.miniLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Open Greybox Scene", GUILayout.Height(28)))
                    {
                        GrayboxBuilder.Open();
                    }

                    if (GUILayout.Button("Rebuild Scene & Assets", GUILayout.Height(28)))
                    {
                        if (EditorUtility.DisplayDialog("Rebuild Greybox", "Rebuild the greybox scene, prefabs, and wave asset from scratch?", "Rebuild", "Cancel"))
                        {
                            GrayboxBuilder.Build();
                        }
                    }
                }
            }
        }

        private void DrawVisualizationSection()
        {
            EditorGUILayout.LabelField("2. Visual Overlays & Debug Controls", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                bool prevRings = TowerTargetVisualizer.ShowRangeRings;
                bool prevLines = TowerTargetVisualizer.ShowTargetLines;
                bool prevLabels = TowerTargetVisualizer.ShowTowerLabels;
                bool prevBars = EnemyHealthBar.ShowHealthBars;

                bool newRings = EditorGUILayout.Toggle("Show Tower Range Rings", prevRings);
                bool newLines = EditorGUILayout.Toggle("Show Target Lock Lines", prevLines);
                bool newLabels = EditorGUILayout.Toggle("Show Tower Priority Badges", prevLabels);
                bool newBars = EditorGUILayout.Toggle("Show Enemy Health Bars", prevBars);

                if (newRings != prevRings || newLines != prevLines || newLabels != prevLabels)
                {
                    TowerTargetVisualizer.ShowRangeRings = newRings;
                    TowerTargetVisualizer.ShowTargetLines = newLines;
                    TowerTargetVisualizer.ShowTowerLabels = newLabels;
                    SceneView.RepaintAll();
                }

                if (newBars != prevBars)
                {
                    EnemyHealthBar.ShowHealthBars = newBars;
                }

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("PlayMode Time Scale:", EditorStyles.miniBoldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("1x (Normal)")) Time.timeScale = 1f;
                    if (GUILayout.Button("2x (Fast)")) Time.timeScale = 2f;
                    if (GUILayout.Button("4x (Rapid)")) Time.timeScale = 4f;
                }
            }
        }

        private void DrawBalanceTestSection()
        {
            EditorGUILayout.LabelField("3. Automated Balance Testing", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Simulates wave playback to measure kill rates and leaks.", EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.Space(4);

                EditorGUI.BeginDisabledGroup(_isTestRunning);
                if (GUILayout.Button("Run Balance Simulation", GUILayout.Height(32)))
                {
                    RunBalanceSimulation();
                }
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("Latest Simulation Report:", EditorStyles.boldLabel);
                EditorGUILayout.TextArea(_lastTestReport, GUILayout.MinHeight(100));
            }
        }

        private void DrawCustomHordeSection()
        {
            EditorGUILayout.LabelField("4. Custom Horde Stress-Test Spawner", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Triggers custom horde presets in PlayMode on demand.", EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.Space(4);

                var customSpawner = FindAnyObjectByType<GrayboxCustomSpawner>();
                EditorGUI.BeginDisabledGroup(!Application.isPlaying || customSpawner == null);

                if (GUILayout.Button("Spawn Swarm Rush (40 Swarm Ants)")) customSpawner?.SpawnPreset_SwarmRush();
                if (GUILayout.Button("Spawn Brute Parade (12 Brute Ants)")) customSpawner?.SpawnPreset_BruteParade();
                if (GUILayout.Button("Spawn Phalanx Shield (16 Shielded & Healers)")) customSpawner?.SpawnPreset_Phalanx();
                if (GUILayout.Button("Spawn Hydra Splitters (10 Splitter Ants)")) customSpawner?.SpawnPreset_HydraSplitters();
                if (GUILayout.Button("Spawn 100-Enemy Mega Horde")) customSpawner?.SpawnPreset_MegaHorde();

                EditorGUI.EndDisabledGroup();

                if (!Application.isPlaying)
                {
                    EditorGUILayout.HelpBox("Press Play to enable custom horde spawner buttons.", MessageType.Info);
                }
            }
        }

        private void DrawEconomySection()
        {
            EditorGUILayout.LabelField("5. Economy & Dev Gold Controls", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var economy = FindAnyObjectByType<GrayboxEconomy>();
                int currentGold = economy != null ? economy.CurrentGold : 0;
                EditorGUILayout.LabelField($"Current Gold Pool: ${currentGold}", EditorStyles.boldLabel);

                EditorGUI.BeginDisabledGroup(!Application.isPlaying || economy == null);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("+ $250 Gold")) economy?.EarnGold(250);
                    if (GUILayout.Button("+ $500 Gold")) economy?.EarnGold(500);
                    if (GUILayout.Button("+ $1,000 Gold")) economy?.EarnGold(1000);
                    if (GUILayout.Button("Reset ($500)")) economy?.ResetGold(500);
                }
                EditorGUI.EndDisabledGroup();

                if (!Application.isPlaying)
                {
                    EditorGUILayout.HelpBox("Press Play to test live Gold earnings and spending.", MessageType.Info);
                }
            }
        }

        private void DrawWaveAssetSummarySection()
        {
            EditorGUILayout.LabelField("7. Wave Asset Inspector", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var waveAsset = AssetDatabase.LoadAssetAtPath<MapWaveDataSO>(WaveAssetPath);
                if (waveAsset == null)
                {
                    EditorGUILayout.HelpBox("MapWaveDataSO asset not found at path.", MessageType.Warning);
                    return;
                }

                EditorGUILayout.LabelField($"Map ID: {waveAsset.mapId}", EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField($"Total Waves: {waveAsset.waves.Count}", EditorStyles.miniLabel);

                for (int i = 0; i < waveAsset.waves.Count; i++)
                {
                    Wave wave = waveAsset.waves[i];
                    EditorGUILayout.LabelField($"Wave {i + 1}: {wave.waveName} ({wave.enemies.Count} enemies)", EditorStyles.miniLabel);
                }
            }
        }

        public void RunBalanceSimulation()
        {
            _isTestRunning = true;
            _lastTestReport = "Running balance simulation harness...\nOpening scene and executing waves...";
            Repaint();

            if (!EditorApplication.isPlaying)
            {
                GrayboxBuilder.Open();
                EditorApplication.isPlaying = true;
            }

            _lastTestReport = "[Simulation Started]\nPress Play or watch Graybox Hud during execution.\nResults are logged upon wave completion.";
            _isTestRunning = false;
        }
    }
}
