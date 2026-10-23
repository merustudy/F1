# -*- coding: utf-8 -*-
"""Round 100: the retreat button's warning for an early retreat (the user's request of 2026-10-23, Design/36 판정 5).

  .venv/bin/python ArtPipeline/Archive/100-early-retreat-warning/mock_round100.py

mock-retreat-warning.png        now and three ways, the whole battle screen at half size
mock-retreat-warning-header.png the header's left at full size, the same four
No paid call: the game's screenshot (game/), shapes and text.
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
FONT = str(ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf")
BASE = Image.open(HERE / "game/ko_05_battle.png").convert("RGB")

BUTTON = (40, 16, 340, 68)          # the retreat button (UiPrefabSetup.Battle: 40,16 — 300x52)
INNER = (50, 22, 330, 62)           # its face, inside the kit frame
RED = (169, 48, 36)                 # the face, sampled from the screenshot
AMBER = (232, 168, 56)              # a warning rim the game's buttons do not use
AMBER_FACE = (150, 92, 20)
TEXT = (244, 236, 226)
DIM = (250, 214, 160)


def font(size):
    return ImageFont.truetype(FONT, size)


def centred(draw, box, text, size, fill, dy=0):
    f = font(size)
    w = draw.textlength(text, font=f)
    x0, y0, x1, y1 = box
    draw.text(((x0 + x1 - w) / 2, (y0 + y1) / 2 - size * 0.62 + dy), text, font=f, fill=fill)


def face(draw, colour):
    draw.rectangle(INNER, fill=colour)


def rim(draw, colour, width=4):
    x0, y0, x1, y1 = BUTTON
    draw.rectangle((x0 - 3, y0 - 3, x1 + 3, y1 + 3), outline=colour, width=width)


def variant(name):
    img = BASE.copy()
    d = ImageDraw.Draw(img, "RGBA")
    if name == "A":
        # The button says it on its face, all through the early floors: two lines, an amber rim.
        face(d, RED)
        centred(d, INNER, "후퇴 (60%)", 22, TEXT, dy=-9)
        centred(d, INNER, "조기 퇴각 · 얻는 것 없음", 15, DIM, dy=12)
        rim(d, AMBER)
    elif name == "B":
        # The first press arms it: the face turns amber for 3 seconds and asks again; the second press tries the retreat.
        face(d, AMBER_FACE)
        centred(d, INNER, "한 번 더: 얻는 것 없이 후퇴", 19, TEXT, dy=-8)
        centred(d, INNER, "3초 안에 다시 누르세요", 14, DIM, dy=12)
        rim(d, AMBER)
    elif name == "C":
        # The press pauses the battle and opens a window over the stage.
        d.rectangle((0, 84, 1920, 1080), fill=(0, 0, 0, 150))
        win = (560, 300, 1360, 660)
        d.rectangle(win, fill=(28, 32, 40), outline=AMBER, width=4)
        centred(d, (win[0], 330, win[2], 390), "지금 후퇴하면 아무것도 얻을 수 없습니다", 34, TEXT)
        for i, line in enumerate(["6층까지의 퇴각은 금·경험치·좋은 특성을 얻지 못합니다.",
                                  "나쁜 특성과 피로는 남고, 시간은 흐르지 않습니다.",
                                  "(전투는 멈춰 있습니다)"]):
            centred(d, (win[0], 420 + i * 40, win[2], 450 + i * 40), line, 24, DIM if i < 2 else (170, 170, 170))
        b1, b2 = (640, 570, 940, 630), (980, 570, 1280, 630)
        d.rectangle(b1, fill=RED, outline=(226, 60, 48), width=3)
        centred(d, b1, "후퇴 시도 (60%)", 26, TEXT)
        d.rectangle(b2, fill=(52, 70, 96), outline=(120, 150, 190), width=3)
        centred(d, b2, "계속 싸우기", 26, TEXT)
    return img


def label(img, title, sub, band=90):
    out = Image.new("RGB", (img.width, img.height + band), (18, 20, 26))
    out.paste(img, (0, band))
    d = ImageDraw.Draw(out)
    d.text((16, 10), title, font=font(30 if band >= 90 else 24), fill=(240, 230, 210))
    d.text((16, 50 if band >= 90 else 40), sub, font=font(20 if band >= 90 else 17), fill=(170, 170, 170))
    return out


VARIANTS = [
    ("지금", "후퇴 (60%) — 1층이어도 아무 표시가 없다", None),
    ("A 단추에 쓰기 (권장)", "1~6층 동안 단추가 두 줄과 호박색 테. 7층부터 지금과 같다. 누르는 법은 그대로", "A"),
    ("B 두 번 누르기", "첫 누름에 호박색으로 바뀌어 3초 동안 다시 묻는다. 둘째 누름이 후퇴를 굴린다", "B"),
    ("C 멈추고 확인 창", "누르면 전투가 멈추고 창이 뜬다. [후퇴 시도]·[계속 싸우기]", "C"),
]


def main():
    tiles = []
    for title, sub, key in VARIANTS:
        img = BASE.copy() if key is None else variant(key)
        tiles.append(label(img.resize((960, 540), Image.LANCZOS), title, sub, band=72))
    w, h = tiles[0].size
    sheet = Image.new("RGB", (w * 2 + 24, h * 2 + 24), (10, 10, 14))
    for i, t in enumerate(tiles):
        sheet.paste(t, ((i % 2) * (w + 24), (i // 2) * (h + 24)))
    sheet.save(HERE / "mock-retreat-warning.png")

    crops = []
    for title, sub, key in VARIANTS[:3]:
        img = BASE.copy() if key is None else variant(key)
        crops.append(label(img.crop((0, 0, 860, 180)), title, sub))
    cw, ch = crops[0].size
    strip = Image.new("RGB", (cw, ch * len(crops) + 16 * (len(crops) - 1)), (10, 10, 14))
    for i, c in enumerate(crops):
        strip.paste(c, (0, i * (ch + 16)))
    strip.save(HERE / "mock-retreat-warning-header.png")


if __name__ == "__main__":
    main()
