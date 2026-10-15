# Technical Documentation: MapWaveDataSO.cs

**Target Unity Version**: Unity 6 (`6000.6.0f1`)  
**Context**: 2D Tower Defense Game  
**File Location**: `Assets/_Game/Features/Enemies/Scripts/EnemySpawner/MapWaveDataSO.cs`  
**Consumer Files**: `EnemySpawner.cs`, `MapWaveLookup.cs` (same folder)

---

## 1. Overview & Purpose

<!-- prose:overview -->
`MapWaveDataSO.cs` defines the **data schema** for wave spawning in the fork: four types in the namespace `HoldTheHill.Features.Enemies`, three of them plain `[Serializable]` classes and one `ScriptableObject`. It holds no update logic and no spawning code. The spawner reads it; `MapWaveLookup` picks which one to read. A designer edits an asset, not a scene.

### Why use ScriptableObjects for Waves?
- **Scene Independence**: a wave list is an `.asset` file in the Project window, not a field on a scene object, so it survives scene rebuilds and does not sit in a scene merge.
- **Easy Balancing & Tuning**: enemy order, counts and delays change in the Inspector with no code change.
- **Variant Configurations**: several assets can share a map id or carry different ones, and the spawner swaps between them through its override slot or its asset list.
<!-- /prose -->

---

## 2. Data Structures Reference

<!-- prose:structure -->
`MapWaveDataSO` and `MapWaveData` each hold a `mapId` and a list of `Wave`; a `Wave` holds a list of `EnemySpawnEntry`. The class diagram in the Indev document still applies unchanged.

### 1. `EnemySpawnEntry`
Represents a single enemy spawn event in a wave.

```csharp
[Serializable]
public class EnemySpawnEntry
{
    public GameObject enemyPrefab;
    public float delayBeforeNext = 1f;
    public Transform spawnPointOverride;
}
```

| Field | Type | Attributes | Description |
| :--- | :--- | :--- | :--- |
| `enemyPrefab` | `GameObject` | `[Tooltip]` | The 2D enemy prefab to instantiate. An entry with no prefab is skipped with a warning; its delay still runs. |
| `delayBeforeNext` | `float` | `[Tooltip]`, `[Min(0f)]` | Delay in seconds after this enemy is spawned before spawning the next enemy. Default `1f`; a value of 0 means no wait at all. |
| `spawnPointOverride` | `Transform` | `[Tooltip]` | Optional custom spawn point. If null, the spawner's default spawn point will be used. Only a scene object can fill it on an inline config; an asset cannot hold a scene reference. |

---

### 2. `Wave`
Represents a wave containing a sequential list of enemies.

```csharp
[Serializable]
public class Wave
{
    public string waveName = "Wave 1";
    public List<EnemySpawnEntry> enemies = new List<EnemySpawnEntry>();
}
```

| Field | Type | Default | Description |
| :--- | :--- | :--- | :--- |
| `waveName` | `string` | `"Wave 1"` | Name or identifier for this wave (e.g. 'Wave 1', 'Boss Wave'). Used in the spawner's log lines. |
| `enemies` | `List<EnemySpawnEntry>` | `new List()` | Sequential list of enemies to spawn one at a time in this wave. |

---

### 3. `MapWaveData`
A serializable container for a map and its waves, for **inline configuration** in the spawner's Inspector (`inlineMapConfigs`).

```csharp
[Serializable]
public class MapWaveData
{
    public string mapId = "Map_01";
    public List<Wave> waves = new List<Wave>();
}
```

| Field | Type | Default | Description |
| :--- | :--- | :--- | :--- |
| `mapId` | `string` | `"Map_01"` | Unique ID or name for this map (e.g., 'Grasslands', 'Level1', or scene name). Matched without regard to case. |
| `waves` | `List<Wave>` | `new List()` | List of delineated waves for this map. |

---

### 4. `MapWaveDataSO`
The `ScriptableObject` asset that stores a map's waves as a project file. `Map_01.asset` in the same folder is the one in use: map id `Map_01`, one wave named `Wave 1` with a single `Triangle.prefab` entry and a delay of 0.

```csharp
[CreateAssetMenu(fileName = "NewMapWaveConfig", menuName = "Tower Defense/Map Wave Config", order = 1)]
public class MapWaveDataSO : ScriptableObject
{
    public string mapId = "Map_01";
    public List<Wave> waves = new List<Wave>();
}
```

- **Fields**: `mapId` (tooltip: unique identifier for this map, e.g., 'Grasslands', 'Desert', or matching Scene name) and `waves` (tooltip: delineated waves for this map), same defaults as `MapWaveData`.
- **`[CreateAssetMenu]`**: adds **Assets > Create > Tower Defense > Map Wave Config**, default file name `NewMapWaveConfig`.
- **Inheritance**: `UnityEngine.ScriptableObject`, so it is saved as an `.asset` file with its own `.meta`.
<!-- /prose -->

---

## 3. Why `[Serializable]` is Crucial

`Wave`, `EnemySpawnEntry` and `MapWaveData` are plain C# classes. Unity only writes a custom class into a scene, prefab or `.asset` file, and only draws it in the Inspector as a foldout with lists and drag slots, when it carries `[System.Serializable]`. Without it the fields would exist in memory and nowhere else. `MapWaveDataSO` needs no attribute; being a `ScriptableObject` is enough.

---

## 4. Workflow & Step-by-Step Guide

<!-- prose:setup -->
### Step 1: Create a Map Wave Asset
1. In the Project window, go to the folder where the map's data lives (the current asset sits in `Assets/_Game/Features/Enemies/Scripts/EnemySpawner/`).
2. **Right-click** in empty space &rarr; **Create > Tower Defense > Map Wave Config**.
3. Name the file after the map (for example `Map_02`).

### Step 2: Configure Map ID & Waves
1. Select the asset.
2. In the **Inspector**:
   - Set **Map Id** to the id the spawner will ask for, or to the scene name if the spawner has **Auto Detect Map From Scene** on. Case does not matter.
   - Under **Waves**, click **`+`** to add a wave and set **Wave Name**.

### Step 3: Populate Enemy Sequence
1. Expand the wave's **Enemies** list and click **`+`** for each enemy.
2. Drag an enemy prefab (one carrying `EnemyHealth` and `EnemyMover`) into **Enemy Prefab**.
3. Set **Delay Before Next** in seconds; it cannot go below 0, and 0 spawns the next entry on the same frame.
4. *(Optional)* **Spawn Point Override** takes a Transform. On an asset it can only be another asset's Transform, so in practice leave it empty and use an inline config on the spawner when a wave needs a scene spawn point.

### Step 4: Hooking into `EnemySpawner`
* **Direct Single-Map Approach**: drag the asset into **Active Map Config Override** on the scene's `EnemySpawner`. It wins regardless of map id.
* **Multi-Map Asset Library**: add it to **Scriptable Map Configs**; the spawner picks the first whose id matches `activeMapId`.
* **Inline**: for a throwaway test, add a `MapWaveData` under **Inline Map Configs** instead; it is checked last.
<!-- /prose -->

---

## 5. Against the Indev version

<!-- prose:changes -->
| Indev | Fork | What a caller changes |
| :--- | :--- | :--- |
| `Assets/Scripts/EnemySpawner/MapWaveDataSO.cs`, global namespace | `Assets/_Game/Features/Enemies/Scripts/EnemySpawner/MapWaveDataSO.cs`, namespace `HoldTheHill.Features.Enemies` | Add `using HoldTheHill.Features.Enemies;`. |
| Lookup is private code inside `EnemySpawner` | `MapWaveLookup.Find(overrideConfig, assets, inline, mapId)` in `MapWaveLookup.cs`, plain C# returning `List<Wave>` or `null`, covered by EditMode tests | Nothing in the Inspector; code or tests that need the same choice call `MapWaveLookup.Find` directly. |

The data itself is unchanged: the four types, their fields, defaults, tooltips, the `[Min(0f)]` on `delayBeforeNext` and the `[CreateAssetMenu]` path are the same as on Indev. The only differences are where the file lives, which namespace it is in, and that the spawner now reads it through `MapWaveLookup` rather than through its own private search.
<!-- /prose -->
