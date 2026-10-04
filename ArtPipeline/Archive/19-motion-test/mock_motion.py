"""Attack and hit poses in the battle (round 19): the valkyrie's approved figure is swapped for her attack pose while her
weapon's lunge plays and for her hit pose while a blow's recoil and flash play. Drawn over the game's own screenshot with
the game's own sprites and its own motion, no API call.
  .venv/bin/python ArtPipeline/Archive/19-motion-test/fit_motion.py      (first: the poses at the figure's size)
  .venv/bin/python ArtPipeline/Archive/19-motion-test/mock_motion.py

What is drawn and how, so that it shows what the game would draw:
- The screenshot is round 18's ko_15 (floor 4, Astrid in row 1). In it she is in the middle of a lunge (+25) and the goblins
  are walking into the rows the overseer left (+75). Here every unit stands in its own column (FieldLayout: eight columns
  of 180, the two row 1 columns 180 apart), and the overseer's ghost and the floating words are gone.
- What shows behind them is the stage as the game lights it (UiPrefabSetup.Battle.BuildStageLight, CandleView): the
  dungeon's background where the battle screen puts it, the half disc of darkness and the warm light around the flame,
  the screen's vignette over everything, blended in linear light as the game's UI is. The model is checked against the
  screenshot where the background shows (printed).
- Units are the game's figures (Assets/@Art/Unit) in their places (225x300, feet on the floor line), each with its shadow
  (150x26, 35% black, 6 above the floor), the front row over the row behind. A pose is drawn on fit_motion.py's wide
  canvas (twice as wide and an eighth deeper, the same top and floor line), so its place is 450x338 and a weapon may
  come down below the floor. The attack is the second one (valkyrie_attack2). Plates move with their units and are drawn
  over them, as in the game.
- The motion is the game's: the lunge (BattleUnitView: 26 out in 0.1 s, back in 0.2 s), the recoil (12, easing out over
  0.26 s) and the flash (FigureView: HitTint (1, 0.55, 0.5) fading over 0.28 s), at half strength on a unit that shows a
  hit pose (user's decision 2026-10-04, "권장안대로"). The pose is shown while the lunge or the recoil plays and 0.05 s more.
- Out: the three moments full size and side by side, and the loop as a GIF (plays in a browser; a preview that shows a
  GIF's first frame only shows no motion), an HTML player (speed and frame by frame) and an MP4 (encode_mp4.swift:
  three loops at full speed, then one four times slower).
"""
import base64
import shutil
import subprocess
from io import BytesIO
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter, ImageFont, ImageMath

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
SHOT = ROOT / "ArtPipeline/Archive/18-cooldown-light/game/ko_15_battle_after_advance.png"
BACKGROUND = ROOT / "Assets/@Art/Background/Dungeon/abandoned_mine.png"
VIGNETTE = ROOT / "Assets/@Art/UI/Icon/vignette.png"
UNIT = ROOT / "Assets/@Art/Unit"
POSES = ROOT / "ArtPipeline/output/character"
# The figures the screenshot was taken with: what is found and taken off it. They were replaced after it (round 20, the
# serious face), and what is drawn again is the game's figures now (UNIT).
SHOT_ART = ROOT / "ArtPipeline/Archive/20-serious-face/before"
FONT = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"

W, H = 1920, 1080
GAMMA = 2.2
STAGE_TOP, STAGE_BOTTOM = 84, 620                     # UiPrefabSetup.Battle: StageFxTop, BoardPanelTop
FLOOR = 206 + 300                                     # BattleFieldTop + BattleFloorY
BG_TEX = (2048, 1365)                                 # the Standalone import's max size of the 2304x1536 background
BG_TOP = FLOOR - 0.57 * 1280                          # the background's floor (57%) on the field's floor
SCREEN_VIGNETTE = 0.35                                # UiPrefabSetup.Kit.ScreenVignetteAlpha

# The stage light (UiPrefabSetup.Battle, Round 16's draw_pieces.py), on the flame's middle (CandleView) at 3.6 s.
LIGHT_REACH, LIGHT_WIDE, WARM_REACH = 800.0, 1.35, 620.0
DARKNESS, WARMTH = 0.88, 0.16
LIT = 230 / 800
CANDLE_LIGHT = (1.0, 0.78, 0.42)
STORM_MS, SHOT_MS = 45000, 3600
REMAINING = max(0.13, 0.08 + 0.84 * (1 - SHOT_MS / STORM_MS))
FLAME = (960.0, 620 + 12 + 300 - (48 + 160 * REMAINING) + 2 - 48 / 2)

# The places (FieldLayout with four party rows and four enemy rows: columns of 180, 10 apart, the sides 180 apart).
COLUMN = 180
PLACE_W, PLACE_H = 225, 300
SHADOW_W, SHADOW_H, SHADOW_ALPHA, SHADOW_UP = 150, 26, 0.35, 6
ASTRID = 120 + 3 * (COLUMN + 10) + COLUMN / 2                  # party row 1: 780
ENEMY_1 = 120 + 4 * COLUMN + 3 * 10 + 180 + COLUMN / 2          # enemy row 1: 1140
ENEMY_2 = ENEMY_1 + COLUMN + 10                                 # enemy row 2: 1330
SHOT_WALK = 75                                                  # how far the screenshot's goblins were from their columns
PLATE_TOP, PLATE_H = 206 + 306, 92
PLATE_REACH = 6

# What the game does (BattleUnitView, FigureView, BattlePresenter).
LUNGE_OUT, LUNGE_BACK, LUNGE = 0.1, 0.2, 26.0
RECOIL_T, RECOIL = 0.26, 12.0
FLASH_T, HIT_TINT = 0.28, (1.0, 0.55, 0.5)
POSE_FLASH = 0.5                                      # the flash on a unit that shows a hit pose
POSE_EXTRA = 0.05                                     # how long a pose stays after its motion ends
FIGURE_H = 896                                        # the figure canvas's height: the place's 300

FPS = 30
SLOW = 4                                              # the MP4's slow loop: four times slower
CROP = (440, STAGE_TOP, 1540, STAGE_BOTTOM)           # the moving pictures: the two row 1s and their neighbours
FRAMES_DIR = ROOT / "ArtPipeline/output/motion_frames"


# --- linear light ------------------------------------------------------------------------------------------------------
def fmap(fn, **images):
    return ImageMath.lambda_eval(lambda a: fn(a), **images)

def to_linear(im):
    return [fmap(lambda a: (a["c"] / 255.0) ** GAMMA, c=c.convert("F")) for c in im.convert("RGB").split()]

def to_srgb(chs):
    return Image.merge("RGB", [fmap(lambda a: a["min"](a["max"](a["c"], 0.0), 1.0) ** (1.0 / GAMMA) * 255.0 + 0.5, c=c).convert("L") for c in chs])

def ramp(axis):
    n = W if axis == 0 else H
    line = Image.new("F", (n, 1) if axis == 0 else (1, n))
    line.putdata([float(i) + 0.5 for i in range(n)])
    return line.resize((W, H), Image.NEAREST)


# --- the stage without anyone on it --------------------------------------------------------------------------------------
def background_drawn():
    """The background where the game draws it: imported at 2048 wide, drawn 1920 wide with its floor on the field's floor."""
    tex = Image.open(BACKGROUND).convert("RGB").resize(BG_TEX, Image.LANCZOS)
    sx, sy = BG_TEX[0] / 1920.0, BG_TEX[1] / 1280.0
    return tex.transform((W, H), Image.AFFINE, data=(sx, 0, 0, 0, sy, -BG_TOP * sy), resample=Image.BILINEAR)

def vignette():
    a = Image.open(VIGNETTE).getchannel("A").resize((W, H), Image.BILINEAR).convert("F")
    return fmap(lambda m: 1.0 - SCREEN_VIGNETTE * m["a"] / 255.0, a=a)

def stage_lit(vig):
    """The empty stage in linear light: the background, the darkness and the warm light around the flame, the vignette."""
    xs, ys = ramp(0), ramp(1)
    stage = Image.new("F", (W, H), 0.0)
    stage.paste(1.0, (0, STAGE_TOP, W, STAGE_BOTTOM))
    cx, cy = FLAME
    d = fmap(lambda a: (((a["x"] - cx) / (LIGHT_REACH * LIGHT_WIDE)) ** 2 + ((cy - a["y"]) / LIGHT_REACH) ** 2) ** 0.5, x=xs, y=ys)
    t = fmap(lambda a: a["min"](a["max"]((a["d"] - LIT) / (1.0 - LIT), 0.0), 1.0), d=d)
    dark = fmap(lambda a: a["t"] * a["t"] * (3.0 - 2.0 * a["t"]) * DARKNESS * a["s"], t=t, s=stage)
    dw = fmap(lambda a: a["min"]((((a["x"] - cx) / (WARM_REACH * LIGHT_WIDE)) ** 2 + ((cy - a["y"]) / WARM_REACH) ** 2) ** 0.5, 1.0), x=xs, y=ys)
    warm = fmap(lambda a: (1.0 - a["u"] * a["u"] * (3.0 - 2.0 * a["u"])) * WARMTH * a["s"], u=dw, s=stage)
    out = []
    for k, c in enumerate(to_linear(background_drawn())):
        tint = CANDLE_LIGHT[k] ** GAMMA
        y = fmap(lambda a: (a["c"] * (1.0 - a["d"]) * (1.0 - a["w"]) + tint * a["w"]) * a["v"], c=c, d=dark, w=warm, v=vig)
        out.append(y)
    return out

def check(stage_srgb, shot):
    """How far the modelled stage is from the screenshot where only the background shows: mean and worst channel, 0..255."""
    regions = {"위 왼쪽": (300, 90, 640, 200), "위 가운데": (880, 90, 1000, 200), "가운데 틈": (900, 230, 980, 480),
               "위 오른쪽": (1560, 90, 1900, 300), "바닥 가운데": (905, 500, 1115, 612), "오른쪽 끝": (1800, 330, 1910, 600)}
    for name, box in regions.items():
        a, b = stage_srgb.crop(box), shot.crop(box)
        diffs = [abs(p - q) for pa, pb in zip(a.getdata(), b.getdata()) for p, q in zip(pa, pb)]
        print(f"  {name:6s} {box}: 평균 {sum(diffs) / len(diffs):.1f}, 큰 쪽 5% {sorted(diffs)[int(len(diffs) * 0.95)]}")


def corrected(stage, shot_lin, shot, stage_srgb, cell=24):
    """The modelled stage brought onto the screenshot's own light: the slow difference between them (the light's falloff,
    the vignette, the importer's resampling) measured where the screenshot shows only background, averaged in cells of
    this size and spread over the cells that show none, then laid over the model. What it leaves is the texture's own
    small differences, so a patch of the model meets the screenshot without a seam."""
    keep = Image.new("L", (W, H), 0)
    keep.putdata([255 if max(abs(p - q) for p, q in zip(pa, pb)) <= 14 else 0 for pa, pb in zip(shot.getdata(), stage_srgb.getdata())])
    band = Image.new("L", (W, H), 0)
    band.paste(255, (0, STAGE_TOP, W, STAGE_BOTTOM))
    m = fmap(lambda a: a["k"] / 255.0 * a["b"] / 255.0, k=keep.convert("F"), b=band.convert("F"))
    low = (W // cell, H // cell)
    cover = list(m.resize(low, Image.BOX).getdata())
    out = []
    for k in range(3):
        num = list(fmap(lambda a: a["s"] * a["m"], s=shot_lin[k], m=m).resize(low, Image.BOX).getdata())
        den = list(fmap(lambda a: a["c"] * a["m"], c=stage[k], m=m).resize(low, Image.BOX).getdata())
        ratio = [n / d if c > 0.15 and d > 1e-5 else None for n, d, c in zip(num, den, cover)]
        while any(r is None for r in ratio):
            filled = list(ratio)
            for i, r in enumerate(ratio):
                if r is None:
                    x, y = i % low[0], i // low[0]
                    near = [ratio[j * low[0] + i2] for i2, j in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1))
                            if 0 <= i2 < low[0] and 0 <= j < low[1] and ratio[j * low[0] + i2] is not None]
                    if near:
                        filled[i] = sum(near) / len(near)
            ratio = filled
        grid = Image.new("F", low)
        grid.putdata(ratio)
        grid = grid.resize((low[0] * 4, low[1] * 4), Image.BILINEAR).resize(low, Image.BOX)   # a little smoother
        r = grid.resize((W, H), Image.BILINEAR)
        out.append(fmap(lambda a: a["c"] * a["r"], c=stage[k], r=r))
    return out


# --- sprites -----------------------------------------------------------------------------------------------------------
def figure_sprite(path, wide=False):
    """A figure's art as the screen draws it: the canvas stretched to its place (225x300, or 450x338 on the wide canvas,
    whose top and floor line are the figure canvas's)."""
    im = Image.open(path).convert("RGBA")
    return im.resize((PLACE_W * (2 if wide else 1), round(PLACE_H * im.height / FIGURE_H)), Image.LANCZOS)

def shadow_sprite():
    """The built-in Knob as the shadow: a white disc with a soft rim, stretched to 150x26."""
    big = Image.new("L", (SHADOW_W * 4, SHADOW_H * 4), 0)
    ImageDraw.Draw(big).ellipse([2, 2, big.width - 3, big.height - 3], fill=255)
    a = big.filter(ImageFilter.GaussianBlur(3)).resize((SHADOW_W, SHADOW_H), Image.LANCZOS)
    out = Image.new("RGBA", (SHADOW_W, SHADOW_H), (0, 0, 0, 0))
    out.putalpha(a)
    return out

def over(frame, sprite, x, y, vig, box, tint=(1.0, 1.0, 1.0), alpha=1.0, black=False, vignetted=True):
    """Draws an sRGB sprite at screen (x, y) over the linear frame of the screen's box, blended in linear light as the UI
    is, under the vignette. A sprite cut out of the screenshot has the vignette in it already (vignetted=False)."""
    w, h = box[2] - box[0], box[3] - box[1]
    layer = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    if _inside(sprite, x, y, box):
        layer.alpha_composite(sprite, (round(x) - box[0], round(y) - box[1]))
    else:
        _clip(layer, sprite, x, y, box)
    a = fmap(lambda m: m["a"] / 255.0 * alpha, a=layer.getchannel("A").convert("F"))
    if black:
        return [fmap(lambda m: m["d"] * (1.0 - m["a"]), d=frame[k], a=a) for k in range(3)]
    cols = to_linear(layer.convert("RGB"))
    v = vig if vignetted else Image.new("F", (w, h), 1.0)
    return [fmap(lambda m: m["d"] * (1.0 - m["a"]) + m["s"] * (tint[k] ** GAMMA) * m["v"] * m["a"], d=frame[k], s=cols[k], a=a, v=v)
            for k in range(3)]

def _inside(sprite, x, y, box):
    return box[0] <= round(x) and box[1] <= round(y) and round(x) + sprite.width <= box[2] and round(y) + sprite.height <= box[3]

def _clip(layer, sprite, x, y, box):
    """Pastes the part of the sprite that falls inside the box."""
    big = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    big.alpha_composite(sprite, (round(x), round(y)))
    layer.alpha_composite(big.crop(box))

def find_astrid(shot, idle):
    """Where the screenshot has her: the left edge of her place that matches her art best (she was lunging), and how
    far her breathing stretched her (FigureView: up to 2% from the feet)."""
    best = None
    for stretch in (1.0, 1.005, 1.01, 1.015, 1.02):
        h = round(PLACE_H * stretch)
        art = idle.resize((PLACE_W, h), Image.LANCZOS)
        solid = [(i % PLACE_W, i // PLACE_W) for i, a in enumerate(art.getchannel("A").getdata()) if a == 255][::7]
        for dx in range(18, 33):
            x0 = ASTRID - PLACE_W / 2 + dx
            score = 0
            for (i, j) in solid:
                p, q = art.getpixel((i, j)), shot.getpixel((round(x0) + i, FLOOR - h + j))
                score += abs(p[0] - q[0]) + abs(p[1] - q[1]) + abs(p[2] - q[2])
            score /= len(solid)
            if best is None or score < best[0]:
                best = (score, dx, stretch)
    return best

def plate_of(shot, stage, x0, x1):
    """A plate cut out of the screenshot: where it differs from the empty stage, with its own alpha."""
    box = (x0, PLATE_TOP - 4, x1, PLATE_TOP + PLATE_H + 6)
    a, b = shot.crop(box), stage.crop(box)
    mask = Image.new("L", a.size, 0)
    mask.putdata([255 if max(abs(p - q) for p, q in zip(pa, pb)) > 14 else 0 for pa, pb in zip(a.getdata(), b.getdata())])
    mask = mask.filter(ImageFilter.MaxFilter(3)).filter(ImageFilter.MinFilter(3))
    # A plate is one filled shape: whatever its outline encloses belongs to it.
    filled = Image.new("L", mask.size, 0)
    rows = [[mask.getpixel((i, j)) for i in range(mask.width)] for j in range(mask.height)]
    for j, row in enumerate(rows):
        on = [i for i, v in enumerate(row) if v]
        if on:
            for i in range(on[0], on[-1] + 1):
                filled.putpixel((i, j), 255)
    out = a.convert("RGBA")
    out.putalpha(filled.filter(ImageFilter.GaussianBlur(0.6)))
    return out, box


# --- the scene ---------------------------------------------------------------------------------------------------------
class Scene:
    def __init__(self):
        self.shot = Image.open(SHOT).convert("RGB")
        self.vig = vignette()
        self.stage = stage_lit(self.vig)
        stage_srgb = to_srgb(self.stage)
        print("빈 무대의 모형과 스크린샷의 차이(배경만 보이는 곳):")
        check(stage_srgb, self.shot)
        shot_lin = to_linear(self.shot)
        self.stage = corrected(self.stage, shot_lin, self.shot, stage_srgb)
        stage_srgb = to_srgb(self.stage)
        print("느린 차이를 맞춘 뒤:")
        check(stage_srgb, self.shot)

        self.idle = Image.open(SHOT_ART / "valkyrie.png").convert("RGBA")
        score, dx, stretch = find_astrid(self.shot, self.idle)
        print(f"스크린샷의 아스트리드: 열에서 +{dx} (돌진 중), 숨쉬기 {stretch:.3f}, 차이 {score:.1f}")

        # Everything this mockup draws again is taken off the screenshot: her (where her art was, and her shadow), her
        # plate, the overseer's ghost, the goblins and their plates, the floating words.
        erase = Image.new("L", (W, H), 0)
        h = round(PLACE_H * stretch)
        art = self.idle.resize((PLACE_W, h), Image.LANCZOS).getchannel("A").point(lambda v: 255 if v > 8 else 0)
        erase.paste(art.filter(ImageFilter.MaxFilter(7)), (round(ASTRID - PLACE_W / 2 + dx), FLOOR - h), art.filter(ImageFilter.MaxFilter(7)))
        d = ImageDraw.Draw(erase)
        sx = ASTRID + dx
        d.ellipse([sx - SHADOW_W / 2 - 6, FLOOR - SHADOW_UP - SHADOW_H / 2 - 5, sx + SHADOW_W / 2 + 6, FLOOR - SHADOW_UP + SHADOW_H / 2 + 5], fill=255)
        d.rectangle([700, 160, 850, 212], fill=255)                                   # "화상 +3"
        d.rectangle([948, STAGE_TOP, 1535, PLATE_TOP + PLATE_H + 4], fill=255)        # the enemy side: ghost, goblins, plates, words
        # A plate's outline and shadow reach a little past its column: take them too.
        d.rectangle([ASTRID + dx - COLUMN / 2 - PLATE_REACH, PLATE_TOP - 4, ASTRID + dx + COLUMN / 2 + PLATE_REACH, PLATE_TOP + PLATE_H + 6], fill=255)
        self.plates = {name: plate_of(self.shot, stage_srgb, round(x - COLUMN / 2 - PLATE_REACH), round(x + COLUMN / 2 + PLATE_REACH))
                       for name, x in (("astrid", ASTRID + dx), ("raider", ENEMY_1 + SHOT_WALK), ("shaman", ENEMY_2 + SHOT_WALK))}
        e = fmap(lambda m: m["e"] / 255.0, e=erase.filter(ImageFilter.GaussianBlur(1.0)).convert("F"))
        self.base = [fmap(lambda m: m["s"] * (1.0 - m["e"]) + m["b"] * m["e"], s=shot_lin[k], b=self.stage[k], e=e) for k in range(3)]

        self.shadow = shadow_sprite()
        self.sprites = {
            "idle": figure_sprite(UNIT / "Job/valkyrie.png"),
            "attack": figure_sprite(POSES / "valkyrie_attack2_wide.png", wide=True),
            "hit": figure_sprite(POSES / "valkyrie_hit_wide.png", wide=True),
            "raider": figure_sprite(UNIT / "Enemy/goblin_raider.png"),
            "shaman": figure_sprite(UNIT / "Enemy/goblin_shaman.png"),
        }

    def unit(self, frame, box, vig, centre, sprite, plate, tint=(1.0, 1.0, 1.0), plate_centre=None):
        """A unit: its shadow, its art and its plate. The plate stands at plate_centre when given (it stays in its
        column while the figure lunges or recoils), else it moves with the figure as the game has it now."""
        frame = over(frame, self.shadow, centre - SHADOW_W / 2, FLOOR - SHADOW_UP - SHADOW_H / 2, vig, box, alpha=SHADOW_ALPHA, black=True)
        frame = over(frame, sprite, centre - sprite.width / 2, FLOOR - PLACE_H, vig, box, tint=tint)
        image, cut = self.plates[plate]
        x = centre if plate_centre is None else plate_centre
        return over(frame, image, x - image.width / 2, cut[1], vig, box, vignetted=False)

    def frame(self, box=(0, 0, W, H), pose="idle", astrid_dx=0.0, astrid_tint=(1.0, 1.0, 1.0), raider_dx=0.0, raider_tint=(1.0, 1.0, 1.0),
              plates_follow=True, idle=None):
        """The screen (or the box of it) at one moment. plates_follow=False keeps every plate in its column (round 19's
        second review); idle draws another figure in place of the approved one (round 20)."""
        f = [c.crop(box) for c in self.base]
        vig = self.vig.crop(box)
        fixed = (lambda x: x) if not plates_follow else (lambda x: None)
        sprite = self.sprites[pose] if pose != "idle" or idle is None else idle
        f = self.unit(f, box, vig, ENEMY_2, self.sprites["shaman"], "shaman", plate_centre=fixed(ENEMY_2))
        f = self.unit(f, box, vig, ENEMY_1 + raider_dx, self.sprites["raider"], "raider", raider_tint, plate_centre=fixed(ENEMY_1))
        f = self.unit(f, box, vig, ASTRID + astrid_dx, sprite, "astrid", astrid_tint, plate_centre=fixed(ASTRID))
        return to_srgb(f)


# --- the motion ----------------------------------------------------------------------------------------------------------
def lunge(t):
    if t < 0 or t >= LUNGE_OUT + LUNGE_BACK:
        return 0.0
    return LUNGE * (t / LUNGE_OUT if t < LUNGE_OUT else 1.0 - (t - LUNGE_OUT) / LUNGE_BACK)

def recoil(t):
    return RECOIL * (1.0 - t / RECOIL_T) ** 2 if 0 <= t < RECOIL_T else 0.0

def flash(t, strength=1.0):
    k = min(1.0, max(0.0, (FLASH_T - t) / FLASH_T)) * strength if t >= 0 else 0.0
    return tuple(1.0 + (c - 1.0) * k for c in HIT_TINT)

ATTACK_AT, HIT_AT, LOOP = 0.5, 1.35, 2.2

def state(t):
    """What the stage shows t seconds into the loop: her weapon fires at ATTACK_AT, the raider's blow lands at HIT_AT."""
    a, h = t - ATTACK_AT, t - HIT_AT
    s = {"pose": "idle", "astrid_dx": 0.0, "astrid_tint": (1.0, 1.0, 1.0), "raider_dx": 0.0, "raider_tint": (1.0, 1.0, 1.0)}
    if 0 <= a < LUNGE_OUT + LUNGE_BACK + POSE_EXTRA:
        s["pose"] = "attack"
    s["astrid_dx"] += lunge(a)
    s["raider_dx"] += recoil(a)
    s["raider_tint"] = flash(a)
    if 0 <= h < RECOIL_T + POSE_EXTRA:
        s["pose"] = "hit"
    s["astrid_dx"] -= recoil(h)
    s["raider_dx"] -= lunge(h)
    if h >= 0:
        s["astrid_tint"] = flash(h, POSE_FLASH)
    return s


def label(img, text, sub=None):
    f = ImageFont.truetype(str(FONT), 30)
    g = ImageFont.truetype(str(FONT), 22)
    out = Image.new("RGB", (img.width, img.height + (92 if sub else 56)), (24, 22, 22))
    out.paste(img, (0, out.height - img.height))
    d = ImageDraw.Draw(out)
    d.text((16, 10), text, fill=(235, 235, 230), font=f)
    if sub:
        d.text((16, 52), sub, fill=(170, 172, 180), font=g)
    return out


def main():
    scene = Scene()
    keys = [
        ("mock-idle.png", "대기 — 지금의 확정 그림", "아스트리드(1열)와 고블린 약탈자(적 1열)·주술사(2열). 모두 제 열에 선 모습", 0.0),
        ("mock-attack.png", "공격하는 순간 — 공격 자세(2차) + 돌진 26", "대도끼가 발동한 0.1초 뒤(돌진의 끝). 도끼가 바닥선 아래로. 맞은 약탈자는 붉게 번쩍이며 12 밀린다(지금 그대로)", ATTACK_AT + LUNGE_OUT),
        ("mock-hit.png", "맞는 순간 — 피격 자세 + 밀림 + 절반 세기의 붉은 번쩍임", "약탈자의 일격이 들어온 0.05초 뒤. 자세가 있는 유닛은 번쩍임 절반(결정). 약탈자는 지금처럼 돌진한다", HIT_AT + 0.05),
    ]
    strip = []
    for name, title, sub, t in keys:
        img = scene.frame(**state(t))
        img.save(HERE / name)
        strip.append(label(img.crop(CROP), title, sub))
        print("목업:", name)
    sheet = Image.new("RGB", (strip[0].width, sum(s.height for s in strip) + 12 * (len(strip) - 1)), (12, 12, 12))
    y = 0
    for s in strip:
        sheet.paste(s, (0, y))
        y += s.height + 12
    sheet.save(HERE / "mock-compare.png")
    print("목업: mock-compare.png")

    normal = [scene.frame(CROP, **state(i / FPS)) for i in range(round(LOOP * FPS))]
    write_gif(normal)
    write_html(normal)
    slow = [scene.frame(CROP, **state(i / (FPS * SLOW))) for i in range(round(LOOP * FPS * SLOW))]
    write_mp4([tagged(f, "1x") for f in normal] * 3 + [tagged(f, f"느리게 x{1 / SLOW:g}") for f in slow])


def phase(t):
    a, h = t - ATTACK_AT, t - HIT_AT
    if 0 <= a < LUNGE_OUT + LUNGE_BACK + POSE_EXTRA:
        return "공격 자세"
    if 0 <= h < RECOIL_T + POSE_EXTRA:
        return "피격 자세"
    return "대기"


def tagged(frame, speed):
    """A frame of the MP4 with its speed written in the corner."""
    out = frame.copy()
    d = ImageDraw.Draw(out)
    f = ImageFont.truetype(str(FONT), 26)
    w = d.textlength(speed, font=f)
    d.rectangle([12, 10, 12 + w + 20, 48], fill=(0, 0, 0))
    d.text((22, 13), speed, fill=(235, 235, 230), font=f)
    return out


def write_gif(frames):
    # One palette for every frame, from the three moments together, so that the colors do not flicker.
    moments = [frames[0], frames[round((ATTACK_AT + LUNGE_OUT) * FPS)], frames[round((HIT_AT + 0.05) * FPS)]]
    montage = Image.new("RGB", (frames[0].width, frames[0].height * 3))
    for i, m in enumerate(moments):
        montage.paste(m, (0, i * m.height))
    palette = montage.quantize(colors=255, method=Image.Quantize.MEDIANCUT)
    gif = [f.quantize(palette=palette, dither=Image.Dither.NONE) for f in frames]
    # A GIF counts in hundredths of a second: 30, 30, 40 ms keeps 30 fps on average.
    durations = [(30, 30, 40)[i % 3] for i in range(len(gif))]
    gif[0].save(HERE / "mock-motion.gif", save_all=True, append_images=gif[1:], duration=durations, loop=0)
    print(f"목업: mock-motion.gif ({len(gif)}장, {FPS}fps, {LOOP}초 반복)")


def write_html(frames):
    """A page that plays the loop at 1x, 0.5x or 0.25x, or a frame at a time. The frames are in it (JPEG), so it opens
    anywhere a page does, without a server."""
    unique, timeline = [], []
    for f in frames:
        data = f.tobytes()
        if not unique or unique[-1][0] != data:
            buffer = BytesIO()
            f.save(buffer, format="JPEG", quality=88)
            unique.append((data, base64.b64encode(buffer.getvalue()).decode("ascii")))
        timeline.append(len(unique) - 1)
    phases = [phase(i / FPS) for i in range(len(frames))]
    page = HTML.replace("__FRAMES__", ",".join(f'"{b}"' for _, b in unique)).replace("__TIMELINE__", ",".join(map(str, timeline)))
    page = page.replace("__PHASES__", ",".join(f'"{p}"' for p in phases)).replace("__FPS__", str(FPS))
    page = page.replace("__W__", str(frames[0].width)).replace("__H__", str(frames[0].height)).replace("__LAST__", str(len(frames) - 1))
    (HERE / "mock-motion.html").write_text(page, encoding="utf-8")
    print(f"목업: mock-motion.html ({len(unique)}장의 서로 다른 그림, {len(frames)}틱, {FPS}fps)")


def write_mp4(frames, name="mock-motion.mp4", what=None):
    if FRAMES_DIR.exists():
        shutil.rmtree(FRAMES_DIR)
    FRAMES_DIR.mkdir(parents=True)
    for i, f in enumerate(frames):
        f.save(FRAMES_DIR / f"{i:05d}.png")
    out = HERE / name
    subprocess.run(["swift", str(HERE / "encode_mp4.swift"), str(FRAMES_DIR), str(FPS), str(out)], check=True)
    shutil.rmtree(FRAMES_DIR)
    print(f"목업: {name} ({len(frames)}장, {FPS}fps{what or f': 보통 속도 3회 + {SLOW}배 느리게 1회'})")


HTML = """<!doctype html>
<html lang="ko">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>발키리 모션 목업</title>
<style>
  :root { --bg: #141212; --ink: #ebebe6; --dim: #a0a2aa; --gold: #d9a441; --button: #2a2626; --line: #4a4444; }
  body { margin: 0; background: var(--bg); color: var(--ink); font-family: -apple-system, "Apple SD Gothic Neo", sans-serif; }
  main { max-width: 1140px; margin: 0 auto; padding: 16px; }
  h1 { font-size: 20px; margin: 4px 0; }
  p { color: var(--dim); margin: 0 0 12px; font-size: 14px; line-height: 1.5; }
  .stage { position: relative; }
  canvas { width: 100%; height: auto; display: block; border-radius: 6px; background: #000; }
  .tag { position: absolute; left: 10px; top: 8px; padding: 3px 10px; border-radius: 4px; background: rgba(0, 0, 0, .6); font-size: 15px; }
  .bar { display: flex; flex-wrap: wrap; gap: 8px; align-items: center; margin-top: 10px; }
  button { background: var(--button); color: var(--ink); border: 1px solid var(--line); border-radius: 6px; padding: 6px 12px; font-size: 14px; cursor: pointer; }
  button.on { border-color: var(--gold); color: var(--gold); }
  input[type=range] { flex: 1; min-width: 160px; }
  .time { font-variant-numeric: tabular-nums; color: var(--dim); font-size: 14px; min-width: 64px; }
</style>
</head>
<body>
<main>
  <h1>발키리 공격·피격 모션 목업 (Round 19)</h1>
  <p>지금 전투 화면의 연출값 그대로: 대기 → 대도끼 발동(공격 자세 + 돌진 26px) → 대기 → 약탈자의 일격(피격 자세 + 밀림 12px + 절반 세기의 붉은 번쩍임) → 대기, 2.2초 반복.
     느리게 보거나 한 프레임씩 넘겨 볼 수 있다.</p>
  <div class="stage"><canvas id="view" width="__W__" height="__H__"></canvas><span class="tag" id="tag">대기</span></div>
  <div class="bar">
    <button id="play">정지</button>
    <button data-speed="1" class="on">1x</button>
    <button data-speed="0.5">0.5x</button>
    <button data-speed="0.25">0.25x</button>
    <button id="prev">◀ 한 프레임</button>
    <button id="next">한 프레임 ▶</button>
    <input type="range" id="seek" min="0" max="__LAST__" value="0">
    <span class="time" id="time">0.00초</span>
  </div>
</main>
<script>
  const FRAMES = [__FRAMES__];
  const TIMELINE = [__TIMELINE__];
  const PHASES = [__PHASES__];
  const FPS = __FPS__;
  const view = document.getElementById("view").getContext("2d");
  const images = FRAMES.map(b => { const i = new Image(); i.src = "data:image/jpeg;base64," + b; return i; });
  let tick = 0, playing = true, speed = 1, clock = 0, last = null;
  function show(t) {
    tick = (t + TIMELINE.length) % TIMELINE.length;
    const image = images[TIMELINE[tick]];
    if (image.complete) view.drawImage(image, 0, 0); else image.onload = () => view.drawImage(image, 0, 0);
    document.getElementById("tag").textContent = PHASES[tick];
    document.getElementById("seek").value = tick;
    document.getElementById("time").textContent = (tick / FPS).toFixed(2) + "초";
  }
  function loop(now) {
    if (last !== null && playing) {
      clock += (now - last) / 1000 * speed;
      const next = Math.floor(clock * FPS) % TIMELINE.length;
      if (next !== tick) show(next);
    }
    last = now;
    requestAnimationFrame(loop);
  }
  function pause() { playing = false; document.getElementById("play").textContent = "재생"; }
  document.getElementById("play").onclick = () => {
    playing = !playing;
    document.getElementById("play").textContent = playing ? "정지" : "재생";
    clock = tick / FPS;
  };
  document.querySelectorAll("[data-speed]").forEach(b => b.onclick = () => {
    speed = Number(b.dataset.speed);
    clock = tick / FPS;
    document.querySelectorAll("[data-speed]").forEach(o => o.classList.toggle("on", o === b));
  });
  document.getElementById("prev").onclick = () => { pause(); show(tick - 1); };
  document.getElementById("next").onclick = () => { pause(); show(tick + 1); };
  document.getElementById("seek").oninput = e => { pause(); show(Number(e.target.value)); };
  Promise.all(images.map(i => i.decode ? i.decode().catch(() => {}) : null)).then(() => show(0));
  requestAnimationFrame(loop);
</script>
</body>
</html>
"""


if __name__ == "__main__":
    main()
