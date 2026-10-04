"""Simpler item cells for the Diablo concept (round 14): each unit's cells sit on one inventory panel, the cells
plain like an inventory grid. Three variants on the D2 + S4 (candle, no side candles) base. Drawn, no API call.
  .venv/bin/python ArtPipeline/Archive/14-ui-diablo/mock_slots.py <battle.png>
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent; sys.path.insert(0, str(HERE))
import mock_ui_diablo as D  # noqa: E402
import mock_clock as C  # noqa: E402

PANEL_PAD = 12
CELL_DARK = (30, 28, 32, 255); CELL_DARK_LINE = (92, 90, 98, 255); CELL_BONE_LINE = (72, 60, 50, 255); CELL_LINE_ONLY = (116, 114, 122, 255)

def base_screen(src):
    """D2 without its cells, blood orb or side candles: the header, belt, plates and a bare stone panel with chains and skulls."""
    img = src.copy(); d = ImageDraw.Draw(img)
    D.header(img, d, src, "버려진 광산 · 1층"); d = ImageDraw.Draw(img); D.potion_belt(img, d); D.plates(img, d); d = ImageDraw.Draw(img)
    tex = Image.open(D.S / "stone_panel.png").convert("RGBA").crop((30, 30, 234, 160)); D.tile(img, D.PANEL, tex)
    D.stone_band(d, (10, 625, 1910, 652)); D.stone_band(d, (10, 1050, 1910, 1075), arches=False); d.rectangle([10, 625, 1910, 1075], outline=D.INK, width=4)
    D.chain(d, 60, 656, 760); D.chain(d, 1860, 656, 760); D.skull(d, 1540, 985, 16); D.skull(d, 1576, 997, 11)
    return img

def boards(img, variant):
    d = ImageDraw.Draw(img)
    for x in D.PARTY_COLS + D.ENEMY_COLS:
        n = 5 if x in D.PARTY_COLS else 1; y1 = D.SLOT_Y0 + (n - 1) * D.SLOT_GAP + D.SLOT_H
        box = (x - PANEL_PAD, D.SLOT_Y0 - PANEL_PAD, x + D.SLOT_W + PANEL_PAD, y1 + PANEL_PAD)
        # one inventory panel per unit: blackened iron with a thin line of old gold inside its edge
        D.rounded(d, box, 6, C.IRON_D, outline=D.INK, width=4); D.rounded(d, (box[0] + 5, box[1] + 5, box[2] - 5, box[3] - 5), 4, None, outline=D.GOLD_D, width=2)
        for i in range(n):
            y = D.SLOT_Y0 + i * D.SLOT_GAP; cell = (x, y, x + D.SLOT_W, y + D.SLOT_H)
            if variant == "dark": d.rectangle(cell, fill=CELL_DARK, outline=CELL_DARK_LINE, width=2)
            elif variant == "bone": d.rectangle(cell, fill=D.BONE, outline=CELL_BONE_LINE, width=2)
            else: d.rectangle(cell, outline=CELL_LINE_ONLY, width=2)
            if i == 0:
                if x == 120: D.rounded(d, (x + 3, y + 3, x + int(D.SLOT_W * 0.55), y + D.SLOT_H - 3), 2, (150, 60, 50, 190) if variant == "bone" else (120, 24, 24, 210))
                icon = Image.open(D.ITEM_DIR / f"{D.COLUMN_ITEMS[x]}.png").convert("RGBA").resize((164, 40), Image.LANCZOS)
                if x in D.ENEMY_COLS: icon = icon.transpose(Image.FLIP_LEFT_RIGHT)
                img.alpha_composite(icon, (x + 8, y + 5)); d = ImageDraw.Draw(img)

def main():
    src = Image.open(sys.argv[1]).convert("RGBA"); base = base_screen(src)
    crops = [("D2 (지금: 리벳 칸)", Image.open(HERE / "mock-D2-bone.png").convert("RGB").crop((100, 620, 880, 920)))]
    for key, name in (("dark", "I1 어두운 칸"), ("bone", "I2 뼈색 칸"), ("line", "I3 선만")):
        # the candle first (its patch of plain stone would otherwise cover the boards beside it), then the boards over its glow
        img = base.copy(); C.s4_candle(img, base); boards(img, key); D.vignette(img)
        img.convert("RGB").save(HERE / f"mock-{key.replace('dark', 'I1-dark').replace('bone', 'I2-bone').replace('line', 'I3-line')}.png")
        crops.append((name, img.convert("RGB").crop((100, 620, 880, 920))))
    gap, lab = 20, 40; w, h = crops[0][1].size; sheet = Image.new("RGB", (len(crops) * (w + gap) + gap, h + lab + 2 * gap), (18, 18, 18)); d = ImageDraw.Draw(sheet); x = gap
    for name, im in crops: d.text((x, gap), name, font=C.font(26), fill=(235, 235, 235)); sheet.paste(im, (x, gap + lab)); x += w + gap
    sheet.save(HERE / "mock-slots-compare.png"); print("slots compare", sheet.size)

if __name__ == "__main__": main()
