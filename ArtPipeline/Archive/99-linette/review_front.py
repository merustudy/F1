"""Round 99: the front-view candidates beside the AI portrait they follow and D. No API call.

Run: .venv/bin/python ArtPipeline/Archive/99-linette/review_front.py [front|portrait]
"""
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
FONT = ROOT / "Assets" / "@Fonts" / "Source" / "Pretendard" / "Pretendard-Medium.ttf"
OUTPUT = ROOT / "ArtPipeline" / "output" / "character"
H, PAD, TOP = 820, 30, 110
BG, PANEL = (24, 22, 22), (44, 40, 40)
SETS = {
    "front": ("Round 99 · 리넷 정면 일러스트 (D 기준, 올린 AI 인물에 가깝게)", "review_front.jpg", [
        ("올린 AI 인물 사진", HERE / "refs" / "linette_front_face_ai.jpg"),
        ("D (지금, 옆을 봄)", OUTPUT / "linette_d.raw.png"),
        ("정면 1 · 우리 얼굴 기준", OUTPUT / "linette_front1.raw.png"),
        ("정면 2 · 사진에 가깝게", OUTPUT / "linette_front2.raw.png"),
        ("정면 3 · 사진에 가장 가깝게", OUTPUT / "linette_front3.raw.png"),
    ]),
    "portrait": ("Round 99 · 리넷 대화용 정면 일러스트 (사진 둘을 바탕으로)", "review_portrait.jpg", [
        ("얼굴 사진", HERE / "refs" / "portrait_face_ai.jpg"),
        ("옷 사진", HERE / "refs" / "portrait_outfit_ai.jpg"),
        ("대화 1 · 우리 얼굴 기준", OUTPUT / "linette_portrait1.raw.png"),
        ("대화 2 · 사진에 가깝게", OUTPUT / "linette_portrait2.raw.png"),
        ("대화 3 · 사진에 가깝게 + 손 동작", OUTPUT / "linette_portrait3.raw.png"),
    ]),
    "portrait2": ("Round 99 · 리넷 대화용 일러스트 (올린 그림을 바탕으로)", "review_portrait2.jpg", [
        ("올린 그림", HERE / "refs" / "portrait_ref_illust.jpg"),
        ("대화 2 (지난 권장)", OUTPUT / "linette_portrait2.raw.png"),
        ("대화 4 · 우리 그림체, 웃음", OUTPUT / "linette_portrait4.raw.png"),
        ("대화 5 · 우리 그림체, 옅은 미소", OUTPUT / "linette_portrait5.raw.png"),
        ("대화 6 · 올린 그림의 결", OUTPUT / "linette_portrait6.raw.png"),
    ]),
    "portrait7": ("Round 99 · 대화 7: 대화 1의 얼굴 + 대화 6의 결 + D의 복장", "review_portrait7.jpg", [
        ("대화 1 (얼굴)", OUTPUT / "linette_portrait1.raw.png"),
        ("대화 6 (결)", OUTPUT / "linette_portrait6.raw.png"),
        ("D (복장)", OUTPUT / "linette_d.raw.png"),
        ("대화 7", OUTPUT / "linette_portrait7.raw.png"),
    ]),
    "portrait89": ("Round 99 · 대화 8(대화 7 + 대화 6의 표정) · 대화 9(대화 6을 대화 7의 결로)", "review_portrait89.jpg", [
        ("대화 7", OUTPUT / "linette_portrait7.raw.png"),
        ("대화 8 · 눈웃음, 입을 살짝", OUTPUT / "linette_portrait8.raw.png"),
        ("대화 6", OUTPUT / "linette_portrait6.raw.png"),
        ("대화 9 · 대화 7의 결", OUTPUT / "linette_portrait9.raw.png"),
    ]),
    "portrait10": ("Round 99 · 대화 10: 대화 9의 얼굴 + 대화 7의 머리색과 복장", "review_portrait10.jpg", [
        ("대화 9", OUTPUT / "linette_portrait9.raw.png"),
        ("대화 7", OUTPUT / "linette_portrait7.raw.png"),
        ("대화 10", OUTPUT / "linette_portrait10.raw.png"),
        ("대화 8 (참고: 7의 웃는 표정)", OUTPUT / "linette_portrait8.raw.png"),
    ]),
    "portrait8b": ("Round 99 · 대화 8-2: 대화 7과 8 사이의 표정", "review_portrait8b.jpg", [
        ("대화 7", OUTPUT / "linette_portrait7.raw.png"),
        ("대화 8-2", OUTPUT / "linette_portrait8b.raw.png"),
        ("대화 8", OUTPUT / "linette_portrait8.raw.png"),
    ]),
    "hands": ("Round 99 · 손만 다시 그린 생성 결과 (여기서 손만 잘라 원본에 붙였다)", "review_hands_redraw.jpg", [
        ("대화 7 · 손 다시", OUTPUT / "linette_portrait7h.raw.png"),
        ("대화 8-2 · 손 다시", OUTPUT / "linette_portrait8bh.raw.png"),
        ("대화 8 · 손 다시", OUTPUT / "linette_portrait8h.raw.png"),
    ]),
}
ITEMS = [
    ("올린 AI 인물 사진", HERE / "refs" / "linette_front_face_ai.jpg"),
    ("D (지금, 옆을 봄)", OUTPUT / "linette_d.raw.png"),
    ("정면 1 · 우리 얼굴 기준", OUTPUT / "linette_front1.raw.png"),
    ("정면 2 · 사진에 가깝게", OUTPUT / "linette_front2.raw.png"),
    ("정면 3 · 사진에 가장 가깝게", OUTPUT / "linette_front3.raw.png"),
]


def font(size):
    return ImageFont.truetype(str(FONT), size)


def panel(path):
    image = Image.open(path)
    if image.mode == "RGBA":
        box = image.getbbox()
        image = image.crop(box)
    ratio = H / image.size[1]
    image = image.resize((round(image.size[0] * ratio), H), Image.LANCZOS)
    board = Image.new("RGB", image.size, PANEL)
    board.paste(image, (0, 0), image if image.mode == "RGBA" else None)
    return board


def main():
    import sys
    title, out, items = SETS[sys.argv[1] if len(sys.argv) > 1 else "front"]
    panels = [(label, panel(path)) for label, path in items]
    width = sum(p.size[0] for _, p in panels) + PAD * (len(panels) + 1)
    sheet = Image.new("RGB", (width, TOP + H + PAD), BG)
    draw = ImageDraw.Draw(sheet)
    draw.text((PAD, 22), title, font=font(32), fill=(238, 232, 220))
    x = PAD
    for label, board in panels:
        draw.text((x, TOP - 40), label, font=font(24), fill=(214, 190, 130))
        sheet.paste(board, (x, TOP))
        x += board.size[0] + PAD
    sheet.save(HERE / out, quality=90)
    print(out, sheet.size)


if __name__ == "__main__":
    main()
