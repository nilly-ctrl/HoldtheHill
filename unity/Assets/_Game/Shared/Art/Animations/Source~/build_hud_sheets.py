"""Hold the Hill animated interface pieces (group Ui, one FX layer each).

  UiHealthBar (80x16)    Idle, Damage, Heal, Low: the hill's bar, drawn full. The game still owns
                         how full it is; these are the frame and the reactions played over it.
  UiFoodCounter (40x16)  Idle, Gain, Spend, Cant (the number is drawn by the game to the right of the seed)
  UiWaveTrack (96x16)    Idle, Advance, Boss: a trail with a marker walking along it
  UiSkillNode (24x24)    Locked, Available, Unlock, Owned
  UiButton (48x16)       Normal, Hover, Press, Disabled (no lettering)
  UiTooltip (64x32)      Open, Idle, Close (no lettering)
  UiStar (16x16)         Empty, Fill, Full: end-of-run rating
  UiTitleLogo (160x64)   Intro, Loop: "HOLD THE HILL" over a mound, in the project's pixel letters

    python build_hud_sheets.py
"""
import importlib.util
import math
import os

import build_anim_sheets as base
from build_anim_sheets import Cv, Sprite, SPARK, SPARK_S, export, tint

HERE = os.path.dirname(os.path.abspath(__file__))
FONT_SRC = os.path.normpath(os.path.join(HERE, "..", "..", "Fonts", "Source~", "build_pixel_fonts.py"))


def glyphs():
    spec = importlib.util.spec_from_file_location("pixel_fonts", FONT_SRC)
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod.G


def ui(name, w, h, caste):
    return Sprite(name, "Ui", w, h, ["FX"], caste)


def tag(s, name, n, draw, ms, repeat=0):
    frames = []
    for k in range(n):
        cv = Cv(s.w, s.h)
        draw(cv, k)
        frames.append({"FX": cv.im})
    s.tag(name, frames, ms, repeat=repeat)


def box(cv, x0, y0, x1, y1, fill, top, bottom, rim="D"):
    """A rounded pixel panel: ink outline, rim, lit top row, shaded bottom row."""
    b = Cv(cv.w, cv.h)
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            corner = (x in (x0, x1)) and (y in (y0, y1))
            if not corner:
                b.px(x, y, rim if x in (x0, x1) or y in (y0, y1) else top if y == y0 + 1 else bottom if y == y1 - 1 else fill)
    b.outline()
    cv.paste(b)


def heart(cv, x, y, col="r", hi="f"):
    for dx, dy in ((0, 1), (1, 0), (2, 1), (3, 0), (4, 1), (0, 2), (1, 2), (2, 2), (3, 2), (4, 2), (1, 3), (2, 3), (3, 3), (2, 4), (1, 1), (3, 1)):
        cv.px(x + dx, y + dy, col)
    cv.px(x + 1, y + 1, hi)


# ================================================================ pieces
def build_health_bar():
    s = ui("UiHealthBar", 80, 16, "Hill health bar")

    def bar(cv, k, fill=("g", "m", "G"), shake=0, flash=None, sweep=None):
        ox = shake
        box(cv, 12 + ox, 4, 76 + ox, 11, "e", "D", "k")
        for y in range(6, 10):
            for x in range(14 + ox, 75 + ox):
                cv.px(x, y, fill[1] if y == 6 else fill[2] if y == 9 else fill[0])
        for x in range(26 + ox, 75 + ox, 12):                     # notches
            cv.line([(x, 6), (x, 9)], "k", a=110)
        disc = Cv(cv.w, cv.h)                                     # the emblem: a heart on a leaf disc
        disc.blob(8 + ox, 8, 6.5, 6.5, "M", "m", "j")
        disc.outline()
        cv.paste(disc)
        heart(cv, 5 + ox, 5 + (1 if flash else 0))
        if flash is not None:                                     # the chunk just lost, flashing white then red
            x0 = 58
            for y in range(6, 10):
                for x in range(x0 + ox, 75 + ox):
                    cv.px(x, y, "w" if flash == 0 else "r" if flash == 1 else "R", a=255 if flash < 2 else 150)
            if flash >= 1:
                for j in range(4):
                    cv.px(60 + j * 4 + ox, 11 + flash * 1.5 + (j % 2), "r", a=255 - flash * 60)
        if sweep is not None:                                     # a bright band running up the bar
            x0 = 14 + sweep * 12
            for y in range(6, 10):
                for x in range(int(x0), int(min(75, x0 + 8))):
                    cv.px(x + ox, y, "w", a=200 if y in (6, 7) else 120)

    tag(s, "Idle", 4, lambda cv, k: (bar(cv, k), cv.px(20 + k * 14, 6, "w")), 220)
    tag(s, "Damage", 5, lambda cv, k: bar(cv, k, shake=(2, -2, 1, -1, 0)[k], flash=(0, 1, 1, 2, 3)[k] if k < 4 else None), 60, repeat=1)
    tag(s, "Heal", 6, lambda cv, k: (bar(cv, k, sweep=k), cv.spr(SPARK_S, 4 + (k % 2) * 6, 0) if k < 5 else None), 70, repeat=1)
    tag(s, "Low", 4, lambda cv, k: bar(cv, k, fill=("r", "f", "R") if k % 2 == 0 else ("R", "r", "v")), 180)
    return s


def build_food_counter():
    s = ui("UiFoodCounter", 40, 16, "Food counter")

    def counter(cv, k, lift=0, scale=0.0, red=False, dx=0):
        box(cv, 1 + dx, 2, 38 + dx, 13, "D", "d", "e")
        seed = Cv(cv.w, cv.h)
        seed.blob(9 + dx, 8 - lift, 4 + scale, 3.4 + scale, "t", "c", "T")
        seed.outline()
        seed.line([(6 + dx, 8 - lift), (11 + dx, 8 - lift)], "T")
        cv.paste(seed)
        if red:
            cv.im.paste(tint(cv.im, "r", 0.45))
            cv.p = cv.im.load()

    tag(s, "Idle", 4, lambda cv, k: counter(cv, k, lift=(0, 0, 1, 0)[k]), 240)
    tag(s, "Gain", 5, lambda cv, k: (counter(cv, k, lift=(1, 2, 1, 0, 0)[k], scale=(0.6, 1.2, 0.6, 0, 0)[k]),
                                    cv.spr(SPARK_S, 14 + k, 1) if k < 3 else None), 60, repeat=1)
    tag(s, "Spend", 4, lambda cv, k: (counter(cv, k, scale=-(0.4, 0.9, 0.4, 0)[k]),
                                     cv.px(16 + k * 3, 12 + k, "t", a=255 - k * 60) if k < 3 else None), 60, repeat=1)
    tag(s, "Cant", 5, lambda cv, k: counter(cv, k, red=k < 4, dx=(1, -1, 1, -1, 0)[k]), 50, repeat=1)
    return s


def build_wave_track():
    s = ui("UiWaveTrack", 96, 16, "Wave progress track")

    def track(cv, k, marker=30.0, hop=0, boss=False):
        for x in range(6, 90):                                    # the trail
            cv.px(x, 9, "d")
            cv.px(x, 10, "D")
            if x % 6 == 0:
                cv.px(x, 8, "T")
        for j, x in enumerate((6, 27, 48, 69)):                   # wave posts
            done = x <= marker
            cv.line([(x, 5), (x, 10)], "D")
            cv.pxs([(x + 1, 5), (x + 2, 5), (x + 1, 6), (x + 2, 6), (x + 3, 6)], "g" if done else "S")
        flag = Cv(cv.w, cv.h)                                     # the hill at the end
        flag.blob(89, 9, 5, 4, "T", "t", "d")
        flag.outline()
        cv.paste(flag)
        cv.px(89, 9, "k")
        m = Cv(cv.w, cv.h)                                        # the marker: a beetle walking the trail
        if boss:
            m.blob(marker, 7 - hop, 4, 3, "V", "P", "Lz")
            m.px(marker + 4, 6 - hop, "y")
        else:
            m.blob(marker, 7 - hop, 2.8, 2.2, "h", "H", "hz")
        m.outline()
        cv.paste(m)
        cv.px(marker + 1.5, 6.5 - hop, "r" if boss else "k")

    tag(s, "Idle", 4, lambda cv, k: track(cv, k, hop=(0, 0, 1, 0)[k]), 220)
    tag(s, "Advance", 6, lambda cv, k: (track(cv, k, marker=30 + (0, 4, 9, 14, 18, 21)[k], hop=(0, 2, 3, 2, 1, 0)[k]),
                                       cv.spr(SPARK_S, 47, 1) if k >= 4 else None), 70, repeat=1)
    tag(s, "Boss", 4, lambda cv, k: (track(cv, k, marker=72, hop=(0, 1, 0, 1)[k], boss=True),
                                    cv.ring(72, 7, 6 + (k % 2) * 2, "r", 1, a=200 - (k % 2) * 80)), 160)
    return s


def build_skill_node():
    s = ui("UiSkillNode", 24, 24, "Skill tree node")

    def node(cv, k, state):
        cols = {"locked": ("x", "S", "k"), "available": ("d", "T", "D"), "owned": ("Y", "y", "d")}[state]
        n = Cv(24, 24)
        n.blob(12, 12, 9.5, 9.5, *cols)
        n.outline()
        n.ring(12, 12, 7.5, cols[2], 1)
        cv.paste(n)
        if state == "locked":                                     # a padlock
            box(cv, 8, 11, 15, 17, "S", "s", "x", rim="x")
            cv.ring(12, 10.5, 3.2, "s", 1, gaps=lambda d: 20 < d < 160)
            cv.px(11.5, 14, "k")
        else:                                                     # a sprouting seed
            cv.line([(11.5, 17), (11.5, 11)], "M" if state == "available" else "j")
            for side in (-1, 1):
                cv.blob(12 + side * 2.6, 9.5, 2.3, 1.6, "g" if state == "available" else "m", "w" if state == "owned" else "g", "M")
        if state == "available":
            cv.ring(12, 12, 10.5 + (0, 0.5, 1, 0.5)[k], "y", 1, a=(120, 180, 240, 180)[k])
        if state == "owned" and k == 1:
            cv.spr(SPARK_S, 17, 3)

    def unlock(cv, k):
        node(cv, 0, "locked" if k < 2 else "owned")
        if k < 2:
            cv.im.paste(tint(cv.im, "w", 0.4 + k * 0.3))
            cv.p = cv.im.load()
        else:
            r = 6 + (k - 2) * 2.5
            cv.ring(12, 12, r, "y", 2 if k < 4 else 1, a=255 - (k - 2) * 55)
            for j in range(6):
                ang = math.radians(j * 60 + k * 15)
                cv.px(12 + math.cos(ang) * (r + 1), 12 + math.sin(ang) * (r + 1), "w", a=255 - (k - 2) * 50)

    tag(s, "Locked", 1, lambda cv, k: node(cv, k, "locked"), 1000)
    tag(s, "Available", 4, lambda cv, k: node(cv, k, "available"), 160)
    tag(s, "Unlock", 6, unlock, 70, repeat=1)
    tag(s, "Owned", 4, lambda cv, k: node(cv, k, "owned"), 260)
    return s


def build_button():
    s = ui("UiButton", 48, 16, "Button (no lettering)")

    def button(cv, k, state):
        if state == "press":
            box(cv, 1, 3, 46, 14, "d", "D", "T")
        elif state == "disabled":
            box(cv, 1, 1, 46, 13, "S", "s", "x", rim="x")
        else:
            box(cv, 1, 1, 46, 13, "T", "t", "d")
            cv.line([(3, 14), (44, 14)], "k", a=110)              # a soft drop shadow
        if state == "hover":
            x = 4 + k * 9
            for d in range(5):                                    # a glint sliding across
                cv.line([(x + d, 3), (x + d - 3, 11)], "c", a=(200, 255, 255, 200, 110)[d])

    tag(s, "Normal", 1, lambda cv, k: button(cv, k, "normal"), 1000)
    tag(s, "Hover", 5, lambda cv, k: button(cv, k, "hover"), 70)
    tag(s, "Press", 3, lambda cv, k: button(cv, k, "press" if k < 2 else "normal"), 60, repeat=1)
    tag(s, "Disabled", 1, lambda cv, k: button(cv, k, "disabled"), 1000)
    return s


def build_tooltip():
    s = ui("UiTooltip", 64, 32, "Tooltip panel (no lettering)")

    def tip(cv, k, open_=1.0):
        half_w, half_h = int(4 + 26 * open_), int(2 + 9 * open_)
        box(cv, 32 - half_w, 13 - half_h, 31 + half_w, 12 + half_h, "e", "D", "k", rim="T")
        if open_ >= 1:
            for y in range(3):                                    # the pointer
                cv.line([(30 + y, 23 + y), (33 - y, 23 + y)], "e")
                cv.px(29 + y, 23 + y, "T")
                cv.px(34 - y, 23 + y, "T")
            cv.px(31.5, 26, "k")
            cv.pxs([(4, 3), (59, 3)], "t")                        # corner studs

    tag(s, "Open", 4, lambda cv, k: tip(cv, k, (0.1, 0.5, 1.1, 1.0)[k]), 50, repeat=1)
    tag(s, "Idle", 1, lambda cv, k: tip(cv, k), 1000)
    tag(s, "Close", 3, lambda cv, k: tip(cv, k, (1.0, 0.5, 0.1)[k]), 50, repeat=1)
    return s


def build_star():
    s = ui("UiStar", 16, 16, "Rating star")

    def star(cv, k, cols, size=1.0):
        st = Cv(16, 16)
        pts = []
        for j in range(10):
            r = (6.5 if j % 2 == 0 else 2.8) * size
            a = math.radians(-90 + j * 36)
            pts.append((8 + math.cos(a) * r, 8.5 + math.sin(a) * r))
        for y in range(16):                                       # scanline fill of the star polygon
            xs = []
            for (x0, y0), (x1, y1) in zip(pts, pts[1:] + pts[:1]):
                if (y0 <= y + 0.5 < y1) or (y1 <= y + 0.5 < y0):
                    xs.append(x0 + (y + 0.5 - y0) * (x1 - x0) / (y1 - y0))
            xs.sort()
            for a, b in zip(xs[::2], xs[1::2]):
                for x in range(int(round(a)), int(round(b))):
                    st.px(x, y, cols[1] if y < 6 else cols[2] if y > 10 else cols[0])
        st.outline()
        cv.paste(st)

    grey, gold = ("x", "S", "k"), ("y", "w", "Y")
    tag(s, "Empty", 1, lambda cv, k: star(cv, k, grey), 1000)
    tag(s, "Fill", 5, lambda cv, k: (star(cv, k, gold, (0.4, 0.9, 1.25, 1.05, 1.0)[k]),
                                    cv.spr(SPARK_S, 1 + k * 2, 1) if 1 <= k <= 3 else None), 60, repeat=1)
    tag(s, "Full", 4, lambda cv, k: (star(cv, k, gold), cv.px((5, 7, 9, 7)[k], (5, 4, 6, 9)[k], "w")), 200)
    return s


def build_logo():
    w, h = 160, 64
    s = ui("UiTitleLogo", w, h, "Title logo")
    G = glyphs()

    def word(cv, text, x, y, scale, col, shade, drop=0):
        for ch in text:
            rows = G.get(ch)
            if rows is None:
                x += 4 * scale
                continue
            for j, row in enumerate(rows):
                for i, bit in enumerate(row):
                    if bit != ".":
                        for sy in range(scale):
                            for sx in range(scale):
                                cv.px(x + i * scale + sx, y + drop + j * scale + sy, col if j < 4 else shade)
            x += (len(rows[0]) + 1) * scale
        return x

    def width(text, scale):
        return sum(((len(G[ch][0]) + 1) if ch in G else 4) * scale for ch in text) - scale

    def logo(cv, k, intro=None):
        t = 1.0 if intro is None else min(1.0, (intro + 1) / 4)
        mound = Cv(w, h)                                          # the hill the words stand on
        mound.blob(80, 64, 70, 24 * t, "T", "t", "d")
        mound.blob(80, 64, 40, 14 * t, "t", "c", "T")
        mound.outline()
        cv.paste(mound)
        if t >= 1:
            cv.blob(80, 60, 5, 4, "k")
            cv.blob(80, 59.5, 3, 2.4, "e")
        letters = Cv(w, h)
        top, bottom = "HOLD THE", "HILL"
        if intro is None or intro >= 1:
            word(letters, top, (w - width(top, 2)) // 2, 4, 2, "c", "t", drop=0 if intro is None or intro >= 2 else -6)
        if intro is None or intro >= 3:
            word(letters, bottom, (w - width(bottom, 4)) // 2, 20, 4, "y", "u", drop=0 if intro is None or intro >= 4 else -8)
        letters.outline()
        letters.outline("e")                                      # a second, softer rim so it reads on any background
        cv.paste(letters)
        if intro is None:                                         # a glint crossing the big word, and ants on the slope
            x0 = (w - width(bottom, 4)) // 2 + k * 14 - 8
            for d in range(3):
                for y in range(20, 48):
                    xx = x0 + d - (y - 20) // 3
                    if 0 <= xx < w and letters.p[int(xx), y][3] and letters.p[int(xx), y][:3] not in (base.rgba("k")[:3], base.rgba("e")[:3]):
                        cv.px(xx, y, "w")
            for j, bx in enumerate((22, 136)):
                a = Cv(w, h)
                step = (k + j * 2) % 4
                a.blob(bx + step, 56 - (step % 2), 2.4, 1.8, "a", "A", "z")
                a.blob(bx + step + (3 if j == 0 else -3), 55.5 - (step % 2), 1.6, 1.4, "a", "A", "z")
                a.outline()
                cv.paste(a)
        elif intro == 5:
            for x, y in ((30, 10), (126, 14), (80, 2)):
                cv.spr(SPARK, x, y)

    tag(s, "Intro", 6, lambda cv, k: logo(cv, k, intro=k), 110, repeat=1)
    tag(s, "Loop", 8, lambda cv, k: logo(cv, k), 140)
    return s


# ================================================================ run
def main():
    sprites = [build_health_bar(), build_food_counter(), build_wave_track(), build_skill_node(), build_button(),
               build_tooltip(), build_star(), build_logo()]
    results = []
    for s in sprites:
        results.append((s, export(s)))
        n = sum(len(f) for _, f, _, _ in s.tags)
        print(f"{s.group:4} {s.name:16} {s.w}x{s.h}  {n:2} frames  " + ", ".join(t[0] for t in s.tags))
    base.preview_sheet(results[:-1], os.path.join(HERE, "HudSheetPreview.png"), scale=3)
    base.preview_sheet(results[-1:], os.path.join(HERE, "LogoSheetPreview.png"), scale=2)
    total = sum(sum(len(f) for _, f, _, _ in s.tags) for s, _ in results)
    print(f"{len(results)} sprites, {total} frames written and read back OK")


if __name__ == "__main__":
    main()
