# -*- coding: utf-8 -*-
"""Round 47: the loot picked on the battle screen right after the win, not on a screen of its own (the user's 2026-10-08 ask), and
an item's facts on a right click. Mockups over a capture of the battle screen just after a won battle (its result window hidden;
captured once by a temporary PlayMode test, since removed): A the result window holds the drops, B the drops stand where the enemy
boards stood, C the drops lie on the stage's floor. No API call.
  .venv/bin/python ArtPipeline/Archive/47-loot-in-battle/mock_loot_battle.py
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[3]; HERE = Path(__file__).resolve().parent
BASE = Path.home() / "Library/Caches/F1/screenshots/20261008-r47base"
SHOTS = Path.home() / "Library/Caches/F1/screenshots/20261008-r46c"
sys.path.insert(0, str(ROOT / "ArtPipeline/Archive/44-shop-node"))
sys.path.insert(0, str(ROOT / "ArtPipeline/Archive/46-right-column"))
import mock_shop as M     # noqa: E402  fonts, palette, the coin, slots, cards, the mouse glyph
import mock_layout as L   # noqa: E402  the kit's frames as Unity draws them, the screen's gloom, buttons

font = M.font
TEXT, DIM, INK, BRASS, GOOD, VIRTUE, FATIGUE = M.TEXT, M.DIM, M.INK, M.BRASS, M.GOOD, M.VIRTUE, M.FATIGUE
BUTTON, QUIET = M.BUTTON, M.QUIET
ENEMY_NAME = (0xF2, 0x8C, 0x7E); INK_TEXT_DIM = (0x6B, 0x55, 0x40)

COLS = [120, 310, 500, 690]                 # the party's board columns (row 4 .. row 1)
CELL_TOP, CELL_STEP, CELL_W, CELL_H = 646, 62, 180, 60
DROP_COLS = [1050, 1240, 1430]              # where the enemies' boards stood (row 1 .. row 3)
WEAPONS = ["longsword", "healing_staff", "mace", "greataxe"]  # Kai, Ella, Cedric, Astrid


def base(name="end_clean"): return Image.open(BASE / f"{name}.png").convert("RGBA")
def shot(name): return Image.open(SHOTS / f"{name}.png").convert("RGBA")


RUSTY = L.item("rusty_blade", "녹슨 칼", "무기 장비", 1, "2.8", "앞에서 2번째 자리까지만 발동", ["맨 앞 적에게 피해 6"], 1)
BUCKLER = L.item("buckler", "버클러", "방어 장비", 1, "5.0", "맨 앞에서만 발동", ["자신에게 보호막 6"], 1, tier="bronze")


def calm_header(img):
    """After the win the retreat and the speeds have nothing to do: the header keeps its title and captions only."""
    plain = img.crop((1150, 6, 1180, 78)).resize((310, 72))
    img.paste(plain, (36, 6)); img.paste(plain.resize((400, 72)), (1515, 6))


def party_side_cells(img, picked=None, dim_filled=False):
    """The boards turn to the party side's look (bright cells, "빈 칸") so they read as places to put things."""
    for c, x in enumerate(COLS):
        for k in range(5):
            y = CELL_TOP + k * CELL_STEP
            if k == 0:
                it = L.item(WEAPONS[c], "", "무기 장비", 1, "", None, [], None)
                cell = M.board_cell(it)
                if dim_filled:
                    cell = Image.blend(cell, Image.new("RGBA", cell.size, (0, 0, 0, 255)), 0.2)
                img.alpha_composite(cell, (x, y))
            else:
                cell = M.nine_slice(M.sprite("slot"), 8, (CELL_W, CELL_H))
                d = ImageDraw.Draw(cell); d.text((14, 18), "빈 칸", font=font(19), fill=INK_TEXT_DIM)
                img.alpha_composite(cell, (x, y))


def drop_column(img, x, it, source, picked=False, taken=False):
    """A drop as a board of its own where the enemy's board stood: a head naming whose it was, the cell (brass while picked)."""
    img.paste(img.crop((118, 622, 302, 645)), (x - 2, 622))
    plain = img.crop((260, 627, 290, 640)).resize((CELL_W - 8, 13))
    img.paste(plain, (x + 4, 627))
    d = ImageDraw.Draw(img); d.text((x + 10, 625), source, font=font(14), fill=ENEMY_NAME)
    frame = img.crop((118, 644, 302, 710)); img.paste(frame, (x - 2, 644))
    img.paste(img.crop((118, 955, 302, 963)), (x - 2, 708))
    if taken:
        cell = M.nine_slice(M.sprite("slot"), 8, (CELL_W, CELL_H)); cell = Image.blend(cell, Image.new("RGBA", cell.size, (20, 20, 24, 255)), 0.55)
        dd = ImageDraw.Draw(cell); dd.text((60, 17), "주움", font=font(19), fill=DIM)
    else:
        cell = M.board_cell(it, selected=picked)
    img.alpha_composite(cell, (x, CELL_TOP))


def cover_skulls(img):
    img.paste(img.crop((1420, 960, 1510, 1030)), (1515, 960))


def banner(layer, lines, box=(560, 100, 800, 74)):
    x, y, w, h = box
    layer.alpha_composite(L.sliced("Frame/plate_label", 44, (w, h)), (x, y))
    d = ImageDraw.Draw(layer); cx = x + 32
    for s, size, color in lines:
        f = font(size); l, t, r, b = d.textbbox((0, 0), s, font=f)
        d.text((cx - l, y + h / 2 - (b - t) / 2 - t), s, font=f, fill=color); cx += (r - l) + 22


def side_card(img, it, cell_box, left=False):
    """Round 42's card beside the cell (the battle's place), its notch on the edge facing the cell."""
    card = M.card(it, notch_x=None, with_price=False); W, H = card.size
    cx0, cy0, cx1, cy1 = cell_box
    x = cx0 - 10 - W - 12 if left else cx1 + 10 + 12
    y = max(630, min(cy0 - 8, 1080 - 12 - H))
    out = Image.new("RGBA", (W + 12, H), (0, 0, 0, 0)); ox = 0 if left else 12
    out.alpha_composite(card, (ox, 0))
    d = ImageDraw.Draw(out); ny = (cy0 + cy1) / 2 - y
    if left:
        d.polygon([(W - 1, ny - 10), (W + 11, ny), (W - 1, ny + 10)], fill=INK + (240,)); d.line([(W - 1, ny - 10), (W + 11, ny), (W - 1, ny + 10)], fill=BRASS + (235,), width=1)
    else:
        d.polygon([(12, ny - 10), (0, ny), (12, ny + 10)], fill=INK + (240,)); d.line([(12, ny - 10), (0, ny), (12, ny + 10)], fill=BRASS + (235,), width=1)
    M.place_card(img, out, x - (0 if left else 12), y)
    return (x, y, x + W, y + H)


def above_card(img, it, cell_box):
    """Round 42's card as the party side places it: above the board panel, centred on the cell's column, the notch down at it."""
    card = M.card(it, notch_x=None, with_price=False); W, H = card.size
    cx0, cy0, cx1, cy1 = cell_box; mid = (cx0 + cx1) / 2
    x = max(12, min(1920 - 12 - W, mid - W / 2)); y = 620 - 8 - H - 12
    out = Image.new("RGBA", (W, H + 12), (0, 0, 0, 0)); out.alpha_composite(card, (0, 0))
    d = ImageDraw.Draw(out); nx = mid - x
    d.polygon([(nx - 10, H - 1), (nx, H + 11), (nx + 10, H - 1)], fill=INK + (240,))
    d.line([(nx - 10, H - 1), (nx, H + 11), (nx + 10, H - 1)], fill=BRASS + (235,), width=1)
    M.place_card(img, out, round(x), round(y))


def note(img, xy, text, color=VIRTUE):
    d = ImageDraw.Draw(img); f = font(20); l, t, r, b = d.textbbox((0, 0), text, font=f)
    x, y = xy; d.rounded_rectangle((x - 10, y - 8, x + (r - l) + 10, y + (b - t) + 10), radius=6, fill=INK + (230,), outline=BRASS + (255,))
    d.text((x - l, y - t), text, font=f, fill=color)


# ---- the three places for the drops -------------------------------------------------------------------------------------------

RESULT = [("승리", 34, GOOD), ("전사자 없음", 22, TEXT), ("지역 코인 +4", 22, VIRTUE)]


def panel_buttons(layer, picked):
    L.button(layer, (1050, 990, 1290, 1054), "인벤토리에 넣기", QUIET, size=24, dim=not picked)
    L.button(layer, (1310, 990, 1490, 1054), "로그 보기", QUIET, size=24)
    L.button(layer, (1640, 990, 1880, 1054), "계속", BUTTON, size=30)


def screen_b(picked=False, card=False, taken=False, two=False):
    """B (recommended): the result as a strip over the stage; the drops where the enemies' boards stood, each a board of one cell
    under the name of whose it was; the boards' own look on the left; the buttons along the panel's bottom right."""
    img = base(); calm_header(img); cover_skulls(img)
    party_side_cells(img)
    drops = [(RUSTY, "고블린 약탈자")] + ([(BUCKLER, "고블린 고참")] if two else [])
    for k, (it, who) in enumerate(drops):
        drop_column(img, DROP_COLS[k], it, who, picked=(picked and k == 0 and not taken), taken=(taken and k == 0))
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    banner(layer, RESULT + [("전리품 " + str(len(drops)), 22, TEXT)])
    panel_buttons(layer, picked and not taken)
    d = ImageDraw.Draw(layer)
    hint = "고른 전리품을 왼쪽 보드의 칸에 놓으세요. 우클릭은 아이템 정보입니다." if picked and not taken else "전리품을 누르면 고릅니다. 남은 것은 계속을 누르면 두고 갑니다. 우클릭은 아이템 정보입니다."
    L.text_lines(d, 1050, 900, hint, font(19), DIM, 830, gap=6)
    img.alpha_composite(L.vignetted(layer))
    if picked and not taken:
        # Where it can go: the empty cells stay bright, the weapons dim (they would be pushed to the inventory: still allowed).
        pass
    if card:
        above_card(img, RUSTY, (DROP_COLS[0], CELL_TOP, DROP_COLS[0] + CELL_W, CELL_TOP + CELL_H))
        M.mouse_glyph(img, DROP_COLS[0] + 150, CELL_TOP + 30, button="right", scale=0.8)
    return img


def screen_a():
    """A: the result window, smaller and up over the stage, holds the drops (a cell, the name, "줍기"); the boards below take them."""
    img = base(); calm_header(img); cover_skulls(img); party_side_cells(img)
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    x, y, w, h = 520, 120, 880, 420
    shade = Image.new("RGBA", img.size, (0, 0, 0, 0)); ImageDraw.Draw(shade).rectangle([0, 84, 1920, 619], fill=(0, 0, 0, 120)); layer.alpha_composite(shade)
    layer.alpha_composite(L.sliced("Frame/plate_label", 44, (w, h)), (x, y))
    d = ImageDraw.Draw(layer)
    M_center(d, x + w / 2, y + 30, "승리", 44, GOOD)
    M_center(d, x + w / 2, y + 96, "전사자 없음 · 지역 코인 +4", 22, TEXT)
    d.text((x + 40, y + 150), "전리품", font=font(20), fill=DIM)
    tx, ty = x + 40, y + 184
    d.rounded_rectangle((tx, ty, tx + 800, ty + 96), radius=8, fill=M.SLOT + (255,), outline=M.SELECTED + (255,), width=3)
    layer.alpha_composite(M.board_cell(RUSTY, selected=False), (tx + 14, ty + 18))
    M.draw_segments(d, tx + 214, ty + 22, M.title_segments(RUSTY), font(22))
    d.text((tx + 214, ty + 56), "무기 장비 · 1칸", font=font(17), fill=DIM)
    d.text((tx + 560, ty + 32), "넣을 칸을 누르세요", font=font(22), fill=VIRTUE)
    L.button(layer, (x + 40, y + h - 96, x + 300, y + h - 32), "로그 보기", QUIET, size=24)
    L.button(layer, (x + w - 300, y + h - 96, x + w - 40, y + h - 32), "계속", BUTTON, size=30)
    L.button(layer, (1050, 990, 1290, 1054), "인벤토리에 넣기", QUIET, size=24)
    L.text_lines(d, 1050, 900, "고른 전리품을 왼쪽 보드의 칸에 놓으세요. 우클릭은 아이템 정보입니다.", font(19), DIM, 830, gap=6)
    img.alpha_composite(L.vignetted(layer))
    return img


def M_center(d, cx, y, s, size, color):
    f = font(size); l, t, r, b = d.textbbox((0, 0), s, font=f); d.text((cx - (r - l) / 2 - l, y - t), s, font=f, fill=color)


def floor_drop(img, it, cx, floor_y, picked=False):
    """C: a drop lying on the floor where its enemy fell: a soft light, the icon on its side, a name plate above (Diablo's ground loot)."""
    glow = Image.new("RGBA", img.size, (0, 0, 0, 0)); g = ImageDraw.Draw(glow)
    g.ellipse((cx - 110, floor_y - 22, cx + 110, floor_y + 22), fill=(255, 220, 150, 120 if picked else 70))
    img.alpha_composite(glow.filter(ImageFilter.GaussianBlur(12)))
    icon = M.icon_of(it, (150, 70)).rotate(-12, expand=True, resample=Image.BICUBIC)
    img.alpha_composite(icon, (round(cx - icon.size[0] / 2), round(floor_y - icon.size[1] + 14)))
    d = ImageDraw.Draw(img); f = font(20); segs = M.title_segments(it); tw = sum(f.getlength(s) for s, _ in segs)
    px0, py0 = cx - tw / 2 - 14, floor_y - 120
    d.rounded_rectangle((px0, py0, px0 + tw + 28, py0 + 36), radius=5, fill=INK + (235,), outline=(VIRTUE if picked else BRASS) + (255,), width=2 if picked else 1)
    M.draw_segments(d, px0 + 14, py0 + 7, segs, f)
    if picked:
        M_center(d, cx, py0 - 30, "넣을 칸을 누르세요", 18, VIRTUE)


def screen_c():
    """C: the drops lie on the stage's floor where the enemies fell, under a name plate each; a click picks one."""
    img = base(); calm_header(img); cover_skulls(img); party_side_cells(img)
    floor_drop(img, RUSTY, 1330, 560, picked=True)
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    banner(layer, RESULT + [("전리품 1", 22, TEXT)])
    panel_buttons(layer, True)
    d = ImageDraw.Draw(layer)
    L.text_lines(d, 1050, 900, "고른 전리품을 왼쪽 보드의 칸에 놓으세요. 우클릭은 아이템 정보입니다.", font(19), DIM, 830, gap=6)
    img.alpha_composite(L.vignetted(layer))
    return img


def now_pair():
    """Today: the result window, then the loot screen (round 46's tiles)."""
    a = base("end_result").convert("RGB").resize((960, 540), Image.LANCZOS)
    b = shot("ko_07_loot").convert("RGB").resize((960, 540), Image.LANCZOS)
    out = Image.new("RGB", (1920, 1080), (18, 18, 18)); out.paste(a, (0, 270)); out.paste(b, (960, 270))
    d = ImageDraw.Draw(out)
    d.text((20, 220), "① 결과 창", font=font(30), fill=TEXT); d.text((980, 220), "② 계속 → 전리품 화면", font=font(30), fill=TEXT)
    return out


def main():
    shots = {
        "mock-A-result-window.png": screen_a(),
        "mock-B-enemy-boards.png": screen_b(picked=True),
        "mock-C-floor.png": screen_c(),
        "mock-B1-won.png": screen_b(two=True),
        "mock-B2-picked.png": screen_b(picked=True, two=True),
        "mock-B3-right-click.png": screen_b(picked=True, card=True, two=True),
        "mock-B4-one-taken.png": screen_b(taken=True, two=True),
    }
    for name, im in shots.items(): im.convert("RGB").save(HERE / name); print(name)

    def half(im): return im.convert("RGB").resize((960, 540), Image.LANCZOS)
    M.sheet([("지금: 결과 창 → 전리품 화면 (두 화면)", half(now_pair())),
             ("A: 결과 창 안에 전리품 (무대를 덮음)", half(shots["mock-A-result-window.png"])),
             ("B (권장): 적의 보드 자리에 전리품", half(shots["mock-B-enemy-boards.png"])),
             ("C: 무대 바닥에 떨어진 전리품 (디아블로식)", half(shots["mock-C-floor.png"]))],
            2, HERE / "mock-compare.png", size=24,
            footer=["모두 전투 화면에서 바로 고른다: 좌클릭 = 고르기, 칸을 누르면 넣기(합치기·밀어내기 그대로), 우클릭 = 아이템 카드. 승리의 말(전사자·코인)은 무대 위 띠(A는 창).",
                    "보드는 이긴 뒤 파티 쪽 모습(밝은 칸, \"빈 칸\")으로 바뀌어 놓을 자리로 읽힌다. 후퇴·배속 버튼은 감춘다. 버튼: 인벤토리에 넣기 · 로그 보기 · 계속(남은 것은 두고 감)."])
    M.sheet([("B-1 이긴 직후 (정예: 드랍 둘)", half(shots["mock-B1-won.png"])),
             ("B-2 하나를 고름 (놋쇠 칸)", half(shots["mock-B2-picked.png"])),
             ("B-3 우클릭: 아이템 카드 (보드 판 위, 아래 꼭지)", half(shots["mock-B3-right-click.png"])),
             ("B-4 하나를 주움 (\"주움\"으로 남음)", half(shots["mock-B4-one-taken.png"]))],
            2, HERE / "mock-B-flow.png", size=24)


if __name__ == "__main__": main()
