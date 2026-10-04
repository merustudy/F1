"""The game's own screenshots with the outline now, at 2/3 and at 1/2, side by side (round 25). No API call.
  ArtPipeline/Archive/25-outline-thin/run_shots.sh <scratch dir>          (first: the game's screenshots with each outline)
  .venv/bin/python ArtPipeline/Archive/25-outline-thin/compare_shots.py <now dir> <2/3 dir> <1/2 dir>

Writes compare-battle.png (the floor 4 battle's stage and the first battle's party, the three outlines one under
another, as the screen shows them), compare-zoom.png (three places at twice the size) and game/<shot>_<now|2-3|1-2>.png
(the whole floor 4 screen).
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
FONT = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"
NAMES = [("now", "지금 외곽선 (띠 4px)"), ("2-3", "외곽선 2/3 (띠 1.35px)"), ("1-2", "외곽선 1/2 (띠 0.4px)")]
# What is the same in every run of the screenshot test: the floor 4 battle (ko_15) whole, and the party of the first
# battle (ko_24). That battle's enemies, and a new run's first battle (ko_05), are drawn anew each run.
SHOTS = [("ko_15_battle_after_advance", (100, 150, 1660, 520), "버려진 광산 4층 — 아스트리드의 공격 자세, 쓰러지는 감독관"),
         ("ko_24_battle_hit_pose", (100, 200, 960, 520), "버려진 광산 1층의 파티 — 세드릭의 피격 자세 (적은 실행마다 새로 정해져 뺐다)")]
FULL = ["ko_15_battle_after_advance"]
ZOOM = [("ko_24_battle_hit_pose", (470, 250, 900, 512)), ("ko_15_battle_after_advance", (640, 250, 1000, 540)),
        ("ko_15_battle_after_advance", (1290, 280, 1500, 505))]
BG, INK, DIM, GOLD = (20, 18, 18), (235, 235, 230), (170, 172, 180), (217, 164, 65)


def label(draw, xy, text, size=26, fill=INK):
    draw.text(xy, text, fill=fill, font=ImageFont.truetype(str(FONT), size))


def main(dirs):
    (HERE / "game").mkdir(exist_ok=True)
    for shot in FULL:
        for (variant, _), folder in zip(NAMES, dirs):
            Image.open(Path(folder) / f"{shot}.png").convert("RGB").save(HERE / "game" / f"{shot}_{variant}.png")

    width = max(box[2] - box[0] for _, box, _ in SHOTS)
    sheet = Image.new("RGB", (width + 24, sum(52 + len(NAMES) * (box[3] - box[1] + 40) for _, box, _ in SHOTS) + 12), BG)
    d = ImageDraw.Draw(sheet)
    y = 12
    for shot, box, title in SHOTS:
        label(d, (14, y), title, 28, GOLD)
        y += 52
        for (variant, name), folder in zip(NAMES, dirs):
            label(d, (14, y + 4), name, 24, DIM)
            sheet.paste(Image.open(Path(folder) / f"{shot}.png").convert("RGB").crop(box), (12, y + 36))
            y += box[3] - box[1] + 40
    sheet.save(HERE / "compare-battle.png")
    print("compare-battle.png", sheet.size)

    cells = [(b[2] - b[0]) * 2 for _, b in ZOOM]
    zh = max(b[3] - b[1] for _, b in ZOOM) * 2
    zoom = Image.new("RGB", (sum(cells) + 12 * (len(ZOOM) + 1), len(NAMES) * (zh + 44) + 56), BG)
    d = ImageDraw.Draw(zoom)
    label(d, (14, 12), "2배로 (게임 스크린샷): 세드릭의 피격 자세와 아스트리드 · 아스트리드의 공격 자세 · 고블린 주술사", 28, GOLD)
    for i, ((variant, name), folder) in enumerate(zip(NAMES, dirs)):
        y = 56 + i * (zh + 44)
        label(d, (14, y + 6), name, 24, DIM)
        x = 12
        for (shot, box), cw in zip(ZOOM, cells):
            part = Image.open(Path(folder) / f"{shot}.png").convert("RGB").crop(box)
            zoom.paste(part.resize((part.width * 2, part.height * 2), Image.NEAREST), (x, y + 38))
            x += cw + 12
    zoom.save(HERE / "compare-zoom.png")
    print("compare-zoom.png", zoom.size)


if __name__ == "__main__":
    if len(sys.argv) != 4:
        sys.exit(__doc__)
    main(sys.argv[1:])
