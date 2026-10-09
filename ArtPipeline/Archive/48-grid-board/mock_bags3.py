# -*- coding: utf-8 -*-
"""Round 48, fourth ask (2026-10-08): "쿨타임은 아이템이 이전처럼 점차 금빛으로 가로로 움직이면서 밝아지며 동작하는 것으로 다시 목업".
The cooldown as today's light that runs from left to right, but on the item: the item (dark at the start) is lit in warm gold
from its left edge as it charges, with a bright front; when full it swells a little and fires (the second ask's swell). On the
third ask's ground (안 2: no cream, the item's squares as one dark piece). Two ways: A = the light only inside the item's
shape, B = the light fills the item's piece as today's cell light did, and the item brightens with it. No API call.
  .venv/bin/python ArtPipeline/Archive/48-grid-board/mock_bags3.py
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter, ImageChops

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import mock_bags as B    # noqa: E402  (the cooldown's swell, battle scene, MP4 writer)
import mock_bags2 as B2  # noqa: E402  (the ground without cream: 안 2)

G = B.G; M = B.M; font = G.font
TEXT, DIM, CHARGE = G.TEXT, G.DIM, B.CHARGE
DARK = 0.40              # the third ask's start on the dark ground
FEATHER = 8              # the soft edge between the lit and the dark part (px)
FRONT_GLOW = 18          # the glow behind the front (px)
GOLD_TINT = 0.24         # how gold the lit part is (0 = the item's own colours)

def lit_of(p):
    """The item's sprite in the light: its own colours drawn a little toward the candle's gold."""
    if not hasattr(p, "lit"):
        a = p.sprite.split()[3]
        rgb = Image.blend(p.sprite.convert("RGB"), Image.new("RGB", p.size, CHARGE), GOLD_TINT)
        p.lit = rgb.convert("RGBA"); p.lit.putalpha(a)
    return p.lit


def dark_of(p):
    if getattr(p, "dark_at", None) != DARK: p.dark, p.dark_at = B.shaded(p.sprite, DARK), DARK
    return p.dark


def ramp(W, H, cut, feather):
    """255 left of the front, 0 right of it, a soft step `feather` wide just before it."""
    m = Image.new("L", (W, H), 0); d = ImageDraw.Draw(m)
    if cut <= 0: return m
    d.rectangle([0, 0, max(0, cut - feather), H], fill=255)
    for k in range(feather):
        x = cut - feather + k
        if 0 <= x < W: d.line([(x, 0), (x, H)], fill=int(255 * (1 - k / feather)))
    return m


def front_band(W, H, cut, alpha_scale=1.0):
    """The front: a bright line and a glow fading to the left (today's FrontLine and FrontGlow)."""
    band = Image.new("RGBA", (W, H), CHARGE + (0,)); d = ImageDraw.Draw(band)
    for k in range(FRONT_GLOW):
        x = int(cut) - k
        if 0 <= x < W: d.line([(x, 0), (x, H)], fill=CHARGE + (int(90 * alpha_scale * (1 - k / FRONT_GLOW)),))
    if 0 <= cut < W: d.line([(cut, 0), (cut, H)], fill=CHARGE + (int(240 * alpha_scale),), width=2)
    return band


def sweep_item(img, xy, p, c, s, mode, enabled=True):
    x, y = xy; W, H = p.size
    firing = s is not None and s < B.POP_T
    if not enabled or firing:
        old = B.DARK; B.DARK = DARK
        ORIG_DRAW(img, xy, p, "v1", c, s, enabled)       # the swell and its glow as in the second ask; grey when unusable
        B.DARK = old; return
    cut = int(round(W * c))
    if mode == "sweepB":
        # the piece lights from the left like today's cell (gold wash, dark part stays the ground), the front across it
        lay = Image.new("RGBA", (W, H), (0, 0, 0, 0)); d = ImageDraw.Draw(lay)
        d.rounded_rectangle([0, 0, W - 1, H - 1], radius=5, fill=CHARGE + (44,))
        mask = Image.new("L", (W, H), 0); ImageDraw.Draw(mask).rounded_rectangle([0, 0, W - 1, H - 1], radius=5, fill=255)
        left = ImageChops.multiply(ramp(W, H, cut, 6), mask); lay.putalpha(ImageChops.multiply(lay.split()[3], left))
        band = front_band(W, H, cut); band.putalpha(ImageChops.multiply(band.split()[3], mask))
        img.alpha_composite(lay, (x, y))
        if 0 < cut < W: img.alpha_composite(band, (x, y))
    m = ramp(W, H, cut, FEATHER)
    sp = Image.composite(lit_of(p), dark_of(p), m); sp.putalpha(p.sprite.split()[3])
    img.alpha_composite(sp, (x, y))
    if mode == "sweepA" and 0 < cut < W:
        band = front_band(W, H, cut); band.putalpha(ImageChops.multiply(band.split()[3], p.glow_mask))
        img.alpha_composite(band, (x, y))
    img.alpha_composite(p.tags, (x, y))


ORIG_DRAW = B.draw_item


def dispatch(img, xy, p, variant, c, s, enabled=True):
    if variant in ("sweepA", "sweepB"): return sweep_item(img, xy, p, c, s, variant, enabled)
    return ORIG_DRAW(img, xy, p, variant, c, s, enabled)


B.draw_item = dispatch      # battle_frame looks draw_item up in its module at call time

PANELS = [("old", "now", "지금 게임: 크림 칸에 금빛이 왼→오, 발동 때 1.3배와 칸의 번쩍임"),
          ("new", "v1", "직전 안 1: 아이템이 고르게 밝아짐 → 1.12배"),
          ("new", "sweepA", "새 안 A (권장): 아이템 모양 안에서 금빛이 왼→오로 밝힘 → 1.12배"),
          ("new", "sweepB", "새 안 B: 아이템의 조각까지 금빛이 왼→오(이전의 칸 빛) → 1.12배")]


def scenes():
    cols, party_old, enemies_old = B.battle_scene()
    bg_old = B.battle_background(cols)
    bg_new = B2.battle_ground(B2.LOOKS[1], cols, {0: ("dagger",)})
    party_new, enemies_new = B2.battle_items(cols)
    return cols, (bg_old, party_old, enemies_old), (bg_new, party_new, enemies_new)


def compare_video():
    cols, old, new = scenes()
    crop = B.CMP; w, h = crop[2] - crop[0], crop[3] - crop[1]; lab = 64; gap = 16
    W = len(PANELS) * (w + gap) + gap; H = h + lab + 46

    def panel(kind, variant, t):
        bg, party, enemies = old if kind == "old" else new
        B.DARK = 0.20 if kind == "old" else DARK
        im = B.battle_frame(bg, cols, party, enemies, t, variant); B.DARK = 0.20
        return im.crop(crop)

    def frame(k):
        t, speed = B.timeline(k)
        out = Image.new("RGBA", (W, H), (18, 18, 18, 255)); d = ImageDraw.Draw(out)
        for n, (kind, variant, label) in enumerate(PANELS):
            x = gap + n * (w + gap)
            for j, ln in enumerate(M.wrap_segments([(label, TEXT)], font(18), w)): M.draw_segments(d, x, 8 + j * 24, ln, font(18))
            out.alpha_composite(panel(kind, variant, t), (x, lab))
        d.text((gap, H - 38), f"{speed}   t = {t:4.2f}초   ·   새 바탕(크림 없음, 아이템의 칸은 한 조각), 시작 어둠 40%. 카이(2열)와 로언(1열).", font=font(17), fill=DIM)
        return out
    B.write_video(frame, 16 * B.FPS, HERE / "mock-BC3-cooldown-compare.mp4")
    frame(int(1.6 * B.FPS)).convert("RGB").save(HERE / "mock-BC3-cooldown-compare.png"); print("mock-BC3-cooldown-compare.png")


def battle_video():
    cols, _, (bg, party, enemies) = scenes()

    def frame(k):
        t, speed = B.timeline(k)
        B.DARK = DARK; im = B.battle_frame(bg, cols, party, enemies, t, "sweepA").crop(B.BATTLE_CROP); B.DARK = 0.20
        d = ImageDraw.Draw(im); d.rounded_rectangle([1000, 360, 1335, 400], radius=6, fill=G.INK + (220,))
        d.text((1012, 368), f"새 안 A · {speed} · {t:4.2f}초", font=font(18), fill=TEXT)
        return im
    B.write_video(frame, 16 * B.FPS, HERE / "mock-BC3-battle.mp4")
    frame(int(2.3 * B.FPS)).convert("RGB").save(HERE / "mock-BC3-battle.png"); print("mock-BC3-battle.png")


def strip():
    """Moments of one cooldown for a long item (longsword 3x1) and a turned one (dagger 1x2), per way."""
    sword_old = B.Parts(G.it("longsword", 0, 0)); dagger_old = B.Parts(G.it("dagger", 0, 0, rot=True))
    sword = B2.item_parts(G.it("longsword", 0, 0), map_tags=False); dagger = B2.item_parts(G.it("dagger", 0, 0, rot=True), map_tags=False)
    moments = [("0%", 0.0, None), ("30%", 0.3, None), ("60%", 0.6, None), ("90%", 0.9, None), ("발동 0.07초(가장 큼)", 0.0, 0.07), ("발동 0.30초(새 쿨다운)", 0.0, 0.30)]
    cw, ch, lab = 282, 150, 240
    out = Image.new("RGBA", (lab + len(moments) * cw + 20, 60 + len(PANELS) * (ch + 12) + 50), (18, 18, 18, 255)); d = ImageDraw.Draw(out)
    for n, (name, _, _) in enumerate(moments): d.text((lab + n * cw, 18), name, font=font(17), fill=DIM)
    for r, (kind, variant, label) in enumerate(PANELS):
        y = 60 + r * (ch + 12)
        for j, ln in enumerate(M.wrap_segments([(label, TEXT)], font(16), lab - 20)): M.draw_segments(d, 12, y + j * 20, ln, font(16))
        for n, (_, c, s) in enumerate(moments):
            cell = Image.new("RGBA", (cw - 10, ch), (50, 48, 50, 255))
            for p_old, p_new, xy in ((sword_old, sword, (10, 20)), (dagger_old, dagger, (206, 16))):
                if kind == "old": B.DARK = 0.20; dispatch(cell, xy, p_old, variant, c, s)
                else:
                    W_, H_ = p_new.size; ImageDraw.Draw(cell).rounded_rectangle([xy[0], xy[1], xy[0] + W_ - 1, xy[1] + H_ - 1], radius=5, fill=B2.PIECE + (255,), outline=B2.PIECE_LINE + (255,))
                    B.DARK = DARK; dispatch(cell, xy, p_new, variant, c, s)
                B.DARK = 0.20
            out.alpha_composite(cell, (lab + n * cw, y))
    d.text((12, out.height - 40), "롱소드(3×1)와 돌린 단검(1×2). 금빛은 모든 아이템에서 가로(왼→오)로 지나간다. 발동하는 순간 쿨다운은 0부터 다시 찬다.", font=font(17), fill=DIM)
    out.convert("RGB").save(HERE / "mock-BC3-cooldown-frames.png"); print("mock-BC3-cooldown-frames.png", out.size)


if __name__ == "__main__":
    strip(); compare_video(); battle_video()
