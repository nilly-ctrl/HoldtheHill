# Validation tools

Editor-only tools for the gray-box sandbox. Assembly `HoldTheHill.Sandbox.Validation` (this folder), namespace `HoldTheHill.Sandbox.NillyCtrl.Editor`. Nothing here changes an asset.

## Project validator

`Tools > Hold the Hill > Validation > Run Project Validator` opens a window with the findings (filter by severity, Ping to select the asset, Copy report).

From code: `GrayboxProjectValidator.Run()` returns a `List<ValidationFinding>` (severity, check, message, asset path, `Target` to ping).

Checks, all limited to `Assets/_Sandbox/nilly-ctrl`:

| Check | Finds |
|---|---|
| MissingReference | scripts, prefabs and assets that prefabs, scenes and data assets point at by GUID but no longer exist; scripts with no loadable class |
| PrefabVariant | variants whose base is missing, nested prefab instances of deleted prefabs, variants that add a second copy of a script the base already has |
| DataAsset | empty name, id, icon, prefab; cost, duration, health, speed or range at or below 0 (towers, bosses, resources, power-ups, species, abilities, skill nodes, achievements, wave enemies) |
| Catalog | towers, achievements and skill nodes no catalog lists; empty or repeated catalog slots; other data assets nothing references |
| Ids | duplicate string ids; skill-node prerequisites that are deleted, outside the tree, self-referencing or looping |
| MetaFile | files and folders without a `.meta` (a sprite without one is an error); orphan `.meta` files |
| ScriptLayout | `.cs` files with more than one MonoBehaviour, or one whose file name differs from its class |

Scenes are read as text, never opened, so it is safe with the Editor in use.

Batch: `-executeMethod HoldTheHill.Sandbox.NillyCtrl.Editor.GrayboxProjectValidator.RunBatch` logs every finding and exits 1 on any error, else 0. Run it through `unity-session-lock.sh ... run <id> "validator" -- -batchmode -nographics -executeMethod ...`, never directly, and not while the project is open in another Editor.

## Balance sim (an estimate)

`Tools > Hold the Hill > Validation > Run Balance Sim (estimate)` (or `Open Balance Sim Window` for settings) runs a scene-free, analytic model of the gray-box tower catalog against `GrayboxWaves.asset` and writes:

- `unity/Assets/_Sandbox/nilly-ctrl/Balance~/sim-report.csv`
- `unity/Assets/_Sandbox/nilly-ctrl/Balance~/sim-report.txt`

Per wave: time to kill, leaks and leak points (hill health), gold start / spent / earned / end, and a verdict of on target, under-tuned (leaks) or over-tuned (enemies die at the entrance).

It is a model. Towers hit the front-most enemies along one line of route; slows, statuses, healers, shield regeneration, projectile travel, upgrades, abilities and the Warden are left out, and the buying strategy is an assumption. Use it to see which waves are far off and in which direction. The real combat is measured by `Tests/GrayboxBalanceHarness.cs` (play-mode) and the existing `Graybox Dashboard`; the window has a button for it, and pasting its console output into the window shows measured kills and leaks beside each estimate.

Batch: `-executeMethod HoldTheHill.Sandbox.NillyCtrl.Editor.GrayboxBalanceSim.RunBatch` (also through the session lock).

Route length defaults to 30 units, the gray-box route in `GrayboxBuilder.Route`. Change it in the window for another level.

## Tests

`Tests/GrayboxValidatorTests.cs` and `Tests/GrayboxBalanceSimTests.cs` cover the pure rules. They are wrapped in `#if UNITY_EDITOR && HTH_VALIDATION_TESTS` so the test assembly still compiles without them. To switch them on:

1. Add `"HoldTheHill.Sandbox.Validation"` to `references` in `Tests/HoldTheHill.Sandbox.Graybox.Tests.asmdef`.
2. Create `Tests/HoldTheHill.Sandbox.Graybox.Tests.rsp` containing `-define:HTH_VALIDATION_TESTS`.
