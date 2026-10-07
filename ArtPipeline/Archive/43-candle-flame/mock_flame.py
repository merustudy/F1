"""Round 43: the candle's flame as a sprite animation (4~6 frames) with a thinner outline (user, 2026-10-07: "the outline is too
thick"). Mockups over the candle of the implemented battle screenshot, zoomed 3x: as MP4 (x1, then x0.5 slow) and a frame sheet.
Variants: as now / the same flame with a thin outline / five frames warped from the same flame / five drawn frames. No API call.
  .venv/bin/python ArtPipeline/Archive/43-candle-flame/mock_flame.py
"""
import math
import shutil
import subprocess
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parents[3]; HERE = Path(__file__).resolve().parent
FONT = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"
SHOT = Path.home() / "Library/Caches/F1/screenshots/20261007-r42d/ko_40_battle_item_card.png"
RAW = ROOT / "ArtPipeline/output/ui_piece/candle_flame.raw.png"
NOW = ROOT / "Assets/@Art/UI/Frame/candle_flame.png"
UI_LINE = (24, 9, 7)
CANVAS = (60, 110)                   # the sprite's canvas (2x)
RECT = (26, 48)                      # the flame's rect on screen (UiPrefabSetup.Battle)
Z = 3                                # the mock's zoom
REGION = (880, 680, 1040, 920)       # the candle on the battle screen
FPS = 30
LENGTH = 3.0
FRAME_FPS = 10                       # how fast the frames of an animated flame change
SEQUENCE = [0, 1, 2, 1, 3, 4, 3, 0, 2, 4, 1, 3]   # not a plain cycle, so the loop does not read as one
ORANGE, MID, CORE, BASE = (228, 103, 50), (246, 157, 59), (255, 222, 130), (103, 47, 27)


def font(px): return ImageFont.truetype(str(FONT), int(round(px)))


# ---- the pipeline's finishing: the art fitted to the canvas, a band of UI_LINE around it ---------------------------------------

def cutout(img):
    return img.crop(img.getbbox())


def outline(img, band):
    if band <= 0:
        return img
    grown = img.split()[3].filter(ImageFilter.MaxFilter(2 * band + 1))
    ring = Image.new("RGBA", img.size, UI_LINE + (0,))
    ring.putalpha(grown)
    ring.alpha_composite(img)
    return ring


def fit_scale(art, band, fill=0.84):
    """The scale that fits this art (plus its band) into 84% of the canvas, and the bottom the art's base stands on."""
    avail = (CANVAS[0] * fill - 2 * band, CANVAS[1] * fill - 2 * band)
    s = min(avail[0] / art.size[0], avail[1] / art.size[1])
    bottom = CANVAS[1] / 2 + art.size[1] * s / 2
    return s, bottom


def place(art, scale, bottom, band):
    """Lays the art on the canvas at this scale with its base at `bottom` (so frames of one flame share a base), then the band."""
    scaled = art.resize((max(1, round(art.size[0] * scale)), max(1, round(art.size[1] * scale))), Image.LANCZOS)
    out = Image.new("RGBA", CANVAS, (0, 0, 0, 0))
    out.alpha_composite(scaled, (round(CANVAS[0] / 2 - scaled.size[0] / 2), round(bottom - scaled.size[1])))
    return outline(out, band)


# ---- the flames ---------------------------------------------------------------------------------------------------------------------

def raw_art(height=220):
    art = cutout(Image.open(RAW).convert("RGBA"))
    return art.resize((round(art.size[0] * height / art.size[1]), height), Image.LANCZOS)


def warp(art, bend, stretch):
    """The same flame bent at the tip (rows shift more towards the top) and stretched from its base."""
    w, h = art.size
    tall = art.resize((w, max(1, round(h * stretch))), Image.LANCZOS)
    out = Image.new("RGBA", (w + 2 * w // 2, tall.size[1]), (0, 0, 0, 0))
    for y in range(tall.size[1]):
        u = 1 - y / tall.size[1]
        dx = bend * 0.18 * w * (u ** 1.6)
        out.alpha_composite(tall.crop((0, y, w, y + 1)), (round(w // 2 + dx), y))
    return cutout(out)


def bezier(p0, p1, p2, p3, n=40):
    pts = []
    for i in range(n + 1):
        t = i / n
        pts.append((
            (1 - t) ** 3 * p0[0] + 3 * (1 - t) ** 2 * t * p1[0] + 3 * (1 - t) * t ** 2 * p2[0] + t ** 3 * p3[0],
            (1 - t) ** 3 * p0[1] + 3 * (1 - t) ** 2 * t * p1[1] + 3 * (1 - t) * t ** 2 * p2[1] + t ** 3 * p3[1]))
    return pts


def tongue(cx, base_y, width, height, bend):
    """A teardrop: a round base and a tip leaning by `bend` (fraction of the width)."""
    tip = (cx + bend * width * 0.9, base_y - height)
    waist_y = base_y - height * 0.3
    left = bezier((cx - width / 2, waist_y), (cx - width * 0.62, base_y - height * 0.62), (tip[0] - width * 0.22, base_y - height * 0.9), tip)
    right = bezier(tip, (tip[0] + width * 0.22, base_y - height * 0.9), (cx + width * 0.62, base_y - height * 0.62), (cx + width / 2, waist_y))
    bottom = [(cx + width / 2 * math.cos(math.radians(a)), waist_y + height * 0.3 * math.sin(math.radians(a))) for a in range(0, 181, 6)]
    return left + right + bottom


def drawn_art(bend, stretch, aa=4):
    """Five-frame flame drawn as flat colours: an orange tongue, a lighter one inside, a cream core and the dark base."""
    W, H = 240 * aa, 440 * aa
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0)); d = ImageDraw.Draw(img)
    cx, base_y = W / 2, H * 0.95
    height = H * 0.78 * stretch
    width = W * 0.62
    d.polygon(tongue(cx, base_y, width, height, bend), fill=ORANGE + (255,))
    d.polygon(tongue(cx, base_y - height * 0.03, width * 0.72, height * 0.72, bend * 0.85), fill=MID + (255,))
    d.polygon(tongue(cx, base_y - height * 0.07, width * 0.44, height * 0.44, bend * 0.7), fill=CORE + (255,))
    d.ellipse((cx - width * 0.26, base_y - height * 0.06 - width * 0.12, cx + width * 0.26, base_y - height * 0.06 + width * 0.12), fill=BASE + (255,))
    img = cutout(img)
    return img.resize((img.size[0] // aa, img.size[1] // aa), Image.LANCZOS)


POSES = [(0.0, 1.0), (0.7, 1.05), (1.0, 0.96), (-0.6, 1.08), (-1.0, 0.97)]


def variants():
    now = Image.open(NOW).convert("RGBA")
    raw = raw_art()
    s, bottom = fit_scale(raw, 2)
    thin = place(raw, s, bottom, 2)
    warped = [place(warp(raw, b, st), s, bottom, 2) for b, st in POSES]
    ref = drawn_art(0.0, 1.0)
    s2, bottom2 = fit_scale(ref, 2)
    drawn = [place(drawn_art(b, st), s2, bottom2, 2) for b, st in POSES]
    return [
        ("지금: 생성한 불꽃 한 장, 외곽선 6(화면 3px), 늘이고 기울이는 흔들림", [now], 1.0),
        ("안 C: 같은 불꽃, 외곽선 2(화면 1px). 흔들림은 지금 그대로", [thin], 1.0),
        ("안 A3: 같은 불꽃을 휘고 늘여 만든 5장(외곽선 2) + 절반의 흔들림", warped, 0.5),
        ("안 A2: 평면 색으로 그린 5장(외곽선 2) + 절반의 흔들림", drawn, 0.5),
    ]


# ---- the scene: the candle of the battle screen without its flame ----------------------------------------------------------------

def scene():
    """The zoomed candle, the flame painted out (each of its pixels takes the colour at the same distance from the flame's middle, to the
    side or above, so that the glow stays), and where the flame's rect stands (its bottom middle)."""
    crop = Image.open(SHOT).convert("RGBA").crop(REGION)
    img = crop.resize((crop.size[0] * Z, crop.size[1] * Z), Image.LANCZOS)
    w, h = img.size
    px = img.load()

    def warm(x, y):
        # The flame's body and core: bright and strongly orange. The glow-lit stone (about 150, 120, 75) and the molten top's cream are not.
        r, g, b, _ = px[x, y]
        return r > 200 and r > g > b and (r - b) / r > 0.45

    hits = [(x, y) for y in range(0, int(h * 0.35)) for x in range(int(w * 0.35), int(w * 0.65)) if warm(x, y)]
    x0 = min(p[0] for p in hits); x1 = max(p[0] for p in hits); y0 = min(p[1] for p in hits); y1 = max(p[1] for p in hits)
    print("flame on the zoomed crop:", (x0, y0, x1, y1))
    band = 3 * Z + 2                                     # the outline (3 screen px) and the glow's rim
    mask = Image.new("L", img.size, 0); md = ImageDraw.Draw(mask)
    for x, y in hits:
        md.point((x, y), 255)
    mask = mask.filter(ImageFilter.MaxFilter(2 * band + 1))
    mpx = mask.load()
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    fixed = img.copy(); fpx = fixed.load()
    for y in range(max(0, y0 - band), min(h, y1 + band + 1)):
        for x in range(max(0, x0 - band), min(w, x1 + band + 1)):
            if mpx[x, y] == 0:
                continue
            r = math.hypot(x - cx, y - cy)
            for sx, sy in ((cx - r, cy), (cx + r, cy), (cx, cy - r)):
                ix, iy = int(round(sx)), int(round(sy))
                if 0 <= ix < w and 0 <= iy < h and mpx[ix, iy] == 0:
                    fpx[x, y] = px[ix, iy]
                    break
    # The flame's rect: its art (with the 3 px outline) ends 14 of 110 canvas pixels above the rect's bottom.
    anchor = (cx, y1 + 3 * Z + 14 / 110 * RECT[1] * Z)
    return fixed, anchor


def wobble(t, amount):
    """CandleView.Update: the stretch and the sway, at the amount asked (1 = as now, 0.5 = half)."""
    tt = t * 11
    stretch = 1 + 0.1 * amount * (0.6 * math.sin(tt) + 0.4 * math.sin(tt * 2.7 + 1.3))
    sway = 4 * amount * math.sin(tt * 0.8 + 0.7)
    return (1 - (stretch - 1) * 0.5, stretch), sway


def lay_flame(scene_img, anchor, sprite, t, amount, animated):
    img = scene_img.copy()
    (sx, sy), sway = wobble(t, amount)
    base = sprite.resize((RECT[0] * Z, RECT[1] * Z), Image.LANCZOS)
    scaled = base.resize((max(1, round(base.size[0] * sx)), max(1, round(base.size[1] * sy))), Image.BICUBIC)
    big = Image.new("RGBA", (base.size[0] * 3, base.size[1] * 3), (0, 0, 0, 0))
    pivot = (big.size[0] / 2, big.size[1] * 0.75)
    big.alpha_composite(scaled, (round(pivot[0] - scaled.size[0] / 2), round(pivot[1] - scaled.size[1])))
    big = big.rotate(sway, resample=Image.BICUBIC, center=pivot)
    img.alpha_composite(big, (round(anchor[0] - pivot[0]), round(anchor[1] - pivot[1])))
    return img


def frame_of(frames, t):
    if len(frames) == 1:
        return frames[0]
    return frames[SEQUENCE[int(t * FRAME_FPS) % len(SEQUENCE)] % len(frames)]


def panel(scene_img, anchor, label, frames, amount, t, speed_label):
    img = lay_flame(scene_img, anchor, frame_of(frames, t), t, amount, len(frames) > 1)
    out = Image.new("RGB", (img.size[0], img.size[1] + 64), (34, 34, 36))
    out.paste(img.convert("RGB"), (0, 64))
    d = ImageDraw.Draw(out)
    f = font(17)
    words = label.split(": ", 1)
    d.text((10, 8), words[0], font=font(20), fill=(230, 226, 214))
    d.text((10, 36), words[1] if len(words) > 1 else "", font=f, fill=(190, 188, 180))
    d.text((img.size[0] - 10, 8), speed_label, font=f, fill=(150, 150, 146), anchor="ra")
    return out


def main():
    scene_img, anchor = scene()
    variants_ = variants()

    # The frame sheet: each variant's frames at 4x the sprite (the canvas 60x110 -> 240x440), and the sprite at the screen's size x3.
    sheet_rows = []
    for label, frames, _ in variants_:
        row = Image.new("RGB", (len(frames) * 250 + 20 + 120, 460), (34, 34, 36))
        for i, fr in enumerate(frames):
            row.paste(fr.resize((240, 440), Image.NEAREST).convert("RGB"), (10 + i * 250, 10), fr.resize((240, 440), Image.NEAREST))
        small = frames[0].resize((RECT[0] * Z, RECT[1] * Z), Image.LANCZOS)
        row.paste(small.convert("RGB"), (len(frames) * 250 + 30, 10), small)
        sheet_rows.append((label, row))
    W = max(r.size[0] for _, r in sheet_rows) + 20; H = sum(r.size[1] + 44 for _, r in sheet_rows) + 60
    sheet = Image.new("RGB", (W, H), (34, 34, 36)); d = ImageDraw.Draw(sheet); y = 10
    for label, row in sheet_rows:
        d.text((10, y), label, font=font(22), fill=(230, 226, 214)); y += 34
        sheet.paste(row, (10, y)); y += row.size[1] + 10
    d.text((10, y), "프레임은 스프라이트 캔버스(60x110)의 4배, 오른쪽 작은 것은 화면 크기(26x48)의 3배. 외곽선 띠는 UI_LINE 색이고 숫자는 2배 캔버스의 px(화면에서는 절반).", font=font(18), fill=(176, 176, 170))
    sheet.save(HERE / "mock-frames.png"); print("wrote", "mock-frames.png", sheet.size)

    # The video: four panels side by side, x1 for LENGTH seconds, then x0.5 for twice as long.
    frames_dir = HERE / "_frames"
    if frames_dir.exists():
        shutil.rmtree(frames_dir)
    frames_dir.mkdir()
    n = round(LENGTH * FPS)
    i = 0
    for speed, count, speed_label in ((1.0, n, "x1"), (0.5, n * 2, "x0.5 (느린 재생)")):
        for k in range(count):
            t = k / FPS * speed
            panels = [panel(scene_img, anchor, label, frames, amount, t, speed_label) for label, frames, amount in variants_]
            out = Image.new("RGB", (sum(p.size[0] for p in panels), panels[0].size[1]), (34, 34, 36))
            x = 0
            for p in panels:
                out.paste(p, (x, 0)); x += p.size[0]
            out.save(frames_dir / f"{i:05d}.png"); i += 1
    video = HERE / "mock-flame.mp4"
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-framerate", str(FPS), "-i", str(frames_dir / "%05d.png"),
                    "-c:v", "libx264", "-pix_fmt", "yuv420p", "-crf", "18", str(video)], check=True)
    shutil.rmtree(frames_dir)
    print("wrote", video.name, i, "frames")

    # A still of the four at one moment, for the report.
    stills = [panel(scene_img, anchor, label, frames, amount, 0.37, "정지 화면") for label, frames, amount in variants_]
    out = Image.new("RGB", (sum(p.size[0] for p in stills), stills[0].size[1]), (34, 34, 36)); x = 0
    for p in stills:
        out.paste(p, (x, 0)); x += p.size[0]
    out.save(HERE / "mock-still.png"); print("wrote mock-still.png", out.size)


if __name__ == "__main__":
    main()
