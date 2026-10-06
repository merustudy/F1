"""Measures of a state pose's size against its approved figure that do not depend on the gear: the face and the feet. No API call.
  .venv/bin/python ArtPipeline/Archive/40-state-poses/measure_body.py
Round 40 found the gold discs and the blades drawn smaller than the body in some poses (the knight's broken pose: discs 0.80, the body
about 0.95), so fit_pose's disc rule would stand the body too big; the user's ask is the same face and head size as the idle.
- face: the largest skin-coloured area whose centre lies in the top 30% of the figure; its area's square root and its width,
  against the approved figure's (the hair over the brow in a broken pose makes it read a little small).
- feet: the horizontal span of the back boot's leather columns (fit_pose.feet), against the approved figure's.
"""
import colorsys
import math
import sys
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / "ArtPipeline" / "tools"))
import fit_pose as F  # noqa: E402
from gen_image import subject_box  # noqa: E402

JOBS = ("valkyrie", "knight", "bishop", "paladin", "archmage", "spellblade")


def skin(r, g, b):
    h, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
    return (h <= 0.11 or h >= 0.97) and 0.12 <= s <= 0.55 and v >= 0.62


def face(image):
    box = subject_box(image)
    top = box[1] + (box[3] - box[1]) * 0.30
    best = None
    for pts in F.blobs(image, skin, 300, 10 ** 7):
        n = len(pts)
        if sum(p[1] for p in pts) / n > top:
            continue
        xs = [p[0] for p in pts]
        if best is None or n > best[0]:
            best = (n, max(xs) - min(xs) + 1)
    return best


def foot_span(image, leather):
    _, _, groups = F.feet(image, leather)
    return len(groups[0])


def main():
    for job in JOBS:
        approved = Image.open(F.OUTPUT / "character" / f"{job}.raw.png").convert("RGBA")
        leather = F.boot_colour(approved)
        fa, sa = face(approved), foot_span(approved, leather)
        print(f"{job}: approved face area {fa[0]} width {fa[1]}, back foot span {sa}")
        for state in ("broken", "resolute"):
            path = F.OUTPUT / "hit" / f"{job}_{state}.raw.png"
            if not path.is_file():
                continue
            raw = Image.open(path).convert("RGBA")
            fp = face(raw)
            try:
                sp = foot_span(raw, leather)
                feet_ratio = f"{sp / sa:.3f} ({sp})"
            except SystemExit:
                feet_ratio = "none"
            da, dp = F.discs(approved), F.discs(raw)
            k = min(F.DISCS, len(da), len(dp))
            disc = f"{F.median(dp[:k]) / F.median(da[:k]):.3f}" if k >= 2 else "none"
            print(f"   {state:9s} face sqrt-area {math.sqrt(fp[0] / fa[0]):.3f} width {fp[1] / fa[1]:.3f} ({fp[0]}, {fp[1]}) | feet {feet_ratio} | discs {disc}")


if __name__ == "__main__":
    main()
