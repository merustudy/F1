# -*- coding: utf-8 -*-
"""Trim the item icons to what is drawn (round 50).

The twenty-one icons were fitted to the canvases of the old strip cells (328x80, 328x188, 328x296: Architecture/13 "후처리 (cell)"),
so most of a canvas is empty on the left and right. A grid piece fits the whole sprite into its box, so a buckler (80 wide of 328)
showed as a dot on 1x1 and the two-wide weapons a fifth smaller than the mockups, which fit what is drawn. This cuts every icon
to the box of its drawn pixels (its ink band included), padded evenly to a multiple of 4 (block compression). It calls nothing
and changes no pixel that is drawn; running it again changes nothing.

  .venv/bin/python ArtPipeline/tools/trim_items.py            # trims Assets/@Art/Item/*.png in place
  .venv/bin/python ArtPipeline/tools/trim_items.py --check    # only reports
"""
import argparse
import sys
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
ITEMS = ROOT / "Assets/@Art/Item"


def trimmed(image):
    box = image.getchannel("A").getbbox()
    if box is None:
        raise ValueError("nothing is drawn")
    cut = image.crop(box)
    w, h = (cut.width + 3) // 4 * 4, (cut.height + 3) // 4 * 4
    out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    out.paste(cut, ((w - cut.width) // 2, (h - cut.height) // 2))
    return out


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--check", action="store_true", help="report the sizes without writing")
    args = parser.parse_args()
    changed = 0
    for path in sorted(ITEMS.glob("*.png")):
        image = Image.open(path).convert("RGBA")
        out = trimmed(image)
        if out.size == image.size:
            print(f"{path.name:20s} {image.width}x{image.height} (already)")
            continue
        changed += 1
        print(f"{path.name:20s} {image.width}x{image.height} -> {out.width}x{out.height}")
        if not args.check:
            out.save(path)
    print(f"{changed} to trim" if args.check else f"{changed} trimmed")
    return 0


if __name__ == "__main__":
    sys.exit(main())
