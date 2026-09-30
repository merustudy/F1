# Stage 02 — Slice 범위 문서

Snapshot: 2026-09-30 (완료)

## Goal

`Docs/Architecture/09_VERTICAL_SLICE.md`가 Slice A의 구간별 범위, 구현 순서, Acceptance, Deferred를 정하고 사용자가 승인했다.
코드, 데이터, Folder, Package, Scene은 만들지 않는다.

## State

완료. 사용자가 권장안대로 승인했다(2026-09-30, Slice A 완료까지 일괄 승인에 포함).

## Done

- `Docs/Architecture/09_VERTICAL_SLICE.md` 작성.
- `CLAUDE.md` §2, §8에 등록하고 §9가 그 문서를 가리키게 했다.
- `Docs/Roadmap.md` 2단계를 완료로 고쳤다.

입력으로 읽은 것: `CLAUDE.md`, `Docs/StarterKit/06_RECOMMENDED_EXTRAS.md` §7, `Docs/StarterKit/CLAUDE.template.md` §9,
`Docs/Design/01`~`09`, `11`(전투 부분만).

## 승인된 범위 선택

아래 권장안이 그대로 승인됐다. 바뀌면 "구간별 범위" 표와 Deferred를 고친다.

1. 성장(경험치, 레벨업, 전직)을 Slice A에서 뺐다. 용병의 직업과 스탯은 고정이다.
2. 경제(재화, 고용, 상점)를 뺐다. 로스터 보충이 없으므로 시작 로스터를 파티 인원보다 많게 둔다.
3. 유물을 뺐다.
4. 로비 행동은 "쉬기" 하나만 넣었다(일수를 써서 피로도 회복).
5. 기한 도달 판정과 런의 승패를 뺐다. 일수는 흐르고 표시만 된다.
6. 노드 종류는 전투와 보스 둘만 넣었다. 아이템은 전투 뒤에 얻는다.
7. 시뮬은 전투와 원정 단위까지만 넣었다. 100일 전체 런 시뮬은 뺐다.

## Open — 다음에 오는 결정

- 규칙 정의(G16)와 결정론 세부(G8)는 7a에서 선택지로 제안한다. 목록은 문서의 "규칙 정의가 필요한 항목".
- 3단계 설계에서 G6(Application Manager), G13(해상도와 창), Template Package 정리, Domain의 Unity 비의존 보장 방식.

## Verification

- `git diff --check`와 새 문서의 공백 검사: 0건.
- 문서 안의 경로와 Link가 실제 파일을 가리키는지 확인했다.
- 검증 체인은 아직 없다(3단계에서 생긴다).
