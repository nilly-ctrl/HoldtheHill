"""Hold the Hill art themes: Neon, Space and Spooky versions of every tower and enemy.

The looks follow three sets of the Ant Tower Formicary (Cyber Neon, Space, Dark Horror), redrawn
at this project's 32 px scale. Unlike the seasons, these are not recolours: the ant and its mound
are drawn again for each theme, and everything else the tower's own draw function adds (jaws,
shots, sparks) is painted from the theme's palette.

  Towers/Tower<Name>[T2|T3]<Theme>   all 19 castes at three tiers. Same tags, canvas, pivot and
                                     timing as Tower<Name>, so a theme swaps in without moving anything.
        Neon    black-chrome ant with a visor, a power cell and lit antenna tips, on a dark pad
                with a dashed ring of light. Each caste has its own colour of light.
        Space   white-suited ant in a bubble helmet with a backpack and a trim band, on a
                cratered regolith mound with pad markers and a beacon.
        Spooky  ashen ant with a skull head, glowing sockets, ribs and tail spurs, on a grave
                mound with a headstone, bones and a candle.
        Military is the exception: it changes the view. Each caste is an upright soldier ant in
                profile with its own weapon or tool, in a sandbag emplacement (build_military_sheets.py).
  Enemies/Enemy<Name><Theme>         the 12 enemies and the two bosses, each one complete file
                                     (Attack and Spawn folded in, as the elites are). Beetles get
                                     their theme detail in themed_beetle; every other creature is
                                     dressed by dress(): markings along the trunk and headgear
  Enemies/Enemy<Name>Elite<Theme>    the 12 elites, each theme in its own livery and rim (ELITE)

  Fx/, Projectiles/, Props/, Ui/     everything else as <Name><Theme>, painted from the theme's palette
                                     (not redrawn): shots, effects, hill and burrow; decor, lights,
                                     buildings, objects, water, terrain, plants, ambient ants, the queen;
                                     and the interface sheets. build_extras() lists them.
  ../../Tiles/Themes/                Ground<Theme>, Road<Theme> and RoadFill<Theme>: a lit grid for
                                     Neon, NeonSpace and Vaporwave, plain ground for the rest (FLOORS)

Review sheets: Theme<Theme>Towers.png, Enemies, Extras, World and Ui, and ThemeCompare.png.

    python build_theme_sheets.py                  every theme
    python build_theme_sheets.py Neon Space       only these
    python build_theme_sheets.py --preview        review sheets only, no files written
"""
import math
import os
import sys
import zlib

from PIL import Image, ImageDraw, ImageFont

import build_anim_sheets as base
import build_elite_sheets as elite
import build_extra_sheets as extra
import build_hud_sheets as hud
import build_military_sheets as military
import build_nature_sheets as nature
import build_roster_sheets as roster
import build_scenery_sheets as scenery
import build_tier_sheets as tiers
import build_weapon_sheets as wpn
import build_world_sheets as world
from build_anim_sheets import Cv, CX, Sprite, TW, export, mirror_x, tower_sprite

HERE = os.path.dirname(os.path.abspath(__file__))
TILE_OUT = os.path.normpath(os.path.join(HERE, "..", "..", "Tiles", "Themes"))

ALL_TOWERS = list(tiers.TOWERS) + list(roster.NEW_TOWERS)

# ================================================================ palettes
# Each theme replaces palette entries by key, so every existing draw function paints in theme
# colours. accents are (light, highlight, dark) and are published as "ac", "ah", "ad".
THEMES = {
    "Neon": dict(
        kind="neon", backdrop="#0e1018", cell="#161a26",
        pal={
            "k": "#0a0c14",
            "a": "#343e58", "A": "#8a9ac8", "z": "#1a1f2e",
            "q": "#3a2244", "Q": "#8a5ab0", "Z": "#1e1228",
            "N": "#1c3a4c", "n2": "#4fb8d8", "Nz": "#0e1e2a",
            "c": "#d8e6ff",
            "y": "#f0ff5a", "Y": "#96b414", "u": "#ffd23a",
            "o": "#ff7ad8", "r": "#ff2fc0", "R": "#8a1470", "f": "#ff9ae6",
            "g": "#a8ff3a", "G": "#4fb81a", "j": "#256010",
            "b": "#8af6ff", "B": "#00d8f0", "n": "#0a7a98",
            "p": "#e6c8ff", "P": "#b05aff", "V": "#5a20b0",
            "s": "#a0b4e0", "S": "#46506c", "x": "#283044",
            "t": "#3a4560", "T": "#232a3c",
            "d": "#141826", "D": "#1a2030", "e": "#0e121c",
            "m": "#00eaff", "M": "#0a7a98",
            "i": "#0f5a66", "I": "#5af0ff", "iz": "#083038",
            "h": "#2a3046", "H": "#8a9ac8", "hz": "#161a28",
            "v": "#120e1c",
            "l": "#ff5ad0", "L": "#8a1a78", "Lz": "#4a0e44",
        },
        accents=[("#00eaff", "#c8fbff", "#0a7a98"), ("#ff3ad0", "#ffc8f2", "#8a1470"), ("#a8ff3a", "#eaffc8", "#4a8a14")],
        rosy=("#46506c", "#a0b4e0", "#283044"), dirt="#232a3c",
    ),
    "Space": dict(
        kind="space", backdrop="#2c2f3a", cell="#3a3d4a",
        pal={
            "k": "#141a2c",
            "a": "#c9d0dc", "A": "#ffffff", "z": "#7c869c",
            "q": "#e0701a", "Q": "#ffa850", "Z": "#8a3c10",
            "N": "#2a3a70", "n2": "#6a8ae0", "Nz": "#161e40",
            "c": "#eef2f8",
            "g": "#7affc0", "G": "#28c890", "j": "#107050",
            "s": "#c4c8d4", "S": "#6e7484", "x": "#42475a",
            "t": "#b4b8c4", "T": "#8a8e9c",
            "d": "#5e6270", "D": "#4a4e5c", "e": "#30333e",
            "m": "#f07a1a", "M": "#a84c10",
            "i": "#3aa86a", "I": "#a8ffc8", "iz": "#1a5a3a",
            "h": "#6a4aa8", "H": "#c0a8ff", "hz": "#34205a",
            "v": "#1a1830",
            "l": "#d86ab0", "L": "#8a2a70", "Lz": "#501040",
        },
        accents=[("#f07a1a", "#ffd0a0", "#a84c10"), ("#4a7af0", "#c0d4ff", "#2a3a90"), ("#28c8b0", "#c0fff2", "#107868")],
        rosy=("#f0c8b0", "#ffffff", "#b08878"), dirt="#8a8e9c",
    ),
    "Spooky": dict(
        kind="spooky", backdrop="#1e1a24", cell="#2a2630",
        pal={
            "k": "#16121a",
            "a": "#767870", "A": "#c4c8b4", "z": "#3e3e42",
            "q": "#6a3a2a", "Q": "#b06a48", "Z": "#38201a",
            "N": "#3a3458", "n2": "#8a7ac8", "Nz": "#1e1a30",
            "c": "#e8e4d0",
            "y": "#ffd86a", "Y": "#c08a20", "u": "#f08020",
            "o": "#f08020", "r": "#c82a2a", "R": "#6a1418", "f": "#ff7a6a",
            "g": "#8aff8a", "G": "#3ab85a", "j": "#1a6a34",
            "b": "#d0c8ff", "B": "#9a8ae8", "n": "#4a3a98",
            "s": "#b0ac9c", "S": "#6a665e", "x": "#403c3a",
            "t": "#6e6254", "T": "#4a4038",
            "d": "#2c2622", "D": "#3a302a", "e": "#221c1a",
            "m": "#7a8a5a", "M": "#4a5438",
            "i": "#4a6a58", "I": "#a8d8b0", "iz": "#243a30",
            "h": "#4a4654", "H": "#a8a4b8", "hz": "#28242e",
            "v": "#1a1420",
            "l": "#a87ab0", "L": "#5a3a6a", "Lz": "#34203f",
        },
        accents=[("#8aff8a", "#e0ffe0", "#2a9a4a"), ("#b07af0", "#e8d8ff", "#5a3aa8"), ("#f08020", "#ffd8a0", "#a04a10")],
        rosy=("#9a8a94", "#d8ccd0", "#5a4c58"), dirt="#4a4038",
    ),
    "Steampunk": dict(
        kind="steampunk", backdrop="#1c1612", cell="#2a221c", tone=("#c89a58", 0.55),
        pal={
            "k": "#1a110b",
            "a": "#75583b", "A": "#c89a58", "z": "#3a2a1c",
            "q": "#8a4a24", "Q": "#d08048", "Z": "#4a2410",
            "N": "#2e4a48", "n2": "#6aa89a", "Nz": "#182a28",
            "s": "#b8a890", "S": "#6e6050", "x": "#40362c",
            "t": "#7e6a5a", "T": "#5a4a40",
            "d": "#3a2e28", "D": "#30261f", "e": "#1e1712",
            "m": "#c89434", "M": "#7a5a1c",
            "i": "#3a6a5e", "I": "#8ad0b8", "iz": "#1e3a34",
            "h": "#5a4a3a", "H": "#b09a78", "hz": "#30281e",
            "l": "#b06a48", "L": "#6a3a24", "Lz": "#3a1e12",
        },
        accents=[("#c89434", "#ffe08a", "#7a5a1c"), ("#d0703a", "#ffc09a", "#7a3a18"), ("#5ab8a0", "#c8fff0", "#2a6a5a")],
        rosy=("#b08a70", "#e8c8a8", "#6a4a38"), dirt="#5a4a40",
    ),
    "Medieval": dict(
        kind="medieval", backdrop="#20241c", cell="#34402c",
        pal={
            "a": "#7a4a2c", "A": "#b8784a", "z": "#3a2016",
            "t": "#a8a8b0", "T": "#7e7e86",
            "d": "#55555e", "D": "#44444c", "e": "#2c2c34",
        },
        accents=[("#c8302a", "#ff9a8a", "#7a1818"), ("#3a5ac8", "#a8c0ff", "#1e2e78"), ("#e0b030", "#fff0a0", "#8a6410")],
        rosy=("#e9b49c", "#fff1d6", "#b9795f"), dirt="#7e7e86",
    ),
    "Samurai": dict(
        kind="samurai", backdrop="#1e1a18", cell="#3a3630", tone=("#c87868", 0.35),
        pal={
            "k": "#170d0a",
            "a": "#4a2a1e", "A": "#8e5e40", "z": "#2a1610",
            "t": "#d8ceb4", "T": "#b8ac90",
            "d": "#8a7e66", "D": "#5a5040", "e": "#3a3228",
            "m": "#f0a8c0", "M": "#c06a8a",
            "i": "#6a2a24", "I": "#d0705a", "iz": "#3a1410",
            "h": "#3a3440", "H": "#8a84a0", "hz": "#1e1a24",
        },
        accents=[("#c8302a", "#ff9a80", "#701410"), ("#3a4a9a", "#a0b0f0", "#1c2458"), ("#d8a830", "#fff0a0", "#7a5a10")],
        rosy=("#c89a70", "#f0d0a8", "#8e5e40"), dirt="#b8ac90",
    ),
    "Candy": dict(
        kind="candy", backdrop="#2a1830", cell="#4a2a50", tone=("#ff9ac8", 0.45),
        pal={
            "k": "#4a0a30",
            "a": "#d4508a", "A": "#ffb0d4", "z": "#a82664",
            "q": "#e07a3a", "Q": "#ffc080", "Z": "#a04a1a",
            "N": "#4a9ad8", "n2": "#b0e0ff", "Nz": "#2a5a98",
            "c": "#fff4e0",
            "s": "#ffe0f0", "S": "#d090b8", "x": "#a06088",
            "t": "#fff0c8", "T": "#e8c890",
            "d": "#b88a50", "D": "#8a5a30", "e": "#5a3418",
            "m": "#62f0b8", "M": "#2aa880",
            "i": "#3ac8a0", "I": "#c0fff0", "iz": "#1a8068",
            "h": "#8a6ad8", "H": "#d8c8ff", "hz": "#4a3a98",
            "l": "#ff8ab0", "L": "#c84a80", "Lz": "#8a2458",
        },
        accents=[("#62f0b8", "#e0fff4", "#2aa880"), ("#ffe04a", "#fffac0", "#c09a10"), ("#6ac8ff", "#d8f2ff", "#2a80c0")],
        rosy=("#ffc8a0", "#fff0d8", "#d89068"), dirt="#e8c890",
    ),
    "Jungle": dict(
        kind="jungle", backdrop="#101a10", cell="#1e3018", tone=("#9ab85a", 0.35),
        pal={
            "k": "#140c08",
            "a": "#472a1a", "A": "#8a5a38", "z": "#24140c",
            "t": "#6a5632", "T": "#4a3a22",
            "d": "#2e2414", "D": "#241a0e", "e": "#160f08",
            "m": "#5ac83a", "M": "#2a7a1c",
            "i": "#2a7a3a", "I": "#8ae08a", "iz": "#144a20",
            "h": "#5a4a2a", "H": "#b8a060", "hz": "#2e2614",
        },
        accents=[("#e0a830", "#fff0a0", "#8a5a10"), ("#d84a2a", "#ffb090", "#7a2010"), ("#3ac8b0", "#c0fff0", "#1a7868")],
        rosy=("#74482c", "#b07a50", "#472a1a"), dirt="#4a3a22",
    ),
    "Pirate": dict(
        kind="pirate", backdrop="#12242c", cell="#1e4250", tone=("#9ac8c0", 0.45),
        pal={
            "k": "#101a1c",
            "a": "#42585a", "A": "#8aa8a0", "z": "#263234",
            "q": "#7a3a2a", "Q": "#c87a5a", "Z": "#401c14",
            "t": "#f0e0b0", "T": "#d8c088",
            "d": "#a88a50", "D": "#6a4a2a", "e": "#3a2816",
            "m": "#4ab890", "M": "#2a7860",
            "i": "#2a6a7a", "I": "#8ad8e8", "iz": "#143a44",
            "h": "#4a4038", "H": "#a89880", "hz": "#281f1a",
            "v": "#1c1a26",
        },
        accents=[("#d83a2a", "#ffa890", "#7a1a10"), ("#d8b040", "#fff0a0", "#8a6a10"), ("#3aa8d8", "#c0ecff", "#1a6090")],
        rosy=("#6a8684", "#b4ccc4", "#42585a"), dirt="#d8c088",
    ),
    "Robot": dict(
        kind="robot", backdrop="#14171c", cell="#22262c", tone=("#b0b8c2", 0.65),
        pal={
            "k": "#14171c",
            "a": "#59606a", "A": "#a8b0ba", "z": "#2f343b",
            "q": "#8a3a2a", "Q": "#d87a5a", "Z": "#4a1c14",
            "N": "#2a4a6a", "n2": "#6aa0d8", "Nz": "#162838",
            "s": "#a8b0ba", "S": "#59606a", "x": "#2f343b",
            "t": "#70777f", "T": "#4a5058",
            "d": "#2c3036", "D": "#22262b", "e": "#15181c",
            "m": "#f0b418", "M": "#8a6408",
            "i": "#3a5a4a", "I": "#8ad0a8", "iz": "#1c3026",
            "h": "#4a4f58", "H": "#aab2bc", "hz": "#262a30",
            "l": "#c86a3a", "L": "#7a3a1c", "Lz": "#40200e",
        },
        accents=[("#f0b418", "#fff0a0", "#8a6408"), ("#f04a3a", "#ffb0a0", "#8a1a10"), ("#4af08a", "#d0ffe0", "#1a8a48")],
        rosy=("#8b939d", "#c9d1da", "#59606a"), dirt="#4a5058",
    ),
    "NeonSpace": dict(
        kind="neonspace", backdrop="#07060f", cell="#100e20",
        pal={
            "k": "#07060f",
            "a": "#2a2f52", "A": "#7a84c8", "z": "#14172e",
            "q": "#3a2244", "Q": "#8a5ab0", "Z": "#1e1228",
            "N": "#3a1a6a", "n2": "#b05aff", "Nz": "#1e0e3a",
            "c": "#d8e6ff",
            "y": "#f0ff5a", "Y": "#96b414", "u": "#ffd23a",
            "o": "#ff7ad8", "r": "#ff2fc0", "R": "#8a1470", "f": "#ff9ae6",
            "g": "#5affc8", "G": "#1ab88a", "j": "#106050",
            "b": "#8af6ff", "B": "#00d8f0", "n": "#0a7a98",
            "p": "#e6c8ff", "P": "#b05aff", "V": "#5a20b0",
            "s": "#a0a8e0", "S": "#464a7c", "x": "#282a4c",
            "t": "#463a78", "T": "#2a2248",
            "d": "#18142c", "D": "#1a1630", "e": "#0e0c1c",
            "m": "#b05aff", "M": "#5a20b0",
            "i": "#0f5a66", "I": "#5af0ff", "iz": "#083038",
            "h": "#3a2a66", "H": "#a88af0", "hz": "#1c1438",
            "v": "#120e1c",
            "l": "#ff5ad0", "L": "#8a1a78", "Lz": "#4a0e44",
        },
        accents=[("#00eaff", "#c8fbff", "#0a7a98"), ("#ff3ad0", "#ffc8f2", "#8a1470"), ("#b05aff", "#ead0ff", "#5a20b0")],
        rosy=("#46507c", "#a0b4f0", "#282e54"), dirt="#2a2248",
    ),
    "Vaporwave": dict(
        kind="vapor", backdrop="#1a0a38", cell="#2c1458", tone=("#ff9ae0", 0.5),
        pal={
            "k": "#2a1048",
            "a": "#c8a0f0", "A": "#f8d8ff", "z": "#7a50c0",
            "q": "#ff8ab0", "Q": "#ffd0e0", "Z": "#c04a80",
            "N": "#01a8d8", "n2": "#a0f0ff", "Nz": "#086a98",
            "c": "#fff0fa",
            "y": "#fffb96", "Y": "#d0b040", "u": "#ffd080",
            "o": "#ff9ae0", "r": "#ff71ce", "R": "#a0308a", "f": "#ffc8f0",
            "g": "#05ffa1", "G": "#00b878", "j": "#087050",
            "b": "#a0f0ff", "B": "#01cdfe", "n": "#0878b0",
            "p": "#e8d0ff", "P": "#b967ff", "V": "#6a30c0",
            "s": "#e0c8ff", "S": "#9a78d0", "x": "#5a3a98",
            "t": "#5a2a9a", "T": "#3a1a6a",
            "d": "#22104a", "D": "#2a1458", "e": "#1a0c38",
            "m": "#01cdfe", "M": "#0878b0",
            "i": "#01a8c8", "I": "#a0f6ff", "iz": "#086078",
            "h": "#8a60d8", "H": "#d8c0ff", "hz": "#4a2a98",
            "v": "#2a1048",
            "l": "#ff71ce", "L": "#b0308a", "Lz": "#6a1458",
        },
        accents=[("#ff71ce", "#ffd0f0", "#b0308a"), ("#01cdfe", "#c0f6ff", "#0878b0"), ("#fffb96", "#ffffe0", "#c0a840")],
        rosy=("#ffb0d0", "#fff0f8", "#c8709a"), dirt="#3a1a6a",
    ),
    "Military": dict(
        kind="military", backdrop="#2a3020", cell="#3c4630",
        pal={
            "m": "#8a9a52", "M": "#5a6a34", "j": "#34401e",
            "g": "#d8e07a", "G": "#8a9a52",
            "t": "#d8c490", "T": "#b49c66",
            "d": "#8a7448", "D": "#6a5636", "e": "#3e3220",
            "i": "#8a7a4a", "I": "#d0c088", "iz": "#4a4026",
            "h": "#5a5f58", "H": "#a8b0a4", "hz": "#30342e",
        },
        accents=[("#ffd23a", "#fff0a0", "#c28a10"), ("#e2412f", "#ffb0a0", "#8e2124"), ("#8a9a52", "#d8e07a", "#34401e")],
        rosy=("#e9b49c", "#fff1d6", "#b9795f"), dirt="#6a5636",
    ),
}

# what each of these themes shoots with: acid (g G j), water and ice (b B n), arcane (p P V), and for some fire and gold
SHOT_COLOURS = {
    "Steampunk": {"g": "#ffe08a", "G": "#c89434", "j": "#7a5a1c", "b": "#e8f0f0", "B": "#a8c0c0", "n": "#5a7878",
                  "p": "#ffc09a", "P": "#d0703a", "V": "#7a3a18"},
    "Samurai": {"g": "#ffc8d8", "G": "#f080a0", "j": "#a04060", "b": "#c8d0e8", "B": "#6a7ab0", "n": "#2a3460",
                "p": "#fff0a0", "P": "#d8a830", "V": "#7a5a10"},
    "Candy": {"g": "#62f0b8", "G": "#2aa880", "j": "#1a7058", "b": "#d8f2ff", "B": "#6ac8ff", "n": "#2a80c0",
              "p": "#f0d0ff", "P": "#c080ff", "V": "#7a40c0", "o": "#ff9ac0", "r": "#ff5a9a", "R": "#a82664", "f": "#ffc8e0",
              "y": "#ffe04a", "Y": "#c09a10", "u": "#ffb84a"},
    "Jungle": {"g": "#c8f04a", "G": "#7aa81c", "j": "#3a5a10", "b": "#c0fff0", "B": "#3ac8b0", "n": "#1a7868",
               "p": "#ffb090", "P": "#d84a2a", "V": "#7a2010"},
    "Pirate": {"g": "#8af0d0", "G": "#2aa890", "j": "#106050", "b": "#c0ecff", "B": "#3aa8d8", "n": "#1a6090",
               "p": "#fff0a0", "P": "#d8b040", "V": "#8a6a10"},
    "Robot": {"g": "#4af08a", "G": "#1a8a48", "j": "#0e5028", "b": "#a0d8ff", "B": "#3a90e0", "n": "#1a4a90",
              "p": "#fff0a0", "P": "#f0b418", "V": "#8a6408", "o": "#ff8a5a", "r": "#f04a3a", "R": "#8a1a10", "f": "#ffb0a0"},
}
for _name, _colours in SHOT_COLOURS.items():
    THEMES[_name]["pal"].update(_colours)

# each theme's elite livery: the colour the body is steeped in, how strongly, and the two colours of its rim
ELITE = {
    "Neon": ("#ff2fc0", 0.45, "#f0ff5a", "#ffffff"),          # overclocked
    "Space": ("#b05aff", 0.45, "#7affc0", "#eafff4"),         # a royal strain of alien
    "Spooky": ("#3a1a4a", 0.50, "#8aff8a", "#e0ffe0"),        # possessed
    "Steampunk": ("#d0703a", 0.50, "#ffe08a", "#c89434"),     # copper-clad, brass trim
    "Medieval": ("#3a3a48", 0.50, "#e0b030", "#fff0a0"),      # black knights
    "Samurai": ("#1a1a22", 0.50, "#c8302a", "#d8a830"),       # black lacquer, red and gold lacing
    "Candy": ("#5a2a1a", 0.50, "#ffe04a", "#ffffff"),         # dipped in chocolate, gold foil
    "Jungle": ("#d84a2a", 0.40, "#e0a830", "#fff0a0"),        # painted for war
    "Pirate": ("#1c1a26", 0.50, "#d8b040", "#fff0a0"),        # black flag, gold braid
    "Robot": ("#f04a3a", 0.40, "#f0b418", "#14171c"),         # red oxide with a hazard-striped rim
    "NeonSpace": ("#b05aff", 0.45, "#00eaff", "#ffffff"),
    "Vaporwave": ("#01cdfe", 0.40, "#fffb96", "#ff71ce"),
    "Military": ("#2a2a2e", 0.50, "#e2412f", "#ffd23a"),      # black ops, red and gold piping
}

SUITED = ("space", "neonspace", "vapor")        # the three that wear a bubble helmet and a backpack
GLOWING = ("neon", "neonspace")                 # the two that light the ground and rim their enemies
# ground (base, two speckle colours) and road (base, light, dark, edge line) for the themes whose floor is not a lit grid
FLOORS = {
    "Space": (("#5e6270", "#6e7484", "#4a4e5c"), ("#8a8e9c", "#a0a4b0", "#6e7280", "#42475a")),
    "Spooky": (("#2c2a30", "#3a3640", "#221e26"), ("#4a4038", "#5e5248", "#3a302a", "#221c1a")),
    "Steampunk": (("#3a2e28", "#4a3c34", "#2c221c"), ("#5a4a40", "#6e5c4e", "#463830", "#c89434")),
    "Medieval": (("#4a6a34", "#587a3e", "#3e5a2c"), ("#8a8a90", "#a0a0a8", "#70707a", "#55555e")),
    "Samurai": (("#6a7050", "#788060", "#585e42"), ("#c8bc9c", "#d8ceb4", "#b0a484", "#8a7e66")),
    "Candy": (("#f0a8d0", "#ffc0e0", "#e090c0"), ("#e8c890", "#fff0c8", "#d0a868", "#b88a50")),
    "Jungle": (("#1e3a18", "#2a4a20", "#162c12"), ("#4a3a22", "#5e4a2c", "#3a2c18", "#241a0e")),
    "Pirate": (("#1e5a70", "#2a6e86", "#184a5e"), ("#8a6a3a", "#a07e48", "#70542c", "#4a341a")),
    "Robot": (("#2c3036", "#383d44", "#22262b"), ("#4a5058", "#5a6068", "#3c4148", "#f0b418")),
    "Military": (("#4a5630", "#586640", "#3c4626"), ("#6a5636", "#7e6844", "#56452a", "#3e3220")),
}
SUNSET = ("#fffb96", "#ffc890", "#ff71ce", "#d060e0", "#8a50e0")

_theme = None       # the THEMES entry being built
_plain = dict(tower_base=base.tower_base, ant=base.ant, beetle=base.beetle, sprite_init=Sprite.__init__,
              centipede=base.centipede, mantis=base.mantis, creature=roster.creature)


def set_accent(index):
    light, hi, dark = _theme["accents"][index % len(_theme["accents"])]
    base.PAL.update(ac=light, ah=hi, ad=dark)


def core_name(name):
    for part in ("Moves", "Elite", *THEMES):
        name = name.replace(part, "")
    return name


def stripe(cv, y, x0, x1, c):
    """Recolour what is already painted on row y between x0 and x1; ink and empty pixels stay."""
    ink = base.rgba("k")[:3]
    y = int(math.floor(y))
    for x in range(int(x0), int(x1) + 1):
        if 0 <= x < cv.w and 0 <= y < cv.h and cv.p[x, y][3] and cv.p[x, y][:3] != ink:
            cv.px(x, y, c)


def box(cv, x0, y0, x1, y1, c, hi, lo, cut=True):
    """A plated rectangle lit from the top left, corners clipped."""
    x0, y0, x1, y1 = int(x0), int(y0), int(x1), int(y1)
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            if cut and (x in (x0, x1)) and (y in (y0, y1)):
                continue
            cv.px(x, y, hi if (x == x0 or y == y0) else lo if (x == x1 or y == y1) else c)


# ================================================================ the ant, per theme
def themed_ant(L, cx=CX, cy=16, col=("a", "A", "z"), head="ant", bob=0, abd=0.0, head_dy=0, head_dx=0,
               tw=(0, 0), legs=0, mand=0, antennae=True, ant_tips=None, lift=0, layer="Body"):
    """base.ant with the same arguments and the same joints, so every tower's own gear still lands
    where it expects; what is drawn on those joints changes with the theme."""
    kind = _theme["kind"]
    if isinstance(col[0], str) and col[0].startswith("#"):          # the nurse's own rosy coat
        col = _theme["rosy"]
    body, ink = Cv(TW, TW), Cv(TW, TW)
    y0 = cy + bob - lift
    hy = y0 - 7 + head_dy
    sw = round(legs)
    left = [
        [(cx - 2, y0 - 2), (cx - 5, y0 - 4), (cx - 6, y0 - 7 - sw)],
        [(cx - 2, y0 - 1), (cx - 6, y0 - 1), (cx - 8, y0 + 1 + sw)],
        [(cx - 2, y0), (cx - 5, y0 + 2), (cx - 7, y0 + 6 - sw)],
    ]
    feet = []
    for i, leg in enumerate(left):
        ink.line(leg, "k")
        other = mirror_x(leg, cx)
        if sw:
            other = [(x, y + (2 * sw if j == 2 else 0) * (1 if i != 1 else -1)) for j, (x, y) in enumerate(other)]
        ink.line(other, "k")
        feet += [leg[-1], other[-1]]
    tips = []
    if antennae:
        for side, t in ((-1, tw[0]), (1, tw[1])):
            if ant_tips:
                pts = [(cx - 2, hy - 2), (cx - 4, hy - 5), ant_tips[0 if side < 0 else 1]]
            elif kind == "spooky":                                   # crooked, with a second kink
                pts = [(cx - 2, hy - 2), (cx - 5, hy - 4), (cx - 3, hy - 6), (cx - 4 - t, hy - 8)]
            else:
                pts = [(cx - 2, hy - 2), (cx - 4, hy - 5), (cx - 3 - t, hy - 8)]
            pts = pts if side < 0 else mirror_x(pts, cx)
            ink.line(pts, "k")
            tips.append(pts[-1])
    m = [(cx - 2 - mand, hy - 3), (cx - 1 - mand, hy - 5)]
    ink.line(m, "k")
    ink.line(mirror_x(m, cx), "k")

    c0, hi, lo = col
    gy = y0 + 7 + round(abd)                                         # the gaster's middle row
    hr = {"ant": (3.5, 2.8), "soldier": (4.5, 3.2), "brute": (4.5, 3.3), "major": (5, 3.6)}[head]
    hx = int(cx + head_dx)
    if kind == "robot":                                              # plated boxes instead of round segments
        ra, hw = int(abd + 0.5), int(hr[0])
        box(body, cx - 4 - ra, y0 + 3 - ra, cx + 3 + ra, y0 + 11 + ra, c0, hi, lo)
        box(body, cx - 1, y0 + 2, cx, y0 + 2, lo, lo, lo, cut=False)
        box(body, cx - 2, y0 - 3, cx + 1, y0 + 1, c0, hi, lo, cut=False)
        box(body, hx - hw, hy - 2, hx + hw - 1, hy + 1, c0, hi, lo)
    else:
        body.blob(cx, y0 + 7, 4 + abd, 4.6 + abd, c0, hi, lo)
        body.blob(cx, y0 + 2.5, 0.8, 0.8, c0)
        body.blob(cx, y0 - 1, 1.6, 2.4, c0, hi, lo)
    if kind in SUITED:                                               # backpack, wider than the thorax
        for y in range(4):
            for x in range(4):
                body.px(cx - 2 + x, y0 - 2 + y, "N")
        body.px(cx - 2, y0 - 2, "n2")
        body.px(cx + 1, y0 + 1, "Nz")
    if kind == "spooky":
        body.blob(cx + head_dx, hy, hr[0], hr[1] + 0.4, "c", "w", "s")   # a skull, whatever the coat
        spur = int(y0 + 11.6 + abd) + 1
        body.pxs([(cx - 2, spur), (cx + 1, spur)], "c")
    elif kind == "medieval":                                         # steel helm with a plume, pauldrons
        body.blob(cx + head_dx, hy, hr[0], hr[1], "s", "c", "S")
        body.pxs([(hx - 1, hy - 4), (hx, hy - 4), (hx, hy - 5), (hx + 1, hy - 5), (hx + 1, hy - 6)], "ac")
        body.pxs([(cx - 3, y0 - 2), (cx + 2, y0 - 2)], "s")
    elif kind == "samurai":                                          # lacquered helmet with a gold crest
        body.blob(cx + head_dx, hy, hr[0], hr[1], "ad", "ac", "ad")
        body.pxs([(hx - 4, hy - 5), (hx - 3, hy - 4), (hx - 2, hy - 3), (hx + 1, hy - 3), (hx + 2, hy - 4), (hx + 3, hy - 5)], "y")
        body.pxs([(cx - 3, y0 - 2), (cx - 3, y0 - 1), (cx + 2, y0 - 2), (cx + 2, y0 - 1)], "ac")
    elif kind != "robot":
        body.blob(cx + head_dx, hy, hr[0], hr[1], c0, hi, lo)
    if kind == "steampunk":                                          # top hat and a smokestack
        for x in range(hx - 3, hx + 3):
            body.px(x, hy - 3, "e")
        for y in (hy - 5, hy - 4):
            for x in range(hx - 2, hx + 2):
                body.px(x, y, "e")
        body.pxs([(cx + 2, y0 - 3), (cx + 2, y0 - 2)], "S")
    elif kind == "candy":                                            # a cherry on top
        body.pxs([(hx - 1, hy - 4), (hx, hy - 4), (hx, hy - 5)], "r")
        body.px(hx - 1, hy - 5, "f")
    elif kind == "jungle":                                           # feather headdress
        body.pxs([(hx - 3, hy - 4), (hx - 4, hy - 5), (hx - 4, hy - 6), (hx + 2, hy - 4), (hx + 3, hy - 5), (hx + 3, hy - 6)], "g")
        body.pxs([(hx - 1, hy - 4), (hx, hy - 4), (hx - 1, hy - 5), (hx, hy - 5)], "ac")
        body.pxs([(hx - 1, hy - 6), (hx, hy - 6)], "ah")
    elif kind == "pirate":                                           # tricorn
        for x in range(hx - 4, hx + 4):
            body.px(x, hy - 3, "v")
        for x in range(hx - 3, hx + 3):
            body.px(x, hy - 4, "v")
        body.pxs([(hx - 1, hy - 5), (hx, hy - 5)], "v")
    body.outline()

    if kind == "neon":
        for x in range(hx - 3, hx + 3):                              # visor
            body.px(x, hy - 1, "ac")
        body.pxs([(hx - 1, hy - 1), (hx, hy - 1)], "ah")
        for y in (-1, 0, 1):                                         # power cell
            body.pxs([(cx - 1, gy + y), (cx, gy + y)], "ac")
        body.px(cx - 1, gy - 1, "ah")
        body.pxs([(cx - 3, gy + 2), (cx + 2, gy + 2)], "ad")
    elif kind == "vapor":
        body.pxs([(hx - 3, hy - 1), (hx + 2, hy - 1)], "k")
        for j, c in enumerate(SUNSET):                               # a sunset down the gaster
            stripe(body, gy - 2 + j, cx - 6, cx + 5, c)
        body.px(cx - 1, y0 - 1, "ac")
    elif kind in SUITED:
        if kind == "neonspace":                                      # a lit visor inside the helmet
            for x in range(hx - 3, hx + 3):
                body.px(x, hy - 1, "ac")
            body.pxs([(hx - 1, hy - 1), (hx, hy - 1)], "ah")
        else:
            body.pxs([(hx - 3, hy - 1), (hx + 2, hy - 1)], "k")
        stripe(body, gy, cx - 6, cx + 5, "ac")                       # trim band
        stripe(body, gy + 1, cx - 6, cx + 5, "ad")
        body.px(cx - 1, y0 - 1, "ac")                                # pack light
    elif kind == "steampunk":
        body.pxs([(hx - 3, hy - 1), (hx + 2, hy - 1)], "ac")         # goggles
        body.pxs([(hx - 2, hy - 1), (hx + 1, hy - 1)], "b")
        for x in range(hx - 2, hx + 2):                              # hat band
            body.px(x, hy - 4, "ac")
        for y in (-1, 0):                                            # pressure gauge
            body.pxs([(cx - 1, gy + y), (cx, gy + y)], "c")
        body.px(cx, gy - 1, "r")
        body.pxs([(cx - 3, gy + 1), (cx + 2, gy + 1), (cx - 2, gy + 3), (cx + 1, gy + 3)], "ah")   # rivets
        body.px(cx + 2, y0 - 5, "w", 150)                            # steam
        body.px(cx + 3, y0 - 6, "w", 90)
    elif kind == "medieval":
        for x in range(hx - 2, hx + 2):                              # visor slit
            body.px(x, hy - 1, "k")
        body.pxs([(hx - 1, hy + 1), (hx, hy + 1)], "S")
        for y in range(-3, 4):                                       # the cross on the tabard
            stripe(body, gy + y, cx - 1, cx, "ac")
        stripe(body, gy - 1, cx - 3, cx + 2, "ac")
    elif kind == "samurai":
        body.pxs([(hx - 3, hy - 1), (hx + 2, hy - 1)], "k")
        body.pxs([(hx - 1, hy + 1), (hx, hy + 1)], "y")
        for y in (-2, 0, 2):                                         # lamellar rows
            stripe(body, gy + y, cx - 6, cx + 5, "ac")
        body.pxs([(cx - 2, gy - 1), (cx + 1, gy - 1), (cx - 2, gy + 1), (cx + 1, gy + 1)], "y")   # lacing
    elif kind == "candy":
        body.pxs([(hx - 3, hy - 1), (hx + 2, hy - 1)], "k")
        body.pxs([(hx - 2, hy - 2), (hx + 1, hy - 2)], "w")
        stripe(body, gy + 1, cx - 6, cx + 5, "ac")                   # candy stripe
        stripe(body, gy + 2, cx - 6, cx + 5, "ah")
        body.pxs([(cx - 2, gy - 3), (cx - 3, gy - 2)], "w")          # gloss
        body.px(cx + 1, gy - 1, "y")                                 # sprinkles
        body.px(cx - 1, gy + 3, "b")
        body.px(cx + 2, gy - 3, "w")
    elif kind == "jungle":
        body.pxs([(hx - 3, hy - 1), (hx + 2, hy - 1)], "k")
        stripe(body, hy, hx - 4, hx + 3, "ac")                       # war paint
        body.pxs([(hx - 1, hy + 1), (hx, hy + 1)], "c")
        for x in range(cx - 4, cx + 4):                              # painted zigzag
            stripe(body, gy + (x % 2), x, x, "ac")
        body.pxs([(cx - 1, y0 - 3), (cx, y0 - 3)], "c")              # bone necklace
    elif kind == "pirate":
        stripe(body, hy - 2, hx - 4, hx + 3, "k")                    # patch strap
        body.pxs([(hx - 3, hy - 1), (hx - 2, hy - 1), (hx + 2, hy - 1)], "k")
        body.pxs([(hx - 4, hy - 3), (hx + 3, hy - 3)], "y")          # hat trim and badge
        body.pxs([(hx - 1, hy - 4), (hx, hy - 4)], "c")
        for y in range(-3, 4):                                       # striped shirt
            stripe(body, gy + y, cx - 6, cx + 5, "c" if y % 2 else "ac")
    elif kind == "robot":
        stripe(body, hy - 1, hx - 5, hx + 4, "z")                    # visor with two lamps
        body.pxs([(hx - 2, hy - 1), (hx + 1, hy - 1)], "ac")
        body.pxs([(hx - 1, hy + 1), (hx + 1, hy + 1)], "k")
        for x in range(cx - 6, cx + 6):                              # hazard stripes
            stripe(body, gy + 2, x, x, "y" if (x // 2) % 2 else "k")
            stripe(body, gy + 3, x, x, "y" if ((x + 1) // 2) % 2 else "k")
        body.pxs([(cx - 3, gy - 3), (cx + 2, gy - 3)], "s")          # bolts
        body.px(cx - 1, y0 - 1, "ac")
    else:
        body.pxs([(hx - 3, hy - 1), (hx + 2, hy - 1), (hx - 3, hy), (hx + 2, hy)], "k")   # sockets
        body.pxs([(hx - 2, hy - 1), (hx + 1, hy - 1)], "ac")
        body.pxs([(hx - 1, hy + 1), (hx, hy + 1)], "s")
        body.pxs([(hx - 2, hy + 2), (hx + 1, hy + 2)], "k")                              # teeth
        for y in (-2, 0, 2):                                         # ribs
            stripe(body, gy + y, cx - 2, cx + 1, lo)
        stripe(body, gy - 1, cx - 1, cx, hi)
        stripe(body, gy + 1, cx - 1, cx, hi)

    if kind in GLOWING:                                              # light spilling onto the pad
        under = Cv(TW, TW)
        under.blob(cx, y0 + 7, 6.5 + abd, 7 + abd, "ac", a=40)
        L[layer].paste(under)
    ink.paste(body)
    if kind in SUITED:                                               # bubble helmet
        helm = Cv(TW, TW)
        glass = {"space": "b", "neonspace": "ac", "vapor": "o"}[kind]
        helm.disc(cx + head_dx, hy, 5.3, glass, a=46)
        helm.ring(cx + head_dx, hy, 5.3, glass, 1, a=230 if kind == "neonspace" else 200)
        helm.pxs([(hx - 3, hy - 4), (hx - 4, hy - 3)], "w")
        ink.paste(helm)
    if kind in ("neon", "spooky", "steampunk", "candy", "robot") + SUITED:
        for tip in tips:
            ink.px(tip[0], tip[1], "ac")
    if kind in ("neon", "robot") + SUITED:
        for x, y in feet:
            ink.px(x, y, "ad")
    L[layer].paste(ink)


# ================================================================ the mound, per theme
def themed_base(L, mound=("T", "t", "d")):
    kind = _theme["kind"]
    sh = L["Shadow"]
    sh.blob(CX, 21.5, 14.5, 10, (0, 0, 0), a=85)
    b = Cv(TW, TW)
    if kind == "vapor":                                              # the striped sun going down behind the pad
        for j, y in enumerate(range(3, 11)):
            if j in (5, 7):
                continue
            half = math.sqrt(max(0.0, 6.5 ** 2 - (y + 0.5 - 9.5) ** 2))
            for x in range(int(CX - half), int(CX + half)):
                b.px(x, y, SUNSET[min(4, j * 5 // 8)])
    b.blob(CX, 19, 13.5, 10.5, *mound)
    if kind == "neonspace":                                          # a dark asteroid with a lit landing ring
        sh.ring(CX, 19.5, 15.5, "ac", 1.2, a=70, ry=12)
        for x, y in ((2, 9), (29, 7), (26, 30), (4, 29)):            # stars
            sh.px(x, y, "w", 200)
        b.ring(CX, 19.5, 10.5, "ad", 1, ry=8)
        b.ring(CX, 19.5, 10.5, "ac", 1, ry=8, gaps=lambda a: int(a // 20) % 3 == 0)
        for x, y in ((6, 21), (25, 16), (21, 26)):                   # small craters
            b.pxs([(x, y), (x + 1, y)], mound[2])
            b.px(x, y - 1, mound[1])
        b.pxs([(6, 25), (6, 24)], "S")                               # beacon
        b.outline()
        b.pxs([(9, 14), (22, 14), (9, 25), (22, 25)], "ah")          # pad markers
        b.px(6, 23, "ac")
        b.px(6, 22, "ah")
    elif kind == "vapor":                                            # a grid running to the horizon
        ink = base.rgba("k")[:3]

        def grid(x, y, c):
            x, y = int(x), int(y)
            if 0 <= x < TW and 0 <= y < TW and b.p[x, y][3] and b.p[x, y][:3] != ink and y >= 11:
                b.px(x, y, c)
        b.outline()
        for y in (13, 16, 20, 25):
            for x in range(TW):
                grid(x, y, "M")
        for k in range(-5, 6):
            for y in range(11, 30):
                grid(CX - 0.5 + k * (1.6 + (y - 11) * 0.16), y, "M")
        b.pxs([(3, 19), (28, 19)], "ac")
        b.pxs([(15, 28), (16, 28)], "m")
    elif kind == "neon":
        sh.ring(CX, 19.5, 15.5, "ac", 1.2, a=70, ry=12)              # glow round the pad
        b.ring(CX, 19.5, 10.5, "ad", 1, ry=8)
        b.ring(CX, 19.5, 10.5, "ac", 1, ry=8, gaps=lambda a: int(a // 30) % 2 == 0)
        b.outline()
        b.pxs([(3, 19), (28, 19), (15, 9), (16, 28)], "ah")          # rim lights
        b.line([(6, 25), (8, 27)], "ad")
        b.line([(25, 25), (23, 27)], "ad")
    elif kind == "space":
        b.ring(CX, 19.5, 10.5, mound[2], 1, ry=8)
        for x, y in ((6, 21), (25, 16), (21, 26)):                   # small craters
            b.pxs([(x, y), (x + 1, y)], mound[2])
            b.px(x, y - 1, mound[1])
        b.pxs([(9, 14), (22, 14), (9, 25), (22, 25)], "ac")          # pad markers
        b.pxs([(6, 25), (6, 24)], "S")                               # beacon
        b.outline()
        b.px(6, 23, "ac")
        b.px(6, 22, "ah")
    elif kind == "steampunk":                                        # a cog: teeth round the rim
        for k in range(12):
            ang = math.radians(k * 30 + 15)
            b.blob(CX + math.cos(ang) * 13.6, 19 + math.sin(ang) * 10.6, 1.4, 1.4, "ac", "ah", "ad")
        b.ring(CX, 19.5, 10.5, "ad", 1, ry=8)
        b.outline()
        b.pxs([(5, 19), (26, 19), (15, 10), (16, 27)], "ah")         # rivets
        b.line([(5, 24), (8, 26)], "S")
    elif kind == "medieval":                                         # a stone keep: merlons along the back
        for deg in range(200, 341, 20):
            x = CX + math.cos(math.radians(deg)) * 12.4
            y = 19 + math.sin(math.radians(deg)) * 9.6
            for dx in (0, 1):
                for dy in (0, 1):
                    b.px(x - 1 + dx, y - 2 + dy, "s")
        b.ring(CX, 19.5, 10.5, mound[2], 1, ry=8)
        for x, y in ((6, 20), (12, 26), (20, 27), (25, 21), (9, 15), (22, 14)):   # mortar
            b.pxs([(x, y), (x + 1, y)], mound[2])
        b.outline()
        for x, y in ((4, 22), (27, 23)):
            b.pxs([(x, y), (x + 1, y - 1), (x + 1, y)], "m")
    elif kind == "samurai":                                          # raked sand and a torii
        b.ring(CX, 19.5, 10.5, mound[2], 1, ry=8)
        b.ring(CX, 19.5, 12, mound[2], 1, ry=9.2, gaps=lambda a: int(a // 20) % 2 == 0)
        for y in range(9, 13):
            b.pxs([(6, y), (10, y)], "r")
        for x in range(5, 12):
            b.px(x, 8, "r")
        for x in range(6, 11):
            b.px(x, 10, "R")
        b.outline()
        b.pxs([(24, 15), (5, 22), (21, 27), (26, 23)], "m")          # fallen petals
        b.pxs([(25, 15), (22, 27)], "M")
    elif kind == "candy":                                            # iced biscuit with a lollipop
        b.blob(CX, 18.5, 11, 8, "ac", "ah", "ad")
        b.line([(5, 13), (5, 17)], "c")
        b.blob(5.5, 11, 2.4, 2.4, "r", "w", "R")
        b.outline()
        for (x, y), c in zip(((8, 17), (23, 15), (11, 23), (21, 23), (17, 13), (25, 20), (7, 21)), "ybwrybw"):
            b.px(x, y, c)
        b.pxs([(5, 11), (6, 10)], "w")
    elif kind == "jungle":                                           # leaves, a vine and bamboo stakes
        b.ring(CX, 19.5, 10.5, "M", 1, ry=8, gaps=lambda a: int(a // 25) % 3 == 0)
        b.blob(4.5, 25, 3.2, 1.7, "M", "m", "j")
        b.blob(27.5, 24, 3.2, 1.7, "M", "m", "j")
        b.blob(26, 12.5, 2.6, 1.5, "M", "m", "j")
        b.line([(7, 8), (7, 12)], "#c8b060")
        b.line([(9, 9), (9, 12)], "#a89040")
        b.outline()
        b.pxs([(7, 10), (9, 11)], "#7a6428")
        b.pxs([(22, 26), (10, 25)], "ac")                            # flowers
        b.pxs([(23, 26), (10, 24)], "ah")
    elif kind == "pirate":                                           # a sand islet with planks, a keg and coins
        sh.ring(CX, 20.5, 16, "#3aa8d8", 1.3, a=95, ry=12.5)
        b.ring(CX, 19.5, 10.5, "D", 1, ry=8, gaps=lambda a: int(a // 12) % 4 == 0)
        box(b, 6, 9, 8, 12, "D", "d", "e", cut=False)
        b.outline()
        b.pxs([(6, 10), (7, 10), (8, 10), (6, 12), (7, 12), (8, 12)], "S")
        b.pxs([(22, 26), (23, 25), (24, 26), (21, 27)], "y")
        b.px(23, 25, "w")
        for x, y in ((4, 21), (27, 22)):
            b.pxs([(x, y), (x + 1, y - 1), (x + 1, y)], "m")
    elif kind == "robot":                                            # a steel deck with a hazard ring
        b.ring(CX, 19.5, 10.5, "k", 1, ry=8)
        b.ring(CX, 19.5, 10.5, "y", 1, ry=8, gaps=lambda a: int(a // 15) % 2 == 0)
        b.line([(6, 11), (6, 13)], "S")
        b.px(6, 10, "r")
        b.outline()
        b.pxs([(4, 19), (27, 19), (15, 9), (16, 28)], "s")           # bolts
        b.px(6, 10, "f")
    else:
        b.ring(CX, 19.5, 10.5, mound[2], 1, ry=8)
        for y in range(9, 13):                                       # headstone, standing proud of the rim
            b.pxs([(7, y), (8, y), (9, y)], "s")
        b.px(8, 8, "s")
        b.pxs([(22, 26), (23, 26), (24, 25), (9, 26)], "c")          # bones
        b.outline()
        b.pxs([(8, 10), (7, 10), (9, 10), (8, 9), (8, 11)], "S")     # the cross on it
        for x, y in ((4, 20), (27, 21), (5, 23)):
            b.pxs([(x, y), (x + 1, y - 1), (x + 1, y)], "m")
        b.pxs([(25, 22), (25, 21)], "c")                             # candle
        b.px(25, 20, "u")
        b.px(25, 19, "y")
    L["Base"].paste(b)


# ================================================================ enemies
def themed_beetle(w, h, cx, cy, Lh, Wh, shell, kind="plain", phase=0.0, mand=0, bob=0):
    if _theme["kind"] == "military":                                 # side-on, like its towers
        return military.side_beetle(w, h, cx, cy, Lh, Wh, shell, kind, phase, mand, bob)
    cv = _plain["beetle"](w, h, cx, cy, Lh, Wh, shell, kind, phase, mand, bob)
    cx += bob
    hx = cx + Lh + 1.5
    look = _theme["kind"]
    if look == "neon":                                               # a lit seam, running lights, a visor
        cv.line([(cx - Lh + 1.5, cy - 0.5), (cx + Lh - 3, cy - 0.5)], "ac")
        for side in (-1, 1):
            cv.px(cx - Lh * 0.4, cy - 0.5 + side * Wh * 0.55, "ah")
            cv.px(cx + Lh * 0.2, cy - 0.5 + side * Wh * 0.6, "ac")
        cv.pxs([(hx + 1, cy - 1), (hx + 1, cy)], "ac")
    elif look in SUITED:                                             # an alien: glowing spots and eyes
        if look == "neonspace":
            cv.line([(cx - Lh + 1.5, cy - 0.5), (cx + Lh - 3, cy - 0.5)], "ad")
        for t in (-0.55, -0.1, 0.35):
            for side in (-1, 1):
                cv.px(cx - 1 + Lh * t, cy - 0.5 + side * Wh * 0.5, "ac")
        cv.pxs([(hx + 1, cy - 2), (hx + 1, cy + 1)], "ah")
    elif look in ("steampunk", "medieval", "samurai", "candy", "jungle", "pirate", "robot"):
        def across(t, c, a=0.6):
            x = cx - 1 + Lh * t
            cv.line([(x, cy - Wh * a), (x, cy - 2)], c)
            cv.line([(x, cy + 1), (x, cy + Wh * a - 1)], c)
        eyes = [(hx + 1, cy - 2), (hx + 1, cy + 1)]
        if look == "steampunk":                                      # brass seam, rivets, lenses
            cv.line([(cx - Lh + 1.5, cy - 0.5), (cx + Lh - 3, cy - 0.5)], "ac")
            for t in (-0.55, -0.1, 0.35):
                for side in (-1, 1):
                    cv.px(cx - 1 + Lh * t, cy - 0.5 + side * Wh * 0.55, "ah")
            cv.pxs(eyes, "b")
        elif look == "medieval":                                     # a heraldic cross on the shell
            cv.line([(cx - Lh + 1.5, cy - 0.5), (cx + Lh - 3, cy - 0.5)], "ac")
            across(0.0, "ac")
        elif look == "samurai":                                      # lamellar plates, gold eyes
            for t in (-0.55, -0.2, 0.15):
                across(t, "ac")
            cv.pxs(eyes, "y")
        elif look == "candy":                                        # gloss and sprinkles
            cv.pxs([(cx - Lh * 0.5, cy - Wh * 0.6), (cx - Lh * 0.5 + 1, cy - Wh * 0.6)], "w")
            for t, c in ((-0.5, "y"), (-0.1, "ac"), (0.3, "w")):
                cv.px(cx - 1 + Lh * t, cy - 0.5 + Wh * 0.45, c)
                cv.px(cx + Lh * t, cy - 0.5 - Wh * 0.3, c)
            cv.pxs(eyes, "w")
        elif look == "jungle":                                       # war paint
            across(-0.4, "ac")
            across(0.1, "c", 0.45)
            cv.pxs(eyes, "ac")
        elif look == "pirate":                                       # striped shirt and a patch
            for t in (-0.6, -0.25, 0.1):
                across(t, "c")
            cv.pxs([(hx + 1, cy - 2), (hx, cy - 2)], "k")
            cv.px(hx + 1, cy + 1, "ac")
        else:                                                        # panel lines, hazard strip, lamps
            across(-0.1, "k", 0.7)
            for j in range(int(Wh * 1.2)):
                cv.px(cx - Lh + 2, cy - Wh * 0.6 + j, "y" if j % 2 else "k")
            cv.pxs(eyes, "ac")
    else:                                                            # bare ribs across the back, lit sockets
        for t in (-0.55, -0.2, 0.15):
            x = cx - 1 + Lh * t
            cv.line([(x, cy - Wh * 0.6), (x, cy - 2)], "k")
            cv.line([(x, cy + 1), (x, cy + Wh * 0.6 - 1)], "k")
        cv.pxs([(hx + 1, cy - 2), (hx + 1, cy + 1)], "ac")
    return cv


# ---- everything that is not a beetle: centipedes, mantises, and the creatures in the roster ----
def body_px(cv, x, y):
    """True where the creature's own body is painted (not empty, not ink)."""
    x, y = int(math.floor(x)), int(math.floor(y))
    if not (0 <= x < cv.w and 0 <= y < cv.h):
        return False
    v = cv.p[x, y]
    return v[3] > 100 and v[:3] != base.rgba("k")[:3]


def vstripe(cv, x, y0, y1, c):
    for y in range(int(math.floor(y0)), int(math.floor(y1)) + 1):
        if body_px(cv, x, y):
            cv.px(x, y, c)


def repaint(cv, cx, cy, r, ramp):
    """Repaint the body inside a circle from (dark, mid, light), by how bright each pixel was."""
    for y in range(int(cy - r) - 1, int(cy + r) + 2):
        for x in range(int(cx - r) - 1, int(cx + r) + 2):
            if (x + 0.5 - cx) ** 2 + (y + 0.5 - cy) ** 2 <= r * r and body_px(cv, x, y):
                v = cv.p[x, y]
                lum = (v[0] * 3 + v[1] * 5 + v[2] * 2) / 10
                cv.px(x, y, ramp[0] if lum < 85 else ramp[1] if lum < 165 else ramp[2])


def triangle(cv, a, b, c, col):
    def side(p, q, x, y):
        return (q[0] - p[0]) * (y - p[1]) - (q[1] - p[1]) * (x - p[0])
    xs, ys = (a[0], b[0], c[0]), (a[1], b[1], c[1])
    for y in range(int(min(ys)) - 1, int(max(ys)) + 2):
        for x in range(int(min(xs)) - 1, int(max(xs)) + 2):
            d = [side(a, b, x + 0.5, y + 0.5), side(b, c, x + 0.5, y + 0.5), side(c, a, x + 0.5, y + 0.5)]
            if all(v >= 0 for v in d) or all(v <= 0 for v in d):
                cv.px(x, y, col)


def dress(cv, head, trunk):
    """Theme gear for a creature facing right. head = (x, y, radius) or None; trunk = (x, y, rx, ry) or None.
    Markings only recolour what is already body; headgear is drawn on top with its own outline."""
    kind = _theme["kind"]
    gear, after = Cv(cv.w, cv.h), []
    if trunk:
        tx, ty, rx, ry = trunk
        top, bot = ty - ry - 2, ty + ry + 2

        def band(t, c):
            for j in range(2 if rx > 8 else 1):
                vstripe(cv, tx + rx * t + j, top, bot, c)

        def dots(t, c, f=0.5):
            for side in (-1, 1):
                x, y = tx + rx * t, ty - 0.5 + side * ry * f
                if body_px(cv, x, y):
                    cv.px(x, y, c)

        def spine(c):
            stripe(cv, ty - 0.5, tx - rx * 0.7, tx + rx * 0.6, c)

        if kind == "neon":
            spine("ac")
            dots(-0.5, "ah")
            dots(0.2, "ac")
        elif kind in SUITED:
            if kind == "neonspace":
                spine("ad")
            for t in (-0.6, -0.1, 0.4):
                dots(t, "ac")
            dots(-0.35, "ah", 0.15)
        elif kind == "spooky":
            for t in (-0.5, -0.1, 0.3):
                band(t, "k")
        elif kind == "steampunk":
            spine("ac")
            for t in (-0.6, -0.1, 0.4):
                dots(t, "ah")
        elif kind == "medieval":
            spine("ac")
            band(0.0, "ac")
        elif kind == "samurai":
            for t in (-0.55, -0.15, 0.25):
                band(t, "ac")
            dots(-0.35, "y", 0.3)
            dots(0.05, "y", 0.3)
        elif kind == "candy":
            for t, c in ((-0.5, "ac"), (-0.1, "w"), (0.3, "ah")):
                band(t, c)
            dots(-0.3, "y", 0.4)
            dots(0.1, "b", 0.6)
        elif kind == "jungle":
            band(-0.3, "ac")
            dots(-0.6, "c")
            dots(0.1, "c")
            dots(0.35, "ac", 0.3)
        elif kind == "pirate":
            for j, t in enumerate((-0.65, -0.35, -0.05, 0.25)):
                band(t, "ac" if j % 2 else "c")
        elif kind == "robot":
            band(-0.1, "k")
            x = tx - rx * 0.7
            for j, y in enumerate(range(int(math.floor(top)), int(math.floor(bot)) + 1)):
                if body_px(cv, x, y):
                    cv.px(x, y, "y" if j % 2 else "k")
            dots(0.4, "s")
    if head:
        hx, hy, r = head
        ex, e = hx + r * 0.35, max(1.0, r * 0.6)
        eyes = [(ex, hy - e), (ex, hy + e - 0.01)]
        big = r >= 3.5

        def put_eyes(c, back=None):
            for x, y in eyes:
                after.append((x, y, c))
                if big or back:
                    after.append((x - 1, y, back or c))

        if kind == "neon":                                           # a visor across the face
            vstripe(cv, ex, hy - r, hy + r, "ac")
            if big:
                vstripe(cv, ex + 1, hy - r, hy + r, "ah")
        elif kind in SUITED:                                         # lit alien eyes
            put_eyes("ah")
        elif kind == "spooky":                                       # a bare skull
            repaint(cv, hx, hy, r + 0.3, ("s", "c", "w"))
            put_eyes("ac", back="k")
        elif kind == "steampunk":                                    # top hat seen from above, goggles
            gear.disc(hx - r * 0.3, hy, max(1.3, r * 0.75), "e")
            gear.disc(hx - r * 0.3, hy, max(0.9, r * 0.45), "ac")
            gear.disc(hx - r * 0.3, hy, max(0.4, r * 0.25), "x")
            put_eyes("b")
        elif kind == "medieval":                                     # steel helm, visor slit, a plume laid back
            repaint(cv, hx, hy, r + 0.3, ("S", "s", "c"))
            vstripe(cv, ex, hy - r * 0.7, hy + r * 0.7, "k")
            for j in range(2 if big else 1):
                gear.line([(hx - r * 0.2, hy - 0.5 - j), (hx - r * 1.6 - 1.5, hy - 0.5 - j)], "ac")
        elif kind == "samurai":                                      # lacquered helmet with a gold crest
            repaint(cv, hx, hy, r + 0.3, ("ad", "ad", "ac"))
            for side in (-1, 1):
                gear.line([(hx, hy - 0.5 + side * r * 0.6), (hx + r * 0.7, hy - 0.5 + side * (r + 1.5))], "y")
            put_eyes("y")
        elif kind == "candy":                                        # a cherry on top
            put_eyes("k")
            gear.disc(hx - r * 0.3, hy, max(1.0, r * 0.5), "r")
            after.append((hx - r * 0.3 - 0.5, hy - 0.6, "f"))
        elif kind == "jungle":                                       # war paint and a fan of feathers
            vstripe(cv, hx, hy - r, hy + r, "ac")
            for side, c in ((-1, "g"), (1, "g"), (0, "ac")):
                gear.line([(hx - r * 0.4, hy - 0.5 + side * r * 0.5), (hx - r * 1.4 - 2, hy - 0.5 + side * (r + 1))], c)
            put_eyes("k")
        elif kind == "pirate":                                       # tricorn
            triangle(gear, (hx + r * 0.9, hy), (hx - r * 0.9, hy - r - 1), (hx - r * 0.9, hy + r + 1), "v")
            after.append((hx - r * 0.3, hy - 0.5, "c"))
            after.append((hx + r * 0.9 - 1, hy - 0.5, "y"))
        elif kind == "robot":                                        # a plated head with two lamps
            box(gear, hx - r, hy - r * 0.8, hx + r * 0.7, hy + r * 0.8 - 1, "a", "A", "z", cut=False)
            put_eyes("ac")
    if gear.im.getbbox():
        gear.outline()
        cv.paste(gear)
    for x, y, c in after:
        cv.px(x, y, c)
    return cv


def dressed_creature(cv, head, trunk):
    cv.im = recoloured(cv.im)
    cv.p = cv.im.load()
    return dress(cv, head, trunk)


def themed_centipede(w, h, cx, cy, segs, spacing, phase, colors=("L", "l", "Lz"), seg_r=(2.0, 2.6),
                     amp=1.0, offsets=None, glow=None):
    if _theme["kind"] == "military":
        return military.side_centipede(w, h, cx, cy, segs, spacing, phase, colors, seg_r, amp, offsets, glow)
    cv = _plain["centipede"](w, h, cx, cy, segs, spacing, phase, colors, seg_r, amp, offsets, glow)
    hx, hy = cx + (segs - 1) * spacing / 2, cy + amp * math.sin(phase)
    if offsets:
        hx, hy = hx + offsets[0][0], hy + offsets[0][1]
        return dressed_creature(cv, (hx, hy, seg_r[0] + 0.4), None)
    return dressed_creature(cv, (hx, hy, seg_r[0] + 0.4), (cx - spacing / 2, cy, (segs - 2) * spacing / 2 + 1, seg_r[1] + amp))


def themed_mantis(w, h, cx, cy, phase, raise_arms=0.0, glow=0.0):
    if _theme["kind"] == "military":
        return military.side_mantis(w, h, cx, cy, phase, raise_arms, glow)
    cv = _plain["mantis"](w, h, cx, cy, phase, raise_arms, glow)
    return dressed_creature(cv, (cx + 9, cy, 2.2), (cx - 5, cy, 6.5, 3.2))


def _snail(phase, dx, pose):
    out = 1 - pose.get("hide", 0.0)
    head = (20 + dx + 13 * out + math.sin(phase) * 1.2, 20.5, max(1.5, 3 * out)) if out > 0.3 else None
    return head, (17 + dx, 19.5, 10, 9)


def _grub(phase, dx, pose):
    keep = 5 - int(round(pose.get("sink", 0.0) * 5))
    head = (18 + dx + math.sin(phase) * 0.9, 13.1, 2.3) if keep > 0 else None
    return head, ((11 + dx, 12.5, 6, 3.6) if keep >= 4 else None)


# where each roster creature's head and trunk are, from its own drawing code: (phase, dx, pose) -> head, trunk
GEOMETRY = {
    "EnemyWasp": lambda ph, dx, po: ((22 + dx, 14.5, 2.4), (11 + dx + po.get("curl", 0) * 1.5, 14.5, 5.5 - po.get("curl", 0), 3.4)),
    "EnemySpider": lambda ph, dx, po: ((18.5 + dx, 16.5, 2.7), (11.5 + dx, 16.5, 4.8, 4.4)),
    "EnemySnail": _snail,
    "EnemyGrub": _grub,
    "EnemyBoss": lambda ph, dx, po: ((48.5 + dx, 32.5, 3.5), None),
    "EnemyMantisQueen": lambda ph, dx, po: ((46 + dx, 32.5, 4.5), (16 + dx + po.get("lay", 0) * 2, 32.5, 13 - po.get("lay", 0) * 2, 6.5)),
    "EnemyHornet": lambda ph, dx, po: ((43 + dx, 26.5 + po.get("drop", 0) * 8, 4.5),
                                       (21 + dx + po.get("curl", 0) * 3, 26.5 + po.get("drop", 0) * 8, 10.5 - po.get("curl", 0) * 2, 6.6)),
}


def themed_creature(name, caste, size, body, *args, **kw):
    if _theme["kind"] == "military":                                 # the creature's own side-on drawing, same poses
        return _plain["creature"](name, caste, size, military.SIDE[name], *args, **kw)
    where = GEOMETRY[name]

    def body2(phase=0.0, dx=0, strike=0, **pose):
        return dressed_creature(body(phase, dx=dx, strike=strike, **pose), *where(phase, dx, pose))
    return _plain["creature"](name, caste, size, body2, *args, **kw)


def glow_rim(im):
    """A soft line of the sprite's light round everything on the layer, outside its ink outline."""
    out = im.copy()
    src, p = im.load(), out.load()
    col = base.rgba("ac", 140)
    for y in range(im.height):
        for x in range(im.width):
            if src[x, y][3]:
                continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, ny = x + dx, y + dy
                if 0 <= nx < im.width and 0 <= ny < im.height and src[nx, ny][3] > 100:
                    p[x, y] = col
                    break
    return out


def recoloured(im):
    """The creatures that are not built from beetle() carry colours the palette swap does not reach.
    Space turns them alien by rotating the colour channels, Spooky drains them toward ash, and most
    of the others pull them toward the theme's own material. Applied before dress() adds the gear."""
    kind = _theme["kind"]
    tone = _theme.get("tone")
    if kind not in ("space", "neonspace", "spooky") and not tone:
        return im
    ink = base.rgba("k")[:3]
    if tone:
        tr, tg, tb, _ = hexa(tone[0])
        t = tone[1]
    out = im.copy()
    p = out.load()
    for y in range(out.height):
        for x in range(out.width):
            r, g, b, a = p[x, y]
            if not a or (r, g, b) == ink:
                continue
            if kind in ("space", "neonspace"):
                p[x, y] = (b, r, g, a)
            elif tone:                                               # pulled toward the theme's own material
                k = (r * 3 + g * 5 + b * 2) / 2550 * 1.15
                p[x, y] = (min(255, int(r * (1 - t) + tr * k * t)), min(255, int(g * (1 - t) + tg * k * t)),
                           min(255, int(b * (1 - t) + tb * k * t)), a)
            else:
                lum = (r * 3 + g * 5 + b * 2) / 10
                p[x, y] = (int(r * 0.3 + lum * 0.68), int(g * 0.3 + lum * 0.7), min(255, int(b * 0.3 + lum * 0.7 + (12 if lum < 110 else 4))), a)
    return out


def themed_elite(sprite, theme):
    """The theme's own elite: the body steeped in the livery colour (shading kept) and a two-colour rim outside its outline."""
    tint, t, rim_a, rim_b = ELITE[theme]
    tr, tg, tb, _ = hexa(tint)
    rims = (hexa(rim_a), hexa(rim_b))
    ink = base.rgba("k")[:3]

    def livery(im):
        out = im.copy()
        src, p = im.load(), out.load()
        for y in range(im.height):
            for x in range(im.width):
                r, g, b, a = src[x, y]
                if a:
                    if (r, g, b) != ink:
                        k = 0.45 + (r * 3 + g * 5 + b * 2) / 2550 * 0.9
                        p[x, y] = (min(255, int(r * (1 - t) + tr * k * t)), min(255, int(g * (1 - t) + tg * k * t)),
                                   min(255, int(b * (1 - t) + tb * k * t)), a)
                    continue
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < im.width and 0 <= ny < im.height and src[nx, ny][3] > 100:
                        p[x, y] = rims[1 if (x + y) % 4 == 0 else 0]
                        break
        return out

    return retitled(sprite, sprite.name + "Elite" + theme, f"{sprite.caste}, elite, {theme.lower()} theme", sprite.tags, livery)


def retitled(sprite, name, caste, tags, body=None, shadow=None):
    s = Sprite(name, sprite.group, sprite.w, sprite.h, sprite.layers, caste)
    for tag, frames, ms, repeat in tags:
        if body:
            frames = [dict(f, Body=body(f["Body"])) for f in frames]
        if shadow:
            frames = [dict(f, Shadow=shadow(f["Shadow"])) for f in frames]
        s.tag(tag, frames, ms, repeat)
    return s


def build_enemies(theme):
    plain = base.build_enemies()
    moves = {m.name[:-len("Moves")]: m for m in extra.build_enemy_moves()}
    bosses = elite.build_bosses()
    out = []
    for s in plain + roster.build_enemies() + bosses:
        tags = list(s.tags) + (list(moves[s.name].tags) if s.name in moves else [])
        whole = retitled(s, s.name, s.caste, tags, shadow=military.flat_shadow if _theme["kind"] == "military" else None)
        out.append(retitled(whole, s.name + theme, f"{s.caste}, {theme.lower()} theme", whole.tags,
                            glow_rim if _theme["kind"] in GLOWING else None))
        if s not in bosses:
            out.append(themed_elite(whole, theme))
    return out


# ================================================================ shots, effects, hill, burrow, ground
def build_extras(theme):
    """Everything else, painted from the theme's palette and named <Name><Theme>, in three lots:
    Extras  shots, hit and status effects, the towers' own extras, the hill and the burrow
    World   decor, lights, buildings, objects, particles and weather, water, terrain, plants, rocks,
            the ambient ants, the queen and her chamber, food pickups
    Ui      wave banner, victory and defeat, cursors, tile markers, and the animated interface pieces"""
    set_accent(0)
    plain = _theme["kind"] == "military"                             # its top-down pieces keep the meadow ant
    base.tower_base = _plain["tower_base"] if plain else themed_base
    base.ant = wpn.ant = _plain["ant"] if plain else themed_ant
    lots = {
        "Extras": [base.build_fx(), base.build_status(), base.build_projectiles(), base.build_impacts(), base.build_props(),
                   wpn.build_tower_extras(), wpn.build_melee_fx(), roster.build_shots(), extra.build_hill_hit(),
                   extra.build_status_overlays()],
        "World": [extra.build_decor(), scenery.build_lights(), scenery.build_buildings(), scenery.build_objects(),
                  scenery.build_particles(), nature.build_water(), nature.build_terrain(), nature.build_plants(),
                  nature.build_colony(), world.build_queen(), world.build_chamber(), world.build_pickups()],
        "Ui": [world.build_banner(), world.build_victory(), world.build_defeat(), world.build_cursor(), world.build_marker(),
               hud.build_health_bar(), hud.build_food_counter(), hud.build_wave_track(), hud.build_skill_node(),
               hud.build_button(), hud.build_tooltip(), hud.build_star(), hud.build_logo()],
    }
    out = {}
    for lot, made in lots.items():
        out[lot] = []
        for item in made:
            for s in (item if isinstance(item, (list, tuple)) else [item]):
                s.name += theme
                s.caste = f"{s.caste}, {theme.lower()} theme" if s.caste else f"{theme.lower()} theme"
                out[lot].append(s)
    return out


def build_tiles(theme):
    """Ground64 and two 32 px road tiles (with and without edge lines), as build_tiles.py makes for the meadow."""
    import random
    t = THEMES[theme]
    rng = random.Random(theme)
    if theme in FLOORS:                                              # plain ground: speckle, and tufts of two pixels
        (g0, g1, g2), (r0, r1, r2, edge) = [[hexa(c) for c in part] for part in FLOORS[theme]]
        ground = Image.new("RGBA", (64, 64), g0)
        g = ground.load()
        for _ in range(170):
            g[rng.randrange(64), rng.randrange(64)] = g1 if rng.random() < 0.5 else g2
        for _ in range(14):
            x, y = rng.randrange(63), rng.randrange(1, 64)
            g[x, y] = g[x + 1, y - 1] = g1
        if theme == "Robot":                                         # plate seams and rivets
            for j in range(64):
                g[j, 0] = g[0, j] = g[j, 32] = g[32, j] = g2
            for x in (3, 29, 35, 61):
                for y in (3, 29, 35, 61):
                    g[x, y] = g1
        if theme == "Pirate":                                        # wave crests
            for _ in range(10):
                x, y = rng.randrange(60), rng.randrange(64)
                for j in range(4):
                    g[x + j, y] = hexa("#6ab0c8")
        tiles = {"Ground" + theme: ground}
        for name, edges in (("Road" + theme, True), ("RoadFill" + theme, False)):
            road = Image.new("RGBA", (32, 32), r0)
            r = road.load()
            for _ in range(70):
                r[rng.randrange(32), rng.randrange(2, 30)] = r1 if rng.random() < 0.5 else r2
            if edges:
                for x in range(32):
                    r[x, 0] = r[x, 31] = edge
                    r[x, 1] = r[x, 30] = r2
            tiles[name] = road
        return tiles
    dark, mid, lite = hexa(t["pal"]["d"]), hexa(t["pal"]["T"]), hexa(t["pal"]["t"])
    line, glow = hexa(t["accents"][0][2]), hexa(t["accents"][0][0])
    second = hexa(t["accents"][1][0])
    ground = Image.new("RGBA", (64, 64), dark)
    g = ground.load()
    for _ in range(90):                                              # grain
        g[rng.randrange(64), rng.randrange(64)] = mid
    for k in range(0, 64, 16):                                       # grid, with a lit node where lines cross
        for j in range(64):
            g[k, j] = g[j, k] = line
    for x in range(0, 64, 16):
        for y in range(0, 64, 16):
            if rng.random() < 0.5:
                g[x, y] = glow
    if t["kind"] != "vapor":
        for _ in range(7):                                           # stars, or stray pixels of light
            g[rng.randrange(1, 63), rng.randrange(1, 63)] = hexa(t["pal"]["c"])
    tiles = {"Ground" + theme: ground}
    for name, edges in (("Road" + theme, True), ("RoadFill" + theme, False)):
        road = Image.new("RGBA", (32, 32), mid)
        r = road.load()
        for _ in range(40):
            r[rng.randrange(32), rng.randrange(2, 30)] = lite if rng.random() < 0.5 else dark
        for x in range(0, 32, 8):                                    # a broken centre line
            for j in range(4):
                r[x + j, 15] = r[x + j, 16] = second
        if edges:
            for x in range(32):
                r[x, 0] = r[x, 31] = dark
                r[x, 1] = r[x, 30] = glow
                r[x, 2] = r[x, 29] = line
        tiles[name] = road
    return tiles


def extras_sheet(theme, sprites, tiles, path, scale=2):
    """The middle frame of each tag of each extra sprite, then the tiles (the ground laid 2 x 2)."""
    t = THEMES[theme]
    font = ImageFont.load_default()
    rows = [(s, [f[len(f) // 2] for _, f, _, _ in s.tags][:8]) for s in sprites]
    cols = 4
    cw = max(140, max(len(fr) * (s.w * scale + 4) for s, fr in rows) // 2 + 150)
    rh = [max(s.h * scale + 18 for s, _ in rows[i:i + cols]) for i in range(0, len(rows), cols)]
    out = Image.new("RGBA", (cols * cw, sum(rh) + (150 if tiles else 8)), hexa(t["backdrop"]))
    d = ImageDraw.Draw(out)
    y = 0
    for i, (s, frs) in enumerate(rows):
        x = (i % cols) * cw
        d.text((x + 4, y + 2), s.name[:-len(theme)], fill=(255, 255, 255, 220), font=font)
        for j, f in enumerate(frs):
            im = Image.new("RGBA", (s.w, s.h), (0, 0, 0, 0))
            for name in s.layers:
                im.alpha_composite(f[name])
            im = im.resize((s.w * scale, s.h * scale), Image.NEAREST)
            if x + 4 + j * (s.w * scale + 4) + im.width <= x + cw:
                out.alpha_composite(im, (x + 4 + j * (s.w * scale + 4), y + 16))
        if i % cols == cols - 1:
            y += rh[i // cols]
    y = sum(rh) + 6
    x = 4
    for name, im in tiles.items():
        if im.width == 64:
            big = Image.new("RGBA", (128, 128))
            for a in (0, 64):
                for b2 in (0, 64):
                    big.paste(im, (a, b2))
            im = big
        else:
            wide = Image.new("RGBA", (128, 32))
            for a in range(0, 128, 32):
                wide.paste(im, (a, 0))
            im = wide.resize((256, 64), Image.NEAREST)
        out.paste(im, (x, y + 14))
        d.text((x, y), name, fill=(255, 255, 255, 220), font=font)
        x += im.width + 12
    out.save(path)


# ================================================================ towers
def build_towers(theme):
    """{tower name: [tier 1, tier 2, tier 3]}"""
    if _theme["kind"] == "military":                                 # its own view and its own drawing: see build_military_sheets
        return military.build_towers(theme, ALL_TOWERS)
    out = {}
    for index, (name, caste, draw, kw) in enumerate(ALL_TOWERS):
        set_accent(index)
        row = []
        for tier in (1, 2, 3):
            tiers._tier = tier
            fb, fa = (themed_base, themed_ant) if tier == 1 else (tiers.tiered_base, tiers.tiered_ant)
            base.tower_base, base.ant, wpn.ant = fb, fa, fa
            suffix = f"T{tier}" if tier > 1 else ""
            row.append(tower_sprite(name + suffix + theme, f"{caste}, tier {tier}, {theme.lower()} theme",
                                    tiers.dressed(draw), **kw))
        out[name] = row
    return out


class themed:
    """Swap the palette and the shared drawing functions for one theme, and put them back after."""

    def __init__(self, theme):
        self.theme = theme

    def __enter__(self):
        global _theme
        _theme = THEMES[self.theme]
        self.pal = dict(base.PAL)
        base.PAL.update(_theme["pal"])
        set_accent(0)
        self.saved = dict(tier2=dict(tiers.TIERS[2]), tb=tiers._plain_base, ta=tiers._plain_ant, tier=tiers._tier,
                          ink=elite.INK, wdirt=wpn.DIRT, edirt=extra.DIRT)
        tiers.TIERS[2].update(cloth="ac", cloth_d="ad")              # the tan banner would vanish into the mound
        tiers._plain_base, tiers._plain_ant = themed_base, themed_ant
        elite.INK = base.rgba("k")[:3]
        wpn.DIRT = extra.DIRT = _theme["dirt"]
        for mod in (base, extra, roster):
            mod.beetle = themed_beetle
        for mod in (base, extra):
            mod.centipede, mod.mantis = themed_centipede, themed_mantis
        roster.creature = themed_creature

        def init(sprite, name, *a, **k):                             # each enemy keeps one colour of light
            if name.startswith("Enemy"):
                set_accent(zlib.crc32(core_name(name).encode()))
            _plain["sprite_init"](sprite, name, *a, **k)
        Sprite.__init__ = init
        return self

    def __exit__(self, *exc):
        global _theme
        _theme = None
        base.PAL.clear()
        base.PAL.update(self.pal)
        s = self.saved
        tiers.TIERS[2].clear()
        tiers.TIERS[2].update(s["tier2"])
        tiers._plain_base, tiers._plain_ant, tiers._tier = s["tb"], s["ta"], s["tier"]
        elite.INK, wpn.DIRT, extra.DIRT = s["ink"], s["wdirt"], s["edirt"]
        base.tower_base, base.ant, wpn.ant = _plain["tower_base"], _plain["ant"], _plain["ant"]
        for mod in (base, extra, roster):
            mod.beetle = _plain["beetle"]
        for mod in (base, extra):
            mod.centipede, mod.mantis = _plain["centipede"], _plain["mantis"]
        roster.creature = _plain["creature"]
        Sprite.__init__ = _plain["sprite_init"]


# ================================================================ review sheets
def hexa(c):
    c = c.lstrip("#")
    return (int(c[0:2], 16), int(c[2:4], 16), int(c[4:6], 16), 255)


def tower_sheet(theme, towers, path, scale=3):
    """Two blocks of towers; per tower: tiers 1 to 3 idle, then tiers 1 to 3 mid-attack."""
    t = THEMES[theme]
    font = ImageFont.load_default()
    cell, label = TW * scale + 6, 104
    per = (len(towers) + 1) // 2
    block = label + cell * 6 + 22
    out = Image.new("RGBA", (block * 2, per * cell + 22), hexa(t["backdrop"]))
    d = ImageDraw.Draw(out)
    for blk in range(2):
        for col, text in enumerate(("T1", "T2", "T3", "T1 hit", "T2 hit", "T3 hit")):
            d.text((blk * block + label + col * cell + (10 if col >= 3 else 0) + 30, 4), text, fill=(255, 255, 255, 200), font=font)
    for r, (name, row) in enumerate(towers.items()):
        bx, by = (r // per) * block, 20 + (r % per) * cell
        d.text((bx + 6, by + cell // 2 - 6), name[5:], fill=(255, 255, 255, 230), font=font)
        for col in range(6):
            im = tiers.frame_image(row[col % 3], ("Idle", "Attack")[col // 3], (0, 2)[col // 3])
            tile = Image.new("RGBA", (TW, TW), hexa(t["cell"]))
            tile.alpha_composite(im)
            out.paste(tile.resize((TW * scale, TW * scale), Image.NEAREST), (bx + label + col * cell + (10 if col >= 3 else 0), by))
    out.save(path)


def enemy_sheet(theme, enemies, path, scale=2):
    """The first walk frame and a mid-attack frame of every enemy."""
    t = THEMES[theme]
    font = ImageFont.load_default()
    cols, cw, ch = 6, 176, 64 * 2 + 18
    rows = (len(enemies) + cols - 1) // cols
    out = Image.new("RGBA", (cols * cw, rows * ch), hexa(t["backdrop"]))
    d = ImageDraw.Draw(out)
    for i, s in enumerate(enemies):
        x, y = (i % cols) * cw, (i // cols) * ch
        tags = dict((tg[0], tg[1]) for tg in s.tags)
        for j, (tag, index) in enumerate((("Walk", 0), ("Attack", 2))):
            if s.w > 40 and j:
                break
            f = tags[tag][index]
            im = Image.new("RGBA", (s.w, s.h), hexa(t["cell"]))
            for name in s.layers:
                im.alpha_composite(f[name])
            im = im.resize((s.w * scale, s.h * scale), Image.NEAREST)
            out.paste(im, (x + 4 + j * (cw // 2 - 2) + (0 if s.w > 40 else (cw // 2 - 6 - im.width) // 2), y + 16 + (ch - 18 - im.height) // 2))
        d.text((x + 4, y + 2), s.name[5:], fill=(255, 255, 255, 220), font=font)
    out.save(path)


def compare_sheet(by_theme, path, scale=4):
    scale = scale if len(by_theme) <= 4 else 3
    """Every caste at tier 1: the meadow original beside each theme."""
    font = ImageFont.load_default()
    names = [n for n, _, _, _ in ALL_TOWERS]
    tiers.TOWERS[:], keep = ALL_TOWERS, list(tiers.TOWERS)
    try:
        originals = tiers.build(1)
    finally:
        tiers.TOWERS[:] = keep
    columns = [("Meadow", "#3e5c2e", {n: [s] for n, s in zip(names, originals)})]
    columns += [(th, THEMES[th]["cell"], towers) for th, towers in by_theme.items()]
    cell, label = TW * scale + 6, 104
    per = (len(names) + 1) // 2
    block = label + cell * len(columns) + 16
    out = Image.new("RGBA", (block * 2, per * cell + 22), (24, 24, 28, 255))
    d = ImageDraw.Draw(out)
    for blk in range(2):
        for c, (title, _, _) in enumerate(columns):
            d.text((blk * block + label + c * cell + 40, 4), title, fill=(255, 255, 255, 220), font=font)
    for r, name in enumerate(names):
        bx, by = (r // per) * block, 20 + (r % per) * cell
        d.text((bx + 6, by + cell // 2 - 6), name[5:], fill=(255, 255, 255, 230), font=font)
        for c, (_, ground, towers) in enumerate(columns):
            tile = Image.new("RGBA", (TW, TW), hexa(ground))
            tile.alpha_composite(tiers.frame_image(towers[name][0], "Idle", 0))
            out.paste(tile.resize((TW * scale, TW * scale), Image.NEAREST), (bx + label + c * cell, by))
    out.save(path)


# ================================================================ run
def main():
    args = sys.argv[1:]
    preview = "--preview" in args
    wanted = [a for a in args if not a.startswith("--")] or list(THEMES)
    by_theme = {}
    count = frames = 0
    for theme in wanted:
        with themed(theme):
            towers = build_towers(theme)
            enemies = build_enemies(theme)
            tower_sheet(theme, towers, os.path.join(HERE, f"Theme{theme}Towers.png"))
            enemy_sheet(theme, enemies, os.path.join(HERE, f"Theme{theme}Enemies.png"))
            for s in [s for row in towers.values() for s in row] + enemies:
                if not preview:
                    export(s)
                count += 1
                frames += sum(len(f) for _, f, _, _ in s.tags)
            more = ""
            lots, tiles = build_extras(theme), build_tiles(theme)
            for lot, sprites in lots.items():
                extras_sheet(theme, sprites, tiles if lot == "Extras" else {}, os.path.join(HERE, f"Theme{theme}{lot}.png"))
            extras = [s for sprites in lots.values() for s in sprites]
            for s in extras:
                if not preview:
                    export(s)
                count += 1
                frames += sum(len(f) for _, f, _, _ in s.tags)
            if not preview:
                os.makedirs(TILE_OUT, exist_ok=True)
                for name, im in tiles.items():
                    im.save(os.path.join(TILE_OUT, name + ".png"))
            more = f", {len(extras)} shots, effects, map and interface sheets, {len(tiles)} tiles"
        by_theme[theme] = towers
        print(f"{theme:9} {len(towers) * 3} towers, {len(enemies)} enemies{more}")
    compare_sheet(by_theme, os.path.join(HERE, "ThemeCompare.png"))
    print(f"{count} sprites, {frames} frames " + ("drawn (preview only)" if preview else "written and read back OK"))


if __name__ == "__main__":
    main()
