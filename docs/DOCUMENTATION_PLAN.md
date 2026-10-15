# Code documentation: how to begin (plan, 2026-10-15)

Jason asked for documentation on the code. He already writes it a certain way, so the plan is to
match his format, replace the three of his documents the stack made stale, then cover the promoted
systems one by one in the order they land, with the reference parts generated from the code so they
cannot drift.

## What Jason's documents look like

Three files on the team's `Indev`, in `Documentation/` at the repository root:
`EnemyScripts_Documentation.md` (198 lines), `EnemySpawner_Documentation.md` (190),
`MapWaveData_Documentation.md` (159). Each one:

1. Title `Technical Documentation: <File>.cs`, then a header block: target Unity version, context,
   file location, companion files.
2. **Overview & Architecture** with "Key Architectural Pillars" bullets.
3. A **state machine** or data-structure section with a `mermaid` diagram where the system has one.
4. **Inspector Fields Reference**: one table per `[Header]` (field, type, default, description).
5. **Public API Reference**: properties, C# events, then each public method with its signature as
   a heading and a short paragraph.
6. **Lifecycle & Extensibility** (what is virtual, how to subclass) or **Execution Flow**.
7. **Integration With Other Systems**, one subsection per neighbour.
8. **Setup & Integration Guide** / **How to create one in Unity**, numbered steps.

That is a good format: it answers "what is in the Inspector", "what can I call", "how does it talk
to its neighbours" and "how do I set one up" in that order. We keep it.

## Jason's documents stay as they are

His three files are not edited, renamed or replaced. They describe his code, and the team has not
yet chosen whose code stays; he will want to read ours next to his before that choice. So:

- Ours live in `Documentation/Fork/`, one file per system, named to match his where the system is
  the same (`Documentation/Fork/EnemyScripts_Documentation.md` beside
  `Documentation/EnemyScripts_Documentation.md`, likewise the spawner and the map wave data), so
  each pair reads one-to-one.
- Each of ours ends with a short "Against the Indev version" section: what the fork's version does
  that his does not, what it drops, and what a caller changes. The facts for the enemy pair are
  already in `docs/BASICENEMY_COMPARISON.md`; the spawner and path facts in
  `docs/ENEMY_SPAWNER_AND_PATH_NOTE.md`.
- `Documentation/README.md` indexes both sets, side by side, with a column for the decision:
  Indev, fork, or merged, blank until the team decides.
- Once a decision is made for a system, the losing document is removed in that merge, not before,
  and the winning one moves to `Documentation/` under the plain name.

## The rule that keeps it true

Every document has two kinds of content: **reference** (fields, properties, events, method
signatures, defaults) and **prose** (overview, pillars, integration, setup). Reference drifts the
moment someone renames a field; prose does not. So:

- Reference sections are **generated** from the scripts, by a small tool
  (`B:\Library\Tools\hth-docs\gen_doc.py`, to write): it reads `[SerializeField]` fields with their
  `[Tooltip]`, `[Header]`, `[Min]`/`[Range]` and defaults, public properties, events and methods
  with their `/// <summary>` text, and emits the two reference sections in Jason's table layout.
  370 of the 381 scripts under `_Game` already carry summaries, so the raw material is there.
- Prose sections are **hand-written** once and kept in the same file between marker comments
  (`<!-- prose:overview -->` ... `<!-- /prose -->`); the tool regenerates only the reference blocks
  and leaves the prose alone.
- A document is regenerated in the same pull request that changes its scripts. One line in
  `AGENTS.md` section 4 says so.

If the tool turns out to be more than a day, the fallback is to write the reference sections by
hand from the same sources and accept the drift; the format stays the same either way.

## Where the documents live and how they are named

- Fork documents in `Documentation/Fork/<System>_Documentation.md`; the plain `Documentation/` folder stays Jason's until a system is decided.
- One document per **system**, not per file, when files only make sense together: the enemy
  document covers `EnemyHealth`, `EnemyMover`, `EnemyShield`, `EnemyHealer`, `EnemySplitter` and
  `EnemyPath`; the placement document covers `TowerPlacer`, `PlacerInput`, `TowerPlacerFeedback`,
  `BuildRules`, `BuildGrid`, `IBuildBlocker`, `PathClearanceBlocker`, `TowerCommands`,
  `PlacementGhost`. A single-file system keeps a single-file document, as his do.
- `Documentation/README.md` is the index: one row per document, what it covers, the branch it
  applies to, last regenerated date.

## Order of work

**Before Wednesday (so Jason can read ours next to his):**

1. `Documentation/Fork/EnemyScripts_Documentation.md`: `EnemyHealth` + `EnemyMover` (+ the optional
   components and `EnemyPath`), with the "Against the Indev version" section from
   `docs/BASICENEMY_COMPARISON.md`.
2. `Documentation/Fork/EnemySpawner_Documentation.md`: the fork's spawner (namespace, the two
   listeners, `Spawn`/`Track`, `MapWaveLookup`), the state diagram unchanged, and what differs.
3. `Documentation/Fork/MapWaveData_Documentation.md`: the same data, new path and namespace.
4. `Documentation/Fork/Combat_Documentation.md`: `IDamageable`, `IHealthReadable`, `IOwned`,
   `DamageInfo`, `DamageType`, `StatusEffectData`, `CombatUtil`. Short; it is what every other
   document points at. No Indev counterpart exists.

**Then one document per stack branch, in merge order**, each written when its branch is about to
merge so it is reviewed with the code:

5. Towers and weapons (`Tower`, `TargetingPriority`, `TowerCredit`, `TowerWeapon` and the weapon
   list with one line each, `Projectile` with `FlightMode` and `ProjectileFlight`).
6. Queen (hill health, breaches, victory and defeat).
7. Economy (`ResourceData`, `Wallet`, `WalletBehaviour`).
8. GameFlow (`GameFlowState`, `GameFlowController`, with the state diagram).
9. TowerData.
10. Placement (the placer document above).

**After the move branch lands:** the sandbox systems that became features (HUD, menus, warden,
specials, power-ups, species, progression, save, localization, input, dev tools), using the READMEs
already in those folders as the prose and the tool for the reference.

## The template

`Documentation/_TEMPLATE_Documentation.md` holds the skeleton with the marker comments, so every
document starts the same way and the tool knows where to write.

## Decisions to take with Jason

- Per-system documents where files only make sense together (proposed above), or strictly one
  per file as his are.
- Whether the mermaid diagrams render where the team reads them (GitHub does; some editors do
  not). If not, a PNG beside each diagram.
- Whether he wants the migration tables ("what changed from BasicEnemy") kept permanently or
  dropped once the merge is done.

## First step

Write the template and the tool's first version against `EnemyHealth`, then produce document 1 and
show it to Jason before the other three, so the format is agreed on one example rather than argued
over four.

## How the remaining five are produced (2026-10-14)

Documents 6 to 10 (Queen, Economy, GameFlow, TowerData, Placement) each run in a session of
their own, on Opus 5.5, on a git branch of their own off `nilly/c1-player-build`:
`nilly/docs-queen`, `nilly/docs-economy`, `nilly/docs-gameflow`, `nilly/docs-towerdata`,
`nilly/docs-placement`. Each writes one file under `Documentation/Fork/` and nothing else; the
index rows in `Documentation/Fork/README.md` are added when the five are merged back, so the
branches cannot conflict. Opus 5.5 because the task is well specified by the template and five
finished examples, needs careful reading of the code to avoid invented fields, and does not need
the top model; Sonnet would be cheaper but the documents are what Jason judges the code by.
