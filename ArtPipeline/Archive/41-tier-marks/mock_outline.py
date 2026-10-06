"""Round 41, second mockup: the tier as an extra OUTLINE around the item's icon in the tier's colour, thicker at a higher tier
(the user's idea), instead of a frame on the cell. Three ladders of thickness, over the game's screenshots and at twice the
size. The outline is the union of the icon's silhouette shifted around a circle, which is also how it can be drawn at runtime.
No API call.
  .venv/bin/python ArtPipeline/Archive/41-tier-marks/mock_outline.py
"""
import math
import sys
from pathlib import Path
from PIL import Image, ImageChops, ImageDraw, ImageFilter

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
from mock_tier_marks import (AA, BATTLE, CELL_H, CELL_W, CROP, GAME, ICON_MX, ICON_MY, ICONS, INK, KO, PARTY, RIM, TEXT, Layer,  # noqa: E402
                             fatigue_tag, font, old_badge, outer, repaint, sheet, star_tag, thin_rim)

# Thickness of the outline on screen (px) per tier, per ladder.
LADDER = {"A": {"Silver": 2, "Gold": 3, "Diamond": 4}, "B": {"Silver": 2, "Gold": 4, "Diamond": 6}, "C": {"Silver": 2, "Gold": 3, "Diamond": 4},
          "AS": {"Silver": 2, "Gold": 3, "Diamond": 4}}                 # AS: the outline of A plus the star tag at the bottom-left (2026-10-07)
GLOW = {"A": 0, "B": 0, "C": 3, "AS": 0}                           # ladder C adds a soft light of the same colour around the outline (px)
NAMES = {"now": "지금: 등급 배지 + 가는 테(Round 35 A)", "A": "안 A: 아이콘 외곽선 2 · 3 · 4", "B": "안 B: 아이콘 외곽선 2 · 4 · 6 (더 굵게)", "C": "안 C: 외곽선 2 · 3 · 4 + 바깥 빛"}


def disk_dilate(alpha, r):
    """The alpha mask grown by r pixels: the union of the mask shifted around circles of radius r and r/2 (and the mask itself)."""
    if r <= 0: return alpha
    out = alpha.copy()
    for radius in (r, r / 2):
        n = max(8, int(2 * math.pi * radius / 1.2))
        for i in range(n):
            a = 2 * math.pi * i / n
            out = ImageChops.lighter(out, ImageChops.offset(alpha, int(round(radius * math.cos(a))), int(round(radius * math.sin(a)))))
    return out


def icon_with_outline(item, tier, how, u):
    """The icon as the cell shows it, at u pixels per screen pixel, with the tier's outline (and light) behind it. Returns the
    RGBA picture of the icon's area (the cell inside its margins)."""
    aw, ah = (CELL_W - 2 * ICON_MX) * u, (CELL_H - 2 * ICON_MY) * u
    src = Image.open(ICONS / f"{item}.png").convert("RGBA")
    s = min(aw / src.width, ah / src.height)
    icon = src.resize((max(1, int(round(src.width * s))), max(1, int(round(src.height * s)))), Image.LANCZOS)
    pad = 12 * u                                           # room for the thickest outline and its light
    area = Image.new("RGBA", (aw + 2 * pad, ah + 2 * pad), (0, 0, 0, 0))
    ox, oy = pad + (aw - icon.width) // 2, pad + (ah - icon.height) // 2
    if tier != "Bronze" and how in LADDER:
        mask = Image.new("L", area.size, 0); mask.paste(icon.split()[3], (ox, oy))
        r = LADDER[how][tier] * u
        grown = disk_dilate(mask, r)
        if GLOW[how]:
            light = grown.filter(ImageFilter.GaussianBlur(GLOW[how] * u)).point(lambda v: int(v * 0.55))
            area.alpha_composite(Image.composite(Image.new("RGBA", area.size, TEXT[tier] + (255,)), Image.new("RGBA", area.size, (0, 0, 0, 0)), light))
        area.alpha_composite(Image.composite(Image.new("RGBA", area.size, RIM[tier] + (255,)), Image.new("RGBA", area.size, (0, 0, 0, 0)), grown))
    area.alpha_composite(icon, (ox, oy))
    return area, pad


def cell_layer(how, item, tier, k, grade=None, fatigue=False, merge_into=None):
    """One cell's icon and marks on a layer at k*AA: the outline variants, or the game's present look."""
    L = Layer(k); u = L.u
    shown = merge_into or tier
    if how == "now" and shown != "Bronze": thin_rim(L, shown)
    area, pad = icon_with_outline(item, shown, how, u)
    L.img.alpha_composite(area, (ICON_MX * u - pad, ICON_MY * u - pad))
    if merge_into:
        # The veil is composited, not drawn: drawing would replace the icon's pixels on the layer.
        veil = Image.new("RGBA", L.img.size, (0, 0, 0, 0))
        ImageDraw.Draw(veil).rounded_rectangle(L.box(3, 3, CELL_W - 3, CELL_H - 3), radius=3 * u, fill=(20, 22, 30, 150))
        L.img.alpha_composite(veil)
        L.text((CELL_W / 2, CELL_H / 2), f"합치기 → {KO[merge_into]}", 20, TEXT[merge_into] + (255,))
    if how == "now" and grade is not None and not merge_into: old_badge(L, grade)
    if how == "AS" and shown != "Bronze": star_tag(L, shown)
    if fatigue: fatigue_tag(L)
    return L


def party_side(how):
    img = Image.open(GAME / "ko_30_map_fatigue.png").convert("RGBA")
    for (col, i), (item, tier, grade, fat) in PARTY.items():
        at = outer(col, i)
        repaint(img, at)
        cell_layer(how, item, tier, 1, grade, fat).lay(img, at)
    return img.crop(CROP)


BATTLE_ITEMS = {(3, 0): "longsword", (2, 0): "longsword", (1, 0): "healing_staff", (0, 0): "fire_staff"}


def luma(p): return 0.299 * p[0] + 0.587 * p[1] + 0.114 * p[2]


def battle(how):
    """In battle the dark of the uncharged part lies over the icon, so over the outline too: the layer is dimmed right of the
    charge's front, by the same amount the screenshot's bone is."""
    img = Image.open(GAME / "ko_05_battle.png").convert("RGBA")
    for (col, i), tier in BATTLE.items():
        at = outer(col, i); x0, y0 = at
        row = [img.getpixel((x0 + x, y0 + 3)) for x in range(3, CELL_W - 3)]
        split = next((x for x, p in enumerate(row) if luma(p) < 120), None)
        L = cell_layer(how, BATTLE_ITEMS[(col, i)], tier, 1)
        if split is not None and split > 0:
            lit = [sum(p[c] for p in row[:split]) / split for c in range(3)]
            dark_px = row[split:]
            dark = [sum(p[c] for p in dark_px) / len(dark_px) for c in range(3)]
            f = [min(1.0, dark[c] / max(1.0, lit[c])) for c in range(3)]
            px = L.img.load(); xs = (split + 3) * L.u
            for y in range(L.img.height):
                for x in range(xs, L.img.width):
                    r, g, b, a = px[x, y]
                    if a: px[x, y] = (int(r * f[0]), int(g * f[1]), int(b * f[2]), a)
        L.lay(img, at)
    return img.crop(CROP)


def detail_cell(how, tier, item="dagger", merge_into=None, k=2, fatigue=True):
    img = Image.new("RGBA", (CELL_W * k, CELL_H * k), (0x2C, 0x26, 0x27, 255))
    ImageDraw.Draw(img).rounded_rectangle((0, 0, CELL_W * k - 1, CELL_H * k - 1), radius=5 * k, fill=(72, 60, 50, 255))
    repaint(img, (0, 0), k)
    cell_layer(how, item, tier, k, 8, fatigue, merge_into).lay(img, (0, 0))
    return img


def main():
    entries = []
    for how in ("now", "A", "B", "C"):
        entries.append((NAMES[how] + " — 파티 쪽", party_side(how)))
        entries.append(("— 전투", battle(how)))
    sheet(entries, 2, HERE / "mock2-compare.png",
          footer=["스테이징은 첫 목업과 같다(로언 롱소드 동, 단검 은, 버클러 금, 약초 주머니 다이아 / 카이 롱소드 금 / 미라 지팡이 은. 전투는 로언 은, 카이 금, 미라 다이아). 동은 표시 없음, 등급 배지 없음.",
                  "외곽선은 아이콘의 먹선 바깥에 단계 색으로 두른 것이고, 전투에서는 충전되지 않은 부분의 어둠이 아이콘과 함께 외곽선도 덮는다(지금의 가는 테는 어둠 위에 있어 늘 보였다)."])
    det = []
    for how in ("now", "A", "B", "C"):
        for tier in ("Silver", "Gold", "Diamond"):
            det.append((f"{NAMES[how].split(':')[0]} · {KO[tier]}", detail_cell(how, tier)))
        det.append((f"{NAMES[how].split(':')[0]} · 합치기 → 은", detail_cell(how, "Bronze", merge_into="Silver", fatigue=False)))
    sheet(det, 4, HERE / "mock2-detail.png", lab=36, gap=18, size=20, footer=["2배 확대. 넷째 열은 고른 아이템이 합쳐질 칸: 다음 단계(은)의 외곽선을 미리 두르고 어두운 막 위에 글."])
    shapes = []
    for item in ("dagger", "buckler", "herb_pouch", "fire_staff"):
        for tier in ("Silver", "Gold", "Diamond"):
            shapes.append((f"안 A · {item} · {KO[tier]}", detail_cell("A", tier, item=item, fatigue=False)))
    sheet(shapes, 3, HERE / "mock2-shapes.png", lab=36, gap=18, size=20, footer=["안 A의 외곽선이 모양이 다른 아이콘(긴 것, 둥근 것, 작은 것)을 어떻게 두르는지. 2배 확대."])


if __name__ == "__main__":
    main()
