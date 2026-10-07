"""Cursors and placement markers for Hold the Hill.

Writes the PNGs into the folder above this one, plus MarkersPreview.png here.

    python build_marker_sprites.py
"""
import math
from pathlib import Path

from PIL import Image

HERE = Path(__file__).resolve().parent
OUT = HERE.parent
SANDBOX = OUT.parent.parent

C = {
    "k": (0x1B, 0x11, 0x0B, 255),   # ink
    "c": (0xFF, 0xF1, 0xD6, 255),   # cream
    "s": (0xC4, 0xBF, 0xB2, 255),   # cream shade
    "w": (255, 255, 255, 255),
    "g": (0x9F, 0xE0, 0x4A, 255), "G": (0x4C, 0x8A, 0x2C, 255),
    "r": (0xE2, 0x41, 0x2F, 255), "R": (0x8E, 0x21, 0x24, 255),
    "y": (0xFF, 0xD2, 0x3A, 255), "Y": (0xC2, 0x8A, 0x10, 255),
    "t": (0xB5, 0x87, 0x3E, 255), "T": (0x83, 0x56, 0x2F, 255),
    "h": (0xC4, 0xBF, 0xB2, 255), "H": (0x6D, 0x67, 0x5C, 255),
}
SPRITES = {}


def art(name, rows):
    w = max(len(r) for r in rows)
    img = Image.new("RGBA", (w, len(rows)), (0, 0, 0, 0))
    for y, row in enumerate(rows):
        for x, ch in enumerate(row):
            if ch != ".":
                img.putpixel((x, y), C[ch])
    return save(name, img)


def save(name, img):
    img.save(OUT / f"{name}.png")
    SPRITES[name] = img
    return img


ARROW = [
    "k...........",
    "kk..........",
    "kck.........",
    "kcck........",
    "kccck.......",
    "kcccck......",
    "kccccck.....",
    "kcccccck....",
    "kccccccck...",
    "kcccccccck..",
    "kccccskkkkk.",
    "kcckcsk.....",
    "kck.kcsk....",
    "kk..kcsk....",
    "k....kcsk...",
    ".....kkk....",
]


def cursors():
    art("Cursor", ARROW)                       # hot spot: top-left pixel
    # arrow with a small hammer: "click to build"
    build = [r.ljust(18, ".") for r in ARROW] + ["." * 18] * 2
    hammer = [
        "..kkkk.",
        ".khhhhk",
        "khhHHHk",
        ".kkTkk.",
        "..kTk..",
        "..kTk..",
        "..kkk..",
    ]
    rows = [list(r) for r in build]
    for j, row in enumerate(hammer):
        for i, ch in enumerate(row):
            if ch != ".":
                rows[10 + j][11 + i] = ch
    art("CursorBuild", ["".join(r) for r in rows])
    # arrow with a red "no" badge
    no = [
        "..kkkk..",
        ".krrrrk.",
        "krrwwrrk",
        "krwwwwrk",
        "krwwwwrk",
        "krrwwrrk",
        ".krrrrk.",
        "..kkkk..",
    ]
    no[3] = "krrrrrrk"
    no[4] = "kwwwwwwk"
    no[2] = "krrrrrrk"
    no[5] = "krrrrrrk"
    rows = [list(r) for r in build]
    for j, row in enumerate(no):
        for i, ch in enumerate(row):
            if ch != ".":
                rows[9 + j][10 + i] = ch
    art("CursorCant", ["".join(r) for r in rows])
    art("CursorTarget", [                      # hot spot: centre (7, 7)
        "......kkk......",
        "......kck......",
        "......kck......",
        "......kck......",
        "......kkk......",
        "...............",
        "kkkkk.....kkkkk",
        "kccck..r..kccck",
        "kkkkk.....kkkkk",
        "...............",
        "......kkk......",
        "......kck......",
        "......kck......",
        "......kck......",
        "......kkk......",
    ])


def tile(name, main, dark, fill_alpha, cross=False, inset=0):
    """32x32 tile marker: corner brackets with an ink edge and a faint fill."""
    n = 32
    img = Image.new("RGBA", (n, n), (0, 0, 0, 0))
    px = img.load()
    a, b = inset, n - 1 - inset
    for y in range(a + 2, b - 1):
        for x in range(a + 2, b - 1):
            px[x, y] = main[:3] + (fill_alpha,)
    arm = 8
    def bracket(cx, cy, dx, dy):
        for i in range(arm):
            for t, col in ((0, C["k"]), (1, main), (2, dark), (3, C["k"])):
                px[cx + dx * i, cy + dy * t] = col if i < arm - 1 or t in (1, 2) else C["k"]
                px[cx + dx * t, cy + dy * i] = col if i < arm - 1 or t in (1, 2) else C["k"]
        for t in range(4):                      # square the corner
            for u in range(4):
                edge = t in (0, 3) or u in (0, 3)
                px[cx + dx * t, cy + dy * u] = C["k"] if (t == 0 or u == 0) else (main if (t == 1 or u == 1) else (dark if (t == 2 or u == 2) else C["k"]))
        px[cx + dx * (arm - 1), cy] = C["k"]
        px[cx, cy + dy * (arm - 1)] = C["k"]
        for t in range(4):
            px[cx + dx * arm, cy + dy * t] = C["k"] if cx + dx * arm in range(n) else px[cx, cy]
            px[cx + dx * t, cy + dy * arm] = C["k"]
    bracket(a, a, 1, 1)
    bracket(b, a, -1, 1)
    bracket(a, b, 1, -1)
    bracket(b, b, -1, -1)
    if cross:
        for i in range(10, 22):
            for o in (0, 1):
                px[i + o, i] = main
                px[n - 1 - i - o, i] = main
        for i in range(10, 22):
            for x, y in ((i - 1, i), (i + 2, i), (n - i, i), (n - 3 - i, i)):
                if px[x, y][3] < 255:
                    px[x, y] = C["k"]
    return save(name, img)


def range_dash():
    # Tileable strip for a LineRenderer range ring: cream dash, gap, with an ink edge above and below.
    img = Image.new("RGBA", (8, 4), (0, 0, 0, 0))
    for x in range(5):
        img.putpixel((x, 0), C["k"])
        img.putpixel((x, 1), C["w"])
        img.putpixel((x, 2), C["c"])
        img.putpixel((x, 3), C["k"])
    save("RangeDash", img)


def path_chevron():
    art("PathChevron", [
        "kk.....",
        "kck....",
        "kcck...",
        ".kcck..",
        "..kcck.",
        "...kcsk",
        "..kcsk.",
        ".kcsk..",
        "kcsk...",
        "ksk....",
        "kk.....",
    ])


def ring_points(cx, cy, r, n):
    return [(cx + r * math.cos(2 * math.pi * i / n), cy + r * math.sin(2 * math.pi * i / n)) for i in range(n)]


def preview():
    def icon(rel):
        return Image.open(SANDBOX / rel).convert("RGBA")

    grass = icon("Tiles/GroundGrass.png") if (SANDBOX / "Tiles" / "GroundGrass.png").exists() else None
    W, H = 256, 128
    pv = Image.new("RGBA", (W, H), (0x4A, 0x6A, 0x32, 255))
    if grass is not None:
        for y in range(0, H, grass.height):
            for x in range(0, W, grass.width):
                pv.alpha_composite(grass, (x, y))

    S = SPRITES
    # three tiles: valid (with a tower ghost and range ring), invalid, selected
    pv.alpha_composite(S["TileValid"], (32, 48))
    tower = icon("Animations/Sheets/Towers/TowerLinear.png").crop((0, 0, 32, 32))
    tower.putalpha(tower.getchannel("A").point(lambda a: a * 3 // 5))
    pv.alpha_composite(tower, (32, 48))
    dash = S["RangeDash"]
    n = 34
    for i, (x, y) in enumerate(ring_points(48, 64, 44, n * 8)):
        col = dash.getpixel((i % 8, 1))
        if col[3] and 0 <= round(x) < W and 0 <= round(y) < H:
            pv.putpixel((round(x), round(y)), C["c"])
    pv.alpha_composite(S["TileInvalid"], (112, 48))
    pv.alpha_composite(S["TileSelected"], (176, 48))
    pv.alpha_composite(icon("Animations/Sheets/Towers/TowerChain.png").crop((0, 0, 32, 32)), (176, 48))
    # path chevrons
    for x in (104, 120, 136, 152, 168, 184):
        pv.alpha_composite(S["PathChevron"], (x, 102))
    # cursors
    pv.alpha_composite(S["Cursor"], (150, 8))
    pv.alpha_composite(S["CursorBuild"], (52, 60))
    pv.alpha_composite(S["CursorCant"], (130, 62))
    pv.alpha_composite(S["CursorTarget"], (214, 12))
    pv.resize((W * 4, H * 4), Image.NEAREST).save(HERE / "MarkersPreview.png")


def main():
    cursors()
    tile("TileValid", C["g"], C["G"], 60)
    tile("TileInvalid", C["r"], C["R"], 70, cross=True)
    tile("TileSelected", C["y"], C["Y"], 0)
    tile("TileHover", C["c"], C["s"], 30, inset=1)
    range_dash()
    path_chevron()
    preview()
    print(f"wrote {len(SPRITES)} sprites")


if __name__ == "__main__":
    main()
