# Round 47 — 전리품을 전투 화면에서 고르기, 아이템 정보는 우클릭

목업이다. 호출 없음. 18단계(몬스터의 전리품)의 화면과 흐름을 고치는 사용자 제안이다.

## 지시

2026-10-08 사용자: "전리품을 전투 후 전투 화면에서 선택하는 것으로 변경. 및 관련 UI 후보 제시(아이템 정보는 마우스 우클릭 툴팁으로 보게)".

읽은 것(해석. 틀리면 판정에서 고친다):

- 이긴 뒤 결과 창 → [계속] → 전리품 화면(Round 46의 타일)의 두 화면을, **전투 화면 하나**로 줄인다. 규칙(무엇이 몇 개 떨어지나, 칸에 놓기·합치기·밀어내기, 두고 가기)은 그대로다.
- 아이템 정보는 **우클릭 카드**(Round 42의 카드)로 본다. 좌클릭은 고르기와 놓기만 한다.

## 지금

- `BattleScreen.ShowResult`: 화면 전체를 덮는 결과 창(승리, 전사자, "전리품: …", "지역 코인 +N", [로그 보기] [계속]). [계속]을 누르면 `LootScreen`(파티 쪽 + 돌판 위의 타일)으로 간다.
- 적이 모두 쓰러지면 무대의 오른쪽과 **보드 판의 오른쪽 절반(적의 보드 자리)이 빈다**(목업의 바탕 `end_clean`: 임시 PlayMode Test로 결과 창만 감추고 찍었다. Test는 지웠다).
- 아이템 카드는 좌클릭으로 열린다(Round 42). 우클릭은 게임 어디에도 없다(Round 44에서 들이지 않기로 했다).
- 저장: 전투 기록(입력과 확정 시각)은 전투 중에만 있다. 전리품 단계(`PickingLoot`)에서 앱을 닫았다 열면 전투를 재현할 수 없다(`07_SAVE.md`).

## 안 (`mock-compare.png`)

| 안 | 전리품의 자리 | 좋은 점 | 아쉬운 점 |
|---|---|---|---|
| A | 결과 창을 작게 해 무대 위로 올리고 그 안에 전리품 줄 | 지금 결과 창을 그대로 살림 | 무대를 덮는다. 창 안의 줄과 아래 보드 사이를 오간다 |
| **B (권장)** | **적의 보드가 섰던 자리**(보드 판 오른쪽 절반)에, 전리품마다 칸 하나짜리 보드(머리 띠에 떨어뜨린 적의 이름) | "그들의 아이템이 떨어졌다"가 보드의 말로 읽힌다. 왼쪽 내 보드로 옮기는 거리가 짧다. 새 그림 없음 | 머리 띠의 이름은 그 아이템을 들었던 적 하나를 고른다(같은 아이템이면 앞 열의 적) |
| C | 무대 바닥, 적이 쓰러진 자리에 놓인 아이템과 이름 패(디아블로의 땅 위 전리품) | 분위기가 가장 좋다 | 빛·이름 패의 새 그림. 바닥의 작은 아이콘이 덜 읽힌다. 셋이 겹치면 자리 문제 |

공통: 승리의 말(승리·전사자·지역 코인·전리품 수)은 무대 위의 띠(A는 창). 후퇴·배속 버튼은 감춘다. 이긴 뒤 왼쪽 보드는 파티 쪽 모습(밝은 칸, "빈 칸")으로 바뀌어 놓을 자리로 읽힌다.
버튼은 보드 판 오른쪽 아래: [인벤토리에 넣기](고른 것이 있을 때) [로그 보기] [계속](남은 것은 두고 간다). 안내 한 줄: "전리품을 누르면 고릅니다. … 우클릭은 아이템 정보입니다."

B의 흐름(`mock-B-flow.png`): 이긴 직후(정예의 드랍 둘을 꾸밈) → 하나를 고름(놋쇠 칸) → 우클릭 카드(보드 판 위, 아래 꼭지가 그 칸을 가리킴. 파티 쪽 카드의 자리 규칙) → 하나를 주움("주움"으로 남음).

## 판정할 것 (모두 권장안을 적었다)

| # | 결정 | 권장 | 대안 |
|---|---|---|---|
| 1 | 전리품의 자리 | **B** 적의 보드 자리 | A 결과 창 안, C 무대 바닥 |
| 2 | 승리의 말 | **결과 창을 없애고 무대 위 띠**(승리·전사자·코인·전리품 수), 이긴 순간 바로 고를 수 있다 | 결과 창을 먼저 보이고 닫으면 고르기 |
| 3 | 끝내기 | **[계속] 하나**: 남은 것은 두고 간다. 마지막 것을 주워도 화면에 머문다(보드를 보고 [계속]) | 지금처럼 마지막 것을 주우면 바로 지도 |
| 4 | 이긴 뒤의 보드 | **전리품 화면과 같은 조작**(고르기, 칸에 놓기, 밀어내기, 합치기 표시, 보드 사이 옮기기). 앞으로·뒤로와 인벤토리 팝업은 없다(지도에서 한다) | 놓기만 되고 옮기기는 지도에서 |
| 5 | 우클릭 카드의 범위 | **모든 화면**: 아이템이 보이는 곳(보드, 전투의 아군·적 칸, 전리품, 상점 타일, 인벤토리 팝업의 줄)에서 우클릭 = 카드, 좌클릭 = 고르기만(Round 42의 "좌클릭 = 고르기 + 카드"를 바꾼다) | 전투 화면만 / 전리품만 |
| 6 | 이어하기 | 전리품을 줍다 앱을 닫으면 **전투 화면을 "이긴 뒤" 모습으로** 다시 연다: 파티는 지금 HP로 서 있고 적은 없으며 전리품은 저장된 그대로. 띠는 "N층 · 전리품"(전사자·코인 줄과 [로그 보기]는 없다: 저장하지 않는 것이다). **저장 형식은 그대로**(9) | 전투 기록을 전리품 단계까지 남겨 재현(저장 10, 무겁다) |
| 7 | 전리품 화면 | **지운다**(`LootScreen`, Prefab, `ScreenId.Loot`). `GamePhase.Loot`의 화면은 전투 화면 | 이어하기용으로만 남김 |
| 8 | 범위 | **Round 47로 18단계의 화면과 흐름을 고친다**. Design/03 §5의 줍는 방법 문구, Architecture/11·12, 07(이어하기의 화면)을 같은 변경에서 고친다 | 새 단계(19) |

## 승인 뒤 할 일

- 기획: Design/03 §5(전투 화면에서 줍기, [계속], 마지막 것을 주워도 머묾, 앞으로·뒤로는 지도에서), 00 "Slice B" 표.
- 화면: `BattleScreen`의 이긴 뒤 상태(띠, 보드의 파티 쪽 모습과 조작, 오른쪽 절반의 전리품 보드, 버튼), 엔진 없이 여는 이어하기 모습, 우클릭(`PointerPress`에 오른쪽 누름, 각 화면의 카드 열기), `LootScreen` 삭제와 `ScreenCatalog`.
- Builder·Test·스크린샷(`_06`·`_07`을 새 장면으로), 문서(Architecture/07·11·12), Roadmap·Handoff.

## 판정

2026-10-08 사용자: **"C안으로 가보자. 나머지에 따라 네 권장안으로 구현"** → 1은 **C**(무대 바닥의 전리품), 2~8은 권장안(결과 창 대신 띠, [계속] 하나, 노드 맵과 같은 보드 조작, 우클릭 카드는 모든 화면, 이어하기는 전투 화면의 "이긴 뒤" 모습, 전리품 화면 삭제, Round 47로 18단계의 화면과 흐름을 고침).

## 구현 (2026-10-08)

- **흐름**(`ExpeditionManager`): `LootOpen` = 원정이 `PickingLoot`이고 전투가 없거나 끝남. 전리품의 질의·명령(`CanTakeLoot`·`TakeLoot`·`…ToInventory`·`LeaveLoot`·`LootMergesAt`·`BattleLoot`)과 아이템 옮기기(`IsBetweenBattles`)가 `LootOpen`을 본다. `GamePhase.Loot`은 앱을 닫았다 연 뒤에만 남고 그 화면은 전투 화면(`ScreenCatalog.ForPhase(Loot)`).
- **전투 화면**(`BattleScreen`): 이긴 뒤(`EnterAfterWin`) 헤더의 후퇴·정지·배속을 감추고, 띠(`VictoryBand`: 승리 · 전사자 · 코인 · 전리품 N, 아래에 사망 문장)를 띄우고, 아군의 전투 보드를 감추고 그 열에 노드 맵의 보드(`PartyBoardView`)를 세우고, 적의 1·2·3열 Column 가운데 바닥에 드랍(`LootDropView`)을 놓고, 패널의 오른쪽에 안내와 [인벤토리에 넣기] [로그 보기] [계속].
  드랍 좌클릭 = 고르기(다시 누르면 풀기), 칸 좌클릭 = 고른 드랍 넣기 또는 노드 맵처럼 집기·옮기기, 우클릭 = 카드(드랍은 아이콘 왼쪽, 칸은 패널 위). 주운 드랍은 사라진다. 마지막 것을 주워도 머문다. [계속] = `LeaveLoot`(남은 것) + `CloseBattle` + 다음 화면.
  진 전투·후퇴·보스전의 승리는 결과 창 그대로(전사자 설명과 로그).
- **이어하기**: 전투 세션 없이 `Loot` 단계로 열리면 파티로 엔진을 새로 세워(`BuildBattleSetup`, 돌지 않음) 아군만 그리고 적은 만들지 않는다. 띠는 "전리품"(`Loot.Title`)만, [로그 보기]는 없다. 저장 형식 9 그대로.
- **우클릭**(`RightClick`: `IPointerClickHandler`로 오른쪽 버튼만 받는 조각. 버튼 옆에 살고 버튼이 꺼져도 받는다): 파티 쪽의 칸(`ItemSlotView`), 전투의 칸(`BattleItemView`, `BattleBoardView.ItemRightClicked`), 상점 타일(`ItemTileView`), 인벤토리의 줄(`InventoryEntryView`), 바닥의 드랍. `PointerPress.Poll`이 오른쪽 누름도 읽어 열린 카드를 닫는다.
  좌클릭의 카드는 모두 뺐다(파티 쪽의 고르기, 전투의 칸). 상점 타일의 카드는 타일의 오른쪽(창 안), 인벤토리의 줄은 줄의 왼쪽(팝업 안), 드랍은 아이콘의 왼쪽(전장 안)에 `ShowBeside`(합치기 안내를 함께).
- **보드의 분리**: `PartyColumnView`의 보드 부분을 `PartyBoardView`로 떼어 냈다(머리 띠·칸·피로 합계, `CellClicked`·`CellRightClicked`). 노드 맵의 열은 그것을 위임해 쓰고(`Slots`·`BoardHeight`·`FatigueTotal` 그대로), 전투 화면의 아군 Column마다 하나씩 숨겨 둔다(`Loot1..4Board`).
- **지운 것**: `LootScreen`·`LootCardView`·`UiPrefabSetup.Loot`·`LootScreen.prefab`·`ScreenId.Loot`·Addressables의 `loot-screen`, 문구 `Loot.Hint`·`Take`·`Taken`·`Leave`, `Battle.Loot`("전리품: …"). 새 문구 `Battle.LootCount`·`LootHint`·`LootPickedHint`.
- **권장안으로 정한 세부**(사후 검토): 드랍의 자리는 적의 1·2·3열 Column 가운데(누구의 것이었는지는 고르지 않는다: `ItemOffer`에 없다), 주운 드랍은 사라진다("주움" 없음), 띠의 사망 문장(결과 창의 것을 띠 아래에), 보스전의 승리는 결과 창 그대로, 이어하기 모습의 시계는 0초, 정비 단계의 보드도 우클릭 카드를 연다(Round 42의 "열지 않는 곳"에서 뺌), 포션은 우클릭 없음.
  드랍의 빛과 판은 도형(그림은 지시가 있을 때). 아군 보드가 전투의 것에서 노드 맵의 것으로 바뀌는 순간 칸의 모양이 바뀐다(어두운 쿨다운 칸 → 밝은 칸: 그것이 "이제 놓을 자리"라는 신호).
- 문서: Design/03 §5(【확정】 Round 47)·00, Architecture/07(이어하기)·09·11(`Battle`·`Loot` 행)·12(화면의 역할, 파티 쪽, 툴팁, 전투 화면의 "이긴 뒤", Test, Deferred), Roadmap, Handoff b18.
- Test: EditMode `TheLoot_IsOpenWhileTheWonBattleStillShows_…`(새), `ADrop_CanGoStraightToTheInventory…`의 `BattleLoot` 단언; PlayMode `Loot_AfterTheWin_…` 둘(새), `PartySide_RightClickingAnItem_…`, `Battle_RightClickingAnItem_…`, 한 바퀴와 인벤토리 Test, `UiSaveTests.Continue_AfterTheAppWasClosedOverTheLoot_…`(새), 스크린샷 `_06_battle_won`·`_07_loot_picked`(옛 `_06_battle_result`·`_07_loot` 대신), `_40`은 우클릭. `UiTestUtil.RightClick`·`EndBattle`·`ContinueAfterBattle`.

## 검증 (2026-10-08)

- 체인 `20261008-184709`(setup editmode): setup OK(Prefab 둘(Battle·NodeMap)과 Stamp 다시, 문구 표, 새 .cs 넷의 .meta, `LootScreen.prefab` 없음, Addressables의 `loot-screen` 항목 삭제), EditMode **752/752**(새 `TheLoot_IsOpenWhileTheWonBattleStillShows_…` 포함). 그 전 한 번은 스크린샷 Test의 지역 변수 이름 충돌로 컴파일 오류 → 고침.
- 체인 `20261008-184931`(sim playmode): sim OK, PlayMode 87 중 2 실패 — 모두 Test의 경로(`Party1Cells/…`·`Party1BoardHead/…`가 새 감싸개 `Party1Board/` 아래로) → 고침. 혼자 돌린 `single-r47`에서 2열의 경로 하나가 더 나와 고침.
- 체인 `20261008-190447`(playmode): **78/87**(9 skipped: 스크린샷의 Explicit). 새 PlayMode Test 셋(이긴 뒤의 띠·드랍·합치기, 보드 사이 옮기기와 [계속], 이어하기)이 모두 들어 있다. **CHAIN OK**.
- 스크린샷 `20261008-r47b`: 9/9, PNG 71(`_06_battle_result`·`_07_loot` 대신 `_06_battle_won`·`_07_loot_picked`. 첫 장은 연출이 가라앉게 1.5초 기다린 뒤 찍는다). 구현 장면은 `game/`(이긴 직후, 드랍을 고름 ko·en, 보스전의 우클릭 카드). `Assets/InitTestScene*` 없음, `git diff --check` 깨끗, ProjectSettings·Scene 변경 없음.
