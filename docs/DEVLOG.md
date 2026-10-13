# Hold the Hill — Devlog

What the team worked on, what we decided, and why. **Newest entries at the top.**

Add an entry when you finish a task or make a decision the rest of the team should know about. Keep it short. If AI helped with the work, also log it in the [AI devlog](AI_DEVLOG.md).

## Entry template

Copy this to the top of the log:

```markdown
## YYYY-MM-DD — Short title
**Who:** your name / GitHub username
**Branch:** branch-name

**Worked on**
- What you built or changed

**Decisions**
- What you chose and why (so nobody has to ask later)

**Problems / blockers**
- Anything that broke, is unfinished, or needs someone else

**Next**
- What you or the team should do next
```

## 2026-10-09 — Tower Shop Architecture Complete
**Who:** Adam Montgomery, with AI assistance — see [AI use log](AI_DEVLOG.md)
**Branch:** `Sandbox/Montgomery-Adam`

**Worked on**
- Architected dynamic Tower Shop UI using ScriptableObjects (TowerData). 
- Built TowerShopManager.cs to instantiate UI cards into a ScrollRect and TowerCardUI.cs to map data to visuals. 
- Analyzed nilly's Tower.cs to ensure architecture alignment. 
- Debugged UI scaling issues.

**Decisions**
- Decouple UI from combat logic using data containers. 
- Grouped UI into a TowerShopPanel master prefab. 
- Commented out nilly's namespace and prefab references to maintain clean compilation until their fork is merged into Indev.

**Problems / blockers**
- Actual tower placement logic and prefab linking (pending Indev merge).

## 2026-10-02 — Dynamic Tower Card UI & Prefab Setup
**Who:** Adam Montgomery, with AI assistance — see [AI use log](AI_DEVLOG.md)
**Branch:** `Sandbox/Montgomery-Adam`

**Worked on**
- Fixed asset transparency issues by establishing a PNG pipeline and using the Unity Sprite Editor for non-square icons.
- Built the `TowerCardBase` UI template and saved it as a prefab in the `_Indev/Features/UI/` folder.
- Wrote `TowerCardDisplay.cs` to link UI elements (`Image`, `TextMeshProUGUI`) via the Inspector using `[Header]` organization.
- Fixed a VS Code `.NET SDK not found` error to restore C# IntelliSense and error checking.
- Created `TowerCardTester.cs` to successfully validate dynamic data injection (name, cost, sprite) when entering Play mode.

**Decisions**
- Enforced transparent PNGs over JPGs for all UI sprites to eliminate solid background boxes.
- Used `Image` variable types in scripts for UI framework slots and `Sprite` types for passing the actual art assets.

**Problems / blockers**
- None currently, but the underlying data architecture for storing the actual tower stats (health, damage, etc.) doesn't exist yet.

**Next**
- Establish the data structure for tower stats.
- Build out the Tower list menu to instantiate and populate these card prefabs dynamically.

---

## 2026-09-23 — Baseline BasicEnemy Script and Tests
**Who:** Jason_Kimoto (MetalBear4-2243), with AI assistance — see [AI use log](AI_DEVLOG.md)
**Branch:** `BasicEnemyScript`

**Worked on**
- Created `BasicEnemy.cs` in `Hold the Hill/Assets/_Game/Features/Enemies/Scripts/` as the baseline component for all basic enemies.
- Configured health as a `double` (`_maxHealth`, `_currentHealth`, `TakeDamage(double)`, `Heal(double)`).
- Configured damage as an `int` (`_damage`, `Damage`).
- Implemented path traversal based on world coordinates (`_pathCoordinates`, `SetPath(...)`), with overloads for `Vector3`, `Vector2`, and `EnemyPath`.
- Added automatic sprite facing direction flip on the X-axis based on travel direction.
- Added event hooks (`OnDied`, `OnDestinationReached`, `OnHealthChanged`) and Scene view path gizmos.
- Added EditMode tests in `Hold the Hill/Assets/_Game/Tests/EditMode/BasicEnemyTests.cs`.
- Wrote comprehensive technical documentation in `Documentation/EnemyScripts_Documentation.md`.

**Decisions**
- Kept movement transform-based (`Vector3.MoveTowards`) for reliable 2D grid/road waypoint traversal.
- Supported both direct coordinate lists (`Vector2`/`Vector3`) and scene `EnemyPath` components for flexible spawning and testing.

**Problems / blockers**
- None.

**Next**
- Integrate `BasicEnemy` prefab variants into `EnemySpawner` wave definitions and test in Play mode.

---

## 2026-09-13 — Project setup, team conventions, and folder structure

**Who:** Nilly (nilly-ctrl), with AI assistance — see [AI use log](AI_DEVLOG.md)
**Branch:** `BryceTest` (not merged into `main`)

**Worked on**
- Set up Git for Unity teamwork: Git LFS for art/audio, Unity scene merging, fixed `.gitignore` rules for the `Hold the Hill/` subfolder.
- Made source art files (Aseprite, PSD, Krita, Affinity, Blender) lockable so two people can't overwrite each other's art.
- Added `.editorconfig` so Rider and Visual Studio format C# the same way.
- Removed template leftovers: 2D tutorial, Unity Version Control, Visual Scripting.
- Reorganized `Assets/_Game/` by feature: `Core/`, `Features/` (10 systems), `Shared/`, plus `ThirdParty/` and per-person `_Sandbox/` folders.
- Split levels into additive scenes (`Bootstrap`, `UI`, `Hill01_Terrain`, `Hill01_Gameplay`) with a `SceneLoader` script.
- Added assembly definitions for game code, editor tools, and tests.
- Wrote the README.

**Decisions**
- **Game scope:** single-player hybrid tower defense (player character + buildable towers), semester-long project.
- **Company name:** Turbulent Towers Studio.
- **Layout:** organize by feature, with small systems merged (e.g. currency + harvestables + loot → `Economy/`).
- **Scenes:** split each level into Terrain and Gameplay scenes so tile painting and object placement don't conflict.
- **Play mode:** domain reload turned **on** — slightly slower Play, but no leftover static values between runs.
- **Aseprite files** stay inside Unity, imported with the Aseprite Importer.
- **Branches:** all of this is on `BryceTest`; `main` only changes through a pull request.

**Problems / blockers**
- `SceneLoader` compiles but hasn't been tested by pressing Play yet.
- The Windows scene-merging path in the README hasn't been checked on a Windows PC.
- `main` isn't protected yet — the repo owner needs to require pull requests in GitHub settings.

**Next**
- Open `Hill01_Gameplay`, press Play, and confirm all four scenes load.
- A Windows teammate confirms the merge tool path.
- Team reviews `BryceTest` and opens a pull request into `main`.

---

## 2026-09-11 — Repository created
**Who:** Jason_Kimoto (MetalBear4-2243)
**Branch:** `main`

**Worked on**
- Created the GitHub repository and a Unity 6000.6.0f1 2D (URP) project in `Hold the Hill/`.
- Added a standard Unity `.gitignore`.
