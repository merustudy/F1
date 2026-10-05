"""Stands the four other monsters' attack and hit poses at the size and place of their figures in the game (round 27),
no API call.
  .venv/bin/python ArtPipeline/Archive/27-monster-poses/fit_monster_poses.py           # candidates/*_wide.png, fit.txt
  .venv/bin/python ArtPipeline/Archive/27-monster-poses/fit_monster_poses.py --debug   # + output/monster_fit/<key>.png

Round 26's fit (../26-monster-motion/fit_monster_poses.py, the goblin raider's two poses, "확정") for creatures that differ
more: a beast on all fours (the cave rat: pink paws, a tail, no gear), a goblin with a bow, a goblin with a staff and a
booted boss.
- Feet: a creature that faces left plants its rightmost foot. The colour of the feet is the commonest light colour in the
  lowest twelfth of the approved figure, or of a taller strip where that one holds only dark claws (the skin of bare
  feet, the rat's paws); the overseer's dark boots are given (FOOT_COLOUR). The back foot is the rightmost area of that
  colour in the lowest sixth of the figure above its floor (the bottom of its right half: a weapon that comes down below
  the feet does so in front, on the left, as the overseer's maul), its inside ink lines bridged; a lifted front foot is
  on the left too, the rat's tail is higher.
- Size: the median of the parts that keep their size whatever the pose, each the square root of its area against the
  approved raw's: the back foot, and parts filled inside their ink outline from a point (PARTS: the approved raw's point
  is exact; a pose's point is a guess the fill starts from the nearest pixel of the part's colour around it).
  What each monster has: the rat its nose and near ear (its teeth part differently in each picture, its far ear is open to
  the fur), the archer its face, the shaman its candle's wax and its round tin flask, the overseer its face. Left out
  because they measured apart from the rest or did not fill as one part in every picture: cloth (the archer's hood),
  parts that turn (the overseer's shoulder plate and maul head), lanterns (they swing, their panes split differently),
  small parts (the archer's quiver and belt ring, the overseer's tusks and strap rings). Where only the face and the
  foot are left (the archer, the overseer) the median is their mean, and the review sheet's heads decide: a roster's
  Scale (attack.csv, hit.csv) replaces the median where one is written.
- Place, colours, the ring and the canvas: round 26's (the game figure as gen_image.fit_figure places it, the back foot
  on its back foot, fit_pose's colour match and ring, 2016x1008).
Writes candidates/<key>_<pose>_wide.png, candidates/<key>_idle_wide.png, copies the raws to candidates/, fit.txt, and
heads.json (where each head is on the pose canvas, for the review sheet).
"""
import argparse
import csv
import json
import math
import shutil
import sys
from pathlib import Path
from PIL import Image, ImageFilter

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
sys.path.insert(0, str(ROOT / "ArtPipeline" / "tools"))
from gen_image import FIGURE_CANVAS, FIGURE_FLOOR_MARGIN, FIGURE_SIDE_MARGIN, subject_box  # noqa: E402
from fit_pose import SIDE, WIDE, blobs, main_colours, match_colours, means, place, worst  # noqa: E402

APPROVED = ROOT / "ArtPipeline/Archive/17-outline-pupils/approved/enemy"
FIGURES = ROOT / "Assets/@Art/Unit/Enemy"
RAWS = ROOT / "ArtPipeline/output/enemy"
DEBUG = ROOT / "ArtPipeline/output/monster_fit"
POSES = {"cave_rat": ("attack", "hit"), "goblin_archer": ("attack", "hit"), "goblin_shaman": ("hit",), "mine_overseer": ("attack", "hit")}
INK_LUMA = 55          # a part is filled inside its ink outline: anything darker than this is ink
FOOT_LUMA = 90         # the feet's colour is a light one: not the dark claws, soles or shading
NEAR = 32              # a pixel is of a colour within this distance (RGB)
REACH = 45             # how far from a pose's point the part's colour is looked for
BRIDGE = 9             # the feet's mask grows by this filter to close the ink lines drawn inside a foot
# The overseer's boots are a dark brown, darker than the light colour the rule looks for (that finds the tan wraps on its
# shins): its feet are this colour, read off the boots of its approved raw.
FOOT_COLOUR = {"mine_overseer": (76, 52, 44)}
# A point inside each part in each raw (found by looking, 2026-10-05).
PARTS = {
    "cave_rat": {"nose": {"approved": (100, 495), "attack": (270, 482), "hit": (510, 272)},
                 "near ear": {"approved": (430, 413), "attack": (660, 424), "hit": (884, 300)}},
    "goblin_archer": {"face": {"approved": (550, 375), "attack": (790, 380), "hit": (960, 330)}},
    "goblin_shaman": {"wax": {"approved": (450, 208), "hit": (980, 195)},
                      "flask": {"approved": (750, 733), "hit": (880, 660)}},
    "mine_overseer": {"face": {"approved": (333, 367), "attack": (690, 240), "hit": (820, 250)}},
}


# Where the head is, for the review sheet's row of heads: the head's parts above, and for the shaman its face (its face
# fills into its ear and beard, so it is a place and not a measure).
HEAD = {"cave_rat": ("nose", "near ear"), "goblin_archer": ("face",), "goblin_shaman": ("face",), "mine_overseer": ("face",)}
ANCHORS = {"goblin_shaman": {"face": {"approved": (475, 358), "hit": (970, 320)}}}


def head_box(boxes):
    """The box around the head's parts: (left, top, right, bottom)."""
    return (min(b[0] for b in boxes), min(b[1] for b in boxes), max(b[2] for b in boxes), max(b[3] for b in boxes))


def mapped(box, origin, start, ratio):
    """A box of a raw on the pose canvas, where the raw's subject starting at start is drawn at origin with ratio."""
    return [round(origin[0] + (box[0] - start[0]) * ratio), round(origin[1] + (box[1] - start[1]) * ratio),
            round(origin[0] + (box[2] - start[0]) * ratio), round(origin[1] + (box[3] - start[1]) * ratio)]


def pixels_box(pts):
    xs, ys = [p[0] for p in pts], [p[1] for p in pts]
    return (min(xs), min(ys), max(xs) + 1, max(ys) + 1)


def roster_heights():
    with (ROOT / "ArtPipeline/Rosters/enemy.csv").open(encoding="utf-8", newline="") as handle:
        return {row["Key"]: int(row["Height"]) for row in csv.DictReader(handle)}


def roster_scales(pose):
    with (HERE / f"{pose}.csv").open(encoding="utf-8", newline="") as handle:
        return {row["Key"]: float(row["Scale"]) for row in csv.DictReader(handle) if (row.get("Scale") or "").strip()}


def luma(r, g, b):
    return 0.299 * r + 0.587 * g + 0.114 * b


def foot_colour(image):
    """The commonest light colour of the lowest twelfth of the figure, or of a taller strip where that one holds only
    dark claws (the rat)."""
    box = subject_box(image)
    for share in (12, 8, 6):
        band = image.crop((box[0], box[3] - (box[3] - box[1]) // share, box[2], box[3]))
        counts = {}
        for r, g, b, a in band.getdata():
            if a > 200 and luma(r, g, b) > FOOT_LUMA:
                key = (r // 8 * 8 + 4, g // 8 * 8 + 4, b // 8 * 8 + 4)
                counts[key] = counts.get(key, 0) + 1
        if sum(counts.values()) >= 200:
            return max(counts, key=counts.get)
    raise SystemExit("실패: 발의 색을 찾지 못했다")


def back_foot(image, colour):
    """The rightmost area of the feet's colour in the lowest sixth of the figure above its floor. The ink lines drawn
    inside a foot (toes, folds of a boot) are bridged first by growing the colour's mask a little; the area counts the
    pixels of the colour only."""
    px = image.load()
    w, h = image.size
    box = subject_box(image)
    # The floor is the bottom of the right half of the figure, where the back foot is: a weapon that comes down below the
    # feet does so in front, on the left (the overseer's maul).
    right = image.crop(((box[0] + box[2]) // 2, box[1], box[2], box[3])).getchannel("A").point(lambda v: 255 if v > 200 else 0)
    floor = box[1] + right.getbbox()[3]
    low = floor - (floor - box[1]) // 6
    # Only the lowest sixth is looked at, so that the colour higher up (the overseer's straps, bands and hair are its
    # boots' brown) is not bridged to the feet.
    mask = Image.new("L", image.size, 0)
    mp = mask.load()
    for y in range(low, floor):
        for x in range(box[0], box[2]):
            r, g, b, a = px[x, y]
            if a > 200 and (r - colour[0]) ** 2 + (g - colour[1]) ** 2 + (b - colour[2]) ** 2 < NEAR * NEAR:
                mp[x, y] = 255
    grown = mask.filter(ImageFilter.MaxFilter(BRIDGE)).load()
    seen, feet = bytearray(w * h), []
    for y0 in range(low, floor):
        for x0 in range(box[0], box[2]):
            if seen[y0 * w + x0] or not grown[x0, y0]:
                continue
            stack, pts = [(x0, y0)], []
            seen[y0 * w + x0] = 1
            while stack:
                x, y = stack.pop()
                if mp[x, y]:
                    pts.append((x, y))
                for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
                    if 0 <= nx < w and 0 <= ny < h and not seen[ny * w + nx] and grown[nx, ny]:
                        seen[ny * w + nx] = 1
                        stack.append((nx, ny))
            if len(pts) >= 1200:
                xs, ys = [p[0] for p in pts], [p[1] for p in pts]
                feet.append({"area": len(pts), "cx": sum(xs) / len(xs), "bottom": max(ys) + 1, "pts": pts})
    if not feet:
        raise SystemExit("실패: 발을 찾지 못했다")
    return max(feet, key=lambda f: f["cx"])


def part(image, point, colour=None):
    """A part filled inside its ink outline from a point; with a colour, from the pixel of that colour nearest the point.
    Returns (area with its holes filled, the pixels, the point it started from)."""
    px = image.load()
    w, h = image.size
    ok = lambda x, y: px[x, y][3] > 200 and luma(*px[x, y][:3]) > INK_LUMA
    start = point
    if colour is not None:
        best = None
        for y in range(point[1] - REACH, point[1] + REACH + 1):
            for x in range(point[0] - REACH, point[0] + REACH + 1):
                if 0 <= x < w and 0 <= y < h and ok(x, y):
                    r, g, b = px[x, y][:3]
                    d = (r - colour[0]) ** 2 + (g - colour[1]) ** 2 + (b - colour[2]) ** 2
                    if d < NEAR * NEAR:
                        far = (x - point[0]) ** 2 + (y - point[1]) ** 2
                        if best is None or far < best[0]:
                            best = (far, (x, y))
        if best is None:
            return None, set(), point
        start = best[1]
    if not ok(*start):
        return None, set(), start
    seen, stack = {start}, [start]
    while stack:
        x, y = stack.pop()
        for n in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
            if 0 <= n[0] < w and 0 <= n[1] < h and n not in seen and ok(*n):
                seen.add(n)
                stack.append(n)
    xs, ys = [p[0] for p in seen], [p[1] for p in seen]
    x0, y0, x1, y1 = min(xs) - 1, min(ys) - 1, max(xs) + 1, max(ys) + 1
    outside, stack = {(x0, y0)}, [(x0, y0)]
    while stack:
        x, y = stack.pop()
        for n in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
            if x0 <= n[0] <= x1 and y0 <= n[1] <= y1 and n not in outside and n not in seen:
                outside.add(n)
                stack.append(n)
    return (x1 - x0 + 1) * (y1 - y0 + 1) - len(outside), seen, start


def mean_colour(image, pts):
    px = image.load()
    n = len(pts)
    return tuple(sum(px[p][i] for p in pts) / n for i in range(3))


def median(values):
    values = sorted(values)
    return (values[(len(values) - 1) // 2] + values[len(values) // 2]) / 2


def game_place(approved, height_percent):
    """Where the game's figure puts the approved raw: its ratio and the top left of its subject on the pose canvas."""
    box = subject_box(approved)
    width, height = box[2] - box[0], box[3] - box[1]
    target = round(FIGURE_CANVAS[1] * height_percent / 100)
    ratio = min(target / height, (FIGURE_CANVAS[0] - 2 * FIGURE_SIDE_MARGIN) / width, 1.0)
    x = (FIGURE_CANVAS[0] - round(width * ratio)) // 2 + SIDE
    y = FIGURE_CANVAS[1] - FIGURE_FLOOR_MARGIN - round(height * ratio)
    return box, ratio, (x, y)


def debug_sheet(key, layers):
    """The approved raw and the poses with what was measured painted over them."""
    DEBUG.mkdir(parents=True, exist_ok=True)
    tiles = []
    for image, painted in layers:
        bg = Image.new("RGBA", image.size, (150, 140, 128, 255))
        bg.alpha_composite(image)
        for pts, colour in painted:
            layer = Image.new("RGBA", image.size, (0, 0, 0, 0))
            d = layer.load()
            for p in pts:
                d[p] = colour
            bg.alpha_composite(layer)
        tiles.append(bg.convert("RGB").resize((bg.width * 2 // 5, bg.height * 2 // 5)))
    sheet = Image.new("RGB", (sum(t.width + 10 for t in tiles), max(t.height for t in tiles)), (20, 20, 20))
    x = 0
    for t in tiles:
        sheet.paste(t, (x, 0))
        x += t.width + 10
    sheet.save(DEBUG / f"{key}.png")


def fit_monster(key, height_percent, debug, log, places):
    approved = Image.open(APPROVED / f"{key}.raw.png").convert("RGBA")
    colour = FOOT_COLOUR.get(key) or foot_colour(approved)
    box, ratio, (ax, ay) = game_place(approved, height_percent)
    foot = back_foot(approved, colour)
    foot_x = ax + (foot["cx"] - box[0]) * ratio
    sole_y = ay + (foot["bottom"] - box[1]) * ratio
    log.append(f"{key}: Height {height_percent}, feet {colour}, game figure x{ratio:.4f} at ({ax}, {ay}), back foot x {foot_x:.1f}, sole {sole_y:.1f}")
    paint = [(255, 0, 255, 160), (0, 255, 255, 160), (255, 255, 0, 160)]
    approved_parts, part_colours, layers = {"back foot": foot["area"]}, {}, [(approved, [(foot["pts"], (0, 255, 0, 160))])]
    head_boxes = {}
    for i, (name, points) in enumerate(list(PARTS[key].items()) + list(ANCHORS.get(key, {}).items())):
        area, pts, _ = part(approved, points["approved"])
        if name in PARTS[key]:
            approved_parts[name] = area
            layers[0][1].append((pts, paint[i]))
        part_colours[name] = mean_colour(approved, pts)
        head_boxes[name] = pixels_box(pts)
    places[key] = {"idle": mapped(head_box([head_boxes[n] for n in HEAD[key]]), (ax, ay), box[:2], ratio), "foot": round(foot_x, 1)}

    idle = Image.new("RGBA", WIDE, (0, 0, 0, 0))
    idle.paste(Image.open(FIGURES / f"{key}.png").convert("RGBA"), (SIDE, 0))
    idle.save(HERE / "candidates" / f"{key}_idle_wide.png")
    main_cols = main_colours(approved)
    approved_means = means(approved, main_cols)
    for pose in POSES[key]:
        raw_path = RAWS / f"{key}_{pose}.raw.png"
        shutil.copyfile(raw_path, HERE / "candidates" / raw_path.name)
        raw = Image.open(raw_path).convert("RGBA")
        b = subject_box(raw)
        p_foot = back_foot(raw, colour)
        parts, painted, head_boxes = {"back foot": p_foot["area"]}, [(p_foot["pts"], (0, 255, 0, 160))], {}
        for i, (name, points) in enumerate(list(PARTS[key].items()) + list(ANCHORS.get(key, {}).items())):
            area, pts, start = part(raw, points[pose], part_colours[name])
            if name in PARTS[key]:
                parts[name] = area
                painted.append((pts, paint[i]))
            if pts:
                head_boxes[name] = pixels_box(pts)
        layers.append((raw, painted))
        ratios = {name: math.sqrt(parts[name] / approved_parts[name]) for name in parts if parts[name]}
        measured = median(list(ratios.values()))
        scale = roster_scales(pose).get(key, measured)
        r = ratio / scale
        origin = (round(foot_x - (p_foot["cx"] - b[0]) * r), round(sole_y - (p_foot["bottom"] - b[1]) * r))
        matched = match_colours(raw, main_cols, approved_means)
        before, after = worst(means(raw, main_cols), approved_means), worst(means(matched, main_cols), approved_means)
        place(matched if after < before else raw, r, origin, b).save(HERE / "candidates" / f"{key}_{pose}_wide.png")
        places[key][pose] = mapped(head_box([head_boxes[n] for n in HEAD[key]]), origin, b[:2], r)
        right, bottom = origin[0] + round((b[2] - b[0]) * r), origin[1] + round((b[3] - b[1]) * r)
        log.append(f"  {pose}: " + ", ".join(f"{name} {value:.3f}" for name, value in ratios.items())
                   + f" -> median {measured:.3f}{' (Scale ' + str(scale) + ')' if scale != measured else ''}, "
                   f"colours worst dE {before:.1f} -> {after:.1f} {'moved' if after < before else 'as drawn'}, "
                   f"x {origin[0]}..{right}, y {origin[1]}..{bottom} (floor {FIGURE_CANVAS[1] - FIGURE_FLOOR_MARGIN})")
        if origin[0] < 0 or origin[1] < 0 or right > WIDE[0] or bottom > WIDE[1]:
            log.append(f"  경고: {key} {pose} 가 자세 캔버스({WIDE[0]}x{WIDE[1]}) 밖으로 나간다")
    if debug:
        debug_sheet(key, layers)


def main():
    parser = argparse.ArgumentParser(description="Stand the monsters' poses at their game figures' size. No API call.")
    parser.add_argument("--key", dest="keys", action="append", choices=sorted(POSES), help="Default: all four.")
    parser.add_argument("--debug", action="store_true", help="Also paint what was measured: output/monster_fit/<key>.png")
    args = parser.parse_args()
    (HERE / "candidates").mkdir(exist_ok=True)
    heights, log, places = roster_heights(), [], {}
    for key in args.keys or POSES:
        fit_monster(key, heights[key], args.debug, log, places)
    if not args.keys:
        (HERE / "fit.txt").write_text("\n".join(log) + "\n", encoding="utf-8")
        (HERE / "heads.json").write_text(json.dumps(places, indent=1) + "\n", encoding="utf-8")
    print("\n".join(log))


if __name__ == "__main__":
    main()
