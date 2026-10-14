"""Hold the Hill animation sheet generator.

Draws every tower, enemy and effect frame by frame in code (same palette and 1 px ink
outline as Icons/), then writes, per sprite:

  ../Aseprite/<Group>/<Name>.aseprite   layered file with one tag per animation; Unity's
                                        Aseprite Importer turns each tag into a clip
  ../Sheets/<Group>/<Name>.png + .json  packed sheet, one row per tag (Aseprite json-array format)
  ./Preview/<Name>.gif                  4x preview cycling every tag
  ./AnimSheetPreview.png                every frame of everything, 3x, for review

    python build_anim_sheets.py

Needs Python with Pillow. Coordinates: towers are top-down and face up (they don't rotate
in game); enemies are top-down and face right, because EnemyMover rotates the transform so
that +x points along the path.
"""
import math
import os
import random

from PIL import Image, ImageDraw, ImageFont

from aseprite_file import read_aseprite, write_aseprite

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, ".."))

# ---------------------------------------------------------------- palette
PAL = {
    "k": "#1b110b",                                  # ink / outline
    "a": "#4a2c1c", "A": "#a0643c", "z": "#2f1a10",  # ant body, highlight, shade
    "q": "#5a2414", "Q": "#c0603a", "Z": "#33130b",  # red-brown (kicker, stag beetle)
    "N": "#1f2f4a", "n2": "#4f72a8", "Nz": "#111a2b",  # frost ant
    "c": "#fff1d6", "w": "#ffffff",
    "y": "#ffd23a", "Y": "#c28a10", "u": "#ffb03a",
    "o": "#ff8a3d", "r": "#e2412f", "R": "#8e2124", "f": "#ff6f7d",
    "g": "#9fe04a", "G": "#4c8a2c", "j": "#2f5a1c",
    "b": "#9fe7ff", "B": "#5aa9d8", "n": "#2d6a96",
    "p": "#e6d4ff", "P": "#9a6cf0", "V": "#5a3aa8",
    "s": "#c4bfb2", "S": "#6d675c", "x": "#45413a",
    "t": "#ecc477", "T": "#b5873e",
    "d": "#83562f", "D": "#5b3a23", "e": "#3a2416",
    "m": "#7cc84a", "M": "#3f7a24",
    "i": "#2f8f74", "I": "#a8f0d0", "iz": "#1a5446",
    "h": "#3b4458", "H": "#b8c6dc", "hz": "#232937",
    "v": "#2a1c30",
    "l": "#c27ab0", "L": "#7a3a6a", "Lz": "#45193d",  # centipede (splitter) purple-brown
}


def rgba(c, a=255):
    if isinstance(c, tuple):
        return (*c[:3], a)
    h = PAL.get(c, c).lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)


def mix(c1, c2, t):
    a, b = rgba(c1), rgba(c2)
    return tuple(round(a[i] + (b[i] - a[i]) * t) for i in range(3)) + (255,)


# ---------------------------------------------------------------- canvas
class Cv:
    """An RGBA canvas with pixel-art drawing helpers. Never anti-aliases."""

    def __init__(self, w, h):
        self.w, self.h = w, h
        self.im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
        self.p = self.im.load()

    def px(self, x, y, c, a=255):
        x, y = int(math.floor(x)), int(math.floor(y))
        if 0 <= x < self.w and 0 <= y < self.h:
            self.p[x, y] = rgba(c, a)

    def pxs(self, pts, c, a=255):
        for x, y in pts:
            self.px(x, y, c, a)

    def line(self, pts, c, a=255):
        for x, y in seg(pts):
            self.px(x, y, c, a)

    def blob(self, cx, cy, rx, ry, base, hi=None, lo=None, a=255, light=(-0.6, -0.8)):
        """Filled ellipse centred on (cx, cy) in pixel-edge coordinates, rim-lit from the top left."""
        for y in range(int(cy - ry) - 1, int(cy + ry) + 2):
            for x in range(int(cx - rx) - 1, int(cx + rx) + 2):
                nx, ny = (x + 0.5 - cx) / rx, (y + 0.5 - cy) / ry
                r2 = nx * nx + ny * ny
                if r2 > 1.0:
                    continue
                s = nx * light[0] + ny * light[1]
                c = base
                if hi and s > 0.3 and r2 > 0.3:
                    c = hi
                elif lo and s < -0.3 and r2 > 0.4:
                    c = lo
                self.px(x, y, c, a)

    def disc(self, cx, cy, r, c, a=255):
        self.blob(cx, cy, r, r, c, a=a)

    def ring(self, cx, cy, r, c, w=1.0, a=255, ry=None, gaps=None):
        """Pixel ring of radius r and thickness w. gaps: callable(angle_deg) -> True to skip."""
        ry = ry or r
        k = ry / r
        for y in range(int(cy - ry) - 2, int(cy + ry) + 3):
            for x in range(int(cx - r) - 2, int(cx + r) + 3):
                dx, dy = x + 0.5 - cx, (y + 0.5 - cy) / k
                d = math.hypot(dx, dy)
                if r - w < d <= r:
                    if gaps and gaps(math.degrees(math.atan2(dy, dx)) % 360):
                        continue
                    self.px(x, y, c, a)

    def spr(self, rows, x, y, flip=False):
        for j, row in enumerate(rows):
            for i, ch in enumerate(row[::-1] if flip else row):
                if ch != ".":
                    self.px(x + i, y + j, ch)

    def paste(self, other, dx=0, dy=0, alpha=1.0):
        im = other.im if isinstance(other, Cv) else other
        if alpha < 1.0:
            im = fade(im, alpha)
        layer = Image.new("RGBA", (self.w, self.h), (0, 0, 0, 0))
        layer.paste(im, (int(dx), int(dy)))
        self.im.alpha_composite(layer)
        self.p = self.im.load()
        return self

    def outline(self, c="k"):
        """1 px 4-neighbour outline around everything opaque."""
        src = self.im.copy().load()
        col = rgba(c)
        for y in range(self.h):
            for x in range(self.w):
                if src[x, y][3]:
                    continue
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < self.w and 0 <= ny < self.h and src[nx, ny][3] > 100:
                        self.p[x, y] = col
                        break
        return self


def seg(pts):
    """Polyline sampled to integer pixels."""
    pts = [(math.floor(x), math.floor(y)) for x, y in pts]
    out = [pts[0]]
    for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
        n = max(abs(x1 - x0), abs(y1 - y0), 1)
        for i in range(n + 1):
            p = (round(x0 + (x1 - x0) * i / n), round(y0 + (y1 - y0) * i / n))
            if out[-1] != p:
                out.append(p)
    return out


def fade(im, alpha):
    r, g, b, a = im.split()
    a = a.point(lambda v: int(v * alpha))
    return Image.merge("RGBA", (r, g, b, a))


def tint(im, c, t, keep_ink=True):
    """Blend every opaque pixel toward colour c by t; ink pixels stay unless keep_ink is False."""
    out = im.copy()
    p = out.load()
    ink = rgba("k")
    target = rgba(c)
    for y in range(out.height):
        for x in range(out.width):
            v = p[x, y]
            if not v[3] or (keep_ink and v[:3] == ink[:3]):
                continue
            p[x, y] = tuple(round(v[i] + (target[i] - v[i]) * t) for i in range(3)) + (v[3],)
    return out


def shift(im, dx, dy, box=None):
    """Move the whole image, or only the part inside box, by (dx, dy)."""
    out = Image.new("RGBA", im.size, (0, 0, 0, 0))
    if box is None:
        out.paste(im, (dx, dy))
        return out
    rest = im.copy()
    ImageDraw.Draw(rest).rectangle([box[0], box[1], box[2] - 1, box[3] - 1], fill=(0, 0, 0, 0))
    piece = im.crop(box)
    out.alpha_composite(rest)
    layer = Image.new("RGBA", im.size, (0, 0, 0, 0))
    layer.paste(piece, (box[0] + dx, box[1] + dy))
    out.alpha_composite(layer)
    return out


def mirror_x(pts, cx):
    return [(2 * cx - 1 - x, y) for x, y in pts]


def mirror_y(pts, cy):
    return [(x, 2 * cy - 1 - y) for x, y in pts]


SPARK = ["..y..", ".yyy.", "yywyy", ".yyy.", "..y.."]
SPARK_S = [".w.", "wyw", ".w."]
FLAKE = ["w.w.w", ".wbw.", "wbwbw", ".wbw.", "w.w.w"]
FLAKE_S = [".b.", "bwb", ".b."]
PLUS = [".g.", "gwg", ".g."]
SNOW = ["...b...", ".b.b.b.", "..bwb..", "bbwwwbb", "..bwb..", ".b.b.b.", "...b..."]
PLUS_L = ["..g..", "..g..", "ggwgg", "..g..", "..g.."]


# ================================================================ sprites
class Sprite:
    def __init__(self, name, group, w, h, layers, caste=""):
        self.name, self.group, self.w, self.h = name, group, w, h
        self.layers = layers
        self.caste = caste
        self.tags = []      # (name, [ {layer: Image} ], duration, repeat)

    def tag(self, name, frames, duration, repeat=0):
        self.tags.append((name, frames, duration, repeat))

    def new(self):
        return {name: Cv(self.w, self.h) for name in self.layers}


def flat(layers):
    return {k: (v.im if isinstance(v, Cv) else v) for k, v in layers.items()}


# ================================================================ towers (32x32, face up)
TW = 32
CX = 16  # pixel-edge centre: the ant is mirror-symmetric about x = 15.5


def tower_base(L, mound=("T", "t", "d")):
    sh = L["Shadow"]
    sh.blob(CX, 21.5, 14.5, 10, (0, 0, 0), a=85)
    base = Cv(TW, TW)
    base.blob(CX, 19, 13.5, 10.5, *mound)
    # inner terrace and a few pebbles / grass so it reads as an ant hill
    base.ring(CX, 19.5, 10.5, mound[2], 1, ry=8)
    for x, y, c in ((6, 15, "c"), (25, 14, "c"), (8, 24, "S"), (24, 25, "s"), (21, 27, "d"), (10, 27, "d")):
        base.px(x, y, c)
    base.outline()
    for x, y in ((4, 20), (27, 21), (5, 23)):
        base.pxs([(x, y), (x + 1, y - 1), (x + 1, y)], "m")
    L["Base"].paste(base)


def ant(L, cx=CX, cy=16, col=("a", "A", "z"), head="ant", bob=0, abd=0.0, head_dy=0, head_dx=0,
        tw=(0, 0), legs=0, mand=0, antennae=True, ant_tips=None, lift=0, layer="Body"):
    """Top-down ant facing up. bob moves the whole body down, abd swells the abdomen,
    tw = antenna tip wiggle (left, right), legs = leg swing (-1..1), mand = mandible spread."""
    body = Cv(TW, TW)
    ink = Cv(TW, TW)
    y0 = cy + bob - lift
    hy = y0 - 7 + head_dy
    # legs: hip -> knee -> foot, left side; right side is mirrored
    sw = round(legs)
    left = [
        [(cx - 2, y0 - 2), (cx - 5, y0 - 4), (cx - 6, y0 - 7 - sw)],
        [(cx - 2, y0 - 1), (cx - 6, y0 - 1), (cx - 8, y0 + 1 + sw)],
        [(cx - 2, y0), (cx - 5, y0 + 2), (cx - 7, y0 + 6 - sw)],
    ]
    for i, leg in enumerate(left):
        ink.line(leg, "k")
        other = mirror_x(leg, cx)
        if sw:  # opposite tripod swings the other way
            other = [(x, y + (2 * sw if j == 2 else 0) * (1 if i != 1 else -1)) for j, (x, y) in enumerate(other)]
        ink.line(other, "k")
    if antennae:
        for side, t in ((-1, tw[0]), (1, tw[1])):
            if ant_tips:
                tip = ant_tips[0 if side < 0 else 1]
                pts = [(cx - 2, hy - 2), (cx - 4, hy - 5), tip]
            else:
                pts = [(cx - 2, hy - 2), (cx - 4, hy - 5), (cx - 3 - t, hy - 8)]
            ink.line(pts if side < 0 else mirror_x(pts, cx), "k")
    # mandibles
    m = [(cx - 2 - mand, hy - 3), (cx - 1 - mand, hy - 5)]
    ink.line(m, "k")
    ink.line(mirror_x(m, cx), "k")

    base, hi, lo = col
    # separate blobs with a one-pixel gap between them, so the outline pass draws the waist
    body.blob(cx, y0 + 7, 4 + abd, 4.6 + abd, base, hi, lo)      # gaster
    body.blob(cx, y0 + 2.5, 0.8, 0.8, base)                      # petiole
    body.blob(cx, y0 - 1, 1.6, 2.4, base, hi, lo)                # thorax
    hr = {"ant": (3.5, 2.8), "soldier": (4.5, 3.2), "brute": (4.5, 3.3), "major": (5, 3.6)}[head]
    body.blob(cx + head_dx, hy, hr[0], hr[1], base, hi, lo)      # head
    body.outline()
    # eyes and the abdomen band sit on top of the outline pass
    body.px(cx - 3 + head_dx, hy - 1, "k")
    body.px(cx + 2 + head_dx, hy - 1, "k")
    body.line([(cx - 2, y0 + 7 + round(abd)), (cx + 1, y0 + 7 + round(abd))], lo)
    ink.paste(body)
    L[layer].paste(ink)


def tower_sprite(name, caste, draw, idle_frames=4, idle_ms=160, attack_frames=6, attack_ms=70, mound=("T", "t", "d")):
    """draw(L, tag, i, n) paints one frame's Body/FX; the mound and shadow are shared."""
    s = Sprite(name, "Towers", TW, TW, ["Shadow", "Base", "Body", "FX"], caste)

    def frames(tag, n):
        out = []
        for i in range(n):
            L = s.new()
            tower_base(L, mound)
            draw(L, tag, i, n)
            out.append(flat(L))
        return out

    s.tag("Idle", frames("Idle", idle_frames), idle_ms)
    s.tag("Attack", frames("Attack", attack_frames), attack_ms, repeat=1)
    s.tag("Upgrade", upgrade_frames(s, draw, mound), 80, repeat=1)
    return s


IDLE_TW = [(0, 0), (1, 0), (1, 1), (0, 1), (0, 0), (1, 0)]
IDLE_ABD = [0.0, 0.25, 0.5, 0.25, 0.0, 0.25]


def idle_kw(i):
    return dict(tw=IDLE_TW[i % 6], abd=IDLE_ABD[i % 6])


def upgrade_frames(s, draw, mound):
    """Gold sparkles spiral up, the tower flashes, a gold ring pops at the base."""
    out = []
    n = 6
    for i in range(n):
        L = s.new()
        tower_base(L, mound)
        draw(L, "Idle", 0, 4)
        flash = [0.0, 0.15, 0.3, 0.2, 0.08, 0.0][i]
        if flash:
            L["Body"] = Cv(TW, TW).paste(tint(L["Body"].im, "y", flash))
        fx = L["FX"]
        if i < 5:
            fx.ring(CX, 20, 4 + i * 2.6, "y", 1, a=255 - i * 40, ry=3 + i * 2)
        for k in range(4):
            ang = math.radians(k * 90 + i * 35)
            r = 11 - i * 0.6
            x, y = CX + math.cos(ang) * r - 2, 18 + math.sin(ang) * r * 0.6 - i * 2.2 - 2
            if i < 5:
                fx.spr(SPARK if (k + i) % 2 else SPARK_S, x, y)
        if 1 <= i <= 4:  # chevron rising over the head
            y = 6 - i
            fx.line([(CX - 4, y + 3), (CX - 1, y), (CX, y), (CX + 3, y + 3)], "y")
            fx.line([(CX - 4, y + 4), (CX - 1, y + 1), (CX, y + 1), (CX + 3, y + 4)], "Y")
        out.append(flat(L))
    return out


# ---- individual towers ---------------------------------------------------------------------
def glob(fx, x, y, r, c=("g", "w", "G")):
    fx.blob(x, y, r, r, c[0], c[1], c[2])


def draw_linear(L, tag, i, n):
    if tag == "Idle":
        ant(L, **idle_kw(i))
        if i == 2:
            L["FX"].px(CX - 1, 6, "g")  # a drip at the jaws
        return
    head = [1, 2, -1, -1, 0, 0][i]
    ant(L, head_dy=head, bob=[0, 1, 0, 0, 0, 0][i], mand=[0, 1, 1, 0, 0, 0][i])
    fx = Cv(TW, TW)
    if i == 1:
        glob(fx, CX, 9, 1.5)
    elif i == 2:
        glob(fx, CX, 4.5, 2.5)
        fx.pxs([(CX - 4, 6), (CX + 3, 6), (CX - 3, 4)], "g")
    elif i == 3:
        glob(fx, CX, 1.5, 2.5)
        fx.pxs([(CX - 5, 5), (CX + 4, 5), (CX - 1, 6), (CX, 7)], "g")
    elif i == 4:
        fx.pxs([(CX - 6, 4), (CX + 5, 4), (CX - 1, 7)], "G")
    if fx.im.getbbox():
        fx.outline()
    L["FX"].paste(fx)


def draw_homing(L, tag, i, n):
    eye = "r" if tag == "Attack" and i in (0, 1, 2) else "R"
    if tag == "Idle":
        ant(L, **idle_kw(i))
        eye = "r" if i == 1 else "R"
    else:
        ant(L, head_dy=[1, 1, -1, 0, 0, 0][i], abd=[0.5, 0.8, 0, 0, 0, 0][i])
    b = L["Body"]
    b.pxs([(CX - 3, 9 + (1 if tag == "Attack" and i < 2 else 0) - (1 if tag == "Attack" and i == 2 else 0)),
           (CX + 2, 9 + (1 if tag == "Attack" and i < 2 else 0) - (1 if tag == "Attack" and i == 2 else 0))], eye)
    if tag == "Attack":
        fx = Cv(TW, TW)
        # a little seeker bug curls out of the abdomen and away to the upper right
        path = [None, (CX, 24), (CX + 6, 20), (CX + 10, 12), (CX + 12, 4), None][i]
        if path:
            x, y = path
            fx.blob(x, y, 1.5, 1.5, "r", "f", "R")
            fx.pxs([(x - 2, y - 1), (x + 1, y - 1)], "w")
            fx.outline()
            if i >= 2:
                prev = [(CX, 24), (CX + 6, 20), (CX + 10, 12)][i - 2]
                fx.line([prev, (x - 1, y + 1)], "o", a=150)
        if i in (0, 1):
            L["FX"].ring(CX + 0.5, 9.5, 3 + i * 2, "r", 1, a=200)
        L["FX"].paste(fx)


def draw_mortar(L, tag, i, n):
    if tag == "Idle":
        ant(L, **idle_kw(i))
        L["Body"].blob(CX, 23, 1.5, 1.5, "S", "s", "x")  # loaded pebble on the abdomen
        L["Body"].px(CX - 1, 22, "s")
        return
    abd = [0.3, 0.8, 1.2, 0.0, 0.0, 0.2][i]
    ant(L, abd=abd, bob=[0, 1, 1, 0, 0, 0][i])
    fx = Cv(TW, TW)
    # top-down lob: the shell grows as it climbs towards the camera, then shrinks away
    shell = [(CX, 23, 1.5), (CX, 23, 1.5), (CX, 20, 2.5), (CX + 3, 12, 3.5), (CX + 7, 4, 3), None][i]
    if shell:
        x, y, r = shell
        fx.blob(x, y, r, r, "S", "s", "x")
        fx.outline()
        if i == 2:
            fx.pxs([(x - 2, y + 4), (x + 2, y + 4), (x, y + 5)], "o")
        if i >= 3:
            fx.px(x - 1, y - r + 1, "o")  # fuse spark
    if i in (2, 3):
        L["FX"].ring(CX, 23, 4 + (i - 2) * 2, "s", 1, a=170)  # puff ring
    L["FX"].paste(fx)


def draw_ricochet(L, tag, i, n):
    held = True
    if tag == "Idle":
        ant(L, **idle_kw(i))
        hx = 0
    else:
        hx = [-1, -2, 1, 2, 1, 0][i]
        ant(L, head_dx=hx, head_dy=[0, 1, -1, -1, 0, 0][i], mand=[1, 1, 1, 0, 0, 0][i])
        held = i not in (3, 4)
    fx = Cv(TW, TW)
    if held:
        fx.blob(CX + hx + (0 if tag == "Idle" or i != 5 else 0), 5.5, 1.5, 1.5, "s", "w", "S")
    else:
        x, y = (CX + 5, 2) if i == 3 else (CX + 9, -1)
        fx.blob(x, y + 1.5, 1.5, 1.5, "s", "w", "S")
        fx.line([(CX + 1, 6), (x - 1, y + 3)], "w", a=160)
    if fx.im.getbbox():
        fx.outline()
    if tag == "Attack" and i == 2:  # whoosh arcs
        fx.line([(CX - 6, 6), (CX - 4, 3), (CX - 1, 2)], "c", a=180)
    L["FX"].paste(fx)


def draw_chain(L, tag, i, n):
    tips = ((CX - 6, 1), (CX + 5, 1))
    if tag == "Idle":
        ant(L, ant_tips=tips, **idle_kw(i))
        fx = L["FX"]
        for k, (x, y) in enumerate(tips):
            if (i + k) % 2 == 0:
                fx.spr(SPARK_S, x - 1, y - 1)
            else:
                fx.px(x, y, "y")
        return
    ant(L, ant_tips=((CX - 7, 1), (CX + 6, 1)) if i in (1, 2, 3) else tips,
        bob=[1, 0, 0, 0, 0, 0][i], abd=[0, 0.4, 0.4, 0.2, 0, 0][i])
    fx = L["FX"]
    lit = [0, 2, 3, 3, 1, 0][i]
    for x, y in ((CX - 7, 1), (CX + 6, 1)) if i in (1, 2, 3) else tips:
        if lit >= 2:
            fx.spr(SPARK, x - 2, y - 2)
        elif lit:
            fx.spr(SPARK_S, x - 1, y - 1)
    if i in (2, 3):  # bolt arcs across the antennae and up off the top edge
        rng = random.Random(i)
        pts = [(CX - 7, 1)]
        for k in range(1, 6):
            pts.append((CX - 7 + k * 13 / 6, 1 + rng.choice((-1, 0, 1))))
        pts.append((CX + 6, 1))
        fx.line(pts, "w")
        mid = [(CX, 1), (CX - 2, -2), (CX + 1, -4)]
        fx.line(mid, "y")
        fx.line([(p[0], p[1] + 1) for p in pts], "y", a=200 if i == 2 else 120)


def draw_beam(L, tag, i, n):
    tips = ((CX - 5, 6), (CX + 4, 6))
    if tag == "Idle":
        ant(L, ant_tips=tips, **idle_kw(i))
        glow = 0
    else:
        ant(L, ant_tips=tips, bob=[0, 0, 1, 1, 0, 0][i])
        glow = [1, 2, 3, 3, 2, 0][i]
    fx = Cv(TW, TW)
    drop_y = 4.5 + (0.5 if tag == "Idle" and i in (1, 2) else 0)
    inner = ["b", "b", "w", "w"][glow]
    fx.blob(CX, drop_y, 3.5, 3.5, inner, "w", "B")
    fx.px(CX - 2, drop_y - 2, "w")
    fx.outline()
    if glow >= 2:
        width = {2: 1, 3: 2}[glow]
        for dx in range(-width, width):
            fx.line([(CX + dx, -1), (CX + dx, 0)], "w")
        L["FX"].ring(CX, drop_y, 5 + (i % 2), "b", 1, a=170)
    L["FX"].paste(fx)


def draw_orbit(L, tag, i, n):
    # the Swarm Nest is a nest cone with tiny ants circling it, not a single ant
    nest = Cv(TW, TW)
    nest.blob(CX, 17, 8.5, 7, "d", "T", "D")
    nest.blob(CX, 16, 5, 4, "D", "d", "e")
    nest.outline()
    nest.blob(CX, 15.5, 2, 1.5, "k")
    L["Body"].paste(nest)
    radius = 11
    if tag == "Attack":
        radius = [11, 13, 14.5, 14, 12.5, 11][i]
        if i in (1, 2):
            L["FX"].ring(CX, 18, radius + 1.5, "c", 1, a=110, ry=(radius + 1.5) * 0.7)
    step = 120 / n
    for k in range(3):
        ang = math.radians(k * 120 + i * step + (90 if tag == "Attack" else 0))
        x = CX + math.cos(ang) * radius
        y = 17.5 + math.sin(ang) * radius * 0.72
        tx, ty = -math.sin(ang), math.cos(ang) * 0.72  # tangent: direction of travel
        tiny = Cv(TW, TW)
        tiny.blob(x - tx * 1.2, y - ty * 1.2, 1.3, 1.3, "a", "A")   # gaster
        tiny.px(x + tx * 1.6, y + ty * 1.6, "A")                    # head
        tiny.outline()
        L["FX"].paste(tiny)
    if tag == "Idle" and i % 2 == 0:
        L["FX"].px(CX, 13, "c")


def draw_frost(L, tag, i, n):
    col = ("N", "n2", "Nz")
    if tag == "Idle":
        ant(L, col=col, **idle_kw(i))
        fx = L["FX"]
        for k, (x, y) in enumerate(((7, 6), (24, 9), (9, 25), (23, 24))):
            if (k + i) % 4 == 0:
                fx.spr(FLAKE_S, x - 1, y - 1)
            elif (k + i) % 4 == 1:
                fx.px(x, y, "b")
        L["Body"].pxs([(CX - 1, 21), (CX, 20), (CX + 1, 23)], "b")  # frost on the gaster
        return
    ant(L, col=col, bob=[1, 1, 0, 0, 0, 0][i], abd=[0.5, 0.8, 0, 0, 0, 0][i])
    L["Body"].pxs([(CX - 1, 21), (CX, 20), (CX + 1, 23)], "w" if i in (1, 2) else "b")
    fx = L["FX"]
    if i >= 1:
        r = [0, 4, 8, 11, 13.5, 15][i]
        fx.ring(CX, 17, r, "w", 1, a=255 - i * 30)
        fx.ring(CX, 17, r - 1, "b", 1, a=200 - i * 30)
        for k in range(6):
            ang = math.radians(k * 60 + 30)
            x, y = CX + math.cos(ang) * r, 17 + math.sin(ang) * r
            if i >= 3:
                fx.spr(FLAKE_S, x - 1.5, y - 1.5)


def draw_knockback(L, tag, i, n):
    col = ("q", "Q", "Z")
    if tag == "Idle":
        ant(L, col=col, head="brute", **idle_kw(i))
        return
    lift = [1, 2, 0, 0, 0, 0][i]
    ant(L, col=col, head="brute", lift=lift, bob=[0, 0, 1, 1, 0, 0][i], legs=[0, 1, -1, 0, 0, 0][i],
        mand=[1, 1, 0, 0, 0, 0][i])
    fx = L["FX"]
    if i >= 2:
        r = [0, 0, 6, 10, 13, 15.5][i]
        fx.ring(CX, 18, r, "c", 2 if i < 4 else 1, a=255 - (i - 2) * 50, ry=r * 0.75)
        rng = random.Random(i)
        for k in range(8):
            ang = math.radians(k * 45 + rng.randint(-10, 10))
            x, y = CX + math.cos(ang) * (r + 1), 18 + math.sin(ang) * (r + 1) * 0.75
            fx.blob(x, y, 1.2, 1, "s", a=230 - (i - 2) * 50)


def draw_mine_layer(L, tag, i, n):
    def mine(fx, x, y, light):
        m = Cv(TW, TW)
        m.blob(x, y, 2.5, 2.5, "h", "H", "hz")
        m.outline()
        m.px(x - 0.5, y - 0.5, light)
        fx.paste(m)

    if tag == "Idle":
        ant(L, **idle_kw(i))
        mine(L["FX"], 7, 25, "r" if i % 2 == 0 else "R")   # stockpile beside the hill
        mine(L["FX"], 10, 27, "R")
        return
    ant(L, bob=[0, 1, 1, 0, 0, 0][i], abd=[0, 0.5, 1, 0.3, 0, 0][i], legs=[0, 1, -1, 1, 0, 0][i])
    mine(L["FX"], 7, 25, "R")
    mine(L["FX"], 10, 27, "R")
    fx = L["FX"]
    pos = [None, (CX, 25), (CX, 28), (CX + 3, 29.5), (CX + 6, 29), (CX + 7, 28.5)][i]
    if pos:
        mine(fx, pos[0], pos[1], "r" if i >= 3 else "R")
    if i in (1, 2):  # dirt flicked up by the digging legs
        fx.pxs([(CX - 7, 10 + i), (CX + 6, 11 - i), (CX - 8, 14), (CX + 7, 15)], "d")


def build_towers():
    return [
        tower_sprite("TowerLinear", "Spitter", draw_linear),
        tower_sprite("TowerHoming", "Seeker", draw_homing),
        tower_sprite("TowerMortar", "Bombardier", draw_mortar, attack_ms=80),
        tower_sprite("TowerRicochet", "Slinger", draw_ricochet),
        tower_sprite("TowerChain", "Storm Ant", draw_chain, attack_ms=60),
        tower_sprite("TowerBeam", "Dewdrop Lens", draw_beam),
        tower_sprite("TowerOrbit", "Swarm Nest", draw_orbit, idle_frames=6, idle_ms=110),
        tower_sprite("TowerFrostAura", "Frost Ant", draw_frost, attack_ms=60),
        tower_sprite("TowerKnockback", "Kicker", draw_knockback, attack_ms=75),
        tower_sprite("TowerMineLayer", "Sapper", draw_mine_layer, attack_ms=90),
    ]


# ================================================================ enemies (face right)
def legs_side(ink, anchors, cy, phase, edge, swing=2):
    """Six legs: hip (x, y) inside the shell, knee just past the shell edge, foot beyond it.
    Mirrored under cy. Tripod gait: front+back on one side move with the middle on the other."""
    s = round(math.sin(phase) * swing)
    for k, (ax, ay, fx) in enumerate(anchors):
        top_off = s if k != 1 else -s
        for off, flip in ((top_off, False), (-top_off, True)):
            span = 1 + (cy - edge) * 0.2
            knee = (ax + fx * 0.4 + off * 0.5, edge - span)
            foot = (ax + fx + off, edge - 2 * span - 0.5 + abs(fx) * 0.25)
            leg = [(ax, ay), knee, foot]
            ink.line(mirror_y(leg, cy) if flip else leg, "k")


def beetle(w, h, cx, cy, Lh, Wh, shell, kind="plain", phase=0.0, mand=0, bob=0):
    """Top-down beetle facing right, centred on pixel row cy - 0.5 (cy is an edge coordinate)."""
    ink = Cv(w, h)
    body = Cv(w, h)
    cx += bob
    anchors = [(cx + Lh - 2, cy - 1, 3.5), (cx + 0, cy - 1, 0.5), (cx - Lh * 0.45, cy - 1, -3.5)]
    legs_side(ink, anchors, cy, phase, cy - Wh)
    hx = cx + Lh + 1.5
    # antennae
    ant_pts = [(hx + 1, cy - 2), (hx + 3, cy - 3), (hx + 4, cy - 4 + (1 if math.sin(phase) > 0 else 0))]
    ink.line(ant_pts, "k")
    ink.line(mirror_y(ant_pts, cy), "k")
    base, hi, lo = shell
    if kind == "stag":
        # two antler jaws that spread out and hook back in, never quite touching
        for flip in (False, True):
            jaw = [(hx + 1, cy - 2), (hx + 3, cy - 4 - mand), (hx + 6, cy - 5 - mand), (hx + 8, cy - 4 - mand)]
            inner = [(hx + 2, cy - 2), (hx + 4, cy - 3 - mand), (hx + 6, cy - 4 - mand)]
            tooth = [(hx + 5, cy - 3 - mand)]
            tip = [(hx + 9, cy - 3 - mand)]
            if flip:
                jaw, inner, tooth, tip = (mirror_y(p, cy) for p in (jaw, inner, tooth, tip))
            body.line(jaw, hi)
            body.line(inner, base)
            body.pxs(tooth + tip, base)
    if kind == "horn":
        body.line([(hx + 1, cy - 1), (hx + 5, cy - 1), (hx + 6, cy - 3)], "c")
        body.line([(hx + 1, cy - 0), (hx + 5, cy - 0)], "s")
    body.blob(hx, cy, 2.6, max(1.8, Wh * 0.5), lo, base)                         # head
    body.blob(cx + Lh - 1.5, cy, Lh * 0.42 + 1, Wh * 0.78, base, hi, lo)            # pronotum
    body.blob(cx - 1, cy, Lh, Wh, base, hi, lo)                                     # elytra
    body.outline()
    # elytra seam and a glint
    seam_y = cy - 0.5
    body.line([(cx - Lh + 1.5, seam_y), (cx + Lh - 3, seam_y)], "k")
    body.px(cx - Lh * 0.4, cy - Wh * 0.55, "w")
    body.line([(cx + Lh - 2.5, cy - Wh * 0.78 + 1), (cx + Lh - 2.5, cy + Wh * 0.78 - 2)], "k")
    if kind == "scarab":  # clypeus rake
        body.pxs([(hx + 2, cy - 2), (hx + 3, cy - 1), (hx + 3, cy), (hx + 2, cy + 1)], "k")
    ink.paste(body)
    return ink


def shadow(w, h, cx, cy, rx, ry):
    sh = Cv(w, h)
    sh.blob(cx, cy, rx, ry, (0, 0, 0), a=80)
    return sh


def hurt_frames(s, base_layers):
    """Two quick frames: white flash, then a red tint knocked one pixel back."""
    out = []
    for k in range(2):
        L = {}
        for name, im in base_layers.items():
            if name == "Body":
                im = tint(im, "w", 0.85) if k == 0 else shift(tint(im, "r", 0.45), -1, 0)
            elif name == "Shadow" and k == 1:
                im = shift(im, -1, 0)
            L[name] = im
        out.append(L)
    return out


def death_frames(s, base_layers, cy, debris=("s", "S"), n=7, splat=None):
    """Flash, then the shell splits along the seam, halves drift apart and fade in a dust puff."""
    body = base_layers["Body"]
    out = []
    rng = random.Random(s.name)
    chips = [(rng.uniform(0, 360), rng.uniform(0.6, 1.2), rng.choice(debris)) for _ in range(8)]
    cx = s.w / 2
    for i in range(n):
        L = {name: Image.new("RGBA", (s.w, s.h), (0, 0, 0, 0)) for name in s.layers}
        if i == 0:
            L["Body"] = tint(body, "w", 0.9)
            L["Shadow"] = base_layers["Shadow"]
        else:
            gap = [0, 0, 1, 2, 3, 4, 5][i]
            top = shift(body, 0, -gap, (0, 0, s.w, int(cy)))
            both = shift(top, 0, gap, (0, int(cy), s.w, s.h))
            if i >= 3:
                both = fade(tint(both, "S", 0.3 + 0.1 * i), [1, 1, 1, 0.8, 0.55, 0.35, 0.15][i])
            L["Body"] = both
            L["Shadow"] = fade(base_layers["Shadow"], max(0.0, 1 - i * 0.2))
            fx = Cv(s.w, s.h)
            if splat and i >= 1:
                fx.blob(cx, cy, 2 + i * 0.6, 1.5 + i * 0.5, splat, a=max(0, 200 - i * 25))
            r = s.w * 0.22 + i * s.w / 16
            for k in range(5 if s.w >= 24 and i >= 2 else 0):  # dust puffs drifting outward
                ang = math.radians(k * 72 + 20)
                pr = max(0.6, 1.8 - i * 0.15) * s.w / 24
                fx.blob(cx + math.cos(ang) * r * 0.9, cy + math.sin(ang) * r * 0.7, pr, pr, "s", "c", "S",
                        a=max(0, 220 - i * 32))
            for ang, spd, c in chips:
                d = r * spd + 1
                fx.px(cx + math.cos(math.radians(ang)) * d, cy + math.sin(math.radians(ang)) * d, c,
                      a=max(0, 255 - i * 30))
            L["FX"] = fx.im
        out.append(L)
    return out


def enemy_beetle(name, caste, size, Lh, Wh, shell, kind, walk_n=6, walk_ms=90, splat=None):
    s = Sprite(name, "Enemies", size, size, ["Shadow", "Body", "FX"], caste)
    cx, cy = size / 2, size / 2 + 0.5
    walk = []
    for i in range(walk_n):
        phase = 2 * math.pi * i / walk_n
        L = s.new()
        L["Shadow"].paste(shadow(size, size, cx, cy + 1, Lh + 3, Wh + 1))
        bob = 1 if i % (walk_n // 2) == walk_n // 4 else 0
        L["Body"].paste(beetle(size, size, cx - 1, cy, Lh, Wh, shell, kind, phase,
                               mand=1 if kind == "stag" and i % walk_n < walk_n // 2 else 0, bob=0))
        walk.append(flat(L))
    s.tag("Walk", walk, walk_ms)
    s.tag("Hurt", hurt_frames(s, walk[0]), 60, repeat=1)
    s.tag("Death", death_frames(s, walk[0], cy, splat=splat), 80, repeat=1)
    return s, walk


def build_enemies():
    out = []
    # Runner: scarab, small and quick
    runner, _ = enemy_beetle("EnemyRunner", "Scarab", 24, 5, 4, ("i", "I", "iz"), "scarab", walk_n=6, walk_ms=60)
    out.append(runner)
    # Grunt: plain beetle
    grunt, _ = enemy_beetle("EnemyGrunt", "Beetle", 24, 6, 5, ("h", "H", "hz"), "plain", walk_n=6, walk_ms=90)
    out.append(grunt)
    # Brute: stag beetle, big and slow
    brute, _ = enemy_beetle("EnemyBrute", "Stag Beetle", 40, 10, 8, ("q", "Q", "Z"), "stag", walk_n=8, walk_ms=120)
    out.append(brute)
    out.append(enemy_shielded())
    out.append(enemy_splitter())
    out.append(enemy_swarm())
    out.append(enemy_healer())
    return out


# ---- Shielded: rhino beetle inside an energy bubble -------------------------------------------
def bubble(cv, cx, cy, r, i, flash=0.0, alpha=1.0):
    fill = mix("b", "w", flash)
    cv.disc(cx, cy, r, fill, a=int((50 + 120 * flash) * alpha))
    cv.ring(cx, cy, r, "b", 1, a=int(220 * alpha))
    cv.ring(cx, cy, r - 1, "w", 1, a=int(70 * alpha))
    # a highlight arc that slides round the bubble as it walks
    a0 = (200 + i * 15) % 360
    cv.ring(cx, cy, r - 2, "w", 1, a=int(230 * alpha),
            gaps=lambda d: not (a0 <= d <= a0 + 50 or a0 <= d + 360 <= a0 + 50))


def enemy_shielded():
    size = 32
    s = Sprite("EnemyShielded", "Enemies", size, size, ["Shadow", "Body", "Shield", "FX"], "Rhino Beetle")
    cx, cy = 16, 16.5
    shell = ("n", "B", "hz")
    walk_n = 6

    def body_frame(i, shield=True, flash=0.0):
        L = s.new()
        L["Shadow"].paste(shadow(size, size, cx, cy + 1, 10, 7))
        L["Body"].paste(beetle(size, size, cx - 2, cy, 7, 5.5, shell, "horn", 2 * math.pi * i / walk_n))
        if shield:
            bubble(L["Shield"], cx, cy, 13.5, i, flash)
        return L

    s.tag("Walk", [flat(body_frame(i)) for i in range(walk_n)], 100)
    hit = []
    for k in range(3):
        L = body_frame(0, flash=[0.9, 0.5, 0.15][k])
        L["FX"].ring(cx, cy, [10, 12, 14][k], "w", 1, a=[255, 160, 80][k])
        hit.append(flat(L))
    s.tag("ShieldHit", hit, 60, repeat=1)
    brk = []
    rng = random.Random(7)
    shards = [(rng.uniform(0, 360), rng.uniform(0.8, 1.3)) for _ in range(12)]
    for k in range(6):
        L = body_frame(0, shield=False)
        if k == 0:
            bubble(L["Shield"], cx, cy, 13.5, 0, 0.8)
        if k <= 1:  # cracks
            for ang in (30, 140, 250, 320):
                a = math.radians(ang)
                L["Shield"].line([(cx + math.cos(a) * 4, cy + math.sin(a) * 4),
                                  (cx + math.cos(a + 0.3) * 9, cy + math.sin(a + 0.3) * 9),
                                  (cx + math.cos(a) * 13.5, cy + math.sin(a) * 13.5)], "w")
            if k == 1:
                bubble(L["Shield"], cx, cy, 13.5, 0, 0.3, alpha=0.6)
        if k >= 1:
            for ang, spd in shards:
                d = 12 + k * 2.5 * spd
                x, y = cx + math.cos(math.radians(ang)) * d, cy + math.sin(math.radians(ang)) * d
                a = max(0, 255 - k * 45)
                L["FX"].px(x, y, "b", a)
                L["FX"].px(x + 1, y, "w", a)
                L["FX"].px(x, y + 1, "B", a)
        brk.append(flat(L))
    s.tag("ShieldBreak", brk, 70, repeat=1)
    bare = [flat(body_frame(i, shield=False)) for i in range(walk_n)]
    s.tag("WalkBare", bare, 100)
    s.tag("Hurt", hurt_frames(s, bare[0]), 60, repeat=1)
    s.tag("Death", death_frames(s, bare[0], cy), 80, repeat=1)
    return s


# ---- Splitter: centipede that bursts into swarm hatchlings -----------------------------------
def centipede(w, h, cx, cy, segs, spacing, phase, colors=("L", "l", "Lz"), seg_r=(2.0, 2.6),
              amp=1.0, offsets=None, glow=None):
    """Segmented crawler facing right. offsets: per-segment (dx, dy) used by the death burst."""
    ink = Cv(w, h)
    body = Cv(w, h)
    base, hi, lo = colors
    pts = []
    for k in range(segs):
        x = cx + (segs - 1) * spacing / 2 - k * spacing
        y = cy + amp * math.sin(phase - k * 0.9)
        if offsets:
            x, y = x + offsets[k][0], y + offsets[k][1]
        pts.append((x, y))
    for k, (x, y) in enumerate(pts):
        swing = round(math.sin(phase - k * 1.4) * 1.2)
        ink.line([(x - 0.5, y - seg_r[1] + 0.5), (x - 0.5 + swing, y - seg_r[1] - 1.5)], "k")
        ink.line([(x - 0.5, y + seg_r[1] - 0.5), (x - 0.5 - swing, y + seg_r[1] + 1.5)], "k")
    hx, hy = pts[0]
    for side in (-1, 1):  # antennae and forcipules
        ink.line([(hx + 1, hy + side * 1), (hx + 3, hy + side * 3), (hx + 5, hy + side * 3)], "k")
    tx, ty = pts[-1]
    for side in (-1, 1):
        ink.line([(tx - 1, ty + side), (tx - 3, ty + side * 2)], "k")
    for k, (x, y) in reversed(list(enumerate(pts))):
        rx, ry = (seg_r[0] + 0.4, seg_r[1] - 0.2) if k == 0 else seg_r
        body.blob(x, y, rx, ry, glow if glow and k in (1, 3, 5) else base, hi, lo)
    body.outline()
    body.pxs([(hx + 1, hy - 1.5), (hx + 1, hy + 0.5)], "y")
    ink.paste(body)
    return ink


def enemy_splitter():
    size = 32
    s = Sprite("EnemySplitter", "Enemies", size, size, ["Shadow", "Body", "FX"], "Centipede")
    cx, cy = 16, 16.5
    walk_n = 6
    walk = []
    for i in range(walk_n):
        L = s.new()
        L["Shadow"].paste(shadow(size, size, cx, cy + 1, 13, 4))
        L["Body"].paste(centipede(size, size, cx, cy, 6, 3.4, 2 * math.pi * i / walk_n))
        walk.append(flat(L))
    s.tag("Walk", walk, 80)
    s.tag("Hurt", hurt_frames(s, walk[0]), 60, repeat=1)
    # death: the glowing brood segments swell, the body bursts, three hatchlings scatter
    death = []
    rng = random.Random(3)
    dirs = [(rng.uniform(-1, 1), rng.uniform(-1.2, 1.2)) for _ in range(6)]
    for k in range(7):
        L = s.new()
        if k < 3:
            L["Shadow"].paste(shadow(size, size, cx, cy + 1, 13, 4))
            glow = ["l", "p", "w"][k]
            offs = [(0, 0)] * 6 if k < 2 else [(dx, dy) for dx, dy in dirs]
            L["Body"].paste(centipede(size, size, cx, cy, 6, 3.4, 0, offsets=offs, glow=glow))
        else:
            spread = (k - 2) * 2.2
            for j, (dx, dy) in enumerate(dirs):
                x = cx + 8.5 - j * 3.4 + dx * spread
                y = cy + dy * spread * 1.6
                if j in (1, 3, 5):  # hatchlings live on
                    h = Cv(size, size)
                    h.blob(x, y, 2, 1.6, "l", "p", "L")
                    h.outline()
                    L["Body"].paste(h)
                else:  # husk bits fade
                    L["Body"].px(x, y, "Lz", max(0, 255 - (k - 2) * 60))
            if k <= 4:
                L["FX"].ring(cx, cy, 4 + (k - 2) * 2.5, "p", 1, a=200 - (k - 3) * 80)
        if k == 2:
            L["FX"].ring(cx, cy, 3, "w", 1, a=200)
        death.append(flat(L))
    s.tag("Death", death, 80, repeat=1)
    return s


def enemy_swarm():
    size = 16
    s = Sprite("EnemySwarm", "Enemies", size, size, ["Shadow", "Body", "FX"], "Centipede Hatchling")
    cx, cy = 8, 8.5
    walk_n = 4
    walk = []
    for i in range(walk_n):
        L = s.new()
        L["Shadow"].paste(shadow(size, size, cx, cy + 1, 6, 3))
        L["Body"].paste(centipede(size, size, cx - 0.5, cy, 3, 2.6, 2 * math.pi * i / walk_n,
                                  colors=("l", "p", "L"), seg_r=(1.4, 1.8), amp=0.6))
        walk.append(flat(L))
    s.tag("Walk", walk, 60)
    s.tag("Hurt", hurt_frames(s, walk[0]), 60, repeat=1)
    s.tag("Death", death_frames(s, walk[0], cy, debris=("p", "L"), n=5, splat="L"), 70, repeat=1)
    return s


# ---- Healer: mantis that pulses heals to nearby bugs -----------------------------------------
def mantis(w, h, cx, cy, phase, raise_arms=0.0, glow=0.0):
    ink = Cv(w, h)
    body = Cv(w, h)
    s = math.sin(phase)
    # mid and hind legs: long, thin, green
    for k, (ax, fx, reach) in enumerate(((cx + 1, 3, 7), (cx - 3, -4, 8))):
        off = round(s * 1.5) * (1 if k == 0 else -1)
        top = [(ax, cy - 1), (ax + fx * 0.5, cy - reach * 0.8), (ax + fx + off, cy - reach)]
        ink.line(top, "G")
        ink.line(mirror_y([(x, y) for x, y in top[:2]] + [(ax + fx - off, cy - reach)], cy), "G")
    # raptorial forelegs: folded forward, or raised and spread while healing
    for side in (-1, 1):
        r = raise_arms
        pts = [(cx + 4, cy + side * 1), (cx + 6 - r * 1, cy + side * (3 + r * 3)),
               (cx + 9 - r * 2, cy + side * (2 + r * 4))]
        if side < 0:
            pts = [(x, y - 1) for x, y in pts]
        ink.line(pts, "g")
        ink.px(pts[1][0] + 1, pts[1][1], "j")
    body.blob(cx - 5, cy, 6.5, 3.2, "M", "g", "j")          # abdomen
    body.blob(cx - 4, cy, 5, 2.4, "m", "g", "M")            # folded wings
    body.blob(cx + 3.5, cy, 4.5, 1.3, "M", "m", "j")        # long prothorax
    body.blob(cx + 9, cy, 1.8, 2.6, "m", "g", "M")          # head
    body.outline()
    body.line([(cx - 9, cy - 0.5), (cx + 0, cy - 0.5)], "M")  # wing seam
    eye = mix("y", "w", glow)
    body.px(cx + 9, cy - 2.5, eye)
    body.px(cx + 9, cy + 1.5, eye)
    ink.paste(body)
    return ink


def enemy_healer():
    size = 32
    s = Sprite("EnemyHealer", "Enemies", size, size, ["Shadow", "Body", "FX"], "Mantis")
    cx, cy = 15, 16.5
    walk_n = 6
    walk = []
    for i in range(walk_n):
        L = s.new()
        L["Shadow"].paste(shadow(size, size, cx, cy + 1, 12, 5))
        L["Body"].paste(mantis(size, size, cx, cy, 2 * math.pi * i / walk_n))
        walk.append(flat(L))
    s.tag("Walk", walk, 100)
    heal = []
    for k in range(6):
        L = s.new()
        L["Shadow"].paste(shadow(size, size, cx, cy + 1, 12, 5))
        up = [0.4, 1.0, 1.0, 1.0, 0.6, 0.2][k]
        g = [0.2, 0.7, 1.0, 0.7, 0.3, 0.0][k]
        L["Body"].paste(mantis(size, size, cx, cy, 0, up, g))
        if g:
            L["Body"] = Cv(size, size).paste(tint(L["Body"].im, "g", g * 0.25))
        fx = L["FX"]
        if 1 <= k <= 4:
            fx.ring(cx + 2, cy, 5 + k * 2.5, "g", 1, a=230 - k * 40)
        for j, (px_, py_) in enumerate(((cx + 6, cy - 9), (cx - 4, cy - 7), (cx + 1, cy + 8), (cx - 8, cy + 6))):
            if (k + j) % 3 != 2 and k >= 1:
                fx.spr(PLUS, px_ - 1, py_ - 1 - k)
        heal.append(flat(L))
    s.tag("Heal", heal, 80, repeat=1)
    s.tag("Hurt", hurt_frames(s, walk[0]), 60, repeat=1)
    s.tag("Death", death_frames(s, walk[0], cy, debris=("g", "M"), splat="G"), 80, repeat=1)
    return s


# ================================================================ effects
def fx_sprite(name, size, caste=""):
    return Sprite(name, "Fx", size, size, ["FX"], caste)


def build_fx():
    out = []

    # Mine (dropped by the Sapper): blinks while armed, then explodes
    s = fx_sprite("FxMine", 32, "Mine")
    armed = []
    for k in range(2):
        cv = Cv(32, 32)
        cv.blob(16, 18, 4.5, 2.5, (0, 0, 0), a=80)
        m = Cv(32, 32)
        m.blob(16, 16, 4, 4, "h", "H", "hz")
        for x, y in ((11, 15), (20, 15), (15, 11), (15, 20)):
            m.px(x, y, "S")
        m.outline()
        m.blob(16, 16, 1.2, 1.2, "r" if k == 0 else "R")
        if k == 0:
            m.ring(16, 16, 3, "r", 1, a=90)
        cv.paste(m)
        armed.append({"FX": cv.im})
    s.tag("Armed", armed, 300)
    boom = []
    rng = random.Random(11)
    debris = [(rng.uniform(0, 360), rng.uniform(0.7, 1.3), rng.choice("SsDy")) for _ in range(14)]
    for k in range(7):
        cv = Cv(32, 32)
        if k == 0:
            cv.disc(16, 16, 6, "w")
            cv.ring(16, 16, 7, "y", 1)
        else:
            r = [0, 9, 12, 13, 13.5, 14, 14][k]
            smoke = max(0, 230 - k * 30)
            # smoke ring outside, fire inside that burns down
            cv.disc(16, 16 - k * 0.5, r, "S", a=smoke // 2)
            cv.ring(16, 16 - k * 0.5, r, "x", 1, a=smoke)
            fire = [0, 8, 7, 5, 3, 1.5, 0][k]
            if fire:
                cv.blob(16, 16, fire, fire, "o", "y", "r")
                cv.disc(16, 15.5, fire * 0.45, "y")
                if k <= 2:
                    cv.disc(16, 15.5, fire * 0.2, "w")
            for ang, spd, c in debris:
                d = r * spd
                cv.px(16 + math.cos(math.radians(ang)) * d, 16 + math.sin(math.radians(ang)) * d, c,
                      a=max(0, 255 - k * 35))
        boom.append({"FX": cv.im})
    s.tag("Explode", boom, 70, repeat=1)
    out.append(s)

    # Frost Aura pulse, drawn at real size: the last frame's ring is 112 px out, the Frost
    # Ant's 3.5-unit radius at 32 px per unit. Upgrades only stretch it by ~15%.
    s = fx_sprite("FxFrostPulse", 232, "Frost Ant pulse")
    frames = []
    c0 = 116
    for k in range(8):
        cv = Cv(232, 232)
        r = 16 + k * (96 / 7)
        a = max(0, 255 - k * 28)
        cv.ring(c0, c0, r, "w", 2, a=a)
        cv.ring(c0, c0, r - 2, "b", 2, a=a * 2 // 3)
        cv.ring(c0, c0, r - 5, "B", 1, a=a // 3)
        for j in range(16):
            ang = math.radians(j * 22.5 + k * 3)
            x, y = c0 + math.cos(ang) * r, c0 + math.sin(ang) * r
            if a > 40:
                cv.spr(FLAKE if j % 2 == 0 else FLAKE_S, x - 2, y - 2)
        frames.append({"FX": cv.im})
    s.tag("Pulse", frames, 60, repeat=1)
    out.append(s)

    # Kicker stomp shockwave, real size: ends 96 px out (3 units)
    s = fx_sprite("FxShockwave", 208, "Kicker stomp")
    frames = []
    rng = random.Random(5)
    puffs = [(rng.uniform(0, 360), rng.uniform(0.94, 1.06)) for _ in range(28)]
    c0 = 104
    for k in range(7):
        cv = Cv(208, 208)
        r = 18 + k * 13
        a = max(0, 255 - k * 34)
        cv.ring(c0, c0, r, "c", max(1, 3 - k // 2), a=a)
        cv.ring(c0, c0, r - max(1, 3 - k // 2), "t", 1, a=a // 2)
        for ang, d in puffs:
            x, y = c0 + math.cos(math.radians(ang)) * r * d, c0 + math.sin(math.radians(ang)) * r * d
            cv.blob(x, y, 2.4 - k * 0.2, 2.4 - k * 0.2, "s", "c", "S", a=a)
        frames.append({"FX": cv.im})
    s.tag("Blast", frames, 60, repeat=1)
    out.append(s)

    # Mantis heal pulse, real size: ends 90 px out (the healer's 2.8-unit radius)
    s = fx_sprite("FxHealPulse", 192, "Mantis heal")
    frames = []
    c0 = 96
    for k in range(7):
        cv = Cv(192, 192)
        r = 12 + k * 13
        a = max(0, 230 - k * 32)
        cv.ring(c0, c0, r, "g", 2 if k < 3 else 1, a=a)
        cv.ring(c0, c0, r - 3, "I", 1, a=a // 2)
        for j in range(10):
            ang = math.radians(j * 36 + 18)
            x, y = c0 + math.cos(ang) * r * 0.8, c0 + math.sin(ang) * r * 0.8 - k
            if 1 <= k <= 5:
                cv.spr(PLUS_L if j % 2 == 0 else PLUS, x - 2, y - 2)
        frames.append({"FX": cv.im})
    s.tag("Pulse", frames, 70, repeat=1)
    out.append(s)

    # Swarm Nest orbiter: a soldier ant running its circuit, facing right
    s = fx_sprite("FxOrbiter", 16, "Swarm Nest ant")
    frames = []
    for k in range(4):
        ink = Cv(16, 16)
        body = Cv(16, 16)
        cx, cy = 8, 8.5
        sw = (1, 0, -1, 0)[k]
        for j, (ax, fx_) in enumerate(((cx + 1, 2), (cx, 0), (cx - 1, -2))):
            off = sw if j != 1 else -sw
            leg = [(ax, cy - 1), (ax + fx_ * 0.5, cy - 3), (ax + fx_ + off, cy - 4.5)]
            other = [(ax, cy - 1), (ax + fx_ * 0.5, cy - 3), (ax + fx_ - off, cy - 4.5)]
            ink.line(leg, "k")
            ink.line(mirror_y(other, cy), "k")
        for side in (-1, 1):
            ink.line([(cx + 5, cy + side - 0.5), (cx + 7, cy + side * 3 - 0.5)], "k")
        body.blob(cx - 3.5, cy, 2.6, 2.2, "q", "Q", "Z")     # gaster
        body.blob(cx, cy, 1.4, 1.3, "q", "Q", "Z")           # thorax
        body.blob(cx + 3.2, cy, 2.0, 2.0, "q", "Q", "Z")     # head
        body.outline()
        body.px(cx + 4, cy - 1.5, "c")
        ink.paste(body)
        frames.append({"FX": ink.im})
    s.tag("Fly", frames, 70)
    out.append(s)
    return out



# ================================================================ projectiles (face right, they rotate)
def proj_sprite(name, size=16, caste=""):
    return Sprite(name, "Projectiles", size, size, ["FX"], caste)


def build_projectiles():
    out = []
    cx, cy = 8, 8.5

    # Spitter's acid glob: wobbling blob with droplets trailing behind
    s = proj_sprite("ProjLinear", caste="Spitter acid glob")
    frames = []
    for k in range(4):
        cv = Cv(16, 16)
        rx, ry = (2.8, 2.2) if k % 2 == 0 else (2.3, 2.6)
        cv.blob(cx + 1, cy, rx, ry, "g", "w", "G")
        cv.outline()
        for j, (dx, dy) in enumerate(((-4, 0), (-6, -1 if k % 2 else 1), (-7, 0))):
            if (j + k) % 3 != 2:
                cv.px(cx + dx, cy + dy, "g" if j == 0 else "G")
        frames.append({"FX": cv.im})
    s.tag("Fly", frames, 60)
    out.append(s)

    # Seeker bug: red body, flapping wings, ember trail
    s = proj_sprite("ProjHoming", caste="Seeker bug")
    frames = []
    for k in range(2):
        cv = Cv(16, 16)
        cv.blob(cx, cy, 2.4, 1.8, "r", "f", "R")
        cv.px(cx + 2.5, cy - 0.5, "R")
        cv.outline()
        wy = (-3, 2) if k == 0 else (-2, 1)
        cv.pxs([(cx - 1, cy + wy[0]), (cx, cy + wy[0]), (cx - 1, cy + wy[1] + 1), (cx, cy + wy[1] + 1)], "w")
        cv.pxs([(cx - 4, cy - 0.5), (cx - 6 + k, cy - 0.5)], "o")
        frames.append({"FX": cv.im})
    s.tag("Fly", frames, 50)
    out.append(s)

    # Bombardier shell: tumbling stone with a fizzing fuse
    s = proj_sprite("ProjMortar", caste="Bombardier shell")
    frames = []
    for k in range(4):
        cv = Cv(16, 16)
        cv.blob(cx, cy, 3.2, 3.2, "S", "s", "x")
        cv.outline()
        ang = math.radians(k * 90 + 225)
        cv.px(cx + math.cos(ang) * 1.6 - 0.5, cy + math.sin(ang) * 1.6 - 0.5, "w")
        cv.px(cx - 4, cy - 4, "y" if k % 2 == 0 else "o")
        cv.px(cx - 5 + (k % 2), cy - 5, "o", a=180)
        frames.append({"FX": cv.im})
    s.tag("Fly", frames, 80)
    out.append(s)

    # Slinger pebble: spinning pebble with a motion streak
    s = proj_sprite("ProjRicochet", caste="Slinger pebble")
    frames = []
    for k in range(4):
        cv = Cv(16, 16)
        cv.blob(cx, cy, 2.2, 2.2, "s", "c", "S")
        cv.outline()
        ang = math.radians(k * 90)
        cv.px(cx + math.cos(ang) * 1.2 - 0.5, cy + math.sin(ang) * 1.2 - 0.5, "w")
        cv.line([(cx - 6, cy - 0.5), (cx - 4, cy - 0.5)], "c", a=150)
        frames.append({"FX": cv.im})
    s.tag("Fly", frames, 50)
    out.append(s)

    # Cluster fragment: flickering ember
    s = proj_sprite("ProjFragment", caste="Shell fragment")
    frames = []
    for k in range(2):
        cv = Cv(16, 16)
        cv.blob(cx, cy, 1.6, 1.6, "o" if k == 0 else "y", "w", "r")
        cv.outline()
        cv.px(cx - 3, cy - 0.5, "o", a=200)
        frames.append({"FX": cv.im})
    s.tag("Fly", frames, 50)
    out.append(s)

    # Mortar fire puddle (GroundHazard): 64 px, scaled to the hazard radius in the builder
    s = proj_sprite("FxHazard", 64, "Fire puddle")
    frames = []
    rng = random.Random(9)
    licks = [(rng.uniform(0, 360), rng.uniform(4, 24), rng.uniform(1.5, 3.2)) for _ in range(22)]
    for k in range(4):
        cv = Cv(64, 64)
        cv.disc(32, 32, 29, "R", a=110)
        cv.disc(32, 32, 24, "r", a=90)
        cv.ring(32, 32, 29, "o", 1, a=150, gaps=lambda d, k=k: int(d / 30 + k) % 3 == 0)
        for j, (ang, dist, r) in enumerate(licks):
            if (j + k) % 4 == 0:
                continue
            a = math.radians(ang + k * 7)
            x, y = 32 + math.cos(a) * dist, 32 + math.sin(a) * dist
            rr = r * (0.75 + 0.25 * ((j + k) % 2))
            cv.blob(x, y, rr, rr, "o", "y", "r", a=230)
            if (j + k) % 3 == 0:
                cv.px(x, y - rr, "y")
        frames.append({"FX": cv.im})
    s.tag("Burn", frames, 100)
    out.append(s)
    return out


# ================================================================ status icons (float over enemies)
def build_status():
    s = Sprite("FxStatus", "Fx", 12, 12, ["FX"], "Status icons")
    flame = ["...o....", "..oo....", "..oyo.o.", ".oyyoo..", ".oywyo..", "oyywyyo.", "oyyyyyo.", ".oyyyo.."]
    drop = ["...g...", "..ggg..", "..ggg..", ".ggwgg.", ".gwggg.", ".ggggg.", "..ggg.."]
    bolt = ["...yy.", "..yy..", ".yyy..", "yywyy.", "..yyy.", "..yy..", ".yy...", ".y...."]
    plus = ["..gg..", "..gg..", "gggggg", "ggwggg", "..gg..", "..gg.."]
    shield = [".bbbbb.", "bbwbbbb", "bwbbbbb", "bbbbbBb", ".bbbBb.", "..bBb..", "...b..."]
    for tag, draw in (("Slow", "flake"), ("Burn", "flame"), ("Poison", "drop"),
                      ("Shock", "bolt"), ("Regen", "plus"), ("Shield", "shield")):
        frames = []
        for k in range(4):
            cv = Cv(12, 12)
            bob = (0, -1, -1, 0)[k]
            if draw == "flake":
                cv.spr(SNOW, 2.5, 2.5 + bob)
            elif draw == "flame":
                rows = flame if k % 2 == 0 else [r[::-1] for r in flame]
                cv.spr(rows, 2, 2 + bob)
            elif draw == "bolt":
                cv.spr(bolt if k % 2 == 0 else [r[::-1] for r in bolt], 3, 2)
                if k % 2:
                    cv.px(1, 3, "w")
                    cv.px(10, 8, "w")
            elif draw == "plus":
                cv.spr(plus, 3, 3 + bob)
                if k >= 2:
                    cv.px(10, 2 + (3 - k), "g")
            elif draw == "shield":
                cv.spr(shield, 2.5, 2.5 + bob)
            else:
                cv.spr(drop, 2.5, 3 + bob)
                if k >= 2:
                    cv.px(9, 2 + (3 - k), "g")
            cv.outline()
            frames.append({"FX": cv.im})
        s.tag(tag, frames, 120)
    return [s]



# ================================================================ impacts, props, tiers, build/sell
def burst(cv, cx, cy, k, n, colors, rays=8, reach=9.0, seed=0, core=None):
    """One frame of a radial burst: a core that shrinks while short streaks fly outward."""
    t = k / max(1, n - 1)
    rng = random.Random(seed)
    if core and t < 0.6:
        r = core * (1 - t)
        cv.blob(cx, cy, r, r, colors[0], "w", colors[-1])
    for j in range(rays):
        ang = math.radians(j * 360 / rays + rng.uniform(-14, 14))
        d0 = 1.5 + reach * t * rng.uniform(0.7, 1.0)
        length = max(0.5, 2.5 * (1 - t))
        a = int(255 * (1 - t * 0.7))
        cv.line([(cx + math.cos(ang) * d0, cy + math.sin(ang) * d0),
                 (cx + math.cos(ang) * (d0 + length), cy + math.sin(ang) * (d0 + length))],
                colors[j % len(colors)], a=a)


def build_impacts():
    out = []
    s = Sprite("FxImpact", "Fx", 24, 24, ["FX"], "Hit effects")
    c0 = 12

    def frames_for(n, draw):
        fr = []
        for k in range(n):
            cv = Cv(24, 24)
            draw(cv, k, n)
            fr.append({"FX": cv.im})
        return fr

    # Spitter acid: a green splat that spreads and thins out
    def acid(cv, k, n):
        burst(cv, c0, c0, k, n, ["g", "G", "m"], rays=7, reach=7, seed=1, core=3.5)
        if k >= 2:
            for j in range(5):
                ang = math.radians(j * 72 + 20)
                cv.px(c0 + math.cos(ang) * (3 + k), c0 + math.sin(ang) * (3 + k) * 0.8, "G", a=255 - k * 40)
    s.tag("Acid", frames_for(5, acid), 60, repeat=1)

    # Pebble or fragment: a sharp white-grey spark
    s.tag("Spark", frames_for(4, lambda cv, k, n: burst(cv, c0, c0, k, n, ["w", "c", "s"], rays=6, reach=6, seed=2, core=2)), 50, repeat=1)
    # Seeker bug: a red pop
    s.tag("Pop", frames_for(4, lambda cv, k, n: burst(cv, c0, c0, k, n, ["r", "o", "f"], rays=8, reach=7, seed=3, core=3)), 55, repeat=1)

    # Storm Ant strike: a yellow-white star flash on the target
    def zap(cv, k, n):
        r = [5, 8, 6, 3][k]
        a = [255, 255, 200, 120][k]
        for ang in range(0, 360, 45):
            rr = r if ang % 90 == 0 else r * 0.6
            x, y = c0 + math.cos(math.radians(ang)) * rr, c0 + math.sin(math.radians(ang)) * rr
            cv.line([(c0, c0), (x, y)], "y" if ang % 90 else "w", a=a)
        cv.blob(c0, c0, 2 if k < 2 else 1.2, 2 if k < 2 else 1.2, "w")
    s.tag("Zap", frames_for(4, zap), 45, repeat=1)

    # Dewdrop beam: a small rising wisp of steam
    def sizzle(cv, k, n):
        for j, (dx, c) in enumerate(((-2, "b"), (0, "w"), (2, "b"))):
            cv.px(c0 + dx, c0 - k * 2 - (j % 2), c, a=230 - k * 60)
        cv.px(c0, c0, "w", a=255 - k * 70)
    s.tag("Sizzle", frames_for(3, sizzle), 60, repeat=1)

    # Swarm Nest ants: two quick bite marks
    def scratch(cv, k, n):
        a = [255, 220, 120][k]
        for off in (-2, 1):
            cv.line([(c0 - 3 + off, c0 - 3 - k), (c0 + 1 + off, c0 + 3 - k)], "c", a=a)
    s.tag("Scratch", frames_for(3, scratch), 60, repeat=1)
    out.append(s)

    # Bombardier shell: fireball then smoke, 48 px
    s = Sprite("FxBlast", "Fx", 48, 48, ["FX"], "Mortar blast")
    frames = []
    rng = random.Random(21)
    bits = [(rng.uniform(0, 360), rng.uniform(0.7, 1.2), rng.choice("SsDy")) for _ in range(16)]
    for k in range(6):
        cv = Cv(48, 48)
        if k == 0:
            cv.disc(24, 24, 7, "w")
            cv.ring(24, 24, 9, "y", 2)
        else:
            r = [0, 12, 16, 18, 19, 20][k]
            smoke = max(0, 220 - k * 36)
            cv.disc(24, 24 - k * 0.6, r, "S", a=smoke // 2)
            cv.ring(24, 24 - k * 0.6, r, "x", 1, a=smoke)
            fire = [0, 11, 9, 6, 3, 0][k]
            if fire:
                cv.blob(24, 24, fire, fire, "o", "y", "r")
                cv.disc(24, 23.5, fire * 0.45, "y")
            for ang, spd, c in bits:
                d = r * spd
                cv.px(24 + math.cos(math.radians(ang)) * d, 24 + math.sin(math.radians(ang)) * d, c, a=max(0, 255 - k * 40))
        frames.append({"FX": cv.im})
    s.tag("Blast", frames, 65, repeat=1)
    out.append(s)

    # Build and sell
    s = Sprite("FxBuild", "Fx", 48, 48, ["FX"], "Tower build / sell")
    rng = random.Random(8)
    clods = [(rng.uniform(0, 360), rng.uniform(0.8, 1.2)) for _ in range(12)]
    rise = []
    for k in range(6):
        cv = Cv(48, 48)
        r = 6 + k * 2.6
        a = max(0, 255 - k * 40)
        cv.ring(24, 26, r, "t", 2 if k < 3 else 1, a=a, ry=r * 0.75)
        for ang, spd in clods:
            d = r * spd
            x, y = 24 + math.cos(math.radians(ang)) * d, 26 + math.sin(math.radians(ang)) * d * 0.75 - (3 - abs(k - 2.5)) * 2
            cv.blob(x, y, 1.6 - k * 0.15, 1.6 - k * 0.15, "d", "T", "D", a=a)
        if k < 3:
            cv.blob(24, 26, 9 - k * 2, 6 - k * 1.5, "c", a=150 - k * 40)
        rise.append({"FX": cv.im})
    s.tag("Rise", rise, 60, repeat=1)
    sell = []
    for k in range(6):
        cv = Cv(48, 48)
        r = 5 + k * 2.4
        a = max(0, 240 - k * 40)
        for j in range(7):
            ang = math.radians(j * 51 + 10)
            cv.blob(24 + math.cos(ang) * r, 26 + math.sin(ang) * r * 0.7, 2.6 - k * 0.3, 2.6 - k * 0.3, "s", "c", "S", a=a)
        for j, dx in enumerate((-6, 0, 6)):  # crumbs of food paid back, arcing up
            y = 22 - k * 3 + (k * k) * 0.35 + j
            cv.blob(24 + dx * (1 + k * 0.25), y, 1.6, 1.6, "y", "w", "Y", a=255 if k < 5 else 140)
        sell.append({"FX": cv.im})
    s.tag("Sell", sell, 60, repeat=1)
    out.append(s)

    # Tier overlays, drawn over a tower: a stone ring and banner at tier 2, gold at tier 3
    s = Sprite("FxTier", "Fx", 32, 32, ["FX"], "Upgrade tiers")
    for tier, (stone, hi, cloth, cloth_d, tip) in ((2, ("s", "c", "t", "T", "S")), (3, ("y", "w", "r", "R", "y"))):
        frames = []
        for k in range(4):
            cv = Cv(32, 32)
            deco = Cv(32, 32)
            # stones along the front (lower) half of the mound rim
            count = 7 if tier == 2 else 9
            for j in range(count):
                ang = math.radians(15 + j * 150 / (count - 1))
                x, y = 16 + math.cos(ang) * 13, 19.5 + math.sin(ang) * 9.5
                deco.blob(x, y, 1.4, 1.2, stone, hi, "S" if tier == 2 else "Y")
            # banner on a pole at the right shoulder of the mound
            deco.line([(27, 4), (27, 13)], "D")
            wave = (0, 1, 0, -1)[k]
            deco.pxs([(22, 5 + wave), (23, 5), (24, 5 - wave if tier == 3 else 5), (25, 5), (26, 5),
                      (22, 6 + wave), (23, 6), (24, 6), (25, 6), (26, 6),
                      (23, 7), (24, 7 + wave), (25, 7), (26, 7)], cloth)
            deco.pxs([(22, 7 + wave), (26, 7)], cloth_d)
            deco.px(27, 3, tip)
            deco.outline()
            cv.paste(deco)
            if tier == 3 and k % 2 == 0:
                cv.spr(SPARK_S, 5, 9 + k)
            frames.append({"FX": cv.im})
        s.tag(f"Tier{tier}", frames, 160)
    out.append(s)
    return out


def build_props():
    out = []
    # The hill being defended: a big anthill with the colony's flag, in four states of repair
    s = Sprite("PropHill", "Props", 64, 64, ["Shadow", "Body", "FX"], "The hill")
    rng = random.Random(30)
    cracks = [[(rng.randint(14, 50), rng.randint(20, 50))] for _ in range(7)]
    for c in cracks:
        x, y = c[0]
        for _ in range(4):
            x += rng.choice((-2, -1, 1, 2)); y += rng.choice((1, 2))
            c.append((x, y))
    for state, (n_cracks, flag, smoke, collapsed) in (("Healthy", (0, True, 0, False)), ("Damaged", (3, True, 1, False)),
                                                     ("Critical", (7, True, 3, False)), ("Destroyed", (7, False, 4, True))):
        frames = []
        for k in range(4):
            L = s.new()
            L["Shadow"].blob(32, 36, 29, 24, (0, 0, 0), a=85)
            body = Cv(64, 64)
            if collapsed:
                body.blob(32, 34, 26, 21, "D", "d", "e")
                body.blob(32, 34, 18, 13, "e", "D", "k")
            else:
                body.blob(32, 33, 27, 23, "T", "t", "d")
                body.ring(32, 34, 21, "d", 1, ry=17)
                body.blob(32, 31, 15, 12, "t", "c", "T")
                body.ring(32, 32, 9, "T", 1, ry=7)
            body.outline()
            if not collapsed:
                body.blob(32, 30, 4.5, 3.5, "k")           # the entrance
                body.blob(32, 29.5, 3, 2, "e")
                for x, y, c in ((12, 26, "c"), (50, 24, "c"), (16, 46, "S"), (46, 48, "s"), (30, 53, "d"), (8, 36, "m"), (55, 38, "m")):
                    body.px(x, y, c)
            for c in cracks[:n_cracks]:
                body.line(c, "D" if not collapsed else "k")
            L["Body"].paste(body)
            if flag:
                f = Cv(64, 64)
                f.line([(44, 6), (44, 22)], "D")
                w = (0, 1, 0, -1)[k]
                for row in range(5):
                    f.line([(45, 7 + row), (51 + (w if row % 2 else 0), 7 + row + (w if row > 2 else 0))], "r" if row < 4 else "R")
                f.px(47, 9, "y")
                f.outline()
                L["Body"].paste(f)
            for j in range(smoke):
                x = (18, 40, 28, 46)[j]
                y = 24 - ((k * 3 + j * 5) % 12)
                L["FX"].blob(x + (k % 2), y, 2.5, 2.5, "S", "s", "x", a=200 - ((k * 3 + j * 5) % 12) * 12)
            frames.append(flat(L))
        s.tag(state, frames, 180)
    out.append(s)

    # Where the enemy comes out
    s = Sprite("PropBurrow", "Props", 48, 48, ["Shadow", "Body", "FX"], "Enemy burrow")

    def burrow(L):
        L["Shadow"].blob(24, 26, 20, 15, (0, 0, 0), a=70)
        body = Cv(48, 48)
        body.blob(24, 24, 19, 15, "D", "d", "e")
        body.outline()
        body.blob(24, 24, 12, 9, "k")
        body.blob(24, 23, 9, 6, "v" if "v" in PAL else "e")
        for x, y, c in ((7, 20, "d"), (40, 19, "d"), (10, 32, "S"), (37, 33, "s"), (22, 38, "T")):
            body.px(x, y, c)
        L["Body"].paste(body)

    idle = []
    for k in range(2):
        L = s.new()
        burrow(L)
        if k == 1:
            L["FX"].pxs([(21, 22), (27, 22)], "r")       # eyes glint in the dark
        idle.append(flat(L))
    s.tag("Idle", idle, 900)
    spawn = []
    for k in range(5):
        L = s.new()
        burrow(L)
        r = 8 + k * 3
        for j in range(8):
            ang = math.radians(j * 45 + 10)
            L["FX"].blob(24 + math.cos(ang) * r, 24 + math.sin(ang) * r * 0.75, 2.2 - k * 0.3, 2.2 - k * 0.3,
                         "d", "T", "D", a=240 - k * 45)
        spawn.append(flat(L))
    s.tag("Spawn", spawn, 60, repeat=1)
    out.append(s)
    return out


# ================================================================ export
TAG_COLORS = [(0xff, 0xd2, 0x3a), (0xe2, 0x41, 0x2f), (0x9f, 0xe0, 0x4a), (0x5a, 0xa9, 0xd8),
              (0x9a, 0x6c, 0xf0), (0xff, 0x8a, 0x3d)]


def palette_of(sprite):
    seen = {}
    for _, frames, _, _ in sprite.tags:
        for layers in frames:
            for im in layers.values():
                for count, c in im.getcolors(1 << 16) or []:
                    if c[3]:
                        seen[c] = seen.get(c, 0) + count
    cols = sorted(seen, key=lambda c: -seen[c])[:255]
    return [(0, 0, 0, 0)] + cols


def composite(layers, order, w, h):
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    for name in order:
        if name in layers:
            im.alpha_composite(layers[name])
    return im


def export(sprite):
    w, h = sprite.w, sprite.h
    ase_dir = os.path.join(ROOT, "Aseprite", sprite.group)
    sheet_dir = os.path.join(ROOT, "Sheets", sprite.group)
    prev_dir = os.path.join(HERE, "Preview")
    for d in (ase_dir, sheet_dir, prev_dir):
        os.makedirs(d, exist_ok=True)

    frames, tags, rows = [], [], []
    for ti, (tname, tframes, dur, repeat) in enumerate(sprite.tags):
        start = len(frames)
        for layers in tframes:
            frames.append((dur, layers))
        tags.append({"name": tname, "from": start, "to": len(frames) - 1, "repeat": repeat,
                     "color": TAG_COLORS[ti % len(TAG_COLORS)]})
        rows.append((tname, start, len(tframes)))

    ase_path = os.path.join(ase_dir, sprite.name + ".aseprite")
    write_aseprite(ase_path, w, h, sprite.layers, frames, tags, palette_of(sprite))

    # verify: parse it back and compare every composited frame
    back = read_aseprite(ase_path)
    assert back["layers"] == sprite.layers, (sprite.name, back["layers"])
    assert [(t["name"], t["from"], t["to"], t["repeat"]) for t in back["tags"]] == \
           [(t["name"], t["from"], t["to"], t["repeat"]) for t in tags]
    comps = []
    for fi, (dur, layers) in enumerate(frames):
        comp = composite(layers, sprite.layers, w, h)
        got = back["frames"][fi]
        assert got["duration"] == dur
        assert comp.tobytes() == got["image"].tobytes(), f"{sprite.name} frame {fi} mismatch"
        comps.append(comp)

    # packed sheet: one row per tag
    cols = max(n for _, _, n in rows)
    sheet = Image.new("RGBA", (cols * w, len(rows) * h), (0, 0, 0, 0))
    json_frames = []
    for r, (tname, start, n) in enumerate(rows):
        for c in range(n):
            fi = start + c
            sheet.paste(comps[fi], (c * w, r * h))
            json_frames.append(
                '    { "filename": "%s %s %d", "frame": { "x": %d, "y": %d, "w": %d, "h": %d }, '
                '"rotated": false, "trimmed": false, "spriteSourceSize": { "x": 0, "y": 0, "w": %d, "h": %d }, '
                '"sourceSize": { "w": %d, "h": %d }, "duration": %d }'
                % (sprite.name, tname, c, c * w, r * h, w, h, w, h, w, h, frames[fi][0]))
    sheet.save(os.path.join(sheet_dir, sprite.name + ".png"))
    tag_json = ",\n".join(
        '    { "name": "%s", "from": %d, "to": %d, "direction": "forward"%s }'
        % (t["name"], t["from"], t["to"], ', "repeat": "1"' if t["repeat"] else "") for t in tags)
    layer_json = ", ".join('{ "name": "%s", "opacity": 255, "blendMode": "normal" }' % l for l in sprite.layers)
    with open(os.path.join(sheet_dir, sprite.name + ".json"), "w", newline="\n") as f:
        import json as _json
        f.write('{ "frames": [\n%s\n ],\n "meta": {\n  "app": "Hold the Hill build_anim_sheets.py",\n'
                '  "description": %s,\n'
                '  "image": "%s.png",\n  "format": "RGBA8888",\n  "size": { "w": %d, "h": %d },\n  "scale": "1",\n'
                '  "frameTags": [\n%s\n  ],\n  "layers": [ %s ]\n }\n}\n'
                % (",\n".join(json_frames), _json.dumps(sprite.caste or ""), sprite.name, sheet.width, sheet.height, tag_json, layer_json))

    # 4x GIF cycling every tag (one-shot tags play once, loops play twice)
    scale = 4
    gif = []
    durs = []
    bg = rgba("#2a1d14")
    for tname, start, n in rows:
        reps = 2 if sprite.tags[[t[0] for t in sprite.tags].index(tname)][3] == 0 else 1
        for _ in range(reps):
            for fi in range(start, start + n):
                im = Image.new("RGBA", (w, h), bg)
                im.alpha_composite(comps[fi])
                gif.append(im.resize((w * scale, h * scale), Image.NEAREST).convert("RGB"))
                durs.append(frames[fi][0])
        gif.append(gif[-1])
        durs.append(250)
    gif[0].save(os.path.join(prev_dir, sprite.name + ".gif"), save_all=True, append_images=gif[1:],
                duration=durs, loop=0, disposal=2)
    return rows, comps


def preview_sheet(results, path, scale=3):
    """Every frame of every sprite on one dark sheet with labels."""
    pad, label_w = 6, 150
    font = ImageFont.load_default()
    rows = []
    for sprite, (tag_rows, comps) in results:
        for tname, start, n in tag_rows:
            rows.append((sprite, tname, [comps[i] for i in range(start, start + n)]))
    width = label_w + max(len(f) * (s.w * scale + pad) for s, _, f in rows) + pad
    height = sum(s.h * scale + pad for s, _, _ in rows) + pad
    sheet = Image.new("RGBA", (width, height), rgba("#2a1d14"))
    d = ImageDraw.Draw(sheet)
    y = pad
    last = None
    for sprite, tname, frs in rows:
        if sprite is not last:
            d.line([(0, y - pad // 2), (width, y - pad // 2)], fill=rgba("#4a3626"))
            d.text((pad, y), sprite.name, fill=rgba("c"), font=font)
            if sprite.caste:
                d.text((pad, y + 12), sprite.caste, fill=rgba("T"), font=font)
            last = sprite
        d.text((pad + 90, y), tname, fill=rgba("y"), font=font)
        x = label_w
        for im in frs:
            cell = Image.new("RGBA", (sprite.w, sprite.h), rgba("#3a2a1e"))
            cell.alpha_composite(im)
            sheet.paste(cell.resize((sprite.w * scale, sprite.h * scale), Image.NEAREST), (x, y))
            x += sprite.w * scale + pad
        y += sprite.h * scale + pad
    sheet.save(path)


def main():
    sprites = (build_towers() + build_enemies() + build_fx() + build_status() + build_projectiles()
               + build_impacts() + build_props())
    results = []
    for s in sprites:
        results.append((s, export(s)))
        n = sum(len(f) for _, f, _, _ in s.tags)
        print(f"{s.group:8} {s.name:16} {s.w}x{s.h}  {n:2} frames  " + ", ".join(t[0] for t in s.tags))
    preview_sheet(results, os.path.join(HERE, "AnimSheetPreview.png"))
    print(f"{len(sprites)} sprites written and read back OK")


if __name__ == "__main__":
    main()
