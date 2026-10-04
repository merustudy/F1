#!/usr/bin/env python3
"""Builds the review sheet of a round: the candidates at the size the game shows them, and large.

A verdict needs both. Art that looks right at full size can turn to mush at the size of its
place on the screen, so the top band draws every candidate at that size on the game's own
background colour, standing on a floor line as it will in battle. The bottom band shows the
style reference and each candidate large, on a checkerboard so the alpha edge can be seen.
An item icon has no floor to stand on: it is shown in the cells of the board it takes.

Makes no API call.
"""

import argparse
import csv
import re
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
REPO = ROOT.parent
OUTPUT_DIR = ROOT / "output"

# The game's colours are read from the UI palette, so the sheet cannot drift from the screens.
UI_PALETTE = REPO / "Assets" / "@Scripts" / "UI" / "UiPalette.cs"
LABEL_FONT = REPO / "Assets" / "@Fonts" / "Source" / "Pretendard" / "Pretendard-Medium.ttf"

# The style reference shown next to the candidates, per type.
REFERENCES = {
    "character": ROOT / "References" / "Character" / "style_ref_roster.png",
    "enemy": ROOT / "References" / "Character" / "style_ref_roster.png",
}

# The colour of the frame behind a figure, by the type of the figure: the party's or the enemy's.
SIDE_COLOURS = {"character": "Party", "enemy": "Enemy"}

# How much of the side's colour the figure's place shows in battle (BattleUnitView.FigureAlpha).
FIGURE_FRAME_ALPHA = 0.35

LARGE_HEIGHT = 896
PADDING = 24
GAP = 16
LABEL_HEIGHT = 30

# An item cell of a unit's board, in the 1920x1080 design space (BattleItemView, UiPrefabSetup):
# a cell is this wide and high, the cells of a board are stacked with a gap, and the icon
# sits inside the rim of the slot. The slot is the frame of the interface kit, stretched as the
# game stretches it; its border is the one UiArt gives it, in the pixels of the sprite (twice the
# size on screen).
ITEM_CELL = (180, 60)
ITEM_CELL_GAP = 2
ITEM_RIM = 7
ITEM_SLOT = OUTPUT_DIR / "ui_variant" / "slot.png"
ITEM_SLOT_BORDER = 30
# The part of an item that has not charged yet is dimmed this much; a brass line marks the charge.
ITEM_DIM = 0.45
ITEM_CHARGE_LINE = (184, 148, 78)
ITEM_DATA = REPO / "Assets" / "@Data" / "Source" / "ItemData.csv"


def palette() -> dict:
    """Colour name -> RGB, parsed from UiPalette.cs."""
    if not UI_PALETTE.is_file():
        raise SystemExit(f"실패: UiPalette.cs 가 없다: {UI_PALETTE}")
    pattern = re.compile(r"Color (\w+) = Rgb\(0x([0-9A-Fa-f]{2}), 0x([0-9A-Fa-f]{2}), 0x([0-9A-Fa-f]{2})\)")
    colours = {name: (int(r, 16), int(g, 16), int(b, 16))
               for name, r, g, b in pattern.findall(UI_PALETTE.read_text(encoding="utf-8"))}
    for needed in ("Background", "Party", "Enemy", "Line", "Text", "TextDim", "Slot"):
        if needed not in colours:
            raise SystemExit(f"실패: UiPalette.cs 에서 {needed} 색을 읽지 못했다.")
    return colours


def font(size: int):
    if LABEL_FONT.is_file():
        return ImageFont.truetype(str(LABEL_FONT), size)
    return ImageFont.load_default(size=size)


def blend(base, over, alpha: float):
    return tuple(round(b + (o - b) * alpha) for b, o in zip(base, over))


def checkerboard(width: int, height: int, tile: int = 16) -> Image.Image:
    board = Image.new("RGB", (width, height), (236, 236, 236))
    draw = ImageDraw.Draw(board)
    for y in range(0, height, tile):
        for x in range((y // tile % 2) * tile, width, tile * 2):
            draw.rectangle([x, y, x + tile - 1, y + tile - 1], fill=(206, 206, 206))
    return board


def scaled_to_height(image: Image.Image, height: int) -> Image.Image:
    width = max(1, round(image.width * height / image.height))
    return image.resize((width, height), Image.LANCZOS)


def item_rows() -> dict:
    """Item id -> its Korean name and the cells it takes, as the game's data has them."""
    if not ITEM_DATA.is_file():
        return {}
    with ITEM_DATA.open(encoding="utf-8-sig", newline="") as handle:
        return {row["Id"]: (row.get("Name.ko-KR", row["Id"]), int(row.get("Size") or 1)) for row in csv.DictReader(handle)}


def nine_slice(image: Image.Image, border: int, size) -> Image.Image:
    """Stretches a frame to a size and keeps its border as drawn, the way a sliced sprite is stretched."""
    width, height = size
    out = Image.new("RGBA", size, (0, 0, 0, 0))
    xs = [(0, border), (border, image.width - border), (image.width - border, image.width)]
    ys = [(0, border), (border, image.height - border), (image.height - border, image.height)]
    xd = [(0, border), (border, width - border), (width - border, width)]
    yd = [(0, border), (border, height - border), (height - border, height)]
    for (sx0, sx1), (dx0, dx1) in zip(xs, xd):
        for (sy0, sy1), (dy0, dy1) in zip(ys, yd):
            part = image.crop((sx0, sy0, sx1, sy1))
            if part.size != (dx1 - dx0, dy1 - dy0):
                part = part.resize((dx1 - dx0, dy1 - dy0), Image.BILINEAR)
            out.paste(part, (dx0, dy0))
    return out


def item_cell(icon: Image.Image, cells: int, charge: float, scale: int) -> Image.Image:
    """An item in the cells it takes, as the board draws it: the slot (as high as its cells stacked), the icon inside its rim.

    charge: how far the item has charged, 0..1. The charged part of the icon is lit from the
    left and the rest is dimmed; 1 shows the whole icon lit. scale 2 is the sprite's own size.
    """
    width = ITEM_CELL[0] * 2
    height = (ITEM_CELL[1] * cells + ITEM_CELL_GAP * (cells - 1)) * 2
    cell = nine_slice(Image.open(ITEM_SLOT).convert("RGBA"), ITEM_SLOT_BORDER, (width, height))

    x, y = (width - icon.width) // 2, (height - icon.height) // 2
    dimmed = Image.blend(Image.new("RGBA", icon.size, (30, 26, 26, 0)), icon, ITEM_DIM)
    dimmed.putalpha(icon.getchannel("A"))
    cell.alpha_composite(dimmed, (x, y))
    rim = ITEM_RIM * 2
    front = rim + round((width - 2 * rim) * charge)
    if front > x:
        cell.alpha_composite(icon.crop((0, 0, min(icon.width, front - x), icon.height)), (x, y))
    if 0 < charge < 1:
        ImageDraw.Draw(cell).rectangle([front - 3, rim, front, height - rim - 1], fill=ITEM_CHARGE_LINE)

    if scale == 1:
        cell = cell.resize((width // 2, height // 2), Image.LANCZOS)
    return cell


def item_sheet(names: list, folder: Path, out: Path) -> int:
    """Item icons in the cells they take: at the size on screen half charged and full, then at twice that."""
    colours = palette()
    rows = item_rows()
    if not ITEM_SLOT.is_file():
        raise SystemExit(f"실패: 칸의 그림이 없다: {ITEM_SLOT} (ui_variants.py 를 먼저 돌린다)")
    items = []
    for name in names:
        path = folder / f"{name}.png"
        if not path.is_file():
            raise SystemExit(f"실패: 그림이 없다: {path}")
        korean, cells = rows.get(name, (name, 1))
        items.append((name, korean, cells, Image.open(path).convert("RGBA")))

    # One item per line: at its size on screen (half charged over full) and at twice that to the right.
    # Every cell is as wide; a line is as high as the item's cells stacked, twice (the two charges), plus the label.
    label_font = font(18)
    widest = ITEM_CELL[0]
    heights = [LABEL_HEIGHT + (ITEM_CELL[1] * cells + ITEM_CELL_GAP * (cells - 1)) * 2 + GAP * 2 for _, _, cells, _ in items]
    width = PADDING * 2 + widest + GAP + widest * 2
    sheet = Image.new("RGBA", (width, PADDING * 2 + sum(heights)), colours["Background"] + (255,))
    draw = ImageDraw.Draw(sheet)

    y = PADDING
    for (name, korean, cells, icon), line_height in zip(items, heights):
        x = PADDING
        draw.text((x, y), f"{korean}  {name}  ({cells}칸)", font=label_font, fill=colours["TextDim"])
        top = y + LABEL_HEIGHT
        half = item_cell(icon, cells, 0.55, 1)
        full = item_cell(icon, cells, 1.0, 1)
        large = item_cell(icon, cells, 1.0, 2)
        sheet.alpha_composite(half, (x, top))
        sheet.alpha_composite(full, (x, top + half.height + GAP))
        sheet.alpha_composite(large, (x + widest + GAP, top))
        y += line_height

    out.parent.mkdir(parents=True, exist_ok=True)
    sheet.convert("RGB").save(out)
    print(f"리뷰 시트: {out} ({sheet.width}x{sheet.height}, 아이콘 {len(items)}개)")
    return 0


def roster_order(kind: str):
    """A sort key that puts names in the order of the type's roster; names it does not list come last."""
    roster = ROOT / "Rosters" / f"{kind}.csv"
    order = {}
    if roster.is_file():
        with roster.open(encoding="utf-8-sig", newline="") as handle:
            order = {row["Key"]: index for index, row in enumerate(csv.DictReader(handle))}
    return lambda name: (order.get(name, len(order)), name)


def parse_args():
    parser = argparse.ArgumentParser(description="Build the review sheet of generated candidates. No API call.")
    parser.add_argument("--type", dest="kind", required=True, help="Generation type, e.g. character.")
    parser.add_argument("--names", default="",
                        help="Comma separated output names, or paths to fitted PNGs kept elsewhere (an archived "
                             "candidate, for a comparison). Default: every fitted image of the type.")
    parser.add_argument("--display-height", type=int, default=300,
                        help="Height of the figure's place on the screen, in the 1920x1080 design space "
                             "(BattleFigureHeight in UiPrefabSetup.Battle.cs).")
    parser.add_argument("--out", default="", help="Sheet path. Default: output/review/<type>.png")
    parser.add_argument("--reference", default="",
                        help="Style reference shown on the sheet instead of the type's (a style test).")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    folder = OUTPUT_DIR / args.kind

    if args.names:
        names = [name.strip() for name in args.names.split(",") if name.strip()]
    else:
        names = sorted(path.stem for path in folder.glob("*.png") if not path.name.endswith(".raw.png"))
        names.sort(key=roster_order(args.kind))
    if not names:
        raise SystemExit(f"실패: 볼 그림이 없다: {folder}")

    out = Path(args.out) if args.out else OUTPUT_DIR / "review" / f"{args.kind}.png"
    if args.kind == "item":
        # An icon is judged in the cell it is shown in, not on a floor line.
        return item_sheet(names, folder, out)

    colours = palette()
    background = colours["Background"]

    figures, frames = [], {}
    for name in names:
        side = args.kind
        if name.endswith(".png"):
            # A fitted image outside the type's output folder: labelled with its folder, e.g.
            # "v1/knight" or "character/knight". A folder named after a type gives the side.
            path = Path(name)
            name = f"{path.parent.name}/{path.stem}"
            side = path.parent.name if path.parent.name in SIDE_COLOURS else args.kind
        else:
            path = folder / f"{name}.png"
        if not path.is_file():
            raise SystemExit(f"실패: 그림이 없다: {path}")
        figures.append((name, Image.open(path).convert("RGBA")))
        frames[name] = blend(background, colours[SIDE_COLOURS.get(side, "Party")], FIGURE_FRAME_ALPHA)
    label_font, title_font = font(18), font(22)

    reference_path = Path(args.reference) if args.reference else REFERENCES.get(args.kind)
    reference = None
    if reference_path is not None and reference_path.is_file():
        reference = scaled_to_height(Image.open(reference_path).convert("RGB"), LARGE_HEIGHT)

    # Top band: display size, on the game's background.
    small = [(name, scaled_to_height(image, args.display_height)) for name, image in figures]
    column = max(image.width for _, image in small)
    top_height = PADDING + LABEL_HEIGHT + args.display_height + 2 + LABEL_HEIGHT + PADDING

    # Bottom band: the reference and every candidate, large.
    large_widths = ([reference.width] if reference is not None else []) + [image.width for _, image in figures]
    bottom_width = PADDING * 2 + sum(large_widths) + GAP * (len(large_widths) - 1)
    bottom_height = PADDING + LABEL_HEIGHT + LARGE_HEIGHT + PADDING

    top_width = PADDING * 2 + column * len(small) + GAP * (len(small) - 1)
    width = max(top_width, bottom_width)
    sheet = Image.new("RGB", (width, top_height + bottom_height), (248, 248, 248))
    draw = ImageDraw.Draw(sheet)

    draw.rectangle([0, 0, width, top_height - 1], fill=background)
    draw.text((PADDING, PADDING), f"display size: figure place {args.display_height}px high (1920x1080)",
              font=title_font, fill=colours["TextDim"])
    figure_top = PADDING + LABEL_HEIGHT
    x = PADDING
    for name, image in small:
        draw.rectangle([x, figure_top, x + column - 1, figure_top + args.display_height - 1], fill=frames[name])
        sheet.paste(image, (x + (column - image.width) // 2, figure_top), image)
        draw.text((x + 4, figure_top + args.display_height + 6), name, font=label_font, fill=colours["Text"])
        x += column + GAP
    draw.rectangle([PADDING, figure_top + args.display_height, x - GAP - 1, figure_top + args.display_height + 1],
                   fill=colours["Line"])

    y = top_height + PADDING
    x = PADDING
    if reference is not None:
        draw.text((x, y), "style reference", font=title_font, fill=(60, 60, 60))
        sheet.paste(reference, (x, y + LABEL_HEIGHT))
        x += reference.width + GAP
    for name, image in figures:
        draw.text((x, y), f"{name}  ({image.width}x{image.height})", font=title_font, fill=(60, 60, 60))
        board = checkerboard(image.width, image.height)
        board.paste(image, (0, 0), image)
        sheet.paste(board, (x, y + LABEL_HEIGHT))
        x += image.width + GAP

    out.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(out)
    print(f"리뷰 시트: {out} ({sheet.width}x{sheet.height}, 그림 {len(figures)}장)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
