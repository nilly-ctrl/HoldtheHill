using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace HoldTheHill.Features.Enemies
{
    /// <summary>
    /// Controls enemy spawning for a 2D Tower Defense game.
    /// Handles map-specific wave configurations, sequential enemy spawning,
    /// and pauses at the end of each wave until prompted to continue.
    /// Listens to <see cref="EnemyHealth.Defeated"/> and <see cref="EnemyMover.ReachedEnd"/>
    /// so its living-enemy count goes down when an enemy dies or walks off the path.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        public enum SpawnerState
        {
            Idle,               // Waiting to start the first wave
            Spawning,           // Currently spawning enemies for the active wave
            WaitingForNextWave, // Wave spawning finished; paused until prompted to continue
            Completed           // All waves for the active map are finished
        }

        [Header("Spawn Position")]
        [Tooltip("Default 2D spawn location. If null, this GameObject's transform is used.")]
        [SerializeField] private Transform defaultSpawnPoint = null;

        [Tooltip("Optional parent transform to organize spawned enemies in the Hierarchy.")]
        [SerializeField] private Transform enemyContainer = null;

        [Header("Map Configuration")]
        [Tooltip("ID or name of the map currently active (e.g., 'Level1', 'Grasslands').")]
        [SerializeField] private string activeMapId = "Map_01";

        [Tooltip("If true, automatically sets activeMapId to the active Scene name upon start.")]
        [SerializeField] private bool autoDetectMapFromScene = false;

        [Tooltip("Direct override asset for this map's waves. If assigned, this takes top priority.")]
        [SerializeField] private MapWaveDataSO activeMapConfigOverride = null;

        [Tooltip("List of ScriptableObject wave configurations for different maps.")]
        [SerializeField] private List<MapWaveDataSO> scriptableMapConfigs = new List<MapWaveDataSO>();

        [Tooltip("Inline wave configurations defined directly in this component's inspector.")]
        [SerializeField] private List<MapWaveData> inlineMapConfigs = new List<MapWaveData>();

        [Header("Wave Progression Settings")]
        [Tooltip("If true, Wave 1 starts automatically when the scene loads. If false, waits for StartNextWave() prompt.")]
        [SerializeField] private bool autoStartFirstWave = false;

        [Tooltip("If true, requires all living enemies of the current wave to be defeated before StartNextWave() can be triggered.")]
        [SerializeField] private bool requireEnemiesClearedBeforeNextWave = false;

        [Header("Events")]
        [Tooltip("Invoked whenever an enemy is instantiated.")]
        public UnityEvent<GameObject> onEnemySpawned;

        [Tooltip("Invoked when a wave starts spawning. Passes (currentWaveNumber, totalWaves).")]
        public UnityEvent<int, int> onWaveStarted;

        [Tooltip("Invoked when all enemies in the current wave have finished spawning. Passes (completedWaveNumber).")]
        public UnityEvent<int> onWaveCompleted;

        [Tooltip("Invoked when all waves for the active map have been finished.")]
        public UnityEvent onAllWavesCompleted;

        [Tooltip("Invoked when the active map changes. Passes (mapId).")]
        public UnityEvent<string> onMapChanged;

        // Runtime state
        private SpawnerState currentState = SpawnerState.Idle;
        private int currentWaveIndex = 0;
        private List<Wave> activeWaves = new List<Wave>();
        private Coroutine spawnCoroutine;
        private readonly List<GameObject> activeLivingEnemies = new List<GameObject>();

        // Public Getters
        public SpawnerState CurrentState { get { return currentState; } }
        public string ActiveMapId { get { return activeMapId; } }
        public int CurrentWaveIndex { get { return currentWaveIndex; } }
        public int CurrentWaveNumber { get { return currentWaveIndex + 1; } }
        public int TotalWaves { get { return activeWaves != null ? activeWaves.Count : 0; } }
        public int LivingEnemyCount { get { return activeLivingEnemies.Count; } }
        public bool IsSpawning { get { return currentState == SpawnerState.Spawning; } }

        private void Awake()
        {
            if (defaultSpawnPoint == null)
            {
                defaultSpawnPoint = transform;
            }
        }

        private void OnEnable()
        {
            EnemyHealth.Defeated += NotifyEnemyDefeated;
            EnemyMover.ReachedEnd += OnEnemyReachedEnd;
        }

        private void OnDisable()
        {
            EnemyHealth.Defeated -= NotifyEnemyDefeated;
            EnemyMover.ReachedEnd -= OnEnemyReachedEnd;
        }

        // An enemy that walks off the end of the path is gone just as surely as a dead one. Without
        // this every leak counted as alive forever, so "all enemies cleared" never came true after
        // the first leak.
        private void OnEnemyReachedEnd(GameObject enemy)
        {
            if (enemy == null)
            {
                return;
            }

            var mover = enemy.GetComponent<EnemyMover>();
            if (mover == null || mover.DespawnsAtEnd)
            {
                NotifyEnemyDefeated(enemy);
            }
        }

        private void Start()
        {
            if (autoDetectMapFromScene)
            {
                activeMapId = SceneManager.GetActiveScene().name;
            }

            LoadMapConfiguration(activeMapId);

            if (autoStartFirstWave)
            {
                StartNextWave();
            }
        }

        /// <summary>
        /// Loads the wave configuration for the given map ID and resets wave progress.
        /// </summary>
        /// <param name="mapId">Identifier for the map.</param>
        public void SetActiveMap(string mapId)
        {
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }

            activeMapId = mapId;
            LoadMapConfiguration(activeMapId);
            currentWaveIndex = 0;
            currentState = SpawnerState.Idle;
            activeLivingEnemies.Clear();

            onMapChanged?.Invoke(activeMapId);
            Debug.Log($"[EnemySpawner] Active map changed to '{activeMapId}' with {activeWaves.Count} waves.");
        }

        /// <summary>
        /// Loads the wave data for the specified map identifier, by <see cref="MapWaveLookup"/>'s
        /// priority: the override asset, then the asset list, then the inline list.
        /// </summary>
        private void LoadMapConfiguration(string mapId)
        {
            List<Wave> found = MapWaveLookup.Find(activeMapConfigOverride, scriptableMapConfigs, inlineMapConfigs, mapId);
            if (found == null)
            {
                activeWaves = new List<Wave>();
                Debug.LogWarning($"[EnemySpawner] No wave configuration found for Map ID '{mapId}'.");
                return;
            }

            activeWaves = found;
        }

        /// <summary>
        /// Prompts the spawner to begin the next wave.
        /// Can be wired to a UI Button OnClick() or invoked by game managers.
        /// </summary>
        [ContextMenu("Start Next Wave")]
        public void StartNextWave()
        {
            if (currentState == SpawnerState.Spawning)
            {
                Debug.LogWarning("[EnemySpawner] Cannot start next wave while a wave is currently spawning.");
                return;
            }

            if (currentState == SpawnerState.Completed)
            {
                Debug.Log("[EnemySpawner] All waves for this map have already been completed.");
                return;
            }

            if (activeWaves == null || activeWaves.Count == 0)
            {
                Debug.LogWarning($"[EnemySpawner] No waves configured for active map '{activeMapId}'.");
                return;
            }

            if (currentWaveIndex >= activeWaves.Count)
            {
                currentState = SpawnerState.Completed;
                onAllWavesCompleted?.Invoke();
                return;
            }

            // Optional check if living enemies must be defeated first
            if (requireEnemiesClearedBeforeNextWave && activeLivingEnemies.Count > 0)
            {
                // Filter out destroyed objects
                activeLivingEnemies.RemoveAll(enemy => enemy == null);
                if (activeLivingEnemies.Count > 0)
                {
                    Debug.LogWarning($"[EnemySpawner] Cannot start next wave: {activeLivingEnemies.Count} enemies still alive.");
                    return;
                }
            }

            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
            }

            spawnCoroutine = StartCoroutine(SpawnWaveRoutine(currentWaveIndex));
        }

        /// <summary>
        /// Coroutine that iterates through the current wave's enemies one by one,
        /// waiting for the specified delay between each spawn.
        /// Pauses at the end of the wave and waits for the next prompt.
        /// </summary>
        private IEnumerator SpawnWaveRoutine(int waveIdx)
        {
            currentState = SpawnerState.Spawning;
            Wave wave = activeWaves[waveIdx];

            Debug.Log($"[EnemySpawner] Starting {wave.waveName} ({waveIdx + 1}/{activeWaves.Count}) with {wave.enemies.Count} enemies.");
            onWaveStarted?.Invoke(waveIdx + 1, activeWaves.Count);

            for (int i = 0; i < wave.enemies.Count; i++)
            {
                EnemySpawnEntry entry = wave.enemies[i];

                if (entry != null && entry.enemyPrefab != null)
                {
                    SpawnEnemy(entry);
                }
                else
                {
                    Debug.LogWarning($"[EnemySpawner] Wave '{wave.waveName}' entry #{i} has no enemy prefab assigned.");
                }

                // Pause before the next enemy (or wait interval after the last enemy)
                if (entry != null && entry.delayBeforeNext > 0f)
                {
                    yield return new WaitForSeconds(entry.delayBeforeNext);
                }
            }

            // Wave spawning complete: mark wave as finished and pause
            int completedWaveNumber = waveIdx + 1;
            currentWaveIndex++;

            Debug.Log($"[EnemySpawner] Finished spawning {wave.waveName}. Pausing spawner until next wave is prompted.");
            onWaveCompleted?.Invoke(completedWaveNumber);

            if (currentWaveIndex >= activeWaves.Count)
            {
                currentState = SpawnerState.Completed;
                Debug.Log("[EnemySpawner] All waves completed for active map!");
                onAllWavesCompleted?.Invoke();
            }
            else
            {
                // Pause and wait for prompt (StartNextWave)
                currentState = SpawnerState.WaitingForNextWave;
            }

            spawnCoroutine = null;
        }

        /// <summary>Instantiates a wave entry's enemy at its spawn point and tracks it.</summary>
        private void SpawnEnemy(EnemySpawnEntry entry)
        {
            Transform spawnPoint = entry.spawnPointOverride != null ? entry.spawnPointOverride : defaultSpawnPoint;
            Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : Vector3.zero;
            Quaternion spawnRotation = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;
            Spawn(entry.enemyPrefab, spawnPosition, spawnRotation, false);
        }

        /// <summary>
        /// Puts an enemy into the run at the default spawn point, snapped to the start of the route.
        /// The one way to add an enemy outside a wave (hordes, specials, cheats), so the living
        /// count and <see cref="onEnemySpawned"/> stay right.
        /// </summary>
        public GameObject Spawn(GameObject prefab)
        {
            Vector3 position = defaultSpawnPoint != null ? defaultSpawnPoint.position : transform.position;
            return Spawn(prefab, position, Quaternion.identity, true);
        }

        /// <summary>Puts an enemy into the run at a position, optionally snapped to the route start, and tracks it.</summary>
        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, bool snapToStart)
        {
            if (prefab == null)
            {
                return null;
            }

            GameObject enemy = Instantiate(prefab, position, rotation, enemyContainer);
            if (snapToStart)
            {
                var mover = enemy.GetComponent<EnemyMover>();
                if (mover != null)
                {
                    mover.SnapToStart();
                }
            }

            Track(enemy);
            return enemy;
        }

        /// <summary>Counts an enemy made elsewhere (a splitter's halves) as living and announces it.</summary>
        public void Track(GameObject enemy)
        {
            if (enemy == null)
            {
                return;
            }

            activeLivingEnemies.Add(enemy);
            onEnemySpawned?.Invoke(enemy);
        }

        /// <summary>
        /// Drops an enemy from the living count. Called from <see cref="EnemyHealth.Defeated"/>; also
        /// for anything that removes an enemy some other way.
        /// </summary>
        public void NotifyEnemyDefeated(GameObject enemy)
        {
            if (enemy != null)
            {
                activeLivingEnemies.Remove(enemy);
            }
        }

        /// <summary>
        /// Stops any spawning and moves to the given wave (0-based) without reloading the map.
        /// The wave does not start until <see cref="StartNextWave"/> is called.
        /// </summary>
        public void RestartFromWave(int waveIndex)
        {
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }

            currentWaveIndex = Mathf.Clamp(waveIndex, 0, Mathf.Max(0, TotalWaves - 1));
            currentState = currentWaveIndex == 0 ? SpawnerState.Idle : SpawnerState.WaitingForNextWave;
            activeLivingEnemies.Clear();
        }

        /// <summary>
        /// Immediately stops any active wave spawning.
        /// </summary>
        public void StopSpawning()
        {
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }

            currentState = SpawnerState.Idle;
        }
    }
}
