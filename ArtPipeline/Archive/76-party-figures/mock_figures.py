# -*- coding: utf-8 -*-
"""Round 76: the party's figures before departure (the user's 2026-10-17 request "파티 구성 이미지 개선 — 출발 전 파티 구성 캐릭터 등을
볼 수 있는 창 추가(전투 선택 시 처럼)"). Mockups over the expedition screen as it is (base-expedition.png = ko_56_expedition,
base-popup.png = ko_66_expedition_party of 2026-10-17 after Round 75): the jobs' figures (Assets/@Art/Unit/Job) placed as the node map's
party side places them. Drawn shapes and text; the figures are the shipped art. No API call.
  .venv/bin/python ArtPipeline/Archive/76-party-figures/mock_figures.py
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
FONT = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"
ART = ROOT / "Assets/@Art/Unit/Job"
TEXT = (0xEB, 0xEB, 0xE6); DIM = (0x9A, 0xA0, 0xAC); BRASS = (0xB8, 0x94, 0x4E); BONE = (0xCD, 0xC0, 0xA0)
PANEL = (0x1E, 0x22, 0x2B); PANEL_LIGHT = (0x2A, 0x30, 0x3C); SLOT = (0x15, 0x18, 0x1F); LINE = (0x55, 0x5C, 0x6B)
BUTTON = (0x3B, 0x6F, 0xB5); QUIET = (0x3A, 0x41, 0x50); SELECTED = (0x80, 0x66, 0x14); GOOD = (0x58, 0xB3, 0x68)
DISABLED = (0x2E, 0x33, 0x3E)

# The roster of the screenshot (day 1): name, job id, job name, level, row in the party (0 = out).
ROSTER = [("아스트리드", "valkyrie", "발키리", 1, 0), ("세드릭", "paladin", "성기사", 1, 0), ("엘라", "bishop", "주교", 1, 3),
          ("카이", "spellblade", "마검사", 1, 2), ("미라", "archmage", "대마법사", 1, 4), ("로언", "knight", "기사", 1, 1)]
BY_ROW = {m[4]: m for m in ROSTER if m[4]}
_figures = {}


def font(px): return ImageFont.truetype(str(FONT), int(round(px)))


def label(d, xy, s, px, color=TEXT, anchor="la"):
    d.text(xy, s, font=font(px), fill=color, anchor=anchor)


def figure(job, height):
    """A job's figure (672x896) scaled to a height, cached."""
    key = (job, height)
    if key not in _figures:
        im = Image.open(ART / (job + ".png")).convert("RGBA")
        w = int(round(im.width * height / im.height))
        _figures[key] = im.resize((w, height), Image.LANCZOS)
    return _figures[key]


def stand(img, cx, foot_y, job, height, shadow_w):
    """The figure standing on a floor line: the shadow under the feet, the feet on the line (the node map's figure place)."""
    d = ImageDraw.Draw(img)
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    ImageDraw.Draw(layer).ellipse((cx - shadow_w / 2, foot_y - 9, cx + shadow_w / 2, foot_y + 9), fill=(0, 0, 0, 90))
    img.alpha_composite(layer)
    fig = figure(job, height)
    img.alpha_composite(fig, (int(cx - fig.width / 2), int(foot_y - height)))


def shade(img, box):
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    ImageDraw.Draw(layer).rectangle(box, fill=(0, 0, 0, 171))
    return Image.alpha_composite(img, layer)


def button(d, box, s, color=QUIET, px=20, text=TEXT):
    d.rectangle(box, fill=color)
    label(d, ((box[0] + box[2]) / 2, (box[1] + box[3]) / 2), s, px, text, "mm")


def pips(d, x, y, w, h=12, n=10, gap=4):
    pw = (w - gap * (n - 1)) / n
    for i in range(n):
        d.rectangle((x + i * (pw + gap), y, x + i * (pw + gap) + pw, y + h), fill=SLOT)


def figure_slot(img, x, y, row, member):
    """A row of the party in the popup as a figure stand (안 A): the figure, then the row and the name, the job, the pips and the moves."""
    w, h = 320, 380
    d = ImageDraw.Draw(img)
    d.rectangle((x, y, x + w, y + h), fill=PANEL_LIGHT)
    if member is None:
        d.rectangle((x + 76, y + 10, x + 244, y + 234), fill=SLOT)
        label(d, (x + 160, y + 122), "비어 있음", 24, DIM, "mm")
        label(d, (x + 16, y + 246), "%d열" % row, 20, DIM)
        label(d, (x + 16, y + 282), "로스터에서 용병을 누르세요", 20, DIM)
        return
    name, job, job_name, lv, _ = member
    stand(img, x + w / 2, y + 234, job, 224, 120)
    d = ImageDraw.Draw(img)
    label(d, (x + 16, y + 246), "%d열" % row, 20, DIM)
    label(d, (x + 60, y + 243), name, 26, TEXT)
    label(d, (x + w - 16, y + 248), "Lv %d" % lv, 18, BONE, "ra")
    label(d, (x + 16, y + 282), job_name, 20, DIM)
    pips(d, x + 16, y + 314, w - 32)
    button(d, (x + 16, y + 332, x + 112, y + 372), "앞으로", QUIET if row > 1 else DISABLED, 20, TEXT if row > 1 else DIM)
    button(d, (x + 120, y + 332, x + 216, y + 372), "뒤로", QUIET if row < 4 else DISABLED, 20, TEXT if row < 4 else DIM)
    button(d, (x + 224, y + 332, x + 304, y + 372), "제외", QUIET, 20)


def chip(img, x, y, member, thumb):
    """A roster chip; with a thumbnail of the figure at the left (안 A)."""
    name, job, job_name, lv, row = member
    w, h = 320, 110
    d = ImageDraw.Draw(img)
    d.rectangle((x, y, x + w, y + h), fill=SELECTED if row else PANEL_LIGHT)
    tx = x + 12
    if thumb:
        d.rectangle((x + 10, y + 12, x + 70, y + 98), fill=SLOT)
        stand(img, x + 40, y + 94, job, 80, 44)
        d = ImageDraw.Draw(img)
        tx = x + 82
    label(d, (tx, y + 10), name, 22, TEXT)
    label(d, (x + w - 12, y + 14), "Lv %d" % lv, 16, BONE, "ra")
    label(d, (tx, y + 44), job_name, 16, DIM)
    if row:
        label(d, (x + w - 12, y + 44), "%d열" % row, 16, BRASS, "ra")
    pips(d, tx, y + 82, x + w - 12 - tx, 10)


def popup_figures(img):
    """안 A: the popup with figure stands in the node map's order (4열 at the left, 1열 at the right), the chips with thumbnails."""
    d = ImageDraw.Draw(img)
    x0, y0, x1, y1 = 260, 110, 1660, 1000
    d.rectangle((x0, y0, x1, y1), fill=PANEL, outline=BRASS, width=2)
    label(d, (x0 + 24, y0 + 22), "원정 파티", 30, DIM)
    button(d, (x1 - 180, y0 + 16, x1 - 24, y0 + 66), "닫기", QUIET, 22)
    label(d, (x0 + 24, y0 + 72), "버려진 광산 · 5일 (→ 6일차)", 22, TEXT)
    for i, row in enumerate((4, 3, 2, 1)):
        figure_slot(img, x0 + 24 + 340 * i, y0 + 104, row, BY_ROW.get(row))
    d = ImageDraw.Draw(img)
    label(d, (x0 + 24, y0 + 502), "로스터 6/8 — 눌러서 파티에 넣고 뺀다", 24, DIM)
    for i, m in enumerate(ROSTER):
        chip(img, x0 + 24 + 344 * (i % 4), y0 + 540 + 130 * (i // 4), m, thumb=True)
    d = ImageDraw.Draw(img)
    label(d, (x0 + 24, y1 - 56), "출발할 수 있습니다", 24, GOOD, "lm")
    button(d, (x1 - 24 - 294, y1 - 24 - 64, x1 - 24, y1 - 24), "출발", BUTTON, 30)
    return img


def strip_figures(img):
    """안 B: the expedition screen's strip shows the party as figure stands (the node map's order), the popup as it is."""
    d = ImageDraw.Draw(img)
    d.rectangle((40, 770, 1880, 1040), fill=(0x1E, 0x22, 0x2B, 230))
    label(d, (64, 782), "파티 — 출발을 누르면 편성한다", 24, DIM)
    for i, row in enumerate((4, 3, 2, 1)):
        x = 64 + 300 * i
        d.rectangle((x, 812, x + 280, 1030), fill=PANEL_LIGHT)
        m = BY_ROW.get(row)
        if m is None:
            label(d, (x + 140, 900), "비어 있음", 22, DIM, "mm")
            continue
        stand(img, x + 140, 962, m[1], 150, 90)
        d = ImageDraw.Draw(img)
        label(d, (x + 14, 966), "%d열" % row, 18, DIM)
        label(d, (x + 56, 964), m[0], 22, TEXT)
        label(d, (x + 266, 968), "Lv %d" % m[3], 16, BONE, "ra")
        label(d, (x + 14, 994), m[2], 18, DIM)
        pips(d, x + 14, 1016, 252, 8)
    label(d, (1706, 850), "출발할 수 있습니다", 22, GOOD, "mm")
    button(d, (1560, 880, 1854, 1000), "출발", BUTTON, 34)
    return img


def caption(img, text):
    d = ImageDraw.Draw(img)
    d.rectangle((0, 0, 1920, 56), fill=(0, 0, 0, 200))
    label(d, (24, 28), text, 32, TEXT, "lm")
    return img


def main():
    base = Image.open(HERE / "base-expedition.png").convert("RGBA")
    now = Image.open(HERE / "base-popup.png").convert("RGBA")
    a = popup_figures(shade(base.copy(), (0, 100, 1920, 1080)))
    b = strip_figures(base.copy())
    c = popup_figures(shade(strip_figures(base.copy()), (0, 100, 1920, 1080)))
    a.save(HERE / "mock-figures-A-popup.png"); b.save(HERE / "mock-figures-B-strip.png"); c.save(HERE / "mock-figures-C-both.png")
    tiles = [caption(now.copy(), "지금 (Round 75 B) — 팝업의 열 칸은 글뿐"),
             caption(a, "안 A — 팝업의 열 칸이 그림 자리(노드 맵처럼 4열 → 1열), 로스터 칩에 작은 그림"),
             caption(b, "안 B — 원정 화면의 띠가 그림 자리, 팝업은 그대로"),
             caption(c, "안 C — 둘 다 (띠와 팝업)")]
    sheet = Image.new("RGBA", (1920 + 20, 1080 + 20), (0x0C, 0x0C, 0x0E, 255))
    for i, t in enumerate(tiles):
        sheet.paste(t.resize((960, 540), Image.LANCZOS), (10 + 970 * (i % 2), 10 + 550 * (i // 2)))
    sheet.save(HERE / "mock-figures-compare.png")
    print("ok")


if __name__ == "__main__":
    main()
