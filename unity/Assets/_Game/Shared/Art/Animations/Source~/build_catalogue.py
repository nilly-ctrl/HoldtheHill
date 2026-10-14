"""Writes ArtCatalogue.csv: one row for every sheet, and one for every ground tile.

    python build_catalogue.py

Columns: Group, File, What it is, Canvas, Frames, Tags (name, frame count, loop or once),
Variant of (the summer or meadow original a seasonal or biome version was made from),
Used by the game.

"Used by the game" is worked out, not typed in: a sheet counts as used when the sandbox's C#
names it, or when it belongs to a family the graybox loads by pattern (the ten towers' FxTower
files, the Enemy...Moves files, the Decor pieces the builder scatters). The weapon library is
listed as "weapon picker only". Everything else is "no": drawn, not yet used.
"""
import csv
import glob
import json
import os
import re

HERE = os.path.dirname(os.path.abspath(__file__))
SHEETS = os.path.normpath(os.path.join(HERE, "..", "Sheets"))
SANDBOX = os.path.normpath(os.path.join(HERE, "..", ".."))
TILES = os.path.join(SANDBOX, "Tiles")
SUFFIXES = ("Autumn", "Winter", "Night", "Sand", "Forest", "Neon", "Space", "Spooky", "Steampunk", "Medieval", "Samurai",
            "Candy", "Jungle", "Pirate", "Robot", "NeonSpace", "Vaporwave", "Military", "Elite")
GRAYBOX_TOWERS = ("Linear", "Homing", "Mortar", "Ricochet", "Chain", "Beam", "Orbit", "FrostAura", "Knockback", "MineLayer")
GRAYBOX_ENEMIES = ("Runner", "Grunt", "Brute", "Shielded", "Splitter", "Swarm", "Healer")
SCATTERED = ("DecorGrass", "DecorFlower", "DecorPebble", "DecorLeaf", "DecorTwig", "DecorMushroom", "DecorRoot", "DecorTrailEdge")


def code_names():
    names = set()
    for path in glob.glob(os.path.join(SANDBOX, "**", "*.cs"), recursive=True):
        if os.sep + "Tests" + os.sep in path:
            continue
        with open(path, encoding="utf-8", errors="ignore") as f:
            names.update(re.findall(r'"([A-Z][A-Za-z0-9]+)"', f.read()))
    return names


def variant_of(name, all_names):
    base = name
    changed = True
    while changed:
        changed = False
        for suffix in SUFFIXES:
            if base.endswith(suffix) and base[:-len(suffix)] in all_names:
                base = base[:-len(suffix)]
                changed = True
    return base if base != name else ""


def used(name, in_code):
    if name.startswith("Wpn"):
        return "weapon picker only"
    if name in in_code:
        return "graybox"
    if name in ("FxTower" + t for t in GRAYBOX_TOWERS) or name in ("Enemy" + e + "Moves" for e in GRAYBOX_ENEMIES) or name in SCATTERED:
        return "graybox"
    return "no"


def main():
    in_code = code_names()
    rows = []
    paths = sorted(glob.glob(os.path.join(SHEETS, "*", "*.json")))
    all_names = {os.path.basename(p)[:-5] for p in paths}
    for path in paths:
        with open(path, encoding="utf-8") as f:
            data = json.load(f)
        name = os.path.basename(path)[:-5]
        frames = data["frames"]
        tags = "; ".join(f'{t["name"]} {t["to"] - t["from"] + 1} {"once" if t.get("repeat") else "loop"}' for t in data["meta"]["frameTags"])
        rows.append([os.path.basename(os.path.dirname(path)), name, data["meta"].get("description", ""),
                     f'{frames[0]["frame"]["w"]}x{frames[0]["frame"]["h"]}', len(frames), tags, variant_of(name, all_names), used(name, in_code)])
    from PIL import Image
    tile_names = {os.path.basename(p)[:-4] for p in glob.glob(os.path.join(TILES, "**", "*.png"), recursive=True)}
    for path in sorted(glob.glob(os.path.join(TILES, "**", "*.png"), recursive=True)):
        name = os.path.basename(path)[:-4]
        folder = os.path.relpath(os.path.dirname(path), SANDBOX).replace(os.sep, "/")
        with Image.open(path) as im:
            size = f"{im.width}x{im.height}"
        rows.append([folder, name, "Ground tile", size, 1, "", variant_of(name, tile_names),
                     "graybox" if folder == "Tiles" else "no"])
    out = os.path.join(HERE, "ArtCatalogue.csv")
    with open(out, "w", newline="", encoding="utf-8-sig") as f:
        w = csv.writer(f)
        w.writerow(["Group", "File", "What it is", "Canvas", "Frames", "Tags", "Variant of", "Used by the game"])
        w.writerows(rows)
    counts = {}
    for r in rows:
        counts[r[7]] = counts.get(r[7], 0) + 1
    print(f"{len(rows)} rows -> {out}")
    print("used by the game: " + ", ".join(f"{k}: {v}" for k, v in sorted(counts.items())))
    print("originals (not a variant of anything):", sum(1 for r in rows if not r[6]))


if __name__ == "__main__":
    main()
