# Slice B 18단계: 몬스터의 전리품

Snapshot: 2026-10-07

## Goal

Architecture/09 "Slice B" Acceptance 10이 참이다: 보스가 아닌 전투에서 이기면 보상 선택 대신 쓰러뜨린 적이 들었던 아이템이 떨어지고, 하나씩 보드나 인벤토리로 줍거나 두고 간다.
같은 시드와 같은 길이면 같은 전리품이다. 전리품을 줍다 끝내도 이어진다. 적의 아이템은 용병이 들 수 있는 물건이다. 시뮬이 전리품을 보고하고 수치가 띠 안에 있다. 체인이 통과한다.

## State

- 2026-10-07 사용자가 16·17단계를 이어서 하면서 제안했다("몬스터 아이템도 우리 장비 및 아이템 스타일로 분류. 전투 후 보상 화면 대신 그들의 아이템 중 일부 드랍(캐릭터 장착 가능), 지역 코인 일부 드랍. 유사 유명 게임 검토 후 변경 검토 및 권장안대로 구현").
  **Round 45**(`ArtPipeline/Archive/45-monster-loot/README.md`: 레퍼런스 일곱, 안, 권장안, 판정할 것 다섯)를 쓰고 "검토 및 권장안대로 구현"의 지시대로 권장안으로 구현했다(사용자는 세션에 없었다: Roadmap "18단계의 승인"은 사후 검토 대상).
- 구현이 끝났고 시뮬로 출발값을 정했다(Design/08 §14). 체인은 아래 Verification.
- 2026-10-08 세션 정리: 주제별 다섯 커밋(Architecture와 기획 / 규칙과 데이터·저장·Application·시뮬과 EditMode Test / 화면과 문구·Prefab·PlayMode Test / 그림: Round 45의 기록 / Roadmap과 Handoff)을 `origin/feature/slice-a`에 푸시했다.
- 16단계의 맞추기 수치를 이 데이터로 다시 잡았다(처치 `FatigueOnKill` 5 → 3: `b16-tuning.md`). 플레이 기록은 아직 없다.

## Done

- 기획: Design/02 §4(적 아이템의 이름·척도, 획득은 전리품과 상점)·§10(포션은 상점에서만), 03 §1(정예)·§5(전리품의 규칙), 07 §4(열 이름), 09 §5(전리품 스트림), 00 "Slice B" 표와 권장안 세부, 08 §14.
- 데이터: `ItemData.csv`(쥐 송곳니·치유의 부적, 계수 송곳니 60·녹슨 칼 80·등불 25, `RewardWeight` → `ShopWeight`), `EnemyData.csv`(등급 쥐 17·약탈자 19·주술사 12·고참 28·주술 대가 18: 전투의 수치는 같다), `PotionData.csv`(`ShopWeight`),
  `BalanceData.csv`(`DropCount` 1, `EliteDropCount` 2, `RewardChoices` 삭제, `FatigueOnKill` 3), Generated 넷. Mapper와 `BalanceData.MaxLootCards`(3), `DungeonData.ItemGradeAt`·`ItemTierAt`(`Reward*`에서 이름을 바꿈).
- Domain: `ExpeditionPhase.PickingLoot`(`ChoosingReward`에서), `ItemOffer`·`OfferKind`(`RewardOption`·`RewardKind`에서: 전리품과 상점의 물건), `ExpeditionState.Loot`(주운 자리는 null), `ExpeditionRules.DropLoot`(적들의 아이템에서 전리품 스트림으로 뽑음, 층의 단계·던전의 등급), `DropAt`, `CanTakeLoot`·`TakeLoot`, `CanTakeLootToInventory`·`TakeLootToInventory`, `LeaveLoot`, `LootMergesAt`, `Pick`·`EndLoot`·`RequireDrop`, `DrawOffers`(상점만), `FloorsBelowTheFirst`, `RngStream.Loot`.
- 저장: `RunSaveData` 9(`Loot`·`OfferRecord`), `RunSaveMigrator.From8To9`(보상 선택 중이던 파일은 노드 고르기로, 전리품은 비움; 4→5·6→7의 보상 처리는 뺌), `RunSaveMapper.ReadLoot`(단계·카드 수·아이템만·하나는 놓여 있음). Architecture/07.
- Application: `GamePhase.Loot`, `ExpeditionManager`의 `CanTakeLoot`·`TakeLoot`·`CanTakeLootToInventory`·`TakeLootToInventory`·`LeaveLoot`·`LootMergesAt`·`BattleLoot`(포션 보상 명령은 뺌). Architecture/11.
- 시뮬: `SimPolicy.PickLoot`(합쳐지는 것·자리 맞는 칸·인벤토리의 차례, 나머지는 둠, 그 뒤 Tidy), `Simulations`의 드랍 줄(떨어진 수, 주운 수). 보고 Design/08 §14.
- 화면: `LootScreen`(보상 화면을 고쳐 씀: 카드마다 줍기, 주운 카드는 남음, 마지막 것을 주우면 지도, 두고 가기), `LootCardView.ShowTaken`·`IsTaken`, `UiPrefabSetup.Loot.cs`(`LootScreen`, `Cards`/`CardTemplate`, `LootHint`, `LootToInventory`, `Leave`), `ScreenId.Loot`(`ui/expedition/loot-screen`),
  문구 `Loot.Title`·`Hint`·`Item`·`Take`·`Selected`·`Taken`·`Leave`·`ToInventory`, `Battle.Loot`, `Map.ShopPotion`(`Reward.*` 삭제), `BattleScreen.ShowResult`의 전리품 줄, `NodeMapScreen`의 라벨 키. 옛 `RewardScreen.prefab`은 지웠다(setup이 `LootScreen.prefab`을 만들고 Addressables의 묵은 항목을 지운다). Architecture/12·14.
- Test: EditMode `ExpeditionRulesTests`(전리품 아홉), `TierRulesTests`, `FatigueRulesTests`, `ExpeditionManagerTests`(둘), `RunSaveTests`, `RunSaveMapperTests`(거부 사례, `Migrate_From8…`, `Save_Refuses…`, `Save_KeepsTheTierOfEveryItemAndDrop`), `StaticDataValidationTests`, `DefinitionTests`, `TestData`·`TestCsv`;
  PlayMode `UiTestUtil`, `UiFlowTests`(루프, 인벤토리 팝업, `Loot_ACardShowsItsTier_…`, 새 `Loot_TakingOneOfTwoDrops_KeepsTheScreen_MarksTheCardTaken_AndLeavingGoesOn`: 둘째 드랍을 꾸며 "주움" 카드와 두고 가기), `UiScreenshotTests`(`_07_loot`, 드랍마다), `GameFlowTests`.
- 문서: CLAUDE.md §9, Architecture/02·04·05·07·08·09·11·12·14, Roadmap("18단계의 승인", 18단계 절, 16·17단계 절, 사후 검토 대기, 머리, 날짜 표, 진행 순서. 맨 위에 잘못 들어가 있던 사후 검토 행 셋을 표로 옮겼다), README 45, 이 Handoff와 b16·b17.

## Open

- 사후 검토(Roadmap "18단계의 권장안 세부"): 적 아이템의 척도(계수와 등급을 함께 바꿈), 드랍 하나(둘이면 96%), 처치 3, 보스전 92.7%(띠의 위 끝), 합치기가 0.14/원정으로 준 것, 글 카드(아이콘 카드는 목업 라운드), 포효의 이름.
- 유니크·아이콘 카드·몬스터 전용 전리품(보스의 것)은 Slice C 후보.
- 직접 플레이(x1, 원정 하나)는 `b16-tuning.md`의 체크리스트(전리품 항목을 더함)로.

## Verification

- `dotnet build Tools/Sim` OK. `transform`으로 Generated 넷을 다시 만들었다.
- 시뮬(시드 1): Design/08 §14. 세 자리(적의 등급 그대로 100% → 층의 단계 81%/드랍 하나 → 우리 척도 66.8%·붕괴 10%) → 레버 아홉(1,000회) → 처치 3 채택: balanced **66.1% / 0.30, 붕괴 42.5%**, safe 59.5% / 0.28, none 80.7% / 1.28, 여섯 파티 70.0 / 60.9 / 48.3 / 41.5 / 6.9 / 0.0%.
- 체인 1(`setup sim editmode`): setup OK(Prefab 셋과 Stamp 다시, 문구 표, 새 .cs의 .meta, `RewardScreen.prefab` 없음), sim OK, EditMode 756 중 3 실패 — 모두 Test의 오류(저장 Test의 옛 `Migrate_From4…`가 치환의 끝 앵커 오류로 남아 있었고, 상점 단계의 거부 사례 둘은 `ValidSave`가 만들지 않는 단계였다) → 고침.
- 체인 2(`20261007-223237`, `editmode playmode`): EditMode **753/753**, PlayMode 85 중 1 실패 — `UiFlowTests`의 단계 카드 Test가 옛 이름·옛 자식 이름(`OptionTitle`)이었다(첫 Test 스크립트가 중간에 멈춰 PlayMode 세 곳의 치환이 빠짐) → 세 곳을 고침.
- 체인 3(`20261007-224414`, `playmode`): **PlayMode 76/85**(9 skipped: 스크린샷의 Explicit), **CHAIN OK**. `Assets/InitTestScene*` 없음.
- 스크린샷 `20261007-225510`: 9/9, PNG 71(`_07_reward` 둘이 `_07_loot` 둘로). Render 설정 그대로. `ArtPipeline/Archive/45-monster-loot/game/`에 전리품 화면(ko·en)과 결과 창.
- 새 PlayMode Test `Loot_TakingOneOfTwoDrops_…`(주움 카드)는 혼자 돌려 **1/1 통과**(`~/Library/Caches/F1/chain/single-*`). 전체 체인은 그 전에 통과했다(다음 체인에서 PlayMode는 77/86이 된다).
- `git diff --check` 깨끗. ProjectSettings·Scene 변경 없음. Addressables 설정 둘은 setup이 `reward-screen` 항목을 `loot-screen`으로 바꾼 것뿐이다.

## Next Action (제안)

- 사용자: 직접 플레이(x1, 원정 하나를 끝까지) → `python3 Tools/playlog.py`의 출력과 체크리스트(`b16-tuning.md`)의 메모를 세션에.
- 세션: 플레이 기록과 메모가 오면 16단계의 맞추기와 마감(17·18단계의 수치도 함께). 사후 검토의 답(척도, 드랍 수, 처치 3, 보스전, 글 카드)이 오면 그것부터.
