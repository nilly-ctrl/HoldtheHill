"""Hold the Hill nature and colony-life sheets.

Nature (Props, top-down):
  WaterPuddle (32x32)      Small, Large: still water with a drifting glint
  WaterStream (32x32)      Horizontal, Vertical, Bend: flowing water that tiles along its length
  FxWater (32x32)          Ripple, Splash
  TerrainBridge (48x32)    Twig, Leaf, Broken: crossings for a stream (the stream runs up-down under them)
  TerrainCliff (32x32)     Edge, CornerLeft, CornerRight: a ledge with the drop toward the bottom
  TerrainTunnel (32x32)    Idle, Eyes, Collapse
  PlantFern, PlantTallGrass (32x32), PlantClover (24x24)   Sway (clover also has Lucky)
  PlantDandelion (32x32)   Bloom, Clock (the seed head), Blow
  RockBoulder (32x32)      A, B, Mossy

Colony life (Props, ants face RIGHT so they can be turned along a trail):
  AntWorker (24x16)        Idle, Walk, Groom, CarrySeed, CarryCrumb, CarryLeaf, CarryPebble, Dig, Tamp
  AntNurse (24x16)         Idle, Walk, CarryEgg, Tend
  AntGuard (24x24)         Idle, Walk, Alert
  AntPair (40x16)          Talk: two workers meeting head to head

    python build_nature_sheets.py
"""
import math
import os
import random

import build_anim_sheets as base
from build_anim_sheets import Cv, Sprite, SPARK_S, export, flat, mirror_y, tint
from build_extra_sheets import blade, leaf, thick_line
from build_scenery_sheets import loop, prop

HERE = os.path.dirname(os.path.abspath(__file__))
WATER = ("B", "b", "n")


# ================================================================ water
def water_blob(cv, cx, cy, rx, ry, k, glints):
    w = Cv(cv.w, cv.h)
    w.blob(cx, cy, rx, ry, *WATER)
    w.outline("n")
    cv.paste(w)
    cv.ring(cx, cy, rx - 2, "b", 1, a=110, ry=ry - 2)
    for j, (dx, dy, length) in enumerate(glints):
        x = cx + dx + (k + j) % 4 - 1
        cv.line([(x, cy + dy), (x + length, cy + dy)], "w", a=230 if (k + j) % 2 else 150)


def build_water():
    out = []
    s = prop("WaterPuddle", 32, "Puddles")
    loop(s, "Small", 4, lambda L, k: water_blob(L["Body"], 16, 17, 8, 5.5, k, [(-4, -2, 2), (1, 1, 3)]), 260)

    def large(L, k):
        water_blob(L["Body"], 14, 16, 11, 8, k, [(-6, -3, 3), (0, 2, 4), (3, -4, 2)])
        water_blob(L["Body"], 23, 21, 7, 5, k + 1, [(-2, 0, 2)])
    loop(s, "Large", 4, large, 260)
    out.append(s)

    s = prop("WaterStream", 32, "Stream pieces (tile along the flow)")

    def flow(L, k, shape):
        cv = L["Body"]
        w = Cv(32, 32)
        if shape == "h":
            for y in range(9, 23):
                for x in range(32):
                    w.px(x, y, "b" if y < 11 else "n" if y > 20 else "B")
        elif shape == "v":
            for x in range(9, 23):
                for y in range(32):
                    w.px(x, y, "b" if x < 11 else "n" if x > 20 else "B")
        else:                                                     # bend: in from the left, out at the bottom
            for y in range(32):
                for x in range(32):
                    d = math.hypot(x + 0.5, y + 0.5 - 32)
                    if 9 <= d < 23:
                        w.px(x, y, "n" if d < 11 else "b" if d > 20 else "B")
        cv.paste(w)
        # banks: a dark line on each side, drawn by hand so the piece still tiles
        if shape == "h":
            cv.line([(0, 8), (31, 8)], "n"); cv.line([(0, 23), (31, 23)], "n")
        elif shape == "v":
            cv.line([(8, 0), (8, 31)], "n"); cv.line([(23, 0), (23, 31)], "n")
        for j in range(5):                                        # streaks sliding a quarter tile per frame
            lane = 11 + j * 2.4
            pos = (j * 13 + k * 8) % 32
            for d in range(4):
                p = (pos + d) % 32
                if shape == "h":
                    cv.px(p, lane, "w", a=220 - d * 40)
                elif shape == "v":
                    cv.px(lane, p, "w", a=220 - d * 40)
                else:
                    ang = math.radians(-(p / 32) * 90)
                    cv.px(math.cos(ang) * (lane + 1), 32 + math.sin(ang) * (lane + 1), "w", a=220 - d * 40)

    loop(s, "Horizontal", 4, lambda L, k: flow(L, k, "h"), 110)
    loop(s, "Vertical", 4, lambda L, k: flow(L, k, "v"), 110)
    loop(s, "Bend", 4, lambda L, k: flow(L, k, "b"), 110)
    out.append(s)

    s = Sprite("FxWater", "Fx", 32, 32, ["FX"], "Water effects")

    def frames(n, draw):
        fr = []
        for k in range(n):
            cv = Cv(32, 32)
            draw(cv, k)
            fr.append({"FX": cv.im})
        return fr

    def ripple(cv, k):
        cv.ring(16, 16, 3 + k * 2.6, "w", 1, a=240 - k * 50, ry=(3 + k * 2.6) * 0.6)
        if k < 3:
            cv.ring(16, 16, 1 + k * 1.8, "b", 1, a=200 - k * 60, ry=(1 + k * 1.8) * 0.6)

    def splash(cv, k):
        for j in range(7):
            ang = math.radians(200 + j * 23)
            d = 3 + k * 2.4
            cv.blob(16 + math.cos(ang) * d, 17 + math.sin(ang) * d - math.sin(k / 4 * math.pi) * 4, 1.6 - k * 0.25, 1.6 - k * 0.25,
                    "b", "w", "B", a=250 - k * 45)
        ripple(cv, k)

    s.tag("Ripple", frames(5, ripple), 80, repeat=1)
    s.tag("Splash", frames(5, splash), 70, repeat=1)
    out.append(s)
    return out


# ================================================================ terrain
def build_terrain():
    out = []
    s = prop("TerrainBridge", 32, "Stream crossings", w=48)

    def under(L):
        for x in range(17, 31):                                   # a strip of the stream beneath, for context
            for y in range(32):
                L["Shadow"].px(x, y, "B" if 19 <= x < 29 else "n", a=255)

    def twig(L, k, broken=False):
        under(L)
        b = Cv(48, 32)
        for j, y in enumerate((9, 13, 17, 21)):
            if broken and j in (1, 2):
                thick_line(b, [(3, y), (17, y + (1 if j == 1 else -1))], ("d", "T", "D"))
                thick_line(b, [(32, y + 1), (44, y)], ("d", "T", "D"))
            else:
                thick_line(b, [(3, y), (24, y + (j % 2)), (44, y)], ("d", "T", "D"))
        for x in (9, 38):                                         # lashings
            b.line([(x, 8), (x, 23)], "M")
        b.outline()
        L["Body"].paste(b)
        if broken:
            L["FX"].ring(24, 15, 3 + k, "w", 1, a=200 - k * 45, ry=2 + k * 0.6)

    def leaf_bridge(L, k):
        under(L)
        leaf(L["Body"], 24, 16, 42, 16, 0, ("m", "g", "M"))
        for x in range(8, 40, 6):
            L["Body"].line([(x, 16), (x + 3, 11)], "M")
            L["Body"].line([(x, 16), (x + 3, 21)], "M")
        if k == 1:
            L["FX"].spr(SPARK_S, 30, 9)

    loop(s, "Twig", 1, twig, 1000)
    loop(s, "Leaf", 2, leaf_bridge, 700)
    loop(s, "Broken", 4, lambda L, k: twig(L, k, True), 160)
    out.append(s)

    s = prop("TerrainCliff", 32, "Ledge pieces; the drop is toward the bottom")

    def cliff(L, k, shape):
        b = Cv(32, 32)
        rng = random.Random(3)
        for x in range(32):
            if shape == "edge":
                top = 12 + round(math.sin(x * 0.7) * 1.2)
            elif shape == "left":
                top = 12 + max(0, 12 - x) * 1.4 if x < 12 else 12 + round(math.sin(x * 0.7) * 1.2)
            else:
                top = 12 + max(0, x - 19) * 1.4 if x > 19 else 12 + round(math.sin(x * 0.7) * 1.2)
            top = int(top)
            for y in range(top, min(32, top + 13)):
                d = y - top
                b.px(x, y, "t" if d < 2 else "T" if d < 5 else "d" if d < 9 else "D")
            b.px(x, top, "c" if x % 5 == 0 else "t")
            if top + 13 < 32:
                for y in range(top + 13, min(32, top + 17)):
                    L["Shadow"].px(x, y, (0, 0, 0), a=90 - (y - top - 13) * 20)
        for _ in range(9):
            x, y = rng.randrange(32), rng.randrange(16, 26)
            if b.p[x, y][3]:
                b.px(x, y, "S")
                b.px(x, y + 1, "e")
        L["Body"].paste(b)
        for x in (5, 17, 26):                                     # grass hanging over the lip
            if b.p[x, 14][3]:
                L["FX"].line([(x, 12), (x, 14)], "m")
                L["FX"].px(x + 1, 13, "M")

    loop(s, "Edge", 1, lambda L, k: cliff(L, k, "edge"), 1000)
    loop(s, "CornerLeft", 1, lambda L, k: cliff(L, k, "left"), 1000)
    loop(s, "CornerRight", 1, lambda L, k: cliff(L, k, "right"), 1000)
    out.append(s)

    s = prop("TerrainTunnel", 32, "Tunnel mouth")

    def tunnel(L, k, eyes=False, fall=0):
        L["Shadow"].blob(16, 20, 13, 9, (0, 0, 0), a=75)
        b = Cv(32, 32)
        b.blob(16, 18, 12, 10, "d", "T", "D")
        b.outline()
        for j in range(7):                                        # stones round the lip
            ang = math.radians(200 + j * 23)
            b.blob(16 + math.cos(ang) * 10, 18 + math.sin(ang) * 8, 2, 1.6, "S", "s", "x")
        hole = Cv(32, 32)
        hole.blob(16, 19, max(0.6, 7 - fall * 1.2), max(0.6, 6 - fall), "k")
        hole.blob(16, 18.5, max(0.4, 5 - fall), max(0.4, 4 - fall * 0.8), "v")
        b.paste(hole)
        L["Body"].paste(b)
        if eyes and k % 3:
            L["FX"].pxs([(13, 18), (18, 18)], "r" if k % 3 == 1 else "o")
        if fall:
            for j in range(8):
                ang = math.radians(j * 45 + fall * 10)
                d = 9 - fall * 1.3
                L["FX"].blob(16 + math.cos(ang) * d, 18 + math.sin(ang) * d * 0.8 - (2 if fall < 3 else 0), 2.2, 1.9, "d", "T", "D", a=255)
            if fall >= 4:
                L["FX"].blob(16, 18, 6, 4.5, "d", "T", "D")
                L["FX"].blob(14, 12 - fall, 3, 2.6, "s", "c", "S", a=150)

    loop(s, "Idle", 1, tunnel, 1000)
    loop(s, "Eyes", 6, lambda L, k: tunnel(L, k, eyes=True), 300)
    loop(s, "Collapse", 6, lambda L, k: tunnel(L, k, fall=k), 90, repeat=1)
    out.append(s)
    return out


# ================================================================ plants and rocks
def build_plants():
    out = []
    s = prop("PlantFern", 32, "Fern")

    def fern(L, k):
        sway = (0, 1, 0, -1)[k]
        L["Shadow"].blob(16, 24, 10, 4, (0, 0, 0), a=70)
        b = L["Body"]
        for j, (ang, length) in enumerate(((-150, 12), (-115, 14), (-90, 15), (-65, 14), (-30, 12))):
            a = math.radians(ang + sway * 3)
            tip = (16 + math.cos(a) * length, 25 + math.sin(a) * length)
            b.line([(16, 25), tip], "M")
            for step in range(2, int(length), 2):                 # leaflets either side of the frond
                px_, py_ = 16 + math.cos(a) * step, 25 + math.sin(a) * step
                size = max(1, 3 - step // 5)
                nx, ny = -math.sin(a), math.cos(a)
                for side in (-1, 1):
                    b.line([(px_, py_), (px_ + nx * size * side, py_ + ny * size * side)], "m" if side < 0 else "g")
        b.blob(16, 25.5, 2, 1.3, "j")

    loop(s, "Sway", 4, fern, 260)
    out.append(s)

    s = prop("PlantTallGrass", 32, "Tall grass")

    def tall(L, k):
        sway = (0, 1, 2, 1)[k]
        L["Shadow"].blob(16, 27, 11, 3, (0, 0, 0), a=70)
        for x, h, lean, col in ((6, 14, -2, "M"), (10, 19, -1, "m"), (14, 22, 0, "M"), (18, 20, 1, "m"), (22, 16, 2, "M"), (26, 12, 3, "m"), (12, 12, -3, "m")):
            blade(L["Body"], x, 27, h, lean + sway * (h / 14), col, "g")
            blade(L["Body"], x + 1, 27, h - 2, lean + sway * (h / 14), "j", "M")
        L["Body"].pxs([(14 + sway * 2, 4), (15 + sway * 2, 3), (18 + sway * 2, 7)], "t")   # seed heads

    loop(s, "Sway", 4, tall, 220)
    out.append(s)

    s = prop("PlantClover", 24, "Clover")

    def clover(L, k, leaves=3):
        sway = (0, 0, 1, 0)[k]
        L["Shadow"].blob(12.5, 15, 8, 5, (0, 0, 0), a=65)
        b = Cv(24, 24)
        for j in range(leaves):
            ang = math.radians(-90 + j * 360 / leaves + (45 if leaves == 4 else 0))
            b.blob(12 + sway + math.cos(ang) * 4.2, 12 + math.sin(ang) * 4.2, 3.6, 3.4, "m", "g", "M")
        b.outline()
        for j in range(leaves):
            ang = math.radians(-90 + j * 360 / leaves + (45 if leaves == 4 else 0))
            b.line([(12 + sway, 12), (12 + sway + math.cos(ang) * 5, 12 + math.sin(ang) * 5)], "g")
        b.px(12 + sway, 12, "j")
        L["Body"].paste(b)
        if leaves == 4 and k == 1:
            L["FX"].spr(SPARK_S, 17, 3)

    loop(s, "Sway", 4, clover, 300)
    loop(s, "Lucky", 4, lambda L, k: clover(L, k, 4), 300)
    out.append(s)

    s = prop("PlantDandelion", 32, "Dandelion")

    def dandelion(L, k, kind):
        sway = (0, 1, 0, -1)[k % 4]
        L["Shadow"].blob(16, 26, 9, 3.5, (0, 0, 0), a=70)
        b = Cv(32, 32)
        for ang, length in ((200, 9), (340, 9), (250, 6), (290, 6)):   # the leaf rosette on the ground
            a = math.radians(ang)
            leaf(b, 16 + math.cos(a) * length * 0.5, 25 + math.sin(a) * length * 0.3, length + 2, 4, ang, ("m", "g", "M"))
        L["Body"].paste(b)
        head = Cv(32, 32)
        hx, hy = 16 + sway, 12
        if kind == "bloom":
            for j in range(12):
                a = math.radians(j * 30 + k * 4)
                head.line([(hx, hy), (hx + math.cos(a) * 6, hy + math.sin(a) * 6)], "y" if j % 2 else "u")
            head.blob(hx, hy, 3, 3, "y", "c", "Y")
            head.outline()
        else:
            gone = k if kind == "blow" else 0
            for j in range(16):
                if j < gone * 3:
                    continue
                a = math.radians(j * 22.5)
                head.px(hx + math.cos(a) * 6, hy + math.sin(a) * 6, "w")
                head.px(hx + math.cos(a) * 4, hy + math.sin(a) * 4, "c", a=200)
            head.disc(hx, hy, 5 - gone * 0.5, "c", a=90)
            head.blob(hx, hy, 1.6, 1.6, "T")
        L["Body"].paste(head)
        if kind == "blow":                                        # seeds drifting off to the right
            for j in range(k * 2):
                x, y = hx + 6 + (k - j * 0.5) * 3.2, hy - 2 - j * 1.5 + math.sin((k + j) * 0.9) * 2
                L["FX"].px(x, y, "w", a=max(60, 255 - j * 25))
                L["FX"].px(x - 1, y + 1, "T", a=max(60, 200 - j * 25))

    loop(s, "Bloom", 4, lambda L, k: dandelion(L, k, "bloom"), 280)
    loop(s, "Clock", 4, lambda L, k: dandelion(L, k, "clock"), 280)
    loop(s, "Blow", 6, lambda L, k: dandelion(L, k, "blow"), 110, repeat=1)
    out.append(s)

    s = prop("RockBoulder", 32, "Boulders")

    def boulder(L, k, shape, moss=False):
        L["Shadow"].blob(17, 22, 13, 7, (0, 0, 0), a=85)
        b = Cv(32, 32)
        if shape == "a":
            b.blob(16, 17, 11, 9, "S", "s", "x")
            b.blob(11, 13, 5, 4, "s", "c", "S")
        else:
            b.blob(13, 18, 8, 7, "S", "s", "x")
            b.blob(21, 16, 7, 8, "S", "s", "x")
            b.blob(17, 22, 5, 3, "x", "S", "k")
        b.outline()
        for pts in ([(12, 16), (15, 19), (14, 23)], [(20, 12), (22, 16), (25, 18)]):
            b.line(pts, "x")
        if moss:
            for x, y, r in ((10, 11, 3), (16, 10, 2.4), (22, 12, 2.6), (7, 15, 2)):
                if b.p[int(x), int(y)][3]:
                    b.blob(x, y, r, r * 0.7, "M", "m", "j")
        L["Body"].paste(b)

    loop(s, "A", 1, lambda L, k: boulder(L, k, "a"), 1000)
    loop(s, "B", 1, lambda L, k: boulder(L, k, "b"), 1000)
    loop(s, "Mossy", 1, lambda L, k: boulder(L, k, "a", True), 1000)
    out.append(s)
    return out


# ================================================================ colony life (ants face right)
def small_ant(cv, cx, cy, phase=None, col=("a", "A", "z"), size=1.0, head=1.0, tw=0, mand=False, turn=0):
    """A worker seen from above, facing right. phase None = standing; turn tips the head (grooming).
    Three separate segments with a pixel of gap between them, so the outline draws the waist."""
    ink, body = Cv(cv.w, cv.h), Cv(cv.w, cv.h)
    s = 0 if phase is None else round(math.sin(phase) * 1.3)
    # legs: front pair reach forward, hind pair back; the tripod on each side swings opposite ways
    for j, (ax, reach) in enumerate(((1.0, 2.2), (0.0, 0.3), (-1.0, -2.4))):
        off = s if j != 1 else -s
        for side, o in ((-1, off), (1, -off)):
            hip = (cx + ax * size, cy - 0.5 + side * 0.6)
            knee = (cx + (ax + reach * 0.5) * size + o * 0.5, cy - 0.5 + side * 2.6 * size)
            foot = (cx + (ax + reach) * size + o, cy - 0.5 + side * 4.4 * size)
            ink.line([hip, knee, foot], "k")
    hx = cx + 3.5 * size
    for side in (-1, 1):
        wig = tw if side < 0 else -tw
        ink.line([(hx + 1, cy - 0.5 + side), (hx + 2.2 * size, cy - 0.5 + side * 2.4), (hx + 3.6 * size, cy - 0.5 + side * (2.2 + wig))], "k")
        if mand:
            ink.px(hx + 2.2 * size * head, cy - 0.5 + side * 0.9, "k")
    b, hi, lo = col
    body.blob(cx - 4.5 * size, cy, 2.5 * size, 2.1 * size, b, hi, lo)             # gaster
    body.blob(cx, cy, 1.0 * size, 1.0 * size, b, hi, lo)                          # thorax
    body.blob(hx, cy + turn, 1.5 * size * head, 1.5 * size * head, b, hi, lo)     # head
    body.outline()
    body.px(cx - 5.5 * size, cy - 1.2 * size, hi)
    ink.paste(body)
    cv.paste(ink)


def carry(cv, x, y, what):
    c = Cv(cv.w, cv.h)
    if what == "seed":
        c.blob(x, y, 2, 1.6, "d", "T", "D")
    elif what == "crumb":
        c.blob(x, y, 2.2, 2, "t", "c", "T")
    elif what == "pebble":
        c.blob(x, y, 2, 1.8, "S", "s", "x")
    elif what == "egg":
        c.blob(x, y, 2, 1.6, "c", "w", "t")
    elif what == "leaf":
        leaf(c, x + 1, y, 8, 5, 75, ("m", "g", "M"))
        cv.paste(c)
        return
    c.outline()
    cv.paste(c)


def build_colony():
    out = []
    rosy = ("#e9b49c", "#fff1d6", "#b9795f")

    def ant_sheet(name, caste, size, col, scale=1.0, head=1.0, mand=False):
        # small ants get a wider canvas, so whatever they carry in front fits
        wide = size + 8 if size < 24 else size
        s = prop(name, size, caste, w=wide)
        cx, cy = (9.5 if size < 24 else size / 2 - 1), size / 2 + 0.5

        def frame(L, phase=None, load=None, dx=0, tw=0, turn=0):
            L["Shadow"].blob(cx + dx - 0.5, cy + 1, 6 * scale, 2.6 * scale, (0, 0, 0), a=70)
            small_ant(L["Body"], cx + dx, cy, phase, col, scale, head, tw, mand, turn)
            if load:
                carry(L["Body"], cx + dx + 6.8 * scale, cy - 0.5, load)

        s.frame, s.cx, s.cy = frame, cx, cy
        loop(s, "Idle", 4, lambda L, k: frame(L, tw=(0, 1, 0, -1)[k]), 220)
        loop(s, "Walk", 6, lambda L, k: frame(L, 2 * math.pi * k / 6), 80)
        return s

    # ---- worker
    s = ant_sheet("AntWorker", "Worker", 16, ("a", "A", "z"))
    f, cx, cy = s.frame, s.cx, s.cy
    loop(s, "Groom", 6, lambda L, k: (f(L, tw=(1, 2, 1, 2, 1, 0)[k], turn=(0, 1, 1, -1, -1, 0)[k]),
                                     L["FX"].px(cx + 5, cy - 2 + (k % 2) * 3, "c") if 1 <= k <= 4 else None), 130)
    for tag, what in (("CarrySeed", "seed"), ("CarryCrumb", "crumb"), ("CarryLeaf", "leaf"), ("CarryPebble", "pebble")):
        loop(s, tag, 6, lambda L, k, what=what: f(L, 2 * math.pi * k / 6, load=what), 95)

    def dig(L, k):
        f(L, dx=(0, 1, 0, 1, 0, 0)[k], tw=1)
        L["Shadow"].blob(cx + 5, cy, 2 + k * 0.3, 1.6 + k * 0.2, "e", a=200)       # the hole grows
        for j in range(3):
            if k and (k + j) % 2:
                L["FX"].px(cx + 2 - j * 2 - k % 3, cy - 3 - (k % 2) - j, "d" if j % 2 else "T")

    loop(s, "Dig", 6, dig, 90)

    def tamp(L, k):
        f(L, dx=(0, 1, 0, 1, 0, 0)[k])
        carry(L["Body"], cx + 5.6, cy - 0.5 + (0, 1, 0, 1, 0, 0)[k] * 0, "pebble")
        if k in (1, 3):
            L["FX"].pxs([(cx + 7, cy - 3), (cx + 7, cy + 2)], "c")

    loop(s, "Tamp", 6, tamp, 100)
    out.append(s)

    # ---- nurse
    s = ant_sheet("AntNurse", "Nurse", 16, rosy)
    f, cx, cy = s.frame, s.cx, s.cy
    loop(s, "CarryEgg", 6, lambda L, k: f(L, 2 * math.pi * k / 6, load="egg"), 100)

    def tend(L, k):
        f(L, tw=(0, 1, 1, 0, 1, 0)[k], turn=(0, 0, 1, 1, 0, 0)[k])
        carry(L["Body"], cx + 6, cy - 0.5, "egg")
        if k in (2, 3):
            hx, hy = cx + 3, cy - 6 - (k - 2)
            L["FX"].pxs([(hx, hy + 1), (hx + 1, hy), (hx + 2, hy + 1), (hx + 3, hy), (hx + 4, hy + 1), (hx + 1, hy + 2), (hx + 2, hy + 2),
                         (hx + 3, hy + 2), (hx + 2, hy + 3), (hx + 1, hy + 1), (hx + 3, hy + 1)], "f")

    loop(s, "Tend", 6, tend, 140)
    out.append(s)

    # ---- guard
    s = ant_sheet("AntGuard", "Guard", 24, ("q", "Q", "Z"), scale=1.45, head=1.25, mand=True)
    f, cx, cy = s.frame, s.cx, s.cy

    def alert(L, k):
        f(L, dx=(0, 1, 2, 1, 0)[k], tw=2)
        if k < 4:
            L["FX"].line([(cx - 2, 2 - (k % 2)), (cx - 2, 6 - (k % 2))], "r")
            L["FX"].px(cx - 2, 8 - (k % 2), "r")
        if k == 2:
            L["FX"].pxs([(cx + 10, cy - 2), (cx + 10, cy + 1)], "c")              # mandibles clack

    loop(s, "Alert", 5, alert, 90, repeat=1)
    out.append(s)

    # ---- two workers meeting
    s = prop("AntPair", 16, "Two workers meeting", w=40)

    def talk(L, k):
        for side, col in ((-1, ("a", "A", "z")), (1, ("a", "A", "z"))):
            one = Cv(40, 16)
            small_ant(one, 10, 8.5, None, col, tw=(1, 2, 1, 0)[(k + (side > 0)) % 4])
            im = one.im if side < 0 else one.im.transpose(0)      # 0 = FLIP_LEFT_RIGHT
            L["Shadow"].blob(9.5 if side < 0 else 30.5, 9.5, 6, 2.6, (0, 0, 0), a=70)
            L["Body"].paste(im)
        if k % 2:
            L["FX"].px(19, 5, "c")
            L["FX"].px(20, 11, "c")

    loop(s, "Talk", 4, talk, 200)
    out.append(s)
    return out


# ================================================================ run
def main():
    nature = build_water() + build_terrain() + build_plants()
    colony = build_colony()
    results = []
    for s in nature + colony:
        results.append((s, export(s)))
        n = sum(len(f) for _, f, _, _ in s.tags)
        print(f"{s.group:6} {s.name:16} {s.w}x{s.h}  {n:2} frames  " + ", ".join(t[0] for t in s.tags))
    base.preview_sheet(results[:len(nature)], os.path.join(HERE, "NatureSheetPreview.png"), scale=3)
    base.preview_sheet(results[len(nature):], os.path.join(HERE, "ColonySheetPreview.png"), scale=4)
    total = sum(sum(len(f) for _, f, _, _ in s.tags) for s, _ in results)
    print(f"{len(results)} sprites, {total} frames written and read back OK")


if __name__ == "__main__":
    main()
