# -*- coding: utf-8 -*-
"""Round 63 (the user, 2026-10-14: "카오스 제로 나이트메어 이미지 스타일 검색 후 우리 그림체 스타일에 조합, 테스트 세트로 발키리
이미지 1개 생성"): the game's valkyrie and the blended test figure side by side — large, the heads at 2x, and on the mine's
stage at the game's size next to the knight and the goblin raider, to see whether the blend still sits with the cast.
Pure Pillow, no API call. The game's art is not changed: everything is read from Assets/@Art and ArtPipeline/output.
  .venv/bin/python ArtPipeline/Archive/63-czn-style-blend/mock_compare.py
"""
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
PRET = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"
NOW = ROOT / "Assets/@Art/Unit/Job/valkyrie.png"
BLEND = HERE / "blend/valkyrie_czn.png"
KNIGHT = ROOT / "Assets/@Art/Unit/Job/knight.png"
GOBLIN = ROOT / "Assets/@Art/Unit/Enemy/goblin_raider.png"
BACKGROUND = ROOT / "Assets/@Art/Background/Dungeon/abandoned_mine.png"
PAPER = (28, 24, 22)
TEXT = (232, 224, 208)
NOTE = (176, 168, 150)
# The head and shoulders of each figure on its 672x896 canvas (read off the two images).
HEADS = {"now": (220, 230, 470, 460), "blend": (215, 205, 465, 435)}
FONTS = {}


def F(px):
    if px not in FONTS:
        FONTS[px] = ImageFont.truetype(str(PRET), px)
    return FONTS[px]


def crop_figure(img):
    return img.crop(img.getchannel("A").point(lambda a: 255 if a > 8 else 0).getbbox())


def large_pair(now, blend, height=700):
    pad = 40
    figs = []
    for img in (now, blend):
        fig = crop_figure(img)
        figs.append(fig.resize((int(fig.width * height / fig.height), height), Image.LANCZOS))
    w = sum(f.width for f in figs) + pad * 3
    sheet = Image.new("RGB", (w, height + 150), PAPER)
    d = ImageDraw.Draw(sheet)
    d.text((pad, 22), "Round 63 시험 — 발키리: 지금 게임 / 섞은 그림체(카오스 제로 나이트메어의 특징을 글로 옮김, 호출 1회)", font=F(24), fill=TEXT)
    x = pad
    for label, fig in zip(("지금 (게임)", "섞은 그림체 (valkyrie_czn)"), figs):
        d.text((x, 76), label, font=F(22), fill=NOTE)
        sheet.paste(fig, (x, 120), fig)
        x += fig.width + pad
    return sheet


def heads(now, blend, zoom=2.0):
    tiles = []
    for key, img in (("now", now), ("blend", blend)):
        tile = img.crop(HEADS[key])
        tiles.append(tile.resize((int(tile.width * zoom), int(tile.height * zoom)), Image.LANCZOS))
    pad = 40
    w = sum(t.width for t in tiles) + pad * 3
    sheet = Image.new("RGB", (w, tiles[0].height + 80), PAPER)
    d = ImageDraw.Draw(sheet)
    d.text((pad, 22), "머리 2배: 선, 눈, 명암", font=F(22), fill=TEXT)
    x = pad
    for tile in tiles:
        sheet.paste(tile, (x, 64), tile)
        x += tile.width + pad
    return sheet


def stage(valkyrie, knight, goblin, scale=0.6):
    """The mine's stage at the game's size (a unit about 290px tall at 1080p), darker at the edges like the candle vignette."""
    bg = Image.open(BACKGROUND).convert("RGB")
    bg = bg.resize((1920, int(bg.height * 1920 / bg.width)), Image.LANCZOS)
    floor = int(bg.height * 0.57)
    strip = bg.crop((0, floor - 460, 1920, floor + 100))
    vignette = Image.new("L", strip.size, 255)
    vd = ImageDraw.Draw(vignette)
    for i in range(0, 120, 4):
        vd.rectangle([i, i, strip.width - i, strip.height - i], outline=150 + int(105 * i / 120), width=4)
    vignette = vignette.filter(ImageFilter.GaussianBlur(40))
    strip = ImageChops.multiply(strip, Image.merge("RGB", (vignette, vignette, vignette)))
    for img, cx in ((knight, 520), (valkyrie, 790), (goblin, 1260)):
        fig = crop_figure(img)
        fig = fig.resize((int(fig.width * 290 / fig.height), 290), Image.LANCZOS)
        strip.paste(fig, (cx - fig.width // 2, 460 - fig.height), fig)
    return strip.resize((int(strip.width * scale), int(strip.height * scale)), Image.LANCZOS)


def stage_sheet(now, blend, knight, goblin):
    strips = [("지금", stage(now, knight, goblin)), ("섞은 발키리만 바꿈 (기사·고블린은 지금 그대로)", stage(blend, knight, goblin))]
    pad = 18
    w, h = strips[0][1].size
    sheet = Image.new("RGB", (w + pad * 2, 64 + len(strips) * (h + 44)), PAPER)
    d = ImageDraw.Draw(sheet)
    d.text((pad, 18), "광산 무대, 게임의 약 60% 크기: 기사 / 발키리 / 고블린 약탈자", font=F(22), fill=TEXT)
    y = 64
    for label, strip in strips:
        d.text((pad, y + 2), label, font=F(20), fill=NOTE)
        sheet.paste(strip, (pad, y + 30))
        y += h + 44
    return sheet


def main():
    now = Image.open(NOW).convert("RGBA")
    blend = Image.open(BLEND).convert("RGBA")
    knight = Image.open(KNIGHT).convert("RGBA")
    goblin = Image.open(GOBLIN).convert("RGBA")
    parts = [large_pair(now, blend), heads(now, blend), stage_sheet(now, blend, knight, goblin)]
    width = max(p.width for p in parts)
    sheet = Image.new("RGB", (width, sum(p.height for p in parts)), PAPER)
    y = 0
    for part in parts:
        sheet.paste(part, (0, y))
        y += part.height
    out = HERE / "mock-blend-vs-current.png"
    sheet.save(out)
    print(out, sheet.size)


if __name__ == "__main__":
    main()
