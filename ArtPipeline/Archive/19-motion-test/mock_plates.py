"""Should a unit's plate (its row, name, HP, states) move with it? (round 19, second review), no API call.
  .venv/bin/python ArtPipeline/Archive/19-motion-test/mock_plates.py      (after fit_motion.py)

The game moves the plate with the figure for every motion (BattleUnitView.Update: the lunge, the recoil and the walk
into a new column all move both). The user asked whether the plate should rather stay where it is. Here the same loop
as mock_motion.py is drawn twice, stacked: as the game has it now (the plates move) and with every plate kept in its
column while its figure lunges or recoils (a walk into a new column still carries the plate: the unit has moved).
Out: mock-plates.mp4 (three loops, then one four times slower) and mock-plates.png (the two moments side by side).
"""
from PIL import Image, ImageDraw, ImageFont

from mock_motion import (ASTRID, ATTACK_AT, CROP, ENEMY_1, FONT, FPS, HERE, HIT_AT, LOOP, LUNGE_OUT, PLATE_H, PLATE_TOP,
                         SLOW, Scene, state, write_mp4)

LABELS = ("지금 — 정보칸도 그림과 함께 움직인다", "제안 — 정보칸은 제자리, 그림(과 그림자)만 움직인다")


def captioned(img, text, size=26):
    out = Image.new("RGB", (img.width, img.height + 46), (24, 22, 22))
    out.paste(img, (0, 46))
    ImageDraw.Draw(out).text((14, 8), text, fill=(235, 235, 230), font=ImageFont.truetype(str(FONT), size))
    return out


def marked(img):
    """Ticks under the plates at the middle of their columns, so that a plate off its column shows."""
    out = img.copy()
    d = ImageDraw.Draw(out)
    for x in (ASTRID, ENEMY_1):
        x -= CROP[0]
        y = PLATE_TOP + PLATE_H + 6 - CROP[1]
        d.polygon([(x, y), (x - 9, y + 12), (x + 9, y + 12)], fill=(217, 164, 65))
    return out


def stacked(scene, t, speed=None):
    now = marked(scene.frame(CROP, **state(t)))
    fixed = marked(scene.frame(CROP, plates_follow=False, **state(t)))
    out = Image.new("RGB", (now.width, 2 * (now.height + 46) + 8), (12, 12, 12))
    out.paste(captioned(now, LABELS[0]), (0, 0))
    out.paste(captioned(fixed, LABELS[1]), (0, now.height + 46 + 8))
    if speed:
        d = ImageDraw.Draw(out)
        f = ImageFont.truetype(str(FONT), 26)
        w = d.textlength(speed, font=f)
        d.rectangle([out.width - w - 34, 6, out.width - 12, 40], fill=(0, 0, 0))
        d.text((out.width - w - 24, 8), speed, fill=(217, 164, 65), font=f)
    return out


def main():
    scene = Scene()
    moments = [(ATTACK_AT + LUNGE_OUT, "공격하는 순간 (돌진의 끝)"), (HIT_AT + 0.05, "맞는 순간 (밀림의 처음)")]
    rows = []
    for t, name in moments:
        now = captioned(marked(scene.frame(CROP, **state(t))), f"{name} · {LABELS[0]}", 22)
        fixed = captioned(marked(scene.frame(CROP, plates_follow=False, **state(t))), f"{name} · {LABELS[1]}", 22)
        row = Image.new("RGB", (now.width * 2 + 12, now.height), (12, 12, 12))
        row.paste(now, (0, 0))
        row.paste(fixed, (now.width + 12, 0))
        rows.append(row)
    sheet = Image.new("RGB", (rows[0].width, sum(r.height for r in rows) + 12), (12, 12, 12))
    sheet.paste(rows[0], (0, 0))
    sheet.paste(rows[1], (0, rows[0].height + 12))
    sheet.save(HERE / "mock-plates.png")
    print("목업: mock-plates.png (금색 표시는 열의 가운데)")

    normal = [stacked(scene, i / FPS, "1x") for i in range(round(LOOP * FPS))]
    slow = [stacked(scene, i / (FPS * SLOW), f"느리게 x{1 / SLOW:g}") for i in range(round(LOOP * FPS * SLOW))]
    write_mp4(normal * 3 + slow, "mock-plates.mp4", ": 위 지금 / 아래 제안, 보통 속도 3회 + 4배 느리게 1회")


if __name__ == "__main__":
    main()
