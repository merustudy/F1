# Slice D 31단계: 범위 — 용병 스탯

Snapshot: 2026-10-18 (완료. 31단계는 판정과 기록까지 끝났고, Round 81은 같은 날 사용자 "권장안 구현"으로 판정돼 32단계가 시작됐다: `completed/d32-stats-core.md`)

## Goal
Slice D(용병 스탯)의 범위 문서와 기획이 승인되고 기록된다: 판정을 【확정】 (Slice D)로 기획에, 범위·순서·Acceptance를 Architecture/09 "Slice D"에 적고, 구현 단계 32~35를 Roadmap에 더한다. 32단계 전에 스탯 표시의 목업(Round 81)을 승인받는다.

## State
- 31단계 판정: 2026-10-18 사용자 결정 셋(행운 대신 인내, 명중·회피는 이번에, 레벨업은 직업의 굴림만)과 민첩은 활·단검의 세기, 나머지 "일단 권장안으로". 표는 Roadmap "31단계의 승인".
- 같은 날 새 세션에서 기록을 끝냈다(아래 Done). **Round 81 판정 대기**(판정 일곱: `ArtPipeline/Archive/81-stat-display/README.md`). 커밋 안 함. 코드·데이터·Asset 변경 없음.

## Done
- 제안서 `Docs/Design/14_Slice_D_Scope_Proposal.md`(앞선 세션, §10·§11 포함). 머리말에 "판정됨"을 더했다.
- 【확정】 (Slice D): Design/02 §4(스탯의 세기·`ScalesWith`·피해 순서·치명타 + 민첩·카드), §5(명중·회피·빗나감·적의 `Accuracy`·`Dodge`), §6(빈사 − 체력); 04 §3(인내·정신력), §6(후보 편차); 05 §1(민첩 기본 무기 없음), §2(스탯 여섯·척도·직업 기본값 표·체력의 HP·레벨업의 굴림), §3; 07 §4(더할 데이터와 단계); 00(문서 목록, 현황, 미결 4, "Slice D" 표와 권장안 세부).
- Architecture/09 "Slice D"(목표, 구간별 범위, 구현 순서 0~4 = Round 81·32~35, Acceptance 1~9), Slice 순서, Deferred. CLAUDE.md §9.
- Roadmap: 갱신 문단, 날짜 표, "31단계의 승인"의 기록 줄, 진행 순서(→ 35), 31단계 절, 32~35단계(대기), 사후 검토 대기의 "Slice D의 권장안 세부".
- Round 81: `ArtPipeline/Archive/81-stat-display/`(목업 스크립트 `mock_stats.py`, 기준 스크린샷 `20261018-084651`, 목업 일곱, 비교 여섯, README).

## Open
- ~~Round 81의 판정 일곱~~ → 2026-10-18 사용자 "권장안 구현": 일곱 모두 권장안(Roadmap "Round 81의 승인").
- 권장안으로 메운 빈 곳(사후 검토): 직업 패시브의 크기는 스탯 밖, 인내는 사건마다 0.5 올림, 레벨의 HP 보너스 열은 34단계에서 지움(32·33 동안은 체력과 더함), 34단계의 옛 저장 레벨 처리.

## Verification
- 문서와 목업만이라 체인은 돌리지 않았다. `git diff --check` 깨끗함. 목업은 PNG를 열어 겹침·잘림을 확인했다(파티 팝업 목록의 한 글자 줄이 "N열"과 겹쳐 툴팁 방식으로 바꿈).

## Next Action (제안)
- Round 81 판정 → 32단계 시작(이 Handoff를 completed로, `completed/d32-stats-core.md`). 32단계는 Architecture/05·07·08·11·12의 해당 절을 먼저 고치고 데이터 → Domain → 저장 18 → 시뮬 전후 → 화면 → 체인 순서.
