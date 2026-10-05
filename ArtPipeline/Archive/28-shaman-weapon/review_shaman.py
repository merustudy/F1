"""The review sheet of the goblin shaman's three pictures (round 28), no API call.
  .venv/bin/python ArtPipeline/Archive/28-shaman-weapon/review_shaman.py   # review-shaman.png

Round 27's sheet (../27-monster-poses/review_monster_poses.py) for the shaman: its game figure, the new attack pose (here)
and its hit pose (round 27, confirmed) at one scale on the stage's colour, the floor line (blue) and its back foot
(orange), and the three heads at the canvas's own size.
"""
import json
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
R27 = ROOT / "ArtPipeline/Archive/27-monster-poses"
FONT = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"
FLOOR, Z = 876, 0.5
INK, DIM, BACK, STAGE = (235, 235, 230), (160, 162, 170), (22, 20, 20), (58, 46, 36)
KEY = "goblin_shaman"
PICTURES = [("idle", R27 / f"candidates/{KEY}_idle_wide.png", "대기 (게임의 그림)"),
            ("attack", HERE / f"candidates/{KEY}_attack_wide.png", "공격 (새로 그림: 등불 지팡이로 내리침)"),
            ("hit", R27 / f"candidates/{KEY}_hit_wide.png", "피격 (Round 27, 확정)")]


def font(size):
    return ImageFont.truetype(str(FONT), size)


def main():
    places = json.loads((R27 / "heads.json").read_text(encoding="utf-8"))[KEY]
    places.update(json.loads((HERE / "heads.json").read_text(encoding="utf-8"))[KEY])
    images = {pose: Image.open(path).convert("RGBA") for pose, path, _ in PICTURES}
    boxes = {pose: image.getchannel("A").getbbox() for pose, image in images.items()}
    top, bottom = min(b[1] for b in boxes.values()) - 12, max(b[3] for b in boxes.values()) + 12
    cells = []
    for pose, _, label in PICTURES:
        b = boxes[pose]
        crop = images[pose].crop((b[0] - 12, top, b[2] + 12, bottom))
        cell = Image.new("RGBA", crop.size, STAGE + (255,))
        cell.alpha_composite(crop)
        cell = cell.convert("RGB").resize((round(crop.width * Z), round(crop.height * Z)), Image.LANCZOS)
        d = ImageDraw.Draw(cell)
        d.line([0, round((FLOOR - top) * Z), cell.width, round((FLOOR - top) * Z)], fill=(70, 140, 220), width=1)
        fx = round((places["foot"] - (b[0] - 12)) * Z)
        for y in range(0, cell.height, 8):
            d.line([fx, y, fx, y + 4], fill=(230, 150, 60), width=1)
        cells.append((cell, label))
    idle = places["idle"]
    side = max(220, min(340, round(2.4 * max(idle[2] - idle[0], idle[3] - idle[1]))))
    heads = []
    for pose, _, label in PICTURES:
        hb = places[pose]
        cx, cy = (hb[0] + hb[2]) // 2, (hb[1] + hb[3]) // 2
        crop = images[pose].crop((cx - side // 2, cy - side // 2, cx + side // 2, cy + side // 2))
        cell = Image.new("RGBA", crop.size, STAGE + (255,))
        cell.alpha_composite(crop)
        cell = cell.convert("RGB")
        d = ImageDraw.Draw(cell)
        for y in range(0, cell.height, 30):
            d.line([0, y, cell.width, y], fill=(70, 70, 80), width=1)
        heads.append((cell, label.split(" (")[0]))
    body_h = max(c.height for c, _ in cells)
    width = max(24 + sum(c.width + 16 for c, _ in cells), 24 + sum(c.width + 16 for c, _ in heads))
    sheet = Image.new("RGB", (width, 60 + body_h + 34 + side + 34), BACK)
    d = ImageDraw.Draw(sheet)
    d.text((24, 12), "고블린 주술사 — 공격 자세 (Round 28) · 같은 배율, 파란 줄은 바닥선, 주황 점선은 디딘 뒷발", fill=INK, font=font(24))
    x = 24
    for cell, label in cells:
        sheet.paste(cell, (x, 60))
        d.text((x + 4, 60 + body_h + 4), label, fill=DIM, font=font(18))
        x += cell.width + 16
    x, y = 24, 60 + body_h + 34
    for cell, label in heads:
        sheet.paste(cell, (x, y))
        d.text((x + 4, y + side + 4), f"{label}의 머리", fill=DIM, font=font(16))
        x += cell.width + 16
    sheet.save(HERE / "review-shaman.png")
    print(f"리뷰 시트: review-shaman.png ({sheet.width}x{sheet.height})")


if __name__ == "__main__":
    main()
