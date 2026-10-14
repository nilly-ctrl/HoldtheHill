"""Hold the Hill seasons and time of day.

Recolours what already exists rather than redrawing it, so every seasonal piece lines up with
its summer original, then adds the few things only a season has.

  ../../Tiles/Seasons/GroundGrass<Season>.png, RoadDirt<Season>.png, RoadDirtFill<Season>.png
        Autumn, Winter, Night versions of the three ground tiles (same size, still tile)
  Props/<Name>Autumn, <Name>Winter, <Name>Night
        PropHill, DecorGrass, PlantFern, PlantTallGrass, PlantClover, RockBoulder, with the
        same tags and frames as the original. Winter also lays snow on every upward edge.
  Towers/Tower<Name>[T2|T3]Autumn, ...Winter      every tower at every tier: only the mound changes
                         (turned leaves in autumn, snow on its rim, stones and banners in winter);
                         the ant and its effects are left alone
  Props/Building<Name>, Light<Name>, Obj<Name>, the rest of Decor, PropBurrow, TerrainBridge,
  TerrainCliff, TerrainTunnel, PlantDandelion      Autumn and Winter versions (lights: winter only)
  Props/DecorSnow        A, B, C: drifts
  Props/DecorLeafPile    A, B: raked-up autumn leaves (B has one skittering off)
  Props/WaterIce         Small, Large (frozen puddles), Crack
  Fx/FxSeason            32x32 pieces that tile: Snow, Leaves; and Breath (a puff of cold air)
  Fx/FxLightGlow is joined by Fx/FxMoonlight (64x64): Moon, Cloud (a shadow drifting across)

    python build_season_sheets.py
"""
import math
import os
import random

from PIL import Image

import build_anim_sheets as base
import build_extra_sheets as extra
import build_nature_sheets as nature
import build_tiles as tiles
from build_anim_sheets import Cv, Sprite, SPARK_S, export, flat
from build_extra_sheets import leaf
from build_scenery_sheets import glow, loop, prop

HERE = os.path.dirname(os.path.abspath(__file__))
TILE_OUT = os.path.normpath(os.path.join(HERE, "..", "..", "Tiles", "Seasons"))
INK = base.rgba("k")[:3]


# ================================================================ recolouring
def is_green(r, g, b):
    return g > r + 8 and g > b + 8


def autumn(px):
    r, g, b, a = px
    if not a or (r, g, b) == INK:
        return px
    if is_green(r, g, b):                                         # leaves turn; everything else stays
        return (min(255, int(g * 1.08 + 48)), int(g * 0.62 + 8), int(b * 0.3), a)
    return px


def winter(px):
    r, g, b, a = px
    if not a or (r, g, b) == INK:
        return px
    lum = (r * 3 + g * 5 + b * 2) / 10
    if is_green(r, g, b):                                         # greenery goes frost-pale
        return (int(lum * 0.45 + 120), int(lum * 0.5 + 128), int(lum * 0.5 + 140), a)
    return (int(r * 0.8 + lum * 0.12 + 14), int(g * 0.82 + lum * 0.12 + 18), int(b * 0.8 + lum * 0.14 + 34), a)   # a cold cast


def night(px):
    r, g, b, a = px
    if not a:
        return px
    if (r, g, b) == INK:
        return (10, 9, 20, a)
    return (int(r * 0.38 + 6), int(g * 0.45 + 12), int(b * 0.7 + 34), a)


SEASONS = {"Autumn": autumn, "Winter": winter, "Night": night}


def recolour(im, fn):
    out = im.copy()
    p = out.load()
    for y in range(out.height):
        for x in range(out.width):
            p[x, y] = fn(p[x, y])
    return out


# Thin things would vanish under two rows of snow, so they get one. Water is a tile: no snow line.
THIN = ("TerrainBridge", "TerrainBridgeDrift", "ObjThornFence", "DecorTwig", "DecorRoot", "PlantSapling", "PlantReed",
        "DecorDriftwood", "ObjWeb", "PlantTallGrass", "PlantBeachGrass")
NO_SNOW = ("WaterShallows", "WaterShore", "WaterIce", "DecorSnow")


def snow_rows(name):
    return 0 if name in NO_SNOW else 1 if name in THIN else 2


def leaf_fall(im, seed):
    """Autumn: a few fallen leaves lying on whatever is drawn (used on tower mounds, which have
    almost no green of their own to turn)."""
    out = im.copy()
    p = out.load()
    rng = random.Random(seed)
    placed = 0
    for _ in range(400):
        x, y = rng.randrange(1, im.width - 2), rng.randrange(im.height // 2, im.height - 2)
        if p[x, y][3] and p[x, y][:3] != INK and p[x + 1, y][3] and p[x + 1, y][:3] != INK:
            c = rng.choice(((226, 65, 47), (255, 138, 61), (255, 210, 58), (160, 86, 42)))
            p[x, y] = c + (255,)
            p[x + 1, y] = tuple(max(0, v - 40) for v in c) + (255,)
            placed += 1
            if placed >= 9:
                break
    return out


def snow_cap(im, depth=2, rows=2):
    """Snow on every pixel that has open sky above it, plus a blue-white row under that."""
    out = im.copy()
    src, p = im.load(), out.load()
    for x in range(im.width):
        for y in range(im.height):
            if not src[x, y][3] or src[x, y][:3] == INK:
                continue
            above = 0
            for d in range(1, depth + 2):
                if y - d < 0 or not src[x, y - d][3] or src[x, y - d][:3] == INK:
                    above = d
                    break
            if 1 <= above <= rows:                                # two solid rows read at game size; thin things get one
                p[x, y] = (255, 255, 255, src[x, y][3])
            elif above == rows + 1 and (x + y) % 3:
                p[x, y] = (214, 236, 250, src[x, y][3])
    return out


def moon_rim(im):
    """Night only: a cold light along every edge that faces up and to the left, where the moon is."""
    out = im.copy()
    src, p = im.load(), out.load()
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, a = src[x, y]
            if not a or (r, g, b) == (10, 9, 20):
                continue
            up = y == 0 or not src[x, y - 1][3] or src[x, y - 1][:3] == (10, 9, 20)
            left = x == 0 or not src[x - 1, y][3] or src[x - 1, y][:3] == (10, 9, 20)
            if up and left:
                p[x, y] = (200, 226, 255, a)
            elif up or left:
                p[x, y] = (min(255, r + 70), min(255, g + 84), min(255, b + 96), a)
    return out


def seasonal(sprite, season, layers_to_snow=("Body",), tint_layers=None, leaves=False):
    """tint_layers: which layers change colour (default: all but the shadow). Effects such as an
    acid glob should keep their colour whatever the season, so callers leave FX out."""
    fn = SEASONS[season]
    s = Sprite(sprite.name + season, sprite.group, sprite.w, sprite.h, sprite.layers, f"{sprite.caste}, {season.lower()}")
    for tag, frames, ms, repeat in sprite.tags:
        new = []
        for f in frames:
            L = {}
            for name, im in f.items():
                if name == "Shadow" or (tint_layers is not None and name not in tint_layers):
                    L[name] = im
                    continue
                im2 = recolour(im, fn)
                rows = snow_rows(sprite.name)
                if season == "Winter" and name in layers_to_snow and rows:
                    im2 = snow_cap(im2, rows=rows)
                if season == "Autumn" and leaves and name in layers_to_snow:
                    im2 = leaf_fall(im2, sprite.name)
                if season == "Night" and name in layers_to_snow and rows:   # not on water: it is a tile
                    im2 = moon_rim(im2)
                L[name] = im2
            new.append(L)
        s.tag(tag, new, ms, repeat)
    return s


# ================================================================ tiles
def tile_winter(im, road):
    """Tiles have no sky above them, so snow is laid by brightness instead: grass almost buried,
    the trail trodden down to frozen mud with snow along its edges."""
    out = recolour(im, winter)
    p = out.load()
    src = im.load()
    rng = random.Random(8)
    for y in range(out.height):
        for x in range(out.width):
            r, g, b, a = p[x, y]
            if road:
                edge = min(y, out.height - 1 - y)
                if edge < 3 or (edge < 6 and rng.random() < 0.35):
                    p[x, y] = (236, 244, 250, a) if rng.random() < 0.8 else (200, 222, 240, a)
            else:
                # follow the grass tile's own mottling, so the snow has soft drifts rather than static
                sr, sg, sb, _ = src[x, y]
                lum = (sr * 3 + sg * 5 + sb * 2) / 10
                if lum > 108:
                    p[x, y] = (255, 255, 255, a)
                elif lum > 92:
                    p[x, y] = (238, 246, 252, a)
                else:
                    p[x, y] = (208, 228, 244, a) if rng.random() < 0.85 else (150, 176, 150, a)   # a blade showing through
    return out


def build_tiles():
    os.makedirs(TILE_OUT, exist_ok=True)
    made = []
    sources = {"GroundGrass": tiles.grass(), "RoadDirt": tiles.road(True), "RoadDirtFill": tiles.road(False)}
    for name, im in sources.items():
        for season, fn in SEASONS.items():
            if season == "Winter":
                out = tile_winter(im, road=name != "GroundGrass")
                if name == "RoadDirtFill":
                    out = recolour(im, winter)                   # the fill has no edges to pile snow on
            elif season == "Autumn" and name == "GroundGrass":
                out = Image.blend(im, recolour(im, autumn), 0.6)   # full strength shouts over the sprites
                p = out.load()
                rng = random.Random(3)
                for _ in range(40):                               # fallen leaves scattered on the grass
                    x, y = rng.randrange(out.width), rng.randrange(out.height)
                    c = rng.choice(((226, 65, 47), (255, 138, 61), (255, 210, 58), (131, 86, 47)))
                    p[x, y] = c + (255,)
                    p[(x + 1) % out.width, y] = c + (255,)
            else:
                out = recolour(im, fn)
            out.save(os.path.join(TILE_OUT, f"{name}{season}.png"))
            made.append((f"{name}{season}", out))
    return made


# ================================================================ things only a season has
def build_extras():
    out = []
    s = prop("DecorSnow", 32, "Snow drifts")
    for tag, lumps in (("A", [(16, 18, 10, 6)]), ("B", [(11, 19, 8, 5), (21, 16, 7, 5)]), ("C", [(9, 17, 5, 3.5), (19, 20, 6, 4), (24, 14, 4, 3)])):
        def draw(L, k, lumps=lumps):
            for x, y, rx, ry in lumps:
                L["Shadow"].blob(x + 1, y + ry * 0.7, rx + 1, ry * 0.6, (40, 70, 110), a=70)
                d = Cv(32, 32)
                d.blob(x, y, rx, ry, "w", "w", "b")
                d.outline("B")
                L["Body"].paste(d)
                L["Body"].line([(x - rx * 0.5, y + ry * 0.3), (x + rx * 0.3, y + ry * 0.5)], "b")
            if k == 1:
                L["FX"].spr(SPARK_S, lumps[0][0] - 3, lumps[0][1] - 5)
        loop(s, tag, 2, draw, 900)
    out.append(s)

    s = prop("DecorLeafPile", 32, "Raked autumn leaves")
    cols = [("r", "f", "R"), ("o", "u", "R"), ("y", "c", "Y"), ("d", "T", "D")]

    def pile(L, k, blow=False):
        L["Shadow"].blob(16.5, 21, 12, 5, (0, 0, 0), a=75)
        rng = random.Random(6)
        for j in range(16):
            ang = rng.uniform(0, 2 * math.pi)
            d = rng.uniform(0, 8) * (1 - j / 30)
            leaf(L["Body"], 16 + math.cos(ang) * d * 1.2, 18 + math.sin(ang) * d * 0.6 - j * 0.25, 7, 4, rng.uniform(0, 180), cols[j % 4])
        if blow:                                                  # one leaf lifts off and skitters away
            leaf(L["FX"], 22 + k * 2.2, 10 - math.sin(k * 0.9) * 3, 6, 3 + (k % 2), k * 50, cols[k % 3])

    loop(s, "A", 1, pile, 1000)
    loop(s, "B", 5, lambda L, k: pile(L, k, True), 120)
    out.append(s)

    s = prop("WaterIce", 32, "Frozen puddles")

    def ice(L, k, blobs, cracked=0):
        for cx, cy, rx, ry in blobs:
            w = Cv(32, 32)
            w.blob(cx, cy, rx, ry, "b", "w", "B")
            w.outline("n")
            L["Body"].paste(w)
            L["Body"].line([(cx - rx * 0.5, cy - ry * 0.3), (cx - rx * 0.1, cy - ry * 0.3)], "w")     # a hard glint
            L["Body"].line([(cx + rx * 0.1, cy + ry * 0.3), (cx + rx * 0.5, cy + ry * 0.3)], "w")
        cx, cy = blobs[0][0], blobs[0][1]
        rng = random.Random(2)
        for j in range(min(cracked, 4) * 2):
            ang = rng.uniform(0, 2 * math.pi)
            pts = [(cx, cy)]
            for step in range(1, 1 + min(cracked, 3)):
                pts.append((cx + math.cos(ang + rng.uniform(-0.4, 0.4)) * step * 2.4, cy + math.sin(ang + rng.uniform(-0.4, 0.4)) * step * 1.6))
            L["Body"].line(pts, "n")
        if cracked == 1:
            L["FX"].spr(SPARK_S, cx - 1, cy - 2)
        if k == 1 and not cracked:
            L["FX"].spr(SPARK_S, cx + 3, cy - 4)

    loop(s, "Small", 2, lambda L, k: ice(L, k, [(16, 17, 8, 5.5)]), 800)
    loop(s, "Large", 2, lambda L, k: ice(L, k, [(14, 16, 11, 8), (23, 21, 7, 5)]), 800)
    loop(s, "Crack", 5, lambda L, k: ice(L, k, [(16, 17, 8, 5.5)], cracked=k + 1), 90, repeat=1)
    out.append(s)

    s = Sprite("FxSeason", "Fx", 32, 32, ["FX"], "Seasonal weather that tiles")

    def tag(name, n, draw, ms, repeat=0):
        frames = []
        for k in range(n):
            cv = Cv(32, 32)
            draw(cv, k)
            frames.append({"FX": cv.im})
        s.tag(name, frames, ms, repeat=repeat)

    flakes = [(random.Random(j).randrange(32), random.Random(90 + j).randrange(32), j % 3) for j in range(16)]

    def snow(cv, k):                                              # eight frames, four pixels a frame: one tile per loop
        for x, y, size in flakes:
            yy = (y + k * 4) % 32
            xx = (x + round(math.sin((k + x) * 0.8) * 1.5)) % 32
            cv.px(xx, yy, "w")
            if size == 0:
                cv.pxs([((xx + 1) % 32, yy), (xx, (yy + 1) % 32), ((xx - 1) % 32, yy), (xx, (yy - 1) % 32)], "b", a=200)

    drift = [(random.Random(40 + j).randrange(32), random.Random(70 + j).randrange(32)) for j in range(5)]
    leaf_cols = [("r", "f", "R"), ("o", "u", "R"), ("y", "c", "Y")]

    def leaves(cv, k):
        for j, (x, y) in enumerate(drift):
            yy = (y + k * 4) % 32
            xx = (x + k * 4 + round(math.sin((k + j) * 1.1) * 2)) % 32
            for ox in (-32, 0, 32):
                for oy in (-32, 0, 32):
                    if -4 < xx + ox < 36 and -4 < yy + oy < 36:
                        leaf(cv, xx + ox, yy + oy, 6, 3 + (k + j) % 2, (k * 45 + j * 70) % 180, leaf_cols[j % 3])

    def breath(cv, k):
        for j in range(3):
            r = 1.5 + k * 0.7 + j * 0.3
            cv.blob(12 + k * 2.2 + j * 3, 17 - k * 1.2 - j, r, r * 0.85, "w", "w", "b", a=max(0, 170 - k * 32))

    tag("Snow", 8, snow, 90)
    tag("Leaves", 8, leaves, 110)
    tag("Breath", 5, breath, 110, repeat=1)
    out.append(s)

    s = Sprite("FxMoonlight", "Fx", 64, 64, ["FX"], "Moonlight on the ground")

    def moon(cv, k):
        glow(cv, 32, 32, 28 + (0, 1, 2, 1)[k], "b", 0.9)
        glow(cv, 32, 32, 16 + (0, 1, 1, 0)[k], "w", 0.7)

    def cloud(cv, k):
        for j, (y, rx) in enumerate(((20, 16), (36, 20), (48, 12))):
            x = (k * 16 + j * 20) % 96 - 16
            for ox in (0,):
                c = Cv(64, 64)
                c.blob(x + ox, y, rx, 7, (10, 9, 30), a=70)
                cv.paste(c)

    frames = []
    for k in range(4):
        cv = Cv(64, 64); moon(cv, k); frames.append({"FX": cv.im})
    s.tag("Moon", frames, 260)
    frames = []
    for k in range(6):
        cv = Cv(64, 64); cloud(cv, k); frames.append({"FX": cv.im})
    s.tag("Cloud", frames, 240)
    out.append(s)
    return out


# ================================================================ everything else, in autumn and winter
def build_everything():
    import build_roster_sheets as roster
    import build_scenery_sheets as scenery
    import build_tier_sheets as tiers
    out = []
    towers = []
    for tier in (1, 2, 3):                                        # the 14 older castes first: roster swaps the table
        towers += tiers.build(tier)
    new = roster.build_towers()
    for tier in (1, 2, 3):
        towers += new[tier]
    for t in towers:
        for season in ("Autumn", "Winter"):
            out.append(seasonal(t, season, layers_to_snow=("Base",), tint_layers=("Base",), leaves=True))

    props = {s.name: s for s in base.build_props() + extra.build_decor() + nature.build_terrain() + nature.build_plants()
             + scenery.build_buildings() + scenery.build_objects() + scenery.build_lights()}
    both = ["PropBurrow", "DecorFlower", "DecorPebble", "DecorLeaf", "DecorTwig", "DecorRoot", "DecorMushroom", "DecorTrailEdge",
            "TerrainBridge", "TerrainCliff", "TerrainTunnel", "PlantDandelion",
            "BuildingGranary", "BuildingNursery", "BuildingBarracks", "BuildingWatchPost", "BuildingFungusFarm",
            "ObjClutter", "ObjSignpost", "ObjSideHole", "ObjThornFence", "ObjWeb", "ObjLog"]
    winter_only = ["LightTorch", "LightEmberPit", "LightGlowCap", "LightLantern", "LightCrystal"]   # nothing on them turns in autumn
    for name in both:
        for season in ("Autumn", "Winter"):
            out.append(seasonal(props[name], season, tint_layers=("Body",)))
    for name in winter_only:
        out.append(seasonal(props[name], "Winter", tint_layers=("Body",)))
    return out


# ================================================================ run
def sources():
    by_name = {}
    for s in base.build_props() + extra.build_decor() + nature.build_plants():
        by_name[s.name] = s
    return [by_name[n] for n in ("PropHill", "DecorGrass", "PlantFern", "PlantTallGrass", "PlantClover", "RockBoulder")]


def main():
    made = build_tiles()
    sheet = Image.new("RGBA", (3 * 200 + 8, 3 * 136 + 8), base.rgba("#2a1d14"))
    for i, (name, im) in enumerate(made):                         # each tile shown 2x2, to prove it still repeats
        big = Image.new("RGBA", (im.width * 2, im.height * 2))
        for ox in (0, 1):
            for oy in (0, 1):
                big.paste(im, (ox * im.width, oy * im.height))
        big = big.resize((big.width * (1 if im.width == 64 else 2), big.height * (1 if im.width == 64 else 2)), Image.NEAREST)
        sheet.paste(big.crop((0, 0, 192, 128)), (4 + (i % 3) * 200, 4 + (i // 3) * 136))
    sheet.save(os.path.join(HERE, "SeasonTilePreview.png"))
    print(f"{len(made)} tiles -> {TILE_OUT}")

    results = []
    for src in sources():
        for season in SEASONS:
            s = seasonal(src, season)
            results.append((s, export(s)))
    extras = build_extras()
    for s in extras:
        results.append((s, export(s)))
    for s, _ in results:
        print(f"{s.group:6} {s.name:22} " + ", ".join(t[0] for t in s.tags))
    # preview: first frame of every tag of the seasonal sets, and all frames of the extras
    base.preview_sheet(results[-len(extras):], os.path.join(HERE, "SeasonExtrasPreview.png"), scale=3)
    cells = []
    for s, (rows, comps) in results[:-len(extras)]:
        cells.append((s, comps[0]))
    scale = 2
    out = Image.new("RGBA", (3 * 150 + 110, 6 * 140), (62, 92, 46, 255))
    from PIL import ImageDraw, ImageFont
    d = ImageDraw.Draw(out)
    font = ImageFont.load_default()
    for i, (s, im) in enumerate(cells):
        row, col = i // 3, i % 3
        bg = {0: (120, 96, 50), 1: (224, 234, 242), 2: (22, 30, 52)}[col]
        d.rectangle([110 + col * 150, row * 140, 110 + col * 150 + 148, row * 140 + 138], fill=bg)
        big = im.resize((im.width * scale, im.height * scale), Image.NEAREST)
        out.alpha_composite(big, (110 + col * 150 + (148 - big.width) // 2, row * 140 + (138 - big.height) // 2))
        if col == 0:
            d.text((4, row * 140 + 60), s.name.replace("Autumn", ""), fill=(255, 241, 214, 255), font=font)
    out.save(os.path.join(HERE, "SeasonPropPreview.png"))
    more = build_everything()
    for s in more:
        export(s)
    print(f"{len(more)} more seasonal sprites: " + ", ".join(sorted({s.name.replace("Autumn", "").replace("Winter", "") for s in more})[:8]) + ", ...")
    pick = [s for s in more if s.name in ("TowerLinearWinter", "TowerQueensGuardT3Winter", "TowerMortarT2Autumn", "BuildingBarracksWinter",
                                          "BuildingBarracksAutumn", "BuildingWatchPostWinter", "LightTorchWinter", "ObjLogWinter",
                                          "ObjThornFenceAutumn", "TerrainTunnelWinter", "BuildingGranaryWinter", "TowerWeaverWinter")]
    strip = Image.new("RGBA", (len(pick) * 150, 150), (224, 234, 242, 255))
    for i, sp in enumerate(pick):
        frame0 = Image.new("RGBA", (sp.w, sp.h))
        for name in sp.layers:
            frame0.alpha_composite(sp.tags[0][1][0][name])
        big = frame0.resize((sp.w * 3, sp.h * 3), Image.NEAREST)
        if "Autumn" in sp.name:
            ImageDraw.Draw(strip).rectangle([i * 150, 0, i * 150 + 149, 149], fill=(120, 96, 50, 255))
        strip.alpha_composite(big, (i * 150 + (150 - big.width) // 2, (150 - big.height) // 2))
    strip.save(os.path.join(HERE, "SeasonEverythingPreview.png"))
    print(f"{len(results) + len(more)} sprites written and read back OK")


if __name__ == "__main__":
    main()
