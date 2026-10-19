# -*- coding: utf-8 -*-
"""Round 85: the stars of a star item while the pointer is on it, in battle (2026-10-19 사용자 플레이 피드백 "전투 중 아이템에 마우스
호버(올릴 경우) 숫돌 별 같은 효과 있는 아이템은 현재 별 표시" → 권장안 "목업 한 장 먼저"). Drawn over the 2026-10-19 screenshots of
20261019-traits37c: base-battle.png = ko_55 (a battle), base-map-stars.png = ko_46 (the node map, the pointer on Rowan's whetstone).
Rowan's board of the map is moved into the battle at the same place (the boards stand where they stand on the map). Shapes only — no API call.
  .venv/bin/python ArtPipeline/Archive/85-battle-stars/mock_battle_stars.py
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
FONT = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"

BOARD = (700, 643, 861, 803)       # Rowan's board inside its frame (the same on the map and in battle)
SHIFT = 190                         # Kai's board is one column of boards to the left
TIP = (796, 645, 858, 697)          # the longsword's tip under the lit star (with the star's glow)
CORNER = (806, 749, 858, 801)       # the empty star square
CURSOR = (834, 716)
LIT_AT = (831, 670)               # where the map draws the lit star


def font(size):
    return ImageFont.truetype(str(FONT), size)


def caption(image, text):
    draw = ImageDraw.Draw(image)
    draw.rectangle((0, 1016, 1920, 1080), fill=(0, 0, 0))
    draw.text((24, 1030), text, font=font(30), fill=(0xEB, 0xEB, 0xE6))


def cursor(image, at):
    x, y = at
    shape = [(x, y), (x, y + 34), (x + 9, y + 26), (x + 16, y + 41), (x + 22, y + 38), (x + 15, y + 24), (x + 26, y + 24)]
    draw = ImageDraw.Draw(image)
    draw.polygon(shape, fill=(255, 255, 255), outline=(0, 0, 0))
    draw.line(shape + [shape[0]], fill=(0, 0, 0), width=2)


def charge(image, box, filled):
    """The battle's cooldown look: the icon shows its colour from the left as it charges; the rest is dark."""
    left, top, right, bottom = box
    split = int(left + (right - left) * filled)
    region = image.crop((split, top, right, bottom))
    pixels = region.load()
    for y in range(region.height):
        for x in range(region.width):
            r, g, b = pixels[x, y]
            if b > r + 30 and b > g + 25:
                continue  # the square's blue: only the icon darkens
            pixels[x, y] = (int(r * 0.42), int(g * 0.42), int(b * 0.42))
    image.paste(region, (split, top))


def lit_star(image, at, radius=13):
    """A lit star as the board draws it (UiPalette.StarLit with UiArt.Glow behind it)."""
    import math
    x, y = at
    glow = Image.new("L", image.size, 0)
    ImageDraw.Draw(glow).ellipse((x - 24, y - 24, x + 24, y + 24), fill=100)
    glow = glow.filter(ImageFilter.GaussianBlur(10))
    image.paste(Image.new("RGB", image.size, (0xEC, 0xC4, 0x5C)), (0, 0), glow)
    points = []
    for i in range(10):
        r = radius if i % 2 == 0 else radius * 0.45
        a = -math.pi / 2 + i * math.pi / 5
        points.append((x + r * math.cos(a), y + r * math.sin(a)))
    draw = ImageDraw.Draw(image)
    draw.polygon(points, fill=(0xEC, 0xC4, 0x5C), outline=(0x3A, 0x2A, 0x10))


def main():
    battle = Image.open(HERE / "base-battle.png").convert("RGB")
    stars = Image.open(HERE / "base-map-stars.png").convert("RGB")

    # The same board without the stars: the tip and the empty square come from Kai's board (the same longsword, an empty square).
    plain = stars.copy()
    for box in (TIP, CORNER):
        patch = stars.crop((box[0] - SHIFT, box[1], box[2] - SHIFT, box[3]))
        plain.paste(patch, box[:2])


    def in_battle(with_stars):
        image = battle.copy()
        board = plain.crop(BOARD)
        image.paste(board, BOARD[:2])
        charge(image, (705, 650, 855, 694), 0.6)   # longsword, 60% charged
        charge(image, (705, 702, 800, 745), 0.35)  # dagger, 35%
        if with_stars:
            # The star layer lies over the icons as on the map: the lit star (StarLit gold with the soft glow) over the charging longsword,
            # the empty star on its square as the map draws it.
            lit_star(image, LIT_AT)
            image.paste(stars.crop(CORNER), CORNER[:2])
        cursor(image, CURSOR)
        return image

    now = in_battle(False)
    caption(now, "지금 — 전투 중에는 숫돌에 마우스를 올려도 별이 없다 (우클릭 카드의 \"걸린 ★ 1/2 — 롱소드\"로만 안다)")
    now.save(HERE / "mock-now.png")

    proposed = in_battle(True)
    caption(proposed, "안 (권장) — 올린 동안 숫돌의 별: 걸린 별 금빛(롱소드 끝) · 빈 별(오른쪽 아래). 내리면 사라짐. 노드 맵과 같은 모양")
    proposed.save(HERE / "mock-stars.png")

    # Side by side, the boards at twice the size: the node map now, the battle now, the battle as proposed.
    region = (690, 620, 872, 810)
    scale = 2
    pointed = stars.copy()
    cursor(pointed, CURSOR)
    panels = [("노드 맵 (지금)", pointed), ("전투 (지금)", now), ("전투 (안)", proposed)]
    w, h = (region[2] - region[0]) * scale, (region[3] - region[1]) * scale
    sheet = Image.new("RGB", (len(panels) * (w + 40) + 40, h + 130), (0x16, 0x16, 0x1C))
    draw = ImageDraw.Draw(sheet)
    for i, (title, source) in enumerate(panels):
        x = 40 + i * (w + 40)
        crop = source.crop(region).resize((w, h), Image.LANCZOS)
        sheet.paste(crop, (x, 70))
        draw.text((x, 20), title, font=font(30), fill=(0xEB, 0xEB, 0xE6))
    draw.text((40, h + 84), "로언의 보드: 롱소드 · 단검 · 숫돌 · 약초 주머니. 전투의 아이콘은 쿨다운만큼 왼쪽부터 밝다(롱소드 60%, 단검 35%).", font=font(24), fill=(0x9A, 0xA0, 0xAC))
    sheet.save(HERE / "compare.png")


if __name__ == "__main__":
    main()
