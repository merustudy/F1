# -*- coding: utf-8 -*-
"""Round 91: the seven state icon candidates as chips of the unit's state line (icon 18px + text 15px, outlined), next to the shipped
shield, burn and bleed chips, at screen size, 2x and 4x. Makes no API call.

    .venv/bin/python ArtPipeline/Archive/91-state-icons/mock_chips.py [--out mock-chips.png] [--freeze freeze_v2 ...]
"""
import argparse
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[3]
ICONS = ROOT / "Assets" / "@Art" / "UI" / "Icon"
OUTPUT = ROOT / "ArtPipeline" / "output" / "ui_icon"
FONT = ROOT / "Assets" / "@Fonts" / "Source" / "Pretendard" / "Pretendard-Medium.ttf"
HERE = Path(__file__).resolve().parent
STAGE = (0x14, 0x18, 0x21, 255)
SHEET = (0x22, 0x26, 0x30, 255)
# Sample colours for the review only; the chips' colours are a mock-up decision.
KINDS = [("freeze", "빙결 2", (0x9F, 0xD3, 0xF0)), ("blind", "암흑 2", (0xB9, 0x8F, 0xE0)), ("aim", "조준 2", (0xF0, 0xD4, 0x7A)),
         ("keen", "예리 2", (0xDC, 0xE6, 0xF0)), ("fury", "격분 2", (0xF0, 0x6E, 0x5A)), ("haste", "가속 2", (0xF3, 0xE2, 0x9A)), ("vulnerable", "취약 2", (0xE0, 0xA0, 0x60))]


def chip_row(chips, scale):
    icon_px, gap, pad = 18 * scale, 3 * scale, 12 * scale
    font = ImageFont.truetype(str(FONT), 15 * scale)
    widths = [icon_px + gap + int(font.getlength(text)) for _, text, _ in chips]
    image = Image.new("RGBA", (pad * 2 + sum(widths) + (len(chips) - 1) * pad, pad * 2 + icon_px), STAGE)
    draw = ImageDraw.Draw(image)
    x = pad
    for (path, text, color), width in zip(chips, widths):
        icon = Image.open(path).convert("RGBA").resize((icon_px, icon_px), Image.LANCZOS)
        image.alpha_composite(icon, (x, pad))
        tx, ty = x + icon_px + gap, pad + (icon_px - 15 * scale) // 2 - scale
        for dx in (-scale, 0, scale):
            for dy in (-scale, 0, scale):
                draw.text((tx + dx, ty + dy), text, font=font, fill=(0, 0, 0, 255))
        draw.text((tx, ty), text, font=font, fill=color + (255,))
        x += width + pad
    return image


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--out", default="mock-chips.png")
    for key, _, _ in KINDS:
        parser.add_argument(f"--{key}", default=key, help=f"Candidate name in output/ui_icon for {key}.")
    args = parser.parse_args()
    chips = [(ICONS / "shield.png", "방어 5", (0x8F, 0xB4, 0xE0)), (ICONS / "burn.png", "화상 3", (0xF3, 0x9C, 0x12)), (ICONS / "bleed.png", "출혈 2", (0xD8, 0x6A, 0x5A))]
    chips += [(OUTPUT / f"{getattr(args, key)}.png", text, color) for key, text, color in KINDS]
    rows = [chip_row(chips, 1), chip_row(chips, 2), chip_row(chips, 4)]
    label_font = ImageFont.truetype(str(FONT), 13)
    sheet = Image.new("RGBA", (max(r.width for r in rows) + 24, sum(r.height + 28 for r in rows) + 12), SHEET)
    draw = ImageDraw.Draw(sheet)
    y = 6
    for row, label in zip(rows, ["화면 크기 (아이콘 18px)", "2배", "4배"]):
        draw.text((12, y), label, font=label_font, fill=(200, 200, 200, 255))
        y += 20
        sheet.alpha_composite(row, (12, y))
        y += row.height + 8
    out = HERE / args.out
    sheet.save(out)
    print(out, sheet.size)


if __name__ == "__main__":
    main()
