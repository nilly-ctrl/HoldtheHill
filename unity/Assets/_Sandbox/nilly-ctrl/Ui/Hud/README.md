# HUD pieces

24 pixel-art sprites for in-game HUD: bars, the wave track, tooltips, cost tags, keycaps, icon slots and pips. Same palette as the buttons and panel in `Ui/` and `UiKit/Sprites/`. See `Source~/HudPreview.png` for them in use.

| Sprite | Size | Use | Slice border (L, B, R, T) |
|---|---|---|---|
| `BarFrame` | 16x10 | Frame for any bar. Put a fill inside, inset 3 px. | 3, 3, 3, 3 |
| `BarFillHealth`, `BarFillShield`, `BarFillDanger`, `BarFillFood`, `BarFillSkill` | 4x4 | Bar fills: green, blue, red, gold, purple | 1, 1, 1, 1 |
| `BarTick` | 1x4 | Segment mark drawn over a fill | none |
| `WaveTrack`, `WaveFill` | 12x6, 4x2 | Wave progress line and its gold fill, inset 2 px | 2 all round; 1, 0, 1, 0 |
| `WaveMarker`, `WaveMarkerDone`, `WaveMarkerBoss`, `WaveMarkerNow` | 7x7 to 9x9 | Flag per wave (red upcoming, grey done), skull for a boss wave, gold diamond for "you are here" | none |
| `Tooltip`, `TooltipArrow` | 16x16, 9x5 | Dark tooltip box and its pointer | 4 all round |
| `CostTag`, `CostTagCant` | 22x12 | Price tag, gold or dimmed red when unaffordable. Text starts 9 px in. | 8, 3, 3, 3 |
| `Keycap` | 12x13 | Hotkey hint. Draw the key label in dark ink. | 4, 5, 4, 4 |
| `Slot`, `SlotSelected`, `SlotDisabled` | 36x36 | Frame for a 32x32 icon, inset 2 px | 4 all round |
| `NotifyDot` | 8x8 | "Something new" dot | none |
| `PipFull`, `PipEmpty` | 7x6 | Tower tier or charge pips | none |

## Import settings

`Editor/PixelIconImporter.cs` sets these on import: Sprite, Point filter, no compression, 32 pixels per unit, and the border from the table. If you add a sliced sprite, add its border to both `Source~/build_hud_sprites.py` and the table in that script. In a Canvas, use Image Type **Sliced** for the ones with a border.

## In the graybox HUD

`GrayboxBuilder` puts these in the scene's `GrayboxIcons` holder, and three scripts draw with them through `GrayboxIcons.DrawSliced`: the hill health bar (`GrayboxBaseHealthUI`), the wave track under it (`GrayboxHud`), and the build bar's slots, keycaps and cost tags (`GrayboxTowerPlacer`). Each falls back to its old plain drawing if the sprites are missing, so a scene built before 2026-10-05 looks as it did until it is rebuilt.

## Changing them

Edit `Source~/build_hud_sprites.py` and run it. It rewrites the PNGs, `borders.json` and the preview.
