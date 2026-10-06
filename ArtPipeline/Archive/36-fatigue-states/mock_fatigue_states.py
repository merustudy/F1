"""Fatigue on the expedition screens (round 36, Slice B stage 15): where a mercenary's fatigue shows on the party side and in
battle, how a breakdown (affliction or virtue) is announced, and how the lobby and the settlement name an affliction. Drawn over
the game's screenshots (game/, run 20261006-b14); the fatigue values and states are staged for the pictures. No API call.
  .venv/bin/python ArtPipeline/Archive/36-fatigue-states/mock_fatigue_states.py
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[3]; HERE = Path(__file__).resolve().parent
FONT = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"
GAME = HERE / "game"

TEXT = (0xEB, 0xEB, 0xE6); DIM = (0x9A, 0xA0, 0xAC); INK = (0x18, 0x09, 0x07); PANEL = (0x1E, 0x22, 0x2B)
FATIGUE = (0xCF, 0xBA, 0xF7); BAR = (0x9E, 0x84, 0xDA); DANGER = (0xC8, 0x4B, 0x6E); VIRTUE = (0xF7, 0xC8, 0x4A); TROUGH = (0x2A, 0x1A, 0x1A)
BLOOD = (0x8A, 0x16, 0x18)


def font(size): return ImageFont.truetype(str(FONT), size)


def text(img, xy, s, size, color, anchor="la", stroke=0):
    ImageDraw.Draw(img).text(xy, s, font=font(size), fill=color, anchor=anchor, stroke_width=stroke, stroke_fill=INK)


def hp_bars(img, y_from, y_to):
    """The HP troughs of the party's marks: (x0, x1, y0, y1) of each, found by the blood fill in the screenshot."""
    px = img.load(); bars = []
    for y in range(y_from, y_to):
        x = 0; found = []
        while x < 960:
            p = px[x, y]
            if abs(p[0] - 0x81) < 30 and p[1] < 40 and p[2] < 40:
                s = x
                while x < 960 and (abs(px[x, y][0] - 0x81) < 30 and px[x, y][1] < 40 and px[x, y][2] < 40 or px[x, y][0] > 200): x += 1
                if x - s > 40: found.append((s, x - 1))
            else: x += 1
        if found:
            # The trough is a little taller than the fill: 4 above and 4 below.
            y0 = y
            while abs(px[found[0][0] + 5, y0][0] - 0x81) < 30: y0 -= 1
            y1 = y
            while abs(px[found[0][0] + 5, y1][0] - 0x81) < 30: y1 += 1
            # The white numbers split a bar's fill: a bar is 160 wide (the marks' inset 10 in a column of 180), so a run that
            # starts within 150 of the last bar's start belongs to it.
            starts = []
            for s, e in found:
                if not starts or s - starts[-1] >= 150: starts.append(s)
            for s in starts: bars.append((s - 2, s + 162, y0 - 3, y1 + 3))
            break
    return bars


# ---- Variants of the fatigue mark on the marks under the feet --------------------------------------------------------------

def strip_inside(img, bar, fatigue, max_fatigue=200, breakdown=100):
    """A: the lower part of the HP trough is the fatigue strip (Darkest Dungeon's stress bar under the HP bar)."""
    x0, x1, y0, y1 = bar; d = ImageDraw.Draw(img)
    sy0, sy1 = y1 - 6, y1 - 2
    d.rectangle((x0 + 3, sy0 - 1, x1 - 3, sy1 + 1), fill=TROUGH)
    w = (x1 - x0 - 6) * min(fatigue, max_fatigue) / max_fatigue
    d.rectangle((x0 + 3, sy0, x0 + 3 + w, sy1), fill=DANGER if fatigue >= breakdown else BAR)
    tx = x0 + 3 + (x1 - x0 - 6) * breakdown / max_fatigue
    d.line((tx, sy0 - 1, tx, sy1 + 1), fill=(0xE8, 0xDE, 0xC8), width=1)


def bar_below(img, bar, fatigue, max_fatigue=200, breakdown=100):
    """B: a thin bar of its own right under the HP bar, as wide as it."""
    x0, x1, y0, y1 = bar; d = ImageDraw.Draw(img)
    by0, by1 = y1 + 2, y1 + 6
    d.rectangle((x0, by0, x1, by1), fill=TROUGH)
    w = (x1 - x0) * min(fatigue, max_fatigue) / max_fatigue
    d.rectangle((x0, by0, x0 + w, by1), fill=DANGER if fatigue >= breakdown else BAR)


def pips_below(img, bar, fatigue, max_fatigue=200, breakdown=100):
    """C: ten pips under the HP bar, as in the lobby."""
    x0, x1, y0, y1 = bar; d = ImageDraw.Draw(img)
    by0, by1 = y1 + 2, y1 + 6; n = 10; gap = 2; w = (x1 - x0 - gap * (n - 1)) / n; pip = max_fatigue / n
    for i in range(n):
        px0 = x0 + i * (w + gap); fill = min(1, max(0, (fatigue - i * pip) / pip))
        d.rectangle((px0, by0, px0 + w, by1), fill=TROUGH)
        if fill > 0: d.rectangle((px0, by0, px0 + w * fill, by1), fill=DANGER if i * pip >= breakdown else BAR)


def state_on_line(img, bar, job, state, color, shift=0):
    """The state line under the bar names the affliction or virtue after the job, in its colour."""
    x0, x1, y0, y1 = bar; d = ImageDraw.Draw(img)
    ly = y1 + 4 + shift
    d.rectangle((x0 - 2, ly, x1 + 2, ly + 20), fill=(0x14, 0x14, 0x1A))
    text(img, (x0 + 6, ly + 1), job, 15, DIM)
    jw = d.textlength(job, font=font(15))
    text(img, (x0 + 6 + jw, ly + 1), " · " + state, 15, color)


# Staged: row 4 (leftmost) fatigue 32; row 3 61; row 2 118 afflicted (공포); row 1 44 virtuous (집중).
PARTY_STAGE = [(32, None), (61, None), (118, ("공포", DANGER)), (44, ("집중", VIRTUE))]
JOBS = ["대마법사", "주교", "마검사", "기사"]


def party_variant(src, how):
    img = src.copy(); bars = hp_bars(img, 500, 560)
    assert len(bars) == 4, bars
    for bar, (fatigue, state), job in zip(bars, PARTY_STAGE, JOBS):
        if how == "A": strip_inside(img, bar, fatigue)
        if how == "B": bar_below(img, bar, fatigue)
        if how == "C": pips_below(img, bar, fatigue)
        if state: state_on_line(img, bar, job, state[0], state[1], shift=0 if how == "A" else 4)
    return img


def sheet(entries, cols, path, lab=44, gap=24, size=26, footer=None):
    w = max(im.width for _, im in entries); h = max(im.height for _, im in entries); rows = (len(entries) + cols - 1) // cols
    lines = footer or []; fh = 34 * len(lines) + (16 if lines else 0)
    out = Image.new("RGB", (cols * (w + gap) + gap, rows * (h + lab + gap) + gap + fh), (18, 18, 18)); d = ImageDraw.Draw(out)
    for k, (name, im) in enumerate(entries):
        x = gap + (k % cols) * (w + gap); y = gap + (k // cols) * (h + lab + gap)
        d.text((x, y), name, font=font(size), fill=(235, 235, 235)); out.paste(im.convert("RGB"), (x, y + lab))
    for j, s in enumerate(lines): d.text((gap, out.height - fh + j * 34), s, font=font(22), fill=(190, 190, 190))
    out.save(path); print(path.name, out.size)


def sheet_party():
    src = Image.open(GAME / "ko_30_map_fatigue.png").convert("RGB")
    crop = (100, 440, 900, 620)
    names = {"A": "A: HP 막대 안 아래쪽의 피로 띠 (다키스트 던전 방식, 권장)", "B": "B: HP 막대 아래의 가는 피로 막대", "C": "C: HP 막대 아래의 칸 열 개 (로비처럼)"}
    entries = [("지금", src.crop(crop))] + [(names[k], party_variant(src, k).crop(crop)) for k in "ABC"]
    zoom = party_variant(src, "A").crop((500, 505, 880, 570)).resize((760, 130), Image.LANCZOS)
    entries.append(("A를 2배로: 마검사 118(붕괴 넘음, 붉음) · 공포, 기사 44 · 집중", zoom))
    sheet(entries, 1, HERE / "mock-party.png",
          footer=["피로 32·61·118·44로 꾸밈. 100의 눈금은 뼈색 선. 100을 넘으면 띠가 붉은 보라. 상태(고통은 붉은 보라, 각성은 금빛)는 직업 줄 끝에 이름으로.",
                  "전투 화면의 발밑 표시도 같은 자리(HP 막대 안 아래쪽)에 같은 띠다. 적에게는 없다."])


# ---- Battle: the strip and the breakdown announcement -----------------------------------------------------------------------

def battle_sheet():
    src = Image.open(GAME / "ko_15_battle_after_advance.png").convert("RGB")
    bars = hp_bars(src, 380, 700)
    img = src.copy()
    # From the left (row 4 first): the spellblade afflicted, the bishop, the paladin, the valkyrie virtuous.
    stage = [(104, ("공포", DANGER)), (66, None), (28, None), (40, ("집중", VIRTUE))][:len(bars)]
    for bar, (fatigue, state) in zip(bars, stage):
        strip_inside(img, bar, fatigue)
        if state:
            x0, x1, y0, y1 = bar
            text(img, (x1 - 4, y1 + 5), state[0], 15, state[1], anchor="ra", stroke=1)
    quiet = img.copy()
    # A: a plate rises from the unit who broke down (as the big numbers do), in the state's colour; the header's caption says it.
    a = img.copy(); d = ImageDraw.Draw(a)
    bx0, bx1, by0, by1 = bars[0]; cx = (bx0 + bx1) / 2
    d.rounded_rectangle((cx - 86, by0 - 230, cx + 86, by0 - 172), radius=8, fill=(0x20, 0x12, 0x18), outline=DANGER, width=3)
    text(a, (cx, by0 - 201), "공포!", 34, DANGER, anchor="mm", stroke=2)
    text(a, (cx, by0 - 160), "붕괴 · 피로 104", 16, FATIGUE, anchor="mm", stroke=1)
    d.rectangle((300, 56, 1000, 80), fill=(0x1C, 0x1C, 0x22)); text(a, (304, 58), "카이가 공포에 빠졌다", 18, DANGER)
    # B: a banner across the stage (Darkest Dungeon's way): the stage darkens for a moment.
    b = img.copy(); veil = Image.new("RGBA", b.size, (0, 0, 0, 0)); ImageDraw.Draw(veil).rectangle((0, 84, 1920, 600), fill=(0, 0, 0, 110))
    b = Image.alpha_composite(b.convert("RGBA"), veil).convert("RGB"); d = ImageDraw.Draw(b)
    d.rectangle((0, 300, 1920, 384), fill=(0x1A, 0x0C, 0x12)); d.line((0, 300, 1920, 300), fill=DANGER, width=3); d.line((0, 384, 1920, 384), fill=DANGER, width=3)
    text(b, (960, 342), "카이 — 공포에 빠졌다", 40, DANGER, anchor="mm", stroke=2)
    # A virtue, the same plate in gold.
    v = img.copy(); d = ImageDraw.Draw(v)
    vx0, vx1, vy0, vy1 = bars[3] if len(bars) > 3 else bars[-1]; vcx = (vx0 + vx1) / 2
    d.rounded_rectangle((vcx - 86, vy0 - 230, vcx + 86, vy0 - 172), radius=8, fill=(0x1E, 0x18, 0x08), outline=VIRTUE, width=3)
    text(v, (vcx, vy0 - 201), "집중!", 34, VIRTUE, anchor="mm", stroke=2)
    text(v, (vcx, vy0 - 160), "각성 · 피로 40", 16, FATIGUE, anchor="mm", stroke=1)
    d.rectangle((300, 56, 1000, 80), fill=(0x1C, 0x1C, 0x22)); text(v, (304, 58), "아스트리드가 집중을 얻었다", 18, VIRTUE)
    crop = (0, 40, 1920, 620)
    sheet([("전투: 피로 띠와 상태 이름 (A안의 자리)", quiet.crop(crop)),
           ("붕괴 연출 A: 유닛 위로 떠오르는 판 + 헤더 자막 (권장)", a.crop(crop)),
           ("붕괴 연출 B: 무대를 가로지르는 띠 (다키스트 던전 방식, 잠깐 어두워짐)", b.crop(crop)),
           ("각성: 같은 판을 금빛으로", v.crop(crop))], 1, HERE / "mock-battle.png",
          footer=["판은 큰 피해 숫자처럼 0.3초에 떠올라 1.2초 머문다(배속으로 나눔). 쓰러짐(200)은 판 \"쓰러짐!\"과 자막, 그 뒤는 지금의 빈사 표시 그대로.",
                  "B는 전투를 멈추지 않는다(연출은 결과를 바꾸지 않는다). 상태의 효과(쿨다운·회복·사망 확률)는 발밑 표시의 상태 이름을 누르면이 아니라 보드 패널의 유닛 줄 설명에 적는다."])


# ---- Lobby and settlement -------------------------------------------------------------------------------------------------

def lobby_sheet():
    src = Image.open(GAME / "ko_12_lobby_after.png").convert("RGB"); img = src.copy(); d = ImageDraw.Draw(img)
    # The roster rows: the fatigue text "피로도 N/200" stands at the pips' left; we append the affliction after it on two rows.
    # The rows: the "피로도 N/200" words (the fatigue's pale violet) stand right of the name; one row every 126.
    px = img.load(); rows = []
    for y in range(140, 1040):
        if any(abs(px[x, y][0] - 0xCF) < 20 and abs(px[x, y][1] - 0xBA) < 20 and abs(px[x, y][2] - 0xF7) < 20 for x in range(620, 720, 2)) and (not rows or y - rows[-1] > 40):
            rows.append(y)
    for y, (name, color, fatigue, effect) in zip(rows[:3], [("공포", DANGER, 128, "아이템이 25% 느리게 돈다"), ("무모", DANGER, 104, "빈사의 사망 확률 +25%p"), (None, None, 0, None)]):
        if not name: continue
        # The words end at the pips' right edge (698); the line left of them is empty, so the effect goes there, small and dim.
        d.rectangle((300, y - 6, 702, y + 22), fill=(0x1A, 0x1D, 0x26))
        words = f"피로도 {fatigue}/200 · {name}"
        text(img, (698, y - 4), words, 20, DANGER, anchor="ra")
        text(img, (698 - d.textlength(words, font=font(20)) - 14, y), effect, 15, DIM, anchor="ra")
    settle = Image.open(GAME / "ko_11_settlement.png").convert("RGB"); sd = ImageDraw.Draw(settle)
    sd.rectangle((80, 500, 1840, 540), fill=(0x1E, 0x22, 0x2B))
    text(settle, (84, 504), "생환자의 피로도: 로언 128", 24, TEXT)
    w = sd.textlength("생환자의 피로도: 로언 128", font=font(24))
    text(settle, (84 + w, 504), " (공포)", 24, DANGER)
    w2 = sd.textlength(" (공포)", font=font(24))
    text(settle, (84 + w + w2, 504), ", 카이 71, 엘라 40, 미라 52", 24, TEXT)
    sheet([("로비: 피로도 글 끝에 고통의 이름, 그 왼쪽에 효과 (칸 열 개는 Round 33 그대로)", img.crop((300, 140, 1140, 520))),
           ("정산: 생환자의 피로도에 고통을 괄호로", settle.crop((60, 440, 1200, 560)))], 1, HERE / "mock-lobby.png",
          footer=["고통은 피로가 100 아래로 내려가면 풀린다(쉬는 날 10씩). 로비는 각성을 보이지 않는다(원정이 끝나면 풀린다)."])


if __name__ == "__main__":
    sheet_party(); battle_sheet(); lobby_sheet()
