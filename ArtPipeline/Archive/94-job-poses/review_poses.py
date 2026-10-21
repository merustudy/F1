# -*- coding: utf-8 -*-
"""Round 94: each new job's approved figure next to its fitted attack, hit and resolute poses, all at one scale on the stage colour,
with the floor line drawn, so that the head size, the rear foot and the weapon can be compared. No API call.

    .venv/bin/python ArtPipeline/Archive/94-job-poses/review_poses.py [--jobs rogue,archer,occultist,minstrel] [--out PATH]
"""
import argparse
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[3]
HERE = Path(__file__).resolve().parent
OUTPUT = ROOT / "ArtPipeline" / "output"
FONT = ROOT / "Assets" / "@Fonts" / "Source" / "Pretendard" / "Pretendard-Medium.ttf"
STAGE = (0x14, 0x18, 0x21)
FLOOR = (90, 100, 120)
FIGURE = (672, 896)            # the figure canvas; the pose canvas is 2016x1008 with the figure canvas centred and 112px of floor below
POSE = (2016, 1008)
SCALE = 0.5


def font(size):
    return ImageFont.truetype(str(FONT), size)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--jobs", default="rogue,archer,occultist,minstrel")
    parser.add_argument("--out", default=str(HERE / "review-poses.png"))
    args = parser.parse_args()
    jobs = args.jobs.split(",")
    cols = [("대기 (character)", lambda j: OUTPUT / "character" / f"{j}.png"),
            ("공격 (attack)", lambda j: OUTPUT / "attack" / f"{j}.png"),
            ("피격 (hit) = 붕괴", lambda j: OUTPUT / "hit" / f"{j}.png"),
            ("각성 (resolute)", lambda j: OUTPUT / "hit" / f"{j}_resolute.png")]
    cell_w, cell_h = int(POSE[0] * SCALE), int(POSE[1] * SCALE)
    pad, label_h, left = 16, 36, 150
    width = left + len(cols) * (cell_w + pad) + pad
    height = label_h + len(jobs) * (cell_h + pad) + pad
    canvas = Image.new("RGBA", (width, height), STAGE + (255,))
    draw = ImageDraw.Draw(canvas)
    for c, (name, _) in enumerate(cols):
        draw.text((left + c * (cell_w + pad) + pad, 8), name, font=font(20), fill=(230, 230, 230))
    for r, job in enumerate(jobs):
        y = label_h + r * (cell_h + pad)
        draw.text((pad, y + cell_h // 2), job, font=font(22), fill=(230, 230, 230))
        for c, (_, path_of) in enumerate(cols):
            x = left + c * (cell_w + pad)
            draw.rectangle([x, y, x + cell_w, y + cell_h], outline=(60, 66, 80))
            path = path_of(job)
            if not path.is_file():
                draw.text((x + 10, y + 10), "없음", font=font(18), fill=(200, 120, 120))
                continue
            im = Image.open(path).convert("RGBA")
            if im.size == FIGURE:
                # Lay the figure on a pose canvas the way the game does: centred, its floor 112px above the bottom.
                board = Image.new("RGBA", POSE, (0, 0, 0, 0))
                board.alpha_composite(im, ((POSE[0] - FIGURE[0]) // 2, 0))
                im = board
            elif im.size != POSE:
                im = im.resize(POSE, Image.LANCZOS)
            small = im.resize((cell_w, cell_h), Image.LANCZOS)
            canvas.alpha_composite(small, (x, y))
            floor = y + int((POSE[1] - 112) * SCALE)
            draw.line([x, floor, x + cell_w, floor], fill=FLOOR, width=1)
    canvas.save(args.out)
    print(args.out)


if __name__ == "__main__":
    main()
