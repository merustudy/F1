# -*- coding: utf-8 -*-
"""Round 49, Diablo II changed (the user, 2026-10-09: "Ui 디아블로 스타일에서 조금 더 변형할게 1. 전투 화면에서는 아이템 뒤 파란색 배경
없음 2. 글씨체는 기존 우리 스타일 적용 3. 용병 가방(인벤) 추가 시 적용될 ui. 적용 후 다시 목업 보여줘").
1. On the battle screen the items lie on the black squares without the blue (also after the win, on the same screen).
2. Every line in Pretendard, today's font; the name plates as today (a number disc and the name). Diablo's colours stay.
3. Adding a bag: a bag's rim is its leather with the stitching (round 48), round Diablo's black squares. While a bag is held the
   frame's empty places show as sunk stone squares with a dashed line, on every board. The shop window in Diablo's stone with
   the bag's tile; the elite's bag on the floor of the won battle with Diablo's ground label; and one board's states (holding,
   fits, over another bag, placed, moving a bag with what it holds, a bag that cannot be picked).
Drawn shapes and today's icons; no API call.
  .venv/bin/python ArtPipeline/Archive/49-inventory-style/mock_diablo_v2.py
"""
import sys
from pathlib import Path
from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageStat

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import mock_inventory_style as S  # noqa: E402
import mock_battle as MB  # noqa: E402
from mock_inventory_style import (s, B, D, cell, span, it, bag, cells, PACK, PAD, SQ, TOP, COLS, comp, text, font, line, rrect,  # noqa: E402
                                  dashed, fill_tex, drop, faded, mul, fit, silhouette, num_badge, lighten, shade, width_of, LEATHER, VIOLET)

ROOT = S.ROOT
POTION = ROOT / "Assets/@Art/Potion"
SHOP = (1012, 178, 1868, 742)                 # today's shop window over the map (ko_43)
TILES = [1038, 1243, 1447, 1651]; TILE_Y, TILE_W, TILE_H = 290, 192, 366
FRAME_LINE = (0x78, 0x6C, 0x5C)               # UiPalette.GridFrameLine
NAME_BAG = {k: v[2] for k, v in S.BAGS.items()}


class DiabloV2(S.Diablo):
    key, title = "D2v2", "디아블로 2 변형 (전투는 바탕 없음, Pretendard, 가방의 가죽 테)"
    INV_TITLE = "인벤토리"
    TEXT = (235, 235, 230); DIM = (154, 160, 172)

    def f(self, px): return font(px)

    def plate(self, img, x, y, m):
        """As today: a dark strip with the number on a disc and the name (an enemy's in red)."""
        d = D(img)
        d.rectangle(B(x - 3, y, x + 157, y + 19), fill=(14, 13, 12))
        line(d, [(x - 3, y + 19), (x + 157, y + 19), (x + 157, y)], (104, 98, 90), 1)
        num_badge(d, x + 7, y + 9.5, m["num"], (196, 162, 92) if not m.get("enemy") else (170, 60, 48), (24, 16, 8), r=7)
        text(d, (x + 20, y + 10), m["name"], font(13), self.RED if m.get("enemy") else self.TEXT, "lm")
        if m.get("fat"): text(d, (x + 153, y + 10), f"피로 +{m['fat']}", font(11), VIOLET, "rm")

    def rim(self, img, X, Y, w, h, leather, q=SQ, r=6):
        """A bag's rim: its leather, a little raised, with the stitching just outside the squares (round 48)."""
        W, H = span(w, q), span(h, q); box = (X - r, Y - r, X + W + r, Y + H + r)
        fill_tex(img, box, shade(leather, 0.92), amp=10, coarse=5)
        d = D(img)
        line(d, [(box[0], box[3]), (box[0], box[1]), (box[2], box[1])], lighten(leather, 42), 1.2)
        line(d, [(box[0], box[3]), (box[2], box[3]), (box[2], box[1])], shade(leather, 0.42), 1.2)
        dashed(d, (box[0] + 3, box[1] + 3, box[2] - 3, box[3] - 3), lighten(leather, 80), 1.3, 5, 3)
        line(d, [(X - 1, Y + H), (X - 1, Y - 1), (X + W, Y - 1)], (6, 6, 6), 1.5)

    def bags(self, img, x0, y0, bags):
        for b in bags: self.rim(img, *cell(x0, y0, b["x"], b["y"]), b["w"], b["h"], LEATHER[b["id"]])
        for b in bags: self.squares(img, *cell(x0, y0, b["x"], b["y"]), b["w"], b["h"])

    def frame(self, img, x0, y0, bags, holding):
        """While a bag is held: the frame's places without a bag, as sunk stone squares with a dashed line (round 48's cue)."""
        if not holding: return
        d = D(img); used = set().union(*[cells(b) for b in bags]) if bags else set()
        for gx in range(3):
            for gy in range(8):
                if (gx, gy) in used: continue
                cx, cy = cell(x0, y0, gx, gy)
                d.rectangle(B(cx, cy, cx + SQ, cy + SQ), fill=(36, 35, 33))
                line(d, [(cx, cy + SQ), (cx, cy), (cx + SQ, cy)], (12, 12, 12), 1)
                line(d, [(cx, cy + SQ), (cx + SQ, cy + SQ), (cx + SQ, cy)], (92, 88, 82), 1)
                dashed(d, (cx + 3, cy + 3, cx + SQ - 3, cy + SQ - 3), FRAME_LINE, 1.2, 4, 3)

    def parts(self, i, mode=None, battle=False, q=SQ):
        under, art, over = super().parts(i, mode=mode, battle=battle, q=q)
        if battle: under = Image.new("RGBA", under.size, (0, 0, 0, 0))   # 1. no blue on the battle screen
        return under, art, over

    def held_bag(self, img, x0, y0, b, kind="fits", label=None, carried=()):
        """The bag in the hand over the frame: its rim and squares a little see-through, the squares green (or red), and what it
        carries with it."""
        lay = Image.new("RGBA", img.size, (0, 0, 0, 0)); self.bags(lay, x0, y0, [b]); comp(img, faded(lay, 0.88), (0, 0))
        d = D(img); col = (176, 30, 30, 125) if kind == "refused" else (26, 150, 48, 110)
        for (gx, gy) in cells(b):
            cx, cy = cell(x0, y0, gx, gy); d.rectangle(B(cx, cy, cx + SQ, cy + SQ), fill=col)
        for i in carried:
            X, Y = cell(x0, y0, b["x"] + i["x"], b["y"] + i["y"]); comp(img, faded(self.piece(dict(i, x=0, y=0)), 0.9), (s(X - PAD), s(Y - PAD)))
        X, Y = cell(x0, y0, b["x"], b["y"])
        if label: self.tag(img, X + span(b["w"]) / 2, Y + span(b["h"]) + 6, label, self.RED if kind == "refused" else self.WHITE)

    def bag_piece(self, b, q=SQ):
        """A bag as a thing (in a shop tile, on the floor): its rim and squares on their own."""
        W, H = span(b["w"], q), span(b["h"], q); m = 10
        lay = Image.new("RGBA", (s(W + 2 * m), s(H + 2 * m)), (0, 0, 0, 0))
        self.rim(lay, m, m, b["w"], b["h"], LEATHER[b["id"]], q); self.squares(lay, m, m, b["w"], b["h"], q)
        return lay, m

    # ---- the shop window -----------------------------------------------------------------------------------------------------

    def button(self, img, box, t, gold=False, dim=False):
        d = D(img)
        fill_tex(img, box, (40, 38, 36) if not gold else (64, 48, 22), amp=12, coarse=6)
        d = D(img)
        line(d, [(box[0], box[3]), (box[0], box[1]), (box[2], box[1])], (130, 122, 108), 1.5)
        line(d, [(box[0], box[3]), (box[2], box[3]), (box[2], box[1])], (10, 10, 10), 1.5)
        text(d, ((box[0] + box[2]) / 2, (box[1] + box[3]) / 2), t, font(21), self.GOLD if not dim else (110, 104, 92), "mm")

    def coin(self, d, cx, cy, r=10):
        d.ellipse(B(cx - r, cy - r, cx + r, cy + r), fill=(204, 166, 64), outline=(96, 72, 20), width=s(1.5))
        d.ellipse(B(cx - r * 0.55, cy - r * 0.55, cx + r * 0.55, cy + r * 0.55), outline=(150, 112, 30), width=s(1))

    def tile(self, img, x, kind, held=False):
        y = TILE_Y; box = (x, y, x + TILE_W, y + TILE_H); d = D(img)
        d.rectangle(B(*box), fill=(18, 17, 16))
        line(d, [(box[0], box[3]), (box[0], box[1]), (box[2], box[1])], (8, 8, 8), 1.5)
        line(d, [(box[0], box[3]), (box[2], box[3]), (box[2], box[1])], (108, 102, 94), 1.5)
        cx = x + TILE_W / 2; area = (y + 14, y + 180)
        name, sub, facts, price = kind["name"], kind["sub"], kind["facts"], kind["price"]
        if kind.get("bag"):
            b = bag(kind["bag"]); lay, m = self.bag_piece(b); lay = faded(lay, 0.7) if held else lay
            W, H = span(b["w"]), span(b["h"]); comp(img, lay, (s(cx - W / 2 - m), s((area[0] + area[1]) / 2 - H / 2 - m)))
        elif kind.get("potion"):
            X, Y = cx - 25, (area[0] + area[1]) / 2 - 25
            self.well(img, X, Y, 1, 1); self.squares(img, X, Y, 1, 1)
            dd = D(img); dd.rectangle(B(X, Y, X + SQ, Y + SQ), fill=(22, 32, 88, 205))
            ic = fit(Image.open(POTION / f"{kind['potion']}.png").convert("RGBA"), s(42), s(42)); comp(img, ic, (s(cx) - ic.width // 2, s(Y + 25) - ic.height // 2))
        else:
            i = kind["item"]; W, H = span(i["w"]), span(i["h"]); X, Y = cx - W / 2, (area[0] + area[1]) / 2 - H / 2
            self.well(img, X, Y, i["w"], i["h"]); self.squares(img, X, Y, i["w"], i["h"])
            comp(img, self.piece(i), (s(X - PAD), s(Y - PAD)))
        d = D(img)
        text(d, (cx, y + 198), name, font(18), kind.get("color", self.WHITE), "mm")
        text(d, (cx, y + 220), sub, font(14), self.DIM, "mm")
        line(d, [(x + 14, y + 236), (x + TILE_W - 14, y + 236)], (60, 58, 54), 1)
        yy = y + 252
        for t, col in facts:
            text(d, (cx, yy), t, font(14), col, "mm"); yy += 19
        self.coin(d, cx - 22, y + TILE_H - 26); text(d, (cx - 6, y + TILE_H - 25), str(price), font(22), self.GOLD, "lm")
        if held:
            d.rectangle(B(x - 2, y - 2, x + TILE_W + 2, y + TILE_H + 2), outline=self.GOLD, width=s(2.5))
            self.tag(img, cx, y + 8, "손에 듦", self.GOLD)

    def shop(self, img):
        x0, y0, x1, y1 = SHOP
        drop(img, SHOP, 2, alpha=170, off=(0, 6), blur=8, color=(0, 0, 0))
        self.stone(img, SHOP, base=(56, 54, 51))
        d = D(img)
        for k in range(3): d.ellipse(B(x0 + 40, y0 + 52 - 7 * k, x0 + 84, y0 + 68 - 7 * k), fill=(204, 166, 64), outline=(96, 72, 20), width=s(1.5))
        text(d, (x0 + 100, y0 + 46), "4층 · 상점", font(30), self.GOLD, "lm")
        text(d, (x0 + 100, y0 + 84), "지역 코인으로 삽니다. 나가면 다음 층으로 갑니다.", font(16), self.DIM, "lm")
        text(d, (x1 - 120, y0 + 48), "가진 코인", font(17), self.DIM, "rm"); self.coin(d, x1 - 100, y0 + 48, 11)
        text(d, (x1 - 32, y0 + 48), "37", font(28), self.GOLD, "rm")
        kinds = [dict(name="치유 포션", sub="포션", facts=[("HP 50 회복", self.WHITE)], price=8, potion="healing_potion"),
                 dict(name="가죽 주머니", sub="가방 · 3×1", facts=[("칸 +3", self.BLUE), ("틀의 빈 자리에 놓습니다", self.WHITE),
                                                             ("인벤토리에는 넣지 않습니다", self.DIM)], price=12, bag="leather_pouch", color=self.GOLD),
                 dict(name="미늘창 · 등급 8", sub="무기 장비 · 3×3", facts=[("쿨다운 3.6초", self.WHITE), ("맨 앞에서만 발동", self.WHITE),
                                                                    ("앞의 적 3명에게 피해 6", self.BLUE), ("전투마다 피로 +1", VIOLET)],
                      price=22, item=dict(id="halberd", x=0, y=0, w=3, h=3, tier="common", rot=False)),
                 dict(name="방벽 포션", sub="포션", facts=[("보호막 40", self.WHITE)], price=8, potion="barrier_potion")]
        for x, k in zip(TILES, kinds): self.tile(img, x, k, held=bool(k.get("bag")))
        self.button(img, (1036, 676, 1283, 720), "새로고침  ·  3")
        self.button(img, (1656, 676, 1843, 720), "나가기")

    # ---- the board panel while a bag is held (every board shows its frame) ----------------------------------------------------

    def boards_holding(self, img, mercs, ghost_on, ghost, label, kind="fits"):
        for x0, m in zip(COLS, mercs):
            self.frame(img, x0, TOP, m["bags"], True)
            self.board(img, x0, TOP, m)
            if m["name"] == ghost_on: self.held_bag(img, x0, TOP, ghost, kind, label)
            self.plate(img, x0, TOP - 24, m)


def carry(img, shot, box, base=(53, 51, 53)):
    """Something of today's lower panel (a line, buttons) carried onto the new panel: as MB.candle, for any box."""
    old = MB.CANDLE; MB.CANDLE = box
    try: MB.candle(img, shot)
    finally: MB.CANDLE = old


def overpaint_line(img, segs, y=924, clear=(998, 906, 1886, 964)):
    d = D(img); d.rectangle(B(*clear), fill=(27, 31, 40)); x = 1001
    for t, c in segs: text(d, (x, y), t, font(17), c, "ls"); x += width_of(t, font(17))


def shop_screen(st):
    shot = Image.open(S.SHOTS / "ko_43_shop_pick.png").convert("RGB")
    img = shot.resize((s(1920), s(1080)), Image.LANCZOS)
    st.panel(img, S.LEFT)
    st.boards_holding(img, S.MERCS, "로언", bag("leather_pouch", 0, 4), "가죽 주머니 · 칸 +3 · 12코인")
    st.shop(img)
    overpaint_line(img, [("가죽 주머니 — 가방 · 3×1 / 칸 +3. 틀의 빈 자리에 놓으면 삽니다(12코인). 안의 아이템과 함께 옮길 수 있습니다.", (235, 235, 230))])
    d = D(img); d.rectangle(B(1648, 992, 1876, 1048), fill=(10, 10, 14, 150))   # "인벤토리에 넣기" is off: a bag does not go there
    X, Y = cell(COLS[3], TOP, 0, 4); S.cursor(img, X + span(3) / 2 + 24, Y + 28)
    return img.resize((1920, 1080), Image.LANCZOS)


def heal(img, box, dx):
    """Cover a box of the stage with the stage dx beside it: matched to the box's own edges in brightness, feathered."""
    x0, y0, x1, y1 = [s(v) for v in box]; d = s(dx); e = s(8)
    src = img.crop((x0 + d, y0, x1 + d, y1)); dst = img.crop((x0, y0, x1, y1))
    edges = lambda im: [sum(c) / 2 for c in zip(ImageStat.Stat(im.crop((0, 0, e, im.height))).mean, ImageStat.Stat(im.crop((im.width - e, 0, im.width, im.height))).mean)]
    ratio = [a / max(1.0, b) for a, b in zip(edges(dst), edges(src))]
    src = Image.merge("RGB", [c.point(lambda v, r=r: min(255, int(v * r))) for c, r in zip(src.split(), ratio)])
    m = Image.new("L", src.size, 0); ImageDraw.Draw(m).rectangle([e, e, src.width - e, src.height - e], fill=255)
    img.paste(src, (x0, y0), m.filter(ImageFilter.GaussianBlur(e / 2)))


def loot_screen(st):
    shot = Image.open(S.SHOTS / "ko_07_loot_picked.png").convert("RGB")
    img = shot.resize((s(1920), s(1080)), Image.LANCZOS)
    # the floor: the dropped blade and its labels covered with the floor beside them, then the elite's bag lying there
    heal(img, (1046, 346, 1234, 432), 196); heal(img, (1050, 478, 1240, 566), 192)
    st.panel(img, MB.PANEL)
    for box in (MB.CANDLE, (1040, 840, 1600, 876), (1050, 988, 1292, 1056), (1308, 988, 1492, 1056), (1638, 988, 1882, 1056)): carry(img, shot, box)
    b = bag("belt_pouch"); lay, m = st.bag_piece(b); W, H = span(2), span(1); fx, fy = 1140 - W / 2, 520 - H / 2
    comp(img, silhouette(lay, (0, 0, 0), blur=s(6), alpha=150), (s(fx - m + 6), s(fy - m + 9)))
    comp(img, lay, (s(fx - m), s(fy - m)))
    d = D(img)
    text(d, (1140, fy - 52), "넣을 칸을 누르세요", font(17), st.GOLD, "mm")
    st.tag(img, 1140, fy - 40, "허리 주머니 · 가방 2×1 · 칸 +2", st.GOLD)
    mercs = [dict(m, items=[dict(i) for i in m["items"]]) for m in S.MERCS]
    for x0, mm in zip(COLS, mercs):
        st.frame(img, x0, TOP, mm["bags"], True)
        st.board(img, x0, TOP, mm, battle={})   # the battle screen: no blue
        if mm["name"] == "미라": st.held_bag(img, x0, TOP, bag("belt_pouch", 0, 3), "fits", "허리 주머니 · 칸 +2")
        st.plate(img, x0, TOP - 24, mm)
    X, Y = cell(COLS[0], TOP, 0, 3); S.cursor(img, X + span(2) / 2 + 20, Y + 26)
    d = D(img); d.rectangle(B(1052, 992, 1290, 1052), fill=(10, 10, 14, 150))   # "인벤토리에 넣기" is off: a bag does not go there
    return img.resize((1920, 1080), Image.LANCZOS)


def bag_states(st):
    """One board (Kai's) while adding and moving bags."""
    out = []; X0, Y0, Wc, Hc = 41, 46, 236, 492
    pouch = bag("leather_pouch", 0, 5)

    def one(label, m=S.DETAIL, holding=False, after=None):
        c = Image.new("RGB", (s(Wc), s(Hc)), (0, 0, 0)); st.panel(c, (0, 0, Wc, Hc))
        st.frame(c, X0, Y0, m["bags"], holding); st.board(c, X0, Y0, m)
        if after: after(c)
        st.plate(c, X0, Y0 - 24, m); out.append((label, c))

    one("① 가방을 들면 틀의 빈 자리가 보임", holding=True)
    one("② 맞는 자리: 초록 · 칸 +3", holding=True, after=lambda c: st.held_bag(c, X0, Y0, pouch, "fits", "가죽 주머니 · 칸 +3"))
    one("③ 다른 가방과 겹침: 빨강", holding=True,
        after=lambda c: st.held_bag(c, X0, Y0, bag("leather_pouch", 0, 3), "refused", "허리 주머니와 겹침"))

    def placed(c):
        X, Y = cell(X0, Y0, 0, 5); st.tag(c, X + span(3) / 2, Y + span(1) + 8, "칸 +3 · 코인 −12", st.GOLD)
    one("④ 놓은 뒤: 새 가방의 칸", dict(S.DETAIL, bags=S.DETAIL["bags"] + [pouch]), after=placed)
    moving = dict(S.DETAIL, bags=[PACK], items=[i for i in S.DETAIL["items"] if i["id"] != "herb_pouch"])
    one("⑤ 가방 옮기기 (든 것과 함께)", moving, holding=True,
        after=lambda c: (st.held_bag(c, X0, Y0, bag("belt_pouch", 0, 6), "fits", "허리 주머니 · 약초 주머니와 함께", carried=[S.it("herb_pouch")]),
                         S.cursor(c, cell(X0, Y0, 1, 6)[0] + 30, cell(X0, Y0, 1, 6)[1] + 28)))
    across = dict(S.DETAIL, bags=[PACK, bag("leather_pouch", 0, 3)],
                  items=[S.it("longsword", 0, 0, "bronze"), S.it("dagger", 2, 2, rot=True), S.it("herb_pouch", 0, 2)])

    def refused_pick(c):
        X, Y = cell(X0, Y0, 0, 3); d = D(c)
        d.rectangle(B(X, Y, X + SQ, Y + SQ), fill=(176, 30, 30, 110))
        st.tag(c, X0 + span(3) / 2, cell(X0, Y0, 0, 4)[1] + 8, "걸친 단검 때문에 집을 수 없음", st.RED)
        S.cursor(c, X + 26, Y + 26)
    one("⑥ 걸친 아이템이 있으면 못 집음", across, after=refused_pick)
    return out


def main():
    st = DiabloV2(); MB.SHORT[st.key] = "디아블로 2 변형"
    v1 = S.Diablo()
    m2 = st.screen().resize((1920, 1080), Image.LANCZOS); m2.save(HERE / "mock-D2v2-map.png"); print("mock-D2v2-map.png")
    S.sheet(st.states(), 6, HERE / "mock-D2v2-states.png", title=f"{st.title}: 한 보드의 상태", scale=0.62, label=20)
    S.sheet(bag_states(st), 6, HERE / "mock-D2v2-bags.png", title="디아블로 2 변형: 가방을 더하고 옮길 때 (카이의 보드)", scale=0.62, label=20,
            footer=["가방의 테는 그 가죽과 바늘땀(Round 48), 안은 디아블로의 검은 칸. 가방을 든 동안만 틀의 빈 자리가 가라앉은 돌 칸과 점선으로 보인다.",
                    "규칙은 19단계 그대로: 빈 자리에만, 다른 가방과 겹치면 안 됨, 가방의 빈 칸을 눌러 집으면 든 것이 함께 감, 두 가방에 걸친 아이템이 있으면 못 집음."])
    shop = shop_screen(st); shop.save(HERE / "mock-D2v2-shop-bag.png"); print("mock-D2v2-shop-bag.png")
    loot = loot_screen(st); loot.save(HERE / "mock-D2v2-loot-bag.png"); print("mock-D2v2-loot-bag.png")
    keep = MB.video(st)
    keep.save(HERE / "mock-D2v2-battle.png")
    v1_map = Image.open(HERE / "mock-D2-map.png").convert("RGB"); v1_battle = Image.open(HERE / "mock-battle-compare.png")
    halfsz = lambda im: im.resize((960, 540), Image.LANCZOS)
    still_v1 = MB.stage(v1); still_v1 = MB.frame(v1, *still_v1, 49 / MB.FPS)
    S.sheet([("처음 디아블로 2: 노드 맵", halfsz(v1_map)), ("변형: 노드 맵 (Pretendard, 가죽 테)", halfsz(m2)),
             ("처음 디아블로 2: 전투", halfsz(still_v1)), ("변형: 전투 (파란 바탕 없음)", halfsz(keep)),
             ("변형: 상점에서 가방을 든 동안", halfsz(shop)), ("변형: 정예의 가방을 주운 동안", halfsz(loot))],
            2, HERE / "mock-D2v2-compare.png",
            footer=["1 전투 화면(이긴 뒤 포함)에서는 아이템 뒤 파란 바탕 없음  2 글은 모두 Pretendard, 이름표는 지금처럼 번호 원과 이름",
                    "3 가방: 가죽 테와 바늘땀, 든 동안 모든 보드에 틀의 빈 자리(가라앉은 돌 칸 + 점선), 놓을 자리 초록 / 안 되면 빨강"])


if __name__ == "__main__":
    main()
