# -*- coding: utf-8 -*-
"""Round 48, second ask (2026-10-08): the B way with C added (the user: "B 방향으로 추가 목업" + "C 가방격자도 추가"): the grid of
squares, turning items, stars between touching items, and bags that make the squares. And the new cooldown the user asked for:
the item itself (not its cell) is shaded hard, brightens with its cooldown, and when it is full the item swells a little and
comes back as its effect fires. Stills over the screenshots of 20261008-r47b and MP4s (x1, then x0.5). No API call.
  .venv/bin/python ArtPipeline/Archive/48-grid-board/mock_bags.py [stills|videos]
"""
import csv
import math
import shutil
import subprocess
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter, ImageChops

HERE = Path(__file__).resolve().parent; ROOT = HERE.parents[2]
sys.path.insert(0, str(HERE))
import mock_grid as G  # noqa: E402  (round 48 first ask: shapes, tiles, sockets, ghost, cursor, star, panel lines)

M = G.M; font = G.font; S, GAP = G.S, G.G
TEXT, DIM, INK, BRASS, VIRTUE, FATIGUE, GOOD, DANGER = G.TEXT, G.DIM, G.INK, G.BRASS, G.VIRTUE, G.FATIGUE, G.GOOD, G.DANGER
CHARGE = G.CHARGE
PARTY_X = G.PARTY_X; ROWS = 6                      # the frame: 3 x 6 (one row more than today's five, under the same column)
FRAME = (644, 1017)
FPS = 30

CATEGORY = {r["Id"]: r["Category"] for r in csv.DictReader(open(ROOT / "Assets/@Data/Source/ItemData.csv", encoding="utf-8"))}
COOLDOWN = {r["Id"]: int(r["CooldownMs"]) for r in csv.DictReader(open(ROOT / "Assets/@Data/Source/ItemData.csv", encoding="utf-8"))}

# Stars (examples made up for the picture, all of one kind: "when a touching item of this category fires, this item does a small
# thing"). The numbers are not proposals; a simulation would set them.
STAR = {
    "ember_flask": ("Weapon", "닿은 무기 장비가 피해를 주면 그 적에게 화상 +1"),
    "buckler": ("Weapon", "닿은 무기 장비가 발동하면 자신에게 보호막 +3"),
    "herb_pouch": ("Armor", "닿은 방어 장비가 발동하면 자신의 HP 2 회복"),
    "ward_charm": ("Support", "닿은 지원 아이템이 발동하면 가장 다친 아군에게 보호막 +2"),
}
BAGS = {"pack": ("가죽 배낭", (3, 4), (96, 64, 38)), "pouch": ("가죽 주머니", (3, 1), (70, 82, 58)), "belt": ("허리 주머니", (2, 1), (110, 54, 42))}


def bagd(kind, x, y, rot=False):
    w, h = BAGS[kind][1]
    if rot: w, h = h, w
    return dict(kind=kind, x=x, y=y, w=w, h=h)


def cells_of(o): return {(o["x"] + dx, o["y"] + dy) for dx in range(o["w"]) for dy in range(o["h"])}
def cell_xy(x0, gx, gy): return G.cell_xy(x0, gx, gy)


# ---- the frame, bags, sockets ---------------------------------------------------------------------------------------------------

def frame(img, x0):
    d = ImageDraw.Draw(img)
    d.rectangle([x0 - 4, FRAME[0], x0 + 184, FRAME[1]], fill=(30, 26, 24, 255), outline=(18, 8, 6, 255), width=2)
    d.rectangle([x0 - 3, FRAME[0] + 2, x0 + 183, FRAME[1] - 2], outline=(112, 88, 46, 255), width=1)


def dashed_cell(img, x0, gx, gy, color=(74, 68, 62)):
    d = ImageDraw.Draw(img); cx, cy = cell_xy(x0, gx, gy)
    for k in range(0, S, 7):
        e = min(k + 3, S - 1)
        d.line([(cx + k, cy), (cx + e, cy)], fill=color + (255,)); d.line([(cx + k, cy + S - 1), (cx + e, cy + S - 1)], fill=color + (255,))
        d.line([(cx, cy + k), (cx, cy + e)], fill=color + (255,)); d.line([(cx + S - 1, cy + k), (cx + S - 1, cy + e)], fill=color + (255,))


_socket_cache = {}


def bag_socket(color):
    if color not in _socket_cache:
        tint = tuple(min(255, int(c * 1.05)) for c in color)
        _socket_cache[color] = G.tinted(M.nine_slice(M.sprite("slot"), 8, (S, S)), tint)
    return _socket_cache[color]


def bag_under(img, x0, b, alpha=255):
    """The bag's leather under its squares, stitched round."""
    X, Y = cell_xy(x0, b["x"], b["y"]); W, H = G.px(b["w"]), G.px(b["h"]); color = BAGS[b["kind"]][2]
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0)); d = ImageDraw.Draw(layer)
    d.rounded_rectangle([X - 4, Y - 4, X + W + 3, Y + H + 3], radius=8, fill=color + (alpha,), outline=(34, 22, 14, alpha), width=2)
    stitch = tuple(min(255, c + 80) for c in color) + (alpha,)
    for k in range(int(X), int(X + W), 8):
        d.line([(k, Y - 1), (k + 4, Y - 1)], fill=stitch); d.line([(k, Y + H), (k + 4, Y + H)], fill=stitch)
    for k in range(int(Y), int(Y + H), 8):
        d.line([(X - 1, k), (X - 1, k + 4)], fill=stitch); d.line([(X + W, k), (X + W, k + 4)], fill=stitch)
    img.alpha_composite(layer)


def bag_sockets(img, x0, b, taken):
    color = BAGS[b["kind"]][2]
    for (gx, gy) in sorted(cells_of(b) - taken): img.alpha_composite(bag_socket(color), cell_xy(x0, gx, gy))


def star_badge(t):
    """A small star at the item's top-left: this item has a touching effect."""
    d = ImageDraw.Draw(t); d.rounded_rectangle([3, 3, 21, 21], radius=5, fill=INK + (225,), outline=BRASS + (255,), width=1)
    pts = []
    for k in range(10):
        r = 7 if k % 2 == 0 else 3
        a = k * math.pi / 5 - math.pi / 2; pts.append((12 + r * math.cos(a), 12 + r * math.sin(a)))
    d.polygon(pts, fill=VIRTUE + (255,))


def contacts(items):
    """(star item, neighbour, x/y of the middle of their shared edge in grid units, lit) for each star item and each item it touches."""
    out = []
    for a in items:
        if a["id"] not in STAR: continue
        for b in items:
            if b is a: continue
            mids = []
            for (x, y) in cells_of(a):
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    if (x + dx, y + dy) in cells_of(b): mids.append((x + 0.5 + dx / 2, y + 0.5 + dy / 2))
            if mids:
                mx = sum(m[0] for m in mids) / len(mids); my = sum(m[1] for m in mids) / len(mids)
                out.append((a, b, mx, my, CATEGORY[b["id"]] == STAR[a["id"]][0]))
    return out


def star_at(img, x0, mx, my, lit, glow=1.0, r=9):
    """A star on a shared edge (grid units from the board's corner), drawn on a small patch; a flash grows it and its glow."""
    cx = x0 + mx * (S + GAP) - GAP / 2; cy = G.TOP + my * (S + GAP) - GAP / 2
    P = 80; patch = Image.new("RGBA", (P, P), (0, 0, 0, 0)); c = P / 2
    if lit:
        g = Image.new("RGBA", (P, P), (0, 0, 0, 0)); R = 15 * glow
        ImageDraw.Draw(g).ellipse([c - R, c - R, c + R, c + R], fill=CHARGE + (int(min(255, 140 * glow)),))
        patch.alpha_composite(g.filter(ImageFilter.GaussianBlur(6)))
    rr = r * (1 + 0.25 * (glow - 1)); pts = []
    for k in range(8):
        q = rr if k % 2 == 0 else rr * 0.38; a = k * math.pi / 4 - math.pi / 2; pts.append((c + q * math.cos(a), c + q * math.sin(a)))
    ImageDraw.Draw(patch).polygon(pts, fill=(VIRTUE if lit else (96, 92, 86)) + (255,), outline=INK + (255,))
    img.alpha_composite(patch, (int(round(cx - c)), int(round(cy - c))))


def board(img, x0, bags, items, battle=False):
    frame(img, x0)
    bag_cells = set().union(*[cells_of(b) for b in bags]) if bags else set()
    for gx in range(3):
        for gy in range(ROWS):
            if (gx, gy) not in bag_cells: dashed_cell(img, x0, gx, gy)
    taken = set().union(*[cells_of(i) for i in items]) if items else set()
    for b in bags: bag_under(img, x0, b)
    for b in bags: bag_sockets(img, x0, b, taken)
    if battle: return
    for i in items:
        t = G.tile(i)
        if i["id"] in STAR: star_badge(t)
        img.alpha_composite(t, cell_xy(x0, i["x"], i["y"]))
    for a, b, mx, my, lit in contacts(items): star_at(img, x0, mx, my, lit)


# ---- the B + C node map -----------------------------------------------------------------------------------------------------------

def it(*a, **k): return G.it(*a, **k)


def scene_map():
    return [
        ([bagd("pack", 0, 0)], [it("fire_staff", 0, 0), it("ember_flask", 0, 1), it("herb_pouch", 0, 2)]),
        ([bagd("pack", 0, 0), bagd("belt", 0, 4)], [it("healing_staff", 0, 0), it("ward_charm", 0, 1, tier="silver"), it("herb_pouch", 0, 4)]),
        ([bagd("pack", 0, 0), bagd("pouch", 0, 4)], [it("longsword", 0, 0), it("ember_flask", 0, 1, sel=True), it("dagger", 2, 1, tier="bronze", fat=True, rot=True)]),
        ([bagd("pack", 0, 0), bagd("pouch", 0, 4), bagd("belt", 0, 5)],
         [it("longsword", 0, 0), it("spear", 0, 1, fat=True, rot=True), it("buckler", 2, 1, tier="silver", fat=True), it("ember_flask", 2, 2, rot=True), it("herb_pouch", 0, 4)]),
    ]


SKULLS = (850, 1004, 916, 1066)


def base_map(scene=None):
    img = G.shot("ko_35_map_tiers"); skulls = img.crop(SKULLS)
    for x0, (bags, items) in zip(PARTY_X, scene or scene_map()): board(img, x0, bags, items)
    for x0, n in zip(PARTY_X, (0, 0, 1, 2)): G.head_fatigue(img, x0, n)
    sk = skulls.copy(); a = sk.split()[3]
    # the skull pile stands over the corner of the 1st column's frame, as it does today over the board table
    mask = Image.eval(sk.convert("L"), lambda v: 255 if v > 70 else 0); img.paste(sk, SKULLS[:2], mask)
    return img


def map_bc():
    img = base_map()
    G.panel_lines(img,
                  [("불씨 플라스크 · 등급 8 — 공격 아이템 / 크기 2×1 / 쿨다운 4.5초 / 맨 앞 적에게 화상 2 / ", TEXT), ("★ 닿은 무기 장비가 피해를 주면 화상 +1(예시)", VIRTUE)],
                  [("가방 안에만 놓습니다. 들고 있을 때 R 또는 휠: 돌리기. ★ 밝음 = 닿은 아이템에 걸림. 우클릭은 아이템 정보.", TEXT)])
    return img


def card_with_stars(title, facts, effects, star_lines, tier="common"):
    W = 420; PAD = 16; inner = W - 2 * PAD - 8
    f_title, f_fact, f_eff = font(24), font(19), font(20)
    lines = [([(title, TEXT)], f_title, 6), ("rule", None, 10)]
    for ln in M.wrap_segments([(facts, DIM)], f_fact, inner): lines.append((ln, f_fact, 4))
    lines.append(("space", None, 4))
    for e in effects:
        for ln in M.wrap_segments([(e, TEXT)], f_eff, inner): lines.append((ln, f_eff, 4))
    lines.append(("space", None, 6))
    for segs in star_lines:
        for ln in M.wrap_segments(segs, f_fact, inner): lines.append((ln, f_fact, 4))
    H = 2 * PAD
    for ln, f, gap in lines: H += (1 if ln == "rule" else 0 if ln == "space" else f.size) + gap
    nh = 12
    img = Image.new("RGBA", (W, H + nh), (0, 0, 0, 0)); d = ImageDraw.Draw(img)
    d.rounded_rectangle((0, 0, W - 1, H - 1), radius=4, fill=INK + (240,), outline=BRASS + (235,), width=1)
    return img, lines, PAD, inner, H, nh


def place_card_above(img, x0, gx, gy, w, title, facts, effects, star_lines):
    card, lines, PAD, inner, H, nh = card_with_stars(title, facts, effects, star_lines)
    d = ImageDraw.Draw(card); x, y = PAD + 8, PAD
    for ln, f, gap in lines:
        if ln == "rule": d.line([(x, y), (x + inner, y)], fill=BRASS + (110,), width=1); y += 1 + gap
        elif ln == "space": y += gap
        else: M.draw_segments(d, x, y, ln, f); y += f.size + gap
    cx, cy = cell_xy(x0, gx, gy); target = cx + G.px(w) / 2
    left = int(min(max(12, target - card.width / 2), 1908 - card.width)); top = 618 - 8 - card.height
    nx = target - left
    d.polygon([(nx - 10, H - 1), (nx + 10, H - 1), (nx, H - 1 + nh)], fill=INK + (240,))
    d.line([(nx - 10, H - 1), (nx, H - 1 + nh), (nx + 10, H - 1)], fill=BRASS + (235,), width=1)
    M.place_card(img, card, left, top)


def card_still():
    img = base_map()
    place_card_above(img, PARTY_X[2], 0, 1, 2, "불씨 플라스크 · 등급 8", "공격 아이템 / 크기 2×1 / 쿨다운 4.5초 / 어디서나 발동",
                     ["맨 앞 적에게 화상 2"],
                     [[("★ 닿은 무기 장비가 피해를 주면 그 적에게 화상 +1(예시)", VIRTUE)], [("지금 닿음: 롱소드, 단검 (★ 둘 다 밝음)", DIM)]])
    G.panel_lines(img, [("불씨 플라스크 · 등급 8 — 공격 아이템 / 크기 2×1 / 쿨다운 4.5초 / 맨 앞 적에게 화상 2 / ", TEXT), ("★ 닿은 무기 장비가 피해를 주면 화상 +1(예시)", VIRTUE)],
                  [("우클릭: 아이템 정보. 카드는 보드 위, 아래 꼭지가 그 아이템을 가리킵니다.", TEXT)])
    return img


# ---- the flows ----------------------------------------------------------------------------------------------------------------------

CROP = (95, 612, 905, 1022)


def held_bag(img, x0, b, color, label, with_items=()):
    """A bag in hand over a frame: its squares tinted (green, red), the items it carries riding along."""
    X, Y = cell_xy(x0, b["x"], b["y"]); W, H = G.px(b["w"]), G.px(b["h"])
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0)); d = ImageDraw.Draw(layer)
    d.rounded_rectangle([X - 4, Y - 4, X + W + 3, Y + H + 3], radius=8, fill=BAGS[b["kind"]][2] + (170,), outline=color + (255,), width=3)
    for (gx, gy) in cells_of(b):
        cx, cy = cell_xy(x0, gx, gy); d.rounded_rectangle([cx, cy, cx + S - 1, cy + S - 1], radius=4, fill=color + (70,))
    img.alpha_composite(layer)
    for i in with_items:
        t = G.tile(i); a = t.split()[3].point(lambda v: int(v * 0.8)); t.putalpha(a); img.alpha_composite(t, cell_xy(x0, i["x"], i["y"]))
    if label:
        d = ImageDraw.Draw(img); f = font(16); tw = d.textlength(label, font=f); lx = min(max(X + W / 2 - tw / 2, x0 - 6), x0 + 186 - tw)
        d.rounded_rectangle([lx - 6, Y - 28, lx + tw + 6, Y - 6], radius=4, fill=INK + (235,), outline=color + (255,), width=1)
        d.text((lx, Y - 27), label, font=f, fill=color)
    cursor_xy = (X + W / 2 + 10, Y + H / 2); G.cursor(img, int(cursor_xy[0]), int(cursor_xy[1]))


def flow_bags():
    frames = []
    img = base_map(); held_bag(img, PARTY_X[0], bagd("pouch", 0, 4), GOOD, "가방 놓기: 칸 +3")
    frames.append(("① 상점·전리품의 가방(가죽 주머니 3×1)을 미라의 빈 자리에", img.crop(CROP)))

    sc = scene_map(); bags, items = sc[1]; sc[1] = ([bags[0]], [items[0], items[1]])
    img = base_map(sc); held_bag(img, PARTY_X[0], bagd("belt", 0, 4), GOOD, "약초 주머니와 함께", with_items=[it("herb_pouch", 0, 4)])
    frames.append(("② 엘라의 허리 주머니를 옮기면 안의 아이템도 함께 간다", img.crop(CROP)))

    img = base_map(); G.ghost(img, PARTY_X[0], 0, 4, 2, 1, DANGER, icon_id="dagger", label="가방 밖"); cx, cy = cell_xy(PARTY_X[0], 0, 4); G.cursor(img, cx + 44, cy + 26)
    frames.append(("③ 가방 밖(점선)에는 놓이지 않는다", img.crop(CROP)))

    img = base_map(); G.ghost(img, PARTY_X[2], 2, 3, 1, 2, GOOD, icon_id="dagger", rot=True, label="두 가방에 걸침")
    cx, cy = cell_xy(PARTY_X[2], 2, 3); G.cursor(img, cx + 30, cy + 50)
    frames.append(("④ 아이템은 맞닿은 두 가방에 걸쳐 놓일 수 있다", img.crop(CROP)))
    return frames


def flow_turn():
    frames = []
    sc = scene_map(); bags, items = sc[3]; base_items = [i for i in items if i["id"] != "spear"]; sc[3] = (bags, base_items)
    img = base_map(sc); G.ghost(img, PARTY_X[3], 0, 1, 3, 2, DANGER, icon_id="spear", label="버클러·플라스크와 겹침")
    cx, cy = cell_xy(PARTY_X[3], 0, 1); G.cursor(img, cx + 90, cy + 60)
    frames.append(("① 창(3×2)을 들고 로언의 빈 자리: 둘과 겹쳐 빨강", img.crop(CROP)))

    img = base_map(sc); G.ghost(img, PARTY_X[3], 0, 1, 2, 3, GOOD, icon_id="spear", rot=True, label="R: 돌림 → 2×3")
    cx, cy = cell_xy(PARTY_X[3], 0, 1); G.cursor(img, cx + 60, cy + 90)
    frames.append(("② R(또는 휠)로 돌리면 2×3: 들어가 초록", img.crop(CROP)))

    img = base_map()
    frames.append(("③ 놓으면 닿은 버클러·플라스크의 ★이 밝아진다(무기 장비에 걸림)", img.crop(CROP)))

    img = base_map(); held = it("ember_flask", 0, 2)
    G.ghost(img, PARTY_X[2], 0, 2, 2, 1, GOOD, icon_id="ember_flask", label="★ 하나 걸림")
    star_at(img, PARTY_X[2], 1.0, 2.0, False); star_at(img, PARTY_X[2], 2.0, 2.5, True)
    cx, cy = cell_xy(PARTY_X[2], 0, 2); G.cursor(img, cx + 70, cy + 30)
    frames.append(("④ 들고 움직이면 닿을 ★을 미리 보인다: 단검(무기) 밝음, 플라스크 흐림", img.crop(CROP)))
    return frames


# ---- the cooldown: today's light and the item's own shade ---------------------------------------------------------------------

POP_UP, POP_T = 0.07, 0.30          # the swell's rise and its whole time (s)
POP_AMP = 0.12                      # "살짝 커졌다가": 12% (today's firing pops the icon 30%)
DARK = 0.20                         # the shade at the start of a cooldown: 20% of the item's light


def pop_scale(s, amp=POP_AMP):
    if s is None or s >= POP_T: return 1.0
    if s < POP_UP: u = s / POP_UP; return 1 + amp * (1 - (1 - u) ** 2)
    u = (s - POP_UP) / (POP_T - POP_UP); return 1 + amp * (1 - u) ** 2


class Parts:
    """An item cut into what the cooldown moves: the tile (fixed), the sprite (icon + tier outline: shaded, swelled) and the tags."""

    def __init__(self, i, enemy=False):
        self.i = i; W, H = G.px(i["w"]), G.px(i["h"]); self.size = (W, H)
        self.bg = M.nine_slice(M.sprite("slot"), 8, (W, H))
        ic = G.fit(G.icon(i["id"], i["rot"], enemy), (W - 12, H - 12))
        self.sprite = Image.new("RGBA", (W, H), (0, 0, 0, 0)); xy = ((W - ic.width) // 2, (H - ic.height) // 2)
        color = M.TIER_MARK[i["tier"]]
        if color:
            r = {"bronze": 2, "silver": 3, "gold": 4}[i["tier"]]
            sil = Image.new("RGBA", ic.size, color + (255,)); sil.putalpha(ic.split()[3].filter(ImageFilter.MaxFilter(2 * r + 1)))
            self.sprite.alpha_composite(sil, xy)
        self.sprite.alpha_composite(ic, xy)
        self.tags = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        if color:
            tmp = Image.new("RGBA", (W, H), (0, 0, 0, 0)); M.tier_marks(tmp, Image.new("RGBA", (1, 1), (0, 0, 0, 0)), (0, 0), i["tier"])
            self.tags.alpha_composite(tmp)
        if i["id"] in STAR and not enemy: star_badge(self.tags)
        a = self.sprite.split()[3]
        self.glow_mask = a.filter(ImageFilter.MaxFilter(9)).filter(ImageFilter.GaussianBlur(6))
        self.gray = Image.merge("RGBA", (*[self.sprite.convert("L")] * 3, a))


def shaded(sprite, b):
    if b >= 0.999: return sprite
    v = int(255 * b); rgb = ImageChops.multiply(sprite.convert("RGB"), Image.new("RGB", sprite.size, (v, v, v)))
    out = rgb.convert("RGBA"); out.putalpha(sprite.split()[3]); return out


def swelled(sprite, k):
    if abs(k - 1) < 1e-3: return sprite, (0, 0)
    W, H = sprite.size; w, h = int(round(W * k)), int(round(H * k))
    return sprite.resize((w, h), Image.BILINEAR), (-(w - W) // 2, -(h - H) // 2)


def glow_layer(p, alpha):
    g = Image.new("RGBA", p.size, CHARGE + (0,)); g.putalpha(p.glow_mask.point(lambda v: int(v * alpha))); return g


def draw_item(img, xy, p, variant, c, s, enabled=True):
    """One item at charge c (0..1) and s seconds after its last firing (None: not fired yet)."""
    x, y = xy; W, H = p.size
    tile = p.bg.copy()
    if not enabled:
        # Not usable from this row: the whole item goes grey and dim (its tile too), and it neither brightens nor swells.
        tile = G.tinted(tile, (150, 150, 150)); sp = shaded(p.gray, 0.38); tile.alpha_composite(sp); tile.alpha_composite(p.tags)
        img.alpha_composite(tile, (x, y)); return

    if variant == "now":
        # Today: the cell's light from the left, the dark over the rest of the cell (icon and all); firing pops the icon 30%
        # and flashes the cell while the dark comes back.
        firing = s is not None and s < 0.3
        k = 1 + 0.3 * math.sin(math.pi * s / 0.3) if firing else 1.0
        spk, off = swelled(p.sprite, k)
        canvas = Image.new("RGBA", (W, H), (0, 0, 0, 0)); canvas.paste(spk, off, spk); tile.alpha_composite(canvas)
        over = Image.new("RGBA", (W, H), (0, 0, 0, 0)); d = ImageDraw.Draw(over); cut = int(round(W * c))
        dark = int(255 * 0.85 * (min(1, s / 0.3) if firing else 1))
        d.rectangle([0, 0, cut, H], fill=CHARGE + (40,)); d.rectangle([cut, 0, W, H], fill=(10, 8, 6, dark))
        if not firing and 0 < cut < W:
            for q in range(18): d.line([(cut - q, 0), (cut - q, H)], fill=CHARGE + (int(90 * (1 - q / 18)),))
            d.line([(cut, 0), (cut, H)], fill=CHARGE + (235,), width=2)
        if firing: d.rectangle([0, 0, W, H], fill=(255, 235, 168, int(255 * 0.6 * (1 - s / 0.3))))
        a = tile.split()[3]; tile.alpha_composite(over); tile.putalpha(a)
        tile.alpha_composite(p.tags); img.alpha_composite(tile, (x, y)); return

    base = DARK + (1 - DARK) * c
    firing = s is not None and s < POP_T
    if firing:
        fade = 0 if s < POP_UP else (s - POP_UP) / (POP_T - POP_UP)
        b = max(base, 1 - fade * (1 - DARK))
    else:
        b = base
    if variant == "v3" and not firing:
        # light rising from the bottom of the item as it charges
        line = H * (1 - c); mask = Image.new("L", (W, H), 0); md = ImageDraw.Draw(mask)
        md.rectangle([0, int(line), W, H], fill=255); mask = mask.filter(ImageFilter.GaussianBlur(3))
        dark_sp = shaded(p.sprite, DARK); lit = p.sprite.copy(); sp = Image.composite(lit, dark_sp, mask)
        sp.putalpha(p.sprite.split()[3])
    else:
        sp = shaded(p.sprite, b)
    k = pop_scale(s) if firing else 1.0
    if variant == "v2" and not firing and c > 0.8:
        tile.alpha_composite(glow_layer(p, 0.7 * (c - 0.8) / 0.2))
    img.alpha_composite(tile, (x, y))
    if firing:
        gg, goff = swelled(glow_layer(p, 0.75 * (1 - s / POP_T)), k * 1.08); img.alpha_composite(gg, (x + goff[0], y + goff[1]))
    spk, off = swelled(sp, k)
    img.alpha_composite(spk, (x + off[0], y + off[1]))
    img.alpha_composite(p.tags, (x, y))


class Item:
    def __init__(self, x0, i, phase, enabled=True, enemy=False):
        self.x0, self.i, self.enabled = x0, i, enabled
        self.period = COOLDOWN[i["id"]] / 1000.0; self.phase = phase * self.period
        self.p = Parts(i, enemy)

    def state(self, t):
        e = t + self.phase; c = (e % self.period) / self.period
        fired = e >= self.period
        return c, (e % self.period) if fired else None


def battle_scene():
    """The B + C boards in battle: the 4 columns with bags and turned items, and the two rats' boards (no bags: enemies carry none)."""
    cols = scene_map()
    party = []
    phases = {"fire_staff": 0.62, "ember_flask": 0.35, "herb_pouch": 0.80, "healing_staff": 0.15, "ward_charm": 0.5, "longsword": 0.45,
              "dagger": 0.7, "spear": 0.9, "buckler": 0.25}
    # 미라 (4th row) also carries a dagger, which fires only from the front two rows: it shows the item that cannot be used.
    cols[0] = (cols[0][0], cols[0][1] + [G.it("dagger", 0, 3)])
    for col, (x0, (bags, items)) in enumerate(zip(PARTY_X, cols)):
        for i in items:
            i = dict(i, sel=False); ph = (phases[i["id"]] + 0.17 * col) % 1.0
            party.append(Item(x0, i, ph, enabled=not (col == 0 and i["id"] == "dagger")))
    enemies = [Item(x0, G.it("rat_bite", 0, 0), ph, enemy=True) for x0, ph in zip(G.ENEMY_X, (0.55, 0.1))]
    return cols, party, enemies


def battle_background(cols):
    img = G.shot("ko_05_battle")
    for x0, (bags, items) in zip(PARTY_X, cols): board(img, x0, bags, items, battle=True)
    for x0 in G.ENEMY_X:
        ImageDraw.Draw(img).rectangle([x0 - 1, 645, x0 + 181, 712], fill=(30, 26, 24, 255))
        for gx in range(1, 3): img.alpha_composite(G.socket(True), G.cell_xy(x0, gx, 0))
    return img


def battle_frame(bg, cols, party, enemies, t, variant="v1"):
    img = bg.copy()
    for unit in party + enemies:
        c, s = unit.state(t) if unit.enabled else (0, None)
        draw_item(img, G.cell_xy(unit.x0, unit.i["x"], unit.i["y"]), unit.p, variant, c, s, unit.enabled)
    # Stars: lit ones flash when the touching item fires.
    by_col = {}
    for u in party: by_col.setdefault(u.x0, []).append(u)
    for x0, units in by_col.items():
        items = [u.i for u in units]
        for a, b, mx, my, lit in contacts(items):
            nb = next(u for u in units if u.i is b); c, s = nb.state(t) if nb.enabled else (0, None)
            glow = 1 + 1.4 * (1 - s / 0.4) if (lit and s is not None and s < 0.4) else 1.0
            star_at(img, x0, mx, my, lit, glow)
    return img


def write_video(frames_fn, n, path):
    tmp = HERE / f".frames-{path.stem}"
    shutil.rmtree(tmp, ignore_errors=True); tmp.mkdir()
    for k in range(n): frames_fn(k).convert("RGB").save(tmp / f"{k:05d}.png")
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-framerate", str(FPS), "-i", str(tmp / "%05d.png"),
                    "-vf", "pad=ceil(iw/2)*2:ceil(ih/2)*2", "-c:v", "libx264", "-pix_fmt", "yuv420p", "-crf", "18", str(path)], check=True)
    shutil.rmtree(tmp); print("wrote", path.name, n, "frames")


def timeline(k):
    """x1 for 8 s, then the first 4 s again at x0.5 (8 s)."""
    t = k / FPS
    return (t, "x1") if t < 8 else ((t - 8) / 2, "x0.5 느린 재생")


VARIANTS = [("now", "지금: 칸의 빛, 발동 때 아이콘 1.3배와 칸의 번쩍임"), ("v1", "안 1 (요청): 아이템만 음영 → 밝아짐 → 1.12배로 커졌다 돌아옴"),
            ("v2", "안 2: 안 1 + 다 차기 직전 아이템 둘레의 빛"), ("v3", "안 3: 아래에서 위로 밝아짐 + 안 1의 커짐")]
CMP = (490, 612, 882, 1022)          # 카이 and 로언


def compare_video():
    cols, party, enemies = battle_scene(); bg = battle_background(cols)
    w, h = CMP[2] - CMP[0], CMP[3] - CMP[1]; lab = 64; gap = 16
    W = len(VARIANTS) * (w + gap) + gap; H = h + lab + 52

    def frame(k):
        t, speed = timeline(k)
        out = Image.new("RGBA", (W, H), (18, 18, 18, 255)); d = ImageDraw.Draw(out)
        for n, (v, label) in enumerate(VARIANTS):
            im = battle_frame(bg, cols, party, enemies, t, v).crop(CMP); x = gap + n * (w + gap)
            for j, ln in enumerate(M.wrap_segments([(label, TEXT)], font(18), w)): M.draw_segments(d, x, 8 + j * 24, ln, font(18))
            out.alpha_composite(im, (x, lab))
        d.text((gap, H - 40), f"{speed}   t = {t:4.2f}초   ·   마검사 카이(2열)와 기사 로언(1열). 쿨다운은 데이터의 값(롱소드 3.0초, 창 3.2초, 단검 1.5초, 버클러 5.0초, 불씨 플라스크 4.5초, 약초 주머니 5.0초).",
               font=font(17), fill=DIM)
        return out
    write_video(frame, 16 * FPS, HERE / "mock-cooldown.mp4")


BATTLE_CROP = (95, 612, 1440, 1022)


def battle_video():
    cols, party, enemies = battle_scene(); bg = battle_background(cols)

    def frame(k):
        t, speed = timeline(k)
        im = battle_frame(bg, cols, party, enemies, t, "v1").crop(BATTLE_CROP)
        d = ImageDraw.Draw(im); d.rounded_rectangle([1000, 360, 1335, 400], radius=6, fill=INK + (220,))
        d.text((1012, 368), f"안 1 · {speed} · {t:4.2f}초", font=font(18), fill=TEXT)
        return im
    write_video(frame, 16 * FPS, HERE / "mock-BC-battle.mp4")


def cooldown_strip(path):
    i = G.it("longsword", 0, 0); p = Parts(i); p2 = Parts(G.it("ember_flask", 0, 0))
    moments = [("0%", 0.0, None), ("30%", 0.3, None), ("60%", 0.6, None), ("90%", 0.9, None), ("다 참 → 발동 0.07초(가장 큼)", 0.0, 0.07), ("발동 0.18초", 0.0, 0.18), ("발동 0.30초(새 쿨다운)", 0.0, 0.30)]
    cw, ch = 210, 96; lab = 250
    out = Image.new("RGBA", (lab + len(moments) * cw + 20, 70 + len(VARIANTS) * (ch + 12) + 50), (18, 18, 18, 255)); d = ImageDraw.Draw(out)
    for n, (name, _, _) in enumerate(moments):
        for j, ln in enumerate(M.wrap_segments([(name, DIM)], font(16), cw - 10)): M.draw_segments(d, lab + n * cw, 14 + j * 20, ln, font(16))
    for r, (v, label) in enumerate(VARIANTS):
        y = 70 + r * (ch + 12)
        for j, ln in enumerate(M.wrap_segments([(label, TEXT)], font(16), lab - 20)): M.draw_segments(d, 12, y + j * 20, ln, font(16))
        for n, (_, c, s) in enumerate(moments):
            cell = Image.new("RGBA", (cw - 10, ch), (30, 26, 24, 255)); draw_item(cell, (8, 18), p, v, c, s)
            out.alpha_composite(cell, (lab + n * cw, y))
    d.text((12, out.height - 40), "롱소드(3×1). 발동하는 순간 쿨다운은 0부터 다시 찬다: 커짐이 끝나는 0.30초에 어둠으로 돌아간다.", font=font(17), fill=DIM)
    out.convert("RGB").save(path); print(path.name, out.size)


def stills():
    a = map_bc(); a.convert("RGB").save(HERE / "mock-BC-map.png"); print("mock-BC-map")
    card_still().convert("RGB").save(HERE / "mock-BC-card.png"); print("mock-BC-card")
    M.sheet(flow_bags(), 2, HERE / "mock-BC-bags.png", size=22,
            footer=["가방(예시): 가죽 배낭 3×4(기본, 빼거나 넘겨주지 못함), 가죽 주머니 3×1, 허리 주머니 2×1. 틀은 3×6, 가방 밖의 칸은 점선.",
                    "가방은 보드 사이로 옮길 수 있고 안의 아이템이 함께 간다. 빈 가방만 인벤토리로 뺀다."])
    M.sheet(flow_turn(), 2, HERE / "mock-BC-turn-stars.png", size=22,
            footer=["★은 닿은 아이템이 있는 변에만 보인다. 밝음 = 그 아이템의 분류가 조건에 맞음, 흐림 = 닿았지만 안 맞음. 왼쪽 위의 별 표 = 닿음 효과가 있는 아이템.",
                    "★의 효과는 그림을 위한 예시다(플라스크·버클러·약초 주머니·수호 부적). 같은 용병의 보드 안에서만 닿는다."])
    cols, party, enemies = battle_scene(); bg = battle_background(cols)
    battle_frame(bg, cols, party, enemies, 2.32, "v1").convert("RGB").save(HERE / "mock-BC-battle.png"); print("mock-BC-battle")
    cooldown_strip(HERE / "mock-cooldown-frames.png")


if __name__ == "__main__":
    what = sys.argv[1] if len(sys.argv) > 1 else "all"
    if what in ("all", "stills"): stills()
    if what in ("all", "videos"): compare_video(); battle_video()
