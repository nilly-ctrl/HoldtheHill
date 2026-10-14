"""Flat textures for the graybox: ground and road tiles, weapon line textures, UI skin pieces.

  ../../Tiles/GroundGrass.png   64x64, repeats in both directions (muted, so sprites stand out)
  ../../Tiles/RoadDirt.png      repeats along x; top and bottom rows are the trail's edges
  ../../Tiles/RoadDirtFill.png  the same dirt with no edges, laid over corners so edge lines don't cross them

  ../../Tiles/LineBolt.png      32x8 lightning strip, tiled along the Storm Ant's LineRenderer
  ../../Tiles/LineBeam.png      32x8 beam strip, tiled along the Dewdrop Lens's LineRenderer
  ../../Ui/Panel.png            24x24 nine-slice panel (border 6)
  ../../Ui/Button*.png          16x16 nine-slice buttons (border 5): normal, hover, pressed, disabled

The graybox draws the road one tiled sprite per path segment, rotated along the segment,
so the road tile only has to repeat along its length.

    python build_tiles.py
"""
import os
import random

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.normpath(os.path.join(HERE, "..", "..", "Tiles"))
N = 32


def hexc(h, a=255):
    h = h.lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)


def grass(n=64):
    rng = random.Random(21)
    base, light, dark, blade, speck = (hexc(c) for c in ("#4f6f34", "#5b7d3b", "#43602c", "#6f9446", "#6b5435"))
    im = Image.new("RGBA", (n, n), base)
    p = im.load()
    # soft two-tone mottling, wrapped so the tile repeats
    for _ in range(110):
        cx, cy, r = rng.randrange(n), rng.randrange(n), rng.uniform(2, 5)
        c = light if rng.random() < 0.5 else dark
        for y in range(-6, 7):
            for x in range(-6, 7):
                if x * x + y * y <= r * r and rng.random() < 0.8:
                    p[(cx + x) % n, (cy + y) % n] = c
    # grass blades: 2 px tall ticks
    for _ in range(120):
        x, y = rng.randrange(n), rng.randrange(n)
        p[x, y] = blade
        p[x, (y + 1) % n] = dark
    for _ in range(24):
        p[rng.randrange(n), rng.randrange(n)] = speck
    return im


def road(edges=True):
    rng = random.Random(4)
    dirt, light, dark, edge, pebble, pebble_hi, tuft = (hexc(c) for c in (
        "#9a7044", "#ab7f50", "#87603a", "#5b3a23", "#c4bfb2", "#ece7da", "#6f9446"))
    im = Image.new("RGBA", (N, N), dirt)
    p = im.load()
    for y in range(N):
        for x in range(N):
            r = rng.random()
            if r < 0.12:
                p[x, y] = light
            elif r < 0.24:
                p[x, y] = dark
    # ant-foot tracks: two faint lanes along the trail
    for lane in (11, 20):
        for x in range(N):
            if rng.random() < 0.55:
                p[x, lane] = dark
    # pebbles (wrap in x only)
    for _ in range(7):
        x, y = rng.randrange(N), rng.randrange(5, N - 5)
        p[x, y] = pebble
        p[(x + 1) % N, y] = pebble
        p[x, y - 1] = pebble_hi
    if not edges:
        return im
    # edges: dark rim with ragged inner line and grass tufts poking in
    for x in range(N):
        for y in (0, 1, N - 2, N - 1):
            p[x, y] = edge
        if rng.random() < 0.45:
            p[x, 2] = edge
        if rng.random() < 0.45:
            p[x, N - 3] = edge
    for _ in range(5):
        x = rng.randrange(N)
        p[x, 0], p[x, 1] = tuft, tuft
        x = rng.randrange(N)
        p[x, N - 1], p[x, N - 2] = tuft, tuft
    return im


def line_bolt():
    """Jagged white-yellow bolt, 32x8, wrapping in x."""
    rng = random.Random(12)
    im = Image.new("RGBA", (N, 8), (0, 0, 0, 0))
    p = im.load()
    ys = [4]
    for x in range(1, N):
        step = rng.choice((-1, -1, 0, 1, 1)) if x % 2 else 0
        ys.append(min(6, max(1, ys[-1] + step)))
    ys[-1] = ys[0]
    ys[-2] = min(6, max(1, (ys[-3] + ys[0]) // 2))
    for x, y in enumerate(ys):
        for dy, c in ((-1, hexc("#9fe7ff", 150)), (1, hexc("#ffd23a", 170))):
            if 0 <= y + dy < 8:
                p[x, y + dy] = c
        p[x, y] = hexc("#ffffff")
        if x % 7 == 3:  # small forks
            fy = y - 2 if y > 2 else y + 2
            p[x, fy] = hexc("#ffd23a", 200)
    return im


def line_beam():
    """Hot white core with a blue sheath and drifting bright nodes, 32x8, wrapping in x."""
    im = Image.new("RGBA", (N, 8), (0, 0, 0, 0))
    p = im.load()
    rows = {0: ("#5aa9d8", 60), 1: ("#5aa9d8", 150), 2: ("#9fe7ff", 230), 3: ("#ffffff", 255),
            4: ("#ffffff", 255), 5: ("#9fe7ff", 230), 6: ("#5aa9d8", 150), 7: ("#5aa9d8", 60)}
    for y, (c, a) in rows.items():
        for x in range(N):
            p[x, y] = hexc(c, a)
    for x0 in (4, 20):  # nodes that slide along as the texture scrolls
        for dx in range(4):
            p[(x0 + dx) % N, 2] = hexc("#ffffff")
            p[(x0 + dx) % N, 5] = hexc("#ffffff")
        p[(x0 + 1) % N, 1] = hexc("#9fe7ff", 230)
        p[(x0 + 2) % N, 6] = hexc("#9fe7ff", 230)
    return im


def nine_slice(size, fill, top, bottom, rim, ink="#1b110b", fill_alpha=255):
    """Rounded pixel box: ink outline, rim line, lit top edge and shaded bottom edge."""
    im = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    p = im.load()
    last = size - 1
    for y in range(size):
        for x in range(size):
            corner = (x in (0, last)) and (y in (0, last))
            if corner:
                continue
            edge = x in (0, last) or y in (0, last)
            inner_corner = (x in (1, last - 1)) and (y in (1, last - 1))
            if edge or inner_corner:
                p[x, y] = hexc(ink)
            elif x in (1, last - 1) or y in (1, last - 1):
                p[x, y] = hexc(rim)
            elif y == 2:
                p[x, y] = hexc(top)
            elif y >= last - 3:
                p[x, y] = hexc(bottom, fill_alpha)
            else:
                p[x, y] = hexc(fill, fill_alpha)
    return im


def main():
    os.makedirs(OUT, exist_ok=True)
    ui = os.path.normpath(os.path.join(OUT, "..", "Ui"))
    os.makedirs(ui, exist_ok=True)
    for name, im in (("GroundGrass", grass()), ("RoadDirt", road()), ("RoadDirtFill", road(edges=False)),
                     ("LineBolt", line_bolt()), ("LineBeam", line_beam())):
        im.save(os.path.join(OUT, name + ".png"))
    for name, im in (
            ("Panel", nine_slice(24, "#2a1d14", "#3a2a1e", "#22170f", "#b5873e", fill_alpha=238)),
            ("Button", nine_slice(16, "#83562f", "#b5873e", "#5b3a23", "#a0643c")),
            ("ButtonHover", nine_slice(16, "#a0643c", "#ecc477", "#83562f", "#ecc477")),
            ("ButtonPressed", nine_slice(16, "#5b3a23", "#3a2416", "#83562f", "#83562f")),
            ("ButtonDisabled", nine_slice(16, "#4a4038", "#5c5148", "#3a322c", "#5c5148"))):
        im.save(os.path.join(ui, name + ".png"))
    # 4x4 preview of the tiles repeating, with a road strip across the middle
    prev = Image.new("RGBA", (N * 8, N * 6))
    g, r = grass(), road()
    for ty in range(3):
        for tx in range(4):
            prev.paste(g, (tx * 2 * N, ty * 2 * N))
    for tx in range(8):
        prev.paste(r, (tx * N, 3 * N))
    prev.resize((prev.width * 4, prev.height * 4), Image.NEAREST).save(os.path.join(HERE, "TilePreview.png"))
    strip = Image.new("RGBA", (N * 4, 60), hexc("#2a1d14"))
    for k in range(4):
        strip.alpha_composite(line_bolt(), (k * N, 4))
        strip.alpha_composite(line_beam(), (k * N, 18))
    for k, name in enumerate(("Panel", "Button", "ButtonHover", "ButtonPressed", "ButtonDisabled")):
        strip.alpha_composite(Image.open(os.path.join(ui, name + ".png")), (4 + k * 26, 32))
    strip.resize((strip.width * 4, strip.height * 4), Image.NEAREST).save(os.path.join(HERE, "LineUiPreview.png"))
    print("tiles written to", OUT, "and", ui)


if __name__ == "__main__":
    main()
