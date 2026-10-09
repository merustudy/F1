# -*- coding: utf-8 -*-
"""Round 49: the inventory's look (the user, 2026-10-09: "인벤토리 ui 스타일이 마음에 안들어. 현재 ui 스타일과 무관하게
1. 디아블로 2 ui 스타일 차용 2. 백팩 배틀즈 ui 스타일 차용 3. 기타 유명 게임 조사 후 네 최선의 ui스타일 차용. 목업 이미지 제공해줘").
One scene in three looks, over the node map with the inventory open (screenshot 20261009-s19c, ko_17): the four boards of stage 19
(the 3 x 8 frame, squares of 50 and gaps of 2, the bags and their items), the longbow held from the inventory over Ella's bag, the
inventory window and the item card. Only the boards, the inventory window, the card and the panel's detail line are drawn again;
the rest is today's screen. Then each look's states on one board: at rest, a held item that fits, one that pushes one item out,
one that cannot go, a held bag, and the battle's cooldown light. Drawn shapes and today's item icons; no API call.
  .venv/bin/python ArtPipeline/Archive/49-inventory-style/mock_inventory_style.py
"""
import math
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter, ImageFont, ImageChops

ROOT = Path(__file__).resolve().parents[3]; HERE = Path(__file__).resolve().parent
SHOTS = Path.home() / "Library/Caches/F1/screenshots/20261009-s19c"
ITEM = ROOT / "Assets/@Art/Item"
PRET = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"
MYUNG = Path("/System/Library/Fonts/Supplemental/AppleMyungjo.ttf")

K = 2                                   # everything is drawn at twice the screen's size, then halved
SQ, GAP = 50, 2                         # GridGeometry.Square, Gap
COLS = [133, 323, 513, 703]             # the four boards (④ to ①), as today
TOP = 646                               # the frame's top, as today
LEFT = (0, 612, 962, 1080)              # the board panel
RIGHT = (972, 92, 1908, 828)            # the inventory window (Architecture/12: 92 to 826 on the right)
PAD = 16                                # room round an item's piece for its outline, glow and shadow


def s(v): return int(round(v * K))
def B(x0, y0, x1, y1): return [s(x0), s(y0), s(x1) - 1, s(y1) - 1]
def span(n, q=SQ): return n * q + (n - 1) * GAP
def cell(x0, y0, gx, gy, q=SQ): return x0 + gx * (q + GAP), y0 + gy * (q + GAP)
def D(img): return ImageDraw.Draw(img, "RGBA")


_fonts = {}


def font(px, path=PRET):
    key = (str(path), px)
    if key not in _fonts: _fonts[key] = ImageFont.truetype(str(path), s(px))
    return _fonts[key]


def myung(px): return font(px, MYUNG)


def text(d, xy, t, f, fill, anchor="la", stroke=0, stroke_fill=None):
    d.text((s(xy[0]), s(xy[1])), t, font=f, fill=fill, anchor=anchor, stroke_width=stroke, stroke_fill=stroke_fill)


def width_of(t, f): return f.getlength(t) / K


def line(d, pts, fill, w=1.0): d.line([(s(x), s(y)) for x, y in pts], fill=fill, width=max(1, s(w)))


def rrect(d, box, r, fill=None, outline=None, w=1.0):
    d.rounded_rectangle(B(*box), radius=s(r), fill=fill, outline=outline, width=max(1, s(w)) if outline else 0)


def dashed(d, box, fill, w=1.5, dash=5.0, gap=3.0):
    x0, y0, x1, y1 = box

    def run(a, b, c, horiz):
        t = a
        while t < b:
            e = min(t + dash, b)
            d.line([(s(t), s(c)), (s(e), s(c))] if horiz else [(s(c), s(t)), (s(c), s(e))], fill=fill, width=max(1, s(w)))
            t += dash + gap
    run(x0, x1, y0, True); run(x0, x1, y1, True); run(y0, y1, x0, False); run(y0, y1, x1, False)


def star(d, cx, cy, r, fill, outline=None):
    pts = []
    for i in range(10):
        a = -math.pi / 2 + i * math.pi / 5; rr = r if i % 2 == 0 else r * 0.45
        pts.append((s(cx + rr * math.cos(a)), s(cy + rr * math.sin(a))))
    d.polygon(pts, fill=fill, outline=outline)


def num_badge(d, cx, cy, n, fill, ink, ring=None, r=7.5):
    d.ellipse(B(cx - r, cy - r, cx + r, cy + r), fill=fill, outline=ring, width=max(1, s(1)) if ring else 0)
    text(d, (cx, cy + 0.5), str(n), font(10), ink, "mm")


def lighten(c, v): return tuple(min(255, x + v) for x in c[:3])
def shade(c, f): return tuple(max(0, min(255, int(x * f))) for x in c[:3])
def mix(a, b, t): return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))


def comp(img, layer, xy):
    """An RGBA layer over an opaque RGB canvas (or over another layer)."""
    if img.mode == "RGB": img.paste(layer, xy, layer)
    else: img.alpha_composite(layer, (max(0, xy[0]), max(0, xy[1])) if min(xy) >= 0 else xy)


# ---- textures and light --------------------------------------------------------------------------------------------------------

def tex(w, h, base, amp=14, coarse=18, blend=0.55, streak=False):
    small = (max(2, w // 70), max(2, h // 3)) if streak else (max(2, w // coarse), max(2, h // coarse))
    c = Image.effect_noise(small, 50).resize((w, h), Image.BICUBIC)
    f = Image.effect_noise((w, h), 50).filter(ImageFilter.GaussianBlur(0.7))
    n = Image.blend(f, c, blend)
    return Image.merge("RGB", [n.point(lambda v, b=b: max(0, min(255, int(b + (v - 128) / 128 * amp)))) for b in base])


def fill_tex(img, box, t_base, amp=14, coarse=18, blend=0.55, streak=False, radius=0):
    x0, y0, x1, y1 = [s(v) for v in box]; w, h = x1 - x0, y1 - y0
    t = tex(w, h, t_base, amp, coarse, blend, streak)
    m = Image.new("L", (w, h), 0); ImageDraw.Draw(m).rounded_rectangle([0, 0, w - 1, h - 1], radius=s(radius), fill=255)
    if img.mode == "RGB": img.paste(t, (x0, y0), m)
    else:
        t = t.convert("RGBA"); t.putalpha(m); img.alpha_composite(t, (x0, y0))


def vignette(img, box, color, strength=0.5, inner=0.5):
    x0, y0, x1, y1 = [s(v) for v in box]; w, h = x1 - x0, y1 - y0
    g = Image.radial_gradient("L").resize((w, h))
    a = g.point(lambda v: int(max(0.0, min(1.0, (v / 255 - inner) / (1 - inner))) * strength * 255))
    lay = Image.new("RGBA", (w, h), color + (0,)); lay.putalpha(a); comp(img, lay, (x0, y0))


def drop(img, box, r, alpha=110, off=(3, 4), blur=4.0, color=(10, 6, 4)):
    x0, y0, x1, y1 = [s(v) for v in box]; w, h = x1 - x0, y1 - y0; p = s(blur * 3)
    lay = Image.new("RGBA", (w + 2 * p, h + 2 * p), (0, 0, 0, 0))
    ImageDraw.Draw(lay).rounded_rectangle([p, p, p + w - 1, p + h - 1], radius=s(r), fill=color + (alpha,))
    comp(img, lay.filter(ImageFilter.GaussianBlur(s(blur))), (x0 - p + s(off[0]), y0 - p + s(off[1])))


# ---- icons ----------------------------------------------------------------------------------------------------------------------

_icons = {}


def icon(id, rot=False, flip=False):
    if (id, rot, flip) not in _icons:
        im = Image.open(ITEM / f"{id}.png").convert("RGBA")
        im = im.crop(im.split()[3].point(lambda a: 255 if a > 8 else 0).getbbox())
        if flip: im = im.transpose(Image.FLIP_LEFT_RIGHT)   # an enemy's item faces left
        if rot: im = im.rotate(-90, expand=True)   # a quarter turn clockwise, as the game turns
        _icons[(id, rot, flip)] = im
    return _icons[(id, rot, flip)]


def icon_of(i): return icon(i["id"], i.get("rot", False), i.get("flip", False))


def fit(im, w, h):
    r = min(w / im.width, h / im.height)
    return im.resize((max(1, round(im.width * r)), max(1, round(im.height * r))), Image.LANCZOS)


def silhouette(im, color, grow=0, blur=0.0, alpha=255):
    a = im.split()[3]
    if grow: a = a.filter(ImageFilter.MaxFilter(2 * grow + 1))
    if blur: a = a.filter(ImageFilter.GaussianBlur(blur))
    if alpha != 255: a = a.point(lambda v: v * alpha // 255)
    out = Image.new("RGBA", im.size, color + (0,)); out.putalpha(a); return out


def padded(im, p):
    out = Image.new("RGBA", (im.width + 2 * p, im.height + 2 * p), (0, 0, 0, 0)); out.alpha_composite(im, (p, p)); return out


def mul(im, f):
    r, g, b, a = im.split()
    return Image.merge("RGBA", [c.point(lambda v: min(255, int(v * f))) for c in (r, g, b)] + [a])


def faded(im, f):
    out = im.copy(); out.putalpha(im.split()[3].point(lambda v: int(v * f))); return out


def cursor(img, x, y):
    d = D(img)
    pts = [(0, 0), (0, 19), (4.8, 14.4), (8, 21.4), (11, 20.2), (7.8, 13.3), (13.8, 13.3)]
    d.polygon([(s(x + a), s(y + b)) for a, b in pts], fill=(248, 246, 240), outline=(12, 10, 8))
    line(d, [(x + a, y + b) for a, b in pts + [pts[0]]], (12, 10, 8), 1.2)


# ---- the scene ------------------------------------------------------------------------------------------------------------------

SHAPES = {"longsword": (3, 1), "fire_staff": (3, 1), "healing_staff": (3, 1), "mace": (3, 1), "rusty_blade": (3, 1),
          "ward_charm": (3, 1), "dagger": (2, 1), "ember_flask": (2, 1), "herb_pouch": (2, 1), "buckler": (1, 1),
          "longbow": (3, 2), "spear": (3, 2)}
CATEGORY = {"buckler": "Armor", "herb_pouch": "Support", "ward_charm": "Support", "ember_flask": "Attack"}
NAMES = {"longsword": "롱소드", "fire_staff": "화염 지팡이", "healing_staff": "치유의 지팡이", "mace": "메이스", "rusty_blade": "녹슨 칼",
         "ward_charm": "수호 부적", "dagger": "단검", "ember_flask": "불씨 플라스크", "herb_pouch": "약초 주머니", "buckler": "버클러",
         "longbow": "장궁", "spear": "창"}
BAGS = {"leather_pack": (3, 3, "가죽 배낭"), "belt_pouch": (2, 1, "허리 주머니"), "leather_pouch": (3, 1, "가죽 주머니")}
LEATHER = {"leather_pack": (0x60, 0x40, 0x26), "belt_pouch": (0x6E, 0x36, 0x2A), "leather_pouch": (0x46, 0x52, 0x3A)}  # UiPalette.Bag
STARS = {"bronze": 1, "silver": 2, "gold": 3}
VIOLET = (0xB4, 0x8C, 0xE6)


def cat(id): return CATEGORY.get(id, "Weapon")


def it(id, x=0, y=0, tier="common", rot=False, fat=False, sel=False):
    w, h = SHAPES[id]
    if rot: w, h = h, w
    return dict(id=id, x=x, y=y, w=w, h=h, tier=tier, rot=rot, fat=fat, sel=sel)


def bag(id, x=0, y=0):
    w, h, _ = BAGS[id]; return dict(id=id, x=x, y=y, w=w, h=h)


def cells(o): return {(o["x"] + i, o["y"] + j) for i in range(o["w"]) for j in range(o["h"])}


PACK = bag("leather_pack")
MERCS = [
    dict(num=4, name="미라", fat=0, bags=[PACK], items=[it("fire_staff"), it("ember_flask", 0, 1, "bronze")]),
    dict(num=3, name="엘라", fat=0, bags=[PACK], items=[it("healing_staff")]),
    dict(num=2, name="카이", fat=1, bags=[PACK, bag("belt_pouch", 0, 3)],
         items=[it("longsword", 0, 0, "bronze"), it("dagger", 0, 1, "bronze", fat=True), it("buckler", 2, 1, "silver"), it("herb_pouch", 0, 3)]),
    dict(num=1, name="로언", fat=2, bags=[PACK, bag("leather_pouch", 0, 3)],
         items=[it("longsword", 0, 0, "silver"), it("herb_pouch", 0, 1, "gold"), it("dagger", 2, 1, rot=True, fat=True),
                it("ember_flask", 0, 2), it("mace", 0, 3, "bronze")]),
]
HELD = dict(it("longbow", 0, 1), kind="fits")            # from the inventory, over Ella's bag: it fits
INVENTORY = [it("spear"), it("rusty_blade", tier="bronze"), it("mace"), it("ward_charm"), it("herb_pouch"), it("buckler", tier="silver")]
USED, HELD_AREA, ALL = 18, 6, 30                          # 18 lying in it + the held longbow's 6, of 30
CARD = dict(id="longbow", name="장궁", grade=8, tier="common", kind="무기 장비", size="3×2", cooldown="3.0초",
            rows="뒤에서 3번째 자리까지만 발동", effect="뒤의 적 2명에게 피해 9", fatigue="전투마다 피로 +1")
DETAIL = dict(num=2, name="카이", fat=1, bags=[PACK, bag("belt_pouch", 0, 3)],
              items=[it("longsword", 0, 0, "bronze"), it("dagger", 0, 1, "bronze", fat=True), it("buckler", 2, 1, "silver"), it("herb_pouch", 0, 3)])


DARK, FLASH = 0.4, 0.3                   # UiPalette.CooldownDark, FireBrighten (round 48, 안 2)


def revealed(art, frac, pad, w):
    """The cooldown as today (round 48, 안 2): the item's art dark, its own colours back from the left with a sharp edge.
    pad and w in the layer's pixels."""
    out = mul(art, DARK); cut = pad + int(round(w * frac))
    if cut > 0: out.paste(art.crop((0, 0, cut, art.height)), (0, 0))
    return out


def greyed(lay, f=0.55):
    """An item that cannot be used where its owner stands, in battle: grey and dim."""
    r, g, b, a = lay.split(); l = Image.merge("RGB", (r, g, b)).convert("L").point(lambda v: int(v * f))
    return Image.merge("RGBA", (l, l, l, a))


def joined(under, art, over):
    out = under.copy(); out.alpha_composite(art); out.alpha_composite(over); return out


class Style:
    """What every look shares: laying out a board, its items, the ghost and the held bag. A look draws the pieces."""
    key = title = ""

    def board(self, img, x0, y0, m, ghost=None, bag_held=None, battle=None):
        self.frame(img, x0, y0, m["bags"], bag_held is not None)
        self.bags(img, x0, y0, m["bags"])
        hit = [i for i in m["items"] if ghost and cells(i) & cells(ghost)]
        for i in m["items"]:
            mode = "swap" if ghost and ghost["kind"] == "swap" and i in hit else None
            if battle is not None:
                under, art, over = self.parts(i, battle=True)
                if i.get("unusable"): under, art = greyed(under), greyed(art)
                elif i["id"] not in battle: pass       # at rest (after the win): as it is
                elif battle[i["id"]] >= 1:   # firing: the whole item a moment brighter, a light round it
                    art = mul(art, 1 + FLASH); under.alpha_composite(silhouette(art, (255, 236, 190), grow=s(2), blur=s(5), alpha=170))
                else: art = revealed(art, battle[i["id"]], s(PAD), s(span(i["w"])))
                lay = joined(under, art, over)
            else: lay = self.piece(i, mode=mode)
            X, Y = cell(x0, y0, i["x"], i["y"])
            comp(img, lay, (s(X - PAD), s(Y - PAD)))
        if ghost: self.ghost(img, x0, y0, ghost, hit)
        if bag_held: self.held_bag(img, x0, y0, bag_held)

    def piece(self, i, mode=None, battle=False, q=SQ, **kw):
        lay = joined(*self.parts(i, mode=mode, battle=battle, q=q, **kw))
        return self.swapped(lay) if mode == "swap" else lay

    def swapped(self, lay): return lay

    def layers(self, i, q=SQ):
        W, H = span(i["w"], q), span(i["h"], q); P = s(PAD); size = (s(W) + 2 * P, s(H) + 2 * P)
        return W, H, P, [Image.new("RGBA", size, (0, 0, 0, 0)) for _ in range(3)]

    def ghost_label(self, g, hit):
        if g["kind"] == "swap": return f"{NAMES[hit[0]['id']]} → 인벤토리"
        if g["kind"] == "refused": return "둘과 겹쳐 놓을 수 없음"
        return None

    def screen(self):
        img = Image.open(SHOTS / "ko_17_map_inventory_selected.png").convert("RGB").resize((s(1920), s(1080)), Image.LANCZOS)
        self.panel(img, LEFT, decor=True)
        for x0, m in zip(COLS, MERCS):
            self.board(img, x0, TOP, m, ghost=HELD if m["name"] == "엘라" else None)
            self.plate(img, x0, TOP - 24, m)
        self.inventory(img)
        detail_line(img)
        gx, gy = cell(COLS[1], TOP, HELD["x"], HELD["y"])
        cursor(img, gx + span(3) / 2 + 22, gy + span(2) / 2 + 8)
        return img

    def states(self):
        """Each state on one board (the detail sheet): 1x 236 x 488, the board at (41, 44)."""
        out = []
        X0, Y0, W, H = 41, 46, 236, 492

        def one(label, m=DETAIL, **kw):
            c = Image.new("RGB", (s(W), s(H)), (0, 0, 0)); self.panel(c, (0, 0, W, H))
            self.board(c, X0, Y0, m, **kw); self.plate(c, X0, Y0 - 24, m); out.append((label, c))
        one("평소 · 롱소드를 고름", dict(DETAIL, items=[dict(i, sel=i["id"] == "longsword") for i in DETAIL["items"]]))
        one("든 것이 맞는 자리", ghost=dict(it("ember_flask", 0, 2), kind="fits"))
        one("하나와 겹침 → 그것이 인벤토리로", ghost=dict(it("ember_flask", 0, 1), kind="swap"))
        one("둘과 겹침 → 놓을 수 없음", ghost=dict(it("longbow", 0, 1), kind="refused"))
        one("가방을 든 동안 (틀의 빈 자리)", bag_held=bag("leather_pouch", 0, 5))
        one("전투: 쿨다운 빛 · 회색은 이 열에서 못 씀", dict(DETAIL, items=[dict(i, unusable=i["id"] == "buckler") for i in DETAIL["items"]]),
            battle={"longsword": 0.7, "dagger": 0.3, "herb_pouch": 1.0})
        return out


def detail_line(img):
    """The panel's line under the node's name says what the hand holds (today's look, not part of the mockup's question)."""
    d = D(img); d.rectangle(B(998, 906, 1886, 964), fill=(27, 31, 40))
    segs = [("장궁 · 등급 8 — 무기 장비 / 크기 3×2 / 쿨다운 3.0초 / 뒤에서 3번째 자리까지만 발동 / 뒤의 적 2명에게 피해 9 / ", (235, 235, 230)),
            ("전투마다 피로 +1", VIOLET)]
    x = 1001
    for t, c in segs:
        text(d, (x, 924), t, font(17), c, "ls"); x += width_of(t, font(17))


# =================================================================================================================================
# 1. Diablo II: carved grey stone, black wells with thin grey lines, the items on a dark blue, a black tooltip with the rarity
#    colours (white, blue, yellow, gold), small-caps serif titles.
# =================================================================================================================================

class Diablo(Style):
    key, title = "D2", "1. 디아블로 2"
    BONE = (205, 192, 160); WHITE = (238, 238, 238); BLUE = (112, 112, 255); YELLOW = (255, 255, 110); GOLD = (199, 179, 119)
    RED = (226, 64, 52); GREY = (128, 128, 128)
    RARITY = {"common": (238, 238, 238), "bronze": (112, 112, 255), "silver": (255, 255, 110), "gold": (199, 179, 119)}
    INV_TITLE = "인 벤 토 리"

    def f(self, px): return myung(px)

    def stone(self, img, box, base=(58, 56, 53)):
        x0, y0, x1, y1 = box
        fill_tex(img, box, shade(base, 0.72), amp=26, coarse=10, blend=0.6)
        band = 13
        fill_tex(img, (x0 + band, y0 + band, x1 - band, y1 - band), base, amp=30, coarse=7, blend=0.65)
        x0b, y0b, x1b, y1b = [s(v) for v in (x0 + band, y0 + band, x1 - band, y1 - band)]
        n = Image.effect_noise((max(2, (x1b - x0b) // 90), max(2, (y1b - y0b) // 90)), 60).resize((x1b - x0b, y1b - y0b), Image.BICUBIC)
        lay = Image.new("RGBA", n.size, (12, 11, 10, 0)); lay.putalpha(n.point(lambda v: max(0, min(70, (v - 124) * 2))))
        comp(img, lay, (x0b, y0b))
        d = D(img)
        for k in range(0, int(x1 - x0) - 40, 120):
            for yy in (y0 + band / 2, y1 - band / 2):
                d.ellipse(B(x0 + 40 + k - 2.5, yy - 2.5, x0 + 40 + k + 2.5, yy + 2.5), fill=(24, 23, 22), outline=(122, 114, 102))
        for o, (a, b) in enumerate([((132, 126, 116), (12, 12, 12)), ((92, 88, 82), (26, 25, 24))]):
            o *= 1.5
            line(d, [(x0 + o, y1 - o - 1), (x0 + o, y0 + o), (x1 - o - 1, y0 + o)], a, 1.5)
            line(d, [(x0 + o, y1 - o - 1), (x1 - o - 1, y1 - o - 1), (x1 - o - 1, y0 + o)], b, 1.5)
        g = 13
        line(d, [(x0 + g, y1 - g), (x0 + g, y0 + g), (x1 - g, y0 + g)], (12, 12, 12), 2)
        line(d, [(x0 + g, y1 - g), (x1 - g, y1 - g), (x1 - g, y0 + g)], (116, 110, 100), 1.5)
        for cx, cy in [(x0 + g, y0 + g), (x1 - g, y0 + g), (x0 + g, y1 - g), (x1 - g, y1 - g)]:
            d.polygon([(s(cx), s(cy - 11)), (s(cx + 11), s(cy)), (s(cx), s(cy + 11)), (s(cx - 11), s(cy))], fill=(30, 29, 28), outline=(130, 122, 108))
            d.polygon([(s(cx), s(cy - 5)), (s(cx + 5), s(cy)), (s(cx), s(cy + 5)), (s(cx - 5), s(cy))], fill=(120, 34, 26))
        vignette(img, box, (0, 0, 0), 0.5, 0.45)

    def panel(self, img, box, decor=False): self.stone(img, box)

    def plate(self, img, x, y, m):
        d = D(img)
        d.rectangle(B(x - 3, y, x + 157, y + 19), fill=(14, 13, 12))
        line(d, [(x - 3, y + 19), (x + 157, y + 19), (x + 157, y)], (104, 98, 90), 1)
        text(d, (x + 3, y + 10), f"{['', 'I', 'II', 'III', 'IV'][m['num']]}  {m['name']}", self.f(13), self.RED if m.get("enemy") else self.BONE, "lm")
        if m.get("fat"): text(d, (x + 153, y + 10), f"피로 +{m['fat']}", font(11), VIOLET, "rm")

    def well(self, img, X, Y, w, h, q=SQ, rim_tint=(34, 33, 31), rim=5):
        """A black well cut into the stone: a recessed rim, thin grey lines, black squares."""
        d = D(img); W, H = span(w, q), span(h, q)
        d.rectangle(B(X - rim, Y - rim, X + W + rim, Y + H + rim), fill=mix((34, 33, 31), rim_tint, 0.45))
        line(d, [(X - rim, Y + H + rim), (X - rim, Y - rim), (X + W + rim, Y - rim)], (10, 10, 10), 1.5)
        line(d, [(X - rim, Y + H + rim), (X + W + rim, Y + H + rim), (X + W + rim, Y - rim)], (112, 106, 98), 1.5)

    def squares(self, img, X, Y, w, h, q=SQ):
        d = D(img)
        d.rectangle(B(X, Y, X + span(w, q), Y + span(h, q)), fill=(56, 56, 60))
        for i in range(w):
            for j in range(h):
                cx, cy = cell(X, Y, i, j, q)
                d.rectangle(B(cx, cy, cx + q, cy + q), fill=(9, 9, 11))
                line(d, [(cx, cy + 0.5), (cx + q, cy + 0.5)], (18, 18, 21), 1)

    def frame(self, img, x0, y0, bags, holding):
        if not holding: return
        d = D(img); used = set().union(*[cells(b) for b in bags])
        for gx in range(3):
            for gy in range(8):
                if (gx, gy) in used: continue
                cx, cy = cell(x0, y0, gx, gy)
                d.rectangle(B(cx, cy, cx + SQ, cy + SQ), fill=(40, 39, 37))
                line(d, [(cx, cy + SQ), (cx, cy), (cx + SQ, cy)], (14, 14, 14), 1)
                line(d, [(cx, cy + SQ), (cx + SQ, cy + SQ), (cx + SQ, cy)], (96, 92, 86), 1)

    def bags(self, img, x0, y0, bags):
        for b in bags: self.well(img, *cell(x0, y0, b["x"], b["y"]), b["w"], b["h"], rim_tint=LEATHER[b["id"]])
        for b in bags: self.squares(img, *cell(x0, y0, b["x"], b["y"]), b["w"], b["h"])

    def parts(self, i, mode=None, battle=False, q=SQ):
        W, H, P, (under, art, over) = self.layers(i, q)
        bg = (54, 72, 156, 215) if i.get("sel") else (96, 82, 28, 215) if mode == "swap" else (22, 32, 88, 205)
        ImageDraw.Draw(under).rectangle([P, P, P + s(W) - 1, P + s(H) - 1], fill=bg)
        ic = fit(icon_of(i), s(W - 6), s(H - 6)); xy = (P + (s(W) - ic.width) // 2, P + (s(H) - ic.height) // 2)
        art.alpha_composite(silhouette(ic, (0, 0, 0), blur=s(1.5), alpha=170), (xy[0] + s(1.5), xy[1] + s(2)))
        art.alpha_composite(ic, xy)
        d = D(over)
        for k in range(STARS.get(i["tier"], 0)):
            cx, cy = PAD + 8 + 10 * k, PAD + H - 8
            star(d, cx, cy, 5.2, (0, 0, 0)); star(d, cx, cy, 4.2, self.RARITY[i["tier"]])
        if i.get("fat") and not battle: text(d, (PAD + W - 3, PAD + 3), "+1", font(12), VIOLET, "ra", stroke=s(1), stroke_fill=(0, 0, 0))
        return under, art, over

    def tag(self, img, cx, y, t, color):
        d = D(img); w = width_of(t, self.f(13)) + 14
        d.rectangle(B(cx - w / 2, y, cx + w / 2, y + 21), fill=(0, 0, 0, 225), outline=(70, 70, 70))
        text(d, (cx, y + 11), t, self.f(13), color, "mm")

    def ghost(self, img, x0, y0, g, hit):
        col = (176, 30, 30, 125) if g["kind"] == "refused" else (26, 150, 48, 115)
        d = D(img)
        for (gx, gy) in cells(g):
            cx, cy = cell(x0, y0, gx, gy); d.rectangle(B(cx, cy, cx + SQ, cy + SQ), fill=col)
        X, Y = cell(x0, y0, g["x"], g["y"]); W, H = span(g["w"]), span(g["h"])
        ic = fit(icon_of(g), s(W - 6), s(H - 6)); xy = (s(X) + (s(W) - ic.width) // 2, s(Y) + (s(H) - ic.height) // 2)
        comp(img, silhouette(ic, (0, 0, 0), blur=s(3), alpha=150), (xy[0] + s(4), xy[1] + s(5))); comp(img, ic, xy)
        lab = self.ghost_label(g, hit)
        if lab: self.tag(img, X + W / 2, Y - 24, lab, self.RED if g["kind"] == "refused" else self.WHITE)

    def held_bag(self, img, x0, y0, b):
        lay = Image.new("RGBA", img.size, (0, 0, 0, 0)); self.bags(lay, x0, y0, [b]); comp(img, faded(lay, 0.85), (0, 0))
        d = D(img)
        for (gx, gy) in cells(b):
            cx, cy = cell(x0, y0, gx, gy); d.rectangle(B(cx, cy, cx + SQ, cy + SQ), fill=(26, 150, 48, 90))
        X, Y = cell(x0, y0, b["x"], b["y"]); self.tag(img, X + span(3) / 2, Y - 25, "가죽 주머니 · 칸 +3", self.WHITE)

    def inventory(self, img):
        x0, y0, x1, y1 = RIGHT; cx = (x0 + x1) / 2
        self.stone(img, RIGHT, base=(56, 54, 51))
        d = D(img)
        d.rectangle(B(cx - 150, y0 + 16, cx + 150, y0 + 58), fill=(14, 13, 12))
        line(d, [(cx - 150, y0 + 58), (cx + 150, y0 + 58), (cx + 150, y0 + 16)], (110, 104, 96), 1.5)
        line(d, [(cx - 150, y0 + 58), (cx - 150, y0 + 16), (cx + 150, y0 + 16)], (6, 6, 6), 1.5)
        text(d, (cx, y0 + 38), self.INV_TITLE, self.f(25), self.GOLD, "mm")
        text(d, (x1 - 28, y0 + 38), f"{USED + HELD_AREA} / {ALL}칸", self.f(17), self.BONE, "rm")
        q = 66; gx0 = cx - span(10, q) / 2; gy0 = y0 + 92
        self.well(img, gx0, gy0, 10, 3, q=q, rim=6); self.squares(img, gx0, gy0, 10, 3, q=q)
        lay = {"spear": (0, 0), "rusty_blade": (0, 2), "mace": (3, 2), "ward_charm": (6, 0), "herb_pouch": (6, 1), "buckler": (8, 1)}
        for i in INVENTORY:
            gx, gy = lay[i["id"]]; X, Y = cell(gx0, gy0, gx, gy, q)
            comp(img, self.piece(i, q=q), (s(X - PAD), s(Y - PAD)))
        # the longbow's place (3, 0) is empty: the hand holds it
        # the coin plate under the grid, as Diablo's gold
        by = gy0 + span(3, q) + 18
        d.rectangle(B(gx0, by, gx0 + 150, by + 32), fill=(14, 13, 12)); line(d, [(gx0, by + 32), (gx0 + 150, by + 32), (gx0 + 150, by)], (104, 98, 90), 1)
        d.ellipse(B(gx0 + 10, by + 7, gx0 + 28, by + 25), fill=(196, 160, 60), outline=(90, 70, 20))
        text(d, (gx0 + 140, by + 16), "3", self.f(18), self.GOLD, "rm")
        text(d, (gx0 + span(10, q), by + 16), "자리는 들어온 차례로 정해집니다", self.f(14), (150, 142, 124), "rm")
        self.card(img, cx, by + 62)
        text(d, (cx, y1 - 30), "아이템을 누르면 손에 듭니다 · 왼쪽 보드의 칸을 누르면 놓입니다", self.f(15), (150, 142, 124), "mm")

    def card(self, img, cx, y):
        """Diablo's tooltip: black, see-through, the lines centred; the name in its rarity colour, the effects blue."""
        c = CARD
        rows = [(c["name"], self.RARITY[c["tier"]], 22), (f"등급 {c['grade']}", self.WHITE, 16), (f"{c['kind']} · 크기 {c['size']}", self.WHITE, 16),
                (f"쿨다운: {c['cooldown']}", self.WHITE, 16), (c["rows"], self.WHITE, 16), (c["effect"], self.BLUE, 17),
                (c["fatigue"], VIOLET, 16), ("R · 우클릭 · 휠: 돌리기   Esc: 내려놓기", self.GREY, 14)]
        h = 22 + sum(r[2] + 11 for r in rows); w = 470
        d = D(img); d.rectangle(B(cx - w / 2, y, cx + w / 2, y + h), fill=(0, 0, 0, 215), outline=(64, 64, 64))
        yy = y + 18
        for t, col, px in rows:
            text(d, (cx, yy + px / 2), t, self.f(px), col, "mm"); yy += px + 11
            if t.startswith("R ·"): pass


# =================================================================================================================================
# 2. Backpack Battles: a sepia paper room drawn in ink, a wooden frame; bags are leather pieces with thick ink lines and stitches,
#    their squares sunk; items drawn over their squares with a thick ink outline and a shadow, no ground; parchment ribbons; the
#    storage crate; a parchment card.
# =================================================================================================================================

class Backpack(Style):
    key, title = "BB", "2. 백팩 배틀즈"
    INK = (42, 25, 14); PAPER = (204, 174, 126); PARCH = (238, 222, 186); WOOD = (112, 74, 42)
    LEATHER = {"leather_pack": (150, 98, 54), "belt_pouch": (162, 76, 54), "leather_pouch": (110, 122, 74)}
    TIER = {"bronze": (214, 140, 70), "silver": (170, 200, 236), "gold": (255, 206, 70)}

    def paper(self, img, box, frame=10):
        x0, y0, x1, y1 = box
        if frame: fill_tex(img, box, self.WOOD, amp=20, streak=True)
        inner = (x0 + frame, y0 + frame, x1 - frame, y1 - frame)
        fill_tex(img, inner, self.PAPER, amp=10, coarse=9, blend=0.5)
        vignette(img, inner, (96, 60, 28), 0.6, 0.42)
        d = D(img)
        if frame: d.rectangle(B(*box), outline=self.INK, width=s(2))
        d.rectangle(B(*inner), outline=self.INK, width=s(2))
        return inner

    def sketch(self, img, x, y, w):
        """A shelf with jars and a book in faded ink (Backpack Battles' room behind the bags)."""
        lay = Image.new("RGBA", img.size, (0, 0, 0, 0)); d = ImageDraw.Draw(lay); ink = self.INK + (70,)
        line(d, [(x, y), (x + w, y)], ink, 2); line(d, [(x + 8, y), (x + 14, y + 12)], ink, 1.5); line(d, [(x + w - 8, y), (x + w - 14, y + 12)], ink, 1.5)
        d.rounded_rectangle(B(x + 6, y - 30, x + 22, y), radius=s(4), outline=ink, width=s(1.5))
        d.ellipse(B(x + 28, y - 22, x + 46, y), outline=ink, width=s(1.5)); line(d, [(x + 34, y - 22), (x + 34, y - 30), (x + 40, y - 30), (x + 40, y - 22)], ink, 1.5)
        d.rectangle(B(x + 52, y - 26, x + 60, y), outline=ink, width=s(1.5)); d.rectangle(B(x + 60, y - 22, x + 67, y), outline=ink, width=s(1.5))
        comp(img, lay, (0, 0))

    def panel(self, img, box, decor=False):
        self.paper(img, box, frame=10 if decor else 0)
        if decor:
            self.sketch(img, 24, 760, 80); self.sketch(img, 24, 880, 80); self.sketch(img, 870, 700, 72)

    def ribbon(self, img, cx, y, t, w=None, px=14, ink=None):
        d = D(img); w = w or width_of(t, myung(px)) + 26; x0, x1 = cx - w / 2, cx + w / 2; y1 = y + 22
        tail = shade(self.PARCH, 0.82)
        for sgn, xe in ((-1, x0), (1, x1)):
            pts = [(xe, y + 4), (xe + sgn * 13, y + 4), (xe + sgn * 7, y + 13), (xe + sgn * 13, y1 + 2), (xe, y1 + 2)]
            d.polygon([(s(a), s(b)) for a, b in pts], fill=tail, outline=self.INK)
        rrect(d, (x0, y, x1, y1), 5, fill=self.PARCH, outline=self.INK, w=1.5)
        ink = ink or self.INK; text(d, (cx, y + 11.5), t, myung(px), ink, "mm", stroke=1, stroke_fill=ink)

    def plate(self, img, x, y, m):
        t = f"{m['num']} · {m['name']}"
        self.ribbon(img, x + 77, y - 4, t, w=max(96, width_of(t, myung(14)) + 26), ink=(140, 32, 20) if m.get("enemy") else None)
        if m.get("fat"):
            d = D(img); cx, cy = x + 146, y + 7
            d.ellipse(B(cx - 11, cy - 11, cx + 11, cy + 11), fill=(148, 104, 196), outline=self.INK, width=s(1.5))
            text(d, (cx, cy + 0.5), f"+{m['fat']}", font(11), (255, 250, 240), "mm")

    def frame(self, img, x0, y0, bags, holding):
        if not holding: return
        d = D(img); used = set().union(*[cells(b) for b in bags])
        for gx in range(3):
            for gy in range(8):
                if (gx, gy) not in used:
                    cx, cy = cell(x0, y0, gx, gy); dashed(d, (cx + 3, cy + 3, cx + SQ - 3, cy + SQ - 3), self.INK + (130,), 1.5, 5, 4)

    def bags(self, img, x0, y0, bags):
        for b in bags:
            X, Y = cell(x0, y0, b["x"], b["y"]); W, H = span(b["w"]), span(b["h"]); m = 5
            body = (X - m, Y - m, X + W + m, Y + H + m); lea = self.LEATHER[b["id"]]
            drop(img, body, 9, alpha=120, off=(3, 4), blur=3)
            fill_tex(img, body, lea, amp=12, coarse=7, radius=9)
            d = D(img)
            rrect(d, (body[0] + 3, body[1] + 3, body[2] - 3, body[1] + 12), 5, fill=lighten(lea, 30) + (60,))
            dashed(d, (body[0] + 3.5, body[1] + 3.5, body[2] - 3.5, body[3] - 3.5), lighten(lea, 78), 1.5, 5, 3.5)
            for i in range(b["w"]):
                for j in range(b["h"]):
                    cx, cy = cell(X, Y, i, j); c = (cx + 3, cy + 3, cx + SQ - 3, cy + SQ - 3)
                    rrect(d, c, 6, fill=shade(lea, 0.66), outline=self.INK + (210,), w=1.2)
                    line(d, [(c[0] + 3, c[3] - 5), (c[0] + 3, c[1] + 3), (c[2] - 5, c[1] + 3)], shade(lea, 0.45) + (200,), 2.5)
                    line(d, [(c[0] + 6, c[3] - 2.5), (c[2] - 2.5, c[3] - 2.5), (c[2] - 2.5, c[1] + 6)], lighten(lea, 22) + (160,), 1)
            rrect(d, body, 9, outline=self.INK, w=2.5)
            if b["id"] == "leather_pack":
                for rx in (body[0] + 9, body[2] - 9):
                    d.ellipse(B(rx - 3.5, body[1] + 5.5, rx + 3.5, body[1] + 12.5), fill=(214, 176, 92), outline=self.INK)

    def swapped(self, lay): return faded(lay, 0.55)

    def parts(self, i, mode=None, battle=False, q=SQ, tilt=0):
        W, H, P, (under, art, over) = self.layers(i, q)
        ic = fit(icon_of(i), s(W + 2), s(H + 2))
        if tilt: ic = ic.rotate(tilt, expand=True, resample=Image.BICUBIC)
        ic = padded(ic, s(4)); ol = silhouette(ic, self.INK, grow=s(2.2)); ol.alpha_composite(ic); ic = ol
        xy = (P + (s(W) - ic.width) // 2, P + (s(H) - ic.height) // 2)
        if i["tier"] in self.TIER: under.alpha_composite(silhouette(ic, self.TIER[i["tier"]], grow=s(3), blur=s(5), alpha=200), xy)
        if i.get("sel"): under.alpha_composite(silhouette(ic, (255, 236, 130), grow=s(3), blur=s(2.5), alpha=255), xy)
        under.alpha_composite(silhouette(ic, (30, 16, 6), blur=s(2.5), alpha=120), (xy[0] + s(3), xy[1] + s(4)))
        art.alpha_composite(ic, xy)
        d = D(over)
        n = STARS.get(i["tier"], 0)
        if n:
            tx, ty = PAD + 2, PAD + H - 15; tw = 7 + 11 * n
            rrect(d, (tx, ty, tx + tw, ty + 15), 4, fill=self.PARCH, outline=self.INK, w=1.2)
            for k in range(n): star(d, tx + 9 + 11 * k, ty + 7.8, 4.6, self.TIER[i["tier"]], outline=self.INK)
        if i.get("fat") and not battle:
            cx, cy = PAD + W - 7, PAD + 7
            d.ellipse(B(cx - 8, cy - 8, cx + 8, cy + 8), fill=(148, 104, 196), outline=self.INK, width=s(1.2))
            text(d, (cx, cy + 0.5), "+1", font(9), (255, 250, 240), "mm")
        return under, art, over

    def note(self, img, cx, y, t, color=None):
        d = D(img); w = width_of(t, myung(13)) + 18
        drop(img, (cx - w / 2, y, cx + w / 2, y + 22), 4, alpha=90, off=(2, 3), blur=2)
        rrect(d, (cx - w / 2, y, cx + w / 2, y + 22), 4, fill=self.PARCH, outline=self.INK, w=1.5)
        text(d, (cx, y + 11.5), t, myung(13), color or self.INK, "mm", stroke=1, stroke_fill=color or self.INK)

    def ghost(self, img, x0, y0, g, hit):
        wash = {"fits": (150, 214, 92, 150), "swap": (250, 214, 90, 160), "refused": (232, 92, 70, 165)}[g["kind"]]
        d = D(img)
        for (gx, gy) in cells(g):
            cx, cy = cell(x0, y0, gx, gy); rrect(d, (cx + 3, cy + 3, cx + SQ - 3, cy + SQ - 3), 6, fill=wash, outline=self.INK, w=1.2)
        X, Y = cell(x0, y0, g["x"], g["y"]); W, H = span(g["w"]), span(g["h"])
        lay = self.piece(dict(g, tier="common")); P = s(PAD)
        comp(img, silhouette(lay, (30, 16, 6), blur=s(6), alpha=120), (s(X - PAD + 9), s(Y - PAD + 12)))
        comp(img, lay, (s(X - PAD - 4), s(Y - PAD - 7)))
        lab = self.ghost_label(g, hit)
        if lab: self.note(img, X + W / 2, Y - 30, lab, (150, 30, 20) if g["kind"] == "refused" else None)

    def held_bag(self, img, x0, y0, b):
        lay = Image.new("RGBA", img.size, (0, 0, 0, 0)); self.bags(lay, x0, y0, [b]); comp(img, faded(lay, 0.92), (s(-4), s(-6)))
        X, Y = cell(x0, y0, b["x"], b["y"]); self.note(img, X + span(3) / 2, Y - 34, "가죽 주머니 · 칸 +3")

    def crate(self, img, box):
        x0, y0, x1, y1 = box; d = D(img)
        drop(img, box, 4, alpha=130, off=(5, 7), blur=5)
        fill_tex(img, box, (88, 56, 30), amp=16, streak=True)
        for yy in range(int(y0) + 46, int(y1), 46): line(d, [(x0 + 14, yy), (x1 - 14, yy)], self.INK + (150,), 1.5)
        for xx in (x0, x1 - 16): fill_tex(img, (xx, y0, xx + 16, y1), (132, 90, 52), amp=14, streak=True); d.rectangle(B(xx, y0, xx + 16, y1), outline=self.INK, width=s(2))
        d.rectangle(B(*box), outline=self.INK, width=s(2.5))

    def lip(self, img, box, t):
        x0, y0, x1, y1 = box; d = D(img)
        fill_tex(img, box, (150, 104, 60), amp=16, streak=True)
        line(d, [(x0, (y0 + y1) / 2), (x1, (y0 + y1) / 2)], self.INK + (140,), 1.5)
        d.rectangle(B(*box), outline=self.INK, width=s(2.5))
        text(d, ((x0 + x1) / 2, (y0 + y1) / 2 + 1), t, myung(22), self.INK + (200,), "mm", stroke=1, stroke_fill=self.INK + (200,))

    def inventory(self, img):
        x0, y0, x1, y1 = RIGHT
        inner = self.paper(img, RIGHT, frame=12)
        self.sketch(img, 1010, 236, 120); self.sketch(img, 1180, 236, 120); self.sketch(img, 1350, 236, 100)
        self.ribbon(img, 1094, y0 + 28, "인벤토리", px=18)
        d = D(img); cx, cy = 1210, y0 + 40
        d.ellipse(B(cx - 13, cy - 13, cx + 13, cy + 13), fill=(232, 188, 70), outline=self.INK, width=s(2)); text(d, (cx, cy + 1), "3", font(14), self.INK, "mm")
        crate = (1006, 300, 1500, 718)
        self.crate(img, crate)
        spots = {"spear": (1030, 330, -4), "rusty_blade": (1220, 326, 3), "buckler": (1404, 336, 0), "mace": (1214, 420, -2),
                 "ward_charm": (1040, 470, 2), "herb_pouch": (1250, 520, -5)}
        for i in INVENTORY:
            x, y, t = spots[i["id"]]
            comp(img, self.piece(i, tilt=t), (s(x - PAD), s(y - PAD)))
        self.lip(img, (1006, 640, 1500, 790), f"보관함 {USED + HELD_AREA}/{ALL}칸")
        self.card(img, (1524, 120, 1880, 520))
        d = D(img)
        text(d, (1702, 560), "아이템을 집어 왼쪽 가방에 놓습니다", myung(15), self.INK, "mm")
        text(d, (1702, 586), "R · 우클릭 · 휠: 돌리기", myung(15), self.INK, "mm")
        text(d, (1702, 612), "Esc: 내려놓기", myung(15), self.INK, "mm")

    def card(self, img, box):
        """Backpack Battles' card: parchment in a dark brown frame, the name, the rarity line, tags, the effects with marks."""
        x0, y0, x1, y1 = box; c = CARD; d = D(img)
        drop(img, box, 8, alpha=140, off=(4, 6), blur=5)
        rrect(d, box, 8, fill=(92, 58, 30), outline=self.INK, w=2)
        fill_tex(img, (x0 + 6, y0 + 6, x1 - 6, y1 - 6), (240, 226, 192), amp=8, coarse=8, radius=5)
        d = D(img); rrect(d, (x0 + 6, y0 + 6, x1 - 6, y1 - 6), 5, outline=self.INK, w=1.2)
        text(d, ((x0 + x1) / 2, y0 + 34), c["name"], myung(26), self.INK, "mm", stroke=1, stroke_fill=self.INK)
        text(d, ((x0 + x1) / 2, y0 + 62), f"일반 · 등급 {c['grade']}", myung(15), (110, 96, 80), "mm")
        tags = ["무기", c["size"], c["cooldown"]]; tx = x0 + 22
        for t in tags:
            w = width_of(t, myung(14)) + 18; rrect(d, (tx, y0 + 80, tx + w, y0 + 104), 11, fill=(222, 198, 150), outline=self.INK, w=1.2)
            text(d, (tx + w / 2, y0 + 92.5), t, myung(14), self.INK, "mm"); tx += w + 8
        line(d, [(x0 + 22, y0 + 118), (x1 - 22, y0 + 118)], self.INK + (110,), 1)
        ic = fit(icon("longbow"), s(150), s(84)); comp(img, silhouette(padded(ic, s(2)), self.INK, grow=s(1.5)), (s((x0 + x1) / 2) - ic.width // 2 - s(2), s(y0 + 128) - s(2)))
        comp(img, ic, (s((x0 + x1) / 2) - ic.width // 2, s(y0 + 128)))
        rows = [("sword", c["effect"], self.INK), ("clock", c["rows"], (90, 74, 58)), ("star", c["fatigue"], (120, 70, 170))]
        yy = y0 + 236
        for mark, t, col in rows:
            mx = x0 + 31
            d.ellipse(B(mx - 10, yy - 10, mx + 10, yy + 10), fill=(222, 198, 150), outline=self.INK, width=s(1))
            if mark == "sword":
                line(d, [(mx - 5, yy + 5), (mx + 5, yy - 5)], self.INK, 1.6); line(d, [(mx - 4, yy + 0.5), (mx - 0.5, yy + 4)], self.INK, 1.6)
            elif mark == "clock":
                d.ellipse(B(mx - 5.5, yy - 5.5, mx + 5.5, yy + 5.5), outline=self.INK, width=s(1.3)); line(d, [(mx, yy - 3.5), (mx, yy), (mx + 3, yy)], self.INK, 1.3)
            else:
                star(d, mx, yy + 0.5, 6, (148, 104, 196), outline=self.INK)
            text(d, (x0 + 50, yy + 1), t, myung(16), col, "lm"); yy += 36
        line(d, [(x0 + 22, y1 - 52), (x1 - 22, y1 - 52)], self.INK + (110,), 1)
        text(d, (x0 + 22, y1 - 30), "보관함에서 든 것", myung(14), (110, 96, 80), "lm")


# =================================================================================================================================
# 3. The expedition hold (recommended): Resident Evil 4's case (the frame is an open case, the held thing lifts), Escape from
#    Tarkov's containers (bags are pieces with their own squares, a short name on the item), Dredge's hold (one piece per item,
#    tinted by its kind, the whole hold's shape always faintly there). Dark, so today's stone and candles stay.
# =================================================================================================================================

class Hold(Style):
    key, title = "C", "3. 권장: 원정 짐칸 (RE4 · 타르코프 · 드레지)"
    BG = (22, 24, 30); CASE = (33, 35, 40); RIM = (70, 52, 38); BRASS = (190, 154, 84); CREAM = (234, 226, 206); DIM = (150, 150, 158)
    KIND = {"Weapon": (62, 76, 100), "Armor": (40, 90, 88), "Support": (64, 94, 56), "Attack": (120, 62, 44)}
    KIND_NAME = {"Weapon": "무기", "Armor": "방어", "Support": "지원", "Attack": "공격"}
    TIER = {"common": (234, 226, 206), "bronze": (0xD5, 0x9A, 0x66), "silver": (0xD3, 0xDB, 0xE4), "gold": (0xF0, 0xC8, 0x5A)}

    def window(self, img, box):
        x0, y0, x1, y1 = box
        fill_tex(img, box, self.BG, amp=7, coarse=10)
        d = D(img)
        d.rectangle(B(*box), outline=(86, 72, 50), width=s(2)); d.rectangle(B(x0 + 4, y0 + 4, x1 - 4, y1 - 4), outline=(44, 40, 36), width=s(1))

    def panel(self, img, box, decor=False):
        """The board panel as one open case (Resident Evil 4's attache case): a leather rim with stitches and brass corners round a
        dark felt lining. The bags lie on the felt; nothing marks the frame outside them."""
        x0, y0, x1, y1 = box; r = 12 if decor else 8
        fill_tex(img, box, (14, 14, 16), amp=4)
        fill_tex(img, (x0 + 2, y0 + 2, x1 - 2, y1 - 2), self.RIM, amp=12, coarse=6, radius=10)
        d = D(img)
        dashed(d, (x0 + 6, y0 + 6, x1 - 6, y1 - 6), lighten(self.RIM, 46), 1.3, 5, 3.5)
        felt = (x0 + r, y0 + r, x1 - r, y1 - r)
        fill_tex(img, felt, (34, 37, 43), amp=7, coarse=3, blend=0.35, radius=5)
        d = D(img)
        for k in range(8): line(d, [(felt[0], felt[1] + k), (felt[2], felt[1] + k)], (0, 0, 0, 110 - 13 * k), 1)
        for k in range(5): line(d, [(felt[0] + k, felt[1]), (felt[0] + k, felt[3])], (0, 0, 0, 80 - 15 * k), 1)
        c = 18 if decor else 12
        for (cx, cy, sx, sy) in [(x0 + 2, y0 + 2, 1, 1), (x1 - 2, y0 + 2, -1, 1), (x0 + 2, y1 - 2, 1, -1), (x1 - 2, y1 - 2, -1, -1)]:
            pts = [(cx, cy), (cx + c * sx, cy), (cx + c * sx, cy + 5 * sy), (cx + 5 * sx, cy + 5 * sy), (cx + 5 * sx, cy + c * sy), (cx, cy + c * sy)]
            d.polygon([(s(a), s(b)) for a, b in pts], fill=self.BRASS, outline=(92, 70, 30))
            d.ellipse(B(cx + 2.5 * sx - 1.6, cy + 2.5 * sy - 1.6, cx + 2.5 * sx + 1.6, cy + 2.5 * sy + 1.6), fill=(96, 72, 30))
        if decor:
            y = 918
            text(d, (26, y - 24), "종류", font(13), self.DIM, "lm")
            for k, (kind, col) in enumerate(self.KIND.items()):
                yy = y + 24 * k; rrect(d, (26, yy - 8, 42, yy + 8), 3, fill=col, outline=shade(col, 0.55), w=1)
                text(d, (50, yy + 0.5), self.KIND_NAME[kind], font(14), self.CREAM, "lm")

    def plate(self, img, x, y, m):
        t = f"{m['num']}  {m['name']}"; w = max(90, width_of(t, font(13)) + 24); x0, x1 = x + 77 - w / 2, x + 77 + w / 2
        enemy = m.get("enemy"); face, rim, hi, ink = ((118, 50, 42), (60, 24, 20), (170, 90, 76), (244, 228, 206)) if enemy else (self.BRASS, (92, 70, 30), (236, 206, 140), (40, 26, 12))
        d = D(img)
        drop(img, (x0, y + 2, x1, y + 22), 3, alpha=140, off=(0, 2), blur=2)
        rrect(d, (x0, y + 2, x1, y + 22), 3, fill=face, outline=rim, w=1)
        line(d, [(x0 + 3, y + 4), (x1 - 3, y + 4)], hi, 1)
        for rx in (x0 + 5, x1 - 5): d.ellipse(B(rx - 1.8, y + 10.2, rx + 1.8, y + 13.8), fill=shade(rim, 1.0))
        text(d, ((x0 + x1) / 2, y + 12.5), t, font(13), ink, "mm")
        if m.get("fat"):
            t = f"피로 +{m['fat']}"; w = width_of(t, font(11)) + 12
            rrect(d, (x + 157 - w, y + 3, x + 157, y + 21), 9, fill=(52, 38, 72), outline=VIOLET, w=1)
            text(d, (x + 157 - w / 2, y + 12.5), t, font(11), VIOLET, "mm")

    def frame(self, img, x0, y0, bags, holding):
        if not holding: return
        d = D(img); used = set().union(*[cells(b) for b in bags])
        for gx in range(3):
            for gy in range(8):
                if (gx, gy) in used: continue
                cx, cy = cell(x0, y0, gx, gy)
                rrect(d, (cx + 1, cy + 1, cx + SQ - 1, cy + SQ - 1), 3, fill=(255, 255, 255, 12))
                dashed(d, (cx + 2, cy + 2, cx + SQ - 2, cy + SQ - 2), self.BRASS + (200,), 1.2, 4, 3)

    def bags(self, img, x0, y0, bags):
        for b in bags:
            X, Y = cell(x0, y0, b["x"], b["y"]); W, H = span(b["w"]), span(b["h"]); lea = LEATHER[b["id"]]
            body = (X - 3, Y - 3, X + W + 3, Y + H + 3)
            drop(img, body, 6, alpha=150, off=(0, 2), blur=2.5, color=(0, 0, 0))
            fill_tex(img, body, lea, amp=12, coarse=6, radius=6)
            d = D(img)
            rrect(d, body, 6, outline=shade(lea, 0.55), w=1.5)
            dashed(d, (body[0] + 1.6, body[1] + 1.6, body[2] - 1.6, body[3] - 1.6), lighten(lea, 80), 1.3, 4, 3)
            for i in range(b["w"]):
                for j in range(b["h"]):
                    cx, cy = cell(X, Y, i, j); c = (cx + 2.5, cy + 2.5, cx + SQ - 2.5, cy + SQ - 2.5)
                    rrect(d, c, 3, fill=(25, 23, 22))
                    line(d, [(c[0], c[3]), (c[0], c[1]), (c[2], c[1])], (8, 8, 8), 1.5)
                    line(d, [(c[0] + 1, c[3]), (c[2], c[3]), (c[2], c[1] + 1)], lighten(lea, 26), 1)

    def swapped(self, lay): return faded(mul(lay, 0.6), 0.8)

    def parts(self, i, mode=None, battle=False, q=SQ):
        W, H, P, (lay, art, over) = self.layers(i, q); base = self.KIND[cat(i["id"])]
        tile = Image.new("RGBA", (s(W), s(H)), (0, 0, 0, 0)); td = ImageDraw.Draw(tile)
        for y in range(s(H)):
            td.line([(0, y), (s(W), y)], fill=mix(lighten(base, 18), shade(base, 0.82), y / max(1, s(H))) + (255,))
        m = Image.new("L", tile.size, 0); ImageDraw.Draw(m).rounded_rectangle([0, 0, s(W) - 1, s(H) - 1], radius=s(5), fill=255); tile.putalpha(m)
        lay.alpha_composite(tile, (P, P))
        d = D(lay)
        rrect(d, (PAD, PAD, PAD + W, PAD + H), 5, outline=shade(base, 0.5), w=1.5)
        line(d, [(PAD + 5, PAD + 1.5), (PAD + W - 5, PAD + 1.5)], lighten(base, 60) + (150,), 1)
        named = i["w"] >= 2 and q >= SQ
        top = 10 if named else 0
        ic = fit(icon_of(i), s(W - 12), s(H - 10 - top))
        xy = (P + (s(W) - ic.width) // 2, P + s(top / 2) + (s(H) - ic.height) // 2)
        art.alpha_composite(silhouette(ic, (0, 0, 0), blur=s(2), alpha=170), (xy[0] + s(2), xy[1] + s(3)))
        art.alpha_composite(ic, xy)
        if named: text(d, (PAD + 5, PAD + 4), NAMES[i["id"]], font(10), self.CREAM + (215,), "la")
        d = D(over)
        n = STARS.get(i["tier"], 0)
        if n:
            col = self.TIER[i["tier"]]; tx, ty = PAD + 3, PAD + H - 17; tw = 6 + 11 * n
            rrect(d, (tx, ty, tx + tw, ty + 14), 3, fill=(20, 12, 10, 235), outline=col, w=1)
            for k in range(n): star(d, tx + 8.5 + 11 * k, ty + 7, 4.4, col)
        if i.get("fat") and not battle:
            rrect(d, (PAD + W - 24, PAD + 3, PAD + W - 3, PAD + 17), 7, fill=(52, 38, 72, 240), outline=VIOLET, w=1)
            text(d, (PAD + W - 13.5, PAD + 10.5), "+1", font(10), VIOLET, "mm")
        if i.get("sel"):
            g = Image.new("RGBA", lay.size, (0, 0, 0, 0)); ImageDraw.Draw(g).rounded_rectangle([P - s(2), P - s(2), P + s(W) + s(1), P + s(H) + s(1)], radius=s(6), outline=self.BRASS + (255,), width=s(2.5))
            lay = Image.alpha_composite(g.filter(ImageFilter.GaussianBlur(s(3))), lay); over.alpha_composite(g)
        return lay, art, over

    def pill(self, img, cx, y, t, color):
        d = D(img); w = width_of(t, font(13)) + 18
        drop(img, (cx - w / 2, y, cx + w / 2, y + 22), 11, alpha=150, off=(0, 2), blur=2, color=(0, 0, 0))
        rrect(d, (cx - w / 2, y, cx + w / 2, y + 22), 11, fill=(18, 18, 22, 240), outline=color, w=1.2)
        text(d, (cx, y + 11.5), t, font(13), color, "mm")

    def ghost(self, img, x0, y0, g, hit):
        col = {"fits": (126, 222, 150), "swap": (240, 200, 90), "refused": (226, 84, 70)}[g["kind"]]
        X, Y = cell(x0, y0, g["x"], g["y"]); W, H = span(g["w"]), span(g["h"])
        lay = Image.new("RGBA", img.size, (0, 0, 0, 0)); d = D(lay)
        for (gx, gy) in cells(g):
            cx, cy = cell(x0, y0, gx, gy); rrect(d, (cx + 1, cy + 1, cx + SQ - 1, cy + SQ - 1), 3, fill=col + (46,))
        rrect(D(lay), (X - 1, Y - 1, X + W + 1, Y + H + 1), 5, outline=col + (255,), w=2)
        comp(img, lay, (0, 0))
        held = faded(self.piece(dict(g, tier="common")), 0.62 if g["kind"] == "refused" else 0.9)
        comp(img, silhouette(held, (0, 0, 0), blur=s(6), alpha=150), (s(X - PAD + 6), s(Y - PAD + 9)))
        comp(img, held, (s(X - PAD - 3), s(Y - PAD - 5)))
        if g["kind"] == "refused":   # the stripes over the held thing, so they read even under it
            m = Image.new("L", img.size, 0); ImageDraw.Draw(m).rectangle(B(X, Y, X + W, Y + H), fill=255)
            h = Image.new("RGBA", img.size, (0, 0, 0, 0)); hd = ImageDraw.Draw(h)
            for k in range(-int(H), int(W), 10): hd.line([(s(X + k), s(Y + H)), (s(X + k + H), s(Y))], fill=col + (150,), width=s(2.5))
            h.putalpha(ImageChops.multiply(h.split()[3], m)); comp(img, h, (0, 0))
            rrect(D(img), (X - 1, Y - 1, X + W + 1, Y + H + 1), 5, outline=col + (255,), w=2)
        lab = self.ghost_label(g, hit)
        if lab: self.pill(img, X + W / 2, Y - 30, lab, col)

    def held_bag(self, img, x0, y0, b):
        lay = Image.new("RGBA", img.size, (0, 0, 0, 0)); self.bags(lay, x0, y0, [b])
        X, Y = cell(x0, y0, b["x"], b["y"]); W, H = span(b["w"]), span(b["h"])
        comp(img, silhouette(lay, (0, 0, 0), blur=s(6), alpha=150), (s(5), s(8))); comp(img, faded(lay, 0.95), (s(-2), s(-4)))
        rrect(D(img), (X - 4, Y - 4, X + W + 4, Y + H + 4), 6, outline=(126, 222, 150), w=2)
        self.pill(img, X + W / 2, Y - 32, "가죽 주머니 · 칸 +3", (126, 222, 150))

    def inventory(self, img):
        x0, y0, x1, y1 = RIGHT
        self.window(img, RIGHT)
        d = D(img)
        text(d, (x0 + 32, y0 + 40), "인벤토리", font(30), self.CREAM, "lm")
        text(d, (x1 - 32, y0 + 42), f"{USED + HELD_AREA} / {ALL}칸", font(20), self.CREAM, "rm")
        px0, py = x0 + 32, y0 + 74; pw = (x1 - x0 - 64 - 29 * 4) / 30
        for k in range(ALL):
            a = px0 + k * (pw + 4); box = (a, py, a + pw, py + 12)
            if k < USED: rrect(d, box, 2, fill=(200, 188, 160))
            elif k < USED + HELD_AREA: rrect(d, box, 2, fill=self.BRASS + (70,), outline=self.BRASS, w=1)
            else: rrect(d, box, 2, fill=(46, 48, 56))
        text(d, (px0, py + 30), "밝은 칸: 들어 있는 것   금 테: 손에 든 장궁(놓으면 빠짐)   어두운 칸: 빈 넓이", font(13), self.DIM, "lm")
        tray = (x0 + 32, y0 + 122, x1 - 32, y0 + 350)
        fill_tex(img, tray, (16, 17, 21), amp=4, radius=6); d = D(img)
        rrect(d, tray, 6, outline=(56, 52, 46), w=1.2)
        for k in range(6): line(d, [(tray[0] + 4, tray[1] + 1 + k), (tray[2] - 4, tray[1] + 1 + k)], (0, 0, 0, 80 - 13 * k), 1)
        flow = [HELD] + INVENTORY; x, y = tray[0] + 22, tray[1] + 24; row_h = 0
        for i in flow:
            W, H = span(i["w"]), span(i["h"])
            if x + W > tray[2] - 18: x, y = tray[0] + 22, y + row_h + 16; row_h = 0
            if i is HELD:
                rrect(d, (x, y, x + W, y + H), 5, fill=(255, 255, 255, 8), outline=self.BRASS + (220,), w=1.5)
                dashed(d, (x + 4, y + 4, x + W - 4, y + H - 4), self.BRASS + (120,), 1, 4, 3)
                text(d, (x + W / 2, y + H / 2), "손에 듦", font(14), self.BRASS, "mm")
            else:
                comp(img, self.piece(i), (s(x - PAD), s(y - PAD)))
            x += W + 16; row_h = max(row_h, H)
        d = D(img)
        text(d, (tray[0] + 4, tray[3] + 20), "인벤토리 안에는 자리가 없습니다. 큰 것부터 차례로 보입니다.", font(14), self.DIM, "lm")
        self.card(img, (x0 + 32, y0 + 398, x0 + 520, y1 - 24))
        hx = x0 + 548; hy = y0 + 412
        text(d, (hx, hy), "손에 든 동안", font(17), self.CREAM, "la"); hy += 36
        for col, t in [((126, 222, 150), "초록 테: 놓이는 자리"), ((240, 200, 90), "금 테: 하나와 겹침 → 그것이 인벤토리로"),
                       ((226, 84, 70), "빨강 빗금: 둘과 겹쳐 놓을 수 없음")]:
            rrect(d, (hx, hy - 1, hx + 22, hy + 17), 3, fill=col + (46,), outline=col, w=1.5); text(d, (hx + 32, hy + 8), t, font(15), self.CREAM, "lm"); hy += 34
        hy += 10
        for t in ["R · 우클릭: 시계 방향으로 돌리기", "휠: 아래 시계 방향, 위 반대", "Esc: 내려놓기"]:
            text(d, (hx, hy + 8), t, font(15), self.DIM, "lm"); hy += 30

    def card(self, img, box):
        """A dark card; its head in the item's kind colour with the icon, the name in its tier's colour; a two-column table."""
        x0, y0, x1, y1 = box; c = CARD; d = D(img); kind = self.KIND[cat(c["id"])]
        drop(img, box, 8, alpha=160, off=(0, 4), blur=5, color=(0, 0, 0))
        rrect(d, box, 8, fill=(30, 32, 38), outline=(96, 80, 54), w=1.5)
        head = (x0 + 1.5, y0 + 1.5, x1 - 1.5, y0 + 92)
        rrect(d, head, 7, fill=kind); d.rectangle(B(head[0], head[3] - 10, head[2], head[3]), fill=kind)
        line(d, [(x0 + 1.5, y0 + 92), (x1 - 1.5, y0 + 92)], (96, 80, 54), 1.5)
        ic = fit(icon(c["id"]), s(120), s(70)); comp(img, silhouette(ic, (0, 0, 0), blur=s(2), alpha=170), (s(x0 + 16) + s(2), s(y0 + 11) + s(3)))
        comp(img, ic, (s(x0 + 16), s(y0 + 11)))
        text(d, (x0 + 150, y0 + 34), c["name"], font(26), self.TIER[c["tier"]], "lm")
        text(d, (x0 + 150, y0 + 66), f"{c['kind']} · {c['size']} · 등급 {c['grade']}", font(15), (214, 214, 220), "lm")
        rows = [("쿨다운", c["cooldown"], self.CREAM), ("발동 자리", c["rows"].replace("까지만 발동", "까지"), self.CREAM),
                ("효과", c["effect"], (255, 244, 214)), ("피로", c["fatigue"].replace("전투마다 피로", "전투마다"), VIOLET)]
        yy = y0 + 124
        for k, v, col in rows:
            text(d, (x0 + 20, yy), k, font(15), self.DIM, "lm"); text(d, (x0 + 116, yy), v, font(16), col, "lm"); yy += 36
        line(d, [(x0 + 20, y1 - 46), (x1 - 20, y1 - 46)], (60, 60, 66), 1)
        text(d, (x0 + 20, y1 - 24), "우클릭: 카드 닫기", font(13), self.DIM, "lm")


# =================================================================================================================================

def sheet(tiles, cols, path, title=None, footer=None, scale=1.0, label=26, bg=(18, 18, 22)):
    ims = [(t, im if scale == 1 else im.resize((int(im.width * scale), int(im.height * scale)), Image.LANCZOS)) for t, im in tiles]
    w, h = ims[0][1].size; gap = 18; th = label + 20; head = 64 if title else 0
    rows = (len(ims) + cols - 1) // cols
    foot = (len(footer) * (label + 10) + 20) if footer else 0
    out = Image.new("RGB", (cols * w + (cols + 1) * gap, head + rows * (h + th + gap) + gap + foot), bg)
    d = ImageDraw.Draw(out); f = ImageFont.truetype(str(PRET), label)
    if title: d.text((gap, 18), title, font=ImageFont.truetype(str(PRET), label + 8), fill=(240, 236, 226))
    for k, (t, im) in enumerate(ims):
        x = gap + (k % cols) * (w + gap); y = head + gap + (k // cols) * (h + th + gap)
        d.text((x, y), t, font=f, fill=(236, 232, 222)); out.paste(im, (x, y + th))
    if footer:
        y = out.height - foot + 6
        for line_ in footer: d.text((gap, y), line_, font=f, fill=(170, 170, 178)); y += label + 10
    out.save(path); print(path.name, out.size)
    return out


def main():
    looks = [Diablo(), Backpack(), Hold()]
    halves = [("지금 게임 (참고)", Image.open(SHOTS / "ko_17_map_inventory_selected.png").convert("RGB").resize((960, 540), Image.LANCZOS))]
    for st in looks:
        full = st.screen().resize((1920, 1080), Image.LANCZOS)
        full.save(HERE / f"mock-{st.key}-map.png"); print(f"mock-{st.key}-map.png")
        halves.append((st.title, full.resize((960, 540), Image.LANCZOS)))
        sheet(st.states(), 6, HERE / f"mock-{st.key}-states.png", title=f"{st.title}: 한 보드의 상태", scale=0.62, label=20)
    sheet(halves, 2, HERE / "mock-compare.png", footer=[
        "같은 장면: 엘라의 가방 위에 인벤토리의 장궁(3×2)을 들고 있고, 오른쪽은 인벤토리 창과 장궁의 카드다.",
        "다시 그린 것은 보드 패널, 인벤토리 창, 카드, 패널의 설명 줄뿐이다(위의 용병, 헤더, 오른쪽 아래 버튼은 지금 그대로). 도형과 지금 아이콘, 호출 없음."])


if __name__ == "__main__":
    main()
