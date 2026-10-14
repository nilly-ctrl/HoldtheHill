# Cursors and placement markers

Ten pixel-art sprites. See `Source~/MarkersPreview.png` for them on the grass tiles.

| Sprite | Size | Use |
|---|---|---|
| `Cursor` | 12x16 | Default pointer. Hot spot is the top-left pixel. |
| `CursorBuild` | 18x18 | Pointer with a hammer, while a tower is picked from the build bar. Hot spot top-left. |
| `CursorCant` | 18x18 | Pointer with a red "no" badge, over a tile that can't be built on. Hot spot top-left. |
| `CursorTarget` | 15x15 | Crosshair for aimed abilities. Hot spot is the centre, (7, 7). |
| `TileValid` | 32x32 | Green corner brackets and a faint fill: you can build here. |
| `TileInvalid` | 32x32 | Red brackets with a cross: you can't. |
| `TileSelected` | 32x32 | Gold brackets around the selected tower. |
| `TileHover` | 32x32 | Cream brackets for plain hover. |
| `RangeDash` | 8x4 | Repeating dash for a range ring. Imported with Repeat wrap; put it on a LineRenderer material with Texture Mode set to Tile. |
| `PathChevron` | 7x11 | Arrow for showing the enemy route. |

`Editor/PixelIconImporter.cs` imports this folder as Point-filtered sprites at 32 pixels per unit.

**Not wired in yet.** The graybox still draws its own range circles and uses the system cursor. To use a cursor: `Cursor.SetCursor(texture, hotspot, CursorMode.Auto)`; the texture needs Read/Write enabled, which the importer does not set.

To change them, edit `Source~/build_marker_sprites.py` and run it.
