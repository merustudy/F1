# -*- coding: utf-8 -*-
"""Round 68: the archmage's new figure (6th try) and its four redrawn poses (attack, hit, broken, resolute) next to the ones they
replace, at one scale. Each picture is its pose canvas (2016x1008, the approved figure as fit_pose.idle_wide stands it, a pose's
fitted file stood back from 2048x1024), so the sizes are the game's own. The second sheet puts the heads side by side on a grid
(a 25 px line), centred on the face, to see that a pose keeps the figure's head size (Docs/Architecture/13 "후처리 (pose)").
No API call.
  .venv/bin/python ArtPipeline/Archive/68-archmage-new-face/review_poses.py
"""
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
sys.path.insert(0, str(ROOT / "ArtPipeline" / "tools"))
import fit_pose as F  # noqa: E402

OUT = ROOT / "ArtPipeline" / "output"
BEFORE = OUT / "_before-r68-archmage"
FONT = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"
PAPER = (40, 36, 34)
TEXT = (232, 224, 208)
POSES = [("대기", None), ("공격", "attack/archmage.png"), ("피격", "hit/archmage.png"),
         ("붕괴", "hit/archmage_broken.png"), ("각성", "hit/archmage_resolute.png")]
# The face's place on the 2016x1008 canvas, read off the pictures (x, y of the face's centre).
FACES_NEW = {"대기": (981, 298), "공격": (1095, 400), "피격": (856, 293), "붕괴": (939, 336), "각성": (1017, 293)}
FACES_OLD = {"대기": (983, 296), "공격": (1064, 295), "피격": (776, 124), "붕괴": (992, 176), "각성": (1016, 156)}


def font(px):
    return ImageFont.truetype(str(FONT), px)


def canvas(root, rel):
    if rel is None:
        return None
    return Image.open(root / rel).convert("RGBA").resize(F.WIDE if hasattr(F, "WIDE") else (2016, 1008), Image.LANCZOS)


def idle(root):
    """The approved figure on the pose canvas, as the game stands it (fit_pose.idle_wide reads output/character)."""
    if root == OUT:
        return F.idle_wide("archmage")
    original = F.OUTPUT
    F.OUTPUT = BEFORE
    try:
        return F.idle_wide("archmage")
    finally:
        F.OUTPUT = original


def frames(root):
    return [(label, idle(root) if rel is None else canvas(root, rel)) for label, rel in POSES]


def sheet_rows(rows, crop=(380, 80, 1640, 1008), scale=0.42):
    w = int((crop[2] - crop[0]) * scale)
    h = int((crop[3] - crop[1]) * scale)
    pad, label_w = 16, 150
    out = Image.new("RGB", (label_w + len(POSES) * (w + pad) + pad, 70 + len(rows) * (h + 50)), PAPER)
    d = ImageDraw.Draw(out)
    d.text((pad, 20), "대마법사: 지금(위) / Round 68 6차로 다시 그린 것(아래). 자세 캔버스 같은 배율 — 게임에서의 크기와 자리", font=font(22), fill=TEXT)
    y = 70
    for row_label, items in rows:
        d.text((pad, y + h // 2), row_label, font=font(22), fill=TEXT)
        x = label_w
        for label, img in items:
            d.text((x, y), label, font=font(18), fill=TEXT)
            tile = Image.new("RGBA", (crop[2] - crop[0], crop[3] - crop[1]), (70, 62, 56, 255))
            tile.alpha_composite(img.crop(crop))
            # the floor line of the pose canvas (112 px above its bottom)
            ImageDraw.Draw(tile).line([(0, 1008 - 112 - crop[1]), (tile.width, 1008 - 112 - crop[1])], fill=(120, 160, 120, 255), width=2)
            out.paste(tile.resize((w, h), Image.LANCZOS).convert("RGB"), (x, y + 26))
            x += w + pad
        y += h + 50
    return out


def heads(rows, half=130, zoom=1.0):
    side = half * 2
    pad, label_w = 16, 150
    out = Image.new("RGB", (label_w + len(POSES) * (side + pad) + pad, 70 + len(rows) * (side + 50)), PAPER)
    d = ImageDraw.Draw(out)
    d.text((pad, 20), "머리: 얼굴을 가운데에, 같은 배율, 25px 격자", font=font(22), fill=TEXT)
    y = 70
    for row_label, items, faces in rows:
        d.text((pad, y + half), row_label, font=font(22), fill=TEXT)
        x = label_w
        for label, img in items:
            cx, cy = faces[label]
            tile = Image.new("RGBA", (side, side), (70, 62, 56, 255))
            tile.alpha_composite(img.crop((cx - half, cy - half, cx + half, cy + half)))
            g = ImageDraw.Draw(tile)
            for k in range(0, side, 25):
                g.line([(k, 0), (k, side)], fill=(255, 255, 255, 40))
                g.line([(0, k), (side, k)], fill=(255, 255, 255, 40))
            d.text((x, y), label, font=font(18), fill=TEXT)
            out.paste(tile.convert("RGB"), (x, y + 26))
            x += side + pad
        y += side + 50
    return out


def main():
    old, new = frames(BEFORE), frames(OUT)
    sheet_rows([("지금", old), ("6차", new)]).save(HERE / "review-poses.png")
    heads([("지금", old, FACES_OLD), ("6차", new, FACES_NEW)]).save(HERE / "review-heads.png")
    print(HERE / "review-poses.png", HERE / "review-heads.png")


if __name__ == "__main__":
    main()
