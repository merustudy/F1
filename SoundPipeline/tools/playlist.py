#!/usr/bin/env python3
"""Writes output/index.html: every candidate under output/ with a player, grouped by key in roster order.

    .venv/bin/python SoundPipeline/tools/playlist.py
    cd SoundPipeline/output && python3 -m http.server 8765 --bind 127.0.0.1   # then open http://127.0.0.1:8765/

Opened as file:// the audio did not play (Kit/05), so the page is served locally and the server is
stopped after the verdict. The music candidates come at very different loudness, and the louder one
sounds better whatever it is; the page plays them all at the quietest one's loudness (the files stay
as they are). Makes no call.
"""
from __future__ import annotations

import array
import html
import json
import math
import subprocess
import tempfile
import wave
from pathlib import Path

import soundlib

PAGE = """<!doctype html>
<html lang="ko"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<title>Sound candidates</title>
<style>
:root {{ color-scheme: dark; }}
body {{ background: #16161c; color: #ebebe6; font: 16px/1.5 -apple-system, "Pretendard", sans-serif; margin: 0 auto; max-width: 980px; padding: 24px 16px 64px; }}
h1 {{ font-size: 24px; margin: 0 0 4px; }}
h2 {{ font-size: 20px; margin: 36px 0 8px; border-bottom: 1px solid #3a4150; padding-bottom: 6px; }}
p.note {{ color: #a0a0a0; margin: 0 0 8px; }}
.row {{ display: grid; grid-template-columns: 48px 1fr; gap: 12px; align-items: center; margin: 10px 0; }}
.label {{ font-size: 22px; font-weight: 700; text-align: center; color: #f0c870; }}
audio {{ width: 100%; }}
details {{ color: #a0a0a0; font-size: 13px; margin: 2px 0 0 60px; }}
code {{ white-space: pre-wrap; word-break: break-word; }}
</style></head><body>
<h1>소리 후보</h1>
<p class="note">Key마다 판정해 주세요: "확정 A", "재생성 - 방향", "소재 교체". 효과음은 -6dBFS로 맞췄고, 배경음은 받은 그대로이고, 세 후보가 같은 크기로 들리게 재생 음량만 맞췄습니다. 끝의 페이드아웃은 확정한 곡을 전체 길이로 만든 뒤 자릅니다.</p>
{groups}
<script>document.querySelectorAll("audio[data-gain]").forEach(a => {{ a.volume = parseFloat(a.dataset.gain); }});</script>
</body></html>
"""


def loudness_db(path: Path):
    """RMS in dBFS, decoded with macOS afconvert; None where that is not available."""
    try:
        with tempfile.TemporaryDirectory() as tmp:
            decoded = Path(tmp) / "decoded.wav"
            subprocess.run(["afconvert", "-f", "WAVE", "-d", "LEI16@44100", str(path), str(decoded)], check=True, capture_output=True)
            with wave.open(str(decoded)) as w:
                samples = array.array("h")
                samples.frombytes(w.readframes(w.getnframes()))
    except (OSError, subprocess.CalledProcessError, wave.Error):
        return None
    mean = sum(v * v for v in samples) / max(1, len(samples))
    return 10 * math.log10(max(mean, 1e-9) / 32768 ** 2)


def group_html(title: str, items: list, gains: dict) -> str:
    rows = []
    for audio, info in items:
        prompt = info.get("prompt") or json.dumps(info.get("plan"), ensure_ascii=False, indent=1)
        gain = gains.get(audio)
        attr = f' data-gain="{gain:.3f}"' if gain is not None else ""
        note = f", 재생 음량 {20 * math.log10(gain):+.1f}dB로 맞춤" if gain is not None and gain < 0.999 else ""
        rows.append(
            f'<div class="row"><div class="label">{html.escape(info["variant"])}</div>'
            f'<audio controls preload="metadata" src="{html.escape(audio)}"{attr}></audio></div>'
            f'<details><summary>문구 ({info["seconds"]:g}초, {html.escape(info["model"])}{note})</summary><code>{html.escape(prompt)}</code></details>')
    return f"<h2>{html.escape(title)}</h2>\n" + "\n".join(rows)


def collect(kind: str, pattern: str, roster: dict) -> list:
    found = {}
    for sidecar in sorted((soundlib.OUTPUT / kind).glob("*.json")):
        info = json.loads(sidecar.read_text(encoding="utf-8"))
        audio = next((p for p in sidecar.parent.glob(sidecar.stem + pattern)), None)
        if audio is not None:
            found.setdefault(info["key"], []).append((f"{kind}/{audio.name}", info))
    groups = []
    for key in roster:  # roster order, not file order
        if key in found:
            items = sorted(found[key], key=lambda item: item[1]["variant"])
            gains = {}
            if kind == "bgm":
                levels = {audio: loudness_db(soundlib.OUTPUT / audio) for audio, _ in items}
                known = [db for db in levels.values() if db is not None]
                if known:
                    quietest = min(known)
                    gains = {audio: 10 ** ((quietest - db) / 20) for audio, db in levels.items() if db is not None}
            groups.append(group_html(f"{roster[key]['Name']} — {key}", items, gains))
    return groups


def main() -> int:
    sfx = soundlib.read_roster("sfx", ["Key", "Name"])
    bgm = soundlib.read_roster("bgm", ["Key", "Name"])
    groups = collect("sfx", ".wav", sfx) + collect("bgm", ".mp3", bgm)
    if not groups:
        soundlib.fail("no candidates under output/sfx or output/bgm")
    page = soundlib.OUTPUT / "index.html"
    page.write_text(PAGE.format(groups="\n".join(groups)), encoding="utf-8")
    print(page)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
