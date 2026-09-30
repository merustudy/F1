# 02. Bootstrap and Managers

Manager의 책임, `AppRoot`, Boot/Main Scene, 초기화와 실패, 종료를 소유한다.
Manager 목록과 의존 방향은 `CLAUDE.md` §4가 소유한다.

## Scene

- `Assets/@Scenes/Boot.unity`(index 0): `AppRoot`와 그 자식 `BootstrapView`, Boot Camera.
- `Assets/@Scenes/Main.unity`(index 1): `MainSceneRoot`, Main Camera, EventSystem, UI Root Canvas.
- 별도 Gameplay Scene은 만들지 않는다. 로비, 원정, 전투 화면은 Main Scene 안에서 UI로 바꾼다.
- Scene은 `F1.Editor.Setup.SceneSetup`이 코드로 만든다. Scene을 손으로 고치지 않고 Setup 코드를 고친 뒤 다시 만든다.

## AppRoot

- Boot Scene에 하나만 있다. `DontDestroyOnLoad`. 두 번째 `AppRoot`는 스스로 파괴된다.
- Manager를 **전부 생성한 뒤** `Managers.Configure`로 한 번에 등록한다.
- 초기화 상태는 `InitializationState`(NotStarted, Initializing, Initialized, Failed)로 노출한다.
- 파괴될 때 `Managers`를 비운다.

## Managers

- `Managers`는 static Service Container일 뿐이다. 로직이 없다.
- `Configure` 전 Getter는 예외(Fail-fast). 두 번째 `Configure`도 예외. 빠진 Manager가 있어도 예외. Getter Lazy Init 금지.
- Manager는 다른 Manager를 `Managers.X`로 찾지 않는다. 생성자 인자로 받는다.
- Domain(`Assets/@Scripts/Gameplay`)은 `Managers`를 모른다.

| Manager | 책임 | 하지 않는 것 | 구현 단계 |
|---|---|---|---|
| `ResourceManager` | Addressables 초기화, Logical Address로 Load/Instantiate, Scope별 Handle 소유와 Release | Content 선택, Fallback 대체 | 4 |
| `SaveManager` | Save 파일의 형식과 I/O (Atomic Write, Backup, 손상 복구) | 상태 적용, Gameplay 판단 | 4 |
| `SettingManager` | 설정 값 소유(Locale), 검증, 저장, `LocaleChanged` 알림 | Unity Localization API 직접 호출(`UnityLocaleAdapter`가 한다, `06_LOCALIZATION.md`) | 4, 6 |
| `DataManager` | Generated JSON Load, 검증, Definition 조회 | CSV Parse, Handle 소유, Runtime/Save 상태 소유 | 5 |
| `UIManager` | 화면 Prefab을 Load해 UI Root에 띄우고 닫기 | Gameplay Rule 계산 | 7 |
| `SceneManagerEx` | Scene 이름 상수, Main Scene Load | 초기화 순서 결정 | 3 |
| `RunManager` | 런(100일) 상태의 주인. 새 런, 이어하기, 로비 명령, 귀환 정산 적용, 런 저장 | 전투 계산(Domain이 한다), 화면 표시 | 7, 8 |
| `ExpeditionManager` | 원정과 전투 진행의 주인. 노드 선택, 전투 세션 진행과 입력 기록, 보상, 원정 종료 | 런 상태 직접 변경(`RunManager`의 명령을 부른다) | 7, 8 |
| `SoundManager` | 효과음·배경음 재생 | 음량 값 소유(`SettingManager`가 한다) | 10 |

`RunManager`와 `ExpeditionManager`가 Application 계층이다. 규칙은 Domain의 순수 C# 코드가 계산하고, 이 둘은 명령을 검증하고
Domain을 호출하고 상태를 확정·저장한 뒤 알린다. 명령과 단계의 상세는 `11_APPLICATION_FLOW.md`가 소유한다.

## 초기화 순서

```text
Boot -> AppRoot 중복 검사 -> Manager 생성 -> Managers.Configure
-> Save 저장소 초기화 -> Settings Load
-> ResourceManager 초기화, App Scope 열기 -> Localization 초기화 -> 저장 Locale 적용
-> Static Data Load/검증
-> Main Scene Load -> Main Binding -> (런 Save가 있으면 이어하기 준비) -> Main UI 표시 -> Initialized
```

- 각 단계는 `BootStep`에 이름과 안정적인 Error Code를 가진다. 단계는 그 영역이 구현되는 Roadmap 단계에서 추가한다.
- 실패하면 단계 이름과 Error Code를 `BootstrapView`에 보이고 멈춘다. 부분 초기화로 Main에 들어가지 않는다.
- `BootstrapView`는 Addressables, Localization 없이 동작한다. 고정 영어 문구와 Latin Font만 쓴다.
- Editor에서 Main Scene을 직접 Play하면 `MainSceneRoot`가 Boot Scene으로 되돌린다.
- 초기화 코드에서 Animation/Tween 완료나 `WaitForEndOfFrame`을 기다리지 않는다.

## 명령과 알림

- 명령과 반환값이 필요한 작업은 직접 호출한다.
- Typed Event는 저장까지 성공해 확정된 사실을 알릴 때만 쓴다. Manager가 C# `event`로 노출한다.
- 순서: `검증 -> 변경 -> Stable State -> Snapshot -> Save 성공 -> Event -> UI`.

## Validation

- Manager가 `CLAUDE.md` §4 목록 안에 있는가? 생성자 인자 외의 방법으로 다른 Manager를 얻지 않는가?
- `Managers`의 Fail-fast 규칙 Test가 통과하는가?
- 새 초기화 단계에 `BootStep` 이름과 Error Code가 있는가?
- Boot에서 Main 진입 PlayMode Test가 통과하는가?

## Deferred and Forbidden

- Deferred: `SoundManager`(10단계), 별도 Gameplay Scene, Pool.
- Forbidden: `GameManager`, `EventManager`, `LocalizationManager`, Getter Lazy Init, Domain에 `Managers` 전달,
  부분 초기화 상태로 Main 진입, Scene 수작업 편집.
