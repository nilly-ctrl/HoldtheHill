# Hold the Hill

A single-player 2D hybrid tower defense by **Turbulent Towers Studio**: control your character and build defenses to hold the hill against waves.

## Docs

- **[Devlog](docs/DEVLOG.md)** — what we worked on, what we decided, and why. Add an entry when you finish a task.
- **[AI use log](docs/AI_DEVLOG.md)** — short disclosure of AI-assisted work (our instructor asks for it). Add one row whenever AI helps.

## Setup (do this once)

1. **Install Unity 6000.6.0f1 exactly** through Unity Hub. Opening the project in any other version rewrites dozens of files and causes conflicts for everyone.
2. **Install Git LFS.** Art, audio, and other big files are stored with LFS. Without it you get tiny placeholder files instead of real assets.
   - Mac: `brew install git-lfs`, then `git lfs install`
   - Windows: included with [Git for Windows](https://git-scm.com/download/win); run `git lfs install`
   - GitHub Desktop already includes it.
3. **Clone the repo**, then open the `unity` folder in Unity Hub (not the repo root).
4. **Set up Unity scene merging** (recommended). Run this inside the repo, using your Unity path:
   - Mac:
     ```
     git config merge.unityyamlmerge.name "Unity SmartMerge"
     git config merge.unityyamlmerge.driver '"/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/Helpers/UnityYAMLMerge" merge -p %O %B %A %A'
     git config merge.unityyamlmerge.recursive binary
     ```
   - Windows:
     ```
     git config merge.unityyamlmerge.name "Unity SmartMerge"
     git config merge.unityyamlmerge.driver '"C:/Program Files/Unity/Hub/Editor/6000.6.0f1/Editor/Data/Tools/UnityYAMLMerge.exe" merge -p %O %B %A %A'
     git config merge.unityyamlmerge.recursive binary
     ```
   Without this, scene conflicts fall back to normal text merging. Nothing breaks, it's just messier.

## Where things go

We organize **by feature**: everything for one system (scripts, prefabs, sprites, data) lives together, so it's obvious who's working on what and merges stay small.

```
unity/Assets/
  _Game/                  all of our game
    Core/                 shared systems every feature can use
      Audio/  Bootstrap/  Events/  GameFeel/  Pooling/  Save/  Utilities/
    Features/             one folder per game system
      Player/             player character, camera
      GameFlow/           hill health, build phase <-> wave phase, win/lose
      Enemies/            enemy types, waves, difficulty scaling, pathfinding
      Towers/             towers and buildings, grid placement
      Combat/             targeting, damage, status effects, power-ups
      Economy/            currency, harvestables, loot drops
      Progression/        upgrade tree, stats, achievements
      Levels/             level data, level select, tutorial
      UI/                 HUD, menus, settings
      DebugTools/         cheat console and test helpers (development builds only)
    Shared/               assets used by 2+ features
      Art/Tilesets/  Audio/Music/  VFX/  Fonts/  Materials/
    Scenes/               Bootstrap, UI, and Levels/<LevelName>/
    Editor/               custom Unity editor tools
    Tests/                EditMode/ and PlayMode/ automated tests
  _Sandbox/<github-username>/   your personal test area - only edit your own
  ThirdParty/             Asset Store imports, kept apart from our code
  Settings/               render pipeline + input settings (leave alone)
```

**Inside a feature**, only create the subfolders you need:

```
Features/Towers/
  Scripts/      ArcherTower.cs, TowerPlacer.cs
  Prefabs/      ArcherTower.prefab, ArcherTower_Tier2.prefab (variant)
  Art/          ArcherTower.aseprite
  Data/         ArcherTowerData.asset (ScriptableObject stats)
  Animations/  Audio/
```

**Rules of thumb**
- **Used by one feature?** Put it in that feature. **Used by two or more?** Move it to `Shared/` (or `Core/` for code).
- **`Core/` never depends on `Features/`.** Features can use Core, not the other way round.
- **ScriptableObjects:** the script goes in `Scripts/`, the `.asset` files with actual numbers go in `Data/`.
- **Tower/enemy tiers** are prefab variants of the base prefab, not copies.
- **Build features as prefabs in your `_Sandbox/`**, test them there, then move the finished folder into `Features/`.
- **Don't use `Resources/` folders.** Reference assets directly or through ScriptableObjects.

## Naming

- **PascalCase, no spaces** for all files and folders: `ArcherTower.prefab`, not `archer tower.prefab`.
- **Script file name = class name**: `ArcherTower.cs` contains `class ArcherTower`.
- **Namespaces follow folders**: `HoldTheHill.Core`, `HoldTheHill.Features.Towers`, etc.
- Add a suffix only where names would clash: `ArcherTower.prefab` + `ArcherTowerData.asset`.

## Scenes

Levels are split into scenes that load together, so painting tiles and placing gameplay objects don't conflict:

| Scene | What goes in it |
|---|---|
| `Scenes/Bootstrap` | `SceneLoader` and managers that live for the whole game |
| `Scenes/UI` | HUD, menus, EventSystem |
| `Levels/Hill01/Hill01_Terrain` | Tilemaps only |
| `Levels/Hill01/Hill01_Gameplay` | Camera, light, spawners, hill, player start |

**Press Play in any of these scenes** and `SceneLoader` opens the rest automatically.

**To add a level:** make `Scenes/Levels/Hill02/` with `Hill02_Terrain` and `Hill02_Gameplay`, add both to **File > Build Profiles > Scene List**, and load them from the level select.

## Code

Our scripts compile into these assemblies (the `.asmdef` files), which keeps recompiles fast:

| Assembly | Folder | For |
|---|---|---|
| `HoldTheHill.Runtime` | `_Game/` | all game code (Core + Features) |
| `HoldTheHill.Editor` | `_Game/Editor/` | editor tools (can't be used in builds) |
| `HoldTheHill.Tests.EditMode` / `.PlayMode` | `_Game/Tests/` | automated tests (**Window > General > Test Runner**) |

**"The type or namespace could not be found"** after using a new package? Select `_Game/HoldTheHill.Runtime.asmdef` and add that package's assembly under **Assembly Definition References**. Input System, URP, 2D Animation, SpriteShape, Tilemap Extras, TextMeshPro, UI, and Timeline are already added.

**Play Mode settings:** domain reload is **on**, so every Play starts with fresh values. Don't change **Project Settings > Editor > Enter Play Mode Settings**.

## How we work

1. **Pull `Indev` before starting anything.** `Indev` is our integration branch - it's where everyone's work comes together and gets tested.
2. **Make a branch for each task**, named `yourname/what-it-does`, e.g. `adam/enemy-spawner`. Branch it off `Indev`.
3. **Keep branches small and short** - merge within a day or two.
4. **Open a pull request into `Indev`.** Don't push straight to `Indev` or `main` - both are shared.
5. **Say in Discord before editing a shared scene or prefab**, and push it as soon as you're done.

> `main` is our stable branch. It is deliberately behind `Indev` right now - we'll bring it up to date in one pass once the documentation and folder structure are settled. Until then, `Indev` is the branch that reflects the project.

## Art files are locked while you edit them

Aseprite (`.aseprite`/`.ase`), Photoshop, Krita, Affinity, and Blender files can't be merged. If two people edit the same one, someone's work is lost. So these files are **read-only until you lock them**:

```
git lfs lock "unity/Assets/_Game/Features/Player/Art/Player.aseprite"     # claim it
git lfs locks                                                                      # see who has what
git lfs unlock "unity/Assets/_Game/Features/Player/Art/Player.aseprite"   # release it after you push
```

Only lock what you're actively editing, and unlock as soon as your change is pushed.
