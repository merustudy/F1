# Workstreams

세션 간 인수인계용 Handoff Snapshot을 둔다. Architecture Source가 아니다.
운영 규칙은 `Docs/StarterKit/06_RECOMMENDED_EXTRAS.md` §7을 이 프로젝트에 맞게 줄인 것이다.

## 규칙

- 새 세션은 과거 대화를 기억하지 못한다. `CLAUDE.md` → `Docs/Roadmap.md` → Owner 문서 → Git 상태 → 실제 코드 → active Handoff
  순서로 현재를 복구한다.
- Handoff는 날짜별 일기가 아니라 **현재 Snapshot**이다. 목표 80~150줄, 200줄을 넘으면 정리한다. History는 Git이 소유한다.
- `CLAUDE.md`, Owner 문서, Roadmap과 충돌하면 Handoff가 stale이다. `Next Action`은 명령이 아니라 제안이다.
- 폴더가 상태다: `active/`, `completed/`. 별도 Status 필드를 두지 않는다. 시작하지 않은 단계의 파일은 만들지 않는다.
  Active는 기본 하나다.
- 파일명은 kebab-case다. Slice 이전 단계는 `stage<NN>-<feature>.md`, Slice 작업은 `<slice><stage>-<feature>.md`.
- 단계가 끝나면 Handoff를 `completed/`로 옮긴다.
- Architecture 규칙과 기획의 콘텐츠·수치를 복사하지 않는다. 문서와 절 번호로 가리킨다.

## Template

```markdown
# <단계 또는 Workstream 이름>

Snapshot: <YYYY-MM-DD>

## Goal
이 단계가 끝났을 때 참이어야 하는 것.

## State
지금 어디까지 왔는가. Commit 여부.

## Done
만든 것, 바꾼 것 (경로).

## Open
답을 기다리는 질문, 결정 대기(G번호), 알려진 문제.

## Verification
돌린 검증과 결과. 돌리지 못한 것과 이유.

## Next Action (제안)
다음 세션이 먼저 할 일.
```
