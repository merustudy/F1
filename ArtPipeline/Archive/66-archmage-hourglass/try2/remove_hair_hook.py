# -*- coding: utf-8 -*-
"""Round 66 (the user, 2026-10-14: "왼쪽 어깨위 머리 살짝 올라온 것 제거"): the 2nd try's one grey strand that hangs from the hair
behind her neck over her left shoulder and curls like a hook (raw x 518-546, y 214-251) is painted over with the skin around it,
and the hair behind the neck is closed where the shoulder line meets the neck, in the ink of the shoulder line. No API call.
Reads try2/archmage_hourglass2.raw.png, writes output/character/archmage_hourglass2b.raw.png and a copy here; then
  .venv/bin/python ArtPipeline/tools/gen_image.py --type character --key archmage --name archmage_hourglass2b \
    --roster ArtPipeline/Archive/66-archmage-hourglass/try2/character.csv --refit
  .venv/bin/python ArtPipeline/Archive/66-archmage-hourglass/try2/remove_hair_hook.py
"""
from pathlib import Path

from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]
SOURCE = HERE / "archmage_hourglass2.raw.png"
OUT = ROOT / "ArtPipeline/output/character/archmage_hourglass2b.raw.png"
SKIN = (247, 186, 144, 254)  # the flat skin fill around the strand
INK = (25, 14, 10, 255)  # the shoulder line
# Below the closing line, around the hanging strand and its curl (raw pixels, read off a 6x grid).
STRAND = [(519, 223), (526, 217), (535, 213), (545, 209), (549, 213), (549, 236), (546, 251),
          (518, 251), (515, 246), (516, 234), (519, 227)]
# Where the hair behind the neck now ends: from the neck's back contour to the shoulder line.
CLOSE = [(518, 222), (526, 216), (535, 212), (546, 207)]
SS = 4  # supersampling for the ink stroke


def main():
    img = Image.open(SOURCE).convert("RGBA")
    ImageDraw.Draw(img).polygon(STRAND, fill=SKIN)

    box = (505, 195, 560, 235)
    w, h = box[2] - box[0], box[3] - box[1]
    stroke = Image.new("RGBA", (w * SS, h * SS), INK[:3] + (0,))
    points = [((x - box[0]) * SS, (y - box[1]) * SS) for x, y in CLOSE]
    ImageDraw.Draw(stroke).line(points, fill=INK, width=4 * SS, joint="curve")
    for x, y in (points[0], points[-1]):
        r = 2 * SS
        ImageDraw.Draw(stroke).ellipse((x - r, y - r, x + r, y + r), fill=INK)
    stroke = stroke.resize((w, h), Image.LANCZOS)
    patch = img.crop(box)
    patch.alpha_composite(stroke)
    img.paste(patch, box[:2])

    img.save(OUT)
    img.save(HERE / OUT.name)
    print(OUT)


if __name__ == "__main__":
    main()
