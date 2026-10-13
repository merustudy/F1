# -*- coding: utf-8 -*-
"""Round 61 trial set (the user, 2026-10-13: "A 시험세트 수행", "목업만 우선 제공"): the valkyrie, the knight and the goblin raider
as they are today and under three post-processing directions, side by side — A (tone unified with the dungeon + a hand-inked
line), ⓐ (crowquill ink and hatching, the Darkest Dungeon direction), ⓑ (a thick wobbly flat-cartoon line, the Backpack
Battles / Mewgenics direction). Pure Pillow, no API call, no numpy. The game's art is not changed: everything is drawn from
Assets/@Art into this folder.
  .venv/bin/python ArtPipeline/Archive/61-style-market-review/mock_trial.py
"""
import random
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageEnhance, ImageFilter, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
PRET = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"
UNITS = [
    ("발키리", ROOT / "Assets/@Art/Unit/Job/valkyrie.png"),
    ("기사", ROOT / "Assets/@Art/Unit/Job/knight.png"),
    ("고블린 약탈자", ROOT / "Assets/@Art/Unit/Enemy/goblin_raider.png"),
]
BACKGROUND = ROOT / "Assets/@Art/Background/Dungeon/abandoned_mine.png"
INK = (24, 18, 14)
PAPER = (28, 24, 22)
FONTS = {}


def F(px):
    if px not in FONTS:
        FONTS[px] = ImageFont.truetype(str(PRET), px)
    return FONTS[px]


# ---- building blocks (pure Pillow) ---------------------------------------------------------------

def smooth_noise(size, cell, seed):
    """A smooth random field in 0..255: coarse gaussian noise blown up and blurred."""
    random.seed(seed)
    w, h = size
    small = Image.effect_noise((max(2, w // cell), max(2, h // cell)), 64)
    return small.resize(size, Image.BICUBIC).filter(ImageFilter.GaussianBlur(cell / 3))


def wobble(img, amp, cell, seed):
    """Hand tremor: a mesh warp whose nodes move by a smooth noise field of up to amp pixels."""
    w, h = img.size
    nx, ny = smooth_noise((w, h), cell, seed), smooth_noise((w, h), cell, seed + 1)
    mesh = []
    for y in range(0, h, cell):
        for x in range(0, w, cell):
            x2, y2 = min(x + cell, w), min(y + cell, h)
            quad = []
            for (qx, qy) in ((x, y), (x, y2), (x2, y2), (x2, y)):
                px, py = min(qx, w - 1), min(qy, h - 1)
                dx = (nx.getpixel((px, py)) - 128) / 128 * amp
                dy = (ny.getpixel((px, py)) - 128) / 128 * amp
                quad.extend((qx + dx, qy + dy))
            mesh.append(((x, y, x2, y2), tuple(quad)))
    return img.transform(img.size, Image.MESH, mesh, Image.BICUBIC)


def tone(img, color, brightness, contrast, warm, light_top, light_bottom):
    """Saturation, brightness, contrast, a warm tint and a candle-like vertical light. Alpha untouched."""
    alpha = img.getchannel("A")
    rgb = img.convert("RGB")
    rgb = ImageEnhance.Color(rgb).enhance(color)
    rgb = ImageEnhance.Brightness(rgb).enhance(brightness)
    rgb = ImageEnhance.Contrast(rgb).enhance(contrast)
    if warm:
        rgb = ImageChops.multiply(rgb, Image.new("RGB", rgb.size, warm))
    grad = Image.linear_gradient("L").resize(rgb.size)  # 0 at the top, 255 at the bottom
    grad = grad.point(lambda v: int(255 * (light_top + (light_bottom - light_top) * v / 255)))
    rgb = ImageChops.multiply(rgb, Image.merge("RGB", (grad, grad, grad)))
    rgb.putalpha(alpha)
    return rgb


def figure_mask(img, above=128):
    return img.getchannel("A").point(lambda v: 255 if v > above else 0)


def quantize(img, colors):
    """A flat printed look: the figure's colours reduced to a few. Done on the original colours (before any tone change) so
    that dark cloaks keep their hue; the transparent area is filled with the figure's average so it takes no palette slot."""
    alpha = img.getchannel("A")
    solid = img.convert("RGB").filter(ImageFilter.MedianFilter(3))  # no speckles from anti-aliased highlights
    mask = figure_mask(img)
    box = mask.getbbox()
    avg = solid.crop(box).resize((1, 1), Image.BOX).getpixel((0, 0))
    filled = Image.composite(solid, Image.new("RGB", solid.size, avg), mask)
    flat = filled.quantize(colors=colors, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE).convert("RGB")
    flat.putalpha(alpha)
    return flat


def thin_dark(img, dark_below, blob=7):
    """The drawn lines: dark pixels inside the figure that are not part of a big dark mass (hair, a cloak)."""
    lum = img.convert("L")
    dark = ImageChops.multiply(lum.point(lambda v: 255 if v < dark_below else 0), figure_mask(img))
    blobs = dark.filter(ImageFilter.MinFilter(blob)).filter(ImageFilter.MaxFilter(blob + 2))
    return ImageChops.subtract(dark, blobs)


def ink_lines(img, lines, thicken):
    """The drawn lines re-inked as solid ink, thickened by a max filter. `lines` is a mask from the original image."""
    if thicken > 1:
        lines = lines.filter(ImageFilter.MaxFilter(thicken))
    lines = ImageChops.multiply(lines, figure_mask(img, 64))
    ink = Image.new("RGBA", img.size, INK + (0,))
    ink.putalpha(lines)
    return Image.alpha_composite(img, ink)


def shade_masks(img, dark_share, darker_share):
    """Where the figure is in shadow: the darkest `dark_share` and `darker_share` of its own pixels (by luminance), from
    the original colours, keeping off the ink lines themselves."""
    lum = img.convert("L")
    mask = figure_mask(img)
    hist = lum.histogram(mask)
    total = sum(hist)
    cut = []
    for share in (dark_share, darker_share):
        acc, level = 0, 0
        for level, n in enumerate(hist):
            acc += n
            if acc >= total * share:
                break
        cut.append(level)
    masks = []
    for level in cut:
        m = ImageChops.multiply(lum.point(lambda v, level=level: 255 if v < level else 0), mask)
        masks.append(m.filter(ImageFilter.MinFilter(3)))
    return masks


def outer_line(img, widths, cell, seed):
    """A hand-inked outer contour whose width wanders between the given odd widths by a noise field."""
    alpha = img.getchannel("A").point(lambda v: 255 if v > 96 else 0)
    noise = smooth_noise(img.size, cell, seed)
    steps = len(widths)
    chosen = None
    for i, w in enumerate(widths):
        dil = alpha.filter(ImageFilter.MaxFilter(w))
        lo, hi = int(255 * i / steps), int(255 * (i + 1) / steps)
        sel = noise.point(lambda v, lo=lo, hi=hi: 255 if lo <= v < hi or (i == steps - 1 and v >= hi) else 0)
        chosen = dil if chosen is None else Image.composite(dil, chosen, sel)
    band = ImageChops.subtract(chosen, alpha)
    ink = Image.new("RGBA", img.size, INK + (0,))
    ink.putalpha(band)
    return Image.alpha_composite(ink, img)


def hatch(img, where, angle, spacing, width, seed, opacity=170):
    """Crowquill hatching: wobbly parallel ink strokes laid only inside the `where` mask (the figure's shadow)."""
    w, h = img.size
    layer = Image.new("L", (w * 2, h * 2), 0)
    d = ImageDraw.Draw(layer)
    for k in range(-h * 2, w * 2 + h * 2, spacing):
        d.line([(k, 0), (k + h * 2, h * 2)], fill=255, width=width)
    layer = layer.rotate(angle - 45, resample=Image.BICUBIC, center=(w, h)).crop((w // 2, h // 2, w // 2 + w, h // 2 + h))
    layer = wobble(layer, 1.5, 40, seed)
    strokes = ImageChops.multiply(layer, where).point(lambda v: v * opacity // 255)
    ink = Image.new("RGBA", img.size, INK + (0,))
    ink.putalpha(strokes)
    return Image.alpha_composite(img, ink)


def grain(img, amount, seed):
    """Paper grain over the figure."""
    random.seed(seed)
    alpha = img.getchannel("A")
    noise = Image.effect_noise(img.size, 40).filter(ImageFilter.GaussianBlur(0.6))
    rgb = Image.blend(img.convert("RGB"), Image.merge("RGB", (noise, noise, noise)), amount)
    rgb.putalpha(alpha)
    return rgb


# ---- the four variants ---------------------------------------------------------------------------

def variant_now(img, seed):
    return img


def variant_a(img, seed):
    """A: the same drawing, its tone pulled toward the dungeon (a little less saturated and darker, warm, lit from above)
    and its line given a hand (tremor, re-inked thin lines, a wandering outer contour, paper grain)."""
    lines = thin_dark(img, 60)
    out = quantize(img, 32)
    out = tone(out, color=0.84, brightness=0.94, contrast=1.04, warm=(255, 246, 234), light_top=1.0, light_bottom=0.88)
    out = wobble(out, 2.0, 56, seed)
    lines = wobble(lines, 2.0, 56, seed)
    out = ink_lines(out, lines, thicken=1)
    out = outer_line(out, (3, 5, 7), 48, seed + 7)
    return grain(out, 0.04, seed)


def variant_ink(img, seed):
    """B-1: crowquill ink and woodcut hatching on a parchment palette (the Darkest Dungeon direction). Hatching only in
    the figure's own darkest 35% (and cross-hatching in its darkest 15%)."""
    lines = thin_dark(img, 70)
    shadow, deep = shade_masks(img, 0.35, 0.15)
    out = quantize(img, 14)
    out = tone(out, color=0.6, brightness=0.95, contrast=1.1, warm=(255, 238, 212), light_top=1.0, light_bottom=0.84)
    out = wobble(out, 3.0, 48, seed)
    lines, shadow, deep = (wobble(m, 3.0, 48, seed) for m in (lines, shadow, deep))
    out = ink_lines(out, lines, thicken=3)
    out = hatch(out, shadow, 45, 9, 2, seed=seed + 3)
    out = hatch(out, deep, -45, 9, 2, seed=seed + 5)
    out = outer_line(out, (5, 7, 9, 11), 40, seed + 11)
    return grain(out, 0.09, seed)


def variant_flat(img, seed):
    """B-2: a thick, wobbly flat-cartoon line and flat fills (the Backpack Battles / Mewgenics direction), a little darker
    than today."""
    lines = thin_dark(img, 70)
    out = quantize(img, 14)
    out = tone(out, color=0.92, brightness=0.95, contrast=1.08, warm=None, light_top=1.0, light_bottom=0.9)
    out = wobble(out, 3.5, 72, seed)
    lines = wobble(lines, 3.5, 72, seed)
    out = ink_lines(out, lines, thicken=3)
    out = outer_line(out, (7, 9, 11), 64, seed + 13)
    return out


VARIANTS = [
    ("지금", "생성 그대로(외곽 띠 1.35px)", variant_now),
    ("A 다듬기", "32색, 채도·명도를 던전 쪽으로 조금, 위에서 오는 빛, 흔들림 2px, 가는 선 다시 긋기, 가변 외곽 1~3px, 종이 결", variant_a),
    ("B-1 잉크·해칭", "양피지 팔레트 14색, 흔들림 3px, 가변 외곽 2~5px, 자기 그림의 어두운 35%에 해칭·15%에 교차 해칭", variant_ink),
    ("B-2 굵은 선 평면", "14색 평면, 흔들림 3.5px, 두꺼운 외곽 3~5px, 안쪽 선도 굵게. 해칭·결 없음", variant_flat),
]


# ---- sheets --------------------------------------------------------------------------------------

def crop_figure(img):
    return img.crop(img.getbbox())


def units_sheet(processed):
    """Rows = variants, columns = the three units, each 520px tall."""
    H, pad, label_w = 520, 24, 300
    cols = []
    for name, _ in UNITS:
        cols.append(name)
    widths = [0] * len(UNITS)
    rows = []
    for vname, note, _ in VARIANTS:
        row = []
        for ui, _ in enumerate(UNITS):
            fig = crop_figure(processed[vname][ui])
            fig = fig.resize((int(fig.width * H / fig.height), H), Image.LANCZOS)
            widths[ui] = max(widths[ui], fig.width)
            row.append(fig)
        rows.append((vname, note, row))
    W = label_w + sum(widths) + pad * (len(UNITS) + 1)
    sheet = Image.new("RGB", (W, 110 + len(VARIANTS) * (H + pad * 2)), PAPER)
    d = ImageDraw.Draw(sheet)
    d.text((pad, 20), "Round 61 시험 세트 — 후처리 목업 (호출 없음). 같은 그림, 만든 방법만 다르다", font=F(26), fill=(230, 222, 205))
    x0 = label_w + pad
    for ui, name in enumerate(cols):
        d.text((x0 + widths[ui] // 2, 86), name, font=F(20), fill=(176, 168, 150), anchor="mm")
        x0 += widths[ui] + pad
    y = 110 + pad
    for vname, note, row in rows:
        d.text((pad, y + 8), vname, font=F(30), fill=(238, 232, 216))
        lines, cur = [], ""
        for word in note.split(" "):
            if len(cur) + len(word) > 18:
                lines.append(cur)
                cur = word
            else:
                cur = (cur + " " + word).strip()
        lines.append(cur)
        for i, line in enumerate(lines):
            d.text((pad, y + 56 + i * 24), line, font=F(16), fill=(176, 168, 150))
        x = label_w + pad
        for ui, fig in enumerate(row):
            sheet.paste(fig, (x + (widths[ui] - fig.width) // 2, y), fig)
            x += widths[ui] + pad
        y += H + pad * 2
    return sheet


def battle_strip(processed_units, scale=0.6):
    """The three units on the mine's stage at the game's size (about 290px tall at 1080p), lit like the battle screen."""
    bg = Image.open(BACKGROUND).convert("RGB")
    bg = bg.resize((1920, int(bg.height * 1920 / bg.width)), Image.LANCZOS)
    floor = int(bg.height * 0.57)
    strip = bg.crop((0, floor - 460, 1920, floor + 100))
    # candle-lit stage: darker at the edges and the top, as the screen's vignette does
    vignette = Image.new("L", strip.size, 255)
    vd = ImageDraw.Draw(vignette)
    for i in range(0, 120, 4):
        shade = 150 + int(105 * i / 120)
        vd.rectangle([i, i, strip.width - i, strip.height - i], outline=shade, width=4)
    vignette = vignette.filter(ImageFilter.GaussianBlur(40))
    strip = ImageChops.multiply(strip, Image.merge("RGB", (vignette, vignette, vignette)))
    stage_h = 290
    for (img, cx) in zip(processed_units, (520, 760, 1180)):
        fig = crop_figure(img)
        fig = fig.resize((int(fig.width * stage_h / fig.height), stage_h), Image.LANCZOS)
        strip.paste(fig, (cx - fig.width // 2, 460 - fig.height), fig)
    return strip.resize((int(strip.width * scale), int(strip.height * scale)), Image.LANCZOS)


def battle_sheet(processed):
    strips = [(vname, battle_strip(processed[vname])) for vname, _, _ in VARIANTS]
    w = strips[0][1].width
    pad = 18
    sheet = Image.new("RGB", (w + pad * 2, 60 + len(strips) * (strips[0][1].height + 44)), PAPER)
    d = ImageDraw.Draw(sheet)
    d.text((pad, 18), "전투 무대에서 본 크기 (게임의 약 60% 크기). 위에서부터 지금 / A / B-1 / B-2", font=F(22), fill=(230, 222, 205))
    y = 60
    for vname, strip in strips:
        d.text((pad, y + 2), vname, font=F(20), fill=(238, 232, 216))
        sheet.paste(strip, (pad, y + 30))
        y += strip.height + 44
    return sheet


def closeup_sheet(processed, unit_index=1, box=(200, 240, 480, 520), zoom=2.2):
    """The knight's head and shoulders at 2.2x: the line is what the directions change."""
    tiles = []
    for vname, _, _ in VARIANTS:
        tile = processed[vname][unit_index].crop(box)
        tile = tile.resize((int(tile.width * zoom), int(tile.height * zoom)), Image.LANCZOS)
        tiles.append((vname, tile))
    pad = 18
    tw, th = tiles[0][1].size
    sheet = Image.new("RGB", (pad + len(tiles) * (tw + pad), th + 90), PAPER)
    d = ImageDraw.Draw(sheet)
    d.text((pad, 16), "기사의 머리 2.2배: 선과 명암의 차이", font=F(22), fill=(230, 222, 205))
    x = pad
    for vname, tile in tiles:
        d.text((x, 52), vname, font=F(20), fill=(238, 232, 216))
        sheet.paste(tile, (x, 82), tile)
        x += tw + pad
    return sheet


def main():
    processed = {}
    for vi, (vname, _, fn) in enumerate(VARIANTS):
        processed[vname] = []
        for ui, (_, path) in enumerate(UNITS):
            img = Image.open(path).convert("RGBA")
            processed[vname].append(fn(img, seed=100 + vi * 10 + ui))
            processed[vname][-1].save(HERE / f"trial-{vi}-{path.stem}.png")
    units_sheet(processed).save(HERE / "mock-trial-units.png")
    battle_sheet(processed).save(HERE / "mock-trial-battle.png")
    closeup_sheet(processed).save(HERE / "mock-trial-closeup.png")
    print("wrote mock-trial-units.png, mock-trial-battle.png, mock-trial-closeup.png")


if __name__ == "__main__":
    main()
