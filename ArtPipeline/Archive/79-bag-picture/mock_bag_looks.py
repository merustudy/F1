"""Round 79, second ask (2026-10-17 user: "지금 이미지는 잘 안와닫네. 백팩배틀즈 등의 유사게임도 검토해보고, 일단 이미지 api를 사용해
목업을 제공해주고 내가 다시 선택할께"): the generated bag pictures (output/bag/<key>_look<N>.png, three looks) laid on the game's own
screens where a bag is a thing — the loot on the floor, the merchant's bag row, the hand — one row per look, and a sheet of the pictures.
  .venv/bin/python ArtPipeline/Archive/79-bag-picture/mock_bag_looks.py
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

OUT = ROOT / "ArtPipeline/output/bag"
LOOKS = {1: "안 1: 닫힌 가방 (아이템 아이콘처럼)", 2: "안 2: 위에서 본 열린 가방, 안에 칸 (백팩 배틀즈식)", 3: "안 3: 펼친 가방, 주머니가 칸 수만큼"}


def icon(key, look):
    path = OUT / f"{key}_look{look}.png"
    return Image.open(path).convert("RGBA") if path.exists() else None


def fit(img, w, h):
    r = min(w / img.width, h / img.height)
    return img.resize((round(img.width * r), round(img.height * r)), Image.LANCZOS)


def floor(base, pic):
    img = base.copy()
    img.paste(base.crop((1200, 420, 1330, 500)), (1080, 420))
    piece = b.tilt(fit(pic, 200, 110))
    cx, foot = 1140, 475
    img.alpha_composite(piece, (round(cx - piece.width / 2), round(foot - piece.height)))
    d = ImageDraw.Draw(img)
    b.plate(d, cx, foot - piece.height - 26, "가죽 주머니  3×1")
    return img.crop((960, 340, 1330, 520))


def shop(base, pouch, belt):
    img = base.copy()
    x0, y0 = 1042, 262 + 7 * 52
    d = ImageDraw.Draw(img)
    for cols, x, pic, price in ((3, x0, pouch, "12"), (2, x0 + 4 * 52, belt, "8")):
        w = cols * b.CELL + (cols - 1) * b.GAP
        d.rectangle([x, y0, x + w - 1, y0 + b.CELL - 1], fill=b.RIM_COLOR)
        p = fit(pic, w - 8, b.CELL - 6)
        img.alpha_composite(p, (x + (w - p.width) // 2, y0 + (b.CELL - p.height) // 2))
        d = ImageDraw.Draw(img)
        m.text(d, (x + w - 4, y0 + b.CELL - 4), price, 15, b.GOLD, "rd")
    return img.crop((1030, 600, 1340, 700))


def hand(pic):
    base = Image.open(HERE / "base-held.png").convert("RGBA")
    img = base.crop((690, 620, 880, 980))
    piece = fit(pic, 150, 56)
    img.alpha_composite(piece, (95 - piece.width // 2, 300 - piece.height // 2))
    d = ImageDraw.Draw(img)
    d.polygon([(92, 296), (92, 318), (97, 313), (101, 322), (105, 320), (101, 311), (108, 311)], fill=(240, 240, 240), outline=(0, 0, 0))
    return img


def main():
    loot = Image.open(HERE / "base-loot.png").convert("RGBA")
    shop_base = Image.open(HERE / "base-shop.png").convert("RGBA")
    rows = []
    for look, title in LOOKS.items():
        pouch, belt, pack = icon("leather_pouch", look), icon("belt_pouch", look), icon("leather_pack", look)
        if pouch is None:
            continue
        belt = belt or icon("belt_pouch", 2) or pouch
        rows.append((title, [floor(loot, pouch), shop(shop_base, pouch, belt), hand(pouch)], [pouch, belt, pack]))

    scale = 1.6; gap, lab = 24, 44
    cells = [[im.convert("RGB").resize((round(im.width * scale), round(im.height * scale)), Image.LANCZOS) for im in r[1]] for r in rows]
    cw = [max(c[i].width for c in cells) for i in range(3)]; rh = max(im.height for c in cells for im in c)
    iconw = 560
    out = Image.new("RGB", (sum(cw) + iconw + gap * 5, len(rows) * (rh + lab + gap) + gap + 60), (18, 18, 18)); d = ImageDraw.Draw(out)
    for ri, (title, _, icons) in enumerate(rows):
        y = gap + ri * (rh + lab + gap)
        m.text(d, (gap, y + 6), title, 28, (235, 235, 235))
        x = gap
        for ci, im in enumerate(cells[ri]):
            out.paste(im, (x, y + lab)); x += cw[ci] + gap
        # the three pictures as generated (fitted canvases), on the slot's black
        ix = x
        for pic in icons:
            if pic is None:
                continue
            p = fit(pic, 170, 120)
            d.rectangle([ix, y + lab, ix + 176, y + lab + 126], fill=(9, 9, 11))
            out.paste(p, (ix + (176 - p.width) // 2, y + lab + (126 - p.height) // 2), p)
            ix += 184
        m.text(d, (x, y + lab + 136), "생성한 그림 그대로: 가죽 주머니 3×1 · 허리 주머니 2×1 · 가죽 배낭 3×3", 18, (190, 190, 190))
    m.text(d, (gap, out.height - 46), "왼쪽부터 바닥의 전리품 / 상점의 가방 줄 / 손에 든 것. 보드 위의 가방은 어느 안에서도 지금의 우물(칸). 그림은 이미지 API로 생성(호출 7회).", 22, (190, 190, 190))
    out.save(HERE / "mock-bag-looks.png"); print("mock-bag-looks.png", out.size)


if __name__ == "__main__":
    main()
