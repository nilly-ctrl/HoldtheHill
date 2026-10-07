"""Hold the Hill title logo, pixel art.

Uses the 5x7 alphabet from Fonts/Source~/build_pixel_fonts.py and the ant sprite from
build_pixel_icons.py, so the logo matches the fonts and icons.

Writes ../PNG/TitleLogo.png (native pixels, transparent) and ./TitleLogoPreview.png (4x on dark).

    python build_logo.py
"""
import importlib.util
import os

from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


fonts = load("fonts", os.path.normpath(os.path.join(HERE, "..", "..", "Fonts", "Source~", "build_pixel_fonts.py")))
icons = load("icons", os.path.join(HERE, "build_pixel_icons.py"))

INK = "#1b110b"
W, H = 168, 98


def hexc(c, a=255):
    c = c.lstrip("#")
    return (int(c[0:2], 16), int(c[2:4], 16), int(c[4:6], 16), a)


def word_mask(text, scale, gap):
    """Bold letters laid out in a row; returns a 2D bool grid."""
    cols = []
    height = 7 * scale
    for i, ch in enumerate(text):
        m, w, h = fonts.mask_of(ch, True, scale, None)
        for x in range(w):
            cols.append([m[y][x] for y in range(h)])
        if i < len(text) - 1:
            for _ in range(gap if ch != " " else 0):
                cols.append([False] * height)
    return [[cols[x][y] for x in range(len(cols))] for y in range(height)]


def paint_word(canvas, mask, ox, oy, bands, outline, thick, shadow, hi=None, lo=None):
    h, w = len(mask), len(mask[0])
    px = canvas.load()

    def on(x, y):
        return 0 <= y < h and 0 <= x < w and mask[y][x]

    def put(x, y, c):
        if 0 <= x < W and 0 <= y < H:
            px[x, y] = c

    sdx, sdy, scol = shadow
    ol = set()
    for y in range(h):
        for x in range(w):
            if mask[y][x]:
                for dy in range(-thick, thick + 1):
                    for dx in range(-thick, thick + 1):
                        if abs(dx) + abs(dy) <= thick + (thick > 1):
                            ol.add((x + dx, y + dy))
    for x, y in ol:
        put(ox + x + sdx, oy + y + sdy, hexc(scol))
    for x, y in ol:
        put(ox + x, oy + y, hexc(outline))
    for y in range(h):
        band = hexc(bands[min(len(bands) - 1, y * len(bands) // h)])
        for x in range(w):
            if not mask[y][x]:
                continue
            c = band
            if hi and not on(x, y - 1):
                c = hexc(hi)
            elif lo and not on(x, y + 1):
                c = hexc(lo)
            put(ox + x, oy + y, c)
    return w, h


def blit_sprite(canvas, rows, ox, oy):
    px = canvas.load()
    for j, row in enumerate(rows):
        for i, ch in enumerate(row):
            if ch != "." and 0 <= ox + i < W and 0 <= oy + j < H:
                px[ox + i, oy + j] = icons.rgba(ch)


def main():
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)

    # the hill: a soil mound the big word sits in
    hill_top = 54

    def hill_y(x):
        t = (x - (W - 1) / 2) / ((W - 1) / 2)
        return round(hill_top + (t * t) * 30)

    for x in range(W):
        top = hill_y(x)
        for y in range(top, H):
            depth = y - top
            col = "#83562f" if depth < 10 else ("#5b3a23" if depth < 24 else "#3a2416")
            if depth in (10, 24) and (x + y) % 2:
                col = "#83562f" if depth == 10 else "#5b3a23"
            im.putpixel((x, y), hexc(col))
        if 0 <= top - 1 < H:
            im.putpixel((x, top - 1), hexc(INK))
        if top < H:
            im.putpixel((x, top), hexc("#7cc84a"))        # grass rim
            if top + 1 < H and x % 3 != 0:
                im.putpixel((x, top + 1), hexc("#4c8a2c"))
    # trim the mound's bottom corners to a rounded base
    for x in range(W):
        for y in range(H):
            edge = min(x, W - 1 - x)
            if y > H - 1 - max(0, 6 - edge):
                im.putpixel((x, y), (0, 0, 0, 0))
    d.line([(0, H - 1), (W - 1, H - 1)], fill=hexc(INK))

    # "HOLD THE" - small, cream
    small = word_mask("HOLD THE", 2, 2)
    sx = (W - len(small[0])) // 2
    paint_word(im, small, sx, 4, ["#ffffff", "#fff1d6", "#ecc477"], INK, 1, (1, 2, "#3a2416"))

    # "HILL" - big, amber to soil-orange
    big = word_mask("HILL", 5, 5)
    bx = (W - len(big[0])) // 2
    by = 44
    paint_word(im, big, bx, by, ["#fff6c8", "#ffd88a", "#f2a93b", "#e07a2e", "#c9562a"], INK, 2,
               (2, 3, "#2a1608"), hi="#ffffff", lo="#8a3414")

    # flag planted on the I, ant marching up the hill to the left
    i_left = bx + len(word_mask("H", 5, 5)[0]) + 5
    i_mid = i_left + len(word_mask("I", 5, 5)[0]) // 2
    pole_top = by - 17
    d.line([(i_mid, pole_top), (i_mid, by - 3)], fill=hexc(INK), width=3)
    d.line([(i_mid, pole_top + 1), (i_mid, by - 3)], fill=hexc("#b5873e"), width=1)
    flag = [(i_mid + 2, pole_top), (i_mid + 15, pole_top + 1), (i_mid + 11, pole_top + 5), (i_mid + 15, pole_top + 10),
            (i_mid + 2, pole_top + 9)]
    d.polygon(flag, fill=hexc("#e2412f"), outline=hexc(INK))
    d.line([(i_mid + 3, pole_top + 2), (i_mid + 9, pole_top + 2)], fill=hexc("#ff6f7d"))

    # Ants at double size so they read at a glance: one marching along the top of the H towards
    # the flag, one coming up the right-hand slope in front of the lettering.
    ant = ["".join(ch * 2 for ch in row) for row in icons.SPR["ant_side"] for _ in (0, 1)]
    blit_sprite(im, ant, bx + 1, by - 2 - len(ant))
    blit_sprite(im, [r[::-1] for r in ant], W - 31, hill_y(W - 17) - len(ant))

    out = os.path.normpath(os.path.join(HERE, "..", "PNG", "TitleLogo.png"))
    im.save(out)
    prev = Image.new("RGBA", (W * 4 + 32, H * 4 + 32), hexc("#241d18"))
    prev.alpha_composite(im.resize((W * 4, H * 4), Image.NEAREST), (16, 16))
    prev.save(os.path.join(HERE, "TitleLogoPreview.png"))
    print("wrote TitleLogo.png", im.size)


if __name__ == "__main__":
    main()
