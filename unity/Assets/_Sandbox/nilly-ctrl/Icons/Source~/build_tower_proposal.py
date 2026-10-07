"""Proposal only: distinct in-game tower sprites to match the emblem-style icons.

Takes the first frame of each existing tower sheet (Animations/Sheets/Towers) and draws a bold,
tower-specific structure over it. Nothing in Animations/ is changed; this writes one comparison
image, TowerSpriteProposal.png, next to this script.

    python build_tower_proposal.py
"""
import importlib.util
import math
from pathlib import Path

from PIL import Image

HERE = Path(__file__).resolve().parent
SANDBOX = HERE.parent.parent


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


icons = load("icons", HERE / "build_pixel_icons.py")
fonts = load("fonts", SANDBOX / "Fonts" / "Source~" / "build_pixel_fonts.py")
L, outlined, rgba = icons.L, icons.outlined, icons.rgba


def frame(name):
    return Image.open(SANDBOX / "Animations" / "Sheets" / "Towers" / f"{name}.png").convert("RGBA").crop((0, 0, 32, 32))


def tint_mound(im, mul):
    """Recolour the tan mound pixels (not the ant or the outline)."""
    out = im.copy()
    px = out.load()
    for y in range(32):
        for x in range(32):
            r, g, b, a = px[x, y]
            if a and r > 150 and g > 110 and b < 150 and r > b + 40:      # sandy mound tones
                px[x, y] = (min(255, int(r * mul[0])), min(255, int(g * mul[1])), min(255, int(b * mul[2])), a)
    return out


def over(base, *layers):
    out = base.copy()
    for lay, outline in layers:
        out.alpha_composite(outlined(lay.im) if outline else lay.im)
    return out


def linear(b):
    sac = L().ell(11, 17, 20, 27, "g").ell(14, 21, 20, 27, "G").ell(12, 18, 19, 25, "g").pxs([(13, 19), (14, 19), (13, 20)], "w")
    spit = L().ell(14, 0, 17, 3, "g").px(15, 1, "w")
    return over(b, (sac, True), (spit, True))


def homing(b):
    ring = L().ell(3, 9, 28, 29, ol="r")
    ring.line([(15, 7), (15, 11)], "r").line([(15, 27), (15, 31)], "r").line([(1, 19), (5, 19)], "r").line([(26, 19), (30, 19)], "r")
    darts = L().poly([(3, 3), (8, 5), (4, 8)], "o").poly([(28, 3), (23, 5), (27, 8)], "o")
    return over(b, (ring, False), (darts, True))


def mortar(b):
    b = tint_mound(b, (0.86, 0.82, 0.8))
    barrel = L().ell(8, 7, 23, 22, "S").ell(10, 9, 21, 20, "k").ell(11, 10, 20, 19, "e")
    barrel.arc((8, 7, 23, 22), 190, 290, "s", 2)
    shells = L().ell(3, 22, 8, 27, "D").px(4, 23, "t").ell(7, 25, 12, 30, "D").px(8, 26, "t")
    return over(b, (barrel, True), (shells, True))


def ricochet(b):
    sling = L().line([(15, 15), (9, 3)], "T", 2).line([(16, 15), (22, 3)], "T", 2)
    band = L().line([(9, 3), (15, 7), (22, 3)], "c")
    pebbles = L().ell(13, 5, 17, 9, "s").px(14, 6, "w").ell(24, 22, 27, 25, "s").ell(21, 25, 24, 28, "s")
    return over(b, (sling, True), (band, False), (pebbles, True))


def chain(b):
    rods = L().rect(4, 7, 5, 18, "s").rect(26, 7, 27, 18, "s").rect(3, 4, 6, 6, "y").rect(25, 4, 28, 6, "y")
    arc = L().line([(6, 5), (10, 2), (13, 6), (17, 1), (20, 5), (25, 4)], "y")
    return over(b, (rods, True), (arc, True))


def beam(b):
    stand = L().line([(9, 12), (13, 6)], "T").line([(22, 12), (18, 6)], "T")
    lens = L().ell(9, -2, 22, 11, "T").ell(10, -1, 21, 10, "B").ell(11, 0, 19, 8, "b").pxs([(12, 2), (13, 1), (14, 1), (12, 3)], "w")
    return over(b, (stand, True), (lens, True))


def frost(b):
    b = tint_mound(b, (0.72, 0.95, 1.25))
    shards = L()
    for x, y, h in [(3, 12, 7), (26, 11, 8), (5, 24, 5), (24, 25, 5), (15, 28, 3)]:
        shards.poly([(x, y + h), (x + 1.5, y), (x + 3, y + h)], "b").px(x + 1, y + 2, "w")
    snow = L().pxs([(8, 11), (9, 10), (10, 10), (21, 10), (22, 10), (23, 11), (12, 9), (19, 9)], "w")
    return over(b, (shards, True), (snow, False))


def knockback(b):
    stones = L()
    for i in range(10):
        a = 2 * math.pi * i / 10
        x, y = 15.5 + 13 * math.cos(a), 19 + 9.5 * math.sin(a)
        stones.ell(x - 1.5, y - 1.5, x + 1.5, y + 1.5, "s")
    waves = L().arc((1, 5, 30, 33), 200, 250, "c").arc((1, 5, 30, 33), 290, 340, "c")
    return over(b, (waves, False), (stones, True))


def mine_layer(b):
    crate = L().rect(1, 17, 9, 25, "T").line([(1, 17), (9, 25)], "D").line([(9, 17), (1, 25)], "D").rect(1, 17, 9, 17, "t")
    mines = L()
    for x, y in [(22, 21), (25, 15)]:
        mines.d.chord([x, y, x + 6, y + 10], 180, 360, fill=rgba("S"))
        mines.px(x + 3, y, "r").px(x + 2, y, "r")
    return over(b, (crate, True), (mines, True))


TOWERS = [
    ("TowerLinear", "SPITTER", linear), ("TowerHoming", "SEEKER", homing), ("TowerMortar", "MORTAR", mortar),
    ("TowerRicochet", "SLINGER", ricochet), ("TowerChain", "STORM", chain), ("TowerBeam", "LENS", beam),
    ("TowerFrostAura", "FROST", frost), ("TowerKnockback", "KICKER", knockback), ("TowerMineLayer", "SAPPER", mine_layer),
]


def text(t):
    hud = next(s for s in fonts.STYLES if s["name"] == "Hud")
    g = {c: fonts.bake(c, hud) for c in set(t)}
    return fonts.render(t, g, max(im.size[1] for im, _ in g.values()))


def main():
    zoom, cell, col = 3, 36, 52
    W = 64 + len(TOWERS) * col
    sheet = Image.new("RGBA", (W, 3 * cell + 18), (0x3C, 0x6A, 0x26, 255))
    sheet.alpha_composite(text("NOW"), (4, 18 + 12))
    sheet.alpha_composite(text("PROPOSED"), (4, 18 + cell + 12))
    sheet.alpha_composite(text("ICON"), (4, 18 + 2 * cell + 12))
    for i, (name, label, fn) in enumerate(TOWERS):
        x = 64 + i * col + (col - 32) // 2
        lab = text(label)
        sheet.alpha_composite(lab, (x + (32 - lab.width) // 2, 4))
        base = frame(name)
        sheet.alpha_composite(base, (x, 18))
        sheet.alpha_composite(fn(frame("TowerLinear") if name != "TowerFrostAura" and name != "TowerKnockback" else base), (x, 18 + cell))
        sheet.alpha_composite(Image.open(HERE.parent / "PNG" / f"{name}Icon.png").convert("RGBA"), (x, 18 + 2 * cell))
    sheet.resize((sheet.width * zoom, sheet.height * zoom), Image.NEAREST).save(HERE / "TowerSpriteProposal.png")
    print("wrote TowerSpriteProposal.png")


if __name__ == "__main__":
    main()
