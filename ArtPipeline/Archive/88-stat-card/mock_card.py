# -*- coding: utf-8 -*-
"""Round 88: the item card's lines after the stat revision (2026-10-20 스탯 개정, Design/17 판정 11: "카드의 줄은 Round 88로 보임").
No base screenshot: the cards are drawn in the card's own colours (CARD_BG, TEXT, EFFECT, Good, BloodLight) at the card's width, text only,
because the card's frame and places do not change — only its lines do. Three columns: the card before, A (the implemented lines: the owner's
cooldown seconds coloured, "언제나 명중" for magic), B (the same with the percent in brackets after the seconds).
  .venv/bin/python ArtPipeline/Archive/88-stat-card/mock_card.py   -> mock-cards.png (the first sheet: before / A / B)
  .venv/bin/python ArtPipeline/Archive/88-stat-card/mock_card.py --v2   -> mock-cards-2.png (2026-10-20 사용자 "피해는 항목 중 가장 위쪽으로 배치 /
  마법 장비는 언제나 명중 빼기 / 괄호치고 스탯 및 곱 보여주는 항목 빼기": before / C — the effect lines first, no "언제나 명중", no stat tail)
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
FONT = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"

CARD_BG = (19, 19, 19)
PANEL = (34, 34, 34)
TEXT = (235, 235, 230)
DIM = (160, 160, 152)
EFFECT = (112, 112, 255)
GOOD = (88, 179, 104)          # UiPalette.Good
BLOOD_LIGHT = (216, 106, 90)   # UiPalette.BloodLight
BRASS = (199, 179, 119)
SHEET_BG = (10, 10, 10)

W, PAD, LINE = 360, 18, 30


def font(size):
    return ImageFont.truetype(str(FONT), size)


def card(draw, x, y, title, subtitle, facts, effects, tail=None):
    """One card: title, the fact lines, the effect lines (blue), a stat tail. Returns the height used. A fact may be a list of (text, colour) runs."""
    draw.rectangle((x, y, x + W, y + 40 + LINE * (len(facts) + len(effects) + (1 if tail else 0)) + 2 * PAD), fill=CARD_BG, outline=(60, 60, 60))
    draw.text((x + PAD, y + PAD), title, font=font(22), fill=BRASS)
    draw.text((x + PAD + 150, y + PAD + 4), subtitle, font=font(15), fill=DIM)
    cy = y + PAD + 40
    for fact in facts:
        runs = fact if isinstance(fact, list) else [(fact, TEXT)]
        cx = x + PAD
        for text, colour in runs:
            draw.text((cx, cy), text, font=font(17), fill=colour)
            cx += draw.textlength(text, font=font(17))
        cy += LINE
    for effect in effects:
        draw.text((x + PAD, cy), effect, font=font(17), fill=EFFECT)
        cy += LINE
    if tail:
        draw.text((x + PAD, cy), tail, font=font(15), fill=DIM)
        cy += LINE
    return cy - y + PAD


def card_v2(draw, x, y, title, subtitle, facts, effects, tail=None):
    """C: the effect lines (blue) come first, then the facts; no stat tail."""
    draw.rectangle((x, y, x + W, y + 40 + LINE * (len(facts) + len(effects)) + 2 * PAD), fill=CARD_BG, outline=(60, 60, 60))
    draw.text((x + PAD, y + PAD), title, font=font(22), fill=BRASS)
    draw.text((x + PAD + 150, y + PAD + 4), subtitle, font=font(15), fill=DIM)
    cy = y + PAD + 40
    for effect in effects:
        draw.text((x + PAD, cy), effect, font=font(17), fill=EFFECT)
        cy += LINE
    for fact in facts:
        runs = fact if isinstance(fact, list) else [(fact, TEXT)]
        cx = x + PAD
        for text, colour in runs:
            draw.text((cx, cy), text, font=font(17), fill=colour)
            cx += draw.textlength(text, font=font(17))
        cy += LINE
    return cy - y + PAD


def column(draw, x, y, heading, cards, draw_card=card):
    """A heading of up to two lines, then the cards."""
    cy = y
    for line in heading.split("\n"):
        draw.text((x, cy), line, font=font(18), fill=TEXT)
        cy += 26
    cy += 10
    for c in cards:
        cy += draw_card(draw, x, cy, *c) + 18
    return cy


def sheet():
    img = Image.new("RGB", (3 * W + 4 * PAD + 2 * 40, 1070), SHEET_BG)
    draw = ImageDraw.Draw(img)
    draw.text((PAD, 12), "Round 88 · 아이템 카드의 줄 (스탯 개정 뒤). 틀과 자리는 그대로, 줄만 바뀐다.", font=font(22), fill=TEXT)
    draw.text((PAD, 44), "주인: 방랑자 (힘 5 · 민첩 7 · 체력 4 · 지식 3 · 지혜 4 · 인내 6). 기준: 세기·쿨타임 5, 명중·치명타 10.", font=font(16), fill=DIM)

    before = [
        ("롱소드", "무기 장비 · 크기 3×1 · 근접", ["쿨다운 3초", [("치명타 ", TEXT), ("3.5%", BLOOD_LIGHT)], [("명중 ", TEXT), ("78%", BLOOD_LIGHT)], "앞에서 2번째 자리까지만 발동"],
         ["앞의 적에게 피해 1.34~2.68"], "(힘 8 ×0.8)"),
        ("화염 지팡이", "무기 장비 · 크기 3×1", ["쿨다운 3.5초", [("치명타 ", TEXT), ("3.5%", BLOOD_LIGHT)], [("명중 ", TEXT), ("77%", BLOOD_LIGHT)], "뒤에서 3번째 자리까지만 발동"],
         ["모든 적에게 피해 0.94~1.88"], "(지식 7 ×0.7)"),
        ("수호 부적", "지원 아이템 · 크기 2×1", ["쿨다운 6초"], ["HP가 가장 낮은 아군에게 보호막 1.38"], "(정신력 9 ×0.9)"),
    ]
    a = [
        ("롱소드", "무기 장비 · 크기 3×1 · 근접", [[("쿨다운 ", TEXT), ("2.94초", GOOD)], [("치명타 ", TEXT), ("2%", BLOOD_LIGHT)], [("명중 ", TEXT), ("77%", BLOOD_LIGHT)], "앞에서 2번째 자리까지만 발동"],
         ["앞의 적에게 피해 1.34~2.68"], None),
        ("화염 지팡이", "무기 장비 · 크기 3×1", [[("쿨다운 ", TEXT), ("3.54초", BLOOD_LIGHT)], [("치명타 ", TEXT), ("0%", BLOOD_LIGHT)], "언제나 명중", "뒤에서 3번째 자리까지만 발동"],
         ["모든 적에게 피해 1.07~2.14"], "(지식 3 ×0.8)"),
        ("수호 부적", "지원 아이템 · 크기 2×1", ["쿨다운 6초"], ["HP가 가장 낮은 아군에게 보호막 1.38"], "(지혜 4 ×0.9)"),
    ]
    b = [
        ("롱소드", "무기 장비 · 크기 3×1 · 근접", [[("쿨다운 ", TEXT), ("2.94초", GOOD), (" (−2%)", DIM)], [("치명타 ", TEXT), ("2%", BLOOD_LIGHT)], [("명중 ", TEXT), ("77%", BLOOD_LIGHT)], "앞에서 2번째 자리까지만 발동"],
         ["앞의 적에게 피해 1.34~2.68"], None),
        ("화염 지팡이", "무기 장비 · 크기 3×1", [[("쿨다운 ", TEXT), ("3.54초", BLOOD_LIGHT), (" (+1%)", DIM)], [("치명타 ", TEXT), ("0%", BLOOD_LIGHT)], "언제나 명중", "뒤에서 3번째 자리까지만 발동"],
         ["모든 적에게 피해 1.07~2.14"], "(지식 3 ×0.8)"),
        ("수호 부적", "지원 아이템 · 크기 2×1", ["쿨다운 6초"], ["HP가 가장 낮은 아군에게 보호막 1.38"], "(지혜 4 ×0.9)"),
    ]
    x = PAD
    column(draw, x, 84, "지금 (개정 전)\n정신력 · 쿨다운은 데이터의 초\n모든 무기가 명중을 굴림", before)
    x += W + 40
    column(draw, x, 84, "A 권장 · 구현\n쿨다운은 주인의 초 (빠르면 초록, 느리면 붉음)\n마법은 \"언제나 명중\"", a)
    x += W + 40
    column(draw, x, 84, "B\nA와 같되 초 뒤에 (−N%)를 덧붙임\n", b)
    draw.text((PAD, 1030), "수치는 보기용이다(방랑자의 출발값, 1성 아님). 색은 카드의 Good(초록)·BloodLight(붉음)·효과의 파랑 그대로.", font=font(15), fill=DIM)
    img.save(HERE / "mock-cards.png")
    print("wrote", HERE / "mock-cards.png")


def sheet_v2():
    """2026-10-20 사용자의 둘째 지시: the effect lines on top, no "언제나 명중" on magic equipment, no stat-and-multiplier tail."""
    img = Image.new("RGB", (2 * W + 3 * PAD + 40, 1250), SHEET_BG)
    draw = ImageDraw.Draw(img)
    draw.text((PAD, 12), "Round 88 (2차) · 아이템 카드의 줄: 효과를 맨 위에, 마법 장비는 명중 줄 없음, 괄호의 스탯·곱 없음.", font=font(20), fill=TEXT)
    draw.text((PAD, 44), "주인: 방랑자 (힘 5 · 민첩 7 · 체력 4 · 지식 3 · 지혜 4 · 인내 6). 틀과 자리는 그대로, 줄의 차례와 수만 바뀐다.", font=font(16), fill=DIM)
    before = [
        ("롱소드", "무기 장비 · 크기 3×1 · 근접", ["쿨다운 3초", [("치명타 ", TEXT), ("3.5%", BLOOD_LIGHT)], [("명중 ", TEXT), ("78%", BLOOD_LIGHT)], "앞에서 2번째 자리까지만 발동"],
         ["앞의 적에게 피해 1.34~2.68"], "(힘 8 ×0.8)"),
        ("메이스", "무기 장비 · 크기 3×1 · 근접", ["쿨다운 3.2초", [("치명타 ", TEXT), ("3.5%", BLOOD_LIGHT)], [("명중 ", TEXT), ("78%", BLOOD_LIGHT)], "앞에서 2번째 자리까지만 발동"],
         ["앞의 적에게 피해 0.66~1.98", "자신에게 보호막 0.98"], "(힘 8 ×0.8)"),
        ("화염 지팡이", "무기 장비 · 크기 3×1", ["쿨다운 3.5초", [("치명타 ", TEXT), ("3.5%", BLOOD_LIGHT)], [("명중 ", TEXT), ("77%", BLOOD_LIGHT)], "뒤에서 3번째 자리까지만 발동"],
         ["모든 적에게 피해 0.94~1.88"], "(지식 7 ×0.7)"),
        ("수호 부적", "지원 아이템 · 크기 2×1", ["쿨다운 6초"], ["HP가 가장 낮은 아군에게 보호막 1.38"], "(정신력 9 ×0.9)"),
    ]
    c = [
        ("롱소드", "무기 장비 · 크기 3×1 · 근접", [[("쿨다운 ", TEXT), ("2.94초", GOOD)], [("치명타 ", TEXT), ("2%", BLOOD_LIGHT)], [("명중 ", TEXT), ("77%", BLOOD_LIGHT)], "앞에서 2번째 자리까지만 발동"],
         ["앞의 적에게 피해 1.34~2.68"]),
        ("메이스", "무기 장비 · 크기 3×1 · 근접", [[("쿨다운 ", TEXT), ("3.14초", GOOD)], [("치명타 ", TEXT), ("2%", BLOOD_LIGHT)], [("명중 ", TEXT), ("77%", BLOOD_LIGHT)], "앞에서 2번째 자리까지만 발동"],
         ["앞의 적에게 피해 0.82~2.47", "자신에게 보호막 0.98"]),
        ("화염 지팡이", "무기 장비 · 크기 3×1", [[("쿨다운 ", TEXT), ("3.54초", BLOOD_LIGHT)], [("치명타 ", TEXT), ("0%", BLOOD_LIGHT)], "뒤에서 3번째 자리까지만 발동"],
         ["모든 적에게 피해 1.07~2.14"]),
        ("수호 부적", "지원 아이템 · 크기 2×1", ["쿨다운 6초"], ["HP가 가장 낮은 아군에게 보호막 1.38"]),
    ]
    x = PAD
    column(draw, x, 84, "지금 (개정 전)\n사실 줄이 위, 효과 줄이 아래, 괄호의 꼬리", before)
    x += W + 40
    column(draw, x, 84, "C 요청안 (구현할 것)\n효과 줄이 맨 위, 그 아래 쿨다운(주인의 초)·치명타·명중(물리만)·자리\n마법 장비는 명중 줄 없음, 꼬리 없음", c, card_v2)
    draw.text((PAD, 1214), "수치는 보기용이다(방랑자의 출발값, 1성 아님). 색은 카드의 Good(초록)·BloodLight(붉음)·효과의 파랑 그대로.", font=font(15), fill=DIM)
    img.save(HERE / "mock-cards-2.png")
    print("wrote", HERE / "mock-cards-2.png")


def sheet_v3():
    """3차 (2026-10-20 사용자 "수치는 소숫첨 첫째자리에서 반올림하자 / 보여주는 항목에서"): C with every number whole — the effect values as the card
    really draws them (Amount/Range were whole already: the earlier sheets' "1.34~2.68" was this mockup's error), the seconds and the percents rounded."""
    img = Image.new("RGB", (2 * W + 3 * PAD + 40, 1250), SHEET_BG)
    draw = ImageDraw.Draw(img)
    draw.text((PAD, 12), "Round 88 (3차) · 카드의 수치는 정수: 쿨다운 초·치명타·명중 %를 반올림, 효과 값은 전부터 정수.", font=font(20), fill=TEXT)
    draw.text((PAD, 44), "주인: 방랑자 (힘 5 · 민첩 7 · 체력 4 · 지식 3 · 지혜 4 · 인내 6). 왼쪽은 2차의 C(소수 그대로), 오른쪽은 3차(정수). 내부 계산은 그대로.", font=font(16), fill=DIM)
    c2 = [
        ("롱소드", "무기 장비 · 크기 3×1 · 근접", [[("쿨다운 ", TEXT), ("2.9초", GOOD)], [("치명타 ", TEXT), ("2%", BLOOD_LIGHT)], [("명중 ", TEXT), ("77%", BLOOD_LIGHT)], "앞에서 2번째 자리까지만 발동"],
         ["앞의 적에게 피해 1-3"]),
        ("단검", "무기 장비 · 크기 2×1 · 근접", [[("쿨다운 ", TEXT), ("1.5초", GOOD)], [("치명타 ", TEXT), ("7%", BLOOD_LIGHT)], [("명중 ", TEXT), ("77%", BLOOD_LIGHT)], "앞에서 2번째 자리까지만 발동"],
         ["앞의 적에게 피해 1"]),
        ("화염 지팡이", "무기 장비 · 크기 3×1", [[("쿨다운 ", TEXT), ("3.5초", BLOOD_LIGHT)], [("치명타 ", TEXT), ("0%", BLOOD_LIGHT)], "뒤에서 3번째 자리까지만 발동"],
         ["모든 적에게 피해 1-2"]),
        ("수호 부적", "지원 아이템 · 크기 2×1", ["쿨다운 6.0초"], ["HP가 가장 낮은 아군에게 보호막 1"]),
    ]
    c3 = [
        ("롱소드", "무기 장비 · 크기 3×1 · 근접", [[("쿨다운 ", TEXT), ("3초", GOOD)], [("치명타 ", TEXT), ("2%", BLOOD_LIGHT)], [("명중 ", TEXT), ("77%", BLOOD_LIGHT)], "앞에서 2번째 자리까지만 발동"],
         ["앞의 적에게 피해 1-3"]),
        ("단검", "무기 장비 · 크기 2×1 · 근접", [[("쿨다운 ", TEXT), ("1초", GOOD)], [("치명타 ", TEXT), ("7%", BLOOD_LIGHT)], [("명중 ", TEXT), ("77%", BLOOD_LIGHT)], "앞에서 2번째 자리까지만 발동"],
         ["앞의 적에게 피해 1"]),
        ("화염 지팡이", "무기 장비 · 크기 3×1", [[("쿨다운 ", TEXT), ("4초", BLOOD_LIGHT)], [("치명타 ", TEXT), ("0%", BLOOD_LIGHT)], "뒤에서 3번째 자리까지만 발동"],
         ["모든 적에게 피해 1-2"]),
        ("수호 부적", "지원 아이템 · 크기 2×1", ["쿨다운 6초"], ["HP가 가장 낮은 아군에게 보호막 1"]),
    ]
    x = PAD
    column(draw, x, 84, "2차 C (소수 그대로)\n쿨다운 2.9초 · 1.5초 · 3.5초, 치명타·명중은 0.5 단위", c2, card_v2)
    x += W + 40
    column(draw, x, 84, "3차 (정수, 구현할 것)\n쿨다운 3초 · 1초 · 4초, 치명타·명중은 정수로 반올림", c3, card_v2)
    draw.text((PAD, 1214), "효과 값(피해 1-3, 보호막 1)은 전부터 정수다. 단검 1.46초는 1초, 화염 지팡이 3.54초는 4초로 보인다.", font=font(15), fill=DIM)
    img.save(HERE / "mock-cards-3.png")
    print("wrote", HERE / "mock-cards-3.png")


if __name__ == "__main__":
    if "--v3" in sys.argv:
        sheet_v3()
    elif "--v2" in sys.argv:
        sheet_v2()
    else:
        sheet()
