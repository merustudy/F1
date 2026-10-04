"""Mockups of a Diablo-flavoured UI (round 14) on the real battle and map screenshots: dark carved stone and
blackened iron, blood red and aged gold, bone, candles. Two variants: D1 dark iron cells, D2 bone cells (lighter,
so the icons read). Generated samples (mock roster) are composited; the rest is drawn. No API call.
  .venv/bin/python ArtPipeline/Archive/14-ui-diablo/mock_ui_diablo.py <battle.png> <map.png>
"""
import math, random, sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageFilter

ROOT = Path(__file__).resolve().parents[3]; OUT = Path(__file__).resolve().parent; S = OUT / "samples"
FONT = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"
ITEM_DIR = ROOT / "Assets/@Art/Item"; POTION_DIR = ROOT / "Assets/@Art/Potion"
random.seed(14)

INK = (16, 10, 10, 255); STONE = (58, 56, 60, 255); STONE_D = (34, 32, 36, 255); STONE_L = (84, 82, 88, 255)
IRON = (46, 44, 50, 255); IRON_L = (96, 94, 100, 255); GOLD = (186, 146, 60, 255); GOLD_D = (120, 92, 34, 255)
BLOOD = (138, 22, 24, 255); BLOOD_L = (190, 44, 40, 255); BONE = (214, 200, 170, 255); BONE_D = (160, 146, 118, 255)
NAVY_IRON = (36, 44, 62, 255); RED_IRON = (74, 30, 30, 255); FLAME = (255, 196, 90, 255)

def font(size): return ImageFont.truetype(str(FONT), size)
def label(d, xy, s, size, color=BONE, anchor="la"): d.text(xy, s, font=font(size), fill=color, anchor=anchor)
def rounded(d, box, r, fill, outline=None, width=0): d.rounded_rectangle(box, radius=r, fill=fill, outline=outline, width=width)

def tile(base, box, tex):
    x0, y0, x1, y1 = box; w, h = tex.size
    for y in range(y0, y1, h):
        for x in range(x0, x1, w):
            base.alpha_composite(tex.crop((0, 0, min(w, x1 - x), min(h, y1 - y))), (x, y))

def stone_band(d, box, arches=True):
    """The carved band of the stone panel: a raised strip with a row of small pointed arches and iron bosses at the corners."""
    x0, y0, x1, y1 = box
    d.rectangle(box, fill=STONE_D, outline=INK, width=3)
    if arches:
        step = 36; h = y1 - y0
        for x in range(x0 + 18, x1 - 18, step):
            cx = x + step / 2; d.polygon([(x + 6, y1 - 4), (x + 6, y0 + h * 0.45), (cx, y0 + 4), (x + step - 6, y0 + h * 0.45), (x + step - 6, y1 - 4)], outline=STONE_L, width=2)
    for px, py in ((x0 + 14, (y0 + y1) // 2), (x1 - 14, (y0 + y1) // 2)):
        d.ellipse([px - 9, py - 9, px + 9, py + 9], fill=IRON, outline=INK, width=2); d.ellipse([px - 4, py - 6, px + 4, py - 2], fill=INK)

def nine(piece, size, border):
    """Stretches a generated frame to a size as a nine-slice (the corners keep their shape)."""
    w, h = size; pw, ph = piece.size; b = border; out = Image.new("RGBA", size, (0, 0, 0, 0))
    cols = [(0, b, 0, b), (b, pw - b, b, w - b), (pw - b, pw, w - b, w)]; rows = [(0, b, 0, b), (b, ph - b, b, h - b), (ph - b, ph, h - b, h)]
    for sx0, sx1, dx0, dx1 in cols:
        for sy0, sy1, dy0, dy1 in rows:
            if dx1 <= dx0 or dy1 <= dy0: continue
            out.paste(piece.crop((sx0, sy0, sx1, sy1)).resize((dx1 - dx0, dy1 - dy0), Image.LANCZOS), (dx0, dy0))
    return out

def tinted(piece, target, keep_dark=40):
    """Recolors a flat iron piece's fill (not its dark outline or gold line) toward a target: a cheap stand-in for tint_fill."""
    out = piece.copy(); px = out.load()
    for y in range(out.height):
        for x in range(out.width):
            r, g, b, a = px[x, y]
            if a == 0: continue
            lum = (r + g + b) / 3
            if lum < keep_dark or (r > g + 30 and r > b + 30) or (r > 140 and g > 110 and b < 90): continue  # outline, red, gold stay
            k = lum / 70.0; px[x, y] = (min(255, int(target[0] * k)), min(255, int(target[1] * k)), min(255, int(target[2] * k)), a)
    return out

def candle(img, d, x, y, h=56):
    glow = Image.new("RGBA", img.size, (0, 0, 0, 0)); ImageDraw.Draw(glow).ellipse([x - 90, y - 110, x + 90, y + 50], fill=(255, 170, 70, 70)); img.alpha_composite(glow.filter(ImageFilter.GaussianBlur(26)))
    d = ImageDraw.Draw(img)
    rounded(d, (x - 11, y - h, x + 11, y), 4, (226, 214, 190, 255), outline=INK, width=3)
    for dx, dy in ((-9, 10), (6, 18), (-3, 30)): d.ellipse([x + dx - 5, y - h + dy - 4, x + dx + 5, y - h + dy + 8], fill=(236, 226, 206, 255), outline=INK, width=2)
    d.line([(x, y - h), (x, y - h - 7)], fill=INK, width=3)
    d.polygon([(x - 7, y - h - 8), (x, y - h - 30), (x + 7, y - h - 8)], fill=FLAME, outline=(200, 110, 30, 255)); d.polygon([(x - 3, y - h - 9), (x, y - h - 20), (x + 3, y - h - 9)], fill=(255, 240, 200, 255))
    d.ellipse([x - 20, y - 6, x + 20, y + 8], fill=IRON, outline=INK, width=3)

def skull(d, cx, cy, r=14):
    d.ellipse([cx - r, cy - r, cx + r, cy + r * 0.8], fill=BONE, outline=INK, width=3); d.rectangle([cx - r * 0.55, cy + r * 0.3, cx + r * 0.55, cy + r * 1.05], fill=BONE, outline=INK, width=3)
    for dx in (-r * 0.4, r * 0.4): d.ellipse([cx + dx - r * 0.28, cy - r * 0.25, cx + dx + r * 0.28, cy + r * 0.25], fill=INK)
    for dx in (-r * 0.3, 0, r * 0.3): d.line([(cx + dx, cy + r * 0.55), (cx + dx, cy + r * 0.95)], fill=INK, width=2)

def chain(d, x, y0, y1):
    for i, y in enumerate(range(y0, y1, 16)):
        a = 7 if i % 2 == 0 else 4; d.ellipse([x - a, y, x + a, y + 18], outline=IRON_L, width=4); d.ellipse([x - a, y, x + a, y + 18], outline=INK, width=1)

def vignette(img, strength=120):
    v = Image.new("RGBA", img.size, (0, 0, 0, 0)); dv = ImageDraw.Draw(v); w, h = img.size
    dv.rectangle([0, 0, w, h], fill=(0, 0, 0, strength)); dv.ellipse([-w * 0.1, -h * 0.25, w * 1.1, h * 1.25], fill=(0, 0, 0, 0))
    img.alpha_composite(v.filter(ImageFilter.GaussianBlur(90)))

# --- screen geometry (1920x1080) ---
PANEL = (10, 625, 1910, 1075); PARTY_COLS = [120 + j * 190 for j in range(4)]; ENEMY_COLS = [1050, 1240]
SLOT_Y0, SLOT_H, SLOT_GAP, SLOT_W = 636, 50, 54, 180
PARTY_PLATES = [(122 + j * 190, 520, 298 + j * 190, 604) for j in range(4)]; ENEMY_PLATES = [(1432, 520, 1608, 604), (1624, 520, 1800, 604)]
PLATE_PAD = 7
BACKGROUND = ROOT / "Assets/@Art/Background/Dungeon/abandoned_mine.png"

def background_patch(img, box):
    """Covers a box of the stage with the dungeon background as the battle screen draws it: 1920x1280 placed at y -224 (BattleFieldTop + floor - 57% of the height)."""
    bg = Image.open(BACKGROUND).convert("RGBA").resize((1920, 1280), Image.LANCZOS); x0, y0, x1, y1 = box
    img.alpha_composite(bg.crop((x0, y0 + 224, x1, y1 + 224)), (x0, y0))
COLUMN_ITEMS = {120: "fire_staff", 310: "healing_staff", 500: "sword", 690: "longsword", 1050: "rat_bite", 1240: "rat_bite"}
PLATE_VALUES = ([("4", "미라", 80, 80), ("3", "엘라", 90, 90), ("2", "카이", 100, 100), ("1", "로언", 120, 140)], [("1", "동굴 쥐", 14, 40), ("2", "동굴 쥐", 14, 40)])

def header(img, d, src, title, sign_box=(740, 84, 1180, 160), patch_from=None, captions=True, buttons=True):
    # the old sign hangs below the header: cover it with the dungeon background (battle) or the plain dark panel beside it (map)
    x0, y0, x1, y1 = sign_box
    if patch_from is None: background_patch(img, sign_box)
    else: img.alpha_composite(src.crop((patch_from, y0, patch_from + (x1 - x0), y1)), (x0, y0))
    tex = Image.open(S / "stone_panel.png").convert("RGBA").crop((30, 30, 234, 160)); tile(img, (0, 0, 1920, 84), tex)
    stone_band(d, (0, 72, 1920, 90), arches=False); d.rectangle([0, 0, 1920, 6], fill=STONE_D)
    if buttons:
        for box in ((36, 14, 344, 70), (1526, 14, 1644, 70), (1646, 14, 1912, 70)): img.alpha_composite(src.crop(box), (box[0], box[1]))
    if captions: img.alpha_composite(src.crop((366, 12, 820, 72)), (366, 12))  # the battle's captions
    plate = nine(Image.open(S / "gothic_plate.png").convert("RGBA"), (330, 76), 44); img.alpha_composite(plate, (960 - 165, 4)); d = ImageDraw.Draw(img)
    label(d, (960, 42), title, 26, color=GOLD, anchor="mm")

def potion_belt(img, d):
    rounded(d, (30, 92, 262, 176), 8, IRON, outline=INK, width=4); d.rectangle([30, 100, 262, 104], fill=IRON_L); d.rectangle([30, 164, 262, 168], fill=IRON_L)
    for i, name in enumerate(("healing_potion", "healing_potion", None)):
        x = 42 + i * 72; rounded(d, (x, 102, x + 64, 166), 5, (24, 20, 22, 255), outline=IRON_L, width=4)
        for px, py in ((x + 6, 108), (x + 58, 108), (x + 6, 160), (x + 58, 160)): d.ellipse([px - 3, py - 3, px + 3, py + 3], fill=IRON_L, outline=INK)
        if name: img.alpha_composite(Image.open(POTION_DIR / f"{name}.png").convert("RGBA").resize((50, 50), Image.LANCZOS), (x + 7, 109))
        if i == 0: rounded(d, (x, 102, x + 64, 166), 5, None, outline=GOLD, width=3)
    rounded(d, (282, 110, 660, 158), 6, (20, 16, 18, 235), outline=GOLD_D, width=2); label(d, (300, 134), "치유 포션 · HP 50 회복: 쓸 아군을 누르세요", 19, color=BONE, anchor="lm")

def plates(img, d):
    base = Image.open(S / "gothic_plate.png").convert("RGBA")
    for boxes, values, color in ((PARTY_PLATES, PLATE_VALUES[0], NAVY_IRON), (ENEMY_PLATES, PLATE_VALUES[1], RED_IRON)):
        piece = nine(tinted(base, color), (176 + 2 * PLATE_PAD, 84 + 2 * PLATE_PAD), 44)
        for (x0, y0, x1, y1), (num, name, hp, mx) in zip(boxes, values):
            img.alpha_composite(piece, (x0 - PLATE_PAD, y0 - PLATE_PAD)); d = ImageDraw.Draw(img)
            d.ellipse([x0 + 10, y0 + 8, x0 + 32, y0 + 30], fill=INK, outline=GOLD, width=2); label(d, (x0 + 21, y0 + 19), num, 13, color=GOLD, anchor="mm")
            label(d, (x0 + 40, y0 + 19), name, 18, color=GOLD, anchor="lm")
            rounded(d, (x0 + 12, y0 + 40, x1 - 12, y0 + 66), 4, (26, 10, 10, 255), outline=GOLD_D, width=2)
            w = int((x1 - x0 - 30) * hp / mx); rounded(d, (x0 + 15, y0 + 43, x0 + 15 + w, y0 + 63), 3, BLOOD); d.rectangle([x0 + 15, y0 + 43, x0 + 15 + w, y0 + 48], fill=BLOOD_L)
            label(d, ((x0 + x1) // 2, y0 + 53), f"{hp}/{mx}", 15, color=BONE, anchor="mm")

def cells(img, d, src, variant):
    piece = Image.open(S / ("bone_slot.png" if variant == "bone" else "iron_slot.png")).convert("RGBA"); cell = nine(piece, (180, 50), 30)
    for x in PARTY_COLS + ENEMY_COLS:
        n = 5 if x in PARTY_COLS else 1
        rounded(d, (x - 6, SLOT_Y0 - 6, x + SLOT_W + 6, SLOT_Y0 + n * SLOT_GAP - SLOT_GAP + SLOT_H + 6), 6, IRON, outline=INK, width=3)
        for i in range(n):
            y = SLOT_Y0 + i * SLOT_GAP; img.alpha_composite(cell, (x, y))
            if i == 0:
                dd = ImageDraw.Draw(img)
                if x == 120: rounded(dd, (x + 8, y + 7, x + int(SLOT_W * 0.55), y + SLOT_H - 7), 3, (120, 24, 24, 220) if variant != "bone" else (150, 60, 50, 200))
                icon = Image.open(ITEM_DIR / f"{COLUMN_ITEMS[x]}.png").convert("RGBA").resize((164, 40), Image.LANCZOS)
                if x in ENEMY_COLS: icon = icon.transpose(Image.FLIP_LEFT_RIGHT)
                img.alpha_composite(icon, (x + 8, y + 5))

def storm_orb(img, d, fill=0.35, time="7.3초", words="폭풍까지 37.7초"):
    orb = Image.open(S / "orb_cradle.png").convert("RGBA").resize((240, 240), Image.LANCZOS); cx, cy = 960, 740
    # the blood inside the sphere, under the glass: a clipped red disc up to the level
    liquid = Image.new("RGBA", img.size, (0, 0, 0, 0)); dl = ImageDraw.Draw(liquid); r = 78
    dl.ellipse([cx - r, cy - r, cx + r, cy + r], fill=(150, 20, 22, 230)); top = cy + r - int(2 * r * fill)
    dl.rectangle([cx - r - 2, cy - r - 2, cx + r + 2, top], fill=(0, 0, 0, 0)); dl.ellipse([cx - r, top - 6, cx + r, top + 6], fill=(190, 44, 40, 230))
    sphere = Image.new("L", img.size, 0); ImageDraw.Draw(sphere).ellipse([cx - r, cy - r, cx + r, cy + r], fill=255); liquid.putalpha(Image.composite(liquid.getchannel("A"), Image.new("L", img.size, 0), sphere))
    glass = Image.new("RGBA", img.size, (0, 0, 0, 0)); ImageDraw.Draw(glass).ellipse([cx - r, cy - r, cx + r, cy + r], fill=(40, 30, 40, 150)); img.alpha_composite(glass); img.alpha_composite(liquid)
    img.alpha_composite(orb, (cx - 120, cy - 120)); d = ImageDraw.Draw(img)
    label(d, (cx, cy - 6), time, 30, color=BONE, anchor="mm"); label(d, (cx, cy + 128), words, 17, color=BONE_D, anchor="mm")

def board_panel(img, d, src, variant):
    tex = Image.open(S / "stone_panel.png").convert("RGBA").crop((30, 30, 234, 160)); tile(img, PANEL, tex)
    stone_band(d, (10, 625, 1910, 652)); stone_band(d, (10, 1050, 1910, 1075), arches=False); d.rectangle([10, 625, 1910, 1075], outline=INK, width=4)
    cells(img, d, src, variant); storm_orb(img, d)
    d = ImageDraw.Draw(img); chain(d, 60, 656, 760); chain(d, 1860, 656, 760)
    candle(img, d, 70, 1000); candle(img, d, 1850, 1000, h=44); d = ImageDraw.Draw(img); skull(d, 1120, 900, 16); skull(d, 1156, 912, 11)

def mock_battle(src, variant, out):
    img = src.copy(); d = ImageDraw.Draw(img)
    header(img, d, src, "버려진 광산 · 1층"); d = ImageDraw.Draw(img); potion_belt(img, d); plates(img, d); d = ImageDraw.Draw(img)
    board_panel(img, d, src, variant); vignette(img); img.convert("RGB").save(out); print("mock", out.name)

def mock_map(src, variant, out):
    img = src.copy(); d = ImageDraw.Draw(img)
    header(img, d, src, "버려진 광산 · 0층 / 4층", sign_box=(420, 84, 860, 160), patch_from=0, captions=False, buttons=False); d = ImageDraw.Draw(img)
    # the potion belt above the right half, the map as a dark stone tablet with carved gold paths and rune nodes
    rounded(d, (980, 92, 1212, 176), 8, IRON, outline=INK, width=4)
    for i, name in enumerate(("healing_potion", "healing_potion", None)):
        x = 992 + i * 72; rounded(d, (x, 102, x + 64, 166), 5, (24, 20, 22, 255), outline=IRON_L, width=4)
        if name: img.alpha_composite(Image.open(POTION_DIR / f"{name}.png").convert("RGBA").resize((50, 50), Image.LANCZOS), (x + 7, 109))
    d = ImageDraw.Draw(img)
    tex = Image.open(S / "stone_panel.png").convert("RGBA").crop((30, 30, 234, 160)); tile(img, (980, 190, 1900, 604), tex); d = ImageDraw.Draw(img)
    stone_band(d, (980, 190, 1900, 214), arches=False); stone_band(d, (980, 580, 1900, 604), arches=False); d.rectangle([980, 190, 1900, 604], outline=INK, width=4)
    nodes = {(0, 0): (1152, 530), (1, 0): (1440, 530), (2, 0): (1728, 530), (0, 1): (1152, 440), (1, 1): (1440, 440), (2, 1): (1728, 440), (0, 2): (1152, 352), (1, 2): (1440, 352), (2, 2): (1728, 352), ("b", 0): (1440, 264)}
    links = [((0, 0), (0, 1)), ((0, 0), (1, 1)), ((1, 0), (1, 1)), ((1, 0), (2, 1)), ((2, 0), (1, 1)), ((2, 0), (2, 1)), ((0, 1), (0, 2)), ((1, 1), (0, 2)), ((1, 1), (2, 2)), ((2, 1), (1, 2)), ((2, 1), (2, 2)), ((0, 2), ("b", 0)), ((1, 2), ("b", 0)), ((2, 2), ("b", 0))]
    for a, b in links: d.line([nodes[a], nodes[b]], fill=GOLD_D, width=5); d.line([nodes[a], nodes[b]], fill=GOLD, width=2)
    for key, (nx, ny) in nodes.items():
        boss = key[0] == "b"; r = 36 if boss else 27
        fill = BLOOD if boss else ((GOLD if key == (0, 0) else ((60, 72, 110, 255) if key in ((1, 0), (2, 0)) else STONE_L)))
        d.ellipse([nx - r - 4, ny - r - 4, nx + r + 4, ny + r + 4], fill=INK); d.ellipse([nx - r, ny - r, nx + r, ny + r], fill=fill, outline=GOLD if not boss else BONE, width=3)
        if boss: skull(d, nx, ny - 4, 15)
        else: d.line([(nx - 11, ny + 11), (nx + 11, ny - 11)], fill=BONE, width=4); d.line([(nx - 11, ny - 11), (nx + 11, ny + 11)], fill=BONE, width=4)
        label(d, (nx, ny + r + 16), "보스" if boss else "전투", 15, color=BONE, anchor="mm")
    # plates and boards of the party side, the panel's right half on a stone tablet
    base = Image.open(S / "gothic_plate.png").convert("RGBA"); piece = nine(tinted(base, NAVY_IRON), (176 + 2 * PLATE_PAD, 84 + 2 * PLATE_PAD), 44)
    for (x0, y0, x1, y1), (num, name, hp, mx), job in zip([(122 + j * 190, 475, 298 + j * 190, 559) for j in range(4)], PLATE_VALUES[0], ("대마법사", "주교", "마검사", "기사")):
        img.alpha_composite(piece, (x0 - PLATE_PAD, y0 - PLATE_PAD)); d = ImageDraw.Draw(img); d.ellipse([x0 + 10, y0 + 8, x0 + 32, y0 + 30], fill=INK, outline=GOLD, width=2); label(d, (x0 + 21, y0 + 19), num, 13, color=GOLD, anchor="mm")
        label(d, (x0 + 40, y0 + 19), name, 18, color=GOLD, anchor="lm"); rounded(d, (x0 + 12, y0 + 38, x1 - 12, y0 + 60), 4, (26, 10, 10, 255), outline=GOLD_D, width=2)
        w = int((x1 - x0 - 30) * hp / mx); rounded(d, (x0 + 15, y0 + 41, x0 + 15 + w, y0 + 57), 3, BLOOD); label(d, ((x0 + x1) // 2, y0 + 49), f"{hp}/{mx}", 14, color=BONE, anchor="mm"); label(d, (x0 + 12, y0 + 66), job, 12, color=BONE_D)
    tile(img, PANEL, tex); d = ImageDraw.Draw(img); stone_band(d, (10, 625, 1910, 652)); stone_band(d, (10, 1050, 1910, 1075), arches=False); d.rectangle([10, 625, 1910, 1075], outline=INK, width=4)
    cell = nine(Image.open(S / ("bone_slot.png" if variant == "bone" else "iron_slot.png")).convert("RGBA"), (180, 50), 30)
    for x in PARTY_COLS:
        rounded(d, (x - 6, SLOT_Y0 - 6, x + SLOT_W + 6, SLOT_Y0 + 4 * SLOT_GAP + SLOT_H + 6), 6, IRON, outline=INK, width=3)
        for i in range(5):
            y = SLOT_Y0 + i * SLOT_GAP; img.alpha_composite(cell, (x, y)); dd = ImageDraw.Draw(img)
            if i == 0:
                icon = Image.open(ITEM_DIR / f"{COLUMN_ITEMS[x]}.png").convert("RGBA").resize((164, 40), Image.LANCZOS); img.alpha_composite(icon, (x + 8, y + 5))
                dd.ellipse([x + 4, y + SLOT_H - 26, x + 26, y + SLOT_H - 4], fill=INK, outline=GOLD, width=2); label(dd, (x + 15, y + SLOT_H - 15), "10", 11, color=GOLD, anchor="mm")
            else: label(dd, (x + 14, y + 25), "빈 칸", 15, color=BONE_D if variant != "bone" else (120, 100, 80, 255), anchor="lm")
    d = ImageDraw.Draw(img); rounded(d, (980, 637, 1900, 1063), 8, (22, 18, 20, 240), outline=GOLD_D, width=3)
    label(d, (1000, 672), "1층 · 전투", 34, color=GOLD, anchor="lm"); label(d, (1000, 708), "누가 기다리는지는 들어가 봐야 압니다.", 20, color=BONE_D, anchor="lm")
    for x, w, text, main in ((1000, 200, "인벤토리로", False), (1220, 236, "인벤토리 보기", False), (1644, 236, "전투 시작", True)):
        rounded(d, (x, 883, x + w, 959), 6, BLOOD if main else IRON, outline=GOLD if main else IRON_L, width=3); label(d, (x + w / 2, 921), text, 24, color=BONE, anchor="mm")
    candle(img, d, 70, 1000); d = ImageDraw.Draw(img); chain(d, 60, 656, 760); vignette(img); img.convert("RGB").save(out); print("mock", out.name)

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
    mock_battle(battle, "iron", OUT / "mock-D1-iron.png"); mock_battle(battle, "bone", OUT / "mock-D2-bone.png"); mock_map(nodemap, "bone", OUT / "mock-D2-bone-map.png")
    compare([OUT / "mock-now.png", OUT / "mock-D1-iron.png", OUT / "mock-D2-bone.png", OUT / "mock-D2-bone-map.png"], OUT / "mock-compare.png")
