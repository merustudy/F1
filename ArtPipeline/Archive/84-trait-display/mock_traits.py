# -*- coding: utf-8 -*-
"""Round 84: where the mercenaries' traits show (Design/16 판정, 2026-10-18 사용자 "1. 용어: 특성 통일 … 8. 그냥 특성으로
통일(아이콘 테 색 차별을 통해 구별 예정) … 나머지 권장안 수행"). Drawn over the 2026-10-18 screenshots of 20261018-jobs36
(base-*.png = ko_57 barracks, ko_58 coach, ko_66 party popup, ko_72 settlement). Shapes and text only — no API call; the trait
icons are placeholders (a letter in a framed square).
  .venv/bin/python ArtPipeline/Archive/84-trait-display/mock_traits.py
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
FONT = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"

# UiPalette
TEXT = (0xEB, 0xEB, 0xE6); DIM = (0x9A, 0xA0, 0xAC); GOOD = (0x58, 0xB3, 0x68); LOW = (0xD8, 0x6A, 0x5A)
BONE = (0xCD, 0xC0, 0xA0); STAR = (0xEC, 0xC4, 0x5C); LINE = (0x55, 0x5C, 0x6B); QUIET = (0x3A, 0x41, 0x50)
WINDOW = (0x1C, 0x22, 0x2A); STAGE = (0x16, 0x16, 0x1C); EDGE = (0xB8, 0x9A, 0x55)
SILVER = (0xB4, 0xBC, 0xC8); BRONZE = (0xB5, 0x7B, 0x4C)
ICON_FILL = (0x14, 0x18, 0x1E)

# The first batch (Design/05 §6): name, origin (innate / acquired), polarity (+ / -), effect line, what brought it.
TRAITS = {
    "eagle_eye": ("매의 눈", "innate", "+", "명중 +5%p", None),
    "clumsy": ("서투름", "innate", "-", "명중 −5%p", None),
    "iron_bones": ("강골", "innate", "+", "빈사의 사망 확률 −5%p", None),
    "frail": ("허약", "innate", "-", "최대 HP −10%", None),
    "steady": ("담대함", "innate", "+", "오르는 피로 −10%", None),
    "timid": ("소심함", "innate", "-", "오르는 피로 +10%", None),
    "seasoned": ("노련함", "acquired", "+", "치명타 +2%p", "클리어한 원정에서"),
    "shaken": ("패배의 기억", "acquired", "-", "각성 −10%p", "후퇴한 원정에서"),
    "survivor": ("구사일생", "acquired", "+", "빈사의 사망 확률 −10%p", "빈사에서 살아 돌아와"),
    "scarred": ("흉터", "acquired", "-", "최대 HP −5%", "빈사에서 살아 돌아와"),
    "limp": ("절뚝임", "acquired", "-", "회피 −5%p", "빈사에서 살아 돌아와"),
    "hardened": ("단련", "acquired", "+", "각성 +10%p", "각성한 원정에서"),
    "nightmares": ("악몽", "acquired", "-", "오르는 피로 +10%", "고통인 채 돌아와"),
}
# The job passives shown as traits (판정 8). The short names are new copy, proposed here (판정할 것 4).
JOB_TRAITS = {
    "warrior": ("선봉", "맨 앞: 전투 시작 때 자신에게 보호막 5"),
    "viking": ("광기", "빈사 상태: 무기 피해 +100%"),
    "ranger": ("급소", "무기의 치명타 확률 +10%"),
    "mage": ("후방 영창", "뒤에서 2번째 자리까지: 무기 피해 +25%"),
    "priest": ("축복", "회복시킨 대상에게 보호막 3"),
}
JOB_NAMES = {"warrior": "전사", "viking": "바이킹", "ranger": "방랑자", "mage": "마법사", "priest": "사제"}

# The barracks as shot (ko_57), a few expeditions later: (name, job, traits). 엘라 has more than a line holds.
BARRACKS = [
    ("아스트리드", "viking", ["iron_bones", "scarred"]),
    ("엘라", "priest", ["steady", "eagle_eye", "iron_bones", "survivor", "hardened", "seasoned", "scarred", "limp"]),
    ("카이", "warrior", ["clumsy"]),
    ("미라", "mage", ["frail", "seasoned"]),
    ("로언", "warrior", ["timid", "nightmares"]),
]
BARRACKS_ROWS = [266 + 100 * i for i in range(5)]
# The coach (ko_58): the three candidates carry 0~2 innate traits.
COACH = [("이보", "priest", ["eagle_eye", "frail"]), ("카린", "ranger", ["steady"]), ("오스카르", "warrior", [])]
COACH_ROWS = [340, 498, 656]
# The party popup (ko_66): the list rows top-down and the stage cards left-right (4열 → 1열).
PARTY_LIST = [("아스트리드", "viking", ["iron_bones", "scarred"]), ("엘라", "priest", ["steady", "eagle_eye", "iron_bones", "survivor", "hardened", "seasoned", "scarred", "limp"]),
              ("카이", "warrior", ["clumsy"]), ("미라", "mage", ["frail", "seasoned"]), ("로언", "warrior", ["timid", "nightmares"])]
PARTY_LIST_ROWS = [232 + 94 * i for i in range(5)]
PARTY_CARDS = [(89, "mage", ["frail", "seasoned"]), (359, "priest", ["steady", "eagle_eye", "iron_bones", "survivor", "hardened", "seasoned", "scarred", "limp"]),
               (629, "warrior", ["clumsy"]), (899, "warrior", ["timid", "nightmares"])]


def font(px): return ImageFont.truetype(str(FONT), int(round(px)))


def label(d, xy, s, px, color=TEXT, anchor="la"):
    d.text(xy, s, font=font(px), fill=color, anchor=anchor)


def width(s, px): return font(px).getlength(s)


def load(name): return Image.open(HERE / name).convert("RGBA")


def caption(img, text, top=False):
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    y = 0 if top else 1080 - 64
    d.rectangle((0, y, 1920, y + 64), fill=(0, 0, 0, 215))
    label(d, (24, y + 32), text, 30, TEXT, "lm")
    return Image.alpha_composite(img, layer)


def wipe(img, box, sample=None):
    """Paint the box with the background the shot has there (sampled at its left edge unless told)."""
    d = ImageDraw.Draw(img)
    color = img.getpixel(sample if sample else (box[0] + 2, (box[1] + box[3]) // 2))
    d.rectangle(box, fill=color)
    return d


# ---------------------------------------------------------------- the icon and its rim (판정할 것 1)
def trait_of(key, job=None):
    """(name, origin, polarity, effect, cause) for a trait key, or the job's trait when key is "job"."""
    if key == "job":
        name, text = JOB_TRAITS[job]
        return name, "job", "+", text, None
    return TRAITS[key]


def rim_colors(scheme, origin, polarity):
    """(rim, glyph, name colour, icon fill) for one trait under a colour scheme.
    1 (권장): the rim says what it does to you — job gold, strength green, weakness red.
    2: the rim says where it came from — job gold, innate silver, acquired bronze; the name's colour says good or bad.
    3: the rim says where it came from, the icon's ground says good or bad."""
    if scheme == 1:
        rim = STAR if origin == "job" else GOOD if polarity == "+" else LOW
        return rim, rim, TEXT, ICON_FILL
    origin_rim = {"job": STAR, "innate": SILVER, "acquired": BRONZE}[origin]
    if scheme == 2:
        name = TEXT if origin == "job" else GOOD if polarity == "+" else LOW
        return origin_rim, origin_rim, name, ICON_FILL
    fill = (0x2E, 0x2A, 0x18) if origin == "job" else (0x1B, 0x3A, 0x24) if polarity == "+" else (0x45, 0x1E, 0x1C)
    return origin_rim, TEXT, TEXT, fill


def icon(d, x, cy, size, trait, scheme=1):
    name, origin, polarity, _, _ = trait
    rim, glyph, _, fill = rim_colors(scheme, origin, polarity)
    y = cy - size // 2
    d.rounded_rectangle((x, y, x + size, y + size), radius=3, fill=fill, outline=rim, width=2 if size < 30 else 3)
    label(d, (x + size / 2, cy + 1), name[0], size * 0.55, glyph, "mm")
    return x + size


def chips(d, x, cy, x_max, keys, job, scheme=1, size=22, px=16, names=True, gap=14):
    """The trait line: the job's trait first, then the mercenary's own. What does not fit becomes a "+N" pill
    (판정할 것 3 — there is no cap)."""
    items = [trait_of("job", job)] + [trait_of(k) for k in keys]
    for i, trait in enumerate(items):
        w = size + (6 + width(trait[0], px) if names else 0)
        rest = len(items) - i - 1
        tail = (gap + width(f"+{rest}", px) + 16) if rest else 0
        if x + w + tail > x_max:
            more = f"+{len(items) - i}"
            d.rounded_rectangle((x, cy - size // 2, x + width(more, px) + 16, cy + size // 2), radius=size // 2, fill=QUIET)
            label(d, (x + 8, cy), more, px, TEXT, "lm")
            return
        icon(d, x, cy, size, trait, scheme)
        if names:
            color = rim_colors(scheme, trait[1], trait[2])[2]
            label(d, (x + size + 6, cy), trait[0], px, color, "lm")
        x += w + gap


def tooltip(img, anchor_x, anchor_y, trait, job=None, scheme=1, above=True):
    """The trait card: icon, name, what kind it is and where it came from, what it does (판정할 것 6)."""
    d = ImageDraw.Draw(img)
    name, origin, polarity, effect, cause = trait
    kind = {"job": f"직업 특성 · {JOB_NAMES.get(job, '')}", "innate": "타고난 특성", "acquired": "얻은 특성"}[origin]
    good = "" if origin == "job" else (" · 장점" if polarity == "+" else " · 약점")
    sub = kind + good + (f" — {cause}" if cause else "")
    w = max(360, int(max(width(sub, 18), width(effect, 22)) + 48))
    h = 132
    x0 = max(40, min(anchor_x - w // 2, 1880 - w))
    y0 = anchor_y - h - 14 if above else anchor_y + 14
    d.rectangle((x0, y0, x0 + w, y0 + h), fill=WINDOW, outline=EDGE, width=2)
    if above:
        d.polygon([(anchor_x - 10, y0 + h), (anchor_x + 10, y0 + h), (anchor_x, y0 + h + 12)], fill=EDGE)
    else:
        d.polygon([(anchor_x - 10, y0), (anchor_x + 10, y0), (anchor_x, y0 - 12)], fill=EDGE)
    icon(d, x0 + 20, y0 + 40, 40, trait, scheme)
    rim = rim_colors(scheme, origin, polarity)[0]
    label(d, (x0 + 74, y0 + 40), name, 26, TEXT, "lm")
    label(d, (x0 + 20, y0 + 82), sub, 18, DIM, "lm")
    label(d, (x0 + 20, y0 + 112), effect, 22, rim if origin != "job" else BONE, "lm")


# ---------------------------------------------------------------- barracks
def barracks(names=True, scheme=1):
    """The passive line under each row's fatigue pips becomes the trait line."""
    img = load("base-barracks.png")
    for (name, job, keys), y0 in zip(BARRACKS, BARRACKS_ROWS):
        d = wipe(img, (78, y0 + 63, 694, y0 + 90), sample=(700 - 12, y0 + 80))
        chips(d, 84, y0 + 77, 690, keys, job, scheme, size=24, px=17, names=names)
    return img


def barracks_a():
    """안 A (권장): icon + name chips; 엘라's line overflows into "+2"; the pointer rests on 아스트리드's 흉터."""
    img = barracks(names=True)
    x = 84 + 24 + 6 + width("광기", 17) + 14 + 24 + 6 + width("강골", 17) + 14
    tooltip(img, int(x + 12), 266 + 63, trait_of("scarred"))
    return img


def barracks_b():
    """안 B: icons only, the name and the effect in the tooltip; the pointer rests on 카이's job trait."""
    img = barracks(names=False)
    tooltip(img, 84 + 12, 466 + 63, trait_of("job", "warrior"), job="warrior")
    return img


# ---------------------------------------------------------------- colour schemes
def colors_sheet():
    img = Image.new("RGBA", (1920, 1080), STAGE + (255,))
    d = ImageDraw.Draw(img)
    label(d, (960, 56), "아이콘 테의 색 — 세 안 (아이콘 안의 글자는 Placeholder, 그림은 요청 시 이미지 API)", 32, TEXT, "mm")
    samples = [("job", "warrior", "직업"), ("eagle_eye", None, "타고난 · 장점"), ("frail", None, "타고난 · 약점"),
               ("survivor", None, "얻은 · 장점"), ("scarred", None, "얻은 · 약점")]
    schemes = [(1, "안 1 (권장) — 테 = 좋고 나쁨: 직업 금빛 · 장점 녹색 · 약점 붉은색. 타고난/얻은은 툴팁의 첫 줄"),
               (2, "안 2 — 테 = 어디서 왔나: 직업 금빛 · 타고난 은빛 · 얻은 청동빛. 좋고 나쁨은 이름의 색"),
               (3, "안 3 — 테 = 어디서 왔나(안 2), 아이콘의 바탕 = 좋고 나쁨(녹 · 적)")]
    for row, (scheme, title) in enumerate(schemes):
        y0 = 104 + row * 302
        d.rectangle((40, y0, 1880, y0 + 284), fill=WINDOW)
        label(d, (64, y0 + 36), title, 24, GOOD if scheme == 1 else TEXT, "lm")
        for i, (key, job, what) in enumerate(samples):
            trait = trait_of(key, job) if key == "job" else trait_of(key)
            x = 90 + i * 250
            icon(d, x, y0 + 130, 64, trait, scheme)
            label(d, (x + 32, y0 + 190), trait[0], 22, rim_colors(scheme, trait[1], trait[2])[2], "mm")
            label(d, (x + 32, y0 + 222), what, 18, DIM, "mm")
        # The same five on a barracks line.
        d.rectangle((1330, y0 + 100, 1856, y0 + 160), fill=(0x29, 0x30, 0x3C))
        label(d, (1346, y0 + 80), "막사 줄에서", 18, DIM, "lm")
        chips(d, 1346, y0 + 130, 1850, ["eagle_eye", "frail", "survivor", "scarred"], "warrior", scheme, size=24, px=17)
    return img


# ---------------------------------------------------------------- coach and party
def coach():
    """The candidate's passive line becomes the trait line (the job trait, then 0~2 innate)."""
    img = load("base-coach.png")
    for (name, job, keys), y0 in zip(COACH, COACH_ROWS):
        d = wipe(img, (510, y0 + 52, 1176, y0 + 82), sample=(1180, y0 + 66))
        chips(d, 514, y0 + 66, 1170, keys, job)
    tooltip(img, 514 + 22 + 6 + int(width("축복", 16)) + 14 + 11, 340 + 52, trait_of("eagle_eye"))
    return img


def party():
    """The list rows: the trait line instead of the passive line. The stage cards: icons only beside the job."""
    img = load("base-party.png")
    for (name, job, keys), y0 in zip(PARTY_LIST, PARTY_LIST_ROWS):
        d = wipe(img, (1244, y0 + 38, 1676, y0 + 58), sample=(1640, y0 + 48))
        chips(d, 1248, y0 + 48, 1672, keys, job, size=18, px=14, gap=10)
    d = ImageDraw.Draw(img)
    for x, job, keys in PARTY_CARDS:
        start = x + int(width(JOB_NAMES[job], 20)) + 14
        chips(d, start, 613, x + 222, keys, job, size=20, px=14, names=False, gap=4)
    tooltip(img, 1248 + 9, 326 + 38, trait_of("job", "priest"), job="priest")
    return img


# ---------------------------------------------------------------- settlement
def settlement():
    """One "새 특성" line per mercenary that got one, under the level-up line (판정할 것 7). 아스트리드 retreated and
    came back from death's door: 패배의 기억 and 흉터."""
    img = load("base-settlement.png")
    items = img.crop((480, 726, 1440, 762))
    d = wipe(img, (480, 716, 1440, 812), sample=(1400, 740))
    img.alpha_composite(items, (480, 774))
    d = ImageDraw.Draw(img)
    x = 524
    d.polygon([(x + 11, 733), (x + 22, 744), (x + 11, 755), (x, 744)], fill=BONE)
    label(d, (x + 32, 744), "새 특성: 아스트리드 —", 22, BONE, "lm")
    x += 32 + int(width("새 특성: 아스트리드 —", 22)) + 12
    for key in ["shaken", "scarred"]:
        trait = trait_of(key)
        icon(d, x, 744, 22, trait)
        label(d, (x + 28, 744), trait[0], 22, TEXT, "lm")
        x += 28 + int(width(trait[0], 22)) + 18
    return img


# ---------------------------------------------------------------- sheets
def sheet(pairs, name, scale=0.5):
    w, h = int(1920 * scale), int(1080 * scale)
    head = 56
    out = Image.new("RGB", (w * len(pairs) + 16 * (len(pairs) - 1), h + head), (0x0C, 0x0D, 0x11))
    d = ImageDraw.Draw(out)
    for i, (title, img) in enumerate(pairs):
        x = i * (w + 16)
        out.paste(img.convert("RGB").resize((w, h), Image.LANCZOS), (x, head))
        label(d, (x + 12, head / 2), title, 28, GOOD if "권장" in title else TEXT, "lm")
    out.save(HERE / name)


def main():
    shots = {
        "mock-barracks-A.png": caption(barracks_a(), "안 A (권장) — 패시브 글 줄이 특성 줄로: 아이콘 + 이름, 넘치면 +N (엘라), 아스트리드의 흉터에 마우스"),
        "mock-barracks-B.png": caption(barracks_b(), "안 B — 아이콘만, 이름과 효과는 툴팁 (카이의 직업 특성에 마우스)"),
        "mock-colors.png": caption(colors_sheet(), "아이콘 테의 색 — 안 1 (권장) · 안 2 · 안 3"),
        "mock-coach.png": caption(coach(), "역마차 — 후보의 특성 줄 (직업 특성 + 타고난 0~2), 고용 전에 보임 (이보의 매의 눈에 마우스)"),
        "mock-party.png": caption(party(), "파티 팝업 — 목록 줄은 특성 줄, 무대 카드는 직업 옆에 아이콘만 (엘라의 직업 특성에 마우스)"),
        "mock-settlement.png": caption(settlement(), "정산 — 새 특성을 얻은 용병마다 한 줄 (레벨 업 줄 아래)"),
    }
    for name, img in shots.items():
        img.convert("RGB").save(HERE / name)
    now = lambda img: caption(img, "지금")
    sheet([("지금", now(load("base-barracks.png"))), ("안 A (권장)", shots["mock-barracks-A.png"]), ("안 B", shots["mock-barracks-B.png"])],
          "compare-1-barracks.png")
    sheet([("역마차 지금", now(load("base-coach.png"))), ("역마차 안 (권장)", shots["mock-coach.png"]),
           ("파티 팝업 지금", now(load("base-party.png"))), ("파티 팝업 안 (권장)", shots["mock-party.png"])], "compare-2-coach-party.png")
    sheet([("지금", now(load("base-settlement.png"))), ("안 (권장)", shots["mock-settlement.png"])], "compare-3-settlement.png")


if __name__ == "__main__":
    main()
