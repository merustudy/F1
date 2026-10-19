# -*- coding: utf-8 -*-
"""Round 86: the enemies' boards as the start bag (3x3), taller when the items do not fit (2026-10-19 사용자 "적 몬스터들도 인벤토리 기본
공간(3*3)으로 시작 (보스 몹 등 아이템 많은 경우 자동 확대) 검토 수행"). Drawn over the 2026-10-19 screenshots of 20261019-stars85:
base-abyss.png = ko_64 (the Abyss Lord's phase 1 and two skeleton guards), base-mine.png = ko_55 (three goblin raiders).
The items' pieces are cut from the screenshots' own wells and laid by the packing proposed (reading order, first place it fits). No API call.
  .venv/bin/python ArtPipeline/Archive/86-enemy-bags/mock_enemy_bags.py
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
FONT = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"

SQ, PITCH = 50, 52                 # a square and the step from one to the next (2px gaps)
TOP = 646                          # the first row of squares under the board's head
COLUMNS = [1063, 1253, 1443]       # where the enemy boards' first squares start (rows 1, 2, 3)
CLEAN = (1640, 1820)               # an empty stretch of the board panel to cover the old wells with

OUTLINE = (22, 12, 12); SQUARE = (13, 13, 13); GAP = (54, 54, 58); BEVEL = (110, 104, 96)
STONE_RED = (68, 34, 28)           # the enemy's well rim today (UiPalette.EnemyRim)
LEATHER = (58, 44, 32)             # the mercenaries' leather pack


def font(size):
    return ImageFont.truetype(str(FONT), size)


def piece(image, x0, gx, gy, w, h):
    """Cuts an item's piece (its squares) from a board whose squares start at x0."""
    left, top = x0 + gx * PITCH, TOP + gy * PITCH
    return image.crop((left, top, left + (w - 1) * PITCH + SQ, top + (h - 1) * PITCH + SQ))


def pack(sizes, width=3, min_height=3, max_height=8):
    """Reading order, the first place each item fits; a row more when one fits nowhere (the bag grows down, 3 wide like the frame)."""
    height = min_height
    while True:
        taken = set()
        places = []
        for w, h in sizes:
            spot = next(((x, y) for y in range(height - h + 1) for x in range(width - w + 1)
                         if all((x + dx, y + dy) not in taken for dx in range(w) for dy in range(h))), None)
            if spot is None:
                break
            places.append(spot)
            taken |= {(spot[0] + dx, spot[1] + dy) for dx in range(w) for dy in range(h)}
        if len(places) == len(sizes):
            return places, height
        height += 1
        assert height <= max_height


def clear(image, x0, rows):
    left, right = x0 - 10, x0 + 2 * PITCH + SQ + 10
    patch = image.crop((CLEAN[0], 640, CLEAN[0] + right - left, TOP + rows * PITCH + 14))
    image.paste(patch, (left, 640))


def well(image, x0, rows, rim):
    draw = ImageDraw.Draw(image)
    right = x0 + 2 * PITCH + SQ
    bottom = TOP + (rows - 1) * PITCH + SQ
    draw.rectangle((x0 - 6, TOP - 3, right + 5, bottom + 5), fill=OUTLINE)
    draw.rectangle((x0 - 5, TOP - 2, right + 2, bottom + 2), fill=rim)
    draw.line((x0 - 5, bottom + 3, right + 3, bottom + 3), fill=BEVEL, width=2)
    draw.line((right + 3, TOP - 2, right + 3, bottom + 3), fill=BEVEL, width=2)
    draw.rectangle((x0, TOP, right, bottom), fill=GAP)
    for gy in range(rows):
        for gx in range(3):
            left, top = x0 + gx * PITCH, TOP + gy * PITCH
            draw.rectangle((left, top, left + SQ - 1, top + SQ - 1), fill=SQUARE)


def board(image, column, pieces, rim):
    """pieces: [(cut image, w, h)] in the enemy's item order."""
    places, rows = pack([(w, h) for _, w, h in pieces])
    x0 = COLUMNS[column]
    clear(image, x0, max(rows, 3))
    well(image, x0, rows, rim)
    for (cut, _, _), (gx, gy) in zip(pieces, places):
        image.paste(cut, (x0 + gx * PITCH, TOP + gy * PITCH))
    return rows


def caption(image, text):
    draw = ImageDraw.Draw(image)
    draw.rectangle((0, 1016, 1920, 1080), fill=(0, 0, 0))
    draw.text((24, 1030), text, font=font(30), fill=(0xEB, 0xEB, 0xE6))


def main():
    abyss = Image.open(HERE / "base-abyss.png").convert("RGB")
    mine = Image.open(HERE / "base-mine.png").convert("RGB")

    # The pieces as the screenshots draw them (each in a well of its own today).
    cleaver = (piece(abyss, COLUMNS[0], 0, 0, 2, 1), 2, 1)
    howl = (piece(abyss, COLUMNS[0], 0, 1, 2, 1), 2, 1)
    blade = (piece(abyss, COLUMNS[1], 0, 0, 2, 1), 2, 1)
    buckler = (piece(abyss, COLUMNS[1], 0, 1, 1, 1), 1, 1)
    raider = (piece(mine, COLUMNS[0], 0, 0, 2, 1), 2, 1)

    def abyss_with(rim):
        image = abyss.copy()
        board(image, 0, [cleaver, howl], rim)
        board(image, 1, [blade, buckler], rim)
        board(image, 2, [blade, buckler], rim)
        return image

    def mine_with(rim):
        image = mine.copy()
        for column in range(3):
            board(image, column, [raider], rim)
        return image

    a = abyss_with(STONE_RED)
    caption(a, "안 A (권장) — 적도 3×3 가방: 붉은 돌 테, 빈 칸은 검게. 아이템은 읽는 차례로 처음 들어가는 자리에 (방패는 칼 옆)")
    a.save(HERE / "mock-abyss-A.png")
    b = abyss_with(LEATHER)
    caption(b, "안 B — 용병과 같은 가죽 테 (적과 아군의 보드가 같은 모습)")
    b.save(HERE / "mock-abyss-B.png")
    m = mine_with(STONE_RED)
    caption(m, "안 A — 광산의 고블린 약탈자 셋: 2×1 하나씩, 나머지 일곱 칸은 비어 있음")
    m.save(HERE / "mock-mine-A.png")

    # Growing (no enemy needs it with today's data: the most is the Abyss Lord's three 2x1 at phases 2 and 3, which fit).
    grown = abyss.copy()
    rows = board(grown, 0, [cleaver, howl, blade, buckler, buckler, blade], STONE_RED)
    caption(grown, f"자동 확대의 예 (지금 데이터에는 없음) — 아이템 여섯(2×1 넷·1×1 둘)이 3×3에 안 들어가 3×{rows}로: 폭 3은 그대로, 아래로 늘어남")
    grown.save(HERE / "mock-grow.png")

    # Side by side: the enemy side of the board panel.
    region = (1040, 620, 1630, 870)
    panels = [("지금", abyss), ("안 A (권장): 붉은 돌 테", a), ("안 B: 가죽 테", b), ("확대의 예: 3×4", grown)]
    w, h = region[2] - region[0], region[3] - region[1]
    sheet = Image.new("RGB", (2 * w + 60, 2 * (h + 50) + 20), (0x16, 0x16, 0x1C))
    draw = ImageDraw.Draw(sheet)
    for i, (title, source) in enumerate(panels):
        x = 20 + (i % 2) * (w + 20)
        y = 10 + (i // 2) * (h + 50)
        draw.text((x, y), title, font=font(26), fill=(0xEB, 0xEB, 0xE6))
        sheet.paste(source.crop(region), (x, y + 40))
    sheet.save(HERE / "compare.png")


if __name__ == "__main__":
    main()
