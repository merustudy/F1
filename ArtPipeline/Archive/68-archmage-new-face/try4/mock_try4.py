# -*- coding: utf-8 -*-
"""Round 68, 4th try (the face traits of a virtual person the user made, in our style): the 3rd and the
4th try side by side — the faces large, every job at the game's scale (both at Height 75), the women's heads at that scale, and
the mine's stage. Pure Pillow, no API call; the game's art is not changed.
  .venv/bin/python ArtPipeline/Archive/68-archmage-new-face/try4/mock_try4.py
"""
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent.parent / "67-archmage-subculture-face"))
sys.path.insert(0, str(HERE.parent))
from mock_faces import JOB, KNIGHT, GOBLIN, faces_large, game_row, heads, load, stack, stage_sheet  # noqa: E402
from mock_newface import on_canvas  # noqa: E402

FIRST_RAW, FIRST_FACE = HERE.parent / "try3/archmage_newface3.raw.png", (400, 40, 640, 280)
FIRST = HERE.parent / "try3/archmage_newface3_h75.png"
SECOND_RAW, SECOND_FACE = HERE / "archmage_newface4.raw.png", (380, 50, 620, 290)
SECOND = HERE / "archmage_newface4_h78.png"


def main():
    jobs = {key: load(JOB / f"{key}.png") for key in ("knight", "valkyrie", "paladin", "bishop", "spellblade", "archmage")}
    first, second = load(FIRST), load(SECOND)
    knight, goblin = load(KNIGHT), load(GOBLIN)
    party = lambda img: [(knight, 380), (jobs["valkyrie"], 640), (img, 900), (goblin, 1340)]
    stack([faces_large("얼굴: 3차와 4차(가상 인물의 얼굴 특징)",
                       [("3차", load(FIRST_RAW), FIRST_FACE), ("4차", load(SECOND_RAW), SECOND_FACE)]),
           game_row("게임 배율: 캔버스 전체를 같은 크기로, 같은 바닥에 (3차 Height 75, 4차 Height 78: 턱~바닥을 발키리와 같게)",
                    [("기사", jobs["knight"]), ("발키리", jobs["valkyrie"]), ("성기사", jobs["paladin"]), ("주교", jobs["bishop"]),
                     ("마검사", jobs["spellblade"]), ("대마법사 지금", jobs["archmage"]), ("3차", first), ("4차", second)]),
           heads("머리: 게임 배율 그대로 2배 (여성 용병)",
                 [("발키리", jobs["valkyrie"], (230, 190, 450, 410)), ("주교", jobs["bishop"], (220, 120, 440, 340)),
                  ("대마법사 지금", jobs["archmage"], (220, 60, 440, 280)),
                  ("3차", first, on_canvas(FIRST_RAW, FIRST_FACE, 75)), ("4차", second, on_canvas(SECOND_RAW, SECOND_FACE, 78))]),
           stage_sheet("광산 무대, 게임 크기의 절반: 기사 / 발키리 / 대마법사 / 고블린 약탈자",
                       [("지금", party(jobs["archmage"])), ("3차", party(first)), ("4차", party(second))])],
          HERE / "mock-try4.png")


if __name__ == "__main__":
    main()
