"""Themed title screen variants for Hold the Hill.

Same 320x180 composition as build_title_art.py (hill, flag, ants, logo, four buttons), redrawn
across themes: time of day, seasons, biomes, genres and retro hardware looks.

Writes into ./Variants (Unity ignores Source~):
  Title<NN>_<Name>.png                  backdrop + logo + buttons at 4x (1280x720)
  Backgrounds/TitleBackground_<Name>.png  the backdrop alone, 320x180
  Sheet_<Group>.png                     one sheet per group, 2x
  TitleVariantsSheet.png                every theme on one sheet, 1x
  Gallery.html                          browse them all

    python build_title_variants.py            all themes
    python build_title_variants.py Neon Dune  only themes whose name contains one of these
"""
import importlib.util
import math
import random
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageOps

HERE = Path(__file__).resolve().parent
SANDBOX = HERE.parent.parent.parent
OUT = HERE / "Variants"
W, H = 320, 180


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


# ---------------------------------------------------------------- colour helpers

def rgb(c):
    c = c.lstrip("#")
    return (int(c[0:2], 16), int(c[2:4], 16), int(c[4:6], 16))


def hexc(c, a=255):
    return rgb(c) + (a,)


def mix(a, b, t):
    ra, rb = rgb(a), rgb(b)
    return "#%02x%02x%02x" % tuple(round(ra[i] + (rb[i] - ra[i]) * t) for i in range(3))


def lighten(c, t):
    return mix(c, "#ffffff", t)


def shade(c, f):
    return mix("#000000", c, f)


def ramp(keys, n):
    out = []
    for i in range(n):
        f = i / (n - 1) * (len(keys) - 1)
        k = min(len(keys) - 2, int(f))
        out.append(mix(keys[k], keys[k + 1], f - k))
    return out


# ---------------------------------------------------------------- themes

DEFAULT = dict(
    layout="right",                       # right | left | center: where the logo and menu sit
    sky=["#241238", "#3b1d4a", "#662852", "#b53e43", "#f07444", "#ffb35a"],
    stars=46, star_cols=["#fff1d6", "#b98cff"],
    body=("sun", 46, 100, 30, "#ffc85a"), body2=None,
    clouds="thin", cloud_col=None,
    aurora=None, ceiling=False, rainbow=None,
    ridge="hills", far="#4c2150", near="#2a2a1c", snowcaps=None,
    props=None, prop_acc=None, fg_props=None,
    hill="mound", strata="depth", earth="#83562f", cap="#7cc84a", cap_thick=1, lava=False,
    glow="#f2a93b",
    ground="tuft", ground_col="#3c6a26", tuft="#7cc84a", flowers=["#fff1d6", "#ffd23a", "#ff6f7d"],
    grid_col=None,
    weather=None, weather_cols=None,
    flag="#e2412f", ink="#1b110b",
    logo=["#fff6c8", "#f2a93b", "#c9562a"], logo_small=None,
    btn=None,                             # None keeps the wooden buttons
    retro=None,                           # (mode, palette) applied to the finished picture
)

GB = ["#0f380f", "#306230", "#8bac0f", "#9bbc0f"]
CGA = ["#000000", "#ff55ff", "#55ffff", "#ffffff"]
REDSCOPE = ["#000000", "#550000", "#aa0000", "#ff0000"]
ONEBIT = ["#000000", "#ffffff"]
AMBER = ["#140a00", "#5a3400", "#b87000", "#ffb000", "#ffd880"]
BLUEPRINT = ["#0a2a6a", "#1a4a9a", "#4a7ac8", "#a8c8f0", "#ffffff"]
SEPIA = ["#2a1a0e", "#5a3e22", "#8a6a3e", "#c0a070", "#f0e0c0"]
C64 = ["#000000", "#ffffff", "#68372b", "#70a4b2", "#6f3d86", "#588d43", "#352879", "#b8c76f",
       "#6f4f25", "#433900", "#9a6759", "#444444", "#6c6c6c", "#9ad284", "#6c5eb5", "#959595"]

THEMES = [
    # ---- time of day
    dict(name="Sunset Stand", group="TimeOfDay", near="#2a2a1c"),
    dict(name="First Light", group="TimeOfDay", layout="left",
         sky=["#2b2a5c", "#5a4a8a", "#a86a9a", "#f09a8a", "#ffd0a0", "#fff0c8"], stars=18,
         body=("sun", 60, 104, 26, "#fff2b0"), far="#7a6a9a", near="#3a5a4a",
         earth="#7a5a3a", cap="#8ad060", ground_col="#4a8a3a", tuft="#9be060",
         logo=["#ffffff", "#ffd0a0", "#e8785a"], props="pine"),
    dict(name="High Noon", group="TimeOfDay", layout="center",
         sky=["#2a7ad8", "#4a9ae8", "#7ac0f4", "#a8dcf8", "#d0f0fc"], stars=0,
         body=("sun", 40, 34, 15, "#fff6b0"), clouds="puffy", cloud_col="#ffffff",
         far="#6aa0c8", near="#3a7a3a", earth="#8a5e34", cap="#8ee04a",
         ground_col="#4a9a2e", tuft="#a4f04a", props="tree", fg_props="tree", prop_acc="#3a8a2e",
         logo=["#ffffff", "#ffe070", "#f08a1e"]),
    dict(name="Blue Hour", group="TimeOfDay",
         sky=["#0e1430", "#1a2450", "#2a3a78", "#4a5aa0", "#8a7ab8", "#d89aa8"], stars=60,
         body=("crescent", 52, 36, 14, "#f4f0ff"), far="#2a3468", near="#141c38",
         earth="#4a3a4a", cap="#4a9a7a", ground_col="#1e4a4a", tuft="#4a9a7a",
         flowers=["#c8d8ff", "#8a7ab8"], weather="fireflies", weather_cols=["#fff2a0"],
         logo=["#ffffff", "#b8c8ff", "#6a6ac8"], btn="#3a4a8a", ink="#0a0e20", props="pine"),
    dict(name="Midnight Watch", group="TimeOfDay", layout="left",
         sky=["#04060e", "#080c1c", "#0e1630", "#16224a", "#22346a"], stars=110,
         star_cols=["#ffffff", "#8ab0ff"], body=("moon", 56, 40, 19, "#e8f0ff"), clouds="thin",
         far="#16224a", near="#0a1020", earth="#3a3040", cap="#3a7a6a", ground_col="#12303a",
         tuft="#3a7a6a", flowers=["#8ab0ff"], weather="fireflies", weather_cols=["#ffe890"],
         glow="#ffd27a", logo=["#ffffff", "#a8c0ff", "#4a5ab8"], btn="#26346a", ink="#04060e",
         props="pine", fg_props="pine"),
    dict(name="Thunderhead", group="TimeOfDay",
         sky=["#16181e", "#22262e", "#333a44", "#48525e", "#667280", "#8a96a0"], stars=0,
         body=None, clouds="puffy", cloud_col="#3a424c", far="#3a424c", near="#1c2228",
         earth="#5a4a3a", cap="#5a8a4a", ground_col="#2a4a2a", tuft="#5a8a4a", flowers=[],
         weather="storm", weather_cols=["#a8b8c8"], flag="#c83a2a",
         logo=["#ffffff", "#ffe870", "#c89a1e"], btn="#3a4450", ink="#0c0e12", props="dead"),

    # ---- seasons
    dict(name="Spring Blossom", group="Seasons",
         sky=["#6ab4e8", "#9acdf0", "#c8e4f8", "#f4dcec", "#ffd0e0"], stars=0,
         body=("sun", 50, 38, 14, "#fff6c8"), clouds="puffy", cloud_col="#ffffff",
         far="#a8c8e0", near="#5aa05a", props="tree", fg_props="tree", prop_acc="#ff9ac8",
         earth="#8a6238", cap="#9be060", ground_col="#5ab040", tuft="#b4f070",
         flowers=["#ffffff", "#ff9ac8", "#ffd23a"], weather="petals",
         weather_cols=["#ffb0d0", "#ffffff"], flag="#ff5a9a",
         logo=["#ffffff", "#ffb0d0", "#e84a8a"], btn="#c85a8a"),
    dict(name="Summer Meadow", group="Seasons", layout="center",
         sky=["#1e8ae0", "#48a8f0", "#80c8f8", "#b8e4fc"], stars=0,
         body=("sun", 284, 36, 16, "#fff080"), clouds="puffy", cloud_col="#ffffff",
         far="#58a8c0", near="#2e8a3a", earth="#946a38", cap="#a4e84a",
         ground_col="#3ea02e", tuft="#b8f060",
         flowers=["#ffffff", "#ffd23a", "#ff6f7d", "#ff9a3a", "#c88aff"],
         props="tree", fg_props="tree", prop_acc="#2e9a2e",
         logo=["#ffffff", "#fff070", "#ffa01e"]),
    dict(name="Autumn Harvest", group="Seasons", layout="left",
         sky=["#4a3a5a", "#8a4a5a", "#c8684a", "#f09a4a", "#ffc86a"], stars=10,
         body=("sun", 50, 98, 28, "#ffb03a"), far="#8a4a3a", near="#5a2a1a",
         props="tree", fg_props="tree", prop_acc="#e8782a",
         earth="#6e4424", cap="#d88a2a", ground_col="#8a5a1e", tuft="#e8a83a",
         flowers=["#e2412f", "#ffd23a"], weather="leaves",
         weather_cols=["#e8782a", "#c83a1e", "#ffd23a"],
         logo=["#fff0c0", "#f09a2a", "#b83a1a"]),
    dict(name="Deep Winter", group="Seasons",
         sky=["#3a4a7a", "#5a70a8", "#8aa0cc", "#c0d0e8", "#e8f0fa"], stars=20,
         star_cols=["#ffffff", "#c0d0e8"], body=("sun", 46, 98, 24, "#fff8e0"),
         ridge="peaks", far="#8a9cc8", near="#4a5a8a", snowcaps="#f4f8ff",
         props="pine", fg_props="pine", prop_acc="#f4f8ff",
         earth="#6a7498", cap="#ffffff", cap_thick=4, ground="flat", ground_col="#dce8f8",
         tuft="#ffffff", flowers=["#ffffff", "#a8c0e8"], weather="snow",
         weather_cols=["#ffffff"], glow="#ffb84a", flag="#d83a3a",
         logo=["#ffffff", "#b8dcff", "#4a8ad8"], btn="#4a6aa8", ink="#161a2e"),

    dict(name="Rainy Season", group="Seasons", layout="center",
         sky=["#2a4a4a", "#3a6a62", "#5a8a7a", "#8ab09a", "#c0d8b8"], stars=0, body=None,
         clouds="puffy", cloud_col="#5a7a74", far="#4a7a6a", near="#1e4a32",
         props="palm", fg_props="palm", prop_acc="#2a8a4a", earth="#5e4428", cap="#5ad05a",
         cap_thick=2, ground_col="#1e6a34", tuft="#6ae06a", flowers=["#ffd23a", "#ff6f9a"],
         weather="rain", weather_cols=["#c8e8e0"], flag="#ffb01e",
         logo=["#ffffff", "#a8f0c8", "#2a9a7a"], btn="#2a6a5a", ink="#0a1a14"),
    dict(name="First Frost", group="Seasons", layout="left",
         sky=["#4a3a6a", "#7a5a8a", "#b888a8", "#e8b8b8", "#ffe8d8"], stars=12,
         star_cols=["#ffffff", "#e8d0f0"], body=("sun", 56, 100, 24, "#fff0e0"),
         far="#9a88b0", near="#5a5270", props="dead", fg_props="dead",
         earth="#6a5448", cap="#d8f0f0", cap_thick=2, ground_col="#8aa8a0", tuft="#e8ffff",
         flowers=["#ffffff", "#c8e8f0"], weather="snow", weather_cols=["#ffffff"],
         flag="#c83a4a", logo=["#ffffff", "#f0c8d8", "#8a6ab8"], btn="#7a6a9a", ink="#1e162a"),

    # ---- biomes
    dict(name="Dune Sea", group="Biomes",
         sky=["#d8703a", "#e8a04a", "#f4bc6a", "#fad890", "#fff0c0"], stars=0,
         body=("sun", 48, 92, 34, "#fff6d0"), clouds=None, ridge="dunes",
         far="#e0a060", near="#b8743a", props="cactus", fg_props="cactus", prop_acc="#5a8a3a",
         earth="#c8904a", cap="#f4d888", cap_thick=2, ground="flat", ground_col="#d8a858",
         tuft="#f4d888", flowers=["#f4d888", "#b8743a"], weather="sand",
         weather_cols=["#fff0c0"], flag="#2a8ac8", logo=["#ffffff", "#ffe08a", "#d8702a"],
         btn="#b8743a"),
    dict(name="Jungle Canopy", group="Biomes", layout="left",
         sky=["#0e3a2a", "#1a5a3a", "#2e8a4a", "#6ac060", "#c8f090"], stars=0,
         body=("sun", 54, 96, 30, "#f0ffb0"), clouds=None, far="#1e6a3e", near="#0a2e1a",
         props="palm", fg_props="palm", prop_acc="#2ea04a",
         earth="#5a3e22", cap="#4ae04a", cap_thick=2, ground_col="#146a2a", tuft="#5af05a",
         flowers=["#ff4a8a", "#ffd23a", "#ff8a1e", "#5ae0ff"], weather="fireflies",
         weather_cols=["#e8ff8a"], flag="#ffb01e", logo=["#ffffff", "#c8f060", "#2ea02e"],
         btn="#2a7a3a", ink="#06180e"),
    dict(name="Bog Lantern", group="Biomes",
         sky=["#12160e", "#1e2a18", "#34402a", "#56603a", "#8a8a4a"], stars=14,
         star_cols=["#d8e8a0", "#8aa860"], body=("moon", 50, 44, 17, "#d8f0a0"),
         far="#2a3a22", near="#141c10", props="dead", fg_props="dead",
         earth="#3e3a26", cap="#6a8a2a", ground_col="#2a3a1a", tuft="#6a8a2a",
         flowers=["#a8e04a", "#e8f08a"], weather="spores", weather_cols=["#a8ff5a", "#e8ff8a"],
         glow="#a8ff5a", flag="#8a5ac8", logo=["#f4ffd0", "#a8d84a", "#4a7a1e"],
         btn="#4a5a26", ink="#0a0e06"),
    dict(name="Aurora Tundra", group="Biomes", layout="center",
         sky=["#04081a", "#0a1430", "#122448", "#1e3a60", "#3a6a80"], stars=90,
         star_cols=["#ffffff", "#8ae8d0"], body=None, clouds=None,
         aurora=["#3af0a0", "#3ac8e0", "#a05af0"], ridge="peaks", far="#22406a",
         near="#0e1c38", snowcaps="#c8e8ff", earth="#5a6a88", cap="#eaf6ff", cap_thick=3,
         ground="flat", ground_col="#b8d0e8", tuft="#eaf6ff", flowers=["#ffffff"],
         weather="snow", weather_cols=["#ffffff"], glow="#ffb84a",
         logo=["#ffffff", "#8af0d0", "#2a8ab8"], btn="#1e4a6a", ink="#04081a",
         props="pine", fg_props="pine"),
    dict(name="Cinder Cone", group="Biomes",
         sky=["#140606", "#2a0a0a", "#4a1010", "#8a2010", "#d84a10", "#ff8a20"], stars=0,
         body=None, clouds="puffy", cloud_col="#2a0e0e", ridge="peaks", far="#3a1010",
         near="#140808", hill="cone", lava=True, earth="#3a2a2a", cap="#ff6a1a",
         glow="#ffd040", ground="flat", ground_col="#1e1414", tuft="#ff6a1a",
         flowers=["#ff6a1a", "#ffd040"], weather="embers", weather_cols=["#ff8a20", "#ffd040"],
         flag="#f4f4f4", logo=["#fff8c0", "#ffb01e", "#e8301e"], btn="#6a1e14", ink="#0a0404",
         props="dead", fg_props="dead"),
    dict(name="Mushroom Grove", group="Biomes", layout="left",
         sky=["#1a0e3a", "#2e1a5a", "#4a2a80", "#7a3aa0", "#c05ab8"], stars=40,
         star_cols=["#ffffff", "#5af0e0"], body=("moon", 54, 42, 18, "#a0fff0"), clouds=None,
         far="#3a2470", near="#1a0e3a", props="mushroom", fg_props="mushroom",
         prop_acc="#5af0e0", earth="#4a2e5a", cap="#5af0c0", cap_thick=2,
         ground_col="#2a1a5a", tuft="#5af0c0", flowers=["#5af0e0", "#ff8af0"],
         weather="spores", weather_cols=["#5af0e0", "#ff8af0"], glow="#5af0e0",
         flag="#ff8af0", logo=["#ffffff", "#8af0e0", "#c05ab8"], btn="#5a3a90", ink="#0e0620"),
    dict(name="Crystal Cavern", group="Biomes",
         sky=["#06040e", "#0e0a20", "#181238", "#241c58", "#34307a"], stars=50,
         star_cols=["#7ad8ff", "#c8a0ff"], body=None, clouds=None, ceiling=True,
         far="#1c1848", near="#0a0818", props="crystal", fg_props="crystal",
         prop_acc="#7ad8ff", earth="#3a3460", cap="#8ae8ff", ground="flat",
         ground_col="#141030", tuft="#8ae8ff", flowers=["#7ad8ff", "#c8a0ff", "#ffffff"],
         weather="spores", weather_cols=["#7ad8ff"], glow="#c8a0ff", flag="#ffd23a",
         logo=["#ffffff", "#8ae8ff", "#6a5ae0"], btn="#2e2a6a", ink="#04030a"),
    dict(name="Tidepool Coast", group="Biomes", layout="center",
         sky=["#2a9ae0", "#5ac0f0", "#9ae0f8", "#d8f4fc", "#fff4d0"], stars=0,
         body=("sun", 36, 36, 15, "#fff6b0"), clouds="puffy", cloud_col="#ffffff",
         ridge="flat", far="#3aa8d8", near="#1a78b8", props=None, fg_props="palm",
         prop_acc="#2ea04a", earth="#d8b070", cap="#8ad860", ground="flat",
         ground_col="#ecd08a", tuft="#fff0b8", flowers=["#ffffff", "#ff9a8a", "#5ac0f0"],
         flag="#ff5a3a", logo=["#ffffff", "#fff0a0", "#ff8a3a"], btn="#2a8ac8"),
    dict(name="Savanna Gold", group="Biomes",
         sky=["#c8501e", "#e8782a", "#f4a03a", "#fac85a", "#ffe890"], stars=0,
         body=("sun", 50, 96, 36, "#ff6a2a"), clouds="thin", far="#b8742a", near="#5a3a14",
         props="acacia", fg_props="acacia", earth="#8a5a2a", cap="#d8b040",
         ground_col="#a88a2a", tuft="#e8c850", flowers=["#fff0a0", "#d8b040"],
         flag="#1e6ac8", logo=["#fff8d0", "#ffc83a", "#c8501e"]),
    dict(name="Red Mesa", group="Biomes", layout="left",
         sky=["#3a2a6a", "#7a4a8a", "#c86a6a", "#f0a070", "#ffd8a0"], stars=24,
         body=("sun", 50, 100, 26, "#ffe0a0"), ridge="mesa", far="#b85a4a", near="#6a2a2a",
         hill="mesa", strata="bands", earth="#b0502e", cap="#e89a5a", ground="flat",
         ground_col="#8a3a24", tuft="#e89a5a", flowers=["#e89a5a", "#5a8a3a"],
         props="cactus", fg_props="cactus", prop_acc="#5a8a3a", flag="#2ab8b8",
         logo=["#fff0d0", "#f0a04a", "#b83a2a"], btn="#8a3a2a"),
    dict(name="Bamboo Thicket", group="Biomes", layout="left",
         sky=["#5a8a6a", "#8ab08a", "#b8d0a0", "#e0e8c0", "#f8f4e0"], stars=0,
         body=("sun", 56, 98, 26, "#fffbe0"), clouds=None, far="#7aa884", near="#2e5a3a",
         props="bamboo", fg_props="bamboo", prop_acc="#5aa83a", earth="#6a5434", cap="#8ad04a",
         ground_col="#3a7a2e", tuft="#a0e05a", flowers=["#ffffff", "#ffd23a"],
         weather="leaves", weather_cols=["#6ab04a", "#a8d06a"], flag="#d8301e",
         logo=["#ffffff", "#d8f08a", "#4a9a3a"], btn="#3a6a3a", ink="#12200e"),
    dict(name="Cloud Kingdom", group="Biomes", layout="center",
         sky=["#2a4ab8", "#4a7ad8", "#8ab0f0", "#c8dcfc", "#fff0e0"], stars=14,
         star_cols=["#ffffff", "#c8dcfc"], body=("sun", 286, 34, 15, "#fff6c0"),
         clouds="puffy", cloud_col="#ffffff", ridge="dunes", far="#c8d8f8", near="#ffffff",
         earth="#7a6a5a", cap="#8ae06a", cap_thick=2, ground="flat", ground_col="#f4f8ff",
         tuft="#ffffff", flowers=["#c8dcfc"], flag="#ffd23a",
         logo=["#ffffff", "#ffe8a0", "#f0a04a"], btn="#6a8ad8", ink="#1a2a5a"),
    dict(name="Salt Flats", group="Biomes",
         sky=["#5a4a9a", "#9a6ab8", "#e88ab0", "#ffb8a0", "#ffe8c8"], stars=20,
         body=("sun", 46, 100, 26, "#fff8f0"), ridge="peaks", far="#a87ab8", near="#6a4a8a",
         earth="#c8b8c0", cap="#ffffff", cap_thick=2, ground="flat", ground_col="#f0e8f0",
         tuft="#ffffff", flowers=["#ffd8e8", "#c8b8c0"], flag="#e8386a",
         logo=["#ffffff", "#ffc0d8", "#9a5ac8"], btn="#9a6ab8", ink="#2a1e4a"),
    dict(name="Highland Moor", group="Biomes", layout="left",
         sky=["#5a6272", "#7a8490", "#a0a8ac", "#c4c8c0", "#e0dcc8"], stars=0, body=None,
         clouds="puffy", cloud_col="#8a94a0", far="#6a7a80", near="#3a4a44",
         props="ruin", fg_props="ruin", earth="#5a4a3a", cap="#9a78b8", cap_thick=2,
         ground_col="#4a5a3a", tuft="#a88ac8", flowers=["#c8a0e0", "#e8d8f0"],
         weather="fog", weather_cols=["#e0dcc8"], flag="#2a5aa8",
         logo=["#ffffff", "#d8c0f0", "#7a5aa8"], btn="#5a5a7a", ink="#16181e"),
    dict(name="Lavender Fields", group="Biomes",
         sky=["#6a4a9a", "#a868b0", "#e890a8", "#ffc098", "#ffe8b0"], stars=8,
         body=("sun", 46, 98, 28, "#fff0c0"), far="#9a78c0", near="#5a3a8a",
         props="windmill", fg_props="windmill", prop_acc="#f4ecff", earth="#7a5a4a",
         cap="#b88af0", cap_thick=2, ground_col="#7a4ab8", tuft="#c8a0ff",
         flowers=["#e8d0ff", "#ffffff"], flag="#ffd23a",
         logo=["#ffffff", "#e8c8ff", "#8a4ad8"], btn="#7a4ab8", ink="#22123a"),
    dict(name="Coral Shallows", group="Biomes", layout="center",
         sky=["#0a6a9a", "#1a9ac0", "#3ac8d8", "#8ae8e0", "#d8fff0"], stars=0, body=None,
         clouds=None, far="#2aa8c0", near="#0a6a8a", props="coral", fg_props="coral",
         prop_acc="#ff6a8a", earth="#d8b888", cap="#ff8a6a", cap_thick=2, ground="flat",
         ground_col="#e8d0a0", tuft="#fff0c8", flowers=["#ff6a8a", "#ffd23a", "#a05af0"],
         weather="bubbles", weather_cols=["#ffffff"], flag="#ffd23a",
         logo=["#ffffff", "#fff0a0", "#ff6a5a"], btn="#1a8ab0", ink="#06283a"),

    # ---- genres
    dict(name="Hallow Hill", group="Genres",
         sky=["#0a0612", "#1a0e2a", "#34184a", "#5a2260", "#8a3a5a"], stars=50,
         star_cols=["#ffe0a0", "#c88aff"], body=("moon", 50, 46, 24, "#ff9a2a"),
         far="#2a143a", near="#0e0816", props="grave", fg_props="dead",
         earth="#3a2a3a", cap="#5a7a3a", ground_col="#1a2a1a", tuft="#5a7a3a",
         flowers=["#ff9a2a", "#c88aff"], weather="fireflies", weather_cols=["#ff9a2a"],
         glow="#ff9a2a", flag="#8a3ac8", logo=["#fff0a0", "#ff9a2a", "#8a3ac8"],
         btn="#4a226a", ink="#08040e"),
    dict(name="Neon Grid", group="Genres",
         sky=["#0a0420", "#1e0a48", "#4a1070", "#901a8a", "#e0308a", "#ff7a5a"], stars=60,
         star_cols=["#ffffff", "#3af0ff"], body=("stripes", 112, 80, 40, "#ffd23a"),
         clouds=None, ridge="peaks", far="#3a1070", near="#12062a", snowcaps="#ff3ac0",
         earth="#1e0e44", cap="#3af0ff", ground="grid", ground_col="#0a0420",
         grid_col="#ff3ac0", tuft="#3af0ff", flowers=[], glow="#ff3ac0", flag="#3af0ff",
         logo=["#ffffff", "#3af0ff", "#ff3ac0"], logo_small=["#ffffff", "#ffd0f4", "#ff8ad8"],
         btn="#5a1a8a", ink="#060212"),
    dict(name="Last Outpost", group="Genres",
         sky=["#3a3a2a", "#5a5438", "#8a7a48", "#b89a58", "#d8b870"], stars=0,
         body=("sun", 48, 98, 28, "#e8e0a0"), clouds="thin", ridge="city", far="#6a6040",
         near="#2e2a1c", prop_acc="#e8c84a", props="ruin", fg_props="ruin",
         earth="#5a4a30", cap="#8a8a3a", ground="flat", ground_col="#4a4428", tuft="#8a8a3a",
         flowers=["#8a8a3a", "#b89a58"], weather="ash", weather_cols=["#d8c890"],
         flag="#d87a1e", logo=["#f4ecc0", "#c8a84a", "#7a5a1e"], btn="#5a5030", ink="#14120a"),
    dict(name="Brass Works", group="Genres", layout="left",
         sky=["#2a1a0e", "#4a2e16", "#7a4a20", "#b8782e", "#e8b050"], stars=0,
         body=("sun", 54, 96, 28, "#ffd880"), clouds="puffy", cloud_col="#4a2e16",
         ridge="city", far="#6a4420", near="#2a1a0e", prop_acc="#ffc84a", props="chimney",
         fg_props="chimney", earth="#5a3a1e", cap="#b8a040", ground_col="#3a2a14",
         tuft="#b8a040", flowers=["#ffc84a", "#e87a2a"], weather="embers",
         weather_cols=["#ffb84a"], flag="#8a1e1e", logo=["#fff4c0", "#e8b040", "#8a5a1e"],
         btn="#8a5a22", ink="#140c06"),
    dict(name="Storybook", group="Genres",
         sky=["#b8a0f0", "#d8b0f0", "#f4c0e0", "#ffd8d0", "#fff0c8"], stars=22,
         star_cols=["#ffffff", "#fff0a0"], body=("sun", 48, 40, 15, "#fff8d8"),
         clouds="puffy", cloud_col="#ffffff", far="#c0a0e0", near="#8ac8a0", props="castle",
         fg_props="tree", prop_acc="#ffb0d8", earth="#b88a6a", cap="#a8f0a0",
         ground_col="#78d098", tuft="#c8ffb8", flowers=["#ffffff", "#ffb0d8", "#fff0a0", "#b8a0f0"],
         weather="petals", weather_cols=["#ffffff", "#fff0a0"], flag="#f06aa8",
         logo=["#ffffff", "#ffd0f0", "#b88af0"], btn="#a880e0", ink="#3a2a5a"),
    dict(name="Xeno Colony", group="Genres", layout="left",
         sky=["#06141a", "#0a2a30", "#0e4a48", "#2a7a5a", "#8ac060", "#e8f08a"], stars=70,
         star_cols=["#ffffff", "#f04a9a"], body=("planet", 60, 44, 24, "#e8a05a"),
         body2=("moon", 108, 22, 6, "#c8f0ff"), clouds=None, ridge="peaks", far="#1a5a50",
         near="#0a2020", props="crystal", fg_props="crystal", prop_acc="#f04a9a",
         earth="#5a2a6a", cap="#f04a9a", cap_thick=2, ground_col="#3a1a4a", tuft="#f04a9a",
         flowers=["#5af0e0", "#e8f08a"], weather="spores", weather_cols=["#e8f08a"],
         glow="#5af0e0", flag="#5af0e0", logo=["#ffffff", "#e8f08a", "#2ab8a0"],
         btn="#1e5a5a", ink="#040e12"),
    dict(name="Abyssal Mound", group="Genres", layout="center",
         sky=["#021020", "#04203a", "#083a5a", "#0e5a7a", "#1e8a9a"], stars=0, body=None,
         clouds=None, far="#0a3a5a", near="#041828", props="kelp", fg_props="kelp",
         prop_acc="#2ad0a0", earth="#3a4a5a", cap="#4ad0a0", cap_thick=2, ground="flat",
         ground_col="#0e2a3a", tuft="#4ad0a0", flowers=["#ff6a8a", "#ffb04a", "#c8f0ff"],
         weather="bubbles", weather_cols=["#c8f0ff"], glow="#5af0ff", flag="#ffb04a",
         logo=["#ffffff", "#8af0f0", "#1e8ac8"], btn="#0e4a6a", ink="#010810"),
    dict(name="Sugar Rush", group="Genres",
         sky=["#ff9ac8", "#ffb8d8", "#ffd8e8", "#fff0f0", "#e8fff8"], stars=0,
         body=("sun", 48, 38, 15, "#fff6a0"), clouds="puffy", cloud_col="#ffffff",
         far="#f8a0d0", near="#c060b0", props="lollipop", fg_props="lollipop",
         prop_acc="#ff5a8a", earth="#8a4a2a", cap="#ff8ac0", cap_thick=4, ground="flat",
         ground_col="#7ae0c0", tuft="#c8fff0",
         flowers=["#ff5a8a", "#ffd23a", "#5ac0ff", "#ffffff"], weather="petals",
         weather_cols=["#ff5a8a", "#ffd23a", "#5ac0ff"], glow="#fff6a0", flag="#5ac0ff",
         logo=["#ffffff", "#ffb0d8", "#ff4a9a"], btn="#e85aa0", ink="#4a1a3a"),
    dict(name="Ink Wash", group="Genres", layout="left",
         sky=["#e0d2b0", "#e8dcc0", "#f0e4cc", "#f4ead8", "#f8f0e0"], stars=0,
         body=("disc", 56, 56, 22, "#d8301e"), clouds="thin", cloud_col="#b8a888",
         ridge="peaks", far="#b0a894", near="#4a4844", props="pine", fg_props="pine",
         earth="#6a6862", cap="#2a2a2a", ground="flat", ground_col="#262626", tuft="#4a4844",
         flowers=["#d8301e"], weather="petals", weather_cols=["#d8301e", "#e88a7a"],
         glow="#d8301e", flag="#d8301e", logo=["#f04a30", "#d8301e", "#8a1a10"],
         logo_small=["#5a5650", "#3a3834", "#1e1e1e"], btn="#4a4844", ink="#141414"),
    dict(name="Royal Banner", group="Genres", layout="center",
         sky=["#1a2a6a", "#2a4a9a", "#4a7ac8", "#8ab0e8", "#d0e4f8"], stars=0,
         body=("sun", 286, 38, 15, "#ffe070"), clouds="puffy", cloud_col="#ffffff",
         far="#4a6aa8", near="#2a5a3a", props="castle", fg_props="castle", prop_acc="#ffd23a",
         earth="#6a4a2e", cap="#5ac04a", ground_col="#2e7a2e", tuft="#7ad85a",
         flowers=["#ffd23a", "#ffffff", "#d83a3a"], flag="#8a2ac8",
         logo=["#fff8c0", "#ffd23a", "#c8821e"], btn="#2a3a9a"),
    dict(name="Lunar Outpost", group="Genres", layout="left",
         sky=["#000004", "#04040c", "#080814", "#0c0c1c", "#141428"], stars=130,
         star_cols=["#ffffff", "#8a9ac8"], body=("globe", 58, 40, 20, "#3a8ae0"), clouds=None,
         ridge="peaks", far="#6a6a74", near="#3a3a44", props="dish", fg_props="dish",
         prop_acc="#e8e8f0", earth="#8a8a94", cap="#c8c8d0", ground="flat",
         ground_col="#5a5a64", tuft="#c8c8d0", flowers=["#3a3a44", "#8a8a94"],
         glow="#5ac8ff", flag="#e8e8f0", logo=["#ffffff", "#b8d8ff", "#3a7ad8"],
         btn="#3a4a6a", ink="#06060a"),
    dict(name="Blood Moon", group="Genres",
         sky=["#0a0204", "#1e0408", "#3a080c", "#6a0e12", "#a81a1a"], stars=30,
         star_cols=["#ffb0a0", "#a83a3a"], body=("moon", 52, 46, 26, "#e8301e"),
         far="#3a0a10", near="#100406", props="grave", fg_props="dead",
         earth="#2a1a1e", cap="#6a1e22", ground_col="#14080a", tuft="#6a1e1e",
         flowers=["#a81a1a"], weather="fog", weather_cols=["#a81a1a"], glow="#ff3a2a",
         flag="#e8e0d0", logo=["#ffd0c0", "#e8301e", "#6a0a0a"], btn="#5a1014", ink="#040102"),
    dict(name="Neon Rain", group="Genres", layout="center",
         sky=["#06081a", "#0e1232", "#1a1e52", "#3a2a7a", "#7a3a9a"], stars=0, body=None,
         clouds=None, ridge="city", far="#2a2a6a", near="#0a0c22", prop_acc="#3af0ff",
         earth="#2a2a44", cap="#ff3ac0", ground="flat", ground_col="#0e1024", tuft="#3af0ff",
         flowers=["#ff3ac0", "#3af0ff", "#ffe83a"], weather="rain", weather_cols=["#8ab8ff"],
         glow="#3af0ff", flag="#ffe83a", logo=["#ffffff", "#ff7ad8", "#7a3af0"],
         btn="#2a2a6a", ink="#03040c"),
    dict(name="Pyramid Dusk", group="Genres", layout="left",
         sky=["#1e1a4a", "#4a2a6a", "#a8485a", "#e8884a", "#ffd070"], stars=34,
         body=("sun", 54, 100, 30, "#ffe8a0"), ridge="dunes", far="#b87a4a", near="#6a3a2a",
         props="pyramid", fg_props="palm", prop_acc="#2a7a3a", hill="pyramid", strata="bands",
         earth="#d8a85a", cap="#f4d888", ground="flat", ground_col="#c8904a", tuft="#f4d888",
         flowers=["#f4d888", "#8a5a2a"], flag="#1e9ab8",
         logo=["#fff4c0", "#ffc83a", "#c86a1e"], btn="#a86a2a"),
    dict(name="Noir Alley", group="Genres",
         sky=["#0a0a0a", "#1a1a1a", "#2e2e2e", "#484848", "#6a6a6a"], stars=0,
         body=("moon", 50, 40, 16, "#f0f0f0"), clouds="thin", cloud_col="#3a3a3a",
         ridge="city", far="#3a3a3a", near="#0e0e0e", prop_acc="#e8e8d0", earth="#3a3a3a",
         cap="#8a8a8a", ground="flat", ground_col="#141414", tuft="#8a8a8a", flowers=[],
         weather="rain", weather_cols=["#b0b0b0"], glow="#e8e8d0", flag="#d81e1e",
         logo=["#ffffff", "#c8c8c8", "#6a6a6a"], btn="#3a3a3a", ink="#000000"),
    dict(name="Vapor Plaza", group="Genres", layout="left",
         sky=["#ff9ad8", "#ffb0e0", "#c8b0f8", "#98d0f8", "#a8f8f0"], stars=24,
         star_cols=["#ffffff", "#fff4c0"], body=("disc", 56, 50, 20, "#fff4c0"), clouds=None,
         ridge="peaks", far="#c8a0f0", near="#8a7ae0", props="palm", fg_props="palm",
         prop_acc="#2ac8b8", earth="#f0a8d0", cap="#a8f8f0", ground="grid",
         ground_col="#5a3a9a", grid_col="#5ae0e0", tuft="#ffffff", flowers=[],
         glow="#fff4c0", flag="#ffe83a", logo=["#ffffff", "#a8f8f0", "#ff7ad8"],
         btn="#b88af0", ink="#3a2a6a"),

    # ---- holidays and festivals
    dict(name="Winter Lights", group="Holidays",
         sky=["#0a1230", "#12204a", "#1e346a", "#2e4a8a", "#4a6aa8"], stars=70,
         star_cols=["#ffffff", "#ffe8a0"], body=("moon", 50, 38, 15, "#f4f8ff"), clouds=None,
         far="#2e4a8a", near="#16244a", props="pine", fg_props="pine", prop_acc="#ffd23a",
         earth="#5a6488", cap="#ffffff", cap_thick=4, ground="flat", ground_col="#d0e0f4",
         tuft="#ffffff", flowers=["#ff4a4a", "#ffd23a", "#4ac8ff", "#5ae05a"],
         weather="snow", weather_cols=["#ffffff"], glow="#ffb84a", flag="#d81e2e",
         logo=["#ffffff", "#ff8a7a", "#c81e2e"], btn="#1e7a3a", ink="#0a1020"),
    dict(name="Lantern Festival", group="Holidays", layout="left",
         sky=["#1a0a1e", "#3a1030", "#6a1a3a", "#a82a3a", "#e8603a"], stars=30,
         star_cols=["#ffe8a0", "#ff9a5a"], body=("moon", 56, 38, 17, "#ffe8a0"), clouds="thin",
         far="#5a1a34", near="#1e0a18", props="lantern", fg_props="lantern",
         prop_acc="#ffb03a", earth="#5a2a2a", cap="#7a9a3a", ground_col="#2a2a1e",
         tuft="#7a9a3a", flowers=["#ffb03a", "#e8301e"], weather="fireflies",
         weather_cols=["#ffb03a", "#ff6a3a"], glow="#ffb03a", flag="#ffd23a",
         logo=["#fff0a0", "#ffb01e", "#d8301e"], btn="#a8261e", ink="#0e0408"),
    dict(name="Pumpkin Patch", group="Holidays",
         sky=["#2a1640", "#5a2a5a", "#a8483a", "#e8782a", "#ffb84a"], stars=20,
         body=("sun", 46, 100, 30, "#ffa03a"), far="#7a3a3a", near="#3a1e1e", props="dead",
         fg_props="pumpkin", prop_acc="#f07a1e", earth="#5e3e22", cap="#7a8a2a",
         ground_col="#4a5a1e", tuft="#9aaa3a", flowers=["#f07a1e", "#ffd23a"],
         weather="leaves", weather_cols=["#f07a1e", "#c83a1e"], flag="#f07a1e",
         logo=["#fff0a0", "#ff9a1e", "#b8501e"], btn="#8a4a1e"),
    dict(name="Fireworks Night", group="Holidays",
         sky=["#060a20", "#0c1438", "#162458", "#243a7a", "#3a5a9a"], stars=40, body=None,
         clouds=None, ridge="city", far="#1e2e62", near="#0a1028", prop_acc="#ffe070",
         earth="#4a3a4a", cap="#4a9a5a", ground_col="#14303a", tuft="#4a9a5a",
         flowers=["#ffe070"], weather="fireworks",
         weather_cols=["#ff5a8a", "#ffe070", "#5ae0ff", "#8aff7a", "#c88aff"], flag="#e8301e",
         logo=["#ffffff", "#ffe070", "#ff5a8a"], btn="#2a3a8a", ink="#04060e"),
    dict(name="Sweetheart", group="Holidays", layout="center",
         sky=["#ff7aa8", "#ff9ab8", "#ffc0d0", "#ffe0e0", "#fff4e8"], stars=0,
         body=("sun", 36, 36, 14, "#ffffff"), clouds="puffy", cloud_col="#ffffff",
         far="#ff9ab8", near="#d84a7a", props="tree", fg_props="tree", prop_acc="#ff4a7a",
         earth="#9a5a4a", cap="#ff8ab0", cap_thick=2, ground_col="#d85a8a", tuft="#ffb0d0",
         flowers=["#ffffff", "#e8204a"], weather="hearts", weather_cols=["#e8204a", "#ffffff"],
         glow="#fff0a0", flag="#e8204a", logo=["#ffffff", "#ff8ab0", "#e8204a"],
         btn="#d8386a", ink="#4a1228"),
    dict(name="Clover Day", group="Holidays",
         sky=["#3a9ad8", "#6abcec", "#a8dcf4", "#d8f4f0", "#f0ffd8"], stars=0,
         body=("sun", 30, 30, 13, "#fff6b0"), clouds="puffy", cloud_col="#ffffff",
         rainbow=(112, 124, 72), far="#5ab87a", near="#1e7a3a", props="tree", fg_props="tree",
         prop_acc="#2a9a3a", earth="#6a4a2a", cap="#3ad04a", cap_thick=2,
         ground_col="#1e8a3a", tuft="#5ae05a", flowers=["#ffd23a", "#ffffff"], flag="#ffd23a",
         logo=["#fff8c0", "#ffd23a", "#2a9a3a"], btn="#1e7a3a", ink="#0e2a12"),

    # ---- retro hardware looks: an existing theme pushed through a fixed palette
    dict(name="Dot Matrix Green", group="Retro", base="Summer Meadow", retro=("lum", GB)),
    dict(name="Four Colour Siege", group="Retro", base="Sunset Stand", retro=("lum", CGA)),
    dict(name="Red Scope", group="Retro", base="Midnight Watch", retro=("lum", REDSCOPE)),
    dict(name="One Bit", group="Retro", base="High Noon", retro=("lum", ONEBIT)),
    dict(name="Sixteen Colour Micro", group="Retro", base="Sunset Stand", retro=("near", C64)),
    dict(name="Amber Phosphor", group="Retro", base="Blue Hour", retro=("lum", AMBER)),
    dict(name="Blueprint", group="Retro", base="Royal Banner", retro=("lum", BLUEPRINT)),
    dict(name="Sepia Print", group="Retro", base="Autumn Harvest", retro=("lum", SEPIA)),
]


def resolve(t):
    out = dict(DEFAULT)
    if "base" in t:
        out.update(next(x for x in THEMES if x["name"] == t["base"]))
        out.pop("base", None)
    out.update(t)
    return out


# ---------------------------------------------------------------- drawing

def smooth_line(seed, base, amp, step, linear=False):
    rnd = random.Random(seed)
    pts = [base + rnd.uniform(-amp, amp) for _ in range(W // step + 3)]
    out = []
    for x in range(W):
        i, f = divmod(x / step, 1)
        i = int(i)
        t = f if linear else (1 - math.cos(f * math.pi)) / 2
        out.append(round(pts[i] * (1 - t) + pts[i + 1] * t))
    return out


def ridge_line(style, layer, base):
    seed = layer + 1
    if style == "peaks":
        return smooth_line(seed * 13, base - 6, 13 - layer * 3, 20 - layer * 3, linear=True)
    if style == "dunes":
        return smooth_line(seed * 5, base, 8, 58 - layer * 8)
    if style == "flat":
        return [base + layer * 2] * W
    if style in ("city", "mesa"):
        rnd = random.Random(seed * 31)
        out = []
        while len(out) < W:
            if style == "city":
                w, top = rnd.randint(5, 13), base - rnd.randint(2, 22 - layer * 4)
            else:
                w, top = rnd.randint(16, 44), base - rnd.choice([0, 0, 7, 13, 19])
            out += [top] * w
        return out[:W]
    return smooth_line(seed, base, 7 - layer, [34, 26, 40][layer])


def draw_prop(d, kind, x, y, s, col, acc):
    c, a = hexc(col), hexc(acc or col)
    s = max(6, s)
    tw = max(1, s // 12)
    if kind == "pine":
        d.rectangle([x - tw // 2, y - s * 0.2, x + (tw + 1) // 2, y], fill=c)
        for i in range(3):
            top = y - s + i * s * 0.24
            hw = s * (0.13 + 0.09 * i)
            d.polygon([(x, top), (x - hw, top + s * 0.4), (x + hw, top + s * 0.4)], fill=c)
            if acc:
                d.line([(x, top), (x - hw * 0.6, top + s * 0.24)], fill=a)
    elif kind == "tree":
        d.rectangle([x - tw // 2, y - s * 0.55, x + (tw + 1) // 2, y], fill=c)
        for dx, dy, r in [(0, -0.7, 0.3), (-0.2, -0.58, 0.22), (0.2, -0.6, 0.24)]:
            cx, cy, rr = x + dx * s, y + dy * s, r * s
            d.ellipse([cx - rr, cy - rr, cx + rr, cy + rr], fill=a)
        if acc:
            d.arc([x - s * 0.3, y - s, x + s * 0.3, y - s * 0.4], 200, 320, fill=hexc(lighten(acc, 0.35)))
    elif kind == "dead":
        d.line([(x, y), (x, y - s * 0.95)], fill=c, width=tw + 1)
        for x0, y0, x1, y1 in [(0, .45, -.3, .82), (0, .6, .32, .98), (-.3, .82, -.36, 1.0),
                               (.32, .98, .2, 1.1), (0, .3, .2, .5), (-.14, .64, -.04, .86)]:
            d.line([(x + x0 * s, y - y0 * s), (x + x1 * s, y - y1 * s)], fill=c, width=1)
    elif kind == "palm":
        top = (x + s * 0.18, y - s * 0.86)
        d.line([(x, y), (x + s * 0.06, y - s * 0.45), top], fill=c, width=tw + 1)
        for dx, dy in [(-.42, .08), (.42, .1), (-.32, -.14), (.34, -.12), (0, -.2), (-.2, .22), (.24, .24)]:
            d.line([top, (top[0] + dx * s * 0.5, top[1] + min(dy, 0) * s - 1),
                    (top[0] + dx * s, top[1] + dy * s)], fill=a, width=tw)
    elif kind == "cactus":
        d.rectangle([x - tw, y - s, x + tw, y], fill=a)
        for sx, h0, h1 in [(-1, 0.45, 0.72), (1, 0.3, 0.62)]:
            ax = x + sx * s * 0.24
            d.line([(x, y - s * h0), (ax, y - s * h0)], fill=a, width=tw + 1)
            d.rectangle([ax - tw * (sx < 0), y - s * h1, ax + tw * (sx > 0), y - s * h0], fill=a)
        d.line([(x - tw, y - s), (x - tw, y)], fill=hexc(lighten(acc or col, 0.25)))
    elif kind == "acacia":
        d.line([(x, y), (x, y - s * 0.6)], fill=c, width=tw + 1)
        d.line([(x, y - s * 0.55), (x - s * 0.3, y - s * 0.84)], fill=c)
        d.line([(x, y - s * 0.55), (x + s * 0.32, y - s * 0.84)], fill=c)
        d.ellipse([x - s * 0.55, y - s, x + s * 0.55, y - s * 0.76], fill=a)
        d.ellipse([x - s * 0.3, y - s * 1.06, x + s * 0.34, y - s * 0.84], fill=a)
    elif kind == "mushroom":
        d.rectangle([x - s * 0.08, y - s * 0.6, x + s * 0.08, y], fill=hexc(lighten(col, 0.25)))
        d.chord([x - s * 0.42, y - s, x + s * 0.42, y - s * 0.24], 180, 360, fill=a)
        for dx, dy in [(-.2, .74), (.12, .84), (.24, .7)]:
            d.point((x + dx * s, y - dy * s), fill=hexc(lighten(acc or col, 0.6)))
        d.line([(x - s * 0.4, y - s * 0.62), (x + s * 0.4, y - s * 0.62)], fill=hexc(shade(acc or col, 0.6)))
    elif kind == "crystal":
        for ox, hh, lean in [(0, 1.0, 0.06), (-0.22, 0.6, -0.14), (0.24, 0.5, 0.16)]:
            bx = x + ox * s
            pts = [(bx - s * 0.1, y), (bx + lean * s - s * 0.04, y - hh * s), (bx + lean * s + s * 0.06, y - hh * s * 0.86),
                   (bx + s * 0.12, y)]
            d.polygon(pts, fill=hexc(mix(col, acc or col, 0.55)))
            d.line([pts[0], pts[1]], fill=a)
    elif kind == "ruin":
        for ox, hh in [(-0.3, 0.9), (0.1, 0.55), (0.42, 1.0)]:
            bx = x + ox * s
            d.rectangle([bx - s * 0.07, y - hh * s, bx + s * 0.07, y], fill=c)
            d.line([(bx - s * 0.11, y - hh * s), (bx + s * 0.11, y - hh * s)], fill=c)
        d.rectangle([x - s * 0.34, y - s * 0.9, x - s * 0.05, y - s * 0.82], fill=c)
    elif kind == "kelp":
        for ox, hh, ph in [(-0.14, 1.0, 0.0), (0.1, 0.72, 1.7), (0.26, 0.9, 3.1)]:
            for k in range(int(hh * s)):
                xx = x + ox * s + round(1.6 * math.sin(k / 3.0 + ph))
                d.line([(xx, y - k), (xx + tw, y - k)], fill=a if k % 5 else c)
    elif kind == "lollipop":
        d.line([(x, y), (x, y - s * 0.7)], fill=hexc("#ffffff"), width=tw)
        r = s * 0.26
        d.ellipse([x - r, y - s * 0.7 - r * 2, x + r, y - s * 0.7], fill=a)
        d.arc([x - r * 0.6, y - s * 0.7 - r * 1.6, x + r * 0.6, y - s * 0.7 - r * 0.4], 150, 420, fill=hexc("#ffffff"))
    elif kind == "grave":
        w = s * 0.3
        d.rectangle([x - w, y - s * 0.5, x + w, y], fill=c)
        d.chord([x - w, y - s * 0.5 - w, x + w, y - s * 0.5 + w], 180, 360, fill=c)
        d.line([(x, y - s * 0.52), (x, y - s * 0.2)], fill=hexc(lighten(col, 0.2)))
        d.line([(x - w * 0.5, y - s * 0.4), (x + w * 0.5, y - s * 0.4)], fill=hexc(lighten(col, 0.2)))
    elif kind == "castle":
        w = s * 0.2
        d.rectangle([x - w, y - s * 0.8, x + w, y], fill=c)
        for k in (-1, 0, 1):
            d.rectangle([x + k * w * 0.8 - 1, y - s * 0.8 - 2, x + k * w * 0.8 + (s > 14), y - s * 0.8], fill=c)
        d.polygon([(x - w * 0.7, y - s * 0.84), (x, y - s * 1.15), (x + w * 0.7, y - s * 0.84)], fill=hexc(shade(col, 0.8)))
        d.rectangle([x - 0.5, y - s * 0.6, x + 0.5, y - s * 0.6 + max(1, s // 8)], fill=a)
        d.line([(x, y - s * 1.15), (x, y - s * 1.32)], fill=c)
        d.rectangle([x + 1, y - s * 1.32, x + 1 + max(1, s // 7), y - s * 1.24], fill=a)
    elif kind == "bamboo":
        for ox, hh in [(-0.16, 0.85), (0.0, 1.1), (0.18, 0.7)]:
            bx = round(x + ox * s)
            d.line([(bx, y), (bx, y - hh * s)], fill=a, width=tw + 1)
            for k in range(1, 6):
                d.point((bx, y - hh * s * k / 6), fill=c)
            for sx in (-1, 1):
                d.line([(bx, y - hh * s * 0.9), (bx + sx * s * 0.16, y - hh * s * 0.9 - sx * 2)], fill=a)
                d.line([(bx, y - hh * s * 0.7), (bx - sx * s * 0.14, y - hh * s * 0.7 + 1)], fill=a)
    elif kind == "lantern":
        d.line([(x, y), (x, y - s)], fill=c, width=tw)
        d.line([(x, y - s), (x + s * 0.24, y - s)], fill=c)
        r = max(1.5, s * 0.13)
        lx, ly = x + s * 0.24, y - s + r + 2
        d.line([(lx, y - s), (lx, ly - r)], fill=c)
        d.ellipse([lx - r, ly - r * 1.2, lx + r, ly + r * 1.2], fill=a)
        d.line([(lx - r * 0.6, ly - r * 1.2), (lx + r * 0.6, ly - r * 1.2)], fill=c)
        d.line([(lx - r * 0.6, ly + r * 1.2), (lx + r * 0.6, ly + r * 1.2)], fill=c)
        d.point((lx, ly), fill=hexc(lighten(acc or col, 0.6)))
    elif kind == "pumpkin":
        rx, ry = s * 0.3, s * 0.22
        d.ellipse([x - rx, y - ry * 2, x + rx, y], fill=a)
        d.arc([x - rx * 0.5, y - ry * 2, x + rx * 0.5, y], 0, 360, fill=hexc(shade(acc or col, 0.75)))
        d.line([(x, y - ry * 2), (x + 1, y - ry * 2 - max(2, s * 0.1))], fill=hexc("#4a6a1e"), width=tw)
        d.arc([x - rx, y - ry * 2, x + rx, y], 190, 260, fill=hexc(lighten(acc or col, 0.4)))
    elif kind == "pyramid":
        d.polygon([(x - s * 0.6, y), (x, y - s * 0.75), (x + s * 0.6, y)], fill=hexc(lighten(col, 0.12)))
        d.polygon([(x, y - s * 0.75), (x + s * 0.6, y), (x + s * 0.12, y)], fill=hexc(shade(col, 0.8)))
    elif kind == "coral":
        def branch(bx, by, ang, ln, depth):
            ex, ey = bx + math.sin(ang) * ln, by - math.cos(ang) * ln
            d.line([(bx, by), (ex, ey)], fill=a, width=tw + (depth > 1))
            if depth:
                for da in (-0.6, 0.1, 0.7):
                    branch(ex, ey, ang + da, ln * 0.62, depth - 1)
            else:
                d.point((ex, ey), fill=hexc(lighten(acc or col, 0.5)))
        branch(x, y, 0.0, s * 0.38, 2)
    elif kind == "dish":
        d.line([(x, y), (x, y - s * 0.6)], fill=c, width=tw + 1)
        d.rectangle([x - s * 0.18, y - 1, x + s * 0.18, y], fill=c)
        d.chord([x - s * 0.36, y - s, x + s * 0.36, y - s * 0.36], 20, 200, fill=a)
        d.line([(x + s * 0.04, y - s * 0.7), (x + s * 0.3, y - s * 1.0)], fill=c)
        d.point((x + s * 0.3, y - s * 1.0), fill=hexc("#ff4a4a"))
    elif kind == "windmill":
        d.polygon([(x - s * 0.17, y), (x - s * 0.09, y - s * 0.72), (x + s * 0.09, y - s * 0.72), (x + s * 0.17, y)], fill=c)
        d.polygon([(x - s * 0.12, y - s * 0.72), (x, y - s * 0.86), (x + s * 0.12, y - s * 0.72)], fill=hexc(shade(col, 0.8)))
        hub = (x, y - s * 0.74)
        for ang in (0.5, 2.07, 3.64, 5.21):
            d.line([hub, (hub[0] + math.cos(ang) * s * 0.46, hub[1] + math.sin(ang) * s * 0.46)], fill=a, width=tw)
    elif kind == "chimney":
        w = max(1, s * 0.09)
        d.polygon([(x - w * 1.6, y), (x - w, y - s), (x + w, y - s), (x + w * 1.6, y)], fill=c)
        d.line([(x - w - 1, y - s), (x + w + 1, y - s)], fill=a)
        for k, (dx, dy, r) in enumerate([(0.1, 1.14, 0.1), (0.26, 1.3, 0.13), (0.46, 1.42, 0.16)]):
            d.ellipse([x + dx * s - r * s, y - dy * s - r * s, x + dx * s + r * s, y - dy * s + r * s],
                      fill=hexc(mix(col, "#888888", 0.25 + k * 0.1)))


def draw_body(im, body, sky_low, ink):
    kind, cx, cy, r, col = body
    px = im.load()
    rings = [(1.0, mix(col, sky_low, 0.4)), (0.87, col), (0.7, lighten(col, 0.35)), (0.5, lighten(col, 0.7))]
    crater = [(-0.35, -0.2, 0.22), (0.3, 0.25, 0.28), (0.1, -0.5, 0.14), (-0.3, 0.45, 0.12)]
    for y in range(max(0, cy - r - 12), min(H, cy + r + 13)):
        for x in range(max(0, cx - r * 2 - 4), min(W, cx + r * 2 + 5)):
            dx, dy = x - cx, y - cy
            dist = math.hypot(dx, dy) / r
            if kind == "planet":
                ring = math.hypot(dx / 1.9, dy / 0.42 - dx * 0.16) / r
                behind = 0.86 < ring <= 1.0 and (dy < dx * 0.07 or dist > 1)
                if dist <= 1:
                    band = int((dy / r + 1) * 4 + math.sin(dx / 5.0))
                    c = [col, lighten(col, 0.25), shade(col, 0.8), lighten(col, 0.45)][band % 4]
                    if dx + dy > r * 0.75:
                        c = shade(c, 0.6)
                    px[x, y] = hexc(c)
                if 0.86 < ring <= 1.0 and (dist > 1 or dy > dx * 0.07):
                    px[x, y] = hexc(lighten(col, 0.55 if ring > 0.93 else 0.3))
                del behind
                continue
            if dist > 1:
                continue
            if kind == "sun":
                px[x, y] = hexc(next(c for f, c in reversed(rings) if dist <= f))
            elif kind == "globe":
                c = col
                if any(math.hypot(dx / r - a * 1.2, dy / r - b) < q * 1.5 for a, b, q in crater):
                    c = "#4ab04a"
                if abs(dy / r + 0.72) < 0.09 or (abs(dy / r - 0.1 - 0.1 * math.sin(dx / 3.0)) < 0.07 and dx < 0):
                    c = "#ffffff"
                if dx + dy > r * 0.55:
                    c = shade(c, 0.45)
                px[x, y] = hexc(c)
            elif kind == "disc":
                px[x, y] = hexc(col)
            elif kind == "stripes":
                gap = dy > -r * 0.1 and int((dy + r * 0.1) % max(4, 9 - dy // 6)) < 1 + (dy > r * 0.3) + (dy > r * 0.65)
                if not gap:
                    px[x, y] = hexc(mix(col, "#ff3a8a", max(0.0, min(1.0, (dy / r + 0.7) / 1.5))))
            elif kind in ("moon", "crescent"):
                if kind == "crescent" and math.hypot(dx - r * 0.45, dy + r * 0.18) / r < 0.92:
                    continue
                c = col
                if kind == "moon":
                    if any(math.hypot(dx / r - a, dy / r - b) < q for a, b, q in crater):
                        c = shade(col, 0.82)
                    if dx + dy > r * 0.9:
                        c = shade(c, 0.8)
                    elif dist > 0.9 and dx + dy < 0:
                        c = lighten(col, 0.5)
                px[x, y] = hexc(c)


def draw_clouds(d, t, bands):
    if t["clouds"] == "thin":
        c1, c2 = t["cloud_col"] or bands[5], lighten(t["cloud_col"], 0.2) if t["cloud_col"] else bands[6]
        for cx, cy, w in [(52, 34, 34), (118, 22, 22), (270, 44, 40), (196, 60, 26), (20, 70, 28)]:
            d.rectangle([cx, cy, cx + w, cy + 1], fill=hexc(c1))
            d.rectangle([cx + 4, cy - 1, cx + w - 6, cy - 1], fill=hexc(c2))
    elif t["clouds"] == "puffy":
        col = t["cloud_col"] or "#ffffff"
        under = mix(col, bands[4], 0.45)
        for cx, cy, w in [(26, 22, 34), (112, 52, 24), (236, 26, 38), (296, 62, 24), (168, 14, 22), (70, 64, 20)]:
            for dx, dy, r in [(0, 0, 0.2), (0.24, -0.12, 0.26), (0.52, -0.04, 0.22), (0.76, 0.02, 0.16)]:
                x, y, rr = cx + dx * w, cy + dy * w, r * w
                d.ellipse([x - rr, y - rr, x + rr, y + rr], fill=hexc(col))
            d.rectangle([cx - w * 0.2, cy + w * 0.08, cx + w * 0.92, cy + w * 0.2], fill=hexc(col))
            d.line([(cx - w * 0.16, cy + w * 0.2), (cx + w * 0.88, cy + w * 0.2)], fill=hexc(under))


def draw_weather(im, t, hill_top):
    kind = t["weather"]
    if not kind:
        return
    cols = t["weather_cols"] or ["#ffffff"]
    lay = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(lay)
    rnd = random.Random(len(t["name"]) * 97 + 5)
    pick = lambda: rnd.choice(cols)
    if kind in ("storm", "rain"):
        for _ in range(170):
            x, y, n = rnd.randrange(-10, W), rnd.randrange(H), rnd.randint(3, 5)
            d.line([(x, y), (x + n // 2, y + n)], fill=hexc(pick(), 150))
        x, y, pts = 34, 0, [(34, 0)]
        while kind == "storm" and y < 104:
            x += rnd.randint(-9, 7)
            y += rnd.randint(8, 15)
            pts.append((x, y))
        if kind == "storm":
            d.line(pts, fill=hexc("#8ab0ff", 200), width=3)
            d.line(pts, fill=hexc("#ffffff"), width=1)
            d.line([pts[3], (pts[3][0] + 14, pts[3][1] + 12), (pts[3][0] + 12, pts[3][1] + 26)], fill=hexc("#ffffff"))
    elif kind == "snow":
        for _ in range(150):
            x, y = rnd.randrange(W), rnd.randrange(H)
            d.point((x, y), fill=hexc(pick(), rnd.choice([255, 255, 170])))
            if rnd.random() < 0.18:
                d.point((x + 1, y), fill=hexc(pick()))
                d.point((x, y + 1), fill=hexc(pick()))
    elif kind in ("embers", "ash"):
        for _ in range(90 if kind == "embers" else 120):
            x = rnd.randrange(W)
            y = int(H - (rnd.random() ** 1.6) * H) if kind == "embers" else rnd.randrange(H)
            d.point((x, y), fill=hexc(pick(), rnd.choice([255, 200, 140])))
            if kind == "embers" and rnd.random() < 0.25:
                d.point((x, y - 1), fill=hexc(lighten(pick(), 0.5)))
    elif kind in ("fireflies", "spores"):
        lo = 70 if kind == "fireflies" else 10
        for _ in range(26 if kind == "fireflies" else 54):
            x, y, c = rnd.randrange(4, W - 4), rnd.randrange(lo, H - 8), pick()
            if kind == "fireflies" or rnd.random() < 0.4:
                for dx, dy in [(-1, 0), (1, 0), (0, -1), (0, 1)]:
                    d.point((x + dx, y + dy), fill=hexc(c, 90))
            d.point((x, y), fill=hexc(lighten(c, 0.4)))
    elif kind in ("petals", "leaves"):
        for _ in range(70):
            x, y, c = rnd.randrange(W), rnd.randrange(H - 10), pick()
            s = rnd.choice([-1, 1])
            d.point((x, y), fill=hexc(c))
            d.point((x + s, y + 1), fill=hexc(c if kind == "petals" else shade(c, 0.8)))
            if kind == "leaves" and rnd.random() < 0.5:
                d.point((x + s * 2, y + 1), fill=hexc(c))
    elif kind == "fireworks":
        for bx, by, r in [(40, 32, 17), (98, 20, 12), (20, 68, 10), (132, 52, 9), (236, 70, 11), (300, 96, 9)]:
            c = pick()
            for k in range(16):
                ang = k * math.tau / 16
                for f, alpha in [(0.4, 255), (0.7, 230), (1.0, 170)]:
                    d.point((bx + math.cos(ang) * r * f, by + math.sin(ang) * r * f * 0.95), fill=hexc(c, alpha))
            d.point((bx, by), fill=hexc("#ffffff"))
            for k in range(r + 4, r + 22, 3):
                d.point((bx, by + k), fill=hexc(c, 110))
    elif kind == "hearts":
        for _ in range(26):
            x, y, c = rnd.randrange(W - 3), rnd.randrange(H - 14), hexc(pick())
            for dx, dy in [(0, 0), (2, 0), (0, 1), (1, 1), (2, 1), (1, 2)]:
                d.point((x + dx, y + dy), fill=c)
    elif kind == "fog":
        for _ in range(22):
            x, y, n = rnd.randrange(-40, W), rnd.randrange(92, 162), rnd.randint(40, 120)
            d.rectangle([x, y, x + n, y + rnd.randint(1, 3)], fill=hexc(pick(), rnd.choice([34, 48, 62])))
            d.rectangle([x + 8, y - 1, x + n - 12, y - 1], fill=hexc(pick(), 30))
    elif kind == "sand":
        for _ in range(80):
            x, y, n = rnd.randrange(W), rnd.randrange(60, H), rnd.randint(3, 9)
            d.line([(x, y), (x + n, y)], fill=hexc(pick(), rnd.choice([90, 140])))
    elif kind == "bubbles":
        for x0, wdt in [(40, 26), (130, 18), (210, 30), (286, 16)]:
            d.polygon([(x0, 0), (x0 + wdt, 0), (x0 + wdt - 46, H), (x0 - 60, H)], fill=hexc("#8af0ff", 22))
        for _ in range(46):
            x, y, r = rnd.randrange(W), rnd.randrange(H), rnd.choice([0, 0, 1, 1, 2])
            if r == 0:
                d.point((x, y), fill=hexc(pick(), 200))
            else:
                d.ellipse([x - r, y - r, x + r, y + r], outline=hexc(pick(), 200))
                d.point((x - r + 1, y - r + 1), fill=hexc("#ffffff"))
    im.alpha_composite(lay)


def background(t, icons):
    rnd = random.Random(7)
    center = t["layout"] == "center"
    ink = t["ink"]
    bands = ramp(t["sky"], 11)
    im = Image.new("RGBA", (W, H))
    px = im.load()
    horizon = 118
    for y in range(H):
        f = min(len(bands) - 1.001, y / horizon * (len(bands) - 1))
        i = int(f)
        for x in range(W):
            dither = 1 if (f - i) > 0.5 and (x + y) % 2 else 0
            px[x, y] = hexc(bands[min(len(bands) - 1, i + dither)])
    d = ImageDraw.Draw(im)

    if t["aurora"]:
        a = t["aurora"]
        for x in range(W):
            for k, (y0, amp, ph) in enumerate([(20, 9, 0.0), (40, 7, 2.1)]):
                top = round(y0 + amp * math.sin(x / 23.0 + ph) + 4 * math.sin(x / 7.0 + ph * 2))
                length = 16 + round(7 * math.sin(x / 11.0 + k))
                for j in range(length):
                    y = top + j
                    fade = j / length
                    if (x + k) % 3 == 0 and fade > 0.35:
                        continue
                    if fade > 0.65 and (x + y) % 2:
                        continue
                    c = a[0] if fade < 0.35 else (a[1] if fade < 0.7 else a[2])
                    if 0 <= y < H:
                        px[x, y] = hexc(mix(c, bands[2], 0.25 + fade * 0.4))

    if t["rainbow"]:
        cx, cy, r0 = t["rainbow"]
        arc = ["#e8483a", "#f08a2a", "#ffd23a", "#5ad05a", "#3a9ae8", "#8a5ad8"]
        for y in range(max(0, cy - r0 - 14), cy):
            for x in range(max(0, cx - r0 - 14), min(W, cx + r0 + 14)):
                k = int((r0 + 12 - math.hypot(x - cx, y - cy)) / 2)
                if 0 <= k < 6 and ((x + y) % 2 or k not in (0, 5)):
                    px[x, y] = hexc(mix(arc[k], bands[min(10, y * 10 // horizon)], 0.3))

    for _ in range(t["stars"]):
        x, y = rnd.randrange(W), rnd.randrange(0, 62)
        px[x, y] = hexc(t["star_cols"][0] if rnd.random() < 0.3 else t["star_cols"][-1])

    if t["clouds"] == "thin":
        draw_clouds(d, t, bands)
    for body in (t["body"], t["body2"]):
        if body:
            draw_body(im, body, bands[-1], ink)
    if t["clouds"] == "puffy":
        draw_clouds(d, t, bands)

    if t["ceiling"]:
        rc = random.Random(41)
        line = smooth_line(17, 9, 5, 16)
        x = 4
        while x < W:
            depth, half = rc.randint(8, 26), rc.randint(3, 6)
            for k in range(-half, half + 1):
                if 0 <= x + k < W:
                    line[x + k] = max(line[x + k], line[x] + round(depth * (1 - abs(k) / (half + 1))))
            x += rc.randint(11, 26)
        for x in range(W):
            for y in range(0, line[x]):
                px[x, y] = hexc(t["far"] if (line[x] - y) > 3 or (x + y) % 2 else lighten(t["far"], 0.1))
            px[x, line[x]] = hexc(lighten(t["far"], 0.3))

    # three ridges, far to near, with silhouettes standing on the nearer two
    cols = [t["far"], mix(t["far"], t["near"], 0.55), t["near"]]
    rp = random.Random(len(t["name"]) + 3)
    for layer, base in enumerate((112, 122, 132)):
        col = cols[layer]
        line = ridge_line(t["ridge"], layer, base)
        if t["props"] and layer > 0:
            x = rp.randint(2, 14)
            while x < W:
                acc = mix(t["prop_acc"], col, 0.55 if layer == 1 else 0.3) if t["prop_acc"] else None
                draw_prop(d, t["props"], x, line[x] + 1, rp.randint(8, 13) + layer * 3, col, acc)
                x += rp.randint(13, 30)
        for x in range(W):
            for y in range(line[x], H):
                px[x, y] = hexc(col)
            px[x, line[x]] = hexc(lighten(col, 0.14))
            if t["snowcaps"] and line[x] < base - 9:
                for k in range(min(3, base - 9 - line[x])):
                    if k < 2 or x % 2:
                        px[x, line[x] + k] = hexc(mix(t["snowcaps"], col, layer * 0.25))
        if t["ridge"] == "city":
            for _ in range(60):
                x = rp.randrange(W)
                y = line[x] + rp.randint(2, 12)
                if y < H:
                    px[x, y] = hexc(mix(t["prop_acc"] or "#ffd27a", col, layer * 0.3))
        if t["ridge"] == "flat":
            for _ in range(40):
                x, y = rp.randrange(W - 6), base + layer * 2 + rp.randint(2, 9)
                d.line([(x, y), (x + rp.randint(2, 6), y)], fill=hexc(lighten(col, 0.3)))

    # the hill
    hx, top, half = (160, 103, 106) if center else (112, 82, 100)
    shape = t["hill"]

    def hill_y(x):
        a = abs((x - hx) / half)
        if a > 1:
            return H + 1
        if shape == "mesa":
            return round(top + (0 if a < 0.3 else ((a - 0.3) / 0.7) ** 0.75 * 62))
        if shape == "pyramid":
            return round(top - 6 + a * 68)
        if shape == "cone":
            return round(top + (3 - a * 20 if a < 0.07 else a ** 1.12 * 62))
        return round(top + a * a * 62)

    e = [t["earth"], shade(t["earth"], 0.84), shade(t["earth"], 0.7)]
    cap, cap_dark = t["cap"], shade(t["cap"], 0.62)
    thick = t["cap_thick"]
    for x in range(W):
        y0 = hill_y(x)
        for y in range(y0, H):
            depth = y - y0
            if t["strata"] == "bands":
                k = (y // 6) % 3
                col = [e[0], e[1], lighten(e[0], 0.16)][k]
                if y % 6 == 0 and (x + y) % 2:
                    col = e[2]
            else:
                col = e[0] if depth < 14 else (e[1] if depth < 34 else e[2])
                if depth in (14, 34) and (x + y) % 2:
                    col = e[0] if depth == 14 else e[1]
            px[x, y] = hexc(col)
        if y0 < H:
            if y0 - 1 >= 0:
                px[x, y0 - 1] = hexc(ink)
            for k in range(thick):
                if y0 + k < H:
                    px[x, y0 + k] = hexc(cap)
            drip = thick + (1 if thick > 1 and x % 5 in (0, 1) else 0) + (1 if thick > 2 and x % 7 == 3 else 0)
            for k in range(thick, drip):
                if y0 + k < H:
                    px[x, y0 + k] = hexc(cap)
            if y0 + drip < H and x % 3:
                px[x, y0 + drip] = hexc(cap_dark)
            if y0 + drip + 1 < H and x % 5 == 0:
                px[x, y0 + drip + 1] = hexc(cap_dark)
    if t["strata"] != "bands":
        for x in range(hx + 8, hx + 70):
            y0 = hill_y(x)
            for k in range(3 + thick, 9 + thick):
                if (x + k) % 3 == 0 and y0 + k < H:
                    px[x, y0 + k] = hexc(lighten(t["earth"], 0.18))
    rnd = random.Random(11)
    for _ in range(40):
        x = rnd.randrange(hx - 80, hx + 80)
        y = hill_y(x) + rnd.randrange(8 + thick, 50)
        if y < H - 14:
            px[x, y] = hexc(rnd.choice([lighten(t["earth"], 0.45), e[2], lighten(t["glow"], 0.3)]))
    if t["lava"]:
        rl = random.Random(3)
        for sx in (-5, 3, 9):
            x, y = hx + sx, hill_y(hx + sx) + 1
            for k in range(rl.randint(22, 44)):
                x += rl.choice([-1, 0, 0, 1]) + (1 if sx > 0 and k % 3 == 0 else 0) - (1 if sx < 0 and k % 3 == 0 else 0)
                y = max(y + 1, hill_y(x) + 1)
                if y < H:
                    px[x, y] = hexc(t["glow"] if k % 4 else lighten(t["glow"], 0.5))
                    px[x + 1, y] = hexc(t["cap"])
    d.ellipse([hx - 9, top + 22, hx + 9, top + 38], fill=hexc(ink))
    d.ellipse([hx - 7, top + 24, hx + 7, top + 38], fill=hexc(shade(t["earth"], 0.3)))
    d.ellipse([hx - 4, top + 30, hx + 4, top + 38], fill=hexc(t["glow"]))
    d.ellipse([hx - 2, top + 33, hx + 2, top + 38], fill=hexc(lighten(t["glow"], 0.45)))

    # flag on the summit
    fy = hill_y(hx - 2) if shape == "cone" else top
    fx = hx - 2 if shape == "cone" else hx
    d.line([(fx, fy - 22), (fx, fy - 1)], fill=hexc(ink), width=3)
    d.line([(fx, fy - 21), (fx, fy - 1)], fill=hexc("#b5873e"))
    d.polygon([(fx + 2, fy - 22), (fx + 17, fy - 20), (fx + 13, fy - 16), (fx + 17, fy - 11), (fx + 2, fy - 12)],
              fill=hexc(t["flag"]), outline=hexc(ink))
    d.line([(fx + 4, fy - 20), (fx + 10, fy - 19)], fill=hexc(lighten(t["flag"], 0.35)))

    # foreground
    gb = 152 if center else 164
    ground = [max(g, gb - 14) for g in smooth_line(5, gb, 3, 22)]
    gc = t["ground_col"]
    if t["fg_props"]:
        dark = mix(gc, ink, 0.6)
        spots = [(8, 30), (27, 22)] + ([(296, 24), (312, 32)] if center else [])
        for x, s in spots:
            acc = mix(t["prop_acc"], ink, 0.25) if t["prop_acc"] else None
            draw_prop(d, t["fg_props"], x, ground[x] + 2, s, dark, acc)
    for x in range(W):
        g = ground[x]
        for y in range(g, H):
            px[x, y] = hexc(gc if y - g < 5 else shade(gc, 0.85))
        px[x, g] = hexc(t["tuft"])
        if t["ground"] == "tuft":
            if x % 4 == 0:
                px[x, g - 1] = hexc(t["tuft"])
            if x % 7 == 3:
                px[x, g - 2] = hexc(lighten(t["tuft"], 0.25))
        elif t["ground"] == "flat":
            if x % 9 < 5 and g + 4 < H:
                px[x, g + 3 + (x // 9) % 3] = hexc(lighten(gc, 0.14))
    if t["ground"] == "grid":
        gcol = hexc(t["grid_col"])
        g0 = min(ground)
        for x in range(W):
            for y in range(ground[x], H):
                px[x, y] = hexc(gc)
            px[x, ground[x]] = hexc(t["tuft"])
        y, step = max(ground) + 3, 3.0
        while y < H:
            d.line([(0, round(y)), (W, round(y))], fill=gcol)
            step *= 1.5
            y += step
        for k in range(-16, 17):
            x0 = 160 + k * 9
            x1 = 160 + k * 34
            d.line([(x0, max(ground) + 1), (x1, H)], fill=gcol)
        del g0
    rnd = random.Random(21)
    if t["flowers"]:
        for _ in range(16):
            x = rnd.randrange(4, W - 4)
            y = ground[x] - 2 if t["ground"] == "tuft" else ground[x] + rnd.randint(1, 8)
            if y + 1 < H:
                px[x, y] = hexc(rnd.choice(t["flowers"]))
                if t["ground"] == "tuft":
                    px[x, y + 1] = hexc(shade(t["tuft"], 0.7))

    # ants marching up the slope, two beetles arriving
    def blit(rows, ox, oy, flip=False):
        for j, row in enumerate(rows):
            row = row[::-1] if flip else row
            for i, ch in enumerate(row):
                if ch != "." and 0 <= ox + i < W and 0 <= oy + j < H:
                    px[ox + i, oy + j] = icons.rgba(ch)
    ant = icons.SPR["ant_side"]
    for x in (hx + 38, hx + 58, hx + 78):
        blit(ant, x, hill_y(x + 7) - 8, flip=True)
    bug = ["".join(c * 2 for c in row) for row in icons.SPR["bug_side"] for _ in (0, 1)]
    for x in (284, 302):
        blit(bug, x, ground[x + 9] - 11, flip=True)

    draw_weather(im, t, top)
    if t["layout"] == "left":
        im = ImageOps.mirror(im)
    return im


# ---------------------------------------------------------------- logo, buttons, retro

def nine(img, b, w, h):
    sw, sh = img.size
    out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    xs = [(0, b, 0, b), (b, sw - b, b, w - b), (sw - b, sw, w - b, w)]
    ys = [(0, b, 0, b), (b, sh - b, b, h - b), (sh - b, sh, h - b, h)]
    for sx0, sx1, dx0, dx1 in xs:
        for sy0, sy1, dy0, dy1 in ys:
            out.paste(img.crop((sx0, sy0, sx1, sy1)).resize((dx1 - dx0, dy1 - dy0), Image.NEAREST), (dx0, dy0))
    return out


def tint(img, base, ink, boost=0.0):
    """Repaint a button in one hue, keeping its light-to-dark structure."""
    keys = [ink, shade(base, 0.55), base, lighten(base, 0.35), lighten(base, 0.8)]
    if boost:
        keys = [keys[0]] + [lighten(k, boost) for k in keys[1:]]
    out = img.copy()
    px = out.load()
    lum = lambda p: (p[0] * 299 + p[1] * 587 + p[2] * 114) / 255000
    vals = [lum(px[x, y]) for x in range(out.width) for y in range(out.height) if px[x, y][3]]
    lo, hi = min(vals), max(vals)
    for y in range(out.height):
        for x in range(out.width):
            p = px[x, y]
            if p[3]:
                f = (lum(p) - lo) / (hi - lo) * (len(keys) - 1)
                k = min(len(keys) - 2, int(f))
                px[x, y] = hexc(mix(keys[k], keys[k + 1], f - k), p[3])
    return out


def wordmark(t, logo):
    logo.W, logo.H = 168, 76
    ink = t["ink"]
    im = Image.new("RGBA", (168, 76), (0, 0, 0, 0))
    hi, mid, lo = t["logo"]
    small_bands = t["logo_small"] or ["#ffffff", lighten(hi, 0.3), mix(hi, mid, 0.5)]
    small = logo.word_mask("HOLD THE", 2, 2)
    logo.paint_word(im, small, (168 - len(small[0])) // 2, 4, small_bands, ink, 1, (1, 2, mix(ink, lo, 0.25)))
    big = logo.word_mask("HILL", 5, 5)
    logo.paint_word(im, big, (168 - len(big[0])) // 2, 34, ramp([hi, mid, lo], 5), ink, 2,
                    (2, 3, mix(ink, lo, 0.2)), hi=lighten(hi, 0.6), lo=shade(lo, 0.62))
    return im


BAYER = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]


def quantize(im, mode, pal, dither=True):
    out = im.convert("RGBA")
    px = out.load()
    cols = [rgb(c) for c in pal]
    if mode == "lum":
        cols.sort(key=lambda c: c[0] * 299 + c[1] * 587 + c[2] * 114)
    n = len(cols)
    for y in range(out.height):
        for x in range(out.width):
            r, g, b, a = px[x, y]
            nudge = (BAYER[y % 4][x % 4] + 0.5) / 16 - 0.5 if dither else 0.0
            if mode == "lum":
                l = (r * 299 + g * 587 + b * 114) / 255000
                c = cols[max(0, min(n - 1, int(l * (n - 1) + 0.5 + nudge * 0.9)))]
            else:
                rr, gg, bb = (v + nudge * 40 for v in (r, g, b))
                c = min(cols, key=lambda q: (q[0] - rr) ** 2 * 3 + (q[1] - gg) ** 2 * 4 + (q[2] - bb) ** 2 * 2)
            px[x, y] = c + ((255 if a > 127 else 0) if not dither else a,)
    return out


def pascal(name):
    return "".join(w.capitalize() for w in name.split())


def label_image(fonts, hud, text):
    g = {c: fonts.bake(c, hud) for c in set(text)}
    return fonts.render(text, g, max(im.size[1] for im, _ in g.values()))


def compose(t, bg, fonts, logo, button, hover):
    """The logo and menu on a clear layer the size of the backdrop."""
    hud = next(s for s in fonts.STYLES if s["name"] == "Hud")
    mock = Image.new("RGBA", bg.size, (0, 0, 0, 0))
    layout = t["layout"]
    mock.alpha_composite(wordmark(t, logo), {"right": (W - 168 - 8, 4), "left": (8, 4), "center": (76, 2)}[layout])
    if t["btn"]:
        normal, hot = tint(button, t["btn"], t["ink"]), tint(hover, t["btn"], t["ink"], 0.22)
    else:
        normal, hot = button, hover
    for i, label in enumerate(["PLAY", "SETTINGS", "CONTROLS", "QUIT"]):
        text = label_image(fonts, hud, label)
        if layout == "center":
            bw, bh = 72, 18
            x, y = 10 + i * (bw + 4), 160
        else:
            bw, bh = 84, 18
            x, y = (W - bw - 50 if layout == "right" else 50), 82 + i * 20
        mock.alpha_composite(nine(hot if i == 0 else normal, 5, bw, bh), (x, y))
        mock.alpha_composite(text, (x + (bw - text.width) // 2, y + (bh - text.height) // 2))
    return mock


def sheet(tiles, cols, scale, fonts, title=None):
    hud = next(s for s in fonts.STYLES if s["name"] == "Hud")
    pad, lab = 6, 14
    rows = (len(tiles) + cols - 1) // cols
    tw, th = W * scale, H * scale
    head = 20 if title else 0
    out = Image.new("RGBA", (pad + cols * (tw + pad), head + pad + rows * (th + lab + pad)), hexc("#241d18"))
    if title:
        out.alpha_composite(label_image(fonts, hud, title), (pad, 5))
    for i, (name, im) in enumerate(tiles):
        x, y = pad + (i % cols) * (tw + pad), head + pad + (i // cols) * (th + lab + pad)
        out.alpha_composite(im.resize((tw, th), Image.NEAREST), (x, y))
        out.alpha_composite(label_image(fonts, hud, name.upper()), (x + 1, y + th + 2))
    return out


GROUP_TITLES = {"TimeOfDay": "Time of day", "Seasons": "Seasons", "Biomes": "Biomes", "Genres": "Genres and moods",
                "Holidays": "Holidays and festivals", "Retro": "Retro and limited palettes"}


def gallery(done):
    parts = ["<!doctype html><meta charset='utf-8'><title>Hold the Hill title variants</title>",
             "<style>body{background:#1b1512;color:#ecc477;font:14px/1.4 system-ui;margin:24px}"
             "h1{font-size:22px}h2{margin:28px 0 10px;font-size:16px;color:#fff1d6}"
             ".g{display:grid;grid-template-columns:repeat(auto-fill,minmax(400px,1fr));gap:14px}"
             "img{width:100%;image-rendering:pixelated;display:block;border:2px solid #3a2416}"
             "a{color:inherit;text-decoration:none}figure{margin:0}figcaption{padding:4px 2px}</style>",
             "<h1>Hold the Hill: title screen variants (%d)</h1>" % len(done)]
    for group, title in GROUP_TITLES.items():
        items = [x for x in done if x[0]["group"] == group]
        if not items:
            continue
        parts.append("<h2>%s</h2><div class='g'>" % title)
        for t, fname in items:
            parts.append("<figure><a href='%s'><img src='%s' alt='%s'></a><figcaption>%s <small>(%s layout)</small>"
                         "</figcaption></figure>" % (fname, fname, t["name"], t["name"], t["layout"]))
        parts.append("</div>")
    (OUT / "Gallery.html").write_text("\n".join(parts), encoding="utf-8")


def main():
    icons = load("icons", SANDBOX / "Icons" / "Source~" / "build_pixel_icons.py")
    fonts = load("fonts", SANDBOX / "Fonts" / "Source~" / "build_pixel_fonts.py")
    logo = load("logo", SANDBOX / "Icons" / "Source~" / "build_logo.py")
    button = Image.open(SANDBOX / "Ui" / "Button.png").convert("RGBA")
    hover = Image.open(SANDBOX / "Ui" / "ButtonHover.png").convert("RGBA")
    (OUT / "Backgrounds").mkdir(parents=True, exist_ok=True)

    wanted = [a.lower() for a in sys.argv[1:]]
    if not wanted:  # numbering shifts when themes are added, so clear this script's own output first
        for old in list(OUT.glob("Title[0-9][0-9]_*.png")) + list((OUT / "Backgrounds").glob("TitleBackground_*.png")):
            old.unlink()
    done, tiles = [], {}
    for n, raw in enumerate(THEMES, 1):
        t = resolve(raw)
        if wanted and not any(w in t["name"].lower() for w in wanted):
            continue
        bg = background(t, icons)
        ui = compose(t, bg, fonts, logo, button, hover)
        if t["retro"]:
            bg, ui = quantize(bg, *t["retro"]), quantize(ui, *t["retro"], dither=False)
        mock = bg.copy()
        mock.alpha_composite(ui)
        slug = pascal(t["name"])
        bg.save(OUT / "Backgrounds" / ("TitleBackground_%s.png" % slug))
        fname = "Title%02d_%s.png" % (n, slug)
        mock.resize((W * 4, H * 4), Image.NEAREST).save(OUT / fname)
        done.append((t, fname))
        tiles.setdefault(t["group"], []).append((t["name"], mock))
        print("wrote", fname)

    if not wanted:
        for group, items in tiles.items():
            sheet(items, 4 if len(items) > 9 else 3, 2, fonts, GROUP_TITLES[group].upper()).save(OUT / ("Sheet_%s.png" % group))
        sheet([x for items in tiles.values() for x in items], 8, 1, fonts).save(OUT / "TitleVariantsSheet.png")
        gallery(done)
        print("wrote %d themes, sheets and Gallery.html" % len(done))


if __name__ == "__main__":
    main()
