"""Round 79 (2026-10-17 user: "드랍된 추가 인벤토리(가방)의 이미지를 좀 다르게 주고 싶어. 개선안 검토 후 보고"): mockups, no API call.
A bag shown as a thing (the loot on the floor, the merchant's tile, the hand) is Diablo's well today: a stone rim round black squares.
The mock lays three looks side by side on the game's own screenshots, using the herb pouch's icon as a stand-in for a bag picture:
  now   the well
  A     a picture of the bag (an icon like an item's), the squares only on the board
  D     the well with the bag's picture faint inside it
  .venv/bin/python ArtPipeline/Archive/79-bag-picture/mock_bag.py
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
sys.path.insert(0, str(HERE.parent / "71-lobby-expedition"))
import mock_lobby as m  # noqa: E402

CELL, GAP, RIM = 50, 2, 5
RIM_STONE = (0x22, 0x21, 0x1F); LEATHER = (0x46, 0x52, 0x3A)
RIM_COLOR = tuple(round(RIM_STONE[i] + (LEATHER[i] - RIM_STONE[i]) * 0.45) for i in range(3))
SQUARE = (0x09, 0x09, 0x0B); LINE = (0x38, 0x38, 0x3C); GOLD = (0xC7, 0xB3, 0x77); NAVY = (0x1F, 0x26, 0x54)
ICON = Image.open(ROOT / "Assets/@Art/Item/herb_pouch.png").convert("RGBA")


def well(cols, rows, alpha=255, icon_alpha=0):
    """Diablo's well: the stone rim tinted by the leather round black squares; optionally the bag's picture faint over the squares (D)."""
    w, h = cols * CELL + (cols - 1) * GAP + 2 * RIM, rows * CELL + (rows - 1) * GAP + 2 * RIM
    img = Image.new("RGBA", (w, h), RIM_COLOR + (alpha,)); d = ImageDraw.Draw(img)
    d.rectangle([RIM, RIM, w - RIM - 1, h - RIM - 1], fill=LINE + (alpha,))
    for r in range(rows):
        for c in range(cols):
            x, y = RIM + c * (CELL + GAP), RIM + r * (CELL + GAP)
            d.rectangle([x, y, x + CELL - 1, y + CELL - 1], fill=SQUARE + (alpha,))
    if icon_alpha:
        pic = picture(w - 2 * RIM - 10, h - 2 * RIM - 10)
        pic.putalpha(pic.getchannel("A").point(lambda v: v * icon_alpha // 255))
        img.alpha_composite(pic, (RIM + 5, RIM + 5))
    return img


def picture(w, h):
    """The stand-in picture (the herb pouch's icon) fitted into w x h."""
    r = min(w / ICON.width, h / ICON.height)
    return ICON.resize((round(ICON.width * r), round(ICON.height * r)), Image.LANCZOS)


def tilt(img, deg=-12):
    return img.rotate(deg, expand=True, resample=Image.BICUBIC)


def plate(d, cx, cy, s, px=20, color=GOLD, line=(0x55, 0x5C, 0x6B)):
    w = m.textw(s, px) + 24
    d.rectangle([cx - w / 2, cy - 16, cx + w / 2, cy + 16], fill=(0, 0, 0), outline=line, width=1)
    m.text(d, (cx, cy), s, px, color, "mm")


def floor_variant(base, kind):
    """The loot on the floor at the dagger's place of the base screenshot (x 1140, floor y 470): the well / the picture / the well with the faint picture."""
    img = base.copy()
    # cover the dagger with a patch of the floor beside it
    img.paste(base.crop((1200, 420, 1330, 500)), (1080, 420))
    if kind == "now":
        piece = tilt(well(3, 1))
    elif kind == "A":
        piece = tilt(picture(190, 110))
    else:
        piece = tilt(well(3, 1, icon_alpha=110))
    cx, foot = 1140, 475
    img.alpha_composite(piece, (round(cx - piece.width / 2), round(foot - piece.height)))
    d = ImageDraw.Draw(img)
    plate(d, cx, foot - piece.height - 26, "가죽 주머니" + ("  3×1" if kind != "now" else ""))
    return img.crop((960, 340, 1330, 520))


def shop_variant(base, kind):
    """The merchant's bag row (row 8 of the grid, the base shows it empty): the well tile / the picture on a leather tile."""
    img = base.copy()
    x0, y0 = 1042, 262 + 7 * 52  # the grid's first square and the eighth row
    if kind == "now":
        tile = well(3, 1)
        img.alpha_composite(tile, (x0 - RIM, y0 - RIM))
        tile2 = well(2, 1)
        img.alpha_composite(tile2, (x0 + 4 * 52 - RIM, y0 - RIM))
    else:
        for cols, x in ((3, x0), (2, x0 + 4 * 52)):
            w = cols * CELL + (cols - 1) * GAP
            d = ImageDraw.Draw(img)
            d.rectangle([x, y0, x + w - 1, y0 + CELL - 1], fill=RIM_COLOR)
            pic = picture(w - 10, CELL - 8)
            img.alpha_composite(pic, (x + (w - pic.width) // 2, y0 + (CELL - pic.height) // 2))
    d = ImageDraw.Draw(img)
    for cols, x, price in ((3, x0, "12"), (2, x0 + 4 * 52, "8")):
        w = cols * CELL + (cols - 1) * GAP
        m.text(d, (x + w - 4, y0 + CELL - 4), price, 15, GOLD, "rd")
    return img.crop((1030, 600, 1340, 700))


def hand_variant(kind):
    """The hand over the board (the base: a bag held, its green shadow where it would go): the well rides the pointer / the picture does."""
    base = Image.open(HERE / "base-held.png").convert("RGBA")
    img = base.crop((690, 620, 880, 980))
    piece = well(3, 1, alpha=215) if kind == "now" else picture(150, 60)
    img.alpha_composite(piece, (95 - piece.width // 2, 300 - piece.height // 2))
    d = ImageDraw.Draw(img)
    d.polygon([(92, 296), (92, 318), (97, 313), (101, 322), (105, 320), (101, 311), (108, 311)], fill=(240, 240, 240), outline=(0, 0, 0))
    return img


def main():
    loot = Image.open(HERE / "base-loot.png").convert("RGBA")
    shop = Image.open(HERE / "base-shop.png").convert("RGBA")
    cells = [
        ("바닥의 전리품 · 지금: 돌 테의 우물", floor_variant(loot, "now")),
        ("바닥의 전리품 · A: 가방의 그림 (이름판에 칸 수)", floor_variant(loot, "A")),
        ("바닥의 전리품 · D: 우물 안에 흐린 그림", floor_variant(loot, "D")),
        ("상점의 가방 줄 · 지금: 우물", shop_variant(shop, "now")),
        ("상점의 가방 줄 · A: 가죽 바탕 위의 그림", shop_variant(shop, "A")),
        ("손에 든 것 · 지금 / A (틀의 초록 그림자는 그대로)", Image.new("RGBA", (380, 360), (0, 0, 0, 0))),
    ]
    hand = cells[5][1]; hand.alpha_composite(hand_variant("now"), (0, 0)); hand.alpha_composite(hand_variant("A"), (190, 0))
    scale = 2.0
    ims = [(n, im.convert("RGB").resize((round(im.width * scale), round(im.height * scale)), Image.LANCZOS)) for n, im in cells]
    cols = 3; w = max(im.width for _, im in ims); h = max(im.height for _, im in ims); gap, lab = 24, 44
    out = Image.new("RGB", (cols * (w + gap) + gap, 2 * (h + lab + gap) + gap + 60), (18, 18, 18)); d = ImageDraw.Draw(out)
    for i, (name, im) in enumerate(ims):
        x = gap + (i % cols) * (w + gap); y = gap + (i // cols) * (h + lab + gap)
        m.text(d, (x, y + 6), name, 26, (235, 235, 235)); out.paste(im, (x, y + lab))
    m.text(d, (gap, out.height - 46), "그림은 약초 주머니 아이콘을 자리 표시로 쓴 것(진짜 가방 그림은 판정 뒤 생성: 허리 주머니 2×1, 가죽 주머니 3×1, 가죽 배낭 3×3). 보드 위의 가방은 어느 안에서도 지금의 우물(칸)이다.", 22, (190, 190, 190))
    out.save(HERE / "mock-bag-compare.png"); print("mock-bag-compare.png", out.size)


if __name__ == "__main__":
    main()
