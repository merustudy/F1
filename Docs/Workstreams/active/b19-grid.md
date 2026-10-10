# Slice B 19단계: 격자 보드

Snapshot: 2026-10-10 (Round 50 구현과 정정)

## Goal

Architecture/09 "Slice B" Acceptance 11이 참이다: 용병의 보드가 틀 3×8 안의 가방 칸이고 시작은 가죽 배낭 3×3이다. 아이템은 가로×세로 모양으로 가방의 칸에 놓이고 든 동안 우클릭·휠·R로 돌며,
놓을 자리가 그림자로 보이고 하나와 겹치면 그것이 인벤토리로 간다. 가방으로 칸이 늘고, 발동은 읽는 순서다. 쿨다운은 아이템의 제 색이 왼쪽부터 드러나는 빛이다.
어느 시점에 끝내도 이어지고 9의 저장 파일이 같은 발동 순서로 격자에 놓인다. 시뮬이 띠 안이고 체인이 통과한다.

## State

- 2026-10-08 Round 48(`ArtPipeline/Archive/48-grid-board/README.md`)의 다섯 차례 목업과 판정 뒤, 사용자가 **"가죽 배낭 3x3로 시작, 틀은 3x8로. 돌리기는 백팩 배틀즈 규칙 조사 후 그대로 적용 구현 시작"**을 지시했다(세 번).
  돌리기를 조사하고(Roadmap "19단계의 승인" 3) 19단계를 열어 구현했다. 합본의 "권장안, 판정 대기"는 이 지시로 권장안대로 구현했고 사후 검토 대상이다.
- 구현과 문서가 끝났고 체인과 스크린샷이 통과했다(아래 Verification). 2026-10-09 Round 49(인벤토리의 디아블로 모습과 10×3 격자)까지 구현했다. **2026-10-09 세션 정리에서 주제별로 커밋하고 푸시했다.**
- 20단계(★ 닿음 효과)는 열지 않았다(Round 48 판정 13의 권장: 19·20의 두 단계, 직접 플레이는 그 뒤 — 판정 대기).

## Done

- 기획: Design/02 §4(격자, 시작 가방, 칸은 가방으로, 모양, 돌리기와 조사, 읽는 순서, 적의 보드, ★ 【제안】, 합치기의 왼쪽 위), 03 §5(정예의 가방, 상점의 가방, 인벤토리의 넓이 30, 놓는 규칙, 가방 다루기),
  10 §5(쿨다운 = 아이템의 빛), 08 §15(시뮬), 00 "Slice B" 표와 권장안 세부.
- 데이터: `ItemData.csv` `Size` → `Width`·`Height`(아이콘의 비율), `JobData.csv`의 `ItemSlots` 삭제, 새 `BagData.csv`(허리 주머니 2×1, 가죽 배낭 3×3 시작, 가죽 주머니 3×1), `BalanceData.csv`의 `InventoryCells` 30·`EliteBagPercent` 50,
  `BoardFrame`(3×8, `DataEnums.cs`), `BagData.cs`, `StaticData`의 `Bags`·`StartBag`과 검증 셋(시작 가방 하나, 무기가 시작 가방에 들어감, 적 아이템 높이 합 ≤ 8), Mapper·Transformer·Loader·`StaticDataFiles`, Generated.
- Domain: `Gameplay/Expedition/ItemBoard.cs`(다시 씀: `Placement`, `BoardItem`, `BoardBag`, `ItemBoard`, `BoardLayout`), `ExpeditionMember.Board`, `OfferKind.Bag`, `BattleUnitSetup.Layout`, `BattleEngine`의 칸 검사 삭제,
  `ExpeditionRules`(놓기 `CanPut`·`Put`, 옮기기·인벤토리·가방 `CanPickBag`·`CanMoveBag`·`MoveBag`(가방째 돌리기), 합치기 자리 `MergesAt`, 정예의 가방 `DrawBag`, 상점의 가방, 정비의 칸, 읽는 순서의 Setup).
- 저장: `RunSaveData` 10(`MemberRecord.Bags`, `BagRecord`, 아이템의 `X`·`Y`·`Turns`, 물건의 `Bag`), `RunSaveMapper.ToBoard`(격자 검증), `RunSaveMigrator.From9To10`.
- Application: `ExpeditionManager`의 보드 명령이 칸과 `Placement`를 받고 가방 명령 셋을 더함.
- 시뮬: `Tools/Sim/src/Policies.cs`(격자의 자리·합치기·사기·줍기·꺼내기), `Simulations.cs`(가방 줄).
- 화면: `UI/GridGeometry.cs`, `UI/BoardHand.cs`(손, 그림자 `GridGhost`, `IOutsideHand`), `UI/TurnInput.cs`, `UI/Views/GridBoardView.cs`, `GridSquareView.cs`, `ItemSlotView.cs`(조각), `PartyBoardView`, `PartyColumnView`, `PartySideView`(손, `TurnHeld`),
  `NodeMapScreen`(`ShopHand`, 정비의 칸, 가방 타일), `ItemTileView`·`ShopTileView`·`LootDropView`(가방), `BattleScreen`(`LootHand`, `DropInHand`), `BattleBoardView`(격자), `BattleItemView`(쿨다운 안 2), `UiPalette`, `UiKeys`, `UiText`,
  문구(`UI_StaticText.csv`: 넷 더함, 여섯 고침), Builder `Editor/Setup/Ui/UiPrefabSetup.Grid.cs`와 PartySide·Battle·NodeMap.
- Test: EditMode `ItemBoardTests`(다시 씀), `Support/TestBoards.cs`, `TestData`·`TestCsv`(모양, 가방 셋), `ExpeditionRulesTests`(격자 묶음), `TierRulesTests`·`ShopRulesTests`(가방 판매 Test)·`FatigueRulesTests`, Flow의 `ExpeditionManagerTests`(가방 명령)·`ShopFlowTests`·`CampFlowTests`·`RunSaveMapperTests`(격자 거부 사례 열여섯, 자리와 방향 저장, `Migrate_From9…`)·`RunSaveTests`, 데이터 Test.
  PlayMode `UiTestUtil`(`ClickSquare`·`RightClickSquare`·`HoverSquare`·`Put`·`ItemAt`·`Regrade`), `UiFlowTests`(보드를 다루는 Test 열), `UiScreenshotTests`(`_08`·`_30`·`_35` 격자로, `_44_map_turned`·`_45_map_bag_held` 더함).
- 문서: CLAUDE.md §9, Architecture/05·07·08·09·11·12, Roadmap("19단계의 승인", 사후 검토 "19단계의 권장안 세부", 19단계 절, 머리, 날짜 표, 진행 순서), 이 Handoff, `b16-tuning.md`의 Round 48 줄.

- **Round 49(2026-10-09) 인벤토리 UI와 격자**: 기획 Design/00·01·03 §5·10 §5, 데이터 `InventoryWidth`·`InventoryHeight`, Domain `InventoryGrid`(`ItemBoard.cs`)·`ExpeditionRules`(자리로 세기, `MoveToInventoryAt`·`MoveInInventory`), 저장(인벤토리의 X·Y·Turns, `From9To10`), `ExpeditionManager`,
  화면(`UiPalette` 디아블로 색, `UiPrefabSetup.Grid`·`PartySide`·`NodeMap`·`Battle`·`Kit`, `GridBoardView`·`GridSquareView`·`ItemSlotView`·`BattleBoardView`·`BattleItemView`·`ItemTileView`·`ShopTileView`·`LootDropView`·`ItemTooltipView`·`PartySideView`, 새 `InventoryGridView`·`SquareGrid`, `BoardHand`의 인벤토리 손, `InventoryEntryView` 삭제), 문구, Test, Architecture/07·08·11·12.

- **Round 50(2026-10-09) 아이템의 크기**: 기획 Design/02 §4·00 "Round 50", 데이터 `ItemData.csv`의 `Width` 열넷 3 → 2(가로는 2칸까지: 3×1 → 2×1, 3×2 → 2×2, 3×3 → 2×3), 시뮬 Design/08 §16, Architecture/12 "아이템의 아이콘". 코드·저장 형식은 그대로(좁아지기만 해 지금 저장의 자리가 모두 유효).
  목업과 대조해 아이콘 파일의 빈 둘레를 잘랐다(`ArtPipeline/tools/trim_items.py`, Architecture/12·13). PlayMode Test 셋이 무기의 마지막 칸을 `Item.Width`로 센다.
  → **정정**(사용자 "기본 무기 3*1로 복구, 장비 무기류는 2*1 이상", "쥐 송곳니를 쥐 발톱으로 대체"): 기본 무기 다섯 3×1, `rat_bite` = 쥐 발톱 2×1(id 그대로, 그림은 송곳니). 시뮬 63.8% / 0.31, 보스전 91.4%(Design/08 §16).

## Open

- **Round 51**(2026-10-10, 판정 뒤 20단계로 구현: `b20-stars.md`. `ArtPipeline/Archive/51-claw-axe-whetstone/README.md`): 쥐 발톱 그림의 후보(`ArtPipeline/output/item/rat_claw.png`, 연결은 승인 뒤), 발키리의 대도끼 3×2(목업과 시뮬), 숫돌과 ★ 닿음(20단계의 권장 범위: ★ 체계와 숫돌 하나). 그림 파이프라인의 아이템 경로는 격자의 모양으로 고쳤다(`gen_image.read_item_cells`).
- 쥐 발톱의 그림(지시가 있을 때, 유료 호출 1회. 그 전에 그림 파이프라인의 아이템 경로를 격자의 모양으로). 2026-10-09 20:01의 진행 중 저장은 정정으로 무효가 된다(인벤토리의 쥐 발톱이 녹슨 칼과 겹침).

- 사후 검토(Roadmap "19단계의 권장안 세부"). 합본과 다르게 한 것: **가방은 인벤토리에 넣지 않는다**(합본의 "빈 가방만 인벤토리로"). 칸이 모두 찬 가방은 아이템을 하나 빼야 집힌다.
- 돌리기의 방향과 가방째 돌리기는 백팩 배틀즈 자료에 없어 권장안이다(Design/02 §4). 사용자가 실제 게임과 다르다고 하면 `TurnInput`과 `ExpeditionRules.MoveBag`을 고친다.
- 시뮬은 18단계보다 클리어 4%p 낮다(62.2%, 띠 안). 시뮬은 밀어내기와 가방 옮기기를 하지 않는다. 직접 플레이 뒤에 가방의 값·정예의 가방 확률·큰 아이템의 모양을 본다.
- 20단계(★ 닿음 효과): 효과를 가진 아이템과 수치는 기획 【제안】 → 시뮬 → 승인. Round 48 판정 8·9·12·13이 남았다.
- 가방의 그림은 도형(가죽 색 셋과 바늘땀 점선)이다. 그림은 지시가 있을 때.
- **Round 49 인벤토리 UI 스타일**(2026-10-09 사용자 "인벤토리 ui 스타일이 마음에 안들어"): 디아블로 2 / 백팩 배틀즈 / 권장 3 원정 짐칸의 목업과 판정할 것 아홉(`ArtPipeline/Archive/49-inventory-style/README.md`). → 사용자가 **디아블로 2를 골라 변형** → 판정 V1~V6("다른안·돌 테·디아블로 식·금색·디아블로식 격자·빨강 칠만") → **구현**(아래 Done의 Round 49 줄). 사후 검토: 인벤토리의 첫 빈 자리 규칙(돌리지 않은 채 먼저), 직접 놓기는 빈 칸에만(바꾸기 없음), 전리품·상점은 버튼으로만 인벤토리에, 인벤토리 칸 66, 저장 버전 10 그대로.
- **Round 50 아이템 크기**(2026-10-09 사용자 "아이템 크기 재조정 검토 후 보고"): 검토·시뮬·목업 셋(`ArtPipeline/Archive/50-item-size/README.md`). 권장 B2 → 사용자 **"권장안 구현"** → 구현(아래 Done의 Round 50 줄). 사후 검토: 보스전 92.3%(띠 위 0.3%p)는 직접 플레이 뒤의 맞추기에서, 가로로 긴 아이콘이 2칸에서 읽히는지는 직접 플레이에서.
- 직접 플레이의 체크리스트(`b16-tuning.md`)에 더할 것: 든 채로 우클릭·휠·R이 손에 맞는지, 그림자가 읽히는지, 3×8 틀의 높이가 패널에 맞는지, 쿨다운 안 2가 잘 읽히는지, 가방을 집는 법(빈 칸)이 드러나는지.

## Verification

- `dotnet build Tools/Sim`, `transform`, `validate` OK(Generated 아홉 + `BagData.json`).
- 시뮬(시드 1, Design/08 §15): balanced **62.2% / 0.34, 붕괴 45.0%, 보스전 89.5%, 138.1초**, safe 54.7% / 0.31, none 67.6% / 1.80, 여섯 파티 64.5 / 56.1 / 42.5 / 33.9 / 5.5 / 0.0%. 가방은 용병마다 0.20개.
- 체인: `setup` OK(Test 컴파일 오류를 모두 고친 뒤), `sim editmode` OK — **EditMode 787/787**.
  PlayMode 1차 87 중 3 실패(Test 둘, 화면 하나: 상점의 물건을 든 동안 설명 줄 → `PartySideView.Refresh`에서 고침) → 2차(`20261009-002305`, `setup playmode`) **PlayMode 78/87**(9 skipped: 스크린샷), **CHAIN OK**.
- 스크린샷 `20261009-s19a`: 9/9, PNG 75. 고친 것 둘: 가방을 든 동안의 틀 칸과 그림자가 꽉 찬 색이었다(`Outline`이 비치는 칸을 채움 → `GridFrameFill`, 그림자는 비치는 칠 + 둘레의 선 `GhostEdge`). 전투·상점·전리품의 격자는 의도대로였다.
- 가방 PlayMode Test(`PartySide_ABagIsPickedByAnEmptySquare_…`)가 버그를 찾았다: 왼쪽 위 칸에 아이템이 있는 가방은 집혀도 놓이지 않았다 → `ExpeditionRules.CanMoveBag`이 가방의 어느 칸으로도 찾는다(`MovableBagAt`), EditMode `ABagPickedUp_MovesByAnyOfItsSquares_EvenOneUnderAnItem`.
- 체인 3(고친 뒤 `setup editmode playmode`): `20261009-005534`: EditMode **788/788**, PlayMode **79/88**(9 skipped), **CHAIN OK**; sim 단계도 OK(`20261009-010716`, 시뮬 수치 같음). 스크린샷 2차: `20261009-s19b` 9/9, PNG 75 — 틀은 옅은 칸, 그림자는 비치는 칠과 둘레의 선과 아이콘. 구현 장면 여덟을 `ArtPipeline/Archive/48-grid-board/game/`에 두었다.
- 보고 뒤 사용자 지적("원래 이런식으로 점선을 넣어서 가방 느낌 넣기로 하지 않았나?"): 목업의 가방 바늘땀 점선(`mock_bags.py`의 `bag_under`)과 가방을 든 동안의 점선 틀 칸(`dashed_cell`)을 빠뜨렸었다 → `UI/Views/DashedFrame.cs`(그림 없이 그리는 점선)로 넣었다: 보드·전투 보드·상점 타일의 가방(`UiPalette.BagStitch`), 틀 칸(`GridFrameLine`).
  prefab 검사(`UiPrefabSetup.AddMissingReferences`)가 Graphic의 Unity 필드(`m_Material`)를 빈 참조로 잡아 F1 필드(`_…`)만 보게 했다. 체인 `setup playmode` OK(PlayMode **79/88**, 가방 Test가 바늘땀과 점선 칸을 확인), `setup editmode` OK(**788/788**). 스크린샷 `20261009-s19c` 9/9(PNG 72: 이번 런의 영어 지도에는 상점이 없어 `en_41`~`43`이 빠짐. 상점 타일의 가방은 이번 그림에 나오지 않았다). `game/`의 장면을 새로 바꿨다.
- 작업 중 한 번 Architecture/12의 절 넷(격자 보드, 파티 쪽, 인벤토리 팝업, 노드 맵의 오른쪽)이 범위 치환으로 지워졌다가 HEAD에서 다시 만들었다(지운 것 말고는 같음을 diff로 확인). 같은 방식으로 지워졌던 UiFlowTests의 doc 주석 하나도 되살렸다.

- **Round 49 구현의 검증**(2026-10-09): `dotnet build Tools/Sim`, `transform`(BalanceData), `validate` OK. 시뮬 balanced 62.2% / 인벤토리 1.24로 같다(시뮬에서는 인벤토리가 차지 않는다).
  체인 1차: EditMode 794 중 2 실패(Test의 기대: 첫 빈 자리의 차례 → 돌리지 않은 채 격자 전체를 먼저 찾도록 `InventoryGrid.FindRoom`을 바꿈, 변환 Test의 기대값 계산 실수), PlayMode 79/88 OK.
  체인 2차 `20261009-162858` **CHAIN OK**(EditMode 794/794, PlayMode 79/88, 9 skipped). 스크린샷 `20261009-r49`에서 목업과 대조: 전투 중 아이템 밑으로 우물의 회색이 비치고 적의 아이템이 바탕 없이 떠 있었다 → `BattleBoardView`가 가방의 칸을 아이템 밑까지 검게, 적의 아이템마다 붉은빛 돌 우물(`UiPalette.EnemyRim`).
  상점의 고른 타일에 목업의 "손에 듦"이 빠져 있었다 → `Map.ShopHeld`. 체인 3차 `20261009-164435` **CHAIN OK**(EditMode 794/794, PlayMode 79/88), 스크린샷 `20261009-r49b` 9/9(PNG 75). 장면 일곱은 `ArtPipeline/Archive/49-inventory-style/game/`.

- **Round 50 구현의 검증**(2026-10-09): `transform`·`validate` OK, 시뮬 balanced 64.4% / 0.30(Design/08 §16). 체인 1차 PlayMode 3 실패(Test의 3칸 가정) → 2차 `20261009-231440` CHAIN OK.
  아이콘을 자른 뒤 체인 3차 `20261009-233251` **CHAIN OK**(EditMode 798/798, PlayMode 79/88, 9 skipped), 스크린샷 `20261009-r50b` 9/9(PNG 75). 장면은 `ArtPipeline/Archive/50-item-size/game/`.

- **정정의 검증**: 체인 `20261009-235658` **CHAIN OK**(setup·sim, EditMode 798/798, PlayMode 79/88), 스크린샷 `20261009-r50c` 9/9.

## Next Action (제안)

- 사용자: 구현 장면(`ArtPipeline/Archive/48-grid-board/game/`)이나 직접 플레이로 격자를 본다. 사후 검토(가방과 인벤토리, 돌리기 방향. 모양과 칸은 Round 50에서 정함)와 20단계(★)의 판정. ★의 시뮬은 Round 50의 모양으로 잰다.
- 사용자: Round 49 구현 장면(`ArtPipeline/Archive/49-inventory-style/game/`)이나 직접 플레이로 인벤토리 격자와 디아블로 모습을 본다. 사후 검토는 Roadmap "Round 49의 세부".
