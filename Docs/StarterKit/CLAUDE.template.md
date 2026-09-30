# <P> Architecture Authority

<!-- 새 프로젝트 root CLAUDE.md 초안. <P>, <...>와 "TODO(game)" 표시를 채운 뒤 이 주석을 지운다.
     짧게 유지한다: 여기에는 불변조건만, 상세는 Docs/Architecture Owner 문서에. -->

이 문서는 <P> Unity 프로젝트의 구현, 수정과 리뷰에 적용되는 최상위 Architecture Authority다.
세부 정책은 이 문서가 등록한 `Docs/Architecture` 문서와 함께 적용한다.

## 1. Authority

- `/CLAUDE.md`의 불변조건은 모든 코드, Asset, 설정, Test와 리뷰에 적용한다.
- `/Docs/Architecture/*.md` 중 아래 Ownership Map에 등록된 문서는 root의 normative extension이다.
- root와 Owner 문서가 충돌하면 root가 우선한다.
- Owner 문서끼리 충돌하면 번호, 수정 시각으로 정하지 않는다. 해당 주제의 Owner를 확인하고, 그래도 의미 충돌이면
  `Conflict requiring architecture decision`으로 보고하고 임의 구현하지 않는다.
- 요청 없이 Architecture 의미, Manager 목록, Save 구조와 의존 방향을 바꾸지 않는다.

## 2. Required Reading

```text
root CLAUDE.md -> 작업 영역의 Owner 문서(여러 영역이면 모두) -> 현재 코드/Asset/Git 상태
```

| 작업 영역 | 필수 문서 |
|---|---|
| Layer / Dependency / Event | `Docs/Architecture/00_ARCHITECTURE.md` |
| Folder / Asset 경로 | `Docs/Architecture/01_PROJECT_STRUCTURE.md` |
| Managers / Scene / Bootstrap | `Docs/Architecture/02_BOOTSTRAP_MANAGERS.md` |
| Namespace / asmdef / Package | `Docs/Architecture/03_ASSEMBLY_PACKAGES.md` |
| Resource / Addressables | `Docs/Architecture/04_RESOURCES_ADDRESSABLES.md` |
| CSV / DataTransformer / DataManager | `Docs/Architecture/05_STATIC_DATA.md` |
| Locale / String Table / Font | `Docs/Architecture/06_LOCALIZATION.md` |
| Save | `Docs/Architecture/07_SAVE.md` |
| Gameplay Domain | `Docs/Architecture/08_GAMEPLAY_DOMAIN.md` |
| Scope / Vertical Slice | `Docs/Architecture/09_VERTICAL_SLICE.md` |
| Testing / Validation | `Docs/Architecture/10_TESTING_VALIDATION.md` |
| 이미지·소리 생성 | `ArtPipeline/STYLE_GUIDE.md`, `ArtPipeline/STYLE_RUNTIME.md` |

<!-- 문서는 그 영역의 첫 구현 때 만든다. 아직 없는 문서는 표에서 빼 둔다. -->

Active Workstream을 이어갈 때는 Owner 문서와 Git 상태를 확인한 뒤 `Docs/Workstreams/README.md`와 해당 active Handoff를 읽는다.

## 3. Architecture Non-Negotiables

### Project scale

- <P>는 1인 개발 규모를 유지한다. 현재 요구를 작은 구체 Type과 Service로 해결한다.
- 미래 확장 가능성만으로 범용 Framework, 추상화, Package를 들이지 않는다.
- 실제 첫 코드/Asset/Test가 생기기 전에 빈 Folder를 만들지 않는다.

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
- UI는 State를 표시하고 명령을 전달하며 Gameplay Rule을 계산하지 않는다.

### Static Data

```text
CSV -> DataTransformer -> JSON -> Addressables -> ResourceManager -> DataManager -> Dictionary<int, T>
```

CSV가 Source of Truth다. Generated JSON은 Git으로 관리하고 손으로 고치지 않는다. ScriptableObject 중심 구조로 대체하지 않는다.

### Direct call and typed event

- 명령이나 반환값이 필요한 작업은 직접 호출한다.
- Typed Event는 이미 확정(저장 성공)된 사실을 여러 Consumer에 알릴 때만 쓴다.
- Global EventBus, Enum + object payload 구조를 만들지 않는다.

### Resource and localization

- 일반 Runtime은 Addressables API와 Handle을 직접 다루지 않는다. `ResourceManager`가 Handle과 Scope를 소유한다.
- UI Static Text는 Unity Localization String Table, Game Data Name/Description은 CSV의 `LocalizedText`. 두 Pipeline을 합치지 않는다.
- 초기 Locale은 `en-US`, `ko-KR`, 기본/Fallback은 `en-US`. <!-- TODO(game): Locale 확정 -->

### Scene and presentation

- Runtime Scene은 `Assets/@Scenes/Boot.unity`(index 0), `Assets/@Scenes/Main.unity`(index 1).
- 단일 `AppRoot`가 Manager를 만들고 명시적으로 조립한다.
- Animation/Tween 완료를 Gameplay 결과, Save, Bootstrap 성공의 조건으로 쓰지 않는다.

## 4. Manager List and Dependency Direction

Manager는 정확히 다음 목록이다. <!-- TODO(game): Application 계층 Manager 확정 -->

```text
ResourceManager
SaveManager
SettingManager  -> SaveManager
DataManager     -> ResourceManager
SoundManager    -> ResourceManager, SettingManager
UIManager       -> ResourceManager
SceneManagerEx
<GameFlowManager> -> DataManager, SaveManager
```

만들지 않는다: `GameManager`, `EventManager`, `LocalizationManager`, 초기 `PoolManager`.

- `Managers`는 AppRoot가 한 번 Configure하는 Service Container다. Getter Lazy Init 금지, Configure 전 Getter는 Fail-fast, 두 번째 Configure는 실패.
- Manager가 다른 Manager를 `Managers.X`로 찾아 Dependency를 해결하지 않는다.
- `SaveManager`는 형식/I/O만 담당한다.

## 5. Gameplay Invariants

<!-- TODO(game): 이 게임에서 절대 깨지면 안 되는 수치/순서/RNG 규칙. 없으면 섹션 삭제.
     예: 수치 타입, 처리 순서의 결정성, RNG 인스턴스 정책 (StarterKit 06 §8) -->

## 6. Save Invariants

- 경로 `<persistentDataPath>/Saves/`, 파일별 같은 Directory의 `.tmp`와 한 세대 `.bak`.
- Atomic Write: Temp -> Flush -> Replace/Move. `.tmp`를 Load 후보로 쓰지 않는다.
- 상태 변경 순서: `검증 -> 변경 -> Stable State -> Snapshot -> Save 성공 -> Event -> UI`.
- Build/Quit/Pause의 마지막 저장에 의존하지 않는다.
<!-- TODO(game): 파일 목록, 실패 시 정책 -->

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

## 8. Architecture Document Ownership Map

| Owner 문서 | 소유 영역 |
|---|---|
| `00_ARCHITECTURE.md` | Layer, Dependency, Direct Call/Event |
| `01_PROJECT_STRUCTURE.md` | Asset 물리 경로, `@` Prefix, Folder 생성 시점 |
| `02_BOOTSTRAP_MANAGERS.md` | Manager 책임, AppRoot, Boot/Main, 초기화/실패/종료 |
| `03_ASSEMBLY_PACKAGES.md` | Namespace, asmdef, Package |
| `04_RESOURCES_ADDRESSABLES.md` | ResourceManager, Address, Group/Label/Scope |
| `05_STATIC_DATA.md` | CSV, DataTransformer, Generated JSON, DataManager |
| `06_LOCALIZATION.md` | UI/Game Data Localization, Locale, Font |
| `07_SAVE.md` | Save DTO, Atomic/Backup/Migration |
| `08_GAMEPLAY_DOMAIN.md` | <!-- TODO(game) --> |
| `09_VERTICAL_SLICE.md` | Slice 범위, Acceptance, Deferred |
| `10_TESTING_VALIDATION.md` | Test/Validation, 검증 보고, Definition of Done |

한 정책의 상세를 여러 문서에 복사하지 않는다. root는 불변조건, Owner는 상세. 교차 영역은 Link로 연결한다.

## 9. Current Vertical Slice

<!-- TODO(game): Slice A = 핵심 루프 한 바퀴 + 저장. 구현 순서 번호 목록. 현재 범위가 아닌 것 명시. -->

## 10. Change Discipline

- 작업 전 root와 관련 Owner 문서를 실제로 읽는다.
- 현재 Working Tree와 기존 파일을 확인하고 사용자 변경을 보존한다.
- 요구와 무관한 파일, Package, Scene, ProjectSettings를 변경하지 않는다.
- 새 Manager, Package, Framework, 저장 형식, 의존 방향을 추가하기 전에 Authority를 확인한다.
- 미확정 구현 선택을 임의로 Architecture Freeze하지 않는다.
- Unity Asset 이동/삭제 시 `.meta`와 Reference를 함께 검증한다.
- Package 변경 시 `manifest.json`과 `packages-lock.json`을 함께 확인한다.
- 작업 후 관련 Test/Validation, `git diff`, `git diff --check`, `git status`를 확인한다.
- 유료 생성 API(이미지/소리)는 요청받았을 때만 호출하고, 승인된 산출물만 `Assets`에 배선한다.
