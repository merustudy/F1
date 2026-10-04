"""Stands the attack and hit poses of a figure at the size of its approved figure (round 19), no API call.
  .venv/bin/python ArtPipeline/Archive/19-motion-test/fit_motion.py

gen_image.py fits every figure to its Height or to the canvas width, so a wider pose comes out smaller than the
figure it was drawn after, and the model draws a pose larger than its reference (it fills the canvas). A pose is
shown in place of the approved figure, so here it is scaled to the same size instead:

- The scale of a pose against the approved raw is measured on parts whose size does not change with the pose
  (README "크기 맞추기"): the round gold clasps, buckle and boot studs (the long axis of each disc), the thickness of
  the axe's haft. The approved figure's own fit (Height 90, bound by the width) then applies to the pose divided by it.
- Every image goes on one canvas twice the figure canvas wide and with room under the floor line (1344x1008): the
  approved figure where its own canvas (672x896) would be, in the middle, and a pose beside it so that its back foot
  (the foot a lunge or a recoil turns on) stays where the approved figure's back foot is and its soles stand on the
  floor line. The floor line is the soles, not the lowest point of the pose: a weapon may come down below it, in front
  of the feet, into the room under it (user's verdict 2026-10-04 on the first attack: "하단부에 여백을 주고 충분히
  도끼를 아래로 내릴 수 있게"). The figure canvas's top and floor line are the canvas's top and floor line.
- The colors are pulled back to the approved figure's: each main color of the pose (color_drift.py) is moved by the
  difference of its mean from the approved figure's, and every other color by a blend of those moves weighted by how
  near it is to each, with the ink and the white of the eyes and teeth held where they are. The model draws a pose a
  shade warmer or cooler (delta E up to about 5); this takes that out without touching the drawing. It is kept only
  when it brings the worst color nearer: a pose drawn close already is left as drawn (the second attack: the move
  would have pushed the blade from 1.6 to 3.7).
- The ring around the silhouette is the one gen_image.py gives (FIGURE_OUTLINE, UI_LINE).
Writes output/character/<name>.matched.png (the pose with its colors pulled back) and <name>_wide.png.
"""
import math
import sys
from pathlib import Path
from PIL import Image, ImageFilter

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
sys.path.insert(0, str(ROOT / "ArtPipeline" / "tools"))
sys.path.insert(0, str(HERE))
from gen_image import ALPHA_FLOOR, FIGURE_CANVAS, FIGURE_FLOOR_MARGIN, FIGURE_OUTLINE, FIGURE_SIDE_MARGIN, UI_LINE, grow, subject_box  # noqa: E402
from color_drift import MAIN, lab, means  # noqa: E402

APPROVED = ROOT / "ArtPipeline/Archive/12-roar-style/approved/character/valkyrie.raw.png"
OUTPUT = ROOT / "ArtPipeline/output/character"
# Room under the floor line for a weapon that comes down below the feet (an eighth of the figure canvas).
BELOW = 112
WIDE = (FIGURE_CANVAS[0] * 2, FIGURE_CANVAS[1] + BELOW)
FLOOR = FIGURE_CANVAS[1] - FIGURE_FLOOR_MARGIN

# How much larger than the approved raw the model drew each pose (README "크기 맞추기"), the window of the raw image
# the back foot is in (x0, y0, x1, y1) and the windows of the boots that stand on the floor (their lowest point is the
# floor line). valkyrie_attack is the first attack, judged again (its axe bent along the bottom), kept for comparison.
POSES = {
    "valkyrie_attack": {"scale": 1.12, "back": (0, 600, 650, 1024), "soles": [(0, 600, 650, 1024), (780, 700, 1150, 1024)]},
    "valkyrie_attack2": {"scale": 1.05, "back": (100, 600, 450, 1024), "soles": [(100, 600, 450, 1024), (780, 700, 1050, 1024)]},
    "valkyrie_hit": {"scale": 1.07, "back": (300, 760, 650, 1024), "soles": [(300, 760, 650, 1024)]},
}
APPROVED_FOOT = (60, 740, 400, 1024)
APPROVED_SOLES = [(60, 740, 400, 1024), (600, 740, 950, 1024)]
LEATHER = (100, 60, 48)
# Colors that stay as they are when the others are pulled back: the ink and the white of the eyes and teeth.
HELD = [(40, 22, 15), (255, 255, 255)]


def match_colors(raw, approved_means):
    """The pose with its main colors moved onto the approved figure's (see the module's notes)."""
    pose_means = means(raw)
    anchors = [(pose_means[k], tuple(a - b for a, b in zip(approved_means[k], pose_means[k])))
               for k in MAIN if k in pose_means and k in approved_means]
    anchors += [(color, (0.0, 0.0, 0.0)) for color in HELD]
    cache = {}

    def moved(color):
        if color not in cache:
            total, shift = 0.0, [0.0, 0.0, 0.0]
            for centre, delta in anchors:
                d = sum((c - m) ** 2 for c, m in zip(color, centre))
                w = 1.0 / (d + 64.0) ** 2
                total += w
                for i in range(3):
                    shift[i] += w * delta[i]
            cache[color] = tuple(max(0, min(255, round(c + v / total))) for c, v in zip(color, shift))
        return cache[color]

    out = raw.copy()
    out.putdata([moved((r, g, b)) + (a,) if a else (r, g, b, a) for r, g, b, a in raw.getdata()])
    return out


def worst(colors, approved_means):
    """The largest distance (delta E) of a main color from the approved figure's."""
    return max(math.dist(lab(approved_means[k]), lab(colors[k])) for k in MAIN if k in colors and k in approved_means)


def foot_x(image, window):
    """The middle of the leather of the boot in the window: the foot a pose turns on."""
    x0, y0, x1, y1 = window
    px = image.load()
    xs = [x for y in range(y0, min(y1, image.height)) for x in range(x0, min(x1, image.width))
          if px[x, y][3] > 200 and sum((c - l) ** 2 for c, l in zip(px[x, y][:3], LEATHER)) < 30 * 30]
    return sum(xs) / len(xs)


def sole_y(image, windows):
    """The lowest drawn point of the boots that stand on the floor: the floor line in the raw image."""
    alpha = image.getchannel("A").load()
    lowest = 0
    for x0, y0, x1, y1 in windows:
        for y in range(min(y1, image.height) - 1, y0 - 1, -1):
            if any(alpha[x, y] > ALPHA_FLOOR for x in range(x0, min(x1, image.width))):
                lowest = max(lowest, y)
                break
    return lowest


def ring(canvas):
    body = canvas.getchannel("A").point(lambda value: 255 if value > ALPHA_FLOOR else 0)
    under = Image.new("RGBA", canvas.size, UI_LINE + (0,))
    under.putalpha(grow(body, FIGURE_OUTLINE).filter(ImageFilter.GaussianBlur(0.8)))
    under.alpha_composite(canvas)
    return under


def layout():
    """Where every image goes on the wide canvas: {name: (raw image, ratio, (x, y) of its subject's box on the canvas,
    its subject's box in the raw)}. The approved figure is "valkyrie_idle"; the poses' raws are the generated ones."""
    approved = Image.open(APPROVED).convert("RGBA")
    box = subject_box(approved)
    width, height = box[2] - box[0], box[3] - box[1]
    # The approved figure's own fit (gen_image.fit_figure): Height 90, bound by the canvas width, its canvas in the middle.
    ratio = min(round(FIGURE_CANVAS[1] * 0.9) / height, (FIGURE_CANVAS[0] - 2 * FIGURE_SIDE_MARGIN) / width)
    x = (FIGURE_CANVAS[0] - round(width * ratio)) // 2 + FIGURE_CANVAS[0] // 2
    foot = x + (foot_x(approved, APPROVED_FOOT) - box[0]) * ratio
    places = {"valkyrie_idle": (approved, ratio, (x, FLOOR - round((sole_y(approved, APPROVED_SOLES) - box[1]) * ratio)), box)}
    for name, pose in POSES.items():
        raw = Image.open(OUTPUT / f"{name}.raw.png").convert("RGBA")
        r = ratio / pose["scale"]
        b = subject_box(raw)
        px = round(foot - (foot_x(raw, pose["back"]) - b[0]) * r)
        places[name] = (raw, r, (px, FLOOR - round((sole_y(raw, pose["soles"]) - b[1]) * r)), b)
    return places


def place(raw, ratio, origin, box):
    subject = raw.crop(box)
    subject = subject.resize((round(subject.width * ratio), round(subject.height * ratio)), Image.LANCZOS)
    canvas = Image.new("RGBA", WIDE, (0, 0, 0, 0))
    canvas.paste(subject, origin)
    return ring(canvas)


def main():
    places = layout()
    approved_means = means(places["valkyrie_idle"][0])
    for name, (raw, ratio, origin, box) in places.items():
        if name != "valkyrie_idle":
            matched = match_colors(raw, approved_means)
            before, after = worst(means(raw), approved_means), worst(means(matched), approved_means)
            if after < before:
                raw = matched
            print(f"{name}: colors, worst delta E {before:.1f} as drawn, {after:.1f} moved -> "
                  f"{'moved' if after < before else 'left as drawn'}")
            raw.save(OUTPUT / f"{name}.matched.png")
        place(raw, ratio, origin, box).save(OUTPUT / f"{name}_wide.png")
        below = origin[1] + round((box[3] - box[1]) * ratio) - FLOOR
        print(f"{name}: ratio {ratio:.4f}, subject at {origin} on {WIDE[0]}x{WIDE[1]}, lowest point {below:+d} from the floor line")


if __name__ == "__main__":
    main()
