"""Comparison sheet of the battle-ready figure's expression (round 20): now (a big grin), A (serious) and B (slightly
grave), no API call.
  .venv/bin/python ArtPipeline/Archive/20-serious-face/review_face.py

- On the stage: round 19's mockup (Archive/19-motion-test/mock_motion.py: the floor 4 battle with the game's light) with
  the valkyrie's figure swapped, at the size the battle shows it.
- The faces side by side at one scale (each figure fitted the way the game fits it: gen_image.fit_figure), twice as big.
- The whole figures at half the canvas, with the expression each prompt asked for.
The face is framed the same way in all three: the middle between the eye whites, a little toward the nose.
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
sys.path.insert(0, str(ROOT / "ArtPipeline" / "tools"))
sys.path.insert(0, str(ROOT / "ArtPipeline/Archive/19-motion-test"))
from gen_image import FIGURE_CANVAS, FIGURE_FLOOR_MARGIN, FIGURE_SIDE_MARGIN, subject_box  # noqa: E402
from mock_motion import PLACE_H, PLACE_W, Scene  # noqa: E402

OUTPUT = ROOT / "ArtPipeline/output/character"
FONT = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"
OUT = HERE / "review-face.png"
SHEET_W = 2016
INK, DIM, BACK, STAGE = (235, 235, 230), (160, 162, 170), (22, 20, 20), (58, 46, 36)
FIGURES = [
    ("지금 — 큰 웃음 (확정 그림)", ROOT / "Assets/@Art/Unit/Job/valkyrie.png",
     ROOT / "ArtPipeline/Archive/12-roar-style/approved/character/valkyrie.raw.png", "a huge confident grin"),
    ("A — 진지함", OUTPUT / "valkyrie_serious.png", OUTPUT / "valkyrie_serious.raw.png",
     "입을 굳게 다문 일자, 다문 턱, 내리고 조금 모은 눈썹, 가늘게 뜬 차분한 눈"),
    ("B — 약간 심각함", OUTPUT / "valkyrie_grim.png", OUTPUT / "valkyrie_grim.raw.png",
     "처진 입꼬리, 살짝 벌린 입술 뒤로 악문 이, 미간의 깊은 주름, 굳고 경계하는 눈"),
]
STAGE_BOX = (600, 190, 1180, 612)      # Astrid and the goblin raider she faces, on the battle screen
FACE_BOX = 170                          # on the figure canvas


def font(size):
    return ImageFont.truetype(str(FONT), size)


def band(title, height, note=None):
    img = Image.new("RGB", (SHEET_W, height + 52), BACK)
    d = ImageDraw.Draw(img)
    d.text((24, 12), title, fill=INK, font=font(28))
    if note:
        d.text((24 + d.textlength(title, font=font(28)) + 18, 18), note, fill=DIM, font=font(20))
    return img, d


def eye_middle(raw):
    """The middle of the two largest eye whites in the head (the upper third), on the raw image."""
    px = raw.load()
    box = subject_box(raw)
    x0, y0, x1, y1 = box[0], box[1], box[2], box[1] + (box[3] - box[1]) // 3
    seen, blobs = set(), []
    for y in range(y0, y1):
        for x in range(x0, x1):
            if (x, y) in seen:
                continue
            r, g, b, a = px[x, y]
            if a < 200 or min(r, g, b) <= 228:
                continue
            stack, pts = [(x, y)], []
            seen.add((x, y))
            while stack:
                cx, cy = stack.pop()
                pts.append((cx, cy))
                for nx, ny in ((cx + 1, cy), (cx - 1, cy), (cx, cy + 1), (cx, cy - 1)):
                    if x0 <= nx < x1 and y0 <= ny < y1 and (nx, ny) not in seen:
                        q = px[nx, ny]
                        if q[3] >= 200 and min(q[:3]) > 228:
                            seen.add((nx, ny))
                            stack.append((nx, ny))
            if 60 <= len(pts) <= 2000:
                blobs.append((len(pts), sum(p[0] for p in pts) / len(pts), sum(p[1] for p in pts) / len(pts)))
    eyes = sorted(blobs, reverse=True)[:2]
    return sum(e[1] for e in eyes) / len(eyes), sum(e[2] for e in eyes) / len(eyes)


def face_crop(fitted, raw):
    """The face on the fitted figure, framed from the raw's eyes through the game's fit (gen_image.fit_figure)."""
    box = subject_box(raw)
    w, h = box[2] - box[0], box[3] - box[1]
    r = min(round(FIGURE_CANVAS[1] * 0.9) / h, (FIGURE_CANVAS[0] - 2 * FIGURE_SIDE_MARGIN) / w, 1.0)
    x = (FIGURE_CANVAS[0] - round(w * r)) // 2
    y = FIGURE_CANVAS[1] - FIGURE_FLOOR_MARGIN - round(h * r)
    ex, ey = eye_middle(raw)
    cx, cy = x + (ex - box[0]) * r, y + (ey - box[1] + 22) * r
    return fitted.crop((round(cx - FACE_BOX / 2), round(cy - FACE_BOX / 2), round(cx + FACE_BOX / 2), round(cy + FACE_BOX / 2)))


def on_stage(sprite, size):
    cell = Image.new("RGBA", size, STAGE + (255,))
    cell.alpha_composite(sprite, ((size[0] - sprite.width) // 2, size[1] - sprite.height))
    return cell.convert("RGB")


def main():
    scene = Scene()
    figures = [(label, Image.open(fitted).convert("RGBA"), Image.open(raw).convert("RGBA"), note) for label, fitted, raw, note in FIGURES]

    w, h = STAGE_BOX[2] - STAGE_BOX[0], STAGE_BOX[3] - STAGE_BOX[1]
    gap = (SHEET_W - 3 * w) // 4
    stage, d = band("전투 화면 — 게임 표시 크기", h + 40, "4층 보스전 목업(Round 19)의 아스트리드 그림만 바꿨다. 무대 조명·명패·적은 같다")
    for i, (label, fitted, _, _) in enumerate(figures):
        idle = fitted.resize((PLACE_W, PLACE_H), Image.LANCZOS)
        shot = scene.frame(STAGE_BOX, idle=idle)
        x = gap + i * (w + gap)
        stage.paste(shot, (x, 52))
        d.text((x + 8, 52 + h + 6), label, fill=INK, font=font(24))

    size = FACE_BOX * 2
    gap = (SHEET_W - 3 * size) // 4
    faces, d = band("얼굴 — 같은 배율에서 2배 확대", size + 74, "생김새·주근깨·땋은 가닥·눈동자 방향은 그대로, 표정만 바뀌었다")
    for i, (label, fitted, raw, note) in enumerate(figures):
        x = gap + i * (size + gap)
        faces.paste(on_stage(face_crop(fitted, raw).resize((size, size), Image.LANCZOS), (size, size)), (x, 52))
        d.text((x + 8, 52 + size + 6), label, fill=INK, font=font(24))
        d.text((x + 8, 52 + size + 40), note if i else "(지금의 소재: a huge confident grin)", fill=DIM, font=font(17))

    fw, fh = FIGURE_CANVAS[0] // 2, FIGURE_CANVAS[1] // 2
    gap = (SHEET_W - 3 * fw) // 4
    whole, d = band("전신 — 캔버스의 절반", fh + 40, "게임이 맞추는 방식 그대로(키 90 또는 폭에 맞춤). 자세·도끼·옷은 그대로다")
    for i, (label, fitted, _, _) in enumerate(figures):
        x = gap + i * (fw + gap)
        whole.paste(on_stage(fitted.resize((fw, fh), Image.LANCZOS), (fw, fh)), (x, 52))
        d.text((x + 8, 52 + fh + 6), label, fill=INK, font=font(24))

    head = Image.new("RGB", (SHEET_W, 110), BACK)
    d = ImageDraw.Draw(head)
    d.text((24, 16), "전투 준비 자세의 표정 시험 (Round 20) — 지금 / A 진지함 / B 약간 심각함", fill=INK, font=font(36))
    d.text((24, 66), "확정 원본을 기준 그림으로 붙여 표정만 바꿔 그렸다(호출 2회). 다크 판타지 무대에 맞는 얼굴을 고르기 위한 비교다",
           fill=DIM, font=font(22))
    parts = [head, stage, faces, whole]
    sheet = Image.new("RGB", (SHEET_W, sum(p.height for p in parts)), BACK)
    y = 0
    for p in parts:
        sheet.paste(p, (0, y))
        y += p.height
    sheet.save(OUT)
    print("리뷰 시트:", OUT, sheet.size)


if __name__ == "__main__":
    main()
