"""Round 69: the tier's word (일반·동·은·금) and its mark on a piece, reconsidered. Two sheets over the game's own screenshot
(run 20261015-r68e, ko_35), no API call:
  mock-marks.png  the board panel as it is and with three marks (A roman-numeral tag, B pips, C metal rim), at 1x in place and at 2x;
                  a battle board for each; the whetstone's ★ beside the tier's ★★ to show the clash.
  mock-words.png  the words: the tooltip's title, the camp's mend line, the merge overlay and the merge hint, for each naming.
  .venv/bin/python ArtPipeline/Archive/69-tier-naming/mock_tier_names.py
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[3]
HERE = Path(__file__).resolve().parent
SHOT = Path.home() / "Library/Caches/F1/screenshots/20261015-r68e/ko_35_map_tiers.png"
FONT = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"
ICONS = ROOT / "Assets/@Art/Item"
AA = 4

SQ, GAP, MARGIN = 50, 2, 5                                     # GridGeometry.Square·Gap, ItemSlotView.ArtMargin
NAVY = (0x1F, 0x26, 0x54); BLACK = (9, 9, 11); LINE = (0x38, 0x38, 0x3C); INK = (0x18, 0x09, 0x07)
TEXT = (0xEB, 0xEB, 0xE6); DIM = (0x9A, 0xA0, 0xAC); EFFECT = (0x70, 0x70, 0xFF); BONE = (0xCD, 0xC0, 0xA0); DGOLD = (0xC7, 0xB3, 0x77)
FATIGUE = (0xCF, 0xBA, 0xF7); STAR_LIT = (0xEC, 0xC4, 0x5C); STAR_HOLLOW = (0x2A, 0x24, 0x1A); STAR_HOLLOW_EDGE = (0x6A, 0x60, 0x4A)
# Tiers as levels 1..4 (1 = 일반, unmarked). The game's two palettes for them: Diablo's rarity (round 49) and the metals (round 41, P3).
RARITY = {2: (0x70, 0x70, 0xFF), 3: (0xFF, 0xFF, 0x6E), 4: (0xC7, 0xB3, 0x77)}
METAL = {2: (0xA8, 0x68, 0x3A), 3: (0x9A, 0xA7, 0xB8), 4: (0xD4, 0xA2, 0x32)}
METAL_TEXT = {2: (0xD5, 0x9A, 0x66), 3: (0xD3, 0xDB, 0xE4), 4: (0xF0, 0xC8, 0x5A)}
METAL_LIGHT = {2: (0xE0, 0xA8, 0x7A), 3: (0xE4, 0xEA, 0xF0), 4: (0xFF, 0xE2, 0x8E)}
ROMAN = {1: "I", 2: "II", 3: "III", 4: "IV"}
OLD = {1: "일반", 2: "동", 3: "은", 4: "금"}

SIZES = {"longsword": (3, 1), "dagger": (2, 1), "buckler": (1, 1), "herb_pouch": (2, 1), "whetstone": (1, 1), "ember_flask": (2, 1)}


def font(px):
    return ImageFont.truetype(str(FONT), int(round(px)))


def icon(name):
    return Image.open(ICONS / f"{name}.png").convert("RGBA")


# ---- a board of 3x3 squares drawn at AA x ------------------------------------------------------------------------------------

class Board:
    """The squares of a start bag, its pieces and their marks, drawn at AA pixels per screen pixel; `lay` puts it on a picture."""

    def __init__(self, scale=1, battle=False):
        self.k = scale * AA
        self.w = 3 * SQ + 2 * GAP
        self.img = Image.new("RGBA", (self.w * self.k, self.w * self.k), LINE + (255,))
        self.d = ImageDraw.Draw(self.img)
        self.battle = battle
        for y in range(3):
            for x in range(3):
                self.rect(self.square(x, y), BLACK)

    def square(self, x, y, w=1, h=1):
        x0 = x * (SQ + GAP); y0 = y * (SQ + GAP)
        return (x0, y0, x0 + w * SQ + (w - 1) * GAP, y0 + h * SQ + (h - 1) * GAP)

    def rect(self, b, fill=None, outline=None, width=1):
        k = self.k
        self.d.rectangle((b[0] * k, b[1] * k, b[2] * k - 1, b[3] * k - 1), fill=fill, outline=outline, width=int(width * k))

    def piece(self, name, x, y, tier=1, mark="now", fatigue=0, merge_to=None, charge=None):
        w, h = SIZES[name]
        b = self.square(x, y, w, h)
        if not self.battle:
            for yy in range(h):                                 # navy square by square, the grid's line between (round 53)
                for xx in range(w):
                    self.rect(self.square(x + xx, y + yy), NAVY)
        art = icon(name)
        box_w = (b[2] - b[0] - 2 * MARGIN) * self.k; box_h = (b[3] - b[1] - 2 * MARGIN) * self.k
        s = min(box_w / art.width, box_h / art.height)
        art = art.resize((max(1, int(art.width * s)), max(1, int(art.height * s))), Image.LANCZOS)
        if charge is not None:                                  # the battle's cooldown: the icon dark, lit from the left as it charges
            dark = art.copy(); px = dark.load()
            for yy in range(dark.height):
                for xx in range(dark.width):
                    r, g, bb, a = px[xx, yy]
                    if xx > art.width * charge:
                        px[xx, yy] = (int(r * 0.35), int(g * 0.35), int(bb * 0.35), a)
            art = dark
        ax = int(((b[0] + b[2]) / 2) * self.k - art.width / 2); ay = int(((b[1] + b[3]) / 2) * self.k - art.height / 2)
        self.img.alpha_composite(art, (ax, ay))
        if fatigue and not self.battle:
            self.text((b[2] - 5, b[1] + 5), f"+{fatigue}", 13, FATIGUE, "ra")
        shown = merge_to if merge_to else tier
        if merge_to:                                            # the merge overlay (round 35): a dark veil and the tier the merge makes
            k = self.k
            veil = Image.new("RGBA", self.img.size, (0, 0, 0, 0))
            ImageDraw.Draw(veil).rectangle((b[0] * k, b[1] * k, b[2] * k - 1, b[3] * k - 1), fill=(0, 0, 0, 150))
            self.img.alpha_composite(veil)
        if shown > 1:
            self.mark(mark, shown, b)
        if merge_to:
            self.merge_label(mark, merge_to, b)

    def merge_label(self, mark, tier, b):
        word = OLD[tier] if mark == "now" else ROMAN[tier]
        color = METAL_TEXT[tier] if mark != "now" else METAL_TEXT[tier]
        cx = (b[0] + b[2]) / 2; cy = b[1] + 14
        f = font(13 * self.k)
        tw = self.d.textlength("합치기 → " + word, font=f) / self.k
        self.rect((cx - tw / 2 - 6, cy - 9, cx + tw / 2 + 6, cy + 9), INK + (230,))
        self.text((cx - tw / 2, cy), "합치기 → ", 13, TEXT, "lm")
        self.text((cx + tw / 2, cy), word, 13, color, "rm")

    def text(self, xy, s, px, color, anchor="mm", f=None):
        self.d.text((xy[0] * self.k, xy[1] * self.k), s, font=f or font(px * self.k), fill=color, anchor=anchor)

    def star(self, cx, cy, r, fill, edge, edge_w=1.0):
        import math
        pts = []
        for n in range(10):
            a = -math.pi / 2 + n * math.pi / 5; rr = r if n % 2 == 0 else r * 0.45
            pts.append(((cx + math.cos(a) * rr) * self.k, (cy + math.sin(a) * rr) * self.k))
        self.d.polygon(pts, fill=fill, outline=edge, width=max(1, int(edge_w * self.k)))

    def diamond(self, cx, cy, r, fill, edge, light=None):
        k = self.k
        self.d.polygon([(cx * k, (cy - r) * k), ((cx + r) * k, cy * k), (cx * k, (cy + r) * k), ((cx - r) * k, cy * k)],
                       fill=fill, outline=edge, width=max(1, int(0.9 * k)))
        if light:
            self.d.polygon([((cx - r * 0.45) * k, cy * k), (cx * k, (cy - r * 0.45) * k), ((cx - r * 0.15) * k, (cy - r * 0.15) * k)], fill=light)

    def mark(self, kind, tier, b):
        """The tier's mark on a piece at level 2..4: now = ★ in Diablo's rarity colour (12 px, bottom-left); A = a roman numeral on an
        ink tag; B = pips (diamonds) along the bottom-left; C = the piece's rim in the tier's metal (The Bazaar)."""
        n = tier - 1
        x0, y1 = b[0] + 4, b[3] - 4
        if kind == "now":
            for i in range(n):
                self.star(x0 + 6 + i * 13, y1 - 6, 6, RARITY[tier], INK, 0.8)
        elif kind == "A":
            s = ROMAN[tier]; f = font(12 * self.k)
            tw = self.d.textlength(s, font=f) / self.k
            self.rect((x0, y1 - 15, x0 + tw + 9, y1), INK + (235,), outline=METAL[tier] + (255,), width=1)
            self.text((x0 + 4.5 + tw / 2, y1 - 7.5), s, 12, METAL_TEXT[tier], "mm", f)
        elif kind == "B":
            for i in range(n):
                self.diamond(x0 + 5 + i * 10, y1 - 5, 4.5, METAL[tier] + (255,), INK + (255,), METAL_LIGHT[tier] + (255,))
        elif kind == "C":
            k = self.k
            rim = Image.new("RGBA", self.img.size, (0, 0, 0, 0)); rd = ImageDraw.Draw(rim)
            rd.rectangle((b[0] * k, b[1] * k, b[2] * k - 1, b[3] * k - 1), outline=METAL[tier] + (255,), width=int(2 * k))
            rd.rectangle(((b[0] + 2) * k, (b[1] + 2) * k, (b[2] - 2) * k - 1, (b[3] - 2) * k - 1), outline=INK + (140,), width=int(0.8 * k))
            rd.line(((b[0] + 1) * k, (b[1] + 1) * k, (b[2] - 1) * k, (b[1] + 1) * k), fill=METAL_LIGHT[tier] + (200,), width=int(0.9 * k))
            self.img.alpha_composite(rim)

    def whetstone_stars(self, x, y, lit_above=True):
        """The whetstone's ★ on its star squares while the pointer is on it (stage 20): lit gold above (a melee weapon lies there), hollow below."""
        for dy, lit in ((-1, lit_above), (1, False)):
            yy = y + dy
            if 0 <= yy < 3:
                b = self.square(x, yy); cx = (b[0] + b[2]) / 2; cy = (b[1] + b[3]) / 2
                if lit:
                    glow = Image.new("RGBA", self.img.size, (0, 0, 0, 0)); gd = ImageDraw.Draw(glow)
                    gd.ellipse(((cx - 16) * self.k, (cy - 16) * self.k, (cx + 16) * self.k, (cy + 16) * self.k), fill=STAR_LIT + (70,))
                    self.img.alpha_composite(glow)
                self.star(cx, cy, 12, STAR_LIT if lit else STAR_HOLLOW, INK if lit else STAR_HOLLOW_EDGE, 1.3)

    def lay(self, target, at, scale=1):
        small = self.img.resize((self.w * scale, self.w * scale), Image.LANCZOS)
        target.alpha_composite(small, at)


def stage(mark, battle=False, scale=1):
    """Rowan's board: longsword II, dagger III, whetstone (hovered: its ★ lit under the longsword), herb pouch IV."""
    b = Board(scale, battle)
    b.piece("longsword", 0, 0, tier=2, mark=mark, charge=0.6 if battle else None)
    b.piece("dagger", 0, 1, tier=3, mark=mark, fatigue=1, charge=0.9 if battle else None)
    b.piece("whetstone", 2, 1, tier=1, mark=mark, charge=1.0 if battle else None)
    b.piece("herb_pouch", 0, 2, tier=4, mark=mark, fatigue=1, charge=0.3 if battle else None)
    if not battle:
        b.whetstone_stars(2, 1)
    return b


def stage_kai(mark, scale=1):
    """Kai's board: longsword (common), a dagger II the held dagger would merge into (the overlay), a buckler III."""
    b = Board(scale)
    b.piece("longsword", 0, 0, tier=1, mark=mark)
    b.piece("dagger", 0, 1, tier=2, mark=mark, fatigue=1, merge_to=3)
    b.piece("buckler", 0, 2, tier=3, mark=mark, fatigue=1)
    return b


LABELS = {"now": "지금: ★ (디아블로 희귀도 색)", "A": "안 A: 로마 숫자 표 (금속색)", "B": "안 B: 눈금 ◆ (금속색)", "C": "안 C: 조각의 금속 테 (The Bazaar)"}


def marks_sheet():
    shot = Image.open(SHOT).convert("RGBA")
    strip = shot.crop((0, 620, 960, 810))                        # the board panel: four boards
    pad = 12; lab = 26
    det_cols = 4; W = max(960 + 2 * pad, pad + det_cols * (154 * 2 + 18))
    rows = []
    for mark in ("now", "A", "B", "C"):
        s = strip.copy()
        stage_kai(mark).lay(s, (513, 646 - 620))
        stage(mark).lay(s, (703, 646 - 620))
        rows.append((LABELS[mark], s))
    # Detail at 2x: Rowan's board for each mark, and the battle board under it.
    det_w = 154 * 2; det_gap = 18
    detail = Image.new("RGBA", (W, lab + det_w + 10 + 154 + lab + 20), (0x1A, 0x18, 0x17, 255))
    dd = ImageDraw.Draw(detail)
    x = pad
    for mark in ("now", "A", "B", "C"):
        dd.text((x, 4), LABELS[mark].split(":")[0], font=font(15), fill=TEXT)
        stage(mark, scale=2).lay(detail, (x, lab), 2)
        dd.text((x, lab + det_w + 4), "전투 보드 (쿨다운 중)", font=font(13), fill=DIM)
        stage(mark, battle=True).lay(detail, (x, lab + det_w + 22))
        x += det_w + det_gap
    H = pad + sum(lab + r[1].height + 8 for r in rows) + detail.height + pad
    sheet = Image.new("RGBA", (W, H), (0x1A, 0x18, 0x17, 255))
    d = ImageDraw.Draw(sheet)
    y = pad
    for label, img in rows:
        d.text((pad, y), label, font=font(15), fill=TEXT)
        sheet.alpha_composite(img, (pad, y + lab - 4))
        y += lab + img.height + 8
    sheet.alpha_composite(detail, (0, y))
    sheet.convert("RGB").save(HERE / "mock-marks.png")


# ---- the words -----------------------------------------------------------------------------------------------------------------

NAMINGS = [
    ("지금: 일반·동·은·금", {1: "일반", 2: "동", 3: "은", 4: "금"}, "now"),
    ("안 1: 로마 숫자 II·III·IV", ROMAN, "metal"),
    ("안 2: 강화 +1·+2·+3", {1: "일반", 2: "+1", 3: "+2", 4: "+3"}, "metal"),
    ("안 3: 별 2성·3성·4성", {1: "1성", 2: "2성", 3: "3성", 4: "4성"}, "metal"),
    ("안 4: 상급·특급·명품", {1: "일반", 2: "상급", 3: "특급", 4: "명품"}, "metal"),
]


def colored(d, xy, parts, px, anchor_left=True):
    """Parts of (text, colour) on one line from xy."""
    f = font(px); x, y = xy
    for s, c in parts:
        d.text((x, y), s, font=f, fill=c)
        x += d.textlength(s, font=f)
    return x


def words_sheet():
    col_w = 330; pad = 14; W = pad + 5 * (col_w + pad)
    H = 470
    sheet = Image.new("RGB", (W, H), (0x1A, 0x18, 0x17))
    d = ImageDraw.Draw(sheet)
    for i, (label, words, palette) in enumerate(NAMINGS):
        x = pad + i * (col_w + pad); y = pad
        d.text((x, y), label, font=font(16), fill=TEXT); y += 30
        tc = (lambda t: RARITY[t]) if palette == "now" else (lambda t: METAL_TEXT[t])
        title = lambda name, t: name if t == 1 else (f"{name} · {words[t]}" if palette == "now" else f"{name} {words[t]}")
        # 1. Diablo's tooltip (inventory window, merchant): the title in the tier's colour, facts, effects in blue.
        d.text((x, y), "툴팁 제목", font=font(12), fill=DIM); y += 16
        d.rectangle((x, y, x + col_w - 1, y + 92), fill=(0, 0, 0), outline=(0x40, 0x40, 0x40))
        cx = x + col_w / 2
        d.text((cx, y + 14), title("단검", 3), font=font(15), fill=tc(3), anchor="mm")
        d.text((cx, y + 34), "무기 장비 / 크기 2×1 / 근접 / 쿨다운 1.5초", font=font(12), fill=TEXT, anchor="mm")
        d.text((cx, y + 52), "맨 앞 적에게 피해 3-6", font=font(12), fill=EFFECT, anchor="mm")
        d.text((cx, y + 70), "전투마다 피로 +1", font=font(12), fill=FATIGUE, anchor="mm")
        y += 104
        # 2. The item card's title (round 42), a common and a gold item.
        d.text((x, y), "카드·바닥 이름판의 제목", font=font(12), fill=DIM); y += 16
        d.text((x, y), title("롱소드", 1), font=font(15), fill=(0xEE, 0xEE, 0xEE)); y += 20
        d.text((x, y), title("약초 주머니", 4), font=font(15), fill=tc(4)); y += 28
        # 3. The camp's mend line: "롱소드  일반 → 동".
        d.text((x, y), "야영지의 정비 창", font=font(12), fill=DIM); y += 16
        d.rectangle((x, y, x + col_w - 1, y + 44), fill=(0x14, 0x1A, 0x2C))
        if palette == "now":
            parts = [("롱소드  ", TEXT), ("일반", (0xC9, 0xC2, 0xB0)), (" → ", TEXT), ("동", METAL_TEXT[2])]
        else:
            parts = [("롱소드  ", TEXT), (words[1], (0xC9, 0xC2, 0xB0)), (" → ", TEXT), (words[2], METAL_TEXT[2])]
        colored(d, (x + 12, y + 8), parts, 15)
        d.text((x + 12, y + 27), "맨 앞 적에게 피해 2-4 → 4-8", font=font(12), fill=DIM); y += 56
        # 4. The merge overlay on a board piece and the hint under the facts.
        d.text((x, y), "합치기 표시와 안내", font=font(12), fill=DIM); y += 16
        d.rectangle((x, y, x + 120, y + 22), fill=(0x18, 0x09, 0x07))
        colored(d, (x + 8, y + 3), [("합치기 → ", TEXT), (words[3], METAL_TEXT[3])], 13); y += 30
        f = font(12)
        if palette == "now":
            lines = [[("같은 ", TEXT), ("단검·동", TEXT), (" 위에 놓으면 합쳐서 ", TEXT), ("은", METAL_TEXT[3]), (" 하나가 됩니다.", TEXT)]]
        elif "성" in words[2]:
            lines = [[("같은 ", TEXT), (f"단검 {words[2]}", TEXT), (" 위에 놓으면 합쳐서 ", TEXT), (words[3], METAL_TEXT[3]), (" 하나가 됩니다.", TEXT)]]
        else:
            lines = [[("같은 ", TEXT), (f"단검 {words[2]}", TEXT), (" 위에 놓으면 합쳐서 ", TEXT), (f"단검 {words[3]}", METAL_TEXT[3]), (" 하나가 됩니다.", TEXT)]]
        for parts in lines:
            # wrap by hand at the arrow phrase so it fits the column
            colored(d, (x, y), parts[:3], 12); y += 17
            colored(d, (x, y), parts[3:], 12); y += 24
        # 5. The shop's price line and a drop: nothing changes there; the ladder as a row for the eye.
        d.text((x, y), "네 단계를 나란히", font=font(12), fill=DIM); y += 16
        xx = x
        for t in (1, 2, 3, 4):
            s = words[t]; c = (0xC9, 0xC2, 0xB0) if t == 1 else tc(t)
            d.text((xx, y), s, font=font(15), fill=c); xx += d.textlength(s, font=font(15)) + 18
    sheet.save(HERE / "mock-words.png")


if __name__ == "__main__":
    marks_sheet()
    words_sheet()
    print("wrote", HERE / "mock-marks.png", HERE / "mock-words.png")
