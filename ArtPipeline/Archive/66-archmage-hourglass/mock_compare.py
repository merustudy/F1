# -*- coding: utf-8 -*-
"""Round 66 (the user, 2026-10-14: "대마법사 대기 이미지로 테스트 이미지 한번 더 생성해보자. 얼굴 더 미형으로 모래시계 몸매로 변형"):
the game's archmage next to the test — large, the heads at twice the size, and on the mine's stage at the game's size next to the
knight and the goblin raider. On the stage each figure's whole 672x896 canvas is drawn at the game's figure place (300px high at
1080p). Pure Pillow, no API call; the game's art is not changed.
  .venv/bin/python ArtPipeline/Archive/66-archmage-hourglass/mock_compare.py 1-2 195,70,425,300 215,65,445,295 205,65,435,295
  .venv/bin/python ArtPipeline/Archive/66-archmage-hourglass/mock_compare.py 2-3 195,70,425,300 205,65,435,295 205,45,435,275
  .venv/bin/python ArtPipeline/Archive/66-archmage-hourglass/mock_compare.py newlook 195,70,425,300 205,65,435,295 215,110,445,340
"""
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
PRET = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"
NOW = ROOT / "Assets/@Art/Unit/Job/archmage.png"
TEST = HERE / "archmage_hourglass.png"
TEST2 = HERE / "try2/archmage_hourglass2.png"
TEST2B = HERE / "try2/archmage_hourglass2b.png"
TEST3 = HERE / "try3/archmage_hourglass3.png"
KEPT = HERE / "kept/archmage_tall-hourglass-slim-sexy.png"
NEWLOOK = HERE / "newlook/archmage_newlook.png"
VALKYRIE = ROOT / "Assets/@Art/Unit/Job/valkyrie.png"
KNIGHT = ROOT / "Assets/@Art/Unit/Job/knight.png"
GOBLIN = ROOT / "Assets/@Art/Unit/Enemy/goblin_raider.png"
BACKGROUND = ROOT / "Assets/@Art/Background/Dungeon/abandoned_mine.png"
PAPER = (28, 24, 22)
TEXT = (232, 224, 208)
NOTE = (176, 168, 150)
PLACE = 300  # the figure place on screen at 1080p; the 896-high canvas is drawn this tall
CANVAS_H = 896
FONTS = {}


def F(px):
    if px not in FONTS:
        FONTS[px] = ImageFont.truetype(str(PRET), px)
    return FONTS[px]


def load(path):
    return Image.open(path).convert("RGBA")


def crop_figure(img):
    return img.crop(img.getchannel("A").point(lambda a: 255 if a > 8 else 0).getbbox())


def large_row(title, items, height=680):
    pad = 40
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


def heads(pairs, zoom=2.0):
    pad = 40
    tiles = [(label, img.crop(box).resize((int((box[2] - box[0]) * zoom), int((box[3] - box[1]) * zoom)), Image.LANCZOS))
             for label, img, box in pairs]
    w = sum(t.width for _, t in tiles) + pad * (len(tiles) + 1)
    sheet = Image.new("RGB", (w, tiles[0][1].height + 100), PAPER)
    d = ImageDraw.Draw(sheet)
    d.text((pad, 18), "머리 2배 (같은 캔버스 배율)", font=F(22), fill=TEXT)
    x = pad
    for label, tile in tiles:
        d.text((x, 54), label, font=F(19), fill=NOTE)
        sheet.paste(tile, (x, 86), tile)
        x += tile.width + pad
    return sheet


def stage(figures, scale=0.5):
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


def sheet_1_2(head_now, head_test, head_test2):
    """The 1st and 2nd tries next to the game's archmage (mock-archmage.png)."""
    now, test, test2 = load(NOW), load(TEST), load(TEST2)
    knight, valkyrie, goblin = load(KNIGHT), load(VALKYRIE), load(GOBLIN)
    party = lambda img: [(knight, 380), (valkyrie, 640), (img, 900), (goblin, 1340)]
    stack([large_row("Round 66 — 대마법사: 모래시계 체형 + 더 미형의 얼굴 (지금 그림체, 같은 높이로 그린 모습)",
                     [("지금 (게임)", now), ("1차: hourglass + 미형", test), ("2차: 더 세게 + 섹시한 옷차림", test2)]),
           heads([("지금", now, head_now), ("1차", test, head_test), ("2차", test2, head_test2)]),
           stage_sheet("광산 무대, 게임 크기의 절반: 기사 / 발키리(hourglass) / 대마법사 / 고블린 약탈자",
                       [("지금", party(now)), ("1차", party(test)), ("2차", party(test2))])],
          HERE / "mock-archmage.png")


def sheet_2_3(head_now, head_test2, head_test3):
    """The 2nd try without the hooked strand and the 3rd (the style sheet as the reference) (mock-archmage-3.png)."""
    now, test2b, test3 = load(NOW), load(TEST2B), load(TEST3)
    knight, valkyrie, goblin = load(KNIGHT), load(VALKYRIE), load(GOBLIN)
    party = lambda img: [(knight, 380), (valkyrie, 640), (img, 900), (goblin, 1340)]
    stack([large_row("Round 66 — 2차(어깨 위 가닥 지움)와 3차(기준 그림을 확정 시트로: 얼굴 c안)",
                     [("지금 (게임)", now), ("2차, 가닥 지움", test2b), ("3차: 확정 시트로 새로", test3)]),
           heads([("지금", now, head_now), ("2차", test2b, head_test2), ("3차", test3, head_test3)]),
           stage_sheet("광산 무대, 게임 크기의 절반: 기사 / 발키리(hourglass) / 대마법사 / 고블린 약탈자",
                       [("지금", party(now)), ("2차", party(test2b)), ("3차", party(test3))])],
          HERE / "mock-archmage-3.png")


def sheet_newlook(head_now, head_kept, head_new):
    """The kept tall version and the new look (mock-archmage-newlook.png)."""
    now, kept, new = load(NOW), load(KEPT), load(NEWLOOK)
    knight, valkyrie, goblin = load(KNIGHT), load(VALKYRIE), load(GOBLIN)
    party = lambda img: [(knight, 380), (valkyrie, 640), (img, 900), (goblin, 1340)]
    stack([large_row("Round 66 — 보관한 판(키 큼·모래시계·슬림·섹시)과 새 모습(키 보통·모래시계·슬림·섹시, 지금 그림과 무관)",
                     [("지금 (게임)", now), ("보관: 키 큼", kept), ("새 모습: 키 보통", new), ("발키리 (키 비교)", valkyrie)]),
           heads([("지금", now, head_now), ("보관", kept, head_kept), ("새 모습", new, head_new)]),
           stage_sheet("광산 무대, 게임 크기의 절반: 기사 / 발키리(hourglass) / 대마법사 / 고블린 약탈자",
                       [("지금", party(now)), ("보관: 키 큼", party(kept)), ("새 모습: 키 보통", party(new))])],
          HERE / "mock-archmage-newlook.png")


if __name__ == "__main__":
    import sys
    boxes = [tuple(int(v) for v in arg.split(",")) for arg in sys.argv[2:5]]
    {"1-2": sheet_1_2, "2-3": sheet_2_3, "newlook": sheet_newlook}[sys.argv[1]](*boxes)
