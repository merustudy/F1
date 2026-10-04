#!/usr/bin/env python3
"""The review sheet of mercenaries' attack and hit poses. No API call.

  review_pose.py                       every job that has both poses fitted (output/attack/<key>.png, output/hit/<key>.png)
  review_pose.py --key paladin --out ArtPipeline/Archive/<round>/review-pose.png

A row per mercenary: the approved figure, the attack pose and the hit pose, on the pose canvas at one scale (half the
canvas: about one and a half times the battle's), standing on the floor line (the blue line) with the back foot where the
figure's is, on the stage's colour. A mercenary that holds a different thing in each hand has its hands written over its
row (Rosters/character.csv, Hands): the three pictures keep them (Docs/Design/10 §5). What to look at before a verdict is
listed in Docs/Architecture/13_ART_PIPELINE.md ("자세 그림의 점검").
"""
import argparse
import csv
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

TOOLS = Path(__file__).resolve().parent
ROOT = TOOLS.parent
REPO = ROOT.parent
sys.path.insert(0, str(TOOLS))
from fit_pose import FLOOR, OUTPUT, idle_wide  # noqa: E402

FONT = REPO / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"
Z = 0.5
INK, DIM, BACK, STAGE = (235, 235, 230), (160, 162, 170), (22, 20, 20), (58, 46, 36)


def font(size):
    return ImageFont.truetype(str(FONT), size)


def characters():
    """Key -> (Hands, the order of the roster)."""
    with (ROOT / "Rosters" / "character.csv").open(encoding="utf-8", newline="") as handle:
        return {row["Key"]: (row.get("Hands") or "").strip() for row in csv.DictReader(handle)}


def row(key, hands):
    images = [idle_wide(key)] + [Image.open(OUTPUT / pose / f"{key}.png").convert("RGBA") for pose in ("attack", "hit")]
    boxes = [image.getchannel("A").getbbox() for image in images]
    top = min(b[1] for b in boxes) - 12
    bottom = max(b[3] for b in boxes) + 12
    cells = []
    for image, box in zip(images, boxes):
        crop = image.crop((box[0] - 12, top, box[2] + 12, bottom))
        cell = Image.new("RGBA", crop.size, STAGE + (255,))
        cell.alpha_composite(crop)
        cell = cell.convert("RGB").resize((round(crop.width * Z), round(crop.height * Z)), Image.LANCZOS)
        ImageDraw.Draw(cell).line([0, round((FLOOR - top) * Z), cell.width, round((FLOOR - top) * Z)], fill=(70, 140, 220), width=1)
        cells.append(cell)
    height = max(c.height for c in cells)
    out = Image.new("RGB", (24 + sum(c.width + 16 for c in cells), height + 84), BACK)
    d = ImageDraw.Draw(out)
    d.text((24, 10), key, fill=INK, font=font(28))
    if hands:
        d.text((24 + d.textlength(key, font=font(28)) + 16, 16), f"손: {hands} (세 그림이 같아야 한다)", fill=DIM, font=font(20))
    x = 24
    for cell, label in zip(cells, ("대기 (확정 그림)", "공격", "피격")):
        out.paste(cell, (x, 52))
        d.text((x + 4, 52 + cell.height + 4), label, fill=DIM, font=font(20))
        x += cell.width + 16
    return out


def main():
    parser = argparse.ArgumentParser(description="Build the review sheet of mercenaries' poses. No API call.")
    parser.add_argument("--key", dest="keys", action="append", help="A job id; may be given more than once. Default: every job with both poses.")
    parser.add_argument("--out", default="", help="Sheet path. Default: output/review/pose.png")
    args = parser.parse_args()
    known = characters()
    keys = args.keys or [key for key in known if (OUTPUT / "attack" / f"{key}.png").is_file() and (OUTPUT / "hit" / f"{key}.png").is_file()]
    if not keys:
        raise SystemExit("실패: 맞춘 자세가 없다. 먼저 fit_pose.py")
    rows = [row(key, known.get(key, "")) for key in keys]
    width = max(r.width for r in rows)
    sheet = Image.new("RGB", (width, sum(r.height for r in rows)), BACK)
    y = 0
    for r in rows:
        sheet.paste(r, (0, y))
        y += r.height
    out = Path(args.out) if args.out else OUTPUT / "review" / "pose.png"
    out.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(out)
    print(f"리뷰 시트: {out} ({sheet.width}x{sheet.height}, 용병 {len(keys)})")
    return 0


if __name__ == "__main__":
    sys.exit(main())
