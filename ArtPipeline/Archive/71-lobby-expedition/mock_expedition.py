"""Round 71, fifth instruction: the expedition screen on the generated map (one call, 2026-10-16). The painting is scaled to the
screen's height (1536 -> 1080, so 1620 wide) and sits at the left; its dark right third lies under the details panel. The three
places get markers where the painting put them. E1 forms the party in the strip at the bottom (recommended), E2 shows it only.
  .venv/bin/python ArtPipeline/Archive/71-lobby-expedition/mock_expedition.py
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import mock_lobby as m  # noqa: E402

W, H = m.W, m.H
PAINT = HERE.parents[1] / "output/background/expedition_map.png"
SCALE = H / 1536                                  # 0.703: the painting's height becomes the screen's
PLACES = {"본부": (202, 752), "버려진 광산": (614, 323), "심연": (873, 113)}


def plate(d, cx, cy, s, px=24, pad=12, color=m.TEXT, line=m.LINE, sub=None, fill=(0x0E, 0x0D, 0x0C)):
    w = max(m.textw(s, px), m.textw(sub, px - 6) if sub else 0) + pad * 2; h = px + 18 + (px - 2 if sub else 0)
    d.rectangle([cx - w / 2, cy - h / 2, cx + w / 2, cy + h / 2], fill=fill, outline=line, width=2)
    m.text(d, (cx, cy - (px / 2 if sub else 0)), s, px, color, "mm")
    if sub:
        m.text(d, (cx, cy + px / 2 + 2), sub, px - 6, m.DIM, "mm")


def translucent(img, box, alpha=225):
    layer = Image.new("RGBA", (W, H), (0, 0, 0, 0)); ld = ImageDraw.Draw(layer)
    ld.rectangle(box, fill=m.PANEL + (alpha,)); img.alpha_composite(layer)


def base():
    paint = Image.open(PAINT).convert("RGB").resize((round(2304 * SCALE), H), Image.LANCZOS)
    img = Image.new("RGBA", (W, H), m.BG + (255,)); img.paste(paint, (0, 0))
    d = ImageDraw.Draw(img)
    # markers: the town (home), the mine (chosen), the abyss (locked)
    x, y = PLACES["본부"]
    d.ellipse([x - 14, y - 14, x + 14, y + 14], fill=m.QUIET, outline=m.BONE, width=3)
    plate(d, x + 66, y, "본부", 20)
    x, y = PLACES["버려진 광산"]
    d.ellipse([x - 40, y - 40, x + 40, y + 40], outline=m.SELECTED, width=4)
    d.ellipse([x - 26, y - 26, x + 26, y + 26], fill=m.PANEL_LIGHT, outline=m.BRASS, width=4)
    plate(d, x, y + 74, "버려진 광산", 24, line=m.BRASS, sub="클리어 1회 · 5일")
    x, y = PLACES["심연"]
    d.ellipse([x - 26, y - 26, x + 26, y + 26], fill=m.SLOT, outline=m.LINE, width=4)
    d.rectangle([x - 9, y - 1, x + 9, y + 11], fill=m.DIM); d.arc([x - 7, y - 13, x + 7, y + 3], 180, 360, fill=m.DIM, width=3)
    plate(d, x + 150, y + 10, "심연 (최종 던전)", 22, color=m.DIM, sub="잠김 · 광산 2회 클리어")
    # the details panel over the painting's dark right third
    translucent(img, (1320, 120, 1880, 760)); d = ImageDraw.Draw(img)
    m.text(d, (1346, 160), "원정", 30, m.DIM, "lm"); m.text(d, (1346, 226), "버려진 광산", 40, m.TEXT, "lm")
    rows = [("지역 속성", "신속"), ("층", "15층 + 보스"), ("소요", "5일 (→ 17일차)"), ("시작 포션", "치유 2"), ("클리어", "1회"), ("보상", "금 (층마다) · 경험치")]
    for i, (k, v) in enumerate(rows):
        yy = 300 + i * 52
        m.text(d, (1346, yy), k, 24, m.DIM, "lm"); m.text(d, (1854, yy), v, 24, m.TEXT, "rm")
    m.text(d, (1346, 660), "심연을 열려면 광산을 2회 클리어", 20, m.DIM, "lm")
    m.text(d, (1346, 700), "100일을 넘기면 심연의 보스와 강제로 싸운다", 20, m.DIM, "lm")
    m.header(img, "원정", right="로비로")
    return img


def e1():
    img = base(); translucent(img, (40, 780, 1880, 1040)); d = ImageDraw.Draw(img)
    m.text(d, (64, 812), "파티 — 로스터에서 눌러 넣고, 열을 눌러 자리를 바꾼다", 24, m.DIM, "lm")
    for r in range(1, 5):
        x = 64 + (r - 1) * 200; who = m.PARTY.get(r)
        m.panel(d, (x, 840, x + 184, 1020), m.SELECTED if who else m.SLOT, outline=m.BRASS if who else m.LINE, width=2)
        m.text(d, (x + 92, 862), f"{r}열", 20, m.BONE if who else m.DIM, "mm"); m.text(d, (x + 92, 920), who or "비었음", 26, m.TEXT if who else m.DIM, "mm")
        if who:
            mm = next(q for q in m.ROSTER if q[0] == who)
            m.text(d, (x + 92, 956), f"{mm[1]} · Lv {mm[2]}", 18, m.DIM, "mm"); m.pips(d, x + 22, 984, 140, mm[5], h=10, gap=3)
    for i, mm in enumerate(m.ROSTER):
        x = 900 + (i % 3) * 230; y = 840 + (i // 3) * 92; on = mm[8] > 0
        m.panel(d, (x, y, x + 214, y + 80), m.PANEL_LIGHT if not on else m.PANEL, outline=m.BRASS if on else None, width=2)
        m.text(d, (x + 14, y + 24), mm[0], 22, m.TEXT if not on else m.DIM, "lm"); m.text(d, (x + 200, y + 24), f"Lv {mm[2]}", 18, m.BONE, "rm")
        m.pips(d, x + 14, y + 50, 186, mm[5], h=10, gap=3)
    m.text(d, (1600, 840), "출발할 수 있습니다", 22, m.GOOD, "lm"); m.button(d, (1600, 880, 1854, 1000), "출발", m.BUTTON, 34)
    return img.convert("RGB")


def e2():
    img = base(); translucent(img, (40, 780, 1880, 1040)); d = ImageDraw.Draw(img)
    m.text(d, (64, 812), "파티 (막사에서 꾸린다)", 24, m.DIM, "lm")
    for r in range(1, 5):
        x = 64 + (r - 1) * 300; who = m.PARTY.get(r); mm = next(q for q in m.ROSTER if q[0] == who)
        m.panel(d, (x, 850, x + 280, 1010), m.PANEL_LIGHT)
        m.text(d, (x + 20, 880), f"{r}열", 20, m.DIM, "lm"); m.text(d, (x + 80, 880), who, 26, m.TEXT, "lm")
        m.text(d, (x + 20, 920), f"{mm[1]} · Lv {mm[2]}", 20, m.DIM, "lm"); m.pips(d, x + 20, 960, 240, mm[5], h=12, gap=3)
    m.text(d, (1600, 840), "출발할 수 있습니다", 22, m.GOOD, "lm"); m.button(d, (1600, 880, 1854, 1000), "출발", m.BUTTON, 34)
    return img.convert("RGB")


def main():
    outs = {"expedition-painted-E1": e1(), "expedition-painted-E2": e2()}
    for k, im in outs.items():
        im.save(HERE / f"mock-{k}.png"); print(f"mock-{k}.png")
    m.sheet([("E1 (권장): 지도 그림 + 던전 정보 + 아래 띠에서 파티를 꾸림", outs["expedition-painted-E1"]),
             ("E2: 같은 지도와 정보, 파티는 막사에서 꾸리고 여기서는 보기만", outs["expedition-painted-E2"])], 2, HERE / "mock-compare-expedition-painted.png",
            footer="지도: output/background/expedition_map.png (생성 1회). 화면 높이에 맞춰 1620 폭으로 두고 오른쪽의 어두운 3분의 1 위에 정보 패널. 표식: 본부·버려진 광산(고름)·심연(잠김).")


if __name__ == "__main__":
    main()
