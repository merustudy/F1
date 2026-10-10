# Slice B 21단계: 상인

Snapshot: 2026-10-10 (구현)

## Goal

Architecture/09 "Slice B" Acceptance 13이 참이다: 상점에 들어가면 물건 8개가 상인의 한 격자에 제 모양대로 놓이고(아이템·가방·포션 함께) 각자 값을 보인다. 가리키면 사실과 값이 툴팁에 나온다.
사면 그 자리가 비고 다른 물건은 그대로 있으며, 새로고침하면 모두 다시 놓인다. 같은 시드와 같은 행동이면 같은 물건이다. 저장 형식은 그대로다.

## State

- 2026-10-10 Round 55(`ArtPipeline/Archive/55-loot-vendor/README.md`): 사용자 "상점 아이템 공급 방식도 디아블로 2 상인이 공급해주는 방식으로" → 조사(디아블로 2 상인, 출처 둘)·상점 크기 시뮬 넷·목업 → "5. 6. 한 화면·새로고침, 나머지 권장안" → 2차 목업과 기획 제안
  → **"d. 포션도 사면 사라짐 / g. 확률 / 나머지 권장안대로 구현"**(Roadmap "21단계의 승인") → 21단계를 열어 구현했다. 같은 Round의 1~4(드랍·물건을 인벤토리 빈 칸에, 바닥 아이콘의 크기)는 20단계의 Round로 먼저 구현했다(`b20-stars.md`).
- 2026-10-10 Round 56(`ArtPipeline/Archive/56-merchant-grid/README.md`): 사용자 "상점 칸을 10*8로, 가장 오른쪽 줄에 포션(최소 1개), 9번째 줄은 비워두기, 나머지는 큰 물건부터" → 검토·목업·포션 변형 시뮬 → "4. 없음, 5. 포션은 그대로, 나머지는 권장안" → 구현.
- 2026-10-10 Round 57(`ArtPipeline/Archive/57-shop-hover-tooltip/README.md`): 사용자 "디아블로2처럼 마우스를 아이템 위로 올렸을 경우 툴팁" → 목업·판정 일곱 → "B안에서 인벤 창에 등급 점수는 삭제해주고, 살수없을때는 배경색을 검붉은 색으로 하자, 나머지는 권장안으로 구현" → 구현.
- 2026-10-10 Round 58(`ArtPipeline/Archive/58-merchant-bag-row/README.md`): 사용자 "가방도 포션처럼 특정 위치에 비치(맨 아래줄은 가방 전용), 아래서 +1줄은 비우기. 바로 구현" → 구현.
- 2026-10-10 세션 정리에서 20단계·Round 50~58과 함께 커밋·푸시했다.

## Done

- 기획: Design/03 §5(【확정】 21단계), 08 §19(시뮬). CLAUDE.md §9. Architecture/07(물건 수), 09(범위·순서 10·Acceptance 13), 12("상점 — 상인").
- 데이터: `BalanceData.csv`의 `ShopSlots` 8, `BalanceData.MaxShopSlots` 8, Generated.
- 코드: `MerchantLayout`(넓은 것부터 읽는 순서, 포션은 끝, 모자라면 행을 더함), `MerchantGridView`(10×5 격자, 조각, 값, 물건의 이벤트), `ItemSlotView.ShowPotion`·`Dim`·`ShowBag(picked)`, `NodeMapScreen`의 상인(재고마다 자리 기억, 툴팁 상자, 우클릭 카드),
  문구 `Map.OfferPrice`, Builder `BuildMerchantGrid`와 상점 창의 정리. 지운 것: `ShopTileView`, `ItemTileView`, `BuildShopTile`·`BuildItemTile` 무리.
- Test: EditMode `MerchantLayoutTests`(셋). PlayMode 상점 Test를 상인으로, `UiTestUtil.ClickGood`, 스크린샷의 상점 장면.
- Round 57: 툴팁은 마우스를 올린 물건 위(`TooltipPlacement.Over`, Frame의 `MerchantInfoBox`, 글에 맞는 크기), 든 동안 없음, 상점의 우클릭 카드 뺌(합치기 안내는 툴팁에),
  툴팁의 이름에 등급 없음(`UiText.TooltipTitle`, 인벤토리 창도), 살 수 없는 물건은 칸이 검붉음(`ItemSlotView.ShowUnaffordable`, `UiPalette.GridPieceUnaffordable`; `Dim`을 대신함). Test: `TooltipPlacementTests` 둘, PlayMode 상점·인벤토리 Test, `HoverGood`·`LeaveGood`.
- Round 58: 아이템은 1~6행(`MerchantGridView.ItemRows`), 7행 빔, 가방은 8행(`BagRow`)의 1~8열에 넓은 것부터(`MerchantLayout.Lay`의 `itemRows`·`bagRow`, `MerchantGood.Kind`). Test: `MerchantLayoutTests` 넷, PlayMode 상점 Test.

## Open

- Round 57의 사후 검토(README "해석"·"빈 곳을 권장안으로"): "인벤 창에 등급 점수"를 툴팁 이름 줄의 "· 등급 N"으로 읽음(카드 제목·바닥 이름판·패널 줄은 그대로), 검붉은 색 `#521818`, 합치기 안내를 툴팁에 둠,
  위쪽 줄의 물건은 툴팁이 지도 머리까지 올라감.
- Round 58의 사후 검토(Roadmap "Round 58의 세부"): 가방은 1~8열 안에만, 넓은 것부터, 아이템이 넘치면 가방 줄도 내려감.
- Round 56(격자 10×8과 포션 열)의 사후 검토(Roadmap "Round 56의 세부"): 1~8열이 절반 넘게 비는 것, 포션이 사면 사라지고 새로고침에도 돌아오지 않는 것.
- 직접 플레이: 8개가 한눈에 읽히는지, 조각의 값이 작지 않은지, 포션이 사면 사라지는 것이 불편하지 않은지, 상점 창이 지도를 가리는 정도.
- 사후 검토(Roadmap "21단계의 권장안 세부"): 앱을 다시 열면 산 자리가 메워져 다시 깔리는 것, 이름("상점" 그대로).
- 디아블로 2의 팔기·되사기·도박은 넣지 않았다(코인이 남는 경제: Design/08 §19).

## Verification

- 시뮬(Design/08 §19): balanced 63.8% / 0.32, 보스전 91.3%, safe 56.2% / 0.29, 여섯 파티 66.5 · 59.2 · 49.5 · 31.9 · 6.1 · 0.0 — 띠 안.
- 체인 `20261010-192410` **CHAIN OK**(setup, sim, EditMode 831/831, PlayMode 82/91, 9 skipped) — 첫 실행에 통과. 스크린샷 `20261010-s21` 9/9(PNG 85).
- 2차 목업과 대조(`ArtPipeline/Archive/55-loot-vendor/game/impl_ko_42_shop.png`·`impl_ko_43_shop_pick.png`): 같다. 다른 점은 제목("상점" 그대로)과 아무것도 가리키지 않을 때 툴팁이 숨는 것.

- Round 56: 체인 1차 통과 뒤 스크린샷에서 격자가 창 밖으로 넘침(Prefab Stamp가 런타임 UI 상수를 몰랐음 → `ComputeStamp`가 `Assets/@Scripts/UI`도 셈). 체인 2차 `20261010-205228` **CHAIN OK**(EditMode 832/832, PlayMode 82/91), 스크린샷 `20261010-r56b` 9/9, 목업과 같음(`ArtPipeline/Archive/56-merchant-grid/game/impl_*`). 시뮬 08 §20: 64.1% / 0.32, 보스전 91.5%.

- Round 57: 체인 `20261010-214920` **CHAIN OK**(EditMode 834/834, PlayMode 82/91), 스크린샷 `20261010-r57` 9/9, 목업 B와 요소마다 대조(`ArtPipeline/Archive/57-shop-hover-tooltip/game/impl_*`).

- Round 58: 체인 `20261010-222748` **CHAIN OK**(EditMode 835/835; 상점 Test는 지도에 상점이 없어 Inconclusive) → PlayMode `20261010-224023` **CHAIN OK**(82/91, 상점 Test 통과), 스크린샷 `20261010-r58` 9/9.

## Next Action (제안)

- 사용자: 직접 플레이로 상점을 한 번 본다(4층 이후의 상점 노드).
