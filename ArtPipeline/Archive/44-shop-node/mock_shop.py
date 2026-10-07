"""Round 44: a shop node on the expedition map and region coins (the user's 2026-10-07 proposal). Mockups over the game's
screenshots (~/Library/Caches/F1/screenshots/20261007-r43): the shop node chosen on the map, the shop's window over the map
(three looks), the two ways of buying (right-click card vs the current click rule), the refresh cost that climbs, and where the
coins show. The shop marker and the coin are drawn shapes standing in for art. No API call.
  .venv/bin/python ArtPipeline/Archive/44-shop-node/mock_shop.py
"""
import math
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageFilter

ROOT = Path(__file__).resolve().parents[3]; HERE = Path(__file__).resolve().parent
FONT = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"
SHOTS = Path.home() / "Library/Caches/F1/screenshots/20261007-r43"
FRAME = ROOT / "Assets/@Art/UI/Frame"; ICON = ROOT / "Assets/@Art/UI/Icon"; ITEM = ROOT / "Assets/@Art/Item"

# UiPalette
TEXT = (0xEB, 0xEB, 0xE6); DIM = (0x9A, 0xA0, 0xAC); INK = (0x18, 0x09, 0x07); BRASS = (0xB8, 0x94, 0x4E)
PANEL = (0x1E, 0x22, 0x2B); PANEL_LIGHT = (0x2A, 0x30, 0x3C); SLOT = (0x15, 0x18, 0x1F); LINE = (0x55, 0x5C, 0x6B)
BUTTON = (0x3B, 0x6F, 0xB5); QUIET = (0x3A, 0x41, 0x50); SELECTED = (0x80, 0x66, 0x14); GOOD = (0x58, 0xB3, 0x68); DEAD = (0x33, 0x33, 0x38)
VIRTUE = (0xF0, 0xC8, 0x5A); FATIGUE = (0xCF, 0xBA, 0xF7); DANGER = (0xC0, 0x39, 0x2B); FATIGUE_BG = (0x3E, 0x2A, 0x5E)
TIER_MARK = {"common": None, "bronze": (0xA8, 0x68, 0x3A), "silver": (0x9A, 0xA7, 0xB8), "gold": (0xD4, 0xA2, 0x32)}
TIER_TEXT = {"common": (0xC9, 0xC2, 0xB0), "bronze": (0xD5, 0x9A, 0x66), "silver": (0xD3, 0xDB, 0xE4), "gold": (0xF0, 0xC8, 0x5A)}
KO = {"bronze": "동", "silver": "은", "gold": "금"}
COIN = (0xD4, 0xA2, 0x32); COIN_DARK = (0x7A, 0x52, 0x16); COIN_LIGHT = (0xF6, 0xDC, 0x86)
WOOD = (0x7A, 0x4E, 0x2C); CLOTH = (0xC8, 0x4B, 0x4B); BONE = (0xE6, 0xDC, 0xC4)

STONE = (992, 202, 1888, 590); PANEL_BG = (28, 32, 42)
PRIMARY_BTN = (1640, 874, 1884, 958); QUIET_BTN = (1216, 874, 1460, 958)
SCREEN = (1920, 1080)


def font(px): return ImageFont.truetype(str(FONT), int(round(px)))
def shot(name): return Image.open(SHOTS / f"{name}.png").convert("RGBA")


def smooth(size, draw):
    k = 4; im = Image.new("RGBA", (size * k, size * k), (0, 0, 0, 0)); draw(ImageDraw.Draw(im), k, size * k)
    return im.resize((size, size), Image.LANCZOS)


def disc(size, color):
    return smooth(size, lambda d, k, s: d.ellipse([0, 0, s - 1, s - 1], fill=color + (255,)))


# ---- drawn shapes: the coin, the shop markers ------------------------------------------------------------------------------------

def coin(size):
    """The region coin: a brass disc with a dark edge, an inner ring and a glint."""
    def draw(d, k, s):
        d.ellipse([0, 0, s - 1, s - 1], fill=COIN_DARK + (255,))
        m = s * 0.09; d.ellipse([m, m, s - 1 - m, s - 1 - m], fill=COIN + (255,))
        m2 = s * 0.27; d.ellipse([m2, m2, s - 1 - m2, s - 1 - m2], outline=COIN_DARK + (190,), width=max(1, int(s * 0.05)))
        d.ellipse([s * 0.20, s * 0.13, s * 0.40, s * 0.27], fill=COIN_LIGHT + (170,))
    return smooth(size, draw)


def marker_coins(size):
    """A: a stack of three coins (the coin of the HUD, piled): money, so a shop."""
    def draw(d, k, s):
        w = max(1, int(s * 0.035)); rx, ry = s * 0.33, s * 0.13
        for cy in (0.72, 0.56, 0.40):
            x, y = s * 0.5, s * cy
            d.ellipse([x - rx, y - ry + s * 0.09, x + rx, y + ry + s * 0.09], fill=COIN_DARK + (255,), outline=INK + (255,), width=w)
            d.ellipse([x - rx, y - ry, x + rx, y + ry], fill=COIN + (255,), outline=INK + (255,), width=w)
            d.ellipse([x - rx * 0.55, y - ry * 0.5, x + rx * 0.55, y + ry * 0.5], outline=COIN_DARK + (160,), width=w)
    return smooth(size, draw)


def marker_awning(size):
    """B: a market stall: two posts, a counter, and a striped canopy with a scalloped edge."""
    def draw(d, k, s):
        w = max(1, int(s * 0.035))
        d.rectangle([s * 0.20, s * 0.40, s * 0.27, s * 0.84], fill=WOOD + (255,), outline=INK + (255,), width=w)
        d.rectangle([s * 0.73, s * 0.40, s * 0.80, s * 0.84], fill=WOOD + (255,), outline=INK + (255,), width=w)
        d.rectangle([s * 0.14, s * 0.62, s * 0.86, s * 0.86], fill=(0x9A, 0x66, 0x3A, 255), outline=INK + (255,), width=w)
        # the canopy: a trapezoid of stripes
        top, bot = s * 0.16, s * 0.44; x0, x1 = s * 0.08, s * 0.92; x0t, x1t = s * 0.22, s * 0.78
        d.polygon([(x0t, top), (x1t, top), (x1, bot), (x0, bot)], fill=BONE + (255,))
        for i in range(0, 5, 2):
            a0 = x0t + (x1t - x0t) * i / 5; a1 = x0t + (x1t - x0t) * (i + 1) / 5
            b0 = x0 + (x1 - x0) * i / 5; b1 = x0 + (x1 - x0) * (i + 1) / 5
            d.polygon([(a0, top), (a1, top), (b1, bot), (b0, bot)], fill=CLOTH + (255,))
        d.polygon([(x0t, top), (x1t, top), (x1, bot), (x0, bot)], outline=INK + (255,), width=w)
        for i in range(5):                        # scallops under the edge
            c0 = x0 + (x1 - x0) * i / 5; c1 = x0 + (x1 - x0) * (i + 1) / 5
            d.chord([c0, bot - s * 0.08, c1, bot + s * 0.08], 0, 180, fill=(CLOTH if i % 2 == 0 else BONE) + (255,), outline=INK + (255,), width=w)
    return smooth(size, draw)


def marker_scale(size):
    """C: a merchant's balance: a post on a base, a beam, two hanging pans."""
    def draw(d, k, s):
        w = max(1, int(s * 0.06)); c = BRASS + (255,)
        d.line([s * 0.5, s * 0.16, s * 0.5, s * 0.84], fill=c, width=w)
        d.line([s * 0.30, s * 0.86, s * 0.70, s * 0.86], fill=c, width=w)
        d.line([s * 0.14, s * 0.28, s * 0.86, s * 0.28], fill=c, width=w)
        d.ellipse([s * 0.44, s * 0.10, s * 0.56, s * 0.22], fill=c)
        for cx in (0.19, 0.81):
            d.line([cx * s, s * 0.28, (cx - 0.11) * s, s * 0.56], fill=c, width=max(1, w // 2))
            d.line([cx * s, s * 0.28, (cx + 0.11) * s, s * 0.56], fill=c, width=max(1, w // 2))
            d.chord([(cx - 0.15) * s, s * 0.42, (cx + 0.15) * s, s * 0.70], 0, 180, fill=c)
    return smooth(size, draw)


MARKERS = {"A": ("A · 코인 더미 (권장)", marker_coins), "B": ("B · 천막 (좌판)", marker_awning), "C": ("C · 저울", marker_scale)}


# ---- the map, the panel, the buttons (round 34's helpers) ------------------------------------------------------------------------

def repaint_node(img, cx, cy, color, marker, label, redraw_line_to=None):
    """A node of the screenshot made into another kind: a new disc and marker over the old, the word under it replaced."""
    d = ImageDraw.Draw(img)
    bg = img.getpixel((cx + 60, cy + 52))
    d.rectangle([cx - 24, cy + 42, cx + 24, cy + 64], fill=bg)
    if redraw_line_to:                                   # a path that ran under the old word
        d.line([(cx, cy + 37), redraw_line_to], fill=BRASS + (round(255 * 0.85),), width=4)
    img.alpha_composite(disc(74, color), (cx - 37, cy - 37))
    m = 48; img.alpha_composite(marker.resize((m, m), Image.LANCZOS), (cx - m // 2, cy - m // 2))
    f = font(18); l, t, r, b = d.textbbox((0, 0), label, font=f)
    d.text((cx - (r - l) / 2 - l, cy + 37 + 4 - t), label, font=f, fill=TEXT, stroke_width=2, stroke_fill=INK)


def button(src, box, width):
    x0, y0, x1, y1 = box; end = 26
    left = src.crop((x0, y0, x0 + end, y1)); right = src.crop((x1 - end, y0, x1, y1)); mid = src.crop((x0 + end, y0, x0 + end + 1, y1))
    out = Image.new("RGBA", (width, y1 - y0)); out.paste(left, (0, 0)); out.paste(mid.resize((width - 2 * end, y1 - y0)), (end, 0)); out.paste(right, (width - end, 0))
    return out


def put_button(img, src, kind, x, width, label, size=30, dim=False):
    b = button(src, PRIMARY_BTN if kind == "primary" else QUIET_BTN, width)
    if dim:
        a = b.split()[3].point(lambda v: int(v * 0.45)); b.putalpha(a)
    img.alpha_composite(b, (x - 4, 874))
    d = ImageDraw.Draw(img); f = font(size); l, t, r, bb = d.textbbox((0, 0), label, font=f)
    d.text((x - 4 + width / 2 - (r - l) / 2 - l, 916 - (bb - t) / 2 - t), label, font=f, fill=(TEXT if not dim else DIM))


def clear_buttons(img):
    ImageDraw.Draw(img).rectangle([996, 870, 1888, 962], fill=PANEL_BG + (255,))


def panel_words(img, title, hint, lines=(), keep_help=True):
    """The panel's right half: the title and the hint replaced, the help kept (moved under the given lines)."""
    help_text = img.crop((996, 738, 1890, 794))
    d = ImageDraw.Draw(img); d.rectangle([996, 642, 1890, 866], fill=PANEL_BG + (255,))
    d.text((1000, 650), title, font=font(34), fill=TEXT)
    d.text((1000, 702), hint, font=font(20), fill=DIM)
    y = 746
    for s, color in lines:
        d.text((1000, y), s, font=font(19), fill=color); y += 28
    if keep_help: img.paste(help_text, (996, y + 8 if lines else 738))


def shop_panel(img, hint="지도 위의 창에서 삽니다.", lines=()):
    """The panel's right half while the shop's window is open: like the camp's, the title and a hint, no battle button."""
    panel_words(img, "10층 · 상점", hint, lines)
    ImageDraw.Draw(img).rectangle([1636, 870, 1888, 962], fill=PANEL_BG + (255,))


def shade(img, box=STONE, alpha=153):
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0)); ImageDraw.Draw(layer).rectangle(box, fill=(0, 0, 0, alpha)); img.alpha_composite(layer)


def draw_coins(img, x, y, n, size=28, align="right", color=TEXT, gap=8, label=None):
    """The coin and a number: `x` is the right edge (align right) or the left edge; `y` the vertical centre."""
    d = ImageDraw.Draw(img); f = font(round(size * 1.05)); s = str(n)
    l, t, r, b = d.textbbox((0, 0), s, font=f); tw = r - l
    lw = 0
    if label:
        fl = font(round(size * 0.7)); ll, lt, lr, lb = d.textbbox((0, 0), label, font=fl); lw = lr - ll + gap
    total = size + gap + tw + lw
    x0 = x - total if align == "right" else x
    if label:
        d.text((x0, y - (lb - lt) / 2 - lt), label, font=fl, fill=DIM); x0 += lw
    img.alpha_composite(coin(size), (round(x0), round(y - size / 2)))
    d.text((x0 + size + gap - l, y - (b - t) / 2 - t), s, font=f, fill=color)
    return (x0, y - size / 2, x0 + total, y + size / 2)


def header_coins(img, n):
    """The coins beside the floor count, top right (every expedition screen)."""
    draw_coins(img, 1700, 41, n, size=28, align="right", color=(0xA0, 0x9C, 0x90))


# ---- items: a board cell with an icon, the tier marks, the card -------------------------------------------------------------------

def nine_slice(src, border, size, tiled=False):
    W, H = size; sw, sh = src.size; b = border
    out = Image.new("RGBA", (W, H), (0, 0, 0, 0))

    def put(piece, xy): out.paste(piece, xy, piece)

    def fill(region, box):
        x0, y0, x1, y1 = box; w, h = x1 - x0, y1 - y0
        if w <= 0 or h <= 0: return
        put(region.resize((w, h), Image.LANCZOS), (x0, y0))
    fill(src.crop((b, b, sw - b, sh - b)), (b, b, W - b, H - b))
    fill(src.crop((b, 0, sw - b, b)), (b, 0, W - b, b)); fill(src.crop((b, sh - b, sw - b, sh)), (b, H - b, W - b, H))
    fill(src.crop((0, b, b, sh - b)), (0, b, b, H - b)); fill(src.crop((sw - b, b, sw, sh - b)), (W - b, b, W, H - b))
    put(src.crop((0, 0, b, b)), (0, 0)); put(src.crop((sw - b, 0, sw, b)), (W - b, 0))
    put(src.crop((0, sh - b, b, sh)), (0, H - b)); put(src.crop((sw - b, sh - b, sw, sh)), (W - b, H - b))
    return out


def sprite(name, scale=0.5):
    im = Image.open(FRAME / f"{name}.png").convert("RGBA")
    return im.resize((int(im.size[0] * scale), int(im.size[1] * scale)), Image.LANCZOS)


def item(id, name, grade, tier="common", cat="무기 장비", size=1, cd="3.0", rows=None, effects=(), fatigue=None, price=0, potion=False):
    facts = [cat, f"크기 {size}칸", f"쿨다운 {cd}초"] + ([rows] if rows else [])
    return dict(id=id, name=name, grade=grade, tier=tier, cat=cat, size=size, facts=facts, effects=list(effects), fatigue=fatigue, price=price, potion=potion)


DAGGER = item("dagger", "단검", 8, cd="1.5", rows="앞에서 2번째 자리까지만 발동", effects=["맨 앞 적에게 피해 3"], fatigue=1, price=12)
SPEAR = item("spear", "창", 8, size=2, cd="3.2", rows="앞에서 2번째 자리까지만 발동", effects=["앞의 적 2명에게 피해 6"], fatigue=1, price=18)
BUCKLER = item("buckler", "버클러", 8, tier="bronze", cat="방어 장비", cd="5.0", rows="맨 앞에서만 발동", effects=["자신에게 보호막 12"], fatigue=1, price=24)
HERB = item("herb_pouch", "약초 주머니", 8, cat="지원 아이템", cd="5.0", effects=["자신의 HP 4 회복"], price=10)
FLASK = item("ember_flask", "불씨 플라스크", 8, cat="공격 아이템", cd="4.5", effects=["맨 앞 적에게 화상 2"], price=14)
POTION = item("healing_potion", "치유 포션", 0, cat="포션", effects=["HP 50 회복"], price=10, potion=True)
STOCK = [DAGGER, SPEAR, BUCKLER, POTION]


def icon_of(it, box):
    """The item's icon fitted into a box (w, h), as the board cell shows it."""
    im = Image.open(ITEM / f"{it['id']}.png").convert("RGBA")
    w, h = box; r = min(w / im.size[0], h / im.size[1])
    return im.resize((max(1, round(im.size[0] * r)), max(1, round(im.size[1] * r))), Image.LANCZOS)


def tier_marks(cell, icon, xy, tier):
    """The outline around the icon in the tier's colour (under the icon) and the tier tag with its stars, bottom-left (round 41)."""
    color = TIER_MARK[tier]
    if not color: return
    r = {"bronze": 2, "silver": 3, "gold": 4}[tier]
    a = icon.split()[3].filter(ImageFilter.MaxFilter(2 * r + 1))
    sil = Image.new("RGBA", icon.size, color + (255,)); sil.putalpha(a)
    cell.alpha_composite(sil, xy)
    cell.alpha_composite(icon, xy)
    stars = {"bronze": 1, "silver": 2, "gold": 3}[tier]
    tw = 6 * 2 + 13 * stars - 1; tx, ty = 5, cell.size[1] - 5 - 20
    d = ImageDraw.Draw(cell); d.rounded_rectangle([tx, ty, tx + tw, ty + 20], radius=4, fill=INK + (235,), outline=color + (255,), width=1)
    star = Image.open(ICON / "star.png").convert("RGBA").resize((12, 12), Image.LANCZOS)
    tint = Image.new("RGBA", star.size, TIER_TEXT[tier] + (255,)); tint.putalpha(star.split()[3])
    for i in range(stars): cell.alpha_composite(tint, (tx + 6 + 13 * i, ty + 4))


def board_cell(it, selected=False, fatigue_tag=False):
    """A board cell (180x60) with the item's icon as the party side draws it."""
    cell = nine_slice(sprite("slot_selected" if selected else "slot"), 8, (180, 60))
    icon = icon_of(it, (164, 48)); xy = ((180 - icon.size[0]) // 2, (60 - icon.size[1]) // 2)
    if it["tier"] != "common":
        tier_marks(cell, icon, xy, it["tier"])
    else:
        cell.alpha_composite(icon, xy)
    if fatigue_tag:
        tag = sprite("fatigue_tag"); tag = nine_slice(tag, 10, (30, 20))
        cell.alpha_composite(tag, (180 - 5 - 30, 5))
        d = ImageDraw.Draw(cell); f = font(14); l, t, r, b = d.textbbox((0, 0), "+1", font=f)
        d.text((180 - 5 - 15 - (r - l) / 2 - l, 15 - (b - t) / 2 - t), "+1", font=f, fill=FATIGUE)
    return cell


def potion_cell(src):
    """The healing potion in its dark slot, cut from the potion strip of the screenshot."""
    return src.crop((995, 103, 1055, 165)).resize((60, 60), Image.LANCZOS)


def title_segments(it):
    segs = [(it["name"], TEXT)]
    if it["tier"] != "common": segs += [(" · ", DIM), (KO[it["tier"]], TIER_TEXT[it["tier"]])]
    if not it["potion"]: segs += [(" · ", DIM), (f"등급 {it['grade']}", TEXT)]
    return segs


def wrap_segments(segs, f, width):
    tokens = []
    for text, color in segs:
        parts = text.split(" ")
        for n, part in enumerate(parts):
            if part: tokens.append((part, color))
            if n < len(parts) - 1: tokens.append((" ", color))
    lines, cur, cur_w = [], [], 0.0
    for tok, color in tokens:
        w = f.getlength(tok)
        if cur and cur_w + w > width and tok != " ":
            lines.append(cur); cur, cur_w = [], 0.0
        if tok == " " and not cur: continue
        while w > width:
            k = len(tok)
            while k > 1 and f.getlength(tok[:k]) > width: k -= 1
            if cur: lines.append(cur); cur, cur_w = [], 0.0
            lines.append([(tok[:k], color)]); tok = tok[k:]; w = f.getlength(tok)
        cur.append((tok, color)); cur_w += w
    if cur: lines.append(cur)
    return lines


def draw_segments(d, x, y, line, f):
    for tok, color in line:
        d.text((x, y), tok, font=f, fill=color + (255,)); x += f.getlength(tok)


def card(it, notch_x=None, with_price=True):
    """Round 42's ink card (look 1) with a notch on its TOP edge pointing up at the shop's tile, and the price as its last line."""
    W = 400; PAD = 16; inner = W - 2 * PAD - 8
    f_title, f_fact, f_eff = font(24), font(19), font(20)
    lines = []
    for ln in wrap_segments(title_segments(it), f_title, inner): lines.append((ln, f_title, 6))
    lines.append(("rule", None, 10))
    facts = it["facts"] if not it["potion"] else ["포션 / 전투 중에 한 번 씁니다"]
    for ln in wrap_segments([(" / ".join(facts), DIM)], f_fact, inner): lines.append((ln, f_fact, 4))
    lines.append(("space", None, 4))
    for eff in it["effects"]:
        for ln in wrap_segments([(eff, TEXT)], f_eff, inner): lines.append((ln, f_eff, 4))
    if it["fatigue"]: lines.append(([(f"전투마다 피로 +{it['fatigue']}", FATIGUE)], f_fact, 4))
    if with_price:
        lines.append(("space", None, 4)); lines.append(([("값 ", DIM), (f"{it['price']} 코인", VIRTUE)], f_fact, 4))
    H = 2 * PAD
    for ln, f, gap in lines: H += (1 if ln == "rule" else 0 if ln == "space" else f.size) + gap
    nh = 12 if notch_x is not None else 0
    img = Image.new("RGBA", (W, H + nh), (0, 0, 0, 0)); d = ImageDraw.Draw(img)
    d.rounded_rectangle((0, nh, W - 1, nh + H - 1), radius=4, fill=INK + (240,), outline=BRASS + (235,), width=1)
    stripe = TIER_MARK[it["tier"]]
    if stripe: d.rectangle((2, nh + 2, 7, nh + H - 3), fill=stripe + (255,))
    if notch_x is not None:
        nx = max(16, min(W - 16, notch_x))
        d.polygon([(nx - 10, nh), (nx + 10, nh), (nx, 0)], fill=INK + (240,))
        d.line([(nx - 10, nh), (nx, 0), (nx + 10, nh)], fill=BRASS + (235,), width=1)
        d.line([(nx - 9, nh), (nx + 9, nh)], fill=INK + (240,), width=1)
    x, y = PAD + 8, nh + PAD
    for ln, f, gap in lines:
        if ln == "rule": d.line([(x, y), (x + inner, y)], fill=BRASS + (110,), width=1); y += 1 + gap
        elif ln == "space": y += gap
        else: draw_segments(d, x, y, ln, f); y += f.size + gap
    return img


def shadowed(card_img, blur=7, alpha=150, dy=4):
    m = blur * 3
    out = Image.new("RGBA", (card_img.size[0] + 2 * m, card_img.size[1] + 2 * m), (0, 0, 0, 0))
    sh = Image.new("RGBA", out.size, (0, 0, 0, 0)); a = card_img.split()[3].point(lambda v: int(v * alpha / 255))
    sh.paste((0, 0, 0, 255), (m, m + dy), a); sh = sh.filter(ImageFilter.GaussianBlur(blur))
    out.alpha_composite(sh); out.alpha_composite(card_img, (m, m))
    return out, m


def place_card(img, card_img, left, top):
    sh, m = shadowed(card_img); img.alpha_composite(sh, (left - m, top - m))


# ---- the shop window ---------------------------------------------------------------------------------------------------------------

BOX_A = (1030, 216, 1850, 576)            # 820x360, over the map like the camp's window (580x320)
TILE_W, TILE_H, TILE_GAP = 186, 160, 10


def window_frame(img, box, title, hint, coins, marker):
    d = ImageDraw.Draw(img)
    d.rounded_rectangle(box, radius=10, fill=PANEL + (255,), outline=BRASS + (255,), width=3)
    img.alpha_composite(marker.resize((80, 80), Image.LANCZOS), (box[0] + 24, box[1] + 22))
    d.text((box[0] + 124, box[1] + 24), title, font=font(32), fill=TEXT)
    d.text((box[0] + 124, box[1] + 72), hint, font=font(19), fill=DIM)
    draw_coins(img, box[2] - 26, box[1] + 46, coins, size=30, color=TEXT, label="가진 코인")


def flat_button(img, box, label, kind="quiet", dim=False, size=24, coins=None):
    d = ImageDraw.Draw(img); color = BUTTON if kind == "primary" else QUIET
    if dim: color = tuple(int(c * 0.6) for c in color)
    d.rounded_rectangle(box, radius=8, fill=color + (255,))
    f = font(size); l, t, r, b = d.textbbox((0, 0), label, font=f); cx, cy = (box[0] + box[2]) / 2, (box[1] + box[3]) / 2
    if coins is None:
        d.text((cx - (r - l) / 2 - l, cy - (b - t) / 2 - t), label, font=f, fill=(DIM if dim else TEXT))
    else:
        cs = 22; fc = font(size); cl, ct, cr, cb = d.textbbox((0, 0), str(coins), font=fc)
        total = (r - l) + 14 + cs + 6 + (cr - cl); x = cx - total / 2
        d.text((x - l, cy - (b - t) / 2 - t), label, font=f, fill=(DIM if dim else TEXT)); x += (r - l) + 14
        img.alpha_composite(coin(cs), (round(x), round(cy - cs / 2))); x += cs + 6
        d.text((x - cl, cy - (cb - ct) / 2 - ct), str(coins), font=fc, fill=(DANGER if dim else VIRTUE))


def tile(img, src, x, y, it, state="normal", w=TILE_W, h=TILE_H, facts=False):
    """One thing for sale: the item as a board cell, its name, (its facts,) its price. state: normal / selected / sold / poor."""
    d = ImageDraw.Draw(img)
    outline, width = LINE, 2
    if state == "selected": outline, width = SELECTED, 3
    fill = SLOT if state != "selected" else (0x26, 0x24, 0x1A)
    d.rounded_rectangle([x, y, x + w, y + h], radius=8, fill=fill + (255,), outline=outline + (255,), width=width)
    if state == "sold":
        f = font(22); l, t, r, b = d.textbbox((0, 0), "팔림", font=f)
        d.text((x + w / 2 - (r - l) / 2 - l, y + h / 2 - (b - t) / 2 - t), "팔림", font=f, fill=DIM); return
    cell = potion_cell(src) if it["potion"] else board_cell(it)
    cx = x + (w - cell.size[0]) // 2; img.alpha_composite(cell, (cx, y + 10))
    f = font(19); segs = title_segments(it); tw = sum(f.getlength(s) for s, _ in segs)
    draw_segments(d, x + w / 2 - tw / 2, y + 80, segs, f)
    sub = it["cat"] if it["potion"] else f"{it['cat']} · {it['size']}칸"
    fs = font(16); l, t, r, b = d.textbbox((0, 0), sub, font=fs)
    d.text((x + w / 2 - (r - l) / 2 - l, y + 106), sub, font=fs, fill=DIM)
    py = y + h - 24
    if facts:
        ff = font(15); yy = y + 128
        for ln in wrap_segments([(" / ".join(it["facts"][2:] if not it["potion"] else ["전투 중에 한 번"]), DIM)], ff, w - 20)[:2]:
            draw_segments(d, x + 10, yy, ln, ff); yy += 19
        for eff in it["effects"]:
            for ln in wrap_segments([(eff, TEXT)], ff, w - 20)[:1]:
                draw_segments(d, x + 10, yy, ln, ff); yy += 19
        if it["fatigue"]: d.text((x + 10, yy), f"전투마다 피로 +{it['fatigue']}", font=ff, fill=FATIGUE); yy += 19
    color = DANGER if state == "poor" else VIRTUE
    draw_coins(img, x + w / 2 + 0, py, it["price"], size=22, align="right", color=color)
    # centre the price: draw_coins aligned right at the centre leaves it left-heavy; shift by half its width
    return


def price_centered(img, cx, cy, n, color=VIRTUE, size=22):
    d = ImageDraw.Draw(img); f = font(round(size * 1.05)); l, t, r, b = d.textbbox((0, 0), str(n), font=f)
    total = size + 8 + (r - l); x0 = cx - total / 2
    img.alpha_composite(coin(size), (round(x0), round(cy - size / 2)))
    d.text((x0 + size + 8 - l, cy - (b - t) / 2 - t), str(n), font=f, fill=color)


def tiles_row(img, src, box, stock, states, facts=False, w=TILE_W, h=TILE_H, top=108):
    """The things for sale side by side under the title; returns each tile's box."""
    n = len(stock); total = n * w + (n - 1) * TILE_GAP; x = box[0] + (box[2] - box[0] - total) // 2; y = box[1] + top
    boxes = []
    for k, it in enumerate(stock):
        bx = x + k * (w + TILE_GAP)
        tile(img, src, bx, y, it, states[k], w=w, h=h, facts=facts)
        boxes.append((bx, y, bx + w, y + h))
    return boxes


def window_a(src, coins=37, states=("normal",) * 4, refresh=3, floor=10, stock=STOCK, hint="지역 코인으로 삽니다. 나가면 다음 층으로 갑니다.", marker=None):
    """A: the camp's kind of window, wider, with the stock as board cells (icons) and the price under each; refresh and leave below."""
    img = src.copy(); shade(img); marker = marker or marker_coins(96)
    window_frame(img, BOX_A, f"{floor}층 · 상점", hint, coins, marker)
    boxes = tiles_row(img, src, BOX_A, stock, states)
    # price under each tile, centred (tile() drew a right-aligned one at the centre; cover and redraw centred)
    d = ImageDraw.Draw(img)
    for (bx, by, bx2, by2), it, st in zip(boxes, stock, states):
        if st == "sold": continue
        d.rectangle([bx + 3, by2 - 38, bx2 - 3, by2 - 3], fill=(SLOT if st != "selected" else (0x26, 0x24, 0x1A)) + (255,))
        price_centered(img, (bx + bx2) / 2, by2 - 22, it["price"], color=(DANGER if st == "poor" else VIRTUE))
    by = BOX_A[1] + 296
    flat_button(img, (BOX_A[0] + 22, by, BOX_A[0] + 22 + 250, by + 48), "새로고침", coins=refresh, dim=coins < refresh)
    flat_button(img, (BOX_A[2] - 22 - 190, by, BOX_A[2] - 22, by + 48), "나가기", kind="primary")
    return img, boxes


BOX_C = (1030, 206, 1850, 586)


def window_c(src, coins=37):
    """C: the same window with three bigger tiles that carry the facts, so that no card is needed."""
    img = src.copy(); shade(img)
    window_frame(img, BOX_C, "10층 · 상점", "지역 코인으로 삽니다. 나가면 다음 층으로 갑니다.", coins, marker_coins(96))
    stock = [DAGGER, SPEAR, BUCKLER]; w, h = 250, 222
    boxes = tiles_row(img, src, BOX_C, stock, ("normal",) * 3, facts=True, w=w, h=h, top=100)
    d = ImageDraw.Draw(img)
    for (bx, by, bx2, by2), it in zip(boxes, stock):
        d.rectangle([bx + 3, by2 - 36, bx2 - 3, by2 - 3], fill=SLOT + (255,))
        price_centered(img, (bx + bx2) / 2, by2 - 20, it["price"])
    by = BOX_C[1] + 330
    flat_button(img, (BOX_C[0] + 22, by, BOX_C[0] + 22 + 250, by + 44), "새로고침", coins=3, size=22)
    flat_button(img, (BOX_C[2] - 22 - 190, by, BOX_C[2] - 22, by + 44), "나가기", kind="primary", size=22)
    return img


def screen_b(src, coins=37):
    """B: the shop as its own screen like the reward screen: the stock as text cards, one under the other, the price where '고르기' is."""
    img = src.copy(); d = ImageDraw.Draw(img)
    d.rectangle([976, 186, 1904, 608], fill=(0x12, 0x14, 0x1A, 255))      # the map region becomes the list
    cards = [DAGGER, SPEAR, BUCKLER]
    for k, it in enumerate(cards):
        x, y = 980, 190 + k * 140; sel = k == 0
        d.rounded_rectangle([x, y, x + 920, y + 132], radius=0, fill=((0x7A, 0x66, 0x1A) if sel else PANEL_LIGHT) + (255,))
        stripe = TIER_MARK[it["tier"]]
        if stripe: d.rectangle([x, y, x + 6, y + 132], fill=stripe + (255,))
        d.text((x + 22, y + 20), "아이템", font=font(18), fill=DIM)
        f = font(28); draw_segments(d, x + 130, y + 12, title_segments(it), f)
        right = "넣을 칸을 누르세요" if sel else "고르기"
        fr = font(22); l, t, r, b = d.textbbox((0, 0), right, font=fr)
        d.text((x + 900 - (r - l) - l, y + 18), right, font=fr, fill=TEXT)
        draw_coins(img, x + 900 - (r - l) - 18, y + 30, it["price"], size=22, align="right", color=VIRTUE)
        d.text((x + 22, y + 56), " / ".join(it["facts"]) + (f"  /  전투마다 피로 +{it['fatigue']}" if it["fatigue"] else ""), font=font(19), fill=DIM)
        d.text((x + 22, y + 86), it["effects"][0], font=font(19), fill=TEXT)
    panel_words(img, "10층 · 상점", "고른 아이템을 넣을 칸을 누르면 삽니다. 인벤토리에 넣기로 바로 받을 수도 있습니다.",
                [("단검 · 등급 8 — 무기 장비 / 크기 1칸 / 쿨다운 1.5초 / 앞에서 2번째 자리까지만 발동", TEXT), ("맨 앞 적에게 피해 3 / 전투마다 피로 +1", FATIGUE)], keep_help=False)
    clear_buttons(img)
    put_button(img, src, "primary", 1000, 236, "인벤토리에 넣기", size=26)
    put_button(img, src, "quiet", 1250, 190, "새로고침 · 3", size=24)
    put_button(img, src, "quiet", 1454, 190, "인벤토리 보기", size=24)
    put_button(img, src, "primary", 1660, 220, "나가기")
    header_coins(img, coins)
    return img


# ---- sheets --------------------------------------------------------------------------------------------------------------------------

def sheet(entries, cols, path, lab=44, gap=24, size=26, footer=None):
    w = max(im.width for _, im in entries); h = max(im.height for _, im in entries); rows = (len(entries) + cols - 1) // cols
    lines = footer or []; fh = 34 * len(lines) + (16 if lines else 0)
    out = Image.new("RGB", (cols * (w + gap) + gap, rows * (h + lab + gap) + gap + fh), (18, 18, 18)); d = ImageDraw.Draw(out)
    for i, (name, im) in enumerate(entries):
        x = gap + (i % cols) * (w + gap); y = gap + (i // cols) * (h + lab + gap)
        d.text((x, y), name, font=font(size), fill=(235, 235, 235)); out.paste(im.convert("RGB"), (x, y + lab))
    for j, s in enumerate(lines): d.text((gap, out.height - fh + j * 34), s, font=font(22), fill=(190, 190, 190))
    out.save(path); print(path.name, out.size)


def note(img, box, text):
    d = ImageDraw.Draw(img); d.rounded_rectangle(box, radius=6, outline=BRASS + (255,), width=3)
    f = font(22); d.rounded_rectangle((box[0], box[1] - 36, box[0] + f.getlength(text) + 20, box[1] - 4), radius=4, fill=INK + (235,), outline=BRASS + (255,))
    d.text((box[0] + 10, box[1] - 32), text, font=f, fill=BRASS)


def mouse_glyph(img, x, y, button="right", scale=1.0):
    """A small mouse with the pressed button lit, as a word for the hand."""
    s = 46 * scale; d = ImageDraw.Draw(img)
    d.rounded_rectangle([x, y, x + s * 0.7, y + s], radius=int(s * 0.33), fill=INK + (230,), outline=BRASS + (255,), width=2)
    d.line([(x + s * 0.35, y), (x + s * 0.35, y + s * 0.42)], fill=BRASS + (255,), width=2)
    d.line([(x, y + s * 0.42), (x + s * 0.7, y + s * 0.42)], fill=BRASS + (255,), width=2)
    bx = (x + s * 0.36, y + 2, x + s * 0.7 - 2, y + s * 0.42 - 1) if button == "right" else (x + 2, y + 2, x + s * 0.35 - 1, y + s * 0.42 - 1)
    d.rounded_rectangle(bx, radius=int(s * 0.2), fill=VIRTUE + (255,))


NODE_CROP = (970, 0, 1910, 980); WINDOW_CROP = (970, 180, 1910, 980); FLOW_CROP = (560, 180, 1910, 980)


def main():
    deep = shot("ko_31_map_deep")            # floor 9: the next floor's battle and elite, the elite chosen
    camp = shot("ko_33_camp")                # the camp's window (floor 15): what a window over the map looks like now
    result = shot("ko_06_battle_result")

    # 1. the shop node on the map: the chosen elite becomes a chosen shop; the panel and the button follow; coins in the header
    node = deep.copy()
    repaint_node(node, 1648, 380, SELECTED, marker_coins(96), "상점", redraw_line_to=(1648, 523))
    panel_words(node, "10층 · 상점", "지역 코인으로 아이템을 사는 곳입니다. 싸움은 없습니다.")
    ImageDraw.Draw(node).rectangle([1636, 870, 1888, 962], fill=PANEL_BG + (255,)); put_button(node, deep, "primary", 1644, 236, "상점으로")
    header_coins(node, 37)
    now = deep.copy(); note(now, (1740, 8, 1900, 74), "지금: 층 수만")
    sheet([("지금: 10층의 정예를 고른 때 (헤더는 층 수만)", now.crop(NODE_CROP)),
           ("상점 노드를 고른 때: 표식(코인 더미), '10층 · 상점', 버튼 '상점으로', 헤더에 가진 코인", node.crop(NODE_CROP))],
          2, HERE / "mock-node.png",
          footer=["상점은 정예·야영지처럼 노드의 한 종류다: 들어가면 싸움 없이 창이 열리고, 나가면 다음 층으로 간다. 어느 층에 얼마나 나오는지는 던전 데이터(정예·야영지와 같은 방식).",
                  "지역 코인은 원정 안에서만 쌓이고 쓰이는 재화다: 전투에서 이기면 받고(아래 mock-coins), 돌아오면 아이템처럼 사라진다. 헤더의 층 수 옆에 늘 보인다."])
    node.convert("RGB").save(HERE / "mock-node-full.png")

    # 1b. the marker: three drawn shapes on a node disc, at the node's size and at 2x
    entries = []
    for key, (label, make) in MARKERS.items():
        big = Image.new("RGBA", (420, 220), (0x37, 0x34, 0x36, 255))
        for i, (size, color, x) in enumerate(((74, BUTTON, 60), (74, SELECTED, 150), (148, GOOD, 250))):
            big.alpha_composite(disc(size, color), (x, 110 - size // 2))
            m = round(size * 0.65); big.alpha_composite(make(96).resize((m, m), Image.LANCZOS), (x + size // 2 - m // 2, 110 - m // 2))
        entries.append((label, big))
    sheet(entries, 3, HERE / "mock-marker.png", footer=["노드 표식의 자리에 둘 도형 셋(지도의 파란·놋쇠·초록 원 위, 오른쪽은 2배). 정예·야영지처럼 도형으로 시작하고 그림은 지시가 있을 때 생성한다."])

    # 2. the window: now (the camp) / A tiles / B list screen / C tiles with facts
    a, _ = window_a(deep); header_coins(a, 37); shop_panel(a)
    a.convert("RGB").save(HERE / "mock-shop-A.png")
    c = window_c(deep); header_coins(c, 37); shop_panel(c)
    b = screen_b(deep)
    sheet([("지금: 야영지의 창 (지도 위, 580x320)", camp.crop(WINDOW_CROP)),
           ("A (권장): 지도 위의 창 820x360 — 보드 칸 모양의 물건 넷, 값, 새로고침·나가기", a.crop(WINDOW_CROP)),
           ("B: 보상처럼 따로 화면 — 글 카드 셋, 값은 '고르기' 자리, 새로고침은 패널", b.crop(WINDOW_CROP)),
           ("C: 창 안의 타일 셋에 사실까지 — 카드가 필요 없지만 셋뿐이고 글이 작다", c.crop(WINDOW_CROP))],
          2, HERE / "mock-window-compare.png",
          footer=["A·C는 야영지처럼 지도 위의 창이고(같은 화면, 같은 그늘), B는 보상처럼 새 화면이다. 가진 코인은 창의 오른쪽 위(A·C)나 헤더(B).",
                  "값과 코인의 수는 그림용이다. 실제 값은 데이터(아이템마다 값, 층에 따른 코인)로 두고 시뮬로 정한다. 단계가 있는 물건(버클러·동)은 보드와 같은 외곽선·별로 보인다."])

    # 3. buying: ㄱ the user's way (right-click = card, left-click = buy to the inventory) vs ㄴ the current rule (click = choose + card, a cell buys)
    # ㄱ①: right-click on the dagger -> the card only
    g1, boxes = window_a(deep); header_coins(g1, 37)
    bx, by, bx2, by2 = boxes[0]; cx = (bx + bx2) // 2
    shop_panel(g1)
    left = max(996, cx - 200); place_card(g1, card(DAGGER, notch_x=cx - left), left, 586)
    mouse_glyph(g1, bx2 - 30, by + 14, "right")
    # ㄱ②: left-click -> bought, straight to the inventory
    g2, _ = window_a(deep, coins=25, states=("sold", "normal", "normal", "normal")); header_coins(g2, 25)
    mouse_glyph(g2, bx2 - 30, by + 14, "left")
    shop_panel(g2, lines=[("단검 · 등급 8을 샀습니다 — 인벤토리 (1/10칸). 코인 37 → 25", TEXT)])
    # ㄴ①: left-click -> chosen (brass rim) + card + the panel's buy button; the boards' cells wait for the click
    n1, boxes = window_a(deep, states=("selected", "normal", "normal", "normal")); header_coins(n1, 37)
    panel_words(n1, "10층 · 상점", "넣을 칸을 누르면 삽니다. 인벤토리에 넣기로 바로 받을 수도 있습니다.",
                [("단검 · 등급 8 — 무기 장비 / 크기 1칸 / 쿨다운 1.5초 / 앞에서 2번째 자리까지만 발동", TEXT), ("맨 앞 적에게 피해 3 / 전투마다 피로 +1", FATIGUE)], keep_help=False)
    clear_buttons(n1)
    put_button(n1, deep, "primary", 1000, 236, "인벤토리에 넣기", size=26); put_button(n1, deep, "quiet", 1250, 190, "인벤토리로", dim=True); put_button(n1, deep, "quiet", 1454, 190, "인벤토리 보기")
    note(n1, (686, 704, 874, 958), "넣을 칸을 누르면 삽니다")
    left = max(996, cx - 200); place_card(n1, card(DAGGER, notch_x=cx - left), left, 586)
    mouse_glyph(n1, bx2 - 30, by + 14, "left")
    # ㄴ②: the cell clicked -> paid, the dagger on Rowan's board, the tile sold
    n2, _ = window_a(deep, coins=25, states=("sold", "normal", "normal", "normal")); header_coins(n2, 25)
    n2.alpha_composite(board_cell(DAGGER, fatigue_tag=True), (690, 708))
    # the board head's fatigue total "피로 +1" (round 32 B1): a tag at the head's right end
    tag = nine_slice(sprite("fatigue_tag"), 10, (62, 20)); n2.alpha_composite(tag, (870 - 5 - 62, 628))
    d = ImageDraw.Draw(n2); f = font(14); l, t, r, b = d.textbbox((0, 0), "피로 +1", font=f); d.text((870 - 5 - 31 - (r - l) / 2 - l, 638 - (b - t) / 2 - t), "피로 +1", font=f, fill=FATIGUE)
    shop_panel(n2, lines=[("단검 · 등급 8을 로언의 보드에 넣었습니다. 코인 37 → 25", TEXT)])
    clear_buttons(n2); put_button(n2, deep, "quiet", 1000, 200, "인벤토리로"); put_button(n2, deep, "quiet", 1220, 236, "인벤토리 보기")
    sheet([("ㄱ① (사용자안) 우클릭 → 카드만 열린다 (고르지 않음)", g1.crop(FLOW_CROP)),
           ("ㄱ② 좌클릭 → 바로 사서 인벤토리로. 타일은 '팔림', 코인 37 → 25", g2.crop(FLOW_CROP)),
           ("ㄴ① (권장) 좌클릭 → 고르기(놋쇠 테) + 카드 + 보드의 칸이 기다린다 (보상과 같은 흐름)", n1.crop(FLOW_CROP)),
           ("ㄴ② 보드의 칸을 누름 → 그때 코인이 나가고 들어간다 (찬 칸이면 합치기·밀어내기도 보상처럼)", n2.crop(FLOW_CROP))],
          2, HERE / "mock-buy-flow.png",
          footer=["ㄱ: 상점에서만 우클릭이 생긴다(다른 화면에는 우클릭이 없다). 한 번에 사지만 인벤토리를 거쳐 다시 꺼내야 하고, 잘못 누르면 코인이 나간다.",
                  "ㄴ: 보상 카드와 같은 '고르고 → 놓기'다. 카드는 Round 42 규칙대로 좌클릭에 열리고 다음 클릭에 닫힌다(위 꼭지만 새로). 놓을 때 사므로 잘못 눌러도 코인이 남고, 같은 아이템 위에 놓으면 합쳐진다.",
                  "둘 다: 코인이 모자라면 타일의 값이 붉고 눌리지 않는다. 포션은 빈 포션 칸이 있을 때만(보상과 같다)."])
    n1.convert("RGB").save(HERE / "mock-buy-1.png"); n2.convert("RGB").save(HERE / "mock-buy-2.png")

    # 4. the refresh: its cost climbs within one shop and starts over at the next shop
    strips = []
    for n, (coins, states, cost, label) in enumerate((
            (37, ("normal",) * 4, 3, "① 들어왔을 때: 새로고침 3코인"),
            (34, ("normal",) * 4, 5, "② 한 번 새로고침 → 물건 넷이 바뀌고 다음은 5코인 (+2)"),
            (29, ("normal",) * 4, 7, "③ 두 번째 → 7코인. 코인이 모자라면 값이 붉고 눌리지 않는다"),
            (6, ("normal", "sold", "normal", "sold"), 7, "④ 가진 코인 6 < 7: 새로고침이 꺼진다. 나가면 다음 상점은 다시 3코인부터"))):
        stock = [STOCK, [HERB, FLASK, DAGGER, POTION], [SPEAR, HERB, BUCKLER, POTION], [SPEAR, HERB, BUCKLER, POTION]][n]
        im, _ = window_a(deep, coins=coins, states=states, refresh=cost, stock=stock)
        strips.append((label, im.crop((1020, 206, 1860, 586))))
    sheet(strips, 2, HERE / "mock-refresh.png",
          footer=["비용은 데이터 둘로 정한다: 시작값(ShopRefreshBase, 그림은 3)과 한 번마다의 증가(ShopRefreshStep, 그림은 2). Balatro($5, +$1)·Brotato(웨이브에 비례한 시작값, 리롤마다 증가)와 같은 모양이고, 상점을 나가면 초기화된다.",
                  "새로고침은 팔리지 않은 물건을 모두 새로 뽑는다(판 자리도 채운다). 뽑는 난수는 원정 시드에서 나온 상점 스트림이라 같은 시드면 같은 물건이 나온다(결정론)."])

    # 5. the coins: where they come from (the battle's result) and where they show (the header)
    r = result.copy(); d = ImageDraw.Draw(r)
    d.text((360, 380), "지역 코인", font=font(26), fill=TEXT)
    draw_coins(r, 500, 395, "+12", size=28, align="left", color=VIRTUE)
    d.text((360, 424), "동굴 쥐 ×2, 고블린 약탈자 — 적마다, 깊은 층일수록 많이", font=font(20), fill=DIM)
    h = deep.copy(); header_coins(h, 37); note(h, (1540, 8, 1900, 74), "헤더: 층 수 옆에 가진 코인")
    sheet([("전투 결과 창: 이번 전투에서 받은 코인", r.crop((310, 170, 1610, 710))),
           ("모든 원정 화면의 헤더: 가진 코인 (2배)", h.crop((1500, 0, 1910, 80)).resize((820, 160), Image.LANCZOS))],
          2, HERE / "mock-coins.png",
          footer=["받는 코인은 데이터로: 적 하나마다 기본값(CoinsPerEnemy) + 층마다의 증가(CoinsPerFloor), 정예는 배(EliteCoinPercent), 보스는 없음(끝이므로). 포션·아이템 보상은 그대로다.",
                  "코인은 ExpeditionState에만 있다(런의 재화가 아니다). 귀환 정산 1(아이템과 포션이 사라진다)에 코인도 함께 사라진다. 로비의 재화·고용·상점(Slice C)은 다른 것이다."])


if __name__ == "__main__": main()
