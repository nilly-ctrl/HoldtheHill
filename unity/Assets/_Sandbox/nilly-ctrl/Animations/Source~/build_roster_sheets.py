"""Hold the Hill roster sheets: five new enemies and five new tower castes.

Enemies (face right; each file has Walk, Hurt, Death, Attack, Spawn, and one move of its own):
  EnemyWasp (32x32)       a flyer: its shadow sits apart from it. Sting
  EnemySpider (32x32)     Rear (rears up before a bite)
  EnemySnail (40x40)      a slow tank. Hide (pulls into its shell), Unhide
  EnemyGrub (24x24)       Burrow (digs under), Emerge
  EnemyBoss (64x64)       the titan beetle. Roar

Towers (face up; Idle, Attack, Upgrade), each at three tiers (Tower<Name>, ...T2, ...T3) with an
FxTower<Name> file holding its shot: Fly, Trail, Hit, Miss, Muzzle and Fly2/Fly3/Hit2/Hit3.
  TowerHoneypot       a replete, swollen with honey, that lobs slowing globs
  TowerWeaver         holds a silk-spinning larva and throws web
  TowerScout          long antennae; marks a target and flicks thorn darts
  TowerQueensGuard    a gilded major whose snap sends a shock along the ground
  TowerFungusFarmer   carries a fungus cap on its back and puffs spores

    python build_roster_sheets.py
"""
import math
import os
import random

import build_anim_sheets as base
import build_tier_sheets as tiers
import build_weapon_sheets as wpn
from build_anim_sheets import (Cv, Sprite, SPARK_S, CX, TW, beetle, death_frames, export, flat, hurt_frames,
                               idle_kw, mirror_y, shadow)
from build_extra_sheets import LUNGE, chomp, spawn_frames, thick_line

HERE = os.path.dirname(os.path.abspath(__file__))


# ================================================================ enemies
def creature(name, caste, size, body, shadow_r, shadow_dy=1, splat=None, debris=("s", "S"), walk_n=6, walk_ms=90,
             reach=8, extra=None):
    """body(phase, dx=0, strike=0, **pose) -> Cv. extra(s, frame) adds the creature's own tags."""
    s = Sprite(name, "Enemies", size, size, ["Shadow", "Body", "FX"], caste)
    cx, cy = size / 2, size / 2 + 0.5

    def frame(phase=0.0, dx=0, strike=0, shadow_scale=1.0, **pose):
        L = s.new()
        L["Shadow"].paste(shadow(size, size, cx + dx, cy + shadow_dy, shadow_r[0] * shadow_scale, shadow_r[1] * shadow_scale))
        L["Body"].paste(body(phase, dx=dx, strike=strike, **pose))
        return L

    walk = [flat(frame(2 * math.pi * i / walk_n)) for i in range(walk_n)]
    s.tag("Walk", walk, walk_ms)
    s.tag("Hurt", hurt_frames(s, walk[0]), 60, repeat=1)
    s.tag("Death", death_frames(s, walk[0], cy, debris=debris, splat=splat), 80, repeat=1)
    attack = []
    for k in range(6):
        L = frame(0.0, dx=LUNGE[k], strike=k)
        if k in (2, 3):
            chomp(L["FX"], cx + LUNGE[k] + reach, cy - 0.5, k, size)
        attack.append(flat(L))
    s.tag("Attack", attack, 75, repeat=1)
    s.tag("Spawn", spawn_frames(s, walk[0], cx, cy), 80, repeat=1)
    if extra:
        extra(s, frame, cx, cy)
    return s


def build_enemies():
    out = []

    # ---- wasp
    def wasp(phase, dx=0, strike=0, curl=0.0):
        w = h = 32
        cx, cy = 16 + dx, 14.5                                    # flies above its shadow
        ink, body = Cv(w, h), Cv(w, h)
        beat = int(round(phase / (2 * math.pi) * 6)) % 2
        for side in (-1, 1):                                      # wings: long, swept back, beating between two angles
            wing = Cv(w, h)
            spread = 34 if beat else 62                           # degrees out from the body line
            a = math.radians(180 - spread) * side
            for step in range(11):
                t = step / 10
                px_, py_ = cx + 2 + math.cos(a) * step, cy - 0.5 + math.sin(a) * step
                half = 0.6 + math.sin(t * math.pi) * 2.2
                wing.blob(px_, py_, half, half, "b", "w", "B", a=150 if beat else 195)
            wing.line([(cx + 2, cy - 0.5 + side), (cx + 2 + math.cos(a) * 9, cy - 0.5 + math.sin(a) * 9)], "B", a=230)   # the vein
            ink.paste(wing)
        for j, ax in enumerate((3, 1, -1)):                       # legs tucked under
            ink.line([(cx + ax, cy - 1), (cx + ax - 1, cy - 3)], "k")
            ink.line([(cx + ax, cy), (cx + ax - 1, cy + 2)], "k")
        tail = (cx - 11 + curl * 5, cy - 0.5 + curl * 0)          # the sting swings under and forward
        ink.line([(cx - 9, cy - 0.5), tail, (tail[0] - 2 + curl * 4, cy - 0.5)], "k")
        body.blob(cx - 5 + curl * 1.5, cy, 5.5 - curl, 3.4, "y", "c", "Y")   # abdomen
        body.blob(cx + 1.5, cy, 2.6, 2.6, "x", "S", "k")          # thorax
        body.blob(cx + 6, cy, 2.3, 2.5, "y", "c", "Y")            # head
        body.outline()
        for bx in (cx - 8, cx - 5.5, cx - 3):                     # stripes
            body.line([(bx + curl, cy - 3), (bx + curl, cy + 2)], "k")
        body.pxs([(cx + 7, cy - 2), (cx + 7, cy + 1)], "k")
        for side in (-1, 1):
            ink.line([(cx + 7, cy - 0.5 + side), (cx + 10, cy - 0.5 + side * 3)], "k")
        ink.paste(body)
        return ink

    def wasp_extra(s, frame, cx, cy):
        sting = []
        for k in range(6):
            L = frame(k * 1.0, dx=(0, -1, 1, 2, 1, 0)[k], curl=(0.2, 0.6, 1.0, 1.0, 0.5, 0)[k])
            if k in (2, 3):
                L["FX"].line([(cx + 8, cy + 3), (cx + 12, cy + 5)], "g")
                L["FX"].spr(SPARK_S, cx + 11, cy + 3) if k == 2 else None
            sting.append(flat(L))
        s.tag("Sting", sting, 70, repeat=1)

    out.append(creature("EnemyWasp", "Wasp (flies)", 32, wasp, (7, 3), shadow_dy=7, splat="Y", debris=("y", "k"),
                        walk_ms=50, reach=9, extra=wasp_extra))

    # ---- spider
    def spider(phase, dx=0, strike=0, rear=0.0):
        w = h = 32
        cx, cy = 16 + dx, 16.5
        ink, body = Cv(w, h), Cv(w, h)
        # eight long legs, each bent at a high knee: what makes it read as a spider and not a beetle
        for j, (ang, length) in enumerate(((30, 13), (62, 12), (112, 12), (148, 13))):
            step = math.sin(phase + j * math.pi / 2) * 9
            lift = rear * (28 if j == 0 else 0)
            for side in (-1, 1):
                a = math.radians(side * (ang - step * (1 if side < 0 else -1) - lift))
                foot = (cx + 2 + math.cos(a) * length, cy - 0.5 + math.sin(a) * length)
                knee = (cx + 2 + math.cos(a) * length * 0.45, cy - 0.5 + math.sin(a) * length * 0.45 + side * 3.5)
                ink.line([(cx + 2, cy - 0.5 + side), knee, foot], "k")
                ink.px(knee[0], knee[1], "L")
        body.blob(cx - 4.5, cy, 4.8, 4.4, "v", "L", "k")          # abdomen
        body.blob(cx + 2.5, cy, 2.8, 2.6, "Lz", "L", "k")         # cephalothorax
        body.outline()
        body.pxs([(cx - 6, cy - 2), (cx - 5, cy - 1), (cx - 4, cy - 2), (cx - 6, cy + 1), (cx - 5, cy), (cx - 4, cy + 1)], "r")  # hourglass
        body.pxs([(cx + 5, cy - 2), (cx + 5, cy + 1), (cx + 6, cy - 1), (cx + 6, cy)], "r" if strike in (2, 3) else "f")       # eyes
        for side in (-1, 1):                                      # fangs
            ink.line([(cx + 6, cy - 0.5 + side), (cx + 8 + (1 if strike in (2, 3) else 0), cy - 0.5 + side * (1 if strike in (2, 3) else 2))], "c")
        ink.paste(body)
        return ink

    def spider_extra(s, frame, cx, cy):
        s.tag("Rear", [flat(frame(0, dx=(0, -1, -1, -1, 0)[k], rear=(0.3, 0.8, 1, 0.8, 0.2)[k])) for k in range(5)], 90, repeat=1)

    out.append(creature("EnemySpider", "Spider", 32, spider, (11, 6), splat="Lz", debris=("L", "v"), walk_ms=70, reach=9,
                        extra=spider_extra))

    # ---- snail
    def snail(phase, dx=0, strike=0, hide=0.0):
        w = h = 40
        cx, cy = 20 + dx, 20.5
        ink, body = Cv(w, h), Cv(w, h)
        stretch = math.sin(phase) * 1.2
        out_ = 1 - hide
        if out_ > 0.05:                                           # the foot and head
            body.blob(cx + 2 + stretch * 0.5, cy + 1, 13 * out_ + 2, 4.5, "t", "c", "T")
            hx = cx + 13 * out_ + stretch
            body.blob(hx, cy, 3 * out_ + 0.5, 3, "t", "c", "T")
            for side in (-1, 1):                                  # eye stalks
                tip = (hx + 4 * out_, cy - 0.5 + side * (3 + (1 if strike in (2, 3) else 0)))
                ink.line([(hx + 1, cy - 0.5 + side), tip], "T")
                ink.px(tip[0], tip[1], "k")
        shell = Cv(w, h)
        shell.blob(cx - 3, cy - 1, 10, 9, "d", "T", "D")
        shell.outline()
        for r, col in ((7.5, "D"), (5, "T"), (2.5, "D")):         # the spiral
            shell.ring(cx - 3, cy - 1, r, col, 1, gaps=lambda d, r=r: (d + r * 20) % 360 < 40)
        shell.px(cx - 7, cy - 6, "c")
        body.outline()
        ink.paste(body)
        ink.paste(shell)
        return ink

    def snail_extra(s, frame, cx, cy):
        s.tag("Hide", [flat(frame(0, hide=(0.25, 0.5, 0.8, 1.0)[k])) for k in range(4)], 100, repeat=1)
        s.tag("Unhide", [flat(frame(0, hide=(1.0, 0.8, 0.5, 0.2, 0.0)[k])) for k in range(5)], 110, repeat=1)

    out.append(creature("EnemySnail", "Snail (slow, armoured)", 40, snail, (15, 7), splat="T", debris=("d", "T"), walk_n=8,
                        walk_ms=150, reach=16, extra=snail_extra))

    # ---- grub
    def grub(phase, dx=0, strike=0, sink=0.0):
        w = h = 24
        cx, cy = 12 + dx, 12.5
        ink, body = Cv(w, h), Cv(w, h)
        segs = 5
        keep = segs - int(round(sink * segs))                     # hind segments vanish first when burrowing
        seams = []
        for k in range(segs - 1, -1, -1):
            if k >= keep:
                continue
            squeeze = math.sin(phase - k * 1.1) * 0.9
            x = cx + 6 - k * 3.4 + squeeze
            y = cy + math.sin(k * 0.9 + 0.4) * 1.6                # a fat body lying in a curve
            ry = 4.0 - abs(k - 2) * 0.35
            col = ("o", "u", "R") if k == 0 else ("c", "w", "t")
            body.blob(x, y, 2.2 if k == 0 else 2.4, 2.6 if k == 0 else ry, *col)
            if k:
                seams.append((x + 1.9, y, ry))
                ink.line([(x - 0.5, y - ry), (x - 1, y - ry - 2)], "k")      # stubby legs
        body.outline()
        for x, y, ry in seams:                                    # the creases between segments
            body.line([(x, y - ry + 1), (x, y + ry - 2)], "t")
        if keep > 0:
            hy = cy + math.sin(0.4) * 1.6
            body.pxs([(cx + 7, hy - 1.5), (cx + 7, hy + 0.5)], "k")
            ink.pxs([(cx + 9, hy - 2), (cx + 9, hy + 1)] if strike in (2, 3) else [(cx + 8, hy - 1), (cx + 8, hy)], "k")
        ink.paste(body)
        return ink

    def grub_extra(s, frame, cx, cy):
        def dirt(L, k, n):
            for j in range(6):
                ang = math.radians(j * 60 + 15)
                d = 3 + k * 1.3
                L["FX"].blob(cx + math.cos(ang) * d, cy + math.sin(ang) * d * 0.7, 1.7, 1.5, "d", "T", "D", a=250 - k * 30)
            L["Shadow"].blob(cx, cy, 5, 3.5, "e", a=120 + k * 20)
            return L

        s.tag("Burrow", [flat(dirt(frame(k, sink=(0.2, 0.4, 0.6, 0.8, 1.0)[k], shadow_scale=1 - k * 0.2), k, 5)) for k in range(5)], 90, repeat=1)
        s.tag("Emerge", [flat(dirt(frame(k, sink=(1.0, 0.8, 0.6, 0.3, 0.0)[k], shadow_scale=0.2 + k * 0.2), 4 - k, 5)) for k in range(5)], 90, repeat=1)

    out.append(creature("EnemyGrub", "Grub (burrows)", 24, grub, (9, 4), splat="t", debris=("c", "t"), walk_ms=110, reach=8,
                        extra=grub_extra))

    # ---- boss
    def boss(phase, dx=0, strike=0, roar=0.0, charge=0.0, cracked=0):
        w = h = 64
        cx, cy = 30 + dx, 32.5
        b = beetle(w, h, cx, cy, 17, 13, ("V", "P", "Lz"), "plain", phase)
        deco = Cv(w, h)
        hx = cx + 18.5
        if charge:                                                # horn lowered and levelled, like a lance
            thick_line(deco, [(hx, cy - 1), (hx + 10, cy - 1), (hx + 14, cy - 1 - (1 - charge) * 3)], ("y", "w", "Y"))
        else:
            thick_line(deco, [(hx, cy - 1), (hx + 9, cy - 1 - roar * 3), (hx + 12, cy - 5 - roar * 3)], ("y", "w", "Y"))    # the great horn
        for j in range(5):                                        # spines along the wing-cases
            x = cx - 12 + j * 5
            for side in (-1, 1):
                deco.line([(x, cy - 0.5 + side * 11), (x - 1, cy - 0.5 + side * 15)], "y")
        deco.outline()
        b.paste(deco)
        b.line([(cx + 4, cy - 10), (cx + 4, cy + 9)], "y")        # a gold band behind the pronotum
        for j in range(3):                                        # plate lines across the wing-cases
            x = cx - 12 + j * 6
            b.line([(x, cy - 9 + j), (x, cy + 8 - j)], "Lz")
            b.px(x + 1, cy - 8 + j, "p")
        for n, side in ((1, -1), (2, 1)):                         # armour knocked off: the upper wing-case first, then the lower
            if cracked < n:
                continue
            px_, py_ = cx - 5, cy - 0.5 + side * 6
            b.blob(px_, py_, 7.5, 4.2, "f", "c", "R")             # the soft back under the plate
            rng = random.Random(40 + n)
            for j in range(7):                                    # a broken edge
                ang = math.radians(j * 51 + 10)
                ex, ey = px_ + math.cos(ang) * 7.5, py_ + math.sin(ang) * 4.2
                b.line([(ex, ey), (ex + rng.choice((-2, -1, 1, 2)), ey + rng.choice((-1, 1)))], "k")
            b.line([(px_ - 4, py_), (px_ - 1, py_ + side), (px_ + 3, py_)], "R")
        if cracked:
            b.line([(cx + 4, cy - 0.5 - 10), (cx + 4, cy - 0.5 - 6)], "Lz")   # the gold band is broken through
        if cracked > 1:
            b.line([(cx + 4, cy - 0.5 + 6), (cx + 4, cy - 0.5 + 9)], "Lz")
        jaws = Cv(w, h)                                           # jaws sized for the body: they gape, then cross
        gape = 5 if (strike in (0, 1) or roar) else 0.5 if charge else 1.5
        for side in (-1, 1):
            pts = [(hx + 1, cy - 0.5 + side * 3), (hx + 6, cy - 0.5 + side * (5 + gape)), (hx + 11, cy - 0.5 + side * (3 + gape)),
                   (hx + 13, cy - 0.5 + side * gape)]
            thick_line(jaws, pts, ("P", "p", "V"))
            jaws.px(hx + 8, cy - 0.5 + side * (3 + gape), "p")    # a tooth
        jaws.outline()
        b.paste(jaws)
        b.pxs([(cx + 21, cy - 3), (cx + 21, cy + 2), (cx + 22, cy - 3), (cx + 22, cy + 2)], "r")
        return b

    def boss_extra(s, frame, cx, cy):
        roar = []
        for k in range(7):
            L = frame(0, dx=(0, -1, -2, 1, 1, 0, 0)[k], roar=(0.3, 0.7, 1, 1, 0.8, 0.4, 0)[k])
            if k >= 2:
                r = 8 + (k - 2) * 5.5
                L["FX"].ring(cx + 20, cy, r, "c", 2 if k < 5 else 1, a=250 - (k - 2) * 45,
                             gaps=lambda d: 60 < d < 300)             # arcs thrown forward from the jaws
                L["FX"].ring(cx + 20, cy, r - 4, "y", 1, a=200 - (k - 2) * 40, gaps=lambda d: 50 < d < 310)
            roar.append(flat(L))
        s.tag("Roar", roar, 90, repeat=1)

        # the charge: horn down, legs a blur, dust torn up behind
        charge = []
        for k in range(6):
            L = frame(2 * math.pi * k / 3, dx=k % 2, charge=1.0)
            for j in range(5):
                y = cy - 12 + j * 6 + ((k + j) % 2)
                x0 = cx - 24 - ((k * 5 + j * 7) % 9)
                L["FX"].line([(x0, y), (x0 + 5 + (j % 2) * 2, y)], "c" if (j + k) % 2 else "t", a=210)
            for j in range(3):
                pr = 2.4 - ((k + j) % 3) * 0.5
                L["FX"].blob(cx - 22 - ((k * 3 + j * 4) % 7), cy - 9 + j * 9, pr, pr, "d", "T", "D", a=200)
            charge.append(flat(L))
        s.tag("ChargeWindup", [flat(frame(0, dx=(-1, -2, -3, -3)[k], charge=(0.2, 0.5, 0.8, 1.0)[k])) for k in range(4)], 110, repeat=1)
        s.tag("Charge", charge, 55)

        def plate_break(n):                                       # a wing-case plate shatters: flash, shards, the soft back shows
            side = -1 if n == 1 else 1
            rng = random.Random(70 + n)
            shards = [(rng.uniform(-160, -20) * (1 if side < 0 else -1), rng.uniform(0.7, 1.3), rng.choice("VPy")) for _ in range(9)]
            frames = []
            for k in range(7):
                L = frame(0, dx=(0, -2, -1, 0, 0, 0, 0)[k], cracked=n - 1 if k == 0 else n)
                if k == 0:
                    L["Body"].im = base.tint(L["Body"].im, "w", 0.85)
                else:
                    for ang, spd, c in shards:
                        d = (3 + k * 3.4) * spd
                        x, y = cx - 6 + math.cos(math.radians(ang)) * d, cy + side * 6 + math.sin(math.radians(ang)) * d
                        if k < 5:
                            L["FX"].blob(x, y, 1.6 - k * 0.15, 1.3 - k * 0.1, c, "p", "Lz", a=255 - k * 30)
                        else:
                            L["FX"].px(x, y, c, a=255 - k * 35)
                    if k <= 2:
                        L["FX"].ring(cx - 6, cy + side * 6, 5 + k * 4, "w", 2 if k == 1 else 1, a=250 - k * 70)
                frames.append(flat(L))
            return frames

        s.tag("PlateBreak", plate_break(1), 80, repeat=1)
        s.tag("WalkCracked", [flat(frame(2 * math.pi * i / 8, cracked=1)) for i in range(8)], 130)
        s.tag("PlateBreak2", plate_break(2), 80, repeat=1)
        s.tag("WalkBare", [flat(frame(2 * math.pi * i / 8, cracked=2)) for i in range(8)], 115)

    out.append(creature("EnemyBoss", "Titan beetle (boss)", 64, boss, (22, 15), walk_n=8, walk_ms=140, reach=30,
                        extra=boss_extra))
    return out


# ================================================================ towers
def draw_honeypot(L, tag, i, n):
    amber = ("u", "y", "Y")
    squeeze = 0.0
    if tag == "Idle":
        base.ant(L, abd=1.6 + idle_kw(i)["abd"], tw=idle_kw(i)["tw"])
    else:
        squeeze = [0.3, 0.8, -0.6, -0.4, 0, 0.2][i]
        base.ant(L, abd=1.6 + squeeze, head_dy=[0, 1, -1, -1, 0, 0][i], mand=[0, 1, 1, 0, 0, 0][i])
    belly = Cv(TW, TW)                                            # the honey shows through the stretched gaster
    belly.blob(CX, 23.5, 4.2 + squeeze, 4.6 + squeeze, *amber, a=235)
    L["Body"].paste(belly)
    L["Body"].px(CX - 2, 21, "w")
    if tag == "Attack" and i in (2, 3, 4):
        y = 4 - (i - 2) * 3
        g = Cv(TW, TW)
        g.blob(CX, y, 2.4 - (i - 2) * 0.4, 2.4 - (i - 2) * 0.4, *amber)
        g.outline()
        L["FX"].paste(g)
        L["FX"].px(CX - 1, y + 3 + (i - 2), "u")


def draw_weaver(L, tag, i, n):
    col = ("M", "m", "j")
    if tag == "Idle":
        base.ant(L, col=col, **idle_kw(i))
        wig = (0, 1, 0, -1)[i % 4]
    else:
        base.ant(L, col=col, head_dy=[0, 1, -2, -1, 0, 0][i], mand=[1, 2, 1, 1, 0, 0][i], legs=[0, 1, -1, 0, 0, 0][i])
        wig = 0
    larva = Cv(TW, TW)                                            # the larva it squeezes for silk, held in the jaws
    hy = 5 + ([0, 1, -2, -1, 0, 0][i] if tag == "Attack" else 0)
    larva.blob(CX + wig * 0.5, hy - 1, 4, 2.2, "c", "w", "t")
    larva.outline()
    for x in (CX - 2, CX, CX + 2):                               # its segments
        larva.px(x + wig * 0.5, hy - 1, "t")
    larva.px(CX + 3 + wig * 0.5, hy - 2, "k")
    L["FX"].paste(larva)
    if tag == "Idle":                                             # a loop of silk hanging between its jaws and the mound
        L["FX"].line([(CX - 4, hy), (CX - 7, hy + 4 + wig), (CX - 9, hy + 9)], "c", a=210)
    if tag == "Attack" and i in (2, 3, 4):
        for ang in (-110, -90, -70):
            a = math.radians(ang)
            d = 3 + (i - 2) * 3
            L["FX"].line([(CX, hy - 1), (CX + math.cos(a) * d, hy - 1 + math.sin(a) * d)], "c", a=255 - (i - 2) * 60)
        L["FX"].px(CX, hy - 2 - (i - 2) * 3, "w")


def draw_scout(L, tag, i, n):
    col = ("d", "T", "D")
    def kit(head_dy=0, glint=False):
        cloak = Cv(TW, TW)                                        # a leaf worn as a cloak over the gaster
        for y in range(18, 29):
            half = (2, 3, 4, 5, 5, 5, 4, 4, 3, 2, 1)[y - 18]
            for x in range(CX - half, CX + half):
                cloak.px(x, y, "g" if x < CX - 1 else "m" if x < CX + 2 else "M")
        cloak.outline()
        cloak.line([(CX - 0.5, 19), (CX - 0.5, 27)], "j")
        L["Body"].paste(cloak)
        lens = Cv(TW, TW)                                         # a dew drop held up as a lens
        lens.blob(CX + 5, 7 + head_dy, 2.2, 2.2, "b", "w", "B")
        lens.outline("n")
        L["FX"].paste(lens)
        if glint:
            L["FX"].px(CX + 4, 6 + head_dy, "w")

    if tag == "Idle":
        sweep = (0, 2, 4, 2)[i % 4]
        base.ant(L, col=col, abd=-0.4, ant_tips=((CX - 6 - sweep, 0), (CX - 4 + sweep, 0)), bob=0)
        kit(glint=i == 1)
        return
    base.ant(L, col=col, abd=-0.4, ant_tips=((CX - 2, [1, 0, -1, -1, 0, 1][i]), (CX - 2, [1, 0, -1, -1, 0, 1][i])),
             head_dy=[0, 1, -1, -1, 0, 0][i], mand=[1, 1, 0, 0, 1, 0][i])
    kit([0, 1, -1, -1, 0, 0][i], glint=i in (1, 2))
    fx = L["FX"]
    if i in (1, 2, 3):                                            # a mark over where it is looking
        r = [0, 5, 4, 3][i]
        fx.ring(CX, 3, r, "r", 1, a=255, gaps=lambda d: 35 < d % 90 < 55)
        fx.px(CX - 0.5, 2.5, "r")
    if i in (3, 4):
        fx.line([(CX - 0.5, 6 - (i - 3) * 4), (CX - 0.5, 3 - (i - 3) * 4)], "M")
        fx.px(CX - 0.5, 2 - (i - 3) * 4, "g")


def draw_queens_guard(L, tag, i, n):
    col = ("v", "L", "k")
    if tag == "Idle":
        base.ant(L, col=col, head="major", mand=(0, 0, 1, 0)[i % 4], **idle_kw(i))
    else:
        base.ant(L, col=col, head="major", mand=[2, 3, 0, 0, 1, 1][i], head_dy=[1, 1, -2, -1, 0, 0][i], lift=[0, 1, 0, 0, 0, 0][i])
    trim = L["Body"]                                              # gilding: brow, shoulders, gaster tip
    hy = 9 + ([1, 1, -2, -1, 0, 0][i] if tag == "Attack" else 0)
    trim.line([(CX - 3, hy - 2), (CX + 2, hy - 2)], "y")
    trim.pxs([(CX - 3, 14), (CX + 2, 14), (CX - 1, 27), (CX, 27)], "y")
    trim.px(CX - 3, hy - 2, "w")
    if tag == "Attack" and i >= 2:
        fx = L["FX"]
        r = [0, 0, 4, 8, 11, 13][i]
        fx.ring(CX, 5, r, "y", 2 if i < 4 else 1, a=255 - (i - 2) * 55, ry=r * 0.5, gaps=lambda d: 20 < d < 160)
        if i == 2:
            fx.spr(SPARK_S, CX - 1.5, 1)


def draw_fungus_farmer(L, tag, i, n):
    if tag == "Idle":
        base.ant(L, **idle_kw(i))
        puff = 0
    else:
        base.ant(L, abd=[0.2, 0.6, -0.2, 0, 0, 0][i], head_dy=[0, 1, 0, 0, 0, 0][i], bob=[0, 1, 0, 0, 0, 0][i])
        puff = [0, 0.6, -0.4, 0, 0, 0][i]
    cap = Cv(TW, TW)                                              # a fungus cap carried on its back
    cap.blob(CX, 20, 5.4 + puff, 4.2 + puff * 0.6, "I", "w", "i")
    cap.outline()
    cap.pxs([(CX - 2, 18), (CX + 2, 19), (CX, 22), (CX - 3, 21)], "w")
    L["Body"].paste(cap)
    fx = L["FX"]
    if tag == "Idle" and i in (1, 3):
        fx.px(CX - 5 + i * 3, 13 - i, "I")
    if tag == "Attack" and i >= 2:
        rng = random.Random(i)
        for j in range(9):                                        # a cloud of spores rolling forward
            ang = math.radians(210 + j * 15)
            d = 5 + (i - 2) * 3.2 + rng.uniform(-1, 1)
            fx.blob(CX + math.cos(ang) * d, 16 + math.sin(ang) * d, 1.7 - (i - 2) * 0.25, 1.6 - (i - 2) * 0.25, "I", "w", "i",
                    a=240 - (i - 2) * 50)


NEW_TOWERS = [
    ("TowerHoneypot", "Honeypot", draw_honeypot, dict(attack_ms=80)),
    ("TowerWeaver", "Weaver", draw_weaver, {}),
    ("TowerScout", "Scout", draw_scout, dict(attack_ms=60)),
    ("TowerQueensGuard", "Queen's Guard", draw_queens_guard, dict(attack_ms=70)),
    ("TowerFungusFarmer", "Fungus Farmer", draw_fungus_farmer, dict(attack_ms=85)),
]

# name, colours, fly kind, trail, hit, muzzle, charge, fly options
NEW_SHOTS = [
    ("Honeypot", ("u", "y", "Y"), "blob", "drips", "sticky", "spit", None, {"size": 1.2}),
    ("Weaver", ("c", "w", "s"), "web", "thread", "web", "snap", None, {}),
    ("Scout", ("M", "g", "j"), "dart", "streak", "spark", "snap", None, {}),
    ("QueensGuard", ("y", "w", "Y"), "wave", "ripple", "ring", "flare", "gather", {}),
    ("FungusFarmer", ("I", "w", "i"), "puff", "glow", "puffcloud", "glow", None, {}),
]


def build_towers():
    """All three tiers of each new caste, using the same dressing as build_tier_sheets."""
    tiers.TOWERS[:] = NEW_TOWERS
    by_tier = {tier: tiers.build(tier) for tier in (1, 2, 3)}
    return by_tier


def build_shots():
    out = []
    for name, c, fly, trail_kind, hit_kind, muzzle_kind, charge_kind, opts in NEW_SHOTS:
        s = Sprite("FxTower" + name, "Fx", wpn.N, wpn.N, ["FX"], name + " shot")
        wpn.weapon_tags(s, fly, c, trail_kind, hit_kind, muzzle_kind, charge_kind, opts)
        hits = dict((t[0], t[1]) for t in s.tags)["Hit"]
        wpn.tier_tags(s, c, lambda tier, fly=fly, c=c, opts=opts: [wpn.tier_fly(fly, k, c, opts, tier) for k in range(4)],
                      [f["FX"] for f in hits])
        out.append(s)
    return out


# ================================================================ run
def main():
    enemies = build_enemies()
    results = []
    for s in enemies:
        results.append((s, export(s)))
        n = sum(len(f) for _, f, _, _ in s.tags)
        print(f"{s.group:8} {s.name:20} {s.w}x{s.h}  {n:2} frames  " + ", ".join(t[0] for t in s.tags))
    base.preview_sheet(results, os.path.join(HERE, "NewEnemyPreview.png"), scale=3)

    by_tier = build_towers()
    count = len(enemies)
    for tier in (1, 2, 3):
        for s in by_tier[tier]:
            export(s)
            count += 1
            print(f"{s.group:8} {s.name:20} " + ", ".join(t[0] for t in s.tags))
    tiers.comparison(by_tier, os.path.join(HERE, "NewTowerPreview.png"))
    shots = build_shots()
    for s in shots:
        export(s)
        count += 1
        print(f"{s.group:8} {s.name:20} " + ", ".join(t[0] for t in s.tags))
    wpn.contact_sheet(shots, os.path.join(HERE, "NewTowerShotPreview.png"), cols=3)
    print(f"{count} sprites written and read back OK")


if __name__ == "__main__":
    main()
