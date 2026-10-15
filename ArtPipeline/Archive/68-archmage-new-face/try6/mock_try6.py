# -*- coding: utf-8 -*-
"""Round 68, 6th try (the face size and the proportions set to the valkyrie's, a mage robe): the 5th and the
6th try side by side — the faces large, every job at the game's scale (both at Height 75), the women's heads at that scale, and
the mine's stage. Pure Pillow, no API call; the game's art is not changed.
  .venv/bin/python ArtPipeline/Archive/68-archmage-new-face/try6/mock_try6.py
"""
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent.parent / "67-archmage-subculture-face"))
sys.path.insert(0, str(HERE.parent))
from mock_faces import JOB, KNIGHT, GOBLIN, faces_large, game_row, heads, load, stack, stage_sheet  # noqa: E402
from mock_newface import on_canvas  # noqa: E402

FIRST_RAW, FIRST_FACE = HERE.parent / "try5/archmage_newface5.raw.png", (410, 30, 650, 270)
FIRST = HERE.parent / "try5/archmage_newface5_h76.png"
SECOND_RAW, SECOND_FACE = HERE / "archmage_newface6.raw.png", (420, 20, 640, 240)
SECOND = HERE / "archmage_newface6_h71.png"
SECOND75 = HERE / "archmage_newface6_h75.png"


def main():
    jobs = {key: load(JOB / f"{key}.png") for key in ("knight", "valkyrie", "paladin", "bishop", "spellblade", "archmage")}
    first, second, second75 = load(FIRST), load(SECOND), load(SECOND75)
    knight, goblin = load(KNIGHT), load(GOBLIN)
    party = lambda img: [(knight, 380), (jobs["valkyrie"], 640), (img, 900), (goblin, 1340)]
    stack([faces_large("얼굴: 5차와 6차(얼굴 크기·비율 재조정, 마법사 로브)",
                       [("5차", load(FIRST_RAW), FIRST_FACE), ("6차", load(SECOND_RAW), SECOND_FACE)]),
           game_row("게임 배율: 캔버스 전체를 같은 크기로, 같은 바닥에 (5차 Height 76, 6차 Height 71: 얼굴 길이를 발키리와 같게, 6차 Height 75)",
                    [("기사", jobs["knight"]), ("발키리", jobs["valkyrie"]), ("성기사", jobs["paladin"]), ("주교", jobs["bishop"]),
                     ("마검사", jobs["spellblade"]), ("대마법사 지금", jobs["archmage"]), ("5차", first), ("6차 (71)", second), ("6차 (75)", second75)]),
           heads("머리: 게임 배율 그대로 2배 (여성 용병)",
                 [("발키리", jobs["valkyrie"], (230, 190, 450, 410)), ("주교", jobs["bishop"], (220, 120, 440, 340)),
                  ("대마법사 지금", jobs["archmage"], (220, 60, 440, 280)),
                  ("5차", first, on_canvas(FIRST_RAW, FIRST_FACE, 76)), ("6차 (71)", second, on_canvas(SECOND_RAW, SECOND_FACE, 71)), ("6차 (75)", second75, on_canvas(SECOND_RAW, SECOND_FACE, 75))]),
           stage_sheet("광산 무대, 게임 크기의 절반: 기사 / 발키리 / 대마법사 / 고블린 약탈자",
                       [("지금", party(jobs["archmage"])), ("5차", party(first)), ("6차 (71)", party(second)), ("6차 (75)", party(second75))])],
          HERE / "mock-try6.png")


if __name__ == "__main__":
    main()
