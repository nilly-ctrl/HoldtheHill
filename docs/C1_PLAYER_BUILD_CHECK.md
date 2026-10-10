# C1: Windows player build check

Date: 2026-10-09. Branch `nilly/c1-player-build`. Task C1 of the level-up plan.

## Result

A Windows player builds and the scenes load. Verified by running the built player headless; **not**
yet verified by a person playing Title to a Kitchen Floor win.

| Check | Result |
|---|---|
| Build (`Tools > Hold the Hill > Build Graybox Player (Windows)`, batch mode) | Succeeded, 292 MB, about 1 minute |
| `GrayboxLevels.CanLoad` in the player | Meadow road `True`, Kitchen floor `True` |
| Title, Play, Home, load Kitchen Floor | Reached, state Playing, map `KitchenFloor` |
| Run ends in victory (forced) | `RunEnd`, victory `True` |
| Played by hand to a real win | **Not done** |

## What failed, and what did not

- The plan's worry was wrong: scenes were not missing from the player. `GrayboxPlayerBuild` already
  passes the level scenes to `BuildPipeline.BuildPlayer`, so Build Settings need no change and
  `ProjectSettings/EditorBuildSettings.asset` was left alone. The scene list is
  `GrayboxLevels.Available()`, so a level only ships if its scene file exists when the build runs.
  Build the generated scenes first.
- The first scene is the graybox scene; it opens on Title.
- Build output goes outside the repo: `-hthBuildDir <folder>`, default `unity/Builds/Graybox`
  (not git-ignored; do not commit it).

## What changed

- Added `GrayboxPlayerSmoke` (`_Sandbox/nilly-ctrl/Graybox/Flow/`): with `-hthSmoke` on the
  command line it does Title, Play, Kitchen Floor, forces the win with
  `GrayboxBaseHealth.TriggerVictory()`, logs `[Smoke]` lines and exits 0 or 1. Without the flag it
  does nothing.

Run it:

```bash
HoldTheHill-Graybox.exe -batchmode -nographics -hthSmoke -logFile smoke.log
```

## Things the player log shows (not fixed)

- `Scene 'Bootstrap' couldn't be loaded`: `_Game/Core/Bootstrap/SceneLoader` asks for the Bootstrap
  scene after the first scene loads. It is not in the player, and the graybox does not need it.
  Harmless here; the old `_Game` flow and the sandbox flow are separate (see C2).
- `The referenced script ... is missing` for `GrayboxCombatSystems` and `Warden`: both generated
  scenes carry a `GrayboxBaseHealthUI` component whose class no longer exists (the HUD moved to
  uGUI). Harmless at runtime; the level builders should stop adding it. The `Warden` message was
  not traced.
- The shader-stripped warnings for URP depth of field, bloom and motion blur appear on every frame
  in the player log, so post-processing does not run in the build. Worth a look before a demo.

## Not tried

- Playing the waves by hand in the player. Driving it by synthetic mouse and keys was started and
  dropped: the player is not a registered app, and a run launched from a shell was closed when the
  shell call ended.
- Windows build size and load time on the lowest target PC.
