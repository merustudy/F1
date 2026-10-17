# Slice C 29단계: 맞추기와 직접 플레이 — 100일 런의 띠, 데이터 조정, Acceptance

Snapshot: 2026-10-16 (27~29단계 일괄 승인 아래 맞추기를 권장안으로. 직접 플레이는 사용자의 몫. 체인은 아래 Verification)

## Goal

Architecture/09 "Slice C" 구현 순서 5가 참이다: 100일 런의 띠가 정해져 있고 시뮬 `run`의 수치가 그 안이며, Acceptance 1~6·8이 확인됐다. 7(직접 플레이로 런 하나를 끝까지)은 사용자가 한다.

## State

- 2026-10-16 사용자 "이어서 … 슬라이스 완성을 위한 나머지 절차를 네 권장안에 따라 완성할 때까지 알아서 진행(필요시 이미지 api 사용)" → Roadmap "27~29단계의 일괄 승인": 띠도 권장안으로 정해 적는다.
- 띠와 조정은 Design/04 §6·§8, 08 §27, Roadmap "사후 검토 대기"의 "29단계의 권장안 세부".
- **사용자의 몫(남김)**: 직접 플레이(Acceptance 7: 런 하나를 끝까지, `completed/b16-tuning.md`의 체크리스트, 원정 15~30분), 그 피드백에 따른 조정, Slice C 완료의 선언(CLAUDE.md §9·Architecture/09의 상태 줄 갱신).

## Done

- 2026-10-17 사용자 "상점에서 현재 용병들 기본 무기도 제공 / 적 드랍에 일반 아이템 드랍 추가 — 검토 후 권장안 구현": `ItemData.DropWeight`(선택 열, 유니크는 0), `DungeonData.GeneralDropChancePercent`(선택 열, 광산 30·심연 0), 기본 무기의 `Price`·`ShopWeight`, `ExpeditionRules.IsShopCandidate`·`IsJobWeapon`·`PartyWeaponIds`(파티 직업의 기본 무기만)·`ShopGoodsCount(data, state)`·`HasGeneralDrops`·`DrawGeneralDrop`·`DropLoot`의 굴림, Manager의 `ShopGoodsCount`, 시뮬의 줄(기본 무기 제공·일반 드랍), Test(`ShopRulesTests` 기본 무기, `ExpeditionRulesTests` 일반 드랍, `ShippedDataTests`). 심연의 확률을 낮춘 까닭과 수치는 08 §32.
- 2026-10-17 사용자 "숫돌값은 지금 복귀": 숫돌 `Price` 1 → 8(`ItemData.csv`·Generated), `ShippedDataTests`의 파는 값(4), Roadmap "임시 조치" 비움, Design/03 §5·08 §22·§33, 00. 시뮬 전후(같은 시드, 복사본에 값 1): 광산 85.0 → 83.7%(산 것 12.1 → 10.5, 합치기 10.5 → 9.8), 심연 11.2%, 런 80.7 → 82.0%(300런의 흔들림 안).
- 2026-10-17 사용자 "원정 로스터는 원정 버튼 누르면 팝업으로 로스터 구성 제공(유사게임 검토 후 권장안 구현)": Round 75(`ArtPipeline/Archive/75-party-popup/`: README의 유사 게임 검토, `mock_party.py`의 안 A·B·C와 `mock-party-compare.png`) → 권장안 B 구현. `ExpeditionScreen`의 파티 팝업(`Frame/Shade`·`Frame/PartyWindow`, `PartyRowSlotView` 넷, `PartyPickChipView` 칩, `PartyDepart`), 빌더 `BuildPartyRowSlot`·`BuildPartyPickChip`, [출발]은 보통 버튼(팝업의 출발이 출발 소리), `LobbyScreen`의 원정 단추·성문은 `PartyTooSmall`에도 켜짐, `UiKeys.Expedition` 여섯(문구 CSV), Test(`Expedition_FormsThePartyInThePopup_…`, `UiTestUtil.DepartFromExpedition`, 흐름·소리 Test의 두 걸음, 스크린샷 `_66_expedition_party`), 문서(Design/04 §7·00, Architecture/12 "원정 화면"·"로비: 마을", Roadmap 29·사후 검토).
- 2026-10-17 사용자 "원정 클리어시 가지고 있는 물건가치 > 지역화폐 환산 / 지역화폐를 금화로 환산하여 금화 보상 — 사안 검토 후 권장안 구현": `RunRules.ItemCoinsFor`(살아 돌아온 구성원의 보드·인벤토리의 팔 수 있는 아이템의 `SellPriceOf` 합), `Settle`의 클리어 환산(`CoinsPerGold`로 나눔, `GoldEarned`에 포함), `SettlementReport.CoinsFromItems`·`CoinsConverted`·`GoldFromCoins`, `BalanceData` `CoinsPerGold` 10, 정산 화면의 줄 `Settle.GoldWithCoins`, Test(`LobbyEconomyTests` 환산·후퇴). 비율은 시뮬(08 §31)로 정했다. 시뮬의 `run`은 `Settle`을 쓰므로 자동으로 반영된다.
- 2026-10-17 사용자 "전리품 드랍된 남은 것을 단축키를 눌러 바로 인벤에 넣기 신설 추가": `ExpeditionRules.CanTakeAllLootToInventory`·`TakeAllLootToInventory`, `ExpeditionManager`의 같은 명령(저장 한 번), `TakeAllKey`(A)와 `BattleScreen.PressTakeAllKey`, `Battle.LootHint`의 안내, Test(`ExpeditionRulesTests` 둘, `UiFlowTests.Loot_TheAKey_…`). 문서 Architecture/08·11·12, Design/03 §5·00. 같은 턴의 "상점 아이템 판매 기능 추가"는 Round 74(목업 여섯, 규칙 권장안)로 판정 → 사용자 "안 b, 50%, 권장안 수용" → 구현: `BalanceData` `SellPercent` 50, 적 아이템의 `Price`, `ExpeditionRules.SellPriceOf`·`CanSellBoardItem`·`SellBoardItem`·`CanSellInventoryItem`·`SellInventoryItem`, Manager의 명령 넷, `ItemTooltipView`의 팔기 버튼(`_sell`, `SellClicked`, `PointerIsOver`), `PartySideView.SellOption`·`SellRequested`, `NodeMapScreen.SellOptionFor`·`OnSellRequested`, 문구 셋(`Map.Sell`·`CannotSell`·`ShopSold`), Test(`ShopRulesTests` 팔기, `ShippedDataTests` 적 아이템의 값, `UiFlowTests` 상점 Test의 팔기, 스크린샷 `_65_shop_sell_card`). 문서 Design/03 §5·07 §4·00, Architecture/05·08·11·12, README 74.
- 2026-10-17 사용자 "보스도 깊이 보정 적용"과 "용병 단순 피격시에는 피로+0(치명타만 피로 추가 적용)": `ExpeditionRules.FloorsBelowTheFirst`가 보스에도 층을 줌(최종 결전은 1층), `BalanceData.csv` `FatigueOnHit` 0(치명타 피로 `CritFatigueTaken` 9는 그대로), `LongExpeditionTests`의 보스 줄, PlayMode의 붕괴 연출 Test는 피격 대신 **동료의 빈사**(1 HP의 미끼를 1열에, 발키리는 2열)로 다시 무대를 꾸밈. 전후와 치명타 피로의 후보는 Design/08 §30.
- 2026-10-17 사용자 "층당 적 hp 점차적 추가 증가 검토" → "권장안 반영": `ExpeditionRules.DeeperMaxHp`(층마다 ×(100+p)/100를 정수로 거듭), `DungeonData.csv` 광산 `EnemyHpPerFloorPercent` 5, `EnemyData.csv` 군주 364·호위 42, Generated, `LongExpeditionTests`(복리 단위 Test와 깊은 층 Test). 검토·전후·남은 결정(보스의 깊이 보정)은 Design/08 §29. 복사본 시뮬은 scratchpad의 `simcopy`(Git에 올리지 않음).
- 2026-10-17 사용자 결정 "깊이 ×2 + 피해 ×0.67 + 피격 1 + 폭풍 70초로 구현"(원정 하나 15~30분): `DungeonData.csv` 광산 30층(정예·야영지 11, 상점 7, 단계 9·20, 야영지 층 30, 층당 HP 2%), `EnemyGroupData.csv` 광산 무리의 층 범위 두 배, `ItemData.csv` 모든 피해 ×0.67(글라디우스 8.04-13.4, 별 +4 유지), `BalanceData.csv` `FatigueOnHit` 1·`StormStartMs` 70000, Generated 넷, `ShippedDataTests`의 글라디우스 값. 전후와 검토한 안은 Design/08 §28. 판이 쉬워졌다(런 92%): 되돌릴 레버를 재어 둠(08 §28 "다시 맞추기"), 고르는 것은 사용자.

- 측정(Design/08 §27): 균형형 300런 시드 1·2·3과 1,000런(52.7~61.3%, 1,000런 54.9%), 안전형(63.7%, 최종 결전 12%). 패배는 거의 전부 고용할 금이 모자란 전멸(41%), 기한과 사망 한도에는 닿지 않음.
- 레버 스윕(저장소 밖 복사본 `--project-root`, 시드 1·300런): 속도(해금 3·4회, 광산 6·7일, 쉬기 2일, 심연 5일, 합친 둘) — 기한의 런은 3% 이하, 승리 56~61%, 전사자만 늚; 경제(고용비 20, 층의 금 3, 클리어의 금 30, 합침, 정책의 일곱 고용·80 치료) — 고용비 20이 77%, 층의 금 3이 73%, 합치면 81%지만 쉬기 0.9·사망 한도 13%.
- 채택: **고용비 30 → 20**(`BalanceData.csv`, Generated). 시드 셋 73~77%, 1,000런 72.7%, 안전형 76%(최종 결전 15%). 저장소 데이터로 다시 돌린 시드 1이 복사본과 같음(76.7%).
- 띠(Design/04 §8, 권장안): 균형형 승리 65~80%, 전멸 ≤ 25%, 사망 한도 ≤ 12%, 끝난 날 40~60일, 최종 던전 열림 ≥ 75%; 안전형 승리 65~80%, 최종 결전 5~25%.
- Test: PlayMode `UiFlowTests.Lobby_TheTown_…`의 고용비(30 → 20, 금 80·60). 시험 데이터(`TestData`·`TestCsv`)의 30은 그대로(시험용).
- Acceptance 점검표: Roadmap 29.
- 문서: Design/00·04 §5·§6·§8·08 §27, Roadmap(29, 사후 검토, 진행), c28 Handoff를 completed로.

## Open

- **판정 대기 Round 76**(2026-10-17 사용자 "파티 구성 이미지 개선 — 출발 전 파티 구성 캐릭터 등을 볼 수 있는 창 추가(전투 선택 시 처럼)"): `ArtPipeline/Archive/76-party-figures/README.md`의 안 A(팝업의 열 칸을 그림 자리로)·B(띠)·C(둘 다)와, 사용자 안 **D**(왼쪽 무대에 서 있는 파티 + 오른쪽 용병 목록 — 목업 뒤 권장을 D로 바꿈)와 결정 셋. 판정 뒤 구현(구현 메모는 README).
- 사용자: 직접 플레이 → 피드백 조정 → Slice C 완료 선언. (숫돌 1 → 8 복귀는 2026-10-17 사용자 지시로 끝냈다.)
- 기한(100일)은 균형형 시뮬에서 닿지 않는다(1% 아래). 플레이의 속도를 본 뒤 속도 레버(08 §27)를 다시 본다.
- 사후 검토: 띠의 값, 고용비 20, 옛 결론 82%/75%를 띠 안의 목표로 둔 것.

## Verification

- `transform`·`validate` OK. 시뮬 Design/08 §27.
- 체인 `20261016-233306`(고용비 20의 데이터와 로비 Test: setup·sim OK, EditMode **917/917**, PlayMode **87/98** — 건너뜀 11은 그래픽이 있어야 하는 스크린샷 Test).
- 2026-10-17 깊이 ×2의 데이터: 체인 `20261017-073612`(setup·sim OK, EditMode **917/917**, PlayMode **87/98**), 그래픽 스크린샷 `~/Library/Caches/F1/screenshots/20261017-074500-depth`(11/11, PNG 109장: 30층 지도의 스크롤, 전투의 "폭풍까지 70.0초").
- 2026-10-17 복리와 심연 HP: 체인 `20261017-084458`(setup·sim OK, EditMode **918/918** — `DeeperMaxHp` 단위 Test를 더함, PlayMode **87/98**). 화면은 바뀌지 않아 스크린샷은 다시 찍지 않았다. 시뮬은 `dotnet build Tools/Sim` 뒤에 다시 잰 값(08 §29).
- 2026-10-17 보스의 깊이 보정과 피격 피로 0: 체인 `20261017-092453`(setup·sim OK, EditMode **918/918**, PlayMode **87/98**), 그래픽 스크린샷 `~/Library/Caches/F1/screenshots/20261017-093500-boss-hit`(11/11, PNG 109장: 붕괴 연출 `ko_38`이 새 무대 — 2열의 발키리가 "강인", 1열의 미끼가 빈사 — 에서 찍힘). 치명타 피로 15/3(데이터만): 체인 `20261017-094417`(setup·sim OK, EditMode 918/918, PlayMode 87/98).
- 2026-10-17 전리품의 A 키: 체인 `20261017-101301`(setup·sim OK, EditMode **920/920**, PlayMode **88/99** — 건너뜀 11은 그래픽 스크린샷 Test). 화면의 배치는 바뀌지 않아 스크린샷은 찍지 않았다.
- 2026-10-17 상점 판매(Round 74 B): 체인 `20261017-110345`(setup·sim OK, EditMode **922/922**, PlayMode **88/99**), 그래픽 스크린샷 `~/Library/Caches/F1/screenshots/20261017-111500-shop-sell`(11/11, PNG 111장: `ko_65_shop_sell_card` — 산 버클러의 카드에 "팔기 · 6 코인").
- 2026-10-17 클리어의 코인 → 금: 체인 `20261017-113133`(setup·sim OK, EditMode **923/923**, PlayMode **88/99**). 화면은 정산 줄의 글자만 바뀌어 스크린샷은 찍지 않았다.
- 2026-10-17 기본 무기 제공·일반 아이템 드랍: 체인 `20261017-123326`(setup·sim OK, EditMode **926/926**, PlayMode **88/99**). 이 체인은 끝에 `leftover`(Assets/InitTestScene*)를 보고했는데, 그 파일은 앞서 EditMode 실패로 내가 중단한 체인 `20261017-122023`의 PlayMode가 남긴 것(12:20)이라 지우고 기록한다. 화면은 바뀌지 않아 스크린샷은 찍지 않았다.
- 2026-10-17 숫돌 값의 복귀: 체인 `20261017-125751`(**CHAIN OK**: setup·sim OK, EditMode **926/926**, PlayMode **88/99**). 화면은 바뀌지 않아 스크린샷은 찍지 않았다.
- 2026-10-17 원정 파티 팝업(Round 75 B): 체인 `20261017-135539`(**CHAIN OK**: setup·sim OK, EditMode **926/926**, PlayMode **89/100** — 건너뜀 11은 그래픽 스크린샷 Test), 그래픽 스크린샷 `~/Library/Caches/F1/screenshots/20261017-140500-party-popup`(11/11, PNG 113: `_66_expedition_party`가 팝업). 그 전의 체인 `20261017-133925`는 새 Test 하나가 막 켜진 열 버튼을 그리기 전에 눌러(Shade가 가린다고 판정) 실패 → Test에 `WaitForRedraw` 둘. 스크린샷을 목업 B와 대조: 창·제목·닫기·던전 줄·열 칸 넷(앞으로/뒤로/제외, 끝 열의 꺼짐)·로스터 머리글·칩 격자(파티 안은 금빛과 "N열")·안내·출발 모두 있음. 다른 점은 파티 안 칩의 놋쇠 테(목업)를 두지 않은 것뿐(막사의 열 버튼과 같은 금빛 칠).

## Next Action (제안)

- 세션: 체인 뒤 커밋·푸시. 다음 세션은 사용자의 직접 플레이 기록(`Logs/`)을 읽어 맞추기와 Slice C 마감.
