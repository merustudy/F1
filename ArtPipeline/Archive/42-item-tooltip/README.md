# Round 42 — 아이템 클릭 툴팁: 누른 아이템의 정보를 카드로

목업이다. 호출 없음. 16단계(맞추기)의 직접 플레이에 앞서 사용자가 더한 검토 항목이다.

## 지시

2026-10-07 사용자: "추가 검토 사항 — 아이템 왼쪽 클릭시 툴팁 나오면서 아이템 정보 나오게(백팩 배틀즈처럼). 툴팁 나온 후 아무 곳이나 클릭시 툴팁 사라짐. 검토 후 목업 이미지 제공".

## 지금

- 파티 쪽(노드 맵·보상): 칸을 누르면 그 아이템을 **고른다**(놋쇠 칸. 옮기기·합치기의 첫 걸음)와 함께 **패널 오른쪽 아래의 설명 줄**에 사실이 한두 줄로 적힌다
  ("단검 · 등급 8 — 무기 장비 / 크기 1칸 / 쿨다운 1.5초 / 앞에서 2번째 자리까지만 발동 / 맨 앞 적에게 피해 3 / 전투마다 피로 +1", 합칠 것이 있으면 "같은 단검 위에 놓으면 합쳐서 동 하나가 됩니다."). 아이템에서 멀고 한 줄에 몰려 있다(`PartySideView._detail`, `UiText.ItemTitle`·`ItemSummary`·`MergeHint`).
- 전투: 보드의 아이템은 눌리지 않는다. 아이템 정보를 볼 길이 없다(보드 클릭은 포션의 대상일 때만).
- 인벤토리 팝업의 줄과 보상 카드는 제목과 사실을 이미 적는다. 야영지 정비의 판은 고른 아이템의 변화를 적는다.
- Architecture/12 "Deferred"에 "툴팁"이 있었다. 이 요청으로 Deferred에서 뺀다(구현할 때 12에 "툴팁" 절을 둔다).

## 레퍼런스 (작품의 자산은 쓰지 않는다. 특징만 글로 읽는다)

| 게임 | 열리는 때 | 내용 (확인한 것) |
|---|---|---|
| Backpack Battles | **마우스를 올리면** 아이템 옆에 카드. 클릭은 집어 드는 것(끌기). Alt를 누른 채면 레시피 툴팁, Alt 두 번이면 툴팁이 **고정**돼 그 안을 다시 가리킬 수 있다. R로 회전 | 이름(희귀도 색), 희귀도·종류, 무기는 피해·스테미나·명중·쿨다운·소켓, 효과 글, 시너지(별·다이아) 표 |
| The Bazaar (기억) | 마우스를 올리면 카드 옆에 상세 | 이름·단계·태그, 쿨다운, 효과 글 |
| Darkest Dungeon (기억) | 마우스를 올리면 | 장신구의 이름(희귀도 색), 효과의 줄, 설명 |

출처: [Steam 가이드: Game Basics, Clarifications](https://steamcommunity.com/sharedfiles/filedetails/?id=3187896204), [Backpack Battles Wiki: Item infobox](https://backpackbattles.wiki.gg/wiki/Template:Item_infobox), [Backpack Battles Wiki: Cooldown](https://backpackbattles.wiki.gg/wiki/Cooldown), [Backpack Battles Wiki: Game Mechanics](https://backpackbattles.wiki.gg/wiki/Game_Mechanics).

읽은 것: 레퍼런스는 모두 **올리면(hover)** 열린다. 우리 조작은 "누르고 누르기"(터치도 되게)라 사용자의 요청대로 **누르면** 열리고 **다음 클릭에 닫히는** 쪽이 맞다 — Backpack Battles의 "고정된 툴팁"에 가깝다.
클릭이 이미 "고르기"에 쓰이므로 둘을 겹친다: 누르면 지금처럼 고르고(파티 쪽) 카드도 열린다. 전투에서는 고르기가 없어 카드만 열린다.

## 안 — 카드의 모양 (`mock-compare.png`: 파티 쪽, `mock-detail.png`: 2배)

카드의 글은 지금 설명 줄의 것 그대로를 줄마다 나눈 것이다: **제목**(이름 · 단계 · 등급. 단계는 그 색), **사실**(분류 / 크기 / 쿨다운 / 자리. 흐린 글), **효과**(줄마다), **피로**(연보라 "전투마다 피로 +1", 기본 무기는 흐린 "기본 무기: 피로 없음"), 합칠 것이 있으면 **합치기 안내**. 폭 400. 새 문구는 없다.

| 안 | 모양 | 읽은 것 |
|---|---|---|
| **1. 먹색 판 + 놋쇠 선 (권장)** | 먹색(`Ink` 94%) 둥근 판에 놋쇠 가는 선, 단계가 있으면 왼쪽 끝에 단계 색의 띠(보상 카드의 띠와 같은 어법), 패널 위에 뜰 때는 아래에 작은 꼭지 | 디아블로 툴팁의 어법(검은 판에 금선). 가볍고 글이 잘 읽힌다. 어느 화면에서나 같다 |
| 2. 돌 패널 카드 | 디아블로 키트의 돌 패널(`panel`, 타일)을 카드로. 제목 밑줄이 단계 색 | 기존 패널과 같은 재질이라 "창"처럼 읽힌다. 무겁고 테가 두꺼워 작은 카드에는 과하다 |
| 3. 쇠 명패 머리 + 먹색 몸 | 헤더의 금선 쇠 명패(`plate_label`)에 이름을 금색으로, 아래 먹색 몸에 단계·등급과 사실 | 가장 꾸민 모양. 이름이 커 보이지만 카드가 50 더 높고, 명패를 폭에 맞춰 늘여야 한다 |

## 안 — 카드의 자리 (`mock-place.png`, 전투는 `mock-battle.png`)

| 자리 | 파티 쪽(노드 맵·보상) | 전투 |
|---|---|---|
| **P1 (권장)** | **보드 패널 위**(패널 위 8), 누른 칸의 열에 가운데를 맞추고 화면 끝에서는 안쪽으로. 아래 가운데의 꼭지가 그 열을 가리킨다. 그 열 용병 그림의 아래쪽과 앞으로·뒤로를 잠시 덮지만 **보드, 합치기 표시, 패널 오른쪽(설명 줄·버튼)은 그대로** | 패널 가운데는 양초라 비켜 **아군의 카드는 보드 왼쪽, 적의 카드는 보드 오른쪽**(누른 칸의 높이, 패널 안) |
| P2 | 칸 바로 옆(Backpack Battles처럼). 1열이면 패널 오른쪽의 설명 줄과 "인벤토리로"를, 4열·3열이면 옆 보드와 "합치기 →" 표시를 덮는다 — 고른 뒤 바로 눌러야 할 칸을 가려 한 번 더 눌러 닫아야 한다 | 같은 자리(옆) |

## 동작 (`mock-flow.png`)

1. 아이템 칸을 누르면 카드가 열린다. 파티 쪽에서는 지금처럼 **그 아이템을 고르는 것도 그대로**(놋쇠 칸, 합칠 칸의 표시, 설명 줄).
2. **다음 클릭(어디든, 누르는 순간)에 카드가 닫힌다.** 그 클릭이 칸·버튼이면 그 일(옮기기·합치기·인벤토리로·인벤토리 보기·전투 시작…)은 그대로 된다. 카드 자체는 클릭을 받지 않는다(카드 위를 눌러도 닫힌다).
   고른 아이템을 다시 누르면 지금처럼 고른 것이 풀리고 카드도 닫힌다. 다른 아이템을 누르면 그 카드가 열린다(한 번에 하나).
3. 바깥 클릭은 **카드만** 닫는다. 고른 것은 지금처럼 남고 설명 줄이 그것을 말한다(그래서 설명 줄은 그대로 둔다. 카드가 열린 동안은 같은 사실이 두 곳에 있지만 카드는 잠깐이다).
4. 전투: 아군·적 보드의 아이템 모두. 전투는 멈추지 않는다. 포션을 든 동안(아군 보드가 포션의 대상일 때)에는 열리지 않는다. 적의 아이템 정보는 싸우는 중에 드러나는 것이라 Design/03 §1의 "미리 보여 주지 않음"과 어긋나지 않는다.
5. 열지 않는 곳: 보상 카드(카드 자체가 정보), 인벤토리 팝업의 줄(줄이 정보), 야영지 정비 단계의 보드(정비 판이 변화를 말한다), 로비(아이템이 없다).

## 권장

**안 1 + 자리 P1 + 위 동작.** 사용자가 바란 것은 "정보가 아이템에서 바로 나오는 것"이고, 안 1은 그 정보를 디아블로 툴팁의 어법으로 가볍게 보이면서 패널의 돌·쇠와 다툼이 없다.
자리는 눌러야 할 것(보드, 패널 오른쪽)을 가리지 않는 P1이 "다음 클릭에 닫힘" 규칙과 맞물린다(가리면 닫는 클릭이 한 번 더 든다).

## 고르면 (구현 계획)

- **View**: `ItemTooltipView`(Prefab의 자식, 화면마다 하나. `UiPrefabSetup`이 코드로 만든다: 먹색 판 Image + 놋쇠 선 + 단계 띠 + TMP 글 넷(제목, 사실, 효과·피로, 합치기)). 글은 `UiText.ItemTitle`·`ItemDetails`·`MergeHint`를 그대로 쓴다(새 문구 없음).
  `Show(EquippedItem, RectTransform cell, Placement)`와 `Hide()`. 클릭을 받지 않는다(`raycastTarget` 끔).
- **자리**: 순수 함수 `TooltipPlacement.Above(cellRect, cardSize, panelTop, screen)`·`Beside(cellRect, cardSize, side, panel)`(EditMode Test). 파티 쪽은 Above, 전투는 Beside(아군 왼쪽·적 오른쪽).
- **열기**: `PartySideView.OnCellClicked`에서 고른 뒤(옮기기·합치기로 끝나는 클릭이 아닐 때) `ShowTooltip`; `BattleScreen`은 `BattleItemView`의 칸에 Button을 두고(포션을 들지 않았을 때만 눌림) 누르면 Show.
- **닫기**: 화면의 `Update`에서 "이번 프레임에 포인터가 눌렸으면" Hide(눌림은 `Input.GetMouseButtonDown(0)`/터치 시작). 누르는 순간 닫히고, 그 클릭의 버튼은 떼는 순간 제 일을 하므로 같은 아이템을 다시 누르면 닫혔다 다시 열린다(고른 것이 풀리는 클릭이면 `ClearSelection`이 Hide).
- **Test**: PlayMode — 칸을 누르면 카드가 보이고 제목이 아이템과 같다, 다른 곳을 누르면 사라진다(고른 칸은 그대로), 전투에서 적의 칸을 누르면 카드가 보드 오른쪽에 있다, 포션을 든 동안은 열리지 않는다. EditMode — 자리의 산술(화면 끝의 밀림, 패널 안의 머묾).
- **문서**: Architecture/12 "Deferred"에서 툴팁을 빼고 "툴팁(아이템 카드)" 절, Test 목록. Roadmap 16단계(Round 42)와 사후 검토 대기(권장안으로 정한 세부), Handoff.
- 스크린샷: `_08_map_item_selected`(카드가 열린 모습), 전투의 카드 장면 하나.

## 판정할 것

1. 카드의 모양: 안 1 / 2 / 3.
2. 카드의 자리: P1(패널 위, 전투는 보드 옆) / P2(칸 옆).
3. 동작: "고르기 그대로 + 카드, 바깥 클릭은 카드만 닫음"(권장) / 바깥 클릭이 고른 것도 푸는지.
4. 전투의 적 아이템도 카드로 보는지(권장: 본다).
5. 열지 않는 곳(보상 카드, 인벤토리 줄, 정비 단계)은 권장대로인지.

## 판정 (2026-10-07)

사용자: **"권장안 구현"** → 안 1(먹색 판 + 놋쇠 선) + 자리 P1(파티 쪽은 패널 위, 전투는 보드 옆) + 동작(고르기 그대로 + 카드, 다음 클릭에 카드만 닫힘) + 전투의 적 아이템도 카드 + 보상 카드·인벤토리 줄·정비 단계에는 없음.

## 구현 (2026-10-07)

- **View** `ItemTooltipView`(`@Scripts/UI/Views`): 먹색 판(`Ink` 94%, 모서리 4)에 놋쇠 선(92%), 단계 띠(6, `UiPalette.TierMark`), 글 다섯(제목 24 / 사실 19 흐림 / 효과 20 / 피로 19 / 합치기 19. VerticalLayoutGroup + ContentSizeFitter로 높이가 글에 맞음),
  꼭지(놋쇠·먹 마름모 둘을 RectMask2D로 아랫부분만. 파티 쪽에서만). `ShowAbove`·`ShowBeside`·`Hide`, Test용 `Placed`·`Anchor`·`ShownFrame`. 글은 `UiText.ItemTitle`, 새 `UiText.ItemCard`(사실·효과·피로를 설명 줄과 같은 조각으로), `UiText.MergeHint`. 새 문구 없음.
- **자리** `TooltipPlacement`(`@Scripts/UI`, 순수 함수. 화면 왼쪽 위 기준, y 아래로): `Above`(패널 위 8, 열 가운데, 화면 끝 12 안, 꼭지는 모서리에서 16 안), `Beside`(보드 옆 10, 칸의 높이, 패널 안 위 6·아래 12, 화면 안).
- **누름** `PointerPress`: `Poll()`이 Input System의 `Mouse.current.leftButton`·`Touchscreen.current.primaryTouch.press`의 `wasPressedThisFrame`을 읽어 마지막 누름의 프레임을 기억한다. 화면은 카드가 열린 프레임보다 뒤의 누름이면 닫는다(같은 클릭으로 열린 카드는 남는다). Test는 `Simulate()`.
  `F1.Runtime.asmdef`에 `Unity.InputSystem` 참조를 더했다(Architecture/03).
- **파티 쪽** `PartySideView`: `_tooltip`·`_boardPanel`. `OnCellClicked`에서 고르기로 끝난 클릭이면 `Refresh` 뒤 `ShowTooltip`(그 칸의 `ItemSlotView`를 열에서 찾아 그 위에, 합칠 것이 있으면 안내). `Update`에서 누름이면 `Hide`. `ClearSelection`과 `Open`에서 `Hide`.
- **전투**: `BattleItemView`에 칸의 `Button`(Builder가 `MakeButton` + 소리 없음 + 틴트 없음)과 `SetClickable`(interactable과 raycast), `BattleBoardView.ItemClicked`·`SetItemsClickable`, `BattleScreen.OnItemClicked`(아군 왼쪽·적 오른쪽, 클릭 소리. 적의 카드에는 피로 줄이 없다 — 적에게는 피로가 없다. 포션을 들었거나 결과가 떴으면 열지 않음), `Render`가 "진행 중이고 포션을 들지 않았을 때"만 칸을 클릭 가능하게, `Update`의 누름과 `ShowResult`에서 `Hide`.
- **Builder** `UiPrefabSetup.BuildItemTooltip`(Kit)과 파티 쪽·전투의 배선(`_tooltip`, `_boardPanel`), 전투 칸의 Button.
- **Test**: `TooltipPlacementTests`(EditMode 4), `UiFlowTests.PartySide_ClickingAnItem_OpensItsCardAboveThePanel_AndTheNextPressClosesOnlyTheCard`, `Battle_ClickingAnItem_OpensItsCardBesideTheBoard_PartyLeftEnemyRight_NotWhileAPotionIsArmed`, `UiTestUtil.Click`이 누름을 흉내 내고 `PressTheBackground`가 바깥 누름. 스크린샷 `_40_battle_item_card`(보스전의 적 카드), `_08_map_item_selected`(파티 쪽의 카드).
- **문서**: Architecture/12 "툴팁: 아이템 카드"(조작 줄, Test 목록, Deferred에서 툴팁을 뺌), 03(asmdef 참조), Roadmap 16단계와 사후 검토 대기("Round 42의 세부"), Handoff.

## 검증 (2026-10-07)

- 체인 `20261007-090545`(setup·sim·editmode): setup OK(Prefab 셋과 Stamp 다시), sim OK, EditMode 730/731 — 실패 하나는 `TooltipPlacementTests`의 기대값 오류(화면 안의 칸은 꼭지 clamp에 닿지 않는다) → Test를 고침.
  체인 `20261007-090718`(editmode·playmode): EditMode **731/731**, PlayMode **75/75**(9 skipped).
- 스크린샷 `20261007-r42`에서 적의 카드에 "전투마다 피로 +1"이 적히는 것을 보고 적의 카드에서 피로 줄을 뺐다(`ShowBeside`의 `withFatigue`). 다시 체인 `20261007-092254`: EditMode 731/731, PlayMode 74/75 —
  실패 `Battle_AnEnemyTheStormKills_IsNoKillMoment`는 긴 전투에서 용병이 붕괴해(16단계에 피격 피로가 2가 되며 더 자주) 무대가 느려진 것을 결정타의 느림으로 읽은 것 → 붕괴의 순간이면 느림 검사를 건너뛰게 고침. 마지막 체인 `20261007-094933`(playmode): **75/75**(9 skipped).
- 스크린샷 `20261007-r42c`: 9/9, PNG **65**(두 Locale × `_40_battle_item_card`가 늘었다). `game/ko_08_map_item_selected_implemented.png`(파티 쪽: 1열 롱소드의 카드가 패널 위, 꼭지가 로언의 열을 가리킴, "기본 무기: 피로 없음"),
  `game/ko_40_battle_item_card_implemented.png`(보스전 시작: 감독관의 망치 카드가 적 보드 오른쪽, 피로 줄 없음). 카드 장면은 보스전 **시작 직후**에 찍는다(30초 뒤에는 런의 시드에 따라 적이 다 죽어 있을 수 있다 — 첫 한국어 실행이 그래서 멈췄다).
- Render 설정 Asset은 바뀌지 않았다. 죽인 스크린샷 실행이 남긴 `Assets/InitTestScene*`은 지웠다.

## 목업 6: 전투 카드의 화살표 (사용자 지시, 2026-10-07. `mock_battle_arrow.py` → `mock6-battle-arrow.png`, `mock6-detail.png`)

사용자: "전투화면 아이템 툴팁에도 화살표가 있는 것으로 목업 제공". 구현된 보스전 장면(`game/ko_40_battle_item_card_implemented.png`, 아군 넷·적 셋) 위에 그렸다.

| 안 | 모양 | 읽은 것 |
|---|---|---|
| 지금(구현) | 보드 옆 10, 꼭지 없음 | 적 카드는 보드 바로 옆이라 관계가 읽히지만, 아군 넷일 때 아군 카드는 이웃 보드 위에 서서 어느 칸의 것인지 흐리다 |
| **A. 옆 꼭지 (권장)** | 자리는 그대로, 카드 옆면(보드 쪽)에 먹색 세모(놋쇠 테, 12 튀어나옴, 밑변 20)가 누른 칸의 세로 가운데에 선다. 카드가 화면 끝에 밀려 **자기 보드를 덮게 되면 보드의 반대쪽**으로 넘어간다(4열의 아군 카드) | 카드가 누른 칸 옆에 붙어 "이 아이템의 카드"가 바로 읽힌다. 아군 넷이면 아군 카드가 이웃 보드 둘(3열·2열)을 잠시 덮는다 — 전투에서 보드는 누를 일이 없으니(포션의 대상은 보드 전체) 읽는 동안의 가림이다. 구현은 `TooltipPlacement.Beside`에 반대쪽 넘김과 꼭지의 y를 더하는 것 |
| B. 위 + 아래 꼭지 | 파티 쪽과 같은 자리: 패널 위 8에, 그 칸의 열에 가운데를 맞추고 꼭지가 아래로 칸을 가리킨다 | 보드를 덮지 않고 규칙이 하나가 된다(파티 쪽 = 전투). 대신 무대의 유닛 아래쪽과 **HP 막대**를 잠시 덮고, 카드가 칸에서 멀다. 구현은 전투에서 `ShowAbove`를 부르는 것뿐 |

전투의 보드는 아군 120~870, 적 1050~1800이고 사이는 양초라, 폭 400의 카드가 보드를 덮지 않고 설 자리는 패널 위(무대)뿐이다. 어느 안이나 카드는 클릭을 받지 않고 다음 클릭에 닫힌다.

권장: **A**. 사용자가 바란 것은 "카드가 어느 아이템의 것인지"이고, 옆 꼭지가 그것을 카드가 선 자리에서 바로 말한다. 전투 중에 HP 막대를 가리는 B보다 이웃 보드를 잠시 가리는 쪽이 낫다. 지금 구현에서 바뀌는 것은 꼭지와 "자기 보드를 덮으면 반대쪽" 규칙뿐이다.

## 판정 2 (2026-10-07): 목업 6은 안 A — 구현

사용자: **"권장안 구현"** → 전투 카드의 보드 쪽 옆면에 꼭지(누른 칸의 세로 가운데, 모서리에서 16 안), 카드가 화면 끝에 밀려 자기 보드를 덮게 되면 보드의 반대쪽으로.

- `TooltipPlacement.Beside(cell, size, left, panel, screen, out notchY, out cardIsLeftOfCell)`: 바라는 쪽의 x를 화면 안으로 민 뒤 그 카드가 칸의 가로 범위와 겹치면 반대쪽으로 다시 놓는다. 꼭지의 y는 칸의 가운데에서 카드의 위까지(16 ~ 높이 − 16).
- `ItemTooltipView`: 꼭지 셋(`_notchBottom`·`_notchLeft`·`_notchRight`)을 한 번에 하나만 켠다(`Notch` None·Bottom·Left·Right, `NotchAt`). `ShowBeside`는 카드가 칸의 왼쪽이면 오른쪽 꼭지, 오른쪽이면 왼쪽 꼭지.
- Builder: `NotchClip`(RectMask2D 안의 마름모 둘)이 아래·왼쪽·오른쪽 꼭지를 만든다. 옆 꼭지의 클립은 카드 옆면에 1 겹쳐 가는 선을 덮고 바깥 12만 보인다.
- Test: `TooltipPlacementTests` 다섯(옆 꼭지의 y, 반대쪽 넘김 — 4열·3열은 오른쪽, 오른쪽 끝의 적은 왼쪽 —, 패널 안에서 꼭지가 칸을 따라감), PlayMode 둘(파티 쪽은 아래 꼭지가 칸의 가운데, 전투는 보드 쪽 옆 꼭지와 자리가 없을 때의 반대쪽).
- 검증: 체인 `20261007-133735`(setup·sim·editmode): setup OK, sim OK, EditMode 731/732 — 실패는 새 Test의 기대값 오류(높이 900의 카드가 패널 위에 걸릴 때 꼭지의 y는 404로 clamp에 닿지 않는다) → 고침. 체인 `20261007-133822`(editmode·playmode): EditMode **732/732**, PlayMode **75/75**(9 skipped).
- 스크린샷 `20261007-r42d`: 9/9, PNG 65. `game/ko_40_battle_item_card_implemented.png`를 옆 꼭지가 붙은 장면으로 바꿨다(감독관의 망치 카드, 왼쪽 옆면의 꼭지가 칸의 가운데 높이). `ko_08`도 같은 실행의 것으로.

## 목업 7: 안 3 다시 — 단계의 명패 (사용자 지시, 2026-10-07. `mock_plate.py` → `mock7-compare.png`, `mock7-tiers.png`, `mock7-plates.png`)

사용자: "툴팁을 처음 제안했던 안 3로 하는 방향으로 재검토. 제목 칸을 등급에 따라 차별 테두리(필요시 이미지 생성), 목업에서 보여준 테두리는 금 등급으로, 화살표는 기존 것 사용. 목업 제공, 승인 후 구현. 현재 툴팁과 비교 목업".
"등급"은 단계(일반·동·은·금)로 읽었다(숫자 등급 8·10마다의 테두리가 아니다).

| | 지금(안 1, 구현됨) | 안 3 다시 |
|---|---|---|
| 제목 | 먹색 판 위 "이름 · 단계 · 등급" 한 줄, 단계는 왼쪽 띠 | **단계의 명패**에 이름(가운데, 단계의 글 색). 명패는 지금 헤더의 금선 쇠 명패(`plate_label`)이고 **선의 금속만 단계마다 다르다**: 금 = 그대로, 은 = 강철, 동 = 구리, 일반 = 장식 없는 무쇠(어둡고 채도 없음). 명패 아래 "단계 · 등급"(일반은 "등급"만) |
| 몸 | 사실 / 효과 / 피로 / 합치기 안내 | 같다. 왼쪽 띠는 뺐다(명패가 단계를 말한다) |
| 꼭지 | 파티 쪽 아래, 전투 옆(구현된 것) | 그대로. 전투에서 누른 칸이 카드의 위쪽이면 옆 꼭지가 **명패의 테에 붙는다**(`mock7-compare` 아래 오른쪽). 대안: 꼭지를 명패 아래(위에서 66)로 내리면 칸의 가운데에서 36 어긋난다 |
| 높이 | 글만큼 | 명패 56 + "단계 · 등급" 줄만큼 **약 50 더 높다** |
| 그림 | 없음 | 명패 넷. `plate_label`의 밝은 선 픽셀(채도 70·명도 100 이상)만 색조를 바꾼 것이라 **호출 없음**(`ui_variants.py`에 "선의 색을 바꾸는" 방식을 하나 더 두거나 라운드 스크립트가 만든다). 단계마다 **장식이 다른** 명패를 바란다면 이미지 생성 4회(약 $0.12)와 승인 라운드 |

읽은 것:
- 명패가 단계를 말하니 "틀 = 단계"(The Bazaar의 어법, Round 41 레퍼런스)가 카드에도 이어지고, 이름이 가운데에 서서 제목이 또렷하다. 금 명패는 헤더와 같은 것이라 전투 화면 위아래가 한 식구로 읽힌다.
- 대신 카드가 50 높아져 파티 쪽에서 용병 그림을 더 덮고, 전투에서 패널 아래 끝에 가까운 칸은 카드가 위로 밀린다. 일반(기본 무기·보상 대부분)의 명패는 장식 없는 무쇠라 금·은·동과의 차이가 분명하다.
- 색만 바꾼 명패는 셋이 같은 꼴이라 단계가 **색으로만** 갈린다(글의 단계 말이 남아 색을 못 가려도 읽힌다).

권장(사용자가 안 3 방향을 정했으므로 세부만): **색 변형 명패 넷(호출 없음)**, 명패 아래 "단계 · 등급" 줄 유지, 띠 삭제, 꼭지는 칸의 가운데 그대로(명패 테에 붙는 경우는 작아서 둔다). 장식이 다른 명패는 플레이 뒤 바라면 생성 라운드로.

## 고르면 (구현 계획, 목업 7)

- 조각: `Rosters/ui_variant.csv`에 `plate_tier_common`·`plate_tier_bronze`·`plate_tier_silver`(Source `gothic_plate`, Mode `lines`: 밝은 선의 색조를 바꿈. 금은 `plate_label` 그대로) → `Assets/@Art/UI/Frame/`. `UiArt`에 셋(Border 44).
- View: `ItemTooltipView`에 명패 Image(9-slice, 폭 400·높이 56)와 그 위의 제목(가운데, `UiPalette.TierText`), 아래 "단계 · 등급" 줄(`UiText.TierWord` + `등급 N`), 왼쪽 띠 삭제. 레이아웃은 명패를 첫 줄로(VerticalLayoutGroup의 자식, 높이 56, 좌우 여백 0).
- 문구: 제목이 이름만이라 `UiText.ItemTitle`은 설명 줄용으로 남고, 카드는 `UiText.Name`과 새 한 줄(`Item.TierGrade` "{0} · 등급 {1}" / 일반 `Item.GradeOnly` "등급 {0}")을 쓴다 — 문구 둘이 는다(CSV).
- Test: `UiFlowTests`의 카드 제목 검사를 명패의 이름과 "단계 · 등급" 줄로. `TooltipPlacement`는 그대로. 스크린샷 둘 다시.
- 문서: Architecture/12 "툴팁"(모양), 13(명패 변형), Design/00(결정 한 줄), Roadmap(사후 검토 대기), Handoff.

## 판정할 것 (목업 7)

1. 안 3 다시로 간다 / 지금(안 1) 그대로.
2. 명패: 색 변형 넷(권장, 호출 없음) / 이미지 생성(장식이 다른 명패, 4회).
3. 전투에서 옆 꼭지가 명패 높이에 걸릴 때: 칸의 가운데 그대로(권장) / 명패 아래로.

## 판정 3 (2026-10-07): 목업 7은 채택하지 않음 — 지금(안 1) 유지

사용자: **"지금 유지하자"** → 카드는 구현된 안 1(먹색 판 + 놋쇠 선, 단계는 왼쪽 띠, 꼭지)대로 둔다. 단계의 명패(목업 7)는 기록으로만 남는다.
