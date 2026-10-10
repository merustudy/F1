# -*- coding: utf-8 -*-
"""Round 51 / stage 20: the whetstone's stand-in icon, a drawn shape (no API call): a grey whetstone bar lying a little aslant, an
ink outline, a darker underside and two pale streaks, trimmed to what is drawn (tools/trim_items.py). The approved mockup
(mock-stars-bb.png) shows the same shape. A picture is drawn when it is asked for.
  .venv/bin/python ArtPipeline/Archive/51-claw-axe-whetstone/whetstone_shape.py
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / "ArtPipeline/tools"))
import trim_items  # noqa: E402

OUT = ROOT / "Assets/@Art/Item/whetstone.png"
INK = (24, 22, 20, 255)


def main():
    bar = Image.new("RGBA", (400, 192), (0, 0, 0, 0)); b = ImageDraw.Draw(bar)
    b.rounded_rectangle([8, 8, 392, 184], 52, fill=(128, 134, 140, 255))
    b.rounded_rectangle([28, 104, 372, 168], 32, fill=(98, 104, 112, 255))
    b.line([(80, 60), (220, 48)], fill=(196, 202, 206, 255), width=12)
    b.line([(140, 80), (320, 68)], fill=(196, 202, 206, 255), width=12)
    b.rounded_rectangle([8, 8, 392, 184], 52, outline=INK, width=14)
    bar = bar.rotate(28, expand=True, resample=Image.BICUBIC)
    # About twice its size on a one-square piece, like the other icons (Architecture/13 "후처리 (`cell`)").
    bar = bar.resize((round(bar.width * 0.3), round(bar.height * 0.3)), Image.LANCZOS)
    icon = trim_items.trimmed(bar)
    icon.save(OUT); print(OUT.relative_to(ROOT), icon.size)


if __name__ == "__main__":
    main()
