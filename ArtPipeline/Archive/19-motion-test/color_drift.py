"""How far the colors of a pose drifted from the approved figure (round 19), no API call.
  .venv/bin/python ArtPipeline/Archive/19-motion-test/color_drift.py

Every opaque pixel is given to the nearest of the figure's main colors (sampled on the approved raw) when it is
close to one; the mean of each color's pixels is compared with the approved figure's as a CIE76 delta E in Lab.
Under about 2 the eye does not tell two flat fills apart side by side; 2 to 5 is a shade apart.
"""
import math
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
APPROVED = ROOT / "ArtPipeline/Archive/12-roar-style/approved/character/valkyrie.raw.png"
POSES = [ROOT / f"ArtPipeline/output/character/{name}.raw.png" for name in ("valkyrie_attack", "valkyrie_attack2", "valkyrie_hit")]
MAIN = {"피부": (248, 172, 117), "머리": (243, 184, 90), "망토": (132, 43, 41), "치마": (60, 56, 75), "털": (215, 187, 162),
        "가죽": (100, 60, 48), "윗옷": (246, 229, 207), "금 장식": (184, 124, 64), "도끼날": (125, 118, 122)}
NEAR = 26


def lab(rgb):
    def lin(c):
        c /= 255.0
        return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4
    r, g, b = (lin(c) for c in rgb)
    x = (0.4124 * r + 0.3576 * g + 0.1805 * b) / 0.95047
    y = 0.2126 * r + 0.7152 * g + 0.0722 * b
    z = (0.0193 * r + 0.1192 * g + 0.9505 * b) / 1.08883
    f = lambda t: t ** (1 / 3) if t > 0.008856 else 7.787 * t + 16 / 116
    return 116 * f(y) - 16, 500 * (f(x) - f(y)), 200 * (f(y) - f(z))


def means(image):
    """The mean of each main color's pixels in an RGBA image."""
    sums = {k: [0, 0, 0, 0] for k in MAIN}
    for r, g, b, a in image.getdata():
        if a <= 200:
            continue
        key, best = None, NEAR * NEAR
        for k, (R, G, B) in MAIN.items():
            d = (r - R) ** 2 + (g - G) ** 2 + (b - B) ** 2
            if d < best:
                key, best = k, d
        if key:
            s = sums[key]
            s[0] += r; s[1] += g; s[2] += b; s[3] += 1
    return {k: (s[0] / s[3], s[1] / s[3], s[2] / s[3]) for k, s in sums.items() if s[3]}


def report(path, base):
    pose = means(Image.open(path).convert("RGBA"))
    print(path.name)
    for k in MAIN:
        if k in base and k in pose:
            e = math.dist(lab(base[k]), lab(pose[k]))
            print(f"  {k:5s} 승인 {tuple(round(c) for c in base[k])} -> {tuple(round(c) for c in pose[k])}  ΔE {e:.1f}")


if __name__ == "__main__":
    base = means(Image.open(APPROVED).convert("RGBA"))
    for raw in POSES:
        report(raw, base)
        matched = raw.with_name(raw.name.replace(".raw.png", ".matched.png"))
        if matched.exists():
            report(matched, base)
