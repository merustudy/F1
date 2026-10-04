"""Before and after round 18, the same battle at 7.3 s (the first battle of a new run): the board panel's cells with the
cooldown of before (a dried-blood fill inside a 7 px margin, behind the icon) and of now (light).
  .venv/bin/python ArtPipeline/Archive/18-cooldown-light/before_after.py
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent; ROOT = HERE.parents[2]
BEFORE = ROOT / "ArtPipeline/Archive/17-outline-pupils/game/ko_05_battle.png"
AFTER = HERE / "game/ko_05_battle.png"
FONT = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"
CROP = (100, 600, 1440, 960)


def main():
    rows = [("전 (Round 17): 마른 피 채움, 칸 안쪽 7px, 아이콘 뒤. 빈 칸은 밝은 뼈색", BEFORE), ("후 (Round 18): 어둠에서 시작해 왼쪽부터 촛불 금빛으로 밝아짐. 빈 칸도 어둡다", AFTER)]
    ims = [Image.open(p).convert("RGB").crop(CROP) for _, p in rows]
    lab, gap = 40, 20; w = ims[0].width
    out = Image.new("RGB", (w + 2 * gap, sum(im.height + lab + gap for im in ims) + gap), (18, 18, 18)); d = ImageDraw.Draw(out); y = gap
    for (name, _), im in zip(rows, ims):
        d.text((gap, y), name, font=ImageFont.truetype(str(FONT), 24), fill=(235, 235, 235)); out.paste(im, (gap, y + lab)); y += im.height + lab + gap
    out.save(HERE / "before-after.png"); print("before-after.png", out.size)


if __name__ == "__main__":
    main()
