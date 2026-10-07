"""Hold the Hill tier sheets: tier 2 and tier 3 body art for all 14 towers.

  Towers/Tower<Name>T2, Tower<Name>T3    Idle / Attack / Upgrade, same canvas, pivot and timing as
                                         Tower<Name>, so a tier swaps in without moving anything.

    python build_tier_sheets.py

Nothing is redrawn by hand: each tower's own drawing code runs again with the mound and the ant
dressed for the tier.
  Tier 2  a ring of fitted stones round the mound, a tan banner, a stone plate on the ant's back
          and a pale band on its gaster.
  Tier 3  gold-capped stones, two red banners, a gold back plate, band and head crest, and a
          glint that travels round the mound while idle.
"""
import math
import os

from PIL import Image

import build_anim_sheets as base
import build_weapon_sheets as wpn
from build_anim_sheets import Cv, CX, SPARK_S, TW, export, tower_sprite

HERE = os.path.dirname(os.path.abspath(__file__))

# stone (base, highlight, shade), banner cloth, cloth shade, pole tip, plate (base, highlight), band
TIERS = {
    2: dict(stone=("s", "c", "S"), count=9, cloth="t", cloth_d="T", tip="S", plate=("s", "c"), band="c"),
    3: dict(stone=("y", "w", "Y"), count=11, cloth="r", cloth_d="R", tip="y", plate=("y", "w"), band="y"),
}

_plain_base = base.tower_base
_plain_ant = base.ant
_tier = 0


def banner(deco, x, top, cloth, cloth_d, tip, flip=False):
    deco.line([(x, top), (x, top + 9)], "D")
    d = 1 if flip else -1
    rows = [(1, 5), (1, 5), (2, 5)]            # cloth columns out from the pole, three rows
    for r, (a, b) in enumerate(rows):
        for k in range(a, b + 1):
            deco.px(x + d * k, top + 1 + r, cloth)
    deco.px(x + d * 5, top + 3, cloth_d)
    deco.px(x + d * 1, top + 3, cloth_d)
    deco.px(x, top - 1, tip)


def tiered_base(L, mound=("T", "t", "d")):
    _plain_base(L, mound)
    t = TIERS[_tier]
    deco = Cv(TW, TW)
    for j in range(t["count"]):                 # stones round the front and sides of the rim
        ang = math.radians(-18 + j * 216 / (t["count"] - 1))
        deco.blob(CX + math.cos(ang) * 13.2, 19.5 + math.sin(ang) * 9.8, 1.6, 1.3, *t["stone"])
    banner(deco, 27, 4, t["cloth"], t["cloth_d"], t["tip"])
    if _tier == 3:
        banner(deco, 4, 5, t["cloth"], t["cloth_d"], t["tip"], flip=True)
    deco.outline()
    L["Base"].paste(deco)


def tiered_ant(L, cx=CX, cy=16, col=("a", "A", "z"), head="ant", bob=0, abd=0.0, head_dy=0, head_dx=0,
               tw=(0, 0), legs=0, mand=0, antennae=True, ant_tips=None, lift=0, layer="Body"):
    _plain_ant(L, cx, cy, col, head, bob, abd, head_dy, head_dx, tw, legs, mand, antennae, ant_tips, lift, layer)
    t = TIERS[_tier]
    y0 = cy + bob - lift
    hy = y0 - 7 + head_dy
    gear = L[layer]
    plate, shine = t["plate"]
    # back plate over the thorax
    gear.pxs([(cx - 1, y0 - 2), (cx, y0 - 2), (cx - 1, y0 - 1), (cx, y0 - 1), (cx - 1, y0), (cx, y0)], plate)
    gear.px(cx - 1, y0 - 2, shine)
    # band round the gaster, one row above the body's own dark band
    gy = y0 + 5 + round(abd)
    gear.line([(cx - 3, gy), (cx + 2, gy)], t["band"])
    if _tier == 3:
        gear.px(cx - 3, gy, "w")
        # crest on the brow
        gear.pxs([(cx - 1 + head_dx, hy - 1), (cx + head_dx, hy - 1)], "y")
        gear.px(cx - 1 + head_dx, hy - 2, "w")
        gear.px(cx + head_dx, hy - 2, "y")


def dressed(draw):
    """The tower's own draw, plus the tier 3 glint travelling round the mound while idle."""
    def inner(L, tag, i, n):
        draw(L, tag, i, n)
        if _tier == 3 and tag == "Idle":
            ang = math.radians(20 + (i % 4) * 45)
            L["FX"].spr(SPARK_S, CX + math.cos(ang) * 13.2 - 1.5, 19.5 + math.sin(ang) * 9.8 - 1.5)
    return inner


TOWERS = [
    ("TowerLinear", "Spitter", base.draw_linear, {}),
    ("TowerHoming", "Seeker", base.draw_homing, {}),
    ("TowerMortar", "Bombardier", base.draw_mortar, dict(attack_ms=80)),
    ("TowerRicochet", "Slinger", base.draw_ricochet, {}),
    ("TowerChain", "Storm Ant", base.draw_chain, dict(attack_ms=60)),
    ("TowerBeam", "Dewdrop Lens", base.draw_beam, {}),
    ("TowerOrbit", "Swarm Nest", base.draw_orbit, dict(idle_frames=6, idle_ms=110)),
    ("TowerFrostAura", "Frost Ant", base.draw_frost, dict(attack_ms=60)),
    ("TowerKnockback", "Kicker", base.draw_knockback, dict(attack_ms=75)),
    ("TowerMineLayer", "Sapper", base.draw_mine_layer, dict(attack_ms=90)),
    ("TowerWorker", "Worker", wpn.draw_worker, {}),
    ("TowerSoldier", "Soldier", wpn.draw_soldier, dict(attack_ms=60)),
    ("TowerMajor", "Major", wpn.draw_major, dict(attack_ms=80)),
    ("TowerNurse", "Nurse", wpn.draw_nurse, dict(attack_ms=90)),
]


def build(tier):
    """tier 1 returns the plain towers (for the comparison sheet only)."""
    global _tier
    _tier = tier
    if tier > 1:
        base.tower_base, base.ant, wpn.ant = tiered_base, tiered_ant, tiered_ant
    try:
        suffix = f"T{tier}" if tier > 1 else ""
        return [tower_sprite(name + suffix, f"{caste}, tier {tier}", dressed(draw), **kw) for name, caste, draw, kw in TOWERS]
    finally:
        base.tower_base, base.ant, wpn.ant = _plain_base, _plain_ant, _plain_ant


def frame_image(sprite, tag, index):
    layers = dict((t[0], t[1]) for t in sprite.tags)[tag][index]
    im = Image.new("RGBA", (sprite.w, sprite.h), (0, 0, 0, 0))
    for name in sprite.layers:
        im.alpha_composite(layers[name])
    return im


def comparison(by_tier, path, scale=4):
    """One row per tower: tier 1, 2 and 3, idle then mid-attack."""
    from PIL import ImageDraw, ImageFont
    font = ImageFont.load_default()
    cell = TW * scale + 8
    rows = len(TOWERS)
    out = Image.new("RGBA", (120 + cell * 6 + 24, rows * cell + 24), (62, 92, 46, 255))
    d = ImageDraw.Draw(out)
    for col, label in enumerate(("T1 idle", "T2 idle", "T3 idle", "T1 attack", "T2 attack", "T3 attack")):
        d.text((120 + col * cell + (24 if col >= 3 else 0) + 30, 4), label, fill=(255, 210, 58, 255), font=font)
    for r, (name, caste, _, _) in enumerate(TOWERS):
        d.text((6, 24 + r * cell + cell // 2 - 6), f"{name[5:]} ({caste})", fill=(255, 241, 214, 255), font=font)
        for col in range(6):
            tier, tag, index = col % 3 + 1, ("Idle", "Attack")[col // 3], (0, 2)[col // 3]
            im = frame_image(by_tier[tier][r], tag, index)
            im = im.resize((TW * scale, TW * scale), Image.NEAREST)
            out.alpha_composite(im, (120 + col * cell + (24 if col >= 3 else 0), 20 + r * cell))
    out.save(path)


def main():
    by_tier = {tier: build(tier) for tier in (1, 2, 3)}
    count = frames = 0
    for tier in (2, 3):
        for s in by_tier[tier]:
            export(s)
            n = sum(len(f) for _, f, _, _ in s.tags)
            count, frames = count + 1, frames + n
            print(f"{s.group:8} {s.name:20} {n:3} frames  " + ", ".join(t[0] for t in s.tags))
    comparison(by_tier, os.path.join(HERE, "TowerTierPreview.png"))
    print(f"{count} sprites, {frames} frames written and read back OK")


if __name__ == "__main__":
    main()
