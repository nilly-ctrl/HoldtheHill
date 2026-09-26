using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a single enemy spawn event in a wave.
/// </summary>
[Serializable]
public class EnemySpawnEntry
{
    [Tooltip("The 2D enemy prefab to instantiate.")]
    public GameObject enemyPrefab;

    [Tooltip("Delay in seconds after this enemy is spawned before spawning the next enemy.")]
    [Min(0f)]
    public float delayBeforeNext = 1f;

    [Tooltip("Optional custom spawn point. If null, the spawner's default spawn point will be used.")]
    public Transform spawnPointOverride;
}

/// <summary>
/// Represents a wave containing a sequential list of enemies.
/// </summary>
[Serializable]
public class Wave
{
    [Tooltip("Name or identifier for this wave (e.g. 'Wave 1', 'Boss Wave').")]
    public string waveName = "Wave 1";

    [Tooltip("Sequential list of enemies to spawn one at a time in this wave.")]
    public List<EnemySpawnEntry> enemies = new List<EnemySpawnEntry>();
}

/// <summary>
/// Serializable data structure representing a map and its associated waves.
/// Can be configured directly inside EnemySpawner inspector.
/// </summary>
[Serializable]
public class MapWaveData
{
    [Tooltip("Unique ID or name for this map (e.g., 'Grasslands', 'Level1', or scene name).")]
    public string mapId = "Map_01";

    [Tooltip("List of delineated waves for this map.")]
    public List<Wave> waves = new List<Wave>();
}

/// <summary>
/// ScriptableObject asset to configure and store map wave configurations in the Project.
/// Create via: Assets > Create > Tower Defense > Map Wave Config
/// </summary>
[CreateAssetMenu(fileName = "NewMapWaveConfig", menuName = "Tower Defense/Map Wave Config", order = 1)]
public class MapWaveDataSO : ScriptableObject
{
    [Tooltip("Unique identifier for this map (e.g., 'Grasslands', 'Desert', or matching Scene name).")]
    public string mapId = "Map_01";

    [Tooltip("Delineated waves for this map.")]
    public List<Wave> waves = new List<Wave>();
}
