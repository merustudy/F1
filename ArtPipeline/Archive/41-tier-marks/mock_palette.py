"""Round 41, third mockup: the COLOURS of the tiers above Bronze. The user found Silver, Gold and especially Diamond (a neon
cyan) out of place in the game's low-saturation palette. Three palettes, each shown as outline A on the dagger in a bone cell,
in a battle cell under the cooldown's dark, and as the words of the item's title on the dark panel with the reward card's
stripe. Twice the screen size. No API call.
  .venv/bin/python ArtPipeline/Archive/41-tier-marks/mock_palette.py
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import mock_tier_marks as marks  # noqa: E402
from mock_tier_marks import CELL_H, CELL_W, RIM, TEXT, font  # noqa: E402
from mock_outline import detail_cell  # noqa: E402

PANEL = (0x1E, 0x22, 0x2B); PALE = (0xEB, 0xEB, 0xE6); DIM = (0x9A, 0xA0, 0xAC)
CHARGE_DARK = (0x1E, 0x10, 0x06); CHARGE_EDGE = (0xFF, 0xCE, 0x68)

# name, note, marks (on the cell) and words (on the dark panels) per tier, and the tier's name in words.
PALETTES = [
    ("지금 (Round 35)", "은 #8C9CB2 · 금 #E2A21E · 다이아 #2EC4E8 — 채도가 높다. 다이아는 보호막의 하늘빛과 겹친다",
     {"Silver": (0x8C, 0x9C, 0xB2), "Gold": (0xE2, 0xA2, 0x1E), "Diamond": (0x2E, 0xC4, 0xE8)},
     {"Silver": (0xD5, 0xDE, 0xEA), "Gold": (0xF7, 0xC8, 0x4A), "Diamond": (0x6F, 0xE3, 0xF8)},
     {"Silver": "은", "Gold": "금", "Diamond": "다이아"}),
    ("P1 금속 셋: 강철 · 낡은 금 · 백금", "은 #7F8B9B · 금 #D4A232 · 다이아 #DCE6EA (글 #C6CFD9 · #F0C85A · #F1F7F9) — 모두 금속. 백금은 뼈색 위에서 옅다",
     {"Silver": (0x7F, 0x8B, 0x9B), "Gold": (0xD4, 0xA2, 0x32), "Diamond": (0xDC, 0xE6, 0xEA)},
     {"Silver": (0xC6, 0xCF, 0xD9), "Gold": (0xF0, 0xC8, 0x5A), "Diamond": (0xF1, 0xF7, 0xF9)},
     {"Silver": "은", "Gold": "금", "Diamond": "다이아"}),
    ("P2 금속 둘 + 깊은 보석: 강철 · 낡은 금 · 깊은 청록 (권장)", "은 #7F8B9B · 금 #D4A232 · 다이아 #3E7F96 (글 #C6CFD9 · #F0C85A · #7FC4D8) — 낮은 채도, 세 색상이 서로 멀다",
     {"Silver": (0x7F, 0x8B, 0x9B), "Gold": (0xD4, 0xA2, 0x32), "Diamond": (0x3E, 0x7F, 0x96)},
     {"Silver": (0xC6, 0xCF, 0xD9), "Gold": (0xF0, 0xC8, 0x5A), "Diamond": (0x7F, 0xC4, 0xD8)},
     {"Silver": "은", "Gold": "금", "Diamond": "다이아"}),
    ("P3 구리 · 은 · 금 (사용자 예시: 이름도 바꾼다)", "1단계 위 #A8683A · 2단계 위 #9AA7B8 · 3단계 위 #D4A232 (글 #D59A66 · #D3DBE4 · #F0C85A) — 기본은 표시 없음, 오른 셋이 동·은·금",
     {"Silver": (0xA8, 0x68, 0x3A), "Gold": (0x9A, 0xA7, 0xB8), "Diamond": (0xD4, 0xA2, 0x32)},
     {"Silver": (0xD5, 0x9A, 0x66), "Gold": (0xD3, 0xDB, 0xE4), "Diamond": (0xF0, 0xC8, 0x5A)},
     {"Silver": "동", "Gold": "은", "Diamond": "금"}),
]


def battle_dark(cell, k=2, charged=0.38):
    """The cell as battle shows it before the item has charged: the cooldown's dark over the part not charged yet (85%),
    the gold line and glow at the charge's front."""
    img = cell.copy(); w, h = img.size
    inner = 2 * k; split = int(inner + (w - 2 * inner) * charged)
    over = Image.new("RGBA", img.size, (0, 0, 0, 0)); d = ImageDraw.Draw(over)
    d.rectangle((split, inner, w - inner - 1, h - inner - 1), fill=CHARGE_DARK + (217,))
    for x in range(20 * k):                                   # the glow behind the front, fading to the left
        a = int(115 * (x / (20 * k)) ** 2)
        d.line([(split - 20 * k + x, inner), (split - 20 * k + x, h - inner - 1)], fill=CHARGE_EDGE + (a,))
    d.rectangle((split - k, inner, split + k - 1, h - inner - 1), fill=CHARGE_EDGE + (230,))
    img.alpha_composite(over)
    return img


def words(tier_word, mark, text, k=2, w=CELL_W * 2, h=54):
    """The item's title on the dark panel, the tier in its colour, with the reward card's stripe at the left."""
    img = Image.new("RGB", (w, h), PANEL); d = ImageDraw.Draw(img)
    d.rectangle((0, 0, 6 * k - 1, h - 1), fill=mark)
    x = 10 * k; y = h // 2; f = font(19 * k)
    for s, c in (("단검 · ", PALE), (tier_word, text), (" · 등급 8", PALE)):
        d.text((x, y), s, font=f, fill=c, anchor="lm"); x += d.textlength(s, font=f)
    return img


def main():
    k = 2; gap = 14; cw, ch = CELL_W * k, CELL_H * k; wh = 54
    row_h = 44 + ch + 6 + wh + 30
    cols = 6
    width = gap + cols * (cw + gap) + gap
    out = Image.new("RGB", (width, gap + len(PALETTES) * row_h + 150), (18, 18, 18)); d = ImageDraw.Draw(out)
    for r, (name, note, mark, text, names) in enumerate(PALETTES):
        RIM.update(mark); TEXT.update(text)
        y = gap + r * row_h
        d.text((gap, y + 4), name, font=font(26), fill=(235, 235, 235))
        d.text((gap + d.textlength(name, font=font(26)) + 24, y + 9), note, font=font(19), fill=(170, 170, 170))
        for i, tier in enumerate(("Silver", "Gold", "Diamond")):
            cell = detail_cell("A", tier, k=k, fatigue=False)
            x = gap + i * (cw + gap)
            out.paste(cell.convert("RGB"), (x, y + 44))
            out.paste(words(names[tier], mark[tier], text[tier]), (x, y + 44 + ch + 6))
            dark = battle_dark(cell, k)
            xd = gap + (3 + i) * (cw + gap) + gap
            out.paste(dark.convert("RGB"), (xd, y + 44))
            d.text((xd + cw / 2, y + 44 + ch + 6 + wh / 2), f"전투 · 충전 중 · {names[tier]}", font=font(18), fill=DIM, anchor="mm")
    foot = ["왼쪽 셋: 파티 쪽의 뼈색 칸 위 외곽선 A(2·3·4)와 그 아래 아이템 제목의 단계 글(어두운 패널)과 보상 카드의 띠. 오른쪽 셋: 전투에서 충전되지 않은 어둠이 덮인 칸.",
            "색이 쓰이는 곳: 칸의 단계 표시, 아이템 제목 '단검 · 은 · 등급 8', 보상 카드 왼쪽의 띠, '합치기 → 은', 정비 창의 '동 → 은'. 금의 글 색은 각성(강인·집중)의 금빛(`UiPalette.Virtue`)과 같다.",
            "P3은 색이 아니라 이름을 바꾸는 안이다: 기본 단계는 표시 없음, 오른 세 단계가 동·은·금(Design/02 §4의 '동·은·금·다이아'를 고친다). 이름을 두고 색만 구리·은·금으로 쓰면 '은'이 구리색이 되어 어긋난다."]
    for j, s in enumerate(foot): d.text((gap, out.height - 140 + j * 30), s, font=font(20), fill=(190, 190, 190))
    path = HERE / "mock3-colors.png"; out.save(path); print(path.name, out.size)


if __name__ == "__main__":
    main()
