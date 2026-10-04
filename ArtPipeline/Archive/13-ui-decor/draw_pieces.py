"""Drawn pieces of the camp kit (round 13) that need no API call: the brass chain the storm clock
hangs from and the warm glow behind the lantern. They go to Assets/@Art/UI/Icon at twice the screen
size (Pixels Per Unit 200).  .venv/bin/python ArtPipeline/Archive/13-ui-decor/draw_pieces.py
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / "ArtPipeline" / "output" / "ui_placeholder"
AA = 4
BRASS = (0xB8, 0x94, 0x4E, 255); BRASS_D = (0x7A, 0x5E, 0x2A, 255); INK = (0x18, 0x09, 0x07, 255)

def ring(d, cx, cy, a, b, color):
    """One oval link: an ink rim, a band of brass and a hole, each inset in proportion to the link's size."""
    ti = 2.5 * AA; tb = min(a, b) * 0.42
    d.ellipse([cx - a, cy - b, cx + a, cy + b], fill=INK)
    d.ellipse([cx - a + ti, cy - b + ti, cx + a - ti, cy + b - ti], fill=color)
    ha, hb = a - ti - tb, b - ti - tb
    if ha > 2 * AA and hb > 2 * AA:
        d.ellipse([cx - ha, cy - hb, cx + ha, cy + hb], fill=INK)
        if ha - ti > AA and hb - ti > AA:
            d.ellipse([cx - ha + ti, cy - hb + ti, cx + ha - ti, cy + hb - ti], fill=(0, 0, 0, 0))

def chain(width=40, height=240, links=6):
    """A vertical chain of oval brass links, every other one seen edge-on, overlapping like a real chain."""
    w, h = width * AA, height * AA
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    pitch = h / links; rx, ry = w * 0.36, pitch * 0.62
    for i in range(links):
        cy = pitch * (i + 0.5); cx = w / 2; wide = i % 2 == 0
        ring(d, cx, cy, rx if wide else rx * 0.5, ry, BRASS if wide else BRASS_D)
    return im.resize((width, height), Image.LANCZOS)

def glow(size=260):
    """A soft warm disc that fades to nothing: the lantern's light on the table. The screen tints and fades it."""
    s = size * AA
    im = Image.new("RGBA", (s, s), (255, 255, 255, 0)); d = ImageDraw.Draw(im)
    for i in range(24):
        r = s / 2 * (1 - i / 24); alpha = int(255 * (i / 24) ** 1.6)
        d.ellipse([s / 2 - r, s / 2 - r, s / 2 + r, s / 2 + r], fill=(255, 255, 255, alpha))
    return im.filter(ImageFilter.GaussianBlur(AA * 6)).resize((size, size), Image.LANCZOS)

if __name__ == "__main__":
    OUT.mkdir(parents=True, exist_ok=True)
    chain().save(OUT / "chain.png"); glow().save(OUT / "glow.png"); print("drawn:", OUT / "chain.png", OUT / "glow.png")
