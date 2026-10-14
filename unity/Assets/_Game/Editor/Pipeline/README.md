# Pipeline tools

Editor tooling for art import, release builds and text tables. Nothing here changes the existing
importers or the game UI. Menus are under **Tools > Hold the Hill > Pipeline**.

| Menu | Class | What it does |
|---|---|---|
| Audit Import Settings | `GrayboxImportAuditWindow` | Lists sandbox textures that deviate from the folder table in `GrayboxImportPresets`; Fix (per row or all) is undoable. |
| Build Release Package (Windows) | `GrayboxReleasePackager` | Runs `GrayboxPlayerBuild.Build`, stamps a version, writes `CHANGELOG.txt` from `git log` since the last tag, zips to `<repo>/Builds~/`. |
| Localization Tools | `GrayboxLocalizationWindow` | Hard-coded string scan, CSV export/import with diff preview, missing-key check. |

## Import presets
`GrayboxImportPresets.Rules` is the documented folder -> settings table (PPU, point filter, no
compression, no mipmaps, pivot, sprite mode). It must stay in step with `PixelIconImporter`,
`PixelAnimImporter`, `UiKitSpriteImporter` and `LevelArtImporter`; those still do the importing.

## Release build
- Version: `Pipeline~/VERSION` + git short hash (or `git describe` if the file is missing).
- Batch: run `Pipeline~/build-release.ps1`; it goes through `B:/Projects/GameDev/Tools/unity-session-lock.sh`.
  Close the Editor on this project first.
- `Builds~/` is not in `.gitignore` (only `Builds/` is). Do not commit its contents.

## Localization
- Runtime: `Graybox/Localization/` (`GrayboxStringTable`, `GrayboxLoc.Get(key)` / `Get(key, args)`, `Language`).
  Fallback is current language, then `en`, then the key.
- Hand a table to it with `GrayboxLoc.SetTable` or a `GrayboxLocBootstrap` component. No scene uses it yet.
- Sample table: `Graybox/Localization/GrayboxStrings_Sample.asset` (10 keys).
- CSV is `key,en,fr,...`; import adds or changes cells only and never deletes.
- Tests: `Tests/GrayboxLocalizationTests.cs`.
