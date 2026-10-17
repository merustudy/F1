# -*- coding: utf-8 -*-
"""Round 74: selling items at the shop (the user's 2026-10-17 request). Mockups over the shop window as it is (base-shop.png, the
screenshot ko_42_shop of 2026-10-17): three ways to sell, side by side with the window as it is. Drawn shapes and text only. No API call.
  .venv/bin/python ArtPipeline/Archive/74-shop-sell/mock_sell.py
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
FONT = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"
TEXT = (0xEB, 0xEB, 0xE6); DIM = (0x9A, 0xA0, 0xAC); BRASS = (0xB8, 0x94, 0x4E); GOLD = (0xD4, 0xA2, 0x32)
SLOT = (0x15, 0x18, 0x1F); PANEL = (0x1E, 0x22, 0x2B); LINE = (0x55, 0x5C, 0x6B); DANGER = (0xC0, 0x39, 0x2B)
BUTTON = (0x3B, 0x6F, 0xB5); QUIET = (0x3A, 0x41, 0x50); SELECTED = (0x80, 0x66, 0x14); GOOD = (0x58, 0xB3, 0x68)


def font(px): return ImageFont.truetype(str(FONT), int(round(px)))


def base(): return Image.open(HERE / "base-shop.png").convert("RGBA")


def coin(d, x, y, r=11):
    d.ellipse((x - r, y - r, x + r, y + r), fill=GOLD, outline=(0x7A, 0x52, 0x16), width=2)
    d.ellipse((x - r * 0.45, y - r * 0.45, x + r * 0.45, y + r * 0.45), outline=(0x7A, 0x52, 0x16), width=2)


def label(d, xy, s, px, color=TEXT, anchor="la"):
    d.text(xy, s, font=font(px), fill=color, anchor=anchor)


def tray(img, holding=None, blocked=None):
    """Option A: the merchant's tray between the refresh and the leave buttons. Put a held item on it and it is sold."""
    d = ImageDraw.Draw(img)
    x0, y0, x1, y1 = 1304, 700, 1636, 752
    d.rounded_rectangle((x0, y0, x1, y1), radius=8, fill=SLOT, outline=BRASS, width=3)
    # dashed inner rim: a slot that waits
    for x in range(x0 + 10, x1 - 10, 14):
        d.line((x, y0 + 7, x + 7, y0 + 7), fill=LINE, width=2); d.line((x, y1 - 7, x + 7, y1 - 7), fill=LINE, width=2)
    if blocked:
        label(d, ((x0 + x1) // 2, (y0 + y1) // 2), blocked, 20, DANGER, "mm")
    elif holding:
        name, value = holding
        d.rounded_rectangle((x0, y0, x1, y1), radius=8, outline=GOLD, width=3)
        label(d, (x0 + 18, (y0 + y1) // 2), "팔기", 22, TEXT, "lm")
        label(d, (x0 + 80, (y0 + y1) // 2), name, 20, DIM, "lm")
        coin(d, x1 - 60, (y0 + y1) // 2)
        label(d, (x1 - 44, (y0 + y1) // 2), value, 22, GOLD, "lm")
    else:
        label(d, (x0 + 18, (y0 + y1) // 2), "팔기", 22, DIM, "lm")
        label(d, (x0 + 80, (y0 + y1) // 2), "아이템을 든 채로 여기에 놓으세요 · 값의 절반", 17, DIM, "lm")
    return img


def held_item(img, box, name):
    """A board item picked up: its square lit and a ghost of it under the pointer, as the game draws a held item."""
    d = ImageDraw.Draw(img)
    d.rectangle(box, outline=GOLD, width=3)
    px, py = 1470, 640
    d.rounded_rectangle((px - 70, py - 18, px + 70, py + 18), radius=6, fill=(0x3A, 0x41, 0x50, 230), outline=BRASS, width=2)
    label(d, (px, py), name, 18, TEXT, "mm")
    return img


def card_button(img):
    """Option B: the item card (right-click) gains a sell button while at the shop."""
    d = ImageDraw.Draw(img)
    x0, y0, x1, y1 = 700, 400, 900, 640
    d.rectangle((x0, y0, x1, y1), fill=(0x0C, 0x0C, 0x0E, 240), outline=LINE, width=2)
    lines = [("녹슨 칼", 20, TEXT), ("무기 장비", 16, DIM), ("크기 2×1 · 동", 16, DIM), ("쿨다운 2.8초", 16, DIM), ("맨 앞에서만 발동", 16, DIM), ("앞열 피해 1.66-3.34", 16, (0x9F, 0xC2, 0xF3)), ("값 12 코인", 16, DIM)]
    y = y0 + 16
    for s, px, c in lines:
        label(d, ((x0 + x1) // 2, y), s, px, c, "mt"); y += px + 10
    d.rounded_rectangle((x0 + 20, y1 - 52, x1 - 20, y1 - 12), radius=6, fill=BUTTON)
    label(d, ((x0 + x1) // 2 - 14, y1 - 32), "팔기", 20, TEXT, "mm")
    coin(d, (x0 + x1) // 2 + 30, y1 - 32, 9); label(d, ((x0 + x1) // 2 + 44, y1 - 32), "6", 20, GOLD, "lm")
    # the caret to the item on the board
    d.polygon([(700, 690), (716, 640), (740, 640)], fill=(0x0C, 0x0C, 0x0E))
    d.rectangle((700, 652, 860, 692), outline=GOLD, width=3)
    return img


def sell_mode(img):
    """Option C: a 'sell' toggle beside the refresh button; while it is on, every sellable item shows its value and a click sells it."""
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((1304, 703, 1440, 750), radius=6, fill=SELECTED, outline=GOLD, width=2)
    label(d, (1372, 726), "팔기 켬", 22, TEXT, "mm")
    # values on the party boards' items (the four base weapons cannot be sold: a dim 'X'), one dagger in a bag, one in the inventory
    for x in (128, 318, 508, 698):
        d.rounded_rectangle((x + 112, 650, x + 164, 672), radius=4, fill=(0x33, 0x33, 0x38, 220))
        label(d, (x + 138, 661), "팔 수 없음", 12, DIM, "mm")
    d.rectangle((128, 700, 236, 750), fill=(0x2A, 0x30, 0x3C), outline=LINE, width=2)
    label(d, (182, 718), "단검 (동)", 15, TEXT, "mm")
    d.rounded_rectangle((176, 728, 236, 750), radius=4, fill=(0x0C, 0x0C, 0x0E, 230))
    coin(d, 190, 739, 8); label(d, (202, 739), "6", 16, GOLD, "lm")
    label(d, (1000, 1020), "아이템을 누르면 값만큼 코인을 받고 사라집니다. 다시 누르면 팔기를 끕니다.", 17, DIM, "lm")
    return img


def caption(img, text):
    d = ImageDraw.Draw(img)
    d.rectangle((0, 0, 1920, 56), fill=(0, 0, 0, 200))
    label(d, (24, 28), text, 30, TEXT, "lm")
    return img


def main():
    out = HERE
    now = caption(base(), "지금: 상점 창. 사기와 새로고침, 나가기만 있다")
    a1 = caption(tray(base()), "안 A (권장): 상인의 접시 — 새로고침과 나가기 사이. 든 아이템을 놓으면 팔린다")
    a2 = caption(tray(held_item(base(), (700, 652, 860, 692), "녹슨 칼 (동)"), holding=("녹슨 칼 (동)", "6")), "안 A, 든 동안: 접시가 값을 보이고 금테로 기다린다")
    a3 = caption(tray(held_item(base(), (700, 652, 860, 692), "롱소드"), blocked="기본 무기는 팔 수 없습니다"), "안 A, 팔 수 없는 것(기본 무기·가방·유니크)을 들면 접시가 붉게 말한다")
    b = caption(card_button(base()), "안 B: 아이템 카드(우클릭)에 '팔기' 버튼 — 상점에서만")
    c = caption(sell_mode(base()), "안 C: '팔기' 토글 — 켜면 칸마다 값이 뜨고 누르면 팔린다")
    for name, img in (("mock-now", now), ("mock-A-tray", a1), ("mock-A-holding", a2), ("mock-A-blocked", a3), ("mock-B-card", b), ("mock-C-mode", c)):
        img.convert("RGB").save(out / f"{name}.png")
    # the comparison: six at half size, two columns
    w, h = 960, 540
    sheet = Image.new("RGB", (w * 2 + 30, h * 3 + 40), (18, 18, 20))
    for i, img in enumerate((now, a1, a2, a3, b, c)):
        sheet.paste(img.convert("RGB").resize((w, h), Image.LANCZOS), (10 + (i % 2) * (w + 10), 10 + (i // 2) * (h + 10)))
    sheet.save(out / "mock-sell-compare.png")
    print("wrote", out / "mock-sell-compare.png")


if __name__ == "__main__":
    main()
