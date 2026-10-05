"""The review sheet of the five monsters' attack and hit poses (rounds 26 and 27), no API call.
  .venv/bin/python ArtPipeline/Archive/27-monster-poses/review_monster_poses.py   # review-poses.png

A block per monster: the game's figure, the attack and the hit on the pose canvas at one scale (half the canvas), on the
stage's colour, the floor line (blue) and the back foot of the figure (orange): the poses keep it planted. Below them the
heads at the canvas's own size, to see that a head keeps its size. The goblin raider is round 26's (confirmed), for
comparison. The shaman has no attack pose: it has no weapon item, so the rule (Docs/Design/10 §5) never shows one.
What to look at is Docs/Architecture/13_ART_PIPELINE.md "자세 그림의 점검", for a creature: the same creature and gear,
the blank yellow eyes without pupils, the head the same size, the planted back foot and the floor line, the weapon whole,
the expression.
"""
import json
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
R26 = ROOT / "ArtPipeline/Archive/26-monster-motion"
sys.path.insert(0, str(R26))
from fit_monster_poses import APPROVED as RAIDER_RAW, face_and_back_foot, skin_colour  # noqa: E402

FONT = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"
FLOOR = 876
Z = 0.5
INK, DIM, BACK, STAGE = (235, 235, 230), (160, 162, 170), (22, 20, 20), (58, 46, 36)
MONSTERS = [("goblin_raider", "고블린 약탈자 (Round 26, 확정)"), ("cave_rat", "동굴 쥐"), ("goblin_archer", "고블린 궁수"),
            ("goblin_shaman", "고블린 주술사 — 공격 자세 없음 (무기 장비가 없어 공격 자세가 보이지 않는다)"), ("mine_overseer", "광산 감독관 (보스, 화면에서 150%)")]
LABELS = (("idle", "대기 (게임의 그림)"), ("attack", "공격"), ("hit", "피격"))


def font(size):
    return ImageFont.truetype(str(FONT), size)


def raider_places(images):
    """Round 26's raider: its heads and back foot found as round 26 finds them (the skin of its face and feet)."""
    skin = skin_colour(Image.open(RAIDER_RAW).convert("RGBA"))
    found = {pose: face_and_back_foot(image, skin) for pose, image in images.items()}
    places = {pose: list(face["box"]) for pose, (face, _) in found.items()}
    places["foot"] = found["idle"][1]["cx"]
    return places


def block(key, title, places):
    folder = R26 if key == "goblin_raider" else HERE
    images = {pose: Image.open(folder / "candidates" / f"{key}_{pose}_wide.png").convert("RGBA")
              for pose, _ in LABELS if (folder / "candidates" / f"{key}_{pose}_wide.png").is_file()}
    if key == "goblin_raider":
        places = raider_places(images)
    boxes = {pose: image.getchannel("A").getbbox() for pose, image in images.items()}
    top = min(b[1] for b in boxes.values()) - 12
    bottom = max(b[3] for b in boxes.values()) + 12
    cells = []
    for pose, label in LABELS:
        if pose not in images:
            w = round(300 * Z)
            cell = Image.new("RGB", (w, round((bottom - top) * Z)), (34, 32, 32))
            ImageDraw.Draw(cell).text((10, cell.height // 2 - 12), "그리지 않음", fill=DIM, font=font(18))
            cells.append((cell, label))
            continue
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
    for pose, label in LABELS:
        if pose not in images:
            continue
        hb = places[pose]
        cx, cy = (hb[0] + hb[2]) // 2, (hb[1] + hb[3]) // 2
        crop = images[pose].crop((cx - side // 2, cy - side // 2, cx + side // 2, cy + side // 2))
        cell = Image.new("RGBA", crop.size, STAGE + (255,))
        cell.alpha_composite(crop)
        cell = cell.convert("RGB")
        d = ImageDraw.Draw(cell)
        for y in range(0, cell.height, 30):
            d.line([0, y, cell.width, y], fill=(70, 70, 80), width=1)
        heads.append((cell, label))

    body_h = max(c.height for c, _ in cells)
    width = max(24 + sum(c.width + 16 for c, _ in cells), 24 + sum(c.width + 16 for c, _ in heads))
    out = Image.new("RGB", (width, 48 + body_h + 34 + side + 34), BACK)
    d = ImageDraw.Draw(out)
    d.text((24, 10), title, fill=INK, font=font(26))
    x = 24
    for cell, label in cells:
        out.paste(cell, (x, 48))
        d.text((x + 4, 48 + body_h + 4), label, fill=DIM, font=font(18))
        x += cell.width + 16
    x, y = 24, 48 + body_h + 34
    for cell, label in heads:
        out.paste(cell, (x, y))
        d.text((x + 4, y + side + 4), f"{label}의 머리", fill=DIM, font=font(16))
        x += cell.width + 16
    return out


def main():
    places = json.loads((HERE / "heads.json").read_text(encoding="utf-8"))
    blocks = [block(key, title, places.get(key, {})) for key, title in MONSTERS]
    width = max(b.width for b in blocks)
    sheet = Image.new("RGB", (width, 56 + sum(b.height + 12 for b in blocks)), BACK)
    d = ImageDraw.Draw(sheet)
    d.text((24, 12), "몬스터의 공격·피격 자세 (Round 26·27) — 같은 배율, 파란 줄은 바닥선, 주황 점선은 디딘 뒷발", fill=INK, font=font(28))
    y = 56
    for b in blocks:
        sheet.paste(b, (0, y))
        y += b.height + 12
    sheet.save(HERE / "review-poses.png")
    print(f"리뷰 시트: review-poses.png ({sheet.width}x{sheet.height})")


if __name__ == "__main__":
    main()
