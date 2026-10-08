"""Packs the alphabets, style definitions and TTFs into the font showcase page (index.html) and
its data file (fonts.json), both written beside this script. Run serve.py to pack and open it.

The page paints the glyphs itself from this data (tester.js mirrors bake() in build_pixel_fonts.py),
so any alphabet can be combined with any theme's colours.
"""
import base64
import json
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
FONTS = os.path.normpath(os.path.join(HERE, "..", "..")).replace("\\", "/")
sys.path.insert(0, FONTS + "/Source~")
import build_pixel_fonts as b  # noqa: E402
import build_faces  # noqa: E402


def b64(path):
    with open(path, "rb") as f:
        return base64.b64encode(f.read()).decode("ascii")


NAMES = {"body": "Body", "display": "Display", "tiny": "Tiny"}
NAMES.update({k: f["name"] for k, f in b.faces.FACES.items()})
face_data = {}
for key, face in b.FACES.items():
    rows = next(iter(face["glyphs"].values()))
    face_data[key] = {
        "name": NAMES[key], "cap": face.get("cap", len(rows)), "desc": face.get("desc", 0), "gap": face["gap"],
        "glyphs": {ch: face["glyphs"][ch] for ch in face["chars"]}, "bold": face["bold"],
        "kern": {a + c: k for (a, c), k in b.face_kerning(key, False).items()},
        "kernBold": {a + c: k for (a, c), k in b.face_kerning(key, True).items()},
    }

uses = {}


def pack(styles, backdrop):
    for st in styles:
        uses.setdefault(st["name"], st["use"].split(" theme ")[-1].capitalize() if st["name"] in ("Title", "Label")
                        else st["use"])
    clean = [{k: v for k, v in st.items() if k != "use" and v is not None} for st in styles]
    return {"backdrop": backdrop, "order": [st["name"] for st in clean], "styles": {st["name"]: st for st in clean}}


base_pal, themes = b.load_themes()
banner = next(st for st in b.STYLES if st["name"] == "Banner")
hud = next(st for st in b.STYLES if st["name"] == "Hud")
face_styles = [build_faces.face_style(*row) for row in build_faces.FACE_STYLES]
data = {
    "faces": face_data,
    "paints": dict(build_faces.PAINTS, Custom={"fill": ["#ffffff"], "outline": "#000000", "shadow": [0, 1, "#000000"]}),
    "uses": uses,
    "samples": {name: texts[-1] if name.startswith("Banner") or name == "Heading" else texts[0]
                for name, texts in b.SAMPLES.items()},
    "buttons": [[code, name] for code, name, _ in b.buttons.BUTTONS],
    "themes": {"Meadow": pack(build_faces.theme_extras("Meadow", banner, hud) + b.STYLES + face_styles, "#4a3a2e")},
}
for name, theme in themes.items():
    paint = b.theme_styles(base_pal, theme, paint_only=True)
    glow = theme["accents"][0][0] if name in build_faces.GLOWING else None
    extras = build_faces.theme_extras(name, paint["Banner"], paint["Hud"], glow)
    data["themes"][name] = pack(extras + b.theme_styles(base_pal, theme), theme["cell"])
with open(HERE + "/fonts.json", "w", encoding="utf-8") as f:
    json.dump(data, f, ensure_ascii=False, separators=(",", ":"))

html = open(HERE + "/template.html", encoding="utf-8").read()
for key, file in (("PIXEL", "HoldTheHillPixel-Regular"), ("BOLD", "HoldTheHillPixel-Bold"),
                  ("DISPLAY", "HoldTheHillDisplay-Regular"), ("TINY", "HoldTheHillTiny-Regular"),
                  ("SERIF", "HoldTheHillSerif-Regular"), ("ROUND", "HoldTheHillRound-Regular"),
                  ("TECH", "HoldTheHillTech-Regular"), ("GOTHIC", "HoldTheHillGothic-Regular"),
                  ("CONDENSED", "HoldTheHillCondensed-Regular"), ("CHISEL", "HoldTheHillChisel-Regular"),
                  ("STENCIL", "HoldTheHillStencil-Regular"), ("SLAB", "HoldTheHillSlab-Regular"),
                  ("BUBBLE", "HoldTheHillBubble-Regular"), ("WIDE", "HoldTheHillWide-Regular"),
                  ("DECO", "HoldTheHillDeco-Regular"), ("RUNIC", "HoldTheHillRunic-Regular"),
                  ("SCRIPT", "HoldTheHillScript-Regular")):
    html = html.replace(f"%%{key}%%", b64(f"{FONTS}/TTF/{file}.ttf"))
start, end = html.rindex("<script>") + len("<script>"), html.rindex("</script>")
html = html[:start] + "\n" + open(HERE + "/tester.js", encoding="utf-8").read() + html[end:]
with open(HERE + "/index.html", "w", encoding="utf-8") as f:
    f.write(html)
print(os.path.getsize(HERE + "/fonts.json"), os.path.getsize(HERE + "/index.html"))
