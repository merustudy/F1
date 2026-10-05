"""Shared pieces of the sound pipeline: paths, the style document, the rosters, the key and the ledger.

The style rules live in SoundPipeline/STYLE_SOUND.md and are read at run time, never copied here.
The key is read from the macOS login keychain only when a call is about to be made; it is never
printed, logged or written anywhere (Docs/Architecture/14_SOUND.md).
"""
from __future__ import annotations

import csv
import datetime
import getpass
import json
import re
import subprocess
import sys
from pathlib import Path

# tools/ -> SoundPipeline/. Paths come from the file, so the scripts run from anywhere.
ROOT = Path(__file__).resolve().parents[1]
STYLE = ROOT / "STYLE_SOUND.md"
ROSTERS = ROOT / "Rosters"
OUTPUT = ROOT / "output"
LEDGER = ROOT / "Archive" / "calls.csv"
LEDGER_FIELDS = ["Time", "Type", "Key", "Variant", "Model", "Seconds", "EstimatedUsd", "Result"]
KEYCHAIN_SERVICE = "ELEVENLABS_API_KEY"

# The API price list (elevenlabs.io/pricing/api, read 2026-10-05) bills by the minute of audio.
# The ledger's cost is an estimate from it; usage.py reads what the account actually spent.
SFX_USD_PER_MINUTE = 0.12
MUSIC_USD_PER_MINUTE = 0.15

KEY_PATTERN = re.compile(r"[a-z0-9]+(-[a-z0-9]+)*")
VARIANT_PATTERN = re.compile(r"[A-Z]")


def fail(message: str) -> None:
    sys.exit(f"[sound] {message}")


def read_style() -> dict:
    """The style document's numbered sections: a ```text block, or a list of '- ' bullets."""
    if not STYLE.exists():
        fail(f"style document missing: {STYLE}")
    text = STYLE.read_text(encoding="utf-8")
    sections = {}
    for match in re.finditer(r"^## (\d+)\. [^\n]*\n(.*?)(?=^## |\Z)", text, re.M | re.S):
        body = match.group(2)
        block = re.search(r"```text\n(.*?)\n```", body, re.S)
        if block:
            sections[int(match.group(1))] = " ".join(block.group(1).split())
        else:
            sections[int(match.group(1))] = [line[2:].strip() for line in body.splitlines() if line.startswith("- ")]
    style = {
        "effect": sections.get(1),
        "sting": sections.get(2),
        "music_positive": sections.get(3),
        "music_negative": sections.get(4),
    }
    for name, value in style.items():
        if not value:
            fail(f"style document section is empty or missing: {name}")
    return style


def read_roster(name: str, columns: list) -> dict:
    """Rows of Rosters/<name>.csv by Key. Keys are kebab-case and unique; every column must be there."""
    path = ROSTERS / f"{name}.csv"
    if not path.exists():
        fail(f"roster missing: {path}")
    with path.open(encoding="utf-8", newline="") as f:
        reader = csv.DictReader(f)
        missing = [c for c in columns if c not in (reader.fieldnames or [])]
        if missing:
            fail(f"{path.name} lacks columns: {missing}")
        rows = {}
        for row in reader:
            key = row["Key"].strip()
            if not KEY_PATTERN.fullmatch(key):
                fail(f"{path.name}: key is not kebab-case: {key!r}")
            if key in rows:
                fail(f"{path.name}: key appears twice: {key}")
            for column in columns:
                if not (row.get(column) or "").strip():
                    fail(f"{path.name}: {key} has no {column}")
            rows[key] = row
    return rows


def parse_variants(text: str) -> list:
    variants = [v.strip() for v in text.split(",") if v.strip()]
    if not variants or any(not VARIANT_PATTERN.fullmatch(v) for v in variants) or len(set(variants)) != len(variants):
        fail(f"variants must be distinct capital letters like A,B,C: {text!r}")
    return variants


def read_key() -> str:
    done = subprocess.run(
        ["security", "find-generic-password", "-s", KEYCHAIN_SERVICE, "-a", getpass.getuser(), "-w"],
        capture_output=True, text=True, check=False)
    if done.returncode != 0 or not done.stdout.strip():
        fail(f"could not read {KEYCHAIN_SERVICE} from the login keychain")
    return done.stdout.strip()


def describe_error(error: Exception) -> str:
    """What went wrong without the message body: the key must never reach the screen or a log."""
    status = getattr(error, "status_code", None)
    return f"{type(error).__name__}" + (f" (HTTP {status})" if status else "")


def now_iso() -> str:
    return datetime.datetime.now().astimezone().isoformat(timespec="seconds")


def log_call(kind: str, key: str, variant: str, model: str, seconds: float, usd_per_minute: float, result: str) -> float:
    """Appends one call to the ledger, failed calls included, and returns its estimated cost."""
    usd = seconds / 60.0 * usd_per_minute
    LEDGER.parent.mkdir(parents=True, exist_ok=True)
    new = not LEDGER.exists()
    with LEDGER.open("a", encoding="utf-8", newline="") as f:
        writer = csv.DictWriter(f, fieldnames=LEDGER_FIELDS, lineterminator="\n")
        if new:
            writer.writeheader()
        writer.writerow({
            "Time": now_iso(), "Type": kind, "Key": key, "Variant": variant, "Model": model,
            "Seconds": f"{seconds:g}", "EstimatedUsd": f"{usd:.4f}", "Result": result,
        })
    return usd


def write_sidecar(audio: Path, info: dict) -> None:
    """The prompt and settings beside each candidate, for the playlist and the round's README."""
    audio.with_suffix(".json").write_text(json.dumps(info, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
