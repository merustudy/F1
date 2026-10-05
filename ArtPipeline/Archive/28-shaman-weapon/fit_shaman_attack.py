"""Stands the goblin shaman's attack pose at the size and place of its figure in the game (round 28), no API call.
  .venv/bin/python ArtPipeline/Archive/28-shaman-weapon/fit_shaman_attack.py   # candidates/goblin_shaman_attack_wide.png, fit.txt

Round 27's fit (../27-monster-poses/fit_monster_poses.py) for the one pose drawn here: the back foot (the rightmost bare
foot, its inside ink lines bridged), the candle's wax and the round tin flask (the parts round 27 measured the shaman's
hit with) for the scale, the face as the head's place for the review sheet; the game figure's place, the colour match
and the ring as round 27's.
"""
import csv
import json
import math
import shutil
import sys
from pathlib import Path
from PIL import Image

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
sys.path.insert(0, str(ROOT / "ArtPipeline/Archive/27-monster-poses"))
import fit_monster_poses as F  # noqa: E402

KEY = "goblin_shaman"
# A point inside each part in the attack raw (found by looking, 2026-10-05); the approved raw's are round 27's.
POINTS = {"wax": (740, 130), "flask": (1150, 670), "face": (790, 280)}


def main():
    (HERE / "candidates").mkdir(exist_ok=True)
    approved = Image.open(F.APPROVED / f"{KEY}.raw.png").convert("RGBA")
    colour = F.foot_colour(approved)
    box, ratio, (ax, ay) = F.game_place(approved, F.roster_heights()[KEY])
    foot = F.back_foot(approved, colour)
    foot_x = ax + (foot["cx"] - box[0]) * ratio
    sole_y = ay + (foot["bottom"] - box[1]) * ratio
    approved_parts, colours, boxes = {"back foot": foot["area"]}, {}, {}
    for name, points in list(F.PARTS[KEY].items()) + list(F.ANCHORS[KEY].items()):
        area, pts, _ = F.part(approved, points["approved"])
        if name in F.PARTS[KEY]:
            approved_parts[name] = area
        colours[name] = F.mean_colour(approved, pts)

    raw_path = F.RAWS / f"{KEY}_attack.raw.png"
    shutil.copyfile(raw_path, HERE / "candidates" / raw_path.name)
    raw = Image.open(raw_path).convert("RGBA")
    b = F.subject_box(raw)
    p_foot = F.back_foot(raw, colour)
    parts = {"back foot": p_foot["area"]}
    for name, point in POINTS.items():
        area, pts, _ = F.part(raw, point, colours[name])
        if name in F.PARTS[KEY]:
            parts[name] = area
        if pts:
            boxes[name] = F.pixels_box(pts)
    ratios = {name: math.sqrt(parts[name] / approved_parts[name]) for name in parts if parts[name]}
    measured = F.median(list(ratios.values()))
    with (HERE / "attack.csv").open(encoding="utf-8", newline="") as handle:
        written = {row["Key"]: float(row["Scale"]) for row in csv.DictReader(handle) if (row.get("Scale") or "").strip()}
    scale = written.get(KEY, measured)
    r = ratio / scale
    origin = (round(foot_x - (p_foot["cx"] - b[0]) * r), round(sole_y - (p_foot["bottom"] - b[1]) * r))
    main_cols = F.main_colours(approved)
    approved_means = F.means(approved, main_cols)
    matched = F.match_colours(raw, main_cols, approved_means)
    before, after = F.worst(F.means(raw, main_cols), approved_means), F.worst(F.means(matched, main_cols), approved_means)
    F.place(matched if after < before else raw, r, origin, b).save(HERE / "candidates" / f"{KEY}_attack_wide.png")
    right, bottom = origin[0] + round((b[2] - b[0]) * r), origin[1] + round((b[3] - b[1]) * r)
    line = (f"{KEY} attack: " + ", ".join(f"{name} {value:.3f}" for name, value in ratios.items())
            + f" -> median {measured:.3f}{' (Scale ' + str(scale) + ')' if scale != measured else ''}, "
            f"colours worst dE {before:.1f} -> {after:.1f} {'moved' if after < before else 'as drawn'}, "
            f"x {origin[0]}..{right}, y {origin[1]}..{bottom} (floor {F.FIGURE_CANVAS[1] - F.FIGURE_FLOOR_MARGIN})")
    if origin[0] < 0 or origin[1] < 0 or right > F.WIDE[0] or bottom > F.WIDE[1]:
        line += f"\n  경고: 자세 캔버스({F.WIDE[0]}x{F.WIDE[1]}) 밖으로 나간다"
    (HERE / "fit.txt").write_text(line + "\n", encoding="utf-8")
    head = F.mapped(F.head_box([boxes["face"]]), origin, b[:2], r)
    (HERE / "heads.json").write_text(json.dumps({KEY: {"attack": head}}, indent=1) + "\n", encoding="utf-8")
    print(line)


if __name__ == "__main__":
    main()
