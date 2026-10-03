#!/usr/bin/env python3
"""Mockups of the board panel with room for six cells in a unit's line (MaxItemSlots = 6).

Reuses the renderer of round 07 (mock_bottom_panel.py): the game's own sprites (background, figures,
faces, UI frames, item icons, Pretendard) drawn at 2x and scaled to 1920x1080. No API call.

  now      : the current panel, boards of 4 cells of 180x60
  overflow : the same cells with 6 slots: a line does not fit in half the panel (why the cells must shrink)
  A        : cells 138x46 (the icons' 3:1 shape), the face stays 60, the panel and the stage stay
  B        : everything in the line scaled the same (face 46), the panel lowered to 248, the stage stays
  C        : cells only narrowed (138x60): the icons sit in a band in the middle of a taller cell
  A-map    : the node map with variant A's lines in the panel's left half

Eight cells (the second round of the mockup, 2026-10-03: "a bit taller, no need to keep the strip"):
  D-asis   : cells 100x60, eight in a line, the strip icons as they are (they turn into slivers)
  D        : cells 100x60 with stand-ins for icons redrawn in the cell's shape, the panel stays
  E        : cells 100x72, the face 72, the panel 372 high
  F        : cells 100x84, the face 84, the panel 402 high (18 px under the plates)
  E-map    : the node map with variant E's lines
A stand-in crops the current icon to the cell's inner shape: the business end of a long item, the
middle of a compact one. It only shows the size an icon drawn for that shape would have.

Variant E chosen (2026-10-03): "boards start at 5 cells and grow to 8; show only the cells a unit has;
a bag behind them, like Backpack Battles, would be nice too". The third sheet (`bag`):
  E-five   : E with 5 / 8 / 6 / 5 cells, nothing behind the cells
  E-bag    : the same with a leather bag behind each unit's cells, as long as the unit's cells
  E-bag-map: the node map with the bags

Square cells (2026-10-03, after E was implemented: "a 1-cell item as a square, and the panel a little
lower"). The fourth sheet (`square`): the cell is a square as high as the line, the face the same size,
the bag around them; eight squares and a face take well under half the panel, so the panel's height
follows the cell: G (72, panel 358), J (68, panel 342), H (64, panel 326). Stand-in icons are the
current icons cropped square (the business end of a long item).
  square-now, square-G, square-J, square-H, square-H-map
"""
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent / "07-battle-panel"))
from PIL import Image, ImageDraw, ImageFont  # noqa: E402

import mock_bottom_panel as m  # noqa: E402
import mock_party_panel as party  # noqa: E402

ROOT = m.ROOT
FACES = ROOT / "Assets" / "@Art" / "Face"
STRIPS = ROOT / "ArtPipeline" / "output" / "item"  # the strip icons of the 2- and 3-cell items (pending)
OUT = ROOT / "ArtPipeline" / "output" / "review"

SEAM_X = 959


def icon_of(key):
    """The item's icon: the pending strip for the 2- and 3-cell items, the game's icon otherwise."""
    p = STRIPS / f"{key}.png"
    if key in ("longbow", "spear", "halberd") and p.exists():
        return Image.open(p).convert("RGBA")
    return m.item_icon(key)


def face_of(side, key):
    return Image.open(FACES / ("Job" if side == "party" else "Enemy") / f"{key}.png").convert("RGBA")


class Geometry:
    """The sizes of one unit's line in the panel and of the panel itself."""

    def __init__(self, cell_w, cell_h, cell_gap, face, face_inset, face_gap, margin, icon_mx, icon_my,
                 badge, line_gap, panel_h, line_h=None, stand_in=False, pad=None, bag_pad_y=4):
        self.cell_w, self.cell_h, self.cell_gap = cell_w, cell_h, cell_gap
        self.stand_in = stand_in
        self.pad = pad
        self.bag_pad_y = bag_pad_y
        self.face, self.face_inset, self.face_gap = face, face_inset, face_gap
        self.margin = margin
        self.icon_mx, self.icon_my = icon_mx, icon_my
        self.badge = badge
        self.line_gap = line_gap
        self.panel_h = panel_h
        self.line_h = line_h if line_h is not None else max(cell_h, face)
        self.panel_y = 1080 - panel_h

    def board_w(self, cells):
        return cells * self.cell_w + (cells - 1) * self.cell_gap

    def line_end(self, cells):
        return self.margin + self.face + self.face_gap + self.board_w(cells)

    def lines_top(self):
        if self.pad is not None:
            return self.panel_y + self.pad
        return self.panel_y + (self.panel_h - (4 * self.line_h + 3 * self.line_gap)) / 2

    def line_y(self, row):
        return self.lines_top() + (row - 1) * (self.line_h + self.line_gap)


NOW = Geometry(cell_w=180, cell_h=60, cell_gap=6, face=60, face_inset=5, face_gap=8, margin=40,
               icon_mx=10, icon_my=8, badge=22, line_gap=8, panel_h=324)
# A: the cells keep the icons' shape (3:1) and shrink until six fit; the face and the line stay 60 high
A = Geometry(cell_w=138, cell_h=46, cell_gap=5, face=60, face_inset=5, face_gap=8, margin=24,
             icon_mx=8, icon_my=6, badge=22, line_gap=8, panel_h=324, line_h=60)
# B: the whole line scaled by 46/60; the panel as low as the four lines need
B = Geometry(cell_w=138, cell_h=46, cell_gap=5, face=46, face_inset=4, face_gap=6, margin=31,
             icon_mx=8, icon_my=6, badge=18, line_gap=6, panel_h=248)
# C: only the width changes; the icons keep their size of A inside a cell as tall as now
C = Geometry(cell_w=138, cell_h=60, cell_gap=5, face=60, face_inset=5, face_gap=8, margin=24,
             icon_mx=8, icon_my=8, badge=22, line_gap=8, panel_h=324)


# Eight cells in a line: the width is the limit (about 100), the height is free. The strip icons
# cannot stay: in a cell 100 wide a 3.6:1 icon is 84x23. So the icons would be redrawn for the cell's shape.
D_ASIS = Geometry(cell_w=100, cell_h=60, cell_gap=4, face=60, face_inset=5, face_gap=8, margin=24,
                  icon_mx=8, icon_my=6, badge=22, line_gap=8, panel_h=324)
D = Geometry(cell_w=100, cell_h=60, cell_gap=4, face=60, face_inset=5, face_gap=8, margin=24,
             icon_mx=8, icon_my=6, badge=22, line_gap=8, panel_h=324, stand_in=True)
E = Geometry(cell_w=100, cell_h=72, cell_gap=4, face=72, face_inset=6, face_gap=8, margin=24,
             icon_mx=8, icon_my=6, badge=24, line_gap=12, panel_h=372, stand_in=True)
F = Geometry(cell_w=100, cell_h=84, cell_gap=4, face=84, face_inset=7, face_gap=8, margin=24,
             icon_mx=8, icon_my=6, badge=26, line_gap=6, panel_h=402, stand_in=True, pad=24)


# Square cells: the cell is as high as it is wide and the face is the same size. The lines are 10 apart
# and the bag reaches 3 above and below the cells, so the bags of two lines stay apart (4 px).
G = Geometry(cell_w=72, cell_h=72, cell_gap=4, face=72, face_inset=6, face_gap=8, margin=24,
             icon_mx=6, icon_my=6, badge=24, line_gap=10, panel_h=358, stand_in=True, pad=20, bag_pad_y=3)
J = Geometry(cell_w=68, cell_h=68, cell_gap=4, face=68, face_inset=6, face_gap=8, margin=24,
             icon_mx=6, icon_my=6, badge=22, line_gap=10, panel_h=342, stand_in=True, pad=20, bag_pad_y=3)
H = Geometry(cell_w=64, cell_h=64, cell_gap=4, face=64, face_inset=5, face_gap=8, margin=24,
             icon_mx=5, icon_my=5, badge=22, line_gap=10, panel_h=326, stand_in=True, pad=20, bag_pad_y=3)


def draw_cell(c, g, x, y, cells, item=None, charge=0.0, mirrored=False, grade=None, selected=False, dim=False, empty_label=None):
    """A cell of the kit: the charge inside the rim from the left, the icon inside the margins, the grade badge."""
    w, h = g.board_w(cells), g.cell_h
    if item is None:
        c.frame("slot", x, y, w, h, alpha=0.45)
        if empty_label:
            c.text(x + w / 2, y + h / 2, empty_label, 15 if h < 50 else 19, m.TEXT_DIM, "mm")
        return
    c.frame("slot_selected" if selected else "slot", x, y, w, h, alpha=0.55 if dim else 1.0)
    if charge > 0:
        rim = 7 if h >= 50 else 6
        c.rect(x + rim, y + rim, max(0, (w - 2 * rim) * charge), h - 2 * rim, m.GAUGE + (int(255 * 0.84),))
    icon = icon_of(item)
    box_w, box_h = w - 2 * g.icon_mx, h - 2 * g.icon_my
    if g.stand_in:
        icon = stand_in(icon, item, box_w / box_h)
    if mirrored:
        icon = icon.transpose(Image.FLIP_LEFT_RIGHT)
    ratio = min(m.S(box_w) / icon.width, m.S(box_h) / icon.height)
    icon = icon.resize((max(1, int(icon.width * ratio)), max(1, int(icon.height * ratio))), Image.LANCZOS)
    c.im.alpha_composite(icon, (m.S(x + g.icon_mx) + (m.S(box_w) - icon.width) // 2, m.S(y + g.icon_my) + (m.S(box_h) - icon.height) // 2))
    if grade is not None:
        size = 24 if h >= 50 else 20
        c.badge(x + 5, y + h - 5 - size, size, grade, 14 if h >= 50 else 12)


END_FIRST = m.LONG_ITEMS | {"spear", "halberd"}


def stand_in(icon, key, ratio):
    """The current icon cropped to a box of this width:height ratio, as wide as the icon allows:
    the right end (the business end) of a long item, the middle of a compact one. A stand-in for
    an icon that would be drawn for the cell's shape."""
    bbox = icon.getchannel("A").point(lambda v: 255 if v > 8 else 0).getbbox()
    left, top, right, bottom = bbox
    ih = bottom - top
    cw = min(right - left, int(ih * ratio))
    if cw < ih * ratio:
        # a tall box: widen it to the ratio on the canvas (transparent room above and below)
        pass
    ch = int(cw / ratio)
    cy = (top + bottom) / 2
    y0 = int(max(0, cy - ch / 2))
    if y0 + ch > icon.height:
        y0 = icon.height - ch
    x0 = right - cw if key in END_FIRST else int((left + right) / 2 - cw / 2)
    return icon.crop((x0, y0, x0 + cw, y0 + ch))


LEATHER = (0x5C, 0x3F, 0x2C)
LEATHER_DARK = (0x2A, 0x1B, 0x12)
LEATHER_SEAM = (0x8E, 0x6A, 0x47)
BAG_PAD_X = 8
BAG_PAD_Y = 4


def draw_bag(c, x, y, w, h):
    """A leather bag behind a unit's cells: a rounded slab with a dark rim and a seam inside it,
    a stand-in for the bag art (Backpack Battles' bags are drawn this way: a leather shape under the cells)."""
    S = m.S
    d = c.draw
    d.rounded_rectangle([S(x), S(y), S(x + w), S(y + h)], radius=S(12), fill=LEATHER + (255,), outline=LEATHER_DARK + (255,), width=S(3))
    d.rounded_rectangle([S(x + 4), S(y + 4), S(x + w - 4), S(y + h - 4)], radius=S(9), outline=LEATHER_SEAM + (200,), width=S(1.5))


def draw_face(c, g, u, x, y, target=False):
    name = "plate_target" if target else ("plate_party" if u["side"] == "party" else "plate_enemy")
    c.frame(name, x, y, g.face, g.face, border_scale=0.5)
    face = face_of(u["side"], u["key"]).resize((m.S(g.face - 2 * g.face_inset),) * 2, Image.LANCZOS)
    c.paste(face, x + g.face_inset, y + g.face_inset)
    c.badge(x + 2, y + g.face - g.badge - 2, g.badge, u["row"], 14 if g.badge >= 22 else 12)


def line_party(c, g, u, y, grades=False, selected=None, empty_label=None, bag=False):
    """A party unit's line from the left edge: the face, then the board to the right."""
    face_y = y + (g.line_h - g.face) / 2
    draw_face(c, g, u, g.margin, face_y)
    x = g.margin + g.face + g.face_gap
    cy = y + (g.line_h - g.cell_h) / 2
    if bag:
        draw_bag(c, x - BAG_PAD_X, cy - g.bag_pad_y, g.board_w(u["slots"]) + 2 * BAG_PAD_X, g.cell_h + 2 * g.bag_pad_y)
    cell = 0
    for item in u["items"]:
        n = item["size"]
        draw_cell(c, g, x, cy, n, item["id"], item.get("charge", 0.0), grade=item.get("grade") if grades else None,
                  selected=selected == (u["key"], cell))
        x += g.board_w(n) + g.cell_gap
        cell += n
    for _ in range(u["slots"] - cell):
        draw_cell(c, g, x, cy, 1, empty_label=empty_label)
        x += g.cell_w + g.cell_gap
    return x - g.cell_gap


def line_enemy(c, g, u, y, bag=False):
    """An enemy's line from the right edge: the face, then the board to the left, icons mirrored."""
    face_y = y + (g.line_h - g.face) / 2
    draw_face(c, g, u, 1920 - g.margin - g.face, face_y)
    x = 1920 - g.margin - g.face - g.face_gap
    cy = y + (g.line_h - g.cell_h) / 2
    if bag:
        draw_bag(c, x - g.board_w(u["slots"]) - BAG_PAD_X, cy - g.bag_pad_y, g.board_w(u["slots"]) + 2 * BAG_PAD_X, g.cell_h + 2 * g.bag_pad_y)
    for item in u["items"]:
        n = item["size"]
        x -= g.board_w(n)
        draw_cell(c, g, x, cy, n, item["id"], item.get("charge", 0.0), mirrored=True)
        x -= g.cell_gap


def draw_panel(c, g):
    c.frame("panel", 0, g.panel_y, 1920, g.panel_h)
    inset = 28 if g.panel_h >= 300 else 22
    c.rect(SEAM_X, g.panel_y + inset, 2, g.panel_h - 2 * inset, m.LINE + (120,))


def battle(g, party_units, enemies, path, label_cells=None, bag=False):
    c = m.Canvas()
    m.scene_stage(c, party_units, enemies)
    draw_panel(c, g)
    by_p = {u["row"]: u for u in party_units}
    by_e = {u["row"]: u for u in enemies}
    for k in range(1, 5):
        y = g.line_y(k)
        if k in by_p:
            line_party(c, g, by_p[k], y, bag=bag)
        if k in by_e:
            line_enemy(c, g, by_e[k], y, bag=bag)
    m.draw_header_and_potions(c)
    if label_cells:
        c.text(SEAM_X, g.panel_y - 14, label_cells, 18, m.TEXT_DIM, "mm")
    return c.finish(path)


def node_map(g, members, path, bag=False):
    """The node map: the party's lines of this geometry in the panel's left half, the node's words and buttons on the right."""
    c = m.Canvas()
    party.map_header(c)
    party.map_panel(c, 980, 110, 920, g.panel_y - 110 - 16)
    party.potions_and_hint(c)
    party.party_stage(c, members, party.BATTLE_TOP, moves_y=party.BATTLE_TOP + 404)
    draw_panel(c, g)
    by_row = {u["row"]: u for u in members}
    for k in range(1, 5):
        if k in by_row:
            line_party(c, g, by_row[k], g.line_y(k), grades=True, selected=party.SELECTED, empty_label="빈 칸", bag=bag)
    y0 = g.panel_y
    c.text(1000, y0 + 44, "2층 · 전투", 34, m.TEXT)
    c.text(1000, y0 + 90, "이 노드의 적은 들어가기 전에는 보이지 않습니다", 20, m.TEXT_DIM)
    c.text(1000, y0 + 140, "장궁 등급 11 — 무기 · 크기 2 · 쿨다운 3.0초 · 뒤에서 3번째까지", 19, m.TEXT)
    c.text(1000, y0 + 166, "뒤에서 2번째까지의 적에게 85% 피해", 19, m.TEXT)
    by = y0 + g.panel_h - 100
    party.kit_button(c, 1000, by, 200, 76, "인벤토리로", 26)
    party.kit_button(c, 1220, by, 236, 76, "인벤토리 보기", 28)
    party.kit_button(c, 1644, by, 236, 76, "전투 시작", 34, party.BUTTON)
    return c.finish(path)


def main():
    party_units = [
        {"side": "party", "row": 1, "key": "knight", "name": "로언", "hp": 120, "maxhp": 140, "shield": 12, "slots": 6,
         "items": [{"id": "longsword", "size": 1, "charge": 0.75, "grade": 10}, {"id": "halberd", "size": 3, "charge": 0.3, "grade": 12},
                   {"id": "buckler", "size": 1, "charge": 0.5, "grade": 9}]},
        {"side": "party", "row": 2, "key": "spellblade", "name": "카이", "hp": 100, "maxhp": 100, "slots": 6,
         "items": [{"id": "sword", "size": 1, "charge": 0.55, "grade": 12}, {"id": "spear", "size": 2, "charge": 0.45, "grade": 11},
                   {"id": "dagger", "size": 1, "charge": 0.2, "grade": 8}]},
        {"side": "party", "row": 3, "key": "bishop", "name": "엘라", "hp": 90, "maxhp": 90, "slots": 6,
         "items": [{"id": "healing_staff", "size": 1, "charge": 0.5, "grade": 10}, {"id": "herb_pouch", "size": 1, "charge": 0.35, "grade": 9},
                   {"id": "ward_charm", "size": 1, "charge": 0.2, "grade": 8}]},
        {"side": "party", "row": 4, "key": "archmage", "name": "미라", "hp": 80, "maxhp": 80, "slots": 6,
         "items": [{"id": "fire_staff", "size": 1, "charge": 0.6, "grade": 10}, {"id": "ember_flask", "size": 1, "charge": 0.25, "grade": 9},
                   {"id": "longbow", "size": 2, "charge": 0.6, "grade": 11}]},
    ]
    enemies = [
        {"side": "enemy", "row": 1, "key": "goblin_raider", "name": "고블린 약탈자", "hp": 41, "maxhp": 80, "burn": 3, "slots": 1,
         "items": [{"id": "rusty_blade", "size": 1, "charge": 0.5}]},
        {"side": "enemy", "row": 2, "key": "goblin_raider", "name": "고블린 약탈자", "hp": 80, "maxhp": 80, "slots": 1,
         "items": [{"id": "rusty_blade", "size": 1, "charge": 0.3}]},
        {"side": "enemy", "row": 3, "key": "goblin_archer", "name": "고블린 궁수", "hp": 55, "maxhp": 55, "slots": 1,
         "items": [{"id": "crude_bow", "size": 1, "charge": 0.65}]},
        {"side": "enemy", "row": 4, "key": "goblin_shaman", "name": "고블린 주술사", "hp": 60, "maxhp": 60, "slots": 2,
         "items": [{"id": "hex_spit", "size": 1, "charge": 0.4}, {"id": "mending_chant", "size": 1, "charge": 0.2}]},
    ]
    # the current game: boards of four cells, so the knight carries only the longsword and the halberd
    four = [dict(u, slots=4, items=[i for i in u["items"] if not (u["key"] == "knight" and i["id"] == "buckler")]) for u in party_units]
    members = [dict(u, job=j) for u, j in zip(party_units, ("기사", "마검사", "주교", "대마법사"))]
    OUT.mkdir(parents=True, exist_ok=True)

    def plan(g, cells):
        return f"줄의 끝 x={g.line_end(cells):.0f} (이음선 {SEAM_X})"

    images = [
        (battle(NOW, four, enemies, OUT / "six-now.png"), f"지금: 칸 180x60, 4칸. {plan(NOW, 4)}"),
        (battle(NOW, party_units, enemies, OUT / "six-overflow.png"), f"지금 크기로 6칸: 줄이 이음선을 넘어 적의 줄과 겹친다. {plan(NOW, 6)}"),
        (battle(A, party_units, enemies, OUT / "six-A.png"), f"A안 (권장): 칸 138x46 (아이콘의 3:1 그대로), 얼굴 60, 줄 높이 60, 패널 324와 무대 그대로. {plan(A, 6)}"),
        (battle(B, party_units, enemies, OUT / "six-B.png"), f"B안: 줄 전체를 같은 비율로 (얼굴 46), 패널을 248로 낮춤 (위에서 832), 무대 그대로. {plan(B, 6)}"),
        (battle(C, party_units, enemies, OUT / "six-C.png"), f"C안: 폭만 138로 줄임 (칸 138x60), 아이콘은 가운데 띠. 패널 그대로. {plan(C, 6)}"),
    ]
    which = sys.argv[1] if len(sys.argv) > 1 else "eight"
    if which == "six":
        m.composite([im for im, _ in images], [label for _, label in images], OUT / "six-compare.png")
        node_map(A, members, OUT / "six-A-map.png")
        return

    if which == "square":
        grown = [dict(u, slots=n) for u, n in zip(party_units, (5, 8, 6, 5))]
        grown_members = [dict(u, slots=n) for u, n in zip(members, (5, 8, 6, 5))]
        images = [
            (battle(E, grown, enemies, OUT / "square-now.png", bag=True),
             f"지금 (E안 구현): 칸 100x72, 패널 372 (위에서 708). 줄의 끝 x={E.line_end(8):.0f}"),
            (battle(G, grown, enemies, OUT / "square-G.png", bag=True),
             f"G안: 정사각 칸 72x72, 얼굴 72, 패널 358 (-14, 위에서 722). 8칸 줄의 끝 x={G.line_end(8):.0f}"),
            (battle(J, grown, enemies, OUT / "square-J.png", bag=True),
             f"J안: 정사각 칸 68x68, 얼굴 68, 패널 342 (-30, 위에서 738). 8칸 줄의 끝 x={J.line_end(8):.0f}"),
            (battle(H, grown, enemies, OUT / "square-H.png", bag=True),
             f"H안: 정사각 칸 64x64, 얼굴 64, 패널 326 (-46, 위에서 754). 8칸 줄의 끝 x={H.line_end(8):.0f}"),
        ]
        m.composite([im for im, _ in images], [label for _, label in images], OUT / "square-compare.png")
        node_map(J, grown_members, OUT / "square-J-map.png", bag=True)
        return

    if which == "bag":
        # boards of 5, 8, 6 and 5 cells: a party that started at five and has grown unevenly
        grown = [dict(u, slots=n) for u, n in zip(party_units, (5, 8, 6, 5))]
        grown_members = [dict(u, slots=n) for u, n in zip(members, (5, 8, 6, 5))]
        images = [
            (battle(E, grown, enemies, OUT / "bag-E-five.png"),
             "E안, 칸은 유닛의 지금 칸 수만큼만 (위에서 5, 8, 6, 5칸): 가방 없이 칸만"),
            (battle(E, grown, enemies, OUT / "bag-E-bag.png", bag=True),
             "E안 + 가방: 유닛의 칸 뒤에 칸 수만큼 긴 가죽 가방 (대역 그림). 칸이 늘면 가방이 길어진다"),
            (node_map(E, grown_members, OUT / "bag-E-bag-map.png", bag=True),
             "노드 맵 · E안 + 가방"),
        ]
        m.composite([im for im, _ in images], [label for _, label in images], OUT / "bag-compare.png")
        return

    eight = [dict(u, slots=8) for u in party_units]
    images = [
        (battle(NOW, four, enemies, OUT / "eight-now.png"), f"지금: 칸 180x60, 4칸. {plan(NOW, 4)}"),
        (battle(D_ASIS, eight, enemies, OUT / "eight-D-asis.png"),
         f"8칸, 칸 100x60, 지금의 띠 아이콘 그대로: 폭에 맞춰 줄어 84x23의 가는 조각이 된다. {plan(D_ASIS, 8)}"),
        (battle(D, eight, enemies, OUT / "eight-D.png"),
         f"D안: 칸 100x60 (1.7:1), 얼굴 60, 패널 324와 무대 그대로. 아이콘은 칸 모양으로 다시 그린다 (여기서는 지금 아이콘을 잘라 넣은 대역). {plan(D, 8)}"),
        (battle(E, eight, enemies, OUT / "eight-E.png"),
         f"E안 (권장): 칸 100x72 (1.4:1), 얼굴 72, 패널 372 (위에서 708). 명패 아래 여백 48. 아이콘은 다시 그린다 (대역). {plan(E, 8)}"),
        (battle(F, eight, enemies, OUT / "eight-F.png"),
         f"F안: 칸 100x84 (1.2:1), 얼굴 84, 패널 402 (위에서 678). 명패 아래 여백 18. 아이콘은 다시 그린다 (대역). {plan(F, 8)}"),
    ]
    m.composite([im for im, _ in images], [label for _, label in images], OUT / "eight-compare.png")
    node_map(E, [dict(u, slots=8) for u in members], OUT / "eight-E-map.png")


if __name__ == "__main__":
    main()
