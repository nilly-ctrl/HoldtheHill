"""Hold the Hill story scenes and wave cards.

Larger pictures, seen from the side, for the moments between play (group Story, one FX layer):
  StoryFounding (160x90)      Intro: a winged queen comes down out of the dawn and lands.
                              Loop: she digs the first mound as the sun comes up.
  StoryRaidWarning (160x90)   Loop: beetles cresting the ridge at dusk, the hill small in the distance.
  StoryChamber (160x90)       Loop: a cut through the hill: tunnels, the queen on her dais, nurses
                              tending eggs, workers passing, glow-fungus for light.
  UiWaveCard (128x48)         a card that announces what is coming, no lettering: Normal, Swarm,
                              Elite, Boss (the game writes the wave number on the blank right half)

    python build_story_sheets.py
"""
import math
import os
import random

import build_anim_sheets as base
from build_anim_sheets import Cv, Sprite, SPARK, SPARK_S, export
from build_scenery_sheets import glow

HERE = os.path.dirname(os.path.abspath(__file__))
W, H = 160, 90


def story(name, caste, w=W, h=H):
    return Sprite(name, "Story", w, h, ["FX"], caste)


def tag(s, name, n, draw, ms, repeat=0):
    frames = []
    for k in range(n):
        cv = Cv(s.w, s.h)
        draw(cv, k)
        frames.append({"FX": cv.im})
    s.tag(name, frames, ms, repeat=repeat)


def bands(cv, colours, top=0, bottom=H):
    """A sky as flat bands, with a dithered row where two meet."""
    n = len(colours)
    step = (bottom - top) / n
    for y in range(top, bottom):
        j = min(n - 1, int((y - top) / step))
        edge = (y - top) - j * step
        for x in range(cv.w):
            c = colours[j]
            if edge < 1 and j > 0 and (x + y) % 2:
                c = colours[j - 1]
            cv.px(x, y, c)


def side_ant(cv, x, y, size=1.0, col=("a", "A", "z"), flip=False, step=0, wings=0.0, crown=False, carry=None):
    """An ant seen from the side, feet on y. x is the middle of its thorax."""
    d = -1 if flip else 1
    a = Cv(cv.w, cv.h)
    for j, lx in enumerate((-2, 0, 2)):
        off = (step if j != 1 else -step) * d
        a.line([(x + lx * size, y - 3 * size), (x + (lx * 1.6) * size + off, y)], "k")
    b = Cv(cv.w, cv.h)
    b.blob(x - d * 5 * size, y - 4.5 * size, 4 * size, 3 * size, *col)       # gaster
    b.blob(x, y - 4.5 * size, 2 * size, 1.8 * size, *col)                   # thorax
    b.blob(x + d * 4 * size, y - 5.5 * size, 2.4 * size, 2.2 * size, *col)  # head
    b.outline()
    a.paste(b)
    a.line([(x + d * 5 * size, y - 7 * size), (x + d * 8 * size, y - 9 * size - step)], "k")
    a.px(x + d * 5 * size, y - 6 * size, "k")
    if wings:
        w = Cv(cv.w, cv.h)
        lift = wings * 6
        w.blob(x - d * 4 * size, y - 8 * size - lift, 6 * size, 2.2 * size, "b", "w", "B", a=190)
        w.blob(x - d * 2 * size, y - 9.5 * size - lift, 4.5 * size, 1.8 * size, "b", "w", "B", a=160)
        a.paste(w)
    if crown:
        cx, cy = x + d * 4 * size, y - 8.5 * size
        a.pxs([(cx - 2, cy), (cx - 1, cy), (cx, cy), (cx + 1, cy), (cx - 2, cy - 1), (cx, cy - 1), (cx + 1, cy - 1), (cx - 2, cy - 2), (cx + 1, cy - 2)], "y")
    if carry:
        c = Cv(cv.w, cv.h)
        c.blob(x + d * 7 * size, y - 6 * size, 2.2 * size, 1.8 * size, *carry)
        c.outline()
        a.paste(c)
    cv.paste(a)


def beetle_side(cv, x, y, size=1.0, col=("h", "H", "hz"), step=0, silhouette=False):
    b = Cv(cv.w, cv.h)
    cols = ("k", "k", "k") if silhouette else col
    for lx in (-4, 0, 4):
        b.line([(x + lx * size, y - 2 * size), (x + lx * size + (step if lx else -step), y)], "k")
    dome = Cv(cv.w, cv.h)
    dome.blob(x, y - 1 * size, 7 * size, 6 * size, *cols)
    for yy in range(int(y - 1 * size), cv.h):                      # flat underside
        for xx in range(cv.w):
            dome.p[xx, yy] = (0, 0, 0, 0)
    dome.blob(x + 7 * size, y - 3 * size, 2.5 * size, 2.2 * size, *cols)
    if not silhouette:
        dome.outline()
    b.paste(dome)
    b.line([(x + 9 * size, y - 4 * size), (x + 12 * size, y - 6 * size - step)], "k")
    cv.paste(b)


def ground(cv, y, col="M", dark="j", tufts=True, seed=1):
    for yy in range(y, cv.h):
        for x in range(cv.w):
            cv.px(x, yy, col if (x * 7 + yy * 3) % 11 else dark)
    cv.line([(0, y), (cv.w - 1, y)], dark)
    if tufts:
        rng = random.Random(seed)
        for _ in range(26):
            x = rng.randrange(cv.w)
            cv.line([(x, y), (x + rng.choice((-1, 0, 1)), y - rng.randint(1, 3))], "m")


# ================================================================ scenes
def build_founding():
    s = story("StoryFounding", "The colony is founded")

    def sky(cv, light):
        dawn = [("#2a2a55", "#5a4a7a", "#b06a6a", "#f0a060", "#ffd890"), ("#3a3a70", "#7a5a8a", "#d07a70", "#ffb870", "#ffe8a8"),
                ("#5a78b8", "#8aa0d0", "#f0b890", "#ffd8a0", "#fff0c8")][light]
        bands(cv, dawn, 0, 66)
        sun_y = 70 - light * 9
        for j in range(9):                                        # rays
            ang = math.radians(200 + j * 17.5)
            cv.line([(80 + math.cos(ang) * 14, sun_y + math.sin(ang) * 14), (80 + math.cos(ang) * (40 + light * 14), sun_y + math.sin(ang) * (40 + light * 14))],
                    "y", a=70 + light * 30)
        cv.disc(80, sun_y, 11, "y")
        cv.disc(80, sun_y, 8, "c")
        ground(cv, 66)

    def intro(cv, k):
        sky(cv, 0)
        t = k / 7
        if k < 6:                                                 # gliding down from the upper left
            x, y = 18 + t * 70, 12 + (t ** 1.6) * 62
            side_ant(cv, x, min(66, y), 1.3, crown=True, wings=1.0 if k % 2 else 0.6, step=0)
        else:                                                     # landed: wings fold and fall away
            side_ant(cv, 82, 66, 1.3, crown=True, wings=0.2 if k == 6 else 0, step=0)
            if k == 7:
                for x in (70, 76):
                    cv.blob(x, 65, 5, 1.2, "b", "w", "B", a=170)
            for j in range(4):
                cv.px(74 + j * 5, 64 - (k - 6) * 2, "t")

    def loop(cv, k):
        sky(cv, 1 + (1 if k >= 3 else 0))
        mound = Cv(W, H)                                          # the first heap of spoil, growing
        hgt = 4 + k * 1.3
        mound.blob(104, 66, 12 + k, hgt, "T", "t", "d")
        for yy in range(66, H):
            for xx in range(W):
                mound.p[xx, yy] = (0, 0, 0, 0)
        mound.outline()
        cv.paste(mound)
        cv.blob(92, 67, 4, 2, "k")                                # the shaft she is digging
        side_ant(cv, 82 + (k % 2), 66, 1.3, crown=True, step=(1, -1)[k % 2])
        for j in range(3):                                        # dirt flicked over her back
            ang = math.radians(250 + j * 22)
            d = 6 + ((k + j) % 3) * 4
            cv.px(94 + math.cos(ang) * d, 62 + math.sin(ang) * d, "d" if j % 2 else "T")
        for x, y in ((12, 60), (150, 58)):                        # the grass is very tall from down here
            for j in range(4):
                cv.line([(x + j * 3, 66), (x + j * 3 + (k % 2) - 1, y - j * 6)], "M")
                cv.line([(x + j * 3 + 1, 66), (x + j * 3 + (k % 2), y - j * 6 + 2)], "m")

    tag(s, "Intro", 8, intro, 130, repeat=1)
    tag(s, "Loop", 6, loop, 200)
    return s


def build_raid():
    s = story("StoryRaidWarning", "Raiders on the ridge")

    def draw(cv, k):
        bands(cv, ["#3a1a30", "#7a2a35", "#c04a30", "#f08a3a", "#ffc860"], 0, 62)
        cv.disc(118, 58, 13, "#ffe8a0")                           # the sun going down behind the hill
        hill = Cv(W, H)                                           # home, far off and small
        hill.blob(126, 62, 18, 12, "k")
        cv.paste(hill)
        cv.line([(126, 44), (126, 50)], "k")
        cv.pxs([(127, 44), (128, 44 + (k % 2)), (129, 44), (127, 45), (128, 45), (127, 46)], "r")
        ridge = Cv(W, H)                                          # the near ridge they come over
        for x in range(W):
            top = 60 + math.sin(x * 0.045) * 7 + math.sin(x * 0.17) * 1.5 - (12 if x < 70 else max(0, 12 - (x - 70) * 0.5))
            for y in range(int(top), H):
                ridge.px(x, y, "#1a0f18")
        cv.paste(ridge)
        for j in range(6):                                        # the column, biggest in front
            x = 8 + j * 15 + (k * 1.2)
            top = 60 + math.sin(x * 0.045) * 7 + math.sin(x * 0.17) * 1.5 - (12 if x < 70 else max(0, 12 - (x - 70) * 0.5))
            beetle_side(cv, x, top + 1, 0.8 + (j % 3) * 0.25, step=(1, -1)[(k + j) % 2], silhouette=True)
            cv.px(x + 6 + (j % 3) * 2, top - 3 - (j % 3), "r")    # an eye catching the light
        for j in range(3):                                        # dust kicked up behind them
            cv.blob(6 + j * 9 - k, 50 - j * 2, 4 - j, 3 - j * 0.6, "#5a2a30", a=150 - j * 30)
        for x in range(0, W, 23):                                 # grass stems in the foreground
            cv.line([(x + 4, H - 1), (x + 6 + (k % 2), H - 16 - (x % 9))], "#0f0810")

    tag(s, "Loop", 6, draw, 170)
    return s


def build_chamber():
    s = story("StoryChamber", "Inside the hill")
    rng = random.Random(21)
    stones = [(rng.randrange(W), rng.randint(24, H - 2), rng.choice("sS")) for _ in range(60)]
    roots = [(rng.randrange(W), rng.randint(3, 6)) for _ in range(14)]

    def tunnel(cv, pts, r=4):
        for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
            n = int(max(abs(x1 - x0), abs(y1 - y0))) + 1
            for i in range(n + 1):
                cv.blob(x0 + (x1 - x0) * i / n, y0 + (y1 - y0) * i / n, r, r * 0.8, "v")

    def draw(cv, k):
        bands(cv, ["#8ac0f0", "#b8dcf8"], 0, 14)
        ground(cv, 14, seed=4)
        for y in range(18, H):                                    # soil, darker with depth
            for x in range(W):
                cv.px(x, y, "d" if y < 34 else "D" if y < 62 else "e")
        hill = Cv(W, H)
        hill.blob(80, 14, 26, 9, "T", "t", "d")
        for yy in range(14, H):
            for xx in range(W):
                hill.p[xx, yy] = (0, 0, 0, 0)
        hill.outline()
        cv.paste(hill)
        for x, y, c in stones:
            cv.px(x, y, c)
        for x, length in roots:
            cv.line([(x, 18), (x + 1, 18 + length), (x - 1, 18 + length * 2)], "T")
        # the diggings
        tunnel(cv, [(80, 8), (80, 30), (60, 40), (56, 56)])
        tunnel(cv, [(80, 30), (110, 38), (128, 50)])
        tunnel(cv, [(56, 56), (30, 60)])
        cv.blob(80, 60, 30, 14, "v")                              # the royal chamber
        cv.blob(128, 54, 16, 9, "v")                              # the nursery
        cv.blob(26, 62, 14, 8, "v")                               # the store
        # light
        for x, y in ((56, 49), (104, 49), (140, 47), (16, 57)):
            glow(cv, x, y, 9 + (k % 2), "I", 1.1)
            cap = Cv(W, H)
            cap.blob(x, y, 2.6, 2, "i", "I", "iz")
            cap.outline()
            cv.paste(cap)
        # the queen on her dais
        cv.blob(80, 72, 16, 3, "D", "d", "e")
        side_ant(cv, 82, 70, 2.1, crown=True, step=0)
        if k in (2, 3):
            cv.spr(SPARK_S, 96, 50)
        # eggs and nurses
        eggs = Cv(W, H)
        for j, (x, y) in enumerate(((120, 60), (125, 61), (130, 60), (135, 61), (123, 58), (132, 58))):
            eggs.blob(x, y - (1 if (j + k) % 5 == 0 else 0), 2.2, 1.7, "c", "w", "t")
        eggs.outline()
        cv.paste(eggs)
        side_ant(cv, 114 + (k % 3), 62, 0.7, col=("#e9b49c", "#fff1d6", "#b9795f"), step=(1, -1)[k % 2])
        side_ant(cv, 140, 62, 0.7, col=("#e9b49c", "#fff1d6", "#b9795f"), flip=True, step=0)
        # the store
        seeds = Cv(W, H)
        for j, (x, y) in enumerate(((18, 67), (23, 68), (28, 67), (33, 68), (21, 65), (30, 65), (25, 63))):
            seeds.blob(x, y, 2.4, 1.7, "d", "T", "D")
        seeds.outline()
        cv.paste(seeds)
        # workers on the move
        along = [(80, 12), (80, 30), (66, 37), (58, 48), (48, 58), (38, 61)]
        for j in range(2):
            t = ((k / 6) + j * 0.5) % 1 * (len(along) - 1)
            i = int(t)
            x = along[i][0] + (along[i + 1][0] - along[i][0]) * (t - i)
            y = along[i][1] + (along[i + 1][1] - along[i][1]) * (t - i)
            side_ant(cv, x, y + 3, 0.7, flip=True, step=(1, -1)[(k + j) % 2], carry=("d", "T", "D"))
        side_ant(cv, 60 + k * 2, 13, 0.7, step=(1, -1)[k % 2])     # one up on the surface

    tag(s, "Loop", 6, draw, 220)
    return s


def build_cards():
    w, h = 128, 48
    s = story("UiWaveCard", "Wave intro card (no lettering)", w, h)
    s.group = "Ui"

    def card(cv, k, kind):
        frame = Cv(w, h)
        rim = {"Normal": "T", "Swarm": "T", "Elite": "y", "Boss": "r"}[kind]
        for y in range(4, 44):
            for x in range(2, 126):
                corner = (x in (2, 125)) and (y in (4, 43))
                if not corner:
                    frame.px(x, y, rim if x in (2, 125) or y in (4, 43) else "D" if y == 5 else "k" if y == 42 else "e")
        frame.outline()
        cv.paste(frame)
        for y in range(8, 40):                                    # the picture well on the left
            for x in range(6, 46):
                cv.px(x, y, "#3a2a1e" if kind != "Boss" else ("#4a1a1a" if (k % 2) else "#3a1414"))
        cv.line([(48, 9), (48, 38)], rim)
        for x in (52, 120):                                       # studs beside the blank lettering area
            cv.px(x, 8, rim)
            cv.px(x, 39, rim)
        bob = (0, 1, 0, -1)[k]
        if kind == "Normal":
            beetle_side(cv, 24, 34, 1.5, step=(1, -1)[k % 2])
        elif kind == "Swarm":
            for j, (x, y) in enumerate(((14, 36), (26, 30), (36, 37), (20, 24), (34, 22))):
                beetle_side(cv, x, y + ((k + j) % 2), 0.6, col=("L", "l", "Lz"), step=(1, -1)[(k + j) % 2])
        elif kind == "Elite":
            beetle_side(cv, 24, 34, 1.5, col=("R", "r", "v"), step=(1, -1)[k % 2])
            cv.ring(24, 27, 13 + (k % 2), "y", 1, a=200)
            cv.spr(SPARK_S, 36, 10 + bob)
        else:
            beetle_side(cv, 22, 37, 2.2, col=("V", "P", "Lz"), step=(1, -1)[k % 2])
            cv.pxs([(38, 28), (39, 28)], "r")
            for j in range(3):                                    # a crown of horns
                cv.line([(14 + j * 6, 24), (13 + j * 6, 18 + bob)], "y")
            cv.ring(24, 26, 17 + (k % 2) * 2, "r", 1, a=160)

    for kind in ("Normal", "Swarm", "Elite", "Boss"):
        tag(s, kind, 4, lambda cv, k, kind=kind: card(cv, k, kind), 180)
    return s


# ================================================================ run
def main():
    sprites = [build_founding(), build_raid(), build_chamber(), build_cards()]
    results = []
    for s in sprites:
        results.append((s, export(s)))
        n = sum(len(f) for _, f, _, _ in s.tags)
        print(f"{s.group:6} {s.name:18} {s.w}x{s.h}  {n:2} frames  " + ", ".join(t[0] for t in s.tags))
    base.preview_sheet(results, os.path.join(HERE, "StorySheetPreview.png"), scale=2)
    total = sum(sum(len(f) for _, f, _, _ in s.tags) for s, _ in results)
    print(f"{len(results)} sprites, {total} frames written and read back OK")


if __name__ == "__main__":
    main()
