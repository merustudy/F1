#!/usr/bin/env python3
"""Stands a mercenary's attack and hit poses at the size of its approved figure, on the pose canvas. No API call.

  fit_pose.py                      every pose that has a raw (output/attack/<key>.raw.png, output/hit/<key>.raw.png)
  fit_pose.py --type attack --key paladin

A pose (gen_image.py --type attack|hit) is drawn after the mercenary's approved figure; here it is stood in place of
that figure (Docs/Architecture/13_ART_PIPELINE.md "후처리 (pose)", the rules of rounds 19 and 23):

- Size: the scale of the pose against the approved raw (output/character/<key>.raw.png) is measured on parts whose size
  does not change with the pose: the round gold discs (clasps, rivets, buckles, studs), the ratio of the medians of the
  largest few. The roster's Scale replaces it where the figure has no such disc or the measure was overruled after
  looking (round 23: the archmage's crystal, the spellblade's square buckles, the paladin's hit).
- Place: the pose canvas is three figure canvases wide and an eighth deeper (2016x1008): the figure canvas (672x896) in
  the middle, its floor line where the figure's is. The pose's back foot (the leftmost boot) stands where the approved
  figure's back foot is and its sole on the floor line; a weapon may come down below it, into the room under the line.
- Colours: each main colour of the pose (the approved raw's, found by quantizing it) is moved by the difference of its
  mean from the approved figure's, the ink and the white held where they are; kept only when the worst colour comes
  nearer (CIE76 delta E).
- The ring around the silhouette is the figures' (FIGURE_OUTLINE, UI_LINE).
Writes output/<type>/<key>.png: the file that goes to Assets/@Art/Pose/Job/<key>_<type>.png.
"""
import argparse
import colorsys
import csv
import math
import sys
from pathlib import Path
from PIL import Image

TOOLS = Path(__file__).resolve().parent
ROOT = TOOLS.parent
sys.path.insert(0, str(TOOLS))
from gen_image import ALPHA_FLOOR, FIGURE_CANVAS, FIGURE_FLOOR_MARGIN, FIGURE_SIDE_MARGIN, FIGURE_OUTLINE, UI_LINE, ring_alpha, subject_box  # noqa: E402

OUTPUT = ROOT / "output"
ROSTERS = ROOT / "Rosters"
POSES = ("attack", "hit")
BELOW = 112
SIDE = FIGURE_CANVAS[0]               # room beside the figure canvas, on each side
WIDE = (FIGURE_CANVAS[0] + 2 * SIDE, FIGURE_CANVAS[1] + BELOW)
FLOOR = FIGURE_CANVAS[1] - FIGURE_FLOOR_MARGIN
DISCS = 4                             # how many of the largest discs are compared
NEAR = 26                             # a pixel belongs to a main colour within this distance (RGB)
HELD = [(40, 22, 15), (255, 255, 255)]


def lab(rgb):
    def lin(c):
        c /= 255.0
        return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4
    r, g, b = (lin(c) for c in rgb)
    x = (0.4124 * r + 0.3576 * g + 0.1805 * b) / 0.95047
    y = 0.2126 * r + 0.7152 * g + 0.0722 * b
    z = (0.0193 * r + 0.1192 * g + 0.9505 * b) / 1.08883
    f = lambda t: t ** (1 / 3) if t > 0.008856 else 7.787 * t + 16 / 116
    return 116 * f(y) - 16, 500 * (f(x) - f(y)), 200 * (f(y) - f(z))


def gold(r, g, b):
    h, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
    return 0.06 <= h <= 0.15 and s >= 0.40 and 0.45 <= v <= 0.90


def blobs(image, test, min_area, max_area):
    """Connected areas of opaque pixels that pass the test: (pixels, [(x, y)...])."""
    px = image.load()
    w, h = image.size
    seen = bytearray(w * h)
    found = []
    for y in range(h):
        for x in range(w):
            i = y * w + x
            if seen[i]:
                continue
            r, g, b, a = px[x, y]
            if a < 200 or not test(r, g, b):
                continue
            stack, pts = [(x, y)], []
            seen[i] = 1
            while stack:
                cx, cy = stack.pop()
                pts.append((cx, cy))
                for nx, ny in ((cx + 1, cy), (cx - 1, cy), (cx, cy + 1), (cx, cy - 1)):
                    if 0 <= nx < w and 0 <= ny < h and not seen[ny * w + nx]:
                        q = px[nx, ny]
                        if q[3] >= 200 and test(*q[:3]):
                            seen[ny * w + nx] = 1
                            stack.append((nx, ny))
            if min_area <= len(pts) <= max_area:
                found.append(pts)
    return found


def discs(image):
    """The long axes of the round gold areas, largest first."""
    axes = []
    for pts in blobs(image, gold, 120, 12000):
        n = len(pts)
        mx, my = sum(p[0] for p in pts) / n, sum(p[1] for p in pts) / n
        cxx = sum((p[0] - mx) ** 2 for p in pts) / n
        cyy = sum((p[1] - my) ** 2 for p in pts) / n
        cxy = sum((p[0] - mx) * (p[1] - my) for p in pts) / n
        root = math.sqrt(max(0.0, (cxx + cyy) ** 2 / 4 - (cxx * cyy - cxy * cxy)))
        major, minor = 4 * math.sqrt((cxx + cyy) / 2 + root), 4 * math.sqrt(max((cxx + cyy) / 2 - root, 1e-6))
        if n / (math.pi / 4 * major * minor) >= 0.8 and minor / major >= 0.35:
            axes.append(major)
    return sorted(axes, reverse=True)


def median(values):
    values = sorted(values)
    return (values[(len(values) - 1) // 2] + values[len(values) // 2]) / 2


def boot_colour(approved):
    """The leather of the boots: the commonest mid-dark warm colour in the lowest tenth of the approved figure."""
    box = subject_box(approved)
    low = approved.crop((box[0], box[3] - (box[3] - box[1]) // 10, box[2], box[3]))
    counts = {}
    for r, g, b, a in low.getdata():
        h, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
        if a > 200 and 0.25 <= v <= 0.75 and s >= 0.25 and (h <= 0.12 or h >= 0.95):
            key = (r // 12 * 12 + 6, g // 12 * 12 + 6, b // 12 * 12 + 6)
            counts[key] = counts.get(key, 0) + 1
    return max(counts, key=counts.get)


def feet(image, leather):
    """The boots in the lowest part of the figure: (back foot x, its sole y, the boots' columns), from the leather."""
    box = subject_box(image)
    y0 = box[3] - (box[3] - box[1]) * 32 // 100
    px = image.load()
    columns = {}
    for y in range(y0, box[3]):
        for x in range(box[0], box[2]):
            r, g, b, a = px[x, y]
            if a > 200 and (r - leather[0]) ** 2 + (g - leather[1]) ** 2 + (b - leather[2]) ** 2 < 34 * 34:
                columns.setdefault(x, []).append(y)
    xs = sorted(x for x, ys in columns.items() if len(ys) >= 6)
    groups, current = [], []
    for x in xs:
        if current and x - current[-1] > 24:
            groups.append(current)
            current = []
        current.append(x)
    if current:
        groups.append(current)
    groups = [g for g in groups if len(g) >= 30]
    if not groups:
        raise SystemExit("실패: 부츠를 찾지 못했다")
    back = groups[0]
    lowest = max(max(columns[x]) for x in back)
    return sum(back) / len(back), lowest, groups


def main_colours(approved):
    """The approved raw's main colours: its quantized palette, without ink and white, by share."""
    box = subject_box(approved)
    rgb = Image.new("RGB", approved.size, (0, 0, 0))
    rgb.paste(approved, mask=approved.getchannel("A").point(lambda a: 255 if a > 200 else 0))
    q = rgb.crop(box).quantize(colors=14, method=Image.Quantize.MEDIANCUT)
    palette = q.getpalette()[:14 * 3]
    counts = sorted(q.getcolors(), reverse=True)
    total = sum(c for c, _ in counts)
    out = []
    for count, index in counts:
        colour = tuple(palette[index * 3:index * 3 + 3])
        if count / total >= 0.015 and max(colour) > 70 and min(colour) < 235:
            out.append(colour)
    return out


def means(image, main):
    sums = {c: [0, 0, 0, 0] for c in main}
    for r, g, b, a in image.getdata():
        if a <= 200:
            continue
        key, best = None, NEAR * NEAR
        for c in main:
            d = (r - c[0]) ** 2 + (g - c[1]) ** 2 + (b - c[2]) ** 2
            if d < best:
                key, best = c, d
        if key:
            s = sums[key]
            s[0] += r; s[1] += g; s[2] += b; s[3] += 1
    return {c: (s[0] / s[3], s[1] / s[3], s[2] / s[3]) for c, s in sums.items() if s[3] > 200}


def worst(colours, approved_means):
    return max(math.dist(lab(approved_means[c]), lab(colours[c])) for c in colours if c in approved_means)


def match_colours(raw, main, approved_means):
    pose_means = means(raw, main)
    anchors = [(pose_means[c], tuple(a - b for a, b in zip(approved_means[c], pose_means[c]))) for c in pose_means if c in approved_means]
    anchors += [(colour, (0.0, 0.0, 0.0)) for colour in HELD]
    cache = {}

    def moved(colour):
        if colour not in cache:
            total, shift = 0.0, [0.0, 0.0, 0.0]
            for centre, delta in anchors:
                w = 1.0 / (sum((c - m) ** 2 for c, m in zip(colour, centre)) + 64.0) ** 2
                total += w
                for i in range(3):
                    shift[i] += w * delta[i]
            cache[colour] = tuple(max(0, min(255, round(c + v / total))) for c, v in zip(colour, shift))
        return cache[colour]

    out = raw.copy()
    out.putdata([moved((r, g, b)) + (a,) if a else (r, g, b, a) for r, g, b, a in raw.getdata()])
    return out


def ring(canvas):
    body = canvas.getchannel("A").point(lambda value: 255 if value > ALPHA_FLOOR else 0)
    under = Image.new("RGBA", canvas.size, UI_LINE + (0,))
    under.putalpha(ring_alpha(body, FIGURE_OUTLINE))
    under.alpha_composite(canvas)
    return under


def place(raw, ratio, origin, box):
    subject = raw.crop(box)
    subject = subject.resize((round(subject.width * ratio), round(subject.height * ratio)), Image.LANCZOS)
    canvas = Image.new("RGBA", WIDE, (0, 0, 0, 0))
    canvas.paste(subject, origin)
    return ring(canvas)


def roster_scales(kind):
    """Key -> the roster's Scale of a pose type, where one is written."""
    with (ROSTERS / f"{kind}.csv").open(encoding="utf-8", newline="") as handle:
        return {row["Key"]: float(row["Scale"]) for row in csv.DictReader(handle) if (row.get("Scale") or "").strip()}


def approved_place(key):
    """The approved figure on the pose canvas: (raw, ratio, origin of its subject's box, box, back foot x on the canvas,
    boot colour), as gen_image.fit_figure fits it (Height 90, bound by the canvas width), its canvas in the middle."""
    approved = Image.open(OUTPUT / "character" / f"{key}.raw.png").convert("RGBA")
    box = subject_box(approved)
    width, height = box[2] - box[0], box[3] - box[1]
    ratio = min(round(FIGURE_CANVAS[1] * 0.9) / height, (FIGURE_CANVAS[0] - 2 * FIGURE_SIDE_MARGIN) / width)
    x = (FIGURE_CANVAS[0] - round(width * ratio)) // 2 + SIDE
    leather = boot_colour(approved)
    back, soles, _ = feet(approved, leather)
    return approved, ratio, (x, FLOOR - round((soles - box[1]) * ratio)), box, x + (back - box[0]) * ratio, leather


def idle_wide(key):
    """The approved figure on the pose canvas, for a review."""
    approved, ratio, origin, box, _, _ = approved_place(key)
    return place(approved, ratio, origin, box)


def fit_pose(kind, key, scales):
    approved, ratio, _, _, foot, leather = approved_place(key)
    raw = Image.open(OUTPUT / kind / f"{key}.raw.png").convert("RGBA")
    approved_discs = discs(approved)
    pose_discs = discs(raw)
    k = min(DISCS, len(pose_discs), len(approved_discs))
    measured = median(pose_discs[:k]) / median(approved_discs[:k]) if k >= 2 else None
    scale = scales.get(key, measured)
    if scale is None:
        raise SystemExit(f"실패: {kind}/{key} 의 배율을 잴 금 장식이 없다. Rosters/{kind}.csv 의 Scale 에 적는다")
    b = subject_box(raw)
    pose_back, pose_soles, _ = feet(raw, leather)
    r = ratio / scale
    origin = (round(foot - (pose_back - b[0]) * r), FLOOR - round((pose_soles - b[1]) * r))

    main = main_colours(approved)
    approved_means = means(approved, main)
    matched = match_colours(raw, main, approved_means)
    before, after = worst(means(raw, main), approved_means), worst(means(matched, main), approved_means)
    out = OUTPUT / kind / f"{key}.png"
    place(matched if after < before else raw, r, origin, b).save(out)
    below = origin[1] + round((b[3] - b[1]) * r) - FLOOR
    right = origin[0] + round((b[2] - b[0]) * r)
    print(f"{kind}/{key}: discs {[round(d) for d in pose_discs[:DISCS]]} against {[round(d) for d in approved_discs[:DISCS]]} -> "
          f"{'scale ' + format(measured, '.3f') if measured else 'no disc'}{' (Scale ' + str(scales[key]) + ')' if key in scales else ''}, "
          f"colours worst dE {before:.1f} -> {after:.1f} {'moved' if after < before else 'as drawn'}, "
          f"x {origin[0]}..{right}, {below:+d} below the floor -> {out.relative_to(ROOT.parent)}")
    if origin[0] < 0 or origin[1] < 0 or right > WIDE[0] or FLOOR + below > WIDE[1]:
        print(f"  경고: {kind}/{key} 가 자세 캔버스({WIDE[0]}x{WIDE[1]}) 밖으로 나간다")


def parse_args():
    parser = argparse.ArgumentParser(description="Stand mercenaries' poses at their approved figures' size. No API call.")
    parser.add_argument("--type", dest="kinds", action="append", choices=POSES, help="attack or hit; may be given twice. Default: both.")
    parser.add_argument("--key", dest="keys", action="append", help="A job id; may be given more than once. Default: every pose that has a raw.")
    return parser.parse_args()


def main():
    args = parse_args()
    for kind in args.kinds or POSES:
        scales = roster_scales(kind)
        keys = args.keys or sorted(path.name[:-len(".raw.png")] for path in (OUTPUT / kind).glob("*.raw.png"))
        for key in keys:
            fit_pose(kind, key, scales)
    return 0


if __name__ == "__main__":
    sys.exit(main())
