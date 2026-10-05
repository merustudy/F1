"""The second proposal for the units' plates (round 29), after a look at Darkest Dungeon and Slay the Spire, no API call.
  ArtPipeline/Archive/29-plates/run_clean_shots.sh <scratch>        (first: the game's screenshots without the plates)
  .venv/bin/python ArtPipeline/Archive/29-plates/mock_plates2.py    # mock2-compare.png, mock2-deathsdoor.png, mock2-states.png

The user turned down the first three ("다 별로 마음에 안드네") and asked for a look at Darkest Dungeon, Slay the Spire and
similar games, one proposal without the panel behind. None of those games puts a panel under a unit: Darkest Dungeon draws
a thin health bar (and stress) at the feet with the status tokens under it, numbers and name on hover; Slay the Spire a red
bar with "current/max" on it, block as a blue badge at its left, the buffs and debuffs in a row under it, names on hover.
The stage is the game's own without its plates (run_clean_shots.sh: the same moments with the plates hidden in a clone):
- 1 Darkest Dungeon's way (no panel): a thin bar and the tokens. Name, row and numbers when the unit is pointed at or pressed
  (one tooltip is shown, on Cedric).
- 2 Slay the Spire's way (no panel): a bar with the numbers on it; the shield or death's door as a badge at its left end, the
  bar's rim in that colour; the other states in a row under it.
- 3 A small name tag: a slim iron tag with the row and the name (and the numbers, dim) and a thin bar under it, the states
  under that. About 60% of the plate's height.
The pieces are the game's (trough, state icons) and this round's samples (bar29_thin, tag29_slim, badge29_a_stud).
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
FONT = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"
FRAME = ROOT / "Assets/@Art/UI/Frame"
ICON = ROOT / "Assets/@Art/UI/Icon"
UI = ROOT / "ArtPipeline/output"
GAME = HERE / "game"

TEXT, INK = (0xEB, 0xEB, 0xE6), (0x14, 0x0C, 0x0A)
BLOOD, BLOOD_LIGHT = (0x8A, 0x16, 0x18), (0xD8, 0x6A, 0x5A)
SHIELD_RIM, DANGER_RIM, TARGET_GLOW = (110, 168, 232), (226, 60, 48), (110, 170, 255)
SHIELD, BURN, DANGER_TEXT = (0x86, 0xB4, 0xE0), (0xF0, 0x9A, 0x48), (0xF2, 0x8C, 0x7E)
FLOOR = 506
Z = 2


def font(size):
    return ImageFont.truetype(str(FONT), size)


def three(piece, size):
    """A piece stretched only along its length: scaled to the height, its two ends kept, its middle stretched."""
    w, h = size
    k = h / piece.height
    piece = piece.resize((max(1, round(piece.width * k)), h), Image.LANCZOS)
    end = min(piece.width // 2, round(h * 1.1))
    out = Image.new("RGBA", size, (0, 0, 0, 0))
    out.alpha_composite(piece.crop((0, 0, end, h)), (0, 0))
    out.alpha_composite(piece.crop((piece.width - end, 0, piece.width, h)), (w - end, 0))
    out.alpha_composite(piece.crop((end, 0, piece.width - end, h)).resize((max(1, w - 2 * end), h), Image.LANCZOS), (end, 0))
    return out


def word(d, xy, s, size, colour, anchor="lm", stroke=2):
    d.text(xy, s, font=font(size), fill=colour, anchor=anchor, stroke_width=stroke, stroke_fill=INK)


def icon(name, size):
    return Image.open(ICON / f"{name}.png").convert("RGBA").resize((size, size), Image.LANCZOS)


def bar(layer, box, frame, hp, max_hp, inset, rim=None, ghost=0.0):
    """A bar: the frame stretched along its length, the blood inside it, a lighter trail of the HP just lost."""
    x0, y0, x1, y1 = box
    piece = Image.open(frame).convert("RGBA")
    layer.alpha_composite(three(piece, (x1 - x0, y1 - y0)), (x0, y0))
    d = ImageDraw.Draw(layer)
    ix0, iy0, ix1, iy1 = x0 + inset[0], y0 + inset[1], x1 - inset[0], y1 - inset[1]
    full = ix1 - ix0
    now = ix0 + round(full * max(0.0, min(1.0, hp / max_hp)))
    if ghost > 0:
        d.rectangle((now, iy0, min(ix1, now + round(full * ghost)), iy1), fill=BLOOD_LIGHT)
    if now > ix0:
        d.rectangle((ix0, iy0, now, iy1), fill=BLOOD)
    if rim:
        d.rounded_rectangle((x0 - 2 * Z, y0 - 2 * Z, x1 + 2 * Z, y1 + 2 * Z), radius=6 * Z, outline=rim, width=2 * Z)


def tokens(layer, x, y, chips, size, text_size):
    d = ImageDraw.Draw(layer)
    for art, words, colour in chips:
        layer.alpha_composite(icon(art, size), (x, y))
        x += size + 3 * Z
        word(d, (x, y + size // 2), words, text_size, colour)
        x += int(d.textlength(words, font=font(text_size))) + 10 * Z


def glow(layer, cx, colour):
    """The potion's target: a soft light on the floor at the feet."""
    g = Image.new("RGBA", layer.size, (0, 0, 0, 0))
    ImageDraw.Draw(g).ellipse((cx - 90 * Z, (FLOOR - 16) * Z, cx + 90 * Z, (FLOOR + 14) * Z), fill=colour + (150,))
    layer.alpha_composite(g.filter(ImageFilter.GaussianBlur(9 * Z)))


def draw_unit(layer, style, cx, unit, state):
    """One unit's marks at twice the size, cx in the layer's pixels."""
    d = ImageDraw.Draw(layer)
    chips = unit.get("states", [])
    if state == "target":
        glow(layer, cx, TARGET_GLOW)
    if style == 1:
        w, top = 150 * Z, (FLOOR + 9) * Z
        rim = DANGER_RIM if state == "danger" else (TARGET_GLOW if state == "target" else None)
        bar(layer, (cx - w // 2, top, cx + w // 2, top + 12 * Z), UI / "ui_frame/bar29_thin.png", unit["hp"], unit["max"], (7 * Z, 3 * Z), rim, unit.get("ghost", 0))
        if state == "danger":
            chips = [("deaths_door", unit["dog"], TEXT)]
        tokens(layer, cx - w // 2 + 2 * Z, top + 16 * Z, chips, 18 * Z, 13 * Z)
        if unit.get("tooltip"):
            tip = (cx - 84 * Z, top + 40 * Z, cx + 84 * Z, top + 66 * Z)
            layer.alpha_composite(three(Image.open(UI / "ui_frame/tag29_slim.png").convert("RGBA"), (tip[2] - tip[0], tip[3] - tip[1])), tip[:2])
            word(d, ((tip[0] + tip[2]) // 2, (tip[1] + tip[3]) // 2), f"{unit['row']}열 {unit['name']} · {unit['hp']}/{unit['max']}", 13 * Z, TEXT, "mm")
    elif style == 2:
        w, top = 160 * Z, (FLOOR + 8) * Z
        rim = {"danger": DANGER_RIM, "target": TARGET_GLOW}.get(state)
        badge = None
        if state == "danger":
            badge = ("deaths_door", "")
        elif any(c[0] == "shield" for c in chips):
            badge = ("shield", next(c[1] for c in chips if c[0] == "shield"))
            rim = rim or SHIELD_RIM
            chips = [c for c in chips if c[0] != "shield"]
        bar(layer, (cx - w // 2, top, cx + w // 2, top + 22 * Z), FRAME / "trough.png", unit["hp"], unit["max"], (6 * Z, 5 * Z), rim, unit.get("ghost", 0))
        word(d, (cx, top + 11 * Z), f"{unit['hp']}/{unit['max']}", 15 * Z, TEXT, "mm")
        if badge:
            bx, by = cx - w // 2 - 20 * Z, top - 7 * Z
            layer.alpha_composite(icon(badge[0], 36 * Z), (bx, by))
            if badge[1]:
                word(d, (bx + 18 * Z, by + 19 * Z), badge[1], 15 * Z, TEXT, "mm")
        if state == "danger":
            chips = [("deaths_door", unit["dog"], TEXT)]
        tokens(layer, cx - w // 2 + 6 * Z, top + 26 * Z, chips, 18 * Z, 13 * Z)
    else:
        w, top = 150 * Z, (FLOOR + 6) * Z
        tag = Image.open(UI / "ui_frame/tag29_slim.png").convert("RGBA")
        tag = three(tag, (w, 24 * Z))
        if state in ("danger", "target"):
            stain = (98, 26, 24) if state == "danger" else (40, 62, 96)
            px = tag.load()
            for yy in range(tag.height):
                for xx in range(tag.width):
                    r, g, b, a = px[xx, yy]
                    if a and max(r, g, b) - min(r, g, b) < 30 and 24 < (r + g + b) / 3 < 95:
                        k = (r + g + b) / 3 / 50
                        px[xx, yy] = (min(255, int(stain[0] * k)), min(255, int(stain[1] * k)), min(255, int(stain[2] * k)), a)
        layer.alpha_composite(tag, (cx - w // 2, top))
        stud = Image.open(UI / "ui_piece/badge29_a_stud.png").convert("RGBA").resize((20 * Z, 20 * Z), Image.LANCZOS)
        layer.alpha_composite(stud, (cx - w // 2 + 4 * Z, top + 2 * Z))
        word(d, (cx - w // 2 + 14 * Z, top + 12 * Z), str(unit["row"]), 12 * Z, INK, "mm", stroke=0)
        # The name and the numbers share the tag: the numbers, then the name, get smaller when they would meet.
        numbers, room = f"{unit['hp']}/{unit['max']}", w - 29 * Z - 7 * Z - 6 * Z
        name_size, number_size = 14 * Z, 11 * Z
        while number_size > 8 * Z and d.textlength(unit["name"], font=font(name_size)) + d.textlength(numbers, font=font(number_size)) > room:
            number_size -= 1
        while name_size > 10 * Z and d.textlength(unit["name"], font=font(name_size)) + d.textlength(numbers, font=font(number_size)) > room:
            name_size -= 1
        word(d, (cx - w // 2 + 29 * Z, top + 12 * Z), unit["name"], name_size, TEXT)
        word(d, (cx + w // 2 - 7 * Z, top + 12 * Z), numbers, number_size, (0xB8, 0xB4, 0xA8), "rm")
        bar(layer, (cx - w // 2, top + 27 * Z, cx + w // 2, top + 37 * Z), UI / "ui_frame/bar29_thin.png", unit["hp"], unit["max"], (6 * Z, 2 * Z), None, unit.get("ghost", 0))
        if state == "danger":
            chips = [("deaths_door", unit["dog"], TEXT)]
        tokens(layer, cx - w // 2 + 2 * Z, top + 40 * Z, chips, 16 * Z, 12 * Z)


SHIELD3 = [("shield", "3", SHIELD)]
SCENES = {
    "ko_15": {
        "party": [(215, {"row": 4, "name": "카이", "hp": 100, "max": 100}),
                  (405, {"row": 3, "name": "엘라", "hp": 90, "max": 90}),
                  (595, {"row": 2, "name": "세드릭", "hp": 130, "max": 130, "states": SHIELD3, "tooltip": True}),
                  (785, {"row": 1, "name": "아스트리드", "hp": 99985, "max": 100000, "states": [("burn", "3", BURN)]})],
        "enemy": [(1225, {"row": 1, "name": "고블린 약탈자", "hp": 80, "max": 80}),
                  (1415, {"row": 2, "name": "고블린 주술사", "hp": 60, "max": 60})],
        "danger": [],
    },
    "ko_18": {
        "party": [(215, {"row": 4, "name": "카이", "hp": 1, "max": 100}),
                  (405, {"row": 3, "name": "엘라", "hp": 1, "max": 90}),
                  (595, {"row": 2, "name": "세드릭", "hp": 1, "max": 130})],
        "enemy": [(cx, {"row": i + 1, "name": "동굴 쥐", "hp": 40, "max": 40}) for i, cx in enumerate((1135, 1325, 1515, 1705))],
        "danger": [(785, {"row": 1, "name": "아스트리드", "hp": 0, "max": 120, "dog": "유예 3.0초 · 피격 1/3"})],
    },
}
STYLES = [(1, "1 — 다키스트 던전식(패널 없음): 얇은 막대와 상태 토큰. 이름·열·숫자는 누르거나 올리면(세드릭)"),
          (2, "2 — 슬레이 더 스파이어식(패널 없음): 숫자가 얹힌 막대, 보호막·빈사는 왼쪽 끝 배지와 테두리 색"),
          (3, "3 — 작은 이름표: 열·이름·숫자의 가는 쇠 띠와 그 아래 얇은 막대(지금 높이의 약 60%)")]


def scene(name, style):
    base = Image.open(GAME / f"{name}_noplate.png").convert("RGBA")
    layer = Image.new("RGBA", (base.width * Z, base.height * Z), (0, 0, 0, 0))
    s = SCENES[name]
    for key, state in (("party", "party"), ("enemy", "enemy"), ("danger", "danger")):
        for cx, unit in s[key]:
            draw_unit(layer, style, cx * Z, unit, state)
    base.alpha_composite(layer.resize(base.size, Image.LANCZOS))
    return base.convert("RGB")


def compare(name, out, now_title):
    f = font(26)
    rows = [("지금 — " + now_title, Image.open(GAME / f"{name}_now.png").convert("RGB"))] + [(t, scene(name, s)) for s, t in STYLES]
    bands = []
    for title, shot in rows:
        crop = shot.crop((100, 330, 1820 if name == "ko_18" else 1540, 640))
        band = Image.new("RGB", (crop.width, crop.height + 44), (20, 18, 18))
        band.paste(crop, (0, 44))
        ImageDraw.Draw(band).text((14, 8), title, fill=(235, 235, 230) if title.startswith("지금") else (217, 164, 65), font=f)
        bands.append(band)
    sheet = Image.new("RGB", (bands[0].width, sum(b.height + 8 for b in bands)), (12, 12, 12))
    y = 0
    for b in bands:
        sheet.paste(b, (0, y))
        y += b.height + 8
    sheet.save(HERE / out)
    return sheet.size


def states():
    """Each proposal in four states at twice the size, over the floor under Cedric (ko_15 without the plates)."""
    base = Image.open(GAME / "ko_15_noplate.png").convert("RGBA")
    patch = base.crop((595 - 120, 400, 595 + 120, 600)).resize((240 * Z, 200 * Z), Image.LANCZOS)
    cases = [("아군 (보호막)", "party", {"row": 2, "name": "세드릭", "hp": 104, "max": 130, "states": SHIELD3, "ghost": 0.08}),
             ("아군 · 빈사", "danger", {"row": 1, "name": "아스트리드", "hp": 0, "max": 120, "dog": "유예 3.0초 · 피격 1/3"}),
             ("포션 대상", "target", {"row": 3, "name": "엘라", "hp": 52, "max": 90}),
             ("적 (화상)", "enemy", {"row": 1, "name": "고블린 약탈자", "hp": 62, "max": 80, "states": [("burn", "2", BURN)]})]
    cell_w, cell_h = patch.width + 20, patch.height + 20
    sheet = Image.new("RGB", (300 + len(cases) * cell_w, 56 + len(STYLES) * cell_h), (20, 18, 18))
    d = ImageDraw.Draw(sheet)
    for j, (label, _, _) in enumerate(cases):
        d.text((300 + j * cell_w + 10, 14), label, fill=(235, 235, 230), font=font(28))
    for i, (style, title) in enumerate(STYLES):
        d.text((16, 56 + i * cell_h + cell_h // 2), title.split(" — ")[0] + " " + title.split(" — ")[1].split("(")[0].split(":")[0], fill=(217, 164, 65), font=font(26), anchor="lm")
        for j, (_, state, unit) in enumerate(cases):
            cell = patch.copy()
            layer = Image.new("RGBA", cell.size, (0, 0, 0, 0))
            # the patch's own coordinates: the unit's column in the middle, the floor line where it is on the screen
            shift = Image.new("RGBA", ((595 + 120) * Z, 650 * Z), (0, 0, 0, 0))
            draw_unit(shift, style, 595 * Z, unit, state)
            layer.alpha_composite(shift.crop(((595 - 120) * Z, 400 * Z, (595 + 120) * Z, 600 * Z)))
            cell.alpha_composite(layer)
            sheet.paste(cell.convert("RGB"), (300 + j * cell_w + 10, 56 + i * cell_h + 10))
    sheet.save(HERE / "mock2-states.png")
    return sheet.size


def main():
    for style, _ in STYLES:
        scene("ko_15", style).save(HERE / f"mock2-{style}.png")
    print("mock2-compare.png", compare("ko_15", "mock2-compare.png", "판 전체를 편의 색으로 칠한 명패"))
    print("mock2-deathsdoor.png", compare("ko_18", "mock2-deathsdoor.png", "빈사(1열 아스트리드)"))
    print("mock2-states.png", states())


if __name__ == "__main__":
    main()
