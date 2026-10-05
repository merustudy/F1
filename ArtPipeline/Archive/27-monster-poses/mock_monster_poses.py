"""The five monsters' attack and hit poses in the battle (rounds 26 and 27), no API call.
  .venv/bin/python ArtPipeline/Archive/27-monster-poses/fit_monster_poses.py     (first: the poses at the figures' size)
  .venv/bin/python ArtPipeline/Archive/27-monster-poses/mock_monster_poses.py    # mock-steps.png, mock-poses.mp4

Round 26's mockup (../26-monster-motion/mock_monster_poses.py: the floor 4 battle as the game lights it, Astrid in party
row 1, every plate in its column, drawn the way the game draws a texture) for each monster in turn in enemy row 1: Astrid's
weapon fires and it is struck (its hit pose while it recoils and 0.05 s more, the red flash at half strength), then its
own item fires (its attack pose while it lunges and 0.05 s more) and Astrid is struck. The shaman has no attack pose: its
hex spit is an attack item, which lunges without a pose (Docs/Design/10 §5), so it lunges as it is. The overseer is drawn
at its FigureScale (150%, from the feet), its texture read at the mip level the game reads at that size.
Row 1's plate is left out (the screenshot's plate is the raider's); row 2 is the shaman with its plate, or the raider
without one while the shaman stands in row 1. The MP4 shows each monster twice at the game's speed, then once at half.
"""
import csv
import math
import shutil
import subprocess
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
sys.path.insert(0, str(ROOT / "ArtPipeline/Archive/23-motion-six"))
sys.path.insert(0, str(ROOT / "ArtPipeline/Archive/25-outline-thin"))
import mock_poses as P  # noqa: E402
import mock_outline_motion as O  # noqa: E402
from mock_grave import CENTRE  # noqa: E402
M = P.M

ART = ROOT / "Assets/@Art"
R26 = ROOT / "ArtPipeline/Archive/26-monster-motion"
FRAMES_DIR = ROOT / "ArtPipeline/output/motion_frames_monsters"
CROP = (520, M.STAGE_TOP, 1460, M.STAGE_BOTTOM)
STEPS_CROP = (600, 110, 1300, 610)
SLOW = 2
MONSTERS = [("goblin_raider", "고블린 약탈자"), ("cave_rat", "동굴 쥐"), ("goblin_archer", "고블린 궁수"),
            ("goblin_shaman", "고블린 주술사 (공격 자세 없음)"), ("mine_overseer", "광산 감독관 (150%)")]
NO_ATTACK_POSE = {"goblin_shaman"}


def figure_scales():
    with (ROOT / "Assets/@Data/Source/EnemyData.csv").open(encoding="utf-8-sig", newline="") as handle:
        return {row["Id"]: int(row["FigureScale"]) / 100 for row in csv.DictReader(handle)}


def drawn(path, size):
    """The texture as the game draws it at this size: the mip level nearest the ratio of texture to screen (level 2 for a
    figure at its place, level 1 for the boss at 150%), box-filtered in linear light with alpha premultiplied, stretched
    bilinearly (round 25's way, ../25-outline-thin/mock_outline_motion.py, at any level)."""
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


def sprite(path, scale=1.0):
    """A figure (672x896) or a pose (2016x1008) at its place on the screen, times the unit's FigureScale."""
    image = Image.open(path)
    return drawn(path, (round(M.PLACE_W * image.width / 672 * scale), round(M.PLACE_H * image.height / M.FIGURE_H * scale)))


class Monsters(P.Six):
    def __init__(self):
        super().__init__()
        self.sprites.update({
            "idle": O.figure(ART / "Unit/Job/valkyrie.png"),
            "attack": O.pose(ART / "Pose/Job/valkyrie_attack.png"),
            "hit": O.pose(ART / "Pose/Job/valkyrie_hit.png"),
            "shaman": O.figure(ART / "Unit/Enemy/goblin_shaman.png"),
            "raider": O.figure(ART / "Unit/Enemy/goblin_raider.png"),
        })
        self.cedric = O.figure(ART / "Unit/Job/paladin.png")
        scales = figure_scales()
        self.monsters = {}
        for key, _ in MONSTERS:
            folder = R26 if key == "goblin_raider" else HERE
            s = scales[key]
            self.monsters[key] = {"scale": s, "idle": sprite(ART / f"Unit/Enemy/{key}.png", s), "hit": sprite(folder / f"candidates/{key}_hit_wide.png", s)}
            if key not in NO_ATTACK_POSE:
                self.monsters[key]["attack"] = sprite(folder / f"candidates/{key}_attack_wide.png", s)
            print("그림:", key)

    def unit(self, frame, box, vig, centre, sprite, plate, tint=(1.0, 1.0, 1.0), plate_centre=None, scale=1.0):
        """A unit's shadow, its picture (its FigureScale from the feet) and its plate when it has one."""
        frame = M.over(frame, self.shadow, centre - M.SHADOW_W / 2, M.FLOOR - M.SHADOW_UP - M.SHADOW_H / 2, vig, box, alpha=M.SHADOW_ALPHA, black=True)
        frame = M.over(frame, sprite, centre - sprite.width / 2, M.FLOOR - M.PLACE_H * scale, vig, box, tint=tint)
        if plate is None:
            return frame
        image, cut = self.plates[plate]
        x = centre if plate_centre is None else plate_centre
        return M.over(frame, image, x - image.width / 2, cut[1], vig, box, vignetted=False)

    def frame(self, key, box=(0, 0, M.W, M.H), pose="idle", astrid_dx=0.0, astrid_tint=(1.0, 1.0, 1.0), raider_dx=0.0,
              raider_tint=(1.0, 1.0, 1.0), raider_pose="idle"):
        f = [c.crop(box) for c in self.base]
        vig = self.vig.crop(box)
        if key == "goblin_shaman":
            f = self.unit(f, box, vig, M.ENEMY_2, self.sprites["raider"], None)
        else:
            f = self.unit(f, box, vig, M.ENEMY_2, self.sprites["shaman"], "shaman", plate_centre=M.ENEMY_2)
        m = self.monsters[key]
        f = self.unit(f, box, vig, M.ENEMY_1 + raider_dx, m[raider_pose], None, raider_tint, scale=m["scale"])
        f = M.over(f, self.shadow, CENTRE[2] - M.SHADOW_W / 2, M.FLOOR - M.SHADOW_UP - M.SHADOW_H / 2, vig, box, alpha=M.SHADOW_ALPHA, black=True)
        f = M.over(f, self.cedric, CENTRE[2] - self.cedric.width / 2, M.FLOOR - M.PLACE_H, vig, box)
        f = self.unit(f, box, vig, M.ASTRID + astrid_dx, self.sprites[pose], "astrid", astrid_tint, plate_centre=M.ASTRID)
        return M.to_srgb(f)


def state(t, key):
    """Round 19's moments (M.state) with the monster's poses: its hit pose while it recoils from Astrid's blow (the flash
    at half strength), its attack pose while its own item lunges (none for the shaman)."""
    s = M.state(t)
    a, h = t - M.ATTACK_AT, t - M.HIT_AT
    s["raider_tint"] = M.flash(a, M.POSE_FLASH)
    s["raider_pose"] = "idle"
    if 0 <= a < M.RECOIL_T + M.POSE_EXTRA:
        s["raider_pose"] = "hit"
    if key not in NO_ATTACK_POSE and 0 <= h < M.LUNGE_OUT + M.LUNGE_BACK + M.POSE_EXTRA:
        s["raider_pose"] = "attack"
    return s


def phase(t, name):
    a, h = t - M.ATTACK_AT, t - M.HIT_AT
    if 0 <= a < M.LUNGE_OUT + M.LUNGE_BACK + M.POSE_EXTRA:
        return f"아스트리드의 공격 → {name.split(' (')[0]}의 피격 자세"
    if 0 <= h < M.LUNGE_OUT + M.LUNGE_BACK + M.POSE_EXTRA:
        return f"{name.split(' (')[0]}의 공격{'(자세 없이 돌진)' if '주술사' in name else ' 자세'} → 아스트리드가 맞음"
    return "대기"


def main():
    M.POSES = P.POSES / "round19"      # round 19's scene loads its own poses first (see ../23-motion-six)
    scene = Monsters()
    cache = {}

    def frame(key, t, box=CROP):
        s = state(t, key)
        k = (key, box, s["pose"], round(s["astrid_dx"], 3), s["astrid_tint"], round(s["raider_dx"], 3), s["raider_tint"], s["raider_pose"])
        if k not in cache:
            cache[k] = scene.frame(key, box, **s)
        return cache[k]

    frames, steps = [], []
    for key, name in MONSTERS:
        normal = [P.tag(frame(key, i / M.FPS), name, phase(i / M.FPS, name), "1x") for i in range(round(M.LOOP * M.FPS))]
        slow = [P.tag(frame(key, i / (M.FPS * SLOW)), name, phase(i / (M.FPS * SLOW), name), f"x{1 / SLOW:g}") for i in range(round(M.LOOP * M.FPS * SLOW))]
        frames += normal * 2 + slow
        steps.append([frame(key, t, STEPS_CROP) for t in (0.0, M.ATTACK_AT + 0.05, M.HIT_AT + M.LUNGE_OUT)])
        print("목업:", name)

    w, h = STEPS_CROP[2] - STEPS_CROP[0], STEPS_CROP[3] - STEPS_CROP[1]
    z = 0.6
    cw, ch = round(w * z), round(h * z)
    f = ImageFont.truetype(str(M.FONT), 22)
    sheet = Image.new("RGB", (3 * cw + 4 * 12, len(steps) * (ch + 40) + 40), (20, 18, 18))
    d = ImageDraw.Draw(sheet)
    for j, title in enumerate(("대기", "맞는 순간 (0.05초 뒤)", "공격하는 순간 (돌진의 끝)")):
        d.text((12 + j * (cw + 12) + 4, 8), title, fill=(235, 235, 230), font=f)
    for i, (row, (_, name)) in enumerate(zip(steps, MONSTERS)):
        y = 40 + i * (ch + 40)
        d.text((16, y + 4), name, fill=(217, 164, 65), font=f)
        for j, image in enumerate(row):
            sheet.paste(image.resize((cw, ch), Image.LANCZOS), (12 + j * (cw + 12), y + 34))
    sheet.save(HERE / "mock-steps.png")
    print("목업: mock-steps.png", sheet.size)

    if FRAMES_DIR.exists():
        shutil.rmtree(FRAMES_DIR)
    FRAMES_DIR.mkdir(parents=True)
    for i, image in enumerate(frames):
        image.save(FRAMES_DIR / f"{i:05d}.png")
    subprocess.run(["swift", str(ROOT / "ArtPipeline/Archive/19-motion-test/encode_mp4.swift"), str(FRAMES_DIR), str(M.FPS), str(HERE / "mock-poses.mp4")], check=True)
    shutil.rmtree(FRAMES_DIR)
    print(f"목업: mock-poses.mp4 ({len(frames)}장, {M.FPS}fps: 몬스터마다 보통 속도로 2회, 이어서 {SLOW}배 느리게 1회)")


if __name__ == "__main__":
    main()
