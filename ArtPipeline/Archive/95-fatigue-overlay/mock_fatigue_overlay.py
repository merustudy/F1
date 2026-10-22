# -*- coding: utf-8 -*-
"""Round 95: the fatigue pips in two laps (2026-10-22, the user's "피로도 ui를 0-100까지 보라 색으로 끝까지 표시 / 100-200은 그 위에 빨간 색으로
중첩해서 표시하는 방향 검토 및 목업 제공").

  .venv/bin/python ArtPipeline/Archive/95-fatigue-overlay/mock_fatigue_overlay.py

Now (round 33 "C"): ten pips of 20 each; the first five fill violet, the five from the threshold (100) red.
A (the user's): ten pips of 10 each. 0..100 fills them all violet; 100..200 fills the same pips again from the left in red, over the violet.
B: as A, but the red lap is drawn inside the pip (a band inset from its top and bottom), so the violet lap shows around it.
The pips are repainted at their places in the game's screenshots (game/): the barracks' roster lines (590 wide, 12 high, 4 apart), and the marks under
the feet on the map and in battle (as wide as the HP bar, 4 high, 2 apart). Colours: UiPalette.Slot, FatigueBar, FatigueDanger, Fatigue.
No paid call: shapes and text only.
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
GAME = HERE / "game"
FONT = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"

SLOT = (0x15, 0x18, 0x1F)
VIOLET = (0x9E, 0x84, 0xDA)        # UiPalette.FatigueBar
RED = (0xC8, 0x4B, 0x6E)           # UiPalette.FatigueDanger
WORDS = (0xCF, 0xBA, 0xF7)         # UiPalette.Fatigue
ENTRY_BG = (42, 49, 61)
SHEET_BG = (14, 15, 19)
TEXT = (235, 235, 230)
DIM = (150, 150, 145)
BRASS = (199, 179, 119)

MAX, BREAK, COUNT = 200, 100, 10


def font(size):
    return ImageFont.truetype(str(FONT), size)


# ---- the three ways of filling one row -----------------------------------------------------------------------------------

def fill_row(draw, pips, y, h, fatigue, way):
    """Paints one row of pips: pips is a list of (left, right) x ranges (inclusive)."""
    for i, (x0, x1) in enumerate(pips):
        w = x1 - x0 + 1
        draw.rectangle((x0, y, x1, y + h - 1), fill=SLOT)
        if way == "now":
            share = MAX / COUNT
            ratio = min(1.0, max(0.0, (fatigue - i * share) / share))
            colour = RED if i * share >= BREAK else VIOLET
            if ratio > 0:
                draw.rectangle((x0, y, x0 + round(ratio * w) - 1, y + h - 1), fill=colour)
            continue

        share = BREAK / COUNT
        first = min(1.0, max(0.0, (fatigue - i * share) / share))
        second = min(1.0, max(0.0, (fatigue - BREAK - i * share) / share))
        if first > 0:
            draw.rectangle((x0, y, x0 + round(first * w) - 1, y + h - 1), fill=VIOLET)
        if second > 0:
            inset = 0 if way == "A" else max(1, round(h * 0.25))
            draw.rectangle((x0, y + inset, x0 + round(second * w) - 1, y + h - 1 - inset), fill=RED)


def roster_pips():
    xs = [(84, 138), (143, 198), (203, 257), (262, 317), (322, 376), (381, 435), (440, 495), (500, 554), (559, 614), (619, 673)]
    return xs


def row_pips(width, gap):
    """COUNT pips across a width with a gap between two, as UiPrefabSetup.Lobby.FatiguePipRow lays them."""
    pip = (width - gap * (COUNT - 1)) / COUNT
    return [(round(i * (pip + gap)), round(i * (pip + gap) + pip) - 1) for i in range(COUNT)]


def marks_pips(x0, width=160):
    out = []
    for i in range(COUNT):
        left = x0 + round(i * width / COUNT) + (0 if i == 0 else 1)
        right = x0 + round((i + 1) * width / COUNT) - (0 if i == COUNT - 1 else 1) - 1
        out.append((left, right))
    return out


# ---- the screens -----------------------------------------------------------------------------------------------------------

BARRACKS = [40, 100, 128, 175, 200]          # 아스트리드, 엘라, 카이, 미라, 로언
MAP = {130: 40, 320: 100, 510: 175, 700: 128}    # 미라, 엘라, 카이, 로언 (the map's 로언 is fearful at 128: "전사 · 공포 · 과로")
BATTLE = {130: 40, 320: 100, 510: 128, 700: 200}  # 미라, 엘라, 카이, 로언


def barracks(way):
    im = Image.open(GAME / "ko_57_lobby_barracks.png").convert("RGB")
    d = ImageDraw.Draw(im)
    f = font(20)
    for row, fatigue in enumerate(BARRACKS):
        top = 314 + 100 * row
        fill_row(d, roster_pips(), top, 12, fatigue, way)
        # the words "피로도 N/200" right-aligned at the pips' end, red from the threshold on (as now)
        d.rectangle((520, 280 + 100 * row, 676, 305 + 100 * row), fill=ENTRY_BG)
        words = f"피로도 {fatigue}/{MAX}"
        tw = d.textlength(words, font=f)
        d.text((673 - tw, 280 + 100 * row), words, font=f, fill=RED if fatigue >= BREAK else WORDS)
    return im


def marks(name, y, values, way):
    im = Image.open(GAME / name).convert("RGB")
    d = ImageDraw.Draw(im)
    for x0, fatigue in values.items():
        fill_row(d, marks_pips(x0), y, 4, fatigue, way)
    return im


def screens(way):
    return barracks(way), marks("ko_36_map_state.png", 538, MAP, way), marks("ko_05_battle.png", 586, BATTLE, way)


# ---- the sheet -------------------------------------------------------------------------------------------------------------

WAYS = [("now", "지금 — 칸 하나 20, 100부터 붉은 칸"), ("A", "A (요청안·권장) — 칸 하나 10, 100 넘으면 그 위를 빨강으로 덮음"), ("B", "B — A와 같되 빨강을 칸 안쪽 띠로(보라가 둘레로 남음)")]


def values_table(sheet, top):
    d = ImageDraw.Draw(sheet)
    d.text((40, top), "1. 값마다 — 막사 줄의 칸(실제 590×12, 여기서는 540 폭으로 줄임, 사이 4)", font=font(26), fill=BRASS)
    top += 46
    col_x = [150, 740, 1330]
    row_width = 540
    for c, (way, label) in enumerate(WAYS):
        d.text((col_x[c], top), label, font=font(16), fill=TEXT if way != "A" else BRASS)
    top += 34
    values = [0, 40, 99, 100, 105, 128, 150, 175, 199, 200]
    for v in values:
        d.text((40, top - 4), f"피로 {v}", font=font(18), fill=RED if v >= BREAK else WORDS)
        for c, (way, _) in enumerate(WAYS):
            row = Image.new("RGB", (row_width, 12), SHEET_BG)
            rd = ImageDraw.Draw(row)
            fill_row(rd, row_pips(row_width, 4), 0, 12, v, way)
            sheet.paste(row, (col_x[c], top))
        top += 30
    return top + 10


def sheet_all(shots):
    width = 1920
    sheet = Image.new("RGB", (width, 2600), SHEET_BG)
    d = ImageDraw.Draw(sheet)
    d.text((40, 24), "Round 95 — 피로 칸의 두 바퀴: 0~100 보라, 100~200은 그 위에 빨강", font=font(34), fill=TEXT)
    d.text((40, 72), "칸은 열 개 그대로. 바뀌는 것은 칸 하나가 맡는 피로(20 → 10)와 100을 넘은 피로를 그리는 법뿐. 글 \"피로도 N/200\"과 색은 지금처럼(100부터 붉음).", font=font(18), fill=DIM)
    top = values_table(sheet, 120)

    d.text((40, top), "2. 막사 — 아스트리드 40 · 엘라 100 · 카이 128 · 미라 175 · 로언 200", font=font(26), fill=BRASS)
    top += 46
    for c, (way, label) in enumerate(WAYS):
        crop = shots[way][0].crop((64, 268, 690, 760))
        crop = crop.resize((int(crop.width * 0.95), int(crop.height * 0.95)), Image.LANCZOS)
        x = 40 + c * 620
        sheet.paste(crop, (x, top + 30))
        d.text((x, top), label.split(" — ")[0], font=font(18), fill=TEXT if way != "A" else BRASS)
    top += 30 + int(492 * 0.95) + 30

    d.text((40, top), "3. 발밑(지도·전투, 실제 크기의 1.55배) — 지도: 미라 40 · 엘라 100 · 카이 175 · 로언 128(공포) / 전투: 미라 40 · 엘라 100 · 카이 128 · 로언 200", font=font(22), fill=BRASS)
    top += 40
    for c, (way, label) in enumerate(WAYS):
        d.text((40, top + 8), label.split(" — ")[0], font=font(18), fill=TEXT if way != "A" else BRASS)
        m = shots[way][1].crop((120, 506, 872, 560))
        m = m.resize((int(m.width * 1.55), int(m.height * 1.55)), Image.NEAREST)
        sheet.paste(m, (150, top))
        top += m.height + 8
        b = shots[way][2].crop((120, 560, 872, 600))
        b = b.resize((int(b.width * 1.55), int(b.height * 1.55)), Image.NEAREST)
        sheet.paste(b, (150, top))
        top += b.height + 26
    return sheet.crop((0, 0, width, top + 20))


def main():
    shots = {way: screens(way) for way, _ in WAYS}
    for way in ("A", "B"):
        for part, im in zip(("barracks", "map", "battle"), shots[way]):
            im.save(HERE / f"mock-{way}-{part}.png")
    sheet_all(shots).save(HERE / "mock-compare.png")
    # the marks at three times, for the 4-pixel rows
    detail = Image.new("RGB", (1920, 600), SHEET_BG)
    d = ImageDraw.Draw(detail)
    d.text((40, 20), "발밑의 칸 3배 — 전투의 로언 200 · 카이 128 · 엘라 100 (위: 지금, 가운데: A, 아래: B)", font=font(24), fill=TEXT)
    y = 70
    for way, label in WAYS:
        crop = shots[way][2].crop((318, 578, 860, 596))
        crop = crop.resize((crop.width * 3, crop.height * 3), Image.NEAREST)
        d.text((40, y + 16), label.split(" — ")[0], font=font(18), fill=TEXT if way != "A" else BRASS)
        detail.paste(crop, (240, y))
        y += crop.height + 40
    detail.crop((0, 0, 1920, y)).save(HERE / "mock-detail.png")


if __name__ == "__main__":
    main()
