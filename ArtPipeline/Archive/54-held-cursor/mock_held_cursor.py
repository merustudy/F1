# -*- coding: utf-8 -*-
"""Round 54 (the user, 2026-10-10: "용병 아이템 보관함에서 아이템 클릭시 기존 아이템 및 아이템 배경색은 제거되고 아이템 아이콘이 마우스 아이콘 대신
마우스 아이콘처럼 따라 다니도록, 아이템 다른 칸에 딱 맞는 경우 그 칸에 아이템창 초록색 배경색만 나오게 (디아블로 2 스타일)", "우클릭으로 돌리면
아이템 아이콘도 회전", review): the held item on the pointer instead of the pointer, its place left empty, only the colour where it would land.
Drawn on today's screenshots (game/, 2026-10-10 r53) at their own pixels: squares 50, gap 2. No API call.
  .venv/bin/python ArtPipeline/Archive/54-held-cursor/mock_held_cursor.py
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
sys.path.insert(0, str(HERE.parent / "49-inventory-style"))
import mock_inventory_style as S  # noqa: E402

GAME = HERE / "game"
SQ, GAP = 50, 2
STEP = SQ + GAP
BOARDS = [(133, 646), (323, 646), (513, 646), (703, 646)]   # the four boards' first square (1920x1080)
INV = (38, 166)                                             # the inventory's first square
EMPTY, LINE = (11, 11, 11), (52, 52, 56)                    # a board's empty square and line (under the panel's dark)
GREEN = (0x1A, 0x96, 0x30)
RED = (0xB0, 0x1E, 0x1E)
SWORD = Image.open(ROOT / "Assets/@Art/Item/longsword.png").convert("RGBA")


def F(px): return ImageFont.truetype(str(S.PRET), px)


def lin(c):
    c /= 255
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def srgb(x):
    v = 12.92 * x if x <= 0.0031308 else 1.055 * x ** (1 / 2.4) - 0.055
    return max(0, min(255, round(v * 255)))


def tint(img, box, colour, alpha=0.45):
    """The ghost's see-through colour over a box, blended as the game blends (the linear space); one block, the lines under it covered black."""
    x0, y0, x1, y1 = box
    px = img.load()
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            under = px[x, y]
            if abs(under[0] - LINE[0]) <= 6 and abs(under[2] - LINE[2]) <= 7 and abs(under[0] - under[1]) <= 3:
                under = EMPTY                      # a line under the block: the floor's black (round 53)
            px[x, y] = tuple(srgb(alpha * lin(c) + (1 - alpha) * lin(u)) for c, u in zip(colour, under))


def box(origin, x, y, w, h):
    ox, oy = origin
    return ox + x * STEP, oy + y * STEP, ox + x * STEP + w * STEP - GAP - 1, oy + y * STEP + h * STEP - GAP - 1


def empty(img, origin, x, y, w, h):
    """An item lifted off: its squares empty again, the lines between them."""
    d = ImageDraw.Draw(img)
    x0, y0, x1, y1 = box(origin, x, y, w, h)
    d.rectangle((x0, y0, x1, y1), fill=LINE)
    for i in range(w):
        for j in range(h):
            d.rectangle((x0 + i * STEP, y0 + j * STEP, x0 + i * STEP + SQ - 1, y0 + j * STEP + SQ - 1), fill=EMPTY)


def held(img, cx, cy, w, h, turns=0, anchor="centre"):
    """The held sword on the pointer: its icon the size of its squares (no blue), turned as held; the pointer at its centre (D2) or at the
    centre of its top-left square. A small ring marks where the pointer is (the game hides the pointer while something is held)."""
    tw, th = (h, w) if turns % 2 else (w, h)
    pw, ph = tw * STEP - GAP, th * STEP - GAP
    art = SWORD.rotate(-90 * turns, expand=True)
    k = min((pw - 10) / art.width, (ph - 10) / art.height)
    art = art.resize((round(art.width * k), round(art.height * k)), Image.LANCZOS)
    if anchor == "centre":
        left, top = cx - pw / 2, cy - ph / 2
    else:
        left, top = cx - SQ / 2, cy - SQ / 2
    img.paste(art, (round(left + (pw - art.width) / 2), round(top + (ph - art.height) / 2)), art)
    d = ImageDraw.Draw(img)
    d.ellipse((cx - 5, cy - 5, cx + 5, cy + 5), outline=(255, 255, 255), width=2)
    d.ellipse((cx - 1, cy - 1, cx + 1, cy + 1), fill=(255, 255, 255))


def arrow(img, x, y):
    """Today's pointer: the system arrow."""
    d = ImageDraw.Draw(img)
    pts = [(x, y), (x, y + 26), (x + 7, y + 20), (x + 12, y + 31), (x + 16, y + 29), (x + 11, y + 18), (x + 19, y + 18)]
    d.polygon(pts, fill=(255, 255, 255), outline=(0, 0, 0))


def base():
    img = Image.open(GAME / "ko_49_map_inventory_open.png").convert("RGB")
    empty(img, BOARDS[3], 0, 0, 3, 1)       # Roen's longsword picked up: its place empty
    # the held longsword's tooltip beside the grid, as today's game shows it while held (ko_50)
    tip = (578, 152, 931, 343)
    img.paste(Image.open(GAME / "ko_50_map_inventory_ghost.png").convert("RGB").crop(tip), tip[:2])
    return img


def now():
    img = Image.open(GAME / "ko_50_map_inventory_ghost.png").convert("RGB")
    x0, y0, x1, y1 = box(INV, 0, 1, 3, 1)
    arrow(img, x0 + 25, y0 + 25)
    return img


def fits_board():
    img = base()
    b = box(BOARDS[2], 0, 1, 3, 1)          # Kai's empty row 1
    tint(img, b, GREEN)
    held(img, (b[0] + b[2]) // 2 + 9, (b[1] + b[3]) // 2 - 7, 3, 1)
    return img


def fits_inventory():
    img = base()
    b = box(INV, 3, 1, 3, 1)
    tint(img, b, GREEN)
    held(img, (b[0] + b[2]) // 2 - 11, (b[1] + b[3]) // 2 + 6, 3, 1)
    return img


def refused():
    img = base()
    b = box(INV, 1, 0, 3, 1)                # over the claw: the inventory takes no swap
    tint(img, b, RED)
    held(img, (b[0] + b[2]) // 2 + 4, (b[1] + b[3]) // 2 + 3, 3, 1)
    return img


def turned():
    img = base()
    b = box(INV, 6, 0, 1, 3)
    tint(img, b, GREEN)
    held(img, (b[0] + b[2]) // 2 + 5, (b[1] + b[3]) // 2 - 8, 3, 1, turns=1)
    return img


def top_left():
    img = base()
    b = box(INV, 3, 1, 3, 1)
    tint(img, b, GREEN)
    held(img, b[0] + 25 + 6, b[1] + 25 - 4, 3, 1, anchor="top-left")
    return img


def crop(img):
    return img.crop((0, 92, 960, 812)).resize((720, 540), Image.LANCZOS)


def main():
    S.sheet([("지금: 원래 자리에 밝은 남색이 남고, 그림자(초록+아이콘 88%)가 칸을 따라 감", crop(now())),
             ("안 1 (권장): 집으면 원래 자리가 빔, 아이콘이 마우스 자리(가운데)", crop(fits_board())),
             ("안 1: 인벤토리 위 — 들어갈 칸에 초록만, 아이콘은 마우스에", crop(fits_inventory())),
             ("안 1: 들어갈 수 없는 자리 — 빨강만", crop(refused())),
             ("안 1: 우클릭·휠·R로 돌림 — 마우스의 아이콘도 돎(1×3)", crop(turned())),
             ("안 2: 마우스가 아이템의 왼쪽 위 칸에(지금의 칸 규칙 그대로)", crop(top_left()))],
            3, HERE / "mock-held-cursor.png", title="든 아이템이 마우스 대신 따라다님 — 디아블로 2 방식 (Round 54)", label=17,
            footer=["흰 고리는 마우스의 자리를 보이려고 그린 것이다(게임에서는 든 동안 마우스를 숨기고 아이콘만 보인다). 집은 롱소드는 로언의 보드에서 사라진다.",
                    "그린 것은 비운 자리, 초록·빨강 칠, 마우스의 아이콘뿐이다(나머지는 지금 게임). 호출 없음."])


if __name__ == "__main__":
    main()
