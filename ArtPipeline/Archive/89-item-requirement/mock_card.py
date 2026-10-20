# -*- coding: utf-8 -*-
"""Round 89: the item card's "요구" line and the refused ghost's label (Slice D stage 42, 장비 요구 스탯 — Docs/Design/02 §4, Architecture/12).
No base screenshot: the cards are drawn in the card's own colours at the card's width, text only, as round 88 did — the card's frame and places do not
change, one fact line is added. Beside the cards, a small board with a held item's refused ghost (red, 45%) and the label the rule would give it.
  .venv/bin/python ArtPipeline/Archive/89-item-requirement/mock_card.py   -> mock-requirement.png
Columns: 지금 (no requirement line) / A 권장 (the line last among the facts) / B (the line right after the size). Three cards each: the longsword on
the warrior's board (met, white), the halberd on the floor with nobody in the party meeting it (red), the iron helm in the shop (met by someone, white).
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
FONT = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"

CARD_BG = (19, 19, 19)
TEXT = (235, 235, 230)
DIM = (160, 160, 152)
EFFECT = (112, 112, 255)
GOOD = (88, 179, 104)          # UiPalette.Good
BLOOD_LIGHT = (216, 106, 90)   # UiPalette.BloodLight
BRASS = (199, 179, 119)
SHEET_BG = (10, 10, 10)
# The board (Architecture/12 "격자 보드"): Diablo's black squares, the navy piece, the bag's stone rim, the ghost's red.
SQUARE = (12, 12, 12)
SQUARE_LINE = (58, 58, 58)
PIECE = (28, 32, 64)
RIM = (74, 66, 54)
GHOST_REFUSED = (0xB0, 0x1E, 0x1E)   # UiPalette.GhostRefused
GHOST_FITS = (0x1A, 0x96, 0x30)      # UiPalette.GhostFits
LABEL_BOX = (0, 0, 0)
LABEL_LINE = (110, 110, 110)

W, PAD, LINE = 360, 18, 30
CELL, GAP, RIM_W = 50, 2, 5


def font(size):
    return ImageFont.truetype(str(FONT), size)


def card(draw, x, y, title, subtitle, effects, facts):
    """One card as the game draws it since round 88: title, the effect lines (blue) first, then the facts. A fact may be a list of (text, colour) runs."""
    draw.rectangle((x, y, x + W, y + 40 + LINE * (len(facts) + len(effects)) + 2 * PAD), fill=CARD_BG, outline=(60, 60, 60))
    draw.text((x + PAD, y + PAD), title, font=font(22), fill=BRASS)
    draw.text((x + PAD + 120, y + PAD + 6), subtitle, font=font(13), fill=DIM)
    cy = y + PAD + 40
    for effect in effects:
        draw.text((x + PAD, cy), effect, font=font(17), fill=EFFECT)
        cy += LINE
    for fact in facts:
        runs = fact if isinstance(fact, list) else [(fact, TEXT)]
        cx = x + PAD
        for text, colour in runs:
            draw.text((cx, cy), text, font=font(17), fill=colour)
            cx += draw.textlength(text, font=font(17))
        cy += LINE
    return cy - y + PAD


def column(draw, x, y, heading, cards):
    cy = y
    for line in heading.split("\n"):
        draw.text((x, cy), line, font=font(18), fill=TEXT)
        cy += 26
    cy += 10
    for c in cards:
        cy += card(draw, x, cy, *c) + 18
    return cy


def board(img, x, y, ghost_colour, label, caption):
    """A 3x3 start bag with the longsword (3x1) on its first row, a held 2x1 item's ghost on the second row and the ghost's label box above it."""
    draw = ImageDraw.Draw(img)
    size = 3 * CELL + 2 * GAP
    draw.rectangle((x - RIM_W, y - RIM_W, x + size + RIM_W, y + size + RIM_W), fill=RIM)
    draw.rectangle((x, y, x + size, y + size), fill=SQUARE_LINE)
    for row in range(3):
        for col in range(3):
            cx, cy = x + col * (CELL + GAP), y + row * (CELL + GAP)
            draw.rectangle((cx, cy, cx + CELL, cy + CELL), fill=PIECE if row == 0 else SQUARE)
    draw.text((x + 8, y + 14), "롱소드", font=font(16), fill=(190, 190, 200))
    # The ghost: one block over the two squares (and the gap between), 45% (GridBoardView.GhostAlpha).
    overlay = Image.new("RGBA", img.size, (0, 0, 0, 0))
    od = ImageDraw.Draw(overlay)
    gy = y + (CELL + GAP)
    od.rectangle((x, gy, x + 2 * CELL + GAP, gy + CELL), fill=ghost_colour + (115,))
    img.paste(Image.alpha_composite(img.convert("RGBA"), overlay).convert("RGB"))
    draw = ImageDraw.Draw(img)
    if label:
        tw = draw.textlength(label, font=font(15))
        bx = x + CELL + GAP // 2 - tw / 2 - 8
        by = gy - 30
        draw.rectangle((bx, by, bx + tw + 16, by + 24), fill=LABEL_BOX, outline=LABEL_LINE)
        draw.text((bx + 8, by + 3), label, font=font(15), fill=TEXT)
    draw.text((x - RIM_W, y + size + 14), caption, font=font(14), fill=DIM)


def sheet():
    img = Image.new("RGB", (3 * W + 4 * PAD + 2 * 40, 1290), SHEET_BG)
    draw = ImageDraw.Draw(img)
    draw.text((PAD, 12), "Round 89 · 아이템 카드의 \"요구\" 줄과 거절 그림자의 글 (42단계 장비 요구 스탯). 틀과 자리는 그대로, 사실 줄 하나가 는다.", font=font(21), fill=TEXT)
    draw.text((PAD, 44), "파티: 로언 전사(힘 6) · 아스트리드 바이킹(힘 7) · 엘라 사제(힘 3) · 미라 마법사(힘 3). 미늘창은 힘 7을 요구한다(바이킹만 든다 → 아래 B 변형 참고).", font=font(16), fill=DIM)

    longsword_now = ("롱소드", "로언의 보드 · 무기 장비 · 3×1 · 근접",
                     ["앞의 적에게 피해 1-3"],
                     [[("쿨다운 ", TEXT), ("3.0초", BLOOD_LIGHT)], [("치명타 ", TEXT), ("−1%", BLOOD_LIGHT)], [("명중 ", TEXT), ("74%", BLOOD_LIGHT)], "앞에서 2번째 자리까지만 발동"])
    halberd_now = ("미늘창", "바닥의 전리품 · 무기 장비 · 2×3 · 근접",
                   ["앞의 적 3명에게 피해 1-2"],
                   ["쿨다운 3.6초", "치명타 5%", "명중 80%", "맨 앞에서만 발동"])
    helm_now = ("철 투구", "상점의 툴팁 · 방어 장비 · 2×1",
                ["자신에게 보호막 2", "받는 치명타 확률 -3%p"],
                ["전투 시작 때 발동"])
    now = [longsword_now, halberd_now, helm_now]

    def with_line(c, line, last=True):
        title, sub, effects, facts = c
        facts = list(facts)
        if last:
            facts.append(line)
        else:
            facts.insert(0, line)
        return (title, sub, effects, facts)

    met = [("요구 힘 5", TEXT)]
    nobody = [("요구 힘 7", BLOOD_LIGHT)]
    helm_line = [("요구 힘 3", TEXT)]
    a = [with_line(longsword_now, met), with_line(halberd_now, nobody), with_line(helm_now, helm_line)]
    b = [with_line(longsword_now, met, last=False), with_line(halberd_now, nobody, last=False), with_line(helm_now, helm_line, last=False)]

    x = PAD
    column(draw, x, 84, "지금\n카드에 요구가 없다\n", now)
    x += W + 40
    column(draw, x, 84, "A 권장 · 사실 줄의 마지막에 \"요구 힘 N\"\n주인이 채우면 흰색, 미달이면 붉음(핏빛)\n주인 없는 카드: 파티에 드는 사람이 없으면 붉음", a)
    x += W + 40
    column(draw, x, 84, "B · 크기 다음에 \"요구 힘 N\"\n색은 A와 같다\n", b)

    # The ghost: the halberd held over the priest's board (refused, with the reason) and over the viking's (fits).
    bx = PAD
    by = 1040
    draw.text((bx, by - 36), "거절 그림자의 글 (A·B 공통): 든 미늘창을 엘라(힘 3)의 보드 위에 — 빨강 + \"힘 7 필요\". 아스트리드(힘 7)의 보드 위에서는 지금처럼 초록, 글 없음.", font=font(16), fill=TEXT)
    board(img, bx + RIM_W, by + 8, GHOST_REFUSED, "힘 7 필요", "엘라(사제, 힘 3)의 보드")
    board(img, bx + RIM_W + 240, by + 8, GHOST_FITS, None, "아스트리드(바이킹, 힘 7)의 보드")
    draw = ImageDraw.Draw(img)
    draw.text((PAD, 1258), "수치는 보기용이다(출발값, 1성 아님). 색은 카드의 흰색·BloodLight(붉음)·효과의 파랑, 그림자의 GhostRefused·GhostFits 그대로. 그림자의 글 상자는 지금의 \"→ 인벤토리\"와 같은 상자다.", font=font(14), fill=DIM)
    img.save(HERE / "mock-requirement.png")
    print("wrote", HERE / "mock-requirement.png")


if __name__ == "__main__":
    sheet()
