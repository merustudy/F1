"""The review sheets of round 40's state poses, no API call.
  .venv/bin/python ArtPipeline/Archive/40-state-poses/review_states.py [--jobs valkyrie,knight,...] [--raw]
review-states.png: a row per mercenary, the approved figure, the broken pose and the resolute pose on the pose canvas at one
scale (half the canvas, about 1.4 times the battle's), standing on the floor line, as tools/review_pose.py draws attack and hit.
The valkyrie's approved pair (round 38) is the first row, the measure the five are judged against.
review-faces.png: the heads of the same three pictures at twice that scale, for the user's ask of this round: the same face and
head size in every state. --raw lays the raw poses at the reference's scale instead of the fitted ones (before fitting).
"""
import argparse
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
sys.path.insert(0, str(ROOT / "ArtPipeline" / "tools"))
from fit_pose import FLOOR, OUTPUT, WIDE, idle_wide  # noqa: E402
from gen_image import subject_box  # noqa: E402

FONT = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"
INK, DIM, BACK, STAGE, LINE = (235, 235, 230), (160, 162, 170), (22, 20, 20), (58, 46, 36), (70, 140, 220)
NAMES = {"valkyrie": "발키리 (Round 38 확정)", "knight": "기사", "bishop": "주교", "paladin": "성기사", "archmage": "대마법사", "spellblade": "마검사"}
LABELS = ("대기 (확정 그림)", "붕괴 · broken", "각성 · resolute")


def font(size):
    return ImageFont.truetype(str(FONT), size)


def pictures(job, raw):
    """The three pictures on the pose canvas (WIDE)."""
    out = [idle_wide(job)]
    for state in ("broken", "resolute"):
        if raw:
            image = Image.open(OUTPUT / "hit" / f"{job}_{state}.raw.png").convert("RGBA")
            box = subject_box(image)
            subject = image.crop(box)
            # The raw is drawn on the reference's 1536x1024 canvas whose soles are at 768 of 1024 and figure 700 tall: laid on
            # the pose canvas at the ratio of the floors, roughly, which is enough to look at before fitting.
            k = (FLOOR / 768) * (WIDE[1] / 1024) * 0.95
            subject = subject.resize((round(subject.width * k), round(subject.height * k)), Image.LANCZOS)
            canvas = Image.new("RGBA", WIDE, (0, 0, 0, 0))
            canvas.paste(subject, ((WIDE[0] - subject.width) // 2, FLOOR - round((box[3] - box[1]) * k) + round((box[3] - 768) * k)))
            out.append(canvas)
        else:
            out.append(Image.open(OUTPUT / "hit" / f"{job}_{state}.png").convert("RGBA").resize(WIDE, Image.LANCZOS))
    return out


def row(job, raw, z=0.5):
    images = pictures(job, raw)
    boxes = [image.getchannel("A").getbbox() for image in images]
    top = min(b[1] for b in boxes) - 12
    bottom = max(b[3] for b in boxes) + 12
    cells = []
    for image, box in zip(images, boxes):
        crop = image.crop((box[0] - 12, top, box[2] + 12, bottom))
        cell = Image.new("RGBA", crop.size, STAGE + (255,))
        cell.alpha_composite(crop)
        cell = cell.convert("RGB").resize((round(crop.width * z), round(crop.height * z)), Image.LANCZOS)
        ImageDraw.Draw(cell).line([0, round((FLOOR - top) * z), cell.width, round((FLOOR - top) * z)], fill=LINE, width=1)
        cells.append(cell)
    height = max(c.height for c in cells)
    out = Image.new("RGB", (24 + sum(c.width + 16 for c in cells), height + 84), BACK)
    d = ImageDraw.Draw(out)
    d.text((24, 10), f"{NAMES.get(job, job)} · {job}", fill=INK, font=font(28))
    x = 24
    for cell, label in zip(cells, LABELS):
        out.paste(cell, (x, 52))
        d.text((x + 4, 52 + cell.height + 4), label, fill=DIM, font=font(20))
        x += cell.width + 16
    return out


def heads(job, raw, z=1.0, share=0.34):
    """The top part of each of the three pictures, at the same scale: the heads side by side."""
    images = pictures(job, raw)
    cells = []
    for image in images:
        box = image.getchannel("A").getbbox()
        tall = max(b[3] for b in [image.getchannel("A").getbbox() for image in images]) - min(b[1] for b in [image.getchannel("A").getbbox() for image in images])
        crop = image.crop((box[0] - 8, box[1] - 8, box[2] + 8, box[1] + round((box[3] - box[1]) * share)))
        cell = Image.new("RGBA", crop.size, STAGE + (255,))
        cell.alpha_composite(crop)
        cells.append(cell.convert("RGB").resize((round(crop.width * z), round(crop.height * z)), Image.LANCZOS))
    height = max(c.height for c in cells)
    out = Image.new("RGB", (24 + sum(c.width + 16 for c in cells), height + 64), BACK)
    d = ImageDraw.Draw(out)
    d.text((24, 8), f"{NAMES.get(job, job)} · {job}", fill=INK, font=font(26))
    x = 24
    for cell, label in zip(cells, LABELS):
        out.paste(cell, (x, 44))
        x += cell.width + 16
    return out


def stack(rows, title):
    width = max(r.width for r in rows)
    sheet = Image.new("RGB", (width, 48 + sum(r.height for r in rows)), BACK)
    ImageDraw.Draw(sheet).text((24, 12), title, fill=INK, font=font(26))
    y = 48
    for r in rows:
        sheet.paste(r, (0, y))
        y += r.height
    return sheet


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--jobs", default="valkyrie,knight,bishop,paladin,archmage,spellblade")
    parser.add_argument("--raw", action="store_true")
    parser.add_argument("--suffix", default="")
    args = parser.parse_args()
    jobs = [j for j in args.jobs.split(",") if (OUTPUT / "hit" / f"{j}_broken{'.raw' if args.raw else ''}.png").is_file()
            and (OUTPUT / "hit" / f"{j}_resolute{'.raw' if args.raw else ''}.png").is_file()]
    fitted = "원본을 기준 그림의 배율로 대충 세운 것" if args.raw else "tools/fit_pose.py로 확정 원본의 크기에 맞춘 것"
    sheet = stack([row(j, args.raw) for j in jobs], f"Round 40 — 붕괴 자세·각성 자세 ({fitted}). 왼쪽: 확정 원본. 모두 게임 크기의 1.4배")
    sheet.save(HERE / f"review-states{args.suffix}.png")
    faces = stack([heads(j, args.raw) for j in jobs], "Round 40 — 머리와 얼굴 (세 그림을 같은 배율로: 얼굴과 머리 크기가 같아야 한다). 게임 크기의 2.8배")
    faces.save(HERE / f"review-faces{args.suffix}.png")
    print(f"review-states{args.suffix}.png {sheet.size}, review-faces{args.suffix}.png {faces.size}, jobs {jobs}")


if __name__ == "__main__":
    main()
