"""The goblin raider's attack and hit poses in the battle (round 26), no API call.
  .venv/bin/python ArtPipeline/Archive/26-monster-motion/fit_monster_poses.py     (first: the poses at the figure's size)
  .venv/bin/python ArtPipeline/Archive/26-monster-motion/mock_monster_poses.py    # mock-steps.png, mock-poses.mp4

Round 23's battle mockup (../23-motion-six/mock_poses.py: the floor 4 battle as the game lights it, Astrid in party row 1
against the goblin raider in enemy row 1, every plate in its column), drawn the way round 25 draws it (../25-outline-thin:
a texture read at mip level 2 and stretched bilinearly, as the game does) with the art the game has now. Astrid's weapon
fires (her attack pose while the lunge plays and 0.05 s more) and the raider is struck; then the raider's blade fires and
Astrid is struck (her hit pose, the red flash at half strength). Two rows of the same moments:
- now: the raider as the game shows it, without poses: it recoils 12 and flashes red at full strength, it lunges 26.
- test: the raider swaps in its hit pose while it recoils and 0.05 s more (the flash at half strength, as for any unit
  with a hit pose), and its attack pose while it lunges and 0.05 s more (its rusty blade is a weapon: Docs/Design/10 §5).
The MP4 shows the pair twice at the game's speed, then once at half speed.
"""
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
FRAMES_DIR = ROOT / "ArtPipeline/output/motion_frames_monster"
CROP = (520, M.STAGE_TOP, 1460, M.STAGE_BOTTOM)
STEPS_CROP = (600, 150, 1300, 610)
SLOW = 2
ROWS = [(False, "지금 — 몬스터는 자세 없이 돌진·밀림, 번쩍임 그대로"), (True, "시험 — 약탈자의 공격·피격 자세, 번쩍임 절반")]


class Raider(P.Six):
    def __init__(self):
        super().__init__()
        self.sprites.update({
            "idle": O.figure(ART / "Unit/Job/valkyrie.png"),
            "attack": O.pose(ART / "Pose/Job/valkyrie_attack.png"),
            "hit": O.pose(ART / "Pose/Job/valkyrie_hit.png"),
            "raider": O.figure(ART / "Unit/Enemy/goblin_raider.png"),
            "raider_attack": O.pose(HERE / "candidates/goblin_raider_attack_wide.png"),
            "raider_hit": O.pose(HERE / "candidates/goblin_raider_hit_wide.png"),
            "shaman": O.figure(ART / "Unit/Enemy/goblin_shaman.png"),
        })
        self.cedric = O.figure(ART / "Unit/Job/paladin.png")

    def unit(self, frame, box, vig, centre, sprite, plate, tint=(1.0, 1.0, 1.0), plate_centre=None):
        # Astrid keeps her own plate (round 23 left it out: it showed six mercenaries there).
        return M.Scene.unit(self, frame, box, vig, centre, sprite, plate, tint, plate_centre)

    def frame(self, box=(0, 0, M.W, M.H), pose="idle", astrid_dx=0.0, astrid_tint=(1.0, 1.0, 1.0), raider_dx=0.0,
              raider_tint=(1.0, 1.0, 1.0), raider_pose="idle"):
        """Round 23's frame with every plate in its column, the raider in the picture of its pose."""
        f = [c.crop(box) for c in self.base]
        vig = self.vig.crop(box)
        raider = self.sprites["raider" if raider_pose == "idle" else f"raider_{raider_pose}"]
        f = self.unit(f, box, vig, M.ENEMY_2, self.sprites["shaman"], "shaman", plate_centre=M.ENEMY_2)
        f = self.unit(f, box, vig, M.ENEMY_1 + raider_dx, raider, "raider", raider_tint, plate_centre=M.ENEMY_1)
        f = M.over(f, self.shadow, CENTRE[2] - M.SHADOW_W / 2, M.FLOOR - M.SHADOW_UP - M.SHADOW_H / 2, vig, box, alpha=M.SHADOW_ALPHA, black=True)
        f = M.over(f, self.cedric, CENTRE[2] - self.cedric.width / 2, M.FLOOR - M.PLACE_H, vig, box)
        f = self.unit(f, box, vig, M.ASTRID + astrid_dx, self.sprites[pose], "astrid", astrid_tint, plate_centre=M.ASTRID)
        return M.to_srgb(f)


def state(t, poses):
    """Round 19's moments (M.state), and with poses the raider's: its hit pose while it recoils from Astrid's blow (the
    flash at half strength), its attack pose while its own blade lunges."""
    s = M.state(t)
    s["raider_pose"] = "idle"
    if poses:
        a, h = t - M.ATTACK_AT, t - M.HIT_AT
        s["raider_tint"] = M.flash(a, M.POSE_FLASH)
        if 0 <= a < M.RECOIL_T + M.POSE_EXTRA:
            s["raider_pose"] = "hit"
        if 0 <= h < M.LUNGE_OUT + M.LUNGE_BACK + M.POSE_EXTRA:
            s["raider_pose"] = "attack"
    return s


def phase(t):
    a, h = t - M.ATTACK_AT, t - M.HIT_AT
    if 0 <= a < M.LUNGE_OUT + M.LUNGE_BACK + M.POSE_EXTRA:
        return "아스트리드의 공격 → 약탈자가 맞음"
    if 0 <= h < M.LUNGE_OUT + M.LUNGE_BACK + M.POSE_EXTRA:
        return "약탈자의 공격 → 아스트리드가 맞음"
    return "대기"


def bar(image, text, colour=(235, 235, 230)):
    f = ImageFont.truetype(str(M.FONT), 24)
    out = Image.new("RGB", (image.width, image.height + 40), (20, 18, 18))
    out.paste(image, (0, 40))
    ImageDraw.Draw(out).text((14, 7), text, fill=colour, font=f)
    return out


def main():
    M.POSES = P.POSES / "round19"      # round 19's scene loads its own poses first (see ../23-motion-six)
    scene = Raider()
    cache = {}

    def frame(poses, t, box=CROP):
        s = state(t, poses)
        key = (box, s["pose"], round(s["astrid_dx"], 3), s["astrid_tint"], round(s["raider_dx"], 3), s["raider_tint"], s["raider_pose"])
        if key not in cache:
            cache[key] = scene.frame(box, **s)
        return cache[key]

    def pair(t, speed):
        rows = [bar(frame(poses, t), name, (217, 164, 65) if poses else (235, 235, 230)) for poses, name in ROWS]
        out = Image.new("RGB", (rows[0].width, sum(r.height for r in rows)), (20, 18, 18))
        y = 0
        for r in rows:
            out.paste(r, (0, y))
            y += r.height
        return P.tag(out, "고블린 약탈자", phase(t), speed)

    normal = [pair(i / M.FPS, "1x") for i in range(round(M.LOOP * M.FPS))]
    slow = [pair(i / (M.FPS * SLOW), f"x{1 / SLOW:g}") for i in range(round(M.LOOP * M.FPS * SLOW))]

    moments = [("대기", 0.0), ("약탈자가 맞는 순간 (0.05초 뒤)", M.ATTACK_AT + 0.05), ("약탈자가 공격하는 순간 (돌진의 끝)", M.HIT_AT + M.LUNGE_OUT)]
    w, h = STEPS_CROP[2] - STEPS_CROP[0], STEPS_CROP[3] - STEPS_CROP[1]
    f = ImageFont.truetype(str(M.FONT), 24)
    sheet = Image.new("RGB", (3 * w + 4 * 12, len(ROWS) * (h + 44) + 44), (20, 18, 18))
    d = ImageDraw.Draw(sheet)
    for j, (title, _) in enumerate(moments):
        d.text((12 + j * (w + 12) + 4, 10), title, fill=(235, 235, 230), font=f)
    for i, (poses, name) in enumerate(ROWS):
        y = 44 + i * (h + 44)
        d.text((16, y + 6), name, fill=(217, 164, 65) if poses else (235, 235, 230), font=f)
        for j, (_, t) in enumerate(moments):
            sheet.paste(frame(poses, t, STEPS_CROP), (12 + j * (w + 12), y + 38))
    sheet.save(HERE / "mock-steps.png")
    print("목업: mock-steps.png", sheet.size)

    frames = normal * 2 + slow
    if FRAMES_DIR.exists():
        shutil.rmtree(FRAMES_DIR)
    FRAMES_DIR.mkdir(parents=True)
    for i, image in enumerate(frames):
        image.save(FRAMES_DIR / f"{i:05d}.png")
    subprocess.run(["swift", str(ROOT / "ArtPipeline/Archive/19-motion-test/encode_mp4.swift"), str(FRAMES_DIR), str(M.FPS), str(HERE / "mock-poses.mp4")], check=True)
    shutil.rmtree(FRAMES_DIR)
    print(f"목업: mock-poses.mp4 ({len(frames)}장, {M.FPS}fps: 지금·시험을 위아래로, 보통 속도로 2회, 이어서 {SLOW}배 느리게 1회)")


if __name__ == "__main__":
    main()
