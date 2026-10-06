"""Stands the state poses of round 40 at the approved figures' size with tools/fit_pose.py, no API call.
  .venv/bin/python ArtPipeline/Archive/40-state-poses/fit_state_poses.py [--jobs knight,bishop] [--scale spellblade_broken=1.0 ...] [--measure]
A pose is output/hit/<job>_broken.raw.png or <job>_resolute.raw.png; fit_pose is told to measure it against the job's approved
figure (round 38's fit_state_poses.py did this for the valkyrie). The scale is the gold discs' where the figure has them
(fit_pose's rule); a job without a usable disc measure (the archmage, the spellblade: round 23) takes the --scale given after
looking, and the straight blades of the knight and the spellblade are measured too as a check (the steel's long axis: the blade
hangs or stands upright in both states, so its whole length shows). --measure only prints; without it the fitted poses are
written to output/hit/<job>_<state>.png (2048x1024), the files that go to Assets/@Art/Pose/Job/ once approved.
"""
import argparse
import colorsys
import math
import sys
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / "ArtPipeline" / "tools"))
import fit_pose as F  # noqa: E402

JOBS = ("knight", "bishop", "paladin", "archmage", "spellblade")
STATES = ("broken", "resolute")
BLADED = ("knight", "spellblade")


def steel(r, g, b):
    h, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
    return s <= 0.14 and 0.45 <= v <= 0.92


def blade_length(image):
    """The long axis of the longest thin light-grey area: a straight blade (the armour's plates are compact)."""
    best = 0.0
    for pts in F.blobs(image, steel, 400, 10**7):
        n = len(pts)
        mx, my = sum(p[0] for p in pts) / n, sum(p[1] for p in pts) / n
        cxx = sum((p[0] - mx) ** 2 for p in pts) / n
        cyy = sum((p[1] - my) ** 2 for p in pts) / n
        cxy = sum((p[0] - mx) * (p[1] - my) for p in pts) / n
        root = math.sqrt(max(0.0, (cxx + cyy) ** 2 / 4 - (cxx * cyy - cxy * cxy)))
        major = 4 * math.sqrt((cxx + cyy) / 2 + root)
        minor = 4 * math.sqrt(max((cxx + cyy) / 2 - root, 1e-6))
        if major / minor >= 3.5 and major > best:
            best = major
    return best


def measure(job, state):
    approved = Image.open(F.OUTPUT / "character" / f"{job}.raw.png").convert("RGBA")
    raw = Image.open(F.OUTPUT / "hit" / f"{job}_{state}.raw.png").convert("RGBA")
    da, dp = F.discs(approved), F.discs(raw)
    k = min(F.DISCS, len(da), len(dp))
    disc = F.median(dp[:k]) / F.median(da[:k]) if k >= 2 else None
    blade = blade_length(raw) / blade_length(approved) if job in BLADED and blade_length(approved) else None
    return disc, blade, [round(d) for d in dp[:F.DISCS]], [round(d) for d in da[:F.DISCS]]


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--jobs", default=",".join(JOBS))
    parser.add_argument("--scale", action="append", default=[], help="<job>_<state>=<scale>, replaces the disc measure")
    parser.add_argument("--measure", action="store_true", help="print the measures only, write nothing")
    args = parser.parse_args()
    scales = {}
    for item in args.scale:
        key, value = item.split("=")
        scales[key] = float(value)
    original = F.approved_place
    for job in args.jobs.split(","):
        F.approved_place = lambda key, job=job: original(job)
        for state in STATES:
            key = f"{job}_{state}"
            if not (F.OUTPUT / "hit" / f"{key}.raw.png").is_file():
                print(f"{key}: 원본 없음")
                continue
            disc, blade, dp, da = measure(job, state)
            print(f"{key}: discs {dp} against {da} -> {'%.3f' % disc if disc else 'none'}"
                  f"{', blade %.3f' % blade if blade else ''}{', Scale %.3f' % scales[key] if key in scales else ''}")
            if not args.measure:
                F.fit_pose("hit", key, scales)
    F.approved_place = original


if __name__ == "__main__":
    main()
