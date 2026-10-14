# -*- coding: utf-8 -*-
"""Round 64 (the user, 2026-10-14: "현재 우리 그림체 기반에서 발키리에서 더 섹시한 체형으로 테스트 3장 뽑아주고, 그림체 섞음
(실사랑 더 가깝게, 만화 체인소맨 스타일 강하게)으로도 테스트 이미지 만들어줘"): the game's valkyrie next to the three body tests and
the manga blend — large, and on the mine's stage at the game's size next to the knight and the goblin raider.
On the stage each figure's whole 672x896 canvas is drawn at the game's figure place (300px high at 1080p), so a figure the fit had to
shrink (a wide stance) stands as small as it would in the game. Pure Pillow, no API call; the game's art is not changed.
  .venv/bin/python ArtPipeline/Archive/64-valkyrie-body-manga/mock_compare.py
"""
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
PRET = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"
NOW = ROOT / "Assets/@Art/Unit/Job/valkyrie.png"
KNIGHT = ROOT / "Assets/@Art/Unit/Job/knight.png"
GOBLIN = ROOT / "Assets/@Art/Unit/Enemy/goblin_raider.png"
BACKGROUND = ROOT / "Assets/@Art/Background/Dungeon/abandoned_mine.png"
BODY = [("A 모래시계 글래머", HERE / "body/valkyrie_body_a.png"),
        ("B 탄탄한 운동선수형", HERE / "body/valkyrie_body_b.png"),
        ("C 긴 다리 핀업", HERE / "body/valkyrie_body_c.png")]
MANGA = ("실사 + 만화풍", HERE / "manga/valkyrie_manga.png")
PAPER = (28, 24, 22)
TEXT = (232, 224, 208)
NOTE = (176, 168, 150)
PLACE = 300  # the figure place on screen at 1080p; the 896-high canvas is drawn this tall
CANVAS_H = 896
# The head and shoulders on the 672x896 canvas (read off the images).
HEAD_NOW = (220, 230, 470, 460)
HEAD_MANGA = (230, 50, 470, 280)
FONTS = {}


def F(px):
    if px not in FONTS:
        FONTS[px] = ImageFont.truetype(str(PRET), px)
    return FONTS[px]


def load(path):
    return Image.open(path).convert("RGBA")


def crop_figure(img):
    return img.crop(img.getchannel("A").point(lambda a: 255 if a > 8 else 0).getbbox())


def large_row(title, items, height=640):
    """Every figure cropped and drawn at the same height: the drawing, not the in-game size."""
    pad = 36
    figs = []
    for label, img in items:
        fig = crop_figure(img)
        figs.append((label, fig.resize((int(fig.width * height / fig.height), height), Image.LANCZOS)))
    w = sum(f.width for _, f in figs) + pad * (len(figs) + 1)
    sheet = Image.new("RGB", (w, height + 140), PAPER)
    d = ImageDraw.Draw(sheet)
    d.text((pad, 20), title, font=F(24), fill=TEXT)
    x = pad
    for label, fig in figs:
        d.text((x, 70), label, font=F(21), fill=NOTE)
        sheet.paste(fig, (x, 112), fig)
        x += fig.width + pad
    return sheet


def stage(valkyrie, knight, goblin, scale=0.5):
    """The mine's stage at the game's size, darker at the edges like the candle vignette."""
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
    k = PLACE / CANVAS_H
    for img, cx in ((knight, 520), (valkyrie, 790), (goblin, 1260)):
        canvas = img.resize((int(img.width * k), PLACE), Image.LANCZOS)
        strip.paste(canvas, (cx - canvas.width // 2, 400 + 8 - canvas.height), canvas)
    return strip.resize((int(strip.width * scale), int(strip.height * scale)), Image.LANCZOS)


def stage_sheet(title, rows, knight, goblin):
    strips = [(label, stage(img, knight, goblin)) for label, img in rows]
    pad = 18
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


def heads(pairs, zoom=2.0):
    pad = 36
    tiles = [(label, img.crop(box).resize((int((box[2] - box[0]) * zoom), int((box[3] - box[1]) * zoom)), Image.LANCZOS))
             for label, img, box in pairs]
    w = sum(t.width for _, t in tiles) + pad * (len(tiles) + 1)
    sheet = Image.new("RGB", (w, tiles[0][1].height + 100), PAPER)
    d = ImageDraw.Draw(sheet)
    d.text((pad, 18), "머리 2배: 선, 눈, 명암", font=F(22), fill=TEXT)
    x = pad
    for label, tile in tiles:
        d.text((x, 54), label, font=F(19), fill=NOTE)
        sheet.paste(tile, (x, 86), tile)
        x += tile.width + pad
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
    now = load(NOW)
    knight, goblin = load(KNIGHT), load(GOBLIN)
    body = [(label, load(path)) for label, path in BODY]
    manga = (MANGA[0], load(MANGA[1]))

    stack([large_row("Round 64 시험 1 — 지금 그림체, 더 섹시한 체형 셋 (같은 높이로 그린 모습)", [("지금 (게임)", now)] + body),
           stage_sheet("광산 무대, 게임 크기의 절반: 기사 / 발키리 / 고블린 약탈자 (넓은 자세는 게임에서처럼 작게 선다)",
                       [("지금", now)] + body, knight, goblin)],
          HERE / "mock-body.png")

    stack([large_row("Round 64 시험 2 — 그림체 섞음: 실사 쪽 + 체인소맨 만화풍 (같은 높이로 그린 모습)", [("지금 (게임)", now), manga]),
           heads([("지금", now, HEAD_NOW), (manga[0], manga[1], HEAD_MANGA)]),
           stage_sheet("광산 무대, 게임 크기의 절반: 섞은 발키리만 바꿈 (기사·고블린은 지금 그대로)", [("지금", now), manga], knight, goblin)],
          HERE / "mock-manga.png")


if __name__ == "__main__":
    main()
