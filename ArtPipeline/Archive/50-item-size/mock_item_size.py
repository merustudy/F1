# -*- coding: utf-8 -*-
"""Round 50, item sizes (the user, 2026-10-09: "아이템 크기 재조정 검토 후 보고").
Today's shapes (A: the icon's ratio, 19단계) against two narrower ones on the same expedition state, in today's look (round 49,
Diablo II changed):
  B  every item one square narrower (3x1 -> 2x1, 2x1 -> 1x1, 3x2 -> 2x2, 3x3 -> 2x3)
  B2 only the three-wide ones narrower (3x1 -> 2x1, 3x2 -> 2x2, 3x3 -> 2x3; 2x1 and 1x1 as they are)
Three sheets: the board panel of one state under each, what fits in the start pack, and the shapes.
Drawn shapes and today's icons; no API call.
  .venv/bin/python ArtPipeline/Archive/50-item-size/mock_item_size.py
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent / "49-inventory-style"))
import mock_inventory_style as S  # noqa: E402
import mock_diablo_v2 as V2  # noqa: E402
from mock_inventory_style import s, B, D, cell, span, bag, text, font, line, PACK, PAD, SQ, COLS, TOP, comp  # noqa: E402

GAME = HERE.parent / "49-inventory-style/game/ko_35_map_tiers.png"     # today's node map (round 49)

A = {"longsword": (3, 1), "greataxe": (3, 1), "mace": (3, 1), "healing_staff": (3, 1), "fire_staff": (3, 1), "dagger": (2, 1),
     "longbow": (3, 2), "spear": (3, 2), "halberd": (3, 3), "buckler": (1, 1), "ward_charm": (3, 1), "herb_pouch": (2, 1),
     "ember_flask": (2, 1), "rat_bite": (1, 1), "rusty_blade": (3, 1), "crude_bow": (3, 1), "lantern_staff": (3, 1),
     "mending_chant": (3, 1), "overseer_maul": (3, 1), "overseer_roar": (2, 1)}
SHAPES = {
    "A": A,
    "B": {k: (max(1, w - 1), h) for k, (w, h) in A.items()},
    "B2": {k: ((2, h) if w == 3 else (w, h)) for k, (w, h) in A.items()},
}
TITLES = {"A": "A  지금 (아이콘의 비율)", "B": "B  모두 한 칸 좁게", "B2": "B2  가로 3칸만 2칸으로 (권장)"}
NAMES = dict(S.NAMES, crude_bow="조잡한 활", mending_chant="치유의 부적", halberd="미늘창", greataxe="대도끼",
             rat_bite="쥐 송곳니", overseer_roar="감독관의 포효", lantern_staff="등불 지팡이", overseer_maul="감독관의 망치")
LOOK = V2.DiabloV2()


def F(px): return ImageFont.truetype(str(S.PRET), px)   # a font for the sheets drawn at the screen's size


def it(opt, id, x, y, tier="common", rot=False):
    w, h = SHAPES[opt][id]
    if rot: w, h = h, w
    return dict(id=id, x=x, y=y, w=w, h=h, tier=tier, rot=rot)


# ---- one expedition state: the same sixteen items under each option ----------------------------------------------------------
# 미라 fire_staff, ember_flask, ward_charm, herb_pouch / 엘라 healing_staff, mending_chant, herb_pouch, crude_bow /
# 카이 longsword, dagger, buckler, rusty_blade, herb_pouch / 로언 longsword, spear, buckler, mace

def state(o):
    I = lambda *a, **k: it(o, *a, **k)
    if o == "A":
        boards = [[I("fire_staff", 0, 0), I("ember_flask", 0, 1, "bronze"), I("ward_charm", 0, 2)],
                  [I("healing_staff", 0, 0), I("mending_chant", 0, 1, "bronze"), I("herb_pouch", 0, 2, "gold")],
                  [I("longsword", 0, 0, "bronze"), I("dagger", 0, 1, "bronze"), I("buckler", 2, 1, "silver"), I("rusty_blade", 0, 2)],
                  [I("longsword", 0, 0, "silver"), I("spear", 0, 1)]]
        out = ["herb_pouch", "crude_bow", "herb_pouch", "buckler", "mace"]
    elif o == "B":
        boards = [[I("fire_staff", 0, 0), I("ember_flask", 2, 0, "bronze"), I("ward_charm", 0, 1), I("herb_pouch", 2, 1)],
                  [I("healing_staff", 0, 0), I("herb_pouch", 2, 0, "gold"), I("mending_chant", 0, 1, "bronze"), I("crude_bow", 0, 2)],
                  [I("longsword", 0, 0, "bronze"), I("dagger", 2, 0, "bronze"), I("rusty_blade", 0, 1), I("buckler", 2, 1, "silver"),
                   I("herb_pouch", 0, 2)],
                  [I("spear", 0, 0), I("longsword", 2, 0, "silver", rot=True), I("mace", 0, 2, "bronze"), I("buckler", 2, 2)]]
        out = []
    else:
        boards = [[I("fire_staff", 0, 0), I("ember_flask", 2, 0, "bronze", rot=True), I("ward_charm", 1, 2), I("herb_pouch", 0, 1, rot=True)],
                  [I("healing_staff", 0, 0), I("mending_chant", 2, 0, "bronze", rot=True), I("crude_bow", 1, 2),
                   I("herb_pouch", 0, 1, "gold", rot=True)],
                  [I("longsword", 0, 0, "bronze"), I("dagger", 2, 0, "bronze", rot=True), I("rusty_blade", 1, 2),
                   I("herb_pouch", 0, 1, rot=True), I("buckler", 1, 1, "silver")],
                  [I("spear", 0, 0), I("longsword", 2, 0, "silver", rot=True), I("mace", 0, 2, "bronze"), I("buckler", 2, 2)]]
        out = []
    return boards, out


MERCS = [dict(num=4, name="미라"), dict(num=3, name="엘라"), dict(num=2, name="카이"), dict(num=1, name="로언")]


def base():
    """Today's node map with the four boards wiped: the panel's own texture from under the boards."""
    img = Image.open(GAME).convert("RGB")
    patch = img.crop((100, 836, 890, 1032)); img.paste(patch, (100, 616))
    return img.resize((s(1920), s(1080)), Image.LANCZOS).convert("RGBA")


def board_panel(o):
    img = base(); boards, out = state(o)
    for x0, m, items in zip(COLS, MERCS, boards):
        LOOK.board(img, x0, TOP, dict(m, bags=[PACK], items=items))
        LOOK.plate(img, x0, TOP - 24, m)
    band = img.resize((1920, 1080), Image.LANCZOS).crop((0, 608, 962, 814))
    on = sum(len(b) for b in boards)
    free = sum(9 - sum(i["w"] * i["h"] for i in b) for b in boards)
    return band, on, free, out


def sheet_compare():
    W, H = 962, 206; side = 540; head = 40; gap = 16
    out = Image.new("RGB", (W + side + 3 * gap, 70 + 3 * (head + H + gap) + 64), (18, 18, 22))
    d = ImageDraw.Draw(out)
    d.text((gap, 18), "같은 원정의 같은 아이템 열여섯 (시작 가죽 배낭 3×3, 넷 모두 같은 물건)", font=F(28), fill=(240, 236, 226))
    y = 70
    for o in ("A", "B", "B2"):
        band, on, free, over = board_panel(o)
        d.text((gap, y + 4), TITLES[o], font=F(24), fill=(236, 214, 150) if o == "B2" else (236, 232, 222))
        out.paste(band, (gap, y + head))
        x = W + 2 * gap; yy = y + head + 8
        lines = [(f"보드에 {on}개 · 빈 칸 {free}", (236, 232, 222))]
        if over:
            lines.append((f"인벤토리로 밀려남 {len(over)}개:", (226, 120, 100)))
            lines.append(("  " + ", ".join(NAMES[i] for i in over), (226, 120, 100)))
        else:
            lines.append(("밀려난 것 없음", (140, 200, 140)))
        notes = {"A": ["3×1이 배낭의 한 줄을 다 차지한다: 줄 셋 = 아이템 셋.", "로언: 기본 무기 + 창(3×2)으로 꽉 참.", "돌리기는 3×1을 1×3으로 바꿀 뿐이다."],
                 "B": ["작은 것(1×1)이 남는 칸을 메운다.", "로언: 창 2×2 + 롱소드를 세로로 돌림 + 메이스 + 버클러.", "단검·약초·플라스크 아이콘이 절반 크기로 준다."],
                 "B2": ["2×1 넷이 바람개비로 맞물린다: 둘은 돌려야 든다.", "카이: 가운데 칸에 버클러까지 다섯.", "작은 것(2×1)의 아이콘은 지금 그대로."]}[o]
        for t in notes: lines.append(("· " + t, (170, 170, 178)))
        for t, c in lines:
            d.text((x, yy), t, font=F(18), fill=c); yy += 28
        y += head + H + gap
    for k, t in enumerate(["다시 그린 것은 보드의 아이템뿐이다(배낭·바탕·이름표는 지금 게임의 모습). 도형과 지금 아이콘, 호출 없음.",
                           "16개 = 기본 무기 넷 + 상점·전리품에서 얻은 열둘. 자리는 사람이 놓을 법한 곳(B2는 돌림을 써서)."]):
        d.text((gap, out.height - 58 + k * 26), t, font=F(17), fill=(170, 170, 178))
    out.save(HERE / "mock-compare.png"); print("mock-compare.png", out.size)


# ---- what fits in the start pack ------------------------------------------------------------------------------------------------

def packs(o):
    I = lambda *a, **k: it(o, *a, **k)
    if o == "A":
        return [("기본 + 3×1 둘 = 셋", [I("longsword", 0, 0), I("rusty_blade", 0, 1), I("ward_charm", 0, 2)]),
                ("기본 + 창 3×2 = 둘 (꽉 참)", [I("longsword", 0, 0), I("spear", 0, 1)]),
                ("작은 것으로 줄을 나눔 = 넷", [I("longsword", 0, 0), I("dagger", 0, 1), I("buckler", 2, 1), I("herb_pouch", 0, 2)]),
                ("미늘창 3×3: 기본 무기를 빼야", [I("halberd", 0, 0)])]
    if o == "B":
        return [("2×1 넷 + 1×1 = 다섯 (둘 돌림)", [I("longsword", 0, 0), I("rusty_blade", 2, 0, rot=True), I("ward_charm", 1, 2),
                                                 I("mending_chant", 0, 1, rot=True), I("dagger", 1, 1)]),
                ("창 2×2 + 기본(돌림) + 둘 = 넷", [I("spear", 0, 0), I("longsword", 2, 0, rot=True), I("mace", 0, 2), I("buckler", 2, 2)]),
                ("미늘창 2×3 + 기본(돌림) + 1×1", [I("halberd", 0, 0), I("longsword", 2, 0, rot=True), I("buckler", 2, 2)]),
                ("1×1이 많아 덜 돌림 = 여섯", [I("longsword", 0, 0), I("dagger", 2, 0), I("herb_pouch", 0, 1), I("ember_flask", 1, 1),
                                                 I("buckler", 2, 1), I("ward_charm", 0, 2)])]
    return [("2×1 넷 + 1×1 = 다섯 (둘 돌림)", [I("longsword", 0, 0), I("dagger", 2, 0, rot=True), I("herb_pouch", 1, 2),
                                             I("ember_flask", 0, 1, rot=True), I("buckler", 1, 1)]),
            ("창 2×2 + 기본(돌림) + 둘 = 넷", [I("spear", 0, 0), I("longsword", 2, 0, rot=True), I("mace", 0, 2), I("buckler", 2, 2)]),
            ("미늘창 2×3 + 기본(돌림) + 1×1", [I("halberd", 0, 0), I("longsword", 2, 0, rot=True), I("buckler", 2, 2)]),
            ("돌리지 않으면 2×1은 셋", [I("longsword", 0, 0), I("dagger", 0, 1), I("herb_pouch", 0, 2)])]


def pack_tile(items):
    w, h = 270, 210; img = Image.new("RGBA", (s(w), s(h)), (30, 29, 28, 255))
    x0, y0 = (w - span(3)) / 2, 22
    LOOK.board(img, x0, y0, dict(num=1, name="", bags=[PACK], items=items))
    return img.resize((w, h), Image.LANCZOS).convert("RGB")


def sheet_packs():
    tiles = []
    for o in ("A", "B", "B2"):
        for k, (t, items) in enumerate(packs(o)):
            tiles.append((TITLES[o].split()[0] + "  " + t, pack_tile(items)))
    S.sheet(tiles, 4, HERE / "mock-packing.png", title="시작 가죽 배낭(3×3)에 드는 것 — 위: A 지금 / 가운데: B / 아래: B2", label=17,
            footer=["A: 줄 = 아이템이라 셋이 보통이고 큰 것(창·장궁 3×2, 미늘창 3×3)이 기본 무기와 함께 들기 어렵다.",
                    "B·B2: 기본 무기가 2×1이 되어 배낭에 넷~다섯이 들고, 2×1을 맞물리려면 돌려야 한다(백팩 배틀즈의 돌리기가 쓰인다)."])


# ---- the shapes -----------------------------------------------------------------------------------------------------------------

GROUPS = [("longsword", "롱소드·대도끼·메이스·두 지팡이·수호 부적,\n녹슨 칼·조잡한 활·등불 지팡이·치유의 부적·감독관의 망치 (열하나)"),
          ("dagger", "단검·약초 주머니·불씨 플라스크·감독관의 포효 (넷)"),
          ("spear", "장궁·창 (둘)"),
          ("halberd", "미늘창 (하나)"),
          ("buckler", "버클러·쥐 송곳니 (둘)")]


def sheet_shapes():
    cols = ("A", "B", "B2"); name_w, col_w, top = 470, 230, 96
    heights = [max(span(SHAPES[o][g][1]) for o in cols) + 46 for g, _ in GROUPS]
    W, H = name_w + col_w * 3 + 40, top + sum(heights) + 70
    img = Image.new("RGBA", (s(W), s(H)), (18, 18, 22, 255)); d = D(img)
    text(d, (20, 20), "모양 표 — 지금(A)과 두 안", font(26), (240, 236, 226))
    for k, o in enumerate(cols):
        text(d, (name_w + col_w * k + col_w / 2, 72), TITLES[o].split("  ")[0] + ("  (권장)" if o == "B2" else ""), font(20),
             (236, 214, 150) if o == "B2" else (236, 232, 222), "mm")
    y = top
    for (g, label), hh in zip(GROUPS, heights):
        line(d, [(20, y - 8), (W - 20, y - 8)], (60, 58, 54), 1)
        yy = y + 6
        for part in label.split("\n"):
            text(d, (20, yy), part, font(16), (200, 196, 186)); yy += 24
        for k, o in enumerate(cols):
            w, h = SHAPES[o][g]; cx = name_w + col_w * k + col_w / 2
            X, Y = cx - span(w) / 2, y + 4
            LOOK.well(img, X, Y, w, h); LOOK.squares(img, X, Y, w, h)
            comp(img, LOOK.piece(dict(id=g, x=0, y=0, w=w, h=h, tier="common")), (s(X - PAD), s(Y - PAD)))
            text(D(img), (cx, Y + span(h) + 18), f"{w}×{h}", font(16), (170, 170, 178), "mm")
        y += hh
    d = D(img)
    text(d, (20, H - 44), "아이콘은 지금 그림을 칸에 맞춰 줄였다. 2×2·2×3·1×1이 된 것은 그림을 모양의 캔버스로 다시 그리면 커진다(그림은 지시가 있을 때).",
         font(15), (150, 150, 158))
    img.resize((W, H), Image.LANCZOS).convert("RGB").save(HERE / "mock-shapes.png"); print("mock-shapes.png", (W, H))


def main():
    sheet_compare()
    sheet_packs()
    sheet_shapes()


if __name__ == "__main__":
    main()
