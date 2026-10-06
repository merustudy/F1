"""The tier rim of an item cell (round 35, A): a white band with a dark hairline inside it, drawn twice its size on screen
(band 4, hairline 1) with square-ish corners like the slot's. White, so the cell tints it with the tier's colour; the hairline
is black at half alpha and stays dark under any tint. A nine-slice frame (UiArt.TierRim, border 12) that stretches to any cell.
Drawn, no API call.
  .venv/bin/python ArtPipeline/Archive/35-tiers/draw_rim.py
"""
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / "Assets/@Art/UI/Frame/tier_rim.png"

S, BAND, LINE, RADIUS, K = 40, 8, 2, 4, 8          # 2x of the screen: band 4, hairline 1, corner 2


def main():
    big = Image.new("RGBA", (S * K, S * K), (0, 0, 0, 0))
    d = ImageDraw.Draw(big)
    d.rounded_rectangle([0, 0, S * K - 1, S * K - 1], radius=RADIUS * K, fill=(255, 255, 255, 255))
    inner = BAND * K
    d.rounded_rectangle([inner, inner, S * K - 1 - inner, S * K - 1 - inner], radius=max(1, (RADIUS - BAND // 2) * K), fill=(0, 0, 0, 128))
    hole = (BAND + LINE) * K
    d.rounded_rectangle([hole, hole, S * K - 1 - hole, S * K - 1 - hole], radius=1, fill=(0, 0, 0, 0))
    big.resize((S, S), Image.LANCZOS).save(OUT)
    print(OUT.relative_to(ROOT), (S, S))


if __name__ == "__main__":
    main()
