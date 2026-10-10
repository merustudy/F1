# -*- coding: utf-8 -*-
"""Round 52 (the user, 2026-10-10: "인벤토리는 용병 그림 있는 칸에 뜨게 하는걸로(편의성 향상)", "인벤토리 단축키 i 적용", review): the
inventory window moved from the map's side (today: it covers the map) to the mercenaries' stage over their figures, next to the boards.
On today's screenshots (game/, 2026-10-10): the node map, the shop and the battle screen after a win. Drawn in today's look (round 49,
Diablo II): the stone window, the 10x3 grid, the coins, Diablo's tooltip. No API call.
  .venv/bin/python ArtPipeline/Archive/52-inventory-place/mock_inventory_place.py
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent / "49-inventory-style"))
import mock_inventory_style as S  # noqa: E402
import mock_diablo_v2 as V2  # noqa: E402
from mock_inventory_style import s, B, D, cell, span, text, font, line, comp, PAD  # noqa: E402

LOOK = V2.DiabloV2()
GAME = HERE / "game"
STAGE = (8, 92, 954, 612)                          # the mercenaries' stage on the left, above the board panel (620)
MAP = (972, 92, 1908, 828)                         # the right side: the map (today's inventory window covers it)
GOLD, BONE, WHITE, BLUE, GREY, VIOLET = (199, 179, 119), (205, 192, 160), (238, 238, 238), (112, 112, 255), (150, 142, 124), (0xB4, 0x8C, 0xE6)


def F(px): return ImageFont.truetype(str(S.PRET), px)


def shot(name):
    return Image.open(GAME / f"{name}.png").convert("RGB").resize((s(1920), s(1080)), Image.LANCZOS).convert("RGBA")


def tooltip(img, x, y, w):
    """Diablo's tooltip of the held rat claw (today's lines), its top-left at (x, y)."""
    rows = [("쥐 발톱 · 등급 8", WHITE, 16), ("무기 장비", WHITE, 14), ("크기 2×1 · 근접", WHITE, 14), ("쿨다운 2.0초", WHITE, 14),
            ("앞에서 2번째 자리까지만 발동", WHITE, 14), ("맨 앞 적에게 피해 4", BLUE, 14), ("전투마다 피로 +1", VIOLET, 14),
            ("R · 우클릭 · 휠: 돌리기", GREY, 12), ("Esc: 내려놓기", GREY, 12)]
    h = 16 + sum(px + 9 for _, _, px in rows)
    d = D(img); d.rectangle(B(x, y, x + w, y + h), fill=(0, 0, 0, 220), outline=(64, 64, 64))
    yy = y + 12
    for t, col, px in rows:
        text(d, (x + w / 2, yy + px / 2), t, font(px), col, "mm"); yy += px + 9
    return h


def window(img, q, box):
    """The inventory window over a box of the screen: the stone, the title and count, the grid (the rat claw at its first room, picked),
    the coins, the held item's tooltip beside the grid and the hint at the bottom."""
    x0, y0, x1, y1 = box
    LOOK.stone(img, box, base=(56, 54, 51))
    d = D(img); cx = (x0 + x1) / 2
    d.rectangle(B(cx - 130, y0 + 16, cx + 130, y0 + 52), fill=(14, 13, 12))
    line(d, [(cx - 130, y0 + 52), (cx + 130, y0 + 52), (cx + 130, y0 + 16)], (110, 104, 96), 1.5)
    text(d, (cx, y0 + 34), "인벤토리", font(22), GOLD, "mm")
    text(d, (x1 - 26, y0 + 34), "2 / 30칸", font(15), BONE, "rm")
    gx0, gy0 = x0 + 34, y0 + 78
    LOOK.well(img, gx0, gy0, 10, 3, q=q, rim=6); LOOK.squares(img, gx0, gy0, 10, 3, q=q)
    claw = dict(id="rat_bite", x=0, y=0, w=2, h=1, tier="common", sel=True)
    X, Y = cell(gx0, gy0, 0, 0, q); comp(img, LOOK.piece(claw, q=q), (s(X - PAD), s(Y - PAD)))
    by = gy0 + span(3, q) + 16
    d = D(img)
    d.rectangle(B(gx0, by, gx0 + 140, by + 30), fill=(14, 13, 12))
    d.ellipse(B(gx0 + 9, by + 6, gx0 + 27, by + 24), fill=(196, 160, 60), outline=(90, 70, 20))
    text(d, (gx0 + 130, by + 15), "4", font(17), GOLD, "rm")
    tx = gx0 + span(10, q) + 22
    tooltip(img, tx, gy0 - 6, x1 - 26 - tx)
    text(D(img), (cx, y1 - 22), "아이템을 누르면 손에 듭니다 · 아래 보드나 이 격자의 빈 칸을 누르면 그 자리에 놓입니다 · I: 열고 닫기", font(14), GREY, "mm")


def shade(img, box, alpha=150):
    lay = Image.new("RGBA", img.size, (0, 0, 0, 0)); ImageDraw.Draw(lay).rectangle(B(*box), fill=(0, 0, 0, alpha)); comp(img, lay, (0, 0))


def node_map(q, height):
    """The node map with the inventory open: today's boards and held claw (ko_17), the map back on the right (ko_35), the window on the stage."""
    img = shot("ko_17_map_inventory_selected")
    right = shot("ko_35_map_tiers").crop(B(*MAP)); img.paste(right, (s(MAP[0]), s(MAP[1])))
    box = (STAGE[0], STAGE[1], STAGE[2], STAGE[1] + height)
    shade(img, STAGE, 90)
    window(img, q, box)
    return img


def over(name, q, height):
    img = shot(name)
    box = (STAGE[0], STAGE[1], STAGE[2], STAGE[1] + height)
    shade(img, (STAGE[0], STAGE[1], STAGE[2], STAGE[3]), 90)
    window(img, q, box)
    return img


def half(img): return img.resize((960, 540), Image.LANCZOS).convert("RGB")


def main():
    now = half(shot("ko_17_map_inventory_selected"))
    a = node_map(66, 392)
    b = node_map(50, 330)
    a.resize((1920, 1080), Image.LANCZOS).convert("RGB").save(HERE / "mock-place-A-map.png")
    S.sheet([("지금: 인벤토리 창이 오른쪽 지도를 덮는다", now),
             ("안 A (권장): 용병 무대 위, 칸 66 그대로 · 지도가 보인다", half(a)),
             ("안 B: 용병 무대 위, 칸 50(보드와 같은 크기) · 창이 낮다", half(b)),
             ("안 A: 상점 창과 함께 열림 (지금은 둘이 같은 자리)", half(over("ko_43_shop_pick", 66, 392)))],
            2, HERE / "mock-inventory-place.png", title="인벤토리 창의 자리 — 용병 그림이 있는 무대 위로 (Round 52)", label=19,
            footer=["창은 무대(위 92 ~ 보드 패널 620) 위에 뜨고 그 아래 무대는 조금 어두워진다. 보드 바로 위라 아이템을 옮기는 거리가 짧고, 지도와 상점 창이 가려지지 않는다.",
                    "툴팁은 격자 오른쪽에 붙는다. 버튼 '인벤토리 보기'와 단축키 I가 열고 닫는다. 그린 것은 창뿐이다(보드·패널·지도는 지금 게임). 호출 없음."])
    S.sheet([("이긴 뒤 전투 화면: 지금 (인벤토리 창이 없다)", half(shot("ko_07_loot_picked"))),
             ("권장: 같은 자리에 인벤토리 창 + I (바닥의 전리품은 보인다)", half(over("ko_07_loot_picked", 66, 392)))],
            2, HERE / "mock-inventory-after-win.png", title="이긴 뒤 전투 화면의 인벤토리 (Round 52, 권장 추가안)", label=19,
            footer=["Round 47은 이긴 뒤 화면에 인벤토리 창을 두지 않았다. 창이 아군 쪽 무대에 뜨면 적 쪽 바닥의 전리품을 가리지 않으므로 같은 창과 I를 둘 수 있다."])


if __name__ == "__main__":
    main()
