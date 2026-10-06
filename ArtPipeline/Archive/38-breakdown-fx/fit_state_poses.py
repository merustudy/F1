"""Stands the valkyrie's state poses (round 38) at the approved figure's size with tools/fit_pose.py, no API call.
  .venv/bin/python ArtPipeline/Archive/38-breakdown-fx/fit_state_poses.py
The poses are named valkyrie_broken and valkyrie_resolute under output/hit/, so fit_pose is told to measure them against the
valkyrie's approved figure (its rule looks the figure up by the pose's key). Writes output/hit/valkyrie_broken.png and
valkyrie_resolute.png (2048x1024, the pose canvas), as the game's pose files are made.
"""
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / "ArtPipeline" / "tools"))
import fit_pose as F  # noqa: E402

original = F.approved_place
F.approved_place = lambda key: original("valkyrie")
for key in ("valkyrie_broken", "valkyrie_resolute"):
    F.fit_pose("hit", key, {})
