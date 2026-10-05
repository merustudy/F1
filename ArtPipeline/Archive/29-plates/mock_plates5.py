"""The user's notes on the applied mockup (round 29), no API call:
"이름 패널 가로 크기를 아이템 칸 가로사이즈에 알맞게 좀 조절해주고, 몬스터 이름은 약간 붉은색 계열로 달랐으면 좋겠어(구별을 위해)".
  .venv/bin/python ArtPipeline/Archive/29-plates/mock_plates5.py   # mock5-detail.png, mock5-<scene>-<A|B>.png (after mock_plates4.py)

- The header strip was drawn 178 wide from the board's left + 1; an item cell is 180 wide and its ink outline shows from 3 left
  of it to 3 right of it (measured on the boards: 307..492 for the board at 310). The strip now spans exactly that: 186.
- A: the monsters' names in a pale red (#F28C7E) on the same iron strip; the party's names stay white.
- B: A, and the monsters' strips' iron stained red as well (the enemy's plate colour, kept dark so that the name still reads).
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import mock_plates2 as P  # noqa: E402
import mock_plates3 as M  # noqa: E402
import mock_plates4 as Q  # noqa: E402

Z = P.Z
GAME = HERE / "game"
ENEMY_NAME = (0xF2, 0x8C, 0x7E)
ENEMY_IRON = (0x6A, 0x2C, 0x26)
VARIANTS = {
    "before": {"span": (1, 179), "enemy": P.TEXT, "iron": None, "title": "직전 목업 — 띠 178(칸보다 좌우 4씩 좁음), 이름은 모두 흰 글"},
    "A": {"span": (-3, 183), "enemy": ENEMY_NAME, "iron": None, "title": "A — 띠를 칸의 테두리에 맞춤(186), 몬스터 이름은 옅은 붉은 글 (권장)"},
    "B": {"span": (-3, 183), "enemy": ENEMY_NAME, "iron": ENEMY_IRON, "title": "B — A에 더해 몬스터 띠의 쇠도 붉게"},
}


def stained(piece, colour):
    """The strip's grey iron (not its gold line, rivets or ink) stained towards a colour, its shading kept."""
    px = piece.load()
    for y in range(piece.height):
        for x in range(piece.width):
            r, g, b, a = px[x, y]
            if a and max(r, g, b) - min(r, g, b) < 30 and 24 < (r + g + b) / 3 < 95:
                k = (r + g + b) / 3 / 50
                px[x, y] = (min(255, int(colour[0] * k)), min(255, int(colour[1] * k)), min(255, int(colour[2] * k)), a)
    return piece


def strip(layer, box, row, name, colour, iron):
    x0, y0, x1, y1 = box
    h = y1 - y0
    tag = P.three(Image.open(P.UI / "ui_frame/tag29_slim.png").convert("RGBA"), (x1 - x0, h))
    if iron:
        tag = stained(tag, iron)
    layer.alpha_composite(tag, (x0, y0))
    stud = h - 4 * Z
    layer.alpha_composite(Image.open(P.UI / "ui_piece/badge29_a_stud.png").convert("RGBA").resize((stud, stud), Image.LANCZOS), (x0 + 4 * Z, y0 + 2 * Z))
    d = ImageDraw.Draw(layer)
    P.word(d, (x0 + 4 * Z + stud // 2, y0 + h // 2), str(row), round(stud * 0.62), P.INK, "mm", stroke=0)
    P.word(d, (x0 + 4 * Z + stud + 6 * Z, y0 + h // 2), name, 14 * Z, colour)


def proposal(name, variant):
    title, shot, _, floor, units, boards = Q.SCENES[name]
    v = VARIANTS[variant]
    base = Image.open(GAME / shot).convert("RGBA")
    out = base.copy()
    for x0, _, _ in boards:
        out.alpha_composite(base.crop((x0 - 8, 632, x0 + 188, 1066)), (x0 - 8, 644))
    P.FLOOR = floor
    layer = Image.new("RGBA", (out.width * Z, out.height * Z), (0, 0, 0, 0))
    for cx, unit, state in units:
        P.draw_unit(layer, 2, cx * Z, unit, state)
        if unit.get("job"):
            Q.job_line(layer, cx * Z, floor, unit["job"])
    P.FLOOR = 506
    for x0, row, label in boards:
        enemy = x0 > 960
        a, b = v["span"]
        strip(layer, ((x0 + a) * Z, 622 * Z, (x0 + b) * Z, 644 * Z), row, label, v["enemy"] if enemy else P.TEXT, v["iron"] if enemy else None)
    out.alpha_composite(layer.resize(out.size, Image.LANCZOS))
    return out.convert("RGB")


def main():
    shots = {}
    for name in Q.SCENES:
        for variant in ("A", "B"):
            shots[name, variant] = proposal(name, variant)
            shots[name, variant].save(HERE / f"mock5-{name}-{variant}.png")
    # The headers at 1.5 times: 4층 전투 (the party and the goblins) and 빈사 (the four rats), each variant.
    bands = []
    for variant in ("before", "A", "B"):
        rows = []
        for name, box in (("battle-15", (104, 612, 1440, 700)), ("battle-18", (1040, 612, 1810, 700))):
            image = proposal(name, variant) if variant == "before" else shots[name, variant]
            crop = image.crop(box)
            rows.append(crop.resize((round(crop.width * 1.5), round(crop.height * 1.5)), Image.LANCZOS))
        w = rows[0].width
        band = Image.new("RGB", (w, 44 + sum(r.height + 6 for r in rows)), (20, 18, 18))
        ImageDraw.Draw(band).text((14, 8), VARIANTS[variant]["title"], fill=(235, 235, 230) if variant == "before" else (217, 164, 65), font=P.font(26))
        y = 44
        for r in rows:
            band.paste(r, (0, y))
            y += r.height + 6
        bands.append(band)
    print("mock5-detail.png", M.stack(bands, "mock5-detail.png"))


if __name__ == "__main__":
    main()
