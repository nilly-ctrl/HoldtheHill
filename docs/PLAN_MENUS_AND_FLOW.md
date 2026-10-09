# Menus and screen flow plan

Plan for the start menu, home hub, pause, death/victory, settings and controls screens.
Written 2026-10-05 by Nilly with Claude. **Local working note, not committed.**

## Decisions

| Area | Decision |
|---|---|
| Location and tech | `_Sandbox/nilly-ctrl`, uGUI Canvas + TextMeshPro prefabs, movable to `_Game/Features/UI` later |
| Flow | Title → Home hub → Playing ↔ Paused → Run End → Home |
| Home | Hub between runs: level/mode select, upgrades, achievements, records |
| Progression | Two layers: the in-run skill tree resets each run; a permanent currency buys lasting upgrades at Home |
| Death screen | Run stats, retry from wave 1, retry current wave (free, unlimited), rewards kept |
| Controls | Full rebinding, with gamepad for menus only; tower placement stays on the mouse |
| Deadline | None. Full scope, one theme at a time, reviewed between themes |

## Starting point (2026-10-05)

- `GrayboxMenuManager.cs` draws start and settings menus as IMGUI pop-ups (M and O). Nothing is
  saved, the SFX slider is not connected, labels use emoji.
- `GrayboxBaseHealthUI.cs` shows one victory/defeat card with a single Restart button.
- Keys are hardcoded across five scripts (N, R, T, V, H, K, A, M, O, 0-9, Esc).
  `Settings/InputSystem_Actions.inputactions` is unused by the graybox. `A` (achievements) will
  clash with WASD.
- `Time.timeScale` is set by three scripts (HUD, menu manager, restart).
- `_Game/Features/UI`, `GameFlow` and `Core/Save` are empty; `Scenes/UI.unity` is near empty.

## Themes in build order

1. **Flow controller and run snapshot.** One owner for game state, pausing and speed. Hides the
   IMGUI HUD under full-screen menus (IMGUI always draws over a Canvas). Takes a checkpoint at
   each wave start (towers, gold, hill HP, wave number) for the wave retry.
2. **Save service.** One versioned JSON file: settings, bindings, records, achievements,
   permanent currency, purchased upgrades.
3. **UI kit.** Prefabs for button, panel, slider, toggle, tabs, confirm dialog, built from the
   `Ui/` sprites and pixel font, with stick/d-pad focus and UI sound hooks.
4. **Pause and run-end screens.** Pause: Resume, Settings, Controls, Restart, Quit to Home.
   Run end: stats, "new best" callout, currency earned, both retry buttons. Victory shares the layout.
5. **Settings.** Tabs for audio, display, gameplay, accessibility. Opens from Title and Pause,
   applies live, saves on close.
6. **Controls.** All hardcoded keys move into one action map. Screen lists bindings from it:
   click-to-rebind, conflict warnings, reset to defaults, glyphs that swap keyboard/pad.
7. **Title and Home hub.** Title: Play, Settings, Controls, Quit. Home: Campaign/Endless select,
   permanent upgrade shop, achievements, records.

## Open questions

- ~~Permanent currency~~ Decided 2026-10-07: Honeydew; 1 per wave cleared plus 5 for a win;
  first upgrades are more starting food, tougher hill, bigger bounties. Numbers are first guesses.
- **Wave retry and records:** free retries inflate "furthest wave". Proposal: count retries used
  and show them beside the record.
- **In-run skill tree panel and HUD:** stay IMGUI for now; rebuilding them in uGUI is a separate
  job after the kit exists.
- **Team hand-off:** this is likely to become the team's GameFlow/UI/Save. Tell the team before
  theme 1 hardens.

## Notes from theme 1

- The graybox spawner auto-starts wave 1 on Play, so the first wave has no build phase; after a
  restart or retry the wave waits for the next-wave key. Decide which the real game wants.
- Retry rewinds run stats (kills, food, time) to the wave start and adds one to `WaveRetries`.
- Not saved in a checkpoint: mines already laid, shots in flight, per-tower damage/kill counters.
- Endless (procedural) waves do not go through `EnemySpawner` waves, so they get no wave checkpoint yet.
- `GrayboxMenuManager` (M and O pop-ups) still exists; themes 4, 5 and 7 replace it.

## Notes from theme 2

- Disk is only touched when a `GrayboxSaveHost` is in the scene (the builder adds one). Tests and
  hostless scenes get in-memory defaults.
- Saves happen on leaving play (pause, run end, quit to Home), on an achievement unlock, when the
  settings panel closes, on a purchase, and on quit.
- Achievements are now lifetime (progress carries across sessions). Before, they reset every Play.
- Best wave: further wins; at the same wave, fewer retries wins. A run retried after a defeat is
  one run in the totals.
- `metaCurrency` and `purchasedUpgrades` are stored and have an API (`GrayboxMetaProgress`), but
  nothing awards currency until the open question is answered.
- `musicVolume`, `sfxVolume`, `autoStartWaves` and `showDamageNumbers` are saved but still not
  connected to anything; theme 5 wires them.
- Level records are keyed by the spawner's map id, which is "Graybox" in the sandbox scene.

## Notes from theme 3

- Screens are built by editor code from the kit prefabs (`UiKitBuilder.AddPanel`, `AddButton`,
  `AddSlider`, `AddToggle`, `AddStepper`, `AddTabBar`/`AddTab`, `AddTitle`, `AddBody`), not by hand.
- Layout is for 640x360 and scales by whole numbers (2x at 720p, 3x at 1080p). Row height 20,
  font sizes 10 and 20 only.
- `UiScreen` owns focus and navigation; do not set navigation on controls by hand, it is rewritten.
- A screen under a dialog stays drawn normally but takes no input.
- Sound: subscribe to `UiFeedback.Cue` (Focus, Press, Change, Back, Denied, Open, Close). Nothing
  subscribes yet (audio is parked).
- The kit assembly does not reference the Graybox assembly; theme 4 adds the reference the other way.
- Tab switching (shoulders, Q/E, PgUp/PgDn) and the back fallback read devices directly; theme 6
  moves them into the action map. Q and E will need to stay free of gameplay bindings on menus.
- Not yet verified by eye above 1x scale or with a real mouse/controller.

## Progress

| Theme | State | Notes |
|---|---|---|
| 1 Flow controller and run snapshot | built 2026-10-05, awaiting review | `Graybox/Flow/`. 41/41 PlayMode tests. Placeholder IMGUI cards stand in for the screens. Live scene needs a rebuild (Tools > Hold the Hill > Build Graybox Combat Test). |
| 2 Save service | built 2026-10-05, awaiting review | `Graybox/Save/`. 58/58 PlayMode tests. File: `%USERPROFILE%\AppData\LocalLow\Turbulent Towers Studio\Hold the Hill\save.json`. |
| 3 UI kit | built 2026-10-05, awaiting review | `_Sandbox/nilly-ctrl/UiKit/`. 86/86 PlayMode tests. Look at it: Tools > Hold the Hill > Open UI Kit Gallery, then Play. |
| 4 Pause and run-end screens | in the project 2026-10-07, awaiting review | `Graybox/Screens/`. Brought over from the scratch copy. |
| 5 Settings | built 2026-10-07, awaiting review | `GrayboxSettingsScreen`, `Save/GrayboxSettings.cs`. Tabs: Audio, Display, Gameplay, Access. |
| 6 Controls | built 2026-10-07, awaiting review | `Graybox/Input/GrayboxControls.cs` (one action map made in code), `GrayboxControlsScreen`. |
| 7 Title and Home hub | built 2026-10-07, awaiting review | `GrayboxTitleScreen`, `GrayboxHomeScreen`, `Save/GrayboxUpgrades.cs`, `Flow/GrayboxEndlessMode.cs`. |

## Notes from themes 5 to 7

- All screens are one generated prefab, `Graybox/Prefabs/GrayboxMenus.prefab`
  (Tools > Hold the Hill > Build Graybox Menus). The scene now opens on the title screen; set
  "Initial State" on the `GrayboxGameFlow` object to Playing to skip the menus while tuning.
- The old IMGUI pop-ups (`GrayboxMenuManager`, M and O) and the placeholder cards are no longer
  in the built scene. Their files remain for the tests that use them.
- Settings apply at once and are written when the screen closes. Music and effects volume are
  stored and exposed (`GrayboxSettings.MusicVolume`, `SfxVolume`) but nothing reads them while
  audio is parked. Display settings do nothing in the editor, only in a build.
- The Access tab has menu size and pause-on-focus-loss only: there is no screen shake or flash
  in the game yet to switch off.
- Controls: the map is made in code because `Settings/InputSystem_Actions.inputactions` is the
  team's file. Menu move, press, back and tab switching still come from that file and are not
  rebindable. Two actions on one key is allowed and shown in orange.
- Gamepad: only Pause (Start) has a binding. No gamepad was connected when this was built.
- Endless mode sends `GrayboxCustomSpawner.SpawnProceduralWave` waves 4 seconds after the field
  clears. It has no wave retry, and its records are kept under "<map>-Endless".
- Honeydew is paid at run end. A run retried after a defeat is paid only for waves beyond what
  it was already paid for.
- Level select (2026-10-08): Home's Play tab has a Level row. Each level is its own generated
  scene, listed in `Flow/GrayboxLevels.cs`; choosing another one loads its scene and starts the
  run there. Records are per level. A level whose scene has not been built is not offered.
- The VICTORY or DEFEAT banner plays first (up to 1.6 s), then the run-end panel opens.
