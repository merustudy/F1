"""Item cooldown as light (round 18): the cell starts dark and lights up from the left as the item charges; when it is
fully lit the item fires. The cover reaches the whole cell (inside its 2px rim) and lies over the icon. The colour of the
light is the choice. Drawn over the current battle screenshot with the game's own sprites and icons, no API call.
  .venv/bin/python ArtPipeline/Archive/18-cooldown-light/mock_cooldown_light.py

How the sheets are made, so that they show what the game would draw:
- The game blends its UI in linear light (ProjectSettings m_ActiveColorSpace 1), so every layer here is blended there too.
  "now" is redrawn the same way at the screenshot's own charges and checked against the screenshot (printed as the mean
  difference per channel), so the variants differ from it only by the cooldown layers.
- Only the item cells are redrawn: the slot sprite as a nine-slice at 2x brought down to the cell, the icon in its place
  (margins 8/5, in proportion), the cooldown layers, and over them the screen vignette (vignette.png, black at 35%).
"""
from dataclasses import dataclass
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageMath

HERE = Path(__file__).resolve().parent; ROOT = HERE.parents[2]
SHOT = ROOT / "ArtPipeline/Archive/17-outline-pupils/game/ko_05_battle.png"   # the battle at 7.3 s, after round 17
FRAME = ROOT / "Assets/@Art/UI/Frame"; ICON = ROOT / "Assets/@Art/UI/Icon"; ITEM = ROOT / "Assets/@Art/Item"
FONT = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"

GAMMA = 2.2
W, H = 1920, 1080
CELL_W, CELL_H, GAP = 180, 60, 2               # BattleItemView.CellWidth, CellHeight, CellGapY
TOP = 620 + 14                                 # UiPrefabSetup.Battle: BoardPanelTop + BoardColumnsTop
PARTY_X = [120, 310, 500, 690]                 # rows 4, 3, 2, 1
ENEMY_X = [1050, 1240]                         # rows 1, 2
SLOT_BORDER = 16                               # UiArt Piece border of the slot (sprite pixels, 2x)
ICON_MX, ICON_MY = 8, 5                        # UiPrefabSetup.Kit.ItemIconMarginX/Y
NOW_INSET, NOW_GAUGE, NOW_ALPHA = 7, (0x8C, 0x3A, 0x2C), 0.84   # KitBar inset, UiPalette.Gauge, CooldownAlpha
FLASH_INSET, FLASH_PEAK, FLASH_LIGHT = 6, 0.6, (255, 235, 168)  # BattleItemView: flash inside 6, PulseFlash, FlashLight
PULSE_POP = 0.3                                 # BattleItemView.PulsePop
ICON_DIM = (0x6E, 0x6E, 0x74)                  # UiPalette.IconDim
VIGNETTE_ALPHA = 0.35                          # UiPrefabSetup.Kit.ScreenVignetteAlpha
RIM = 2                                        # the slot's dark rim: the new cover fills everything inside it
FEATHER, GLOW_W, GLOW_A, LINE_A = 6, 20, 0.45, 0.9   # the soft edge of the dark, the glow behind the front, the front line
SHADE = 0.85                                   # how dark a cell is before it charges (black over it, blended in linear light)
EMPTY_ALPHA = 0.8                              # UiPrefabSetup.Kit.EmptyCellAlpha

# The items of the screenshot and the charge each shows in the variants (the same for all, so only the look differs).
PARTY = [("fire_staff", 0.12), ("healing_staff", 0.62), ("sword", 0.80), ("longsword", 0.38)]
ENEMY = [("rat_bite", 0.55), ("rat_bite", 0.92)]


@dataclass
class Look:
    key: str
    label: str
    shade: tuple = None      # the dark over the part not yet charged (None: the cooldown of now, a red fill behind the icon)
    shade_a: float = 0.0
    tint: tuple = None       # the light over the charged part (None: no colour, the cell as it is)
    tint_a: float = 0.0
    edge: tuple = None       # the line and glow at the front of the charge
    flash: tuple = FLASH_LIGHT


NOW = Look("now", "지금: 붉은 채움(마른 피 84%)이 칸 안쪽 7px에서 왼쪽→오른쪽, 아이콘 뒤")
LOOKS = [
    Look("A", "A  뼈빛 — 어둠이 걷히며 칸 본래의 색으로", shade=(0x18, 0x09, 0x07), shade_a=SHADE, edge=(255, 240, 212), flash=(255, 244, 222)),
    Look("B", "B  촛불 금빛 (권장) — 따뜻한 금빛이 번지며 밝아짐", shade=(0x1E, 0x10, 0x06), shade_a=SHADE, tint=(255, 184, 72), tint_a=0.22,
         edge=(255, 206, 104), flash=FLASH_LIGHT),
    Look("C", "C  영혼 푸른빛 — 푸른 마나처럼 차오름", shade=(0x06, 0x0C, 0x1E), shade_a=SHADE, tint=(110, 168, 255), tint_a=0.22,
         edge=(160, 204, 255), flash=(196, 222, 255)),
    Look("D", "D  비전 보라 — 보랏빛 마력이 차오름", shade=(0x14, 0x08, 0x1E), shade_a=SHADE, tint=(178, 118, 255), tint_a=0.22,
         edge=(212, 172, 255), flash=(226, 204, 255)),
]


# --- linear light -------------------------------------------------------------------------------------------------
def f(fn, **images):
    return ImageMath.lambda_eval(lambda a: fn(a), **images)

def to_linear(im):
    return [f(lambda a: (a["c"] / 255.0) ** GAMMA, c=c.convert("F")) for c in im.convert("RGB").split()]

def to_srgb(chs):
    return Image.merge("RGB", [f(lambda a: a["min"](a["max"](a["c"], 0.0), 1.0) ** (1.0 / GAMMA) * 255.0 + 0.5, c=c).convert("L") for c in chs])

def lin_value(v):
    return (v / 255.0) ** GAMMA

def alpha_f(mask_l, k=1.0):
    """An L mask (0..255) as an alpha in 0..1, times k."""
    return mask_l.convert("F").point(lambda v: v * (k / 255.0))

def over(dst, src, a):
    """Blends src over dst in linear light with the alpha a (all F images of one size)."""
    return [f(lambda e: e["s"] * e["a"] + e["d"] * (1.0 - e["a"]), s=s, d=d, a=a) for s, d in zip(src, dst)]

def solid(color, size):
    return [Image.new("F", size, lin_value(v)) for v in color]

def over_rgba(dst, im):
    """An sRGB RGBA image over dst: its colours go to linear light, its alpha stays as it is (as the GPU does)."""
    return over(dst, to_linear(im), alpha_f(im.split()[3]))

def over_color(dst, color, mask):
    return over(dst, solid(color, mask.size), mask)


# --- the kit --------------------------------------------------------------------------------------------------------
def nine(piece, size, border):
    """Stretches a sprite to a size as a nine-slice (the corners keep their shape), like Image.Type.Sliced."""
    w, h = size; pw, ph = piece.size; b = border; out = Image.new("RGBA", size, (0, 0, 0, 0))
    cols = [(0, b, 0, b), (b, pw - b, b, w - b), (pw - b, pw, w - b, w)]; rows = [(0, b, 0, b), (b, ph - b, b, h - b), (ph - b, ph, h - b, h)]
    for sx0, sx1, dx0, dx1 in cols:
        for sy0, sy1, dy0, dy1 in rows:
            if dx1 <= dx0 or dy1 <= dy0: continue
            out.paste(piece.crop((sx0, sy0, sx1, sy1)).resize((dx1 - dx0, dy1 - dy0), Image.LANCZOS), (dx0, dy0))
    return out

def frame(sprite, border, w, h):
    """A kit frame at screen size: the sprite is 2x, so slice at 2x and bring it down."""
    return nine(sprite, (2 * w, 2 * h), border).resize((w, h), Image.LANCZOS)

SLOT = Image.open(FRAME / "slot.png").convert("RGBA")
VIGNETTE = Image.open(ICON / "vignette.png").convert("RGBA").resize((W, H), Image.LANCZOS).split()[3]

def font(size): return ImageFont.truetype(str(FONT), size)

def board_h(cells): return cells * CELL_H + (cells - 1) * GAP

def icon_layer(item, w, h, mirrored, pop=1.0, dim=False):
    """The item's icon in the cell's icon place (margins 8/5, in proportion), as an RGBA layer of the cell's size."""
    icon = Image.open(ITEM / f"{item}.png").convert("RGBA")
    bw, bh = w - 2 * ICON_MX, h - 2 * ICON_MY; k = min(bw / icon.width, bh / icon.height) * pop
    icon = icon.resize((max(1, round(icon.width * k)), max(1, round(icon.height * k))), Image.LANCZOS)
    if mirrored: icon = icon.transpose(Image.FLIP_LEFT_RIGHT)
    if dim:
        r, g, b, a = icon.split()
        icon = Image.merge("RGBA", [c.point(lambda v, m=m: v * m // 255) for c, m in zip((r, g, b), ICON_DIM)] + [a])
    layer = Image.new("RGBA", (w, h), (0, 0, 0, 0)); layer.alpha_composite(icon, ((w - icon.width) // 2, (h - icon.height) // 2))
    return layer


# --- one cell -------------------------------------------------------------------------------------------------------
def masks_for(look, w, h, charge):
    """The alpha masks of the light cooldown at this charge: the tint over the charged part, the dark over the rest
    (soft at its left edge), the glow behind the front and the front line. All inside the rim."""
    x0, x1, y0, y1 = RIM, w - RIM, RIM, h - RIM
    front = x0 + charge * (x1 - x0)
    tint = Image.new("L", (w, h), 0); dark = Image.new("L", (w, h), 0); glow = Image.new("L", (w, h), 0); line = Image.new("L", (w, h), 0)
    dt, dd, dg, dl = (ImageDraw.Draw(m) for m in (tint, dark, glow, line))
    for x in range(x0, x1):
        c = x + 0.5
        if c < front: dt.line([(x, y0), (x, y1 - 1)], fill=255)
        s = 1.0 if charge <= 0.0 else min(max((c - front) / FEATHER, 0.0), 1.0); s = s * s * (3 - 2 * s)
        if s > 0: dd.line([(x, y0), (x, y1 - 1)], fill=round(255 * s))
        if front - GLOW_W <= c < front: dg.line([(x, y0), (x, y1 - 1)], fill=round(255 * ((c - (front - GLOW_W)) / GLOW_W) ** 2))
        if abs(c - front) <= 1.0: dl.line([(x, y0), (x, y1 - 1)], fill=255)
    return tint, dark, glow, line

def cell(look, item, cells, charge, mirrored=False, active=True, flash_t=None, base=None):
    """One item's cell block at screen size, in linear light. base is what lies under it (the bag): a linear crop,
    or None for a plain dark bag colour. flash_t is the time into the firing pulse (0..1) or None."""
    w, h = CELL_W, board_h(cells)
    dst = base if base is not None else solid((52, 52, 52), (w, h))
    dst = over_rgba(dst, frame(SLOT, SLOT_BORDER, w, h))
    pop = 1.0 + PULSE_POP * __import__("math").sin(flash_t * __import__("math").pi) if flash_t is not None else 1.0
    lit = charge if active else 0.0
    if look.shade is None:
        # now: the charge, a red fill from the left inside the 7px margin, behind the icon
        fill = Image.new("L", (w, h), 0)
        if lit > 0: ImageDraw.Draw(fill).rectangle([NOW_INSET, NOW_INSET, NOW_INSET + round(lit * (w - 2 * NOW_INSET)) - 1, h - NOW_INSET - 1], fill=255)
        dst = over_color(dst, NOW_GAUGE, alpha_f(fill, NOW_ALPHA))
        dst = over_rgba(dst, icon_layer(item, w, h, mirrored, pop, dim=not active))
        inset = FLASH_INSET
    else:
        # The light's colour goes on the cell under the icon, so that a lit icon shows its own colours; the dark goes
        # over the icon, so that an item not yet charged is dark as a whole.
        tint, dark, glow, line = masks_for(look, w, h, lit)
        if look.tint is not None: dst = over_color(dst, look.tint, alpha_f(tint, look.tint_a))
        dst = over_rgba(dst, icon_layer(item, w, h, mirrored, pop, dim=not active))
        # While the firing flash plays the cell stays lit and the dark comes back with it: a flash over the dark
        # would turn into a muddy tone, and the cell reads as "fired, now cooling" instead.
        returning = flash_t if flash_t is not None else 1.0
        dst = over_color(dst, look.shade, alpha_f(dark, look.shade_a * returning))
        if active and 0.0 < lit < 1.0:
            dst = over_color(dst, look.edge, alpha_f(glow, GLOW_A))
            dst = over_color(dst, look.edge, alpha_f(line, LINE_A))
        inset = RIM
    if flash_t is not None:
        fm = Image.new("L", (w, h), 0); ImageDraw.Draw(fm).rectangle([inset, inset, w - inset - 1, h - inset - 1], fill=255)
        dst = over_color(dst, look.flash, alpha_f(fm, FLASH_PEAK * (1.0 - flash_t)))
    return dst

def put(img_lin, chs, x, y):
    """Pastes linear channels into the linear screen, with the screen vignette over them as in the game."""
    w, h = chs[0].size
    v = alpha_f(VIGNETTE.crop((x, y, x + w, y + h)), VIGNETTE_ALPHA)
    chs = over_color(chs, (0, 0, 0), v)
    for i in range(3): img_lin[i].paste(chs[i], (x, y))


# --- sheets ---------------------------------------------------------------------------------------------------------
def darken_empty(img, src_lin, look):
    """The empty cells unlit too: the same dark as a cell that has not charged, over the empty cell of the screenshot,
    inside its rim. (Only the party has empty cells: an enemy has as many cells as its items take.)"""
    for x in PARTY_X:
        for k in range(1, 5):
            y = TOP + k * (CELL_H + GAP)
            dst = [c.crop((x, y, x + CELL_W, y + CELL_H)) for c in src_lin]
            m = Image.new("L", (CELL_W, CELL_H), 0); ImageDraw.Draw(m).rectangle([RIM, RIM, CELL_W - RIM - 1, CELL_H - RIM - 1], fill=255)
            dst = over_color(dst, look.shade, alpha_f(m, look.shade_a))
            for i in range(3): img[i].paste(dst[i], (x, y))

def render(src_lin, look, party=PARTY, enemy=ENEMY, dark_empty=False):
    img = [c.copy() for c in src_lin]
    if dark_empty: darken_empty(img, src_lin, look)
    for x, (item, charge) in zip(PARTY_X, party):
        base = [c.crop((x, TOP, x + CELL_W, TOP + CELL_H)) for c in src_lin]
        put(img, cell(look, item, 1, charge, base=base), x, TOP)
    for x, (item, charge) in zip(ENEMY_X, enemy):
        base = [c.crop((x, TOP, x + CELL_W, TOP + CELL_H)) for c in src_lin]
        put(img, cell(look, item, 1, charge, mirrored=True, base=base), x, TOP)
    return img

def screenshot_charges(src):
    """The charge each item of the screenshot shows: the end of the red in the rows between the fill's top and the icon."""
    out = []
    for x in PARTY_X + ENEMY_X:
        y = TOP + NOW_INSET + 1
        reds = [px for px in range(x + NOW_INSET, x + CELL_W - NOW_INSET) if (lambda p: p[0] - p[2] > 45 and p[1] < 130)(src.getpixel((px, y)))]
        out.append(0.0 if not reds else (max(reds) + 1 - (x + NOW_INSET)) / (CELL_W - 2 * NOW_INSET))
    return out

def check_now(src, src_lin):
    """Redraws the cells of the screenshot as they are now and reports how far the redraw is from the game's pixels."""
    ch = screenshot_charges(src)
    party = [(item, c) for (item, _), c in zip(PARTY, ch[:4])]; enemy = [(item, c) for (item, _), c in zip(ENEMY, ch[4:])]
    redraw = to_srgb(render(src_lin, NOW, party, enemy))
    for i, x in enumerate(PARTY_X[1:] + ENEMY_X):                    # Mira's cell is mid-flash in the screenshot: skip it
        box = (x + RIM, TOP + RIM, x + CELL_W - RIM, TOP + CELL_H - RIM)
        a, b = src.crop(box), redraw.crop(box)
        diff = [sum(abs(p - q) for p, q in zip(ca.getdata(), cb.getdata())) / (ca.width * ca.height) for ca, cb in zip(a.split(), b.split())]
        print(f"check now x={x}: charge {ch[i + 1]:.2f}, mean |diff| per channel {[round(d, 1) for d in diff]}")

def label_sheet(entries, path, lab=40, gap=20, size=26, footer=None, bg=(18, 18, 18)):
    w = max(im.width for _, im in entries); fh = 46 * len(footer) if footer else 0
    out = Image.new("RGB", (w + 2 * gap, sum(im.height + lab + gap for _, im in entries) + gap + fh), bg); d = ImageDraw.Draw(out); y = gap
    for name, im in entries:
        d.text((gap, y), name, font=font(size), fill=(235, 235, 235)); out.paste(im, (gap, y + lab)); y += im.height + lab + gap
    for i, line in enumerate(footer or []):
        d.text((gap, y + 6 + 46 * i), line, font=font(22), fill=(190, 190, 190))
    out.save(path); print(path.name, out.size)

def time_sheet(path):
    """One cell through a cycle, every look in a row: charges, the firing flash, right after, and an item that cannot be
    used where its owner stands. 1.5x, on the bag's dark."""
    steps = [("0% (전투 시작)", 0.0, None, True), ("30%", 0.3, None, True), ("60%", 0.6, None, True), ("90%", 0.9, None, True),
             ("100% → 발동, 번쩍", 0.02, 0.15, True), ("0.3초 뒤: 다시 충전", 0.1, None, True), ("쓸 수 없는 자리", 0.0, None, False)]
    k = 1.5; cw, chh = round(CELL_W * k), round(CELL_H * k); gap = 14; lab_w = 300; head = 44; row_gap = 26
    rows = [NOW] + LOOKS
    out = Image.new("RGB", (lab_w + len(steps) * (cw + gap) + gap, head + len(rows) * (chh + row_gap) + 70), (18, 18, 18)); d = ImageDraw.Draw(out)
    for j, (name, *_rest) in enumerate(steps):
        d.text((lab_w + j * (cw + gap), 10), name, font=font(22), fill=(200, 200, 200))
    for i, look in enumerate(rows):
        y = head + i * (chh + row_gap)
        d.text((16, y + chh // 2 - 14), {"now": "지금"}.get(look.key, look.label.split(" — ")[0]), font=font(24), fill=(235, 235, 235))
        for j, (name, charge, flash_t, active) in enumerate(steps):
            im = to_srgb(cell(look, "longsword", 1, charge, active=active, flash_t=flash_t)).resize((cw, chh), Image.LANCZOS)
            out.paste(im, (lab_w + j * (cw + gap), y))
    d.text((16, out.height - 56), "롱소드 1칸, 1.5배. '발동'은 번쩍임(0.3초)이 시작되고 0.05초 뒤: 아이콘이 튀어오르고, A~D는 번쩍이는 동안 밝은 채로 있다가 어둠이 돌아온다. '쓸 수 없는 자리'는 주인이 선 열에서 못 쓰는 아이템(아이콘을 흐리게, 지금과 같음).",
           font=font(20), fill=(170, 170, 170))
    out.save(path); print(path.name, out.size)

def tall_sheet(path):
    """The 2- and 3-cell items (longbow, halberd) at 60%, every look, 1:1: the cover reaches the whole tall cell."""
    rows = [NOW] + LOOKS; gap = 16; lab = 36
    out = Image.new("RGB", (len(rows) * (2 * CELL_W + 3 * gap) + gap, lab + board_h(3) + 2 * gap), (18, 18, 18)); d = ImageDraw.Draw(out)
    for i, look in enumerate(rows):
        x = gap + i * (2 * CELL_W + 3 * gap)
        d.text((x, 8), {"now": "지금"}.get(look.key, look.label.split(" — ")[0]), font=font(22), fill=(235, 235, 235))
        out.paste(to_srgb(cell(look, "longbow", 2, 0.6)), (x, lab + gap))
        out.paste(to_srgb(cell(look, "halberd", 3, 0.6)), (x + CELL_W + gap, lab + gap))
    out.save(path); print(path.name, out.size)

# The cooldowns of the animation (seconds) for the six cells, and its length: short, so that every item fires in it.
ANIM_CD = [1.6, 2.2, 1.3, 2.6, 1.1, 1.8]
ANIM_SECONDS, ANIM_FPS, PULSE = 4.0, 12, 0.3     # BattleItemView.PulseDuration

def anim_gif(src_lin, path, looks, crop=(100, 618, 1440, 710), dark_empty=False):
    """The cells of the screenshot charging and firing, every look in a row. Charges start at the sheet's charges."""
    start = [c for _, c in PARTY] + [c for _, c in ENEMY]
    lab = 34; gap = 10; cw, chh = crop[2] - crop[0], crop[3] - crop[1]
    frames = []
    for n in range(round(ANIM_SECONDS * ANIM_FPS)):
        t = n / ANIM_FPS
        sheet_im = Image.new("RGB", (cw + 2 * gap, len(looks) * (chh + lab + gap) + gap), (18, 18, 18)); d = ImageDraw.Draw(sheet_im)
        for i, look in enumerate(looks):
            img = [c.copy() for c in src_lin]
            if dark_empty and look.shade is not None: darken_empty(img, src_lin, look)
            for j, (x, (item, _)) in enumerate(zip(PARTY_X + ENEMY_X, PARTY + ENEMY)):
                elapsed = start[j] * ANIM_CD[j] + t
                charge = (elapsed % ANIM_CD[j]) / ANIM_CD[j]
                since = elapsed % ANIM_CD[j] if elapsed >= ANIM_CD[j] else None    # time since the last firing, if it fired
                flash_t = since / PULSE if since is not None and since < PULSE else None
                base = [c.crop((x, TOP, x + CELL_W, TOP + CELL_H)) for c in src_lin]
                put(img, cell(look, item, 1, charge, mirrored=j >= len(PARTY), flash_t=flash_t, base=base), x, TOP)
            y = gap + i * (chh + lab + gap)
            d.text((gap, y + 2), look.label if look.key == "now" else look.label.split(" — ")[0], font=font(22), fill=(235, 235, 235))
            sheet_im.paste(to_srgb([c.crop(crop) for c in img]), (gap, y + lab))
        frames.append(sheet_im.convert("P", palette=Image.ADAPTIVE, colors=255))
    frames[0].save(path, save_all=True, append_images=frames[1:], duration=round(1000 / ANIM_FPS), loop=0, optimize=True)
    print(path.name, frames[0].size, len(frames), "frames", round(path.stat().st_size / 1e6, 2), "MB")

def main():
    src = Image.open(SHOT).convert("RGB"); src_lin = to_linear(src)
    check_now(src, src_lin)
    fulls = {}
    for look in [NOW] + LOOKS:
        im = to_srgb(render(src_lin, look)); fulls[look.key] = im; im.save(HERE / f"mock-{look.key}.png"); print(f"mock-{look.key}.png")
    crop = (100, 604, 1440, 770)
    label_sheet([(look.label, fulls[look.key].crop(crop)) for look in [NOW] + LOOKS], HERE / "mock-compare.png",
                footer=["모든 안에서 같은 순간(같은 충전): 미라 12%, 엘라 62%, 카이 80%, 로언 38%, 쥐 55% / 92%. 실제 크기(1:1).",
                        "A~D: 칸 테두리 안을 가득 덮은 어둠이 왼쪽부터 걷히고, 걷힌 자리는 밝다(아이콘 포함). 다 밝아지면 발동한다."])
    time_sheet(HERE / "mock-time.png")
    tall_sheet(HERE / "mock-tall.png")
    # the empty cells: as now (bright bone) or unlit like a cell that has not charged
    b = LOOKS[1]; b_dark = to_srgb(render(src_lin, b, dark_empty=True)); b_dark.save(HERE / "mock-B-dark-empty.png"); print("mock-B-dark-empty.png")
    crop = (100, 604, 1440, 960)
    label_sheet([("B안, 빈 칸은 지금처럼 밝은 뼈색 (빈 칸이 충전 전의 아이템보다 밝다)", fulls["B"].crop(crop)),
                 ("B안 + 빈 칸도 불이 꺼진 칸으로 (권장, 지시 밖): 보드가 어둡고 충전된 만큼만 밝다", b_dark.crop(crop))], HERE / "mock-empty.png")
    anim_gif(src_lin, HERE / "mock-anim.gif", [NOW] + LOOKS)

if __name__ == "__main__": main()
