"""The lobby's fatigue (round 33, Slice B stage 12): fatigue now builds up from 0 to 200, 100 is the breakdown check and 200
the collapse (Docs/Design/04_Lobby_100Day_Economy.md §3). How a roster line shows it, drawn over a lobby screenshot: the
words and the bar of every line are redrawn with example values, the rest is the game's. No API call.
  .venv/bin/python ArtPipeline/Archive/33-fatigue-display/mock_lobby_fatigue.py [lobby.png]
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[3]; HERE = Path(__file__).resolve().parent
FONT = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"
SHOT = Path(sys.argv[1]) if len(sys.argv) > 1 else HERE / "game/ko_03_lobby_party.png"

# The roster line (UiPrefabSetup.Lobby.BuildRosterEntry): 1112 x 116 from x 64, a line every 126 from y 192. In it the
# fatigue words (22, right-aligned in 410..610 x 12..46) and the bar (20..610 x 54..68, track Slot, fill Good).
ENTRY_X, ENTRY_TOPS = 64, (192, 318, 444, 570, 696, 822)
WORDS = (410, 12, 610, 46); BAR = (20, 54, 610, 68)
MAX, BREAK = 200, 100

PANEL_LIGHT = (0x2A, 0x30, 0x3C); TRACK = (0x15, 0x18, 0x1F); TEXT = (0xEB, 0xEB, 0xE6); GOOD = (0x58, 0xB3, 0x68)
FATIGUE = (0xCF, 0xBA, 0xF7)               # UiPalette.Fatigue (round 32): the words of the fatigue tags
VIOLET = (0x9E, 0x84, 0xDA)                # the fatigue tag's rim: the bar's fill below the breakdown
DANGER = (0xC8, 0x4B, 0x6E)                # past the breakdown: a red that keeps some of the violet
NOTCH = (0xE8, 0xDE, 0xC8)                 # bone: the mark at 100

# The roster in the screenshot's order, with fatigue after an expedition of four and a day of rest.
VALUES = (0, 12, 54, 86, 61, 128)          # 아스트리드, 세드릭, 엘라, 카이, 미라, 로언


def font(size): return ImageFont.truetype(str(FONT), size)


def entry_box(top, box):
    return (ENTRY_X + box[0], top + box[1], ENTRY_X + box[2], top + box[3])


def words(d, top, value, color):
    x0, y0, x1, y1 = entry_box(top, WORDS)
    d.rectangle([x0, y0, x1, y1], fill=PANEL_LIGHT)
    s = f"피로도 {value}/{MAX}"; f = font(22)
    l, t, r, b = d.textbbox((0, 0), s, font=f)
    d.text((x1 - r, y0 + (y1 - y0 - (b - t)) / 2 - t), s, font=f, fill=color)


def bar(d, top, value, variant):
    x0, y0, x1, y1 = entry_box(top, BAR); w = x1 - x0
    d.rectangle([x0, y0, x1 - 1, y1 - 1], fill=TRACK)
    fill_to = x0 + round(w * value / MAX); mid = x0 + round(w * BREAK / MAX)
    if variant == "now":
        if value: d.rectangle([x0, y0, fill_to - 1, y1 - 1], fill=GOOD)
        return
    if variant == "C":
        # Ten pips of 20, a gap of 4 between them; the five past 100 are the danger's.
        pips, gap = MAX // 20, 4; pw = (w - gap * (pips - 1)) / pips
        d.rectangle([x0, y0, x1 - 1, y1 - 1], fill=PANEL_LIGHT)
        for i in range(pips):
            px0 = x0 + round(i * (pw + gap)); px1 = x0 + round(i * (pw + gap) + pw)
            full = min(1.0, max(0.0, (value - i * 20) / 20))
            d.rectangle([px0, y0, px1 - 1, y1 - 1], fill=TRACK)
            if full > 0:
                d.rectangle([px0, y0, px0 + round((px1 - px0) * full) - 1, y1 - 1], fill=VIOLET if i < pips // 2 else DANGER)
        return
    below = min(fill_to, mid)
    if value: d.rectangle([x0, y0, below - 1, y1 - 1], fill=VIOLET)
    if fill_to > mid:
        d.rectangle([mid, y0, fill_to - 1, y1 - 1], fill=DANGER if variant == "B" else VIOLET)
    d.rectangle([mid - 1, y0 - 3, mid, y1 + 2], fill=NOTCH)


def render(src, variant):
    img = src.copy(); d = ImageDraw.Draw(img)
    for top, value in zip(ENTRY_TOPS, VALUES):
        color = TEXT if variant in ("now", "A") else (FATIGUE if value < BREAK else DANGER)
        words(d, top, value, color)
        bar(d, top, value, variant)
    return img


LABELS = {
    "now": "지금(12단계 구현): 쌓인 값, 녹색 막대",
    "A": "A: 연보라 막대와 100의 눈금",
    "B": "B: A + 100을 넘은 부분과 글이 붉게 (권장)",
    "C": "C: 다키스트 던전처럼 칸 열 개(20씩), 뒤의 다섯이 붉게",
}


def sheet(entries, cols, path, lab=44, gap=24, size=26, footer=None):
    w = max(im.width for _, im in entries); h = max(im.height for _, im in entries); rows = (len(entries) + cols - 1) // cols
    fh = 50 if footer else 0
    out = Image.new("RGB", (cols * (w + gap) + gap, rows * (h + lab + gap) + gap + fh), (18, 18, 18)); d = ImageDraw.Draw(out)
    for i, (name, im) in enumerate(entries):
        x = gap + (i % cols) * (w + gap); y = gap + (i // cols) * (h + lab + gap)
        d.text((x, y), name, font=font(size), fill=(235, 235, 235)); out.paste(im.convert("RGB"), (x, y + lab))
    if footer: d.text((gap, out.height - fh + 10), footer, font=font(22), fill=(190, 190, 190))
    out.save(path); print(path.name, out.size)


def main():
    src = Image.open(SHOT).convert("RGB"); fulls = {}
    for v in LABELS:
        fulls[v] = render(src, v); fulls[v].save(HERE / f"mock-{v}.png"); print(f"mock-{v}.png")
    roster = (40, 120, 1200, 950)
    sheet([(LABELS[v], fulls[v].crop(roster)) for v in LABELS], 2, HERE / "mock-compare.png",
          footer="피로도 예: 아스트리드 0, 세드릭 12, 엘라 54, 카이 86, 미라 61, 로언 128. 100에서 붕괴 판정, 200에서 쓰러짐(15단계).")
    detail = (ENTRY_X, ENTRY_TOPS[5], ENTRY_X + 640, ENTRY_TOPS[5] + 116)
    sheet([(LABELS[v].split(":")[0], fulls[v].crop(detail).resize((2 * (detail[2] - detail[0]), 2 * (detail[3] - detail[1])), Image.LANCZOS)) for v in LABELS],
          2, HERE / "mock-detail.png", lab=40, gap=16, size=24, footer="로언(128)의 줄 왼쪽 절반을 2배로.")


if __name__ == "__main__": main()
