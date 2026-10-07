"""The marker of a shop node and the region coin (round 44, judged "권장안 구현": the mockup's drawn shapes as they are): a stack
of three coins for the node (96x96, twice its 48 on screen inside a node of 74, like node_camp) and one coin (64x64, twice its 32;
shown at 22..30). Drawn, no API call; drawn K times as big and brought down.
  .venv/bin/python ArtPipeline/Archive/44-shop-node/draw_icons.py
"""
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[3]
ICON = ROOT / "Assets/@Art/UI/Icon"
K = 8
INK = (0x18, 0x09, 0x07, 255)
COIN, COIN_DARK, COIN_LIGHT = (0xD4, 0xA2, 0x32, 255), (0x7A, 0x52, 0x16, 255), (0xF6, 0xDC, 0x86, 255)


def drawn(size, draw):
    big = Image.new("RGBA", (size * K, size * K), (0, 0, 0, 0))
    draw(ImageDraw.Draw(big), size * K)
    return big.resize((size, size), Image.LANCZOS)


def node_shop():
    """A stack of three coins seen a little from the side, each with an ink outline (mock_shop.marker_coins)."""
    def draw(d, s):
        w = max(1, int(s * 0.035)); rx, ry = s * 0.33, s * 0.13
        for cy in (0.72, 0.56, 0.40):
            x, y = s * 0.5, s * cy
            d.ellipse([x - rx, y - ry + s * 0.09, x + rx, y + ry + s * 0.09], fill=COIN_DARK, outline=INK, width=w)
            d.ellipse([x - rx, y - ry, x + rx, y + ry], fill=COIN, outline=INK, width=w)
            d.ellipse([x - rx * 0.55, y - ry * 0.5, x + rx * 0.55, y + ry * 0.5], outline=COIN_DARK[:3] + (160,), width=w)
    return drawn(96, draw)


def coin():
    """One coin from the front: a brass disc with a dark edge, an inner ring and a glint (mock_shop.coin)."""
    def draw(d, s):
        d.ellipse([0, 0, s - 1, s - 1], fill=COIN_DARK)
        m = s * 0.09; d.ellipse([m, m, s - 1 - m, s - 1 - m], fill=COIN)
        m2 = s * 0.27; d.ellipse([m2, m2, s - 1 - m2, s - 1 - m2], outline=COIN_DARK[:3] + (190,), width=max(1, int(s * 0.05)))
        d.ellipse([s * 0.20, s * 0.13, s * 0.40, s * 0.27], fill=COIN_LIGHT[:3] + (170,))
    return drawn(64, draw)


def main():
    for name, icon in (("node_shop", node_shop()), ("coin", coin())):
        path = ICON / f"{name}.png"
        icon.save(path)
        print(path.relative_to(ROOT), icon.size, icon.getbbox())


if __name__ == "__main__":
    main()
