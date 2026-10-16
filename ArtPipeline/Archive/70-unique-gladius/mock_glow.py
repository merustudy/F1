"""Round 70, follow-up (user: a Diablo-like glow behind the unique item): the gladius piece as implemented (the gold rim) beside two ways of
a soft gold glow behind its icon — with the rim and instead of it — at 1x in place and at 2x, on the node map and in battle (no API call).
The glow is the game's own glow piece (Icon/glow, behind a lit star) tinted the unique gold.
  mock-glow.png   the still comparison
  mock-glow.mp4   option A with a slow breathing (2 s a cycle, 60% to 100%), twice, at 2x — for the motion (needs ffmpeg)
  .venv/bin/python ArtPipeline/Archive/70-unique-gladius/mock_glow.py
"""
import math
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent / "69-tier-naming"))
import mock_tier_names as m  # noqa: E402
import mock_forge as f  # noqa: E402

ROOT = HERE.parents[2]
GLOW = ROOT / "Assets/@Art/UI/Icon/glow.png"
UNIQUE = f.UNIQUE
SHOT = Path.home() / "Library/Caches/F1/screenshots/20261016-081843/ko_54_map_unique_card.png"


def font(px):
    return ImageFont.truetype(str(m.FONT), int(round(px)))


def tinted_glow(size, alpha):
    """The glow piece scaled to `size` (w, h) and tinted the unique gold at `alpha` (0..1) of its own alpha."""
    g = Image.open(GLOW).convert("RGBA").resize(size, Image.LANCZOS)
    r, gg, b, a = g.split()
    tint = Image.new("RGBA", size, UNIQUE + (0,))
    tint.putalpha(a.point(lambda v: int(v * alpha)))
    return tint


def gladius_board(scale, rim, glow, battle=False):
    """Rowan's board: the gladius 3x1 across the top (with/without the rim, with/without the glow behind its icon), a dagger under its centre."""
    b = m.Board(scale, battle)
    sq = b.square(0, 0, 3, 1)
    if not battle:
        for x in range(3):
            b.rect(b.square(x, 0), m.NAVY)
    if glow > 0:
        k = b.k
        w = (sq[2] - sq[0]) * k; h = (sq[3] - sq[1]) * k
        layer = Image.new("RGBA", b.img.size, (0, 0, 0, 0))
        g = tinted_glow((int(w * 1.15), int(h * 2.2)), glow)
        layer.alpha_composite(g, (int(sq[0] * k + w / 2 - g.width / 2), int(sq[1] * k + h / 2 - g.height / 2)))
        # clipped to the piece's squares, as a UI mask would clip it
        mask = Image.new("L", b.img.size, 0)
        ImageDraw.Draw(mask).rectangle((sq[0] * k, sq[1] * k, sq[2] * k - 1, sq[3] * k - 1), fill=255)
        clipped = Image.new("RGBA", b.img.size, (0, 0, 0, 0)); clipped.paste(layer, (0, 0), mask)
        b.img.alpha_composite(clipped)
    was = b.battle; b.battle = True                           # the ground is drawn already (or none in battle): the icon only
    b.piece("gladius3", 0, 0, tier=4, mark="B", charge=0.7 if battle else None)
    b.battle = was
    if rim:
        f.unique_rim(b, sq)
    b.piece("dagger", 0, 1, tier=1, mark="B", fatigue=1, charge=0.9 if battle else None)
    return b


def still():
    f.chosen_marks()
    m.SIZES["gladius3"] = (3, 1)
    _i = m.icon
    m.icon = lambda n: Image.open(ROOT / "Assets/@Art/Item/gladius.png").convert("RGBA") if n == "gladius3" else _i(n)
    opts = [("지금: 금 테", True, 0.0), ("A: 금 테 + 뒤의 금빛 (권장)", True, 0.75), ("B: 금빛만 (테 없음)", False, 0.9)]
    pad = 12; col = 308 + 24; W = pad + 3 * col
    sheet = Image.new("RGBA", (W, 26 + 308 + 26 + 154 + 26 + 60), (0x1A, 0x18, 0x17, 255)); d = ImageDraw.Draw(sheet)
    for i, (label, rim, glow) in enumerate(opts):
        x = pad + i * col
        d.text((x, 4), label, font=font(15), fill=m.TEXT)
        gladius_board(2, rim, glow).lay(sheet, (x, 26), 2)
        d.text((x, 26 + 308 + 6), "전투 보드 (쿨다운 중)", font=font(13), fill=m.DIM)
        gladius_board(1, rim, glow, battle=True).lay(sheet, (x, 26 + 308 + 26), 1)
    d.text((pad, 26 + 308 + 26 + 154 + 10), "금빛은 켜진 ★ 뒤의 빛(Icon/glow)을 유니크 금으로 물들인 것, 조각의 칸 안에서만. A의 MP4는 2초에 한 번 60~100%로 숨 쉰다.", font=font(13), fill=m.DIM)
    sheet.convert("RGB").save(HERE / "mock-glow.png"); print("wrote", HERE / "mock-glow.png")


def movie():
    if shutil.which("ffmpeg") is None:
        print("no ffmpeg: the MP4 is skipped"); return
    f.chosen_marks()
    fps, seconds, cycle = 30, 4.0, 2.0
    frames = int(fps * seconds)
    tmp = Path(tempfile.mkdtemp())
    for i in range(frames):
        t = i / fps
        glow = 0.6 + 0.4 * (0.5 - 0.5 * math.cos(2 * math.pi * t / cycle))
        sheet = Image.new("RGBA", (308 + 24, 308 + 24), (0x1A, 0x18, 0x17, 255))
        gladius_board(2, True, glow).lay(sheet, (12, 12), 2)
        sheet.convert("RGB").save(tmp / f"f{i:04d}.png")
    out = HERE / "mock-glow.mp4"
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-framerate", str(fps), "-i", str(tmp / "f%04d.png"), "-c:v", "libx264", "-pix_fmt", "yuv420p", "-vf", "scale=trunc(iw/2)*2:trunc(ih/2)*2", str(out)], check=True)
    shutil.rmtree(tmp)
    print("wrote", out)


if __name__ == "__main__":
    still()
    movie()
