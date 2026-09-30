# F1 Roadmap

단계, 결정 관문, 진행 상태를 소유한다. 규칙은 [CLAUDE.md](../CLAUDE.md)가, 상세는 Owner 문서가 소유한다.
이 문서는 Authority가 아니다.

- 갱신: 2026-09-30 (2단계 완료, 3~8단계 일괄 승인으로 진행 중)
- 상태 값: 대기 / 진행 / 완료
- 단계 번호는 고정이다. CLAUDE.md와 대화가 번호로 가리킨다.
- 모든 단계는 CLAUDE.md §10 "단계 절차"를 따른다. 한 번에 한 단계만, 다음 단계는 시작 지시를 받은 뒤에.
- 문서 위치 표기: `Design/NN` = `Docs/Design/NN_*.md`, `Kit/NN` = `Docs/StarterKit/NN_*.md`, `Architecture/NN` = `Docs/Architecture/NN_*.md`.

## 일괄 승인 (2026-09-30)

사용자가 Slice A 완료(8단계)까지 "권장안대로 모두 구현"을 승인했다. 이 범위에서는 CLAUDE.md §10 단계 절차의 3, 6, 7을 아래로 대체한다.

- 3~8단계는 단계별 승인 없이 이어서 진행한다.
- 미결 결정 관문은 권장안으로 닫고, "결정됨" 표와 Owner 문서, 기획문서에 기록해 사후에 검토받는다.
  기획문서에는 `【확정】 (2026-09-30 일괄 승인)`으로 적는다. 사용자가 바꾸면 기획문서를 먼저 고친다.
- 단계가 끝날 때마다 주제별로 로컬 커밋한다. 푸시는 하지 않는다. 작업 브랜치는 `feature/slice-a`다.
- 9단계부터는 다시 단계별 승인을 받는다.

## 진행 순서

```text
1 -> 2 -> 3 -> 4 -> 5 -> 7a -> 7b -> 6 -> 7c -> 7d -> 8 -> 9 -> 10 -> 11
```

- 7단계는 넷(7a 규칙 명세, 7b Domain과 시뮬, 7c Application, 7d UI)으로 나눠 각각 승인받는다.
- 7a와 7b를 6단계(언어)보다 먼저 한다. 규칙을 다시 정하므로 시뮬이 빨리 있어야 하고, 순수 C# Domain은 언어 작업에 의존하지 않는다.
- 3, 4, 5, 6단계는 기획 내용에 의존하지 않는다. 기획이 바뀌어도 다시 하지 않는다.

## 결정 관문 — 미결

"늦어도" 열의 시점 전에 정한다. 정해지면 내용을 해당 Owner 문서(또는 CLAUDE.md)로 옮기고 아래 "결정됨" 표로 옮긴다.

| # | 미결 결정 | 근거 문서 위치 | 늦어도 |
|---|---|---|---|
| G7 | 런과 원정의 저장 시점, 이어하기의 상세 (방향과 파일 구성은 결정됨) | Architecture/07, Architecture/09 Acceptance 7 | 8단계 |
| G8 | 결정론 세부 (시간 단위, 동시 발동 순서, 난수 스트림, 수치 타입, 시드 파생) | Design/09 §5, Design/02 §3·§11 (전부 【제안】) | 7a |
| G9 | 그림체와 컨셉 방향 (기존 안은 미채택) | Design/10 | 9단계 시작 전 |
| G11 | 소리 방향 | 기획문서에 없음, Kit/05 "승인 라운드" | 10단계 시작 전 |
| G14 | 식별자 타입(문자열 id 단독 / 정수 `Id` + `Key`)과 조회 구조, 변환 도구를 Unity 밖에서도 돌리는 방식 | Design/09 §1-4, Kit/02 | 5단계 설계 승인 시 |
| G16 | Slice A의 규칙 정의 (전투, 원정, 로비와 귀환, 포션과 후퇴, 시뮬 정책) | Architecture/09 "규칙 정의가 필요한 항목" | 7a |
| G17 | 게임 제목(`productName`). Save 경로가 여기서 나온다 | ProjectSettings 현재값 `F1` | 출시 전 |

## 결정 관문 — 결정됨 (2026-09-30)

| # | 결정 | 기록 위치 |
|---|---|---|
| G0 | 원본 자료(기획 v0.5.x, `sim.py`, `build_data.py`, `prototype_data.json`, `lobby_sim`)는 없다. 규칙을 다시 정하고 시뮬을 새로 만든다. 기존 수치와 시뮬 결론은 재검증 전까지 출발값과 목표다 | Design/00 |
| G1 | CSV 원천 -> DataTransformer -> Generated JSON (Kit/02의 파이프라인). 하위 결정은 G14 | CLAUDE.md §3, Design/09 §3 |
| G2 | C# Domain이 기준 구현이다. 시뮬은 같은 Domain 코드를 Unity 밖에서 돌린다 | CLAUDE.md §3·§5, Design/09 §2 |
| G3 | 포션 슬롯과 후퇴를 도입하고 Slice A에 넣는다. 세부는 G16 | Design/02 §10 |
| G4 | Slice A = 핵심 루프 한 바퀴 + 저장/이어하기 | Architecture/09 |
| G5 | Locale은 `ko-KR`(기본/Fallback)과 `en-US` | CLAUDE.md §3 |
| G6 | Application 계층 Manager는 `RunManager`(런)와 `ExpeditionManager`(원정과 전투) | CLAUDE.md §4, Architecture/02 |
| G7 | 방향: 상태가 확정될 때마다 저장하고, 확정된 손실은 이어하기로 되돌릴 수 없다. 파일은 `settings.json`과 `run.json`, 슬롯은 하나 | CLAUDE.md §6, Architecture/07 |
| G9 | 다크 중세 흉상, 대규모 로스터, 소영주·공성전은 채택하지 않는다 | Design/10 |
| G10 | Git LFS를 쓴다. TextMeshPro 기본 리소스(ttf, png)가 3단계에 들어오므로 3단계 시작 때 설정했다 | `.gitattributes` |
| G12 | Prefix는 `F1` | CLAUDE.md |
| G13 | 대상 OS는 Windows와 macOS. `companyName`은 `funitup`. 기준 해상도 1920x1080(16:9), 기본은 전체 화면 창, 창 크기 조절 가능 | `ProjectSetup`, Architecture/02 |
| G15 | Resource Scope는 `App`, `Lobby`, `Expedition`. 기획의 "런"은 Scope가 아니다 | Architecture/04 |

## 단계

완료 기준은 초안이다. 각 단계의 설계 승인 때 확정한다.

### 1. CLAUDE.md와 git 설정 — 상태: 완료

- 산출: root `CLAUDE.md`, 이 문서, `.gitignore`/`.gitattributes` 병합, `Docs/Workstreams`, 결정 사항의 기획문서 반영.

### 2. Slice 범위 문서 — 상태: 완료

- 산출: `Docs/Architecture/09_VERTICAL_SLICE.md`. Slice A의 구간별 범위, 구현 순서, Acceptance, Deferred, 규칙 정의가 필요한 항목.
- 권장안(성장·경제·유물·기한 판정 제외, 로비 행동은 쉬기 하나)대로 승인됐다.

### 3. 기반 — 상태: 완료

- 산출: Boot/Main Scene(`SceneSetup`이 코드로 생성), `AppRoot`, `Managers`, `SceneManagerEx`, asmdef 4개, EditMode Test 4개,
  PlayMode Test 3개, `Tools/chain.sh`, `ProjectSetup`(Player 설정). Owner 문서 Architecture/01, 02, 03, 10.
- 함께 한 것: Template의 쓰지 않는 Package 11개 제거, TMP Essential Resources 반입, Git LFS 설정, `SampleScene` 삭제.
- Domain의 Unity 비의존은 asmdef 분리가 아니라 시뮬 실행기가 소스를 직접 컴파일하는 것으로 보장한다(Architecture/03). 실행기는 7b에서 만든다.

### 4. ResourceManager + Addressables + Save/Settings — 상태: 완료

- 산출: Addressables 2.11.2와 Newtonsoft JSON 3.2.2 도입, `ResourceManager`(Logical Address, Scope), `AddressablesSetup`(Entry 목록 동기화와 검사),
  `SaveManager`와 `SaveStorage`(Atomic Write, 한 세대 Backup, 손상 복구), `SettingManager`와 `settings.json`, `LocalePolicy`.
  Owner 문서 Architecture/04, 07.
- Boot 단계 추가: Save 저장소 초기화, 설정 Load, Resource 초기화. 실패하면 그 단계의 Error Code를 보이고 멈춘다.
- Test는 임시 Save Root를 쓴다.

### 5. 데이터 — 상태: 대기

- 범위: CSV -> DataTransformer -> Generated JSON -> Addressables -> `DataManager`를 첫 CSV 하나로 관통한다(Kit/02).
  시뮬 우선 루프에 Unity Editor가 끼지 않도록, 변환과 Definition 검증을 Unity 밖에서도 돌릴 수 있게 설계한다.
  Owner 문서는 Static Data 영역.
- 완료 기준: 첫 Definition이 Load되고 검증 실패 경우가 Test로 고정됐다. Generated JSON의 stale 검출이 있다.
  Owner 문서가 아이템과 유물의 데이터 키와 Type 분리를 규정한다. 체인 통과.
- 결정할 것: G14, 첫 데이터 파일로 무엇을 쓸지.

### 6. 언어 — 상태: 대기

- 범위: UI String Table과 Sync Setup, Game Data `LocalizedText`, Font(한글 Glyph 포함), Locale 정책.
  Owner 문서는 Localization 영역(Kit/03).
- 완료 기준: 누락 Key/번역, Font Family와 Glyph Coverage가 Test로 검증된다. Boot 화면이 Localization에 의존하지 않는다. 체인 통과.
- 결정할 것: Font 선택과 License, UI String CSV에 `Id` 열을 둘지(Kit/03 "주의").

### 7. 게임 루프 (Slice A)

범위와 Acceptance는 Architecture/09가 소유한다. 그림은 도형 Placeholder다.
7d가 끝나기 전에는 유료 이미지/소리 API를 호출하지 않는다.

#### 7a. 규칙 명세 — 상태: 대기

- 범위: Architecture/09의 "규칙 정의가 필요한 항목"을 전투 -> 원정 -> 로비와 귀환 순으로, 선택지와 함께 제안한다.
  승인된 것만 `Docs/Design`에 【확정】으로 적는다.
- 완료 기준: Slice A에 필요한 규칙이 `Docs/Design`에서 전부 【확정】이다.
- 결정할 것: G16, G8.

#### 7b. Domain과 시뮬 — 상태: 대기

- 범위: Slice A Static Data, 순수 C# Domain(전투 -> 원정 -> 런), 결정론 Test, Unity 밖 시뮬 실행기, 수치 보고.
  Owner 문서는 Gameplay Domain 영역.
- 완료 기준: 같은 시드와 입력의 반복 실행 결과가 같다는 Test가 있다. 시뮬 실행기가 게임과 같은 Generated JSON으로
  전투와 원정을 돌려 수치를 보고한다. 체인 통과.
- 결정할 것: 시뮬 결과를 본 뒤의 수치 확정.

#### 7c. Application — 상태: 대기

- 범위: 런, 원정, 전투의 흐름 조율과 명령 처리, 상태 확정 순서.
- 완료 기준: UI 없이 Test로 루프 한 바퀴를 돌릴 수 있다. 체인 통과.

#### 7d. UI — 상태: 대기

- 범위: 로비, 노드 맵, 전투, 결과 화면. 도형 Placeholder.
- 완료 기준: Architecture/09의 Acceptance 중 저장(7번)을 뺀 항목.
- 결정할 것: 화면 구성(목업으로 선택).

### 8. 저장/이어하기 — 상태: 대기

- 범위: 런 상태와 원정 진행 상태의 저장, 이어하기. Owner 문서는 Save 영역에 추가.
- 완료 기준: Architecture/09의 Acceptance 7번. 저장 실패 시 정책이 Test로 고정됐다. Schema Version과 Migration 골격이 있다.
- 결정할 것: G7.

### 9. 그림 — 상태: 대기

- 범위: `ArtPipeline` 세팅(가상환경, 키체인, 스타일 문서, 레퍼런스), 스모크 1회, 첫 승인 라운드, 승인분 배선(Kit/04).
- 완료 기준: 스타일 문서가 승인됐다. 승인된 그림만 `Assets`에 배선됐고 체인을 통과한다.
- 결정할 것: G9, 그릴 타입 목록, 예산.
- 선행: 7단계 완료.

### 10. 소리 — 상태: 대기

- 범위: 효과음·배경음 생성 파이프라인, 승인 라운드, `SoundManager` 배선(Kit/05).
- 완료 기준: 승인된 소리만 배선됐다. Import 정책이 Setup 코드로 고정됐다. 체인 통과.
- 결정할 것: G11, 예산.

### 11. 플레이테스트 -> 수정 -> Slice B — 상태: 대기

- 범위: Slice A 플레이테스트, 수정, Slice B 범위 문서.
- 완료 기준: Slice B 범위 문서가 승인됐다.
- 결정할 것: Slice B 범위(Architecture/09의 Deferred에서 고른다).
- 사용자 작업: Windows에서 확인하려면 첫 Windows 빌드 전에 Unity Hub에서 Windows Build Support 모듈을 추가한다.
