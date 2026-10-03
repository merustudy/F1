# Slice A 개정 2~5 — 상대 범위, 아이템 크기, 인벤토리, 화면 통일, 인벤토리 용량, 아이템 칸 수와 칸 크기

Snapshot: 2026-10-03 (개정 2~4는 구현, 검증, 커밋, 푸시 완료(2026-10-01). 개정 5(아이템 칸 5칸·최대 8칸, 보드 패널의 정사각 칸 72와 가방, 패널 358)를 구현해 체인을 통과했고 같은 날 세션 정리에서 커밋·푸시했다. 사후 검토 대기)

## Goal

개정 2: 아이템을 쓸 수 있는 자리와 패시브의 자리 조건을 공격 범위와 같이 "앞이나 뒤에서 N번째까지"로 센다(조합 절벽의 선택지 "다").
아이템은 크기(1~3칸)만큼 보드를 차지하고, 보드에서 밀려난 아이템은 원정 인벤토리로 간다. Slice A의 루프, 저장, 화면, 시뮬이 새 규칙으로 그대로 돈다.

개정 3: 노드 맵과 보상 화면의 파티가 전투와 같은 열로 보인다("C안"). 노드를 고를 때 적은 보이지 않는다. 포션은 세 화면에서 같은 자리, 인벤토리는 팝업.

개정 4: 인벤토리는 10칸(`InventoryCells`). 아이템은 크기만큼 차지하고, 들어갈 자리가 없으면 인벤토리로 갈 수 없다.

개정 5: 용병의 보드는 5칸으로 시작하고 최대 8칸이다. 보드 패널의 칸은 정사각 72x72이고 한 줄에 여덟 칸이 들어가며, 유닛의 칸 수만큼만 보이고 뒤에 가방이 깔린다. 패널은 358.

## State

- 세 개정 모두 구현, 문서, 검증, 커밋이 끝났다. 2026-10-01 세션 정리에서 `8a778e5` 위에 네 커밋으로 올렸다
  (문서 / 데이터·Domain·저장·시뮬·Test / 화면 / Roadmap·Handoff. 세 개정이 같은 파일을 겹쳐 고쳐서 개정별이 아니라 층별로 나눴다). 작업 브랜치는 `feature/slice-a`.
- 2026-10-01 개정 1~4(`7458333..ce9fec4`)를 코드 리뷰했다(사용자 지시 "1단계만 수행"). 결과를 바꾸는 버그는 없었고, 5건(정확성 낮음 1, 구조 1, 재사용 1, 단순화 2)을
  사용자 승인("권장 구현")으로 모두 반영했다(아래 "리뷰 반영"). 체인 통과. 2026-10-01 세션 정리에서 두 커밋(리뷰 반영 / Roadmap·Handoff)으로 올리고 푸시했다.
- 개정 1(열)의 Handoff는 `completed/a-rows.md`로 옮겼다. 그 사후 검토 가운데 조합 절벽과 아이템 크기가 개정 2로 이어졌고,
  플레이테스트는 사용자가 추후로 미뤘다. 개정 3은 개정 2 직후 목업 여섯 장으로, 개정 4는 개정 3 직후 한 줄의 지시로 정했다.
- 승인 범위와 사후 검토 목록: `Docs/Roadmap.md` "개정 2의 승인", "개정 3의 승인", "개정 4의 승인", "사후 검토 대기".
  기획의 목록: `Docs/Design/00_INDEX.md` "개정 2", "개정 3", "개정 4".
- 목업(SVG와 PNG)은 저장소 밖(세션 scratchpad)에 있고 보관하지 않는다. 승인된 내용은 Architecture/12가 가진다.

## 개정 3 (화면 통일과 적 정보 숨김)

사용자가 정한 것: C안(노드 맵·보상의 파티를 전투와 같은 열로, 왼쪽에), 적 정보 숨김(①, Design/03 §1 【확정】), 포션을 전투와 같은 자리에,
인벤토리는 "인벤토리 보기" 버튼과 세로 팝업. A안(전투의 장비를 하단 패널에 가로로)은 보류됐다.

권장안으로 정한 세부(검토 대상): 팝업이 오른쪽(맵, 보상 목록)을 전부 덮는다 / 노드 맵에도 같은 포션 자리와 팝업 / 죽은 용병의 열은 비워 둔다(전투와 같이) /
"인벤토리로" 버튼은 파티 아래 / 열 순서는 전투와 같은 4열→1열 / 보상을 고른 동안 팝업의 아이템과 "인벤토리로"는 꺼진다(고르는 것은 하나).

바꾼 것:
- Views: `PartySideView`(파티 쪽 전체: 열, 포션, 팝업, 선택 모델), `PartyColumnView`(열 하나: 그림, 카드, 세로 칸), `InventoryEntryView`(팝업의 한 줄).
  `PartyBoardView`와 `BoardMemberView`는 지웠다. `ItemSlotView`는 한 줄(이름 왼쪽, 등급 오른쪽)이고 높이를 받는다.
- Screens: `NodeMapScreen`(적 목록 제거, 인벤토리 토글), `RewardScreen`(보상을 위아래로, 인벤토리 토글).
- Builders: `UiPrefabSetup.PartySide`(옛 Board 파일을 대체. 열의 자리는 `PartySideView`가 열릴 때 전투와 같은 `FieldLayout`으로 계산한다 — 리뷰 반영), `NodeMap`, `Reward`,
  `Battle`의 `BuildFigure`·`BuildSilhouette(prefix)` 공용화.
- 문구: `Map.Unknown`, `Board.InventoryShow`·`InventoryHide`·`InventoryTitle`·`InventoryHint`, `Board.Hint` 갱신(영어는 파티 아래 두 줄에 맞게 짧게). 지운 것: `Map.Enemies`·`Enemy`, `Board.Dead`·`Potions`·`Inventory`.
  보상 카드가 넓고 낮아져서 `UiText.ItemDetails`는 종류·크기·쿨다운·자리를 한 줄에, 효과를 줄마다 적는다(스크린샷에서 다섯 줄이 카드 밖으로 넘친 것을 고침).
- 문서: Design/03 §1, 00("개정 3"), Architecture/12("파티 쪽", "인벤토리 팝업", "노드 맵의 오른쪽", "보상 화면의 오른쪽", Test).
- Test: PlayMode `PartySide_ForwardAndBack_...`, `PartySide_InventoryPopup_...`(옛 Board Test 둘을 대체), 스크린샷의 팝업 장면.

## 개정 4 (인벤토리 용량)

사용자가 정한 것: "인벤토리는 아이템칸 10칸으로 제한하자". 개정 2의 권장안(용량 무제한)을 바꿨다. `BalanceData.InventoryCells` = 10.

권장안으로 정한 세부(검토 대상):
- "아이템칸 10칸"을 **칸**으로 읽었다. 아이템은 보드에서처럼 크기만큼 차지한다(10개가 아니다). 10개로 읽는 게 맞다면 `FreeInventoryCells` 한 곳을 고치면 된다.
- 들어갈 자리가 없으면 인벤토리로 가는 길이 모두 막힌다: 보상의 "인벤토리에 넣기", 보드의 "인벤토리로", 찬 자리에 넣어 밀려나는 것(보상이든 인벤토리의 아이템이든).
  인벤토리에서 보드의 찬 자리로 넣을 때는 나가는 아이템의 칸이 비는 것으로 친다. 아무 데도 못 넣는 보상은 넘긴다.
- "칸에 아이템이 있어 집을 수 있는가"를 `CanPickItem`으로 따로 뒀다. 전에는 화면이 `CanMoveToInventory`를 그 뜻으로 썼는데, 이제 그 질의는 자리까지 보므로
  인벤토리가 차면 보드 사이의 옮기기까지 막혀 버린다. 집는 것은 인벤토리가 차도 된다.
- 저장 버전은 3 그대로(DTO의 모양이 같다). 넘치는 파일은 검증이 거른다. 상수의 하한은 가장 큰 아이템의 크기(3).

바꾼 것:
- Data: `BalanceData.InventoryCells`(3..100), `BalanceData.csv`, Test 데이터(`TestData`, `TestCsv`)에 10.
- Domain: `ExpeditionRules.FreeInventoryCells`, `CanTakeRewardToInventory`, `CanPickItem`. `CanPlaceReward`·`TakeItemReward`·`CanPlaceItem`이 밀려나는 아이템의 자리를 본다.
  `CanMoveToInventory`/`MoveToInventory`, `CanPlaceFromInventory`/`PlaceFromInventory`가 `StaticData`를 받는다.
- Flow: `ExpeditionManager.CanTakeRewardToInventory`, `CanPickItem`. `RunSaveMapper`의 검증 하나.
- Sim: `ChooseReward`가 보드 → 인벤토리(들어가는 첫 아이템) → 넘기기.
- UI: 팝업 제목 "인벤토리 (쓴 칸/전체 칸)", 보상의 "인벤토리에 넣기"는 들어갈 때만, 집기는 `CanPickItem`. 문구 `Board.InventoryTitle`의 인자 둘.
- 문서: Design/03 §5, 00("개정 4", 현황, 개정 2의 표), 01(용어), Architecture/07(검증, Schema Version), 08(인벤토리), 11(질의), 12(팝업, Test). Roadmap.
- Test: `ExpeditionRulesTests` 둘(칸으로 세는 것과 집기 / 밀려나는 아이템의 자리), `DefinitionTests`(하한), `RunSaveMapperTests`(넘침 거부, 꽉 찬 것 허용),
  `ExpeditionManagerTests`(질의), PlayMode 팝업 Test에 제목과 가득 찬 상태.

## 개정 5 (아이템 칸 수와 보드 패널의 칸 크기, 2026-10-03)

사용자가 정한 것(목업 네 번. Roadmap "Slice A 개정 5"): E안(칸 100x72, 얼굴 72, 패널 372), 초기 5칸·최대 8칸, 칸은 유닛의 지금 칸 수만큼만, 칸 뒤에 가방(백팩 배틀즈식), "이 모양으로 구현".
E안을 구현한 뒤 "1칸을 정사각으로, 패널을 약간 낮게" → **G안**(정사각 72x72, 얼굴 72, 줄 사이 10, 패널 358·위에서 722) "G안으로 반영". 2칸 148x72, 3칸 224x72.
아이템의 크기와 아이콘은 사용자가 따로 재조절한다(미정). (→ 2026-10-03 저녁 V안이 칸을 180x50의 세로 쌓임으로 되돌려 아이콘 재조절은 필요 없어졌다. `a-battle-feel.md` "세로 열 라운드".)

권장안으로 정한 세부(검토 대상, Design/00 "개정 5"): 칸이 느는 방식은 비워 둠(지금은 모두 5칸) / 줄 사이 10(가방이 닿지 않게), 여백 24, 칸 사이 4, 아이콘 여백 6, 가방은 칸의 좌우 8·위아래 3 /
가방은 도형 Placeholder 틀(`ArtPipeline/Archive/08-panel-cells/draw_bag.py` → `Assets/@Art/UI/Frame/bag.png`, Border 40), 적의 줄에도 같음 / 패널이 34 높아진 만큼 맵 596, 보상 카드 188(사이 8), 팝업 606, 버튼 줄 258.

바꾼 것:
- Data: `JobData.csv`의 `ItemSlots` 4 → 5(여섯 직업), `JobData.json`, `JobData.MaxItemSlots` 6 → 8. `StaticDataValidationTests`의 넘치는 적은 9칸으로.
- Views: `BattleItemView`(칸 72x72, 간격 4. 세로 칸의 `CellGap`·`BoardHeight`는 지웠다), `BattleBoardView.BoardWidth`, `PartyColumnView`(`_slotParent`를 RectTransform으로 두고 칸 수만큼의 폭으로, `BoardWidth`).
- Builders: `UiPrefabSetup.Battle`(패널 722/358, 여백 24, 얼굴 72·안쪽 6·배지 24, 줄 사이 10, `BuildBoardBag`), `PartySide`(가방, 버튼 줄 258), `Kit`(아이콘 여백 6, `BoardBagPadX/Y` 8/3, `BuildBoardBag`), `Reward`(카드 188, 사이 8), `UiArt.Bag`(Border 40).
  Prefab 셋과 Stamp가 다시 만들어졌다. `bag.png`와 `.meta`가 새로 생겼다(setup의 Import).
- Test: PlayMode `UiFlowTests`의 보드 검사 둘에 보드(와 가방)의 폭이 칸 수와 같은 것.
- 문서: Design/02 §4, 10 §5, 00(현황, 개정 2 표, "개정 5"), 08 §8; Architecture/12("아이템의 아이콘", "파티 쪽", "보상 화면의 오른쪽", "전투 화면", Test), 13("후처리 (`cell`)" 표, "얼굴", "Unity 배선", Deferred); Roadmap.
- 기록: `ArtPipeline/Archive/08-panel-cells/`(README, `mock_panel_cells.py`, 시트 아홉 장, `draw_bag.py`).

## 리뷰 반영 (2026-10-01)

개정 1~4의 코드 리뷰에서 나온 5건. 규칙과 데이터는 바뀌지 않았다(시뮬 수치 그대로).

- `ExpeditionRules.CanMoveItem`: 같은 보드 안에서 **이미 끝에 있는 아이템을 빈 칸으로** 옮기는 것은 바뀌는 게 없으므로 거부한다(전에는 "가능"이라 화면이 빈 칸을 켜 주고 눌러도 아무 일이 없었다).
  `ExpeditionRulesTests.MoveItem_WithinOneBoard_ReordersIt`에 고정.
- `FieldLayout`(새 파일, `UI/`): 전장 Column의 폭과 x를 계산하는 공식 한 곳(`ColumnGap`·`SideGap`도 여기로. 옛 `BattleScreen` 상수는 지웠다).
  `BattleScreen.LayoutColumns`와 새 `PartySideView.LayoutColumns`가 같은 공식을 쓴다. 전에는 파티 쪽 Column의 자리가 Prefab 생성 때 4인 기준으로 고정이어서
  `PartySize`가 4가 아니면 전투 화면과 어긋났다. Builder(`UiPrefabSetup.PartySide`)는 전투 전장과 같은 상자 `PartyField` 안에 Column을 두고, Column 안은 전투 유닛처럼 `UiBuild.Line`으로 만든다
  (`_columnRoots` 참조는 없앴고 `_field`·`_floor`가 생겼다. Prefab 둘과 스탬프가 다시 만들어졌다).
- `PartyColumnView`: 칸 높이·간격 상수를 지우고 `BattleItemView.CellHeight`·`CellGap`·`BoardHeight`를 쓴다. Builder의 카드 높이도 같은 공식.
- `PartySideView.ToggleInventory`: 자기 `Refresh()`를 뺐다. 토글 뒤에는 화면(`NodeMapScreen`·`RewardScreen`)의 `Refresh()`가 한 번만 다시 그린다.
- `BattleEngine`: `RearmostRow`를 지우고 `EnemyBack` 타깃도 `LineLength`를 쓴다(살아 있는 줄은 1..n에 빈 열이 없으므로 같은 값).
- 문서: Architecture/12("파티 쪽", "전투 화면"의 Column 자리 계산).

## 결정

사용자가 정한 것 (2026-10-01, 기획문서의 `【확정】 (개정 2)`):

1. 쓸 수 있는 자리는 열 번호가 아니라 앞이나 뒤에서 세어 "N번째까지"로 정한다 (선택지 "다").
2. 패시브의 자리 조건도 같은 셈법이다.
3. 아이템은 여러 칸을 차지할 수 있다.
4. 원정에 인벤토리를 두고, 보드에서 밀려난 아이템은 거기로 간다 (제시한 C1~C3 대신 사용자가 새로 정한 안).

"그 외 권장안 반영"으로 맡겨져 권장안으로 정한 것 (`【확정】 (개정 2 권장안)`, 검토 대상). 그중 판단이 들어간 것:

- **"뒤에서 N번째까지"는 살아 있는 줄의 길이로 센다.** 넷이면 `back:3`은 2~4열, 둘이면 1~2열. 줄은 짧아지기만 하므로 전투 중에 자리를 잃는 일이 없고
  켜지는 일만 있다. 맨 뒤가 죽어 줄이 짧아지면 앞 유닛의 `back:N`이 켜질 수 있다(직관과 어긋날 수 있는 점. 02 §4에 적었다).
  그래서 죽음마다 그 쪽 **전원**의 쓸 수 있는 자리를 다시 본다(`BattleEngine.RefreshSide`).
- **양쪽에 같은 규칙이다.** 적의 궁수와 주술사도 앞이 죽은 뒤 계속 쏜다. "앞을 먼저 잡아 뒤를 멈춘다"는 전술은 사라졌다. 그 몫만큼 적이 세져
  `crude_bow` 계수를 100 → 90으로 내려 기본 파티를 개정 전 자리(≈81%)에 뒀다(08 §8).
- **데이터 형식**: `Rows`와 `PassiveRows`를 `front:N` / `back:N` / `all`로 적는다(`RowSpan`). 열 번호 목록 `1+2`는 더 쓰지 않는다.
- **보드는 순서 있는 목록 + 칸 수**(B2)다. 빈 칸 없이 앞에서부터, 발동 순서 = 보드 순서. 칸 번호가 있는 자리(Bazaar식, 사이 빈 칸 허용)는 인접 효과가 없어 두지 않았다.
- 인벤토리는 용량 제한이 없다 — **개정 4가 `InventoryCells` 10칸으로 바꿨다**(위 "개정 4").
- **넣는 규칙**: 빈 자리를 고르면 보드의 끝에(들어갈 때만), 찬 자리를 고르면 그 아이템이 인벤토리로 빠지고 새 아이템이 그 자리에. 빠진 뒤에도 안 들어가면 못 넣는다.
  "버리기"는 두지 않았다(없어지는 아이템이 없다).
- **보드 칸 수 4** (A1 "늘림"). 보상이 전투 층 수(3)만큼이라 5 이상이면 보드가 차지 않아 크기가 뜻을 잃는다. 4면 3칸 아이템은 무기와 함께 들어가고,
  2칸 둘은 안 들어간다.
- **콘텐츠**: 기본 무기 전부 1칸. `longbow`·`spear` 2칸(계수 70 → 85), `halberd` 3칸(55 → 80). 큰 아이템이 칸당 효율에서 손해만 보지 않게.
- **저장**: Schema Version 3. 2(칸마다 아이템이나 null, 인벤토리 없음)는 `From2To3`로 올린다(null을 빼고 빈 인벤토리). 아이템 크기 때문에 보드가 넘치면 검증에서 무효.
- **화면**: 보드 칸은 가로로 `ItemSlots`개, 아이템은 크기만큼 넓은 View. 인벤토리 줄은 보드 아래(포션 줄 다음), 칩을 왼쪽부터, 스크롤 없이(넘치면 잘림).
  조작은 누르고 누르기. "인벤토리로"(보드의 고른 아이템)와 "인벤토리에 넣기"(보상 화면의 고른 보상)는 다른 버튼이다. 안내문은 아무것도 고르지 않았을 때 보드 아래 한 줄.
- **패시브 설명문**: knight·paladin "맨 앞:", archmage "뒤에서 2번째 자리까지:".

## Done

- 기획: Design/02 §4·§7(머리말 포함), 03 §5, 07 §4, 01 §6, 00("개정 2", 현황), 08 §7 결정·§8(시뮬).
- Owner 문서: Architecture/05(`RowSpan` 구문), 07(run.json 트리, 저장 시점, 검증, Schema Version 3), 08("자리 (열)"의 `RowSpan`·`RefreshSide`, 새 절 "아이템 보드와 인벤토리", Test·Validation),
  09(범위 표와 상태 수명에 인벤토리), 11(단계별 명령, 질의 목록), 12("아이템 보드와 인벤토리를 보여 주는 방식", 전투 카드, Test).
- Data: `Data/Definitions/RowSpan.cs`(`RowEnd`, `Contains(row, lineLength)`, `TryParse`), `ItemData.Size`·`Rows: RowSpan`·`UsableIn(row, lineLength)`,
  `PassiveSpec.Rows: RowSpan`(null 허용), `BattleRows`는 `IsValid`만, `StaticData`의 검증(무기 크기 ≤ 직업 칸 수, 적 아이템 크기 합 ≤ `MaxItemSlots`),
  `CsvRow.Rows`/`OptionalRows`, `ItemMapper`의 `Size` 열.
- Domain: `Gameplay/Expedition/ItemBoard.cs`(칸 ↔ 목록, `CanPut`, `Put`), `ExpeditionState`(`Members[].Items` 목록, `ItemSlots`, `Inventory`),
  `ExpeditionRules`(`CanPlaceReward`/`TakeItemReward`(칸), `TakeItemRewardToInventory`, `CanMoveItem`/`MoveItem`, `CanMoveToInventory`/`MoveToInventory`,
  `CanPlaceFromInventory`/`PlaceFromInventory`, `LivingCount`. `SwapItems`는 없앴다), `BattleTypes`(`BattleUnitSetup.ItemSlots`, 빈 칸 없는 목록),
  `BattleEngine`(`CreateUnit`에 줄 길이, `RefreshSide`, `ConditionHolds`가 줄 길이를 본다, Setup 검증에 보드 넘침과 빈 항목).
- Save: `RunSaveData` v3(`ExpeditionRecord.Inventory`, `MemberRecord.Items`는 빈 항목 없음), `RunSaveMapper`(`ToItems`, 보드가 직업 칸 수 안인지), `RunSaveMigrator.From2To3`.
- Flow: `ExpeditionManager`의 보드·인벤토리 명령과 `Can...` 질의.
- Sim: `SimPolicy.ChooseReward`(들어가면 보드, 아니면 인벤토리, 그 뒤 `EquipFromInventory`), `expedition` 통계에 끝날 때 보드와 인벤토리의 아이템 수.
- UI (개정 3이 파티 쪽을 `PartySideView`·`PartyColumnView`로 바꿨다. 위 "개정 3" 절): `ItemSlotView`(`Item`, `Index`), 선택 모델과 `ExternalCanPlace`·`CellClickOverride`,
  `RewardScreen`("인벤토리에 넣기", 고른 동안 칸 켜기), `BattleUnitView`(크기만큼 칸), `UiText.Rows`와 `Item.Size`, `UiPrefabSetup.PartySide`/`Reward`.
  문구: `Board.Inventory`·`InventoryEmpty`·`ToInventory`·`Hint`, `Item.Size`·`RowsFront`·`RowsFrontOne`·`RowsBack`·`RowsBackOne`, `Reward.Hint`·`ToInventory`. `Item.Rows`는 없앴다.
- CSV: `ItemData.csv`(`Size` 열, `Rows` 구문, 계수), `JobData.csv`(`ItemSlots` 4, `PassiveRows` 구문, `PassiveText`).
- Test: `ItemBoardTests`(새 파일), `DefinitionTests`(`RowSpan`), `StaticDataValidationTests`(크기 검증 둘), `StaticDataTransformerTests`(구문), `BattleEngineTests`의 "Advancing"
  (켜지는 것, 꺼지지 않는 것, 맨 뒤가 죽을 때, 패시브의 줄 길이), `ExpeditionRulesTests`의 보드·인벤토리 묶음, `RunSaveMapperTests`(v3 검증, 2 → 3 변환), `ExpeditionManagerTests`,
  `GameLoopTests`, `RunSaveTests`, `ShippedDataTests`, PlayMode `UiFlowTests.Board_Inventory_...`, `UiScreenshotTests`(인벤토리 장면 둘).

## Open

- 사람이 직접 플레이한 확인은 없다(사용자가 추후로 미룸). 큰 아이템과 인벤토리의 손맛은 스크린샷으로만 봤다.
- 조합 절벽의 나머지 선택지(무기의 자리, 뒤 열용 보상, 지팡이 직업)는 플레이테스트 뒤에 정한다(08 §7, §8).
- 큰 아이템과 인벤토리의 값은 짧은 던전에서는 거의 안 보인다(보드가 차지 않는다). 긴 던전(Deferred)에서 다시 본다.
- 적 쪽에도 상대 범위를 적용한 것은 권장안이다. 아군만 상대, 적은 절대로 두는 변형(적 아이템은 별도 id라 데이터만으로 가능)은 쓰지 않았다.
- 옛 저장 파일(Version 2)은 변환해 읽는다. 변환한 보드가 직업의 칸 수를 넘으면(없을 것이다: 옛 아이템은 전부 1칸이었고 칸은 3 → 4) 무효로 보고 타이틀이 알린다.

## Verification

- 개정 5 (2026-10-03, E안 뒤 G안) `Tools/chain.sh`: setup OK, sim OK, EditMode 616/616, PlayMode 43/43(`[Explicit]` 스크린샷 4개 제외). `git diff --check` 깨끗.
  두 번째 setup에서 Prefab 셋과 Stamp의 해시가 같다. 시뮬(같은 명령): 81.2% / 0.26 → 81.7% / 0.25, 보드 6.86개, 인벤토리 0.00개.
  바뀐 생성물: Prefab 셋, Stamp, `JobData.json`, 새 `bag.png`(.meta). ProjectSettings, Packages, Scene은 그대로.
  `Tools/screenshots.sh` 34장(4/4, G안): 전투(`_05`)에 정사각 다섯 칸의 보드가 가방 위에, 적의 줄은 거울상에 가방, 전진 뒤(`_15`) 죽은 적의 줄이 없고 두 칸의 적은 두 칸 가방,
  노드 맵(`_04`, `en_17`)과 보상(`_07`)의 파티 쪽도 같고 "빈 칸"/"Empty"와 등급 배지가 72의 정사각 칸에 들어가며, 보상 카드 셋이 패널 위에 들어간다. 넷은 `ArtPipeline/Archive/08-panel-cells/game/`.
- 리뷰 반영 뒤 `Tools/chain.sh`: setup OK, sim OK, EditMode 587/587, PlayMode 40/40(`[Explicit]` 스크린샷 3개 제외). `git diff --check` 깨끗.
  바뀐 생성물: `NodeMapScreen.prefab`, `RewardScreen.prefab`, `UiPrefabStamp.txt`. ProjectSettings, Packages, Scene, Generated JSON은 그대로.
  `Tools/screenshots.sh` 33장: 노드 맵(`_04`)·보상(`_07`)·인벤토리 팝업(`_17`)의 파티 열 넷이 전투(`_05`)의 아군 열과 같은 x와 폭에 서고, 카드 안 요소가 열 폭을 따른다.
  (관찰: 파티 쪽의 열은 전투보다 50px 위에 선다 — `PartyTop` 186 vs `BattleFieldTop` 236. 전투의 배경 그림 여백 때문이며 개정 3 때부터 그랬다. 사후 검토 거리.)
- `Tools/chain.sh` (개정 2~4를 합친 상태): setup OK, sim OK, EditMode 587/587, PlayMode 40/40(`[Explicit]` 스크린샷 3개 제외).
  개정 4 뒤의 시뮬(같은 명령): 81.2% / 0.26, 인벤토리 0.01개로 같다(Design/08 §8 "상태").
  개정 2만 있던 상태에서는 setup을 두 번 더 돌려도 파일이 바뀌지 않는 것을 확인했다.
  ProjectSettings, Packages, Scene은 바뀌지 않았다. 바뀐 생성물: Font Atlas, String Table 셋, `NodeMapScreen.prefab`, `RewardScreen.prefab`, `BattleScreen.prefab`(그림 자리의 이름),
  `UiPrefabStamp.txt`, Generated JSON 둘.
- 새 Test: `ItemBoardTests`, `RowSpan`(파싱·판정·범위), 크기 검증 둘, `BattleEngineTests`의 켜지는 것·꺼지지 않는 것·맨 뒤가 죽을 때·패시브의 줄 길이,
  `ExpeditionRulesTests`의 보드·인벤토리 묶음, 저장(v3 검증, 2 → 3 변환), PlayMode `PartySide_ForwardAndBack_...`, `PartySide_InventoryPopup_...`.
- 시뮬(3,000회, 시드 1, balanced): 기본 파티 81.2% / 사망 0.26 (개정 전 80.7% / 0.27). 가른 결과와 조합 표는 Design/08 §8.
- `Tools/screenshots.sh`: 33장. 적 목록이 없는 노드 맵과 파티 열 넷(`_04`), 보드의 아이템을 고른 상태와 설명 한 줄(`_08`), 인벤토리 팝업에서 항목을 고른 상태(`_17`),
  보상 화면의 쌓인 카드와 "인벤토리에 넣기"·"인벤토리 보기"·"넘기기"(`_07`), 전투(`_05`, 바뀐 것 없음). 두 Locale. 영어 안내문이 두 줄에 들어가는지 봤다.

## 알아둘 것

- 자리를 묻는 곳은 `RowSpan.Contains(row, lineLength)` 하나다. 줄 길이는 그 쪽의 살아 있는 수이고, 전투 밖(시뮬 정책, 화면)에서는 `ExpeditionRules.LivingCount`.
- 보드를 바꾸는 코드는 `ItemBoard`를 거친다. 칸 ↔ 아이템의 계산은 그곳뿐이고, 화면은 Manager의 `Can...`을 칸마다 물어 버튼을 켠다.
- `BattleItemState.SlotIndex`는 보드의 순서다(발동 순서). 로그의 `ItemActivated.A`도 같다.
- 저장소를 건드리지 않는 데이터 실험: `Assets/@Data`를 다른 폴더에 복사해 고치고 `transform --project-root <그 폴더>`, `expedition --project-root <그 폴더>`.
  이번 개정의 가른 실험도 그렇게 했다(scratch 폴더, 저장소 밖).

## Next Action (제안)

0. (해결) 2026-10-03 저녁 V안이 칸을 세로 쌓임의 띠로 되돌려 아이콘은 그대로 맞는다(`a-battle-feel.md` "세로 열 라운드").
1. 사용자가 Unity에서 직접 플레이하고 세 화면의 파티 열, 인벤토리 팝업(10칸), 적이 보이지 않는 노드 선택을 본다(`Assets/@Scenes/Boot.unity`).
2. 사후 검토: Roadmap "사후 검토 대기"의 개정 2~4 항목을 보고 바꿀 것을 정한다. 바꿀 때는 문서(Design/00의 해당 개정 절이 가리키는 곳)를 먼저 고친다.
3. 사후 검토가 끝나면 이 문서를 `completed/`로 옮기고, 9단계(그림)는 시작 지시를 받은 뒤 시작한다. 그림 자리가 세 화면에서 같은 크기이니 그림 요청에 그대로 쓸 수 있다.
   (2026-10-02: G9가 정해졌고 9단계를 시작했다. 9단계의 기록은 `completed/stage09-art.md`가 가진다. 이 문서는 개정 2~4의 사후 검토가 끝날 때까지 active에 둔다.)
