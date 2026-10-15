# Technical Documentation: TowerData.cs

**Target Unity Version**: Unity 6 (`6000.6.0f1`)  
**Context**: 2D Tower Defense Game  
**File Location**: `Assets/_Game/Features/Towers/Data/TowerData.cs`  
**Consumer Files**: `TowerCommands.cs`, `TowerPlacer.cs`, `PlacementGhost.cs`, `TowerArchetypes.cs` (all under `Features/Towers/Scripts/`), `TowerShopManager.cs`, `TowerCardUI.cs` (under `Features/UI/`)

---

## 1. Overview & Architecture

<!-- prose:overview -->
`TowerData.cs` is one `ScriptableObject` in the namespace `HoldTheHill.Features.Towers`: one asset per buildable tower. It holds what the shop, the build bar and the placement ghost need **before a tower exists**: a name, icons, a base cost, the resource it is paid in, a role, the prefab to place and a ghost sprite. It holds no combat numbers. Range, fire interval and weapons live on the `Tower` prefab it points at, which is covered in [Towers_Documentation.md](Towers_Documentation.md). It has no update logic and no Unity messages.

### Key Architectural Pillars
- **Build data only**: the class remarks say the asset "only holds what the build UI needs before a tower exists"; `Range` is read through from the prefab rather than stored twice.
- **Read-only surface**: every field is `private [SerializeField]` with a getter and no setter, so only the Inspector, `SerializedObject` or an editor builder writes it.
- **Adam's asset format still loads**: the three original shop fields carry `[FormerlySerializedAs]` with Adam Montgomery's names (`towerName`, `towerIcon`, `honeydewCost`), and the script's `.meta` GUID is the one his class used, so his assets open as this class.
- **Costs and upgrades stay outside**: discounts, wallet checks and upgrade prices belong to `TowerCommands` and `TowerPlacer`; this asset carries only the base build cost.
<!-- /prose -->

---

## 2. State Machine / Data Structures

<!-- prose:structure -->
There is no state machine and there are no nested `[Serializable]` types: no upgrade tiers, no modifier lists. Upgrade prices are an `int[]` on `TowerPlacer` (`_upgradeCosts`, default `{ 150, 300 }`), the same for every tower. The one enum the asset draws is `TowerArchetype` (namespace `HoldTheHill.Features.Combat`), stored as an int in YAML:

| Value | Name | Summary from the enum |
| :--- | :--- | :--- |
| 0 | `Unspecified` | Not set: ask the tower's weapon, or treat it as a Gunner. |
| 1 | `Gunner` | Fires single shots at one target: straight, homing or bouncing. |
| 2 | `Artillery` | Area damage at a point, slowly delivered or laid ahead of time: mortars, mines. |
| 3 | `Arc` | Hits at once and holds on or jumps between targets: chain lightning, beams. |
| 4 | `Controller` | Acts on everything in range to change how enemies move: slows, knockback. |
| 5 | `Brawler` | Fights at close range: the worker, soldier and major castes. |
| 6 | `Support` | Does no damage; heals or repairs: the nurse. |
| 7 | `Summoner` | Makes units that fight for it; its power is their number: the swarm nest. |
<!-- /prose -->

---

## 3. Inspector Fields Reference

<!-- generated:fields -->
`[CreateAssetMenu(fileName = "New Tower", menuName = "Hold The Hill/Tower Data")]`.

### Shop Display
| Field | Type | Default | Description |
| :--- | :--- | :--- | :--- |
| `_displayName` | `string` | `null` | Name on the shop card, the build menu and the tower card. `[FormerlySerializedAs("towerName")]`. |
| `_icon` | `Sprite` | `null` | Icon on the shop card. `[FormerlySerializedAs("towerIcon")]`. |
| `_iconName` | `string` | `null` | Icon name, for a UI that looks its icons up by name (the graybox build bar). |

### Cost
| Field | Type | Default | Description |
| :--- | :--- | :--- | :--- |
| `_baseCost` | `int` | `100` | Cost to build, before any discount. `[Min(0)]`, `[FormerlySerializedAs("honeydewCost")]`. |
| `_costResource` | `ResourceData` | `null` | What the cost is paid in. Empty means the run's main resource. |

### Placement
| Field | Type | Default | Description |
| :--- | :--- | :--- | :--- |
| `_archetype` | `TowerArchetype` | `Unspecified` | The role it plays. Unspecified takes it from the prefab's weapon, or Gunner if it has none. |
| `_prefab` | `Tower` | `null` | The tower placed when this is built. |
| `_ghostSprite` | `Sprite` | `null` | Sprite shown under the cursor while placing. Empty shows the range ring only. |
<!-- /generated -->

---

## 4. Public API Reference

<!-- generated:api -->
### Properties
| Property | Type | Description |
| :--- | :--- | :--- |
| `DisplayName` | `string` | Returns `_displayName`. No `/// <summary>`. |
| `Icon` | `Sprite` | Returns `_icon`. No `/// <summary>`. |
| `IconName` | `string` | Returns `_iconName`. No `/// <summary>`. |
| `BaseCost` | `int` | Returns `_baseCost`. No `/// <summary>`. |
| `CostResource` | `ResourceData` | What the cost is paid in, or null for the run's main resource. |
| `Archetype` | `TowerArchetype` | The tower's role: set on the asset, else its weapon's, else Gunner (a tower with only a projectile). Computed on every read: if `_archetype` is not `Unspecified` it wins; otherwise `GetComponent<TowerWeapon>()` on the prefab, and its `Archetype` if that is not `Unspecified`; otherwise `Gunner`. |
| `Prefab` | `Tower` | Returns `_prefab`. No `/// <summary>`. |
| `GhostSprite` | `Sprite` | Returns `_ghostSprite`. No `/// <summary>`. |
| `Range` | `float` | The prefab's reach in world units, or 0 with no prefab. Reads `Tower.Range` live, so it follows the prefab. |

### C# Events
None.

### Public Methods
None. The class declares only fields and properties.
<!-- /generated -->

---

## 5. Lifecycle & Execution Flow

<!-- prose:lifecycle -->
- **No Unity messages**: no `Awake`, `OnEnable`, `OnValidate` or `OnDestroy`. Nothing is cached; every property reads the serialized field, or the prefab, at the moment it is asked.
- **Computed reads**: `Archetype` calls `GetComponent<TowerWeapon>()` on the prefab each time it is read with `_archetype` left `Unspecified`. `Range` reads `Prefab.Range` each time.
- **Behaviour pinned by `TowerDataTests.cs`** (EditMode): with no prefab, `Prefab` is null, `Range` is 0 and `Archetype` is `Gunner`; an archetype set on the asset (`Support`) wins; `Range` returns the prefab's `_range` (3.5 in the test); `CostResource` is null when unset. The test named `TheOldShopFields_StillRead_ThroughTheirNewNames` writes `_displayName` and `_baseCost` (the new names), so the `[FormerlySerializedAs]` path itself is not exercised by it.
<!-- /prose -->

---

## 6. Integration With Other Systems

<!-- prose:integration -->
### Runtime readers and tests (`Assets/_Game`)
- **`TowerCommands.cs`**: `CostOf` is `BaseCost` times `CostMultiplier`; `CanAfford` and `TryBuild` check and spend in `CostResource`; `TryBuild` returns null with no `Prefab`; `Place` instantiates `Prefab` and names the tower `DisplayName`. Upgrades are paid in the main resource, not `CostResource`.
- **`TowerPlacer.cs`**: holds the catalogue as `[SerializeField] List<TowerData> _towers` ("The towers that can be built, in build bar order"), exposed as the virtual `Towers`; wraps `BuildAtMenu`, `CostOf`, `CanAfford` and `Place`; fills `Preview` and calls the ghost while placing.
- **`PlacementGhost.cs`**, **`PlacementPreview.cs`**: `Show` draws `GhostSprite` at the cell and a range ring of `max(Range, 0.5)`; the preview struct's `Tower` field holds the entry under the cursor.
- **`TowerArchetypes.cs`**: `Of(tower, catalog)` falls back to the catalogue entry whose `DisplayName` equals the placed tower's name, then reads its `Archetype`.
- **`TowerShopManager.cs`**, **`TowerCardUI.cs`** (`Features/UI/`): the shop makes one card per entry of `List<TowerData> availableTowers`; `SetupCard` shows `DisplayName`, `BaseCost` and `Icon`, and the click only logs.
- **`TowerDataTests.cs`**: the four tests listed in section 5.
- **`TowerCommandsTests.cs`**, **`TowerPlacerTests.cs`**: build a `TowerData` with `CreateInstance` as the thing to build, pay for and place. **`GrayboxTowerCatalogTests.cs`** (sandbox) reads catalogue entries.

### Graybox readers (`Assets/_Sandbox/nilly-ctrl`)
- **`GrayboxTowerCatalog.cs`**, **`GrayboxTowerPlacer.cs`**: a `ScriptableObject` list of `TowerData` in build bar order (slot i uses hotkey i); the placer overrides `Towers` with it.
- **`GrayboxBuildBar.cs`**, **`GrayboxBuildSlot.cs`**: the bar compares each entry's `Archetype` with the one before it to split the row by role; a slot shows `IconName` and `DisplayName`.
- **`GrayboxBuildMenu.cs`**, **`GrayboxBuildOption.cs`**: one option per catalogue entry with `IconName`, `DisplayName` and the placer's `CostOf` and `CanAfford`.
- **`GrayboxPlacementLabel.cs`**: shows the `DisplayName` of the tower being placed.
- **`DevBuiltinCommands.cs`**, **`DevReplayPlayer.cs`**, **`DevTuningPanel.cs`**: console, replay and tuning tools that find an entry by `DisplayName` and read `BaseCost`, `CostResource` or `Prefab`.
- **`GrayboxTowerBuilder.cs`**, **`GrayboxSpeciesBuilder.cs`**, **`ContentWizardUnitBuilder.cs`**: editor builders that create the assets and write the fields through `SerializedObject`.
- **`BalanceSheetKinds.cs`**: lists `TowerData` assets in the balance sheet and edits `_displayName`, `_iconName`, `_baseCost` and `_archetype`.
- **`DevTuningWriteBackEditor.cs`**: names the type only in a comment, saying tower numbers live on the prefab, not here.
- **`BalanceInputBuilder.cs`**: reads `DisplayName`, `BaseCost`, `Prefab` and `Range` into the balance simulation's input.
- **`CatalogMembershipCheck.cs`**, **`DataAssetCheck.cs`**: flag `TowerData` assets that no catalogue lists; error on an empty `DisplayName` or no `Prefab`, check `IconName`, warn on `BaseCost <= 0`, and check the prefab's range and fire interval.
<!-- /prose -->

---

## 7. Setup & Integration Guide

<!-- prose:setup -->
### Existing assets
Seventeen assets on this branch name the `TowerData` script GUID (`7d7d87ffd3d324c4188af7248f9e3da8`), all under `Assets/_Sandbox/nilly-ctrl/Graybox/Data/Towers/`. None sets `_icon`, so `Icon` is empty on all of them; the build bar uses `IconName` instead.

| Asset | Display name | Base cost | Archetype |
| :--- | :--- | :--- | :--- |
| `TowerLinearData` | Bullet Tower | 100 | Gunner |
| `TowerHomingData` | Homing Tower | 150 | Gunner |
| `TowerRicochetData` | Ricochet Tower | 175 | Gunner |
| `TowerMortarData` | Mortar Tower | 200 | Artillery |
| `TowerMineLayerData` | Mine Layer | 250 | Artillery |
| `TowerChainData` | Chain Lightning | 200 | Arc |
| `TowerBeamData` | Beam Tower | 225 | Arc |
| `TowerFrostAuraData` | Frost Aura | 225 | Controller |
| `TowerKnockbackData` | Knockback Pulse | 200 | Controller |
| `TowerWorkerData` | Worker | 75 | Brawler |
| `TowerSoldierData` | Soldier | 125 | Brawler |
| `TowerMajorData` | Major | 200 | Brawler |
| `TowerNurseData` | Nurse | 150 | Support |
| `TowerOrbitData` | Swarm Nest | 175 | Summoner |
| `FireAnt/TowerLinear_FireAntData` | Fire Bullet Tower | 100 | not set in YAML |
| `FireAnt/TowerSoldier_FireAntData` | Fire Soldier | 125 | not set in YAML |
| `FireAnt/TowerWorker_FireAntData` | Fire Worker | 75 | not set in YAML |

They are listed by `Graybox/Data/GrayboxTowerCatalog.asset` and `Graybox/Data/Species/FireAntCatalog.asset`. Their YAML still carries an old `m_EditorClassIdentifier` (`GrayboxTowerData`); Unity loads by the script GUID. Adam's six test assets in `Assets/_Sandbox/1103-Montgomery-Adam/Testing/` (`Tower1` to `Tower6`) also carry this GUID; see section 8.

### Step 1: Create the asset
Make the `Tower` prefab first ([Towers_Documentation.md](Towers_Documentation.md), section 8). Then, in the Project window, **right-click > Create > Hold The Hill > Tower Data** and rename `New Tower` after the tower.

### Step 2: Fill it in
1. **Display Name**: the name on the card, the menu and the placed tower's GameObject.
2. **Icon** for a shop card; **Icon Name** for the graybox build bar.
3. **Base Cost**; leave **Cost Resource** empty to pay in the run's main resource.
4. **Prefab**: the `Tower` prefab. Leave **Archetype** at `Unspecified` if the prefab has a weapon that names its role; set it for projectile-only towers.
5. **Ghost Sprite**: optional; without it the ghost is the range ring only.

### Step 3: Offer it
Add the asset to `TowerPlacer`'s **Towers** list, a `GrayboxTowerCatalog`, or a `TowerShopManager`'s **Available Towers**. List order is build bar order.
<!-- /prose -->

---

## 8. Against the Indev version

<!-- prose:changes -->
Indev has `unity/Assets/_Game/Features/UI/TowerListScripts/TowerData.cs`, Adam Montgomery's shop card data (10.9.2026), read by `TowerShopManager.cs` in the same folder and `TowerCardUI.cs` in `UI/TowerCardScript/`.

| | Indev `TowerData` | Fork `TowerData` |
| :--- | :--- | :--- |
| File | `Features/UI/TowerListScripts/TowerData.cs` | `Features/Towers/Data/TowerData.cs` |
| Namespace | global | `HoldTheHill.Features.Towers` |
| `.meta` GUID | `7d7d87ffd3d324c4188af7248f9e3da8` | same |
| Name | `public string towerName` | `_displayName`, read as `DisplayName` |
| Cost | `public int honeydewCost` | `_baseCost` (default 100, `[Min(0)]`), read as `BaseCost`; plus `CostResource` |
| Icon | `public Sprite towerIcon` | `_icon`, read as `Icon`; plus `IconName` |
| Prefab | commented out (`towerPrefab`, waiting for the Tower namespace) | `_prefab` (`Tower`), plus `GhostSprite`, `Archetype` and `Range` |
| Access | public fields | private serialized fields, get-only properties |
| Create menu | `Hold The Hill/Tower Data`, `New Tower` | same |
| Read by | `TowerShopManager`, `TowerCardUI` | `TowerShopManager`, `TowerCardUI`, `TowerCommands`, `TowerPlacer`, `PlacementGhost`, `TowerArchetypes`, graybox and editor tools (section 6) |

**How a shop or card reads the fork asset**: add `using HoldTheHill.Features.Towers;` and read `DisplayName`, `BaseCost` and `Icon` instead of `towerName`, `honeydewCost` and `towerIcon`. The fork's copies of `TowerShopManager.cs` and `TowerCardUI.cs` already do exactly this and nothing else. A card that should show the discounted price would call `TowerPlacer.CostOf(data)`; one that should build would call `TowerPlacer.BuildAtMenu(data)` or `TowerCommands.TryBuild`. Neither card does yet; its click still logs "Placement logic pending indev merge."

**Assets**: because the GUID matches and the old field names are kept through `[FormerlySerializedAs]`, an Indev asset such as Adam's `Tower1` to `Tower6` loads as the fork class with its name, cost and icon intact.

**Two classes named `TowerData`**: this branch still has Adam's sandbox copy, `Assets/_Sandbox/1103-Montgomery-Adam/Scripts/TowerData.cs`, a global-namespace class with the Indev fields and the same create-menu path. It has no `.meta` in git, and that folder has no `.asmdef`. Indev's global `TowerData` is the same class under `Features/UI/TowerListScripts/`. Because a global `TowerData` exists, the fork's editor scripts under `_Sandbox/nilly-ctrl/Editor/` (the builders, authoring and validation tools in section 6) write the type fully qualified as `HoldTheHill.Features.Towers.TowerData`.
<!-- /prose -->
