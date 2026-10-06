"""The review sheet of round 37 (the valkyrie's breakdown and virtue busts). No API call.
  .venv/bin/python ArtPipeline/Archive/37-valkyrie-states/make_review.py [--game <ko_38_battle_breakdown.png>]

Top band: the approved figure and the two raw busts, large, on the stage's colour. Under it: the banner of the battle screen
(UiPrefabSetup.Battle: 1920x84 band, lines 2, words 40) drawn with PIL in its two colours, with the bust in the picture slot
left of the words at three sizes (64 inside the band; 96 and 120 reaching over its edges, as a portrait does), the whole bust
and the head alone. With --game, the same slots are also drawn on the real screenshot of the breakdown.
Writes review-busts.png (and review-game.png).
"""
import argparse
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
REPO = HERE.parents[2]
sys.path.insert(0, str(REPO / "ArtPipeline" / "tools"))
from gen_image import subject_box  # noqa: E402

FONT = REPO / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"
APPROVED = REPO / "ArtPipeline/Archive/20-serious-face/approved/character/valkyrie.raw.png"
OUT = REPO / "ArtPipeline/output/character"
STAGE, INK, TEXT, DIM = (58, 46, 36), (24, 9, 7), (235, 235, 230), (154, 160, 172)
AFFLICTION, VIRTUE = (200, 75, 110), (247, 200, 74)   # UiPalette.FatigueDanger, UiPalette.Virtue
BAND_TOP, BAND_H, LINE = 310, 84, 2
BAND_ALPHA, VEIL_ALPHA = 0.85, 0.7


def font(size):
    return ImageFont.truetype(str(FONT), size)


def subject(path):
    image = Image.open(path).convert("RGBA")
    return image.crop(subject_box(image))


def head(bust):
    """A square around the head: as wide as the bust's upper part, from the top of the hair."""
    side = int(bust.height * 0.62)
    left = max(0, bust.width // 2 - side // 2 + int(bust.width * 0.06))
    return bust.crop((left, 0, min(bust.width, left + side), side))


def fitted(image, size):
    scale = min(size / image.width, size / image.height)
    return image.resize((max(1, round(image.width * scale)), max(1, round(image.height * scale))), Image.LANCZOS)


def outlined(image, width=2):
    """A dark ring around the silhouette, as the figures get on the stage (gen_image's outline, coarsely)."""
    from PIL import ImageFilter
    alpha = image.getchannel("A")
    ring = alpha.filter(ImageFilter.MaxFilter(2 * width + 1))
    back = Image.new("RGBA", image.size, INK + (0,))
    back.putalpha(ring)
    back.alpha_composite(image)
    return back


def band(words, color, bust, bust_name, scale=1.0, over=None):
    """The banner on a strip of the stage (or over a screenshot strip), with the bust in the slot at three sizes."""
    width = 1920
    strip = Image.new("RGBA", (width, 260), STAGE + (255,))
    if over is not None:
        strip.paste(over.resize((width, 260)), (0, 0))
    veil = Image.new("RGBA", strip.size, (0, 0, 0, int(255 * (1 - (1 - VEIL_ALPHA) ** (1 / 2.2)) * 0.6)))
    strip.alpha_composite(veil)
    top = (260 - BAND_H) // 2
    draw = ImageDraw.Draw(strip, "RGBA")
    draw.rectangle((0, top, width, top + BAND_H), fill=INK + (int(255 * BAND_ALPHA),))
    draw.rectangle((0, top, width, top + LINE), fill=color)
    draw.rectangle((0, top + BAND_H - LINE, width, top + BAND_H), fill=color)
    f = font(40)
    text_w = draw.textlength(words, font=f)

    # Three slots: the bust (and the head alone) at 64 inside the band, 96 and 120 over its edges; the words after each.
    x = 90
    for size, label in ((64, "64 안"), (96, "96 걸침"), (120, "120 걸침")):
        for picture, kind in ((bust, "전신"), (head(bust), "머리")):
            piece = outlined(fitted(picture, size - 4))
            y = top + BAND_H // 2 - piece.height // 2
            strip.alpha_composite(piece, (x, y))
            draw.text((x, top + BAND_H + 8 + (18 if kind == "머리" else 0)), f"{label} · {kind}", font=font(16), fill=DIM)
            x += size + 26
        x += 40
    draw.text((x, top + BAND_H // 2 - 26), words, font=f, fill=color, stroke_width=2, stroke_fill=INK)
    return strip


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--game", default="", help="The breakdown screenshot (ko_38_battle_breakdown.png) to lay the slots on.")
    args = parser.parse_args()

    approved = subject(APPROVED)
    afflicted1 = subject(OUT / "valkyrie_afflicted.raw.png")
    afflicted = subject(OUT / "valkyrie_afflicted2.raw.png")
    virtue = subject(OUT / "valkyrie_virtue.raw.png")

    sheet = Image.new("RGBA", (1920, 700 + 2 * 290), STAGE + (255,))
    draw = ImageDraw.Draw(sheet)
    draw.text((30, 20), "Round 37 — 발키리의 붕괴(고통)·각성 초상. 확정 원본 / 붕괴 1차(땀) / 붕괴 2차(무너진 정신, 땀 없음) / 각성. (원본 그대로, 같은 높이)", font=font(26), fill=TEXT)
    x = 40
    for image, label in ((approved, "확정 원본"), (afflicted1, "붕괴 1차 · afflicted"), (afflicted, "붕괴 2차 · afflicted2 (재생성)"), (virtue, "각성 · virtue")):
        big = fitted(image, 540)
        sheet.alpha_composite(big, (x, 100 + (540 - big.height) // 2))
        draw.text((x, 676), label, font=font(22), fill=DIM)
        x += big.width + 36

    sheet.alpha_composite(band("아스트리드 — 공포에 빠졌다", AFFLICTION, afflicted, "afflicted"), (0, 710))
    sheet.alpha_composite(band("아스트리드 — 집중의 각성", VIRTUE, virtue, "virtue"), (0, 1000))
    sheet.convert("RGB").save(HERE / "review-busts.png")
    print("review-busts.png")

    if args.game:
        shot = Image.open(args.game).convert("RGBA")
        strip = shot.crop((0, BAND_TOP + BAND_H // 2 - 130, 1920, BAND_TOP + BAND_H // 2 + 130))
        game = Image.new("RGBA", (1920, 2 * 290), STAGE + (255,))
        game.alpha_composite(band("아스트리드 — 공포에 빠졌다", AFFLICTION, afflicted, "afflicted", over=strip), (0, 0))
        game.alpha_composite(band("아스트리드 — 집중의 각성", VIRTUE, virtue, "virtue", over=strip), (0, 290))
        game.convert("RGB").save(HERE / "review-game.png")
        print("review-game.png")


if __name__ == "__main__":
    main()
