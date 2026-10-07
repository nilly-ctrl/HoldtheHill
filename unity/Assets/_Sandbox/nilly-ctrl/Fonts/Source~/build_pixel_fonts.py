"""Hold the Hill pixel fonts: baked colour styles, plain TTFs, and their preview sheets.

Three hand-drawn alphabets (glyphs_body.py 5x7 with lowercase, glyphs_display.py 8x12 capitals,
glyphs_tiny.py 3x5 capitals) and the key and gamepad prompts (glyphs_buttons.py).

Writes:
  ../Atlases/<Style>.png + .json          one baked atlas per style in STYLES (multi-colour, transparent);
                                          the JSON holds glyph rects, advances and kerning pairs and is
                                          read by DamageNumbers/PixelFontStyle.cs
  ../Atlases/Themes/<Theme>/<Style>.*     the same styles painted from each art theme's palette
                                          (palettes come from Animations/Source~/build_theme_sheets.py)
  ../TTF/HoldTheHillPixel-Regular.ttf / -Bold.ttf   body font: capitals, lowercase, accents, symbols, prompts
  ../TTF/HoldTheHillDisplay-Regular.ttf             display font, capitals only (pixel-exact at size 20)
  ../TTF/HoldTheHillTiny-Regular.ttf                tiny font, capitals only (pixel-exact at size 10)
  ../../DamageNumbers/PixelGlyphs.cs      constants for the symbols and prompts
  ./FontSheet.png          every base style with sample text
  ./Specimen.png           every glyph of the four TTFs, kerning on and off, the prompt table
  ./SpecimenStyles.png     every glyph of every base style
  ./Themes/Theme<Name>.png and ./ThemeCompare.png    the themed sets

    python build_pixel_fonts.py               everything
    python build_pixel_fonts.py --no-themes   skip the themed atlases
    python build_pixel_fonts.py --check       also render the TTF files and compare them with the drawings
"""
import json
import os
import sys

from PIL import Image

import glyphs_body as body
import glyphs_buttons as buttons
import glyphs_display as display
import glyphs_faces as faces
import glyphs_tiny as tiny

HERE = os.path.dirname(os.path.abspath(__file__))
ATLAS_DIR = os.path.normpath(os.path.join(HERE, "..", "Atlases"))
TTF_DIR = os.path.normpath(os.path.join(HERE, "..", "TTF"))
THEME_SHEETS = os.path.join(HERE, "Themes")
ANIM_SOURCE = os.path.normpath(os.path.join(HERE, "..", "..", "Animations", "Source~"))
GLYPHS_CS = os.path.normpath(os.path.join(HERE, "..", "..", "DamageNumbers", "PixelGlyphs.cs"))

# gap: empty columns between letters in the plain fonts. kern_cap: the most a pair may close up.
FACES = {
    "body": dict(glyphs=body.CAPS, chars=list(body.CAPS), bold=body.BOLD, gap=1, kern_cap=1),
    "display": dict(glyphs=display.GLYPHS, chars=display.CHARS, bold={}, gap=2, kern_cap=2),
    "tiny": dict(glyphs=tiny.GLYPHS, chars=tiny.CHARS, bold={}, gap=1, kern_cap=1),
}
# Kerning only ever applies between these; digits keep their columns and symbols their air.
for _key, _f in faces.FACES.items():
    if not _f["lower"] and "×" not in _f["glyphs"]:
        _f["glyphs"]["×"] = _f["glyphs"]["x"]
    FACES[_key] = dict(glyphs=_f["glyphs"], chars=list(_f["glyphs"]), bold={}, gap=_f["gap"],
                       kern_cap=_f["kern_cap"], cap=_f["cap"], desc=_f["desc"])
KERN_PUNCT = ".,:;!?'\"-/()"


def hexc(c, a=255):
    c = c.lstrip("#")
    return (int(c[0:2], 16), int(c[2:4], 16), int(c[4:6], 16), a)


def mix(a, b, t):
    ca, cb = hexc(a), hexc(b)
    return "#%02x%02x%02x" % tuple(round(ca[i] + (cb[i] - ca[i]) * t) for i in range(3))


def lum(c):
    r, g, b, _ = hexc(c)
    return (0.299 * r + 0.587 * g + 0.114 * b) / 255


# ---------------------------------------------------------------- styles
# fill: top-to-bottom colour bands. hi: top-edge highlight. lo: bottom-edge shade (bevel: how deep).
# outline thickness in output pixels; shadow (dx, dy, colour); slant: rows per 1 px shift;
# gap: columns added between letters (default 1, or 2 when scaled).
STYLES = [
    dict(name="DamageNormal", use="Physical hits, MISS / BLOCK text", bold=False, scale=1,
         fill=["#ffffff", "#f1e9da", "#d9cdb8"], outline="#1b110b", shadow=(0, 1, "#1b110b")),
    dict(name="DamageCrit", use="Critical hits (spawn at 1x, punch to 2x)", bold=True, scale=2, slant=4,
         fill=["#fffbd0", "#ffe14a", "#ffb03a", "#ff7a2e", "#e2412f"],
         outline="#2a0808", thick=2, shadow=(2, 2, "#2a0808")),
    dict(name="DamageFire", use="Fire and burn ticks", bold=True, scale=1,
         fill=["#fff2a0", "#ffb03a", "#ff6a2a", "#d6361c"], hi="#fff8d0", outline="#3a0c0f", shadow=(0, 1, "#3a0c0f")),
    dict(name="DamagePoison", use="Poison and acid ticks", bold=True, scale=1,
         fill=["#e8ffa8", "#9fe04a", "#62b032", "#3a7a22"], outline="#0f2208", shadow=(0, 1, "#0f2208")),
    dict(name="DamageLightning", use="Chain lightning and shock", bold=True, scale=1, slant=3,
         fill=["#ffffff", "#fffbe0", "#ffe14a", "#ffd23a"], hi="#ffffff", outline="#17245a", shadow=(1, 1, "#17245a")),
    dict(name="DamageMagic", use="Magic damage", bold=True, scale=1,
         fill=["#fbf4ff", "#dcc6ff", "#b48cf6", "#8a5ce6"], hi="#ffffff", outline="#21134d", shadow=(0, 1, "#21134d")),
    dict(name="DamageTrue", use="True (armour-piercing) damage", bold=False, scale=1,
         fill=["#ffffff", "#d8f6ff", "#9fe7ff", "#5aa9d8"], hi="#ffffff", outline="#0b2236", shadow=(1, 1, "#0b2236")),
    dict(name="Heal", use="Healing on ants and the Queen (+N)", bold=True, scale=1,
         fill=["#fff0f4", "#ffc2d0", "#ff8fa2", "#f0607a"], hi="#ffffff", outline="#4a1020", shadow=(0, 1, "#4a1020")),
    dict(name="Resource", use="Food gained / spent, costs, rewards", bold=True, scale=1,
         fill=["#fff6c8", "#f6d88a", "#ecc477", "#c9963f"], hi="#fffbe6", lo="#8a5f24", outline="#3a2416", shadow=(0, 1, "#3a2416")),
    dict(name="Hud", use="HUD counters: wave, timer, Queen HP, stats", bold=False, scale=1,
         fill=["#fff1d6"], outline="#1b110b", shadow=(1, 1, "#00000080")),
    # ---- display alphabet
    dict(name="Heading", face="display", use="Headings and banner sub-lines", bold=False, scale=1, gap=2,
         fill=["#ffffff", "#fff1d6", "#fff1d6", "#ecd9b4"], outline="#1b110b", shadow=(0, 2, "#1b110b")),
    dict(name="Combo", face="display", use="Combo counter and its label (x12, 12 HITS)", bold=False, scale=1,
         slant=4, gap=2, fill=["#fff6c8", "#ffd23a", "#ff8a3d", "#f0447a", "#b0308a"], hi="#ffffff",
         outline="#2a0a24", thick=2, shadow=(2, 2, "#2a0a24")),
    dict(name="Banner", face="display", use="Wave banners (WAVE 3, WAVE CLEAR)", bold=False, scale=2, gap=2,
         fill=["#fffbe6", "#fff1d6", "#f6d88a", "#ecc477", "#c9963f"], hi="#ffffff", lo="#8a5f24", bevel=2,
         outline="#2a1708", thick=2, shadow=(0, 3, "#1b110b")),
    dict(name="BannerVictory", face="display", use="VICTORY and other good news", bold=False, scale=2, gap=2,
         fill=["#ffffff", "#fff6a0", "#ffd23a", "#ffb03a", "#e08a1a"], hi="#ffffff", lo="#a85a10", bevel=2,
         outline="#3a1a06", thick=2, shadow=(0, 3, "#3a1a06")),
    dict(name="BannerDefeat", face="display", use="DEFEAT and other bad news", bold=False, scale=2, gap=2,
         fill=["#ffb0a0", "#f0604a", "#e2412f", "#b02a26", "#8e2124"], hi="#ffd0c0", lo="#5a1014", bevel=2,
         outline="#200808", thick=2, shadow=(0, 3, "#200808")),
    # ---- tiny alphabet
    dict(name="TinyLabel", face="tiny", use="Small labels: bar values, tooltips, slot numbers", bold=False,
         scale=1, gap=0, fill=["#fff1d6"], outline="#1b110b", shadow=(0, 0, "#1b110b")),
    dict(name="TinyCost", face="tiny", use="Costs on the build bar", bold=False, scale=1, gap=0,
         fill=["#fff6c8", "#f6d88a", "#ecc477"], outline="#3a2416", shadow=(0, 0, "#3a2416")),
]


def theme_styles(base, theme, paint_only=False):
    """STYLES repainted from one art theme's palette. Shapes, sizes and names stay the same."""
    p = dict(base)
    p.update(theme["pal"])
    ink = p["k"]
    white = "#ffffff"

    def light(c, t):
        return mix(c, white, t)

    def dark(c, t=0.7):
        d = mix(c, ink, t)
        return d if lum(d) < 0.16 else mix(d, "#000000", 0.5)

    a0, a1 = theme["accents"][0], theme["accents"][1]
    c, y, u, o, r, big_r, f = p["c"], p["y"], p["u"], p["o"], p["r"], p["R"], p["f"]
    paint = {
        "DamageNormal": dict(fill=[light(c, 0.7), c, mix(c, p["s"], 0.5)], outline=ink),
        "DamageCrit": dict(fill=[light(y, 0.7), y, u, o, r], outline=dark(big_r)),
        "DamageFire": dict(fill=[light(y, 0.5), u, mix(o, r, 0.5), mix(r, big_r, 0.3)], hi=light(y, 0.8),
                           outline=dark(big_r)),
        "DamagePoison": dict(fill=[light(p["g"], 0.5), p["g"], mix(p["g"], p["G"], 0.6), p["G"]], outline=dark(p["j"])),
        "DamageLightning": dict(fill=[white, light(y, 0.8), y, u], hi=white, outline=dark(p["n"], 0.55)),
        "DamageMagic": dict(fill=[light(p["p"], 0.6), p["p"], mix(p["p"], p["P"], 0.6), p["P"]], hi=white,
                            outline=dark(p["V"], 0.6)),
        "DamageTrue": dict(fill=[white, p["b"], mix(p["b"], p["B"], 0.5), p["B"]], hi=white, outline=dark(p["n"])),
        "Heal": dict(fill=[light(f, 0.85), light(f, 0.5), light(f, 0.15), mix(f, r, 0.35)], hi=white,
                     outline=dark(big_r, 0.75)),
        "Resource": dict(fill=[light(y, 0.7), light(y, 0.3), mix(y, u, 0.5), mix(u, p["Y"], 0.5)], hi=light(y, 0.85),
                         lo=p["Y"], outline=dark(p["Y"], 0.75)),
        "Hud": dict(fill=[c], outline=ink),
        "Heading": dict(fill=[light(c, 0.7), c, c, mix(c, p["s"], 0.4)], outline=ink),
        "Combo": dict(fill=[white, a1[1], a1[0], mix(a1[0], a1[2], 0.5), a1[2]], hi=white, outline=dark(a1[2])),
        "Banner": dict(fill=[white, a0[1], light(a0[0], 0.3), a0[0], mix(a0[0], a0[2], 0.5)], hi=white, lo=a0[2],
                       outline=dark(a0[2])),
        "BannerVictory": dict(fill=[white, light(y, 0.6), y, u, mix(u, p["Y"], 0.5)], hi=white, lo=p["Y"],
                              outline=dark(p["Y"], 0.8)),
        "BannerDefeat": dict(fill=[light(r, 0.6), light(r, 0.25), r, mix(r, big_r, 0.5), big_r], hi=light(r, 0.75),
                             lo=dark(big_r, 0.3), outline=dark(big_r, 0.8)),
        "TinyLabel": dict(fill=[c], outline=ink),
        "TinyCost": dict(fill=[light(y, 0.7), light(y, 0.3), mix(y, u, 0.5)], outline=dark(p["Y"], 0.75)),
    }
    if paint_only:
        return paint
    out = []
    for st in STYLES:
        new = {k: v for k, v in st.items() if k not in ("fill", "hi", "lo", "outline")}
        new.update(paint[st["name"]])
        sdx, sdy, scol = st["shadow"]
        new["shadow"] = (sdx, sdy, scol if len(scol) > 7 else new["outline"])
        out.append(new)
    return out


# ---------------------------------------------------------------- glyph masks
def rows_to_mask(rows):
    return [[c == "#" for c in r] for r in rows]


def smear(mask):
    """Bold: every pixel also fills the one to its right."""
    w = len(mask[0])
    return [[(row[x] if x < w else False) or (x > 0 and row[x - 1]) for x in range(w + 1)] for row in mask]


def weighted(glyphs, bold_forms, ch, bold):
    """A glyph's unscaled mask at regular or bold weight."""
    if bold and ch in bold_forms:
        return rows_to_mask(bold_forms[ch])
    m = rows_to_mask(glyphs[ch])
    return smear(m) if bold and ch != " " else m


def face_mask(face, ch, bold):
    """A glyph's mask padded to the face's full height, so every glyph shares one baseline."""
    m = weighted(face["glyphs"], face["bold"], ch, bold)
    full = face.get("cap", len(m)) + face.get("desc", 0)
    return m + [[False] * len(m[0]) for _ in range(full - len(m))]


def mask_of(face, ch, bold, scale, slant):
    m = face_mask(face, ch, bold)
    w, h = len(m[0]), len(m)
    if scale > 1:
        m = [[m[y // scale][x // scale] for x in range(w * scale)] for y in range(h * scale)]
        w, h = w * scale, h * scale
    if slant:
        extra = (h - 1) // slant
        out = []
        for y in range(h):
            shift = (h - 1 - y) // slant
            out.append([False] * shift + m[y] + [False] * (extra - shift))
        m, w = out, w + extra
    return m, w, h


# ---------------------------------------------------------------- kerning
def kerning_pairs(masks, chars, gap, cap):
    """Pairs that can close up without touching, as {(first, second): negative pixels}.

    masks is {char: (mask, top)}. Two letters close up while every row of the first stays more
    than `gap` away from the same row and the rows above and below it in the second.
    """
    edge = {}
    for ch in chars:
        m, top = masks[ch]
        rows = {}
        for i, row in enumerate(m):
            if any(row):
                rows[i - top] = (row.index(True), row[::-1].index(True))   # blank columns left and right
        edge[ch] = rows
    pairs = {}
    for a in chars:
        for b in chars:
            best = None
            for y, (_, right) in edge[a].items():
                for yy in (y - 1, y, y + 1):
                    if yy in edge[b]:
                        d = right + gap + edge[b][yy][0]
                        best = d if best is None else min(best, d)
            if best is not None and best > gap:
                pairs[(a, b)] = -min(cap, best - gap)
    return pairs


def kern_chars(chars):
    return [c for c in chars if c.isalpha() or c in KERN_PUNCT]


_face_kern = {}


def face_kerning(face_name, bold):
    """Kerning for a baked face at 1x, cached."""
    key = (face_name, bold)
    if key not in _face_kern:
        face = FACES[face_name]
        masks = {ch: (face_mask(face, ch, bold), face.get("cap", len(face["glyphs"][ch])))
                 for ch in face["chars"]}
        chars = [c for c in kern_chars(face["chars"]) if c not in "x×"]
        _face_kern[key] = kerning_pairs(masks, chars, face["gap"], face["kern_cap"])
    return _face_kern[key]


# ---------------------------------------------------------------- glyph baking
def bake(ch, st):
    face = FACES[st.get("face", "body")]
    scale, thick = st["scale"], st.get("thick", 1)
    m, w, h = mask_of(face, ch, st["bold"], scale, st.get("slant"))
    sdx, sdy, scol = st["shadow"]
    glow = st.get("glow")              # (colour, alpha 0-255): a soft ring two pixels past the outline
    pad = thick + (2 if glow else 0)
    pad_l, pad_t = pad, pad
    W, H = w + pad * 2 + sdx, h + pad * 2 + sdy
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    p = im.load()

    def on(x, y):
        return 0 <= y < h and 0 <= x < w and m[y][x]

    # outline: square dilation of the glyph by `thick`
    ol = [[False] * W for _ in range(H)]
    for y in range(h):
        for x in range(w):
            if m[y][x]:
                for dy in range(-thick, thick + 1):
                    for dx in range(-thick, thick + 1):
                        if thick == 1 and dx and dy:
                            continue  # 1px outline: plus-shaped, keeps corners crisp
                        ol[y + pad_t + dy][x + pad_l + dx] = True
    if glow:
        reach = thick + 2
        for y in range(h):
            for x in range(w):
                if m[y][x]:
                    for dy in range(-reach, reach + 1):
                        for dx in range(-reach, reach + 1):
                            far = abs(dx) + abs(dy)
                            if far <= reach + 1:
                                a = glow[1] if far <= reach - 1 else glow[1] // 2
                                px_, py_ = x + pad_l + dx, y + pad_t + dy
                                if p[px_, py_][3] < a:
                                    p[px_, py_] = hexc(glow[0], a)
    sh = hexc(scol[:7], int(scol[7:9], 16) if len(scol) > 7 else 255)
    for y in range(H):
        for x in range(W):
            if ol[y][x] and 0 <= y + sdy < H and 0 <= x + sdx < W and (sdx or sdy):
                p[x + sdx, y + sdy] = sh
    olc = hexc(st["outline"])
    for y in range(H):
        for x in range(W):
            if ol[y][x]:
                p[x, y] = olc
    if st.get("hollow"):
        # outline only: the letter itself is left clear
        for y in range(h):
            for x in range(w):
                if m[y][x]:
                    p[x + pad_l, y + pad_t] = (0, 0, 0, 0)
    bands = st.get("fill") or ["#000000"]
    stripes = st.get("stripes")        # diagonal stripes this many pixels wide, cycling through the fill
    bevel = st.get("bevel", 1)
    for y in range(h):
        band = hexc(bands[min(len(bands) - 1, y * len(bands) // h)])
        for x in range(w):
            if not m[y][x] or st.get("hollow"):
                continue
            c = hexc(bands[((x + y) // stripes) % len(bands)]) if stripes else band
            if st.get("hi") and any(not on(x, y - d) for d in range(1, bevel + 1)):
                c = hexc(st["hi"])
            elif st.get("lo") and any(not on(x, y + d) for d in range(1, bevel + 1)):
                c = hexc(st["lo"])
            p[x + pad_l, y + pad_t] = c
    gap = st.get("gap", 1 if scale == 1 else 2)
    if ch != " ":
        advance = w + thick + gap
    else:
        # the body's space is its historical width; the other alphabets get a full word space
        advance = w + 1 if st.get("face", "body") == "body" else w + thick + gap
    return im, advance


# ---------------------------------------------------------------- atlases
class Baked:
    """One baked style: glyph images, the cell height and the kerning between them."""

    def __init__(self, st, glyphs, cell_h, kern):
        self.st, self.glyphs, self.cell_h, self.kern = st, glyphs, cell_h, kern

    def render(self, text, kern=True):
        widest = max(im.size[0] for im, _ in self.glyphs.values())
        text = [c for c in text if c in self.glyphs]
        out = Image.new("RGBA", (sum(self.glyphs[c][1] for c in text) + widest, self.cell_h), (0, 0, 0, 0))
        x, prev = 0, None
        for c in text:
            im, adv = self.glyphs[c]
            if kern and prev is not None:
                x += self.kern.get((prev, c), 0)
            out.alpha_composite(im, (x, self.cell_h - im.size[1]))
            x += adv
            prev = c
        return out.crop(out.getbbox())


def build_style(st, out_dir, theme=None, write=True):
    face_name = st.get("face", "body")
    chars = FACES[face_name]["chars"]
    glyphs = {ch: bake(ch, st) for ch in chars}
    kern = {pair: k * st["scale"] for pair, k in face_kerning(face_name, st["bold"]).items()}
    cell_h = max(im.size[1] for im, _ in glyphs.values())
    baked = Baked(st, glyphs, cell_h, kern)
    if not write:
        return baked
    cols = 16
    rows = [chars[i:i + cols] for i in range(0, len(chars), cols)]
    widths = [sum(glyphs[c][0].size[0] + 1 for c in r) + 1 for r in rows]
    atlas_w = 1 << (max(widths) - 1).bit_length()
    atlas_h = 1 << (len(rows) * (cell_h + 1) + 1 - 1).bit_length()
    atlas = Image.new("RGBA", (atlas_w, atlas_h), (0, 0, 0, 0))
    meta = {"style": st["name"], "use": st["use"], "lineHeight": cell_h, "baseline": cell_h - st.get("thick", 1)
            - (2 if st.get("glow") else 0) - st["shadow"][1] - FACES[face_name].get("desc", 0) * st["scale"], "atlasWidth": atlas_w, "atlasHeight": atlas_h, "glyphs": []}
    y = 1
    for r in rows:
        x = 1
        for ch in r:
            im, adv = glyphs[ch]
            atlas.alpha_composite(im, (x, y))
            meta["glyphs"].append({"char": ch, "code": ord(ch), "x": x, "y": y, "w": im.size[0], "h": im.size[1],
                                   "advance": adv})
            x += im.size[0] + 1
        y += cell_h + 1
    # Added after the glyphs so older readers of this file are unaffected.
    meta["face"] = face_name
    if theme:
        meta["theme"] = theme
    meta["digitAdvance"] = max(glyphs[d][1] for d in "0123456789")
    meta["kerning"] = [{"first": ord(a), "second": ord(b), "amount": k} for (a, b), k in sorted(kern.items())]
    os.makedirs(out_dir, exist_ok=True)
    atlas.save(os.path.join(out_dir, st["name"] + ".png"))
    with open(os.path.join(out_dir, st["name"] + ".json"), "w", encoding="utf-8") as f:
        json.dump(meta, f, indent=1, ensure_ascii=False)
    return baked


SAMPLES = {
    "DamageNormal": ["128", "-42", "MISS"],
    "DamageCrit": ["CRIT!", "9999"],
    "DamageFire": ["37", "-12", "BURN"],
    "DamagePoison": ["18", "-6", "x3"],
    "DamageLightning": ["255", "-64", "ZAP!"],
    "DamageMagic": ["160", "-33"],
    "DamageTrue": ["500", "-75"],
    "Heal": ["+25", "+120"],
    "Resource": ["+40", "-120", "1.5K"],
    "Hud": ["WAVE 3/10", "02:45", "HP 85%"],
    "Heading": ["BOSS INCOMING", "PAUSED"],
    "Combo": ["x12", "24 HITS!"],
    "Banner": ["WAVE 3", "WAVE CLEAR"],
    "BannerVictory": ["VICTORY!"],
    "BannerDefeat": ["DEFEAT"],
    "TinyLabel": ["HP 120/200", "LV 3", "x2"],
    "TinyCost": ["125", "1.5K", "+40"],
}
COMPARE = [("DamageNormal", "128"), ("DamageCrit", "99!"), ("DamageFire", "37"), ("DamagePoison", "18"),
           ("DamageLightning", "64"), ("DamageMagic", "33"), ("DamageTrue", "75"), ("Heal", "+25"),
           ("Resource", "+40"), ("Hud", "WAVE 3"), ("TinyLabel", "HP 85%"), ("TinyCost", "125"),
           ("Heading", "PAUSED"), ("Combo", "x12")]
COMPARE_BIG = [("Banner", "WAVE 3"), ("BannerVictory", "VICTORY"), ("BannerDefeat", "DEFEAT")]


def zoomed(im, z):
    return im.resize((im.size[0] * z, im.size[1] * z), Image.NEAREST)


def sample_sheet(baked, path, label_font, backdrop="#4a3a2e", title=None, styles=None, samples=None):
    """One row per style: its name, then its sample text."""
    gap, x0 = 16, 300
    lines = []
    if title:
        lines.append((None, [zoomed(label_font.render(title.upper()), 4)]))
    for st in styles or STYLES:
        b = baked[st["name"]]
        z = 4 if b.cell_h < 14 else 3 if b.cell_h < 24 else 2
        lines.append((label_font.render(st["name"].upper()),
                      [zoomed(b.render(t), z) for t in (samples or SAMPLES)[st["name"]]]))
    width = max(x0 + sum(p.size[0] + 48 for p in parts) for _, parts in lines) + 16
    height = sum(max(p.size[1] for p in parts) + gap for _, parts in lines) + gap
    sheet = Image.new("RGBA", (width, height), hexc(backdrop))
    y = gap
    for label, parts in lines:
        lh = max(p.size[1] for p in parts)
        x = 16
        if label is not None:
            big = zoomed(label, 2)
            sheet.alpha_composite(big, (16, y + (lh - big.size[1]) // 2))
            x = x0
        for part in parts:
            sheet.alpha_composite(part, (x, y + lh - part.size[1]))
            x += part.size[0] + 48
        y += lh + gap
    sheet.save(path)


def style_specimen(baked, path, label_font):
    """Every glyph of every base style, laid out as it is in the atlas."""
    blocks = []
    for st in STYLES:
        b = baked[st["name"]]
        chars = FACES[st.get("face", "body")]["chars"]
        per_row = 26
        rows = ["".join(chars[i:i + per_row]) for i in range(0, len(chars), per_row)]
        z = 2 if b.cell_h < 24 else 1
        blocks.append((zoomed(label_font.render(st["name"].upper() + ": " + st["use"].upper().replace(";", ",")), 2),
                       [zoomed(spaced(b, r), z) for r in rows]))
    width = max(max(label.size[0], max(r.size[0] for r in rows)) for label, rows in blocks) + 32
    height = sum(label.size[1] + 8 + sum(r.size[1] + 6 for r in rows) + 18 for label, rows in blocks) + 16
    sheet = Image.new("RGBA", (width, height), hexc("#4a3a2e"))
    y = 16
    for label, rows in blocks:
        sheet.alpha_composite(label, (16, y))
        y += label.size[1] + 8
        for r in rows:
            sheet.alpha_composite(r, (16, y))
            y += r.size[1] + 6
        y += 18
    sheet.save(path)


def spaced(b, text):
    """Glyphs side by side with a little air and no kerning, for specimen rows."""
    widest = max(im.size[0] for im, _ in b.glyphs.values())
    out = Image.new("RGBA", (len(text) * (widest + 2), b.cell_h), (0, 0, 0, 0))
    x = 0
    for c in text:
        if c == " ":
            continue
        im, _ = b.glyphs[c]
        out.alpha_composite(im, (x, b.cell_h - im.size[1]))
        x += im.size[0] + 2
    return out.crop(out.getbbox())


def compare_sheet(by_theme, backdrops, path, label_font):
    """One strip per theme on that theme's backdrop, so the sets can be told apart at a glance."""
    strips = []
    for name, baked in by_theme.items():
        small = [zoomed(baked[s].render(t), 2) for s, t in COMPARE]
        big = [baked[s].render(t) for s, t in COMPARE_BIG]
        label = zoomed(label_font.render(name.upper()), 2)
        h = max(p.size[1] for p in small + big) + 16
        w = 190 + sum(p.size[0] + 22 for p in small + big)
        strip = Image.new("RGBA", (w, h), hexc(backdrops[name]))
        strip.alpha_composite(label, (12, (h - label.size[1]) // 2))
        x = 190
        for part in small + big:
            strip.alpha_composite(part, (x, (h - part.size[1]) // 2))
            x += part.size[0] + 22
        strips.append(strip)
    sheet = Image.new("RGBA", (max(s.size[0] for s in strips), sum(s.size[1] for s in strips)), hexc("#000000"))
    y = 0
    for s in strips:
        sheet.alpha_composite(Image.new("RGBA", (sheet.size[0], s.size[1]), s.getpixel((0, 0))), (0, y))
        sheet.alpha_composite(s, (0, y))
        y += s.size[1]
    sheet.save(path)


# ---------------------------------------------------------------- plain fonts (TTF)
HEADROOM = 2     # rows kept above the ascent when rendering, for the accents on capitals


class Plain:
    """A one-colour font as the TTF holds it: glyph masks by code point plus its metrics."""

    def __init__(self, file, family, style, px, ascent, descent, gap):
        self.file, self.family, self.style = file, family, style
        self.px, self.ascent, self.descent, self.gap = px, ascent, descent, gap
        self.glyphs = {}        # code point -> (mask, top)
        self.aliases = {}       # code point -> code point that draws it
        self.kern = {}          # (code point, code point) -> pixels
        self.digit_w = 0

    def add(self, ch, mask, top):
        self.glyphs[ord(ch) if isinstance(ch, str) else ch] = (mask, top)

    def finish(self, kern_cap, kern_from):
        self.digit_w = max(len(self.glyphs[ord(d)][0][0]) for d in "0123456789")
        chars = [c for c in kern_from if ord(c) in self.glyphs]
        masks = {c: self.glyphs[ord(c)] for c in chars}
        self.kern = {(ord(a), ord(b)): k for (a, b), k in kerning_pairs(masks, chars, self.gap, kern_cap).items()}

    def metrics(self, code):
        """(left bearing, advance) in pixels. Digits share one width so counters do not jitter."""
        w = len(self.glyphs[code][0][0])
        if chr(code).isdigit():
            return (self.digit_w - w) // 2, self.digit_w + self.gap
        return 0, w + self.gap

    def resolve(self, ch):
        code = ord(ch)
        return code if code in self.glyphs else self.aliases.get(code)

    def render(self, text, kern=True):
        """The text as an image of 0/255 pixels, the ascent HEADROOM rows from the top."""
        codes = [self.resolve(c) for c in text]
        width = sum(self.metrics(c)[1] for c in codes) + 4
        im = Image.new("L", (width, self.ascent + self.descent + HEADROOM + 1), 0)
        p = im.load()
        x, prev = 0, None
        for code in codes:
            if kern and prev is not None:
                x += self.kern.get((prev, code), 0)
            mask, top = self.glyphs[code]
            lsb, adv = self.metrics(code)
            for i, row in enumerate(mask):
                for j, on in enumerate(row):
                    yy = HEADROOM + self.ascent - top + i
                    if on and 0 <= yy < im.size[1] and 0 <= x + lsb + j < width:
                        p[x + lsb + j, yy] = 255
            x += adv
            prev = code
        return im


def body_font(bold):
    f = Plain("HoldTheHillPixel-" + ("Bold" if bold else "Regular"), "Hold the Hill Pixel",
              "Bold" if bold else "Regular", px=100, ascent=8, descent=2, gap=1)
    for ch in body.ALL:
        f.add(ch, weighted(body.ALL, body.BOLD, ch, bold), body.TOP.get(ch, 7))
    for code, _, rows in buttons.BUTTONS:
        f.add(code, rows_to_mask(rows), buttons.TOP)
    f.aliases = {ord(k): ord(v) for k, v in body.ALIASES.items()}
    f.aliases.update({ord(k): v for k, v in buttons.ALIASES.items()})
    f.finish(1, kern_chars(list(body.ALL)))
    return f


def caps_font(file, family, glyphs, px, ascent, descent, gap, kern_cap):
    f = Plain(file, family, "Regular", px=px, ascent=ascent, descent=descent, gap=gap)
    height = len(glyphs["A"])
    for ch, rows in glyphs.items():
        if ch not in "^v":
            f.add(ch, rows_to_mask(rows), height)
    # Capitals only: lowercase types as capitals (the small cross stays on the multiplication sign).
    f.aliases = {c: c - 32 for c in range(ord("a"), ord("z") + 1)}
    f.aliases.update({ord(k): ord(v) for k, v in body.ALIASES.items() if ord(v) in f.glyphs})
    del f.glyphs[ord("x")]
    f.finish(kern_cap, kern_chars([c for c in glyphs if c not in "x×^v"]))
    return f


def build_ttf(f):
    from fontTools.fontBuilder import FontBuilder
    from fontTools.pens.ttGlyphPen import TTGlyphPen
    from fontTools.ttLib import newTable
    from fontTools.ttLib.tables._k_e_r_n import KernTable_format_0

    px = f.px
    names = {code: "uni%04X" % code for code in f.glyphs}
    cmap = dict(names)
    cmap.update({code: names[target] for code, target in f.aliases.items()})
    fb = FontBuilder(1000, isTTF=True)
    fb.setupGlyphOrder([".notdef"] + [names[c] for c in f.glyphs])
    fb.setupCharacterMap(cmap)
    outlines, metrics = {}, {}

    # .notdef: a hollow box, so a missing character is visible instead of silently blank
    cap = f.glyphs[ord("A")][1]
    box = [[x in (0, 3) or y in (0, cap - 1) for x in range(4)] for y in range(cap)]
    for gname, (mask, top), (lsb, adv) in [(".notdef", (box, cap), (0, 4 + f.gap))] + [
            (names[c], f.glyphs[c], f.metrics(c)) for c in f.glyphs]:
        pen = TTGlyphPen(None)
        first = min((row.index(True) for row in mask if any(row)), default=0)
        for i, row in enumerate(mask):
            x = 0
            while x < len(row):
                if not row[x]:
                    x += 1
                    continue
                x1 = x
                while x1 < len(row) and row[x1]:
                    x1 += 1
                left, right = (lsb + x) * px, (lsb + x1) * px
                y0, y1 = (top - i - 1) * px, (top - i) * px
                pen.moveTo((left, y0))
                pen.lineTo((left, y1))
                pen.lineTo((right, y1))
                pen.lineTo((right, y0))
                pen.closePath()
                x = x1
        outlines[gname] = pen.glyph()
        metrics[gname] = (adv * px, (lsb + first) * px)
    fb.setupGlyf(outlines)
    fb.setupHorizontalMetrics(metrics)
    fb.setupHorizontalHeader(ascent=f.ascent * px, descent=-f.descent * px)
    fb.setupNameTable({"familyName": f.family, "styleName": f.style})
    # Accented capitals rise two rows past the ascent; the Windows metrics cover them so nothing clips.
    fb.setupOS2(sTypoAscender=f.ascent * px, sTypoDescender=-f.descent * px, sTypoLineGap=0,
                usWinAscent=(f.ascent + 2) * px, usWinDescent=f.descent * px,
                sCapHeight=cap * px, sxHeight=(5 if cap == 7 else cap) * px)
    fb.setupPost()
    # A font with no instructions gets FreeType's auto-hinter, which stretches small shapes onto its
    # own grid (a five-row dot came out six rows tall). Having a program of its own switches that
    # off; this one only turns dropout control on. It has to be longer than seven bytes to count.
    from fontTools.ttLib.tables import ttProgram
    prep = newTable("prep")
    prep.program = ttProgram.Program()
    prep.program.fromAssembly(["PUSHW[ ]", "511", "SCANCTRL[ ]", "PUSHB[ ]", "4", "SCANTYPE[ ]",
                               "PUSHB[ ]", "0", "POP[ ]"])
    fb.font["prep"] = prep
    fb.font["maxp"].maxStackElements = 4
    if f.kern:
        # GPOS for TextMeshPro and shaping engines, and the old kern table for everything else.
        fea = "feature kern {\n" + "".join(
            f"  pos {names[a]} {names[b]} {k * px};\n" for (a, b), k in sorted(f.kern.items())) + "} kern;\n"
        fb.addOpenTypeFeatures(fea)
        sub = KernTable_format_0()
        sub.version, sub.coverage, sub.tupleIndex = 0, 1, None
        sub.kernTable = {(names[a], names[b]): k * px for (a, b), k in f.kern.items()}
        kern = newTable("kern")
        kern.version = 0
        kern.kernTables = [sub]
        fb.font["kern"] = kern
    fb.save(os.path.join(TTF_DIR, f.file + ".ttf"))


def check_ttf(f, size, lines):
    """Renders the TTF file with FreeType and compares it, pixel for pixel, with the drawings.

    Pillow's plain layout does not kern, so the lines are compared unkerned and the kerning is
    checked by reading both tables back out of the file.
    """
    from fontTools.ttLib import TTFont
    from PIL import ImageDraw, ImageFont

    font = ImageFont.truetype(os.path.join(TTF_DIR, f.file + ".ttf"), size, layout_engine=ImageFont.Layout.BASIC)
    bad = 0
    for text in lines:
        want = f.render(text, kern=False)
        got = Image.new("L", want.size, 0)
        d = ImageDraw.Draw(got)
        d.fontmode = "1"
        d.text((0, HEADROOM), text, font=font, fill=255)
        if want.getbbox() != got.getbbox() or want.crop(want.getbbox()).tobytes() != got.crop(got.getbbox()).tobytes():
            bad += 1
            want.save(os.path.join(HERE, "_check_want.png"))
            got.save(os.path.join(HERE, "_check_got.png"))
            print(f"  MISMATCH {f.file}: {ascii(text)} want bbox {want.getbbox()} got {got.getbbox()}")
    tt = TTFont(os.path.join(TTF_DIR, f.file + ".ttf"))
    want_pairs = {("uni%04X" % a, "uni%04X" % b): k * f.px for (a, b), k in f.kern.items()}
    old = dict(tt["kern"].kernTables[0].kernTable)
    gpos = {}
    for lookup in tt["GPOS"].table.LookupList.Lookup:
        for sub in lookup.SubTable:
            if sub.LookupType == 9:
                sub = sub.ExtSubTable
            for first, pair_set in zip(sub.Coverage.glyphs, sub.PairSet):
                for rec in pair_set.PairValueRecord:
                    gpos[(first, rec.SecondGlyph)] = rec.Value1.XAdvance
    kern_ok = old == want_pairs and gpos == want_pairs
    bad += 0 if kern_ok else 1
    print(f"  {f.file}: {len(lines) - min(bad, len(lines))}/{len(lines)} lines match the drawings at size {size}; "
          f"{len(want_pairs)} kerning pairs, kern table {'ok' if old == want_pairs else 'WRONG'}, "
          f"GPOS {'ok' if gpos == want_pairs else 'WRONG'}")
    return bad


SPECIMEN = {
    "upper": "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
    "lower": "abcdefghijklmnopqrstuvwxyz",
    "digits": "0123456789 +-=.,:;!?%/\\×",
    "punct": "'\"()[]{}<>#$&*@^~_|`",
    "symbols": "←→↑↓▲▼◀▶♥★☆✓✕●•…°±∞—",
    "accent_upper": "ÀÁÂÃÄÈÉÊËÌÍÎÏÑÒÓÔÕÖÙÚÛÜÝÇ",
    "accent_lower": "àáâãäèéêëìíîïñòóôõöùúûüýÿçß¡¿",
}
PANGRAMS = ["The quick brown fox jumps over the lazy dog.", "Pack my box with five dozen liquor jugs!",
            "Wave 3 of 10: 1,250 food, 85% HP. Hold the Hill!"]
KERN_DEMO = "To Ty Yo We LT P. r, fa TA'S AWAY Type"


def specimen_sheet(fonts, path):
    """Every glyph of the plain fonts, drawn from the same masks the TTFs are made from."""
    bg, fg, dim = hexc("#4a3a2e"), hexc("#fff1d6"), hexc("#c9a777")
    label_font = fonts[0]

    def ink(mask_im, colour, z):
        big = mask_im.resize((mask_im.size[0] * z, mask_im.size[1] * z), Image.NEAREST)
        out = Image.new("RGBA", big.size, colour)
        out.putalpha(big)
        return out

    items = []   # (image, indent)

    def line(f, text, z, colour=fg, kern=True, indent=0):
        items.append((ink(f.render(text, kern), colour, z), indent))

    def label(text):
        items.append((None, 0))
        line(label_font, text, 2, dim)

    regular, bold, disp, small = fonts
    for f, z in ((regular, 3), (bold, 3)):
        label(f"{f.family} {f.style}  ({f.file}.ttf, pixel-exact at size 10, 20, 30)")
        for key in SPECIMEN:
            line(f, SPECIMEN[key], z)
        for p in PANGRAMS:
            line(f, p, z)
    label("Kerning: on, then off")
    line(regular, KERN_DEMO, 4)
    line(regular, KERN_DEMO, 4, dim, kern=False)
    label("Digits share one width: 111 and 000 line up")
    line(regular, "111,111", 4)
    line(regular, "000,000", 4)

    label(f"{disp.family}  ({disp.file}.ttf, capitals only, pixel-exact at size 20, 40)")
    for text in ("ABCDEFGHIJKLM", "NOPQRSTUVWXYZ", "0123456789", ".,!?:-+/%×'()", "WAVE CLEAR!  HOLD THE HILL",
                 "LT AV TA P. LY (ON)"):
        line(disp, text, 2)
    line(disp, "LT AV TA P. LY (OFF)", 2, dim, kern=False)

    label(f"{small.family}  ({small.file}.ttf, capitals only, pixel-exact at size 10, 20)")
    for text in ("ABCDEFGHIJKLMNOPQRSTUVWXYZ", "0123456789 +-=.,:!?%/×'()<>", "HP 120/200  LV 3  COST 125  x2  +40%",
                 "THE QUICK BROWN FOX JUMPS OVER THE LAZY DOG"):
        line(small, text, 4)

    label("Key, mouse and gamepad prompts (body font, Private Use Area; names are in PixelGlyphs.cs)")
    per_row = 6
    cell_w = 300
    rows = [buttons.BUTTONS[i:i + per_row] for i in range(0, len(buttons.BUTTONS), per_row)]
    for r in rows:
        strip = Image.new("RGBA", (cell_w * per_row, 34), (0, 0, 0, 0))
        for i, (code, name, _) in enumerate(r):
            glyph = ink(regular.render(chr(code)), fg, 3)
            strip.alpha_composite(glyph, (i * cell_w, 0))
            text = ink(regular.render("%04X %s" % (code, name)), dim, 2)
            strip.alpha_composite(text, (i * cell_w + glyph.size[0] + 10, 8))
        items.append((strip, 0))

    width = max(im.size[0] + indent for im, indent in items if im is not None) + 32
    height = sum((im.size[1] + 8) if im is not None else 14 for im, _ in items) + 24
    sheet = Image.new("RGBA", (width, height), bg)
    y = 12
    for im, indent in items:
        if im is None:
            y += 14
            continue
        sheet.alpha_composite(im, (16 + indent, y))
        y += im.size[1] + 8
    sheet.save(path)


def write_glyph_constants(regular):
    """DamageNumbers/PixelGlyphs.cs: names for the characters nobody can type."""
    symbol_names = {"←": "ArrowLeft", "→": "ArrowRight", "↑": "ArrowUp", "↓": "ArrowDown", "▲": "TriangleUp",
                    "▼": "TriangleDown", "◀": "TriangleLeft", "▶": "TriangleRight", "♥": "Heart", "★": "Star",
                    "☆": "StarOutline", "✓": "Check", "✕": "Cross", "●": "Dot", "•": "Bullet", "…": "Ellipsis",
                    "°": "Degree", "±": "PlusMinus", "∞": "Infinity", "×": "Times"}
    out = ["// Generated by Fonts/Source~/build_pixel_fonts.py. Do not edit by hand; change the script and run it.",
           "namespace HoldTheHill.Sandbox.NillyCtrl", "{", "    /// <summary>",
           "    /// Characters in the Hold the Hill body font (HoldTheHillPixel Regular and Bold) that have no",
           "    /// key on a keyboard: the UI symbols and the key, mouse and gamepad prompts.",
           "    /// Use them in a string, for example <c>$\"{PixelGlyphs.KeySpace} START WAVE\"</c>.",
           "    /// </summary>", "    public static class PixelGlyphs", "    {"]
    for ch, name in symbol_names.items():
        assert ord(ch) in regular.glyphs, ch
        out.append(f"        public const string {name} = \"\\u{ord(ch):04X}\";")
    out.append("")
    for code, name, _ in buttons.BUTTONS:
        out.append(f"        public const string {name} = \"\\u{code:04X}\";")
    out += ["", "        /// <summary>The prompt for a letter or digit key, or an empty string if it has none.</summary>",
            "        public static string Key(char key)", "        {",
            "            char upper = char.ToUpperInvariant(key);",
            "            if (upper >= 'A' && upper <= 'Z')", "            {",
            "                return ((char)(0xE020 + (upper - 'A'))).ToString();", "            }", "",
            "            if (upper >= '0' && upper <= '9')", "            {",
            "                return ((char)(0xE010 + (upper - '0'))).ToString();", "            }", "",
            "            return string.Empty;", "        }", "    }", "}", ""]
    with open(GLYPHS_CS, "w", encoding="utf-8", newline="\n") as fh:
        fh.write("\n".join(out))


# ---------------------------------------------------------------- themes
def load_themes():
    """(base palette, THEMES) from the animation generator, or None if it cannot be imported."""
    sys.path.insert(0, ANIM_SOURCE)
    try:
        import build_anim_sheets
        import build_theme_sheets
        return build_anim_sheets.PAL, build_theme_sheets.THEMES
    except Exception as e:  # another session may be part-way through editing those scripts
        print(f"themes skipped: could not import the theme palettes ({type(e).__name__}: {e})")
        return None
    finally:
        sys.path.remove(ANIM_SOURCE)


def main():
    args = sys.argv[1:]
    os.makedirs(ATLAS_DIR, exist_ok=True)
    os.makedirs(TTF_DIR, exist_ok=True)

    baked = {st["name"]: build_style(st, ATLAS_DIR) for st in STYLES}
    label_font = baked["Hud"]
    sample_sheet(baked, os.path.join(HERE, "FontSheet.png"), label_font)
    style_specimen(baked, os.path.join(HERE, "SpecimenStyles.png"), label_font)

    fonts = [body_font(False), body_font(True),
             caps_font("HoldTheHillDisplay-Regular", "Hold the Hill Display", display.GLYPHS, 50, 13, 2, 2, 2),
             caps_font("HoldTheHillTiny-Regular", "Hold the Hill Tiny", tiny.GLYPHS, 100, 6, 1, 1, 1)]
    for f in fonts:
        build_ttf(f)
    specimen_sheet(fonts, os.path.join(HERE, "Specimen.png"))
    write_glyph_constants(fonts[0])
    print(f"wrote {len(STYLES)} styles, {len(fonts)} TTFs "
          f"({', '.join(f'{f.file} {len(f.glyphs)} glyphs {len(f.kern)} pairs' for f in fonts)})")

    themes = None if "--no-themes" in args else load_themes()
    import build_faces
    build_faces.run(baked, label_font, themes, fonts[0], "--check" in args)

    if themes:
        base_pal, all_themes = themes
        os.makedirs(THEME_SHEETS, exist_ok=True)
        by_theme, backdrops = {"Meadow": baked}, {"Meadow": "#4a3a2e"}
        for name, theme in all_themes.items():
            out_dir = os.path.join(ATLAS_DIR, "Themes", name)
            themed = {st["name"]: build_style(st, out_dir, theme=name) for st in theme_styles(base_pal, theme)}
            sample_sheet(themed, os.path.join(THEME_SHEETS, f"Theme{name}.png"), label_font,
                         backdrop=theme["cell"], title=name)
            by_theme[name], backdrops[name] = themed, theme["cell"]
        compare_sheet(by_theme, backdrops, os.path.join(HERE, "ThemeCompare.png"), label_font)
        print(f"wrote {len(all_themes)} themed sets of {len(STYLES)} styles")

    if "--check" in args:
        bad = 0
        body_lines = list(SPECIMEN.values()) + PANGRAMS + [KERN_DEMO, "111,111 000", "".join(
            chr(c) for c, _, _ in buttons.BUTTONS[:20])]
        bad += check_ttf(fonts[0], 10, body_lines)
        bad += check_ttf(fonts[1], 10, body_lines)
        bad += check_ttf(fonts[2], 20, ["ABCDEFGHIJKLM", "NOPQRSTUVWXYZ", "0123456789", ".,!?:-+/%×'()", "Wave Clear LT P."])
        bad += check_ttf(fonts[3], 10, ["ABCDEFGHIJKLMNOPQRSTUVWXYZ", "0123456789 +-=.,:!?%/×'()<>", "Hp 120/200 LT"])
        sys.exit(1 if bad else 0)


if __name__ == "__main__":
    main()
