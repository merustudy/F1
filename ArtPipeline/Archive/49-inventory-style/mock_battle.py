# -*- coding: utf-8 -*-
"""Round 49, the battle (the user, 2026-10-09: "각 목업으로 전투(약5초) 전투 목업 영상 보여줘"). Each look of
`mock_inventory_style.py` in battle for 5 s at x1: today's battle shot (20261009-s19c, ko_05) with its lower panel drawn again in
the look, the storm candle carried over, the four boards (the node map's scene with the longbow now in Ella's bag) and the two
goblins' boards. The cooldown is today's (round 48, 안 2): the item's art dark at 40%, its own colours back from the left with a
sharp edge; when lit it fires: it swells to 1.12 and is 30% brighter for a moment, and the dark comes back. Kai's buckler fires
only from the front row, so it is grey in the second. Cooldowns are the data's. Also one MP4 of the three panels stacked, and a
still of them. Drawn shapes and today's icons; no API call.
  .venv/bin/python ArtPipeline/Archive/49-inventory-style/mock_battle.py
"""
import shutil
import subprocess
import sys
from pathlib import Path
from PIL import Image, ImageChops, ImageDraw

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import mock_inventory_style as S  # noqa: E402
from mock_inventory_style import s, cell, span, it, bag, PACK, PAD, TOP, COLS, K, comp, D, text, font, rrect, mul, greyed  # noqa: E402

FPS, SECONDS = 30, 5
FRAMES = Path("/private/tmp/claude-502/-Users-funitup-Projects-F1/69777346-2ffd-4eb2-89a0-bb134dba10e2/scratchpad")
PANEL = (0, 612, 1920, 1080)
ENEMY_X = [1253, 1443]                    # the two goblins' boards, as today
CANDLE = (878, 632, 1180, 1012)           # the storm candle with its light and its two lines, from today's shot
UNDER_CANDLE = (53, 51, 53)               # today's panel there
POP_UP, POP_T = 0.07, 0.30                # the swell's rise and its whole time (round 48)
SWELL = 0.12

COOLDOWN = {"longsword": 3.0, "mace": 3.2, "healing_staff": 4.0, "fire_staff": 3.5, "dagger": 1.5, "longbow": 3.0,
            "buckler": 5.0, "herb_pouch": 5.0, "ember_flask": 4.5, "rusty_blade": 2.8}   # ItemData.CooldownMs
PARTY = [
    dict(num=4, name="미라", bags=[PACK], items=[it("fire_staff"), it("ember_flask", 0, 1, "bronze")]),
    dict(num=3, name="엘라", bags=[PACK], items=[it("healing_staff"), it("longbow", 0, 1)]),
    dict(num=2, name="카이", bags=[PACK, bag("belt_pouch", 0, 3)],
         items=[it("longsword", 0, 0, "bronze"), it("dagger", 0, 1, "bronze"), dict(it("buckler", 2, 1, "silver"), unusable=True), it("herb_pouch", 0, 3)]),
    dict(num=1, name="로언", bags=[PACK, bag("leather_pouch", 0, 3)],
         items=[it("longsword", 0, 0, "silver"), it("herb_pouch", 0, 1, "gold"), it("dagger", 2, 1, rot=True), it("ember_flask", 0, 2), it("mace", 0, 3, "bronze")]),
]
ENEMIES = [dict(num=n, name="고블린 약탈자", enemy=True, bags=[], items=[dict(it("rusty_blade"), flip=True)]) for n in (2, 3)]
# How far each item's cooldown has run at t = 0, so that firings spread over the five seconds.
PHASE = {("미라", "fire_staff"): 0.55, ("미라", "ember_flask"): 0.30, ("엘라", "healing_staff"): 0.85, ("엘라", "longbow"): 0.20,
         ("카이", "longsword"): 0.70, ("카이", "dagger"): 0.10, ("카이", "herb_pouch"): 0.45, ("로언", "longsword"): 0.35,
         ("로언", "herb_pouch"): 0.75, ("로언", "dagger"): 0.60, ("로언", "ember_flask"): 0.05, ("로언", "mace"): 0.50,
         (2, "rusty_blade"): 0.40, (3, "rusty_blade"): 0.80}
SHORT = {"D2": "1. 디아블로 2", "BB": "2. 백팩 배틀즈", "C": "3. 원정 짐칸 (권장)"}


def half(im): return im.resize((im.width // K, im.height // K), Image.LANCZOS)


def strongest(im):
    r, g, b = im.split(); return ImageChops.lighter(ImageChops.lighter(r, g), b)


def candle(img, shot):
    """Today's candle carried onto the new panel. Its light is what it adds to today's panel colour, added to the new panel; the
    candle, its flame, its cup and its two lines (far from the panel colour) are taken as they are."""
    crop = shot.crop(CANDLE); base = Image.new("RGB", crop.size, UNDER_CANDLE); size = (s(crop.width), s(crop.height))
    plus = ImageChops.subtract(crop, base); minus = ImageChops.subtract(base, crop)
    solid = ImageChops.lighter(strongest(plus).point(lambda v: max(0, min(255, (v - 70) * 255 // 50))),
                               strongest(minus).point(lambda v: max(0, min(255, (v - 15) * 255 // 25))))
    at = (s(CANDLE[0]), s(CANDLE[1])); region = img.crop((at[0], at[1], at[0] + size[0], at[1] + size[1]))
    lit = ImageChops.subtract(ImageChops.add(region, plus.resize(size, Image.LANCZOS)), minus.resize(size, Image.LANCZOS))
    img.paste(Image.composite(crop.resize(size, Image.LANCZOS), lit, solid.resize(size, Image.LANCZOS)), at)


def decor(st, img):
    if isinstance(st, S.Backpack):
        for x, y in ((24, 760), (24, 880), (1700, 760), (1700, 880)): st.sketch(img, x, y, 80)
        d = D(img)   # a dark wooden plaque under the candle's two lines, which are light (on paper they would not read)
        S.drop(img, (872, 930, 1048, 1008), 6, alpha=110, off=(2, 3), blur=3)
        S.fill_tex(img, (872, 930, 1048, 1008), (70, 46, 26), amp=14, streak=True, radius=6)
        rrect(D(img), (872, 930, 1048, 1008), 6, outline=st.INK, w=2)
    elif isinstance(st, S.Hold):
        d = D(img); y = 918
        text(d, (26, y - 24), "종류", font(13), st.DIM, "lm")
        for k, (kind, col) in enumerate(st.KIND.items()):
            yy = y + 24 * k; rrect(d, (26, yy - 8, 42, yy + 8), 3, fill=col, outline=S.shade(col, 0.55), w=1)
            text(d, (50, yy + 0.5), st.KIND_NAME[kind], font(14), st.CREAM, "lm")


class Unit:
    """One item in battle at the screen's size: its art (what darkens, lights and swells) and what lies over it (tier, tags)."""

    def __init__(self, x0, m, i, art, over):
        X, Y = cell(x0, TOP, i["x"], i["y"])
        self.xy = (X - PAD, Y - PAD); self.w = span(i["w"])
        self.art, self.over = half(art), half(over); self.dark = mul(self.art, S.DARK)
        self.usable = not i.get("unusable")
        self.period = COOLDOWN[i["id"]]; self.phase = PHASE.get((m["num"] if m.get("enemy") else m["name"], i["id"]), 0.0)

    def reveal(self, c):
        out = self.dark.copy(); cut = PAD + int(round(self.w * c))
        out.paste(self.art.crop((0, 0, cut, self.art.height)), (0, 0)); return out

    def layer(self, t):
        """(image, offset) at t seconds."""
        e = t + self.phase * self.period; c = (e % self.period) / self.period
        since = (e % self.period) if e >= self.period else None
        if since is None or since >= POP_T: return self.reveal(c), (0, 0)
        bump = max(0.0, 1 - abs(since - POP_UP) / POP_UP) if since < 2 * POP_UP else 0.0
        lit = mul(self.art, 1 + S.FLASH * bump)
        if since > 0.18: lit = Image.blend(lit, self.reveal(c), (since - 0.18) / (POP_T - 0.18))
        k = 1 + SWELL * ((1 - (1 - since / POP_UP) ** 2) if since < POP_UP else (1 - (since - POP_UP) / (POP_T - POP_UP)) ** 2)
        w, h = lit.size; nw, nh = round(w * k), round(h * k)
        return lit.resize((nw, nh), Image.BILINEAR), ((w - nw) // 2, (h - nh) // 2)


def stage(st):
    shot = Image.open(S.SHOTS / "ko_05_battle.png").convert("RGB")
    img = shot.resize((s(1920), s(1080)), Image.LANCZOS)
    st.panel(img, PANEL); decor(st, img); candle(img, shot)
    units = []
    for x0, m in list(zip(COLS, PARTY)) + list(zip(ENEMY_X, ENEMIES)):
        st.frame(img, x0, TOP, m["bags"], False); st.bags(img, x0, TOP, m["bags"])
        for i in m["items"]:
            X, Y = cell(x0, TOP, i["x"], i["y"])
            if m.get("enemy") and isinstance(st, S.Diablo):   # Diablo's items sit in a well; an enemy has no bag, so a bare one
                st.well(img, X, Y, i["w"], i["h"], rim_tint=(110, 40, 32)); st.squares(img, X, Y, i["w"], i["h"])
            under, art, over = st.parts(i, battle=True)
            if i.get("unusable"): under, art = greyed(under), greyed(art)
            comp(img, under, (s(X - PAD), s(Y - PAD)))
            units.append(Unit(x0, m, i, art, over))
        st.plate(img, x0, TOP - 24, m)
    return half(img), units


def frame(st, still, units, t):
    im = still.copy()
    for u in units:
        lay, off = u.layer(t) if u.usable else (u.art, (0, 0))
        im.paste(lay, (u.xy[0] + off[0], u.xy[1] + off[1]), lay)
        im.paste(u.over, u.xy, u.over)
    d = ImageDraw.Draw(im, "RGBA"); f = S.ImageFont.truetype(str(S.PRET), 19)
    label = f"{SHORT[st.key]} · x1 · {t:4.2f}초"
    d.rounded_rectangle([1596, 1028, 1906, 1068], radius=6, fill=(14, 12, 10, 225), outline=(120, 100, 64, 255))
    d.text((1751, 1048), label, font=f, fill=(238, 232, 220), anchor="mm")
    return im


def video(st):
    still, units = stage(st)
    tmp = FRAMES / f"frames-{st.key}"; shutil.rmtree(tmp, ignore_errors=True); tmp.mkdir(parents=True)
    keep = None
    for k in range(FPS * SECONDS):
        t = k / FPS; im = frame(st, still, units, t); im.save(tmp / f"{k:05d}.png")
        if k == 49: keep = im                       # t = 1.63 s: three items swelling at once
    out = HERE / f"mock-{st.key}-battle.mp4"
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-framerate", str(FPS), "-i", str(tmp / "%05d.png"),
                    "-c:v", "libx264", "-pix_fmt", "yuv420p", "-crf", "18", str(out)], check=True)
    shutil.rmtree(tmp); print(out.name)
    return keep


def main():
    looks = [S.Diablo(), S.Backpack(), S.Hold()]
    stills = [(st.title, video(st)) for st in looks]
    ins = sum([["-i", str(HERE / f"mock-{st.key}-battle.mp4")] for st in looks], [])
    chains = ";".join(f"[{n}:v]crop=1920:480:0:600,scale=1280:-2[v{n}]" for n in range(3))
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", *ins, "-filter_complex", f"{chains};[v0][v1][v2]vstack=inputs=3",
                    "-c:v", "libx264", "-pix_fmt", "yuv420p", "-crf", "18", str(HERE / "mock-battle-compare.mp4")], check=True)
    print("mock-battle-compare.mp4")
    S.sheet([(t, im.crop((0, 560, 1920, 1080))) for t, im in stills], 1, HERE / "mock-battle-compare.png", scale=0.62, label=22,
            footer=["t = 1.63초의 한 장면. 쿨다운은 지금 게임의 안 2(제 색이 왼쪽부터 또렷한 경계로, 다 차면 1.12배와 30% 밝게).",
                    "카이(2열)의 버클러는 맨 앞에서만 발동하므로 회색. 쿨다운은 데이터의 값. 영상은 mock-*-battle.mp4(5초, x1)."])


if __name__ == "__main__":
    main()
