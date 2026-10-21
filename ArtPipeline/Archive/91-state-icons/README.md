# Round 91 — 상태 아이콘 일곱: 빙결·암흑·조준·예리·격분·가속·취약 (2026-10-21) — 확정·연결(판정 2)

지시: 2026-10-21 사용자 **"가속, 취약 넣는 방향으로 구현 착수 / 모든 효과는 적/아군 서로 가리지 않고 적용될 수 있게 구현"**(Design/18 §10의 판정. 판정 8 "어휘마다 상태 아이콘 1회씩 생성 → 승인 뒤 칩과 함께 연결"은 권장안).
45단계(2차 효과 어휘)의 일곱 상태다. Round 90의 셋(출혈·재생·가시)과 같은 길: `ui_icon` 타입, 96x96, 외곽선 6, 호출 상한 7(항목당 한 번).
연결(배선)은 하지 않았다: 승인된 것만 `Assets/@Art/UI/Icon`에 넣고 상태 줄에 칩을 더한다(Round 90의 꼴).

## 소재 (`ArtPipeline/Rosters/ui_icon.csv`)

| Key | 상태 | Subject |
|---|---|---|
| `freeze` | 빙결 | a single big six-pointed ice crystal, pale icy blue with a lighter core, its six arms short and thick |
| `blind` | 암흑 | a single closed eye seen from the front, big, the lid dark purple with a few heavy lashes below it |
| `aim` | 조준 | a round archery target seen from the front: a red center dot inside one cream ring and one red ring |
| `keen` | 예리 | a short broad steel blade standing point up, bright silver with a thin pale gleam along its edge |
| `fury` | 격분 | a clenched fist seen from the front, deep red, thick and blocky |
| `haste` | 가속 | a single small bird wing seen from the side, cream with brass-brown tips, swept back as if in flight |
| `vulnerable` | 취약 | a grey heater shield seen from the front, split by one wide jagged crack running from its top edge down through its middle |

## 후보

| 파일 | 무엇 |
|---|---|
| `candidates/<key>.png` 일곱 | 맞춘 후보 96x96(원본 `.raw.png`은 `ArtPipeline/output/ui_icon/`) |
| `mock-chips.png` | 상태 줄의 칩으로(아이콘 18px + 글 15px), 지금의 방어·화상·출혈 칩 옆에, 화면 크기·2배·4배(`mock_chips.py`. 글의 색은 견본: 정하는 것은 연결 때) |

- **빙결**: 여섯 팔의 얼음 결정, 옅은 하늘색에 밝은 심. 18px에서 눈꽃으로 읽힌다.
- **암흑**: 감은 눈, 어두운 보라 눈꺼풀과 속눈썹. 18px에서는 어두운 타원으로 보여 일곱 가운데 가장 읽힘이 약하다 — 약하면 재생성 방향은 "눈꺼풀을 밝은 보라로, 속눈썹을 굵게".
- **조준**: 붉은 과녁(크림 고리 하나, 붉은 고리 하나, 붉은 심). 18px에서 과녁으로 읽힌다.
- **예리**: 끝을 위로 세운 짧은 강철 검, 놋쇠 날밑. 2배부터 검으로 읽히고 18px에서는 가는 세로획(느낌표꼴)이 된다 — 약하면 재생성 방향은 "날을 넓게, 날밑을 크게, 비스듬히".
- **격분**: 붉은 주먹. 18px에서 붉은 덩어리로 읽힌다(주먹의 손가락은 2배부터).
- **가속**: 크림색 날개 하나, 끝은 놋쇠빛. 18px에서 날개로 읽힌다.
- **취약**: 가운데가 갈라진 회색 방패. 18px에서 방패의 금이 보인다.
- 호출 7회, 약 $0.20(`Archive/calls.csv` 2026-10-21 08:42~08:44). 누계 328회 약 $8.87(상한 $10).

## 판정 1 (2026-10-21)

사용자 **"예리는 다른 아이콘 없을까? 가속은 노란색 번개 모양으로 재생성. 승인 후 연결"**, 이어 **"암흑은 흑먼지 스타일로 재생성. 승인 후 연결"**: 셋을 소재를 바꿔 1회씩 재생성(상한을 올려 10). 빙결·조준·격분·취약은 그대로.

- **예리**의 대안으로 낸 것(호출 없음): ① 날의 반짝임(네 꼭지 빛, 권장 — 18px에서 가장 또렷하고 조준·격분과 겹치지 않음) ② 세 줄 베인 자국(붉은 발톱 자국) ③ 교차한 단검 둘 ④ 급소의 붉은 X 표. ①을 그렸다: `candidates/keen_v2.png` — 크림색 네 꼭지 빛에 붉은 심. 18px에서 ✦로 읽힌다.
  소재: a single big four-pointed star of light, the glint of a sharpened edge: a cream white star with long sharp points, a small deep red core, drawn alone.
- **가속**: `candidates/haste_v2.png` — 노란 번개 하나, 아래를 향한 두 번 꺾임, 밝은 노란 심. 18px에서 ⚡로 읽힌다(폭풍 아이콘의 구름 없는 번개와 구별되도록 번개만).
  소재: a single big lightning bolt pointing down, bright yellow with a paler yellow core, thick and zigzagging twice, drawn alone with no cloud. `--extra "Only the bolt: no cloud, no sky, no storm."`
- **암흑**: `candidates/blind_v2.png` — 검은 먼지 구름(둥근 검댕 덩어리, 회색 가장자리, 위로 튀는 알갱이 몇). 18px에서 어두운 뭉게 꼴로 읽힌다(1차의 감은 눈보다 윤곽이 분명).
  소재: a single puff of black dust: a rounded billowing cloud of soot, black and dark grey with a slightly lighter grey edge, a few small specks flying off its top, drawn alone. `--extra "A cloud of black dust only: no eye, no face, no smoke stack, no ground."`
- `mock-chips-v2.png`: 셋을 바꿔 넣은 칩 견본. 호출 3회 약 $0.086. Round 91 누계 10회 약 $0.29, 전체 누계 331회 약 $8.95(상한 $10).

## 판정 2 (2026-10-21) — 확정·연결

사용자 **"연결"**: 빙결 1차·암흑 2차·조준 1차·예리 2차·격분 1차·가속 2차·취약 1차의 일곱을 확정. 원본은 `approved/`(`<key>.png`·`<key>.raw.png` — `blind`·`keen`·`haste`는 2차의 것). 배선은 아래 "판정 (경위)"대로: `Assets/@Art/UI/Icon/freeze.png`·`blind.png`·`aim.png`·`keen.png`·`fury.png`·`haste.png`·`vulnerable.png`(96x96), `UiArt.Freeze`~`Vulnerable`, 상태 줄의 칩 일곱(`UnitFreeze`~`UnitVulnerable`, 가시 다음에 그 차례, 글은 중첩 수, 색은 견본 `mock-chips-v2.png`의 것 — `UiPalette.Freeze`~`Vulnerable`), 문구 일곱(`Battle.Freeze`~`Vulnerable`), `BattleUnitView`의 보임(빈사 중에는 숨김), Architecture/12 "발밑 표시". 호출은 없다(Round 91 누계 10회 약 $0.29 그대로). 떠오르는 글·소리는 없다(연출은 목업 뒤). 체인은 Handoff `d44-effect-vocabulary.md`.

## 판정 (경위)

항목별로 "확정" / "재생성 - 방향" / "소재 교체". 확정되면 Round 90과 같이: `Assets/@Art/UI/Icon/<key>.png`, `UiArt`의 주소 일곱, 상태 줄의 칩 일곱(`BuildStateChip`, 화상·출혈·재생·가시 다음에 빙결·암흑·조준·예리·격분·가속·취약의 차례), `BattleUnitView`의 보임, 문구 `Battle.<Key>` 일곱(수만), `UiPalette`의 색 일곱, 체인 한 번.
