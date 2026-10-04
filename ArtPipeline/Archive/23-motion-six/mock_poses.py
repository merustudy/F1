"""The six mercenaries' attack and hit poses in the battle (round 23), no API call.
  .venv/bin/python ArtPipeline/Archive/23-motion-six/fit_poses.py      (first: the poses at the figures' size)
  .venv/bin/python ArtPipeline/Archive/23-motion-six/mock_poses.py     # mock-steps.png, mock-poses.mp4

Round 19's mockup (../19-motion-test/mock_motion.py) for each mercenary in turn: the floor 4 battle as the game lights
it, the mercenary standing in party row 1 against the goblin raider. Its weapon fires (the attack pose while the lunge
plays, 0.1 s out and 0.2 s back, and 0.05 s more), then the raider's blow lands (the hit pose while the recoil plays,
0.26 s, and 0.05 s more, with the red flash at half strength). Every plate stays in its column (round 19's rule, in the
game). The party's row 1 plate is left out: the screenshot's is Astrid's and the mockup shows six mercenaries there.
Cedric in row 2 is taken off the screenshot (it has the art of round 18, his small head) and drawn again with the art the
game shows now (round 21's way, ../21-grave/mock_grave.py).
A pose is drawn on fit_poses.py's wide canvas (three figure canvases wide, an eighth deeper: its place is 675x338 with
the figure place in the middle). The MP4 shows the six at the game's speed twice, then once at half speed.
"""
import shutil
import subprocess
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
sys.path.insert(0, str(ROOT / "ArtPipeline/Archive/19-motion-test"))
sys.path.insert(0, str(ROOT / "ArtPipeline/Archive/21-grave"))
import mock_motion as M  # noqa: E402
from mock_grave import CENTRE, find  # noqa: E402
sys.path.insert(0, str(HERE))
from fit_poses import drawn  # noqa: E402

JOBS = [("knight", "로언 · 기사"), ("valkyrie", "아스트리드 · 발키리"), ("bishop", "엘라 · 주교"),
        ("paladin", "세드릭 · 성기사"), ("archmage", "미라 · 대마법사"), ("spellblade", "카이 · 마검사")]
POSES = ROOT / "ArtPipeline/output/character"
FRAMES_DIR = ROOT / "ArtPipeline/output/motion_frames_six"
SLOW = 2


def wide_sprite(path):
    """A pose as the screen would draw it: its wide canvas stretched so that the figure canvas in it fills the place."""
    image = Image.open(path).convert("RGBA")
    return image.resize((round(M.PLACE_W * image.width / 672), round(M.PLACE_H * image.height / M.FIGURE_H)), Image.LANCZOS)


class Six(M.Scene):
    def __init__(self):
        super().__init__()
        # Cedric (row 2) as the screenshot has him is taken off: his art where it was and his shadow, not his plate.
        art = Image.open(M.SHOT_ART / "paladin.png").convert("RGBA")
        score, dx, stretch = find(self.shot, art, CENTRE[2])
        print(f"스크린샷의 세드릭: 2열에서 {dx:+d}, 숨쉬기 {stretch:.2f}, 차이 {score:.1f}")
        h = round(M.PLACE_H * stretch)
        mask = art.resize((M.PLACE_W, h), Image.LANCZOS).getchannel("A").point(lambda v: 255 if v > 8 else 0).filter(ImageFilter.MaxFilter(7))
        erase = Image.new("L", (M.W, M.H), 0)
        erase.paste(mask, (round(CENTRE[2] - M.PLACE_W / 2 + dx), M.FLOOR - h), mask)
        sx = CENTRE[2] + dx
        ImageDraw.Draw(erase).ellipse([sx - M.SHADOW_W / 2 - 6, M.FLOOR - M.SHADOW_UP - M.SHADOW_H / 2 - 5,
                                       sx + M.SHADOW_W / 2 + 6, M.FLOOR - M.SHADOW_UP + M.SHADOW_H / 2 + 5], fill=255)
        # His plate is below the floor line and stays: the erased area ends above it.
        ImageDraw.Draw(erase).rectangle([0, M.PLATE_TOP - 4, M.W, M.H], fill=0)
        e = M.fmap(lambda m: m["e"] / 255.0, e=erase.filter(ImageFilter.GaussianBlur(1.0)).convert("F"))
        self.base = [M.fmap(lambda m: m["s"] * (1.0 - m["e"]) + m["b"] * m["e"], s=self.base[k], b=self.stage[k], e=e) for k in range(3)]
        self.cedric = M.figure_sprite(M.UNIT / "Job/paladin.png")

    def frame(self, box=(0, 0, M.W, M.H), pose="idle", astrid_dx=0.0, astrid_tint=(1.0, 1.0, 1.0), raider_dx=0.0,
              raider_tint=(1.0, 1.0, 1.0), plates_follow=True, idle=None):
        """Round 19's frame with Cedric drawn again in row 2, behind row 1."""
        f = [c.crop(box) for c in self.base]
        vig = self.vig.crop(box)
        fixed = (lambda x: x) if not plates_follow else (lambda x: None)
        f = self.unit(f, box, vig, M.ENEMY_2, self.sprites["shaman"], "shaman", plate_centre=fixed(M.ENEMY_2))
        f = self.unit(f, box, vig, M.ENEMY_1 + raider_dx, self.sprites["raider"], "raider", raider_tint, plate_centre=fixed(M.ENEMY_1))
        f = M.over(f, self.shadow, CENTRE[2] - M.SHADOW_W / 2, M.FLOOR - M.SHADOW_UP - M.SHADOW_H / 2, vig, box, alpha=M.SHADOW_ALPHA, black=True)
        f = M.over(f, self.cedric, CENTRE[2] - self.cedric.width / 2, M.FLOOR - M.PLACE_H, vig, box)
        f = self.unit(f, box, vig, M.ASTRID + astrid_dx, self.sprites[pose], "astrid", astrid_tint, plate_centre=fixed(M.ASTRID))
        return M.to_srgb(f)

    def use(self, key):
        self.sprites["idle"] = M.figure_sprite(M.UNIT / f"Job/{key}.png")
        self.sprites["attack"] = wide_sprite(POSES / f"{drawn(key, 'attack')}_wide.png")
        self.sprites["hit"] = wide_sprite(POSES / f"{drawn(key, 'hit')}_wide.png")

    def unit(self, frame, box, vig, centre, sprite, plate, tint=(1.0, 1.0, 1.0), plate_centre=None):
        if plate != "astrid":
            return super().unit(frame, box, vig, centre, sprite, plate, tint, plate_centre)
        frame = M.over(frame, self.shadow, centre - M.SHADOW_W / 2, M.FLOOR - M.SHADOW_UP - M.SHADOW_H / 2, vig, box, alpha=M.SHADOW_ALPHA, black=True)
        return M.over(frame, sprite, centre - sprite.width / 2, M.FLOOR - M.PLACE_H, vig, box, tint=tint)


def tag(frame, name, phase, speed):
    out = frame.copy()
    d = ImageDraw.Draw(out)
    f = ImageFont.truetype(str(M.FONT), 26)
    text = f"{name} — {phase}   {speed}"
    d.rectangle([12, 10, 12 + d.textlength(text, font=f) + 20, 48], fill=(0, 0, 0))
    d.text((22, 13), text, fill=(235, 235, 230), font=f)
    return out


def main():
    # Round 19's scene loads its own valkyrie poses first; they were set aside under round19/ when these were drawn.
    M.POSES = POSES / "round19"
    scene = Six()
    normal, slow, steps = [], [], []
    for key, name in JOBS:
        scene.use(key)
        normal += [tag(scene.frame(M.CROP, plates_follow=False, **M.state(i / M.FPS)), name, M.phase(i / M.FPS), "1x")
                   for i in range(round(M.LOOP * M.FPS))]
        slow += [tag(scene.frame(M.CROP, plates_follow=False, **M.state(i / (M.FPS * SLOW))), name, M.phase(i / (M.FPS * SLOW)), f"x{1 / SLOW:g}")
                 for i in range(round(M.LOOP * M.FPS * SLOW))]
        steps.append([scene.frame(M.CROP, plates_follow=False, **M.state(t)) for t in (0.0, M.ATTACK_AT + M.LUNGE_OUT, M.HIT_AT + 0.05)])
        print("목업:", name)

    w, h = steps[0][0].size
    z = 0.5
    cw, ch = round(w * z), round(h * z)
    f = ImageFont.truetype(str(M.FONT), 22)
    sheet = Image.new("RGB", (3 * cw + 4 * 12, len(steps) * (ch + 40) + 40), (20, 18, 18))
    d = ImageDraw.Draw(sheet)
    for j, title in enumerate(("대기", "공격하는 순간 (돌진의 끝)", "맞는 순간 (0.05초 뒤)")):
        d.text((12 + j * (cw + 12) + 4, 8), title, fill=(235, 235, 230), font=f)
    for i, (row, (_, name)) in enumerate(zip(steps, JOBS)):
        y = 40 + i * (ch + 40)
        d.text((16, y + 4), name, fill=(170, 172, 180), font=f)
        for j, image in enumerate(row):
            sheet.paste(image.resize((cw, ch), Image.LANCZOS), (12 + j * (cw + 12), y + 34))
    sheet.save(HERE / "mock-steps.png")
    print("목업: mock-steps.png", sheet.size)

    frames = normal * 2 + slow
    if FRAMES_DIR.exists():
        shutil.rmtree(FRAMES_DIR)
    FRAMES_DIR.mkdir(parents=True)
    for i, frame in enumerate(frames):
        frame.save(FRAMES_DIR / f"{i:05d}.png")
    subprocess.run(["swift", str(ROOT / "ArtPipeline/Archive/19-motion-test/encode_mp4.swift"), str(FRAMES_DIR), str(M.FPS), str(HERE / "mock-poses.mp4")], check=True)
    shutil.rmtree(FRAMES_DIR)
    print(f"목업: mock-poses.mp4 ({len(frames)}장, {M.FPS}fps: 여섯이 보통 속도로 2회, 이어서 {SLOW}배 느리게 1회)")


if __name__ == "__main__":
    main()
