"""Drawn pieces of the Diablo kit (round 14), no API call: the plain bone cells of the inventory grid (the user asked for
cells without ornament), the chosen cell in gold, the candle's molten top, a smoke wisp for the burnt-out candle and an
iron chain. Twice the screen size (Pixels Per Unit 200).  .venv/bin/python ArtPipeline/Archive/14-ui-diablo/draw_pieces.py
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[3]; OUT = ROOT / "ArtPipeline" / "output" / "ui_placeholder"; AA = 4
INK = (0x18, 0x09, 0x07, 255); BONE = (214, 200, 170, 255); BONE_LINE = (72, 60, 50, 255); GOLD = (0xB8, 0x94, 0x4E, 255)
WAX = (236, 226, 206, 255); WAX_D = (214, 198, 168, 255); IRON = (96, 94, 100, 255); IRON_D = (46, 44, 50, 255)

def cell(fill, size=(360, 100), line=4, radius=10):
    """A plain cell: one flat fill with a thin dark line, rounded a little. Stretched as a nine-slice (UiArt border 16)."""
    w, h = size[0] * AA, size[1] * AA; im = Image.new("RGBA", (w, h), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    d.rounded_rectangle([0, 0, w - 1, h - 1], radius=radius * AA, fill=BONE_LINE); d.rounded_rectangle([line * AA, line * AA, w - 1 - line * AA, h - 1 - line * AA], radius=(radius - line) * AA, fill=fill)
    return im.resize(size, Image.LANCZOS)

def candle_top(size=(112, 40)):
    """The molten rim of the candle at its current height: an ellipse of wax with a darker pool inside and a dark edge."""
    w, h = size[0] * AA, size[1] * AA; im = Image.new("RGBA", (w, h), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    d.ellipse([0, 0, w - 1, h - 1], fill=INK); d.ellipse([3 * AA, 3 * AA, w - 1 - 3 * AA, h - 1 - 3 * AA], fill=WAX); d.ellipse([14 * AA, 10 * AA, w - 1 - 14 * AA, h - 1 - 9 * AA], fill=WAX_D)
    return im.resize(size, Image.LANCZOS)

def smoke(size=(60, 140)):
    """A wisp of grey smoke rising and thinning: a wavy band, blurred, fading towards the top."""
    w, h = size[0] * AA, size[1] * AA; im = Image.new("RGBA", (w, h), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    import math
    pts = [(w / 2 + math.sin(y / h * math.pi * 2.2) * w * 0.22, y) for y in range(h, 0, -AA * 2)]
    for i, (x, y) in enumerate(pts):
        t = 1 - y / h; r = (6 + 14 * t) * AA; a = int(190 * (1 - t) ** 1.2); d.ellipse([x - r, y - r, x + r, y + r], fill=(170, 170, 176, a))
    return im.filter(ImageFilter.GaussianBlur(AA * 3)).resize(size, Image.LANCZOS)

def chain(width=40, height=240, links=6):
    w, h = width * AA, height * AA; im = Image.new("RGBA", (w, h), (0, 0, 0, 0)); d = ImageDraw.Draw(im); pitch = h / links; rx, ry = w * 0.36, pitch * 0.62
    def ring(cx, cy, a, b, color):
        ti = 2.5 * AA; tb = min(a, b) * 0.42; d.ellipse([cx - a, cy - b, cx + a, cy + b], fill=INK); d.ellipse([cx - a + ti, cy - b + ti, cx + a - ti, cy + b - ti], fill=color)
        ha, hb = a - ti - tb, b - ti - tb
        if ha > 2 * AA and hb > 2 * AA:
            d.ellipse([cx - ha, cy - hb, cx + ha, cy + hb], fill=INK)
            if ha - ti > AA and hb - ti > AA: d.ellipse([cx - ha + ti, cy - hb + ti, cx + ha - ti, cy + hb - ti], fill=(0, 0, 0, 0))
    for i in range(links):
        cy = pitch * (i + 0.5); wide = i % 2 == 0; ring(w / 2, cy, rx if wide else rx * 0.5, ry, IRON if wide else IRON_D)
    return im.resize((width, height), Image.LANCZOS)

if __name__ == "__main__":
    OUT.mkdir(parents=True, exist_ok=True)
    cell(BONE).save(OUT / "slot.png"); cell(GOLD).save(OUT / "slot_selected.png"); candle_top().save(OUT / "candle_top.png"); smoke().save(OUT / "smoke.png"); chain().save(OUT / "chain.png")
    print("drawn: slot, slot_selected, candle_top, smoke, chain ->", OUT)
