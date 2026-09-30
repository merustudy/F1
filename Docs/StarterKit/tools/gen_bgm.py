#!/usr/bin/env python3
"""Generates C1's background music with ElevenLabs: three instrumental loops, one for the lobby,
one for the run's floors and one for the boss floor.

2026-09-30: the first round's toy-box palette (marimba, ukulele, glockenspiel, major keys) read as
too cheerful against the game's dark background, so the three tracks were redrawn dark. Each is a
single-chunk composition plan, which is what lets `negative_styles` shut the old palette out; the
lobby is a dark lo-fi beat (candidate C1), the floors a creeping dorian groove (A2) and the boss an
oppressive tom-driven loop (A1). The 30-second candidates the choice was made from are described in
`ArtPipeline/Archive/sfx-replay-layer/README.md`.

Usage:
    .venv/bin/python ArtPipeline/tools/gen_bgm.py <out-dir> [name ...]

Written as mp3 straight from the API (stereo, 44.1 kHz, 128 kbps); the key comes from the
login keychain the way gen_sfx.py reads it, never from the environment and never printed.
"""
import getpass, subprocess, sys, time
from pathlib import Path

from elevenlabs import ElevenLabs

KEYCHAIN_SERVICE = "ELEVENLABS_API_KEY"
SHARED_POSITIVE = ["dark cartoon fantasy adventure game soundtrack", "instrumental only", "loopable steady groove",
                   "no build-up", "no ending drop", "dry room production"]
SHARED_NEGATIVE = ["ukulele", "glockenspiel", "cheerful", "major key", "bright and bouncy", "children's toy music",
                   "vocals", "singing", "epic orchestral swell", "fade out", "electronic dance", "8-bit chiptune"]

# name, section label, positive styles, seconds
ITEMS = [
    ("main-bgm", "[Lobby]",
     ["76 BPM", "dark lo-fi hip hop beat", "dusty slow drums", "minor jazz chords on muted electric piano",
      "deep sub bass", "vinyl crackle", "melancholic and heavy", "no lead hook"], 60),
    ("challenge-bgm", "[Dungeon floors]",
     ["100 BPM", "creeping dorian groove", "bass marimba", "detuned music box", "tom and shaker groove",
      "mischievous but ominous", "steady mid energy", "low woodwind counter line"], 90),
    ("house-bgm", "[Boss]",
     ["116 BPM", "oppressive boss fight loop", "pounding low toms and bass drum", "chromatic low string ostinato",
      "staccato cello stabs", "sinister music box counter melody", "minor key", "relentless steady drive"], 75),
]


def read_key() -> str:
    done = subprocess.run(
        ["security", "find-generic-password", "-s", KEYCHAIN_SERVICE, "-a", getpass.getuser(), "-w"],
        capture_output=True, text=True, check=False)
    if done.returncode != 0:
        sys.exit(f"Keychain에서 키를 읽지 못했다: {done.stderr.strip()}")
    return done.stdout.strip()


def plan_for(label: str, positive, seconds: int) -> dict:
    return {"chunks": [{"text": label, "duration_ms": seconds * 1000,
                        "positive_styles": positive + SHARED_POSITIVE,
                        "negative_styles": SHARED_NEGATIVE, "context_adherence": "high"}]}


def generate(client: ElevenLabs, out: Path, item) -> str:
    name, label, positive, seconds = item
    for attempt in range(3):
        try:
            audio = client.music.compose(
                composition_plan=plan_for(label, positive, seconds), model_id="music_v2_5",
                output_format="mp3_44100_128")
            path = out / f"{name}.mp3"
            with open(path, "wb") as f:
                for chunk in audio:
                    f.write(chunk)
            return f"{name}: {path.stat().st_size // 1024} KB"
        except Exception as error:  # the message never carries the key
            if attempt == 2:
                return f"{name}: FAILED {type(error).__name__}: {str(error)[:200]}"
            time.sleep(5)
    return f"{name}: FAILED"


def main() -> None:
    if len(sys.argv) < 2:
        sys.exit(__doc__)
    out = Path(sys.argv[1])
    out.mkdir(parents=True, exist_ok=True)
    only = set(sys.argv[2:])
    client = ElevenLabs(api_key=read_key())
    for item in ITEMS:
        if only and item[0] not in only:
            continue
        print(generate(client, out, item), flush=True)


if __name__ == "__main__":
    main()
