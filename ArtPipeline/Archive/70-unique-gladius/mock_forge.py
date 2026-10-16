"""Round 70: the first unique, the gladius (longsword + two whetstones), as a mockup over the game's own screenshots (no API call):
  mock-forge.png  the recipe on a board (the whetstones' stars lit on the longsword) and the gladius after; the camp window with the
                  forge as a third card (A) and as a row of the mend step (B); the gladius card with the plate head (round 42, mockup 7)
                  beside the card as it is; the gladius piece unmarked / with a unique-gold rim.
The gladius icon is a stand-in: the longsword's art with the middle of its blade cut out. The real icon is one generation call.
  .venv/bin/python ArtPipeline/Archive/70-unique-gladius/mock_forge.py
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent / "69-tier-naming"))
import mock_tier_names as m  # noqa: E402

ROOT = HERE.parents[2]
SHOTS = Path.home() / "Library/Caches/F1/screenshots/20261015-233559"
UNIQUE = (0xC7, 0xB3, 0x77)                                   # Diablo's unique gold (the title colour round 69 freed)
UNIQUE_LIGHT = (0xF0, 0xE0, 0xA8)
CHOSEN = {2: (0x70, 0x70, 0xFF), 3: (0xD3, 0xDB, 0xE4), 4: (0xFF, 0xFF, 0x6E)}
WORDS = {1: "일반", 2: "1성", 3: "2성", 4: "3성"}


def font(px):
    return ImageFont.truetype(str(m.FONT), int(round(px)))


def gladius_icon():
    """The longsword's art with the middle third of the blade cut out: a short sword, a stand-in for the generated icon."""
    art = Image.open(ROOT / "Assets/@Art/Item/longsword.png").convert("RGBA")
    w, h = art.size
    cut0, cut1 = int(w * 0.50), int(w * 0.78)
    out = Image.new("RGBA", (w - (cut1 - cut0), h), (0, 0, 0, 0))
    out.alpha_composite(art.crop((0, 0, cut0, h)), (0, 0))
    out.alpha_composite(art.crop((cut1, 0, w, h)), (cut0, 0))
    return out


m.SIZES["gladius"] = (2, 1)
_icon = m.icon


def icon(name):
    return gladius_icon() if name == "gladius" else _icon(name)


m.icon = icon


def chosen_marks():
    m.METAL.update(CHOSEN); m.METAL_TEXT.update(CHOSEN); m.ROMAN.update(WORDS)

    def mark(self, kind, tier, b):
        n = tier - 1; x0, y1 = b[0] + 4, b[3] - 4
        for i in range(n):
            self.diamond(x0 + 6 + i * 13, y1 - 6, 6, CHOSEN[tier] + (255,), m.INK + (255,), None)
    m.Board.mark = mark


def unique_rim(board, b):
    """Option (b): a 2 px rim in the unique gold round the gladius's piece."""
    k = board.k
    rim = Image.new("RGBA", board.img.size, (0, 0, 0, 0)); rd = ImageDraw.Draw(rim)
    rd.rectangle((b[0] * k, b[1] * k, b[2] * k - 1, b[3] * k - 1), outline=UNIQUE + (255,), width=int(2 * k))
    rd.line(((b[0] + 1) * k, (b[1] + 1) * k, (b[2] - 1) * k, (b[1] + 1) * k), fill=UNIQUE_LIGHT + (190,), width=int(0.8 * k))
    board.img.alpha_composite(rim)


def board_before(scale=1):
    """Rowan's board with the recipe laid: a 2성 longsword across the top, a whetstone under its middle square (its star lit on the sword)
    and... the frame is 3 wide, so the second whetstone goes under the sword too — both whetstones in row 1, each lighting the sword."""
    b = m.Board(scale)
    b.piece("longsword", 0, 0, tier=3, mark="B")
    b.piece("whetstone", 0, 1, tier=1, mark="B")
    b.piece("whetstone", 2, 1, tier=1, mark="B")
    b.piece("buckler", 1, 2, tier=1, mark="B", fatigue=1)
    # The stars of both whetstones on the sword: lit above each; the squares below are hollow.
    for x in (0, 2):
        b.whetstone_stars(x, 1, lit_above=True)
    return b


def board_after(scale=1, rim=False):
    b = m.Board(scale)
    b.piece("gladius", 0, 0, tier=3, mark="B")
    if rim:
        unique_rim(b, b.square(0, 0, 2, 1))
    b.piece("buckler", 1, 2, tier=1, mark="B", fatigue=1)
    return b


# ---- the camp window ------------------------------------------------------------------------------------------------------------

CAMP_BOX = (1150, 300, 1730, 620)                             # the camp window on ko_33 (choices) / ko_34 (mend step)
CARD_REST = (1178 - 1150, 435 - 300, 1432 - 1150, 590 - 300)
CARD_MEND = (1447 - 1150, 435 - 300, 1702 - 1150, 590 - 300)
BLUE = (0x2E, 0x64, 0xAE)


def camp_three_cards():
    """Option A: three cards of the same height, narrower, the forge card on the right in the unique gold's tone."""
    img = Image.open(SHOTS / "ko_33_camp.png").convert("RGB").crop(CAMP_BOX)
    d = ImageDraw.Draw(img)
    # Blank the two cards and lay three.
    d.rectangle((CARD_REST[0], CARD_REST[1], CARD_MEND[2], CARD_MEND[3]), fill=(0x14, 0x1A, 0x2C))
    x0, x1, y0, y1 = CARD_REST[0], CARD_MEND[2], CARD_REST[1], CARD_REST[3]
    gap = 10; w = (x1 - x0 - 2 * gap) // 3
    cards = [("쉬기", ["HP 30% 회복", "피로도 -20"], BLUE, m.TEXT), ("정비", ["보드의 아이템 하나를", "한 단계 위로"], BLUE, m.TEXT),
             ("담금질", ["로언의 롱소드 2성 + 숫돌 둘", "→ 글라디우스 2성"], (0x5A, 0x48, 0x1E), UNIQUE_LIGHT)]
    for i, (title, body, fill, tc) in enumerate(cards):
        cx0 = x0 + i * (w + gap)
        d.rectangle((cx0, y0, cx0 + w, y1), fill=fill, outline=(UNIQUE if i == 2 else fill))
        d.text((cx0 + 14, y0 + 16), title, font=font(24), fill=tc)
        for j, line in enumerate(body):
            d.text((cx0 + 14, y0 + 62 + j * 22), line, font=font(14), fill=tc)
    return img


def camp_mend_row():
    """Option B: the mend step as it is, with the forge offered as the first row of the step (a plate above the item list)."""
    img = Image.open(SHOTS / "ko_34_camp_mend.png").convert("RGB").crop(CAMP_BOX)
    d = ImageDraw.Draw(img)
    # Over the mend plate: the forge row above it, in the unique gold.
    y = 430 - 300 - 48
    d.rectangle((1178 - 1150 - 4, y - 6, 1702 - 1150 + 4, y + 46), fill=(0x14, 0x1A, 0x2C))   # over the hint line
    d.rectangle((1178 - 1150, y, 1702 - 1150, y + 40), fill=(0x2A, 0x24, 0x14), outline=UNIQUE)
    d.text((1178 - 1150 + 12, y + 10), "담금질  로언의 롱소드 2성 + 숫돌 둘 → 글라디우스 2성", font=font(15), fill=UNIQUE_LIGHT)
    return img


def camp_confirm():
    """The forge's confirm step (as the mend has one): what goes in, what comes out, the cost."""
    img = Image.open(SHOTS / "ko_34_camp_mend.png").convert("RGB").crop(CAMP_BOX)
    d = ImageDraw.Draw(img)
    d.rectangle((110, 20, img.width - 4, img.height - 4), fill=(0x14, 0x1A, 0x2C))
    d.text((120, 36), "15층 · 야영지 · 담금질", font=font(26), fill=m.TEXT)
    d.text((120, 74), "쉬지 않고 담금질합니다. 로언의 피로 +20.", font=font(14), fill=m.DIM)
    d.rectangle((26, 128, img.width - 26, 230), fill=(0x1E, 0x26, 0x40))
    ing = icon("longsword").resize((84, 24), Image.LANCZOS); img.paste(ing, (42, 150), ing)
    ws = icon("whetstone").resize((26, 26), Image.LANCZOS); img.paste(ws, (134, 150), ws); img.paste(ws, (166, 150), ws)
    d.text((204, 150), "롱소드 2성 + 숫돌 + 숫돌", font=font(15), fill=m.TEXT)
    d.text((204, 172), "→", font=font(15), fill=m.DIM)
    g = icon("gladius").resize((64, 24), Image.LANCZOS); img.paste(g, (236, 170), g)
    m.colored(d, (308, 172), [("글라디우스 2성", UNIQUE)], 15)
    d.text((42, 200), "맨 앞 적 둘에게 피해 12-18 · 치명타 10% · 2×1 · 기본 무기", font=font(13), fill=m.EFFECT)
    d.rectangle((26, 246, 276, 292), fill=(0x2A, 0x33, 0x4A)); d.text((151, 269), "돌아가기", font=font(18), fill=m.TEXT, anchor="mm")
    d.rectangle((img.width - 276, 246, img.width - 26, 292), fill=(0x5A, 0x48, 0x1E), outline=UNIQUE); d.text((img.width - 151, 269), "담금질하기", font=font(18), fill=UNIQUE_LIGHT, anchor="mm")
    return img


# ---- the card ------------------------------------------------------------------------------------------------------------------

def plate_unique():
    """plate_label as it is: its gold lines are the unique's gold already."""
    return Image.open(ROOT / "Assets/@Art/UI/Frame/plate_label.png").convert("RGBA").resize((180, 88), Image.LANCZOS)


def nine(src, border, size):
    w, h = size; sw, sh = src.size; out = Image.new("RGBA", size, (0, 0, 0, 0)); b = border
    boxes = [((0, 0, b, b), (0, 0)), ((sw - b, 0, sw, b), (w - b, 0)), ((0, sh - b, b, sh), (0, h - b)), ((sw - b, sh - b, sw, sh), (w - b, h - b))]
    for box, at in boxes:
        out.alpha_composite(src.crop(box), at)
    out.alpha_composite(src.crop((b, 0, sw - b, b)).resize((w - 2 * b, b)), (b, 0))
    out.alpha_composite(src.crop((b, sh - b, sw - b, sh)).resize((w - 2 * b, b)), (b, h - b))
    out.alpha_composite(src.crop((0, b, b, sh - b)).resize((b, h - 2 * b)), (0, b))
    out.alpha_composite(src.crop((sw - b, b, sw, sh - b)).resize((b, h - 2 * b)), (w - b, b))
    out.alpha_composite(src.crop((b, b, sw - b, sh - b)).resize((w - 2 * b, h - 2 * b)), (b, b))
    return out


def card_now():
    W, H = 400, 150
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0)); d = ImageDraw.Draw(img)
    d.rounded_rectangle((0, 0, W - 1, H - 1), radius=4, fill=m.INK + (240,), outline=(0xB8, 0x94, 0x4E, 200))
    d.text((24, 14), "글라디우스 2성", font=font(22), fill=UNIQUE)
    d.line((24, 44, W - 16, 44), fill=(0xB8, 0x94, 0x4E, 160))
    d.text((24, 54), "무기 장비 / 크기 2×1 / 근접 / 쿨다운 3.0초 / 치명타 10% /", font=font(13), fill=m.TEXT)
    d.text((24, 72), "앞에서 2번째 자리까지만 발동", font=font(13), fill=m.TEXT)
    d.text((24, 94), "맨 앞 적 둘에게 피해 12-18", font=font(14), fill=m.TEXT)
    d.text((24, 116), "기본 무기: 피로 없음 · 유니크: 파티에 하나", font=font(13), fill=m.DIM)
    return img


def card_plate():
    W, HEAD = 400, 56
    H = HEAD + 120
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0)); d = ImageDraw.Draw(img)
    d.rounded_rectangle((0, HEAD // 2, W - 1, H - 1), radius=4, fill=m.INK + (240,), outline=(0xB8, 0x94, 0x4E, 200))
    img.alpha_composite(nine(plate_unique(), 22, (W, HEAD)), (0, 0)); d = ImageDraw.Draw(img)
    d.text((W / 2, HEAD / 2), "글라디우스", font=font(24), fill=UNIQUE, anchor="mm")
    d.text((W / 2, HEAD + 14), "유니크 · 2성", font=font(14), fill=UNIQUE, anchor="mm")
    d.text((24, HEAD + 32), "무기 장비 / 크기 2×1 / 근접 / 쿨다운 3.0초 / 치명타 10% /", font=font(13), fill=m.TEXT)
    d.text((24, HEAD + 50), "앞에서 2번째 자리까지만 발동", font=font(13), fill=m.TEXT)
    d.text((24, HEAD + 72), "맨 앞 적 둘에게 피해 12-18", font=font(14), fill=m.TEXT)
    d.text((24, HEAD + 94), "기본 무기: 피로 없음 · 파티에 하나", font=font(13), fill=m.DIM)
    return img


def main():
    chosen_marks()
    pad = 12; W = 1240
    sheet = Image.new("RGBA", (W, 1500), (0x1A, 0x18, 0x17, 255)); d = ImageDraw.Draw(sheet)
    y = pad
    d.text((pad, y), "1. 재료: 롱소드(여기서는 2성)의 위·아래 ★에 숫돌 둘이 켜진 보드  →  담금질 뒤: 글라디우스 2×1 (왼쪽: 표 없음 / 오른쪽: 유니크 금 테)", font=font(15), fill=m.TEXT); y += 26
    board_before(2).lay(sheet, (pad, y), 2)
    d.text((pad + 330, y + 140), "→", font=font(40), fill=m.DIM)
    board_after(2).lay(sheet, (pad + 400, y), 2)
    board_after(2, rim=True).lay(sheet, (pad + 740, y), 2)
    y += 310 + 16
    d.text((pad, y), "2. 야영지 창 — A: 셋째 카드(재료가 갖춰진 때만)   B: 정비 걸음의 첫 줄   C: 담금질의 확인 걸음(정비처럼 두 걸음)", font=font(15), fill=m.TEXT); y += 26
    a = camp_three_cards(); b = camp_mend_row(); c = camp_confirm()
    s = 0.68
    a = a.resize((int(a.width * s), int(a.height * s)), Image.LANCZOS); b = b.resize((int(b.width * s), int(b.height * s)), Image.LANCZOS); c = c.resize((int(c.width * s), int(c.height * s)), Image.LANCZOS)
    sheet.paste(a.convert("RGBA"), (pad, y)); sheet.paste(b.convert("RGBA"), (pad + a.width + 12, y)); sheet.paste(c.convert("RGBA"), (pad + a.width + b.width + 24, y))
    y += a.height + 16
    d.text((pad, y), "3. 글라디우스의 카드 — 왼쪽: 지금의 카드(제목만 유니크 금)   오른쪽: 보관해 둔 명패 머리 카드(Round 42 목업 7)", font=font(15), fill=m.TEXT); y += 26
    cn = card_now(); cp = card_plate()
    sheet.paste(cn, (pad, y), cn); sheet.paste(cp, (pad + 440, y), cp)
    y += max(cn.height, cp.height) + 16
    d.text((pad, y), "4. 아이콘 자리: 롱소드의 날을 줄인 임시 그림(실제는 생성 1회 ≈ $0.02)", font=font(15), fill=m.TEXT); y += 26
    g = icon("gladius"); g2 = g.resize((g.width * 2, g.height * 2), Image.LANCZOS); sheet.paste(g2, (pad, y), g2)
    ls = icon("longsword"); ls2 = ls.resize((ls.width * 2, ls.height * 2), Image.LANCZOS); sheet.paste(ls2, (pad + 500, y), ls2)
    d.text((pad + 500, y + 170), "(비교: 롱소드)", font=font(13), fill=m.DIM)
    y += 200
    sheet = sheet.crop((0, 0, W, y))
    sheet.convert("RGB").save(HERE / "mock-forge.png"); print("wrote", HERE / "mock-forge.png")


if __name__ == "__main__":
    main()


# ---- the gladius's own stars (judgment 4: "중앙 상하로 인접한 1칸씩") ----------------------------------------------------------------

def gladius_stars_sheet():
    """Two shapes for a gladius that sharpens its neighbours like a 3성 whetstone: (a) 3×1 in the longsword's place, stars above and below its
    centre square; (b) 2×1, stars above and below its first square. A dagger under the star is lit (+4) in both."""
    chosen_marks()
    m.SIZES["gladius3"] = (3, 1)
    _i = m.icon
    m.icon = lambda n: gladius_icon() if n in ("gladius", "gladius3") else _i(n)
    pad = 12; W = 2 * 308 + 3 * pad + 20
    sheet = Image.new("RGBA", (W, 26 + 308 + 60), (0x1A, 0x18, 0x17, 255)); d = ImageDraw.Draw(sheet)
    d.text((pad, 4), "(a) 3×1: 롱소드 자리 그대로, 가운데 칸의 위·아래 ★        (b) 2×1: 첫 칸의 위·아래 ★   — 아래의 단검이 ★에 켜져 +4", font=font(14), fill=m.TEXT)
    for i, (name, cx) in enumerate((("gladius3", 1), ("gladius", 0))):
        b = m.Board(2)
        b.piece(name, 0, 0, tier=4, mark="B")
        unique_rim(b, b.square(0, 0, m.SIZES[name][0], 1))
        b.piece("dagger", 0, 1, tier=1, mark="B", fatigue=1)
        # the gladius's stars: above the centre (off the frame) and below it (on the dagger: lit)
        sq = b.square(cx, 1); c = ((sq[0] + sq[2]) / 2, (sq[1] + sq[3]) / 2)
        glow = Image.new("RGBA", b.img.size, (0, 0, 0, 0)); gd = ImageDraw.Draw(glow)
        gd.ellipse(((c[0] - 16) * b.k, (c[1] - 16) * b.k, (c[0] + 16) * b.k, (c[1] + 16) * b.k), fill=m.STAR_LIT + (70,)); b.img.alpha_composite(glow)
        b.star(c[0], c[1], 12, m.STAR_LIT, m.INK, 1.3)
        b.lay(sheet, (pad + i * (308 + 20), 26), 2)
    d.text((pad, 26 + 308 + 8), "글라디우스 3성의 ★ 효과 = 3성 숫돌과 같다(별에 놓인 근접 무기 피해 +4). 위쪽 ★은 틀 밖이라 (a)에서는 아래 하나만 쓰인다.", font=font(13), fill=m.DIM)
    sheet.convert("RGB").save(HERE / "mock-gladius-stars.png"); print("wrote", HERE / "mock-gladius-stars.png")


if __name__ == "__main__" and "--stars" in sys.argv:
    gladius_stars_sheet()
