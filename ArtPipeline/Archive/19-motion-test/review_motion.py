"""Review sheet of the attack and hit poses against the approved figure (round 19), no API call.
  .venv/bin/python ArtPipeline/Archive/19-motion-test/fit_motion.py      (first)
  .venv/bin/python ArtPipeline/Archive/19-motion-test/review_motion.py

One sheet: the three at the size the battle shows them and half the canvas, the first attack beside the second (its
axe bent along the bottom; the second reaches below the floor line), the faces side by side at one scale, the mean of
each main color with its distance from the approved figure (before and after fit_motion.py pulls it back), and what
the calls returned as they are.
"""
import math
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
sys.path.insert(0, str(HERE))
from color_drift import MAIN, lab, means  # noqa: E402
from fit_motion import APPROVED, FIGURE_CANVAS, FLOOR, OUTPUT, POSES, WIDE, layout  # noqa: E402

FONT = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"
OUT = HERE / "review-motion.png"
SHEET_W = 2016
INK, DIM, BACK, STAGE = (235, 235, 230), (160, 162, 170), (22, 20, 20), (58, 46, 36)
SCREEN = 300 / FIGURE_CANVAS[1]         # the battle draws the 896 high figure canvas 300 high
# The middle of the face in each raw, measured between the eyes and the nose (README "크기 맞추기").
FACE = {"valkyrie_idle": (522, 235), "valkyrie_attack": (830, 245), "valkyrie_attack2": (805, 215), "valkyrie_hit": (535, 175)}
FACE_BOX = 170                          # on the wide canvas, the same for all three: they are at one scale there
NAMES = [("valkyrie_idle", "지금 (확정 그림)"), ("valkyrie_attack2", "공격 (2차)"), ("valkyrie_hit", "피격")]
ATTACKS = [("valkyrie_attack", "공격 1차 — 판정: 도끼가 바닥에서 꺾였다"), ("valkyrie_attack2", "공격 2차 — 발 아래 여백, 도끼가 바닥선 아래로")]


def font(size):
    return ImageFont.truetype(str(FONT), size)


def wide(name):
    return Image.open(OUTPUT / f"{name}_wide.png").convert("RGBA")


def band(title, height, note=None):
    img = Image.new("RGB", (SHEET_W, height + 52), BACK)
    d = ImageDraw.Draw(img)
    d.text((24, 12), title, fill=INK, font=font(28))
    if note:
        d.text((24 + d.textlength(title, font=font(28)) + 18, 18), note, fill=DIM, font=font(20))
    return img, d


def on_stage(sprite, size):
    cell = Image.new("RGBA", size, STAGE + (255,))
    cell.alpha_composite(sprite, ((size[0] - sprite.width) // 2, size[1] - sprite.height))
    return cell.convert("RGB")


def figures(scale, title, note, names=None):
    """The wide canvases side by side at one scale, with the floor line (the soles) drawn across."""
    names = names or NAMES
    w, h = round(WIDE[0] * scale), round(WIDE[1] * scale)
    gap = (SHEET_W - len(names) * w) // (len(names) + 1)
    img, d = band(title, h + 40, note)
    for i, (name, label) in enumerate(names):
        x = gap + i * (w + gap)
        img.paste(on_stage(wide(name).resize((w, h), Image.LANCZOS), (w, h)), (x, 52))
        d.line([(x, 52 + round(FLOOR * scale)), (x + w, 52 + round(FLOOR * scale))], fill=(200, 80, 70), width=1)
        d.text((x + 8, 52 + h + 6), label, fill=INK, font=font(24))
    return img


def faces(places):
    size = FACE_BOX * 2
    gap = (SHEET_W - 3 * size) // 4
    img, d = band("얼굴 — 같은 배율(게임 캔버스)에서 2배 확대", size + 40, "얼굴의 생김새·주근깨·땋은 가닥·귀·눈동자 방향을 본다. 표정만 자세에 맞춰 바뀌었다")
    for i, (name, label) in enumerate(NAMES):
        raw, ratio, origin, box = places[name]
        cx = origin[0] + (FACE[name][0] - box[0]) * ratio
        cy = origin[1] + (FACE[name][1] - box[1]) * ratio
        crop = wide(name).crop((round(cx - FACE_BOX / 2), round(cy - FACE_BOX / 2), round(cx + FACE_BOX / 2), round(cy + FACE_BOX / 2)))
        x = gap + i * (size + gap)
        img.paste(on_stage(crop.resize((size, size), Image.LANCZOS), (size, size)), (x, 52))
        d.text((x + 8, 52 + size + 6), label, fill=INK, font=font(24))
    return img


def colors():
    approved = means(Image.open(APPROVED).convert("RGBA"))
    columns = [("확정 그림", approved)]
    for name, label in (("valkyrie_attack2", "공격 2차"), ("valkyrie_hit", "피격")):
        columns.append((f"{label} 생성 그대로", means(Image.open(OUTPUT / f"{name}.raw.png").convert("RGBA"))))
        columns.append((f"{label} 쓰는 그림", means(Image.open(OUTPUT / f"{name}.matched.png").convert("RGBA"))))
    row_h, col_w, left = 38, 350, 150
    img, d = band("색 — 부위마다 평균색과 확정 그림과의 차이(ΔE)", row_h * (len(MAIN) + 1) + 20,
                  "ΔE 2 아래는 나란히 놓아도 구별되지 않는다. 색 맞춤(fit_motion.py, 호출 없음)은 가장 큰 차이를 줄일 때만 쓴다")
    for j, (label, _) in enumerate(columns):
        d.text((left + j * col_w, 58), label, fill=INK, font=font(22))
    for i, k in enumerate(MAIN):
        y = 52 + row_h * (i + 1)
        d.text((24, y + 6), k, fill=INK, font=font(22))
        for j, (_, m) in enumerate(columns):
            if k not in m:
                continue
            x = left + j * col_w
            d.rectangle([x, y + 4, x + 70, y + row_h - 6], fill=tuple(round(c) for c in m[k]), outline=(90, 90, 90))
            if j:
                e = math.dist(lab(approved[k]), lab(m[k]))
                d.text((x + 84, y + 8), f"ΔE {e:.1f}", fill=(235, 120, 100) if e >= 2 else DIM, font=font(20))
    return img


def raws():
    h = 260
    img, d = band("생성 원본 — 호출이 돌려준 그대로(흰 바탕)", h + 40, "피격은 확정 원본을, 공격 2차는 그것을 발 아래 여백이 있는 캔버스에 앉힌 그림을 기준 그림으로 붙였다")
    x = 24
    for path, label in ((APPROVED, "확정 원본"), (HERE / "reference-floor.png", "공격 2차의 기준 그림"),
                        (OUTPUT / "valkyrie_attack2.raw.png", "공격 2차 (생성 그대로)"), (OUTPUT / "valkyrie_hit.raw.png", "피격 (생성 그대로)")):
        im = Image.open(path).convert("RGBA")
        im = im.resize((round(im.width * h / im.height), h), Image.LANCZOS)
        white = Image.new("RGBA", im.size, (255, 255, 255, 255))
        white.alpha_composite(im)
        img.paste(white.convert("RGB"), (x, 52))
        d.text((x + 8, 52 + h + 6), label, fill=INK, font=font(24))
        x += im.width + 40
    return img


def main():
    places = layout()
    scale = {name: places[name][1] / places["valkyrie_idle"][1] for name in POSES}
    head = Image.new("RGB", (SHEET_W, 120), BACK)
    d = ImageDraw.Draw(head)
    d.text((24, 18), "발키리 공격·피격 자세 시험 (Round 19) — 확정 그림과 같은 크기·같은 바닥선·같은 색", fill=INK, font=font(36))
    d.text((24, 72), "확정 원본을 기준 그림으로 붙여 자세와 표정만 바꿔 그렸다. 크기는 모양이 변하지 않는 부위로 재서 맞췄다: "
           f"공격 2차 ×{1 / scale['valkyrie_attack2']:.2f}, 피격 ×{1 / scale['valkyrie_hit']:.2f}배로 크게 나온 것을 되돌림. 바닥선은 발바닥", fill=DIM, font=font(22))
    parts = [head,
             figures(SCREEN, "게임 표시 크기", f"전투 화면이 그리는 크기(그림 자리 225x300, 자세는 넓은 캔버스 450x{round(WIDE[1] * SCREEN)}). 붉은 선이 바닥선(발바닥)"),
             figures(0.47, "캔버스의 절반 가까이", "같은 배율이라 머리·몸·부츠의 크기가 같아야 한다. 디딘 뒷발이 같은 자리다"),
             figures(0.6, "공격 1차 → 2차", "1차는 발이 캔버스 바닥에 붙어 도끼가 발 높이에서 접혔다. 2차는 바닥선 아래로 내려간다", ATTACKS),
             faces(places), colors(), raws()]
    sheet = Image.new("RGB", (SHEET_W, sum(p.height for p in parts)), BACK)
    y = 0
    for p in parts:
        sheet.paste(p, (0, y))
        y += p.height
    sheet.save(OUT)
    print("리뷰 시트:", OUT, sheet.size)


if __name__ == "__main__":
    main()
