# -*- coding: utf-8 -*-
"""Round 68, 2nd try (the user: "조금 더 가린 옷, 뒤 망토 없는 옷 스타일, 재생성" + "진행" of the face touch-ups): the 1st and the
2nd try side by side — the faces large, every job at the game's scale (both at Height 75), the women's heads at that scale, and
the mine's stage. Pure Pillow, no API call; the game's art is not changed.
  .venv/bin/python ArtPipeline/Archive/68-archmage-new-face/try2/mock_try2.py
"""
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent.parent / "67-archmage-subculture-face"))
sys.path.insert(0, str(HERE.parent))
from mock_faces import JOB, KNIGHT, GOBLIN, faces_large, game_row, heads, load, stack, stage_sheet  # noqa: E402
from mock_newface import on_canvas  # noqa: E402

FIRST_RAW, FIRST_FACE = HERE.parent / "archmage_newface.raw.png", (380, 40, 620, 280)
FIRST = HERE.parent / "archmage_newface_h75.png"
SECOND_RAW, SECOND_FACE = HERE / "archmage_newface2.raw.png", (400, 50, 640, 290)
SECOND = HERE / "archmage_newface2_h75.png"


def main():
    jobs = {key: load(JOB / f"{key}.png") for key in ("knight", "valkyrie", "paladin", "bishop", "spellblade", "archmage")}
    first, second = load(FIRST), load(SECOND)
    knight, goblin = load(KNIGHT), load(GOBLIN)
    party = lambda img: [(knight, 380), (jobs["valkyrie"], 640), (img, 900), (goblin, 1340)]
    stack([faces_large("얼굴: 1차와 2차(눈썹·입술·머리 다듬기)",
                       [("1차", load(FIRST_RAW), FIRST_FACE), ("2차", load(SECOND_RAW), SECOND_FACE)]),
           game_row("게임 배율: 캔버스 전체를 같은 크기로, 같은 바닥에 (1차·2차 모두 Height 75)",
                    [("기사", jobs["knight"]), ("발키리", jobs["valkyrie"]), ("성기사", jobs["paladin"]), ("주교", jobs["bishop"]),
                     ("마검사", jobs["spellblade"]), ("대마법사 지금", jobs["archmage"]), ("1차", first), ("2차", second)]),
           heads("머리: 게임 배율 그대로 2배 (여성 용병)",
                 [("발키리", jobs["valkyrie"], (230, 190, 450, 410)), ("주교", jobs["bishop"], (220, 120, 440, 340)),
                  ("대마법사 지금", jobs["archmage"], (220, 60, 440, 280)),
                  ("1차", first, on_canvas(FIRST_RAW, FIRST_FACE, 75)), ("2차", second, on_canvas(SECOND_RAW, SECOND_FACE, 75))]),
           stage_sheet("광산 무대, 게임 크기의 절반: 기사 / 발키리 / 대마법사 / 고블린 약탈자",
                       [("지금", party(jobs["archmage"])), ("1차", party(first)), ("2차", party(second))])],
          HERE / "mock-try2.png")


if __name__ == "__main__":
    main()
