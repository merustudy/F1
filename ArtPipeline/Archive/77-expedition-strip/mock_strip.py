# -*- coding: utf-8 -*-
"""Round 77: the expedition screen without the party strip (the user's 2026-10-17 question "원정 하단에 로스터는 굳이 안보여줘도 될 것 같아 …
따로 용병 리스트 버튼을 눌러 제공하는 방식으로 할까? 검토"). Mockups over the screen as it is (base-expedition.png = ko_56_expedition after
Round 76): the strip gone, the map whole, the details panel reaching the bottom with the status and the buttons. Drawn shapes and text,
the map is the shipped painting. No API call.
  .venv/bin/python ArtPipeline/Archive/77-expedition-strip/mock_strip.py
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
FONT = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"
MAP = ROOT / "Assets/@Art/Background/Expedition/map.png"
TEXT = (0xEB, 0xEB, 0xE6); DIM = (0x9A, 0xA0, 0xAC); BRASS = (0xB8, 0x94, 0x4E); BONE = (0xCD, 0xC0, 0xA0)
PANEL = (0x1E, 0x22, 0x2B); PANEL_LIGHT = (0x2A, 0x30, 0x3C); SLOT = (0x15, 0x18, 0x1F); LINE = (0x55, 0x5C, 0x6B)
BUTTON = (0x3B, 0x6F, 0xB5); QUIET = (0x3A, 0x41, 0x50); GOOD = (0x58, 0xB3, 0x68); BACKGROUND = (0x12, 0x14, 0x1A)


def font(px): return ImageFont.truetype(str(FONT), int(round(px)))


def label(d, xy, s, px, color=TEXT, anchor="la"):
    d.text(xy, s, font=font(px), fill=color, anchor=anchor)


def button(d, box, s, color=QUIET, px=22, text=TEXT):
    d.rectangle(box, fill=color)
    label(d, ((box[0] + box[2]) / 2, (box[1] + box[3]) / 2), s, px, text, "mm")


def without_strip():
    """The screen with the strip gone: the painting whole under the header, the details panel to the bottom."""
    img = Image.open(HERE / "base-expedition.png").convert("RGBA")
    painting = Image.open(MAP).convert("RGBA").resize((1620, 1080), Image.LANCZOS)
    # The painting back where the strip and the panel's shade were (the screen draws the painting first, then the panels).
    img.paste(painting.crop((0, 770, 1620, 1080)), (0, 770))
    ImageDraw.Draw(img).rectangle((1620, 770, 1920, 1080), fill=BACKGROUND)
    # The details panel grows to the bottom (the same 90% panel), its upper part as it is.
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    ImageDraw.Draw(layer).rectangle((1320, 760, 1880, 1040), fill=PANEL + (230,))
    img = Image.alpha_composite(img, layer)
    return img


def option_1(img):
    """안 1: the status and two buttons at the panel's bottom — [파티 편성] opens the popup to look and form, [출발] opens it to set out."""
    d = ImageDraw.Draw(img)
    d.line((1346, 790, 1854, 790), fill=LINE, width=1)
    label(d, (1346, 812), "파티 4명 · 출발할 수 있습니다", 22, GOOD)
    button(d, (1346, 860, 1600, 1000), "파티 편성", QUIET, 28)
    button(d, (1620, 860, 1854, 1000), "출발", BUTTON, 34)
    return img


def option_2(img):
    """안 2: a short party line as well — who stands where and the worst fatigue — then the buttons."""
    d = ImageDraw.Draw(img)
    d.line((1346, 772, 1854, 772), fill=LINE, width=1)
    label(d, (1346, 784), "파티", 20, DIM)
    label(d, (1346, 812), "1열 로언 · 2열 카이 · 3열 엘라 · 4열 미라", 20, TEXT)
    label(d, (1346, 840), "피로 최대 0/200 · 고통 없음", 18, DIM)
    label(d, (1854, 784), "출발할 수 있습니다", 20, GOOD, "ra")
    button(d, (1346, 884, 1600, 1000), "파티 편성", QUIET, 28)
    button(d, (1620, 884, 1854, 1000), "출발", BUTTON, 34)
    return img


def caption(img, text):
    d = ImageDraw.Draw(img)
    d.rectangle((0, 0, 1920, 56), fill=(0, 0, 0, 200))
    label(d, (24, 28), text, 32, TEXT, "lm")
    return img


def main():
    now = Image.open(HERE / "base-expedition.png").convert("RGBA")
    a = option_1(without_strip())
    b = option_2(without_strip())
    a.save(HERE / "mock-strip-1-buttons.png"); b.save(HERE / "mock-strip-2-line.png")
    tiles = [caption(now.copy(), "지금 — 아래 띠에 파티 칸 넷과 [출발]"),
             caption(a.copy(), "안 1 (권장) — 띠 없음. 정보 패널 아래에 안내와 [파티 편성] [출발]"),
             caption(b.copy(), "안 2 — 띠 없음. 정보 패널 아래에 파티 한 줄(열·이름·피로)과 두 버튼")]
    sheet = Image.new("RGBA", (1920 + 20, 540 * 2 + 30), (0x0C, 0x0C, 0x0E, 255))
    for i, t in enumerate(tiles):
        sheet.paste(t.resize((960, 540), Image.LANCZOS), (10 + 970 * (i % 2), 10 + 550 * (i // 2)))
    sheet.save(HERE / "mock-strip-compare.png")
    print("ok")


if __name__ == "__main__":
    main()
