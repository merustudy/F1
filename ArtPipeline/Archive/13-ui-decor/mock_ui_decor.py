"""Mockups of three UI decoration directions on top of the real battle screenshot (round 13).

A camp kit (planks, hanging sign, potion belt, props)  B hand-painted wonky signboards  C parchment ledger.
Generated sample pieces (round 13 mock roster) are composited for A; everything else is drawn. No API call.
Run from the repository root: .venv/bin/python ArtPipeline/Archive/13-ui-decor/mock_ui_decor.py <battle.png> <map.png>
"""
import math, random, sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageFilter

ROOT = Path(__file__).resolve().parents[3]
OUT = Path(__file__).resolve().parent
FONT = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"
PLANKS = ROOT / "ArtPipeline/output/ui_frame/mock_table_planks.png"
SIGN = ROOT / "ArtPipeline/output/ui_piece/mock_sign.png"
POTION = ROOT / "ArtPipeline/output/ui_icon/mock_potion_heal.png"
STORM = ROOT / "Assets/@Art/UI/Icon/storm.png"
random.seed(13)

INK = (24, 9, 7, 255)
WOOD = (74, 48, 32, 255); WOOD_L = (104, 70, 44, 255); WOOD_D = (52, 33, 22, 255)
BRASS = (184, 140, 60, 255); BRASS_D = (120, 88, 34, 255)
NAVY = (44, 74, 112, 255); RED = (112, 53, 44, 255)
CREAM = (232, 220, 192, 255); CREAM_D = (206, 190, 156, 255); INKB = (60, 36, 24, 255)
PAINT_NAVY = (52, 92, 140, 255); PAINT_RED = (170, 56, 44, 255); PAINT_CREAM = (236, 226, 196, 255)

def font(size): return ImageFont.truetype(str(FONT), size)

def rounded(draw, box, r, fill, outline=None, width=0):
    draw.rounded_rectangle(box, radius=r, fill=fill, outline=outline, width=width)

def nails(draw, box, step=60, r=3, inset=9, color=BRASS, dark=BRASS_D):
    x0, y0, x1, y1 = box
    for x in range(x0 + inset, x1 - inset + 1, step):
        for y in (y0 + inset, y1 - inset):
            draw.ellipse([x - r, y - r, x + r, y + r], fill=color, outline=dark)
    for y in range(y0 + inset + step, y1 - inset, step):
        for x in (x0 + inset, x1 - inset):
            draw.ellipse([x - r, y - r, x + r, y + r], fill=color, outline=dark)

def wobble_poly(box, amp=3, step=18, rot=0.0):
    """A rectangle whose edges wobble like a hand-cut board, optionally rotated a little."""
    x0, y0, x1, y1 = box; pts = []
    def edge(ax, ay, bx, by):
        n = max(2, int(math.hypot(bx - ax, by - ay) / step))
        for i in range(n):
            t = i / n; px = ax + (bx - ax) * t; py = ay + (by - ay) * t
            nx, ny = -(by - ay), (bx - ax); L = math.hypot(nx, ny) or 1
            w = random.uniform(-amp, amp); pts.append((px + nx / L * w, py + ny / L * w))
    edge(x0, y0, x1, y0); edge(x1, y0, x1, y1); edge(x1, y1, x0, y1); edge(x0, y1, x0, y0)
    if rot:
        cx, cy = (x0 + x1) / 2, (y0 + y1) / 2; c, s = math.cos(rot), math.sin(rot)
        pts = [(cx + (x - cx) * c - (y - cy) * s, cy + (x - cx) * s + (y - cy) * c) for x, y in pts]
    return pts

def tile(base, box, tex):
    x0, y0, x1, y1 = box; w, h = tex.size
    for y in range(y0, y1, h):
        for x in range(x0, x1, w):
            t = tex.crop((0, 0, min(w, x1 - x), min(h, y1 - y))); base.alpha_composite(t, (x, y))

def label(draw, xy, s, size, color=(240, 236, 228, 255), anchor="la"):
    draw.text(xy, s, font=font(size), fill=color, anchor=anchor)

def ink_frame(img, color=INK, width=12):
    """A hand-drawn dark border around the whole screen: a torn, slightly wobbly edge with an inner shadow."""
    w, h = img.size; layer = Image.new("RGBA", img.size, (0, 0, 0, 0)); d = ImageDraw.Draw(layer)
    outer = [(0, 0), (w, 0), (w, h), (0, h)]
    inner = wobble_poly((width, width, w - width, h - width), amp=4, step=40)
    d.polygon(outer, fill=color); d.polygon(inner, fill=(0, 0, 0, 0))
    shadow = Image.new("RGBA", img.size, (0, 0, 0, 0)); ds = ImageDraw.Draw(shadow)
    ds.polygon(wobble_poly((width - 2, width - 2, w - width + 2, h - width + 2), amp=4, step=40), outline=(0, 0, 0, 120), width=10)
    shadow = shadow.filter(ImageFilter.GaussianBlur(5)); layer.alpha_composite(shadow)
    img.alpha_composite(layer)

# -------- regions of the 1920x1080 battle screenshot (UiPrefabSetup.Battle)
PANEL = (10, 625, 1910, 1075)
PARTY_COLS = [120 + j * 190 for j in range(4)]; ENEMY_COLS = [1050, 1240]
SLOT_Y0, SLOT_H, SLOT_GAP, SLOT_W = 636, 50, 54, 180
PARTY_PLATES = [(122 + j * 190, 520, 298 + j * 190, 604) for j in range(4)]
ENEMY_PLATES = [(1248, 520, 1424, 604), (1440, 520, 1616, 604)]
CLOCK = (960, 712, 80); STORM_TEXT = (890, 806, 1045, 834)
POTIONS = (36, 92, 636, 172)

def crop_slots(src, dst, rows_party=5, rows_enemy=1):
    for x in PARTY_COLS:
        for i in range(rows_party):
            y = SLOT_Y0 + i * SLOT_GAP; dst.alpha_composite(src.crop((x, y, x + SLOT_W, y + SLOT_H)), (x, y))
    for x in ENEMY_COLS:
        for i in range(rows_enemy):
            y = SLOT_Y0 + i * SLOT_GAP; dst.alpha_composite(src.crop((x, y, x + SLOT_W, y + SLOT_H)), (x, y))

def crop_clock(src, dst):
    cx, cy, r = CLOCK; box = (cx - r, cy - r, cx + r, cy + r)
    mask = Image.new("L", src.size, 0); ImageDraw.Draw(mask).ellipse(box, fill=255)
    piece = Image.new("RGBA", src.size, (0, 0, 0, 0)); piece.paste(src, (0, 0), mask); dst.alpha_composite(piece)
    dst.alpha_composite(src.crop(STORM_TEXT), (STORM_TEXT[0], STORM_TEXT[1]))

def plate_values():
    return [("4", "미라", 80, 80), ("3", "엘라", 90, 90), ("2", "카이", 100, 100), ("1", "로언", 120, 140)], [("1", "고블린 약탈자", 54, 80), ("2", "고블린 약탈자", 54, 80)]

def hp_bar(draw, box, hp, mx, groove=(40, 26, 18, 255), fill=(84, 160, 72, 255), chalk=False):
    x0, y0, x1, y1 = box
    rounded(draw, box, 5, groove, outline=INK, width=2)
    w = int((x1 - x0 - 6) * hp / mx)
    if chalk:
        for x in range(x0 + 4, x0 + 4 + w, 8): draw.line([(x, y0 + 4), (x + 4, y1 - 4)], fill=(230, 240, 220, 230), width=3)
    else:
        rounded(draw, (x0 + 3, y0 + 3, x0 + 3 + w, y1 - 3), 3, fill)
    label(draw, ((x0 + x1) // 2, (y0 + y1) // 2), f"{hp}/{mx}", 16, anchor="mm")

def props_camp(img, draw):
    # lantern (bottom right of the panel)
    x, y = 1800, 905; rounded(draw, (x - 22, y, x + 22, y + 70), 6, (70, 60, 52, 255), outline=INK, width=3)
    draw.rectangle([x - 14, y + 12, x + 14, y + 56], fill=(246, 196, 90, 255), outline=INK, width=2)
    draw.rectangle([x - 8, y - 14, x + 8, y], fill=(60, 50, 44, 255), outline=INK, width=3); draw.arc([x - 16, y - 34, x + 16, y - 6], 180, 360, fill=INK, width=3)
    glow = Image.new("RGBA", img.size, (0, 0, 0, 0)); ImageDraw.Draw(glow).ellipse([x - 90, y - 50, x + 90, y + 130], fill=(246, 196, 90, 60)); img.alpha_composite(glow.filter(ImageFilter.GaussianBlur(24)))
    # rolled map (bottom left)
    x, y = 70, 960; rounded(draw, (x, y, x + 150, y + 36), 18, CREAM, outline=INK, width=3); draw.ellipse([x - 10, y, x + 26, y + 36], fill=CREAM_D, outline=INK, width=3); draw.ellipse([x + 124, y, x + 160, y + 36], fill=CREAM_D, outline=INK, width=3)
    draw.line([(x + 60, y + 4), (x + 60, y + 32)], fill=(150, 40, 40, 255), width=4)
    # dice beside the clock
    for (x, y, rot) in ((1060, 880, 0.2), (1098, 896, -0.3)):
        pts = wobble_poly((x, y, x + 28, y + 28), amp=0, rot=rot); draw.polygon(pts, fill=CREAM, outline=INK); cx, cy = x + 14, y + 14
        for dx, dy in ((-7, -7), (7, 7), (0, 0)): draw.ellipse([cx + dx - 2, cy + dy - 2, cx + dx + 2, cy + dy + 2], fill=INK)

def clear_header(img, src, boxes=((360, 8, 560, 72), (700, 8, 1220, 76))):
    """Covers the old title and the caption lines with plain header leather taken from the screenshot."""
    patch = src.crop((1230, 10, 1500, 74)).convert("RGBA")
    for x0, y0, x1, y1 in boxes:
        for x in range(x0, x1, patch.width):
            piece = patch.crop((0, 0, min(patch.width, x1 - x), min(patch.height, y1 - y0))); img.alpha_composite(piece, (x, y0))

def sign_header(img, draw, title, size=24, painted=False, board_width=320):
    """The hanging sign piece, scaled so that its board (not its rope loops) is board_width wide, hung from the top edge."""
    sign = Image.open(SIGN).convert("RGBA"); sign = sign.crop(sign.getchannel("A").point(lambda a: 255 if a > 8 else 0).getbbox())
    # After the crop the piece is as wide as its board (the rope loops stand inside that width).
    scale = board_width / sign.width; sign = sign.resize((round(sign.width * scale), round(sign.height * scale)), Image.LANCZOS)
    x = 960 - sign.width // 2; y = -2; img.alpha_composite(sign, (x, y))
    color = (236, 220, 180, 255) if not painted else (240, 236, 228, 255)
    label(draw, (960, y + int(sign.height * 0.66)), title, size, color=color, anchor="mm")

def potion_belt(img, draw, style):
    x0, y0, x1, y1 = POTIONS
    if style == "camp":
        rounded(draw, (x0, y0 + 6, x1, y1 - 6), 14, (92, 60, 36, 255), outline=INK, width=4)
        for x in range(x0 + 18, x1 - 18, 14): draw.line([(x, y0 + 14), (x + 6, y0 + 14)], fill=(214, 186, 140, 200), width=2); draw.line([(x, y1 - 14), (x + 6, y1 - 14)], fill=(214, 186, 140, 200), width=2)
        rounded(draw, (x0 + 10, y0 + 18, x0 + 44, y1 - 18), 4, BRASS, outline=INK, width=3)
        bottle = Image.open(POTION).convert("RGBA").resize((56, 56), Image.LANCZOS)
        for i in range(3):
            lx = x0 + 70 + i * 190; rounded(draw, (lx, y0 + 12, lx + 170, y1 - 12), 10, (70, 44, 26, 255), outline=(40, 24, 14, 255), width=3)
            if i < 2: img.alpha_composite(bottle, (lx + 8, y0 + 12)); label(draw, (lx + 74, y0 + 26), "치유 포션", 18); label(draw, (lx + 74, y0 + 48), "HP 50 회복", 15, color=(200, 190, 170, 255))
            else: label(draw, (lx + 85, (y0 + y1) // 2), "빈 고리", 17, color=(150, 136, 118, 255), anchor="mm")
    elif style == "paint":
        draw.polygon(wobble_poly((x0, y0 + 4, x1, y1 - 4), amp=4, step=24, rot=0.01), fill=(98, 66, 40, 255), outline=INK)
        for i in range(3):
            lx = x0 + 22 + i * 198; draw.polygon(wobble_poly((lx, y0 + 14, lx + 176, y1 - 14), amp=3, rot=random.uniform(-0.03, 0.03)), fill=PAINT_CREAM if i < 2 else (120, 94, 70, 255), outline=INK)
            if i < 2:
                draw.ellipse([lx + 14, y0 + 22, lx + 46, y1 - 22], fill=PAINT_RED, outline=INK, width=3); draw.rectangle([lx + 25, y0 + 16, lx + 35, y0 + 26], fill=(150, 110, 70, 255), outline=INK)
                label(draw, (lx + 60, y0 + 24), "치유 포션", 19, color=INKB); label(draw, (lx + 60, y0 + 48), "HP 50", 15, color=INKB)
            else: label(draw, (lx + 88, (y0 + y1) // 2), "빈 고리", 17, color=(226, 214, 190, 255), anchor="mm")
    else:  # ledger: sketched bottles on a paper strip
        draw.polygon(wobble_poly((x0, y0 + 4, x1, y1 - 4), amp=2, step=30), fill=CREAM, outline=INKB)
        for i in range(3):
            lx = x0 + 22 + i * 198; draw.rectangle([lx, y0 + 14, lx + 176, y1 - 14], outline=INKB, width=2)
            if i < 2:
                draw.ellipse([lx + 12, y0 + 24, lx + 44, y1 - 20], outline=INKB, width=3); draw.rectangle([lx + 22, y0 + 16, lx + 34, y0 + 26], outline=INKB, width=2)
                draw.ellipse([lx + 18, y0 + 38, lx + 38, y1 - 24], fill=(170, 60, 50, 255))
                label(draw, (lx + 58, y0 + 22), "치유 포션", 19, color=INKB); label(draw, (lx + 58, y0 + 46), "HP 50 회복", 15, color=(110, 86, 60, 255))
            else: label(draw, (lx + 88, (y0 + y1) // 2), "— 빈 칸 —", 17, color=(150, 130, 100, 255), anchor="mm")

def plates(draw, style):
    party, enemy = plate_values()
    for boxes, values, side in ((PARTY_PLATES, party, "party"), (ENEMY_PLATES, enemy, "enemy")):
        for box, (num, name, hp, mx) in zip(boxes, values):
            x0, y0, x1, y1 = box
            if style == "camp":
                rounded(draw, box, 8, WOOD, outline=INK, width=4); rounded(draw, (x0 + 5, y0 + 5, x1 - 5, y1 - 5), 6, None, outline=WOOD_L, width=2)
                draw.rectangle([x0 + 5, y0 + 5, x1 - 5, y0 + 30], fill=NAVY if side == "party" else RED)
                nails(draw, box, step=400, r=3, inset=8)
                draw.ellipse([x0 + 12, y0 + 8, x0 + 32, y0 + 28], fill=BRASS, outline=INK, width=2); label(draw, (x0 + 22, y0 + 18), num, 14, color=INK, anchor="mm")
                label(draw, (x0 + 40, y0 + 18), name, 18, anchor="lm"); hp_bar(draw, (x0 + 12, y0 + 40, x1 - 12, y0 + 66), hp, mx)
            elif style == "paint":
                rot = random.uniform(-0.035, 0.035); draw.polygon(wobble_poly(box, amp=3, rot=rot), fill=PAINT_NAVY if side == "party" else PAINT_RED, outline=INK)
                draw.polygon(wobble_poly((x0 + 7, y0 + 7, x1 - 7, y1 - 7), amp=2, rot=rot), outline=(230, 222, 200, 160))
                draw.ellipse([x0 + 10, y0 + 8, x0 + 34, y0 + 32], fill=(176, 40, 36, 255), outline=INK, width=2); label(draw, (x0 + 22, y0 + 20), num, 14, color=(246, 236, 220, 255), anchor="mm")
                label(draw, (x0 + 42, y0 + 20), name, 19, anchor="lm"); hp_bar(draw, (x0 + 12, y0 + 42, x1 - 12, y0 + 68), hp, mx, groove=(30, 24, 20, 255), chalk=True)
            else:
                draw.polygon(wobble_poly(box, amp=2, step=30), fill=CREAM, outline=INKB)
                draw.line([(x0 + 10, y0 + 34), (x1 - 10, y0 + 34)], fill=(190, 170, 140, 255), width=1)
                draw.ellipse([x0 + 10, y0 + 8, x0 + 34, y0 + 32], fill=(170, 46, 40, 255) if side == "enemy" else NAVY, outline=INKB, width=2); label(draw, (x0 + 22, y0 + 20), num, 14, anchor="mm")
                label(draw, (x0 + 42, y0 + 20), name, 19, color=INKB, anchor="lm"); hp_bar(draw, (x0 + 12, y0 + 42, x1 - 12, y0 + 68), hp, mx, groove=(226, 214, 190, 255), fill=(118, 150, 88, 255))
                draw.text((x0 + 12, y0 + 42), "", font=font(10))

def clock_decor(img, draw, style):
    cx, cy, r = CLOCK
    if style == "camp":
        draw.ellipse([cx - r - 10, cy - r - 10, cx + r + 10, cy + r + 10], outline=BRASS_D, width=6)
        for i in range(8): y = 632 - i * 10; draw.ellipse([cx - 5, y - 5, cx + 5, y + 5], outline=BRASS, width=3)
        storm = Image.open(STORM).convert("RGBA").resize((36, 36), Image.LANCZOS); img.alpha_composite(storm, (cx - 18, cy - r - 46))
    elif style == "paint":
        draw.polygon(wobble_poly((cx - r - 16, cy - r - 16, cx + r + 16, cy + r + 16), amp=4, rot=0.05), outline=PAINT_CREAM, width=4)
    else:
        draw.ellipse([cx - r - 12, cy - r - 12, cx + r + 12, cy + r + 12], outline=INKB, width=3)
        for a in range(0, 360, 30): ax = cx + math.cos(math.radians(a)) * (r + 12); ay = cy + math.sin(math.radians(a)) * (r + 12); draw.ellipse([ax - 3, ay - 3, ax + 3, ay + 3], fill=INKB)

ITEM_DIR = ROOT / "Assets/@Art/Item"
COLUMN_ITEMS = {120: "fire_staff", 310: "healing_staff", 500: "sword", 690: "longsword", 1050: "rusty_blade", 1240: "rusty_blade"}

def board_rects():
    """The bag behind each unit's column of cells: 4 px beside the cells, 3 px above and below (V안)."""
    rects = []
    for x in PARTY_COLS: rects.append((x - 4, SLOT_Y0 - 3, x + SLOT_W + 4, SLOT_Y0 + 5 * SLOT_GAP - SLOT_GAP + SLOT_H + 3, 5, x))
    for x in ENEMY_COLS: rects.append((x - 4, SLOT_Y0 - 3, x + SLOT_W + 4, SLOT_Y0 + SLOT_H + 3, 1, x))
    return rects

def stitches(draw, box, color, step=10, inset=4):
    x0, y0, x1, y1 = box
    for x in range(x0 + 8, x1 - 8, step): draw.line([(x, y0 + inset), (x + 5, y0 + inset)], fill=color, width=2); draw.line([(x, y1 - inset), (x + 5, y1 - inset)], fill=color, width=2)
    for y in range(y0 + 8, y1 - 8, step): draw.line([(x0 + inset, y), (x0 + inset, y + 5)], fill=color, width=2); draw.line([(x1 - inset, y), (x1 - inset, y + 5)], fill=color, width=2)

def icon_in(img, cx, y, pale=False):
    icon = Image.open(ITEM_DIR / f"{COLUMN_ITEMS[cx]}.png").convert("RGBA").resize((164, 40), Image.LANCZOS)
    if cx in ENEMY_COLS: icon = icon.transpose(Image.FLIP_LEFT_RIGHT)
    img.alpha_composite(icon, (cx + 8, y + 5))

def boards(img, draw, src, style):
    """The bottom panel's boards in one of the contrast variants, drawn over the planks."""
    if style == "navy":
        # A1: the leather bag becomes a navy wool bag (a tint variant of the bag frame); the iron cells stay
        for x0, y0, x1, y1, n, cx in board_rects():
            rounded(draw, (x0 - 3, y0 - 3, x1 + 3, y1 + 3), 7, (46, 62, 96, 255), outline=(20, 26, 40, 255), width=3); stitches(draw, (x0 - 3, y0 - 3, x1 + 3, y1 + 3), (140, 156, 190, 170), inset=3)
        crop_slots(src, img); crop_clock(src, img)
    elif style == "paper":
        # A2: each unit's kit on a parchment sheet pinned to the table; cells are ink boxes
        for x0, y0, x1, y1, n, cx in board_rects():
            draw.polygon(wobble_poly((x0 - 4, y0 - 6, x1 + 4, y1 + 6), amp=2, step=24, rot=random.uniform(-0.006, 0.006)), fill=CREAM, outline=INKB)
            for px, py in ((x0 + 4, y0 - 1), (x1 - 4, y0 - 1)): draw.ellipse([px - 5, py - 5, px + 5, py + 5], fill=(176, 40, 36, 255), outline=INK, width=2)
            for i in range(n):
                y = SLOT_Y0 + i * SLOT_GAP; cell = (cx + 2, y + 2, cx + SLOT_W - 2, y + SLOT_H - 2)
                draw.rectangle(cell, fill=(224, 212, 182, 255) if i else (214, 200, 166, 255), outline=INKB, width=2)
                if i == 0:
                    if cx == 120: draw.rectangle((cell[0] + 2, cell[1] + 2, cell[0] + int((cell[2] - cell[0]) * 0.55), cell[3] - 2), fill=(206, 170, 96, 255))
                    icon_in(img, cx, y)
        crop_clock(src, img)
    elif style == "roll":
        # A3: a canvas tool roll per unit: cream canvas with a leather strap at the top, each cell a stitched pocket
        for x0, y0, x1, y1, n, cx in board_rects():
            rounded(draw, (x0 - 4, y0 - 12, x1 + 4, y1 + 4), 8, (214, 198, 160, 255), outline=(60, 44, 30, 255), width=3)
            draw.rectangle([x0 - 4, y0 - 12, x1 + 4, y0 - 2], fill=(92, 60, 36, 255)); draw.rectangle([x0 - 4, y0 - 12, x1 + 4, y0 - 2], outline=(60, 44, 30, 255), width=2)
            draw.rectangle([cx + 70, y0 - 13, cx + 110, y0 + 1], fill=BRASS, outline=INK, width=2)
            for i in range(n):
                y = SLOT_Y0 + i * SLOT_GAP; cell = (cx, y, cx + SLOT_W, y + SLOT_H)
                rounded(draw, cell, 5, (196, 178, 140, 255) if i else (186, 166, 126, 255), outline=(70, 50, 34, 255), width=2); stitches(draw, cell, (120, 100, 70, 200), step=9, inset=3)
                if i == 0:
                    if cx == 120: rounded(draw, (cell[0] + 3, cell[1] + 3, cell[0] + int(SLOT_W * 0.55), cell[3] - 3), 3, (176, 142, 72, 255))
                    icon_in(img, cx, y)
        crop_clock(src, img)
    elif style == "felt":
        # A4: the table's top is a navy felt cloth inside the wooden edge; the leather bags and cells stay as they are
        x0, y0, x1, y1 = PANEL; felt = (x0 + 26, y0 + 26, x1 - 26, y1 - 26)
        draw.polygon(wobble_poly(felt, amp=2, step=50), fill=(40, 52, 78, 255), outline=(22, 28, 44, 255)); stitches(draw, felt, (150, 160, 190, 120), step=14, inset=6)
        for bx0, by0, bx1, by1, n, cx in board_rects(): img.alpha_composite(src.crop((bx0, by0, bx1, by1)), (bx0, by0))
        crop_slots(src, img); crop_clock(src, img)
    else:
        crop_slots(src, img); crop_clock(src, img)

def mock_battle(src, style, out, board_style="leather"):
    img = src.copy(); draw = ImageDraw.Draw(img)
    x0, y0, x1, y1 = PANEL
    if style == "camp":
        tex = Image.open(PLANKS).convert("RGBA").crop((22, 22, 242, 170)); tile(img, PANEL, tex)
        rounded(draw, PANEL, 10, None, outline=WOOD_D, width=14); rounded(draw, PANEL, 10, None, outline=INK, width=4); nails(draw, PANEL, step=64, r=4, inset=10)
        boards(img, draw, src, board_style); props_camp(img, draw); clock_decor(img, draw, style)
        clear_header(img, src); sign_header(img, draw, "버려진 광산 · 1층"); potion_belt(img, draw, "camp"); plates(draw, "camp"); ink_frame(img)
    elif style == "paint":
        draw.rectangle(PANEL, fill=(118, 84, 56, 255))
        for y in range(y0, y1, 38): draw.line([(x0, y), (x1, y)], fill=(96, 66, 44, 255), width=3)
        for _ in range(14):
            px, py = random.randint(x0 + 30, x1 - 30), random.randint(y0 + 20, y1 - 20); c = random.choice([PAINT_RED, PAINT_NAVY, PAINT_CREAM]); draw.ellipse([px - 6, py - 4, px + 6, py + 4], fill=c)
        draw.polygon(wobble_poly(PANEL, amp=5, step=40), outline=INK, width=6)
        crop_slots(src, img); crop_clock(src, img); clock_decor(img, draw, style)
        for x in PARTY_COLS + ENEMY_COLS:
            for i in range(5 if x in PARTY_COLS else 1):
                y = SLOT_Y0 + i * SLOT_GAP; draw.polygon(wobble_poly((x - 2, y - 2, x + SLOT_W + 2, y + SLOT_H + 2), amp=3, rot=random.uniform(-0.02, 0.02)), outline=PAINT_CREAM, width=3)
        clear_header(img, src); draw.polygon(wobble_poly((560, 6, 1360, 78), amp=4, rot=0.01), fill=(98, 66, 40, 255), outline=INK); draw.polygon(wobble_poly((572, 14, 1348, 70), amp=3, rot=0.01), outline=PAINT_CREAM, width=3)
        label(draw, (960, 42), "버려진 광산 · 1층", 30, color=PAINT_CREAM, anchor="mm")
        # paint bucket and brush props
        draw.rectangle([1820, 960, 1870, 1030], fill=(120, 120, 128, 255), outline=INK, width=3); draw.ellipse([1816, 950, 1874, 972], fill=PAINT_RED, outline=INK, width=3); draw.line([(1850, 940), (1900, 880)], fill=(150, 110, 70, 255), width=8); draw.line([(1892, 888), (1906, 872)], fill=PAINT_CREAM, width=10)
        potion_belt(img, draw, "paint"); plates(draw, "paint"); ink_frame(img, color=(40, 26, 18, 255), width=14)
    else:
        draw.rectangle(PANEL, fill=CREAM)
        for y in range(y0 + 40, y1, 36): draw.line([(x0 + 40, y), (x1 - 40, y)], fill=(204, 190, 160, 255), width=1)
        draw.line([(x0 + 90, y0), (x0 + 90, y1)], fill=(190, 90, 80, 160), width=2)
        for (sx, sy, sr) in ((1700, 980, 46), (300, 700, 30)): draw.ellipse([sx - sr, sy - sr, sx + sr, sy + sr], outline=(170, 130, 80, 110), width=6)
        rounded(draw, PANEL, 4, None, outline=INKB, width=3)
        crop_slots(src, img); crop_clock(src, img); clock_decor(img, draw, style)
        for x in PARTY_COLS + ENEMY_COLS:
            for i in range(5 if x in PARTY_COLS else 1):
                y = SLOT_Y0 + i * SLOT_GAP; draw.rectangle([x - 3, y - 3, x + SLOT_W + 3, y + SLOT_H + 3], outline=INKB, width=2)
        # quill and ink pot
        draw.ellipse([1790, 940, 1850, 1000], fill=(40, 30, 36, 255), outline=INKB, width=3); draw.line([(1830, 950), (1880, 870)], fill=INKB, width=4); draw.polygon([(1878, 866), (1900, 850), (1886, 880)], fill=(226, 214, 190, 255), outline=INKB)
        clear_header(img, src); draw.rectangle([540, 10, 1380, 74], fill=CREAM, outline=INKB, width=3); draw.rectangle([552, 18, 1368, 66], outline=(170, 60, 50, 200), width=2)
        label(draw, (960, 42), "버려진 광산 · 1층", 30, color=INKB, anchor="mm")
        potion_belt(img, draw, "ledger"); plates(draw, "ledger"); ink_frame(img, color=(60, 40, 28, 255), width=12)
    img.convert("RGB").save(out); print("mock", out.name)

def mock_map_camp(src, out):
    """The camp kit on the node map: planks below, sign above, the map itself on a pinned parchment sheet with ink paths."""
    img = src.copy(); draw = ImageDraw.Draw(img)
    panel = (10, 625, 1910, 1075); tex = Image.open(PLANKS).convert("RGBA").crop((22, 22, 242, 170)); tile(img, panel, tex)
    rounded(draw, panel, 10, None, outline=WOOD_D, width=14); rounded(draw, panel, 10, None, outline=INK, width=4); nails(draw, panel, step=64, r=4, inset=10)
    # the party columns (slots) and the right-side text and buttons come back from the screenshot
    for x in PARTY_COLS:
        for i in range(5): y = SLOT_Y0 + i * SLOT_GAP; img.alpha_composite(src.crop((x, y, x + SLOT_W, y + SLOT_H)), (x, y))
    img.alpha_composite(src.crop((990, 640, 1900, 1060)), (990, 640))
    # the map: a parchment sheet pinned on the dark area, with ink paths and hand-drawn markers
    sheet = (990, 190, 1890, 600); draw.polygon(wobble_poly(sheet, amp=4, step=40, rot=0.004), fill=CREAM, outline=INKB)
    for (px, py) in ((1002, 202), (1878, 206), (1000, 588), (1880, 590)): draw.ellipse([px - 7, py - 7, px + 7, py + 7], fill=(176, 40, 36, 255), outline=INK, width=2)
    nodes = {(0, 0): (1152, 530), (1, 0): (1440, 530), (2, 0): (1728, 530), (0, 1): (1152, 440), (1, 1): (1440, 440), (2, 1): (1728, 440), (0, 2): (1152, 352), (1, 2): (1440, 352), (2, 2): (1728, 352), ("b", 0): (1440, 264)}
    links = [((0, 0), (0, 1)), ((0, 0), (1, 1)), ((1, 0), (1, 1)), ((1, 0), (2, 1)), ((2, 0), (1, 1)), ((2, 0), (2, 1)), ((0, 1), (0, 2)), ((1, 1), (0, 2)), ((1, 1), (2, 2)), ((2, 1), (1, 2)), ((2, 1), (2, 2)), ((0, 2), ("b", 0)), ((1, 2), ("b", 0)), ((2, 2), ("b", 0))]
    for a, b in links:
        (ax, ay), (bx, by) = nodes[a], nodes[b]; n = 9
        for i in range(n):
            t0, t1 = i / n, (i + 0.55) / n; draw.line([(ax + (bx - ax) * t0, ay + (by - ay) * t0), (ax + (bx - ax) * t1, ay + (by - ay) * t1)], fill=INKB, width=3)
    for key, (nx, ny) in nodes.items():
        boss = key[0] == "b"; r = 34 if boss else 26
        fill = (176, 40, 36, 255) if boss else ((214, 170, 70, 255) if key == (0, 0) else ((120, 150, 200, 255) if key in ((1, 0), (2, 0)) else CREAM_D))
        draw.ellipse([nx - r, ny - r, nx + r, ny + r], fill=fill, outline=INKB, width=3)
        if boss:
            draw.ellipse([nx - 12, ny - 14, nx + 12, ny + 8], fill=CREAM, outline=INKB, width=2); draw.rectangle([nx - 8, ny + 4, nx + 8, ny + 14], fill=CREAM, outline=INKB, width=2)
            for dx in (-5, 5): draw.ellipse([nx + dx - 3, ny - 7, nx + dx + 3, ny - 1], fill=INKB)
        else:
            draw.line([(nx - 12, ny + 12), (nx + 12, ny - 12)], fill=INKB, width=4); draw.line([(nx - 12, ny - 12), (nx + 12, ny + 12)], fill=INKB, width=4)
        label(draw, (nx, ny + r + 14), "보스" if boss else "전투", 15, color=INKB, anchor="mm")
    label(draw, (1440, 222), "버려진 광산 — 4층까지", 20, color=INKB, anchor="mm")
    img.alpha_composite(Image.new("RGBA", (1000, 70), (30, 34, 43, 255)), (560, 6)); sign_header(img, draw, "버려진 광산 · 0층 / 4층", size=22, board_width=300); potion_belt(img, draw, "camp")
    # plates of the party side
    values, _ = plate_values()
    for box, (num, name, hp, mx), job in zip([(122 + j * 190, 475, 298 + j * 190, 559) for j in range(4)], values, ("대마법사", "주교", "마검사", "기사")):
        x0, y0, x1, y1 = box; rounded(draw, box, 8, WOOD, outline=INK, width=4); draw.rectangle([x0 + 5, y0 + 5, x1 - 5, y0 + 30], fill=NAVY)
        draw.ellipse([x0 + 12, y0 + 8, x0 + 32, y0 + 28], fill=BRASS, outline=INK, width=2); label(draw, (x0 + 22, y0 + 18), num, 14, color=INK, anchor="mm")
        label(draw, (x0 + 40, y0 + 18), name, 18, anchor="lm"); hp_bar(draw, (x0 + 12, y0 + 38, x1 - 12, y0 + 60), hp, mx); label(draw, (x0 + 12, y0 + 66), job, 13, color=(200, 190, 170, 255))
    ink_frame(img); img.convert("RGB").save(out); print("mock", out.name)

def compare(paths, out, cols=2, w=960):
    ims = [Image.open(p).convert("RGB") for p in paths]; h = round(ims[0].height * w / ims[0].width); gap, lab = 16, 34
    rows = math.ceil(len(ims) / cols); sheet = Image.new("RGB", (cols * w + (cols + 1) * gap, rows * (h + lab) + (rows + 1) * gap), (18, 18, 18)); d = ImageDraw.Draw(sheet)
    for i, (p, im) in enumerate(zip(paths, ims)):
        c, r = i % cols, i // cols; x = gap + c * (w + gap); y = gap + r * (h + lab + gap)
        d.text((x, y), Path(p).stem.replace("mock-", ""), font=font(24), fill=(235, 235, 235)); sheet.paste(im.resize((w, h), Image.LANCZOS), (x, y + lab))
    sheet.save(out); print("compare", out.name, sheet.size)

if __name__ == "__main__":
    battle = Image.open(sys.argv[1]).convert("RGBA"); nodemap = Image.open(sys.argv[2]).convert("RGBA")
    battle.convert("RGB").save(OUT / "mock-now.png")
    mock_battle(battle, "camp", OUT / "mock-A-camp.png"); mock_battle(battle, "paint", OUT / "mock-B-painted.png"); mock_battle(battle, "ledger", OUT / "mock-C-ledger.png")
    mock_map_camp(nodemap, OUT / "mock-A-camp-map.png")
    mock_battle(battle, "camp", OUT / "mock-A1-navy-bag.png", board_style="navy"); mock_battle(battle, "camp", OUT / "mock-A2-paper.png", board_style="paper")
    mock_battle(battle, "camp", OUT / "mock-A3-tool-roll.png", board_style="roll"); mock_battle(battle, "camp", OUT / "mock-A4-felt-table.png", board_style="felt")
    for stale in ("mock-A1-mat.png", "mock-A3-canvas.png"):
        if (OUT / stale).exists(): (OUT / stale).unlink()
    panels = []
    for name in ("mock-A-camp", "mock-A1-navy-bag", "mock-A2-paper", "mock-A3-tool-roll", "mock-A4-felt-table"):
        im = Image.open(OUT / f"{name}.png").convert("RGB").crop((0, 600, 1920, 1080)); panels.append((name, im))
    sheet = Image.new("RGB", (1920 + 32, len(panels) * (480 + 40) + 16), (18, 18, 18)); d = ImageDraw.Draw(sheet); y = 16
    for name, im in panels: d.text((16, y), name.replace("mock-", ""), font=font(24), fill=(235, 235, 235)); sheet.paste(im, (16, y + 32)); y += 520
    sheet.save(OUT / "mock-A-panels.png"); print("panels", sheet.size)
    compare([OUT / "mock-now.png", OUT / "mock-A-camp.png", OUT / "mock-B-painted.png", OUT / "mock-C-ledger.png"], OUT / "mock-compare.png")
