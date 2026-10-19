# Slice D 중의 플레이 피드백 (2026-10-19)

Snapshot: 2026-10-19 (완료: 다섯 구현 — 드랍 단계·시뮬, Round 85 전투 별 표시, Round 86 적의 시작 가방, 초반 상점, 기본 무기의 합치기·유니크 피로. 2026-10-19 세션 정리에서 커밋·푸시)

## Goal
사용자 플레이 피드백 다섯을 검토 보고의 권장안대로 반영한다: (1) 전투 중 별 아이템(숫돌·글라디우스)에 마우스를 올리면 지금 별을 보드에 그림 — 목업(Round 85) 승인 뒤, (2) 몬스터 드랍의 높은 단계를 줄임 — 드랍마다 단계 굴림, (3) 적의 보드를 시작 가방(3×3)으로, 아이템이 많으면 자동 확대 — 목업(Round 86) 뒤, (4) 원정 초반(3~5층)의 상점 — 상점 층과 시작 코인, (5) 기본 무기의 합치기와 기본 무기·유니크의 피로 없음.

## State
- (5) 2026-10-19 사용자 "기본무기 끼리 합쳤을 때 성급 올라가도록 / 기본 무기를 베이스로 합성되는 +성급 무기 및 유니크 아이템은 피로도 소모 없게" → "검토 후 보고" → "권장안 구현" → 구현.
- (4) 2026-10-19 사용자 "원정시 초기 3-5 노드 안에 상점은 한개 노드 이상 등장하는 방안 검토" → 검토(08 §43) → "권장안 반영" → 구현.
- (3) 2026-10-19 사용자 "적 몬스터들도 인벤토리 기본 공간(3*3)으로 시작 (보스 몹 등 아이템 많은 경우 자동 확대)" → 검토·Round 86 목업 → "권장안 구현" → 구현.
- (2) 구현·시뮬·문서 끝. 체인은 아래 Verification.
- (1) Round 85 목업(`ArtPipeline/Archive/85-battle-stars/`) → 사용자 "권장안 구현"(셋 모두 권장안) → 구현.
- 38단계(얻은 특성)는 설계 보고 뒤 시작 지시 대기(변하지 않음).

## Done
- 규칙: Design/03 §5 【확정】(2026-10-19) — 일반 위의 드랍은 아이템마다 `DropTierKeepPercent`%로 그 단계, 아니면 한 단계 아래. 정예 +1·드랍 수·가방·상점 그대로.
- 데이터: `BalanceData.csv` `DropTierKeepPercent` 50, Generated 다시 만듦. `BalanceData.cs` KeySpec(0~100)·속성.
- Domain: `ExpeditionRules.DropLoot`·`DropTier`(따로 파생한 `Derive(원정 시드, "loot-tier", 노드 id)`의 Loot 스트림: 무엇이 떨어지는지 그대로).
- Test: `ExpeditionRulesTests.DropLoot_AnItemAboveCommon_KeepsTheTierWithTheChance_ElseDropsATierLower`, `TestData`는 100(옛 규칙: 기존 Test 그대로), `TestCsv`에 키.
- 문서: Design/03 §5·07·08 §42, Architecture/08 "전리품", Roadmap(날짜 표·요약).
- (1) 화면: 새 `PointerHover`(포인터 오름·내림, Test의 Simulate), `BattleItemView.Hover`, `BattleBoardView`의 별 층(`_starLayer`·`_starTemplate`, 조각을 놓은 뒤 맨 뒤로)·`ShowStars`·`Stars`·`StarAt`·`ItemPointed`/`ItemLeft`, `BattleScreen.BattleStars`(원정 보드의 `StarRules.Marks`, 아군의 별 아이템만), 빌더 `UiPrefabSetup.Battle`·`Grid`. Test: PlayMode `Battle_AWhetstonesStars_ShowWhilePointedAt_AsOnTheNodeMap`, 스크린샷 `ko_76_battle_star_hover`(ko_55의 무대에 숫돌을 더함). 문서 Architecture/12 "★", Round 85 README(판정됨).

- (3) 적의 시작 가방: `BoardFrame.Pack`(Data: 읽는 차례의 처음 자리, 모자라면 한 줄씩), `BoardLayout.Packed`·`BoardBag.ExtraRows`(Gameplay), `ExpeditionRules.EnemySetup`(기본·페이즈), `StaticData.CheckEnemyBag`(8줄 넘으면 문제 — 옛 "한 줄씩의 높이" 검사를 대신), `BattleBoardView`(적의 가방 테 `EnemyRim`). Test: EditMode `ItemBoardTests.AnEnemysBoard_IsTheStartBag_…`, `StaticDataValidationTests.AnEnemysItems_MustFitTheStartBagGrownWithinTheFrame`, `ExpeditionRulesTests` 두 곳; PlayMode 두 곳(적도 가방 1·빈 칸). 문서 Architecture/08·12, Round 86 README(판정됨). 시뮬 그대로(배치는 엔진이 읽지 않음).

- (4) 초반 상점: `DungeonData.ShopFloor`(선택 열, 0 없음, 2..층·야영지 층 아님)·CSV(광산 4, 심연 0)·`MapGenerator.DrawKind`(그 층은 모두 상점), `StaticData`(상점 층은 적 무리 검사 빼기), `BalanceData.ExpeditionStartCoins` 20·`ExpeditionRules.Create`. Test: EditMode `ShopRulesTests.Map_TheShopFloorIsAllShops_SoEveryPathMeetsOne`·`AnExpedition_StartsWithTheStartCoins`, `TestData` 0·`TestCsv`. 문서 03 §1·§5·07·08 §43, Architecture/08.

- (5) 기본 무기의 합치기: `ExpeditionRules.CanMerge`·`MergesInto`(기본 무기 조건 삭제)·`MergeInto`(둘 중 하나가 기본 무기면 결과도), `FatigueRules.CostsFatigue`(유니크 0), `UiText.AddFatigue`·`UiKeys.Item.UniqueNoFatigue`·문구 "유니크: 피로 없음", 시뮬 정책(보드의 기본 무기를 옮겨 합치지 않음). Test: `TierRulesTests.BaseWeapons_Merge_AndTheMergeIsABaseWeapon_ThatCostsNoFatigue`·`AUniqueItem_CostsNoFatigue_EvenWhenNotABaseWeapon`, 옛 "기본 무기는 합치지 않음" 단언 삭제. 문서 02 §4·03 §5·04 §3·08 §44, Architecture/08·12.

## Open
- 안전형 런 83.9%는 아직 띠(65~80%) 위, 끝난 날 34일은 띠(40~60일) 아래 — 39단계(맞추기)의 입력.
- 원정 끝 보드의 3성은 4.8개로 남음(합치기·정비·상점) — 줄이려면 합치기가 레버(별도 검토, 사용자가 원할 때).

## Verification
- `dotnet run --project Tools/Sim -- validate` OK. 시뮬 전후는 08 §42(검토의 변형은 scratchpad 복사본, 기준 12줄이 저장소와 같음).
- 체인 `20261019-093002` CHAIN OK(EditMode 1004/1004, PlayMode 94/105 — 건너뜀 11은 그래픽 스크린샷 Test).
- (1)까지 넣은 뒤 체인 `20261019-100331` CHAIN OK(EditMode 1004/1004, PlayMode 95/106). 스크린샷 `20261019-stars85`(11/11): `ko_76`을 Round 85 목업과 대조 — 같음(`impl-compare.png`).

- (3)까지 넣은 뒤 체인 `20261019-112344` CHAIN OK(EditMode 1005/1005, PlayMode 95/106). 스크린샷 `20261019-enemybags86`(11/11): `ko_64`·`ko_55`의 적 보드를 Round 86 목업과 대조 — 같음(`impl-compare.png`). 시뮬 `run` 균형형 시드 1 72.3%(그대로).

- (4)까지 넣은 뒤 체인 `20261019-120510` CHAIN OK(EditMode 1007/1007, PlayMode 95/106). 시뮬 전후는 08 §43.

- (5)까지 넣은 뒤 체인 `20261019-143639` CHAIN OK(EditMode 1009/1009, PlayMode 95/106). 시뮬 전후는 08 §44.

## Next Action (제안)
- 38단계(얻은 특성)는 설계 보고(2026-10-19, Roadmap 38단계)대로 시작 지시를 받아서. 39단계의 입력: 안전형 런 86% 안팎(띠 위), 끝난 날 33일(띠 아래), 균형형 시드 3의 전멸 25%대.
