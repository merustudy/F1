"""The head size of a state pose against its approved figure, read from the eyes. No API call.
  .venv/bin/python ArtPipeline/Archive/40-state-poses/measure_eyes.py [--jobs knight,bishop]
The distance between the centres of the two eye whites (the white areas inside the face's skin area, the two largest) does not
change with the pose, the expression or the hair, only a little with the turn of the head, which the pictures share (three-quarter
view to the right): its ratio pose/approved is the scale fit_pose needs for the head to keep its size (round 40: the gold discs
and the weapons were drawn smaller than the body in several poses, so the disc rule stood the body too big).
"""
import argparse
import colorsys
import math
import sys
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / "ArtPipeline" / "tools"))
import fit_pose as F  # noqa: E402
from gen_image import subject_box  # noqa: E402


def skin(r, g, b):
    h, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
    return (h <= 0.11 or h >= 0.97) and 0.12 <= s <= 0.55 and v >= 0.62


def white(r, g, b):
    h, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
    return v >= 0.84 and s <= 0.13


def face_box(image):
    box = subject_box(image)
    limit = box[1] + (box[3] - box[1]) * 0.40
    best = None
    for pts in F.blobs(image, skin, 300, 10 ** 7):
        n = len(pts)
        if sum(p[1] for p in pts) / n > limit:
            continue
        if best is None or n > best[0]:
            xs = [p[0] for p in pts]
            ys = [p[1] for p in pts]
            best = (n, (min(xs) - 10, min(ys) - 10, max(xs) + 10, max(ys) + 10))
    return best[1]


def eyes(image):
    """((x, y), (x, y), sizes) of the two largest white areas inside the face's box, or None."""
    fb = face_box(image)
    found = []
    for pts in F.blobs(image.crop(fb), white, 20, 1500):
        n = len(pts)
        xs = [p[0] for p in pts]
        ys = [p[1] for p in pts]
        if max(xs) - min(xs) > 70 or max(ys) - min(ys) > 70:
            continue
        found.append((n, fb[0] + sum(xs) / n, fb[1] + sum(ys) / n))
    found.sort(reverse=True)
    if len(found) < 2:
        return None
    a, b = found[0], found[1]
    return (round(a[1]), round(a[2])), (round(b[1]), round(b[2])), (a[0], b[0]), math.hypot(a[1] - b[1], a[2] - b[2])


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--jobs", default="valkyrie,knight,bishop,paladin,archmage,spellblade")
    args = parser.parse_args()
    for job in args.jobs.split(","):
        approved = Image.open(F.OUTPUT / "character" / f"{job}.raw.png").convert("RGBA")
        ea = eyes(approved)
        print(f"{job}: approved eyes {ea}")
        for state in ("broken", "resolute"):
            path = F.OUTPUT / "hit" / f"{job}_{state}.raw.png"
            if not path.is_file():
                continue
            ep = eyes(Image.open(path).convert("RGBA"))
            if ea and ep:
                print(f"   {state:9s} eyes {ep[0]} {ep[1]} sizes {ep[2]} distance {ep[3]:.1f} -> scale {ep[3] / ea[3]:.3f}")
            else:
                print(f"   {state:9s} eyes {ep}")


if __name__ == "__main__":
    main()
