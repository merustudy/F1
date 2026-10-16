"""Round 70, follow-up 2 (user: no rim; the icon's outline itself glows gold and breathes): the gladius with a soft gold glow that follows its
silhouette — made offline from the icon's alpha (dilated and blurred, tinted the unique gold) — on the node map at 2x and in battle at 1x.
First sheet (narrow 4px / wide 7px beside the rim), then the user's "2px and 3px only": widths are the dilation on screen at 1x (the icon
is shown at 0.435 of its pixels: 144x40 in a 3x1 piece), the blur adds about half as much again. No API call.
Then "no breathing, 1px": the glow is still, and the sheet shows 1px with 2px and 3px for reference (the movie is not made any more).
  mock-outline-glow.png    the still comparison: 1px / 2px / 3px
  mock-outline-glow.mp4    (earlier) 2px and 3px side by side, breathing — `movie()`, not run since the breathing was dropped
  .venv/bin/python ArtPipeline/Archive/70-unique-gladius/mock_outline_glow.py
"""
import math
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter, ImageFont

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent / "69-tier-naming"))
import mock_tier_names as m  # noqa: E402
import mock_forge as f  # noqa: E402

ROOT = HERE.parents[2]
ICON = ROOT / "Assets/@Art/Item/gladius.png"
UNIQUE = f.UNIQUE
GLOW_LIGHT = (0xF2, 0xE2, 0xA6)


def font(px):
    return ImageFont.truetype(str(m.FONT), int(round(px)))


def outline_glow(icon, spread, blur):
    """A glow sprite from the icon's alpha: dilated by `spread` px and blurred by `blur` px (icon pixels), white; tinted later."""
    a = icon.split()[3]
    pad = spread + blur * 3
    big = Image.new("L", (icon.width + 2 * pad, icon.height + 2 * pad), 0)
    big.paste(a, (pad, pad))
    big = big.filter(ImageFilter.MaxFilter(2 * spread + 1)).filter(ImageFilter.GaussianBlur(blur))
    g = Image.new("RGBA", big.size, (255, 255, 255, 0)); g.putalpha(big)
    return g, pad


def lay_icon_and_glow(b, name, x, y, w, h, glow_spec, strength, battle_charge=None):
    """Draws the glow (if any) and then the icon in the piece's art box, the way the view would: glow behind, tinted gold at `strength`."""
    sq = b.square(x, y, w, h); k = b.k
    art = Image.open(ICON).convert("RGBA")
    box_w = (sq[2] - sq[0] - 2 * m.MARGIN) * k; box_h = (sq[3] - sq[1] - 2 * m.MARGIN) * k
    s = min(box_w / art.width, box_h / art.height)
    aw, ah = max(1, int(art.width * s)), max(1, int(art.height * s))
    ax = int((sq[0] + sq[2]) / 2 * k - aw / 2); ay = int((sq[1] + sq[3]) / 2 * k - ah / 2)
    if glow_spec:
        spread, blur = glow_spec
        g, pad = outline_glow(art, spread, blur)
        gw, gh = int(g.width * s), int(g.height * s)
        g = g.resize((gw, gh), Image.LANCZOS)
        r, gg, bb, a = g.split()
        tint = Image.new("RGBA", g.size, UNIQUE + (0,))
        # a lighter core near the edge: blend the light tone where the alpha is strong
        core = Image.new("RGBA", g.size, GLOW_LIGHT + (0,)); core.putalpha(a.point(lambda v: int(min(255, v * 1.6) * 0.55)))
        tint.putalpha(a.point(lambda v: int(v * strength)))
        tint.alpha_composite(core)
        layer = Image.new("RGBA", b.img.size, (0, 0, 0, 0))
        layer.alpha_composite(tint, (ax - int(pad * s), ay - int(pad * s)))
        b.img.alpha_composite(layer)
    if battle_charge is not None:
        dark = art.copy(); px = dark.load()
        for yy in range(dark.height):
            for xx in range(dark.width):
                r_, g_, b_, a_ = px[xx, yy]
                if xx > art.width * battle_charge:
                    px[xx, yy] = (int(r_ * 0.35), int(g_ * 0.35), int(b_ * 0.35), a_)
        art = dark
    art = art.resize((aw, ah), Image.LANCZOS)
    b.img.alpha_composite(art, (ax, ay))


def board(scale, mode, strength=1.0, battle=False):
    """Rowan's board: the gladius across the top (mode: 'rim' as implemented, or a glow spec (spread, blur) in icon pixels), a dagger under it."""
    f.chosen_marks()
    b = m.Board(scale, battle)
    sq = b.square(0, 0, 3, 1)
    if not battle:
        for x in range(3):
            b.rect(b.square(x, 0), m.NAVY)
    lay_icon_and_glow(b, "gladius", 0, 0, 3, 1, None if mode == "rim" else mode, strength, battle_charge=0.7 if battle else None)
    b.mark("B", 4, sq)
    if mode == "rim":
        f.unique_rim(b, sq)
    b.piece("dagger", 0, 1, tier=1, mark="B", fatigue=1, charge=0.9 if battle else None)
    return b


ICON_PER_SCREEN = 328 / 144.0    # the gladius's pixels per screen pixel at 1x (144x40 art box in the 3x1 piece)


def width(px):
    """The glow spec (spread, blur) in icon pixels for a dilation of `px` on screen; the blur is about 0.6 of the spread."""
    spread = max(1, int(round(px * ICON_PER_SCREEN)))
    return spread, max(2, int(round(spread * 0.6)))


NARROW = width(3.5)  # the first sheet's "narrow" (8, 5)
WIDE = width(6)      # the first sheet's "wide" (14, 8)


def still():
    opts = [("외곽선 금빛 1px (번짐까지 약 2px)", width(1)), ("참고: 2px", width(2)), ("참고: 3px", width(3))]
    pad = 12; col = 308 + 24; W = pad + 3 * col
    sheet = Image.new("RGBA", (W, 26 + 308 + 26 + 154 + 26 + 80), (0x1A, 0x18, 0x17, 255)); d = ImageDraw.Draw(sheet)
    for i, (label, mode) in enumerate(opts):
        x = pad + i * col
        d.text((x, 4), label, font=font(15), fill=m.TEXT)
        board(2, mode).lay(sheet, (x, 26), 2)
        d.text((x, 26 + 308 + 6), "전투 보드 (쿨다운 중)", font=font(13), fill=m.DIM)
        board(1, mode, battle=True).lay(sheet, (x, 26 + 308 + 26), 1)
    y = 26 + 308 + 26 + 154 + 10
    d.text((pad, y), "폭 = 아이콘의 실루엣을 넓힌 너비(화면 px, 1x 기준). 그 바깥으로 흐림이 폭의 절반쯤 더 번진다. 금 + 밝은 심을 구운 그림, 테는 없다.", font=font(13), fill=m.DIM)
    d.text((pad, y + 20), "숨쉬기 없음: 빛은 늘 같은 밝기로 선다.", font=font(13), fill=m.DIM)
    sheet.convert("RGB").save(HERE / "mock-outline-glow.png"); print("wrote", HERE / "mock-outline-glow.png")


def movie():
    if shutil.which("ffmpeg") is None:
        print("no ffmpeg"); return
    fps, seconds, cycle = 30, 4.0, 2.0
    tmp = Path(tempfile.mkdtemp())
    for i in range(int(fps * seconds)):
        t = i / fps
        strength = 0.55 + 0.45 * (0.5 - 0.5 * math.cos(2 * math.pi * t / cycle))
        sheet = Image.new("RGBA", (2 * (308 + 24) + 12, 308 + 24 + 22), (0x1A, 0x18, 0x17, 255)); d = ImageDraw.Draw(sheet)
        for j, (label, spec) in enumerate((("2px", width(2)), ("3px", width(3)))):
            x = 12 + j * (308 + 24)
            d.text((x, 4), label, font=font(15), fill=m.TEXT)
            board(2, spec, strength).lay(sheet, (x, 24), 2)
        sheet.convert("RGB").save(tmp / f"f{i:04d}.png")
    out = HERE / "mock-outline-glow.mp4"
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-framerate", str(fps), "-i", str(tmp / "f%04d.png"), "-c:v", "libx264", "-pix_fmt", "yuv420p", "-vf", "scale=trunc(iw/2)*2:trunc(ih/2)*2", str(out)], check=True)
    shutil.rmtree(tmp); print("wrote", out)


if __name__ == "__main__":
    still()
