"""Round 71, second instruction: the lobby as a town — a painted background with the quarters as buildings you click to enter
(Darkest Dungeon's Hamlet, borrowed). The painting is a stand-in drawn with shapes (no API call); the real one is one generation
call after the user approves the layout. Three layouts and the look of a building entered:
  mock-town-1.png  the town with a thin roster strip at the bottom and the gate button (recommended)
  mock-town-2.png  the town alone: buildings and the gate, the roster lives in the barracks
  mock-town-3.png  the town with a status card floating over every building
  mock-town-enter.png  the barracks entered: the town dims and its panel opens, [닫기] goes back
  .venv/bin/python ArtPipeline/Archive/71-lobby-expedition/mock_town.py
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import mock_lobby as m  # noqa: E402

W, H = m.W, m.H
SKY_TOP = (0x0E, 0x10, 0x18); SKY_LOW = (0x2C, 0x26, 0x30); HILL = (0x14, 0x15, 0x1C); HILL2 = (0x1A, 0x1A, 0x22)
GROUND = (0x1C, 0x19, 0x16); ROAD = (0x30, 0x2B, 0x25); WALL = (0x24, 0x22, 0x24); WALL_LIGHT = (0x33, 0x2F, 0x30); ROOF = (0x1A, 0x16, 0x18)
WINDOW = (0xE8, 0xA8, 0x50); WINDOW_DIM = (0x8A, 0x62, 0x30); LANTERN = (0xFF, 0xCE, 0x68); MOON = (0xD8, 0xD2, 0xC0)
LABEL = (0, 0, 0); PLATE = (0x0E, 0x0D, 0x0C)


def sky(img):
    d = ImageDraw.Draw(img)
    for y in range(100, 640):
        t = (y - 100) / 540
        c = tuple(round(SKY_TOP[i] + (SKY_LOW[i] - SKY_TOP[i]) * t) for i in range(3))
        d.line([(0, y), (W, y)], fill=c)
    # the moon and its glow
    glow = Image.new("RGBA", (W, H), (0, 0, 0, 0)); gd = ImageDraw.Draw(glow)
    gd.ellipse([1440, 150, 1580, 290], fill=MOON + (60,))
    glow = glow.filter(ImageFilter.GaussianBlur(30)); img.alpha_composite(glow)
    d.ellipse([1480, 190, 1540, 250], fill=MOON)
    # far hills and a chapel on the ridge
    d.polygon([(0, 640), (0, 560), (240, 500), (520, 540), (760, 470), (1040, 520), (1300, 460), (1560, 520), (1920, 480), (1920, 640)], fill=HILL)
    d.polygon([(0, 640), (0, 600), (320, 570), (640, 600), (900, 560), (1200, 600), (1500, 570), (1920, 610), (1920, 640)], fill=HILL2)
    d.polygon([(1120, 560), (1120, 470), (1150, 470), (1150, 430), (1160, 420), (1170, 430), (1170, 470), (1200, 470), (1200, 560)], fill=HILL)
    d.line([(1160, 410), (1160, 395)], fill=HILL, width=3); d.line([(1152, 402), (1168, 402)], fill=HILL, width=3)
    # the ground and the road out to the gate
    d.rectangle([0, 640, W, H], fill=GROUND)
    d.polygon([(0, 1000), (0, 940), (500, 760), (1100, 720), (1680, 700), (1920, 690), (1920, 760), (1700, 770), (1150, 800), (560, 860), (0, 1000)], fill=ROAD)


def building(d, box, roof=True, windows=(), light=WINDOW, outline=None, banner=False, chimney=False):
    x0, y0, x1, y1 = box
    d.rectangle([x0, y0, x1, y1], fill=WALL, outline=outline, width=4 if outline else 0)
    d.rectangle([x0, y0, x0 + 10, y1], fill=WALL_LIGHT)
    if roof:
        d.polygon([(x0 - 16, y0), (x1 + 16, y0), ((x0 + x1) / 2, y0 - (x1 - x0) * 0.32)], fill=ROOF, outline=outline, width=4 if outline else 0)
    if chimney:
        d.rectangle([x1 - 60, y0 - 70, x1 - 36, y0 + 4], fill=ROOF)
        for i in range(3):
            d.ellipse([x1 - 60 - i * 8, y0 - 110 - i * 30, x1 - 30 + i * 8, y0 - 80 - i * 30], fill=(0x3A, 0x36, 0x3A))
    for (wx, wy, ww, wh) in windows:
        d.rectangle([wx, wy, wx + ww, wy + wh], fill=light)
        d.line([(wx + ww / 2, wy), (wx + ww / 2, wy + wh)], fill=ROOF, width=2); d.line([(wx, wy + wh / 2), (wx + ww, wy + wh / 2)], fill=ROOF, width=2)
    if banner:
        d.rectangle([x0 + 40, y0 - 60, x0 + 46, y1], fill=(0x40, 0x3A, 0x34))
        d.polygon([(x0 + 46, y0 - 56), (x0 + 110, y0 - 56), (x0 + 96, y0 - 30), (x0 + 110, y0 - 4), (x0 + 46, y0 - 4)], fill=m.DANGER)


def lantern(d, x, y, glow_img=None):
    d.line([(x, y), (x, y + 90)], fill=(0x3A, 0x36, 0x3A), width=6)
    d.rectangle([x - 10, y - 24, x + 10, y], fill=LANTERN)
    if glow_img is not None:
        g = ImageDraw.Draw(glow_img); g.ellipse([x - 70, y - 90, x + 70, y + 50], fill=LANTERN + (70,))


def coach(d, x, y, outline=None):
    d.rectangle([x, y, x + 200, y + 90], fill=WALL, outline=outline, width=4 if outline else 0)
    d.rectangle([x + 20, y + 14, x + 70, y + 50], fill=WINDOW_DIM); d.rectangle([x + 110, y + 14, x + 160, y + 50], fill=WINDOW_DIM)
    d.polygon([(x - 10, y), (x + 210, y), (x + 200, y - 18), (x, y - 18)], fill=ROOF)
    for wx in (x + 40, x + 160):
        d.ellipse([wx - 28, y + 70, wx + 28, y + 126], fill=ROOF, outline=WALL_LIGHT, width=6)
    d.line([(x + 200, y + 60), (x + 280, y + 40)], fill=WALL_LIGHT, width=6)


def gate(d, x, y, outline=None):
    d.rectangle([x, y, x + 60, y + 200], fill=WALL, outline=outline, width=4 if outline else 0)
    d.rectangle([x + 180, y, x + 240, y + 200], fill=WALL, outline=outline, width=4 if outline else 0)
    d.polygon([(x - 10, y), (x + 250, y), (x + 250, y - 30), (x - 10, y - 30)], fill=ROOF, outline=outline, width=4 if outline else 0)
    d.rectangle([x + 60, y + 20, x + 180, y + 200], fill=(0x3E, 0x36, 0x40))
    # a signpost
    d.line([(x + 290, y + 60), (x + 290, y + 200)], fill=(0x40, 0x3A, 0x34), width=8)
    d.polygon([(x + 250, y + 70), (x + 380, y + 70), (x + 400, y + 90), (x + 380, y + 110), (x + 250, y + 110)], fill=m.BONE)
    m.text(d, (x + 318, y + 90), "원정", 22, m.PANEL, "mm")


def plate(d, cx, cy, s, px=26, pad=14, color=m.TEXT, fill=PLATE, line=m.BRASS, sub=None):
    w = max(m.textw(s, px), m.textw(sub, px - 6) if sub else 0) + pad * 2; h = px + 20 + (px - 2 if sub else 0)
    d.rectangle([cx - w / 2, cy - h / 2, cx + w / 2, cy + h / 2], fill=fill, outline=line, width=2)
    m.text(d, (cx, cy - (px / 2 if sub else 0)), s, px, color, "mm")
    if sub:
        m.text(d, (cx, cy + px / 2 + 2), sub, px - 6, m.DIM, "mm")


# the quarters: where each stands and what the label says
BARRACKS = (230, 440, 620, 640); INN = (770, 380, 1080, 640); COACH_XY = (1190, 520); GATE_XY = (1480, 430)
QUARTERS = {"막사": (425, 690), "쉼터": (925, 690), "역마차": (1330, 690), "원정": (1700, 690)}
STATUS = {"막사": "로스터 6/8 · 공포 1", "쉼터": "피로 100 넘은 용병 1", "역마차": "후보 3 · 고용 금 30", "원정": "광산 · 심연은 잠김"}


def town(hover=None, labels=True):
    img = Image.new("RGBA", (W, H), m.BG + (255,))
    sky(img)
    glow = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    hl = m.BRASS
    # barracks: a long hall with a banner
    bx0, by0, bx1, by1 = BARRACKS
    building(d, BARRACKS, windows=[(bx0 + 60 + i * 90, by0 + 60, 44, 56) for i in range(4)], outline=hl if hover == "막사" else None, banner=True)
    # inn: two storeys and a chimney
    ix0, iy0, ix1, iy1 = INN
    building(d, INN, windows=[(ix0 + 40, iy0 + 40, 46, 56), (ix0 + 140, iy0 + 40, 46, 56), (ix0 + 240, iy0 + 40, 46, 56), (ix0 + 40, iy0 + 150, 46, 56), (ix0 + 240, iy0 + 150, 46, 56)],
             outline=hl if hover == "쉼터" else None, chimney=True)
    d.rectangle([ix0 + 130, iy0 + 150, ix0 + 190, iy1], fill=ROOF)  # the door
    lantern(d, ix0 - 30, iy0 + 140, glow)
    # stagecoach at its post
    coach(d, COACH_XY[0], COACH_XY[1], outline=hl if hover == "역마차" else None)
    lantern(d, COACH_XY[0] - 40, COACH_XY[1] - 40, glow)
    # the gate and the road out
    gate(d, GATE_XY[0], GATE_XY[1], outline=hl if hover == "원정" else None)
    glow = glow.filter(ImageFilter.GaussianBlur(24)); img.alpha_composite(glow)
    d = ImageDraw.Draw(img)
    if labels:
        for name, (cx, cy) in QUARTERS.items():
            plate(d, cx, cy, name, line=hl if hover == name else m.LINE)
    return img


def strip(d):
    """A thin roster strip (120 px): a chip per mercenary with the level, the fatigue pips and the affliction, and the gate button."""
    m.panel(d, (0, 950, W, H), m.PANEL)
    d.line([(0, 950), (W, 950)], fill=m.LINE, width=2)
    for i, mm in enumerate(m.ROSTER):
        x = 40 + i * 250
        m.panel(d, (x, 964, x + 232, 1066), m.PANEL_LIGHT)
        m.text(d, (x + 14, 986), mm[0], 22, m.TEXT, "lm"); m.text(d, (x + 218, 986), f"Lv {mm[2]}", 18, m.BONE, "rm")
        m.text(d, (x + 14, 1014), mm[1] + (f" · {mm[6]}" if mm[6] else ""), 18, m.FDANGER if mm[6] else m.DIM, "lm")
        m.pips(d, x + 14, 1040, 204, mm[5], h=10, gap=3)
    m.text(d, (1560, 986), f"로스터 6/8 · 사망 0/15", 20, m.DIM, "lm")
    m.button(d, (1560, 1004, 1880, 1066), "원정 화면으로", m.BUTTON, 24)


def town_1():
    img = town(hover="역마차"); d = ImageDraw.Draw(img)
    m.header(img, "용병단 본부"); d = ImageDraw.Draw(img)
    # the hover tooltip over the stagecoach
    plate(d, 1330, 470, "역마차", 24, sub="후보 3명 · 고용 금 30 · 내보내기", line=m.BRASS)
    strip(d)
    return img.convert("RGB")


def town_2():
    img = town(hover=None); m.header(img, "용병단 본부"); d = ImageDraw.Draw(img)
    m.text(d, (W / 2, 1040), "건물을 눌러 들어간다 · 성문의 표지판이 원정 화면", 20, m.DIM, "mm")
    return img.convert("RGB")


def town_3():
    img = town(hover=None, labels=False); m.header(img, "용병단 본부"); d = ImageDraw.Draw(img)
    for name, (cx, cy) in QUARTERS.items():
        plate(d, cx, cy + 10, name, 26, sub=STATUS[name], line=m.LINE)
    return img.convert("RGB")


def barracks_panel(d):
    m.panel(d, (40, 130, 1880, 1040), m.PANEL, outline=m.BRASS, width=2)
    m.text(d, (64, 170), "막사 — 로스터", 30, m.DIM, "lm")
    m.button(d, (1700, 146, 1856, 196), "닫기", m.QUIET, 22)
    for i, mm in enumerate(m.ROSTER):
        y = 206 + i * 126
        m.roster_line(d, 64, y, mm, rows=False, level=False)
        x = 64 + 700
        m.text(d, (x, y + 40), f"Lv {mm[2]}", 30, m.BONE, "lm"); m.text(d, (x + 110, y + 42), f"경험치 {mm[3]}/{mm[4]}", 20, m.DIM, "lm")
        m.exp_bar(d, x, y + 66, 300, mm[3], mm[4])
        m.button(d, (1700, y + 36, 1856, y + 80), "내보내기", m.QUIET, 20, m.DIM)
    m.text(d, (64, 985), "금 120 · 로스터 6/8 · 사망 0/15 · 버려진 광산 클리어 1회 · 심연 잠김(광산 2회)", 22, m.DIM, "lm")


def town_enter():
    img = town(hover=None); m.header(img, "용병단 본부")
    shade = Image.new("RGBA", (W, H), (0, 0, 0, 150)); shade.paste((0, 0, 0, 0), (0, 0, W, 100)); img.alpha_composite(shade)
    d = ImageDraw.Draw(img); barracks_panel(d)
    return img.convert("RGB")


def main():
    now = Image.open(m.SHOT).convert("RGB")
    outs = {"town-1": town_1(), "town-2": town_2(), "town-3": town_3(), "town-enter": town_enter()}
    for k, im in outs.items():
        im.save(HERE / f"mock-{k}.png"); print(f"mock-{k}.png")
    m.sheet([("지금: 로비 하나에 로스터·열·던전·쉬기·출발", now),
             ("1 (권장): 마을 그림 + 건물 버튼, 아래에 얇은 로스터 띠와 [원정 화면으로] (역마차에 마우스를 올린 모습)", outs["town-1"]),
             ("2: 마을 그림 + 건물 버튼만 (로스터는 막사 안에)", outs["town-2"]),
             ("3: 마을 그림 + 건물마다 상태 카드", outs["town-3"])], 2, HERE / "mock-compare-town.png",
            footer="마을 그림은 도형으로 그린 자리 표시다. 진짜 그림은 배치를 정한 뒤 생성 1회(지시가 있을 때). 건물: 막사(로스터·레벨·내보내기), 쉼터(쉬기·치료), 역마차(고용), 성문(원정 화면).")


if __name__ == "__main__" and "--painted" not in sys.argv and "--topdown" not in sys.argv:
    main()


# ----------------------------------------------------------------- the generated painting (one call, 2026-10-16) in layout 1
PAINT = HERE.parents[1] / "output/background/lobby_town.png"
PAINT_TOP = 30                                   # the row of the 1920x1280 scaled painting that sits under the header
# hotspots on the 1920-wide painting (x0, y0, x1, y1) and where each label plate goes
PAINT_HOTSPOTS = {"막사": (0, 400, 700, 835), "쉼터": (720, 300, 1185, 835), "역마차": (1215, 610, 1595, 835), "원정": (1600, 390, 1920, 835)}
PAINT_LABELS = {"막사": (355, 876), "쉼터": (935, 876), "역마차": (1400, 876), "원정": (1760, 876)}


def painted_town(hover=None, labels=True):
    paint = Image.open(PAINT).convert("RGB").resize((W, round(W * 1536 / 2304)), Image.LANCZOS)
    img = Image.new("RGBA", (W, H), m.BG + (255,))
    img.paste(paint.crop((0, PAINT_TOP, W, PAINT_TOP + H)), (0, 0))
    d = ImageDraw.Draw(img)
    if hover:
        x0, y0, x1, y1 = PAINT_HOTSPOTS[hover]
        # the hovered building brightens: a brass rim and a light wash
        wash = Image.new("RGBA", (W, H), (0, 0, 0, 0)); wd = ImageDraw.Draw(wash)
        wd.rectangle([x0, y0, x1, y1], fill=(255, 220, 150, 28), outline=m.BRASS + (255,), width=3)
        img.alpha_composite(wash); d = ImageDraw.Draw(img)
    if labels:
        for name, (cx, cy) in PAINT_LABELS.items():
            plate(d, cx, cy, name, line=m.BRASS if hover == name else m.LINE)
    return img


def town_painted_1():
    img = painted_town(hover="역마차"); m.header(img, "용병단 본부"); d = ImageDraw.Draw(img)
    plate(d, 1400, 560, "역마차", 24, sub="후보 3명 · 고용 금 30 · 내보내기", line=m.BRASS)
    strip(d)
    return img.convert("RGB")


def town_painted_plain():
    img = painted_town(hover=None, labels=False); m.header(img, "용병단 본부")
    return img.convert("RGB")


def town_painted_enter():
    img = painted_town(hover=None); m.header(img, "용병단 본부")
    shade = Image.new("RGBA", (W, H), (0, 0, 0, 150)); shade.paste((0, 0, 0, 0), (0, 0, W, 100)); img.alpha_composite(shade)
    d = ImageDraw.Draw(img); barracks_panel(d)
    return img.convert("RGB")


def main_painted():
    outs = {"town-painted-1": town_painted_1(), "town-painted-plain": town_painted_plain(), "town-painted-enter": town_painted_enter()}
    for k, im in outs.items():
        im.save(HERE / f"mock-{k}.png"); print(f"mock-{k}.png")
    m.sheet([("생성한 그림을 안 1에 얹음 (역마차에 마우스를 올린 모습)", outs["town-painted-1"]),
             ("막사에 들어간 모습", outs["town-painted-enter"])], 2, HERE / "mock-compare-painted.png",
            footer="그림: output/background/lobby_town.png (2304x1536, 생성 1회). 화면에는 1920 폭으로 줄여 위 30px부터 1080을 보인다. 건물의 발치는 그림 높이의 약 64%.")


if __name__ == "__main__" and "--painted" in sys.argv:
    main_painted()


# ----------------------------------------------------------------- the three-quarter view (second call) beside the side view
PAINT_TD = HERE.parents[1] / "output/background/lobby_town_topdown.png"
TD_TOP = 0
TD_HOTSPOTS = {"막사": (20, 260, 800, 700), "쉼터": (815, 160, 1280, 585), "역마차": (1275, 470, 1590, 720), "원정": (1590, 205, 1920, 640)}
TD_LABELS = {"막사": (413, 740), "쉼터": (1046, 628), "역마차": (1432, 760), "원정": (1755, 684)}


def painted_any(path, top, hotspots, labels_pos, hover=None, labels=True):
    paint = Image.open(path).convert("RGB").resize((W, round(W * 1536 / 2304)), Image.LANCZOS)
    img = Image.new("RGBA", (W, H), m.BG + (255,))
    img.paste(paint.crop((0, top, W, top + H)), (0, 0))
    d = ImageDraw.Draw(img)
    if hover:
        x0, y0, x1, y1 = hotspots[hover]
        wash = Image.new("RGBA", (W, H), (0, 0, 0, 0)); wd = ImageDraw.Draw(wash)
        wd.rectangle([x0, y0, x1, y1], fill=(255, 220, 150, 28), outline=m.BRASS + (255,), width=3)
        img.alpha_composite(wash); d = ImageDraw.Draw(img)
    if labels:
        for name, (cx, cy) in labels_pos.items():
            plate(d, cx, cy, name, line=m.BRASS if hover == name else m.LINE)
    return img


def town_td_1():
    img = painted_any(PAINT_TD, TD_TOP, TD_HOTSPOTS, TD_LABELS, hover="역마차"); m.header(img, "용병단 본부"); d = ImageDraw.Draw(img)
    plate(d, 1432, 430, "역마차", 24, sub="후보 3명 · 고용 금 30 · 내보내기", line=m.BRASS)
    strip(d)
    return img.convert("RGB")


def town_td_plain():
    img = painted_any(PAINT_TD, TD_TOP, TD_HOTSPOTS, TD_LABELS, hover=None, labels=False); m.header(img, "용병단 본부")
    return img.convert("RGB")


def main_topdown():
    outs = {"town-td-1": town_td_1(), "town-td-plain": town_td_plain()}
    for k, im in outs.items():
        im.save(HERE / f"mock-{k}.png"); print(f"mock-{k}.png")
    side1 = Image.open(HERE / "mock-town-painted-1.png").convert("RGB"); sidep = Image.open(HERE / "mock-town-painted-plain.png").convert("RGB")
    m.sheet([("옆 시점 (첫 생성): 안 1에 얹음", side1), ("비스듬히 내려다본 시점 (둘째 생성): 안 1에 얹음", outs["town-td-1"]),
             ("옆 시점: 그림만", sidep), ("내려다본 시점: 그림만", outs["town-td-plain"])], 2, HERE / "mock-compare-view.png",
            footer="같은 소재(막사·여관·역마차·성문·예배당·달)를 두 시점으로. 둘 다 생성 1회(각 약 $0.033). 내려다본 시점은 위 5분의 1이 하늘이라 달이 헤더에 가린다.")


if __name__ == "__main__" and "--topdown" in sys.argv:
    main_topdown()
