# -*- coding: utf-8 -*-
"""Round 60 / stage 22 (the user, 2026-10-11: "A에 색만 빨강으로 다시 목업 제공"): look A of the critical hit (a number 1.6x with the small
word "치명타" above, 1.4 s) in the damage red instead of gold, beside gold A, on the enemy's side and on the party's (an enemy's
critical hit on the knight). Over the boss battle after stage 22 (game/ko_40_after.png). The motion is FloatingTextView's, as in
mock_crit_motion.py. No API call.
  .venv/bin/python ArtPipeline/Archive/60-damage-range/mock_crit_red.py
Writes mock-crit-red.png and mock-crit-red.mp4 (the MP4 stays out of Git).
"""
import subprocess
import sys
import tempfile
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent / "49-inventory-style"))
import mock_inventory_style as S  # noqa: E402

FULL = Image.open(HERE / "game" / "ko_40_after.png").convert("RGB")
ENEMY_SIDE, PARTY_SIDE = (760, 80, 1720, 620), (300, 80, 1260, 620)
RED, GOLD, INK, WHITE, DIM = (226, 76, 60), (255, 196, 77), (16, 12, 10), (238, 238, 238), (176, 168, 150)
RISE, POP_TIME, POP_SCALE, FADE_FROM, SIZE = 84, 0.14, 1.35, 0.55, 34
FONTS = {}


def font(px):
    px = max(8, int(round(px)))
    if px not in FONTS:
        FONTS[px] = ImageFont.truetype(str(S.PRET), px)
    return FONTS[px]


def number(layer, start, age, life, text, color, scale, word=None):
    """One rising number at its age (seconds), as FloatingTextView draws it."""
    if age < 0 or age >= life:
        return
    t = age / life
    rise = 1 - (1 - t) * (1 - t)
    pop = POP_SCALE + (1 - POP_SCALE) * (age / POP_TIME) if age < POP_TIME else 1.0
    alpha = 255 if t < FADE_FROM else int(255 * (1 - (t - FADE_FROM) / (1 - FADE_FROM)))
    x, y = start[0], start[1] - RISE * rise
    d = ImageDraw.Draw(layer)
    px = SIZE * scale * pop
    for text_, size, dy in ((text, px, 0), (word, px * 0.45, -px * 0.95)):
        if text_:
            d.text((x + 2, y + dy + 2), text_, font=font(size), fill=INK + (alpha,), anchor="ms")
            d.text((x, y + dy), text_, font=font(size), fill=color + (alpha,), anchor="ms")


HIT_AT, CRIT_AT = 0.10, 0.45
# Crop coordinates: the enemy side (the boss and the veteran), the party side (the knight in row 1 and the spellblade behind him).
BOSS, VETERAN = (380, 200), (570, 250)
KNIGHT, SPELLBLADE = (480, 270), (290, 270)
PANELS = [
    (ENEMY_SIDE, "지금", "치명타도 보통 피해처럼 빨강 -5",
     lambda l, a: (number(l, VETERAN, a + 0.35, 1.05, "-3", RED, 1.05), number(l, BOSS, a, 1.05, "-5", RED, 1.05))),
    (ENEMY_SIDE, "안 A 금빛 (앞서 권장)", "금빛 1.6배, 위에 작은 \"치명타\", 1.4초",
     lambda l, a: (number(l, VETERAN, a + 0.35, 1.05, "-3", RED, 1.05), number(l, BOSS, a, 1.4, "-5!", GOLD, 1.6, word="치명타"))),
    (ENEMY_SIDE, "안 A 빨강 (요청) — 적이 맞을 때", "빨강 1.6배, 위에 작은 \"치명타\", 1.4초. 보통 피해 -3과 크기·글로 갈린다",
     lambda l, a: (number(l, VETERAN, a + 0.35, 1.05, "-3", RED, 1.05), number(l, BOSS, a, 1.4, "-5!", RED, 1.6, word="치명타"))),
    (PARTY_SIDE, "안 A 빨강 (요청) — 용병이 맞을 때", "감독관의 망치 치명타 -12 (기사 38의 20%를 넘어 원래는 \"큰 피해\" 1.4배), 옆은 보통 피해 -4",
     lambda l, a: (number(l, SPELLBLADE, a + 0.35, 1.05, "-4", RED, 1.05), number(l, KNIGHT, a, 1.4, "-12!", RED, 1.6, word="치명타"))),
]


def panel(index, time):
    box, label, sub, draw = PANELS[index]
    im = FULL.crop(box).convert("RGBA")
    layer = Image.new("RGBA", im.size, (0, 0, 0, 0))
    draw(layer, time - CRIT_AT)
    im = Image.alpha_composite(im, layer).convert("RGB")
    d = ImageDraw.Draw(im)
    d.rectangle([0, 0, 959, 539], outline=(90, 84, 70), width=3)
    w = d.textlength(label, font=font(26))
    d.rectangle([12, 12, 36 + w, 52], fill=(20, 18, 16))
    d.text((24, 32), label, font=font(26), fill=RED if "요청" in label else WHITE, anchor="lm")
    d.rectangle([12, 506, 948, 532], fill=(20, 18, 16))
    d.text((24, 519), sub, font=font(17), fill=DIM, anchor="lm")
    return im


def sheet(time, slow=False):
    out = Image.new("RGB", (1920, 1080), (12, 12, 12))
    for i in range(4):
        out.paste(panel(i, time), ((i % 2) * 960, (i // 2) * 540))
    if slow:
        d = ImageDraw.Draw(out)
        d.rectangle([880, 1040, 1040, 1076], fill=(20, 18, 16))
        d.text((960, 1058), "0.5배 느리게", font=font(20), fill=GOLD, anchor="mm")
    return out


sheet(CRIT_AT + 0.2).save(HERE / "mock-crit-red.png")

FPS, LOOP = 30, 2.0
with tempfile.TemporaryDirectory() as tmp:
    frame = 0
    for speed, loops in ((1.0, 3), (0.5, 1)):
        for _ in range(loops):
            for k in range(int(LOOP / speed * FPS)):
                sheet(k * speed / FPS, slow=speed < 1).save(f"{tmp}/f{frame:05d}.png")
                frame += 1
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-framerate", str(FPS), "-i", f"{tmp}/f%05d.png",
                    "-c:v", "libx264", "-pix_fmt", "yuv420p", "-crf", "20", str(HERE / "mock-crit-red.mp4")], check=True)
print("wrote mock-crit-red.png, mock-crit-red.mp4")
