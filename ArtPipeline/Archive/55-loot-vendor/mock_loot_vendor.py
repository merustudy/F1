# -*- coding: utf-8 -*-
"""Round 55 (the user, 2026-10-10: "전리품 화면에서 인벤토리를 킨 다음 드랍된 전리품 드래그앤 드롭으로 인벤토리 넣을 수 있게 / 드랍된 전리품
아이콘 크기를 아이템 창과 동일한 크기로 통일", "상점 아이템 공급 방식도 디아블로 2 상인이 공급해주는 방식으로", review): the floor's icons at
the item windows' size, a drop held over the inventory, and a Diablo II merchant's window in place of the shop's four tiles. Drawn on
today's screenshots (game/, 2026-10-10 r54d) at their own pixels. No API call.
  .venv/bin/python ArtPipeline/Archive/55-loot-vendor/mock_loot_vendor.py
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
sys.path.insert(0, str(HERE.parent / "49-inventory-style"))
import mock_inventory_style as S  # noqa: E402

GAME = HERE / "game"
ART = ROOT / "Assets/@Art/Item"
SQ, GAP = 50, 2
STEP = SQ + GAP
GROUND, LINE, EMPTY = (31, 38, 84), (56, 56, 61), (13, 13, 13)
GREEN, RED = (21, 105, 34), (123, 25, 25)
GOLD, BONE, WHITE, BLUE, GREY = (199, 179, 119), (205, 192, 160), (238, 238, 238), (112, 112, 255), (150, 142, 124)
STONE, EDGE, PLATE = (57, 55, 57), (112, 106, 98), (14, 13, 12)
INV = (38, 166)                      # the inventory window's first square (ko_48)
FLOOR_Y = 547                        # the floor line the drops lie on (ko_06)


def F(px): return ImageFont.truetype(str(S.PRET), px)


def span(n): return n * SQ + (n - 1) * GAP


def icon(item_id): return Image.open(ART / f"{item_id}.png").convert("RGBA")


def fit(im, w, h):
    k = min(w / im.width, h / im.height)
    return im.resize((max(1, round(im.width * k)), max(1, round(im.height * k))), Image.LANCZOS)


def lying(item_id, w, h, box):
    """An icon on the floor: fitted in a box, tilted as the floor's drops are (-12 degrees)."""
    return fit(icon(item_id), *box).rotate(12, expand=True, resample=Image.BICUBIC)


def floor_patch(img, box, source_dx):
    """Covers a box of the stage with the floor beside it (the drop picked up or redrawn)."""
    x0, y0, x1, y1 = box
    img.paste(img.crop((x0 + source_dx, y0, x1 + source_dx, y1)), (x0, y0))


# ---- 1. the floor's icons ---------------------------------------------------------------------------------------------------------

ITEMS = [("rat_bite", 2, 1, "쥐 발톱 2×1"), ("dagger", 2, 1, "단검 2×1"), ("buckler", 1, 1, "버클러 1×1"), ("spear", 2, 2, "창 2×2"),
         ("halberd", 2, 3, "미늘창 2×3"), ("greataxe", 3, 2, "전투도끼 3×2"), ("longsword", 3, 1, "롱소드 3×1")]


def floor_strip(unified):
    """The drops' icons on today's floor: in the floor's box 150x70 (today) or at their squares' size in an item window (proposal)."""
    base = Image.open(GAME / "ko_48_battle_won_inventory.png").convert("RGB")   # the floor after the drop was taken: nothing lies there
    strip = base.crop((960, 420, 1920, 620)).resize((1680, 350), Image.LANCZOS)
    w = Image.new("RGB", (1680, 350)); w.paste(strip, (0, 0))
    d = ImageDraw.Draw(w)
    x = 30
    for item_id, iw, ih, name in ITEMS:
        box = (span(iw) - 10, span(ih) - 10) if unified else (150, 70)
        ic = lying(item_id, iw, ih, box)
        w.paste(ic, (x + (200 - ic.width) // 2, 250 - ic.height), ic)
        d.text((x + 100, 290), name, font=F(18), fill=WHITE, anchor="mm")
        x += 235
    return w


# ---- 2. a drop held over the inventory ---------------------------------------------------------------------------------------------

def inventory_scene(now):
    """The battle screen after a win with the inventory open (ko_48's window over ko_06's stage), the floor's claw picked and on the
    pointer over the inventory's (4, 1): red today (a drop goes there by its button only), green as proposed (a press lays it there)."""
    img = Image.open(GAME / "ko_06_battle_won.png").convert("RGB")
    win = Image.open(GAME / "ko_48_battle_won_inventory.png").convert("RGB")
    img.paste(win.crop((0, 140, 1920, 620)), (0, 140))              # the window over the stage and the floor with the claw off it (on the pointer)
    img.paste(win.crop((0, 92, 960, 140)), (0, 92))                 # the window's top over the band's left end; the band's right end stays ("전리품 1")
    d = ImageDraw.Draw(img)
    x0, y0 = INV[0] + 4 * STEP, INV[1] + STEP
    d.rectangle((x0, y0, x0 + span(2) - 1, y0 + SQ - 1), fill=RED if now else GREEN)
    ic = fit(icon("rat_bite"), span(2) - 10, SQ - 10)
    cx, cy = x0 + span(2) // 2 + 8, y0 + SQ // 2 - 4
    img.paste(ic, (cx - ic.width // 2, cy - ic.height // 2), ic)
    d.ellipse((cx - 5, cy - 5, cx + 5, cy + 5), outline=(255, 255, 255), width=2)
    return img


# ---- 3. a Diablo II merchant's window ---------------------------------------------------------------------------------------------

WINDOW = (1010, 176, 1870, 742)        # where today's shop window lies (860x566 over the map)
GRID = (10, 7)
STOCK = [("longsword", 0, 0, 3, 1), ("mace", 3, 0, 3, 1), ("fire_staff", 6, 0, 3, 1), ("dagger", 0, 1, 2, 1), ("rusty_blade", 2, 1, 2, 1),
         ("crude_bow", 4, 1, 2, 1), ("spear", 0, 2, 2, 2), ("longbow", 2, 2, 2, 2), ("halberd", 4, 2, 2, 3), ("greataxe", 6, 2, 3, 2),
         ("lantern_staff", 6, 4, 3, 1), ("healing_staff", 0, 4, 3, 1), ("buckler", 9, 0, 1, 1), ("whetstone", 9, 1, 1, 1)]


def vendor():
    img = Image.open(GAME / "ko_42_shop.png").convert("RGB")
    d = ImageDraw.Draw(img)
    x0, y0, x1, y1 = WINDOW
    d.rectangle(WINDOW, fill=STONE, outline=EDGE, width=2)
    d.rectangle((x0 + 20, y0 + 16, x0 + 300, y0 + 52), fill=PLATE)
    d.text((x0 + 160, y0 + 34), "4층 · 상인", font=F(22), fill=GOLD, anchor="mm")
    d.text((x1 - 24, y0 + 34), "가진 코인 37", font=F(18), fill=BONE, anchor="rm")
    tabs = [("무기", True), ("방어·기타", False), ("포션", False)]
    tx = x0 + 20
    for name, on in tabs:
        d.rectangle((tx, y0 + 64, tx + 120, y0 + 96), fill=PLATE if on else (34, 33, 33), outline=EDGE if on else (70, 66, 62))
        d.text((tx + 60, y0 + 80), name, font=F(17), fill=GOLD if on else GREY, anchor="mm")
        tx += 126
    gx, gy = x0 + 26, y0 + 112
    d.rectangle((gx - 6, gy - 6, gx + span(GRID[0]) + 5, gy + span(GRID[1]) + 5), fill=(34, 33, 31))
    d.rectangle((gx, gy, gx + span(GRID[0]) - 1, gy + span(GRID[1]) - 1), fill=LINE)
    for i in range(GRID[0]):
        for j in range(GRID[1]):
            d.rectangle((gx + i * STEP, gy + j * STEP, gx + i * STEP + SQ - 1, gy + j * STEP + SQ - 1), fill=EMPTY)
    for item_id, ix, iy, w, h in STOCK:
        for i in range(w):
            for j in range(h):
                X, Y = gx + (ix + i) * STEP, gy + (iy + j) * STEP
                d.rectangle((X, Y, X + SQ - 1, Y + SQ - 1), fill=GROUND)
        ic = fit(icon(item_id), span(w) - 10, span(h) - 10)
        img.paste(ic, (gx + ix * STEP + (span(w) - ic.width) // 2, gy + iy * STEP + (span(h) - ic.height) // 2), ic)
    # the pointed offer's tooltip beside the grid: Diablo's box, with its price
    bx = gx + span(GRID[0]) + 22
    rows = [("창 · 등급 8", WHITE, 17), ("무기 장비 · 크기 2×2", WHITE, 14), ("쿨다운 3.2초", WHITE, 14), ("앞에서 2번째 자리까지만 발동", WHITE, 14),
            ("앞의 적 2명에게 피해 6", BLUE, 14), ("전투마다 피로 +1", (180, 140, 230), 14), ("값 16 코인", GOLD, 17)]
    h_ = 18 + sum(px + 10 for _, _, px in rows)
    d.rectangle((bx, gy, x1 - 22, gy + h_), fill=(0, 0, 0))
    yy = gy + 14
    for t, col, px in rows:
        d.text(((bx + x1 - 22) / 2, yy + px / 2), t, font=F(px), fill=col, anchor="mm"); yy += px + 10
    d.rectangle((gx + 0 * STEP - 2, gy + 2 * STEP - 2, gx + span(2) + 1, gy + 2 * STEP + span(2) + 1), outline=(240, 220, 150), width=2)
    d.text(((x0 + x1) / 2, y1 - 52), "누르면 손에 듭니다 · 보드나 인벤토리에 놓으면 삽니다 · 든 아이템을 이 격자에 놓으면 팝니다", font=F(14), fill=GREY, anchor="mm")
    d.rectangle((x1 - 200, y1 - 38, x1 - 20, y1 - 10), fill=(46, 92, 170))
    d.text((x1 - 110, y1 - 24), "나가기", font=F(18), fill=WHITE, anchor="mm")
    return img


# ---- 3b. the merchant again (the user, 2026-10-10: "무기, 방어구기타, 물약을 구분하지 말고 한 화면에 보여주고, 새로고침 버튼 추가") ----------

POTIONS = ROOT / "Assets/@Art/Potion"
STOCK2 = [("item", "longsword", 0, 0, 3, 1, 14), ("item", "spear", 3, 0, 2, 2, 16), ("item", "dagger", 5, 0, 2, 1, 10), ("item", "ember_flask", 7, 0, 2, 1, 12),
          ("item", "buckler", 9, 0, 1, 1, 12), ("item", "herb_pouch", 5, 1, 2, 1, 10), ("bag", "leather_pouch", 7, 1, 3, 1, 12),
          ("item", "whetstone", 0, 1, 1, 1, 8), ("potion", "healing_potion", 1, 1, 1, 1, 8), ("potion", "barrier_potion", 2, 1, 1, 1, 8)]
GRID2 = (10, 5)
BAG_RIM = (78, 58, 42)


def vendor2():
    img = Image.open(GAME / "ko_42_shop.png").convert("RGB")
    d = ImageDraw.Draw(img)
    x0, y0, x1, y1 = WINDOW
    d.rectangle(WINDOW, fill=STONE, outline=EDGE, width=2)
    d.rectangle((x0 + 20, y0 + 16, x0 + 300, y0 + 52), fill=PLATE)
    d.text((x0 + 160, y0 + 34), "4층 · 상인", font=F(22), fill=GOLD, anchor="mm")
    d.text((x1 - 24, y0 + 34), "가진 코인 37", font=F(18), fill=BONE, anchor="rm")
    d.text((x0 + 22, y0 + 72), "지역 코인으로 삽니다. 무기·방어구·아이템·가방·포션을 한 곳에서 팝니다. 나가면 다음 층으로 갑니다.", font=F(15), fill=GREY, anchor="lm")
    gx, gy = x0 + 26, y0 + 100
    d.rectangle((gx - 6, gy - 6, gx + span(GRID2[0]) + 5, gy + span(GRID2[1]) + 5), fill=(34, 33, 31))
    d.rectangle((gx, gy, gx + span(GRID2[0]) - 1, gy + span(GRID2[1]) - 1), fill=LINE)
    for i in range(GRID2[0]):
        for j in range(GRID2[1]):
            d.rectangle((gx + i * STEP, gy + j * STEP, gx + i * STEP + SQ - 1, gy + j * STEP + SQ - 1), fill=EMPTY)
    for kind, oid, ix, iy, w, h, price in STOCK2:
        X0, Y0 = gx + ix * STEP, gy + iy * STEP
        if kind == "bag":
            d.rectangle((X0, Y0, X0 + span(w) - 1, Y0 + span(h) - 1), fill=BAG_RIM)
            for i in range(w):
                d.rectangle((X0 + 5 + i * (span(w) - 10 + 2) // w, Y0 + 5, X0 + 5 + (i + 1) * (span(w) - 10 + 2) // w - 3, Y0 + span(h) - 6), fill=EMPTY)
        else:
            for i in range(w):
                for j in range(h):
                    X, Y = X0 + i * STEP, Y0 + j * STEP
                    d.rectangle((X, Y, X + SQ - 1, Y + SQ - 1), fill=GROUND)
            src = POTIONS / f"{oid}.png" if kind == "potion" else ART / f"{oid}.png"
            ic = fit(Image.open(src).convert("RGBA"), span(w) - 10, span(h) - 10)
            img.paste(ic, (X0 + (span(w) - ic.width) // 2, Y0 + (span(h) - ic.height) // 2), ic)
        d.text((X0 + span(w) - 4, Y0 + span(h) - 3), str(price), font=F(13), fill=GOLD, anchor="rd", stroke_width=2, stroke_fill=(0, 0, 0))
    bx = gx + span(GRID2[0]) + 22
    rows = [("창 · 등급 8", WHITE, 17), ("무기 장비 · 크기 2×2", WHITE, 14), ("쿨다운 3.2초", WHITE, 14), ("앞에서 2번째 자리까지만 발동", WHITE, 14),
            ("앞의 적 2명에게 피해 6", BLUE, 14), ("전투마다 피로 +1", (180, 140, 230), 14), ("값 16 코인", GOLD, 17)]
    h_ = 18 + sum(px + 10 for _, _, px in rows)
    d.rectangle((bx, gy, x1 - 22, gy + h_), fill=(0, 0, 0))
    yy = gy + 14
    for tx, col, px in rows:
        d.text(((bx + x1 - 22) / 2, yy + px / 2), tx, font=F(px), fill=col, anchor="mm"); yy += px + 10
    d.rectangle((gx + 3 * STEP - 2, gy - 2, gx + 3 * STEP + span(2) + 1, gy + span(2) + 1), outline=(240, 220, 150), width=2)
    d.text(((x0 + x1) / 2, y1 - 64), "누르면 손에 듭니다 · 보드나 인벤토리에 놓으면 삽니다 · 포션은 누르면 빈 포션 칸으로", font=F(14), fill=GREY, anchor="mm")
    d.rectangle((x0 + 20, y1 - 46, x0 + 220, y1 - 12), fill=(40, 42, 50), outline=(80, 80, 90))
    d.text((x0 + 120, y1 - 29), "새로고침  ● 3", font=F(18), fill=WHITE, anchor="mm")
    d.rectangle((x1 - 200, y1 - 46, x1 - 20, y1 - 12), fill=(46, 92, 170))
    d.text((x1 - 110, y1 - 29), "나가기", font=F(18), fill=WHITE, anchor="mm")
    return img


def half(img, box): return img.crop(box).resize(((box[2] - box[0]) * 3 // 4, (box[3] - box[1]) * 3 // 4), Image.LANCZOS)


def main():
    S.sheet([("지금: 상점 창의 물건 넷(타일)", half(vendor_now(), (972, 92, 1908, 826))),
             ("상인 2차안: 한 화면(탭 없음)에 무기·방어구·아이템·가방·포션, 새로고침 버튼", half(vendor2(), (972, 92, 1908, 826)))],
            2, HERE / "mock-vendor-v2.png", title="상점을 디아블로 2 상인처럼 — 2차 (Round 55)", label=19,
            footer=["물건 여덟(가방 포함)과 포션 둘이 한 격자(10×5)에. 각 조각의 오른쪽 아래에 값, 가리킨 물건은 툴팁에 값까지. 새로고침은 지금처럼(3코인부터 +2).",
                    "그린 것은 오른쪽의 창뿐이다(지도·파티는 지금 게임). 호출 없음."])
    vendor2().save(HERE / "mock-vendor-v2-full.png")
    S.sheet([("지금: 바닥의 상자 150×70에 맞춤 — 아이템마다 아이템 창의 0.73~2.26배", floor_strip(False)),
             ("통일(권장): 아이템 창의 칸 크기 그대로(2×1은 102×50 안) — 바닥의 기울기 −12°는 그대로", floor_strip(True))],
            1, HERE / "mock-floor-icons.png", title="바닥 전리품 아이콘의 크기 (Round 55)", label=19,
            footer=["바닥의 판(이름)과 빛은 그대로. 마우스에 든 것(Round 54)과 인벤토리·보드의 크기가 같아진다."])
    S.sheet([("지금: 드랍을 든 채 인벤토리 위 — 빨강(버튼으로만 들어감)", half(inventory_scene(True), (0, 92, 1920, 820))),
             ("제안: 초록 — 그 칸을 누르면 그 자리로 들어감", half(inventory_scene(False), (0, 92, 1920, 820)))],
            1, HERE / "mock-drop-to-inventory.png", title="드랍을 인벤토리의 칸에 바로 (Round 55)", label=19,
            footer=["흰 고리는 마우스 자리 표시용. 바닥의 쥐 발톱은 들어 올려 마우스에 있다(Round 54)."])
    S.sheet([("지금: 상점 창의 물건 넷(타일)", half(vendor_now(), (972, 92, 1908, 826))),
             ("디아블로 2 상인(안): 격자에 물건을 깔고 탭으로 나눔, 가리킨 물건의 툴팁과 값", half(vendor(), (972, 92, 1908, 826)))],
            2, HERE / "mock-vendor.png", title="상점을 디아블로 2 상인처럼 (Round 55)", label=19,
            footer=["오른쪽은 무기 탭의 물건 열넷(10×7 격자). 판매(든 아이템을 격자에 놓음)와 탭은 판정할 것."])


def vendor_now():
    return Image.open(GAME / "ko_42_shop.png").convert("RGB")


if __name__ == "__main__":
    main()
