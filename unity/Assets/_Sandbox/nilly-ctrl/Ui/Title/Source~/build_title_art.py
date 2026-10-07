"""Title screen art for Hold the Hill: a 320x180 pixel-art backdrop and a layout mockup.

320x180 scales by whole numbers to 1280x720 (4x) and 1920x1080 (6x).

Writes into this folder (Unity ignores it):
  TitleBackground.png   the backdrop on its own
  TitleMockup.png       backdrop + logo + buttons, 4x, to show the intended layout

Copy TitleBackground.png up one folder when the title screen is built.

    python build_title_art.py
"""
import importlib.util
import math
import random
from pathlib import Path

from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
SANDBOX = HERE.parent.parent.parent
W, H = 320, 180

SKY = ["#241238", "#2e1742", "#3b1d4a", "#4c2150", "#662852", "#8a2f4e", "#b53e43", "#d8553f", "#f07444", "#ff9a4e", "#ffb35a"]
INK = "#1b110b"


def hexc(c, a=255):
    c = c.lstrip("#")
    return (int(c[0:2], 16), int(c[2:4], 16), int(c[4:6], 16), a)


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


def ridge(seed, base, amp, step):
    """A soft hill line: y for every x."""
    rnd = random.Random(seed)
    pts = [base + rnd.uniform(-amp, amp) for _ in range(W // step + 3)]
    out = []
    for x in range(W):
        i, f = divmod(x / step, 1)
        i = int(i)
        t = (1 - math.cos(f * math.pi)) / 2
        out.append(round(pts[i] * (1 - t) + pts[i + 1] * t))
    return out


def background(icons):
    im = Image.new("RGBA", (W, H))
    px = im.load()
    horizon = 118
    for y in range(H):
        f = min(len(SKY) - 1.001, y / horizon * (len(SKY) - 1))
        i = int(f)
        for x in range(W):
            dither = 1 if (f - i) > 0.5 and (x + y) % 2 else 0
            px[x, y] = hexc(SKY[min(len(SKY) - 1, i + dither)])
    d = ImageDraw.Draw(im)

    # stars in the upper sky
    rnd = random.Random(7)
    for _ in range(46):
        x, y = rnd.randrange(W), rnd.randrange(0, 58)
        px[x, y] = hexc("#fff1d6" if rnd.random() < 0.3 else "#b98cff")

    # setting sun, low on the left behind the hill, clear of the logo and the menu
    sx, sy = 46, 100
    for r, col in [(30, "#ffb03a"), (26, "#ffc85a"), (21, "#ffd27a"), (15, "#fff2b0")]:
        d.ellipse([sx - r, sy - r, sx + r, sy + r], fill=hexc(col))

    # thin clouds
    for cx, cy, w in [(52, 34, 34), (118, 22, 22), (270, 44, 40), (196, 60, 26), (20, 70, 28)]:
        d.rectangle([cx, cy, cx + w, cy + 1], fill=hexc("#8a2f4e"))
        d.rectangle([cx + 4, cy - 1, cx + w - 6, cy - 1], fill=hexc("#b53e43"))

    # three ridges, far to near
    for seed, base, amp, step, col, rim in [(1, 112, 7, 34, "#4c2150", "#662852"), (2, 122, 6, 26, "#3a1d3a", "#4c2150"),
                                            (3, 132, 5, 40, "#2a2a1c", "#3c4a22")]:
        line = ridge(seed, base, amp, step)
        for x in range(W):
            for y in range(line[x], H):
                px[x, y] = hexc(col)
            px[x, line[x]] = hexc(rim)

    # the hill: a big mound left of centre
    hx, top, half = 112, 82, 100
    def hill_y(x):
        t = (x - hx) / half
        return round(top + (t * t) * 62) if abs(t) <= 1 else H + 1
    for x in range(W):
        y0 = hill_y(x)
        for y in range(y0, H):
            depth = y - y0
            col = "#83562f" if depth < 14 else ("#6e4728" if depth < 34 else "#5b3a23")
            if depth in (14, 34) and (x + y) % 2:
                col = "#83562f" if depth == 14 else "#6e4728"
            px[x, y] = hexc(col)
        if y0 < H:
            if y0 - 1 >= 0:
                px[x, y0 - 1] = hexc(INK)
            px[x, y0] = hexc("#7cc84a")
            if y0 + 1 < H and x % 3:
                px[x, y0 + 1] = hexc("#4c8a2c")
            if y0 + 2 < H and x % 5 == 0:
                px[x, y0 + 2] = hexc("#4c8a2c")
    # sunlit side, pebbles and the entrance
    for x in range(hx + 8, hx + 70):
        y0 = hill_y(x)
        for k in range(3, 9):
            if (x + k) % 3 == 0 and y0 + k < H:
                px[x, y0 + k] = hexc("#a0643c")
    rnd = random.Random(11)
    for _ in range(40):
        x = rnd.randrange(hx - 80, hx + 80)
        y = hill_y(x) + rnd.randrange(8, 50)
        if y < H - 14:
            px[x, y] = hexc(rnd.choice(["#b9b4a8", "#5b3a23", "#ecc477"]))
    d.ellipse([hx - 9, top + 22, hx + 9, top + 38], fill=hexc(INK))
    d.ellipse([hx - 7, top + 24, hx + 7, top + 38], fill=hexc("#2a1608"))
    d.ellipse([hx - 4, top + 30, hx + 4, top + 38], fill=hexc("#f2a93b"))
    d.ellipse([hx - 2, top + 33, hx + 2, top + 38], fill=hexc("#ffd27a"))

    # flag on the summit
    d.line([(hx, top - 22), (hx, top - 1)], fill=hexc(INK), width=3)
    d.line([(hx, top - 21), (hx, top - 1)], fill=hexc("#b5873e"))
    d.polygon([(hx + 2, top - 22), (hx + 17, top - 20), (hx + 13, top - 16), (hx + 17, top - 11), (hx + 2, top - 12)],
              fill=hexc("#e2412f"), outline=hexc(INK))

    # foreground grass
    ground = ridge(5, 164, 3, 22)
    for x in range(W):
        g = max(ground[x], 150)
        for y in range(g, H):
            px[x, y] = hexc("#3c6a26" if y - g < 5 else "#2f5a1c")
        px[x, g] = hexc("#7cc84a")
        if x % 4 == 0 and g - 1 >= 0:
            px[x, g - 1] = hexc("#7cc84a")
        if x % 7 == 3 and g - 2 >= 0:
            px[x, g - 2] = hexc("#9fe04a")
    rnd = random.Random(21)
    for _ in range(14):
        x = rnd.randrange(4, W - 4)
        y = max(ground[x], 150) - 2
        col = rnd.choice(["#fff1d6", "#ffd23a", "#ff6f7d"])
        px[x, y] = hexc(col)
        px[x, y + 1] = hexc("#4c8a2c")

    # a column of ants marching up the right slope, and two beetles arriving from the right
    ant = icons.SPR["ant_side"]
    def blit(rows, ox, oy, flip=False):
        for j, row in enumerate(rows):
            row = row[::-1] if flip else row
            for i, ch in enumerate(row):
                if ch != "." and 0 <= ox + i < W and 0 <= oy + j < H:
                    px[ox + i, oy + j] = icons.rgba(ch)
    for x in (150, 170, 190):
        blit(ant, x, hill_y(x + 7) - 8, flip=True)
    bug = ["".join(c * 2 for c in row) for row in icons.SPR["bug_side"] for _ in (0, 1)]
    for x in (284, 302):
        blit(bug, x, max(ground[x + 9], 150) - 11, flip=True)
    return im


def nine(img, b, w, h):
    sw, sh = img.size
    out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    xs = [(0, b, 0, b), (b, sw - b, b, w - b), (sw - b, sw, w - b, w)]
    ys = [(0, b, 0, b), (b, sh - b, b, h - b), (sh - b, sh, h - b, h)]
    for sx0, sx1, dx0, dx1 in xs:
        for sy0, sy1, dy0, dy1 in ys:
            out.paste(img.crop((sx0, sy0, sx1, sy1)).resize((dx1 - dx0, dy1 - dy0), Image.NEAREST), (dx0, dy0))
    return out


def wordmark():
    """The logo's lettering and flag without its own mound, since the backdrop supplies the hill."""
    logo = load("logo", SANDBOX / "Icons" / "Source~" / "build_logo.py")
    logo.W, logo.H = 168, 76
    im = Image.new("RGBA", (168, 76), (0, 0, 0, 0))
    small = logo.word_mask("HOLD THE", 2, 2)
    logo.paint_word(im, small, (168 - len(small[0])) // 2, 4, ["#ffffff", "#fff1d6", "#ecc477"], INK, 1, (1, 2, "#3a2416"))
    big = logo.word_mask("HILL", 5, 5)
    bx, by = (168 - len(big[0])) // 2, 34
    logo.paint_word(im, big, bx, by, ["#fff6c8", "#ffd88a", "#f2a93b", "#e07a2e", "#c9562a"], INK, 2,
                    (2, 3, "#2a1608"), hi="#ffffff", lo="#8a3414")
    return im


def main():
    icons = load("icons", SANDBOX / "Icons" / "Source~" / "build_pixel_icons.py")
    fonts = load("fonts", SANDBOX / "Fonts" / "Source~" / "build_pixel_fonts.py")
    bg = background(icons)
    bg.save(HERE / "TitleBackground.png")

    mock = bg.copy()
    mock.alpha_composite(wordmark(), (W - 168 - 8, 4))

    hud = next(s for s in fonts.STYLES if s["name"] == "Hud")
    button = Image.open(SANDBOX / "Ui" / "Button.png").convert("RGBA")
    hover = Image.open(SANDBOX / "Ui" / "ButtonHover.png").convert("RGBA")
    for i, label in enumerate(["PLAY", "SETTINGS", "CONTROLS", "QUIT"]):
        g = {c: fonts.bake(c, hud) for c in set(label)}
        text = fonts.render(label, g, max(im.size[1] for im, _ in g.values()))
        bw, bh = 84, 18
        x, y = W - bw - 50, 82 + i * 20
        mock.alpha_composite(nine(hover if i == 0 else button, 5, bw, bh), (x, y))
        mock.alpha_composite(text, (x + (bw - text.width) // 2, y + (bh - text.height) // 2))
    mock.resize((W * 4, H * 4), Image.NEAREST).save(HERE / "TitleMockup.png")
    print("wrote TitleBackground.png and TitleMockup.png")


if __name__ == "__main__":
    main()
