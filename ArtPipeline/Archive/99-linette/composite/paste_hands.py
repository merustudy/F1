"""Round 99: put the redrawn hands of dialogue 7, 8-2 and 8 onto the generated originals. No API call.

In use (the user, the same day: "취소 이전 손 합성한 그림으로 3개 다시 연결"). A copy of what it writes is kept in
candidates/linette_portrait<n>_paste.raw.png.

The hands were redrawn with the original attached (STYLE_RUNTIME-portrait-hands.md: everything kept, only the two
hands clean and with bare nails) as output/character/linette_portrait<n>h.raw.png. Only the hands are taken from it:
the redraw is shifted onto the original by the offset that matches them best (searched within SEARCH pixels over
everything outside the hands), and pasted inside HANDS with an edge softened over FEATHER pixels. Outside it the
picture is the original, pixel for pixel: the face, the expression, the hair, the dress and the ledger.

Run: .venv/bin/python ArtPipeline/Archive/99-linette/composite/paste_hands.py [--preview]
  --preview writes the mask over each original (composite/paste_mask.jpg) and changes nothing.
Without it, ArtPipeline/output/character/linette_portrait<n>.raw.png becomes the pasted picture (the original stays in
candidates/linette_portrait<n>_ink.raw.png); refit it with gen_image.py --refit afterwards.
"""
import sys
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageStat

HERE = Path(__file__).resolve().parent
ROUND = HERE.parent
ROOT = HERE.parents[3]
OUTPUT = ROOT / "ArtPipeline" / "output" / "character"
NAMES = ["linette_portrait7", "linette_portrait8b", "linette_portrait8"]
# Both hands and their wrists, with a margin over the book, the dress and the skin around them (the pose is the same
# in all three originals, within a pixel).
HANDS = [(555, 660), (605, 664), (630, 698), (648, 745), (650, 800), (672, 832), (660, 862), (612, 858), (608, 900),
         (606, 965), (596, 1005), (560, 1028), (470, 1030), (440, 1000), (448, 950), (462, 900), (478, 866),
         (494, 826), (500, 790), (500, 740), (515, 700), (535, 678)]
FEATHER = 8
SEARCH = 6
MATCH_BOX = (300, 120, 760, 1200)  # the figure, for the offset


def hands_mask(size):
    mask = Image.new("L", size, 0)
    ImageDraw.Draw(mask).polygon(HANDS, fill=255)
    return mask.filter(ImageFilter.GaussianBlur(FEATHER / 2))


def best_offset(original, redraw, mask):
    """The shift of the redraw that matches the original best outside the hands (grey levels, mean difference)."""
    keep = ImageChops.invert(mask.point(lambda value: 255 if value else 0)).crop(MATCH_BOX)
    base = original.convert("L").crop(MATCH_BOX)
    best = None
    for dx in range(-SEARCH, SEARCH + 1):
        for dy in range(-SEARCH, SEARCH + 1):
            moved = redraw.convert("L").crop((MATCH_BOX[0] + dx, MATCH_BOX[1] + dy,
                                              MATCH_BOX[2] + dx, MATCH_BOX[3] + dy))
            diff = ImageStat.Stat(ImageChops.difference(base, moved), mask=keep).mean[0]
            if best is None or diff < best[0]:
                best = (diff, dx, dy)
    return best


def main():
    preview = "--preview" in sys.argv
    sheet = []
    for name in NAMES:
        original = Image.open(ROUND / "candidates" / f"{name}_ink.raw.png").convert("RGBA")
        redraw = Image.open(OUTPUT / f"{name}h.raw.png").convert("RGBA")
        mask = hands_mask(original.size)
        diff, dx, dy = best_offset(original, redraw, mask)
        print(f"{name}: redraw shifted by ({-dx}, {-dy}), mean grey difference outside the hands {diff:.1f}")
        moved = Image.new("RGBA", original.size, (0, 0, 0, 0))
        moved.paste(redraw, (-dx, -dy))
        pasted = Image.composite(moved, original, mask)
        if preview:
            shown = original.copy()
            shown.paste((255, 0, 255, 255), (0, 0), mask.point(lambda value: value // 2))
            sheet.append(shown.crop((380, 620, 740, 1060)).convert("RGB"))
            continue
        pasted.save(OUTPUT / f"{name}.raw.png")
    if preview:
        width, height = sheet[0].size
        board = Image.new("RGB", (width * len(sheet), height))
        for index, part in enumerate(sheet):
            board.paste(part, (index * width, 0))
        board.save(HERE / "paste_mask.jpg", quality=88)


if __name__ == "__main__":
    main()
