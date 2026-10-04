"""Seven cells at most, each a little taller, the board panel as high as now (round 15). Drawn over the current battle
screenshot with the game's own sprites (slot, bag) and item icons, so the only thing that changes between the sheets
is the geometry. No API call.
  .venv/bin/python ArtPipeline/Archive/15-seven-cells/mock_seven_cells.py <battle.png>
Row 1 of the party (the right-most party column) is drawn with the most cells a board can have, to show that the
most cells still fit the panel; the other party columns show the 5 cells a mercenary has now. A 2-cell longbow and
a 3-cell halberd stand in two columns to show the tall shapes.
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[3]; HERE = Path(__file__).resolve().parent
FRAME = ROOT / "Assets/@Art/UI/Frame"; ICON = ROOT / "Assets/@Art/UI/Icon"; ITEM = ROOT / "Assets/@Art/Item"
FONT = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"

PANEL_TOP, PANEL_H = 620, 460                 # UiPrefabSetup.Battle: BoardPanelTop, BoardPanelHeight
CELL_W = 180                                   # BattleItemView.CellWidth (= a stage column)
BAG_PAD_X, BAG_PAD_Y = 4, 10                   # UiPrefabSetup.Kit: BoardBagPadX/Y
ICON_MARGIN_X, ICON_MARGIN_Y = 8, 5            # ItemIconMarginX/Y
FILL_INSET = 7                                 # KitBar inset of the item cell
EMPTY_ALPHA = 0.8                              # EmptyCellAlpha
GAUGE = (0x8C, 0x3A, 0x2C, int(0.84 * 255))    # UiPalette.Gauge at CooldownAlpha
VIGNETTE_ALPHA = 0.35                          # ScreenVignetteAlpha
SLOT_BORDER, BAG_BORDER = 16, 40               # UiArt Piece borders (sprite pixels, 2x)
PARTY_X = [120, 310, 500, 690]                 # cells' x of party rows 4,3,2,1 (field left 120 + (180+10)k)
ENEMY_X = [1050, 1240]                         # enemy rows 1,2
CLEAN_X = 1430                                 # an empty enemy column of the screenshot: the stone to erase with

class Geo:
    def __init__(self, key, label, max_cells, cell_h, gap, col_top):
        self.key, self.label, self.max_cells, self.cell_h, self.gap, self.col_top = key, label, max_cells, cell_h, gap, col_top
    def board_h(self, cells): return cells * self.cell_h + (cells - 1) * self.gap
    @property
    def fits(self): return self.board_h(self.max_cells) + 2 * self.col_top <= PANEL_H

NOW = Geo("now", "지금: 최대 8칸, 칸 180x50, 간격 4", 8, 50, 4, 16)
A = Geo("A", "A안: 최대 7칸, 칸 180x58, 간격 4", 7, 58, 4, 15)
B = Geo("B", "B안: 최대 7칸, 칸 180x60, 간격 2", 7, 60, 2, 14)
C = Geo("C", "C안: 최대 7칸, 칸 180x56, 간격 6", 7, 56, 6, 16)

# (item, cells, charge) per column; None fills the rest with empty cells. Row 1 gets the most cells (see the docstring).
PARTY = [[("healing_staff", 1, 0.25)], [("mace", 1, 0.6), ("longbow", 2, 0.4)], [("sword", 1, 0.8), ("halberd", 3, 0.15)], [("longsword", 1, 0.5)]]
ENEMY = [[("rusty_blade", 1, 0.7)], [("rusty_blade", 1, 0.3)]]

def font(size): return ImageFont.truetype(str(FONT), size)

def nine(piece, size, border):
    """Stretches a sprite to a size as a nine-slice (the corners keep their shape), like Image.Type.Sliced."""
    w, h = size; pw, ph = piece.size; b = border; out = Image.new("RGBA", size, (0, 0, 0, 0))
    cols = [(0, b, 0, b), (b, pw - b, b, w - b), (pw - b, pw, w - b, w)]; rows = [(0, b, 0, b), (b, ph - b, b, h - b), (ph - b, ph, h - b, h)]
    for sx0, sx1, dx0, dx1 in cols:
        for sy0, sy1, dy0, dy1 in rows:
            if dx1 <= dx0 or dy1 <= dy0: continue
            out.paste(piece.crop((sx0, sy0, sx1, sy1)).resize((dx1 - dx0, dy1 - dy0), Image.LANCZOS), (dx0, dy0))
    return out

def frame(sprite, border, w, h):
    """A kit frame at screen size: the sprite is 2x, so slice at 2x and bring it down."""
    return nine(sprite, (2 * w, 2 * h), border).resize((w, h), Image.LANCZOS)

SLOT = Image.open(FRAME / "slot.png").convert("RGBA"); BAG = Image.open(FRAME / "bag.png").convert("RGBA")
VIGNETTE = Image.open(ICON / "vignette.png").convert("RGBA").resize((1920, 1080), Image.LANCZOS)
_v = VIGNETTE.split()[3].point(lambda a: int(a * VIGNETTE_ALPHA)); VIGNETTE = Image.merge("RGBA", (Image.new("L", VIGNETTE.size, 0),) * 3 + (_v,))

def with_alpha(im, k):
    a = im.split()[3].point(lambda v: int(v * k)); out = im.copy(); out.putalpha(a); return out

def item_cell(g, item, cells, charge, mirrored):
    """A cell block of an item: the slot as the gauge (fills from the left inside the rim), the icon over it."""
    h = g.board_h(cells); block = frame(SLOT, SLOT_BORDER, CELL_W, h)
    fill = Image.new("RGBA", block.size, (0, 0, 0, 0)); d = ImageDraw.Draw(fill)
    d.rectangle([FILL_INSET, FILL_INSET, FILL_INSET + round(charge * (CELL_W - 2 * FILL_INSET)), h - FILL_INSET - 1], fill=GAUGE)
    block.alpha_composite(fill)
    icon = Image.open(ITEM / f"{item}.png").convert("RGBA")
    bw, bh = CELL_W - 2 * ICON_MARGIN_X, h - 2 * ICON_MARGIN_Y; k = min(bw / icon.width, bh / icon.height)
    icon = icon.resize((max(1, round(icon.width * k)), max(1, round(icon.height * k))), Image.LANCZOS)
    if mirrored: icon = icon.transpose(Image.FLIP_LEFT_RIGHT)
    block.alpha_composite(icon, ((CELL_W - icon.width) // 2, (h - icon.height) // 2))
    return block

def board(img, g, x, items, cells, mirrored=False):
    """One unit's board: the bag behind, then the item blocks and the empty cells stacked from the column's top."""
    top = PANEL_TOP + g.col_top; bh = g.board_h(cells)
    img.alpha_composite(frame(BAG, BAG_BORDER, CELL_W + 2 * BAG_PAD_X, bh + 2 * BAG_PAD_Y), (x - BAG_PAD_X, top - BAG_PAD_Y))
    y = top; used = 0
    for item, n, charge in items:
        img.alpha_composite(item_cell(g, item, n, charge, mirrored), (x, y)); y += g.board_h(n) + g.gap; used += n
    empty = with_alpha(frame(SLOT, SLOT_BORDER, CELL_W, g.cell_h), EMPTY_ALPHA)
    for _ in range(cells - used):
        img.alpha_composite(empty, (x, y)); y += g.cell_h + g.gap
    box = (x - BAG_PAD_X, top - BAG_PAD_Y, x + CELL_W + BAG_PAD_X, top + bh + BAG_PAD_Y)
    img.alpha_composite(VIGNETTE.crop(box), box[:2])   # the screen vignette lies over everything in the game

def erase(img, src, x, y0, y1):
    """Puts clean stone (from an empty column at the same height) over an old board. The new bag covers it again."""
    img.paste(src.crop((CLEAN_X - BAG_PAD_X - 2, y0, CLEAN_X + CELL_W + BAG_PAD_X + 2, y1)), (x - BAG_PAD_X - 2, y0))

def render(src, g):
    img = src.copy()
    for x in PARTY_X: erase(img, src, x, PANEL_TOP + NOW.col_top - BAG_PAD_Y, PANEL_TOP + NOW.col_top + NOW.board_h(5) + BAG_PAD_Y + 1)
    for x in ENEMY_X: erase(img, src, x, PANEL_TOP + NOW.col_top - BAG_PAD_Y, PANEL_TOP + NOW.col_top + NOW.board_h(1) + BAG_PAD_Y + 1)
    for i, x in enumerate(PARTY_X):
        board(img, g, x, PARTY[i], g.max_cells if i == len(PARTY_X) - 1 else 5)
    for i, x in enumerate(ENEMY_X):
        board(img, g, x, ENEMY[i], sum(n for _, n, _ in ENEMY[i]), mirrored=True)
    return img

def sheet(entries, cols, path, lab=44, gap=24, size=28, footer=None):
    w, h = entries[0][1].size; rows = (len(entries) + cols - 1) // cols
    fh = 50 if footer else 0
    out = Image.new("RGB", (cols * (w + gap) + gap, rows * (h + lab + gap) + gap + fh), (18, 18, 18)); d = ImageDraw.Draw(out)
    for i, (name, im) in enumerate(entries):
        x = gap + (i % cols) * (w + gap); y = gap + (i // cols) * (h + lab + gap)
        d.text((x, y), name, font=font(size), fill=(235, 235, 235)); out.paste(im, (x, y + lab))
    if footer: d.text((gap, out.height - fh + 10), footer, font=font(22), fill=(190, 190, 190))
    out.save(path); print(path.name, out.size)

def main():
    src = Image.open(sys.argv[1]).convert("RGBA")
    geos = [NOW, A, B, C]; fulls = {}
    for g in geos:
        assert g.fits, g.key
        print(f"{g.key}: {g.max_cells} cells -> board {g.board_h(g.max_cells)} + {2 * g.col_top} = {g.board_h(g.max_cells) + 2 * g.col_top} of {PANEL_H}; 5 cells = {g.board_h(5)}; 2-cell item {g.board_h(2)}, 3-cell {g.board_h(3)}")
        img = render(src, g); fulls[g.key] = img; img.convert("RGB").save(HERE / f"mock-{g.key}.png")
    # the compare sheet: the party's half of the panel and the candle, 1:1, two by two
    crop = (100, 600, 1120, 1080)
    sheet([(g.label, fulls[g.key].convert("RGB").crop(crop)) for g in geos], 2, HERE / "mock-compare.png",
          footer="모든 안에서 패널은 460(위에서 620) 그대로. 맨 오른쪽 아군 열(1열)은 최대 칸을 채운 예, 나머지 아군은 지금의 5칸. 장궁(2칸)과 미늘창(3칸)은 모양을 보이는 예.")
    # the detail: one 5-cell column (mace + longbow) and the most-cells column (row 1) of every variant, 1:1
    entries = []
    for g in geos:
        im = fulls[g.key].convert("RGB")
        entries.append((f"{g.key} 5칸", im.crop((PARTY_X[1] - 14, PANEL_TOP, PARTY_X[1] + CELL_W + 14, PANEL_TOP + PANEL_H))))
        entries.append((f"{g.key} 최대 {g.max_cells}칸", im.crop((PARTY_X[3] - 14, PANEL_TOP, PARTY_X[3] + CELL_W + 14, PANEL_TOP + PANEL_H))))
    sheet(entries, 8, HERE / "mock-detail.png", lab=40, gap=16, size=24)

if __name__ == "__main__": main()
