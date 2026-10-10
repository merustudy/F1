# -*- coding: utf-8 -*-
"""Round 51, the rat claw (the user, 2026-10-10: "쥐 발톱 그림 새로 생성"): the candidate against today's fang, trimmed to what
is drawn (tools/trim_items.py) and laid in a 2x1 piece as the node map shows it (the blue piece) and as the battle shows it (no
blue), at the screen's size and twice. No API call (the candidate is ArtPipeline/output/item/rat_claw.png).
  .venv/bin/python ArtPipeline/Archive/51-claw-axe-whetstone/review_claw.py
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
sys.path.insert(0, str(ROOT / "ArtPipeline/tools"))
sys.path.insert(0, str(HERE.parent / "49-inventory-style"))
import trim_items  # noqa: E402
import mock_inventory_style as S  # noqa: E402
import mock_diablo_v2 as V2  # noqa: E402
from mock_inventory_style import s, span, PAD, comp  # noqa: E402

LOOK = V2.DiabloV2()
FANG = ROOT / "Assets/@Art/Item/rat_bite.png"
CLAW = ROOT / "ArtPipeline/output/item/rat_claw.png"


def piece(art, battle):
    """A 2x1 piece with the icon fitted inside the margin, as ItemSlotView does (preserve aspect, 5 inside)."""
    S._icons[("x", False, False)] = art
    under, icon, over = LOOK.parts(dict(id="x", x=0, y=0, w=2, h=1, tier="common"), battle=battle)
    lay = Image.new("RGBA", under.size, (0, 0, 0, 0)); lay.alpha_composite(under)
    W, H = span(2), span(1); ic = S.fit(art, s(W - 10), s(H - 10))
    lay.alpha_composite(ic, (s(PAD) + (s(W) - ic.width) // 2, s(PAD) + (s(H) - ic.height) // 2))
    return lay


def tile(art, battle):
    img = Image.new("RGBA", (s(150), s(90)), (40, 39, 37, 255))
    X, Y = 22, 20
    LOOK.well(img, X, Y, 2, 1); LOOK.squares(img, X, Y, 2, 1)
    comp(img, piece(art, battle), (s(X - PAD), s(Y - PAD)))
    return img.resize((150, 90), Image.LANCZOS)


def main():
    rows = [("송곳니 (지금)", trim_items.trimmed(Image.open(FANG).convert("RGBA"))),
            ("발톱 (후보)", trim_items.trimmed(Image.open(CLAW).convert("RGBA")))]
    W, H = 150, 90; f = S.ImageFont.truetype(str(S.PRET), 18) if hasattr(S, "ImageFont") else None
    from PIL import ImageFont
    f = ImageFont.truetype(str(S.PRET), 18)
    out = Image.new("RGB", (40 + 160 + 4 * (W * 2 + 20), 60 + len(rows) * (H * 2 + 20) + 40), (18, 18, 22))
    d = ImageDraw.Draw(out)
    for k, t in enumerate(["노드 맵 (화면 크기)", "전투 (화면 크기)", "노드 맵 (2배)", "전투 (2배)"]):
        d.text((200 + k * (W * 2 + 20), 20), t, font=f, fill=(236, 232, 222))
    for r, (name, art) in enumerate(rows):
        y = 60 + r * (H * 2 + 20)
        d.text((20, y + H - 10), name, font=f, fill=(236, 214, 150) if r else (236, 232, 222))
        for k, (battle, scale) in enumerate([(False, 1), (True, 1), (False, 2), (True, 2)]):
            im = tile(art, battle)
            if scale == 2: im = im.resize((W * 2, H * 2), Image.LANCZOS)
            out.paste(im, (200 + k * (W * 2 + 20), y + (H * 2 - im.height) // 2))
    d.text((20, out.height - 32), "2×1 조각(칸 50·사이 2, 여백 5), 아이콘은 그려진 부분으로 자른 뒤 비율을 지켜 맞춤. 호출 1회($0.021).", font=f, fill=(170, 170, 178))
    out.save(HERE / "review-claw.png"); print("review-claw.png", out.size)


if __name__ == "__main__":
    main()
