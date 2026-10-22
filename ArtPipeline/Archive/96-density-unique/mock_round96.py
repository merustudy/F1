# -*- coding: utf-8 -*-
"""Round 96: the screens of stage 49 (원정의 밀도, Design/26 판정 16) and stage 50 (유니크의 조합, Design/29 판정 8·9) — mockups.

  .venv/bin/python ArtPipeline/Archive/96-density-unique/mock_round96.py

mock-link.png      the link between the pieces of a recipe on a board: three shapes, then the cases (ready, a tier short, one whetstone and two
                   bases, a unique the party holds, a diagonal)
mock-effect.mp4    after a won battle: the recipe's pieces glow, the catalyst is drawn into the base, the base turns into the unique with a gold
                   ring, the band says so; then the coin pile on the floor flies into the band's coins (2 loops at 1x, 1 at 1/2x; not in Git)
mock-effect-steps.png  the key frames of it
mock-coins.png     the coin pile on the floor (A) against the band's words only (B, now)
mock-text.png      the death's door line (now) and the death sentence without the grace, and the camp of two cards
No paid call: the game's screenshots (game/), the game's own item pictures, shapes and text.
"""
import shutil
import subprocess
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageFilter

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
GAME = HERE / "game"
ITEMS = ROOT / "Assets/@Art/Item"
FONT = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"
FONT_BOLD = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Bold.ttf"
FRAMES_DIR = HERE / "_frames"

SLOT = (0x15, 0x18, 0x1F)
PIECE = (0x1F, 0x26, 0x54)          # UiPalette.GridPiece
GOLD = (0xC7, 0xB3, 0x77)           # UiPalette.DiabloGold = Unique
LINK_GOLD = (0xF2, 0xC9, 0x4C)
LINK_GREY = (0x8E, 0x8E, 0x8A)
TIER = {1: (0x70, 0x70, 0xFF), 2: (0xD3, 0xDB, 0xE4), 3: (0xFF, 0xFF, 0x6E)}   # UiPalette.Rarity
SHEET_BG = (16, 17, 21)
TEXT = (235, 235, 230)
DIM = (150, 150, 145)
GREEN = (0x8F, 0xD1, 0x7A)

SQ, GAP = 50, 2
KAI = (513, 646)                     # the top-left square of row 2's board (카이) in ko_06, measured
FPS = 30


def font(size, bold=False):
    return ImageFont.truetype(str(FONT_BOLD if bold and FONT_BOLD.exists() else FONT), size)


def icon(name):
    return Image.open(ITEMS / f"{name}.png").convert("RGBA")


def cell(origin, c, r, sq=SQ, gap=GAP):
    return origin[0] + c * (sq + gap), origin[1] + r * (sq + gap)


def piece_rect(origin, c, r, w, h, sq=SQ, gap=GAP):
    x, y = cell(origin, c, r, sq, gap)
    return x, y, x + w * sq + (w - 1) * gap - 1, y + h * sq + (h - 1) * gap - 1


def paste_icon(img, name, rect, scale=1.0, alpha=1.0, offset=(0, 0)):
    x0, y0, x1, y1 = rect
    w, h = x1 - x0 + 1, y1 - y0 + 1
    pic = icon(name)
    k = min((w - 8) / pic.width, (h - 8) / pic.height) * scale
    if k <= 0:
        return
    pic = pic.resize((max(1, round(pic.width * k)), max(1, round(pic.height * k))), Image.LANCZOS)
    if alpha < 1.0:
        a = pic.getchannel("A").point(lambda v: round(v * alpha))
        pic.putalpha(a)
    cx, cy = x0 + w / 2 + offset[0], y0 + h / 2 + offset[1]
    img.alpha_composite(pic, (round(cx - pic.width / 2), round(cy - pic.height / 2)))


def draw_piece(img, origin, c, r, w, h, name, tier=None, sq=SQ, gap=GAP, outline=None, outline_w=2):
    d = ImageDraw.Draw(img)
    rect = piece_rect(origin, c, r, w, h, sq, gap)
    d.rectangle(rect, fill=PIECE)
    paste_icon(img, name, rect)
    if outline:
        d.rectangle(rect, outline=outline, width=outline_w)
    if tier:
        d.text((rect[2] - 4, rect[3] - 2), "★" * tier, fill=TIER[tier], font=font(max(10, sq // 4)), anchor="rd")
    return rect


def draw_board(img, origin, cols=3, rows=3, sq=SQ, gap=GAP):
    d = ImageDraw.Draw(img)
    for r in range(rows):
        for c in range(cols):
            x, y = cell(origin, c, r, sq, gap)
            d.rectangle((x, y, x + sq - 1, y + sq - 1), fill=SLOT)


def diamond(d, cx, cy, s, fill):
    d.polygon([(cx, cy - s), (cx + s, cy), (cx, cy + s), (cx - s, cy)], fill=fill, outline=(20, 18, 12))


def edge_mid(a, b):
    """The middle of the side two rects (inclusive pixel boxes) share, rounded to the gap."""
    ax0, ay0, ax1, ay1 = a
    bx0, by0, bx1, by1 = b
    if ay1 < by0 or by1 < ay0:                       # stacked: share a horizontal side
        y = (min(ay1, by1) + max(ay0, by0)) / 2
        x0, x1 = max(ax0, bx0), min(ax1, bx1)
        return (x0 + x1) / 2, y
    x = (min(ax1, bx1) + max(ax0, bx0)) / 2
    y0, y1 = max(ay0, by0), min(ay1, by1)
    return x, (y0 + y1) / 2


# ---- mock-link.png -----------------------------------------------------------------------------------------------------

def link_sheet():
    S, G = 70, 3
    W, H = 1920, 1080
    sheet = Image.new("RGBA", (W, H), SHEET_BG + (255,))
    d = ImageDraw.Draw(sheet)
    d.text((40, 28), "Round 96 · 잇는 표시 (50단계, Design/29 판정 8) — 같은 보드에서 맞닿은 레시피의 재료 사이", fill=TEXT, font=font(30))
    d.text((40, 72), "보드 칸 1.4배. 별(★)은 이 그림에서만 단계를 알리려고 붙였다(게임의 단계 표시는 그대로).", fill=DIM, font=font(20))

    def board(x, y, title, items, links, note, note_colour=DIM, line=None, outlines=None):
        origin = (x, y + 44)
        d.text((x, y), title, fill=TEXT, font=font(22))
        draw_board(sheet, origin, sq=S, gap=G)
        rects = []
        for (c, r, w, h, name, tier) in items:
            out = (outlines or {}).get(len(rects))
            rects.append(draw_piece(sheet, origin, c, r, w, h, name, tier, S, G, outline=out, outline_w=4))
        dd = ImageDraw.Draw(sheet)
        if line:
            (i, j, colour) = line
            ax = (rects[i][0] + rects[i][2]) / 2; ay = (rects[i][1] + rects[i][3]) / 2
            bx = (rects[j][0] + rects[j][2]) / 2; by = (rects[j][1] + rects[j][3]) / 2
            dd.line((ax, ay, bx, by), fill=colour, width=5)
            dd.ellipse((ax - 6, ay - 6, ax + 6, ay + 6), fill=colour); dd.ellipse((bx - 6, by - 6, bx + 6, by + 6), fill=colour)
        for (i, j, colour) in links:
            cx, cy = edge_mid(rects[i], rects[j])
            diamond(dd, cx, cy, 11, colour)
        ny = y + 44 + 3 * S + 2 * G + 14
        for k, part in enumerate(note.split("\n")):
            dd.text((x, ny + k * 28), part, fill=note_colour if k == 0 else DIM, font=font(20))

    sword3 = (0, 0, 3, 1, "longsword", 3)
    # Row 1: the three shapes of the same ready pair
    y1 = 130
    d.text((40, y1), "모양 — 롱소드 3성 + 숫돌 3성 (다음에 이기면 글라디우스)", fill=GOLD, font=font(24))
    board(40, y1 + 46, "A 맞닿은 변의 가운데에 마름모 (권장)", [sword3, (0, 1, 1, 1, "whetstone", 3)], [(0, 1, LINK_GOLD)], "작고 칸을 가리지 않는다.\n두 칸이 맞닿은 곳을 가리킨다.")
    board(540, y1 + 46, "B 두 아이템의 가운데를 잇는 선 (백팩 배틀즈)", [sword3, (0, 1, 1, 1, "whetstone", 3)], [], "백팩 배틀즈와 가장 닮았다.\n아이콘 위를 지나가 그림을 가린다.", line=(0, 1, LINK_GOLD))
    board(1040, y1 + 46, "C 두 아이템의 테두리를 금빛으로", [sword3, (0, 1, 1, 1, "whetstone", 3)], [], "눈에 잘 띈다.\n어느 둘이 짝인지는 말하지 않는다(여럿일 때).", outlines={0: LINK_GOLD, 1: LINK_GOLD})

    # Row 2: the cases, in shape A
    y2 = 600
    d.text((40, y2), "경우 (모양 A)", fill=GOLD, font=font(24))
    board(40, y2 + 46, "1 준비됨 — 금빛", [sword3, (2, 1, 1, 1, "whetstone", 3)], [(0, 1, LINK_GOLD)], "끝 칸 아래도 맞닿음(위·아래·좌·우).", GREEN)
    board(420, y2 + 46, "2 숫돌 2성 — 흐린 회색", [sword3, (0, 1, 1, 1, "whetstone", 2)], [(0, 1, LINK_GREY)], "단계가 모자람: 이겨도 안 된다.\n카드의 레시피 줄이 \"숫돌 3성 필요\".")
    board(800, y2 + 46, "3 숫돌 하나에 바탕 둘 — 하나만", [(0, 0, 3, 1, "fire_staff", 3), (0, 1, 1, 1, "whetstone", 3), (0, 2, 3, 1, "fire_staff", 3)],
          [(0, 1, LINK_GOLD)], "읽는 차례로 첫째(윗줄)와만 잇는다.\n이어진 하나만 잿불이 된다(사용자 판정 3).", GREEN)
    board(1180, y2 + 46, "4 이미 가진 유니크 — 표시 없음", [sword3, (0, 1, 1, 1, "whetstone", 3)], [], "파티가 글라디우스를 이미 가짐:\n같은 유니크는 하나(판정 6). 카드가 까닭을 말함.")
    board(1560, y2 + 46, "5 대각선 — 맞닿음 아님", [(0, 0, 2, 1, "dagger", 3), (2, 1, 1, 1, "whetstone", 3)], [], "변을 맞대야 한다.\n(단검은 레시피가 없어 예로만.)")
    sheet.convert("RGB").save(HERE / "mock-link.png")
    print("목업: mock-link.png")


# ---- the battle after the win: unique effect and coins ------------------------------------------------------------------------

BAND_COINS = (1013, 126)            # the band's "지역 코인 +N" (its middle), ko_06
FLOOR_COINS = (950, 528)            # where the pile lies on the floor, between the party and the drops


def coin_pile(img, cx, cy, scale=1.0, alpha=1.0, label=None):
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    a = round(255 * alpha)
    for k, (dx, dy) in enumerate([(-16, 6), (14, 8), (0, 2), (-6, -6), (8, -10), (0, -18)]):
        x, y = cx + dx * scale, cy + dy * scale
        rw, rh = 15 * scale, 6 * scale
        d.ellipse((x - rw, y - rh + 3 * scale, x + rw, y + rh + 3 * scale), fill=(120, 86, 24, a))
        d.ellipse((x - rw, y - rh, x + rw, y + rh), fill=(236, 190, 70, a), outline=(140, 100, 30, a))
    if label:
        d.text((cx + 26 * scale, cy - 22 * scale), label, fill=(242, 201, 76, a), font=font(round(24 * scale)), anchor="lm",
               stroke_width=3, stroke_fill=(20, 14, 6, a))
    img.alpha_composite(layer)


def battle_base():
    img = Image.open(GAME / "ko_06_battle_won.png").convert("RGBA")
    # Staged on 카이's board: the longsword at 3 stars on the top row (it is there) and a 3-star whetstone under its first square.
    draw_piece(img, KAI, 0, 0, 3, 1, "longsword")
    draw_piece(img, KAI, 0, 1, 1, 1, "whetstone")
    return img


def ease(t):
    t = max(0.0, min(1.0, t))
    return t * t * (3 - 2 * t)


def effect_frame(t, base):
    img = base.copy()
    d = ImageDraw.Draw(img)
    sword = piece_rect(KAI, 0, 0, 3, 1)
    stone = piece_rect(KAI, 0, 1, 1, 1)
    # 0.15–0.45 both pieces glow
    glow = ease((t - 0.15) / 0.3) * (1 - ease((t - 0.85) / 0.2))
    if glow > 0:
        layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
        ld = ImageDraw.Draw(layer)
        for rect in (sword, stone) if t < 0.75 else (sword,):
            ld.rectangle((rect[0] - 4, rect[1] - 4, rect[2] + 4, rect[3] + 4), outline=LINK_GOLD + (round(230 * glow),), width=6)
        img.alpha_composite(layer.filter(ImageFilter.GaussianBlur(3)))
        img.alpha_composite(layer)
    # the link (before the pieces melt)
    if t < 0.45:
        cx, cy = edge_mid(sword, stone)
        diamond(ImageDraw.Draw(img), cx, cy, 8, LINK_GOLD)
    # 0.45–0.75 the whetstone is drawn into the sword's middle
    if t >= 0.45:
        k = ease((t - 0.45) / 0.3)
        ImageDraw.Draw(img).rectangle(stone, fill=SLOT)
        if k < 1:
            sx, sy = (stone[0] + stone[2]) / 2, (stone[1] + stone[3]) / 2
            tx, ty = (sword[0] + sword[2]) / 2, (sword[1] + sword[3]) / 2
            paste_icon(img, "whetstone", stone, scale=1 - 0.7 * k, alpha=1 - k, offset=((tx - sx) * k, (ty - sy) * k))
    # 0.75–0.85 a flash; from 0.8 the gladius
    if t >= 0.8:
        ImageDraw.Draw(img).rectangle(sword, fill=PIECE)
        under = Image.new("RGBA", img.size, (0, 0, 0, 0))
        paste_icon(under, "gladius_glow", (sword[0] - 6, sword[1] - 6, sword[2] + 6, sword[3] + 6))
        img.alpha_composite(under)
        paste_icon(img, "gladius", sword)
    flash = 1 - abs((t - 0.8) / 0.08) if 0.72 <= t <= 0.88 else 0
    if flash > 0:
        layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
        ImageDraw.Draw(layer).rectangle(sword, fill=(255, 244, 200, round(220 * flash)))
        img.alpha_composite(layer)
    # 0.85–1.3 a gold ring spreads out from the piece
    ring = (t - 0.85) / 0.45
    if 0 <= ring <= 1:
        grow = round(4 + 26 * ease(ring))
        layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
        ImageDraw.Draw(layer).rounded_rectangle((sword[0] - grow, sword[1] - grow, sword[2] + grow, sword[3] + grow), radius=8,
                                                outline=GOLD + (round(255 * (1 - ring)),), width=4)
        img.alpha_composite(layer)
    # 1.0–1.4 the band's second line
    line = ease((t - 1.0) / 0.4)
    if line > 0:
        layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
        ld = ImageDraw.Draw(layer)
        ld.rounded_rectangle((800, 168, 1120, 206), radius=6, fill=(18, 20, 28, round(225 * line)), outline=GOLD + (round(255 * line),), width=2)
        ld.text((960, 187), "유니크: 글라디우스", fill=GOLD + (round(255 * line),), font=font(24), anchor="mm")
        img.alpha_composite(layer)
    # the coin pile: on the floor from the win, flying into the band 1.4–1.9, the band's coins flash
    fly = ease((t - 1.4) / 0.5)
    if fly < 1:
        x = FLOOR_COINS[0] + (BAND_COINS[0] - FLOOR_COINS[0]) * fly
        y = FLOOR_COINS[1] + (BAND_COINS[1] - FLOOR_COINS[1]) * fly - 120 * (4 * fly * (1 - fly))
        coin_pile(img, x, y, scale=1.5 - 0.9 * fly, alpha=1.0, label="+8" if fly == 0 else None)
    pop = 1 - abs((t - 1.95) / 0.15) if 1.8 <= t <= 2.1 else 0
    if pop > 0:
        layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
        ImageDraw.Draw(layer).rounded_rectangle((950, 108, 1076, 144), radius=6, outline=LINK_GOLD + (round(255 * pop),), width=3)
        img.alpha_composite(layer)
    return magnifier(img)


def magnifier(img):
    """The mockup's own loupe (not in the game): 카이's board at 2.4x over the stage's right side."""
    crop = img.crop((500, 626, 680, 806)).resize((432, 432), Image.LANCZOS)
    x, y = 1440, 170
    d = ImageDraw.Draw(img)
    d.rectangle((x - 6, y - 36, x + 432 + 5, y + 432 + 5), fill=(12, 12, 14), outline=DIM, width=2)
    d.text((x, y - 18), "확대 ×2.4 — 카이의 보드 (목업에만)", fill=DIM, font=font(18), anchor="lm")
    img.paste(crop, (x, y))
    d.rectangle((498, 624, 682, 808), outline=DIM, width=1)
    return img


def tag(img, t, speed):
    d = ImageDraw.Draw(img)
    d.rectangle((1590, 84, 1910, 120), fill=(0, 0, 0))
    d.text((1600, 102), f"t = {t:4.2f}s   {speed}", fill=TEXT, font=font(20), anchor="lm")
    return img


def effect_movie():
    base = battle_base()
    total = 2.6
    normal = [tag(effect_frame(i / FPS, base), i / FPS, "1x") for i in range(round(total * FPS))]
    slow = [tag(effect_frame(i / (FPS * 2), base), i / (FPS * 2), "1/2x") for i in range(round(total * FPS * 2))]
    steps = [(0.0, "이긴 순간 (잇는 표시, 바닥의 코인 더미)"), (0.35, "두 재료가 빛남"), (0.6, "숫돌이 롱소드로 빨려 듦"),
             (0.95, "글라디우스, 금빛 고리"), (1.45, "띠의 둘째 줄, 코인이 날아감"), (2.3, "끝 (전리품 줍기)")]
    z = 0.5
    cw, ch = round(1920 * z), round(1080 * z)
    sheet = Image.new("RGB", (2 * cw + 3 * 12, 3 * (ch + 44) + 20), (20, 18, 18))
    sd = ImageDraw.Draw(sheet)
    for k, (t, title) in enumerate(steps):
        x = 12 + (k % 2) * (cw + 12)
        y = 12 + (k // 2) * (ch + 44)
        sd.text((x + 4, y + 4), f"{t:.2f}초 · {title}", fill=TEXT, font=font(22))
        sheet.paste(effect_frame(t, base).convert("RGB").resize((cw, ch), Image.LANCZOS), (x, y + 36))
    sheet.save(HERE / "mock-effect-steps.png")
    print("목업: mock-effect-steps.png")
    frames = normal * 2 + slow
    if FRAMES_DIR.exists():
        shutil.rmtree(FRAMES_DIR)
    FRAMES_DIR.mkdir(parents=True)
    for i, frame in enumerate(frames):
        frame.convert("RGB").resize((1280, 720), Image.LANCZOS).save(FRAMES_DIR / f"{i:05d}.png")
    subprocess.run(["swift", str(ROOT / "ArtPipeline/Archive/19-motion-test/encode_mp4.swift"), str(FRAMES_DIR), str(FPS), str(HERE / "mock-effect.mp4")], check=True)
    shutil.rmtree(FRAMES_DIR)
    print(f"목업: mock-effect.mp4 ({len(frames)}장, {FPS}fps: 보통 속도 2회, 1/2배 1회)")


def coins_sheet():
    now = Image.open(GAME / "ko_06_battle_won.png").convert("RGBA")
    a = now.copy()
    coin_pile(a, *FLOOR_COINS, scale=1.5, label="+8")
    z = 0.5
    cw, ch = round(1920 * z), round(1080 * z)
    sheet = Image.new("RGB", (2 * cw + 36, ch + 120), (20, 18, 18))
    d = ImageDraw.Draw(sheet)
    d.text((12, 10), "Round 96 · 코인 더미 (49단계, Design/26 판정 12) — 코인은 이긴 전투마다 들어온다(적마다 2, 층마다 2: 1층의 쥐 넷이면 +8)", fill=TEXT, font=font(22))
    d.text((12, 48), "A (권장) 쓰러진 적의 자리에 코인 더미와 \"+N\" → 이긴 순간 저절로 띠의 \"지역 코인\"으로 날아감(0.5초, 누르지 않음)", fill=GOLD, font=font(20))
    d.text((cw + 24, 76), "B 지금: 띠의 글만", fill=DIM, font=font(20))
    sheet.paste(a.convert("RGB").resize((cw, ch), Image.LANCZOS), (12, 108))
    sheet.paste(now.convert("RGB").resize((cw, ch), Image.LANCZOS), (cw + 24, 108))
    sheet.save(HERE / "mock-coins.png")
    print("목업: mock-coins.png")


def text_sheet():
    W = 1920
    sheet = Image.new("RGB", (W, 1080), (20, 18, 18))
    d = ImageDraw.Draw(sheet)
    d.text((40, 28), "Round 96 · 빈사의 줄과 사망 문장 (49단계 판정 5·16), 야영지 (50단계 판정 7)", fill=TEXT, font=font(30))
    y = 92
    d.text((40, y), "빈사 아이콘 옆 줄 — 바꾸지 않는다", fill=GOLD, font=font(24))
    d.text((60, y + 40), "48단계까지: \"유예 2.1초 · 피격 1/3\"  →  유예 0(49단계)에서는 빈사에 드는 순간부터 지금의 \"피격마다 사망 30%\"가 뜬다(코드 그대로).", fill=TEXT, font=font(20))
    y = 190
    d.text((40, y), "사망 문장 (결과 창 · 띠 · 로그) — \"유예\"를 뺀다", fill=GOLD, font=font(24))
    rows = [
        ("지금", "카이 — 12.4초에 빈사 → 유예가 끝난 뒤 13.1초에 사망. 마지막 피격: 고블린 약탈자의 녹슨 칼 (사망 확률 30%, 굴림 12)", DIM),
        ("권장", "카이 — 12.4초에 빈사 → 13.1초에 사망. 마지막 피격: 고블린 약탈자의 녹슨 칼 (사망 확률 30%, 굴림 12)", TEXT),
        ("영어", "Kai — at death's door at 12.4s → died at 13.1s. Last hit: Goblin Raider's Rusty Blade (death chance 30%, rolled 12)", TEXT),
        ("그대로", "\"N번 맞아 유예가 깨짐\"(유예가 있을 때)과 탈진의 문장은 그대로 — 데이터가 유예를 다시 켜면 쓰인다.", DIM),
    ]
    for k, (label, text, colour) in enumerate(rows):
        d.text((60, y + 44 + k * 36), label, fill=DIM, font=font(20))
        d.text((150, y + 44 + k * 36), text, fill=colour, font=font(20))
    y = 400
    d.text((40, y), "야영지 — 담금질 카드를 뺀다 (쉬기 · 정비 둘)", fill=GOLD, font=font(24))
    z = 0.42
    cw, ch = round(1920 * z), round(1080 * z)
    for k, (name, title) in enumerate((("ko_52_camp_forge.png", "지금: 레시피가 갖춰지면 카드 셋 (쉬기 · 정비 · 담금질)"),
                                       ("ko_33_camp.png", "50단계: 늘 둘 (지금 레시피가 없을 때의 모습 그대로)"))):
        x = 40 + k * (cw + 40)
        d.text((x, y + 44), title, fill=TEXT, font=font(20))
        sheet.paste(Image.open(GAME / name).convert("RGB").resize((cw, ch), Image.LANCZOS), (x, y + 80))
    sheet.save(HERE / "mock-text.png")
    print("목업: mock-text.png")


def main():
    link_sheet()
    coins_sheet()
    text_sheet()
    effect_movie()


if __name__ == "__main__":
    main()
