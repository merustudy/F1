#!/usr/bin/env python3
"""Measures sound candidates, for choosing among them without listening (Docs/Architecture/14_SOUND.md). Makes no call.

    .venv/bin/python SoundPipeline/tools/measure.py <file.wav|file.mp3> [...] [--json out.json]

Per file: the length; the RMS and the peak (dBFS); the brightness, as the frequency of a sine whose slope carries
the same share of its energy (a lower one is darker and heavier); the attack, from the start to the loudest
moment; and the decay, from the loudest moment until the sound has fallen 30 dB (a shorter one is drier).
Music is decoded with macOS afconvert. Nothing outside the standard library.
"""
from __future__ import annotations

import argparse
import array
import json
import math
import subprocess
import tempfile
import wave
from pathlib import Path

HOP_SECONDS = 0.01
DECAY_DB = 30.0


def read_mono(path: Path) -> tuple:
    """The samples folded to mono, and the sample rate."""
    source = path
    with tempfile.TemporaryDirectory() as tmp:
        if path.suffix.lower() != ".wav":
            source = Path(tmp) / "decoded.wav"
            subprocess.run(["afconvert", "-f", "WAVE", "-d", "LEI16@44100", str(path), str(source)], check=True, capture_output=True)
        with wave.open(str(source)) as w:
            channels, rate = w.getnchannels(), w.getframerate()
            samples = array.array("h")
            samples.frombytes(w.readframes(w.getnframes()))
    if channels == 1:
        return list(samples), rate
    return [sum(samples[i:i + channels]) / channels for i in range(0, len(samples) - channels + 1, channels)], rate


def db(value: float) -> float:
    return 20 * math.log10(max(value, 1e-9) / 32768)


def measure(path: Path) -> dict:
    mono, rate = read_mono(path)
    energy = sum(v * v for v in mono)
    slope = sum((mono[i] - mono[i - 1]) ** 2 for i in range(1, len(mono)))
    ratio = min(slope / energy, 3.999) if energy else 0.0
    brightness = rate / (2 * math.pi) * math.acos(1 - ratio / 2)

    hop = max(1, int(HOP_SECONDS * rate))
    envelope = [math.sqrt(sum(v * v for v in mono[i:i + hop]) / hop) for i in range(0, len(mono) - hop + 1, hop)]
    loudest = max(range(len(envelope)), key=envelope.__getitem__) if envelope else 0
    floor = envelope[loudest] * 10 ** (-DECAY_DB / 20) if envelope else 0.0
    fallen = next((i for i in range(loudest, len(envelope)) if envelope[i] < floor), len(envelope))
    return {
        "file": path.name,
        "seconds": round(len(mono) / rate, 3),
        "rms_db": round(db(math.sqrt(energy / len(mono))) if mono else -180.0, 1),
        "peak_db": round(db(max(abs(v) for v in mono)) if mono else -180.0, 1),
        "brightness_hz": round(brightness),
        "attack_s": round(loudest * HOP_SECONDS, 3),
        "decay_s": round((fallen - loudest) * HOP_SECONDS, 3),
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("files", nargs="+", type=Path)
    parser.add_argument("--json", type=Path)
    args = parser.parse_args()
    rows = [measure(path) for path in args.files]
    print(f"{'file':34} {'sec':>6} {'rms':>6} {'peak':>6} {'bright':>7} {'attack':>7} {'decay':>6}")
    for r in rows:
        print(f"{r['file']:34} {r['seconds']:6.2f} {r['rms_db']:6.1f} {r['peak_db']:6.1f} {r['brightness_hz']:7d} {r['attack_s']:7.2f} {r['decay_s']:6.2f}")
    if args.json:
        args.json.write_text(json.dumps(rows, ensure_ascii=False, indent=1) + "\n", encoding="utf-8")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
