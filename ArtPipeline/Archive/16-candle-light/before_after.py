"""Copies the round's screenshots next to the mockups and puts the battle before (round 15) and after side by side, with
the storm's two moments under them. Each new run draws its own map, so the first battle's enemies differ between runs:
the after is the English battle, which met the goblins the Korean one met before.
  .venv/bin/python ArtPipeline/Archive/16-candle-light/before_after.py <screenshot dir>
"""
import shutil, sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent; ROOT = HERE.parents[2]
BEFORE = ROOT / "ArtPipeline/Archive/15-seven-cells/game"
NAMES = ["ko_04_map", "ko_05_battle", "ko_07_reward", "ko_09_boss_battle", "ko_18_battle_deaths_door",
         "ko_19_battle_storm_near", "ko_20_battle_storm", "en_05_battle"]
FONT = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"

def main():
    src = Path(sys.argv[1]); out = HERE / "game"; out.mkdir(exist_ok=True)
    for n in NAMES:
        shutil.copy(src / f"{n}.png", out / f"{n}.png")
    tiles = [("전 (Round 15): 비네트가 뒤집혀 가운데가 가장 어둡다", Image.open(BEFORE / "ko_05_battle.png")),
             ("후 (Round 16, B안): 양초를 중심으로 반원의 빛 (같은 적 무리가 나온 영어 화면)", Image.open(out / "en_05_battle.png")),
             ("폭풍 5초 전: 양초가 짧아지고 빛이 줄었다", Image.open(out / "ko_19_battle_storm_near.png")),
             ("폭풍: 불이 꺼져 무대 전체가 어둡다", Image.open(out / "ko_20_battle_storm.png"))]
    scale, gap, lab = 0.5, 24, 44; w, h = int(1920 * scale), int(1080 * scale)
    sheet = Image.new("RGB", (2 * (w + gap) + gap, 2 * (h + lab + gap) + gap), (18, 18, 18)); d = ImageDraw.Draw(sheet); f = ImageFont.truetype(str(FONT), 24)
    for i, (name, im) in enumerate(tiles):
        x = gap + (i % 2) * (w + gap); y = gap + (i // 2) * (h + lab + gap)
        d.text((x, y), name, font=f, fill=(235, 235, 235)); sheet.paste(im.convert("RGB").resize((w, h), Image.LANCZOS), (x, y + lab))
    sheet.save(HERE / "before-after.png"); print("before-after", sheet.size, "| copied", len(NAMES))

if __name__ == "__main__": main()
