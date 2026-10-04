"""A fallen mercenary's grave on the battle stage (round 21), no API call.
  .venv/bin/python ArtPipeline/Archive/21-grave/mock_grave.py      (after the three graves were generated: output/prop/)

The proposal: a mercenary who falls leaves a grave (one picture for every mercenary) in the column the death frees. The
living always stand from row 1 without a gap (Design/02 §2: the rows behind a death advance), so the freed column is
always the one right behind the living, and a grave never stands in anybody's way:
- S1, the rearmost falls (Kai, row 4): nobody advances and the grave stands where he fell.
- S2, the frontmost falls (Astrid, row 1): the three behind advance a row each (the plates' row numbers with them) and
  her grave stands in row 4, the column the advance has freed.
Under a grave the fallen's plate stays, dimmed, with the name only (no row number, HP or states).

Drawn over round 19's mockup of the floor 4 battle (Archive/19-motion-test/mock_motion.py: the game's own light, the
same lit stage under everything taken off), with the game's figures. Every party member is taken off the screenshot and
drawn again from its art, so that the units can move to other columns.
Out: mock-S1-<grave>.png for the three graves, mock-S2.png (the recommended grave), mock-compare.png.
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
sys.path.insert(0, str(ROOT / "ArtPipeline/Archive/19-motion-test"))
from mock_motion import (COLUMN, ENEMY_1, ENEMY_2, FLOOR, FONT, PLACE_H, PLACE_W, PLATE_REACH, SHADOW_ALPHA, SHADOW_H,  # noqa: E402
                         SHADOW_UP, SHADOW_W, SHOT_ART, STAGE_BOTTOM, STAGE_TOP, UNIT, W, H, Scene, figure_sprite, fmap,
                         over, plate_of, to_linear, to_srgb)

PROPS = ROOT / "ArtPipeline/output/prop"
GRAVES = [("grave_wood", "A 나무 십자가"), ("grave_stone", "B 돌 묘비 (권장)"), ("grave_cairn", "C 돌무더기 십자가")]
RECOMMENDED = "grave_stone"
STEP = COLUMN + 10                                   # one column to the next
# The middle of each party column. The game stands its party 4 px right of FieldLayout's arithmetic (found on the
# screenshot: Cedric, Ella and Kai all match their art there), so the columns are drawn where the game has them.
CENTRE = {row: 784 - (row - 1) * STEP for row in (1, 2, 3, 4)}
# The party of the screenshot: who stands in which row, and their art. Astrid was lunging there (round 19 finds her).
PARTY = {"cedric": (2, "Job/paladin.png"), "ella": (3, "Job/bishop.png"), "kai": (4, "Job/spellblade.png")}
CROP = (0, STAGE_TOP, 1540, STAGE_BOTTOM)
BADGE = (165, 133, 69)                               # the gold of a plate's row badge
INK = (30, 24, 20)
GRAVE_SHADOW = 0.75                                  # a grave's shadow is narrower than a figure's


def find(shot, art, centre):
    """The x offset and the breathing stretch at which the screenshot shows this art in its place best."""
    best = None
    for stretch in (1.0, 1.01, 1.02):
        h = round(PLACE_H * stretch)
        sprite = art.resize((PLACE_W, h), Image.LANCZOS)
        solid = [(i % PLACE_W, i // PLACE_W) for i, a in enumerate(sprite.getchannel("A").getdata()) if a == 255][::9]
        for dx in range(-8, 9):
            x0 = round(centre - PLACE_W / 2 + dx)
            score = sum(sum(abs(p - q) for p, q in zip(sprite.getpixel((i, j))[:3], shot.getpixel((x0 + i, FLOOR - h + j))))
                        for i, j in solid) / len(solid)
            if best is None or score < best[0]:
                best = (score, dx, stretch)
    return best


def badge(plate):
    """The pixels of the row badge: the gold disc near the plate's top left (the plate's gold rim is left out by the window)."""
    px = plate.load()
    return [(x, y) for y in range(12, 40) for x in range(18, 50)
            if px[x, y][3] > 200 and sum((c - b) ** 2 for c, b in zip(px[x, y][:3], BADGE)) < 30 * 30]


def renumbered(plate, number):
    """The plate with another row number in its badge: the gold disc is painted over and the digit drawn again."""
    out = plate.copy()
    px = out.load()
    pts = badge(out)
    cx, cy = sum(p[0] for p in pts) / len(pts), sum(p[1] for p in pts) / len(pts)
    d = ImageDraw.Draw(out)
    d.ellipse([cx - 7.5, cy - 7.5, cx + 7.5, cy + 7.5], fill=BADGE + (255,))
    f = ImageFont.truetype(str(FONT), 15)
    w = d.textlength(str(number), font=f)
    d.text((cx - w / 2, cy - 9), str(number), fill=INK + (255,), font=f)
    return out


def fallen(plate):
    """The plate under a grave: the row badge, the HP bar and the states painted over with the plate's own blue (the
    middle of the empty right end of its state line), then dimmed. The name stays."""
    out = plate.copy()
    px = out.load()
    samples = sorted((px[x, y][:3] for y in range(74, 86) for x in range(130, 170) if px[x, y][3] > 200), key=sum)
    blue = samples[len(samples) // 2]
    pts = badge(out)
    cx, cy = sum(p[0] for p in pts) / len(pts), sum(p[1] for p in pts) / len(pts)
    d = ImageDraw.Draw(out)
    d.ellipse([cx - 14, cy - 14, cx + 14, cy + 14], fill=blue + (255,))
    d.rectangle([PLATE_REACH + 8, 38, out.width - PLATE_REACH - 8, out.height - 13], fill=blue + (255,))
    r, g, b, a = out.split()
    dim = Image.merge("RGB", (r, g, b)).point(lambda v: int(v * 0.55))
    dim.putalpha(a)
    return dim


class Graves:
    def __init__(self):
        self.scene = Scene()
        shot = self.scene.shot
        stage_srgb = to_srgb(self.scene.stage)
        # Take every party member off (Astrid is off already): their art where it was, their shadows and plates.
        erase = Image.new("L", (W, H), 0)
        d = ImageDraw.Draw(erase)
        self.arts, self.plates = {}, {}
        for name, (row, path) in PARTY.items():
            art = Image.open(SHOT_ART / Path(path).name).convert("RGBA")      # as the screenshot has it
            score, dx, stretch = find(shot, art, CENTRE[row])
            print(f"스크린샷의 {name}: {row}열에서 {dx:+d}, 숨쉬기 {stretch:.2f}, 차이 {score:.1f}")
            h = round(PLACE_H * stretch)
            mask = art.resize((PLACE_W, h), Image.LANCZOS).getchannel("A").point(lambda v: 255 if v > 8 else 0).filter(ImageFilter.MaxFilter(7))
            x = round(CENTRE[row] - PLACE_W / 2 + dx)
            erase.paste(mask, (x, FLOOR - h), mask)
            sx = CENTRE[row] + dx
            d.ellipse([sx - SHADOW_W / 2 - 6, FLOOR - SHADOW_UP - SHADOW_H / 2 - 5, sx + SHADOW_W / 2 + 6, FLOOR - SHADOW_UP + SHADOW_H / 2 + 5], fill=255)
            x0, x1 = round(sx - COLUMN / 2 - PLATE_REACH), round(sx + COLUMN / 2 + PLATE_REACH)
            self.plates[name] = plate_of(shot, stage_srgb, x0, x1)
            d.rectangle([x0, self.plates[name][1][1], x1, self.plates[name][1][3]], fill=255)
            self.arts[name] = figure_sprite(UNIT / path)
        self.plates["astrid"] = self.scene.plates["astrid"]
        self.arts["astrid"] = self.scene.sprites["idle"]
        e = fmap(lambda m: m["e"] / 255.0, e=erase.filter(ImageFilter.GaussianBlur(1.0)).convert("F"))
        self.base = [fmap(lambda m: m["s"] * (1.0 - m["e"]) + m["b"] * m["e"], s=self.scene.base[k], b=self.scene.stage[k], e=e) for k in range(3)]
        self.graves = {key: figure_sprite(PROPS / f"{key}.png") for key, _ in GRAVES}

    def draw(self, standing, grave, fallen_name):
        """standing: {row: name} of the living; the grave stands in the row behind them with the fallen's plate."""
        box = CROP
        f = [c.crop(box) for c in self.base]
        vig = self.scene.vig.crop(box)
        f = self.scene.unit(f, box, vig, ENEMY_2, self.scene.sprites["shaman"], "shaman")
        f = self.scene.unit(f, box, vig, ENEMY_1, self.scene.sprites["raider"], "raider")
        rows = sorted(standing) + [max(standing) + 1]
        for row in reversed(rows):                     # the back row first: a row is drawn over the row behind it
            centre = CENTRE[row]
            if row in standing:
                name = standing[row]
                sprite, (plate, cut) = self.arts[name], self.plates[name]
                plate = renumbered(plate, row)
                shadow_w = 1.0
            else:
                sprite, (plate, cut) = self.graves[grave], self.plates[fallen_name]
                plate = fallen(plate)
                shadow_w = GRAVE_SHADOW
            shadow = self.scene.shadow.resize((round(SHADOW_W * shadow_w), SHADOW_H), Image.LANCZOS)
            f = over(f, shadow, centre - shadow.width / 2, FLOOR - SHADOW_UP - SHADOW_H / 2, vig, box, alpha=SHADOW_ALPHA, black=True)
            f = over(f, sprite, centre - sprite.width / 2, FLOOR - PLACE_H, vig, box)
            f = over(f, plate, centre - plate.width / 2, cut[1], vig, box, vignetted=False)
        return to_srgb(f)


def label(img, text, sub):
    out = Image.new("RGB", (img.width, img.height + 88), (24, 22, 22))
    out.paste(img, (0, 88))
    d = ImageDraw.Draw(out)
    d.text((16, 10), text, fill=(235, 235, 230), font=ImageFont.truetype(str(FONT), 30))
    d.text((16, 52), sub, fill=(170, 172, 180), font=ImageFont.truetype(str(FONT), 21))
    return out


def main():
    graves = Graves()
    s1 = {1: "astrid", 2: "cedric", 3: "ella"}
    s2 = {1: "cedric", 2: "ella", 3: "kai"}
    panels = []
    for key, name in GRAVES:
        img = graves.draw(s1, key, "kai")
        img.save(HERE / f"mock-S1-{key}.png")
        panels.append(label(img.crop((0, 0, 1000, img.height)), f"S1 · {name}", "맨 뒤의 카이(4열)가 쓰러짐: 전진 없음, 그 자리에 무덤과 어두운 이름 명패"))
    img = graves.draw(s2, RECOMMENDED, "astrid")
    img.save(HERE / "mock-S2.png")
    s2_panel = label(img, "S2 · 맨 앞의 아스트리드(1열)가 쓰러짐",
                     "뒤의 셋이 한 칸씩 전진(열 번호도 함께)하고, 무덤은 전진으로 비게 된 4열에. 살아 있는 줄은 늘 1열부터 빈 열 없이 선다")
    print("목업: mock-S1-*.png, mock-S2.png")

    gap = 12
    top_w = sum(p.width for p in panels) + gap * (len(panels) - 1)
    sheet = Image.new("RGB", (max(top_w, s2_panel.width), panels[0].height + gap + s2_panel.height), (12, 12, 12))
    x = 0
    for p in panels:
        sheet.paste(p, (x, 0))
        x += p.width + gap
    sheet.paste(s2_panel, (0, panels[0].height + gap))
    sheet.save(HERE / "mock-compare.png")
    print("목업: mock-compare.png", sheet.size)


if __name__ == "__main__":
    main()
