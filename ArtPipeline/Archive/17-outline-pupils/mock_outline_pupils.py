"""Round 17 review: the figures' outline at half its width, and the monsters' eyes without pupils. No API call.
  .venv/bin/python ArtPipeline/Archive/17-outline-pupils/mock_outline_pupils.py

The figures are made by the pipeline itself: gen_image.fit on the saved raw images, with FIGURE_OUTLINE at 8 (now; it
gives the Assets files bit for bit) or 4 (half), and for the monsters also on the raw images remove_pupils.py edited.
They are stood on the battle stage as the game stands them (UiPrefabSetup.Battle: a 225x300 place, its foot on the floor
at 506, the shadow 150x26 at 35% six above it, the boss at 150%) over the dungeon's background lit by the candle as the
game lights it at 7.3 s (Archive/16-candle-light/mock_candle_light.py), and blended in linear light. The party and
the monsters in one line are a made-up line-up, to show every figure the outline and the eyes touch; the boss is shown
on his own. The texture is sampled the way the game samples a mipmapped sprite at a third of its size (box to the mip,
then bilinear).
"""
import sys
from io import BytesIO
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent; ROOT = HERE.parents[2]
sys.path.insert(0, str(ROOT / "ArtPipeline" / "tools"))
sys.path.insert(0, str(ROOT / "ArtPipeline" / "Archive" / "16-candle-light"))
sys.path.insert(0, str(HERE))
import gen_image as g  # noqa: E402
import mock_candle_light as light  # noqa: E402
import remove_pupils  # noqa: E402

FONT = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"
FLOOR, PLACE_W, PLACE_H = 506, 225, 300
SHADOW = (150, 26, 0.35, 6)
PARTY = [("knight", 777), ("spellblade", 587), ("bishop", 397), ("archmage", 207)]                 # rows 1..4
ENEMIES = [("goblin_raider", 1143), ("goblin_archer", 1333), ("goblin_shaman", 1523), ("cave_rat", 1713)]
BOSS = ("mine_overseer", 1143, 1.5)
STAGE = (0, 84, 1920, 620)


def figure(kind, key, outline, pupils=True, edited=None):
    """The fitted figure as the pipeline would make it, with this outline, from the raw (or its pupil-less edit)."""
    g.FIGURE_OUTLINE = outline
    # the monsters from their confirmed raws (with pupils): the pipeline's own raws lost theirs after the review
    raw = ((remove_pupils.SOURCE if kind == "enemy" else g.OUTPUT_DIR / kind) / f"{key}.raw.png").read_bytes()
    if not pupils:
        image, _ = remove_pupils.remove(Image.open(BytesIO(raw)), remove_pupils.EYES[key])
        buffer = BytesIO(); image.save(buffer, format="PNG"); raw = buffer.getvalue()
    png, _ = g.fit(kind, raw, g.read_roster_row(kind, key, None))
    g.FIGURE_OUTLINE = 8
    return Image.open(BytesIO(png)).convert("RGBA")


def sampled(art, w, h):
    """A mipmapped texture drawn at about a third of its size: the nearest larger mip (box), then bilinear."""
    mip = art
    while mip.width // 2 >= w and mip.height // 2 >= h:
        mip = mip.resize((mip.width // 2, mip.height // 2), Image.BOX)
    return mip.resize((round(w), round(h)), Image.BILINEAR)


def over(frame, layer):
    """Puts an sRGB RGBA layer over the linear frame, blended in linear light."""
    a = light.fmap(lambda m: m["a"] / 255.0, a=layer.getchannel("A").convert("F"))
    cols = light.to_linear(layer.convert("RGB"))
    return [light.fmap(lambda m: m["d"] * (1.0 - m["a"]) + m["s"] * m["a"], d=frame[k], s=cols[k], a=a) for k in range(3)]


def stage(units):
    """The lit background with the units on it: [(art, centre x, scale)], drawn in the order given."""
    bg = light.background_drawn()
    b = next(v for v in light.VARIANTS if v.key == "B")
    dark, warm = light.light_layers(b, light.SHOT_MS)
    tint = [c ** light.GAMMA for c in light.GLOW_TINT[:3]]
    frame = []
    for k, c in enumerate(light.to_linear(bg)):
        y = light.fmap(lambda m: m["c"] * (1.0 - m["d"]), c=c, d=dark)
        frame.append(light.fmap(lambda m: m["y"] * (1.0 - m["w"]) + tint[k] * m["w"], y=y, w=warm))
    for art, cx, scale in units:
        w, h = PLACE_W * scale, PLACE_H * scale
        shadow = Image.new("RGBA", (light.W, light.H), (0, 0, 0, 0))
        sw, sh, sa, lift = SHADOW
        ImageDraw.Draw(shadow).ellipse([cx - sw / 2, FLOOR - lift - sh / 2, cx + sw / 2, FLOOR - lift + sh / 2], fill=(0, 0, 0, round(255 * sa)))
        frame = over(frame, shadow)
        layer = Image.new("RGBA", (light.W, light.H), (0, 0, 0, 0))
        layer.alpha_composite(sampled(art, w, h), (round(cx - w / 2), round(FLOOR - h)))
        frame = over(frame, layer)
    return light.to_srgb(light.vignetted(frame, light.vignette_fixed()))


def font(size):
    return ImageFont.truetype(str(FONT), size)


def sheet(entries, path, gap=20, lab=40, footer=()):
    w = max(im.width for _, im in entries); hs = [im.height for _, im in entries]
    out = Image.new("RGB", (w + 2 * gap, sum(hs) + len(entries) * (lab + gap) + gap + 32 * len(footer) + (16 if footer else 0)), (18, 18, 18))
    d = ImageDraw.Draw(out); y = gap
    for name, im in entries:
        d.text((gap, y), name, font=font(26), fill=(235, 235, 235)); out.paste(im, (gap, y + lab)); y += lab + im.height + gap
    for line in footer:
        d.text((gap, y), line, font=font(21), fill=(190, 190, 190)); y += 32
    out.save(path); print(path.name, out.size)


def main():
    variants = [("now", "지금: 외곽 띠 8px(화면에서 약 2.7px), 눈동자 있음", 8, True),
                ("half", "외곽 띠 1/2: 4px(화면에서 약 1.3px)", 4, True),
                ("half-nopupil", "외곽 띠 1/2 + 몬스터 눈동자 없음", 4, False)]
    stages, bosses, figs = {}, {}, {}
    for key, label, outline, pupils in variants:
        party = [(figure("character", k, outline), x, 1.0) for k, x in reversed(PARTY)]
        enemies = [(figure("enemy", k, outline, pupils), x, 1.0) for k, x in reversed(ENEMIES)]
        stages[key] = stage(party + enemies).crop(STAGE)
        stages[key].save(HERE / f"stage-{key}.png")
        boss = figure("enemy", BOSS[0], outline, pupils)
        bosses[key] = stage([(figure("character", "knight", outline), 777, 1.0), (boss, BOSS[1], BOSS[2])]).crop((620, 84, 1420, 620))
        figs[key] = (party, enemies, boss)
    sheet([(label, stages[key]) for key, label, _, _ in variants], HERE / "review-stage.png",
          footer=["합성 미리보기: 파티 넷과 몬스터 넷을 한 줄에 세운 것(실제 적 무리가 아니다). 배경과 촛불 빛, 그림의 크기와 자리는 게임과 같다.",
                  "그림은 파이프라인의 맞추기를 그대로 돌려 만들었다(지금은 Assets와 같은 파일). 명패·패널은 뺐다."])
    # close-ups at twice the screen size: what the outline and the eyes look like up close
    crops = [("기사·마검사", (470, 200, 900, 520)), ("고블린 약탈자·궁수", (1020, 260, 1450, 520)), ("주술사·쥐", (1400, 260, 1830, 520))]
    entries = []
    for name, box in crops:
        row = Image.new("RGB", (3 * (box[2] - box[0]) * 2 + 40, (box[3] - box[1]) * 2), (18, 18, 18))
        for i, (key, _, _, _) in enumerate(variants):
            part = stages[key].crop((box[0], box[1] - STAGE[1], box[2], box[3] - STAGE[1]))
            row.paste(part.resize((part.width * 2, part.height * 2), Image.NEAREST), (i * (part.width * 2 + 20), 0))
        entries.append((f"{name} (2배 확대): 지금 / 외곽 1/2 / 외곽 1/2 + 눈동자 없음", row))
    sheet(entries, HERE / "review-closeup.png")
    # the monsters' heads as the screen shows them, four times bigger: now / half outline without pupils
    heads = [("고블린 약탈자", stages, (1094, 293 - 84, 1188, 373 - 84)), ("고블린 궁수", stages, (1277, 323 - 84, 1371, 383 - 84)),
             ("고블린 주술사", stages, (1461, 316 - 84, 1548, 394 - 84)), ("동굴 쥐", stages, (1631, 397 - 84, 1731, 457 - 84)),
             ("광산 감독관(1.5배)", bosses, (369, 118, 500, 238))]
    entries = []
    for name, source, box in heads:
        pair = [source[key].crop(box) for key in ("now", "half-nopupil")]
        row = Image.new("RGB", (2 * pair[0].width * 4 + 24, pair[0].height * 4), (18, 18, 18))
        for i, part in enumerate(pair):
            row.paste(part.resize((part.width * 4, part.height * 4), Image.NEAREST), (i * (part.width * 4 + 24), 0))
        entries.append((f"{name}: 지금 / 외곽 1/2 + 눈동자 없음 (화면 크기의 4배)", row))
    sheet(entries, HERE / "review-eyes.png")
    boss_row = Image.new("RGB", (3 * 800 + 40, 536), (18, 18, 18))
    for i, (key, _, _, _) in enumerate(variants):
        boss_row.paste(bosses[key], (i * 820, 0))
    sheet([("보스(광산 감독관, 1.5배): 지금 / 외곽 1/2 / 외곽 1/2 + 눈동자 없음", boss_row)], HERE / "review-boss.png")


if __name__ == "__main__":
    main()
