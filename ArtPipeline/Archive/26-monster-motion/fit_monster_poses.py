"""Stands the goblin raider's attack and hit poses at the size and place of its figure in the game (round 26), no API call.
  .venv/bin/python ArtPipeline/Archive/26-monster-motion/fit_monster_poses.py   # candidates/*_wide.png, fit.txt

Round 23's fit (ArtPipeline/tools/fit_pose.py) turned for a creature that faces left and goes barefoot:
- Size: the raider has no gold disc. The scale of a pose against the approved raw is the median of five parts whose size
  does not change with the pose: the face (the largest area of skin in the upper part), the planted back foot, and three
  pieces of gear found from a point inside each (SEEDS): the blade, the dome of the cap and the candle's wax. Each is the
  square root of its area against the approved raw's. The eyes are left out (an expression changes them) and so is the
  lantern (it swings and turns). A roster's Scale (attack.csv, hit.csv) replaces the median where one is written.
- Place: the back foot of a creature that faces left is its rightmost foot. The pose's back foot (the rightmost area of
  skin at the bottom of the figure) stands where the game figure's back foot is, its sole on that foot's sole. The game
  figure is placed as gen_image.fit_figure places it (the roster's Height, bound by the canvas width), in the middle of
  the pose canvas (three figure canvases wide and an eighth deeper, 2016x1008).
- Colours and the ring: fit_pose's (the approved raw's main colours, moved only when the worst comes nearer; FIGURE_OUTLINE).
Writes candidates/<key>_<pose>_wide.png, candidates/<key>_idle_wide.png (the game's figure on the pose canvas), copies the
raws to candidates/ and the measures to fit.txt.
"""
import colorsys
import csv
import math
import shutil
import sys
from pathlib import Path
from PIL import Image

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
sys.path.insert(0, str(ROOT / "ArtPipeline" / "tools"))
from gen_image import FIGURE_CANVAS, FIGURE_FLOOR_MARGIN, FIGURE_SIDE_MARGIN, subject_box  # noqa: E402
from fit_pose import SIDE, WIDE, blobs, main_colours, match_colours, means, place, worst  # noqa: E402

KEY = "goblin_raider"
APPROVED = ROOT / "ArtPipeline/Archive/17-outline-pupils/approved/enemy/goblin_raider.raw.png"
FIGURE = ROOT / "Assets/@Art/Unit/Enemy/goblin_raider.png"
RAWS = ROOT / "ArtPipeline/output/enemy"
POSES = ("attack", "hit")
INK_LUMA = 55          # a part is filled inside its ink outline: anything darker than this is ink
# A point inside each piece of gear, in each raw (found by looking, 2026-10-05). A point that falls on ink is skipped.
SEEDS = {
    "blade": {"approved": [(145, 329), (110, 300)], "attack": [(300, 800)], "hit": [(600, 240)]},
    "cap": {"approved": [(545, 209)], "attack": [(760, 180)], "hit": [(1060, 160), (1040, 140), (1080, 200)]},
    "wax": {"approved": [(455, 130), (470, 160), (460, 145)], "attack": [(632, 120)], "hit": [(1010, 90)]},
}


def roster_height():
    with (ROOT / "ArtPipeline/Rosters/enemy.csv").open(encoding="utf-8", newline="") as handle:
        return {row["Key"]: int(row["Height"]) for row in csv.DictReader(handle)}[KEY]


def roster_scales(pose):
    with (HERE / f"{pose}.csv").open(encoding="utf-8", newline="") as handle:
        return {row["Key"]: float(row["Scale"]) for row in csv.DictReader(handle) if (row.get("Scale") or "").strip()}


def skin_colour(image):
    """The skin: the commonest mid-tone grey-green of the figure."""
    counts = {}
    for r, g, b, a in image.getdata():
        if a <= 200:
            continue
        h, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
        if 0.10 <= h <= 0.25 and 0.15 <= s <= 0.50 and 0.40 <= v <= 0.70:
            key = (r // 8 * 8 + 4, g // 8 * 8 + 4, b // 8 * 8 + 4)
            counts[key] = counts.get(key, 0) + 1
    return max(counts, key=counts.get)


def skin_areas(image, skin):
    near = lambda r, g, b: (r - skin[0]) ** 2 + (g - skin[1]) ** 2 + (b - skin[2]) ** 2 < 32 * 32
    out = []
    for pts in blobs(image, near, 1500, 400000):
        xs, ys = [p[0] for p in pts], [p[1] for p in pts]
        out.append({"area": len(pts), "cx": sum(xs) / len(xs), "box": (min(xs), min(ys), max(xs) + 1, max(ys) + 1)})
    return out


def face_and_back_foot(image, skin):
    """The face (the largest skin area whose middle is in the upper half) and the back foot (the rightmost large skin area
    whose bottom is in the lowest tenth of the figure: a lifted front foot is above it)."""
    box = subject_box(image)
    height = box[3] - box[1]
    areas = skin_areas(image, skin)
    face = max((a for a in areas if (a["box"][1] + a["box"][3]) / 2 < box[1] + height / 2), key=lambda a: a["area"])
    feet = [a for a in areas if a["area"] >= 3000 and a["box"][3] >= box[3] - height / 10]
    if not feet:
        raise SystemExit("실패: 바닥의 발을 찾지 못했다")
    return face, max(feet, key=lambda a: a["cx"])


def part_area(image, seeds):
    """A part inside its ink outline, filled from the given points, its holes (scribbles inside it) filled too."""
    px = image.load()
    w, h = image.size
    ok = lambda x, y: px[x, y][3] > 200 and 0.299 * px[x, y][0] + 0.587 * px[x, y][1] + 0.114 * px[x, y][2] > INK_LUMA
    seen = {s for s in seeds if ok(*s)}
    stack = list(seen)
    while stack:
        x, y = stack.pop()
        for n in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
            if 0 <= n[0] < w and 0 <= n[1] < h and n not in seen and ok(*n):
                seen.add(n)
                stack.append(n)
    if not seen:
        return None
    xs, ys = [p[0] for p in seen], [p[1] for p in seen]
    x0, y0, x1, y1 = min(xs) - 1, min(ys) - 1, max(xs) + 1, max(ys) + 1
    outside, stack = {(x0, y0)}, [(x0, y0)]
    while stack:
        x, y = stack.pop()
        for n in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
            if x0 <= n[0] <= x1 and y0 <= n[1] <= y1 and n not in outside and n not in seen:
                outside.add(n)
                stack.append(n)
    return (x1 - x0 + 1) * (y1 - y0 + 1) - len(outside)


def median(values):
    values = sorted(values)
    return (values[(len(values) - 1) // 2] + values[len(values) // 2]) / 2


def game_place(approved):
    """Where the game's figure puts the approved raw: its ratio and the top left of its subject on the pose canvas."""
    box = subject_box(approved)
    width, height = box[2] - box[0], box[3] - box[1]
    target = round(FIGURE_CANVAS[1] * roster_height() / 100)
    ratio = min(target / height, (FIGURE_CANVAS[0] - 2 * FIGURE_SIDE_MARGIN) / width, 1.0)
    x = (FIGURE_CANVAS[0] - round(width * ratio)) // 2 + SIDE
    y = FIGURE_CANVAS[1] - FIGURE_FLOOR_MARGIN - round(height * ratio)
    return box, ratio, (x, y)


def main():
    out = HERE / "candidates"
    out.mkdir(exist_ok=True)
    log = []
    approved = Image.open(APPROVED).convert("RGBA")
    skin = skin_colour(approved)
    box, ratio, (ax, ay) = game_place(approved)
    face, foot = face_and_back_foot(approved, skin)
    foot_x = ax + (foot["cx"] - box[0]) * ratio
    sole_y = ay + (foot["box"][3] - box[1]) * ratio
    log.append(f"approved {APPROVED.relative_to(ROOT)}: skin {skin}, game figure x{ratio:.4f} at ({ax}, {ay}) on the pose canvas, "
               f"back foot x {foot_x:.1f}, sole {sole_y:.1f}")
    approved_parts = {"face": face["area"], "back foot": foot["area"]}
    approved_parts.update({name: part_area(approved, seeds["approved"]) for name, seeds in SEEDS.items()})

    idle = Image.new("RGBA", WIDE, (0, 0, 0, 0))
    idle.paste(Image.open(FIGURE).convert("RGBA"), (SIDE, 0))
    idle.save(out / f"{KEY}_idle_wide.png")

    main_cols = main_colours(approved)
    approved_means = means(approved, main_cols)
    for pose in POSES:
        raw_path = RAWS / f"{KEY}_{pose}.raw.png"
        shutil.copyfile(raw_path, out / raw_path.name)
        raw = Image.open(raw_path).convert("RGBA")
        b = subject_box(raw)
        p_face, p_foot = face_and_back_foot(raw, skin)
        parts = {"face": p_face["area"], "back foot": p_foot["area"]}
        parts.update({name: part_area(raw, seeds[pose]) for name, seeds in SEEDS.items()})
        ratios = {name: math.sqrt(parts[name] / approved_parts[name]) for name in parts if parts[name] and approved_parts[name]}
        measured = median(list(ratios.values()))
        scale = roster_scales(pose).get(KEY, measured)
        r = ratio / scale
        origin = (round(foot_x - (p_foot["cx"] - b[0]) * r), round(sole_y - (p_foot["box"][3] - b[1]) * r))
        matched = match_colours(raw, main_cols, approved_means)
        before, after = worst(means(raw, main_cols), approved_means), worst(means(matched, main_cols), approved_means)
        place(matched if after < before else raw, r, origin, b).save(out / f"{KEY}_{pose}_wide.png")
        right = origin[0] + round((b[2] - b[0]) * r)
        bottom = origin[1] + round((b[3] - b[1]) * r)
        log.append(f"{pose}: " + ", ".join(f"{name} {value:.3f}" for name, value in ratios.items())
                   + f" -> median {measured:.3f}{' (Scale ' + str(scale) + ')' if scale != measured else ''}, "
                   f"colours worst dE {before:.1f} -> {after:.1f} {'moved' if after < before else 'as drawn'}, "
                   f"x {origin[0]}..{right}, y {origin[1]}..{bottom} (floor {FIGURE_CANVAS[1] - FIGURE_FLOOR_MARGIN})")
        if origin[0] < 0 or origin[1] < 0 or right > WIDE[0] or bottom > WIDE[1]:
            log.append(f"  경고: {pose} 가 자세 캔버스({WIDE[0]}x{WIDE[1]}) 밖으로 나간다")
    (HERE / "fit.txt").write_text("\n".join(log) + "\n", encoding="utf-8")
    print("\n".join(log))


if __name__ == "__main__":
    main()
