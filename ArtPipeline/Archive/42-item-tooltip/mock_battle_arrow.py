"""Round 42, mockup 6: an arrow (notch) on the battle card too (user, 2026-10-07), drawn over the implemented boss-battle screenshot
(game/ko_40_battle_item_card_implemented.png: the party of four, the boss and two goblins, the overseer's maul card). Three looks: as
implemented (no notch), a notch on the card's side pointing at the cell, and the party side's way (above the panel, notch down).
No API call.
  .venv/bin/python ArtPipeline/Archive/42-item-tooltip/mock_battle_arrow.py
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import mock_tooltip as mt  # noqa: E402
from mock_tooltip import INK, BRASS, CARD_W, PAD, body_lines, text_height, draw_lines, shadowed, sheet, cell, PANEL_TOP, SCREEN  # noqa: E402

MAUL = mt.item("감독관의 망치", 27, cd="4.5", rows="앞에서 2번째 자리까지만 발동", effects=["맨 앞 적에게 피해 40"])
FIRE_STAFF = mt.item("화염 지팡이", 10, tier="bronze", cd="3.5", rows="뒤에서 3번째 자리까지만 발동", effects=["적 전체에게 피해 22"], fatigue="base")
LONGSWORD = mt.item("롱소드", 10, cd="3.0", rows="앞에서 2번째 자리까지만 발동", effects=["맨 앞 적에게 피해 11"], fatigue="base")
ENEMY_CELL = (1052, 646, 1232, 706)
NOTCH = 12                                             # how far the notch stands out


def card_with_notch(it, side=None, side_y=None, bottom_x=None):
    """The ink card (look 1) with a notch on one side (at side_y from the card's top) or under its bottom edge (at bottom_x).
    Returns the image and the card's top-left offset inside it (the notch needs a margin)."""
    inner_w = CARD_W - 2 * PAD - 8
    lines = body_lines(it, inner_w)
    H = text_height(lines) + 2 * PAD
    m = NOTCH + 2
    img = Image.new("RGBA", (CARD_W + 2 * m, H + 2 * m), (0, 0, 0, 0)); d = ImageDraw.Draw(img)
    x0, y0 = m, m
    d.rounded_rectangle((x0, y0, x0 + CARD_W - 1, y0 + H - 1), radius=4, fill=INK + (240,), outline=BRASS + (235,), width=1)
    d.rounded_rectangle((x0 + 1, y0 + 1, x0 + CARD_W - 2, y0 + H - 2), radius=3, outline=(255, 255, 255, 16), width=1)
    stripe = mt.TIER_MARK[it["tier"]]
    if stripe:
        d.rectangle((x0 + 2, y0 + 2, x0 + 7, y0 + H - 3), fill=stripe + (255,))
    if side is not None:
        y = y0 + max(14, min(H - 14, side_y))
        if side == "left":
            pts = [(x0, y - 10), (x0 - NOTCH, y), (x0, y + 10)]
            d.polygon(pts, fill=INK + (240,))
            d.line([(x0, y - 10), (x0 - NOTCH, y), (x0, y + 10)], fill=BRASS + (235,), width=1)
            d.line([(x0, y - 9), (x0, y + 9)], fill=INK + (240,), width=1)
        else:
            x1 = x0 + CARD_W - 1
            d.polygon([(x1, y - 10), (x1 + NOTCH, y), (x1, y + 10)], fill=INK + (240,))
            d.line([(x1, y - 10), (x1 + NOTCH, y), (x1, y + 10)], fill=BRASS + (235,), width=1)
            d.line([(x1, y - 9), (x1, y + 9)], fill=INK + (240,), width=1)
    if bottom_x is not None:
        nx = x0 + max(16, min(CARD_W - 16, bottom_x)); yb = y0 + H - 1
        d.polygon([(nx - 10, yb), (nx + 10, yb), (nx, yb + NOTCH)], fill=INK + (240,))
        d.line([(nx - 10, yb), (nx, yb + NOTCH), (nx + 10, yb)], fill=BRASS + (235,), width=1)
        d.line([(nx - 9, yb), (nx + 9, yb)], fill=INK + (240,), width=1)
    draw_lines(d, x0 + PAD + 8, y0 + PAD, lines, inner_w, BRASS + (110,))
    return img, (m, m), (CARD_W, H)


def put(shot, img, offset, left, top):
    """Lays the card so that its top-left corner is at (left, top) on the screenshot."""
    shadow, sm = shadowed(img)
    shot.alpha_composite(shadow, (left - offset[0] - sm, top - offset[1] - sm))


def beside(shot, it, cell_box, side, gap=10):
    """Look A: beside the board (as implemented), the notch on the card's side at the cell's middle height."""
    cx0, cy0, cx1, cy1 = cell_box
    probe, _, (w, h) = card_with_notch(it)
    left = cx1 + gap if side == "right" else cx0 - gap - w
    left = max(12, min(SCREEN[0] - 12 - w, left))
    if left < cx1 and left + w > cx0:
        # Pushed in from the screen's edge over its own board: the card goes to the other side of the board instead.
        side = "left" if side == "right" else "right"
        left = cx1 + gap if side == "right" else cx0 - gap - w
        left = max(12, min(SCREEN[0] - 12 - w, left))
    img, off, _ = card_with_notch(it, side="left" if side == "right" else "right", side_y=(cy1 - cy0) // 2)
    top = max(PANEL_TOP + 6, min(SCREEN[1] - 12 - h, cy0))
    put(shot, img, off, left, top)
    return (left, top, left + w, top + h)


def above(shot, it, cell_box, gap=8):
    """Look B: the party side's way — above the panel, centred on the cell's column, the notch down at the cell."""
    cx0, cy0, cx1, cy1 = cell_box
    cx = (cx0 + cx1) // 2
    probe, _, (w, h) = card_with_notch(it)
    left = max(12, min(SCREEN[0] - 12 - w, cx - w // 2))
    img, off, _ = card_with_notch(it, bottom_x=cx - left)
    put(shot, img, off, left, PANEL_TOP - gap - h)


def base():
    return Image.open(HERE / "game/ko_40_battle_item_card_implemented.png").convert("RGBA")


def without_card():
    """The implemented screenshot with the card painted out: the panel there is plain stone, sampled beside the card."""
    shot = base()
    patch = shot.crop((1660, 640, 1700, 820)).resize((420, 180), Image.LANCZOS)
    shot.alpha_composite(patch, (1236, 640))
    return shot


CROP = (60, 330, 1860, 1080)


def main():
    now = base()
    a = without_card(); beside(a, MAUL, ENEMY_CELL, "right")
    a2 = without_card(); beside(a2, FIRE_STAFF, cell(0, 0), "left")
    a3 = without_card(); beside(a3, LONGSWORD, cell(3, 0), "left")
    b = without_card(); above(b, MAUL, ENEMY_CELL); above(b, FIRE_STAFF, cell(0, 0))
    PARTY = (60, 330, 1000, 1080)
    sheet([("지금(구현): 적 카드는 보드 오른쪽 10에, 꼭지 없음", now.crop(CROP)),
           ("안 A: 보드 옆 그대로 + 카드 옆면의 꼭지가 누른 칸의 가운데 높이를 가리킴 (권장)", a.crop(CROP)),
           ("안 A, 아군이 넷일 때 4열의 지팡이: 왼쪽에 자리가 없어 오른쪽으로 넘어가고 3열·2열의 보드를 잠시 덮는다", a2.crop(PARTY)),
           ("안 A, 1열의 롱소드: 왼쪽에 서며 3열·2열의 보드를 잠시 덮는다", a3.crop(PARTY)),
           ("안 B: 파티 쪽과 같은 자리 — 패널 위에, 꼭지가 아래로 그 칸의 열을 가리킴 (적·아군 둘 다 그렸다. 한 번에 하나)", b.crop(CROP))],
          2, HERE / "mock6-battle-arrow.png",
          footer=["보스전 시작(0.0초)의 구현 장면 위에 그렸다. 안 A의 꼭지는 카드 옆면에 붙은 먹색 세모(놋쇠 테, 12 튀어나옴)이고 누른 칸의 가운데 높이에 선다; 안 B의 꼭지는 파티 쪽 카드의 것과 같다. 안 A에서 카드가 화면 끝에 밀려 자기 보드를 덮게 되면 보드의 반대쪽으로 넘어간다.",
                  "전투의 보드는 아군 넷 120~870, 적 1050~, 사이는 양초라 폭 400의 카드가 보드를 덮지 않고 설 자리는 패널 위(무대)뿐이다. 안 A는 카드가 누른 칸 옆에 붙어 '이 아이템의 카드'가 바로 읽히지만 이웃 보드를 잠시 덮고,",
                  "안 B는 보드를 덮지 않는 대신 무대의 유닛 아래쪽과 HP 막대를 잠시 덮고 카드가 칸에서 멀다(파티 쪽과 규칙이 하나가 된다). 카드는 클릭을 받지 않아 다음 클릭에 닫히는 것은 어느 안이나 같다."])

    # Detail: the two notches at twice the size.
    sa, offa, (w, h) = card_with_notch(MAUL, side="left", side_y=30)
    sb, offb, _ = card_with_notch(MAUL, bottom_x=200)
    det = []
    for label, img in (("안 A · 옆 꼭지 (칸의 가운데 높이)", sa), ("안 B · 아래 꼭지 (파티 쪽과 같음)", sb)):
        shadow, m = shadowed(img)
        bg = Image.new("RGBA", shadow.size, (0x2A, 0x30, 0x3C, 255)); bg.alpha_composite(shadow)
        det.append((label, bg.resize((bg.size[0] * 2, bg.size[1] * 2), Image.LANCZOS)))
    sheet(det, 2, HERE / "mock6-detail.png", footer=["2배 확대. 꼭지는 12 튀어나오고 밑변 20. 옆 꼭지는 누른 칸의 세로 가운데에 선다(카드가 위아래로 밀려도 칸을 따라간다)."])


if __name__ == "__main__":
    main()
