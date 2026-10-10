# -*- coding: utf-8 -*-
"""Round 53 (the user's play feedback, 2026-10-10: "용병 아이템창 파란색 배경있어도 뒤에 격자 구분선은 유지 / 아이템 드래그 시 인벤에 초록 배경은
격자 구분없이 나오게 / 목업 제공. 승인 후 구현"): an item's blue ground split by the grid's lines, and the held thing's green ghost as one
block without them. Drawn on today's screenshots (game/, 2026-10-10 r52b) at their own pixels: the squares are 50 with a gap of 2, so the
lines are known; the blue (or green) pixels in a gap are recoloured and the icon over them is left alone. No API call.
  .venv/bin/python ArtPipeline/Archive/53-grid-lines/mock_grid_lines.py
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
sys.path.insert(0, str(HERE.parent / "49-inventory-style"))
import mock_inventory_style as S  # noqa: E402

GAME = HERE / "game"
SQ, GAP = 50, 2
STEP = SQ + GAP

LINE = (52, 52, 56)              # the grid's grey line between two squares (a board's; the inventory's is (56, 56, 61))
INV_LINE = (56, 56, 61)
GROUND = (28, 36, 78)            # an item's dark blue ground
PICKED = (53, 71, 147)           # the held item's lighter blue
TILE_GROUND = (18, 28, 74)       # a shop tile's piece under the tile's dark
GREEN = (21, 101, 33)            # the ghost's green over a black square (today's 45% in the linear space)
NAVY = (14, 18, 42)              # option B: the line inside an item's ground, a darker blue

BOARDS = [(133, 646), (323, 646), (513, 646), (703, 646)]   # the four boards' top-left square on ko_17 (1920x1080)
INVENTORY = (38, 166)                                       # the inventory window's first square on ko_17
GHOST_ON_BOARD = (BOARDS[3], 0, 1, 2, 1)                     # ko_17: the inventory's rat claw held over Roen's board, (0, 1), 2x1
INV_GHOST = (4, 1, 2, 1)                                     # drawn here: the claw moved over the inventory's (4, 1)
DAGGER = (1695, 335, 2, 1)                                   # ko_43: the dagger's piece on its shop tile (2x1)


def F(px): return ImageFont.truetype(str(S.PRET), px)


def near(c, ref, tol):
    return sum(abs(a - b) for a, b in zip(c, ref)) <= tol


def box(origin, x, y, w, h):
    ox, oy = origin
    return ox + x * STEP, oy + y * STEP, ox + x * STEP + w * STEP - GAP - 1, oy + y * STEP + h * STEP - GAP - 1


def gaps(x0, y0, x1, y1, w, h):
    """The gap pixels inside a piece of w x h squares whose box is (x0, y0, x1, y1): the columns and rows between its squares."""
    for i in range(1, w):
        gx = x0 + i * STEP - GAP
        for x in range(gx, gx + GAP):
            for y in range(y0, y1 + 1):
                yield x, y
    for j in range(1, h):
        gy = y0 + j * STEP - GAP
        for y in range(gy, gy + GAP):
            for x in range(x0, x1 + 1):
                yield x, y


def split(img, b, w, h, ground, line, tol=16):
    """The piece's ground in its gaps turns to the line; the icon's pixels there stay (the icon lies over the lines)."""
    px = img.load()
    for x, y in gaps(*b, w, h):
        if near(px[x, y], ground, tol):
            px[x, y] = line


def join(img, b, w, h, line, green, tol=10):
    """The ghost's gaps filled with its green: one block. The ghost icon's pixels stay."""
    px = img.load()
    for x, y in gaps(*b, w, h):
        if near(px[x, y], line, tol):
            px[x, y] = green


def ghost(img, b, w, h, icon, whole):
    """A ghost drawn where today's game has none (the inventory): green squares, the gaps too when whole, and the icon at 88%."""
    x0, y0, x1, y1 = b
    d = ImageDraw.Draw(img)
    if whole:
        d.rectangle((x0, y0, x1, y1), fill=GREEN)
    else:
        for i in range(w):
            for j in range(h):
                d.rectangle((x0 + i * STEP, y0 + j * STEP, x0 + i * STEP + SQ - 1, y0 + j * STEP + SQ - 1), fill=GREEN)
    bw, bh = x1 - x0 + 1 - 10, y1 - y0 + 1 - 10
    art = icon.copy()
    k = min(bw / art.width, bh / art.height)
    art = art.resize((max(1, round(art.width * k)), max(1, round(art.height * k))), Image.LANCZOS)
    a = art.getchannel("A").point(lambda v: int(v * 0.88))
    art.putalpha(a)
    img.paste(art, (x0 + (x1 - x0 + 1 - art.width) // 2, y0 + (y1 - y0 + 1 - art.height) // 2), art)


def node_map(look):
    """ko_17 in a look: "now", "A" (grey lines, one green block) or "B" (navy lines, one green block)."""
    img = Image.open(GAME / "ko_17_map_inventory_selected.png").convert("RGB")
    if look == "now":
        return img
    line = LINE if look == "A" else NAVY
    for origin in BOARDS:
        split(img, box(origin, 0, 0, 3, 1), 3, 1, GROUND, line)
    split(img, box(INVENTORY, 0, 0, 2, 1), 2, 1, PICKED, INV_LINE if look == "A" else (30, 40, 86), tol=20)
    origin, x, y, w, h = GHOST_ON_BOARD
    join(img, box(origin, x, y, w, h), w, h, (55, 55, 60), GREEN)
    return img


def inventory(look):
    """The inventory window of ko_17 with the held claw's ghost drawn over its (4, 1)."""
    img = node_map(look)
    icon = Image.open(ROOT / "Assets/@Art/Item/rat_bite.png").convert("RGBA")
    x, y, w, h = INV_GHOST
    ghost(img, box(INVENTORY, x, y, w, h), w, h, icon, whole=look != "now")
    return img


def shop(look):
    img = Image.open(GAME / "ko_43_shop_pick.png").convert("RGB")
    if look != "now":
        x0, y0, w, h = DAGGER
        split(img, (x0, y0, x0 + w * STEP - GAP - 1, y0 + h * STEP - GAP - 1), w, h, TILE_GROUND, LINE if look == "A" else NAVY, tol=14)
    return img


def zoom(img, crop, k):
    c = img.crop(crop)
    return c.resize((round(c.width * k), round(c.height * k)), Image.LANCZOS)


def grid(rows, cols, path, title, footer):
    """Rows of scenes under a heading each, a column per look (named once at the top), each scene's crops the same size across its row."""
    gap, head, lab = 20, 70, 24
    cw = max(im.width for _, ims in rows for im in ims)
    ch = [max(im.height for im in ims) for _, ims in rows]
    W = gap + len(cols) * (cw + gap)
    H = head + lab + 20 + sum(h + lab + 22 + gap for h in ch) + len(footer) * 32 + 30
    out = Image.new("RGB", (W, H), (18, 18, 22)); d = ImageDraw.Draw(out)
    d.text((gap, 20), title, font=F(32), fill=(240, 236, 226))
    y = head
    for i, col in enumerate(cols):
        d.text((gap + i * (cw + gap), y), col, font=F(lab + 2), fill=(236, 214, 150))
    y += lab + 20
    for (name, ims), h in zip(rows, ch):
        d.text((gap, y), name, font=F(lab - 2), fill=(200, 198, 190))
        for i, im in enumerate(ims):
            out.paste(im, (gap + i * (cw + gap), y + lab + 12))
        y += h + lab + 22 + gap
    for line_ in footer:
        d.text((gap, y), line_, font=F(21), fill=(170, 170, 178)); y += 32
    out.save(path); print(path.name, out.size)


def main():
    looks = ["now", "A", "B"]
    cols = ["지금", "안 A (권장): 회색 선 그대로", "안 B: 파랑 안의 선은 어두운 남색"]
    boards = [zoom(node_map(k), (505, 641, 864, 806), 2.0) for k in looks]
    inv = [zoom(inventory(k), (24, 150, 383, 285), 2.0) for k in looks]
    tiles = [zoom(shop(k), (1500, 322, 1859, 398), 2.0) for k in looks]
    grid([("노드 맵의 보드(카이·로언, 이긴 뒤 보드도 같음): 롱소드 3×1의 파랑, 인벤토리에서 든 발톱 2×1의 초록 그림자", boards),
          ("인벤토리 창: 든 발톱(밝은 파랑)과 그 그림자 — 그림자는 지금 게임의 것을 (4, 1)에 그림", inv),
          ("상점 타일의 조각(단검 2×1, 버클러 1×1)", tiles)],
         cols, HERE / "mock-grid-lines.png",
         "격자 구분선과 그림자 — 파랑은 칸마다, 초록은 한 덩어리 (Round 53)",
         ["파랑(아이템의 바탕): 칸 사이의 선이 아이템 뒤로 보인다. 아이콘은 선 위에 그대로. 든 아이템의 밝은 파랑, 밀려날 아이템의 어두운 금빛도 같다.",
          "초록(놓일 자리의 그림자): 칸 사이도 초록으로 채워 한 덩어리. 놓이지 않는 자리의 빨강도 같다. 보드와 인벤토리 모두.",
          "전투 중의 보드는 지금도 파랑이 없고 선이 보인다(바뀌지 않음). 그린 것은 선과 인벤토리의 그림자뿐이다(나머지는 지금 게임). 호출 없음."])
    node_map("A").save(HERE / "mock-grid-lines-A-map.png"); print("mock-grid-lines-A-map.png")


if __name__ == "__main__":
    main()
