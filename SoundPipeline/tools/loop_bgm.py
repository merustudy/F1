#!/usr/bin/env python3
"""Turns an approved music candidate into a seamless loop for the game (Docs/Architecture/14_SOUND.md). Makes no call.

    .venv/bin/python SoundPipeline/tools/loop_bgm.py <in.mp3> <out.wav> --bpm 92 [--seam <seam.wav>]

The music API fades its end out whatever the prompt says, so the track cannot simply repeat. This measures
the tempo, then finds the place a whole number of phrases (4 bars) after the music starts where the audio
is most like the start (of joints about as good, the longest, so that the loop repeats least), cuts there, and blends the audio that followed the cut into the first moments of the loop so the jump back is
smooth. Then the loop is set to a common loudness and written as 16-bit stereo 44.1 kHz wav: Unity makes
the compressed copy, so the music is compressed once. --seam also writes the last 4 seconds followed by
the first 4, to hear the joint. Decoding uses macOS afconvert; nothing outside the standard library.
"""
from __future__ import annotations

import argparse
import array
import math
import subprocess
import tempfile
import wave
from pathlib import Path

RATE = 44100
HOP = 441                      # 10 ms
TARGET_RMS_DB = -20.0          # the loudness every loop is set to
PEAK_LIMIT_DB = -1.0
START_DB = -45.0               # the music starts at the first sound above this
TAIL_GUARD = 1.5               # seconds kept away from the faded end
MIN_LOOP = 16.0                # seconds
MATCH = 4.0                    # seconds compared after the start and after a candidate cut (about a phrase and a half)
PHRASE = 4                     # bars: a cut mid-phrase jumps back in the middle of a musical sentence
CLOSE = 0.02                   # joints this close to the best are as good: the longest of them repeats least
XFADE = 0.06                   # seconds of blend at the joint


def fail(message: str) -> None:
    raise SystemExit(f"[loop] {message}")


def decode(path: Path) -> array.array:
    with tempfile.TemporaryDirectory() as tmp:
        target = Path(tmp) / "decoded.wav"
        done = subprocess.run(["afconvert", "-f", "WAVE", "-d", "LEI16@44100", str(path), str(target)], capture_output=True, text=True)
        if done.returncode != 0:
            fail(f"afconvert could not decode {path.name}: {done.stderr.strip()}")
        with wave.open(str(target)) as w:
            if w.getnchannels() != 2 or w.getframerate() != RATE:
                fail(f"expected stereo {RATE} Hz, got {w.getnchannels()} channels at {w.getframerate()} Hz")
            samples = array.array("h")
            samples.frombytes(w.readframes(w.getnframes()))
    return samples


def db(value: float) -> float:
    return 20 * math.log10(max(value, 1e-9) / 32768)


def envelope(mono: list) -> list:
    return [math.sqrt(sum(v * v for v in mono[i:i + HOP]) / HOP) for i in range(0, len(mono) - HOP + 1, HOP)]


def correlation(a: list, b: list) -> float:
    n = min(len(a), len(b))
    ma, mb = sum(a[:n]) / n, sum(b[:n]) / n
    num = sum((a[i] - ma) * (b[i] - mb) for i in range(n))
    den = math.sqrt(sum((a[i] - ma) ** 2 for i in range(n)) * sum((b[i] - mb) ** 2 for i in range(n)))
    return num / den if den else 0.0


def measured_bar(env: list, start_hop: int, asked_bar: float) -> float:
    """The bar length the onsets repeat at, searched within 10% of the asked one, in seconds.

    A bar spans many hops, so its length comes out far finer than a single beat's would (a beat's
    lag lands on a 10 ms hop and is easily mistaken for its half).
    """
    onset = [max(0.0, env[i] - env[i - 1]) for i in range(start_hop + 1, len(env))]
    centre = asked_bar * RATE / HOP
    lags = range(int(centre * 0.9), int(centre * 1.1) + 1)
    best_lag = max(lags, key=lambda lag: correlation(onset[:-lag], onset[lag:]))
    return best_lag * HOP / RATE


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("source", type=Path)
    parser.add_argument("target", type=Path)
    parser.add_argument("--bpm", type=float, required=True)
    parser.add_argument("--seam", type=Path)
    args = parser.parse_args()

    stereo = decode(args.source)
    frames = len(stereo) // 2
    mono = [(stereo[2 * i] + stereo[2 * i + 1]) / 2 for i in range(frames)]
    env = envelope(mono)

    threshold = 32768 * 10 ** (START_DB / 20)
    first = next((i for i, v in enumerate(mono) if abs(v) > threshold), None)
    if first is None:
        fail("the track is silent")
    start = max(0, first - int(0.005 * RATE))
    start_hop = start // HOP

    bar = measured_bar(env, start_hop, 4 * 60.0 / args.bpm)
    measured_bpm = 4 * 60.0 / bar

    # Candidate cuts: whole phrases after the start, long enough, and early enough that the compared
    # stretch after the cut ends before the faded tail.
    match_hops = int(MATCH * RATE / HOP)
    last_cut = frames / RATE - TAIL_GUARD - MATCH - start / RATE
    head = env[start_hop:start_hop + match_hops]
    joints = []
    bars = PHRASE
    while bars * bar <= last_cut:
        if bars * bar >= MIN_LOOP:
            centre = start_hop + round(bars * bar * RATE / HOP)
            joints.append(max((correlation(head, env[hop:hop + match_hops]), hop, bars) for hop in range(centre - 3, centre + 4)))
        bars += PHRASE
    if not joints:
        fail(f"the track is too short for a loop of whole phrases ({PHRASE} bars) of at least {MIN_LOOP:g}s")
    best_score = max(j[0] for j in joints)
    score, cut_hop, cut_bars = max((j for j in joints if j[0] >= best_score - CLOSE), key=lambda j: j[2])

    # Refine the cut to the sample on a 4x thinned signal, then on the full one.
    def wave_score(offset: int, step: int, length: int) -> float:
        a = mono[start:start + length:step]
        b = mono[offset:offset + length:step]
        return correlation(a, b)

    coarse = max(range(cut_hop * HOP - HOP, cut_hop * HOP + HOP + 1, 4), key=lambda o: wave_score(o, 4, int(0.25 * RATE)))
    cut = max(range(coarse - 4, coarse + 5), key=lambda o: wave_score(o, 1, int(0.05 * RATE)))
    length = cut - start

    # The loop, with the audio that followed the cut blended into its first moments (equal power).
    blend = int(XFADE * RATE)
    loop = array.array("h", stereo[2 * start:2 * cut])
    out = [float(v) for v in loop]
    for i in range(blend):
        t = i / blend
        fade_in, fade_out = math.sin(t * math.pi / 2), math.cos(t * math.pi / 2)
        for ch in (0, 1):
            out[2 * i + ch] = out[2 * i + ch] * fade_in + stereo[2 * (cut + i) + ch] * fade_out

    rms = math.sqrt(sum(v * v for v in out) / len(out))
    peak = max(abs(v) for v in out)
    gain = 10 ** ((TARGET_RMS_DB - db(rms)) / 20)
    gain = min(gain, 32768 * 10 ** (PEAK_LIMIT_DB / 20) / peak)
    result = array.array("h", (max(-32768, min(32767, int(round(v * gain)))) for v in out))

    args.target.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(args.target), "wb") as w:
        w.setnchannels(2)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(result.tobytes())

    if args.seam:
        tail = result[-2 * 4 * RATE:] if len(result) > 2 * 4 * RATE else result
        joint = array.array("h", tail) + array.array("h", result[:2 * 4 * RATE])
        with wave.open(str(args.seam), "wb") as w:
            w.setnchannels(2)
            w.setsampwidth(2)
            w.setframerate(RATE)
            w.writeframes(joint.tobytes())

    final_rms = db(math.sqrt(sum(v * v for v in result) / len(result)))
    final_peak = db(max(abs(v) for v in result))
    print(f"{args.source.name} -> {args.target.name}")
    print(f"  tempo asked {args.bpm:g} BPM, measured {measured_bpm:.1f} BPM (bar {bar:.3f}s)")
    print(f"  joints (bars: likeness): {', '.join(f'{b}: {c:.2f}' for c, _, b in joints)}")
    print(f"  start {start / RATE:.3f}s, loop {length / RATE:.3f}s = {cut_bars} bars of {bar:.3f}s, likeness of the joint {score:.2f}")
    print(f"  gain {db(gain * 32768):+.1f} dB, RMS {final_rms:.1f} dBFS, peak {final_peak:.1f} dBFS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
