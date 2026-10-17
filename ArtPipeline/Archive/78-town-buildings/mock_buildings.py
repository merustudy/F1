"""Round 78 (2026-10-17 user: "로비 배경을 더 멀리서 보는 느낌으로 / 건물은 개별 이미지로 가서 레벨업시 점차 커지는 느낌(클래시오브클랜)
/ 업그레이드시 건설 중 느낌으로 소요 일수 / 이미지 api로 생성"): mockups, no API call. The town is pulled back (the approved painting
scaled down, its edges blurred out as the wider grounds), the four buildings are cut from the same painting and pasted back at a
level's size (level 1 small, level 3 big), one of them under construction (scaffold lines and a "건설 중 · N일" badge). The real art is
generated after the judgement: a far "empty grounds" background, a building per level with a transparent background, one scaffold.
  .venv/bin/python ArtPipeline/Archive/78-town-buildings/mock_buildings.py
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter

HERE = Path(__file__).resolve().parent
R71 = HERE.parent / "71-lobby-expedition"
sys.path.insert(0, str(R71))
import mock_lobby as m  # noqa: E402
import mock_town as t  # noqa: E402

W, H = m.W, m.H
PAINT = R71 / "approved/lobby_town_topdown.png"
NOW = Path.home() / "Library/Caches/F1/screenshots/20261017-160500-no-strip/ko_02_lobby_empty.png"
FAR = 0.70                                   # the pull-back: the painting at 70% of the screen width
OX, OY = round((W - W * FAR) / 2), 70        # where the shrunken painting sits (under the header, the yard runs under the strip)
# the buildings on the 1920-wide painting (Round 71 fourth instruction): hotspot and the plate's centre
HOT = {"막사": (20, 260, 800, 700), "쉼터": (815, 160, 1280, 585), "역마차": (1275, 470, 1590, 720), "원정": (1590, 205, 1920, 640)}
PLATE = {"막사": (413, 740), "쉼터": (1046, 628), "역마차": (1432, 760), "원정": (1755, 684)}
LEVEL_SCALE = {1: 0.82, 2: 1.0, 3: 1.22}     # how much bigger a building's picture is at each level (the mock's guess)
EFFECT = {"막사": ("로스터 상한", (8, 10, 12)), "쉼터": ("치료 피로", (-40, -60, -80)), "역마차": ("후보", (3, 4, 5))}
UPGRADE = {2: (40, 3), 3: (80, 5)}           # gold, days — starting values for the sim to settle
SCAFFOLD = (0x5A, 0x44, 0x2C); PLANK = (0x7A, 0x5E, 0x3C)


def painting_1920():
    return Image.open(PAINT).convert("RGB").resize((W, round(W * 1536 / 2304)), Image.LANCZOS)


def far_base(p):
    """The wider grounds: the painting blurred and darkened fills the screen, the painting itself sits smaller in the middle."""
    base = p.resize((W, round(p.height)), Image.LANCZOS).crop((0, 0, W, H)).filter(ImageFilter.GaussianBlur(14))
    base = Image.eval(base, lambda v: round(v * 0.55))
    img = base.convert("RGBA")
    small = p.resize((round(W * FAR), round(p.height * FAR)), Image.LANCZOS)
    mask = Image.new("L", small.size, 0); md = ImageDraw.Draw(mask)
    f = 70
    md.rectangle([f, f, small.width - f, small.height - f], fill=255)
    mask = mask.filter(ImageFilter.GaussianBlur(40))
    img.paste(small, (OX, OY), mask)
    return img


def far_xy(x, y):
    return OX + x * FAR, OY + y * FAR


def ground_patch(p, box):
    """Bare ground to put under a building that got smaller: the yard's dirt, stretched over the building's box."""
    x0, y0, x1, y1 = box
    # the same spot blurred and a little darker: the colours stay (bushes, dirt), the building melts into a vague plot
    spot = p.crop((x0, y0, x1, y1)).filter(ImageFilter.GaussianBlur(22))
    return Image.eval(spot, lambda v: round(v * 0.8))


def feather(size, f=26):
    mask = Image.new("L", size, 0); d = ImageDraw.Draw(mask)
    d.rectangle([f, f, size[0] - f, size[1] - f], fill=255)
    return mask.filter(ImageFilter.GaussianBlur(f * 0.6))


def place_building(img, p, name, level, hover=False):
    """The building cut from the painting, scaled by its level, standing on the same ground line (bottom-centre anchored)."""
    x0, y0, x1, y1 = HOT[name]
    fx0, fy0 = far_xy(x0, y0); fx1, fy1 = far_xy(x1, y1)
    k = LEVEL_SCALE[level]
    if k < 1:
        dirt = ground_patch(p, (x0, y0, x1, y1)).resize((round(fx1 - fx0), round(fy1 - fy0)), Image.LANCZOS)
        img.paste(dirt, (round(fx0), round(fy0)), feather(dirt.size, 44))
    crop = p.crop((x0, y0, x1, y1))
    w, h = round((x1 - x0) * FAR * k), round((y1 - y0) * FAR * k)
    crop = crop.resize((w, h), Image.LANCZOS)
    cx, by = (fx0 + fx1) / 2, fy1
    px, py = round(cx - w / 2), round(by - h)
    img.paste(crop, (px, py), feather(crop.size, 22))
    if hover:
        wash = Image.new("RGBA", img.size, (0, 0, 0, 0)); wd = ImageDraw.Draw(wash)
        wd.rectangle([px, py, px + w, py + h], fill=(255, 220, 150, 28), outline=m.BRASS + (255,), width=3)
        img.alpha_composite(wash)
    return (px, py, px + w, py + h)


def scaffold(img, box, days_left=3, days=3):
    """Under construction: poles and planks over the building, a plank pile, and the badge with the days."""
    x0, y0, x1, y1 = box
    lay = Image.new("RGBA", img.size, (0, 0, 0, 0)); d = ImageDraw.Draw(lay)
    d.rectangle([x0, y0, x1, y1], fill=(10, 8, 6, 90))
    for x in (x0 + 8, (x0 + x1) / 2, x1 - 8):
        d.line([(x, y0 - 30), (x, y1)], fill=SCAFFOLD + (255,), width=9)
    for i in range(3):
        y = y0 + (y1 - y0) * (0.2 + i * 0.3)
        d.line([(x0 - 10, y), (x1 + 10, y)], fill=PLANK + (255,), width=7)
        d.line([(x0 + 8, y), ((x0 + x1) / 2, y - (y1 - y0) * 0.3)], fill=SCAFFOLD + (220,), width=5)
    # a plank pile by the door and a crane arm
    d.rectangle([x1 - 90, y1 - 26, x1 - 10, y1 - 4], fill=PLANK + (255,))
    d.rectangle([x1 - 80, y1 - 46, x1 - 20, y1 - 26], fill=SCAFFOLD + (255,))
    d.line([(x0 + 8, y0 - 30), (x0 + (x1 - x0) * 0.6, y0 - 90)], fill=SCAFFOLD + (255,), width=8)
    d.line([(x0 + (x1 - x0) * 0.6, y0 - 90), (x0 + (x1 - x0) * 0.6, y0 + 10)], fill=(0x90, 0x8A, 0x80, 255), width=3)
    d.rectangle([x0 + (x1 - x0) * 0.6 - 16, y0 + 10, x0 + (x1 - x0) * 0.6 + 16, y0 + 36], fill=PLANK + (255,))
    img.alpha_composite(lay)


def badge(d, cx, cy, days_left, days, label="건설 중"):
    """The plate's second line as a badge: "건설 중 · 3일 남음" over a thin progress bar."""
    w, h = 300, 62
    d.rectangle([cx - w / 2, cy - h / 2, cx + w / 2, cy + h / 2], fill=t.PLATE, outline=m.BRASS, width=2)
    m.text(d, (cx, cy - 10), f"{label} · {days_left}일 남음", 22, m.BRASS, "mm")
    bx0, bx1, by = cx - w / 2 + 16, cx + w / 2 - 16, cy + 16
    d.rectangle([bx0, by, bx1, by + 8], fill=m.SLOT)
    d.rectangle([bx0, by, bx0 + (bx1 - bx0) * (days - days_left) / days, by + 8], fill=m.BRASS)


def plates(d, levels, hover=None, construction=None):
    for name, (cx, cy) in PLATE.items():
        fx, fy = far_xy(cx, cy)
        lv = levels.get(name)
        sub = None
        if name in construction if construction else False:
            sub = None
        t.plate(d, fx, fy + 10, name if lv is None else f"{name}  Lv {lv}", line=m.BRASS if hover == name else m.LINE)
        if construction and name in construction:
            left, total = construction[name]
            badge(d, fx, fy + 70, left, total)


def town_far(levels, hover=None, construction=None, bubble=None):
    p = painting_1920()
    img = far_base(p)
    boxes = {}
    # back to front so the nearer buildings overlap the farther ones
    for name in ("원정", "쉼터", "역마차", "막사"):
        boxes[name] = place_building(img, p, name, levels.get(name, 2), hover=(hover == name))
    if construction:
        for name, (left, total) in construction.items():
            scaffold(img, boxes[name], left, total)
    d = ImageDraw.Draw(img)
    plates(d, levels, hover, construction)
    m.header(img, "용병단 본부"); d = ImageDraw.Draw(img)
    if bubble:
        name, lines = bubble
        x0, y0, x1, y1 = boxes[name]
        t.plate(d, (x0 + x1) / 2, y0 - 50, lines[0], 24, sub=lines[1], line=m.BRASS)
    t.strip(d)
    return img


def window_upgrade(img):
    """The barracks window with the building's level line and the [업그레이드] button (the mock of what the window gains)."""
    shade = Image.new("RGBA", (W, H), (0, 0, 0, 150)); shade.paste((0, 0, 0, 0), (0, 0, W, 100)); img.alpha_composite(shade)
    d = ImageDraw.Draw(img)
    m.panel(d, (40, 130, 1880, 1040), m.PANEL, outline=m.BRASS, width=2)
    m.text(d, (64, 170), "막사 — 로스터", 30, m.DIM, "lm")
    m.button(d, (1700, 146, 1856, 196), "닫기", m.QUIET, 22)
    # the building's line under the title: level, what the next level gives, the price and the days, and the command
    m.panel(d, (64, 206, 1856, 262), m.PANEL_LIGHT)
    m.text(d, (84, 234), "막사 Lv 1", 26, m.BONE, "lm")
    m.text(d, (230, 234), "로스터 상한 8  →  Lv 2: 10   ·   금 40   ·   3일", 24, m.DIM, "lm")
    m.button(d, (1560, 213, 1836, 255), "업그레이드  금 40 · 3일", m.BUTTON, 22)
    for i, mm in enumerate(m.ROSTER):
        y = 276 + i * 116
        m.roster_line(d, 64, y, mm, rows=False, level=False)
        x = 64 + 700
        m.text(d, (x, y + 40), f"Lv {mm[2]}", 30, m.BONE, "lm"); m.text(d, (x + 110, y + 42), f"경험치 {mm[3]}/{mm[4]}", 20, m.DIM, "lm")
        m.exp_bar(d, x, y + 66, 300, mm[3], mm[4])
        m.button(d, (1700, y + 36, 1856, y + 80), "내보내기", m.QUIET, 20, m.DIM)
    m.text(d, (64, 1005), "금 120 · 로스터 6/8 · 사망 0/15 · 버려진 광산 클리어 1회 · 심연 잠김(광산 2회)", 22, m.DIM, "lm")
    return img


def level_sheet():
    """One building at the three levels and under construction, side by side, at the mock's scale."""
    p = painting_1920()
    x0, y0, x1, y1 = HOT["막사"]
    crop = p.crop((x0, y0, x1, y1))
    cells = []
    for lv in (1, 2, 3):
        k = LEVEL_SCALE[lv] * 0.5
        cells.append((f"막사 Lv {lv}", crop.resize((round((x1 - x0) * k), round((y1 - y0) * k)), Image.LANCZOS)))
    cw, ch = 520, 330
    out = Image.new("RGBA", (cw * 4 + 100, ch + 140), m.BG + (255,)); d = ImageDraw.Draw(out)
    for i, (name, im) in enumerate(cells):
        cx = 50 + i * cw + cw / 2
        out.paste(im, (round(cx - im.width / 2), round(ch - im.height) + 20))
        m.text(d, (cx, ch + 60), name, 28, m.TEXT, "mm")
        m.text(d, (cx, ch + 96), f"{EFFECT['막사'][0]} {EFFECT['막사'][1][i]}" + (f" · 금 {UPGRADE[i + 1][0]} · {UPGRADE[i + 1][1]}일" if i else " (시작)"), 20, m.DIM, "mm")
    # the construction cell: level 1 with the scaffold, the badge
    i = 3; cx = 50 + i * cw + cw / 2
    im = cells[0][1]
    px, py = round(cx - im.width / 2), round(ch - im.height) + 20
    out.paste(im, (px, py))
    scaffold(out, (px, py, px + im.width, py + im.height))
    d = ImageDraw.Draw(out)
    m.text(d, (cx, ch + 60), "막사 Lv 1 → 2 건설 중", 28, m.TEXT, "mm")
    badge(d, cx, ch + 110, 2, 3)
    return out.convert("RGB")


def main():
    now = Image.open(NOW).convert("RGB")
    outs = {}
    # A: pulled back, every building at level 1 (a fresh run), the stagecoach under the pointer
    outs["A-far-lv1"] = town_far({"막사": 1, "쉼터": 1, "역마차": 1}, hover="역마차",
                                 bubble=("역마차", ("역마차 Lv 1", "후보 3명 · 고용 금 20 · 업그레이드 금 40"))).convert("RGB")
    # A at level 3: the buildings grown
    outs["A-far-lv3"] = town_far({"막사": 3, "쉼터": 3, "역마차": 3}).convert("RGB")
    # construction: the barracks going 1 → 2 with 2 of 3 days left
    outs["A-construction"] = town_far({"막사": 1, "쉼터": 2, "역마차": 1}, construction={"막사": (2, 3)}, hover="막사",
                                      bubble=("막사", ("막사 Lv 1 → 2 건설 중", "2일 남음 · 그동안도 들어갈 수 있다"))).convert("RGB")
    # the window with the upgrade line
    outs["A-window"] = window_upgrade(town_far({"막사": 1, "쉼터": 1, "역마차": 1})).convert("RGB")
    for k, im in outs.items():
        im.save(HERE / f"mock-{k}.png"); print(f"mock-{k}.png")
    level_sheet().save(HERE / "mock-levels.png"); print("mock-levels.png")
    m.sheet([("지금: 마을 그림 한 장, 건물은 그림 위의 핫스팟", now),
             ("A: 더 멀리서 (그림 70%, 둘레는 흐린 터) · 건물 Lv 1 (작게) · 역마차에 마우스", outs["A-far-lv1"]),
             ("A: 건물 Lv 3 (크게, 장식이 늘어난 모습의 자리)", outs["A-far-lv3"]),
             ("A: 막사 Lv 1 → 2 건설 중 (비계 + 이름표 밑의 배지 '건설 중 · 2일 남음')", outs["A-construction"])],
            2, HERE / "mock-compare.png",
            footer="목업: 건물은 지금 그림에서 잘라 레벨대로 키운 것(Lv 1 82% · Lv 2 100% · Lv 3 122%)이고 터는 흐린 자리 표시다. 진짜 그림은 판정 뒤 생성: 먼 시점의 빈 터 1장 + 건물 3곳 × 3레벨(투명) + 비계 1장.")


if __name__ == "__main__":
    main()
