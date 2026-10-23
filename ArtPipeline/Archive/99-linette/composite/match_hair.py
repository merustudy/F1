"""Round 99: give dialogue 8-2 and 8 the hair colour of dialogue 7. No API call.

The three were generated apart and the black of the hair came out a little lighter in 8 and lighter still in 8-2.
Only the tone moves, never a shape: in the head (above HEAD_BOTTOM) the hair is the near-neutral dark (channel
spread under SPREAD, grey level under DARK), so the skin, the eyes' brown and the lips stay out. Its grey levels in
8-2 or 8 are mapped onto those of 7 quantile by quantile, the mapping fading to no change over FADE grey levels
above the hair's lightest strands, and each pixel is scaled as a whole so its hue stays.

The in-game picture (linette_ingame, the user: "새 인게임 이미지 머리색을 d이미지의 머리색으로 변경") takes the hair
of D the same way, but channel by channel, since its hair is a warmer grey-brown than D's neutral black; it is
written as linette_ingame_dhair and the generated linette_ingame stays as it is.

Run: .venv/bin/python ArtPipeline/Archive/99-linette/composite/match_hair.py
Reads the pictures with the pasted hands (candidates/linette_portrait<n>_paste.raw.png) and writes
ArtPipeline/output/character/linette_portrait<n>.raw.png, then linette_ingame.raw.png -> linette_ingame_dhair.raw.png;
refit them with gen_image.py --refit afterwards.
"""
from pathlib import Path

from PIL import Image

HERE = Path(__file__).resolve().parent
ROUND = HERE.parent
ROOT = HERE.parents[3]
OUTPUT = ROOT / "ArtPipeline" / "output" / "character"
TARGET = "linette_portrait7"
SOURCES = ["linette_portrait8b", "linette_portrait8"]
HEAD_BOTTOM = 500  # above the neckline: the hair, the face and the choker
DARK, SPREAD, FADE = 120, 30, 50
STEPS = 200
# The in-game pictures (1024x1024): the hair and the choker end above the dress's neckline.
INGAME_TARGET, INGAME_SOURCE, INGAME_OUT = "linette_d", "linette_ingame", "linette_ingame_dhair"
INGAME_HEAD_BOTTOM, INGAME_SPREAD = 225, 60  # 60 takes the warm strands in the bangs too; the skin and the lips spread wider


def grey(p):
    return (p[0] + p[1] + p[2]) / 3


def is_hair(p):
    return p[3] > 250 and grey(p) < DARK and max(p[:3]) - min(p[:3]) < SPREAD


def hair_levels(image):
    px = image.load()
    levels = sorted(grey(px[x, y]) for y in range(HEAD_BOTTOM) for x in range(image.size[0]) if is_hair(px[x, y]))
    return [levels[min(len(levels) - 1, len(levels) * i // STEPS)] for i in range(STEPS + 1)]


def mapping(source, target):
    """Grey level -> new grey level: quantile to quantile up to the source's lightest hair, then fading to none."""
    top = source[-1]

    def new_level(level):
        if level >= top + FADE:
            return level
        if level >= top:
            share = (level - top) / FADE
            return target[-1] + (level - target[-1]) * share
        for i in range(STEPS):
            if source[i] <= level <= source[i + 1]:
                span = source[i + 1] - source[i]
                t = (level - source[i]) / span if span else 0
                return target[i] + (target[i + 1] - target[i]) * t
        return target[0] * level / source[0] if source[0] else level

    return new_level


def channel_levels(image, bottom, spread):
    """The hair's quantiles in each channel (the near-neutral dark above bottom)."""
    px = image.load()
    hair = [px[x, y] for y in range(bottom) for x in range(image.size[0])
            if px[x, y][3] > 0 and grey(px[x, y]) < DARK and max(px[x, y][:3]) - min(px[x, y][:3]) < spread]
    levels = []
    for channel in range(3):
        values = sorted(p[channel] for p in hair)
        levels.append([values[min(len(values) - 1, len(values) * i // STEPS)] for i in range(STEPS + 1)])
    return levels


def match_ingame():
    target = channel_levels(Image.open(OUTPUT / f"{INGAME_TARGET}.raw.png").convert("RGBA"),
                            INGAME_HEAD_BOTTOM, INGAME_SPREAD)
    image = Image.open(OUTPUT / f"{INGAME_SOURCE}.raw.png").convert("RGBA")
    source = channel_levels(image, INGAME_HEAD_BOTTOM, INGAME_SPREAD)
    maps = [mapping(source[channel], target[channel]) for channel in range(3)]
    px = image.load()
    changed = 0
    for y in range(INGAME_HEAD_BOTTOM):
        for x in range(image.size[0]):
            p = px[x, y]
            if p[3] == 0 or grey(p) >= DARK + FADE or max(p[:3]) - min(p[:3]) >= INGAME_SPREAD:
                continue
            px[x, y] = tuple(max(0, min(255, round(maps[channel](p[channel])))) for channel in range(3)) + (p[3],)
            changed += 1
    image.save(OUTPUT / f"{INGAME_OUT}.raw.png")
    middle = STEPS // 2
    print(f"{INGAME_OUT}: hair median {tuple(source[c][middle] for c in range(3))} -> "
          f"{tuple(target[c][middle] for c in range(3))}, {changed} px")


def main():
    target = hair_levels(Image.open(OUTPUT / f"{TARGET}.raw.png").convert("RGBA"))
    for name in SOURCES:
        image = Image.open(ROUND / "candidates" / f"{name}_paste.raw.png").convert("RGBA")
        source = hair_levels(image)
        new_level = mapping(source, target)
        px = image.load()
        changed = 0
        for y in range(HEAD_BOTTOM):
            for x in range(image.size[0]):
                p = px[x, y]
                level = grey(p)
                if p[3] == 0 or level == 0 or level >= source[-1] + FADE or max(p[:3]) - min(p[:3]) >= SPREAD:
                    continue
                scale = new_level(level) / level
                px[x, y] = tuple(max(0, min(255, round(c * scale))) for c in p[:3]) + (p[3],)
                changed += 1
        image.save(OUTPUT / f"{name}.raw.png")
        print(f"{name}: hair median {source[STEPS // 2]:.0f} -> {target[STEPS // 2]:.0f}, "
              f"lightest {source[-1]:.0f} -> {target[-1]:.0f}, {changed} px")
    match_ingame()


if __name__ == "__main__":
    main()
