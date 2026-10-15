"""Round 69, the implementation beside the chosen mockup (no API call): crops of the game's screenshots next to the matching part of
mock-chosen.png, so the elements can be checked one by one (the pips' size, edge and colours, the words' colours, the titles).
  .venv/bin/python ArtPipeline/Archive/69-tier-naming/impl_compare.py <screenshot dir>
Writes game/impl-compare.png and copies the scenes it used into game/.
"""
import shutil
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
FONT = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"
GAME = HERE / "game"
TEXT = (0xEB, 0xEB, 0xE6); DIM = (0x9A, 0xA0, 0xAC); BG = (0x1A, 0x18, 0x17)

# (label, screenshot, crop box, scale)
SCENES = [
    ("노드 맵의 보드: 1성 ◆ · 2성 ◆◆ · 3성 ◆◆◆, 합치기 → 2성 (ko_35)", "ko_35_map_tiers.png", (500, 620, 960, 810), 2),
    ("야영지의 정비 창: 일반 → 1성 (ko_34)", "ko_34_camp_mend.png", (1150, 300, 1730, 620), 1),
    ("설명 줄의 합치기 안내 (ko_08)", "ko_08_map_item_selected.png", (975, 850, 1900, 1000), 1),
    ("전투 보드의 눈금 (ko_09: 뒤 열의 1성 무기)", "ko_09_boss_battle.png", (500, 620, 960, 810), 2),
    ("카드의 제목 (ko_47)", "ko_47_map_star_card.png", (0, 90, 960, 640), 1),
]


def font(px):
    return ImageFont.truetype(str(FONT), px)


def main(shots):
    shots = Path(shots)
    GAME.mkdir(exist_ok=True)
    mock = Image.open(HERE / "mock-chosen.png").convert("RGB")
    pad = 12
    W = max(984, 2 * 460 + 3 * pad)
    rows = [("확정 목업 (mock-chosen.png)", mock)]
    for label, name, box, scale in SCENES:
        src = shots / name
        if not src.exists():
            print("missing", src); continue
        shutil.copy(src, GAME / ("impl_" + name))
        img = Image.open(src).convert("RGB").crop(box)
        if scale != 1:
            img = img.resize((img.width * scale, img.height * scale), Image.LANCZOS)
        if img.width > W - 2 * pad:
            r = (W - 2 * pad) / img.width
            img = img.resize((int(img.width * r), int(img.height * r)), Image.LANCZOS)
        rows.append((label, img))
    H = pad + sum(22 + img.height + 10 for _, img in rows)
    sheet = Image.new("RGB", (W, H), BG)
    d = ImageDraw.Draw(sheet)
    y = pad
    for label, img in rows:
        d.text((pad, y), label, font=font(14), fill=TEXT); y += 22
        sheet.paste(img, (pad, y)); y += img.height + 10
    sheet.save(GAME / "impl-compare.png")
    print("wrote", GAME / "impl-compare.png")


if __name__ == "__main__":
    main(sys.argv[1])
