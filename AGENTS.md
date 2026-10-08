# Hold the Hill — Agent Guidelines

Single-player 2D hybrid tower defense by **Turbulent Towers Studio**.

**This file holds the rules for every AI tool** (Claude Code, Antigravity, Gemini, Codex). `CLAUDE.md` and `.gemini/rules.md` only add notes for their own tool.

- Setup, folder structure, naming and architecture: [README.md](README.md). Read it first and follow it exactly.
- Where tasks, documents, builds and code live, and how they link: [docs/PROJECT_MAP.md](docs/PROJECT_MAP.md).
- If `AGENTS.local.md` exists, read it too. It holds rules for this one computer (git identity, fork remotes, local-only work) and wins over this file where they differ. It is never committed.

---

## 1. Git workflow
- **Group repository:** `MetalBear4-2243/HoldtheHill` is shared. Never commit, push, merge, or open auto-merge PRs into `Indev` or `main` without the person's explicit approval for that change.
- **Branches:**
  - `Indev`: integration and testing branch. Branch from `Indev` and target PRs to `Indev`.
  - `main`: stable release branch, currently stale. Do not target or branch from it.
  - Feature branches: `name/what-it-does` (e.g. `adam/enemy-spawner`). Keep them small.
- **Tasks:** tasks live on the Notion board (linked from the project map). A pull request's description links the Notion task it finishes, and the task is marked Done when that pull request is merged, not before.
- **Git identity:** commit with the person's GitHub noreply address, never a private email.
- **Never commit:** `Library/`, `Logs/`, `Temp/`, `UserSettings/`, or generated `*.csproj`/`*.sln`/`*.slnx`.

---

## 2. AI disclosure
- **One row per AI-assisted session** in [docs/AI_DEVLOG.md](docs/AI_DEVLOG.md), added before the pull request is opened: Date, Who, Tool, What the AI helped with, What we decided, Not tested yet.
- **Keep the row short and specific, about 300 characters.** Say in plain words what was made, the decision a teammate would want to know, and what is untested. Name a file only when a teammate needs to find it. No stock phrases ("code assistance and refactoring support"), and no line counts.
- Never soften or drop "Not tested yet".
- Commits keep their `Co-Authored-By` line.

---

## 3. Unity
- **Unity version:** `6000.6.0f1` exactly. Never touch `ProjectSettings/ProjectVersion.txt`.
- **Unity root:** the `unity/` folder, not the repository root.
- **Domain reload** stays on. Do not change Enter Play Mode Settings.
- **One Unity at a time:** Unity allows one instance per project. Do not start a batch or test run, or rebuild generated scenes, while a person or another AI session has the project open or is mid-run.
- **Git LFS:** art and audio are stored with LFS. `.aseprite`/`.ase`, `.psd`, Krita, Affinity and Blender files are lockable. Before editing one, run `git lfs locks` and leave it alone if a teammate holds the lock. Take your own lock (`git lfs lock "<path>"`) when the change is about to be pushed, and unlock after it merges.
- `.meta` files move, rename and get committed together with their asset.

---

## 4. Code
The README is the full reference. The rules AI tools break most often:
- Gameplay code lives in `unity/Assets/_Game/Features/<Feature>/`. `Core/` never depends on `Features/`.
- Namespaces mirror folders (`HoldTheHill.Features.Towers`). PascalCase files; file name matches class name.
- A new Unity or package dependency needs its assembly added to `HoldTheHill.Runtime.asmdef`.
- No `Resources/` folders.
- Only touch your own `_Sandbox/<github-username>/`. Never edit a teammate's.
