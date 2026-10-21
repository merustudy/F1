# -*- coding: utf-8 -*-
"""Round 90: the status icon candidates as chips at the size the unit's state line shows them.

Draws the chips of the state line (icon 18px + text 15px, outlined) next to the shipped shield and
burn chips, at screen size, 2x and 4x, on the stage's dark colour. Makes no API call.

    .venv/bin/python ArtPipeline/Archive/90-status-icons/mock_chips.py [--thorns thorns_v2] [--out mock-chips-v2.png]
"""

import argparse
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[3]
ICONS = ROOT / "Assets" / "@Art" / "UI" / "Icon"
OUTPUT = ROOT / "ArtPipeline" / "output" / "ui_icon"
FONT = ROOT / "Assets" / "@Fonts" / "Source" / "Pretendard" / "Pretendard-Medium.ttf"
HERE = Path(__file__).resolve().parent

# The text colours are samples for the review only: the chips' colours are a mock-up round's decision (Design/18 판정 11).
STAGE = (0x14, 0x18, 0x21, 255)
SHEET = (0x22, 0x26, 0x30, 255)


def chip_row(chips, scale):
    icon_px = 18 * scale
    gap = 3 * scale
    pad = 12 * scale
    font = ImageFont.truetype(str(FONT), 15 * scale)
    widths = [icon_px + gap + int(font.getlength(text)) for _, text, _ in chips]
    width = pad * 2 + sum(widths) + (len(chips) - 1) * pad
    height = pad * 2 + icon_px
    image = Image.new("RGBA", (width, height), STAGE)
    draw = ImageDraw.Draw(image)
    x = pad
    for (path, text, color), chip_width in zip(chips, widths):
        icon = Image.open(path).convert("RGBA").resize((icon_px, icon_px), Image.LANCZOS)
        image.alpha_composite(icon, (x, pad))
        tx, ty = x + icon_px + gap, pad + (icon_px - 15 * scale) // 2 - scale
        for dx in (-scale, 0, scale):
            for dy in (-scale, 0, scale):
                draw.text((tx + dx, ty + dy), text, font=font, fill=(0, 0, 0, 255))
        draw.text((tx, ty), text, font=font, fill=color + (255,))
        x += chip_width + pad
    return image


def main():
    parser = argparse.ArgumentParser(description="Round 90 chip-size mock of the status icons.")
    parser.add_argument("--bleed", default="bleed", help="Candidate name in output/ui_icon for the bleed icon.")
    parser.add_argument("--regen", default="regen")
    parser.add_argument("--thorns", default="thorns")
    parser.add_argument("--out", default="mock-chips.png", help="File name in this round's folder.")
    args = parser.parse_args()

    chips = [
        (ICONS / "shield.png", "방어 5", (0x8F, 0xB4, 0xE0)),
        (ICONS / "burn.png", "화상 3", (0xF3, 0x9C, 0x12)),
        (OUTPUT / f"{args.bleed}.png", "출혈 2", (0xE0, 0x4A, 0x4A)),
        (OUTPUT / f"{args.regen}.png", "재생 3", (0x7C, 0xC8, 0x6A)),
        (OUTPUT / f"{args.thorns}.png", "가시 2", (0xE8, 0xE4, 0xD8)),
    ]
    rows = [chip_row(chips, 1), chip_row(chips, 2), chip_row(chips, 4)]
    labels = ["화면 크기 (아이콘 18px)", "2배", "4배"]
    label_font = ImageFont.truetype(str(FONT), 13)
    width = max(row.width for row in rows) + 24
    height = sum(row.height + 28 for row in rows) + 12
    sheet = Image.new("RGBA", (width, height), SHEET)
    draw = ImageDraw.Draw(sheet)
    y = 6
    for row, label in zip(rows, labels):
        draw.text((12, y), label, font=label_font, fill=(200, 200, 200, 255))
        y += 20
        sheet.alpha_composite(row, (12, y))
        y += row.height + 8
    out = HERE / args.out
    sheet.save(out)
    print(out, sheet.size)


if __name__ == "__main__":
    main()
