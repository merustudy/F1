# -*- coding: utf-8 -*-
"""Round 48, fifth ask (2026-10-08): "금빛 없이 그냥 아이템 아이콘이 가로로 밝아지면서(백팩 배틀즈는 세로인데 이걸 가로로 바꾼 버전)
완전 밝아지면 동작하는 걸로". The item's own colours come back from its left edge as it charges (no gold, no front line), and
when the whole item is lit it fires: it swells a little (the second ask) with no coloured glow. On the third ask's ground
(no cream, the item's squares as one dark piece, dark 40%). No API call.
  .venv/bin/python ArtPipeline/Archive/48-grid-board/mock_bags4.py
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import mock_bags3 as B3  # noqa: E402  (the fourth ask: the gold sweep, its panels, scenes; installs its own draw_item)

B = B3.B; B2 = B3.B2; G = B.G; M = B.M; font = G.font
TEXT, DIM = G.TEXT, G.DIM
DARK = B3.DARK                       # 40%
FLASH = 0.30                         # 안 1: at the swell's top the item is 30% brighter than its own colours, then settles
SOFT, HARD = 10, 1                   # the edge between the lit and the dark part (px)


def bright(p, f):
    if f <= 1.001: return p.sprite
    rgb = p.sprite.convert("RGB").point(lambda v: min(255, int(v * f))); out = rgb.convert("RGBA"); out.putalpha(p.sprite.split()[3]); return out


def revealed(p, c, edge):
    W, H = p.size
    sp = Image.composite(p.sprite, B3.dark_of(p), B3.ramp(W, H, int(round(W * c)), edge)); sp.putalpha(p.sprite.split()[3]); return sp


def plain_item(img, xy, p, c, s, edge, flash, enabled=True):
    x, y = xy
    if not enabled:
        old = B.DARK; B.DARK = DARK; B3.ORIG_DRAW(img, xy, p, "v1", c, s, False); B.DARK = old; return
    if s is not None and s < B.POP_T:
        # Fired: the whole item lit, swelled to 1.12 (and, in 안 1, a moment brighter than itself); from 0.18 s it settles back
        # into the new cooldown's dark, its left edge already coming back.
        bump = 1 - abs(s - B.POP_UP) / B.POP_UP if s < 2 * B.POP_UP else 0.0
        lit = bright(p, 1 + flash * max(0.0, bump))
        if s > 0.18: lit = Image.blend(lit, revealed(p, c, edge), (s - 0.18) / (B.POP_T - 0.18))
        spk, off = B.swelled(lit, B.pop_scale(s)); img.alpha_composite(spk, (x + off[0], y + off[1]))
    else:
        img.alpha_composite(revealed(p, c, edge), (x, y))
    img.alpha_composite(p.tags, (x, y))


VARIANTS = {"plain1": (SOFT, FLASH), "plain2": (HARD, FLASH), "plain3": (SOFT, 0.0)}
PREV = B.draw_item


def dispatch(img, xy, p, variant, c, s, enabled=True):
    if variant in VARIANTS:
        edge, flash = VARIANTS[variant]; return plain_item(img, xy, p, c, s, edge, flash, enabled)
    return PREV(img, xy, p, variant, c, s, enabled)


B.draw_item = dispatch

PANELS = [("new", "sweepA", "직전 새 안 A: 금빛이 왼→오"),
          ("new", "plain1", "안 1 (권장): 금빛 없이 제 색이 왼→오로 드러남(부드러운 경계), 다 밝으면 1.12배 + 잠깐 더 밝게"),
          ("new", "plain2", "안 2: 안 1과 같고 경계만 또렷한 선"),
          ("new", "plain3", "안 3: 안 1과 같고 발동은 커짐만(더 밝아지지 않음)")]


def compare_video():
    cols, _, new = B3.scenes()
    crop = B.CMP; w, h = crop[2] - crop[0], crop[3] - crop[1]; lab = 64; gap = 16
    W = len(PANELS) * (w + gap) + gap; H = h + lab + 46
    bg, party, enemies = new

    def frame(k):
        t, speed = B.timeline(k)
        out = Image.new("RGBA", (W, H), (18, 18, 18, 255)); d = ImageDraw.Draw(out)
        for n, (_, variant, label) in enumerate(PANELS):
            x = gap + n * (w + gap)
            for j, ln in enumerate(M.wrap_segments([(label, TEXT)], font(17), w)): M.draw_segments(d, x, 6 + j * 22, ln, font(17))
            B.DARK = DARK; im = B.battle_frame(bg, cols, party, enemies, t, variant); B.DARK = 0.20
            out.alpha_composite(im.crop(crop), (x, lab))
        d.text((gap, H - 38), f"{speed}   t = {t:4.2f}초   ·   크림 없는 바탕(아이템의 칸은 한 조각), 시작 어둠 40%. 카이(2열)와 로언(1열). 백팩 배틀즈의 세로 차오름을 가로로.", font=font(17), fill=DIM)
        return out
    B.write_video(frame, 16 * B.FPS, HERE / "mock-BC4-cooldown-compare.mp4")
    frame(int(1.6 * B.FPS)).convert("RGB").save(HERE / "mock-BC4-cooldown-compare.png"); print("mock-BC4-cooldown-compare.png")


def battle_video():
    cols, _, (bg, party, enemies) = B3.scenes()

    def frame(k):
        t, speed = B.timeline(k)
        B.DARK = DARK; im = B.battle_frame(bg, cols, party, enemies, t, "plain1").crop(B.BATTLE_CROP); B.DARK = 0.20
        d = ImageDraw.Draw(im); d.rounded_rectangle([1000, 360, 1335, 400], radius=6, fill=G.INK + (220,))
        d.text((1012, 368), f"안 1 · {speed} · {t:4.2f}초", font=font(18), fill=TEXT)
        return im
    B.write_video(frame, 16 * B.FPS, HERE / "mock-BC4-battle.mp4")
    frame(int(2.3 * B.FPS)).convert("RGB").save(HERE / "mock-BC4-battle.png"); print("mock-BC4-battle.png")


def strip():
    sword = B2.item_parts(G.it("longsword", 0, 0), map_tags=False); dagger = B2.item_parts(G.it("dagger", 0, 0, rot=True), map_tags=False)
    moments = [("0%", 0.0, None), ("30%", 0.3, None), ("60%", 0.6, None), ("90%", 0.9, None), ("다 밝음 → 발동 0.07초", 0.0, 0.07),
               ("발동 0.18초", 0.06, 0.18), ("발동 0.30초(새 쿨다운)", 0.1, 0.30)]
    cw, ch, lab = 282, 150, 240
    out = Image.new("RGBA", (lab + len(moments) * cw + 20, 60 + len(PANELS) * (ch + 12) + 50), (18, 18, 18, 255)); d = ImageDraw.Draw(out)
    for n, (name, _, _) in enumerate(moments): d.text((lab + n * cw, 18), name, font=font(17), fill=DIM)
    for r, (_, variant, label) in enumerate(PANELS):
        y = 60 + r * (ch + 12)
        for j, ln in enumerate(M.wrap_segments([(label, TEXT)], font(16), lab - 20)): M.draw_segments(d, 12, y + j * 20, ln, font(16))
        for n, (_, c, s) in enumerate(moments):
            cell = Image.new("RGBA", (cw - 10, ch), (50, 48, 50, 255))
            for p, xy in ((sword, (10, 20)), (dagger, (206, 16))):
                W_, H_ = p.size
                ImageDraw.Draw(cell).rounded_rectangle([xy[0], xy[1], xy[0] + W_ - 1, xy[1] + H_ - 1], radius=5, fill=B2.PIECE + (255,), outline=B2.PIECE_LINE + (255,))
                B.DARK = DARK; dispatch(cell, xy, p, variant, c, s); B.DARK = 0.20
            out.alpha_composite(cell, (lab + n * cw, y))
    d.text((12, out.height - 40), "롱소드(3×1)와 돌린 단검(1×2). 모든 아이템이 가로(왼→오)로 밝아진다. 발동 뒤 0.18초부터 새 쿨다운의 어둠으로 돌아가며 왼쪽부터 다시 드러난다.", font=font(17), fill=DIM)
    out.convert("RGB").save(HERE / "mock-BC4-cooldown-frames.png"); print("mock-BC4-cooldown-frames.png", out.size)


if __name__ == "__main__":
    strip(); compare_video(); battle_video()
