"""The long expedition's map and camp (round 34, Slice B stage 13): 15 map floors and the boss, elite and camp nodes, and what
the party does at a camp. Drawn over the current node map screenshot (game/ko_04_map.png); the map is the game's own
generator output (`Tools/Sim map --dungeon abandoned_mine --seed 7` over the stage's starting data, game/map15_seed7.txt).
The elite and camp markers are drawn shapes standing in for art. No API call.
  .venv/bin/python ArtPipeline/Archive/34-long-map/mock_long_map.py
"""
import math
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[3]; HERE = Path(__file__).resolve().parent
FONT = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"
ICON = ROOT / "Assets/@Art/UI/Icon"
SHOT = HERE / "game/ko_04_map.png"; MAP = HERE / "game/map15_seed7.txt"

# The node map (UiPrefabSetup.NodeMap): the tablet at (980, 190) 920x414, its stone inside (992..1888, 202..590); a node is a
# 74 disc, its marker 13 in, its name under it (18); a path is a 4-wide line of brass at 85%. The panel's right half: the
# title (34), the hint (20), the help (19), the buttons at y 878 (76 high): 인벤토리로, 인벤토리 보기 at 1220, 전투 시작 at 1644.
STONE = (992, 202, 1888, 590); VIEW = (996, 206, 1884, 586)
BUTTON = (0x3B, 0x6F, 0xB5); GOOD = (0x58, 0xB3, 0x68); DEAD = (0x33, 0x33, 0x38); QUIET = (0x3A, 0x41, 0x50); SELECTED = (0x80, 0x66, 0x14)
TEXT = (0xEB, 0xEB, 0xE6); DIM = (0x9A, 0xA0, 0xAC); BRASS = (0xB8, 0x94, 0x4E); INK = (0x18, 0x09, 0x07)
ELITE = (0xC8, 0x4B, 0x4B); FIRE = (0xF3, 0x9C, 0x12); FIRE_IN = (0xFF, 0xD8, 0x6A); WOOD = (0x7A, 0x4E, 0x2C)
PANEL_BG = (28, 32, 42); STONE_BG = (55, 52, 54)
KIND_NAME = {"Battle": "전투", "Elite": "정예", "Camp": "야영지", "Boss": "보스"}


def font(size): return ImageFont.truetype(str(FONT), size)


def read_map():
    nodes = {}
    for line in MAP.read_text().splitlines():
        parts = line.split(" ")
        i, floor, col, kind = int(parts[0]), int(parts[1]), int(parts[2]), parts[3]
        nxt = parts[5] if len(parts) > 5 else ""
        nodes[i] = dict(id=i, floor=floor, col=col, kind=kind, next=[int(x) for x in nxt.split(",") if x])
    return nodes


NODES = read_map()
FLOORS = max(n["floor"] for n in NODES.values())          # 16: 15 floors and the boss


class State:
    """Where the party is: the node it stands on, the nodes it can choose, the one chosen (NodeMapScreen's states)."""
    def __init__(self, current, selected=-1, at_camp=False):
        self.current = current; self.selected = selected
        self.floor = NODES[current]["floor"]
        self.available = set() if at_camp else set(NODES[current]["next"])

    def color(self, n):
        if n["id"] in self.available: return SELECTED if n["id"] == self.selected else BUTTON
        if n["id"] == self.current: return GOOD
        return DEAD if n["floor"] <= self.floor else QUIET


# Floor 10, choosing between a battle and an elite on floor 11 (the elite chosen); floor 6, choosing between two camps
# (the first chosen); at the camp of floor 7.
CHOOSING_ELITE = State(22, selected=25)
CHOOSING_CAMP = State(12, selected=14)
AT_CAMP = State(14, at_camp=True)


def smooth(size, draw):
    k = 4; im = Image.new("RGBA", (size * k, size * k), (0, 0, 0, 0)); draw(ImageDraw.Draw(im), k, size * k)
    return im.resize((size, size), Image.LANCZOS)


def disc(size, color):
    return smooth(size, lambda d, k, s: d.ellipse([0, 0, s - 1, s - 1], fill=color + (255,)))


def campfire(size):
    """A drawn camp marker: two crossed logs and a flame."""
    def draw(d, k, s):
        for a in (math.radians(20), math.radians(160)):
            cx, cy = s * 0.5, s * 0.78; dx, dy = math.cos(a) * s * 0.36, math.sin(a) * s * 0.10
            d.line([cx - dx, cy + dy, cx + dx, cy - dy], fill=WOOD + (255,), width=int(s * 0.11))
        d.polygon([(s * 0.5, s * 0.12), (s * 0.70, s * 0.45), (s * 0.66, s * 0.68), (s * 0.5, s * 0.74), (s * 0.34, s * 0.68), (s * 0.30, s * 0.45)], fill=FIRE + (255,))
        d.polygon([(s * 0.5, s * 0.36), (s * 0.60, s * 0.55), (s * 0.5, s * 0.70), (s * 0.40, s * 0.55)], fill=FIRE_IN + (255,))
    return smooth(size, draw)


BATTLE = Image.open(ICON / "node_battle.png").convert("RGBA"); BOSS = Image.open(ICON / "node_boss.png").convert("RGBA")


def marker(kind, size):
    if kind == "Camp": return campfire(size)
    if kind == "Boss": return BOSS.resize((size, size), Image.LANCZOS)
    icon = BATTLE.resize((size, size), Image.LANCZOS)
    if kind == "Elite":
        # The battle's swords on a red diamond: an elite is a harder battle.
        back = smooth(size, lambda d, k, s: d.polygon([(s / 2, 0), (s - 1, s / 2), (s / 2, s - 1), (0, s / 2)], fill=ELITE + (255,)))
        small = int(size * 0.8); back.alpha_composite(icon.resize((small, small), Image.LANCZOS), ((size - small) // 2, (size - small) // 2))
        return back
    return icon


def draw_node(img, x, y, n, state, size, label):
    img.alpha_composite(disc(size, state.color(n)), (round(x - size / 2), round(y - size / 2)))
    m = round(size * 0.65); img.alpha_composite(marker(n["kind"], m), (round(x - m / 2), round(y - m / 2)))
    if label:
        d = ImageDraw.Draw(img); f = font(18); s = KIND_NAME[n["kind"]]
        l, t, r, b = d.textbbox((0, 0), s, font=f)
        d.text((x - (r - l) / 2 - l, y + size / 2 + 4 - t), s, font=f, fill=TEXT, stroke_width=2, stroke_fill=INK)


def draw_map(img, pos, state, size, width, label):
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0)); d = ImageDraw.Draw(layer)
    for n in NODES.values():
        for t in n["next"]:
            if n["id"] in pos and t in pos: d.line([pos[n["id"]], pos[t]], fill=BRASS + (round(255 * 0.85),), width=width)
    for i, p in pos.items(): draw_node(layer, p[0], p[1], NODES[i], state, size, label)
    # Only what is inside the stone shows, as in a scroll view.
    mask = Image.new("L", img.size, 0); ImageDraw.Draw(mask).rectangle(VIEW, fill=255)
    alpha = Image.composite(layer.split()[3], Image.new("L", img.size, 0), mask); layer.putalpha(alpha)
    img.alpha_composite(layer)


def clear_map(img):
    ImageDraw.Draw(img).rectangle(STONE, fill=STONE_BG + (255,))


def progress(img, state):
    """The header's floor count (Map.Progress: "{0}층 / {1}층")."""
    d = ImageDraw.Draw(img); d.rectangle([1500, 14, 1900, 70], fill=(0x35, 0x34, 0x36))
    f = font(30); s = f"{state.floor}층 / {FLOORS}층"; l, t, r, b = d.textbbox((0, 0), s, font=f)
    d.text((1880 - r, 41 - (b - t) / 2 - t), s, font=f, fill=(0xA0, 0x9C, 0x90))


def row_positions(floor, x0, x1):
    row = sorted((n for n in NODES.values() if n["floor"] == floor), key=lambda n: n["col"])
    return {n["id"]: x0 + (n["col"] + 0.5) / len(row) * (x1 - x0) for n in row}


SPACING = 88.5          # the floors as far apart as now (354 / 4)


def map_a(src, state):
    """Scrolling up, as Slay the Spire's map: the floors as far apart as now, the view scrolled so that the current floor is near the bottom."""
    img = src.copy(); clear_map(img); progress(img, state)
    # The floor four above is just out of view, its name too.
    base = VIEW[1] - 59 + 4 * SPACING
    pos = {}
    for f in range(1, FLOORS + 1):
        y = base - (f - state.floor) * SPACING
        for i, x in row_positions(f, VIEW[0], VIEW[2] - 20).items(): pos[i] = (x, y)
    draw_map(img, pos, state, 74, 4, True)
    d = ImageDraw.Draw(img)
    # The scroll bar: its thumb is the part of the floors in view.
    x = VIEW[2] - 10; top, bottom = VIEW[1] + 4, VIEW[3] - 4
    d.rounded_rectangle([x, top, x + 6, bottom], radius=3, fill=(0x2A, 0x28, 0x2A))
    content = FLOORS * SPACING; seen_bottom = (state.floor - 0.5) * SPACING - (VIEW[3] - base); seen_top = seen_bottom + (VIEW[3] - VIEW[1])
    t0 = top + (1 - min(content, seen_top) / content) * (bottom - top); t1 = top + (1 - max(0, seen_bottom) / content) * (bottom - top)
    d.rounded_rectangle([x, t0, x + 6, t1], radius=3, fill=BRASS)
    return img


def map_b(src, state):
    """The whole map at once, upward as now: the floors 24 apart, small nodes with markers only."""
    img = src.copy(); clear_map(img); progress(img, state)
    spacing = (VIEW[3] - VIEW[1]) / FLOORS; pos = {}
    for f in range(1, FLOORS + 1):
        for i, x in row_positions(f, VIEW[0] + 60, VIEW[2] - 60).items(): pos[i] = (x, VIEW[3] - (f - 0.5) * spacing)
    draw_map(img, pos, state, 22, 2, False)
    return img


def map_c(src, state):
    """The whole map at once, left to right: the floors as columns 55 apart, the nodes of a floor spread up and down, markers only."""
    img = src.copy(); clear_map(img); progress(img, state)
    spacing = (VIEW[2] - VIEW[0]) / FLOORS; pos = {}
    for f in range(1, FLOORS + 1):
        for i, y in row_positions(f, VIEW[1] + 10, VIEW[3] - 10).items(): pos[i] = (VIEW[0] + (f - 0.5) * spacing, VIEW[3] + VIEW[1] - y)
    draw_map(img, pos, state, 42, 3, False)
    return img


# ---- The panel ------------------------------------------------------------------------------------------------------

def button(src, box, width):
    """A button of the screenshot cut to another width: its ends kept, its middle stretched, its words gone."""
    x0, y0, x1, y1 = box; end = 26
    left = src.crop((x0, y0, x0 + end, y1)); right = src.crop((x1 - end, y0, x1, y1)); mid = src.crop((x0 + end, y0, x0 + end + 1, y1))
    out = Image.new("RGBA", (width, y1 - y0)); out.paste(left, (0, 0)); out.paste(mid.resize((width - 2 * end, y1 - y0)), (end, 0)); out.paste(right, (width - end, 0))
    return out


PRIMARY = (1640, 874, 1884, 958); QUIET_BUTTON = (1216, 874, 1460, 958)


def put_button(img, src, kind, x, width, label, size=30):
    b = button(src, PRIMARY if kind == "primary" else QUIET_BUTTON, width)
    img.alpha_composite(b, (x - 4, 874))
    d = ImageDraw.Draw(img); f = font(size); l, t, r, bb = d.textbbox((0, 0), label, font=f)
    d.text((x - 4 + width / 2 - (r - l) / 2 - l, 916 - (bb - t) / 2 - t), label, font=f, fill=TEXT)


def clear_battle_button(img):
    ImageDraw.Draw(img).rectangle([1636, 870, 1888, 962], fill=PANEL_BG + (255,))


def panel_words(img, title, hint, lines=(), keep_help=True):
    """The panel's right half: the title and the hint replaced, the help kept (moved under the given lines)."""
    help_text = img.crop((996, 738, 1890, 794))
    d = ImageDraw.Draw(img); d.rectangle([996, 642, 1890, 866], fill=PANEL_BG + (255,))
    d.text((1000, 650), title, font=font(34), fill=TEXT)
    d.text((1000, 702), hint, font=font(20), fill=DIM)
    y = 746
    for s, color in lines:
        d.text((1000, y), s, font=font(19), fill=color); y += 28
    if keep_help: img.paste(help_text, (996, y + 8 if lines else 738))


def selected_elite(src):
    img = map_a(src, CHOOSING_ELITE)
    panel_words(img, "11층 · 정예", "강한 적 무리가 기다립니다.")
    return img


def selected_camp(src):
    img = map_a(src, CHOOSING_CAMP)
    panel_words(img, "7층 · 야영지", "싸움 없이 쉬어 가는 곳입니다.")
    clear_battle_button(img); put_button(img, src, "primary", 1644, 236, "야영지로")
    return img


def camp_a(src):
    """At the camp: the panel's right half is the camp; the two choices take the battle button's place."""
    img = map_a(src, AT_CAMP)
    panel_words(img, "7층 · 야영지", "하나를 고르면 다음 층으로 갑니다. 그 전에 보드와 자리를 바꿀 수 있습니다.",
                [("쉬기: 살아 있는 용병마다 HP를 최대의 30% 회복하고, 피로도를 20 덜어 냅니다.", TEXT),
                 ("정비: 아이템 하나를 한 단계 올립니다. (14단계)", TEXT)])
    clear_battle_button(img)
    put_button(img, src, "quiet", 1468, 200, "정비"); put_button(img, src, "primary", 1680, 200, "쉬기")
    return img


def camp_b(src):
    """At the camp: a window over the map with the two choices; the panel keeps only the boards' buttons."""
    img = map_a(src, AT_CAMP)
    shade = Image.new("RGBA", img.size, (0, 0, 0, 0)); ImageDraw.Draw(shade).rectangle(STONE, fill=(0, 0, 0, 150)); img.alpha_composite(shade)
    d = ImageDraw.Draw(img); box = (1150, 236, 1730, 556)
    d.rounded_rectangle(box, radius=10, fill=(0x1B, 0x1E, 0x28), outline=BRASS, width=3)
    img.alpha_composite(campfire(80), (box[0] + 24, box[1] + 22))
    d.text((box[0] + 124, box[1] + 26), "7층 · 야영지", font=font(32), fill=TEXT)
    d.text((box[0] + 124, box[1] + 72), "하나를 고르면 다음 층으로 갑니다.", font=font(19), fill=DIM)
    for k, (title, desc, color) in enumerate((("쉬기", ["HP 30% 회복", "피로도 -20"], BUTTON), ("정비", ["아이템 하나를", "한 단계 위로 (14단계)"], QUIET))):
        bx = box[0] + 24 + k * 272; by = box[1] + 134
        d.rounded_rectangle([bx, by, bx + 260, by + 160], radius=8, fill=color)
        d.text((bx + 18, by + 14), title, font=font(30), fill=TEXT)
        for j, s in enumerate(desc): d.text((bx + 18, by + 66 + j * 28), s, font=font(19), fill=TEXT)
    panel_words(img, "7층 · 야영지", "지도 위의 창에서 하나를 고릅니다.")
    clear_battle_button(img)
    return img


def sheet(entries, cols, path, lab=44, gap=24, size=26, footer=None):
    w = max(im.width for _, im in entries); h = max(im.height for _, im in entries); rows = (len(entries) + cols - 1) // cols
    lines = footer or []; fh = 34 * len(lines) + (16 if lines else 0)
    out = Image.new("RGB", (cols * (w + gap) + gap, rows * (h + lab + gap) + gap + fh), (18, 18, 18)); d = ImageDraw.Draw(out)
    for i, (name, im) in enumerate(entries):
        x = gap + (i % cols) * (w + gap); y = gap + (i // cols) * (h + lab + gap)
        d.text((x, y), name, font=font(size), fill=(235, 235, 235)); out.paste(im.convert("RGB"), (x, y + lab))
    for j, s in enumerate(lines): d.text((gap, out.height - fh + j * 34), s, font=font(22), fill=(190, 190, 190))
    out.save(path); print(path.name, out.size)


def main():
    src = Image.open(SHOT).convert("RGBA")
    maps = {"now": src.copy(), "A": map_a(src, CHOOSING_ELITE), "B": map_b(src, CHOOSING_ELITE), "C": map_c(src, CHOOSING_ELITE)}
    labels = {"now": "지금: 4층(3층 + 보스), 한 화면", "A": "A: 위로 스크롤, 층 사이와 노드 크기는 지금 그대로 (권장)",
              "B": "B: 16층을 한 화면에, 위로", "C": "C: 16층을 한 화면에, 왼쪽에서 오른쪽으로"}
    for k, im in maps.items():
        if k != "now": im.convert("RGB").save(HERE / f"mock-map-{k}.png")      # "now" is game/ko_04_map.png
    crop = (970, 0, 1910, 610)
    sheet([(labels[k], maps[k].crop(crop)) for k in labels], 2, HERE / "mock-map-compare.png",
          footer=["지도는 게임의 생성기 그대로(시드 7, 13단계 출발 데이터). 10층에서 11층의 전투와 정예 중 정예를 고른 때.",
                  "정예 = 붉은 마름모 위의 칼, 야영지 = 모닥불 (둘 다 그림 자리의 도형). 지나온 층은 지금처럼 어둡게."])
    panels = {"elite": selected_elite(src), "camp-node": selected_camp(src), "camp-A": camp_a(src), "camp-B": camp_b(src)}
    for k, im in panels.items(): im.convert("RGB").save(HERE / f"mock-{k}.png")
    crop = (970, 180, 1910, 980)
    sheet([("정예 노드를 고른 때 (공통)", panels["elite"].crop(crop)), ("야영지 노드를 고른 때 (공통): 버튼이 '야영지로'", panels["camp-node"].crop(crop)),
           ("야영지 A: 패널이 야영지가 되고, 전투 시작 자리에 정비·쉬기 (권장)", panels["camp-A"].crop(crop)), ("야영지 B: 지도 위의 창에서 고른다", panels["camp-B"].crop(crop))],
          2, HERE / "mock-camp-compare.png",
          footer=["13단계에는 쉬기만 있고 정비는 14단계에 들어온다. 야영지에서 고르기 전에 보드와 자리를 바꿀 수 있다(왼쪽 보드는 그대로).",
                  "야영지에 들어간 상태는 저장되어, 앱을 닫았다 열면 야영지로 돌아온다."])


if __name__ == "__main__": main()
