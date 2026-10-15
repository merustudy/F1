# -*- coding: utf-8 -*-
"""Round 68: the archmage with a new face in our own style (common beauty traits, no subculture face) next to the
game's archmage and Round 67's redraw with the saved face subculture_60 — the faces large, every job at the game's scale
(the new face at Height 75: chin-to-floor as the valkyrie's), the women's heads at that scale, and the mine's stage.
Pure Pillow, no API call; the game's art is not changed.
  .venv/bin/python ArtPipeline/Archive/68-archmage-new-face/mock_newface.py
"""
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent / "67-archmage-subculture-face"))
from mock_faces import JOB, KNIGHT, GOBLIN, bbox, faces_large, game_row, heads, load, stack, stage_sheet  # noqa: E402

RAW = HERE / "archmage_newface.raw.png"
FITTED = HERE / "archmage_newface_h75.png"
FIT_HEIGHT = 75
FACE_RAW = (380, 40, 620, 280)
SUB60_DIR = HERE.parent / "67-archmage-subculture-face/sub60"
SUB60_RAW, SUB60_FACE = SUB60_DIR / "archmage_sub60.raw.png", (420, 40, 640, 260)
SUB60_FITTED = SUB60_DIR / "archmage_sub60_h74.png"
CANVAS = (672, 896)


def on_canvas(raw, face, fit_height):
    """A raw box carried through fit_figure's arithmetic (Height, centred, on the floor line)."""
    b = bbox(load(raw))
    w, h = b[2] - b[0], b[3] - b[1]
    r = min(round(CANVAS[1] * fit_height / 100) / h, (CANVAS[0] - 24) / w)
    x0 = (CANVAS[0] - round(w * r)) // 2
    y0 = CANVAS[1] - 20 - round(h * r)
    return (round(x0 + (face[0] - b[0]) * r), round(y0 + (face[1] - b[1]) * r),
            round(x0 + (face[2] - b[0]) * r), round(y0 + (face[3] - b[1]) * r))


def main():
    jobs = {key: load(JOB / f"{key}.png") for key in ("knight", "valkyrie", "paladin", "bishop", "spellblade", "archmage")}
    new, sub60 = load(FITTED), load(SUB60_FITTED)
    knight, goblin = load(KNIGHT), load(GOBLIN)
    party = lambda img: [(knight, 380), (jobs["valkyrie"], 640), (img, 900), (goblin, 1340)]
    stack([faces_large("얼굴: 서브컬쳐_60과 새 얼굴(우리 그림체)",
                       [("Round 67 서브컬쳐_60", load(SUB60_RAW), SUB60_FACE), ("Round 68 새 얼굴", load(RAW), FACE_RAW)]),
           game_row(f"게임 배율: 캔버스 전체를 같은 크기로, 같은 바닥에 (새 얼굴은 Height {FIT_HEIGHT}: 턱~바닥을 발키리와 같게)",
                    [("기사", jobs["knight"]), ("발키리", jobs["valkyrie"]), ("성기사", jobs["paladin"]), ("주교", jobs["bishop"]),
                     ("마검사", jobs["spellblade"]), ("대마법사 지금", jobs["archmage"]), ("서브컬쳐_60", sub60), ("새 얼굴", new)]),
           heads("머리: 게임 배율 그대로 2배 (여성 용병)",
                 [("발키리", jobs["valkyrie"], (230, 190, 450, 410)), ("주교", jobs["bishop"], (220, 120, 440, 340)),
                  ("대마법사 지금", jobs["archmage"], (220, 60, 440, 280)),
                  ("서브컬쳐_60", sub60, on_canvas(SUB60_RAW, SUB60_FACE, 74)), ("새 얼굴", new, on_canvas(RAW, FACE_RAW, FIT_HEIGHT))]),
           stage_sheet("광산 무대, 게임 크기의 절반: 기사 / 발키리 / 대마법사 / 고블린 약탈자",
                       [("지금", party(jobs["archmage"])), ("서브컬쳐_60", party(sub60)), ("새 얼굴", party(new))])],
          HERE / "mock-newface.png")


if __name__ == "__main__":
    main()
