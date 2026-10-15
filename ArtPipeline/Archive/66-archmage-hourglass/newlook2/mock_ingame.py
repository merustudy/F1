# -*- coding: utf-8 -*-
"""Round 66, new look 2 (the user, 2026-10-14: "키 비교는 인게임 기준으로 해야되서 안 맞는것 같아"): every figure at the game's
scale — each job's whole 672x896 canvas drawn at the same size and on the same floor, as the game places them (the fit scales a
figure to its roster Height, or less when it is wide) — the heads at that scale, and the mine's stage at the game's size.
Pure Pillow, no API call; the game's art is not changed.
  .venv/bin/python ArtPipeline/Archive/66-archmage-hourglass/newlook2/mock_ingame.py
"""
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]
JOB = ROOT / "Assets/@Art/Unit/Job"
PRET = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"
GOBLIN = ROOT / "Assets/@Art/Unit/Enemy/goblin_raider.png"
BACKGROUND = ROOT / "Assets/@Art/Background/Dungeon/abandoned_mine.png"
NEW90 = HERE / "archmage_newlook2.png"
NEW82 = HERE / "archmage_newlook2_h82.png"
PAPER = (28, 24, 22)
TEXT = (232, 224, 208)
NOTE = (176, 168, 150)
LINE = (120, 150, 190)
CANVAS = (672, 896)
PLACE = 300  # the figure place on screen at 1080p; the 896-high canvas is drawn this tall
FONTS = {}


def F(px):
    if px not in FONTS:
        FONTS[px] = ImageFont.truetype(str(PRET), px)
    return FONTS[px]


def load(path):
    return Image.open(path).convert("RGBA")


def bbox(img):
    return img.getchannel("A").point(lambda a: 255 if a > 8 else 0).getbbox()


def game_row(title, items, height=600):
    """Whole canvases at one scale on one floor: the sizes the game shows, relative to each other."""
    k = height / CANVAS[1]
    pad = 10
    w = int(CANVAS[0] * k * 0.78)
    sheet = Image.new("RGB", (pad + len(items) * (w + pad), height + 150), PAPER)
    d = ImageDraw.Draw(sheet)
    d.text((pad + 8, 16), title, font=F(22), fill=TEXT)
    top = 120
    d.line([(0, top + height - int(20 * k)), (sheet.width, top + height - int(20 * k))], fill=LINE, width=1)
    x = pad
    for label, img in items:
        canvas = img.resize((int(CANVAS[0] * k), height), Image.LANCZOS)
        box = bbox(img)
        cx = x + w // 2
        sheet.paste(canvas, (cx - canvas.width // 2, top), canvas)
        d.text((x + 4, 58), label, font=F(18), fill=TEXT)
        d.text((x + 4, 84), f"키 {box[3] - box[1]}px", font=F(16), fill=NOTE)
        x += w + pad
    return sheet


def heads(title, pairs, zoom=2.0):
    """The heads at the game's scale (the same canvas pixels), so their sizes compare as in the game."""
    pad = 30
    tiles = [(label, img.crop(box).resize((int((box[2] - box[0]) * zoom), int((box[3] - box[1]) * zoom)), Image.LANCZOS))
             for label, img, box in pairs]
    w = sum(t.width for _, t in tiles) + pad * (len(tiles) + 1)
    sheet = Image.new("RGB", (w, max(t.height for _, t in tiles) + 100), PAPER)
    d = ImageDraw.Draw(sheet)
    d.text((pad, 16), title, font=F(22), fill=TEXT)
    x = pad
    for label, tile in tiles:
        d.text((x, 54), label, font=F(18), fill=NOTE)
        sheet.paste(tile, (x, 86), tile)
        x += tile.width + pad
    return sheet


def stage(figures, scale=0.5):
    bg = load(BACKGROUND).convert("RGB")
    bg = bg.resize((1920, int(bg.height * 1920 / bg.width)), Image.LANCZOS)
    floor = int(bg.height * 0.57)
    strip = bg.crop((0, floor - 400, 1920, floor + 90))
    vignette = Image.new("L", strip.size, 255)
    vd = ImageDraw.Draw(vignette)
    for i in range(0, 120, 4):
        vd.rectangle([i, i, strip.width - i, strip.height - i], outline=150 + int(105 * i / 120), width=4)
    vignette = vignette.filter(ImageFilter.GaussianBlur(40))
    strip = ImageChops.multiply(strip, Image.merge("RGB", (vignette, vignette, vignette)))
    k = PLACE / CANVAS[1]
    for img, cx in figures:
        canvas = img.resize((int(img.width * k), PLACE), Image.LANCZOS)
        strip.paste(canvas, (cx - canvas.width // 2, 400 + 8 - canvas.height), canvas)
    return strip.resize((int(strip.width * scale), int(strip.height * scale)), Image.LANCZOS)


def stage_sheet(title, rows):
    pad = 18
    strips = [(label, stage(figs)) for label, figs in rows]
    w, h = strips[0][1].size
    sheet = Image.new("RGB", (w + pad * 2, 60 + len(strips) * (h + 40)), PAPER)
    d = ImageDraw.Draw(sheet)
    d.text((pad, 16), title, font=F(22), fill=TEXT)
    y = 60
    for label, strip in strips:
        d.text((pad, y), label, font=F(19), fill=NOTE)
        sheet.paste(strip, (pad, y + 28))
        y += h + 40
    return sheet


def stack(parts, out):
    width = max(p.width for p in parts)
    sheet = Image.new("RGB", (width, sum(p.height for p in parts)), PAPER)
    y = 0
    for part in parts:
        sheet.paste(part, (0, y))
        y += part.height
    sheet.save(out)
    print(out, sheet.size)


def main():
    jobs = {key: load(JOB / f"{key}.png") for key in ("knight", "valkyrie", "paladin", "bishop", "spellblade", "archmage")}
    new90, new82 = load(NEW90), load(NEW82)
    goblin = load(GOBLIN)
    party = lambda img: [(jobs["knight"], 380), (jobs["valkyrie"], 640), (img, 900), (goblin, 1340)]
    stack([game_row("게임 배율: 캔버스 전체를 같은 크기로, 같은 바닥에 (키 = 게임에서 서는 높이, 캔버스 896 기준)",
                    [("기사", jobs["knight"]), ("발키리", jobs["valkyrie"]), ("성기사", jobs["paladin"]),
                     ("주교", jobs["bishop"]), ("마검사", jobs["spellblade"]), ("대마법사 지금", jobs["archmage"]),
                     ("새 모습 2 (Height 90)", new90), ("새 모습 2 (Height 82)", new82)]),
           heads("머리: 게임 배율 그대로 2배 (같은 캔버스 픽셀)",
                 [("발키리", jobs["valkyrie"], (240, 200, 440, 380)),
                  ("대마법사 지금", jobs["archmage"], (230, 80, 430, 260)),
                  ("새 모습 2 (90)", new90, (280, 130, 480, 310)),
                  ("새 모습 2 (82)", new82, (290, 200, 490, 380))]),
           stage_sheet("광산 무대, 게임 크기의 절반: 기사 / 발키리 / 대마법사 / 고블린 약탈자",
                       [("지금", party(jobs["archmage"])), ("새 모습 2 (Height 90)", party(new90)),
                        ("새 모습 2 (Height 82: 키를 발키리에)", party(new82))])],
          HERE / "mock-ingame.png")


if __name__ == "__main__":
    main()
