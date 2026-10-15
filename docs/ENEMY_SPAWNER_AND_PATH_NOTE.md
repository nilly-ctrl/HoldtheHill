# The enemy spawner and the enemy path: what is wrong today, and what the stack does about it

Written 2026-10-15 from the code on the team's `Indev` (`127d56bf`). Nothing here is about who wrote
what: both files did their job for the first prototype, and these are the things that bite once
the game grows past one map and one enemy type.

## EnemySpawner

1. **The living-enemy count only ever goes up.** `NotifyEnemyDefeated` exists, but nothing on
   `Indev` calls it. `LivingEnemyCount` is therefore the number ever spawned, minus whatever has
   been destroyed by the time the list is purged of nulls. An enemy that walks off the end of the
   path is never removed, so "all enemies cleared" is never true again after the first leak, and
   `requireEnemiesClearedBeforeNextWave` can only pass by accident.
2. **Enemies from anywhere else are invisible to it.** The spawner counts only what it
   instantiates itself. A boss, a splitter's halves, a cheat, a special event: none of them show in
   the count, so nothing built on that count (victory, "wave cleared", the HUD) can be trusted.
3. **A wave is "completed" when it has finished spawning, not when it is dead.** `onWaveCompleted`
   fires at the end of the spawn coroutine and the state goes to `Completed` after the last wave
   has spawned, while its enemies may still be walking. The names suggest the opposite.
4. **Map selection is a string match across three lists.** The map id is compared, ignoring case,
   against an override asset, a list of assets and a list of inline configs, and
   `autoDetectMapFromScene` makes the scene name the map id. There is no check at edit time, so a
   typo means silence until the warning at runtime.
5. **No pooling.** Every enemy is `Instantiate`d and `Destroy`ed (rule 26). Fine for ten enemies,
   not for swarms.
6. **It logs every wave.** Start, finish, map change, each at `Debug.Log`, in every player build.
7. **Spawn position is the spawn point or `Vector3.zero`.** Nothing puts an enemy on the start of
   the route; it depends on someone placing the spawn point exactly on the first waypoint.
8. **One file does three jobs** (413 lines): map lookup, the wave state machine and the spawn
   coroutine, plus five public `UnityEvent` fields that are null when the component is added from
   code, so tests have to construct them by hand.

## EnemyPath

1. **The route is read once in `Awake`.** Waypoints are the child transforms' positions at that
   moment. Move a waypoint afterwards, or create the path after an enemy, and they disagree.
2. **It only knows positions.** No route length, no "how far along is this enemy", so First/Last
   targeting and any progress display have nothing to ask.
3. **It draws its own road at runtime.** `CreateRoad` spawns one stretched square sprite per segment
   in `Awake`, under a child named "Road", with its own colour and sorting order. The placeholder
   art is baked into the gameplay class, is rebuilt every play, cannot be edited in the scene and
   does not match a level's floor art.
4. **The build rule lives on the path, tied to the road's width.** `Blocks(cell, size)` uses
   `_roadWidth` and a hidden margin of 0.01, so "how wide the road looks" and "how close a tower may
   stand" are the same number, and the placement rule cannot be tested without a scene.
5. **Two path systems.** `BasicEnemy` keeps its own copy of the coordinates (`SetPath` copies the
   positions in) and finds the `EnemyPath` with `FindAnyObjectByType` in `Start`; the shared
   `EnemyMover` walks the `EnemyPath` directly. Two walkers, two copies of the route.
6. **One path per scene, found by search.** Everything finds "the" path with
   `FindAnyObjectByType`, so two spawners or two routes on one map are impossible, and the search
   cost sits in every enemy's `Start`.
7. **Small things.** The gizmo skips the road by its name string; a path with fewer than two
   waypoints draws nothing and says nothing.

## What the promotion stack already does

- Branch 5 (`promote-spawner-1a`): the spawner joins `HoldTheHill.Features.Enemies` and listens to
  `EnemyHealth.Defeated` and `EnemyMover.ReachedEnd` itself, so deaths and leaks both leave the
  count (spawner 1 and the leak half of 2).
- Branch 6 (`promote-spawner-1b`): `Spawn` and `Track` are the one way an enemy enters a run, so
  bosses, halves, cheats and specials are counted and raise `onEnemySpawned`; `Spawn` can snap to
  the route start (spawner 2 and 7).
- Branch 2 (`promote-enemies`): route maths on `EnemyPath` (length, distance along the route) that
  `EnemyMover` and targeting use (path 2).
- Branch 11 (`promote-placement-rules`): `PathClearanceBlocker` and `BuildRules` own where a tower
  may stand, as plain rules with EditMode tests; the path's `Blocks` is no longer the rule (path 4).
- Branch 16 (`promote-spawner-lookup`): the map lookup is `MapWaveLookup`, plain C# with tests, and
  the spawner is 384 lines (spawner 4 in part, 8).
- Branch 17 (`promote-retire-basic-enemy`): `BasicEnemy` is gone and `Triangle` walks with
  `EnemyMover`, so there is one path system (path 5).

## Still open after the stack

- "Wave completed" still means "finished spawning" (spawner 3). The flow decides victory from
  `Completed` plus `LivingEnemyCount == 0`, which works, but the event name misleads; a `WaveCleared`
  event raised when the count hits zero would say what it means.
- No pooling (spawner 5) and the per-wave logs (spawner 6).
- The road sprites in `Awake` (path 3), the once-in-Awake read (path 1) and the single path found
  by search (path 6). The level builder draws the real floor now, so the road drawing can become
  an opt-in or move to an editor-time helper.
- An edit-time check that every map id a spawner names exists (spawner 4).
