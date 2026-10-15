# -*- coding: utf-8 -*-
"""Round 68, 5th try (the virtual woman's image attached as a second reference for the face): the 4th and the
5th try side by side — the faces large, every job at the game's scale (both at Height 75), the women's heads at that scale, and
the mine's stage. Pure Pillow, no API call; the game's art is not changed.
  .venv/bin/python ArtPipeline/Archive/68-archmage-new-face/try5/mock_try5.py
"""
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent.parent / "67-archmage-subculture-face"))
sys.path.insert(0, str(HERE.parent))
from mock_faces import JOB, KNIGHT, GOBLIN, faces_large, game_row, heads, load, stack, stage_sheet  # noqa: E402
from mock_newface import on_canvas  # noqa: E402

FIRST_RAW, FIRST_FACE = HERE.parent / "try4/archmage_newface4.raw.png", (380, 50, 620, 290)
FIRST = HERE.parent / "try4/archmage_newface4_h78.png"
SECOND_RAW, SECOND_FACE = HERE / "archmage_newface5.raw.png", (410, 30, 650, 270)
SECOND = HERE / "archmage_newface5_h76.png"


def main():
    jobs = {key: load(JOB / f"{key}.png") for key in ("knight", "valkyrie", "paladin", "bishop", "spellblade", "archmage")}
    first, second = load(FIRST), load(SECOND)
    knight, goblin = load(KNIGHT), load(GOBLIN)
    party = lambda img: [(knight, 380), (jobs["valkyrie"], 640), (img, 900), (goblin, 1340)]
    stack([faces_large("얼굴: 4차(글로만)와 5차(가상 인물 이미지를 붙임)",
                       [("4차", load(FIRST_RAW), FIRST_FACE), ("5차", load(SECOND_RAW), SECOND_FACE)]),
           game_row("게임 배율: 캔버스 전체를 같은 크기로, 같은 바닥에 (4차 Height 78, 5차 Height 76: 턱~바닥을 발키리와 같게)",
                    [("기사", jobs["knight"]), ("발키리", jobs["valkyrie"]), ("성기사", jobs["paladin"]), ("주교", jobs["bishop"]),
                     ("마검사", jobs["spellblade"]), ("대마법사 지금", jobs["archmage"]), ("4차", first), ("5차", second)]),
           heads("머리: 게임 배율 그대로 2배 (여성 용병)",
                 [("발키리", jobs["valkyrie"], (230, 190, 450, 410)), ("주교", jobs["bishop"], (220, 120, 440, 340)),
                  ("대마법사 지금", jobs["archmage"], (220, 60, 440, 280)),
                  ("4차", first, on_canvas(FIRST_RAW, FIRST_FACE, 78)), ("5차", second, on_canvas(SECOND_RAW, SECOND_FACE, 76))]),
           stage_sheet("광산 무대, 게임 크기의 절반: 기사 / 발키리 / 대마법사 / 고블린 약탈자",
                       [("지금", party(jobs["archmage"])), ("4차", party(first)), ("5차", party(second))])],
          HERE / "mock-try5.png")


if __name__ == "__main__":
    main()
