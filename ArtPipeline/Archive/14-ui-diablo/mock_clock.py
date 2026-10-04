"""Four storm-clock alternatives for the Diablo concept (round 14), drawn on the D2 battle mockup. The blood orb reads
as health, so the clock must not look like a vitals orb: S1 the orb filled with storm-coloured light instead of blood,
S2 an hourglass whose sand runs out, S3 a round iron storm rune that fills as a ring (the ring the game has now, in iron
and gold), S4 a candle burning down with a skull holder. Drawn, no API call.
  .venv/bin/python ArtPipeline/Archive/14-ui-diablo/mock_clock.py
"""
import math
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageFilter

OUT = Path(__file__).resolve().parent; S = OUT / "samples"; ROOT = OUT.parents[2]
FONT = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"; STORM_ICON = ROOT / "Assets/@Art/UI/Icon/storm.png"
INK = (16, 10, 10, 255); IRON = (46, 44, 50, 255); IRON_L = (96, 94, 100, 255); IRON_D = (28, 26, 30, 255)
GOLD = (186, 146, 60, 255); GOLD_D = (120, 92, 34, 255); BONE = (214, 200, 170, 255); BONE_D = (160, 146, 118, 255)
STORM = (120, 170, 220, 255); STORM_L = (200, 226, 250, 255); STORM_D = (54, 84, 130, 255); FLAME = (255, 196, 90, 255)
CX, CY = 960, 740; FILL = 0.35; TIME = "7.3초"; WORDS = "폭풍까지 37.7초"

def font(size): return ImageFont.truetype(str(FONT), size)
def label(d, xy, s, size, color=BONE, anchor="mm"): d.text(xy, s, font=font(size), fill=color, anchor=anchor)

def clear(img, src):
    """Puts the plain stone panel back where the blood orb was drawn (the mockup's panel texture, from beside it)."""
    patch = src.crop((1100, 626, 1100 + 320, 626 + 320)); img.alpha_composite(patch, (CX - 160, 626))
    img.alpha_composite(src.crop((1100, 946, 1420, 1000)), (CX - 160, 946))

def chain(d, x, y0, y1):
    for i, y in enumerate(range(y0, y1, 16)):
        a = 7 if i % 2 == 0 else 4; d.ellipse([x - a, y, x + a, y + 18], outline=IRON_L, width=4); d.ellipse([x - a, y, x + a, y + 18], outline=INK, width=1)

def storm_mark(img, d, cx, cy, size):
    icon = Image.open(STORM_ICON).convert("RGBA").resize((size, size), Image.LANCZOS); img.alpha_composite(icon, (cx - size // 2, cy - size // 2))

def s1_storm_orb(img, src):
    """The orb the user saw, filled with the storm's cold light instead of blood: the same piece, a different liquid."""
    clear(img, src); orb = Image.open(S / "orb_cradle.png").convert("RGBA").resize((240, 240), Image.LANCZOS); r = 78
    liquid = Image.new("RGBA", img.size, (0, 0, 0, 0)); dl = ImageDraw.Draw(liquid)
    dl.ellipse([CX - r, CY - r, CX + r, CY + r], fill=(70, 120, 190, 220)); top = CY + r - int(2 * r * FILL)
    dl.rectangle([CX - r - 2, CY - r - 2, CX + r + 2, top], fill=(0, 0, 0, 0)); dl.ellipse([CX - r, top - 6, CX + r, top + 6], fill=(150, 200, 245, 230))
    # a lightning flicker inside the light
    dl.line([(CX - 14, top + 10), (CX - 2, top + 30), (CX - 10, top + 30), (CX + 6, top + 56)], fill=(230, 240, 255, 230), width=3)
    sphere = Image.new("L", img.size, 0); ImageDraw.Draw(sphere).ellipse([CX - r, CY - r, CX + r, CY + r], fill=255); liquid.putalpha(Image.composite(liquid.getchannel("A"), Image.new("L", img.size, 0), sphere))
    glass = Image.new("RGBA", img.size, (0, 0, 0, 0)); ImageDraw.Draw(glass).ellipse([CX - r, CY - r, CX + r, CY + r], fill=(24, 30, 44, 170)); img.alpha_composite(glass); img.alpha_composite(liquid)
    img.alpha_composite(orb, (CX - 120, CY - 120)); d = ImageDraw.Draw(img); storm_mark(img, d, CX, CY - 96, 30)
    label(d, (CX, CY - 6), TIME, 30); label(d, (CX, CY + 128), WORDS, 17, color=BONE_D)

def s2_hourglass(img, src):
    """An iron-framed hourglass: the sand in the top bulb is what is left before the storm, the bottom is what has run."""
    clear(img, src); d = ImageDraw.Draw(img); w, h = 140, 200; x0, y0 = CX - w // 2, CY - h // 2 - 10
    for y in (y0, y0 + h - 22): d.rounded_rectangle([x0 - 16, y, x0 + w + 16, y + 22], radius=6, fill=IRON, outline=INK, width=3); d.rectangle([x0 - 16, y + 7, x0 + w + 16, y + 10], fill=IRON_L)
    for px in (x0 - 6, x0 + w + 6): d.line([(px, y0 + 22), (px, y0 + h - 22)], fill=IRON_L, width=8); d.line([(px, y0 + 22), (px, y0 + h - 22)], fill=INK, width=2)
    gy0, gy1 = y0 + 24, y0 + h - 24; mid = (gy0 + gy1) // 2; gl = (40, 44, 60, 150)
    top = [(x0 + 10, gy0), (x0 + w - 10, gy0), (CX + 8, mid), (CX - 8, mid)]; bot = [(CX - 8, mid), (CX + 8, mid), (x0 + w - 10, gy1), (x0 + 10, gy1)]
    d.polygon(top, fill=gl, outline=INK); d.polygon(bot, fill=gl, outline=INK)
    # sand: the top keeps (1 - FILL) of its bulb from the neck up, the bottom has FILL piled in a mound
    remain = 1 - FILL; sand_top = mid - int((mid - gy0) * remain)
    def width_at(y, a, b, ya, yb): return a + (b - a) * (y - ya) / (yb - ya)
    pts = [(CX - 8, mid), (CX + 8, mid)]
    for y in range(mid, sand_top, -4): hw = width_at(y, 8, (w - 20) / 2, mid, gy0); pts.insert(0, (CX - hw, y)); pts.append((CX + hw, y))
    d.polygon(pts, fill=BONE); d.polygon(pts, outline=BONE_D)
    mound_h = int((gy1 - mid) * FILL); d.pieslice([CX - 46, gy1 - mound_h * 2, CX + 46, gy1 + mound_h], 180, 360, fill=BONE); d.line([(CX, mid), (CX, gy1 - mound_h)], fill=BONE, width=3)
    d.ellipse([CX - 10, gy0 - 14, CX + 10, gy0 + 6], fill=GOLD, outline=INK, width=2)
    label(d, (CX, y0 + h + 22), TIME, 26); label(d, (CX, y0 + h + 52), WORDS, 17, color=BONE_D)
    chain(d, CX, 626, y0 - 2)

def s3_rune_disc(img, src):
    """A round iron plate with a carved storm rune; a gold ring around it fills clockwise as the storm comes (what the game does now, in this material)."""
    clear(img, src); d = ImageDraw.Draw(img); r = 84
    d.ellipse([CX - r - 8, CY - r - 8, CX + r + 8, CY + r + 8], fill=INK); d.ellipse([CX - r, CY - r, CX + r, CY + r], fill=IRON, outline=IRON_L, width=3)
    d.ellipse([CX - r + 14, CY - r + 14, CX + r - 14, CY + r - 14], fill=IRON_D, outline=INK, width=3)
    for i in range(12):
        a = math.radians(i * 30 - 90); d.line([(CX + (r - 4) * math.cos(a), CY + (r - 4) * math.sin(a)), (CX + (r - 12) * math.cos(a), CY + (r - 12) * math.sin(a))], fill=GOLD_D, width=3)
    d.arc([CX - r + 4, CY - r + 4, CX + r - 4, CY + r - 4], start=-90, end=-90 + 360 * FILL, fill=GOLD, width=9)
    d.arc([CX - r + 4, CY - r + 4, CX + r - 4, CY + r - 4], start=-90 + 360 * FILL, end=270, fill=(60, 56, 62, 255), width=9)
    # the rune: a storm cloud with a bolt, carved (dark line, lighter edge below)
    storm_mark(img, d, CX, CY - 34, 44); d = ImageDraw.Draw(img)
    for px, py in ((CX, CY - r - 2), (CX, CY + r + 2), (CX - r - 2, CY), (CX + r + 2, CY)): d.ellipse([px - 5, py - 5, px + 5, py + 5], fill=IRON_L, outline=INK, width=2)
    label(d, (CX, CY + 14), TIME, 28); label(d, (CX, CY + r + 30), WORDS, 17, color=BONE_D); chain(d, CX, 626, CY - r - 10)

def s4_candle(img, src):
    """A thick candle on a skull holder: it burns down as the storm comes; the flame flickers lower near the end."""
    clear(img, src); d = ImageDraw.Draw(img); full = 150; left = int(full * (1 - FILL)); x = CX; base = CY + 70
    glow = Image.new("RGBA", img.size, (0, 0, 0, 0)); ImageDraw.Draw(glow).ellipse([x - 120, base - left - 160, x + 120, base - left + 40], fill=(255, 170, 70, 80)); img.alpha_composite(glow.filter(ImageFilter.GaussianBlur(30))); d = ImageDraw.Draw(img)
    d.ellipse([x - 54, base - 10, x + 54, base + 24], fill=IRON, outline=INK, width=3); d.ellipse([x - 40, base - 4, x + 40, base + 16], fill=IRON_D, outline=INK, width=2)
    # the skull on the holder's front
    sx, sy = x, base + 2; d.ellipse([sx - 15, sy - 15, sx + 15, sy + 10], fill=BONE, outline=INK, width=2); d.rectangle([sx - 8, sy + 5, sx + 8, sy + 15], fill=BONE, outline=INK, width=2)
    for dx in (-6, 6): d.ellipse([sx + dx - 4, sy - 7, sx + dx + 4, sy + 1], fill=INK)
    d.rounded_rectangle([x - 26, base - 6 - left, x + 26, base - 6], radius=5, fill=(226, 214, 190, 255), outline=INK, width=3)
    for i, (dx, dy) in enumerate(((-20, 18), (14, 30), (-6, 54), (18, 70))):
        if dy < left: d.ellipse([x + dx - 6, base - 6 - left + dy - 5, x + dx + 6, base - 6 - left + dy + 9], fill=(236, 226, 206, 255), outline=INK, width=2)
    top = base - 6 - left; d.line([(x, top), (x, top - 10)], fill=INK, width=3)
    d.polygon([(x - 10, top - 10), (x, top - 44), (x + 10, top - 10)], fill=FLAME, outline=(200, 110, 30, 255)); d.polygon([(x - 4, top - 11), (x, top - 28), (x + 4, top - 11)], fill=(255, 240, 200, 255))
    # the hours carved on the candle: marks at every quarter
    for q in range(1, 4): y = base - 6 - full * q / 4; d.line([(x + 18, y), (x + 26, y)], fill=BONE_D, width=2) if y > top else None
    label(d, (x, base + 46), TIME, 26); label(d, (x, base + 76), WORDS, 17, color=BONE_D); storm_mark(img, d, x + 60, top - 20, 26)

def main():
    src = Image.open(OUT / "mock-D2-bone.png").convert("RGBA")
    variants = (("S1-storm-orb", s1_storm_orb), ("S2-hourglass", s2_hourglass), ("S3-rune-ring", s3_rune_disc), ("S4-candle", s4_candle))
    crops = []
    for name, fn in variants:
        img = src.copy(); fn(img, src); img.convert("RGB").save(OUT / f"mock-{name}.png"); crops.append((name, img.crop((CX - 230, 600, CX + 230, 1000)).convert("RGB")))
    crops.insert(0, ("blood-orb (seen)", src.crop((CX - 230, 600, CX + 230, 1000)).convert("RGB")))
    gap, lab = 20, 36; w, h = crops[0][1].size; sheet = Image.new("RGB", (len(crops) * (w + gap) + gap, h + lab + 2 * gap), (18, 18, 18)); d = ImageDraw.Draw(sheet); x = gap
    for name, im in crops: d.text((x, gap), name, font=font(24), fill=(235, 235, 235)); sheet.paste(im, (x, gap + lab)); x += w + gap
    sheet.save(OUT / "mock-clock-compare.png"); print("clock compare", sheet.size)

if __name__ == "__main__": main()
