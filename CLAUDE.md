# Hold the Hill (team repo)

Single-player 2D hybrid tower defense by Turbulent Towers Studio. **`README.md` is the team's source of truth** for setup, folder layout, naming, scenes, assemblies and workflow; read it before changing anything and follow it exactly.

## Repo rules for Claude
- GitHub `MetalBear4-2243/HoldtheHill` is a **group repo**. `Indev` and `main` are both shared. Never commit, push, merge, or open auto-merge PRs into either without the user's explicit approval for that specific change.
- Branches, in order of stability:
  - **`Indev` is the group integration and test branch.** This is where the team's work actually lands and gets tested together. Branch from it, PR into it, treat it as shared.
  - **`main` is the stable branch and is currently stale** — three commits of empty Unity template. It gets brought up to date deliberately, once documentation and structure are settled. Don't target it or assume it reflects the project.
  - **Your own work:** branch off `Indev` named `name/what-it-does` (e.g. `nilly/enemy-spawner`), PR back into `Indev`. Keep them small.
- PRs open against `MetalBear4-2243/HoldtheHill` (the team repo) with `--base Indev`. `nilly-ctrl/HoldtheHill` is the user's fork and is the `origin` remote; the team repo is `upstream`. Cross-fork head is `nilly-ctrl:<branch>`.
- Commits here must use the user's GitHub noreply email (`322642514+nilly-ctrl@users.noreply.github.com`), already set as this repo's local `user.email`. Their global git identity is a private address and GitHub rejects pushes that would expose it (`GH007`).
- AI-assisted changes to this repo get a one-line row in `docs/AI_DEVLOG.md` (Date, Who, Tool, What the AI helped with, What we decided, Not tested yet) in the same commit set, and commits keep their `Co-Authored-By` line.
- Unity project root is the `unity/` subfolder, not the repo root.
- Unity **6000.6.0f1** exactly. Don't touch `ProjectSettings/ProjectVersion.txt`.
- Git LFS stores art and audio. `.aseprite`/`.ase`, `.psd`, Krita, Affinity and Blender files are lockable: `git lfs lock "<path>"` before editing, and `git lfs unlock` after pushing.
- Don't commit `Library/`, `Logs/`, `Temp/`, `UserSettings/`, or generated `*.csproj`/`*.sln`/`*.slnx`.

## Where code goes (summary of README)
- Game code: `unity/Assets/_Game/` → `Core/` (shared systems; never depends on Features), `Features/<System>/` (Player, GameFlow, Enemies, Towers, Combat, Economy, Progression, Levels, UI, DebugTools), `Shared/` (assets used by 2+ features), `Scenes/`, `Editor/`, `Tests/`.
- Personal experiments: `_Sandbox/<github-username>/` only. The user's sandbox is `_Sandbox/nilly-ctrl/`; never edit teammates' sandboxes.
- Assemblies: `HoldTheHill.Runtime` (`_Game/`), `HoldTheHill.Editor`, `HoldTheHill.Tests.EditMode` / `.PlayMode`. A new package needs adding to `HoldTheHill.Runtime.asmdef` references.
- Namespaces follow folders (`HoldTheHill.Features.Towers`). PascalCase files, file name = class name. ScriptableObject scripts in `Scripts/`, `.asset` data in `Data/`. Tiers are prefab variants. No `Resources/` folders.
- Levels are additive scene pairs: `Levels/<Level>/<Level>_Terrain` (tilemaps) + `<Level>_Gameplay`; `SceneLoader` in `Scenes/Bootstrap` loads the rest on Play.
- Domain reload stays **on** (don't change Enter Play Mode Settings).

## Related assets
Ant tower art (28 classes, 10 themes, animations) lives outside the repo in `/Users/nilly/GameDev/Assets/Ants/`. Anything imported here must follow the feature-folder layout, PascalCase naming, and LFS locking.
