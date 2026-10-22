# -*- coding: utf-8 -*-
"""Round 96, second pass (2026-10-22, the user's "1. 잇는 선으로(각 아이템 중심점끼리) / 3. 금빛 고리 제거, 합쳐질 때 금빛이 퍼지는 다른 이펙트
제공(합쳐지기 전에는 따로 없다가) / 4. 막음 / 나머지 권고안. 목업 제공. 내 승인 후 구현").

  .venv/bin/python ArtPipeline/Archive/96-density-unique/mock_round96b.py

mock2-link.png             the link as a line between the pieces' middles: the five cases at 1.4x, and the line at the game's own size
mock2-effect-v1.mp4 …-v3   the merge after a won battle, three ways of the gold spreading when the pieces merge (nothing before it); input locked
                           until it ends (2 loops at 1x, 1 at 1/2x; not in Git)
mock2-effect-steps.png     the key moments of the three, 카이's board at 2.4x, and the screen locked / unlocked
No paid call. Helpers from mock_round96.py (the first pass).
"""
import math
import random
import shutil
import subprocess
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import mock_round96 as R   # noqa: E402

FPS = R.FPS
TOTAL = 2.4
UNLOCK = 1.3                         # the merge and the band's line are over; the coins flying do not lock
VARIANTS = {
    "v1": "금빛 번짐 — 맞닿은 곳에서 금빛이 아이템을 채우며 퍼지고, 걷히면 유니크",
    "v2": "빛살 — 맞닿은 곳에서 금빛 빛살이 사방으로 뻗었다 사라짐",
    "v3": "금가루 — 맞닿은 곳에서 금가루가 터져 흩어지며 내려앉음",
}


def link_line(img, a, b, colour, width=4, dot=5):
    d = ImageDraw.Draw(img)
    ax, ay = (a[0] + a[2]) / 2, (a[1] + a[3]) / 2
    bx, by = (b[0] + b[2]) / 2, (b[1] + b[3]) / 2
    d.line((ax, ay, bx, by), fill=(20, 16, 8), width=width + 3)
    d.line((ax, ay, bx, by), fill=colour, width=width)
    for x, y in ((ax, ay), (bx, by)):
        d.ellipse((x - dot - 1, y - dot - 1, x + dot + 1, y + dot + 1), fill=(20, 16, 8))
        d.ellipse((x - dot, y - dot, x + dot, y + dot), fill=colour)


# ---- mock2-link.png --------------------------------------------------------------------------------------------------------

def link_sheet():
    S, G = 70, 3
    sheet = Image.new("RGBA", (1920, 1080), R.SHEET_BG + (255,))
    d = ImageDraw.Draw(sheet)
    d.text((40, 28), "Round 96 (2차) · 잇는 선 — 각 아이템의 중심점끼리 (사용자 판정 1)", fill=R.TEXT, font=R.font(30))
    d.text((40, 72), "보드 칸 1.4배. 별(★)은 이 그림에서만 단계를 알리려고 붙였다. 선은 아이콘 위에, 끝에 점.", fill=R.DIM, font=R.font(20))

    def board(x, y, title, items, links, note, colour=R.DIM):
        origin = (x, y + 44)
        d.text((x, y), title, fill=R.TEXT, font=R.font(22))
        R.draw_board(sheet, origin, sq=S, gap=G)
        rects = [R.draw_piece(sheet, origin, c, r, w, h, name, tier, S, G) for (c, r, w, h, name, tier) in items]
        for (i, j, col) in links:
            link_line(sheet, rects[i], rects[j], col, width=5, dot=7)
        ny = y + 44 + 3 * S + 2 * G + 14
        for k, part in enumerate(note.split("\n")):
            ImageDraw.Draw(sheet).text((x, ny + k * 28), part, fill=colour if k == 0 else R.DIM, font=R.font(20))

    sword3 = (0, 0, 3, 1, "longsword", 3)
    y = 130
    d.text((40, y), "경우", fill=R.GOLD, font=R.font(24))
    board(40, y + 46, "1 준비됨 — 금빛 선", [sword3, (2, 1, 1, 1, "whetstone", 3)], [(0, 1, R.LINK_GOLD)], "다음에 이기면 글라디우스.", R.GREEN)
    board(420, y + 46, "2 숫돌 2성 — 흐린 회색 선", [sword3, (0, 1, 1, 1, "whetstone", 2)], [(0, 1, R.LINK_GREY)], "단계가 모자람: 이겨도 안 된다.\n카드의 레시피 줄이 \"숫돌 3성 필요\".")
    board(800, y + 46, "3 숫돌 하나에 바탕 둘 — 선 하나", [(0, 0, 3, 1, "fire_staff", 3), (0, 1, 1, 1, "whetstone", 3), (0, 2, 3, 1, "fire_staff", 3)],
          [(0, 1, R.LINK_GOLD)], "읽는 차례로 첫째(윗줄)와만 잇는다.\n선으로 이어진 하나만 잿불이 된다(판정 3).", R.GREEN)
    board(1180, y + 46, "4 이미 가진 유니크 — 선 없음", [sword3, (0, 1, 1, 1, "whetstone", 3)], [], "같은 유니크는 하나(판정 6).\n카드가 까닭을 말함.")
    board(1560, y + 46, "5 대각선 — 선 없음", [(0, 0, 2, 1, "dagger", 3), (2, 1, 1, 1, "whetstone", 3)], [], "변을 맞대야 한다.\n(단검은 레시피가 없어 예로만.)")

    # The line at the game's size on 카이's board (the won battle's board panel), and at 2x.
    y2 = 640
    d.text((40, y2), "게임의 크기 (칸 50) — 노드 맵·전투의 이긴 뒤·야영지·상점의 보드에 같은 선", fill=R.GOLD, font=R.font(24))
    base = Image.open(R.GAME / "ko_06_battle_won.png").convert("RGBA")
    R.draw_piece(base, R.KAI, 0, 0, 3, 1, "longsword")
    stone = R.draw_piece(base, R.KAI, 0, 1, 1, 1, "whetstone")
    sword = R.piece_rect(R.KAI, 0, 0, 3, 1)
    link_line(base, sword, stone, R.LINK_GOLD)
    crop = base.crop((440, 618, 900, 812))
    sheet.alpha_composite(crop, (40, y2 + 50))
    d.text((40, y2 + 254), "1배 — 선 4px, 끝의 점 지름 10px", fill=R.DIM, font=R.font(20))
    big = base.crop((500, 626, 680, 806)).resize((360, 360), Image.LANCZOS)
    sheet.alpha_composite(big, (560, y2 + 50))
    d.text((560, y2 + 420), "2배", fill=R.DIM, font=R.font(20))
    d.text((980, y2 + 60), "선은 그 보드의 주인 용병의 것만 보인다(두 보드에 걸친 선은 없다).", fill=R.TEXT, font=R.font(20))
    d.text((980, y2 + 96), "아이템을 집어 들면 그 아이템의 선은 사라지고, 놓으면 다시 셈한다.", fill=R.TEXT, font=R.font(20))
    d.text((980, y2 + 132), "전투 중에는 보드가 바뀌지 않으므로 선도 그대로 보인다.", fill=R.TEXT, font=R.font(20))
    sheet.convert("RGB").save(R.HERE / "mock2-link.png")
    print("목업: mock2-link.png")


# ---- the effect ------------------------------------------------------------------------------------------------------------

def ease(t):
    return R.ease(t)


def battle_base():
    img = Image.open(R.GAME / "ko_06_battle_won.png").convert("RGBA")
    R.draw_piece(img, R.KAI, 0, 0, 3, 1, "longsword")
    R.draw_piece(img, R.KAI, 0, 1, 1, 1, "whetstone")
    return img


EMPTY = (12, 12, 12)                 # an empty square of the board as the battle screen draws it (measured in ko_06)
BUTTONS = (1050, 990, 1880, 1056)
DROPS = [(1070, 380, 1220, 560), (1250, 380, 1410, 560)]


def lock_overlay(img, t):
    """Input locked until UNLOCK: the buttons and the drops dim (a dark veil), the hint line says so."""
    if t >= UNLOCK:
        return
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    ld = ImageDraw.Draw(layer)
    ld.rectangle(BUTTONS, fill=(10, 10, 14, 150))
    for box in DROPS:
        ld.rectangle(box, fill=(10, 10, 14, 110))
    img.alpha_composite(layer)


def spread_v1(img, sword, contact, k):
    """Gold fills the piece from the contact point (a widening disc clipped to the piece), then lifts from the rim inward."""
    x0, y0, x1, y1 = sword
    w, h = x1 - x0 + 1, y1 - y0 + 1
    reach = math.hypot(max(contact[0] - x0, x1 - contact[0]), max(contact[1] - y0, y1 - contact[1]))
    mask = Image.new("L", img.size, 0)
    md = ImageDraw.Draw(mask)
    if k < 0.5:
        r = reach * ease(k / 0.5)
        md.ellipse((contact[0] - r, contact[1] - r, contact[0] + r, contact[1] + r), fill=235)
    else:
        md.rectangle(sword, fill=round(235 * (1 - ease((k - 0.5) / 0.5))))
    clip = Image.new("L", img.size, 0)
    ImageDraw.Draw(clip).rectangle((x0 - 3, y0 - 3, x1 + 3, y1 + 3), fill=255)
    mask = Image.composite(mask, Image.new("L", img.size, 0), clip).filter(ImageFilter.GaussianBlur(3))
    gold = Image.new("RGBA", img.size, (255, 214, 90, 255))
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    layer.paste(gold, (0, 0), mask)
    # a soft spill around the piece at the peak
    spill = 1 - abs(k - 0.5) / 0.5
    if spill > 0:
        halo = Image.new("RGBA", img.size, (0, 0, 0, 0))
        ImageDraw.Draw(halo).rounded_rectangle((x0 - 10, y0 - 10, x1 + 10, y1 + 10), radius=10, fill=(255, 210, 80, round(120 * spill)))
        img.alpha_composite(halo.filter(ImageFilter.GaussianBlur(8)))
    img.alpha_composite(layer)


def spread_v2(img, sword, contact, k):
    """Rays of gold from the contact point, growing then fading; the piece flashes at the peak."""
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    ld = ImageDraw.Draw(layer)
    length = 30 + 150 * ease(min(1, k / 0.6))
    a = round(230 * (1 - ease((k - 0.4) / 0.6))) if k > 0.4 else 230
    for i in range(14):
        ang = i * 2 * math.pi / 14 + 0.2
        half = 0.07
        p1 = (contact[0] + math.cos(ang - half) * length, contact[1] + math.sin(ang - half) * length)
        p2 = (contact[0] + math.cos(ang + half) * length, contact[1] + math.sin(ang + half) * length)
        ld.polygon([contact, p1, p2], fill=(255, 214, 90, a))
    img.alpha_composite(layer.filter(ImageFilter.GaussianBlur(2)))
    flash = 1 - abs(k - 0.35) / 0.2 if 0.15 <= k <= 0.55 else 0
    if flash > 0:
        f = Image.new("RGBA", img.size, (0, 0, 0, 0))
        ImageDraw.Draw(f).rectangle(sword, fill=(255, 240, 190, round(210 * flash)))
        img.alpha_composite(f)


PARTICLES = []
_rng = random.Random(96)
for _ in range(46):
    ang = _rng.uniform(0, 2 * math.pi)
    speed = _rng.uniform(80, 230)
    PARTICLES.append((math.cos(ang) * speed, math.sin(ang) * speed - 60, _rng.uniform(2.0, 4.5), _rng.uniform(0, 6.28)))


def spread_v3(img, sword, contact, k):
    """Gold dust bursts from the contact point, falls a little and twinkles out; the piece is tinted gold a moment."""
    tint = 1 - abs(k - 0.3) / 0.3 if k <= 0.6 else 0
    if tint > 0:
        f = Image.new("RGBA", img.size, (0, 0, 0, 0))
        ImageDraw.Draw(f).rectangle(sword, fill=(255, 210, 80, round(150 * tint)))
        img.alpha_composite(f)
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    ld = ImageDraw.Draw(layer)
    s = k * 0.9
    for vx, vy, size, phase in PARTICLES:
        x = contact[0] + vx * s
        y = contact[1] + vy * s + 180 * s * s
        twinkle = 0.6 + 0.4 * math.sin(phase + k * 30)
        a = round(255 * twinkle * (1 - ease((k - 0.45) / 0.55))) if k > 0.45 else round(255 * twinkle)
        r = size * (1 - 0.4 * k)
        ld.ellipse((x - r, y - r, x + r, y + r), fill=(255, 220, 110, max(0, a)))
    img.alpha_composite(layer.filter(ImageFilter.GaussianBlur(0.6)))


SPREAD = {"v1": spread_v1, "v2": spread_v2, "v3": spread_v3}
SWAP = {"v1": 0.75, "v2": 0.62, "v3": 0.62}


def frame(t, base, variant):
    img = base.copy()
    d = ImageDraw.Draw(img)
    sword = R.piece_rect(R.KAI, 0, 0, 3, 1)
    stone = R.piece_rect(R.KAI, 0, 1, 1, 1)
    contact = ((sword[0] + sword[2]) / 2, (sword[1] + sword[3]) / 2)
    # the line, until the merge starts (no glow before the merge: the user's 3)
    if t < 0.15:
        link_line(img, sword, stone, R.LINK_GOLD)
    # 0.15–0.45 the whetstone is drawn into the sword's middle (this is the merge)
    if t >= 0.15:
        k = ease((t - 0.15) / 0.3)
        d.rectangle(stone, fill=EMPTY)
        if k < 1:
            sx, sy = (stone[0] + stone[2]) / 2, (stone[1] + stone[3]) / 2
            R.paste_icon(img, "whetstone", stone, scale=1 - 0.7 * k, alpha=1 - 0.6 * k, offset=((contact[0] - sx) * k, (contact[1] - sy) * k))
    # the unique under the gold
    if t >= SWAP[variant]:
        ImageDraw.Draw(img).rectangle(sword, fill=R.PIECE)
        R.paste_icon(img, "gladius", sword)
    # 0.45–1.05 the gold spreads
    if 0.45 <= t <= 1.05:
        SPREAD[variant](img, sword, contact, (t - 0.45) / 0.6)
    # 0.95–1.3 the band's second line
    line = ease((t - 0.95) / 0.35)
    if line > 0:
        layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
        ld = ImageDraw.Draw(layer)
        ld.rounded_rectangle((800, 168, 1120, 206), radius=6, fill=(18, 20, 28, round(225 * line)), outline=R.GOLD + (round(255 * line),), width=2)
        ld.text((960, 187), "유니크: 글라디우스", fill=R.GOLD + (round(255 * line),), font=R.font(24), anchor="mm")
        img.alpha_composite(layer)
    # 1.1–1.6 the coin pile flies into the band
    fly = ease((t - 1.1) / 0.5)
    if fly < 1:
        x = R.FLOOR_COINS[0] + (R.BAND_COINS[0] - R.FLOOR_COINS[0]) * fly
        y = R.FLOOR_COINS[1] + (R.BAND_COINS[1] - R.FLOOR_COINS[1]) * fly - 120 * (4 * fly * (1 - fly))
        R.coin_pile(img, x, y, scale=1.5 - 0.9 * fly, label="+8" if fly == 0 else None)
    pop = 1 - abs((t - 1.65) / 0.15) if 1.5 <= t <= 1.8 else 0
    if pop > 0:
        layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
        ImageDraw.Draw(layer).rounded_rectangle((950, 108, 1076, 144), radius=6, outline=R.LINK_GOLD + (round(255 * pop),), width=3)
        img.alpha_composite(layer)
    lock_overlay(img, t)
    return R.magnifier(img)


def tag(img, t, speed, variant):
    d = ImageDraw.Draw(img)
    d.rectangle((1430, 84, 1910, 120), fill=(0, 0, 0))
    state = "조작 막힘" if t < UNLOCK else "조작 됨"
    d.text((1440, 102), f"{variant}  t = {t:4.2f}s  {speed}  · {state}", fill=R.TEXT, font=R.font(20), anchor="lm")
    return img


def movie(variant, base):
    normal = [tag(frame(i / FPS, base, variant), i / FPS, "1x", variant) for i in range(round(TOTAL * FPS))]
    slow = [tag(frame(i / (FPS * 2), base, variant), i / (FPS * 2), "1/2x", variant) for i in range(round(TOTAL * FPS * 2))]
    frames = normal * 2 + slow
    out = R.HERE / f"mock2-effect-{variant}.mp4"
    if R.FRAMES_DIR.exists():
        shutil.rmtree(R.FRAMES_DIR)
    R.FRAMES_DIR.mkdir(parents=True)
    for i, f in enumerate(frames):
        f.convert("RGB").resize((1280, 720), Image.LANCZOS).save(R.FRAMES_DIR / f"{i:05d}.png")
    subprocess.run(["swift", str(R.ROOT / "ArtPipeline/Archive/19-motion-test/encode_mp4.swift"), str(R.FRAMES_DIR), str(FPS), str(out)], check=True)
    shutil.rmtree(R.FRAMES_DIR)
    print(f"목업: {out.name} ({len(frames)}장)")


def steps(base):
    moments = [(0.0, "이긴 순간: 선만"), (0.35, "숫돌이 빨려 듦"), (0.6, "금빛이 퍼짐"), (0.8, "퍼짐 · 유니크"), (1.3, "끝 · 띠의 줄")]
    z = 1.5
    cw = round(190 * z)
    sheet = Image.new("RGB", (60 + 160 + len(moments) * (cw + 14), 120 + 3 * (cw + 60) + 360), (20, 18, 18))
    d = ImageDraw.Draw(sheet)
    d.text((24, 16), "Round 96 (2차) · 합쳐질 때 금빛이 퍼지는 이펙트 셋 — 카이의 보드 확대(목업에만), 합쳐지기 전에는 선뿐 (사용자 판정 3)", fill=R.TEXT, font=R.font(24))
    for j, (t, title) in enumerate(moments):
        d.text((200 + j * (cw + 14), 64), f"{t:.2f}초 · {title}", fill=R.DIM, font=R.font(18))
    for i, variant in enumerate(("v1", "v2", "v3")):
        y = 96 + i * (cw + 60)
        name = VARIANTS[variant].split(" — ")[0]
        d.text((24, y + cw / 2 - 30), variant.upper(), fill=R.GOLD, font=R.font(28))
        d.text((24, y + cw / 2 + 6), name, fill=R.TEXT, font=R.font(20))
        if variant == "v1":
            d.text((24, y + cw / 2 + 36), "(권장)", fill=R.GREEN, font=R.font(18))
        for j, (t, _) in enumerate(moments):
            f = frame(t, base, variant).crop((470, 600, 710, 840)).resize((cw, cw), Image.LANCZOS)
            sheet.paste(f.convert("RGB"), (200 + j * (cw + 14), y))
        d.text((200, y + cw + 8), VARIANTS[variant], fill=R.DIM, font=R.font(18))
    y = 96 + 3 * (cw + 60) + 10
    d.text((24, y), "조작 막힘 (사용자 판정 4): 유니크가 만들어진 전투에서 이펙트가 끝날 때까지(1.3초) 아래 단추와 바닥의 전리품이 어둡고 눌리지 않는다. 결과는 이미 저장된 뒤다.", fill=R.TEXT, font=R.font(20))
    zz = 0.3
    fw, fh = round(1920 * zz), round(1080 * zz)
    for j, (t, title) in enumerate(((0.6, "0.60초 — 막힘"), (1.5, "1.50초 — 풀림 (코인은 날아가는 중)"))):
        f = frame(t, base, "v1").convert("RGB").resize((fw, fh), Image.LANCZOS)
        sheet.paste(f, (24 + j * (fw + 20), y + 40))
        d.text((24 + j * (fw + 20), y + 40 + fh + 4), title, fill=R.DIM, font=R.font(18))
    sheet.save(R.HERE / "mock2-effect-steps.png")
    print("목업: mock2-effect-steps.png", sheet.size)


def main():
    link_sheet()
    base = battle_base()
    steps(base)
    for variant in ("v1", "v2", "v3"):
        movie(variant, base)


if __name__ == "__main__":
    main()
