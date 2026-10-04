#!/usr/bin/env python3
"""Builds the review sheet of a round: the candidates at the size the game shows them, and large.

A verdict needs both. Art that looks right at full size can turn to mush at the size of its
place on the screen, so the top band draws every candidate at that size on the game's own
background colour, standing on a floor line as it will in battle. The bottom band shows the
style reference and each candidate large, on a checkerboard so the alpha edge can be seen.
An item icon has no floor to stand on: it is shown in the cells of the board it takes.

A mercenary (type character) is judged among the others: the top band stands the candidates
next to every mercenary the game shows now, and a band of heads shows them all at one scale,
lined up on the eyes, with the band the chin has to fall in (FACE_BAND, a man's or a woman's as
the roster's Gender says). A head drawn small for a big body was approved once without being seen
next to the others (2026-10-04, the paladin).

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

# The face of a mercenary, from the eye line to the chin (the tip of a beard), on the fitted canvas
# (672x896): every mercenary's falls in the band of its sex, so that no head looks small next to the
# others; a woman's is a little smaller (Docs/Design/10_Art_Direction.md §2,
# Docs/Architecture/13_ART_PIPELINE.md "승인 라운드"). The men measured 68 to 81 (the paladin drawn
# again; his small head was 41), the women 51 to 56 (2026-10-04). The roster's Gender says which.
FACE_BAND = {"male": (65, 85), "female": (48, 62)}
CHARACTER_ROSTER = ROOT / "Rosters" / "character.csv"
# The figures the game shows now, the mercenaries a candidate is judged among.
APPROVED_FIGURES = REPO / "Assets" / "@Art" / "Unit" / "Job"
# The head's window around the eye on the fitted canvas, and how much it is enlarged.
HEAD_WINDOW = (220, 150, 110)   # width, above the eye, below the eye
HEAD_ZOOM = 2

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


def near_eye(image: Image.Image):
    """The middle of the largest eye white in the upper part of a figure (the near eye of a head
    turned to the right), or None when no eye white is found."""
    px = image.load()
    alpha = image.getchannel("A").getbbox()
    if alpha is None:
        return None
    x0, y0, x1, y1 = alpha[0], alpha[1], alpha[2], alpha[1] + int((alpha[3] - alpha[1]) * 0.45)
    seen, best = set(), None
    for y in range(y0, y1):
        for x in range(x0, x1):
            if (x, y) in seen:
                continue
            r, g, b, a = px[x, y]
            if a < 200 or min(r, g, b) <= 225:
                continue
            stack, count, sx, sy = [(x, y)], 0, 0, 0
            seen.add((x, y))
            while stack:
                cx, cy = stack.pop()
                count, sx, sy = count + 1, sx + cx, sy + cy
                for nx, ny in ((cx + 1, cy), (cx - 1, cy), (cx, cy + 1), (cx, cy - 1)):
                    if x0 <= nx < x1 and y0 <= ny < y1 and (nx, ny) not in seen:
                        q = px[nx, ny]
                        if q[3] >= 200 and min(q[:3]) > 225:
                            seen.add((nx, ny))
                            stack.append((nx, ny))
            if 30 <= count <= 2000 and (best is None or count > best[0]):
                best = (count, sx / count, sy / count)
    return None if best is None else (round(best[1]), round(best[2]))


def genders() -> dict:
    """Mercenary key -> "male" or "female", from the character roster."""
    with CHARACTER_ROSTER.open(encoding="utf-8", newline="") as handle:
        return {row["Key"]: (row.get("Gender") or "").strip() for row in csv.DictReader(handle)}


def face_band(name: str, known: dict):
    """The band of a figure named by its key, "game/<key>" or "<key>_<pose>": (low, high, sex), or None when the
    roster does not say whose figure it is."""
    stem = name.split("/")[-1]
    key = stem if stem in known else stem.split("_")[0]
    sex = known.get(key)
    return FACE_BAND[sex] + (sex,) if sex in FACE_BAND else None


def head_band(figures: list, colours: dict) -> Image.Image:
    """Every figure's head at one scale, lined up on the near eye, with the band its chin has to fall in."""
    width, above, below = HEAD_WINDOW
    cell_w, cell_h = width * HEAD_ZOOM, (above + below) * HEAD_ZOOM
    title, label = font(22), font(18)
    band = Image.new("RGB", (PADDING * 2 + len(figures) * (cell_w + GAP) - GAP, PADDING + LABEL_HEIGHT * 2 + cell_h + LABEL_HEIGHT + PADDING),
                     colours["Background"])
    draw = ImageDraw.Draw(band, "RGBA")
    bands = ", ".join(f"{sex} {low}-{high}px" for sex, (low, high) in FACE_BAND.items())
    draw.text((PADDING, PADDING), f"heads at one scale (canvas x{HEAD_ZOOM}), lined up on the eyes: the chin falls in the green band "
              f"below the eye on the canvas (FACE_BAND: {bands})", font=title, fill=colours["TextDim"])
    top = PADDING + LABEL_HEIGHT * 2
    known = genders()
    for i, (name, image) in enumerate(figures):
        x = PADDING + i * (cell_w + GAP)
        eye = near_eye(image)
        found = eye is not None
        if not found:
            box = image.getchannel("A").getbbox()
            eye = ((box[0] + box[2]) // 2, box[1] + (box[3] - box[1]) * 22 // 100)
        crop = image.crop((eye[0] - width // 2, eye[1] - above, eye[0] + width // 2, eye[1] + below))
        cell = Image.new("RGBA", crop.size, colours["Background"] + (255,))
        cell.alpha_composite(crop)
        band.paste(cell.convert("RGB").resize((cell_w, cell_h), Image.LANCZOS), (x, top))
        band_of = face_band(name, known)
        low, high, sex = band_of if band_of else (min(b[0] for b in FACE_BAND.values()), max(b[1] for b in FACE_BAND.values()), "?")
        chin = (top + (above + low) * HEAD_ZOOM, top + (above + high) * HEAD_ZOOM)
        draw.rectangle([x, chin[0], x + cell_w - 1, chin[1]], fill=(90, 200, 110, 70), outline=(90, 200, 110, 200))
        draw.line([x, top + above * HEAD_ZOOM, x + cell_w - 1, top + above * HEAD_ZOOM], fill=(110, 170, 230, 200), width=1)
        note = f"{sex} {low}-{high}" + ("" if found else ", no eye found: lined up on the head")
        draw.text((x + 4, top + cell_h + 6), f"{name} ({note})", font=label, fill=colours["Text"])
        print(f"  머리: {name}: 눈 {eye}{'' if found else ' (눈을 찾지 못해 머리 위에서 맞춤)'}. "
              f"턱이 초록 띠({'성별을 모름, ' if sex == '?' else ''}눈에서 {low}~{high}px) 안에 있는지 본다")
    return band


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

    # A mercenary is judged among the mercenaries the game shows now.
    lineup = figures
    if args.kind == "character":
        approved = sorted((path.stem for path in APPROVED_FIGURES.glob("*.png")), key=roster_order(args.kind))
        lineup = [(f"game/{key}", Image.open(APPROVED_FIGURES / f"{key}.png").convert("RGBA")) for key in approved] + figures
        for name, _ in lineup:
            frames.setdefault(name, blend(background, colours["Party"], FIGURE_FRAME_ALPHA))

    reference_path = Path(args.reference) if args.reference else REFERENCES.get(args.kind)
    reference = None
    if reference_path is not None and reference_path.is_file():
        reference = scaled_to_height(Image.open(reference_path).convert("RGB"), LARGE_HEIGHT)

    # Top band: display size, on the game's background.
    small = [(name, scaled_to_height(image, args.display_height)) for name, image in lineup]
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

    if args.kind == "character":
        heads = head_band(lineup, colours)
        whole = Image.new("RGB", (max(sheet.width, heads.width), sheet.height + heads.height), (248, 248, 248))
        whole.paste(sheet, (0, 0))
        whole.paste(heads, (0, sheet.height))
        sheet = whole

    out.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(out)
    print(f"리뷰 시트: {out} ({sheet.width}x{sheet.height}, 그림 {len(figures)}장)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
