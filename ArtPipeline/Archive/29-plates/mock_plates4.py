"""Proposal 2 with names B and the stage 48 lower, the user's choice (round 29), on every screen that has the units' plates.
  ArtPipeline/Archive/29-plates/run_lowered_shots.sh <scratch> 48 7       (the battle moments, plates hidden, stage 48 lower)
  ArtPipeline/Archive/29-plates/run_party_shots.sh <scratch> 7            (every screen, plates hidden, the proposal's stages)
  PLATES=1 ArtPipeline/Archive/29-plates/run_party_shots.sh <scratch> 7   (every screen as the game shows it now)
  copied into game/ (see SHOTS), then
  .venv/bin/python ArtPipeline/Archive/29-plates/mock_plates4.py     # mock4-<scene>.png (now above, the proposal below)

The user's answer to the third mockups: "1. B안 2. 48 내림 위 안을 적용해서 목업 이미지 제공. 내 승인 후 구현".
- Under the feet (proposal 2): the bar with its numbers, the badge at its left end (a shield and its number, the skull at death's
  door) and the rim of its colour, the state line under the bar. On the party side of the node map and the reward screen the
  state line names the job, as it does now.
- Names B: each board of the panel has a header with the row (the gold stud) and the name; the boards move 12 down.
- The battle's stage (figures, background, floating words) is 48 lower: the floor 506 -> 554. The party side stands on the panel
  (its move buttons 12 above it): the marks are 46 high where the plate was 92, so its figures go 46 lower: the floor 464 -> 510.
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import mock_plates2 as P  # noqa: E402
import mock_plates3 as M  # noqa: E402

Z = P.Z
GAME = HERE / "game"
BATTLE_FLOOR, PARTY_FLOOR = 506 + 48, 464 + 46
PARTY_X = {4: 210, 3: 400, 2: 590, 1: 780}
TEXT_DIM = (0x9A, 0xA0, 0xAC)
# The state line keeps the game's sizes (icons 18, words 15; the second mockups drew the words at 13).
_tokens = P.tokens
P.tokens = lambda layer, x, y, chips, size, text_size: _tokens(layer, x, y, chips, size, 15 * Z)

def party_units(members):
    """The party side: (row, name, hp, max, job) -> the units under the party's columns."""
    return [(PARTY_X[row], {"row": row, "name": name, "hp": hp, "max": hp_max, "job": job}, "party") for row, name, hp, hp_max, job in members]


# name: (title, the proposal's shot, the shot of now, floor, units [(cx, unit, state)], boards [(x0, row, name)])
# The values are read from the shots of now (the same seed): the trail of the HP just lost (ghost) measured on their bars, and
# the places measured on them. The columns' centres are 210, 400, 590, 780 (the party) and 1140, 1330, 1520, 1710 (the enemy),
# the boards' and the still plates' alike; a plate that walks (ko_15's goblins, ko_05's rats, ko_09's Mira) is where it is in
# the shot of now (the same moment in the proposal's shot: the figures measured within a pixel or two), and in ko_15 the
# party's plates stand 4 left of their columns. The boards stand in the columns the units go to.
PARTY_04 = [(4, "미라", 80, 80, "대마법사"), (3, "엘라", 90, 90, "주교"), (2, "카이", 100, 100, "마검사"), (1, "로언", 140, 140, "기사")]
PARTY_07 = PARTY_04[:3] + [(1, "로언", 134, 140, "기사")]
PARTY_BOARDS = [(120, 4, "미라"), (310, 3, "엘라"), (500, 2, "카이"), (690, 1, "로언")]
UNITS_05 = ([(cx, {"row": row, "name": name, "hp": hp, "max": hp_max, "ghost": ghost}, "target")
             for cx, row, name, hp, hp_max, ghost in ((210, 4, "미라", 80, 80, 0), (400, 3, "엘라", 90, 90, 0),
                                                       (590, 2, "카이", 100, 100, 0), (780, 1, "로언", 120, 140, 0.14))]
            + [(cx, {"row": row, "name": "동굴 쥐", "hp": 14, "max": 40, "ghost": 0.65}, "enemy") for cx, row in ((1501, 1), (1692, 2))])
SHIELD3 = [("shield", "3", P.SHIELD)]
UNITS_15 = [(206, {"row": 4, "name": "카이", "hp": 100, "max": 100}, "party"), (396, {"row": 3, "name": "엘라", "hp": 90, "max": 90}, "party"),
            (586, {"row": 2, "name": "세드릭", "hp": 130, "max": 130, "states": SHIELD3}, "party"),
            (776, {"row": 1, "name": "아스트리드", "hp": 99985, "max": 100000, "states": [("burn", "3", P.BURN)]}, "party"),
            (1205, {"row": 1, "name": "고블린 약탈자", "hp": 80, "max": 80}, "enemy"), (1395, {"row": 2, "name": "고블린 주술사", "hp": 60, "max": 60}, "enemy")]
UNITS_18 = ([(cx, {"row": row, "name": name, "hp": 1, "max": hp_max}, "party") for cx, row, name, hp_max in ((210, 4, "카이", 100), (400, 3, "엘라", 90), (590, 2, "세드릭", 130))]
            + [(780, {"row": 1, "name": "아스트리드", "hp": 0, "max": 120, "dog": "유예 3.0초 · 피격 1/3"}, "danger")]
            + [(cx, {"row": row, "name": "동굴 쥐", "hp": 40, "max": 40}, "enemy") for cx, row in ((1140, 1), (1330, 2), (1520, 3), (1710, 4))])
UNITS_09 = [(225, {"row": 1, "name": "미라", "hp": 0, "max": 80, "dog": "피격마다 사망 30%", "ghost": 0.31}, "danger"),
            (1140, {"row": 1, "name": "광산 감독관", "hp": 66, "max": 300, "ghost": 0.5}, "enemy")]
SCENES = {
    "battle-05": ("첫 전투(포션을 고른 때: 아군이 대상)", "ko_05_s7_party.png", "ko_05_s7_now.png", BATTLE_FLOOR, UNITS_05,
                  PARTY_BOARDS + [(1050, 1, "동굴 쥐"), (1240, 2, "동굴 쥐")]),
    "battle-15": ("4층 전투(보호막·화상, 적이 걸어 들어옴)", "ko_15_s7_lowered.png", "ko_15_s7_now.png", BATTLE_FLOOR, UNITS_15,
                  [(120, 4, "카이"), (310, 3, "엘라"), (500, 2, "세드릭"), (690, 1, "아스트리드"), (1050, 1, "고블린 약탈자"), (1240, 2, "고블린 주술사")]),
    "battle-18": ("빈사", "ko_18_s7_lowered.png", "ko_18_s7_now.png", BATTLE_FLOOR, UNITS_18,
                  [(120, 4, "카이"), (310, 3, "엘라"), (500, 2, "세드릭"), (690, 1, "아스트리드")]
                  + [(x0, row, "동굴 쥐") for x0, row in ((1050, 1), (1240, 2), (1430, 3), (1620, 4))]),
    "battle-09": ("보스 전투(150% 감독관, 빈사)", "ko_09_s7_party.png", "ko_09_s7_now.png", BATTLE_FLOOR, UNITS_09,
                  [(690, 1, "미라"), (1050, 1, "광산 감독관")]),
    "map": ("노드 맵", "ko_04_s7_party.png", "ko_04_s7_now.png", PARTY_FLOOR, party_units(PARTY_04), PARTY_BOARDS),
    "reward": ("보상", "ko_07_s7_party.png", "ko_07_s7_now.png", PARTY_FLOOR, party_units(PARTY_07), PARTY_BOARDS),
}


def job_line(layer, cx, floor, job):
    """The party side's state line: the job, where the battle's states are."""
    P.word(ImageDraw.Draw(layer), (cx - 80 * Z + 6 * Z, (floor + 34 + 9) * Z), job, 15 * Z, TEXT_DIM)


def proposal(name):
    title, shot, _, floor, units, boards = SCENES[name]
    base = Image.open(GAME / shot).convert("RGBA")
    out = base.copy()
    for x0, _, _ in boards:
        out.alpha_composite(base.crop((x0 - 8, 632, x0 + 188, 1066)), (x0 - 8, 644))
    P.FLOOR = floor
    layer = Image.new("RGBA", (out.width * Z, out.height * Z), (0, 0, 0, 0))
    for cx, unit, state in units:
        P.draw_unit(layer, 2, cx * Z, unit, state)
        if unit.get("job"):
            job_line(layer, cx * Z, floor, unit["job"])
    P.FLOOR = 506
    for x0, row, label in boards:
        M.strip(layer, ((x0 + 1) * Z, 622 * Z, (x0 + 179) * Z, 644 * Z), row, label, 14)
    out.alpha_composite(layer.resize(out.size, Image.LANCZOS))
    return out.convert("RGB")


def main():
    for name, (title, shot, now, floor, units, boards) in SCENES.items():
        proposal(name).save(HERE / f"mock4-{name}-full.png")
    for name in ("battle-05", "battle-15", "battle-18", "battle-09"):
        title, _, now, _, _, _ = SCENES[name]
        box = (0, 0, 1920, 1080)
        print(f"mock4-{name}.png", M.stack([M.band(Image.open(GAME / now).convert("RGB"), box, f"{title} — 지금", True),
                                             M.band(Image.open(HERE / f"mock4-{name}-full.png").convert("RGB"), box, f"{title} — 2안 + 이름 B + 무대 48 내림")],
                                            f"mock4-{name}.png"))
    # The party side: the left half of the node map and of the reward screen (the right half does not change), now | proposal.
    box = (0, 0, 1000, 1080)
    cells = []
    for name in ("map", "reward"):
        title, _, now, _, _, _ = SCENES[name]
        cells.append((M.band(Image.open(GAME / now).convert("RGB"), box, f"{title} — 지금", True),
                      M.band(Image.open(HERE / f"mock4-{name}-full.png").convert("RGB"), box, f"{title} — 2안 + 이름 B, 그림 46 내림")))
    w, h = cells[0][0].size
    sheet = Image.new("RGB", (2 * w + 8, 2 * h + 8), (12, 12, 12))
    for i, (a, b) in enumerate(cells):
        sheet.paste(a, (0, i * (h + 8)))
        sheet.paste(b, (w + 8, i * (h + 8)))
    sheet.save(HERE / "mock4-party.png")
    print("mock4-party.png", sheet.size)
    print("explain-header-stud.png", explain())


def explain():
    """What the header strip and the gold stud are: the two sample pieces, where the proposal uses them, what they replace."""
    f, small = P.font(30), P.font(22)
    sheet = Image.new("RGB", (1600, 1130), (24, 22, 22))
    d = ImageDraw.Draw(sheet)
    d.text((24, 18), "머리 띠와 금 징 — Round 29에서 만든 견본 조각 (ArtPipeline/output, 게임에는 아직 없음)", fill=(235, 235, 230), font=f)
    card = (53, 50, 53)
    d.rectangle((24, 76, 840, 330), fill=card)
    tag = Image.open(P.UI / "ui_frame/tag29_slim.png").convert("RGBA")
    sheet.paste(tag.resize((720, 120), Image.LANCZOS), (72, 110), tag.resize((720, 120), Image.LANCZOS))
    d.text((44, 252), "머리 띠 (tag29_slim, 360x60): 검게 그을린 쇠의 낮은 띠,", fill=(217, 164, 65), font=small)
    d.text((44, 284), "안쪽 가장자리의 가는 금선, 양 끝의 작은 리벳", fill=(217, 164, 65), font=small)
    d.rectangle((864, 76, 1576, 330), fill=card)
    stud = Image.open(P.UI / "ui_piece/badge29_a_stud.png").convert("RGBA").resize((168, 168), Image.LANCZOS)
    sheet.paste(stud, (884, 100), stud)
    d.text((1072, 130), "금 징 (badge29_a_stud, 64x64):", fill=(217, 164, 65), font=small)
    d.text((1072, 162), "오래된 금빛의 둥근 징(못 머리).", fill=(217, 164, 65), font=small)
    d.text((1072, 194), "테두리가 솟은 납작한 원", fill=(217, 164, 65), font=small)
    d.text((1072, 238), "안에 열 번호를 적는다", fill=(217, 164, 65), font=small)
    d.text((24, 356), "적용안: 보드마다 머리에 띠를 늘여 깔고, 왼쪽 끝 징에 열 번호, 그 오른쪽에 이름 (4층 전투, 2배)", fill=(235, 235, 230), font=f)
    crop = Image.open(HERE / "mock4-battle-15-full.png").convert("RGB").crop((112, 612, 872, 700))
    sheet.paste(crop.resize((crop.width * 2, crop.height * 2), Image.LANCZOS), (40, 404))
    d.text((24, 600), "지금: 유닛마다의 명패 왼쪽 위에 열 번호 배지 — 그린 그림이 아니라 도형 원 (같은 장면, 2배)", fill=(235, 235, 230), font=f)
    now = Image.open(GAME / "ko_15_s7_now.png").convert("RGB")
    for i, x0 in enumerate((112, 302, 492, 682)):
        c = now.crop((x0, 508, x0 + 190, 612))
        sheet.paste(c.resize((c.width * 2, c.height * 2), Image.LANCZOS), (40 + i * 386, 648))
    d.text((24, 878), "적용안에서 이 판(명패)은 없어진다: 체력은 발밑의 숫자 막대, 열 번호와 이름은 아래 보드의 머리 띠로 간다.", fill=(235, 235, 230), font=small)
    d.text((24, 914), "노드 맵·보상의 파티 쪽 보드에도 같은 머리 띠. 두 조각은 이번 Round의 이미지 생성 호출로 만든 것이라, 쓰려면 승인이 필요하다.", fill=(235, 235, 230), font=small)
    sheet = sheet.crop((0, 0, 1600, 960))
    sheet.save(HERE / "explain-header-stud.png")
    return sheet.size


if __name__ == "__main__":
    main()
