"""Round 70 / stage 23, the implementation beside the chosen mockups (no API call): crops of the game's screenshots next to the matching
panel of mock-forge.png and mock-gladius-stars.png, so the elements can be checked one by one (the third card, the forge step's plate,
the gladius piece's rim and pips, the plate-head card).
  .venv/bin/python ArtPipeline/Archive/70-unique-gladius/impl_compare.py <screenshot dir>
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
TEXT = (0xEB, 0xEB, 0xE6); BG = (0x1A, 0x18, 0x17)

CAMP = (1150, 300, 1730, 620)
# (label, screenshot, crop box, scale, mock file, mock crop box)
SCENES = [
    ("야영지 창: 카드 셋 (ko_52)  |  목업 A", "ko_52_camp_forge.png", CAMP, 1, "mock-forge.png", (12, 386, 410, 606)),
    ("담금질 걸음 (ko_53)  |  목업 C", "ko_53_camp_forge_step.png", CAMP, 1, "mock-forge.png", (826, 386, 1226, 606)),
    ("유니크의 카드와 조각 (ko_54)  |  목업 3 명패 머리 (금 테는 후속 2에서 금빛으로)", "ko_54_map_unique_card.png", (0, 300, 960, 810), 1, "mock-forge.png", (440, 648, 870, 830)),
    # Follow-up 2: the glow round the gladius's outline (2px, no breathing) beside the 2px panel of mock-outline-glow.png, both at 2x.
    ("유니크의 조각: 금빛 외곽선 2px, 숨쉬기 없음, 눈금 없음 (ko_54, 2배)  |  목업 2px (후속 3 전: 눈금 셋)", "ko_54_map_unique_card.png", (690, 625, 870, 805), 2, "mock-outline-glow.png", (344, 26, 652, 334)),
    # The unique in battle (ko_55, a second and a half in: half lit) beside the mock's battle board at 2px, both at 2x.
    ("전투의 유니크: 충전 중에도 온전한 금빛 (ko_55, 2배)  |  목업 2px 전투 보드", "ko_55_battle_unique.png", (690, 625, 870, 805), 2, "mock-outline-glow.png", (344, 360, 498, 514)),
]


def font(px):
    return ImageFont.truetype(str(FONT), px)


def main(shots):
    shots = Path(shots)
    GAME.mkdir(exist_ok=True)
    pad = 12
    W = 1300
    rows = []
    for label, name, box, scale, mock_name, mock_box in SCENES:
        src = shots / name
        if not src.exists():
            print("missing", src); continue
        shutil.copy(src, GAME / ("impl_" + name))
        img = Image.open(src).convert("RGB").crop(box)
        if scale != 1:
            img = img.resize((img.width * scale, img.height * scale), Image.LANCZOS)
        mock = Image.open(HERE / mock_name).convert("RGB").crop(mock_box)
        h = max(img.height, mock.height)
        row = Image.new("RGB", (img.width + mock.width + 20, h), BG)
        row.paste(img, (0, 0)); row.paste(mock, (img.width + 20, 0))
        if row.width > W - 2 * pad:
            r = (W - 2 * pad) / row.width
            row = row.resize((int(row.width * r), int(row.height * r)), Image.LANCZOS)
        rows.append((label, row))
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
