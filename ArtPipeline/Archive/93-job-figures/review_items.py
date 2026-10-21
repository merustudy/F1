# -*- coding: utf-8 -*-
"""Round 93: the two new base-weapon icons (cursed_tome, lute) on 3x1 board pieces at screen size and 2x, next to the shipped 3x1
base weapons (longsword, fire_staff, healing_staff) and the stage-46 staffs. No API call.

    .venv/bin/python ArtPipeline/Archive/93-job-figures/review_items.py
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[3]
HERE = Path(__file__).resolve().parent
OUTPUT = ROOT / "ArtPipeline" / "output" / "item"
SHIPPED = ROOT / "Assets" / "@Art" / "Item"
FONT = ROOT / "Assets" / "@Fonts" / "Source" / "Pretendard" / "Pretendard-Medium.ttf"
SQUARE, GAP, MARGIN = 50, 2, 4
STAGE = (0x14, 0x18, 0x21)
PIECE = (27, 37, 83)
LINE = (120, 120, 140)


def font(size):
    return ImageFont.truetype(str(FONT), size)


def piece(canvas, origin, w, h, icon, scale):
    x, y = origin
    pw, ph = (w * SQUARE + (w - 1) * GAP) * scale, (h * SQUARE + (h - 1) * GAP) * scale
    draw = ImageDraw.Draw(canvas)
    draw.rectangle([x, y, x + pw, y + ph], fill=PIECE, outline=LINE)
    if icon is not None:
        box = (pw - 2 * MARGIN * scale, ph - 2 * MARGIN * scale)
        im = icon.copy()
        im.thumbnail(box, Image.LANCZOS)
        canvas.alpha_composite(im, (int(x + (pw - im.width) / 2), int(y + (ph - im.height) / 2)))
    return pw, ph


def main():
    rows = [
        ("후보 cursed_tome (저주의 서)", OUTPUT / "cursed_tome.png"),
        ("후보 lute (낡은 류트)", OUTPUT / "lute.png"),
        ("지금 longsword", SHIPPED / "longsword.png"),
        ("지금 fire_staff", SHIPPED / "fire_staff.png"),
        ("지금 healing_staff", SHIPPED / "healing_staff.png"),
        ("지금 chant_staff", SHIPPED / "chant_staff.png"),
        ("지금 shadow_staff", SHIPPED / "shadow_staff.png"),
    ]
    width, line = 1180, 150
    canvas = Image.new("RGBA", (width, 40 + line * len(rows)), STAGE + (255,))
    draw = ImageDraw.Draw(canvas)
    draw.text((16, 10), "Round 93 — 3×1 조각 위의 아이콘: 1배(화면 크기)와 2배", font=font(20), fill=(230, 230, 230))
    y = 44
    for label, path in rows:
        icon = Image.open(path).convert("RGBA") if path.is_file() else None
        draw.text((16, y + 40), label, font=font(17), fill=(220, 220, 220))
        piece(canvas, (300, y + 30), 3, 1, icon, 1)
        piece(canvas, (480, y), 3, 1, icon, 2)
        if icon is not None:
            draw.text((820, y + 40), f"{icon.width}x{icon.height}", font=font(15), fill=(160, 160, 170))
        y += line
    out = HERE / "review-items.png"
    canvas.save(out)
    print(out)


if __name__ == "__main__":
    main()
