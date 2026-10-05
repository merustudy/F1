#!/usr/bin/env python3
"""Generates sound effect candidates with ElevenLabs (Docs/Architecture/14_SOUND.md).

    .venv/bin/python SoundPipeline/tools/gen_sfx.py <key> [<key> ...] --variants A,B,C --max-calls 9 [--dry-run]

One candidate is one call. The prompt is the roster's Subject, the style document's tail (the effect
tail, or the sting tail for victory and defeat) and a length hint. The API's PCM comes stereo
interleaved whatever its documentation says, so it is folded to mono; then the silence at both ends is
trimmed, the peak set to -6 dBFS, and the result written as 16-bit mono 44.1 kHz wav to
output/sfx/<key>_<variant>.wav with its prompt beside it. Every call goes to the ledger, failed ones too.
Everything that can fail for free fails before the key is read; --dry-run stops there.
"""
from __future__ import annotations

import argparse
import array
import math
import wave

import soundlib

RATE = 44100
MODEL = "eleven_text_to_sound_v2"
PROMPT_INFLUENCE = 0.6
SILENCE = 32768 * 10 ** (-60 / 20)  # -60 dBFS
LEAD_PAD = int(0.005 * RATE)
TAIL_PAD = int(0.030 * RATE)
PEAK = 32767 * 10 ** (-6 / 20)  # -6 dBFS


def length_hint(seconds: float) -> str:
    if seconds <= 0.8:
        return "very short"
    if seconds <= 1.5:
        return "short"
    return ""


def prompt_of(row: dict, style: dict) -> str:
    tail = style["sting"] if row["Tail"].strip() == "sting" else style["effect"]
    parts = [row["Subject"].strip(), tail, length_hint(float(row["Seconds"]))]
    return ", ".join(p for p in parts if p)


def to_mono(pcm: bytes, seconds: float) -> array.array:
    samples = array.array("h")
    samples.frombytes(pcm[: len(pcm) // 2 * 2])
    # Stereo comes as twice the samples of the asked length; average each pair.
    if len(samples) > 1.5 * seconds * RATE:
        return array.array("h", ((samples[i] + samples[i + 1]) // 2 for i in range(0, len(samples) - 1, 2)))
    return samples


def trim(samples: array.array) -> array.array:
    loud = [i for i, s in enumerate(samples) if abs(s) > SILENCE]
    if not loud:
        return samples
    return samples[max(0, loud[0] - LEAD_PAD): min(len(samples), loud[-1] + TAIL_PAD)]


def normalize(samples: array.array) -> array.array:
    peak = max((abs(s) for s in samples), default=0)
    if peak == 0:
        return samples
    gain = PEAK / peak
    return array.array("h", (max(-32768, min(32767, int(round(s * gain)))) for s in samples))


def write_wav(path, samples: array.array) -> None:
    with wave.open(str(path), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(samples.tobytes())


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("keys", nargs="+")
    parser.add_argument("--variants", default="A")
    parser.add_argument("--max-calls", type=int, required=True)
    parser.add_argument("--dry-run", action="store_true")
    args = parser.parse_args()

    style = soundlib.read_style()
    roster = soundlib.read_roster("sfx", ["Key", "Name", "Subject", "Seconds", "Tail"])
    variants = soundlib.parse_variants(args.variants)
    plan = []
    for key in args.keys:
        row = roster.get(key) or soundlib.fail(f"not in Rosters/sfx.csv: {key}")
        seconds = float(row["Seconds"])
        if not 0.5 <= seconds <= 30:
            soundlib.fail(f"{key}: Seconds must be 0.5..30 (the API's range), got {seconds}")
        if row["Tail"].strip() not in ("effect", "sting"):
            soundlib.fail(f"{key}: Tail must be effect or sting")
        for variant in variants:
            plan.append((key, variant, row, seconds, prompt_of(row, style)))
    if len(plan) > args.max_calls:
        soundlib.fail(f"{len(plan)} calls planned, more than --max-calls {args.max_calls}")

    estimate = sum(s for _, _, _, s, _ in plan) / 60 * soundlib.SFX_USD_PER_MINUTE
    for key, variant, row, seconds, prompt in plan:
        print(f"{key}_{variant} ({row['Name']}, {seconds:g}s): {prompt}")
    print(f"{len(plan)} calls, about ${estimate:.3f}")
    if args.dry_run:
        return 0

    from elevenlabs import ElevenLabs

    client = ElevenLabs(api_key=soundlib.read_key())
    out_dir = soundlib.OUTPUT / "sfx"
    out_dir.mkdir(parents=True, exist_ok=True)
    failures = 0
    for key, variant, row, seconds, prompt in plan:
        target = out_dir / f"{key}_{variant}.wav"
        try:
            pcm = b"".join(client.text_to_sound_effects.convert(
                text=prompt, duration_seconds=seconds, prompt_influence=PROMPT_INFLUENCE,
                output_format="pcm_44100", model_id=MODEL))
        except Exception as error:  # noqa: BLE001 - any failure is logged and counted, never shown in full
            failures += 1
            soundlib.log_call("sfx", key, variant, MODEL, seconds, soundlib.SFX_USD_PER_MINUTE, f"failed: {soundlib.describe_error(error)}")
            print(f"{target.name}: failed ({soundlib.describe_error(error)})")
            continue
        samples = normalize(trim(to_mono(pcm, seconds)))
        write_wav(target, samples)
        soundlib.write_sidecar(target, {
            "key": key, "name": row["Name"], "variant": variant, "prompt": prompt,
            "seconds": seconds, "model": MODEL, "time": soundlib.now_iso(),
        })
        soundlib.log_call("sfx", key, variant, MODEL, seconds, soundlib.SFX_USD_PER_MINUTE, "ok")
        print(f"{target.name}: {len(samples) / RATE:.2f}s")
    return 1 if failures else 0


if __name__ == "__main__":
    raise SystemExit(main())
