# Slice C 28단계: 최종 던전과 최종 보스 — Abyss Lord와 스켈레톤 호위, 페이즈

Snapshot: 2026-10-16 (27~29단계 일괄 승인 아래 권장안으로 구현. 체인은 아래 Verification)

## Goal

Architecture/09 "Slice C" 구현 순서 4가 참이다: 최종 던전 `abyss`에 Abyss Lord와 스켈레톤 호위가 제 수치로 서고, 군주는 HP 75/50/25%에서 아이템 묶음을 바꿔 든다. 심연의 그림(전신·자세·배경·아이콘)이 배선되고, 시뮬 `run`이 최종 던전의 결과를 따로 보고한다.

## State

- 2026-10-16 사용자 "이어서 … 슬라이스 완성을 위한 나머지 절차를 네 권장안에 따라 완성할 때까지 알아서 진행(필요시 이미지 api 사용)" → Roadmap "27~29단계의 일괄 승인". 28단계를 권장안으로 구현했다.
- 권장안으로 정한 세부는 Roadmap "사후 검토 대기"의 "28단계의 권장안 세부", Design/02 §9, 03 §1, 08 §26, Round 73 README.
- 다음은 29단계(맞추기와 직접 플레이): 같은 일괄 승인으로 이어서. 100일 띠의 결정과 숫돌 값 복귀(1 → 8)는 29단계.

## Done

- 데이터: `EnemyData.csv`의 선택 열 `Phases`(`EnemyPhase`: 문턱 1..99 내려가는 차례, 묶음 1개 이상·높이 합 ≤ 틀, 아이템 참조 검증; `CsvRow.PhaseList`, `EnemyMapper`), `abyss_lord`(Lv14·HP 280·식칼, 페이즈 셋)·`skeleton_guard`(Lv8·HP 32·녹슨 칼 220%·버클러 120%), `EnemyGroupData.csv`의 `abyss_bones`(둘 1~5층)·`abyss_bones_three`(셋 4~5층)·`abyss_lord_court`(군주+호위 둘, 보스; 임시 `abyss_watch`·`abyss_lord_stand_in` 삭제), `ItemData.csv`의 `abyss_cleaver`(앞열 근접 6-14, 4초)·`abyss_howl`(전체 2.5-5, 6초)·`abyss_grasp`(뒷열 3.5-8, 5초), `DungeonData.csv`의 `abyss` 배경. `StaticData`: `FinalBossLevel`은 최종 던전의 보스 무리에 선 적만, 그 위 레벨은 없음. Generated 넷.
- Domain: `BattleUnitSetup.Phases`·`BattlePhaseSetup`(문턱·아이템·`Layout`), `BattleEventKind.PhaseChanged`(Target, A 페이즈, B 문턱, C HP), `BattleUnit.Phase`·`Items`·`EquippedNow`·`Layout`·`EnterPhase`, `BattleEngine.EnterPhasesReached`(`ApplyDamage`의 `Damaged` 뒤, 살아 있을 때: 묶음 교체, 쿨다운은 그 순간부터, 여러 문턱은 차례로), `ExpeditionRules.EnemySetup`의 페이즈(세기 배율 같이, `BoardLayout.Stacked`).
- 화면: `BattlePresenter`의 `PhaseChanged`(보드 `Rebind`, "페이즈 N" `Fx.Phase` — N은 문턱의 차례(75% = 1; 첫 스크린샷이 "페이즈 2"로 보여 +1을 뺐다), 흔들림 6), `BattleBoardView.Rebind`(`Layout ?? Stacked(EquippedNow)`), `BattleLogText`의 `Log.PhaseChanged`. 문구 `UI_StaticText.csv`.
- 그림: Round 73(`ArtPipeline/Archive/73-abyss/`): `STYLE_RUNTIME.md` `## Dungeon: abyss`, Roster(enemy·background·item·enemy_attack·enemy_hit), 생성 11회(상한 12. 군주 1차 거절 → 좁은 자세 2차), 자세 배율은 고리(투구·목걸이)·방패·리벳으로 잼(`measure_scale.py`), 배선: `Assets/@Art/Unit/Enemy/`·`Pose/Enemy/`·`Background/Dungeon/abyss.png`·`Item/abyss_*.png`, CSV의 `Figure`·`Background`·`Icon`.
- 시뮬: `run`의 "final dungeon" 줄(런당 원정·클리어·전멸·후퇴·전사자·클리어한 런). 결과 Design/08 §26(안 셋, 채택: 새 파티 18.8%, 300런 59.3%).
- Test: EditMode `BossPhaseTests`(페이즈 진입과 쿨다운, 한 타에 둘, 재생 동일, 데이터 거부, Setup의 빈 목록과 아군의 null), `StaticDataTransformerTests`(페이즈 매핑·거부), `StaticDataValidationTests`(최종 보스 레벨), `ShippedDataTests`(`ShippedAbyssLord_CarriesThreeFallingPhases_IntoTheBattleSetup`: 군주의 레벨·페이즈 셋·무리·전투 Setup). `TestCsv`의 `Phases` 열. PlayMode `UiScreenshotTests.AbyssBoss_Korean`(`UiTestUtil.EnterTheAbyssBossBattle`: 광산 클리어를 런에 써 심연을 열고 원정 화면의 표식을 골라 출발, `FightThroughToTheBoss(bossPowerPercent)`: 보스 노드 앞에서 챔피언의 무기를 600%로 낮춰 군주가 몇 타를 버팀 — 군주·호위·배경, 첫 페이즈의 순간).
- 문서: Architecture/05(`Phases`, `FinalBossLevel` 검증), 08("최종 보스의 페이즈"), 12(페이즈 연출), Design/00·02 §9·03 §1·07 §4·08 §26, Roadmap(28 완료, 임시 조치의 심연 행 삭제, 사후 검토), README 73.

## Open

- 사후 검토: 군주·호위·아이템의 값과 페이즈 묶음, 무리의 층, Round 73의 판정 전부(머리가 큰 자세는 표정의 과장으로 둠), 페이즈에 소리 없음(14_SOUND의 자리는 비워 둠).
- 승리 59%는 02 §9의 목표(82%/75%)보다 낮다: 29단계의 띠와 맞추기에서 본다.
- 도구: `run_roster.py`가 자세를 "실패"로 센다(맞춘 파일은 `fit_pose.py`가 만듦) — README 73 "도구의 메모".

## Verification

- `transform`·`validate` OK(6 jobs, 25 items, 9 enemies). 시뮬 Design/08 §26.
- 체인 `20261016-224451`(setup·sim OK, EditMode 912/916 — 실패 넷은 모두 Test 쪽: 보스 페이즈 Test의 시간표(영웅이 초마다 300을 때려 5초 전에 보스가 죽음 → 4초마다로), 보스 노드를 고르지 않고 `BeginBattle` → `BuildBattleSetup`, 근접 목록에 식칼, 시험 데이터의 `chief`가 두 보스 무리에 섬 → 첫 부분에서 `pit_lord`를 비움; PlayMode 87/97 — 건너뜀 10은 그래픽이 있어야 하는 스크린샷 Test) → 고친 뒤 `20261016-225935`(EditMode **917/917**, PlayMode **87/98** — 건너뜀 11, 심연 보스 스크린샷 Test `AbyssBoss_Korean`을 더함).
- 그래픽 스크린샷 `~/Library/Caches/F1/screenshots/20261016-232120-abyss`(11/11, PNG 109장): `ko_63_battle_abyss_boss`(심연 6층의 보스 전투 — 군주 150%가 1열, 호위 둘, 심연 배경, HP 280/32/32, 군주의 보드에 식칼)와 `ko_64_battle_abyss_phase`(7.2초, 아스트리드의 치명타 77로 HP 179/280 → "페이즈 1"이 떠오르고 자막 "심연의 군주: 페이즈 1 (HP 75%) — 아이템이 바뀐다", 보드가 식칼 + 울부짖음으로 다시 묶임). 첫 실행은 "페이즈 2"로 보여 번호를 문턱의 차례로 고치고 다시 찍었다.
- 표기 수정 뒤 UI 스크립트가 바뀌어 Prefab 도장이 낡았다(`FindProblems_ForShippedPrefabs`) → `setup editmode` 재실행 `20261016-232323`(setup OK, EditMode **917/917**).

## Next Action (제안)

- 세션: 체인과 스크린샷(심연의 보스 전투: 군주·호위·배경·페이즈 글) 뒤 커밋하고 29단계로.
