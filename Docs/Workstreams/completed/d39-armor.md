# Slice D 39단계: 방어구

Snapshot: 2026-10-19 (완료: 규칙·데이터·엔진·시뮬·화면·아이콘·Test·문서. 2026-10-19 세션 정리에서 커밋·푸시)

## Goal
방어 장비 둘 — 철 갑옷 2×2·철 투구 2×1 — 이 상점과 전리품에 나오고, 전투 시작 때 한 번 보호막을 주며(철 투구는 받는 치명타도 줄임), 장비 피로 +1을 낸다(Design/02 §3·§4 "방어구", 04 §3). 화면은 Round 87 목업대로. 범위는 Architecture/09 "Slice D" 구현 순서 7-1·8.

## State
- 2026-10-19 사용자 "방어구 아이템 신설 검토 …", "장비-방어구 타입으로 피로 소모 추가 검토" → 검토 보고(판정할 것 아홉) → "8. 철 갑옷, 철 투구로 생성 / 나머지 권장안 구현"으로 시작했다. 맞추기는 40단계로 옮겼다.
- Round 87 목업(`ArtPipeline/Archive/87-armor/`)은 처음 "대기"였고, 같은 날 "권장안 구현 및 이미지 api를 통한 아이콘 생성 보고. 내 승인 후 연결"로 화면을 목업대로 구현했다.
- 아이콘(호출 3회): 사용자 "1. 재생성 2. 확정" → "1. 철 갑옷 확정 2. 철 투구 방향 반대로 수정"("A. 좌우 뒤집기") — 둘 다 확정·배선(README "아이콘").
- 곁가지(39단계 밖, 따로 커밋): 같은 날 사용자 플레이 테스트 보고 "상점 물건을 든 채 우클릭하면 회전 안 됨" → 확인 결과 10-18 결정(우클릭 = 제자리로)대로의 동작 → 사용자 "우클릭으로 돌리기를 다시 원함" → `TurnInput`의 우클릭은 다시 시계 방향 90°, 제자리로는 Esc(Design/03 §5·02 §4, Architecture/12, 문구 넷).
- 2026-10-19 세션 정리에서 주제별로 커밋·푸시했다.

## Done
- 기획: Design/02 §3(전투 시작 아이템의 예외)·§4("방어구", 치명타 확률의 뺌)·04 §3(피로는 지금 규칙)·07(열 둘)·00 "Slice D"(판정 표)·08 §46(시뮬).
- 데이터: `ItemData.csv`의 선택 열 `Trigger`·`CritTakenPercent`, 행 `iron_armor`·`iron_helm`. Generated 다시 만듦.
- Data: `ItemTrigger`, `ItemData.Trigger`·`CritTakenPercent`·`Cycles`와 검사, 매퍼.
- Domain: `BattleEngine.ApplyBattleStartItems`(패시브 다음, 0ms 한 번), `NextItemMs`·`FireItems`는 `Cycles`만, `RollCritical`의 `CritTakenPercent(target)` → `StatRules.CritChance(…, takenPercent)`, `BattleItemState.Cycles`.
- 시뮬: 원정 보고의 방어구 줄(`Simulations.cs`). 정책 그대로.
- 화면: 문구 `Item.BattleStart`·`Item.CritTaken`, `UiText`의 사실·효과 줄, `BattleItemView`(돌지 않는 아이템은 늘 밝음), `BattlePresenter.PlayOpeningArmour`(+ `BattleScreen`이 0ms에 열린 전투에서만 부름).
- Test: EditMode `ArmorTests`(8), `StaticDataTransformerTests`·`ShippedDataTests` 하나씩; PlayMode `Armour_TheCardSaysWhenItActivates_AndTheBattleOpensWithItsShield`, 스크린샷 `TheArmour_Korean`(`ko_79`~`ko_81`).
- 문서: Architecture/05·08("방어구")·09(범위·순서 7-1·8·9·Acceptance 5-4·저장 23)·12("방어구"), CLAUDE.md §9(32~40), Roadmap.
- 그림: Round 87 목업과 스크립트, 아이콘 후보·리뷰 시트, `Rosters/item.csv`의 두 줄, 확정 아이콘 둘(`Assets/@Art/Item/iron_armor.png`·`iron_helm.png`, `Icon` 열).

## Open
- 시뮬의 읽음(08 §46): 광산 원정 94.0 → 88.8% — 정책이 방어구를 뒷열에 넣어서. 정책을 "1열에만"으로 바꿀지와 방어구의 값은 40단계(맞추기)의 입력.
- 목업과 다른 것: 전투 카드의 자리는 지금 규칙대로 보드의 왼쪽(목업은 오른쪽).

## Verification
- `dotnet build Tools/Sim` OK, `validate` OK. 시뮬: HEAD 복사본(scratchpad)의 전과 비교, 방어구를 내지 않은 복사본은 전과 같은 수.
- 체인: 엔진까지 `20261019-181346` CHAIN OK(EditMode 1028/1028, PlayMode 95/106), 화면 뒤 `20261019-204833` CHAIN OK(EditMode 1028/1028, PlayMode 96/108 — 건너뜀 12는 그래픽 스크린샷 Test).
- 스크린샷 `20261019-armour39`(12/12): `ko_79`~`ko_81`을 Round 87 목업과 대조 — 요소가 같다. 아이콘 배선 뒤 체인 `20261019-225648` CHAIN OK(EditMode 1028/1028, PlayMode 96/108), 스크린샷 `20261019-armour39c`(12/12): 아이콘 둘이 보인다(`impl-*.png`).

## Next Action (제안)
- 40단계(맞추기와 직접 플레이)는 시작 지시를 받아서. 다음 플레이에서 상점 물건·보드 아이템을 든 채 우클릭이 돌리는지(자동 Test는 마우스를 흉내 내지 않음), 방어구의 카드·0초 발동을 본다.
