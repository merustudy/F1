"""How a support item's activation could move its owner (round 24), no API call.
  .venv/bin/python ArtPipeline/Archive/24-support-motion/mock_support.py     # mock-support.mp4, mock-support-steps.png

The user (2026-10-04): equipment held in the hand (sword, bow, staff, the healing staff too) shows the attack pose when it
fires; a support item (a charm, a pouch, a flask) gets "the dynamic of the attack motion we had before the attack images"
instead. Round 23's battle mockup (the floor 4 battle as the game lights it) with the knight in party row 1, four
moments of 1.2 s, the action at 0.3 s:
- 무기 (참고): the weapon fires: the attack pose and the lunge (26 out in 0.1 s, back in 0.2 s), as approved in round 23.
- 지원 A 돌진: the same lunge with the battle-ready figure (the attack motion before the poses).
- 지원 B 솟구침: the figure rises 14 and comes down (0.1 s up, 0.2 s down): the motions of the battle then go three
  ways, attack toward the enemy, hit away from it, an item used upward.
- 지원 C 맥동과 빛: the figure swells 6% from the feet (0.1 s, back in 0.2 s) with a warm candle light behind it that
  fades over 0.3 s (a light behind the figure, as the game's glow piece is laid behind the candle's flame: a tint on the
  figure itself can only darken it, Image.color multiplies).
The MP4 plays the four twice at the game's speed, then once at half speed.
"""
import shutil
import subprocess
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
sys.path.insert(0, str(ROOT / "ArtPipeline/Archive/23-motion-six"))
import mock_poses as P  # noqa: E402
M = P.M

FRAMES_DIR = ROOT / "ArtPipeline/output/motion_frames_support"
KEY = "knight"
SEGMENT, ACTION = 1.2, 0.3
HOP = 14.0
SWELL = 0.06
GLOW = (255, 214, 140)            # the candle gold of the cooldown light (UiPalette.ChargeLight)
GLOW_ALPHA = 0.6
GLOW_SIZE = 300
GLOW_T = 0.3
MOMENTS = [("무기 (참고) — 공격 자세 + 돌진", "weapon"), ("지원 A — 돌진 (자세 없는 이전의 공격 모션)", "lunge"),
           ("지원 B — 솟구침", "hop"), ("지원 C — 맥동과 빛", "pulse")]
SLOW = 2


class Support(P.Six):
    """The scene with row 1's figure moved up and swollen as well as sideways."""

    def frame(self, box=(0, 0, M.W, M.H), pose="idle", dx=0.0, dy=0.0, swell=1.0, glow=0.0):
        self.row1 = (dy, swell, glow)
        return super().frame(box, pose=pose, astrid_dx=dx, plates_follow=False)

    def glow_sprite(self):
        if not hasattr(self, "_glow"):
            g = Image.new("RGBA", (GLOW_SIZE, GLOW_SIZE), GLOW + (0,))
            r = GLOW_SIZE / 2
            # PIL's radial gradient is 181 at the middle of each side (255 at the corners): fade to nothing there.
            g.putalpha(Image.radial_gradient("L").resize((GLOW_SIZE, GLOW_SIZE)).point(lambda v: round(255 * max(0.0, 1.0 - v / 181) ** 1.6)))
            self._glow = g
        return self._glow

    def unit(self, frame, box, vig, centre, sprite, plate, tint=(1.0, 1.0, 1.0), plate_centre=None):
        if plate != "astrid":
            return super().unit(frame, box, vig, centre, sprite, plate, tint, plate_centre)
        dy, swell, glow = self.row1
        if glow > 0:
            g = self.glow_sprite()
            frame = M.over(frame, g, centre - g.width / 2, M.FLOOR - M.PLACE_H * 0.55 - g.height / 2, vig, box, alpha=GLOW_ALPHA * glow)
        if swell != 1.0:
            sprite = sprite.resize((round(sprite.width * swell), round(sprite.height * swell)), Image.LANCZOS)
        frame = M.over(frame, self.shadow, centre - M.SHADOW_W / 2, M.FLOOR - M.SHADOW_UP - M.SHADOW_H / 2, vig, box, alpha=M.SHADOW_ALPHA, black=True)
        top = M.FLOOR - M.PLACE_H * swell - dy
        return M.over(frame, sprite, centre - sprite.width / 2, top, vig, box, tint=tint)


def out_back(t, total=0.3, up=0.1):
    """0 -> 1 in `up`, back to 0 by `total` (the lunge's shape)."""
    if t < 0 or t >= total:
        return 0.0
    return t / up if t < up else 1.0 - (t - up) / (total - up)


def state(kind, t):
    a = t - ACTION
    s = {"pose": "idle", "dx": 0.0, "dy": 0.0, "swell": 1.0, "glow": 0.0}
    if kind == "weapon":
        if 0 <= a < M.LUNGE_OUT + M.LUNGE_BACK + M.POSE_EXTRA:
            s["pose"] = "attack"
        s["dx"] = M.lunge(a)
    elif kind == "lunge":
        s["dx"] = M.lunge(a)
    elif kind == "hop":
        s["dy"] = HOP * out_back(a)
    elif kind == "pulse":
        s["swell"] = 1.0 + SWELL * out_back(a)
        s["glow"] = max(0.0, 1.0 - a / GLOW_T) if a >= 0 else 0.0
    return s


def tag(frame, text, speed):
    out = frame.copy()
    d = ImageDraw.Draw(out)
    f = ImageFont.truetype(str(M.FONT), 26)
    label = f"{text}   {speed}"
    d.rectangle([12, 10, 12 + d.textlength(label, font=f) + 20, 48], fill=(0, 0, 0))
    d.text((22, 13), label, fill=(235, 235, 230), font=f)
    return out


def main():
    M.POSES = P.POSES / "round19"
    scene = Support()
    scene.use(KEY)
    crop = (440, M.STAGE_TOP, 1200, M.STAGE_BOTTOM)
    normal, slow, steps = [], [], []
    for title, kind in MOMENTS:
        normal += [tag(scene.frame(crop, **state(kind, i / M.FPS)), title, "1x") for i in range(round(SEGMENT * M.FPS))]
        slow += [tag(scene.frame(crop, **state(kind, i / (M.FPS * SLOW))), title, f"x{1 / SLOW:g}") for i in range(round(SEGMENT * M.FPS * SLOW))]
        steps.append((title, scene.frame(crop, **state(kind, ACTION + 0.1))))
        print("목업:", title)

    w, h = steps[0][1].size
    f = ImageFont.truetype(str(M.FONT), 22)
    sheet = Image.new("RGB", (2 * w + 36, 2 * (h + 40) + 24), (20, 18, 18))
    d = ImageDraw.Draw(sheet)
    for i, (title, image) in enumerate(steps):
        x, y = 12 + (i % 2) * (w + 12), 12 + (i // 2) * (h + 40)
        d.text((x + 4, y), title + " — 0.1초 (가장 멀리)", fill=(235, 235, 230), font=f)
        sheet.paste(image, (x, y + 30))
    sheet.save(HERE / "mock-support-steps.png")
    print("목업: mock-support-steps.png", sheet.size)

    frames = normal * 2 + slow
    if FRAMES_DIR.exists():
        shutil.rmtree(FRAMES_DIR)
    FRAMES_DIR.mkdir(parents=True)
    for i, frame in enumerate(frames):
        frame.save(FRAMES_DIR / f"{i:05d}.png")
    subprocess.run(["swift", str(ROOT / "ArtPipeline/Archive/19-motion-test/encode_mp4.swift"), str(FRAMES_DIR), str(M.FPS), str(HERE / "mock-support.mp4")], check=True)
    shutil.rmtree(FRAMES_DIR)
    print(f"목업: mock-support.mp4 ({len(frames)}장, {M.FPS}fps: 네 가지를 보통 속도로 2회, 이어서 {SLOW}배 느리게 1회)")


if __name__ == "__main__":
    main()
