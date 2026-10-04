"""Two ways a fallen mercenary's grave could play (round 21, the user asked for both as moving mockups), no API call.
  .venv/bin/python ArtPipeline/Archive/21-grave/mock_grave_motion.py      (after the graves: output/prop/)

- 1안 (recommended): the grave stands in the column the death frees, right behind the living, and stays there with the
  fallen's plate dimmed to the name. The living keep standing from row 1 without a gap.
- 2안: like the other motions, the grave shows for a moment where the mercenary fell and goes.
- 2안-B (the user's variant of 2안): no fading at a fall. The mercenary turns into a grave at once, the grave stays
  GRAVE_HOLD (0.7 s) and goes in GRAVE_VANISH (0.15 s), and only then do the ones behind walk forward: the walk waits
  while the grave stands. Only the picture waits; the battle itself advances them at the fall as before (Design/02
  §2), so their row numbers change when they start to walk, with the walk.

The same story for both, on round 19's floor 4 battle (Archive/19-motion-test/mock_motion.py: the game's light, the
same lit stage under everything taken off), with the game's figures now (the serious face) and grave C (grave_cairn):
four stand; Kai (row 4) falls; then Astrid (row 1) falls and the two behind her advance a row each (their row numbers
with them). The fall is the game's: the figure's ghost fades and sinks where it stood (BattleFxLayer.Ghost: 0.55 s,
26 down) and "쓰러짐" rises over it (FloatingTextView: 1.4 s, 84 up, 1.4 times, the danger red); the advance is the
game's walk (BattleUnitView.Walk: 0.35 s, easing out). The stage's shake at a death is left out.
A grave comes down onto the floor in 0.3 s. In 2안 it stays 0.6 s and fades out in 0.45 s, sinking a little.
Out: mock-grave-1.gif, mock-grave-2.gif, mock-grave-2b.gif (20 fps), the same as mp4 (three times over) and the steps as png.
  .venv/bin/python ArtPipeline/Archive/21-grave/mock_grave_motion.py 2b      (only one of them: 1, 2 or 2b)
"""
import shutil
import subprocess
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(ROOT / "ArtPipeline/Archive/19-motion-test"))
from mock_grave import CENTRE, GRAVE_SHADOW, PROPS, Graves, fallen, renumbered  # noqa: E402
from mock_motion import (ENEMY_1, ENEMY_2, FLOOR, FONT, PLACE_H, SHADOW_ALPHA, SHADOW_H, SHADOW_UP, SHADOW_W,  # noqa: E402
                         STAGE_BOTTOM, STAGE_TOP, figure_sprite, over, to_srgb)

GRAVE = "grave_cairn"                                # the user's pick (C)
FPS, LOOP = 20, 5.2
CROP = (0, STAGE_TOP, 1100, STAGE_BOTTOM)
KAI_FALLS, ASTRID_FALLS = 0.8, 2.6
GHOST_LIFE, GHOST_SINK = 0.55, 26.0                  # BattleFxLayer
TEXT_LIFE, TEXT_RISE, TEXT_SIZE, TEXT_BIG = 1.4, 84.0, 34, 1.4   # FloatingTextView, UiPrefabSetup.Battle
POP_TIME, POP_SCALE, FADE_FROM = 0.14, 1.35, 0.55
WALK = 0.35                                          # BattleUnitView.WalkDuration
DROP_TIME, DROP_FROM = 0.3, 28.0                     # a grave comes down onto the floor
HOLD, FADE, SINK = 0.6, 0.45, 8.0                    # 2안: how long it stays, how it goes
GRAVE_HOLD, GRAVE_VANISH = 0.7, 0.15                 # 2안-B: the grave stays, then goes; the walk waits for it
DANGER, INK = (0xC0, 0x39, 0x2B), (0x18, 0x09, 0x07)  # UiPalette
CAPTIONS = {
    1: "1안 (권장) — 쓰러지면 비게 된 열(살아 있는 줄 바로 뒤)에 무덤이 서고 남는다. 명패는 이름만 어둡게",
    2: "2안 — 다른 모션처럼 쓰러진 자리에 무덤이 잠깐 나타났다 사라진다",
    "2b": "2안-B — 쓰러지면 바로 무덤으로 바뀌고 0.7초 뒤 사라진 다음 뒤의 용병이 전진한다 (판정은 지금처럼 그 순간)",
}


def ease_out(t):
    return 1.0 - (1.0 - t) ** 3


def grave_look(age, stays):
    """How a grave shows `age` seconds after its mercenary fell: (alpha, how far above the floor)."""
    if age < 0:
        return 0.0, 0.0
    if age < DROP_TIME:
        k = age / DROP_TIME
        return min(1.0, k * 2.0), DROP_FROM * (1.0 - ease_out(k))
    if stays:
        return 1.0, 0.0
    gone = age - DROP_TIME - HOLD
    if gone < 0:
        return 1.0, 0.0
    if gone < FADE:
        k = gone / FADE
        return 1.0 - k, -SINK * k
    return 0.0, 0.0


def grave_look_b(age):
    """2안-B: the grave is there at once, stays GRAVE_HOLD and goes in GRAVE_VANISH. (alpha, lift)"""
    if age < 0 or age >= GRAVE_HOLD + GRAVE_VANISH:
        return 0.0, 0.0
    if age < GRAVE_HOLD:
        return 1.0, 0.0
    return 1.0 - (age - GRAVE_HOLD) / GRAVE_VANISH, 0.0


def text_sprite(text, scale, alpha):
    f = ImageFont.truetype(str(FONT), max(1, round(TEXT_SIZE * scale)))
    probe = ImageDraw.Draw(Image.new("RGBA", (1, 1)))
    l, t, r, b = probe.textbbox((0, 0), text, font=f)
    img = Image.new("RGBA", (r - l + 8, b - t + 8), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    a = round(255 * alpha)
    d.text((4 - l + 2, 4 - t + 2), text, font=f, fill=INK + (a,))
    d.text((4 - l, 4 - t), text, font=f, fill=DANGER + (a,))
    return img


def faded(sprite, alpha):
    out = sprite.copy()
    out.putalpha(out.getchannel("A").point(lambda v: round(v * alpha)))
    return out


class Story:
    def __init__(self):
        self.g = Graves()
        self.grave = figure_sprite(PROPS / f"{GRAVE}.png")

    def frame(self, t, option):
        g, scene, box = self.g, self.g.scene, CROP
        f = [c.crop(box) for c in g.base]
        vig = scene.vig.crop(box)
        f = scene.unit(f, box, vig, ENEMY_2, scene.sprites["shaman"], "shaman")
        f = scene.unit(f, box, vig, ENEMY_1, scene.sprites["raider"], "raider")

        # Who stands where, and the graves: (column, name, the time its mercenary fell). In 2안-B the walk (and the new
        # row numbers with it) waits until the grave in front has gone.
        walk_at = ASTRID_FALLS + (GRAVE_HOLD + GRAVE_VANISH if option == "2b" else 0.0)
        if t < KAI_FALLS:
            living, walking = {1: "astrid", 2: "cedric", 3: "ella", 4: "kai"}, {}
        elif t < ASTRID_FALLS:
            living, walking = {1: "astrid", 2: "cedric", 3: "ella"}, {}
        elif t < walk_at:
            living, walking = {2: "cedric", 3: "ella"}, {}
        else:
            living, walking = {1: "cedric", 2: "ella"}, {1: 2, 2: 3}
        if option == 1:
            graves = [(4, "kai", KAI_FALLS), (3, "astrid", ASTRID_FALLS)]
        else:
            graves = [(4, "kai", KAI_FALLS), (1, "astrid", ASTRID_FALLS)]

        # The graves lie on the floor behind every unit; in 1안 each keeps its fallen's plate, dimmed.
        for column, name, fell in sorted(graves, reverse=True):
            alpha, lift = grave_look_b(t - fell) if option == "2b" else grave_look(t - fell, stays=option == 1)
            if alpha <= 0:
                continue
            centre = CENTRE[column]
            shadow = scene.shadow.resize((round(SHADOW_W * GRAVE_SHADOW), SHADOW_H), Image.LANCZOS)
            f = over(f, shadow, centre - shadow.width / 2, FLOOR - SHADOW_UP - SHADOW_H / 2, vig, box, alpha=SHADOW_ALPHA * alpha, black=True)
            f = over(f, self.grave, centre - self.grave.width / 2, FLOOR - PLACE_H - lift, vig, box, alpha=alpha)
            if option == 1:
                plate, cut = g.plates[name]
                f = over(f, fallen(plate), centre - plate.width / 2, cut[1], vig, box, alpha=min(1.0, (t - fell) / DROP_TIME), vignetted=False)

        # The living, the back row first; one who advanced walks in from the column it stood in.
        for row in sorted(living, reverse=True):
            name = living[row]
            centre = CENTRE[row]
            if row in walking:
                k = min(1.0, (t - walk_at) / WALK)
                centre += (CENTRE[walking[row]] - CENTRE[row]) * (1.0 - k) ** 2
            f = over(f, scene.shadow, centre - SHADOW_W / 2, FLOOR - SHADOW_UP - SHADOW_H / 2, vig, box, alpha=SHADOW_ALPHA, black=True)
            f = over(f, g.arts[name], centre - g.arts[name].width / 2, FLOOR - PLACE_H, vig, box)
            plate, cut = g.plates[name]
            f = over(f, renumbered(plate, row), centre - plate.width / 2, cut[1], vig, box, vignetted=False)

        # The fall itself (the effects layer, over the units): the ghost and the word.
        for name, column, fell in (("kai", 4, KAI_FALLS), ("astrid", 1, ASTRID_FALLS)):
            age = t - fell
            if option != "2b" and 0 <= age < GHOST_LIFE:     # 2안-B: no fading figure, the grave is there at once
                k = age / GHOST_LIFE
                art = g.arts[name]
                f = over(f, art, CENTRE[column] - art.width / 2, FLOOR - PLACE_H + GHOST_SINK * k, vig, box, alpha=1.0 - k)
            if 0 <= age < TEXT_LIFE:
                k = age / TEXT_LIFE
                pop = POP_SCALE + (1.0 - POP_SCALE) * min(1.0, age / POP_TIME)
                alpha = 1.0 if k < FADE_FROM else 1.0 - (k - FADE_FROM) / (1.0 - FADE_FROM)
                word = text_sprite("쓰러짐", TEXT_BIG * pop, alpha)
                y = FLOOR - PLACE_H - TEXT_RISE * (1.0 - (1.0 - k) ** 2)
                f = over(f, word, CENTRE[column] - word.width / 2, y - word.height / 2, vig, box)
        return to_srgb(f)


def captioned(img, option):
    out = Image.new("RGB", (img.width, img.height + 46), (24, 22, 22))
    out.paste(img, (0, 46))
    ImageDraw.Draw(out).text((14, 9), CAPTIONS[option], fill=(235, 235, 230), font=ImageFont.truetype(str(FONT), 24))
    return out


def save_gif(frames, path):
    picks = [frames[0], frames[round((KAI_FALLS + 0.4) * FPS)], frames[round((ASTRID_FALLS + 0.3) * FPS)], frames[-1]]
    montage = Image.new("RGB", (frames[0].width, frames[0].height * len(picks)))
    for i, m in enumerate(picks):
        montage.paste(m, (0, i * m.height))
    palette = montage.quantize(colors=255, method=Image.Quantize.MEDIANCUT)
    gif = [fr.quantize(palette=palette, dither=Image.Dither.NONE) for fr in frames]
    gif[0].save(path, save_all=True, append_images=gif[1:], duration=1000 // FPS, loop=0)
    print("목업:", path.name, f"({len(gif)}장, {FPS}fps, {LOOP}초 반복)")


def save_mp4(frames, path):
    folder = ROOT / "ArtPipeline/output/grave_frames"
    if folder.exists():
        shutil.rmtree(folder)
    folder.mkdir(parents=True)
    for i, fr in enumerate(frames * 3):
        fr.save(folder / f"{i:05d}.png")
    subprocess.run(["swift", str(ROOT / "ArtPipeline/Archive/19-motion-test/encode_mp4.swift"), str(folder), str(FPS), str(path)], check=True)
    shutil.rmtree(folder)


def main():
    story = Story()
    options = sys.argv[1:] or ["1", "2", "2b"]
    for option in [int(o) if o.isdigit() else o for o in options]:
        frames = [captioned(story.frame(i / FPS, option), option) for i in range(round(LOOP * FPS))]
        save_gif(frames, HERE / f"mock-grave-{option}.gif")
        save_mp4(frames, HERE / f"mock-grave-{option}.mp4")
        # the moments side by side for a quick look: before, after Kai, after Astrid (2안-B: also while the grave
        # stands and the ones behind wait, and while they walk)
        if option == "2b":
            moments = [0.0, KAI_FALLS + 0.3, ASTRID_FALLS + 0.3, ASTRID_FALLS + GRAVE_HOLD + GRAVE_VANISH + WALK / 2, LOOP - 0.05]
        else:
            moments = [0.0, KAI_FALLS + 1.0, LOOP - 0.05]
        strip = [frames[min(len(frames) - 1, round(m * FPS))] for m in moments]
        sheet = Image.new("RGB", (strip[0].width, sum(s.height for s in strip) + 16), (12, 12, 12))
        y = 0
        for s in strip:
            sheet.paste(s, (0, y))
            y += s.height + 8
        sheet.save(HERE / f"mock-grave-{option}-steps.png")


if __name__ == "__main__":
    main()
