"""Candle light over the battle stage (round 16): the stage is lit in a half disc around the storm candle and darkens
with the distance from it, the way the torch lights the corridor in Darkest Dungeon. Drawn over the current battle
screenshot with the game's own background and candle sprites, no API call.
  .venv/bin/python ArtPipeline/Archive/16-candle-light/mock_candle_light.py

How the sheets are made, so that they show what the game would draw:
- The game blends its UI in linear light (ProjectSettings m_ActiveColorSpace 1), so every layer here is blended there too.
- The screenshot's screen vignette is taken out first. The game's vignette piece (Assets/@Art/UI/Icon/vignette.png, drawn
  by Archive/09-battle-ui-feel/draw_pieces.py) is inside out: opaque in the middle and clear at the edges, so the middle
  of the screen is the darkest now (about 31% black at the centre, 1% in the corners). The variants draw it the way
  Architecture/12 and the approved Diablo mockup have it: clear in the middle, dark at the edges, 35% as now.
- The light is a dark layer right over the background, under everything else (units, plates, potions, numbers, panel).
  Which pixels of the screenshot are background is found by comparing it with the background drawn where the game
  draws it; the units, the plates and the numbers keep their brightness, except in C where the units take a share.
"""
from dataclasses import dataclass
from pathlib import Path
from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageFont, ImageMath

HERE = Path(__file__).resolve().parent; ROOT = HERE.parents[2]
SHOT = ROOT / "ArtPipeline/Archive/15-seven-cells/game/ko_05_battle.png"     # the battle at 7.3 s (round 15's screenshot)
BACKGROUND = ROOT / "Assets/@Art/Background/Dungeon/abandoned_mine.png"
VIGNETTE = ROOT / "Assets/@Art/UI/Icon/vignette.png"
FRAME = ROOT / "Assets/@Art/UI/Frame"; ICON = ROOT / "Assets/@Art/UI/Icon"
FONT = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"

W, H = 1920, 1080
GAMMA = 2.2
SCREEN_VIGNETTE = 0.35                         # UiPrefabSetup.Kit.ScreenVignetteAlpha
STAGE_TOP, STAGE_BOTTOM = 84, 620              # UiPrefabSetup.Battle.StageFxTop, BoardPanelTop: where the background shows
BG_TOP = 206 + 300 - 0.57 * 1280               # BattleFieldTop + BattleFloorY - BattleBackgroundFloor * height = -223.6
BG_TEX = (2048, 1365)                          # the Standalone import's max size (2048) of the 2304x1536 background
UI_BOXES = [(30, 92, 262, 176), (278, 110, 664, 158)]   # the potion belt and the chosen potion's plate over the stage
# A screenshot pixel this close to the background (0..255, any channel) is background: a little in flat places, more
# where the background has edges (its compression and resampling miss there).
BG_MATCH, BG_MATCH_EDGE = 10, 0.5
FIGURE_BAND = (215, 508)                       # the figures' rows on the stage: above the plates, below the potions
NUMBERS = (1040, 95, 1580, 232)                # the floating numbers of the screenshot

# The candle (UiPrefabSetup.Battle.BuildCandle, CandleView) and the storm (BalanceData, BattleScreen)
BOX_BOTTOM = 620 + 12 + 300                    # BoardPanelTop + CandleTop + CandleHeight
BODY_BOTTOM, BODY_W, BODY_H = 48, 56, 160
ART_BOTTOM, ART_TOP, STUB = 0.08, 0.92, 0.13
FLAME_W, FLAME_H, GLOW = 26, 48, 280
GLOW_TINT = (1.0, 0.78, 0.42, 0.45)            # the glow behind the flame
GUTTER = 0.55                                  # CandleView.GutterScale
STORM_MS, DUSK_MS = 45000, 10000               # BalanceData.StormStartMs, BattleScreen.StormDuskMs
SHOT_MS = 7300
TEXT, TEXT_DIM, BURN = (0xEB, 0xEB, 0xE6), (0x9A, 0xA0, 0xAC), (0xF3, 0x9C, 0x12)   # UiPalette


@dataclass
class Light:
    key: str
    label: str
    lit: float          # from the flame out to here the background is as lit as now
    dark_at: float      # from here on the darkness is full
    dark: float         # the full darkness: black over the background at this alpha (blended in linear light, like the UI)
    warm: float         # the candle's warm light on the background at the flame (alpha), fading out by warm_r
    warm_r: float
    units: float = 0.0  # the share of the darkness the units take (0: they stay as bright as now)
    wide: float = 1.0   # the half disc's width over its height: the stage is wide and low, so the light reaches further sideways


VARIANTS = [
    Light("A", "A  은은한 빛 — 가장자리만 어둡게", lit=380, dark_at=1250, dark=0.60, warm=0.08, warm_r=650),
    Light("B", "B  반원의 빛 (권장) — 가운데 둘이 밝고 뒤 열로 갈수록 어둡게", lit=230, dark_at=800, dark=0.88, warm=0.16, warm_r=620, wide=1.35),
    Light("C", "C  좁혀 오는 어둠 — 가운데만 밝고 유닛 그림도 어둠에 잠김", lit=140, dark_at=600, dark=0.94, warm=0.18, warm_r=480, units=0.55, wide=1.25),
]


# --- linear light -------------------------------------------------------------------------------------------------
def fmap(fn, **images):
    return ImageMath.lambda_eval(lambda a: fn(a), **images)

def to_linear(im):
    return [fmap(lambda a: (a["c"] / 255.0) ** GAMMA, c=c.convert("F")) for c in im.convert("RGB").split()]

def to_srgb(chs):
    return Image.merge("RGB", [fmap(lambda a: a["min"](a["max"](a["c"], 0.0), 1.0) ** (1.0 / GAMMA) * 255.0 + 0.5, c=c).convert("L") for c in chs])

def gray(value):
    return Image.new("F", (W, H), float(value))

def ramp(axis):
    """An F image holding each pixel's centre x (axis 0) or y (axis 1)."""
    n = W if axis == 0 else H
    line = Image.new("F", (n, 1) if axis == 0 else (1, n)); line.putdata([float(i) + 0.5 for i in range(n)])
    return line.resize((W, H), Image.NEAREST)

XS, YS = ramp(0), ramp(1)
FIGURES = None                                 # figure_band(), set in main()
STAGE = Image.new("F", (W, H), 0.0); STAGE.paste(1.0, (0, STAGE_TOP, W, STAGE_BOTTOM))


# --- the screen vignette: as the game has it (inside out) and the right way round --------------------------------
def vignette_now():
    a = Image.open(VIGNETTE).getchannel("A").resize((W, H), Image.BILINEAR).convert("F")
    return fmap(lambda m: m["a"] / 255.0, a=a)

def vignette_fixed(size=512):
    """draw_pieces.vignette with the ramp turned round: clear in the middle, opaque towards the edges."""
    im = Image.new("L", (size, size), 0); d = ImageDraw.Draw(im); c = size / 2; steps = 64
    for i in range(steps):
        t = i / (steps - 1); r = (size * 0.78) * (1 - t)
        d.ellipse([c - r, c - r, c + r, c + r], fill=int(255 * ((1 - t) ** 1.6)))
    a = im.filter(ImageFilter.GaussianBlur(size / 24)).resize((W, H), Image.BILINEAR).convert("F")
    return fmap(lambda m: m["a"] / 255.0, a=a)


# --- which pixels of the screenshot are background ---------------------------------------------------------------
def background_drawn():
    """The background where the game draws it: imported at 2048 wide, drawn 1920 wide with its top at -223.6."""
    tex = Image.open(BACKGROUND).convert("RGB").resize(BG_TEX, Image.LANCZOS)
    sx, sy = BG_TEX[0] / 1920.0, BG_TEX[1] / 1280.0
    return tex.transform((W, H), Image.AFFINE, data=(sx, 0, 0, 0, sy, -BG_TOP * sy), resample=Image.BILINEAR)

def unit_mask(clean, bg):
    """1 where something stands in front of the background on the stage (a unit, a plate, a number, the potions), else 0."""
    lin = [(v / 255.0) ** GAMMA for v in range(256)]
    box = (0, STAGE_TOP, W, STAGE_BOTTOM); size = (W, STAGE_BOTTOM - STAGE_TOP)
    gray = bg.convert("L"); edge = ImageChops.subtract(gray.filter(ImageFilter.MaxFilter(5)), gray.filter(ImageFilter.MinFilter(5)))
    out = bytearray(size[0] * size[1])
    for i, (x, b, e) in enumerate(zip(clean.crop(box).getdata(), bg.crop(box).getdata(), edge.crop(box).getdata())):
        if max(abs(x[0] - b[0]), abs(x[1] - b[1]), abs(x[2] - b[2])) <= BG_MATCH + BG_MATCH_EDGE * e:
            continue
        # the floor under a figure's shadow is the background darkened evenly, the same share in every channel
        if x[0] <= b[0] + 3 and x[1] <= b[1] + 3 and x[2] <= b[2] + 3:
            r = [lin[x[k]] / max(lin[b[k]], 1e-4) for k in range(3)]
            if min(r) >= 0.4 and max(r) - min(r) <= 0.12:
                continue
        out[i] = 255
    m = Image.frombytes("L", size, bytes(out))
    d = ImageDraw.Draw(m)
    for x0, y0, x1, y1 in UI_BOXES:
        d.rectangle([x0, y0 - STAGE_TOP, x1, y1 - STAGE_TOP], fill=255)
    m = m.filter(ImageFilter.MinFilter(3)).filter(ImageFilter.MaxFilter(3))      # drop single specks of mismatch
    m = m.filter(ImageFilter.MaxFilter(9)).filter(ImageFilter.MinFilter(9))      # close the units' ink lines
    # fill what the units enclose: background not reached from the stage's edges is inside a unit
    inv = ImageChops.invert(m); w, h = size
    for seed in [(x, 0) for x in range(0, w, 8)] + [(x, h - 1) for x in range(0, w, 8)] + [(0, y) for y in range(0, h, 8)] + [(w - 1, y) for y in range(0, h, 8)]:
        if inv.getpixel(seed) == 255:
            ImageDraw.floodfill(inv, seed, 128)
    m = inv.point(lambda v: 255 if v == 255 else (0 if v == 128 else 255))
    m = m.filter(ImageFilter.GaussianBlur(1.2))
    full = Image.new("L", (W, H), 0); full.paste(m, (0, STAGE_TOP))
    return fmap(lambda a: a["m"] / 255.0, m=full.convert("F"))

def figure_band():
    """1 over the figures' rows, without the potions and the numbers: in C the figures take part of the darkness, the
    plates, the potions and the numbers (UI over the light) do not."""
    band = Image.new("L", (W, H), 0); d = ImageDraw.Draw(band)
    d.rectangle([0, FIGURE_BAND[0], W, FIGURE_BAND[1]], fill=255)
    for x0, y0, x1, y1 in UI_BOXES + [NUMBERS]:
        d.rectangle([x0, y0, x1, y1], fill=0)
    return fmap(lambda a: a["b"] / 255.0, b=band.filter(ImageFilter.GaussianBlur(4)).convert("F"))


# --- the light ------------------------------------------------------------------------------------------------------
def remaining(ms):
    """CandleView.Show: the share of the body's sprite the wax still fills."""
    if ms >= STORM_MS:
        return STUB
    return max(STUB, ART_BOTTOM + (ART_TOP - ART_BOTTOM) * (1 - ms / STORM_MS))

def closeness(ms):
    return 1.0 if ms >= STORM_MS else min(1.0, max(0.0, (ms - (STORM_MS - DUSK_MS)) / DUSK_MS))

def flame_centre(ms):
    """The flame's middle on the screen: its foot stands on the wax's top (topY - 2) and it is FLAME_H tall."""
    return 960.0, BOX_BOTTOM - (BODY_BOTTOM + BODY_H * remaining(ms)) + 2 - FLAME_H / 2

def smoothstep(t):
    return t * t * (3.0 - 2.0 * t)

def light_layers(v, ms, out=False):
    """The darkness (alpha of black) and the warm light (alpha) over the background, as F images over the whole frame."""
    if out:
        return fmap(lambda a: a["s"] * v.dark, s=STAGE), gray(0.0)
    cx, cy = flame_centre(ms)
    # the light pulls in by a fifth over the storm's last seconds, as the flame gutters (CandleView: GutterScale)
    k = closeness(ms); reach = 1.0 - 0.2 * k; warmth = 1.0 - (1.0 - GUTTER) * k
    lit, span = v.lit * reach, (v.dark_at - v.lit) * reach
    dist = fmap(lambda a: (((a["x"] - cx) / v.wide) ** 2 + (a["y"] - cy) ** 2) ** 0.5, x=XS, y=YS)
    t = fmap(lambda a: a["min"](a["max"]((a["d"] - lit) / span, 0.0), 1.0), d=dist)
    dark = fmap(lambda a: a["t"] * a["t"] * (3.0 - 2.0 * a["t"]) * v.dark * a["s"], t=t, s=STAGE)
    u = fmap(lambda a: a["min"](a["d"] / (v.warm_r * reach), 1.0), d=dist)
    warm = fmap(lambda a: (1.0 - a["u"] * a["u"] * (3.0 - 2.0 * a["u"])) * v.warm * warmth * a["s"], u=u, s=STAGE)
    return dark, warm

def lit_frame(clean, mask, v, vfix, ms=SHOT_MS, out=False):
    """The undone screenshot (linear) with the light over its background, then the screen vignette the right way round."""
    dark, warm = light_layers(v, ms, out)
    tint = [c ** GAMMA for c in GLOW_TINT[:3]]
    figures = fmap(lambda a: a["m"] * a["b"] * v.units, m=mask, b=FIGURES)
    frame = []
    for k, c in enumerate(clean):
        y = fmap(lambda a: a["c"] * (1.0 - a["d"] * ((1.0 - a["m"]) + a["f"])), c=c, d=dark, m=mask, f=figures)
        y = fmap(lambda a: a["y"] * (1.0 - a["w"] * (1.0 - a["m"])) + tint[k] * a["w"] * (1.0 - a["m"]), y=y, w=warm, m=mask)
        frame.append(y)
    return frame

def vignetted(frame, v):
    return [fmap(lambda a: a["c"] * (1.0 - SCREEN_VIGNETTE * a["v"]), c=c, v=v) for c in frame]


# --- the candle at another moment (for the time strip) -------------------------------------------------------------
def over(frame, sprite, x, y, w, h, tint=(1.0, 1.0, 1.0), alpha=1.0, preserve=True, crop_bottom=None):
    """Draws a sprite (sRGB) into its box over the linear frame, blended in linear light as the UI does."""
    sp = sprite
    if crop_bottom is not None:                       # Image.Type.Filled, vertical from the bottom: keep the bottom share
        keep = max(1, round(sp.height * crop_bottom)); sp = sp.crop((0, sp.height - keep, sp.width, sp.height))
        h = h * crop_bottom; y = y + (h / crop_bottom - h)
    if preserve:                                      # KitIcon: preserveAspect, centred in the box
        s = min(w / sp.width, h / sp.height); nw, nh = sp.width * s, sp.height * s; x, y, w, h = x + (w - nw) / 2, y + (h - nh) / 2, nw, nh
    sp = sp.resize((max(1, round(w)), max(1, round(h))), Image.LANCZOS)
    layer = Image.new("RGBA", (W, H), (0, 0, 0, 0)); layer.alpha_composite(sp, (round(x), round(y)))
    a = fmap(lambda m: m["a"] / 255.0 * alpha, a=layer.getchannel("A").convert("F"))
    cols = to_linear(layer.convert("RGB"))
    return [fmap(lambda m: m["d"] * (1.0 - m["a"]) + m["s"] * (tint[k] ** GAMMA) * m["a"], d=frame[k], s=cols[k], a=a) for k in range(3)]

# The panel's gap between the two row 1 boards, where the candle, its light and its words are, and where clean stone
# can be taken from: right of the skulls and left of the right chain (the panel's stone is tiled, so a shift by the
# tile's width repeats it).
GAP = (876, STAGE_BOTTOM, 1044, 1012)
SHIFTS = range(744, 804)

def stone_shift(img):
    """The shift along x, among those that land on clean stone, that repeats the stone under the gap best."""
    ref = img.crop((GAP[0], 1012, GAP[2], 1070)); best = None
    for s in SHIFTS:
        diff = ImageChops.difference(ref, img.crop((GAP[0] + s, 1012, GAP[2] + s, 1070))).convert("L")
        score = sum(i * n for i, n in enumerate(diff.histogram()))
        if best is None or score < best[0]:
            best = (score, s)
    return best[1]

def candle_at(clean, ms):
    """The undone screenshot (linear) with the candle, its light and its words at another moment: the gap is filled
    with clean stone and the candle drawn again with the game's sprites where CandleView puts them."""
    s = stone_shift(to_srgb(clean)); f = []
    for c in clean:
        c = c.copy(); c.paste(c.crop((GAP[0] + s, GAP[1], GAP[2] + s, GAP[3])), (GAP[0], GAP[1])); f.append(c)
    holder = Image.open(FRAME / "candle_holder.png").convert("RGBA"); body = Image.open(FRAME / "candle_body.png").convert("RGBA")
    top = Image.open(ICON / "candle_top.png").convert("RGBA"); flame = Image.open(FRAME / "candle_flame.png").convert("RGBA")
    glow = Image.open(ICON / "glow.png").convert("RGBA"); smoke = Image.open(ICON / "smoke.png").convert("RGBA")
    rem = remaining(ms); storm = ms >= STORM_MS; g = 1.0 - (1.0 - GUTTER) * closeness(ms)
    top_y = BOX_BOTTOM - (BODY_BOTTOM + BODY_H * rem); foot = top_y + 2
    if not storm:                                     # the glow is the flame's child: it scales with the flame
        f = over(f, glow, 960 - GLOW * g / 2, foot - FLAME_H * g / 2 - GLOW * g / 2, GLOW * g, GLOW * g, tint=GLOW_TINT[:3], alpha=GLOW_TINT[3] * g)
    f = over(f, holder, 960 - 60, BOX_BOTTOM - 62, 120, 62)
    f = over(f, body, 960 - BODY_W / 2, BOX_BOTTOM - BODY_BOTTOM - BODY_H, BODY_W, BODY_H, preserve=False, crop_bottom=rem)
    f = over(f, top, 960 - (BODY_W + 8) / 2, top_y - 10, BODY_W + 8, 20)
    if storm:
        f = over(f, smoke, 960 - 20, top_y - 90 - 20, 40, 90, alpha=0.75)
    else:
        f = over(f, flame, 960 - FLAME_W * g / 2, foot - FLAME_H * g, FLAME_W * g, FLAME_H * g)
    return f

def clock_words(img, ms):
    """The battle time and the storm's words under the holder (BattleScreen.RenderClock)."""
    d = ImageDraw.Draw(img); storm = ms >= STORM_MS
    d.text((960, 952), f"{ms / 1000:.1f}초", font=font(28), fill=TEXT, anchor="mm")
    words = "폭풍! 다음 피해 2" if storm else f"폭풍까지 {(STORM_MS - ms) / 1000:.1f}초"
    wf = font(18); tw = d.textlength(words, font=wf); x0 = 960 - (26 + 8 + tw) / 2
    icon = Image.open(ICON / "storm.png").convert("RGBA").resize((26, 26), Image.LANCZOS); img.paste(icon, (round(x0), 977), icon)
    d.text((x0 + 34, 990), words, font=wf, fill=BURN if storm else TEXT_DIM, anchor="lm")
    return img


# --- sheets -------------------------------------------------------------------------------------------------------
def font(size):
    return ImageFont.truetype(str(FONT), size)

def wrap(text, width, f):
    """Splits a line at spaces so that each piece fits the width."""
    lines, line = [], ""
    for word in text.split(" "):
        test = (line + " " + word).strip()
        if line and ImageDraw.Draw(Image.new("L", (1, 1))).textlength(test, font=f) > width:
            lines.append(line); line = word
        else:
            line = test
    return lines + [line]

def sheet(entries, cols, path, scale=0.5, lab=46, gap=24, size=28, footer=None):
    tiles = [(name, im.resize((round(im.width * scale), round(im.height * scale)), Image.LANCZOS)) for name, im in entries]
    w, h = tiles[0][1].size; rows = (len(tiles) + cols - 1) // cols
    lines = [piece for text in (footer or []) for piece in wrap(text, cols * (w + gap) - gap, font(22))]
    fh = 18 + 34 * len(lines)
    out = Image.new("RGB", (cols * (w + gap) + gap, rows * (h + lab + gap) + gap + fh), (18, 18, 18)); d = ImageDraw.Draw(out)
    for i, (name, im) in enumerate(tiles):
        x = gap + (i % cols) * (w + gap); y = gap + (i // cols) * (h + lab + gap)
        d.text((x, y), name, font=font(size), fill=(235, 235, 235)); out.paste(im, (x, y + lab))
    for j, line in enumerate(lines):
        d.text((gap, out.height - fh + 6 + 34 * j), line, font=font(22), fill=(190, 190, 190))
    out.save(path); print(path.name, out.size)


def main():
    shot = Image.open(SHOT).convert("RGB")
    vnow, vfix = vignette_now(), vignette_fixed()
    # take the inside-out vignette out of the screenshot
    clean = [fmap(lambda a: a["c"] / (1.0 - SCREEN_VIGNETTE * a["v"]), c=c, v=vnow) for c in to_linear(shot)]
    mask = unit_mask(to_srgb(clean), background_drawn())
    global FIGURES; FIGURES = figure_band()
    to_srgb([mask, mask, mask]).save(HERE / "mask-units.png")

    shot.save(HERE / "mock-now.png")
    to_srgb(vignetted(clean, vfix)).save(HERE / "mock-vignette-fixed.png")   # only the vignette the right way round
    fulls = {"now": shot}
    for v in VARIANTS:
        img = to_srgb(vignetted(lit_frame(clean, mask, v, vfix), vfix)); img.save(HERE / f"mock-{v.key}.png"); fulls[v.key] = img

    sheet([("지금 (비네트가 뒤집혀 가운데가 가장 어둡다)", fulls["now"])] + [(v.label, fulls[v.key]) for v in VARIANTS], 2, HERE / "mock-compare.png",
          footer=["빛의 중심은 양초의 불꽃. 빛은 배경 바로 위의 어둠 층이라 유닛·명패·숫자·포션·패널은 지금 밝기 그대로다(C만 유닛 그림이 배경 어둠의 55%만큼 어두워진다).",
                  "A·B·C는 뒤집힌 화면 비네트를 바로잡은 상태다(가장자리 35%). 수치는 게임처럼 선형 색공간에서 섞었다."])

    # B over the battle: the light follows the flame down as the candle burns, pulls in as the storm nears, goes out with it
    b = next(v for v in VARIANTS if v.key == "B"); moments = [(0, "전투 시작 (0초)"), (40000, "폭풍 5초 전 (40초)"), (45000, "폭풍 (불이 꺼짐)")]
    strip = []
    for ms, name in moments:
        img = to_srgb(vignetted(lit_frame(candle_at(clean, ms), mask, b, vfix, ms=ms, out=ms >= STORM_MS), vfix))
        img = clock_words(img, ms); img.save(HERE / f"mock-B-{ms // 1000:02d}s.png"); strip.append((name, img.crop((0, STAGE_TOP, W, 1012))))
    sheet(strip, 1, HERE / "mock-B-time.png", scale=0.5, footer=[
        "B안의 시간 흐름. 빛의 중심이 불꽃이라 양초가 타면 빛도 함께 내려가 무대가 조금씩 어두워진다. 폭풍 10초 전부터는 불꽃이 작아지며 빛이 1/5 줄고, 폭풍이 오면 불이 꺼져 무대의 배경 전체가 가장 어두운 값이 된다. 지금의 '폭풍의 어스름' 비네트는 이것이 대신한다.",
        "유닛과 떠오른 숫자는 7.3초 스크린샷 그대로다. 양초·빛·시간 글만 그 순간에 맞춰 게임의 조각으로 다시 그렸다."])


if __name__ == "__main__":
    main()
