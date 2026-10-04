"""Copies the round's screenshots next to the review and puts the same battles before (round 16) and after side by side:
the outline at half its width and the monsters' eyes without pupils. Each new run draws its own map, so only battles
that met the same enemies are paired: the English first battle (goblin raiders), the Korean one (cave rats) and the
boss battle after an advance (the overseer with a goblin raider and the shaman).
  .venv/bin/python ArtPipeline/Archive/17-outline-pupils/before_after.py <round 16 screenshot dir> <screenshot dir>
"""
import shutil, sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent; ROOT = HERE.parents[2]
NAMES = ["ko_04_map", "ko_05_battle", "ko_09_boss_battle", "ko_15_battle_after_advance", "ko_18_battle_deaths_door",
         "ko_19_battle_storm_near", "ko_20_battle_storm", "en_05_battle"]
FONT = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"
PAIRS = [("영어 전투: 기사와 고블린 약탈자", "en_05_battle", (640, 240, 1660, 520), 1),
         ("보스전 전진 뒤: 감독관, 약탈자, 주술사", "ko_15_battle_after_advance", (960, 170, 1660, 520), 1),
         ("1열 고블린 약탈자의 얼굴 (2배)", "en_05_battle", (1080, 285, 1240, 385), 2),
         ("2열 동굴 쥐의 얼굴 (2배)", "ko_05_battle", (1240, 385, 1400, 465), 2)]


def main():
    before, after = Path(sys.argv[1]), Path(sys.argv[2]); out = HERE / "game"; out.mkdir(exist_ok=True)
    for n in NAMES:
        shutil.copy(after / f"{n}.png", out / f"{n}.png")
    f = ImageFont.truetype(str(FONT), 22); gap, lab = 20, 34
    rows = []
    for name, shot, box, zoom in PAIRS:
        parts = [Image.open(d / f"{shot}.png").convert("RGB").crop(box) for d in (before, after)]
        parts = [p.resize((p.width * zoom, p.height * zoom), Image.NEAREST) for p in parts]
        row = Image.new("RGB", (2 * parts[0].width + gap, parts[0].height + lab), (18, 18, 18)); d = ImageDraw.Draw(row)
        d.text((0, 4), name, font=f, fill=(235, 235, 235))
        for i, p in enumerate(parts):
            row.paste(p, (i * (p.width + gap), lab))
        rows.append(row)
    head = 44
    sheet = Image.new("RGB", (max(r.width for r in rows) + 2 * gap, head + sum(r.height for r in rows) + gap * (len(rows) + 1)), (18, 18, 18))
    ImageDraw.Draw(sheet).text((gap, gap), "왼쪽: 전 (띠 8px, 눈동자 있음)   오른쪽: 후 (띠 4px, 몬스터 눈동자 없음)   같은 적이 나온 전투끼리", font=ImageFont.truetype(str(FONT), 24), fill=(200, 200, 200))
    y = gap + head
    for r in rows:
        sheet.paste(r, (gap, y)); y += r.height + gap
    sheet.save(HERE / "before-after.png"); print("before-after", sheet.size, "| copied", len(NAMES))


if __name__ == "__main__":
    main()
