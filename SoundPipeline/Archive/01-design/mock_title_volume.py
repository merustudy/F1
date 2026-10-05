"""Mockup: where the music and effect volume buttons go on the title screen (stage 10, no calls).

    .venv/bin/python SoundPipeline/Archive/01-design/mock_title_volume.py SoundPipeline/Archive/01-design/title_volume_mockup.png

Run from the repository root (the font path is relative to it).

Draws the title screen from the builder's numbers (UiPrefabSetup.Title: 1920x1080 design space,
column x 760, y 460, buttons 400x84, gap 20) with Continue shown (a run exists: the longest column),
then three ways to add the two volume buttons. Each button cycles on -> low -> off.
"""
import sys
from PIL import Image, ImageDraw, ImageFont

FONT = "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"
BG, BLUE, QUIET, TEXT, DIM = (22, 22, 28), (59, 112, 181), (59, 64, 79), (235, 235, 230), (150, 150, 150)
W, H = 1920, 1080


def font(size):
    return ImageFont.truetype(FONT, size)


def button(d, x, y, w, h, label, fill, size):
    d.rectangle([x, y, x + w, y + h], fill=fill)
    f = font(size)
    box = d.textbbox((0, 0), label, font=f)
    d.text((x + (w - (box[2] - box[0])) / 2 - box[0], y + (h - (box[3] - box[1])) / 2 - box[1]), label, font=f, fill=TEXT)


def column(d, x, y, w, h, gap, items):
    for label, fill, size in items:
        button(d, x, y, w, h, label, fill, size)
        y += h + gap


def screen(draw_buttons):
    im = Image.new("RGB", (W, H), BG)
    d = ImageDraw.Draw(im)
    f = font(140)
    box = d.textbbox((0, 0), "F1", font=f)
    d.text(((W - (box[2] - box[0])) / 2 - box[0], 180 + (180 - (box[3] - box[1])) / 2 - box[1]), "F1", font=f, fill=TEXT)
    draw_buttons(d)
    return im


NEW, CONT, LANG, QUIT = ("새 런", BLUE, 36), ("이어하기", BLUE, 36), ("언어: 한국어", QUIET, 32), ("종료", QUIET, 32)
MUSIC, SFX = ("음악: 켬", QUIET, 32), ("효과음: 켬", QUIET, 32)


def now(d):
    column(d, 760, 460, 400, 84, 20, [NEW, CONT, LANG, QUIT])


def plan1(d):
    # Two more in the column: six buttons only fit when they are lower (72) and closer (14).
    column(d, 760, 400, 400, 72, 14, [NEW, CONT, LANG, MUSIC, SFX, QUIT])


def plan2(d):
    # The column keeps the run's buttons; the settings stand in one row under it.
    column(d, 760, 460, 400, 84, 20, [NEW, CONT, QUIT])
    for i, item in enumerate([("언어: 한국어", QUIET, 28), ("음악: 켬", QUIET, 28), ("효과음: 켬", QUIET, 28)]):
        button(d, 550 + i * 280, 800, 260, 64, *item)


def plan3(d):
    # The column as it is; two small buttons in the bottom right corner.
    column(d, 760, 460, 400, 84, 20, [NEW, CONT, LANG, QUIT])
    button(d, 1420, 990, 220, 60, "음악: 켬", QUIET, 26)
    button(d, 1660, 990, 220, 60, "효과음: 켬", QUIET, 26)


PANELS = [
    ("지금 (이어하기가 보일 때)", now),
    ("안 1 — 열에 두 개를 더함 (버튼을 72로 낮춤)", plan1),
    ("안 2 — 설정 한 줄: 언어·음악·효과음 (권장)", plan2),
    ("안 3 — 오른쪽 아래 구석에 작은 버튼 둘", plan3),
]

if __name__ == "__main__":
    out = sys.argv[1]
    pw, ph, head = 960, 540, 56
    sheet = Image.new("RGB", (pw * 2 + 30, (ph + head) * 2 + 70), (40, 40, 46))
    d = ImageDraw.Draw(sheet)
    for i, (title, draw) in enumerate(PANELS):
        x, y = (i % 2) * (pw + 30), (i // 2) * (ph + head)
        d.text((x + 8, y + 12), title, font=font(30), fill=TEXT)
        sheet.paste(screen(draw).resize((pw, ph), Image.LANCZOS), (x, y + head))
    d.text((8, (ph + head) * 2 + 18), "음악·효과음 버튼은 누를 때마다 켬 → 작게 → 끔 (언어 버튼과 같은 방식). 이어하기는 진행 중인 런이 있을 때만 보인다.", font=font(26), fill=DIM)
    sheet.save(out)
    print(out, sheet.size)
