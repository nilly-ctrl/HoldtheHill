"""Hold the Hill 32x32 pixel-art icon generator.

Creatures are hand-placed pixel sprites (the ASCII grids below). Props, badges and
UI glyphs are drawn straight onto the 32x32 grid with PIL, which never anti-aliases,
and then get an automatic 1 px outline.

Writes:
  ../PNG/<Name>Icon.png          32x32 in-game icons (import with Point filter)
  ../AppIcon/AppIcon_<n>.png     app icon at platform sizes, plus AppIcon.ico
  ./IconSheet.png                8x preview sheet for review

    python build_pixel_icons.py
"""
import math
import os

from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
PNG_DIR = os.path.normpath(os.path.join(HERE, "..", "PNG"))
APP_DIR = os.path.normpath(os.path.join(HERE, "..", "AppIcon"))
N = 32

# ---------------------------------------------------------------- palette
PAL = {
    "k": "#1b110b",  # outline / ink
    "a": "#4a2c1c", "A": "#a0643c",  # ant body, ant highlight
    "q": "#5a2414", "Q": "#c0603a",  # red-brown (soldier, stag)
    "N": "#1f2f4a",                  # frost ant body
    "c": "#fff1d6", "w": "#ffffff",
    "y": "#ffd23a", "Y": "#c28a10", "u": "#ffb03a",
    "o": "#ff8a3d", "r": "#e2412f", "R": "#8e2124",
    "g": "#9fe04a", "G": "#4c8a2c", "j": "#2f5a1c",
    "b": "#9fe7ff", "B": "#5aa9d8", "n": "#2d6a96",
    "p": "#e6d4ff", "P": "#9a6cf0",
    "s": "#c4bfb2", "S": "#6d675c",
    "t": "#ecc477", "T": "#b5873e",
    "d": "#83562f", "D": "#5b3a23", "e": "#3a2416",
    "m": "#7cc84a", "M": "#3f7a24",
    "i": "#2f8f74", "I": "#a8f0d0",
    "h": "#3b4458", "H": "#b8c6dc",
    "f": "#ff6f7d", "v": "#2a1c30",
}


def rgba(c, alpha=255):
    c = PAL.get(c, c).lstrip("#")
    return (int(c[0:2], 16), int(c[2:4], 16), int(c[4:6], 16), alpha)


# ---------------------------------------------------------------- sprites
# '.' is transparent. Sprites marked "pre-outlined" already carry their k outline.
SPR = {
    # pre-outlined, 15x23, top-down ant facing up
    "ant": [
        ".k...........k.",
        "..k.........k..",
        "...k.k...k.k...",
        "....kk...kk....",
        "....kkkkkkk....",
        "...kaAaaaaak...",
        "...kAaaaaaak...",
        "...kaaaaaaak...",
        "....kaaaaak....",
        ".kk..kkakk..kk.",
        "...kk.kak.kk...",
        ".....kaAak.....",
        ".kkkkkaaakkkkk.",
        ".....kaaak.....",
        "...kk.kkk.kk...",
        ".kk...kak...kk.",
        ".....kaaak.....",
        "....kaAaaak....",
        "...kaAaaaaak...",
        "...kaaaaaaak...",
        "...kaaaaaaak...",
        "....kaaaaak....",
        ".....kkkkk.....",
    ],
    # soldier: wide head and sickle mandibles
    # soldier: big head, wide pincer jaws (12 rows; body continues from ant row 10)
    "soldier_head": [
        ".....kk.kk.....",
        "....kQk.kQk....",
        "...kQk...kQk...",
        "..kQk.....kQk..",
        "..kQk.....kQk..",
        "k.kQQk...kQQk.k",
        ".kkkkkkkkkkkkk.",
        ".kaAaaaaaaaaak.",
        ".kAacaaaaacaak.",
        ".kaaaaaaaaaaak.",
        "..kaaaaaaaaak..",
        ".kk.kkkakkk.kk.",
    ],
    # knockback "kicker": stocky head with short jaws
    "brute_head": [
        ".k.kk.....kk.k.",
        "..kQQk...kQQk..",
        "..kkQQk.kQQkk..",
        "..kkkkkkkkkkk..",
        "..kaAaaaaaaak..",
        "..kAaaaaaaaak..",
        "..kaaaaaaaaak..",
        "..kaaaaaaaaak..",
        "...kaaaaaaak...",
        ".kk.kkkakkk.kk.",
    ],
    # major: huge plug head
    "major_head": [
        "...k.......k...",
        "....k.....k....",
        "..kkkkkkkkkkk..",
        ".kaAaaaaaaaaak.",
        ".kAaaaaaaaaaak.",
        ".kaaaaaaaaaaak.",
        ".kaaaaaaaaaaak.",
        ".kaaaaaaaaaaak.",
        "..kaaaaaaaaak..",
        ".kk.kkkakkk.kk.",
    ],
    # pre-outlined, 7x9 tiny ant
    "ant_small": [
        ".k...k.",
        "..kak..",
        "k.kak.k",
        ".kkakk.",
        "k.kak.k",
        "..kak..",
        ".kaaak.",
        ".kaAak.",
        "..kkk..",
    ],
    # pre-outlined, 14x8 side-view ant facing right
    "ant_side": [
        "...........k..",
        "..........k...",
        ".kkk.....kkk..",
        "kaAak.kkkaAck.",
        "kaaakkaaakaaak",
        "kaaak.kaakkkk.",
        ".kkk.k.kk.k...",
        "....k..k...k..",
    ],
    # pre-outlined, 13x15 top-down beetle; 1 shell, 2 highlight, 3 shade
    "beetle": [
        "..k.......k..",
        "...k.kkk.k...",
        "....k111k....",
        "k..kkkkkkk..k",
        ".kk1211111kk.",
        "...k11111k...",
        "k.kkkkkkkkk.k",
        ".k1211k1111k.",
        "kk1211k1113kk",
        ".k1211k1113k.",
        ".k1211k1113k.",
        "kk1111k1133kk",
        "..k111k133k..",
        "...k11k13k...",
        "....kkkkk....",
    ],
    "horn": [
        "......k......",
        ".....k2k.....",
        "....k111k....",
    ],
    "stag_jaws": [
        ".kk.......kk.",
        "k11k.....k11k",
        ".k11k...k11k.",
        "..k11k.k11k..",
    ],
    # pre-outlined, 17x17 wasp
    "wasp": [
        ".....k.....k.....",
        "......k...k......",
        ".....kyyyyyk.....",
        ".....kkyyykk.....",
        ".kkk..kyyyk..kkk.",
        "kwwbkkkkkkkkbwwwk",
        "kwwwbkkkkkkbwwwwk",
        ".kwwwwkkkkkwwwwk.",
        "..kkkkkyyykkkkk..",
        "......kyyyk......",
        ".....kkkkkkk.....",
        ".....kyyyyyk.....",
        ".....kkkkkkk.....",
        "......kyyyk......",
        "......kkkkk......",
        ".......kyk.......",
        "........k........",
    ],
    # pre-outlined tiny bugs for targeting icons; 1 shell 2 hi 3 shade
    "bug5": [
        ".k.k.",
        "kk1kk",
        "k121k",
        "k111k",
        "k131k",
        ".kkk.",
    ],
    "bug7": [
        ".k...k.",
        "..kkk..",
        "kk111kk",
        ".k121k.",
        "kk121kk",
        ".k111k.",
        "kk131kk",
        "..kkk..",
    ],
    # raw (get outlined when placed)
    "heart": [
        ".rr...rr.",
        "rwrr.rrrr",
        "rrrrrrrrr",
        "rrrrrrrrr",
        ".rrrrrrr.",
        "..rrrrr..",
        "...rrr...",
        "....r....",
    ],
    "crown": [
        "y...y...y",
        "yy.yyy.yy",
        "yyyyryyyy",
        "yyyyyyyyy",
        "YYYYYYYYY",
    ],
    "crown_s": [
        "y.y.y",
        "yyyyy",
        "YYYYY",
    ],
    "spark": [
        "..y..",
        ".yyy.",
        "yywyy",
        ".yyy.",
        "..y..",
    ],
    "spark_w": [
        ".w.",
        "www",
        ".w.",
    ],
    "flake": [
        "w.w.w",
        ".www.",
        "wwbww",
        ".www.",
        "w.w.w",
    ],
    # pre-outlined 9x5 side-view ant facing right (Swarm Nest)
    "ant_tiny": [
        "......k.k",
        ".kk...kk.",
        "kAAkAkAAk",
        ".kkk.kkk.",
        "..k.k.k..",
    ],
    # pre-outlined 9x6 side-view beetle walking right (targeting icons)
    "bug_side": [
        "..kkkk...",
        ".krwrrk..",
        "krrrrrrkk",
        "kRrrrrkRk",
        ".kkkkkkk.",
        ".k.k.k...",
    ],
    # pre-outlined 11x10 ant head, the "portrait" in the corner of emblem-style tower icons
    "bust": [
        ".k.......k.",
        "..k.....k..",
        "...kkkkk...",
        "..kaAaaak..",
        ".kaAaaaaak.",
        ".kacaaacak.",
        ".kaaaaaaak.",
        "..kaaaaak..",
        "..kkakakk..",
        "...k...k...",
    ],
    "two": ["uuu", "..u", "uuu", "u..", "uuu"],
    "x": ["u.u", ".u.", "u.u"],
}

for _n, _rows in SPR.items():
    if len({len(r) for r in _rows}) != 1:
        raise SystemExit(f"sprite {_n} has uneven rows: {[len(r) for r in _rows]}")


def body(head="ant", **remap):
    """An ant sprite: optional head swap, then character remap (e.g. a='q')."""
    rows = SPR["ant"]
    if head != "ant":
        rows = SPR[head] + rows[10:]
    return [r.translate(str.maketrans(remap)) if remap else r for r in rows]


def recolor(rows, **remap):
    return [r.translate(str.maketrans(remap)) for r in rows]


def beetle(shell="h", hi="H", shade="k", kind="plain"):
    rows = SPR["beetle"]
    if kind == "horn":
        rows = SPR["horn"] + rows[3:]
    elif kind == "stag":
        rows = SPR["stag_jaws"] + rows[2:]
    return recolor(rows, **{"1": shell, "2": hi, "3": shade})


# ---------------------------------------------------------------- drawing
class Layer:
    def __init__(self):
        self.im = Image.new("RGBA", (N, N), (0, 0, 0, 0))
        self.d = ImageDraw.Draw(self.im)

    def px(self, x, y, c):
        if 0 <= x < N and 0 <= y < N:
            self.im.putpixel((int(x), int(y)), rgba(c))
        return self

    def pxs(self, pts, c):
        for x, y in pts:
            self.px(x, y, c)
        return self

    def ell(self, x0, y0, x1, y1, c=None, ol=None, w=1):
        self.d.ellipse([x0, y0, x1, y1], fill=rgba(c) if c else None, outline=rgba(ol) if ol else None, width=w)
        return self

    def rect(self, x0, y0, x1, y1, c):
        self.d.rectangle([x0, y0, x1, y1], fill=rgba(c))
        return self

    def poly(self, pts, c):
        self.d.polygon(pts, fill=rgba(c))
        return self

    def line(self, pts, c, w=1):
        self.d.line(pts, fill=rgba(c), width=w)
        return self

    def arc(self, box, a0, a1, c, w=1):
        self.d.arc(box, a0, a1, fill=rgba(c), width=w)
        return self

    def clear(self, x0, y0, x1, y1, shape="ell"):
        (self.d.ellipse if shape == "ell" else self.d.rectangle)([x0, y0, x1, y1], fill=(0, 0, 0, 0))
        return self

    def spr(self, rows, x, y, flip=False):
        if isinstance(rows, str):
            rows = SPR[rows]
        for j, row in enumerate(rows):
            if flip:
                row = row[::-1]
            for i, ch in enumerate(row):
                if ch != ".":
                    self.px(x + i, y + j, ch)
        return self

    def dots(self, pts, c, on=2, off=2):
        """Dashed path through integer points."""
        for idx, p in enumerate(pts):
            if idx % (on + off) < on:
                self.px(p[0], p[1], c)
        return self


def bez(p0, p1, p2, steps=80):
    """Quadratic bezier sampled to unique integer pixels."""
    out = []
    for i in range(steps + 1):
        t = i / steps
        x = (1 - t) ** 2 * p0[0] + 2 * (1 - t) * t * p1[0] + t * t * p2[0]
        y = (1 - t) ** 2 * p0[1] + 2 * (1 - t) * t * p1[1] + t * t * p2[1]
        p = (round(x), round(y))
        if not out or out[-1] != p:
            out.append(p)
    return out


def seg(pts):
    """Polyline sampled to integer pixels."""
    out = []
    for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
        n = max(abs(x1 - x0), abs(y1 - y0))
        for i in range(n + 1):
            p = (round(x0 + (x1 - x0) * i / n), round(y0 + (y1 - y0) * i / n))
            if not out or out[-1] != p:
                out.append(p)
    return out


def outlined(im, c="k"):
    src = im.load()
    out = im.copy()
    o = out.load()
    col = rgba(c)
    for y in range(N):
        for x in range(N):
            if src[x, y][3]:
                continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, ny = x + dx, y + dy
                if 0 <= nx < N and 0 <= ny < N and src[nx, ny][3]:
                    o[x, y] = col
                    break
    return out


class Icon:
    def __init__(self, base=None, shadow=True):
        self.im = base if base is not None else Image.new("RGBA", (N, N), (0, 0, 0, 0))
        self.shadow = shadow

    def add(self, layer, outline=True):
        g = outlined(layer.im) if outline else layer.im
        if self.shadow:
            sh = Image.new("RGBA", (N, N), (0, 0, 0, 0))
            gp, sp = g.load(), sh.load()
            for y in range(N - 1):
                for x in range(N):
                    if gp[x, y][3] and not gp[x, y + 1][3]:
                        sp[x, y + 1] = (0, 0, 0, 80)
            self.im.alpha_composite(sh)
        self.im.alpha_composite(g)
        return self

    def shapes(self):
        lay = Layer()
        self._pending = lay
        return lay


def L():
    return Layer()


# ---------------------------------------------------------------- badges
CATS = {
    "tower":    ("#ffe0a0", "#f2a93b", "#c97a26", "#4a2a12"),
    "enemy":    ("#f2806b", "#c8413a", "#8e2124", "#3a0c0f"),
    "ability":  ("#d8c4ff", "#9a7ae8", "#5a3aa8", "#21134d"),
    "resource": ("#c8eb8e", "#7fb24a", "#4c8a2c", "#1d3a11"),
}


def _fill_gradient(mask, light, mid, dark, rim):
    im = Image.new("RGBA", (N, N), (0, 0, 0, 0))
    p = im.load()
    for y in range(N):
        for x in range(N):
            if not mask(x, y):
                continue
            edge = any(not (0 <= x + dx < N and 0 <= y + dy < N and mask(x + dx, y + dy))
                       for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))
            if edge:
                p[x, y] = rgba(rim)
                continue
            # 3 bands with a checker dither at each seam
            if y < 10 or (y == 10 and (x + y) % 2):
                c = light
            elif y < 21 or (y == 21 and (x + y) % 2):
                c = mid
            else:
                c = dark
            p[x, y] = rgba(c)
    # inner rim highlight (top) and shade (bottom)
    for y in range(N):
        for x in range(N):
            if p[x, y][3] and p[x, y] != rgba(rim):
                up = y > 0 and p[x, y - 1] == rgba(rim)
                down = y < N - 1 and p[x, y + 1] == rgba(rim)
                if up:
                    p[x, y] = (255, 255, 255, 255) if 4 < x < 27 else rgba(light)
                elif down:
                    p[x, y] = rgba(dark)
    return im


CUT = [3, 2, 1]  # rounded-corner pixels removed on rows 0..2


def square_mask(x, y):
    for i, c in enumerate(CUT):
        if (y == i or y == N - 1 - i) and (x < c or x > N - 1 - c):
            return False
    return True


def circle_mask(x, y):
    return (x - 15.5) ** 2 + (y - 15.5) ** 2 <= 15.7 ** 2


def badge(cat):
    return _fill_gradient(square_mask, *CATS[cat])


def round_badge(light, mid, dark):
    return _fill_gradient(circle_mask, light, mid, dark, "#1b110b")


# ================================================================ icons
ICONS = {}  # name -> (group, label, Image)


def reg(group, name, label, im):
    ICONS[name] = (group, label, im)


def ant_at(ic, x, y, rows=None):
    ic.add(L().spr(rows or SPR["ant"], x, y), outline=False)


# ---------------------------------------------------------------- towers
T = "Towers"


def bust(ic, x=2, y=20, **remap):
    """The ant portrait in the lower-left corner of emblem-style tower icons."""
    ic.add(L().spr(recolor(SPR["bust"], **remap) if remap else SPR["bust"], x, y), outline=False)


def tower_linear():
    ic = Icon(badge("tower"))
    ic.add(L().line([(12, 20), (17, 15)], "c").line([(9, 17), (12, 14)], "c").line([(15, 23), (19, 19)], "c"), outline=False)
    ic.add(L().ell(16, 3, 28, 15, "g").ell(20, 8, 27, 14, "G").ell(17, 4, 26, 13, "g")
           .pxs([(19, 6), (20, 6), (19, 7), (18, 8)], "w"))
    ic.add(L().ell(12, 14, 18, 20, "g").pxs([(14, 16), (14, 15)], "w"))
    bust(ic)
    return ic.im


def tower_homing():
    ic = Icon(badge("tower"))
    ic.add(L().dots(bez((9, 20), (10, 6), (17, 11)), "c", 2, 1), outline=False)
    ret = L().ell(10, 2, 28, 20, ol="r", w=2)
    ret.line([(19, 0), (19, 5)], "r", 2).line([(19, 17), (19, 22)], "r", 2)
    ret.line([(8, 11), (13, 11)], "r", 2).line([(25, 11), (30, 11)], "r", 2)
    ic.add(ret)
    ic.add(L().ell(17, 9, 21, 13, "y").px(18, 10, "w"))
    bust(ic)
    return ic.im


def tower_mortar():
    ic = Icon(badge("tower"))
    ic.add(L().dots(bez((8, 20), (10, 0), (16, 9)), "c", 2, 1), outline=False)
    ic.add(L().ell(13, 7, 27, 21, "D").ell(15, 9, 26, 20, "e").ell(14, 8, 24, 18, "D")
           .pxs([(17, 10), (18, 10), (16, 11), (16, 12)], "t").rect(19, 5, 22, 7, "S"))
    ic.add(L().line([(21, 5), (23, 3), (26, 3)], "k"), outline=False)
    ic.add(L().spr("spark", 24, 0))
    bust(ic)
    return ic.im


def tower_ricochet():
    ic = Icon(badge("tower"))
    ic.add(L().line([(9, 19), (14, 5), (20, 17), (26, 6)], "c", 2))
    lay = L()
    for x, y in [(14, 5), (20, 17)]:
        lay.pxs([(x - 3, y), (x + 3, y), (x, y - 3), (x, y + 3)], "w")
    ic.add(lay, outline=False)
    ic.add(L().ell(22, 2, 29, 9, "s").ell(25, 5, 28, 8, "S").pxs([(24, 4), (25, 4), (24, 5)], "w"))
    bust(ic)
    return ic.im


def tower_chain():
    ic = Icon(badge("tower"))
    ic.add(L().poly([(22, 2), (12, 13), (17, 13), (13, 23), (25, 10), (20, 10), (25, 2)], "y")
           .line([(22, 4), (16, 11)], "w"))
    ic.add(L().line([(23, 14), (27, 17), (25, 19), (29, 23)], "y", 1))
    ic.add(L().spr("spark_w", 27, 22).spr("spark_w", 9, 3))
    bust(ic)
    return ic.im


def tower_beam():
    ic = Icon(badge("tower"))
    ic.add(L().poly([(19, 11), (28, 1), (30, 4), (22, 14)], "p"), outline=False)
    ic.add(L().line([(21, 11), (29, 2)], "w"), outline=False)
    ic.add(L().ell(9, 7, 23, 21, "B").ell(10, 8, 21, 19, "b").ell(15, 13, 21, 19, "B").ell(11, 9, 19, 17, "b")
           .pxs([(12, 11), (13, 10), (14, 10), (12, 12)], "w"))
    ic.add(L().spr("spark", 25, 0))
    bust(ic)
    return ic.im


def rot90(rows):
    """Rotate a sprite 90 degrees clockwise (an ant facing up then faces right)."""
    return ["".join(rows[len(rows) - 1 - j][i] for j in range(len(rows))) for i in range(len(rows[0]))]


def tower_orbit():
    ic = Icon(badge("tower"))
    ring = (3, 11, 28, 29)
    nest_ant = recolor(SPR["ant_small"], a="A", A="t")
    ic.add(L().arc(ring, 180, 360, "c"), outline=False)            # back half of the orbit
    mound = L()
    mound.d.chord([10, 8, 21, 32], 180, 360, fill=rgba("d"))
    mound.ell(13, 14, 18, 20, "e").rect(13, 18, 18, 20, "e")
    mound.pxs([(12, 16), (12, 15), (13, 13), (14, 12)], "t")
    ic.add(mound)
    ic.add(L().arc(ring, 0, 180, "c"), outline=False)              # front half
    # three ants marching clockwise: up the left side, down the right, left along the front
    ic.add(L().spr(nest_ant, 1, 14), outline=False)
    ic.add(L().spr(nest_ant[::-1], 24, 12), outline=False)
    ic.add(L().spr(rot90(nest_ant), 11, 24, flip=True), outline=False)
    return ic.im


def tower_worker():
    ic = Icon(badge("tower"))
    ant_at(ic, 8, 8)
    ic.add(L().ell(9, 2, 21, 9, "t").line([(11, 6), (15, 5), (19, 5)], "T").px(11, 4, "w").px(12, 3, "w"))
    return ic.im


def tower_soldier():
    ic = Icon(badge("tower"))
    ant_at(ic, 8, 3, body("soldier_head", a="q", A="Q", c="Q"))
    return ic.im


def tower_major():
    ic = Icon(badge("tower"))
    lay = L()
    for x, y in [(3, 3), (10, 2), (17, 2), (24, 3)]:
        lay.rect(x, y, x + 5, y + 3, "s").px(x, y, "w")
    ic.add(lay)
    ant_at(ic, 8, 8, body("major_head"))
    return ic.im


def tower_nurse():
    ic = Icon(badge("tower"))
    big = Layer()
    for j, row in enumerate(recolor(SPR["heart"], r="f")):       # heart at 2x
        for i, ch in enumerate(row):
            if ch != ".":
                for dx in (0, 1):
                    for dy in (0, 1):
                        big.px(11 + i * 2 + dx, 3 + j * 2 + dy, ch)
    ic.add(big)
    ic.add(L().rect(19, 6, 20, 13, "w").rect(16, 9, 23, 10, "w"), outline=False)
    ic.add(L().spr("spark_w", 27, 18).spr("spark_w", 6, 4))
    bust(ic)
    return ic.im


def tower_frost_aura():
    ic = Icon(badge("tower"))
    ic.add(L().ell(6, 0, 30, 24, ol="b"), outline=False)
    flake = L()
    cx, cy = 18, 12
    for a in range(0, 180, 60):
        dx, dy = math.cos(math.radians(a + 90)) * 9, math.sin(math.radians(a + 90)) * 9
        flake.line([(cx - dx, cy - dy), (cx + dx, cy + dy)], "w", 2)
    for a in range(0, 360, 60):
        ux, uy = math.cos(math.radians(a + 90)), math.sin(math.radians(a + 90))
        bx, by = cx + ux * 6, cy + uy * 6
        for sd in (-1, 1):
            vx, vy = math.cos(math.radians(a + 90 + sd * 55)), math.sin(math.radians(a + 90 + sd * 55))
            flake.line([(bx, by), (bx + vx * 3, by + vy * 3)], "w")
    ic.add(flake)
    ic.add(L().pxs([(18, 12), (17, 12), (18, 11), (17, 11)], "b"), outline=False)
    bust(ic, a="N", A="b")
    return ic.im


def tower_knockback():
    ic = Icon(badge("tower"))
    ic.add(L().arc((0, 4, 16, 22), 300, 60, "w", 2).arc((5, 2, 22, 24), 300, 60, "w", 2).arc((11, 0, 28, 26), 305, 55, "c", 2))
    ic.add(L().spr(recolor(SPR["bug5"], **{"1": "h", "2": "H", "3": "h"}), 25, 2), outline=False)
    ic.add(L().pxs([(24, 9), (25, 10), (27, 9)], "c"), outline=False)
    bust(ic, x=1, y=11)
    return ic.im


def tower_mine_layer():
    ic = Icon(badge("tower"))
    ic.add(L().rect(11, 21, 29, 23, "D"))
    mine = L()
    mine.d.chord([12, 7, 28, 35], 180, 360, fill=rgba("S"))
    mine.rect(12, 20, 28, 21, "S")
    mine.pxs([(15, 13), (16, 12), (17, 11), (15, 14)], "s").rect(18, 4, 22, 7, "r").pxs([(19, 5)], "f")
    mine.pxs([(11, 16), (10, 15), (29, 16), (30, 15), (14, 9), (13, 8), (26, 9), (27, 8)], "k")
    ic.add(mine)
    ic.add(L().spr("spark_w", 23, 1))
    bust(ic)
    return ic.im


for fn, label in [(tower_linear, "Spitter — Linear"), (tower_homing, "Seeker — Homing"),
                  (tower_mortar, "Bombardier — Mortar"), (tower_ricochet, "Slinger — Ricochet"),
                  (tower_chain, "Storm Ant — Chain"), (tower_beam, "Dewdrop Lens — Beam"),
                  (tower_orbit, "Swarm Nest — Orbit"), (tower_worker, "Worker"),
                  (tower_soldier, "Soldier"), (tower_major, "Major — Wall"), (tower_nurse, "Nurse"),
                  (tower_frost_aura, "Frost Ant — Frost Aura"), (tower_knockback, "Kicker — Knockback"),
                  (tower_mine_layer, "Sapper — Mine Layer")]:
    reg(T, fn.__name__, label, fn())

# ---------------------------------------------------------------- enemies
E = "Enemies"


def enemy_scarab():
    ic = Icon(badge("enemy"))
    ic.add(L().line([(3, 21), (8, 21)], "c").line([(2, 25), (7, 25)], "c").line([(5, 17), (9, 17)], "c"), outline=False)
    ic.add(L().spr(beetle("i", "I", "k"), 11, 7), outline=False)
    ic.add(L().spr(recolor(SPR["bug5"], **{"1": "i", "2": "I", "3": "i"}), 4, 4), outline=False)
    return ic.im


def enemy_beetle():
    ic = Icon(badge("enemy"))
    ic.add(L().spr(beetle("h", "H", "k", "horn"), 9, 8), outline=False)
    ic.add(L().pxs([(4, 26), (5, 25), (6, 26), (24, 26), (25, 25), (26, 26)], "c"), outline=False)
    return ic.im


def enemy_stag_beetle():
    ic = Icon(badge("enemy"))
    ic.add(L().spr(beetle("q", "Q", "e", "stag"), 9, 7), outline=False)
    return ic.im


def enemy_wasp_drone():
    ic = Icon(badge("enemy"))
    ic.add(L().spr("wasp", 7, 8), outline=False)
    return ic.im


def enemy_centipede():
    ic = Icon(badge("enemy"))
    pts = [(6, 26), (8, 23), (10, 20), (13, 18), (16, 17), (19, 15), (21, 12), (22, 9), (23, 6)]
    legs = L()
    for (x, y), (nx, ny) in zip(pts, pts[1:]):
        ang = math.atan2(ny - y, nx - x) + math.pi / 2
        for sd in (-1, 1):
            legs.line([(x + round(sd * 3 * math.cos(ang)), y + round(sd * 3 * math.sin(ang))),
                       (x + round(sd * 4.6 * math.cos(ang)), y + round(sd * 4.6 * math.sin(ang)))], "o")
    lay = L()
    for x, y in pts[:-1]:
        lay.ell(x - 2, y - 2, x + 2, y + 2, "q").px(x - 1, y - 1, "Q")
    hx, hy = pts[-1]
    lay.ell(hx - 2, hy - 2, hx + 2, hy + 2, "q").px(hx - 1, hy, "c").px(hx + 1, hy, "c")
    lay.pxs([(hx - 2, hy - 3), (hx - 3, hy - 4), (hx + 2, hy - 3), (hx + 3, hy - 4)], "k")
    ic.add(legs)
    ic.add(lay)
    return ic.im


def enemy_mantis():
    ic = Icon(badge("enemy"))
    lay = L()
    lay.poly([(14, 15), (19, 13), (25, 17), (28, 22), (26, 24), (20, 20), (14, 18)], "m")   # abdomen + wings
    lay.line([(17, 16), (24, 19)], "M")
    lay.line([(15, 16), (9, 9)], "m", 2)                                                 # thorax
    lay.line([(13, 19), (14, 25), (13, 28)], "M").line([(19, 19), (21, 25), (23, 28)], "M")  # legs
    lay.line([(11, 12), (6, 11), (4, 15)], "m", 2)                                       # raptorial arm
    lay.poly([(5, 4), (12, 4), (9, 9)], "m")                                             # head
    lay.px(5, 5, "r").px(6, 5, "r").px(11, 5, "r").px(10, 5, "r")
    ic.add(lay)
    ic.add(L().pxs([(6, 3), (5, 2), (4, 1), (11, 3), (12, 2), (13, 1)], "k").pxs([(4, 13), (5, 14)], "k"), outline=False)
    return ic.im


def enemy_queen_wasp():
    ic = Icon(badge("enemy"))
    ic.add(L().spr("wasp", 7, 10).pxs([(12, 13), (18, 13)], "r"), outline=False)
    ic.add(L().spr("crown", 11, 4))
    return ic.im


for fn, label in [(enemy_scarab, "Scarab — fast swarm"), (enemy_beetle, "Beetle — armoured charger"),
                  (enemy_stag_beetle, "Stag Beetle — elite"), (enemy_wasp_drone, "Wasp Drone — flyer"),
                  (enemy_centipede, "Centipede — digger"), (enemy_mantis, "Mantis — boss"),
                  (enemy_queen_wasp, "Queen Wasp — boss")]:
    reg(E, fn.__name__, label, fn())

# ---------------------------------------------------------------- abilities
A = "Abilities"


def ability_rally():
    ic = Icon(badge("ability"))
    ic.add(L().line([(8, 3), (8, 28)], "T"))
    ic.add(L().poly([(9, 4), (24, 5), (20, 9), (24, 13), (9, 12)], "r").px(10, 5, "f"))
    ic.add(L().pxs([(13, 7), (14, 8), (15, 7), (14, 9), (16, 8), (12, 9)], "c"), outline=False)
    ic.add(L().line([(14, 22), (18, 18), (22, 22)], "y", 2).line([(14, 27), (18, 23), (22, 27)], "y", 2))
    return ic.im


def ability_shove():
    ic = Icon(badge("ability"))
    pts = []
    for i in range(16):
        ang = math.pi * 2 * i / 16 - math.pi / 2
        r = 12 if i % 2 == 0 else 6
        pts.append((15.5 + r * math.cos(ang), 15.5 + r * math.sin(ang)))
    ic.add(L().poly(pts, "y"))
    ic.add(L().line([(7, 16), (21, 16)], "w", 2).poly([(19, 11), (25, 16), (19, 21)], "w"))
    ic.add(L().spr(recolor(SPR["bug5"], **{"1": "h", "2": "H", "3": "h"}), 25, 22), outline=False)
    return ic.im


def ability_dig():
    ic = Icon(badge("ability"))
    mound = L()
    mound.d.chord([2, 20, 29, 40], 180, 360, fill=rgba("d"))
    mound.rect(3, 29, 28, 29, "d").pxs([(8, 24), (9, 23), (10, 23)], "t")
    ic.add(mound)
    ic.add(L().line([(24, 3), (16, 17)], "T", 2).line([(22, 3), (26, 3)], "T", 2))
    ic.add(L().poly([(13, 15), (19, 18), (16, 24), (12, 23), (11, 20)], "s").px(13, 17, "w"))
    ic.add(L().ell(5, 9, 7, 11, "D").ell(4, 4, 5, 5, "D").ell(9, 5, 10, 6, "D"))
    return ic.im


def ability_repair():
    ic = Icon(badge("ability"))
    lay = L().line([(7, 25), (17, 15)], "s", 3)
    lay.ell(14, 6, 25, 17, "s").clear(19, 4, 26, 11).px(16, 9, "w")
    ic.add(lay)
    ic.add(L().line([(7, 4), (7, 12)], "g", 3).line([(3, 8), (11, 8)], "g", 3))
    return ic.im


for fn, label in [(ability_rally, "Rally"), (ability_shove, "Shove"), (ability_dig, "Dig"), (ability_repair, "Repair")]:
    reg(A, fn.__name__, label, fn())

# ---------------------------------------------------------------- resources / HUD
R = "Resources & HUD"


def rot_ellipse(cx, cy, rx, ry, deg, n=28):
    th = math.radians(deg)
    return [(cx + rx * math.cos(t) * math.cos(th) - ry * math.sin(t) * math.sin(th),
             cy + rx * math.cos(t) * math.sin(th) + ry * math.sin(t) * math.cos(th))
            for t in [2 * math.pi * i / n for i in range(n)]]


def res_food():
    ic = Icon(badge("resource"))
    ic.add(L().poly(rot_ellipse(13, 18, 9, 6, -25), "t").line([(7, 21), (13, 17), (19, 15)], "T").pxs([(9, 15), (10, 14), (11, 14)], "w"))
    ic.add(L().poly([(23, 5), (27, 11), (27, 14), (25, 16), (21, 16), (19, 14), (19, 11)], "b").pxs([(21, 12), (21, 13)], "w"))
    return ic.im


def res_queen_health():
    ic = Icon(badge("resource"))
    heart = [r.replace(".", ".") for r in SPR["heart"]]
    big = Layer()
    for j, row in enumerate(heart):          # heart at 2x
        for i, ch in enumerate(row):
            if ch != ".":
                for dx in (0, 1):
                    for dy in (0, 1):
                        big.px(7 + i * 2 + dx, 11 + j * 2 + dy, ch)
    ic.add(big)
    ic.add(L().spr("crown", 11, 4))
    return ic.im


def res_brood():
    ic = Icon(badge("resource"))
    for x0, y0 in [(4, 12), (18, 12), (11, 5)]:
        ic.add(L().ell(x0, y0, x0 + 9, y0 + 13, "c").ell(x0 + 2, y0 + 2, x0 + 3, y0 + 4, "w").line([(x0 + 3, y0 + 9), (x0 + 6, y0 + 9)], "t"))
    return ic.im


def res_wave():
    ic = Icon(badge("resource"))
    ic.add(L().line([(7, 3), (7, 28)], "T"))
    ic.add(L().poly([(8, 4), (26, 4), (22, 9), (26, 15), (8, 15)], "v"))
    ic.add(L().spr(recolor(SPR["bug5"], k="c", **{"1": "c", "2": "w", "3": "c"}), 12, 7), outline=False)
    return ic.im


def res_threat_radar():
    ic = Icon(badge("resource"))
    lay = L().ell(4, 4, 27, 27, "j")
    ic.add(lay)
    rings = L().ell(9, 9, 22, 22, ol="G").ell(13, 13, 18, 18, ol="G").line([(15, 4), (15, 27)], "G").line([(4, 15), (27, 15)], "G")
    rings.d.pieslice([4, 4, 27, 27], 270, 330, fill=rgba("g"))
    ic.add(rings, outline=False)
    ic.add(L().pxs([(20, 8), (21, 8), (20, 9), (21, 9), (9, 20), (10, 20), (9, 21), (10, 21), (22, 20)], "r"), outline=False)
    return ic.im


for fn, label in [(res_food, "Food"), (res_queen_health, "Queen Health"), (res_brood, "Brood"),
                  (res_wave, "Wave"), (res_threat_radar, "Threat Radar")]:
    reg(R, fn.__name__, label, fn())

# ---------------------------------------------------------------- damage & status
D = "Damage & Status"


def star(cx, cy, r_out, r_in, n=4, rot=-90):
    pts = []
    for i in range(n * 2):
        ang = math.radians(rot + 180 / n * i)
        r = r_out if i % 2 == 0 else r_in
        pts.append((cx + r * math.cos(ang), cy + r * math.sin(ang)))
    return pts


def dmg_physical():
    ic = Icon(round_badge("#9a948a", "#6d675c", "#3e3a34"))
    for dx in (-6, 0, 6):
        ic.add(L().poly([(20 + dx, 4), (23 + dx, 5), (12 + dx, 27), (10 + dx, 26)], "w")
               .line([(21 + dx, 6), (12 + dx, 24)], "s"))
    return ic.im


def dmg_magic():
    ic = Icon(round_badge("#d4b8ff", "#9a6cf0", "#4b2a96"))
    ic.add(L().poly(star(14, 17, 10, 3), "p"))
    ic.add(L().spr("spark_w", 22, 5).spr("spark_w", 6, 23))
    return ic.im


def dmg_true():
    ic = Icon(round_badge("#f4f6fa", "#c4c8d6", "#7a8197"))
    ic.add(L().poly([(15.5, 4), (25, 12), (15.5, 27), (6, 12)], "b").line([(6, 12), (25, 12)], "B")
           .line([(11, 12), (15.5, 4)], "B").line([(20, 12), (15.5, 4)], "B").line([(11, 12), (15.5, 26)], "B")
           .line([(20, 12), (15.5, 26)], "B").pxs([(10, 10), (11, 9)], "w"))
    return ic.im


def dmg_fire():
    ic = Icon(round_badge("#ffc27a", "#ff8a3d", "#b5381c"))
    ic.add(L().poly([(16, 27), (9, 24), (8, 17), (11, 12), (12, 15), (14, 9), (18, 4), (19, 11), (23, 15), (24, 21), (21, 26)], "o")
           .poly([(16, 26), (12, 23), (13, 18), (16, 15), (19, 19), (20, 23)], "y"))
    return ic.im


def dmg_poison():
    ic = Icon(round_badge("#c4ec8e", "#8ccf4f", "#2f6a1c"))
    ic.add(L().poly([(14, 4), (21, 14), (22, 19), (19, 24), (13, 25), (8, 22), (7, 17), (9, 12)], "g")
           .rect(10, 15, 12, 17, "k").rect(16, 15, 18, 17, "k").pxs([(11, 20), (13, 20), (15, 20), (17, 20)], "k"))
    ic.add(L().ell(23, 6, 25, 8, "g").px(25, 12, "g"))
    return ic.im


def dmg_lightning():
    ic = Icon(round_badge("#fff0a0", "#ffd23a", "#c28a10"))
    ic.add(L().poly([(18, 3), (8, 17), (14, 17), (11, 28), (23, 13), (17, 13), (21, 3)], "y").line([(17, 5), (12, 13)], "w"))
    return ic.im


def status_slow():
    ic = Icon(round_badge("#c0e6fa", "#7cbce6", "#2d6a96"), shadow=False)
    lay = L()
    for a in range(0, 360, 45):
        lay.line([(15.5, 15.5), (15.5 + 12 * math.cos(math.radians(a)), 15.5 + 12 * math.sin(math.radians(a)))], "w")
    for r in (4, 8, 12):
        lay.line([(15.5 + r * math.cos(math.radians(a)), 15.5 + r * math.sin(math.radians(a))) for a in range(0, 361, 45)], "w")
    ic.add(lay, outline=False)
    ic.add(L().ell(20, 7, 23, 10, "k").pxs([(19, 6), (24, 6), (19, 11), (24, 11), (18, 8), (25, 8)], "k"), outline=False)
    return ic.im


def status_stun():
    ic = Icon(round_badge("#fff0a0", "#f5d04a", "#a8751a"))
    ic.add(L().ell(5, 7, 26, 14, ol="w"), outline=False)
    ic.add(L().spr("spark", 3, 8).spr("spark", 13, 4).spr("spark", 23, 8))
    ic.add(L().spr(recolor(SPR["bug7"], **{"1": "h", "2": "H", "3": "h"}), 12, 18), outline=False)
    return ic.im


for fn, label in [(dmg_physical, "Physical"), (dmg_magic, "Magic"), (dmg_true, "True"), (dmg_fire, "Fire"),
                  (dmg_poison, "Poison"), (dmg_lightning, "Lightning"), (status_slow, "Slow (web)"), (status_stun, "Stun")]:
    reg(D, fn.__name__, label, fn())

# ---------------------------------------------------------------- targeting
TG = "Targeting"
BUG = SPR["bug_side"]


def big_bug():
    """bug_side at 2x."""
    return ["".join(ch * 2 for ch in row) for row in BUG for _ in (0, 1)]


def slate():
    return Icon(round_badge("#b4bfd0", "#8b98ad", "#2e3648"))


def lock_on(ic, x0, y0, x1, y1):
    """Yellow corner brackets around the chosen target."""
    lay = L()
    for (cx, cy, dx, dy) in [(x0, y0, 1, 1), (x1, y0, -1, 1), (x0, y1, 1, -1), (x1, y1, -1, -1)]:
        lay.pxs([(cx, cy), (cx + dx, cy), (cx, cy + dy)], "y")
    ic.add(lay)


def path_row(ic, pick):
    ic.add(L().line([(3, 23), (25, 23)], "c").poly([(24, 20), (28, 23), (24, 26)], "c"))
    xs = (2, 11, 20)
    for x in xs:
        ic.add(L().spr(BUG, x, 15), outline=False)
    x = xs[pick]
    lock_on(ic, x - 2, 12, x + 10, 22)
    ic.add(L().poly([(x + 2, 6), (x + 7, 6), (x + 4, 9)], "y"))


def target_first():
    ic = slate()
    path_row(ic, 2)
    return ic.im


def target_last():
    ic = slate()
    path_row(ic, 0)
    return ic.im


def target_closest():
    ic = slate()
    ic.add(L().ell(3, 3, 28, 28, ol="c"), outline=False)
    ic.add(L().rect(13, 12, 18, 17, "u").px(14, 13, "y"))
    for x, y in [(17, 4), (3, 20), (17, 20)]:
        ic.add(L().spr(BUG, x, y), outline=False)
    ic.add(L().dots(seg([(16, 18), (19, 20)]), "y", 1, 1), outline=False)
    lock_on(ic, 15, 18, 27, 27)
    return ic.im


def target_strongest():
    ic = slate()
    ic.add(L().line([(3, 26), (28, 26)], "c"), outline=False)
    ic.add(L().spr(BUG, 2, 20), outline=False)
    ic.add(L().spr(big_bug(), 12, 13), outline=False)
    lock_on(ic, 10, 11, 30, 26)
    ic.add(L().poly([(18, 7), (21, 3), (24, 7)], "y").rect(20, 7, 22, 9, "y"))
    return ic.im


def target_weakest():
    ic = slate()
    for x, frac in [(1, .9), (11, .2), (21, .6)]:
        ic.add(L().spr(BUG, x, 18), outline=False)
        bar = L().rect(x, 13, x + 8, 15, "k").rect(x + 1, 14, x + max(1, round(7 * frac)), 14, "r" if frac < .35 else "g")
        ic.add(bar, outline=False)
    lock_on(ic, 9, 11, 21, 25)
    ic.add(L().poly([(13, 5), (18, 5), (15, 8)], "y"))
    return ic.im


for fn, label in [(target_first, "First"), (target_last, "Last"), (target_closest, "Closest"),
                  (target_strongest, "Strongest"), (target_weakest, "Weakest")]:
    reg(TG, fn.__name__, label, fn())

# ---------------------------------------------------------------- UI (no badge: cream glyph, dark outline)
U = "UI"


def ui(fn_draw):
    ic = Icon(shadow=True)
    lay = L()
    fn_draw(lay)
    ic.add(lay)
    return ic.im


def gear(l):
    pts = []
    for i in range(8):
        a = math.radians(i * 45)
        for da, r in [(-11, 14), (11, 14), (19, 10.5), (34, 10.5)]:
            pts.append((15.5 + r * math.cos(a + math.radians(da)), 15.5 + r * math.sin(a + math.radians(da))))
    l.poly(pts, "c").clear(11, 11, 20, 20)


UI = [
    ("ui_play", "Play", lambda l: l.poly([(8, 4), (26, 15.5), (8, 27)], "c")),
    ("ui_pause", "Pause", lambda l: l.rect(7, 5, 12, 26, "c").rect(19, 5, 24, 26, "c")),
    ("ui_speed_2x", "Speed 2x", lambda l: l.poly([(2, 6), (14, 15), (2, 24)], "c").poly([(14, 6), (26, 15), (14, 24)], "c")
     .spr("two", 22, 24).spr("x", 26, 26)),
    ("ui_settings", "Settings", gear),
    ("ui_sell", "Sell", lambda l: l.ell(3, 8, 22, 27, "c").poly(rot_ellipse(12.5, 17.5, 5, 3, -25), "t")
     .arc((10, 1, 29, 20), 280, 20, "c", 2).poly([(25, 14), (30, 13), (28, 18)], "c")),
    ("ui_upgrade", "Upgrade", lambda l: l.poly([(15.5, 3), (27, 15), (21, 15), (21, 18), (10, 18), (10, 15), (4, 15)], "c")
     .rect(10, 20, 21, 22, "u").rect(10, 25, 21, 27, "u")),
    ("ui_build", "Build", lambda l: l.line([(6, 27), (18, 15)], "c", 3).poly([(13, 9), (20, 3), (28, 11), (22, 18)], "c")),
    ("ui_close", "Close", lambda l: l.line([(7, 7), (24, 24)], "c", 5).line([(24, 7), (7, 24)], "c", 5)),
    ("ui_home", "Home", lambda l: l.poly([(15.5, 3), (29, 15), (25, 15), (25, 28), (19, 28), (19, 20), (12, 20), (12, 28), (6, 28), (6, 15), (2, 15)], "c")),
    ("ui_restart", "Restart", lambda l: l.arc((5, 5, 26, 26), 330, 270, "c", 4).poly([(16, 1), (25, 6), (17, 12)], "c")),
    ("ui_lock", "Locked", lambda l: l.arc((9, 3, 22, 18), 180, 360, "c", 3).rect(9, 10, 11, 14, "c").rect(20, 10, 22, 14, "c")
     .rect(6, 14, 25, 28, "c").rect(15, 18, 16, 23, "k")),
    ("ui_info", "Info", lambda l: l.ell(3, 3, 28, 28, "c").rect(14, 8, 17, 10, "k").rect(14, 13, 17, 23, "k")),
    ("ui_sound_on", "Sound On", lambda l: l.poly([(3, 11), (9, 11), (16, 4), (16, 27), (9, 20), (3, 20)], "c")
     .arc((12, 9, 24, 22), 300, 60, "c", 2).arc((10, 4, 30, 27), 300, 60, "c", 2)),
    ("ui_sound_off", "Sound Off", lambda l: l.poly([(3, 11), (9, 11), (16, 4), (16, 27), (9, 20), (3, 20)], "c")
     .line([(20, 11), (28, 20)], "r", 3).line([(28, 11), (20, 20)], "r", 3)),
    ("ui_music", "Music", lambda l: l.line([(11, 23), (11, 6), (26, 3), (26, 20)], "c", 3).line([(11, 7), (26, 4)], "c", 4)
     .ell(4, 20, 12, 27, "c").ell(19, 17, 27, 24, "c")),
]
for name, label, fn in UI:
    reg(U, name, label, ui(fn))

# ---------------------------------------------------------------- status effects on enemies (round badges)
ST = "Status Effects"


def status_bug(ic, x=7, y=15, **remap):
    ic.add(L().spr(recolor(big_bug(), **remap) if remap else big_bug(), x, y), outline=False)


def status_burn():
    ic = Icon(round_badge("#ffc27a", "#ff8a3d", "#b5381c"))
    status_bug(ic, 7, 16)
    ic.add(L().poly([(10, 17), (8, 12), (11, 9), (12, 12), (15, 4), (18, 10), (20, 7), (23, 12), (22, 17)], "o")
           .poly([(13, 17), (12, 13), (15, 9), (18, 13), (19, 17)], "y"))
    return ic.im


def status_poison():
    ic = Icon(round_badge("#c4ec8e", "#8ccf4f", "#2f6a1c"))
    status_bug(ic, 7, 16, r="g", R="G")
    ic.add(L().ell(9, 9, 13, 13, "g").px(10, 10, "w").ell(16, 4, 21, 9, "g").px(17, 5, "w").ell(22, 11, 24, 13, "g"))
    ic.add(L().pxs([(11, 28), (12, 29), (20, 28)], "g"), outline=False)
    return ic.im


def status_chill():
    ic = Icon(round_badge("#e4f6ff", "#a8dcf6", "#3a82b8"))
    status_bug(ic, 7, 16, r="B", R="n")
    flake = L()
    for a in range(0, 180, 45):
        dx, dy = math.cos(math.radians(a)) * 6, math.sin(math.radians(a)) * 6
        flake.line([(15.5 - dx, 9 - dy), (15.5 + dx, 9 + dy)], "w")
    flake.pxs([(15, 9), (16, 9)], "b")
    ic.add(flake)
    ic.add(L().pxs([(9, 28), (9, 29), (14, 28), (20, 28), (20, 29), (20, 30)], "w"), outline=False)
    return ic.im


def status_shock():
    ic = Icon(round_badge("#fff0a0", "#ffd23a", "#c28a10"))
    status_bug(ic, 7, 16)
    ic.add(L().poly([(18, 2), (11, 10), (15, 10), (12, 17), (21, 8), (17, 8), (20, 2)], "y").line([(17, 4), (14, 8)], "w"))
    ic.add(L().pxs([(5, 14), (6, 13), (26, 15), (25, 14), (4, 22), (27, 23)], "w"), outline=False)
    return ic.im


def status_shield():
    ic = Icon(round_badge("#c8d4ec", "#8b98ad", "#2e3648"))
    status_bug(ic, 7, 13)
    bubble = L()
    bubble.d.ellipse([3, 6, 28, 28], fill=rgba("b", 70), outline=rgba("b"), width=2)
    bubble.pxs([(8, 10), (9, 9), (10, 9), (7, 12)], "w")
    ic.add(bubble, outline=False)
    return ic.im


def status_regen():
    ic = Icon(round_badge("#d8f8c0", "#9fe04a", "#3f7a24"))
    status_bug(ic, 7, 16)
    lay = L()
    for x, y, arm, half in [(8, 8, 3, 1), (18, 5, 4, 1), (25, 12, 2, 0)]:
        lay.rect(x - arm, y - half, x + arm, y + half, "w")
        lay.rect(x - half, y - arm, x + half, y + arm, "w")
    ic.add(lay)
    return ic.im


def status_knockback():
    ic = Icon(round_badge("#d8c4ff", "#9a7ae8", "#3a2a78"))
    status_bug(ic, 3, 15)
    ic.add(L().arc((17, 13, 25, 27), 300, 60, "w").arc((19, 11, 28, 29), 310, 50, "c"), outline=False)
    ic.add(L().line([(11, 8), (24, 8)], "c", 2).poly([(11, 4), (5, 8), (11, 13)], "c"))
    return ic.im


def status_vulnerable():
    ic = Icon(round_badge("#f2806b", "#c8413a", "#6a1418"))
    left = L().poly([(6, 6), (14, 6), (12, 11), (15, 15), (12, 19), (14, 26), (8, 21), (6, 14)], "s").pxs([(8, 8), (8, 9), (9, 8)], "w")
    right = L().poly([(18, 5), (26, 5), (26, 13), (24, 20), (18, 25), (16, 18), (19, 14), (16, 10)], "S").px(24, 7, "s")
    ic.add(left)
    ic.add(right)
    ic.add(L().poly([(13, 24), (18, 24), (15.5, 28)], "y"))
    return ic.im


def combo_badge():
    return Icon(round_badge("#6a70b8", "#3a3f7a", "#1a1c40"))


def combo_frost_shatter():
    ic = combo_badge()
    ic.add(L().poly(star(15.5, 15.5, 7, 3, 6), "w"))
    shards = L()
    for a in range(20, 380, 60):
        cx, cy = 15.5 + 11 * math.cos(math.radians(a)), 15.5 + 11 * math.sin(math.radians(a))
        ux, uy = math.cos(math.radians(a)), math.sin(math.radians(a))
        shards.poly([(cx + ux * 3.5, cy + uy * 3.5), (cx - uy * 2, cy + ux * 2), (cx - ux * 2.5, cy - uy * 2.5), (cx + uy * 2, cy - ux * 2)], "b")
    ic.add(shards)
    ic.add(L().pxs([(15, 15), (16, 16), (15, 16), (16, 15)], "b"), outline=False)
    return ic.im


def combo_overcharge():
    ic = combo_badge()
    bubble = L()
    bubble.d.ellipse([4, 4, 27, 27], fill=rgba("b", 80), outline=rgba("b"), width=2)
    ic.add(bubble, outline=False)
    ic.add(L().poly([(19, 2), (9, 15), (15, 15), (11, 29), (23, 12), (17, 12), (21, 2)], "y").line([(18, 5), (13, 12)], "w"))
    ic.add(L().spr("spark_w", 3, 6).spr("spark_w", 25, 21))
    return ic.im


def combo_mine_chain():
    ic = combo_badge()
    # two mines joined by a fuse of sparks: the first has gone off, the second is next
    ic.add(L().poly(star(9, 13, 8, 3.5, 8), "u").poly(star(9, 13, 4.5, 2, 8), "y").ell(8, 12, 10, 14, "w"))
    ic.add(L().dots(seg([(15, 15), (19, 18), (22, 20)]), "y", 1, 1), outline=False)
    mine = L()
    mine.d.chord([17, 15, 28, 33], 180, 360, fill=rgba("S"))
    mine.rect(21, 13, 24, 15, "r").px(19, 19, "s")
    ic.add(mine)
    return ic.im


for fn, label in [(status_burn, "Burning"), (status_poison, "Poisoned"), (status_chill, "Chilled"),
                  (status_shock, "Shocked"), (status_shield, "Shielded"), (status_regen, "Regenerating"),
                  (status_knockback, "Knocked back"), (status_vulnerable, "Vulnerable"),
                  (combo_frost_shatter, "Combo: Frost Shatter"), (combo_overcharge, "Combo: Overcharge"),
                  (combo_mine_chain, "Combo: Mine Chain")]:
    reg(ST, fn.__name__, label, fn())

# ---------------------------------------------------------------- skill tree nodes (hexagon badges)
SK = "Skills"
BRANCH = {
    "ballistics": ("#ffc0a8", "#e8683c", "#a0301a", "#3a0c0f"),
    "control":    ("#e0f6ff", "#8fd0f0", "#3a82b8", "#0b2236"),
    "economy":    ("#fff2b0", "#f0c048", "#b5873e", "#3a2416"),
}


def hex_mask(x, y):
    return abs(x - 15.5) <= min(15.5, (15.5 - abs(y - 15.5)) * 2 + 0.5)


def skill_badge(branch):
    return Icon(_fill_gradient(hex_mask, *BRANCH[branch]))


def skill_ballistics_heavy_caliber():
    ic = skill_badge("ballistics")
    ic.add(L().rect(12, 13, 19, 24, "t").poly([(12, 13), (15.5, 5), (19, 13)], "S").rect(12, 22, 19, 24, "T")
           .line([(13, 14), (13, 20)], "w").px(15, 8, "s"))
    ic.add(L().line([(23, 12), (23, 18)], "y", 2).line([(20, 15), (26, 15)], "y", 2))
    return ic.im


def skill_ballistics_rapid_cycling():
    ic = skill_badge("ballistics")
    ic.add(L().line([(4, 10), (9, 10)], "c").line([(6, 16), (13, 16)], "c").line([(4, 22), (9, 22)], "c"), outline=False)
    lay = L()
    for x, y in [(12, 8), (17, 14), (12, 20)]:
        lay.ell(x, y, x + 5, y + 4, "g").px(x + 1, y + 1, "w")
    ic.add(lay)
    ic.add(L().poly([(22, 12), (27, 16), (22, 20)], "y"))
    return ic.im


def skill_ballistics_explosive_payload():
    ic = skill_badge("ballistics")
    ic.add(L().ell(4, 4, 27, 27, ol="c"), outline=False)
    ic.add(L().poly(star(15.5, 15.5, 10, 5, 8), "u").poly(star(15.5, 15.5, 6, 3, 8), "y").ell(14, 14, 17, 17, "w"))
    return ic.im


def skill_control_deep_freeze():
    ic = skill_badge("control")
    lay = L()
    for a in range(0, 180, 60):
        dx, dy = math.cos(math.radians(a + 90)) * 10, math.sin(math.radians(a + 90)) * 10
        lay.line([(15.5 - dx, 15.5 - dy), (15.5 + dx, 15.5 + dy)], "w", 2)
    for a in range(0, 360, 60):
        ux, uy = math.cos(math.radians(a + 90)), math.sin(math.radians(a + 90))
        bx, by = 15.5 + ux * 6.5, 15.5 + uy * 6.5
        for s in (-1, 1):
            vx, vy = math.cos(math.radians(a + 90 + s * 55)), math.sin(math.radians(a + 90 + s * 55))
            lay.line([(bx, by), (bx + vx * 3, by + vy * 3)], "w")
    ic.add(lay)
    ic.add(L().pxs([(15, 15), (16, 15), (15, 16), (16, 16)], "b"), outline=False)
    return ic.im


def skill_control_heavy_shockwave():
    ic = skill_badge("control")
    ic.add(L().arc((-2, 6, 14, 25), 300, 60, "w", 2).arc((2, 4, 20, 27), 300, 60, "w", 2).arc((8, 2, 26, 29), 305, 55, "c", 2))
    ic.add(L().ell(4, 13, 8, 18, "y"))
    return ic.im


def skill_control_absolute_zero():
    ic = skill_badge("control")
    ic.add(L().poly([(9, 8), (20, 6), (24, 12), (23, 24), (11, 26), (7, 19)], "b").line([(9, 8), (13, 13), (24, 12)], "w")
           .line([(13, 13), (11, 26)], "B").pxs([(10, 10), (11, 11)], "w"))
    ic.add(L().spr(recolor(SPR["bug5"], **{"1": "n", "2": "B", "3": "n"}), 14, 15), outline=False)
    ic.add(L().line([(20, 3), (17, 9), (20, 10), (18, 14)], "w"), outline=False)
    return ic.im


def skill_economy_scavenger_bounties():
    ic = skill_badge("economy")
    # one big seed, drawn dark-rimmed so it stands off the gold badge, with a bounty "+"
    ic.add(L().poly(rot_ellipse(14, 18, 9, 6, -25), "T"))
    ic.add(L().poly(rot_ellipse(14, 17, 7.5, 4.6, -25), "t").line([(9, 20), (14, 17), (19, 15)], "T")
           .pxs([(11, 14), (12, 13), (13, 13)], "w"), outline=False)
    ic.add(L().line([(23, 6), (23, 13)], "g", 3).line([(20, 9), (27, 9)], "g", 3))
    return ic.im


def skill_economy_bulk_discounts():
    ic = skill_badge("economy")
    ic.add(L().poly([(6, 14), (13, 7), (25, 7), (25, 19), (18, 26), (6, 26)], "c").clear(20, 9, 22, 11))
    ic.add(L().rect(9, 13, 11, 15, "r").rect(15, 20, 17, 22, "r").line([(17, 13), (10, 22)], "r", 2), outline=False)
    return ic.im


def skill_economy_salvage_mastery():
    ic = skill_badge("economy")
    ic.add(L().arc((6, 6, 25, 25), 20, 290, "g", 3).poly([(19, 2), (26, 7), (18, 11)], "g"))
    ic.add(L().line([(11, 20), (17, 14)], "s", 2).ell(15, 9, 21, 15, "s").clear(18, 8, 22, 11).px(12, 19, "w"))
    return ic.im


for fn, label in [(skill_ballistics_heavy_caliber, "Heavy Caliber"), (skill_ballistics_rapid_cycling, "Rapid Cycling"),
                  (skill_ballistics_explosive_payload, "Explosive Payload"), (skill_control_deep_freeze, "Deep Freeze"),
                  (skill_control_heavy_shockwave, "Heavy Shockwave"), (skill_control_absolute_zero, "Absolute Zero"),
                  (skill_economy_scavenger_bounties, "Scavenger Bounties"), (skill_economy_bulk_discounts, "Bulk Discounts"),
                  (skill_economy_salvage_mastery, "Salvage Mastery")]:
    reg(SK, fn.__name__, label, fn())

# ---------------------------------------------------------------- graybox enemies (the ones actually in the game)
GE = "Graybox Enemies"


def side_beetle(shell, hi, shade="k"):
    """The top-down beetle turned to face right, like the in-game enemies."""
    return rot90(beetle(shell, hi, shade))


def enemy_runner():
    ic = Icon(badge("enemy"))
    ic.add(L().line([(3, 11), (9, 11)], "c").line([(2, 16), (10, 16)], "c").line([(4, 21), (9, 21)], "c"), outline=False)
    ic.add(L().spr(side_beetle("i", "I"), 13, 9), outline=False)
    return ic.im


def enemy_grunt():
    ic = Icon(badge("enemy"))
    legs = L()
    for x, top, bottom in [(11, 7, 24), (15, 6, 25), (19, 7, 24)]:
        legs.line([(x + 1, 13), (x, top + 2), (x - 2, top)], "k").line([(x + 1, 18), (x, bottom - 2), (x - 2, bottom)], "k")
    ic.add(legs, outline=False)
    ic.add(L().ell(6, 10, 21, 22, "h").ell(8, 11, 19, 16, "H").ell(7, 12, 20, 22, "h").line([(8, 16), (20, 16)], "k")
           .pxs([(10, 12), (11, 12), (12, 12)], "w"))
    ic.add(L().ell(19, 11, 26, 21, "h").pxs([(23, 14), (23, 18)], "c").px(21, 13, "H"))
    ic.add(L().line([(26, 14), (28, 11)], "k").line([(26, 18), (28, 21)], "k"), outline=False)
    return ic.im


def enemy_brute():
    ic = Icon(badge("enemy"))
    legs = L()
    for x, top, bottom in [(8, 4, 27), (13, 3, 28), (19, 4, 27)]:
        legs.line([(x + 2, 12), (x, top + 2), (x - 2, top)], "k").line([(x + 2, 20), (x, bottom - 2), (x - 2, bottom)], "k")
    ic.add(legs, outline=False)
    ic.add(L().ell(3, 7, 23, 25, "q").ell(5, 8, 21, 16, "Q").ell(4, 10, 22, 25, "q").line([(5, 16), (21, 16)], "e")
           .pxs([(8, 10), (9, 9), (10, 9), (11, 9)], "t"))
    ic.add(L().ell(21, 10, 29, 22, "q").pxs([(26, 13), (26, 19)], "c").pxs([(23, 12), (24, 11)], "Q"))
    ic.add(L().line([(29, 13), (30, 10)], "k").line([(29, 19), (30, 22)], "k"), outline=False)
    return ic.im


def enemy_shielded():
    ic = Icon(badge("enemy"))
    ic.add(L().spr(side_beetle("B", "b", "n"), 9, 9), outline=False)
    bubble = L()
    bubble.d.ellipse([3, 3, 28, 28], fill=rgba("b", 70), outline=rgba("b"), width=2)
    bubble.pxs([(8, 8), (9, 7), (10, 7), (7, 10)], "w")
    ic.add(bubble, outline=False)
    return ic.im


def grub(lay, x, y, body, flip=False):
    lay.rect(x, y, x + 6, y + 2, body).px(x + (0 if flip else 6), y + 1, "y").px(x + (5 if flip else 1), y, "w")
    return lay


def enemy_swarm():
    ic = Icon(badge("enemy"))
    lay = L()
    for x, y, c in [(4, 6, "f"), (17, 5, "p"), (10, 12, "c"), (20, 14, "f"), (5, 19, "p"), (15, 21, "c")]:
        grub(lay, x, y, c)
    ic.add(lay)
    legs = L()
    for x, y in [(4, 6), (17, 5), (10, 12), (20, 14), (5, 19), (15, 21)]:
        legs.pxs([(x + 1, y + 4), (x + 3, y + 4), (x + 5, y + 4)], "k")
    ic.add(legs, outline=False)
    return ic.im


def enemy_splitter():
    ic = Icon(badge("enemy"))
    body = L()
    for i, x in enumerate([4, 8, 12, 16]):
        body.ell(x, 12, x + 5, 18, "P").px(x + 1, 13, "p")
    body.pxs([(20, 14), (20, 16)], "y")
    ic.add(body)
    legs = L()
    for x in (6, 10, 14, 18):
        legs.pxs([(x, 10), (x, 20)], "k")
    ic.add(legs, outline=False)
    ic.add(L().line([(21, 13), (24, 8)], "c").line([(21, 17), (24, 22)], "c"), outline=False)
    ic.add(grub(grub(L(), 22, 4, "P"), 22, 23, "P"))
    return ic.im


def enemy_healer():
    ic = Icon(badge("enemy"))
    legs = L()
    for x in (9, 14, 19):
        legs.line([(x, 17), (x - 2, 12), (x - 3, 9)], "M").line([(x, 19), (x - 2, 24), (x - 3, 27)], "M")
    ic.add(legs)
    ic.add(L().poly([(3, 18), (8, 15), (22, 15), (26, 18), (22, 21), (8, 21)], "m").line([(6, 18), (22, 18)], "M")
           .ell(23, 15, 28, 21, "m").px(26, 17, "y").pxs([(9, 16), (10, 16), (11, 16)], "g"))
    ic.add(L().rect(20, 3, 22, 11, "w").rect(17, 6, 25, 8, "w"))
    ic.add(L().rect(21, 4, 21, 10, "g").rect(18, 7, 24, 7, "g"), outline=False)
    return ic.im


for fn, label in [(enemy_runner, "Runner"), (enemy_grunt, "Grunt"), (enemy_brute, "Brute"),
                  (enemy_shielded, "Shielded"), (enemy_swarm, "Swarm"), (enemy_splitter, "Splitter"),
                  (enemy_healer, "Healer")]:
    reg(GE, fn.__name__, label, fn())

# ---------------------------------------------------------------- achievements (crest badges)
AC = "Achievements"


def crest_mask(x, y):
    if y <= 17:
        cut = {0: 2, 1: 1}.get(y, 0)
        return cut <= x <= 31 - cut
    return abs(x - 15.5) <= 15.5 - (y - 17) * 1.12


def ach():
    return Icon(_fill_gradient(crest_mask, "#8f9cf0", "#5560c0", "#2f357a", "#12143a"))


def ach_first_blood():
    ic = ach()
    ic.add(L().poly([(15.5, 4), (21, 13), (22, 18), (19, 22), (12, 22), (9, 18), (10, 13)], "r")
           .pxs([(13, 14), (13, 15), (14, 12)], "f"))
    return ic.im


def ach_colony_defender():
    ic = ach()
    ic.add(L().poly([(7, 4), (24, 4), (24, 13), (15.5, 23), (7, 13)], "s").line([(8, 5), (23, 5)], "w")
           .line([(22, 6), (22, 13)], "S").line([(22, 13), (16, 21)], "S"))
    ic.add(L().spr("bust", 10, 6), outline=False)
    return ic.im


def ach_ant_terminator():
    ic = ach()
    ic.add(L().ell(9, 4, 22, 16, "c").rect(12, 15, 19, 19, "c"))
    ic.add(L().rect(11, 9, 13, 12, "k").rect(18, 9, 20, 12, "k").pxs([(15, 13), (16, 13)], "k")
           .pxs([(13, 17), (13, 18), (15, 17), (15, 18), (16, 17), (16, 18), (18, 17), (18, 18)], "k"), outline=False)
    return ic.im


def ach_gold_tycoon():
    ic = ach()
    lay = L()
    for y in (17, 13, 9):
        lay.ell(8, y, 23, y + 6, "Y").ell(8, y - 2, 23, y + 4, "y")
    lay.pxs([(11, 9), (12, 8), (13, 8)], "w")
    ic.add(lay)
    ic.add(L().spr("spark_w", 23, 4))
    return ic.im


def ach_architect():
    ic = ach()
    ic.add(L().line([(10, 22), (18, 12)], "T", 2).poly([(13, 8), (18, 4), (25, 11), (20, 15)], "s").px(17, 7, "w"))
    ic.add(L().rect(6, 6, 10, 8, "s").rect(8, 10, 12, 12, "s"))
    return ic.im


def ach_boss_slayer():
    ic = ach()
    ic.add(L().spr("crown", 11, 4))
    ic.add(L().ell(11, 12, 20, 19, "c").rect(13, 19, 18, 21, "c"))
    ic.add(L().rect(13, 15, 14, 16, "k").rect(17, 15, 18, 16, "k").pxs([(14, 20), (16, 20), (17, 20)], "k"), outline=False)
    return ic.im


def ach_wave_survivor():
    ic = ach()
    ic.add(L().ell(5, 8, 15, 18, ol="y", w=2).ell(16, 8, 26, 18, ol="y", w=2))
    ic.add(L().pxs([(7, 10), (8, 9), (18, 10), (19, 9)], "w"), outline=False)
    return ic.im


def ach_commander():
    ic = ach()
    ic.add(L().poly(star(15.5, 12.5, 10, 4.2, 5), "y").pxs([(14, 8), (15, 7), (13, 10)], "w"))
    return ic.im


def ach_mine_master():
    ic = ach()
    ic.add(L().poly(star(15.5, 11, 10, 5, 8), "u").poly(star(15.5, 11, 6, 3, 8), "y"))
    mine = L()
    mine.d.chord([10, 14, 21, 30], 180, 360, fill=rgba("S"))
    mine.rect(14, 12, 17, 14, "r").px(12, 17, "s")
    ic.add(mine)
    return ic.im


def ach_fortress():
    ic = ach()
    wall = L()
    wall.rect(7, 10, 24, 20, "s")
    for x in (7, 12, 17, 22):
        wall.rect(x, 6, x + 2, 9, "s")
    wall.line([(7, 13), (24, 13)], "S").line([(7, 17), (24, 17)], "S")
    wall.pxs([(11, 11), (11, 12), (19, 11), (19, 12), (15, 14), (15, 15), (15, 16), (10, 18), (10, 19), (20, 18), (20, 19)], "S")
    wall.pxs([(8, 7), (13, 7)], "w")
    ic.add(wall)
    ic.add(L().spr("heart", 11, 11))
    return ic.im


for fn, label in [(ach_first_blood, "First Blood"), (ach_colony_defender, "Colony Defender"),
                  (ach_ant_terminator, "Ant Terminator"), (ach_gold_tycoon, "Gold Tycoon"),
                  (ach_architect, "Master Architect"), (ach_boss_slayer, "Boss Slayer"),
                  (ach_wave_survivor, "Endless Survivor"), (ach_commander, "Supreme Commander"),
                  (ach_mine_master, "Minefield Master"), (ach_fortress, "Impenetrable Fortress")]:
    reg(AC, fn.__name__, label, fn())

# ---------------------------------------------------------------- app icon
SKY = ["#2a1640", "#3b1d4a", "#5a2450", "#8a2f4e", "#b53e43", "#d8553f", "#f07444", "#ff9a4e", "#ffb35a"]


def app_icon():
    im = Image.new("RGBA", (N, N))
    p = im.load()
    for y in range(N):
        for x in range(N):
            f = y / 3.6
            i = int(f)
            frac = f - i
            idx = min(i + (1 if frac > .5 and (x + y) % 2 else 0), len(SKY) - 1)
            p[x, y] = rgba(SKY[idx])
    ic = Icon(im, shadow=False)
    ic.add(L().ell(3, 2, 19, 18, "u").ell(5, 4, 17, 16, "#ffd27a").ell(7, 6, 15, 14, "#fff2b0"), outline=False)
    hill = L()
    for x in range(N):
        top = round(15 + max(0, abs(x - 15.5) - 6) ** 2 * 0.12)
        for y in range(top, N):
            hill.px(x, y, "d" if y < 25 else "D")
        hill.px(x, top - 1, "k")
    hill.pxs([(5, 21), (6, 20), (7, 19), (8, 18), (9, 17)], "T")
    hill.line([(17, 16), (16, 20), (19, 23)], "e").line([(16, 20), (11, 25), (9, 29)], "e").line([(19, 23), (24, 28)], "e")
    hill.ell(14, 26, 19, 30, "e").pxs([(16, 28), (17, 28)], "u")
    ic.add(hill, outline=False)
    ic.add(L().line([(20, 1), (20, 14)], "T"))
    ic.add(L().poly([(21, 1), (29, 2), (27, 4), (29, 7), (21, 7)], "r").px(22, 2, "f"))
    ic.add(L().spr("ant_side", 5, 8), outline=False)
    ic.add(L().pxs([(18, 10), (19, 9)], "a"), outline=False)
    return ic.im


APP = app_icon()

# ================================================================ output


def pascal(name):
    return "".join(w.capitalize() for w in name.split("_")) + "Icon"


def up(im, size):
    """Integer nearest-neighbour upscale, then a gentle resample to the exact size."""
    k = max(1, math.ceil(size / N))
    big = im.resize((N * k, N * k), Image.NEAREST)
    return big if big.size[0] == size else big.resize((size, size), Image.LANCZOS)


def main():
    os.makedirs(PNG_DIR, exist_ok=True)
    os.makedirs(APP_DIR, exist_ok=True)
    for name, (_, _, im) in ICONS.items():
        im.save(os.path.join(PNG_DIR, pascal(name) + ".png"))

    APP.save(os.path.join(APP_DIR, "AppIcon_32.png"))
    for size in (1024, 512, 256, 192, 180, 167, 152, 128, 120, 96, 87, 80, 76, 64, 58, 48, 40, 29, 20, 16):
        img = APP.resize((16, 16), Image.BOX) if size == 16 else up(APP, size)
        img.save(os.path.join(APP_DIR, f"AppIcon_{size}.png"))
    # rounded, transparent-corner version for desktop
    rounded = APP.copy()
    rp = rounded.load()
    for y in range(N):
        for x in range(N):
            if not square_mask(x, y):
                rp[x, y] = (0, 0, 0, 0)
    rounded.save(os.path.join(APP_DIR, "AppIconRounded_32.png"))
    up(rounded, 256).save(os.path.join(APP_DIR, "AppIconRounded_256.png"))
    up(rounded, 1024).save(os.path.join(APP_DIR, "AppIconRounded_1024.png"))
    frames = [rounded.resize((16, 16), Image.BOX)] + [up(rounded, n) for n in (32, 48, 64, 128, 256)]
    frames[-1].save(os.path.join(APP_DIR, "AppIcon.ico"), format="ICO", sizes=[f.size for f in frames],
                    append_images=frames[:-1])

    # preview sheet: 8x, grouped
    scale, cols, cell_w, cell_h = 4, 10, 32 * 4 + 16, 32 * 4 + 16
    groups = {}
    groups.setdefault("App", []).append(("app", APP))
    for name, (g, _, im) in ICONS.items():
        groups.setdefault(g, []).append((name, im))
    rows = sum(math.ceil(len(v) / cols) for v in groups.values())
    sheet = Image.new("RGBA", (cols * cell_w + 16, rows * cell_h + 16), rgba("#241d18"))
    y = 8
    for g, items in groups.items():
        for i, (_, im) in enumerate(items):
            r, c = divmod(i, cols)
            sheet.alpha_composite(im.resize((32 * scale, 32 * scale), Image.NEAREST), (8 + c * cell_w + 8, y + r * cell_h + 8))
        y += math.ceil(len(items) / cols) * cell_h
    sheet.save(os.path.join(HERE, "IconSheet.png"))
    print(f"wrote {len(ICONS)} icons + app icon")


if __name__ == "__main__":
    main()
