# -*- coding: utf-8 -*-
"""Round 94, 판정 1차: the rogue's 5th figure next to her hit and resolute poses before (drawn after the 8th figure, candidates/*_v1)
and after (drawn after the 5th, with the dagger in her near hand), all at one scale on the stage colour with the floor line. No API call.

    .venv/bin/python ArtPipeline/Archive/94-job-poses/review_rogue_hands.py [--out PATH]
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
FIGURE = (672, 896)
POSE = (2016, 1008)
SCALE = 0.5


def font(size):
    return ImageFont.truetype(str(FONT), size)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--out", default=str(HERE / "review-rogue-hands.png"))
    args = parser.parse_args()
    rows = [[("대기 (5차 대표 그림)", OUTPUT / "character" / "rogue.png"),
             ("피격 전 (8차 기준, v1)", HERE / "candidates" / "rogue_hit_v1.png"),
             ("피격 후 (5차 기준, 앞손)", OUTPUT / "hit" / "rogue.png")],
            [("대기 (5차 대표 그림)", OUTPUT / "character" / "rogue.png"),
             ("각성 전 (8차 기준, v1)", HERE / "candidates" / "rogue_resolute_v1.png"),
             ("각성 후 (5차 기준, 앞손)", OUTPUT / "hit" / "rogue_resolute.png")]]
    cell_w, cell_h = int(POSE[0] * SCALE), int(POSE[1] * SCALE)
    pad, label_h = 16, 36
    width = len(rows[0]) * (cell_w + pad) + pad
    height = len(rows) * (label_h + cell_h + pad) + pad
    canvas = Image.new("RGBA", (width, height), STAGE + (255,))
    draw = ImageDraw.Draw(canvas)
    for r, cols in enumerate(rows):
        for c, (name, path) in enumerate(cols):
            x = pad + c * (cell_w + pad)
            y = pad + r * (label_h + cell_h + pad) + label_h
            draw.text((x, y - label_h + 6), name, font=font(20), fill=(230, 230, 230))
            draw.rectangle([x, y, x + cell_w, y + cell_h], outline=(60, 66, 80))
            if not path.is_file():
                draw.text((x + 10, y + 10), "없음", font=font(18), fill=(200, 120, 120))
                continue
            im = Image.open(path).convert("RGBA")
            if im.size == FIGURE:
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
