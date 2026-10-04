"""Takes the dark pupils out of the monsters' yellow eyes, on the raw generated image, no API call (round 17).
  .venv/bin/python ArtPipeline/Archive/17-outline-pupils/remove_pupils.py [--to <dir>] [key ...]

An eye is a patch of lantern yellow inside a hand-drawn ink outline, and its pupil is a dark blob inside that outline.
A pupil the yellow closes all round is a hole in the yellow, painted the eye's yellow. A pupil that touches the outline
is painted within the eye's opening, given as a polygon per eye, except within a line's width of the skin: the
outline stays where it was. Colour alone cannot do it here: the goblins' grey-green skin lies on the blend between
the ink and the yellow, and a pupil merges with the heavy upper lid.
It reads the confirmed raws (Archive/12-roar-style/approved/enemy) and writes them without pupils where the pipeline keeps
its raws (output/enemy, so gen_image.py --refit stands them up) and to approved/enemy here; --to <dir> is a trial.
Decided after the review: the user's "눈동자는 a안으로 구현" (2026-10-04, Design/10 §3).
"""
import sys
from collections import deque
from pathlib import Path
from PIL import Image

HERE = Path(__file__).resolve().parent; ROOT = HERE.parents[2]
SOURCE = ROOT / "ArtPipeline" / "Archive" / "12-roar-style" / "approved" / "enemy"   # the confirmed raws, with their pupils
KEEP = HERE / "approved" / "enemy"                                                   # the raws without, kept in the repository
sys.path.insert(0, str(ROOT / "ArtPipeline" / "tools"))
import gen_image as g  # noqa: E402  (the pipeline's own grow and paths)

# The eyes of each monster in its raw image. A pupil that the yellow closes all round is a hole in it: "hole" gives a
# point in the yellow. A pupil that touches the eye's outline cannot be told from the line by its colour: "opening"
# gives the eye's opening as a polygon (read off 8x close-ups, ArtPipeline/Archive/17-outline-pupils/), and what is not
# yellow inside it is painted, except within a line's width of the skin, where the outline is. "pupil" gives the pupil
# itself as a polygon, for an eye whose white is too pale to tell from the skin: what is not the eye's colour in it is painted.
EYES = {
    "goblin_raider": [("opening", [(373, 268), (383, 263), (390, 261), (396, 267), (402, 276), (397, 284), (395, 292), (380, 292), (372, 284)]),
                      ("hole", (467, 285))],
    # the archer's pale eyes have a thin sliver of white left of the pupil, as light as the skin: the pupil itself is given
    "goblin_archer": [("pupil", [(517, 298), (525, 298), (527, 301), (526, 306), (521, 308), (517, 306)]),
                      ("pupil", [(471, 292), (477, 292), (478, 296), (477, 301), (472, 302), (471, 298)])],
    "goblin_shaman": [("hole", (428, 326)),
                      ("opening", [(355, 331), (360, 327), (368, 326), (376, 329), (379, 333), (372, 338), (363, 342), (355, 340)])],
    "cave_rat": [("opening", [(184, 405), (192, 402), (199, 404), (207, 410), (214, 418), (219, 427), (218, 437), (206, 440), (185, 439), (182, 425)]),
                 ("opening", [(246, 434), (250, 429), (257, 424), (264, 419), (272, 414), (283, 410), (294, 408), (303, 411), (307, 422),
                              (306, 440), (300, 452), (290, 460), (275, 463), (258, 456), (249, 447)])],
    "mine_overseer": [("opening", [(286, 290), (289, 285), (295, 282), (303, 281), (310, 279), (316, 277), (320, 280), (319, 289),
                                   (314, 296), (305, 299), (296, 299), (289, 296)]),
                      ("opening", [(250, 282), (255, 280), (261, 282), (265, 285), (264, 289), (258, 293), (252, 294), (250, 288)])],
}

INK = (28, 16, 12)
LINE = 6                 # the ink line's width in the raw image, measured around the eyes (5 to 7)
MARGIN = 40              # the window around an eye that is looked at


def luma(p):
    return 0.299 * p[0] + 0.587 * p[1] + 0.114 * p[2]


def is_yellow(p):
    """The lantern yellow of the eyes."""
    r, gg, b = p[:3]
    return r > 185 and gg > 150 and b < 175 and r - b > 45 and gg - b > 25


def is_pale(p):
    """Yellow or the archer's paler cream: what an eye's own colour is sampled from."""
    r, gg, b = p[:3]
    return r > 185 and gg > 150 and b < 215 and r - b > 30 and gg - b > 15


def near(p, colour, reach=46):
    return sum((p[i] - colour[i]) ** 2 for i in range(3)) <= reach * reach


def is_dark(p):
    return luma(p) < 70


def to_image(mask, w, h):
    return Image.frombytes("L", (w, h), bytes(255 if m else 0 for m in mask))


def flood(seeds, allowed, w, h):
    """The pixels reachable from the seeds through allowed ones (four neighbours)."""
    seen = bytearray(w * h); queue = deque()
    for i in seeds:
        if allowed[i] and not seen[i]:
            seen[i] = 1; queue.append(i)
    while queue:
        i = queue.popleft(); x, y = i % w, i // w
        for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
            if 0 <= nx < w and 0 <= ny < h:
                j = ny * w + nx
                if allowed[j] and not seen[j]:
                    seen[j] = 1; queue.append(j)
    return seen


def remove(raw: Image.Image, eyes):
    """Returns the raw image without pupils, and per eye its box and how many pixels were painted (for the report)."""
    from PIL import ImageDraw
    image = raw.convert("RGBA"); W, H = image.size
    report = []
    for kind, where in eyes:
        points = [where] if kind == "hole" else where
        xs0 = [x for x, _ in points]; ys0 = [y for _, y in points]
        box = (max(0, min(xs0) - MARGIN), max(0, min(ys0) - MARGIN), min(W, max(xs0) + MARGIN + 1), min(H, max(ys0) + MARGIN + 1))
        crop = image.crop(box); w, h = crop.size; px = list(crop.getdata())
        # this eye's own colour: the brightest of what looks like an eye where the eye is
        if kind == "hole":
            seed = px[(where[1] - box[1]) * w + (where[0] - box[0])]
            colour = seed[:3]
        else:  # "opening" or "pupil"
            area0 = Image.new("L", (w, h), 0)
            ImageDraw.Draw(area0).polygon([(x - box[0], y - box[1]) for x, y in where], fill=255)
            inner = [px[i][:3] for i, a in enumerate(area0.tobytes()) if a and is_pale(px[i])]
            inner.sort(key=luma)
            top = inner[len(inner) * 2 // 3:] or inner
            colour = tuple(sorted(c[k] for c in top)[len(top) // 2] for k in range(3))
        yellow = [p[3] > 200 and (is_yellow(p) or near(p, colour)) for p in px]
        if kind == "hole":
            # the yellow patch the point is in, and what it closes all round
            patch = flood([(where[1] - box[1]) * w + (where[0] - box[0])], yellow, w, h)
            cells = [i for i in range(w * h) if patch[i]]
            if not cells:
                raise SystemExit(f"no yellow at {where}")
            xs = [i % w for i in cells]; ys = [i // w for i in cells]
            inside = bytearray(w * h)
            for i in range(w * h):
                x, y = i % w, i // w
                inside[i] = min(xs) <= x <= max(xs) and min(ys) <= y <= max(ys) and not patch[i]
            edge = [i for i in range(w * h) if inside[i] and (i % w in (min(xs), max(xs)) or i // w in (min(ys), max(ys)))]
            open_ = flood(edge, inside, w, h)
            paint = [i for i in range(w * h) if inside[i] and not open_[i]]
        else:
            area = Image.new("L", (w, h), 0)
            ImageDraw.Draw(area).polygon([(x - box[0], y - box[1]) for x, y in where], fill=255)
            area = area.tobytes()
            cells = [i for i in range(w * h) if area[i] and yellow[i]]
            if not cells:
                raise SystemExit(f"no yellow in the opening {where[:2]}...")
            near_yellow = g.grow(to_image(yellow, w, h), 2).tobytes()
            # outside the eye: what is neither ink, nor this yellow, nor the blend at its edge (or is clear)
            outside = bytearray(w * h)
            for i, p in enumerate(px):
                outside[i] = p[3] < 128 or not (yellow[i] or is_dark(p) or near_yellow[i])
            if kind == "pupil":
                # the polygon is the pupil itself (read off the close-up): all of it that is not the eye's colour
                paint = [i for i in range(w * h) if area[i] and not yellow[i]]
            else:
                rim = g.grow(to_image(outside, w, h), LINE - 1).tobytes()
                paint = [i for i in range(w * h) if area[i] and not yellow[i] and not outside[i] and not rim[i]]
        eye = tuple(sorted(px[i][c] for i in cells)[len(cells) // 2] for c in range(3))
        done = bytearray(w * h)
        for i in paint:
            done[i] = 1
        # the pupil's soft edge too: yellow a little darker than the eye's, right around what is painted
        around = g.grow(to_image(done, w, h), 2).tobytes()
        edge = [i for i in range(w * h) if around[i] and not done[i] and yellow[i] and luma(px[i]) < luma(eye) - 4]
        for i in edge:
            done[i] = 1
        # each painted pixel takes the eye's colour around it (the white has a gentle shade), else the eye's median
        source = [px[i] for i in range(w * h)]
        for i in paint + edge:
            x, y = i % w, i // w; acc = [0, 0, 0]; n = 0
            for r in (4, 7, 11):
                for yy in range(max(0, y - r), min(h, y + r + 1)):
                    for xx in range(max(0, x - r), min(w, x + r + 1)):
                        j = yy * w + xx
                        if yellow[j] and not done[j] and luma(source[j]) >= luma(eye) - 10:
                            for c in range(3):
                                acc[c] += source[j][c]
                            n += 1
                if n >= 6:
                    break
            colour = tuple(round(acc[c] / n) for c in range(3)) if n else eye
            px[i] = colour + (px[i][3],)
        crop.putdata(px); image.paste(crop, box[:2])
        xs = [i % w for i in cells]; ys = [i // w for i in cells]
        report.append((kind, (box[0] + min(xs), box[1] + min(ys), box[0] + max(xs), box[1] + max(ys)), len(cells), len(paint)))
    return image, report


def main():
    """Reads the confirmed raws (with pupils) and writes them without: by default where the pipeline keeps its raws
    (gen_image.py --refit stands them up from there) and next to this script (approved/enemy, the repository's copy);
    with --to <dir>, only to that folder (a trial)."""
    args = sys.argv[1:]
    trial = None
    if args[:1] == ["--to"]:
        trial, args = Path(args[1]), args[2:]
    keys = args or list(EYES)
    targets = [trial] if trial else [g.OUTPUT_DIR / "enemy", KEEP]
    for target in targets:
        target.mkdir(parents=True, exist_ok=True)
    for key in keys:
        raw = Image.open(SOURCE / f"{key}.raw.png")
        image, found = remove(raw, EYES[key])
        for target in targets:
            image.save(target / f"{key}.raw.png")
        print(key, "eyes:", [(kind, box, f"yellow {n}", f"painted {p}") for kind, box, n, p in found], "->", ", ".join(str(t) for t in targets))


if __name__ == "__main__":
    main()
