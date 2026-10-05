"""Proposal 2 chosen (Slay the Spire's way, round 29): where the name and the row go, and the stage lowered, no API call.
  ArtPipeline/Archive/29-plates/run_lowered_shots.sh <scratch> 0 7             (the game's screenshots, plates hidden, seed 7)
  ArtPipeline/Archive/29-plates/run_lowered_shots.sh <scratch> 48 7            (the same with the stage 48 lower)
  PLATES=1 ArtPipeline/Archive/29-plates/run_lowered_shots.sh <scratch> 0 7    (the same battle as the game shows it now)
  copied into game/ as ko_15_s7_<noplate|lowered|now>.png and ko_18_s7_<noplate|lowered|now>.png, then
  .venv/bin/python ArtPipeline/Archive/29-plates/mock_plates3.py   # mock3-names.png, mock3-lowered-15.png, mock3-lowered-18.png

The user's answer to the second proposal: "1. 2안으로 2. 두가지 목업 제공. 보고나서 결정 3. 캐릭터와 무대 내리는 버전 목업 제공".
- Names A: the marks of proposal 2 under everyone; the unit the mouse is on (or that is pressed) shows a slim iron strip with
  its row (the stud) and name under its marks (Cedric here; the arrow stands for the mouse).
- Names B: the same marks; each board of the panel below has a header with the row and the name. The boards move 12 down
  (the panel's top margin 14 becomes 2 and the header takes 22: seven cells, 432, still fit in the panel's 460).
- Lowered: the game's own screenshot with the field (figures, background, floating words) 48 lower, the marks under the
  lowered feet, against the same battle with the stage where it is now. A new run's seed comes from the OS, so the shots
  are all rendered with the seed 7 (the first battle's enemies are otherwise different from shot to shot).
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import mock_plates2 as P  # noqa: E402

Z = P.Z
DOWN = 48
GAME = HERE / "game"
UNITS = {name: [(cx, unit, key) for key in ("party", "enemy", "danger") for cx, unit in P.SCENES[name][key]] for name in P.SCENES}
# The boards of the panel stand under the units' columns where the units stand when they have walked in (ko_15's goblins are
# still walking in: their marks are where they are, their boards where they will stand).
BOARDS_15 = [(125, 4, "카이"), (315, 3, "엘라"), (505, 2, "세드릭"), (695, 1, "아스트리드"), (1045, 1, "고블린 약탈자"), (1235, 2, "고블린 주술사")]


def shot(name, kind):
    return Image.open(GAME / f"{name}_s7_{kind}.png").convert("RGBA")


def marks(base, units, floor):
    """Proposal 2's marks under every unit of the scene, with the floor line at `floor`."""
    P.FLOOR = floor
    layer = Image.new("RGBA", (base.width * Z, base.height * Z), (0, 0, 0, 0))
    for cx, unit, state in units:
        P.draw_unit(layer, 2, cx * Z, unit, state)
    out = base.copy()
    out.alpha_composite(layer.resize(base.size, Image.LANCZOS))
    P.FLOOR = 506
    return out


def strip(layer, box, row, name, size):
    """A slim iron strip with the row on a gold stud and the name (at twice the size)."""
    x0, y0, x1, y1 = box
    h = y1 - y0
    layer.alpha_composite(P.three(Image.open(P.UI / "ui_frame/tag29_slim.png").convert("RGBA"), (x1 - x0, h)), (x0, y0))
    stud = h - 4 * Z
    layer.alpha_composite(Image.open(P.UI / "ui_piece/badge29_a_stud.png").convert("RGBA").resize((stud, stud), Image.LANCZOS), (x0 + 3 * Z, y0 + 2 * Z))
    d = ImageDraw.Draw(layer)
    P.word(d, (x0 + 3 * Z + stud // 2, y0 + h // 2), str(row), round(stud * 0.62), P.INK, "mm", stroke=0)
    P.word(d, (x0 + 3 * Z + stud + 6 * Z, y0 + h // 2), name, size * Z, P.TEXT)


def cursor(layer, x, y):
    """The mouse: a plain arrow (bone with an ink outline)."""
    pts = [(0, 0), (0, 26), (7, 20), (12, 31), (17, 29), (12, 18), (21, 18)]
    ImageDraw.Draw(layer).polygon([(x + px * Z, y + py * Z) for px, py in pts], fill=(240, 236, 226), outline=P.INK, width=2 * Z)


def over(base, draw):
    layer = Image.new("RGBA", (base.width * Z, base.height * Z), (0, 0, 0, 0))
    draw(layer)
    base.alpha_composite(layer.resize(base.size, Image.LANCZOS))
    return base


def names_a():
    base = marks(shot("ko_15", "noplate"), UNITS["ko_15"], 506)
    cx, top = 595, 506 + 56

    def draw(layer):
        strip(layer, ((cx - 76) * Z, top * Z, (cx + 76) * Z, (top + 28) * Z), 2, "세드릭", 16)
        cursor(layer, (cx + 34) * Z, 430 * Z)
    return over(base, draw).convert("RGB")


def names_b():
    base = marks(shot("ko_15", "noplate"), UNITS["ko_15"], 506)
    out = base.copy()
    for x0, _, _ in BOARDS_15:
        out.alpha_composite(base.crop((x0 - 8, 632, x0 + 188, 1066)), (x0 - 8, 644))

    def draw(layer):
        for x0, row, name in BOARDS_15:
            strip(layer, ((x0 + 1) * Z, 622 * Z, (x0 + 179) * Z, 644 * Z), row, name, 14)
    return over(out, draw).convert("RGB")


def band(image, box, title, now=False):
    crop = image.crop(box)
    out = Image.new("RGB", (crop.width, crop.height + 44), (20, 18, 18))
    out.paste(crop, (0, 44))
    ImageDraw.Draw(out).text((14, 8), title, fill=(235, 235, 230) if now else (217, 164, 65), font=P.font(26))
    return out


def stack(bands, path):
    sheet = Image.new("RGB", (max(b.width for b in bands), sum(b.height + 8 for b in bands)), (12, 12, 12))
    y = 0
    for b in bands:
        sheet.paste(b, (0, y))
        y += b.height + 8
    sheet.save(HERE / path)
    return sheet.size


def main():
    a, b = names_a(), names_b()
    a.save(HERE / "mock3-names-a.png")
    b.save(HERE / "mock3-names-b.png")
    box = (0, 84, 1920, 790)
    print("mock3-names.png", stack([band(shot("ko_15", "now").convert("RGB"), box, "지금 — 명패에 열·이름·체력·상태", True),
                                    band(a, box, "이름 A — 2안. 마우스를 올리거나 누르면 그 유닛의 표시 아래에 열과 이름(세드릭)"),
                                    band(b, box, "이름 B — 2안. 아래 패널의 보드마다 머리에 열과 이름(보드는 12 아래로)")], "mock3-names.png"))
    for name, title in (("ko_15", "4층 전투"), ("ko_18", "빈사")):
        now = marks(shot(name, "noplate"), UNITS[name], 506).convert("RGB")
        lowered = marks(shot(name, "lowered"), UNITS[name], 506 + DOWN).convert("RGB")
        now.save(HERE / f"mock3-{name}-2.png")
        lowered.save(HERE / f"mock3-{name}-2-lowered.png")
        box = (0, 84, 1920, 720)
        print(f"mock3-lowered-{name[3:]}.png", stack([band(shot(name, "now").convert("RGB"), box, f"{title} — 지금", True),
                                                      band(now, box, f"{title} — 2안, 캐릭터와 무대는 지금 자리"),
                                                      band(lowered, box, f"{title} — 2안, 캐릭터와 무대를 {DOWN} 내림")], f"mock3-lowered-{name[3:]}.png"))


if __name__ == "__main__":
    main()
