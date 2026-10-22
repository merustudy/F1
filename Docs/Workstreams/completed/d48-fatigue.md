# Slice D 48단계: 피로 개정

Snapshot: 2026-10-22 오후 (사용자 "피로도의 최대치를 100으로 … 정신 붕괴 또는 각성 … 목록 제안, 이전 피로도 수치에 따른 쿨타임 감소는 일단 동작 안되도록" → Design/21 → 사용자 수정안 "최대 200, 200 탈진, 0에서 풀림, 과로, 각성 중 200이면 해제·40 — 검토, 개선할 점(DD1 차용 우려)" → §8 → **"위 제안대로 구현"** → 기획·Owner 문서 → 구현 → 시뮬 전후 → 체인)

## Goal
피로가 다키스트 던전 1의 뼈대(0~200, 100 판정, 200 탈진)를 지키되 우리 것(과로, 야영지의 풀림, 수치형 상태)을 갖는다. 사용자가 정한 규칙과 Design/21 §8의 권장안 R1~R7을 구현하고, 런 띠는 43단계 2차에서 되돌린다.

## State
- 규칙(04 §3 【확정】 48단계, 판정 표 00 "48단계의 판정"): 최대 200 그대로, 100에서 정신 붕괴 또는 각성, 200 탈진(빈사, 이미 빈사면 사망, 200에 머묾), 정신 붕괴는 피로 0(`AfflictionRecovery`)·야영지의 쉬기·쉼터의 치료로 풀림, 과로(정신 붕괴인 채 100 이상이면 모든 아이템 쿨다운 +`OverworkCooldownPercent` 25%), 각성 중 200이면 해제하고 40.
- 상태 아홉(`FatigueStateData.csv`): 정신 붕괴 공포(주는 피해 −25%)·절망(받는 회복 −50%)·무모(빈사 사망 +25%p)·난폭(전투마다 동료 피로 +3, 회피 −10%p, 피해 +10%)·비이성(명중 −15%p), 각성 강인(받는 회복 +50%, 빈사 −15%p)·용기(오르는 피로 −33%, 전투마다 동료 피로 −3)·집중(명중 +10%p, 치명타 +8%p)·강력(피해 +25%). 상태의 쿨다운은 모두 0(열과 엔진은 남김).
- 화면: 이름(정신 붕괴·탈진), 상태 이름 뒤 "· 과로"(전투의 상태 줄, 직업 줄), 야영지 쉬기 카드의 셋째 줄 "정신 붕괴 해제"(첫 글 "정신 붕괴가 풀린다"는 세 카드에서 "풀린/다"로 끊겨 줄였다), 쉼터 창 아래 줄 "치료는 날이 가지 않고 정신 붕괴를 푼다", 쉼터 말풍선은 정신 붕괴도 셈, 로스터 글은 정신 붕괴면 100 아래에서도 붉음, 로그 둘(각성이 다함, 상태가 주는 동료 피로). 목업 없이 글만(권장안 — 사후 검토).
- 저장: 버전 26 그대로. 검증만 "정신 붕괴는 피로가 `AfflictionRecovery` 위"로 완화(옛 파일은 모두 지남).
- 시뮬 전후(08 §55): 광산 58.6 → 56.0%(띠 안), 런 균형형 68.5 → 63.5%·안전형 67.6 → 64.5%(띠 아래), 전멸 24.5 → 30.9%(띠 위), 끝난 날 36.6 → 38.2일.
- 커밋: 아래 "State (커밋)".

## State (커밋)
- 2026-10-22 세션 정리: `e199898` 기획·Architecture(48단계 + Round 95), `f6e6fbc` 48단계 구현(데이터·Domain·저장 검증·화면의 글·EditMode Test·시뮬), `d90b4f7` Round 95(막대의 둘째 채움·칸 빌더·Prefab·PlayMode Test·목업 기록), 정리 커밋(Roadmap·CLAUDE.md·Handoff). 푸시 origin/feature/slice-a. Handoff는 completed로 옮겼다(48단계 완료). 스크린샷이 바꾼 Unity 설정 셋은 되돌렸다.

## Done
- 기획: Design/04 §3(【확정】 48단계 다섯 곳 — 풀림·과로·각성 중 최대, 효과 아홉, 목록, 탈진, 체력의 최대 피로 닫음), 07(`FatigueStateData` 열), 00(결정 줄, "48단계의 판정" 표, 21 줄), 21(머리말 판정됨, §8), 08 §55.
- Architecture: 05(선택 열 여섯, 키 둘), 07(검증), 08(피로 절의 48단계), 09(구현 순서 17), 12(이름·"· 과로"·야영지 카드·쉼터·로그·탈진의 글).
- 데이터: `BalanceData.csv`(키 둘), `FatigueStateData.csv`(열 여섯, 행 아홉), Generated JSON 둘.
- 코드: `FatigueStateData`, `DefinitionMappers`(선택 열), `BalanceData`(키·검증), `FatigueRules`(`AfflictionEnds`·`IsOverworked`·`Cure`), `StatRules.FatigueGain`(상태 %), `BattleEngine`(명중·회피·치명타·피해·피로 %·동료 피로·과로·각성 중 최대), `BattleTypes`(원인 `virtue_end`), `ExpeditionRules`(야영지의 풀림, 진입 피로), `RunRules.Treat`(풂), `RunSaveMapper`(검증), UI(`UiText`·`UiKeys`·`BattleLogText`·`BattleUnitView`·`PartyColumnView`·`RosterEntryView`·`LobbyScreen`·`ExpeditionScreen`·`NodeMapScreen`), 문구 `UI_StaticText.csv`, 시뮬(정책·보고).
- Test: 새 `FatigueRevisionTests`(열둘), `FatigueBreakdownTests`(옛 풀림 둘을 지우고 정산의 쉬는 날을 새 규칙으로), `LobbyEconomyTests`(치료가 풂), `RunSaveMapperTests`(검증 둘), PlayMode `UiFlowTests`(직업 줄·상태 줄의 "· 과로" 셋), `TestData`·`TestCsv`(키 둘: 회복 0, 과로 0 — 과로 Test는 25).

## Open
- 런 띠(균형형 63.5·안전형 64.5%, 전멸 30.9%, 끝난 날 38.2일): 43단계 2차 맞추기(`active/d43-tuning.md`). 레버 후보: 쉬는 날의 회복, 치료의 값·양, 원정의 날.
- 사후 검토: 권장안으로 정한 것(Roadmap "사후 검토 대기"의 48단계 줄) — 특히 과로의 표시(글만), 야영지 카드의 셋째 줄, 쉼터 말풍선의 글 길이.
- (닫힘 2026-10-22) **Round 95**(피로 칸의 두 바퀴: 0~100 보라, 100~200은 그 위에 빨강): 사용자 "권장안 구현" → 구현, 체인 `20261022-135553` CHAIN OK, 스크린샷 대조. `ArtPipeline/Archive/95-fatigue-overlay/README.md`.
- 새 상태 넷(난폭·비이성·용기·강력)의 그림이나 아이콘은 없다(상태는 글로만 보인다. 그림 라운드는 요청 때).

## Verification
- `transform` → `validate` OK(8 jobs, 58 items, 9 enemies).
- 시뮬 전후 08 §55(같은 시드; 전은 개정 전 코드·데이터의 복사본 — 첫 12줄이 개정 전 저장소 빌드와 같음).
- 전체 체인 `20261022-112000`: setup OK, sim OK(8 jobs, 58 items, 9 enemies), EditMode **1104/1104**(새 열둘, 지운 둘), PlayMode 96/110 — 실패 하나 `Camp_ItsWindowOpensOverTheMap_AndRestingHealsAndGoesOn`(야영지 카드의 기대값이 옛 두 줄: 의도한 셋째 줄이라 Test를 고침).
- PlayMode 재실행 `20261022-114347` **CHAIN OK**: 97/110(건너뜀 13은 그래픽 스크린샷 Test). `Assets/InitTestScene*` 없음, `git diff --check` 깨끗.
- 스크린샷 `Tools/screenshots.sh`(13/13, `~/Library/Caches/F1/screenshots/20261022-d48`): `ko_36_map_state` 직업 줄 "전사 · 공포 · 과로"(붉게)와 설명 줄 "공포: 주는 피해 -25%", `ko_33_camp` 쉬기 카드 세 줄, `ko_52_camp_forge`(세 카드)에서 셋째 줄이 "풀린/다"로 끊겨 문구를 "정신 붕괴 해제"로 줄였고 쉼터 창 아래 줄에 "정신 붕괴를 푼다"를 더함(`en_52`는 "Breakdowns / end"가 카드 안에 들어감). `ko_38`·`ko_39`(각성 강인)·`ko_59`(쉼터) 그대로. 쉼터 말풍선은 캡처가 없다(사후 검토).
- 문구를 고친 뒤 마지막 전체 체인 `20261022-121350` **CHAIN OK**: setup·sim OK, EditMode **1104/1104**, PlayMode **97/110**(건너뜀 13은 스크린샷 Test). `Assets/InitTestScene*` 없음, `git diff --check` 깨끗.

## Next Action
- (끝) 48단계와 Round 95. 이어지는 일은 `active/d43-tuning.md`(43단계 2차 맞추기).
