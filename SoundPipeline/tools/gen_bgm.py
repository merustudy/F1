#!/usr/bin/env python3
"""Generates background music candidates with ElevenLabs (Docs/Architecture/14_SOUND.md).

    .venv/bin/python SoundPipeline/tools/gen_bgm.py <key> --variants A,B,C --seconds 30 --max-calls 3 [--dry-run]

One candidate is one call: a single-chunk composition plan, which is the one way `negative_styles`
can shut a palette out. The positive styles are the track's BPM and Styles from Rosters/bgm.csv and the
style document's shared positive; the negative are the document's. --seconds overrides the roster's
length (30 for the trial candidates; the chosen one is made again at full length). The mp3 is written
as it comes to output/bgm/<key>_<variant>_<seconds>s.mp3 with its plan beside it. Looping (cutting the
faded tail at a bar) is a later, local step. Every call goes to the ledger, failed ones too.
"""
from __future__ import annotations

import argparse

import soundlib

MODEL = "music_v2_5"
OUTPUT_FORMAT = "mp3_44100_192"


def plan_of(row: dict, style: dict, seconds: int) -> dict:
    positive = [f"{row['Bpm'].strip()} BPM"] + [s.strip() for s in row["Styles"].split(";") if s.strip()] + style["music_positive"]
    return {"chunks": [{
        "text": row["Label"].strip(),
        "duration_ms": seconds * 1000,
        "positive_styles": positive,
        "negative_styles": style["music_negative"],
        "context_adherence": "high",
    }]}


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("key")
    parser.add_argument("--variants", default="A")
    parser.add_argument("--seconds", type=int)
    parser.add_argument("--max-calls", type=int, required=True)
    parser.add_argument("--dry-run", action="store_true")
    args = parser.parse_args()

    style = soundlib.read_style()
    roster = soundlib.read_roster("bgm", ["Key", "Name", "Label", "Bpm", "Styles", "Seconds"])
    row = roster.get(args.key) or soundlib.fail(f"not in Rosters/bgm.csv: {args.key}")
    seconds = args.seconds or int(row["Seconds"])
    if not 10 <= seconds <= 300:
        soundlib.fail(f"seconds must be 10..300, got {seconds}")
    variants = soundlib.parse_variants(args.variants)
    if len(variants) > args.max_calls:
        soundlib.fail(f"{len(variants)} calls planned, more than --max-calls {args.max_calls}")

    plan = plan_of(row, style, seconds)
    estimate = len(variants) * seconds / 60 * soundlib.MUSIC_USD_PER_MINUTE
    print(f"{args.key} ({row['Name']}), {seconds}s x {len(variants)}: {plan}")
    print(f"{len(variants)} calls, about ${estimate:.3f}")
    if args.dry_run:
        return 0

    from elevenlabs import ElevenLabs

    client = ElevenLabs(api_key=soundlib.read_key())
    out_dir = soundlib.OUTPUT / "bgm"
    out_dir.mkdir(parents=True, exist_ok=True)
    failures = 0
    for variant in variants:
        target = out_dir / f"{args.key}_{variant}_{seconds}s.mp3"
        try:
            audio = b"".join(client.music.compose(composition_plan=plan, model_id=MODEL, output_format=OUTPUT_FORMAT))
        except Exception as error:  # noqa: BLE001 - any failure is logged and counted, never shown in full
            failures += 1
            soundlib.log_call("bgm", args.key, variant, MODEL, seconds, soundlib.MUSIC_USD_PER_MINUTE, f"failed: {soundlib.describe_error(error)}")
            print(f"{target.name}: failed ({soundlib.describe_error(error)})")
            continue
        target.write_bytes(audio)
        soundlib.write_sidecar(target, {
            "key": args.key, "name": row["Name"], "variant": variant, "plan": plan,
            "seconds": seconds, "model": MODEL, "time": soundlib.now_iso(),
        })
        soundlib.log_call("bgm", args.key, variant, MODEL, seconds, soundlib.MUSIC_USD_PER_MINUTE, "ok")
        print(f"{target.name}: {len(audio) / 1024:.0f} KB")
    return 1 if failures else 0


if __name__ == "__main__":
    raise SystemExit(main())
