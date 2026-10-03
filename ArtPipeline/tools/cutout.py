#!/usr/bin/env python3
"""Cuts a figure out of a picture with a plain background, so the picture itself can be used as art.

For art that is not generated: a concept picture approved as it is, drawn on a plain light or
dark backdrop. The result is the raw image of a roster key (output/<type>/<name>.raw.png),
and gen_image.py --refit then stands it on the figure canvas like any generated piece.

Only the background that reaches the edge of the picture is removed. A region of the
background colour that the outline closes off (white eyes, a pale shirt) stays. Along the
cut the backdrop is taken back out of the blended edge pixels, so no light fringe is left
around the outline.

Makes no API call.
"""

import argparse
import sys
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageOps

ROOT = Path(__file__).resolve().parents[1]
OUTPUT_DIR = ROOT / "output"

# A pixel this close to the backdrop on every channel counts as backdrop.
DEFAULT_THRESHOLD = 24

# How far into the figure the edge is softened, in pixels. It must stay inside the outline.
DEFAULT_BAND = 2

# A pixel that differs from the backdrop by this much on its strongest channel is taken as fully
# the figure. Edge pixels between the threshold and this are blends of outline and backdrop.
SOLID_DIFFERENCE = 200

FILLED = 128


class PipelineError(Exception):
    """Something the operator can act on."""


def backdrop_of(image: Image.Image, threshold: int):
    """The colour of the backdrop: the four corners, which must agree."""
    width, height = image.size
    corners = [image.getpixel(point) for point in ((0, 0), (width - 1, 0), (0, height - 1), (width - 1, height - 1))]
    first = corners[0]
    for corner in corners[1:]:
        if max(abs(a - b) for a, b in zip(first, corner)) > threshold:
            raise PipelineError(f"네 모서리의 색이 서로 다르다. 배경이 한 색이 아니다: {corners}")
    return first


def cut_out(source: Image.Image, threshold: int, band: int):
    """Returns the RGBA cut-out and how much of the picture was backdrop."""
    image = source.convert("RGB")
    backdrop = backdrop_of(image, threshold)

    # How far each pixel is from the backdrop, on its strongest channel.
    red, green, blue = ImageChops.difference(image, Image.new("RGB", image.size, backdrop)).split()
    distance = ImageChops.lighter(ImageChops.lighter(red, green), blue)

    # Backdrop-like pixels, then only those that reach the edge of the picture: the fill starts
    # outside the picture, on a frame of backdrop one pixel wide.
    like = distance.point(lambda value: 0 if value <= threshold else 255)
    framed = ImageOps.expand(like, border=1, fill=0)
    ImageDraw.floodfill(framed, (0, 0), FILLED)
    outside = framed.crop((1, 1, 1 + image.width, 1 + image.height)).point(lambda value: 255 if value == FILLED else 0)

    # The band of figure pixels next to the cut.
    grown = outside.filter(ImageFilter.MaxFilter(2 * band + 1))
    edge = ImageChops.subtract(grown, outside)

    alpha = outside.point(lambda value: 0 if value else 255)
    result = image.convert("RGBA")
    result.putalpha(alpha)

    pixels = result.load()
    edge_data = edge.load()
    distance_data = distance.load()
    softened = 0
    for y in range(image.height):
        for x in range(image.width):
            if not edge_data[x, y]:
                continue
            share = min(1.0, distance_data[x, y] / SOLID_DIFFERENCE)
            if share >= 1.0:
                continue
            # The pixel is share * figure + (1 - share) * backdrop: take the backdrop back out.
            r, g, b, _ = pixels[x, y]
            colour = tuple(
                max(0, min(255, round(back + (value - back) / share))) if share > 0 else value
                for value, back in zip((r, g, b), backdrop)
            )
            pixels[x, y] = (*colour, round(255 * share))
            softened += 1

    total = image.width * image.height
    removed = sum(1 for value in outside.getdata() if value)
    return result, {"backdrop": backdrop, "removed": removed / total, "softened": softened}


def parse_args():
    parser = argparse.ArgumentParser(description="Cut a figure out of a picture with a plain background. No API call.")
    parser.add_argument("--source", required=True, help="The picture to cut the figure out of.")
    parser.add_argument("--type", dest="kind", required=True, help="Generation type the result belongs to, e.g. character.")
    parser.add_argument("--key", required=True, help="Roster key the picture is the art of.")
    parser.add_argument("--name", default="", help="Output file stem. Defaults to the key.")
    parser.add_argument("--threshold", type=int, default=DEFAULT_THRESHOLD,
                        help="How close to the backdrop colour a pixel must be to count as backdrop (per channel).")
    parser.add_argument("--band", type=int, default=DEFAULT_BAND,
                        help="How many pixels into the figure the edge is softened.")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    source = Path(args.source)
    raw_path = OUTPUT_DIR / args.kind / f"{args.name or args.key}.raw.png"

    try:
        if not source.is_file():
            raise PipelineError(f"원본 그림이 없다: {source}")
        if raw_path.exists():
            raise PipelineError(f"산출물이 이미 있다. 덮어쓰지 않는다: {raw_path}")

        result, report = cut_out(Image.open(source), args.threshold, args.band)
        if result.getchannel("A").getbbox() is None:
            raise PipelineError("그림이 전부 배경으로 잘렸다. --threshold 를 낮춰라.")

        raw_path.parent.mkdir(parents=True, exist_ok=True)
        result.save(raw_path)
        print(f"오려 냈다: {source.name} -> {raw_path} ({result.width}x{result.height})")
        print(f"      배경색 {report['backdrop']}, 걷어 낸 배경 {report['removed']:.1%}, 부드럽게 한 가장자리 {report['softened']:,}px")
        print(f"      이어서: gen_image.py --type {args.kind} --key {args.key} --refit")
    except PipelineError as error:
        print(f"\n실패: {error}", file=sys.stderr)
        return 1

    return 0


if __name__ == "__main__":
    sys.exit(main())
