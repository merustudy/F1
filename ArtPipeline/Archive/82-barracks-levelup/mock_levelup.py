# -*- coding: utf-8 -*-
"""Round 82: the level-up moves from the settlement to the barracks (2026-10-18 사용자 "레벨업은 원정 이후 막사에서 내가 용병을
클릭해 올리는 식") and the five jobs that replace the six (same instruction: 전사·바이킹·방랑자·마법사·사제). Drawn over the
2026-10-18 screenshots (base-*.png = ko_57, ko_72, ko_67 of 20261018-stats34d). Shapes and text only — no API call.
  .venv/bin/python ArtPipeline/Archive/82-barracks-levelup/mock_levelup.py
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
FONT = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"

# UiPalette
TEXT = (0xEB, 0xEB, 0xE6); DIM = (0x9A, 0xA0, 0xAC); GOOD = (0x58, 0xB3, 0x68); LOW = (0xD8, 0x6A, 0x5A)
BONE = (0xCD, 0xC0, 0xA0); GOLD = (0xC7, 0xB3, 0x77); STAR = (0xEC, 0xC4, 0x5C); FATIGUE = (0xCF, 0xBA, 0xF7)
LINE = (0x55, 0x5C, 0x6B); QUIET = (0x3A, 0x41, 0x50); ROW = (0x2A, 0x31, 0x3D); WINDOW = (0x1C, 0x22, 0x2A)
STAGE = (0x16, 0x16, 0x1C); BUTTON = (0x22, 0x29, 0x31); BLUE = (0x3B, 0x6F, 0xB5); EDGE = (0xB8, 0x9A, 0x55)
UP_FILL = (0xC9, 0x9A, 0x3A); UP_TEXT = (0x1C, 0x1A, 0x14)

BASELINE = 10
LABELS = ["힘", "민첩", "체력", "지식", "정신력", "인내"]
# The five jobs (재편안, Design/15 §4): name, base stats (str dex con int spi end), passive line, max HP.
JOBS = {
    "warrior": ("전사", [12, 9, 12, 6, 9, 11], "맨 앞: 전투 시작 때 자신에게 보호막 5", 38),
    "viking": ("바이킹", [13, 11, 10, 5, 8, 9], "빈사 상태: 무기 피해 +100%", 33),
    "ranger": ("방랑자", [8, 14, 9, 7, 9, 11], "무기의 치명타 확률 +10%", 27),
    "mage": ("마법사", [6, 9, 7, 15, 11, 8], "뒤에서 2번째 자리까지: 무기 피해 +25%", 22),
    "priest": ("사제", [7, 8, 9, 11, 14, 10], "회복시킨 대상에게 보호막 3", 25),
}
# The barracks' order as shipped, each on its new job (세드릭 성기사 → 전사, 카이 마검사 → 방랑자).
# (name, job, level, exp, exp to next): the four who went (엘라·카이·미라·로언) came back with 20 from a cleared mine.
ROSTER = [
    ("아스트리드", "viking", 2, 4, 25), ("세드릭", "warrior", 1, 0, 15), ("엘라", "priest", 1, 20, 15),
    ("카이", "ranger", 1, 20, 15), ("미라", "mage", 1, 20, 15), ("로언", "warrior", 1, 20, 15),
]
# 로언's roll for level 2 (warrior: 힘 S 110, 체력 A 90, 인내 C 50 came up).
ROWAN_GAIN = [1, 0, 1, 0, 0, 1]
ROWS = [266 + 100 * i for i in range(6)]
STAT_X = [742 + 78 * i for i in range(6)]


def font(px): return ImageFont.truetype(str(FONT), int(round(px)))


def label(d, xy, s, px, color=TEXT, anchor="la"):
    d.text(xy, s, font=font(px), fill=color, anchor=anchor)


def width(s, px): return font(px).getlength(s)


def tone(value):
    return GOOD if value > BASELINE else LOW if value < BASELINE else TEXT


def caption(img, text, top=False):
    """The caption bar at the foot (or the head, where the foot holds what the picture is about)."""
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    y = 0 if top else 1080 - 64
    d.rectangle((0, y, 1920, y + 64), fill=(0, 0, 0, 215))
    label(d, (24, y + 32), text, 30, TEXT, "lm")
    return Image.alpha_composite(img, layer)


def dim(img, alpha=170):
    layer = Image.new("RGBA", img.size, (0, 0, 0, alpha))
    return Image.alpha_composite(img, layer)


def load(name): return Image.open(HERE / name).convert("RGBA")


def button(d, box, text, fill, color, px=22):
    d.rounded_rectangle(box, radius=4, fill=fill)
    label(d, ((box[0] + box[2]) / 2, (box[1] + box[3]) / 2), text, px, color, "mm")


# ---------------------------------------------------------------- barracks
def ready(level, exp, to_next):
    return to_next > 0 and exp >= to_next


def row_frame(d, y0, name, job):
    """Job, passive and stats redrawn on the new jobs; the name, the fatigue and its pips stay as shot."""
    title, stats, passive, _ = JOBS[job]
    d.rectangle((300, y0 + 8, 540, y0 + 42), fill=ROW)
    label(d, (314, y0 + 25), title, 22, DIM, "lm")
    d.rectangle((80, y0 + 64, 690, y0 + 90), fill=ROW)
    label(d, (84, y0 + 75), passive, 18, DIM, "lm")
    return stats


def stats_cells(d, y0, values, gains=None):
    d.rectangle((700, y0 + 4, 1188, y0 + 88), fill=ROW)
    for i, x in enumerate(STAT_X):
        if i:
            d.line((x - 39, y0 + 16, x - 39, y0 + 76), fill=QUIET, width=1)
        label(d, (x, y0 + 25), LABELS[i], 18, DIM, "mm")
        label(d, (x, y0 + 60), str(values[i]), 30, tone(values[i]), "mm")
        if gains and gains[i]:
            label(d, (x + 20, y0 + 44), "+" + str(gains[i]), 16, GOOD, "lm")


def level_block(d, y0, level, exp, to_next, with_button=True):
    """The level block (1196..1676): Lv, experience and its bar; a ready mercenary's bar is full and gold, and the
    [레벨 업] button stands at the bar's end (안 A·C)."""
    d.rectangle((1196, y0 + 4, 1676, y0 + 88), fill=ROW)
    label(d, (1204, y0 + 29), f"Lv {level}", 26, BONE, "lm")
    is_ready = ready(level, exp, to_next)
    bar_end = 1396 if (is_ready and with_button) else 1544
    label(d, (1276 if (is_ready and with_button) else 1314, y0 + 31), f"경험치 {exp}/{to_next}", 18, STAR if is_ready else DIM, "lm")
    d.rectangle((1204, y0 + 56, bar_end, y0 + 64), fill=STAGE)
    fill = min(1.0, exp / to_next) if to_next else 0
    if fill > 0:
        d.rectangle((1204, y0 + 56, 1204 + int((bar_end - 1204) * fill), y0 + 64), fill=STAR if is_ready else BONE)
    if is_ready and with_button:
        button(d, (1412, y0 + 20, 1560, y0 + 72), "▲ 레벨 업", UP_FILL, UP_TEXT, 22)


def clear_footer(d):
    """The shot was taken with the barracks under construction; the warning line goes (it does not stop a level-up)."""
    d.rectangle((600, 958, 1320, 998), fill=WINDOW)


def barracks_rows(img, rowan_done=False, with_button=True):
    d = ImageDraw.Draw(img)
    clear_footer(d)
    for (name, job, level, exp, to_next), y0 in zip(ROSTER, ROWS):
        stats = row_frame(d, y0, name, job)
        if name == "로언" and rowan_done:
            grown = [s + g for s, g in zip(stats, ROWAN_GAIN)]
            stats_cells(d, y0, grown, ROWAN_GAIN)
            level_block(d, y0, 2, exp - to_next, 25, with_button)
            d.rounded_rectangle((64, y0, 1856, y0 + 92), radius=2, outline=EDGE, width=2)
        else:
            stats_cells(d, y0, stats)
            level_block(d, y0, level, exp, to_next, with_button)
    return img


def barracks_a():
    """안 A (권장): the button in the row; the row itself shows the roll — +N beside each value that grew, a gold rim
    until the window closes. 로언 was just levelled; 엘라·카이·미라 still wait."""
    return barracks_rows(load("base-barracks.png"), rowan_done=True)


def barracks_b():
    """안 B: a click on the row (the mercenary) opens its card; [레벨 업] on the card turns its values into before → after."""
    img = barracks_rows(load("base-barracks.png"), rowan_done=True, with_button=False)
    d = ImageDraw.Draw(img)
    for y0, (name, job, level, exp, to_next) in zip(ROWS, ROSTER):
        if ready(level, exp, to_next) and name != "로언":
            label(d, (1560, y0 + 31), "▲", 22, STAR, "lm")
    img = dim(img)
    d = ImageDraw.Draw(img)
    x0, y0, x1, y1 = 600, 200, 1320, 900
    d.rectangle((x0, y0, x1, y1), fill=WINDOW, outline=EDGE, width=2)
    title, stats, passive, hp = JOBS["warrior"]
    label(d, (x0 + 32, y0 + 44), "로언", 40, TEXT, "lm")
    label(d, (x0 + 32 + width("로언", 40) + 18, y0 + 48), f"{title} · Lv 1 → 2", 26, STAR, "lm")
    label(d, (x0 + 32, y0 + 96), passive, 20, DIM, "lm")
    label(d, (x0 + 32, y0 + 136), "경험치 5/25", 20, DIM, "lm")
    d.rectangle((x0 + 32, y0 + 160, x1 - 32, y0 + 168), fill=STAGE)
    d.rectangle((x0 + 32, y0 + 160, x0 + 32 + int((x1 - x0 - 64) * 5 / 25), y0 + 168), fill=BONE)
    for i in range(6):
        cx = x0 + 32 + (i % 3) * 220
        cy = y0 + 210 + (i // 3) * 120
        d.rectangle((cx, cy, cx + 204, cy + 104), fill=ROW)
        label(d, (cx + 16, cy + 26), LABELS[i], 20, DIM, "lm")
        before, gain = stats[i], ROWAN_GAIN[i]
        if gain:
            label(d, (cx + 16, cy + 70), str(before), 26, DIM, "lm")
            label(d, (cx + 60, cy + 70), "→", 24, DIM, "lm")
            label(d, (cx + 96, cy + 70), str(before + gain), 34, tone(before + gain), "lm")
            label(d, (cx + 146, cy + 70), f"+{gain}", 22, GOOD, "lm")
        else:
            label(d, (cx + 16, cy + 70), str(before), 34, tone(before), "lm")
    label(d, (x0 + 32, y0 + 480), "최대 HP 41.8 → 43.7", 24, GOOD, "lm")
    button(d, (x0 + 32, y1 - 110, x0 + 330, y1 - 40), "레벨 업 완료", QUIET, DIM, 24)
    button(d, (x1 - 330, y1 - 110, x1 - 32, y1 - 40), "닫기", BUTTON, TEXT, 24)
    return img


def barracks_c():
    """안 C: the button in the row, and the roll in a popup over the barracks; [확인] closes it."""
    img = barracks_rows(load("base-barracks.png"), rowan_done=True)
    img = dim(img)
    d = ImageDraw.Draw(img)
    x0, y0, x1, y1 = 620, 270, 1300, 800
    d.rectangle((x0, y0, x1, y1), fill=WINDOW, outline=EDGE, width=2)
    title, stats, _, _ = JOBS["warrior"]
    label(d, (960, y0 + 60), "레벨 업!", 48, STAR, "mm")
    label(d, (960, y0 + 120), f"로언 · {title}   Lv 1 → Lv 2", 28, TEXT, "mm")
    for i in range(6):
        cx = x0 + 70 + (i % 2) * 300
        cy = y0 + 180 + (i // 2) * 56
        before, gain = stats[i], ROWAN_GAIN[i]
        label(d, (cx, cy), LABELS[i], 24, DIM, "lm")
        if gain:
            label(d, (cx + 90, cy), f"{before} → {before + gain}", 26, tone(before + gain), "lm")
            label(d, (cx + 200, cy), f"+{gain}", 24, GOOD, "lm")
        else:
            label(d, (cx + 90, cy), str(before), 26, tone(before), "lm")
    label(d, (960, y0 + 380), "최대 HP 41.8 → 43.7", 26, GOOD, "mm")
    button(d, (760, y1 - 100, 1160, y1 - 30), "확인", BLUE, TEXT, 30)
    return img


# ---------------------------------------------------------------- settlement
def settlement(level_line):
    """The settlement as the party comes back (Round 81's frame). level_line: the stage 34 growth lines (지금) or the
    one line that sends the player to the barracks (안)."""
    img = load("base-settlement.png")
    d = ImageDraw.Draw(img)
    grow = 80 if not level_line else 46
    d.rectangle((410, 100, 1510, 980), fill=(0x16, 0x16, 0x1C))
    top, bottom = 160 - grow // 2, 920 + grow // 2
    d.rectangle((410, top, 1510, bottom), fill=WINDOW)
    up = -(grow // 2)
    label(d, (960, 245 + up), "던전 클리어", 64, GOOD, "mm")
    label(d, (960, 316 + up), "버려진 광산", 32, DIM, "mm")
    label(d, (490, 402 + up), "생환: 로언, 카이, 엘라, 미라", 30, TEXT, "lm")
    label(d, (490, 458 + up), "전사: 없음", 30, TEXT, "lm")
    label(d, (490, 530 + up), "생환자의 피로도: 로언 64, 카이 30, 엘라 102 (공포), 미라 41", 28, TEXT, "lm")
    label(d, (490, 578 + up), "5일이 지나 6일차가 되었습니다", 28, TEXT, "lm")
    label(d, (490, 626 + up), "얻은 금 +80 (30층 도달) · 보유 금 80", 28, GOLD, "lm")
    if not level_line:
        label(d, (490, 674 + up), "경험치 +20 · 레벨 업: 로언 Lv 2, 엘라 Lv 2", 28, BONE, "lm")
        y = 718 + up
        for who, gains in [("로언 Lv 2", ["힘 +1", "체력 +1", "인내 +1"]), ("엘라 Lv 2", ["정신력 +2", "지식 +1"])]:
            label(d, (520, y), who, 24, BONE, "lm")
            x = 520 + width(who, 24) + 16
            for j, g in enumerate(gains):
                if j:
                    label(d, (x, y), "·", 24, DIM, "lm"); x += width("·", 24) + 8
                label(d, (x, y), g, 24, GOOD, "lm"); x += width(g, 24) + 8
            y += 34
        lost = 800 + up
    else:
        label(d, (490, 674 + up), "경험치 +20", 28, BONE, "lm")
        label(d, (520, 718 + up), "▲ 레벨 업 가능: 로언, 카이, 엘라, 미라 — 막사에서 올린다", 24, STAR, "lm")
        lost = 766 + up
    label(d, (490, lost), "원정에서 얻은 아이템과 포션은 모두 사라졌습니다", 24, DIM, "lm")
    button(d, (760, bottom - 110, 1160, bottom - 20), "확인", BLUE, TEXT, 36)
    return img


# ---------------------------------------------------------------- lobby
STRIP = [("아스트리드", "viking", "Lv 2", False), ("세드릭", "warrior", "Lv 1", False), ("엘라", "priest", "Lv 1", True),
         ("카이", "ranger", "Lv 1", True), ("미라", "mage", "Lv 1", True), ("로언", "warrior", "Lv 1", True)]


def lobby(new):
    """The roster strip under the town: the job names (재편안) and, for a mercenary who can level, a gold ▲ beside the
    level; the barracks' sign counts them."""
    img = load("base-lobby.png")
    if not new:
        return img
    d = ImageDraw.Draw(img)
    for i, (name, job, lv, up) in enumerate(STRIP):
        x0 = 40 + 184 * i
        d.rectangle((x0 + 4, 1004, x0 + 172, 1030), fill=ROW)
        label(d, (x0 + 10, 1017), JOBS[job][0], 16, DIM, "lm")
        d.rectangle((x0 + 110, 976, x0 + 172, 1002), fill=ROW)
        label(d, (x0 + 166, 989), lv, 16, STAR if up else DIM, "rm")
        if up:
            label(d, (x0 + 166 - width(lv, 16) - 4, 988), "▲", 14, STAR, "rm")
    # The barracks' sign (280..560, 700..748): a small gold count at its right end.
    d.rounded_rectangle((520, 690, 590, 724), radius=12, fill=UP_FILL)
    label(d, (555, 707), "▲4", 20, UP_TEXT, "mm")
    return img


# ---------------------------------------------------------------- jobs
def jobs_sheet():
    """The five jobs side by side: the figure each one takes (four reuse a figure we have; 방랑자 has none yet), its base
    weapon, HP, stats and passive. 성기사 and 마검사 go."""
    img = Image.new("RGBA", (1920, 1080), STAGE + (255,))
    d = ImageDraw.Draw(img)
    label(d, (960, 50), "직업 재편안 — 다섯 직업과 그림·기본 무기 (성기사·마검사는 삭제)", 34, TEXT, "mm")
    cols = [("warrior", "knight", "longsword", "롱소드", "기사의 그림"), ("viking", "valkyrie", "greataxe", "전투도끼", "발키리의 그림"),
            ("ranger", None, "longbow", "장궁", "새 그림 (생성 필요)"), ("mage", "archmage", "fire_staff", "화염 지팡이", "대마법사의 그림"),
            ("priest", "bishop", "healing_staff", "치유의 지팡이", "주교의 그림")]
    for i, (job, figure, weapon, weapon_name, figure_note) in enumerate(cols):
        x0 = 40 + i * 372
        d.rectangle((x0, 100, x0 + 352, 1040), fill=WINDOW)
        title, stats, passive, hp = JOBS[job]
        label(d, (x0 + 176, 140), title, 36, STAR if job == "ranger" else TEXT, "mm")
        box = (x0 + 26, 175, x0 + 326, 535)
        if figure:
            art = Image.open(ROOT / f"Assets/@Art/Unit/Job/{figure}.png").convert("RGBA")
            art.thumbnail((300, 360), Image.LANCZOS)
            img.alpha_composite(art, (x0 + 176 - art.width // 2, 535 - art.height))
        else:
            d.rectangle(box, outline=LINE, width=2)
            d.ellipse((x0 + 136, 230, x0 + 216, 310), fill=QUIET)
            d.rounded_rectangle((x0 + 116, 320, x0 + 236, 500), radius=30, fill=QUIET)
            d.arc((x0 + 196, 280, x0 + 296, 480), start=-80, end=80, fill=LINE, width=5)
        d = ImageDraw.Draw(img)
        label(d, (x0 + 176, 560), figure_note, 18, STAR if figure is None else DIM, "mm")
        icon = Image.open(ROOT / f"Assets/@Art/Item/{weapon}.png").convert("RGBA")
        icon.thumbnail((96, 96), Image.LANCZOS)
        img.alpha_composite(icon, (x0 + 30, 590 + (96 - icon.height) // 2))
        d = ImageDraw.Draw(img)
        label(d, (x0 + 140, 622), weapon_name, 24, TEXT, "lm")
        label(d, (x0 + 140, 656), f"최대 HP {hp}", 20, DIM, "lm")
        for k in range(6):
            cx = x0 + 30 + (k % 3) * 104
            cy = 730 + (k // 3) * 64
            label(d, (cx, cy), LABELS[k], 18, DIM, "lm")
            label(d, (cx, cy + 28), str(stats[k]), 28, tone(stats[k]), "lm")
        words, line, y = passive.split(" "), "", 880
        for w in words:
            if width(line + " " + w, 20) > 300 and line:
                label(d, (x0 + 30, y), line.strip(), 20, BONE, "lm"); y += 30; line = ""
            line += " " + w
        label(d, (x0 + 30, y), line.strip(), 20, BONE, "lm")
    return img


# ---------------------------------------------------------------- sheets
def sheet(pairs, name, scale=0.5):
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
    shots = {
        "mock-barracks-A.png": caption(barracks_a(), "안 A (권장) — 줄의 [▲ 레벨 업] → 그 줄의 스탯 칸에 +N, 금빛 테두리 (로언을 막 올림)"),
        "mock-barracks-B.png": caption(barracks_b(), "안 B — 줄(용병)을 누르면 용병 카드, 카드의 [레벨 업] → 값이 전 → 후 (누른 뒤)"),
        "mock-barracks-C.png": caption(barracks_c(), "안 C — 줄의 [▲ 레벨 업] → 결과 창 \"레벨 업!\" (전 → 후, HP), [확인]으로 닫음"),
        "mock-settlement.png": caption(settlement(True), "정산 — 성장 줄 대신 \"레벨 업 가능\" 한 줄 (굴림은 막사에서 누를 때)"),
        "mock-lobby.png": caption(lobby(True), "로비 — 띠의 레벨 옆 ▲(올릴 수 있음), 막사 표지에 ▲4 (직업 이름은 재편안)", top=True),
        "mock-jobs.png": caption(jobs_sheet(), "직업 다섯 — 넷은 있는 그림을 쓰고 방랑자는 새 그림 (기본 무기는 모두 있는 아이템)"),
    }
    for name, img in shots.items():
        img.convert("RGB").save(HERE / name)
    now = lambda img, t: caption(img, t)
    sheet([("지금", now(load("base-barracks.png"), "지금")), ("안 A (권장)", shots["mock-barracks-A.png"]),
           ("안 B", shots["mock-barracks-B.png"]), ("안 C", shots["mock-barracks-C.png"])], "compare-1-barracks.png")
    sheet([("지금 (34단계의 성장 줄)", now(settlement(False), "지금 — 정산에서 굴리고 성장 줄")), ("안 (권장)", shots["mock-settlement.png"])],
          "compare-2-settlement.png")
    sheet([("지금", caption(load("base-lobby.png"), "지금", top=True)), ("안 (권장)", shots["mock-lobby.png"])], "compare-3-lobby.png")


if __name__ == "__main__":
    main()
