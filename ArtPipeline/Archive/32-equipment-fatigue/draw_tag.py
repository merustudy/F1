"""The fatigue tag of the UI (round 32, B1): a plum pill with a violet rim, the cost written on it by the screen.
Drawn, no API call. Twice its size on screen (20 high), with round ends, so it is a nine-slice frame whose border is
half its height (UiArt.FatigueTag): it stretches sideways for "피로 +N" and keeps its ends.
  .venv/bin/python ArtPipeline/Archive/32-equipment-fatigue/draw_tag.py
"""
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / "Assets/@Art/UI/Frame/fatigue_tag.png"

H, W, RIM, K = 40, 64, 4, 8                 # 2x of the screen's 20 high; the rim is 2 on screen; drawn K times and brought down
FILL, EDGE = (0x2C, 0x1A, 0x3A, 255), (0x9E, 0x84, 0xDA, 255)   # the mockup's colours (mock_equipment_fatigue.py)


def main():
    big = Image.new("RGBA", (W * K, H * K), (0, 0, 0, 0))
    d = ImageDraw.Draw(big)
    d.rounded_rectangle([0, 0, W * K - 1, H * K - 1], radius=H * K // 2, fill=EDGE)
    d.rounded_rectangle([RIM * K, RIM * K, (W - RIM) * K - 1, (H - RIM) * K - 1], radius=(H - 2 * RIM) * K // 2, fill=FILL)
    big.resize((W, H), Image.LANCZOS).save(OUT)
    print(OUT.relative_to(ROOT), (W, H))


if __name__ == "__main__":
    main()
