"""Round 42, mockup 7 (user, 2026-10-07): the card the way of look 3 — the name on a plate whose frame differs by tier (the gold tier
is the gold-lined iron plate of the first mockup), an ink body under it, and the notch as implemented. Next to the card as it is
(look 1). The tier plates are the game's plate_label recoloured (gold as is; silver, bronze and plain iron by hue), so no API call.
  .venv/bin/python ArtPipeline/Archive/42-item-tooltip/mock_plate.py
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import mock_tooltip as mt  # noqa: E402
import mock_battle_arrow as ba  # noqa: E402
from mock_tooltip import INK, BRASS, DIM, TEXT, CARD_W, PAD, body_lines, text_height, draw_lines, shadowed, sheet, cell, nine_slice, sprite, PANEL_TOP, SCREEN, font  # noqa: E402

TIER_TEXT = mt.TIER_TEXT
KO_TIER = {"common": "일반", "bronze": "동", "silver": "은", "gold": "금"}
HEAD_H = 56
NOTCH = ba.NOTCH

LONGSWORD = mt.item("롱소드", 10, cd="3.0", rows="앞에서 2번째 자리까지만 발동", effects=["맨 앞 적에게 피해 11"], fatigue="base")
DAGGER = mt.item("단검", 8, tier="bronze", cd="1.5", rows="앞에서 2번째 자리까지만 발동", effects=["맨 앞 적에게 피해 7"], fatigue=1, merge=True)
BUCKLER = mt.item("버클러", 8, tier="silver", cat="방어 장비", cd="5.0", rows="맨 앞에서만 발동", effects=["자신에게 보호막 19"], fatigue=1)
HERB = mt.item("약초 주머니", 8, tier="gold", cat="지원 아이템", cd="5.0", effects=["자신의 HP 19 회복"])
MAUL = ba.MAUL
SHOTS_R41 = Path.home() / "Library/Caches/F1/screenshots/20261007-r41b"


# ---- the tier plates: plate_label's gold lines turned to the tier's metal -----------------------------------------------------------

_PLATES = {}


def plate(tier):
    """The gold-lined iron plate as it is for gold; for the others its coloured (saturated) pixels are turned: copper for bronze, steel
    for silver, dull iron for common. The iron body (grey) stays."""
    if tier in _PLATES:
        return _PLATES[tier]
    src = sprite("plate_label")                      # 180x88 at the screen's scale
    rgb = src.convert("RGB"); alpha = src.split()[3]
    hsv = rgb.convert("HSV"); px = hsv.load()
    w, h = hsv.size
    for y in range(h):
        for x in range(w):
            hh, s, v = px[x, y]
            if s < 70 or v < 100:
                continue                         # only the bright lines; the dark iron body stays
            if tier == "bronze":
                px[x, y] = (int(20 / 360 * 255), min(255, int(s * 1.1)), int(v * 0.88))
            elif tier == "silver":
                px[x, y] = (int(215 / 360 * 255), int(s * 0.14), min(255, int(v * 1.08)))
            elif tier == "common":
                px[x, y] = (int(30 / 360 * 255), int(s * 0.1), int(v * 0.72))
    out = hsv.convert("RGB").convert("RGBA"); out.putalpha(alpha)
    _PLATES[tier] = out
    return out


def card_plate(it, notch=None, notch_at=None):
    """Look 3 revisited: the name on the tier's plate, "tier · grade" under it, the facts on an ink body; the notch as implemented
    (bottom for the party side, a side for the battle)."""
    tier = it["tier"]
    inner_w = CARD_W - 2 * PAD
    f_name, f_sub = font(24), font(19)
    lines = body_lines(it, inner_w, with_title=False)
    H = HEAD_H + 6 + f_sub.size + 8 + text_height(lines) + PAD
    m = NOTCH + 2
    img = Image.new("RGBA", (CARD_W + 2 * m, H + 2 * m), (0, 0, 0, 0)); d = ImageDraw.Draw(img)
    x0, y0 = m, m
    d.rounded_rectangle((x0, y0 + HEAD_H // 2, x0 + CARD_W - 1, y0 + H - 1), radius=4, fill=INK + (240,), outline=BRASS + (200,), width=1)
    head = nine_slice(plate(tier), 22, (CARD_W, HEAD_H), tiled=False)
    img.alpha_composite(head, (x0, y0)); d = ImageDraw.Draw(img)
    d.text((x0 + CARD_W / 2, y0 + HEAD_H / 2), it["name"], font=f_name, fill=TIER_TEXT[tier] + (255,), anchor="mm")
    y = y0 + HEAD_H + 6
    sub = [(KO_TIER[tier], TIER_TEXT[tier]), (" · ", DIM), (f"등급 {it['grade']}", DIM)] if tier != "common" else [(f"등급 {it['grade']}", DIM)]
    sub_w = sum(f_sub.getlength(t) for t, _ in sub)
    mt.draw_segments(d, x0 + (CARD_W - sub_w) / 2, y, sub, f_sub); y += f_sub.size + 8
    draw_lines(d, x0 + PAD, y, lines, inner_w, BRASS + (110,))
    if notch == "bottom":
        nx = x0 + max(16, min(CARD_W - 16, notch_at)); yb = y0 + H - 1
        d.polygon([(nx - 10, yb), (nx + 10, yb), (nx, yb + NOTCH)], fill=INK + (240,))
        d.line([(nx - 10, yb), (nx, yb + NOTCH), (nx + 10, yb)], fill=BRASS + (200,), width=1)
        d.line([(nx - 9, yb), (nx + 9, yb)], fill=INK + (240,), width=1)
    elif notch in ("left", "right"):
        yy = y0 + max(14, min(H - 14, notch_at))
        if notch == "left":
            d.polygon([(x0, yy - 10), (x0 - NOTCH, yy), (x0, yy + 10)], fill=INK + (240,))
            d.line([(x0, yy - 10), (x0 - NOTCH, yy), (x0, yy + 10)], fill=BRASS + (200,), width=1)
            d.line([(x0, yy - 9), (x0, yy + 9)], fill=INK + (240,), width=1)
        else:
            x1 = x0 + CARD_W - 1
            d.polygon([(x1, yy - 10), (x1 + NOTCH, yy), (x1, yy + 10)], fill=INK + (240,))
            d.line([(x1, yy - 10), (x1 + NOTCH, yy), (x1, yy + 10)], fill=BRASS + (200,), width=1)
            d.line([(x1, yy - 9), (x1, yy + 9)], fill=INK + (240,), width=1)
    return img, (m, m), (CARD_W, H)


def above_plate(shot, it, cell_box, gap=8):
    cx0, cy0, cx1, cy1 = cell_box
    cx = (cx0 + cx1) // 2
    _, _, (w, h) = card_plate(it)
    left = max(12, min(SCREEN[0] - 12 - w, cx - w // 2))
    img, off, _ = card_plate(it, notch="bottom", notch_at=cx - left)
    ba.put(shot, img, off, left, PANEL_TOP - gap - h)


def beside_plate(shot, it, cell_box, side, gap=10):
    cx0, cy0, cx1, cy1 = cell_box
    _, _, (w, h) = card_plate(it)
    left = cx1 + gap if side == "right" else cx0 - gap - w
    left = max(12, min(SCREEN[0] - 12 - w, left))
    top = max(PANEL_TOP + 6, min(SCREEN[1] - 12 - h, cy0))
    img, off, _ = card_plate(it, notch="left" if side == "right" else "right", notch_at=(cy1 - cy0) // 2)
    ba.put(shot, img, off, left, top)


def above_now(shot, it, cell_box, gap=8):
    cx0, cy0, cx1, cy1 = cell_box
    cx = (cx0 + cx1) // 2
    _, _, (w, h) = ba.card_with_notch(it)
    left = max(12, min(SCREEN[0] - 12 - w, cx - w // 2))
    img, off, _ = ba.card_with_notch(it, bottom_x=cx - left)
    ba.put(shot, img, off, left, PANEL_TOP - gap - h)


PARTY_CROP = (380, 340, 1300, 1080)
BATTLE_CROP = (560, 330, 1860, 1080)


def main():
    herb_cell = cell(3, 3)                          # Rowan's gold herb pouch on ko_35_map_tiers (before round 42: no card open)
    party_now = Image.open(SHOTS_R41 / "ko_35_map_tiers.png").convert("RGBA"); above_now(party_now, HERB, herb_cell)
    party_plate = Image.open(SHOTS_R41 / "ko_35_map_tiers.png").convert("RGBA"); above_plate(party_plate, HERB, herb_cell)
    battle_now = ba.without_card(); ba.beside(battle_now, MAUL, ba.ENEMY_CELL, "right")
    battle_plate = ba.without_card(); beside_plate(battle_plate, MAUL, ba.ENEMY_CELL, "right")
    sheet([("지금(안 1): 먹색 판 + 놋쇠 선, 단계는 왼쪽 띠 — 금 약초 주머니, 파티 쪽", party_now.crop(PARTY_CROP)),
           ("안 3 다시: 이름은 단계의 명패에(금 = 금선 쇠 명패), 아래 먹색 몸 — 같은 아이템", party_plate.crop(PARTY_CROP)),
           ("지금(안 1): 전투의 적 카드(일반), 옆 꼭지", battle_now.crop(BATTLE_CROP)),
           ("안 3 다시: 일반의 명패(장식 없는 무쇠 선) + 옆 꼭지 — 같은 자리", battle_plate.crop(BATTLE_CROP))],
          2, HERE / "mock7-compare.png",
          footer=["명패는 지금 헤더의 금선 쇠 명패(plate_label)이고, 단계마다 선의 금속만 다르다: 금은 그대로, 은은 강철, 동은 구리, 일반은 장식 없는 무쇠(어둡고 채도 없음). 이름의 글은 단계의 글 색.",
                  "명패 아래 '단계 · 등급'(일반은 '등급'만). 몸은 지금 카드와 같고 왼쪽 띠는 뺐다(명패가 단계를 말한다). 꼭지는 구현된 것 그대로(파티 쪽 아래, 전투 옆). 카드가 50 더 높다."])

    # The four tiers side by side, as now and as look 3, at 1.5x.
    items = [LONGSWORD, DAGGER, BUCKLER, HERB]
    rows = []
    for label, maker in (("지금(안 1)", lambda it: ba.card_with_notch(it)[0]), ("안 3 다시", lambda it: card_plate(it)[0])):
        for it in items:
            img = maker(it)
            shadow, _ = shadowed(img)
            bg = Image.new("RGBA", shadow.size, (0x2A, 0x30, 0x3C, 255)); bg.alpha_composite(shadow)
            bg = bg.resize((int(bg.size[0] * 1.5), int(bg.size[1] * 1.5)), Image.LANCZOS)
            rows.append((f"{label} · {it['name']} ({KO_TIER[it['tier']]})", bg))
    sheet(rows, 4, HERE / "mock7-tiers.png", lab=36, size=20,
          footer=["1.5배. 네 단계: 롱소드(일반, 기본 무기) / 단검(동, 합치기 안내) / 버클러(은) / 약초 주머니(금). 명패의 선: 일반 무쇠 · 동 구리 · 은 강철 · 금 금(지금 헤더의 것).",
                  "명패는 plate_label을 색만 바꾼 것이라 호출이 없다(ui_variants.py에 '선의 색을 바꾸는' 방식 하나). 단계마다 장식이 다른 명패를 바란다면 이미지 생성 4회로 그린다(승인 라운드)."])

    # The four plates alone at 2x.
    plates = []
    for tier in ("common", "bronze", "silver", "gold"):
        p = nine_slice(plate(tier), 22, (CARD_W, HEAD_H), tiled=False)
        bg = Image.new("RGBA", (p.size[0] + 40, p.size[1] + 40), (0x2A, 0x30, 0x3C, 255)); bg.alpha_composite(p, (20, 20))
        d = ImageDraw.Draw(bg); d.text((bg.size[0] / 2, 20 + HEAD_H / 2), {"common": "롱소드", "bronze": "단검", "silver": "버클러", "gold": "약초 주머니"}[tier], font=font(24), fill=TIER_TEXT[tier] + (255,), anchor="mm")
        plates.append((f"명패 · {KO_TIER[tier]}", bg.resize((bg.size[0] * 2, bg.size[1] * 2), Image.LANCZOS)))
    sheet(plates, 2, HERE / "mock7-plates.png", lab=36, size=20, footer=["2배. 폭 400·높이 56의 9-slice(Border 22). 글은 단계의 글 색(일반 #C9C2B0 · 동 #D59A66 · 은 #D3DBE4 · 금 #F0C85A)."])


if __name__ == "__main__":
    main()
