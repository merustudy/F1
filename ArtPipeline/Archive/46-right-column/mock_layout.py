# -*- coding: utf-8 -*-
"""Round 46: the right column of the node map, the shop and the loot screen laid out again (the user's 2026-10-08 asks):
the potions go where the battle has them (top left), the map / shop / loot area grows taller, and the panel with the words
and the inventory buttons is halved. Mockups over the screenshots of 20261007-225510; the map is a real map of the game's
generator (seed 5, Tools/Sim `map`) laid out with NodeMapScreen's own formula; frames are the kit's sprites drawn as Unity
draws them (half scale, tiled or sliced). No API call.
  .venv/bin/python ArtPipeline/Archive/46-right-column/mock_layout.py [check]
"""
import subprocess
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageChops, ImageFilter

ROOT = Path(__file__).resolve().parents[3]; HERE = Path(__file__).resolve().parent
SHOTS = Path.home() / "Library/Caches/F1/screenshots/20261007-225510"
UI = ROOT / "Assets/@Art/UI"
sys.path.insert(0, str(ROOT / "ArtPipeline/Archive/44-shop-node"))
import mock_shop as M  # noqa: E402  (round 44: fonts, palette, the coin, board cells, tier marks, word wrapping)

font = M.font
TEXT, DIM, INK, BRASS = M.TEXT, M.DIM, M.INK, M.BRASS
PANEL, SLOT, LINE = M.PANEL, M.SLOT, M.LINE
BUTTON, QUIET, SELECTED, GOOD, DEAD = M.BUTTON, M.QUIET, M.SELECTED, M.GOOD, M.DEAD
VIRTUE, FATIGUE, DANGER = M.VIRTUE, M.FATIGUE, M.DANGER
STONE_TEXT = TEXT


def shot(name): return Image.open(SHOTS / f"{name}.png").convert("RGBA")
def art(name): return Image.open(UI / f"{name}.png").convert("RGBA")


# ---- the kit's frames as Unity draws them -------------------------------------------------------------------------------------

def half(name):
    im = art(name); return im.resize((im.width // 2, im.height // 2), Image.LANCZOS)


def tiled(name, border, size):
    """Image.Type.Tiled at the canvas's scale (sprites are 200 per unit, the canvas 100): corners kept, edges and the middle
    repeated from the bottom-left corner of their area, the last tile cut at the top and the right."""
    src = half(name); b = border // 2; sw, sh = src.size; W, H = size
    out = Image.new("RGBA", (W, H), (0, 0, 0, 0))

    def fill(piece, box):
        x0, y0, x1, y1 = box; pw, ph = piece.size; rw, rh = x1 - x0, y1 - y0
        if rw <= 0 or rh <= 0: return
        region = Image.new("RGBA", (rw, rh), (0, 0, 0, 0)); y = rh - ph
        while y > -ph:
            x = 0
            while x < rw: region.paste(piece, (x, y)); x += pw
            y -= ph
        out.alpha_composite(region, (x0, y0))
    fill(src.crop((b, b, sw - b, sh - b)), (b, b, W - b, H - b))
    fill(src.crop((b, 0, sw - b, b)), (b, 0, W - b, b)); fill(src.crop((b, sh - b, sw - b, sh)), (b, H - b, W - b, H))
    fill(src.crop((0, b, b, sh - b)), (0, b, b, H - b)); fill(src.crop((sw - b, b, sw, sh - b)), (W - b, b, W, H - b))
    for (cx, cy), (px, py) in (((0, 0), (0, 0)), ((sw - b, 0), (W - b, 0)), ((0, sh - b), (0, H - b)), ((sw - b, sh - b), (W - b, H - b))):
        out.alpha_composite(src.crop((cx, cy, cx + b, cy + b)), (px, py))
    return out


def sliced(name, border, size, tint=None):
    """Image.Type.Sliced at the canvas's scale, tinted like an Image's color."""
    im = M.nine_slice(half(name), border // 2, size)
    if tint:
        rgb = ImageChops.multiply(im.convert("RGB"), Image.new("RGB", im.size, tint)); out = rgb.convert("RGBA"); out.putalpha(im.split()[3]); im = out
    return im


VIG = art("Icon/vignette").split()[3].resize((1920, 1080), Image.BILINEAR).point(lambda a: int(round(255 - 0.35 * a)))


def vignetted(layer):
    """A full-screen layer darkened by the screen's gloom (BuildScreenVignette, alpha 0.35), as everything under it is."""
    rgb = ImageChops.multiply(layer.convert("RGB"), Image.merge("RGB", (VIG, VIG, VIG)))
    out = rgb.convert("RGBA"); out.putalpha(layer.split()[3]); return out


BG = (23, 23, 29)   # the screen's ground before the gloom (the screenshot reads 21,21,26 in the middle)


def button(layer, box, label, color, size=24, dim=False):
    x0, y0, x1, y1 = box
    layer.alpha_composite(sliced("Frame/button", 32, (x1 - x0, y1 - y0), tint=color), (x0, y0))
    d = ImageDraw.Draw(layer); f = font(size); l, t, r, b = d.textbbox((0, 0), label, font=f)
    d.text(((x0 + x1) / 2 - (r - l) / 2 - l, (y0 + y1) / 2 - (b - t) / 2 - t), label, font=f, fill=(DIM if dim else TEXT))


def text_lines(d, x, y, s, f, color, width, gap=8):
    for ln in M.wrap_segments([(s, color)], f, width):
        M.draw_segments(d, x, y, ln, f); y += f.size + gap
    return y


# ---- the map: the game's generator, NodeMapScreen's layout ---------------------------------------------------------------------

KIND = {"Battle": ("node_battle", "전투"), "Elite": ("node_elite", "정예"), "Camp": ("node_camp", "야영지"),
        "Shop": ("node_shop", "상점"), "Boss": ("node_boss", "보스")}
SPACING, NAME_ROOM, NODE = 88.5, 26, 74


def load_map(seed):
    out = subprocess.run(["dotnet", str(ROOT / "Tools/Sim/bin/Debug/net10.0/F1.Sim.dll"), "map", "--seed", str(seed), "--project-root", str(ROOT)],
                         capture_output=True, text=True, check=True).stdout
    nodes = {}
    for line in out.strip().splitlines():
        p = line.split(); nid, floor, col, kind = int(p[0]), int(p[1]), int(p[2]), p[3]
        nodes[nid] = dict(id=nid, floor=floor, col=col, kind=kind, next=[int(v) for v in p[5].split(",")] if len(p) > 5 else [])
    return nodes


def bottom_margin(view_h):
    m = view_h % SPACING - SPACING / 2
    while m < NAME_ROOM: m += SPACING
    return m


def disc(color):
    """Unity's knob, tinted: a disc whose edge goes soft and a shade darker."""
    s = NODE; k = 4; big = Image.new("RGBA", (s * k, s * k), (0, 0, 0, 0)); d = ImageDraw.Draw(big)
    dark = tuple(int(c * 0.78) for c in color)
    d.ellipse([0, 0, s * k - 1, s * k - 1], fill=dark + (255,)); m = 3 * k
    d.ellipse([m, m, s * k - 1 - m, s * k - 1 - m], fill=color + (255,))
    return big.resize((s, s), Image.LANCZOS).filter(ImageFilter.GaussianBlur(0.6))


def draw_map(layer, tablet, nodes, current=None, passed=(), selected=None, scroll_floor=1):
    """The tablet (tiled, 40) with the map in its view: inset 14, the nodes laid out in its width less 20 (the bar's room),
    floor by floor 88.5 apart over the bottom margin, the paths under the nodes, the view's edges fading over 16, the bar."""
    x, y, w, h = tablet
    layer.alpha_composite(tiled("Frame/tablet", 40, (w, h)), (x, y))
    vx, vy, vw, vh = x + 14, y + 14, w - 28, h - 28; aw = vw - 20
    floors = max(n["floor"] for n in nodes.values()); margin = bottom_margin(vh)
    content = margin + floors * SPACING; rng = content - vh
    off = max(0.0, min(rng, (scroll_floor - 1) * SPACING))
    per = {}
    for n in nodes.values(): per.setdefault(n["floor"], []).append(n)
    pos = {}
    for f, ns in per.items():
        for n in ns: pos[n["id"]] = ((n["col"] + 0.5) / len(ns) * aw, vh - (margin + (f - 0.5) * SPACING) + off)
    view = Image.new("RGBA", (vw, vh), (0, 0, 0, 0)); d = ImageDraw.Draw(view)
    reach = {n["id"] for n in nodes.values()} if current is None else set(nodes[current]["next"])
    if current is None: reach = {n["id"] for n in per[1]}
    for n in nodes.values():
        for t in n["next"]:
            d.line([pos[n["id"]], pos[t]], fill=BRASS + (217,), width=4)
    f18 = font(18)
    for n in nodes.values():
        px, py = pos[n["id"]]
        if py < -60 or py > vh + 60: continue
        if n["id"] == selected: color = SELECTED
        elif n["id"] == current: color = GOOD
        elif n["id"] in passed: color = DEAD
        elif n["id"] in reach: color = BUTTON
        else: color = QUIET
        view.alpha_composite(disc(color), (round(px - NODE / 2), round(py - NODE / 2)))
        icon = art("Icon/" + KIND[n["kind"]][0]).resize((48, 48), Image.LANCZOS)
        view.alpha_composite(icon, (round(px - 24), round(py - 24)))
        label = KIND[n["kind"]][1]; l, t, r, b = d.textbbox((0, 0), label, font=f18)
        d.text((px - (r - l) / 2 - l, py + NODE / 2 + 2 + (24 - (b - t)) / 2 - t), label, font=f18, fill=(DIM if n["id"] in passed else TEXT))
    # RectMask2D softness 16 at the top and the bottom
    a = view.split()[3]; ramp = Image.new("L", (1, vh), 255)
    for i in range(16): ramp.putpixel((0, i), int(255 * (i + 0.5) / 16)); ramp.putpixel((0, vh - 1 - i), int(255 * (i + 0.5) / 16))
    view.putalpha(ImageChops.multiply(a, ramp.resize((vw, vh))))
    layer.alpha_composite(view, (vx, vy))
    # the bar: the track 4 in from the view's right end, the handle as long as the view is of the content
    tx0, tx1 = x + w - 14 - 4 - 6, x + w - 14 - 4; ty0, ty1 = y + 14 + 4, y + h - 14 - 4
    dd = ImageDraw.Draw(layer); dd.rounded_rectangle([tx0, ty0, tx1, ty1], radius=3, fill=INK + (153,))
    hl = (ty1 - ty0) * vh / content; hpos = 0 if rng <= 0 else off / rng
    hy1 = ty1 - (ty1 - ty0 - hl) * hpos; dd.rounded_rectangle([tx0, hy1 - hl, tx1, hy1], radius=3, fill=BRASS + (255,))
    return pos


# ---- items as the boards draw them --------------------------------------------------------------------------------------------

def item(id, name, cat, size, cd, rows, effects, fatigue, price=0, grade=8, tier="common"):
    facts = [cat, f"크기 {size}칸", f"쿨다운 {cd}초"] + ([rows] if rows else [])
    return dict(id=id, name=name, grade=grade, tier=tier, cat=cat, size=size, cd=cd, rows=rows, facts=facts,
                effects=list(effects), fatigue=fatigue, price=price, potion=False)


LONGBOW = item("longbow", "장궁", "무기 장비", 2, "3.0", "뒤에서 3번째 자리까지만 발동", ["뒤의 적 2명에게 피해 6"], 1, 16)
HERB = item("herb_pouch", "약초 주머니", "지원 아이템", 1, "5.0", None, ["자신의 HP 4 회복"], None, 10)
BUCKLER = item("buckler", "버클러", "방어 장비", 1, "5.0", "맨 앞에서만 발동", ["자신에게 보호막 6"], 1, 12)
WARD = item("ward_charm", "수호 부적", "지원 아이템", 1, "6.0", None, ["HP가 가장 낮은 아군에게 보호막 5"], None, 12)
FANG = item("rat_bite", "쥐 송곳니", "무기 장비", 1, "2.0", "앞에서 2번째 자리까지만 발동", ["맨 앞 적에게 피해 4"], 1)
RUSTY = item("rusty_blade", "녹슨 칼", "무기 장비", 1, "2.8", "앞에서 2번째 자리까지만 발동", ["맨 앞 적에게 피해 6"], 1)
STOCK = [LONGBOW, HERB, BUCKLER, WARD]


def cell(it):
    """The item as it lies on a board: a slot as tall as its cells (60 each, 2 apart), the icon fitted inside."""
    h = 60 * it["size"] + 2 * (it["size"] - 1)
    c = M.nine_slice(M.sprite("slot"), 8, (180, h)); icon = M.icon_of(it, (164, h - 12))
    c.alpha_composite(icon, ((180 - icon.size[0]) // 2, (h - icon.size[1]) // 2))
    return c


def centered(d, cx, y, s, f, color):
    l, t, r, b = d.textbbox((0, 0), s, font=f); d.text((cx - (r - l) / 2 - l, y - t), s, font=f, fill=color)


def item_tile(layer, box, it, state="normal", foot="price", facts=True, cell_room=122):
    """A tile of the shop or the loot: the cell at its own size, the name and kind, (the facts, the effects, the fatigue,) then
    the price or the loot's action at the foot. state: normal / picked / taken."""
    x0, y0, x1, y1 = box; w = x1 - x0; d = ImageDraw.Draw(layer)
    rim, width, fill = (LINE, 2, SLOT) if state != "picked" else (BRASS, 3, (0x26, 0x24, 0x1A))
    d.rounded_rectangle(box, radius=8, fill=fill + (255,), outline=rim + (255,), width=width)
    if state == "taken":
        centered(d, (x0 + x1) / 2, (y0 + y1) / 2 - 12, "주움", font(22), DIM); return
    c = cell(it); layer.alpha_composite(c, (x0 + (w - 180) // 2, y0 + 10 + (cell_room - c.size[1]) // 2))
    y = y0 + 10 + cell_room + 10
    f = font(19); segs = M.title_segments(it); tw = sum(f.getlength(s) for s, _ in segs)
    M.draw_segments(d, (x0 + x1) / 2 - tw / 2, y, segs, f); y += 26
    centered(d, (x0 + x1) / 2, y, f"{it['cat']} · {it['size']}칸", font(16), DIM); y += 26
    if facts:
        d.line([(x0 + 12, y), (x1 - 12, y)], fill=BRASS + (110,), width=1); y += 10
        ff, fe = font(15), font(16)
        y = text_lines(d, x0 + 12, y, f"쿨다운 {it['cd']}초", ff, DIM, w - 24, gap=5)
        if it["rows"]: y = text_lines(d, x0 + 12, y, it["rows"], ff, DIM, w - 24, gap=5)
        y += 3
        for e in it["effects"]: y = text_lines(d, x0 + 12, y, e, fe, TEXT, w - 24, gap=5)
        if it["fatigue"]: y = text_lines(d, x0 + 12, y, f"전투마다 피로 +{it['fatigue']}", ff, FATIGUE, w - 24, gap=5)
    if foot == "price":
        M.price_centered(layer, (x0 + x1) / 2, y1 - 24, it["price"])
    elif foot == "take":
        bx = (x0 + 14, y1 - 58, x1 - 14, y1 - 14)
        if state == "picked": centered(d, (x0 + x1) / 2, y1 - 46, "넣을 칸을 누르세요", font(19), VIRTUE)
        else: button(layer, bx, "줍기", QUIET, size=22)


# ---- the screens ---------------------------------------------------------------------------------------------------------------

POTIONS = (980, 92, 232, 84)             # the strip on the party side now, under the header above the right half
BATTLE_POTIONS_X = 30                    # where the battle screen has it (BuildPotionStrip(frame, 30f))
PLATE_H = 218                            # the right half's plate: 436 now, halved


def clear_right(layer, top=84, bottom=1080, x0=966):
    ImageDraw.Draw(layer).rectangle([x0, top, 1920, bottom], fill=BG + (255,))


def move_potions(base, layer):
    """The potion strip leaves the right half for the battle's place, top left (the party stands clear of it)."""
    strip = base.crop((POTIONS[0], POTIONS[1], POTIONS[0] + POTIONS[2], POTIONS[1] + POTIONS[3]))
    ImageDraw.Draw(layer).rectangle([POTIONS[0], POTIONS[1], POTIONS[0] + POTIONS[2], POTIONS[1] + POTIONS[3]], fill=BG + (255,))
    return strip


def split_table(base, out, left_end=960):
    """A: the board panel keeps only the boards (0..left_end); its right edge and corners are the screenshot's own right edge,
    moved; the heap of skulls moves in with it."""
    skulls = base.crop((872, 1004, 978, 1068))
    edge = base.crop((1900, 620, 1920, 1080))
    stone = base.crop((700, 980, 720, 1060)).resize((140, 80))
    out.paste(stone, (left_end - 142, 980)); out.paste(edge, (left_end - 20, 620))
    out.alpha_composite(skulls, (left_end - 20 - 4 - 106, 1004))


def panel(layer, box, title=None, hint=None, lines=(), buttons=()):
    """The halved plate: a title with its hint beside it (or none), up to two more lines, the buttons along the bottom."""
    x, y, w, h = box
    layer.alpha_composite(sliced("Frame/plate_label", 44, (w, h)), (x, y))
    d = ImageDraw.Draw(layer); ty = y + 18
    if title:
        f = font(34); l, t, r, b = d.textbbox((0, 0), title, font=f); d.text((x + 20 - l, ty - t), title, font=f, fill=TEXT)
        if hint:
            fh = font(20); hl, ht, hr, hb = d.textbbox((0, 0), hint, font=fh)
            d.text((x + 20 + (r - l) + 24 - hl, ty + (b - t) - (hb - ht) - ht + 1), hint, font=fh, fill=DIM)
        ty += 52
    for s, size, color in lines:
        ty = text_lines(d, x + 20, ty, s, font(size), color, w - 40, gap=7)
    by = y + h - 16 - 64
    for bx, bw, label, color, size in buttons:
        button(layer, (bx, by, bx + bw, by + 64), label, color, size=size)


HELP = "아이템을 누른 뒤 다른 칸을 누르면 옮기거나 서로 바꿉니다. 인벤토리로는 고른 아이템을 인벤토리에 넣고 인벤토리 보기에서 꺼냅니다. 앞으로와 뒤로는 옆 열의 용병과 자리를 바꿉니다."
MAP_BUTTONS = [(1000, 200, "인벤토리로", QUIET, 24), (1220, 236, "인벤토리 보기", QUIET, 24)]


def compose(base, layer, under=None):
    out = base.copy()
    if under: under(out)
    out.alpha_composite(vignetted(layer))
    return out


def screen_map(nodes, structure="A"):
    """The node map at the start: the potions top left, the tablet from under the header (92) down to the plate, the plate halved."""
    base = shot("ko_04_map"); layer = Image.new("RGBA", base.size, (0, 0, 0, 0))
    strip = move_potions(base, layer)
    if structure == "A":
        clear_right(layer, top=84, bottom=1080, x0=962)
        layer.alpha_composite(tiled("Frame/table", 40, (952, 242)), (968, 838))
        tablet = (980, 92, 920, 734)
    else:
        clear_right(layer, top=84, bottom=619, x0=962)
        tablet = (980, 92, 920, 746)
    draw_map(layer, tablet, nodes, selected=0)
    panel(layer, (980, 850, 920, PLATE_H), "1층 · 전투", "누가 기다리는지는 들어가 봐야 압니다.", [(HELP, 19, TEXT)],
          MAP_BUTTONS + [(1644, 236, "전투 시작", BUTTON, 30)])
    out = compose(base, layer, (lambda o: split_table(base, o)) if structure == "A" else None)
    out.alpha_composite(strip, (BATTLE_POTIONS_X, POTIONS[1]))
    return out


SHADE = 153


def shop_window(layer, view, tiles_h, facts, card_room=0):
    """The shop's window over the taller map: the camp's kind of box (brass rim, panel), the marker, title, hint and coins at
    its top, the four offers as tiles, (the picked offer's card,) refresh and leave at the bottom."""
    vx, vy, vw, vh = view
    W = 860; H = 108 + tiles_h + (16 + card_room if card_room else 0) + 16 + 48 + 22
    x0 = vx + (vw - W) // 2; y0 = vy + (vh - H) // 2; box = (x0, y0, x0 + W, y0 + H)
    d = ImageDraw.Draw(layer)
    d.rounded_rectangle(box, radius=10, fill=PANEL + (255,), outline=BRASS + (255,), width=3)
    layer.alpha_composite(art("Icon/node_shop").resize((80, 80), Image.LANCZOS), (x0 + 24, y0 + 22))
    d.text((x0 + 124, y0 + 24), "4층 · 상점", font=font(32), fill=TEXT)
    d.text((x0 + 124, y0 + 72), "지역 코인으로 삽니다. 나가면 다음 층으로 갑니다.", font=font(19), fill=DIM)
    M.draw_coins(layer, x0 + W - 26, y0 + 46, 37, size=30, color=TEXT, label="가진 코인")
    tw = 196; gap = 10; tx = x0 + (W - (4 * tw + 3 * gap)) // 2; ty = y0 + 108; boxes = []
    for k, it in enumerate(STOCK):
        b = (tx + k * (tw + gap), ty, tx + k * (tw + gap) + tw, ty + tiles_h); boxes.append(b)
        item_tile(layer, b, it, "picked" if k == 0 else "normal", foot="price", facts=facts)
    by = y0 + H - 22 - 48
    M.flat_button(layer, (x0 + 22, by, x0 + 22 + 250, by + 48), "새로고침", coins=3)
    M.flat_button(layer, (x0 + W - 22 - 190, by, x0 + W - 22, by + 48), "나가기", kind="primary")
    return box, boxes


def screen_shop(nodes, variant="S1"):
    """At the shop on floor 4 (passed 1-3, here on 12), the first offer picked. S1: the facts in every tile, no card.
    S2: tiles as now but taller (the cell at its own size), the picked offer's card inside the window under them."""
    base = shot("ko_42_shop"); layer = Image.new("RGBA", base.size, (0, 0, 0, 0))
    strip = move_potions(base, layer)
    clear_right(layer, top=84, bottom=1080, x0=962)
    layer.alpha_composite(tiled("Frame/table", 40, (952, 242)), (968, 838))
    tablet = (980, 92, 920, 734)
    draw_map(layer, tablet, nodes, current=12, passed={0, 3, 7}, scroll_floor=4)
    view = (tablet[0] + 14, tablet[1] + 14, tablet[2] - 28, tablet[3] - 28)
    shade = Image.new("RGBA", layer.size, (0, 0, 0, 0)); ImageDraw.Draw(shade).rectangle([view[0], view[1], view[0] + view[2] - 1, view[1] + view[3] - 1], fill=(0, 0, 0, SHADE))
    layer.alpha_composite(shade)
    if variant == "S1":
        shop_window(layer, view, 372, True)
        hint = "고른 아이템을 넣을 칸을 누르세요. 인벤토리에 넣기로 바로 받을 수도 있습니다."
    else:
        crd = M.card(LONGBOW, notch_x=None, with_price=False)
        box, boxes = shop_window(layer, view, 236, False, card_room=crd.size[1])
        layer.alpha_composite(crd, (boxes[0][0], boxes[0][3] + 16))
        hint = "고른 아이템을 넣을 칸을 누르세요. 인벤토리에 넣기로 바로 받을 수도 있습니다."
    panel(layer, (980, 850, 920, PLATE_H), "4층 · 상점", "지도 위의 창에서 삽니다.", [(hint, 19, TEXT)],
          MAP_BUTTONS + [(1644, 236, "인벤토리에 넣기", BUTTON, 26)])
    out = compose(base, layer, lambda o: split_table(base, o))
    out.alpha_composite(strip, (BATTLE_POTIONS_X, POTIONS[1]))
    return out


LOOT_HINT = "주울 아이템을 고른 뒤 넣을 칸을 누르세요. 찬 칸에 넣으면 원래 아이템은 인벤토리로 갑니다. 인벤토리에 넣기로 바로 받을 수도 있습니다."
LOOT_BUTTONS = [(1000, 210, "인벤토리에 넣기", QUIET, 24), (1230, 170, "인벤토리로", QUIET, 24), (1420, 210, "인벤토리 보기", QUIET, 24), (1650, 230, "두고 가기", QUIET, 24)]


def wide_card(layer, box, it, picked):
    """L1: today's text card, taller, with the cell at its own size on the left."""
    x0, y0, x1, y1 = box; d = ImageDraw.Draw(layer)
    d.rectangle(box, fill=((0x80, 0x66, 0x14) if picked else M.PANEL_LIGHT) + (255,))
    c = cell(it); layer.alpha_composite(c, (x0 + 20, y0 + (y1 - y0 - c.size[1]) // 2))
    tx = x0 + 220; d.text((tx, y0 + 18), "아이템", font=font(19), fill=DIM)
    M.draw_segments(d, tx + 90, y0 + 12, M.title_segments(it), font(26))
    act = "넣을 칸을 누르세요" if picked else "줍기"; f = font(22); l, t, r, b = d.textbbox((0, 0), act, font=f)
    d.text((x1 - 20 - (r - l) - l, y0 + 16), act, font=f, fill=TEXT)
    y = y0 + 58
    y = text_lines(d, tx, y, " / ".join(it["facts"]), font(19), TEXT, x1 - tx - 20, gap=6)
    for e in it["effects"]: y = text_lines(d, tx, y, e, font(19), TEXT, x1 - tx - 20, gap=6)
    if it["fatigue"]: text_lines(d, tx, y, f"전투마다 피로 +{it['fatigue']}", font(19), FATIGUE, x1 - tx - 20, gap=6)


def screen_loot(variant="L2"):
    """The loot of an elite fight (two drops), the first picked. L2: the drops as the shop's tiles on the map's tablet.
    L1: today's cards, as wide as the right half, taller, with the cell."""
    base = shot("ko_07_loot"); layer = Image.new("RGBA", base.size, (0, 0, 0, 0))
    strip = move_potions(base, layer)
    clear_right(layer, top=84, bottom=1080, x0=962)
    layer.alpha_composite(tiled("Frame/table", 40, (952, 242)), (968, 838))
    tablet = (980, 92, 920, 734)
    layer.alpha_composite(tiled("Frame/tablet", 40, tablet[2:]), tablet[:2])
    drops = [FANG, RUSTY]
    if variant == "L2":
        tw, th, gap = 250, 372, 24; tx = tablet[0] + (tablet[2] - (2 * tw + gap)) // 2; ty = tablet[1] + (tablet[3] - th) // 2
        for k, it in enumerate(drops):
            item_tile(layer, (tx + k * (tw + gap), ty, tx + k * (tw + gap) + tw, ty + th), it, "picked" if k == 0 else "normal", foot="take")
    else:
        y = tablet[1] + 34
        for k, it in enumerate(drops):
            wide_card(layer, (tablet[0] + 34, y, tablet[0] + tablet[2] - 34, y + 196), it, k == 0); y += 196 + 12
    panel(layer, (980, 850, 920, PLATE_H), None, None, [(LOOT_HINT, 20, DIM), (HELP, 19, TEXT)], LOOT_BUTTONS)
    out = compose(base, layer, lambda o: split_table(base, o))
    out.alpha_composite(strip, (BATTLE_POTIONS_X, POTIONS[1]))
    return out


# ---- the check: the kit drawn at today's places beside the screenshot -----------------------------------------------------------

def check():
    base = shot("ko_04_map"); layer = Image.new("RGBA", base.size, (0, 0, 0, 0))
    layer.alpha_composite(tiled("Frame/tablet", 40, (920, 414)), (980, 190))
    layer.alpha_composite(sliced("Frame/plate_label", 44, (920, 436)), (980, 632))
    button(layer, (1644, 878, 1880, 954), "전투 시작", BUTTON, size=30)
    mine = vignetted(layer); ref = base.copy()
    pairs = [(980, 190, 1900, 330), (980, 600, 1900, 700), (1600, 860, 1900, 1080)]
    rows = []
    for b in pairs:
        a = ref.crop(b); m = Image.new("RGBA", a.size, BG + (255,)); m.alpha_composite(mine.crop(b))
        row = Image.new("RGBA", (a.width, a.height * 2 + 6), (255, 0, 255, 255)); row.paste(a, (0, 0)); row.paste(m, (0, a.height + 6)); rows.append(row)
    W = max(r.width for r in rows); H = sum(r.height + 12 for r in rows)
    out = Image.new("RGBA", (W, H), (255, 0, 255, 255)); y = 0
    for r in rows: out.paste(r, (0, y)); y += r.height + 12
    out.save(HERE / "check.png"); print("check.png", out.size)


def main():
    if len(sys.argv) > 1 and sys.argv[1] == "check": check(); return
    nodes = load_map(5)
    shots = {
        "mock-map-A.png": screen_map(nodes, "A"),
        "mock-map-B.png": screen_map(nodes, "B"),
        "mock-shop-S1.png": screen_shop(nodes, "S1"),
        "mock-shop-S2.png": screen_shop(nodes, "S2"),
        "mock-loot-L2.png": screen_loot("L2"),
        "mock-loot-L1.png": screen_loot("L1"),
    }
    for name, im in shots.items(): im.convert("RGB").save(HERE / name); print(name)

    def half_of(im): return im.convert("RGB").resize((960, 540), Image.LANCZOS)
    M.sheet([("지도 · 지금", half_of(shot("ko_04_map"))),
             ("지도 · 안 A (권장): 물약 왼쪽 위, 지도 판 92~826, 패널 218", half_of(shots["mock-map-A.png"])),
             ("상점 · 지금 (물건을 고르면 카드가 창 아래에)", half_of(shot("ko_43_shop_pick"))),
             ("상점 · 안 S1 (권장): 타일에 효과까지, 카드 없음", half_of(shots["mock-shop-S1.png"])),
             ("전리품 · 지금", half_of(shot("ko_07_loot"))),
             ("전리품 · 안 L2 (권장): 상점 타일과 같은 카드", half_of(shots["mock-loot-L2.png"]))],
            2, HERE / "mock-compare.png", size=24,
            footer=["지도의 노드는 목업용 실제 생성 지도(시드 5)다. 지금 화면의 지도와 판이 달라도 배치 공식과 간격(88.5)은 게임 그대로다.",
                    "패널은 436 → 218. 제목 옆에 안내, 그 아래 두 줄, 버튼 높이 76 → 64. 표(보드 판)는 보드 아래만 남고 오른쪽 아래에 작은 판이 따로 선다(안 A)."])
    M.sheet([("지도 · 안 B: 표는 그대로, 지도 판이 표 위에 겹침", half_of(shots["mock-map-B.png"])),
             ("상점 · 안 S2: 타일은 키우되 글은 그대로, 카드는 창 안 아래에", half_of(shots["mock-shop-S2.png"])),
             ("전리품 · 안 L1: 지금의 글 카드를 키우고 칸 그림을 더함", half_of(shots["mock-loot-L1.png"]))],
            2, HERE / "mock-alternatives.png", size=24)


if __name__ == "__main__": main()
