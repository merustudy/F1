"""Item tiers on the screens (round 35, Slice B stage 14): how a tier shows on an item's cell (three ways), where a chosen item
would merge, a reward of a tier, and the camp's upkeep (the window's second card, and choosing the item). Drawn over the
game's screenshots (game/, run 20261006-b13b); the tiers on the cells are staged for the pictures. No API call.
  .venv/bin/python ArtPipeline/Archive/35-tiers/mock_tiers.py
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[3]; HERE = Path(__file__).resolve().parent
FONT = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"
GAME = HERE / "game"

# Tier colours (proposal): Bronze keeps the cell as it is; Silver, Gold and Diamond get their colour.
# Deep enough to show on the bone cells and on the dark cells of a battle.
TIER = {"Bronze": (0xB0, 0x6E, 0x3A), "Silver": (0x8C, 0x9C, 0xB2), "Gold": (0xE2, 0xA2, 0x1E), "Diamond": (0x2E, 0xC4, 0xE8)}
TIER_KO = {"Bronze": "동", "Silver": "은", "Gold": "금", "Diamond": "다이아"}
# The tier's name in words over the dark panels and the cards: lighter than the rims.
TIER_TEXT = {"Bronze": (0xD8, 0x92, 0x58), "Silver": (0xD5, 0xDE, 0xEA), "Gold": (0xF7, 0xC8, 0x4A), "Diamond": (0x6F, 0xE3, 0xF8)}
INK = (0x18, 0x09, 0x07); TEXT = (0xEB, 0xEB, 0xE6); DIM = (0x9A, 0xA0, 0xAC); PANEL = (0x1E, 0x22, 0x2B)
BONE = (208, 194, 165); BRASS_FILL = (180, 145, 76); BUTTON = (54, 104, 170); QUIET = (0x3A, 0x41, 0x50)
FATIGUE = (0xCF, 0xBA, 0xF7)


def font(size): return ImageFont.truetype(str(FONT), size)


def cell(col, i):
    """The light inside of a board cell: column 0..3 (rows 4..1 from the left), cell 0..4 from the top. The same on the party side and in battle."""
    x0 = 122 + 190 * col; y0 = 648 + 62 * i
    return (x0, y0, x0 + 175, y0 + 55)


def rim(img, box, color, width=4):
    """Variant A: the cell's inside edge in the tier's colour, a dark hairline outside it."""
    d = ImageDraw.Draw(img)
    x0, y0, x1, y1 = box
    d.rectangle((x0, y0, x1, y1), outline=color, width=width)
    d.rectangle((x0 + width, y0 + width, x1 - width, y1 - width), outline=tuple(c // 2 for c in color), width=1)


def badge(img, box, color):
    """Variant B: the grade badge (bottom-left) takes the tier's colour; its number stays."""
    x0, y0, x1, y1 = box
    cx, cy = x0 + 12 - 3, y1 - 12 - 2 + 3
    px = img.load()
    for y in range(cy - 13, cy + 14):
        for x in range(cx - 13, cx + 14):
            if (x - cx) ** 2 + (y - cy) ** 2 > 12 ** 2: continue
            r, g, b = px[x, y][:3]
            if r - b > 50 and r > 110:                     # the brass of the badge, not its ink
                k = (r + g + b) / (173 + 139 + 72)
                px[x, y] = tuple(min(255, int(c * k)) for c in color) + ((px[x, y][3],) if len(px[x, y]) == 4 else ())


def gem(img, box, color):
    """Variant C: a small gem at the cell's top-left corner, Bronze too."""
    d = ImageDraw.Draw(img)
    x0, y0 = box[0] + 5, box[1] + 5; s = 14
    pts = [(x0 + s / 2, y0), (x0 + s, y0 + s / 2), (x0 + s / 2, y0 + s), (x0, y0 + s / 2)]
    d.polygon(pts, fill=color, outline=INK)


def text(img, xy, s, size, color, anchor="la"):
    ImageDraw.Draw(img).text(xy, s, font=font(size), fill=color, anchor=anchor)


def recolor(img, box, src, dst, tol=40):
    """Turns a cell's fill from one colour to another (a chosen cell's brass to bone and back), keeping the icon."""
    px = img.load()
    for y in range(box[1], box[3] + 1):
        for x in range(box[0], box[2] + 1):
            p = px[x, y]
            if all(abs(p[k] - src[k]) <= tol for k in range(3)):
                px[x, y] = tuple(min(255, max(0, p[k] - src[k] + dst[k])) for k in range(3)) + tuple(p[3:])


# The staged tiers: on the party side (node map, round 32's board: longsword, dagger (chosen), buckler, herb pouch on row 1).
PARTY_TIERS = [((3, 0), "Silver"), ((3, 2), "Gold"), ((3, 3), "Diamond"), ((2, 0), "Gold"), ((0, 0), "Silver")]
BATTLE_TIERS = [((3, 0), "Silver"), ((2, 0), "Gold"), ((0, 0), "Diamond")]


def tiers_on(img, marks, how):
    for (col, i), tier in marks:
        box = cell(col, i)
        if how == "A": rim(img, box, TIER[tier])
        if how == "B": badge(img, box, TIER[tier])
        if how == "C": gem(img, box, TIER[tier])
    return img


def sheet(entries, cols, path, lab=44, gap=24, size=26, footer=None):
    w = max(im.width for _, im in entries); h = max(im.height for _, im in entries); rows = (len(entries) + cols - 1) // cols
    lines = footer or []; fh = 34 * len(lines) + (16 if lines else 0)
    out = Image.new("RGB", (cols * (w + gap) + gap, rows * (h + lab + gap) + gap + fh), (18, 18, 18)); d = ImageDraw.Draw(out)
    for k, (name, im) in enumerate(entries):
        x = gap + (k % cols) * (w + gap); y = gap + (k // cols) * (h + lab + gap)
        d.text((x, y), name, font=font(size), fill=(235, 235, 235)); out.paste(im.convert("RGB"), (x, y + lab))
    for j, s in enumerate(lines): d.text((gap, out.height - fh + j * 34), s, font=font(22), fill=(190, 190, 190))
    out.save(path); print(path.name, out.size)


def legend():
    im = Image.new("RGB", (900, 60), (18, 18, 18)); d = ImageDraw.Draw(im); x = 10
    for tier, color in TIER.items():
        d.rectangle((x, 16, x + 28, 44), fill=color, outline=INK); d.text((x + 38, 30), f"{TIER_KO[tier]}", font=font(24), fill=TEXT, anchor="lm"); x += 150
    return im


# ---- 1. A tier on a cell ------------------------------------------------------------------------------------------------

def sheet_cells():
    party = Image.open(GAME / "ko_30_map_fatigue.png").convert("RGB")
    battle = Image.open(GAME / "ko_05_battle.png").convert("RGB")
    crop_party = (100, 616, 900, 966); crop_battle = (100, 616, 900, 966)
    entries = [("지금 (파티 쪽)", party.crop(crop_party)), ("지금 (전투)", battle.crop(crop_battle))]
    names = {"A": "A: 칸의 안쪽 테가 단계의 색 (권장)", "B": "B: 등급 배지가 단계의 색", "C": "C: 왼쪽 위의 작은 보석"}
    for how in "ABC":
        entries.append((names[how] + " — 파티 쪽", tiers_on(party.copy(), PARTY_TIERS, how).crop(crop_party)))
        entries.append((names[how] + " — 전투", tiers_on(battle.copy(), BATTLE_TIERS if how != "B" else [], how).crop(crop_battle)))
    sheet(entries, 2, HERE / "mock-cells.png",
          footer=["단계는 스크린샷 위에 꾸민 것: 로언(1열) 롱소드 은, 버클러 금, 약초 주머니 다이아, 카이의 소드 금, 미라의 지팡이 은(전투는 카이 금, 미라 다이아). 동은 지금 그대로.",
                  "색: 동 #B06E3A(B·C만), 은 #8C9CB2, 금 #E2A21E, 다이아 #2EC4E8. B는 전투 칸에 배지가 없어 전투에서는 보이지 않고, 금 배지는 지금의 놋쇠 배지와 같아 보인다."])
    legend().save(HERE / "mock-legend.png")


# ---- 2. Merging and a reward ------------------------------------------------------------------------------------------

def merge_view():
    """The dagger (Bronze) chosen on row 1; another Bronze dagger on row 2's board: its cell says it would merge into Silver."""
    img = Image.open(GAME / "ko_30_map_fatigue.png").convert("RGB")
    chosen = cell(3, 1); target = cell(2, 1)
    dagger = img.crop((chosen[0] - 3, chosen[1] - 3, chosen[2] + 4, chosen[3] + 4))
    recolor(dagger, (0, 0, dagger.width - 1, dagger.height - 1), BRASS_FILL, BONE, tol=30)
    img.paste(dagger, (target[0] - 3, target[1] - 3))
    # Where it would merge: the cell under a veil, the next tier's rim and what happens.
    veil = Image.new("RGBA", img.size, (0, 0, 0, 0))
    ImageDraw.Draw(veil).rectangle(target, fill=(20, 22, 30, 150))
    img = Image.alpha_composite(img.convert("RGBA"), veil).convert("RGB")
    rim(img, target, TIER["Silver"], width=4)
    text(img, ((target[0] + target[2]) / 2, (target[1] + target[3]) / 2), "합치기 → 은", 20, (0xE6, 0xEC, 0xF4), anchor="mm")
    # The panel: the chosen item's line with its tier, wrapped as the game wraps it, and what putting it on the same one does.
    d = ImageDraw.Draw(img)
    d.rectangle((996, 742, 1890, 830), fill=PANEL)
    x = 1000
    for s_, color in (("단검 · ", TEXT), ("동", TIER_TEXT["Bronze"]), (" · 등급 8 — 무기 장비 / 크기 1칸 / 쿨다운 1.5초 / 앞에서 2번째 자리까지만 발동 / 맨 앞 적에게 피해 3 /", TEXT)):
        text(img, (x, 746), s_, 19, color); x += d.textlength(s_, font=font(19))
    text(img, (1000, 770), "전투마다 피로 +1", 19, FATIGUE)
    text(img, (1000, 800), "같은 단검·동 위에 놓으면 합쳐서 은 하나가 됩니다(칸과 장비 피로가 하나 준다).", 19, (0xB4, 0xC2, 0xD6))
    return img


def card_title(img, y0, y1, x0, x1, parts):
    """Writes a reward card's title over the old one: each row of pixels is first painted with the card's own colour at its right end."""
    px = img.load()
    for y in range(y0, y1):
        c = px[x1 + 10, y]
        for x in range(x0, x1):
            px[x, y] = c
    x = x0 + 5
    for s_, color in parts:
        text(img, (x, y0 + 2), s_, 26, color); x += ImageDraw.Draw(img).textlength(s_, font=font(26))


def reward_view():
    """A Silver longbow offered after a floor-5 battle (chosen), a Gold buckler (an elite's): the tier in the title and as a stripe."""
    img = Image.open(GAME / "ko_07_reward.png").convert("RGB")
    card_title(img, 196, 232, 1105, 1520, (("장궁 · ", TEXT), ("은", TIER_TEXT["Silver"]), (" · 등급 8", TEXT)))
    card_title(img, 336, 372, 1105, 1520, (("버클러 · ", TEXT), ("금", TIER_TEXT["Gold"]), (" · 등급 8", TEXT)))
    d = ImageDraw.Draw(img)
    d.rectangle((980, 190, 986, 322), fill=TIER["Silver"])
    d.rectangle((980, 330, 986, 462), fill=TIER["Gold"])
    return img


def sheet_merge():
    m = merge_view(); r = reward_view()
    sheet([("합치기: 고른 단검(동)을 놓으면 합쳐지는 칸", m.crop((100, 600, 1920, 980))),
           ("보상: 단계를 제목과 카드 왼쪽 띠에", r.crop((960, 170, 1920, 640)))], 1, HERE / "mock-merge-reward.png",
          footer=["합칠 수 있는 칸: 다음 단계의 색 테와 \"합치기 → 은\" 표. 놓으면 그 칸이 한 단계 위가 되고 고른 것은 사라진다.",
                  "보상 카드: 정예는 한 단계 위(예: 금 버클러). 고른 아이템의 줄도 단계를 적는다."])


# ---- 3. The camp's upkeep -----------------------------------------------------------------------------------------------

WINDOW = (1150, 237, 1730, 557); BOX = (1153, 240, 1727, 554); REST_CARD = (1310, 371, 1570, 531)


def camp_cards():
    img = Image.open(GAME / "ko_33_camp.png").convert("RGB")
    card = img.crop(REST_CARD)
    d = ImageDraw.Draw(img)
    d.rectangle((BOX[0] + 10, 360, BOX[2] - 10, 545), fill=PANEL)
    left = (BOX[0] + 24, 371); right = (BOX[0] + 24 + 260 + 12, 371)
    img.paste(card, left)
    other = card.copy(); od = ImageDraw.Draw(other)
    od.rectangle((10, 8, 250, 152), fill=BUTTON)
    od.text((18, 12), "정비", font=font(30), fill=TEXT)
    od.text((18, 64), "보드의 아이템 하나를", font=font(19), fill=TEXT); od.text((18, 92), "한 단계 위로", font=font(19), fill=TEXT)
    img.paste(other, right)
    return img


def camp_upkeep():
    """After 정비: the window says to choose on the boards; the chosen longsword's change and the two buttons are in it."""
    img = Image.open(GAME / "ko_33_camp.png").convert("RGB")
    fire = img.crop((1174, 259, 1254, 339))
    d = ImageDraw.Draw(img)
    d.rectangle(BOX, fill=PANEL)
    img.paste(fire, (1174, 259))
    text(img, (1274, 262), "15층 · 야영지 · 정비", 32, TEXT)
    text(img, (1274, 310), "왼쪽 보드에서 올릴 아이템을 고르세요.", 19, DIM)
    # The chosen item: its icon from the board, the tier change and its effect.
    sword = img.crop((cell(3, 0)[0] + 8, cell(3, 0)[1] + 5, cell(3, 0)[2] - 8, cell(3, 0)[3] - 5))
    d.rounded_rectangle((1177, 366, 1703, 470), radius=8, fill=(0x2A, 0x30, 0x3C))
    img.paste(sword, (1190, 380))
    x = 1370
    for s, color in (("롱소드  ", TEXT), ("동", TIER_TEXT["Bronze"]), (" → ", TEXT), ("은", TIER_TEXT["Silver"])):
        text(img, (x, 382), s, 26, color); x += ImageDraw.Draw(img).textlength(s, font=font(26))
    text(img, (1370, 424), "맨 앞 적에게 피해 11 → 22", 19, TEXT)
    for (x0, x1, label, color) in ((1177, 1433, "돌아가기", QUIET), (1447, 1703, "정비하기", BUTTON)):
        d.rounded_rectangle((x0, 484, x1, 540), radius=8, fill=color)
        text(img, ((x0 + x1) / 2, 512), label, 24, TEXT, anchor="mm")
    # On the boards: the longsword chosen (brass). Moving items rests meanwhile: the panel's two board buttons are hidden.
    recolor(img, cell(3, 0), (211, 197, 167), BRASS_FILL, tol=26)
    d.rectangle((996, 862, 1470, 962), fill=(28, 32, 42))
    return img


def sheet_camp():
    sheet([("야영지 창: 쉬기와 정비", camp_cards().crop((980, 180, 1920, 620))),
           ("정비를 누른 뒤: 보드에서 고르고 창에서 정한다", camp_upkeep().crop((100, 180, 1920, 980)))], 1, HERE / "mock-camp-upkeep.png",
          footer=["정비: 보드(왼쪽)의 아이템을 누르면 금색으로 골라지고, 창이 그 아이템의 단계와 효과가 어떻게 바뀌는지 보인다. \"정비하기\"로 정하고 다음 층으로 간다.",
                  "\"돌아가기\"는 두 카드로 돌아간다. 다이아는 고를 수 없다. 정비하는 동안 보드의 아이템 옮기기는 쉰다(누르면 정비할 아이템을 고른다)."])


if __name__ == "__main__":
    sheet_cells(); sheet_merge(); sheet_camp()
