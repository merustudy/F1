"""The three graves (round 21) at the size the battle would show them and twice as big, no API call.
  .venv/bin/python ArtPipeline/Archive/21-grave/review_graves.py
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
FONT = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"
GRAVES = [("grave_wood", "A 나무 십자가", "밧줄로 묶은 두 판자, 흙 둔덕"), ("grave_stone", "B 돌 묘비 (권장)", "십자가를 새긴 둥근 돌, 흙 둔덕"),
          ("grave_cairn", "C 돌무더기 십자가", "돌무더기에 꽂은 나무 십자가")]
BACK, STAGE, INK, DIM = (22, 20, 20), (58, 46, 36), (235, 235, 230), (160, 162, 170)


def main():
    f, g = ImageFont.truetype(str(FONT), 26), ImageFont.truetype(str(FONT), 19)
    w = 640
    sheet = Image.new("RGB", (3 * w + 4 * 16, 52 + 600 + 90), BACK)
    d = ImageDraw.Draw(sheet)
    d.text((16, 12), "쓰러진 용병의 무덤 (Round 21) — 게임 크기(왼쪽 아래)와 2배", fill=INK, font=f)
    for i, (key, name, note) in enumerate(GRAVES):
        x = 16 + i * (w + 16)
        cell = Image.new("RGBA", (w, 600), STAGE + (255,))
        art = Image.open(ROOT / f"ArtPipeline/output/prop/{key}.png").convert("RGBA")
        big = art.resize((450, 600), Image.LANCZOS)
        small = art.resize((225, 300), Image.LANCZOS)
        cell.alpha_composite(big, (w - 450, 0))
        cell.alpha_composite(small, (0, 300))
        sheet.paste(cell.convert("RGB"), (x, 52))
        d.text((x + 4, 52 + 600 + 8), name, fill=INK, font=f)
        d.text((x + 4, 52 + 600 + 46), note, fill=DIM, font=g)
    sheet.save(HERE / "review-graves.png")
    print("리뷰 시트:", HERE / "review-graves.png", sheet.size)


if __name__ == "__main__":
    main()
