"""Stands the six mercenaries' attack and hit poses at the size of their approved figures (round 23), no API call.
  .venv/bin/python ArtPipeline/Archive/23-motion-six/fit_poses.py [key ...]

Round 19's rules (../19-motion-test/fit_motion.py) for every job:

- Size: a pose is scaled to the approved figure by parts whose size does not change with the pose: the round gold
  discs (clasps, rivets, buckles, studs). Their long axes are measured on the approved raw and on the pose, and the
  ratio of the medians of the largest few is the scale (round 19 measured the valkyrie's by hand, 1.04 and 1.08; this
  measures 1.03 and 1.09 on the same images). The archmage has no gold disc (her earrings are rings): her staff's red
  crystal is measured instead (RIGID). A pose whose measure is overruled after looking at it takes its scale from SCALE.
- Place: everything stands on one canvas three figure canvases wide with room under the floor line (2016x1008: round
  19's 1344 was too narrow for the archmage's staff and the spellblade's thrust): the approved figure where its own
  canvas (672x896) would be, in the middle, and a pose so that its back foot stays where the
  approved figure's back foot is and its soles stand on the floor line. The floor line is the soles, not the lowest
  point: a weapon may come down below it, in front of the feet. The back foot is found as the leftmost boot (the leather
  of the boots in the lowest part of the figure) and the floor line is its sole: the back foot stands on the floor in a
  lunge and in a recoil alike, while the front foot may lift and a brown haft may come down beside it (FEET overrides).
- Colors: each main color of the pose (the approved raw's own, found by quantizing it) is moved by the difference of its
  mean from the approved figure's, the ink and the white held where they are; kept only when it brings the worst color
  nearer (CIE76 delta E).
- The ring around the silhouette is the one gen_image.py gives (FIGURE_OUTLINE, UI_LINE).
Writes output/character/<key>_idle_wide.png and, per pose, <name>.matched.png and <name>_wide.png, and prints what it
measured (fit.txt keeps the last run).
"""
import colorsys
import math
import sys
from pathlib import Path
from PIL import Image, ImageFilter

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
sys.path.insert(0, str(ROOT / "ArtPipeline" / "tools"))
sys.path.insert(0, str(ROOT / "ArtPipeline/Archive/19-motion-test"))
from gen_image import ALPHA_FLOOR, FIGURE_CANVAS, FIGURE_FLOOR_MARGIN, FIGURE_SIDE_MARGIN, FIGURE_OUTLINE, UI_LINE, grow, subject_box  # noqa: E402
from color_drift import lab  # noqa: E402

OUTPUT = ROOT / "ArtPipeline/output/character"
APPROVED = {
    "knight": ROOT / "ArtPipeline/Archive/20-serious-face/approved/character/knight.raw.png",
    "valkyrie": ROOT / "ArtPipeline/Archive/20-serious-face/approved/character/valkyrie.raw.png",
    "bishop": ROOT / "ArtPipeline/Archive/20-serious-face/approved/character/bishop.raw.png",
    "paladin": ROOT / "ArtPipeline/Archive/22-paladin-head/approved/character/paladin.raw.png",
    "archmage": ROOT / "ArtPipeline/Archive/20-serious-face/approved/character/archmage.raw.png",
    "spellblade": ROOT / "ArtPipeline/Archive/20-serious-face/approved/character/spellblade.raw.png",
}
POSES = ("attack", "hit")
BELOW = 112
SIDE = FIGURE_CANVAS[0]               # room beside the figure canvas, on each side
WIDE = (FIGURE_CANVAS[0] + 2 * SIDE, FIGURE_CANVAS[1] + BELOW)
FLOOR = FIGURE_CANVAS[1] - FIGURE_FLOOR_MARGIN
DISCS = 4                    # how many of the largest discs are compared
NEAR = 26                    # a pixel belongs to a main color within this distance (RGB)
HELD = [(40, 22, 15), (255, 255, 255)]
# Overrides after looking at a pose: SCALE[name] = scale; FEET[name] = (back window, [sole windows]) on its raw.
# The spellblade has no round gold part: his scale is the long axis of his boots' square gold buckles (idle 41 and 40,
# attack 38 and 37, hit 42 and 40); his ears then stand the same height in the three (38 on the canvas).
# The paladin's hit turns his shoulders away, so their discs measure small (0.877): his belt buckle (69 to 75) and his
# head from the hair to the tip of the beard on the raws (185 to 200) both give 0.92.
SCALE = {"spellblade_attack": 0.93, "spellblade_hit": 1.0, "paladin_hit": 0.92}
# A pose drawn again after the verdict (user 2026-10-04: the paladin's attack had swapped hands) stands in for the first.
DRAWN = {"paladin_attack": "paladin_attack2"}


def drawn(key, pose):
    """The name of the picture a pose of a mercenary is now."""
    return DRAWN.get(f"{key}_{pose}", f"{key}_{pose}")
FEET = {}


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


def red(r, g, b):
    h, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
    return (h <= 0.03 or h >= 0.95) and s >= 0.55 and v >= 0.40


def crystal(image):
    """The long axis of the largest red area: the archmage's crystal."""
    best = max(blobs(image, red, 200, 40000), key=len)
    n = len(best)
    mx, my = sum(p[0] for p in best) / n, sum(p[1] for p in best) / n
    cxx = sum((p[0] - mx) ** 2 for p in best) / n
    cyy = sum((p[1] - my) ** 2 for p in best) / n
    cxy = sum((p[0] - mx) * (p[1] - my) for p in best) / n
    root = math.sqrt(max(0.0, (cxx + cyy) ** 2 / 4 - (cxx * cyy - cxy * cxy)))
    return [4 * math.sqrt((cxx + cyy) / 2 + root)]


# The parts a job's scale is measured on, when they are not its gold discs.
RIGID = {"archmage": crystal}


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
    under.putalpha(grow(body, FIGURE_OUTLINE).filter(ImageFilter.GaussianBlur(0.8)))
    under.alpha_composite(canvas)
    return under


def place(raw, ratio, origin, box):
    subject = raw.crop(box)
    subject = subject.resize((round(subject.width * ratio), round(subject.height * ratio)), Image.LANCZOS)
    canvas = Image.new("RGBA", WIDE, (0, 0, 0, 0))
    canvas.paste(subject, origin)
    return ring(canvas)


def fit(key, log):
    approved = Image.open(APPROVED[key]).convert("RGBA")
    box = subject_box(approved)
    width, height = box[2] - box[0], box[3] - box[1]
    # The approved figure's own fit (gen_image.fit_figure): Height 90, bound by the canvas width, its canvas in the middle.
    ratio = min(round(FIGURE_CANVAS[1] * 0.9) / height, (FIGURE_CANVAS[0] - 2 * FIGURE_SIDE_MARGIN) / width)
    x = (FIGURE_CANVAS[0] - round(width * ratio)) // 2 + SIDE
    leather = boot_colour(approved)
    back, soles, _ = feet(approved, leather)
    foot = x + (back - box[0]) * ratio
    origin = (x, FLOOR - round((soles - box[1]) * ratio))
    place(approved, ratio, origin, box).save(OUTPUT / f"{key}_idle_wide.png")
    rigid = RIGID.get(key, discs)
    approved_discs = rigid(approved)
    main = main_colours(approved)
    approved_means = means(approved, main)
    log.append(f"{key}: idle ratio {ratio:.4f}, boots {leather}, back foot x {back:.0f}, soles y {soles}, "
               f"discs {[round(d) for d in approved_discs[:DISCS]]}, {len(main)} main colours")

    for pose in POSES:
        name = drawn(key, pose)
        raw = Image.open(OUTPUT / f"{name}.raw.png").convert("RGBA")
        pose_discs = rigid(raw)
        k = min(DISCS, len(pose_discs), len(approved_discs))
        measured = median(pose_discs[:k]) / median(approved_discs[:k]) if k >= (1 if key in RIGID else 2) else None
        scale = SCALE.get(name, measured)
        if scale is None:
            raise SystemExit(f"실패: {name} 의 배율을 잴 금 장식이 없다. SCALE 에 적는다")
        b = subject_box(raw)
        if name in FEET:
            (bx0, by0, bx1, by1), windows = FEET[name]
            pose_back = (bx0 + bx1) / 2
            pose_soles = max(w[3] for w in windows)
        else:
            pose_back, pose_soles, _ = feet(raw, leather)
        r = ratio / scale
        origin = (round(foot - (pose_back - b[0]) * r), FLOOR - round((pose_soles - b[1]) * r))

        matched = match_colours(raw, main, approved_means)
        before, after = worst(means(raw, main), approved_means), worst(means(matched, main), approved_means)
        use = matched if after < before else raw
        matched.save(OUTPUT / f"{name}.matched.png")
        place(use, r, origin, b).save(OUTPUT / f"{name}_wide.png")
        below = origin[1] + round((b[3] - b[1]) * r) - FLOOR
        right = origin[0] + round((b[2] - b[0]) * r)
        if origin[0] < 0 or origin[1] < 0 or right > WIDE[0] or FLOOR + below > WIDE[1]:
            log.append(f"  경고: {name} 이 넓은 캔버스 밖으로 나간다")
        log.append(f"  {name}: discs {[round(d) for d in pose_discs[:DISCS]]} -> scale {measured if measured is None else round(measured, 3)}"
                   f"{' (SCALE ' + str(scale) + ')' if name in SCALE else ''}, back foot x {pose_back:.0f}, soles y {pose_soles}, "
                   f"colours worst dE {before:.1f} -> {after:.1f} {'moved' if after < before else 'as drawn'}, "
                   f"subject {origin[0]}..{right} x {origin[1]}..{FLOOR + below} ({below:+d} below the floor) on {WIDE[0]}x{WIDE[1]}")


def main():
    keys = sys.argv[1:] or list(APPROVED)
    log = []
    for key in keys:
        fit(key, log)
        print("\n".join(log[-3:]))
    (HERE / "fit.txt").write_text("\n".join(log) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
