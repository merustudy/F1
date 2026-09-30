# 06. 추가로 가져가기를 권하는 구조

요청한 네 가지(폴더, 데이터/언어, 이미지, 음악)가 제대로 돌려면 아래가 같이 있어야 한다.
우선순위 순이다. 각 절 끝에 C1 원본 문서를 적었다.

| # | 구조 | 왜 필요한가 | 권장도 |
|---|---|---|---|
| 1 | Authority 문서 체계 (`CLAUDE.md` + Owner 문서) | 에이전트 세션이 매번 같은 규칙으로 시작한다 | 필수 |
| 2 | Boot/Main + AppRoot + `Managers` | Data/Localization/Sound가 올라갈 자리 | 필수 |
| 3 | `ResourceManager` + Logical Address + Scope | Data JSON·그림·소리 배선의 공통 통로 | 필수 |
| 4 | Editor Setup 코드 + Batch 검증 체인 | Editor를 열지 않고 변환·배선·Test | 필수 |
| 5 | asmdef 3~4개 + Test 피라미드 | 체인이 돌 대상 | 필수 |
| 6 | Save: Atomic Write + Backup | 설정·진행 저장 | 저장이 있으면 |
| 7 | Workstream Handoff | 세션 간 인수인계 | 권장 |
| 8 | 결정적 RNG | 이어하기·재현·밸런스 시뮬 | 게임에 따라 |
| 9 | Unity 밖 밸런스 도구 | 수치 결정 근거 | 나중에 |

---

## 1. Authority 문서 체계

```text
Level 1  /CLAUDE.md                 핵심 불변조건만 (짧게). 충돌 시 항상 우선
Level 2  /Docs/Architecture/NN_*.md 영역별 Owner 문서 (상세 규칙)
```

- 한 정책의 상세는 **한 문서만** 소유한다. root는 불변조건과 Owner 목록(Ownership Map), 나머지는 Link.
- 번호는 읽는 순서일 뿐이다. Owner끼리 충돌하면 번호·날짜로 정하지 않고 해당 주제의 Owner를 확인하고, 그래도 충돌이면 구현을 멈추고 보고한다.
- root에 "작업 영역 → 필수 문서" 표를 둔다. 에이전트는 작업 전 root + 해당 Owner를 실제로 읽는다.
- 각 Owner 문서의 끝에 `Validation` 체크리스트와 `Deferred and Forbidden`을 둔다. **하지 않기로 한 것**을 적는 것이 1인 프로젝트에서 범위를 지킨다.
- Nested `CLAUDE.md`, 별도 ADR 체계는 만들지 않는다.

초안: [`CLAUDE.template.md`](CLAUDE.template.md). C1 원본: `CLAUDE.md`, `Docs/Architecture/00_ARCHITECTURE.md`.

> C1의 교훈: Owner 문서에 날짜별 변경 기록("2026-09-24 ○○ 추가 …")을 계속 덧붙였더니 `05_STATIC_DATA.md`의 절반이
> 콘텐츠 연대기가 됐다. 새 프로젝트에서는 Owner 문서에 **현재 규칙만** 두고, 경위는 Git과 Workstream에 맡긴다.

## 2. Boot/Main + AppRoot + Managers

- Scene은 `Boot`(index 0)와 `Main`(index 1) 둘. 별도 Gameplay Scene은 필요가 증명될 때 추가한다.
- 단일 `AppRoot`(DontDestroyOnLoad)가 Manager를 **전부 생성한 뒤** `Managers.Configure(...)`로 한 번에 등록한다.
- `Managers`는 static Service Container일 뿐이다.
  - Configure 전 Getter는 예외(Fail-fast). 두 번째 Configure도 예외. Getter Lazy Init 금지.
  - Manager는 다른 Manager를 `Managers.X`로 찾지 않는다. **생성자 인자**로 받는다.
  - Domain(순수 C#)은 `Managers`를 모른다.
- Manager 목록과 의존 방향을 `CLAUDE.md`에 **닫힌 목록**으로 적는다. 새 Manager는 Authority 확인 후에만.

새 프로젝트의 시작 목록(게임 무관한 일곱 개):

```text
ResourceManager
SaveManager
SettingManager  -> SaveManager
DataManager     -> ResourceManager
SoundManager    -> ResourceManager, SettingManager
UIManager       -> ResourceManager
SceneManagerEx
(+ 게임 진행을 조율하는 Application 계층 Manager 1~2개)
```

초기화 순서(C1에서 게임 고유 단계를 뺀 것):

```text
Boot -> AppRoot 중복 검사 -> Manager 생성 -> Managers.Configure
-> Save 저장소 초기화 -> Settings Load
-> ResourceManager 초기화 -> Localization 초기화 -> 저장 Locale 적용
-> Data-App Load/검증 -> (진행 Save Load/Apply) -> Sound 초기화 -> App Scope Resource
-> Main Scene Load -> Main Binding -> Main UI 표시 -> Initialized
```

- `BootstrapView`는 Addressables/Localization/DOTween 없이 동작한다(실패 화면이 그것들에 의존하면 안 된다).
- 초기화 실패는 단계 이름과 Error Code를 보이고 멈춘다. 부분 초기화로 Main에 들어가지 않는다.
- Editor에서 Main을 직접 Play해도 Boot를 거치게 한다(Editor Direct Play 처리).

계층:

```text
Unity Presentation -> UI/Scene/Audio Adapter -> Managers -> Application Coordination -> Domain(순수 C#) -> Plain Runtime State
```

- UI는 State를 표시하고 명령을 전달한다. Rule을 계산하지 않는다.
- 명령·반환값이 필요한 것은 **직접 호출**. Typed Event는 "이미 확정된 사실"을 여러 Consumer에 알릴 때만. Global EventBus 금지.
- Animation/Tween 완료를 Gameplay 확정·Save의 조건으로 쓰지 않는다(결과를 먼저 확정·저장하고 연출은 그 뒤).

C1 원본: `02_BOOTSTRAP_MANAGERS.md`, `00_ARCHITECTURE.md`.

## 3. ResourceManager + Logical Address + Scope

```text
Game Code -> Managers.Resource -> ResourceManager -> Addressables
```

- 일반 코드는 Addressables API와 `AsyncOperationHandle`을 만지지 않는다. `ResourceManager`가 Handle을 소유한다.
- **Logical Address**: 물리 경로가 아닌 소문자 kebab 경로.

```text
data/app/<name>      data/run/<name>
ui/main/<name>       ui/run/<name>       ui/transient/<name>
audio/app/<name>     audio/main/<name>   audio/run/<name>
<domain>/<category>/<key>                # 예: slot/relic/fairy-dust
```

- **Scope Label** 넷: `scope-app`, `scope-main`, `scope-run`, `scope-transient`. Entry당 정확히 하나. 여러 Scope가 쓰면 더 긴 수명으로 올린다.
- 개별 Release가 아니라 **Scope Release**가 기본이다: `BeginScope` → `LoadAsync<T>(address, scope)` → `ReleaseScope`.
- Group은 책임별 소수(`<P>-Data`, `<P>-UI`, `<P>-Audio`, `<P>-<Domain>`). Scope마다 Group을 만들지 않는다.
- Load 실패는 명확히 실패한다. 자동 Placeholder 대체 없음.
- Localization Package가 만든 Group/Handle은 Package에 맡긴다.
- Local Addressables만. Remote/CDN은 요구가 생길 때.

API 방향:

```text
InitializeAsync()
BeginScope(App/Main/Run) / BeginTransientScope(owner)
LoadAsync<T>(logicalAddress, scope)
InstantiateAsync(logicalAddress, parent, scope)
ReleaseInstance(instance) / ReleaseScope(scope)
```

C1 원본: `04_RESOURCES_ADDRESSABLES.md`.

## 4. Editor Setup 코드 + 검증 체인

Asset 설정을 Inspector 클릭이 아니라 **멱등한 Editor static 메서드**로 한다. Menu와 Batch 양쪽에서 부르고, 끝에 고정 로그 토큰을 찍는다.

| 메서드 | 하는 일 | 로그 토큰 |
|---|---|---|
| `DataTransformer.TransformMenu` | CSV → JSON | `<P>_DATA_TRANSFORM_DONE` |
| `LocalizationSetup.SyncMenu` | Settings/Locale/Table 보장 + CSV Import | `<P>_LOCALIZATION_SYNC_DONE` |
| `AddressablesSetup.SyncMenu` | Group/Entry/Label + Import 정책 | `<P>_ADDRESSABLES_SYNC_DONE` |
| `UiPrefabSetup` 등 | Prefab 구조·Font 지정 | — |

체인(Editor 버전은 `ProjectSettings/ProjectVersion.txt`에서 읽는다):

```text
Transform: -batchmode -quit -nographics -executeMethod <P>.Editor.Data.DataTransformer.TransformMenu
Locale:    -batchmode -quit -nographics -executeMethod <P>.Editor.Data.LocalizationSetup.SyncMenu
Sync:      -batchmode -quit -nographics -executeMethod <P>.Editor.Data.AddressablesSetup.SyncMenu
EditMode:  -batchmode -nographics -runTests -testPlatform EditMode -testResults <저장소 밖>/editmode.xml   (-quit 없이)
PlayMode:  -batchmode -nographics -runTests -testPlatform PlayMode -testResults <저장소 밖>/playmode.xml
```

C1에서 밟은 함정(그대로 적용):

- **체인 스크립트를 저장소에 커밋한다**(`Tools/chain.sh`). C1은 세션마다 scratchpad에 다시 써서 시간을 버렸다.
- 결과는 exit code가 아니라 **결과 XML의 test-case result**로 판정한다. Compile Error(exit 1)와 Test 실패(exit 2)를 섞어 읽지 않는다.
- `-logFile`과 결과 XML은 저장소 밖에 둔다.
- Editor가 열려 있으면 Batch가 실패한다. 검사는 실행 파일 경로에 앵커한다:
  `pgrep -f "^/Applications/Unity/Hub/Editor/[^ ]*/Unity.app/Contents/MacOS/Unity( |$)"` (`pgrep -f "MacOS/Unity"`는 Hub와 자기 셸을 잡는다).
- zsh: 공백 든 명령을 변수 하나에 넣으면 단어 분리가 안 된다(exit 127) → 함수로 감싼다. `&&` 체인 안의 `grep`은 매치 없으면 체인을 끊는다 → `;`와 명시적 exit 출력.
- PlayMode Test가 **실제 Save 폴더를 지울 수 있다.** 체인 전후로 `persistentDataPath/Saves`를 백업/복원하거나, Test가 별도 Save Root를 쓰게 처음부터 설계한다(후자 권장).
- 그래픽 Batch(`-nographics` 없이)는 URP/ProjectSettings Asset을 건드린다. 끝나면 되돌린다.
- 중단된 Test Run은 `Assets/InitTestScene*`를 남긴다. 체인 끝에서 확인한다.
- Batch Coroutine에서 `WaitForEndOfFrame`은 돌아오지 않는다.

작업 후 공통 확인: 관련 Test → `git diff` → `git diff --check` → `git status`.

변이 확인(선택): 새 Test가 정말 잡는지 보려고 대상 코드를 한 줄 망가뜨려 그 Test만 돌린다. 원본 백업 + `trap`으로 원복을 보장하고, 변이가 **컴파일되는지** 확인한다.

C1 원본: `10_TESTING_VALIDATION.md`.

## 5. asmdef와 Test 피라미드

```text
Assets/@Scripts/<P>.Runtime.asmdef          Root Namespace <P>     (Runtime은 하나만)
Assets/@Scripts/Editor/<P>.Editor.asmdef    Editor only            -> <P>.Runtime
Assets/@Tests/EditMode/<P>.Tests.EditMode.asmdef                   -> <P>.Runtime (+ <P>.Editor 필요 시)
Assets/@Tests/PlayMode/<P>.Tests.PlayMode.asmdef                   -> <P>.Runtime
```

- Layer/Feature별 Runtime asmdef 분할을 하지 않는다. 폴더는 Assembly 경계가 아니다.
- 금지 방향: Runtime → Editor, Runtime → Tests, Editor → Tests.
- Package asmdef Reference는 그 Type을 처음 쓸 때 추가한다.
- `Override References`를 켜면 `Assets/Plugins`의 Precompiled DLL 자동 참조가 끊긴다(Newtonsoft, DOTween). 쓰는 DLL을 전부 나열해야 한다.
- Package는 실제로 필요할 때 설치하고 `manifest.json`과 `packages-lock.json`을 함께 확인한다.

C1 기반 Package: Addressables 2.10, Localization 1.5, Newtonsoft Json 3.2, Input System 1.18, URP 17.3, Test Framework 1.6, uGUI 2.0, (Vendor) DOTween Pro.

Test 피라미드:

```text
많음  순수 C# Domain Test          (EditMode Runner에서 실행)
중간  Editor/Storage/Data 통합      (EditMode)
적음  Scene/MonoBehaviour/Addressables/Localization Runtime  (PlayMode)
별도  Presentation Manual QA       (레이아웃, 타이밍, 음량, 손맛 — 자동화하지 않는다)
```

- Scene·Frame·GameObject가 필요 없으면 PlayMode를 쓰지 않는다. Test하려고 순수 로직을 MonoBehaviour로 만들지 않는다.
- 이름: `<Subject>Tests` / `<Behavior>_When<Condition>_<Expected>`.
- **출고 Data 감사 Test**가 값어치가 컸다: 모든 CSV 행이 Load되고, 모든 Art/Audio Key가 Entry와 파일을 갖고, 남는 Entry가 없다.

C1 원본: `03_ASSEMBLY_PACKAGES.md`, `10_TESTING_VALIDATION.md`.

## 6. Save: Atomic Write + Backup

게임 내용과 무관한 부분만.

- 경로: `<persistentDataPath>/Saves/`. 최소 `settings.json`(전역) + 진행 Save.
- 파일마다 같은 Directory의 `.tmp`와 한 세대 `.bak`.
- Atomic Write: 같은 Directory에 Temp 쓰기 → Flush → Replace/Move.
- Primary가 손상되고 Backup이 정상이면 Backup을 덮지 않는 별도 Repair 경로. `.tmp`는 Load 후보가 아니다.
- `SaveManager`는 형식/I/O만. 상태 적용은 그 상태의 주인이 한다.
- Save DTO ≠ Runtime State. DTO의 표현(문자열화된 숫자 등)을 Domain으로 흘리지 않는다.
- `SchemaVersion` + Migration 단계 함수.
- **Build/Quit/Pause 시점의 마지막 저장에 의존하지 않는다.** 상태가 확정될 때마다 저장한다.

순서(확정 → 저장 → 알림 → 연출):

```text
Command 검증 -> 상태 변경 -> Stable State -> 직렬화 Snapshot -> Atomic Save 성공 -> Typed Event -> UI/연출
```

저장 실패 시: 해당 상태의 추가 변경을 막고, **처음 실패한 동일 byte**만 재시도한다.
(C1 수준의 엄격함 — RNG까지 포함한 완전 재현 — 이 필요 없는 게임이면 "실패 시 재시도 + 사용자 알림" 정도로 줄인다.)

C1 원본: `07_SAVE_CHECKPOINT.md`.

## 7. Workstream Handoff

```text
Docs/Workstreams/README.md      운영 규칙 + Template
Docs/Workstreams/active/<name>.md
Docs/Workstreams/completed/<name>.md
```

- 새 세션은 과거 대화를 기억하지 못한다. Architecture → Git 상태 → 실제 Code → active Workstream 순으로 현재를 복구한다.
- Workstream은 날짜별 일기가 아니라 **현재 Handoff Snapshot**이다(목표 80~150줄, 200줄 넘으면 정리). History는 Git이 소유한다.
- Workstream은 Architecture Source가 아니다. 충돌하면 Workstream이 stale이다. `Next Action`은 명령이 아니라 제안이다.
- 폴더가 상태다(별도 Status 필드 없음). Planned 기능은 파일을 만들지 않는다. 기본 Active는 하나.
- 파일명: `<slice><stage>-<feature>.md` (kebab-case).

C1 원본: `Docs/Workstreams/README.md` (Template 포함 — 그대로 복사해도 된다).

함께 가져갈 작업 습관:

- **Vertical Slice**로 범위를 자른다: Slice A = 핵심 루프 한 바퀴 + 저장/이어하기, Slice B = 빌드/성장 요소. 그 밖은 Deferred로 명시.
- 주제별 Commit("topic commit"): 한 Commit이 한 가지를 말한다. Source CSV + Generated JSON + 관련 Test는 같은 Commit.
- 디자인 결정은 **후보 제시 → 선택 → 구현**. UI는 Prefab rect로 그린 Pillow 목업에 글자 라벨(A/B/C)을 붙여 한 글자로 답받는다.

## 8. 결정적 RNG (필요한 게임만)

이어하기에서 같은 결과, 시드 공유, 밸런스 시뮬이 필요하면:

- 판마다 PCG32 인스턴스 하나. Gameplay에서 `UnityEngine.Random`/새 `System.Random` 금지(결과에 영향 없는 연출에만 허용).
- Save에 `InitialSeed`, `State`, `Stream`, `DrawCount`를 저장하고 그대로 복원한다.
- 결과와 RNG State를 **연출 전에** 저장한다.
- Gameplay 수치에 float을 쓰지 않는다(정수 또는 Scale 정수).
- Substream은 필요가 증명될 때.

액션 게임처럼 Frame 단위 재현이 필요 없으면 생략한다.

## 9. Unity 밖 밸런스 도구 (나중에)

C1은 `Tools/BalanceSim`(dotnet 콘솔, Domain 코드를 그대로 참조해 수천 판 시뮬)과 `Tools/ScoreModel`(Python 수식 모델)로
수치 결정을 뒷받침했다. 전제는 **Domain이 Unity 비의존 순수 C#**이라는 것이다(2번의 계층 규칙이 이걸 가능하게 한다).
콘텐츠가 어느 정도 찬 뒤에 만든다. 결과는 결정 근거일 뿐 Test 기준이 아니다.

---

## C1에서 아쉬웠던 점 (새 프로젝트에서 처음부터 다르게)

| C1에서 | 새 프로젝트에서 |
|---|---|
| legacy `Text`로 시작 → Font Fallback이 안 돼 Font 병합 | 처음부터 TextMeshPro |
| 검증 체인·BGM 트림·배치 러너를 scratchpad에서 매번 재작성 | `Tools/`·`ArtPipeline/tools/`에 커밋 |
| 보조 도구가 CSV를 naive하게 읽어 "쉼표 금지", 열 index 제약 | 같은 Parser 또는 Generated JSON을 읽는다 |
| `DataTransformer.cs` 1,400줄+, `AddressablesSetup.cs` 1,000줄+ | Definition/Category별 파일로 분할 |
| Owner 문서에 날짜별 변경 기록 누적 | 현재 규칙만. 경위는 Git/Workstream |
| PlayMode Test가 실제 Save 슬롯을 지움 | Test 전용 Save Root |
| 보관용 PNG 148장이 일반 Git History에 | 초기에 Git LFS 여부 결정 |
| BGM `.meta`를 손으로 패치 | `AudioImporter` 정책을 Setup 코드로 |
