# Slice D 49단계: 원정의 밀도

Snapshot: 2026-10-22 저녁 (사용자 "노드 고르기를 20번으로 줄이고 전투 밀도를 높이려고 해 … 빈사되면 바로 사망 여부를 판정 … 몬스터의 아이템을 많이 장착하게 하고 드랍은 몬스터 아이템이 아닌 용병 아이템과 지역화폐가 드랍되도록 변경 검토" → 제안서 Design/26 → **"판정할것 - 권장안 반영"** + "노드 20 중에 일반적으로 상점은 3~4번, 야영지는 약 2번 정도 들어 갈 수 있게 구성" → 다시 맞춤 → 기획·Owner 문서 → 데이터·코드·저장 27·Test → 시뮬 전후 → 체인. 같은 메시지의 "담금질을 백팩 배틀즈 방식으로"는 검토 Design/29)

## Goal
노드 고르기를 20번으로 줄이고 싸움 하나를 무겁게 한다(몬스터가 더 들고, 파티는 용병 아이템 드랍과 같은 아이템 드랍으로 빨리 자란다). 빈사는 다키스트 던전 1처럼 유예가 없다. 광산은 띠 안에, 런 띠는 43단계 2차에서.

## State
- 결정: 00 "Design/26의 판정"(열여섯 + 노드 구성). 기획 03 §1·§4·§5, 02 §6, 04 §3·§5, 05 §2, 07, 08 §56. Owner: Architecture/05·07·08·09·12, CLAUDE.md §9.
- 데이터: `DungeonData`(광산 19층·정예/야영지 7층부터·야영지 층 19·상점 층 3·무작위 상점 5층부터 22%·야영지 10%·1성 6층·2성 13층·HP 복리 8%·일반 드랍 100%), `EnemyGroupData`(무리의 층 범위 비례), `EnemyData`(`DeepItems` 4·9·14층), `BalanceData`(`DogGraceMs` 0, `DropCount` 2, `EliteDropCount` 3, 새 키 `DropOwnedPercent` 50, `CoinsPerEnemy`·`CoinsPerFloor` 2, `GoldPerFloor` 3, `ExpClear` 16, `FatigueBattleEntry` 3), Generated JSON 넷.
- 코드: `BalanceData.DropOwnedPercent`, `ExpeditionRules.DropLoot`의 같은 아이템 드랍(`OwnedDropCandidates`, 스트림 `loot-owned`), 저장 27(`RunSaveData.CurrentSchemaVersion`, `RunSaveMigrator.From26To27`·`OldMineFloor`·`NewMineFloor` — 진행 중인 광산 원정은 후퇴로 정산).
- Test: `ExpeditionRulesTests.DropLoot_WithTheOwnedChance_…`, `RunSaveMapperTests.Migrate_From26_…` 둘(출고 데이터로), `ShippedDataTests`(광산 몬스터 묶음 4·9·14, 일반 드랍 100%), `TestData`·`TestCsv`(키 `DropOwnedPercent` 0), 저장 버전 단언 둘.
- 화면(판정 16)은 Round 96 목업 뒤 50단계와 함께 구현했다(사망 문장에서 "유예가 끝난 뒤"를 뺌, 코인 더미 — `completed/d50-unique.md`). 빈사 아이콘 옆 줄은 유예가 0이라 빈사에 드는 순간부터 지금의 "피격마다 사망 N%"가 뜬다(코드 그대로, 고칠 것 없음).
- 커밋(2026-10-22 세션 정리, 50단계와 함께): 기획 `9d9ef95`, 구현 `b2a036e`(데이터·Domain·저장 27·시뮬·EditMode), 화면 `9351cfb`, Round 96 `3b51edc`, 푸시 origin/feature/slice-a.

## Verification
- `transform` → `validate` OK(8 jobs, 58 items, 9 enemies).
- 시뮬 전후 08 §56(같은 시드, 저장소 빌드): 광산 56.0 → 64.9% / 죽음 0.39 → 0.44 / 붕괴 40 → 32%, 합치기 7.2 → 11.1, 런 균형형 63.5 → 90.4%·안전형 64.5 → 96.5%(띠 위 — 2차에서), 심연 새 파티 0.6 → 75.0%. 후의 광산·런 출력은 복사본 변형과 byte 단위로 같다.
- 체인 `20261022-161950`: setup·sim OK, EditMode 1106/1107 — 실패 하나는 저장 버전을 26으로 못 박은 단언(`Save_KeepsTheStats_…`, 27로 고침). 그 체인의 PlayMode는 실패 넷(첫 전투의 드랍이 하나라는 옛 전제 셋, 드랍이 용병 아이템 풀에서 무작위라 앞열이 요구를 못 채운 것 하나 — Test가 드랍을 정해 두고 드랍 2개를 전제로 고침).
- 마지막 전체 체인 `20261022-164808` **CHAIN OK**: setup·sim OK, EditMode **1107/1107**, PlayMode **97/110**(건너뜀 13은 스크린샷 Test). `Assets/InitTestScene*` 없음, `git diff --check` 깨끗. (그 사이 `20261022-164654`는 고친 Test의 지운 변수로 컴파일 실패.)

## Open
- **43단계 2차(런 띠)**: 판정 14 — 출발점 군주 600 + 광산 7일(복사본 측정: 균형형 67.1%·전멸 24.6%·41.0일, 안전형 78.5%·결전행 13.3%). 같은 날 앞선 2차 권장(군주 345 + 7일)은 거둠.
- 원정당 죽음 0.44(띠 0.3~0.4 위 가장자리) — 유예 없음의 성질(판정 13, 사후 검토).
- Acceptance 2(원정 15~30분): 원정이 약 9분 — 직접 플레이 뒤 다시(판정 4).
- 사후 검토: 권장안으로 정한 것(몬스터 아이템의 고르기, 같은 아이템 드랍의 후보 규칙, 저장 27의 정산, 코인 값).

## Next Action
- 이 단계는 끝났다. 다음은 43단계 2차(런 띠, 출발점 군주 600 + 광산 7일 — `active/d43-tuning.md`)와 직접 플레이(원정의 길이, Acceptance 2).
