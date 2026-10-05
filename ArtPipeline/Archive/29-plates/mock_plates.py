"""Mockups of the units' plates (row, name, HP, states) drawn again to match the art of now (round 29), no API call.
  .venv/bin/python ArtPipeline/Archive/29-plates/mock_plates.py   # mock-compare.png, mock-states.png, mock-<now|A|B|C>.png

The battle screenshot game/ko_15_now.png (floor 4 after the enemy's advance: Cedric with a shield, Astrid burning) with every
plate drawn again by this script at the game's places (the plate is its column, 180x92 under the figure), at twice the size
and halved as the game halves its art. "Now" is drawn from the game's own pieces with the same code, so the four rows compare
alike. The pieces of A, B and C are this round's samples (frames.csv, pieces.csv; output/ui_frame, output/ui_piece):
- A: a plate of blackened iron with a gold line and an enamel band at its left end. The band carries the row (a gold stud)
  and the side's colour; the plate itself stays iron. Name, HP and states stand right of the band.
- B: a tablet of carved stone with bone at its corners, the stone stained with the side's colour; a bone token for the row.
- C: a strap of worn leather with stitches; the row on a wax seal of the side's colour. The leather is the same for both sides.
The HP trough, its blood and the state icons are the game's own in all four.
"""
import colorsys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
FONT = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"
FRAME = ROOT / "Assets/@Art/UI/Frame"
ICON = ROOT / "Assets/@Art/UI/Icon"
OUT_UI = ROOT / "ArtPipeline/output"
SHOT = HERE / "game/ko_15_now.png"

TEXT, TEXT_DIM, INK = (0xEB, 0xEB, 0xE6), (0x9A, 0xA0, 0xAC), (0x1A, 0x10, 0x0C)
BLOOD, BLOOD_LIGHT, BRASS = (0x8A, 0x16, 0x18), (0xD8, 0x6A, 0x5A), (0xB8, 0x8E, 0x48)
SHIELD, BURN = (0x86, 0xB4, 0xE0), (0xF0, 0x9A, 0x48)
PLATE_W, PLATE_H, PLATE_TOP = 180, 92, 512
SIDE = {"party": "plate_party", "enemy": "plate_enemy", "danger": "plate_danger", "target": "plate_target"}
# The colour of a side in A's enamel, B's stone and C's wax.
ENAMEL = {"party": (44, 74, 112), "enemy": (116, 50, 42), "danger": (178, 46, 38), "target": (64, 108, 164)}
STONE = {"party": (52, 64, 84), "enemy": (82, 52, 46), "danger": (118, 38, 34), "target": (62, 86, 118)}
WAX = {"party": (52, 86, 142), "enemy": (146, 40, 36), "danger": (196, 48, 40), "target": (82, 128, 186)}
IRON_STAIN = {"danger": (92, 26, 24), "target": (40, 60, 92)}


def font(size):
    return ImageFont.truetype(str(FONT), size)


def nine(piece, size, border):
    w, h = size
    pw, ph = piece.size
    b = border
    out = Image.new("RGBA", size, (0, 0, 0, 0))
    cols = [(0, b, 0, b), (b, pw - b, b, w - b), (pw - b, pw, w - b, w)]
    rows = [(0, b, 0, b), (b, ph - b, b, h - b), (ph - b, ph, h - b, h)]
    for sx0, sx1, dx0, dx1 in cols:
        for sy0, sy1, dy0, dy1 in rows:
            if dx1 > dx0 and dy1 > dy0:
                out.alpha_composite(piece.crop((sx0, sy0, sx1, sy1)).resize((dx1 - dx0, dy1 - dy0), Image.LANCZOS), (dx0, dy0))
    return out


def recolour(image, test, target, gain=1.0):
    """Moves the pixels that pass the test to the target colour, keeping their lightness against the mean of them."""
    out = image.copy()
    px = out.load()
    picked = [(x, y) for y in range(out.height) for x in range(out.width) if px[x, y][3] > 0 and test(*px[x, y][:3])]
    if not picked:
        return out
    mean = sum(sum(px[p][:3]) / 3 for p in picked) / len(picked)
    for p in picked:
        r, g, b, a = px[p]
        k = (r + g + b) / 3 / mean * gain
        px[p] = (min(255, int(target[0] * k)), min(255, int(target[1] * k)), min(255, int(target[2] * k)), a)
    return out


def blueish(r, g, b):
    return b > r + 18 and b > g + 4


def greyish(r, g, b):
    h, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
    return s < 0.22 and 0.18 < v < 0.85


def reddish(r, g, b):
    return r > g + 40 and r > b + 30


def text(d, box, s, size, colour, anchor="lm", stroke=0):
    x0, y0, x1, y1 = box
    x = x0 if anchor[0] == "l" else (x0 + x1) / 2
    d.text((x, (y0 + y1) / 2), s, font=font(size), fill=colour, anchor=anchor, stroke_width=stroke, stroke_fill=INK)


def trough(img, box, hp, max_hp):
    """The game's HP bar: the iron trough, the blood inside it (4 in), the numbers over it."""
    x0, y0, x1, y1 = box
    img.alpha_composite(nine(Image.open(FRAME / "trough.png").convert("RGBA"), (x1 - x0, y1 - y0), 30), (x0, y0))
    d = ImageDraw.Draw(img)
    inner = (x0 + 8, y0 + 8, x1 - 8, y1 - 8)
    fill = inner[0] + round((inner[2] - inner[0]) * max(0.0, min(1.0, hp / max_hp)))
    if fill > inner[0]:
        d.rectangle((inner[0], inner[1], fill, inner[3]), fill=BLOOD)
    text(d, box, f"{hp}/{max_hp}", 30, TEXT, "mm")


def states(img, x, y, chips, right):
    """The line of states: an icon (18) and its number or words (15), 10 apart, at twice the size. Words too long for the
    room up to `right` are drawn smaller (A's band leaves less room: the death's door line would need a smaller font)."""
    d = ImageDraw.Draw(img)
    for art, words, colour in chips:
        icon = Image.open(ICON / f"{art}.png").convert("RGBA").resize((36, 36), Image.LANCZOS)
        img.alpha_composite(icon, (x, y))
        x += 42
        size = 30
        while size > 22 and x + d.textlength(words, font=font(size)) > right:
            size -= 1
        d.text((x, y + 18), words, font=font(size), fill=colour, anchor="lm")
        x += int(d.textlength(words, font=font(size))) + 20


def plate(style, state, unit, width=PLATE_W):
    """One plate at twice the size: (width*2) x 184."""
    W, H = width * 2, PLATE_H * 2
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    left = 22                                                     # where the HP bar and the states start (2x)
    if style == "now":
        img.alpha_composite(nine(Image.open(FRAME / f"{SIDE[state]}.png").convert("RGBA"), (W, H), 44))
        d = ImageDraw.Draw(img)
        d.ellipse((22, 20, 74, 72), fill=INK)
        d.ellipse((28, 26, 68, 66), fill=BRASS)
        text(d, (22, 20, 74, 72), str(unit["row"]), 34, INK, "mm")
        name_x = 90
    elif style == "A":
        frame = recolour(nine(Image.open(OUT_UI / "ui_frame/plate29_a_iron.png").convert("RGBA"), (W, H), 44), blueish, ENAMEL[state], 1.15)
        if state in IRON_STAIN:
            # Death's door and the potion's target are warnings: the iron itself takes the colour, as the whole plate does now.
            frame = recolour(frame, lambda r, g, b: max(r, g, b) - min(r, g, b) < 30 and 24 < (r + g + b) / 3 < 95, IRON_STAIN[state])
        img.alpha_composite(frame)
        stud = Image.open(OUT_UI / "ui_piece/badge29_a_stud.png").convert("RGBA").resize((52, 52), Image.LANCZOS)
        img.alpha_composite(stud, (18, 18))
        d = ImageDraw.Draw(img)
        text(d, (18, 18, 70, 70), str(unit["row"]), 32, INK, "mm")
        name_x = left = 86
    elif style == "B":
        frame = Image.open(OUT_UI / "ui_frame/plate29_b_stone.png").convert("RGBA")
        img.alpha_composite(recolour(nine(frame, (W, H), 44), greyish, STONE[state]))
        token = Image.open(OUT_UI / "ui_piece/badge29_b_bone.png").convert("RGBA").resize((54, 54), Image.LANCZOS)
        img.alpha_composite(token, (22, 18))
        d = ImageDraw.Draw(img)
        text(d, (22, 18, 76, 72), str(unit["row"]), 32, INK, "mm")
        name_x = 92
    else:
        frame = Image.open(OUT_UI / "ui_frame/plate29_c_leather.png").convert("RGBA")
        frame = nine(frame, (W, H), 44)
        if state == "danger":
            frame = recolour(frame, lambda r, g, b: not greyish(r, g, b) and r > b, (110, 34, 28), 1.1)
        elif state == "target":
            frame = recolour(frame, lambda r, g, b: not greyish(r, g, b) and r > b, (96, 70, 52), 1.25)
        img.alpha_composite(frame)
        seal = Image.open(OUT_UI / "ui_piece/badge29_c_seal.png").convert("RGBA")
        seal = recolour(seal, reddish, WAX[state], 1.05).resize((62, 62), Image.LANCZOS)
        img.alpha_composite(seal, (16, 14))
        d = ImageDraw.Draw(img)
        text(d, (16, 14, 78, 76), str(unit["row"]), 32, TEXT, "mm", stroke=2)
        name_x = 92
    d = ImageDraw.Draw(img)
    text(d, (name_x, 20, W - 22, 72), unit["name"], 40, TEXT)
    trough(img, (left, 78, W - 22, 124), unit["hp"], unit["max"])
    if unit.get("states"):
        states(img, left + 4, 132, unit["states"], W - 20)
    return img


# The units of the screenshot, where their plates are (the x of the plate's left edge).
PARTY = [
    (125, {"row": 4, "name": "카이", "hp": 100, "max": 100}),
    (315, {"row": 3, "name": "엘라", "hp": 90, "max": 90}),
    (505, {"row": 2, "name": "세드릭", "hp": 130, "max": 130, "states": [("shield", "3", SHIELD)]}),
    (695, {"row": 1, "name": "아스트리드", "hp": 99985, "max": 100000, "states": [("burn", "3", BURN)]}),
]
ENEMIES = [
    (1135, {"row": 1, "name": "고블린 약탈자", "hp": 80, "max": 80}),
    (1325, {"row": 2, "name": "고블린 주술사", "hp": 60, "max": 60}),
]
STATES = [
    ("아군", "party", {"row": 2, "name": "세드릭", "hp": 104, "max": 130, "states": [("shield", "3", SHIELD)]}),
    ("아군 · 빈사", "danger", {"row": 1, "name": "아스트리드", "hp": 0, "max": 120, "states": [("deaths_door", "빈사 3.0초 · 피격 1/3", TEXT)]}),
    ("포션 대상", "target", {"row": 3, "name": "엘라", "hp": 52, "max": 90}),
    ("적", "enemy", {"row": 1, "name": "고블린 약탈자", "hp": 62, "max": 80, "states": [("burn", "2", BURN)]}),
]
STYLES = [("now", "지금 — 쇠 명패를 편의 색으로 칠함, 열 번호는 도형 원"), ("A", "A — 쇠 명패와 법랑 띠: 편의 색은 왼쪽 띠, 열 번호는 금 징"),
          ("B", "B — 돌판과 뼈 장식: 돌에 편의 색이 배어 듦, 열 번호는 뼈 패"), ("C", "C — 가죽 띠와 밀랍 봉인: 가죽은 같고 편의 색은 봉인")]


def battle(style):
    img = Image.open(SHOT).convert("RGBA")
    for units, state in ((PARTY, "party"), (ENEMIES, "enemy")):
        for x, unit in units:
            p = plate(style, state, unit).resize((PLATE_W, PLATE_H), Image.LANCZOS)
            img.alpha_composite(p, (x, PLATE_TOP))
    return img.convert("RGB")


def main():
    f = font(26)
    crops, shots = [], {}
    for style, title in STYLES:
        shot = battle(style)
        shot.save(HERE / f"mock-{style}.png")
        crop = shot.crop((100, 330, 1540, 630))
        band = Image.new("RGB", (crop.width, crop.height + 44), (20, 18, 18))
        band.paste(crop, (0, 44))
        ImageDraw.Draw(band).text((14, 8), title, fill=(217, 164, 65) if style != "now" else (235, 235, 230), font=f)
        crops.append(band)
    sheet = Image.new("RGB", (crops[0].width, sum(c.height + 8 for c in crops)), (12, 12, 12))
    y = 0
    for c in crops:
        sheet.paste(c, (0, y))
        y += c.height + 8
    sheet.save(HERE / "mock-compare.png")

    cell_w, cell_h = PLATE_W * 2 + 24, PLATE_H * 2 + 24
    grid = Image.new("RGB", (240 + len(STATES) * cell_w, 50 + len(STYLES) * cell_h), (58, 46, 36))
    d = ImageDraw.Draw(grid)
    for j, (label, _, _) in enumerate(STATES):
        d.text((240 + j * cell_w + 10, 12), label, fill=(235, 235, 230), font=f)
    for i, (style, title) in enumerate(STYLES):
        d.text((14, 50 + i * cell_h + cell_h // 2), title.split(" — ")[0], fill=(217, 164, 65), font=font(30), anchor="lm")
        for j, (_, state, unit) in enumerate(STATES):
            p = plate(style, state, unit)
            grid.paste(p, (240 + j * cell_w + 12, 50 + i * cell_h + 12), p)
    grid.save(HERE / "mock-states.png")
    print("목업: mock-compare.png", sheet.size, "mock-states.png", grid.size, "mock-<now|A|B|C>.png")


if __name__ == "__main__":
    main()
