"""Moves the point of a straight blade along its axis, locally, no API call (round 39): out to lengthen, in to shorten.
  .venv/bin/python ArtPipeline/Archive/39-spellblade-sword/shift_blade_tip.py <raw.png> <out.png> gx,gy tx,ty <shift px> [tip piece px]

The blade is a straight, even shape, so its point (the last TIP pixels along the axis) can be moved without redrawing anything
else: moved out, the gap is filled with a copy of the straight stretch just before the point; moved in, the steel beyond the new
point is cleared (it lies over nothing: a blade's end reaches past the body) and the point is laid over the straight blade. Only
pixels of the blade and its ink outline move (a mask of the grey steel grown by the outline's width), by a whole number of pixels
along the axis, so nothing is resampled. gx,gy is the blade's end at the guard and tx,ty its point, read off the raw by eye.
Round 39 used it twice: the fourth figure (+120) and the attack pose (-80, whose point reached past the pose canvas).
"""
import math
import sys
from pathlib import Path
from PIL import Image, ImageFilter

HALF = 60
OUTLINE = 6


def main():
    source, target = Path(sys.argv[1]), Path(sys.argv[2])
    gx, gy = (int(v) for v in sys.argv[3].split(","))
    tx, ty = (int(v) for v in sys.argv[4].split(","))
    shift = float(sys.argv[5])
    tip_piece = int(sys.argv[6]) if len(sys.argv) > 6 else 110

    image = Image.open(source).convert("RGBA")
    px = image.load()
    dx, dy = tx - gx, ty - gy
    axis = math.hypot(dx, dy)
    ux, uy = dx / axis, dy / axis
    # A whole-pixel step along the axis, as close to the shift as the axis allows.
    step = (round(shift * ux), round(shift * uy))
    moved = math.copysign(math.hypot(*step), shift)

    steel = Image.new("L", image.size, 0)
    sp = steel.load()
    ts = []
    x0, x1 = max(0, min(gx, tx) - HALF), min(image.width, max(gx, tx) + HALF)
    y0, y1 = max(0, min(gy, ty) - HALF), min(image.height, max(gy, ty) + HALF)
    for y in range(y0, y1):
        for x in range(x0, x1):
            r, g, b, a = px[x, y]
            t = (x - gx) * ux + (y - gy) * uy
            n = -(x - gx) * uy + (y - gy) * ux
            if a > 128 and abs(r - g) < 18 and abs(g - b) < 22 and r > 120 and -10 <= t <= axis + 40 and abs(n) <= HALF:
                sp[x, y] = 255
                ts.append(t)
    ts.sort()
    length = ts[int(len(ts) * 0.998)]
    blade = steel.filter(ImageFilter.MaxFilter(2 * OUTLINE + 1))
    bp = blade.load()

    def along(x, y):
        return (x - gx) * ux + (y - gy) * uy

    def piece(t_from, t_to):
        mask = Image.new("L", image.size, 0)
        mp = mask.load()
        for y in range(image.height):
            for x in range(image.width):
                if bp[x, y] and t_from <= along(x, y) <= t_to:
                    mp[x, y] = 255
        layer = Image.new("RGBA", image.size, (0, 0, 0, 0))
        layer.paste(image, (0, 0), mask)
        return layer

    out = image.copy()
    tip = piece(length - tip_piece, length + 40)
    if moved > 0:
        fill = piece(length - tip_piece - moved, length - tip_piece)
        out.alpha_composite(fill, step)
        out.alpha_composite(tip, step)
    else:
        # Clear the blade beyond where the point will end, then lay the point over the straight blade.
        op = out.load()
        for y in range(image.height):
            for x in range(image.width):
                if bp[x, y] and along(x, y) > length + moved - tip_piece:
                    op[x, y] = (0, 0, 0, 0)
        # What the clearing took from under the point's new place (the straight blade there) the point itself brings back.
        out.alpha_composite(tip, step)
    out.save(target)
    print(f"blade {length:.0f}px -> {length + moved:.0f}px along the axis; the point moved by {step} -> {target}")


if __name__ == "__main__":
    main()
