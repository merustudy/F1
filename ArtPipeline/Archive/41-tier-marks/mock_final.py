"""Round 41, fourth and fifth mockups (mock4: two candidates; mock5 adds the user's "outline + stars"): the two candidates in the chosen colours (P3: copper, silver, gold for the three tiers above the
base, which is unmarked; the tiers are renamed 동·은·금). Candidate "테두리 2안": the thin rim of round 35 plus the star tag
(1·2·3 stars) at the bottom-left; candidate "외곽선 A": the icon's outline 2·3·4 px. Over the game's screenshots and at
twice the size. No API call.
  .venv/bin/python ArtPipeline/Archive/41-tier-marks/mock_final.py
"""
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import mock_tier_marks as frame  # noqa: E402
import mock_outline as outline  # noqa: E402
from mock_tier_marks import KO, RIM, TEXT, sheet  # noqa: E402

# P3 (2026-10-07): the internal keys stay Silver/Gold/Diamond (the three tiers above the base); their colours and names change.
RIM.update({"Silver": (0xA8, 0x68, 0x3A), "Gold": (0x9A, 0xA7, 0xB8), "Diamond": (0xD4, 0xA2, 0x32)})
TEXT.update({"Bronze": (0xC9, 0xC2, 0xB0), "Silver": (0xD5, 0x9A, 0x66), "Gold": (0xD3, 0xDB, 0xE4), "Diamond": (0xF0, 0xC8, 0x5A)})
KO.update({"Bronze": "기본", "Silver": "동", "Gold": "은", "Diamond": "금"})

NAMES = {"2": "테두리 2안: 가는 테(단계 색) + 왼쪽 아래 별 표 ★·★★·★★★", "A": "외곽선 A안: 아이콘 외곽선 2 · 3 · 4",
         "AS": "외곽선 + 별: 아이콘 외곽선 2 · 3 · 4 + 왼쪽 아래 별 표 ★·★★·★★★"}


def main():
    entries = [(NAMES["2"] + " — 파티 쪽", frame.party_side("2")), ("— 전투", frame.battle("2")),
               (NAMES["A"] + " — 파티 쪽", outline.party_side("A")), ("— 전투", outline.battle("A")),
               (NAMES["AS"] + " — 파티 쪽", outline.party_side("AS")), ("— 전투", outline.battle("AS"))]
    sheet(entries, 2, HERE / "mock5-compare.png",
          footer=["P3의 색과 이름: 기본은 표시 없음, 오른 셋이 동 #A8683A · 은 #9AA7B8 · 금 #D4A232. 스테이징: 로언(1열) 롱소드 기본, 단검 동(+1), 버클러 은(+1), 약초 주머니 금 / 카이 롱소드 은 / 미라 지팡이 동.",
                  "전투는 로언 동, 카이 은, 미라 금. 등급 배지는 없다. 외곽선은 전투에서 충전되지 않은 어둠에 함께 덮인다(그대로 두기로 함). 가는 테와 별 표는 어둠 위에 있어 늘 보인다."])
    det = []
    for how, mod in (("2", frame), ("A", outline), ("AS", outline)):
        for tier in ("Silver", "Gold", "Diamond"):
            det.append((f"{NAMES[how].split(':')[0]} · {KO[tier]}", mod.detail_cell(how, tier)))
        det.append((f"{NAMES[how].split(':')[0]} · 합치기 → 동", mod.detail_cell(how, "Bronze", merge_into="Silver", fatigue=False)))
    sheet(det, 4, HERE / "mock5-detail.png", lab=36, gap=18, size=20,
          footer=["2배 확대. 넷째 열은 기본 단계의 단검 둘을 합칠 때의 표시: '합치기 → 동'(동의 표시를 미리 두른다). '+1'은 장비 피로의 표(Round 32)."])


if __name__ == "__main__":
    main()
