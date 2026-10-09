# -*- coding: utf-8 -*-
"""Round 48, the whole so far (2026-10-08, the user: "2안으로 반영. 이제것 목업 반영된 것들을 목업으로 보여줘").
One look for everything the round has drawn: the B + C board (squares, turning, stars, bags), no cream behind items, the bag's
empty squares dark, nothing where no bag is (the third ask; 안 2's one piece per item, recommended), and the cooldown of the
fifth ask's 안 2 (the user's pick: the item's own colours come back from the left with a sharp edge, no gold; when all lit it
swells to 1.12 and is a moment brighter, then the dark comes back). Node map, the hands' flow, the card, the battle (still and
MP4), and today's game beside it. No API call.
  .venv/bin/python ArtPipeline/Archive/48-grid-board/mock_final.py
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import mock_bags4 as B4  # noqa: E402  (the fifth ask's cooldown; through it the third ask's ground and the B + C pieces)

B3 = B4.B3; B2 = B4.B2; B = B4.B; G = B.G; M = B.M; font = G.font
TEXT, DIM, VIRTUE, GOOD, DANGER = G.TEXT, G.DIM, G.VIRTUE, G.GOOD, G.DANGER
LOOK = B2.LOOKS[1]
COOLDOWN = "plain2"
PARTY_X = G.PARTY_X


def map_screen():
    return B2.map_full(LOOK)


def unpicked():
    """The node map's boards with nothing picked: in frames 2 to 6 the hand holds something from the inventory or the loot."""
    return [(bags, [dict(i, sel=False) for i in items]) for bags, items in B.scene_map()]


def calm():
    return B2.map_look(LOOK, unpicked())


def flows():
    frames = []
    crop = B.CROP

    img = B2.map_look(LOOK)
    cx, cy = B.cell_xy(PARTY_X[2], 0, 1); G.cursor(img, cx + 70, cy + 30)
    frames.append(("① 누르면 고름: 금 테와 옅은 금빛. 우클릭은 아이템 정보 카드", img.crop(crop)))

    img = calm(); G.ghost(img, PARTY_X[2], 0, 2, 2, 1, GOOD, icon_id="ember_flask", label="★ 하나 걸림")
    B.star_at(img, PARTY_X[2], 1.0, 2.0, False); B.star_at(img, PARTY_X[2], 2.0, 2.5, True)
    cx, cy = B.cell_xy(PARTY_X[2], 0, 2); G.cursor(img, cx + 70, cy + 30)
    frames.append(("② 인벤토리의 플라스크를 들고 움직이면 놓일 칸이 초록, 닿을 ★을 미리(밝음 = 걸림)", img.crop(crop)))

    img = calm(); G.ghost(img, PARTY_X[3], 1, 4, 2, 1, VIRTUE, icon_id="dagger", label="약초 주머니 → 인벤토리")
    cx, cy = B.cell_xy(PARTY_X[3], 1, 4); G.cursor(img, cx + 44, cy + 26)
    frames.append(("③ 단검을 들고 하나와 겹치면 금: 겹친 것은 인벤토리로", img.crop(crop)))

    img = calm(); G.ghost(img, PARTY_X[2], 0, 1, 3, 2, DANGER, icon_id="longbow", label="플라스크·단검과 겹침")
    cx, cy = B.cell_xy(PARTY_X[2], 0, 1); G.cursor(img, cx + 90, cy + 60)
    frames.append(("④ 전리품의 장궁(3×2)을 들고 둘 이상과 겹치면 빨강: 놓이지 않음", img.crop(crop)))

    img = calm(); G.ghost(img, PARTY_X[2], 0, 2, 2, 3, GOOD, icon_id="longbow", rot=True, label="R: 돌림 → 2×3")
    cx, cy = B.cell_xy(PARTY_X[2], 0, 2); G.cursor(img, cx + 60, cy + 90)
    frames.append(("⑤ R 또는 휠로 돌리면 2×3: 배낭과 주머니에 걸쳐 초록", img.crop(crop)))

    img = calm()
    for x0, (bags, _) in zip(PARTY_X, B.scene_map()):
        cells = set().union(*[B.cells_of(b) for b in bags])
        for gx in range(3):
            for gy in range(B.ROWS):
                if (gx, gy) not in cells: B.dashed_cell(img, x0, gx, gy, color=(120, 108, 92))
    B.held_bag(img, PARTY_X[0], B.bagd("pouch", 0, 4), GOOD, "가방 놓기: 칸 +3")
    frames.append(("⑥ 가방을 든 동안만 틀(3×6)의 빈 자리가 점선. 가방을 옮기면 안의 아이템도 함께", img.crop(crop)))
    return frames


def card_screen():
    img = B2.map_look(LOOK)
    B.place_card_above(img, PARTY_X[2], 0, 1, 2, "불씨 플라스크 · 등급 8", "공격 아이템 / 크기 2×1 / 쿨다운 4.5초 / 어디서나 발동",
                       ["맨 앞 적에게 화상 2"],
                       [[("★ 닿은 무기 장비가 피해를 주면 그 적에게 화상 +1(예시)", VIRTUE)], [("지금 닿음: 롱소드, 단검 (★ 둘 다 밝음)", DIM)]])
    G.panel_lines(img, [("불씨 플라스크 · 등급 8 — 공격 아이템 / 크기 2×1 / 쿨다운 4.5초 / 맨 앞 적에게 화상 2 / ", TEXT), ("★ 닿은 무기 장비가 피해를 주면 화상 +1(예시)", VIRTUE)],
                  [("우클릭: 아이템 정보. 카드는 보드 위, 아래 꼭지가 그 아이템을 가리킵니다.", TEXT)])
    return img


def battle():
    cols, _, (bg, party, enemies) = B3.scenes()

    def frame_at(t):
        B.DARK = B4.DARK; im = B.battle_frame(bg, cols, party, enemies, t, COOLDOWN); B.DARK = 0.20
        return im

    still = frame_at(2.3); still.convert("RGB").save(HERE / "mock-final-battle.png"); print("mock-final-battle.png")

    def frame(k):
        t, speed = B.timeline(k); im = frame_at(t)
        d = ImageDraw.Draw(im); d.rounded_rectangle([1640, 1024, 1900, 1062], radius=6, fill=G.INK + (220,))
        d.text((1652, 1031), f"{speed} · {t:4.2f}초", font=font(18), fill=TEXT)
        return im
    B.write_video(frame, 16 * B.FPS, HERE / "mock-final-battle.mp4")
    return still


def main():
    m = map_screen(); m.convert("RGB").save(HERE / "mock-final-map.png"); print("mock-final-map.png")
    c = card_screen(); c.convert("RGB").save(HERE / "mock-final-card.png"); print("mock-final-card.png")
    M.sheet(flows(), 2, HERE / "mock-final-flow.png", size=21,
            footer=["모두 크림 없는 바탕(아이템의 칸은 한 조각). 가방 밖의 자리는 가방을 든 동안만 보인다.",
                    "★ 효과의 내용(플라스크·버클러·약초 주머니·수호 부적)은 그림을 위한 예시다."])
    b = battle()
    half = lambda im: im.convert("RGB").resize((960, 540), Image.LANCZOS)
    M.sheet([("지금 게임: 노드 맵", half(G.shot("ko_35_map_tiers"))), ("Round 48 반영: 노드 맵", half(m)),
             ("지금 게임: 전투", half(G.shot("ko_05_battle"))), ("Round 48 반영: 전투(쿨다운 안 2)", half(b))],
            2, HERE / "mock-final-compare.png", size=24,
            footer=["지금 게임의 장면은 스크린샷 20261008-r47b. 반영 쪽은 같은 장면 위에 보드만 다시 그렸다(무대·패널·지도는 그대로)."])


if __name__ == "__main__":
    main()
