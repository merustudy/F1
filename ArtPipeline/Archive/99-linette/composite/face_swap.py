"""Round 99: Linette D with I's face (eyes, brows, nose, mouth, makeup), keeping D's black hair and outline. No API call.

I's face is scaled 1.03 about its left eye and moved onto D's left eye (both faces turn the same way and are the same size:
eye distance 45 and 44 px in the 1024 originals). Only the inner face (brows to mouth) is taken, through a feathered polygon,
and I's copper hair is keyed out so D's black bangs and strands stay. Writes ArtPipeline/output/character/linette_d_iface.raw.png;
fit it with gen_image.py --refit (see README).

Run: .venv/bin/python ArtPipeline/Archive/99-linette/composite/face_swap.py
"""
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[4]
OUTPUT = ROOT / "ArtPipeline" / "output" / "character"

SCALE = 1.03
I_EYE = (515, 134)   # I's left eye (the nearer one) in its 1024 original
D_EYE = (496, 126)   # D's left eye
FACE = [(472, 114), (560, 114), (562, 150), (552, 176), (530, 186), (500, 184), (480, 170), (472, 140)]  # D coordinates
MOUTH = (503, 161, 535, 177)  # D coordinates: I's lips are red like its hair, so they are never keyed out
FEATHER = 3


def moved_i(d_size):
    i = Image.open(OUTPUT / "linette_i.raw.png").convert("RGBA")
    big = i.resize((round(i.size[0] * SCALE), round(i.size[1] * SCALE)), Image.LANCZOS)
    layer = Image.new("RGBA", d_size, (0, 0, 0, 0))
    offset = (round(D_EYE[0] - I_EYE[0] * SCALE), round(D_EYE[1] - I_EYE[1] * SCALE))
    layer.paste(big, offset, big)
    return layer


def hair_key(layer):
    """Opaque everywhere except I's copper hair (red well above green, not skin-light) and its transparent parts."""
    key = Image.new("L", layer.size, 0)
    src = layer.load()
    dst = key.load()
    for y in range(layer.size[1]):
        for x in range(layer.size[0]):
            r, g, b, a = src[x, y]
            hair = r > 45 and r > g * 1.4 and r < 215 and g < 130
            in_mouth = MOUTH[0] <= x <= MOUTH[2] and MOUTH[1] <= y <= MOUTH[3]
            dst[x, y] = 0 if (a < 200 or (hair and not in_mouth)) else 255
    return key


def main():
    d = Image.open(OUTPUT / "linette_d.raw.png").convert("RGBA")
    layer = moved_i(d.size)
    mask = Image.new("L", d.size, 0)
    ImageDraw.Draw(mask).polygon(FACE, fill=255)
    mask = mask.filter(ImageFilter.GaussianBlur(FEATHER))
    mask = ImageChops.multiply(mask, hair_key(layer).filter(ImageFilter.GaussianBlur(1)))
    out = d.copy()
    out.paste(layer, (0, 0), mask)
    out.save(OUTPUT / "linette_d_iface.raw.png")
    print("linette_d_iface.raw.png")


if __name__ == "__main__":
    main()
