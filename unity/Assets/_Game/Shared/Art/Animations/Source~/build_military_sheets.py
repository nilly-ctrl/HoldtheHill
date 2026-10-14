"""Hold the Hill military theme: every caste as a soldier ant with its own weapon or tool.

This theme changes the view. The other themes keep the meadow's top-down ant facing up; here each
ant stands upright in profile, facing right, inside a sandbag emplacement, so that what it carries
reads at 32 px. Canvas, pivot, tags and timing are still those of Tower<Name>, so a sprite swaps
in without moving anything. Towers do not rotate in game, so facing right is only a convention.

  Rifleman (Spitter)        rifle                 Engineer (Worker)          wrench
  Rocketeer (Seeker)        shoulder launcher     Infantry (Soldier)         rifle and bayonet
  Mortar crew (Bombardier)  mortar tube           Sergeant (Major)           machete, peaked cap
  Shotgunner (Slinger)      pump shotgun          Medic (Nurse)              syringe and satchel
  Signaller (Storm Ant)     radio pack, arc lance Foam trooper (Honeypot)    tank and sprayer
  Sniper (Dewdrop Lens)     scoped long rifle     Net gunner (Weaver)        net launcher
  Drone operator (Nest)     controller, 2 drones  Scout                      binoculars and pistol
  Cryo trooper (Frost Ant)  coolant tank, sprayer Commander (Queen's Guard)  megaphone, peaked cap
  Breacher (Kicker)         sledgehammer          Chem trooper (Fungus)      gas mask, tank, sprayer
  Sapper                    shovel and a mine

Tier 2 adds sandbags, a pennant and a stripe on the helmet; tier 3 adds more sandbags, an ammo
crate, a flag and a second stripe.

The enemies are the other army, side-on as well and walking right, in red helmets: beetles carry
a gun on the back like a turret (the stag beetle a cannon, the rhino a shield), the centipede is a
column with packs, the mantis a medic, the wasp and hornet carry a bomb, the spider twin guns, the
snail a howitzer through its shell, the grub dynamite; the titan beetle is a twin-cannon tank with
a banner and the mantis queen a general with a sabre. See "the other army" below. Colours come from the palette, so build_theme_sheets.py sets
the Military palette first and then calls build_towers() here.
"""
import math

import build_anim_sheets as base
from build_anim_sheets import Cv, CX, PLUS, SPARK, SPARK_S, TW, tower_sprite

GROUND = 25          # the row the feet stand on
_tier = 1

TANKS = {"B": ("B", "b", "n"), "u": ("u", "y", "Y"), "g": ("G", "g", "j")}


# ================================================================ the emplacement
def bunker(L, mound=None):
    """Trodden earth, and whatever stands behind the ant. The front wall is laid by front_wall(), over the feet."""
    L["Shadow"].blob(CX, 24.5, 15, 7, (0, 0, 0), a=85)
    b = Cv(TW, TW)
    b.blob(CX, 23.5, 14, 6.5, "D", "d", "e")
    for x, y in ((6, 22), (11, 27), (20, 21), (25, 23)):
        b.px(x, y, "e")
    if _tier == 2:                                                   # a pennant
        b.line([(4, 13), (4, 21)], "S")
        for y, x1 in ((13, 8), (14, 7), (15, 6)):
            for x in range(5, x1 + 1):
                b.px(x, y, "m")
    if _tier == 3:                                                   # a flag and an ammo crate
        b.line([(4, 7), (4, 20)], "S")
        for y in range(7, 11):
            for x in range(5, 10):
                b.px(x, y, "r")
        b.px(7, 8, "y")
        for y in range(19, 23):
            for x in range(6, 11):
                b.px(x, y, "M" if y > 19 else "m")
        b.pxs([(8, 21), (7, 20)], "y")
    b.outline()
    L["Base"].paste(b)


def front_wall(L):
    bags = Cv(TW, TW)
    spots = [(20, 27.0), (24.5, 26.4), (28.5, 25.2)]
    if _tier >= 2:
        spots += [(15.5, 27.4), (26.5, 24.2)]
    if _tier == 3:
        spots += [(11, 27.2), (22.2, 24.9)]
    for x, y in sorted(spots, key=lambda p: p[1]):                   # each bag keeps its own outline
        bag = Cv(TW, TW)
        bag.blob(x, y, 2.5, 1.6, "T", "t", "D")
        bag.outline()
        bags.paste(bag)
    L["Body"].paste(bags)


# ================================================================ the soldier
def trooper(L, kit, bob=0, dx=0, head_dy=0, tw=0):
    """An upright ant in profile, facing right. Returns the thorax column and row offset, and the head centre."""
    ink, body = Cv(TW, TW), Cv(TW, TW)
    x0, y0 = 15 + dx, bob
    ink.line([(x0 - 2, 19 + y0), (x0 - 4, 22), (x0 - 5, GROUND)], "k")
    ink.line([(x0 + 1, 19 + y0), (x0 + 2, 22), (x0 + 3, GROUND)], "k")
    ink.line([(x0 - 1, 19 + y0), (x0 - 1, GROUND)], "k")
    hx, hy = x0 + 2, 10 + y0 + head_dy
    ink.line([(hx - 1, hy - 2), (hx - 3, hy - 5 - tw), (hx - 6, hy - 5)], "k")      # antennae, swept back
    ink.line([(hx, hy - 2), (hx - 1, hy - 6), (hx - 3, hy - 7 + tw)], "k")
    ink.line([(hx + 3, hy + 2), (hx + 4, hy + 3)], "k")                             # mandible
    coat = kit.get("coat", ("a", "A", "z"))
    body.blob(x0 - 4.5, 19.5 + y0, 3.6, 2.8, *coat)                                 # gaster, carried low behind
    body.blob(x0, 16.5 + y0, 2.2, 3.2, *coat)                                       # thorax, upright
    body.blob(hx, hy + 0.8, 3.2, 2.9, *coat)                                        # head
    if kit.get("tank"):
        body.blob(x0 - 3.5, 14.5 + y0, 1.9, 2.7, *TANKS[kit["tank"]])
    if kit.get("pack"):
        for y in range(13, 17):
            for x in (x0 - 4, x0 - 3):
                body.px(x, y + y0, "x" if y > 13 else "S")
    hat = kit.get("hat", "helmet")
    if hat == "cap":                                                 # peaked cap
        body.blob(hx - 0.3, hy - 1.2, 3.5, 1.7, "M", "m", "j")
        body.pxs([(hx + 3, hy), (hx + 4, hy)], "x")
    else:
        dome = ("c", "w", "s") if hat == "medic" else ("M", "m", "j")
        body.blob(hx - 0.3, hy - 1.0, 3.9, 2.1, *dome)
        for x in range(hx - 4, hx + 4):                              # brim
            body.px(x, hy, dome[2])
    body.outline()
    body.px(hx + 1, hy + 1, "k")                                     # eye
    for y in range(15, 19):                                          # field vest over the thorax
        for x in range(x0 - 2, x0 + 2):
            if body.p[x, y + y0][3] and body.p[x, y + y0][:3] != base.rgba("k")[:3]:
                body.px(x, y + y0, "m" if x == x0 - 2 else "M")
    body.px(x0, 16 + y0, "j")
    if hat == "cap":
        for x in range(hx - 3, hx + 3):
            body.px(x, hy - 1, "r")
        body.px(hx, hy - 2, "y")
    elif hat == "medic":
        body.pxs([(hx - 1, hy - 3), (hx - 2, hy - 2), (hx - 1, hy - 2), (hx, hy - 2), (hx - 1, hy - 1)], "r")
    elif _tier >= 2:                                                 # rank on the helmet
        body.px(hx - 1, hy - 2, "y")
        if _tier == 3:
            body.px(hx - 1, hy - 3, "y")
    if kit.get("mask"):
        body.pxs([(hx + 2, hy + 2), (hx + 3, hy + 2), (hx + 2, hy + 1)], "x")
        body.px(hx + 3, hy + 3, "S")
    if kit.get("binoc"):
        body.pxs([(hx + 2, hy), (hx + 3, hy), (hx + 2, hy + 1), (hx + 3, hy + 1)], "x")
        body.px(hx + 4, hy + 1, "b")
    ink.paste(body)
    L["Body"].paste(ink)
    return x0, y0, hx, hy


# ================================================================ what they carry
def gun(w, ax, ay, length=11, stock=True, mag=True, scope=False, bayonet=False, pump=False):
    if stock:
        w.pxs([(ax - 3, ay + 1), (ax - 2, ay + 1), (ax - 2, ay), (ax - 1, ay)], "d")
    for x in range(ax, ax + length):
        w.px(x, ay, "S" if x >= ax + length - 4 else "x")
    if mag:
        w.pxs([(ax + 3, ay + 1), (ax + 3, ay + 2)], "x")
    if pump:
        w.pxs([(ax + 4, ay + 1), (ax + 5, ay + 1), (ax + 6, ay + 1)], "d")
    if scope:
        w.pxs([(ax + 2, ay - 1), (ax + 3, ay - 1), (ax + 4, ay - 1)], "S")
        w.px(ax + 5, ay - 1, "b")
    if bayonet:
        w.pxs([(ax + length, ay), (ax + length + 1, ay), (ax + length + 2, ay)], "c")
    return dict(hands=[(ax, ay + 1), (ax + 5, ay + 1)], muzzle=(ax + length, ay))


def tube(w, ax, ay, length=13, back=5, flare=False, loaded=False):
    x1 = ax - back + length
    for x in range(ax - back, x1):
        w.px(x, ay - 3, "m")
        w.px(x, ay - 2, "M")
    w.px(ax + 1, ay - 4, "x")                                        # sight
    if flare:
        w.pxs([(x1, ay - 4), (x1, ay - 3), (x1, ay - 2), (x1, ay - 1)], "S")
    if loaded:
        w.pxs([(x1, ay - 3), (x1, ay - 2)], "r")
    return dict(hands=[(ax, ay - 1), (ax + 4, ay - 1)], muzzle=(x1 + 1, ay - 2), rear=(ax - back - 1, ay - 2))


def pole(w, ax, ay, deg, length, head):
    """A handle held at an angle (0 points right, 90 up) with something on the end."""
    c, s = math.cos(math.radians(deg)), -math.sin(math.radians(deg))
    tip = (ax + c * length, ay + s * length)
    w.line([(ax - c * 2, ay - s * 2), tip], "s" if head == "blade" else "d")
    if head == "blade":
        w.line([(ax - c * 2, ay - s * 2), (ax, ay)], "d")
        w.line([(ax + c * 3 - s, ay + s * 3 + c), (tip[0] - s, tip[1] + c)], "c")
    elif head == "hammer":
        w.blob(tip[0] + 0.5, tip[1] + 0.5, 2.3, 1.9, "S", "s", "x")
    elif head == "shovel":
        for j in range(3):
            w.line([(tip[0] + c * j - s, tip[1] + s * j + c), (tip[0] + c * j + s, tip[1] + s * j - c)], "s")
    elif head == "wrench":
        w.pxs([(tip[0] - s, tip[1] + c), (tip[0] + s, tip[1] - c), (tip[0] + c - s, tip[1] + s + c), (tip[0] + c + s, tip[1] + s - c)], "s")
    elif head == "lance":
        w.line([(ax + c * (length - 3), ay + s * (length - 3)), tip], "S")
        w.pxs([(tip[0] - s, tip[1] + c), (tip[0] + s, tip[1] - c)], "y")
    return dict(hands=[(ax, ay), (ax + c * 3, ay + s * 3)], tip=tip)


def flash(fx, x, y, big=False):
    fx.spr(SPARK if big else SPARK_S, x - (2 if big else 1), y - (2 if big else 1))


def smoke(fx, x, y, r, a):
    fx.blob(x, y, r, r, "s", "c", "S", a=a)


# ---- one function per kit: draws the weapon on w, its effect on fx; k is the attack frame, or None while idle
def w_rifle(w, fx, c, k, spec, idle):
    g = gun(w, c["ax"], c["ay"], length=spec.get("length", 11), scope=spec.get("scope", False), pump=spec.get("pump", False),
            mag=not spec.get("pump", False))
    mx, my = g["muzzle"]
    if spec.get("pump"):                                             # one loud spread of shot
        if k == 1:
            flash(fx, mx + 1, my, big=True)
            for dy in (-4, 0, 4):
                fx.line([(mx + 3, my + dy * 0.3), (31, my + dy)], "y", a=220)
        if k in (2, 3):
            smoke(fx, mx + 2 + k, my - 1, 1.6, 200 - k * 40)
    else:                                                            # two rounds
        if k in (1, 3):
            flash(fx, mx + 1, my)
            fx.line([(mx + 3, my), (31, my)], "y")
            fx.line([(mx + 3, my), (mx + 6, my)], "w")
        if k in (2, 4):
            fx.px(mx + 1, my - 1, "s", 180)
            fx.px(c["ax"] + 2, c["ay"] - 2 - (k // 2), "y")          # a spent case
    return g


def w_sniper(w, fx, c, k, spec, idle):
    g = gun(w, c["ax"], c["ay"], length=13, scope=True, mag=False)
    w.line([(c["ax"] + 9, c["ay"] + 1), (c["ax"] + 10, c["ay"] + 3)], "x")          # bipod
    mx, my = g["muzzle"]
    if k == 0 or (k is None and idle == 2):
        fx.px(c["ax"] + 5, c["ay"] - 2, "w")                         # a glint off the scope
    if k == 1:
        fx.disc(mx + 1, my + 0.5, 1.5, "b")
    if k in (2, 3):
        fx.line([(mx, my), (31, my)], "w")
        fx.line([(mx, my - 1), (31, my - 1)], "b", a=150)
        fx.line([(mx, my + 1), (31, my + 1)], "b", a=150)
        flash(fx, mx + 1, my, big=k == 2)
    if k == 4:
        fx.line([(mx, my), (31, my)], "b", a=90)
    return g


def w_launcher(w, fx, c, k, spec, idle):
    g = tube(w, c["ax"], c["ay"], loaded=k in (None, 0, 5))
    mx, my = g["muzzle"]
    rx, ry = g["rear"]
    if k == 1:
        flash(fx, mx, my, big=True)
        smoke(fx, rx - 1, ry, 2.2, 220)
    if k in (1, 2, 3):
        x = (mx + 2, mx + 7, 30)[k - 1]
        y = my - (0, 1, 3)[k - 1]
        fx.line([(x - 3, y), (x, y)], "S")
        fx.px(x + 1, y, "r")
        for j in range(1, k + 1):
            smoke(fx, x - 3 - j * 3, y + j * 0.4, 1.4, 230 - j * 45)
        fx.px(x - 4, y, "y")
    if k in (2, 3, 4):
        smoke(fx, rx - k, ry - 1, 2.6 - k * 0.3, 200 - k * 40)
    return g


def w_netgun(w, fx, c, k, spec, idle):
    g = tube(w, c["ax"], c["ay"], length=10, back=3, flare=True)
    mx, my = g["muzzle"]
    if k == 1:
        flash(fx, mx + 1, my)
    if k == 2:
        fx.disc(mx + 3, my + 0.5, 1.6, "c")
    if k in (3, 4):
        r = 3 + (k - 3) * 1.5
        cx = min(mx + 6, 31 - r)
        fx.ring(cx, my + 0.5, r, "c", 1, a=255 - (k - 3) * 90)
        fx.line([(cx - r, my), (cx + r - 1, my)], "c", a=220 - (k - 3) * 90)
        fx.line([(cx - 0.5, my - r + 1), (cx - 0.5, my + r - 1)], "c", a=220 - (k - 3) * 90)
    return g


def w_mortar(w, fx, c, k, spec, idle):
    w.line([(21, GROUND), (26, GROUND)], "x")                        # baseplate
    w.line([(22, 24), (26, 16)], "s")
    w.line([(23, 24), (27, 16)], "S")
    w.line([(25, 19), (28, GROUND)], "x")                            # bipod
    held = {None: (c["ax"] + 2, c["ay"]), 0: (23, 13), 1: (26, 14)}.get(k)
    if held:
        w.pxs([(held[0], held[1]), (held[0], held[1] + 1)], "S")
        w.px(held[0], held[1] - 1, "r")
    hand = held or (c["ax"] + 3, c["ay"] + 1)
    if k == 2:
        flash(fx, 27, 14, big=True)
        fx.pxs([(29, 9), (29, 10)], "S")
        fx.px(29, 8, "r")
    if k == 3:
        fx.pxs([(30, 3), (30, 4)], "S")
        smoke(fx, 27, 13, 2.2, 220)
        fx.line([(29, 6), (28, 10)], "s", a=120)
    if k == 4:
        smoke(fx, 27.5, 11, 2.6, 130)
    return dict(hands=[hand, (hand[0] - 1, hand[1] + 1)])


def w_lance(w, fx, c, k, spec, idle):
    w.line([(c["x0"] - 4, 12 + c["y0"]), (c["x0"] - 7, 3)], "S")     # the radio's whip aerial
    g = pole(w, c["ax"], c["ay"], 50, 9, "lance")
    tx, ty = g["tip"]
    if k is None and idle == 2:
        fx.spr(SPARK_S, tx, ty - 3)
    if k in (1, 2, 3):
        bolts = ([(tx, ty), (tx + 3, ty - 3), (tx + 5, ty), (31, ty - 4)],
                 [(tx, ty), (tx + 2, ty + 2), (tx + 5, ty - 2), (tx + 7, ty + 3), (31, ty + 1)],
                 [(tx, ty), (tx + 4, ty - 5), (tx + 6, ty - 2), (31, ty - 8)])
        fx.line(bolts[k - 1], "y")
        fx.line(bolts[k - 1][:2], "w")
        fx.line([(c["x0"] - 7, 3), (c["x0"] - 5 + k, 1)], "y")
        flash(fx, tx, ty, big=k == 1)
    return g


def w_controller(w, fx, c, k, spec, idle):
    ax, ay = c["ax"], c["ay"]
    for y in (ay - 1, ay, ay + 1):
        for x in range(ax + 1, ax + 5):
            w.px(x, y, "x")
    w.line([(ax + 4, ay - 2), (ax + 5, ay - 5)], "S")
    w.px(ax + 2, ay, "g" if (idle % 2 or k) else "G")
    for j in range(2):                                               # two drones circling the post
        ang = math.radians((k * 30 if k is not None else idle * 60) + j * 180)
        reach = 1.0 if k is None else (1.0, 1.15, 1.3, 1.3, 1.15, 1.0)[k]
        x, y = CX + math.cos(ang) * 12 * reach, 12 + math.sin(ang) * 6 * reach
        d = Cv(TW, TW)
        d.pxs([(x - 1, y), (x, y), (x - 2, y - 1), (x + 1, y - 1)], "x")
        d.pxs([(x - 3, y - 1), (x + 2, y - 1)], "s")
        d.outline()
        fx.paste(d)
        fx.px(x, y, "r" if (idle + j) % 2 else "f")
        if k in (2, 3):
            fx.line([(x + 2, y + 1), (x + 5, y + 3)], "y")
    return dict(hands=[(ax + 1, ay + 1), (ax + 4, ay + 1)])


def w_sprayer(w, fx, c, k, spec, idle):
    ax, ay = c["ax"], c["ay"]
    w.line([(c["x0"] - 3, ay + 1), (ax - 2, ay + 3), (ax, ay + 1)], "x")           # hose
    g = gun(w, ax, ay, length=7, stock=False, mag=False)
    w.px(ax + 7, ay - 1, "S")
    mx, my = g["muzzle"]
    lite, core, dark = spec["spray"]
    if k is None and idle == 1:
        fx.px(mx + 1, my + 1, lite)                                  # a drip
    if k in (1, 2, 3, 4):
        for j in range(k * 3 + 2):
            t = (j + 1) / (k * 3 + 2)
            d = t * (4 + k * 3.5)
            y = my + math.sin(j * 2.4 + k) * d * 0.35
            r = 0.8 + t * (1.2 if k < 4 else 0.6)
            fx.blob(min(30.5, mx + 1 + d), y, r, r, lite, core, dark, a=255 - (k == 4) * 110)
    return g


def swing(w, fx, c, k, spec, idle, head, length, poses, arc=True):
    deg = poses[0] if k is None else poses[1][k]
    g = pole(w, c["ax"], c["ay"], deg, length, head)
    if arc and k in (2, 3):
        fx.ring(c["ax"], c["ay"], length + 2, "w" if k == 2 else "c", 1, a=255 - (k - 2) * 110,
                gaps=lambda a: not (a < 25 or a > 290))
    if arc and k == 3:
        flash(fx, g["tip"][0] + 1, g["tip"][1])
    return g


def w_wrench(w, fx, c, k, spec, idle):
    return swing(w, fx, c, k, spec, idle, "wrench", 6, (60, (110, 130, 10, -20, 30, 60)))


def w_machete(w, fx, c, k, spec, idle):
    return swing(w, fx, c, k, spec, idle, "blade", 9, (65, (110, 135, 5, -25, 30, 65)))


def w_hammer(w, fx, c, k, spec, idle):
    g = swing(w, fx, c, k, spec, idle, "hammer", 9, (70, (100, 125, -35, -35, 30, 70)), arc=False)
    if k in (2, 3):
        r = 4 + (k - 2) * 3
        fx.ring(25, GROUND + 0.5, r, "c", 1, a=255 - (k - 2) * 100, ry=r * 0.45)
        for j in range(5):
            fx.px(25 + math.cos(j * 1.3) * (r + 1), GROUND - 1 - (j % 3) - (k - 2), "d")
        fx.line([(23, GROUND + 1), (20, GROUND + 3)], "e")
        fx.line([(27, GROUND + 1), (30, GROUND + 2)], "e")
    return g


def w_shovel(w, fx, c, k, spec, idle):
    g = swing(w, fx, c, k, spec, idle, "shovel", 8, (75, (40, -50, -50, 20, 60, 75)), arc=False)
    if k in (1, 2):
        for j in range(4):
            fx.px(24 + j * 1.5, GROUND - 2 - j % 2 - (k - 1) * 2, "d")
    if k in (3, 4):
        fx.disc(26, GROUND + 0.5, 1.6, "x")
        fx.px(25, GROUND, "f" if k == 4 else "r")
    return g


def w_bayonet(w, fx, c, k, spec, idle):
    g = gun(w, c["ax"], c["ay"], length=9, bayonet=True)
    mx, my = g["muzzle"]
    if k in (2, 3):
        fx.line([(mx + 3, my), (mx + 6, my)], "w", a=255 - (k - 2) * 120)
        fx.spr(SPARK_S, mx + 3, my - 3 + (k - 2) * 4)
    return g


def w_syringe(w, fx, c, k, spec, idle):
    ax, ay = c["ax"], c["ay"]
    for y in range(18, 21):                                          # satchel on the hip
        for x in range(c["x0"] - 2, c["x0"] + 1):
            w.px(x, y + c["y0"], "c")
    w.px(c["x0"] - 1, 19 + c["y0"], "r")
    w.line([(ax, ay), (ax + 5, ay)], "c")
    w.px(ax - 1, ay, "s")
    w.px(ax + 6, ay, "b")
    if k is None and idle == 1:
        fx.px(ax + 7, ay - 1, "b")
    if k in (1, 2, 3, 4):
        fx.ring(CX, 20, 5 + k * 2.4, "g", 1, a=230 - k * 40, ry=3 + k * 1.3)
        for j, (x, y) in enumerate(((22, 12), (9, 14), (26, 20))):
            if (k + j) % 3 != 2:
                fx.spr(PLUS, x - 1, y - 1 - k)
    return dict(hands=[(ax, ay + 1), (ax + 2, ay + 1)])


def w_pistol(w, fx, c, k, spec, idle):
    g = gun(w, c["ax"] + 2, c["ay"], length=5, stock=False, mag=False)
    w.px(c["ax"] + 2, c["ay"] + 1, "d")
    mx, my = g["muzzle"]
    if k in (1, 3):
        flash(fx, mx + 1, my)
        fx.line([(mx + 3, my), (31, my)], "g")
        fx.line([(mx + 3, my), (mx + 5, my)], "w")
    g["hands"] = [(c["ax"] + 2, c["ay"] + 1), (c["hx"] + 2, c["hy"] + 2)]         # the other hand holds the binoculars up
    return g


def w_megaphone(w, fx, c, k, spec, idle):
    ax, ay = c["ax"], c["ay"] - 2
    for j in range(7):
        half = int(round(j * 0.5))
        for y in range(ay - half, ay + half + 1):
            w.px(ax + 1 + j, y, "y" if j == 6 else ("s" if y <= ay else "S"))
    w.px(ax, ay, "x")
    if k in (1, 2, 3, 4):
        for n in range(k if k < 4 else 2):
            r = 3 + (k - n) * 3 if k < 4 else 12 - n * 3
            fx.ring(ax + 8, ay + 0.5, r, "y" if n % 2 == 0 else "c", 1, a=250 - k * 35, gaps=lambda a: not (a < 48 or a > 312))
    return dict(hands=[(ax + 1, ay + 1), (ax + 3, ay + 2)])


# ================================================================ the castes
Q = ("q", "Q", "Z")
SPECS = {
    "TowerLinear": dict(role="Rifleman", weapon=w_rifle, recoil=True),
    "TowerHoming": dict(role="Rocketeer", weapon=w_launcher, recoil=True),
    "TowerMortar": dict(role="Mortar crew", weapon=w_mortar, crouch=True),
    "TowerRicochet": dict(role="Shotgunner", weapon=w_rifle, pump=True, length=9, recoil=True),
    "TowerChain": dict(role="Signaller", weapon=w_lance, pack=True),
    "TowerBeam": dict(role="Sniper", weapon=w_sniper, recoil=True),
    "TowerOrbit": dict(role="Drone operator", weapon=w_controller),
    "TowerFrostAura": dict(role="Cryo trooper", weapon=w_sprayer, tank="B", spray=("b", "w", "B"), coat=("N", "n2", "Nz")),
    "TowerKnockback": dict(role="Breacher", weapon=w_hammer, strike=True, coat=Q),
    "TowerMineLayer": dict(role="Sapper", weapon=w_shovel, strike=True),
    "TowerWorker": dict(role="Engineer", weapon=w_wrench, strike=True),
    "TowerSoldier": dict(role="Infantry", weapon=w_bayonet, lunge=True, coat=Q),
    "TowerMajor": dict(role="Sergeant", weapon=w_machete, strike=True, hat="cap"),
    "TowerNurse": dict(role="Medic", weapon=w_syringe, hat="medic"),
    "TowerHoneypot": dict(role="Foam trooper", weapon=w_sprayer, tank="u", spray=("u", "y", "Y")),
    "TowerWeaver": dict(role="Net gunner", weapon=w_netgun, recoil=True),
    "TowerScout": dict(role="Scout", weapon=w_pistol, binoc=True),
    "TowerQueensGuard": dict(role="Commander", weapon=w_megaphone, hat="cap", shout=True),
    "TowerFungusFarmer": dict(role="Chem trooper", weapon=w_sprayer, tank="g", spray=("g", "w", "G"), mask=True),
}


def make_draw(spec):
    def draw(L, tag, i, n):
        k = i if tag == "Attack" else None
        bob, dx, head_dy, tw = (0, 0, 1, 1, 0, 0)[i % 6], 0, 0, (0, 1, 1, 0, 0, 1)[i % 6]
        if k is not None:
            bob = tw = 0
            if spec.get("recoil"):
                dx = -(0, 1, 1, 0, 0, 0)[k]
            if spec.get("lunge"):
                dx = (0, -1, 2, 3, 1, 0)[k]
            if spec.get("strike"):
                bob = (0, -1, 1, 1, 0, 0)[k]
            if spec.get("crouch"):
                bob = (0, 0, 1, 1, 0, 0)[k]
            if spec.get("shout"):
                head_dy = (0, -1, -1, -1, 0, 0)[k]
        x0, y0, hx, hy = trooper(L, spec, bob=bob, dx=dx, head_dy=head_dy, tw=tw)
        c = dict(x0=x0, y0=y0, hx=hx, hy=hy, ax=x0 + 2, ay=15 + y0)
        w, fx = Cv(TW, TW), Cv(TW, TW)
        got = spec["weapon"](w, fx, c, k, spec, i)
        w.outline()
        L["Body"].paste(w)
        for x, y in got["hands"]:                                    # arms, over whatever they hold
            L["Body"].line([(x0 + 1, 15 + y0), (x, y)], "k")
            L["Body"].px(x, y, "A")
        front_wall(L)
        L["FX"].paste(fx)
    return draw


def build_towers(suffix, towers):
    """towers: (name, caste, draw, keyword arguments) as build_tier_sheets lists them. -> {name: [tier 1, 2, 3]}"""
    global _tier
    plain = base.tower_base
    base.tower_base = bunker
    out = {}
    try:
        for name, caste, _, kw in towers:
            spec = SPECS[name]
            row = []
            for _tier in (1, 2, 3):
                tag = f"T{_tier}" if _tier > 1 else ""
                row.append(tower_sprite(name + tag + suffix, f"{spec['role']} ({caste}), tier {_tier}, military theme",
                                        make_draw(spec), **kw))
            out[name] = row
    finally:
        base.tower_base = plain
        _tier = 1
    return out


# ================================================================ the other army, side-on
# These stand in for the top-down drawing functions the enemy builders call (beetle, centipede,
# mantis, and each roster creature's own body), with the same arguments, so every tag an enemy has
# (Walk, Hurt, Death, Attack, Spawn and its own extras) is still built by the same code.
# cy is the builders' body centre; side-on, the feet stand on cy + 3, where the flattened shadow lies.
RED = ("R", "r", "Z")


def painted(cv, x, y):
    x, y = int(math.floor(x)), int(math.floor(y))
    return 0 <= x < cv.w and 0 <= y < cv.h and cv.p[x, y][3] > 0 and cv.p[x, y][:3] != base.rgba("k")[:3]


def legs(ink, xs, top, ground, phase, c="k"):
    for j, x in enumerate(xs):
        s = math.sin(phase + j * 2.094)
        lift = 1 if math.cos(phase + j * 2.094) > 0.3 else 0
        ink.line([(x, top), (x + (1 if s > 0 else -1), (top + ground) / 2), (x + round(s * 1.6), ground - lift)], c)


def helmet(cv, hx, hy, hr, col=RED):
    cv.blob(hx - 0.2, hy - hr * 0.5, hr + 0.5, hr * 0.7, *col)


def barrel(cv, x, y, length, thick=1):
    for j in range(thick):
        for i in range(int(length)):
            cv.px(x + i, y + j, "S" if (i >= length - 3 or j == 0 and thick > 1) else "x")
    return x + length, y + (thick - 1) / 2


def flag(cv, x, top, bottom, big=False):
    cv.line([(x, top), (x, bottom)], "S")
    for j in range(4 if big else 3):
        for i in range(1, (7 if big else 5) - j % 2):
            cv.px(x + i, top + j, "r")
    cv.px(x + 2, top + 1, "y")


def side_bug(w, h, cx, cy, L, H, coat, phase=0.0, weapon="rifle", strike=None, tilt=0.0, plates=0, cracked=0,
             banner=False, horn=0.0):
    """A beetle in profile, walking right, with a weapon mounted on its back like a turret."""
    ink, body, gear = Cv(w, h), Cv(w, h), Cv(w, h)
    ground = cy + 3
    leg = max(2.0, H * 0.5)
    by = ground - leg - H * 0.9
    legs(ink, [cx - L * 0.6 + j * L * 0.6 for j in range(3)], by + H * 0.5, ground, phase)
    hx, hy, hr = cx + L * 0.85, by + H * 0.3 - tilt, max(1.7, H * 0.55)
    ink.line([(hx + hr * 0.4, hy - hr), (hx + hr + 1.5, hy - hr - 2)], "k")
    ink.line([(hx + hr, hy + hr * 0.3), (hx + hr + 1.5, hy + hr)], "k")
    body.blob(cx - L * 0.15, by, L * 0.85, H, *coat)
    body.blob(hx, hy, hr, hr, *coat)
    helmet(body, hx, hy, hr)
    body.outline()
    body.px(hx + hr * 0.45, hy + hr * 0.4, "k")
    for j in range(int(L * 1.2)):                                    # camouflage
        if j % 3 == 0:
            x, y = cx - L * 0.8 + j * 1.25, by - H * 0.35 + (j % 2) * H * 0.6
            if painted(body, x, y):
                body.pxs([(x, y), (x + 1, y)], coat[2])
    for j in range(plates):                                          # armour plates
        x = cx - L * 0.65 + j * L * 0.42
        body.line([(x, by - H * 0.8 + j), (x, by + H * 0.6)], coat[2])
    for n in range(cracked):                                         # a plate knocked off
        body.blob(cx - L * 0.45 + n * L * 0.5, by - H * 0.25, L * 0.24, H * 0.42, "f", "c", "R")
    top = by - H
    muzzle = None
    if weapon == "smg":
        gear.pxs([(cx - 1, top), (cx, top)], "x")
        muzzle = barrel(gear, cx - 1, top - 1, L * 0.9)
    elif weapon == "rifle":
        gear.pxs([(cx - 2, top), (cx - 1, top)], "d")
        muzzle = barrel(gear, cx - 2, top - 1, L * 1.3)
    elif weapon == "cannon":
        gear.blob(cx - L * 0.1, top + 0.5, L * 0.32, max(1.5, H * 0.32), "S", "s", "x")
        muzzle = barrel(gear, cx, top - 1, L * 1.15, thick=2)
    elif weapon == "twin":
        gear.blob(cx - L * 0.15, top + 1, L * 0.3, H * 0.3, "S", "s", "x")
        barrel(gear, cx - 2, top - 3 - tilt, L * 1.2, thick=2)
        muzzle = barrel(gear, cx, top + 1 - tilt, L * 1.1, thick=2)
        muzzle = (muzzle[0], top - 1 - tilt)
    elif weapon == "shield":
        for y in range(int(hy - hr - 2), int(hy + hr + 2)):
            gear.pxs([(hx + hr + 2, y), (hx + hr + 3, y)], "S")
            gear.px(hx + hr + 2, y, "s")
        gear.pxs([(cx - 1, top), (cx, top)], "x")
        barrel(gear, cx - 1, top - 1, L * 0.8)
    if horn:
        for j in range(2):
            gear.line([(hx + hr, hy - 1 + j), (hx + hr + horn, hy - 4 - tilt + j)], "y")
    if banner:
        flag(gear, cx - L * 0.7, max(1, top - H * 0.9), top + 1, big=True)
    gear.outline()
    ink.paste(body)
    ink.paste(gear)
    if muzzle and strike in (2, 3):
        flash(ink, muzzle[0] + 1, muzzle[1], big=L > 8)
    return ink


def side_beetle(w, h, cx, cy, Lh, Wh, shell, kind="plain", phase=0.0, mand=0, bob=0):
    weapon = {"scarab": "smg", "plain": "rifle", "stag": "cannon", "horn": "shield"}.get(kind, "rifle")
    return side_bug(w, h, cx + bob - 1, cy, Lh * 0.9, Wh * 0.62, shell, phase, weapon, strike=3 if mand >= 2 else None)


def side_centipede(w, h, cx, cy, segs, spacing, phase, colors=("L", "l", "Lz"), seg_r=(2.0, 2.6),
                   amp=1.0, offsets=None, glow=None):
    """A column on the march: segments humping along, a pack on every other one, the head in a helmet with a gun."""
    ink, body, gear = Cv(w, h), Cv(w, h), Cv(w, h)
    c0, hi, lo = colors
    ry = seg_r[1] - 0.4
    ground = cy + 3
    pts = []
    for k in range(segs):
        x = cx + (segs - 1) * spacing / 2 - k * spacing
        y = ground - 2 - ry - abs(math.sin(phase - k * 0.9)) * amp
        if offsets:
            x, y = x + offsets[k][0], y + offsets[k][1]
        pts.append((x, y))
    for k, (x, y) in enumerate(pts):
        swing = round(math.sin(phase - k * 1.4) * 1.2)
        ink.line([(x - 0.5, y + ry - 0.5), (x - 0.5 + swing, ground if not offsets else y + ry + 2)], "k")
    hx, hy = pts[0]
    ink.line([(hx + 1, hy - ry), (hx + 3, hy - ry - 2)], "k")
    tx, ty = pts[-1]
    ink.line([(tx - 1, ty), (tx - 3, ty - 1)], "k")
    for k, (x, y) in reversed(list(enumerate(pts))):
        body.blob(x, y, seg_r[0] + 0.3, ry, glow if glow and k in (1, 3, 5) else c0, hi, lo)
        if k and k % 2 == 0 and seg_r[0] >= 2:
            gear.pxs([(x - 1, y - ry - 1), (x, y - ry - 1), (x - 1, y - ry), (x, y - ry)], "M")
    helmet(body, hx, hy, seg_r[0] + 0.3)
    body.outline()
    body.px(hx + 1, hy + 0.6, "k")
    if seg_r[0] >= 2:
        barrel(gear, hx - 1, hy - ry - 2, seg_r[0] * 2 + 3)
    else:
        gear.pxs([(hx + seg_r[0] + 1, hy), (hx + seg_r[0] + 2, hy)], "c")            # a hatchling's bayonet
    gear.outline()
    ink.paste(body)
    ink.paste(gear)
    return ink


def side_mantis(w, h, cx, cy, phase, raise_arms=0.0, glow=0.0, s=1.0, hat="medic", sabre=None, banner=False, lay=0.0):
    """An upright mantis in profile. The healer is a medic; at twice the size, with a cap, a sabre and a banner, the queen."""
    ink, body, gear = Cv(w, h), Cv(w, h), Cv(w, h)
    ground = cy + 3
    ax, ay = cx - 5 * s + lay * 2, ground - 4.5 * s                  # abdomen
    legs(ink, [ax - 2 * s, ax + 3 * s], ay + 1.5 * s, ground, phase, "G")
    hx, hy = cx + 6 * s, ay - 8.5 * s                                # head
    if banner:
        flag(gear, ax - 2 * s, hy - 6, ay, big=True)
    body.blob(ax, ay, 6.2 * s - lay * 2, 2.5 * s + lay * 1.5, "M", "g", "j")
    body.blob(ax - 0.5 * s, ay - 0.8 * s, 5 * s, 1.5 * s, "m", "g", "M")             # folded wings
    for t in (0.0, 0.33, 0.66, 1.0):                                 # the long thorax, rising to the head
        body.blob(ax + 5 * s + t * (hx - ax - 6 * s), ay - 0.5 * s + t * (hy + 2 * s - ay), 1.5 * s, 1.5 * s, "M", "m", "j")
    body.blob(hx, hy, 2.1 * s, 1.9 * s, "m", "g", "M")
    if hat == "medic":
        helmet(body, hx, hy, 2.1 * s, ("c", "w", "s"))
        for y in range(3):                                           # the medic's satchel
            for x in range(4):
                gear.px(ax - 1 + x, ay - 3.4 * s - y, "c")
    else:
        body.blob(hx - 0.3, hy - 1.6 * s, 2.6 * s, 1.1 * s, "M", "m", "j")           # peaked cap
        body.pxs([(hx + 2.2 * s, hy - 1.0 * s), (hx + 3 * s, hy - 1.0 * s)], "x")
    body.outline()
    eye = base.mix("y", "w", glow)
    body.px(hx + 1.2 * s, hy + 0.4 * s, eye)
    if hat == "medic":
        body.px(hx - 0.5, hy - 1.6 * s, "r")
        gear.outline()
        gear.px(ax, ay - 3.4 * s - 1, "r")
    else:
        for x in range(int(hx - 2 * s), int(hx + 2 * s)):
            body.px(x, hy - 1.0 * s, "r")
        body.px(hx, hy - 2.0 * s, "y")
        gear.outline()
    sx, sy = hx - 1.5 * s, hy + 3.5 * s                              # shoulder
    r = raise_arms
    elbow = (sx + 4 * s, sy + 1 * s - r * 3 * s)
    tip = (sx + 7 * s, sy - 1.5 * s - r * 4.5 * s)
    for j in range(2 if s > 1.5 else 1):
        ink.line([(sx, sy + j), (elbow[0], elbow[1] + j), (tip[0], tip[1] + j)], "g")
    ink.paste(body)
    ink.paste(gear)
    if sabre is not None:                                            # held high, or brought down on a strike
        blade = Cv(w, h)
        end = (tip[0] + 9, tip[1] + 4) if sabre == "down" else (tip[0] + 3, tip[1] - 11 - r * 2)
        blade.line([tip, end], "s")
        blade.line([(tip[0] + 1, tip[1]), (end[0] + 1, end[1])], "c")
        blade.pxs([(tip[0] - 1, tip[1]), (tip[0], tip[1] + 1)], "y")
        blade.outline()
        ink.paste(blade)
    return ink


def side_flyer(w, h, cx, cy, phase, s=1.0, curl=0.0, strike=0, beats=6, big=False):
    """A wasp in profile with a bomb slung under it; curl swings the bomb down and away."""
    ink, body, gear = Cv(w, h), Cv(w, h), Cv(w, h)
    beat = int(round(phase / (2 * math.pi) * beats)) % 2
    wing = Cv(w, h)
    for step in range(int(8 * s)):                                   # one wing up, or swept back
        t = step / (8 * s)
        a = math.radians(250 if beat else 205)
        half = 0.6 * s + math.sin(t * math.pi) * 1.8 * s
        wing.blob(cx + 1.5 * s + math.cos(a) * step, cy - 2 * s + math.sin(a) * step, half, half,
                  "u" if big else "b", "c" if big else "w", "o" if big else "B", a=150 if beat else 195)
    ink.paste(wing)
    for ax in (3, 1, -1):
        ink.line([(cx + ax * s, cy + 2 * s), (cx + (ax - 1) * s, cy + 4 * s)], "k")
    ink.line([(cx - 9 * s, cy + 0.5), (cx - 11.5 * s, cy + 1.5 * s)], "k")          # the sting
    coat = ("o", "u", "R") if big else ("y", "c", "Y")
    body.blob(cx - 5 * s, cy + 0.6 * s, 5.2 * s, 2.9 * s, *coat)
    body.blob(cx + 1.5 * s, cy, 2.5 * s, 2.5 * s, "x", "S", "k")
    hx, hy, hr = cx + 5.8 * s, cy + 0.5 * s, 2.2 * s
    body.blob(hx, hy, hr, hr, *coat)
    helmet(body, hx, hy, hr)
    body.outline()
    for bx in (-8, -5.5, -3):
        body.line([(cx + bx * s, cy - 2 * s), (cx + bx * s, cy + 2.8 * s)], "k")
        if big:
            body.line([(cx + bx * s + 1, cy - 2 * s), (cx + bx * s + 1, cy + 2.8 * s)], "v")
    body.pxs([(hx + hr * 0.4, hy + hr * 0.2), (hx + hr * 0.4 + 1, hy + hr * 0.2)], "b")     # goggles
    by = cy + 4.2 * s + curl * 5 * s
    gear.blob(cx - 1 * s + curl * 3 * s, by, 2.6 * s, 1.3 * s, "S", "s", "x")       # the bomb
    gear.pxs([(cx - 3.8 * s + curl * 3 * s, by - 1.4 * s), (cx - 3.8 * s + curl * 3 * s, by + 0.6 * s)], "x")
    gear.outline()
    gear.px(cx + 1.6 * s + curl * 3 * s, by - 0.5, "r")
    ink.paste(body)
    ink.paste(gear)
    if strike in (2, 3):
        flash(ink, hx + hr + 2, hy + 1, big=big)
    return ink


def side_spider(phase, dx=0, strike=0, rear=0.0):
    w = h = 32
    cx, cy = 16 + dx, 16.5
    ink, body, gear = Cv(w, h), Cv(w, h), Cv(w, h)
    ground = cy + 3
    by = ground - 6
    for j, (foot, knee) in enumerate(((10, 6), (5, 3), (-5, -3), (-10, -6))):       # long legs, knees above the body
        step = math.sin(phase + j * math.pi / 2) * 2
        lift = rear * 7 if j == 0 else (1 if math.cos(phase + j * math.pi / 2) > 0.4 else 0)
        ink.line([(cx + (1 if j < 2 else -1), by + 1), (cx + knee, by - 5 - (rear * 3 if j == 0 else 0)),
                  (cx + foot + step, ground - lift)], "k")
        ink.px(cx + knee, by - 5 - (rear * 3 if j == 0 else 0), "L")
    body.blob(cx - 4.5, by - 0.5, 4.8, 4.0, "v", "L", "k")
    hx, hy = cx + 2.8, by + 1 - rear * 2
    body.blob(hx, hy, 2.9, 2.5, "Lz", "L", "k")
    helmet(body, hx, hy, 2.9)
    body.outline()
    body.pxs([(cx - 5, by - 1), (cx - 4, by), (cx - 5, by + 1)], "r")
    body.pxs([(hx + 1.5, hy + 0.6), (hx + 2.5, hy + 1.2)], "r" if strike in (2, 3) else "f")
    barrel(gear, cx - 5, by - 6.5, 11, thick=1)                      # twin guns on its back
    m = barrel(gear, cx - 4, by - 4.8, 11, thick=1)
    gear.pxs([(cx - 5, by - 5.5), (cx - 4, by - 5.5)], "x")
    gear.outline()
    ink.paste(body)
    ink.paste(gear)
    if strike in (2, 3):
        flash(ink, m[0] + 1, by - 5.5 - (strike - 2) * 1.5)
    return ink


def side_snail(phase, dx=0, strike=0, hide=0.0):
    w = h = 40
    cx, cy = 20 + dx, 20.5
    ink, body, gear = Cv(w, h), Cv(w, h), Cv(w, h)
    ground = cy + 3
    stretch = math.sin(phase) * 1.2
    out_ = 1 - hide
    if out_ > 0.05:                                                  # the foot and head
        body.blob(cx + 2 + stretch * 0.5, ground - 1.6, 13 * out_ + 2, 2.2, "t", "c", "T")
        hx = cx + 13 * out_ + stretch
        body.blob(hx, ground - 4, 2.6 * out_ + 0.5, 2.6, "t", "c", "T")
        if out_ > 0.5:
            helmet(body, hx, ground - 4, 2.6)
        ink.line([(hx + 1, ground - 6), (hx + 3 * out_, ground - 10)], "T")
        ink.px(hx + 3 * out_, ground - 10, "k")
    if out_ > 0.3:                                                   # a howitzer through the top of the shell
        for j in range(2):
            gear.line([(cx - 2, cy - 11 + j), (cx + 4 + 8 * out_, cy - 15 + j)], "S" if j == 0 else "x")
    shell = Cv(w, h)
    shell.blob(cx - 3, cy - 5.5, 10, 8.5, "d", "T", "D")
    shell.outline()
    for r, col in ((7.5, "D"), (5, "T"), (2.5, "D")):
        shell.ring(cx - 3, cy - 5.5, r, col, 1, gaps=lambda d, r=r: (d + r * 20) % 360 < 40)
    shell.pxs([(cx + 3, cy - 5), (cx + 4, cy - 5)], "k")             # a firing slit
    body.outline()
    gear.outline()
    ink.paste(gear)
    ink.paste(body)
    ink.paste(shell)
    if out_ > 0.3 and strike in (2, 3):
        flash(ink, cx + 5 + 8 * out_, cy - 15, big=True)
    return ink


def side_grub(phase, dx=0, strike=0, sink=0.0):
    w = h = 24
    cx, cy = 12 + dx, 12.5
    ink, body, gear = Cv(w, h), Cv(w, h), Cv(w, h)
    ground = cy + 3
    down = sink * 8
    seams = []
    for k in range(4, -1, -1):
        hump = abs(math.sin(phase - k * 1.1)) * 1.3
        x = cx + 6 - k * 3.2 + math.sin(phase - k * 1.1) * 0.8
        ry = 3.1 - abs(k - 2) * 0.3
        y = ground - 1 - ry - hump + down
        col = ("o", "u", "R") if k == 0 else ("c", "w", "t")
        body.blob(x, y, 2.2 if k == 0 else 2.3, 2.5 if k == 0 else ry, *col)
        if k == 0:
            helmet(body, x, y, 2.3)
            head = (x, y)
        else:
            seams.append((x + 1.8, y, ry))
            ink.line([(x - 0.5, y + ry - 0.5), (x - 0.5, y + ry + 1.5)], "k")
        if k == 2:                                                   # dynamite strapped to its back
            for i in range(3):
                gear.pxs([(x - 1 + i, y - ry - 2), (x - 1 + i, y - ry - 1)], "r")
            fuse = (x + 2, y - ry - 3)
    body.outline()
    for x, y, ry in seams:
        body.line([(x, y - ry + 1), (x, y + ry - 2)], "t")
    body.px(head[0] + 1, head[1] + 0.6, "k")
    gear.outline()
    gear.px(fuse[0], fuse[1], "y" if int(phase * 2) % 2 == 0 or strike in (2, 3) else "u")
    ink.paste(body)
    ink.paste(gear)
    if sink:                                                         # nothing shows below the ground it has gone into
        for y in range(int(ground) + 1, h):
            for x in range(w):
                ink.p[x, y] = (0, 0, 0, 0)
    return ink


SIDE = {
    "EnemyWasp": lambda phase=0.0, dx=0, strike=0, curl=0.0: side_flyer(32, 32, 15 + dx, 12.5, phase, 1.0, curl, strike),
    "EnemySpider": side_spider,
    "EnemySnail": side_snail,
    "EnemyGrub": side_grub,
    "EnemyBoss": lambda phase=0.0, dx=0, strike=0, roar=0.0, charge=0.0, cracked=0: side_bug(
        64, 64, 29 + dx, 32.5, 19, 11, ("V", "P", "Lz"), phase, "twin", strike, tilt=roar * 3 - charge, plates=3,
        cracked=cracked, banner=True, horn=8),
    "EnemyMantisQueen": lambda phase=0.0, dx=0, strike=0, summon=0.0, lay=0.0: side_mantis(
        64, 64, 28 + dx, 32.5, phase, raise_arms=summon, glow=summon, s=2.0, hat="cap",
        sabre="down" if strike in (2, 3) else "up", banner=True, lay=lay),
    "EnemyHornet": lambda phase=0.0, dx=0, strike=0, curl=0.0, drop=0.0: side_flyer(
        64, 64, 31 + dx, 24.5 + drop * 8, phase, 2.0, curl, strike, beats=8, big=True),
}


def flat_shadow(im, drop=2):
    """Top-down shadows are as deep as the body is wide; under a creature seen from the side they lie flat."""
    box = im.getbbox()
    if not box:
        return im
    x0, y0, x1, y1 = box
    piece = im.crop(box)
    deep = max(2, int((y1 - y0) * 0.38))
    piece = piece.resize((x1 - x0, deep), 0)
    out = im.copy()
    out.paste((0, 0, 0, 0), (0, 0, im.width, im.height))
    out.paste(piece, (x0, (y0 + y1) // 2 + drop - deep // 2))
    return out
