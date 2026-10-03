#!/usr/bin/env python3
"""The bag behind a unit's item cells, as a drawn placeholder: a leather slab with a dark rim and a
seam inside it, the shape the user approved in the mockup (mock_panel_cells.py, draw_bag). No API call.

The sprite is a nine-slice: 128x128 at twice the screen size (Pixels Per Unit 200), border 40, so
the corner (radius 24, the rim and the seam's curve) stays and the middle stretches to the board's
length. It is copied to Assets/@Art/UI/Frame/bag.png and named in UiArt (Border 40).

  .venv/bin/python ArtPipeline/Archive/08-panel-cells/draw_bag.py
"""
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / "ArtPipeline" / "output" / "ui_frame" / "bag.png"

SIZE = 128          # sprite px (2x screen)
RADIUS = 24
RIM = 6
SEAM_INSET = 8
SEAM = 3
LEATHER = (0x5C, 0x3F, 0x2C, 255)
LEATHER_DARK = (0x2A, 0x1B, 0x12, 255)
LEATHER_SEAM = (0x8E, 0x6A, 0x47, 200)
AA = 4              # drawn this many times larger, then shrunk, for smooth edges


def main():
    s = SIZE * AA
    im = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rounded_rectangle([0, 0, s - 1, s - 1], radius=RADIUS * AA, fill=LEATHER, outline=LEATHER_DARK, width=RIM * AA)
    inset = SEAM_INSET * AA
    d.rounded_rectangle([inset, inset, s - 1 - inset, s - 1 - inset], radius=(RADIUS - SEAM_INSET) * AA, outline=LEATHER_SEAM, width=SEAM * AA)
    out = im.resize((SIZE, SIZE), Image.LANCZOS)
    OUT.parent.mkdir(parents=True, exist_ok=True)
    out.save(OUT)
    print("wrote", OUT, out.size)


if __name__ == "__main__":
    main()
