# -*- coding: utf-8 -*-
"""Round 51 (the user, 2026-10-10: "발키리 기본무기 전투도끼 3*2 사이즈 변경 및 목업 제공", "★ 닿음 효과 — 숫돌 아이템 신설 - 위아래 닿은
근접무기 공격력 +1"), in today's look (round 49, Diablo II changed):
  mock-axe.png        the valkyrie's great axe 3x2 on her start pack: today (3x1) and three ways to show its icon
  mock-whetstone.png  the whetstone and its stars: on the board, both stars, turned, held, its card, in battle
The whetstone is a drawn shape (no picture yet); the axe's tilted icon is today's icon turned, not a new picture. No API call.
  .venv/bin/python ArtPipeline/Archive/51-claw-axe-whetstone/mock_axe_whetstone.py
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
sys.path.insert(0, str(HERE.parent / "49-inventory-style"))
sys.path.insert(0, str(HERE.parent / "50-item-size"))
sys.path.insert(0, str(ROOT / "ArtPipeline/tools"))
import mock_inventory_style as S  # noqa: E402
import mock_diablo_v2 as V2  # noqa: E402
import mock_item_size as M50  # noqa: E402
import trim_items  # noqa: E402
from mock_inventory_style import s, B, D, cell, span, bag, text, font, line, star, PACK, PAD, SQ, COLS, TOP, comp  # noqa: E402

LOOK = V2.DiabloV2()
GOLD = (236, 196, 92); DIM = (92, 88, 80); WHITE = (238, 238, 238); BLUE = (112, 112, 255); GREY = (150, 150, 158)


def F(px): return ImageFont.truetype(str(S.PRET), px)


# ---- art: today's icons trimmed, the axe turned, and a drawn whetstone ----------------------------------------------------------

def item_png(id): return trim_items.trimmed(Image.open(ROOT / f"Assets/@Art/Item/{id}.png").convert("RGBA"))


def register(id, im):
    S._icons[(id, False, False)] = im
    S._icons[(id, True, False)] = im.rotate(-90, expand=True)


def whetstone_art():
    """A grey whetstone bar lying a little aslant, an ink outline and two pale streaks: a stand-in until it is drawn."""
    W, H = 240, 240; im = Image.new("RGBA", (W, H), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    bar = Image.new("RGBA", (200, 96), (0, 0, 0, 0)); b = ImageDraw.Draw(bar)
    b.rounded_rectangle([4, 4, 196, 92], 26, fill=(128, 134, 140), outline=(24, 22, 20), width=7)
    b.rounded_rectangle([14, 52, 186, 84], 16, fill=(98, 104, 112))
    for x0, x1 in ((40, 110), (70, 160)): b.line([(x0, 30 if x0 == 40 else 40), (x1, 24 if x0 == 40 else 34)], fill=(196, 202, 206), width=6)
    b.rounded_rectangle([4, 4, 196, 92], 26, outline=(24, 22, 20), width=7)
    bar = bar.rotate(28, expand=True, resample=Image.BICUBIC)
    im.alpha_composite(bar, ((W - bar.width) // 2, (H - bar.height) // 2))
    return trim_items.trimmed(im)


AXE = item_png("greataxe")
register("greataxe", AXE)
register("greataxe_tilt", trim_items.trimmed(AXE.rotate(30, expand=True, resample=Image.BICUBIC)))
register("whetstone", whetstone_art())
for k in ("longsword", "dagger", "herb_pouch", "mace", "buckler", "rusty_blade", "fire_staff", "healing_staff", "rat_bite", "ember_flask"):
    register(k, item_png(k))
NAMES = dict(M50.NAMES, whetstone="숫돌", greataxe="대도끼")
MELEE = {"longsword", "greataxe", "greataxe_tilt", "mace", "dagger", "rusty_blade", "rat_bite", "spear", "halberd"}


def it(id, x, y, w, h, tier="common", rot=False, sel=False):
    return dict(id=id, x=x, y=y, w=w, h=h, tier=tier, rot=rot, sel=sel)


# ---- stars: a whetstone's two stars sit on the middle of its top and bottom edges (left and right once turned) --------------

def star_spots(i):
    """(gx, gy, side) of a whetstone's two star sides: the squares beyond them and where on the board the star is drawn."""
    if i["rot"]:
        return [((i["x"] - 1, i["y"]), (i["x"], i["y"] + 0.5)), ((i["x"] + 1, i["y"]), (i["x"] + 1, i["y"] + 0.5))]
    return [((i["x"], i["y"] - 1), (i["x"] + 0.5, i["y"])), ((i["x"], i["y"] + 1), (i["x"] + 0.5, i["y"] + 1))]


def at(c):
    """A board coordinate in squares to pixels: a whole number is the line between squares, a half the middle of a square."""
    return c * (SQ + S.GAP) - S.GAP / 2 if c == int(c) else int(c) * (SQ + S.GAP) + SQ / 2


def owner(items, gx, gy):
    for o in items:
        if o["x"] <= gx < o["x"] + o["w"] and o["y"] <= gy < o["y"] + o["h"]: return o
    return None


def stars(img, x0, y0, items, lit_override=None, flash=False):
    d = D(img)
    for w in [i for i in items if i["id"] == "whetstone"]:
        for (gx, gy), (sx, sy) in star_spots(w):
            o = owner(items, gx, gy)
            lit = o is not None and o["id"] in MELEE if lit_override is None else lit_override
            X, Y = x0 + at(sx), y0 + at(sy)
            if flash and lit:
                halo = Image.new("RGBA", img.size, (0, 0, 0, 0)); hd = ImageDraw.Draw(halo)
                hd.ellipse(B(X - 16, Y - 16, X + 16, Y + 16), fill=(255, 220, 120, 120)); comp(img, halo.filter(S.ImageFilter.GaussianBlur(s(6))), (0, 0))
                d = D(img)
            star(d, X, Y, 8.2, (0, 0, 0)); star(d, X, Y, 6.6, GOLD if lit else DIM)


def board(img, x0, y0, items, bags=(PACK,), ghost=None, battle=None, lit=None, flash=False):
    LOOK.board(img, x0, y0, dict(num=1, name="", bags=list(bags), items=items), ghost=ghost, battle=battle)
    shown = items + ([ghost] if ghost else [])
    stars(img, x0, y0, shown, lit_override=lit, flash=flash)


def plus(img, x0, y0, i, t="+1"):
    """The bonus a whetstone gives, on the weapon's piece: a small gold number at its top right."""
    X, Y = cell(x0, y0, i["x"], i["y"]); W = span(i["w"])
    text(D(img), (X + W - 4, Y + 3), t, font(13), GOLD, "ra", stroke=s(1), stroke_fill=(0, 0, 0))


def tile(draw, w=300, h=250, bg=(30, 29, 28)):
    img = Image.new("RGBA", (s(w), s(h)), bg + (255,)); draw(img)
    return img.resize((w, h), Image.LANCZOS).convert("RGB")


def sheet(tiles, cols, path, title, footer):
    S.sheet(tiles, cols, path, title=title, label=17, footer=footer)


# ---- the axe ----------------------------------------------------------------------------------------------------------------------

def axe_sheet():
    X0, Y0 = 73, 30
    tiles = [
        ("지금: 대도끼 3×1, 빈 줄 둘", lambda im: board(im, X0, Y0, [it("greataxe", 0, 0, 3, 1)])),
        ("3×2 안 1: 지금 아이콘을 평평하게", lambda im: board(im, X0, Y0, [it("greataxe", 0, 0, 3, 2)])),
        ("3×2 안 2 (권장): 지금 아이콘을 비스듬히", lambda im: board(im, X0, Y0, [it("greataxe_tilt", 0, 0, 3, 2)])),
        ("안 2 + 남은 한 줄: 단검 2×1 + 버클러", lambda im: board(im, X0, Y0, [it("greataxe_tilt", 0, 0, 3, 2), it("dagger", 0, 2, 2, 1),
                                                                    it("buckler", 2, 2, 1, 1)])),
        ("안 2 + 숫돌: 아래에 닿아 ★ 걸림 (+1)", lambda im: (board(im, X0, Y0, [it("greataxe_tilt", 0, 0, 3, 2), it("whetstone", 1, 2, 1, 1),
                                                                     it("buckler", 2, 2, 1, 1)]),
                                                               plus(im, X0, Y0, it("greataxe_tilt", 0, 0, 3, 2)))),
        ("안 2 전투 (파란 바탕 없음, 쿨다운 절반)", lambda im: board(im, X0, Y0, [it("greataxe_tilt", 0, 0, 3, 2)], battle={"greataxe_tilt": 0.5})),
    ]
    sheet([(t, tile(f)) for t, f in tiles], 3, HERE / "mock-axe.png",
          "발키리의 대도끼 3×2 — 시작 가죽 배낭 3×3 (아스트리드)",
          ["안 1: 지금 그림(가로 띠)이라 위아래가 빈다. 안 2: 같은 그림을 30° 돌려 구움(호출 없음).",
           "안 3(그림 없음): 3×2 구도로 다시 그림(호출 1회, 발키리 그림을 레퍼런스로).",
           "3×2면 시작 배낭에 남는 것은 한 줄(3칸): 2×1과 1×1, 또는 숫돌. 숫돌을 바로 아래에 두면 ★이 걸린다."])

    # the party band: the valkyrie in front with 안 2
    img = M50.base()
    boards = [[it("fire_staff", 0, 0, 3, 1), it("ember_flask", 0, 1, 2, 1, "bronze")],
              [it("healing_staff", 0, 0, 3, 1), it("herb_pouch", 0, 1, 2, 1, "gold")],
              [it("longsword", 0, 0, 3, 1, "bronze"), it("dagger", 0, 1, 2, 1), it("buckler", 2, 1, 1, 1, "silver")],
              [it("greataxe_tilt", 0, 0, 3, 2), it("whetstone", 1, 2, 1, 1)]]
    mercs = [dict(num=4, name="미라"), dict(num=3, name="엘라"), dict(num=2, name="카이"), dict(num=1, name="아스트리드")]
    for x0, m, items in zip(COLS, mercs, boards):
        board(img, x0, TOP, items)
        LOOK.plate(img, x0, TOP - 24, m)
    plus(img, COLS[3], TOP, boards[3][0])
    band = img.resize((1920, 1080), Image.LANCZOS).crop((0, 608, 962, 814)).convert("RGB")
    out = Image.new("RGB", (band.width + 36, band.height + 80), (18, 18, 22)); d = ImageDraw.Draw(out)
    d.text((18, 16), "노드 맵의 보드 패널: 아스트리드(①)의 대도끼 3×2(안 2)와 그 아래 숫돌", font=F(22), fill=(240, 236, 226))
    out.paste(band, (18, 56)); out.save(HERE / "mock-axe-panel.png"); print("mock-axe-panel.png", out.size)


# ---- the whetstone ------------------------------------------------------------------------------------------------------------------

def card(im):
    d = D(im); x0, y0, w, h = 20, 18, 260, 190
    d.rectangle(B(x0, y0, x0 + w, y0 + h), fill=(0, 0, 0, 228), outline=(70, 70, 70))
    lines = [("숫돌 · 등급 8", font(17), WHITE), ("기타 아이템 · 1×1", font(13), GREY), ("", None, None),
             ("★ 위아래에 닿은 근접 무기", font(14), GOLD), ("    무기의 피해 +1", font(14), BLUE), ("", None, None),
             ("지금 닿음: 롱소드 (+1)", font(14), WHITE), ("아래: 약초 주머니 — 근접 무기 아님", font(12), GREY),
             ("쿨다운 없음 · 피로 없음", font(12), GREY)]
    y = y0 + 16
    for t, f, c in lines:
        if t: text(d, (x0 + w / 2, y), t, f, c, "mm")
        y += 19 if t else 8


def whetstone_sheet():
    X0, Y0 = 73, 34
    ls = it("longsword", 0, 0, 3, 1)
    tiles = [
        ("롱소드 아래: 위 ★ +1, 아래 ★ 꺼짐(약초)", lambda im: (board(im, X0, Y0, [ls, it("whetstone", 0, 1, 1, 1), it("dagger", 1, 1, 2, 1),
                                                                         it("herb_pouch", 0, 2, 2, 1)]), plus(im, X0, Y0, ls))),
        ("위아래 둘 다: 롱소드와 메이스가 +1씩", lambda im: (board(im, X0, Y0, [ls, it("whetstone", 1, 1, 1, 1), it("mace", 0, 2, 3, 1)]),
                                                         plus(im, X0, Y0, ls), plus(im, X0, Y0, it("mace", 0, 2, 3, 1)))),
        ("돌리면 ★도 돈다: 왼쪽의 단검에 걸림", lambda im: (board(im, X0, Y0, [ls, it("dagger", 0, 1, 2, 1), it("whetstone", 2, 1, 1, 1, rot=True)]),
                                                        plus(im, X0, Y0, it("dagger", 0, 1, 2, 1)))),
        ("든 동안: 놓으면 걸릴 ★을 미리 보임", lambda im: board(im, X0, Y0, [ls, it("herb_pouch", 0, 2, 2, 1)],
                                                          ghost=dict(it("whetstone", 2, 1, 1, 1), kind="fits"))),
        ("우클릭 카드", card),
        ("전투: 롱소드가 발동하는 순간 ★이 금빛으로", lambda im: board(im, X0, Y0, [ls, it("whetstone", 0, 1, 1, 1), it("dagger", 1, 1, 2, 1)],
                                                              battle={"longsword": 1.0, "dagger": 0.35}, flash=True)),
    ]
    sheet([(t, tile(f)) for t, f in tiles], 3, HERE / "mock-whetstone.png",
          "숫돌(1×1)과 ★ 닿음 — 위아래 변의 ★에 닿은 근접 무기의 피해 +1 (숫돌 그림은 도형)",
          ["★은 숫돌의 위·아래 변 가운데(돌리면 왼쪽·오른쪽). 근접 무기가 닿으면 금빛, 아니면 어둡다.",
           "강화된 무기는 조각 오른쪽 위에 금빛 +1.",
           "근접 무기(권장, 데이터의 새 열): 롱소드·대도끼·메이스·단검·창·미늘창·쥐 발톱·녹슨 칼·감독관의 망치."])


def main():
    axe_sheet()
    whetstone_sheet()


if __name__ == "__main__":
    main()
