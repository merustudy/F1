# -*- coding: utf-8 -*-
"""Round 51, the stars as Backpack Battles shows them (the user, 2026-10-10: "별 표시 위치 및 방식은 백팩 배틀즈 조사 검토 후 그대로 적용
검토 보고"). What the research found (README "별의 조사"): a star is a square of the item's grid around its own squares (the
whetstone: star, square, star), it shows around the item when the item is placed or held, a held item shows whether it would
light the stars, a star lights up when an item that counts lies on its square, and an item fills one star of another item.
Here that is laid on today's look: the whetstone's stars on the square above and the square below it, shown while the
whetstone is held or the pointer is on it, gold when a melee weapon lies there, hollow otherwise; the bonus in the cards.
The whetstone is a drawn shape. No API call.
  .venv/bin/python ArtPipeline/Archive/51-claw-axe-whetstone/mock_stars_bb.py
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import mock_axe_whetstone as A  # noqa: E402  (registers the icons and the drawn whetstone)
from mock_axe_whetstone import S, it, board, card, tile, sheet, plus, GOLD, WHITE, BLUE, GREY  # noqa: E402
from mock_inventory_style import s, B, D, cell, span, text, font, star, SQ, PAD, comp  # noqa: E402

HOLLOW_FILL, HOLLOW_LINE = (58, 56, 52), (150, 146, 136)


def star_cells(w):
    """A whetstone's star squares: above and below it (left and right once turned)."""
    return [(w["x"] - 1, w["y"]), (w["x"] + 1, w["y"])] if w["rot"] else [(w["x"], w["y"] - 1), (w["x"], w["y"] + 1)]


def on_bags(gx, gy, bags):
    return any(b["x"] <= gx < b["x"] + b["w"] and b["y"] <= gy < b["y"] + b["h"] for b in bags)


def cell_stars(img, x0, y0, items, w, bags=(A.PACK,)):
    """The stars of one whetstone in their squares, over whatever lies there: lit gold where a melee weapon lies, hollow elsewhere."""
    for gx, gy in star_cells(w):
        if not on_bags(gx, gy, bags): continue
        o = A.owner([i for i in items if i is not w], gx, gy)
        lit = o is not None and o["id"] in A.MELEE
        cx, cy = cell(x0, y0, gx, gy); cx += SQ / 2; cy += SQ / 2
        if lit:
            halo = Image.new("RGBA", img.size, (0, 0, 0, 0)); ImageDraw.Draw(halo).ellipse(B(cx - 20, cy - 20, cx + 20, cy + 20), fill=(255, 214, 110, 110))
            comp(img, halo.filter(ImageFilter.GaussianBlur(s(7))), (0, 0))
        d = D(img)
        star(d, cx, cy, 14.5, (0, 0, 0))
        star(d, cx, cy, 12, GOLD if lit else HOLLOW_LINE)
        if not lit: star(d, cx, cy, 8.4, HOLLOW_FILL)


def whetstone_of(items): return next(i for i in items if i["id"] == "whetstone")


def scene(items, show=True, ghost=None, pointer=None, x0=73, y0=34):
    def draw(im):
        A.LOOK.board(im, x0, y0, dict(num=1, name="", bags=[A.PACK], items=items), ghost=ghost)
        if show: cell_stars(im, x0, y0, items + ([ghost] if ghost else []), ghost or whetstone_of(items))
        if pointer:
            X, Y = cell(x0, y0, *pointer); S.cursor(im, X + 30, Y + 30)
    return draw


def weapon_card(im):
    d = D(im); x0, y0, w, h = 20, 18, 260, 190
    d.rectangle(B(x0, y0, x0 + w, y0 + h), fill=(0, 0, 0, 228), outline=(70, 70, 70))
    rows = [("롱소드 · 등급 10", font(17), WHITE), ("무기 장비 · 3×1 · 근접", font(13), GREY), ("", None, None),
            ("쿨다운 3.0초", font(14), BLUE), ("앞에서 2번째 자리까지만 발동", font(14), BLUE), ("맨 앞 적에게 피해 12", font(14), BLUE),
            ("(★ 숫돌 +1)", font(13), GOLD), ("", None, None), ("기본 무기 · 피로 없음", font(12), GREY)]
    y = y0 + 16
    for t, f, c in rows:
        if t: text(d, (x0 + w / 2, y), t, f, c, "mm")
        y += 19 if t else 8


def bb_card(im):
    d = D(im); x0, y0, w, h = 20, 18, 260, 190
    d.rectangle(B(x0, y0, x0 + w, y0 + h), fill=(0, 0, 0, 228), outline=(70, 70, 70))
    rows = [("숫돌 · 등급 8", font(17), WHITE), ("기타 아이템 · 1×1", font(13), GREY), ("", None, None)]
    y = y0 + 16
    for t, f, c in rows:
        if t: text(d, (x0 + w / 2, y), t, f, c, "mm")
        y += 19 if t else 8
    star(d, x0 + 40, y + 2, 7.5, (0, 0, 0)); star(d, x0 + 40, y + 2, 6, GOLD)
    text(d, (x0 + 52, y + 2), "에 놓인 근접 무기: 피해 +1", font(14), BLUE, "lm"); y += 26
    text(d, (x0 + w / 2, y), "걸린 ★ 1/2 — 롱소드", font(14), WHITE, "mm"); y += 30
    text(d, (x0 + w / 2, y), "발동하지 않음 · 피로 없음", font(12), GREY, "mm")


def main():
    ls = it("longsword", 0, 0, 3, 1)
    base = [ls, it("whetstone", 0, 1, 1, 1), it("dagger", 1, 1, 2, 1), it("herb_pouch", 0, 2, 2, 1)]
    tiles = [
        ("앞 안: 변의 ★ · 늘 보임 · +1", lambda im: (A.board(im, 73, 34, base), plus(im, 73, 34, ls))),
        ("1 평소: ★ 숨김", scene(base, show=False)),
        ("2 숫돌에 마우스: 위 ★ 켜짐, 아래 ★ 빔", scene(base, pointer=(0, 1))),
        ("3 든 동안: 놓일 자리의 ★ 미리", scene([ls, it("dagger", 0, 1, 2, 1), it("mace", 0, 2, 3, 1)],
                                                 ghost=dict(it("whetstone", 2, 1, 1, 1), kind="fits"))),
        ("4 돌린 숫돌: ★ 칸이 좌우로", scene([ls, it("dagger", 0, 1, 2, 1), it("whetstone", 2, 1, 1, 1, rot=True)], pointer=(2, 1))),
        ("5 빈 칸은 빈 ★, 가방 밖은 없음", scene([ls, it("whetstone", 1, 2, 1, 1)], pointer=(1, 2))),
        ("6 숫돌의 카드", bb_card),
        ("7 롱소드의 카드: 강화는 수치로", weapon_card),
        ("8 전투: ★ 없음(조사 밖)", lambda im: A.LOOK.board(im, 73, 34, dict(num=1, name="", bags=[A.PACK], items=base),
                                                               battle={"longsword": 0.6, "dagger": 0.3})),
    ]
    sheet([(t, tile(f)) for t, f in tiles], 3, HERE / "mock-stars-bb.png",
          "★ 표시 — 백팩 배틀즈의 방식 그대로 (숫돌 그림은 도형)",
          ["★은 숫돌의 위 칸과 아래 칸(돌리면 왼쪽·오른쪽 칸)에 그린다. 그 칸의 아이템 위에 겹쳐 그리고, 가방 밖의 칸에는 그리지 않는다.",
           "근접 무기가 그 칸에 있으면 금빛으로 켜지고, 아니면 빈 별이다. 강화는 무기의 카드에 수치로 적는다(조각에 +1을 붙이지 않음)."])


if __name__ == "__main__":
    main()
