"""Hold the Hill story scenes, part two, and character portraits.

  Story/StoryVictory (160x90)        Loop: morning after; the hill stands, the colony celebrates
  Story/StoryDefeat (160x90)         Loop: night and rain; the hill is broken and raiders pick over it
  Story/StoryBossBeetle (160x90)     Intro, Loop: the titan beetle arrives
  Story/StoryBossMantis (160x90)     Intro, Loop: the mantis queen rises out of the grass
  Story/StoryBossHornet (160x90)     Intro, Loop: the hornet comes down out of the sun
  Ui/UiPortrait (48x48)              one tag per character, each a 4-frame loop with a blink:
                                     Queen and the 19 tower castes

    python build_story_more_sheets.py
"""
import math
import os
import random

import build_anim_sheets as base
from build_anim_sheets import Cv, Sprite, SPARK, SPARK_S, export
from build_extra_sheets import thick_line
from build_story_sheets import H, W, bands, beetle_side, ground, side_ant, story, tag

HERE = os.path.dirname(os.path.abspath(__file__))


def hill_side(cv, x, base_y, rx, ry, cols=("T", "t", "d"), broken=False):
    m = Cv(cv.w, cv.h)
    if broken:
        m.blob(x - rx * 0.25, base_y + 1, rx, ry * 0.4, "D", "d", "e")
        m.blob(x + rx * 0.5, base_y, rx * 0.5, ry * 0.3, "d", "T", "D")
    else:
        m.blob(x, base_y, rx, ry, *cols)
        m.blob(x, base_y - ry * 0.45, rx * 0.55, ry * 0.5, cols[1], "c", cols[0])
    for yy in range(base_y, cv.h):
        for xx in range(cv.w):
            m.p[xx, yy] = (0, 0, 0, 0)
    m.outline()
    cv.paste(m)
    if not broken:
        cv.blob(x, base_y - 5, 4, 4.5, "k")


def flag(cv, x, top, k, col="r"):
    cv.line([(x, top), (x, top + 14)], "T")
    cv.line([(x + 1, top), (x + 1, top + 14)], "d")
    wave = (0, 1, 0, -1)[k % 4]
    f = Cv(cv.w, cv.h)
    for row in range(7):
        for c in range(11):
            f.px(x + 2 + c, top + 1 + row + (wave if c > 5 else 0), "R" if row == 6 or c == 10 else col)
    f.outline()
    cv.paste(f)


# ================================================================ endings
def build_victory():
    s = story("StoryVictory", "The hill stands")

    def draw(cv, k):
        bands(cv, ["#6aa8e8", "#8cc0f0", "#b8dcf8", "#e8f4d8", "#fff4c0"], 0, 66)
        cv.disc(30, 22, 10, "y")
        cv.disc(30, 22, 7, "c")
        for j in range(8):
            ang = math.radians(j * 45 + k * 6)
            cv.line([(30 + math.cos(ang) * 13, 22 + math.sin(ang) * 13), (30 + math.cos(ang) * 19, 22 + math.sin(ang) * 19)], "y")
        for cx, cy in ((90, 14), (132, 24)):                      # clouds
            cv.blob(cx + k * 0.6, cy, 12, 4, "w")
            cv.blob(cx + 8 + k * 0.6, cy - 3, 7, 4, "w")
        ground(cv, 66)
        hill_side(cv, 96, 66, 44, 34)
        flag(cv, 96, 16, k)
        side_ant(cv, 96, 33, 1.2, crown=True, step=0)             # the queen on the summit
        for j, x in enumerate((22, 38, 54, 136, 150)):            # the colony, jumping
            up = (0, 2, 4, 2)[(k + j) % 4]
            side_ant(cv, x, 66 - up, 0.8, flip=x > 96, step=(1, -1)[(k + j) % 2])
        rng = random.Random(k)
        for _ in range(16):                                       # petals thrown in the air
            cv.px(rng.randrange(W), rng.randrange(4, 60), rng.choice("fyrgw"))
        for x, y in ((70, 20), (124, 14)):
            if k % 2:
                cv.spr(SPARK, x, y)

    tag(s, "Loop", 6, draw, 180)
    return s


def build_defeat():
    s = story("StoryDefeat", "The hill has fallen")
    rain = [(random.Random(j).randrange(W), random.Random(99 + j).randrange(H)) for j in range(46)]

    def draw(cv, k):
        bands(cv, ["#0c0f1e", "#141a30", "#1e2644", "#2a3050"], 0, 66)
        cv.disc(128, 18, 8, "#c8d4e8")                            # a thin moon behind cloud
        cv.blob(126 + k, 20, 14, 4, "#1e2644")
        ground(cv, 66, col="#1e3a2a", dark="#12241c", tufts=False)
        hill_side(cv, 88, 66, 44, 34, broken=True)
        cv.line([(96, 62), (104, 46)], "D")                       # the snapped pole, its flag in the mud
        cv.pxs([(105, 46), (106, 47), (107, 47), (105, 47)], "R")
        for j in range(3):                                        # smoke
            for stp in range(4):
                y = 54 - stp * 9 - (k * 2 + j * 3) % 9
                if y > 4:
                    cv.blob(66 + j * 16 + (stp % 2) * 3, y, 4 - stp * 0.6, 3.4 - stp * 0.5, "#3a3e50", a=190 - stp * 40)
        beetle_side(cv, 70, 60, 1.3, step=(1, -1)[k % 2], silhouette=True)   # raiders on the ruin
        beetle_side(cv, 118, 66, 1.0, step=(1, -1)[(k + 1) % 2], silhouette=True)
        cv.px(79, 55, "r")
        cv.px(125, 62, "r")
        cv.blob(22, 65, 5, 1.6, "#2a1c14")                        # one who did not get away
        for x, y in rain:
            yy = (y + k * 15) % H
            cv.line([((x - k * 3) % W, yy), ((x - k * 3 - 1) % W, yy + 3)], "#6a80b0", a=170)

    tag(s, "Loop", 6, draw, 150)
    return s


# ================================================================ boss arrivals
def build_boss_scenes():
    out = []

    # ---- titan beetle: fills the frame, the ground shakes
    s = story("StoryBossBeetle", "The titan beetle arrives")

    def beetle(cv, k, intro=None):
        bands(cv, ["#2a1830", "#4a2440", "#7a3048", "#b04a40"], 0, 66)
        ground(cv, 66, col="#3a4a2a", dark="#28341e")
        x = 40 if intro is None else -60 + intro * 20              # walks in from the left
        shake = (0, 1, 0, -1)[k % 4] if intro is None or intro >= 4 else 0
        big = Cv(W, H)
        beetle_side(big, x, 66 + shake, 5.5, col=("V", "P", "Lz"), step=(2, -2)[k % 2])
        cv.paste(big)
        for dx in (-24, -10, 4, 18):                              # ribs down the wing-case, so it is not a bare dome
            top = 66 - 33 * math.sqrt(max(0.0, 1 - (dx / 38.5) ** 2)) + 3
            cv.line([(x + dx, top + shake), (x + dx + 2, 58 + shake)], "Lz")
            cv.line([(x + dx + 1, top + 1 + shake), (x + dx + 2, top + 9 + shake)], "p")
        cv.line([(x + 30, 42 + shake), (x + 32, 59 + shake)], "y")       # a gold band where the head joins
        cv.line([(x + 31, 42 + shake), (x + 33, 59 + shake)], "Y")
        for side in (0, 1):                                       # jaws
            thick_line(cv, [(x + 50, 52 + side * 4 + shake), (x + 60, 50 + side * 9 + shake), (x + 66, 54 + side * 4 + shake)], ("P", "p", "V"))
        thick_line(cv, [(x + 60, 38 + shake), (x + 80, 30 + shake), (x + 90, 18 + shake)], ("y", "w", "Y"))   # the horn
        cv.blob(x + 44, 44 + shake, 2, 2, "r")
        cv.px(x + 44, 43 + shake, "w")
        hill_side(cv, 140, 66, 14, 10)                            # the hill, tiny beside it
        if intro is None or intro >= 4:
            for j in range(6):                                    # stones jumping off the ground
                cv.px(8 + j * 26 + (k % 2), 64 - ((k + j) % 3) * 2, "s")
            cv.ring(x + 20, 66, 30 + (k % 3) * 8, "c", 1, a=120, ry=4)

    tag(s, "Intro", 6, lambda cv, k: beetle(cv, k, intro=k), 140, repeat=1)
    tag(s, "Loop", 4, beetle, 170)
    out.append(s)

    # ---- mantis queen: rises out of tall grass
    s = story("StoryBossMantis", "The mantis queen rises")

    def mantis(cv, k, intro=None):
        bands(cv, ["#12281e", "#1e4030", "#3a6a44", "#7aa860"], 0, 66)
        ground(cv, 66, tufts=False)
        rise = 1.0 if intro is None else min(1.0, intro / 4)
        y0 = 66 + (1 - rise) * 44
        sway = (0, 1, 0, -1)[k % 4] if intro is None else 0
        body = Cv(W, H)
        body.blob(93 + sway * 0.5, y0 - 10, 8, 18, "m", "g", "M")        # folded wings and abdomen behind her
        body.line([(93 + sway * 0.5, y0 - 26), (94 + sway * 0.5, y0 + 6)], "M")
        thick_line(body, [(86, y0), (82 + sway, y0 - 26), (80 + sway, y0 - 44)], ("M", "g", "j"))        # the long thorax
        for dx in (-1, 2):
            thick_line(body, [(86 + dx, y0), (82 + dx + sway, y0 - 26), (80 + dx + sway, y0 - 44)], ("M", "g", "j"))
        body.blob(80 + sway, y0 - 50, 8, 6, "m", "g", "M")        # the head, a wide triangle
        body.outline()
        cv.paste(body)
        for side in (-1, 1):                                      # eyes and the folded, praying forelegs
            cv.blob(80 + sway + side * 6, y0 - 51, 2.4, 2.4, "y" if intro is None or intro >= 4 else "Y")
            cv.px(80 + sway + side * 6, y0 - 51, "k")
            spread = 0 if intro is not None and intro < 5 else (0, 2, 4, 2)[k % 4]
            elbow = (82 + sway + side * (10 + spread), y0 - 30)
            tip = (82 + sway + side * (6 + spread * 2), y0 - 14)
            thick_line(cv, [(82 + sway + side * 2, y0 - 34), elbow, tip], ("g", "w", "M"))
            for j in range(3):
                cv.px(elbow[0] + (tip[0] - elbow[0]) * (0.3 + j * 0.25) + side * 2, elbow[1] + (tip[1] - elbow[1]) * (0.3 + j * 0.25), "c")
        cv.pxs([(78 + sway, y0 - 57), (80 + sway, y0 - 58), (82 + sway, y0 - 57)], "y")   # her crown
        for x in range(0, W, 5):                                  # grass in front of her
            h = 14 + (x * 7) % 18
            lean = ((x // 5 + k) % 3) - 1
            cv.line([(x, H - 1), (x + lean, H - 1 - h)], "M")
            cv.line([(x + 1, H - 1), (x + 1 + lean, H - h)], "j")
        if intro is None and k % 2:
            cv.spr(SPARK_S, 70, 12)

    tag(s, "Intro", 6, lambda cv, k: mantis(cv, k, intro=k), 150, repeat=1)
    tag(s, "Loop", 4, mantis, 200)
    out.append(s)

    # ---- hornet: out of the sun
    s = story("StoryBossHornet", "The hornet comes down out of the sun")

    def hornet(cv, k, intro=None):
        bands(cv, ["#f8d070", "#f0a850", "#e08040", "#b85838"], 0, 66)
        cv.disc(112, 20, 16, "w")
        cv.disc(112, 20, 12, "c")
        ground(cv, 66, col="#5a6a30", dark="#3e4a22")
        hill_side(cv, 30, 66, 20, 14)
        flag(cv, 30, 38, k)
        t = 1.0 if intro is None else intro / 5
        size = 0.5 + t * 2.0                                      # grows as it comes closer
        x, y = 112 - t * 26, 20 + t * 22 + ((0, 1, 0, -1)[k % 4] if intro is None else 0)
        dark = intro is not None and intro < 2                    # at first only a shape against the sun
        cols = ("k", "k", "k") if dark else ("o", "u", "R")
        b = Cv(W, H)
        for side, a in ((-1, 150), (-1, 110)):                    # wings, a blur above the back
            wing = Cv(W, H)
            beat = (k + (a == 110)) % 2
            wing.blob(x + 2 * size, y - (5 + beat * 3) * size, 8 * size, (2 + beat) * size, "c", "w", "u", a=a)
            cv.paste(wing)
        b.blob(x + 8 * size, y + 1 * size, 7 * size, 4.2 * size, *cols)      # abdomen, toward the viewer's right
        b.blob(x, y, 3.6 * size, 3.4 * size, "k" if dark else "R", "k" if dark else "r", "k" if dark else "v")
        b.blob(x - 5 * size, y + 0.5 * size, 3 * size, 3 * size, *cols)
        b.outline()
        cv.paste(b)
        if not dark:
            for j in range(3):
                bx = x + (5 + j * 3) * size
                cv.line([(bx, y - 2.5 * size), (bx, y + 4 * size)], "k")
            cv.blob(x - 6 * size, y - 0.5 * size, 1.2 * size, 1.2 * size, "k")
        cv.line([(x + 15 * size, y + 2 * size), (x + 19 * size, y + 4 * size)], "k")      # the sting
        for j in range(3):
            cv.line([(x - (1 - j) * 2 * size, y + 3 * size), (x - (2 - j) * 2 * size, y + 7 * size)], "k")
        if intro is None:
            for j in range(4):                                    # the downdraft flattening the grass
                cv.line([(52 + j * 8, 64), (48 + j * 8 - (k % 2) * 2, 60)], "m")

    tag(s, "Intro", 6, lambda cv, k: hornet(cv, k, intro=k), 130, repeat=1)
    tag(s, "Loop", 4, hornet, 120)
    out.append(s)
    return out


# ================================================================ portraits
# name, body colours, head width, what marks them out
CASTES = [
    ("Queen", ("a", "A", "z"), 1.15, "crown"),
    ("Spitter", ("a", "A", "z"), 1.0, "acid"), ("Seeker", ("a", "A", "z"), 1.0, "redeyes"), ("Bombardier", ("a", "A", "z"), 1.05, "pebble"),
    ("Slinger", ("a", "A", "z"), 0.95, "sling"), ("StormAnt", ("a", "A", "z"), 1.0, "sparks"), ("DewdropLens", ("a", "A", "z"), 1.0, "lens"),
    ("SwarmNest", ("d", "T", "D"), 1.0, "swarm"), ("FrostAnt", ("N", "n2", "Nz"), 1.0, "frost"), ("Kicker", ("q", "Q", "Z"), 1.1, "none"),
    ("Sapper", ("a", "A", "z"), 1.0, "mine"), ("Worker", ("a", "A", "z"), 0.95, "pebble"), ("Soldier", ("q", "Q", "Z"), 1.15, "jaws"),
    ("Major", ("a", "A", "z"), 1.3, "jaws"), ("Nurse", ("#e9b49c", "#fff1d6", "#b9795f"), 0.95, "heart"),
    ("Honeypot", ("u", "y", "Y"), 1.0, "honey"), ("Weaver", ("M", "m", "j"), 1.0, "silk"), ("Scout", ("d", "T", "D"), 0.9, "lens"),
    ("QueensGuard", ("v", "L", "k"), 1.25, "gold"), ("FungusFarmer", ("a", "A", "z"), 1.0, "fungus"),
]


def build_portraits():
    n, c0 = 48, 24
    s = Sprite("UiPortrait", "Ui", n, n, ["FX"], "Character portraits, one tag each")

    def portrait(cv, k, col, wide, mark):
        back = Cv(n, n)                                           # a round frame
        back.blob(c0, c0, 22, 22, "t", "c", "T")                  # pale, so a dark ant stands out against it
        back.outline()
        back.ring(c0, c0, 22, "d", 2)
        back.ring(c0, c0, 19.5, "T", 1)
        cv.paste(back)
        tw = (0, 1, 0, -1)[k]
        ink = Cv(n, n)
        for side in (-1, 1):                                      # antennae, elbowed
            ink.line([(c0 + side * 6, 15), (c0 + side * 12, 8), (c0 + side * (15 + tw * side), 3)], "k")
            ink.line([(c0 + side * 7, 15), (c0 + side * 13, 8)], "k")
        face = Cv(n, n)
        face.blob(c0, 26, 13 * wide, 11, *col)                    # the head, seen from the front
        face.blob(c0, 42, 9 * wide, 7, *col)                      # shoulders
        face.outline()
        ink.paste(face)
        cv.paste(ink)
        blink = k == 3
        for side in (-1, 1):                                      # eyes
            ex = c0 + side * 6 * wide
            if blink:
                cv.line([(ex - 2, 24), (ex + 2, 24)], "k")
            else:
                cv.blob(ex, 24, 3, 3.6, "k")
                cv.px(ex - 1, 22, "w")
                if mark == "redeyes":
                    cv.px(ex, 24, "r")
            jaw_open = 2 if mark == "jaws" and k == 1 else 0      # mandibles
            jaw = [(c0 + side * 4, 34), (c0 + side * (7 + jaw_open), 38), (c0 + side * (2 + jaw_open), 41)]
            thick_line(cv, jaw, (col[2], col[1], "k") if mark != "jaws" else ("c", "w", "s"))
        cv.line([(c0 - 3, 33), (c0 + 2, 33)], col[2])
        # what marks the caste out
        if mark == "crown":
            cr = Cv(n, n)
            for x in range(c0 - 7, c0 + 7):
                cr.px(x, 14, "y")
                cr.px(x, 13, "y")
            for x in (c0 - 7, c0 - 3, c0, c0 + 3, c0 + 6):
                cr.line([(x, 12), (x, 9)], "y")
            cr.outline()
            cv.paste(cr)
            cv.pxs([(c0 - 3, 13), (c0 + 3, 13)], "r")
        elif mark == "acid":
            cv.blob(c0, 40 + (k % 2), 2.4, 2.6, "g", "w", "G")
        elif mark in ("pebble", "mine"):
            p = Cv(n, n)
            p.blob(c0, 40, 4, 3.2, *(("S", "s", "x") if mark == "pebble" else ("h", "H", "hz")))
            p.outline()
            cv.paste(p)
            if mark == "mine" and k % 2:
                cv.px(c0, 39, "r")
        elif mark == "sling":
            cv.line([(c0 - 10, 38), (c0, 43), (c0 + 10, 38)], "T")
        elif mark == "sparks":
            for side in (-1, 1):
                cv.spr(SPARK_S, c0 + side * 15 - 1, 2 + (k % 2))
        elif mark == "lens":
            lens = Cv(n, n)
            lens.blob(c0 + 9, 22, 5, 5, "b", "w", "B", a=200)
            lens.outline("n")
            cv.paste(lens)
        elif mark == "swarm":
            for j in range(3):
                ang = math.radians(j * 120 + k * 30)
                cv.blob(c0 + math.cos(ang) * 17, 20 + math.sin(ang) * 10, 1.6, 1.2, "a", "A", "z")
        elif mark == "frost":
            cv.spr(base.SNOW, c0 - 3, 4)
        elif mark == "heart":
            hx, hy = c0 + 12, 8 - (k % 2)
            cv.pxs([(hx, hy + 1), (hx + 1, hy), (hx + 2, hy + 1), (hx + 3, hy), (hx + 4, hy + 1), (hx + 1, hy + 2), (hx + 2, hy + 2),
                    (hx + 3, hy + 2), (hx + 2, hy + 3), (hx + 1, hy + 1), (hx + 3, hy + 1)], "f")
        elif mark == "honey":
            cv.blob(c0 - 4, 37, 1.6, 2.2, "y", "w", "u")
            cv.px(c0 - 4, 40 + (k % 2), "u")
        elif mark == "silk":
            cv.line([(c0, 38), (c0 - 6, 44 + (k % 2)), (c0 - 12, 46)], "c")
            cv.blob(c0, 39, 3, 1.8, "c", "w", "t")
        elif mark == "gold":
            cv.line([(c0 - 9, 17), (c0 + 8, 17)], "y")
            cv.line([(c0 - 7, 16), (c0 + 6, 16)], "y")
            cv.px(c0 - 6, 16, "w")
        elif mark == "fungus":
            cap = Cv(n, n)
            cap.blob(c0, 13, 9, 4.5, "I", "w", "i")
            cap.outline()
            cv.paste(cap)

    for name, col, wide, mark in CASTES:
        tag(s, name, 4, lambda cv, k, col=col, wide=wide, mark=mark: portrait(cv, k, col, wide, mark), 400)
    return s


# ================================================================ run
def main():
    scenes = [build_victory(), build_defeat()] + build_boss_scenes()
    portraits = build_portraits()
    results = []
    for s in scenes + [portraits]:
        results.append((s, export(s)))
        n = sum(len(f) for _, f, _, _ in s.tags)
        print(f"{s.group:6} {s.name:18} {s.w}x{s.h}  {n:2} frames  " + ", ".join(t[0] for t in s.tags))
    # scenes: two frames of each, side by side; portraits: the first frame of each tag in a grid
    from PIL import Image
    sheet = Image.new("RGBA", (W * 2 * 2 + 12, (H * 2 + 6) * len(scenes) + 6), base.rgba("#2a1d14"))
    for i, (s, (rows, comps)) in enumerate(results[:len(scenes)]):
        last = rows[-1]
        for j, index in enumerate((last[1], last[1] + last[2] // 2)):
            sheet.paste(comps[index].resize((W * 2, H * 2), Image.NEAREST), (4 + j * (W * 2 + 4), 6 + i * (H * 2 + 6)))
    sheet.save(os.path.join(HERE, "StoryMorePreview.png"))
    rows, comps = results[-1][1]
    grid = Image.new("RGBA", (10 * 100 + 4, 2 * 100 + 4), base.rgba("#2a1d14"))
    for i, (name, start, count) in enumerate(rows):
        grid.alpha_composite(comps[start].resize((96, 96), Image.NEAREST), (4 + (i % 10) * 100, 4 + (i // 10) * 100))
    grid.save(os.path.join(HERE, "PortraitPreview.png"))
    total = sum(sum(len(f) for _, f, _, _ in s.tags) for s, _ in results)
    print(f"{len(results)} sprites, {total} frames written and read back OK")


if __name__ == "__main__":
    main()
