# -*- coding: utf-8 -*-
"""Round 67, the redraw with the saved face `subculture_60` (no hat, the valkyrie's proportions, colours that read on the
stage): the face next to the 60% test, every job at the game's scale with the redraw at Height 74 (its head as big as the
valkyrie's), the heads of the women at that scale, and the mine's stage. Pure Pillow, no API call; the game's art is not changed.
  .venv/bin/python ArtPipeline/Archive/67-archmage-subculture-face/sub60/mock_sub60.py
"""
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent))
from mock_faces import JOB, KNIGHT, GOBLIN, bbox, faces_large, game_row, heads, load, stack, stage_sheet  # noqa: E402

RAW = HERE / "archmage_sub60.raw.png"
FITTED = HERE / "archmage_sub60_h74.png"
FIT_HEIGHT = 74
FACE_RAW = (420, 40, 640, 260)  # the head on the raw image (read off a grid)
TEST60 = HERE.parent / "face60/archmage_face60.raw.png"
TEST60_FACE = (410, 120, 630, 340)
CANVAS = (672, 896)


def face_on_canvas():
    """The raw head box carried through fit_figure's arithmetic (Height 74, centred, on the floor line)."""
    b = bbox(load(RAW))
    w, h = b[2] - b[0], b[3] - b[1]
    r = min(round(CANVAS[1] * FIT_HEIGHT / 100) / h, (CANVAS[0] - 24) / w)
    x0 = (CANVAS[0] - round(w * r)) // 2
    y0 = CANVAS[1] - 20 - round(h * r)
    fx0, fy0, fx1, fy1 = FACE_RAW
    return (round(x0 + (fx0 - b[0]) * r), round(y0 + (fy0 - b[1]) * r),
            round(x0 + (fx1 - b[0]) * r), round(y0 + (fy1 - b[1]) * r))


def main():
    jobs = {key: load(JOB / f"{key}.png") for key in ("knight", "valkyrie", "paladin", "bishop", "spellblade", "archmage")}
    redraw = load(FITTED)
    knight, goblin = load(KNIGHT), load(GOBLIN)
    party = lambda img: [(knight, 380), (jobs["valkyrie"], 640), (img, 900), (goblin, 1340)]
    stack([faces_large("얼굴: 60% 시험과 다시 그림(서브컬쳐_60)",
                       [("60% 시험", load(TEST60), TEST60_FACE), ("다시 그림 (Face = subculture_60)", load(RAW), FACE_RAW)]),
           game_row(f"게임 배율: 캔버스 전체를 같은 크기로, 같은 바닥에 (다시 그림은 Height {FIT_HEIGHT}: 머리를 발키리와 같게)",
                    [("기사", jobs["knight"]), ("발키리", jobs["valkyrie"]), ("성기사", jobs["paladin"]), ("주교", jobs["bishop"]),
                     ("마검사", jobs["spellblade"]), ("대마법사 지금", jobs["archmage"]), ("다시 그림", redraw)]),
           heads("머리: 게임 배율 그대로 2배 (여성 용병)",
                 [("발키리", jobs["valkyrie"], (230, 190, 450, 410)), ("주교", jobs["bishop"], (220, 120, 440, 340)),
                  ("대마법사 지금", jobs["archmage"], (220, 60, 440, 280)), ("다시 그림", redraw, face_on_canvas())]),
           stage_sheet("광산 무대, 게임 크기의 절반: 기사 / 발키리 / 대마법사 / 고블린 약탈자",
                       [("지금", party(jobs["archmage"])), ("다시 그림", party(redraw))])],
          HERE / "mock-sub60.png")


if __name__ == "__main__":
    main()
