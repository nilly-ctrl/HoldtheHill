"""Turns a hand-drawn level map (see level_grid.py) into the files Unity uses.

    python build_level.py maps/kitchen_floor.txt              preview only, into previews/
    python build_level.py maps/kitchen_floor.txt --install    also writes ../<Name>/<Name>_Ground.png
                                                              and <Name>.json for the Unity builder
    python build_level.py --all --install                     every map in maps/
    python build_level.py maps/variants/*.txt                 several maps also get previews/Variants.png

A map's asset name is its `name:` line without spaces ("Kitchen Floor" -> KitchenFloor).

The ground image is the whole floor at 32 px per cell. The preview is the same picture at
double size with the route, the spawn, the hill and the buildable cells marked.
"""
import json
import os
import re
import sys

from PIL import Image, ImageDraw

import kitchen_tiles as kt
from level_grid import Level, LevelError, TALL

N = kt.N
HERE = os.path.dirname(os.path.abspath(__file__))
LEVELS = os.path.normpath(os.path.join(HERE, ".."))


def _sides(level, x, y, test):
    """(N, E, S, W) of `test` on the four neighbours."""
    return tuple(test(x + dx, y + dy) for dx, dy in ((0, -1), (1, 0), (0, 1), (-1, 0)))


def render_ground(level):
    im = Image.new("RGBA", (level.width * N, level.height * N), kt.C["seam"])
    tall = lambda x, y: level.kind(x, y) in TALL

    for y in range(level.height):
        for x in range(level.width):
            kind = level.kind(x, y)
            seed = x * 131 + y * 17
            tone = "cream" if (x + y) % 2 == 0 else "sage"
            at = (x * N, y * N)
            if kind in ("road", "spawn", "hill"):
                on_route = level.on_route(x, y)
                links = _sides(level, x, y, lambda a, b: level.is_roadish(a, b)
                               and (level.kind(a, b) == "hill" or level.on_route(a, b))
                               and (on_route or level.kind(a, b) == "hill"))
                im.paste(kt.road(links, seed), at)
            elif kind == "cabinet":
                im.paste(kt.cabinet(seed, _sides(level, x, y, lambda a, b: level.kind(a, b) != "cabinet")), at)
            elif kind == "fridge":
                im.paste(kt.fridge(seed, _sides(level, x, y, lambda a, b: level.kind(a, b) != "fridge")), at)
            elif kind == "mat":
                im.paste(kt.mat(seed, _sides(level, x, y, lambda a, b: level.kind(a, b) != "mat")), at)
            else:
                im.paste(kt.floor(tone, seed), at)
                if kind == "leg":
                    im.alpha_composite(kt.leg(seed), at)
                elif kind == "crumbs":
                    im.alpha_composite(kt.crumbs(seed), at)
                elif kind == "spill":
                    im.alpha_composite(
                        kt.spill(seed, _sides(level, x, y, lambda a, b: level.kind(a, b) == "spill")), at)

            if kind == "spawn":
                toward = "W" if x == 0 else "E" if x == level.width - 1 else \
                    "N" if y == 0 else "S" if y == level.height - 1 else None
                im.alpha_composite(kt.hole(seed, toward), at)
            if not tall(x, y):
                north, west = tall(x, y - 1), tall(x - 1, y)
                corner = tall(x - 1, y - 1)
                # the map's own border is a wall, but it throws no shadow into the room
                north = north and y > 0
                west = west and x > 0
                corner = corner and x > 0 and y > 0
                if north or west or corner:
                    im.alpha_composite(kt.shadow(north, west, corner), at)

    for region in level.regions("hill"):
        xs, ys = [c[0] for c in region], [c[1] for c in region]
        w, h = max(xs) - min(xs) + 1, max(ys) - min(ys) + 1
        im.alpha_composite(kt.hill(w, h, len(region)), (min(xs) * N, min(ys) * N))
    return im


def render_preview(level, ground):
    scale = 2
    s = N * scale
    im = ground.resize((ground.width * scale, ground.height * scale), Image.NEAREST)
    d = ImageDraw.Draw(im, "RGBA")
    for x, y in level.build_cells:  # a small corner tick on every cell a tower can stand on
        d.rectangle((x * s + 5, y * s + 5, x * s + 10, y * s + 6), fill=(255, 255, 255, 110))
        d.rectangle((x * s + 5, y * s + 5, x * s + 6, y * s + 10), fill=(255, 255, 255, 110))
    centre = lambda c: (c[0] * s + s // 2, c[1] * s + s // 2)
    for i in range(2, len(level.route) - 1, 3):  # chevrons along the route
        (ax, ay), (bx, by) = level.route[i - 1], level.route[i]
        dx, dy = bx - ax, by - ay
        cx, cy = centre(level.route[i])
        tip = (cx + dx * 7, cy + dy * 7)
        left = (cx - dx * 4 - dy * 7, cy - dy * 4 - dx * 7)
        right = (cx - dx * 4 + dy * 7, cy - dy * 4 + dx * 7)
        d.line([left, tip, right], fill=(255, 244, 200, 190), width=2)
    for cell, text in ((level.route[0], "S"), (level.route[-1], "H")):
        cx, cy = centre(cell)
        d.ellipse((cx - 11, cy - 11, cx + 11, cy + 11), fill=(20, 16, 12, 200), outline=(255, 244, 200, 255))
        d.text((cx - 3, cy - 5), text, fill=(255, 244, 200, 255))

    bar = 22
    out = Image.new("RGBA", (im.width, im.height + bar), kt.hexc("#22252b"))
    out.paste(im, (0, bar))
    caption = (f"{level.name}   |   {level.width} x {level.height} cells   |   road {len(level.route)} cells, "
               f"{len(level.waypoints) - 2} bends   |   {len(level.build_cells)} build cells (ticked)")
    ImageDraw.Draw(out).text((8, 5), caption, fill=kt.hexc("#d8d2c4"))
    return out


def asset_name(level):
    return re.sub(r"[^A-Za-z0-9]", "", level.name.title())


def build(map_path, install=False):
    level = Level(map_path)
    ground = render_ground(level)
    preview = render_preview(level, ground)
    os.makedirs(os.path.join(HERE, "previews"), exist_ok=True)
    stem = os.path.splitext(os.path.basename(map_path))[0]
    preview_path = os.path.join(HERE, "previews", stem + ".png")
    preview.save(preview_path)
    print(f"{stem}: road {len(level.route)} cells, {len(level.waypoints) - 2} bends, "
          f"{len(level.build_cells)} build cells -> {preview_path}")
    if install:
        name = asset_name(level)
        folder = os.path.join(LEVELS, name)
        os.makedirs(folder, exist_ok=True)
        ground.save(os.path.join(folder, name + "_Ground.png"))
        with open(os.path.join(folder, name + ".json"), "w", encoding="utf-8", newline="\n") as fh:
            json.dump(level.to_json(), fh, indent=1)
            fh.write("\n")
        print("installed", folder)
    return level, preview


def main(argv):
    install = "--install" in argv
    paths = [a for a in argv if not a.startswith("--")]
    if "--all" in argv:
        folder = os.path.join(HERE, "maps")
        paths = sorted(os.path.join(folder, f) for f in os.listdir(folder) if f.endswith(".txt"))
    if not paths:
        print(__doc__)
        return 1
    previews = []
    for path in paths:
        try:
            previews.append(build(path, install)[1])
        except LevelError as error:
            print("MAP ERROR:", error)
            return 1
    if len(previews) > 1:
        gap = 12
        sheet = Image.new("RGBA", (max(p.width for p in previews),
                                   sum(p.height for p in previews) + gap * (len(previews) - 1)),
                          kt.hexc("#101216"))
        y = 0
        for p in previews:
            sheet.paste(p, (0, y))
            y += p.height + gap
        sheet.save(os.path.join(HERE, "previews", "Variants.png"))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
