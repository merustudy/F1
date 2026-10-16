"""Stage 25 (the end of a run): the result window over the town, as it will look once the lobby is the town (stage 26). Three states:
a victory, a defeat at the death limit, and the lobby past the last day with only the final stand left (the gate lit, the rest barred).
No API call; the town is round 71's approved painting.
  .venv/bin/python ArtPipeline/Archive/72-run-end/mock_run_end.py
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent / "71-lobby-expedition"))
import mock_lobby as m  # noqa: E402
import mock_town as t  # noqa: E402

W, H = m.W, m.H
PAINT = HERE.parent / "71-lobby-expedition/approved/lobby_town_topdown.png"
t.PAINT_TD = PAINT


def window(img, title, title_color, lines, button):
    shade = Image.new("RGBA", (W, H), (0, 0, 0, 170)); shade.paste((0, 0, 0, 0), (0, 0, W, 100)); img.alpha_composite(shade)
    d = ImageDraw.Draw(img)
    box = (510, 290, 1410, 790)
    m.panel(d, box, m.PANEL, outline=m.BRASS, width=2)
    m.text(d, (960, 350), title, 48, title_color, "mm")
    for i, (k, v) in enumerate(lines):
        y = 430 + i * 48
        m.text(d, (560, y), k, 26, m.DIM, "lm"); m.text(d, (1360, y), v, 26, m.TEXT, "rm")
    m.button(d, (760, 690, 1160, 760), button, m.BUTTON, 30)


def victory():
    img = t.painted_any(PAINT, t.TD_TOP, t.TD_HOTSPOTS, t.TD_LABELS); m.header(img, "용병단 본부")
    window(img, "최종 보스를 쓰러뜨렸습니다 — 승리", m.VIRTUE,
           [("끝난 날", "87일차"), ("원정", "14회 (광산 11, 심연 3)"), ("전사자", "3명"), ("살아남은 용병", "로언 · 카이 · 엘라 · 브란 · 테오")], "새 런")
    return img.convert("RGB")


def defeat():
    img = t.painted_any(PAINT, t.TD_TOP, t.TD_HOTSPOTS, t.TD_LABELS); m.header(img, "용병단 본부")
    window(img, "전사자가 15명에 닿았습니다 — 패배", m.DANGER,
           [("끝난 날", "63일차"), ("원정", "10회 (광산 9, 심연 1)"), ("전사자", "15명"), ("살아남은 용병", "미라 · 요한나")], "새 런")
    return img.convert("RGB")


def final_stand_due():
    img = t.painted_any(PAINT, t.TD_TOP, t.TD_HOTSPOTS, t.TD_LABELS, hover="원정"); m.header(img, "용병단 본부"); d = ImageDraw.Draw(img)
    # the day counter past the last day, the gate's plate changed, the rest barred
    m.panel(d, (700, 20, 1220, 80), m.BG)
    m.text(d, (W / 2, 50), "101일차 / 100일 — 최종 결전", 40, m.VIRTUE, "mm")
    t.plate(d, 1755, 684, "최종 결전", 26, line=m.BRASS, sub="심연의 보스와 싸운다")
    t.plate(d, 1046, 628, "쉼터", 26, color=m.DIM, line=m.LINE, sub="쉴 날이 없다")
    t.plate(d, 1600, 560, "원정", 24, sub="기한이 지났습니다. 최종 결전으로 출발합니다", line=m.BRASS)
    t.strip(d)
    m.button(d, (1560, 1004, 1880, 1066), "최종 결전으로", m.BUTTON, 24)
    return img.convert("RGB")


def main():
    outs = {"run-end-victory": victory(), "run-end-defeat": defeat(), "final-stand-due": final_stand_due()}
    for k, im in outs.items():
        im.save(HERE / f"mock-{k}.png"); print(f"mock-{k}.png")
    m.sheet([("승리: 최종 보스를 쓰러뜨림 (결과 창, [새 런])", outs["run-end-victory"]),
             ("패배: 사망 한도 (결과 창, [새 런])", outs["run-end-defeat"]),
             ("100일을 넘긴 로비: 성문이 '최종 결전', 쉼터는 닫힘, 띠의 단추도 최종 결전", outs["final-stand-due"])],
            2, HERE / "mock-compare-run-end.png",
            footer="25단계의 규칙을 26단계의 마을 로비 위에 그린 것. 지금의 로비(도형)에는 같은 글이 기존 결과 판과 출발 안내 줄에 들어간다.")


if __name__ == "__main__":
    main()
