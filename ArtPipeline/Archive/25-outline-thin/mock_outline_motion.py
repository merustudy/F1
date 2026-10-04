"""The valkyrie's attack and hit with the outline now, at 2/3 and at 1/2 (round 25), no API call.
  .venv/bin/python ArtPipeline/Archive/25-outline-thin/thin_outline.py          (first: the thinner outlines)
  .venv/bin/python ArtPipeline/Archive/25-outline-thin/mock_outline_motion.py   # mock-motion.mp4, mock-motion-steps.png

Round 23's battle mockup (../23-motion-six/mock_poses.py: the floor 4 battle as the game lights it, Astrid in party row 1
against the goblin raider, every plate in its column): her weapon fires (the attack pose while the lunge plays and 0.05 s
more), then the raider's blow lands (the hit pose while the recoil plays and 0.05 s more, the red flash at half strength).
Every figure on show is drawn with the outline of the variant: Astrid's three pictures, Cedric behind her, the raider and
the shaman. The crop leaves out Ella and Kai, who are the screenshot's own.

A figure is drawn as the game draws it, not with LANCZOS: the game reads the figure's texture (672x896, mip maps, bilinear)
at its place (225x300, about a third) from mip level 2, a quarter of the texture (168x224, two 2x2 box halvings in linear
light), and stretches that bilinearly to the place; a pose (2016x1008 at 675x338) the same way. Against the game's
screenshot of Astrid (ko_24 of 2026-10-04) this differs by 8.3 on average over her opaque pixels, LANCZOS by 16.4 and
level 1 by 11.2. The outline's look depends on it: the game's figures are softer than their files.
The MP4 shows each outline twice at the game's speed, then each once at half speed.
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

ART = HERE / "before"                 # the outline before the change (Assets/@Art as it was in the review)
THIN = ROOT / "ArtPipeline/output/outline"
FRAMES_DIR = ROOT / "ArtPipeline/output/motion_frames_outline"
VARIANTS = [("now", "지금 외곽선"), ("2-3", "외곽선 2/3"), ("1-2", "외곽선 1/2")]
CROP = (520, M.STAGE_TOP, 1460, M.STAGE_BOTTOM)
STEPS_CROP = (600, 150, 1300, 610)
SLOW = 2


def art(variant, folder, name):
    return (ART if variant == "now" else THIN / variant) / folder / name


def drawn(path, size):
    """The texture as the game draws it at this size: mip level 2 (a quarter, box-filtered in linear light, alpha
    premultiplied), stretched bilinearly."""
    image = Image.open(path).convert("RGBA")
    a = M.fmap(lambda m: m["a"] / 255.0, a=image.getchannel("A").convert("F"))
    small = (image.width // 4, image.height // 4)
    pre = [M.fmap(lambda m: m["c"] * m["a"], c=c, a=a).resize(small, Image.BOX).resize(size, Image.BILINEAR) for c in M.to_linear(image)]
    alpha = a.resize(small, Image.BOX).resize(size, Image.BILINEAR)
    colour = M.to_srgb([M.fmap(lambda m: m["c"] / m["max"](m["a"], 1e-6), c=c, a=alpha) for c in pre])
    out = colour.convert("RGBA")
    out.putalpha(M.fmap(lambda m: m["min"](m["max"](m["a"], 0.0), 1.0) * 255.0 + 0.5, a=alpha).convert("L"))
    return out


def figure(path):
    return drawn(path, (M.PLACE_W, M.PLACE_H))


def pose(path):
    image = Image.open(path)
    return drawn(path, (round(M.PLACE_W * image.width / 672), round(M.PLACE_H * image.height / M.FIGURE_H)))


class Outline(P.Six):
    def __init__(self):
        super().__init__()
        self.sets = {}
        for variant, _ in VARIANTS:
            self.sets[variant] = {
                "idle": figure(art(variant, "Unit/Job", "valkyrie.png")),
                "attack": pose(art(variant, "Pose/Job", "valkyrie_attack.png")),
                "hit": pose(art(variant, "Pose/Job", "valkyrie_hit.png")),
                "raider": figure(art(variant, "Unit/Enemy", "goblin_raider.png")),
                "shaman": figure(art(variant, "Unit/Enemy", "goblin_shaman.png")),
                "cedric": figure(art(variant, "Unit/Job", "paladin.png")),
            }
            print("그림:", variant)

    def use_outline(self, variant):
        chosen = self.sets[variant]
        for key in ("idle", "attack", "hit", "raider", "shaman"):
            self.sprites[key] = chosen[key]
        self.cedric = chosen["cedric"]

    def unit(self, frame, box, vig, centre, sprite, plate, tint=(1.0, 1.0, 1.0), plate_centre=None):
        # Astrid keeps her own plate here (round 23 left row 1's plate out: it showed six mercenaries there).
        return M.Scene.unit(self, frame, box, vig, centre, sprite, plate, tint, plate_centre)


def tag(frame, text):
    out = frame.copy()
    d = ImageDraw.Draw(out)
    f = ImageFont.truetype(str(M.FONT), 26)
    d.rectangle([12, 10, 12 + d.textlength(text, font=f) + 20, 48], fill=(0, 0, 0))
    d.text((22, 13), text, fill=(235, 235, 230), font=f)
    return out


def main():
    M.POSES = P.POSES / "round19"      # round 19's scene loads its own poses first (see ../23-motion-six)
    scene = Outline()
    cache = {}

    def frame(variant, t, box=CROP):
        s = M.state(t)
        key = (variant, box, s["pose"], round(s["astrid_dx"], 3), s["astrid_tint"], round(s["raider_dx"], 3), s["raider_tint"])
        if key not in cache:
            scene.use_outline(variant)
            cache[key] = scene.frame(box, plates_follow=False, **s)
        return cache[key]

    normal, slow = [], []
    for variant, name in VARIANTS:
        loop = [tag(frame(variant, i / M.FPS), f"{name} — {M.phase(i / M.FPS)}   1x") for i in range(round(M.LOOP * M.FPS))]
        normal += loop * 2
        slow += [tag(frame(variant, i / (M.FPS * SLOW)), f"{name} — {M.phase(i / (M.FPS * SLOW))}   x{1 / SLOW:g}")
                 for i in range(round(M.LOOP * M.FPS * SLOW))]
        print("목업:", name)

    # The three moments side by side for each outline.
    moments = [("대기", 0.0), ("공격하는 순간 (돌진의 끝)", M.ATTACK_AT + M.LUNGE_OUT), ("맞는 순간 (0.05초 뒤)", M.HIT_AT + 0.05)]
    w, h = STEPS_CROP[2] - STEPS_CROP[0], STEPS_CROP[3] - STEPS_CROP[1]
    f = ImageFont.truetype(str(M.FONT), 24)
    sheet = Image.new("RGB", (3 * w + 4 * 12, len(VARIANTS) * (h + 44) + 44), (20, 18, 18))
    d = ImageDraw.Draw(sheet)
    for j, (title, _) in enumerate(moments):
        d.text((12 + j * (w + 12) + 4, 10), title, fill=(235, 235, 230), font=f)
    for i, (variant, name) in enumerate(VARIANTS):
        y = 44 + i * (h + 44)
        d.text((16, y + 6), name, fill=(217, 164, 65), font=f)
        for j, (_, t) in enumerate(moments):
            sheet.paste(frame(variant, t, STEPS_CROP), (12 + j * (w + 12), y + 38))
    sheet.save(HERE / "mock-motion-steps.png")
    print("목업: mock-motion-steps.png", sheet.size)

    frames = normal + slow
    if FRAMES_DIR.exists():
        shutil.rmtree(FRAMES_DIR)
    FRAMES_DIR.mkdir(parents=True)
    for i, image in enumerate(frames):
        image.save(FRAMES_DIR / f"{i:05d}.png")
    subprocess.run(["swift", str(ROOT / "ArtPipeline/Archive/19-motion-test/encode_mp4.swift"), str(FRAMES_DIR), str(M.FPS), str(HERE / "mock-motion.mp4")], check=True)
    shutil.rmtree(FRAMES_DIR)
    print(f"목업: mock-motion.mp4 ({len(frames)}장, {M.FPS}fps: 외곽선 셋이 보통 속도로 2회씩, 이어서 {SLOW}배 느리게 1회씩)")


if __name__ == "__main__":
    main()
