# -*- coding: utf-8 -*-
"""Round 87: the armour items, 철 갑옷 (2x2) and 철 투구 (2x1) (2026-10-19 사용자 "방어구 아이템 신설 검토" → "8. 철 갑옷, 철 투구로 생성 /
나머지 권장안 구현"). Drawn over the 2026-10-19 screenshots of 20261019-traits38. No art yet: the pieces are the game's own placeholder (the
item's blue piece with its name, ItemSlotView without an icon). No API call.
  base-shop.png = ko_42 (the shop and the mace's tooltip), base-shop-clean.png = ko_43 (the same shop, no tooltip: its top covers the old one),
  base-fatigue.png = ko_30 (the board's "피로 +2" pill), base-battle.png = ko_40 (0.0초 of the floor 31 battle, the warrior's shield 5),
  base-battle-clean.png = ko_09 (the same enemies' boards without a card).
  .venv/bin/python ArtPipeline/Archive/87-armor/mock_armor.py
With --icons (the review of the first icons, 2026-10-19 사용자 "이미지 api를 통한 아이콘 생성 보고"): the same two screens with the candidates
of candidates/ in the pieces (review-shop.png, review-battle.png) and the icons beside the shipped ones (review-icons.png).
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
FONT = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"

SQ, PITCH = 50, 52                  # a square and the step to the next (2px gaps)
SHOP = (1041, 266)                  # the merchant grid's first square
BOARD = (703, 646)                  # 로언's board (row 1), the same on the map and in battle

PIECE = (27, 37, 83)                # UiPalette.GridPiece
FLASH = (88, 112, 214)              # the piece lit as it activates (the cell's flash)
EMPTY = (12, 12, 12)
TEXT = (235, 235, 230)
EFFECT = (112, 112, 255)            # UiPalette.TooltipEffect
FATIGUE = (207, 186, 247)           # UiPalette.Fatigue
PRICE = (199, 179, 119)
TIP_BG = (21, 21, 21)
CARD_BG = (19, 19, 19)
SHIELD_BLUE = (120, 170, 255)       # the "방어 +N" pop (Architecture/12: 파란 "방어 +N")
ART_MARGIN = 5                      # ItemSlotView.ArtMargin: the icon's margin inside its piece
# The second round (2026-10-19 판정 "1. 재생성 2. 확정"): the regenerated armour beside the approved helm. The first armour stays in candidates/.
ICONS = {"철 갑옷": HERE / "candidates/iron_armor_v2.png", "철 투구": HERE / "approved/iron_helm.png", "철\n투구": HERE / "approved/iron_helm.png"}
USE_ICONS = "--icons" in sys.argv


def font(size):
    return ImageFont.truetype(str(FONT), size)


def square(origin, x, y):
    return origin[0] + x * PITCH, origin[1] + y * PITCH


def piece(draw, origin, x, y, w, h, name, colour=PIECE, size=15, ground=True):
    left, top = square(origin, x, y)
    right, bottom = left + (w - 1) * PITCH + SQ, top + (h - 1) * PITCH + SQ
    if ground:
        draw.rectangle((left, top, right - 1, bottom - 1), fill=colour)
    if USE_ICONS:
        icon = Image.open(ICONS[name]).convert("RGBA")
        icon = icon.crop(icon.getbbox())
        if h > w and icon.width > icon.height:
            icon = icon.rotate(-90, expand=True)     # turned upright on the board (a quarter clockwise)
        box_w, box_h = right - left - 2 * ART_MARGIN, bottom - top - 2 * ART_MARGIN
        scale = min(box_w / icon.width, box_h / icon.height)
        icon = icon.resize((max(1, round(icon.width * scale)), max(1, round(icon.height * scale))), Image.LANCZOS)
        draw._image.paste(icon, (int((left + right - icon.width) / 2), int((top + bottom - icon.height) / 2)), icon)
    else:
        draw.multiline_text(((left + right) / 2, (top + bottom) / 2), name, font=font(size), fill=TEXT, anchor="mm", align="center", spacing=2)
    return left, top, right, bottom


def fatigue_mark(draw, box):
    """The piece's "+1" (round 32 B1): a violet ring in its top-right corner."""
    right, top = box[2], box[1]
    draw.ellipse((right - 22, top + 3, right - 3, top + 22), outline=FATIGUE, width=2, fill=(40, 30, 60))
    draw.text((right - 12.5, top + 12.5), "+1", font=font(11), fill=FATIGUE, anchor="mm")


def price(draw, box, value):
    draw.text((box[2] - 4, box[3] - 3), str(value), font=font(14), fill=TEXT, anchor="rd", stroke_width=2, stroke_fill=(0, 0, 0))


def lines_box(draw, centre_x, top, lines, line_height, bg, width=None, border=(64, 64, 64)):
    """Centred lines on a dark box (the Diablo II tooltip, round 49; the battle card, round 42). lines = [(text, colour, size)]."""
    widest = max(draw.textlength(text, font=font(size)) for text, _, size in lines)
    width = width or int(widest) + 28
    height = len(lines) * line_height + 18
    left = int(centre_x - width / 2)
    draw.rectangle((left, top, left + width, top + height), fill=bg, outline=border)
    y = top + 9 + line_height / 2
    for text, colour, size in lines:
        draw.text((centre_x, y), text, font=font(size), fill=colour, anchor="mm")
        y += line_height
    return left, top, left + width, top + height


def board(draw, flash=False):
    """로언 (warrior, row 1): the base longsword, 철 갑옷 under it on the left, 철 투구 turned upright on the right — the 3x3 pack full."""
    for x, y in [(0, 1), (1, 1), (2, 1), (0, 2), (1, 2), (2, 2)]:
        left, top = square(BOARD, x, y)
        draw.rectangle((left, top, left + SQ - 1, top + SQ - 1), fill=EMPTY)
    colour = FLASH if flash else PIECE
    # In battle a piece has no blue ground (round 49): the icon lies on its black squares, lit as it fires.
    armour = piece(draw, BOARD, 0, 1, 2, 2, "철 갑옷", colour, ground=not (USE_ICONS and flash))
    helm = piece(draw, BOARD, 2, 1, 1, 2, "철\n투구", colour, ground=not (USE_ICONS and flash))
    return armour, helm


def shop():
    image = Image.open(HERE / "base-shop.png").convert("RGB")
    clean = Image.open(HERE / "base-shop-clean.png").convert("RGB")
    image.paste(clean.crop((985, 0, 1240, 262)), (985, 0))           # the mace's tooltip goes
    pill = Image.open(HERE / "base-fatigue.png").convert("RGB").crop((806, 622, 870, 645))
    draw = ImageDraw.Draw(image)

    armour = piece(draw, SHOP, 0, 2, 2, 2, "철 갑옷")
    price(draw, armour, 20)
    helm = piece(draw, SHOP, 2, 2, 2, 1, "철 투구")
    price(draw, helm, 12)

    # The board: the two pieces bought onto 로언 and the head's sum, "피로 +2" (each armour +1; the base weapon 0).
    for box in board(draw):
        fatigue_mark(draw, box)
    image.paste(pill, (806, 622))

    # The helmet's tooltip, hovered in the shop (round 49's lines; the new ones are "전투 시작 때 발동" and "받는 치명타 확률 -3%p").
    lines = [
        ("철 투구", TEXT, 15), ("방어 장비", TEXT, 13), ("크기 2×1", TEXT, 13), ("전투 시작 때 발동", TEXT, 13),
        ("자신에게 보호막 2", EFFECT, 13), ("받는 치명타 확률 -3%p", EFFECT, 13), ("전투마다 피로 +1", FATIGUE, 13), ("값 12 코인", PRICE, 13),
    ]
    lines_box(draw, (helm[0] + helm[2]) / 2, helm[1] - 176, lines, 18.5, TIP_BG, border=(78, 71, 69))
    image.save(HERE / ("review-shop.png" if USE_ICONS else "mock-shop.png"))


def battle():
    image = Image.open(HERE / "base-battle.png").convert("RGB")
    clean = Image.open(HERE / "base-battle-clean.png").convert("RGB")
    image.paste(clean.crop((1040, 640, 1620, 830)), (1040, 640))      # the overseer's maul card goes
    draw = ImageDraw.Draw(image)

    # 0.0초: both pieces activate once and light up; 로언's shield 5 (선봉) + 5 (갑옷) + 2 (투구) = 13 (5.45 + 5.45 + 2.18 = 13.08).
    armour, helm = board(draw, flash=True)
    for box in (armour, helm):
        draw.rectangle(box, outline=(170, 190, 255), width=2)
    draw.rounded_rectangle((690, 563, 706, 584), radius=3, fill=(52, 62, 80))
    draw.text((698, 574), "13", font=font(12), fill=TEXT, anchor="mm", stroke_width=1, stroke_fill=(0, 0, 0))
    draw.text((780, 290), "방어 +5", font=font(34), fill=SHIELD_BLUE, anchor="mm", stroke_width=3, stroke_fill=(10, 14, 30))
    draw.text((780, 240), "방어 +2", font=font(34), fill=SHIELD_BLUE, anchor="mm", stroke_width=3, stroke_fill=(10, 14, 30))

    # The helmet's card in battle (round 42: facts on one line, the effects, the fatigue).
    lines = [
        ("철 투구", TEXT, 20), ("방어 장비 / 크기 2×1 / 전투 시작 때 발동", TEXT, 16),
        ("자신에게 보호막 2", EFFECT, 17), ("받는 치명타 확률 -3%p", EFFECT, 17), ("전투마다 피로 +1", FATIGUE, 16),
    ]
    lines_box(draw, 1072, 650, lines, 30, CARD_BG, width=372, border=(40, 40, 40))
    image.save(HERE / ("review-battle.png" if USE_ICONS else "mock-battle.png"))


def icons_sheet():
    """The two candidates at twice the screen and at the screen, on the slot's dark ground, beside shipped icons of the same squares."""
    sheet = Image.new("RGB", (1240, 560), (34, 32, 34))
    draw = ImageDraw.Draw(sheet)
    shipped = ROOT / "Assets/@Art/Item"
    rows = [("철 갑옷 2×2 (재생성)", HERE / "candidates/iron_armor_v2.png", 2, 2), ("철 투구 2×1 (확정)", HERE / "approved/iron_helm.png", 2, 1),
            ("버클러 1×1 (지금)", shipped / "buckler.png", 1, 1), ("창 2×2 (지금)", shipped / "spear.png", 2, 2), ("단검 2×1 (지금)", shipped / "dagger.png", 2, 1)]
    x = 20
    for label, path, w, h in rows:
        for scale in (2, 1):
            pw, ph = ((w - 1) * PITCH + SQ) * scale, ((h - 1) * PITCH + SQ) * scale
            top = 60 if scale == 2 else 340
            draw.rectangle((x, top, x + pw - 1, top + ph - 1), fill=PIECE)
            icon = Image.open(path).convert("RGBA")
            icon = icon.crop(icon.getbbox())
            box_w, box_h = pw - 2 * ART_MARGIN * scale, ph - 2 * ART_MARGIN * scale
            k = min(box_w / icon.width, box_h / icon.height)
            icon = icon.resize((round(icon.width * k), round(icon.height * k)), Image.LANCZOS)
            sheet.paste(icon, (x + (pw - icon.width) // 2, top + (ph - icon.height) // 2), icon)
        draw.text((x, 30), label, font=font(16), fill=TEXT)
        x += ((w - 1) * PITCH + SQ) * 2 + 40
    draw.text((20, 300), "위: 화면의 2배 / 아래: 화면 크기 (조각의 파란 바탕, 여백 5)", font=font(15), fill=PRICE)
    sheet.save(HERE / "review-icons.png")


if __name__ == "__main__":
    shop()
    battle()
    if USE_ICONS:
        icons_sheet()
