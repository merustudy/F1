"""Round 69, the chosen spec drawn once more (no API call): the user's judgment — the tiers 일반·1성·2성·3성, option B's pips at the
stars' size, coloured 1성 blue (round 49's Bronze), 2성 silver, 3성 yellow (round 49's Silver); the words in the same colours.
  mock-chosen.png  Rowan's and Kai's boards at 1x in place and at 2x, the battle board, and the words.
  .venv/bin/python ArtPipeline/Archive/69-tier-naming/mock_chosen.py
"""
from PIL import Image, ImageDraw

import mock_tier_names as m

CHOSEN = {2: (0x70, 0x70, 0xFF), 3: (0xD3, 0xDB, 0xE4), 4: (0xFF, 0xFF, 0x6E)}
WORDS = {1: "일반", 2: "1성", 3: "2성", 4: "3성"}


def apply():
    """The pips and the words take the chosen colours; option B's diamonds get the star's ink edge weight."""
    m.METAL.update(CHOSEN)
    m.METAL_TEXT.update(CHOSEN)
    m.METAL_LIGHT.update({2: (0xA0, 0xA0, 0xFF), 3: (0xF2, 0xF6, 0xF9), 4: (0xFF, 0xFF, 0xB0)})
    m.ROMAN.update(WORDS)

    def mark(self, kind, tier, b):
        n = tier - 1
        x0, y1 = b[0] + 4, b[3] - 4
        for i in range(n):                                      # a diamond the star's size (12), its ink edge the star's (round 41)
            self.diamond(x0 + 6 + i * 13, y1 - 6, 6, CHOSEN[tier] + (255,), m.INK + (255,), None)

    m.Board.mark = mark


def main():
    apply()
    shot = Image.open(m.SHOT).convert("RGBA")
    strip = shot.crop((0, 620, 960, 810))
    m.stage_kai("B").lay(strip, (513, 646 - 620))
    m.stage("B").lay(strip, (703, 646 - 620))
    pad = 12
    W = 960 + 2 * pad
    sheet = Image.new("RGBA", (W, 26 + strip.height + 26 + 154 * 2 + 26 + 154 + 300), (0x1A, 0x18, 0x17, 255))
    d = ImageDraw.Draw(sheet)
    d.text((pad, 4), "확정: 일반·1성·2성·3성, 마름모 눈금(별 크기), 1성 파랑 · 2성 은 · 3성 노랑", font=m.font(15), fill=m.TEXT)
    sheet.alpha_composite(strip, (pad, 26))
    y = 26 + strip.height + 4
    d.text((pad, y), "2배", font=m.font(13), fill=m.DIM)
    m.stage("B", scale=2).lay(sheet, (pad, y + 20), 2)
    d.text((pad + 154 * 2 + 20, y), "전투 보드 (쿨다운 중)", font=m.font(13), fill=m.DIM)
    m.stage("B", battle=True).lay(sheet, (pad + 154 * 2 + 20, y + 20))
    y += 20 + 154 * 2 + 16
    # The words: tooltip title, the mend line, the merge overlay and hint, the ladder.
    x = pad
    d.text((x, y), "말", font=m.font(13), fill=m.DIM); y += 18
    d.rectangle((x, y, x + 330, y + 92), fill=(0, 0, 0), outline=(0x40, 0x40, 0x40))
    cx = x + 165
    d.text((cx, y + 14), "단검 2성", font=m.font(15), fill=CHOSEN[3], anchor="mm")
    d.text((cx, y + 34), "무기 장비 / 크기 2×1 / 근접 / 쿨다운 1.5초", font=m.font(12), fill=m.TEXT, anchor="mm")
    d.text((cx, y + 52), "맨 앞 적에게 피해 3-6", font=m.font(12), fill=m.EFFECT, anchor="mm")
    d.text((cx, y + 70), "전투마다 피로 +1", font=m.font(12), fill=m.FATIGUE, anchor="mm")
    x2 = x + 350
    m.colored(d, (x2, y + 8), [("롱소드  ", m.TEXT), ("일반", (0xC9, 0xC2, 0xB0)), (" → ", m.TEXT), ("1성", CHOSEN[2])], 15)
    d.text((x2, y + 30), "맨 앞 적에게 피해 2-4 → 4-8", font=m.font(12), fill=m.DIM)
    d.rectangle((x2, y + 56, x2 + 120, y + 78), fill=m.INK)
    m.colored(d, (x2 + 8, y + 59), [("합치기 → ", m.TEXT), ("2성", CHOSEN[3])], 13)
    x3 = x2 + 300
    m.colored(d, (x3, y + 8), [("같은 ", m.TEXT), ("단검 1성", m.TEXT), (" 위에 놓으면 합쳐서", m.TEXT)], 12)
    m.colored(d, (x3, y + 26), [("2성", CHOSEN[3]), (" 하나가 됩니다.", m.TEXT)], 12)
    xx = x3
    for t in (1, 2, 3, 4):
        s = WORDS[t]; c = (0xC9, 0xC2, 0xB0) if t == 1 else CHOSEN[t]
        d.text((xx, y + 60), s, font=m.font(15), fill=c); xx += d.textlength(s, font=m.font(15)) + 18
    sheet = sheet.crop((0, 0, W, y + 110))
    sheet.convert("RGB").save(m.HERE / "mock-chosen.png")
    print("wrote", m.HERE / "mock-chosen.png")


if __name__ == "__main__":
    main()
