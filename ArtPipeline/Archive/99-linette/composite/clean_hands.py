"""Round 99: wipe the ink smudges off Linette's hands in the dialogue portraits (7, 8-2, 8). No API call.

Dropped (the user found the hands odd): the hands were redrawn and pasted instead (paste_hands.py). Kept as a
record; running it again overwrites the pasted pictures.

The subject asked for "ink-stained fingertips" and the model spread grey blots over the knuckles and fingers.
Inside the hand box the skin is warm ((R - B) / R, measured per picture: 0.39 in 7, 0.37 in 8 and 8-2) and the ink
is greyer, soft edge included; the book and the outline's edge are warmer still. Ink patches ringed mostly by skin
are the blots, and so are the faint brownish islands in the skin (see blot_mask); each is filled from the skin
around it, and a median over the skin nearby wipes the faint ring.
The black nail polish goes too (the user, the same day): inside the nail boxes the grey and black of the nails and
of their smear on the thumbs is filled from the skin and tinted a pale nail colour; the black outline stays.

Run: .venv/bin/python ArtPipeline/Archive/99-linette/composite/clean_hands.py [--preview]
  --preview writes the masks (top row: blots green, nails magenta) and the cleaned hands (bottom row) to
  composite/hands_mask.jpg and changes nothing.
It always starts from the generated original, kept as candidates/<name>_ink.raw.png the first time, and writes the
cleaned picture over ArtPipeline/output/character/<name>.raw.png; refit it with gen_image.py --refit afterwards.
"""
import shutil
import sys
from collections import deque
from pathlib import Path

from PIL import Image

HERE = Path(__file__).resolve().parent
ROUND = HERE.parent
ROOT = HERE.parents[3]
OUTPUT = ROOT / "ArtPipeline" / "output" / "character"
NAMES = ["linette_portrait7", "linette_portrait8b", "linette_portrait8"]
BOX = (400, 655, 720, 1030)  # both hands holding the ledger; the pose is the same in all three
MAX_BLOT = 600
# The nails (left, top, right, bottom): the upper thumb's, the lower thumb's and the lower hand's fingertips along
# the book's edge. The blot pass leaves the patches centred in them to the nail pass.
NAILS = [(566, 660, 612, 706), (488, 838, 532, 874), (560, 848, 604, 995)]
NAIL_TINT, NAIL_TINT_SHARE = (255, 236, 226), 0.35
MIN_NAIL, MAX_NAIL = 12, 300  # under 12 px: a fleck of the line's soft edge


def lum(p):
    return 0.299 * p[0] + 0.587 * p[1] + 0.114 * p[2]


def warmth(p):
    """(R - B) / R: the skin sits at one value per picture (SkinTone), the ink and its soft edge lower,
    the book and the outline's edge higher."""
    return (p[0] - p[2]) / p[0] if p[0] else 0


class SkinTone:
    """The picture's skin warmth (the median over the bright skin in the box) and the cuts measured from it."""

    def __init__(self, px):
        skin = [px[x, y] for y in range(BOX[1], BOX[3]) for x in range(BOX[0], BOX[2])
                if lum(px[x, y]) > 185 and px[x, y][0] > 200]
        self.warmth = sorted(warmth(p) for p in skin)[len(skin) // 2]
        self.color = tuple(sorted(p[i] for p in skin)[len(skin) // 2] for i in range(3))

    def is_ink(self, p):
        return 55 < lum(p) < 225 and warmth(p) < self.warmth - 0.06

    def is_ring(self, p):
        """The pale soft edge a blot leaves on the skin."""
        return 60 < lum(p) < 245 and warmth(p) < self.warmth - 0.025

    def is_skin(self, p):
        """Skin, its cel shadow, or a blot's ring."""
        return lum(p) > 140 and warmth(p) >= self.warmth - 0.09

    def is_clean_skin(self, p):
        return lum(p) > 140 and warmth(p) >= self.warmth - 0.01

    def is_polish(self, p):
        """The nail's grey or black, lighter than the outline's core and less warm than the skin or the book."""
        return 36 < lum(p) < 200 and warmth(p) < self.warmth - 0.07

    def is_dark_nail(self, p):
        """The fingertips' nails along the book are drawn dark and as warm as the skin; the book is warmer."""
        return 36 < lum(p) < 100 and warmth(p) < self.warmth + 0.02


def near_line(px, x, y):
    """Right beside the black line: its soft edge is as grey as the ink, so it is left alone."""
    return any(lum(px[q]) < 45 for q in neighbours(x, y) if inside(*q))


def neighbours(x, y):
    for dx in (-1, 0, 1):
        for dy in (-1, 0, 1):
            if dx or dy:
                yield x + dx, y + dy


def inside(x, y):
    return BOX[0] <= x < BOX[2] and BOX[1] <= y < BOX[3]


def components(cells):
    left = set(cells)
    while left:
        start = left.pop()
        part, queue = [start], deque([start])
        while queue:
            for q in neighbours(*queue.popleft()):
                if q in left:
                    left.discard(q)
                    part.append(q)
                    queue.append(q)
        yield set(part)


def blot_mask(px, tone):
    """Grey ink patches ringed mostly by skin, then the faint brownish ones that are islands in the skin
    (the cel shadow is as dark but runs into the outline); the dress, the pages, the lines and the two nails in
    NAILS are not. The mask then takes in the soft ring around each patch."""
    cells = [(x, y) for y in range(BOX[1], BOX[3]) for x in range(BOX[0], BOX[2])]
    mask = set()

    def keep(part, need):
        if len(part) > MAX_BLOT:
            return False
        cx = sum(x for x, _ in part) / len(part)
        cy = sum(y for _, y in part) / len(part)
        if any(left <= cx < right and top <= cy < bottom for left, top, right, bottom in NAILS):
            return False
        rim = {q for x, y in part for q in neighbours(x, y) if inside(*q) and q not in part}
        skin = sum(1 for q in rim if tone.is_skin(px[q]))
        dark = sum(1 for q in rim if lum(px[q]) < 60)
        return rim and skin / len(rim) >= need and dark / len(rim) < 0.35

    cells = [q for q in cells if not near_line(px, *q)]
    for part in components(q for q in cells if tone.is_ink(px[q])):
        if keep(part, 0.4):
            mask |= part
    for part in components(q for q in cells if q not in mask and 60 < lum(px[q]) < 188):
        if keep(part, 0.9):
            mask |= part
    for _ in range(3):
        mask |= {q for x, y in mask for q in neighbours(x, y)
                 if inside(*q) and q not in mask and tone.is_ring(px[q]) and not near_line(px, *q)}
    return mask


def fill(px, mask, tone):
    """Onion-peel the hole from its rim: each pixel takes the mean of its known clean-skin neighbours."""
    left = set(mask)
    source = tone.is_clean_skin
    while left:
        done = {}
        for x, y in left:
            known = [px[q] for q in neighbours(x, y) if q not in left and source(px[q])]
            if known:
                done[(x, y)] = tuple(round(sum(c[i] for c in known) / len(known)) for i in range(3)) + (px[x, y][3],)
        if not done:
            if source == tone.is_skin:
                for q in left:  # a pocket ringed only by the line: the picture's skin
                    px[q] = tone.color + (px[q][3],)
                break
            source = tone.is_skin  # a pocket ringed only by shadow or line: take the shadow
            continue
        for q, value in done.items():
            px[q] = value
        left -= done.keys()


def smooth(px, mask):
    """A 5x5 median over the skin around each filled hole: wipes the faint ring left at the hole's edge and keeps
    the cel shadow's edge (a median does not blur an edge). The lines and their soft edge are left alone."""
    zone = set(mask)
    for _ in range(4):
        zone |= {q for x, y in zone for q in neighbours(x, y) if inside(*q)}
    zone = {q for q in zone if lum(px[q]) > 140 and not near_line(px, *q)}
    done = {}
    for x, y in zone:
        around = [px[x + dx, y + dy] for dx in range(-2, 3) for dy in range(-2, 3)
                  if (x + dx, y + dy) in zone]
        done[(x, y)] = tuple(sorted(c[i] for c in around)[len(around) // 2] for i in range(3)) + (px[x, y][3],)
    for q, value in done.items():
        px[q] = value


def nail_mask(px, tone):
    """The polish inside the nail boxes (patches with skin within three pixels, across a thin line at most), the
    near-black inside it (the outline, on its edge, keeps fewer polish neighbours), and the pale ring the polish
    leaves on the skin. NAILS[:2] are the thumbs."""
    cells = [(x, y) for left, top, right, bottom in NAILS for y in range(top, bottom) for x in range(left, right)]
    fingertips = NAILS[2]
    polish = set()
    for part in components(q for q in cells if tone.is_polish(px[q]) or (
            fingertips[0] <= q[0] < fingertips[2] and fingertips[1] <= q[1] < fingertips[3]
            and tone.is_dark_nail(px[q]))):
        if MIN_NAIL <= len(part) <= MAX_NAIL and any(tone.is_skin(px[x + dx, y + dy]) for x, y in part
                                                     for dx in range(-3, 4) for dy in range(-3, 4)):
            polish |= part
    for _ in range(4):
        polish |= {q for x, y in polish for q in neighbours(x, y) if q not in polish and in_nail(*q)
                   and lum(px[q]) <= 36 and sum(1 for n in neighbours(*q) if n in polish) >= 5}
    # On the thumbs a thick black band runs through the polish: a near-black pixel with polish on both sides of it
    # along one axis, within six pixels, is polish too (the outline has polish on one side only).
    # Along the fingertips the reach is shorter, so the line between two fingers stays.
    polish |= {(x, y) for index, (left, top, right, bottom) in enumerate(NAILS) for y in range(top, bottom)
               for x in range(left, right)
               if (x, y) not in polish and lum(px[x, y]) <= 36
               and any(any((x + k * dx, y + k * dy) in polish for k in range(1, 7 if index < 2 else 4))
                       and any((x - k * dx, y - k * dy) in polish for k in range(1, 7 if index < 2 else 4))
                       for dx, dy in ((1, 0), (0, 1), (1, 1), (1, -1)))}
    ring = set()
    for _ in range(2):
        ring |= {q for x, y in polish | ring for q in neighbours(x, y)
                 if q not in polish and q not in ring and in_nail(*q) and tone.is_ring(px[q])}
    return polish, ring


def in_nail(x, y):
    return any(left <= x < right and top <= y < bottom for left, top, right, bottom in NAILS)


def paint_nails(px, polish, ring, tone):
    """Flat cel colours: the nail a pale tint of the picture's skin, its ring the skin."""
    nail = tuple(round(tone.color[i] + (NAIL_TINT[i] - tone.color[i]) * NAIL_TINT_SHARE) for i in range(3))
    for q in polish:
        px[q] = nail + (px[q][3],)
    for q in ring:
        px[q] = tone.color + (px[q][3],)


def main():
    preview = "--preview" in sys.argv
    sheet = []
    for name in NAMES:
        raw = OUTPUT / f"{name}.raw.png"
        backup = ROUND / "candidates" / f"{name}_ink.raw.png"
        if not backup.exists():
            shutil.copy2(raw, backup)
        image = Image.open(backup).convert("RGBA")
        px = image.load()
        tone = SkinTone(px)
        blots = blot_mask(px, tone)
        polish, ring = nail_mask(px, tone)
        polish, ring = polish - blots, ring - blots
        nails = polish | ring
        print(f"{name}: skin warmth {tone.warmth:.2f}, blots {len(blots)} px, nails {len(nails)} px")
        shown = image.copy()
        fill(px, blots, tone)
        smooth(px, blots)
        paint_nails(px, polish, ring, tone)
        if preview:
            spx = shown.load()
            for q in blots:
                spx[q] = (0, 255, 0, 255)
            for q in nails:
                spx[q] = (255, 0, 255, 255)
            sheet.append((shown.crop(BOX), image.crop(BOX)))
            continue
        image.save(raw)
    if preview:
        width, height = BOX[2] - BOX[0], BOX[3] - BOX[1]
        board = Image.new("RGB", (width * len(sheet), height * 2))
        for index, (masked, filled) in enumerate(sheet):
            board.paste(masked.convert("RGB"), (index * width, 0))
            board.paste(filled.convert("RGB"), (index * width, height))
        board = board.resize((board.size[0] * 2, board.size[1] * 2), Image.NEAREST)
        board.save(HERE / "hands_mask.jpg", quality=90)


if __name__ == "__main__":
    main()
