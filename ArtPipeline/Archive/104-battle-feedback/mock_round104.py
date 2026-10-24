# -*- coding: utf-8 -*-
"""Round 104 (2026-10-24, the user's play feedback: "경직 스턴 이펙트(머리위 노란별 여러개가 빙글빙글 돌아가는)" ·
"원정 전투 시작시 초기 1~2초 동안 장착 중인 아이템에 의한 버프/디버프/방어력효과 적용 구현(최대 체력은 미리 적용)" — the user chose
"보여 주기만" — · "효과 아이콘에 마우스 오른쪽 클릭시 설명 툴팁").

  .venv/bin/python ArtPipeline/Archive/104-battle-feedback/mock_round104.py

mock-stun.mp4             the stagger's stars: now (nothing) and A·B·C side by side (2 loops at 1x, 1 at 1/2x; not in Git)
mock-stun-steps.png       the key moments of A·B·C
mock-opening-{now,a,b,c}.mp4  the battle's opening: now (all at 0.0 s at once) and A·B·C (one by one while the clock holds; not in Git)
mock-opening-steps.png    the key moments of A
mock-cards.png            an effect chip's right-click card: A·B·C, and A for other effects at the game's size
No paid call. The base is the screenshot ko_80_battle_armour_start (20261023-d55b) with ko_05's background patched over its floating words.
"""
import math
import shutil
import subprocess
import tempfile
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
SHOTS = Path.home() / "Library/Caches/F1/screenshots/20261023-d55b"
FONT = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"
ICON = ROOT / "Assets/@Art/UI/Icon"
ITEM = ROOT / "Assets/@Art/Item"
FPS = 30

SHEET_BG = (16, 17, 21)
TEXT = (235, 235, 230)
DIM = (150, 150, 145)
GOLD = (0xC7, 0xB3, 0x77)
SHIELD = (0x8F, 0xD3, 0xF4)
CHARGE = (0xFF, 0xCE, 0x68)
THORNS = (0xEB, 0xEB, 0xE6)
GOOD = (0x58, 0xB3, 0x68)
STAR_FILL = (0xFF, 0xD8, 0x4A)
STAR_EDGE = (0x0A, 0x08, 0x06)
COLOURS = {
    "burn": (0xF3, 0x9C, 0x12), "bleed": (0xD8, 0x6A, 0x5A), "regen": (0x58, 0xB3, 0x68), "thorns": THORNS,
    "freeze": (0x9F, 0xD3, 0xF0), "blind": (0xB9, 0x8F, 0xE0), "aim": (0xF0, 0xD4, 0x7A), "keen": (0xDC, 0xE6, 0xF0),
    "fury": (0xF0, 0x6E, 0x5A), "haste": (0xF3, 0xE2, 0x9A), "vulnerable": (0xE0, 0xA0, 0x60), "shield": SHIELD,
    "deaths_door": (0xE2, 0x3C, 0x30),
}

# Where things stand on the 1920x1080 battle screen (measured on ko_80).
ROEN_BAR = (705, 563, 860, 583)
MIRA_BAR = (130, 563, 290, 583)
ELLA_BAR = (320, 563, 480, 583)
RAT_BAR = (1060, 563, 1218, 583)
ROEN_HEAD = (790, 322)
RAT_HEAD = (1108, 432)
CELL = 52


def font(size):
    return ImageFont.truetype(str(FONT), size)


def icon(name, size):
    return Image.open(ICON / f"{name}.png").convert("RGBA").resize((size, size), Image.LANCZOS)


def item_art(name, w, h):
    art = Image.open(ITEM / f"{name}.png").convert("RGBA")
    art.thumbnail((w, h), Image.LANCZOS)
    return art


def board_cell(board_x, col, row):
    return (board_x + 3 + col * CELL, 645 + row * CELL)


# ---- the base ------------------------------------------------------------------------------------------------------------------

def base():
    """ko_80 without what its opening drew: the floating words, the captions, Roen's badge and blue rim (Kai's bar copied over)."""
    img = Image.open(SHOTS / "ko_80_battle_armour_start.png").convert("RGBA")
    clean = Image.open(SHOTS / "ko_05_battle.png").convert("RGBA")
    for box in ((690, 180, 880, 290), (360, 22, 620, 76)):
        img.paste(clean.crop(box), box[:2])
    img.paste(img.crop((495, 552, 675, 594)), (685, 552))
    # The pieces this mockup adds: Mira's robe, Ella's leather belt (no art yet: the blue placeholder), the first rat's bristle hide.
    d = ImageDraw.Draw(img)
    x, y = board_cell(130, 0, 1)
    art = item_art("robe", 2 * CELL - 8, CELL - 8)
    img.alpha_composite(art, (x + (2 * CELL - art.width) // 2, y + (CELL - art.height) // 2))
    x, y = board_cell(320, 0, 1)
    d.rounded_rectangle((x + 3, y + 3, x + 2 * CELL - 3, y + CELL - 3), 5, fill=(0x2C, 0x4A, 0x7A), outline=(0x6E, 0x96, 0xD0))
    d.text((x + CELL, y + CELL // 2), "가죽 허리띠", fill=TEXT, font=font(13), anchor="mm")
    x, y = board_cell(1060, 0, 1)
    art = item_art("bristle_hide", 2 * CELL - 6, CELL - 6)
    img.alpha_composite(art, (x + (2 * CELL - art.width) // 2, y + (CELL - art.height) // 2))
    # Max HP is there from the first frame (the user's "최대 체력은 미리 적용"): Ella 23 + 24.
    hp_text(img, ELLA_BAR, "47/47")
    return img


def hp_text(img, bar, text):
    d = ImageDraw.Draw(img)
    d.rectangle((bar[0] + 30, bar[1] + 2, bar[2] - 30, bar[3] - 2), fill=(0x9A, 0x1E, 0x1E))
    d.text(((bar[0] + bar[2]) / 2, (bar[1] + bar[3]) / 2), text, fill=(240, 240, 240), font=font(15), anchor="mm")


def clock(img, text):
    d = ImageDraw.Draw(img)
    d.rectangle((880, 935, 1040, 975), fill=img.getpixel((875, 954)))
    d.text((960, 954), text, fill=(235, 235, 235), font=font(27), anchor="mm")


def shield_badge(img, bar, amount):
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((bar[0] - 2, bar[1] - 2, bar[2] + 2, bar[3] + 2), 3, outline=(0x6E, 0xA8, 0xE8), width=2)
    b = icon("shield", 30)
    img.alpha_composite(b, (bar[0] - 22, bar[1] - 5))
    d.text((bar[0] - 7, bar[1] + 10), str(amount), fill=(245, 245, 245), font=font(13), anchor="mm", stroke_width=2,
           stroke_fill=(10, 10, 10))


def chip(img, x, y, name, text, colour=None):
    """A chip of the state line: an 18 icon and its count. Returns the chip's box."""
    img.alpha_composite(icon(name, 18), (x, y))
    d = ImageDraw.Draw(img)
    f = font(15)
    d.text((x + 21, y + 9), text, fill=colour or COLOURS[name], font=f, anchor="lm", stroke_width=2, stroke_fill=(12, 10, 8))
    w = 21 + int(d.textlength(text, font=f))
    return (x, y, x + w, y + 18)


def flash(img, box, a):
    """An item's activation: the piece brightens and its square rims gold."""
    if a <= 0:
        return
    over = Image.new("RGBA", img.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(over)
    d.rectangle(box, fill=(255, 240, 200, int(70 * a)), outline=(0xFF, 0xCE, 0x68, int(255 * a)), width=3)
    img.alpha_composite(over)


def float_text(img, centre, text, colour, age, size=44, life=1.0, rise=46):
    """The game's floating words: rise and fade over a second."""
    if age < 0 or age > life:
        return
    a = 1.0 if age < life * 0.6 else 1 - (age - life * 0.6) / (life * 0.4)
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    y = centre[1] - rise * min(1.0, age / life * 1.4)
    d.text((centre[0], y), text, fill=colour + (int(255 * a),), font=font(size), anchor="mm", stroke_width=3,
           stroke_fill=(15, 12, 10, int(200 * a)))
    img.alpha_composite(layer)


def captions(img, lines):
    d = ImageDraw.Draw(img)
    for i, line in enumerate(lines[-2:]):
        newest = i == len(lines[-2:]) - 1
        d.text((372, 34 + i * 20), line, fill=(235, 235, 235) if newest else (150, 150, 150), font=font(14))


def tag(img, text, sub=None):
    d = ImageDraw.Draw(img)
    f = font(26)
    w = d.textlength(text, font=f)
    d.rounded_rectangle((16, 1000, 40 + w, 1064), 8, fill=(0, 0, 0, 200))
    d.text((28, 1032), text, fill=(255, 220, 120), font=f, anchor="lm")
    if sub:
        d.text((60 + w, 1032), sub, fill=TEXT, font=font(20), anchor="lm", stroke_width=3, stroke_fill=(0, 0, 0))


# ---- 2. the opening ------------------------------------------------------------------------------------------------------------

ROEN_ARMOUR = (703, 697, 807, 801)
ROEN_HELM = (807, 697, 858, 801)
MIRA_ROBE = (133, 697, 237, 749)
RAT_HIDE = (1063, 697, 1167, 749)
MIRA_HEAD = (210, 300)
RAT_FLOAT = (1140, 400)
ROEN_FLOAT = (780, 250)

# One activation: when, which piece, the words over whom, what it leaves (badge / chip), its caption line.
STEPS = [
    dict(piece=ROEN_ARMOUR, words=[(ROEN_FLOAT, "방어 +8", SHIELD)], badge=("roen", 13), line="로언의 철 갑옷 → 로언: 보호막 +8"),
    dict(piece=ROEN_HELM, words=[(ROEN_FLOAT, "방어 +4", SHIELD)], badge=("roen", 18), line="로언의 철 투구 → 로언: 보호막 +4"),
    dict(piece=MIRA_ROBE, words=[(MIRA_HEAD, "방어 +5", SHIELD), (MIRA_HEAD, "충전 1.5초", CHARGE)],
         badge=("mira", 5), line="미라의 로브 → 미라: 보호막 +5 · 충전 1.5초"),
    dict(piece=RAT_HIDE, words=[(RAT_FLOAT, "가시 +1", THORNS)], chip=True, line="동굴 쥐의 가시 털가죽 → 동굴 쥐: 가시 +1"),
]
BELT_LINE = "엘라의 가죽 허리띠 → 엘라: 최대 체력 +24"
FIRST, GAP, TAIL, LONGEST = 0.30, 0.35, 0.50, 2.0
OPENING_LOOP = 3.0


def schedule(variant):
    """The time each step fires, and when the clock starts. 'now': everything on the second frame, the clock at once."""
    n = len(STEPS)
    if variant == "now":
        return [1 / FPS] * n, 0.0
    if variant == "b":                                       # unit by unit: Roen's two together
        times = [0.30, 0.30, 0.80, 1.30]
        return times, 1.30 + TAIL
    gap = min(GAP, (LONGEST - FIRST - TAIL) / max(1, n - 1))
    times = [FIRST + i * gap for i in range(n)]
    return times, times[-1] + TAIL


def risen(age, life=1.0, rise=46):
    return rise * min(1.0, age / life * 1.4)


def launches(variant):
    """Where each floating word starts: over its unit, and above any word still rising there (it never covers one: ko_80's rows)."""
    times, _ = schedule(variant)
    out = []
    for step, at in zip(STEPS, times):
        for k, (centre, words, colour) in enumerate(step["words"]):
            y = centre[1]
            for (px, py), _, _, pat in out:
                if px == centre[0] and at - pat < 1.0:
                    y = min(y, py - risen(at - pat) - 40)
            out.append(((centre[0], y), words, colour, at))
    return out


def opening_frame(t, variant, clean):
    img = clean.copy()
    times, start = schedule(variant)
    lines = [BELT_LINE]                                     # the passive's shield has no caption, as now
    roen_badge, mira_badge, rat_chip = 5, 0, False             # Roen's passive (선봉 5.45) is there before the items
    for i, (step, at) in enumerate(zip(STEPS, times)):
        if t < at:
            continue
        age = t - at
        flash(img, step["piece"], 1 - age / 0.35)
        lines.append(step["line"])
        if "badge" in step:
            who, amount = step["badge"]
            if who == "roen":
                roen_badge = amount
            else:
                mira_badge = amount
        if step.get("chip"):
            rat_chip = True
    for (x, y), words, colour, at in launches(variant):
        float_text(img, (x, y), words, colour, t - at)
    if variant == "now":
        float_text(img, (400, 300), "최대 체력 +24", GOOD, t - 1 / FPS)
    shield_badge(img, ROEN_BAR, roen_badge)
    if mira_badge:
        shield_badge(img, MIRA_BAR, mira_badge)
    if rat_chip:
        chip(img, RAT_BAR[0], 597, "thorns", "1")
    captions(img, lines)
    running = max(0.0, t - start)
    if variant == "c" and t < start:
        clock(img, "전투 준비")
    else:
        clock(img, f"{running:.1f}초")
    return img


def write_movie(path, frame_at, loop, size=None):
    tmp = Path(tempfile.mkdtemp())
    frames = []
    for speed, loops in ((1.0, 2), (0.5, 1)):
        n = int(round(loop / speed * FPS))
        for _ in range(loops):
            for i in range(n):
                frames.append((i / FPS * speed, speed))
    for k, (t, speed) in enumerate(frames):
        img = frame_at(t, speed)
        if size:
            img = img.resize(size, Image.LANCZOS)
        img.convert("RGB").save(tmp / f"f{k:05d}.png")
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-framerate", str(FPS), "-i", str(tmp / "f%05d.png"),
                    "-c:v", "libx264", "-pix_fmt", "yuv420p", "-crf", "20", str(path)], check=True)
    shutil.rmtree(tmp)


OPENING_NAMES = {
    "now": ("지금", "0.0초에 한꺼번에 · 글이 겹침 · 시계는 바로 감 (가시·충전은 글 없음)"),
    "a": ("A (권장)", "하나씩 0.35초 간격 · 시계 0.0초에 멈춤 · 끝나고 0.5초 뒤 시작"),
    "b": ("B", "유닛마다 한꺼번에 0.5초 간격"),
    "c": ("C", "A + 시계 자리에 \"전투 준비\""),
}


def opening_movies(clean):
    for v, (name, sub) in OPENING_NAMES.items():
        def frame_at(t, speed, v=v):
            img = opening_frame(t, v, clean)
            tag(img, f"{name} · {'x1' if speed == 1 else '느리게 1/2'}", sub)
            return img
        write_movie(HERE / f"mock-opening-{v}.mp4", frame_at, OPENING_LOOP, size=(1440, 810))


def opening_steps(clean):
    times, start = schedule("a")
    moments = [(0.0, "0.0초 — 화면이 열림. 최대 체력은 이미 47/47, 선봉의 5"), (times[0] + 0.15, "0.3초 — 철 갑옷: 방어 +8, 배지 13"),
               (times[1] + 0.15, "0.65초 — 철 투구: 방어 +4, 배지 18"), (times[2] + 0.2, "1.0초 — 로브: 방어 +5 · 충전 1.5초"),
               (times[3] + 0.2, "1.35초 — 쥐의 가시 털가죽: 가시 +1, 칩"), (start + 0.4, "1.85초부터 — 시계가 간다")]
    W, H = 640, 530
    sheet = Image.new("RGBA", (W * 3 + 80, 150 + (H + 60) * 2), SHEET_BG + (255,))
    d = ImageDraw.Draw(sheet)
    d.text((40, 28), "Round 104 · 2. 전투의 시작 — A안의 순간들 (목업의 로브·허리띠·가시 털가죽은 보이려고 넣은 것)", fill=TEXT, font=font(30))
    d.text((40, 76), "시계는 1.85초 동안 0.0초에 멈춰 있다(전투의 시간이 아니라 화면의 시간). x2는 그 절반, x4는 4분의 1. 규칙·시뮬·저장은 그대로.",
           fill=DIM, font=font(20))
    for i, (t, label) in enumerate(moments):
        img = opening_frame(t, "a", clean).crop((60, 20, 1300, 1000)).resize((W, int(W * 980 / 1240)), Image.LANCZOS)
        x, y = 40 + (i % 3) * W, 140 + (i // 3) * (H + 60)
        sheet.alpha_composite(img.crop((0, 0, W, H - 10)), (x, y + 34))
        d.text((x, y), label, fill=(255, 220, 120), font=font(19))
    sheet.convert("RGB").save(HERE / "mock-opening-steps.png")


# ---- 1. the stagger's stars ----------------------------------------------------------------------------------------------------

def star_points(cx, cy, r, turn=0.0):
    pts = []
    for k in range(10):
        a = -math.pi / 2 + k * math.pi / 5 + turn
        rr = r if k % 2 == 0 else r * 0.45
        pts.append((cx + rr * math.cos(a), cy + rr * math.sin(a)))
    return pts


def draw_star(layer, cx, cy, r, alpha, turn=0.0, fill=STAR_FILL):
    d = ImageDraw.Draw(layer)
    d.polygon(star_points(cx, cy, r + 2.2, turn), fill=STAR_EDGE + (int(255 * alpha),))
    d.polygon(star_points(cx, cy, r, turn), fill=fill + (int(255 * alpha),))


def stars(img, head, age, length, variant):
    """The stars over a staggered unit for its stagger's time; they fade over the last 0.15 s."""
    if age < 0 or age > length:
        return
    alpha = min(1.0, age / 0.08) * (1.0 if age < length - 0.15 else (length - age) / 0.15)
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    cx, cy = head
    if variant == "c":
        d = ImageDraw.Draw(layer)
        for k in range(12):
            a0 = age * 540 + k * 30
            d.arc((cx - 50, cy - 14, cx + 50, cy + 14), a0, a0 + 14, fill=(255, 216, 74, int(170 * alpha)), width=3)
    count, rx, ry, r, period = {"a": (5, 46, 12, 10, 0.7), "b": (3, 40, 11, 14, 0.9), "c": (5, 46, 12, 10, 0.7)}[variant]
    placed = []
    for k in range(count):
        a = 2 * math.pi * (age / period + k / count)
        x = cx + rx * math.cos(a)
        bob = 5 * math.sin(age * 12 + k) if variant == "b" else 0
        y = cy + ry * math.sin(a) + bob
        depth = (math.sin(a) + 1) / 2                        # 0 at the back, 1 at the front
        placed.append((depth, x, y))
    for depth, x, y in sorted(placed):
        draw_star(layer, x, y, r * (0.75 + 0.35 * depth), alpha * (0.55 + 0.45 * depth), turn=depth * 0.6)
    img.alpha_composite(layer)


STUN_LOOP = 2.6
STUN_CROP = (640, 240, 1250, 640)
# (when, whose head, how long): the hammer staggers the rat 0.8 s, again before that ends (it adds on: to 1.9 s), and Roen 0.8 s.
STAGGERS = [(0.3, "rat", 0.8), (0.9, "rat", 0.8), (1.2, "roen", 0.8)]


def stun_spans():
    """(start, who, end) per run of stars: a stagger while the stars still turn adds its time to their end."""
    spans = []
    for at, who, ms in STAGGERS:
        last = next((s for s in reversed(spans) if s[1] == who), None)
        if last and at < last[2]:
            spans[spans.index(last)] = (last[0], who, last[2] + ms)
        else:
            spans.append((at, who, at + ms))
    return spans


def stun_frame(t, variant, clean):
    img = clean.copy()
    heads = {"rat": RAT_HEAD, "roen": ROEN_HEAD}
    for at, who, _ in STAGGERS:
        float_text(img, (heads[who][0] + 10, heads[who][1] - 40), "2", (240, 240, 240), t - at, size=36, life=0.8, rise=30)
    if variant != "now":
        for start, who, end in stun_spans():
            stars(img, heads[who], t - start, end - start, variant)
    return img.crop(STUN_CROP)


STUN_NAMES = {
    "now": ("지금", "표시 없음 (피해 숫자뿐)"),
    "a": ("A (권장)", "노란 별 다섯이 머리 위 타원을 돈다 (0.7초에 한 바퀴)"),
    "b": ("B", "별 셋, 크게, 위아래로 통통"),
    "c": ("C", "A + 노란 소용돌이 선"),
}


def stun_movie(clean):
    w, h = STUN_CROP[2] - STUN_CROP[0], STUN_CROP[3] - STUN_CROP[1]

    def frame_at(t, speed):
        sheet = Image.new("RGBA", (w * 2 + 60, 110 + (h + 50) * 2), SHEET_BG + (255,))
        d = ImageDraw.Draw(sheet)
        d.text((20, 18), "Round 104 · 1. 경직의 별 — 쥐: 0.3초 경직 0.8초, 0.9초에 또 (이어 붙어 1.9초까지) / 로언: 1.2초 경직 0.8초",
               fill=TEXT, font=font(20))
        d.text((20, 52), ("x1" if speed == 1 else "느리게 1/2") + f" · {t:.2f}초", fill=(255, 220, 120), font=font(20))
        for i, (v, (name, sub)) in enumerate(STUN_NAMES.items()):
            x, y = 20 + (i % 2) * (w + 20), 90 + (i // 2) * (h + 50)
            d.text((x, y), f"{name} — {sub}", fill=(255, 220, 120) if i else DIM, font=font(18))
            sheet.alpha_composite(stun_frame(t, v, clean), (x, y + 30))
        return sheet

    write_movie(HERE / "mock-stun.mp4", frame_at, STUN_LOOP)


def stun_steps(clean):
    moments = [0.35, 0.55, 1.0, 1.5, 1.95]
    w, h = STUN_CROP[2] - STUN_CROP[0], STUN_CROP[3] - STUN_CROP[1]
    s = 0.62
    cw, ch = int(w * s), int(h * s)
    sheet = Image.new("RGBA", (40 + len(moments) * (cw + 12), 120 + 3 * (ch + 44)), SHEET_BG + (255,))
    d = ImageDraw.Draw(sheet)
    d.text((40, 24), "Round 104 · 1. 경직의 별 — A·B·C의 순간들 (게임 크기의 0.62배)", fill=TEXT, font=font(28))
    d.text((40, 66), "별은 경직이 민 시간(전투 시간) 동안 돈다: 배속이 빠르면 그만큼 짧다. 또 맞으면 이어 붙는다. 적·아군 모두, 키에 맞춰 머리 위.",
           fill=DIM, font=font(18))
    for r, v in enumerate(("a", "b", "c")):
        y = 110 + r * (ch + 44)
        d.text((40, y), f"{STUN_NAMES[v][0]} — {STUN_NAMES[v][1]}", fill=(255, 220, 120), font=font(18))
        for c, t in enumerate(moments):
            img = stun_frame(t, v, clean).resize((cw, ch), Image.LANCZOS)
            x = 40 + c * (cw + 12)
            sheet.alpha_composite(img, (x, y + 28))
            d.text((x + 6, y + 32), f"{t:.2f}초", fill=TEXT, font=font(15), stroke_width=2, stroke_fill=(0, 0, 0))
    sheet.convert("RGB").save(HERE / "mock-stun-steps.png")


# ---- 3. the effect chips' cards ------------------------------------------------------------------------------------------------

# A card's words: (icon, title, kind, what it does now, the rule). The numbers are BalanceData's (2026-10-24).
CARDS = {
    "freeze": ("빙결 3", "해로운 상태", "아이템의 쿨다운이 3% 느려진다", "중첩 1에 1% · 다음 발동부터 · 1초마다 1씩 준다"),
    "shield": ("보호막 18", "방어", "피해를 HP보다 먼저 18 막는다", "출혈은 막지 못한다 · 전투가 끝나면 사라진다"),
    "deaths_door": ("빈사", "위험", "피격마다 사망 판정 32%", "HP를 회복하면 벗어난다"),
    "thorns": ("가시 1", "이로운 상태", "근접 무기로 때린 적에게 피해 1", "전투가 끝날 때까지 남는다"),
    "haste": ("가속 3", "이로운 상태", "아이템의 쿨다운이 3% 빨라진다", "중첩 1에 1% · 다음 발동부터 · 1초마다 1씩 준다"),
    "vulnerable": ("취약 2", "해로운 상태", "받는 무기·공격 아이템의 피해 +10%", "중첩 1에 5% · 1초마다 1씩 준다"),
    "bleed": ("출혈 2", "해로운 상태", "1초마다 피해 2 (보호막을 뚫는다)", "1초마다 1씩 준다 · 회복 한 번에 1씩 준다"),
}


def card_a(name, scale=1.0):
    title, kind, now, rule = CARDS[name]
    s = scale
    f1, f2, f3 = font(int(22 * s)), font(int(15 * s)), font(int(17 * s))
    probe = ImageDraw.Draw(Image.new("RGBA", (1, 1)))
    w = int(max(probe.textlength(now, font=f3), probe.textlength(rule, font=f2), 180 * s) + 40 * s)
    h = int(118 * s)
    card = Image.new("RGBA", (w, h + int(10 * s)), (0, 0, 0, 0))
    d = ImageDraw.Draw(card)
    d.rectangle((0, 0, w - 1, h - 1), fill=(0x1C, 0x1F, 0x26, 245), outline=GOLD, width=max(1, int(2 * s)))
    tip = w // 2
    d.polygon([(tip - 9 * s, h - 2), (tip + 9 * s, h - 2), (tip, h + 9 * s)], fill=GOLD)
    card.alpha_composite(icon(name, int(28 * s)), (int(18 * s), int(14 * s)))
    colour = COLOURS[name]
    d.text((int(56 * s), int(28 * s)), title, fill=colour, font=f1, anchor="lm")
    d.text((int(18 * s), int(58 * s)), kind, fill=DIM, font=f2, anchor="lm")
    d.text((int(18 * s), int(80 * s)), now, fill=colour, font=f3, anchor="lm")
    d.text((int(18 * s), int(102 * s)), rule, fill=DIM, font=f2, anchor="lm")
    return card


def card_b(name, scale=1.0):
    title, _, now, _ = CARDS[name]
    text = f"{title} · {now}"
    f = font(int(16 * scale))
    probe = ImageDraw.Draw(Image.new("RGBA", (1, 1)))
    w, h = int(probe.textlength(text, font=f) + 24 * scale), int(30 * scale)
    card = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(card)
    d.rectangle((0, 0, w - 1, h - 1), fill=(0, 0, 0, 220), outline=(90, 90, 90), width=1)
    d.text((12 * scale, h / 2), text, fill=COLOURS[name], font=f, anchor="lm")
    return card


def card_c(scale=1.0):
    rows = ["freeze", "vulnerable", "bleed"]
    s = scale
    f1, f2, f3 = font(int(20 * s)), font(int(15 * s)), font(int(16 * s))
    w, h = int(420 * s), int((56 + 50 * len(rows)) * s)
    card = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(card)
    d.rectangle((0, 0, w - 1, h - 1), fill=(0x1C, 0x1F, 0x26, 245), outline=GOLD, width=max(1, int(2 * s)))
    d.text((int(16 * s), int(24 * s)), "동굴 쥐 — 효과 셋", fill=TEXT, font=f1, anchor="lm")
    for i, name in enumerate(rows):
        title, _, now, rule = CARDS[name]
        y = int((56 + 50 * i) * s)
        card.alpha_composite(icon(name, int(22 * s)), (int(16 * s), y))
        d.text((int(46 * s), y + int(11 * s)), f"{title}  {now}", fill=COLOURS[name], font=f3, anchor="lm")
        d.text((int(46 * s), y + int(32 * s)), rule, fill=DIM, font=f2, anchor="lm")
    return card


def with_chips(clean):
    img = clean.copy()
    shield_badge(img, ROEN_BAR, 18)
    x = ROEN_BAR[0]
    for name, text in (("thorns", "1"), ("aim", "8"), ("haste", "3")):
        x = chip(img, x, 597, name, text)[2] + 8
    x = RAT_BAR[0]
    boxes = {}
    for name, text in (("freeze", "3"), ("vulnerable", "2"), ("bleed", "2")):
        boxes[name] = chip(img, x, 597, name, text)
        x = boxes[name][2] + 8
    return img, boxes


def cards_sheet(clean):
    img, boxes = with_chips(clean)
    fb = boxes["freeze"]
    sheet = Image.new("RGBA", (1920, 1330), SHEET_BG + (255,))
    d = ImageDraw.Draw(sheet)
    d.text((40, 24), "Round 104 · 3. 효과 칩의 우클릭 카드 — 동굴 쥐의 빙결 칩을 우클릭 (아이템·특성 카드처럼 다음 누름에 닫힌다)", fill=TEXT, font=font(28))
    d.text((40, 66), "칩: 상태 줄의 열하나(화상~취약)와 빈사, HP 막대 왼쪽의 보호막 배지. 글은 String Table, 수는 그 유닛의 지금 값과 BalanceData.",
           fill=DIM, font=font(19))

    def scene(card, label, x, y, s=1.0):
        crop = (690, 380, 1480, 640)
        g = img.copy()
        cx = (fb[0] + fb[2]) // 2
        g.alpha_composite(card, (cx - card.width // 2, fb[1] - card.height - 6))
        d2 = ImageDraw.Draw(g)
        d2.rectangle((fb[0] - 3, fb[1] - 3, fb[2] + 3, fb[3] + 3), outline=(255, 255, 255), width=1)
        part = g.crop(crop)
        if s != 1.0:
            part = part.resize((int(part.width * s), int(part.height * s)), Image.LANCZOS)
        d.text((x, y), label, fill=(255, 220, 120), font=font(20))
        sheet.alpha_composite(part, (x, y + 32))

    scene(card_a("freeze"), "A (권장) — 특성 카드와 같은 꼴: 이름·수, 갈래, 지금의 효과, 규칙", 40, 110, 1.1)
    scene(card_b("freeze"), "B — 한 줄 툴팁", 1000, 110, 1.1)
    scene(card_c(), "C — 그 유닛의 효과 모두를 한 카드에 (어느 칩을 눌러도)", 40, 500, 1.1)
    # The game's size, the whole stage, with A open.
    g = img.copy()
    card = card_a("freeze")
    cx = (fb[0] + fb[2]) // 2
    g.alpha_composite(card, (cx - card.width // 2, fb[1] - card.height - 6))
    d.text((1000, 500), "A — 게임 크기 (무대의 일부)", fill=(255, 220, 120), font=font(20))
    sheet.alpha_composite(g.crop((690, 380, 1500, 640)), (1000, 532))
    d.text((40, 900), "A의 다른 효과들 (게임 크기의 1.25배) — 수는 그 순간의 값", fill=(255, 220, 120), font=font(20))
    x = 40
    y = 940
    for name in ("shield", "deaths_door", "thorns", "haste", "vulnerable", "bleed"):
        c = card_a(name, 1.25)
        if x + c.width > 1880:
            x, y = 40, y + 190
        sheet.alpha_composite(c, (x, y))
        x += c.width + 24
    sheet.convert("RGB").save(HERE / "mock-cards.png")


def main():
    clean = base()
    cards_sheet(clean)
    opening_steps(clean)
    stun_steps(clean)
    stun_movie(clean)
    opening_movies(clean)


if __name__ == "__main__":
    main()
