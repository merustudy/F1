# -*- coding: utf-8 -*-
"""Make the gold glow behind a unique item's icon (stage 23, round 70 follow-up 2: "금테는 삭제, 빛을 넣는데 외곽선이 … / 2px로 확정. 숨쉬기 제거").

For every unique item of ItemData.csv that has an icon, this writes `<icon>_glow.png` beside the icon in Assets/@Art/Item: the icon's
alpha widened by 2 screen pixels (in icon pixels: 2 divided by the scale the icon is shown at in its piece, which fits the icon into
the piece's art box; the gladius is shown at 0.435, so 5) and blurred by 0.6 of that, in the unique gold with a lighter core along
the edge, baked (the screen tints no colour into it: two colours cannot be one tint). The data names the glow through
ItemData.GlowIcon (Icon + "-glow": item/gladius-glow), the screen lays it behind the icon at the icon's scale (GridGeometry.LayGlow). It calls nothing
and running it again writes the same bytes; run it again after an icon is redrawn or trimmed.

  .venv/bin/python ArtPipeline/tools/glow_items.py            # writes the glows that differ
  .venv/bin/python ArtPipeline/tools/glow_items.py --check    # only reports which are missing or stale
"""
import argparse
import csv
import io
import sys
from pathlib import Path

from PIL import Image, ImageFilter

ROOT = Path(__file__).resolve().parents[2]
ITEMS = ROOT / "Assets/@Art/Item"
ITEM_DATA = ROOT / "Assets/@Data/Source/ItemData.csv"

SQUARE, GAP, ART_MARGIN = 50, 2, 5                 # GridGeometry.Square, Gap, ItemSlotView.ArtMargin
WIDTH_PX = 2                                       # the user's "2px로 확정"
BLUR_OF_SPREAD = 0.6
GOLD = (0xC7, 0xB3, 0x77)                          # UiPalette.Unique (Diablo's gold)
LIGHT = (0xF2, 0xE2, 0xA6)                         # the lighter core along the edge
LIGHT_GAIN, LIGHT_ALPHA = 1.6, 0.55


def art_box(width, height):
    """The icon's box in a piece of width x height squares (GridGeometry.ArtBox for a piece that is not turned)."""
    return width * SQUARE + (width - 1) * GAP - 2 * ART_MARGIN, height * SQUARE + (height - 1) * GAP - 2 * ART_MARGIN


def glow(icon, width, height):
    """The glow of `icon` (RGBA) shown in a piece of width x height squares: widened 2 screen px, blurred, gold with a light core."""
    box_w, box_h = art_box(width, height)
    scale = min(box_w / icon.width, box_h / icon.height)
    spread = max(1, int(round(WIDTH_PX / scale)))
    blur = max(1, int(round(spread * BLUR_OF_SPREAD)))
    pad = spread + blur * 3
    alpha = Image.new("L", (icon.width + 2 * pad, icon.height + 2 * pad), 0)
    alpha.paste(icon.getchannel("A"), (pad, pad))
    alpha = alpha.filter(ImageFilter.MaxFilter(2 * spread + 1)).filter(ImageFilter.GaussianBlur(blur))
    out = Image.new("RGBA", alpha.size, GOLD + (0,))
    out.putalpha(alpha)
    core = Image.new("RGBA", alpha.size, LIGHT + (0,))
    core.putalpha(alpha.point(lambda v: int(min(255, v * LIGHT_GAIN) * LIGHT_ALPHA)))
    out.alpha_composite(core)
    return out, (spread, blur)


def png_bytes(image):
    buffer = io.BytesIO()
    image.save(buffer, "PNG")
    return buffer.getvalue()


def uniques():
    """(id, icon file name, width, height) of every unique item that has an icon, in the CSV's order."""
    with open(ITEM_DATA, newline="", encoding="utf-8") as f:
        for row in csv.DictReader(f):
            if row["Unique"].strip().lower() == "true" and row["Icon"].strip():
                address = row["Icon"].strip()                      # "item/crude-bow" -> crude_bow.png (ArtSetup.AssetPath)
                yield row["Id"], address.rsplit("/", 1)[-1].replace("-", "_"), int(row["Width"]), int(row["Height"])


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--check", action="store_true", help="report without writing")
    args = parser.parse_args()
    stale = 0
    for item_id, name, width, height in uniques():
        source = ITEMS / (name + ".png")
        target = ITEMS / (name + "_glow.png")
        if not source.exists():
            print(f"{item_id}: no icon at {source}"); stale += 1; continue
        made, (spread, blur) = glow(Image.open(source).convert("RGBA"), width, height)
        data = png_bytes(made)
        same = target.exists() and target.read_bytes() == data
        state = "same" if same else ("missing" if not target.exists() else "stale")
        print(f"{item_id}: {target.name} {made.width}x{made.height} (spread {spread}, blur {blur} icon px) — {state}")
        if same:
            continue
        stale += 1
        if not args.check:
            target.write_bytes(data)
            print(f"  wrote {target}")
    if args.check and stale:
        sys.exit(1)


if __name__ == "__main__":
    main()
