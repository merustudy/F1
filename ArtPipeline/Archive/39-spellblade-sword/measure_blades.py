"""Measures the swords (round 39), no API call: how long and how broad each blade is drawn, in the pixels of its raw, inside an
oriented band along the blade (guard end to tip, given by hand), counting the light grey steel. Also the figure's height, so that
the lengths can be compared at the game's size (every figure is fitted to the same canvas height).
  .venv/bin/python ArtPipeline/Archive/39-spellblade-sword/measure_blades.py
"""
import math
import sys
from pathlib import Path
from PIL import Image

R = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(R / "ArtPipeline" / "tools"))
from gen_image import subject_box  # noqa: E402

OUT = R / "ArtPipeline/output/character"
# (name, raw, guard end of the blade, tip) — the two points read off the raw by eye.
SWORDS = [
    ("knight", OUT / "knight.raw.png", (640, 385), (990, 110)),
    ("spellblade (old, red)", OUT / "spellblade.raw.png", (395, 530), (965, 700)),
    ("longsword 1", OUT / "spellblade_longsword.raw.png", (330, 535), (970, 715)),
    ("longsword 2", OUT / "spellblade_longsword2.raw.png", (340, 540), (985, 745)),
    ("longsword 3", OUT / "spellblade_longsword3.raw.png", (355, 540), (690, 675)),
    ("longsword 4 (3, 날을 늘림)", OUT / "spellblade_longsword4.raw.png", (355, 540), (801, 720)),
]
HALF = 45
FAR = 250                       # the breadth is read beyond this point along the blade, clear of the hands and the knight's pauldron


def steel(px):
    r, g, b, a = px
    return a > 128 and abs(r - g) < 18 and abs(g - b) < 22 and r > 120


def measure(path, guard, tip):
    im = Image.open(path).convert("RGBA")
    px = im.load()
    gx, gy = guard
    dx, dy = tip[0] - gx, tip[1] - gy
    length_axis = math.hypot(dx, dy)
    ux, uy = dx / length_axis, dy / length_axis
    ts, ns = [], []
    x0, x1 = max(0, min(gx, tip[0]) - HALF), min(im.width, max(gx, tip[0]) + HALF)
    y0, y1 = max(0, min(gy, tip[1]) - HALF), min(im.height, max(gy, tip[1]) + HALF)
    for y in range(y0, y1):
        for x in range(x0, x1):
            t = (x - gx) * ux + (y - gy) * uy
            n = -(x - gx) * uy + (y - gy) * ux
            if -40 <= t <= length_axis + 60 and abs(n) <= HALF and steel(px[x, y]):
                ts.append(t)
                ns.append(n)
    if len(ts) < 200:
        return None
    far = sorted(n for t, n in zip(ts, ns) if t > FAR)
    ts.sort()
    length = ts[int(len(ts) * 0.998)] - max(0.0, ts[int(len(ts) * 0.002)])
    breadth = far[int(len(far) * 0.97)] - far[int(len(far) * 0.03)] if len(far) > 50 else float("nan")
    box = subject_box(im)
    return length, breadth, box[3] - box[1]


if __name__ == "__main__":
    rows = []
    for name, path, guard, tip in SWORDS:
        if not path.exists():
            continue
        measured = measure(path, guard, tip)
        if measured is None:
            print(f"{name}: no steel (a red blade)")
            continue
        length, breadth, height = measured
        fitted = 806 / height          # the figure is fitted 806 tall (Height 90 of 896)
        rows.append((name, length, breadth, height, length * fitted, breadth * fitted))
        print(f"{name}: figure {height}px tall; blade {length:.0f} x {breadth:.0f}px in the raw -> {length * fitted:.0f} x {breadth * fitted:.0f}px on the fitted canvas")
