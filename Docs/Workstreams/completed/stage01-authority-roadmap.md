# Stage 01 — CLAUDE.md, Roadmap, git 설정

Snapshot: 2026-09-30 (완료)

## Goal

root `CLAUDE.md`와 `Docs/Roadmap.md`가 있고, git 설정이 StarterKit 예시와 병합돼 있고,
사용자가 결정할 질문·불일치·충돌이 전달되고 답이 문서에 반영돼 있다.

## State

완료. 사용자가 질문에 답했고(2026-09-30) 결정을 문서에 반영했다. Commit하지 않았다.

## Done

- `CLAUDE.md`: `Docs/StarterKit/CLAUDE.template.md` 기반. Prefix `F1`.
- `Docs/Roadmap.md`: 결정 관문(미결 / 결정됨), 진행 순서, 단계별 범위.
- `.gitignore`, `.gitattributes`: `Docs/StarterKit/tools/*.example`과 병합.
  - 가져오지 않은 줄: DOTween Pro, AllIn1SpriteShader, `Tools/BalanceSim` (C1의 Vendor·도구 경로).
  - `*.asset`은 `text=auto`(바이너리 `.asset`의 줄바꿈 변환 방지), `*.scenetemplate`에도 `-whitespace`.
- `Docs/Workstreams/README.md`.
- `Docs/Design`: 2026-09-30 결정 반영(00, 01, 02, 03, 04, 07, 08, 09, 10, 11). 날짜를 붙여 고쳤고 새 규칙은 쓰지 않았다.

## 2026-09-30에 받은 결정

내용은 `Docs/Roadmap.md` "결정 관문 — 결정됨"이 소유한다. 요약:

- 원본 자료 없음. 규칙 재정의, 시뮬은 새로 만든다. 기존 수치는 재검증 전까지 출발값.
- C# Domain이 기준 구현. 데이터 원천은 CSV.
- 포션과 후퇴 도입, Slice A 포함. Slice A = 핵심 루프 한 바퀴 + 저장.
- Locale `ko-KR`(기본) + `en-US`. 대상 OS Windows + macOS. `companyName` = `funitup`.
- 다크 중세 흉상, 대규모 로스터, 소영주·공성전은 미채택.
- Save는 엄격한 쪽(확정 시 저장, 손실 되돌리기 불가). Git LFS 사용(6단계에서 설정).
- Roadmap 조정 제안 7개 승인. 기획문서 간 불일치는 열어 두는 방향의 권고안으로 처리.
- 기획은 크게 바뀔 수 있다고 가정하고 진행한다.

## 저장소 상태 (1단계 시점)

Unity 6000.3.9f1, 2D URP Template. `Assets`에 스크립트와 `@` 폴더 없음. Scene은 `SampleScene` 하나.
Addressables, Localization, Newtonsoft 미설치. Template의 2D 기능 Package와 내장 모듈이 전부 켜져 있다.
이 Mac에 Python 3.9.6, dotnet 10.0.201, Unity Editor 6000.3.9f1(모듈: macOS, Android, iOS)이 있다.
Windows Build Support 모듈과 `git-lfs`는 없다.

## Verification

- `git diff --check`: 추적 중인 Unity 직렬화 파일의 공백 경고가 병합 뒤 0건.
- 추적 중인 Asset의 변경 없음.
- 검증 체인은 아직 없다(3단계에서 생긴다).
