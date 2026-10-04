"""Drawn piece of the cooldown light (round 18), no API call. White with an alpha ramp that the screen tints and
stretches (UiPrefabSetup.Battle.BuildBattleItem, BattleItemView). Written to ArtPipeline/output/ui_placeholder and copied
to Assets/@Art/UI/Icon.
  .venv/bin/python ArtPipeline/Archive/18-cooldown-light/draw_pieces.py

- charge_ramp: clear on the left, opaque on the right, rising as the square of the way across (the glow behind the front
  of the charge in mock_cooldown_light.py). The cell uses it twice: tinted gold left of the front as the glow, and tinted
  dark right of the front as the soft left end of the dark (the mockup eased that one with a smoothstep; over its 6 px
  the two are alike). Rounded up, so that no pixel is fully clear: the importer trims a sprite to what is not fully clear.
"""
import math
import shutil
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / "ArtPipeline" / "output" / "ui_placeholder"
ASSET = ROOT / "Assets" / "@Art" / "UI" / "Icon"


def ramp(width=64, height=8):
    alpha = Image.new("L", (width, height), 0)
    px = alpha.load()
    for i in range(width):
        a = math.ceil(255 * ((i + 0.5) / width) ** 2 - 1e-9)
        for j in range(height):
            px[i, j] = max(1, a)
    out = Image.new("RGBA", (width, height), (255, 255, 255, 0))
    out.putalpha(alpha)
    return out


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    path = OUT / "charge_ramp.png"
    ramp().save(path)
    shutil.copyfile(path, ASSET / "charge_ramp.png")
    print(path, "->", ASSET / "charge_ramp.png")


if __name__ == "__main__":
    main()
