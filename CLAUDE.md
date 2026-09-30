# F1 Architecture Authority

이 문서는 F1 Unity 프로젝트의 구현, 수정과 리뷰에 적용되는 최상위 Architecture Authority다.
세부 정책은 이 문서가 등록한 `Docs/Architecture` 문서와 함께 적용한다.

단계, 결정 관문, 진행 상태는 [Docs/Roadmap.md](Docs/Roadmap.md)가 소유한다.
이 문서의 "결정 대기 (G번호)"는 그 문서의 결정 관문 표를 가리킨다. 결정 대기 항목은 임의로 구현하지 않는다.

## 1. Authority

- `/CLAUDE.md`의 불변조건은 모든 코드, Asset, 설정, Test와 리뷰에 적용한다.
- `/Docs/Architecture/*.md` 중 §8 Ownership Map에 등록된 문서는 root의 normative extension이다.
- root와 Owner 문서가 충돌하면 root가 우선한다.
- Owner 문서끼리 충돌하면 번호, 수정 시각으로 정하지 않는다. 해당 주제의 Owner를 확인하고, 그래도 의미 충돌이면
  `Conflict requiring architecture decision`으로 보고하고 임의 구현하지 않는다.
- 요청 없이 Architecture 의미, Manager 목록, Save 구조와 의존 방향을 바꾸지 않는다.

다음 문서는 Authority가 아니다.

| 위치 | 성격 |
|---|---|
| `Docs/Design/` | 기획 입력. 구조 결정의 근거로 쓸 수 있는 것은 【확정】 태그뿐이다 (태그 규칙: `Docs/Design/00_INDEX.md`) |
| `Docs/StarterKit/` | 이전 프로젝트(C1)에서 추출한 구조 제안. 이 문서나 Owner 문서로 옮겨 확정하기 전에는 규칙이 아니다 |
| `Docs/Roadmap.md` | 단계, 결정 관문, 진행 상태 |
| `Docs/Workstreams/` | 세션 간 Handoff Snapshot. Architecture와 충돌하면 Workstream이 stale이다 |

## 2. Required Reading

```text
root CLAUDE.md -> Docs/Roadmap.md(현재 단계, 결정 관문) -> 작업 영역의 Owner 문서(여러 영역이면 모두)
-> 관련 기획(Docs/Design) -> 현재 코드/Asset/Git 상태
```

| 작업 영역 | 필수 문서 |
|---|---|
| 모든 단계 작업 | `Docs/Roadmap.md` |
| Folder / Asset 경로 | `Docs/Architecture/01_PROJECT_STRUCTURE.md` |
| Managers / Scene / Bootstrap | `Docs/Architecture/02_BOOTSTRAP_MANAGERS.md` |
| Namespace / asmdef / Package | `Docs/Architecture/03_ASSEMBLY_PACKAGES.md` |
| Scope / Vertical Slice | `Docs/Architecture/09_VERTICAL_SLICE.md` |
| Testing / Validation | `Docs/Architecture/10_TESTING_VALIDATION.md` |
| 기획을 근거로 쓰는 모든 작업 | `Docs/Design/00_INDEX.md`(태그 규칙), `Docs/Design/09_Implementation_Constraints.md` |
| 전투 | `Docs/Design/02_Combat_System.md` |
| 던전 / 로비·100일 / 용병 성장 / 유물 | `Docs/Design/03`~`06` 중 해당 문서 |
| 데이터 | `Docs/Design/07_Data_Design.md` |
| Owner 문서가 아직 없는 영역의 설계 | `Docs/StarterKit/README.md`와 그 영역의 StarterKit 문서 (제안) |

Owner 문서는 그 영역의 첫 구현 단계에서 작성하고, 같은 변경에서 이 표와 §8에 등록한다.

Active Workstream을 이어갈 때는 Owner 문서와 Git 상태를 확인한 뒤 `Docs/Workstreams/README.md`와 해당 active Handoff를 읽는다.

## 3. Architecture Non-Negotiables

### Project scale

- F1은 1인 개발 규모를 유지한다. 현재 요구를 작은 구체 Type과 Service로 해결한다.
- 미래 확장 가능성만으로 범용 Framework, 추상화, Package를 들이지 않는다.
- 실제 첫 코드/Asset/Test가 생기기 전에 빈 Folder를 만들지 않는다.
- 기획은 크게 바뀔 수 있다고 가정한다. 바뀌기 쉬운 수치는 데이터에, 규칙은 작은 순수 C# Type과 Test에 둔다.
  정해지지 않은 영역은 미리 만들지 않고 비워 둔다.

### Layer and state boundaries

```text
Unity Presentation
-> UI / Scene / Audio Adapters
-> Managers Service Container
-> Application Coordination
-> Domain (순수 C#)
-> Plain Runtime State
```

- Static Data, Runtime State, Save DTO를 분리하고 명확한 ID로 연결한다.
- Domain은 `Managers`, MonoBehaviour, UI, Scene, Addressables, Save DTO를 직접 참조하지 않는다. Dependency는 생성자/메서드 인자로 받는다.
- Domain은 Unity 없이 컴파일되고 실행된다. 시뮬 실행기가 같은 Domain 코드를 Unity 밖에서 돌린다 (§5).
- UI는 State를 표시하고 명령을 전달하며 Gameplay Rule을 계산하지 않는다.
- Domain의 내부 구조는 규칙 명세가 승인된 뒤 Gameplay Domain Owner 문서에서 정한다.

### Static Data

```text
CSV -> DataTransformer -> JSON -> Addressables -> ResourceManager -> DataManager
```

- CSV가 Source of Truth다. Generated JSON은 Git으로 관리하고 손으로 고치지 않는다. ScriptableObject 중심 구조로 대체하지 않는다.
- 밸런스 상수는 데이터 파일에서만 읽는다 (§5).
- 게임과 시뮬은 같은 Generated JSON을 읽는다 (`Docs/Design/09_Implementation_Constraints.md` §3).
- 식별자 타입과 조회 구조, 변환 도구의 실행 방식은 결정 대기다 (G14). Static Data Owner 문서에서 확정한다.

### Direct call and typed event

- 명령이나 반환값이 필요한 작업은 직접 호출한다.
- Typed Event는 이미 확정(저장 성공)된 사실을 여러 Consumer에 알릴 때만 쓴다.
- Global EventBus, Enum + object payload 구조를 만들지 않는다.

### Resource and localization

- 일반 Runtime은 Addressables API와 Handle을 직접 다루지 않는다. `ResourceManager`가 Handle과 Scope를 소유한다.
- UI Static Text는 Unity Localization String Table, Game Data Name/Description은 CSV의 `LocalizedText`. 두 Pipeline을 합치지 않는다.
- Locale은 `ko-KR`, `en-US`. 기본/Fallback은 `ko-KR`.

### Scene and presentation

- Runtime Scene은 `Assets/@Scenes/Boot.unity`(index 0), `Assets/@Scenes/Main.unity`(index 1).
- 단일 `AppRoot`가 Manager를 만들고 명시적으로 조립한다.
- Animation/Tween 완료를 Gameplay 결과, Save, Bootstrap 성공의 조건으로 쓰지 않는다.

## 4. Manager List and Dependency Direction

Manager는 아래 닫힌 목록이다. 목록 밖의 Manager는 Authority 확인 없이 만들지 않는다.

```text
ResourceManager
SaveManager
SettingManager    -> SaveManager
DataManager       -> ResourceManager
SoundManager      -> ResourceManager, SettingManager
UIManager         -> ResourceManager
SceneManagerEx
RunManager        -> DataManager, SaveManager
ExpeditionManager -> DataManager, SaveManager, RunManager
```

- `RunManager`(런)와 `ExpeditionManager`(원정과 전투)가 Application 계층이다. 규칙은 Domain이 계산한다.
- 각 Manager는 그 영역이 구현되는 단계에서 만든다. 책임은 `Docs/Architecture/02_BOOTSTRAP_MANAGERS.md`가 소유한다.

만들지 않는다: `GameManager`, `EventManager`, `LocalizationManager`, 초기 `PoolManager`.

- `Managers`는 AppRoot가 한 번 Configure하는 Service Container다. Getter Lazy Init 금지, Configure 전 Getter는 Fail-fast, 두 번째 Configure는 실패.
- Manager가 다른 Manager를 `Managers.X`로 찾아 Dependency를 해결하지 않는다.
- `SaveManager`는 형식/I/O만 담당한다.

## 5. Gameplay Invariants

출처는 `Docs/Design/09_Implementation_Constraints.md`의 【확정】 항목(§1, §2, §4)이다. 수치와 콘텐츠는 여기에 복사하지 않는다.
괄호 안은 근거 위치다. 09가 바뀌면 같은 변경에서 이 절을 고친다.

1. 밸런스 상수는 데이터 파일에서만 읽는다. 코드에 하드코딩하지 않는다. (09 §1-1, 07 §1)
2. 전투는 결정론적이다. 같은 시드 + 같은 입력 이벤트열 = 같은 결과. 따라서 전투 결과 계산은 시드에서 나온 난수와
   입력 이벤트열 외의 것(다른 난수원, 실제 시각, 프레임 간격, 연출 완료 시점)에 의존하지 않는다. (09 §1-2, 02 §1·§11)
3. 아이템(던전 한정, 쿨다운으로 능동 발동)과 유물(영구 패시브)은 다른 개념이다. 데이터 키, Type, 모듈을 분리한다. (09 §1-3, 06 §1·§4)
4. 데이터 식별자는 영어 id다. (09 §1-4, 07 §1)
5. 전열/후열 판정은 직업의 권장 열이 아니라 실제 파티 위치로 한다. (09 §1-6, 02 §2, 06 §3)
6. 시작 유물은 없다. (09 §1-7, 06 §2)
7. 최종 보스 전용 적 레벨을 다른 적에 쓰지 않는다. 레벨 값은 기획과 데이터가 소유한다. (09 §1-8, 03 §4)
8. 전투의 기준 구현은 C# Domain이다. 시뮬은 같은 Domain 코드와 같은 데이터로 돌리고, 밸런스 결정은 시뮬로 검증한 뒤
   문서화한다. (09 §2, 02 §1)

09 §1-5(【미결】 임의 해결 금지)와 §4(시뮬 우선 워크플로)는 절차 규칙이므로 §10에 둔다.
결정론의 세부 방식(시간 단위, 난수 스트림, 수치 타입)은 기획에서 【제안】 상태이며 결정 대기다 (G8).

## 6. Save Invariants

- 경로 `<persistentDataPath>/Saves/`, 파일별 같은 Directory의 `.tmp`와 한 세대 `.bak`.
- Atomic Write: Temp -> Flush -> Replace/Move. `.tmp`를 Load 후보로 쓰지 않는다.
- 상태 변경 순서: `검증 -> 변경 -> Stable State -> Snapshot -> Save 성공 -> Event -> UI`.
- Build/Quit/Pause의 마지막 저장에 의존하지 않는다. 상태가 확정될 때마다 저장한다.
- 확정된 손실(용병의 사망)은 이어하기로 되돌릴 수 없어야 한다.
- 파일 구성, 저장 시점, 이어하기의 상세는 결정 대기다 (G7). Save Owner 문서에서 확정한다.

## 7. Prohibited Patterns

- Generic Global EventBus
- Reflection 기반 Gameplay/Data/Save Framework
- Service Locator를 Domain에 전달하기
- Manager Getter Lazy Initialization
- 미래용 빈 Folder 대량 생성, 미래 가능성만으로 Package 설치
- Save DTO를 Domain/Runtime State로 사용
- Addressables Handle을 일반 코드에 노출
- UI에서 Gameplay Rule 계산
- Tween Callback으로 Gameplay Commit
- Build/Quit/Pause 시 마지막 Save에 의존
- Remote Addressables / Cloud Save 선행 도입
- 범용 DI/Reactive/Event/Repository Framework
- 밸런스 상수 하드코딩
- 아이템과 유물을 하나의 Type, 데이터 키, 모듈로 합치기
- 기획의 미확정 항목을 구현하면서 임의로 채우기

## 8. Architecture Document Ownership Map

| Owner 문서 | 소유 영역 |
|---|---|
| `01_PROJECT_STRUCTURE.md` | Asset 물리 경로, `@` Prefix, Folder 생성 시점 |
| `02_BOOTSTRAP_MANAGERS.md` | Manager 책임, AppRoot, Boot/Main, 초기화/실패/종료 |
| `03_ASSEMBLY_PACKAGES.md` | Namespace, asmdef, Package |
| `09_VERTICAL_SLICE.md` | Slice 범위, 구현 순서, Acceptance, Deferred |
| `10_TESTING_VALIDATION.md` | Test/Validation, 검증 체인, Definition of Done |

- Owner 문서를 만들면 같은 변경에서 이 표와 §2 표에 등록한다. 등록되지 않은 문서는 규칙이 아니다.
- 한 정책의 상세를 여러 문서에 복사하지 않는다. root는 불변조건, Owner는 상세. 교차 영역은 Link로 연결한다.
- Owner 문서에는 현재 규칙만 둔다. 경위는 Git과 Workstream이 소유한다.

## 9. Current Vertical Slice

현재 Slice는 **Slice A: 핵심 루프 한 바퀴 + 저장/이어하기**다.
범위, 구현 순서, Acceptance, Deferred는 [Docs/Architecture/09_VERTICAL_SLICE.md](Docs/Architecture/09_VERTICAL_SLICE.md)가 소유한다.

- 그 문서의 Deferred 항목은 현재 범위가 아니다. 코드, 데이터, Folder를 미리 만들지 않는다.
- Gameplay 규칙은 `Docs/Design`에서 【확정】된 것만 구현한다.
- 단계별 범위와 진행 상태: [Docs/Roadmap.md](Docs/Roadmap.md)

## 10. Change Discipline

### 단계 절차 (Roadmap의 모든 단계에 공통)

1. root, 관련 Owner 문서, 관련 StarterKit 문서, `Docs/Design`의 관련 기획을 실제로 읽는다.
2. 이 프로젝트 기준으로 줄인 Owner 문서를 `Docs/Architecture`에 먼저 작성한다.
3. 설계를 보여주고 사용자 승인을 받은 뒤 구현한다.
4. 검증 체인을 돌리고 결과를 보고한다 (체인이 생기는 3단계 이후).
5. `Docs/Workstreams/active`의 Handoff를 갱신하고 `Docs/Roadmap.md`의 상태를 고친다.
6. Commit은 요청받았을 때만, 주제별로 한다.
7. 한 번에 한 단계만 한다. 다음 단계는 시작 지시를 받은 뒤에 한다.

사용자가 범위를 정해 일괄 승인하면 그 범위 안에서는 3, 6, 7을 그 승인 내용으로 대체한다.
현재 유효한 일괄 승인의 범위와 조건은 `Docs/Roadmap.md` "일괄 승인"에 적는다.

### 기획 입력

- 구조 결정의 근거는 【확정】뿐이다. 【검토】【미결】【원본확인】【해석】【제안】과 태그 없는 서술은 미확정으로 취급한다.
- 원본 기획과 원본 시뮬은 없다. 【원본확인】은 다시 정할 대상이다. 다시 정할 때는 선택지를 【제안】으로 제시하고,
  사용자가 승인한 것만 `Docs/Design`에 【확정】으로 적는다. 승인 전에는 구현하지 않는다.
  선택지에 레퍼런스 게임의 방식을 넣을 때는 출처를 밝힌다.
- 기획의 수치와 옛 시뮬 결론은 새 시뮬로 재검증하기 전까지 출발값과 목표다 (`Docs/Design/00_INDEX.md`).
- 기획문서끼리, 또는 기획과 StarterKit이 어긋나면 고르지 않는다. 선택지, 영향, 권장안을 보고한다.
- 기획의 콘텐츠와 수치를 이 문서나 Owner 문서에 복사하지 않는다. `Docs/Design` 문서를 Link로 가리킨다.
- 밸런스 수치 변경은 시뮬 우선 워크플로를 따른다: 데이터 패치 -> 시뮬 실행(변경 전후 같은 시드) -> 수치 보고
  -> 확정된 것만 문서 갱신 (`Docs/Design/09_Implementation_Constraints.md` §4).
- 기획 결정이 바뀌면 `Docs/Design`을 먼저 고치고, 그에 딸린 Owner 문서와 이 문서의 §5를 같은 변경에서 고친다.
- StarterKit의 C1 고유 규칙(Slot/Score 도메인, `BigInteger`, C1의 Manager 목록 등)을 기본값으로 가져오지 않는다.

### 변경 규율

- 현재 Working Tree와 기존 파일을 확인하고 사용자 변경을 보존한다.
- 요구와 무관한 파일, Package, Scene, ProjectSettings를 변경하지 않는다.
- 새 Manager, Package, Framework, 저장 형식, 의존 방향을 추가하기 전에 Authority를 확인한다.
- 미확정 구현 선택을 임의로 Architecture Freeze하지 않는다.
- Unity Asset 이동/삭제 시 `.meta`와 Reference를 함께 검증한다.
- Package 변경 시 `manifest.json`과 `packages-lock.json`을 함께 확인한다.
- 작업 후 관련 Test/Validation, `git diff`, `git diff --check`, `git status`를 확인한다.
- 유료 생성 API(이미지/소리)는 Roadmap 7단계가 끝나기 전에는 호출하지 않는다. 그 전까지는 도형 Placeholder를 쓴다.
  이후에도 요청받았을 때만 호출하고, 승인된 산출물만 `Assets`에 배선한다.
