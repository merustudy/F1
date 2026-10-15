# -*- coding: utf-8 -*-
"""Round 68, 3rd try (the user: "현재 그림체로" — the 2nd made lighter for the stage, plain boots): the 2nd and the
3rd try side by side — the faces large, every job at the game's scale (both at Height 75), the women's heads at that scale, and
the mine's stage. Pure Pillow, no API call; the game's art is not changed.
  .venv/bin/python ArtPipeline/Archive/68-archmage-new-face/try3/mock_try3.py
"""
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent.parent / "67-archmage-subculture-face"))
sys.path.insert(0, str(HERE.parent))
from mock_faces import JOB, KNIGHT, GOBLIN, faces_large, game_row, heads, load, stack, stage_sheet  # noqa: E402
from mock_newface import on_canvas  # noqa: E402

FIRST_RAW, FIRST_FACE = HERE.parent / "try2/archmage_newface2.raw.png", (400, 50, 640, 290)
FIRST = HERE.parent / "try2/archmage_newface2_h75.png"
SECOND_RAW, SECOND_FACE = HERE / "archmage_newface3.raw.png", (400, 40, 640, 280)
SECOND = HERE / "archmage_newface3_h75.png"


def main():
    jobs = {key: load(JOB / f"{key}.png") for key in ("knight", "valkyrie", "paladin", "bishop", "spellblade", "archmage")}
    first, second = load(FIRST), load(SECOND)
    knight, goblin = load(KNIGHT), load(GOBLIN)
    party = lambda img: [(knight, 380), (jobs["valkyrie"], 640), (img, 900), (goblin, 1340)]
    stack([faces_large("얼굴: 2차와 3차(밝은 옷, 매끈한 장화)",
                       [("2차", load(FIRST_RAW), FIRST_FACE), ("3차", load(SECOND_RAW), SECOND_FACE)]),
           game_row("게임 배율: 캔버스 전체를 같은 크기로, 같은 바닥에 (2차·3차 모두 Height 75)",
                    [("기사", jobs["knight"]), ("발키리", jobs["valkyrie"]), ("성기사", jobs["paladin"]), ("주교", jobs["bishop"]),
                     ("마검사", jobs["spellblade"]), ("대마법사 지금", jobs["archmage"]), ("2차", first), ("3차", second)]),
           heads("머리: 게임 배율 그대로 2배 (여성 용병)",
                 [("발키리", jobs["valkyrie"], (230, 190, 450, 410)), ("주교", jobs["bishop"], (220, 120, 440, 340)),
                  ("대마법사 지금", jobs["archmage"], (220, 60, 440, 280)),
                  ("2차", first, on_canvas(FIRST_RAW, FIRST_FACE, 75)), ("3차", second, on_canvas(SECOND_RAW, SECOND_FACE, 75))]),
           stage_sheet("광산 무대, 게임 크기의 절반: 기사 / 발키리 / 대마법사 / 고블린 약탈자",
                       [("지금", party(jobs["archmage"])), ("2차", party(first)), ("3차", party(second))])],
          HERE / "mock-try3.png")


if __name__ == "__main__":
    main()
