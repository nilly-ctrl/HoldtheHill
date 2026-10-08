# Commit plan for `nilly/tower-projectile-system` (2026-10-04)

**Nothing here has been committed or pushed.** Refreshed 2026-10-05 (twice; the second time after the tier, world, scenery and melee work): `git status` now shows about 160 entries (many are whole new folders). This is a proposal for turning them into small commits a teammate can review. This file itself is a local working note: don't commit it.

Branch is at `7b7de1e`. The work came from two tools on 2026-10-02 to 10-04: Antigravity (Gemini) wrote most gameplay code; Claude Code wrote the art, audio, animation, tests and fixes. `docs/AI_DEVLOG.md` has a row for each piece, and each commit below should carry its rows.

## Decide first

| Question | Why it matters |
|---|---|
| **Does game code go in this PR?** Commits 1 to 3 change `_Game/`, which the whole team shares. | The README asks for small branches. These could be their own PR into `Indev`, ahead of the sandbox work that depends on them. |
| **Where did `Audio/` go?** `_Sandbox/nilly-ctrl/Audio/` (about 1,000 WAVs) is no longer in the repo. The generator output is still at `B:\Projects\GameDev\Source\Audio\hold-the-hill\out\` (500 MB with music). | `GrayboxSfxBank.asset` now holds zero clips, so the graybox is silent. **Decided 2026-10-04: ignore audio for now.** Commit 8 is on hold; the sound scripts in commit 7 are harmless without clips. |
| ~~`ProjectSettings.asset` icon-slot noise~~ | **Done 2026-10-04:** reverted to the committed version. |
| **`docs/TASK_PROJECTILE_SYSTEM.md`** still names the old `Hold the Hill/` folder. | Fix the paths or leave it out. |

## Leave out (never commit)

`.gemini/`, `unity-build-log.txt`, `unity/UpgradeLog.htm`, `unity/UpgradeLog2.htm`, `unity/test-log.txt`, `unity/test-results.xml`, `_Sandbox/nilly-ctrl/Screenshots~/`, this file. Worth adding the logs and test output to `unity/.gitignore`.

## Proposed commits, in order

Order matters: each commit only depends on the ones above it (the graybox scripts in 7 call into the animation assembly in 6). I have not built each step in isolation, so compile-check as you go.

| # | Commit | Paths | Made by | Checked how |
|---|---|---|---|---|
| 1 | Add shield, splitter and healer enemy traits | `_Game/Features/Enemies/Scripts/`: `EnemyShield`, `EnemySplitter`, `EnemyHealer` (new), `EnemyHealth` (also the `StatusApplied` event), `EnemyMover` (changed); `Combat/Scripts/DamageInfo.cs` | Antigravity, then Claude (splitter fix, `Damaged`/`Healed` events, `Pulsed`) | PlayMode tests |
| 2 | Add Frost Aura, Knockback and Mine Layer towers | `_Game/Features/Combat/Scripts/`: `FrostAuraTower`, `KnockbackTower`, `MineLayerTower`, `ProximityMine` | Antigravity, then Claude (visual-only events) | PlayMode tests |
| 2a | Add shot ownership and a launch event | `_Game/Features/Combat/Scripts/`: `Projectile.cs` (`Owner`, static `Launched`), `GroundHazard.cs`, `CombatUtil.cs` (`OwnerOf`), `ClusterProjectile.cs`; `Towers/Scripts/Tower.cs` sets the owner; `EnemyHealth` credits it; `EnemyMover.DespawnsAtEnd` and the spawner bridge's leak report | Claude | PlayMode tests |
| 3 | Add tower levels and upgrade modifiers | `_Game/Features/Towers/Scripts/Tower.cs`, `_Game/Features/Progression/Scripts/` | Antigravity, then Claude (`IUpgradeModifiers`) | PlayMode tests |
| 4 | Sandbox: icons and pixel fonts | `_Sandbox/nilly-ctrl/Icons/`, `Fonts/`, `Editor/PixelIconImporter.cs`, `Editor/PixelFontBuilder.cs` | Claude | Imported in Unity |
| 5 | Sandbox: animation sheets, tiles and UI skin art | `Animations/` (now 508 `.aseprite` files: the original 35, 23 `Wpn`, 10 `FxTower`, 4 castes, `FxMelee`, 7 `Enemy*Moves`, `FxHillHit`, 8 `Decor`, `FxStatusOverlay`, 28 tower tiers, 9 world and interface, 19 scenery, 15 nature and colony life, 25 new enemies and castes, 23 seasonal, 8 animated interface, 14 elites and bosses, 165 more seasonal, 8 riverbank, 4 story, then 94 more riverbank and forest pieces with their winter and night versions and 6 more story files), plus `Tiles/Seasons/` (9 tiles), `Tiles/Riverbank/` (9) and `Tiles/Forest/` (9), with `Source~/Gallery.html` and its generators, `Tiles/`, `Ui/`, `Editor/PixelAnimImporter.cs` | Claude | 127 imported in a scratch copy on 10-05; nothing drawn after that (scenery, nature, colony, roster, seasons, interface) has been imported; contact sheets and the gallery |
| 6 | Sandbox: animation and feedback components | `Graybox/Animation/` (its own asmdef: clip player, tower/enemy/mine animators, status visuals, impact effects, orbiter and line visuals, feedback events, and from 10-05 `GrayboxTowerExtras` and `GrayboxShotTrail`), `Editor/GrayboxAnimLibraryBuilder.cs`. **Needs `Projectile.Launched` from commit 2a.** | Claude | PlayMode tests; screenshots |
| 7 | Sandbox: graybox gameplay tools, HUD skin and sound player | `Graybox/`: `GrayboxTowerPlacer`, `TowerTargetVisualizer`, `EnemyHealthBar`, `GrayboxEconomy`, `GrayboxSkillTree(+UI)`, `GrayboxCustomSpawner`, `GrayboxProceduralWaveGenerator`, `GrayboxBaseHealth(+UI)`, `GrayboxAchievements`, `GrayboxMenuManager`, `GrayboxStatusCombos`, `GrayboxDamageNumbers`, `GrayboxHud`, `GrayboxUi`, `GrayboxPropVisuals`, `GrayboxSfx`, `GrayboxSfxBank`, the `.asmdef`; `DamageNumbers/`; `Editor/GrayboxDashboard.cs`, `Editor/GrayboxSfxBankBuilder.cs` | Antigravity (gameplay tools), Claude (skin, sound, Input System and compile fixes) | Play run and screenshots; nobody has played it by hand |
| 8 | Sandbox: sound files | Whatever audio is restored under `Audio/Sfx/` | Claude (generated) | Cues fired in a Play run on 10-02; silent now (see above) |
| 9 | Sandbox: tests | `Tests/GrayboxAnimationTests.cs`, `Tests/GrayboxMechanicsTests.cs`, `Tests/GrayboxGameFlowTests.cs`, `Tests/GrayboxSaveTests.cs`, `Tests/GrayboxBalanceHarness.cs`, `Tests/*.asmdef` | Claude and Antigravity | 92 PlayMode tests pass in a scratch copy on 10-05 (one fixed and rerun) |
| 8a | Sandbox: melee castes | `Graybox/GrayboxMeleeTower.cs` and the four placer entries in `GrayboxTowerPlacer.cs` (goes with commit 7 if that is simpler) | Claude | 3 PlayMode tests; close-up renders |
| 9a | Sandbox: weapon picker | `Graybox/GrayboxWeaponPicker.cs`, `Editor/GrayboxWeaponPickerBuilder.cs`, and the scene it generates, `Graybox/GrayboxWeaponPicker.unity` (run **Build Weapon Picker** first) | Claude | One PlayMode test; screenshots |
| 9b | Sandbox: menus, flow and saves (another session's work, 10-05) | `Graybox/Flow/`, `Graybox/Save/`, `docs/PLAN_MENUS_AND_FLOW.md`, plus its team-file edits (`EnemySpawner.RestartFromWave`, serialized `Tower.Level`) | Claude (other session) | See its devlog rows; not reviewed here |
| 10 | Rebuild the graybox | `Editor/GrayboxBuilder.cs` (now also scatters map dressing), `Editor/AiSessionLockGuard.cs`, then everything it generates: `Graybox/GrayboxCombatTest.unity`, `Graybox/Prefabs/`, `GrayboxWaves.asset`, `GrayboxAnimLibrary.asset`, `GrayboxSfxBank.asset`, `GrayboxBolt.mat`, `GrayboxBeam.mat` | Generated | Run **Tools > Hold the Hill > Build Graybox Combat Test** right before committing, so the generated files match the code |

`docs/AI_DEVLOG.md` changes go in with each commit (or as one final commit if splitting the rows is too fiddly).

## Before any of it

- **Rebuild first.** As of 2026-10-05 the real project's `GrayboxCombatTest.unity` and `GrayboxAnimLibrary.asset` predate the weapon, extras and dressing work (it was verified in a scratch copy). Run **Build Graybox Combat Test**, then **Build Weapon Picker**.
- The four-wave harness (`GrayboxBalanceHarness`) passes but its wave 4 hit the 150 s timeout with two enemies left on the field. Worth a look before the tests commit.

- Every `.meta` goes with its file.
- `.aseprite`, `.png`, `.ttf` and `.wav` are Git LFS files (`.gitattributes` already says so). Check `git lfs status` after staging; `git lfs ls-files` is empty today, so this branch would be the first to use LFS for real.
- Two Edit-mode tests in `_Game/Tests/EditMode/BasicEnemyTests.cs` fail on the committed code. That's a teammate's file and predates this branch; mention it in the PR rather than fixing it here.
- Commits use the repo's local `user.email` (GitHub noreply) and keep the `Co-Authored-By` line.

---

## Update 2026-10-05: icons, fonts, damage numbers and HUD art

Added by the Claude session that made the icon set, pixel fonts and damage numbers. **Still nothing committed.** The tree is now about 155 uncommitted paths. This section only covers that session's files; the Flow, Save, UI kit and weapon-sheet work from 10-05 (see `PLAN_MENUS_AND_FLOW.md` and the devlog) still needs its own rows from the sessions that made it.

### Changes to the commits above

| # | What changes |
|---|---|
| 1 | `EnemyHealth` also gained the static `Damaged` and `Healed` events, and `DamageInfo` the `IsCritical` flag. Same files, so no new commit; say so in the message. |
| 4 | `Icons/` grew: 20 more icons (status, combo, skill), `TitleLogo.png`, ten redrawn tower icons, and `Source~/build_logo.py`. `PixelFontBuilder.cs` now also writes `Fonts/Generated/*Style.asset` and has `Configure(spawner)`. Commit `Fonts/Generated/` with it, or leave it out and let the menu rebuild it. |
| 7 | Add `Graybox/GrayboxIcons.cs`. `GrayboxHud`, `GrayboxBaseHealthUI`, `GrayboxSkillTreeUI`, `GrayboxTowerPlacer`, `GrayboxBaseHealth` and `GrayboxStatusCombos` changed to use icons and the pixel popups. `HoldTheHill.Sandbox.Graybox.asmdef` now references `HoldTheHill.Sandbox.DamageNumbers`, so **commit 4b below must come before 7.** |
| 10 | `GrayboxBuilder.cs` adds `BuildIcons()` and puts `DamageNumberSpawner` on `GrayboxCombatSystems` in place of `GrayboxDamageNumbers`. The scene has been rebuilt with both. |

### New commits

| # | Commit | Paths | Depends on | Checked how |
|---|---|---|---|---|
| 4b | Sandbox: floating damage numbers | `_Sandbox/nilly-ctrl/DamageNumbers/` (five scripts, its own asmdef, README) | 1 (events), 4 (font atlases) | Compiled; windowed Play run on 10-04 with screenshots `Screenshots~/20`, `21`; wave 1 only, no crit seen |
| 5b | Sandbox: HUD pieces and title art | `Ui/Hud/` (24 sprites + `Source~` generator), `Ui/Title/Source~/` (backdrop, mockup, generator) | nothing | Preview images only. **Not imported correctly yet:** `PixelIconImporter.cs` still needs a `Ui/Hud/` rule for Point filter and slice borders (see `Ui/Hud/README.md`) |
| 11 | Docs: asset guide | `docs/ASSET_GUIDE.md` | nothing | Read-through |

### Leave out (additions)

`Icons/Source~/towers-ant-body-v2/` (backup of the earlier tower icons), `Icons/Source~/vector-v1/` (superseded vector set), `Icons/Source~/Redo.png`, `docs/PLAN_MENUS_AND_FLOW.md` (marked local), the generated `*.csproj` files and `Backup/`, `Backup1/` under `unity/`.

### Decide

- **`Graybox/GrayboxDamageNumbers.cs`** is no longer used by anything. Delete it before commit 7, or commit it as dead code.
- **`Fonts/Generated/*.fontsettings` and `.mat`** (legacy Unity Font assets) are not referenced by any scene, prefab or asset. The HUD uses the TTFs and the popups use the `*Style.asset` files. Dropping them means removing that part of `PixelFontBuilder.Build()`.

### Later on 2026-10-05

- **4 (icons and fonts):** 17 more icons (7 graybox enemies, 10 achievements), two redrawn, logo ants redrawn. `PixelFontBuilder.cs` no longer makes legacy `.fontsettings` / `.mat` files (they are deleted), and `PixelIconImporter.cs` now also covers `Ui/Hud/` and `Ui/Markers/`. Because of that importer change, **5b should come after 4**, not stand alone.
- **5b (HUD pieces and title art):** add `Ui/Markers/` (10 sprites, generator, README).
- **6 (animation components):** `GrayboxStatusVisuals.cs` gained Shock, Regen and Shield; `Animations/Source~/build_anim_sheets.py` and the `FxStatus` sheet (`.aseprite`, `.png`, `.json`) gained the three clips. Only that one sheet was re-exported.
- **7 (graybox tools):** `GrayboxIcons` gained `DrawSliced`; `GrayboxBaseHealthUI`, `GrayboxHud` and `GrayboxTowerPlacer` draw with the HUD pieces; `GrayboxAchievements` names the new achievement icons. `GrayboxDamageNumbers.cs` is deleted, so the "Decide" item above is settled.
- **10 (rebuild):** the scene has **not** been rebuilt since these changes, because the Unity editor was open on the project. Rebuild before committing the generated scene and `GrayboxAnimLibrary.asset`.
- **Leave out:** `Icons/Source~/TowerSpriteProposal.png` is a proposal image; commit it only if the team wants it in the repo.
