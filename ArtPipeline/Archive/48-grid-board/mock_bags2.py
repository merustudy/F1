# -*- coding: utf-8 -*-
"""Round 48, third ask (2026-10-08): "아이템을 채우면 뒤에 크림색을 지우고, 빈칸은 가방의 밖 칸색을 적용, 가방 적용이 안되는 칸은
아예 안보이게". The B + C boards again: no cream tile behind an item, the empty squares of a bag in the dark of the old
outside squares, and nothing at all where no bag is (the board table shows through). Three readings side by side, and the
battle (the item's shade from mock_bags) on the new ground. No API call.
  .venv/bin/python ArtPipeline/Archive/48-grid-board/mock_bags2.py
"""
import shutil
import subprocess
import sys
from pathlib import Path
from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import mock_bags as B  # noqa: E402  (round 48 second ask: bags, stars, the cooldown's shade and swell, battle scene)

G = B.G; M = B.M; font = G.font; S = G.S
TEXT, DIM, INK, BRASS, VIRTUE, GOOD, DANGER = G.TEXT, G.DIM, G.INK, G.BRASS, G.VIRTUE, G.GOOD, G.DANGER
PARTY_X = G.PARTY_X
EMPTY, EMPTY_LINE = (30, 26, 24), (62, 56, 52)      # the old outside squares' dark (mock_bags.frame) with a thin line
PIECE, PIECE_LINE = (40, 35, 31), (92, 76, 54)       # 안 2: an item's squares as one piece


def table_patch(img):
    """A stretch of the board table under the columns (no skulls), to lay where no bag is."""
    return img.crop((120, 968, 840, 1062))


def hide_column(img, x0, patch, top=644, bottom=1022):
    for y in range(top, bottom, patch.height):
        img.paste(patch.crop((0, 0, 196, min(patch.height, bottom - y))), (x0 - 8, y))


def square(img, xy, fill, line, radius=4):
    x, y = xy; ImageDraw.Draw(img).rounded_rectangle([x, y, x + S - 1, y + S - 1], radius=radius, fill=fill + (255,), outline=line + (255,), width=1)


def piece(img, x0, i, fill, line, width=1):
    X, Y = B.cell_xy(x0, i["x"], i["y"]); W, H = G.px(i["w"]), G.px(i["h"])
    ImageDraw.Draw(img).rounded_rectangle([X, Y, X + W - 1, Y + H - 1], radius=5, fill=fill + (255,) if fill else None, outline=line + (255,), width=width)


def scaled(c, k): return tuple(max(0, min(255, int(v * k))) for v in c)


class Look:
    def __init__(self, key, label, empty, behind):
        self.key, self.label, self.empty, self.behind = key, label, empty, behind   # behind: "squares" | "piece"


LOOKS = [Look("1", "안 1 (지시 그대로): 크림 없음, 빈 칸 = 가방 밖 칸의 어두운 색, 아이템 뒤도 칸", "dark", "squares"),
         Look("2", "안 2 (권장): 안 1 + 아이템의 칸을 한 조각으로(모양이 읽힘)", "dark", "piece"),
         Look("3", "안 3: 빈 칸 = 가방 가죽의 어두운 색, 아이템은 밝은 가죽 조각", "leather", "piece")]


def ground(img, x0, bags, items, look, patch, battle=False, disabled=()):
    """The board without the cream: the table where no bag is, each bag's leather, its empty squares, and the items' ground."""
    hide_column(img, x0, patch)
    for b in bags: B.bag_under(img, x0, b)
    taken = set().union(*[B.cells_of(i) for i in items]) if items else set()
    for b in bags:
        color = B.BAGS[b["kind"]][2]
        for (gx, gy) in sorted(B.cells_of(b)):
            if (gx, gy) in taken and look.behind == "piece": continue
            if look.empty == "dark": square(img, B.cell_xy(x0, gx, gy), EMPTY, EMPTY_LINE)
            else: square(img, B.cell_xy(x0, gx, gy), scaled(color, 0.55), scaled(color, 0.4))
    for i in items:
        bag = next(b for b in bags if (i["x"], i["y"]) in B.cells_of(b))
        if look.behind == "piece":
            fill, line = (PIECE, PIECE_LINE) if look.empty == "dark" else (scaled(B.BAGS[bag["kind"]][2], 0.85), scaled(B.BAGS[bag["kind"]][2], 1.4))
            if i["id"] in disabled: fill, line = (46, 46, 46), (90, 90, 90)
            piece(img, x0, i, fill, line)
        if i.get("sel"):
            piece(img, x0, i, None, VIRTUE, width=2)
            X, Y = B.cell_xy(x0, i["x"], i["y"]); W, H = G.px(i["w"]), G.px(i["h"])
            lay = Image.new("RGBA", (W, H), (128, 102, 20, 90)); img.alpha_composite(lay, (X, Y))


def item_parts(i, enemy=False, map_tags=True):
    p = B.Parts(i, enemy)
    p.bg = Image.new("RGBA", p.size, (0, 0, 0, 0))               # no cream
    if i["fat"] and map_tags: G.fatigue_tag(p.tags, p.size[0] - 4, 4)
    return p


def board_map(img, x0, bags, items, look, patch):
    ground(img, x0, bags, items, look, patch)
    for i in items:
        p = item_parts(i); xy = B.cell_xy(x0, i["x"], i["y"])
        img.alpha_composite(p.sprite, xy); img.alpha_composite(p.tags, xy)
    for a, b, mx, my, lit in B.contacts(items): B.star_at(img, x0, mx, my, lit)


def map_look(look, scene=None):
    img = G.shot("ko_35_map_tiers"); patch = table_patch(img); skulls = img.crop(B.SKULLS)
    for x0, (bags, items) in zip(PARTY_X, scene or B.scene_map()): board_map(img, x0, bags, items, look, patch)
    for x0, n in zip(PARTY_X, (0, 0, 1, 2)): G.head_fatigue(img, x0, n)
    mask = Image.eval(skulls.convert("L"), lambda v: 255 if v > 70 else 0); img.paste(skulls, B.SKULLS[:2], mask)
    return img


def map_full(look):
    img = map_look(look)
    G.panel_lines(img,
                  [("불씨 플라스크 · 등급 8 — 공격 아이템 / 크기 2×1 / 쿨다운 4.5초 / 맨 앞 적에게 화상 2 / ", TEXT), ("★ 닿은 무기 장비가 피해를 주면 화상 +1(예시)", VIRTUE)],
                  [("가방 안에만 놓습니다. 들고 있을 때 R 또는 휠: 돌리기. ★ 밝음 = 닿은 아이템에 걸림. 우클릭은 아이템 정보.", TEXT)])
    return img


def flows(look):
    """With nothing outside the bags on screen, where a bag can go shows only while one is in hand."""
    frames = []
    img = map_look(look)
    for x0, (bags, _) in zip(PARTY_X, B.scene_map()):
        cells = set().union(*[B.cells_of(b) for b in bags])
        for gx in range(3):
            for gy in range(B.ROWS):
                if (gx, gy) not in cells: B.dashed_cell(img, x0, gx, gy, color=(120, 108, 92))
    B.held_bag(img, PARTY_X[0], B.bagd("pouch", 0, 4), GOOD, "가방 놓기: 칸 +3")
    frames.append(("① 가방을 들 때만 가방이 갈 수 있는 자리(틀 3×6)가 점선으로 나타난다", img.crop(B.CROP)))
    img = map_look(look); G.ghost(img, PARTY_X[2], 2, 3, 1, 2, GOOD, icon_id="dagger", rot=True, label="놓기")
    cx, cy = B.cell_xy(PARTY_X[2], 2, 3); G.cursor(img, cx + 30, cy + 50)
    frames.append(("② 아이템을 들 때는 가방 칸만: 놓일 칸이 초록", img.crop(B.CROP)))
    return frames


# ---- battle on the new ground ---------------------------------------------------------------------------------------------

def battle_ground(look, cols, disabled_cols):
    img = G.shot("ko_05_battle"); patch = table_patch(img)
    for col, (x0, (bags, items)) in enumerate(zip(PARTY_X, cols)):
        items = [dict(i, sel=False) for i in items]          # nothing is picked in a battle
        ground(img, x0, bags, items, look, patch, battle=True, disabled=disabled_cols.get(col, ()))
    for x0 in G.ENEMY_X:
        # enemies carry no bags: their board is only the squares of what they hold
        hide_column(img, x0, patch, top=644, bottom=714)
        piece(img, x0, G.it("rat_bite", 0, 0), PIECE, PIECE_LINE)
    return img


def battle_items(cols):
    _, party, enemies = B.battle_scene()
    for u in party: u.p = item_parts(u.i, map_tags=False)
    for u in enemies: u.p = item_parts(u.i, enemy=True, map_tags=False)
    return party, enemies


PANELS = [("old", "이전: 크림 판 위, 시작 어둠 20%", 0.20), ("2", "안 2: 어두운 칸 위, 시작 어둠 20%", 0.20), ("2", "안 2 (권장): 어두운 칸 위, 시작 어둠 40%", 0.40)]


def compare_battle_video():
    cols, party_old, enemies_old = B.battle_scene()
    cols[0] = (cols[0][0], [i for i in cols[0][1]])
    bg_old = B.battle_background(cols)
    look = LOOKS[1]; bg_new = battle_ground(look, cols, {0: ("dagger",)})
    party_new, enemies_new = battle_items(cols)
    crop = B.CMP; w, h = crop[2] - crop[0], crop[3] - crop[1]; lab = 40; gap = 16
    W = len(PANELS) * (w + gap) + gap; H = h + lab + 46

    def frame(k):
        t, speed = B.timeline(k)
        out = Image.new("RGBA", (W, H), (18, 18, 18, 255)); d = ImageDraw.Draw(out)
        for n, (kind, label, dark) in enumerate(PANELS):
            B.DARK = dark
            im = B.battle_frame(bg_old, cols, party_old, enemies_old, t, "v1") if kind == "old" else B.battle_frame(bg_new, cols, party_new, enemies_new, t, "v1")
            x = gap + n * (w + gap); d.text((x, 10), label, font=font(18), fill=TEXT); out.alpha_composite(im.crop(crop), (x, lab))
        B.DARK = 0.20
        d.text((gap, H - 38), f"{speed}   t = {t:4.2f}초   ·   쿨다운 연출은 안 1(아이템만 음영 → 밝아짐 → 1.12배). 카이(2열)와 로언(1열).", font=font(17), fill=DIM)
        return out
    B.write_video(frame, 16 * B.FPS, HERE / "mock-BC2-battle-compare.mp4")

    B.DARK = 0.40
    full = B.battle_frame(bg_new, cols, party_new, enemies_new, 3.1, "v1"); full.convert("RGB").save(HERE / "mock-BC2-battle.png"); print("mock-BC2-battle")
    B.DARK = 0.20
    still = Image.new("RGBA", (W, h + lab + 10), (18, 18, 18, 255)); d = ImageDraw.Draw(still)
    for n, (kind, label, dark) in enumerate(PANELS):
        B.DARK = dark
        im = B.battle_frame(bg_old, cols, party_old, enemies_old, 0.9, "v1") if kind == "old" else B.battle_frame(bg_new, cols, party_new, enemies_new, 0.9, "v1")
        x = gap + n * (w + gap); d.text((x, 10), label, font=font(18), fill=TEXT); still.alpha_composite(im.crop(crop), (x, lab))
    B.DARK = 0.20
    still.convert("RGB").save(HERE / "mock-BC2-battle-compare.png"); print("mock-BC2-battle-compare.png")


def main():
    old = B.base_map(); looks = [map_look(l) for l in LOOKS]
    M.sheet([("이전 (B + C 목업): 크림 판, 가방 칸, 가방 밖 점선", old.crop(B.CROP))] + [(l.label, im.crop(B.CROP)) for l, im in zip(LOOKS, looks)],
            2, HERE / "mock-BC2-compare.png", size=21,
            footer=["가방이 없는 자리는 보드 판 바탕 그대로(틀도 없다). 머리 띠(열 번호, 이름, 피로 합계)는 그대로.",
                    "같은 장비·같은 가방. ★과 왼쪽 위 ★ 표, 단계 외곽선·별 표, 피로 표는 그대로."])
    map_full(LOOKS[1]).convert("RGB").save(HERE / "mock-BC2-map.png"); print("mock-BC2-map")
    M.sheet(flows(LOOKS[1]), 2, HERE / "mock-BC2-flow.png", size=21,
            footer=["가방 밖이 보이지 않으므로, 가방을 손에 든 동안만 틀(3×6)의 빈 자리를 점선으로 보인다. 아이템을 들 때는 보이지 않는다."])
    compare_battle_video()


if __name__ == "__main__":
    main()
