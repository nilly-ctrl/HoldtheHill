# The promotion stack: graybox shared code into `_Game`

Proposal from Nilly, for the team meeting on 2026-10-15. Nothing here is merged. The code is on
the fork `nilly-ctrl/HoldtheHill` as 17 branches, each built on the one before, and each compiles
against `Indev` (`127d56bf`). Every PR into `Indev` still needs the team's yes.

## Why

The graybox game in `_Sandbox/nilly-ctrl` and the team code in `_Game` grew two versions of the
same systems: hill health, enemies, towers, placer, economy, game flow. The stack keeps one of
each in `_Game` and keeps every file GUID, so scenes, prefabs and assets that point at `Tower`,
`Queen`, `TowerData`, `TowerPlacer`, `EnemySpawner` or `MapWaveDataSO` keep their links and
their Inspector values.

## The branches, in merge order

| # | Branch (`nilly/...`) | What it does | Needs a word from |
|---|---|---|---|
| 1 | `promote-combat-contracts` | `IDamageable`, `DamageInfo`, `StatusEffectData`, `CombatUtil`. Interfaces and data only | — |
| 2 | `promote-enemies` | `EnemyHealth` + `EnemyMover` (+ shield, healer, splitter); route maths on `EnemyPath` | Jason |
| 3 | `promote-towers` | `Tower` becomes the placed tower (targeting, levels, credit); 22 weapon scripts into `Features/Combat` | Adam |
| 4 | `promote-tests` | 11 PlayMode tests that build their own scenes; proof for 1–3 | — |
| 5 | `promote-spawner-1a` | Spawner in `HoldTheHill.Features.Enemies`; counts deaths and leaks itself | spawner owner |
| 6 | `promote-spawner-1b` | `EnemySpawner.Spawn`/`Track` are the one way an enemy enters a run | spawner owner |
| 7 | `promote-queen` | `Queen` becomes the hill: breach damage from each enemy, once-only game over, heal, bonus health | Jason |
| 8 | `promote-economy` | `Wallet` (plain C#) + `WalletBehaviour` pays bounties | team (Q1 below) |
| 9 | `promote-gameflow` | `GameFlowController`: which screen is up, pause, speed; only writer of `Time.timeScale` | — |
| 10 | `promote-towerdata` | Adam's `TowerData` moves to `Features/Towers/Data`, gains the fields the placer needs; old field names still read. Contains the merge with `Indev` | Adam |
| 11 | `promote-placement-rules` | `BuildGrid`, `BuildRules`, `PathClearanceBlocker`: where a tower may stand, as tested rules | level people (Q2) |
| 12 | `promote-tower-commands` | Build, upgrade, sell with costs and events | — |
| 13 | `promote-tower-placer` | `TowerPlacer` becomes the placer, input through Input System actions | placer / controls owner |
| 14 | `promote-retire-silverfish` | Deletes `SilverfishEnemy` (unreferenced) | Jason |
| 15 | `promote-projectile-split` | `Projectile` travel maths into `ProjectileFlight`; behaviour unchanged | — |
| 16 | `promote-spawner-lookup` | Map-to-waves lookup into `MapWaveLookup`, with tests | spawner owner |
| 17 | `promote-retire-basic-enemy` | Removes `BasicEnemy`; `Triangle.prefab` rebuilt in place (same GUID, same numbers) on the shared components | Jason |

Most branches carry their own EditMode tests (4–11 each). On the fork, with all 17 in place,
every team EditMode and PlayMode test passes, plus the graybox suites.

**Not tested on `Indev`:** no team scene exercises this code yet. After each merge, open the
project, let it compile, run the tests, and check the scene that branch touches (tower list
scene after 10; a scene with the three input actions after 13; Triangle walking a path after 17).
Nobody has played a full run since the placer and flow were split.

## For Jason: `Queen` (branch 7) and `BasicEnemy` (branches 2, 14, 17)

- `Queen` keeps its name, file, GUID and `_maxHealth`. Health becomes `float`. Breaches come from
  `EnemyMover.ReachedEnd`, and each enemy prefab sets its own `BreachDamage` (Grunt 10, Brute 25,
  Runner 5, Swarm 2, ...). Game over fires once. No static `Instance`. `QueenTests` rewritten, 11 tests.
- `BasicEnemy` does health, movement, facing and gizmos in one class. The shared enemy splits it
  into `EnemyHealth` and `EnemyMover`; shields, healing, slows, bounties and First/Last targeting
  hang off those as extra components. `EnemyPath` stays yours, with route length and distance added.
- What changes for callers: `double` → `float`; `Damage` → `BreachDamage`; `SetPath(...)` → the
  enemy walks the path it references; `OnDestinationReached` → `EnemyMover.Arrived`/`ReachedEnd`;
  `OnDied` → `EnemyHealth.Died`/`Defeated`. Your two `BasicEnemyTests` that fail in EditMode on
  `Indev` today go with `BasicEnemy`.
- **Asks:** yes or no to the rewrite; keep the name `Queen` (recommended) or rename to `Hill`;
  is click-to-kill (Silverfish) a design you want kept?

## For Adam: `TowerData` (branch 10)

- Your `TowerData` keeps its GUID, so your six `Testing/` assets and `TowerCardBase.prefab` keep
  their values. It moves to `Features/Towers/Data/` in `HoldTheHill.Features.Towers`.
- Public fields become properties; old names still load (`towerName` → `DisplayName`,
  `towerIcon` → `Icon`, `honeydewCost` → `BaseCost`). New: `CostResource`, `Archetype`, `Prefab`
  (the `Tower` your commented-out line was waiting for), `GhostSprite`, `IconName`.
- `TowerShopManager` and `TowerCardUI` get a `using` and read the properties; nothing else changes.
- **Asks:** yes or no; keep or drop your sandbox copies of the three files; does
  `TowerShopManager` become the build bar that drives the placer?

## Spawner and path: what is fixed, what is still open

Fixed by the stack: the living-enemy count now drops on deaths and leaks (it only went up);
enemies spawned by anything (bosses, splitter halves, cheats) are counted; one path system
instead of two; the build rule is a tested rule instead of the road's width; the spawner is
under 400 lines.

Still open after it: `onWaveCompleted` fires when a wave finishes *spawning*, not when it is
dead (a `WaveCleared` event would say what it means); no pooling; a `Debug.Log` per wave in
player builds; `EnemyPath` reads its waypoints once in `Awake` and draws its own road sprites at
runtime; every script finds "the" path by search, so one path per scene; no edit-time check that a
spawner's map id exists.

## Questions for the team

1. **Food or Honeydew buys a tower?** The wallet and `TowerData.CostResource` hold either; the
   stack does not choose.
2. **Grid origin:** `BuildGrid.Snap` centres cells on whole units. One function to change if the
   level tilemaps say otherwise.
3. **One PR per branch, or a few grouped PRs?** Merging a later branch brings all the ones before it.

## How it lands

One branch at a time into the fork's `Indev`, an Editor check after each, then a PR to the team's
`Indev` for that branch (or group) once the people named above have said yes.
