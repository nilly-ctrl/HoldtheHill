"""Composes a mock-up of a dressed map from the finished sheets, to see how they sit together.

    python build_mockup.py            -> MockupScene.png (one frame) and MockupScene.gif (a 2.4 s loop)
    python build_mockup.py Winter     -> the same scene on the winter tiles: MockupSceneWinter.png/.gif
    python build_mockup.py Autumn
    python build_mockup.py Riverbank  -> the riverbank biome: MockupSceneRiverbank.png/.gif
    python build_mockup.py Forest     -> the forest floor biome

Nothing here is game code: it only reads ../Sheets and ../../Tiles and pastes frames. Positions
are in sheet pixels on a 480x270 stage (15 x 8.4 world units at 32 pixels a unit), saved at 3x.
"""
import json
import os
import random
import sys

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
SHEETS = os.path.normpath(os.path.join(HERE, "..", "Sheets"))
TILES = os.path.normpath(os.path.join(HERE, "..", "..", "Tiles"))
W, H, SCALE = 480, 270, 3
STEP_MS, STEPS = 100, 24
_cache = {}


def sheet(name):
    if name not in _cache:
        for group in os.listdir(SHEETS):
            path = os.path.join(SHEETS, group, name + ".json")
            if os.path.exists(path):
                with open(path, encoding="utf-8") as f:
                    data = json.load(f)
                _cache[name] = (Image.open(path[:-5] + ".png").convert("RGBA"), data)
                break
        else:
            raise KeyError(name)
    return _cache[name]


def frame(name, tag, t_ms):
    im, data = sheet(name)
    t = next(t for t in data["meta"]["frameTags"] if t["name"] == tag)
    frames = data["frames"][t["from"]:t["to"] + 1]
    total = sum(f["duration"] for f in frames)
    t_ms %= total
    for f in frames:
        if t_ms < f["duration"]:
            r = f["frame"]
            return im.crop((r["x"], r["y"], r["x"] + r["w"], r["y"] + r["h"]))
        t_ms -= f["duration"]
    return None


def scene(season):
    """(layer, name, tag, centre x, centre y, options). Layers: 0 ground effects, 1 props, 2 actors, 3 air, 4 interface."""
    rng = random.Random(11)
    s = []
    add = lambda layer, name, tag, x, y, **o: s.append((layer, name, tag, x, y, o))
    plant = lambda n: n + season if season in ("Autumn", "Winter") and n in ("DecorGrass", "PlantFern", "PlantTallGrass", "PlantClover", "RockBoulder") else n
    road_y, stream_x = 150, 196

    # scatter small plants clear of the trail and stream
    for _ in range(46):
        x, y = rng.randint(8, W - 8), rng.randint(40, H - 8)
        if abs(y - road_y) < 26 or abs(x - stream_x) < 22:
            continue
        kind = rng.choice(["DecorGrass"] * 5 + ["DecorFlower", "DecorPebble", "PlantClover", "DecorLeaf"])
        if season == "Winter" and kind in ("DecorFlower", "DecorLeaf"):
            kind = "DecorSnow"
        add(1, plant(kind), rng.choice("ABC") if kind not in ("PlantClover",) else "Sway", x, y, flip=rng.random() < 0.5, offset=rng.randint(0, 900))

    for x, y, n, tag in ((60, 70, "PlantFern", "Sway"), (330, 232, "PlantFern", "Sway"), (150, 236, "PlantTallGrass", "Sway"),
                         (268, 60, "PlantTallGrass", "Sway"), (36, 226, "RockBoulder", "Mossy"), (452, 74, "RockBoulder", "B"),
                         (238, 240, "PlantDandelion", "Clock"), (112, 52, "PlantDandelion", "Bloom")):
        if season == "Winter" and n == "PlantDandelion":
            n, tag = "DecorSnow", "B"
        add(1, plant(n), tag, x, y)
    add(1, "WaterIce" if season == "Winter" else "WaterPuddle", "Large", 396, 226)
    add(1, "ObjLog", "Idle", 96, 238)
    add(1, "ObjSignpost", "Idle", 228, 118)
    add(1, "DecorRoot", "A", 300, 44)
    add(1, "ObjSideHole", "Idle", 30, 60)

    # colony buildings along the top, a farm below
    add(1, "BuildingGranary", "Idle", 134, 84)
    add(1, "BuildingNursery", "Idle", 268, 208)
    add(1, "BuildingWatchPost", "Idle", 410, 96)
    add(1, "BuildingFungusFarm", "Idle", 76, 196)
    add(1, "BuildingBarracks", "Idle", 348, 62)

    # lights, each on a pool of light
    for x, y, n, tag, pool in ((174, 122, "LightTorch", "Lit", "Warm"), (218, 178, "LightTorch", "Lit", "Warm"),
                               (304, 236, "LightGlowCap", "Glow", "Green"), (380, 150 - 34, "LightLantern", "Glow", "Warm"),
                               (40, 150 + 40, "LightCrystal", "Glow", "Cool"), (160, 52, "LightEmberPit", "Burn", "Warm")):
        add(0, "FxLightGlow", pool, x, y + 4)
        add(1, n, tag, x, y)

    # the two ends of the trail
    add(1, "PropBurrow", "Idle", 22, road_y)
    add(3, "PropHill" + (season if season in ("Autumn", "Winter") else ""), "Healthy", 446, road_y)

    # towers either side of the trail, at mixed tiers
    for x, y, n in ((70, 110, "TowerLinear"), (128, 190, "TowerHoneypotT2"), (250, 108, "TowerQueensGuardT3"),
                    (300, 190, "TowerWeaver"), (340, 110, "TowerFrostAuraT3"), (400, 190, "TowerMortarT2"), (166, 190, "TowerWorker")):
        add(2, n, "Idle", x, y)
    add(4, "UiPlaceMarker", "Valid", 300, 110)

    # enemies on the trail, heading right
    for x, n, tag in ((56, "EnemyBoss", "Walk"), (120, "EnemySnail", "Walk"), (158, "EnemyGrub", "Walk"), (236, "EnemySpider", "Walk"),
                      (272, "EnemyGrunt", "Walk"), (300, "EnemyRunner", "Walk"), (346, "EnemyShielded", "Walk")):
        add(2, n, tag, x, road_y, walk=10)
    add(3, "EnemyWasp", "Walk", 386, road_y - 22, walk=18)

    # colony life
    add(2, "AntWorker", "CarrySeed", 150, 104, walk=6)
    add(2, "AntWorker", "CarryLeaf", 60, 216, walk=6)
    add(2, "AntGuard", "Idle", 420, 196)
    add(2, "AntNurse", "CarryEgg", 238, 196, walk=5)
    add(2, "AntPair", "Talk", 360, 222)
    add(2, "AntWorker", "Dig", 196 + 60, 76)

    # air
    for x, y in ((120, 150 - 60), (320, 250), (430, 40)):
        add(3, "FxParticles", "Firefly", x, y, offset=x * 7)
    add(3, "FxParticles", "Pollen", 200, 40)
    add(3, "FxParticles", "Smoke", 160, 40)

    # interface
    add(4, "UiHealthBar", "Idle", 240, 12)
    add(4, "UiFoodCounter", "Idle", 26, 12)
    add(4, "UiWaveTrack", "Idle", 420, 12)
    add(4, "UiCursor", "Build", 312, 122)
    return s, road_y, stream_x


def ground(season, road_y, stream_x, t_ms):
    suffix = season if season in ("Autumn", "Winter", "Night") else ""
    folder = os.path.join(TILES, "Seasons") if suffix else TILES
    grass = Image.open(os.path.join(folder, f"GroundGrass{suffix}.png")).convert("RGBA")
    road = Image.open(os.path.join(folder, f"RoadDirt{suffix}.png")).convert("RGBA")
    im = Image.new("RGBA", (W, H))
    for y in range(0, H, grass.height):
        for x in range(0, W, grass.width):
            im.paste(grass, (x, y))
    if season != "Winter":                                        # the stream, top to bottom (frozen over in winter: left out)
        for y in range(0, H, 32):
            im.alpha_composite(frame("WaterStream", "Vertical", t_ms), (stream_x - 16, y))
    for x in range(0, W, road.width):
        im.alpha_composite(road, (x, road_y - road.height // 2))
    return im


def paste(im, sprite, x, y, flip=False):
    if flip:
        sprite = sprite.transpose(Image.FLIP_LEFT_RIGHT)
    px, py = int(x - sprite.width / 2), int(y - sprite.height / 2)
    layer = Image.new("RGBA", im.size)
    layer.paste(sprite, (px, py))
    im.alpha_composite(layer)


def render(season, t_ms):
    items, road_y, stream_x = scene(season)
    im = ground(season, road_y, stream_x, t_ms)
    if season != "Winter":
        paste(im, frame("TerrainBridge", "Twig", t_ms), stream_x, road_y)
    for layer, name, tag, x, y, o in sorted(items, key=lambda i: (i[0], i[4])):
        sprite = None
        for candidate in ((name + season, name) if season in ("Autumn", "Winter") and layer < 4 else (name,)):
            try:                                                  # the seasonal version of a piece, where one exists
                sprite = frame(candidate, tag, t_ms + o.get("offset", 0))
                break
            except (KeyError, StopIteration):
                continue
        if sprite is None:
            continue
        dx = (t_ms / 1000.0) * o.get("walk", 0)
        paste(im, sprite, x + dx, y, o.get("flip", False))
    if season == "Winter":                                        # snow falling over everything
        for y in range(0, H, 32):
            for x in range(0, W, 32):
                im.alpha_composite(frame("FxSeason", "Snow", t_ms), (x, y))
    return im.resize((W * SCALE, H * SCALE), Image.NEAREST)


BIOMES = {
    "Riverbank": dict(folder="Riverbank", ground="GroundSand", road="RoadSand", hill="PropHillSand", burrow="PropBurrowSand"),
    "Forest": dict(folder="Forest", ground="GroundLitter", road="RoadEarth", hill="PropHillForest", burrow="PropBurrowForest"),
}


def biome_items(biome):
    """(layer, name, tag, x, y, options) for a biome's own pieces; towers, enemies and ants are shared."""
    rng = random.Random(5)
    s = []
    add = lambda layer, name, tag, x, y, **o: s.append((layer, name, tag, x, y, o))
    road_y = 132
    if biome == "Riverbank":
        for x in range(0, W, 32):                                 # the river along the bottom
            add(0, "WaterShore", "Edge", x + 16, 222)
            add(0, "WaterShallows", "Flow", x + 16, 254)
        for x, y, n, tag in ((40, 196, "PlantReed", "Sway"), (300, 200, "PlantReed", "Sway"), (440, 198, "PlantReed", "Sway"),
                             (110, 40, "PlantBeachGrass", "Sway"), (250, 182, "PlantBeachGrass", "Sway"), (410, 60, "PlantBeachGrass", "Sway"),
                             (150, 180, "DecorDriftwood", "A"), (330, 40, "DecorDriftwood", "B"), (30, 36, "RockRiver", "A"),
                             (200, 206, "RockRiver", "Wet"), (120, 250, "CritterStrider", "Skate"), (360, 246, "CritterStrider", "Skate")):
            add(1, n, tag, x, y)
        for _ in range(24):
            x, y = rng.randint(8, W - 8), rng.randint(20, 200)
            if abs(y - road_y) > 24:
                kind = rng.choice([("DecorShell", "A"), ("DecorShell", "B"), ("DecorShell", "C"), ("DecorRiver", "Feather"),
                                   ("DecorRiver", "Tracks"), ("DecorRiver", "Ripple"), ("DecorRiver", "Pebbles"), ("DecorRiver", "Weed")])
                add(1, kind[0], kind[1], x, y, offset=x * 5)
        lights = ((190, 100, "LightShellLamp", "Glow", "Green"), (286, 166, "LightReedTorch", "Lit", "Warm"), (70, 166, "LightReedTorch", "Lit", "Warm"))
        buildings = (("BuildingGranarySand", 150, 66), ("BuildingWatchPostSand", 396, 176), ("BuildingNurserySand", 70, 84))
    else:
        for x, y, n, tag in ((60, 200, "PlantToadstool", "Idle"), (410, 56, "PlantToadstool", "Idle"), (230, 216, "PlantSapling", "Sway"),
                             (30, 60, "PlantSapling", "Sway"), (330, 214, "ObjFallenLog", "Idle"), (120, 40, "PlantMoss", "B"),
                             (300, 60, "PlantMoss", "A"), (150, 226, "PlantFern", "Sway"), (446, 220, "PlantFern", "Sway"),
                             (250, 40, "RockBoulder", "Mossy"), (180, 190, "CritterPillbug", "Walk"), (370, 96, "CritterPillbug", "Walk")):
            add(1, n, tag, x, y, walk=4 if n == "CritterPillbug" else 0)
        for _ in range(26):
            x, y = rng.randint(8, W - 8), rng.randint(20, H - 10)
            if abs(y - road_y) > 24:
                add(1, "DecorLitter", rng.choice(["LeafA", "LeafB", "Needles", "AcornCap", "Cone"]), x, y)
        for x, y in ((100, 80), (260, 200), (400, 150), (200, 50)):
            add(3, "FxDapple", "Sun", x, y, offset=x * 9)
        lights = ((196, 96, "LightFoxfire", "Glow", "Green"), (290, 170, "LightGlowCap", "Glow", "Green"), (80, 170, "LightTorch", "Lit", "Warm"))
        buildings = (("BuildingGranaryForest", 150, 70), ("BuildingFungusFarmForest", 400, 196), ("BuildingBarracksForest", 70, 96))
    for x, y, n, tag, pool in lights:
        add(0, "FxLightGlow", pool, x, y + 4)
        add(1, n, tag, x, y)
    for n, x, y in buildings:
        add(1, n, "Idle", x, y)
    cfg = BIOMES[biome]
    add(1, cfg["burrow"], "Idle", 22, road_y)
    add(3, cfg["hill"], "Healthy", 446, road_y)
    for x, y, n in ((110, road_y - 40, "TowerLinear"), (220, road_y + 40, "TowerHoneypotT2"), (250, road_y - 40, "TowerScoutT3"),
                    (340, road_y + 40, "TowerWeaver"), (360, road_y - 40, "TowerFungusFarmerT2")):
        add(2, n, "Idle", x, y)
    for x, n in ((70, "EnemySnail"), (130, "EnemyGrub"), (190, "EnemySpiderElite"), (250, "EnemyGrunt"), (300, "EnemyRunnerElite")):
        add(2, n, "Walk", x, road_y, walk=10)
    add(3, "EnemyHornet", "Walk", 380, road_y - 30, walk=14)
    add(2, "AntWorker", "CarrySeed", 170, road_y - 62, walk=6)
    add(2, "AntGuard", "Idle", 420, road_y + 50)
    add(4, "UiHealthBar", "Idle", 240, 12)
    add(4, "UiFoodCounter", "Idle", 26, 12)
    add(4, "UiWaveTrack", "Idle", 420, 12)
    return s, road_y


def render_biome(biome, t_ms):
    cfg = BIOMES[biome]
    folder = os.path.join(TILES, cfg["folder"])
    grass = Image.open(os.path.join(folder, cfg["ground"] + ".png")).convert("RGBA")
    road = Image.open(os.path.join(folder, cfg["road"] + ".png")).convert("RGBA")
    items, road_y = biome_items(biome)
    im = Image.new("RGBA", (W, H))
    for y in range(0, H, grass.height):
        for x in range(0, W, grass.width):
            im.paste(grass, (x, y))
    for x in range(0, W, road.width):
        im.alpha_composite(road, (x, road_y - road.height // 2))
    for layer, name, tag, x, y, o in sorted(items, key=lambda i: (i[0], i[4])):
        try:
            sprite = frame(name, tag, t_ms + o.get("offset", 0))
        except (KeyError, StopIteration):
            print("missing", name, tag)
            continue
        paste(im, sprite, x + (t_ms / 1000.0) * o.get("walk", 0), y, o.get("flip", False))
    return im.resize((W * SCALE, H * SCALE), Image.NEAREST)


def main():
    season = sys.argv[1] if len(sys.argv) > 1 else ""
    if season in BIOMES:
        frames = [render_biome(season, i * STEP_MS) for i in range(STEPS)]
    else:
        frames = [render(season, i * STEP_MS) for i in range(STEPS)]
    base_name = os.path.join(HERE, "MockupScene" + season)
    frames[6].save(base_name + ".png")
    small = [f.resize((W * 2, H * 2), Image.NEAREST).convert("P", palette=Image.ADAPTIVE, colors=128) for f in frames]
    small[0].save(base_name + ".gif", save_all=True, append_images=small[1:], duration=STEP_MS, loop=0)
    print(f"{base_name}.png and .gif ({STEPS} frames)")


if __name__ == "__main__":
    main()
