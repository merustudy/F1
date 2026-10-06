"""Round 38: the presentation of a breakdown (an affliction) and a virtue, three proposals, no API call.
  .venv/bin/python ArtPipeline/Archive/38-breakdown-fx/mock_breakdown_fx.py          # mock-steps.png, mock-breakdown.mp4

The stage is the game's own render of the staged breakdown battle a moment after the banner has gone
(../36-fatigue-states/game/battle_state_implemented.png: Astrid in row 1, "무모" under her feet). Over it the mockup lays each
proposal's layers by the game's numbers where they exist (the kill moment's slow 25%, its dark 0.87 and its zoom 1.1; the
banner's veil 0.7 and its 0.15/1.2/0.3 s; FigureView's pulse), and draws Astrid's figure again on top where she must stand out
of the dark (Assets/@Art/Unit/Job/valkyrie.png at the figure's place, with her shadow and her marks cut from the render).
  A  띠 보강: the banner as it is, slid in from the left, with the portrait (round 37) on the effect burst (this round) at its left
     and the state's words larger; the unit shivers (an affliction) or swells (a virtue) in the state's colour.
  B  결의 시험: the kill moment's grammar: 0.8 s at 25%, the stage dark but the unit, a zoom of 1.1 on it; behind the unit a
     glow in the state's colour and the effect burst (the ink burst of a breakdown, the light burst of a virtue); the unit
     shivers or swells; the state's word large over its head with the name under it. No banner.
  C  B + 머리 위 알림: B, and in the empty headroom above the figures an announcement in Darkest Dungeon's way: the portrait on
     its burst and "아스트리드 / 무모에 빠졌다", slid in from the left, staying a moment after the dark has gone.
The MP4 plays each clip at x1, then at x0.5.

Second run (the user chose B, 2026-10-06): B alone at a zoom of 1.2 and of 1.3, the figure swapped for its state pose (drawn this
round after the approved figure, as an attack pose is) while the dark is there and brought back as it goes, the shadow left
uncoloured, and the ink burst drawn again to read on the dark stage. Writes mock-steps2.png, mock-breakdown2.mp4, review-poses.png.

Third run (the user's verdict on the second): the zoom 1.2, the poses confirmed, the first ink burst back (preferred) and both
effects at 0.8 of their size; and the collapse at the most fatigue shown the recommended way (the breakdown's frame with its own
words). Writes mock-steps3.png, mock-breakdown3.mp4.
"""
import math
import random
import shutil
import subprocess
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
sys.path.insert(0, str(ROOT / "ArtPipeline" / "tools"))
from gen_image import subject_box  # noqa: E402

FONT = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"
BASE = ROOT / "ArtPipeline/Archive/36-fatigue-states/game/battle_state_implemented.png"
SPRITE = ROOT / "Assets/@Art/Unit/Job/valkyrie.png"
GLOW = ROOT / "Assets/@Art/UI/Icon/glow.png"
OUT = ROOT / "ArtPipeline/output"
BUSTS = {"affliction": OUT / "character/valkyrie_afflicted2.raw.png", "virtue": OUT / "character/valkyrie_virtue.raw.png"}
BURSTS = {"affliction": OUT / "ui_piece/fx38_ink_burst.raw.png", "virtue": OUT / "ui_piece/fx38_light_burst.raw.png"}   # the first ink burst: the user preferred it
# The state poses stood at the figure's size by tools/fit_pose.py (fit_state_poses.py): the pose canvas, 2048x1024, which the game draws
# 675 wide with its floor line 112/1008 up (UiPrefabSetup.Battle, FigureView), as it draws the attack and hit poses.
POSES = {"affliction": OUT / "hit/valkyrie_broken.png", "virtue": OUT / "hit/valkyrie_resolute.png"}
POSE_W, POSE_H, POSE_FLOOR = 675, 300 * 1008 / 896, 112 / 1008
SWAP_UNTIL = 1.05                              # the state pose shows while the dark is there, then the figure comes back (SLOW_FOR + DARK_OUT)
FRAMES_DIR = OUT / "motion_frames_round38"

W = 1920
STAGE = (0, 84, 1920, 620)                      # the stage's box: StageFxTop to BoardPanelTop
TOP = STAGE[1]
SH = STAGE[3] - STAGE[1]
FLOOR = 254 + 300                               # BattleFieldTop + BattleFloorY
CX = 780                                        # Astrid's column centre (row 1 of the party, FieldLayout)
PLACE_W, PLACE_H = 225, 300
MARKS = (690, 560, 870, 612)                    # her marks, in screen coordinates
SHADOW_W, SHADOW_H, SHADOW_ALPHA, SHADOW_UP = 150, 26, 0.35, 6
FPS = 30

INK, TEXT, DIM = (24, 9, 7), (235, 235, 230), (154, 160, 172)
COLOR = {"affliction": (200, 75, 110), "virtue": (247, 200, 74), "collapse": (200, 75, 110)}       # UiPalette.FatigueDanger, UiPalette.Virtue
WORD = {"affliction": "무모", "virtue": "집중", "collapse": "피로로 쓰러졌다"}
SENTENCE = {"affliction": "무모에 빠졌다", "virtue": "집중의 각성", "collapse": "피로로 쓰러졌다"}
# The collapse at the most fatigue (the recommended way, 2026-10-06): the breakdown's frame, pose and burst, with its own words.
STATE_BASE = {"affliction": "affliction", "virtue": "virtue", "collapse": "affliction"}
NAME = "아스트리드"

# The game's numbers.
KILL_SLOW, KILL_DARK, KILL_ZOOM = 0.25, 0.87, 1.1
DARK_IN, DARK_OUT, ZOOM_IN, ZOOM_OUT = 0.08, 0.25, 0.12, 0.25
BAND_TOP, BAND_H, BAND_LINE, BAND_ALPHA, VEIL = 310, 84, 2, 0.85, 0.7
BAND_IN, BAND_HOLD, BAND_OUT = 0.15, 1.2, 0.3
PULSE_T, PULSE = 0.3, 0.06

# The proposals' own numbers.
SLOW_FOR = 0.8                                  # B, C: the slow stretch (real seconds at x1)
BURST_IN = 0.18
GLOW_SIZE = 352                                 # 440 x 0.8 (third run: the effects at 0.8)
BURST_SIZE = {"affliction": 416, "virtue": 352}  # 520 and 440 x 0.8
GLOW_ALPHA = 0.55
SHIVER_T, SHIVER = 0.5, 4.0
FLASH_T = 0.4
TINT = {"affliction": (0.62, 0.52, 0.72), "virtue": (1.0, 0.92, 0.62), "collapse": (0.62, 0.52, 0.72)}
WORD_SIZE, NAME_SIZE = 84, 30
HEADLINE_IN, HEADLINE_HOLD, HEADLINE_OUT = 0.25, 1.1, 0.3
LENGTH = 2.2

VARIANTS = {
    "F": "B 결의 시험 · 줌 1.2배 · 효과 0.8배 · 먹 튐 1차 — 0.8초 25% 속도, 유닛만 밝게, 상태의 자세로 바뀌었다가 어둠이 걷히며 원복, 뒤의 빛과 효과, 머리 위의 큰 글",
}
ZOOM = {"F": 1.2}
STATE_TITLE = {"affliction": "붕괴(고통: 무모)", "virtue": "각성(집중)", "collapse": "쓰러짐(피로 200)"}


def font(size):
    return ImageFont.truetype(str(FONT), size)


def ease(x):
    x = min(1.0, max(0.0, x))
    return x * x * (3 - 2 * x)


def ease_out(x):
    x = min(1.0, max(0.0, x))
    return 1 - (1 - x) * (1 - x)


def tinted(image, factors=None, alpha=1.0):
    if factors is None and alpha >= 1.0:
        return image
    r, g, b, a = image.split()
    if factors is not None:
        r, g, b = (ch.point(lambda v, f=f: min(255, round(v * f))) for ch, f in zip((r, g, b), factors))
    if alpha < 1.0:
        a = a.point(lambda v: round(v * alpha))
    return Image.merge("RGBA", (r, g, b, a))


def coloured_alpha(mask, color, alpha):
    """A flat colour through an alpha mask."""
    out = Image.new("RGBA", mask.size, color + (0,))
    out.putalpha(mask.point(lambda v: min(255, round(v * alpha))))
    return out


def head(bust):
    """A square around the head of a bust: as in round 37's review sheet."""
    side = int(bust.height * 0.62)
    left = max(0, bust.width // 2 - side // 2 + int(bust.width * 0.06))
    return bust.crop((left, 0, min(bust.width, left + side), side))


def fitted(image, size):
    scale = min(size / image.width, size / image.height)
    return image.resize((max(1, round(image.width * scale)), max(1, round(image.height * scale))), Image.LANCZOS)


def outlined(image, width=3):
    from PIL import ImageFilter
    alpha = image.getchannel("A")
    ring = alpha.filter(ImageFilter.MaxFilter(2 * width + 1))
    back = Image.new("RGBA", image.size, INK + (0,))
    back.putalpha(ring)
    back.alpha_composite(image)
    return back


class Scene:
    def __init__(self):
        screen = Image.open(BASE).convert("RGBA")
        self.stage = screen.crop(STAGE)
        self.marks = screen.crop(MARKS)
        sprite = Image.open(SPRITE).convert("RGBA")
        self.sprite = sprite.resize((PLACE_W, PLACE_H), Image.LANCZOS)
        # The state poses as the game draws a pose: the canvas centred on the column, its floor line on the floor.
        self.pose = {}
        for state, path in POSES.items():
            pose = Image.open(path).convert("RGBA").resize((POSE_W, round(POSE_H)), Image.LANCZOS)
            self.pose[state] = (pose, CX - POSE_W / 2, FLOOR - POSE_H * (1 - POSE_FLOOR))
        shadow = Image.new("L", (SHADOW_W * 4, SHADOW_H * 4), 0)
        ImageDraw.Draw(shadow).ellipse((0, 0, SHADOW_W * 4 - 1, SHADOW_H * 4 - 1), fill=round(255 * SHADOW_ALPHA))
        self.shadow = Image.new("RGBA", (SHADOW_W, SHADOW_H), (0, 0, 0, 0))
        self.shadow.putalpha(shadow.resize((SHADOW_W, SHADOW_H), Image.LANCZOS))
        glow = Image.open(GLOW).convert("RGBA").getchannel("A")
        self.glow = {state: coloured_alpha(glow.resize((GLOW_SIZE, GLOW_SIZE), Image.LANCZOS), COLOR[state], GLOW_ALPHA) for state in BURSTS}
        self.burst = {}
        self.bust = {}
        for state in BURSTS:
            burst = Image.open(BURSTS[state]).convert("RGBA")
            self.burst[state] = fitted(burst.crop(subject_box(burst)), BURST_SIZE[state])
            bust = Image.open(BUSTS[state]).convert("RGBA")
            # The whole bust, its own silhouette: a square cut of the head looked pasted on (the first run of this mockup).
            self.bust[state] = bust.crop(subject_box(bust))
        # The collapse borrows the breakdown's glow, burst, bust and pose.
        for table in (self.glow, self.burst, self.bust, self.pose):
            table["collapse"] = table[STATE_BASE["collapse"]]
        self.fonts = {}
        self.band_text = {}

    def f(self, size):
        if size not in self.fonts:
            self.fonts[size] = font(size)
        return self.fonts[size]

    def unit(self, layer, t, state, pulse_at=0.0, tinted_shadow=False):
        """Astrid over whatever is under her: the shadow (in the state's colour while she is in the state, B and C), the figure
        (shivering or swelling, flashing), her marks."""
        dx = 0.0
        if STATE_BASE[state] == "affliction" and 0 <= t < SHIVER_T:
            rnd = random.Random(round(t * 1000))
            dx = (rnd.random() * 2 - 1) * SHIVER * (1 - t / SHIVER_T)
        scale = 1.0
        if state == "virtue" and 0 <= t - pulse_at < PULSE_T:
            k = (t - pulse_at) / PULSE_T
            scale = 1 + PULSE * math.sin(k * math.pi)
        swapped = 0 <= t < SWAP_UNTIL
        if swapped:
            art, left, top = self.pose[state]
        else:
            art, left, top = self.sprite, CX - PLACE_W / 2, FLOOR - PLACE_H
        if scale != 1.0:
            # The swell grows from the feet.
            grown = art.resize((round(art.width * scale), round(art.height * scale)), Image.BILINEAR)
            left -= (grown.width - art.width) / 2
            top -= grown.height - art.height
            art = grown
        flash = max(0.0, 1 - t / FLASH_T) if t >= 0 else 0.0
        if flash > 0:
            art = tinted(art, tuple(1 + (c - 1) * flash for c in TINT[state]))
        shadow = self.shadow
        if tinted_shadow and t >= 0:
            shadow = coloured_alpha(self.shadow.getchannel("A"), COLOR[state], 1.4)
        layer.alpha_composite(shadow, (round(CX - SHADOW_W / 2), round(FLOOR - SHADOW_UP - SHADOW_H / 2 - TOP)))
        layer.alpha_composite(art, (round(left + dx), round(top - TOP)))
        layer.alpha_composite(self.marks, (MARKS[0], MARKS[1] - TOP))

    def effect(self, layer, t, state, alpha=1.0):
        """The glow and the burst behind the unit's chest: scaled in, then the burst turns (a virtue) or trembles (an affliction)."""
        if t < 0 or alpha <= 0:
            return
        k = ease_out(t / BURST_IN)
        cx, cy = CX, FLOOR - 170 - TOP
        glow = self.glow[state]
        g = glow.resize((max(2, round(GLOW_SIZE * (0.5 + 0.5 * k))),) * 2, Image.BILINEAR)
        layer.alpha_composite(tinted(g, alpha=alpha), (round(cx - g.width / 2), round(cy - g.height / 2)))
        burst = self.burst[state]
        size = 0.35 + 0.65 * k
        if state == "virtue":
            burst = burst.rotate(-12 * t, resample=Image.BICUBIC, expand=False)
            jx = jy = 0.0
        else:
            rnd = random.Random(round(t * 1000) + 7)
            j = 3.0 * max(0.0, 1 - t / 0.5)
            jx, jy = (rnd.random() * 2 - 1) * j, (rnd.random() * 2 - 1) * j
        b = burst.resize((max(2, round(burst.width * size)), max(2, round(burst.height * size))), Image.BILINEAR)
        layer.alpha_composite(tinted(b, alpha=alpha), (round(cx + jx - b.width / 2), round(cy + jy - b.height / 2)))

    def big_word(self, layer, t, state, alpha):
        """B: the state's word large over the unit's head, the name under it, rising a little as it comes."""
        if t < 0.1 or alpha <= 0:
            return
        k = ease_out((t - 0.1) / 0.4)
        cx = CX
        y = FLOOR - PLACE_H - 20 - 30 * k - TOP
        a = round(255 * alpha * min(1.0, (t - 0.1) / 0.15))
        d = ImageDraw.Draw(layer, "RGBA")
        size = WORD_SIZE if len(WORD[state]) <= 2 else 56
        d.text((cx, y), WORD[state], font=self.f(size), fill=COLOR[state] + (a,), anchor="ms", stroke_width=4, stroke_fill=INK + (a,))
        d.text((cx, y + NAME_SIZE + 6), NAME, font=self.f(NAME_SIZE), fill=TEXT + (a,), anchor="ms", stroke_width=2, stroke_fill=INK + (a,))

    def headline(self, layer, t, state):
        """C: the announcement in the headroom: the portrait on its burst, the name and the sentence, slid in from the left."""
        start = 0.15
        if t < start:
            return
        age = t - start
        if age < HEADLINE_IN:
            slide = 1 - ease_out(age / HEADLINE_IN)
            alpha = 1.0
        elif age < HEADLINE_IN + HEADLINE_HOLD:
            slide, alpha = 0.0, 1.0
        elif age < HEADLINE_IN + HEADLINE_HOLD + HEADLINE_OUT:
            slide, alpha = 0.0, 1 - (age - HEADLINE_IN - HEADLINE_HOLD) / HEADLINE_OUT
        else:
            return
        plate = Image.new("RGBA", (W, 190), (0, 0, 0, 0))
        burst = fitted(self.burst[state], 300)
        bx, by = 700, 95
        plate.alpha_composite(burst, (bx - burst.width // 2, by - burst.height // 2))
        bust = outlined(fitted(self.bust[state], 200))
        plate.alpha_composite(bust, (bx - bust.width // 2, by - bust.height // 2 + 8))
        d = ImageDraw.Draw(plate, "RGBA")
        d.text((880, 60), NAME, font=self.f(36), fill=TEXT + (255,), anchor="ls", stroke_width=2, stroke_fill=INK + (255,))
        d.text((880, 130), SENTENCE[state], font=self.f(62), fill=COLOR[state] + (255,), anchor="ls", stroke_width=4, stroke_fill=INK + (255,))
        plate = tinted(plate, alpha=alpha)
        layer.alpha_composite(plate, (round(-W * slide * 0.6), 6))

    def band(self, layer, t, state):
        """A: the game's banner, slid in from the left instead of faded, with the portrait on a small burst at its left and larger words."""
        if t < 0:
            return
        if t < BAND_IN:
            slide, alpha = 1 - ease_out(t / BAND_IN), 1.0
        elif t < BAND_IN + BAND_HOLD:
            slide, alpha = 0.0, 1.0
        elif t < BAND_IN + BAND_HOLD + BAND_OUT:
            slide, alpha = 0.0, 1 - (t - BAND_IN - BAND_HOLD) / BAND_OUT
        else:
            return
        veil = Image.new("RGBA", layer.size, (0, 0, 0, round(255 * VEIL * alpha)))
        layer.alpha_composite(veil)
        strip = Image.new("RGBA", (W, 220), (0, 0, 0, 0))
        top = 60
        d = ImageDraw.Draw(strip, "RGBA")
        d.rectangle((0, top, W, top + BAND_H), fill=INK + (round(255 * BAND_ALPHA),))
        d.rectangle((0, top, W, top + BAND_LINE), fill=COLOR[state] + (255,))
        d.rectangle((0, top + BAND_H - BAND_LINE, W, top + BAND_H), fill=COLOR[state] + (255,))
        burst = fitted(self.burst[state], 170)
        bx, by = 660, top + BAND_H // 2
        strip.alpha_composite(burst, (bx - burst.width // 2, by - burst.height // 2))
        bust = outlined(fitted(self.bust[state], 128))
        strip.alpha_composite(bust, (bx - bust.width // 2, by - bust.height // 2 + 4))
        name_font, word_font = self.f(34), self.f(46)
        x = 770
        d.text((x, by + 14), NAME, font=name_font, fill=TEXT + (255,), anchor="ls", stroke_width=2, stroke_fill=INK + (255,))
        x += d.textlength(NAME, font=name_font) + 22
        d.text((x, by + 16), SENTENCE[state], font=word_font, fill=COLOR[state] + (255,), anchor="ls", stroke_width=3, stroke_fill=INK + (255,))
        strip = tinted(strip, alpha=alpha)
        layer.alpha_composite(strip, (round(-W * slide), BAND_TOP - TOP - top))


def frame(scene, variant, state, t):
    """One frame of a clip at real time t (the breakdown at t = 0), in stage coordinates."""
    out = scene.stage.copy()
    # B and C: the kill moment's grammar. The dark comes in at the breakdown and goes back when the slow stretch ends.
    dark = KILL_DARK * min(ease(t / DARK_IN), 1 - ease((t - SLOW_FOR) / DARK_OUT)) if t >= 0 else 0.0
    zoom = 1 + (ZOOM[variant] - 1) * min(ease(t / ZOOM_IN), 1 - ease((t - SLOW_FOR) / ZOOM_OUT)) if t >= 0 else 1.0
    fade = 1.0 if t < SLOW_FOR else max(0.0, 1 - (t - SLOW_FOR) / DARK_OUT)
    if dark > 0:
        out.alpha_composite(Image.new("RGBA", out.size, (0, 0, 0, round(255 * dark))))
    lit = Image.new("RGBA", out.size, (0, 0, 0, 0))
    scene.effect(lit, t, state, alpha=fade)
    scene.unit(lit, t, state)
    out.alpha_composite(lit)
    word_fade = 1.0 if t < 1.5 else max(0.0, 1 - (t - 1.5) / 0.3)
    scene.big_word(out, t, state, word_fade)
    if zoom > 1.0005:
        px, py = CX, FLOOR - 170 - TOP
        w, h = out.size
        big = out.resize((round(w * zoom), round(h * zoom)), Image.BICUBIC)
        left, top = round(px * zoom - px), round(py * zoom - py)
        out = big.crop((left, top, left + w, top + h))
    return out.convert("RGB")


def review(scene):
    """The poses next to the figure, at one scale: the approved figure, the breakdown pose, the virtue pose (the fitted files)."""
    k = 420 / PLACE_H
    sheet = Image.new("RGB", (2100, 560), (58, 46, 36))
    d = ImageDraw.Draw(sheet)
    d.text((24, 16), "Round 38 — 발키리의 붕괴 자세·각성 자세 (tools/fit_pose.py로 확정 원본의 크기에 맞춘 것). 왼쪽: 확정 원본. 모두 게임 크기의 1.4배", font=font(22), fill=TEXT)
    idle = scene.sprite.resize((round(PLACE_W * k), round(PLACE_H * k)), Image.LANCZOS)
    sheet.paste(idle, (120, 520 - idle.height), idle)
    d.text((140, 526), "확정 원본 · 대기", font=font(20), fill=DIM)
    for i, state in enumerate(("affliction", "virtue")):
        art, left, top = scene.pose[state]
        big = art.resize((round(art.width * k), round(art.height * k)), Image.LANCZOS)
        x = 820 + i * 720
        sheet.paste(big, (round(x - big.width / 2), round(520 - (FLOOR - top) * k)), big)
        d.text((x - 70, 526), {"affliction": "붕괴 자세 · broken", "virtue": "각성 자세 · resolute"}[state], font=font(20), fill=DIM)
    d.line((0, 520, sheet.width, 520), fill=(90, 110, 160), width=2)
    sheet.save(HERE / "review-poses.png")
    print("목업: review-poses.png")


def tag(image, title, speed):
    out = Image.new("RGB", (image.width, image.height + 48), (16, 14, 14))
    out.paste(image, (0, 48))
    d = ImageDraw.Draw(out)
    f = font(28)
    d.text((18, 9), title, fill=(217, 164, 65), font=f)
    d.text((out.width - 18, 9), speed, fill=TEXT, font=f, anchor="ra")
    return out


def main():
    scene = Scene()
    clips = [(v, s) for v in VARIANTS for s in ("affliction", "virtue", "collapse")]

    # The steps sheet: three moments of each clip.
    moments = [("붕괴 직후 (+0.1초)", 0.1), ("0.5초 뒤 (느린 구간)", 0.5), ("1.3초 뒤 (어둠이 걷힌 뒤)", 1.3)]
    z = 0.5
    cw, ch = round(W * z), round(SH * z)
    f = font(22)
    sheet = Image.new("RGB", (3 * cw + 4 * 12, len(clips) * (ch + 40) + 40), (20, 18, 18))
    d = ImageDraw.Draw(sheet)
    for j, (title, _) in enumerate(moments):
        d.text((12 + j * (cw + 12) + 4, 8), title, fill=TEXT, font=f)
    for i, (variant, state) in enumerate(clips):
        y = 40 + i * (ch + 40)
        d.text((16, y + 4), f"{VARIANTS[variant]}  ·  {STATE_TITLE[state]}", fill=(217, 164, 65), font=f)
        for j, (_, t) in enumerate(moments):
            sheet.paste(frame(scene, variant, state, t).resize((cw, ch), Image.LANCZOS), (12 + j * (cw + 12), y + 34))
    sheet.save(HERE / "mock-steps3.png")
    print("목업: mock-steps3.png", sheet.size)

    review(scene)

    frames = []
    for variant, state in clips:
        title = f"{VARIANTS[variant]}  ·  {STATE_TITLE[state]}"
        n = round(LENGTH * FPS)
        frames += [tag(frame(scene, variant, state, i / FPS), title, "x1") for i in range(n)]
        frames += [tag(frame(scene, variant, state, i / (FPS * 2)), title, "x0.5 (느린 재생)") for i in range(n * 2)]
        print("목업:", variant, state, n * 3)

    if FRAMES_DIR.exists():
        shutil.rmtree(FRAMES_DIR)
    FRAMES_DIR.mkdir(parents=True)
    for i, image in enumerate(frames):
        image.save(FRAMES_DIR / f"{i:05d}.png")
    video = HERE / "mock-breakdown3.mp4"
    subprocess.run(["swift", str(ROOT / "ArtPipeline/Archive/19-motion-test/encode_mp4.swift"), str(FRAMES_DIR), str(FPS), str(video)], check=True)
    shutil.rmtree(FRAMES_DIR)
    print(f"목업: {video.name} ({len(frames)}장, {FPS}fps)")


if __name__ == "__main__":
    main()
