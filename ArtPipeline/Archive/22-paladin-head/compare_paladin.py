"""The paladin before and after his head was drawn bigger (round 22), among the other mercenaries. No API call.
  .venv/bin/python ArtPipeline/Archive/22-paladin-head/compare_paladin.py      # compare-paladin.png

- The six at the size of their place in battle (225x300), once with the paladin the game shows now and once with the
  one drawn again.
- Every head at one scale, lined up on the near eye, with the band the chin has to fall in (review_sheet.FACE_BAND).
- The two paladins whole, at half the canvas.
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
sys.path.insert(0, str(ROOT / "ArtPipeline" / "tools"))
from review_sheet import HEAD_WINDOW, face_band, genders, near_eye  # noqa: E402

GAME = ROOT / "Assets/@Art/Unit/Job"
NEW = ROOT / "ArtPipeline/output/character/paladin_head.png"
FONT = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"
OUT = HERE / "compare-paladin.png"
JOBS = [("knight", "로언 · 기사"), ("valkyrie", "아스트리드 · 발키리"), ("bishop", "엘라 · 주교"),
        ("paladin", "세드릭 · 성기사"), ("archmage", "미라 · 대마법사"), ("spellblade", "카이 · 마검사")]
INK, DIM, BACK, STAGE = (235, 235, 230), (160, 162, 170), (22, 20, 20), (58, 46, 36)
PLACE = (225, 300)
ZOOM = 1.5


def font(size):
    return ImageFont.truetype(str(FONT), size)


def band(width, height, title, note=None):
    img = Image.new("RGB", (width, height + 52), BACK)
    d = ImageDraw.Draw(img, "RGBA")
    d.text((24, 12), title, fill=INK, font=font(28))
    if note:
        d.text((24 + d.textlength(title, font=font(28)) + 18, 18), note, fill=DIM, font=font(20))
    return img, d


def on_stage(sprite, size):
    cell = Image.new("RGBA", size, STAGE + (255,))
    cell.alpha_composite(sprite, ((size[0] - sprite.width) // 2, size[1] - sprite.height))
    return cell.convert("RGB")


def head(image, band_of):
    width, above, below = HEAD_WINDOW
    eye = near_eye(image)
    crop = image.crop((eye[0] - width // 2, eye[1] - above, eye[0] + width // 2, eye[1] + below))
    cell = Image.new("RGBA", crop.size, STAGE + (255,))
    cell.alpha_composite(crop)
    cell = cell.convert("RGB").resize((round(width * ZOOM), round((above + below) * ZOOM)), Image.LANCZOS)
    d = ImageDraw.Draw(cell, "RGBA")
    y0, y1 = round((above + band_of[0]) * ZOOM), round((above + band_of[1]) * ZOOM)
    d.rectangle([0, y0, cell.width - 1, y1], fill=(90, 200, 110, 70), outline=(90, 200, 110, 200))
    d.line([0, round(above * ZOOM), cell.width - 1, round(above * ZOOM)], fill=(110, 170, 230, 220), width=1)
    return cell


def main():
    figures = {key: Image.open(GAME / f"{key}.png").convert("RGBA") for key, _ in JOBS}
    new = Image.open(NEW).convert("RGBA")
    gap = 12
    width = 24 * 2 + 7 * (round(HEAD_WINDOW[0] * ZOOM) + gap)

    rows = []
    head_img = Image.new("RGB", (width, 104), BACK)
    d = ImageDraw.Draw(head_img)
    d.text((24, 14), "성기사의 머리 크기 (Round 22) — 지금 / 다시 그림", fill=INK, font=font(34))
    d.text((24, 62), "확정 원본을 기준 그림으로 붙여 얼굴·수염·갑옷·철퇴·방패·자세를 그대로 두고 비율만 바꿨다(약 4등신, 호출 1회). 승인되면 게임에 연결한다",
           fill=DIM, font=font(20))
    rows.append(head_img)

    for title, paladin in (("지금 (게임)", figures["paladin"]), ("다시 그림", new)):
        lineup, d = band(width, PLACE[1] + 40, f"게임 크기 — {title}", "그림 자리 225x300, 같은 캔버스와 바닥선")
        for i, (key, name) in enumerate(JOBS):
            image = paladin if key == "paladin" else figures[key]
            x = 24 + i * (PLACE[0] + 40)
            lineup.paste(on_stage(image.resize(PLACE, Image.LANCZOS), PLACE), (x, 52))
            d.text((x + 4, 52 + PLACE[1] + 6), name + (" ← " + title if key == "paladin" else ""), fill=INK if key == "paladin" else DIM, font=font(20))
        rows.append(lineup)

    cell_h = round(sum(HEAD_WINDOW[1:]) * ZOOM)
    heads, d = band(width, cell_h + 40, "머리 — 같은 배율, 눈높이 맞춤",
                    "턱(수염이 있으면 수염 끝)이 초록 띠(캔버스에서 눈 아래 남성 65~85px, 여성 48~62px) 안에 들어야 한다")
    known = genders()
    cells = [(name, key, figures[key]) for key, name in JOBS if key != "paladin"] + [("성기사 — 지금 (41px)", "paladin", figures["paladin"]), ("성기사 — 다시 그림 (81px)", "paladin", new)]
    for i, (name, key, image) in enumerate(cells):
        x = 24 + i * (round(HEAD_WINDOW[0] * ZOOM) + gap)
        heads.paste(head(image, face_band(key, known)), (x, 52))
        d.text((x + 4, 52 + cell_h + 6), name, fill=INK if "성기사" in name else DIM, font=font(18))
    rows.append(heads)

    fw, fh = 336, 448
    whole, d = band(width, fh + 40, "전신 — 캔버스의 절반", "다시 그린 쪽은 머리가 커지고 몸통·다리가 짧아져, 폭에 맞춰 줄어 키는 693 → 665px(맞추기가 재는 키)")
    for i, (title, image) in enumerate((("지금", figures["paladin"]), ("다시 그림", new))):
        x = 24 + i * (fw + 40)
        whole.paste(on_stage(image.resize((fw, fh), Image.LANCZOS), (fw, fh)), (x, 52))
        d.text((x + 4, 52 + fh + 6), title, fill=INK, font=font(22))
    rows.append(whole)

    sheet = Image.new("RGB", (width, sum(r.height for r in rows)), BACK)
    y = 0
    for r in rows:
        sheet.paste(r, (0, y))
        y += r.height
    sheet.save(OUT)
    print("비교 시트:", OUT, sheet.size)


if __name__ == "__main__":
    main()
