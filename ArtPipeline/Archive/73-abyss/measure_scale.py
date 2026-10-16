#!/usr/bin/env python3
"""Round 73: the Scale of a monster's pose against its approved figure, measured on the cranium.

The skull of a skeleton is a bone-cream area closed by ink lines (the jaw, the teeth and the neck are cut off by them),
and its size does not change with the pose while the head keeps its three-quarter turn. The cranium is the cream
component touching the top of the largest pale green eye. Scale = sqrt(area of the figure's cranium / area of the pose's).
  .venv/bin/python ArtPipeline/Archive/73-abyss/measure_scale.py skeleton_guard abyss_lord
"""
import colorsys
import sys
from collections import deque
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "output"


def components(mask, w, h):
    seen = [False] * (w * h)
    comps = []
    for start in range(w * h):
        if not mask[start] or seen[start]:
            continue
        seen[start] = True
        q = deque([start]); comp = []
        while q:
            i = q.popleft(); comp.append(i)
            x, y = i % w, i // w
            for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
                if 0 <= nx < w and 0 <= ny < h:
                    j = ny * w + nx
                    if mask[j] and not seen[j]:
                        seen[j] = True; q.append(j)
        comps.append(comp)
    return comps


def classes(path):
    im = Image.open(path).convert("RGBA")
    w, h = im.size
    px = im.load()
    green = [False] * (w * h); cream = [False] * (w * h); rust = [False] * (w * h)
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a < 128:
                continue
            hh, ll, ss = colorsys.rgb_to_hls(r / 255, g / 255, b / 255)
            if 0.17 <= hh <= 0.36 and ss >= 0.25 and ll >= 0.55:
                green[y * w + x] = True
            elif ll >= 0.70 and ss <= 0.55 and 0.04 <= hh <= 0.18:
                cream[y * w + x] = True
            elif 0.15 <= ll <= 0.5 and ss >= 0.25 and (hh <= 0.1 or hh >= 0.95):
                rust[y * w + x] = True
    return w, h, green, cream, rust


def near_component(mask, w, h, eye, reach=70):
    """The largest component of the mask met by rays from the eye's centre, in eight directions."""
    cx = sum(i % w for i in eye) // len(eye); cy = sum(i // w for i in eye) // len(eye)
    hits = set()
    for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (-1, 1), (1, -1), (-1, -1)):
        for step in range(1, reach):
            x, y = cx + dx * step, cy + dy * step
            if not (0 <= x < w and 0 <= y < h):
                break
            if mask[y * w + x]:
                hits.add(y * w + x); break
    best = []
    for comp in components(mask, w, h):
        if len(comp) > len(best) and any(i in hits for i in comp):
            best = comp
    return len(best)


def measure(path):
    w, h, green, cream, rust = classes(path)
    eyes = sorted(components(green, w, h), key=len, reverse=True)
    if not eyes:
        raise SystemExit(f"{path.name}: no pale green eye found")
    eye = eyes[0]
    return {"eye": len(eye), "face": near_component(cream, w, h, eye), "iron": near_component(rust, w, h, eye)}


for key in [a for a in sys.argv[1:] if not a.startswith("--")]:
    fig = measure(OUT / "enemy" / f"{key}.raw.png")
    print(f"{key}: figure eye {fig['eye']}px, face {fig['face']}px, iron {fig['iron']}px")
    for kind in ("enemy_attack", "enemy_hit"):
        p = OUT / kind / f"{key}.raw.png"
        if not p.exists():
            print(f"  {kind}: (no raw yet)"); continue
        m = measure(p)
        parts = {k: (fig[k] / m[k]) ** 0.5 for k in fig if m[k] and fig[k]}
        print(f"  {kind}: eye {m['eye']}px, face {m['face']}px, iron {m['iron']}px -> Scale " + ", ".join(f"{k} {v:.6f}" for k, v in parts.items()))


def crown_area(path):
    """The rust pixels of the crown: the window above the largest eye (the band sits on the brow, the spikes up to 260px above)."""
    w, h, green, cream, rust = classes(path)
    eye = sorted(components(green, w, h), key=len, reverse=True)[0]
    cx = sum(i % w for i in eye) // len(eye); top = min(i // w for i in eye)
    return sum(1 for y in range(max(top - 260, 0), min(top + 15, h)) for x in range(max(cx - 150, 0), min(cx + 150, w)) if rust[y * w + x])


if "--crown" in sys.argv:
    key = "abyss_lord"
    fig = crown_area(OUT / "enemy" / f"{key}.raw.png")
    print(f"{key}: figure crown {fig}px")
    for kind in ("enemy_attack", "enemy_hit"):
        area = crown_area(OUT / kind / f"{key}.raw.png")
        print(f"  {kind}: crown {area}px -> Scale by crown {(fig / area) ** 0.5:.6f}")


def ring_width(path, where):
    """The widest row of an iron ring round the head or the neck: a horizontal ring projects to its diameter at any turn of
    the head (the skullcap's band above the eye, the lord's collar below it; the chain hanging from the collar is narrower)."""
    w, h, green, cream, rust = classes(path)
    eye = sorted(components(green, w, h), key=len, reverse=True)[0]
    cx = sum(i % w for i in eye) // len(eye)
    top = min(i // w for i in eye); bottom = max(i // w for i in eye)
    best = None
    for comp in components(rust, w, h):
        if len(comp) < 300:
            continue
        xs = [i % w for i in comp]; ys = [i // w for i in comp]
        if not (min(xs) - 40 <= cx <= max(xs) + 40):
            continue
        if where == "above" and not (top - 120 <= max(ys) <= top + 30):
            continue
        if where == "below" and not (bottom <= min(ys) <= bottom + 220):
            continue
        if best is None or len(comp) > len(best):
            best = comp
    if best is None:
        raise SystemExit(f"{path.name}: no ring {where} the eye")
    rows = {}
    for i in best:
        x, y = i % w, i // w
        lo, hi = rows.get(y, (x, x)); rows[y] = (min(lo, x), max(hi, x))
    return max(hi - lo + 1 for lo, hi in rows.values()), len(best)


if "--rings" in sys.argv:
    for key, where in (("skeleton_guard", "above"), ("abyss_lord", "below")):
        fig, area = ring_width(OUT / "enemy" / f"{key}.raw.png", where)
        print(f"{key}: figure ring {where} the eye: widest row {fig}px (area {area})")
        for kind in ("enemy_attack", "enemy_hit"):
            width, area = ring_width(OUT / kind / f"{key}.raw.png", where)
            print(f"  {kind}: widest row {width}px (area {area}) -> Scale by ring {fig / width:.6f}")


if "--list" in sys.argv:
    for key in ("skeleton_guard", "abyss_lord"):
        for kind in ("enemy", "enemy_attack", "enemy_hit"):
            path = OUT / kind / f"{key}.raw.png"
            w, h, green, cream, rust = classes(path)
            eye = sorted(components(green, w, h), key=len, reverse=True)[0]
            cx = sum(i % w for i in eye) // len(eye); cy = sum(i // w for i in eye) // len(eye)
            print(f"{kind}/{key}: eye centre ({cx},{cy}), size {w}x{h}")
            comps = [c for c in components(rust, w, h) if len(c) >= 300]
            for comp in sorted(comps, key=len, reverse=True)[:12]:
                xs = [i % w for i in comp]; ys = [i // w for i in comp]
                rows = {}
                for i in comp:
                    x, y = i % w, i // w
                    lo, hi = rows.get(y, (x, x)); rows[y] = (min(lo, x), max(hi, x))
                widest = max(hi - lo + 1 for lo, hi in rows.values())
                print(f"    rust area {len(comp):6d}  x {min(xs):4d}..{max(xs):4d}  y {min(ys):4d}..{max(ys):4d}  widest row {widest:4d}  (dx {min(xs)-cx:+5d}..{max(xs)-cx:+5d}, dy {min(ys)-cy:+5d}..{max(ys)-cy:+5d})")
