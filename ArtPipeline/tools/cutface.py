#!/usr/bin/env python3
"""Cuts the face of every approved figure out.

The game shows no faces now: the battle's board panel lost them with the V plan (2026-10-03), and the
faces and their wiring were taken out of the game on 2026-10-05 (Docs/Architecture/13_ART_PIPELINE.md
"얼굴"). This stays for a screen that wants faces again.

A face is a square cut from the top of a full-body figure, where the head is. The source is the
approved figure the game shows (Assets/@Art/Unit), never a candidate, so a face is as approved as
its figure. The cut is arithmetic: the square's side is a share of the figure's height, it is
centred on the topmost part of the figure (the head, as a rule), and the roster nudges it (FaceDx,
FaceDy, in the figure's pixels) where a hat or a weapon stands above the head. The face is written
at twice its size on screen, like an icon, and a review sheet shows every face at its size on the
colours of the two sides. No API call.

  cutface.py --type character          every row of the roster
  cutface.py --type enemy --only goblin_shaman
"""

import argparse
import csv
import re
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
REPO = ROOT.parent
ROSTERS = ROOT / "Rosters"
OUTPUT_DIR = ROOT / "output" / "face"
REVIEW = ROOT / "output" / "review" / "faces.png"

# Where the game keeps the approved figures of a type, and the folder its faces go to.
FIGURES = {
    "character": REPO / "Assets" / "@Art" / "Unit" / "Job",
    "enemy": REPO / "Assets" / "@Art" / "Unit" / "Enemy",
}
SIDES = {"character": "Party", "enemy": "Enemy"}

# The face was 60x60 on the old board panel (before the V plan) and is drawn at twice that.
FACE_SIZE = 120
# The square's side as a share of the figure's height (the head of a six-heads-tall figure, with
# some hair and shoulder), and how far above the topmost pixel it starts.
FACE_SHARE = 0.27
FACE_TOP_MARGIN = 0.015
# The band under the topmost pixel whose pixels say where the head is, as a share of the height.
HEAD_BAND = 0.06
ALPHA_FLOOR = 8

UI_PALETTE = REPO / "Assets" / "@Scripts" / "UI" / "UiPalette.cs"
LABEL_FONT = REPO / "Assets" / "@Fonts" / "Source" / "Pretendard" / "Pretendard-Medium.ttf"


def read_roster(kind: str) -> list:
    path = ROSTERS / f"{kind}.csv"
    if not path.is_file():
        raise SystemExit(f"실패: Roster 가 없다: {path}")
    with path.open(encoding="utf-8", newline="") as handle:
        return list(csv.DictReader(handle))


def nudge(row: dict, column: str) -> int:
    value = (row.get(column) or "").strip()
    if not value:
        return 0
    if not re.fullmatch(r"-?\d+", value):
        raise SystemExit(f"실패: Roster의 '{row['Key']}' 의 {column} 이 정수가 아니다: '{value}'")
    return int(value)


def cut_face(figure: Image.Image, dx: int, dy: int) -> Image.Image:
    """The square around the head: centred on the topmost pixels of the figure, nudged by the roster."""
    mask = figure.getchannel("A").point(lambda value: 255 if value > ALPHA_FLOOR else 0)
    box = mask.getbbox()
    if box is None:
        raise SystemExit("실패: 그림이 전부 투명하다.")

    left, top, right, bottom = box
    height = bottom - top
    band = mask.crop((left, top, right, top + max(2, int(height * HEAD_BAND))))
    xs = [x for x in range(band.width) for y in range(band.height) if band.getpixel((x, y))]
    centre = left + (sum(xs) / len(xs) if xs else band.width / 2)

    side = height * FACE_SHARE
    x0 = centre - side / 2 + dx
    y0 = top - height * FACE_TOP_MARGIN + dy
    crop = figure.crop((int(round(x0)), int(round(y0)), int(round(x0 + side)), int(round(y0 + side))))
    return crop.resize((FACE_SIZE, FACE_SIZE), Image.LANCZOS)


def palette() -> dict:
    pattern = re.compile(r"Color (\w+) = Rgb\(0x([0-9A-Fa-f]{2}), 0x([0-9A-Fa-f]{2}), 0x([0-9A-Fa-f]{2})\)")
    return {name: (int(r, 16), int(g, 16), int(b, 16)) for name, r, g, b in pattern.findall(UI_PALETTE.read_text(encoding="utf-8"))}


def review(faces: list, out: Path) -> None:
    """Every face at its size on screen and at twice that, on the plate colour of its side."""
    colours = palette()
    font = ImageFont.truetype(str(LABEL_FONT), 14) if LABEL_FONT.is_file() else ImageFont.load_default()
    cell, gap, label = FACE_SIZE + 60 + 16, 24, 22
    width = 24 * 2 + len(faces) * cell + (len(faces) - 1) * gap
    sheet = Image.new("RGB", (width, 24 * 2 + FACE_SIZE + label), colours["Background"])
    draw = ImageDraw.Draw(sheet)
    x = 24
    for kind, key, face in faces:
        colour = colours[SIDES[kind]]
        draw.rectangle([x, 24, x + 60 - 1, 24 + 60 - 1], fill=colour)
        sheet.paste(face.resize((60, 60), Image.LANCZOS), (x, 24), face.resize((60, 60), Image.LANCZOS))
        draw.rectangle([x + 60 + 16, 24, x + 60 + 16 + FACE_SIZE - 1, 24 + FACE_SIZE - 1], fill=colour)
        sheet.paste(face, (x + 60 + 16, 24), face)
        draw.text((x, 24 + FACE_SIZE + 4), key, font=font, fill=colours["TextDim"])
        x += cell + gap
    out.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(out)
    print(f"리뷰 시트: {out} ({sheet.width}x{sheet.height}, 얼굴 {len(faces)}개)")


def parse_args():
    parser = argparse.ArgumentParser(description="Cut the faces out of the approved figures. No API call.")
    parser.add_argument("--type", dest="kinds", action="append", choices=sorted(FIGURES), required=True,
                        help="Roster to cut faces for; may be given twice.")
    parser.add_argument("--only", default="", help="Comma separated keys. Default: every row of the roster.")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    wanted = {key.strip() for key in args.only.split(",") if key.strip()}
    faces = []
    for kind in args.kinds:
        for row in read_roster(kind):
            key = row["Key"]
            if wanted and key not in wanted:
                continue
            source = FIGURES[kind] / f"{key}.png"
            if not source.is_file():
                raise SystemExit(f"실패: 확정된 그림이 없다: {source}")
            face = cut_face(Image.open(source).convert("RGBA"), nudge(row, "FaceDx"), nudge(row, "FaceDy"))
            OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
            out = OUTPUT_DIR / f"{key}.png"
            face.save(out)
            print(f"{kind}/{key}: {source.name} -> {out} ({FACE_SIZE}x{FACE_SIZE})")
            faces.append((kind, key, face))

    if not faces:
        raise SystemExit("실패: 자를 그림이 없다.")
    review(faces, REVIEW)
    return 0


if __name__ == "__main__":
    sys.exit(main())
