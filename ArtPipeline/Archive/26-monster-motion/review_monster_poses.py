"""The review sheet of the goblin raider's attack and hit poses (round 26), no API call.
  .venv/bin/python ArtPipeline/Archive/26-monster-motion/review_monster_poses.py   # review-poses.png

Top: the game's figure, the attack and the hit on the pose canvas at one scale (half the canvas: about one and a half times
the battle's), on the stage's colour, standing on the floor line (blue) with the back foot (the rightmost, orange line)
where the figure's is. Below: the three heads at the canvas's own size, to see that the head keeps its size. What to look
at is Docs/Architecture/13_ART_PIPELINE.md "자세 그림의 점검", for a creature: the same creature and gear, the blank yellow
eyes without pupils, the head the same size, the planted back foot and the floor line, the weapon whole, the expression.
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
sys.path.insert(0, str(HERE))
from fit_monster_poses import APPROVED, KEY, face_and_back_foot, skin_colour  # noqa: E402

FONT = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"
FLOOR = 876
Z = 0.5
HEAD = (300, 270)
INK, DIM, BACK, STAGE = (235, 235, 230), (160, 162, 170), (22, 20, 20), (58, 46, 36)
LABELS = (("idle", "대기 (게임의 그림)"), ("attack", "공격"), ("hit", "피격"))


def font(size):
    return ImageFont.truetype(str(FONT), size)


def main():
    from PIL import Image as I
    skin = skin_colour(I.open(APPROVED).convert("RGBA"))
    images = {pose: Image.open(HERE / "candidates" / f"{KEY}_{pose}_wide.png").convert("RGBA") for pose, _ in LABELS}
    boxes = {pose: image.getchannel("A").getbbox() for pose, image in images.items()}
    feet = {pose: face_and_back_foot(image, skin) for pose, image in images.items()}
    top = min(b[1] for b in boxes.values()) - 12
    bottom = max(b[3] for b in boxes.values()) + 12
    foot_x = feet["idle"][1]["cx"]

    cells = []
    for pose, _ in LABELS:
        b = boxes[pose]
        crop = images[pose].crop((b[0] - 12, top, b[2] + 12, bottom))
        cell = Image.new("RGBA", crop.size, STAGE + (255,))
        cell.alpha_composite(crop)
        cell = cell.convert("RGB").resize((round(crop.width * Z), round(crop.height * Z)), Image.LANCZOS)
        d = ImageDraw.Draw(cell)
        d.line([0, round((FLOOR - top) * Z), cell.width, round((FLOOR - top) * Z)], fill=(70, 140, 220), width=1)
        fx = round((foot_x - (b[0] - 12)) * Z)
        for y in range(0, cell.height, 8):
            d.line([fx, y, fx, y + 4], fill=(230, 150, 60), width=1)
        cells.append(cell)

    heads = []
    for pose, _ in LABELS:
        face = feet[pose][0]["box"]
        cx, cy = (face[0] + face[2]) // 2, (face[1] + face[3]) // 2
        crop = images[pose].crop((cx - HEAD[0] // 2, cy - HEAD[1] // 2 - 40, cx + HEAD[0] // 2, cy + HEAD[1] // 2 - 40))
        cell = Image.new("RGBA", crop.size, STAGE + (255,))
        cell.alpha_composite(crop)
        cell = cell.convert("RGB")
        d = ImageDraw.Draw(cell)
        for y in range(0, cell.height, 30):
            d.line([0, y, cell.width, y], fill=(70, 70, 80), width=1)
        heads.append((cell, face[3] - face[1], face[2] - face[0]))

    width = max(24 + sum(c.width + 16 for c in cells), 24 + 3 * (HEAD[0] + 16))
    height = 60 + max(c.height for c in cells) + 40 + 44 + HEAD[1] + 40
    sheet = Image.new("RGB", (width, height), BACK)
    d = ImageDraw.Draw(sheet)
    d.text((24, 12), "고블린 약탈자 — 공격·피격 자세 시험 (Round 26)", fill=INK, font=font(28))
    x = 24
    for cell, (_, label) in zip(cells, LABELS):
        sheet.paste(cell, (x, 60))
        d.text((x + 4, 60 + cell.height + 6), label, fill=DIM, font=font(20))
        x += cell.width + 16
    y = 60 + max(c.height for c in cells) + 40
    d.text((24, y), "머리 (캔버스 그대로의 크기, 회색 줄 30px 간격)", fill=INK, font=font(22))
    x = 24
    for (cell, h, w), (_, label) in zip(heads, LABELS):
        sheet.paste(cell, (x, y + 40))
        d.text((x + 4, y + 40 + cell.height + 6), f"{label}: 얼굴 피부 {w}x{h}px", fill=DIM, font=font(18))
        x += cell.width + 16
    sheet.save(HERE / "review-poses.png")
    print(f"리뷰 시트: review-poses.png ({sheet.width}x{sheet.height})")


if __name__ == "__main__":
    main()
