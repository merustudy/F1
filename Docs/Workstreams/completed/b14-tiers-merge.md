# Slice B 14단계: 단계와 합치기

Snapshot: 2026-10-06

## Goal

아이템에 단계(동·은·금·다이아)가 있고 단계만큼 효과가 커진다. 같은 아이템·같은 단계를 보드의 칸에 놓으면 한 단계 위 하나로 합쳐진다.
깊은 층의 보상은 높은 단계이고 정예는 한 단계 위다. 야영지에서 쉬기 대신 정비(아이템 하나를 한 단계 위로)를 고를 수 있다. 화면이 단계를 보인다(목업).
범위와 순서: Architecture/09 "Slice B", 규칙: Design/02 §4, 03 §1·§5.

## State

- 규칙·데이터·저장·시뮬, 그리고 화면까지 됐다. 화면은 목업 Round 35(`ArtPipeline/Archive/35-tiers/README.md`)의 판정 "권장안 구현"(칸 A, 합칠 칸의 알림, 보상 카드의 띠, 정비의 두 걸음)대로다.
- 커밋 안 함(12·13단계와 함께 세션 정리에서). 브랜치 `feature/slice-a`.

## Done

- 데이터: `ItemTier`(`DataEnums`), `BalanceData.TierSilverPercent`·`TierGoldPercent`·`TierDiamondPercent`(200·300·400)와 `TierPercent`, `DungeonData.SilverFloor`·`GoldFloor`·`DiamondFloor`(5·10·0)와 `RewardTierAt`,
  광산의 `ItemGradePerFloor` 2 → 0(13단계 출발값 1에서 0으로: 단계가 대신한다). 변환기, Generated JSON.
- Domain: `EquippedItem.Tier`·`Magnitude`·`TierUp`, `ItemEffect.MagnitudeAt(등급, 퍼센트)`, 전투가 단계를 쓴다. `RewardOption.Tier`, 보상의 단계(층, 정예 +1).
  합치기: `ExpeditionRules.CanMerge`, `MoveItem`·`PlaceFromInventory`·`TakeItemReward`와 각 `Can…`. 정비: `CanUpgradeAtCamp`·`UpgradeAtCamp`.
- Application: `ExpeditionManager.CanUpgradeAtCamp`·`UpgradeAtCamp`.
- 저장: `run.json` 버전 5(`ItemRecord.Tier`, `RewardRecord.Tier`, 기본 Bronze), `From4To5`, 검증(모르는 이름, 포션 보상은 동).
- 시뮬: 정책의 합치기와 야영지의 선택(HP 50% 아래면 쉬기, 아니면 정비), 보고(합치기, 정비, 끝날 때의 단계).
- 화면(Round 35): 그린 조각 `tier_rim`과 `UiPalette`의 단계 색, 칸의 테와 합치기 표(`ItemSlotView`, `BattleItemView`), 제목의 단계와 합치기 문장(`UiText`), 합치기의 질의
  (`ExpeditionRules`·`ExpeditionManager`의 `MergesAt`·`RewardMergesAt`·`HasMergeTarget`, `PartySideView.ExternalMerges`), 보상 카드의 띠(`RewardOptionView`), 야영지 창의 두 카드와 정비 단계
  (`UiPrefabSetup.NodeMap`, `NodeMapScreen`의 정비 상태, `PartySideView.ExternalSelected*`·`DetailOverride`), 문구 14개(단계 이름 넷, 합치기 둘, 변화, 정비 여섯, 제목 고침).
- Test: EditMode `TierRulesTests`(새, 14개), `RunSaveMapperTests`(버전 5 변환, 단계의 왕복, 깨진 단계 넷), `CampFlowTests`(정비의 저장), 변환기 Test, TestCsv·TestData.
  PlayMode 넷(칸의 테, 합치기 표시와 합치기, 보상 카드, 정비의 흐름). 스크린샷 `_35_map_tiers`, `_34_camp_mend`.
- 문서: Design/00 "Slice B", 02 §4, 03 §1·§5, 07, 08 §10. Architecture/07(버전 5), 08 "단계와 합치기", 09(저장 줄), 11(정비 명령), 12(단계·합치기의 표시, 보상 카드, 정비의 두 걸음, Test), 13(`tier_rim`).
  Round 35 README(판정과 구현).

## Open

- 피로: 정비를 고르면 쉬지 않으므로 살아 돌아온 용병의 피로가 늘었다(평균 19 → 32). 15단계(피로의 판정)에서 다시 본다.
- 정비하는 동안 인벤토리 팝업은 닫히고 보드의 아이템 옮기기는 쉰다(권장안으로 정함). 플레이해 보고 불편하면 다시 본다.

## Verification

- `dotnet build Tools/Sim` 오류 0, `transform`·`validate` OK. 시뮬(시드 1): balanced 3,000회 66.0% / 0.33, safe 56.0% / 0.29, none 81.8% / 1.25, 보스전 90.3%. 레버와 여섯 파티는 Design/08 §10.
- 체인(04:26, `~/Library/Caches/F1/chain/20261006-042648`, `UiText`를 고치기 전): setup OK, sim OK, EditMode 697/697.
- 체인(04:35, `UiText`·`RewardScreen`을 고친 뒤): setup OK, sim OK, EditMode 697/697, PlayMode 66/66(`[Explicit]` 8개 제외). `git diff --check` 깨끗.
- 체인(화면까지 넣은 뒤, `~/Library/Caches/F1/chain/20261006-084117`): setup OK(Prefab 셋과 Stamp, 문구 표, `tier_rim` Import), sim OK, EditMode 698/698, PlayMode 70/70(`[Explicit]` 8개 제외).
  `git diff --check` 깨끗, `Assets/InitTestScene*` 없음.
- 스크린샷 57장(8/8, `~/Library/Caches/F1/screenshots/20261006-b14`): `ko_35_map_tiers`(고른 단검, 2열의 같은 단검에 "합치기 → 은", 버클러 금·약초 주머니 다이아의 테, 설명 줄의 합치기 문장),
  `ko_34_camp_mend`(정비 단계: 롱소드 동 → 은, 피해 11 → 22, 돌아가기·정비하기, 고른 칸은 놋쇠색, 인벤토리 버튼 없음). 1층의 보상·전투는 동이라 띠와 테가 없다(맞다).
  스크린샷 실행이 바꾼 렌더 설정 셋(`UniversalRP.asset`, `UniversalRenderPipelineGlobalSettings.asset`, `ProjectSettings.asset`)은 `git checkout`으로 되돌렸다.

## Next Action (제안)

- 15단계(피로의 판정): 전투 중의 사건, 100 붕괴 판정과 고통·각성, 200 쓰러짐, 파티 쪽·전투의 피로 표시와 연출(목업), 시뮬(절벽 점검). Handoff는 `b15-fatigue-breakdown.md`.
