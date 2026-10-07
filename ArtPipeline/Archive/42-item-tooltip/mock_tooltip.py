"""Round 42: an item's facts as a card beside the clicked cell (a Backpack Battles-like tooltip), drawn over the game's
screenshots (~/Library/Caches/F1/screenshots/20261007-r41b). Three looks for the card and the way it behaves on the party side
and in battle. No API call.
  .venv/bin/python ArtPipeline/Archive/42-item-tooltip/mock_tooltip.py
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageFilter

ROOT = Path(__file__).resolve().parents[3]; HERE = Path(__file__).resolve().parent
FONT = ROOT / "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf"
SHOTS = Path.home() / "Library/Caches/F1/screenshots/20261007-r41b"
FRAME = ROOT / "Assets/@Art/UI/Frame"

# UiPalette
TEXT = (0xEB, 0xEB, 0xE6); DIM = (0x9A, 0xA0, 0xAC); INK = (0x18, 0x09, 0x07); BRASS = (0xB8, 0x94, 0x4E)
FATIGUE = (0xCF, 0xBA, 0xF7)
TIER_MARK = {"common": None, "bronze": (0xA8, 0x68, 0x3A), "silver": (0x9A, 0xA7, 0xB8), "gold": (0xD4, 0xA2, 0x32)}
TIER_TEXT = {"common": (0xC9, 0xC2, 0xB0), "bronze": (0xD5, 0x9A, 0x66), "silver": (0xD3, 0xDB, 0xE4), "gold": (0xF0, 0xC8, 0x5A)}
KO = {"bronze": "동", "silver": "은", "gold": "금"}
UP = {"common": "bronze", "bronze": "silver", "silver": "gold"}

CARD_W = 400; PAD = 16; GAP = 10
PANEL_TOP = 620                                           # the board panel's top edge on every expedition screen
SCREEN = (1920, 1080)


def font(px): return ImageFont.truetype(str(FONT), int(round(px)))


def cell(col, i):
    """The outer box of a board cell: column 0..3 (rows 4..1 from the left), cell i from the top (mock_tier_marks)."""
    x0 = 120 + 190 * col; y0 = 646 + 62 * i
    return (x0, y0, x0 + 180, y0 + 60)


# ---- the item's words (UiText.ItemTitle / ItemDetails / MergeHint, from UI_StaticText.csv) ------------------------------------

def item(name, grade, tier="common", cat="무기 장비", size=1, cd="3.0", rows=None, effects=(), fatigue=None, merge=False):
    facts = [cat, f"크기 {size}칸", f"쿨다운 {cd}초"] + ([rows] if rows else [])
    return dict(name=name, grade=grade, tier=tier, facts=facts, effects=list(effects), fatigue=fatigue, merge=merge)


DAGGER = item("단검", 8, cd="1.5", rows="앞에서 2번째 자리까지만 발동", effects=["맨 앞 적에게 피해 3"], fatigue=1, merge=True)
BUCKLER = item("버클러", 8, tier="silver", cat="방어 장비", cd="5.0", rows="맨 앞에서만 발동", effects=["자신에게 보호막 19"], fatigue=1)
LONGSWORD = item("롱소드", 10, cd="3.0", rows="앞에서 2번째 자리까지만 발동", effects=["맨 앞 적에게 피해 11"], fatigue="base")
MAUL = item("감독관의 망치", 27, cd="4.5", rows="앞에서 2번째 자리까지만 발동", effects=["맨 앞 적에게 피해 40"])
FIRE_STAFF = item("화염 지팡이", 10, tier="bronze", cd="3.5", rows="뒤에서 3번째 자리까지만 발동", effects=["적 전체에게 피해 22"], fatigue="base")


def title_segments(it):
    segs = [(it["name"], TEXT)]
    if it["tier"] != "common":
        segs += [(" · ", DIM), (KO[it["tier"]], TIER_TEXT[it["tier"]])]
    segs += [(" · ", DIM), (f"등급 {it['grade']}", TEXT)]
    return segs


def merge_segments(it):
    up = UP[it["tier"]]
    if it["tier"] == "common":
        return [(f"같은 {it['name']} 위에 놓으면 합쳐서 ", TEXT), (KO[up], TIER_TEXT[up]), (" 하나가 됩니다.", TEXT)]
    return [(f"같은 {it['name']}·{KO[it['tier']]} 위에 놓으면 합쳐서 ", TEXT), (KO[up], TIER_TEXT[up]), (" 하나가 됩니다.", TEXT)]


# ---- text -----------------------------------------------------------------------------------------------------------------------

def wrap_segments(segs, f, width):
    """Greedy word wrap of coloured segments; a word longer than the line is broken by characters."""
    tokens = []
    for text, color in segs:
        parts = text.split(" ")
        for n, part in enumerate(parts):
            if part:
                tokens.append((part, color))
            if n < len(parts) - 1:
                tokens.append((" ", color))
    lines, cur, cur_w = [], [], 0.0
    for tok, color in tokens:
        w = f.getlength(tok)
        if cur and cur_w + w > width and tok != " ":
            lines.append(cur); cur, cur_w = [], 0.0
        if tok == " " and not cur:
            continue
        while w > width:                                   # break a long word
            k = len(tok)
            while k > 1 and f.getlength(tok[:k]) > width:
                k -= 1
            if cur:
                lines.append(cur); cur, cur_w = [], 0.0
            lines.append([(tok[:k], color)]); tok = tok[k:]; w = f.getlength(tok)
        cur.append((tok, color)); cur_w += w
    if cur:
        lines.append(cur)
    return lines


def draw_segments(d, x, y, line, f):
    for tok, color in line:
        d.text((x, y), tok, font=f, fill=color + (255,))
        x += f.getlength(tok)


def body_lines(it, inner_w, with_title=True):
    """[(segments, font, line gap)] of the card's text in order, wrapped to inner_w."""
    out = []
    f_title, f_fact, f_eff = font(24), font(19), font(20)
    if with_title:
        for ln in wrap_segments(title_segments(it), f_title, inner_w):
            out.append((ln, f_title, 6))
        out.append(("rule", None, 10))
    for ln in wrap_segments([(" / ".join(it["facts"]), DIM)], f_fact, inner_w):
        out.append((ln, f_fact, 4))
    out.append(("space", None, 4))
    for eff in it["effects"]:
        for ln in wrap_segments([(eff, TEXT)], f_eff, inner_w):
            out.append((ln, f_eff, 4))
    if it["fatigue"] == "base":
        out.append(([("기본 무기: 피로 없음", DIM)], f_fact, 4))
    elif it["fatigue"]:
        out.append(([(f"전투마다 피로 +{it['fatigue']}", FATIGUE)], f_fact, 4))
    if it["merge"]:
        out.append(("space", None, 4))
        for ln in wrap_segments(merge_segments(it), f_fact, inner_w):
            out.append((ln, f_fact, 4))
    return out


def text_height(lines):
    h = 0
    for ln, f, gap in lines:
        if ln == "rule":
            h += 1 + gap
        elif ln == "space":
            h += gap
        else:
            h += f.size + gap
    return h


def draw_lines(d, x, y, lines, inner_w, rule_color):
    for ln, f, gap in lines:
        if ln == "rule":
            d.line([(x, y), (x + inner_w, y)], fill=rule_color, width=1); y += 1 + gap
        elif ln == "space":
            y += gap
        else:
            draw_segments(d, x, y, ln, f); y += f.size + gap
    return y


# ---- frames ---------------------------------------------------------------------------------------------------------------------

def nine_slice(src, border, size, tiled):
    """Lays a 9-slice sprite over `size`: corners as they are, edges and the middle tiled or stretched."""
    W, H = size; sw, sh = src.size; b = border
    out = Image.new("RGBA", (W, H), (0, 0, 0, 0))

    def put(piece, xy):
        out.paste(piece, xy, piece)

    def fill(region, box):
        x0, y0, x1, y1 = box; w, h = x1 - x0, y1 - y0
        if w <= 0 or h <= 0:
            return
        if tiled:
            tw, th = region.size
            for yy in range(y0, y1, th):
                for xx in range(x0, x1, tw):
                    put(region.crop((0, 0, min(tw, x1 - xx), min(th, y1 - yy))), (xx, yy))
        else:
            put(region.resize((w, h), Image.LANCZOS), (x0, y0))

    fill(src.crop((b, b, sw - b, sh - b)), (b, b, W - b, H - b))
    fill(src.crop((b, 0, sw - b, b)), (b, 0, W - b, b)); fill(src.crop((b, sh - b, sw - b, sh)), (b, H - b, W - b, H))
    fill(src.crop((0, b, b, sh - b)), (0, b, b, H - b)); fill(src.crop((sw - b, b, sw, sh - b)), (W - b, b, W, H - b))
    put(src.crop((0, 0, b, b)), (0, 0)); put(src.crop((sw - b, 0, sw, b)), (W - b, 0))
    put(src.crop((0, sh - b, b, sh)), (0, H - b)); put(src.crop((sw - b, sh - b, sw, sh)), (W - b, H - b))
    return out


def sprite(name, scale=0.5):
    im = Image.open(FRAME / f"{name}.png").convert("RGBA")
    return im.resize((int(im.size[0] * scale), int(im.size[1] * scale)), Image.LANCZOS)


def shadowed(card, blur=7, alpha=150, dy=4):
    m = blur * 3
    out = Image.new("RGBA", (card.size[0] + 2 * m, card.size[1] + 2 * m), (0, 0, 0, 0))
    sh = Image.new("RGBA", out.size, (0, 0, 0, 0))
    a = card.split()[3].point(lambda v: int(v * alpha / 255))
    sh.paste((0, 0, 0, 255), (m, m + dy), a)
    sh = sh.filter(ImageFilter.GaussianBlur(blur))
    out.alpha_composite(sh); out.alpha_composite(card, (m, m))
    return out, m


# ---- the three looks ----------------------------------------------------------------------------------------------------------

def card_ink(it, notch=None):
    """1: an ink card with a brass hairline, the tier as a stripe down the left edge; a small notch towards the cell when it floats above the panel."""
    inner_w = CARD_W - 2 * PAD - 8
    lines = body_lines(it, inner_w)
    H = text_height(lines) + 2 * PAD
    nh = 12 if notch is not None else 0
    img = Image.new("RGBA", (CARD_W, H + nh), (0, 0, 0, 0)); d = ImageDraw.Draw(img)
    d.rounded_rectangle((0, 0, CARD_W - 1, H - 1), radius=4, fill=INK + (240,), outline=BRASS + (235,), width=1)
    d.rounded_rectangle((1, 1, CARD_W - 2, H - 2), radius=3, outline=(255, 255, 255, 16), width=1)
    stripe = TIER_MARK[it["tier"]]
    if stripe:
        d.rectangle((2, 2, 7, H - 3), fill=stripe + (255,))
    if notch is not None:
        nx = max(16, min(CARD_W - 16, notch))
        d.polygon([(nx - 10, H - 1), (nx + 10, H - 1), (nx, H + nh - 1)], fill=INK + (240,))
        d.line([(nx - 10, H - 1), (nx, H + nh - 1), (nx + 10, H - 1)], fill=BRASS + (235,), width=1)
        d.line([(nx - 9, H - 1), (nx + 9, H - 1)], fill=INK + (240,), width=1)
    draw_lines(d, PAD + 8, PAD, lines, inner_w, BRASS + (110,))
    return img


def card_stone(it):
    """2: the carved stone panel of the Diablo kit (panel, tiled) as the card; the tier underlines the title."""
    inner_w = CARD_W - 2 * 24
    lines = body_lines(it, inner_w)
    H = text_height(lines) + 2 * 24
    img = nine_slice(sprite("panel"), 20, (CARD_W, H), tiled=True)
    d = ImageDraw.Draw(img)
    stripe = TIER_MARK[it["tier"]]
    y = draw_lines(d, 24, 24, lines[:1], inner_w, BRASS + (110,)) if False else None
    # Draw the text; the rule under the title takes the tier's colour.
    rule = (stripe or BRASS) + (160,)
    draw_lines(d, 24, 24, lines, inner_w, rule)
    return img


def card_plate(it):
    """3: the gold-lined iron plate of the headers as the card's head with the name in gold; the facts on an ink body."""
    head_h = 50
    inner_w = CARD_W - 2 * PAD
    f_name = font(24); f_sub = font(19)
    lines = body_lines(it, inner_w, with_title=False)
    sub = [(KO[it["tier"]], TIER_TEXT[it["tier"]]), (" · ", DIM)] if it["tier"] != "common" else []
    sub += [(f"등급 {it['grade']}", DIM)]
    H = head_h + 6 + f_sub.size + 8 + text_height(lines) + PAD
    img = Image.new("RGBA", (CARD_W, H), (0, 0, 0, 0)); d = ImageDraw.Draw(img)
    d.rounded_rectangle((0, head_h // 2, CARD_W - 1, H - 1), radius=4, fill=INK + (240,), outline=BRASS + (200,), width=1)
    head = nine_slice(sprite("plate_label"), 22, (CARD_W, head_h), tiled=False)
    img.alpha_composite(head, (0, 0)); d = ImageDraw.Draw(img)
    d.text((CARD_W / 2, head_h / 2), it["name"], font=f_name, fill=BRASS + (255,), anchor="mm")
    y = head_h + 6
    sub_w = sum(f_sub.getlength(t) for t, _ in sub)
    draw_segments(d, (CARD_W - sub_w) / 2, y, sub, f_sub); y += f_sub.size + 8
    draw_lines(d, PAD, y, lines, inner_w, BRASS + (110,))
    return img


LOOKS = {"1": ("안 1 · 먹색 판 + 놋쇠 선 (권장)", card_ink), "2": ("안 2 · 돌 패널 카드", card_stone), "3": ("안 3 · 쇠 명패 머리 + 먹색 몸", card_plate)}


# ---- placing the card on a screenshot -------------------------------------------------------------------------------------------

def place_above(shot, card_img, cell_box, gap=8):
    """The party side: the card floats above the board panel, centred on the cell (clamped to the screen), so that no board is covered."""
    x0, y0, x1, y1 = cell_box
    cx = (x0 + x1) // 2
    w, h = card_img.size
    left = max(12, min(SCREEN[0] - 12 - w, cx - w // 2))
    top = PANEL_TOP - gap - h
    img, m = shadowed(card_img)
    shot.alpha_composite(img, (left - m, top - m))
    return (left, top, left + w, top + h)


def place_beside(shot, card_img, cell_box, side, gap=GAP):
    """In battle: beside the board, on the side of the panel that has room (the party's cards to the left, the enemy's to the right)."""
    x0, y0, x1, y1 = cell_box
    w, h = card_img.size
    left = x0 - gap - w if side == "left" else x1 + gap
    left = max(12, min(SCREEN[0] - 12 - w, left))
    top = max(PANEL_TOP + 6, min(SCREEN[1] - 12 - h, y0))
    img, m = shadowed(card_img)
    shot.alpha_composite(img, (left - m, top - m))
    return (left, top, left + w, top + h)


def shot(name):
    return Image.open(SHOTS / f"{name}.png").convert("RGBA")


# ---- sheets ---------------------------------------------------------------------------------------------------------------------

def sheet(entries, cols, path, footer=(), lab=40, gap=24, size=24, pad=24):
    """Pictures with a label above each, in a grid; footer lines at the bottom (mock_tier_marks.sheet)."""
    f = font(size); ff = font(20)
    cw = [0] * cols; rh = []
    for n, (_, im) in enumerate(entries):
        c = n % cols; cw[c] = max(cw[c], im.size[0])
        if c == 0:
            rh.append(0)
        rh[-1] = max(rh[-1], im.size[1])
    W = pad * 2 + sum(cw) + gap * (cols - 1)
    H = pad * 2 + sum(h + lab + gap for h in rh) + (len(footer) * 28 + 12 if footer else 0)
    out = Image.new("RGB", (W, H), (34, 34, 36)); d = ImageDraw.Draw(out)
    y = pad
    for r, h in enumerate(rh):
        x = pad
        for c in range(cols):
            n = r * cols + c
            if n >= len(entries):
                break
            label, im = entries[n]
            d.text((x, y + 6), label, font=f, fill=(230, 226, 214))
            out.paste(im.convert("RGB"), (x, y + lab))
            x += cw[c] + gap
        y += h + lab + gap
    for line in footer:
        d.text((pad, y), line, font=ff, fill=(176, 176, 170)); y += 28
    out.save(path)
    print("wrote", path.relative_to(ROOT), out.size)


def frame_note(img, box, text):
    """A brass bracket around a region of the current screen, with a word."""
    d = ImageDraw.Draw(img)
    d.rounded_rectangle(box, radius=6, outline=BRASS + (255,), width=3)
    f = font(22)
    d.rounded_rectangle((box[0], box[1] - 36, box[0] + f.getlength(text) + 20, box[1] - 4), radius=4, fill=INK + (235,), outline=BRASS + (255,))
    d.text((box[0] + 10, box[1] - 32), text, font=f, fill=BRASS + (255,))


PARTY_CROP = (380, 340, 1300, 1080)
BATTLE_CROP = (60, 600, 1700, 1080)


def main():
    dagger_cell = cell(3, 1)                                # Rowan's chosen dagger on ko_35_map_tiers
    # 1. the three looks, over the party side (the dagger's card floats above the panel), and the current screen
    now = shot("ko_35_map_tiers")
    frame_note(now, (992, 734, 1888, 822), "지금: 패널 오른쪽 아래의 설명 줄")
    entries = [("지금 · 고른 아이템의 사실은 패널 오른쪽 아래 두 줄", now.crop(PARTY_CROP))]
    for key, (label, make) in LOOKS.items():
        s = shot("ko_35_map_tiers")
        card = make(DAGGER, notch=(dagger_cell[0] + dagger_cell[2]) // 2 - 580) if key == "1" else make(DAGGER)
        place_above(s, card, dagger_cell)
        entries.append((label, s.crop(PARTY_CROP)))
    sheet(entries, 2, HERE / "mock-compare.png",
          footer=["파티 쪽(노드 맵·보상): 칸을 누르면 그 아이템을 고르는 것은 지금 그대로이고, 카드가 보드 패널 위(무대 아래쪽)에 뜬다 — 다른 보드와 '합치기 →' 표시를 가리지 않게. 다음 클릭(어디든)에 카드만 닫힌다.",
                  "카드의 글은 지금 설명 줄의 것 그대로(제목, 분류·크기·쿨다운·자리, 효과, 피로, 합치기 안내)를 줄마다 나눈 것이다. 폭 400. 단계가 있으면 안 1은 왼쪽 띠, 안 2는 제목 밑줄, 안 3은 명패 아래 글이 단계의 색."])

    # 2. the behaviour (look 1): party side click -> card, next click -> closed; battle (its own sheet): enemy to the right, party to the left
    a = shot("ko_35_map_tiers")
    place_above(a, card_ink(DAGGER, notch=(dagger_cell[0] + dagger_cell[2]) // 2 - 580), dagger_cell)
    b = shot("ko_35_map_tiers")
    sheet([("① 파티 쪽: 칸을 누름 → 고르기(놋쇠 칸)는 그대로 + 카드", a.crop(PARTY_CROP)),
           ("② 아무 데나 누름 → 카드만 닫힘 (고른 칸과 설명 줄은 그대로)", b.crop(PARTY_CROP))],
          2, HERE / "mock-flow.png",
          footer=["다음 클릭에 닫히는 것은 '누르는 순간'이다: 그 클릭이 칸·버튼이면 그 일(옮기기, 합치기, 인벤토리 보기…)은 그대로 된다. 카드 자체는 클릭을 받지 않는다(카드 위를 눌러도 닫힌다).",
                  "고른 아이템을 다시 누르면 지금처럼 고른 것이 풀리고, 카드도 닫힌다. 다른 아이템을 누르면 그 아이템의 카드가 열린다."])
    battle = shot("ko_09_boss_battle")
    place_beside(battle, card_ink(MAUL), (1052, 646, 1232, 706), "right")
    place_beside(battle, card_ink(FIRE_STAFF), cell(2, 0), "left")
    sheet([("③ 전투: 적의 아이템을 누르면 보드 오른쪽에, 아군의 것은 왼쪽에 (한 번에 하나. 그림은 둘 다)", battle.crop(BATTLE_CROP))],
          1, HERE / "mock-battle.png",
          footer=["전투에서는 지금 아이템 정보를 볼 길이 없다. 카드는 전투를 멈추지 않고, 포션을 든 동안(아군 보드가 포션의 대상일 때)에는 열리지 않는다. 적의 아이템도 카드로 본다(싸우는 중에 드러나는 정보).",
                  "패널의 가운데는 양초라 비켜 둔다: 아군의 카드는 보드 왼쪽(사슬 쪽), 적의 카드는 보드 오른쪽. 세로는 누른 칸의 높이에 맞추고 패널 안에 머문다."])

    # 2b. where the card goes on the party side: above the panel (P1) or beside the cell like Backpack Battles (P2)
    p1 = shot("ko_35_map_tiers")
    place_above(p1, card_ink(DAGGER, notch=(dagger_cell[0] + dagger_cell[2]) // 2 - 580), dagger_cell)
    p2 = shot("ko_35_map_tiers")
    place_beside(p2, card_ink(DAGGER), dagger_cell, "right")
    p3 = shot("ko_35_map_tiers")
    staff_cell = cell(0, 0)
    place_beside(p3, card_ink(item("화염 지팡이", 10, cd="3.5", rows="뒤에서 3번째 자리까지만 발동", effects=["적 전체에게 피해 11"], fatigue="base")), staff_cell, "right")
    sheet([("자리 P1 (권장): 보드 패널 위, 누른 칸의 열에 맞춰. 보드를 가리지 않는다", p1.crop(PARTY_CROP)),
           ("자리 P2: 칸 바로 옆(오른쪽). 1열이면 패널 오른쪽의 설명 줄과 '인벤토리로'를 가린다", p2.crop(PARTY_CROP)),
           ("자리 P2, 4열의 지팡이: 3열·2열의 보드와 '합치기 →' 표시를 가린다", p3.crop((100, 340, 1020, 1080)))],
          3, HERE / "mock-place.png",
          footer=["고른 아이템은 옮길 칸·합칠 칸을 바로 눌러야 하므로 카드가 보드를 덮으면 한 번 더 눌러 닫아야 한다. P1은 그 열의 용병 그림 아래쪽과 앞으로·뒤로를 잠시 덮지만 보드와 패널 오른쪽은 그대로다.",
                  "P1의 카드는 패널 위 8에 뜨고 아래 가운데의 작은 꼭지가 누른 칸의 열을 가리킨다. 화면 양 끝에서는 안쪽으로 밀린다."])

    # 3. detail: the three looks at twice the size, a common item with a merge hint and a silver one
    det = []
    for key, (label, make) in LOOKS.items():
        for it in (DAGGER, BUCKLER):
            card = make(it)
            img, m = shadowed(card)
            bg = Image.new("RGBA", img.size, (0x2A, 0x30, 0x3C, 255)); bg.alpha_composite(img)
            bg = bg.resize((bg.size[0] * 2, bg.size[1] * 2), Image.LANCZOS)
            det.append((f"{label.split(' (')[0]} · {it['name']}" + (" (은 ★★)" if it["tier"] == "silver" else " (일반, 합치기 안내)"), bg))
    sheet(det, 2, HERE / "mock-detail.png", lab=40,
          footer=["2배 확대. 제목 24 / 사실 19(흐린 글) / 효과 20 / 피로 19(연보라, 기본 무기는 흐린 글) / 합치기 안내 19. 색은 UiPalette(Text, TextDim, Fatigue, Tier*Text, Brass, Ink). 글꼴은 Pretendard Medium."])


if __name__ == "__main__":
    main()
