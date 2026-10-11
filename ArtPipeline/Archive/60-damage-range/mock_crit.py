# -*- coding: utf-8 -*-
"""Round 60 / stage 22 (the user, 2026-10-11: "치명타는 맞는 대상마다 굴림", "치명타 크기는 범위 내에서 결정된 피해의 1.5배로"):
how a critical hit rises from the unit it hit, next to today's hit numbers, and the item card with a damage range and a crit chance,
over today's boss battle (game/ko_40_before.png, screenshots 2026-10-10 r58). The new numbers are Round 60's (longsword 2-4).
No API call.
  .venv/bin/python ArtPipeline/Archive/60-damage-range/mock_crit.py
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent / "49-inventory-style"))
import mock_inventory_style as S  # noqa: E402

BASE = Image.open(HERE / "game" / "ko_40_before.png").convert("RGB")
RED, GOLD, INK, WHITE, BLUE, DIM, BAR = (226, 76, 60), (255, 196, 77), (16, 12, 10), (238, 238, 238), (112, 112, 255), (176, 168, 150), (129, 20, 20)
TIP_BG, TIP_EDGE = (19, 19, 19), (70, 70, 70)


def F(px): return ImageFont.truetype(str(S.PRET), px)


def outlined(d, xy, text, font, fill, shadow=INK, anchor="ms", off=2):
    x, y = xy
    d.text((x + off, y + off), text, font=font, fill=shadow, anchor=anchor)
    d.text((x, y), text, font=font, fill=fill, anchor=anchor)


def new_numbers(im):
    """Round 60's HP over today's bars, the knight's shield 20 -> 5, and no tooltip (the cards are drawn below)."""
    d = ImageDraw.Draw(im)
    hp = {210: "22/22", 400: "25/25", 590: "27/27", 780: "38/38", 1140: "120/120", 1330: "41/41", 1520: "30/30"}
    for x, text in hp.items():
        d.rectangle([x - 70, 565, x + 70, 581], fill=BAR)
        d.text((x, 573), text, font=F(15), fill=WHITE, anchor="mm")
    d.ellipse([688, 563, 708, 583], fill=(60, 60, 64))
    d.text((698, 573), "5", font=F(13), fill=WHITE, anchor="mm")
    d.rectangle([1176, 646, 1576, 795], fill=(46, 45, 47))  # today's tooltip out (the panel's grey)
    return im


def card(d, x, y, w, lines):
    """Diablo's dark card: title, facts, effects (round 57's tooltip look)."""
    h = 22 + sum(size + 10 for _, _, size in lines)
    d.rectangle([x, y, x + w, y + h], fill=TIP_BG, outline=TIP_EDGE)
    cy = y + 14
    for text, color, size in lines:
        d.text((x + w // 2, cy), text, font=F(size), fill=color, anchor="mt")
        cy += size + 10
    return h


# 1. the floats: four panels of the stage round the boss (crop 960 x 540 from x 760, y 80), today and three looks for a crit
def stage_panel(label, sub, draws):
    im = new_numbers(BASE.copy()).crop((760, 80, 1720, 620))
    d = ImageDraw.Draw(im)
    for draw in draws:
        draw(d)
    d.rectangle([0, 0, 959, 539], outline=(90, 84, 70), width=3)
    d.rectangle([12, 12, 12 + 18 * len(label) + 40, 52], fill=(20, 18, 16))
    d.text((24, 32), label, font=F(26), fill=GOLD if "권장" in label else WHITE, anchor="lm")
    d.text((24, 520), sub, font=F(18), fill=DIM, anchor="lm")
    return im


BOSS, VET = (380, 150), (570, 230)   # above the boss's and the veteran's heads, in crop coordinates


def normal(d):  # today's hit: red, 34 px x 1.05
    outlined(d, VET, "-3", F(36), RED)


def heavy(d):   # today's heavy hit (20% of max HP or more): red x 1.4
    outlined(d, BOSS, "-15", F(48), RED)


def crit_a(d):  # A: gold number x 1.6 with "!", the word above
    outlined(d, (BOSS[0], BOSS[1] - 62), "치명타", F(26), GOLD)
    outlined(d, BOSS, "-5!", F(58), GOLD, off=3)


def crit_b(d):  # B: today's red, bigger, with "!"
    outlined(d, BOSS, "-5!", F(58), RED, off=3)


def crit_c(d):  # C: gold number x 1.6, no word
    outlined(d, BOSS, "-5", F(58), GOLD, off=3)


panels = [
    stage_panel("지금", "보통 피해 -3(빨강), 큰 피해 -15(빨강 1.4배)", [normal, heavy]),
    stage_panel("안 A (권장) 금빛 + \"치명타\"", "치명타 = 굴린 피해 × 1.5. 금빛 1.6배, 위에 작은 \"치명타\", 보통 피해는 빨강 그대로", [normal, crit_a]),
    stage_panel("안 B 빨강 + !", "색은 그대로 크게만. 큰 피해(1.4배)와 잘 갈리지 않는다", [normal, crit_b]),
    stage_panel("안 C 금빛 숫자만", "글 없이 금빛 1.6배. 처음 보는 사람에게 뜻이 덜 드러난다", [normal, crit_c]),
]
sheet = Image.new("RGB", (1920, 1080), (12, 12, 12))
for i, p in enumerate(panels):
    sheet.paste(p, ((i % 2) * 960, (i // 2) * 540))
sheet.save(HERE / "mock-crit-floats.png")

# 2. the cards: today's boss battle with Round 60's HP, and two cards (the knight's longsword, the boss's maul)
im = new_numbers(BASE.copy())
d = ImageDraw.Draw(im)
card(d, 560, 640, 420, [
    ("롱소드", WHITE, 22),
    ("무기 장비 / 크기 3×1 / 근접 / 쿨다운 3.0초", WHITE, 17),
    ("치명타 5% / 앞에서 2번째 자리까지만 발동", WHITE, 17),
    ("맨 앞 적에게 피해 2-4", BLUE, 19),
    ("기본 무기: 피로 없음", (180, 140, 230), 16),
])
card(d, 1177, 648, 396, [
    ("감독관의 망치", WHITE, 22),
    ("무기 장비 / 크기 2×1 / 근접 / 쿨다운 4.5초", WHITE, 17),
    ("치명타 5% / 앞에서 2번째 자리까지만 발동", WHITE, 17),
    ("맨 앞 적에게 피해 6-17", BLUE, 19),
])
d.rectangle([20, 1000, 900, 1060], fill=(20, 18, 16))
d.text((36, 1030), "카드: 등급 없음, \"피해 최소-최대\", 치명타 확률. HP는 새 수(기사 38, 감독관 120). 지금의 카드는 game/ko_40_before.png", font=F(17), fill=DIM, anchor="lm")
im.save(HERE / "mock-crit-card.png")
print("wrote mock-crit-floats.png, mock-crit-card.png")
