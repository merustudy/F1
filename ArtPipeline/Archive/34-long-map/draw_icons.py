"""The markers of the new map nodes (round 34, judged "3. 권장은": the mockup's drawn shapes as they are): an elite (the
battle's crossed swords on a red diamond) and a camp (two crossed logs and a flame). Drawn, no API call. Twice their size on
screen (48, inside a node of 74), like node_battle and node_boss; drawn K times and brought down.
  .venv/bin/python ArtPipeline/Archive/34-long-map/draw_icons.py
"""
import math
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[3]
ICON = ROOT / "Assets/@Art/UI/Icon"
S, K = 96, 8
ELITE = (0xC8, 0x4B, 0x4B, 255)                                            # the mockup's colours (mock_long_map.py)
FIRE, FIRE_IN, WOOD = (0xF3, 0x9C, 0x12, 255), (0xFF, 0xD8, 0x6A, 255), (0x7A, 0x4E, 0x2C, 255)


def drawn(draw):
    big = Image.new("RGBA", (S * K, S * K), (0, 0, 0, 0))
    draw(ImageDraw.Draw(big), S * K)
    return big.resize((S, S), Image.LANCZOS)


def elite():
    def draw(d, s):
        d.polygon([(s / 2, 0), (s - 1, s / 2), (s / 2, s - 1), (0, s / 2)], fill=ELITE)
    icon = drawn(draw)
    swords = Image.open(ICON / "node_battle.png").convert("RGBA")
    small = round(S * 0.8)
    icon.alpha_composite(swords.resize((small, small), Image.LANCZOS), ((S - small) // 2, (S - small) // 2))
    return icon


def camp():
    def draw(d, s):
        for a in (math.radians(20), math.radians(160)):
            cx, cy = s * 0.5, s * 0.78
            dx, dy = math.cos(a) * s * 0.36, math.sin(a) * s * 0.10
            d.line([cx - dx, cy + dy, cx + dx, cy - dy], fill=WOOD, width=int(s * 0.11))
        d.polygon([(s * 0.5, s * 0.12), (s * 0.70, s * 0.45), (s * 0.66, s * 0.68), (s * 0.5, s * 0.74), (s * 0.34, s * 0.68), (s * 0.30, s * 0.45)], fill=FIRE)
        d.polygon([(s * 0.5, s * 0.36), (s * 0.60, s * 0.55), (s * 0.5, s * 0.70), (s * 0.40, s * 0.55)], fill=FIRE_IN)
    return drawn(draw)


def main():
    for name, icon in (("node_elite", elite()), ("node_camp", camp())):
        path = ICON / f"{name}.png"
        icon.save(path)
        print(path.relative_to(ROOT), icon.size, icon.getbbox())


if __name__ == "__main__":
    main()
