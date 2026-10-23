"""Round 99: the hands of dialogue 7, 8-2 and 8, the generated original beside the picture with the redrawn hands
put on it (paste_hands.py). No API call.

Run: .venv/bin/python ArtPipeline/Archive/99-linette/composite/review_hands.py
Writes review_hands.jpg into the round folder.
"""
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROUND = HERE.parent
ROOT = HERE.parents[3]
FONT = ROOT / "Assets" / "@Fonts" / "Source" / "Pretendard" / "Pretendard-Medium.ttf"
OUTPUT = ROOT / "ArtPipeline" / "output" / "character"
ITEMS = [("대화 7", "linette_portrait7"), ("대화 8-2", "linette_portrait8b"), ("대화 8", "linette_portrait8")]
BOX = (400, 640, 720, 1030)
SCALE = 1.5
PAD, TOP, LABEL = 24, 96, 40
BG, WHITE, GOLD = (24, 22, 22), (238, 232, 220), (214, 190, 130)


def font(size):
    return ImageFont.truetype(str(FONT), size)


def crop(path):
    image = Image.open(path).convert("RGBA")
    flat = Image.new("RGBA", image.size, (60, 56, 56, 255))
    flat.alpha_composite(image)
    part = flat.crop(BOX).convert("RGB")
    return part.resize((round(part.size[0] * SCALE), round(part.size[1] * SCALE)), Image.LANCZOS)


def main():
    cell_w = round((BOX[2] - BOX[0]) * SCALE)
    cell_h = round((BOX[3] - BOX[1]) * SCALE)
    width = PAD + len(ITEMS) * (2 * cell_w + PAD) + PAD
    height = TOP + LABEL + cell_h + PAD
    board = Image.new("RGB", (width, height), BG)
    draw = ImageDraw.Draw(board)
    draw.text((PAD, 28), "Round 99 · 대화 7·8-2·8의 손: 원본(왼쪽)과 손만 다시 그려 붙인 것(오른쪽)", font=font(34), fill=WHITE)
    x = PAD
    for label, name in ITEMS:
        draw.text((x, TOP), f"{label} · 전", font=font(26), fill=(150, 140, 130))
        board.paste(crop(ROUND / "candidates" / f"{name}_ink.raw.png"), (x, TOP + LABEL))
        x += cell_w
        draw.text((x + 8, TOP), "뒤", font=font(26), fill=GOLD)
        board.paste(crop(OUTPUT / f"{name}.raw.png"), (x, TOP + LABEL))
        x += cell_w + PAD
    board.save(ROUND / "review_hands.jpg", quality=88)
    print(f"review_hands.jpg: {board.size[0]}x{board.size[1]}")


if __name__ == "__main__":
    main()
