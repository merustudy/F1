#!/usr/bin/env python3
"""Generates sound effects with ElevenLabs for C1 (2026-09-29, the replay's sound layer).

Usage:
    .venv/bin/python ArtPipeline/tools/gen_sfx.py <out-dir> [name ...]

Prompts live in ITEMS; give names to generate a subset. Every clip is fetched as 44.1 kHz PCM
(the API's PCM is stereo interleaved whatever the docs say, so it is folded to mono), trimmed
of trailing silence, peak-normalised to -6 dBFS and written as wav. The key is read from the
login keychain the way test_imagegen.py reads its own: never from the environment, never
printed, never logged.
"""
import array, getpass, subprocess, sys, time, wave
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path

from elevenlabs import ElevenLabs

RATE = 44100
KEYCHAIN_SERVICE = "ELEVENLABS_API_KEY"
STYLE = ", cute clean cartoon fantasy game, toy-like, dry, no reverb, no music"

# name, prompt, seconds (the API's floor is 0.5).
ITEMS = [
    ("hit-attack", "Short warm wooden xylophone 'tok' hit with a tiny bright coin ding on top" + STYLE + ", very short", 0.6),
    ("hit-amplify", "Short bright glass chime rising quickly upward, magical sparkle" + STYLE + ", very short", 0.7),
    ("hit-factor", "Low soft 'whomp' bass thud followed by a shimmering magical glitter tail, power-up" + STYLE + ", short", 0.9),
    ("layer-sparkle", "Tiny high glitter twinkle, a single quick sparkle" + STYLE + ", very short", 0.5),
    ("layer-thump", "Deep soft cartoon bass thump, punchy, no tail" + STYLE + ", very short", 0.5),
    ("coin-scatter", "A handful of small gold coins scattering and bouncing on a wooden table" + STYLE + ", short", 1.0),
    ("win-stinger", "Short cheerful win jingle, xylophone and small bells, three rising notes, bright" + STYLE, 1.2),
    ("jackpot-fanfare", "Triumphant playful jackpot fanfare with bells, xylophone and a shower of coins, bright" + STYLE, 2.0),
    ("zero-thud", "Dull muffled wooden thud, deflated 'nothing happened', flat" + STYLE + ", very short", 0.5),
    # Second batch (2026-09-29): the player's actions and the run's events, one each.
    ("hold-on", "Small wooden latch clicking shut, a crisp 'clack' with a tiny metal pin" + STYLE + ", very short", 0.5),
    ("hold-off", "Small wooden latch clicking open, a softer 'click' with a short release" + STYLE + ", very short", 0.5),
    ("reroll", "Quick short whoosh of wooden reels spinning past, a soft ratchet flutter" + STYLE + ", very short", 0.6),
    ("shop-buy", "Coins dropped into a leather purse with a bright little chime, purchase made" + STYLE + ", short", 0.8),
    ("shop-sell", "A few coins tossed onto a wooden counter, sliding, a soft clink" + STYLE + ", short", 0.7),
    ("refuse", "Low dull 'bonk' with a short flat descending buzz, request denied, comedic" + STYLE + ", very short", 0.5),
    ("monster-hit", "Cartoon 'whack' impact on a big soft creature, a padded punchy thud with a squash" + STYLE + ", very short", 0.6),
    ("boss-intro", "Deep single ominous taiko drum hit with a low rumbling tail, playful menace" + STYLE + ", short", 1.5),
    ("forge-clang", "Blacksmith hammer striking an anvil once, bright metallic clang with a short magical shimmer" + STYLE + ", short", 0.9),
    ("floor-choose", "Soft parchment map page turning with a light footstep, journey onward" + STYLE + ", short", 0.7),
    # Payline roll call (2026-09-30): candidate B of four, one per line as it blinks; the screen pitches it up per line.
    ("payline-line", "Quick short slide of a wooden marker across a board with a tiny bell tick at the end, a line being drawn" + STYLE + ", very short", 0.5),
]


def read_key() -> str:
    done = subprocess.run(
        ["security", "find-generic-password", "-s", KEYCHAIN_SERVICE, "-a", getpass.getuser(), "-w"],
        capture_output=True, text=True, check=False)
    if done.returncode != 0:
        sys.exit(f"Keychain에서 키를 읽지 못했다: {done.stderr.strip()}")
    return done.stdout.strip()


def to_mono(pcm: bytes) -> array.array:
    a = array.array("h")
    a.frombytes(pcm)
    return array.array("h", ((a[i] + a[i + 1]) // 2 for i in range(0, len(a) - 1, 2)))


def trim(a: array.array, floor_db: float = -60.0, pad: float = 0.03) -> array.array:
    threshold = 32768 * 10 ** (floor_db / 20)
    end = len(a)
    while end > 0 and abs(a[end - 1]) < threshold:
        end -= 1
    return a[:min(len(a), end + int(pad * RATE))]


def normalise(a: array.array, peak_db: float = -6.0) -> array.array:
    peak = max(1, max(abs(x) for x in a))
    gain = 32768 * 10 ** (peak_db / 20) / peak
    return array.array("h", (max(-32768, min(32767, int(x * gain))) for x in a))


def save(path: Path, a: array.array) -> None:
    with wave.open(str(path), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(a.tobytes())


def generate(client: ElevenLabs, out: Path, item) -> str:
    name, text, seconds = item
    for attempt in range(3):
        try:
            audio = client.text_to_sound_effects.convert(
                text=text, duration_seconds=seconds, prompt_influence=0.6, output_format="pcm_44100")
            a = normalise(trim(to_mono(b"".join(audio))))
            save(out / f"{name}.wav", a)
            return f"{name}: {len(a) / RATE:.2f}s"
        except Exception as error:  # the message never carries the key
            if attempt == 2:
                return f"{name}: FAILED {type(error).__name__}"
            time.sleep(3)
    return f"{name}: FAILED"


def main() -> None:
    if len(sys.argv) < 2:
        sys.exit(__doc__)
    out = Path(sys.argv[1])
    out.mkdir(parents=True, exist_ok=True)
    only = set(sys.argv[2:])
    todo = [item for item in ITEMS if not only or item[0] in only]
    client = ElevenLabs(api_key=read_key())
    with ThreadPoolExecutor(max_workers=3) as pool:
        for line in pool.map(lambda item: generate(client, out, item), todo):
            print(line, flush=True)


if __name__ == "__main__":
    main()
