# Slice D 31단계: 범위 — 용병 스탯

Snapshot: 2026-10-18 (세션 정리. 31단계는 판정까지 끝났다: Roadmap "31단계의 승인". 기획의 【확정】 기록과 Architecture/09 "Slice D"는 다음 세션의 첫 일이라 active에 둔다)

## Goal
Slice D(용병 스탯)의 범위 문서와 기획이 승인된다: `Docs/Design/14_Slice_D_Scope_Proposal.md` §9의 판정할 것 열다섯에 사용자가 답하고, 고른 것을 【확정】 (Slice D)로 02·04·05·07과 00 "Slice D" 표에 적고, Architecture/09 "Slice D"(범위·순서·Acceptance)를 확정한다. 그 뒤 구현 단계 32~를 Roadmap에 더한다.

## State
- 2026-10-18 사용자 "31단계 시작, 주제는 용병 스탯, 먼저 참고 게임 분석(우리 모험가 길드·배틀 브라더스·다키스트 던전), 검토 후 보고". 사용자 예시(OAG 차용: 힘·민첩·체력·지식·정신력·행운)는 확정안이 아니다.
- 제안서 Design/14를 썼다. 같은 날 둘째 지시("11은 b+d식 변형 / 행운을 다른 성질의 스탯으로 / 명중·회피도 이번에 추가 예정 / 일단 이 사안만 검토")로 §10(레벨업 V1, 행운 → 인내, 명중·회피의 식)을 더했다. 셋째 지시(레벨업은 굴림만 / 인내 + 민첩의 세기 축 / 명중·회피 이번에)로 §11을 더했다(명중은 무기의 스탯, 회피·치명타는 민첩, `dex` 아이템 넷). **판정 대기.** 커밋 안 함. 코드·데이터·Asset 변경 없음.

## Done
- 상태 복구: feature/slice-a 7e73568(origin과 같음), Slice C 완료, active Handoff 없음.
- 참고 게임 조사(위키·개발자 포럼, 출처는 Design/14 §3): OAG는 STR·DEX·INT·END·WIS·WP 여섯, 행운 없음, 힘은 +1씩 더함, 성장은 직업 등급 %의 확률. BB는 여덟 수치가 곧 전투 수치, 셋 고르기 성장, 출신 범위의 무작위 시작값, 행운 없음. DD는 전투 수치가 스탯, 결의 레벨은 저항 +10%와 등급 해금, 각성 25%.
- 코드의 걸릴 자리 확인: `BattleEngine.Activate`(굴림 → 별 → 치명타 → 패시브), `RollCritical`(아이템 `CritChancePercent`), `BreakDown`(`VirtueChancePercent`), `FatigueRules.DogDeathChancePercent`, `LevelRules.MaxHpOf`, `ExpeditionRules`의 전리품 `DropWeight`, `RunRules.DrawRecruits`(`Recruit` 스트림), `MercenaryState`·`RecruitState`, 저장 17.
- 문서: Design/14(새), Design/00 문서 목록, Roadmap(31단계, 진행 순서, 날짜 표), 이 Handoff.

## Open
- 열린 결정 없음. 사용자 "일단 권장안으로 다음 세션에 진행"(2026-10-18)으로 §9·§10-4·§11-4의 판정이 모두 끝났다(사용자 결정 셋: 인내, 명중·회피 이번에, 레벨업은 굴림만; 나머지 권장안).
- 다음 세션이 할 일(아직 안 함): 기획 【확정】 (Slice D) 기록(02 §4·§5, 04 §3·§6, 05 §2, 07, 00 "Slice D" 표), Architecture/09 "Slice D"(범위·순서 32~35·Acceptance), CLAUDE.md §9, Roadmap 32~35단계, 이 Handoff를 completed로. 32단계 전에 스탯 표시 목업(Round 81: 막사 줄·후보·파티 팝업·정산 줄·카드의 "명중 N%"·전투의 "빗나감").

## Verification
- 돌린 것 없음(문서만). `git diff --check` 깨끗함(Roadmap 끝의 빈 줄을 고침).

## Next Action (제안)
- 다음 세션 시작 명령: "CLAUDE.md → Docs/Roadmap.md(31단계의 승인) → Docs/Design/14_Slice_D_Scope_Proposal.md(§5·§10·§11) → Docs/Workstreams/active/d31-scope.md 순서로 읽고 상태를 복구해줘. 31단계는 2026-10-18에 권장안으로 판정됐다. 판정대로 기획에 【확정】 (Slice D)를 적고 Architecture/09 "Slice D"와 CLAUDE.md §9를 쓴 뒤, Round 81(스탯 표시 목업)을 보여주고 승인 뒤 32단계를 시작해."
