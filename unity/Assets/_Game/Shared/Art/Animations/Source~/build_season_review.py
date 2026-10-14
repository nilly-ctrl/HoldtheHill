"""Contact sheets of every seasonal sprite beside its original, for looking through.

    python build_season_review.py     -> SeasonReviewTowers.png, SeasonReviewProps.png

Each cell shows the first frame as summer, autumn, winter and night (where that version exists),
each on a ground colour to match.
"""
import glob
import json
import os

from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
SHEETS = os.path.normpath(os.path.join(HERE, "..", "Sheets"))
SEASONS = ["", "Autumn", "Winter", "Night"]
GROUND = {"": (79, 111, 52), "Autumn": (120, 96, 50), "Winter": (224, 234, 242), "Night": (22, 30, 52)}


def first_frame(path):
    with open(path[:-4] + ".json", encoding="utf-8") as f:
        r = json.load(f)["frames"][0]["frame"]
    return Image.open(path).convert("RGBA").crop((r["x"], r["y"], r["x"] + r["w"], r["y"] + r["h"]))


def review(group, out_name, cols, scale=2):
    names = {os.path.basename(p)[:-4]: p for p in glob.glob(os.path.join(SHEETS, group, "*.png"))}
    bases = sorted(n for n in names if any(n + s in names for s in SEASONS[1:]) and not any(n.endswith(s) for s in SEASONS[1:]))
    font = ImageFont.load_default()
    cell_w = max(first_frame(names[b]).width for b in bases) * scale + 4
    cell_h = max(first_frame(names[b]).height for b in bases) * scale + 16
    block_w = cell_w * 4 + 10
    rows = (len(bases) + cols - 1) // cols
    sheet = Image.new("RGBA", (block_w * cols, cell_h * rows), (42, 29, 20, 255))
    d = ImageDraw.Draw(sheet)
    for i, b in enumerate(bases):
        ox, oy = (i % cols) * block_w, (i // cols) * cell_h
        d.text((ox + 2, oy + 1), b, fill=(255, 241, 214, 255), font=font)
        for j, season in enumerate(SEASONS):
            if b + season not in names:
                continue
            im = first_frame(names[b + season])
            big = im.resize((im.width * scale, im.height * scale), Image.NEAREST)
            d.rectangle([ox + j * cell_w, oy + 13, ox + j * cell_w + cell_w - 2, oy + cell_h - 2], fill=GROUND[season])
            sheet.alpha_composite(big, (ox + j * cell_w + (cell_w - big.width) // 2, oy + 13 + (cell_h - 15 - big.height) // 2))
    sheet.save(os.path.join(HERE, out_name))
    print(f"{out_name}: {len(bases)} sprites, {sheet.size}")


if __name__ == "__main__":
    review("Towers", "SeasonReviewTowers.png", cols=3)
    review("Props", "SeasonReviewProps.png", cols=3)
