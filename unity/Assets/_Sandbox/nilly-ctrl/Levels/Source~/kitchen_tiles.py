"""Kitchen floor tiles, drawn at ant scale: 32 px is one cell and one world unit.

The floor is small ceramic tiles in two tones, one tile per cell, so the tile grid is also
the build grid. The enemy road is a wide grout channel where a row of tiles is missing.
Light comes from the top left: tiles are raised above the grout, and cabinets and the
fridge throw a shadow down and to the right.

Every function takes a `seed`, so the same cell always draws the same way.

    python kitchen_tiles.py      writes KitchenTiles.png, a labelled sheet of every piece
"""
import math
import os
import random

from PIL import Image, ImageDraw

N = 32
HERE = os.path.dirname(os.path.abspath(__file__))


def hexc(h, a=255):
    h = h.lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)


PAL = {
    # floor tiles: (base, highlight, lowlight)
    "cream": ("#cfc6ad", "#ddd5bf", "#b9b097"),
    "sage": ("#93a79c", "#a5b8ad", "#7f948a"),
    "seam": "#6f675b",
    "crack": "#857c6c",
    # grout road
    "grout": "#7a7166", "grout_hi": "#8a8176", "grout_lo": "#696055",
    "grout_shade": "#4a4239", "grout_lip": "#9a9184", "grit": "#a59c8c",
    # counter top (cabinets) and fridge, seen from above
    "counter": "#3b4048", "counter_hi": "#4b515b", "counter_lo": "#2f333a",
    "trim": "#8a6a45", "trim_hi": "#b08a5c", "trim_lo": "#5e4529",
    "steel": "#c5ccd3", "steel_hi": "#e1e7ec", "steel_lo": "#9aa3ad",
    "steel_seam": "#6d7680", "handle": "#4d555e",
    # things on the floor
    "wood": "#8a5a34", "wood_hi": "#a97647", "wood_lo": "#5e3b20",
    "mat": "#9c4a45", "mat_hi": "#b8605a", "mat_lo": "#7a3835", "mat_stripe": "#c9b48f",
    "crumb": "#e2c078", "crumb_hi": "#f5e0a6", "crumb_lo": "#b98f4a",
    "spill": "#8c4f9e", "spill_rim": "#6b3a7a", "spill_hi": "#e6c9f0",
    "hole": "#17110c", "hole_rim": "#3a3028",
    "soil": "#6b4a2e", "soil_hi": "#8a6540", "soil_lo": "#4a321e", "sugar": "#efe6cf",
}
C = {k: (tuple(hexc(x) for x in v) if isinstance(v, tuple) else hexc(v)) for k, v in PAL.items()}


def _rng(seed):
    return random.Random(seed * 7919 + 13)


# ---------- floor ----------

def floor(tone, seed=0):
    """One ceramic tile. `tone` is 'cream' or 'sage'."""
    rng = _rng(seed)
    base, hi, lo = C[tone]
    im = Image.new("RGBA", (N, N), base)
    p = im.load()
    for y in range(N):
        for x in range(N):
            r = rng.random()
            if r < 0.05:
                p[x, y] = hi
            elif r < 0.10:
                p[x, y] = lo
    for i in range(1, N - 1):
        p[i, 1], p[1, i] = hi, hi
        p[i, N - 2], p[N - 2, i] = lo, lo
    for i in range(N):
        p[i, 0] = p[0, i] = p[i, N - 1] = p[N - 1, i] = C["seam"]
    # about one tile in seven has a hairline crack or a chipped corner
    roll = rng.random()
    if roll < 0.08:
        x, y = rng.randrange(6, 26), 2
        while y < N - 2:
            p[x, y] = C["crack"]
            x = min(N - 3, max(2, x + rng.choice((-1, 0, 0, 1))))
            y += 1
    elif roll < 0.15:
        cx, cy = rng.choice(((2, 2), (N - 3, 2), (2, N - 3), (N - 3, N - 3)))
        for y in range(-4, 5):
            for x in range(-4, 5):
                if abs(x) + abs(y) <= 4 and 1 <= cx + x <= N - 2 and 1 <= cy + y <= N - 2:
                    p[cx + x, cy + y] = C["seam"] if abs(x) + abs(y) < 4 else lo
    return im


def mat(seed=0, edges=(False, False, False, False)):
    """Woven mat. `edges` is (N, E, S, W): True where the mat ends, which gets a hem or a fringe."""
    rng = _rng(seed)
    im = Image.new("RGBA", (N, N), C["mat"])
    p = im.load()
    for y in range(N):
        for x in range(N):
            if (x + y) % 4 == 0:
                p[x, y] = C["mat_hi"]
            elif (x - y) % 4 == 0:
                p[x, y] = C["mat_lo"]
            if y % 16 in (6, 7, 8) and (x + y) % 2:
                p[x, y] = C["mat_stripe"]
            if rng.random() < 0.03:
                p[x, y] = C["mat_lo"]
    north, east, south, west = edges
    for i in range(N):
        if north:
            p[i, 0], p[i, 1] = C["mat_lo"], C["mat_hi"]
        if south:
            p[i, N - 1], p[i, N - 2] = C["mat_lo"], C["mat_lo"]
        if west:  # fringe on the short ends
            p[0, i] = C["mat_stripe"] if i % 2 else C["mat_lo"]
            p[1, i] = C["mat_stripe"] if i % 2 else C["mat"]
            p[2, i] = C["mat_lo"]
        if east:
            p[N - 1, i] = C["mat_stripe"] if i % 2 else C["mat_lo"]
            p[N - 2, i] = C["mat_stripe"] if i % 2 else C["mat"]
            p[N - 3, i] = C["mat_lo"]
    return im


# ---------- road ----------

def road(links=(False, True, False, True), seed=0):
    """Grout channel. `links` is (N, E, S, W): True where the road continues into the next cell."""
    rng = _rng(seed)
    im = Image.new("RGBA", (N, N), C["grout"])
    p = im.load()
    for y in range(N):
        for x in range(N):
            r = rng.random()
            if r < 0.14:
                p[x, y] = C["grout_hi"]
            elif r < 0.28:
                p[x, y] = C["grout_lo"]
    for _ in range(5):
        x, y = rng.randrange(4, N - 4), rng.randrange(4, N - 4)
        p[x, y] = C["grit"]
        if rng.random() < 0.5:
            p[x + 1, y] = C["grit"]
    if rng.random() < 0.35:
        x, y = rng.randrange(6, N - 6), rng.randrange(6, N - 6)
        p[x, y], p[x + 1, y], p[x, y - 1] = C["crumb"], C["crumb_lo"], C["crumb_hi"]
    north, east, south, west = links
    # Where the channel meets a tile: the tile's seam, then its shadow on the lit sides' far edge.
    for i in range(N):
        if not north:  # the tile above overhangs: a deep shadow
            p[i, 0] = C["seam"]
            p[i, 1] = p[i, 2] = C["grout_shade"]
            if rng.random() < 0.5:
                p[i, 3] = C["grout_shade"]
        if not west:
            p[0, i] = C["seam"]
            p[1, i] = C["grout_shade"]
            if rng.random() < 0.4:
                p[2, i] = C["grout_shade"]
        if not south:  # facing the light: a pale lip
            p[i, N - 1] = C["seam"]
            p[i, N - 2] = C["grout_lip"]
        if not east:
            p[N - 1, i] = C["seam"]
            p[N - 2, i] = C["grout_lip"]
    return im


def hole(seed=0, toward=None):
    """The dark opening enemies crawl out of, drawn over a road cell. `toward` is 'N', 'E', 'S'
    or 'W' when the spawn is on the edge of the map: the hole then runs off that edge."""
    im = Image.new("RGBA", (N, N), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    box = {"N": (6, -12, 25, 21), "S": (6, 10, 25, 43), "W": (-12, 6, 21, 25), "E": (10, 6, 43, 25)}.get(
        toward, (6, 6, 25, 25))
    d.rounded_rectangle(box, radius=9, fill=C["hole_rim"])
    d.rounded_rectangle((box[0] + 2, box[1] + 2, box[2] - 2, box[3] - 2), radius=7, fill=C["hole"])
    rng = _rng(seed)
    p = im.load()
    for _ in range(10):  # loose grit around the mouth
        x, y = rng.randrange(3, N - 3), rng.randrange(3, N - 3)
        if p[x, y][3] == 0:
            p[x, y] = C["soil_lo"] if rng.random() < 0.5 else C["grit"]
    return im


def hill(width_cells=2, height_cells=1, seed=0):
    """The ant hill: a ring of carried-up soil and sugar round a dark entrance. One image for the
    whole hill, laid over its cells."""
    w, h = width_cells * N, height_cells * N
    rng = _rng(seed)
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    p = im.load()
    cx, cy = (w - 1) / 2, (h - 1) / 2
    rx, ry = w / 2 - 2, h / 2 - 2
    for y in range(h):
        for x in range(w):
            d = math.hypot((x - cx) / rx, (y - cy) / ry) + rng.uniform(-0.07, 0.07)
            if d > 1:
                continue
            if d < 0.22:
                p[x, y] = C["hole"]
            elif d < 0.32:
                p[x, y] = C["soil_lo"]
            else:
                # lit from the top left, so that side of the mound is paler
                lit = ((cx - x) / rx + (cy - y) / ry) * 0.5
                r = rng.random() + lit * 0.5
                p[x, y] = C["soil_hi"] if r > 0.75 else C["soil_lo"] if r < 0.22 else C["soil"]
                if d > 0.86 and rng.random() < 0.45:
                    p[x, y] = (0, 0, 0, 0)  # ragged rim
                elif rng.random() < 0.035:
                    p[x, y] = C["sugar"]
    return im


# ---------- tall things ----------

def _slab(base, hi, lo, seed, fleck=0.10):
    rng = _rng(seed)
    im = Image.new("RGBA", (N, N), base)
    p = im.load()
    for y in range(N):
        for x in range(N):
            r = rng.random()
            if r < fleck / 2:
                p[x, y] = hi
            elif r < fleck:
                p[x, y] = lo
    return im


def cabinet(seed=0, open_sides=(False, False, True, False)):
    """Counter top seen from above. `open_sides` is (N, E, S, W): True where it ends, which gets
    a wooden edge strip."""
    im = _slab(C["counter"], C["counter_hi"], C["counter_lo"], seed)
    p = im.load()
    north, east, south, west = open_sides
    for i in range(N):
        if north:
            p[i, 0], p[i, 1], p[i, 2] = C["trim_hi"], C["trim"], C["trim_lo"]
        if west:
            p[0, i], p[1, i], p[2, i] = C["trim_hi"], C["trim"], C["trim_lo"]
        if south:
            p[i, N - 3], p[i, N - 2], p[i, N - 1] = C["trim_hi"], C["trim"], C["trim_lo"]
        if east:
            p[N - 3, i], p[N - 2, i], p[N - 1, i] = C["trim_hi"], C["trim"], C["trim_lo"]
    return im


def fridge(seed=0, open_sides=(False, False, True, False)):
    """Fridge top. The side that faces the floor below gets the door: a seam and a handle bar."""
    rng = _rng(seed)
    im = Image.new("RGBA", (N, N), C["steel"])
    p = im.load()
    for y in range(N):  # brushed steel: short horizontal streaks
        for x in range(N):
            if rng.random() < 0.10:
                p[x, y] = C["steel_hi"] if rng.random() < 0.5 else C["steel_lo"]
                if x + 1 < N:
                    p[x + 1, y] = p[x, y]
    north, east, south, west = open_sides
    for i in range(N):
        if north:
            p[i, 0], p[i, 1] = C["steel_seam"], C["steel_hi"]
        if west:
            p[0, i], p[1, i] = C["steel_seam"], C["steel_hi"]
        if east:
            p[N - 1, i], p[N - 2, i] = C["steel_seam"], C["steel_lo"]
        if south:
            p[i, N - 1] = C["steel_seam"]
            p[i, N - 2] = p[i, N - 3] = C["steel_lo"]
            p[i, N - 9] = C["steel_seam"]  # the door's top edge
            if 5 <= i <= N - 6:
                p[i, N - 6], p[i, N - 5] = C["handle"], C["steel_hi"]
    return im


def leg(seed=0):
    """A round wooden table or chair leg with its shadow, drawn over a floor tile."""
    im = Image.new("RGBA", (N, N), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.ellipse((9, 9, 30, 30), fill=(0, 0, 0, 80))
    d.ellipse((4, 4, 25, 25), fill=C["wood_lo"])
    d.ellipse((5, 5, 24, 24), fill=C["wood"])
    d.arc((7, 7, 22, 22), 150, 300, fill=C["wood_hi"])
    d.ellipse((10, 10, 19, 19), outline=C["wood_lo"])
    d.ellipse((13, 13, 16, 16), fill=C["wood_lo"])
    d.point([(8, 11), (9, 9), (11, 8)], fill=C["wood_hi"])
    return im


# ---------- small things ----------

def crumbs(seed=0):
    rng = _rng(seed)
    im = Image.new("RGBA", (N, N), (0, 0, 0, 0))
    p = im.load()
    cx, cy = rng.randrange(12, 20), rng.randrange(12, 20)
    for i in range(9):
        x = cx + int(rng.gauss(0, 5)) if i else cx
        y = cy + int(rng.gauss(0, 5)) if i else cy
        size = 3 if i == 0 else rng.choice((1, 1, 2))
        for dy in range(size):
            for dx in range(size):
                if 2 <= x + dx < N - 2 and 2 <= y + dy < N - 2:
                    p[x + dx, y + dy] = C["crumb_hi"] if dx == 0 and dy == 0 else \
                        C["crumb_lo"] if dx == size - 1 and dy == size - 1 and size > 1 else C["crumb"]
        if 2 <= x + size < N - 2 and 2 <= y + size < N - 2 and size > 1:
            p[x + size, y + size] = (0, 0, 0, 60)
    return im


def spill(seed=0, links=(False, False, False, False)):
    """A sticky puddle. `links` is (N, E, S, W): True where the next cell is also spill, so the
    puddle runs across the join."""
    rng = _rng(seed)
    mask = Image.new("L", (N, N), 0)
    d = ImageDraw.Draw(mask)
    d.ellipse((5, 6, 26, 25), fill=255)
    for _ in range(4):
        x, y, r = rng.randrange(8, 24), rng.randrange(8, 24), rng.randrange(4, 8)
        d.ellipse((x - r, y - r, x + r, y + r), fill=255)
    north, east, south, west = links
    if north:
        d.rectangle((9, 0, 22, 14), fill=255)
    if south:
        d.rectangle((9, 17, 22, N - 1), fill=255)
    if west:
        d.rectangle((0, 10, 14, 21), fill=255)
    if east:
        d.rectangle((17, 10, N - 1, 21), fill=255)
    im = Image.new("RGBA", (N, N), (0, 0, 0, 0))
    p, m = im.load(), mask.load()
    body, rim = C["spill"][:3] + (175,), C["spill_rim"][:3] + (215,)
    for y in range(N):
        for x in range(N):
            if not m[x, y]:
                continue
            edge = any(not (0 <= x + dx < N and 0 <= y + dy < N) or not m[x + dx, y + dy]
                       for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))
            inside_join = (north and y == 0) or (south and y == N - 1) or (west and x == 0) or (east and x == N - 1)
            p[x, y] = rim if edge and not inside_join else body
    for x, y in ((11, 10), (12, 10), (10, 11), (19, 18)):  # glints
        if m[x, y]:
            p[x, y] = C["spill_hi"]
    return im


def shadow(north=False, west=False, corner=False):
    """What a cabinet or fridge throws onto the cell below it (`north`), to its right (`west`),
    or diagonally below and right (`corner`). Transparent black, laid over the finished cell."""
    im = Image.new("RGBA", (N, N), (0, 0, 0, 0))
    p = im.load()
    depth, strength = 9, 105
    for y in range(N):
        for x in range(N):
            a = 0
            if north and y < depth:
                a = max(a, strength * (depth - y) // depth)
            if west and x < depth - 3:
                a = max(a, strength * (depth - 3 - x) // (depth - 3))
            if corner and not north and not west and y < depth and x < depth - 3:
                a = max(a, min(strength * (depth - y) // depth, strength * (depth - 3 - x) // (depth - 3)))
            if a:
                p[x, y] = (0, 0, 0, a)
    return im


# ---------- sheet ----------

def _sheet():
    def over(base, top):
        out = base.copy()
        out.alpha_composite(top)
        return out

    f = lambda: floor("cream", 3)
    r = lambda: road((False, True, False, True), 3)
    pieces = [
        ("floor cream", floor("cream", 1)), ("floor sage", floor("sage", 2)),
        ("cracked", floor("cream", 22)), ("chipped", floor("sage", 4)),
        ("road E-W", road((False, True, False, True), 1)), ("road N-S", road((True, False, True, False), 2)),
        ("bend S-E", road((False, True, True, False), 3)), ("bend N-W", road((True, False, False, True), 4)),
        ("spawn", over(r(), hole(1))), ("spawn at edge", over(r(), hole(1, "W"))),
        ("cabinet", cabinet(1)), ("cabinet end", cabinet(2, (False, True, True, False))),
        ("fridge", fridge(1)), ("fridge end", fridge(2, (False, True, True, False))),
        ("shadow below", over(f(), shadow(north=True))), ("shadow right", over(f(), shadow(west=True))),
        ("leg", over(f(), leg())), ("mat", mat(1)), ("mat end", mat(2, (True, False, False, True))),
        ("crumbs", over(f(), crumbs(5))), ("spill", over(f(), spill(1))),
        ("spill join", over(f(), spill(2, (False, True, False, False)))),
    ]
    hill_piece = Image.new("RGBA", (N * 2, N), C["grout"])
    hill_piece.alpha_composite(road((False, False, False, True), 5), (0, 0))
    hill_piece.alpha_composite(road((False, False, False, False), 6), (N, 0))
    hill_piece.alpha_composite(hill(2, 1, 1))
    scale, cols, pad, label = 3, 6, 10, 12
    rows = (len(pieces) + cols - 1) // cols + 1
    cw, ch = N * scale + pad, N * scale + pad + label
    sheet = Image.new("RGBA", (cols * cw + pad, rows * ch + pad), hexc("#22252b"))
    d = ImageDraw.Draw(sheet)
    for i, (name, im) in enumerate(pieces):
        x, y = pad + (i % cols) * cw, pad + (i // cols) * ch
        sheet.alpha_composite(im.resize((N * scale, N * scale), Image.NEAREST), (x, y))
        d.text((x, y + N * scale + 1), name, fill=hexc("#d8d2c4"))
    x, y = pad, pad + (rows - 1) * ch
    sheet.alpha_composite(hill_piece.resize((N * 2 * scale, N * scale), Image.NEAREST), (x, y))
    d.text((x, y + N * scale + 1), "hill (2 cells)", fill=hexc("#d8d2c4"))
    out = os.path.join(HERE, "KitchenTiles.png")
    sheet.save(out)
    print("wrote", out)


if __name__ == "__main__":
    _sheet()
