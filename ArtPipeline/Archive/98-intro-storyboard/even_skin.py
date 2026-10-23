"""Round 98 02판 #8: take the red out of Linette's forearms, wrists and hands. No API call.

The user: "#8 리넷 손목과 손이 다른 곳에 비해 빨간 느낌인데 확인", then "팔뚝과 손목이 어깨색이 아직도 붉어 다른
피부색과 통일 시켜줘". Her skin's G/R and B/R (medians of the lit skin) are 0.72-0.74 and 0.58-0.60 on the shoulders,
chest, face and thighs, but 0.63 and 0.50 on the forearms. Only the colour moves, never a shape: inside FIGURE (her
body below the chin, softened at the edge), a lit skin pixel (red over green over blue) redder than GREEN_RATIO or
BLUE_RATIO has its green and blue raised to them, then the whole pixel is scaled back to its grey level. The dress
and the chains are grey and the wood is darker, so they stay.

Run: .venv/bin/python ArtPipeline/Archive/98-intro-storyboard/even_skin.py
Reads ArtPipeline/output/background/b08_market_v02_try1.raw.png (the generated scene, kept the first time) and writes
b08_market_v02.raw.png; refit it with gen_image.py --refit afterwards.
"""
import shutil
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
SCENES = ROOT / "ArtPipeline" / "output" / "background"
SOURCE, OUT = "b08_market_v02_try1.raw.png", "b08_market_v02.raw.png"
# Linette from below the chin to the knees, arms and hands included.
FIGURE = [(1300, 400), (1700, 400), (1720, 520), (1710, 700), (1660, 900), (1640, 1000), (1630, 1110), (1680, 1300),
          (1640, 1460), (1330, 1460), (1290, 1250), (1300, 1000), (1300, 700), (1285, 520)]
GREEN_RATIO, BLUE_RATIO = 0.73, 0.59  # the shoulders, chest, face and thighs
LIT, RED = 100, 110
FEATHER = 12


def grey(p):
    return (p[0] + p[1] + p[2]) / 3


def even(p):
    r, g, b = p
    if r < RED or grey(p) < LIT or not r > g > b:
        return p
    new_g, new_b = max(g, r * GREEN_RATIO), max(b, r * BLUE_RATIO)
    if (new_g, new_b) == (g, b):
        return p
    scale = grey(p) / ((r + new_g + new_b) / 3)
    return tuple(max(0, min(255, round(c * scale))) for c in (r, new_g, new_b))


def main():
    source = SCENES / SOURCE
    if not source.exists():
        shutil.copy2(SCENES / OUT, source)
    image = Image.open(source).convert("RGB")
    evened = image.copy()
    px = evened.load()
    left, top = min(x for x, _ in FIGURE), min(y for _, y in FIGURE)
    right, bottom = max(x for x, _ in FIGURE), max(y for _, y in FIGURE)
    for y in range(top, bottom):
        for x in range(left, right):
            px[x, y] = even(px[x, y])
    mask = Image.new("L", image.size, 0)
    ImageDraw.Draw(mask).polygon(FIGURE, fill=255)
    mask = mask.filter(ImageFilter.GaussianBlur(FEATHER / 2))
    Image.composite(evened, image, mask).save(SCENES / OUT)
    print(f"{OUT}: skin redder than G/R {GREEN_RATIO}, B/R {BLUE_RATIO} evened inside the figure")


if __name__ == "__main__":
    main()
