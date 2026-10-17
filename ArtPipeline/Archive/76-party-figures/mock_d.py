# -*- coding: utf-8 -*-
"""Round 76, 안 D (the user's 2026-10-17 idea "노드맵 전투선택 화면 용병 구성 창(서있는 것)을 왼쪽에 배치하고 오른쪽에 용병 목록 제공"):
the party standing at the left as the node map's party side stands it (figures on a stage, 4열 → 1열), the roster as a list at the right.
Drawn over the expedition screen as it is (base-expedition.png), the shipped job figures. No API call.
  .venv/bin/python ArtPipeline/Archive/76-party-figures/mock_d.py
"""
from pathlib import Path
from PIL import Image, ImageDraw
import mock_figures as m

HERE = Path(__file__).resolve().parent
PASSIVE = {"아스트리드": "빈사 상태: 무기 피해 +100%", "세드릭": "맨 앞: 전투 시작 때 아군 전체에 보호막 2", "엘라": "회복시킨 대상에게 보호막 3",
           "카이": "무기 적중 시 화상 3", "미라": "뒤에서 2번째 자리까지: 무기 피해 +25%", "로언": "맨 앞: 전투 시작 때 자신에게 보호막 5"}


def stage_column(img, x, w, foot_y, row, member):
    """A column of the stage as the node map's party side draws it: the figure (300 high) on the floor, the foot marks under it."""
    d = ImageDraw.Draw(img)
    cx = x + w / 2
    if member is None:
        d.rectangle((cx - 90, foot_y - 300, cx + 90, foot_y), fill=(0x15, 0x18, 0x1F, 120))
        m.label(d, (cx, foot_y - 150), "비어 있음", 24, m.DIM, "mm")
        m.label(d, (cx, foot_y + 24), "%d열" % row, 20, m.DIM, "mm")
        m.label(d, (cx, foot_y + 54), "오른쪽 목록에서 넣으세요", 18, m.DIM, "mm")
        return
    name, job, job_name, lv, _ = member
    m.stand(img, cx, foot_y, job, 300, 150)
    d = ImageDraw.Draw(img)
    m.label(d, (cx - 110, foot_y + 16), "%d열" % row, 20, m.DIM)
    m.label(d, (cx - 66, foot_y + 13), name, 26, m.TEXT)
    m.label(d, (cx + 110, foot_y + 18), "Lv %d" % lv, 18, m.BONE, "ra")
    m.label(d, (cx - 110, foot_y + 50), job_name, 20, m.DIM)
    m.pips(d, cx - 110, foot_y + 82, 220, 12)
    m.button(d, (cx - 110, foot_y + 104, cx - 4, foot_y + 144), "앞으로", m.QUIET if row > 1 else m.DISABLED, 20, m.TEXT if row > 1 else m.DIM)
    m.button(d, (cx + 4, foot_y + 104, cx + 110, foot_y + 144), "뒤로", m.QUIET if row < 4 else m.DISABLED, 20, m.TEXT if row < 4 else m.DIM)


def roster_row(img, x, y, w, member, full):
    """A line of the roster list at the right, as the barracks lists them: name, job, level, passive, the pips, and the party button."""
    d = ImageDraw.Draw(img)
    name, job, job_name, lv, row = member
    h = 86
    d.rectangle((x, y, x + w, y + h), fill=m.SELECTED if row else m.PANEL_LIGHT)
    d.rectangle((x + 10, y + 7, x + 66, y + 79), fill=m.SLOT)
    m.stand(img, x + 38, y + 76, job, 66, 36)
    d = ImageDraw.Draw(img)
    m.label(d, (x + 80, y + 6), name, 24, m.TEXT)
    m.label(d, (x + 210, y + 11), job_name, 18, m.DIM)
    m.label(d, (x + 310, y + 11), "Lv %d" % lv, 17, m.BONE)
    m.label(d, (x + 80, y + 38), PASSIVE[name], 15, m.DIM)
    m.pips(d, x + 80, y + 66, 320, 9)
    if row:
        m.label(d, (x + w - 140, y + 43), "%d열" % row, 20, m.BRASS, "rm")
        m.button(d, (x + w - 124, y + 23, x + w - 12, y + 63), "빼기", m.QUIET, 20)
    else:
        m.button(d, (x + w - 124, y + 23, x + w - 12, y + 63), "넣기", m.QUIET if not full else m.DISABLED, 20, m.TEXT if not full else m.DIM)


def option_d(img):
    d = ImageDraw.Draw(img)
    x0, y0, x1, y1 = 40, 110, 1880, 1010
    d.rectangle((x0, y0, x1, y1), fill=m.PANEL, outline=m.BRASS, width=2)
    m.label(d, (x0 + 24, y0 + 22), "원정 파티", 30, m.DIM)
    m.label(d, (x0 + 170, y0 + 28), "버려진 광산 · 5일 (→ 6일차)", 22, m.TEXT)
    m.button(d, (x1 - 180, y0 + 16, x1 - 24, y0 + 66), "닫기", m.QUIET, 22)

    # The stage at the left (the node map's party side: four columns, 1열 at the right), a floor line under the feet.
    sx0, sx1 = x0 + 24, x0 + 1104
    d.rectangle((sx0, y0 + 84, sx1, y1 - 24), fill=(0x16, 0x19, 0x20))
    foot = y0 + 440
    d.line((sx0 + 20, foot + 2, sx1 - 20, foot + 2), fill=m.LINE, width=2)
    cw = (sx1 - sx0) / 4
    for i, row in enumerate((4, 3, 2, 1)):
        stage_column(img, sx0 + cw * i, cw, foot, row, m.BY_ROW.get(row))
    d = ImageDraw.Draw(img)
    m.label(d, (sx0 + 24, y1 - 72), "출발할 수 있습니다", 24, m.GOOD, "lm")
    m.button(d, (sx1 - 24 - 294, y1 - 24 - 64 - 16, sx1 - 24, y1 - 24 - 16), "출발", m.BUTTON, 30)

    # The roster list at the right.
    rx0, rx1 = x0 + 1128, x1 - 24
    m.label(d, (rx0, y0 + 90), "로스터 6/8 — 넣기로 비어 있는 맨 앞 열에, 빼면 뒤가 전진", 20, m.DIM)
    full = len(m.BY_ROW) >= 4
    for i, mem in enumerate(m.ROSTER):
        roster_row(img, rx0, y0 + 120 + 94 * i, rx1 - rx0, mem, full)
    d = ImageDraw.Draw(img)
    for i in range(len(m.ROSTER), 8):
        y = y0 + 120 + 94 * i
        d.rectangle((rx0, y, rx1, y + 86), outline=m.LINE, width=1)
        m.label(d, ((rx0 + rx1) / 2, y + 43), "빈 자리 (역마차에서 고용)", 18, m.DIM, "mm")
    return img


def main():
    base = Image.open(HERE / "base-expedition.png").convert("RGBA")
    dimg = option_d(m.shade(base.copy(), (0, 100, 1920, 1080)))
    dimg.save(HERE / "mock-figures-D-stage-list.png")
    a = Image.open(HERE / "mock-figures-A-popup.png").convert("RGBA")
    tiles = [m.caption(a, "안 A — 팝업의 열 칸이 그림 자리, 아래에 로스터 칩"),
             m.caption(dimg.copy(), "안 D (사용자 안) — 왼쪽 무대에 서 있는 파티(노드 맵처럼), 오른쪽에 용병 목록")]
    sheet = Image.new("RGBA", (1920 + 20, 540 + 20), (0x0C, 0x0C, 0x0E, 255))
    for i, t in enumerate(tiles):
        sheet.paste(t.resize((960, 540), Image.LANCZOS), (10 + 970 * i, 10))
    sheet.save(HERE / "mock-figures-compare-AD.png")
    print("ok")


if __name__ == "__main__":
    main()
