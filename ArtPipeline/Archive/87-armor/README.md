# Round 87 — 방어구: 철 갑옷·철 투구 (2026-10-19) — 판정됨: 목업, 아이콘 둘 확정·배선

판정(2026-10-19 사용자 **"권장안 구현 및 이미지 api를 통한 아이콘 생성 보고. 내 승인 후 연결"**): 화면은 목업대로(권장안) 구현했다(Architecture/12 "방어구"). 아이콘은 생성해 아래 "아이콘"에 보고했고, 승인 뒤에 배선한다.

지시: 2026-10-19 사용자 "방어구 아이템 신설 검토 / 갑옷-전투 시작시 방어력 부여(2*2) / 투구-전투 시작시 소폭 방어력 부여/치명타 확률 감소(2*1)",
"장비-방어구 타입으로 피로 소모 추가 검토" → 검토 보고(판정할 것 아홉) → "8. 철 갑옷, 철 투구로 생성 / 나머지 권장안 구현".
판정 9(권장안): 화면은 구현 전에 이 목업으로 승인받는다. 목업에는 그림 생성 API를 부르지 않았다(아이콘은 게임의 Placeholder — 그림이 없는 아이템은 파란 조각에 이름).

## 그림

| 파일 | 무엇 |
|---|---|
| `mock-shop.png` | 상점(4층): 철 갑옷 2×2(값 20)·철 투구 2×1(값 12), 철 투구의 툴팁, 로언의 보드에 산 둘(투구는 세워 돌림)과 조각의 "+1"·머리의 "피로 +2" |
| `mock-battle.png` | 전투 0.0초: 두 조각이 한 번 발동해 밝아짐, 로언 위 "방어 +5"·"방어 +2", 보호막 배지 13(선봉 5.45 + 갑옷 5.45 + 투구 2.18), 철 투구의 카드 |

- 바탕은 `20261019-traits38`의 `ko_42`·`ko_43`(상점), `ko_30`(피로 알약), `ko_40`·`ko_09`(31층 전투). 다시 그리기: `.venv/bin/python ArtPipeline/Archive/87-armor/mock_armor.py`

## 화면에 새로 생기는 것

- 카드·툴팁의 사실 줄: 쿨다운 자리에 **"전투 시작 때 발동"**(새 문구). 효과 줄에 **"받는 치명타 확률 −3%p"**(새 문구, 효과의 파랑). 나머지 줄(분류·크기·보호막·피로·값)은 지금 문구 그대로.
- 전투: 0초에 지금의 발동 연출(칸의 번쩍임, 방어 분류의 움직임·소리, "방어 +N")이 한 번. 그 뒤 충전 막대는 없다(숫돌처럼 늘 밝음).
- 보드: 방어 장비라 조각마다 "+1"과 머리의 합계 — 지금 규칙 그대로.
- 아이콘: 그림이 생기기 전에는 이름의 Placeholder. 그림은 이미지 API를 요청받으면.

## 구현 (2026-10-19)

- `impl-map.png`(`ko_79_map_armour_card`), `impl-battle.png`(`ko_80_battle_armour_start`), `impl-battle-card.png`(`ko_81_battle_armour_card`): 스크린샷 `20261019-armour39c`(아이콘 둘을 배선한 뒤. 처음 대조는 Placeholder의 `20261019-armour39`).
- 목업과 대조: 카드의 세 사실 줄("방어 장비 / 크기 2×1 / 전투 시작 때 발동")·파란 효과 두 줄·피로 줄, 조각의 "+1"과 머리의 "피로 +2", 전투 0초의 "방어 +2"·"방어 +5"와 보호막 배지 13 — 목업과 같다. 자막에 두 줄("로언의 철 갑옷 → 로언: 보호막 +5")이 더 보인다.
  다른 것: 전투 카드는 지금 규칙대로 보드의 왼쪽에 선다(목업은 오른쪽). Placeholder 때 세운 투구의 이름이 "철 투 / 구"로 줄이 바뀌던 것은 아이콘으로 없어졌다. 조각의 "+1"은 지금 규칙대로 아이콘 위의 보라 글이다.

## 아이콘 (2026-10-19, 판정 대기)

`item` 타입, 기준 그림은 타입의 것(스타일 시트), 호출 상한 2(항목당 한 번). 소재(`ArtPipeline/Rosters/item.csv`):

| Key | 칸 → 캔버스 | Subject |
|---|---|---|
| `iron_armor` | 2×2 → 거의 정사각(§21), 1024x1024 → 328x348 | an iron armor: a grey steel breastplate with big rounded shoulder plates and a short skirt of overlapping steel bands, brown leather straps at the sides, standing upright, seen from the front, with no one inside it |
| `iron_helm` | 2×1 → 띠(§19), 1536x512 → 328x100 | an iron helmet: a rounded grey steel cap with a broad flat brim all around it, like a kettle hat, and a brown leather chin strap hanging below, seen from the side, clearly wider than it is tall |

| 파일 | 무엇 |
|---|---|
| `candidates/iron_armor.png`, `candidates/iron_helm.png` | 맞춘 후보(원본 `.raw.png`은 `ArtPipeline/output/item/`) |
| `review-icons.png` | 두 후보를 화면의 2배·화면 크기로, 지금의 버클러·창·단검 옆에 |
| `review-shop.png`, `review-battle.png` | 목업 둘의 조각에 후보를 넣은 것(`mock_armor.py --icons`) |

- **철 갑옷: 미늘창이 함께 그려졌다**(갑옷 뒤로 비스듬히). 소재에는 무기가 없고, §21의 "긴 것(미늘창 등)은 비스듬히"라는 예가 그대로 들어간 것으로 보인다. 권장: **재생성 1회** — `--extra "Draw the armor alone: no weapon, no halberd, no pole, no spear."`
- 철 투구: 챙 넓은 철모(케틀 햇)에 갈색 턱끈. 띠 캔버스의 폭 53%라 2×1 조각에서는 가운데에 작게 서고, 세운 조각(돌린 것)에서는 칸을 채운다. 권장: **확정**.
- 호출 2회, 약 $0.0505(`Archive/calls.csv`). 누계 314회 약 $8.47(상한 $10).

판정(2026-10-19 사용자 **"1. 재생성 2. 확정"**):

- **철 투구 확정**: 원본 `approved/iron_helm.png`·`.raw.png`. `Assets/@Art/Item/iron_helm.png`(`trim_items.py`로 188x100), `ItemData.csv`의 `Icon` `item/iron-helm`.
- **철 갑옷 재생성 1회**(`--name iron_armor_v2 --extra "Draw the armor alone: no weapon, no halberd, no pole, no spear, nothing behind or across it."`): `candidates/iron_armor_v2.png` — 무기 없이 갑옷만, 어깨판·가죽끈·띠 치마. 첫 후보(`candidates/iron_armor.png`, 미늘창)는 거절. **판정 대기**(권장: 확정). 리뷰 시트 `review-icons.png`·`review-shop.png`·`review-battle.png`는 이 둘로 다시 그렸다.
- 호출 1회, 약 $0.0298. 누계 315회 약 $8.49(상한 $10).

판정(2026-10-19 사용자 **"1. 철 갑옷 확정 2. 철 투구 방향 반대로 수정"** → 방향의 뜻은 선택지 셋(지금 / A 좌우 뒤집기 / B 위아래 뒤집기)으로 물어 **"A. 좌우 뒤집기"**):

- **철 갑옷 확정**: 원본 `approved/iron_armor.png`·`.raw.png`(= `iron_armor_v2`). `Assets/@Art/Item/iron_armor.png`(`trim_items.py`로 328x336), `Icon` `item/iron-armor`.
- **철 투구 좌우 뒤집기**(호출 없음, PIL `ImageOps.mirror`): 턱끈 고리가 왼쪽. `approved/iron_helm.png`와 `Assets/@Art/Item/iron_helm.png`가 뒤집은 것이고, 뒤집기 전은 `candidates/iron_helm.png`·`approved/iron_helm.raw.png`.
