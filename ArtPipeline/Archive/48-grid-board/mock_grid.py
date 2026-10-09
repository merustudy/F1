# -*- coding: utf-8 -*-
"""Round 48: the mercenary's item board as a grid of squares (the user's 2026-10-08 ask: "split the long horizontal cell into
about three squares, like Diablo's inventory, toward the Backpack Battles format"). Mockups over the screenshots of
20261008-r47b: the board columns keep their place and size (180 wide, the five 60-high cells of today) and are drawn again as
3 x 5 squares of 58 with gaps of 3. Items keep their icons (cut to their drawn part and fitted, no new picture); their shapes
follow the icons. No API call.
  .venv/bin/python ArtPipeline/Archive/48-grid-board/mock_grid.py
"""
import math
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter, ImageChops

ROOT = Path(__file__).resolve().parents[3]; HERE = Path(__file__).resolve().parent
SHOTS = Path.home() / "Library/Caches/F1/screenshots/20261008-r47b"
sys.path.insert(0, str(ROOT / "ArtPipeline/Archive/44-shop-node"))
import mock_shop as M  # noqa: E402  (round 44: fonts, palette, nine-slice, kit sprites, tier marks, sheets)

font = M.font
TEXT, DIM, INK, BRASS, VIRTUE, FATIGUE, GOOD, DANGER = M.TEXT, M.DIM, M.INK, M.BRASS, M.VIRTUE, M.FATIGUE, M.GOOD, M.DANGER
PANEL_BG = (27, 31, 41)
CHARGE = (255, 214, 140)

# ---- geometry: today's board columns (cells 180 x 60, gap 2, five rows from y 646) as a 3 x 5 grid ------------------------------
S, G = 58, 3                       # a square and the gap: 3 x 58 + 2 x 3 = 180, 5 x 58 + 4 x 3 = 302 (today's five rows: 308)
PARTY_X = [120, 310, 500, 690]     # the four party columns, 4th row to 1st (left to right)
ENEMY_X = [1050, 1240]             # the two rats' boards in the battle shot
TOP = 649                          # the grid's top: centred in today's 646..954
AREA = (646, 954)

# Shapes (width x height in squares). Rule of the proposal: the shape follows the icon already drawn (its width / height),
# and today's size N stays N rows high. No icon is drawn again.
SHAPES = {
    "buckler": (1, 1), "rat_bite": (1, 1),
    "dagger": (2, 1), "herb_pouch": (2, 1), "ember_flask": (2, 1), "overseer_roar": (2, 1),
    "longsword": (3, 1), "greataxe": (3, 1), "mace": (3, 1), "healing_staff": (3, 1), "fire_staff": (3, 1), "ward_charm": (3, 1),
    "mending_chant": (3, 1), "rusty_blade": (3, 1), "crude_bow": (3, 1), "lantern_staff": (3, 1), "overseer_maul": (3, 1),
    "longbow": (3, 2), "spear": (3, 2), "halberd": (3, 3),
}
NAMES = {
    "buckler": "버클러", "rat_bite": "쥐 송곳니", "dagger": "단검", "herb_pouch": "약초 주머니", "ember_flask": "불씨 플라스크",
    "overseer_roar": "감독관의 포효", "longsword": "롱소드", "greataxe": "대도끼", "mace": "메이스", "healing_staff": "치유의 지팡이",
    "fire_staff": "화염 지팡이", "ward_charm": "수호 부적", "mending_chant": "치유의 부적", "rusty_blade": "녹슨 칼", "crude_bow": "조잡한 활",
    "lantern_staff": "등불 지팡이", "overseer_maul": "감독관의 망치", "longbow": "장궁", "spear": "창", "halberd": "미늘창",
}
SIZE_NOW = {k: 1 for k in SHAPES} | {"longbow": 2, "spear": 2, "halberd": 3}
ENEMY_ONLY = {"rat_bite", "rusty_blade", "crude_bow", "lantern_staff", "mending_chant", "overseer_maul", "overseer_roar"}


def shot(name): return Image.open(SHOTS / f"{name}.png").convert("RGBA")
def px(n): return n * S + (n - 1) * G
def cell_xy(x0, gx, gy): return x0 + gx * (S + G), TOP + gy * (S + G)


def it(id, x, y, tier="common", fat=False, sel=False, rot=False, flip=False, charge=None):
    w, h = SHAPES[id]
    if rot: w, h = h, w
    return dict(id=id, x=x, y=y, w=w, h=h, tier=tier, fat=fat, sel=sel, rot=rot, flip=flip, charge=charge)


# ---- pieces ---------------------------------------------------------------------------------------------------------------------

def icon(id, rot=False, flip=False):
    im = Image.open(M.ITEM / f"{id}.png").convert("RGBA")
    im = im.crop(im.split()[3].point(lambda a: 255 if a > 8 else 0).getbbox())
    if flip: im = im.transpose(Image.FLIP_LEFT_RIGHT)
    if rot: im = im.rotate(90, expand=True)
    return im


def fit(im, box):
    r = min(box[0] / im.width, box[1] / im.height)
    return im.resize((max(1, round(im.width * r)), max(1, round(im.height * r))), Image.LANCZOS)


def tinted(im, rgb):
    out = ImageChops.multiply(im.convert("RGB"), Image.new("RGB", im.size, rgb)).convert("RGBA"); out.putalpha(im.split()[3]); return out


def socket(battle=False):
    """An empty square: the kit's slot darkened like the battle's empty cell (78, 69, 56)."""
    return tinted(M.nine_slice(M.sprite("slot"), 8, (S, S)), (98, 94, 90) if not battle else (92, 86, 80))


def fatigue_tag(img, right, top):
    tag = M.nine_slice(M.sprite("fatigue_tag"), 10, (30, 20)); img.alpha_composite(tag, (right - 30, top))
    d = ImageDraw.Draw(img); f = font(14); l, t, r, b = d.textbbox((0, 0), "+1", font=f)
    d.text((right - 15 - (r - l) / 2 - l, top + 10 - (b - t) / 2 - t), "+1", font=f, fill=FATIGUE)


def tile(i, battle=False):
    W, H = px(i["w"]), px(i["h"])
    base = M.nine_slice(M.sprite("slot_selected" if i["sel"] else "slot"), 8, (W, H))
    ic = fit(icon(i["id"], i["rot"], i["flip"]), (W - 12, H - 12)); xy = ((W - ic.width) // 2, (H - ic.height) // 2)
    if i["tier"] != "common": M.tier_marks(base, ic, xy, i["tier"])
    else: base.alpha_composite(ic, xy)
    if i["fat"] and not battle: fatigue_tag(base, W - 4, 4)
    if i["charge"] is not None: base = charged(base, i["charge"])
    return base


def charged(t, frac):
    """The cooldown as light (Architecture/12 "쿨다운 = 빛"): the charged part warm, the rest dark, a bright front."""
    W, H = t.size; cut = int(round(W * frac)); a = t.split()[3]
    over = Image.new("RGBA", (W, H), (0, 0, 0, 0)); d = ImageDraw.Draw(over)
    d.rectangle([0, 0, cut, H], fill=CHARGE + (40,)); d.rectangle([cut, 0, W, H], fill=(16, 12, 10, 150))
    if 0 < cut < W:
        for k in range(18): d.line([(cut - k, 0), (cut - k, H)], fill=CHARGE + (int(90 * (1 - k / 18)),))
        d.line([(cut, 0), (cut, H)], fill=CHARGE + (235,), width=2)
    out = t.copy(); out.alpha_composite(over); out.putalpha(a); return out


def clear_column(img, x0, top=AREA[0], bottom=AREA[1]):
    ImageDraw.Draw(img).rectangle([x0 - 1, top - 1, x0 + 181, bottom + 1], fill=(36, 32, 30, 255))


def board(img, x0, items, rows=5, battle=False, cells=None, flip=False):
    """A grid board: dark sockets where nothing lies, a parchment tile per item over its squares. `cells` limits the usable squares."""
    clear_column(img, x0, TOP - 3, TOP + px(rows) + 3) if rows != 5 else clear_column(img, x0)
    taken = set()
    for i in items:
        for dx in range(i["w"]):
            for dy in range(i["h"]): taken.add((i["x"] + dx, i["y"] + dy))
    for gy in range(rows):
        for gx in range(3):
            if (gx, gy) in taken or (cells is not None and (gx, gy) not in cells): continue
            img.alpha_composite(socket(battle), cell_xy(x0, gx, gy))
    for i in items:
        if flip: i = dict(i, flip=True)
        img.alpha_composite(tile(i, battle), cell_xy(x0, i["x"], i["y"]))


def ghost(img, x0, gx, gy, w, h, color, icon_id=None, rot=False, label=None):
    X, Y = cell_xy(x0, gx, gy); W, H = px(w), px(h)
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0)); d = ImageDraw.Draw(layer)
    for dx in range(w):
        for dy in range(h):
            cx, cy = cell_xy(x0, gx + dx, gy + dy)
            d.rounded_rectangle([cx, cy, cx + S - 1, cy + S - 1], radius=4, fill=color + (105,), outline=color + (255,), width=2)
    img.alpha_composite(layer)
    if icon_id:
        ic = fit(icon(icon_id, rot), (W - 12, H - 12)); a = ic.split()[3].point(lambda v: int(v * 0.75)); ic.putalpha(a)
        img.alpha_composite(ic, (X + (W - ic.width) // 2, Y + (H - ic.height) // 2))
    if label:
        d = ImageDraw.Draw(img); f = font(16); tw = d.textlength(label, font=f)
        lx = min(max(X + W / 2 - tw / 2, x0 - 6), x0 + 186 - tw)
        d.rounded_rectangle([lx - 6, Y - 26, lx + tw + 6, Y - 4], radius=4, fill=INK + (235,), outline=color + (255,), width=1)
        d.text((lx, Y - 25), label, font=f, fill=color)


def cursor(img, x, y, held=None, rot=False):
    """The pointer; with an item in hand, the item rides under it (Diablo: the item follows the cursor)."""
    if held:
        w, h = SHAPES[held]
        if rot: w, h = h, w
        ic = fit(icon(held, rot), (px(w) - 12, px(h) - 12)); a = ic.split()[3].point(lambda v: int(v * 0.9)); ic.putalpha(a)
        sh = Image.new("RGBA", ic.size, (0, 0, 0, 0)); sh.putalpha(a.point(lambda v: int(v * 0.5)))
        img.alpha_composite(sh.filter(ImageFilter.GaussianBlur(4)), (x - ic.width // 2 + 4, y - ic.height // 2 + 6))
        img.alpha_composite(ic, (x - ic.width // 2, y - ic.height // 2))
    d = ImageDraw.Draw(img); s = 1.15
    pts = [(0, 0), (0, 26), (7, 20), (12, 31), (16, 29), (11, 18), (20, 18)]
    d.polygon([(x + p[0] * s, y + p[1] * s) for p in pts], fill=(250, 250, 245), outline=(10, 10, 10))


def star(img, cx, cy, lit=True, r=9):
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0)); d = ImageDraw.Draw(layer)
    if lit:
        g = Image.new("RGBA", img.size, (0, 0, 0, 0)); ImageDraw.Draw(g).ellipse([cx - 16, cy - 16, cx + 16, cy + 16], fill=CHARGE + (150,))
        layer.alpha_composite(g.filter(ImageFilter.GaussianBlur(6)))
    pts = []
    for k in range(8):
        rr = r if k % 2 == 0 else r * 0.38
        ang = k * 3.14159 / 4 - 3.14159 / 2
        pts.append((cx + rr * math.cos(ang), cy + rr * math.sin(ang)))
    d.polygon(pts, fill=(VIRTUE if lit else (96, 92, 86)) + (255,), outline=INK + (255,))
    img.alpha_composite(layer)


def edge_star(img, x0, gx, gy, side, lit=True):
    """A star on the side of a square (the Backpack Battles mark: what touches this side gets the effect)."""
    cx, cy = cell_xy(x0, gx, gy); cx += S / 2; cy += S / 2; half = (S + G) / 2
    dx, dy = {"l": (-half, 0), "r": (half, 0), "u": (0, -half), "d": (0, half)}[side]
    star(img, cx + dx, cy + dy, lit)


def bag(img, x0, gx, gy, w, h, color=(96, 64, 38)):
    """A bag of the C variant: leather under its squares, stitched; the squares outside every bag are not there."""
    X, Y = cell_xy(x0, gx, gy); W, H = px(w), px(h)
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([X - 4, Y - 4, X + W + 3, Y + H + 3], radius=8, fill=color + (255,), outline=(34, 22, 14, 255), width=2)
    stitch = tuple(min(255, c + 70) for c in color)
    for k in range(int(X) + 2, int(X + W), 8): d.line([(k, Y - 1), (k + 4, Y - 1)], fill=stitch + (255,), width=1); d.line([(k, Y + H), (k + 4, Y + H)], fill=stitch + (255,), width=1)
    for k in range(int(Y) + 2, int(Y + H), 8): d.line([(X - 1, k), (X - 1, k + 4)], fill=stitch + (255,), width=1); d.line([(X + W, k), (X + W, k + 4)], fill=stitch + (255,), width=1)


def no_room(img, x0, cells):
    d = ImageDraw.Draw(img)
    for gx, gy in cells:
        cx, cy = cell_xy(x0, gx, gy)
        for k in range(0, S, 7):
            d.line([(cx + k, cy), (cx + min(k + 3, S - 1), cy)], fill=(70, 66, 62, 255)); d.line([(cx + k, cy + S - 1), (cx + min(k + 3, S - 1), cy + S - 1)], fill=(70, 66, 62, 255))
            d.line([(cx, cy + k), (cx, cy + min(k + 3, S - 1))], fill=(70, 66, 62, 255)); d.line([(cx + S - 1, cy + k), (cx + S - 1, cy + min(k + 3, S - 1))], fill=(70, 66, 62, 255))


def panel_lines(img, first, second, heading=None):
    """The right panel's two description lines (Round 46: two lines under the heading)."""
    d = ImageDraw.Draw(img)
    if heading:
        d.rectangle([995, 862, 1888, 906], fill=PANEL_BG + (255,)); d.text((1000, 864), heading[0], font=font(34), fill=TEXT)
        if heading[1]: d.text((1000 + d.textlength(heading[0], font=font(34)) + 24, 878), heading[1], font=font(20), fill=DIM)
    d.rectangle([995, 910, 1888, 962], fill=PANEL_BG + (255,))
    for y, line in ((913, first), (937, second)):
        size = 19   # like the game: a long line gets smaller letters (19 down to 14)
        while size > 14 and sum(d.textlength(seg, font=font(size)) for seg, _ in line) > 880: size -= 1
        x = 1000
        for seg, color in line:
            d.text((x, y + (19 - size) / 2), seg, font=font(size), fill=color); x += d.textlength(seg, font=font(size))


def head_fatigue(img, x0, n):
    """Repaint the board head's fatigue sum ("피로 +N") for the column at x0 (0 removes it)."""
    img.alpha_composite(HEAD_PATCH, (x0 + 110, 626)); d = ImageDraw.Draw(img)
    if n:
        f = font(13); label = f"피로 +{n}"; tw = d.textlength(label, font=f); w = tw + 16
        tag = M.nine_slice(M.sprite("fatigue_tag"), 10, (int(w), 18)); img.alpha_composite(tag, (int(x0 + 176 - w), 625))
        d.text((x0 + 176 - w + 8, 626), label, font=f, fill=FATIGUE)


HEAD_PATCH = shot("ko_35_map_tiers").crop((230, 626, 299, 640))   # a stretch of the board head without a tag (미라's)


def board_now(img, x0, items):
    """Today's board for the same items: bands of 180 x 60 stacked in order, size N = N bands, empty bands say "빈 칸"."""
    clear_column(img, x0); y = AREA[0]; used = 0
    for i in items:
        n = SIZE_NOW[i["id"]]; W, H = 180, 60 * n + 2 * (n - 1)
        cell = M.nine_slice(M.sprite("slot_selected" if i["sel"] else "slot"), 8, (W, H))
        ic = fit(icon(i["id"]), (164, H - 12)); xy = ((W - ic.width) // 2, (H - ic.height) // 2)
        if i["tier"] != "common": M.tier_marks(cell, ic, xy, i["tier"])
        else: cell.alpha_composite(ic, xy)
        if i["fat"]: fatigue_tag(cell, W - 5, 5)
        img.alpha_composite(cell, (x0, y)); y += H + 2; used += n
    for _ in range(5 - used):
        cell = M.nine_slice(M.sprite("slot"), 8, (180, 60)); ImageDraw.Draw(cell).text((14, 18), "빈 칸", font=font(19), fill=(100, 80, 61))
        img.alpha_composite(cell, (x0, y)); y += 62


def map_now():
    img = shot("ko_35_map_tiers")
    for x0, items in zip(PARTY_X, loadout_a()): board_now(img, x0, items)
    for x0, n in zip(PARTY_X, (0, 0, 1, 2)): head_fatigue(img, x0, n)
    return img


# ---- the screens ----------------------------------------------------------------------------------------------------------------

def loadout_a():
    return [
        [it("fire_staff", 0, 0), it("ember_flask", 0, 1), it("herb_pouch", 0, 2)],                                   # 미라 (4열)
        [it("healing_staff", 0, 0), it("ward_charm", 0, 1, tier="silver")],                                        # 엘라 (3열)
        [it("longsword", 0, 0), it("dagger", 0, 1, tier="bronze", fat=True, sel=True), it("herb_pouch", 0, 2)],      # 카이 (2열)
        [it("longsword", 0, 0), it("spear", 0, 1, fat=True), it("buckler", 0, 3, tier="silver", fat=True), it("ember_flask", 1, 3)],  # 로언 (1열)
    ]


def map_a():
    img = shot("ko_35_map_tiers")
    for x0, items in zip(PARTY_X, loadout_a()): board(img, x0, items)
    for x0, n in zip(PARTY_X, (0, 0, 1, 2)): head_fatigue(img, x0, n)
    panel_lines(img,
                [("단검 · ", TEXT), ("동", M.TIER_TEXT["bronze"]), (" · 등급 8 — 무기 장비 / 크기 2×1 / 쿨다운 1.5초 / 앞에서 2번째 자리까지만 발동 / 맨 앞 적에게 피해 6 / ", TEXT), ("전투마다 피로 +1", FATIGUE)],
                [("놓을 칸을 누르면 그 칸을 왼쪽 위로 놓입니다. 겹친 아이템은 인벤토리로 갑니다. 우클릭은 아이템 정보.", TEXT)])
    return img


def map_b():
    img = shot("ko_35_map_tiers")
    boards = [
        [it("fire_staff", 0, 0), it("ember_flask", 0, 1), it("herb_pouch", 1, 2, rot=False)],
        [it("healing_staff", 0, 0), it("ward_charm", 0, 1, tier="silver")],
        [it("longsword", 0, 0), it("dagger", 2, 1, tier="bronze", fat=True, rot=True), it("herb_pouch", 0, 1, sel=True)],
        [it("longsword", 0, 0), it("spear", 0, 1, fat=True, rot=True), it("buckler", 2, 1, tier="silver", fat=True), it("ember_flask", 2, 2, rot=True)],
    ]
    for x0, items in zip(PARTY_X, boards): board(img, x0, items)
    for x0, n in zip(PARTY_X, (0, 0, 1, 2)): head_fatigue(img, x0, n)
    # Examples of touching effects (made up for the picture): the flask's stars light the weapon it touches, the buckler's the
    # weapon beside it, the herb pouch's the item above it.
    edge_star(img, PARTY_X[3], 2, 1, "l", True); edge_star(img, PARTY_X[3], 2, 2, "l", True); edge_star(img, PARTY_X[3], 2, 3, "l", True)
    edge_star(img, PARTY_X[3], 2, 3, "d", False)
    edge_star(img, PARTY_X[2], 0, 1, "u", True); edge_star(img, PARTY_X[2], 1, 1, "u", True); edge_star(img, PARTY_X[2], 0, 1, "d", False)
    edge_star(img, PARTY_X[0], 0, 1, "u", True); edge_star(img, PARTY_X[0], 1, 1, "u", True); edge_star(img, PARTY_X[0], 1, 1, "d", True)
    panel_lines(img,
                [("약초 주머니 · 등급 8 — 지원 아이템 / 크기 2×1 / 쿨다운 5.0초 / 자신의 HP 4 회복 / ", TEXT), ("★ 위에 닿은 무기가 발동하면 HP 1 회복(예시)", VIRTUE)],
                [("들고 있을 때 R 또는 휠: 돌리기. ★은 닿은 칸의 아이템에 효과를 줍니다(밝으면 걸림).", TEXT)])
    return img


def map_c():
    img = shot("ko_35_map_tiers")
    base = {(gx, gy) for gx in range(3) for gy in range(4)}
    extra = [None, None, (0, 4, 2, 1), (0, 4, 3, 1)]
    boards = [
        [it("fire_staff", 0, 0), it("ember_flask", 0, 1), it("herb_pouch", 0, 2)],
        [it("healing_staff", 0, 0), it("ward_charm", 0, 1, tier="silver")],
        [it("longsword", 0, 0), it("dagger", 0, 1, tier="bronze", fat=True), it("herb_pouch", 0, 4, sel=True)],
        [it("longsword", 0, 0), it("spear", 0, 1, fat=True), it("buckler", 0, 3, tier="silver", fat=True), it("ember_flask", 1, 3), it("dagger", 0, 4, fat=True)],
    ]
    for x0, items, ex in zip(PARTY_X, boards, extra):
        clear_column(img, x0)
        bag(img, x0, 0, 0, 3, 4)
        cells = set(base)
        if ex:
            bag(img, x0, ex[0], ex[1], ex[2], ex[3], color=(70, 82, 58)); cells |= {(ex[0] + dx, ex[1] + dy) for dx in range(ex[2]) for dy in range(ex[3])}
        no_room(img, x0, [(gx, gy) for gx in range(3) for gy in range(5) if (gx, gy) not in cells])
        taken = {(i["x"] + dx, i["y"] + dy) for i in items for dx in range(i["w"]) for dy in range(i["h"])}
        for (gx, gy) in sorted(cells - taken): img.alpha_composite(socket(), cell_xy(x0, gx, gy))
        for i in items: img.alpha_composite(tile(i), cell_xy(x0, i["x"], i["y"]))
    for x0, n in zip(PARTY_X, (0, 0, 1, 3)): head_fatigue(img, x0, n)
    panel_lines(img,
                [("약초 주머니 · 등급 8 — 지원 아이템 / 크기 2×1 / 쿨다운 5.0초 / 자신의 HP 4 회복", TEXT)],
                [("가방(가죽) 안의 칸에만 놓습니다. 기본 가방 3×4, 정예·상점의 주머니가 칸을 더합니다(예시).", TEXT)])
    return img


def battle_a():
    img = shot("ko_05_battle")
    boards = [
        [it("fire_staff", 0, 0, charge=0.92), it("ember_flask", 0, 1, charge=0.35), it("herb_pouch", 0, 2, charge=0.6)],
        [it("healing_staff", 0, 0, charge=0.8), it("ward_charm", 0, 1, tier="silver", charge=0.25)],
        [it("longsword", 0, 0, charge=0.45), it("dagger", 0, 1, tier="bronze", charge=0.75), it("herb_pouch", 0, 2, charge=0.1)],
        [it("longsword", 0, 0, charge=0.45), it("spear", 0, 1, charge=0.55), it("buckler", 0, 3, tier="silver", charge=0.3), it("ember_flask", 1, 3, charge=0.68)],
    ]
    for x0, items in zip(PARTY_X, boards): board(img, x0, items, battle=True)
    for x0, frac in zip(ENEMY_X, (0.62, 0.18)):
        ImageDraw.Draw(img).rectangle([x0 - 1, 645, x0 + 181, 710], fill=(36, 32, 30, 255))
        board(img, x0, [it("rat_bite", 0, 0, flip=True, charge=frac)], rows=1, battle=True)
    return img


def flow_a():
    """Picking up and putting down, Diablo's way: the item rides the cursor, its squares show where it lands."""
    frames = []
    crop = (100, 612, 900, 975)
    sel = loadout_a()

    def base(selected_kai=True):
        img = shot("ko_35_map_tiers"); bs = loadout_a()
        if not selected_kai: bs[2][1]["sel"] = False
        for x0, items in zip(PARTY_X, bs): board(img, x0, items)
        for x0, n in zip(PARTY_X, (0, 0, 1, 2)): head_fatigue(img, x0, n)
        return img

    img = base(); cursor(img, *(lambda p: (p[0] + 70, p[1] + 30))(cell_xy(PARTY_X[2], 0, 1)))
    frames.append(("① 누르면 집는다: 단검이 놋쇠 칸, 우클릭은 정보 카드", img.crop(crop)))

    def held(img, gx, gy, color, label):
        ghost(img, PARTY_X[3], gx, gy, 2, 1, color, icon_id="dagger", label=label)
        cx, cy = cell_xy(PARTY_X[3], gx, gy); cursor(img, cx + 44, cy + 26)

    img = base(); held(img, 1, 4, GOOD, "놓기")
    frames.append(("② 들고 움직이면 놓일 칸이 초록: 로언의 빈 줄", img.crop(crop)))

    img = base(); held(img, 0, 3, DANGER, "버클러·플라스크와 겹침")
    frames.append(("③ 둘 이상과 겹치면 빨강: 놓이지 않는다", img.crop(crop)))

    img = base(); held(img, 1, 2, VIRTUE, "창 → 인벤토리")
    frames.append(("④ 하나와 겹치면 금: 겹친 것은 인벤토리로(지금 규칙)", img.crop(crop)))
    return frames


def shapes_sheet(path):
    order = ["buckler", "rat_bite", "dagger", "herb_pouch", "ember_flask", "overseer_roar",
             "longsword", "greataxe", "mace", "healing_staff", "fire_staff", "ward_charm", "mending_chant", "rusty_blade", "crude_bow",
             "lantern_staff", "overseer_maul", "longbow", "spear", "halberd"]
    cols = 5; cw, ch = 220, 300
    out = Image.new("RGBA", (cols * cw + 40, ((len(order) + cols - 1) // cols) * ch + 140), (18, 18, 18, 255)); d = ImageDraw.Draw(out)
    d.text((24, 18), "모양 표: 지금의 크기 N칸(띠) → 격자의 너비×높이. 모양은 지금 아이콘의 비율을 따른다(다시 그리지 않음).", font=font(24), fill=TEXT)
    d.text((24, 54), "흐린 이름 = 적만 드는 것(전리품으로 떨어진다). 3×5 격자 하나 = 지금 띠 다섯.", font=font(20), fill=DIM)
    for k, id in enumerate(order):
        w, h = SHAPES[id]; x = 20 + (k % cols) * cw; y = 100 + (k // cols) * ch
        grid = Image.new("RGBA", (px(3) + 8, px(3) + 8), (36, 32, 30, 255))
        for gx in range(3):
            for gy in range(3):
                if gx < w and gy < h: continue
                grid.alpha_composite(socket(), (4 + gx * (S + G), 4 + gy * (S + G)))
        grid.alpha_composite(tile(it(id, 0, 0)), (4, 4))
        out.alpha_composite(grid, (x, y + 40))
        d.text((x, y), NAMES[id], font=font(22), fill=DIM if id in ENEMY_ONLY else TEXT)
        d.text((x, y + 40 + grid.height + 8), f"{SIZE_NOW[id]}칸 → {w}×{h}", font=font(20), fill=VIRTUE)
    out.convert("RGB").save(path); print(path.name, out.size)


def main():
    now = map_now(); a, b, c = map_a(), map_b(), map_c(); battle_now = shot("ko_05_battle"); bat = battle_a()
    for name, im in (("mock-A-map", a), ("mock-B-map", b), ("mock-C-map", c), ("mock-A-battle", bat)):
        im.convert("RGB").save(HERE / f"{name}.png"); print(name)
    crop = (100, 612, 900, 975)
    M.sheet([("지금: 띠 다섯 칸(180×60). A와 같은 장비", now.crop(crop)), ("A 디아블로 격자 (권장): 3×5, 직사각형, 돌리기·인접 없음", a.crop(crop)),
             ("B 백팩 배틀즈 격자: A + 돌리기 + ★ 맞닿음 효과(예시)", b.crop(crop)), ("C 가방 격자: 가방이 칸을 만든다(예시: 기본 3×4 + 주머니)", c.crop(crop))],
            2, HERE / "mock-compare.png", size=24,
            footer=["같은 자리·같은 넓이(열마다 180×302). 칸은 58 정사각형, 사이 3. 빈 칸은 어둡게, 아이템은 양피지 판으로(모양이 읽히게).",
                    "노드 맵의 보드(ko_35 위에 다시 그림). 머리 띠의 피로 합계는 각 안의 장비대로 고쳐 적었다."])
    M.sheet([("지금", battle_now.crop((100, 612, 1440, 975))), ("A: 전투 화면의 격자(쿨다운 빛은 아이템마다 왼쪽→오른쪽)", bat.crop((100, 612, 1440, 975)))],
            1, HERE / "mock-A-battle-compare.png", size=24,
            footer=["적의 보드도 같은 격자(폭 3, 줄은 든 것만큼). 적의 아이콘은 지금처럼 좌우를 뒤집는다."])
    M.sheet(flow_a(), 2, HERE / "mock-A-flow.png", size=24,
            footer=["오른쪽 판의 설명 줄: \"놓을 칸을 누르면 그 칸을 왼쪽 위로 놓입니다. 겹친 아이템은 인벤토리로 갑니다.\"",
                    "같은 단검·동 위에 놓으면 지금처럼 금색 \"합치기 → 은\". 들고 있는 동안 Esc나 같은 칸 = 내려놓기 취소."])
    shapes_sheet(HERE / "mock-shapes.png")


if __name__ == "__main__":
    main()
