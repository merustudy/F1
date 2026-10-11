# -*- coding: utf-8 -*-
"""Round 60 / stage 22, again (the user, 2026-10-11: "치명타 모습 목업 다시 제공해줘"): the three looks for a critical hit's number over
the boss battle as the game draws it after stage 22 (game/ko_40_after.png, screenshots 2026-10-11 s22b), still and moving. The motion is
FloatingTextView's: it pops in at 1.35x and settles in 0.14 s, rises 84 px slowing down, and fades over the last 45% of its life
(an ordinary number 1.05 s, a heavy one 1.4 s). No API call.
  .venv/bin/python ArtPipeline/Archive/60-damage-range/mock_crit_motion.py
Writes mock-crit-floats-v2.png and mock-crit-floats.mp4 (the MP4 stays out of Git).
"""
import subprocess
import sys
import tempfile
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent / "49-inventory-style"))
import mock_inventory_style as S  # noqa: E402

BASE = Image.open(HERE / "game" / "ko_40_after.png").convert("RGB").crop((760, 80, 1720, 620))
RED, GOLD, INK, WHITE, DIM = (226, 76, 60), (255, 196, 77), (16, 12, 10), (238, 238, 238), (176, 168, 150)
RISE, POP_TIME, POP_SCALE, FADE_FROM, SIZE = 84, 0.14, 1.35, 0.55, 34
FONTS = {}


def font(px):
    px = max(8, int(round(px)))
    if px not in FONTS:
        FONTS[px] = ImageFont.truetype(str(S.PRET), px)
    return FONTS[px]


def number(layer, start, age, life, text, color, scale, word=None):
    """One rising number at its age (seconds), as FloatingTextView draws it; nothing before it starts or after it is gone."""
    if age < 0 or age >= life:
        return
    t = age / life
    rise = 1 - (1 - t) * (1 - t)
    pop = POP_SCALE + (1 - POP_SCALE) * (age / POP_TIME) if age < POP_TIME else 1.0
    alpha = 255 if t < FADE_FROM else int(255 * (1 - (t - FADE_FROM) / (1 - FADE_FROM)))
    x, y = start[0], start[1] - RISE * rise
    d = ImageDraw.Draw(layer)
    px = SIZE * scale * pop
    d.text((x + 2, y + 2), text, font=font(px), fill=INK + (alpha,), anchor="ms")
    d.text((x, y), text, font=font(px), fill=color + (alpha,), anchor="ms")
    if word:
        wy = y - px * 0.95
        d.text((x + 2, wy + 2), word, font=font(px * 0.45), fill=INK + (alpha,), anchor="ms")
        d.text((x, wy), word, font=font(px * 0.45), fill=color + (alpha,), anchor="ms")


VETERAN, BOSS = (570, 250), (380, 200)   # where the numbers start: the top of the veteran's and the boss's figures (crop coordinates)
HIT_AT, CRIT_AT = 0.10, 0.45             # an ordinary hit on the veteran, then a critical hit on the boss (seconds into the loop)
PANELS = [
    ("지금", "치명타도 보통 피해처럼: 빨강 -5 (큰 피해 기준 20%를 넘지 않으면 1배)", lambda l, a: number(l, BOSS, a, 1.05, "-5", RED, 1.05)),
    ("안 A (권장) 금빛 + \"치명타\"", "금빛 1.6배, 위에 작은 \"치명타\", 1.4초. 보통 피해는 빨강 그대로",
     lambda l, a: number(l, BOSS, a, 1.4, "-5!", GOLD, 1.6, word="치명타")),
    ("안 B 빨강 + !", "색은 그대로 1.6배. 큰 피해(빨강 1.4배)와 잘 갈리지 않는다", lambda l, a: number(l, BOSS, a, 1.4, "-5!", RED, 1.6)),
    ("안 C 금빛 숫자만", "금빛 1.6배, 글 없음. 처음 보는 사람에게 뜻이 덜 드러난다", lambda l, a: number(l, BOSS, a, 1.4, "-5", GOLD, 1.6)),
]


def panel(index, time):
    label, sub, crit = PANELS[index]
    im = BASE.copy().convert("RGBA")
    layer = Image.new("RGBA", im.size, (0, 0, 0, 0))
    number(layer, VETERAN, time - HIT_AT, 1.05, "-3", RED, 1.05)
    crit(layer, time - CRIT_AT)
    im = Image.alpha_composite(im, layer).convert("RGB")
    d = ImageDraw.Draw(im)
    d.rectangle([0, 0, 959, 539], outline=(90, 84, 70), width=3)
    w = d.textlength(label, font=font(26))
    d.rectangle([12, 12, 36 + w, 52], fill=(20, 18, 16))
    d.text((24, 32), label, font=font(26), fill=GOLD if "권장" in label else WHITE, anchor="lm")
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


# The still: the moment the critical number has just settled (0.2 s into it).
sheet(CRIT_AT + 0.2).save(HERE / "mock-crit-floats-v2.png")

# The motion: three loops of 2 s at speed, then one at half speed.
FPS, LOOP = 30, 2.0
with tempfile.TemporaryDirectory() as tmp:
    frame = 0
    for speed, loops in ((1.0, 3), (0.5, 1)):
        for _ in range(loops):
            for k in range(int(LOOP / speed * FPS)):
                sheet(k * speed / FPS, slow=speed < 1).save(f"{tmp}/f{frame:05d}.png")
                frame += 1
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-framerate", str(FPS), "-i", f"{tmp}/f%05d.png",
                    "-c:v", "libx264", "-pix_fmt", "yuv420p", "-crf", "20", str(HERE / "mock-crit-floats.mp4")], check=True)
print("wrote mock-crit-floats-v2.png, mock-crit-floats.mp4")
