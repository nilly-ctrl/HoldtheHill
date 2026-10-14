"""Hold the Hill scenery sheets: light sources, colony buildings, objects and particles.

  Props/Light<Kind>        Torch, EmberPit, GlowCap, Lantern, Crystal (32x32): a lit loop each, plus
                           Flare / Out where it makes sense
  Fx/FxLightGlow           soft dithered pools of light to lay under a light (64x64): Warm, Cool, Green
  Props/Building<Kind>     Granary, Nursery, Barracks, WatchPost, FungusFarm (48x48):
                           Idle, Build (rises out of the ground), Ruin, and one action each
  Props/Obj<Kind>          Clutter (16x16 variants), Signpost, SideHole, ThornFence, Web, Log
  Fx/FxParticles           small looping or one-shot motes (16x16): Dust, Ember, Spore, Pollen,
                           Leaf, Firefly, Sparkle, Smoke
  Fx/FxWeather             32x32 pieces that tile: Rain, Mist, Wind; and RainSplash

    python build_scenery_sheets.py

Everything is top-down like the rest of the set, lit from the top left, with a 1 px ink outline.
"""
import math
import os
import random

from PIL import Image

import build_anim_sheets as base
from build_anim_sheets import Cv, Sprite, SPARK, SPARK_S, export, fade, flat, tint
from build_extra_sheets import leaf, mask_circle, thick_line

HERE = os.path.dirname(os.path.abspath(__file__))
DIRT = "#87603a"


def prop(name, size, caste, w=None):
    return Sprite(name, "Props", w or size, size, ["Shadow", "Body", "FX"], caste)


def loop(s, tag, n, draw, ms, repeat=0):
    frames = []
    for k in range(n):
        L = s.new()
        draw(L, k)
        frames.append(flat(L))
    s.tag(tag, frames, ms, repeat=repeat)
    return frames


def glow(cv, cx, cy, r, col, strength=1.0):
    """A dithered pool of light: solid-ish at the centre, checkered and fainter toward the rim."""
    for y in range(int(cy - r) - 1, int(cy + r) + 2):
        for x in range(int(cx - r) - 1, int(cx + r) + 2):
            d = math.hypot(x + 0.5 - cx, y + 0.5 - cy) / r
            if d > 1:
                continue
            if d > 0.66 and (x + y) % 2:
                continue
            if d > 0.85 and (x % 2 or y % 2):
                continue
            cv.px(x, y, col, a=int((95 - 60 * d) * strength))


def flame(cv, x, y, h, k, cols=("r", "o", "y")):
    """A teardrop flame rooted at (x, y), h pixels tall, flickering with k."""
    lean = (0, 1, 0, -1)[k % 4]
    for j in range(int(h)):
        t = j / max(1, h - 1)
        half = max(0.5, (1 - t) ** 0.8 * (h * 0.32) * (1.15 if j < 2 else 1))
        xc = x + lean * t
        for dx in range(int(-half - 0.5), int(half + 1.5)):
            if abs(dx) <= half:
                col = cols[2] if abs(dx) < half * 0.45 and t < 0.7 else cols[1] if abs(dx) < half * 0.85 else cols[0]
                cv.px(xc + dx - 0.5, y - j, col)
    cv.px(x - 0.5, y - 1, "w")


# ================================================================ light sources
def build_lights():
    out = []
    c0 = 16

    # ---- twig torch stuck in the ground
    s = prop("LightTorch", 32, "Twig torch")

    def torch(L, k, lit=True, flare=0):
        L["Shadow"].blob(c0 + 1, 24, 5, 2.4, (0, 0, 0), a=80)
        post = Cv(32, 32)
        for dx in (-1, 0):
            post.line([(c0 + dx, 13), (c0 + dx, 24)], "d" if dx else "D")
        post.blob(c0, 12.5, 2.6, 2, "D", "d", "e")              # resin-soaked head
        post.pxs([(c0 - 2, 18), (c0 + 1, 21)], "T")              # bindings
        post.outline()
        L["Body"].paste(post)
        if lit:
            glow(L["FX"], c0, 12, 9 + (k % 2) + flare * 2, "o")
            flame(L["FX"], c0, 11, 7 + (k % 2) + flare * 2, k)
            L["FX"].px(c0 - 3 + (k * 2) % 6, 3 - (k % 3), "y")   # a spark
        else:
            L["Body"].px(c0 - 1, 11, "r")
            L["FX"].blob(c0 + (k % 2), 8 - k, 1.8, 1.6, "S", "s", "x", a=160 - k * 30)

    loop(s, "Lit", 4, torch, 100)
    loop(s, "Flare", 4, lambda L, k: torch(L, k, True, (1, 2, 1, 0)[k]), 70, repeat=1)
    loop(s, "Out", 4, lambda L, k: torch(L, k, False), 220)
    out.append(s)

    # ---- ember pit: a ring of pebbles round burning twigs
    s = prop("LightEmberPit", 32, "Ember pit")

    def pit(L, k, lit=True):
        L["Shadow"].blob(c0, 19, 11, 7, (0, 0, 0), a=70)
        body = Cv(32, 32)
        body.blob(c0, 18, 8, 5.5, "e", "D", "k")
        for j in range(9):
            ang = math.radians(j * 40)
            body.blob(c0 + math.cos(ang) * 9, 18 + math.sin(ang) * 6, 2, 1.6, "S", "s", "x")
        body.outline()
        thick_line(body, [(c0 - 5, 20), (c0 + 4, 16)], ("d", "T", "D"))
        thick_line(body, [(c0 - 4, 15), (c0 + 5, 20)], ("d", "T", "D"))
        L["Body"].paste(body)
        if lit:
            glow(L["FX"], c0, 16, 13 + (k % 2), "o")
            flame(L["FX"], c0 - 3, 17, 6 + (k % 2), k)
            flame(L["FX"], c0 + 2, 18, 8 - (k % 2), k + 1)
            flame(L["FX"], c0, 16, 5 + ((k + 1) % 2), k + 2)
        else:
            for j, (dx, dy) in enumerate(((-3, 0), (2, 1), (0, -2), (4, -1))):
                if (j + k) % 3:
                    L["FX"].px(c0 + dx, 18 + dy, "o" if (j + k) % 2 else "r")

    loop(s, "Burn", 4, pit, 100)
    loop(s, "Embers", 4, lambda L, k: pit(L, k, False), 260)
    out.append(s)

    # ---- glow-cap mushroom
    s = prop("LightGlowCap", 32, "Glow-cap mushroom")

    def cap(L, k, bright=True):
        L["Shadow"].blob(c0 + 1, 21, 8, 4, (0, 0, 0), a=75)
        pulse = (0, 1, 2, 1)[k]
        if bright:
            glow(L["FX"], c0, 15, 12 + pulse, "I")
        body = Cv(32, 32)
        body.blob(c0 + 6, 20, 3, 2.6, "i", "I", "iz")
        body.blob(c0 - 6, 21, 2.4, 2, "i", "I", "iz")
        body.blob(c0, 15, 7.5, 6.5, "i", "I", "iz")
        body.outline()
        for x, y in ((c0 - 3, 13), (c0 + 2, 12), (c0 + 3, 17), (c0 - 2, 18), (c0 + 6, 19)):
            body.px(x, y, "w" if bright and (x + k) % 2 else "I")
        if not bright:
            body.im.paste(tint(body.im, "S", 0.45))
            body.p = body.im.load()
        L["Body"].paste(body)
        if bright:
            for j in range(3):                                   # spores lifting off
                L["FX"].px(c0 - 6 + j * 6, 6 - ((k + j * 2) % 5), "I", a=230 - ((k + j * 2) % 5) * 40)

    loop(s, "Glow", 4, cap, 220)
    loop(s, "Dim", 1, lambda L, k: cap(L, k, False), 1000)
    out.append(s)

    # ---- firefly lantern: a firefly in a seed-pod cage on a twig
    s = prop("LightLantern", 32, "Firefly lantern")

    def lantern(L, k):
        L["Shadow"].blob(c0 + 1, 25, 5, 2.2, (0, 0, 0), a=80)
        on = (1, 1, 0.5, 1, 1, 0.2)[k]
        glow(L["FX"], c0, 11, 11, "y", on)
        post = Cv(32, 32)
        for dx in (-1, 0):
            post.line([(c0 + dx, 16), (c0 + dx, 25)], "d" if dx else "D")
        post.blob(c0, 11, 5, 5.5, "T", "t", "d")
        post.outline()
        post.blob(c0, 11, 3.4, 4, "e")                           # the hollow
        for x in (c0 - 2, c0 + 1):                               # cage ribs
            post.line([(x, 7), (x, 15)], "T")
        L["Body"].paste(post)
        fx, fy = c0 + (0, 1, 0, -1, 0, 1)[k] - 0.5, 11 + (0, -1, 0, 1, 0, 0)[k]
        L["FX"].px(fx, fy, "w" if on > 0.6 else "Y")
        L["FX"].px(fx, fy + 1, "y" if on > 0.3 else "Y")

    loop(s, "Glow", 6, lantern, 180)
    out.append(s)

    # ---- dew crystal
    s = prop("LightCrystal", 32, "Dew crystal")

    def crystal(L, k):
        L["Shadow"].blob(c0 + 1, 23, 7, 3, (0, 0, 0), a=75)
        glow(L["FX"], c0, 15, 11 + (0, 1, 0, 0)[k], "b")
        body = Cv(32, 32)
        for cx, top, w, col in ((c0 - 5, 13, 2, "B"), (c0 + 5, 11, 2, "B"), (c0, 5, 3, "b")):
            for y in range(top, 23):
                half = min(w, (y - top) * 0.8 + 0.5, (23 - y) * 1.2 + 0.5)
                for x in range(int(cx - half), int(cx + half) + 1):
                    body.px(x, y, "w" if x < cx - half + 1 else col if x <= cx else "n")
        body.outline()
        L["Body"].paste(body)
        gx, gy = ((c0 - 1, 9), (c0 + 5, 14), (c0 - 5, 16), (c0 + 1, 18))[k]
        L["FX"].spr(SPARK_S, gx - 1, gy - 1)

    loop(s, "Glow", 4, crystal, 260)
    out.append(s)

    # ---- pools of light to lay under any of them
    s = Sprite("FxLightGlow", "Fx", 64, 64, ["FX"], "Pools of light")
    for tag, col in (("Warm", "o"), ("Cool", "b"), ("Green", "I")):
        frames = []
        for k in range(4):
            cv = Cv(64, 64)
            glow(cv, 32, 32, 27 + (0, 1.5, 3, 1.5)[k], col, 1.25)
            glow(cv, 32, 32, 13 + (0, 1, 2, 1)[k], "w" if col != "o" else "y", 0.8)
            frames.append({"FX": cv.im})
        s.tag(tag, frames, 180)
    out.append(s)
    return out


# ================================================================ buildings
def raise_frames(s, pose, cx, cy, n=6):
    """Rising out of the ground: uncovered from the middle outward under a shower of dirt."""
    out = []
    half = s.w / 2
    for k in range(n):
        t = k / (n - 1)
        L = {name: Image.new("RGBA", (s.w, s.h), (0, 0, 0, 0)) for name in s.layers}
        L["Shadow"] = fade(pose["Shadow"], t)
        body = mask_circle(pose["Body"], cx, cy, half * (0.25 + 1.3 * t))
        L["Body"] = tint(body, DIRT, 0.7 * (1 - t))
        fx = Cv(s.w, s.h)
        if k < n - 1:
            for j in range(10):
                ang = math.radians(j * 36 + 10)
                d = half * (0.3 + 0.65 * t)
                fx.blob(cx + math.cos(ang) * d, cy + math.sin(ang) * d * 0.8 - math.sin(t * math.pi) * 3,
                        2.4 - t * 1.2, 2.1 - t, "d", "T", "D", a=int(250 - t * 130))
        else:
            fx.paste(pose["FX"])
        L["FX"] = fx.im
        out.append(L)
    return out


def ruin_frames(s, pose, cx, cy):
    """Knocked flat: darkened, cracked, with rubble and a thread of smoke."""
    out = []
    rng = random.Random(s.name)
    cracks = []
    for _ in range(5):
        x, y = cx + rng.uniform(-10, 10), cy + rng.uniform(-10, 4)
        pts = [(x, y)]
        for _ in range(4):
            x += rng.choice((-2, -1, 1, 2)); y += rng.choice((1, 2))
            pts.append((x, y))
        cracks.append(pts)
    rubble = [(cx + rng.uniform(-17, 17), cy + rng.uniform(4, 15), rng.uniform(1.4, 2.6)) for _ in range(9)]
    for k in range(4):
        L = {name: Image.new("RGBA", (s.w, s.h), (0, 0, 0, 0)) for name in s.layers}
        L["Shadow"] = pose["Shadow"]
        body = Cv(s.w, s.h)
        # the lower two thirds survive; the top is gone
        low = pose["Body"].copy()
        p = low.load()
        for y in range(s.h):
            for x in range(s.w):
                if y < cy - 6 + ((x * 7) % 5):
                    p[x, y] = (0, 0, 0, 0)
        body.paste(tint(low, "x", 0.45))
        for pts in cracks:
            body.line(pts, "k")
        heap = Cv(s.w, s.h)
        for x, y, r in rubble:
            heap.blob(x, y, r, r * 0.8, "S", "s", "x")
        heap.outline()
        body.paste(heap)
        L["Body"] = body.im
        fx = Cv(s.w, s.h)
        for step in range(3):
            y = cy - 8 - step * 7 - (k * 2) % 7
            fx.blob(cx + 3 + (step % 2) * 3, y, 3 - step * 0.6, 2.6 - step * 0.5, "S", "s", "x", a=170 - step * 45)
        L["FX"] = fx.im
        out.append(L)
    return out


def building(name, caste, draw, action=None):
    """draw(L, k, act) paints one frame; act is 0 for Idle or 1..n-1 through the action."""
    s = prop("Building" + name, 48, caste)
    idle = loop(s, "Idle", 4, lambda L, k: draw(L, k, 0), 220)
    s.tag("Build", raise_frames(s, idle[0], 24, 26), 90, repeat=1)
    if action:
        tag, n, ms = action
        loop(s, tag, n, lambda L, k: draw(L, 0, k + 1), ms, repeat=1)
    s.tag("Ruin", ruin_frames(s, idle[0], 24, 26), 240)
    return s


def little_ant(cv, x, y, facing_up=True, col=("a", "A", "z")):
    a = Cv(cv.w, cv.h)
    d = -1 if facing_up else 1
    a.blob(x, y - d * 2.5, 1.7, 1.9, *col)
    a.blob(x, y + d * 0.5, 1.2, 1.2, *col)
    a.blob(x, y + d * 2.8, 1.6, 1.4, *col)
    a.outline()
    cv.paste(a)


def build_buildings():
    out = []
    c0 = 24

    def granary(L, k, act):
        L["Shadow"].blob(c0 + 1, 33, 19, 11, (0, 0, 0), a=85)
        b = Cv(48, 48)
        b.blob(c0, 27, 17, 14, "T", "t", "d")                    # woven pod walls
        for r in (13, 9, 5):
            b.ring(c0, 27, r + 3, "d", 1, ry=r + 1)
        b.outline()
        b.blob(c0, 25, 8, 6, "e", "D", "k")                      # the open top
        seeds = Cv(48, 48)
        fill = 5 + (1 if act >= 4 else 0)
        rng = random.Random(4)
        for j in range(fill * 3):                                # heaped pale seeds, so the store reads as full, not as a hole
            seeds.blob(c0 + rng.uniform(-6, 6), 25 + rng.uniform(-4, 3.5), 2.1, 1.5, "t", "c", "T")
        seeds.outline()
        b.paste(seeds)
        L["Body"].paste(b)
        if act == 0 and k == 2:
            L["FX"].spr(SPARK_S, c0 + 4, 20)
        if act:                                                  # a worker carries a seed up and tips it in
            y = (44, 39, 34, 31, 33, 38)[act - 1]
            little_ant(L["FX"], c0 + 12 - (act - 1) * 1.6, y)
            if act <= 3:
                L["FX"].blob(c0 + 12 - (act - 1) * 1.6, y - 5, 1.9, 1.4, "d", "T", "D")
            if act == 4:
                L["FX"].spr(SPARK_S, c0 + 3, 21)

    def nursery(L, k, act):
        L["Shadow"].blob(c0 + 1, 33, 19, 11, (0, 0, 0), a=85)
        b = Cv(48, 48)
        b.blob(c0, 27, 17, 14, "t", "c", "T")                    # a pale dome of chewed leaf
        b.ring(c0, 28, 13, "T", 1, ry=10)
        b.outline()
        b.blob(c0, 28, 9, 7, "e", "D", "k")                      # looking down into the brood pit
        eggs = Cv(48, 48)
        for j, (dx, dy) in enumerate(((-5, -1), (-1, -3), (3, -1), (-3, 2), (1, 1), (5, 2), (-1, 4))):
            wob = 1 if act and (j + act) % 3 == 0 else 0
            eggs.blob(c0 + dx, 28 + dy - wob, 2, 1.6, "c", "w", "t")
        eggs.outline()
        b.paste(eggs)
        L["Body"].paste(b)
        heart_k = k if act == 0 else act
        if act or k in (1, 2):
            hx, hy = c0 + 12, 14 - heart_k
            L["FX"].pxs([(hx, hy + 1), (hx + 1, hy), (hx + 2, hy + 1), (hx + 3, hy), (hx + 4, hy + 1), (hx + 1, hy + 2),
                         (hx + 2, hy + 2), (hx + 3, hy + 2), (hx + 2, hy + 3), (hx + 1, hy + 1), (hx + 3, hy + 1)], "f")
        if act == 4:                                             # one hatches
            L["FX"].ring(c0 - 1, 25, 4, "w", 1)
        if act >= 5:
            little_ant(L["FX"], c0 - 1, 25, col=("#e9b49c", "#fff1d6", "#b9795f"))

    def barracks(L, k, act):
        L["Shadow"].blob(c0 + 1, 33, 20, 12, (0, 0, 0), a=85)
        b = Cv(48, 48)
        b.blob(c0, 27, 15, 12, "d", "T", "D")                    # packed-earth yard
        b.outline()
        ring = Cv(48, 48)
        for j in range(16):                                      # a palisade of thorns
            ang = math.radians(j * 22.5 + 11)
            if 60 < (j * 22.5 + 11) < 120:
                continue                                         # the gate, facing down
            x, y = c0 + math.cos(ang) * 16, 27 + math.sin(ang) * 13
            ring.line([(x, y + 2), (x, y - 3)], "M")
            ring.px(x, y - 4, "g")
            ring.px(x - 1, y + 1, "j")
        ring.outline()
        L["Body"].paste(b)
        march = act if act else 0
        for j, (dx, dy) in enumerate(((-5, -3), (3, -3), (-5, 3), (3, 3))):   # soldiers drilling
            step = (0, -1, 0, 1)[(march + j) % 4] if act else 0
            little_ant(L["Body"], c0 + dx + 1, 27 + dy + step, col=("q", "Q", "Z"))
        L["Body"].paste(ring)
        f = Cv(48, 48)
        f.line([(c0 + 15, 6), (c0 + 15, 18)], "D")
        wave = (0, 1, 0, -1)[k if not act else act % 4]
        for row in range(4):
            f.line([(c0 + 9 - (wave if row % 2 else 0), 7 + row), (c0 + 14, 7 + row)], "r" if row < 3 else "R")
        f.outline()
        L["Body"].paste(f)
        if act in (2, 4):
            L["FX"].pxs([(c0 - 9, 20), (c0 + 8, 20)], "c")       # mandibles clack

    def watch_post(L, k, act):
        L["Shadow"].blob(c0 + 2, 36, 11, 6, (0, 0, 0), a=85)
        b = Cv(48, 48)
        for x, lean in ((c0 - 7, 2), (c0 + 6, -2), (c0 - 1, 0)):  # three grass-blade legs
            b.line([(x, 40), (x + lean, 22)], "M")
            b.line([(x + 1, 40), (x + lean + 1, 22)], "m")
        b.blob(c0, 19, 10, 7, "T", "t", "d")                     # the platform
        b.ring(c0, 19, 10, "d", 1, ry=7)
        b.outline()
        L["Body"].paste(b)
        look = ((0, 0), (-2, 0), (0, 0), (2, 0))[k] if not act else ((0, -1), (0, -2), (0, -1), (0, -2), (0, 0))[act - 1]
        little_ant(L["Body"], c0 + look[0], 18 + look[1])
        if act and act < 5:
            L["FX"].line([(c0 + 8, 6 - (act % 2)), (c0 + 8, 11 - (act % 2))], "r")
            L["FX"].px(c0 + 8, 13 - (act % 2), "r")
            L["FX"].ring(c0, 18, 6 + act * 3, "r", 1, a=220 - act * 45)

    def fungus_farm(L, k, act):
        L["Shadow"].blob(c0 + 1, 33, 19, 11, (0, 0, 0), a=85)
        b = Cv(48, 48)
        b.blob(c0, 28, 17, 13, "D", "d", "e")                    # a bed of chewed-leaf mulch
        b.outline()
        for j in range(14):
            b.px(c0 - 13 + (j * 5) % 27, 22 + (j * 7) % 13, "M" if j % 2 else "j")
        caps = Cv(48, 48)
        grow = 0.25 * min(act, 4) if act else 0
        for j, (dx, dy, r) in enumerate(((-9, -2, 3.2), (-2, -5, 3.8), (6, -3, 3), (-5, 4, 2.8), (3, 3, 3.6), (10, 4, 2.6))):
            caps.blob(c0 + dx, 28 + dy, r + grow, (r + grow) * 0.85, "t", "c", "T")
        caps.outline()
        b.paste(caps)
        L["Body"].paste(b)
        for j in range(4):                                       # spores
            y = 16 - ((k * 2 + j * 3) % 9)
            L["FX"].px(c0 - 9 + j * 6, y, "c", a=230 - ((k * 2 + j * 3) % 9) * 22)
        if act >= 3:                                             # a farmer comes to cut one
            little_ant(L["FX"], c0 + 14 - (act - 3) * 2, 37 - (act - 3))
        if act == 6:
            L["FX"].spr(SPARK_S, c0 + 2, 26)

    out.append(building("Granary", "Granary: a woven pod of stored seeds", granary, ("Deliver", 6, 110)))
    out.append(building("Nursery", "Nursery: the brood pit", nursery, ("Hatch", 6, 130)))
    out.append(building("Barracks", "Barracks: a thorn-ringed drill yard", barracks, ("Drill", 6, 120)))
    out.append(building("WatchPost", "Watch post on grass-blade stilts", watch_post, ("Alert", 5, 100)))
    out.append(building("FungusFarm", "Fungus farm", fungus_farm, ("Harvest", 6, 130)))
    return out


# ================================================================ objects
def build_objects():
    out = []

    # ---- small clutter, one variant per tag
    s = prop("ObjClutter", 16, "Small objects")

    def acorn(L, k):
        L["Shadow"].blob(8.5, 12, 4.5, 2, (0, 0, 0), a=75)
        b = Cv(16, 16)
        b.blob(8, 9.5, 3.6, 3.6, "T", "t", "d")
        b.blob(8, 6, 4, 2.2, "d", "T", "D")
        b.outline()
        b.px(8, 3, "D")
        b.px(6, 9, "c")
        L["Body"].paste(b)

    def sack(L, k):
        L["Shadow"].blob(8.5, 12.5, 5, 2, (0, 0, 0), a=75)
        b = Cv(16, 16)
        b.blob(8, 9.5, 4.4, 3.8, "t", "c", "T")                  # a leaf wrapped round seeds
        b.blob(8, 5, 1.6, 1.4, "t", "c", "T")
        b.outline()
        b.line([(6, 6), (9, 6)], "M")                            # grass tie
        b.pxs([(6, 10), (9, 11), (8, 9)], "T")
        L["Body"].paste(b)

    def shell(L, k):
        L["Shadow"].blob(8.5, 12, 5.5, 2.2, (0, 0, 0), a=75)
        b = Cv(16, 16)
        b.blob(8, 9, 5, 3.6, "h", "H", "hz")                     # an empty beetle wing-case
        b.outline()
        b.line([(4, 9), (12, 9)], "k")
        b.blob(9, 10.5, 2, 1.2, "k")
        b.px(6, 7, "w")
        L["Body"].paste(b)

    def cairn(L, k):
        L["Shadow"].blob(8.5, 13, 5.5, 2, (0, 0, 0), a=75)
        for x, y, rx, ry in ((8, 11.5, 4.5, 2.4), (8.5, 8, 3.2, 2), (8, 5, 2, 1.5)):
            one = Cv(16, 16)
            one.blob(x, y, rx, ry, "S", "s", "x")
            one.outline()
            L["Body"].paste(one)

    def dew(L, k):
        L["Shadow"].blob(8.5, 12, 4.5, 1.8, (0, 0, 0), a=60)
        leaf(L["Body"], 8, 10, 13, 6, 8, ("m", "g", "M"))
        d = Cv(16, 16)
        d.blob(8, 8, 3 + (0.3 if k % 2 else 0), 2.8, "b", "w", "B", a=215)
        d.outline("n")
        L["Body"].paste(d)
        L["FX"].px(6 + (k % 2), 6, "w")
        if k == 2:
            L["FX"].spr(SPARK_S, 10, 3)

    for tag, draw, n in (("Acorn", acorn, 1), ("SeedSack", sack, 1), ("Shell", shell, 1), ("Cairn", cairn, 1), ("Dewdrop", dew, 4)):
        loop(s, tag, n, draw, 1000 if n == 1 else 260)
    out.append(s)

    # ---- signpost
    s = prop("ObjSignpost", 24, "Twig signpost")

    def sign(L, k, tilt=0):
        L["Shadow"].blob(12.5, 20, 4.5, 2, (0, 0, 0), a=80)
        b = Cv(24, 24)
        b.line([(11 + tilt, 9), (11, 20)], "D")
        b.line([(12 + tilt, 9), (12, 20)], "d")
        board = [(5 + tilt, 5), (18 + tilt, 4), (21 + tilt, 7), (18 + tilt, 10), (5 + tilt, 10)]
        for y in range(4, 11):
            for x in range(5, 22):
                inside = x + 0.0 <= 18 + tilt + (3 - abs(y - 7)) if x > 18 + tilt else x >= 5 + tilt
                if inside:
                    b.px(x, y, "t" if y < 6 else "T")
        b.outline()
        b.line([(8 + tilt, 6), (15 + tilt, 6)], "D")              # scratched marks
        b.line([(8 + tilt, 8), (13 + tilt, 8)], "D")
        L["Body"].paste(b)

    loop(s, "Idle", 1, sign, 1000)
    loop(s, "Wobble", 5, lambda L, k: sign(L, k, (1, -1, 1, 0, 0)[k]), 70, repeat=1)
    out.append(s)

    # ---- a side hole with a worker looking out
    s = prop("ObjSideHole", 32, "Side entrance")

    def hole(L, k, peek=0.0):
        L["Shadow"].blob(16, 18, 11, 8, (0, 0, 0), a=70)
        b = Cv(32, 32)
        b.blob(16, 17, 10, 7.5, "d", "T", "D")
        b.outline()
        b.blob(16, 17, 5.5, 4, "k")
        b.blob(16, 16.5, 4, 2.6, "e")
        for x, y, c in ((8, 13, "c"), (24, 14, "S"), (10, 22, "s"), (22, 22, "T")):
            b.px(x, y, c)
        L["Body"].paste(b)
        if peek > 0:
            a = Cv(32, 32)
            y = 19 - peek * 5
            a.blob(16, y, 3, 2.4, "a", "A", "z")
            a.outline()
            a.pxs([(14, y - 1), (17, y - 1)], "k")
            L["Body"].paste(mask_circle(a.im, 16, 16, 6.5))
            L["FX"].line([(14, y - 3), (12, y - 6 - (k % 2))], "k")
            L["FX"].line([(17, y - 3), (19, y - 6 - ((k + 1) % 2))], "k")
        elif k == 1:
            L["FX"].pxs([(14, 17), (17, 17)], "c")                # eyes in the dark

    loop(s, "Idle", 2, hole, 900)
    loop(s, "Peek", 7, lambda L, k: hole(L, k, (0.3, 0.7, 1, 1, 1, 0.6, 0.2)[k]), 110, repeat=1)
    out.append(s)

    # ---- thorn fence pieces
    s = prop("ObjThornFence", 32, "Thorn fence pieces")

    def stake(b, x, y, broken=False, short=False):
        h = 3 if broken else 3 if short else 7   # short: on an up-down run, so stakes don't merge
        b.line([(x, y), (x, y - h)], "M")
        b.line([(x + 1, y), (x + 1, y - h + 1)], "j")
        if not broken:
            b.px(x, y - h - 1, "g")

    def fence(L, pts, broken=False):
        b = Cv(32, 32)
        for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
            thick_line(b, [(x0, y0 - 2), (x1, y1 - 2)], ("d", "T", "D"))
            steps = max(1, int(math.hypot(x1 - x0, y1 - y0) // 6))
            for j in range(steps + 1):
                x, y = x0 + (x1 - x0) * j / steps, y0 + (y1 - y0) * j / steps
                L["Shadow"].blob(x + 1, y + 1, 2.4, 1.4, (0, 0, 0), a=70)
                stake(b, x, y, broken=bool(broken) and j % 2 == 1, short=abs(y1 - y0) > abs(x1 - x0))
        b.outline()
        L["Body"].paste(b)

    loop(s, "Horizontal", 1, lambda L, k: fence(L, [(1, 20), (30, 20)]), 1000)
    loop(s, "Vertical", 1, lambda L, k: fence(L, [(16, 4), (16, 30)]), 1000)
    loop(s, "Corner", 1, lambda L, k: fence(L, [(16, 30), (16, 18), (30, 18)]), 1000)
    loop(s, "Broken", 1, lambda L, k: fence(L, [(1, 20), (30, 20)], broken=True), 1000)
    out.append(s)

    # ---- a web strung in a corner
    s = prop("ObjWeb", 32, "Corner web")

    def web(L, k):
        sway = (0, 1, 0, -1)[k]
        b = L["Body"]
        hub = (9 + sway, 9 + sway)
        for ex, ey in ((1, 1), (30, 2), (2, 30), (24, 12), (12, 24), (28, 22)):
            b.line([hub, (ex, ey)], "c", a=220)
        for r in (5, 9, 13):
            pts = [(hub[0] + math.cos(math.radians(d)) * r, hub[1] + math.sin(math.radians(d)) * r) for d in range(-10, 101, 22)]
            b.line(pts, "s", a=210)
        L["FX"].px(15 + sway, 12, "b")
        L["FX"].px(11, 17 + sway, "w")
        fly = Cv(32, 32)                                          # something caught
        fly.blob(17 + sway, 17 + sway, 1.8, 1.4, "x", "S", "k")
        fly.outline()
        L["FX"].paste(fly)

    loop(s, "Sway", 4, web, 260)
    out.append(s)

    # ---- hollow twig log
    s = prop("ObjLog", 32, "Hollow log", w=48)

    def log(L, k):
        L["Shadow"].blob(25, 21, 21, 6, (0, 0, 0), a=80)
        b = Cv(48, 32)
        for y in range(10, 22):
            for x in range(6, 42):
                b.px(x, y, "T" if y < 12 else "d" if y < 18 else "D")
        b.blob(41, 16, 4, 6, "t", "c", "T")                       # the cut end
        b.blob(6, 16, 3, 6, "D", "d", "e")
        b.outline()
        b.blob(41, 16, 2.2, 3.6, "e", "D", "k")
        for x in (12, 20, 27, 34):
            b.line([(x, 12), (x + 2, 15), (x + 1, 19)], "D")
        b.pxs([(15, 10), (16, 9), (30, 10)], "m")                 # moss
        b.blob(23, 10, 3, 1.5, "M", "m", "j")
        L["Body"].paste(b)

    loop(s, "Idle", 1, log, 1000)
    out.append(s)
    return out


# ================================================================ particles
def build_particles():
    c0 = 8
    s = Sprite("FxParticles", "Fx", 16, 16, ["FX"], "Small motes")

    def tag(name, n, draw, ms, repeat=0):
        frames = []
        for k in range(n):
            cv = Cv(16, 16)
            draw(cv, k)
            frames.append({"FX": cv.im})
        s.tag(name, frames, ms, repeat=repeat)

    def dust(cv, k):
        r = 1.5 + k * 1.1
        for j in range(4):
            ang = math.radians(j * 90 + 30)
            cv.blob(c0 + math.cos(ang) * k * 1.2, c0 + math.sin(ang) * k * 0.9, r * 0.7, r * 0.6, "t", "c", "T", a=230 - k * 45)

    def ember(cv, k):
        for j in range(3):
            y = 14 - ((k * 2 + j * 5) % 14)
            x = 4 + j * 4 + round(math.sin((k + j) * 1.3))
            cv.px(x, y, "y" if y > 8 else "o" if y > 4 else "r", a=255 - (14 - y) * 10)
            if y > 9:
                cv.px(x, y + 1, "o", a=120)

    def mote(col, hi):
        def draw(cv, k):
            for j in range(3):
                x = 3 + j * 5 + round(math.sin((k + j * 2) * 0.9) * 1.5)
                y = 11 - j * 3 - round(math.cos((k + j) * 0.8) * 1.5)
                cv.px(x, y, col, a=230)
                if (k + j) % 3 == 0:
                    cv.px(x, y - 1, hi, a=200)
        return draw

    def falling_leaf(cv, k):
        x = 4 + k * 1.2 + math.sin(k * 1.2) * 2
        y = 2 + k * 1.6
        # one leaf, one colour: it turns edge-on and back as it tumbles
        leaf(cv, x, y, 7, (4, 2.2, 4, 2.2, 4, 3)[k], 20 + k * 50, ("o", "u", "R"))

    def firefly(cv, k):
        x, y = c0 + math.cos(k * 1.05) * 4, c0 + math.sin(k * 2.1) * 3
        on = (1, 1, 0, 0, 1, 0)[k]
        if on:
            cv.disc(x, y, 2.6, "y", a=70)
            cv.px(x, y, "w")
            cv.px(x - 1, y, "y")
        else:
            cv.px(x, y, "Y")

    def sparkle(cv, k):
        for j, (x, y) in enumerate(((3, 4), (11, 3), (7, 10), (12, 12))):
            phase = (k + j) % 4
            if phase == 0:
                cv.px(x, y, "w")
            elif phase == 1:
                cv.spr(SPARK_S, x - 1, y - 1)
            elif phase == 2:
                cv.px(x, y, "y")

    def smoke(cv, k):
        for j in range(3):
            y = 13 - ((k + j * 2) % 6) * 2.2
            r = 1.2 + ((k + j * 2) % 6) * 0.45
            cv.blob(c0 + math.sin((k + j) * 1.1) * 2, y, r, r, "S", "s", "x", a=int(200 - ((k + j * 2) % 6) * 30))

    tag("Dust", 5, dust, 70, repeat=1)
    tag("Ember", 6, ember, 90)
    tag("Spore", 6, mote("I", "w"), 160)
    tag("Pollen", 6, mote("y", "c"), 160)
    tag("Leaf", 6, falling_leaf, 120, repeat=1)
    tag("Firefly", 6, firefly, 180)
    tag("Sparkle", 4, sparkle, 130)
    tag("Smoke", 6, smoke, 130)

    w = Sprite("FxWeather", "Fx", 32, 32, ["FX"], "Weather pieces that tile")

    def wtag(name, n, draw, ms, repeat=0):
        frames = []
        for k in range(n):
            cv = Cv(32, 32)
            draw(cv, k)
            frames.append({"FX": cv.im})
        w.tag(name, frames, ms, repeat=repeat)

    drops = [(random.Random(5 + j).randrange(32), random.Random(50 + j).randrange(32)) for j in range(14)]

    def rain(cv, k):
        for x, y in drops:                                        # each frame shifts by a quarter tile, so it loops
            yy, xx = (y + k * 8) % 32, (x - k * 2) % 32
            cv.px(xx, yy, "b", a=210)
            cv.px((xx + 1) % 32, (yy - 1) % 32, "b", a=150)
            cv.px((xx + 1) % 32, (yy - 2) % 32, "B", a=90)

    def splash(cv, k):
        for j, (x, y) in enumerate(((8, 10), (22, 16), (13, 25))):
            r = 1 + k * 1.2
            cv.ring(x, y, r + 1, "b", 1, a=230 - k * 55, ry=(r + 1) * 0.5)
            if k == 0:
                cv.px(x, y - 1, "w")

    def mist(cv, k):
        for j, (x, y, rx) in enumerate(((6, 8, 7), (24, 15, 8), (12, 25, 7))):
            xx = (x + k * 8) % 32                                 # drifts one tile over four frames
            for dx in (-32, 0, 32):
                glow(cv, xx + dx, y, rx, "c", 0.38)               # thin: it lies over the whole map

    def wind(cv, k):
        for j, (x, y, length) in enumerate(((2, 6, 9), (16, 14, 12), (6, 24, 8))):
            x0 = (x + k * 8) % 32
            for d in range(length):
                cv.px((x0 + d) % 32, y + round(math.sin((d + k) * 0.6)), "c", a=int(200 * (1 - abs(d - length / 2) / (length / 2 + 0.01))))

    wtag("Rain", 4, rain, 60)
    wtag("RainSplash", 4, splash, 70, repeat=1)
    wtag("Mist", 4, mist, 320)
    wtag("Wind", 4, wind, 90)
    return [s, w]


# ================================================================ run
def main():
    groups = [("lights", build_lights()), ("buildings", build_buildings()), ("objects", build_objects()),
              ("particles", build_particles())]
    results = []
    for label, sprites in groups:
        for s in sprites:
            results.append((s, export(s)))
            n = sum(len(f) for _, f, _, _ in s.tags)
            print(f"{s.group:6} {s.name:20} {s.w}x{s.h}  {n:2} frames  " + ", ".join(t[0] for t in s.tags))
    by_name = {s.name: r for s, r in ((s, (s, r)) for s, r in results)}
    pick = lambda names: [by_name[n] for n in names]
    base.preview_sheet(pick([s.name for s in groups[0][1]]), os.path.join(HERE, "LightSheetPreview.png"), scale=3)
    base.preview_sheet(pick([s.name for s in groups[1][1]]), os.path.join(HERE, "BuildingSheetPreview.png"), scale=2)
    base.preview_sheet(pick([s.name for s in groups[2][1] + groups[3][1]]), os.path.join(HERE, "ObjectSheetPreview.png"), scale=3)
    total = sum(sum(len(f) for _, f, _, _ in s.tags) for s, _ in results)
    print(f"{len(results)} sprites, {total} frames written and read back OK")


if __name__ == "__main__":
    main()
