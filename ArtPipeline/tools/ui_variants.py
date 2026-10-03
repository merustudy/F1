#!/usr/bin/env python3
"""Makes the frames the game uses from the fitted frames: each in one flat color, its own or another.

A frame is generated once. The colors it comes in (a side's plate, a selected slot, the white
button the game tints) are made here by giving its fill another color, so that every variant has
the same shape, outline and details. Rosters/ui_variant.csv says which: Key (the name of the
sprite), Source (a key of Rosters/ui_frame.csv), Fill (#RRGGBB, or empty for the frame's own
color). No API call is made.
"""

import argparse
import csv
import re
import sys
from pathlib import Path

from PIL import Image

from gen_image import OUTPUT_DIR, ROSTERS, PipelineError, flatten_fill, require_file

SOURCE_DIR = OUTPUT_DIR / "ui_frame"
VARIANT_DIR = OUTPUT_DIR / "ui_variant"
ROSTER = ROSTERS / "ui_variant.csv"
KEY_PATTERN = re.compile(r"[a-z][a-z0-9]*(_[a-z0-9]+)*")
FILL_PATTERN = re.compile(r"#([0-9A-Fa-f]{6})")


def read_variants() -> list:
    """Every row of the roster, checked: a name of its own, a fitted source and a color that parses."""
    require_file(ROSTER, f"Roster {ROSTER.name}")
    with ROSTER.open(encoding="utf-8", newline="") as handle:
        rows = list(csv.DictReader(handle))

    variants = []
    seen = set()
    for number, row in enumerate(rows, start=2):
        key = (row.get("Key") or "").strip()
        source = (row.get("Source") or "").strip()
        fill = (row.get("Fill") or "").strip()
        where = f"{ROSTER.name}({number})"
        if not KEY_PATTERN.fullmatch(key):
            raise PipelineError(f"{where}: Key '{key}' 는 영어 snake_case 여야 한다.")
        if key in seen:
            raise PipelineError(f"{where}: Key '{key}' 가 두 번 나온다.")
        seen.add(key)

        target = None
        if fill:
            match = FILL_PATTERN.fullmatch(fill)
            if not match:
                raise PipelineError(f"{where}: Fill '{fill}' 은 #RRGGBB 이거나 비어 있어야 한다.")
            target = tuple(int(match.group(1)[i:i + 2], 16) for i in (0, 2, 4))

        path = SOURCE_DIR / f"{source}.png"
        require_file(path, f"{where} 의 Source '{source}' 의 맞춘 그림")
        variants.append((key, path, target))
    return variants


def main() -> int:
    parser = argparse.ArgumentParser(description="Make the color variants of the fitted UI frames. No API call.")
    parser.add_argument("--only", default="", help="Comma separated keys. Default: every row of the roster.")
    args = parser.parse_args()
    only = {key for key in args.only.split(",") if key}

    try:
        variants = read_variants()
        unknown = only - {key for key, _, _ in variants}
        if unknown:
            raise PipelineError(f"Roster에 없는 Key: {', '.join(sorted(unknown))}")

        VARIANT_DIR.mkdir(parents=True, exist_ok=True)
        for key, source, target in variants:
            if only and key not in only:
                continue
            image = flatten_fill(Image.open(source).convert("RGBA"), target)
            output = VARIANT_DIR / f"{key}.png"
            image.save(output, format="PNG")
            color = "틀의 색 그대로" if target is None else "#%02X%02X%02X" % target
            print(f"{key}: {source.name} -> {output.name} ({image.width}x{image.height}, {color})")
    except PipelineError as error:
        print(f"\n실패: {error}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
