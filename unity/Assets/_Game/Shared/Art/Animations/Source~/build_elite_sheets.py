"""Hold the Hill elite enemies and two more bosses.

  Enemies/Enemy<Name>Elite    an elite of each of the 12 enemies: the same frames and tags, in a
                              darker blood-red livery with a gold rim round the body. For the first
                              seven the Attack and Spawn from Enemy<Name>Moves are folded in, so an
                              elite is one complete file.
  Enemies/EnemyMantisQueen    boss (64x64): Walk, Hurt, Death, Attack, Spawn, Summon
  Enemies/EnemyHornet         boss, a flyer (64x64): Walk, Hurt, Death, Attack, Spawn, Dive

    python build_elite_sheets.py
"""
import math
import os

import build_anim_sheets as base
import build_extra_sheets as extra
import build_roster_sheets as roster
from build_anim_sheets import Cv, Sprite, SPARK_S, export, flat
from build_extra_sheets import thick_line

HERE = os.path.dirname(os.path.abspath(__file__))
INK = base.rgba("k")[:3]


# ================================================================ elites
def livery(px):
    r, g, b, a = px
    if not a or (r, g, b) == INK:
        return px
    lum = (r * 3 + g * 5 + b * 2) / 10
    return (min(255, int(lum * 0.35 + r * 0.55 + 36)), int(g * 0.42 + lum * 0.1), int(b * 0.42 + lum * 0.1 + 8), a)


def gold_rim(im):
    """A one-pixel gold line round everything on the layer, outside its ink outline."""
    out = im.copy()
    src, p = im.load(), out.load()
    gold, pale = base.rgba("y"), base.rgba("w")
    for y in range(im.height):
        for x in range(im.width):
            if src[x, y][3]:
                continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, ny = x + dx, y + dy
                if 0 <= nx < im.width and 0 <= ny < im.height and src[nx, ny][3] > 100:
                    p[x, y] = pale if (dx, dy) == (1, 0) and (x + y) % 5 == 0 else gold
                    break
    return out


def elite_of(sprite, extra_tags=()):
    s = Sprite(sprite.name + "Elite", "Enemies", sprite.w, sprite.h, sprite.layers, sprite.caste.split(" attack")[0] + ", elite")
    for tag, frames, ms, repeat in list(sprite.tags) + list(extra_tags):
        new = []
        for f in frames:
            L = dict(f)
            body = L["Body"].copy()
            p = body.load()
            for y in range(body.height):
                for x in range(body.width):
                    p[x, y] = livery(p[x, y])
            L["Body"] = gold_rim(body)
            new.append(L)
        s.tag(tag, new, ms, repeat)
    return s


def build_elites():
    moves = {m.name[:-len("Moves")]: m for m in extra.build_enemy_moves()}
    out = []
    for s in base.build_enemies():
        out.append(elite_of(s, moves[s.name].tags if s.name in moves else ()))
    for s in roster.build_enemies():
        out.append(elite_of(s))
    return out


# ================================================================ bosses
def build_bosses():
    out = []

    def mantis(phase, dx=0, strike=0, summon=0.0, lay=0.0):
        w = h = 64
        cx, cy = 28 + dx, 32.5
        ink, body = Cv(w, h), Cv(w, h)
        s = math.sin(phase)
        for k, (ax, fx, reach) in enumerate(((cx + 4, 6, 15), (cx - 6, -8, 17))):   # walking legs
            off = round(s * 3) * (1 if k == 0 else -1)
            for side, o in ((-1, off), (1, -off)):
                thick_line(ink, [(ax, cy - 0.5 + side * 2), (ax + fx * 0.5, cy - 0.5 + side * reach * 0.75),
                                 (ax + fx + o, cy - 0.5 + side * reach)], ("M", "g", "j"))
        snap = 1.0 if strike in (2, 3) else 0.0
        for side in (-1, 1):                                      # raptorial forelegs: folded, raised wide, or snapped shut
            spread = 6 + summon * 9 - snap * 4
            elbow = (cx + 14 - summon * 3, cy - 0.5 + side * (spread + 3))
            tip = (cx + 22 + snap * 5 - summon * 2, cy - 0.5 + side * (spread - 2 - snap * 2))
            thick_line(ink, [(cx + 9, cy - 0.5 + side * 2), elbow, tip], ("g", "w", "M"))
            for j in range(3):                                    # spines along the forearm
                t = 0.3 + j * 0.25
                ink.px(elbow[0] + (tip[0] - elbow[0]) * t, elbow[1] + (tip[1] - elbow[1]) * t - side * 2, "c")
        body.blob(cx - 12 + lay * 2, cy, 13 - lay * 2, 6.5 + lay * 1.5, "M", "g", "j")   # abdomen (bunches up to lay)
        body.blob(cx - 9, cy, 11, 4.6, "m", "g", "M")             # folded wings
        body.blob(cx + 7, cy, 9, 2.8, "M", "m", "j")              # prothorax
        body.blob(cx + 18, cy, 3.6, 5.4, "m", "g", "M")           # head
        body.outline()
        body.line([(cx - 20, cy - 0.5), (cx - 1, cy - 0.5)], "M")
        for bx in (cx - 16, cx - 10, cx - 4):
            body.line([(bx, cy - 4), (bx, cy + 3)], "j")
        eye = "w" if summon > 0.5 else "y"
        body.blob(cx + 19, cy - 3.5, 1.6, 1.6, eye)
        body.blob(cx + 19, cy + 3.5, 1.6, 1.6, eye)
        crown = Cv(w, h)                                          # a queen: three gold points between the eyes
        crown.pxs([(cx + 15, cy - 2), (cx + 15, cy - 1), (cx + 15, cy), (cx + 15, cy + 1), (cx + 14, cy - 2), (cx + 13, cy - 0.5), (cx + 14, cy + 1)], "y")
        crown.outline()
        ink.paste(body)
        ink.paste(crown)
        for side in (-1, 1):
            ink.line([(cx + 20, cy - 0.5 + side * 2), (cx + 25, cy - 0.5 + side * 7), (cx + 30, cy - 0.5 + side * 8)], "k")
        return ink

    def mantis_extra(s, frame, cx, cy):
        frames = []
        for k in range(7):
            L = frame(0, summon=(0.3, 0.7, 1, 1, 1, 0.6, 0.2)[k])
            if 1 <= k <= 5:
                r = 8 + k * 4.5
                L["FX"].ring(cx - 4, cy, r, "g", 2 if k < 4 else 1, a=250 - k * 40, ry=r * 0.8)
                for j in range(6):
                    ang = math.radians(j * 60 + k * 20)
                    L["FX"].spr(SPARK_S, cx - 4 + math.cos(ang) * (r - 3) - 1, cy + math.sin(ang) * (r - 3) * 0.8 - 1)
            frames.append(flat(L))
        s.tag("Summon", frames, 100, repeat=1)

        # lays an egg sac behind her: steps forward, bunches up, and it is left on the path
        frames = []
        for k in range(7):
            L = frame(0, dx=(0, 1, 2, 3, 4, 4, 4)[k], lay=(0.3, 0.7, 1.0, 1.0, 0.6, 0.2, 0.0)[k])
            if k >= 2:
                grow = (0, 0, 1.4, 2.4, 3.2, 3.6, 3.6)[k]
                egg = Cv(s.w, s.h)
                egg.blob(cx - 28, cy, grow, grow * 0.9, "c", "w", "t")
                egg.outline()
                L["FX"].paste(egg)
                if k in (3, 4):
                    L["FX"].line([(cx - 25, cy - 0.5), (cx - 22 + (k - 3) * 2, cy - 0.5)], "c", a=200)   # a thread of silk still joining them
            frames.append(flat(L))
        s.tag("LayEgg", frames, 100, repeat=1)

        # warded: while enough egg sacs live, a ring of them circles her and nothing gets through
        frames = []
        for i in range(8):
            L = frame(2 * math.pi * i / 8)
            L["FX"].ring(cx - 4, cy, 27, "g", 1, a=210, ry=21, gaps=lambda d, i=i: int((d + i * 11.25) / 22.5) % 2 == 0)
            L["FX"].ring(cx - 4, cy, 29, "I", 1, a=110, ry=23)
            for j in range(3):
                ang = math.radians(j * 120 + i * 15)
                mote = Cv(s.w, s.h)
                mote.blob(cx - 4 + math.cos(ang) * 27, cy + math.sin(ang) * 21, 2.2, 2.0, "c", "w", "t")
                mote.outline()
                L["FX"].paste(mote)
            frames.append(flat(L))
        s.tag("Warded", frames, 130)
        frames = []
        for k in range(5):                                        # the ward failing
            L = frame(0, dx=(0, -1, -2, -1, 0)[k])
            if k == 0:
                L["Body"].im = base.tint(L["Body"].im, "w", 0.7)
            r = 27 + k * 2
            L["FX"].ring(cx - 4, cy, r, "g" if k < 2 else "M", 1, a=220 - k * 45, ry=r * 0.78, gaps=lambda d, k=k: int(d / (14 + k * 6)) % 2 == 0)
            for j in range(6):
                ang = math.radians(j * 60 + 20)
                L["FX"].px(cx - 4 + math.cos(ang) * (r + k * 2), cy + math.sin(ang) * (r * 0.78 + k * 2), "c", a=240 - k * 45)
            frames.append(flat(L))
        s.tag("WardBreak", frames, 90, repeat=1)

    out.append(roster.creature("EnemyMantisQueen", "Mantis queen (boss)", 64, mantis, (24, 11), splat="G", debris=("g", "M"),
                               walk_n=8, walk_ms=130, reach=32, extra=mantis_extra))

    def hornet(phase, dx=0, strike=0, curl=0.0, drop=0.0):
        w = h = 64
        cx, cy = 30 + dx, 26.5 + drop * 8                          # flies above its shadow; drop brings it down onto it
        ink, body = Cv(w, h), Cv(w, h)
        beat = int(round(phase / (2 * math.pi) * 8)) % 2
        for side in (-1, 1):
            wing = Cv(w, h)
            a = math.radians(180 - (30 if beat else 58)) * side
            for step in range(22):
                t = step / 21
                half = 1 + math.sin(t * math.pi) * 4.2
                wing.blob(cx + 4 + math.cos(a) * step, cy - 0.5 + math.sin(a) * step, half, half, "u", "c", "o", a=140 if beat else 185)
            wing.line([(cx + 4, cy - 0.5 + side * 2), (cx + 4 + math.cos(a) * 18, cy - 0.5 + math.sin(a) * 18)], "o", a=230)
            ink.paste(wing)
        for ax in (7, 3, -1):                                     # legs trailing
            ink.line([(cx + ax, cy - 2), (cx + ax - 3, cy - 7)], "k")
            ink.line([(cx + ax, cy + 1), (cx + ax - 3, cy + 6)], "k")
        tail = (cx - 22 + curl * 10, cy - 0.5)
        thick_line(ink, [(cx - 17, cy - 0.5), tail, (tail[0] - 4 + curl * 8, cy - 0.5)], ("k", "x", "k"))   # the sting
        body.blob(cx - 9 + curl * 3, cy, 10.5 - curl * 2, 6.6, "o", "u", "R")   # abdomen
        body.blob(cx + 4, cy, 5, 5, "R", "r", "v")                # thorax
        body.blob(cx + 13, cy, 4.4, 4.8, "o", "u", "R")           # head
        body.outline()
        for bx in (cx - 15, cx - 11, cx - 7, cx - 3):             # bands
            body.line([(bx + curl * 2, cy - 6), (bx + curl * 2, cy + 5)], "k")
            body.line([(bx + 1 + curl * 2, cy - 5), (bx + 1 + curl * 2, cy + 4)], "v")
        body.blob(cx + 14, cy - 3, 1.8, 1.6, "k")
        body.blob(cx + 14, cy + 3, 1.8, 1.6, "k")
        ink.paste(body)
        for side in (-1, 1):
            ink.line([(cx + 16, cy - 0.5 + side * 2), (cx + 22, cy - 0.5 + side * 6)], "k")
            ink.line([(cx + 17, cy - 0.5 + side), (cx + 19 + (2 if strike in (2, 3) else 0), cy - 0.5 + side * (1 if strike in (2, 3) else 2))], "c")
        return ink

    def hornet_extra(s, frame, cx, cy):
        frames = []
        for k in range(7):                                        # drops out of the air, stings, lifts off again
            d = (0.2, 0.6, 1.0, 1.0, 0.7, 0.3, 0.0)[k]
            L = frame(k * 0.8, drop=d, curl=(0, 0.3, 0.9, 1.0, 0.5, 0.1, 0)[k], shadow_scale=0.7 + d * 0.5)
            if k in (2, 3):
                L["FX"].ring(cx - 2, cy + 16, 8 + (k - 2) * 7, "c", 2, a=240 - (k - 2) * 90, ry=4 + (k - 2) * 3)
                L["FX"].spr(SPARK_S, cx + 4, cy + 12)
            frames.append(flat(L))
        s.tag("Dive", frames, 80, repeat=1)

        # marks a tower: hangs in the air, sting cocked, and throws a red sighting line ahead of it
        frames = []
        for k in range(6):
            L = frame(k * 0.8, dx=(0, -1, -1, -1, -1, 0)[k], curl=(0.2, 0.5, 0.7, 0.7, 0.7, 0.4)[k])
            if 1 <= k <= 4:
                L["FX"].pxs([(cx + 12, cy - 9), (cx + 12, cy - 3)], "w")                  # eyes flare
                for j in range(k):
                    L["FX"].ring(cx + 12, cy - 6, 9 + j * 5, "r", 1, a=240 - j * 50, gaps=lambda d: 28 < d < 332)
            frames.append(flat(L))
        s.tag("Mark", frames, 110, repeat=1)

        def carrying(L, i, sway=0):
            """A tower's mound slung under the hornet. Height shows as distance down the screen, so the
            load hangs between the body and its shadow, in plain view, with the tower's ant still on it."""
            mx, my = cx + 6 + sway, cy + 6
            load = Cv(s.w, s.h)
            load.blob(mx, my, 12, 8.5, "T", "t", "d")
            load.outline()
            load.ring(mx, my + 0.5, 8.5, "d", 1, ry=5.5)
            load.pxs([(mx - 9, my - 2), (mx + 8, my + 3), (mx - 6, my + 5), (mx + 4, my - 5)], "c")
            for gx in (mx - 11, mx + 10):                         # grass still rooted in it
                load.pxs([(gx, my + 1), (gx + 1, my), (gx + 1, my + 1)], "m")
            ant = Cv(s.w, s.h)                                    # the tower's ant, facing up, antennae going
            ant.blob(mx, my + 3, 2.4, 2.6, "a", "A", "z")
            ant.blob(mx, my - 0.5, 1.4, 1.4, "a", "A", "z")
            ant.blob(mx, my - 3.5, 2.2, 1.9, "a", "A", "z")
            ant.outline()
            wig = (0, 1, 2, 1)[i % 4]
            for side in (-1, 1):
                ant.line([(mx - 0.5 + side * 1.5, my - 5), (mx - 0.5 + side * (3 + wig), my - 7 - (wig % 2))], "k")
                ant.line([(mx - 0.5 + side * 2, my), (mx - 0.5 + side * 5, my - 1 + (wig - 1) * side)], "k")
            body = L["Body"].im
            for ax in (7, 3, -1):                                 # the hornet's legs reach down and grip the rim
                load.line([(cx - 2 + ax, cy - 5 + 4), (mx - 6 + (ax - 3) * 2.2, my - 5)], "k")
            load.paste(body)
            load.paste(ant)                                       # on top, so the wings never hide it
            L["Body"] = load
            return L

        frames = []
        for i in range(8):
            sway = (0, 0, 1, 1, 0, 0, -1, -1)[i]
            L = carrying(frame(2 * math.pi * i / 8, drop=0.1, shadow_scale=1.4), i, sway)
            if i % 2 == 0:                                        # crumbs of the mound falling away
                L["FX"].px(cx - 12 + (i * 5) % 19, cy + 21 + (i % 3), "d")
                L["FX"].px(cx - 6 + (i * 7) % 15, cy + 23, "T", a=200)
            frames.append(flat(L))
        s.tag("Carry", frames, 60)

        frames = []
        for k in range(6):                                        # shot off its mark: thrown back, wings splayed
            L = frame(0 if k % 2 else 0.8, dx=(-2, -5, -6, -4, -2, 0)[k], curl=0.0, drop=(0.1, 0.25, 0.3, 0.2, 0.1, 0)[k])
            if k == 0:
                L["Body"].im = base.tint(L["Body"].im, "w", 0.85)
            if k <= 3:
                for j in range(5):
                    ang = math.radians(-60 + j * 30)
                    d = 5 + k * 4
                    L["FX"].px(cx + 16 + math.cos(ang) * d, cy - 6 + math.sin(ang) * d, "y" if j % 2 else "w", a=255 - k * 50)
            if k == 1:
                L["FX"].spr(base.SPARK, cx + 14, cy - 9)
            frames.append(flat(L))
        s.tag("Flinch", frames, 75, repeat=1)

    out.append(roster.creature("EnemyHornet", "Hornet (boss, flies)", 64, hornet, (16, 6), shadow_dy=16, splat="R", debris=("o", "k"),
                               walk_n=8, walk_ms=45, reach=24, extra=hornet_extra))
    return out


# ================================================================ run
def main():
    elites, bosses = build_elites(), build_bosses()
    results = []
    for s in elites + bosses:
        results.append((s, export(s)))
        n = sum(len(f) for _, f, _, _ in s.tags)
        print(f"{s.group:8} {s.name:22} {s.w}x{s.h}  {n:2} frames  " + ", ".join(t[0] for t in s.tags))
    base.preview_sheet(results[len(elites):], os.path.join(HERE, "NewBossPreview.png"), scale=2)
    # elites: the first walk frame of each, beside nothing else, on grass
    from PIL import Image
    sheet = Image.new("RGBA", (12 * 100, 110), (62, 92, 46, 255))
    for i, (s, (rows, comps)) in enumerate(results[:len(elites)]):
        im = comps[0].resize((s.w * 2, s.h * 2), Image.NEAREST)
        sheet.alpha_composite(im, (i * 100 + (100 - im.width) // 2, (110 - im.height) // 2))
    sheet.save(os.path.join(HERE, "ElitePreview.png"))
    total = sum(sum(len(f) for _, f, _, _ in s.tags) for s, _ in results)
    print(f"{len(results)} sprites, {total} frames written and read back OK")


if __name__ == "__main__":
    main()
