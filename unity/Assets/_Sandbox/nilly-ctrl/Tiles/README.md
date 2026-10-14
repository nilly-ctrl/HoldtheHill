# Tiles

Two kinds of thing live here.

- **Flat graybox textures** in this folder and in `Forest/`, `Riverbank/`, `Seasons/`, `Themes/`: one ground and one road texture each, stretched by the graybox. Made by the scripts in `Animations/Source~/`.
- **Tile sets** in `Sets/`: for painting a level on a Tilemap. Made by `Source~/build_tile_sets.py`. The rest of this page is about these.

## What a set is

32 px per cell, one cell per world unit, the same grid as `Hill01_Terrain`.

| Set | Ground (8 variations) | Second terrains |
|---|---|---|
| Meadow | Grass | Pond |
| Forest | Leaf litter | Stream |
| Riverbank | Sand | River |
| Patio | Grit between the slabs | Slab (raised), Puddle |
| Picnic | Gingham cloth | Fold (raised), Grass past the hem |
| Kitchen | Cream and sage floor tiles, alternating | Counter (raised), Spill |

Each second terrain is a 47-tile blob sheet, so any shape you paint joins up: single cells, one-cell channels, diagonal steps, holes. Its edge is drawn inside its own cells on a clear background, so it goes on **its own Tilemap above the ground**.

The Kitchen set uses the palette of `Levels/Source~/kitchen_tiles.py` so the two match. It is daytime; the "2 a.m." look is left to lighting.

## Painting with one

1. Open **Window > 2D > Tile Palette** and pick `<Set>Palette`.
2. Make a Grid with one Tilemap for the ground and one per second terrain, the terrain layers with a higher Order in Layer.
3. Paint the ground tile on the ground layer. It picks a variation from each cell's position, so the same map always looks the same.
4. Paint a terrain's Rule Tile on its own layer. Edges and corners sort themselves out.

To see one already painted, open `Sets/<Set>/<Set>Demo.unity`: the sample map on a Ground Tilemap with one Tilemap per terrain above it. **Tools > Hold the Hill > Build Tile Set Demo Scenes** writes these again from scratch, so save your own painting under another name.

Meadow, Forest and Patio ground is also flipped left to right in half the cells (the `Mirror` box on the ground tile) for more variety. The other grounds have a pattern or lighting that a flip would break.

Nothing here has colliders or says where towers can go; the tiles are pictures only.

## Changing or adding a set

```
cd unity/Assets/_Sandbox/nilly-ctrl/Tiles/Source~
python build_tile_sets.py            # every set
python build_tile_sets.py Meadow     # one set
```

Needs Python 3 with Pillow and NumPy. Look at `Source~/Preview/<Set>.png` (a sample map) and `<Set>Sheets.png` first, then run **Tools > Hold the Hill > Build Tile Sets** in Unity. That slices the sheets and updates the tile assets and palettes in place; sprite ids come from the sprite names, so painted maps keep their tiles.

A new set is one entry in `SETS` at the bottom of the script: a ground function, and for each second terrain a fill function and a kind (`liquid`, `raised` or `cutout`).

## Files

| File | What it is |
|---|---|
| `Sets/<Set>/<Set><Ground>.png` | Ground variations in a row |
| `Sets/<Set>/<Set><Terrain>.png` | 47 blob tiles, then four more centre tiles |
| `Sets/<Set>/*Tile.asset` | The tiles you paint with |
| `Sets/<Set>/<Set>Palette.prefab` | Tile Palette |
| `Sets/TileSets.json` | What is in each sheet, read by the builder |
| `GroundVariantTile.cs` | Ground tile that picks a variation per cell |
| `Editor/TileSetBuilder.cs` | The menu item |
