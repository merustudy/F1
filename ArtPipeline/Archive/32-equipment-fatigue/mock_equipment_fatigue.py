"""Equipment fatigue (round 32): every equipment item on a mercenary's board except the base weapon adds 1 fatigue
when a battle starts (user, 2026-10-06). How that cost shows next to the board: drawn over the current reward screen
with the game's own sprites (slot) and item icons, so only the fatigue marks differ between the sheets. No API call.
  .venv/bin/python ArtPipeline/Archive/32-equipment-fatigue/mock_equipment_fatigue.py
The screenshot has only the base weapons, so every board is filled with an example loadout from the middle of an
expedition (the same in every sheet, "now" included): a weapon, an armor, an attack item and support items, so that
the sheets show which items cost and which do not.
"""
import re
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[3]; HERE = Path(__file__).resolve().parent
FRAME = ROOT / "Assets/@Art/UI/Frame"; ITEM = ROOT / "Assets/@Art/Item"
FONT = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"
REWARD = HERE / "game/ko_07_reward.png"; MAP = HERE / "game/ko_08_map_item_selected.png"

# The party side (UiPrefabSetup.PartySide and Kit, BattleItemView).
PANEL_TOP = 620; CELLS_TOP = PANEL_TOP + 24        # BoardPanelTop, BoardCellsTop
CELL_W, CELL_H, GAP = 180, 60, 2                   # BattleItemView.CellWidth, CellHeight, CellGapY
HEAD_H, HEAD_OUT, HEAD_PAD = 22, 3, 6              # BoardHeadHeight, BoardHeadOut, BoardHeadNameGap
ICON_MX, ICON_MY = 8, 5                            # ItemIconMarginX/Y
BADGE, BADGE_INSET = 24, 5                         # GradeBadgeSize/Inset
SLOT_BORDER = 16                                   # UiArt piece border of the slot (sprite pixels, 2x)
COLUMN_X = {4: 120, 3: 310, 2: 500, 1: 690}        # the cells' x of each row (the rearmost row on the left)

# The reward cards (UiPrefabSetup.Reward): the options start at PartyRightTop, 132 high with 8 between; the body
# (19, Text) starts 46 under a card's top and 20 in from its left.
CARD_X, CARD_TOPS, CARD_BODY_DY, BODY_SIZE = 980, (190, 330, 470), 46, 19

# UiPalette, and the one new colour: the fatigue's (a pale violet: no other mark of the game uses it, so it is not
# read as a burn's orange or the cooldown's gold).
INK = (0x18, 0x09, 0x07); BRASS = (0xB8, 0x94, 0x4E); TEXT = (0xEB, 0xEB, 0xE6); TEXT_DIM = (0x9A, 0xA0, 0xAC)
FATIGUE = (0xCF, 0xBA, 0xF7); FATIGUE_FILL = (0x2C, 0x1A, 0x3A); FATIGUE_RIM = (0x9E, 0x84, 0xDA)

# (item, cells, grade, costs) from the top of each board. "costs": an equipment item (무기 장비 or 방어 장비) that is not
# the base weapon. The base weapon is the first item of each board.
BOARDS = {
    4: [("fire_staff", 1, 10, False), ("longbow", 2, 10, True)],                                   # 미라: 장궁(무기) +1
    3: [("healing_staff", 1, 10, False), ("herb_pouch", 1, 8, False), ("ward_charm", 1, 10, False)],  # 엘라: 지원 둘은 0
    2: [("sword", 1, 12, False), ("spear", 2, 10, True), ("ember_flask", 1, 8, False)],              # 카이: 창 +1, 플라스크(공격) 0
    1: [("longsword", 1, 10, False), ("buckler", 1, 8, True), ("dagger", 1, 10, True)],              # 로언: 버클러(방어) +1, 단검 +1
}
CARD_COSTS = (True, True, True)     # the three options of the screenshot: 버클러, 창, 장궁 — all equipment


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


SLOT = Image.open(FRAME / "slot.png").convert("RGBA")


def text_in(img, box, s, size, fill, stroke=0, stroke_fill=None, align="center"):
    """Writes one line in a box: centred, or against the box's right edge; vertically centred on the glyphs."""
    d = ImageDraw.Draw(img); f = font(size)
    l, t, r, b = d.textbbox((0, 0), s, font=f, stroke_width=stroke)
    x = box[2] - r if align == "right" else box[0] + (box[2] - box[0] - (r - l)) / 2 - l
    y = box[1] + (box[3] - box[1] - (b - t)) / 2 - t
    d.text((x, y), s, font=f, fill=fill, stroke_width=stroke, stroke_fill=stroke_fill)


def smooth(draw_at, w, h, k=4):
    """Draws a small shape at k times its size and brings it down, for clean round edges."""
    im = Image.new("RGBA", (w * k, h * k), (0, 0, 0, 0)); draw_at(ImageDraw.Draw(im), k)
    return im.resize((w, h), Image.LANCZOS)


def grade_badge(grade):
    """The grade badge of a party-side cell: an ink knob with a brass face 3 in, the grade in ink (Kit.KitBadge)."""
    def draw(d, k):
        d.ellipse([0, 0, BADGE * k - 1, BADGE * k - 1], fill=INK + (255,))
        d.ellipse([3 * k, 3 * k, (BADGE - 3) * k - 1, (BADGE - 3) * k - 1], fill=BRASS + (255,))
    im = smooth(draw, BADGE, BADGE); text_in(im, (0, 0, BADGE, BADGE), str(grade), 14, INK); return im


def fatigue_tag(s="+1", w=30, h=20):
    """The new mark on a cell whose item costs fatigue: a small plum pill with a violet rim and the cost.
    Without a width it is as wide as its words and 8 on either side (the board head's total, second sheets)."""
    if w is None: w = round(ImageDraw.Draw(Image.new("RGBA", (1, 1))).textlength(s, font=font(13))) + 16
    def draw(d, k):
        d.rounded_rectangle([0, 0, w * k - 1, h * k - 1], radius=h * k // 2, fill=FATIGUE_RIM + (255,))
        d.rounded_rectangle([2 * k, 2 * k, (w - 2) * k - 1, (h - 2) * k - 1], radius=(h - 4) * k // 2, fill=FATIGUE_FILL + (255,))
    im = smooth(draw, w, h); text_in(im, (0, 0, w, h), s, 13, FATIGUE); return im


FATIGUE_INK = (0x4A, 0x2A, 0x6E)    # the fatigue's colour dark enough to read on the bone of a cell (B2)


def slot_tag(s, h=20):
    """B2: the total in a small cell of its own: the item cells' slot (bone, ink rim) cut down, the words in dark violet."""
    w = round(ImageDraw.Draw(Image.new("RGBA", (1, 1))).textlength(s, font=font(13))) + 18
    im = nine(SLOT, (2 * w, 2 * h), SLOT_BORDER // 2).resize((w, h), Image.LANCZOS)
    text_in(im, (0, 0, w, h), s, 13, FATIGUE_INK); return im


def item_block(item, cells, grade):
    """A party-side cell block: the slot, the icon in its place (in proportion), the grade badge at the bottom-left."""
    h = cells * CELL_H + (cells - 1) * GAP
    block = frame(SLOT, SLOT_BORDER, CELL_W, h)
    icon = Image.open(ITEM / f"{item}.png").convert("RGBA")
    bw, bh = CELL_W - 2 * ICON_MX, h - 2 * ICON_MY; k = min(bw / icon.width, bh / icon.height)
    icon = icon.resize((max(1, round(icon.width * k)), max(1, round(icon.height * k))), Image.LANCZOS)
    block.alpha_composite(icon, ((CELL_W - icon.width) // 2, (h - icon.height) // 2))
    block.alpha_composite(grade_badge(grade), (BADGE_INSET, h - BADGE_INSET - BADGE))
    return block


def draw_boards(img, variant):
    """Every board's items over the screenshot's cells (they are opaque and in the same places), then the variant's marks."""
    d = ImageDraw.Draw(img)
    for row, items in BOARDS.items():
        x = COLUMN_X[row]; y = CELLS_TOP; cost = 0
        for item, cells, grade, costs in items:
            block = item_block(item, cells, grade)
            if costs and variant in ("B", "C", "B1", "B2", "B3"):
                block.alpha_composite(fatigue_tag(), (CELL_W - 5 - 30, 5))
            img.alpha_composite(block, (x, y)); y += cells * CELL_H + cells * GAP; cost += costs
        if cost and variant in ("A", "B"):
            # The total on the head of the board, against its right end, outlined like the name. The name's glyphs sit
            # 1.5 under the strip's middle (the strip's art has more rim above), so the box is moved down with them.
            head = (x - HEAD_OUT, PANEL_TOP + 1, x + CELL_W + HEAD_OUT - HEAD_PAD, PANEL_TOP + HEAD_H + 2)
            text_in(img, head, f"피로 +{cost}", 13, FATIGUE, stroke=1, stroke_fill=INK, align="right")
        if cost and variant in ("B1", "B2", "B3"):
            # Second sheets: the total framed like the cells' marks, against the head's right end (5 in from the strip's
            # end, as far as the cells' marks are from a cell's), centred on the name's glyphs (632.5).
            mark = {"B1": lambda: fatigue_tag(f"피로 +{cost}", w=None), "B2": lambda: slot_tag(f"피로 +{cost}"),
                    "B3": lambda: fatigue_tag(f"+{cost}")}[variant]()
            img.alpha_composite(mark, (x + CELL_W + HEAD_OUT - 5 - mark.width, PANEL_TOP + 1 + (HEAD_H + 1 - mark.height) // 2))


def card_text_end(img, top):
    """The right end of the facts line of a reward card: the last pixel that stands out from the card's own colour."""
    y0, y1 = top + CARD_BODY_DY + 2, top + CARD_BODY_DY + 22
    bg = img.getpixel((1890, top + CARD_BODY_DY + 12))[:3]; end = CARD_X + 20
    for y in range(y0, y1):
        for x in range(CARD_X + 20, 1890):
            p = img.getpixel((x, y))[:3]
            if sum(abs(a - b) for a, b in zip(p, bg)) > 120: end = max(end, x)
    return end


def draw_cards(img):
    """Adds the cost to the facts line of every option that is an equipment item: " / 전투마다 피로 +1"."""
    d = ImageDraw.Draw(img); f = font(BODY_SIZE)
    for top, costs in zip(CARD_TOPS, CARD_COSTS):
        if not costs: continue
        x = card_text_end(img, top) + 1; y = top + CARD_BODY_DY
        d.text((x, y), " / ", font=f, fill=TEXT); x += d.textlength(" / ", font=f)
        d.text((x, y), "전투마다 피로 +1", font=f, fill=FATIGUE)


def render(variant):
    img = Image.open(REWARD).convert("RGBA")
    draw_boards(img, variant)
    if variant != "now": draw_cards(img)
    return img


# The node map's line about the chosen item (PartyDetail: 19, Text, 880 x 54 at the panel's x 1000 and 122 under its top,
# so two lines). Its glyphs start 3 under the box's top; a line is 24 high.
DETAIL_BOX = (1000, PANEL_TOP + 122, 1880, PANEL_TOP + 122 + 54); DETAIL_LINE = 24
DETAIL_BASE = [("롱소드 · 등급 10 — 무기 장비 / 크기 1칸 / 쿨다운 3.0초 / 앞에서 2번째 자리까지만 발동 / 맨 앞 적에게 피해 11", TEXT),
               (" / ", TEXT), ("기본 무기: 피로 없음", TEXT_DIM)]
DETAIL_FOUND = [("단검 · 등급 10 — 무기 장비 / 크기 1칸 / 쿨다운 1.5초 / 앞에서 2번째 자리까지만 발동 / 맨 앞 적에게 피해 4", TEXT),
                (" / ", TEXT), ("전투마다 피로 +1", FATIGUE)]


def rich_wrap(d, segments, box, line_h, f):
    """Writes coloured runs left to right and wraps between words when a word would cross the box's right edge."""
    x0, y0, x1, _ = box; top = d.textbbox((0, 0), "롱", font=f)[1]
    x, y = x0, y0 + 3 - top
    for text, color in segments:
        for word in re.findall(r"\S+\s*|\s+", text):
            if x > x0 and x + d.textlength(word.rstrip(), font=f) > x1:
                x, y = x0, y + line_h; word = word.lstrip()
            d.text((x, y), word, font=f, fill=color); x += d.textlength(word, font=f)


def detail_lines():
    """The right half of the node map with the chosen item's line: the base weapon, and a dagger that was found."""
    src = Image.open(MAP).convert("RGBA"); f = font(BODY_SIZE); crops = []
    bg = src.getpixel((DETAIL_BOX[2] - 4, DETAIL_BOX[1] + 30))
    for segments in (DETAIL_BASE, DETAIL_FOUND):
        img = src.copy(); d = ImageDraw.Draw(img)
        d.rectangle(DETAIL_BOX, fill=bg); rich_wrap(d, segments, DETAIL_BOX, DETAIL_LINE, f)
        crops.append(img.crop((980, PANEL_TOP + 16, 1900, PANEL_TOP + 196)))
    return crops


def sheet(entries, cols, path, lab=44, gap=24, size=28, footer=None):
    w = max(im.width for _, im in entries); h = max(im.height for _, im in entries); rows = (len(entries) + cols - 1) // cols
    fh = 50 if footer else 0
    out = Image.new("RGB", (cols * (w + gap) + gap, rows * (h + lab + gap) + gap + fh), (18, 18, 18)); d = ImageDraw.Draw(out)
    for i, (name, im) in enumerate(entries):
        x = gap + (i % cols) * (w + gap); y = gap + (i // cols) * (h + lab + gap)
        d.text((x, y), name, font=font(size), fill=(235, 235, 235)); out.paste(im.convert("RGB"), (x, y + lab))
    if footer: d.text((gap, out.height - fh + 10), footer, font=font(22), fill=(190, 190, 190))
    out.save(path); print(path.name, out.size)


LABELS = {
    "now": "지금: 표시 없음",
    "A": "A안: 보드 머리에 합계만",
    "B": "B안: 칸마다 +1, 보드 머리에 합계 (권장)",
    "C": "C안: 칸마다 +1만",
}


def main():
    fulls = {}
    for v in LABELS:
        img = render(v); fulls[v] = img; img.convert("RGB").save(HERE / f"mock-{v}.png"); print(f"mock-{v}.png")
    boards = (100, 606, 890, 966)
    sheet([(LABELS[v], fulls[v].crop(boards)) for v in LABELS], 2, HERE / "mock-compare.png",
          footer="보드는 원정 중반쯤의 예로 채웠다(모든 안이 같다). 피로를 내는 것: 기본 무기가 아닌 무기 장비·방어 장비 하나에 전투마다 +1. 지원·공격 아이템은 0.")
    knight = (684, 612, 876, 836)
    sheet([(LABELS[v].split(":")[0], fulls[v].crop(knight).resize((2 * (knight[2] - knight[0]), 2 * (knight[3] - knight[1])), Image.LANCZOS)) for v in LABELS],
          4, HERE / "mock-detail.png", lab=40, gap=16, size=24,
          footer="로언(1열)의 보드를 2배로: 롱소드(기본 무기, 0), 버클러(방어 장비, +1), 단검(무기 장비, +1).")
    cards = (980, 186, 1904, 606)
    sheet([("지금", fulls["now"].crop(cards)), ("A·B·C 공통: 장비의 보상 카드에 비용", fulls["B"].crop(cards))], 2, HERE / "mock-cards.png")
    base, found = detail_lines()
    sheet([("노드 맵에서 기본 무기를 골랐을 때 (A·B·C 공통)", base), ("얻은 장비를 골랐을 때", found)], 1, HERE / "mock-lines.png", lab=40, gap=16, size=24)

    # Second sheets (user, 2026-10-06): B with the head's total framed like the cells' marks, for one look.
    seconds = {"B": "B안 (지난 목업): 머리의 합계는 글만", "B1": "B1: 칸의 표와 같은 표에 '피로 +N' (권장)",
               "B2": "B2: 칸과 같은 뼈색 틀에 '피로 +N'", "B3": "B3: 칸의 표와 같은 표에 숫자만 '+N'"}
    for v in ("B1", "B2", "B3"):
        img = render(v); fulls[v] = img; img.convert("RGB").save(HERE / f"mock2-{v}.png"); print(f"mock2-{v}.png")
    sheet([(seconds[v], fulls[v].crop(boards)) for v in seconds], 2, HERE / "mock2-compare.png",
          footer="칸의 표와 보상 카드·고른 아이템 줄의 문구는 넷 모두 같다(B안). 바뀌는 것은 보드 머리의 합계뿐이다.")
    pair = (494, 612, 876, 836)      # 카이(2열, +1)와 로언(1열, +2)
    sheet([(seconds[v].split(":")[0].split(" (")[0], fulls[v].crop(pair).resize((2 * (pair[2] - pair[0]), 2 * (pair[3] - pair[1])), Image.LANCZOS)) for v in seconds],
          2, HERE / "mock2-detail.png", lab=40, gap=16, size=24, footer="카이(2열, 피로 +1)와 로언(1열, 피로 +2)의 보드 머리와 위 세 칸을 2배로.")


if __name__ == "__main__": main()
