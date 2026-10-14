"""Hold the Hill weapon sheets: projectiles, trails, hits, misses, muzzle puffs and charge-ups.

Three groups, all in the ant-world style of build_anim_sheets.py (which this imports):

  Weapons/Wpn<Family>      one per firing-sound family (23): Fly, Trail, Hit, Miss, Muzzle and,
                           for the heavier ones, Charge
  Fx/FxTower<Name>         extra pieces for the 10 graybox towers: Muzzle, Trail, Miss, Charge,
                           plus one special tag where the weapon has one
  Towers/Tower<Caste>      Worker, Soldier, Major and Nurse (Idle / Attack / Upgrade)
  Fx/FxMelee               Bite, Slash, Slam, Block and Heal for those castes

    python build_weapon_sheets.py

Every weapon file is 32x32 with the effect centred, so one sheet row per tag. Conventions:

  Fly     loops; the shot faces RIGHT (+x), ready to rotate along its path. For the beam,
          bolt, flame and wave weapons it is a strip or burst that also runs left to right.
  Trail   one-shot; spawn copies at the shot's position as it flies and let them fade.
  Hit     one-shot at the point of impact on an enemy.
  Miss    one-shot when the shot lands on bare ground: the hit, duller and dustier.
  Muzzle  one-shot at the tower's mouth, opening to the right.
  Charge  one-shot wind-up before firing.
  Fly2, Fly3, Hit2, Hit3
          the shot and its hit for a tier 2 and tier 3 tower: a size up, a rim of glow,
          and at tier 3 sparkles and a second ring.
"""
import math
import os
import random

from PIL import Image

import build_anim_sheets as base
from build_anim_sheets import (Cv, Sprite, ant, burst, export, fade, flat, idle_kw, preview_sheet,
                               tint, tower_sprite, CX, FLAKE_S, SPARK_S, TW)

HERE = os.path.dirname(os.path.abspath(__file__))
N = 32
C0 = 16          # canvas centre (pixel-edge coordinate)
CY = 16.5        # row-centred, so thin shots sit on one pixel row
DIRT = "#87603a"


# ================================================================ projectiles in flight
def fly_blob(cv, k, c, size=1.0):
    rx, ry = ((3.0, 2.3) if k % 2 == 0 else (2.5, 2.7))
    cv.blob(C0 + 1, CY, rx * size, ry * size, c[0], c[1], c[2])
    cv.outline()
    cv.px(C0 - 3 * size - 1, CY - 0.5 + (k % 2), c[0])
    cv.px(C0 - 3 * size - 3, CY - 0.5, c[2])


def fly_rock(cv, k, c, r=3.0, cap=False):
    cv.blob(C0, CY, r, r if not cap else r * 0.85, c[0], c[1], c[2])
    if cap:  # acorn: a darker cap over the back third
        for y in range(int(CY - r), int(CY + r) + 1):
            for x in range(int(C0 - r) - 1, int(C0 - r * 0.25)):
                if cv.p[x, y][3]:
                    cv.px(x, y, c[2])
    cv.outline()
    ang = math.radians(k * 90 + 225)
    cv.px(C0 + math.cos(ang) * r * 0.5 - 0.5, CY + math.sin(ang) * r * 0.5 - 0.5, "w")


def fly_shard(cv, k, c):
    for dx in range(-5, 6):
        half = max(0, 2 - abs(dx + 1) // 2) if dx < 4 else 0
        for dy in range(-half, half + 1):
            cv.px(C0 + dx, CY - 0.5 + dy, c[1] if dy < 0 else c[0] if dy == 0 else c[2])
    cv.outline()
    cv.px(C0 - 1 + (k % 3), CY - 1.5, "w")


def fly_dart(cv, k, c, striped=False, long=False):
    length = 7 if long else 5
    for dx in range(-length, 4):
        col = c[0]
        if striped and (dx // 2) % 2 == 0:
            col = "k"
        cv.px(C0 + dx, CY - 0.5, col)
    cv.pxs([(C0 + 4, CY - 0.5), (C0 + 5, CY - 0.5), (C0 + 3, CY - 1.5), (C0 + 3, CY + 0.5)], c[1])   # head
    wob = k % 2
    cv.pxs([(C0 - length, CY - 1.5 - wob), (C0 - length, CY + 0.5 + wob), (C0 - length + 1, CY - 1.5), (C0 - length + 1, CY + 0.5)], c[2])
    cv.outline()


def fly_orb(cv, k, c):
    cv.ring(C0, CY, 5, c[0], 1, a=110 + (k % 2) * 60)
    cv.blob(C0, CY, 3, 3, c[0], c[1], c[2])
    cv.blob(C0 - 0.5, CY - 0.5, 1.2, 1.2, "w")
    ang = math.radians(k * 90)
    cv.px(C0 + math.cos(ang) * 5 - 0.5, CY + math.sin(ang) * 5 - 0.5, c[1])
    cv.px(C0 - math.cos(ang) * 5 - 0.5, CY - math.sin(ang) * 5 - 0.5, c[1])


def fly_bubble(cv, k, c):
    r = 3.6 + (0.3 if k % 2 else 0)
    cv.disc(C0, CY, r, c[0], a=70)
    cv.ring(C0, CY, r, c[0], 1)
    cv.ring(C0, CY, r - 1, c[2], 1, a=120)
    cv.px(C0 - 2 + (k % 2), CY - 2.5, "w")
    cv.px(C0 - 1 + (k % 2), CY - 2.5, "w")
    cv.px(C0 + 1.5, CY + 1.5, c[1], a=200)


def fly_puff(cv, k, c):
    rng = random.Random(k)
    for j in range(5):
        ang = math.radians(j * 72 + k * 25)
        cv.blob(C0 + math.cos(ang) * 2.2, CY + math.sin(ang) * 1.8, 2.2 + rng.random(), 2.0 + rng.random(), c[0], c[1], c[2], a=230)
    cv.blob(C0, CY, 2.2, 2, c[1], a=240)
    for j in range(3):
        cv.px(C0 - 5 - j * 2, CY - 1.5 + ((j + k) % 3), c[0], a=200 - j * 50)


def fly_seeds(cv, k, c):
    for j in range(3):
        ang = math.radians(j * 120 + k * 30)
        x, y = C0 + math.cos(ang) * 3, CY + math.sin(ang) * 3
        s = Cv(N, N)
        s.blob(x, y, 1.8, 1.3, c[0], c[1], c[2])
        s.outline()
        cv.paste(s)


def fly_web(cv, k, c):
    cv.blob(C0 + 1, CY, 3.2, 3.2, c[0], c[1], c[2])
    cv.outline()
    for a in range(0, 180, 60):
        ang = math.radians(a + k * 15)
        cv.line([(C0 + 1 - math.cos(ang) * 2.5, CY - math.sin(ang) * 2.5), (C0 + 1 + math.cos(ang) * 2.5, CY + math.sin(ang) * 2.5)], c[2])
    cv.line([(C0 - 3, CY - 0.5), (C0 - 7, CY - 0.5 + (1 if k % 2 else -1)), (C0 - 11, CY - 0.5)], c[0], a=200)


def fly_beam(cv, k, c):
    """A strip that tiles left to right; frames slide the bright nodes along."""
    for x in range(N):
        cv.px(x, CY - 2.5, c[2], a=150)
        cv.px(x, CY + 1.5, c[2], a=150)
        cv.px(x, CY - 1.5, c[0])
        cv.px(x, CY + 0.5, c[0])
        cv.px(x, CY - 0.5, "w")
    for x0 in (4, 20):
        for dx in range(3):
            x = (x0 + dx + k * 4) % N
            cv.px(x, CY - 1.5, "w")
            cv.px(x, CY + 0.5, "w")


def fly_bolt(cv, k, c):
    rng = random.Random(40 + k)
    y = CY - 0.5
    pts = [(0, y)]
    for x in range(3, N, 3):
        y = min(CY + 4, max(CY - 5, y + rng.choice((-2, -1, 1, 2))))
        pts.append((x, y))
    pts.append((N - 1, CY - 0.5))
    cv.line([(x, yy + 1) for x, yy in pts], c[0], a=190)
    cv.line(pts, "w")
    fx, fy = pts[rng.randrange(2, len(pts) - 2)]
    cv.line([(fx, fy), (fx + 2, fy - 3)], c[0])


def fly_wave(cv, k, c):
    """Three arcs sweeping right, like ripples off a drum."""
    for j in range(3):
        cx = 2 + j * 7 + k * 1.75
        a = 255 - j * 50
        for dy in range(-6, 7):
            x = cx + math.sqrt(max(0, 64 - dy * dy * 1.6)) * 0.75
            cv.px(x, CY - 0.5 + dy, c[1] if abs(dy) < 3 else c[0], a=a if abs(dy) < 5 else a // 2)


def fly_flame(cv, k, c):
    rng = random.Random(60 + k)
    for j in range(7):
        x = 5 + j * 3.4
        r = 1.6 + j * 0.45 + rng.uniform(-0.3, 0.3)
        y = CY + rng.uniform(-1, 1) * (j * 0.25)
        cv.blob(x, y, r, r, c[0], c[1], c[2], a=255 - j * 10)
    for j in range(6):
        cv.blob(5 + j * 3.4, CY, 1.0 + j * 0.3, 1.0 + j * 0.3, c[1], "w" if j < 3 else c[1], c[0])
    cv.px(6, CY - 0.5, "w")


def fly_harpoon(cv, k, c):
    fly_dart(cv, k, c, long=True)
    cv.pxs([(C0 + 2, CY - 2.5), (C0 + 2, CY + 1.5)], c[1])                 # barbs
    y = CY - 0.5
    for x in range(int(C0 - 9), -1, -1):                                   # silk line back to the tower
        cv.px(x, y + round(math.sin((x + k * 2) * 0.7)), "c", a=210)


FLY = {"blob": fly_blob, "rock": fly_rock, "shard": fly_shard, "dart": fly_dart, "orb": fly_orb,
       "bubble": fly_bubble, "puff": fly_puff, "seeds": fly_seeds, "web": fly_web, "beam": fly_beam,
       "bolt": fly_bolt, "wave": fly_wave, "flame": fly_flame, "harpoon": fly_harpoon}


# ================================================================ trails (one-shot, 4 frames)
def trail(kind, cv, k, c):
    a = [255, 200, 130, 60][k]
    if kind == "drips":
        for j, dx in enumerate((-2, 1, 3)):
            cv.px(C0 + dx, CY - 1 + k * (1 + j % 2), c[0] if j else c[2], a=a)
    elif kind == "sparks":
        rng = random.Random(k)
        for j in range(4 - k // 2):
            cv.px(C0 + rng.randint(-3, 3), CY + rng.randint(-3, 2), "w" if j == 0 else c[0], a=a)
    elif kind == "smoke":
        r = 1.5 + k * 0.9
        cv.blob(C0 - k, CY - k * 0.8, r, r, "S", "s", "x", a=a * 3 // 4)
    elif kind == "frost":
        if k < 2:
            cv.spr(FLAKE_S, C0 - 1.5, CY - 1.5)
        cv.pxs([(C0 - 3 - k, CY - 2), (C0 + 2, CY + 2 + k)], "b", a=a)
        cv.px(C0, CY, "w", a=a)
    elif kind == "dust":
        r = 1.3 + k * 0.8
        cv.blob(C0 - k, CY, r, r * 0.8, "t", "c", "T", a=a * 2 // 3)
    elif kind == "glow":
        for j in range(3):
            cv.px(C0 - j * 2 - k, CY - 1 + (j % 2) * 2, c[1] if j == 0 else c[0], a=a)
        cv.ring(C0, CY, 1.5 + k, c[0], 1, a=a // 3)
    elif kind == "ripple":
        cv.ring(C0, CY, 2 + k * 1.6, c[0], 1, a=a)
    elif kind == "streak":
        cv.line([(C0 - 5 + k, CY - 0.5), (C0 + 3 - k, CY - 0.5)], c[1], a=a)
        cv.line([(C0 - 3 + k, CY + 0.5), (C0 + 1 - k, CY + 0.5)], c[0], a=a // 2)
    elif kind == "thread":
        cv.line([(C0 - 7, CY - 0.5), (C0, CY - 0.5 + k), (C0 + 6, CY - 0.5)], "c", a=a)
    elif kind == "bubbles":
        for j, (dx, r) in enumerate(((-2, 1.5), (2, 1.0), (0, 0.8))):
            cv.ring(C0 + dx, CY - k * (1 + j * 0.5), r + 0.5, c[0], 1, a=a)


# ================================================================ hits (one-shot, 5 frames)
def hit(kind, cv, k, c):
    n = 5
    t = k / (n - 1)
    if kind == "splat":
        burst(cv, C0, CY, k, n, [c[0], c[2], c[1]], rays=7, reach=8, seed=1, core=4)
        if k >= 1:
            for j in range(6):
                ang = math.radians(j * 60 + 15)
                cv.px(C0 + math.cos(ang) * (3 + k * 1.6), CY + math.sin(ang) * (3 + k * 1.6) * 0.8, c[2], a=255 - k * 45)
    elif kind == "spark":
        burst(cv, C0, CY, k, n, ["w", c[1], c[0]], rays=6, reach=8, seed=2, core=2.2)
    elif kind == "crumble":
        rng = random.Random(6)
        for j in range(6):
            ang = math.radians(j * 60 + 10)
            d = 3 + t * 7
            r = (2.8 - t * 1.4) * rng.uniform(0.8, 1.1)
            cv.blob(C0 + math.cos(ang) * d, CY + math.sin(ang) * d * 0.75, r, r, "t", "c", "T", a=int(225 * (1 - t * 0.8)))
        burst(cv, C0, CY, k, n, ["w", c[1], c[0]], rays=7, reach=9, seed=2, core=3)
    elif kind == "burstglow":
        burst(cv, C0, CY, k, n, [c[0], c[1], "w"], rays=8, reach=9, seed=3, core=3.5)
        cv.ring(C0, CY, 3 + k * 2.4, c[1], 1, a=255 - k * 55)
    elif kind == "shatter":
        rng = random.Random(7)
        for j in range(7):
            ang = math.radians(j * 51 + rng.uniform(-10, 10))
            d = 2 + t * 9 * rng.uniform(0.7, 1.0)
            x, y = C0 + math.cos(ang) * d, CY + math.sin(ang) * d
            cv.line([(x, y), (x + math.cos(ang + 1) * 2, y + math.sin(ang + 1) * 2)], c[1] if j % 2 else "w", a=255 - k * 45)
        if k < 2:
            cv.blob(C0, CY, 3 - k, 3 - k, "w", "w", c[0])
        if k in (1, 2):
            cv.spr(FLAKE_S, C0 - 6, CY - 6)
            cv.spr(FLAKE_S, C0 + 3, CY + 2)
    elif kind == "puffcloud":
        rng = random.Random(5)
        for j in range(6):
            ang = math.radians(j * 60 + 20)
            d = 2 + t * 6
            r = (3.2 - t * 1.6) * rng.uniform(0.8, 1.1)
            cv.blob(C0 + math.cos(ang) * d, CY + math.sin(ang) * d * 0.8, r, r, c[0], c[1], c[2], a=int(235 * (1 - t * 0.75)))
    elif kind == "blast":
        if k == 0:
            cv.disc(C0, CY, 5, "w")
            cv.ring(C0, CY, 7, "y", 1)
        else:
            r = [0, 8, 10, 11, 12][k]
            cv.disc(C0, CY - k * 0.5, r, "S", a=max(0, 110 - k * 20))
            cv.ring(C0, CY - k * 0.5, r, "x", 1, a=max(0, 220 - k * 45))
            fire = [0, 7, 5, 3, 0][k]
            if fire:
                cv.blob(C0, CY, fire, fire, c[0], c[1], c[2])
                cv.disc(C0, CY - 0.5, fire * 0.4, "w" if k == 1 else c[1])
    elif kind == "sticky":
        # spreads into a flat puddle that lingers
        rx = 3 + t * 6
        cv.blob(C0, CY + 1, rx, 2 + t * 2.2, c[0], c[1], c[2], a=255 - k * 22)
        cv.outline(c[2])
        if k < 3:
            for j in range(5):
                ang = math.radians(j * 72)
                cv.px(C0 + math.cos(ang) * (rx + 2), CY + 1 + math.sin(ang) * 4, c[0], a=220 - k * 60)
        cv.px(C0 - rx * 0.4, CY, "w", a=220 - k * 30)
    elif kind == "ring":
        cv.ring(C0, CY, 3 + k * 2.6, c[1], 2 if k < 2 else 1, a=255 - k * 50)
        cv.ring(C0, CY, 1 + k * 2.0, c[0], 1, a=200 - k * 45)
        if k == 0:
            cv.blob(C0, CY, 2, 2, "w")
    elif kind == "web":
        r = 4 + min(k, 2) * 3
        a = 255 - max(0, k - 2) * 70
        for ang in range(0, 360, 45):
            x, y = C0 + math.cos(math.radians(ang)) * r, CY + math.sin(math.radians(ang)) * r
            cv.line([(C0, CY), (x, y)], c[0], a=a)
        for rr in (r * 0.5, r * 0.9):
            pts = [(C0 + math.cos(math.radians(a2)) * rr, CY + math.sin(math.radians(a2)) * rr) for a2 in range(0, 361, 45)]
            cv.line(pts, c[1], a=a)
    elif kind == "scorch":
        if k < 2:
            cv.blob(C0, CY, 4 - k, 4 - k, "w", "w", c[0])
            cv.ring(C0, CY, 5 + k * 2, c[0], 1, a=230 - k * 60)
        else:
            cv.blob(C0, CY + 1, 4, 2.6, "x", "S", "k", a=230 - (k - 2) * 60)
            cv.px(C0 - 1, CY - 2 - (k - 2) * 2, "s", a=200 - (k - 2) * 60)   # a wisp of steam
            cv.px(C0 + 1, CY - 3 - (k - 2) * 2, "c", a=160 - (k - 2) * 50)


def miss_from(hit_cv, k):
    """The hit, but landing on bare ground: smaller-feeling, dirt-coloured, with kicked-up specks."""
    im = fade(tint(hit_cv.im, DIRT, 0.6, keep_ink=False), 0.85)
    cv = Cv(N, N)
    cv.paste(im)
    rng = random.Random(11)
    for j in range(5):
        ang = math.radians(200 + j * 35 + rng.uniform(-10, 10))
        d = 3 + k * 2.2
        cv.px(C0 + math.cos(ang) * d, CY + math.sin(ang) * d * 0.7 - (2 - abs(k - 1.5)), "d" if j % 2 else "T", a=255 - k * 55)
    return cv


# ================================================================ muzzle (3 frames) and charge (6)
def muzzle(kind, cv, k, c):
    a = [255, 200, 110][k]
    if kind == "spit":
        cv.blob(C0 + k, CY, 2.6 - k * 0.7, 2.2 - k * 0.6, c[0], c[1], c[2], a=a)
        for j, ang in enumerate((-40, -15, 10, 35)):
            d = 4 + k * 3 + (j % 2)
            x, y = C0 + math.cos(math.radians(ang)) * d, CY + math.sin(math.radians(ang)) * d
            cv.blob(x, y, 1.3 - k * 0.25, 1.3 - k * 0.25, c[0], c[1], c[2], a=a)
        if k == 0:
            cv.px(C0 + 1, CY - 1, "w")
    elif kind == "flash":
        r = [5, 7, 4][k]
        for ang in (-50, -25, 0, 25, 50):
            x, y = C0 + math.cos(math.radians(ang)) * r * (1.4 if ang == 0 else 1), CY + math.sin(math.radians(ang)) * r
            cv.line([(C0, CY - 0.5), (x, y)], "w" if ang == 0 else c[1], a=a)
        cv.blob(C0 + 1, CY, 2.5 - k * 0.6, 2.5 - k * 0.6, "w", "w", c[0], a=a)
        if k:
            cv.blob(C0 - 2 - k, CY - 2 - k, 2 + k * 0.5, 2 + k * 0.5, "S", "s", "x", a=150 - k * 30)
    elif kind == "dust":
        for j in range(4):
            ang = math.radians(-60 + j * 40)
            d = 2 + k * 2.5
            cv.blob(C0 + math.cos(ang) * d, CY + math.sin(ang) * d, 2 - k * 0.4, 2 - k * 0.4, "t", "c", "T", a=a * 3 // 4)
    elif kind == "glow":
        cv.ring(C0, CY, 3 + k * 2.5, c[1], 1, a=a)
        cv.blob(C0, CY, 3 - k, 3 - k, c[0], "w", c[2], a=a)
    elif kind == "snap":
        for ang in (-35, 0, 35):
            d0, d1 = 2 + k * 2, 5 + k * 2
            cv.line([(C0 + math.cos(math.radians(ang)) * d0, CY - 0.5 + math.sin(math.radians(ang)) * d0),
                     (C0 + math.cos(math.radians(ang)) * d1, CY - 0.5 + math.sin(math.radians(ang)) * d1)], c[1], a=a)
    elif kind == "flare":
        r = [6, 8, 5][k]
        cv.line([(C0 - r, CY - 0.5), (C0 + r, CY - 0.5)], "w", a=a)
        cv.line([(C0, CY - r), (C0, CY + r - 1)], "w", a=a)
        cv.ring(C0, CY, r * 0.6, c[0], 1, a=a)
        cv.blob(C0, CY, 2, 2, "w")


def charge(kind, cv, k, c):
    n = 6
    t = k / (n - 1)
    if kind == "gather":      # motes spiral in, the core swells, then a flash
        r = 12 * (1 - t) + 2
        for j in range(8):
            ang = math.radians(j * 45 + k * 22)
            cv.px(C0 + math.cos(ang) * r, CY + math.sin(ang) * r, c[1] if j % 2 else c[0], a=160 + int(95 * t))
        cv.blob(C0, CY, 1 + t * 3, 1 + t * 3, c[0], "w", c[2])
        if k == n - 1:
            cv.ring(C0, CY, 6, "w", 1)
    elif kind == "heave":     # the ground strains: dust and pebbles lift, then drop
        lift = math.sin(t * math.pi) * 5
        for j in range(6):
            x = C0 - 8 + j * 3.2
            cv.blob(x, CY + 4 - lift * (0.5 + (j % 3) * 0.25), 1.4, 1.2, "t" if j % 2 else "d", "c", "T", a=230)
        cv.line([(C0 - 9, CY + 6), (C0 + 9, CY + 6)], "D", a=120 + int(100 * t))
        if k >= 4:
            cv.spr(SPARK_S, C0 - 1.5, CY - 6)
    elif kind == "crackle":   # sparks jump around a growing knot of static
        rng = random.Random(80 + k)
        for j in range(3 + k):
            ang = rng.uniform(0, 2 * math.pi)
            d = rng.uniform(2, 4 + t * 6)
            x, y = C0 + math.cos(ang) * d, CY + math.sin(ang) * d
            cv.line([(C0, CY), ((C0 + x) / 2 + rng.choice((-1, 1)), (CY + y) / 2), (x, y)], "w" if j % 2 else c[0], a=200)
        cv.blob(C0, CY, 1 + t * 2, 1 + t * 2, c[0], "w", c[2])


# ================================================================ tier 2 and tier 3 shots and hits
STRIPS = ("beam", "bolt", "wave", "flame")   # run edge to edge, so they get no orbiting sparkles


def glow_rim(im, col, a, diagonal=False):
    """A 1 px rim of col around everything already drawn, outside the ink outline."""
    src = im.load()
    out = im.copy()
    p = out.load()
    near = ((1, 0), (-1, 0), (0, 1), (0, -1)) + (((1, 1), (-1, 1), (1, -1), (-1, -1)) if diagonal else ())
    rim = base.rgba(col, a)
    for y in range(N):
        for x in range(N):
            if src[x, y][3]:
                continue
            for dx, dy in near:
                nx, ny = x + dx, y + dy
                if 0 <= nx < N and 0 <= ny < N and src[nx, ny][3] > 100:
                    p[x, y] = rim
                    break
    return out


def tier_fly(fly, k, c, opts, tier):
    opts = dict(opts)
    if fly == "blob":
        opts["size"] = opts.get("size", 1.0) * (1.2 if tier == 2 else 1.4)
    elif fly == "rock":
        opts["r"] = opts.get("r", 3.0) + 0.7 * (tier - 1)
    cv = Cv(N, N)
    if fly == "bolt" and tier == 3:          # a second arc flickering behind the first
        ghost = Cv(N, N)
        FLY[fly](ghost, k + 2, c, **opts)
        cv.paste(ghost, alpha=0.55)
    FLY[fly](cv, k, c, **opts)
    im = glow_rim(cv.im, c[1], 170 if tier == 2 else 230)
    if tier == 3:
        im = glow_rim(im, c[0], 110, diagonal=True)
    out = Cv(N, N)
    out.paste(im)
    if tier == 3 and fly not in STRIPS:
        ang = math.radians(k * 90 + 45)
        for sign in (1, -1):
            out.px(C0 + sign * math.cos(ang) * 8 - 0.5, CY + sign * math.sin(ang) * 6 - 0.5, "w")
        for j in range(3):                   # a short bright wake
            out.px(C0 - 9 - j * 2, CY - 0.5 + ((j + k) % 3 - 1), c[1], a=220 - j * 60)
    return out.im


def tier_hit(base_im, k, c, tier):
    """base_im: the tier 1 hit frame, 32x32. Adds a ring at tier 2; rays and sparkles too at tier 3."""
    cv = Cv(N, N)
    cv.ring(C0, CY, 5 + k * 2.6, c[1], 2 if (tier == 3 and k < 2) else 1, a=max(0, 240 - k * 50))
    if tier == 3:
        burst(cv, C0, CY, k, 5, ["w", c[1]], rays=10, reach=11, seed=20)
        if k in (1, 2):
            for dx, dy in ((-9, -8), (7, -9), (8, 6), (-10, 5)):
                cv.spr(SPARK_S, C0 + dx, CY + dy)
    cv.paste(base_im)
    return cv.im


def centred(im):
    """A smaller frame (16x16 projectile, 24x24 impact) on the 32x32 canvas."""
    out = Image.new("RGBA", (N, N), (0, 0, 0, 0))
    out.paste(im, ((N - im.width) // 2, (N - im.height) // 2))
    return out


def tier_tags(s, c, fly_frames, hit_frames):
    """fly_frames(tier) -> images, or None. hit_frames: the tier 1 hit images, or None."""
    for tier in (2, 3):
        if fly_frames:
            s.tag(f"Fly{tier}", [{"FX": im} for im in fly_frames(tier)], 70)
    for tier in (2, 3):
        if hit_frames:
            s.tag(f"Hit{tier}", [{"FX": tier_hit(im, k, c, tier)} for k, im in enumerate(hit_frames)], 55, repeat=1)


# ================================================================ the 23 weapon families
#  name, organic reading, fly, colours (base, highlight, shade), trail, hit, muzzle, charge, fly options
WEAPONS = [
    ("AcidSpit", "Formic acid glob", "blob", ("g", "w", "G"), "drips", "splat", "spit", None, {}),
    ("BoulderToss", "Heaved river pebble", "rock", ("S", "s", "x"), "dust", "crumble", "dust", "heave", {"r": 4.2}),
    ("BubbleShot", "Dew bubble", "bubble", ("b", "w", "B"), "bubbles", "ring", "glow", None, {}),
    ("Cannon", "Seed-pod launcher", "rock", ("d", "T", "D"), "smoke", "blast", "flash", "heave", {"r": 3.0}),
    ("Catapult", "Acorn lob", "rock", ("T", "t", "d"), "dust", "crumble", "dust", "heave", {"r": 3.6, "cap": True}),
    ("Crossbow", "Thorn bolt", "dart", ("M", "g", "j"), "streak", "spark", "snap", None, {}),
    ("EmberFlick", "Glowing ember", "blob", ("o", "y", "r"), "sparks", "burstglow", "snap", None, {"size": 0.7}),
    ("FlameBurst", "Formic fire jet", "flame", ("o", "y", "r"), "smoke", "blast", "flash", "gather", {}),
    ("Harpoon", "Barbed stinger on silk", "harpoon", ("s", "w", "S"), "thread", "spark", "snap", "gather", {}),
    ("HoneyGlob", "Honey glob", "blob", ("u", "y", "Y"), "drips", "sticky", "spit", None, {"size": 1.25}),
    ("IceShard", "Frost shard", "shard", ("b", "w", "B"), "frost", "shatter", "glow", None, {}),
    ("Laser", "Dew-lens ray", "beam", ("b", "w", "B"), "glow", "scorch", "flare", "gather", {}),
    ("LightningBolt", "Static arc", "bolt", ("y", "w", "Y"), "sparks", "burstglow", "flare", "crackle", {}),
    ("MagicOrb", "Royal-jelly orb", "orb", ("P", "p", "V"), "glow", "burstglow", "glow", "gather", {}),
    ("Mortar", "Mud bomb", "rock", ("D", "d", "e"), "dust", "blast", "dust", "heave", {"r": 3.6}),
    ("PebbleSling", "Slung pebble", "rock", ("s", "c", "S"), "streak", "spark", "dust", None, {"r": 2.2}),
    ("Plasma", "Glow-spore", "orb", ("i", "I", "iz"), "glow", "burstglow", "glow", "gather", {}),
    ("PollenPuff", "Pollen cloud", "puff", ("y", "c", "Y"), "dust", "puffcloud", "dust", None, {}),
    ("SeedBurst", "Seed scatter", "seeds", ("T", "t", "d"), "dust", "spark", "dust", None, {}),
    ("SonicPulse", "Stridulation wave", "wave", ("s", "c", "S"), "ripple", "ring", "glow", "crackle", {}),
    ("StingerDart", "Wasp-stinger dart", "dart", ("y", "w", "Y"), "streak", "splat", "snap", None, {"striped": True}),
    ("VenomBolt", "Venom bolt", "blob", ("P", "p", "V"), "drips", "splat", "spit", None, {"size": 1.1}),
    ("WebShot", "Silk web ball", "web", ("c", "w", "s"), "thread", "web", "snap", None, {}),
]


def frames_of(n, draw):
    out = []
    for k in range(n):
        cv = Cv(N, N)
        draw(cv, k)
        out.append({"FX": cv.im})
    return out


def weapon_tags(s, fly, c, trail_kind, hit_kind, muzzle_kind, charge_kind, opts, with_fly=True, with_hit=True):
    if with_fly:
        s.tag("Fly", frames_of(4, lambda cv, k: FLY[fly](cv, k, c, **opts)), 70)
    s.tag("Trail", frames_of(4, lambda cv, k: trail(trail_kind, cv, k, c)), 60, repeat=1)
    hits = []
    for k in range(5):
        cv = Cv(N, N)
        hit(hit_kind, cv, k, c)
        hits.append(cv)
    if with_hit:
        s.tag("Hit", [{"FX": cv.im} for cv in hits], 55, repeat=1)
    s.tag("Miss", [{"FX": miss_from(hits[k], k).im} for k in range(4)], 60, repeat=1)
    s.tag("Muzzle", frames_of(3, lambda cv, k: muzzle(muzzle_kind, cv, k, c)), 50, repeat=1)
    if charge_kind:
        s.tag("Charge", frames_of(6, lambda cv, k: charge(charge_kind, cv, k, c)), 80, repeat=1)


def build_weapons():
    out = []
    for name, reading, fly, c, trail_kind, hit_kind, muzzle_kind, charge_kind, opts in WEAPONS:
        s = Sprite("Wpn" + name, "Weapons", N, N, ["FX"], reading)
        weapon_tags(s, fly, c, trail_kind, hit_kind, muzzle_kind, charge_kind, opts)
        hits = dict((t[0], t[1]) for t in s.tags)["Hit"]
        tier_tags(s, c, lambda tier, fly=fly, c=c, opts=opts: [tier_fly(fly, k, c, opts, tier) for k in range(4)],
                  [f["FX"] for f in hits])
        out.append(s)
    return out


# ================================================================ extras for the 10 graybox towers
# These towers already have a tower sheet, a projectile (Proj*) and a hit (FxImpact). This adds
# what they lack. Colours match the existing shots.
def special_bounce(cv, k):     # Slinger: the pebble glancing off one enemy on its way to the next
    burst(cv, C0, CY, k, 4, ["w", "c", "s"], rays=5, reach=6, seed=9, core=1.6)
    cv.line([(C0 - 6 + k, CY + 3), (C0, CY - 0.5), (C0 + 6 - k, CY + 3)], "c", a=220 - k * 50)


def special_freeze(cv, k):     # Frost Ant: an ice crust that sits over a chilled enemy (loops)
    rng = random.Random(3)
    for j in range(9):
        ang = math.radians(j * 40)
        r = 8 + (1 if (j + k) % 3 == 0 else 0)
        x, y = C0 + math.cos(ang) * r, CY + math.sin(ang) * r * 0.8
        cv.line([(x, y), (x - math.cos(ang) * 3, y - math.sin(ang) * 3)], "b" if j % 2 else "w", a=220)
    cv.ring(C0, CY, 8, "b", 1, a=110, ry=6.5)
    for j in range(3):
        x, y = C0 + rng.randint(-5, 5), CY + rng.randint(-4, 4)
        if (j + k) % 2 == 0:
            cv.px(x, y, "w")


def special_crack(cv, k):      # Kicker: cracks in the ground under the stomp, fading
    rng = random.Random(14)
    a = [255, 230, 190, 140, 90, 40][k]
    for j in range(6):
        ang = math.radians(j * 60 + rng.uniform(-15, 15))
        pts = [(C0, CY)]
        for step in range(1, 4):
            d = step * (2.5 + min(k, 2))
            pts.append((C0 + math.cos(ang + rng.uniform(-0.3, 0.3)) * d, CY + math.sin(ang + rng.uniform(-0.3, 0.3)) * d * 0.75))
        cv.line(pts, "e", a=a)
    cv.blob(C0, CY, 2, 1.5, "D", a=a)


def special_lay(cv, k):        # Sapper: dirt kicked up as a mine is dug in
    for j in range(6):
        ang = math.radians(200 + j * 28)
        d = 2 + k * 2.2
        cv.blob(C0 + math.cos(ang) * d, CY + math.sin(ang) * d * 0.7 - (3 - abs(k - 1.5) * 2), 1.6 - k * 0.25, 1.4 - k * 0.2,
                "d", "T", "D", a=250 - k * 50)
    if k < 2:
        cv.blob(C0, CY + 2, 5 - k, 2, "D", "d", "e", a=200)


def special_bite(cv, k):       # Swarm Nest: an orbiting ant's nip
    a = [255, 220, 120][k]
    for side in (-1, 1):
        cv.line([(C0 + side * (4 - k), CY - 4), (C0 + side * (1 if k else 2), CY - 0.5), (C0 + side * (4 - k), CY + 3)], "c", a=a)
    cv.px(C0, CY - 0.5, "w", a=a)


TOWER_EXTRAS = [
    # name, reading, colours, trail, hit kind (used only to make Miss), muzzle, charge, special
    ("Linear", "Spitter", ("g", "w", "G"), "drips", "splat", "spit", None, None),
    ("Homing", "Seeker", ("r", "f", "R"), "sparks", "burstglow", "snap", None, None),
    ("Mortar", "Bombardier", ("S", "s", "x"), "smoke", "blast", "flash", "heave", None),
    ("Ricochet", "Slinger", ("s", "c", "S"), "streak", "spark", "dust", None, ("Bounce", 4, 50, 1, special_bounce)),
    ("Chain", "Storm Ant", ("y", "w", "Y"), "sparks", "scorch", "flare", "crackle", None),
    ("Beam", "Dewdrop Lens", ("b", "w", "B"), "glow", "scorch", "flare", "gather", None),
    ("Orbit", "Swarm Nest", ("q", "Q", "Z"), "dust", "spark", "dust", None, ("Bite", 3, 60, 1, special_bite)),
    ("FrostAura", "Frost Ant", ("b", "w", "B"), "frost", "shatter", "glow", "gather", ("Freeze", 4, 140, 0, special_freeze)),
    ("Knockback", "Kicker", ("c", "w", "s"), "dust", "ring", "dust", "heave", ("Crack", 6, 90, 1, special_crack)),
    ("MineLayer", "Sapper", ("h", "H", "hz"), "dust", "blast", "dust", None, ("Lay", 4, 70, 1, special_lay)),
]


# Which existing shot and hit each graybox tower uses (see GrayboxImpactFx), for the tiered versions.
TOWER_SHOT = {"Linear": "ProjLinear", "Homing": "ProjHoming", "Mortar": "ProjMortar", "Ricochet": "ProjRicochet"}
TOWER_HIT = {"Linear": "Acid", "Homing": "Pop", "Ricochet": "Spark", "Chain": "Zap", "Beam": "Sizzle", "Orbit": "Scratch"}


def tiered_shot(frames, c, tier):
    out = []
    for k, f in enumerate(frames):
        im = glow_rim(centred(f["FX"]), c[1], 170 if tier == 2 else 230)
        if tier == 3:
            im = glow_rim(im, c[0], 110, diagonal=True)
            cv = Cv(N, N)
            cv.paste(im)
            ang = math.radians(k * 90 + 45)
            for sign in (1, -1):
                cv.px(C0 + sign * math.cos(ang) * 7 - 0.5, CY + sign * math.sin(ang) * 6 - 0.5, "w")
            im = cv.im
        out.append(im)
    return out


def build_tower_extras():
    out = []
    shots = {s.name: dict((t[0], t[1]) for t in s.tags) for s in base.build_projectiles()}
    impacts = {t[0]: t[1] for s in base.build_impacts() if s.name == "FxImpact" for t in s.tags}
    for name, reading, c, trail_kind, hit_kind, muzzle_kind, charge_kind, special in TOWER_EXTRAS:
        s = Sprite("FxTower" + name, "Fx", N, N, ["FX"], reading + " extras")
        weapon_tags(s, None, c, trail_kind, hit_kind, muzzle_kind, charge_kind, {}, with_fly=False, with_hit=False)
        if special:
            tag, n, ms, repeat, draw = special
            s.tag(tag, frames_of(n, draw), ms, repeat=repeat)
        fly = None
        if name in TOWER_SHOT:
            fly = lambda tier, fr=shots[TOWER_SHOT[name]]["Fly"], c=c: tiered_shot(fr, c, tier)
        if name in TOWER_HIT:
            hit_ims = [centred(f["FX"]) for f in impacts[TOWER_HIT[name]]]
        else:
            hit_ims = []
            for k in range(5):
                cv = Cv(N, N)
                hit(hit_kind, cv, k, c)
                hit_ims.append(cv.im)
        tier_tags(s, c, fly, hit_ims)
        out.append(s)
    return out


# ================================================================ the four melee castes
def slash_arc(cv, k, n, colors=("w", "c"), r=9.0, cy=6.0, sweep=140):
    """A crescent that sweeps left to right above the ant's head."""
    t = k / max(1, n - 1)
    a0 = 200 + sweep * max(0, t - 0.35)
    a1 = 200 + sweep * min(1, t + 0.35)
    for deg in range(int(a0), int(a1), 4):
        ang = math.radians(deg)
        x, y = CX + math.cos(ang) * r, cy + 9 + math.sin(ang) * r * 0.7
        cv.px(x, y, colors[0], a=int(255 * (1 - t * 0.5)))
        cv.px(x, y + 1, colors[1], a=int(170 * (1 - t * 0.6)))


def draw_worker(L, tag, i, n):
    # a worker carrying a clod of dirt; it bites and shoves
    if tag == "Idle":
        ant(L, **idle_kw(i))
        clod_y, mand = 5, 0
    else:
        lunge = [0, 1, -2, -2, -1, 0][i]
        ant(L, head_dy=lunge, mand=[1, 1, 0, 0, 1, 0][i], legs=[0, 1, -1, 0, 0, 0][i])
        clod_y, mand = 5 + lunge, 0
        if i in (2, 3):
            L["FX"].pxs([(CX - 5, 2), (CX + 4, 2), (CX - 6, 4), (CX + 5, 4)], "c")
    # a grey pebble held out in front of the jaws, so it reads against the brown head
    clod = Cv(TW, TW)
    clod.blob(CX, clod_y - 2, 2.8, 2.3, "S", "s", "x")
    clod.outline()
    L["FX"].paste(clod)


def draw_soldier(L, tag, i, n):
    col = ("q", "Q", "Z")
    if tag == "Idle":
        ant(L, col=col, head="soldier", mand=(0, 0, 1, 0)[i % 4], **idle_kw(i))
        return
    ant(L, col=col, head="soldier", mand=[2, 3, 0, 0, 1, 1][i], head_dy=[1, 1, -2, -1, 0, 0][i], abd=[0, 0.4, 0, 0, 0, 0][i])
    if i in (2, 3, 4):
        slash_arc(L["FX"], i - 2, 3, r=9, cy=-2)


def draw_major(L, tag, i, n):
    # the big-headed major plugs the tunnel and slams anything that gets close
    if tag == "Idle":
        ant(L, head="major", **idle_kw(i))
        return
    ant(L, head="major", lift=[1, 2, 0, 0, 0, 0][i], head_dy=[0, -1, 2, 2, 1, 0][i], bob=[0, 0, 1, 1, 0, 0][i], legs=[0, 1, -1, 0, 0, 0][i])
    fx = L["FX"]
    if i >= 2:
        r = [0, 0, 5, 8, 11, 13][i]
        fx.ring(CX, 10, r, "c", 2 if i < 4 else 1, a=255 - (i - 2) * 55, ry=r * 0.6)
        for side in (-1, 1):
            fx.line([(CX + side * (r + 1), 10), (CX + side * (r + 3), 8 + (i % 2))], "s", a=255 - (i - 2) * 55)


def draw_nurse(L, tag, i, n):
    col = ("#e9b49c", "#fff1d6", "#b9795f")   # a rosy nurse ant, clear of the tan mound
    def heart(fx, x, y, a=255):
        for dx, dy in ((0, 1), (1, 0), (2, 1), (3, 0), (4, 1), (0, 2), (1, 2), (2, 2), (3, 2), (4, 2), (1, 3), (2, 3), (3, 3), (2, 4), (1, 1), (3, 1)):
            fx.px(x + dx, y + dy, "f", a=a)
        fx.px(x + 1, y + 1, "w", a=a)
    if tag == "Idle":
        ant(L, col=col, **idle_kw(i))
        if i in (1, 2):
            heart(L["FX"], CX + 7, 4 - (i - 1), 200)
        return
    ant(L, col=col, abd=[0.3, 0.6, 0.9, 0.5, 0.2, 0][i], head_dy=[0, 1, 0, -1, 0, 0][i])
    fx = L["FX"]
    if 1 <= i <= 4:
        fx.ring(CX, 17, 4 + i * 2.6, "f", 1, a=240 - i * 40)
    for j, (x, y) in enumerate(((CX - 10, 12), (CX + 6, 9), (CX - 2, 3))):
        if i >= j + 1:
            heart(fx, x, y - (i - j) * 2, a=max(60, 255 - (i - j) * 45))


def build_castes():
    return [
        tower_sprite("TowerWorker", "Worker", draw_worker),
        tower_sprite("TowerSoldier", "Soldier", draw_soldier, attack_ms=60),
        tower_sprite("TowerMajor", "Major", draw_major, attack_ms=80),
        tower_sprite("TowerNurse", "Nurse", draw_nurse, attack_ms=90),
    ]


def build_melee_fx():
    s = Sprite("FxMelee", "Fx", N, N, ["FX"], "Melee and heal effects")

    def bite(cv, k):
        a = [255, 230, 130][k]
        gap = [5, 1, 3][k]
        for side in (-1, 1):
            cv.line([(C0 + side * (gap + 4), CY - 5), (C0 + side * gap, CY - 0.5), (C0 + side * (gap + 4), CY + 4)], "c", a=a)
            cv.line([(C0 + side * (gap + 5), CY - 4), (C0 + side * (gap + 1), CY - 0.5), (C0 + side * (gap + 5), CY + 3)], "s", a=a // 2)
        if k == 1:
            cv.spr(SPARK_S, C0 - 1.5, CY - 2)

    def slash(cv, k):
        t = k / 3
        for deg in range(int(200 + 150 * max(0, t - 0.3)), int(200 + 150 * min(1, t + 0.4)), 3):
            ang = math.radians(deg)
            for w, col, al in ((11, "w", 255), (10, "c", 200), (9, "Q", 120)):
                cv.px(C0 + math.cos(ang) * w, CY + 4 + math.sin(ang) * w * 0.8, col, a=int(al * (1 - t * 0.5)))

    def slam(cv, k):
        r = [3, 6, 9, 12, 14][k]
        a = 255 - k * 45
        cv.ring(C0, CY, r, "c", 2 if k < 3 else 1, a=a, ry=r * 0.7)
        rng = random.Random(4)
        for j in range(6):
            ang = math.radians(j * 60 + rng.uniform(-12, 12))
            cv.line([(C0 + math.cos(ang) * 2, CY + math.sin(ang) * 1.5), (C0 + math.cos(ang) * (r - 1), CY + math.sin(ang) * (r - 1) * 0.7)], "e", a=a * 2 // 3)
        for j in range(5):
            ang = math.radians(j * 72 + 30)
            cv.blob(C0 + math.cos(ang) * r, CY + math.sin(ang) * r * 0.7, 1.4, 1.2, "s", "c", "S", a=a)

    def block(cv, k):
        a = [255, 220, 150, 70][k]
        for deg in range(-70, 71, 4):
            ang = math.radians(deg)
            cv.px(C0 + 3 + math.cos(ang) * (7 + k), CY + math.sin(ang) * (9 + k), "w" if abs(deg) < 30 else "c", a=a)
            cv.px(C0 + 2 + math.cos(ang) * (7 + k), CY + math.sin(ang) * (9 + k), "s", a=a // 2)
        if k < 2:
            cv.spr(SPARK_S, C0 + 9, CY - 2)

    def heal(cv, k):
        a = 255 - k * 35
        cv.ring(C0, CY, 3 + k * 2, "f", 1, a=a)
        for j, (x, y) in enumerate(((C0 - 8, CY + 2), (C0 + 4, CY), (C0 - 2, CY - 5))):
            yy = y - k * 2
            for dx, dy in ((0, 1), (1, 0), (2, 1), (3, 0), (4, 1), (0, 2), (1, 2), (2, 2), (3, 2), (4, 2), (1, 3), (2, 3), (3, 3), (2, 4), (1, 1), (3, 1)):
                cv.px(x + dx, yy + dy, "f", a=a)
            cv.px(x + 1, yy + 1, "w", a=a)

    s.tag("Bite", frames_of(3, bite), 55, repeat=1)
    s.tag("Slash", frames_of(4, slash), 50, repeat=1)
    s.tag("Slam", frames_of(5, slam), 60, repeat=1)
    s.tag("Block", frames_of(4, block), 60, repeat=1)
    s.tag("Heal", frames_of(6, heal), 80, repeat=1)
    return [s]


# ================================================================ run
def contact_sheet(sprites, path, scale=3, cols=3):
    """A compact review sheet: every sprite's packed sheet on a grass-green ground, with labels."""
    from PIL import ImageDraw, ImageFont
    font = ImageFont.load_default()
    tiles = []
    for s in sprites:
        sheet = Image.open(os.path.join(base.ROOT, "Sheets", s.group, s.name + ".png")).convert("RGBA")
        sheet = sheet.resize((sheet.width * scale, sheet.height * scale), Image.NEAREST)
        tile = Image.new("RGBA", (sheet.width + 76, sheet.height + 16), (62, 92, 46, 255))
        d = ImageDraw.Draw(tile)
        d.text((4, 2), f"{s.name}  ({s.caste})", fill=(255, 241, 214, 255), font=font)
        for r, (tname, _, _, _) in enumerate(s.tags):
            d.text((4, 16 + r * s.h * scale + s.h * scale // 2 - 6), tname, fill=(255, 210, 58, 255), font=font)
        tile.alpha_composite(sheet, (72, 16))
        tiles.append(tile)
    cw = max(t.width for t in tiles) + 8
    rows = [tiles[i:i + cols] for i in range(0, len(tiles), cols)]
    heights = [max(t.height for t in row) + 8 for row in rows]
    out = Image.new("RGBA", (cw * cols, sum(heights)), (42, 29, 20, 255))
    y = 0
    for row, hgt in zip(rows, heights):
        for i, t in enumerate(row):
            out.alpha_composite(t, (i * cw + 4, y + 4))
        y += hgt
    out.save(path)


def main():
    groups = [("weapons", build_weapons()), ("tower extras", build_tower_extras()),
              ("castes", build_castes()), ("melee", build_melee_fx())]
    results = []
    for label, sprites in groups:
        for s in sprites:
            results.append((s, export(s)))
            n = sum(len(f) for _, f, _, _ in s.tags)
            print(f"{s.group:8} {s.name:18} {n:3} frames  " + ", ".join(t[0] for t in s.tags))
    contact_sheet(groups[0][1], os.path.join(HERE, "WeaponSheetPreview.png"))
    contact_sheet(groups[1][1] + groups[3][1], os.path.join(HERE, "TowerExtrasPreview.png"))
    preview_sheet([r for r in results if r[0].group == "Towers"], os.path.join(HERE, "CasteSheetPreview.png"))
    total = sum(sum(len(f) for _, f, _, _ in s.tags) for s, _ in results)
    print(f"{len(results)} sprites, {total} frames written and read back OK")


if __name__ == "__main__":
    main()
