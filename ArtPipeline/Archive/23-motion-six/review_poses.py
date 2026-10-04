"""The review sheet of the six mercenaries' attack and hit poses (round 23), no API call.
  .venv/bin/python ArtPipeline/Archive/23-motion-six/fit_poses.py       (first: the poses at the figures' size)
  .venv/bin/python ArtPipeline/Archive/23-motion-six/review_poses.py    # review-poses.png

A row per mercenary: the approved figure (idle), the attack pose and the hit pose, at one scale on fit_poses.py's wide
canvas, standing on the floor line (the blue line) with the back foot where the idle figure's is, on the stage's colour.
The scale is half the canvas: about one and a half times the battle's.
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
sys.path.insert(0, str(HERE))
from fit_poses import drawn  # noqa: E402

OUTPUT = ROOT / "ArtPipeline/output/character"
FONT = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"
OUT = HERE / "review-poses.png"
JOBS = [("knight", "로언 · 기사", "장검"), ("valkyrie", "아스트리드 · 발키리", "대도끼"), ("bishop", "엘라 · 주교", "태양 지팡이"),
        ("paladin", "세드릭 · 성기사", "가시 철퇴와 방패"), ("archmage", "미라 · 대마법사", "수정 지팡이"), ("spellblade", "카이 · 마검사", "붉은 검")]
POSES = [("idle", "대기 (게임의 그림)"), ("attack", "공격"), ("hit", "피격")]
# A mercenary who holds a different thing in each hand keeps each in its hand in every pose (user 2026-10-04: the
# paladin's attack had swapped the mace and the shield; a single one-handed weapon may change hands).
HANDS = {"paladin": "철퇴는 오른손(앞 팔), 방패는 왼팔"}
Z = 0.5
FLOOR = 896 - 20
INK, DIM, BACK, STAGE = (235, 235, 230), (160, 162, 170), (22, 20, 20), (58, 46, 36)


def font(size):
    return ImageFont.truetype(str(FONT), size)


def row(key, name, weapon):
    images = [Image.open(OUTPUT / (f"{key}_idle_wide.png" if pose == "idle" else f"{drawn(key, pose)}_wide.png")).convert("RGBA") for pose, _ in POSES]
    boxes = [image.getchannel("A").getbbox() for image in images]
    top = min(b[1] for b in boxes) - 12
    bottom = max(b[3] for b in boxes) + 12
    cells = []
    for image, box in zip(images, boxes):
        crop = image.crop((box[0] - 12, top, box[2] + 12, bottom))
        cell = Image.new("RGBA", crop.size, STAGE + (255,))
        cell.alpha_composite(crop)
        cell = cell.convert("RGB").resize((round(crop.width * Z), round(crop.height * Z)), Image.LANCZOS)
        ImageDraw.Draw(cell).line([0, round((FLOOR - top) * Z), cell.width, round((FLOOR - top) * Z)], fill=(70, 140, 220), width=1)
        cells.append(cell)
    height = max(c.height for c in cells)
    out = Image.new("RGB", (24 + sum(c.width + 16 for c in cells), height + 84), BACK)
    d = ImageDraw.Draw(out)
    d.text((24, 10), name, fill=INK, font=font(28))
    hands = f" — 손: {HANDS[key]} (세 그림이 같아야 한다)" if key in HANDS else ""
    d.text((24 + d.textlength(name, font=font(28)) + 16, 16), weapon + hands, fill=DIM, font=font(20))
    x = 24
    for cell, (_, label) in zip(cells, POSES):
        out.paste(cell, (x, 52))
        d.text((x + 4, 52 + cell.height + 4), label, fill=DIM, font=font(20))
        x += cell.width + 16
    return out


def main():
    rows = [row(*job) for job in JOBS]
    width = max(r.width for r in rows)
    head = Image.new("RGB", (width, 118), BACK)
    d = ImageDraw.Draw(head)
    d.text((24, 14), "용병 여섯의 공격·피격 자세 (Round 23) — 대기 / 공격 / 피격", fill=INK, font=font(34))
    d.text((24, 58), "확정 원본을 기준 그림으로 붙여 자세와 표정만 바꿨다(호출 12회). 모양이 변하지 않는 부위로 잰 배율로",
           fill=DIM, font=font(20))
    d.text((24, 84), "대기 그림과 같은 크기에, 디딘 뒷발을 같은 자리에 세웠다. 파란 선은 바닥선", fill=DIM, font=font(20))
    sheet = Image.new("RGB", (width, head.height + sum(r.height for r in rows)), BACK)
    sheet.paste(head, (0, 0))
    y = head.height
    for r in rows:
        sheet.paste(r, (0, y))
        y += r.height
    sheet.save(OUT)
    print("리뷰 시트:", OUT, sheet.size)


if __name__ == "__main__":
    main()
