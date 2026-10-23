"""Round 99: every Linette candidate in one line, the whole figure above and the face below. No API call.

Run: .venv/bin/python ArtPipeline/Archive/99-linette/review_lineup.py linette_c linette_d ...
"""
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
FONT = ROOT / "Assets" / "@Fonts" / "Source" / "Pretendard" / "Pretendard-Medium.ttf"
OUTPUT = ROOT / "ArtPipeline" / "output" / "character"
FIGURE_H, FACE, PAD, TOP, LABEL = 640, 420, 30, 80, 40
BG, PANEL = (24, 22, 22), (44, 40, 40)
LABELS = {
    "linette_c": "C 잿빛 단발",
    "linette_d": "D 사진 바탕 · 검은 드레스",
    "linette_e": "E 차가운 기록자",
    "linette_f": "F 그림자 첩자",
    "linette_g": "G 연회의 귀부인",
    "linette_h": "H 아이라인 없음",
    "linette_i": "I 구릿빛 적갈색",
    "linette_j": "J 잿빛 은발",
    "linette_k": "K 짙은 와인빛",
    "linette_l": "L 순한 얼굴",
    "linette_m": "M 화장기 없음",
    "linette_n": "N 더 소녀",
    "linette_d_iface": "D + I의 얼굴 (합성)",
    "linette_ingame": "새 인게임 · 대화 그림 기준",
    "linette_ingame2": "새 인게임 2 · 머리를 더 어둡게 (다시 그림)",
    "linette_ingame_dhair": "새 인게임 · 머리를 D의 색으로",
}


def font(size):
    return ImageFont.truetype(str(FONT), size)


def on_panel(image, size):
    panel = Image.new("RGB", size, PANEL)
    ratio = min(size[0] / image.size[0], size[1] / image.size[1])
    image = image.resize((round(image.size[0] * ratio), round(image.size[1] * ratio)), Image.LANCZOS)
    panel.paste(image, ((size[0] - image.size[0]) // 2, (size[1] - image.size[1]) // 2), image)
    return panel


def main():
    args = sys.argv[1:]
    out = "review_lineup.jpg"
    if args and args[0].startswith("--out="):
        out = args.pop(0)[len("--out="):]
    names = args or list(LABELS)
    column = FACE
    width = len(names) * column + (len(names) + 1) * PAD
    height = TOP + LABEL + FIGURE_H + PAD + FACE + PAD
    sheet = Image.new("RGB", (width, height), BG)
    draw = ImageDraw.Draw(sheet)
    draw.text((PAD, 22), "Round 99 · 리넷 후보 (위: 게임 크기의 캔버스, 아래: 얼굴)", font=font(32), fill=(238, 232, 220))
    for index, name in enumerate(names):
        x = PAD + index * (column + PAD)
        draw.text((x, TOP), LABELS.get(name, name), font=font(24), fill=(214, 190, 130))
        figure = Image.open(OUTPUT / f"{name}.png").convert("RGBA")
        sheet.paste(on_panel(figure, (column, FIGURE_H)), (x, TOP + LABEL))
        raw = Image.open(OUTPUT / f"{name}.raw.png").convert("RGBA")
        left, top, right, _ = raw.getbbox()
        span = right - left
        face = raw.crop((left + int(span * 0.2), top, left + int(span * 0.8), top + int(span * 0.6)))
        sheet.paste(on_panel(face, (column, FACE)), (x, TOP + LABEL + FIGURE_H + PAD))
    sheet.save(HERE / out, quality=90)
    print(out, sheet.size)


if __name__ == "__main__":
    main()
