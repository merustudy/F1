"""Round 79, third ask (2026-10-17 user: "안2번식으로 가는데 칸은 인벤 네모칸에 최대한 맞추어 이미지 생성 / 한번 가방 설치하면 칸이 기존 칸으로
늘어나는 방안과 가방이미지를 뒷 배경으로 두는 방안의 각 목업"): the look-2 pictures redrawn with their compartments on the grid
(output/bag/<key>_look2b.png), and the board after a bag is placed, two ways:
  (a) the cells only: the bag's squares join the board as Diablo's well (the stone rim and the black squares, as today)
  (b) the bag's picture under its squares: the picture is laid where the bag lies and the board's black squares are drawn over its
      compartments, so the leather rim and the stitched bars show between and round the squares; items go on top as always
  .venv/bin/python ArtPipeline/Archive/79-bag-picture/mock_board.py
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(HERE.parent / "71-lobby-expedition"))
import mock_lobby as m  # noqa: E402
import mock_bag as b  # noqa: E402
import mock_bag_looks as L  # noqa: E402

OUT = ROOT / "ArtPipeline/output/bag"
CELL, GAP, RIM = b.CELL, b.GAP, b.RIM
FRAME_FILL = (0x24, 0x23, 0x21); FRAME_LINE = (0x78, 0x6C, 0x5C)


def pic(key):
    return Image.open(OUT / f"{key}_look2b.png").convert("RGBA")


def raw(key):
    """The picture as generated (no outline ring): the opening is measured on this one."""
    return Image.open(OUT / f"{key}_look2b.raw.png").convert("RGBA")


def opening(img):
    """The box of the bag's opening: the columns and rows where dark pixels (the compartments) make up most of the picture's filled height
    or width, which leaves out the leather rim, its outline and the small fittings."""
    px = img.load(); w, h = img.size
    dark = [[px[x, y][3] > 200 and px[x, y][0] + px[x, y][1] + px[x, y][2] < 160 for x in range(w)] for y in range(h)]
    filled_rows = [any(px[x, y][3] > 200 for x in range(w)) for y in range(h)]
    filled_cols = [any(px[x, y][3] > 200 for y in range(h)) for x in range(w)]
    col_ok = [sum(dark[y][x] for y in range(h)) > 0.45 * sum(1 for y in range(h) if filled_rows[y] and px[x, y][3] > 200) for x in range(w)]
    row_ok = [sum(dark[y][x] for x in range(w)) > 0.45 * sum(1 for x in range(w) if filled_cols[x] and px[x, y][3] > 200) for y in range(h)]
    xs = [x for x in range(w) if col_ok[x]]; ys = [y for y in range(h) if row_ok[y]]
    return (min(xs), min(ys), max(xs), max(ys)) if xs and ys else (0, 0, w, h)


def squares_area(cols, rows):
    return cols * CELL + (cols - 1) * GAP, rows * CELL + (rows - 1) * GAP


def picture_under(cols, rows, key):
    """The picture scaled so that its opening covers the squares' area exactly; returns the image and the squares' offset inside it."""
    img = raw(key)
    ox0, oy0, ox1, oy1 = opening(img)
    aw, ah = squares_area(cols, rows)
    sx, sy = aw / (ox1 - ox0 + 1), ah / (oy1 - oy0 + 1)
    scaled = img.resize((max(1, round(img.width * sx)), max(1, round(img.height * sy))), Image.LANCZOS)
    return scaled, (round(ox0 * sx), round(oy0 * sy))


def draw_squares(d, x, y, cols, rows, items=()):
    for r in range(rows):
        for c in range(cols):
            sx, sy = x + c * (CELL + GAP), y + r * (CELL + GAP)
            d.rectangle([sx, sy, sx + CELL - 1, sy + CELL - 1], fill=b.SQUARE)


def frame(width_cols=3, height_rows=8):
    """A party board's frame (3x8): the sunken stone with dashed lines, as the game draws the frame while a bag is held."""
    aw, ah = squares_area(width_cols, height_rows)
    M = 24
    img = Image.new("RGBA", (aw + 2 * M, ah + 2 * M), m.PANEL + (255,)); d = ImageDraw.Draw(img)
    d.rectangle([M, M, M + aw - 1, M + ah - 1], fill=FRAME_FILL)
    for r in range(height_rows):
        for c in range(width_cols):
            sx, sy = M + c * (CELL + GAP), M + r * (CELL + GAP)
            # dashed outline: 4 on, 3 off
            for i in range(0, CELL, 7):
                d.line([(sx + i, sy), (sx + min(i + 3, CELL - 1), sy)], fill=FRAME_LINE)
                d.line([(sx + i, sy + CELL - 1), (sx + min(i + 3, CELL - 1), sy + CELL - 1)], fill=FRAME_LINE)
                d.line([(sx, sy + i), (sx, sy + min(i + 3, CELL - 1))], fill=FRAME_LINE)
                d.line([(sx + CELL - 1, sy + i), (sx + CELL - 1, sy + min(i + 3, CELL - 1))], fill=FRAME_LINE)
    return img


def board(way):
    """The frame with the start pack (3x3) at the top and a leather pouch (3x1) placed on row 5, drawn the given way."""
    img = frame(); d = ImageDraw.Draw(img)
    placements = [("leather_pack", 3, 3, 0, 0), ("leather_pouch", 3, 1, 0, 4)]
    for key, cols, rows, cx, cy in placements:
        x, y = 24 + cx * (CELL + GAP), 24 + cy * (CELL + GAP)
        aw, ah = squares_area(cols, rows)
        if way == "a":
            # Diablo's well as today: the stone rim tinted by the leather, the grey between, the black squares.
            d.rectangle([x - RIM, y - RIM, x + aw + RIM - 1, y + ah + RIM - 1], fill=b.RIM_COLOR)
            d.rectangle([x, y, x + aw - 1, y + ah - 1], fill=b.LINE)
            draw_squares(d, x, y, cols, rows)
        else:
            # The picture is the bag on the board: its compartments, scaled onto the grid, are the squares (no black squares drawn over them).
            scaled, (ox, oy) = picture_under(cols, rows, key)
            layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
            layer.paste(scaled, (x - ox, y - oy), scaled)
            img.alpha_composite(layer); d = ImageDraw.Draw(img)
    # a few items on the pack, as on the board (the knight's longsword 3x1, a dagger 2x1, a pouch 2x1), from the base screenshot
    base = Image.open(HERE / "base-held.png").convert("RGBA")
    for (sx0, sy0, sx1, sy1), (tx, ty) in (((704, 648, 858, 696), (24, 24)), ((704, 700, 806, 748), (24, 76)), ((704, 752, 806, 800), (24, 128))):
        piece = base.crop((sx0, sy0, sx1, sy1))
        img.alpha_composite(piece, (tx, ty))
    return img


def main():
    cells = [
        ("보드 (a): 칸만 늘어남 — 지금의 우물", board("a")),
        ("보드 (b): 가방 그림이 칸의 뒷배경 — 그림의 칸이 곧 칸", board("b")),
    ]
    loot = Image.open(HERE / "base-loot.png").convert("RGBA"); shop = Image.open(HERE / "base-shop.png").convert("RGBA")
    pouch, belt, pack = pic("leather_pouch"), pic("belt_pouch"), pic("leather_pack")
    things = [("바닥의 전리품", L.floor(loot, pouch)), ("상점의 가방 줄", L.shop(shop, pouch, belt)), ("손에 든 것", L.hand(pouch))]
    scale = 2.2
    out_w = 2000; gap = 24
    out = Image.new("RGB", (out_w, 1700), (18, 18, 18)); d = ImageDraw.Draw(out)
    m.text(d, (gap, 10), "안 2 다시 그림 (칸을 보드의 네모 칸에 맞춤): 생성한 그림 그대로", 28, (235, 235, 235))
    x = gap
    for p in (pouch, belt, pack):
        q = L.fit(p, 300, 200)
        d.rectangle([x, 56, x + 306, 262], fill=(9, 9, 11)); out.paste(q, (x + (306 - q.width) // 2, 56 + (206 - q.height) // 2), q); x += 330
    m.text(d, (x + 10, 120), "가죽 주머니 3×1 · 허리 주머니 2×1 · 가죽 배낭 3×3\n(호출 3회. 입 안의 칸이 보드의 칸 자리에 오도록 지시)", 22, (190, 190, 190))
    y = 300
    for i, (name, im) in enumerate(cells):
        q = im.convert("RGB").resize((round(im.width * scale), round(im.height * scale)), Image.LANCZOS)
        xx = gap + i * 600
        m.text(d, (xx, y), name, 22, (235, 235, 235))
        out.paste(q, (xx, y + 34))
    xx = gap + 2 * 600 + 40
    m.text(d, (xx, y), "물건일 때 (안 2 다시 그린 그림)", 24, (235, 235, 235))
    yy = y + 40
    for name, im in things:
        q = im.convert("RGB").resize((round(im.width * 1.4), round(im.height * 1.4)), Image.LANCZOS)
        m.text(d, (xx, yy), name, 20, (190, 190, 190)); out.paste(q, (xx, yy + 28)); yy += q.height + 44
    m.text(d, (gap, out.height - 40), "(a)는 지금의 보드 그대로(칸만 는다). (b)는 그림의 입을 격자에 맞춰 깔아 그림의 칸이 곧 보드의 칸이고 가죽 테와 바느질이 칸 사이·둘레로 보인다. 아이템은 어느 쪽이든 칸 위에 놓인다.", 22, (190, 190, 190))
    out.save(HERE / "mock-bag-board.png"); print("mock-bag-board.png", out.size)


if __name__ == "__main__":
    main()
