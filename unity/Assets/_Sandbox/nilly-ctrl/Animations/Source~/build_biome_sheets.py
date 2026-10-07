"""Hold the Hill second biome: the riverbank.

A sandy shore beside slow water. The towers, enemies and colony pieces are shared with the meadow;
this adds the ground they stand on and the things that only grow or wash up here.

  ../../Tiles/Riverbank/GroundSand.png     64x64, repeats both ways
  ../../Tiles/Riverbank/RoadSand.png       32x32, repeats along x: a trail of damp packed sand
  ../../Tiles/Riverbank/RoadSandFill.png   the same with no edges, for bends
  Props/PropHillSand (64x64)       the hill built of sand: Healthy, Damaged, Critical, Destroyed
  Props/WaterShore (32x32)         the water's edge, sand above and water below: Edge (tiles along x),
                                   CornerLeft, CornerRight. The foam line breathes in and out.
  Props/PlantReed (32x48)          Sway
  Props/PlantBeachGrass (32x32)    Sway
  Props/DecorShell (16x16)         A, B, C
  Props/DecorDriftwood (48x32)     A, B
  Props/RockRiver (32x32)          A, B, Wet (a glint runs over it)
  Props/CritterStrider (24x24)     Skate: a water strider, for the water. Faces right.

    python build_biome_sheets.py
"""
import math
import os
import random

from PIL import Image

import build_anim_sheets as base
from build_anim_sheets import Cv, Sprite, SPARK_S, export, flat
from build_extra_sheets import blade, thick_line
from build_scenery_sheets import loop, prop

HERE = os.path.dirname(os.path.abspath(__file__))
TILE_OUT = os.path.normpath(os.path.join(HERE, "..", "..", "Tiles", "Riverbank"))
SAND = ("#d9c08a", "#ecd9a8", "#bfa36c")          # base, light, shade
WET = ("#a88d5c", "#b99e6c", "#8f764a")
INK = base.rgba("k")[:3]


def hexc(h):
    h = h.lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), 255)


# ================================================================ tiles
def sand_tile(n=64):
    rng = random.Random(31)
    b, light, dark = (hexc(c) for c in SAND)
    im = Image.new("RGBA", (n, n), b)
    p = im.load()
    for _ in range(80):                                           # soft mottling, wrapped
        cx, cy, r = rng.randrange(n), rng.randrange(n), rng.uniform(2, 5)
        c = light if rng.random() < 0.55 else dark
        for y in range(-6, 7):
            for x in range(-6, 7):
                if x * x + y * y <= r * r and rng.random() < 0.7:
                    p[(cx + x) % n, (cy + y) % n] = c
    for row in range(0, n, 8):                                    # wind ripples: short wavy dashes
        for x in range(n):
            y = (row + round(math.sin(x * 2 * math.pi / 32 + row) * 1.5)) % n
            if (x + row * 3) % 11 < 6:
                p[x, y] = dark
                p[x, (y - 1) % n] = light
    for _ in range(18):                                           # shell grit
        p[rng.randrange(n), rng.randrange(n)] = hexc("#fff1d6")
    for _ in range(10):
        p[rng.randrange(n), rng.randrange(n)] = hexc("#83562f")
    return im


def road_tile(edges=True, n=32):
    rng = random.Random(9)
    b, light, dark = (hexc(c) for c in WET)
    im = Image.new("RGBA", (n, n), b)
    p = im.load()
    for y in range(n):
        for x in range(n):
            r = rng.random()
            if r < 0.1:
                p[x, y] = light
            elif r < 0.22:
                p[x, y] = dark
    for lane in (11, 20):                                         # tracks worn by passing feet
        for x in range(n):
            if rng.random() < 0.5:
                p[x, lane] = dark
    for _ in range(5):
        x, y = rng.randrange(n), rng.randrange(6, n - 6)
        p[x, y] = hexc("#c4bfb2")
        p[x, y - 1] = hexc("#ece7da")
    if edges:
        edge = hexc("#7a6540")
        for x in range(n):
            for y in (0, 1, n - 2, n - 1):
                p[x, y] = edge
            if rng.random() < 0.45:
                p[x, 2] = edge
            if rng.random() < 0.45:
                p[x, n - 3] = edge
            if rng.random() < 0.2:                                # dry sand spilling onto the trail
                p[x, 3] = hexc(SAND[1])
            if rng.random() < 0.2:
                p[x, n - 4] = hexc(SAND[1])
    return im


def build_tiles():
    os.makedirs(TILE_OUT, exist_ok=True)
    made = {"GroundSand": sand_tile(), "RoadSand": road_tile(True), "RoadSandFill": road_tile(False)}
    for name, im in made.items():
        im.save(os.path.join(TILE_OUT, name + ".png"))
    return made


# ================================================================ sprites
def to_sand(px):
    r, g, b, a = px
    if not a or (r, g, b) == INK:
        return px
    if g > r + 8 and g > b + 8:                                   # the few grass pixels become dry beach grass
        return (int(g * 0.9 + 40), int(g * 0.95 + 20), int(b * 0.6 + 40), a)
    lum = (r * 3 + g * 5 + b * 2) / 10
    return (min(255, int(r * 0.55 + lum * 0.4 + 28)), min(255, int(g * 0.6 + lum * 0.38 + 30)), min(255, int(b * 0.7 + lum * 0.25 + 26)), a)


def sand_hill():
    src = next(s for s in base.build_props() if s.name == "PropHill")
    s = Sprite("PropHillSand", "Props", src.w, src.h, src.layers, "The hill, built of sand")
    for tag, frames, ms, repeat in src.tags:
        new = []
        for f in frames:
            L = dict(f)
            body = L["Body"].copy()
            p = body.load()
            for y in range(body.height):
                for x in range(body.width):
                    p[x, y] = to_sand(p[x, y])
            for x, y in ((14, 40), (48, 44), (22, 50), (40, 52)):  # shells pressed into its skirt
                if p[x, y][3]:
                    p[x, y] = hexc("#fff1d6")
                    p[x + 1, y] = hexc("#ff6f7d")
            L["Body"] = body
            new.append(L)
        s.tag(tag, new, ms, repeat)
    return s


def build_sprites():
    out = [sand_hill()]

    # ---- the water's edge
    s = prop("WaterShore", 32, "Shoreline: sand above, water below")

    def shore(L, k, corner=0):
        b = L["Body"]
        breathe = (0, 1, 2, 1)[k]
        for x in range(32):
            if corner == 0:
                line = 14 + math.sin(x * 2 * math.pi / 32) * 1.5
            elif corner < 0:                                      # the water wraps round to the left
                line = 14 + max(0, 12 - x) * -1.4
            else:
                line = 14 + max(0, x - 19) * -1.4
            line += breathe
            for y in range(32):
                if y < line - 3:
                    continue                                      # dry sand: left clear, the ground tile shows
                if y < line:
                    b.px(x, y, WET[0] if y > line - 2 else WET[1], a=255)    # damp sand
                elif y < line + 1.5:
                    b.px(x, y, "w")                               # foam
                elif y < line + 3 and (x + k) % 3:
                    b.px(x, y, "b")
                else:
                    b.px(x, y, "B" if (x * 3 + y * 5 + k * 4) % 17 else "b")
        for j in range(3):                                        # glints drifting on the water
            L["FX"].px((j * 11 + k * 4) % 32, 24 + j * 3, "w")

    loop(s, "Edge", 4, shore, 280)
    loop(s, "CornerLeft", 4, lambda L, k: shore(L, k, -1), 280)
    loop(s, "CornerRight", 4, lambda L, k: shore(L, k, 1), 280)
    out.append(s)

    # ---- reeds
    s = Sprite("PlantReed", "Props", 32, 48, ["Shadow", "Body", "FX"], "Reeds")

    def reeds(L, k):
        sway = (0, 1, 2, 1)[k]
        L["Shadow"].blob(16, 44, 10, 3, (0, 0, 0), a=70)
        b = L["Body"]
        for x, hgt, lean in ((8, 30, -2), (13, 38, -1), (18, 40, 1), (23, 32, 2), (11, 22, -3), (21, 24, 3)):
            top = (x + lean + sway * hgt / 30, 44 - hgt)
            b.line([(x, 44), (x + lean * 0.4, 44 - hgt * 0.5), top], "M")
            b.line([(x + 1, 44), (x + 1 + lean * 0.4, 44 - hgt * 0.5), (top[0] + 1, top[1] + 1)], "j")
            if hgt > 28:                                          # a cattail head
                head = Cv(32, 48)
                for dy in range(7):
                    head.px(top[0], top[1] + dy, "d")
                    head.px(top[0] + 1, top[1] + dy, "D")
                head.outline()
                b.paste(head)
                b.px(top[0], top[1] - 2, "T")
        for x, lean in ((5, -4), (26, 4), (15, 0)):               # broad leaves at the foot
            blade(b, x, 44, 12, lean + sway, "m", "g")

    loop(s, "Sway", 4, reeds, 260)
    out.append(s)

    # ---- beach grass
    s = prop("PlantBeachGrass", 32, "Beach grass")

    def tuft(L, k):
        sway = (0, 1, 2, 1)[k]
        L["Shadow"].blob(16, 26, 10, 3, (0, 0, 0), a=65)
        pale, mid, dark = "#cfd29a", "#a9b07a", "#7d8556"
        for j in range(13):
            ang = math.radians(-160 + j * 11.5)
            length = 12 + (j * 7) % 6
            tip = (16 + math.cos(ang) * length + sway * (1 if j > 6 else 0.4), 26 + math.sin(ang) * length)
            L["Body"].line([(16 + (j - 6) * 0.4, 26), tip], pale if j % 3 == 0 else mid if j % 3 == 1 else dark)
        L["Body"].line([(12, 26), (20, 26)], dark)

    loop(s, "Sway", 4, tuft, 240)
    out.append(s)

    # ---- shells
    s = prop("DecorShell", 16, "Shells")

    def shell(L, k, kind):
        L["Shadow"].blob(8.5, 11.5, 4.5, 1.8, (0, 0, 0), a=60)
        b = Cv(16, 16)
        if kind == "A":                                           # a fan shell
            for x in range(3, 13):
                top = 5 + abs(x - 7.5) ** 1.6 * 0.35
                for y in range(int(top), 11 - int(abs(x - 7.5) > 3.5)):
                    b.px(x, y, "c" if x % 2 else "f")
            b.outline()
            b.px(7, 11, "T")
        elif kind == "B":                                         # a spiral
            b.blob(8, 8.5, 4, 3.4, "t", "c", "T")
            b.outline()
            b.ring(8, 8.5, 2.6, "T", 1, gaps=lambda d: d < 60)
            b.px(8, 8, "d")
        else:                                                     # broken bits
            for x, y in ((5, 8), (10, 7), (8, 11)):
                one = Cv(16, 16)
                one.blob(x, y, 1.8, 1.3, "c", "w", "t")
                one.outline()
                b.paste(one)
        L["Body"].paste(b)

    for kind in "ABC":
        loop(s, kind, 1, lambda L, k, kind=kind: shell(L, k, kind), 1000)
    out.append(s)

    # ---- driftwood
    s = prop("DecorDriftwood", 32, "Driftwood", w=48)

    def drift(L, k, kind):
        L["Shadow"].blob(25, 21, 20, 5, (0, 0, 0), a=70)
        b = Cv(48, 32)
        grey = ("s", "c", "S")
        if kind == "A":
            for dy in (0, 1, 2):
                thick_line(b, [(5, 17 + dy), (20, 14 + dy), (43, 16 + dy)], grey)
            thick_line(b, [(20, 14), (27, 7), (33, 6)], grey)
            thick_line(b, [(30, 17), (36, 23)], grey)
        else:
            for dy in (0, 1):
                thick_line(b, [(6, 12 + dy), (24, 19 + dy), (42, 13 + dy)], grey)
            thick_line(b, [(24, 19), (22, 26)], grey)
        b.outline()
        for x in (12, 26, 36):
            b.px(x, 16 if kind == "A" else 15, "x")
        L["Body"].paste(b)

    for kind in "AB":
        loop(s, kind, 1, lambda L, k, kind=kind: drift(L, k, kind), 1000)
    out.append(s)

    # ---- river stones
    s = prop("RockRiver", 32, "River stones")

    def stone(L, k, kind, wet=False):
        L["Shadow"].blob(17, 21, 12, 5, (0, 0, 0), a=75)
        b = Cv(32, 32)
        if kind == "A":
            b.blob(16, 17, 11, 7, "s", "c", "S")
        else:
            b.blob(11, 18, 7, 5, "S", "s", "x")
            b.blob(21, 16, 8, 6, "s", "c", "S")
            b.blob(16, 22, 4, 2.6, "d", "T", "D")
        b.outline()
        b.line([(9, 18), (16, 20), (24, 18)], "S")                # a band of darker stone
        L["Body"].paste(b)
        if wet:
            x = 8 + k * 5
            for d in range(3):
                L["FX"].line([(x + d, 12), (x + d - 2, 16)], "w", a=(150, 240, 150)[d])
            L["Body"].ring(16, 20, 12.5, "B", 1, a=150, ry=6.5)   # a ring of damp sand

    loop(s, "A", 1, lambda L, k: stone(L, k, "A"), 1000)
    loop(s, "B", 1, lambda L, k: stone(L, k, "B"), 1000)
    loop(s, "Wet", 4, lambda L, k: stone(L, k, "A", True), 200)
    out.append(s)

    # ---- water strider
    s = prop("CritterStrider", 24, "Water strider (faces right)")

    def strider(L, k):
        glide = (0, 1, 2, 2, 1, 0)[k]
        cx, cy = 10 + glide, 12.5
        ink = Cv(24, 24)
        for ax, fx, fy in ((2, 6, 8), (0, -1, 9), (-2, -7, 7)):   # long legs out to dimples on the water
            for side in (-1, 1):
                foot = (cx + ax + fx - glide * 0.5, cy - 0.5 + side * fy)
                ink.line([(cx + ax, cy - 0.5), foot], "k")
                L["Shadow"].ring(foot[0], foot[1] + 0.5, 2 + (k % 2) * 0.5, "w", 1, a=150, ry=1.2)
        body = Cv(24, 24)
        body.blob(cx, cy, 4.5, 1.5, "x", "S", "k")
        body.outline()
        ink.paste(body)
        L["Body"].paste(ink)
        L["Body"].px(cx + 4, cy - 1, "w")

    loop(s, "Skate", 6, strider, 110)
    out.append(s)
    return out


# ================================================================ run
def main():
    tiles = build_tiles()
    sprites = build_sprites()
    results = []
    for s in sprites:
        results.append((s, export(s)))
        n = sum(len(f) for _, f, _, _ in s.tags)
        print(f"{s.group:6} {s.name:18} {s.w}x{s.h}  {n:2} frames  " + ", ".join(t[0] for t in s.tags))
    # a small composed view: sand, a trail across it, the shore along the bottom, and one of everything
    W, H = 384, 224
    im = Image.new("RGBA", (W, H))
    for y in range(0, H, 64):
        for x in range(0, W, 64):
            im.paste(tiles["GroundSand"], (x, y))
    for x in range(0, W, 32):
        im.alpha_composite(tiles["RoadSand"], (x, 72))
    comps = {s.name: (s, comps) for s, (rows, comps) in results}

    def put(name, index, x, y):
        s, c = comps[name]
        layer = Image.new("RGBA", (W, H))
        layer.paste(c[index], (int(x - s.w / 2), int(y - s.h / 2)))
        im.alpha_composite(layer)

    for x in range(0, W, 32):
        put("WaterShore", 0, x + 16, 208)
    put("PropHillSand", 0, 340, 88)
    put("PlantReed", 0, 40, 170); put("PlantReed", 2, 300, 176)
    put("PlantBeachGrass", 0, 110, 40); put("PlantBeachGrass", 2, 220, 150)
    put("DecorShell", 0, 150, 140); put("DecorShell", 1, 60, 50); put("DecorShell", 2, 260, 44)
    put("DecorDriftwood", 0, 150, 170); put("DecorDriftwood", 1, 250, 36)
    put("RockRiver", 0, 30, 30); put("RockRiver", 2, 200, 190)
    put("CritterStrider", 0, 100, 212); put("CritterStrider", 3, 330, 214)
    im.resize((W * 3, H * 3), Image.NEAREST).save(os.path.join(HERE, "RiverbankPreview.png"))
    total = sum(sum(len(f) for _, f, _, _ in s.tags) for s, _ in results)
    print(f"3 tiles and {len(results)} sprites, {total} frames written and read back OK")


if __name__ == "__main__":
    main()
