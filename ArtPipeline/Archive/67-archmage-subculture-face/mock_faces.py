# -*- coding: utf-8 -*-
"""Round 67: the four face levels (20·40·60·80%) next to Round 66's new look 2 (0%: our pretty-face test standard) — the faces
large from the raw images, then every figure at the game's scale (whole canvases at one scale on one floor, the levels fitted at
Height 80 so that chin-to-floor matches the valkyrie's), the heads at that scale, and the mine's stage at the game's size.
Pure Pillow, no API call; the game's art is not changed.
  .venv/bin/python ArtPipeline/Archive/67-archmage-subculture-face/mock_faces.py
"""
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
JOB = ROOT / "Assets/@Art/Unit/Job"
PRET = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"
GOBLIN = ROOT / "Assets/@Art/Unit/Enemy/goblin_raider.png"
KNIGHT = JOB / "knight.png"
BACKGROUND = ROOT / "Assets/@Art/Background/Dungeon/abandoned_mine.png"
NEWLOOK2 = ROOT / "ArtPipeline/Archive/66-archmage-hourglass/newlook2/archmage_newlook2.raw.png"
LEVELS = (20, 40, 60, 80)
# The face on each raw image (read off a grid): (left, top, right, bottom).
FACE_RAW = {0: (440, 110, 620, 290), 20: (450, 120, 630, 300), 40: (430, 120, 610, 300),
            60: (430, 140, 610, 320), 80: (400, 130, 580, 310)}
PAPER = (28, 24, 22)
LIGHT = (236, 228, 212)
TEXT = (232, 224, 208)
NOTE = (176, 168, 150)
LINE = (120, 150, 190)
CANVAS = (672, 896)
FLOOR = 20
SIDE = 12
PLACE = 300
FIT_HEIGHT = 80
FONTS = {}


def F(px):
    if px not in FONTS:
        FONTS[px] = ImageFont.truetype(str(PRET), px)
    return FONTS[px]


def load(path):
    return Image.open(path).convert("RGBA")


def bbox(img):
    return img.getchannel("A").point(lambda a: 255 if a > 8 else 0).getbbox()


def raw(level):
    return load(HERE / f"face{level}/archmage_face{level}.raw.png")


def fitted(level):
    return load(HERE / f"face{level}/archmage_face{level}_h80.png")


def face_on_canvas(level):
    """The raw face box carried through fit_figure's arithmetic (Height 80, centred, on the floor line)."""
    b = bbox(raw(level))
    w, h = b[2] - b[0], b[3] - b[1]
    r = min(round(CANVAS[1] * FIT_HEIGHT / 100) / h, (CANVAS[0] - 2 * SIDE) / w)
    x0 = (CANVAS[0] - round(w * r)) // 2
    y0 = CANVAS[1] - FLOOR - round(h * r)
    fx0, fy0, fx1, fy1 = FACE_RAW[level]
    return (round(x0 + (fx0 - b[0]) * r), round(y0 + (fy0 - b[1]) * r),
            round(x0 + (fx1 - b[0]) * r), round(y0 + (fy1 - b[1]) * r))


def faces_large(title, items, size=430):
    pad = 24
    sheet = Image.new("RGB", (pad + len(items) * (size + pad), size + 110), PAPER)
    d = ImageDraw.Draw(sheet)
    d.text((pad, 16), title, font=F(24), fill=TEXT)
    x = pad
    for label, img, box in items:
        bg = Image.new("RGBA", img.size, LIGHT + (255,))
        bg.alpha_composite(img)
        sheet.paste(bg.crop(box).resize((size, size), Image.LANCZOS).convert("RGB"), (x, 90))
        d.text((x, 58), label, font=F(20), fill=NOTE)
        x += size + pad
    return sheet


def game_row(title, items, height=560):
    k = height / CANVAS[1]
    pad = 10
    w = int(CANVAS[0] * k * 0.8)
    sheet = Image.new("RGB", (pad + len(items) * (w + pad), height + 150), PAPER)
    d = ImageDraw.Draw(sheet)
    d.text((pad + 8, 16), title, font=F(22), fill=TEXT)
    top = 120
    d.line([(0, top + height - int(FLOOR * k)), (sheet.width, top + height - int(FLOOR * k))], fill=LINE, width=1)
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
    cols = 2
    sheet = Image.new("RGB", (pad + cols * (w + pad), 60 + ((len(strips) + 1) // 2) * (h + 40)), PAPER)
    d = ImageDraw.Draw(sheet)
    d.text((pad, 16), title, font=F(22), fill=TEXT)
    for i, (label, strip) in enumerate(strips):
        x = pad + (i % cols) * (w + pad)
        y = 60 + (i // cols) * (h + 40)
        d.text((x, y), label, font=F(19), fill=NOTE)
        sheet.paste(strip, (x, y + 28))
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
    valkyrie, archmage, knight, goblin = load(JOB / "valkyrie.png"), load(JOB / "archmage.png"), load(KNIGHT), load(GOBLIN)
    fits = {level: fitted(level) for level in LEVELS}
    party = lambda img: [(knight, 380), (valkyrie, 640), (img, 900), (goblin, 1340)]
    stack([faces_large("Round 67 — 얼굴: 중국·일본 서브컬처 여캐 얼굴을 20·40·60·80% (원본에서 크게)",
                       [("0% (Round 66 새 모습 2)", load(NEWLOOK2), FACE_RAW[0])]
                       + [(f"{level}%", raw(level), FACE_RAW[level]) for level in LEVELS]),
           game_row(f"게임 배율: 캔버스 전체를 같은 크기로, 같은 바닥에 (네 단계는 Height {FIT_HEIGHT}: 턱~바닥을 발키리와 같게)",
                    [("발키리", valkyrie), ("대마법사 지금", archmage)] + [(f"{level}%", fits[level]) for level in LEVELS]),
           heads("머리: 게임 배율 그대로 2배",
                 [("발키리", valkyrie, (240, 200, 440, 380)), ("대마법사 지금", archmage, (230, 80, 430, 260))]
                 + [(f"{level}%", fits[level], face_on_canvas(level)) for level in LEVELS]),
           stage_sheet("광산 무대, 게임 크기의 절반: 기사 / 발키리 / 대마법사 / 고블린 약탈자",
                       [("지금", party(archmage))] + [(f"{level}%", party(fits[level])) for level in LEVELS])],
          HERE / "mock-faces.png")


if __name__ == "__main__":
    main()
