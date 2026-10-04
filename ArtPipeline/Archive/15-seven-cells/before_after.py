"""Copies the round's screenshots next to the mockups and puts the battle screen before (round 14) and after side by side.
  .venv/bin/python ArtPipeline/Archive/15-seven-cells/before_after.py <screenshot dir>
"""
import shutil, sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent; ROOT = HERE.parents[2]
BEFORE = ROOT / "ArtPipeline/Archive/14-ui-diablo/game"
NAMES = ["ko_04_map", "ko_05_battle", "ko_07_reward", "ko_09_boss_battle", "ko_17_map_inventory_selected", "ko_18_battle_deaths_door", "en_05_battle"]
FONT = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"

def main():
    src = Path(sys.argv[1]); out = HERE / "game"; out.mkdir(exist_ok=True)
    for n in NAMES:
        shutil.copy(src / f"{n}.png", out / f"{n}.png")
    pairs = [("전 (Round 14): 최대 8칸, 칸 180x50, 간격 4", Image.open(BEFORE / "ko_05_battle.png")),
             ("후 (개정 6, B안): 최대 7칸, 칸 180x60, 간격 2", Image.open(out / "ko_05_battle.png"))]
    crop = (100, 600, 1120, 1080); gap, lab = 24, 44; w, h = crop[2] - crop[0], crop[3] - crop[1]
    sheet = Image.new("RGB", (2 * (w + gap) + gap, h + lab + 2 * gap), (18, 18, 18)); d = ImageDraw.Draw(sheet); f = ImageFont.truetype(str(FONT), 26)
    for i, (name, im) in enumerate(pairs):
        x = gap + i * (w + gap); d.text((x, gap), name, font=f, fill=(235, 235, 235)); sheet.paste(im.convert("RGB").crop(crop), (x, gap + lab))
    sheet.save(HERE / "before-after.png"); print("before-after", sheet.size, "| copied", len(NAMES))

if __name__ == "__main__": main()
