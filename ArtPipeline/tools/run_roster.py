#!/usr/bin/env python3
"""Runs gen_image.py once per roster row, one after another.

gen_image.py makes exactly one API call per run; this only decides which rows to run and
when to stop. A row whose output already exists is skipped, so running it again after a
failure spends calls only on what is missing. --max-calls is required: a round states
its cap before it starts, and the runner stops there.
"""

import argparse
import csv
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ROSTERS = ROOT / "Rosters"
OUTPUT_DIR = ROOT / "output"
GEN_IMAGE = Path(__file__).resolve().parent / "gen_image.py"


def roster_keys(kind: str) -> list:
    path = ROSTERS / f"{kind}.csv"
    if not path.is_file():
        raise SystemExit(f"실패: Roster 가 없다: {path}")
    with path.open(encoding="utf-8", newline="") as handle:
        return [row["Key"] for row in csv.DictReader(handle)]


def parse_args():
    parser = argparse.ArgumentParser(description="Generate the missing rows of a roster, one call per row.")
    parser.add_argument("--type", dest="kind", required=True, help="Roster and generation type, e.g. character.")
    parser.add_argument("--max-calls", type=int, required=True, help="Stop after this many API calls.")
    parser.add_argument("--only", default="", help="Comma separated keys. Default: every row of the roster.")
    parser.add_argument("--quality", default="", choices=["", "low", "medium", "high"], help="Passed on to gen_image.py.")
    parser.add_argument("--dry-run", action="store_true", help="Passed on to gen_image.py: no key, no call.")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    keys = roster_keys(args.kind)

    if args.only:
        wanted = [key.strip() for key in args.only.split(",") if key.strip()]
        unknown = [key for key in wanted if key not in keys]
        if unknown:
            raise SystemExit(f"실패: Roster 에 없는 Key: {', '.join(unknown)}")
        keys = wanted

    calls, made, skipped, failed, waiting = 0, [], [], [], []
    for key in keys:
        output = OUTPUT_DIR / args.kind / f"{key}.png"
        if output.exists() and not args.dry_run:
            skipped.append(key)
            continue

        if calls >= args.max_calls:
            waiting.append(key)
            continue

        command = [sys.executable, str(GEN_IMAGE), "--type", args.kind, "--key", key]
        if args.quality:
            command += ["--quality", args.quality]
        if args.dry_run:
            command.append("--dry-run")

        print(f"\n=== {key} ===", flush=True)
        try:
            subprocess.run(command, check=False)
        except KeyboardInterrupt:
            print("\n중단했다.", file=sys.stderr)
            return 130

        if args.dry_run:
            continue

        # A run counts as a call whether it worked or not: a failure may still have been billed.
        calls += 1

        # Success is the file, not the exit code: the API sometimes returns nothing without an error.
        (made if output.exists() else failed).append(key)

    if args.dry_run:
        return 0

    print(f"\n호출 {calls}회 (상한 {args.max_calls}). 만든 것 {len(made)}, 건너뛴 것 {len(skipped)}, "
          f"실패 {len(failed)}, 상한에 걸려 남은 것 {len(waiting)}.")
    for label, group in (("만든 것", made), ("건너뛴 것(이미 있음)", skipped), ("실패", failed), ("남은 것", waiting)):
        if group:
            print(f"  {label}: {', '.join(group)}")

    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
