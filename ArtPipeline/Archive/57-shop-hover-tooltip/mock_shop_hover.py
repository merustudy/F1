# -*- coding: utf-8 -*-
"""Round 57 (the user, 2026-10-10: "상점칸에서 상품 정보를 옆 UI로 주는게 아니라 디아블로2처럼 마우스를 아이템 위로 올렸을 경우 툴팁이 발생하게 검토"): the
merchant's goods described in Diablo II's tooltip over the good the pointer is on, in place of the box beside the grid. Drawn in the
shop's look (round 56: 10x8, goods in the first eight columns, the potions down the last) over today's map (game/, 2026-10-10 r56b).
No API call.
  .venv/bin/python ArtPipeline/Archive/57-shop-hover-tooltip/mock_shop_hover.py
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
sys.path.insert(0, str(HERE.parent / "49-inventory-style"))
import mock_inventory_style as S  # noqa: E402

GAME = HERE / "game"
ART, POTIONS = ROOT / "Assets/@Art/Item", ROOT / "Assets/@Art/Potion"
SQ, GAP = 50, 2
STEP = SQ + GAP
GROUND, LINE, EMPTY = (31, 38, 84), (56, 56, 61), (13, 13, 13)
GOLD, WHITE, BLUE, GREY, VIOLET, RED = (199, 179, 119), (238, 238, 238), (112, 112, 255), (150, 142, 124), (180, 140, 230), (210, 70, 60)
STONE, EDGE = (57, 55, 57), (112, 106, 98)
# today's goods (r56b): (id, x, y, w, h, price); the potion down the last column
GOODS = [("halberd", 0, 0, 2, 3, 22), ("longbow", 2, 0, 2, 2, 16), ("spear", 4, 0, 2, 2, 16), ("dagger", 6, 0, 2, 1, 10),
         ("herb_pouch", 4, 2, 2, 1, 10), ("ward_charm", 0, 3, 2, 1, 12)]
BAGS = [("belt_pouch", 6, 1, 2, 1, 8, (78, 58, 42)), ("leather_pouch", 2, 2, 3, 1, 12, (70, 74, 52))]
POTS = [("healing_potion", 9, 0, 8)]
SPEAR_LINES = [("창 · 등급 8", WHITE, 16), ("무기 장비 · 크기 2×2", WHITE, 13), ("쿨다운 3.2초", WHITE, 13), ("앞에서 2번째 자리까지만 발동", WHITE, 13),
               ("앞의 적 2명에게 피해 6", BLUE, 13), ("전투마다 피로 +1", VIOLET, 13), ("값 16 코인", GOLD, 15)]


def F(px): return ImageFont.truetype(str(S.PRET), px)


def span(n): return n * SQ + (n - 1) * GAP


def fit(im, w, h):
    k = min(w / im.width, h / im.height)
    return im.resize((max(1, round(im.width * k)), max(1, round(im.height * k))), Image.LANCZOS)


def window(img, x0, y0, width, info_box):
    """The shop's window (round 56) with or without the box beside the grid; returns the grid's top-left."""
    rows = 8
    grid_h = span(rows) + 12
    height = 108 + grid_h + 16 + 48 + 22
    x1, y1 = x0 + width, y0 + height
    d = ImageDraw.Draw(img)
    d.rectangle((x0, y0, x1, y1), fill=STONE, outline=EDGE, width=2)
    d.text((x0 + 30, y0 + 40), "4층 · 상점", font=F(30), fill=GOLD, anchor="lm")
    d.text((x0 + 30, y0 + 80), "지역 코인으로 삽니다. 나가면 다음 층으로 갑니다.", font=F(17), fill=GREY, anchor="lm")
    d.text((x1 - 24, y0 + 44), "가진 코인  37", font=F(20), fill=GOLD, anchor="rm")
    gx, gy = x0 + 27, y0 + 114
    d.rectangle((gx - 6, gy - 6, gx + span(10) + 5, gy + span(rows) + 5), fill=(34, 33, 31))
    d.rectangle((gx, gy, gx + span(10) - 1, gy + span(rows) - 1), fill=LINE)
    for i in range(10):
        for j in range(rows):
            d.rectangle((gx + i * STEP, gy + j * STEP, gx + i * STEP + SQ - 1, gy + j * STEP + SQ - 1), fill=EMPTY)
    for gid, x, y, w, h, price in GOODS:
        piece(img, d, gx, gy, x, y, w, h, ART / f"{gid}.png", price)
    for gid, x, y, w, h, price, rim in BAGS:
        X0, Y0 = gx + x * STEP, gy + y * STEP
        d.rectangle((X0, Y0, X0 + span(w) - 1, Y0 + span(h) - 1), fill=rim)
        for i in range(w):
            d.rectangle((X0 + 5 + i * (span(w) - 8) // w, Y0 + 5, X0 + 5 + (i + 1) * (span(w) - 8) // w - 3, Y0 + span(h) - 6), fill=EMPTY)
        d.text((X0 + span(w) - 4, Y0 + span(h) - 3), str(price), font=F(13), fill=GOLD, anchor="rd", stroke_width=2, stroke_fill=(0, 0, 0))
    for pid, x, y, price in POTS:
        piece(img, d, gx, gy, x, y, 1, 1, POTIONS / f"{pid}.png", price)
    if info_box:
        bx = gx + span(10) + 6 + 16
        d.rectangle((bx, gy - 6, x1 - 22, gy - 6 + grid_h), fill=(10, 10, 10))
    by = y0 + 114 - 6 + grid_h + 16
    d.rectangle((x0 + 20, by, x0 + 270, by + 48), fill=(40, 42, 50), outline=(80, 80, 90))
    d.text((x0 + 145, by + 24), "새로고침  ● 3", font=F(22), fill=WHITE, anchor="mm")
    d.rectangle((x1 - 210, by, x1 - 20, by + 48), fill=(46, 92, 170))
    d.text((x1 - 115, by + 24), "나가기", font=F(22), fill=WHITE, anchor="mm")
    return gx, gy


def piece(img, d, gx, gy, x, y, w, h, src, price):
    for i in range(w):
        for j in range(h):
            X, Y = gx + (x + i) * STEP, gy + (y + j) * STEP
            d.rectangle((X, Y, X + SQ - 1, Y + SQ - 1), fill=GROUND)
    ic = fit(Image.open(src).convert("RGBA"), span(w) - 10, span(h) - 10)
    X0, Y0 = gx + x * STEP, gy + y * STEP
    img.paste(ic, (X0 + (span(w) - ic.width) // 2, Y0 + (span(h) - ic.height) // 2), ic)
    d.text((X0 + span(w) - 4, Y0 + span(h) - 3), str(price), font=F(13), fill=GOLD, anchor="rd", stroke_width=2, stroke_fill=(0, 0, 0))


def tooltip(img, cx, top_of_piece, bottom_of_piece, lines, bounds):
    """Diablo's tooltip over the piece the pointer is on: centred on it, above it (below when there is no room), kept inside the bounds."""
    w = 16 + max(F(px).getbbox(t)[2] for t, _, px in lines) + 16
    h = 14 + sum(px + 8 for _, _, px in lines) + 6
    left = min(max(cx - w / 2, bounds[0]), bounds[2] - w)
    top = top_of_piece - 8 - h
    if top < bounds[1]:
        top = bottom_of_piece + 8
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    ImageDraw.Draw(layer).rectangle((left, top, left + w, top + h), fill=(0, 0, 0, 220), outline=(64, 64, 64))
    img.paste(Image.alpha_composite(img.convert("RGBA"), layer).convert("RGB"))
    d = ImageDraw.Draw(img)
    y = top + 12
    for t, col, px in lines:
        d.text((left + w / 2, y + px / 2), t, font=F(px), fill=col, anchor="mm"); y += px + 8


def pointer(img, x, y):
    d = ImageDraw.Draw(img)
    pts = [(x, y), (x, y + 24), (x + 6, y + 18), (x + 11, y + 28), (x + 15, y + 26), (x + 10, y + 16), (x + 17, y + 16)]
    d.polygon(pts, fill=(255, 255, 255), outline=(0, 0, 0))


def base():
    img = Image.open(GAME / "ko_41_map_shop.png").convert("RGB")    # the map without today's window: the window is drawn anew
    shade = Image.new("RGBA", img.size, (0, 0, 0, 0)); ImageDraw.Draw(shade).rectangle((980, 100, 1900, 818), fill=(0, 0, 0, 120))
    return Image.alpha_composite(img.convert("RGBA"), shade).convert("RGB")


def now():
    """Today's shop (round 56): the box beside the grid shows the good the pointer is on."""
    return Image.open(GAME / "ko_42_shop.png").convert("RGB")


def hovered(narrow):
    img = base()
    width = 27 + span(10) + 6 + 27 + 4 if narrow else 860
    x0 = round(1440 - width / 2)
    y0 = round(459 - (108 + span(8) + 12 + 16 + 48 + 22) / 2)
    gx, gy = window(img, x0, y0, width, info_box=False)
    sx, sy = gx + 4 * STEP, gy + 0 * STEP                     # the spear (2x2) at (4, 0)
    tooltip(img, sx + span(2) / 2, sy, sy + span(2), SPEAR_LINES, (980, 100, 1900, 818))
    pointer(img, sx + 52, sy + 40)
    return img


def hovered_low():
    """The same over a good on the top row with no room above in the window: the tooltip goes over the map's edge or below."""
    img = base()
    width = 27 + span(10) + 6 + 27 + 4
    x0 = round(1440 - width / 2)
    y0 = round(459 - (108 + span(8) + 12 + 16 + 48 + 22) / 2)
    gx, gy = window(img, x0, y0, width, info_box=False)
    hx, hy = gx + 4 * STEP, gy + 2 * STEP                     # the herb pouch (2x1) at (4, 2)
    lines = [("약초 주머니 · 등급 8", WHITE, 16), ("지원 아이템 · 크기 2×1", WHITE, 13), ("쿨다운 5.0초", WHITE, 13), ("HP가 가장 낮은 아군 HP 12 회복", BLUE, 13),
             ("전투마다 피로 +1", VIOLET, 13), ("값 10 코인", GOLD, 15)]
    tooltip(img, hx + span(2) / 2, hy, hy + SQ, lines, (980, 100, 1900, 818))
    pointer(img, hx + 50, hy + 26)
    return img


def crop(img): return img.crop((972, 92, 1908, 826)).resize((702, 550), Image.LANCZOS)


def main():
    S.sheet([("지금(Round 56): 가리킨 물건을 격자 옆 상자에", crop(now())),
             ("안 A (권장): 마우스를 올린 물건 위에 툴팁, 옆 상자를 빼고 창을 좁힘", crop(hovered(True))),
             ("안 B: 같은 툴팁, 창 너비는 그대로(오른쪽이 빔)", crop(hovered(False)))],
            3, HERE / "mock-shop-hover.png", title="상점의 상품 정보 — 디아블로 2처럼 마우스를 올리면 툴팁 (Round 57)", label=18,
            footer=["툴팁은 디아블로의 검은 상자(가운데 정렬: 이름·분류와 크기·쿨다운·자리·효과·피로·값)이고 가리킨 물건의 위에, 위에 자리가 없으면 아래에 뜬다. 마우스가 떠나면 사라진다.",
                    "그린 것은 상점 창과 툴팁, 화살표다(지도는 오늘의 게임). 호출 없음."])
    hovered(True).save(HERE / "mock-shop-hover-A-full.png")
    hovered_low().crop((972, 92, 1908, 826)).save(HERE / "mock-shop-hover-A-middle.png")


if __name__ == "__main__":
    main()
