"""Heads side by side with a grid, to compare the head size of a state pose with the approved figure's by eye. No API call.
  .venv/bin/python ArtPipeline/Archive/40-state-poses/review_heads.py [--jobs knight,bishop] [--suffix=-pass1]
Each cell is a 400x400 window of the pose canvas (the fitted pose, 2048x1024 stood back on 2016x1008; the approved figure as
fit_pose.idle_wide stands it) centred on the face (the largest skin-coloured area in the top 40% of the figure), at the canvas's
own scale (about 2.8 times the battle's), with a line every 25 px. Writes review-heads<suffix>.png.
"""
import argparse
import colorsys
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
sys.path.insert(0, str(ROOT / "ArtPipeline" / "tools"))
import fit_pose as F  # noqa: E402
from fit_pose import OUTPUT, WIDE, idle_wide  # noqa: E402
from gen_image import subject_box  # noqa: E402

FONT = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"
INK, DIM, BACK, STAGE, GRID = (235, 235, 230), (160, 162, 170), (22, 20, 20), (58, 46, 36), (95, 80, 66)
WINDOW, STEP = 400, 25
LABELS = ("대기", "붕괴", "각성")


def skin(r, g, b):
    h, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
    return (h <= 0.11 or h >= 0.97) and 0.12 <= s <= 0.55 and v >= 0.62


def face_centre(image):
    box = subject_box(image)
    limit = box[1] + (box[3] - box[1]) * 0.40
    best = None
    for pts in F.blobs(image, skin, 300, 10 ** 7):
        n = len(pts)
        cy = sum(p[1] for p in pts) / n
        if cy > limit:
            continue
        if best is None or n > best[0]:
            best = (n, round(sum(p[0] for p in pts) / n), round(cy))
    return (best[1], best[2]) if best else ((box[0] + box[2]) // 2, box[1] + 120)


def cell(image):
    cx, cy = face_centre(image)
    x0, y0 = cx - WINDOW // 2, max(0, cy - WINDOW // 2 - 40)
    crop = image.crop((x0, y0, x0 + WINDOW, y0 + WINDOW))
    out = Image.new("RGBA", crop.size, STAGE + (255,))
    d = ImageDraw.Draw(out)
    for k in range(0, WINDOW, STEP):
        d.line([k, 0, k, WINDOW], fill=GRID, width=1)
        d.line([0, k, WINDOW, k], fill=GRID, width=1)
    out.alpha_composite(crop)
    return out.convert("RGB")


def row(job):
    images = [idle_wide(job)] + [Image.open(OUTPUT / "hit" / f"{job}_{s}.png").convert("RGBA").resize(WIDE, Image.LANCZOS) for s in ("broken", "resolute")]
    cells = [cell(i) for i in images]
    out = Image.new("RGB", (24 + 3 * (WINDOW + 16), WINDOW + 72), BACK)
    d = ImageDraw.Draw(out)
    font = ImageFont.truetype(str(FONT), 26)
    d.text((24, 8), job, fill=INK, font=font)
    x = 24
    for c, label in zip(cells, LABELS):
        out.paste(c, (x, 44))
        d.text((x + 6, 44 + 4), label, fill=INK, font=font)
        x += WINDOW + 16
    return out


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--jobs", default="valkyrie,knight,bishop,paladin,archmage,spellblade")
    parser.add_argument("--suffix", default="")
    args = parser.parse_args()
    rows = [row(j) for j in args.jobs.split(",")]
    sheet = Image.new("RGB", (rows[0].width, sum(r.height for r in rows)), BACK)
    y = 0
    for r in rows:
        sheet.paste(r, (0, y))
        y += r.height
    sheet.save(HERE / f"review-heads{args.suffix}.png")
    print(f"review-heads{args.suffix}.png {sheet.size}")


if __name__ == "__main__":
    main()
