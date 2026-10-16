"""Round 71 (Slice C, before stage 25/26): the lobby with its quarters and the expedition screen, as mockups in the game's own
palette and font (no API call). The user decided (stage 24): one lobby screen (quarters: barracks, stagecoach, rest) and one
expedition screen that picks the dungeon, borrowed from Darkest Dungeon's embark screen. What the mockups decide: how the
quarters are laid out, and where the party and its rows are formed (lobby or expedition screen).
  .venv/bin/python ArtPipeline/Archive/71-lobby-expedition/mock_lobby.py [lobby-screenshot.png]
Outputs (here): mock-now.png (the game's lobby), mock-lobby-A/B/C.png, mock-lobby-B-coach.png, mock-lobby-B-rest.png,
mock-expedition-E1/E2.png, mock-compare-lobby.png, mock-compare-expedition.png.
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[3]; HERE = Path(__file__).resolve().parent
FONT = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"
SHOT = Path(sys.argv[1]) if len(sys.argv) > 1 and not sys.argv[1].startswith("--") else Path.home() / "Library/Caches/F1/screenshots/20261016-151905/ko_03_lobby_party.png"
W, H = 1920, 1080

# UiPalette
BG = (0x12, 0x14, 0x1A); PANEL = (0x1E, 0x22, 0x2B); PANEL_LIGHT = (0x2A, 0x30, 0x3C); SLOT = (0x15, 0x18, 0x1F)
TEXT = (0xEB, 0xEB, 0xE6); DIM = (0x9A, 0xA0, 0xAC); BUTTON = (0x3B, 0x6F, 0xB5); QUIET = (0x3A, 0x41, 0x50); SELECTED = (0x80, 0x66, 0x14)
GOOD = (0x58, 0xB3, 0x68); DANGER = (0xC0, 0x39, 0x2B); FATIGUE = (0xCF, 0xBA, 0xF7); FBAR = (0x9E, 0x84, 0xDA); FDANGER = (0xC8, 0x4B, 0x6E)
BRASS = (0xB8, 0x94, 0x4E); BONE = (0xCD, 0xC0, 0xA0); GOLD = (0xC7, 0xB3, 0x77); LINE = (0x55, 0x5C, 0x6B); VIRTUE = (0xF0, 0xC8, 0x5A)

# Example state: day 12 after one expedition (5 days) and a day of rest; a mercenary came home afflicted.
DAY, GOLD_HELD, DEATHS, LIMIT, CAP = 12, 120, 0, 15, 8
ROSTER = [  # name, job, level, exp, exp to next, fatigue, affliction, passive, party row (0 = none)
    ("아스트리드", "발키리", 2, 4, 20, 0, None, "빈사 상태: 무기 피해 +100%", 0),
    ("세드릭", "성기사", 1, 12, 15, 12, None, "맨 앞: 전투 시작 때 아군 전체에 보호막 2", 0),
    ("엘라", "주교", 2, 9, 20, 54, None, "회복시킨 대상에게 보호막 3", 3),
    ("카이", "마검사", 2, 9, 20, 86, None, "무기 적중 시 화상 3", 2),
    ("미라", "대마법사", 1, 12, 15, 61, None, "뒤에서 2번째 자리까지: 무기 피해 +25%", 4),
    ("로언", "기사", 3, 2, 30, 128, "공포", "맨 앞: 전투 시작 때 자신에게 보호막 5", 1),
]
RECRUITS = [("브란", "기사", "맨 앞: 전투 시작 때 자신에게 보호막 5", 30), ("요한나", "주교", "회복시킨 대상에게 보호막 3", 30), ("테오", "마검사", "무기 적중 시 화상 3", 30)]
HIRE, CURE_GOLD, CURE_RELIEF, REST_RELIEF = 30, 20, 40, 10
PARTY = {r[8]: r[0] for r in ROSTER if r[8]}


def font(px):
    return ImageFont.truetype(str(FONT), int(round(px)))


def text(d, xy, s, px, color=TEXT, anchor="la"):
    d.text(xy, s, font=font(px), fill=color, anchor=anchor)


def textw(s, px):
    l, t, r, b = font(px).getbbox(s); return r - l


def panel(d, box, fill=PANEL, outline=None, width=2):
    d.rectangle(box, fill=fill, outline=outline, width=width if outline else 0)


def button(d, box, label, fill=QUIET, px=26, color=TEXT):
    panel(d, box, fill)
    text(d, ((box[0] + box[2]) / 2, (box[1] + box[3]) / 2), label, px, color, "mm")


def pips(d, x, y, w, value, maximum=200, h=14, gap=4):
    n = 10; pw = (w - gap * (n - 1)) / n
    for i in range(n):
        px0 = x + round(i * (pw + gap)); px1 = x + round(i * (pw + gap) + pw)
        d.rectangle([px0, y, px1 - 1, y + h - 1], fill=SLOT)
        full = min(1.0, max(0.0, (value - i * (maximum / n)) / (maximum / n)))
        if full > 0:
            d.rectangle([px0, y, px0 + round((px1 - px0) * full) - 1, y + h - 1], fill=FBAR if i < n // 2 else FDANGER)


def exp_bar(d, x, y, w, exp, need, h=8):
    d.rectangle([x, y, x + w - 1, y + h - 1], fill=SLOT)
    d.rectangle([x, y, x + round(w * exp / need) - 1, y + h - 1], fill=BONE)


def header(img, title, gold=True, right="타이틀로"):
    d = ImageDraw.Draw(img)
    panel(d, (0, 0, W, 100), BG)
    text(d, (40, 50), title, 44, TEXT, "lm")
    text(d, (W / 2, 50), f"{DAY}일차 / 100일", 44, TEXT, "mm")
    if gold:
        text(d, (1400, 50), f"금 {GOLD_HELD}", 32, GOLD, "lm")
    button(d, (1640, 20, 1880, 80), right, QUIET, 26)


def roster_line(d, x, y, m, w=1112, rows=True, level=True, dismiss=False):
    name, job, lv, exp, need, fat, aff, passive, row = m
    panel(d, (x, y, x + w, y + 116), PANEL_LIGHT)
    text(d, (x + 20, y + 28), name, 30, TEXT, "lm")
    if level:
        text(d, (x + 20 + textw(name, 30) + 14, y + 30), f"Lv {lv}", 22, BONE, "lm")
    text(d, (x + 250, y + 30), job, 22, DIM, "lm")
    words = f"피로도 {fat}/200" + (f" · {aff}" if aff else "")
    text(d, (x + 610, y + 30), words, 22, FATIGUE if fat < 100 else FDANGER, "rm")
    pips(d, x + 20, y + 54, 590, fat)
    text(d, (x + 20, y + 92), passive, 20, DIM, "lm")
    if aff:
        text(d, (x + 610, y + 92), "아이템이 25% 느리게 돈다", 18, FDANGER, "rm")
    if rows:
        for r in range(1, 5):
            bx = x + 636 + (r - 1) * 88
            button(d, (bx, y + 28, bx + 80, y + 88), f"{r}열", SELECTED if row == r else QUIET, 24)
        if row:
            button(d, (x + 988, y + 28, x + 1100, y + 88), "제외", QUIET, 24)
    elif dismiss:
        button(d, (x + 980, y + 36, x + 1092, y + 80), "해고", QUIET, 22, DIM)


def party_panel(d, box, title="파티", go="원정 화면으로"):
    x0, y0, x1, y1 = box
    panel(d, box, PANEL)
    text(d, (x0 + 26, y0 + 40), title, 30, DIM, "lm")
    for r in range(1, 5):
        yy = y0 + 100 + (r - 1) * 50
        text(d, (x0 + 26, yy), f"{r}열", 26, DIM, "lm")
        text(d, (x0 + 140, yy), PARTY.get(r, "-"), 26, TEXT, "lm")
    text(d, (x0 + 26, y1 - 170), "출발할 수 있습니다", 24, GOOD, "lm")
    button(d, (x0 + 26, y1 - 130, x1 - 26, y1 - 30), go, BUTTON, 34)


def guild_panel(d, box):
    x0, y0, x1, y1 = box
    panel(d, box, PANEL)
    text(d, (x0 + 26, y0 + 40), "용병단", 30, DIM, "lm")
    lines = [("금", f"{GOLD_HELD}", GOLD), ("로스터", f"{len(ROSTER)} / {CAP}", TEXT), ("사망", f"{DEATHS} / {LIMIT}", TEXT),
             ("버려진 광산", "클리어 1회", TEXT), ("심연", "잠김 · 광산 2회", DIM)]
    for i, (k, v, c) in enumerate(lines):
        yy = y0 + 100 + i * 50
        text(d, (x0 + 26, yy), k, 24, DIM, "lm"); text(d, (x1 - 26, yy), v, 24, c, "rm")


# ----------------------------------------------------------------- lobby A: quarters as a column of tabs on the left
def lobby_A():
    img = Image.new("RGB", (W, H), BG); header(img, "용병단 본부"); d = ImageDraw.Draw(img)
    tabs = [("막사", True), ("역마차", False), ("쉼터", False)]
    for i, (name, on) in enumerate(tabs):
        button(d, (40, 120 + i * 110, 230, 210 + i * 110), name, SELECTED if on else QUIET, 30)
    button(d, (40, 940, 230, 1040), "원정", BUTTON, 30)
    panel(d, (250, 120, 1420, 1040), PANEL)
    text(d, (276, 160), "막사", 30, DIM, "lm")
    for i, m in enumerate(ROSTER):
        roster_line(d, 276, 192 + i * 126, m, w=1120, rows=True)
    party_panel(d, (1440, 120, 1880, 1040))
    return img


# ----------------------------------------------------------------- lobby B: quarters as a strip of tabs under the header
def tab_strip(d, on):
    names = ["막사", "역마차", "쉼터"]
    for i, name in enumerate(names):
        button(d, (40 + i * 240, 112, 260 + i * 240, 172), name, SELECTED if name == on else QUIET, 28)
    button(d, (1640, 112, 1880, 172), "원정 화면으로", BUTTON, 28)


def lobby_B():
    img = Image.new("RGB", (W, H), BG); header(img, "용병단 본부"); d = ImageDraw.Draw(img)
    tab_strip(d, "막사")
    panel(d, (40, 190, 1200, 1040), PANEL)
    text(d, (64, 230), "막사 — 로스터", 30, DIM, "lm")
    for i, m in enumerate(ROSTER):
        roster_line(d, 64, 262 + i * 126, m, rows=False, level=False)
        # the level and its exp, large, where the row buttons were
        x, y = 64 + 700, 262 + i * 126
        text(d, (x, y + 40), f"Lv {m[2]}", 30, BONE, "lm")
        text(d, (x + 110, y + 42), f"경험치 {m[3]}/{m[4]}", 20, DIM, "lm")
        exp_bar(d, x, y + 66, 300, m[3], m[4])
    guild_panel(d, (1218, 190, 1880, 560))
    panel(d, (1218, 580, 1880, 1040), PANEL)
    text(d, (1244, 620), "다음", 30, DIM, "lm")
    text(d, (1244, 680), "파티는 원정 화면에서 꾸린다.", 24, TEXT, "lm")
    text(d, (1244, 720), "지금 파티: 로언 · 카이 · 엘라 · 미라", 22, DIM, "lm")
    text(d, (1244, 780), "최종 결전까지 88일", 24, DIM, "lm")
    button(d, (1244, 900, 1854, 1000), "원정 화면으로", BUTTON, 34)
    return img


def lobby_B_coach():
    img = Image.new("RGB", (W, H), BG); header(img, "용병단 본부"); d = ImageDraw.Draw(img)
    tab_strip(d, "역마차")
    panel(d, (40, 190, 1880, 640), PANEL)
    text(d, (64, 230), "역마차 — 이번 주의 후보 (다음 귀환에 새로 온다)", 30, DIM, "lm")
    for i, (name, job, passive, price) in enumerate(RECRUITS):
        x = 64 + i * 600
        panel(d, (x, 270, x + 560, 600), PANEL_LIGHT)
        text(d, (x + 24, 310), name, 34, TEXT, "lm"); text(d, (x + 24, 356), job, 24, DIM, "lm")
        text(d, (x + 24, 400), "Lv 1 · 피로도 0/200", 22, FATIGUE, "lm")
        text(d, (x + 24, 440), passive, 20, DIM, "lm")
        button(d, (x + 24, 500, x + 536, 570), f"고용 — 금 {price}", BUTTON if GOLD_HELD >= price else QUIET, 28)
    panel(d, (40, 660, 1880, 1040), PANEL)
    text(d, (64, 700), f"로스터 {len(ROSTER)} / {CAP} — 내보내기(비용 없음, 돌아오지 않는다)", 30, DIM, "lm")
    for i, m in enumerate(ROSTER):
        x = 64 + (i % 3) * 600; y = 740 + (i // 3) * 140
        panel(d, (x, y, x + 560, y + 120), PANEL_LIGHT)
        text(d, (x + 24, y + 34), m[0], 28, TEXT, "lm"); text(d, (x + 200, y + 36), f"{m[1]} · Lv {m[2]}", 22, DIM, "lm")
        pips(d, x + 24, y + 70, 320, m[5])
        last = i == 5 and False
        button(d, (x + 400, y + 30, x + 536, y + 90), "해고", QUIET, 24, DIM)
    return img


def lobby_B_rest():
    img = Image.new("RGB", (W, H), BG); header(img, "용병단 본부"); d = ImageDraw.Draw(img)
    tab_strip(d, "쉼터")
    panel(d, (40, 190, 640, 1040), PANEL)
    text(d, (64, 230), "쉬기", 30, DIM, "lm")
    text(d, (64, 290), "하루를 보낸다. 모두의 피로도가", 24, TEXT, "lm")
    text(d, (64, 326), f"{REST_RELIEF} 내려간다.", 24, TEXT, "lm")
    text(d, (64, 380), f"{DAY}일차 → {DAY + 1}일차", 24, DIM, "lm")
    button(d, (64, 440, 616, 540), f"쉬기 (피로도 -{REST_RELIEF} · 1일)", BUTTON, 30)
    panel(d, (660, 190, 1880, 1040), PANEL)
    text(d, (684, 230), f"치료 — 금 {CURE_GOLD}으로 한 사람의 피로도를 {CURE_RELIEF} 푼다 (날은 가지 않는다)", 30, DIM, "lm")
    for i, m in enumerate(ROSTER):
        y = 270 + i * 126
        panel(d, (684, y, 1856, y + 116), PANEL_LIGHT)
        text(d, (708, y + 32), m[0], 28, TEXT, "lm"); text(d, (900, y + 34), f"{m[1]} · Lv {m[2]}", 22, DIM, "lm")
        words = f"피로도 {m[5]}/200" + (f" · {m[6]}" if m[6] else "")
        text(d, (1480, y + 34), words, 22, FATIGUE if m[5] < 100 else FDANGER, "rm")
        pips(d, 708, y + 64, 772, m[5])
        can = m[5] > 0 and GOLD_HELD >= CURE_GOLD
        button(d, (1520, y + 28, 1832, y + 88), f"치료 — 금 {CURE_GOLD}", BUTTON if can else QUIET, 24, TEXT if can else DIM)
    return img


# ----------------------------------------------------------------- lobby C: every quarter on one screen, no tabs
def lobby_C():
    img = Image.new("RGB", (W, H), BG); header(img, "용병단 본부"); d = ImageDraw.Draw(img)
    panel(d, (40, 120, 1200, 1040), PANEL)
    text(d, (64, 160), "막사", 30, DIM, "lm")
    for i, m in enumerate(ROSTER):
        roster_line(d, 64, 192 + i * 126, m, rows=True)
    # stagecoach
    panel(d, (1218, 120, 1880, 470), PANEL)
    text(d, (1244, 160), f"역마차 — 후보 셋 · 로스터 {len(ROSTER)}/{CAP}", 28, DIM, "lm")
    for i, (name, job, passive, price) in enumerate(RECRUITS):
        y = 196 + i * 86
        panel(d, (1244, y, 1854, y + 76), PANEL_LIGHT)
        text(d, (1264, y + 26), name, 26, TEXT, "lm"); text(d, (1400, y + 28), f"{job} · Lv 1", 20, DIM, "lm")
        button(d, (1640, y + 12, 1840, y + 64), f"고용 금 {price}", BUTTON, 22)
    text(d, (1244, 452), "내보내기는 로스터 줄의 이름을 눌러서", 18, DIM, "lm")
    # rest
    panel(d, (1218, 490, 1880, 700), PANEL)
    text(d, (1244, 530), "쉼터", 28, DIM, "lm")
    button(d, (1244, 566, 1540, 626), f"쉬기 (-{REST_RELIEF} · 1일)", QUIET, 22)
    button(d, (1560, 566, 1854, 626), f"치료 금 {CURE_GOLD} (-{CURE_RELIEF})", QUIET, 22)
    text(d, (1244, 660), "치료는 로스터에서 고른 용병에게", 18, DIM, "lm")
    # expedition
    panel(d, (1218, 720, 1880, 1040), PANEL)
    text(d, (1244, 760), "원정 — 파티: 로언 · 카이 · 엘라 · 미라", 24, DIM, "lm")
    text(d, (1244, 800), f"금 {GOLD_HELD} · 사망 {DEATHS}/{LIMIT} · 최종 결전까지 88일", 20, DIM, "lm")
    text(d, (1244, 860), "출발할 수 있습니다", 24, GOOD, "lm")
    button(d, (1244, 900, 1854, 1000), "원정 화면으로", BUTTON, 34)
    return img


# ----------------------------------------------------------------- expedition screen (Darkest Dungeon's embark, borrowed)
def pick(d, x, y, c):
    d.line([(x - 14, y + 14), (x + 10, y - 10)], fill=c, width=5)
    d.arc([x - 2, y - 24, x + 26, y + 4], 150, 300, fill=c, width=5)


def lock(d, x, y, c):
    d.rectangle([x - 13, y - 2, x + 13, y + 15], fill=c)
    d.arc([x - 9, y - 19, x + 9, y + 5], 180, 360, fill=c, width=4)


def map_and_details(d, selected="mine"):
    panel(d, (40, 120, 1300, 760), PANEL)
    text(d, (64, 160), "지도", 30, DIM, "lm")
    # a path from the camp to the mine and on to the abyss
    pts = [(200, 620), (520, 470), (900, 330)]
    for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
        n = 14
        for k in range(n):
            t0, t1 = k / n, (k + 0.5) / n
            d.line([(x0 + (x1 - x0) * t0, y0 + (y1 - y0) * t0), (x0 + (x1 - x0) * t1, y0 + (y1 - y0) * t1)], fill=LINE, width=4)
    # the camp
    d.ellipse([pts[0][0] - 18, pts[0][1] - 18, pts[0][0] + 18, pts[0][1] + 18], fill=QUIET, outline=BONE, width=3)
    text(d, (pts[0][0], pts[0][1] + 40), "본부", 22, DIM, "mm")
    # the mine, chosen
    x, y = pts[1]
    if selected == "mine":
        d.ellipse([x - 48, y - 48, x + 48, y + 48], outline=SELECTED, width=4)
    d.ellipse([x - 34, y - 34, x + 34, y + 34], fill=PANEL_LIGHT, outline=BRASS, width=4)
    pick(d, x, y, BONE)
    text(d, (x, y + 70), "버려진 광산", 26, TEXT, "mm"); text(d, (x, y + 100), "클리어 1회 · 5일", 20, DIM, "mm")
    # the abyss, locked
    x, y = pts[2]
    d.ellipse([x - 34, y - 34, x + 34, y + 34], fill=SLOT, outline=LINE, width=4)
    lock(d, x, y - 2, DIM)
    text(d, (x, y + 70), "심연 (최종 던전)", 26, DIM, "mm"); text(d, (x, y + 100), "잠김 · 광산 2회 클리어", 20, DIM, "mm")
    text(d, (64, 720), "다른 지역은 Slice D에서 더한다. 100일을 넘기면 심연의 보스와 강제로 싸운다.", 18, DIM, "lm")
    # details
    panel(d, (1320, 120, 1880, 760), PANEL)
    text(d, (1346, 160), "원정", 30, DIM, "lm")
    text(d, (1346, 226), "버려진 광산", 40, TEXT, "lm")
    rows = [("지역 속성", "신속"), ("층", "15층 + 보스"), ("소요", "5일 (→ 17일차)"), ("시작 포션", "치유 2"), ("클리어", "1회"), ("보상", "금 (층마다) · 경험치")]
    for i, (k, v) in enumerate(rows):
        yy = 300 + i * 52
        text(d, (1346, yy), k, 24, DIM, "lm"); text(d, (1854, yy), v, 24, TEXT, "rm")
    text(d, (1346, 660), "심연을 열려면 광산을 2회 클리어", 20, DIM, "lm")


def expedition_E1():
    img = Image.new("RGB", (W, H), BG); header(img, "원정", right="로비로"); d = ImageDraw.Draw(img)
    map_and_details(d)
    # the party strip, Darkest Dungeon's: four slots and the roster to put into them
    panel(d, (40, 780, 1880, 1040), PANEL)
    text(d, (64, 812), "파티 — 로스터에서 눌러 넣고, 열을 눌러 자리를 바꾼다", 24, DIM, "lm")
    for r in range(1, 5):
        x = 64 + (r - 1) * 200
        who = PARTY.get(r)
        panel(d, (x, 840, x + 184, 1020), SELECTED if who else SLOT, outline=BRASS if who else LINE, width=2)
        text(d, (x + 92, 862), f"{r}열", 20, BONE if who else DIM, "mm")
        text(d, (x + 92, 920), who or "비었음", 26, TEXT if who else DIM, "mm")
        if who:
            m = next(mm for mm in ROSTER if mm[0] == who)
            text(d, (x + 92, 956), f"{m[1]} · Lv {m[2]}", 18, DIM, "mm")
            pips(d, x + 22, 984, 140, m[5], h=10, gap=3)
    for i, m in enumerate(ROSTER):
        x = 900 + (i % 3) * 230; y = 840 + (i // 3) * 92
        on = m[8] > 0
        panel(d, (x, y, x + 214, y + 80), PANEL_LIGHT if not on else PANEL, outline=BRASS if on else None, width=2)
        text(d, (x + 14, y + 24), m[0], 22, TEXT if not on else DIM, "lm"); text(d, (x + 200, y + 24), f"Lv {m[2]}", 18, BONE, "rm")
        pips(d, x + 14, y + 50, 186, m[5], h=10, gap=3)
    text(d, (1600, 840), "출발할 수 있습니다", 22, GOOD, "lm")
    button(d, (1600, 880, 1854, 1000), "출발", BUTTON, 34)
    return img


def expedition_E2():
    img = Image.new("RGB", (W, H), BG); header(img, "원정", right="로비로"); d = ImageDraw.Draw(img)
    map_and_details(d)
    panel(d, (40, 780, 1880, 1040), PANEL)
    text(d, (64, 812), "파티 (막사에서 꾸린다)", 24, DIM, "lm")
    for r in range(1, 5):
        x = 64 + (r - 1) * 300
        who = PARTY.get(r); m = next(mm for mm in ROSTER if mm[0] == who)
        panel(d, (x, 850, x + 280, 1010), PANEL_LIGHT)
        text(d, (x + 20, 880), f"{r}열", 20, DIM, "lm"); text(d, (x + 80, 880), who, 26, TEXT, "lm")
        text(d, (x + 20, 920), f"{m[1]} · Lv {m[2]}", 20, DIM, "lm")
        pips(d, x + 20, 960, 240, m[5], h=12, gap=3)
    text(d, (1600, 840), "출발할 수 있습니다", 22, GOOD, "lm")
    button(d, (1600, 880, 1854, 1000), "출발", BUTTON, 34)
    return img


def sheet(entries, cols, path, scale=0.5, lab=44, gap=24, px=26, footer=None):
    ims = [(n, im.resize((round(im.width * scale), round(im.height * scale)), Image.LANCZOS)) for n, im in entries]
    w = max(im.width for _, im in ims); h = max(im.height for _, im in ims); rows = (len(ims) + cols - 1) // cols
    fh = 50 if footer else 0
    out = Image.new("RGB", (cols * (w + gap) + gap, rows * (h + lab + gap) + gap + fh), (18, 18, 18)); d = ImageDraw.Draw(out)
    for i, (name, im) in enumerate(ims):
        x = gap + (i % cols) * (w + gap); y = gap + (i // cols) * (h + lab + gap)
        text(d, (x, y + 4), name, px, (235, 235, 235)); out.paste(im, (x, y + lab))
    if footer:
        text(d, (gap, out.height - fh + 10), footer, 22, (190, 190, 190))
    out.save(path); print(path.name, out.size)


def main():
    now = Image.open(SHOT).convert("RGB"); now.save(HERE / "mock-now.png")
    outs = {"lobby-A": lobby_A(), "lobby-B": lobby_B(), "lobby-B-coach": lobby_B_coach(), "lobby-B-rest": lobby_B_rest(),
            "lobby-C": lobby_C(), "expedition-E1": expedition_E1(), "expedition-E2": expedition_E2()}
    for k, im in outs.items():
        im.save(HERE / f"mock-{k}.png"); print(f"mock-{k}.png")
    sheet([("지금: 로비 하나에 로스터·열·던전·쉬기·출발", now),
           ("A: 구역 탭을 왼쪽 세로로, 파티는 막사의 열 버튼", outs["lobby-A"]),
           ("B (권장): 구역 탭을 위 가로로, 막사는 레벨·경험치, 파티는 원정 화면에서", outs["lobby-B"]),
           ("C: 탭 없이 한 화면에 역마차·쉼터·원정", outs["lobby-C"])], 2, HERE / "mock-compare-lobby.png",
          footer="예: 12일차, 금 120, 로스터 6/8, 원정 하나 뒤(로언 128 공포). 안 B의 역마차·쉼터 구역은 mock-lobby-B-coach.png, mock-lobby-B-rest.png.")
    sheet([("B의 역마차: 후보 셋(고용 금 30)과 내보내기", outs["lobby-B-coach"]),
           ("B의 쉼터: 쉬기(모두 -10, 1일)와 치료(금 20, 한 사람 -40)", outs["lobby-B-rest"])], 2, HERE / "mock-compare-quarters.png")
    sheet([("E1 (권장): 지도·던전 정보 + 아래 띠에서 파티를 꾸림 (다키스트 던전의 출정)", outs["expedition-E1"]),
           ("E2: 지도·던전 정보, 파티는 막사에서 꾸리고 여기서는 보기만", outs["expedition-E2"])], 2, HERE / "mock-compare-expedition.png",
          footer="지도의 표식 둘: 버려진 광산(열림)과 심연(최종 던전, 광산 2회 클리어로 열림). 다른 지역은 Slice D.")


if __name__ == "__main__":
    main()
