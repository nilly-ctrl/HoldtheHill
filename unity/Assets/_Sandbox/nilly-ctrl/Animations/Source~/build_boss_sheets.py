"""Hold the Hill boss mechanics and special enemies (picked 2026-10-06).

New bosses (64x64, face right; each has Walk, Hurt, Death, Attack, Spawn and its own moves):
  Enemies/EnemyOrbWeaver     walks its own threads and cocoons towers. Spin, Cocoon, ThreadWalk
  Enemies/EnemyBoulderBug    a giant pill bug that rolls up. RollUp, Roll, Pop, Stunned, Recover
  Enemies/EnemyRivalQueen    queen of the red colony, come to dig a second burrow. Dig, Call

New enemies:
  Enemies/EnemySilverfish (24x24)   fast, one hit point; leaves a quick zone where it dies
  Enemies/EnemyThiefAnt (24x24)     a red ant that steals food. Grab, WalkLoaded, Drop
  Enemies/EnemyBombardier (32x32)   bombardier beetle; scalds the ground where it dies. Boil
  Enemies/EnemyEggSac (24x24)       what the Mantis Queen lays. Idle, Hurt, Hatch, Burst

What they leave on the map and on towers:
  Props/PropRivalBurrow (48x48)     Stage1, Stage2, Stage3, Complete, Collapse
  Fx/FxWebThread (32x32)            Strand (tiles left to right), Anchor, Snap, Burn
  Fx/FxQuickZone (64x64)            Appear, Loop (chevrons run toward +x), Fade
  Fx/FxScald (64x64)                Burst, Boil, Fade
  Fx/FxTowerAffliction (32x32)      worn over a tower: Stunned, MarkLock, Marked, WebWrap, Webbed,
                                    WebBreak, CocoonWrap, Cocooned, CocoonBreak, Scalded

The new moves of the three bosses that were already drawn live with their sprites: the Titan
Beetle's ChargeWindup, Charge, PlateBreak, WalkCracked, PlateBreak2 and WalkBare in
build_roster_sheets.py; the Mantis Queen's LayEgg, Warded and WardBreak and the Hornet's Mark,
Carry and Flinch in build_elite_sheets.py.

    python build_boss_sheets.py
"""
import math
import os
import random

import build_anim_sheets as base
from build_anim_sheets import Cv, Sprite, SPARK, SPARK_S, beetle, export, flat, hurt_frames, shadow, tint
from build_extra_sheets import thick_line
from build_roster_sheets import creature

HERE = os.path.dirname(os.path.abspath(__file__))
RED_ANT = ("R", "r", "v")           # the rival colony
SILK = ("c", "w", "s")


def outlined(w, h, draw):
    cv = Cv(w, h)
    draw(cv)
    cv.outline()
    return cv


def whiten(L, t=0.85):
    L["Body"].im = tint(L["Body"].im, "w", t)
    return L


def stars(fx, cx, cy, i, rx=8, ry=3, n=3):
    """Small stars circling a point: the usual sign for stunned."""
    for j in range(n):
        ang = math.radians(j * 360 / n + i * 60 / n * 2)
        if math.sin(ang) > -0.95:
            fx.spr(SPARK_S, cx + math.cos(ang) * rx - 1, cy + math.sin(ang) * ry - 1)


# ================================================================ a right-facing ant
def ant_right(w, h, cx, cy, k, cols, phase=0.0, mand=0.0, dip=0.0, gaster=1.0, thick=False, mask=False, crown=False,
              ant_up=0.0, scoop=0.0):
    """Top-down ant facing right. k scales it: 1 fits a 24 px canvas."""
    ink, body = Cv(w, h), Cv(w, h)
    base_c, hi, lo = cols
    s = math.sin(phase)
    hx = cx + 6 * k + dip
    hips = (cx + 2.6 * k, cx + 1.4 * k, cx + 0.2 * k)
    feet = ((5.0 * k, 5.6 * k), (0.5 * k, 6.6 * k), (-5.2 * k, 6.2 * k))
    for j, (hip, (fx, fy)) in enumerate(zip(hips, feet)):
        off = round(s * 1.5 * k) * (1 if j != 1 else -1)
        for side, o in ((-1, off), (1, -off)):
            if j == 0 and scoop:                                  # front legs reach forward to dig
                o = scoop * 3 * k * (1 if side < 0 else -1) * (1 if s >= 0 else -1)
                fy_ = fy * (1 - 0.45 * abs(scoop))
            else:
                fy_ = fy
            pts = [(hip, cy - 0.5 + side * k), (hip + fx * 0.45, cy - 0.5 + side * fy_ * 0.72), (hip + fx + o, cy - 0.5 + side * fy_)]
            if thick:
                thick_line(ink, pts, (base_c, hi, lo))
            else:
                ink.line(pts, "k")
    for side in (-1, 1):                                          # antennae, elbowed
        wig = (1 if s > 0 else 0) * side
        pts = [(hx + 1.5 * k, cy - 0.5 + side * 1.5 * k), (hx + 3.4 * k, cy - 0.5 + side * (3.4 + ant_up) * k),
               (hx + (5.2 - ant_up * 2) * k, cy - 0.5 + side * (3.0 + ant_up * 2.5) * k + wig)]
        ink.line(pts, "k")
        m = [(hx + 2.3 * k, cy - 0.5 + side * 1.1 * k), (hx + 3.9 * k, cy - 0.5 + side * (0.5 + mand) * k)]
        ink.line(m, "k")
        if thick:
            ink.line([(x, y + 1) for x, y in m], "k")
    gx = cx - 5.4 * k - (gaster - 1) * 3.2 * k
    body.blob(gx, cy, 4.0 * k * gaster, 3.2 * k * gaster, base_c, hi, lo)       # gaster
    body.blob(cx - 1.2 * k, cy, 0.8 * k, 0.8 * k, base_c)                       # petiole
    body.blob(cx + 1.6 * k, cy, 2.4 * k, 1.7 * k, base_c, hi, lo)               # thorax
    body.blob(hx, cy, 2.6 * k, 2.7 * k, base_c, hi, lo)                         # head
    body.outline()
    for j in (0, 1) if k < 1.5 else (0, 1, 2, 3):                               # gaster bands
        bx = gx - 1.5 * k * gaster + j * (2.0 if k < 1.5 else 2.6 * k * gaster / 2.2)
        half = 3.2 * k * gaster * math.sqrt(max(0.0, 1 - ((bx - gx) / (4.0 * k * gaster)) ** 2)) - 1
        body.line([(bx, cy - half), (bx, cy + half - 1)], lo)
        if crown:
            body.px(bx, cy - 0.5, "y")
    ex = hx + 1.0 * k
    if mask:                                                      # a robber's mask across the eyes
        body.line([(ex - 1, cy - 2.7 * k + 1), (ex - 1, cy + 2.7 * k - 2)], "k")
        body.line([(ex, cy - 2.7 * k + 1), (ex, cy + 2.7 * k - 2)], "k")
        body.pxs([(ex, cy - 0.5 - 1.6 * k), (ex, cy - 0.5 + 1.6 * k)], "w")
    else:
        body.pxs([(ex, cy - 0.5 - 1.8 * k), (ex, cy - 0.5 + 1.8 * k)], "k")
    ink.paste(body)
    if crown:
        cr = Cv(w, h)
        x0 = hx - 1.5 * k
        for dy in (-2, -1, 0, 1):
            cr.px(x0, cy + dy, "y")
        cr.pxs([(x0 - 1, cy - 2), (x0 - 2, cy - 2), (x0 - 1, cy - 0.5), (x0 - 2, cy - 0.5), (x0 - 1, cy + 1), (x0 - 2, cy + 1)], "y")
        cr.px(x0 - 3, cy - 0.5, "w")
        cr.outline()
        ink.paste(cr)
        for side in (-1, 1):                                      # stubs where her wings were
            ink.line([(cx + 0.5 * k, cy - 0.5 + side * 1.9 * k), (cx - 1.5 * k, cy - 0.5 + side * 3.0 * k)], "c")
    return ink


def crumb(w, h, x, y, r=2.2):
    return outlined(w, h, lambda cv: (cv.blob(x, y, r, r * 0.9, "t", "c", "T"), cv.px(x - 1, y - 1, "w")))


# ================================================================ regular enemies
def build_enemies():
    out = []

    # ---- silverfish: a tapering, wriggling body, long feelers, three tail bristles
    def silverfish(phase, dx=0, strike=0):
        w = h = 24
        cx, cy = 12 + dx, 12.5
        ink, body = Cv(w, h), Cv(w, h)
        pts = []
        for j in range(8):
            x = cx + 6.5 - j * 1.9
            y = cy + math.sin(phase - j * 0.85) * (0.25 + j * 0.2)
            r = (2.5, 2.9, 2.8, 2.5, 2.1, 1.7, 1.3, 1.0)[j]
            pts.append((x, y, r))
        for j in (1, 2, 3):                                       # legs flicker under the front half
            x, y, r = pts[j]
            kick = 1 if math.sin(phase + j * 2.1) > 0 else 0
            ink.line([(x, y - r), (x + 1 - kick * 2, y - r - 2.5)], "k")
            ink.line([(x, y + r - 1), (x - 1 + kick * 2, y + r + 1.5)], "k")
        hx, hy, _ = pts[0]
        for side in (-1, 1):                                      # feelers
            ink.line([(hx + 1, hy - 0.5 + side), (hx + 3, hy - 0.5 + side * 3), (hx + 5, hy - 0.5 + side * (4 + (1 if math.sin(phase) * side > 0 else 0)))], "S")
        tx, ty, _ = pts[-1]
        for spread in (-3.5, 0, 3.5):                             # tail bristles
            ink.line([(tx, ty - 0.5), (tx - 4, ty - 0.5 + spread)], "S")
        for x, y, r in reversed(pts):
            body.blob(x, y, r * 0.95, r, "H", "w", "h")
        body.outline()
        for j in range(1, 7):                                     # the overlapping scales
            x, y, r = pts[j]
            body.line([(x + 0.9, y - r + 1), (x + 0.9, y + r - 2)], "h")
            body.px(x, y - r + 0.2, "w")
        body.pxs([(hx + 1, hy - 2), (hx + 1, hy + 1)], "k")
        ink.paste(body)
        return ink

    out.append(creature("EnemySilverfish", "Silverfish (fast, 1 hp, leaves a quick zone)", 24, silverfish, (9, 3.5),
                        splat="H", debris=("H", "s"), walk_n=6, walk_ms=40, reach=9))

    # ---- thief ant
    def thief(phase, dx=0, strike=0, dip=0.0, mand=None, loaded=0.0):
        """Drawn by hand, not with ant_right: at 24 px every pixel has to be placed. Three clear body
        parts in bright red, few legs lines, a black mask with white eyes, and a swag sack on its back."""
        w = h = 24
        cx, cy = 10 + dx, 12.5
        row = cy - 0.5
        m = mand if mand is not None else (1.6 if strike in (0, 1) else 0.3 if strike in (2, 3) else 1.0)
        if loaded:
            m = 1.5
        ink, body = Cv(w, h), Cv(w, h)
        s = 1 if math.sin(phase) > 0.3 else -1 if math.sin(phase) < -0.3 else 0
        hx = cx + 5.5 + dip
        for j, (hip, fx, fy) in enumerate(((cx + 3, 4, 5), (cx + 1.5, 0, 6), (cx, -4, 5))):   # tripod gait
            off = s * (1 if j != 1 else -1)
            for side, o in ((-1, off), (1, -off)):
                ink.line([(hip, row + side), (hip + fx * 0.5, row + side * (fy - 2)), (hip + fx + o, row + side * fy)], "k")
        for side in (-1, 1):
            ink.line([(hx + 2, row + side * 2), (hx + 4, row + side * 4), (hx + 6, row + side * (4 - (1 if s * side > 0 else 0)))], "k")
            ink.line([(hx + 3, row + side), (hx + 4, row + side * round(m))], "k")
        body.blob(cx - 5.5, cy, 4.2, 3.4, "r", "f", "R")          # gaster
        body.px(cx - 10, row, "r")
        body.blob(cx - 0.5, cy, 1.0, 1.0, "R")                    # waist
        body.blob(cx + 2, cy, 2.0, 1.7, "r", "f", "R")            # thorax
        body.blob(hx, cy, 3.0, 3.2, "r", "f", "R")                # head, big for its body
        body.outline()
        body.line([(cx - 5, cy - 2.4), (cx - 5, cy + 1.4)], "R")  # one band
        body.line([(hx + 1, cy - 3), (hx + 1, cy + 2)], "k")      # the mask
        body.pxs([(hx, cy - 3), (hx, cy + 2)], "k")
        body.pxs([(hx + 1, row - 2), (hx + 1, row + 2)], "w")
        body.pxs([(hx - 1, cy - 4), (hx - 2, cy - 5)], "k")       # its knot, trailing
        ink.paste(body)
        if loaded:                                                # a full swag sack tied on its back
            sx = cx - 4.5
            sack = Cv(w, h)
            r = 2.2 + loaded * 1.4
            sack.blob(sx, cy, r, r * 0.95, "t", "c", "T")
            sack.blob(sx + r + 0.5, cy, 1.0, 1.0, "T")            # the tied neck
            sack.outline()
            sack.px(sx - 1, cy - 1.5, "w")
            sack.line([(sx + r - 1, cy - r + 1), (sx + r - 1, cy + r - 2)], "D")
            ink.paste(sack)
        return ink

    def thief_extra(s, frame, cx, cy):
        frames = []
        for k in range(6):                                        # head down into the stockpile, up with a crumb
            L = frame(0, dx=(0, 1, 2, 2, 1, 0)[k], dip=(0, 1, 1.5, 1.5, 0.5, 0)[k], mand=(1.2, 2.0, 2.0, 0.8, 1.4, 1.4)[k],
                      loaded=(0, 0, 0, 0.6, 1, 1)[k])
            if k in (2, 3):
                L["FX"].pxs([(cx + 9 + k, cy - 5), (cx + 10 + k, cy + 3)], "t")
            frames.append(flat(L))
        s.tag("Grab", frames, 80, repeat=1)
        s.tag("WalkLoaded", [flat(frame(2 * math.pi * i / 6, loaded=1)) for i in range(6)], 70)
        frames = []
        for k in range(5):                                        # the crumb knocked out of its jaws
            L = frame(0, dx=(0, -1, -1, 0, 0)[k], mand=2.0)
            hop = (0, -3, -4, -2, 0)[k]
            L["FX"].paste(crumb(s.w, s.h, max(3, cx - 6 - k * 0.8), cy + hop - 2, 2.6))   # the sack bounces off its back
            if k == 0:
                whiten(L, 0.7)
            frames.append(flat(L))
        s.tag("Drop", frames, 80, repeat=1)

    out.append(creature("EnemyThiefAnt", "Thief ant (steals food and runs back)", 24, thief, (9, 4.5), splat="R",
                        debris=("r", "R"), walk_ms=65, reach=10, extra=thief_extra))

    # ---- bombardier beetle: orange head and shoulders, dark blue wing-cases
    def bombardier(phase, dx=0, strike=0, heat=0.0):
        w = h = 32
        cx, cy = 15 + dx, 16.5
        Lh, Wh = 9, 6.5 + heat * 0.8
        b = beetle(w, h, cx, cy, Lh, Wh, ("h", "H", "hz"), "plain", phase)
        hx = cx + Lh + 1.5
        front = Cv(w, h)
        front.blob(hx, cy, 2.6, 3.2, "o", "u", "R")
        front.blob(cx + Lh - 1.5, cy, Lh * 0.42 + 1, Wh * 0.78, "o", "u", "R")
        front.outline()
        front.pxs([(hx + 1, cy - 2), (hx + 1, cy + 1)], "k")
        b.paste(front)
        glow = "y" if heat > 0.6 else "u"
        for sx, sy in ((cx - 4, 3), (cx + 1, 3.5)):               # warning spots
            for side in (-1, 1):
                b.blob(sx, cy + side * sy, 1.5 + heat * 0.6, 1.3 + heat * 0.5, glow, "y" if heat else None)
        b.blob(cx - 9.5, cy, 1.4 + heat, 1.8 + heat, "o" if not heat else "y", "y")   # the turret at the tip of the abdomen
        return b

    def bombardier_extra(s, frame, cx, cy):
        frames = []
        for k in range(7):                                        # about to blow: swells, glows, vents steam
            heat = (0.2, 0.4, 0.6, 0.8, 1.0, 1.0, 1.0)[k]
            L = frame(k * 2.0, dx=(0, 0, 1, -1, 1, -1, 0)[k], heat=heat)
            if k >= 4:
                L["Body"].im = tint(L["Body"].im, "y" if k < 6 else "w", 0.25 + (k - 4) * 0.25)
            for j in range(k):
                ang = math.radians(200 + j * 53 + k * 17)
                d = 8 + (j * 3 + k * 2) % 6
                L["FX"].blob(cx - 2 + math.cos(ang) * d, cy + math.sin(ang) * d * 0.8 - k * 0.5, 1.6, 1.4, "c", "w", "s", a=170 - j * 12)
            frames.append(flat(L))
        s.tag("Boil", frames, 90, repeat=1)

    out.append(creature("EnemyBombardier", "Bombardier beetle (scalds the ground where it dies)", 32, bombardier, (12, 7.5),
                        splat="u", debris=("o", "h"), walk_ms=100, reach=13, extra=bombardier_extra))

    # ---- the mantis queen's egg sac
    s = Sprite("EnemyEggSac", "Enemies", 24, 24, ["Shadow", "Body", "FX"], "Egg sac (laid by the Mantis Queen)")
    cx, cy = 12, 12.5

    def sac(L, swell=0.0, dx=0, crack=0, embryo=1.0):
        L["Shadow"].paste(shadow(24, 24, cx + dx, cy + 1.5, 8, 5))
        ink = Cv(24, 24)
        for ax, ay in ((-9, -7), (9, -6), (-8, 8), (8, 8)):       # silk holding it to the ground
            ink.line([(cx + dx + ax * 0.4, cy + ay * 0.4), (cx + ax, cy + ay)], "c", a=220)
        b = Cv(24, 24)
        b.blob(cx + dx, cy, 6.2 + swell, 5.4 + swell, "c", "w", "t")
        b.outline()
        b.blob(cx + dx + 0.5, cy + 0.5, 3.2 * embryo, 2.6 * embryo, "T", "t", "d", a=170)   # what is growing inside
        b.pxs([(cx + dx + 1.5, cy - 0.5)], "g")
        for rx in (-4, 4):                                        # ribs of the casing
            b.line([(cx + dx + rx, cy - 3), (cx + dx + rx, cy + 2)], "t")
        if crack >= 1:
            b.line([(cx + dx - 2, cy - 5), (cx + dx, cy - 2), (cx + dx - 1, cy), (cx + dx + 2, cy + 2)], "k")
        if crack >= 2:
            b.line([(cx + dx, cy - 2), (cx + dx + 3, cy - 3)], "k")
            b.line([(cx + dx + 2, cy + 2), (cx + dx + 1, cy + 5)], "k")
        ink.paste(b)
        L["Body"].paste(ink)
        return L

    idle = [flat(sac(s.new(), swell=(0, 0.3, 0.6, 0.3)[i], embryo=(1, 1.05, 1.15, 1.05)[i])) for i in range(4)]
    s.tag("Idle", idle, 180)
    s.tag("Hurt", hurt_frames(s, idle[0]), 60, repeat=1)
    frames = []
    for k in range(7):                                            # rocks, cracks, splits, and a nymph climbs out
        if k < 4:
            L = sac(s.new(), swell=0.4 + k * 0.2, dx=(0, -1, 1, -1)[k], crack=(0, 1, 2, 2)[k], embryo=1.2)
        else:
            L = s.new()
            L["Shadow"].paste(shadow(24, 24, cx, cy + 1.5, 8, 5))
            gap = (k - 3) * 1.5
            husk = Cv(24, 24)
            husk.blob(cx - 2 - gap, cy - 1, 4, 5, "c", "w", "t")
            husk.blob(cx + 3 + gap, cy + 1, 3.6, 4.6, "t", "c", "T")
            husk.blob(cx, cy, 3.2 + gap * 0.3, 3.0, "e")
            husk.outline()
            L["Body"].paste(husk, alpha=1.0 if k < 6 else 0.6)
            ny = Cv(24, 24)                                       # the nymph, heading right
            nx = cx + (k - 4) * 3
            ny.blob(nx - 1, cy, 3.0, 2.0, "M", "g", "j")
            ny.blob(nx + 3, cy, 1.6, 1.8, "m", "g", "M")
            ny.outline()
            ny.pxs([(nx + 4, cy - 1.5), (nx + 4, cy + 0.5)], "y")
            L["FX"].paste(ny)
        frames.append(flat(L))
    s.tag("Hatch", frames, 110, repeat=1)
    frames = []
    for k in range(6):                                            # killed before it hatched
        L = s.new()
        if k == 0:
            whiten(sac(L, swell=1.0, crack=2))
        else:
            L["Shadow"].paste(shadow(24, 24, cx, cy + 1.5, 8, 5), alpha=max(0.0, 1 - k * 0.2))
            L["FX"].blob(cx, cy, 3 + k * 1.1, 2.4 + k * 0.8, "g", "I", "M", a=max(0, 230 - k * 40))
            rng = random.Random(5)
            for j in range(9):
                ang, spd = rng.uniform(0, 6.28), rng.uniform(0.6, 1.3)
                d = (2 + k * 2.2) * spd
                L["FX"].px(cx + math.cos(ang) * d, cy + math.sin(ang) * d, "c" if j % 2 else "t", a=max(0, 255 - k * 40))
        frames.append(flat(L))
    s.tag("Burst", frames, 70, repeat=1)
    out.append(s)
    return out


# ================================================================ bosses
def build_bosses():
    out = []

    # ---- orb-weaver
    def orb_weaver(phase, dx=0, strike=0, rear=0.0, wrap=None):
        w = h = 64
        cx, cy = 29 + dx, 32.5
        ink, body = Cv(w, h), Cv(w, h)
        ax = cx + 5
        for j, (ang, length) in enumerate(((27, 26), (58, 24), (116, 23), (150, 26))):
            step = math.sin(phase + j * math.pi / 2) * 8
            for side in (-1, 1):
                lift = rear * 22 if j == 0 else rear * 8 if j == 1 else 0
                a_deg = ang - step * (1 if side < 0 else -1) - lift
                ln = length
                if wrap is not None and j < 2:                    # the front two pairs working at something ahead of her
                    reach = 1 if (wrap + j + (0 if side < 0 else 1)) % 2 else 0
                    a_deg = (14, 30)[j] - reach * 8
                    ln = (22, 19)[j] + reach * 3
                a = math.radians(side * a_deg)
                foot = (ax + math.cos(a) * ln, cy - 0.5 + math.sin(a) * ln)
                knee = (ax + math.cos(a) * ln * 0.45, cy - 0.5 + math.sin(a) * ln * 0.45 + side * 6)
                thick_line(ink, [(ax, cy - 0.5 + side * 2), knee, foot], ("x", "S", "k"))
                for t in (0.25, 0.7):                             # banded legs
                    bx, by = knee[0] + (foot[0] - knee[0]) * t, knee[1] + (foot[1] - knee[1]) * t
                    ink.pxs([(bx, by), (bx, by + 1)], "u")
                ink.px(knee[0], knee[1], "u")
        body.blob(cx - 10, cy, 12.5, 10.5, "y", "c", "Y")         # abdomen
        body.blob(cx + 7, cy, 5.4, 4.8, "s", "w", "S")            # cephalothorax, silver-haired
        body.outline()
        for j, bx in enumerate((cx - 18, cx - 13, cx - 8, cx - 3)):     # black chevrons down the back
            half = 10.5 * math.sqrt(max(0.0, 1 - ((bx + 1 - (cx - 10)) / 12.5) ** 2)) - 1
            for side in (-1, 1):
                pts = [(bx + 2, cy - 0.5), (bx, cy - 0.5 + side * half * 0.55), (bx + 2, cy - 0.5 + side * half)]
                body.line(pts, "k")
                body.line([(x + 1, y) for x, y in pts], "k")
            body.pxs([(bx + 4, cy - 3), (bx + 4, cy + 2)], "w")
        eye = "f" if strike in (2, 3) or rear > 0.5 else "r"
        body.pxs([(cx + 10, cy - 3), (cx + 10, cy + 2), (cx + 11, cy - 2), (cx + 11, cy + 1), (cx + 9, cy - 1), (cx + 9, cy)], eye)
        ink.paste(body)
        bite = 1 if strike in (2, 3) else 0
        for side in (-1, 1):                                      # fangs
            ink.line([(cx + 12, cy - 0.5 + side * 2), (cx + 15 + bite, cy - 0.5 + side * (2 - bite))], "c")
            ink.line([(cx + 12, cy - 0.5 + side * 3), (cx + 14 + bite, cy - 0.5 + side * (3 - bite))], "k")
        return ink

    def orb_extra(s, frame, cx, cy):
        frames = []
        for k in range(7):                                        # rears, then casts a thread ahead
            L = frame(0, dx=(0, -1, -2, 0, 0, 0, 0)[k], rear=(0.4, 0.8, 1.0, 0.5, 0.2, 0.1, 0)[k])
            if k >= 3:
                x1 = (0, 0, 0, 50, 58, 63, 63)[k]
                L["FX"].line([(cx + 13, cy - 0.5), (x1, cy - 0.5)], "c", a=255 - (k - 3) * 25)
                L["FX"].line([(cx + 13, cy + 0.5), (x1 - 6, cy + 0.5)], "s", a=150)
                if k < 6:
                    L["FX"].spr(SPARK_S, x1 - 1, cy - 2)
            frames.append(flat(L))
        s.tag("Spin", frames, 90, repeat=1)

        frames = []
        for k in range(8):                                        # binds something in front of her
            L = frame(0, dx=1, wrap=k)
            r = 1.5 + k * 0.55
            bundle = outlined(s.w, s.h, lambda cv, r=r: cv.blob(cx + 24, cy, r * 0.8, r, *SILK))
            for j in range(int(r)):
                bundle.line([(cx + 24 - r * 0.6, cy - r + 2 + j * 2 + (k % 2)), (cx + 24 + r * 0.6, cy - r + 1 + j * 2 + (k % 2))], "s")
            L["FX"].paste(bundle)
            L["FX"].line([(cx + 14, cy - 0.5), (cx + 24 - r, cy - 0.5 + (1 if k % 2 else -1))], "c")
            frames.append(flat(L))
        s.tag("Cocoon", frames, 85, repeat=1)

        frames = []
        for i in range(8):                                        # walking a thread she has strung over the map
            L = frame(2 * math.pi * i / 8, shadow_scale=0.55)
            L["Shadow"].im = base.shift(base.fade(L["Shadow"].im, 0.6), 0, 5)
            line = Cv(s.w, s.h)
            sag = 1 if i % 4 in (1, 2) else 0
            line.line([(0, cy - 0.5), (cx - 14, cy - 0.5 + sag), (cx + 14, cy - 0.5 + sag), (s.w - 1, cy - 0.5)], "c", a=235)
            line.line([(0, cy + 0.5), (cx - 14, cy + 0.5 + sag), (cx + 14, cy + 0.5 + sag), (s.w - 1, cy + 0.5)], "s", a=150)
            line.paste(L["Body"].im)
            L["Body"] = line
            frames.append(flat(L))
        s.tag("ThreadWalk", frames, 100)

    out.append(creature("EnemyOrbWeaver", "Orb-weaver (boss; shortcuts on threads, cocoons towers)", 64, orb_weaver, (22, 14),
                        splat="Y", debris=("y", "k"), walk_n=8, walk_ms=110, reach=30, extra=orb_extra))

    # ---- boulder bug
    PLATE = ("h", "H", "hz")
    BALL_R = 14.5

    def bug_ball(cv, cx, cy, spin):
        b = Cv(cv.w, cv.h)
        b.blob(cx, cy, BALL_R, BALL_R, *PLATE)
        b.outline()
        n = 10
        for j in range(n):                                        # plate seams wrapping round as it rolls
            th = spin + j * 2 * math.pi / n
            if math.cos(th) <= 0.05:
                continue
            for y in range(int(cy - BALL_R) + 1, int(cy + BALL_R)):
                half = math.sqrt(max(0.0, (BALL_R - 1) ** 2 - (y + 0.5 - cy) ** 2))
                x = cx + half * math.sin(th)
                b.px(x, y, "hz")
                if math.cos(th) > 0.5 and abs(y + 0.5 - cy) < BALL_R * 0.6:
                    b.px(x + 1, y, "H" if y < cy - 2 else "h")
        b.blob(cx - 5, cy - 6, 2.4, 1.6, "w", a=200)             # the shine does not turn with it
        cv.paste(b)

    def boulder_bug(phase, dx=0, strike=0, curl=0.0, spin=0.0, flipped=0.0, squash=0.0):
        w = h = 64
        cx, cy = 30 + dx, 32.5
        ink, body = Cv(w, h), Cv(w, h)
        if curl >= 1.0:
            bug_ball(ink, cx + 1, cy, spin)
            return ink
        Lh = 20 - curl * (20 - BALL_R)
        Wh = (11.5 + curl * (BALL_R - 11.5)) * (1 - squash * 0.45)
        seams = [cx - Lh + (j + 1) * 2 * Lh / 9 + math.sin(phase - j * 0.8) * 0.7 for j in range(8)]

        def half_at(x):
            return Wh * math.sqrt(max(0.0, 1 - ((x - cx) / Lh) ** 2))

        if curl < 0.6:
            hx = cx + Lh - 1 - curl * 6
            for side in (-1, 1):                                  # antennae and tail forks
                ink.line([(hx + 2, cy - 0.5 + side * 2), (hx + 6, cy - 0.5 + side * 4), (hx + 9, cy - 0.5 + side * (4 + (1 if math.sin(phase) * side > 0 else 0)))], "k")
                ink.line([(cx - Lh + 1, cy - 0.5 + side * 2), (cx - Lh - 2, cy - 0.5 + side * 3)], "k")
            if not flipped:
                for j, x in enumerate(seams[:-1]):               # seven pairs of short legs rippling under the rim
                    kick = round(math.sin(phase * 2 - j * 1.1) * 1.5)
                    hw = half_at(x + 1)
                    for side in (-1, 1):
                        ink.line([(x + 1, cy - 0.5 + side * (hw - 1)), (x + 1 + kick, cy - 0.5 + side * (hw + 3))], "k")
        cols = ("s", "c", "S") if flipped else PLATE
        body.blob(cx, cy, Lh, Wh, *cols)
        if curl < 0.6:
            body.blob(cx + Lh - 1 - curl * 6, cy, 3.4, 5.2 * (1 - squash * 0.4), cols[2], cols[0])        # head
        body.outline()
        for j, x in enumerate(seams):
            hw = half_at(x) - 1
            if hw < 2:
                continue
            body.line([(x, cy - hw), (x, cy + hw - 1)], "S" if flipped else "hz")
            if not flipped:
                body.line([(x + 1, cy - hw), (x + 1, cy - hw * 0.45)], "H")
        if flipped:                                               # on its back: the pale belly and every leg in the air
            for j, x in enumerate(seams[:-1]):
                kick = round(math.sin(phase * 2 + j * 1.3) * 2)
                for side in (-1, 1):
                    body.line([(x + 2, cy - 0.5 + side * 2), (x + 2 + kick, cy - 0.5 + side * min(7, half_at(x + 2) - 2))], "x")
            body.line([(cx - Lh + 4, cy - 0.5), (cx + Lh - 6, cy - 0.5)], "S")
        elif curl < 0.6:
            ex = cx + Lh - 1 - curl * 6 + 1
            body.pxs([(ex, cy - 3), (ex, cy + 2)], "r" if strike in (2, 3) else "k")
            body.px(cx - Lh * 0.4, cy - Wh * 0.6, "w")
        ink.paste(body)
        return ink

    def bug_extra(s, frame, cx, cy):
        s.tag("RollUp", [flat(frame(0, dx=(0, -1, 0, 1, 1)[k], curl=(0.25, 0.5, 0.75, 1.0, 1.0)[k], spin=0.0,
                                    shadow_scale=(0.95, 0.88, 0.8, 0.72, 0.72)[k])) for k in range(5)], 80, repeat=1)
        frames = []
        for i in range(6):                                        # rolling: one seam's worth of turn over the loop
            L = frame(0, dx=(0, 0, 1, 0, 0, -1)[i], curl=1.0, spin=i * (2 * math.pi / 10) / 6, shadow_scale=0.72)
            for j in range(4):
                y = cy - 11 + j * 7 + ((i + j) % 2)
                x0 = cx - 22 - ((i * 5 + j * 6) % 8)
                L["FX"].line([(x0, y), (x0 + 5, y)], "c" if (i + j) % 2 else "t", a=200)
            for j in range(3):
                pr = 2.6 - ((i + j) % 3) * 0.6
                L["FX"].blob(cx - 19 - ((i * 3 + j * 4) % 7), cy + 8 + j * 3 - ((i + j) % 2) * 2, pr, pr, "d", "T", "D", a=210)
            frames.append(flat(L))
        s.tag("Roll", frames, 50)

        frames = []
        for k in range(7):                                        # knocked out of the roll and onto its back
            if k < 2:
                L = whiten(frame(0, dx=(0, -3)[k], curl=1.0, spin=0.2, shadow_scale=0.72), 0.85 if k == 0 else 0.3)
            else:
                L = frame(k * 1.3, dx=(0, 0, -4, -3, -2, -1, 0)[k], curl=(0, 0, 0.6, 0.3, 0, 0, 0)[k], flipped=1.0,
                          squash=(0, 0, 0, 0.3, 0.15, 0, 0)[k], shadow_scale=0.8 + k * 0.03)
            if k <= 3:
                L["FX"].ring(cx + 12, cy, 6 + k * 6, "y" if k < 2 else "c", 2 if k < 2 else 1, a=250 - k * 55)
                if k < 2:
                    L["FX"].spr(SPARK, cx + 14 + k * 4, cy - 10)
                    L["FX"].spr(SPARK, cx + 13 + k * 4, cy + 6)
            frames.append(flat(L))
        s.tag("Pop", frames, 80, repeat=1)

        frames = []
        for i in range(6):
            L = frame(i * 1.05, flipped=1.0)
            stars(L["FX"], cx + 18, cy - 15, i, rx=9, ry=3)
            frames.append(flat(L))
        s.tag("Stunned", frames, 110)
        s.tag("Recover", [flat(frame(k, dx=(0, 0, 1, 0, 0)[k], flipped=(1, 1, 0, 0, 0)[k], squash=(0.3, 0.7, 0.7, 0.3, 0)[k],
                                     shadow_scale=(1, 0.9, 0.9, 1, 1)[k])) for k in range(5)], 90, repeat=1)

    out.append(creature("EnemyBoulderBug", "Boulder bug (boss; rolls up, knock it open)", 64, boulder_bug, (24, 13),
                        splat="S", debris=("h", "H"), walk_n=8, walk_ms=120, reach=27, extra=bug_extra))

    # ---- rival queen
    def rival_queen(phase, dx=0, strike=0, dip=0.0, scoop=0.0, ant_up=0.0):
        m = 1.8 if strike in (0, 1) else 0.2 if strike in (2, 3) else 1.0
        return ant_right(64, 64, 29 + dx, 32.5, 2.25, RED_ANT, phase, mand=m, dip=dip, gaster=1.3, thick=True, crown=True,
                         ant_up=ant_up, scoop=scoop)

    def queen_extra(s, frame, cx, cy):
        frames = []
        for i in range(6):                                        # digging the new burrow: head down, dirt thrown back past her
            L = frame(i * math.pi, dx=(0, 1, 2, 1, 0, -1)[i], dip=(0, 2, 3, 2, 0, 0)[i], scoop=(0.4, 1, 1, 0.6, 0.3, 0.6)[i])
            L["Shadow"].blob(cx + 20, cy, 6 + (i % 3), 5 + (i % 2), "e", a=200)
            for j in range(6):
                side = -1 if j % 2 else 1
                t = ((i + j * 2) % 6) / 5
                x = cx + 14 - t * 30
                y = cy + side * (9 + math.sin(t * math.pi) * 9 + (j // 2) * 2)
                pr = 2.3 - t * 0.9
                L["FX"].blob(x, y, pr, pr, "d", "T", "D", a=int(250 - t * 120))
            frames.append(flat(L))
        s.tag("Dig", frames, 85)

        frames = []
        for k in range(7):                                        # calls the colony: antennae up, scent rolling out
            L = frame(0, ant_up=(0.3, 0.7, 1, 1, 1, 0.6, 0.2)[k])
            if 1 <= k <= 5:
                for j in range(2):
                    r = 6 + k * 5 - j * 6
                    if r > 3:
                        L["FX"].ring(cx - 12, cy, r, "f" if j == 0 else "l", 2 if k < 3 else 1, a=240 - k * 38, ry=r * 0.8,
                                     gaps=lambda d, k=k: int((d + k * 15) / 30) % 4 == 0)
            frames.append(flat(L))
        s.tag("Call", frames, 100, repeat=1)

    out.append(creature("EnemyRivalQueen", "Rival queen (boss; digs a second burrow)", 64, rival_queen, (26, 12),
                        splat="R", debris=("r", "R"), walk_n=8, walk_ms=120, reach=30, extra=queen_extra))
    return out


# ================================================================ props and effects
def build_props():
    s = Sprite("PropRivalBurrow", "Props", 48, 48, ["Shadow", "Body", "FX"], "The rival queen's burrow, as it is dug")

    def mound(L, stage, k=0, sink=0.0):
        r = (7, 12, 16, 19)[stage] * (1 - sink * 0.25)
        L["Shadow"].blob(24, 26, r + 2, (r + 2) * 0.75, (0, 0, 0), a=int(70 * (1 - sink)))
        body = Cv(48, 48)
        if stage == 0:                                            # scratch marks and the first scrapings
            for j in range(4):
                body.line([(17 + j * 4, 20 + (j % 2)), (19 + j * 4, 27 + (j % 2))], "e")
            body.blob(24, 24, 5, 3.5, "D", "d", "e")
            body.blob(15, 28, 2.6, 2, "d", "T", "D")
            body.blob(33, 21, 2.2, 1.8, "d", "T", "D")
        else:
            cols = ("D", "d", "e") if not sink else ("e", "D", "k")
            body.blob(24, 24, r, r * 0.78, *cols)
            body.outline()
            hole = (3.5, 6.5, 9, 11.5)[stage] * (1 - sink)
            if hole > 1:
                body.blob(24, 24, hole, hole * 0.75, "k")
                body.blob(24, 23.4, hole * 0.72, hole * 0.5, "v")
            for j in range(stage * 3):                            # fresh spoil round the rim
                ang = math.radians(j * 360 / (stage * 3) + 20)
                body.px(24 + math.cos(ang) * (r - 2), 24 + math.sin(ang) * (r - 2) * 0.78, "T" if j % 2 else "t")
        L["Body"].paste(body)
        if stage >= 2 and not sink:                               # the red colony's pennant goes up
            flag = Cv(48, 48)
            fx_, fy_ = 37, 10 if stage == 3 else 13
            flag.line([(fx_, fy_), (fx_, fy_ + 9)], "D")
            wave = k % 2
            flag.pxs([(fx_ + 1, fy_), (fx_ + 2, fy_ + wave), (fx_ + 3, fy_ + wave), (fx_ + 1, fy_ + 1), (fx_ + 2, fy_ + 1 + wave),
                      (fx_ + 3, fy_ + 1), (fx_ + 4, fy_ + 1 - wave), (fx_ + 1, fy_ + 2), (fx_ + 2, fy_ + 2)], "r")
            flag.px(fx_ + 1, fy_ + 1, "y")
            flag.outline()
            L["Body"].paste(flag)
        return L

    for stage in range(3):
        frames = []
        for k in range(4):
            L = mound(s.new(), stage, k)
            for j in range(2 + stage):                            # dirt still being thrown out
                ang = math.radians(j * 97 + k * 40 + stage * 30)
                d = (7, 12, 16)[stage] + 2 + (k + j) % 3
                L["FX"].blob(24 + math.cos(ang) * d, 24 + math.sin(ang) * d * 0.78 - (1 if (k + j) % 2 else 0), 1.5, 1.3, "d", "T", "D", a=230)
            frames.append(flat(L))
        s.tag(f"Stage{stage + 1}", frames, 160)
    frames = []
    for k in range(4):
        L = mound(s.new(), 3, k)
        if k in (1, 2):
            L["FX"].pxs([(21, 23), (27, 23)], "r")
        frames.append(flat(L))
    s.tag("Complete", frames, 400)
    frames = []
    for k in range(7):                                            # destroyed: falls in on itself
        L = mound(s.new(), 3, k, sink=(0.15, 0.35, 0.55, 0.75, 0.9, 1.0, 1.0)[k])
        if k == 0:
            whiten(L, 0.6)
        for j in range(8):
            ang = math.radians(j * 45 + 10)
            d = 8 + k * 2.6
            pr = max(0.7, 3.2 - k * 0.4)
            L["FX"].blob(24 + math.cos(ang) * d, 24 + math.sin(ang) * d * 0.78, pr, pr, "s", "c", "S", a=max(0, 230 - k * 34))
        frames.append(flat(L))
    s.tag("Collapse", frames, 90, repeat=1)
    return [s]


def build_fx():
    out = []

    # ---- the orb-weaver's thread
    s = base.fx_sprite("FxWebThread", 32, "Orb-weaver thread (Strand tiles left to right)")
    frames = []
    for k in range(4):
        cv = Cv(32, 32)
        sag = (0, 1, 0, -1)[k]
        cv.line([(0, 15), (8, 15 + sag * 0.5), (16, 15 + sag), (24, 15 + sag * 0.5), (31, 15)], "c", a=240)
        cv.line([(0, 16), (8, 16 + sag * 0.5), (16, 16 + sag), (24, 16 + sag * 0.5), (31, 16)], "s", a=150)
        cv.px((k * 8 + 4) % 32, 15, "w")                          # dew running along it
        cv.px((k * 8 + 20) % 32, 15, "w")
        frames.append({"FX": cv.im})
    s.tag("Strand", frames, 160)
    frames = []
    for k in range(2):
        cv = Cv(32, 32)
        for j in range(7):
            ang = math.radians(j * 360 / 7 + 12)
            cv.line([(16, 15.5), (16 + math.cos(ang) * (9 + (j + k) % 2), 15.5 + math.sin(ang) * (9 + (j + k) % 2))], "c", a=230)
        cv.ring(16, 16, 6, "s", 1, a=200, gaps=lambda d: d % 51 < 6)
        knot = outlined(32, 32, lambda c: c.blob(16, 16, 2.6, 2.4, *SILK))
        cv.paste(knot)
        frames.append({"FX": cv.im})
    s.tag("Anchor", frames, 400)
    frames = []
    for k in range(5):                                            # cut: the two ends whip back
        cv = Cv(32, 32)
        gap = 2 + k * 3.5
        curl = min(4, k * 1.5)
        cv.line([(0, 15), (16 - gap - 2, 15), (16 - gap, 15 - curl)], "c", a=250 - k * 40)
        cv.line([(31, 15), (16 + gap + 2, 15), (16 + gap, 15 + curl)], "c", a=250 - k * 40)
        if k < 2:
            cv.spr(SPARK_S, 15, 14)
        frames.append({"FX": cv.im})
    s.tag("Snap", frames, 70, repeat=1)
    frames = []
    for k in range(6):                                            # burnt through by a beam or a fire
        cv = Cv(32, 32)
        edge = 4 + k * 6
        if edge < 31:
            cv.line([(edge, 15), (31, 15)], "c", a=240)
            cv.line([(edge, 16), (31, 16)], "s", a=150)
            cv.blob(edge, 15.5, 1.8, 1.8, "o", "y", "r")
            cv.px(edge - 2, 13 - k % 2, "y")
        for x in range(0, min(edge, 32), 3):
            cv.px(x, 15 - ((x + k) % 3), "S", a=max(0, 160 - (edge - x) * 6))
        frames.append({"FX": cv.im})
    s.tag("Burn", frames, 70, repeat=1)
    out.append(s)

    # ---- quick zone left by a silverfish
    s = base.fx_sprite("FxQuickZone", 64, "Quick zone: enemies inside move faster (chevrons run toward +x)")

    def zone(cv, k, grow=1.0, alpha=1.0):
        r = 27 * grow
        cv.disc(32, 32, r, "H", a=int(70 * alpha))
        cv.disc(32, 32, r * 0.72, "b", a=int(55 * alpha))
        cv.ring(32, 32, r, "w", 1, a=int(200 * alpha), gaps=lambda d: int((d + k * 10) / 20) % 3 == 0)
        cv.ring(32, 32, r - 2, "B", 1, a=int(120 * alpha), gaps=lambda d: int((d - k * 10) / 30) % 2 == 0)
        for row, y in enumerate((20, 32, 44)):                    # chevrons streaming forward
            for n in range(3):
                x = 14 + n * 14 + ((k * 14) // 6 + row * 5) % 14
                if (x - 32) ** 2 + (y - 32) ** 2 > (r - 7) ** 2:
                    continue
                for d in range(4):
                    cv.px(x - d, y - 0.5 - d, "w", a=int(230 * alpha))
                    cv.px(x - d, y - 0.5 + d, "w", a=int(230 * alpha))
                    cv.px(x - d - 1, y - 0.5 - d, "B", a=int(170 * alpha))
                    cv.px(x - d - 1, y - 0.5 + d, "B", a=int(170 * alpha))
        rng = random.Random(k // 2)
        for _ in range(5):                                        # shed scales glinting
            ang, d = rng.uniform(0, 6.28), rng.uniform(0, r - 4)
            cv.px(32 + math.cos(ang) * d, 32 + math.sin(ang) * d, "w", a=int(255 * alpha))

    frames = []
    for k in range(5):
        cv = Cv(64, 64)
        zone(cv, k, grow=(0.25, 0.55, 0.85, 1.08, 1.0)[k], alpha=(0.6, 0.8, 1, 1, 1)[k])
        if k < 3:
            cv.ring(32, 32, 8 + k * 9, "w", 2, a=230 - k * 60)
        frames.append({"FX": cv.im})
    s.tag("Appear", frames, 70, repeat=1)
    frames = []
    for k in range(6):
        cv = Cv(64, 64)
        zone(cv, k)
        frames.append({"FX": cv.im})
    s.tag("Loop", frames, 80)
    frames = []
    for k in range(5):
        cv = Cv(64, 64)
        zone(cv, k, grow=1 - k * 0.06, alpha=(0.85, 0.65, 0.45, 0.28, 0.12)[k])
        frames.append({"FX": cv.im})
    s.tag("Fade", frames, 90, repeat=1)
    out.append(s)

    # ---- the bombardier's scald
    s = base.fx_sprite("FxScald", 64, "Scald: towers standing in it are silenced")
    rng = random.Random(21)
    lobes = [(rng.uniform(0, 360), rng.uniform(10, 19), rng.uniform(5, 8)) for _ in range(9)]
    bubbles = [(rng.uniform(0, 360), rng.uniform(2, 20), rng.randint(0, 5)) for _ in range(12)]

    def puddle(cv, k, grow=1.0, alpha=1.0):
        cv.disc(32, 33, 13 * grow, "o", a=int(150 * alpha))
        for ang, d, r in lobes:
            a = math.radians(ang)
            cv.blob(32 + math.cos(a) * d * grow, 33 + math.sin(a) * d * 0.8 * grow, r * grow, r * 0.8 * grow, "o", a=int(150 * alpha))
        cv.disc(32, 33, 11 * grow, "u", a=int(150 * alpha))
        cv.ring(32, 33, 25 * grow, "y", 1, a=int(170 * alpha), ry=20 * grow, gaps=lambda d: int((d + k * 9) / 18) % 3 == 0)
        for ang, d, born in bubbles:                              # bubbles rise, swell and pop
            age = (k - born) % 6
            a = math.radians(ang)
            x, y = 32 + math.cos(a) * d * grow, 33 + math.sin(a) * d * 0.8 * grow
            if age < 3:
                cv.blob(x, y, 1 + age * 0.7, 1 + age * 0.7, "y", "w", a=int(240 * alpha))
            elif age == 3:
                cv.ring(x, y, 3.2, "w", 1, a=int(220 * alpha), gaps=lambda dd: int(dd / 45) % 2 == 0)
        for j in range(5):                                        # steam climbing off it
            x = 14 + j * 9 + (1 if (k + j) % 2 else 0)
            y = 30 - ((k * 3 + j * 7) % 18)
            cv.blob(x, y, 2.2 - (30 - y) * 0.06, 1.8 - (30 - y) * 0.05, "c", "w", a=int(max(0, 170 - (30 - y) * 7) * alpha))

    frames = []
    for k in range(6):
        cv = Cv(64, 64)
        puddle(cv, k, grow=(0.3, 0.6, 0.9, 1.1, 1.03, 1.0)[k])
        if k < 4:
            cv.ring(32, 33, 7 + k * 7, "w" if k < 2 else "y", 2, a=240 - k * 50, ry=(7 + k * 7) * 0.8)
            for j in range(8):
                ang = math.radians(j * 45 + 10)
                d = 6 + k * 7
                cv.blob(32 + math.cos(ang) * d, 33 + math.sin(ang) * d * 0.8 - (3 - abs(k - 1.5)) * 2, 1.6, 1.6, "y", "w", "o", a=250 - k * 40)
        frames.append({"FX": cv.im})
    s.tag("Burst", frames, 70, repeat=1)
    frames = []
    for k in range(6):
        cv = Cv(64, 64)
        puddle(cv, k)
        frames.append({"FX": cv.im})
    s.tag("Boil", frames, 110)
    frames = []
    for k in range(5):
        cv = Cv(64, 64)
        puddle(cv, k, grow=1 - k * 0.1, alpha=(0.85, 0.65, 0.45, 0.28, 0.12)[k])
        frames.append({"FX": cv.im})
    s.tag("Fade", frames, 100, repeat=1)
    out.append(s)

    # ---- what a tower wears while something has been done to it
    s = base.fx_sprite("FxTowerAffliction", 32, "Worn over a tower: stunned, marked, webbed, cocooned, scalded")
    C = (16, 17)

    frames = []
    for i in range(6):
        cv = Cv(32, 32)
        stars(cv, 16, 6, i, rx=9, ry=3)
        frames.append({"FX": cv.im})
    s.tag("Stunned", frames, 90)

    def reticle(cv, r, k=0, a=255):
        cv.ring(C[0], C[1], r, "r", 1.6, a=a, gaps=lambda d: (d + 22.5 + k * 15) % 90 < 22)
        for j in range(4):
            ang = math.radians(j * 90 + k * 15)
            cv.line([(C[0] + math.cos(ang) * (r - 4), C[1] + math.sin(ang) * (r - 4)), (C[0] + math.cos(ang) * (r - 1), C[1] + math.sin(ang) * (r - 1))], "f", a=a)
        cv.pxs([(C[0] - 1, C[1] - 1), (C[0], C[1] - 1), (C[0] - 1, C[1]), (C[0], C[1])], "r", a=a)

    frames = []
    for k in range(5):
        cv = Cv(32, 32)
        reticle(cv, (17, 16, 15, 12.5, 14)[k], k=5 - k, a=(120, 170, 220, 255, 255)[k])
        frames.append({"FX": cv.im})
    s.tag("MarkLock", frames, 80, repeat=1)
    frames = []
    for k in range(4):
        cv = Cv(32, 32)
        reticle(cv, (14, 13.5, 13, 13.5)[k])
        frames.append({"FX": cv.im})
    s.tag("Marked", frames, 140)

    def web(cv, t=1.0, shimmer=-1, a=235):
        """t = how much of the web is spun, from the rim inward."""
        spokes = 8
        for j in range(spokes):
            ang = math.radians(j * 360 / spokes + 11)
            r0 = 14 * (1 - t)
            cv.line([(C[0] + math.cos(ang) * r0, C[1] + math.sin(ang) * r0), (C[0] + math.cos(ang) * 14, C[1] + math.sin(ang) * 14)],
                    "w" if j == shimmer else "c", a=a)
        for n, r in enumerate((12, 8, 4)):
            if t < (n + 1) / 3 - 0.05:
                continue
            pts = [(C[0] + math.cos(math.radians(j * 45 + 11)) * (r - (1.5 if j % 2 else 0)), C[1] + math.sin(math.radians(j * 45 + 11)) * (r - (1.5 if j % 2 else 0)))
                   for j in range(9)]
            cv.line(pts, "c" if n != shimmer % 3 else "w", a=a - 40)

    frames = []
    for k in range(5):
        cv = Cv(32, 32)
        web(cv, (0.2, 0.4, 0.67, 0.85, 1.0)[k])
        if k < 2:
            cv.spr(SPARK_S, 15, 16)
        frames.append({"FX": cv.im})
    s.tag("WebWrap", frames, 70, repeat=1)
    frames = []
    for k in range(4):
        cv = Cv(32, 32)
        web(cv, 1.0, shimmer=k * 2)
        frames.append({"FX": cv.im})
    s.tag("Webbed", frames, 220)
    frames = []
    for k in range(5):
        cv = Cv(32, 32)
        full = Cv(32, 32)
        web(full, 1.0, a=235 - k * 40)
        for j in range(4):                                        # torn into quarters that drift apart
            ox, oy = (-1, 1)[j % 2], (-1, 1)[j // 2]
            x0, y0 = (0 if ox < 0 else 16), (0 if oy < 0 else 17)
            piece = full.im.crop((x0, y0, x0 + 16, y0 + (17 if oy < 0 else 15)))
            layer = base.Image.new("RGBA", (32, 32), (0, 0, 0, 0))
            layer.paste(piece, (x0 + ox * k * 2, y0 + oy * k * 2))
            cv.im.alpha_composite(layer)
        cv.p = cv.im.load()
        if k == 0:
            cv.spr(SPARK, 13, 14)
        frames.append({"FX": cv.im})
    s.tag("WebBreak", frames, 70, repeat=1)

    def cocoon(cv, fill=1.0, breathe=0.0, shake=0):
        b = Cv(32, 32)
        b.blob(C[0] + shake, C[1] - 1, (7.5 + breathe) * fill, (10.5 + breathe) * fill, *SILK)
        b.outline("S")
        ry = (10.5 + breathe) * fill
        for j in range(-4, 5):                                    # windings
            y = C[1] - 1 + j * 2.3
            if abs(y - (C[1] - 1)) > ry - 2:
                continue
            half = (7.5 + breathe) * fill * math.sqrt(max(0.0, 1 - ((y - (C[1] - 1)) / ry) ** 2)) - 1
            b.line([(C[0] + shake - half, y + 1), (C[0] + shake + half, y - 1)], "s")
        cv.paste(b)
        for ang in (35, 145, 215, 325):                           # guy lines to the mound
            a = math.radians(ang)
            cv.line([(C[0] + math.cos(a) * 7 * fill, C[1] - 1 + math.sin(a) * 9 * fill), (C[0] + math.cos(a) * 14, C[1] + math.sin(a) * 13)], "c", a=220)

    frames = []
    for k in range(6):
        cv = Cv(32, 32)
        cocoon(cv, fill=(0.3, 0.5, 0.7, 0.85, 1.05, 1.0)[k])
        frames.append({"FX": cv.im})
    s.tag("CocoonWrap", frames, 80, repeat=1)
    frames = []
    for k in range(6):
        cv = Cv(32, 32)
        cocoon(cv, breathe=(0, 0.3, 0.5, 0.3, 0, 0)[k], shake=(0, 0, 0, 0, 1, -1)[k])   # the ant inside still struggling
        frames.append({"FX": cv.im})
    s.tag("Cocooned", frames, 160)
    frames = []
    for k in range(5):
        cv = Cv(32, 32)
        if k == 0:
            cocoon(cv)
            cv.im = tint(cv.im, "w", 0.8)
            cv.p = cv.im.load()
        else:
            rng = random.Random(8)
            for j in range(10):                                   # shreds of silk thrown clear
                ang, spd = rng.uniform(0, 6.28), rng.uniform(0.6, 1.2)
                d = (3 + k * 3.2) * spd
                x, y = C[0] + math.cos(ang) * d, C[1] - 1 + math.sin(ang) * d
                tail = (x + math.cos(ang + 1) * 4, y + math.sin(ang + 1) * 4)
                cv.line([(x, y), tail], "w" if j % 2 else "c", a=255 - k * 30)
                cv.line([(x, y + 1), (tail[0], tail[1] + 1)], "s", a=255 - k * 30)
            if k <= 2:
                cv.ring(C[0], C[1] - 1, 5 + k * 3, "w", 1, a=200 - k * 60)
        frames.append({"FX": cv.im})
    s.tag("CocoonBreak", frames, 70, repeat=1)

    frames = []
    for k in range(6):
        cv = Cv(32, 32)
        cv.ring(C[0], C[1] + 3, 13, "u", 1, a=170, ry=9, gaps=lambda d, k=k: int((d + k * 12) / 24) % 3 == 0)
        for j, x in enumerate((6, 12, 19, 25)):                   # steam off the mound, and the ant keeping its head down
            y = 20 - ((k * 3 + j * 5) % 18)
            cv.blob(x + (1 if (k + j) % 2 else 0), y, 2.2 - (20 - y) * 0.07, 1.8 - (20 - y) * 0.06, "c", "w", a=max(0, 210 - (20 - y) * 9))
        for j, (x, y) in enumerate(((7, 24), (24, 25), (15, 28), (20, 22))):
            if (k + j) % 3 == 0:
                cv.blob(x, y, 1.4, 1.2, "y", "w")
            elif (k + j) % 3 == 1:
                cv.px(x, y, "u")
        frames.append({"FX": cv.im})
    s.tag("Scalded", frames, 110)
    out.append(s)
    return out


# ================================================================ run
def main():
    groups = (("BossMovesPreview.png", 2, build_bosses()),
              ("SpecialEnemyPreview.png", 3, build_enemies()),
              ("BossFxPreview.png", 2, build_props() + build_fx()))
    count = frames = 0
    for path, scale, sprites in groups:
        results = []
        for s in sprites:
            results.append((s, export(s)))
            n = sum(len(f) for _, f, _, _ in s.tags)
            count, frames = count + 1, frames + n
            print(f"{s.group:8} {s.name:20} {s.w}x{s.h}  {n:3} frames  " + ", ".join(t[0] for t in s.tags))
        base.preview_sheet(results, os.path.join(HERE, path), scale=scale)
    print(f"{count} sprites, {frames} frames written and read back OK")


if __name__ == "__main__":
    main()
