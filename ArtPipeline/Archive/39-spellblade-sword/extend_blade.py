"""Lengthens the blade of the third candidate to the knight's length, locally, no API call (round 39).
  .venv/bin/python ArtPipeline/Archive/39-spellblade-sword/extend_blade.py

The third generation drew the right sword (broad, the knight's fittings) but short: its blade measures 283 against the knight's 372
on the fitted canvas. A blade is a straight, even shape, so it can be made longer without redrawing anything else: the pointed tip
(the last T pixels along the blade) is moved S pixels further out along the blade's axis, and the gap is filled with a copy of the
straight stretch just before it. Only pixels of the blade and its ink outline move (a mask of the grey steel grown by the outline's
width); whatever the longer blade now covers (the leg behind it) is covered as the drawn blade covers it. The shift is a whole
number of pixels, so nothing is resampled. Writes output/character/spellblade_longsword4.raw.png; the fit is gen_image.py --refit.
"""
import math
import sys
from pathlib import Path
from PIL import Image, ImageFilter

R = Path(__file__).resolve().parents[3]
OUT = R / "ArtPipeline/output/character"
SOURCE = OUT / "spellblade_longsword3.raw.png"
TARGET = OUT / "spellblade_longsword4.raw.png"
GUARD, TIP = (355, 540), (690, 675)      # the blade's axis in the third candidate, read off by eye
HALF = 60                                 # half the width of the band the blade lies in
TIP_PIECE = 110                           # how much of the end moves out: the point and a little straight blade
SHIFT = (111, 45)                         # how far the point moves, along the axis, in whole pixels (about 120)
OUTLINE = 6                               # the ink line around the steel, in pixels


def main():
    image = Image.open(SOURCE).convert("RGBA")
    px = image.load()
    gx, gy = GUARD
    dx, dy = TIP[0] - gx, TIP[1] - gy
    axis = math.hypot(dx, dy)
    ux, uy = dx / axis, dy / axis

    # The steel of the blade, and the distance of every steel pixel along the axis.
    steel = Image.new("L", image.size, 0)
    sp = steel.load()
    ts = []
    for y in range(max(0, min(gy, TIP[1]) - HALF), min(image.height, max(gy, TIP[1]) + HALF)):
        for x in range(max(0, gx - 20), min(image.width, TIP[0] + HALF)):
            r, g, b, a = px[x, y]
            t = (x - gx) * ux + (y - gy) * uy
            n = -(x - gx) * uy + (y - gy) * ux
            if a > 128 and abs(r - g) < 18 and abs(g - b) < 22 and r > 120 and -10 <= t <= axis + 30 and abs(n) <= HALF:
                sp[x, y] = 255
                ts.append(t)
    ts.sort()
    length = ts[int(len(ts) * 0.998)]
    blade = steel.filter(ImageFilter.MaxFilter(2 * OUTLINE + 1))   # the steel and its ink outline
    bp = blade.load()

    def piece(t_from, t_to):
        """The blade's pixels between two distances along the axis, as a masked copy."""
        mask = Image.new("L", image.size, 0)
        mp = mask.load()
        for y in range(image.height):
            for x in range(image.width):
                if bp[x, y]:
                    t = (x - gx) * ux + (y - gy) * uy
                    if t_from <= t <= t_to:
                        mp[x, y] = 255
        layer = Image.new("RGBA", image.size, (0, 0, 0, 0))
        layer.paste(image, (0, 0), mask)
        return layer

    shift = math.hypot(*SHIFT)
    tip = piece(length - TIP_PIECE, length + 40)
    fill = piece(length - TIP_PIECE - shift, length - TIP_PIECE)
    out = image.copy()
    out.alpha_composite(fill, SHIFT)      # the straight stretch, moved out: it covers the old point
    out.alpha_composite(tip, SHIFT)       # the point, moved out
    out.save(TARGET)
    print(f"blade {length:.0f}px along the axis -> about {length + shift:.0f}px; the point moved by {SHIFT} -> {TARGET.relative_to(R)}")


if __name__ == "__main__":
    main()
