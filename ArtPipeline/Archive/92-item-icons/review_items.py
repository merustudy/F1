# -*- coding: utf-8 -*-
"""Round 92: the stage 46 icon candidates laid on board pieces of their own shape (GridGeometry: 50px squares, 2px gaps), at screen
size and 2x, grouped by category, next to a few shipped icons for comparison; the two bags as the merchant's tiles; the two potions
in the 64px pocket and at their 96px. Makes no API call.

    .venv/bin/python ArtPipeline/Archive/92-item-icons/review_items.py [--out-dir ArtPipeline/Archive/92-item-icons]
      [--name KEY=NAME ...]   use output/<type>/<NAME>.png for KEY (a second candidate, e.g. --name banner=banner_v2)
"""
import argparse
import csv
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[3]
HERE = Path(__file__).resolve().parent
OUTPUT = ROOT / "ArtPipeline" / "output"
SHIPPED_ITEMS = ROOT / "Assets" / "@Art" / "Item"
SHIPPED_BAGS = ROOT / "Assets" / "@Art" / "Bag"
SHIPPED_POTIONS = ROOT / "Assets" / "@Art" / "Potion"
FONT = ROOT / "Assets" / "@Fonts" / "Source" / "Pretendard" / "Pretendard-Medium.ttf"
DATA = ROOT / "Assets" / "@Data" / "Source"

SQUARE, GAP, MARGIN = 50, 2, 4          # GridGeometry.Square, Gap; ItemSlotView.ArtMargin
STAGE = (0x14, 0x18, 0x21)
PIECE = (27, 37, 83)                    # UiPalette.GridPiece
LINE = (64, 72, 96)
TEXT = (235, 235, 230)
DIM = (150, 155, 165)
POCKET = (58, 52, 44)
BAG_RIM = {"knapsack": (112, 80, 48), "big_pack": (86, 60, 36)}

GROUPS = [
    ("무기 장비 (새 8 + 적 1 + 유니크 4)", ["serrated_dagger", "war_hammer", "pickaxe", "berserker_axe", "crossbow", "frost_staff", "shadow_staff",
                                       "chant_staff", "goblin_sawblade", "bloodgale", "windcutter", "cinder", "dawnlight"]),
    ("공격 아이템 4", ["frost_flask", "ash_pouch", "throwing_knives", "hex_bone"]),
    ("지원 아이템 6", ["censer", "holy_water", "war_drum", "banner", "war_horn", "bloodlust_charm"]),
    ("방어 장비 5 + 적 1", ["spiked_shield", "studded_armor", "leather_armor", "robe", "cloth_hood", "bristle_hide"]),
    ("지금 것 (비교)", ["dagger", "longsword", "halberd", "buckler", "iron_armor", "ember_flask"]),
]


def font(size):
    return ImageFont.truetype(str(FONT), size)


def read_items():
    with (DATA / "ItemData.csv").open(encoding="utf-8", newline="") as handle:
        return {row["Id"]: row for row in csv.DictReader(handle)}


def icon_of(key, names, shipped_dir, kind):
    name = names.get(key, key)
    candidate = OUTPUT / kind / f"{name}.png"
    if candidate.is_file() and key not in names and key in SHIPPED_KEYS:
        candidate = shipped_dir / f"{key}.png"
    if not candidate.is_file():
        candidate = shipped_dir / f"{key}.png"
    return Image.open(candidate).convert("RGBA") if candidate.is_file() else None


SHIPPED_KEYS = {"dagger", "longsword", "halberd", "buckler", "iron_armor", "ember_flask"}


def piece(canvas, origin, w, h, icon, scale):
    """A board piece of w x h squares at scale, its icon fitted inside by aspect (as ItemSlotView does)."""
    x0, y0 = origin
    pw = (w * SQUARE + (w - 1) * GAP) * scale
    ph = (h * SQUARE + (h - 1) * GAP) * scale
    draw = ImageDraw.Draw(canvas)
    draw.rectangle([x0, y0, x0 + pw, y0 + ph], fill=PIECE, outline=LINE, width=max(1, scale))
    if icon is None:
        draw.text((x0 + 6, y0 + 6), "missing", font=font(12 * scale), fill=(255, 120, 120))
        return pw, ph
    box_w, box_h = pw - 2 * MARGIN * scale, ph - 2 * MARGIN * scale
    bbox = icon.getbbox()
    art = icon.crop(bbox) if bbox else icon
    ratio = min(box_w / art.width, box_h / art.height)
    size = (max(1, round(art.width * ratio)), max(1, round(art.height * ratio)))
    art = art.resize(size, Image.LANCZOS)
    canvas.alpha_composite(art, (round(x0 + (pw - size[0]) / 2), round(y0 + (ph - size[1]) / 2)))
    return pw, ph


def items_sheet(names, out_path):
    items = read_items()
    cell_w, pad = 420, 14
    rows = []
    for title, keys in GROUPS:
        rows.append(("title", title))
        for key in keys:
            rows.append(("item", key))
    # Height: title 36; item row = max(piece 1x + piece 2x) + label.
    heights = []
    for kind, value in rows:
        if kind == "title":
            heights.append(40)
        else:
            h = int(items[value]["Height"])
            heights.append(max((h * SQUARE + (h - 1) * GAP) * 2 + 24, 96) + pad)
    width = cell_w * 2 + 40
    sheet = Image.new("RGBA", (width, sum(heights) + 40), STAGE + (255,))
    draw = ImageDraw.Draw(sheet)
    draw.text((20, 10), "Round 92 — 아이템 아이콘 후보: 조각(1배 · 2배). 키 · 이름 · 칸", font=font(18), fill=TEXT)
    y = 40
    for (kind, value), row_h in zip(rows, heights):
        if kind == "title":
            draw.text((20, y + 10), value, font=font(17), fill=(199, 179, 119))
            y += row_h
            continue
        row = items[value]
        w, h = int(row["Width"]), int(row["Height"])
        shipped = value in SHIPPED_KEYS
        icon = icon_of(value, names, SHIPPED_ITEMS, "item")
        pw1, ph1 = piece(sheet, (20, y + 22), w, h, icon, 1)
        piece(sheet, (20 + pw1 + 24, y + 22), w, h, icon, 2)
        label = f"{value}{' (지금 것)' if shipped else ''} · {row['Name.ko-KR']} · {w}×{h}"
        if value in names:
            label += f" · 후보 {names[value]}"
        draw.text((20, y + 2), label, font=font(14), fill=TEXT if not shipped else DIM)
        y += row_h
    sheet.save(out_path)
    return out_path


def bags_potions_sheet(names, out_path):
    with (DATA / "BagData.csv").open(encoding="utf-8", newline="") as handle:
        bags = {row["Id"]: row for row in csv.DictReader(handle)}
    sheet = Image.new("RGBA", (1240, 520), STAGE + (255,))
    draw = ImageDraw.Draw(sheet)
    draw.text((20, 10), "가방 둘(상인 타일: 1배 · 2배)과 포션 둘(64px 주머니 · 96px 원본), 지금 것 옆에", font=font(18), fill=TEXT)
    x = 20
    for key in ["belt_pouch", "leather_pouch", "knapsack", "big_pack"]:
        row = bags[key]
        w, h = int(row["Width"]), int(row["Height"])
        name = names.get(key, key)
        path = OUTPUT / "bag" / f"{name}.png"
        if key not in names and (SHIPPED_BAGS / f"{key}.png").is_file():
            path = SHIPPED_BAGS / f"{key}.png"
        icon = Image.open(path).convert("RGBA") if path.is_file() else None
        for scale, dy in ((1, 50), (2, 50 + (h * SQUARE + (h - 1) * GAP) + 20)):
            pw = (w * SQUARE + (w - 1) * GAP) * scale
            ph = (h * SQUARE + (h - 1) * GAP) * scale
            rim = BAG_RIM.get(key, (96, 70, 44))
            draw.rectangle([x, dy, x + pw, dy + ph], fill=rim, outline=LINE, width=scale)
            if icon is not None:
                art = icon.resize((pw, ph), Image.LANCZOS)
                sheet.alpha_composite(art, (x, dy))
            else:
                draw.text((x + 6, dy + 6), "missing", font=font(12), fill=(255, 120, 120))
        shipped = key in ("belt_pouch", "leather_pouch")
        draw.text((x, 30), f"{key}{' (지금 것)' if shipped else ''} {w}×{h}", font=font(13), fill=DIM if shipped else TEXT)
        x += (w * SQUARE + (w - 1) * GAP) * 2 + 40
    # Potions.
    y = 400
    x = 20
    for key, shipped_file in [("potion_heal", "healing_potion"), ("potion_shield", "barrier_potion"), ("potion_cleanse", None), ("potion_regen", None)]:
        name = names.get(key, key)
        path = OUTPUT / "ui_icon" / f"{name}.png"
        if shipped_file and key not in names:
            path = SHIPPED_POTIONS / f"{shipped_file}.png"
        icon = Image.open(path).convert("RGBA") if path.is_file() else None
        draw.rectangle([x, y, x + 64, y + 64], fill=POCKET, outline=LINE)
        if icon is not None:
            small = icon.resize((50, 50), Image.LANCZOS)
            sheet.alpha_composite(small, (x + 7, y + 7))
            sheet.alpha_composite(icon.resize((96, 96), Image.LANCZOS), (x + 80, y - 16))
        else:
            draw.text((x + 4, y + 24), "missing", font=font(12), fill=(255, 120, 120))
        draw.text((x, y + 70), f"{key}{' (지금 것)' if shipped_file else ''}", font=font(13), fill=DIM if shipped_file else TEXT)
        x += 210
    sheet.save(out_path)
    return out_path


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--out-dir", default=str(HERE))
    parser.add_argument("--name", action="append", default=[], help="KEY=NAME: use output/<type>/<NAME>.png for KEY")
    args = parser.parse_args()
    names = dict(pair.split("=", 1) for pair in args.name)
    out = Path(args.out_dir)
    print(items_sheet(names, out / "review-items.png"))
    print(bags_potions_sheet(names, out / "review-bags-potions.png"))


if __name__ == "__main__":
    main()
