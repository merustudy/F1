# -*- coding: utf-8 -*-
"""Round 56 (the user, 2026-10-10: "상점 칸을 10*8로 확장시키고, 가장 오른쪽 줄에는 포션을 세로로 비치(포션은 상점 방문시 최소 1개는 나오도록) / 9번째 줄은
비워두기 / 나머지들은 큰 물건부터 배치", review): the merchant's grid 10 across and 8 down, the goods biggest first in its first eight columns,
the ninth column empty and the potions down the tenth. Drawn over today's shop (game/, 2026-10-10 s21) in its own look. No API call.
  .venv/bin/python ArtPipeline/Archive/56-merchant-grid/mock_merchant_grid.py
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
GOLD, BONE, WHITE, BLUE, GREY = (199, 179, 119), (205, 192, 160), (238, 238, 238), (112, 112, 255), (150, 142, 124)
STONE, EDGE, PLATE = (57, 55, 57), (112, 106, 98), (14, 13, 12)
GOODS = [("halberd", 2, 3, 22), ("spear", 2, 2, 16), ("longbow", 2, 2, 16), ("dagger", 2, 1, 10), ("ember_flask", 2, 1, 12),
         ("herb_pouch", 2, 1, 10), ("ward_charm", 2, 1, 12), ("buckler", 1, 1, 12)]
POTS = [("healing_potion", 8), ("barrier_potion", 8)]


def F(px): return ImageFont.truetype(str(S.PRET), px)


def span(n): return n * SQ + (n - 1) * GAP


def fit(im, w, h):
    k = min(w / im.width, h / im.height)
    return im.resize((max(1, round(im.width * k)), max(1, round(im.height * k))), Image.LANCZOS)


def lay(goods, width):
    """The merchant's layout (MerchantLayout): the widest first, then the taller, in reading order."""
    order = sorted(range(len(goods)), key=lambda i: (-(goods[i][1] * goods[i][2]), -goods[i][2], i))
    taken, places = set(), {}
    for i in order:
        _, w, h, _ = goods[i]
        y = 0
        while i not in places:
            for x in range(0, width - w + 1):
                if all((x + dx, y + dy) not in taken for dx in range(w) for dy in range(h)):
                    places[i] = (x, y)
                    taken.update((x + dx, y + dy) for dx in range(w) for dy in range(h))
                    break
            y += 1
    return places


def piece(img, d, gx, gy, x, y, w, h, src, price):
    for i in range(w):
        for j in range(h):
            X, Y = gx + (x + i) * STEP, gy + (y + j) * STEP
            d.rectangle((X, Y, X + SQ - 1, Y + SQ - 1), fill=GROUND)
    ic = fit(Image.open(src).convert("RGBA"), span(w) - 10, span(h) - 10)
    X0, Y0 = gx + x * STEP, gy + y * STEP
    img.paste(ic, (X0 + (span(w) - ic.width) // 2, Y0 + (span(h) - ic.height) // 2), ic)
    d.text((X0 + span(w) - 4, Y0 + span(h) - 3), str(price), font=F(13), fill=GOLD, anchor="rd", stroke_width=2, stroke_fill=(0, 0, 0))


def window(cols, rows, goods_cols, potion_col, pots):
    img = Image.open(GAME / "ko_42_shop.png").convert("RGB")
    d = ImageDraw.Draw(img)
    grid_h = span(rows) + 12
    height = 108 + grid_h + 16 + 48 + 22
    x0 = 1010; y0 = round(459 - height / 2); x1 = x0 + 860; y1 = y0 + height
    shade = Image.new("RGBA", img.size, (0, 0, 0, 0)); ImageDraw.Draw(shade).rectangle((980, 100, 1900, 818), fill=(0, 0, 0, 120))
    img = Image.alpha_composite(img.convert("RGBA"), shade).convert("RGB"); d = ImageDraw.Draw(img)
    d.rectangle((x0, y0, x1, y1), fill=STONE, outline=EDGE, width=2)
    coins = Image.open(ROOT / "Assets/@Art/UI/Icon/node-shop.png").convert("RGBA") if (ROOT / "Assets/@Art/UI/Icon/node-shop.png").exists() else None
    d.text((x0 + 130, y0 + 40), "4층 · 상점", font=F(30), fill=GOLD, anchor="lm")
    d.text((x0 + 130, y0 + 80), "지역 코인으로 삽니다. 나가면 다음 층으로 갑니다.", font=F(18), fill=GREY, anchor="lm")
    d.text((x1 - 24, y0 + 44), "가진 코인  37", font=F(20), fill=GOLD, anchor="rm")
    gx, gy = x0 + 27, y0 + 108 + 6
    d.rectangle((gx - 6, gy - 6, gx + span(cols) + 5, gy + span(rows) + 5), fill=(34, 33, 31))
    d.rectangle((gx, gy, gx + span(cols) - 1, gy + span(rows) - 1), fill=LINE)
    for i in range(cols):
        for j in range(rows):
            d.rectangle((gx + i * STEP, gy + j * STEP, gx + i * STEP + SQ - 1, gy + j * STEP + SQ - 1), fill=EMPTY)
    places = lay(GOODS, goods_cols)
    for i, (gid, w, h, price) in enumerate(GOODS):
        x, y = places[i]
        piece(img, d, gx, gy, x, y, w, h, ART / f"{gid}.png", price)
    if potion_col is not None:
        for j, (pid, price) in enumerate(pots):
            piece(img, d, gx, gy, potion_col, j, 1, 1, POTIONS / f"{pid}.png", price)
    bx = gx + span(cols) + 6 + 16
    rows_ = [("미늘창 · 등급 8", WHITE, 16), ("무기 장비", WHITE, 13), ("크기 2×3", WHITE, 13), ("쿨다운 3.6초", WHITE, 13), ("맨 앞에서만 발동", WHITE, 13),
             ("앞의 적 3명에게 피해 6", BLUE, 13), ("전투마다 피로 +1", (180, 140, 230), 13), ("값 22 코인", GOLD, 15)]
    hh = 16 + sum(px + 9 for _, _, px in rows_)
    d.rectangle((bx, gy - 6, x1 - 22, gy - 6 + grid_h), fill=(10, 10, 10))
    yy = gy + 8
    for tx, col, px in rows_:
        d.text(((bx + x1 - 22) / 2, yy + px / 2), tx, font=F(px), fill=col, anchor="mm"); yy += px + 9
    by = y0 + 108 + grid_h + 16
    d.rectangle((x0 + 20, by, x0 + 270, by + 48), fill=(40, 42, 50), outline=(80, 80, 90))
    d.text((x0 + 145, by + 24), "새로고침  ● 3", font=F(22), fill=WHITE, anchor="mm")
    d.rectangle((x1 - 210, by, x1 - 20, by + 48), fill=(46, 92, 170))
    d.text((x1 - 115, by + 24), "나가기", font=F(22), fill=WHITE, anchor="mm")
    if potion_col is not None:
        px_ = gx + potion_col * STEP + SQ // 2
        d.text((px_, gy - 10), "포션", font=F(13), fill=GREY, anchor="md")
    return img


def main():
    now = Image.open(GAME / "ko_42_shop.png").convert("RGB").crop((972, 92, 1908, 826))
    one = window(10, 8, 8, 9, POTS[:1]).crop((972, 92, 1908, 826))
    two = window(10, 8, 8, 9, POTS).crop((972, 92, 1908, 826))
    S.sheet([("지금(21단계): 10×5, 포션도 물건 8개 안에", now.resize((702, 550), Image.LANCZOS)),
             ("안: 10×8 — 1~8열 물건(큰 것부터), 9열 빔, 10열 포션(이번엔 1개)", one.resize((702, 550), Image.LANCZOS)),
             ("같은 안: 포션 2개(종류마다)", two.resize((702, 550), Image.LANCZOS))],
            3, HERE / "mock-merchant-grid.png", title="상인의 격자 10×8과 포션 열 (Round 56)", label=18,
            footer=["물건 여덟은 오늘 실행의 것(미늘창·창·장궁·단검·불씨 플라스크·약초 주머니·수호 부적·버클러)을 큰 것부터 1~8열에. 9열은 비우고 10열 위에서부터 포션.",
                    "툴팁은 격자 오른쪽(가리킨 물건). 창은 격자만큼 높아진다(620). 그린 것은 창뿐이다. 호출 없음."])
    two.save(HERE / "mock-merchant-grid-full.png")


if __name__ == "__main__":
    main()
