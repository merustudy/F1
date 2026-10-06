# Slice B 12단계: 피로의 뼈대와 장비 피로

Snapshot: 2026-10-06

## Goal

피로도가 0에서 쌓이는 값(0~200)이 되고, 전투에 들어갈 때마다 기본 피로와 장비 피로(기본 무기가 아닌 무기·방어 장비 하나에 +1)가 쌓이며,
로비에서 쉬는 날에 내려가고 원정 사이에 남는다. 장비 피로가 파티 쪽 보드에 보인다(Round 32 B1). 붕괴 판정·쓰러짐은 15단계.
범위와 순서: Architecture/09 "Slice B", 규칙: Design/04 §3.

## State

- 11단계(Slice B 범위) 완료: 사용자 "권장대로 구현"(Roadmap "Slice B의 승인"). Design·Architecture/09·Roadmap·CLAUDE.md §9 갱신.
- 12단계 완료(2026-10-06). 로비의 피로도는 사용자가 고른 "C"(칸 열 개, Round 33)로 구현했다. 보드 칸은 Slice B에서 늘리지 않는다("D").
- 커밋 안 함. 브랜치 `feature/slice-a`(새 브랜치는 만들지 않았다).

## Done

- 기획: Design/00 "Slice B", 01 §6, 02 §4, 03 §1·§2·§4·§5, 04 §3·§9, 07 §3·§4. 범위: Architecture/09 "Slice B". Roadmap 11~16단계.
- Domain: `FatigueRules`(새 파일), `EquippedItem.IsBase`, `ExpeditionMember.Fatigue`, `PartyMember.Fatigue`, `ExpeditionRules.Create`(기본 무기 표시, 피로를 받음)·`BeginBattle`(전투에 들어가는 비용),
  `RunRules`(새 런 0, 출전 문턱 없앰 — `DepartCheck.NotEnoughFatigue` 삭제, 쉬는 날 내려감, 정산이 구성원의 피로를 로스터에 씀), `SettlementReport.SurvivorFatigue`(전 `FatigueCost`).
- 데이터: `BalanceData`의 `MaxFatigue` 100 → 200, 새 키 `FatigueBattleEntry` 3·`FatigueEquipment` 1. `DungeonData`의 `FatigueCost` 열 삭제. Generated JSON 다시 만듦.
- 저장: `run.json` 버전 4(`MemberRecord.Fatigue`, `ItemRecord.Base`, 검증: 구성원 피로 범위, 기본 무기는 직업의 무기여야). `From3To4`(남은 피로 → 쌓인 피로 = 100 − 값, 구성원은 떠날 때의 값, 직업의 무기 = 기본 무기),
  `RunSaveMigrator.Migrate`가 Static Data를 받는다.
- 시뮬: 원정 보고에 "살아 돌아온 용병의 피로(평균, 최고)"와 "전투마다 장비 피로".
- 화면: 조각 `Frame/fatigue_tag`(그린 것, `Archive/32-equipment-fatigue/draw_tag.py`), `UiArt.FatigueTag`, `UiPalette.Fatigue`, 칸의 "+1" 표(`ItemSlotView`)와 머리의 합계(`PartyColumnView`),
  설명의 피로(`UiText`), 로비의 일수·쉬기 문구, 정산의 "생환자의 피로도: 이름 값", 문구 4개 추가·4개 변경·1개 삭제.
- Test: `FatigueRulesTests`(새), RunRules·GameLoop·ExpeditionManager·RunSave·RunSaveMapper(3 → 4 변환, 2 → 4)·Definition·Transformer Test 갱신, PlayMode `PartySide_EquipmentShowsItsFatigue...`.
- Owner 문서: Architecture/07 "Schema Version"·검증, 08 "피로", 11 "전투 진행"·"귀환 정산", 12 "파티 쪽"·Test, 13 조각.

- 로비의 피로도(Round 33 "C"): `BalanceData.FatigueBreakdown` 100(붕괴의 문턱), `UiPalette.FatigueBar`·`FatigueDanger`, 로스터 줄의 칸 열 개(`UiPrefabSetup.Lobby`, `RosterEntryView`),
  PlayMode `Lobby_FatigueShowsAsTenPips_RedFromTheBreakdownOn`. 보드 칸 "D"는 Design/02 §4, 00, Architecture/09에 적었다.

## Open

- 피로 수치(`FatigueBattleEntry` 3, 로비 회복 10/일)는 출발값이다. 13단계(15층)에서 시뮬로 다시 본다.

## Verification

- `dotnet build Tools/Sim` 오류 0. `transform`·`validate` OK. 시뮬(기본 파티, balanced, 3,000회, 시드 1): 81.7% / 0.25로 전과 같다(피로는 아직 전투를 바꾸지 않는다).
  살아 돌아온 용병의 피로 평균 12.82, 최고 18, 전투마다 장비 피로 0.23.
- 체인 1(2026-10-06 01:58, `~/Library/Caches/F1/chain/20261006-015847`, 버전 3을 거부하던 코드): setup OK(Prefab 둘·Stamp, 문구 표, 폰트 아틀라스, `fatigue_tag` Import), sim OK, EditMode 667/667, PlayMode 61/61(`[Explicit]` 8개 제외).
- 체인 2(02:05, `20261006-020548`, 3 → 4 변환으로 바꾼 뒤): sim OK, EditMode 668/668, PlayMode 61/61(8개 제외). `git diff --check` 깨끗, `Assets/InitTestScene*` 없음.
- 스크린샷 47장(8/8, `~/Library/Caches/F1/screenshots/20261006-b12`): 새 `ko_30_map_fatigue`·`en_30_map_fatigue`(1열 보드에 찾은 단검·버클러·약초 주머니를 잠시 얹은 노드 맵)가
  목업 B1과 같다: 단검·버클러에만 "+1", 머리 띠 "피로 +2", 고른 단검의 줄 끝 "전투마다 피로 +1"(두 줄로 넘어감). 로비는 "2일 소요", "쉬기 (피로도 -10 · 1일)", 로스터 "피로도 0/200".

- 체인 3(02:58, `20261006-025824`, 로비의 칸 열 개 뒤): setup OK, sim OK, EditMode 668/668, PlayMode 62/62(8개 제외).

## Next Action (제안)

- 13단계(긴 원정). Handoff는 `b13-long-expedition.md`.
