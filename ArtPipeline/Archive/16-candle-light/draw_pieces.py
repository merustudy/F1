"""Drawn pieces of the candle light (round 16), no API call. White with an alpha ramp; the screen tints and stretches
them (UiPrefabSetup.Battle, CandleView). Written to ArtPipeline/output/ui_placeholder and copied to Assets/@Art/UI/Icon.
  .venv/bin/python ArtPipeline/Archive/16-candle-light/draw_pieces.py

- vignette: the screen's gloom, the red of death's door. Archive/09-battle-ui-feel/draw_pieces.py drew its ramp inside
  out (opaque in the middle, so the middle of the screen was the darkest); this one is clear in the middle and opaque
  towards the edges, as Architecture/12 and the approved Diablo mockup have it.
- candle_dark: the darkness around the candle's light, the upper half of a disc whose middle is the flame. Clear out to
  LIT of the full-dark reach, full from the reach on, eased between (smoothstep), as mockup B (mock_candle_light.py).
  The sprite spans twice the reach on every side of the flame so that it still covers the stage when the light pulls
  in: the screen stretches it to 4 x reach x wide by 2 x reach.
- candle_warm: the candle's warm light on the background, the upper half of a disc: full at the flame, gone at its rim
  (1 - smoothstep). The screen stretches it to 2 x reach x wide by reach.
"""
import math
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[3]; OUT = ROOT / "ArtPipeline" / "output" / "ui_placeholder"

LIT = 230 / 800          # mockup B: clear out to 230 px from the flame, full darkness from 800 px


def smoothstep(t):
    t = min(1.0, max(0.0, t))
    return t * t * (3.0 - 2.0 * t)


def vignette(size=512):
    """Clear in the middle, opaque towards the edges; the screen tints and stretches it."""
    im = Image.new("L", (size, size), 0)
    d = ImageDraw.Draw(im)
    c = size / 2
    steps = 64
    for i in range(steps):
        t = i / (steps - 1)
        r = (size * 0.78) * (1 - t)
        d.ellipse([c - r, c - r, c + r, c + r], fill=int(255 * ((1 - t) ** 1.6)))
    im = im.filter(ImageFilter.GaussianBlur(size / 24))
    out = Image.new("RGBA", (size, size), (255, 255, 255, 0))
    out.putalpha(im)
    return out


def half_disc(width, height, extent, alpha_of):
    """The upper half of a disc around the middle of the bottom edge, extent units on every side; alpha_of(distance).
    Rounded up, so that a pixel inside the disc is never fully clear: the importer trims a sprite to what is not fully
    clear, and a trimmed warm light would be stretched a little wider than it was drawn."""
    a = Image.new("L", (width, height), 0)
    px = a.load()
    for j in range(height):
        v = (height - j - 0.5) / height * extent
        for i in range(width):
            u = ((i + 0.5) / width * 2.0 - 1.0) * extent
            px[i, j] = math.ceil(255 * alpha_of((u * u + v * v) ** 0.5) - 1e-9)
    out = Image.new("RGBA", (width, height), (255, 255, 255, 0))
    out.putalpha(a)
    return out


def candle_dark(width=512, height=256):
    return half_disc(width, height, 2.0, lambda d: smoothstep((d - LIT) / (1.0 - LIT)))


def candle_warm(width=512, height=256):
    return half_disc(width, height, 1.0, lambda d: 1.0 - smoothstep(d))


if __name__ == "__main__":
    OUT.mkdir(parents=True, exist_ok=True)
    for name, image in (("vignette", vignette()), ("candle_dark", candle_dark()), ("candle_warm", candle_warm())):
        image.save(OUT / f"{name}.png")
        print("drawn:", name, image.size, "->", OUT / f"{name}.png")
