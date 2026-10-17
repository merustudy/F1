# Slice C 30단계: 건물의 단계와 건설 — 먼 시점의 마을, 레벨마다 자라는 건물, 건설 중

Snapshot: 2026-10-18 Slice C 완료로 completed에 (2026-10-17에 Round 78 판정 "진행"으로 30단계를 구현하고 Round 79를 더했다. 10-18에 사용자 플레이 피드백 넷을 "검토 후 네 권장안 구현"으로 구현하고 성문은 배경의 돌 아치로 했다. 모두 커밋·푸시됨. 체인은 아래 Verification. Slice C에 더 구현할 것은 없다: 남은 것은 사용자의 직접 플레이와 완료 선언)

## Goal

Design/04 §7 "건물의 단계와 건설"이 참이다: 로비의 건물 넷이 레벨 1~3의 그림으로 먼 시점의 터 위에 서고, 업그레이드가 금을 내고 공사 일수 뒤에 레벨을 올리며, 공사 중인 건물은 잠기고, 레벨의 효과가 데이터대로이며, 저장(16)이 이어지고, 시뮬 `run`이 건설 정책으로도 띠 안이다(Architecture/09 Acceptance 9). 그 뒤 29단계의 직접 플레이(사용자).

## State

- 2026-10-17 사용자 "로비 배경을 더 멀리서 보는 느낌으로 / 건물은 개별 이미지로 가서 건물 레벨업시 점차 커지는 느낌(클래시오브클랜) / 업그레이드시 건설 중 느낌으로 소요 일수 / 이미지 api를 통해 필요 이미지 생성" → **Round 78**(`ArtPipeline/Archive/78-town-buildings/`: 검토·설계 제안·목업 다섯·판정할 것 열하나) → 판정 "1. 슬라이스 c / 2. 뒤 플레이 / 3. 넷 / 4. 3 / 5. 권당(권장) / 6. 권장안 / 7. 잠든다(잠근다) / 8. 제한 없음 / 9. 목업 / 10. 비계 그림 / 11. 한번에 / 진행" → Roadmap "30단계의 승인".
- 권장안에 없던 **성문의 효과**는 정산의 금 +10/+20%로 정해 적었다(사후 검토).

## Done

- 기획: Design/04 §5(건설비)·§7(【확정】 건물의 단계와 건설, 권장안 세부), 13 §6, 00(현황·Slice C 표 15), 08 §34(시뮬 전후).
- 데이터: `BuildingData.csv`(넷 × 3), `BuildingData.cs`·`BuildingId`, `StaticData.Buildings`·`MaxBuildingLevel`·`BuildingAt`(사다리 검사), `BuildingMapper`, 파일 목록·로더·변환기, `TestData.Buildings`·`TestCsv.Buildings`(두 레벨, 40·3일, 쉼터 2일).
- 규칙: `BuildingRules`(`LevelOf`·`ConstructionOf`·`DaysLeft`·`IsLocked`·`BonusOf`·`NextLevel`·`RosterLimit`·`RecruitCount`·`TreatmentRelief`·`GoldBonusPercent`·`WithGateBonus`·`CanUpgrade`·`Upgrade`·`Complete`, `UpgradeCheck`), `RunState.BuildingLevels`·`Constructions`·`ConstructionState`, `RunRules`(`DrawRecruits`·`CanHire`·`CanDismiss(data, …)`·`Dismiss(data, …)`·`CanTreat`·`Treat`·`CanDepart`의 잠금과 효과, `Rest`가 끝난 건물을 돌려줌, `PassDays`가 `Complete`를 부름, `Settle`의 `GateBonusPercent`·`BuildingsCompleted`), `HireCheck.CoachLocked`·`TreatCheck.InnLocked`·`DepartCheck.GateLocked`.
- 저장 16: `RunRecord.Buildings`·`Constructions`, `RunSaveMapper`(쓰기·읽기·검증: 건물 먼저 읽어 상한·후보 수를 효과로 검증), `From15To16`.
- Manager: `RunManager.BuildingLevel`·`ConstructionOf`·`IsBuildingLocked`·`CanUpgrade`·`Upgrade`, `Rest`의 반환, `Dismiss`의 인자.
- 시뮬: `Simulations.Run(…, build)`(가장 싼 다음 레벨부터, 성문 공사 중엔 쉬기, 보고 줄 "buildings (stage 30)"), `Program` `--build 0|1`.
- 그림: `STYLE_RUNTIME.md` §29 Building·§30 Reference Rule·§31 Forbidden과 타입 표, `gen_image.py`의 `building`(`fit=trim`, `fit_trim`), `Rosters/building.csv`(13), `Archive/78-town-buildings/STYLE_RUNTIME-town-far.md`·`town-far.csv`·`generate.sh`·`generate.log`·`review-buildings.png`·`approved/`. 생성 14회(약 $0.49, 누계 302회 약 $8.18). 배선 `Assets/@Art/Building/*.png`(13), `Background/Lobby/town.png`(먼 터로 교체), `ArtAddress.Building`·`Scaffold`, `ArtSetup`의 Building 정책과 Entry(Lobby Scope).
- 화면: `BuildingView`(`Place`·`SetScaffold`·`ShowConstruction`·`HideConstruction`, 비계·배지), `BuildingLineView`, `UiPrefabSetup.Lobby`(`BuildBuilding`·`BuildBuildingLine`·`ListScroll`, 성문 창, 역마차 창 980x760), `LobbyScreen`(`Plots`·`LevelScale`·`Quarter.Gate`, `PrepareAsync`의 그림, `RefreshTown`·`RefreshLine`·`Effect`·`RefreshGate`·`GateWords`, 말풍선, `Upgrade`), `ExpeditionScreen`의 `GateLocked` 상태, `UiKeys.Lobby` 열다섯과 `UI_StaticText.csv`.
- Test: EditMode `BuildingRulesTests`(9), `RunSaveMapperTests`(깨진 저장 열하나, `Migrate_From15_…`, `Save_KeepsTheBuildingsLevels_…`), `StaticDataTransformerTests`(사다리 거부 여섯, 읽기), `ShippedDataTests.ShippedBuildings_…`, `LobbyEconomyTests`의 인자. PlayMode `Lobby_TheTown_UpgradesABuilding_ForGoldAndDays_LocksItWhileItIsBuilt_AndGrowsItWhenTheDaysPass`, 마을 Test(레벨 이름표·그림·성문 창), 성문 클릭 둘을 띠의 단추로, 스크린샷 `_67_lobby_construction`(막사 공사 중, 랩 끝에 2레벨).
- 문서: Architecture/04·05·07·08·09·11·12·13, CLAUDE.md §9(25~30), Roadmap(갱신 줄, 진행 표, 30단계의 승인, 사후 검토, 진행 순서, 30단계), README 78.

## Done (2026-10-18 플레이 피드백, 사용자 "위 사안 검토 후 네 권장안 구현")

- 상태 복구: 10-17 플레이 기록 셋(09:03 20층에서 끝남 약 10분, **16:48 광산 31층 클리어 11.6분**(전투 18회 7.1분·회당 24초, 노드 맵 3.9분, 1일 → 6일 정산), 17:08 이어하기). 두 부팅의 `run=Corrupt`는 옛 저장(12 이전)과 데이터가 바뀐 전투 중 저장(09:14, 20층)으로 추정(파일은 덮여 확인 불가).
- (1) **상점 판매 1/2**: `BalanceData.csv` `SellPercent` 50 → **25**(Generated). `ShippedDataTests`(숫돌 2, 녹슨 칼 1, 쥐 이빨 1). Design/03 §5. 클리어의 코인 → 금(§31)의 아이템 몫도 반.
- (2) **인벤토리에서도 합치기**: `ExpeditionRules`의 `MergesInInventoryAt`·`LootMergesInInventoryAt`·`ShopMergesInInventoryAt`·`InventoryItemAt`, `CanMoveInInventory`·`MoveInInventory`·`CanMoveToInventoryAt`·`MoveToInventoryAt`·`CanTakeLootToInventoryAt`·`TakeLootToInventoryAt`·`CanBuyToInventoryAt`·`BuyToInventoryAt`의 합치기 가지, `HasMergeTarget`이 인벤토리도 봄; Manager의 질의 셋; `IOutsideHand.MergesInInventoryAt`(`DropInHand`·`ShopHand`); `BoardHand.InventoryGhost`의 `Merges`와 글; `InventoryGridView`의 합치기 표시(`merges`)와 글 상자(`_ghostLabelPlate`·`_ghostLabel`, `GhostLabel`), `InventoryWindowView.Show`의 합치기 판정, 빌더 `BuildInventoryGrid`의 `InventoryGhostLabel`. Test: `TierRulesTests.TheInventory_MergesToo_…`, `ShopRulesTests.Buying_OntoTheSameItemInTheInventory_MergesIt`, PlayMode `PartySide_AHeldItem_OverTheSameItemInTheInventory_SaysMerge_AndMergesThere`. Design/02 §4·03 §5, Architecture/08·11·12.
- (3) **성문은 업그레이드 없음**: `BuildingData.csv`의 `gate` 셋 삭제, `BuildingId.All` = 셋(`Gate` 상수 삭제), `BuildingRules`의 `GoldBonusPercent`·`WithGateBonus`, `RunRules`의 `GateLocked`·정산 보너스, `DepartCheck.GateLocked`, `SettlementReport.GateBonusPercent` 삭제; 저장 **17**(`From16To17`: 성문의 레벨·공사를 버림); 시뮬 정책(역마차 → 막사 → 쉼터, 성문의 쉬기 없음); 성문 그림 셋 삭제(`gate_1`~`gate_3`, `Rosters/building.csv`의 줄; 처음엔 `gate_1`을 `gate.png`로 남겼으나 사용자 "새로 추가된 성문 이미지는 지우고 이전 배경 성문 이용"으로 그것도 지움), `LobbyScreen`의 `GatePlot`(1780, 505, 폭 280)·`GateHeight` 170의 투명한 핫스팟(`BuildingView.Place(null, …)`: 그림 없음, 빛 없음)·누르면 `OnDepart`·`Quarter.Gate`와 성문 창의 필드·`RefreshGate`·`EffectGate` 삭제; 빌더의 성문 창 삭제; `ExpeditionScreen`의 `GateLocked` 상태 삭제; 문구 넷 삭제(`Lobby.EffectGate`·`GateTitle`·`GateNote`·`GateLocked`). Test: `BuildingRulesTests`(성문 Test 삭제, 기한 뒤는 역마차, 효과 셋), `RunSaveMapperTests`(`Migrate_From16_DropsTheGate_…`, 17의 `GateAsABuilding`은 깨진 저장, 역마차로), `ShippedDataTests`·`StaticDataTransformerTests`·`ArtSetupTests`·`TestData`·`TestCsv`, PlayMode 마을 Test 둘(성문 "원정", 창 없음, 바로 원정 화면, 지어지지 않음). Design/00·04 §5·§7·08 §35, Architecture/05·07·08·09·11·12·13, README 78.
- (4) **든 장비의 왼쪽/오른쪽 클릭**: `TurnInput`의 우클릭은 돌리기 → 내려놓기(`cancel`), `PartySideView.LetGo`·`BattleScreen.LetGo`(Esc·우클릭·Test), `OnHeldPressed`의 그 밖: 보드의 아이템은 `MoveToInventory`(첫 빈 자리, 넣는 소리; 자리 없으면 제자리), 나머지는 제자리. 문구 `Board.Hint`·`Board.TurnHint`·`Map.ShopBuyHint`·`Battle.LootPickedHint`. Test: PlayMode `PartySide_AHeldItem_…_TheRightButtonSendsItBack_AndAPressElsewhereSendsItToTheInventory`. Design/03 §5, Architecture/12.
- 시뮬(08 §35, 같은 시드 300런): 균형형 74.7 → **79.7%**(성문 공사의 쉬기 7.2 → 1.3일, 끝난 날 46 → 40), 안전형 71.0 → 71.3%. 모두 띠 안(균형형은 위 끝).
- 목업(`ArtPipeline/Archive/80-play-feedback/`): 성문 그림의 선택(A 목책 / B 돌 문루 / C 쌍탑, 권장 C) → 사용자 **"새로 추가된 성문 이미지는 지우고 이전 배경 성문 이용"**: 그림 없이 터의 돌 아치가 성문.

## Open

- **Round 79 판정 대기**(2026-10-17 사용자 "드랍된 추가 인벤토리(가방)의 이미지를 좀 다르게 주고 싶어. 개선안 검토 후 보고"): `ArtPipeline/Archive/79-bag-picture/README.md` — 가방이 물건일 때(바닥·상점·손) 우물 대신 그림을 권장. 둘째 지시("백팩배틀즈 등 검토, 이미지 api로 목업")로 유사 게임을 보고 세 안(닫힌 가방 / 위에서 본 열린 가방·칸 / 펼친 가방·주머니)의 그림을 생성(7회 약 $0.16, `gen_image.py`의 `bag` 타입)해 `mock-bag-looks.png`에 얹었다. 권장 안 2 → 셋째 지시("안 2로, 칸을 네모 칸에 맞춰 / 칸만 늘어남과 그림이 뒷배경의 목업")로 칸 맞춤 소재(`look2b.csv`)로 셋을 다시 생성(3회)하고 보드의 두 방식을 `mock-bag-board.png`에. → 판정 **(a)**(보드는 우물, 물건일 때는 안 2의 그림을 칸에 맞춰): 구현함 — `bag` 타입의 캔버스를 가방 조각의 비율로(`--refit`), `BagData.Icon`, `ExpeditionArt.OfBag`, 바닥·상점·손의 그림, 이름판 "가죽 주머니  3×1", Test와 스크린샷 `_68_loot_bag`, Design/03 §5·Architecture/05·12·13. 라운드 호출 10회 약 $0.23. 넷째 지시(상점 타일의 그림 크기 → "ㄱ 여백 0, 바닥·손도, 값은 작은 판")도 반영(`ItemSlotView`·`LootDropView`·`HeldPointerView`의 꽉 찬 그림, `MerchantPriceTemplate`의 판). "가방은 설치하면 못 옮김"은 검토 보고 → 사용자 "권고안 구현" → **(1) 완전 고정을 구현**(규칙·Manager·손·포인터·Prefab·문구·Test·스크린샷 장면. README 79 "구현 (1)").
- 사용자: 직접 플레이(29단계 Acceptance 7, 이제 건물까지) → 피드백 조정 → Slice C 완료 선언.
- 사후 검토(Roadmap Round 78 줄): 성문의 효과와 크기, 잠금의 범위(쉬기는 됨), 성문의 창, 시뮬 정책(성문 먼저), 그림 13장의 판정(리뷰 시트), 터의 자리와 배율.
- 완성의 알림(정산 화면에 "막사 Lv 2 완성" 줄)은 두지 않았다(Deferred, Architecture/12).

## Verification

- `transform`·`validate` OK(`Tools/Sim`). 시뮬 08 §34.
- 체인 setup `20261017-182912` OK(그 전 `20261017-182558`은 창마다의 건물 줄 이름이 겹쳐 실패 → 접두사).
- 체인 `20261017-183001`(sim OK, EditMode 949/950 — `ArtSetupTests`가 건물의 Lobby Scope를 몰랐다, PlayMode 88/101 — 도우미 `OpenQuarter`가 성문의 창을 몰랐다) → 둘을 고쳐 `20261017-184713`(**CHAIN OK**: EditMode **950/950**, PlayMode **90/101** — 건너뜀 11은 그래픽 스크린샷 Test).
- 그래픽 스크린샷 `~/Library/Caches/F1/screenshots/20261017-185000-buildings`(11/11, PNG 117: `_67_lobby_construction` 새로). 스크린샷 실행이 URP·ProjectSettings 파일을 건드려 `git checkout`으로 되돌렸다.
- 목업(`mock-A-far-lv1`·`mock-A-construction`·`mock-A-window`)과 대조: 먼 시점의 터 위에 건물 넷이 제 터에 섬(Lv 1은 작게) ✓, 이름표 "막사  Lv 1" ✓, 공사 중의 비계(그림 위)·배지("건설 중 · 3일 남음"과 막대)·말풍선 둘째 줄 ✓, 이름표의 흐림 ✓, 창의 건물 줄("Lv 1 · Lv 1 → Lv 2: 후보 3명 → 4명 · 금 40 · 3일 · [업그레이드 금 40 · 3일]") ✓, 공사 중의 창("Lv 1 → Lv 2 건설 중 · 3일 남음", 붉은 "건설 중이라 쓸 수 없다", 내보내기 꺼짐) ✓, `_12_lobby_after`에서 막사가 **Lv 2**로 커짐(로스터 2/10) ✓. 다른 점: 터 그림의 오른쪽 위에 그려진 돌 아치가 1레벨 성문 옆으로 보인다(3레벨은 거의 덮는다 — 사후 검토: 터를 다시 그리거나 성문의 자리를 오른쪽으로), 목업의 흐린 둘레 대신 진짜 그림은 끝까지 그려져 있다.

- 2026-10-17 Round 79(가방의 그림): 체인 `20261017-195344`(**CHAIN OK**: setup·sim OK, EditMode **951/951**, PlayMode **90/101** — 건너뜀 11은 그래픽 스크린샷 Test), 그래픽 스크린샷 `~/Library/Caches/F1/screenshots/20261017-204500-bags`(11/11, PNG 119: `_68_loot_bag` 새로 — 바닥의 가죽 주머니가 그림으로 눕고 빛 위에, 이름판 "가죽 주머니  3×1"). 그 전의 두 번(`203000`·`201500`)은 장면 꾸미기의 실수(드랍 자리는 이긴 순간에 만들어지고, 마지막 것을 주우면 목록이 빈다)로 가방이 안 보이거나 Test가 깨져 첫 드랍 자리에 잠깐 세우는 것으로 고쳤다. 상점의 가방 타일은 고정 시드의 재고에 가방이 없어, 장면 `_69_shop_bags`가 재고의 마지막 둘을 가방으로 바꿔(자리는 새로고침 횟수로 기억되므로 그 수를 잠깐 올려 새로 깖) 찍는다: `~/Library/Caches/F1/screenshots/20261017-211500-shop`(11/11, PNG 121) — 8행에 가죽 주머니(12)·허리 주머니(8)가 가죽 색 조각 위의 그림으로. 목업(`mock-bag-board.png` 오른쪽 "바닥의 전리품")과 대조: 그림·눕힘·이름판의 칸 수 모두 같음. 스크린샷 실행이 건드린 URP·ProjectSettings는 `git checkout`으로 되돌렸고, `AddressableAssetSettings.asset`의 해시 변경은 setup이 Entry(건물·가방)를 더한 것이라 둔다.

- 2026-10-17 Round 79 넷째 지시(꽉 찬 그림, 값의 판): 체인 `20261017-205545`(**CHAIN OK**: setup OK, EditMode **951/951**, PlayMode **90/101**; 그 전 `205524`는 `MerchantGridView`의 `using UnityEngine.UI` 누락으로 컴파일 실패 → 고침), 그래픽 스크린샷 `~/Library/Caches/F1/screenshots/20261017-213000-bags`(11/11, PNG 121): `_69_shop_bags`의 가방 둘이 조각에 꽉 차고 모든 값이 작은 어두운 판 위에, `_68_loot_bag`의 바닥 가방이 칸 크기로 커짐. 목업 `mock-shop-bag-size.png` 안 ㄱ과 대조: 같음(값의 판은 목업에 없던 판정 3의 것).

- 2026-10-17 Round 79 가방 고정(완전 고정): 체인 `20261017-220815`(**CHAIN OK**: setup·sim OK, EditMode **949/949** — 옮기기 Test 셋이 하나로, PlayMode **90/101**; 그 전 셋(`220653`·`220723`·`220752`)은 정리에서 남은 중괄호 둘과 새 Test의 인자 누락으로 컴파일 실패 → 고침), 그래픽 스크린샷 `~/Library/Caches/F1/screenshots/20261017-224500-fixed`(11/11, PNG 119: `_45_map_bag_held` → `_45_map_bag_placed`, 놓인 주머니 둘의 보드). 안내문 `Board.Hint`에서 가방 집기 문장을 뺐다.

## Verification (2026-10-18 플레이 피드백)

- `transform`·`validate` OK(`BalanceData.json`·`BuildingData.json` 다시 생성). 시뮬 08 §35.
- 체인 `20261018-074315`(전체: setup·sim OK, EditMode 950/952 — `StaticDataTransformerTests`의 `gate` 줄 둘, PlayMode 90/102 — 마을 Test의 내 단정 하나(빈 파티라 `PartyTooSmall`)) → 고쳐 `20261018-080059`(EditMode 951/952 — 단정의 예외 타입, PlayMode **91/102**, 건너뜀 11은 그래픽 스크린샷 Test) → `20261018-081843`(**EditMode 952/952, CHAIN OK**).
- 그래픽 스크린샷 `~/Library/Caches/F1/screenshots/20261018-081854`(11/11, PNG 119): `_02_lobby_empty`의 성문 이름표가 "원정"(레벨 없음), 나머지 셋은 "Lv 1". 스크린샷 실행이 건드린 URP·ProjectSettings는 `git checkout`으로 되돌렸다.
- 성문 그림 삭제(사용자 "이전 배경 성문 이용"): 체인 `20261018-082938`(**CHAIN OK**: setup·sim OK, EditMode **952/952**, PlayMode **91/102**), 그래픽 스크린샷 `~/Library/Caches/F1/screenshots/20261018-084651`(11/11, PNG 119): `_02_lobby_empty`에서 성문 자리에 터 그림의 돌 아치만 보이고 이름표 "원정"이 그 밑 화면 안에 선다. URP·ProjectSettings는 되돌렸다.
- 이 변경에서 새로 든 Test: EditMode 셋(인벤토리 합치기 둘, 저장 16 → 17), PlayMode 하나(인벤토리의 합치기)와 고친 하나(우클릭 복귀·빈 곳 클릭의 인벤토리행).

## Next Action (제안)

- 세션: 2026-10-18 세션 정리로 커밋·푸시했다(30단계와 Round 79까지). 2026-10-18 세션 정리로 커밋·푸시했다(플레이 피드백 넷과 배경의 성문까지). 다음 세션은 `CLAUDE.md` → `Docs/Roadmap.md` → 이 Handoff(와 `c29-tuning.md`)를 읽고, 새 플레이 기록이 있으면 읽어 피드백 조정 → Slice C 마감. 열린 결정은 없다(Round 80의 성문 그림은 사용자가 "배경의 성문"으로 정했다). 사후 검토 대상은 Roadmap "사후 검토 대기"의 Round 78·79 줄(성문의 효과, 잠금의 범위, 성문의 창, 시뮬 정책, 그림의 판정, 값의 판, 가방 고정)과 터 그림의 돌 아치.
