"""Round 99: Linette's illustration beside intro v01 #8 and Marianne (past). No API call.

Run: .venv/bin/python ArtPipeline/Archive/99-linette/review_sheet.py <name>   (e.g. linette_c -> review_c.jpg)
"""
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
FONT = ROOT / "Assets" / "@Fonts" / "Source" / "Pretendard" / "Pretendard-Medium.ttf"
H, PAD, TOP = 900, 40, 130
BG, PANEL = (24, 22, 22), (44, 40, 40)


def font(size):
    return ImageFont.truetype(str(FONT), size)


def fit(image, height=H):
    ratio = height / image.size[1]
    return image.resize((round(image.size[0] * ratio), height), Image.LANCZOS)


def on_panel(image):
    panel = Image.new("RGB", image.size, PANEL)
    panel.paste(image, (0, 0), image)
    return panel


def main():
    name = sys.argv[1] if len(sys.argv) > 1 else "linette_c"
    figure = Image.open(ROOT / "ArtPipeline" / "output" / "character" / f"{name}.png").convert("RGBA")
    raw = Image.open(ROOT / "ArtPipeline" / "output" / "character" / f"{name}.raw.png").convert("RGBA")
    left, top, right, _ = raw.getbbox()
    width = right - left
    face = raw.crop((left + int(width * 0.15), top, left + int(width * 0.85), top + int(width * 0.75)))
    old = Image.open(ROOT / "ArtPipeline" / "Archive" / "98-intro-storyboard" / "v01" / "scenes" / "b08_market.jpg").convert("RGB")
    w, h = old.size
    old = old.crop((int(w * 0.62), 0, int(w * 0.86), int(h * 0.62)))
    marianne = Image.open(ROOT / "ArtPipeline" / "Archive" / "98-intro-storyboard" / "refs" / "marianne_past.png").convert("RGB")
    panels = [
        ("인트로 01판 #8의 리넷", fit(old)),
        (f"{name} (게임 크기의 캔버스)", on_panel(fit(figure))),
        (f"{name} 얼굴 확대", on_panel(fit(face))),
        ("마리안(과거) #02 확정", fit(marianne)),
    ]
    width = sum(p.size[0] for _, p in panels) + PAD * (len(panels) + 1)
    sheet = Image.new("RGB", (width, TOP + H + PAD), BG)
    draw = ImageDraw.Draw(sheet)
    draw.text((PAD, 24), f"Round 99 · 리넷 일러스트 ({name})", font=font(34), fill=(238, 232, 220))
    x = PAD
    for label, panel in panels:
        draw.text((x, TOP - 40), label, font=font(24), fill=(214, 190, 130))
        sheet.paste(panel, (x, TOP))
        x += panel.size[0] + PAD
    out = HERE / f"review_{name.split('_')[-1]}.jpg"
    sheet.save(out, quality=90)
    print(out.name, sheet.size)


if __name__ == "__main__":
    main()
