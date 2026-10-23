# -*- coding: utf-8 -*-
"""Round 97: the test gold button in the lobby (the user's request of 2026-10-23) — where it stands.

  .venv/bin/python ArtPipeline/Archive/97-test-gold/mock_round97.py

mock-gold-button.png  now, and three places: A beside the gold in the header, B in the bottom strip left of [원정 화면으로], C the town's top left
No paid call: the game's screenshot (game/), shapes and text.
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
FONT = str(ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf")
BASE = Image.open(HERE / "game/ko_12_lobby_after.png").convert("RGB")

TEST_FILL = (74, 32, 64)      # a colour the game's own buttons never use, so it reads as a tool
TEST_RIM = (232, 92, 200)
TEXT = (245, 236, 244)


def font(size):
    return ImageFont.truetype(FONT, size)


def button(draw, box, label, size=26):
    draw.rectangle(box, fill=TEST_FILL, outline=TEST_RIM, width=3)
    f = font(size)
    w = draw.textlength(label, font=f)
    x0, y0, x1, y1 = box
    draw.text(((x0 + x1 - w) / 2, (y0 + y1) / 2 - size * 0.62), label, font=f, fill=TEXT)


def variant(name):
    img = BASE.copy()
    d = ImageDraw.Draw(img)
    if name == "A":
        # The gold reads "금 57" at about x 1545..1620; the button goes left of it, the gold keeps its place.
        button(d, (1290, 26, 1466, 74), "금 +100 (테스트)", 22)
    elif name == "B":
        button(d, (1330, 1004, 1540, 1066), "금 +100 (테스트)", 24)
    elif name == "C":
        button(d, (24, 118, 234, 170), "금 +100 (테스트)", 24)
    return img


def label(img, text, sub):
    out = Image.new("RGB", (img.width, img.height + 90), (18, 20, 26))
    out.paste(img, (0, 90))
    d = ImageDraw.Draw(out)
    d.text((24, 14), text, font=font(40), fill=(240, 240, 240))
    d.text((24, 58), sub, font=font(24), fill=(170, 176, 190))
    return out


def main():
    panels = [
        label(BASE, "지금", "버튼 없음"),
        label(variant("A"), "A (권장) 머리줄, 금 옆", "누르면 바로 옆의 '금 N'이 오른다. 에디터와 개발 빌드에만"),
        label(variant("B"), "B 아래 띠, [원정 화면으로] 왼쪽", "명령 단추들과 한 줄. 손이 원정 단추 가까이 간다"),
        label(variant("C"), "C 마을 그림 왼쪽 위", "그림 위에 뜬다. 건물·이름표와 겹치지 않는 자리"),
    ]
    scale = 0.5
    w, h = int(panels[0].width * scale), int(panels[0].height * scale)
    sheet = Image.new("RGB", (w * 2 + 30, h * 2 + 30), (10, 11, 14))
    for i, p in enumerate(panels):
        sheet.paste(p.resize((w, h), Image.LANCZOS), ((i % 2) * (w + 30), (i // 2) * (h + 30)))
    sheet.save(HERE / "mock-gold-button.png")
    # The header at full size, now and A, to read the text.
    crop = (1200, 0, 1920, 100)
    zoom = Image.new("RGB", (720, 230), (10, 11, 14))
    zoom.paste(BASE.crop(crop), (0, 0))
    zoom.paste(variant("A").crop(crop), (0, 120))
    zoom.save(HERE / "mock-gold-button-header.png")


if __name__ == "__main__":
    main()
