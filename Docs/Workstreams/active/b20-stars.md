# Slice B 20단계: ★ 닿음과 숫돌

Snapshot: 2026-10-10 (구현, Round 52~55 구현, 세션 정리에서 커밋)

## Goal

Architecture/09 "Slice B" Acceptance 12가 참이다: 숫돌을 상점에서 사서 근접 무기의 위나 아래에 놓으면 그 별이 켜지고 그 무기의 피해가 숫돌의 단계만큼 오른다(같은 시드와 입력이면 같은 결과).
숫돌은 발동하지 않고 피로가 없다. 숫돌을 든 동안과 마우스를 올린 동안 별 칸에 별이 보이고(켜짐·빈 별), 카드가 강화를 수치로 적는다. 저장 형식은 그대로다.

## State

- 2026-10-10 Round 51(`ArtPipeline/Archive/51-claw-axe-whetstone/README.md`): 쥐 발톱 그림 생성, 전투도끼 3×2 목업과 시뮬, 숫돌·★의 조사와 목업 → 별의 표시는 백팩 배틀즈를 조사해 그대로(`mock-stars-bb.png`)
  → 사용자 **"4. 전투도끼로 바꿈, 그 외 모두 권장안, 백팩배틀즈 조사 확인한 것을 그대로 옮긴 모습을 적용하여 구현"**(Roadmap "20단계의 승인") → 20단계를 열어 구현했다.
- 구현과 문서는 끝났다. 검증은 아래 Verification.
- 2026-10-10 Round 55(`ArtPipeline/Archive/55-loot-vendor/README.md`): 검토(드랍을 인벤토리에, 바닥 아이콘 크기, 디아블로 2 상인과 상점 크기 시뮬) → 사용자 "5. 6. 한 화면·새로고침 / 나머지 권장안대로 / 함께 기획 제한" → 1~4 구현, 상인은 2차 목업과 21단계 기획 제안.
- 2026-10-10 Round 54(`ArtPipeline/Archive/54-held-cursor/README.md`): 사용자 "디아블로 2 스타일 … 검토 후 보고" → 목업·판정 일곱 → **"구현"** → 권장안대로 구현(든 것은 마우스에, 자리를 비움, 놓일 칸은 칠만, 든 동안 누름은 든 것이 먼저).
- 2026-10-10 Round 53(`ArtPipeline/Archive/53-grid-lines/README.md`): 플레이 피드백 → 목업 → 사용자 **"a권장안으로, 인벤토리 아이템 배경색도 용병 보드의 파란색과 같은 색상으로 변경 구현"** → 구현(남색은 칸마다·사이에 회색 선, 그림자는 한 덩어리, 남색은 불투명으로 어디서나 같은 색).
- 2026-10-10 Round 52(`ArtPipeline/Archive/52-inventory-place/README.md`): 검토와 목업 → 사용자 **"3. b안 / 나머지 권장안 구현"** → 구현(등불 지팡이 3×1, 인벤토리 창을 용병 무대 위로·칸 50, 이긴 뒤 화면에도 같은 창, 단축키 I, 이긴 뒤 보드가 마지막 전리품 뒤에 굳던 버그).

## Done

- 기획: Design/02 §4(전투도끼 3×2, ★과 숫돌, 근접 무기, 쥐 발톱 그림), 03 §5(숫돌은 상점에서만), 07(열 셋), 10 §5(별의 표시), 00 "Round 51과 20단계", 08 §17(시뮬). CLAUDE.md §9.
- Architecture: 05(열과 검사, `IsPassive`), 08(`StarRules`, 전투의 +N, 비활성), 09(범위·순서 9·Acceptance 12), 12(별의 표시, 카드), 13(숫돌 도형, 쥐 발톱, 전투도끼 30°).
- 데이터: `ItemData.csv`의 `Melee`·`Stars`·`StarDamage`, `whetstone`(기타 1×1, 별 `0:-1+0:1`, `StarDamage` 1, 값 8·가중치 8), `greataxe` 전투도끼/Battle Axe 3×2, Generated. 그림: `rat_bite`(새 발톱), `greataxe`(30°), `whetstone`(도형).
- 코드: `StarSquare`·`ItemData`(검사, `IsPassive`), `CsvRow.StarSquares`, `ItemMapper`(효과 없는 줄), `StarRules`(`StarSquares`·`Marks`·`DamageOn`·`Sources`·`Lit`·`DamageInReadingOrder`), `EquippedItem.StarDamage`,
  `BattleUnitSetup.StarDamage`, `BattleItemState.StarDamage`, 엔진(`Works`: 효과 없는 아이템은 늘 비활성, 무기 피해에 +N을 패시브 곱 앞에), `ExpeditionRules.AddPartyRow`.
  시뮬: `Policies.MemberWithStarRoomFor`, 보고의 별 줄. 화면: `UiPalette`의 별 색, `GridStarView`, `GridBoardView`의 별 층(`Stars`·`StarAt`), Builder `BuildGridStar`, `BoardHand.StarsFor`·`HoveredStarItem`,
  `PartyBoardView`·`PartyColumnView`·`PartySideView`·`BattleScreen`(별의 배선, 마우스가 별 아이템에 오르내릴 때만 다시 그림, 카드의 별 줄), `ItemTooltipView`(`starLine`), `UiText`(`StarLine`, "근접", "발동하지 않음", 별의 효과 줄), `BattleItemView`(늘 다 밝음), 문구 여섯(`UiKeys.Item`).
- Test: EditMode `StarRulesTests`(일곱), 엔진 셋(+N과 패시브, 발동하지 않음, 길이 검사), 데이터(`ItemData` 검사, CSV의 별·효과 없는 줄과 잘못된 별 넷, 출하 데이터의 숫돌·근접·전투도끼), `ItemSoundTests`(기타 아이템은 발동하지 않음).
  PlayMode: 숫돌의 별(마우스·든 동안·카드), 출하 데이터로 꾸미던 Test 열이 기본 무기가 한 줄인 용병의 보드를 씀(`UiTestUtil.FlatRow`: Test 파티의 1열은 발키리), 상점 Test는 숫돌을 고르지 않음, 스크린샷 `_46_map_whetstone_stars`·`_47_map_star_card`.
- Round 52: 데이터 `lantern_staff` 너비 3, `InventoryWindowView`(두 화면이 함께 씀)와 Builder `BuildInventoryPopup`(무대의 그늘, 창 944×350, 칸 50), `PartySideView`의 창, 이긴 뒤 화면의 `LootInventoryToggle`·창·격자 배선, `InventoryKey`(I)와 두 화면의 `PressInventoryKey`, 문구 셋("(I)"), `ExpeditionManager.WonBattleOnShow`. 문서 Design/00·02·03·08 §18·10 §5, Architecture/11·12. Test: EditMode 둘(마지막 전리품 뒤의 보드, 진 전투), PlayMode 셋을 고침(마지막 전리품 뒤 옮기기, 이긴 뒤 창, 노드 맵 창의 자리·칸 50·I), 스크린샷 `_48_battle_won_inventory`.
- Round 54: `GridGeometry.Anchor`(가운데 겨눔)·`PointIn`·`Centre`, `BoardHand`의 겨눔(`AimBoard`·`AimInventory`·`AimNowhere`·`AimChanged`)과 `Commit`, 새 `HeldPointerView`(층·마우스의 든 것, Builder `BuildHeldPointer`), `PartySideView`·`BattleScreen`의 `AimFrom`·`OnHeldPressed`·`LetThroughWhileHeld`, 보드·인벤토리의 들린 조각(`lifted`)과 칠만의 그림자, `LootDropView`의 알파. 문서 Design/03 §5·10 §5, Architecture/12. Test: EditMode `GridGeometryTests`, PlayMode 새 Test 하나와 이긴 뒤 Test의 드랍, 고친 Test 둘, `UiTestUtil`(층을 거치는 누름, 마우스 자리).

## Open

- 21단계(상인)는 승인되어 구현했다: `b21-merchant.md`.
- Round 54의 사후 검토(Roadmap "Round 54의 세부"): 직접 플레이에서 가운데 겨눔이 손에 맞는지(짝수 칸의 반 칸 어긋남), 마우스가 숨는 동안 버튼을 찾기 쉬운지, 칸 밖을 누르면 돌아가는 것이 실수로 자주 나지 않는지, Unity 에디터 밖(빌드)에서 OS 마우스가 제때 돌아오는지.
- Round 53의 사후 검토(Roadmap "Round 53의 세부"): 직접 플레이에서 남색 안의 선이 아이템을 조각나 보이게 하지 않는지, 한 덩어리 그림자가 자리를 잘 말하는지.
- Round 52의 사후 검토(Roadmap "Round 52의 세부"): 직접 플레이에서 칸 50의 아이콘이 읽히는지, 창이 무대의 HP·피로를 가리는 것이 불편한지, I가 손에 맞는지.
- 사후 검토(Roadmap "20단계의 권장안 세부"). 시뮬은 숫돌을 원정당 0.13개만 쓰고 기본 파티의 클리어가 1.5%p 낮다(상점 자리를 차지): 직접 플레이에서 숫돌을 쓰는지, 상점 자리가 아까운지, +1이 느껴지는지.
- 전투도끼 3×2는 발키리 파티에서 엇갈린다(+2.8%p / −6.4%p). 직접 플레이에서 발키리의 남는 한 줄이 답답한지.
- 숫돌의 그림(지시가 있을 때, 호출 1회: 1×1 → 거의 정사각 구도), 전투도끼를 3×2 구도로 다시 그리기(지시가 있을 때, 호출 1회).
- 2026-10-09 20:01의 진행 중 저장은 Round 50 정정의 쥐 발톱(2×1)으로 무효다(b19 Open).
- `review_sheet.py --type item`은 아직 옛 띠 칸이다(Architecture/13).

## Verification

- `dotnet build Tools/Sim`, `transform`, `validate` OK(아이템 21).
- 시뮬(Design/08 §17): balanced 62.3% / 0.33, 붕괴 44.5%, 보스전 89.4% — 띠 안. 숫돌 0.13/원정. 변형(값 4: 62.6%, 가중치 4: 63.8%).
- 체인 1차 `20261010-090516`: setup·sim OK, EditMode 815 중 1 실패(`ItemSoundTests`: 기타 아이템이 출하 데이터에 없다는 가정), PlayMode 89 중 11 실패(출하 데이터로 꾸미는 Test가 1열 보드의 둘째 줄을 비었다고 봄: 1열은 발키리, 전투도끼 3×2).
  → `UiTestUtil.FlatRow`(기본 무기가 한 줄인 용병의 열), 상점 Test는 숫돌을 고르지 않음, `ItemSoundTests`는 "기타 아이템은 발동하지 않는다"로.
- 고친 뒤 체인 2차 `20261010-092022` **CHAIN OK**(EditMode 815/815, PlayMode 80/89, 9 skipped: 스크린샷), 스크린샷 `20261010-s20` 9/9(PNG 79: `_46_map_whetstone_stars`·`_47_map_star_card`가 늘었다).
- 목업(`mock-stars-bb.png`)과 대조해 고친 것 둘: 빈 별이 검은 칸에 묻혔다(별 그림에 어두운 테두리가 있어 바깥 별의 밝은 띠를 덮음 → 바깥 32, 빈 별의 칠 20, 켜진 별의 칠 26: `GridStarView`),
  카드의 피해 줄이 별의 +1을 세지 않았고 별 줄에 괄호가 없었다(목업: "맨 앞 적에게 피해 12" 아래 "(★ 숫돌 +1)" → `UiText.ItemCard`의 `starDamage`, 문구 `Item.StarBonus`). 문구 표의 쉼표를 따옴표로 감싸지 않아 setup이 한 번 멈췄다(고침).
- 체인 4차 `20261010-094851` **CHAIN OK**(EditMode 815/815, PlayMode 80/89), 스크린샷 `20261010-s20b` 9/9(PNG 79). 장면 넷: `ArtPipeline/Archive/51-claw-axe-whetstone/game/`(숫돌의 별, 롱소드의 카드, 붕괴 장면의 전투도끼 3×2, 바닥의 쥐 발톱).

- Round 52: 시뮬(저장소 데이터) balanced 61.1% / 0.34, 붕괴 44.5%, 보스전 89.5%(Design/08 §18, 띠 안). 체인 1차 `20261010-115714` **CHAIN OK**(EditMode 817/817, PlayMode 80/89, 9 skipped). 목업과 대조해 고친 것 둘(그늘 35% → 55%: 선형 색 공간, 이긴 뒤 화면의 창을 승리 띠 위로) 뒤 체인 2차 `20261010-121320` **CHAIN OK**(같은 수), 스크린샷 `20261010-r52b` 9/9(PNG 81). 구현 장면: `ArtPipeline/Archive/52-inventory-place/game/impl_*`.
- Round 53: 체인 `20261010-152115` **CHAIN OK**(EditMode 817/817, PlayMode 80/89), 스크린샷 `20261010-r53` 9/9(PNG 82: `_49`·`_50`, 무작위 지도에 상점이 없어 상점 장면 셋 빠짐), 상점 타일의 Test를 더한 뒤 PlayMode `20261010-153656` **CHAIN OK**. 목업 A와 대조해 같음: `ArtPipeline/Archive/53-grid-lines/game/impl_*`.
- Round 54: 체인 1차 PlayMode 2 실패(규칙이 바뀐 Test) → 고친 뒤 `20261010-165150` **CHAIN OK**. 스크린샷에서 찾아 고친 것: 상점에서 든 채 나가기(Test), 마우스의 아이콘이 가상 마우스로 화면 밖에(`PlaceAt`), 든 가방이 초록을 가림(비치게). 최종 체인 `20261010-172553` **CHAIN OK**(EditMode 826/826, PlayMode 81/90), sim OK, 스크린샷 `20261010-r54d` 9/9(PNG 85). 장면: `ArtPipeline/Archive/54-held-cursor/impl-held-cursor.png`.
- Round 55(1~4): 체인 `20261010-185252` **CHAIN OK**(EditMode 828/828, PlayMode 82/91) 첫 실행에 통과, 스크린샷 `20261010-r55` 9/9(PNG 85). 바닥 아이콘은 아이템 창의 크기.
## Next Action (제안)

- 사용자: 직접 플레이(x1로 원정 하나)로 숫돌과 별, 전투도끼 3×2를 본다. 체크리스트는 `b16-tuning.md`(이 단계의 것을 더함).
