"""Builds the UI kit's 9-slice sprites into the folder above this one.

    python build_ui_sprites.py

Button*.png and Panel.png are copied from ../../Ui (the IMGUI skin's art) so the uGUI kit and the
old HUD look the same; everything else is drawn here from the same palette. Slice borders are not
stored in the PNGs: Editor/UiKitSpriteImporter.cs sets them by file name on import.
"""
from pathlib import Path
import shutil

from PIL import Image

HERE = Path(__file__).resolve().parent
OUT = HERE.parent
LEGACY = OUT.parent.parent / "Ui"

CLEAR = (0, 0, 0, 0)
OUTLINE = (0x1B, 0x11, 0x0B, 255)
GOLD_HI = (0xEC, 0xC4, 0x77, 255)
GOLD = (0xB5, 0x87, 0x3E, 255)
WOOD_HI = (0xA0, 0x64, 0x3C, 255)
WOOD = (0x83, 0x56, 0x2F, 255)
WOOD_LO = (0x5B, 0x3A, 0x23, 255)
WOOD_DARK = (0x3A, 0x24, 0x16, 255)
WELL = (0x2A, 0x1D, 0x14, 255)
WELL_LO = (0x22, 0x17, 0x0F, 255)
CREAM = (0xFF, 0xF1, 0xD6, 255)


def canvas(w, h):
    return Image.new("RGBA", (w, h), CLEAR)


def box(img, fill, outline=OUTLINE):
    """Outlined box with the four corner pixels cut, like the legacy buttons."""
    w, h = img.size
    px = img.load()
    for y in range(h):
        for x in range(w):
            edge = x in (0, w - 1) or y in (0, h - 1)
            corner = x in (0, w - 1) and y in (0, h - 1)
            px[x, y] = CLEAR if corner else (outline if edge else fill)
    # Fill the inner corners so the cut reads as a rounded outline.
    for x, y in ((1, 1), (w - 2, 1), (1, h - 2), (w - 2, h - 2)):
        px[x, y] = outline
    return px


def rows(px, x0, x1, y, colour):
    for x in range(x0, x1 + 1):
        px[x, y] = colour


def save(img, name):
    img.save(OUT / f"{name}.png")
    print(f"{name}.png  {img.size[0]}x{img.size[1]}")


def well(name, size, rim=None):
    """A dark recessed box: slider track, toggle box, value field."""
    img = canvas(size, size)
    px = box(img, WELL)
    rows(px, 2, size - 3, 1, WELL_LO)  # shadow under the top edge
    if rim is not None:
        for i in range(2, size - 2):
            px[i, 1] = px[i, size - 2] = rim
            px[1, i] = px[size - 2, i] = rim
    save(img, name)


def slider_fill():
    img = canvas(6, 6)
    px = img.load()
    for y in range(6):
        colour = GOLD_HI if y == 0 else (WOOD if y == 5 else GOLD)
        rows(px, 0, 5, y, colour)
    save(img, "SliderFill")


def slider_handle(name, face, top):
    img = canvas(8, 14)
    px = box(img, face)
    rows(px, 2, 5, 1, top)
    rows(px, 2, 5, 12, WOOD_LO)
    # Two grip notches.
    for y in (5, 8):
        rows(px, 2, 5, y, WOOD_LO)
    save(img, name)


def toggle_check():
    img = canvas(14, 14)
    px = img.load()
    # A chunky tick, two pixels thick.
    tick = [(3, 7), (4, 8), (5, 9), (6, 8), (7, 7), (8, 6), (9, 5), (10, 4)]
    for x, y in tick:
        px[x, y] = CREAM
        px[x, y - 1] = CREAM
        px[x, y + 1] = GOLD
    save(img, "ToggleCheck")


def tab(name, face, top, bottom_open):
    img = canvas(16, 16)
    px = box(img, face)
    rows(px, 2, 13, 1, top)
    if bottom_open:
        # The selected tab runs into the panel below it: no bottom edge.
        for y in (14, 15):
            px[0, y] = OUTLINE
            px[15, y] = OUTLINE
            rows(px, 1, 14, y, face)
    else:
        rows(px, 2, 13, 13, WOOD_DARK)
    save(img, name)


def arrow(name, colour):
    img = canvas(6, 9)
    px = img.load()
    for x in range(5):
        for y in range(x, 9 - x):
            px[x, y] = colour
    save(img, name)


def focus_ring():
    """Corner brackets drawn just outside a control to show keyboard or gamepad focus."""
    size, arm = 16, 5
    img = canvas(size, size)
    px = img.load()
    for i in range(arm):
        for x, y in ((i, 0), (0, i), (size - 1 - i, 0), (size - 1, i),
                     (i, size - 1), (0, size - 1 - i), (size - 1 - i, size - 1), (size - 1, size - 1 - i)):
            px[x, y] = CREAM
    save(img, "FocusRing")


def divider():
    img = canvas(4, 2)
    px = img.load()
    rows(px, 0, 3, 0, OUTLINE)
    rows(px, 0, 3, 1, WOOD_LO)
    save(img, "Divider")


def main():
    for name in ("Button", "ButtonHover", "ButtonPressed", "ButtonDisabled", "Panel"):
        shutil.copyfile(LEGACY / f"{name}.png", OUT / f"{name}.png")
        print(f"{name}.png  copied from Ui/")

    well("Well", 12)
    well("ToggleBox", 14)
    well("ToggleBoxHover", 14, rim=GOLD_HI)
    slider_fill()
    slider_handle("SliderHandle", WOOD_HI, GOLD)
    slider_handle("SliderHandleHover", GOLD, GOLD_HI)
    toggle_check()
    tab("TabActive", WOOD, GOLD_HI, bottom_open=True)
    tab("TabInactive", WOOD_LO, WOOD, bottom_open=False)
    tab("TabHover", WOOD_HI, GOLD_HI, bottom_open=False)
    arrow("Arrow", CREAM)
    focus_ring()
    divider()


if __name__ == "__main__":
    main()
