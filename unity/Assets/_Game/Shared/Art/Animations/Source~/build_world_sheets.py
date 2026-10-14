"""Hold the Hill world and interface sheets.

  Props/PropQueen        the queen (48x48): Idle, Alarm, Cheer, Fall
  Props/PropChamber      her chamber inside the hill (64x64): Idle, Breached
  Props/PickupFood       food dropped on the field (16x16): Crumb, Seed, Berry, Honey
  Fx/FxPickup            a pickup appearing and being collected (16x16): Spawn, Collect
  Ui/UiWaveBanner        the banner that announces a wave (96x32): In, Hold, Out. No lettering:
                         the game draws the wave number over the blank middle.
  Ui/UiVictory           end-of-run scene, the hill standing (96x64): Intro, Loop
  Ui/UiDefeat            end-of-run scene, the hill fallen (96x64): Intro, Loop
  Ui/UiCursor            mouse cursors (16x16, hot spot top left): Point, Click, Build, Sell, Deny
  Ui/UiPlaceMarker       the tile under the cursor (32x32): Valid, Invalid, Select, Placed

    python build_world_sheets.py
"""
import math
import os
import random

import build_anim_sheets as base
from build_anim_sheets import Cv, Sprite, SPARK, SPARK_S, export, fade, flat, mirror_x, tint
from build_weapon_sheets import contact_sheet

HERE = os.path.dirname(os.path.abspath(__file__))


def single(name, group, w, h, caste):
    return Sprite(name, group, w, h, ["FX"], caste)


def frames_of(w, h, n, draw):
    out = []
    for k in range(n):
        cv = Cv(w, h)
        draw(cv, k)
        out.append({"FX": cv.im})
    return out


# ================================================================ the queen
def queen(L, bob=0, abd=0.0, tw=(0, 0), head_dy=0, crown=True, crown_at=None, legs=0, col=("a", "A", "z")):
    """Top-down queen facing up on a 48x48 canvas: a worker's plan with a far larger gaster."""
    cx, y0 = 24, 20 + bob
    hy = y0 - 9 + head_dy
    ink, body = Cv(48, 48), Cv(48, 48)
    left = [
        [(cx - 3, y0 - 3), (cx - 8, y0 - 6), (cx - 10, y0 - 11 - legs)],
        [(cx - 3, y0 - 1), (cx - 9, y0 - 1), (cx - 13, y0 + 2 + legs)],
        [(cx - 3, y0 + 1), (cx - 8, y0 + 4), (cx - 12, y0 + 10 - legs)],
    ]
    for leg in left:
        ink.line(leg, "k")
        ink.line(mirror_x(leg, cx), "k")
    for side, t in ((-1, tw[0]), (1, tw[1])):
        pts = [(cx - 3, hy - 3), (cx - 6, hy - 7), (cx - 5 - t, hy - 11)]
        ink.line(pts if side < 0 else mirror_x(pts, cx), "k")
    m = [(cx - 3, hy - 4), (cx - 2, hy - 6)]
    ink.line(m, "k")
    ink.line(mirror_x(m, cx), "k")

    b, hi, lo = col
    body.blob(cx, y0 + 14, 8.5 + abd, 10.5 + abd, b, hi, lo)      # gaster
    body.blob(cx, y0 + 3, 1.4, 1.2, b)                            # petiole
    body.blob(cx, y0 - 2, 3.2, 4.2, b, hi, lo)                    # thorax
    body.blob(cx, hy, 4.6, 3.8, b, hi, lo)                        # head
    body.outline()
    for j, dy in enumerate((9, 13, 17)):                          # pale bands across the gaster
        half = (7, 8, 6)[j] + round(abd)
        body.line([(cx - half, y0 + dy + round(abd)), (cx + half - 1, y0 + dy + round(abd))], "T" if j != 1 else "t")
    body.px(cx - 4, hy - 1, "k")
    body.px(cx + 3, hy - 1, "k")
    body.pxs([(cx - 5, y0 + 7), (cx - 6, y0 + 9)], hi)
    ink.paste(body)
    L["Body"].paste(ink)
    if crown:
        x, y = crown_at if crown_at else (cx, hy - 3)
        c = Cv(48, 48)
        c.pxs([(x - 3, y), (x - 2, y), (x - 1, y), (x, y), (x + 1, y), (x + 2, y),
               (x - 3, y - 1), (x - 1, y - 1), (x, y - 1), (x + 2, y - 1), (x - 3, y - 2), (x + 2, y - 2), (x - 1, y - 2), (x, y - 2)], "y")
        c.pxs([(x - 2, y), (x + 1, y)], "r")
        c.px(x - 1, y - 2, "w")
        c.outline()
        L["Body"].paste(c)


def build_queen():
    s = Sprite("PropQueen", "Props", 48, 48, ["Shadow", "Body", "FX"], "The queen")

    def shadow(L, a=85):
        L["Shadow"].blob(24, 34, 15, 13, (0, 0, 0), a=a)

    idle = []
    for k in range(4):
        L = s.new()
        shadow(L)
        queen(L, abd=(0, 0.4, 0.8, 0.4)[k], tw=((0, 0), (1, 0), (1, 1), (0, 1))[k])
        idle.append(flat(L))
    s.tag("Idle", idle, 220)

    alarm = []
    for k in range(6):
        L = s.new()
        shadow(L)
        queen(L, bob=(0, -1, 0, -1, 0, 0)[k], tw=((2, 2), (2, 2), (1, 2), (2, 1), (1, 1), (0, 0))[k],
              head_dy=(-1, -1, 0, -1, 0, 0)[k], legs=(1, -1, 1, -1, 0, 0)[k])
        if k < 5:
            fx = L["FX"]
            for x in (9, 38):                                     # exclamation marks either side
                fx.line([(x, 4 - (k % 2)), (x, 9 - (k % 2))], "r")
                fx.px(x, 11 - (k % 2), "r")
            fx.ring(24, 11, 9 + k * 1.5, "r", 1, a=200 - k * 40)
        alarm.append(flat(L))
    s.tag("Alarm", alarm, 90, repeat=1)

    cheer = []
    for k in range(6):
        L = s.new()
        shadow(L)
        queen(L, bob=(0, -2, -3, -2, 0, 0)[k], tw=((1, 1), (2, 2), (2, 2), (1, 1), (0, 0), (0, 0))[k],
              abd=(0, 0.3, 0.6, 0.3, 0, 0)[k])
        fx = L["FX"]
        for j in range(5):
            ang = math.radians(200 + j * 35 + k * 8)
            r = 14 + k * 1.5
            if (j + k) % 2 == 0 and k < 5:
                fx.spr(SPARK if j % 2 else SPARK_S, 24 + math.cos(ang) * r - 2, 16 + math.sin(ang) * r - 2)
        cheer.append(flat(L))
    s.tag("Cheer", cheer, 90, repeat=1)

    fall = []
    for k in range(7):
        L = s.new()
        shadow(L, a=85 - k * 6)
        grey = (0, 0.1, 0.25, 0.4, 0.5, 0.6, 0.65)[k]
        drop = (0, 0, 2, 5, 8, 9, 9)[k]                           # the crown slips off and rolls away
        queen(L, bob=(0, 1, 1, 2, 2, 2, 2)[k], abd=-(0, 0.2, 0.5, 0.8, 1.0, 1.0, 1.0)[k], head_dy=(0, 1, 2, 2, 3, 3, 3)[k],
              tw=(-1, -1), legs=(0, -1, -2, -2, -2, -2, -2)[k], crown_at=(24 + drop * 1.4, 8 + drop))
        if grey:
            body = L["Body"]
            L["Body"] = Cv(48, 48).paste(tint(body.im, "S", grey))
        if 1 <= k <= 4:
            for j in range(4):
                L["FX"].blob(12 + j * 8, 40 - k, 2.2 - k * 0.3, 2 - k * 0.3, "s", "c", "S", a=200 - k * 40)
        fall.append(flat(L))
    s.tag("Fall", fall, 110, repeat=1)
    return s


def build_chamber():
    s = Sprite("PropChamber", "Props", 64, 64, ["Shadow", "Body", "FX"], "The queen's chamber, inside the hill")
    rng = random.Random(12)
    eggs = [(rng.uniform(10, 24), rng.uniform(36, 50)) for _ in range(9)] + [(rng.uniform(42, 54), rng.uniform(38, 50)) for _ in range(6)]
    fungi = [(9, 16), (54, 18), (50, 52), (13, 54)]

    def room(L, k, alarm=False):
        body = Cv(64, 64)
        body.blob(32, 32, 30, 28, "D", "d", "e")                  # the earth around the room
        body.outline()
        body.blob(32, 32, 25, 23, "e", "D", "k")                  # the hollow
        body.blob(32, 33, 22, 20, "v")
        for ang in range(0, 360, 40):                             # roots and pebbles in the wall
            x, y = 32 + math.cos(math.radians(ang)) * 27, 32 + math.sin(math.radians(ang)) * 25
            body.px(x, y, "S" if ang % 80 else "T")
        body.blob(32, 30, 11, 8, "D", "d", "e")                   # the dais the queen rests on
        body.ring(32, 30, 11, "T", 1, ry=8)
        L["Body"].paste(body)
        eg = Cv(64, 64)
        for j, (x, y) in enumerate(eggs):
            eg.blob(x, y, 1.9, 1.5, "c", "w", "t")
        eg.outline()
        for j, (x, y) in enumerate(eggs):
            if (j + k) % 5 == 0:
                eg.px(x - 0.5, y - 1, "w")
        L["Body"].paste(eg)
        for j, (x, y) in enumerate(fungi):                        # glow-fungus lamps
            glow = (0, 1, 2, 1)[(k + j) % 4]
            col = ("i", "I", "iz") if not alarm else ("r", "f", "R")
            L["FX"].disc(x, y, 5 + glow, col[0], a=45 + glow * 15)
            cap = Cv(64, 64)
            cap.blob(x, y, 2.4, 2, *col)
            cap.outline()
            L["FX"].paste(cap)
            L["FX"].px(x - 1, y - 1, "w")

    idle = []
    for k in range(4):
        L = s.new()
        room(L, k)
        idle.append(flat(L))
    s.tag("Idle", idle, 260)
    breached = []
    for k in range(4):
        L = s.new()
        room(L, k, alarm=True)
        L["Body"] = Cv(64, 64).paste(tint(L["Body"].im, "r", 0.08 + 0.08 * (k % 2)))
        for j in range(6):                                        # dirt shaken from the ceiling
            x = 12 + j * 8 + (k * 3 + j) % 4
            y = 14 + (k * 7 + j * 11) % 36
            L["FX"].px(x, y, "t" if j % 2 else "d")
            L["FX"].px(x, y - 1, "d", a=140)
        breached.append(flat(L))
    s.tag("Breached", breached, 140)
    return s


# ================================================================ pickups
def build_pickups():
    s = Sprite("PickupFood", "Props", 16, 16, ["Shadow", "Body", "FX"], "Food on the field")

    def crumb(b):
        b.blob(8, 9, 3.6, 3, "t", "c", "T")
        b.outline()
        b.pxs([(6, 8), (9, 10), (10, 8)], "T")

    def seed(b):
        for y in range(5, 13):
            half = (1, 2, 3, 3, 3, 2, 2, 1)[y - 5]
            for x in range(8 - half, 8 + half):
                b.px(x, y, "d" if x < 8 else "D")
        b.outline()
        b.line([(8, 5), (8, 11)], "e")
        b.px(6, 7, "T")

    def berry(b):
        b.blob(8, 9.5, 3.6, 3.4, "r", "f", "R")
        b.outline()
        b.pxs([(7, 5), (8, 5), (9, 4)], "M")
        b.px(6, 8, "w")

    def honey(b):
        b.blob(8, 10, 3.4, 2.6, "u", "y", "Y")
        b.blob(8, 7.5, 2, 2.4, "u", "y", "Y")
        b.outline()
        b.px(7, 6, "w")
        b.px(6, 9, "y")

    for tag, draw in (("Crumb", crumb), ("Seed", seed), ("Berry", berry), ("Honey", honey)):
        frames = []
        for k in range(4):
            L = s.new()
            lift = (0, 1, 2, 1)[k]
            L["Shadow"].blob(8, 13.5, 4 - lift * 0.4, 1.5, (0, 0, 0), a=80 - lift * 10)
            body = Cv(16, 16)
            draw(body)
            L["Body"].paste(body, 0, -lift)
            if k == 2:
                L["FX"].spr(SPARK_S, 11, 2)
            frames.append(flat(L))
        s.tag(tag, frames, 170)

    fx = single("FxPickup", "Fx", 16, 16, "A pickup appearing and being collected")

    def spawn(cv, k):
        r = (2, 4, 6, 7)[k]
        cv.ring(8, 9, r, "c", 1, a=255 - k * 55, ry=r * 0.7)
        for j in range(4):
            ang = math.radians(j * 90 + 45)
            cv.px(8 + math.cos(ang) * (r + 1), 9 + math.sin(ang) * (r + 1) * 0.7, "t", a=255 - k * 50)

    def collect(cv, k):
        y = 9 - k * 2
        a = 255 - k * 40
        cv.line([(7, y - 2), (7, y + 2)], "y", a=a)                # a rising plus
        cv.line([(5, y), (9, y)], "y", a=a)
        cv.px(7, y, "w", a=a)
        for j in range(4):
            ang = math.radians(j * 90 + k * 30)
            cv.px(8 + math.cos(ang) * (2 + k * 1.4), 10 + math.sin(ang) * (2 + k * 1.4), "c" if j % 2 else "y", a=a)

    fx.tag("Spawn", frames_of(16, 16, 4, spawn), 60, repeat=1)
    fx.tag("Collect", frames_of(16, 16, 5, collect), 60, repeat=1)
    return [s, fx]


# ================================================================ wave banner
def build_banner():
    w, h = 96, 32
    s = single("UiWaveBanner", "Ui", w, h, "Wave announcement banner (blank middle for the number)")

    def ribbon(cv, open_, flutter=0):
        """open_ 0..1: how far the leaf scroll has unrolled from the centre."""
        half = int(8 + 36 * open_)
        cx, top, bottom = 48, 9, 23
        r = Cv(w, h)
        for x in range(cx - half, cx + half):
            for y in range(top, bottom):
                edge = y in (top, bottom - 1)
                r.px(x, y, "M" if edge else "m" if y < top + 4 else "g" if (x + y) % 9 == 0 and y < top + 7 else "m")
            r.px(x, bottom - 2, "M")
        if open_ > 0.6:                                           # forked tails
            for side in (-1, 1):
                x0 = cx + side * half
                for d in range(6):
                    for y in range(top + 3 + (flutter if side < 0 else -flutter), bottom + 3 + (flutter if side < 0 else -flutter)):
                        if abs(y - (top + bottom) / 2 - 3) > d - 1 or d < 3:
                            r.px(x0 + side * d - (1 if side < 0 else 0), y, "M" if d < 2 else "j")
        for side in (-1, 1):                                      # the rolled ends
            x0 = cx + side * half
            r.blob(x0, (top + bottom) / 2, 2.5, 8.5, "T", "t", "d")
        r.outline()
        cv.paste(r)
        if open_ >= 1:
            for x in (cx - 30, cx + 29):                          # ant-head studs
                cv.blob(x + 0.5, 16, 2.2, 2, "a", "A", "z")
                cv.pxs([(x - 1, 13), (x + 1, 13)], "k")
            cv.line([(cx - 22, 12), (cx + 21, 12)], "g", a=120)

    s.tag("In", frames_of(w, h, 5, lambda cv, k: ribbon(cv, (0.0, 0.3, 0.65, 1.08, 1.0)[k])), 60, repeat=1)
    s.tag("Hold", frames_of(w, h, 4, lambda cv, k: ribbon(cv, 1.0, (0, 1, 0, -1)[k])), 200)
    s.tag("Out", frames_of(w, h, 4, lambda cv, k: ribbon(cv, (1.0, 0.65, 0.3, 0.0)[k])), 60, repeat=1)
    return s


# ================================================================ end-of-run scenes
def scene_hill(cv, k, standing=True, rise=1.0):
    w, h = 96, 64
    ground = 50
    for y in range(ground, h):                                    # grass strip
        for x in range(w):
            cv.px(x, y, "M" if (x * 7 + y * 3) % 11 else "m")
    cv.line([(0, ground), (w - 1, ground)], "j")
    hill = Cv(w, h)
    if standing:
        height = 30 * rise
        hill.blob(48, ground, 30, height, "T", "t", "d")
        hill.blob(48, ground - height * 0.45, 16, height * 0.5, "t", "c", "T")
    else:
        hill.blob(40, ground + 1, 30, 11, "D", "d", "e")
        hill.blob(62, ground, 16, 7, "d", "T", "D")
    for y in range(ground, h):                                    # cut the mound off at the ground line
        for x in range(w):
            hill.p[x, y] = (0, 0, 0, 0)
    hill.outline()
    cv.paste(hill)
    if standing and rise >= 1:
        cv.blob(48, 44, 4, 4.5, "k")                              # the entrance
        cv.blob(48, 43.5, 2.6, 3, "e")
        for x, y in ((30, 44), (63, 40), (40, 30), (56, 26)):
            cv.px(x, y, "c")


def build_victory():
    w, h = 96, 64
    s = single("UiVictory", "Ui", w, h, "End-of-run scene: the hill stands")

    def flag(cv, k, top):
        wave = (0, 1, 0, -1)[k % 4]
        f = Cv(w, h)
        f.line([(47, top), (47, top + 15)], "T")                  # a pale pole, two pixels wide, so it shows against the sky
        f.line([(48, top), (48, top + 15)], "d")
        for row in range(8):                                      # a banner that ripples from the pole outward
            for col in range(13):
                y = top + 1 + row + (wave if col > 6 else 0) + (1 if col > 10 and wave else 0)
                f.px(49 + col, y, "R" if row == 7 or col == 12 else "f" if row == 0 else "r")
        for dx, dy in ((4, 3), (5, 3), (4, 4), (5, 4), (5, 2), (4, 5)):   # a small gold seed
            f.px(49 + dx, top + 1 + dy, "y")
        f.outline()
        cv.paste(f)
        cv.pxs([(47, top - 1), (48, top - 1)], "y")

    def draw(cv, k, intro):
        rise = (0.35, 0.6, 0.85, 1.05, 1.0, 1.0)[k] if intro else 1.0
        if not intro or k >= 3:                                   # sun rays behind the hill
            for j in range(9):
                ang = math.radians(200 + j * 17.5 + (k % 2) * 4)
                cv.line([(48 + math.cos(ang) * 22, 34 + math.sin(ang) * 22), (48 + math.cos(ang) * 46, 34 + math.sin(ang) * 46)],
                        "y", a=110 if j % 2 else 70)
        scene_hill(cv, k, True, rise)
        if rise >= 1:
            flag(cv, k, 6)
            for x, ph in ((22, 0), (33, 2), (64, 1), (75, 3)):    # ants cheering along the ground
                up = (0, 1, 2, 1)[(k + ph) % 4]
                a = Cv(w, h)
                a.blob(x, 50 - up, 2, 1.6, "a", "A", "z")
                a.blob(x, 47.5 - up, 1.5, 1.3, "a", "A", "z")
                a.outline()
                cv.paste(a)
                cv.pxs([(x - 2, 45 - up - (up > 0)), (x + 1, 45 - up - (up > 0))], "k")
            rng = random.Random(k if not intro else 9)
            for j in range(10 if not intro else 4):
                cv.spr(SPARK_S if j % 3 else SPARK, rng.randint(4, 88), rng.randint(2, 30))

    s.tag("Intro", frames_of(w, h, 6, lambda cv, k: draw(cv, k, True)), 90, repeat=1)
    s.tag("Loop", frames_of(w, h, 6, lambda cv, k: draw(cv, k, False)), 160)
    return s


def build_defeat():
    w, h = 96, 64
    s = single("UiDefeat", "Ui", w, h, "End-of-run scene: the hill has fallen")

    def draw(cv, k, intro):
        if intro and k < 3:                                       # the mound sags, then gives way
            scene_hill(cv, k, True, (1.0, 0.8, 0.55)[k])
            cv.im.paste(tint(cv.im, "S", 0.15 * k))
            cv.p = cv.im.load()
        else:
            scene_hill(cv, k, False)
            cv.line([(55, 49), (61, 37)], "D")                    # the snapped flag pole
            cv.pxs([(62, 37), (63, 38), (64, 38), (62, 38), (63, 39)], "R")
            for x, y in ((30, 46), (44, 44), (52, 47), (70, 47)):
                cv.px(x, y, "e")
        phase = k if not intro else k + 2
        for j, x in enumerate((30, 44, 58)):                      # smoke
            for step in range(4):
                y = 40 - step * 8 - (phase * 2 + j * 3) % 8
                if 0 <= y < 44 and (not intro or k >= 2):
                    cv.blob(x + (step % 2) * 3 - 1, y, 3.5 - step * 0.5, 3 - step * 0.4, "S", "s", "x", a=190 - step * 40)
        if intro and 2 <= k <= 4:                                 # dust thrown out as it collapses
            for j in range(8):
                ang = math.radians(190 + j * 23)
                r = 14 + (k - 2) * 9
                cv.blob(48 + math.cos(ang) * r, 48 + math.sin(ang) * r * 0.5, 3 - (k - 2) * 0.6, 2.5 - (k - 2) * 0.5, "t", "c", "T", a=220 - (k - 2) * 60)
        if not intro or k >= 4:
            b = Cv(w, h)                                          # a beetle stands on the ruin
            b.blob(40, 38, 5, 3.4, "h", "H", "hz")
            b.blob(46, 38.5, 2, 2, "hz", "h")
            b.outline()
            cv.paste(b)
            cv.line([(36, 41), (35, 43)], "k")
            cv.line([(41, 41), (41, 43)], "k")
            cv.line([(44, 41), (45, 43)], "k")

    s.tag("Intro", frames_of(w, h, 6, lambda cv, k: draw(cv, k, True)), 110, repeat=1)
    s.tag("Loop", frames_of(w, h, 4, lambda cv, k: draw(cv, k, False)), 220)
    return s


# ================================================================ cursors and placement markers
ARROW = ["k.........", "kk........", "kck.......", "kcck......", "kccck.....", "kcccck....", "kccccck...",
         "kcccccck..", "kccccckkk.", "kcckcck...", "kck.kcck..", "kk..kcck..", ".....kk..."]


def build_cursor():
    s = single("UiCursor", "Ui", 16, 16, "Mouse cursors; the hot spot is the top-left pixel")

    def arrow(cv, dx=0, dy=0, body="c"):
        for j, row in enumerate(ARROW):
            for i, ch in enumerate(row):
                if ch != ".":
                    cv.px(i + dx, j + dy, "k" if ch == "k" else body)

    def badge(cv, kind, k):
        x, y = 11, 11
        b = Cv(16, 16)
        if kind == "build":                                        # a green plus
            b.line([(x, y - 2 - (k % 2)), (x, y + 2 - (k % 2))], "g")
            b.line([(x - 2, y - (k % 2)), (x + 2, y - (k % 2))], "g")
        elif kind == "sell":                                       # a turning coin
            rx = (2.6, 1.6, 0.8, 1.6)[k]
            b.blob(x + 0.5, y + 0.5, rx, 2.6, "y", "w", "Y")
        b.outline()
        cv.paste(b)

    def deny(cv, k):
        cv.ring(8, 8, 6.5, "r", 2)
        cv.line([(4, 4), (11, 11)], "r")
        cv.line([(5, 4), (12, 11)], "r")
        out = Cv(16, 16).paste(cv.im).outline()
        cv.im.paste(out.im)
        cv.p = cv.im.load()
        if k:
            cv.im.paste(tint(cv.im, "w", 0.25))
            cv.p = cv.im.load()

    def click(cv, k):
        arrow(cv, 1 if k == 0 else 0, 1 if k == 0 else 0)
        if k:
            for ang in (200, 245, 290):
                d0, d1 = 2 + k, 4 + k
                cv.line([(1 + math.cos(math.radians(ang)) * -d0, 1 - math.sin(math.radians(ang)) * -d0 - 3),
                         (1 + math.cos(math.radians(ang)) * -d1, 1 - math.sin(math.radians(ang)) * -d1 - 3)], "y")

    s.tag("Point", frames_of(16, 16, 2, lambda cv, k: arrow(cv, body="c" if k == 0 else "w")), 500)
    s.tag("Click", frames_of(16, 16, 3, click), 50, repeat=1)
    s.tag("Build", frames_of(16, 16, 4, lambda cv, k: (arrow(cv), badge(cv, "build", k))), 200)
    s.tag("Sell", frames_of(16, 16, 4, lambda cv, k: (arrow(cv), badge(cv, "sell", k))), 130)
    s.tag("Deny", frames_of(16, 16, 2, deny), 300)
    return s


def build_marker():
    s = single("UiPlaceMarker", "Ui", 32, 32, "The tile under the cursor")

    def brackets(cv, inset, col, dark):
        b = Cv(32, 32)
        lo, hi = inset, 31 - inset
        for cxn, cyn, sx, sy in ((lo, lo, 1, 1), (hi, lo, -1, 1), (lo, hi, 1, -1), (hi, hi, -1, -1)):
            b.line([(cxn, cyn), (cxn + sx * 6, cyn)], col)
            b.line([(cxn, cyn), (cxn, cyn + sy * 6)], col)
            b.line([(cxn + sx, cyn + sy), (cxn + sx * 6, cyn + sy)], dark)
            b.line([(cxn + sx, cyn + sy), (cxn + sx, cyn + sy * 6)], dark)
        b.outline()
        cv.paste(b)

    def valid(cv, k):
        cv.blob(16, 16, 13, 13, "g", a=30 + (k % 2) * 15)
        brackets(cv, (2, 3, 4, 3)[k], "g", "G")

    def invalid(cv, k):
        cv.blob(16, 16, 13, 13, "r", a=35 + (k % 2) * 20)
        brackets(cv, 3, "r", "R")
        x = Cv(32, 32)
        x.line([(11, 11), (20, 20)], "r")
        x.line([(20, 11), (11, 20)], "r")
        x.line([(12, 11), (21, 20)], "R")
        x.line([(21, 11), (12, 20)], "R")
        x.outline()
        cv.paste(x, (0, 1, 0, -1)[k], 0)

    def select(cv, k):
        brackets(cv, (1, 2, 3, 2)[k], "y", "Y")
        if k == 1:
            cv.spr(SPARK_S, 27, 2)

    def placed(cv, k):
        r = 6 + k * 3
        cv.ring(16, 16, r, "c", 2 if k < 2 else 1, a=255 - k * 50)
        for j in range(8):
            ang = math.radians(j * 45 + 22)
            cv.blob(16 + math.cos(ang) * r, 16 + math.sin(ang) * r, 1.6 - k * 0.25, 1.4 - k * 0.2, "t", "c", "T", a=255 - k * 45)
        if k < 3:
            cv.spr(SPARK, 14, 4 - k * 2)

    s.tag("Valid", frames_of(32, 32, 4, valid), 150)
    s.tag("Invalid", frames_of(32, 32, 4, invalid), 90)
    s.tag("Select", frames_of(32, 32, 4, select), 160)
    s.tag("Placed", frames_of(32, 32, 5, placed), 60, repeat=1)
    return s


# ================================================================ run
def main():
    props = [build_queen(), build_chamber()] + build_pickups()
    ui = [build_banner(), build_victory(), build_defeat(), build_cursor(), build_marker()]
    results = []
    for s in props + ui:
        results.append((s, export(s)))
        n = sum(len(f) for _, f, _, _ in s.tags)
        print(f"{s.group:6} {s.name:16} {s.w}x{s.h}  {n:2} frames  " + ", ".join(t[0] for t in s.tags))
    base.preview_sheet(results[:4], os.path.join(HERE, "WorldSheetPreview.png"))
    base.preview_sheet(results[4:], os.path.join(HERE, "UiSheetPreview.png"), scale=2)
    total = sum(sum(len(f) for _, f, _, _ in s.tags) for s, _ in results)
    print(f"{len(results)} sprites, {total} frames written and read back OK")


if __name__ == "__main__":
    main()
