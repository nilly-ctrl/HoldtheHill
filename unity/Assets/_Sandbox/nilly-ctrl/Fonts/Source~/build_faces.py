"""The ten extra alphabets (glyphs_faces.py): their TTFs, their baked styles in the new colour
treatments, and a title and label style for each art theme in the alphabet that suits it.

Run through build_pixel_fonts.py, which calls run(). Writes:
  ../TTF/HoldTheHill<Face>-Regular.ttf        one per alphabet
  ../Atlases/<Face><Paint>.png + .json        FACE_STYLES below
  ../Atlases/Title.*, Label.*                 the base (meadow) title and label
  ../Atlases/Themes/<Theme>/Title.*, Label.*  each theme's own
  ./FaceSheet.png        every face style with sample text
  ./SpecimenFaces.png    every glyph of the seven TTFs
  ./ThemeTitles.png      each theme's title and label
"""
import os
import unicodedata

from PIL import Image

import build_pixel_fonts as b
import glyphs_faces as faces

# Colour treatments. Any of them can go on any alphabet; FACE_STYLES picks the pairings that are baked.
PAINTS = {
    "Plain": dict(fill=["#ffffff", "#fff1d6", "#ecd9b4"], outline="#1b110b", shadow=(0, 1, "#1b110b")),
    "Gold": dict(fill=["#fffbe6", "#ffe98a", "#ffd23a", "#e0a020", "#b87818"], hi="#ffffff", lo="#7a4a10",
                 outline="#2a1708", shadow=(0, 2, "#2a1708")),
    "Chrome": dict(fill=["#ffffff", "#cfe8ff", "#8ab8e8", "#3a4a6a", "#b89a6a", "#f0d8a0", "#fff6d8"],
                   hi="#ffffff", outline="#10182a", shadow=(0, 2, "#10182a")),
    "Neon": dict(fill=["#ffffff", "#e8fbff"], outline="#00c8e8", glow=("#00eaff", 110), shadow=(0, 0, "#000000")),
    "Ice": dict(fill=["#ffffff", "#d8f6ff", "#9fe7ff", "#5aa9d8", "#3a78b8"], hi="#ffffff", outline="#0b2236",
                shadow=(0, 2, "#0b2236")),
    "Blood": dict(fill=["#ff8a7a", "#e2412f", "#b02a26", "#7a1418"], hi="#ffc0b0", lo="#4a080c",
                  outline="#1a0406", shadow=(0, 2, "#1a0406")),
    "Bone": dict(fill=["#fffff0", "#e8e4d0", "#c4c0a8"], lo="#8a866e", outline="#16121a", shadow=(0, 1, "#16121a")),
    "Candy": dict(fill=["#ff8ab0", "#fff4f8"], stripes=2, hi="#ffffff", outline="#7a1a4a", shadow=(0, 2, "#7a1a4a")),
    "Jade": dict(fill=["#e0fff0", "#8ae8b8", "#3ab88a", "#1a7a5a"], hi="#ffffff", outline="#0a2a20",
                 shadow=(0, 2, "#0a2a20")),
    "Hollow": dict(hollow=True, outline="#fff1d6", shadow=(1, 1, "#1b110b")),
    "Wood": dict(fill=["#f0d8a0", "#d8a860", "#b07838", "#8a5424"], hi="#fff0c8", lo="#5a3414", outline="#2a1708",
                 shadow=(0, 2, "#2a1708")),
    "Stamp": dict(fill=["#e8dcc0"], thick=0, outline="#e8dcc0", shadow=(1, 1, "#1b110b")),
}

# (style name, alphabet, paint, scale, what it is for)
FACE_STYLES = [
    ("SerifPlain", "serif", "Plain", 1, "Lore and menu text"),
    ("SerifGold", "serif", "Gold", 2, "Storybook titles"),
    ("RoundPlain", "round", "Plain", 1, "Friendly menu text"),
    ("RoundCandy", "round", "Candy", 2, "Sweet, bouncy titles"),
    ("TechPlain", "tech", "Plain", 1, "Readouts and sci-fi HUD text"),
    ("TechNeon", "tech", "Neon", 2, "Glowing sci-fi titles"),
    ("TechChrome", "tech", "Chrome", 2, "Metal titles"),
    ("GothicBone", "gothic", "Bone", 1, "Boss names"),
    ("GothicBlood", "gothic", "Blood", 2, "Spooky titles and boss warnings"),
    ("CondensedPlain", "condensed", "Plain", 1, "Text where width is short"),
    ("CondensedIce", "condensed", "Ice", 2, "Tall cold titles"),
    ("ChiselPlain", "chisel", "Plain", 1, "Carved-looking labels"),
    ("ChiselJade", "chisel", "Jade", 2, "Carved titles"),
    ("StencilStamp", "stencil", "Stamp", 1, "Crate and bunker markings"),
    ("StencilHollow", "stencil", "Hollow", 1, "Outlined headings"),
    ("SlabPlain", "slab", "Plain", 1, "Signs and wanted posters"),
    ("SlabWood", "slab", "Wood", 2, "Saloon and shipyard titles"),
    ("BubblePlain", "bubble", "Plain", 1, "Loud, soft headings"),
    ("BubbleCandy", "bubble", "Candy", 2, "Big sweet titles"),
    ("WidePlain", "wide", "Plain", 1, "Arcade readouts"),
    ("WideNeon", "wide", "Neon", 2, "Arcade and racing titles"),
    ("DecoPlain", "deco", "Plain", 1, "Tall, elegant labels"),
    ("DecoGold", "deco", "Gold", 2, "Marquee and poster titles"),
    ("RunicBone", "runic", "Bone", 1, "Carved stones and relics"),
    ("RunicIce", "runic", "Ice", 2, "Cold carved titles"),
    ("ScriptPlain", "script", "Plain", 1, "Notes, letters and signatures"),
    ("ScriptGold", "script", "Gold", 2, "Flourished titles"),
    ("DisplayChrome", "display", "Chrome", 1, "Metal headings in the display alphabet"),
    ("DisplayNeon", "display", "Neon", 1, "Glowing headings in the display alphabet"),
]

# Which alphabet each art theme's Title and Label are set in.
THEME_FACE = {
    "Meadow": "round", "Neon": "tech", "Space": "tech", "Spooky": "gothic", "Steampunk": "serif",
    "Medieval": "serif", "Samurai": "chisel", "Candy": "round", "Jungle": "chisel", "Pirate": "serif",
    "Robot": "stencil", "NeonSpace": "tech", "Vaporwave": "condensed", "Military": "stencil",
}
GLOWING = ("Neon", "NeonSpace", "Vaporwave")

# Pairings picked in the font artifact: a theme's style repainted in another alphabet. Each row is
# (theme, style, alphabet) with an optional fourth item naming a colour treatment, "bold" and/or
# "italic", for example ("Neon", "Banner", "slab", "Gold bold"). It bakes <Style><Alphabet>... beside
# that theme's other atlases ("Meadow" is the base set). The artifact writes the rows for you.
PAIRINGS = [
]

# ---------------------------------------------------------------- accents, symbols and prompts
HEAVY = ("round", "gothic", "stencil", "slab", "bubble")      # alphabets with strokes two or more pixels wide
# Each mark as (wide, narrow) for thin alphabets and again for heavy ones.
MARKS = {
    "\u0300": ((b.body.MARKS["\u0300"][0], ["#..", ".#."]), ([".##...", "..##.."], ["##..", ".##."])),
    "\u0301": ((b.body.MARKS["\u0301"][0], ["..#", ".#."]), (["...##.", "..##.."], ["..##", ".##."])),
    "\u0302": ((b.body.MARKS["\u0302"][0], [".#.", "#.#"]), (["..##..", ".#..#."], [".##.", "#..#"])),
    "\u0303": ((b.body.MARKS["\u0303"][0], [".##", "##."]), ([".##.##", "##.##."], [".#.#", "#.#."])),
    "\u0308": ((b.body.MARKS["\u0308"][0], ["#.#", "..."]), (["##..##", "......"], ["#..#", "...."])),
}
CEDILLA = (["..#.", ".#.."], ["..##", ".##."])


def _centred(mask, width):
    extra = width - len(mask[0])
    left = extra // 2
    return [[False] * left + row + [False] * (extra - left) for row in mask]


def _scaled(mask, k):
    if k == 1:
        return mask
    return [[mask[y // k][x // k] for x in range(len(mask[0]) * k)] for y in range(len(mask) * k)]


def add_extras(font, key):
    """Accented letters, the body alphabet's symbols and punctuation, and the key prompts."""
    f = faces.FACES[key]
    cap, heavy = f["cap"], key in HEAVY
    letters = {ch: b.rows_to_mask(rows) for ch, rows in f["glyphs"].items() if ch.isalpha()}
    for mark, bases in b.body.ACCENTED.items():
        wide, narrow = MARKS[mark][1 if heavy else 0]
        for base in bases:
            if base not in letters:
                continue
            m = letters[base]
            inked = [i for i, row in enumerate(m) if any(row)]
            start = inked[0]
            if base in "ij":                                   # drop the dot: the mark replaces it
                gaps = [i for i in range(start, inked[-1]) if not any(m[i])]
                if gaps:
                    start = gaps[-1] + 1
            body_rows = m[start:]
            w = len(body_rows[0])
            pick = b.rows_to_mask(wide if w >= len(wide[0]) - 1 else narrow)
            width = max(w, len(pick[0]))
            font.add(unicodedata.normalize("NFC", base + mark), _centred(pick, width) + _centred(body_rows, width),
                     cap - start + 2)
    for base in "Cc":
        if base in letters:
            m = letters[base][:cap]
            ced = b.rows_to_mask(CEDILLA[1 if heavy else 0])
            width = max(len(m[0]), len(ced[0]))
            font.add(unicodedata.normalize("NFC", base + "\u0327"), _centred(m, width) + _centred(ced, width), cap)
    for ch, flipped in (("!", "¡"), ("?", "¿")):
        m = b.rows_to_mask(f["glyphs"][ch])
        rows = [i for i, row in enumerate(m) if any(row)]
        font.add(flipped, m[rows[0]:rows[-1] + 1][::-1], max(cap - 2, 3))     # upside down, hanging below the line
    # The body alphabet's symbols, spare punctuation and prompts, doubled for the big alphabets.
    k = 2 if font.px == 50 else 1
    # A leaning face takes the lean back out of its letter gap; these upright extras need a plain one.
    spare = 1 - f["gap"] if f["gap"] < 1 else 0
    for ch, rows in list(b.body.PUNCT.items()) + list(b.body.SYMBOLS.items()):
        if ord(ch) not in font.glyphs and ch not in "ß¡¿":
            font.add(ch, _scaled(b.rows_to_mask(rows), k), b.body.TOP.get(ch, 7) * k)
            if spare:
                font.widths[ord(ch)] = len(rows[0]) * k + spare
    for code, _, rows in b.buttons.BUTTONS:
        font.add(code, _scaled(b.rows_to_mask(rows), k), b.buttons.TOP * k)
        if spare:
            font.widths[code] = len(rows[0]) * k + spare
    font.aliases.update({ord(c): v for c, v in b.buttons.ALIASES.items()})


def face_style(name, face, paint, scale, use):
    st = dict(name=name, face=face, use=use, bold=False, scale=scale, gap=b.FACES[face]["gap"],
              thick=1 if scale == 1 else 2)
    st.update(PAINTS[paint])
    if scale > 1:
        st["bevel"] = 2
        sdx, sdy, scol = st["shadow"]
        st["shadow"] = (sdx, sdy + 1 if sdy else 0, scol)
    return st


def spell(face, text):
    """Text as this alphabet can show it: capitals where it has no lowercase."""
    return text if faces.FACES.get(face, {}).get("lower") else text.upper()


def theme_extras(name, banner, label, glow=None):
    """A theme's Title (large, in the theme's banner colours) and Label (small and plain)."""
    face = THEME_FACE.get(name, "round")
    big = 1 if face == "stencil" else 2      # the stencil alphabet is already twelve pixels tall
    title = dict(name="Title", face=face, use=f"{name} theme titles", bold=False, scale=big, gap=b.FACES[face]["gap"],
                 thick=2, bevel=big, fill=banner["fill"], hi=banner.get("hi"), lo=banner.get("lo"),
                 outline=banner["outline"], shadow=(0, 3 if big == 2 else 2, banner["outline"]))
    if glow:
        title["glow"] = (glow, 110)
        title["shadow"] = (0, 0, banner["outline"])
    small = dict(name="Label", face=face, use=f"{name} theme labels and menu text", bold=False, scale=1,
                 gap=b.FACES[face]["gap"], fill=label["fill"], outline=label["outline"], shadow=(1, 1, "#00000080"))
    return [title, small]


def pairing_style(st, face, extras=""):
    """A style in another alphabet, colour treatment or weight, named after what changed.

    extras is a few words: a name from PAINTS, "bold", "italic". Mirrors resolve() in the font
    artifact, so what gets baked is what was previewed there.
    """
    new = dict(st)
    name = st["name"]
    if face != st.get("face", "body"):
        new["face"] = face
        new["gap"] = b.FACES[face]["gap"]
        name += faces.FACES[face]["name"] if face in faces.FACES else face.capitalize()
    for word in extras.split():
        if word in PAINTS:
            for key in ("fill", "hi", "lo", "outline", "glow", "stripes", "hollow"):
                new.pop(key, None)
            paint = PAINTS[word]
            new.update({k: v for k, v in paint.items() if k != "shadow"})
            new["shadow"] = (st["shadow"][0], st["shadow"][1], paint["shadow"][2])   # the style's offset, the paint's colour
            name += word
        elif word == "bold":
            new["bold"] = True
            name += "Bold"
        elif word == "italic":
            # lean one pixel every three rows whatever the scale, and take the lean back out of the letter gap
            face_def = b.FACES[new.get("face", "body")]
            rows = next(iter(face_def["glyphs"].values()))
            tall = (face_def.get("cap", len(rows)) + face_def.get("desc", 0)) * new["scale"]
            new["slant"] = 3 * new["scale"]
            new["gap"] = new.get("gap", 1 if new["scale"] == 1 else 2) - (tall - 1) // new["slant"]
            name += "Italic"
        else:
            raise ValueError(f"pairing option {word!r} is not a paint, bold or italic")
    new["name"] = name
    new["use"] = f"{st['use']} (pairing of {st['name']})"
    return new


ITALIC_LEAN = 3     # rows per pixel of lean in the italic TTFs


def restyle(font, weight):
    """Turns a finished regular font into its Bold or Italic. Prompts stay as they are."""
    font.file = font.file.replace("-Regular", "-" + weight)
    font.style = weight
    for code, (mask, top) in list(font.glyphs.items()):
        if 0xE000 <= code < 0xF000 or not any(any(row) for row in mask):
            continue
        if weight == "Bold":
            font.glyphs[code] = (b.smear(mask), top)
        else:
            # lean right, one pixel every few rows, pivoting on the baseline; the advance stays put
            shifts = [(top - 1 - i) // ITALIC_LEAN + 1 for i in range(len(mask))]
            extra = max(shifts)
            font.widths.setdefault(code, len(mask[0]))
            font.glyphs[code] = ([[False] * sh + row + [False] * (extra - sh) for sh, row in zip(shifts, mask)], top)
    if weight == "Bold":
        # the letters are a pixel wider, so work the kerning and the digit column out again
        font.finish(font.kern_cap, font.kern_from)
    return font


def plain_font(key, weight="Regular"):
    f = faces.FACES[key]
    px = 50 if key == "stencil" else 100
    k = 2 if px == 50 else 1
    # room for the prompts (eight rows up, one down) and for descending marks, whatever the letter height
    font = b.Plain(f["file"], f["family"], "Regular", px=px, ascent=max(f["cap"] + 1, 8 * k),
                   descent=max(f["desc"], 2 * k), gap=f["gap"])
    for ch, rows in f["glyphs"].items():
        font.add(ch, b.rows_to_mask(rows), f["cap"])
    font.aliases = {ord(k): ord(v) for k, v in b.body.ALIASES.items() if ord(v) in font.glyphs}
    if not f["lower"]:
        # capitals only: lowercase types as capitals (the small cross stays on the multiplication sign)
        del font.glyphs[ord("x")]
        font.aliases.update({c: c - 32 for c in range(ord("a"), ord("z") + 1)})
    add_extras(font, key)
    font.kern_cap, font.kern_from = f["kern_cap"], b.kern_chars([c for c in f["glyphs"] if f["lower"] or c not in "x×"])
    font.finish(font.kern_cap, font.kern_from)
    return font if weight == "Regular" else restyle(font, weight)


def ink(mask_im, colour, z):
    big = mask_im.resize((mask_im.size[0] * z, mask_im.size[1] * z), Image.NEAREST)
    out = Image.new("RGBA", big.size, colour)
    out.putalpha(big)
    return out


EXTRAS_LINE = "ÀÉÎÕÜÇÑ ¡¿ $&#@ ←→↑↓ ♥★✓✕ \ue002 \ue024 \ue050 \ue060 \ue06a"


def specimen(fonts, label_font, path):
    bg, fg, dim = b.hexc("#4a3a2e"), b.hexc("#fff1d6"), b.hexc("#c9a777")
    items = []
    for key, font in fonts.items():
        f = faces.FACES[key]
        size = "20, 40" if font.px == 50 else "10, 20, 30"
        kind = "capitals and lowercase" if f["lower"] else "capitals only"
        items.append(None)
        items.append(ink(label_font.render(f"{f['family']}  ({f['file']}.ttf, {kind}, pixel-exact at size {size})"), dim, 2))
        z = 2 if font.px == 50 else 3
        lines = ["ABCDEFGHIJKLMNOPQRSTUVWXYZ"]
        if f["lower"]:
            lines.append("abcdefghijklmnopqrstuvwxyz")
        lines += ["0123456789 +-.,:!?%/×'()", spell(key, "Hold the Hill! Wave 3 of 10, 85% HP."), EXTRAS_LINE]
        if f["lower"]:
            lines.append("àéîõüçñ ¡¿")
        for text in lines:
            items.append(ink(font.render(text), fg, z))
    width = max(im.size[0] for im in items if im is not None) + 32
    height = sum((im.size[1] + 8) if im is not None else 14 for im in items) + 24
    sheet = Image.new("RGBA", (width, height), bg)
    y = 12
    for im in items:
        if im is None:
            y += 14
            continue
        sheet.alpha_composite(im, (16, y))
        y += im.size[1] + 8
    sheet.save(path)


def title_sheet(by_theme, backdrops, path, label_font):
    strips = []
    for name, baked in by_theme.items():
        face = THEME_FACE[name]
        parts = [b.zoomed(label_font.render(name.upper()), 2), b.zoomed(baked["Title"].render(spell(face, name + " Hill")), 2),
                 b.zoomed(baked["Label"].render(spell(face, "Wave 3 of 10  Start")), 3)]
        h = max(p.size[1] for p in parts) + 20
        strip = Image.new("RGBA", (190 + parts[1].size[0] + 40 + parts[2].size[0] + 24, h), b.hexc(backdrops[name]))
        strip.alpha_composite(parts[0], (12, (h - parts[0].size[1]) // 2))
        strip.alpha_composite(parts[1], (190, (h - parts[1].size[1]) // 2))
        strip.alpha_composite(parts[2], (190 + parts[1].size[0] + 40, (h - parts[2].size[1]) // 2))
        strips.append(strip)
    sheet = Image.new("RGBA", (max(s.size[0] for s in strips), sum(s.size[1] for s in strips)), (0, 0, 0, 255))
    y = 0
    for s in strips:
        sheet.alpha_composite(Image.new("RGBA", (sheet.size[0], s.size[1]), s.getpixel((0, 0))), (0, y))
        sheet.alpha_composite(s, (0, y))
        y += s.size[1]
    sheet.save(path)


def run(base_baked, label_font, themes, body_regular, check):
    # ---- TTFs
    fonts = {key: plain_font(key) for key in faces.FACES}
    variants = [plain_font(key, weight) for key in faces.FACES for weight in ("Bold", "Italic")]
    for font in list(fonts.values()) + variants:
        b.build_ttf(font)
    specimen(fonts, body_regular, os.path.join(b.HERE, "SpecimenFaces.png"))

    # ---- baked face styles
    styles = [face_style(*row) for row in FACE_STYLES]
    baked = {st["name"]: b.build_style(st, b.ATLAS_DIR) for st in styles}
    samples = {st["name"]: [spell(st["face"], "Hold the Hill"), "128!"] for st in styles}
    b.sample_sheet(baked, os.path.join(b.HERE, "FaceSheet.png"), label_font, styles=styles, samples=samples)

    # ---- a title and a label per theme
    banner = next(st for st in b.STYLES if st["name"] == "Banner")
    hud = next(st for st in b.STYLES if st["name"] == "Hud")
    meadow_extras = theme_extras("Meadow", banner, hud)
    defs = {"Meadow": {st["name"]: st for st in b.STYLES + styles + meadow_extras}}
    by_theme = {"Meadow": {st["name"]: b.build_style(st, b.ATLAS_DIR, theme="Meadow") for st in meadow_extras}}
    backdrops = {"Meadow": "#4a3a2e"}
    if themes:
        base_pal, all_themes = themes
        for name, theme in all_themes.items():
            paint = b.theme_styles(base_pal, theme, paint_only=True)
            glow = theme["accents"][0][0] if name in GLOWING else None
            extras = theme_extras(name, paint["Banner"], paint["Hud"], glow)
            out_dir = os.path.join(b.ATLAS_DIR, "Themes", name)
            defs[name] = {st["name"]: st for st in b.theme_styles(base_pal, theme) + extras}
            by_theme[name] = {st["name"]: b.build_style(st, out_dir, theme=name) for st in extras}
            backdrops[name] = theme["cell"]
    title_sheet(by_theme, backdrops, os.path.join(b.HERE, "ThemeTitles.png"), label_font)
    for theme_name, style_name, face, *extras in PAIRINGS:
        st = pairing_style(defs[theme_name][style_name], face, " ".join(extras))
        out_dir = b.ATLAS_DIR if theme_name == "Meadow" else os.path.join(b.ATLAS_DIR, "Themes", theme_name)
        b.build_style(st, out_dir, theme=theme_name)
        print(f"  pairing: {theme_name} {st['name']}")
    print(f"wrote {len(variants)} bold and italic TTFs")
    print(f"wrote {len(fonts)} more TTFs, {len(styles)} face styles, a Title and a Label for {len(by_theme)} themes")

    if check:
        bad = 0
        for key, font in fonts.items():
            lines = ["ABCDEFGHIJKLMNOPQRSTUVWXYZ", "0123456789 +-.,:!?%/×'()", "Hold the Hill LT P.", EXTRAS_LINE]
            if faces.FACES[key]["lower"]:
                lines.append("abcdefghijklmnopqrstuvwxyz")
            bad += b.check_ttf(font, 20 if font.px == 50 else 10, lines)
        for font in variants:
            bad += b.check_ttf(font, 20 if font.px == 50 else 10, ["Hold the Hill 128", EXTRAS_LINE])
        if bad:
            raise SystemExit(1)
