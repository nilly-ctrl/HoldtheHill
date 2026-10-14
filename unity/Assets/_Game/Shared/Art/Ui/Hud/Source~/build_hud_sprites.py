"""HUD pieces for Hold the Hill: bars, wave track, tooltip, cost tag, keycap, icon slots, pips.

Same palette as the buttons and panel in Ui/ and UiKit/Sprites. Most pieces are 9-slice sprites;
the slice borders are listed in BORDERS below and applied on import by Editor/PixelIconImporter.cs
(keep the two in step). Writes the PNGs into the folder above this one, plus HudPreview.png here.

    python build_hud_sprites.py
"""
import importlib.util
import json
from pathlib import Path

from PIL import Image

HERE = Path(__file__).resolve().parent
OUT = HERE.parent
SANDBOX = OUT.parent.parent

CLEAR = (0, 0, 0, 0)
INK = (0x1B, 0x11, 0x0B, 255)
GOLD_HI = (0xEC, 0xC4, 0x77, 255)
GOLD = (0xB5, 0x87, 0x3E, 255)
WOOD_HI = (0xA0, 0x64, 0x3C, 255)
WOOD = (0x83, 0x56, 0x2F, 255)
WOOD_LO = (0x5B, 0x3A, 0x23, 255)
WOOD_DARK = (0x3A, 0x24, 0x16, 255)
WELL = (0x2A, 0x1D, 0x14, 255)
WELL_LO = (0x22, 0x17, 0x0F, 255)
CREAM = (0xFF, 0xF1, 0xD6, 255)
WHITE = (255, 255, 255, 255)
RED = (0xE2, 0x41, 0x2F, 255)
RED_LO = (0x8E, 0x21, 0x24, 255)
CYAN = (0x9F, 0xE7, 0xFF, 255)
GREY = (0x6D, 0x67, 0x5C, 255)
GREY_HI = (0xC4, 0xBF, 0xB2, 255)

# name -> (left, bottom, right, top) slice border in pixels, Unity's order
BORDERS = {}
SPRITES = {}


def canvas(w, h, fill=CLEAR):
    return Image.new("RGBA", (w, h), fill)


def put(img, pts, c):
    for x, y in pts:
        if 0 <= x < img.width and 0 <= y < img.height:
            img.putpixel((x, y), c)


def rect(img, x0, y0, x1, y1, c):
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            img.putpixel((x, y), c)


def frame(img, x0, y0, x1, y1, c, cut=True):
    """1 px rectangle outline; cut=True leaves the four corner pixels empty."""
    for x in range(x0, x1 + 1):
        for y in (y0, y1):
            if not (cut and x in (x0, x1)):
                img.putpixel((x, y), c)
    for y in range(y0 + 1, y1):
        img.putpixel((x0, y), c)
        img.putpixel((x1, y), c)


def save(name, img, border=None):
    img.save(OUT / f"{name}.png")
    SPRITES[name] = img
    if border is not None:
        BORDERS[name] = border if isinstance(border, tuple) else (border,) * 4


# ---------------------------------------------------------------- bars
def bar_frame():
    """Dark well with a gold rim. Fill sprites sit inside, inset 3 px."""
    img = canvas(16, 10)
    frame(img, 0, 0, 15, 9, INK)
    frame(img, 1, 1, 14, 8, GOLD, cut=False)
    rect(img, 1, 1, 14, 1, GOLD_HI)
    put(img, [(1, 1), (14, 1), (1, 8), (14, 8)], INK)
    rect(img, 2, 2, 13, 7, WELL)
    rect(img, 2, 2, 13, 2, WELL_LO)
    save("BarFrame", img, 3)


def bar_fill(name, hi, base, lo):
    img = canvas(4, 4)
    rect(img, 0, 0, 3, 0, hi)
    rect(img, 0, 1, 3, 2, base)
    rect(img, 0, 3, 3, 3, lo)
    save(name, img, (1, 1, 1, 1))


def bar_tick():
    img = canvas(1, 4)
    rect(img, 0, 0, 0, 3, (0x1B, 0x11, 0x0B, 150))
    save("BarTick", img)


# ---------------------------------------------------------------- wave track
def wave_track():
    img = canvas(12, 6)
    frame(img, 0, 0, 11, 5, INK)
    rect(img, 1, 1, 10, 4, WELL)
    rect(img, 1, 1, 10, 1, WELL_LO)
    save("WaveTrack", img, 2)
    fill = canvas(4, 2)
    rect(fill, 0, 0, 3, 0, GOLD_HI)
    rect(fill, 0, 1, 3, 1, GOLD)
    save("WaveFill", fill, (1, 0, 1, 0))


def marker(name, rows, colours):
    h, w = len(rows), len(rows[0])
    img = canvas(w, h)
    for y, row in enumerate(rows):
        for x, ch in enumerate(row):
            if ch != ".":
                img.putpixel((x, y), colours[ch])
    save(name, img)


def wave_markers():
    c = {"k": INK, "c": CREAM, "r": RED, "R": RED_LO, "g": GOLD_HI, "G": GOLD, "w": WHITE, "s": GREY_HI, "S": GREY}
    marker("WaveMarker", [
        ".kkkk..",
        "kcrrrk.",
        "kcrrrrk",
        "kcrrrk.",
        "kckkk..",
        "kck....",
        "kck....",
        ".k.....",
    ], c)
    marker("WaveMarkerDone", [
        ".kkkk..",
        "kcsssk.",
        "kcssssk",
        "kcsssk.",
        "kckkk..",
        "kck....",
        "kck....",
        ".k.....",
    ], c)
    marker("WaveMarkerBoss", [
        "..kkkkk..",
        ".kcccccK.".replace("K", "k"),
        "kcccccccK".replace("K", "k"),
        "kckkckkck",
        "kckkckkck",
        "kccckccck",
        ".kcccccK.".replace("K", "k"),
        ".kckckck.",
        "..k.k.k..",
    ], c)
    marker("WaveMarkerNow", [
        "...k...",
        "..kgk..",
        ".kgwgk.",
        "kggggGk",
        ".kgGGk.",
        "..kGk..",
        "...k...",
    ], c)


# ---------------------------------------------------------------- tooltip
def tooltip():
    img = canvas(16, 16)
    frame(img, 0, 0, 15, 15, INK)
    frame(img, 1, 1, 14, 14, GOLD, cut=False)
    put(img, [(1, 1), (14, 1), (1, 14), (14, 14)], INK)
    rect(img, 1, 1, 14, 1, GOLD_HI)
    put(img, [(1, 1), (14, 1)], INK)
    rect(img, 2, 2, 13, 13, WELL)
    rect(img, 2, 13, 13, 13, WELL_LO)
    save("Tooltip", img, 4)
    marker("TooltipArrow", [
        "kGGGGGGGk",
        ".kWWWWWk.",
        "..kWWWk..",
        "...kWk...",
        "....k....",
    ], {"k": INK, "G": GOLD, "W": WELL})


# ---------------------------------------------------------------- cost tag
def cost_tag(name, body, body_hi, body_lo, rim):
    """Price tag pointing left with a string hole. Slice: left 8, right 3."""
    w, h = 22, 12
    img = canvas(w, h)
    for y in range(h):
        inset = max(0, 5 - y) if y < 6 else max(0, y - 6)   # pointed left end
        for x in range(inset, w):
            edge = y in (0, h - 1) or x == w - 1 or x == inset
            img.putpixel((x, y), INK if edge else body)
    put(img, [(w - 1, 0), (w - 1, h - 1)], CLEAR)
    put(img, [(w - 2, 1), (w - 2, h - 2)], INK)
    for x in range(6, w - 1):
        if img.getpixel((x, 1)) != INK:
            img.putpixel((x, 1), body_hi)
        if img.getpixel((x, h - 2)) != INK:
            img.putpixel((x, h - 2), body_lo)
    rect(img, 4, 5, 5, 6, INK)                        # string hole
    put(img, [(6, 5), (6, 6), (3, 5), (3, 6), (4, 4), (5, 4), (4, 7), (5, 7)], rim)
    save(name, img, (8, 3, 3, 3))


# ---------------------------------------------------------------- keycap
def keycap():
    img = canvas(12, 13)
    frame(img, 0, 0, 11, 12, INK)
    rect(img, 1, 1, 10, 9, CREAM)
    rect(img, 1, 1, 10, 1, WHITE)
    rect(img, 1, 10, 10, 11, GREY_HI)
    rect(img, 1, 11, 10, 11, GREY)
    put(img, [(1, 1), (10, 1), (1, 11), (10, 11)], INK)
    save("Keycap", img, (4, 5, 4, 4))


# ---------------------------------------------------------------- icon slots (hold a 32x32 icon)
def slot(name, rim, rim_hi, inner, glow=None):
    img = canvas(36, 36)
    frame(img, 0, 0, 35, 35, INK)
    frame(img, 1, 1, 34, 34, rim, cut=False)
    rect(img, 1, 1, 34, 1, rim_hi)
    put(img, [(1, 1), (34, 1), (1, 34), (34, 34)], INK)
    rect(img, 2, 2, 33, 33, inner)
    rect(img, 2, 2, 33, 2, WELL_LO if inner == WELL else inner)
    if glow:
        frame(img, 2, 2, 33, 33, glow, cut=False)
    save(name, img, 4)


# ---------------------------------------------------------------- small bits
def small_bits():
    c = {"k": INK, "r": RED, "R": RED_LO, "w": WHITE, "g": GOLD_HI, "G": GOLD, "d": WELL, "D": WOOD_DARK}
    marker("NotifyDot", [
        "..kkkk..",
        ".krrrrk.",
        "krwrrrrk",
        "krrrrrrk",
        "krrrrrrk",
        "krrrrRRk",
        ".krRRRk.",
        "..kkkk..",
    ], c)
    marker("PipFull", [
        ".kkkkk.",
        "kgwgggk",
        "kgggggk",
        "kgggGGk",
        "kgGGGGk",
        ".kkkkk.",
    ], c)
    marker("PipEmpty", [
        ".kkkkk.",
        "kdddddk",
        "kdddddk",
        "kdddddk",
        "kDDDDDk",
        ".kkkkk.",
    ], c)


# ---------------------------------------------------------------- preview
def nine(img, border, w, h):
    l, b, r, t = border
    sw, sh = img.size
    out = canvas(w, h)
    xs = [(0, l, 0, l), (l, sw - r, l, w - r), (sw - r, sw, w - r, w)]
    ys = [(0, t, 0, t), (t, sh - b, t, h - b), (sh - b, sh, h - b, h)]
    for sx0, sx1, dx0, dx1 in xs:
        for sy0, sy1, dy0, dy1 in ys:
            if sx1 > sx0 and sy1 > sy0 and dx1 > dx0 and dy1 > dy0:
                out.paste(img.crop((sx0, sy0, sx1, sy1)).resize((dx1 - dx0, dy1 - dy0), Image.NEAREST), (dx0, dy0))
    return out


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


def preview():
    fonts = load("fonts", SANDBOX / "Fonts" / "Source~" / "build_pixel_fonts.py")
    hud = next(s for s in fonts.STYLES if s["name"] == "Hud")
    gold = next(s for s in fonts.STYLES if s["name"] == "Resource")

    def text(t, style=hud):
        g = {c: fonts.bake(c, style) for c in set(t)}
        return fonts.render(t, g, max(im.size[1] for im, _ in g.values()))

    def icon(name):
        return Image.open(SANDBOX / "Icons" / "PNG" / f"{name}.png").convert("RGBA")

    S, B = SPRITES, BORDERS
    W, H = 300, 150
    pv = canvas(W, H, (0x4A, 0x6A, 0x32, 255))

    def bar(x, y, w, fill, frac, ticks=0):
        pv.alpha_composite(nine(S["BarFrame"], B["BarFrame"], w, 10), (x, y))
        fw = max(2, round((w - 6) * frac))
        pv.alpha_composite(nine(S[fill], B[fill], fw, 4), (x + 3, y + 3))
        for i in range(1, ticks):
            pv.alpha_composite(S["BarTick"], (x + 3 + round((w - 6) * i / ticks), y + 3))

    # Queen health + shield
    pv.alpha_composite(icon("ResQueenHealthIcon"), (4, 2))
    bar(40, 8, 110, "BarFillHealth", 0.72, ticks=5)
    bar(40, 20, 110, "BarFillShield", 0.4)
    pv.alpha_composite(text("72/100"), (156, 8))

    # food with a cost tag
    pv.alpha_composite(icon("ResFoodIcon"), (206, 2))
    pv.alpha_composite(text("516", gold), (242, 10))

    # wave track with markers
    pv.alpha_composite(text("WAVE 3/8"), (6, 42))
    tx, ty, tw = 70, 44, 200
    pv.alpha_composite(nine(S["WaveTrack"], B["WaveTrack"], tw, 6), (tx, ty))
    pv.alpha_composite(nine(S["WaveFill"], B["WaveFill"], round((tw - 4) * 0.34), 2), (tx + 2, ty + 2))
    for i in range(8):
        x = tx + 2 + round((tw - 4) * i / 7)
        name = "WaveMarkerBoss" if i in (4, 7) else ("WaveMarkerDone" if i < 2 else "WaveMarker")
        m = S[name]
        pv.alpha_composite(m, (x - m.width // 2, ty - m.height + 1))
    now = S["WaveMarkerNow"]
    pv.alpha_composite(now, (tx + 2 + round((tw - 4) * 0.34) - 3, ty + 5))

    # build slots with keycaps, cost tags, tier pips
    towers = ["TowerLinearIcon", "TowerMortarIcon", "TowerChainIcon", "TowerFrostAuraIcon", "TowerMineLayerIcon"]
    costs = ["100", "200", "200", "225", "250"]
    for i, (t, cost) in enumerate(zip(towers, costs)):
        x, y = 8 + i * 58, 70
        kind = "SlotSelected" if i == 1 else ("SlotDisabled" if i == 4 else "Slot")
        pv.alpha_composite(S[kind], (x, y))
        ic = icon(t)
        if i == 4:
            ic.putalpha(ic.getchannel("A").point(lambda a: a // 2))
        pv.alpha_composite(ic, (x + 2, y + 2))
        key = nine(S["Keycap"], B["Keycap"], 12, 13)
        pv.alpha_composite(key, (x - 4, y - 5))
        for gy, row in enumerate(fonts.G[str(i + 1)]):          # hotkey digit, dark on the keycap
            for gx, ch in enumerate(row):
                if ch == "#":
                    pv.putpixel((x - 4 + (12 - len(row)) // 2 + gx, y - 5 + 2 + gy), INK)
        tag_name = "CostTagCant" if i == 4 else "CostTag"
        label = text(cost)
        tag = nine(S[tag_name], B[tag_name], label.width + 13, 12)
        pv.alpha_composite(tag, (x + 2, y + 38))
        pv.alpha_composite(label, (x + 11, y + 39))
        for p in range(3):
            pv.alpha_composite(S["PipFull" if p <= (i % 3) else "PipEmpty"], (x + 38, y + 2 + p * 7))
    pv.alpha_composite(S["NotifyDot"], (8 + 2 * 58 + 30, 66))

    # tooltip
    tip_text = text("CHAIN 200")
    tip = nine(S["Tooltip"], B["Tooltip"], tip_text.width + 12, 20)
    tipx, tipy = 8 + 2 * 58 - 14, 128
    pv.alpha_composite(tip, (tipx, tipy))
    pv.alpha_composite(tip_text, (tipx + 6, tipy + 5))

    big = pv.resize((W * 4, H * 4), Image.NEAREST)
    big.save(HERE / "HudPreview.png")


def main():
    bar_frame()
    bar_fill("BarFillHealth", (0xC8, 0xF0, 0x8A, 255), (0x7F, 0xC8, 0x4A, 255), (0x4C, 0x8A, 0x2C, 255))
    bar_fill("BarFillShield", (0xD8, 0xF6, 0xFF, 255), (0x7C, 0xBC, 0xE6, 255), (0x2D, 0x6A, 0x96, 255))
    bar_fill("BarFillDanger", (0xFF, 0x9A, 0x8A, 255), RED, RED_LO)
    bar_fill("BarFillFood", (0xFF, 0xF2, 0xB0, 255), (0xEC, 0xC4, 0x77, 255), GOLD)
    bar_fill("BarFillSkill", (0xE6, 0xD4, 0xFF, 255), (0x9A, 0x6C, 0xF0, 255), (0x5A, 0x3A, 0xA8, 255))
    bar_tick()
    wave_track()
    wave_markers()
    tooltip()
    cost_tag("CostTag", GOLD_HI, (0xFF, 0xF2, 0xB0, 255), GOLD, GOLD)
    cost_tag("CostTagCant", (0xB0, 0x6A, 0x5E, 255), (0xC8, 0x86, 0x7A, 255), RED_LO, RED_LO)
    keycap()
    slot("Slot", GOLD, GOLD_HI, WELL)
    slot("SlotSelected", CYAN, WHITE, WOOD_DARK, glow=(0x5A, 0xA9, 0xD8, 255))
    slot("SlotDisabled", GREY, GREY_HI, WELL_LO)
    small_bits()
    (HERE / "borders.json").write_text(json.dumps({k: list(v) for k, v in sorted(BORDERS.items())}, indent=1))
    preview()
    print(f"wrote {len(SPRITES)} sprites")


if __name__ == "__main__":
    main()
