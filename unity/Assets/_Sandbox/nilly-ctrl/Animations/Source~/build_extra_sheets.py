"""Hold the Hill extra sheets: enemy attacks and spawns, hits on the hill, and map dressing.

  Enemies/Enemy<Name>Moves   Attack and Spawn for each of the 7 enemies. Same canvas, pivot and
                             layers as Enemy<Name>, so the clips swap in without moving anything.
  Fx/FxHillHit               what the hill shows when an enemy reaches it: Hit, HitHeavy, Breach
  Fx/FxStatusOverlay         worn over an enemy while a status lasts: Burn, Poison, Web, Honey
  Props/Decor<Kind>          ground dressing. Each tag is one variant (A, B, C); grass and flowers sway.

    python build_extra_sheets.py

Kept apart from build_anim_sheets.py so the files the graybox already uses are not rewritten.
"""
import math
import os
import random

from PIL import Image

import build_anim_sheets as base
from build_anim_sheets import (Cv, Sprite, beetle, bubble, centipede, export, fade, flat, mantis, shadow,
                               tint, SPARK_S)
from build_weapon_sheets import contact_sheet

HERE = os.path.dirname(os.path.abspath(__file__))
DIRT = "#87603a"


# ================================================================ enemy attack and spawn
LUNGE = [-1, -2, 1, 2, 1, 0]     # wind back, strike, settle


def chomp(fx, x, y, k, size):
    """Jaws closing on the hill at (x, y): frames 2 and 3 of an attack. Chips of dirt fly back."""
    x = min(x, fx.w - 3)
    if k == 2:
        fx.spr(SPARK_S, x - 1, y - 1.5)
    reach = 2 + (k - 2) * 2
    for j, ang in enumerate((115, 150, 210, 245)):
        d = 2 + reach + (j % 2)
        fx.px(x + math.cos(math.radians(ang)) * d, y + math.sin(math.radians(ang)) * d,
              "t" if j % 2 else "d", a=255 - (k - 2) * 70)
    if size >= 32:
        for side in (-1, 1):
            fx.line([(x + 1, y + side * (4 - (k - 2))), (x + 2, y + side * (2 - (k - 2)))], "c", a=230 - (k - 2) * 60)


def mask_circle(im, cx, cy, r):
    out = im.copy()
    p = out.load()
    for y in range(out.height):
        for x in range(out.width):
            if (x + 0.5 - cx) ** 2 + (y + 0.5 - cy) ** 2 > r * r:
                p[x, y] = (0, 0, 0, 0)
    return out


def spawn_frames(s, pose, cx, cy, n=6):
    """Digging out of the ground: a dark hole, the body uncovered from the middle outward and
    shedding its coat of dirt, clods thrown clear."""
    out = []
    half = s.w / 2
    for k in range(n):
        t = k / (n - 1)
        L = {name: Image.new("RGBA", (s.w, s.h), (0, 0, 0, 0)) for name in s.layers}
        ground = Cv(s.w, s.h)
        ground.blob(cx, cy, half * (0.7 - 0.25 * t), half * (0.5 - 0.2 * t), "e", a=int(230 * (1 - t)))
        ground.paste(fade(pose["Shadow"], t))
        L["Shadow"] = ground.im
        r = half * (0.3 + 1.2 * t)
        L["Body"] = tint(mask_circle(pose["Body"], cx, cy, r), DIRT, 0.75 * (1 - t))
        if "Shield" in pose and k >= n - 2:
            L["Shield"] = fade(pose["Shield"], 0.5 if k == n - 2 else 1.0)
        fx = Cv(s.w, s.h)
        if k < n - 1:
            for j in range(7):
                ang = math.radians(j * 51 + 15)
                d = half * (0.35 + 0.6 * t)
                pr = max(0.7, (1.9 - t * 1.2) * s.w / 32)
                fx.blob(cx + math.cos(ang) * d, cy + math.sin(ang) * d * 0.8 - math.sin(t * math.pi) * 2, pr, pr,
                        "d", "T", "D", a=int(250 - t * 140))
        L["FX"] = fx.im
        out.append(L)
    return out


def moves(name, caste, size, layers):
    return Sprite(name + "Moves", "Enemies", size, size, layers, caste + " attack and spawn")


def beetle_moves(name, caste, size, Lh, Wh, shell, kind):
    s = moves(name, caste, size, ["Shadow", "Body", "FX"])
    cx, cy = size / 2, size / 2 + 0.5

    def pose(dx=0, mand=0):
        L = s.new()
        L["Shadow"].paste(shadow(size, size, cx + dx, cy + 1, Lh + 3, Wh + 1))
        L["Body"].paste(beetle(size, size, cx - 1 + dx, cy, Lh, Wh, shell, kind, 0.0, mand=mand))
        return L

    attack = []
    for k in range(6):
        L = pose(LUNGE[k], mand=[1, 2, 0, 0, 1, 0][k] if kind == "stag" else 0)
        if k in (2, 3):
            chomp(L["FX"], cx - 1 + LUNGE[k] + Lh + (9 if kind == "stag" else 5), cy - 0.5, k, size)
        attack.append(flat(L))
    s.tag("Attack", attack, 90 if kind == "stag" else 70, repeat=1)
    s.tag("Spawn", spawn_frames(s, flat(pose()), cx, cy), 80, repeat=1)
    return s


def shielded_moves():
    size, cx, cy = 32, 16, 16.5
    s = moves("EnemyShielded", "Rhino Beetle", size, ["Shadow", "Body", "Shield", "FX"])

    def pose(dx=0, flash=0.0):
        L = s.new()
        L["Shadow"].paste(shadow(size, size, cx + dx, cy + 1, 10, 7))
        L["Body"].paste(beetle(size, size, cx - 2 + dx, cy, 7, 5.5, ("n", "B", "hz"), "horn", 0.0))
        bubble(L["Shield"], cx + dx, cy, 13.5, 0, flash)
        return L

    attack = []
    for k in range(6):                       # a horn thrust; the bubble flares as it lands
        L = pose(LUNGE[k], flash=[0, 0, 0.7, 0.4, 0.1, 0][k])
        if k in (2, 3):
            chomp(L["FX"], cx + LUNGE[k] + 13, cy - 1.5, k, size)
        attack.append(flat(L))
    s.tag("Attack", attack, 80, repeat=1)
    s.tag("Spawn", spawn_frames(s, flat(pose()), cx, cy), 80, repeat=1)
    return s


def centipede_moves(name, caste, size, cx, cy, segs, spacing, shadow_r, **kw):
    s = moves(name, caste, size, ["Shadow", "Body", "FX"])

    def pose(dx=0, amp_scale=1.0, phase=0.0):
        L = s.new()
        L["Shadow"].paste(shadow(size, size, cx + dx, cy + 1, *shadow_r))
        args = dict(kw)
        args["amp"] = args.get("amp", 1.0) * amp_scale
        L["Body"].paste(centipede(size, size, cx + dx, cy, segs, spacing, phase, **args))
        return L

    attack = []
    for k in range(6):                       # coils up, then snaps forward with its forcipules
        L = pose(LUNGE[k], amp_scale=[1.5, 2.2, 0.3, 0.3, 0.8, 1.0][k], phase=k * 0.5)
        if k in (2, 3):
            chomp(L["FX"], cx + LUNGE[k] + (segs - 1) * spacing / 2 + 5, cy - 0.5, k, size)
        attack.append(flat(L))
    s.tag("Attack", attack, 70, repeat=1)
    s.tag("Spawn", spawn_frames(s, flat(pose()), cx, cy), 80, repeat=1)
    return s


def healer_moves():
    size, cx, cy = 32, 15, 16.5
    s = moves("EnemyHealer", "Mantis", size, ["Shadow", "Body", "FX"])

    def pose(dx=0, arms=0.0):
        L = s.new()
        L["Shadow"].paste(shadow(size, size, cx + dx, cy + 1, 12, 5))
        L["Body"].paste(mantis(size, size, cx + dx, cy, 0.0, arms))
        return L

    attack = []
    for k in range(6):                       # forelegs spread wide, then snap shut in front
        L = pose([0, -1, 1, 1, 0, 0][k], arms=[0.6, 1.0, -0.5, -0.5, 0.2, 0.0][k])
        fx = L["FX"]
        if k in (2, 3):
            x = cx + 13
            for side in (-1, 1):             # the two sweeps the forelegs cut through the air
                fx.line([(x - 3, cy + side * (7 - (k - 2) * 2)), (x + 1, cy + side * (3 - (k - 2)))], "c", a=240 - (k - 2) * 90)
            if k == 2:
                fx.spr(SPARK_S, x, cy - 1.5)
        attack.append(flat(L))
    s.tag("Attack", attack, 70, repeat=1)
    s.tag("Spawn", spawn_frames(s, flat(pose()), cx, cy), 80, repeat=1)
    return s


def build_enemy_moves():
    return [
        beetle_moves("EnemyRunner", "Scarab", 24, 5, 4, ("i", "I", "iz"), "scarab"),
        beetle_moves("EnemyGrunt", "Beetle", 24, 6, 5, ("h", "H", "hz"), "plain"),
        beetle_moves("EnemyBrute", "Stag Beetle", 40, 10, 8, ("q", "Q", "Z"), "stag"),
        shielded_moves(),
        centipede_moves("EnemySplitter", "Centipede", 32, 16, 16.5, 6, 3.4, (13, 4)),
        centipede_moves("EnemySwarm", "Centipede Hatchling", 16, 7.5, 8.5, 3, 2.6, (6, 3),
                        colors=("l", "p", "L"), seg_r=(1.4, 1.8), amp=0.6),
        healer_moves(),
    ]


# ================================================================ hits on the hill
def build_hill_hit():
    n, c0 = 48, 24
    s = Sprite("FxHillHit", "Fx", n, n, ["FX"], "An enemy reaching the hill")

    def frames(count, draw):
        out = []
        for k in range(count):
            cv = Cv(n, n)
            draw(cv, k, k / (count - 1))
            out.append({"FX": cv.im})
        return out

    def clods(cv, t, count, reach, size, seed):
        rng = random.Random(seed)
        for j in range(count):
            ang = math.radians(j * 360 / count + rng.uniform(-15, 15))
            d = 4 + reach * t * rng.uniform(0.7, 1.0)
            r = size * (1 - t * 0.6) * rng.uniform(0.7, 1.1)
            cv.blob(c0 + math.cos(ang) * d, c0 + math.sin(ang) * d * 0.8 - math.sin(t * math.pi) * 3, r, r,
                    "t" if j % 3 == 0 else "T", "c", "d", a=int(255 * (1 - t * 0.75)))

    def hit(cv, k, t):                       # a bite out of the mound
        cv.ring(c0, c0, 5 + k * 3, "r", 2 if k < 2 else 1, a=max(0, 230 - k * 55), ry=(5 + k * 3) * 0.8)
        clods(cv, t, 8, 15, 2.2, 3)
        if k < 2:
            cv.blob(c0, c0, 4 - k * 1.5, 4 - k * 1.5, "w", "w", "f")

    def heavy(cv, k, t):                     # a brute's blow: cracks, a bigger shower, a second ring
        rng = random.Random(8)
        a = int(255 * (1 - t * 0.8))
        for j in range(7):
            ang = math.radians(j * 51 + rng.uniform(-12, 12))
            pts = [(c0, c0)]
            for step in range(1, 4):
                d = step * (3 + min(k, 3) * 1.2)
                pts.append((c0 + math.cos(ang + rng.uniform(-0.3, 0.3)) * d, c0 + math.sin(ang + rng.uniform(-0.3, 0.3)) * d * 0.8))
            cv.line(pts, "e", a=a)
        cv.ring(c0, c0, 6 + k * 3.4, "r", 2, a=max(0, 240 - k * 45), ry=(6 + k * 3.4) * 0.8)
        cv.ring(c0, c0, 3 + k * 2.4, "f", 1, a=max(0, 200 - k * 45), ry=(3 + k * 2.4) * 0.8)
        clods(cv, t, 11, 19, 3.0, 5)
        if k < 2:
            cv.blob(c0, c0, 6 - k * 2, 6 - k * 2, "w", "w", "y")

    def breach(cv, k, t):                    # something slips inside: dust drawn in toward the entrance
        r = 18 * (1 - t) + 3
        for j in range(10):
            ang = math.radians(j * 36 + k * 28)
            cv.blob(c0 + math.cos(ang) * r, c0 + math.sin(ang) * r * 0.8, 1.6, 1.4, "s" if j % 2 else "t", "c", "S",
                    a=int(120 + 120 * t))
        cv.blob(c0, c0, 3 + t * 2, 2.4 + t * 1.6, "k", a=int(200 * t))
        if k == 5:
            cv.ring(c0, c0, 8, "r", 1, ry=6.5)

    s.tag("Hit", frames(5, hit), 60, repeat=1)
    s.tag("HitHeavy", frames(6, heavy), 65, repeat=1)
    s.tag("Breach", frames(6, breach), 70, repeat=1)
    return [s]


# ================================================================ status overlays
def build_status_overlays():
    """Loops drawn over a one-unit enemy, centred on it. The Frost Ant's ice crust
    (FxTowerFrostAura / Freeze) is the fifth of the family."""
    n, c0, cy = 32, 16, 16.5
    s = Sprite("FxStatusOverlay", "Fx", n, n, ["FX"], "Worn by an enemy while a status lasts")

    def frames(count, draw):
        out = []
        for k in range(count):
            cv = Cv(n, n)
            draw(cv, k)
            out.append({"FX": cv.im})
        return out

    def flame(cv, x, y, h, k):
        cv.blob(x, y, 1.8, 1.6, "r", "o", "R")
        for j in range(int(h)):
            w = 1 if j > h * 0.5 else 2
            lean = ((j + k) % 3) - 1 if j > 1 else 0
            for dx in range(w):
                cv.px(x - w / 2 + dx + lean * 0.5, y - 1 - j, "o" if j < h * 0.6 else "y")
        cv.px(x - 0.5, y - 0.5, "y")
        cv.px(x - 0.5, y - 1.5, "w" if k % 2 == 0 else "y")

    def burn(cv, k):
        for j, (dx, dy, h) in enumerate(((-7, 3, 5), (-2, -4, 6), (4, 4, 5), (7, -2, 4), (-5, -2, 4))):
            flame(cv, c0 + dx, cy + dy, h + ((k + j) % 2), k + j)
        for j in range(4):   # embers drifting up
            y = cy - 6 - ((k * 2 + j * 3) % 8)
            cv.px(c0 - 6 + j * 4 + (k % 2), y, "y" if j % 2 else "o", a=255 - ((k * 2 + j * 3) % 8) * 25)

    def poison(cv, k):
        cv.ring(c0, cy, 9, "g", 1, a=70 + (k % 2) * 40, ry=7)
        for j, (dx, dy, r) in enumerate(((-6, 2, 1.8), (1, -3, 2.2), (6, 3, 1.5), (-2, 5, 1.4), (5, -5, 1.6))):
            rise = (k + j) % 4
            y = cy + dy - rise * 1.5
            if rise == 3:        # the bubble pops
                cv.pxs([(c0 + dx - 2, y), (c0 + dx + 1, y), (c0 + dx - 1, y - 2), (c0 + dx, y + 1)], "g")
                continue
            cv.ring(c0 + dx, y, r + 0.5, "g", 1)
            cv.disc(c0 + dx, y, r - 0.4, "G", a=150)
            cv.px(c0 + dx - 1, y - 1, "w")
        for j, dx in enumerate((-8, 8)):     # drips off the sides
            cv.px(c0 + dx, cy + 2 + (k + j * 2) % 4, "G")
            cv.px(c0 + dx, cy + 1 + (k + j * 2) % 4, "g")

    def web(cv, k):
        a = 235 if k == 0 else 205      # a slow shimmer
        for ang in range(0, 180, 30):
            ca, sa = math.cos(math.radians(ang + 8)), math.sin(math.radians(ang + 8))
            cv.line([(c0 - ca * 11, cy - sa * 9), (c0 + ca * 11, cy + sa * 9)], "c", a=a)
        for r in (4.5, 8.5):
            pts = [(c0 + math.cos(math.radians(d + 8)) * r, cy + math.sin(math.radians(d + 8)) * r * 0.82) for d in range(0, 361, 30)]
            cv.line(pts, "w" if r < 5 else "s", a=a)
        for dx, dy in ((-4, -3), (5, 2), (-1, 6)):     # dew caught on the threads
            cv.px(c0 + dx, cy + dy, "b" if k == 0 else "w")

    def honey(cv, k):
        # a see-through glaze, so the enemy still shows under it
        cv.blob(c0, cy - 1, 9, 6.5, "u", "y", "Y", a=95)
        cv.ring(c0, cy - 1, 9, "u", 1, a=230, ry=6.5)
        for j, (dx, length) in enumerate(((-7, 4), (-3, 6), (2, 5), (6, 7))):
            drop = length + ((k + j) % 4) - 1
            for d in range(drop):
                cv.px(c0 + dx, cy + 2 + d, "u", a=230)
            cv.blob(c0 + dx + 0.5, cy + 2.5 + drop, 1.3, 1.3, "u", "y", "Y")
        cv.pxs([(c0 - 4, cy - 5), (c0 - 3, cy - 5), (c0 - 5, cy - 4)], "w")

    s.tag("Burn", frames(4, burn), 90)
    s.tag("Poison", frames(4, poison), 150)
    s.tag("Web", frames(2, web), 400)
    s.tag("Honey", frames(4, honey), 200)
    return [s]


# ================================================================ map dressing
def decor(name, size, caste):
    return Sprite("Decor" + name, "Props", size, size, ["Shadow", "Body"], caste)


def still(s, tag, draw):
    L = s.new()
    draw(L)
    s.tag(tag, [flat(L)], 1000)


def swaying(s, tag, draw, ms=240):
    frames = []
    for k in range(4):
        L = s.new()
        draw(L, (0, 1, 0, -1)[k])
        frames.append(flat(L))
    s.tag(tag, frames, ms)


def blade(cv, x, base_y, height, lean, col, tip):
    cv.line([(x, base_y), (x + lean * 0.4, base_y - height * 0.55), (x + lean, base_y - height)], col)
    cv.px(x + lean, base_y - height, tip)


def leaf(cv, cx, cy, length, width, angle, cols):
    """A top-down leaf: a pointed oval along `angle` with a midrib. Drawn on its own canvas and
    pasted, so its outline never thickens whatever is already on cv."""
    base_c, hi, lo = cols
    one = Cv(cv.w, cv.h)
    ca, sa = math.cos(math.radians(angle)), math.sin(math.radians(angle))
    for y in range(cv.h):
        for x in range(cv.w):
            dx, dy = x + 0.5 - cx, y + 0.5 - cy
            u, v = (dx * ca + dy * sa) / (length / 2), (-dx * sa + dy * ca) / (width / 2)
            if abs(u) <= 1 and abs(v) <= (1 - u * u) ** 0.7:
                one.px(x, y, hi if v < -0.25 else lo if v > 0.45 else base_c)
    one.outline()
    half = length / 2
    one.line([(cx - ca * (half - 1), cy - sa * (half - 1)), (cx + ca * (half + 1.5), cy + sa * (half + 1.5))], lo)
    cv.paste(one)


def thick_line(cv, pts, cols):
    base_c, hi, lo = cols
    for x, y in base.seg(pts):
        cv.px(x, y, base_c)
        cv.px(x, y + 1, lo)
    for x, y in base.seg(pts)[::3]:
        cv.px(x, y, hi)


def build_decor():
    out = []

    # ---- grass tufts
    s = decor("Grass", 16, "Grass tufts")
    tufts = {
        "A": [(5, 4, -2, "M"), (10, 5, 2, "M"), (7, 7, -1, "m"), (9, 6, 1, "m")],
        "B": [(4, 3, -1, "M"), (7, 5, 0, "m"), (11, 4, 2, "M"), (9, 3, 1, "m"), (6, 4, -2, "M")],
        "C": [(7, 9, -1, "m"), (9, 8, 1, "M"), (8, 6, 0, "m")],
    }
    for tag, blades in tufts.items():
        def draw(L, sway, blades=blades):
            L["Shadow"].blob(8, 13.5, 5, 1.6, (0, 0, 0), a=60)
            for x, hgt, lean, col in blades:
                blade(L["Body"], x, 13, hgt, lean + sway * (1 if hgt > 4 else 0), col, "g")
            L["Body"].line([(5, 13), (10, 13)], "j")
        swaying(s, tag, draw)
    out.append(s)

    # ---- flowers
    s = decor("Flower", 16, "Flowers")
    for tag, petal, heart in (("A", "w", "y"), ("B", "y", "o"), ("C", "f", "c")):
        def draw(L, sway, petal=petal, heart=heart):
            L["Shadow"].blob(8, 13.5, 4, 1.4, (0, 0, 0), a=60)
            b = L["Body"]
            b.line([(8, 13), (8, 9), (8 + sway, 6)], "M")
            b.pxs([(7, 11), (9, 10)], "m")
            x, y = 8 + sway, 5
            head = Cv(16, 16)
            head.pxs([(x - 2, y), (x + 2, y), (x, y - 2), (x, y + 2), (x - 1, y - 1), (x + 1, y - 1), (x - 1, y + 1), (x + 1, y + 1),
                      (x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)], petal)
            head.px(x, y, heart)
            head.outline()
            b.paste(head)
        swaying(s, tag, draw, ms=280)
    out.append(s)

    # ---- pebbles
    s = decor("Pebble", 16, "Pebbles")
    stones = {
        "A": [(8, 9, 3.6, 2.8)],
        "B": [(5.5, 9, 2.6, 2.1), (10.5, 10.5, 2.0, 1.6)],
        "C": [(5, 7, 1.8, 1.5), (10, 8, 2.2, 1.7), (7.5, 11.5, 1.6, 1.3)],
    }
    for tag, group in stones.items():
        def draw(L, group=group):
            for x, y, rx, ry in group:
                L["Shadow"].blob(x + 0.5, y + ry * 0.7, rx + 0.8, ry * 0.7, (0, 0, 0), a=70)
                L["Body"].blob(x, y, rx, ry, "S", "s", "x")
            L["Body"].outline()
            for x, y, rx, ry in group:
                L["Body"].px(x - rx * 0.4, y - ry * 0.5, "c")
        still(s, tag, draw)
    out.append(s)

    # ---- fallen leaves
    s = decor("Leaf", 24, "Fallen leaves")
    for tag, cols, angle, size in (("A", ("m", "g", "M"), 25, (15, 8)), ("B", ("o", "u", "R"), -35, (14, 8)),
                                   ("C", ("T", "t", "d"), 55, (13, 9))):
        def draw(L, cols=cols, angle=angle, size=size):
            L["Shadow"].blob(12.5, 13.5, size[0] * 0.5, size[1] * 0.55, (0, 0, 0), a=60)
            leaf(L["Body"], 12, 12, size[0], size[1], angle, cols)
        still(s, tag, draw)
    out.append(s)

    # ---- twigs
    s = decor("Twig", 24, "Twigs")
    for tag, pts, spur in (("A", [(3, 15), (10, 12), (20, 9)], [(10, 12), (13, 7)]),
                           ("B", [(5, 6), (11, 11), (19, 17)], [(11, 11), (16, 9)])):
        def draw(L, pts=pts, spur=spur):
            body = Cv(24, 24)
            thick_line(body, spur, ("d", "T", "D"))
            thick_line(body, pts, ("d", "T", "D"))
            body.outline()
            L["Shadow"].paste(fade(tint(base.shift(body.im, 1, 2), "k", 1.0, keep_ink=False), 0.3))
            L["Body"].paste(body)
        still(s, tag, draw)
    out.append(s)

    # ---- roots breaking the surface
    s = decor("Root", 32, "Roots")
    for tag, main, branches in (
            ("A", [(2, 20), (9, 15), (17, 17), (24, 12), (30, 13)], [[(9, 15), (12, 9), (17, 6)], [(17, 17), (20, 23), (26, 26)]]),
            ("B", [(4, 6), (10, 12), (14, 20), (22, 24), (29, 22)], [[(10, 12), (17, 10), (23, 6)], [(14, 20), (9, 25)]])):
        def draw(L, main=main, branches=branches):
            body = Cv(32, 32)
            for br in branches:
                thick_line(body, br, ("D", "d", "e"))
            for dy in (0, 1):
                thick_line(body, [(x, y + dy) for x, y in main], ("D", "d", "e"))
            body.outline()
            L["Shadow"].paste(fade(tint(base.shift(body.im, 1, 2), "k", 1.0, keep_ink=False), 0.3))
            L["Body"].paste(body)
        still(s, tag, draw)
    out.append(s)

    # ---- mushrooms, seen from above
    s = decor("Mushroom", 16, "Mushrooms")

    def red(L):
        L["Shadow"].blob(8.5, 10.5, 5, 3.5, (0, 0, 0), a=70)
        L["Body"].blob(8, 8, 4.6, 4.2, "r", "f", "R")
        L["Body"].outline()
        L["Body"].pxs([(6, 6), (9, 7), (7, 10), (10, 9), (10, 5)], "c")

    def brown(L):
        for x, y, r in ((5.5, 9.5, 2.8), (10.5, 7, 3.2), (9.5, 11.5, 1.8)):
            L["Shadow"].blob(x + 0.5, y + 1.8, r + 0.5, r * 0.7, (0, 0, 0), a=70)
            cap = Cv(16, 16)
            cap.blob(x, y, r, r * 0.9, "t", "c", "T")
            cap.outline()
            cap.px(x - 0.5, y - 0.5, "T")
            L["Body"].paste(cap)

    still(s, "A", red)
    still(s, "B", brown)
    out.append(s)

    # ---- trail edges: laid along the road so its border is not a ruled line
    s = decor("TrailEdge", 32, "Trail edge pieces (the trail runs left to right under them)")

    def overhang(L):                         # grass spilling over the edge
        rng = random.Random(2)
        for x in range(1, 31, 2):
            hgt = rng.randint(3, 7)
            blade(L["Body"], x, 18 + rng.randint(-1, 1), hgt, rng.choice((-1, 0, 1)), "M" if x % 4 else "m", "g")
        L["Body"].line([(0, 18), (31, 18)], "j")

    def crumble(L):                          # dirt crumbs scattered out onto the grass
        rng = random.Random(6)
        for _ in range(34):
            x = rng.randint(1, 30)
            y = 19 - abs(rng.gauss(0, 4.5))  # thick against the trail, thinning out into the grass
            L["Body"].blob(x, y, rng.uniform(1.0, 2.2), rng.uniform(0.9, 1.6), DIRT if rng.random() < 0.6 else "d", None, "D")

    def stones(L):                           # a line of small stones along the border
        rng = random.Random(9)
        x = 2.5
        while x < 30:
            rx = rng.uniform(1.4, 2.4)
            y = 15 + rng.uniform(-1.5, 1.5)
            L["Shadow"].blob(x + 0.5, y + 1.5, rx + 0.5, 1.2, (0, 0, 0), a=70)
            one = Cv(32, 32)
            one.blob(x, y, rx, rx * 0.8, "S", "s", "x")
            one.outline()
            L["Body"].paste(one)
            x += rx * 2 + rng.uniform(1.5, 4)

    still(s, "A", overhang)
    still(s, "B", crumble)
    still(s, "C", stones)
    out.append(s)
    return out


# ================================================================ run
def main():
    groups = [("enemy moves", build_enemy_moves()), ("hill", build_hill_hit()), ("decor", build_decor()),
              ("status", build_status_overlays())]
    results = []
    for label, sprites in groups:
        for s in sprites:
            results.append((s, export(s)))
            n = sum(len(f) for _, f, _, _ in s.tags)
            print(f"{s.group:8} {s.name:20} {s.w}x{s.h}  {n:2} frames  " + ", ".join(t[0] for t in s.tags))
    base.preview_sheet([r for r in results if r[0].group == "Enemies" or r[0].name == "FxHillHit"],
                       os.path.join(HERE, "EnemyMovesPreview.png"))
    contact_sheet(groups[2][1], os.path.join(HERE, "DecorPreview.png"), scale=4, cols=4)
    contact_sheet(groups[3][1], os.path.join(HERE, "StatusOverlayPreview.png"), scale=5, cols=1)
    total = sum(sum(len(f) for _, f, _, _ in s.tags) for s, _ in results)
    print(f"{len(results)} sprites, {total} frames written and read back OK")


if __name__ == "__main__":
    main()
