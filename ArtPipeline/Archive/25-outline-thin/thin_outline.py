"""Round 25: the figures' outline at 2/3 and at 1/2 of its thickness now, for a review. No API call.
  .venv/bin/python ArtPipeline/Archive/25-outline-thin/thin_outline.py

What a figure's outline is now: the art's own ink line round the silhouette and, under it, the dark ring the fit draws
round every figure (gen_image.fit_figure: FIGURE_OUTLINE 4 px on the 672x896 canvas, UI_LINE, its outer edge softened by
a blur of 0.8). So the outline can be made thinner without a call and without touching what the model drew: by
re-fitting the approved raws with a narrower ring, as `--refit` does. A ring between two whole pixels is the blend of
the two whole rings (the screen draws the canvas at about a third, so what counts is how much ink there is).

The thickness is measured, not assumed (thickness()): per pixel of depth inward from the silhouette, the share of dark
pixels above the share deep inside (where only the inner lines are), added up, with the soft rim outside. For the eleven
figures, the ring that makes the outline 2/3 of now is 1.05..1.59 px and the one that makes it 1/2 is 0.07..0.67 px; the
mockup uses their means for every figure, as one constant would: 1.35 and 0.4.

Writes ArtPipeline/output/outline/<2-3|1-2>/ as Assets/@Art lays them out (Unit/Job, Unit/Enemy, Pose/Job): the six
jobs, the five enemies and the twelve poses (tools/fit_pose.py with the same ring). A ring of 4 is checked to give the
files in Assets back to the byte.

The 2/3 was chosen and is the pipeline's own now (gen_image.FIGURE_OUTLINE 1.35, ring_alpha). What the review compared
with, Assets/@Art before the change, is kept in before/ and read from there.
"""
import contextlib
import io
import sys
from io import BytesIO
from pathlib import Path
from PIL import Image, ImageChops, ImageFilter

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
sys.path.insert(0, str(ROOT / "ArtPipeline/tools"))
import gen_image as G  # noqa: E402
import fit_pose as P  # noqa: E402

ART = HERE / "before"                 # Assets/@Art as it was in the review
OUT = ROOT / "ArtPipeline/output/outline"
NOW = 4
VARIANTS = {"2-3": 1.35, "1-2": 0.4}
JOBS = ["knight", "valkyrie", "bishop", "paladin", "archmage", "spellblade"]
ENEMIES = ["goblin_raider", "goblin_archer", "goblin_shaman", "cave_rat", "mine_overseer"]


def ring_alpha(body, r):
    """The ring's alpha for a width in canvas pixels: the fit's ring (grow, then a blur of 0.8) and, between two
    whole widths, their blend."""
    def whole(n):
        return G.grow(body, n).filter(ImageFilter.GaussianBlur(0.8)) if n > 0 else Image.new("L", body.size, 0)
    lo = int(r)
    t = r - lo
    return whole(lo) if t < 1e-6 else Image.blend(whole(lo), whole(lo + 1), t)


def ringed(canvas, r):
    body = canvas.getchannel("A").point(lambda v: 255 if v > G.ALPHA_FLOOR else 0)
    under = Image.new("RGBA", canvas.size, G.UI_LINE + (0,))
    under.putalpha(ring_alpha(body, r))
    under.alpha_composite(canvas)
    return under


def figure(kind, key, r):
    """The approved raw fitted as gen_image does, with a ring of r."""
    saved, G.FIGURE_OUTLINE = G.FIGURE_OUTLINE, 0
    try:
        png, _ = G.fit(kind, (G.OUTPUT_DIR / kind / f"{key}.raw.png").read_bytes(), G.read_roster_row(kind, key))
    finally:
        G.FIGURE_OUTLINE = saved
    return ringed(Image.open(BytesIO(png)).convert("RGBA"), r)


def pose(kind, key, r):
    """The pose fitted as tools/fit_pose.py does, with a ring of r (what it would save is kept here instead)."""
    kept = {}
    place, ring = P.place, P.ring

    class Keep:
        def __init__(self, image):
            self.image = image

        def save(self, _):
            kept["image"] = self.image

    P.ring = lambda canvas: ringed(canvas, r)
    P.place = lambda *args: Keep(place(*args))
    try:
        with contextlib.redirect_stdout(io.StringIO()):
            P.fit_pose(kind, key, P.roster_scales(kind))
    finally:
        P.place, P.ring = place, ring
    return kept["image"]


def erode(mask, n):
    return ImageChops.invert(G.grow(ImageChops.invert(mask), n))


def thickness(image, depth=16, dark=70):
    """The outline's mean thickness in canvas pixels (see the docstring above)."""
    a = image.getchannel("A")
    inside = a.point(lambda v: 255 if v >= 128 else 0)
    ink = image.convert("L").point(lambda v: 255 if v < dark else 0)
    rim = sum(v / 255.0 for v, i in zip(a.getdata(), inside.getdata()) if 8 < v < 128 and not i)
    shares, before = [], inside
    for _ in range(depth):
        after = erode(before, 1)
        ring = ImageChops.subtract(before, after)
        n = sum(1 for v in ring.getdata() if v)
        shares.append((n, sum(1 for v, m in zip(ring.getdata(), ink.getdata()) if v and m) / max(1, n)))
        before = after
    base = sum(s for _, s in shares[-4:]) / 4
    t = rim / shares[0][0]
    for _, s in shares:
        if s - base < 0.08:
            break
        t += min(1.0, (s - base) / (1 - base))
    return t


def main():
    screen = 225 / 672
    print("그림(전신): 지금의 외곽선 -> 2/3 안, 1/2 안 (그림 캔버스 px, 괄호는 화면 px)")
    for kind, folder, keys in (("character", "Unit/Job", JOBS), ("enemy", "Unit/Enemy", ENEMIES)):
        for key in keys:
            now = Image.open(ART / folder / f"{key}.png").convert("RGBA")
            same = figure(kind, key, NOW).tobytes() == now.tobytes()
            t0 = thickness(now)
            line = f"  {key:14s} {t0:5.2f} ({t0 * screen:.2f})"
            for name, r in VARIANTS.items():
                image = figure(kind, key, r)
                path = OUT / name / folder / f"{key}.png"
                path.parent.mkdir(parents=True, exist_ok=True)
                image.save(path)
                t = thickness(image)
                line += f" -> {name}: {t:5.2f} ({t * screen:.2f}, {t / t0:.0%})"
            print(line + ("" if same else "   경고: 띠 4로 다시 맞춘 것이 before와 다르다"))
    print("자세:")
    for key in JOBS:
        for kind in P.POSES:
            now = Image.open(ART / "Pose/Job" / f"{key}_{kind}.png").convert("RGBA")
            same = pose(kind, key, NOW).tobytes() == now.tobytes()
            for name, r in VARIANTS.items():
                path = OUT / name / "Pose/Job" / f"{key}_{kind}.png"
                path.parent.mkdir(parents=True, exist_ok=True)
                pose(kind, key, r).save(path)
            print(f"  {key}_{kind}: {'띠 4로 다시 맞춘 것이 before와 같다' if same else '경고: 띠 4로 다시 맞춘 것이 before와 다르다'}")
    print("출력:", OUT.relative_to(ROOT))


if __name__ == "__main__":
    main()
