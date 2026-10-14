# -*- coding: utf-8 -*-
"""Round 64 (2026-10-14, the user: "도끼는 현재 만족. 얼굴은 2차 얼굴로 해줘. 이번에 생성한 그림 얼굴이 미묘하게 다르네"): the valkyrie's
attack pose = the third try (the axe the user approved) with the face of the second try. No API call.
  .venv/bin/python ArtPipeline/Archive/64-valkyrie-body-manga/compose_attack_face.py
Reads approved/attack-sources/attack_try2.raw.png and attack_try3.raw.png, writes output/attack/valkyrie.raw.png (then fit it with
tools/fit_pose.py --type attack --key valkyrie, as any attack pose).

1. The second try is turned and moved so that its two eye whites land on the third's (a similarity: scale 1.006, 2.4 degrees):
   the eyes, brows, ear, bangs and hair outline then lie on each other; only the mouth and the chin differ (the third opens the
   mouth wider and its chin sits about 10 px lower).
2. Above a cut just under the nose, everything inside a polygon that runs through the hair (where the two match) is the second's.
3. Below the cut, the second's face is taken only inside its own outline (its skin area, cut off from the neck and the ear, with
   the holes of the eyes and mouth filled, grown by 4 px for the ink line), so the boundary lies on its ink.
4. Where the third's longer jaw would still show outside that outline (its chin and its jaw line under the second's chin), the
   second's own neck is taken too: under its chin the second has the cloak's collar and a lock of hair, as in the second try.
The figure's silhouette (alpha) stays the third's.
"""
import colorsys
from collections import deque
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFilter

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
SOURCES = HERE / "approved" / "attack-sources"
OUT = ROOT / "ArtPipeline" / "output" / "attack" / "valkyrie.raw.png"

EYES_2 = [(796, 192), (843, 198)]   # the eye whites' centres, read off the pictures
EYES_3 = [(710, 200), (757, 208)]
CUT = 256                           # the cut between the upper and the lower part, just under the nose
TOP = [(640, 150), (800, 150), (812, 200), (808, CUT + 6), (636, CUT + 6), (634, 200)]
BOX = (620, 140, 820, 320)          # where the face is looked for
LIMIT = [(645, 150), (805, 150), (815, 200), (808, 260), (790, 285), (745, 300), (712, 306), (690, 300), (668, 272), (652, 245), (648, 200)]
SEED = (735, 225)                   # a point of the cheek in both
OUTLINE = 4


def similarity(src, dst):
    import math
    (x1, y1), (x2, y2) = src
    (u1, v1), (u2, v2) = dst
    s = math.hypot(u2 - u1, v2 - v1) / math.hypot(x2 - x1, y2 - y1)
    th = math.atan2(v2 - v1, u2 - u1) - math.atan2(y2 - y1, x2 - x1)
    c, si = s * math.cos(th), s * math.sin(th)
    mx, my, mu, mv = (x1 + x2) / 2, (y1 + y2) / 2, (u1 + u2) / 2, (v1 + v2) / 2
    tx, ty = mu - (c * mx - si * my), mv - (si * mx + c * my)
    det = c * c + si * si
    ia, ib = c / det, si / det
    return (ia, ib, -(ia * tx + ib * ty), -ib, ia, -(-ib * tx + ia * ty))


def skin(p):
    r, g, b, a = p
    if a < 200:
        return False
    h, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
    return (h <= 0.09 or h >= 0.97) and 0.18 <= s <= 0.62 and v >= 0.66


def face_region(im):
    lim = Image.new("L", im.size, 0)
    ImageDraw.Draw(lim).polygon(LIMIT, fill=255)
    lp, px = lim.load(), im.load()

    def inside(x, y):
        return BOX[0] <= x < BOX[2] and BOX[1] <= y < BOX[3] and lp[x, y]

    comp, queue = set(), deque([SEED])
    while queue:
        x, y = queue.popleft()
        if (x, y) in comp or not inside(x, y) or not skin(px[x, y]):
            continue
        comp.add((x, y))
        queue.extend([(x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)])
    mask = Image.new("L", im.size, 0)
    mp = mask.load()
    for x, y in comp:
        mp[x, y] = 255
    border = ([(x, BOX[1]) for x in range(BOX[0], BOX[2])] + [(x, BOX[3] - 1) for x in range(BOX[0], BOX[2])]
              + [(BOX[0], y) for y in range(BOX[1], BOX[3])] + [(BOX[2] - 1, y) for y in range(BOX[1], BOX[3])])
    outside, queue = set(), deque(border)
    while queue:
        x, y = queue.popleft()
        if (x, y) in outside or not (BOX[0] <= x < BOX[2] and BOX[1] <= y < BOX[3]) or mp[x, y]:
            continue
        outside.add((x, y))
        queue.extend([(x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)])
    for y in range(BOX[1], BOX[3]):
        for x in range(BOX[0], BOX[2]):
            if (x, y) not in outside:
                mp[x, y] = 255
    return mask


def grow(mask, radius):
    for _ in range(radius):
        mask = mask.filter(ImageFilter.MaxFilter(3))
    return mask


def main():
    second = Image.open(SOURCES / "attack_try2.raw.png").convert("RGBA")
    third = Image.open(SOURCES / "attack_try3.raw.png").convert("RGBA")
    warped = second.transform(third.size, Image.AFFINE, similarity(EYES_2, EYES_3), resample=Image.BICUBIC)

    top = Image.new("L", third.size, 0)
    ImageDraw.Draw(top).polygon(TOP, fill=255)
    ramp = Image.new("L", third.size, 0)
    rp = ramp.load()
    for y in range(third.size[1]):
        v = 255 if y < CUT - 4 else max(0, int(255 * (CUT + 6 - y) / 10))
        for x in range(600, 840):
            rp[x, y] = v
    top = ImageChops.multiply(top.filter(ImageFilter.GaussianBlur(2)), ramp)

    f2, f3 = grow(face_region(warped), OUTLINE), grow(face_region(third), OUTLINE)
    low = Image.new("L", third.size, 0)
    ImageDraw.Draw(low).rectangle([0, CUT - 8, third.size[0], third.size[1]], fill=255)
    lower = ImageChops.multiply(f2, low).filter(ImageFilter.GaussianBlur(0.8))
    jaw = ImageChops.multiply(ImageChops.subtract(f3, f2), low).filter(ImageFilter.MaxFilter(3)).filter(ImageFilter.GaussianBlur(0.8))
    mask = ImageChops.multiply(ImageChops.lighter(ImageChops.lighter(top, lower), jaw), warped.getchannel("A"))

    out = Image.composite(warped, third, mask)
    out.putalpha(third.getchannel("A"))
    OUT.parent.mkdir(parents=True, exist_ok=True)
    out.save(OUT)
    print(OUT)


if __name__ == "__main__":
    main()
