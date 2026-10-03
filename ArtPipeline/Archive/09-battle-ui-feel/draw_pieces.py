#!/usr/bin/env python3
"""Drawn placeholder pieces for the battle's presentation: the storm clock's dial and ring, and
the vignette the storm's dusk and death's door tint over the stage. No API call. They go to
Assets/@Art/UI (UiArt: Dial, Ring, Vignette) at twice the screen size (Pixels Per Unit 200).

  .venv/bin/python ArtPipeline/Archive/09-battle-ui-feel/draw_pieces.py
"""
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / "ArtPipeline" / "output" / "ui_placeholder"
AA = 4

NAVY = (0x1E, 0x22, 0x2B, 255)
NAVY_DEEP = (0x15, 0x18, 0x1F, 255)
BRASS = (0xB8, 0x94, 0x4E, 255)
INK = (0x18, 0x09, 0x07, 255)


def disc(draw, cx, cy, r, fill, outline=None, width=0):
    draw.ellipse([cx - r, cy - r, cx + r, cy + r], fill=fill, outline=outline, width=width)


def dial(size=400):
    """The face of the clock: a dark disc with a brass rim and a thinner inner ring, outlined in ink."""
    s = size * AA
    im = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    c = s / 2
    disc(d, c, c, s / 2 - 2 * AA, NAVY_DEEP, INK, 6 * AA)
    disc(d, c, c, s / 2 - 14 * AA, NAVY_DEEP, BRASS, 5 * AA)
    disc(d, c, c, s / 2 - 48 * AA, NAVY, INK, 3 * AA)
    # twelve small ticks on the brass rim
    for i in range(12):
        import math
        a = math.radians(i * 30 - 90)
        r0, r1 = s / 2 - 36 * AA, s / 2 - 26 * AA
        d.line([(c + r0 * math.cos(a), c + r0 * math.sin(a)), (c + r1 * math.cos(a), c + r1 * math.sin(a))], fill=BRASS, width=4 * AA)
    return im.resize((size, size), Image.LANCZOS)


def ring(size=400, outer_radius=186, inner_radius=162):
    """A flat white ring (the screen colors it) with a thin ink edge, lying on the dial's brass rim.

    The generated dial (476 px, fitted) has its brass rim from radius 189 to 159; both are drawn
    at twice the screen size, so the ring sits a little inside the rim and the brass shows at
    both of its edges."""
    s = size * AA
    im = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    c = s / 2
    outer = outer_radius * AA
    inner = inner_radius * AA
    disc(d, c, c, outer, INK)
    disc(d, c, c, outer - 3 * AA, (255, 255, 255, 255))
    disc(d, c, c, inner + 3 * AA, INK)
    disc(d, c, c, inner, (0, 0, 0, 0))
    return im.resize((size, size), Image.LANCZOS)


def vignette(size=512):
    """White, transparent in the middle and opaque at the edges; the screen tints and stretches it."""
    im = Image.new("L", (size, size), 0)
    d = ImageDraw.Draw(im)
    c = size / 2
    steps = 64
    for i in range(steps):
        t = i / (steps - 1)
        r = (size * 0.78) * (1 - t)
        value = int(255 * (t ** 1.6))
        d.ellipse([c - r, c - r, c + r, c + r], fill=value)
    im = im.filter(ImageFilter.GaussianBlur(size / 24))
    out = Image.new("RGBA", (size, size), (255, 255, 255, 0))
    out.putalpha(im)
    return out


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    # The dial drawn here was the placeholder before the generated one (ui_piece "dial"); it is kept for the record.
    for name, image in (("dial", dial()), ("ring", ring()), ("vignette", vignette())):
        path = OUT / f"{name}.png"
        image.save(path)
        print("wrote", path, image.size)


if __name__ == "__main__":
    main()
