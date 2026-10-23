"""Round 103 mockups: the captain's handwriting and the lobby's countdown. No calls.

Over the 55단계 screenshots (20261023-d55b) the subtitles and the header's words are wiped and drawn again:
- font: the intro's #3 (the captain's words and the king's quoted words in one line) in the current Pretendard and two handwriting
  candidates. Only the captain's words change; the quoted words stay in print (Design/33 §9).
- countdown: the header "N주차 / 99주" against "일식까지 N주" and the lines that would follow it.

    .venv/bin/python ArtPipeline/Archive/103-handwriting-countdown/mock_round103.py

The handwriting candidates are read from the copy macOS ships (NanumScript.ttc: Nanum Brush Script, Nanum Pen Script; SIL OFL 1.1).
The game would ship the same fonts from their published source with the licence (Architecture/06 "Font").
"""
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
SHOTS = Path.home() / "Library" / "Caches" / "F1" / "screenshots" / "20261023-d55b"
PRETENDARD = ROOT / "Assets" / "@Fonts" / "Source" / "Pretendard" / "Pretendard-Medium.ttf"
NANUM = Path("/System/Library/AssetsV2/PreinstalledAssetsV2/InstallWithOs/com_apple_MobileAsset_Font7/"
             "2a4953d2780c8873b8c4b67605322d4c33c2efe1.asset/AssetData/NanumScript.ttc")

WHITE = (238, 232, 220)   # UiPalette.StoryWhite
GOLD = (214, 190, 130)    # UiPalette.StoryGold
TEXT = (236, 236, 240)
FINAL = (222, 184, 90)
LINE_SIZE = 45            # IntroScreen.LineSize
LINE_STEP = 63            # LineSize + IntroScreen.LineGap
LINES_BOTTOM = 69         # IntroScreen.LinesBottom

FONTS = {
    "now": ("지금: Pretendard (색으로만 가름)", lambda size: ImageFont.truetype(str(PRETENDARD), size), 1.0),
    "pen": ("A: 나눔손글씨 펜 (권장)", lambda size: ImageFont.truetype(str(NANUM), size, index=1), 1.38),
    "brush": ("B: 나눔손글씨 붓", lambda size: ImageFont.truetype(str(NANUM), size, index=0), 1.30),
}


def print_font(size):
    return ImageFont.truetype(str(PRETENDARD), size)


def wipe(img, box):
    """Takes the words out of a box: the bright strokes over the dark band are opened away (min then max), only where the picture
    was brighter than that, with a soft edge, so the rest of the picture stays as it was."""
    region = img.crop(box)
    opened = region.filter(ImageFilter.MinFilter(15)).filter(ImageFilter.MaxFilter(15)).filter(ImageFilter.GaussianBlur(3))
    diff = ImageChops.subtract(region.convert("L"), opened.convert("L"))
    mask = diff.point(lambda v: 255 if v > 18 else 0).filter(ImageFilter.MaxFilter(9)).filter(ImageFilter.GaussianBlur(3))
    region.paste(opened, (0, 0), mask)
    img.paste(region, box[:2])


def segments(line):
    """(text, quoted) runs of a line: the words in “…” are another's voice."""
    runs, buf, quoted = [], "", False
    for c in line:
        if c == "“" and not quoted:
            if buf:
                runs.append((buf, False))
            buf, quoted = c, True
        elif c == "”" and quoted:
            runs.append((buf + c, True))
            buf, quoted = "", False
        else:
            buf += c
    if buf:
        runs.append((buf, quoted))
    return runs


def draw_line(draw, y, line, hand):
    """One centered subtitle line: the captain's runs in the hand, the quoted runs in print and gold, a shadow under both."""
    _, make, scale = FONTS[hand]
    hand_font = make(int(LINE_SIZE * scale))
    quote_font = print_font(LINE_SIZE)
    runs = [(t, q, quote_font if q else hand_font) for t, q in segments(line)]
    width = sum(draw.textlength(t, font=f) for t, _, f in runs)
    x = (1920 - width) / 2
    for t, q, f in runs:
        # Both faces sit on one baseline.
        base = y + quote_font.getmetrics()[0]
        top = base - f.getmetrics()[0]
        draw.text((x + 3, top + 3), t, font=f, fill=(0, 0, 0, 200))
        draw.text((x, top), t, font=f, fill=(GOLD if q else WHITE) + (255,))
        x += draw.textlength(t, font=f)


def intro_frame(shot, lines, hand):
    img = Image.open(SHOTS / shot).convert("RGB")
    top = 1080 - LINES_BOTTOM - (len(lines) * LINE_SIZE + (len(lines) - 1) * (LINE_STEP - LINE_SIZE))
    wipe(img, (300, top - 16, 1620, 1080 - LINES_BOTTOM + 14))
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    for i, line in enumerate(lines):
        draw_line(d, top + i * LINE_STEP, line, hand)
    return Image.alpha_composite(img.convert("RGBA"), layer)


def header(shot, text, color, size=40, sub=None):
    """The header bar with its middle words replaced (the bar is one flat colour)."""
    img = Image.open(SHOTS / shot).convert("RGBA")
    bar = img.getpixel((700, 12))
    d = ImageDraw.Draw(img)
    d.rectangle((560, 0, 1360, 98), fill=bar)
    f = print_font(size)
    if sub is None:
        w = d.textlength(text, font=f)
        d.text(((1920 - w) / 2, 50 - size * 0.62), text, font=f, fill=color)
    else:
        w = d.textlength(text, font=f)
        d.text(((1920 - w) / 2, 10), text, font=f, fill=color)
        s = print_font(22)
        w = d.textlength(sub, font=s)
        d.text(((1920 - w) / 2, 62), sub, font=s, fill=(150, 150, 160))
    return img


def replace_text(img, box, text, size, color, fill=None, align="left"):
    d = ImageDraw.Draw(img)
    d.rectangle(box, fill=fill or img.getpixel((box[0] + 1, box[1] + 1)))
    f = print_font(size)
    x = box[0] + 4 if align == "left" else (box[0] + box[2] - d.textlength(text, font=f)) / 2
    d.text((x, (box[1] + box[3]) / 2 - size * 0.62), text, font=f, fill=color)
    return img


def label(img, text, sub=None, y=0):
    """The option's name in a red tag; y = 110 keeps it under a header bar."""
    img = img.convert("RGBA")
    d = ImageDraw.Draw(img)
    f = print_font(40)
    w = d.textlength(text, font=f)
    h = 64 if sub is None else 104
    right = w + 60 if sub is None else max(w, d.textlength(sub, font=print_font(28))) + 60
    d.rectangle((0, y, right, y + h), fill=(170, 40, 40, 235))
    d.text((30, y + 10), text, font=f, fill=(255, 255, 255))
    if sub:
        d.text((30, y + 62), sub, font=print_font(28), fill=(255, 230, 230))
    return img


def sheet(images, cols, title, out, scale=0.5):
    w, h = int(1920 * scale), int(1080 * scale)
    pad, head = 24, 80
    rows = (len(images) + cols - 1) // cols
    canvas = Image.new("RGB", (cols * w + (cols + 1) * pad, head + rows * (h + pad) + pad), (24, 24, 28))
    d = ImageDraw.Draw(canvas)
    d.text((pad, 22), title, font=print_font(34), fill=(240, 240, 240))
    for i, img in enumerate(images):
        r, c = divmod(i, cols)
        canvas.paste(img.convert("RGB").resize((w, h), Image.LANCZOS), (pad + c * (w + pad), head + r * (h + pad)))
    canvas.save(out)


THRONE = ["왕이 우리를 불렀다. “구덩이를 쳐라. 내 병사들을 붙여 주마.”",
          "그날 왕의 눈이 누구를 보고 있었는지, 나는 몰랐다."]
THRONE_EN = ["The king summoned us. “Strike the pit. My soldiers will march with you.”",
             "I never saw whom his eyes were on that day."]


def main():
    # 1. The captain's handwriting.
    fonts = []
    for hand in ("now", "pen", "brush"):
        img = label(intro_frame("ko_95_intro_throne.png", THRONE, hand), FONTS[hand][0])
        img.save(HERE / f"mock-font-{hand}.png")
        fonts.append(img)
        en = label(intro_frame("ko_95_intro_throne.png", THRONE_EN, hand), FONTS[hand][0] + " · 영어")
        en.save(HERE / f"mock-font-{hand}-en.png")
        fonts.append(en)
    sheet(fonts, 2, "Round 103-1 · 단장의 혼잣말 서체 (#3 왕좌의 방: 흰 글 = 단장, 금빛 따옴표 = 왕의 말, 인쇄체 그대로)",
          HERE / "mock-font-sheet.png")

    # 2. The countdown.
    now = label(Image.open(SHOTS / "ko_03_lobby_party.png"), "지금: 지난 주를 센다", y=110)
    a = label(header("ko_03_lobby_party.png", "일식까지 99주", TEXT), "A: 남은 주만 (권장)", y=110)
    b = label(header("ko_03_lobby_party.png", "일식까지 99주 · 1주차", TEXT), "B: 남은 주 · 지난 주", y=110)
    c = label(header("ko_03_lobby_party.png", "일식까지 99주", TEXT, size=36, sub="1주차 / 99주"), "C: 두 줄", y=110)
    for name, img in (("now", now), ("a", a), ("b", b), ("c", c)):
        img.save(HERE / f"mock-countdown-{name}.png")
    sheet([now, a, b, c], 2, "Round 103-2 · 로비 머리줄 (첫 주)", HERE / "mock-countdown-sheet.png")

    # 3. What A changes with it: the final stand, the expedition's header and its return line.
    final_now = label(Image.open(SHOTS / "ko_60_lobby_final_stand.png"), "지금: 99주가 지난 뒤", y=110)
    final_a = label(header("ko_60_lobby_final_stand.png", "일식의 날 — 최종 결전", FINAL), "A: 일식의 날", y=110)
    exp_now = label(Image.open(SHOTS / "ko_66_expedition_party.png"), "지금: 원정 화면", y=110)
    exp_a = header("ko_66_expedition_party.png", "일식까지 99주", TEXT, size=44)
    exp_a = replace_text(exp_a, (205, 132, 700, 172), "버려진 광산 · 1주 (→ 일식까지 98주)", 22, (236, 236, 240))
    exp_a = label(exp_a, "A: 원정 화면", "돌아오는 때도 남은 주로", y=110)
    for name, img in (("final-now", final_now), ("final-a", final_a), ("expedition-now", exp_now), ("expedition-a", exp_a)):
        img.save(HERE / f"mock-countdown-{name}.png")
    sheet([final_now, final_a, exp_now, exp_a], 2, "Round 103-3 · A를 고르면 함께 바뀌는 곳", HERE / "mock-countdown-follow-sheet.png")


if __name__ == "__main__":
    main()
