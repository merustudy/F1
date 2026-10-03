#!/usr/bin/env python3
"""Mockups of the node map and the reward screen with the party's boards in the battle's bottom panel.

Reuses the battle mockup's renderer (mock_bottom_panel.py). The right side of these screens is
the game's shape UI (plain panels and buttons), drawn as plain rectangles here too. No API call.

  now-map / now-reward : the current layout (vertical boards under the plates, popup inventory)
  A-map / A-reward     : the battle's panel; its right half holds the screen's own controls
  B-map                : the panel's right half holds the inventory as cells (no popup)
"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from PIL import Image, ImageDraw, ImageFont  # noqa: E402

import mock_bottom_panel as m  # noqa: E402

PANEL = (0x1E, 0x22, 0x2B)
PANEL_LIGHT = (0x2A, 0x30, 0x3C)
BUTTON = (0x3B, 0x6F, 0xB5)
SLOT = (0x15, 0x18, 0x1F)

PARTY_TOP_NOW = 186        # PartyTop in UiPrefabSetup.PartySide
BATTLE_TOP = 262           # the stage in battle (and in the new variants)


def plain(c, x, y, w, h, fill):
    c.rect(x, y, w, h, fill + (255,))


def plain_button(c, x, y, w, h, label, size, fill=m.BUTTON_QUIET, enabled=True):
    plain(c, x, y, w, h, fill if enabled else (0x2B, 0x30, 0x3A))
    c.text(x + w / 2, y + h / 2, label, size, m.TEXT if enabled else m.TEXT_DIM, "mm")


def kit_button(c, x, y, w, h, label, size, tint=m.BUTTON_QUIET):
    c.frame("button", x, y, w, h, tint=tint)
    c.text(x + w / 2, y + h / 2, label, size, m.TEXT, "mm")


# ------------------------------------------------------------------ party side

def board_cell(c, x, y, w, h, item=None, grade=None, selected=False, dim=False):
    """A cell of a member's board between battles: the slot (brass when chosen), the icon, the grade badge."""
    if item is None:
        c.frame("slot", x, y, w, h, alpha=0.45)
        c.text(x + w / 2, y + h / 2, "빈 칸", 19, m.TEXT_DIM, "mm")
        return
    c.frame("slot_selected" if selected else "slot", x, y, w, h, alpha=0.55 if dim else 1.0)
    icon = m.item_icon(item)
    box_w, box_h = w - 20, h - 16
    ratio = min(m.S(box_w) / icon.width, m.S(box_h) / icon.height)
    icon = icon.resize((max(1, int(icon.width * ratio)), max(1, int(icon.height * ratio))), Image.LANCZOS)
    c.im.alpha_composite(icon, (m.S(x + 10) + (m.S(box_w) - icon.width) // 2, m.S(y + 8) + (m.S(box_h) - icon.height) // 2))
    c.badge(x + 5, y + h - 5 - 24, 24, grade, 14)


def party_stage(c, members, top, moves_y=None):
    """The party's figures and plates at the battle's places; the move buttons under the plates when moves_y is given."""
    for u in sorted(members, key=lambda u: -u["row"]):
        im = m.figure_image("party", u["key"]).resize((m.S(m.FIGURE_W), m.S(m.FIGURE_H)), Image.LANCZOS)
        cx = m.party_column_x(u["row"]) + m.COLUMN_W / 2
        c.paste(im, cx - m.FIGURE_W / 2, top)
    for u in members:
        x = m.party_column_x(u["row"])
        plate_y = top + m.FIGURE_H + 6
        c.frame("plate_party", x, plate_y, m.COLUMN_W, m.PLATE_H)
        c.badge(x + 11, plate_y + 10, 26, u["row"], 17)
        c.text(x + 45, plate_y + 23, u["name"], 20)
        c.frame("slot", x + 11, plate_y + 39, m.COLUMN_W - 22, 23, border_scale=0.4)
        inner_w = m.COLUMN_W - 22 - 8
        c.rect(x + 15, plate_y + 43, inner_w * u["hp"] / u["maxhp"], 15, m.GOOD + (255,))
        c.text(x + m.COLUMN_W / 2, plate_y + 50.5, f"{u['hp']}/{u['maxhp']}", 15, m.TEXT, "mm")
        c.text(x + 13, plate_y + 74, u["job"], 15, m.TEXT_DIM)
        if moves_y is not None:
            half = m.COLUMN_W / 2
            kit_button(c, x, moves_y, half - 2, 40, "앞으로", 18, m.BUTTON_QUIET if u["row"] > 1 else (0x2B, 0x30, 0x3A))
            kit_button(c, x + half + 2, moves_y, half - 2, 40, "뒤로", 18, m.BUTTON_QUIET if u["row"] < 4 else (0x2B, 0x30, 0x3A))


def vertical_boards(c, members, top, selected):
    for u in members:
        x = m.party_column_x(u["row"])
        y = top
        cell = 0
        for item in u["items"]:
            n = item["size"]
            h = m.CELL_H * n + m.CELL_GAP * (n - 1)
            board_cell(c, x, y, m.COLUMN_W, h, item["id"], item["grade"], selected == (u["key"], cell))
            y += h + m.CELL_GAP
            cell += n
        for _ in range(u["slots"] - cell):
            board_cell(c, x, y, m.COLUMN_W, m.CELL_H)
            y += m.CELL_H + m.CELL_GAP


def panel_party_rows(c, members, panel_y, panel_h, selected):
    """The left half of the bottom panel: one line per row, the face then the cells side by side."""
    pitch = m.CELL_H + 8
    top = panel_y + (panel_h - (4 * m.CELL_H + 3 * 8)) / 2
    by_row = {u["row"]: u for u in members}
    for k in range(1, 5):
        y = top + pitch * (k - 1)
        if k not in by_row:
            continue
        u = by_row[k]
        m.draw_face(c, u, 40, y, 60)
        x = 108
        cell = 0
        for item in u["items"]:
            n = item["size"]
            w = m.COLUMN_W * n + 6 * (n - 1)
            board_cell(c, x, y, w, m.CELL_H, item["id"], item["grade"], selected == (u["key"], cell))
            x += w + 6
            cell += n
        for _ in range(u["slots"] - cell):
            board_cell(c, x, y, m.COLUMN_W, m.CELL_H)
            x += m.COLUMN_W + 6


def potions_and_hint(c, hint_x=656):
    c.frame("panel", 30, 92, 610, 84, border_scale=0.75)
    for i in range(3):
        x = 42 + 198 * i
        c.frame("slot", x, 102, 190, 64)
        if i < 2:
            c.text(x + 14, 122, "치유 포션", 21, m.TEXT)
            c.text(x + 14, 148, "HP 50 회복", 17, m.TEXT_DIM)
        else:
            c.text(x + 14, 134, "빈 칸", 21, m.TEXT_DIM)


# -------------------------------------------------------------------- right side

def map_header(c):
    plain(c, 0, 0, 1920, 80, PANEL)
    c.text(40, 40, "버려진 광산", 36, m.TEXT)
    c.text(1880, 40, "2층 / 3층", 30, m.TEXT_DIM, "rm")


def reward_header(c):
    plain(c, 0, 0, 1920, 80, PANEL)
    c.text(40, 40, "보상", 36, m.TEXT)


def map_panel(c, x, y, w, h, chosen=True):
    """The map: three floors of nodes, bottom to top, with the paths between them; the chosen node in brass."""
    plain(c, x, y, w, h, PANEL)
    ax, ay, aw, ah = x + 30, y + 30, w - 60, h - 60
    floors = [[("전투", "done"), ("전투", "done")], [("전투", "open"), ("전투", "chosen" if chosen else "open"), ("전투", "open")], [("보스", "far")]]
    rows = len(floors)
    centers = []
    for f, nodes in enumerate(floors):
        cy = ay + ah - (ah / (rows + 0.2)) * (f + 0.6)
        n = len(nodes)
        centers.append([(ax + aw * (i + 1) / (n + 1), cy) for i in range(n)])
    for f in range(rows - 1):
        for (x0, y0) in centers[f]:
            for (x1, y1) in centers[f + 1]:
                c.draw.line([(m.S(x0), m.S(y0)), (m.S(x1), m.S(y1))], fill=m.LINE + (255,), width=m.S(6))
    for f, nodes in enumerate(floors):
        for (label, state), (cx, cy) in zip(nodes, centers[f]):
            fill = {"done": (0x33, 0x33, 0x38), "open": BUTTON, "chosen": m.SELECTED, "far": m.BUTTON_QUIET}[state]
            plain(c, cx - 75, cy - 42, 150, 84, fill)
            c.text(cx, cy, label, 28, m.TEXT, "mm")


def reward_cards(c, x, y, w, selected=None):
    cards = [
        ("아이템", "장궁 (등급 11)", "무기 · 크기 2 · 쿨다운 3.0초 · 뒤에서 3번째까지\n뒤에서 2번째까지의 적에게 85% 피해", "보드의 칸을 고르세요"),
        ("아이템", "수호 부적 (등급 9)", "보조 · 크기 1 · 쿨다운 6.0초\nHP가 가장 낮은 아군에게 보호막 70", "받기"),
        ("포션", "방벽 포션", "쓰면 아군 하나에게 보호막 40", "받기"),
    ]
    for i, (kind, title, body, action) in enumerate(cards):
        cy = y + i * 212
        plain(c, x, cy, w, 200, m.SELECTED if selected == i else PANEL_LIGHT)
        c.text(x + 20, cy + 27, kind, 22, m.TEXT_DIM)
        c.text(x + 20, cy + 66, title, 32, m.TEXT)
        lines = body.split("\n")
        for j, line in enumerate(lines):
            c.text(x + 20, cy + 106 + j * 28, line, 22, m.TEXT)
        c.text(x + w - 20, cy + 175, action, 26, m.TEXT, "rm")


def detail_line(c, x, y, w, text):
    c.text(x, y, text, 19, m.TEXT)


# ---------------------------------------------------------------------- variants

MEMBERS = [
    {"side": "party", "row": 1, "key": "knight", "name": "로언", "job": "기사", "hp": 112, "maxhp": 140, "slots": 4,
     "items": [{"id": "longsword", "size": 1, "grade": 10}, {"id": "halberd", "size": 3, "grade": 12}]},
    {"side": "party", "row": 2, "key": "spellblade", "name": "카이", "job": "마검사", "hp": 100, "maxhp": 100, "slots": 4,
     "items": [{"id": "sword", "size": 1, "grade": 12}, {"id": "spear", "size": 2, "grade": 11}]},
    {"side": "party", "row": 3, "key": "bishop", "name": "엘라", "job": "주교", "hp": 90, "maxhp": 90, "slots": 4,
     "items": [{"id": "healing_staff", "size": 1, "grade": 10}, {"id": "herb_pouch", "size": 1, "grade": 9}, {"id": "ward_charm", "size": 1, "grade": 8}]},
    {"side": "party", "row": 4, "key": "archmage", "name": "미라", "job": "대마법사", "hp": 80, "maxhp": 80, "slots": 4,
     "items": [{"id": "fire_staff", "size": 1, "grade": 10}, {"id": "ember_flask", "size": 1, "grade": 9}, {"id": "longbow", "size": 2, "grade": 11}]},
]
SELECTED = ("archmage", 2)
DETAIL = "장궁 등급 11 — 무기 · 크기 2 · 쿨다운 3.0초 · 뒤에서 3번째까지 · 뒤에서 2번째까지의 적에게 85% 피해"
HINT = "아이템을 누른 뒤 다른 칸을 누르면 옮기거나 바꿉니다. 인벤토리로 보내려면 \"인벤토리로\"."

INVENTORY = [{"id": "dagger", "size": 1, "grade": 8}, {"id": "herb_pouch", "size": 1, "grade": 7},
             {"id": "longbow", "size": 2, "grade": 9}, {"id": "ward_charm", "size": 1, "grade": 8}]


def now_map(path):
    c = m.Canvas()
    map_header(c)
    map_panel(c, 980, 110, 920, 690)
    plain(c, 980, 812, 920, 228, PANEL)
    c.text(1004, 854, "2층 · 전투", 34, m.TEXT)
    c.text(1004, 899, "이 노드의 적은 들어가기 전에는 보이지 않습니다", 20, m.TEXT_DIM)
    plain_button(c, 1004, 928, 236, 76, "인벤토리 보기", 28)
    plain_button(c, 1640, 928, 236, 76, "전투 시작", 34, BUTTON)
    potions_and_hint(c)
    party_stage(c, MEMBERS, PARTY_TOP_NOW, moves_y=PARTY_TOP_NOW + 404)
    vertical_boards(c, MEMBERS, PARTY_TOP_NOW + 450, SELECTED)
    detail_line(c, 20, 896 + 12, 760, DETAIL)
    kit_button(c, 800, 896, 150, 50, "인벤토리로", 22)
    return c.finish(path)


def now_reward(path):
    c = m.Canvas()
    reward_header(c)
    reward_cards(c, 980, 110, 920, selected=0)
    c.text(980, 800, "보상을 하나 고르세요. 보드의 칸이나 인벤토리에 넣을 수 있습니다.", 22, m.TEXT_DIM)
    plain_button(c, 980, 940, 290, 84, "인벤토리에 넣기", 30)
    plain_button(c, 1290, 940, 290, 84, "인벤토리 보기", 30)
    plain_button(c, 1600, 940, 300, 84, "넘기기", 30)
    potions_and_hint(c)
    party_stage(c, MEMBERS, PARTY_TOP_NOW, moves_y=PARTY_TOP_NOW + 404)
    vertical_boards(c, MEMBERS, PARTY_TOP_NOW + 450, None)
    detail_line(c, 20, 896 + 12, 760, "장궁을 넣을 칸을 고르세요: 켜진 칸만 받습니다.")
    kit_button(c, 800, 896, 150, 50, "인벤토리로", 22, (0x2B, 0x30, 0x3A))
    return c.finish(path)


def a_map(path):
    """The battle's panel: the party's lines on the left, the node's panel and the buttons on the right."""
    c = m.Canvas()
    map_header(c)
    map_panel(c, 980, 110, 920, 630)
    potions_and_hint(c)
    party_stage(c, MEMBERS, BATTLE_TOP, moves_y=BATTLE_TOP + 404)
    panel_y, panel_h = 756, 324
    c.frame("panel", 0, panel_y, 1920, panel_h)
    c.rect(959, panel_y + 28, 2, panel_h - 56, m.LINE + (120,))
    panel_party_rows(c, MEMBERS, panel_y, panel_h, SELECTED)
    c.text(1000, 800, "2층 · 전투", 34, m.TEXT)
    c.text(1000, 846, "이 노드의 적은 들어가기 전에는 보이지 않습니다", 20, m.TEXT_DIM)
    c.text(1000, 896, "장궁 등급 11 — 무기 · 크기 2 · 쿨다운 3.0초 · 뒤에서 3번째까지", 19, m.TEXT)
    c.text(1000, 922, "뒤에서 2번째까지의 적에게 85% 피해", 19, m.TEXT)
    kit_button(c, 1000, 980, 200, 76, "인벤토리로", 26)
    plain_button(c, 1220, 980, 236, 76, "인벤토리 보기", 28)
    plain_button(c, 1644, 980, 236, 76, "전투 시작", 34, BUTTON)
    return c.finish(path)


def a_reward(path):
    c = m.Canvas()
    reward_header(c)
    reward_cards(c, 980, 110, 920, selected=0)
    potions_and_hint(c)
    party_stage(c, MEMBERS, BATTLE_TOP, moves_y=BATTLE_TOP + 404)
    panel_y, panel_h = 756, 324
    c.frame("panel", 0, panel_y, 1920, panel_h)
    c.rect(959, panel_y + 28, 2, panel_h - 56, m.LINE + (120,))
    panel_party_rows(c, MEMBERS, panel_y, panel_h, None)
    c.text(1000, 800, "보상을 하나 고르세요. 보드의 칸이나 인벤토리에 넣을 수 있습니다.", 22, m.TEXT_DIM)
    c.text(1000, 846, "장궁을 넣을 칸을 고르세요: 켜진 칸만 받습니다.", 19, m.TEXT)
    plain_button(c, 1000, 980, 210, 76, "인벤토리에 넣기", 26)
    kit_button(c, 1230, 980, 170, 76, "인벤토리로", 26, (0x2B, 0x30, 0x3A))
    plain_button(c, 1420, 980, 210, 76, "인벤토리 보기", 26)
    plain_button(c, 1650, 980, 230, 76, "넘기기", 28)
    return c.finish(path)


def b_map(path):
    """The panel's right half holds the inventory as cells (no popup); the node's panel moves under the map."""
    c = m.Canvas()
    map_header(c)
    map_panel(c, 980, 110, 920, 490)
    plain(c, 980, 612, 920, 128, PANEL)
    c.text(1004, 644, "2층 · 전투", 34, m.TEXT)
    c.text(1004, 686, "이 노드의 적은 들어가기 전에는 보이지 않습니다", 20, m.TEXT_DIM)
    plain_button(c, 1640, 632, 236, 76, "전투 시작", 34, BUTTON)
    potions_and_hint(c)
    party_stage(c, MEMBERS, BATTLE_TOP, moves_y=BATTLE_TOP + 404)
    panel_y, panel_h = 756, 324
    c.frame("panel", 0, panel_y, 1920, panel_h)
    c.rect(959, panel_y + 28, 2, panel_h - 56, m.LINE + (120,))
    panel_party_rows(c, MEMBERS, panel_y, panel_h, SELECTED)
    # the inventory: ten cells in lines of four, the items in order, each as wide as its cells
    c.text(1000, 798, "인벤토리 (5/10칸)", 24, m.TEXT)
    kit_button(c, 1700, 780, 180, 40, "인벤토리로", 20)
    x0, y0 = 1000, 826
    cells_per_line, cell_w, gap = 4, 180, 6
    line = 0
    col = 0
    for item in INVENTORY:
        n = item["size"]
        if col + n > cells_per_line:
            line += 1
            col = 0
        board_cell(c, x0 + col * (cell_w + gap), y0 + line * 68, cell_w * n + gap * (n - 1), 60, item["id"], item["grade"])
        col += n
    used = sum(i["size"] for i in INVENTORY)
    for k in range(used, 10):
        if col >= cells_per_line:
            line += 1
            col = 0
        board_cell(c, x0 + col * (cell_w + gap), y0 + line * 68, cell_w, 60)
        col += 1
    c.text(1000, 1058, "인벤토리의 아이템을 누른 뒤 보드의 칸을 누르면 들어갑니다.", 18, m.TEXT_DIM)
    return c.finish(path)


def main():
    out = m.OUT
    out.mkdir(parents=True, exist_ok=True)
    images = [
        (now_map(out / "party-now-map.png"), "노드 맵 · 지금: 파티 아래에 세로 칸, 오른쪽에 맵과 노드 패널, 인벤토리는 팝업"),
        (a_map(out / "party-A-map.png"), "노드 맵 · A안 (권장): 전투와 같은 하단 패널. 왼쪽은 파티의 줄(얼굴, 가로 칸, 등급 배지), 오른쪽은 노드 정보와 버튼. 인벤토리는 팝업 그대로"),
        (b_map(out / "party-B-map.png"), "노드 맵 · B안: 패널의 오른쪽에 인벤토리(10칸)를 늘 보임. 노드 정보는 맵 아래로. 팝업 없음"),
        (now_reward(out / "party-now-reward.png"), "보상 · 지금"),
        (a_reward(out / "party-A-reward.png"), "보상 · A안: 같은 하단 패널. 오른쪽은 안내와 버튼. 보상 카드는 위에 그대로"),
    ]
    m.composite([im for im, _ in images], [label for _, label in images], out / "party-compare.png")


if __name__ == "__main__":
    main()
