"""The six jobs before and after the serious face (round 20, after "권장안 반영"), no API call.
  .venv/bin/python ArtPipeline/Archive/20-serious-face/review_six.py

For each job: the face of the figure the game shows now and of the one drawn again with only the expression changed
(STYLE_RUNTIME-face2.md and Rosters/character.csv; the valkyrie is the A of the test), twice as big at one scale, and
the two whole figures as the game fits them. The face is framed from the eyes as in review_face.py.
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
sys.path.insert(0, str(HERE))
from review_face import BACK, DIM, FONT, INK, STAGE, band, face_crop, font, on_stage  # noqa: E402

OUTPUT = ROOT / "ArtPipeline/output/character"
APPROVED = ROOT / "ArtPipeline/Archive/12-roar-style/approved/character"
JOBS = [("knight", "기사", "엄함"), ("valkyrie", "발키리", "결연함"), ("bishop", "주교", "엄숙함"),
        ("paladin", "성기사", "무거움"), ("archmage", "대마법사", "집중"), ("spellblade", "마검사", "차가움")]
OUT = HERE / "review-six.png"
SHEET_W = 2016


def main():
    cell = (SHEET_W - 7 * 16) // 6                  # one job's column
    face = cell // 2 - 4
    fig_w, fig_h = cell // 2 - 4, round((cell // 2 - 4) * 896 / 672)
    rows = []
    head = Image.new("RGB", (SHEET_W, 104), BACK)
    d = ImageDraw.Draw(head)
    d.text((24, 14), "직업 여섯의 진지한 표정 (Round 20) — 왼쪽 지금, 오른쪽 표정만 바꾼 그림", fill=INK, font=font(34))
    d.text((24, 62), "확정 원본을 기준 그림으로 붙여 표정만 바꿨다(발키리는 시험의 A, 나머지 다섯은 호출 5회). 자세·옷·무기·크기는 그대로다. 승인되면 게임에 연결한다",
           fill=DIM, font=font(20))
    rows.append(head)

    faces, d = band("얼굴 — 같은 배율, 2배", face + 40)
    figures, e = band("전신 — 게임이 맞추는 방식 그대로", fig_h + 40)
    for i, (job, name, mood) in enumerate(JOBS):
        x = 16 + i * (cell + 16)
        now = (Image.open(ROOT / f"Assets/@Art/Unit/Job/{job}.png").convert("RGBA"), Image.open(APPROVED / f"{job}.raw.png").convert("RGBA"))
        new = (Image.open(OUTPUT / f"{job}_serious.png").convert("RGBA"), Image.open(OUTPUT / f"{job}_serious.raw.png").convert("RGBA"))
        for k, (fitted, raw) in enumerate((now, new)):
            crop = face_crop(fitted, raw).resize((face, face), Image.LANCZOS)
            faces.paste(on_stage(crop, (face, face)), (x + k * (face + 8), 52))
            figures.paste(on_stage(fitted.resize((fig_w, fig_h), Image.LANCZOS), (fig_w, fig_h)), (x + k * (fig_w + 8), 52))
        d.text((x + 4, 52 + face + 6), f"{name} — {mood}", fill=INK, font=font(21))
        e.text((x + 4, 52 + fig_h + 6), f"{name}: 지금 / 진지함", fill=DIM, font=font(18))
    rows += [faces, figures]
    sheet = Image.new("RGB", (SHEET_W, sum(r.height for r in rows)), BACK)
    y = 0
    for r in rows:
        sheet.paste(r, (0, y))
        y += r.height
    sheet.save(OUT)
    print("리뷰 시트:", OUT, sheet.size)


if __name__ == "__main__":
    main()
