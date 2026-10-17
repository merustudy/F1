# -*- coding: utf-8 -*-
"""Round 75: forming the party in a popup (the user's 2026-10-17 request "원정 로스터는 원정 버튼 누르면 팝업으로 로스터 구성 제공").
Mockups over the screens as they are (base-expedition.png = ko_56_expedition, base-lobby.png = ko_03_lobby_party of 2026-10-17):
three places for the popup, side by side with the expedition screen as it is. Drawn shapes and text only. No API call.
  .venv/bin/python ArtPipeline/Archive/75-party-popup/mock_party.py
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
FONT = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"
TEXT = (0xEB, 0xEB, 0xE6); DIM = (0x9A, 0xA0, 0xAC); BRASS = (0xB8, 0x94, 0x4E); BONE = (0xCD, 0xC0, 0xA0)
PANEL = (0x1E, 0x22, 0x2B); PANEL_LIGHT = (0x2A, 0x30, 0x3C); SLOT = (0x15, 0x18, 0x1F); LINE = (0x55, 0x5C, 0x6B)
BUTTON = (0x3B, 0x6F, 0xB5); QUIET = (0x3A, 0x41, 0x50); SELECTED = (0x80, 0x66, 0x14); GOOD = (0x58, 0xB3, 0x68); BURN = (0xF3, 0x9C, 0x12)
DISABLED = (0x2E, 0x33, 0x3E)

# The roster of the screenshot (day 1): name, job, level, row in the party (0 = out).
ROSTER = [("아스트리드", "발키리", 2, 0), ("세드릭", "성기사", 1, 0), ("엘라", "주교", 1, 3), ("카이", "마검사", 1, 2), ("미라", "대마법사", 1, 4), ("로언", "기사", 1, 1)]


def font(px): return ImageFont.truetype(str(FONT), int(round(px)))


def label(d, xy, s, px, color=TEXT, anchor="la"):
    d.text(xy, s, font=font(px), fill=color, anchor=anchor)


def shade(img, box):
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    ImageDraw.Draw(layer).rectangle(box, fill=(0, 0, 0, 171))
    return Image.alpha_composite(img, layer)


def button(d, box, s, color=QUIET, px=22, text=TEXT):
    d.rectangle(box, fill=color)
    label(d, ((box[0] + box[2]) / 2, (box[1] + box[3]) / 2), s, px, text, "mm")


def pips(d, x, y, w, h=12, n=10, gap=4):
    pw = (w - gap * (n - 1)) / n
    for i in range(n):
        d.rectangle((x + i * (pw + gap), y, x + i * (pw + gap) + pw, y + h), fill=SLOT)


def slot(d, x, y, row, member):
    """A row of the party in the popup: the member's card with the moves, or the empty row and its hint."""
    w, h = 320, 200
    d.rectangle((x, y, x + w, y + h), fill=PANEL_LIGHT)
    label(d, (x + 16, y + 14), "%d열" % row, 20, DIM)
    if member is None:
        label(d, (x + 76, y + 12), "비어 있음", 26, DIM)
        label(d, (x + 16, y + 56), "로스터에서 용병을 누르세요", 20, DIM)
        return
    name, job, lv = member
    label(d, (x + 76, y + 12), name, 26, TEXT)
    label(d, (x + w - 16, y + 16), "Lv %d" % lv, 18, BONE, "ra")
    label(d, (x + 16, y + 56), job, 20, DIM)
    pips(d, x + 16, y + 92, w - 32)
    button(d, (x + 16, y + 136, x + 112, y + 180), "앞으로", QUIET if row > 1 else DISABLED, 20, TEXT if row > 1 else DIM)
    button(d, (x + 120, y + 136, x + 216, y + 180), "뒤로", QUIET if row < 4 else DISABLED, 20, TEXT if row < 4 else DIM)
    button(d, (x + 224, y + 136, x + 304, y + 180), "제외", QUIET, 20)


def chip(d, x, y, member):
    """A mercenary of the roster in the popup: a button. In the party it is lit and tagged with its row."""
    name, job, lv, row = member
    w, h = 320, 110
    d.rectangle((x, y, x + w, y + h), fill=SELECTED if row else PANEL_LIGHT, outline=BRASS if row else None, width=2 if row else 0)
    label(d, (x + 12, y + 10), name, 22, TEXT)
    label(d, (x + 200 + 60, y + 14), "Lv %d" % lv, 16, BONE, "ra")
    label(d, (x + 12, y + 44), job, 16, DIM)
    if row:
        label(d, (x + w - 12, y + 44), "%d열" % row, 16, BRASS, "ra")
    pips(d, x + 12, y + 82, w - 24, 10)


def popup(img, title, sub, confirm, status="출발할 수 있습니다", status_color=GOOD):
    """The popup itself: the four rows on top, the roster under them, the words and the confirming button at the bottom."""
    d = ImageDraw.Draw(img)
    x0, y0, x1, y1 = 260, 140, 1660, 940
    d.rectangle((x0, y0, x1, y1), fill=PANEL, outline=BRASS, width=2)
    label(d, (x0 + 24, y0 + 22), title, 30, DIM)
    button(d, (x1 - 180, y0 + 16, x1 - 24, y0 + 66), "닫기", QUIET, 22)
    label(d, (x0 + 24, y0 + 70), sub, 22, TEXT)
    by_row = {m[3]: m for m in ROSTER if m[3]}
    for i in range(4):
        m = by_row.get(i + 1)
        slot(d, x0 + 24 + 340 * i, y0 + 110, i + 1, None if m is None else (m[0], m[1], m[2]))
    label(d, (x0 + 24, y0 + 334), "로스터 6/8 — 눌러서 파티에 넣고 뺀다", 24, DIM)
    for i, m in enumerate(ROSTER):
        chip(d, x0 + 24 + 344 * (i % 4), y0 + 376 + 130 * (i // 4), m)
    label(d, (x0 + 24, y1 - 72), status, 24, status_color)
    button(d, (x1 - 24 - 294, y1 - 24 - 64, x1 - 24, y1 - 24), confirm, BUTTON, 30)
    return img


def option_a():
    """A: the lobby's expedition button opens the popup over the town; confirming goes to the expedition screen (the party read only there)."""
    img = shade(Image.open(HERE / "base-lobby.png").convert("RGBA"), (0, 100, 1920, 960))
    return popup(img, "원정 파티", "파티를 꾸리고 원정 화면으로 간다", "원정 화면으로")


def option_b():
    """B (recommended): the expedition screen's departure opens the popup over the map; the departure itself is inside it."""
    img = shade(Image.open(HERE / "base-expedition.png").convert("RGBA"), (0, 100, 1920, 1080))
    return popup(img, "원정 파티", "버려진 광산 · 5일 (→ 6일차)", "출발")


def option_c():
    """C: the popup opens by itself when the screen opens, and again from a button on the strip; confirming closes it, the departure stays on the screen."""
    base = Image.open(HERE / "base-expedition.png").convert("RGBA")
    d = ImageDraw.Draw(base)
    button(d, (1300, 844, 1500, 894), "파티 편성", QUIET, 22)
    img = shade(base, (0, 100, 1920, 1080))
    return popup(img, "원정 파티", "버려진 광산 · 5일 (→ 6일차)", "확인")


def caption(img, text):
    d = ImageDraw.Draw(img)
    d.rectangle((0, 0, 1920, 56), fill=(0, 0, 0, 200))
    label(d, (24, 28), text, 32, TEXT, "lm")
    return img


def main():
    now = Image.open(HERE / "base-expedition.png").convert("RGBA")
    a, b, c = option_a(), option_b(), option_c()
    a.save(HERE / "mock-party-A-lobby.png"); b.save(HERE / "mock-party-B-depart.png"); c.save(HERE / "mock-party-C-auto.png")
    tiles = [caption(now.copy(), "지금 — 파티는 막사에서 꾸리고 원정 화면은 보기만"),
             caption(a, "안 A — 로비의 [원정]이 팝업을 연다 → 원정 화면"),
             caption(b, "안 B (권장) — 원정 화면의 [출발]이 팝업을 열고, 출발은 팝업에서"),
             caption(c, "안 C — 원정 화면이 열리면 팝업이 바로 뜨고, 띠의 [파티 편성]으로 다시")]
    sheet = Image.new("RGBA", (1920 + 20, 1080 + 20), (0x0C, 0x0C, 0x0E, 255))
    for i, t in enumerate(tiles):
        sheet.paste(t.resize((960, 540), Image.LANCZOS), (10 + 970 * (i % 2), 10 + 550 * (i // 2)))
    sheet.save(HERE / "mock-party-compare.png")
    print("ok")


if __name__ == "__main__":
    main()
