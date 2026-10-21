# Round 90 — 상태 아이콘: 출혈·재생·가시 (2026-10-21) — 판정 대기

지시: 2026-10-21 사용자 **"출혈·재생,가시에 대한 아이콘 생성. 연결은 내 승인 후"**(같은 메시지의 앞 줄 "흡혈은 삭제"는 44단계에서 흡혈을 뺀 것으로 처리했다).
44단계(효과 어휘, Design/18)의 유닛 상태 셋이다. 정화·경직·충전은 그 자리에서 끝나는 효과라 상태가 없고, 지금의 상태 아이콘 넷(방어·화상·빈사·폭풍)에 셋을 더한다.
연결(배선)은 하지 않았다: 승인 뒤 상태 칩의 자리는 Design/18 판정 11의 **목업 라운드**와 함께 정한다.

## 소재 (`ArtPipeline/Rosters/ui_icon.csv`, `ui_icon` 타입, 96x96, 외곽선 6)

| Key | Subject |
|---|---|
| `bleed` | a single large drop of dark red blood falling, with two smaller drops beside it and a small light highlight on the big one |
| `regen` | a green sprout: a short stem with two broad leaves, one on each side, growing upward |
| `thorns` | a short curved bramble branch in dark green with three big sharp thorns pointing outward |

기준 그림은 타입의 것(`References/Character/style_ref_roster.png`), 프롬프트는 §14·§16·§17·§18(지금의 화상·방어 아이콘과 같은 길). 호출 상한 3(항목당 한 번).

## 후보

| 파일 | 무엇 |
|---|---|
| `candidates/bleed.png`, `candidates/regen.png`, `candidates/thorns.png` | 맞춘 후보 96x96(원본 `.raw.png`은 `ArtPipeline/output/ui_icon/`) |
| `mock-chips.png` | 유닛 발밑의 상태 줄처럼: 아이콘 18px + 글 15px의 칩을 **화면 크기·2배·4배**로, 지금의 방어·화상 칩 옆에(글의 색은 견본: 출혈 붉음·재생 초록·가시 연두 — 정하는 것은 목업 라운드) |

- **출혈**: 큰 핏방울 하나와 작은 둘, 밝은 점. 18px에서도 방울 셋이 읽힌다.
- **재생**: 짧은 줄기에 넓은 잎 둘. 초록 한 톤과 어두운 잎맥. 18px에서 새싹으로 읽힌다.
- **가시**: 비스듬한 가시 가지, 큰 가시 셋. 18px에서는 가지가 선처럼 가늘어져 셋 가운데 가장 작게 보인다 — 화면 크기에서 읽힘이 약하면 재생성 방향은 "가지를 굵게, 가시를 더 크게 둘로".
- 호출 3회, 약 $0.086(`Archive/calls.csv` 2026-10-21 07:10). 누계 318회 약 $8.58(상한 $10).
- `tools/review_sheet.py`는 `UiPalette.cs`의 Party 색을 읽지 못해 돌지 않았다(도구가 팔레트의 지금 이름과 어긋남 — 이번 라운드는 `mock-chips.png`로 대신). 도구의 수정은 다음 라운드에.

## 판정 1 (2026-10-21)

사용자 **"가시만 한개나 두개만 나오게(가지 없이)(흰색) 재생성"**: 출혈·재생은 그대로(연결은 승인 뒤), 가시는 **소재 교체 + 재생성 1회**(상한을 1 올려 4).

- 새 소재(`Rosters/ui_icon.csv`의 `thorns` 행을 바꿈): two big sharp thorns standing side by side and pointing upward, bone white, drawn alone with no branch, no stem and no leaf. 전의 소재(가지에 가시 셋, 어두운 초록)는 위 표에 남긴다.
- `candidates/thorns_v2.png`(원본 `output/ui_icon/thorns_v2.raw.png`): 뼈빛 흰색의 큰 가시 둘이 나란히 위를 향하고, 가지·줄기·잎 없음. 18px에서 흰 V 꼴의 두 뾰족이로 읽혀 1차보다 또렷하다(`mock-chips-v2.png`: 글의 색 견본은 흰색으로).
- 호출 1회, 약 $0.029. Round 90 누계 4회 약 $0.11, 전체 누계 319회 약 $8.61(상한 $10).

## 판정 2 (2026-10-21)

사용자 **"너무 얇아. 이전에 가지에 달린 가시 느낌으로 재생성"**: 가시 2차(가지 없는 흰 가시 둘)는 가늘어 보여 거절. 1차의 "가지에 달린 가시"로 돌아가되 굵게, 흰 가시 둘은 유지 — **소재 교체 + 재생성 1회**(상한 5).

- 새 소재(`Rosters/ui_icon.csv`의 `thorns` 행): a short thick dark brown branch with two big heavy sharp thorns growing from it, the thorns bone white and broad at their base, the branch as thick as the thorns. `--extra "Make it chunky: the branch and both thorns are thick and bold, nothing thin or spindly."`
- `candidates/thorns_v3.png`(원본 `output/ui_icon/thorns_v3.raw.png`): 비스듬한 굵은 흑갈색 가지에 뼈빛 흰색의 큰 가시 둘(밑이 넓고 끝이 뾰족), 잘린 끝의 나이테. 18px에서 어두운 막대에 흰 뾰족이 둘로 읽혀 2차보다 묵직하다(`mock-chips-v3.png`).
- 호출 1회, 약 $0.029. Round 90 누계 5회 약 $0.14, 전체 누계 320회 약 $8.64(상한 $10).

## 판정 3 (2026-10-21)

사용자 **"해당 가시를 크게하고 가지는 제거하여 재생성"**: 3차의 굵은 흰 가시 둘은 좋고, 가지는 빼고 가시를 키운다 — **소재 교체 + 재생성 1회**(상한 6).

- 새 소재(`Rosters/ui_icon.csv`의 `thorns` 행): two big heavy sharp bramble thorns, bone white, each a broad thick wedge with a wide base and a pointed tip, standing side by side and filling the picture, drawn alone with no branch, no stem and no leaf. `--extra "Make the two thorns as big and heavy as possible: each one a broad thick wedge, as chunky as a tusk, nothing thin or spindly, filling the canvas edge to edge with only a small margin. No branch at all."`
- `candidates/thorns_v4.png`(원본 `output/ui_icon/thorns_v4.raw.png`): 뼈빛 흰색의 넓은 쐐기 가시 둘이 캔버스를 거의 채운다(생성 캔버스의 97%). 가지 없음. 18px에서 흰 뾰족이 둘이 굵게 읽힌다(`mock-chips-v4.png`) — 2차(가는 둘)와 달리 밑이 넓어 묵직하다.
- 호출 1회, 약 $0.029. Round 90 누계 6회 약 $0.17, 전체 누계 321회 약 $8.67(상한 $10).

## 판정 4 (2026-10-21) — 확정·연결

사용자 **"이걸로 가자 연결"**: 출혈 1차·재생 1차·가시 4차 확정. 원본은 `approved/`(`bleed`·`regen`·`thorns` = `thorns_v4`의 `.png`·`.raw.png`). 배선은 아래 그대로: `Assets/@Art/UI/Icon/bleed.png`·`regen.png`·`thorns.png`(96x96), `UiArt.Bleed`·`Regen`·`Thorns`, 상태 줄의 칩 셋(`UnitBleed`·`UnitRegen`·`UnitThorns`, 화상 다음에 그 차례, 글의 색 핏빛·초록·흰 글 — 견본 `mock-chips-v4.png`의 것), 문구 셋(`Battle.Bleed`·`Regen`·`Thorns`), Architecture/12 "발밑 표시". 떠오르는 글·소리는 없다(연출은 목업 뒤). 체인은 Handoff `d44-effect-vocabulary.md`.

## 판정 (경위)

항목별로 "확정" / "재생성 - 방향" / "소재 교체". 확정되면:

1. `Assets/@Art/UI/Icon/bleed.png`·`regen.png`·`thorns.png`로 복사(96x96 그대로, UI 아이콘의 Import 정책), `UiArt`의 주소 셋, `ArtSetup`의 목록.
2. 유닛 발밑의 상태 줄에 칩 셋(`UiPrefabSetup.Battle.BuildStateChip`: 화상 칩과 같은 꼴 — 아이콘 18px + "출혈 N"·"재생 N"·"가시 N"), `BattleUnitView`가 `Bleed`·`Regen`·`Thorns`로 보이고 숨김. 글의 색과 칩의 차례는 목업 라운드(Design/18 판정 11)에서 정한다.
3. 문구 셋(`Battle.Bleed`·`Battle.Regen`·`Battle.Thorns`), 체인 한 번.
