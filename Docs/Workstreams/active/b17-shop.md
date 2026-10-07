# Slice B 17단계: 상점 노드와 지역 코인

Snapshot: 2026-10-07

## Goal

Architecture/09 "Slice B" Acceptance 9가 참이다: 상점 노드가 지도에 나오고, 전투에서 이기면 지역 코인을 받으며, 상점에서 물건을 보드·인벤토리·포션 칸으로 사고 새로고침의 비용이 상점 안에서 오르며,
원정이 끝나면 코인이 사라진다. 상점에서 끝내도 같은 물건으로 이어진다. 시뮬이 상점을 보고하고 수치가 띠 안에 있다. 체인이 통과한다.

## State

- 2026-10-07 사용자가 Round 44(상점 검토와 목업 여섯)에 **"권장안 구현"**으로 답했다 → 17단계를 열고 구현했다(Roadmap "17단계의 승인", 17단계 절). 16단계는 사용자의 직접 플레이를 기다리는 채로 함께 active다(`b16-tuning.md`).
- 구현이 끝났고 시뮬을 돌려 출발값을 정했다(Design/08 §13). 체인은 통과했다(아래 Verification). 스크린샷은 `20261007-r44b`(값 라벨의 말줄임을 고친 뒤).
- 2026-10-07 세션 정리: 주제별 다섯 커밋(Architecture와 기획 / 규칙과 데이터·저장·Application·시뮬과 EditMode Test / 화면과 그림 조각·문구·UI Test / 그림: Round 44의 기록과 42의 보관 / Roadmap과 Handoff)을 `origin/feature/slice-a`에 푸시했다.

## Done

- 기획: Design/03 §1(상점 노드)·§5(지역 코인, 상점에서 사기, 새로고침), 04 §5·§9, 07 §4, 09 §5(상점 스트림), 00 "Slice B" 표, 01 §6.
- Domain: `MapNodeKind.Shop`(`IsFought` 아님), `MapGenerator.DrawKind`의 상점 확률(정예 → 야영지 → 상점), `DungeonData.ShopMinFloor`·`ShopChancePercent`·`ShopCanStandOn`, `ItemData.Price`·`PotionData.Price`,
  `BalanceData`의 `ShopSlots`(최대 4)·`ShopRefreshBase`·`ShopRefreshStep`·`CoinsPerEnemy`·`CoinsPerFloor`·`EliteCoinPercent`, `RngStream.Shop`, `ExpeditionPhase.AtShop`, `ExpeditionState.Coins`·`Shop`(`ShopState`),
  `ExpeditionRules`: `CoinsFor`(`CompleteBattle`이 더함), `EnterShop`·`DrawStock`(보상과 같은 `DrawOptions`에 "값이 있는 것만"), `PriceOf`, `RefreshCost`, `OfferAt`, `CanAfford`, `CanBuyToBoard`·`BuyToBoard`, `CanBuyToInventory`·`BuyToInventory`, `CanBuyPotion`·`BuyPotion`, `ShopMergesAt`, `CanRefreshShop`·`RefreshShop`, `LeaveShop`; `IsBetweenBattles`에 `AtShop`.
- 데이터: `BalanceData.csv` 여섯 줄, `DungeonData.csv` 두 열(4, 15), `ItemData.csv`·`PotionData.csv`의 `Price`, Generated 넷. Mapper(`DefinitionMappers`).
- 저장: `RunSaveData` 8(`ExpeditionRecord.Coins`, `Shop`: `ShopRecord`의 `Stock`(판 자리 null)·`Refreshes`), `RunSaveMigrator.From7To8`, `RunSaveMapper`(기록·읽기·검증: 상점 단계와 기록의 일치, 상점 노드, 칸 수, 물건, 음수). Architecture/07.
- Application: `GamePhase.Shop`(`ScreenCatalog`는 노드 맵), `ExpeditionManager.EnterNode`의 상점, `ShopStock`·`PriceOf`·`RefreshCost`·`CanAfford`·`CanBuy…`·`Buy…`·`ShopMergesAt`·`CanRefreshShop`·`RefreshShop`·`LeaveShop`·`BattleCoins`. Architecture/11.
- 시뮬: `SimPolicy.ShopAt`(포션 둘까지, 합치기, 자리 맞는 칸; 새로고침 둘까지), `Simulations`의 상점 줄(상점·산 것·새로고침·코인 받음/씀/남음). 보고 Design/08 §13.
- 화면: `UiPrefabSetup.NodeMap`의 상점 창(`Shop`·`ShopWindow`·`ShopBox`·`ShopTiles`·`ShopRefresh`·`ShopLeave`)과 `BuildShopTile`·`BuildHeaderCoins`, 패널의 `ShopBuy`; `ShopTileView`(상태 넷: 판매·고름·못 삼·팔림); `NodeMapScreen`의 상점(고르기·카드·사기·새로고침·나가기·구매 메모);
  `RewardScreen`·`UiPrefabSetup.Reward`의 헤더 코인; `BattleScreen.ShowResult`의 "지역 코인 +N"; `ItemTooltipView.ShowBelow`·`Notch.Top`·`TooltipPlacement.Below`와 Kit의 위 꼭지; `MapNodeView._shopIcon`; `UiArt.NodeShop`·`Coin`과 `ArtPipeline/Archive/44-shop-node/draw_icons.py`;
  문구 `Map.Shop`·`ShopHint`·`EnterShop`·`AtShop`·`ShopChoose`·`ShopBuyHint`·`ShopCoinsLabel`·`ShopRefresh`·`ShopLeave`·`ShopSold`·`ShopItemSub`·`ShopBought`·`Coins`, `Battle.Coins`. Architecture/12 "상점"·"툴팁"·"전투 화면", 13, 14.
- Test: EditMode `ShopRulesTests`(11), `ShopFlowTests`(5), `RunSaveMapperTests.Migrate_From7…`, `TooltipPlacementTests.Below_…`(2), `TestData`·`TestCsv`·`StaticDataTransformerTests`·`FlowTestKit`의 갱신; PlayMode `UiFlowTests.Shop_…`(지도에 상점이 없는 드문 판은 Inconclusive), `UiTestUtil`(상점 경로, `GoIntoTheFirstNode`), 스크린샷 `_41_map_shop`·`_42_shop`·`_43_shop_pick`.
- 문서: CLAUDE.md §9(17단계), Roadmap("17단계의 승인", 17단계 절, 사후 검토 대기, 진행 순서, 날짜 표), README 44 "판정·구현".

## Open

- 사후 검토(Roadmap "17단계의 권장안 세부"): 야영지 바로 앞 층의 상점, "인벤토리에 넣기"의 자리, 코인의 층 증가가 전투마다인 것, 값과 수입(사람은 시뮬보다 많이 쓴다), 붕괴가 난 원정 29%(띠의 아래 끝: 상점이 전투 한 판을 대신해서).
- 상점의 소리(코인)와 그림(표식·코인)은 지시가 있을 때(유료 호출).
- 16단계의 직접 플레이는 상점이 있는 데이터로: `b16-tuning.md`의 체크리스트에 상점(창의 읽힘, 값의 느낌, 새로고침을 쓰고 싶은지, 코인이 남는지)을 더해 본다.

## Verification

- `dotnet build Tools/Sim` OK(순수 C#), `transform`으로 Generated 넷을 다시 만들었다.
- 시뮬(시드 1): 상점 없음(확률 0)은 §12 J를 재현(64.3% / 0.35 / 34.3%). 코인 2/1: 71.3% → 레버 스윕 여덟(1,000회) → 코인 1/1(채택): balanced 67.5% / 0.32, safe 60.0% / 0.29, none 75.9% / 1.50, 여섯 파티 72.2 / 62.8 / 49.9 / 28.3 / 2.5 / 0.0%.
- 체인 1(`20261007-200249`, setup): `ShopTileView.IsSold` 누락의 컴파일 오류 → 더함. 체인 2(`20261007-200648`, setup): OK(Prefab 셋과 Stamp 다시, 문구 표·글꼴 아틀라스 다시, 새 .cs·.png의 .meta).
- 체인 3(`20261007-201020`, sim editmode playmode): sim OK, EditMode 748/751 — 실패 셋은 Test의 오류(저장소 Test의 손으로 쓴 포션 행에 `Price` 칸 없음, 카드 자리 Test가 모서리에서 멈추는 꼭지를 잘못 기대, 상점 Test가 포션 칸을 채우기 전에 물건을 뽑음) → 고침.
  PlayMode 74/85 — 실패 둘도 Test의 오류(`GameFlowTests`의 단계 분기에 상점 없음 → `LeaveShop`, `UiFlowTests.Shop_…`가 산 뒤 비워진 자리의 id를 읽음 → 미리 보관) → 고침.
- 체인 4(`20261007-2027xx`, editmode playmode): EditMode **751/751**, PlayMode **76/85**(9 skipped: 스크린샷의 Explicit. `Shop_…`은 그 판의 지도에 상점이 있어 끝까지 돌았다). `Assets/InitTestScene*` 없음.
- 스크린샷 `20261007-r44`(9/9, PNG 71): 상점 장면 셋과 결과 창의 "+4", 보상 헤더의 코인이 보였다. 그런데 **타일의 값 숫자가 비어 있었다**(코인 표식만). 진단 PlayMode(라벨의 너비 29.94 = 선호 너비, 글자 수 0)로
  말줄임(Ellipsis) 넘침이 꼭 맞는 너비에서 글자를 모두 지운 것을 찾아 숫자 라벨을 넘침(`Numeral`)으로 바꾸고, Test가 값의 글자 수를 확인하게 했다(Architecture/12 "상점").
- 체인 5(`verify17`: setup OK → `Shop_…` 1/1 → 스크린샷 `20261007-r44b` 9/9, PNG 71, Render 설정 그대로 → sim OK, EditMode **751/751**, PlayMode **76/85**(9 skipped)). **CHAIN OK**. 구현 장면은 `ArtPipeline/Archive/44-shop-node/game/`.
- `git diff --check` 깨끗. ProjectSettings 변경 없음. setup이 바꾼 것은 Prefab 셋(NodeMap·Reward·Battle)과 Stamp, 문구 표 셋, 글꼴 아틀라스, Generated 넷이다.

## Next Action (제안)

- 사용자: 직접 플레이(상점이 있는 원정 하나, x1) → 플레이 기록과 메모 → 16단계의 맞추기와 마감(17단계의 수치도 함께).
- 사후 검토 항목(Roadmap "17단계의 권장안 세부")은 플레이 뒤에 본다.
