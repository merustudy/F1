"""Round 30: the moment an enemy falls to a unit's blow, as the game shows it now and as proposals A and B would, no API call.
  ArtPipeline/Archive/30-kill-moment/run_stage_shots.sh <scratch>               (first: the game's own empty stage, game/)
  .venv/bin/python ArtPipeline/Archive/30-kill-moment/mock_kill_moment.py       # mock-kill.mp4, mock-steps.png (now, A, B)
  .venv/bin/python ArtPipeline/Archive/30-kill-moment/mock_kill_moment.py zoom  # mock-kill-zoom.mp4, mock-steps-zoom.png

The stage is the game's render of the floor-4 battle at its start with every unit hidden (game/ko_30_stage_empty.png: the
background, the candle's light, the vignette, all at their places since round 29). On it the party of round 23 stands on
the left (Kai, Ella, Cedric, Astrid in rows 4 to 1) and two goblins on the right (the raider in row 1 at 15 of 80 HP, the
shaman in row 2), with the marks of round 29 under their feet. Astrid's greataxe fires at 0.4 s and kills the raider.
Everything moves by the game's numbers (BattleUnitView, FigureView, BattleFxLayer, FloatingTextView, BattlePresenter):
the lunge with the attack pose, the recoil with the hit pose and the half red flash, the numbers that rise, the shake, the
ghost that fades and sinks, the walk of those behind; the figures drawn like the game draws them (mip level, the vignette).
- Now: the raider leaves the stage at once (its ghost in the hit pose fades and sinks, 0.55 s), "쓰러짐" and the shake at the
  blow, the shaman walks into row 1 at once.
- A, the kill moment (recommended): from the blow, 0.5 s (real time, x1) in which the battle and every motion run at 25%; the
  rest of the stage dims (45%) while Astrid (her attack pose) and the raider (its hit pose, held: it is still there) stand
  out over it; then normal speed, and the raider goes as now ("쓰러짐", the shake, its ghost); the shaman walks after it.
- B: A, and the stage draws in 1.08 times towards the two (0.12 s in, back in 0.2 s at the end).
The MP4 plays each twice at the game's speed, then the three once more at half speed.

The user chose B ("B로 가자") and asked for the zoom at 1.12 and 1.16 as well, and for no shake during the slow motion
("슬로우 모션시에는 화면 떨림은 빼는 게 좋을 것 같아"): `zoom` makes B at 1.08, 1.12 and 1.16 with neither of the kill's shakes
(the heavy blow's 6 at the blow, the fall's 10 when the raider goes). Every other shake of the game is as it is. And the
dimming at 60% instead of 45% ("어둠 수치도 60%로 증가").
"""
import math
import random
import shutil
import subprocess
import sys
from pathlib import Path
from PIL import Image, ImageChops, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
sys.path.insert(0, str(ROOT / "ArtPipeline/Archive/19-motion-test"))
sys.path.insert(0, str(ROOT / "ArtPipeline/Archive/29-plates"))
import mock_motion as M  # noqa: E402  (round 19: the colour helpers)
import mock_plates2 as P  # noqa: E402  (round 29: the marks of proposal 2)

ART = ROOT / "Assets/@Art"
FONT = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"
EMPTY = HERE / "game/ko_30_stage_empty.png"
FRAMES_DIR = ROOT / "ArtPipeline/output/motion_frames_round30"

W, H = 1920, 1080
STAGE = (0, 84, 1920, 620)                      # UiPrefabSetup.Battle: StageFxTop to BoardPanelTop
TOP = STAGE[1]
FLOOR = 254 + 300                               # BattleFieldTop + BattleFloorY (round 29)
PLACE_W, PLACE_H = 225, 300
POSE_W, POSE_H, POSE_FLOOR = 675, round(300 * 1008 / 896), 112 / 1008
SHADOW_W, SHADOW_H, SHADOW_ALPHA, SHADOW_UP = 150, 26, 0.35, 6
SCREEN_VIGNETTE = 0.35
FPS = 30

# BattleUnitView, FigureView, BattleFxLayer, FloatingTextView, BattlePresenter
LUNGE_OUT, LUNGE_BACK, LUNGE = 0.1, 0.2, 26.0
RECOIL_T, RECOIL = 0.26, 12.0
POSE_EXTRA = 0.05
WALK_T = 0.35
FLASH_T = 0.28
HALF_HIT_TINT = (1.0, 0.775, 0.75)              # HitTint (1, 0.55, 0.5) at half strength: the unit shows a hit pose
BREATH_PERIOD, BREATH = 1.8, 0.02
GHOST_LIFE, GHOST_SINK = 0.55, 26.0
SHAKE_FALL = 36.0
RISE, POP_T, POP, FADE_FROM = 84.0, 0.14, 1.35, 0.55
TEXT_SIZE, STACK_STEP = 34, 36
DANGER, INK = (0xC0, 0x39, 0x2B), (0x18, 0x09, 0x07)

# The proposals.
HIT = 0.4                                       # when the greataxe fires (real time, x1)
SLOW, SLOW_FOR = 0.25, 0.5                      # A and B: the battle and every motion at 25% for 0.5 s of real time
DIM_IN, DIM_OUT = 0.08, 0.12
ZOOM_IN, ZOOM_OUT = 0.12, 0.2

# What each mockup shows: the slow half second and how dark the rest of the stage goes, the zoom (None for none), the
# kill's shakes.
VARIANTS = {
    "now": {"slow": False, "dim": 0.0, "zoom": None, "shake": True, "length": 2.0,
            "title": "지금 — 적은 맞는 순간 잔상이 되고, 뒤의 적이 바로 걸어 들어온다"},
    "A": {"slow": True, "dim": 0.45, "zoom": None, "shake": True, "length": 2.4, "title": "A 결정타 슬로우 — 0.5초 동안 25% 속도, 둘 말고는 어둡게"},
    "B": {"slow": True, "dim": 0.45, "zoom": 1.08, "shake": True, "length": 2.4, "title": "B — A + 두 유닛 쪽으로 1.08배 줌"},
    "B108": {"slow": True, "dim": 0.6, "zoom": 1.08, "shake": False, "length": 2.4, "title": "B 1.08배 줌 — 떨림 없음, 어둠 60%"},
    "B112": {"slow": True, "dim": 0.6, "zoom": 1.12, "shake": False, "length": 2.4, "title": "B 1.12배 줌 — 떨림 없음, 어둠 60%"},
    "B116": {"slow": True, "dim": 0.6, "zoom": 1.16, "shake": False, "length": 2.4, "title": "B 1.16배 줌 — 떨림 없음, 어둠 60%"},
}
SETS = {"": (("now", "A", "B"), "mock-kill.mp4", "mock-steps.png"),
        "zoom": (("B108", "B112", "B116"), "mock-kill-zoom.mp4", "mock-steps-zoom.png")}

PARTY = [("kai", "spellblade", 4, 210, 100, 100), ("ella", "bishop", 3, 400, 90, 90),
         ("cedric", "paladin", 2, 590, 130, 130), ("astrid", "valkyrie", 1, 780, 96, 120)]
ENEMIES = [("raider", "goblin_raider", 1, 1140, 15, 80), ("shaman", "goblin_shaman", 2, 1330, 60, 60)]
PHASE = {"kai": 0.0, "ella": 1.3, "cedric": 2.6, "astrid": 3.9, "raider": 5.2, "shaman": 0.7}
DAMAGE = 27                                     # the greataxe's blow: heavy (at least 20% of the raider's 80)


def drawn(path, size):
    """The texture as the game draws it at this size: the mip level nearest the ratio, box-filtered in linear light with
    alpha premultiplied, then stretched bilinearly (round 27's way)."""
    image = Image.open(path).convert("RGBA")
    level = max(0, round(math.log2(image.height / size[1])))
    a = M.fmap(lambda m: m["a"] / 255.0, a=image.getchannel("A").convert("F"))
    small = (image.width >> level, image.height >> level)
    pre = [M.fmap(lambda m: m["c"] * m["a"], c=c, a=a).resize(small, Image.BOX).resize(size, Image.BILINEAR) for c in M.to_linear(image)]
    alpha = a.resize(small, Image.BOX).resize(size, Image.BILINEAR)
    colour = M.to_srgb([M.fmap(lambda m: m["c"] / m["max"](m["a"], 1e-6), c=c, a=alpha) for c in pre])
    out = colour.convert("RGBA")
    out.putalpha(M.fmap(lambda m: m["min"](m["max"](m["a"], 0.0), 1.0) * 255.0 + 0.5, a=alpha).convert("L"))
    return out


def marks_sprite(unit):
    """A unit's marks (round 29) drawn by round 29's mockup at twice the size and brought down: (image, left, top) on screen."""
    local_floor, half = 10, 130
    layer = Image.new("RGBA", (2 * half * P.Z, 90 * P.Z), (0, 0, 0, 0))
    saved = P.FLOOR
    P.FLOOR = local_floor
    P.draw_unit(layer, 2, half * P.Z, unit, "party" if unit.get("side") == "party" else "enemy")
    P.FLOOR = saved
    return layer.resize((2 * half, 90), Image.LANCZOS), -half, FLOOR - local_floor


class Scene:
    def __init__(self):
        self.stage = Image.open(EMPTY).convert("RGBA").crop(STAGE)
        vig = Image.open(ART / "UI/Icon/vignette.png").convert("RGBA").resize((W, H), Image.BILINEAR).getchannel("A").crop(STAGE)
        self.vignette = vig.point(lambda a: round(255 * (1.0 - SCREEN_VIGNETTE * a / 255.0)))
        self.font = {}
        self.units = {}
        for key, job, row, cx, hp, hp_max in PARTY:
            self.units[key] = self.load(key, ART / f"Unit/Job/{job}.png", ART / f"Pose/Job/{job}_attack.png", ART / f"Pose/Job/{job}_hit.png", cx, hp, hp_max, "party")
        for key, enemy, row, cx, hp, hp_max in ENEMIES:
            self.units[key] = self.load(key, ART / f"Unit/Enemy/{enemy}.png", ART / f"Pose/Enemy/{enemy}_attack.png", ART / f"Pose/Enemy/{enemy}_hit.png", cx, hp, hp_max, "enemy")
        self.raider_dead = marks_sprite({"hp": 0, "max": 80, "ghost": 15 / 80, "side": "enemy"})
        shadow = Image.new("L", (SHADOW_W * 4, SHADOW_H * 4), 0)
        ImageDraw.Draw(shadow).ellipse((0, 0, SHADOW_W * 4 - 1, SHADOW_H * 4 - 1), fill=round(255 * SHADOW_ALPHA))
        self.shadow = Image.new("RGBA", (SHADOW_W, SHADOW_H), (0, 0, 0, 0))
        self.shadow.putalpha(shadow.resize((SHADOW_W, SHADOW_H), Image.LANCZOS))
        self.breaths = {}

    def load(self, key, idle, attack, hit, cx, hp, hp_max, side):
        return {"key": key, "cx": cx, "idle": drawn(idle, (PLACE_W, PLACE_H)), "attack": drawn(attack, (POSE_W, POSE_H)),
                "hit": drawn(hit, (POSE_W, POSE_H)), "marks": marks_sprite({"hp": hp, "max": hp_max, "side": side})}

    def text(self, size):
        if size not in self.font:
            self.font[size] = ImageFont.truetype(str(FONT), size)
        return self.font[size]

    def breathed(self, unit, pose, g):
        """The art at its breath: 2% at most, from the feet (FigureView). Quantized so that a loop reuses its images."""
        image = unit[pose]
        k = 1.0 + BREATH * (0.5 + 0.5 * math.sin(g * 2 * math.pi / BREATH_PERIOD + PHASE[unit["key"]]))
        k = round(k / 0.002) * 0.002
        key = (unit["key"], pose, k)
        if key not in self.breaths:
            self.breaths[key] = image.resize((round(image.width * k), round(image.height * k)), Image.BILINEAR)
        return self.breaths[key]

    def figure(self, layer, unit, pose, dx, g, tint=None, alpha=1.0, dy=0.0, shadow=True):
        """The unit's shadow and art, its feet on the floor at its column (plus the motion), on a layer in stage coordinates."""
        cx = unit["cx"] + dx
        if shadow:
            layer.alpha_composite(self.shadow, (round(cx - SHADOW_W / 2), round(FLOOR - SHADOW_UP - SHADOW_H / 2 - TOP)))
        art = self.breathed(unit, pose, g)
        if tint is not None or alpha < 1.0:
            art = art.copy()
            r, gr, b, a = art.split()
            if tint is not None:
                r, gr, b = (ch.point(lambda v, f=f: round(v * f)) for ch, f in zip((r, gr, b), tint))
            if alpha < 1.0:
                a = a.point(lambda v: round(v * alpha))
            art = Image.merge("RGBA", (r, gr, b, a))
        if pose == "idle":
            left, top = cx - art.width / 2, FLOOR - art.height
        else:
            left, top = cx - art.width / 2, FLOOR - art.height * (1 - POSE_FLOOR)
        layer.alpha_composite(art, (round(left), round(top + dy - TOP)))

    def marks(self, layer, sprite, x):
        """A unit's marks under its feet, centred on x (its column, plus a walk or the shake)."""
        image, left, top = sprite
        layer.alpha_composite(image, (round(x + left), round(top - TOP)))

    def vignetted(self, layer):
        """The screen's vignette over a layer of units (the empty stage has it already)."""
        r, g, b, a = layer.split()
        r, g, b = (ImageChops.multiply(ch, self.vignette) for ch in (r, g, b))
        return Image.merge("RGBA", (r, g, b, a))

    def float_text(self, frame, x, y, text, scale, age, life):
        """FloatingTextView: shoots up 84 and slows, pops from 1.35 times in 0.14 s, fades over the last 45% of its life."""
        if age < 0 or age >= life:
            return
        t = age / life
        rise = 1 - (1 - t) * (1 - t)
        pop = POP + (1 - POP) * (age / POP_T) if age < POP_T else 1.0
        alpha = 1.0 if t < FADE_FROM else 1 - (t - FADE_FROM) / (1 - FADE_FROM)
        size = max(8, round(TEXT_SIZE * scale * pop))
        font = self.text(size)
        layer = Image.new("RGBA", frame.size, (0, 0, 0, 0))
        d = ImageDraw.Draw(layer)
        cy = y - RISE * rise - TOP
        d.text((x + 2, cy + 2), text, font=font, fill=INK + (round(255 * alpha),), anchor="mm")
        d.text((x, cy), text, font=font, fill=DANGER + (round(255 * alpha),), anchor="mm")
        frame.alpha_composite(layer)


def clock(variant, t):
    """Real time to the battle's (and the motions') time: the same, except during the slow 0.5 s after the blow (A, B)."""
    if not VARIANTS[variant]["slow"] or t < HIT:
        return t
    if t < HIT + SLOW_FOR:
        return HIT + (t - HIT) * SLOW
    return HIT + SLOW_FOR * SLOW + (t - HIT - SLOW_FOR)


def ease(x):
    x = min(1.0, max(0.0, x))
    return x * x * (3 - 2 * x)


def frame(scene, variant, t):
    v = VARIANTS[variant]
    g = clock(variant, t)
    a = g - HIT                                  # time since the blow, in the battle's time
    release = HIT + SLOW_FOR if v["slow"] else HIT              # when the raider goes (real time)
    gone = t >= release and a >= 0
    since_release = clock(variant, t) - clock(variant, release) if gone else -1.0
    units = scene.units

    # Astrid: the lunge with her attack pose from the blow.
    if 0 <= a < LUNGE_OUT:
        astrid_dx = LUNGE * a / LUNGE_OUT
    elif 0 <= a < LUNGE_OUT + LUNGE_BACK:
        astrid_dx = LUNGE * (1 - (a - LUNGE_OUT) / LUNGE_BACK)
    else:
        astrid_dx = 0.0
    astrid_pose = "attack" if 0 <= a < LUNGE_OUT + LUNGE_BACK + POSE_EXTRA else "idle"

    # The raider: the recoil with its hit pose and the half flash, until it goes.
    raider_dx = RECOIL * (1 - a / RECOIL_T) ** 2 if 0 <= a < RECOIL_T else 0.0
    raider_pose = "hit" if 0 <= a < RECOIL_T + POSE_EXTRA else "idle"
    flash = max(0.0, 1 - a / FLASH_T) if a >= 0 else 0.0
    tint = tuple(1 + (c - 1) * flash for c in HALF_HIT_TINT) if flash > 0 else None

    # The shaman walks into row 1 when the raider goes.
    if gone and since_release < WALK_T:
        shaman_cx_dx = -190 + 190 * (1 - since_release / WALK_T) ** 2
    elif gone:
        shaman_cx_dx = -190.0
    else:
        shaman_cx_dx = 0.0

    # The shake: 6 for the heavy blow, 10 when the raider falls (the larger wins), dying down 36 a second. The zoom
    # mockups have neither: the user would take the shake out of the slow motion.
    shake = 0.0
    if a >= 0 and v["shake"]:
        shake = max(0.0, 6 - SHAKE_FALL * a)
    if gone and v["shake"]:
        shake = max(shake, 10 - SHAKE_FALL * since_release)
    rnd = random.Random(round(g * 1000) * 7 + list(VARIANTS).index(variant) + 1)
    angle, radius = rnd.random() * 2 * math.pi, math.sqrt(rnd.random()) * shake
    sx, sy = radius * math.cos(angle), radius * math.sin(angle)

    held = v["slow"] and a >= 0 and not gone                 # A and B: the raider stays, in its hit pose, until it goes
    dim = 0.0
    if v["slow"] and a >= 0:
        rt = t - HIT
        dim = v["dim"] * min(ease(rt / DIM_IN), 1 - ease((rt - (SLOW_FOR - DIM_OUT * 0.4)) / DIM_OUT))

    def draw_raider(layer):
        if not gone:
            scene.figure(layer, units["raider"], raider_pose, raider_dx + sx, g, tint=tint, dy=sy)
            scene.marks(layer, scene.raider_dead if a >= 0 else units["raider"]["marks"], units["raider"]["cx"] + sx)

    def draw_astrid(layer):
        scene.figure(layer, units["astrid"], astrid_pose, astrid_dx + sx, g, dy=sy)
        scene.marks(layer, units["astrid"]["marks"], units["astrid"]["cx"] + sx)

    # The others, in the game's order (the rearmost row first, the party's before the enemy's).
    others = Image.new("RGBA", scene.stage.size, (0, 0, 0, 0))
    for key in ("kai", "shaman", "ella", "cedric"):
        u = units[key]
        dx = shaman_cx_dx if key == "shaman" else 0.0
        scene.figure(others, u, "idle", dx + sx, g, dy=sy)
        scene.marks(others, u["marks"], u["cx"] + dx + sx)
    highlighted = Image.new("RGBA", scene.stage.size, (0, 0, 0, 0))
    if not v["slow"] or not held:
        draw_raider(others)                      # row 1s: the enemy's over the party's, the attacker over everyone
        target = others if not v["slow"] else highlighted
        draw_astrid(target if astrid_pose == "attack" else others)
    else:
        draw_raider(highlighted)
        draw_astrid(highlighted)

    out = scene.stage.copy()
    out.alpha_composite(scene.vignetted(others))
    if dim > 0:
        out = Image.blend(out, Image.new("RGBA", out.size, (0, 0, 0, 255)), dim)
    out.alpha_composite(scene.vignetted(highlighted))

    # The ghost (the fx layer): the raider's art as it was, fading and sinking, from when it goes.
    if gone and since_release < GHOST_LIFE:
        k = since_release / GHOST_LIFE
        ghost_pose = "hit" if (clock(variant, release) - HIT) < RECOIL_T + POSE_EXTRA else "idle"
        ghost_dx = RECOIL * (1 - (clock(variant, release) - HIT) / RECOIL_T) ** 2 if (clock(variant, release) - HIT) < RECOIL_T else 0.0
        scene.figure(out, units["raider"], ghost_pose, ghost_dx, clock(variant, release), alpha=1 - k, dy=GHOST_SINK * k, shadow=False)

    # The numbers: the blow's at the blow (heavy: large), "쓰러짐" when the raider goes, stacked above it.
    top = FLOOR - PLACE_H
    if a >= 0:
        scene.float_text(out, units["raider"]["cx"], top, f"-{DAMAGE}", 1.4, a, 1.4)
    if gone:
        scene.float_text(out, units["raider"]["cx"], top - STACK_STEP, "쓰러짐", 1.4, since_release, 1.4)

    if v["zoom"] and a >= 0:
        rt = t - HIT
        z = 1 + (v["zoom"] - 1) * min(ease(rt / ZOOM_IN), 1 - ease((rt - (SLOW_FOR - 0.05)) / ZOOM_OUT))
        if z > 1.0005:
            px, py = (units["astrid"]["cx"] + units["raider"]["cx"]) / 2, FLOOR - 150 - TOP
            w, h = out.size
            big = out.resize((round(w * z), round(h * z)), Image.BICUBIC)
            out = big.crop((round(px * z - px), round(py * z - py), round(px * z - px) + w, round(py * z - py) + h))
    return out.convert("RGB")


def tag(image, variant, speed):
    out = image.copy()
    d = ImageDraw.Draw(out)
    f = ImageFont.truetype(str(FONT), 30)
    d.rectangle((0, 0, out.width, 48), fill=(16, 14, 14))
    d.text((18, 8), VARIANTS[variant]["title"], fill=(235, 235, 230) if variant == "now" else (217, 164, 65), font=f)
    d.text((out.width - 18, 8), speed, fill=(235, 235, 230), font=f, anchor="ra")
    return out


def main():
    names, video, steps = SETS[sys.argv[1] if len(sys.argv) > 1 else ""]
    scene = Scene()
    frames = []
    for variant in names:
        n = round(VARIANTS[variant]["length"] * FPS)
        normal = [tag(frame(scene, variant, i / FPS), variant, "x1") for i in range(n)]
        frames += normal * 2
        print("목업:", variant, len(normal))
    for variant in names:
        n = round(VARIANTS[variant]["length"] * FPS * 2)
        frames += [tag(frame(scene, variant, i / (FPS * 2)), variant, "x0.5 (느린 재생)") for i in range(n)]

    # Key moments of each: just after the blow, in the middle of the slow half second, after the raider has gone.
    moments = [("맞은 직후 (+0.05초)", HIT + 0.05), ("0.25초 뒤", HIT + 0.25), ("0.7초 뒤", HIT + 0.7)]
    w, h = STAGE[2] - STAGE[0], STAGE[3] - STAGE[1]
    z = 0.5
    cw, ch = round(w * z), round(h * z)
    f = ImageFont.truetype(str(FONT), 22)
    sheet = Image.new("RGB", (3 * cw + 4 * 12, len(names) * (ch + 40) + 40), (20, 18, 18))
    d = ImageDraw.Draw(sheet)
    for j, (title, _) in enumerate(moments):
        d.text((12 + j * (cw + 12) + 4, 8), title, fill=(235, 235, 230), font=f)
    for i, variant in enumerate(names):
        y = 40 + i * (ch + 40)
        d.text((16, y + 4), VARIANTS[variant]["title"], fill=(217, 164, 65) if variant != "now" else (235, 235, 230), font=f)
        for j, (_, t) in enumerate(moments):
            sheet.paste(frame(scene, variant, t).resize((cw, ch), Image.LANCZOS), (12 + j * (cw + 12), y + 34))
    sheet.save(HERE / steps)
    print("목업:", steps, sheet.size)

    if FRAMES_DIR.exists():
        shutil.rmtree(FRAMES_DIR)
    FRAMES_DIR.mkdir(parents=True)
    for i, image in enumerate(frames):
        image.save(FRAMES_DIR / f"{i:05d}.png")
    subprocess.run(["swift", str(ROOT / "ArtPipeline/Archive/19-motion-test/encode_mp4.swift"), str(FRAMES_DIR), str(FPS), str(HERE / video)], check=True)
    shutil.rmtree(FRAMES_DIR)
    print(f"목업: {video} ({len(frames)}장, {FPS}fps)")


if __name__ == "__main__":
    main()
