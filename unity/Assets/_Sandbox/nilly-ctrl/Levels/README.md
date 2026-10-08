# Hand-drawn levels

A level here starts as a text map that someone draws by hand, one character per cell. Scripts turn the map into a floor picture and a playable scene. Nothing about the layout is random.

## Make or change a level

1. Draw the map in `Source~/maps/<name>.txt` (copy `kitchen_floor.txt` to start).
2. Run `python build_level.py maps/<name>.txt` in `Source~`. It checks the map and writes a picture to `Source~/previews/`. Look at it.
3. When it looks right, run it again with `--install`. That writes `<Name>/<Name>_Ground.png` and `<Name>/<Name>.json` here.
4. In Unity: **Tools > Hold the Hill > Levels > Build Kitchen Floor**. Open `<Name>/<Name>.unity` and press Play.

## Map characters

| Mark | Meaning | Towers |
|---|---|---|
| `S` | enemy spawn (exactly one) | no |
| `#` | enemy road | no |
| `H` | the hill (one or more, touching) | no |
| `.` | floor | yes |
| `M` | mat or rug | yes |
| `^` | cabinet or wall | no |
| `F` | fridge | no |
| `T` | table or chair leg | no |
| `*` | crumbs (a resource later; decoration for now) | no |
| `~` | spill (a hazard later; decoration for now) | no |

Rules the checker enforces:

- The road is one line from `S` to an `H`, moving up, down, left or right. No forks and no second spawn: the game has a single `EnemyPath`.
- Two stretches of road never touch side by side.
- Every row is the same width.

Good to know when drawing:

- Towers cannot stand on the cells that touch the road (the tower placer keeps 1.2 units clear). Leave three cells between two parallel stretches to get one row of build cells between them. The preview ticks every build cell.
- 22 x 12 cells fills a 16:9 screen. The camera fits itself to other sizes.

## Files

| File | What it is |
|---|---|
| `Source~/level_grid.py` | Reads and checks a map; works out the route and the build cells |
| `Source~/kitchen_tiles.py` | Draws the kitchen pieces; run it alone for the sheet `KitchenTiles.png` |
| `Source~/build_level.py` | Draws the floor and the preview, writes the json |
| `Source~/maps/variants/` | Layouts that were drawn as options |
| `../Editor/GrayboxLevelBuilder.cs` | Builds the scene: rebuilds the graybox, then swaps the map under it |
| `../Editor/LevelArtImporter.cs` | Import settings for the floor picture (32 pixels per cell) |
| `../Graybox/GrayboxBuildMask.cs` | The cells nothing can be built on, checked by the tower placer |

`<Name>.unity` is generated. Change the map or the builder, not the scene.

## Not done yet

- The level's own rules from idea 14 in `docs/LEVEL_IDEAS.md`: darkness, the robot vacuum, the fridge light.
- Crumbs and spills do nothing in play.
- A new theme needs its own tile drawing file beside `kitchen_tiles.py` and a `theme:` switch in `build_level.py`.
