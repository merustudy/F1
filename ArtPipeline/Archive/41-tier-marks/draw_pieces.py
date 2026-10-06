"""The drawn pieces of the tier marks (round 41), twice their size on screen, no API call:
  Frame/tier_tag.png  the tier tag at an item cell's bottom-left corner: an ink pill with a white rim (the cell tints the
                      rim with the tier's colour; the ink stays ink under any tint). Round ends, 64x40, Border 20 so that it
                      stretches sideways only (like fatigue_tag).
  Icon/star.png       a star of the tag: white with an ink outline, 24x24 (12 on screen), tinted with the tier's word colour.
  .venv/bin/python ArtPipeline/Archive/41-tier-marks/draw_pieces.py
"""
import math
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[3]
FRAME = ROOT / "Assets/@Art/UI/Frame"
ICON = ROOT / "Assets/@Art/UI/Icon"
INK = (0x18, 0x09, 0x07)
AA = 8


def tier_tag(w=64, h=40, rim=3):
    big = Image.new("RGBA", (w * AA, h * AA), (0, 0, 0, 0)); d = ImageDraw.Draw(big)
    d.rounded_rectangle([0, 0, w * AA - 1, h * AA - 1], radius=h * AA // 2, fill=(255, 255, 255, 255))
    d.rounded_rectangle([rim * AA, rim * AA, (w - rim) * AA - 1, (h - rim) * AA - 1], radius=(h // 2 - rim) * AA, fill=INK + (235,))
    return big.resize((w, h), Image.LANCZOS)


def star(size=24, outline=2):
    big = Image.new("RGBA", (size * AA, size * AA), (0, 0, 0, 0)); d = ImageDraw.Draw(big)
    cx = cy = size * AA / 2

    def points(r_outer, r_inner):
        pts = []
        for n in range(10):
            a = -math.pi / 2 + n * math.pi / 5
            r = r_outer if n % 2 == 0 else r_inner
            pts.append((cx + math.cos(a) * r, cy + math.sin(a) * r))
        return pts

    r = (size / 2 - 0.5) * AA
    d.polygon(points(r, r * 0.46), fill=INK + (255,))
    d.polygon(points(r - outline * AA, (r - outline * AA) * 0.42), fill=(255, 255, 255, 255))
    return big.resize((size, size), Image.LANCZOS)


def main():
    tier_tag().save(FRAME / "tier_tag.png"); print("drawn", FRAME / "tier_tag.png")
    star().save(ICON / "star.png"); print("drawn", ICON / "star.png")


if __name__ == "__main__":
    main()
