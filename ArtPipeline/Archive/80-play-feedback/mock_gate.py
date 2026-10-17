# -*- coding: utf-8 -*-
"""Round 80 (2026-10-18 user's play feedback "성문은 업그레이드 없음(원정)"): the gate is one picture now, so which one? No API call:
the three gate pictures of Round 78 (level 1 wooden palisade, level 2 stone gatehouse, level 3 twin towers) laid on the far town at the
gate's plot, cropped round it, side by side. A = today's (level 1, width 312), B = level 2 at 380, C = level 3 at 464 (the level-3 size).
Run: .venv/bin/python ArtPipeline/Archive/80-play-feedback/mock_gate.py <gate_2.png> <gate_3.png> (the two deleted pictures, restored from Git)."""
import sys
from PIL import Image, ImageDraw

town = Image.open("Assets/@Art/Background/Lobby/town.png").convert("RGBA").resize((1920, 1280), Image.LANCZOS)
options = [("A", "Assets/@Art/Building/gate.png", 312), ("B", sys.argv[1], 380), ("C", sys.argv[2], 464)]
foot = (1680, 575)
crop = (1340, 200, 1920, 780)
panels = []
for label, path, width in options:
    gate = Image.open(path).convert("RGBA")
    height = round(gate.height * width / gate.width)
    gate = gate.resize((width, height), Image.LANCZOS)
    scene = town.copy()
    scene.alpha_composite(gate, (foot[0] - width // 2, foot[1] - height))
    panel = scene.crop(crop)
    draw = ImageDraw.Draw(panel)
    draw.rectangle((0, 0, 70, 40), fill=(0, 0, 0, 200))
    draw.text((12, 8), label, fill=(255, 255, 255, 255))
    panels.append(panel)

w, h = panels[0].size
sheet = Image.new("RGBA", (w * 3 + 40, h + 20), (20, 20, 24, 255))
for i, panel in enumerate(panels):
    sheet.alpha_composite(panel, (10 + i * (w + 10), 10))
sheet.convert("RGB").save("ArtPipeline/Archive/80-play-feedback/mock-gate-compare.png")
print("ok", sheet.size)
