#!/usr/bin/env python3
"""Mockups of the item cells after two asks of 2026-10-03 (evening), both about The Bazaar.

Ask 1: "아이템을 바자르 같은 느낌으로. 쿨타임을 가로에서 세로로(위→아래나 아래→위). 칸의 크기가 적절한지, 세로로 더 늘리는 방향도 검토. 목업."
Ask 2: "바자르는 가로로 배열. 우리는 세로 버전으로: UI에서도 세로 버전(열 순서대로 배열)으로. 네가 더 나은 버전이 있으면 그걸로 목업."

Reuses the renderers of round 07 (the stage, the kit) and round 08 (the lines of the panel, the geometry). No API call.

  now : the game as it is (G: square cells 72, the fill from the left, the strip icons shrunk to the cell)
  V   : ask 2 as asked - a unit's board is a vertical column right under its stage column (so the columns stand in row
        order like the figures), cells 180x50 so that eight fit, no faces (the plate is right above), the fill from the
        bottom. The clock shrinks to 160 to fit between the two 1열 columns; the captions move to the header.
  B   : my version - the lines stay (a Bazaar shelf per row, the four shelves stacked in row order), but a cell is a
        standing card 72x90 with the fill rising from the bottom. The stage moves up 16.
  A   : the cheapest - the square as it is, only the fill from the bottom (detail sheet only)

Sheets:
  compare : now / V / B at 1280 wide
  detail  : 1:1 crops of one side of the panel: now, V (fill from the bottom), V (fill toward the enemy), A, B,
            B with the art filling the cell and the firing cell glowing in the color of its effect
  maps    : V and B on the node map
Full screens: V, B.

Stand-in icons (V uses the icons as they are: they were drawn for 180-wide cells, and the 2- and 3-cell icons for a
vertical stack): a long item is rotated to stand in a portrait or square cell, a compact one is cropped to the cell's
shape. They only show the size a redrawn icon would have.
"""
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent / "07-battle-panel"))
sys.path.insert(0, str(HERE.parent / "08-panel-cells"))
from PIL import Image, ImageDraw, ImageFont  # noqa: E402

import mock_bottom_panel as m  # noqa: E402
import mock_party_panel as party  # noqa: E402
import mock_panel_cells as cells  # noqa: E402

OUT = m.OUT
S = m.S
m.BORDER["bag"] = 40

STAGE_TOP = 262                                        # BattleFieldTop
PLATE_BOTTOM = STAGE_TOP + m.FIGURE_H + 6 + m.PLATE_H  # 660
PARTY_COLUMN_H = m.FIGURE_H + 6 + m.PLATE_H + 6 + 40   # the party side: figure, plate, move buttons = 444
BIG = {"longbow", "spear", "halberd"}
CAPTIONS = ["적 1열이 비어 뒤에서 전진", "미라의 화염 지팡이 → 동굴 쥐: 피해 13", "미라의 화염 지팡이 → 동굴 쥐: 피해 13"]
# the color of the glow of a firing cell, by the item's effect (my suggestion: the floating numbers already use these)
FLASH = {"longsword": m.DANGER, "healing_staff": m.GOOD, "ward_charm": m.SHIELD, "ember_flask": m.BURN}


class Geo(cells.Geometry):
    """Round 08's geometry of the lines, plus the fill direction and how far the stage moves up."""

    def __init__(self, *args, vertical=False, stage_up=0, flash=False, **kw):
        super().__init__(*args, **kw)
        self.vertical = vertical
        self.stage_up = stage_up
        self.flash = flash


# (cell_w, cell_h, cell_gap, face, face_inset, face_gap, margin, icon_mx, icon_my, badge, line_gap, panel_h, ...)
NOW = Geo(72, 72, 4, 72, 6, 8, 24, 6, 6, 24, 10, 358, stand_in=False, pad=20, bag_pad_y=3)
A = Geo(72, 72, 4, 72, 6, 8, 24, 6, 6, 24, 10, 358, stand_in=True, pad=20, bag_pad_y=3, vertical=True)
B = Geo(72, 90, 4, 72, 6, 8, 24, 6, 6, 24, 8, 416, stand_in=True, pad=16, bag_pad_y=3, vertical=True, stage_up=16)
B_FULL = Geo(72, 90, 4, 72, 6, 8, 24, 2, 2, 24, 8, 416, stand_in=True, pad=16, bag_pad_y=3, vertical=True, stage_up=16, flash=True)


class ColumnGeo:
    """V: a vertical column of cells under each stage column. Eight cells must fit, so the cell is a strip."""

    def __init__(self, cell_w=180, cell_h=50, gap=4, pad=16, stage_up=56, icon_mx=8, icon_my=5, toward_enemy=False):
        self.cell_w, self.cell_h, self.gap, self.pad = cell_w, cell_h, gap, pad
        self.stage_up = stage_up
        self.icon_mx, self.icon_my = icon_mx, icon_my
        self.toward_enemy = toward_enemy  # the fill runs along the strip toward the enemy instead of rising
        self.panel_h = 2 * pad + 8 * cell_h + 7 * gap
        self.panel_y = 1080 - self.panel_h

    def board_h(self, n):
        return n * self.cell_h + (n - 1) * self.gap


V = ColumnGeo()
V_TOWARD = ColumnGeo(toward_enemy=True)


# ------------------------------------------------------------------ pieces

def stand_in(icon, key, box_w, box_h):
    """A stand-in for an icon drawn for this box: a long item rotated to stand (or lean) in it, a 2- or 3-cell
    icon laid flatter in a landscape box, a compact item cropped to the box's shape (round 08)."""
    ratio = box_w / box_h
    if key in BIG:
        im = icon.rotate(-22, expand=True, resample=Image.BICUBIC) if ratio > 1.3 else icon
    elif key in m.LONG_ITEMS:
        angle = 72 if ratio < 0.95 else (45 if ratio < 1.3 else 20)
        im = icon.rotate(angle, expand=True, resample=Image.BICUBIC)
    else:
        return cells.stand_in(icon, key, ratio)
    bbox = im.getchannel("A").point(lambda v: 255 if v > 8 else 0).getbbox()
    return im.crop(bbox)


def place_icon(c, icon, x, y, box_w, box_h, scale=1.0):
    r = min(S(box_w) / icon.width, S(box_h) / icon.height) * scale
    im = icon.resize((max(1, int(icon.width * r)), max(1, int(icon.height * r))), Image.LANCZOS)
    c.im.alpha_composite(im, (S(x) + (S(box_w) - im.width) // 2, S(y) + (S(box_h) - im.height) // 2))


def draw_fill(c, x, y, w, h, charge, vertical, toward_right=True, rim=7):
    """The cooldown inside the rim: rising from the bottom, or running sideways (toward the enemy)."""
    if charge <= 0:
        return
    color = m.GAUGE + (int(255 * 0.84),)
    iw, ih = w - 2 * rim, h - 2 * rim
    if vertical:
        fh = ih * charge
        if fh >= 1:
            c.rect(x + rim, y + rim + ih - fh, iw, fh, color)
    else:
        fw = iw * charge
        if fw >= 1:
            c.rect(x + rim if toward_right else x + rim + iw - fw, y + rim, fw, ih, color)


def draw_bag(c, x, y, w, h):
    c.frame("bag", x, y, w, h)


def draw_cell(c, g, x, y, n, item=None, charge=0.0, mirrored=False, grade=None, selected=False, dim=False, empty_label=None):
    """A cell of a line (round 08's signature, so that its line functions can call this one)."""
    w, h = g.board_w(n), g.cell_h
    if item is None:
        c.frame("slot", x, y, w, h, alpha=0.45)
        if empty_label:
            c.text(x + w / 2, y + h / 2, empty_label, 15 if h < 50 else 19, m.TEXT_DIM, "mm")
        return
    c.frame("slot_selected" if selected else "slot", x, y, w, h, alpha=0.55 if dim else 1.0)
    flash = FLASH.get(item) if g.flash else None
    draw_fill(c, x, y, w, h, 1.0 if flash else charge, g.vertical, toward_right=not mirrored)
    icon = m.item_icon(item)
    box_w, box_h = w - 2 * g.icon_mx, h - 2 * g.icon_my
    if g.stand_in:
        icon = stand_in(icon, item, box_w, box_h)
    if mirrored:
        icon = icon.transpose(Image.FLIP_LEFT_RIGHT)
    place_icon(c, icon, x + g.icon_mx, y + g.icon_my, box_w, box_h, 1.18 if flash else 1.0)
    if flash:
        c.rect(x + 6, y + 6, w - 12, h - 12, flash + (105,))
    if grade is not None:
        size = 24 if h >= 50 else 20
        c.badge(x + 5, y + h - 5 - size, size, grade, 14 if h >= 50 else 12)


cells.draw_cell = draw_cell
cells.draw_bag = draw_bag


def draw_column_cell(c, g, x, y, n, item=None, charge=0.0, mirrored=False, grade=None, selected=False, empty_label=None):
    """A cell of a vertical column (V): the icons as they are, since they were drawn for 180-wide cells."""
    w, h = g.cell_w, g.board_h(n)
    if item is None:
        c.frame("slot", x, y, w, h, alpha=0.45)
        if empty_label:
            c.text(x + w / 2, y + h / 2, empty_label, 17, m.TEXT_DIM, "mm")
        return
    c.frame("slot_selected" if selected else "slot", x, y, w, h)
    draw_fill(c, x, y, w, h, charge, vertical=not g.toward_enemy, toward_right=not mirrored)
    icon = m.item_icon(item)
    if mirrored:
        icon = icon.transpose(Image.FLIP_LEFT_RIGHT)
    place_icon(c, icon, x + g.icon_mx, y + g.icon_my, w - 2 * g.icon_mx, h - 2 * g.icon_my)
    if grade is not None:
        c.badge(x + 5, y + h - 5 - 22, 22, grade, 13)


def column(c, g, u, x, top, mirrored=False, grades=False, selected=None, empty_label=None):
    """A unit's board as a column from the top down: the items in order, then the empty cells, a bag behind."""
    draw_bag(c, x - 8, top - 3, g.cell_w + 16, g.board_h(u["slots"]) + 6)
    y = top
    cell = 0
    for item in u["items"]:
        n = item["size"]
        draw_column_cell(c, g, x, y, n, item["id"], item.get("charge", 0.0), mirrored,
                         grade=item.get("grade") if grades else None, selected=selected == (u["key"], cell))
        y += g.board_h(n) + g.gap
        cell += n
    for _ in range(u["slots"] - cell):
        draw_column_cell(c, g, x, y, 1, empty_label=empty_label)
        y += g.cell_h + g.gap


def draw_header(c, captions_in_header=False):
    """The battle header, the potion strip and the hint, as the game draws them now (round 09)."""
    c.frame("panel", 0, 0, 1920, 84)
    c.frame("button", 40, 16, 300, 52, tint=m.DANGER)
    c.text(190, 42, "후퇴 (60%)", 28, m.TEXT, "mm")
    if captions_in_header:
        c.text(372, 42, "버려진 광산 · 1층", 26, m.TEXT)
        for i, s in enumerate(CAPTIONS):
            c.text(960, 22 + 20 * i, s, 16, m.TEXT if i == 2 else m.TEXT_DIM, "mm")
    else:
        c.text(960, 42, "버려진 광산 · 1층", 30, m.TEXT, "mm")
    c.frame("button", 1530, 16, 110, 52, tint=m.SELECTED)
    c.text(1585, 42, "정지", 24, m.TEXT, "mm")
    for i, label in enumerate(("x1", "x2", "x4")):
        c.frame("button", 1650 + 90 * i, 16, 80, 52, tint=m.BUTTON_QUIET)
        c.text(1690 + 90 * i, 42, label, 24, m.TEXT, "mm")
    party.potions_and_hint(c)
    hint = "치유 포션: 쓸 아군을 누르세요"
    w = c.text_width(hint, 20) + 44
    c.frame("plate_label", 656, 110, w, 48, border_scale=0.6)
    c.text(656 + 22, 134, hint, 20, m.TEXT_DIM)


def draw_potions_right(c):
    """The potion strip moved above the map (V's node map: the party must stand higher)."""
    c.frame("panel", 980, 92, 610, 84, border_scale=0.75)
    for i in range(3):
        x = 992 + 198 * i
        c.frame("slot", x, 102, 190, 64)
        if i < 2:
            c.text(x + 14, 122, "치유 포션", 21, m.TEXT)
            c.text(x + 14, 148, "HP 50 회복", 17, m.TEXT_DIM)
        else:
            c.text(x + 14, 134, "빈 칸", 21, m.TEXT_DIM)


def draw_clock(c, panel_y, size=200, top=18, with_captions=True):
    """The storm clock in the middle of the panel (round 09): the dial, the ring, the time, the storm line, the captions."""
    box = size / 0.84
    dial = m.sprite("dial").resize((S(box), S(box)), Image.LANCZOS)
    c.paste(dial, 960 - box / 2, panel_y + top - (box - size) / 2)
    cx, cy = 960, panel_y + top + size / 2
    r = size / 2 - 11
    c.draw.arc([S(cx - r), S(cy - r), S(cx + r), S(cy + r)], start=-90, end=-90 + 360 * 0.16,
               fill=m.BRASS + (255,), width=S(size * 0.05))
    c.text(cx, cy, "7.3초", int(size * 0.2), m.TEXT, "mm")
    ly = panel_y + top + size + 10
    c.icon("storm", cx - 78, ly, 24)
    c.text(cx - 48, ly + 12, "폭풍까지 37.7초", 20 if size >= 200 else 18, m.TEXT_DIM)
    if with_captions:
        for i, s in enumerate(CAPTIONS):
            c.text(960, ly + 48 + 26 * i, s, 18, m.TEXT if i == 2 else m.TEXT_DIM, "mm")


# ----------------------------------------------------------------- screens

def battle_lines(g, party_units, enemies, path):
    """The panel of lines (now, A, B): round 08's lines with this file's cells."""
    c = m.Canvas()
    m.FIELD_TOP = STAGE_TOP - g.stage_up
    m.scene_stage(c, party_units, enemies)
    m.FIELD_TOP = STAGE_TOP
    cells.draw_panel(c, g)
    by_p = {u["row"]: u for u in party_units}
    by_e = {u["row"]: u for u in enemies}
    for k in range(1, 5):
        y = g.line_y(k)
        if k in by_p:
            cells.line_party(c, g, by_p[k], y, bag=True)
        if k in by_e:
            cells.line_enemy(c, g, by_e[k], y, bag=True)
    draw_clock(c, g.panel_y)
    draw_header(c)
    return c.finish(path)


def battle_columns(g, party_units, enemies, path):
    """V: a column under each stage column, the clock in the gap between the two 1열 columns, the captions in the header."""
    c = m.Canvas()
    m.FIELD_TOP = STAGE_TOP - g.stage_up
    m.scene_stage(c, party_units, enemies)
    m.FIELD_TOP = STAGE_TOP
    c.frame("panel", 0, g.panel_y, 1920, g.panel_h)
    top = g.panel_y + g.pad
    for u in party_units:
        column(c, g, u, m.party_column_x(u["row"]), top)
    for u in enemies:
        column(c, g, u, m.enemy_column_x(u["row"]), top, mirrored=True)
    draw_clock(c, g.panel_y, size=160, top=16, with_captions=False)
    draw_header(c, captions_in_header=True)
    return c.finish(path)


def map_right(c, g_panel_y, g_panel_h):
    c.text(1000, g_panel_y + 44, "2층 · 전투", 34, m.TEXT)
    c.text(1000, g_panel_y + 90, "이 노드의 적은 들어가기 전에는 보이지 않습니다", 20, m.TEXT_DIM)
    c.text(1000, g_panel_y + 140, "장궁 등급 11 — 무기 · 크기 2 · 쿨다운 3.0초 · 뒤에서 3번째까지", 19, m.TEXT)
    c.text(1000, g_panel_y + 166, "뒤에서 2번째까지의 적에게 85% 피해", 19, m.TEXT)
    by = g_panel_y + g_panel_h - 100
    party.kit_button(c, 1000, by, 200, 76, "인벤토리로", 26)
    party.kit_button(c, 1220, by, 236, 76, "인벤토리 보기", 28)
    party.kit_button(c, 1644, by, 236, 76, "전투 시작", 34, party.BUTTON)


def node_map_lines(g, members, path, party_top):
    """B on the node map: the party stands higher so that its move buttons clear the taller panel."""
    c = m.Canvas()
    party.map_header(c)
    party.map_panel(c, 980, 110, 920, g.panel_y - 110 - 16)
    party.potions_and_hint(c)
    party.party_stage(c, members, party_top, moves_y=party_top + m.FIGURE_H + 6 + m.PLATE_H + 6)
    cells.draw_panel(c, g)
    by_row = {u["row"]: u for u in members}
    for k in range(1, 5):
        if k in by_row:
            cells.line_party(c, g, by_row[k], g.line_y(k), grades=True, selected=party.SELECTED, empty_label="빈 칸", bag=True)
    map_right(c, g.panel_y, g.panel_h)
    return c.finish(path)


def node_map_columns(g, members, path):
    """V on the node map: the columns under the party, the potions moved above the map so that the party can stand higher."""
    c = m.Canvas()
    party.map_header(c)
    draw_potions_right(c)
    party.map_panel(c, 980, 190, 920, g.panel_y - 190 - 16)
    party_top = g.panel_y - 12 - PARTY_COLUMN_H
    party.party_stage(c, members, party_top, moves_y=party_top + m.FIGURE_H + 6 + m.PLATE_H + 6)
    c.frame("panel", 0, g.panel_y, 1920, g.panel_h)
    for u in members:
        column(c, g, u, m.party_column_x(u["row"]), g.panel_y + g.pad, grades=True, selected=party.SELECTED, empty_label="빈 칸")
    map_right(c, g.panel_y, g.panel_h)
    return c.finish(path)


def detail_sheet(entries, path, width=960, band=44):
    """1:1 crops stacked with a label each: the panel of one side at the size the player sees."""
    total = sum(y1 - y0 + band for _, (x0, y0, x1, y1), _ in entries)
    sheet = Image.new("RGB", (width, total), (24, 26, 32))
    d = ImageDraw.Draw(sheet)
    f = ImageFont.truetype(str(m.FONT), 20)
    y = 0
    for im, (x0, y0, x1, y1), label in entries:
        d.text((16, y + band / 2), label, font=f, fill=m.TEXT, anchor="lm")
        crop = im.crop((x0, y0, x1, y1))
        sheet.paste(crop, ((width - crop.width) // 2, y + band))
        y += crop.height + band
    sheet.save(path)
    print("wrote", path)


def main():
    party_units = [
        {"side": "party", "row": 1, "key": "knight", "name": "로언", "hp": 120, "maxhp": 140, "shield": 12, "slots": 5,
         "items": [{"id": "longsword", "size": 1, "charge": 0.75, "grade": 10}, {"id": "halberd", "size": 3, "charge": 0.3, "grade": 12},
                   {"id": "buckler", "size": 1, "charge": 0.5, "grade": 9}]},
        {"side": "party", "row": 2, "key": "spellblade", "name": "카이", "hp": 100, "maxhp": 100, "slots": 8,
         "items": [{"id": "sword", "size": 1, "charge": 0.55, "grade": 12}, {"id": "spear", "size": 2, "charge": 0.45, "grade": 11},
                   {"id": "dagger", "size": 1, "charge": 0.2, "grade": 8}]},
        {"side": "party", "row": 3, "key": "bishop", "name": "엘라", "hp": 90, "maxhp": 90, "slots": 6,
         "items": [{"id": "healing_staff", "size": 1, "charge": 0.5, "grade": 10}, {"id": "herb_pouch", "size": 1, "charge": 0.35, "grade": 9},
                   {"id": "ward_charm", "size": 1, "charge": 0.2, "grade": 8}]},
        {"side": "party", "row": 4, "key": "archmage", "name": "미라", "hp": 80, "maxhp": 80, "slots": 5,
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
    members = [dict(u, job=j) for u, j in zip(party_units, ("기사", "마검사", "주교", "대마법사"))]
    OUT.mkdir(parents=True, exist_ok=True)

    now = battle_lines(NOW, party_units, enemies, OUT / "bazaar-now.png")
    v = battle_columns(V, party_units, enemies, OUT / "bazaar-V.png")
    v_toward = battle_columns(V_TOWARD, party_units, enemies, OUT / "bazaar-V-toward.png")
    a = battle_lines(A, party_units, enemies, OUT / "bazaar-A.png")
    b = battle_lines(B, party_units, enemies, OUT / "bazaar-B.png")
    b_full = battle_lines(B_FULL, party_units, enemies, OUT / "bazaar-B-full.png")

    m.composite(
        [now, v, b],
        [f"지금 (G안): 열마다 가로 줄, 정사각 칸 72, 왼쪽부터 차는 쿨다운, 패널 358 (위에서 722). 띠 아이콘이 칸의 폭에 맞춰 줄어 가운데 띠로 보인다",
         f"V안 (사용자 안: 바자르의 가로 배열을 세로로): 유닛의 세로 열을 무대의 열 바로 아래에 (열 순서 그대로), 칸 180x50 띠 8개, 얼굴 없음, 아래에서 차는 쿨다운. "
         f"패널 {V.panel_h} (위에서 {V.panel_y}), 무대 {V.stage_up} 위로, 시계 160, 자막은 헤더로",
         f"B안 (권장): 열마다 가로 줄은 그대로 (바자르의 선반 넷을 열 순서로 쌓은 것), 칸을 세운 카드 72x90, 아래에서 차는 쿨다운. "
         f"패널 {B.panel_h} (위에서 {B.panel_y}), 무대 {B.stage_up} 위로. 아이콘은 세운 모양으로 다시 그린다 (대역)"],
        OUT / "bazaar-compare.png")

    def lines_crop(g):
        return (0, g.panel_y - 70, 960, 1080)

    def columns_crop(g):
        return (100, g.panel_y - 70, 900, 1080)

    detail_sheet(
        [(now, lines_crop(NOW), "지금: 정사각 72, 왼쪽부터 차는 쿨다운 (아이콘은 지금의 띠)"),
         (v, columns_crop(V), f"V안: 무대 열 아래의 세로 열, 칸 180x50 (8칸이 {V.panel_h - 2 * V.pad}), 아래에서 차는 쿨다운 (아이콘은 지금 것 그대로)"),
         (v_toward, columns_crop(V_TOWARD), "V안의 변형: 쿨다운이 띠를 따라 적 쪽으로 (아군은 오른쪽으로, 적은 왼쪽으로)"),
         (a, lines_crop(A), "A안: 정사각 72 그대로, 쿨다운만 아래에서 위로 (아이콘은 정사각으로 다시 그린 대역)"),
         (b, lines_crop(B), "B안 (권장): 세운 카드 72x90, 아래에서 차는 쿨다운, 패널 416 (아이콘은 세운 모양의 대역)"),
         (b_full, lines_crop(B_FULL), "B안 + 제안: 아이콘이 칸을 채우고 (여백 6 -> 2), 발동한 칸은 효과의 색으로 빛나며 아이콘이 커진다 (피해 빨강, 회복 초록, 보호막 파랑, 화상 주황)")],
        OUT / "bazaar-detail.png")

    node_map_columns(V, members, OUT / "bazaar-V-map.png")
    node_map_lines(B, members, OUT / "bazaar-B-map.png", party_top=B.panel_y - 12 - PARTY_COLUMN_H)


if __name__ == "__main__":
    main()
