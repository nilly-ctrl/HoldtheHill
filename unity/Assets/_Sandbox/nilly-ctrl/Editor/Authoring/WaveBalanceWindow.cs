using System.Collections.Generic;
using System.IO;
using HoldTheHill.Features.Enemies;
using UnityEditor;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl.Editor
{
    /// <summary>
    /// Edits a <see cref="MapWaveDataSO"/> as a table: one row per wave with its enemy count,
    /// total hit points, how long it takes to spawn and a rough damage-per-second the towers need.
    /// </summary>
    /// <remarks>
    /// All edits go through a SerializedObject on the asset, so Ctrl+Z works and the table is
    /// always the saved data. "DPS needed" is a deliberately crude figure: the wave's hit-point
    /// pool (health plus shield) divided by the seconds it takes to spawn plus the "time under
    /// fire" you set. The endless preview asks GrayboxProceduralWaveGenerator for its waves
    /// (nothing is copied from it), and scales by its own health multiplier.
    /// </remarks>
    internal sealed class WaveBalanceWindow : EditorWindow
    {
        private const string PrefabFolder = "Assets/_Sandbox/nilly-ctrl/Graybox/Prefabs";

        private struct EnemyInfo
        {
            public float Health;
            public float Shield;
            public float Speed;
            public int Bounty;
        }

        private struct WaveStats
        {
            public int Count;
            public int Missing;
            public float HpPool;
            public float SpawnSeconds;
            public int Bounty;
        }

        private readonly List<MapWaveDataSO> _assets = new List<MapWaveDataSO>();
        private string[] _assetLabels = new string[0];
        private int _assetIndex;
        private SerializedObject _so;
        private int _selectedWave = -1;
        private Vector2 _tableScroll;
        private Vector2 _detailScroll;
        private float _timeUnderFire = 20f;
        private float _bulkDelay = 1f;
        private int _quickIndex;
        private int _quickCount = 5;
        private List<GameObject> _enemyPrefabs;
        private string[] _enemyNames = new string[0];
        private readonly Dictionary<GameObject, EnemyInfo> _info = new Dictionary<GameObject, EnemyInfo>();
        private bool _showEndless;
        private int _endlessFrom = 1;
        private int _endlessTo = 20;

        [MenuItem("Tools/Hold the Hill/Authoring/Wave and Balance Editor")]
        public static void Open()
        {
            var window = GetWindow<WaveBalanceWindow>("Wave & Balance");
            window.minSize = new Vector2(560f, 420f);
            window.Show();
        }

        private void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedo;
            Refresh();
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        private void OnUndoRedo()
        {
            _info.Clear();
            _so?.Update();
            Repaint();
        }

        private void Refresh()
        {
            MapWaveDataSO current = _assetIndex >= 0 && _assetIndex < _assets.Count ? _assets[_assetIndex] : null;
            _assets.Clear();
            var labels = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:MapWaveDataSO"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<MapWaveDataSO>(path);
                if (asset != null)
                {
                    _assets.Add(asset);
                    labels.Add(path.Replace("Assets/", string.Empty).Replace('/', '∕'));
                }
            }

            _assetLabels = labels.ToArray();
            _assetIndex = current != null ? Mathf.Max(0, _assets.IndexOf(current)) : 0;
            if (current == null)
            {
                // Prefer the graybox waves, the ones the graybox scene plays.
                int graybox = _assets.FindIndex(a => a.name == "GrayboxWaves");
                _assetIndex = graybox >= 0 ? graybox : 0;
            }

            _enemyPrefabs = new List<GameObject>();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetDirectoryName(path)?.Replace('\\', '/') != PrefabFolder)
                {
                    continue;
                }

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null && prefab.GetComponent<EnemyHealth>() != null && !prefab.name.EndsWith("Base"))
                {
                    _enemyPrefabs.Add(prefab);
                }
            }

            _enemyPrefabs.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            _enemyNames = _enemyPrefabs.ConvertAll(p => p.name.Replace("Graybox", string.Empty)).ToArray();
            _info.Clear();
            BindAsset();
        }

        private void BindAsset()
        {
            _so = _assetIndex >= 0 && _assetIndex < _assets.Count ? new SerializedObject(_assets[_assetIndex]) : null;
            _selectedWave = -1;
        }

        private EnemyInfo InfoFor(GameObject prefab)
        {
            if (_info.TryGetValue(prefab, out EnemyInfo info))
            {
                return info;
            }

            var health = prefab.GetComponent<EnemyHealth>();
            var shield = prefab.GetComponent<EnemyShield>();
            var mover = prefab.GetComponent<EnemyMover>();
            info = new EnemyInfo
            {
                Health = health != null ? health.MaxHealth : 0f,
                Shield = shield != null ? shield.MaxShield : 0f,
                Speed = mover != null ? mover.Speed : 0f,
                Bounty = health != null ? health.BountyValue : 0,
            };
            _info[prefab] = info;
            return info;
        }

        private WaveStats StatsFor(SerializedProperty wave)
        {
            SerializedProperty enemies = wave.FindPropertyRelative("enemies");
            var stats = new WaveStats();
            for (int i = 0; i < enemies.arraySize; i++)
            {
                SerializedProperty entry = enemies.GetArrayElementAtIndex(i);
                var prefab = entry.FindPropertyRelative("enemyPrefab").objectReferenceValue as GameObject;
                if (prefab == null)
                {
                    stats.Missing++;
                    continue;
                }

                EnemyInfo info = InfoFor(prefab);
                stats.Count++;
                stats.HpPool += info.Health + info.Shield;
                stats.Bounty += info.Bounty;
                if (i < enemies.arraySize - 1)
                {
                    stats.SpawnSeconds += entry.FindPropertyRelative("delayBeforeNext").floatValue;
                }
            }

            return stats;
        }

        private float DpsNeeded(WaveStats stats) => stats.HpPool / Mathf.Max(1f, stats.SpawnSeconds + _timeUnderFire);

        private void OnGUI()
        {
            DrawToolbar();
            if (_so == null || _so.targetObject == null)
            {
                EditorGUILayout.HelpBox("No MapWaveDataSO assets found.", MessageType.Info);
                return;
            }

            _so.Update();
            SerializedProperty waves = _so.FindProperty("waves");

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(_so.FindProperty("mapId"));
            }

            _timeUnderFire = Mathf.Max(0f, EditorGUILayout.FloatField(
                new GUIContent("Time under fire (s)", "Seconds the last enemy of a wave is assumed to stay in reach after it spawns. Only used for DPS needed."),
                _timeUnderFire));

            DrawTable(waves);
            DrawWaveTools(waves);
            if (_selectedWave >= 0 && _selectedWave < waves.arraySize)
            {
                DrawDetail(waves.GetArrayElementAtIndex(_selectedWave));
            }

            DrawEndless();

            if (_so.ApplyModifiedProperties())
            {
                Repaint();
            }
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                int picked = EditorGUILayout.Popup(_assetIndex, _assetLabels, EditorStyles.toolbarPopup);
                if (picked != _assetIndex)
                {
                    _assetIndex = picked;
                    BindAsset();
                }

                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60f)))
                {
                    Refresh();
                }

                if (GUILayout.Button("Ping", EditorStyles.toolbarButton, GUILayout.Width(40f)) && _assetIndex < _assets.Count)
                {
                    EditorGUIUtility.PingObject(_assets[_assetIndex]);
                }

                if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(45f)) && _assetIndex < _assets.Count)
                {
                    EditorUtility.SetDirty(_assets[_assetIndex]);
                    AssetDatabase.SaveAssets();
                }
            }
        }

        private void DrawTable(SerializedProperty waves)
        {
            const float indexW = 30f, countW = 60f, hpW = 70f, spawnW = 62f, bountyW = 60f, dpsW = 70f, vsW = 60f;

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.Label("#", EditorStyles.miniBoldLabel, GUILayout.Width(indexW));
                GUILayout.Label("Wave", EditorStyles.miniBoldLabel, GUILayout.MinWidth(120f));
                GUILayout.Label("Enemies", EditorStyles.miniBoldLabel, GUILayout.Width(countW));
                GUILayout.Label("HP pool", EditorStyles.miniBoldLabel, GUILayout.Width(hpW));
                GUILayout.Label("Spawn s", EditorStyles.miniBoldLabel, GUILayout.Width(spawnW));
                GUILayout.Label("Bounty", EditorStyles.miniBoldLabel, GUILayout.Width(bountyW));
                GUILayout.Label("DPS need", EditorStyles.miniBoldLabel, GUILayout.Width(dpsW));
                GUILayout.Label("HP vs prev", EditorStyles.miniBoldLabel, GUILayout.Width(vsW));
            }

            float height = Mathf.Min(waves.arraySize * 20f + 8f, 240f);
            _tableScroll = EditorGUILayout.BeginScrollView(_tableScroll, GUILayout.Height(height));
            var total = new WaveStats();
            float previousHp = 0f;
            for (int i = 0; i < waves.arraySize; i++)
            {
                SerializedProperty wave = waves.GetArrayElementAtIndex(i);
                WaveStats stats = StatsFor(wave);
                total.Count += stats.Count;
                total.HpPool += stats.HpPool;
                total.SpawnSeconds += stats.SpawnSeconds;
                total.Bounty += stats.Bounty;

                Color previous = GUI.backgroundColor;
                if (i == _selectedWave)
                {
                    GUI.backgroundColor = new Color(0.5f, 0.75f, 1f);
                }

                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    if (GUILayout.Button((i + 1).ToString(), GUILayout.Width(indexW)))
                    {
                        _selectedWave = i;
                        GUI.FocusControl(null);
                    }

                    EditorGUILayout.PropertyField(wave.FindPropertyRelative("waveName"), GUIContent.none, GUILayout.MinWidth(120f));
                    string count = stats.Missing > 0 ? $"{stats.Count} (+{stats.Missing}?)" : stats.Count.ToString();
                    GUILayout.Label(count, GUILayout.Width(countW));
                    GUILayout.Label(stats.HpPool.ToString("0"), GUILayout.Width(hpW));
                    GUILayout.Label(stats.SpawnSeconds.ToString("0.0"), GUILayout.Width(spawnW));
                    GUILayout.Label(stats.Bounty.ToString(), GUILayout.Width(bountyW));
                    GUILayout.Label(DpsNeeded(stats).ToString("0.0"), GUILayout.Width(dpsW));
                    GUILayout.Label(
                        i > 0 && previousHp > 0f ? $"{(stats.HpPool / previousHp - 1f) * 100f:+0;-0;0}%" : "-",
                        GUILayout.Width(vsW));
                }

                GUI.backgroundColor = previous;
                previousHp = stats.HpPool;
            }

            EditorGUILayout.EndScrollView();

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.Label("Total", EditorStyles.miniBoldLabel, GUILayout.Width(indexW + 126f));
                GUILayout.Label(total.Count.ToString(), EditorStyles.miniBoldLabel, GUILayout.Width(countW));
                GUILayout.Label(total.HpPool.ToString("0"), EditorStyles.miniBoldLabel, GUILayout.Width(hpW));
                GUILayout.Label(total.SpawnSeconds.ToString("0.0"), EditorStyles.miniBoldLabel, GUILayout.Width(spawnW));
                GUILayout.Label(total.Bounty.ToString(), EditorStyles.miniBoldLabel, GUILayout.Width(bountyW));
            }
        }

        private void DrawWaveTools(SerializedProperty waves)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add wave"))
                {
                    int at = waves.arraySize;
                    waves.InsertArrayElementAtIndex(at);
                    SerializedProperty wave = waves.GetArrayElementAtIndex(at);
                    wave.FindPropertyRelative("waveName").stringValue = $"Wave {at + 1}";
                    wave.FindPropertyRelative("enemies").ClearArray();
                    _selectedWave = at;
                }

                bool has = _selectedWave >= 0 && _selectedWave < waves.arraySize;
                using (new EditorGUI.DisabledScope(!has))
                {
                    if (GUILayout.Button("Duplicate"))
                    {
                        waves.InsertArrayElementAtIndex(_selectedWave);
                        _selectedWave++;
                        waves.GetArrayElementAtIndex(_selectedWave).FindPropertyRelative("waveName").stringValue += " copy";
                    }

                    if (GUILayout.Button("Up") && _selectedWave > 0)
                    {
                        waves.MoveArrayElement(_selectedWave, _selectedWave - 1);
                        _selectedWave--;
                    }

                    if (GUILayout.Button("Down") && _selectedWave < waves.arraySize - 1)
                    {
                        waves.MoveArrayElement(_selectedWave, _selectedWave + 1);
                        _selectedWave++;
                    }

                    if (GUILayout.Button("Delete") && EditorUtility.DisplayDialog(
                            "Delete wave", $"Delete wave {_selectedWave + 1}?", "Delete", "Cancel"))
                    {
                        waves.DeleteArrayElementAtIndex(_selectedWave);
                        _selectedWave = -1;
                    }
                }
            }
        }

        private void DrawDetail(SerializedProperty wave)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Wave {_selectedWave + 1}: {wave.FindPropertyRelative("waveName").stringValue}", EditorStyles.boldLabel);
            SerializedProperty enemies = wave.FindPropertyRelative("enemies");

            // Counts per enemy type.
            var counts = new SortedDictionary<string, int>();
            for (int i = 0; i < enemies.arraySize; i++)
            {
                var prefab = enemies.GetArrayElementAtIndex(i).FindPropertyRelative("enemyPrefab").objectReferenceValue;
                string key = prefab != null ? prefab.name.Replace("Graybox", string.Empty) : "(missing)";
                counts[key] = counts.TryGetValue(key, out int n) ? n + 1 : 1;
            }

            var parts = new List<string>();
            foreach (KeyValuePair<string, int> pair in counts)
            {
                parts.Add($"{pair.Value} x {pair.Key}");
            }

            EditorGUILayout.LabelField(parts.Count > 0 ? string.Join(", ", parts) : "Empty wave", EditorStyles.wordWrappedMiniLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (_enemyNames.Length > 0)
                {
                    _quickIndex = Mathf.Clamp(EditorGUILayout.Popup(_quickIndex, _enemyNames), 0, _enemyNames.Length - 1);
                    _quickCount = Mathf.Max(1, EditorGUILayout.IntField(_quickCount, GUILayout.Width(40f)));
                    if (GUILayout.Button("Add enemies", GUILayout.Width(100f)))
                    {
                        for (int i = 0; i < _quickCount; i++)
                        {
                            int at = enemies.arraySize;
                            enemies.InsertArrayElementAtIndex(at);
                            SerializedProperty entry = enemies.GetArrayElementAtIndex(at);
                            entry.FindPropertyRelative("enemyPrefab").objectReferenceValue = _enemyPrefabs[_quickIndex];
                            entry.FindPropertyRelative("delayBeforeNext").floatValue = _bulkDelay;
                            entry.FindPropertyRelative("spawnPointOverride").objectReferenceValue = null;
                        }
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                _bulkDelay = Mathf.Max(0f, EditorGUILayout.FloatField("Delay", _bulkDelay));
                if (GUILayout.Button("Set all delays", GUILayout.Width(110f)))
                {
                    for (int i = 0; i < enemies.arraySize; i++)
                    {
                        enemies.GetArrayElementAtIndex(i).FindPropertyRelative("delayBeforeNext").floatValue = _bulkDelay;
                    }
                }
            }

            float height = Mathf.Min(enemies.arraySize * 22f + 6f, 220f);
            _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll, GUILayout.Height(height));
            int remove = -1;
            for (int i = 0; i < enemies.arraySize; i++)
            {
                SerializedProperty entry = enemies.GetArrayElementAtIndex(i);
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label((i + 1).ToString(), GUILayout.Width(28f));
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("enemyPrefab"), GUIContent.none);
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("delayBeforeNext"), GUIContent.none, GUILayout.Width(60f));
                    if (GUILayout.Button("x", GUILayout.Width(22f)))
                    {
                        remove = i;
                    }
                }
            }

            EditorGUILayout.EndScrollView();
            if (remove >= 0)
            {
                enemies.DeleteArrayElementAtIndex(remove);
            }
        }

        private void DrawEndless()
        {
            EditorGUILayout.Space();
            _showEndless = EditorGUILayout.Foldout(_showEndless, "Endless waves (GrayboxProceduralWaveGenerator, read only)", true);
            if (!_showEndless)
            {
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                _endlessFrom = Mathf.Max(1, EditorGUILayout.IntField("From wave", _endlessFrom));
                _endlessTo = Mathf.Clamp(EditorGUILayout.IntField("To wave", _endlessTo), _endlessFrom, _endlessFrom + 60);
            }

            GameObject Load(string id) => AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/Graybox{id}.prefab");

            GameObject temp = new GameObject("WaveBalancePreview") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                // Awake does not run in edit mode, so this has no side effects.
                var generator = temp.AddComponent<GrayboxProceduralWaveGenerator>();
                for (int n = _endlessFrom; n <= _endlessTo; n++)
                {
                    Wave wave = generator.GenerateWave(
                        n, Load("Runner"), Load("Grunt"), Load("Brute"), Load("Shielded"), Load("Splitter"), Load("Healer"));
                    float multiplier = generator.GetHealthMultiplier(n);
                    float hp = 0f;
                    float seconds = 0f;
                    for (int i = 0; i < wave.enemies.Count; i++)
                    {
                        EnemyInfo info = InfoFor(wave.enemies[i].enemyPrefab);
                        hp += (info.Health + info.Shield) * multiplier;
                        if (i < wave.enemies.Count - 1)
                        {
                            seconds += wave.enemies[i].delayBeforeNext;
                        }
                    }

                    EditorGUILayout.LabelField(
                        wave.waveName,
                        $"{wave.enemies.Count} enemies, HP x{multiplier:0.00} = {hp:0}, spawn {seconds:0.0}s, " +
                        $"DPS {hp / Mathf.Max(1f, seconds + _timeUnderFire):0.0}");
                }
            }
            finally
            {
                DestroyImmediate(temp);
            }
        }
    }
}
