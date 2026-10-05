"""The shaman's new attack pose and the overseer's maul in front, in the battle (round 28), no API call.
  .venv/bin/python ArtPipeline/Archive/28-shaman-weapon/fit_shaman_attack.py      (first)
  .venv/bin/python ArtPipeline/Archive/28-shaman-weapon/mock_shaman_overseer.py   # mock-steps.png, mock-poses.mp4

Round 27's mockup (../27-monster-poses/mock_monster_poses.py) for the two monsters this round changes, drawn in the order
the game draws its units: the battle field's columns are built from the rearmost row to row 1, the party's before the
enemy's (UiPrefabSetup.Battle), so enemy row 1 is over party row 1 (round 27's mockup had it the other way round, and the
overseer's maul went behind Astrid: user "감독관 망치 내릴때 이미지가 용병 뒤쪽이 아닌 앞쪽으로 배치"). And while a unit
attacks (its lunge and 0.05 s more) it is drawn in front of everyone (the rule recommended for the game, both sides).
The shaman attacks with its lantern staff (the weapon recommended to take the hex spit's place), so it shows its attack
pose. The MP4 shows each twice at the game's speed, then once at half speed.
"""
import shutil
import subprocess
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
sys.path.insert(0, str(ROOT / "ArtPipeline/Archive/27-monster-poses"))
import mock_monster_poses as R  # noqa: E402
M, P = R.M, R.P

FRAMES_DIR = ROOT / "ArtPipeline/output/motion_frames_round28"
MONSTERS = [("goblin_shaman", "고블린 주술사 (등불 지팡이)"), ("mine_overseer", "광산 감독관 (150%)")]


class Front(R.Monsters):
    def __init__(self):
        super().__init__()
        shaman = self.monsters["goblin_shaman"]
        shaman["attack"] = R.sprite(HERE / "candidates/goblin_shaman_attack_wide.png", shaman["scale"])

    def frame(self, key, box=(0, 0, M.W, M.H), pose="idle", astrid_dx=0.0, astrid_tint=(1.0, 1.0, 1.0), raider_dx=0.0,
              raider_tint=(1.0, 1.0, 1.0), raider_pose="idle", attacker=None):
        """The game's order (party row 2, enemy row 2, party row 1, enemy row 1), the attacker last."""
        f = [c.crop(box) for c in self.base]
        vig = self.vig.crop(box)
        m = self.monsters[key]

        def astrid(f):
            return self.unit(f, box, vig, M.ASTRID + astrid_dx, self.sprites[pose], "astrid", astrid_tint, plate_centre=M.ASTRID)

        def monster(f):
            return self.unit(f, box, vig, M.ENEMY_1 + raider_dx, m[raider_pose], None, raider_tint, scale=m["scale"])

        f = M.over(f, self.shadow, R.CENTRE[2] - M.SHADOW_W / 2, M.FLOOR - M.SHADOW_UP - M.SHADOW_H / 2, vig, box, alpha=M.SHADOW_ALPHA, black=True)
        f = M.over(f, self.cedric, R.CENTRE[2] - self.cedric.width / 2, M.FLOOR - M.PLACE_H, vig, box)
        if key == "goblin_shaman":
            f = self.unit(f, box, vig, M.ENEMY_2, self.sprites["raider"], None)
        else:
            f = self.unit(f, box, vig, M.ENEMY_2, self.sprites["shaman"], "shaman", plate_centre=M.ENEMY_2)
        order = (monster, astrid) if attacker == "astrid" else (astrid, monster)
        for draw in order:
            f = draw(f)
        return M.to_srgb(f)


def state(t, key):
    s = R.state(t, key)
    a, h = t - M.ATTACK_AT, t - M.HIT_AT
    s["attacker"] = "astrid" if 0 <= a < M.LUNGE_OUT + M.LUNGE_BACK + M.POSE_EXTRA else ("monster" if 0 <= h < M.LUNGE_OUT + M.LUNGE_BACK + M.POSE_EXTRA else None)
    return s


def main():
    M.POSES = P.POSES / "round19"      # round 19's scene loads its own poses first (see ../23-motion-six)
    scene = Front()
    R.NO_ATTACK_POSE.clear()           # the shaman now has a weapon and its attack pose (Front loads it from here)
    cache = {}

    def frame(key, t, box=R.CROP):
        s = state(t, key)
        k = (key, box, s["pose"], round(s["astrid_dx"], 3), s["astrid_tint"], round(s["raider_dx"], 3), s["raider_tint"], s["raider_pose"], s["attacker"])
        if k not in cache:
            cache[k] = scene.frame(key, box, **s)
        return cache[k]

    frames, steps = [], []
    for key, name in MONSTERS:
        normal = [P.tag(frame(key, i / M.FPS), name, R.phase(i / M.FPS, name), "1x") for i in range(round(M.LOOP * M.FPS))]
        slow = [P.tag(frame(key, i / (M.FPS * R.SLOW)), name, R.phase(i / (M.FPS * R.SLOW), name), f"x{1 / R.SLOW:g}") for i in range(round(M.LOOP * M.FPS * R.SLOW))]
        frames += normal * 2 + slow
        steps.append([frame(key, t, R.STEPS_CROP) for t in (0.0, M.ATTACK_AT + 0.05, M.HIT_AT + M.LUNGE_OUT)])
        print("목업:", name)

    w, h = R.STEPS_CROP[2] - R.STEPS_CROP[0], R.STEPS_CROP[3] - R.STEPS_CROP[1]
    z = 0.6
    cw, ch = round(w * z), round(h * z)
    f = ImageFont.truetype(str(M.FONT), 22)
    sheet = Image.new("RGB", (3 * cw + 4 * 12, len(steps) * (ch + 40) + 40), (20, 18, 18))
    d = ImageDraw.Draw(sheet)
    for j, title in enumerate(("대기", "맞는 순간 (0.05초 뒤)", "공격하는 순간 (돌진의 끝, 공격하는 쪽이 앞)")):
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
    print(f"목업: mock-poses.mp4 ({len(frames)}장, {M.FPS}fps: 둘이 보통 속도로 2회, 이어서 {R.SLOW}배 느리게 1회)")


if __name__ == "__main__":
    main()
