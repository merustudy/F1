# -*- coding: utf-8 -*-
"""Round 81: where the six stats show (Slice D, 31단계 판정 13 "목업 뒤 구현"). Drawn over the screens as they are
(base-*.png = the 2026-10-18 screenshots ko_57, ko_58, ko_66, ko_11, ko_47, ko_05): the barracks rows (two ways), the coach's
candidates with their spread from the job, the party popup's stage and list, the item card counted with its owner's stats,
the miss in battle, and the growth line in the settlement. Shapes and text only — no API call.
  .venv/bin/python ArtPipeline/Archive/81-stat-display/mock_stats.py
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
FONT = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"

# UiPalette
TEXT = (0xEB, 0xEB, 0xE6); DIM = (0x9A, 0xA0, 0xAC); GOOD = (0x58, 0xB3, 0x68); LOW = (0xD8, 0x6A, 0x5A)
BONE = (0xCD, 0xC0, 0xA0); GOLD = (0xC7, 0xB3, 0x77); STAR = (0xEC, 0xC4, 0x5C); TIER3 = (0xFF, 0xFF, 0x6E)
EFFECT = (0x7B, 0x7B, 0xFF); FATIGUE = (0xCF, 0xBA, 0xF7); LINE = (0x55, 0x5C, 0x6B); QUIET = (0x3A, 0x41, 0x50)
ROW = (0x2A, 0x31, 0x3D); WINDOW = (0x1C, 0x22, 0x2A); STAGE = (0x16, 0x16, 0x1C); PANEL = (0x1E, 0x22, 0x2B)
TIP = (0x13, 0x13, 0x13); TIP_EDGE = (0x27, 0x27, 0x27); TIP_LINE = (0x40, 0x40, 0x40)

BASELINE = 10
LABELS = ["힘", "민첩", "체력", "지식", "정신력", "인내"]
# Job base values (Design/05 §2, 출발값 14 §11-2): str dex con int spi end
JOB = {
    "knight": [12, 9, 12, 6, 9, 11], "valkyrie": [13, 11, 10, 5, 8, 9], "paladin": [11, 8, 12, 8, 12, 11],
    "bishop": [7, 8, 9, 11, 14, 10], "archmage": [6, 9, 7, 15, 11, 8], "spellblade": [10, 12, 9, 12, 9, 9],
}
# The starting roster in the barracks' order (no spread: job base).
ROSTER = [("아스트리드", "valkyrie"), ("세드릭", "paladin"), ("엘라", "bishop"), ("카이", "spellblade"), ("미라", "archmage"), ("로언", "knight")]
# The coach's three with an example spread of -2..+2 (Design/04 §6).
CANDIDATES = [("knight", [2, -1, 0, 1, 0, 1]), ("valkyrie", [-1, 2, 1, 0, -1, 0]), ("archmage", [0, 1, -2, 2, 1, 0])]


def font(px): return ImageFont.truetype(str(FONT), int(round(px)))


def label(d, xy, s, px, color=TEXT, anchor="la"):
    d.text(xy, s, font=font(px), fill=color, anchor=anchor)


def width(s, px): return font(px).getlength(s)


def tone(value):
    """Above the baseline bright green, at it plain, below it a soft red (권장: 기준 대비 색)."""
    return GOOD if value > BASELINE else LOW if value < BASELINE else TEXT


def stat_pair(d, x, y, name, value, px=18, gap=6, anchor_y="m"):
    """'힘 12': the name dim, the value toned. Returns the x after it."""
    label(d, (x, y), name, px, DIM, "l" + anchor_y)
    x += width(name, px) + gap
    label(d, (x, y), str(value), px, tone(value), "l" + anchor_y)
    return x + width(str(value), px)


def caption(img, text):
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    d.rectangle((0, 1080 - 64, 1920, 1080), fill=(0, 0, 0, 215))
    label(d, (24, 1080 - 32), text, 30, TEXT, "lm")
    return Image.alpha_composite(img, layer)


def load(name): return Image.open(HERE / name).convert("RGBA")


# ---------------------------------------------------------------- barracks
BARRACKS_ROWS = [266 + 100 * i for i in range(6)]


def barracks_a():
    """안 A: the row buttons stay; the level block narrows and a 3×2 stat grid stands beside it."""
    img = load("base-barracks.png")
    d = ImageDraw.Draw(img)
    levels = [("Lv 2", "경험치 4/25", 4 / 25)] + [("Lv 1", "경험치 0/15", 0)] * 5
    for (name, job), y0, (lv, exp, fill) in zip(ROSTER, BARRACKS_ROWS, levels):
        d.rectangle((1196, y0 + 2, 1680, y0 + 90), fill=ROW)
        label(d, (1204, y0 + 30), lv, 26, BONE, "lm")
        label(d, (1270, y0 + 32), exp, 16, DIM, "lm")
        d.rectangle((1204, y0 + 56, 1384, y0 + 64), fill=STAGE)
        if fill > 0:
            d.rectangle((1204, y0 + 56, 1204 + int(180 * fill), y0 + 64), fill=BONE)
        d.line((1398, y0 + 12, 1398, y0 + 80), fill=LINE, width=1)
        values = JOB[job]
        for i in range(6):
            cx = 1412 + (i % 3) * 90
            cy = y0 + 28 + (i // 3) * 36
            stat_pair(d, cx, cy, LABELS[i], values[i], 18, 5)
    return img


def barracks_b():
    """안 B (권장): the row buttons (1~4열·제외) leave the barracks — the party popup forms the party (Round 75's own
    recommendation) — and the six stats take their place as a sheet: name over value."""
    img = load("base-barracks.png")
    d = ImageDraw.Draw(img)
    for (name, job), y0 in zip(ROSTER, BARRACKS_ROWS):
        d.rectangle((700, y0 + 4, 1188, y0 + 88), fill=ROW)
        values = JOB[job]
        for i in range(6):
            x0 = 712 + i * 78
            if i:
                d.line((x0, y0 + 16, x0, y0 + 76), fill=QUIET, width=1)
            cx = x0 + 39
            label(d, (cx, y0 + 28), LABELS[i], 18, DIM, "mm")
            label(d, (cx, y0 + 62), str(values[i]), 30, tone(values[i]), "mm")
    return img


# ---------------------------------------------------------------- coach
COACH_OLD = [338, 456, 574]
COACH_NEW = [338, 496, 654]


def coach():
    """The candidates' rows grow by a stat line: the value toned against the baseline, and the spread from the job's
    own value as a small ▲/▼ (the hiring decision: 'a stronger knight than most')."""
    base = load("base-coach.png")
    img = base.copy()
    d = ImageDraw.Draw(img)
    d.rectangle((494, 330, 1426, 860), fill=WINDOW)
    for (job, spread), oy, ny in zip(CANDIDATES, COACH_OLD, COACH_NEW):
        img.paste(base.crop((494, oy, 1426, oy + 110)), (494, ny))
        d.rectangle((494, ny + 110, 1426, ny + 150), fill=ROW)
        button = base.crop((1194, oy + 28, 1406, oy + 84))
        d.rectangle((1188, ny + 20, 1420, ny + 92), fill=ROW)
        img.paste(button, (1194, ny + 47))
        values = [b + s for b, s in zip(JOB[job], spread)]
        x = 514
        for i in range(6):
            end = stat_pair(d, x, ny + 120, LABELS[i], values[i], 20, 6)
            if spread[i]:
                mark = ("▲" if spread[i] > 0 else "▼") + str(abs(spread[i]))
                label(d, (end + 3, ny + 113), mark, 14, GOOD if spread[i] > 0 else LOW, "lm")
            x += 106
        d.line((514, ny + 102, 1170, ny + 102), fill=QUIET, width=1)
    return img


# ---------------------------------------------------------------- party popup
STAGE_COLUMNS = [(89, "archmage"), (359, "bishop"), (629, "spellblade"), (899, "knight")]
LIST_ROWS = [230 + 94 * i for i in range(6)]


def party():
    """The stage's cards get a 2×3 stat block between the job and [앞으로][뒤로] (the buttons move down). The list rows
    stay as they are (a short line of six does not fit beside "N열" and reads badly on the gold rows); a hover on a row
    shows that mercenary's six in a tooltip at the row's left, like the item cards."""
    base = load("base-party.png")
    img = base.copy()
    d = ImageDraw.Draw(img)
    d.rectangle((84, 646, 1124, 702), fill=STAGE)
    img.paste(base.crop((84, 646, 1124, 702)), (84, 742))
    for x0, job in STAGE_COLUMNS:
        values = JOB[job]
        for i in range(6):
            cx = x0 + (i % 2) * 112
            cy = 658 + (i // 2) * 28
            stat_pair(d, cx, cy, LABELS[i], values[i], 18, 6)
    # Hover on 세드릭's row (the second): the row lights a little and the tooltip opens to its left.
    y0 = LIST_ROWS[1]
    light = Image.new("RGBA", img.size, (0, 0, 0, 0))
    ImageDraw.Draw(light).rectangle((1168, y0, 1856, y0 + 86), outline=(0x6E, 0xAA, 0xFF, 255), width=2)
    img = Image.alpha_composite(img, light)
    d = ImageDraw.Draw(img)
    box = (846, y0 - 6, 1150, y0 + 104)
    d.rectangle(box, fill=TIP, outline=TIP_EDGE)
    d.polygon([(1150, y0 + 34), (1162, y0 + 43), (1150, y0 + 52)], fill=TIP)
    label(d, (998, y0 + 18), "세드릭 · 성기사 Lv 1", 20, TEXT, "mm")
    d.line((862, y0 + 36, 1134, y0 + 36), fill=TIP_LINE, width=1)
    values = JOB["paladin"]
    for i in range(6):
        stat_pair(d, 864 + (i % 3) * 94, y0 + 56 + (i // 3) * 30, LABELS[i], values[i], 18, 5)
    return img


# ---------------------------------------------------------------- item card
def card():
    """The card counted with its owner (로언, 기사: 힘 12, 민첩 9): damage ×1.2 (2-4 → 3-4 with the whetstone),
    치명타 5 − 0.5 = 4.5%, 명중 80 + 2 = 82% against a target at the baseline. The box grows by a line upward."""
    img = load("base-card.png")
    base = load("base-card.png")
    d = ImageDraw.Draw(img)
    # Put back what the old box covered above it, then the taller box.
    left, right, bottom = 582, 978, 610
    top = 361 - 24
    img.paste(Image.open(ROOT / "ArtPipeline/Archive/81-stat-display/base-card.png").convert("RGBA").crop((left, top, right, 361)), (left, top))
    d.rectangle((left - 1, top - 1, right, bottom), fill=TIP, outline=TIP_EDGE)
    cx = 780
    label(d, (cx, top + 28), "롱소드", 24, TEXT, "mm")
    d.line((604, top + 49, 958, top + 49), fill=TIP_LINE, width=1)
    label(d, (cx, top + 67), "무기 장비 / 크기 3×1 / 근접 / 쿨다운 3.0초 /", 18, TEXT, "mm")
    # 치명타 and 명중 with the owner's stats: the changed numbers in green/red against the data.
    line = [("치명타 ", TEXT), ("4.5%", LOW), (" / 명중 ", TEXT), ("82%", GOOD), (" /", TEXT)]
    total = sum(width(s, 18) for s, _ in line)
    x = cx - total / 2
    for s, color in line:
        label(d, (x, top + 90), s, 18, color, "lm")
        x += width(s, 18)
    label(d, (cx, top + 113), "앞에서 2번째 자리까지만 발동", 18, TEXT, "mm")
    label(d, (cx, top + 143), "맨 앞 적에게 피해 3-4", 22, EFFECT, "mm")
    parts = [("(★ 숫돌 +1)", STAR), ("  ", TEXT), ("(힘 12 ×1.2)", GOOD)]
    total = sum(width(s, 20) for s, _ in parts)
    x = cx - total / 2
    for s, color in parts:
        label(d, (x, top + 167), s, 20, color, "lm")
        x += width(s, 20)
    label(d, (cx, top + 196), "기본 무기: 피로 없음", 18, DIM, "mm")
    recipe = [("담금질(야영지): 롱소드 ", TEXT), ("3성", TIER3), (" + 숫돌 ", TEXT), ("3성", TIER3), ("(★ 켜", TEXT)]
    total = sum(width(s, 18) for s, _ in recipe)
    x = cx - total / 2
    for s, color in recipe:
        label(d, (x, top + 225), s, 18, color, "lm")
        x += width(s, 18)
    label(d, (cx, top + 248), "짐) → 글라디우스", 18, TEXT, "mm")
    return img


# ---------------------------------------------------------------- battle
def battle():
    """미라's 화염 지팡이 (지식 15) hits the three — each target rolls its own 85%: two hits, one miss. The miss is a gray
    '빗나감' where the damage would stand (the 치명타 word's place), and a line in the ticker with the chance and the roll."""
    img = load("base-battle.png")
    clean = Image.open(HERE / "base-battle-clean.png").convert("RGBA")
    d = ImageDraw.Draw(img)
    # The middle goblin's "-2" goes; the clean background (ko_51, same floor) comes back under it.
    img.paste(clean.crop((1290, 215, 1390, 290)), (1290, 215))
    label(d, (1330, 250), "빗나감", 40, DIM, "mm")
    # The ticker: the newest line brighter (as it is).
    img.paste(clean.crop((360, 8, 790, 74)), (360, 8))
    lines = ["미라의 화염 지팡이 → 고블린 약탈자: 피해 2",
             "미라의 화염 지팡이 → 고블린 약탈자: 빗나감 (명중 85% · 굴림 91)",
             "미라의 화염 지팡이 → 고블린 약탈자: 피해 2"]
    for i, s in enumerate(lines):
        label(d, (372, 21 + 20 * i), s, 15, DIM if i < 2 else TEXT, "lm")
    return img


# ---------------------------------------------------------------- settlement
def settlement_frame(grow=0):
    """The settlement as it is when the party comes back (the shipped base is a wipe): 던전 클리어 and the lines from
    UiPrefabSetup.Settlement (panel 410,160 1100×760). grow = how much taller the panel stands (half up, half down)."""
    img = load("base-settlement.png")
    d = ImageDraw.Draw(img)
    d.rectangle((410, 120, 1510, 960), fill=(0x16, 0x16, 0x1C))
    top, bottom = 160 - grow // 2, 920 + grow // 2
    d.rectangle((410, top, 1510, bottom), fill=(0x1C, 0x22, 0x2A))
    up = -(grow // 2)
    label(d, (960, 245 + up), "던전 클리어", 64, GOOD, "mm")
    label(d, (960, 316 + up), "버려진 광산", 32, DIM, "mm")
    label(d, (490, 402 + up), "생환: 로언, 카이, 엘라, 미라", 30, TEXT, "lm")
    label(d, (490, 458 + up), "전사: 없음", 30, TEXT, "lm")
    label(d, (490, 530 + up), "생환자의 피로도: 로언 64, 카이 30, 엘라 102 (공포), 미라 41", 28, TEXT, "lm")
    label(d, (490, 578 + up), "5일이 지나 6일차가 되었습니다", 28, TEXT, "lm")
    label(d, (490, 626 + up), "얻은 금 +80 (30층 도달) · 보유 금 80", 28, GOLD, "lm")
    label(d, (490, 674 + up), "경험치 +20 · 레벨 업: 로언 Lv 2, 엘라 Lv 2", 28, BONE, "lm")
    # The confirm button at the panel's foot (400×90, 20 from the bottom as it is).
    d.rectangle((760, bottom - 110, 1160, bottom - 20), fill=(0x3B, 0x6F, 0xB5))
    label(d, (960, bottom - 65), "확인", 36, TEXT, "mm")
    return img, up


def settlement_now():
    img, up = settlement_frame()
    label(ImageDraw.Draw(img), (490, 720 + up), "원정에서 얻은 아이템과 포션은 모두 사라졌습니다", 24, DIM, "lm")
    return img


def settlement_new():
    """A line for each mercenary who levelled: what the job's roll gave (Design/05 §2). The panel grows by the lines
    (32 each: here two, so 80 with the gap) and the 'items lost' line moves under them."""
    img, up = settlement_frame(80)
    d = ImageDraw.Draw(img)
    growth = [("로언 Lv 2", ["힘 +1", "체력 +1", "인내 +1"]), ("엘라 Lv 2", ["정신력 +2", "지식 +1"])]
    for i, (who, gains) in enumerate(growth):
        y = 718 + up + 34 * i
        label(d, (520, y), who, 24, BONE, "lm")
        x = 520 + width(who, 24) + 16
        for j, g in enumerate(gains):
            if j:
                label(d, (x, y), "·", 24, DIM, "lm"); x += width("·", 24) + 8
            label(d, (x, y), g, 24, GOOD, "lm"); x += width(g, 24) + 8
    label(d, (490, 800 + up), "원정에서 얻은 아이템과 포션은 모두 사라졌습니다", 24, DIM, "lm")
    return img


# ---------------------------------------------------------------- sheets
def sheet(pairs, name, scale=0.5):
    """Side by side at half size with a header over each: 지금 | 안 ..."""
    w, h = int(1920 * scale), int(1080 * scale)
    head = 56
    out = Image.new("RGB", (w * len(pairs) + 16 * (len(pairs) - 1), h + head), (0x0C, 0x0D, 0x11))
    d = ImageDraw.Draw(out)
    for i, (title, img) in enumerate(pairs):
        x = i * (w + 16)
        out.paste(img.convert("RGB").resize((w, h), Image.LANCZOS), (x, head))
        label(d, (x + 12, head / 2), title, 28, GOOD if "권장" in title else TEXT, "lm")
    out.save(HERE / name)


def main():
    clean_from = Image.open(Path.home() / "Library/Caches/F1/screenshots/20261018-084651/ko_51_battle_critical.png")
    if not (HERE / "base-battle-clean.png").exists():
        clean_from.save(HERE / "base-battle-clean.png")
    shots = {
        "mock-barracks-A.png": caption(barracks_a(), "안 A — 열 버튼 유지, 레벨 칸 옆에 스탯 3×2"),
        "mock-barracks-B.png": caption(barracks_b(), "안 B (권장) — 막사의 열 버튼을 빼고 그 자리에 스탯 여섯 (파티는 원정 팝업에서)"),
        "mock-coach.png": caption(coach(), "역마차 — 후보마다 스탯 줄: 값은 기준 10 대비 색, ▲▼는 직업 기본값 대비 편차"),
        "mock-party.png": caption(party(), "파티 팝업 — 무대 카드에 스탯 2×3, 목록 줄은 그대로 두고 마우스를 올리면 툴팁"),
        "mock-card.png": caption(card(), "아이템 카드 — 주인(로언, 힘 12·민첩 9)의 스탯으로 셈한 피해·치명타·명중"),
        "mock-battle-miss.png": caption(battle(), "전투 — 화염 지팡이(지식 15, 명중 85%)가 셋 중 하나를 빗나감: 회색 글과 자막 줄"),
        "mock-settlement.png": caption(settlement_new(), "정산 — 레벨 업한 용병마다 굴림으로 자란 스탯 한 줄 (줄 수만큼 패널이 커짐)"),
    }
    for name, img in shots.items():
        img.convert("RGB").save(HERE / name)
    now = lambda f, t: caption(load(f), t)
    sheet([("지금", now("base-barracks.png", "지금")), ("안 A", shots["mock-barracks-A.png"]), ("안 B (권장)", shots["mock-barracks-B.png"])], "compare-1-barracks.png")
    sheet([("지금", now("base-coach.png", "지금")), ("안 (권장)", shots["mock-coach.png"])], "compare-2-coach.png")
    sheet([("지금", now("base-party.png", "지금")), ("안 (권장)", shots["mock-party.png"])], "compare-3-party.png")
    sheet([("지금", now("base-card.png", "지금")), ("안 (권장)", shots["mock-card.png"])], "compare-4-card.png")
    sheet([("지금", now("base-battle.png", "지금")), ("안 (권장)", shots["mock-battle-miss.png"])], "compare-5-battle.png")
    sheet([("지금 (생환한 정산)", caption(settlement_now(), "지금 (생환한 정산을 같은 줄로 다시 그림)")), ("안 (권장)", shots["mock-settlement.png"])], "compare-6-settlement.png")


if __name__ == "__main__":
    main()
