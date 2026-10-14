"""Hold the Hill biomes, part two: the rest of the riverbank, and the forest floor.

Run build_biome_sheets.py first; this builds on it.

Riverbank additions (Props unless noted):
  PropBurrowSand, Building<Kind>Sand (x5)    the shared pieces in the riverbank's pale sand
  TerrainBridgeDrift (48x32)     Plank, Broken: driftwood laid across a channel
  WaterShallows (32x32)          Flow: open water that tiles both ways, for the river itself
  LightShellLamp (32x32)         Glow: a glow-worm curled in a spiral shell
  LightReedTorch (32x32)         Lit: a burning bundle of reeds
  DecorRiver (16x16)             Feather, Weed, Tracks, Ripple, Pebbles

The forest floor:
  ../../Tiles/Forest/GroundLitter.png (64x64), RoadEarth.png, RoadEarthFill.png (32x32)
  PropHillForest, PropBurrowForest, Building<Kind>Forest (x5)   the shared pieces in dark loam
  PlantToadstool (32x32)         Idle, Puff
  PlantMoss (32x32)              A, B
  PlantSapling (32x48)           Sway
  ObjFallenLog (64x32)           Idle
  DecorLitter (16x16)            LeafA, LeafB, Needles, AcornCap, Cone
  LightFoxfire (32x32)           Glow: glowing fungus on a stump
  CritterPillbug (16x16)         Walk, Roll. Faces right.
  Fx/FxDapple (64x64)            Sun: patches of light moving as the leaves overhead move

Both biomes in winter and at night:
  ../../Tiles/Riverbank/ and ../../Tiles/Forest/: each tile also as ...Winter and ...Night
  <Name>Winter and <Name>Night for every prop unique to either biome

    python build_biome_more_sheets.py
"""
import math
import os
import random

from PIL import Image

import build_anim_sheets as base
import build_biome_sheets as river
import build_scenery_sheets as scenery
import build_season_sheets as season
from build_anim_sheets import Cv, Sprite, SPARK_S, export, flat
from build_extra_sheets import blade, leaf, thick_line
from build_scenery_sheets import flame, glow, loop, prop

HERE = os.path.dirname(os.path.abspath(__file__))
FOREST_OUT = os.path.normpath(os.path.join(HERE, "..", "..", "Tiles", "Forest"))
INK = base.rgba("k")[:3]


def hexc(h):
    h = h.lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), 255)


def recoloured(src, suffix, fn, caste, touch=None):
    s = Sprite(src.name + suffix, src.group, src.w, src.h, src.layers, caste)
    for tag, frames, ms, repeat in src.tags:
        new = []
        for f in frames:
            L = dict(f)
            body = L["Body"].copy()
            p = body.load()
            for y in range(body.height):
                for x in range(body.width):
                    p[x, y] = fn(p[x, y])
            if touch:
                touch(body)
            L["Body"] = body
            new.append(L)
        s.tag(tag, new, ms, repeat)
    return s


def to_loam(px):
    r, g, b, a = px
    if not a or (r, g, b) == INK:
        return px
    if g > r + 8 and g > b + 8:
        return (int(r * 0.8), int(g * 0.85 + 6), int(b * 0.8), a)   # greens deepen into moss
    return (int(r * 0.6 + 8), int(g * 0.58 + 12), int(b * 0.58 + 10), a)


def moss_on(body):
    """Moss creeping over the upper left of whatever is drawn."""
    p = body.load()
    rng = random.Random(body.width * 7 + body.height)
    for _ in range(body.width * 2):
        x, y = rng.randrange(body.width // 2 + 4), rng.randrange(body.height // 2 + 4)
        if p[x, y][3] and p[x, y][:3] != INK:
            p[x, y] = hexc("#3f7a24") if rng.random() < 0.6 else hexc("#7cc84a")


# ================================================================ riverbank additions
def build_river_more():
    out = []
    shared = {s.name: s for s in base.build_props() + scenery.build_buildings()}
    out.append(recoloured(shared["PropBurrow"], "Sand", river.to_sand, "Enemy burrow in sand"))
    for name in ("BuildingGranary", "BuildingNursery", "BuildingBarracks", "BuildingWatchPost", "BuildingFungusFarm"):
        out.append(recoloured(shared[name], "Sand", river.to_sand, shared[name].caste + ", in sand"))

    s = prop("TerrainBridgeDrift", 32, "Driftwood bridge", w=48)

    def planks(L, k, broken=False):
        for x in range(17, 31):                                   # a strip of the channel beneath, for context
            for y in range(32):
                L["Shadow"].px(x, y, "B" if 19 <= x < 29 else "n")
        b = Cv(48, 32)
        grey = ("s", "c", "S")
        for j, y in enumerate((8, 12, 16, 20, 24)):
            if broken and j in (1, 2, 3):
                thick_line(b, [(3, y), (15 + j, y + 1)], grey)
                thick_line(b, [(33 - j, y), (45, y - 1)], grey)
            else:
                thick_line(b, [(3, y + (j % 2)), (45, y - (j % 2))], grey)
        b.outline()
        for x in (10, 24, 38):
            b.px(x, 12, "x")
        L["Body"].paste(b)
        if broken:
            L["FX"].ring(24, 16, 3 + k, "w", 1, a=200 - k * 45, ry=2 + k * 0.6)

    loop(s, "Plank", 1, planks, 1000)
    loop(s, "Broken", 4, lambda L, k: planks(L, k, True), 160)
    out.append(s)

    s = prop("WaterShallows", 32, "Open water (tiles both ways)")

    def shallows(L, k):
        b = L["Body"]
        for y in range(32):
            for x in range(32):
                b.px(x, y, "B" if (x * 3 + y * 5) % 17 else "b")
        for j in range(6):                                        # glints sliding a quarter tile a frame
            x, y = (j * 11 + k * 8) % 32, (j * 7 + 3) % 32
            for d in range(3):
                b.px((x + d) % 32, y, "w", a=230 - d * 60)
        for j in range(3):                                        # darker deeps
            b.px((j * 13 + 5) % 32, (j * 9 + 14) % 32, "n")

    loop(s, "Flow", 4, shallows, 140)
    out.append(s)

    s = prop("LightShellLamp", 32, "Glow-worm in a shell")

    def lamp(L, k):
        L["Shadow"].blob(17, 22, 8, 3, (0, 0, 0), a=70)
        glow(L["FX"], 16, 17, 11 + (0, 1, 2, 1)[k], "g")
        b = Cv(32, 32)
        b.blob(16, 17, 7, 6, "t", "c", "T")
        b.outline()
        b.ring(16, 17, 4.6, "T", 1, gaps=lambda d: d < 50)
        b.ring(16, 17, 2.4, "d", 1, gaps=lambda d: 180 < d < 250)
        L["Body"].paste(b)
        L["FX"].blob(20, 19, 2.2, 1.6, "g", "w", "G")              # the worm, at the mouth of the shell
        if k == 2:
            L["FX"].px(22, 16, "w")

    loop(s, "Glow", 4, lamp, 240)
    out.append(s)

    s = prop("LightReedTorch", 32, "Burning reed bundle")

    def torch(L, k):
        L["Shadow"].blob(17, 25, 5, 2.2, (0, 0, 0), a=75)
        b = Cv(32, 32)
        for dx, col in ((-2, "M"), (-1, "m"), (0, "M"), (1, "j")):
            b.line([(16 + dx, 14), (16 + dx * 0.5, 25)], col)
        b.line([(13, 18), (18, 18)], "T")
        b.line([(14, 22), (17, 22)], "T")
        b.outline()
        L["Body"].paste(b)
        glow(L["FX"], 16, 12, 9 + (k % 2), "o")
        flame(L["FX"], 16, 13, 7 + (k % 2), k)
        flame(L["FX"], 14, 14, 4 + ((k + 1) % 2), k + 1)

    loop(s, "Lit", 4, torch, 100)
    out.append(s)

    s = prop("DecorRiver", 16, "Things washed up")

    def feather(L, k):
        L["Shadow"].blob(8.5, 11, 5, 1.6, (0, 0, 0), a=55)
        leaf(L["Body"], 8, 8, 13, 5, 25, ("c", "w", "s"))
        L["Body"].line([(3, 6), (13, 10)], "S")

    def weed(L, k):
        for j, (x, lean) in enumerate(((5, -1), (8, 1), (11, 0))):
            sway = (0, 1, 0, -1)[(k + j) % 4]
            L["Body"].line([(x, 13), (x + lean, 9), (x + lean + sway, 5), (x + sway, 2)], "M")
            L["Body"].line([(x + 1, 13), (x + 1 + lean, 9), (x + 1 + lean + sway, 5)], "m")
            L["Body"].px(x + sway, 2, "g")

    def tracks(L, k):
        for x, y in ((3, 4), (8, 8), (4, 12), (11, 13)):          # a wading bird's three-toed prints
            for dx, dy in ((0, 0), (-1, -2), (0, -2), (1, -2)):
                L["Body"].px(x + dx, y + dy, river.WET[2])

    def ripple(L, k):
        for row in (4, 8, 12):
            for x in range(1, 15):
                y = row + round(math.sin(x * 0.9 + row) * 1)
                L["Body"].px(x, y, river.SAND[2])
                L["Body"].px(x, y - 1, river.SAND[1])

    def pebbles(L, k):
        for x, y, r in ((4, 6, 1.8), (10, 5, 2.2), (7, 10, 1.6), (12, 11, 1.4)):
            L["Shadow"].blob(x + 0.5, y + 1.2, r + 0.5, r * 0.6, (0, 0, 0), a=60)
            one = Cv(16, 16)
            one.blob(x, y, r, r * 0.8, "s", "c", "S")
            one.outline()
            L["Body"].paste(one)

    loop(s, "Feather", 1, feather, 1000)
    loop(s, "Weed", 4, weed, 260)
    loop(s, "Tracks", 1, tracks, 1000)
    loop(s, "Ripple", 1, ripple, 1000)
    loop(s, "Pebbles", 1, pebbles, 1000)
    out.append(s)
    return out


# ================================================================ forest floor
LITTER = ("#5a4630", "#6b5238", "#463524")
EARTH = ("#3f2f22", "#4c3a2a", "#2e2118")


def litter_tile(n=64):
    rng = random.Random(44)
    b, light, dark = (hexc(c) for c in LITTER)
    im = Image.new("RGBA", (n, n), b)
    p = im.load()
    for _ in range(90):
        cx, cy, r = rng.randrange(n), rng.randrange(n), rng.uniform(2, 5)
        c = light if rng.random() < 0.5 else dark
        for y in range(-6, 7):
            for x in range(-6, 7):
                if x * x + y * y <= r * r and rng.random() < 0.75:
                    p[(cx + x) % n, (cy + y) % n] = c
    leaves = ["#7a4a24", "#8a5a2a", "#6a3a20", "#93702e", "#55602a", "#a0562a"]
    for _ in range(24):                                           # fallen leaves: small pointed ovals at every angle
        cx, cy = rng.randrange(n), rng.randrange(n)
        ang = rng.uniform(0, math.pi)
        col = hexc(rng.choice(leaves))
        col = tuple((c + bc) // 2 for c, bc in zip(col[:3], b[:3])) + (255,)   # half-way to the ground colour: a floor, not confetti
        length, width = rng.uniform(2.5, 4.5), rng.uniform(1.2, 2)
        for y in range(-5, 6):
            for x in range(-5, 6):
                u = (x * math.cos(ang) + y * math.sin(ang)) / length
                v = (-x * math.sin(ang) + y * math.cos(ang)) / width
                if abs(u) <= 1 and abs(v) <= (1 - u * u) ** 0.7:
                    shade = tuple(max(0, c - 12) for c in col[:3]) + (255,) if v > 0.3 else col
                    p[(cx + x) % n, (cy + y) % n] = shade
    for _ in range(22):                                           # pine needles
        x, y = rng.randrange(n), rng.randrange(n)
        dx, dy = rng.choice(((1, 0), (1, 1), (0, 1), (1, -1)))
        for d in range(rng.randint(2, 4)):
            p[(x + dx * d) % n, (y + dy * d) % n] = hexc("#6f6038")
    return im


def earth_tile(edges=True, n=32):
    rng = random.Random(15)
    b, light, dark = (hexc(c) for c in EARTH)
    im = Image.new("RGBA", (n, n), b)
    p = im.load()
    for y in range(n):
        for x in range(n):
            r = rng.random()
            if r < 0.1:
                p[x, y] = light
            elif r < 0.24:
                p[x, y] = dark
    for lane in (11, 20):
        for x in range(n):
            if rng.random() < 0.5:
                p[x, lane] = dark
    y = 15                                                        # a root crossing the trail lengthwise, wrapping
    for x in range(n):
        if x % 5 == 0 and 2 < x < n - 3:
            y = max(8, min(23, y + rng.choice((-1, 0, 1))))
        p[x, 15 if x < 2 or x > n - 3 else y] = hexc("#6b5238")
    if edges:
        moss, moss_d = hexc("#3f7a24"), hexc("#2f5a1c")
        for x in range(n):
            for yy in (0, 1, n - 2, n - 1):
                p[x, yy] = moss_d if (x + yy) % 3 else moss
            if rng.random() < 0.5:
                p[x, 2] = moss
            if rng.random() < 0.5:
                p[x, n - 3] = moss
    return im


def build_forest_tiles():
    os.makedirs(FOREST_OUT, exist_ok=True)
    made = {"GroundLitter": litter_tile(), "RoadEarth": earth_tile(True), "RoadEarthFill": earth_tile(False)}
    for name, im in made.items():
        im.save(os.path.join(FOREST_OUT, name + ".png"))
    return made


def build_forest():
    out = []
    shared = {s.name: s for s in base.build_props() + scenery.build_buildings()}
    out.append(recoloured(shared["PropHill"], "Forest", to_loam, "The hill, in forest loam", touch=moss_on))
    out.append(recoloured(shared["PropBurrow"], "Forest", to_loam, "Enemy burrow in loam"))
    for name in ("BuildingGranary", "BuildingNursery", "BuildingBarracks", "BuildingWatchPost", "BuildingFungusFarm"):
        out.append(recoloured(shared[name], "Forest", to_loam, shared[name].caste + ", in loam", touch=moss_on))

    s = prop("PlantToadstool", 32, "Toadstools")

    def toadstools(L, k, puff=0):
        L["Shadow"].blob(17, 24, 11, 4, (0, 0, 0), a=75)
        for x, y, r, h in ((10, 22, 5, 8), (21, 20, 7, 11), (15, 25, 3.5, 5)):
            bob = (0, 1, 0, 0)[k] if puff == 0 and r > 6 else 0
            stalk = Cv(32, 32)
            stalk.blob(x, y - h * 0.3, 1.6, h * 0.5, "c", "w", "t")
            stalk.outline()
            L["Body"].paste(stalk)
            cap = Cv(32, 32)
            squash = 0.25 if puff in (1, 2) and r > 6 else 0
            cap.blob(x, y - h + bob + squash * 2, r + squash * 2, r * (0.62 - squash * 0.3), "r", "f", "R")
            cap.outline()
            for dx, dy in ((-2, -1), (2, 0), (0, 1), (-3, 1)):
                if abs(dx) < r - 1:
                    cap.px(x + dx, y - h + bob + dy + squash * 2, "c")
            L["Body"].paste(cap)
        if puff:
            for j in range(8):
                ang = math.radians(200 + j * 20)
                d = 3 + puff * 3
                L["FX"].px(21 + math.cos(ang) * d, 9 + math.sin(ang) * d * 0.7, "c" if j % 2 else "t", a=255 - puff * 40)
        elif k == 2:
            L["FX"].px(24, 6, "c")

    loop(s, "Idle", 4, toadstools, 260)
    loop(s, "Puff", 5, lambda L, k: toadstools(L, 0, k + 1), 90, repeat=1)
    out.append(s)

    s = prop("PlantMoss", 32, "Moss cushions")
    for tag, lumps in (("A", [(16, 17, 10, 6)]), ("B", [(10, 15, 6, 4), (21, 19, 8, 5), (14, 23, 4, 2.6)])):
        def moss(L, k, lumps=lumps):
            rng = random.Random(len(lumps))
            for x, y, rx, ry in lumps:
                L["Shadow"].blob(x + 1, y + ry * 0.6, rx + 0.5, ry * 0.6, (0, 0, 0), a=65)
                m = Cv(32, 32)
                m.blob(x, y, rx, ry, "M", "m", "j")
                m.outline("j")
                L["Body"].paste(m)
                for _ in range(int(rx * 2)):                      # a nap of lighter tips
                    px_, py_ = x + rng.uniform(-rx, rx) * 0.8, y + rng.uniform(-ry, ry) * 0.7
                    L["Body"].px(px_, py_, "g" if rng.random() < 0.3 else "m")
        loop(s, tag, 1, moss, 1000)
    out.append(s)

    s = Sprite("PlantSapling", "Props", 32, 48, ["Shadow", "Body", "FX"], "Sapling")

    def sapling(L, k):
        sway = (0, 1, 2, 1)[k]
        L["Shadow"].blob(16, 44, 8, 3, (0, 0, 0), a=70)
        b = L["Body"]
        thick_line(b, [(16, 44), (15, 30), (16 + sway * 0.5, 16)], ("d", "T", "D"))
        for j, (y, side, length) in enumerate(((34, -1, 7), (30, 1, 8), (25, -1, 6), (21, 1, 6), (17, -1, 4))):
            tip = (16 + side * length + sway * (1 - j / 6), y - 3)
            b.line([(15.5, y), tip], "D")
            leaf(b, tip[0] + side * 2, tip[1] - 1, 8, 4.5, -25 * side + 180 * (side < 0), ("m", "g", "M"))
        leaf(b, 16 + sway, 12, 9, 5, -90, ("m", "g", "M"))

    loop(s, "Sway", 4, sapling, 280)
    out.append(s)

    s = prop("ObjFallenLog", 32, "Fallen log with bracket fungus", w=64)

    def log(L, k):
        L["Shadow"].blob(33, 23, 29, 6, (0, 0, 0), a=85)
        b = Cv(64, 32)
        for y in range(8, 24):
            for x in range(6, 58):
                b.px(x, y, "#6b5238" if y < 11 else "#5a4630" if y < 18 else "#463524")
        b.blob(57, 16, 5, 8, "t", "c", "T")
        b.blob(6, 16, 4, 8, "#463524", "#5a4630", "#2e2118")
        b.outline()
        b.blob(57, 16, 2.6, 4.6, "#2e2118")
        b.ring(57, 16, 4, "T", 1, ry=6.5)
        for x in (14, 24, 35, 46):
            b.line([(x, 10), (x + 2, 14), (x + 1, 21)], "#2e2118")
        for x, w in ((12, 9), (30, 12), (44, 6)):                 # moss along the top
            b.blob(x + w / 2, 9, w / 2, 2, "M", "m", "j")
        L["Body"].paste(b)
        for x, y in ((20, 21), (27, 22), (40, 21)):               # bracket fungi stepping down the side
            f = Cv(64, 32)
            f.blob(x, y, 3.4, 1.6, "t", "c", "T")
            f.outline()
            L["Body"].paste(f)

    loop(s, "Idle", 1, log, 1000)
    out.append(s)

    s = prop("DecorLitter", 16, "Forest litter")
    loop(s, "LeafA", 1, lambda L, k: (L["Shadow"].blob(8.5, 10.5, 5, 2, (0, 0, 0), a=55), leaf(L["Body"], 8, 8, 12, 6, 30, ("o", "u", "R"))), 1000)
    loop(s, "LeafB", 1, lambda L, k: (L["Shadow"].blob(8.5, 10.5, 5, 2, (0, 0, 0), a=55), leaf(L["Body"], 8, 8, 11, 7, -50, ("d", "T", "D"))), 1000)

    def needles(L, k):
        for j, ang in enumerate((20, 75, 130, 160, 100)):
            a = math.radians(ang)
            x, y = 4 + j * 2, 12 - (j % 2) * 2
            L["Body"].line([(x, y), (x + math.cos(a) * 7, y - math.sin(a) * 7)], "#8a7a44" if j % 2 else "#6b6a2e")

    def cap(L, k):
        L["Shadow"].blob(8.5, 11, 4.5, 1.8, (0, 0, 0), a=60)
        c = Cv(16, 16)
        c.blob(8, 8.5, 4, 3, "d", "T", "D")
        c.outline()
        c.blob(8, 8.5, 2.2, 1.5, "e")
        for x in (5, 7, 9, 11):
            c.px(x, 6, "T")
        L["Body"].paste(c)

    def cone(L, k):
        L["Shadow"].blob(8.5, 12, 5.5, 2, (0, 0, 0), a=65)
        c = Cv(16, 16)
        c.blob(8, 8.5, 5.4, 3.6, "d", "T", "D")
        c.outline()
        for row in range(3):                                      # scales
            for x in range(4 + (row % 2), 13, 2):
                c.px(x, 6 + row * 2, "e")
                c.px(x, 5 + row * 2, "T")
        L["Body"].paste(c)

    loop(s, "Needles", 1, needles, 1000)
    loop(s, "AcornCap", 1, cap, 1000)
    loop(s, "Cone", 1, cone, 1000)
    out.append(s)

    s = prop("LightFoxfire", 32, "Foxfire on a stump")

    def foxfire(L, k):
        L["Shadow"].blob(17, 23, 10, 4, (0, 0, 0), a=80)
        glow(L["FX"], 16, 17, 12 + (0, 1, 2, 1)[k], "g")
        b = Cv(32, 32)
        b.blob(16, 18, 9, 7, "#5a4630", "#6b5238", "#2e2118")
        b.outline()
        b.blob(16, 17, 6, 4.5, "t", "c", "T")                     # the cut top
        b.ring(16, 17, 4, "T", 1, ry=3)
        b.ring(16, 17, 2, "T", 1, ry=1.4)
        L["Body"].paste(b)
        for j, (x, y) in enumerate(((8, 20), (11, 23), (22, 22), (25, 19), (19, 12))):   # the glowing caps
            f = Cv(32, 32)
            f.blob(x, y, 1.8, 1.3, "g", "w", "G")
            f.outline("G")
            L["FX"].paste(f)
            if (j + k) % 4 == 0:
                L["FX"].px(x, y - 3, "w")

    loop(s, "Glow", 4, foxfire, 240)
    out.append(s)

    s = prop("CritterPillbug", 16, "Pill bug (faces right)")

    def bug(L, k, curl=0.0):
        L["Shadow"].blob(8, 10.5, 5 - curl, 2, (0, 0, 0), a=70)
        ink = Cv(16, 16)
        if curl < 0.5:
            for j in range(5):                                    # little legs
                off = round(math.sin(k * 1.6 + j) * 1)
                ink.px(4 + j * 2 + off, 5, "k")
                ink.px(4 + j * 2 - off, 11, "k")
            ink.line([(12, 7), (14, 5 + (k % 2))], "k")
            ink.line([(12, 9), (14, 10 - (k % 2))], "k")
        body = Cv(16, 16)
        body.blob(8, 8.5, 5 - curl * 1.4, 3 + curl * 0.9, "S", "s", "x")
        body.outline()
        if curl < 0.9:
            for x in (5, 7, 9, 11):                               # the plates
                body.line([(x - curl * (x - 8) * 0.3, 6 + (1 if x in (5, 11) else 0)), (x - curl * (x - 8) * 0.3, 10 - (1 if x in (5, 11) else 0))], "x")
        else:
            body.ring(8, 8.5, 2.2, "x", 1)
        ink.paste(body)
        L["Body"].paste(ink)

    loop(s, "Walk", 4, bug, 110)
    loop(s, "Roll", 5, lambda L, k: bug(L, k, (0.2, 0.5, 0.8, 1.0, 1.0)[k]), 80, repeat=1)
    out.append(s)

    s = Sprite("FxDapple", "Fx", 64, 64, ["FX"], "Dappled sunlight")
    spots = [(random.Random(j).randint(8, 56), random.Random(30 + j).randint(8, 56), random.Random(60 + j).uniform(4, 9)) for j in range(7)]
    frames = []
    for k in range(6):
        cv = Cv(64, 64)
        for j, (x, y, r) in enumerate(spots):
            ph = k / 6 * 2 * math.pi + j
            glow(cv, x + math.sin(ph) * 2, y + math.cos(ph * 0.7) * 1.5, r + math.sin(ph * 1.3) * 1, "y", 0.9)
        frames.append({"FX": cv.im})
    s.tag("Sun", frames, 260)
    out.append(s)
    return out


# ================================================================ both biomes in winter and at night
def seasonal_tiles():
    made = []
    river_tiles = {"GroundSand": river.sand_tile(), "RoadSand": river.road_tile(True), "RoadSandFill": river.road_tile(False)}
    forest_tiles = {"GroundLitter": litter_tile(), "RoadEarth": earth_tile(True), "RoadEarthFill": earth_tile(False)}
    for folder, tiles in ((river.TILE_OUT, river_tiles), (FOREST_OUT, forest_tiles)):
        for name, im in tiles.items():
            ground = name.startswith("Ground")
            winter = season.recolour(im, season.winter) if name.endswith("Fill") else season.tile_winter(im, road=not ground)
            night = season.recolour(im, season.night)
            winter.save(os.path.join(folder, name + "Winter.png"))
            night.save(os.path.join(folder, name + "Night.png"))
            made += [(name + "Winter", winter), (name + "Night", night)]
    return made


def main():
    river.build_tiles()
    build_forest_tiles()
    tiles = seasonal_tiles()
    summer = build_river_more() + build_forest()
    unique_river = [s for s in river.build_sprites()]
    results = []
    for s in summer:
        results.append((s, export(s)))
        n = sum(len(f) for _, f, _, _ in s.tags)
        print(f"{s.group:6} {s.name:22} {s.w}x{s.h}  {n:2} frames  " + ", ".join(t[0] for t in s.tags))
    count = len(summer)
    for s in summer + unique_river:
        for name in ("Winter", "Night"):
            export(season.seasonal(s, name, tint_layers=("Body",)))
            count += 1
    base.preview_sheet([r for r in results if r[0].name in ("TerrainBridgeDrift", "LightShellLamp", "LightReedTorch", "DecorRiver",
                                                           "PlantToadstool", "PlantMoss", "PlantSapling", "ObjFallenLog", "DecorLitter",
                                                           "LightFoxfire", "CritterPillbug")],
                       os.path.join(HERE, "BiomeMorePreview.png"), scale=3)
    print(f"{len(tiles)} seasonal tiles, {count} sprites written and read back OK")


if __name__ == "__main__":
    main()
