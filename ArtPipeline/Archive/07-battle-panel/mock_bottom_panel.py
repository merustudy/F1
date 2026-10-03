#!/usr/bin/env python3
"""Mockups of the battle screen with the item boards moved into a bottom panel.

Draws the 1920x1080 screen with the game's own sprites (background, figures, UI frames, item
icons, Pretendard) at 2x and scales down. No API call.

  now : the current layout (vertical boards under the plates) - to check the renderer against
        ArtPipeline/Archive/03-items/mock-battle.png
  A   : bottom panel, strip cells (180x60) in one row per unit, row k = 열 k, faces at both ends
  B   : bottom panel, square cells (60x60), two blocks per line, lower panel
"""
import sys
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFont

ROOT = Path("/Users/funitup/Projects/F1")
ART = ROOT / "Assets" / "@Art"
FONT = ROOT / "Assets" / "@Fonts" / "Source" / "Pretendard" / "Pretendard-Medium.ttf"
OUT = ROOT / "ArtPipeline" / "output" / "review"
K = 2  # render scale

# UiPalette
BG = (0x12, 0x14, 0x1A)
TEXT = (0xEB, 0xEB, 0xE6)
TEXT_DIM = (0x9A, 0xA0, 0xAC)
DANGER = (0xC0, 0x39, 0x2B)
BUTTON_QUIET = (0x3A, 0x41, 0x50)
SELECTED = (0x80, 0x66, 0x14)
GOOD = (0x58, 0xB3, 0x68)
SHIELD = (0x8F, 0xD3, 0xF4)
BURN = (0xF3, 0x9C, 0x12)
GAUGE = (0x84, 0x6A, 0x2C)
BRASS = (0xB8, 0x94, 0x4E)
INK = (0x18, 0x09, 0x07)
ICON_DIM = (0x6E, 0x6E, 0x74)
LINE = (0x55, 0x5C, 0x6B)

# UiArt borders (sprite px)
BORDER = {"panel": 40, "plate_party": 44, "plate_enemy": 44, "plate_danger": 44, "plate_target": 44,
          "plate_label": 44, "slot": 30, "slot_selected": 30, "button": 32}

_sprites = {}
_fonts = {}


def S(v):
    return int(round(v * K))


def sprite(name):
    if name not in _sprites:
        folder = "Icon" if name in ("shield", "burn", "deaths_door", "storm") else "Frame"
        _sprites[name] = Image.open(ART / "UI" / folder / f"{name}.png").convert("RGBA")
    return _sprites[name]


def font(size):
    if size not in _fonts:
        _fonts[size] = ImageFont.truetype(str(FONT), S(size))
    return _fonts[size]


def nine_slice(name, w, h, border_scale=1.0, tint=None, alpha=1.0):
    """A frame stretched to w x h (design px), its border scaled like Unity's pixelsPerUnitMultiplier."""
    src = sprite(name)
    b = BORDER[name]
    tb = max(1, int(round(b * border_scale)))
    W, H = S(w), S(h)
    tb = min(tb, W // 2 - 1, H // 2 - 1)
    out = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    xs = [(0, b), (b, src.width - b), (src.width - b, src.width)]
    ys = [(0, b), (b, src.height - b), (src.height - b, src.height)]
    xd = [(0, tb), (tb, W - tb), (W - tb, W)]
    yd = [(0, tb), (tb, H - tb), (H - tb, H)]
    for (sx0, sx1), (dx0, dx1) in zip(xs, xd):
        for (sy0, sy1), (dy0, dy1) in zip(ys, yd):
            part = src.crop((sx0, sy0, sx1, sy1))
            size = (max(1, dx1 - dx0), max(1, dy1 - dy0))
            if part.size != size:
                part = part.resize(size, Image.BILINEAR)
            out.paste(part, (dx0, dy0))
    if tint is not None:
        out = ImageChops.multiply(out, Image.new("RGBA", out.size, tint + (255,)))
    if alpha < 1.0:
        a = out.getchannel("A").point(lambda v: int(v * alpha))
        out.putalpha(a)
    return out


class Canvas:
    def __init__(self):
        self.im = Image.new("RGBA", (S(1920), S(1080)), BG + (255,))
        self.draw = ImageDraw.Draw(self.im)

    def paste(self, img, x, y):
        self.im.alpha_composite(img, (S(x), S(y)))

    def frame(self, name, x, y, w, h, **kw):
        self.paste(nine_slice(name, w, h, **kw), x, y)

    def text(self, x, y, s, size, fill=TEXT, anchor="lm"):
        self.draw.text((S(x), S(y)), s, font=font(size), fill=fill + (255,), anchor=anchor)

    def text_width(self, s, size):
        return self.draw.textlength(s, font=font(size)) / K

    def rect(self, x, y, w, h, fill):
        layer = Image.new("RGBA", (S(w), S(h)), fill)
        self.im.alpha_composite(layer, (S(x), S(y)))

    def badge(self, x, y, size, number, fontsize):
        """A brass disc with a dark rim and a number: the row of a unit, the grade of an item."""
        d = self.draw
        d.ellipse([S(x), S(y), S(x + size), S(y + size)], fill=INK + (255,))
        d.ellipse([S(x + 3), S(y + 3), S(x + size - 3), S(y + size - 3)], fill=BRASS + (255,))
        self.text(x + size / 2, y + size / 2 + 0.5, str(number), fontsize, INK, "mm")

    def icon(self, name, x, y, size):
        im = sprite(name).resize((S(size), S(size)), Image.LANCZOS)
        self.paste(im, x, y)

    def finish(self, path):
        out = self.im.convert("RGB").resize((1920, 1080), Image.LANCZOS)
        out.save(path)
        print("wrote", path)
        return out


# ------------------------------------------------------------------------- art

def figure_image(side, key):
    folder = "Job" if side == "party" else "Enemy"
    return Image.open(ART / "Unit" / folder / f"{key}.png").convert("RGBA")


def item_icon(key):
    return Image.open(ART / "Item" / f"{key}.png").convert("RGBA")


LONG_ITEMS = {"sword", "longsword", "greataxe", "mace", "healing_staff", "fire_staff", "dagger",
              "rusty_blade", "crude_bow", "overseer_maul"}

# key -> (dx, dy, scale) in figure px: a hat or a staff above the head pulls the crop off the face
FACE_TUNE = {
    "bishop": (0, 75, 1.0),
    "goblin_raider": (0, 62, 1.0),
    "goblin_archer": (0, 62, 1.0),
    "goblin_shaman": (150, 115, 1.0),
}


def face_crop(side, key, size):
    """The head of a figure: a square cut from the top of the figure around its topmost pixels."""
    im = figure_image(side, key)
    mask = im.getchannel("A").point(lambda v: 255 if v > 8 else 0)
    bbox = mask.getbbox()
    left, top, right, bottom = bbox
    h = bottom - top
    band = mask.crop((left, top, right, top + max(2, int(h * 0.06))))
    xs = [x for x in range(band.width) for y in range(band.height) if band.getpixel((x, y))]
    cx = left + (sum(xs) / len(xs) if xs else band.width / 2)
    dx, dy, sc = FACE_TUNE.get(key, (0, 0, 1.0))
    side_px = h * 0.27 * sc
    x0 = cx - side_px / 2 + dx
    y0 = top - h * 0.015 + dy
    crop = im.crop((int(x0), int(y0), int(x0 + side_px), int(y0 + side_px)))
    return crop.resize((S(size), S(size)), Image.LANCZOS)


# ---------------------------------------------------------------------- layout

FIELD_LEFT, FIELD_TOP, FIELD_WIDTH = 120, 262, 1680
FIGURE_H, FIGURE_W = 300, 225
COLUMN_W, COLUMN_GAP, SIDE_GAP = 180, 10, 180
PLATE_TOP, PLATE_H = 568, 92
ITEMS_TOP = 666
CELL_H, CELL_GAP = 60, 4


def party_column_x(row):  # row 1..4
    return FIELD_LEFT + (COLUMN_W + COLUMN_GAP) * (4 - row)


def enemy_column_x(row):
    return FIELD_LEFT + COLUMN_W * 4 + COLUMN_GAP * 3 + SIDE_GAP + (COLUMN_W + COLUMN_GAP) * (row - 1)


def draw_background(c, feet_y):
    bg = Image.open(ART / "Background" / "Dungeon" / "abandoned_mine.png").convert("RGBA")
    bg = bg.resize((S(1920), S(1280)), Image.LANCZOS)
    y = feet_y - 0.57 * 1280
    c.im.alpha_composite(bg, (0, S(y)))


def draw_figures(c, party, enemies, figure_h):
    # rear rows first so that row 1 is drawn over row 2
    figure_w = figure_h * 0.75
    for units, xfunc in ((party, party_column_x), (enemies, enemy_column_x)):
        for u in sorted(units, key=lambda u: -u["row"]):
            im = figure_image(u["side"], u["key"]).resize((S(figure_w), S(figure_h)), Image.LANCZOS)
            cx = xfunc(u["row"]) + COLUMN_W / 2
            c.paste(im, cx - figure_w / 2, FIELD_TOP)


def draw_plate(c, u, x, y):
    name = "plate_party" if u["side"] == "party" else "plate_enemy"
    c.frame(name, x, y, COLUMN_W, PLATE_H)
    c.badge(x + 11, y + 10, 26, u["row"], 17)
    c.text(x + 45, y + 10 + 13, u["name"], 20)
    # HP bar: the slot frame shrunk, the fill inside its rim
    c.frame("slot", x + 11, y + 39, COLUMN_W - 22, 23, border_scale=0.4)
    inner_w = COLUMN_W - 22 - 8
    c.rect(x + 15, y + 43, inner_w * u["hp"] / u["maxhp"], 15, GOOD + (255,))
    c.text(x + COLUMN_W / 2, y + 39 + 11.5, f"{u['hp']}/{u['maxhp']}", 15, TEXT, "mm")
    sx = x + 13
    if u.get("shield"):
        c.icon("shield", sx, y + 65, 18)
        c.text(sx + 21, y + 74, str(u["shield"]), 15, SHIELD)
        sx += 21 + c.text_width(str(u["shield"]), 15) + 10
    if u.get("burn"):
        c.icon("burn", sx, y + 65, 18)
        c.text(sx + 21, y + 74, str(u["burn"]), 15, BURN)


def draw_cell(c, x, y, w, h, item=None, charge=0.0, mirrored=False, square_stand_in=False, strip_fit=False):
    """An item cell: the slot frame, the charge inside its rim from the left, the icon inside the margins."""
    if item is None:
        c.frame("slot", x, y, w, h, alpha=0.45)
        return
    c.frame("slot", x, y, w, h)
    rim = 7
    c.rect(x + rim, y + rim, max(0, (w - 2 * rim) * charge), h - 2 * rim, GAUGE + (int(255 * 0.84),))
    icon = item_icon(item)
    if square_stand_in and icon.width > icon.height * 1.6:
        # a stand-in for a square icon that does not exist yet: the business end of a long item
        # (drawn at the right end of its strip), the middle of a compact one
        side = icon.height
        x0 = icon.width - side if item in LONG_ITEMS else (icon.width - side) // 2
        icon = icon.crop((x0, 0, x0 + side, side))
    if mirrored:
        icon = icon.transpose(Image.FLIP_LEFT_RIGHT)
    box_w, box_h = w - 20, h - 16
    ratio = min(S(box_w) / icon.width, S(box_h) / icon.height)
    icon = icon.resize((max(1, int(icon.width * ratio)), max(1, int(icon.height * ratio))), Image.LANCZOS)
    c.im.alpha_composite(icon, (S(x + 10) + (S(box_w) - icon.width) // 2, S(y + 8) + (S(box_h) - icon.height) // 2))


def draw_vertical_board(c, u, x, y):
    for item in u["items"]:
        n = item["size"]
        h = CELL_H * n + CELL_GAP * (n - 1)
        draw_cell(c, x, y, COLUMN_W, h, item["id"], item["charge"], mirrored=u["side"] == "enemy")
        y += h + CELL_GAP
    for _ in range(u["slots"] - sum(i["size"] for i in u["items"])):
        draw_cell(c, x, y, COLUMN_W, CELL_H)
        y += CELL_H + CELL_GAP


def draw_header_and_potions(c):
    c.frame("panel", 0, 0, 1920, 84)
    c.frame("button", 40, 16, 300, 52, tint=DANGER)
    c.text(190, 42, "후퇴 (60%)", 28, TEXT, "mm")
    c.text(960, 42, "7.3초", 40, TEXT, "mm")
    c.icon("storm", 1168, 20, 44)
    c.text(1220, 42, "폭풍까지 37.7초", 26, TEXT_DIM)
    c.frame("button", 1530, 16, 110, 52, tint=SELECTED)
    c.text(1585, 42, "정지", 24, TEXT, "mm")
    for i, label in enumerate(("x1", "x2", "x4")):
        c.frame("button", 1650 + 90 * i, 16, 80, 52, tint=BUTTON_QUIET)
        c.text(1690 + 90 * i, 42, label, 24, TEXT, "mm")
    c.frame("panel", 30, 92, 610, 84, border_scale=0.75)
    for i in range(3):
        x = 42 + 198 * i
        c.frame("slot", x, 102, 190, 64)
        if i < 2:
            c.text(x + 14, 102 + 20, "치유 포션", 21, TEXT)
            c.text(x + 14, 102 + 46, "HP 50 회복", 17, TEXT_DIM)
        else:
            c.text(x + 14, 102 + 32, "빈 칸", 21, TEXT_DIM)
    hint = "포션을 누른 뒤 아군을 누르세요"
    w = c.text_width(hint, 20) + 44
    c.frame("plate_label", 656, 110, w, 48, border_scale=0.6)
    c.text(656 + 22, 134, hint, 20, TEXT_DIM)


def draw_face(c, u, x, y, size):
    name = "plate_party" if u["side"] == "party" else "plate_enemy"
    c.frame(name, x, y, size, size, border_scale=0.5)
    inset = 5
    face = face_crop(u["side"], u["key"], size - 2 * inset)
    c.paste(face, x + inset, y + inset)
    c.badge(x + 2, y + size - 24, 22, u["row"], 14)


# ------------------------------------------------------------------- variants

def scene_stage(c, party, enemies, figure_h=FIGURE_H):
    """The stage: the background with its floor under the figures' feet, the figures, their plates."""
    feet_y = FIELD_TOP + figure_h
    draw_background(c, feet_y)
    draw_figures(c, party, enemies, figure_h)
    plate_top = feet_y + 6
    for u in party:
        draw_plate(c, u, party_column_x(u["row"]), plate_top)
    for u in enemies:
        draw_plate(c, u, enemy_column_x(u["row"]), plate_top)


def draw_bottom_panel_rows(c, party, enemies, panel_y, panel_h):
    """Variant A's panel: one line per row (열 1 at the top), strip cells, faces at both ends."""
    c.frame("panel", 0, panel_y, 1920, panel_h)
    pitch = CELL_H + 8
    top = panel_y + (panel_h - (4 * CELL_H + 3 * 8)) / 2
    by_row_p = {u["row"]: u for u in party}
    by_row_e = {u["row"]: u for u in enemies}
    for k in range(1, 5):
        y = top + pitch * (k - 1)
        if k in by_row_p:
            u = by_row_p[k]
            draw_face(c, u, 40, y, 60)
            row_items_lr(c, u, 108, y, COLUMN_W, CELL_H, 6)
        if k in by_row_e:
            u = by_row_e[k]
            draw_face(c, u, 1820, y, 60)
            row_items_rl(c, u, 1812, y, COLUMN_W, CELL_H, 6)
    # a faint seam in the middle, like the gap between the two sides on the stage
    c.rect(959, panel_y + 28, 2, panel_h - 56, LINE + (120,))


def variant_now(party, enemies, path):
    c = Canvas()
    scene_stage(c, party, enemies)
    for u in party:
        draw_vertical_board(c, u, party_column_x(u["row"]), ITEMS_TOP)
    for u in enemies:
        draw_vertical_board(c, u, enemy_column_x(u["row"]), ITEMS_TOP)
    draw_header_and_potions(c)
    return c.finish(path)


def row_items_lr(c, u, x, y, cell_w, cell_h, gap, square=False):
    """A unit's board laid out to the right from x: items in order, then the empty cells."""
    for item in u["items"]:
        n = item["size"]
        w = cell_w * n + gap * (n - 1)
        draw_cell(c, x, y, w, cell_h, item["id"], item["charge"], square_stand_in=square)
        x += w + gap
    for _ in range(u["slots"] - sum(i["size"] for i in u["items"])):
        draw_cell(c, x, y, cell_w, cell_h)
        x += cell_w + gap
    return x


def row_items_rl(c, u, x_end, y, cell_w, cell_h, gap, square=False):
    """An enemy's board laid out to the left from x_end: the first item next to the face, icons mirrored."""
    x = x_end
    for item in u["items"]:
        n = item["size"]
        w = cell_w * n + gap * (n - 1)
        x -= w
        draw_cell(c, x, y, w, cell_h, item["id"], item["charge"], mirrored=True, square_stand_in=square)
        x -= gap
    return x


def variant_a(party, enemies, path):
    """Bottom panel: one line per row (열 1 at the top), strip cells 180x60, faces at both ends."""
    c = Canvas()
    scene_stage(c, party, enemies)
    draw_bottom_panel_rows(c, party, enemies, 756, 324)
    draw_header_and_potions(c)
    return c.finish(path)


def variant_c(party, enemies, path):
    """Variant A with a bigger stage: the figures grow into the room the boards left (300 -> 340)."""
    c = Canvas()
    scene_stage(c, party, enemies, figure_h=340)
    draw_bottom_panel_rows(c, party, enemies, 756, 324)
    draw_header_and_potions(c)
    return c.finish(path)


def variant_b(party, enemies, path):
    """Bottom panel: square cells 60x60, two blocks per line (열 1·2 above 열 3·4), a lower panel."""
    c = Canvas()
    scene_stage(c, party, enemies)
    cell = 60
    block_w = 60 + 8 + (cell * 4 + 6 * 3)  # face + board of four cells = 326
    panel_h = 2 * cell + 8 + 2 * 30
    panel_y = 1080 - panel_h
    c.frame("panel", 0, panel_y, 1920, panel_h)
    by_row_p = {u["row"]: u for u in party}
    by_row_e = {u["row"]: u for u in enemies}
    for k in range(1, 5):
        line = (k - 1) // 2
        col = (k - 1) % 2
        y = panel_y + 30 + line * (cell + 8)
        if k in by_row_p:
            u = by_row_p[k]
            x = 40 + col * (block_w + 24)
            draw_face(c, u, x, y, 60)
            row_items_lr(c, u, x + 68, y, cell, cell, 6, square=True)
        if k in by_row_e:
            u = by_row_e[k]
            x_face = 1880 - 60 - col * (block_w + 24)
            draw_face(c, u, x_face, y, 60)
            row_items_rl(c, u, x_face - 8, y, cell, cell, 6, square=True)
    c.rect(959, panel_y + 24, 2, panel_h - 48, LINE + (120,))
    draw_header_and_potions(c)
    return c.finish(path)


def faces_sheet(party, enemies, path):
    c = Canvas()
    c.im = Image.new("RGBA", (S(11 * 100 + 40), S(140)), BG + (255,))
    c.draw = ImageDraw.Draw(c.im)
    x = 20
    for u in party + enemies:
        draw_face(c, u, x, 20, 80)
        c.text(x + 40, 120, u["key"], 12, TEXT_DIM, "mm")
        x += 100
    out = c.im.convert("RGB").resize((c.im.width // K, c.im.height // K), Image.LANCZOS)
    out.save(path)
    print("wrote", path)


def composite(images, labels, path, width=1280):
    band = 48
    h = int(1080 * width / 1920)
    sheet = Image.new("RGB", (width, len(images) * (h + band)), (24, 26, 32))
    d = ImageDraw.Draw(sheet)
    f = ImageFont.truetype(str(FONT), 26)
    y = 0
    for im, label in zip(images, labels):
        d.text((16, y + band / 2), label, font=f, fill=TEXT, anchor="lm")
        sheet.paste(im.resize((width, h), Image.LANCZOS), (0, y + band))
        y += h + band
    sheet.save(path)
    print("wrote", path)


def main():
    party = [
        {"side": "party", "row": 1, "key": "knight", "name": "로언", "hp": 62, "maxhp": 140, "shield": 12, "slots": 4,
         "items": [{"id": "longsword", "size": 1, "charge": 0.75}, {"id": "halberd", "size": 3, "charge": 0.3}]},
        {"side": "party", "row": 2, "key": "spellblade", "name": "카이", "hp": 71, "maxhp": 100, "slots": 4,
         "items": [{"id": "sword", "size": 1, "charge": 0.55}, {"id": "spear", "size": 2, "charge": 0.45}]},
        {"side": "party", "row": 3, "key": "bishop", "name": "엘라", "hp": 90, "maxhp": 90, "slots": 4,
         "items": [{"id": "healing_staff", "size": 1, "charge": 0.5}, {"id": "herb_pouch", "size": 1, "charge": 0.35},
                   {"id": "ward_charm", "size": 1, "charge": 0.2}]},
        {"side": "party", "row": 4, "key": "archmage", "name": "미라", "hp": 80, "maxhp": 80, "slots": 4,
         "items": [{"id": "fire_staff", "size": 1, "charge": 0.6}, {"id": "ember_flask", "size": 1, "charge": 0.25},
                   {"id": "longbow", "size": 2, "charge": 0.6}]},
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
    OUT.mkdir(parents=True, exist_ok=True)
    which = sys.argv[1:] or ["faces", "now", "A", "B", "C", "compare"]
    if "faces" in which:
        faces_sheet(party, enemies, OUT / "panel-faces.png")
    results = {}
    if "now" in which:
        results["now"] = variant_now(party, enemies, OUT / "panel-now.png")
    if "A" in which:
        results["A"] = variant_a(party, enemies, OUT / "panel-A.png")
    if "B" in which:
        results["B"] = variant_b(party, enemies, OUT / "panel-B.png")
    if "C" in which:
        results["C"] = variant_c(party, enemies, OUT / "panel-C.png")
    if "compare" in which and len(results) == 4:
        composite([results["now"], results["A"], results["B"], results["C"]],
                  ["지금: 명패 아래에 아이템을 세로로",
                   "A안 (권장): 하단 패널, 띠 칸(180x60)을 유닛마다 한 줄로 (1열이 위, 적은 거울), 양 끝에 얼굴. 무대는 그대로",
                   "B안: 하단 패널, 정사각 칸(60x60), 한 편을 두 줄 두 블록으로 (낮은 패널). 아이콘 21개를 다시 그려야 한다",
                   "C안: A안 + 무대 확대 (그림 자리 300 -> 340). 비워진 세로 공간을 캐릭터에 준다"],
                  OUT / "panel-compare.png")


if __name__ == "__main__":
    main()
