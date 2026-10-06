"""Tier marks on an item cell (round 41): the grade badge goes, and the tier above Bronze gets a richer mark than the flat rim of
round 35. Three ways, drawn over the game's screenshots (game/, run 20261006-b16a) and at twice the size for the detail. The
staged tiers are for the pictures. No API call.
  .venv/bin/python ArtPipeline/Archive/41-tier-marks/mock_tier_marks.py
"""
import math
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[3]; HERE = Path(__file__).resolve().parent
FONT = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"
ICONS = ROOT / "Assets/@Art/Item"
GAME = HERE / "game"
AA = 4                                                   # supersampling of the drawn marks

# The game's colours (UiPalette): the rims and the words of the tiers; the ink, the bone of a cell, the brass of the old badge.
RIM = {"Silver": (0x8C, 0x9C, 0xB2), "Gold": (0xE2, 0xA2, 0x1E), "Diamond": (0x2E, 0xC4, 0xE8)}
TEXT = {"Bronze": (0xD8, 0x92, 0x58), "Silver": (0xD5, 0xDE, 0xEA), "Gold": (0xF7, 0xC8, 0x4A), "Diamond": (0x6F, 0xE3, 0xF8)}
KO = {"Bronze": "동", "Silver": "은", "Gold": "금", "Diamond": "다이아"}
STARS = {"Silver": 1, "Gold": 2, "Diamond": 3}
INK = (0x18, 0x09, 0x07); BONE = (211, 198, 168); BRASS = (0xB8, 0x94, 0x4E); PALE = (0xEB, 0xEB, 0xE6)
PLUM = (0x2E, 0x1A, 0x3C); VIOLET = (0x9E, 0x86, 0xD6); FATIGUE = (0xCF, 0xBA, 0xF7)
# The metals of variant 1: body, light (the top-left bevel), dark (the bottom-right bevel).
METAL = {
    "Silver": ((0x9A, 0xA8, 0xBA), (0xDD, 0xE5, 0xEE), (0x5A, 0x66, 0x78)),
    "Gold": ((0xD6, 0xA2, 0x2E), (0xFF, 0xE4, 0x8E), (0x86, 0x5C, 0x10)),
    "Diamond": ((0x5C, 0xD2, 0xEE), (0xD8, 0xF8, 0xFF), (0x1C, 0x8C, 0xAC)),
}

CELL_W, CELL_H = 180, 60                                 # the outer size of a cell on screen
ICON_MX, ICON_MY = 8, 5                                  # the icon's margins inside the cell (UiPrefabSetup.Kit)


def font(px): return ImageFont.truetype(str(FONT), int(round(px)))


def outer(col, i):
    """The outer box of a board cell on the party-side and battle screenshots: column 0..3 (rows 4..1 from the left), cell i from the top."""
    x0 = 120 + 190 * col; y0 = 646 + 62 * i
    return (x0, y0)


# ---- drawing one cell's marks on a supersampled layer ----------------------------------------------------------------------

class Layer:
    """A transparent layer the size of one cell, drawn at k*AA pixels per screen pixel and laid on the picture at the end."""

    def __init__(self, k):
        self.k = k; self.u = k * AA
        self.img = Image.new("RGBA", (CELL_W * self.u, CELL_H * self.u), (0, 0, 0, 0))
        self.d = ImageDraw.Draw(self.img)

    def box(self, x0, y0, x1, y1):
        u = self.u; return (x0 * u, y0 * u, x1 * u - 1, y1 * u - 1)

    def rrect(self, b, r, fill=None, outline=None, width=1):
        self.d.rounded_rectangle(self.box(*b), radius=r * self.u, fill=fill, outline=outline, width=max(1, int(round(width * self.u))))

    def ring(self, b, band, r, fill):
        """A rounded band `band` wide inside the box b, laid over what is on the layer already (a veil stays under it)."""
        temp = Image.new("RGBA", self.img.size, (0, 0, 0, 0)); d = ImageDraw.Draw(temp)
        x0, y0, x1, y1 = b
        d.rounded_rectangle(self.box(x0, y0, x1, y1), radius=r * self.u, fill=fill)
        d.rounded_rectangle(self.box(x0 + band, y0 + band, x1 - band, y1 - band), radius=max(0.5, r - band) * self.u, fill=(0, 0, 0, 0))
        self.img.alpha_composite(temp)

    def text(self, xy, s, px, color, anchor="mm"):
        self.d.text((xy[0] * self.u, xy[1] * self.u), s, font=font(px * self.u), fill=color, anchor=anchor)

    def star(self, cx, cy, r, color, outline=None):
        pts = []
        for n in range(10):
            a = -math.pi / 2 + n * math.pi / 5; rr = r if n % 2 == 0 else r * 0.45
            pts.append(((cx + math.cos(a) * rr) * self.u, (cy + math.sin(a) * rr) * self.u))
        self.d.polygon(pts, fill=color, outline=outline, width=max(1, int(0.8 * self.u)) if outline else 0)

    def diamond(self, cx, cy, r, fill, outline):
        u = self.u
        self.d.polygon([(cx * u, (cy - r) * u), ((cx + r) * u, cy * u), (cx * u, (cy + r) * u), ((cx - r) * u, cy * u)],
                       fill=fill, outline=outline, width=max(1, int(0.9 * u)))

    def lay(self, target, at):
        small = self.img.resize((CELL_W * self.k, CELL_H * self.k), Image.LANCZOS)
        target.alpha_composite(small, (at[0] * self.k, at[1] * self.k))


def gradient_ring(L, b, band, r, light, body, dark):
    """A bevelled metal band: lit from the top-left, shaded at the bottom-right, with a bright seam along the lit edges."""
    u = L.u; x0, y0, x1, y1 = b
    W, H = L.img.size
    grad = Image.new("RGBA", (W, H))
    g = ImageDraw.Draw(grad)
    # Vertical gradient light -> body -> dark over the band's box.
    top, bottom = y0 * u, y1 * u
    for y in range(top, bottom):
        t = (y - top) / max(1, bottom - top - 1)
        c = tuple(int(round(light[i] * (1 - t) + dark[i] * t)) for i in range(3))
        c = tuple(int(round(c[i] * 0.55 + body[i] * 0.45)) for i in range(3))
        g.line([(0, y), (W, y)], fill=c + (255,))
    mask = Image.new("L", (W, H), 0)
    md = ImageDraw.Draw(mask)
    md.rounded_rectangle(L.box(*b), radius=r * u, fill=255)
    md.rounded_rectangle(L.box(x0 + band, y0 + band, x1 - band, y1 - band), radius=max(0.5, r - band) * u, fill=0)
    L.img.paste(grad, (0, 0), mask)
    # The seam: a bright line along the outer top and left edges, a dark one along the inner bottom and right edges.
    L.d.line([((x0 + r) * u, (y0 + 1) * u), ((x1 - r) * u, (y0 + 1) * u)], fill=light + (230,), width=max(1, int(1.1 * u)))
    L.d.line([((x0 + 1) * u, (y0 + r) * u), ((x0 + 1) * u, (y1 - r) * u)], fill=light + (200,), width=max(1, int(1.1 * u)))
    L.d.line([((x0 + band + r) * u, (y1 - band - 1) * u), ((x1 - band - r) * u, (y1 - band - 1) * u)], fill=light + (120,), width=max(1, int(0.8 * u)))


def metal_frame(L, tier):
    """Variant 1: the cell's edge becomes a frame of the tier's metal (The Bazaar's frame as the tier), with a stud at each corner.
    Diamond is crystal: facets across the band and gems at the corners."""
    body, light, dark = METAL[tier]
    b = (2, 2, CELL_W - 2, CELL_H - 2)                   # just inside the cell's own dark line
    band = 6; r = 4
    gradient_ring(L, b, band, r, light, body, dark)
    L.rrect(b, r, outline=INK + (255,), width=1)                                        # the outer hairline
    L.rrect((b[0] + band, b[1] + band, b[2] - band, b[3] - band), 1, outline=INK + (170,), width=1)   # the inner hairline
    if tier == "Diamond":
        # Facets: pale streaks leaning right along the top and bottom bands, and down the sides.
        u = L.u
        for x in range(b[0] + 14, b[2] - 12, 16):
            for yy in (b[1], b[3] - band):
                L.d.line([((x) * u, (yy + band - 1) * u), ((x + 4) * u, (yy + 1) * u)], fill=(255, 255, 255, 150), width=max(1, int(1.2 * u)))
        for y in range(b[1] + 14, b[3] - 12, 14):
            for xx in (b[0], b[2] - band):
                L.d.line([((xx + 1) * u, (y + 4) * u), ((xx + band - 1) * u, (y) * u)], fill=(255, 255, 255, 150), width=max(1, int(1.2 * u)))
        for cx, cy in ((b[0] + 3, b[1] + 3), (b[2] - 3, b[1] + 3), (b[0] + 3, b[3] - 3), (b[2] - 3, b[3] - 3)):
            L.diamond(cx, cy, 6.5, light + (255,), INK + (255,))
            L.diamond(cx - 1.2, cy - 1.2, 2.2, (255, 255, 255, 255), None)
    else:
        s = 9
        for cx, cy in ((b[0] + 3, b[1] + 3), (b[2] - 3, b[1] + 3), (b[0] + 3, b[3] - 3), (b[2] - 3, b[3] - 3)):
            L.rrect((cx - s / 2, cy - s / 2, cx + s / 2, cy + s / 2), 1.5, fill=body + (255,), outline=INK + (255,), width=1)
            L.rrect((cx - s / 2 + 1.5, cy - s / 2 + 1.5, cx - s / 2 + 4.5, cy - s / 2 + 4.5), 0.5, fill=light + (255,))


def thin_rim(L, tier):
    """Round 35 (A), as the game draws it now: a flat band 4 inside the cell's line, a dark hairline inside it."""
    b = (3, 3, CELL_W - 3, CELL_H - 3)
    L.ring(b, 4, 4, RIM[tier] + (255,))
    L.rrect((b[0] + 4, b[1] + 4, b[2] - 4, b[3] - 4), 1, outline=(0, 0, 0, 128), width=1)


def star_tag(L, tier, stars=None):
    """Variant 2: at the bottom-left corner, where the grade badge was, an ink tag with the tier's stars (Silver 1, Gold 2,
    Diamond 3) in the tier's colour, rimmed in it: the twin of the fatigue tag at the top-right."""
    n = STARS[tier] if stars is None else stars
    h = 20; pitch = 13; w = 10 + pitch * n + 2
    x0 = 5; y1 = CELL_H - 5; y0 = y1 - h; x1 = x0 + w
    L.rrect((x0, y0, x1, y1), h / 2, fill=INK + (235,), outline=RIM[tier] + (255,), width=1.5)
    for s in range(n):
        cx = x0 + 6 + pitch * s + 6.5
        L.star(cx, y0 + h / 2 + 0.5, 6.2, TEXT[tier] + (255,), outline=INK + (255,))


def old_badge(L, grade):
    """The grade badge the user is removing: a brass disc with an ink rim at the bottom-left corner."""
    s = 24; x0 = 5; y1 = CELL_H - 5
    L.d.ellipse(L.box(x0, y1 - s, x0 + s, y1), fill=INK + (255,))
    L.d.ellipse(L.box(x0 + 3, y1 - s + 3, x0 + s - 3, y1 - 3), fill=BRASS + (255,))
    L.text((x0 + s / 2, y1 - s / 2), str(grade), 14, INK + (255,))


def fatigue_tag(L):
    """The "+1" of equipment that costs fatigue (round 32, B1), at the top-right corner."""
    w, h = 30, 20; x1 = CELL_W - 5; y0 = 5
    L.rrect((x1 - w, y0, x1, y0 + h), h / 2, fill=PLUM + (255,), outline=VIOLET + (255,), width=1.5)
    L.text((x1 - w / 2, y0 + h / 2 + 0.5), "+1", 13, FATIGUE + (255,))


def merge_mark(L, into, how):
    """A cell the chosen item would merge into: a veil, the words of the tier the merge makes, and that tier's mark."""
    L.rrect((3, 3, CELL_W - 3, CELL_H - 3), 3, fill=(20, 22, 30, 150))
    if how in ("1", "3"): metal_frame(L, into)
    if how in ("now", "2"): thin_rim(L, into)
    if how in ("2", "3"): star_tag(L, into)
    L.text((CELL_W / 2 + (14 if how in ("2", "3") else 0), CELL_H / 2), f"합치기 → {KO[into]}", 20, TEXT[into] + (255,))


def marks(L, how, tier, grade=None, fatigue=False):
    """The marks of one cell in one variant. `how`: now | 1 | 2 | 3."""
    if tier != "Bronze":
        if how == "now": thin_rim(L, tier)
        if how in ("1", "3"): metal_frame(L, tier)
        if how == "2": thin_rim(L, tier)
        if how in ("2", "3"): star_tag(L, tier)
    if how == "now" and grade is not None: old_badge(L, grade)
    if fatigue: fatigue_tag(L)


# ---- the party side: cells repainted and staged ------------------------------------------------------------------------

def icon_image(item, k):
    """The item's icon as the cell shows it: inside the margins, keeping its ratio, centred."""
    im = Image.open(ICONS / f"{item}.png").convert("RGBA")
    aw, ah = (CELL_W - 2 * ICON_MX) * k, (CELL_H - 2 * ICON_MY) * k
    s = min(aw / im.width, ah / im.height)
    return im.resize((max(1, int(round(im.width * s))), max(1, int(round(im.height * s)))), Image.LANCZOS)


def repaint(img, at, k=1):
    """Fills the inside of a cell with bone (the badge, the icon and the tags go), keeping the cell's dark line."""
    x0, y0 = at
    ImageDraw.Draw(img).rounded_rectangle((x0 * k + 2 * k, y0 * k + 2 * k, (x0 + CELL_W) * k - 2 * k - 1, (y0 + CELL_H) * k - 2 * k - 1),
                                          radius=3 * k, fill=BONE + (255,))


def paste_icon(img, at, item, k=1):
    ic = icon_image(item, k)
    x0, y0 = at
    ax, ay = (x0 + ICON_MX) * k, (y0 + ICON_MY) * k
    aw, ah = (CELL_W - 2 * ICON_MX) * k, (CELL_H - 2 * ICON_MY) * k
    img.alpha_composite(ic, (ax + (aw - ic.width) // 2, ay + (ah - ic.height) // 2))


# Staged boards: (column, cell) -> (item, tier, grade, costs fatigue). Rowan (row 1) carries the found equipment.
PARTY = {
    (3, 0): ("longsword", "Bronze", 10, False), (3, 1): ("dagger", "Silver", 8, True), (3, 2): ("buckler", "Gold", 8, True), (3, 3): ("herb_pouch", "Diamond", 8, False),
    (2, 0): ("longsword", "Gold", 12, False), (1, 0): ("healing_staff", "Bronze", 10, False), (0, 0): ("fire_staff", "Silver", 10, False),
}
BATTLE = {(3, 0): "Silver", (2, 0): "Gold", (0, 0): "Diamond"}
CROP = (100, 616, 900, 966)


def party_side(how):
    img = Image.open(GAME / "ko_30_map_fatigue.png").convert("RGBA")
    for (col, i), (item, tier, grade, fat) in PARTY.items():
        at = outer(col, i)
        repaint(img, at); paste_icon(img, at, item)
        L = Layer(1); marks(L, how, tier, grade, fat); L.lay(img, at)
    return img.crop(CROP)


def battle(how):
    img = Image.open(GAME / "ko_05_battle.png").convert("RGBA")
    for (col, i), tier in BATTLE.items():
        L = Layer(1); marks(L, how, tier); L.lay(img, outer(col, i))
    return img.crop(CROP)


# ---- the detail at twice the size ----------------------------------------------------------------------------------------

def detail_cell(how, tier, item="dagger", merge_into=None, k=2, fatigue=True):
    """One cell drawn from scratch at k times the screen: the bone cell with its line, the icon, the marks."""
    img = Image.new("RGBA", (CELL_W * k, CELL_H * k), (0x2C, 0x26, 0x27, 255))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((0, 0, CELL_W * k - 1, CELL_H * k - 1), radius=5 * k, fill=(72, 60, 50, 255))
    repaint(img, (0, 0), k)
    paste_icon(img, (0, 0), item, k)
    L = Layer(k)
    if merge_into: merge_mark(L, merge_into, how)
    else: marks(L, how, tier, 8, fatigue)
    L.lay(img, (0, 0))
    return img


def sheet(entries, cols, path, lab=40, gap=20, size=24, footer=()):
    w = max(im.width for _, im in entries); h = max(im.height for _, im in entries); rows = (len(entries) + cols - 1) // cols
    fh = 32 * len(footer) + (16 if footer else 0)
    out = Image.new("RGB", (cols * (w + gap) + gap, rows * (h + lab + gap) + gap + fh), (18, 18, 18)); d = ImageDraw.Draw(out)
    for n, (name, im) in enumerate(entries):
        x = gap + (n % cols) * (w + gap); y = gap + (n // cols) * (h + lab + gap)
        d.text((x, y + lab - 8), name, font=font(size), fill=(235, 235, 235), anchor="ls")
        out.paste(im.convert("RGB"), (x + (w - im.width) // 2, y + lab))
    for j, s in enumerate(footer): d.text((gap, out.height - fh + j * 32), s, font=font(21), fill=(190, 190, 190))
    out.save(path); print(path.name, out.size)


NAMES = {"now": "지금: 등급 배지 + 가는 테(Round 35 A)", "1": "안 1: 금속 테 — 단계의 금속으로 된 틀, 모서리 징(다이아는 수정과 보석)",
         "2": "안 2: 별 표 — 가는 테는 그대로, 왼쪽 아래 표에 별 1·2·3", "3": "안 3: 금속 테 + 별 표"}


def main():
    entries = []
    for how in ("now", "1", "2", "3"):
        entries.append((NAMES[how] + " — 파티 쪽", party_side(how)))
        entries.append(("— 전투", battle(how)))
    sheet(entries, 2, HERE / "mock-compare.png",
          footer=["스테이징: 로언(1열) 롱소드 동, 단검 은(+1), 버클러 금(+1), 약초 주머니 다이아 / 카이 롱소드 금 / 미라 지팡이 은. 전투는 로언 은, 카이 금, 미라 다이아. 동은 어느 안에서나 지금 그대로.",
                  "안 1~3은 등급 배지가 없다(등급은 아이템 제목 '단검 · 은 · 등급 8'에만). 전투 칸에는 원래 배지가 없다."])
    det = []
    for how in ("now", "1", "2", "3"):
        for tier in ("Silver", "Gold", "Diamond"):
            det.append((f"{NAMES[how].split(':')[0]} · {KO[tier]}", detail_cell(how, tier)))
        det.append((f"{NAMES[how].split(':')[0]} · 합치기 → 은", detail_cell(how, "Bronze", merge_into="Silver", fatigue=False)))
    sheet(det, 4, HERE / "mock-detail.png", lab=36, gap=18, size=20,
          footer=["2배 확대. 왼쪽 위의 '+1'은 장비 피로의 표(Round 32), 넷째 열은 고른 아이템이 합쳐질 칸의 표시(Round 35)."])


if __name__ == "__main__":
    main()
