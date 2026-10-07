#!/usr/bin/env python3
"""Sums a play log by screen (stage 16 "플레이 기록": Docs/Architecture/10_TESTING_VALIDATION.md).

    Tools/playlog.py [file ...]      the newest file of the Logs folder when omitted

A line of the log is: time <tab> screen <tab> state. A screen's time runs until the next line; the last line has none.
Lines are grouped by screen and, when the state names a different phase (phase=), by that too.
"""
import glob
import os
import sys
from datetime import datetime

LOGS = os.path.expanduser("~/Library/Application Support/funitup/F1/Logs")


def parse(path):
    rows = []
    with open(path, encoding="utf-8") as file:
        for line in file:
            parts = line.rstrip("\n").split("\t")
            if len(parts) < 3:
                continue
            rows.append((datetime.strptime(parts[0], "%Y-%m-%d %H:%M:%S.%f"), parts[1], parts[2]))
    return rows


def phase(state):
    for token in state.split():
        if token.startswith("phase="):
            return token[len("phase="):]
    return ""


def key_of(screen, state):
    name = phase(state)
    return screen if not name or name == screen else f"{screen} ({name})"


def report(path):
    rows = parse(path)
    print(path)
    if len(rows) < 2:
        print("  fewer than two lines")
        return

    totals, counts = {}, {}
    for (at, screen, state), (next_at, _, _) in zip(rows, rows[1:]):
        key = key_of(screen, state)
        totals[key] = totals.get(key, 0.0) + (next_at - at).total_seconds()
        counts[key] = counts.get(key, 0) + 1

    total = (rows[-1][0] - rows[0][0]).total_seconds()
    print(f"  {'screen':<22}{'minutes':>9}{'times':>7}{'share':>8}")
    for key, seconds in sorted(totals.items(), key=lambda item: -item[1]):
        print(f"  {key:<22}{seconds / 60:>9.1f}{counts[key]:>7}{100 * seconds / total:>7.0f}%")
    print(f"  {'total':<22}{total / 60:>9.1f}")
    print(f"  from {rows[0][1]} [{rows[0][2]}] at {rows[0][0]:%H:%M:%S}")
    print(f"  to   {rows[-1][1]} [{rows[-1][2]}] at {rows[-1][0]:%H:%M:%S}")
    print("  timeline:")
    for at, screen, state in rows:
        print(f"    {at:%H:%M:%S} {screen:<11} {state}")


def main(argv):
    files = argv[1:] or sorted(glob.glob(os.path.join(LOGS, "play-*.log")), key=os.path.getmtime)[-1:]
    if not files:
        print(f"No play log. Looked in: {LOGS}")
        return 2
    for path in files:
        report(path)
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
