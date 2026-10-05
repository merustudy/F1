#!/usr/bin/env python3
"""Round 03: the user left the verdict to the recommendation ("남은 것도 생성후 알아서 권장안 연결", 2026-10-05).
Nobody here can listen, so the effects are chosen by what can be measured (SoundPipeline/tools/measure.py):

- A candidate is out when it came back much shorter than asked (under half: mostly silence) or nearly silent.
- A short effect (asked 1 second or less) takes the crispest and driest: the greatest peak over RMS.
- A longer effect takes the darkest and heaviest: the lowest brightness.
- A tie goes to the length closest to the one asked.

The rule agrees with all four of the user's own picks in round 02 (all A): hit and button were the crispest,
the mercenary's death and the dungeon music the darkest.

    .venv/bin/python SoundPipeline/Archive/03-rest/pick.py [--json picks.json]
"""
from __future__ import annotations

import argparse
import csv
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]  # SoundPipeline
sys.path.insert(0, str(ROOT / "tools"))
import measure  # noqa: E402

SHORT = 1.0


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--json", type=Path)
    args = parser.parse_args()
    with (ROOT / "Rosters" / "sfx.csv").open(encoding="utf-8", newline="") as f:
        roster = list(csv.DictReader(f))
    picks = []
    for row in roster:
        key, asked = row["Key"], float(row["Seconds"])
        files = sorted((ROOT / "output" / "sfx").glob(f"{key}_[A-Z].wav"))
        if not files:
            continue
        measured = [measure.measure(path) for path in files]
        for m in measured:
            m["variant"] = m["file"][len(key) + 1:-4]
            m["crest_db"] = round(m["peak_db"] - m["rms_db"], 1)
            m["valid"] = m["seconds"] >= asked / 2 and m["rms_db"] > -45
        valid = [m for m in measured if m["valid"]] or measured
        if asked <= SHORT:
            chosen = max(valid, key=lambda m: (m["crest_db"], -abs(m["seconds"] - asked)))
            rule = "crispest"
        else:
            chosen = min(valid, key=lambda m: (m["brightness_hz"], abs(m["seconds"] - asked)))
            rule = "darkest"
        picks.append({"key": key, "name": row["Name"], "asked": asked, "rule": rule, "pick": chosen["variant"], "candidates": measured})
        line = "  ".join(
            f"{m['variant']}{'*' if m is chosen else ' '}{'' if m['valid'] else '(out)'} {m['seconds']:.2f}s crest {m['crest_db']:4.1f} bright {m['brightness_hz']:5d}"
            for m in measured)
        print(f"{key:16} {rule:9} -> {chosen['variant']}   {line}")
    if args.json:
        args.json.write_text(json.dumps(picks, ensure_ascii=False, indent=1) + "\n", encoding="utf-8")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
