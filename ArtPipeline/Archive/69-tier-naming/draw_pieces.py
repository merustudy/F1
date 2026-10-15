"""The drawn piece of the tier mark (round 69), twice its size on screen, no API call:
  Icon/tier_pip.png  a pip of the tier tag: a white diamond with an ink outline, 24x24 (12 on screen — the star's size, as the user
                     asked: "외곽선 및 도형의 크기는 현재 별과 동일하게"), tinted with the tier's colour.
  .venv/bin/python ArtPipeline/Archive/69-tier-naming/draw_pieces.py
"""
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[3]
ICON = ROOT / "Assets/@Art/UI/Icon"
INK = (0x18, 0x09, 0x07)
AA = 8


def pip(size=24, outline=2):
    big = Image.new("RGBA", (size * AA, size * AA), (0, 0, 0, 0)); d = ImageDraw.Draw(big)
    c = size * AA / 2
    r = (size / 2 - 0.5) * AA
    d.polygon([(c, c - r), (c + r, c), (c, c + r), (c - r, c)], fill=INK + (255,))
    r2 = r - outline * AA * 1.35                                   # a diamond's edge is diagonal: the same screen thickness needs a little more
    d.polygon([(c, c - r2), (c + r2, c), (c, c + r2), (c - r2, c)], fill=(255, 255, 255, 255))
    return big.resize((size, size), Image.LANCZOS)


def main():
    pip().save(ICON / "tier_pip.png"); print("drawn", ICON / "tier_pip.png")


if __name__ == "__main__":
    main()
