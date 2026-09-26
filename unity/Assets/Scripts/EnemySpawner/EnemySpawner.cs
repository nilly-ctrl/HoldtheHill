using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// Controls enemy spawning for a 2D Tower Defense game.
/// Handles map-specific wave configurations, sequential enemy spawning,
/// and pauses at the end of each wave until prompted to continue.
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
    /// Loads the wave data for the specified map identifier.
    /// Priority:
    /// 1. activeMapConfigOverride (if assigned)
    /// 2. scriptableMapConfigs matching mapId
    /// 3. inlineMapConfigs matching mapId
    /// </summary>
    private void LoadMapConfiguration(string mapId)
    {
        activeWaves = new List<Wave>();

        // 1. Check direct override
        if (activeMapConfigOverride != null)
        {
            activeWaves = activeMapConfigOverride.waves;
            return;
        }

        // 2. Search ScriptableObject list
        if (scriptableMapConfigs != null)
        {
            foreach (var config in scriptableMapConfigs)
            {
                if (config != null && string.Equals(config.mapId, mapId, System.StringComparison.OrdinalIgnoreCase))
                {
                    activeWaves = config.waves;
                    return;
                }
            }
        }

        // 3. Search Inline Inspector configurations
        if (inlineMapConfigs != null)
        {
            foreach (var inlineConfig in inlineMapConfigs)
            {
                if (inlineConfig != null && string.Equals(inlineConfig.mapId, mapId, System.StringComparison.OrdinalIgnoreCase))
                {
                    activeWaves = inlineConfig.waves;
                    return;
                }
            }
        }

        Debug.LogWarning($"[EnemySpawner] No wave configuration found for Map ID '{mapId}'.");
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

    /// <summary>
    /// Instantiates an enemy GameObject and tracks it.
    /// </summary>
    private void SpawnEnemy(EnemySpawnEntry entry)
    {
        Transform spawnPoint = entry.spawnPointOverride != null ? entry.spawnPointOverride : defaultSpawnPoint;
        Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        Quaternion spawnRotation = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

        GameObject spawnedEnemy = Instantiate(entry.enemyPrefab, spawnPosition, spawnRotation, enemyContainer);
        activeLivingEnemies.Add(spawnedEnemy);

        onEnemySpawned?.Invoke(spawnedEnemy);
    }

    /// <summary>
    /// Allows external enemy logic (or health components) to notify the spawner when an enemy dies.
    /// </summary>
    public void NotifyEnemyDefeated(GameObject enemy)
    {
        if (activeLivingEnemies.Contains(enemy))
        {
            activeLivingEnemies.Remove(enemy);
        }
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
