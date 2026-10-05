#!/usr/bin/env python3
"""Prints the account's plan and the credits used in this billing period. Generates nothing and costs nothing.

    .venv/bin/python SoundPipeline/tools/usage.py

Run it before and after a round to put what the round really spent beside the ledger's estimate.
The key is read from the keychain and never shown.
"""
from __future__ import annotations

import datetime

import soundlib


def main() -> int:
    from elevenlabs import ElevenLabs

    client = ElevenLabs(api_key=soundlib.read_key())
    try:
        sub = client.user.subscription.get()
    except Exception as error:  # noqa: BLE001 - shown without its message so the key never leaks
        soundlib.fail(f"could not read the subscription: {soundlib.describe_error(error)}")
    reset = sub.next_character_count_reset_unix
    reset_text = datetime.datetime.fromtimestamp(reset).strftime("%Y-%m-%d") if reset else "-"
    print(f"tier {sub.tier}, credits used {sub.character_count} of {sub.character_limit}, resets {reset_text}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
