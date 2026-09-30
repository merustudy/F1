# 03. Assembly, Namespace, Package

Namespace, asmdef, Package를 소유한다.

## Assembly

```text
Assets/@Scripts/F1.Runtime.asmdef                       Root Namespace F1   (Runtime은 하나만)
Assets/@Scripts/Editor/F1.Editor.asmdef                 Editor only         -> F1.Runtime
Assets/@Tests/EditMode/F1.Tests.EditMode.asmdef         Editor only         -> F1.Runtime, F1.Editor
Assets/@Tests/PlayMode/F1.Tests.PlayMode.asmdef                             -> F1.Runtime
```

- Layer나 Feature별로 Runtime asmdef를 나누지 않는다. 폴더는 Assembly 경계가 아니다.
- 금지 방향: Runtime -> Editor, Runtime -> Tests, Editor -> Tests.
- Package asmdef Reference는 그 Type을 처음 쓸 때 추가한다.
- Test asmdef는 `overrideReferences`를 켜고 쓰는 Precompiled DLL(`nunit.framework.dll` 등)을 전부 나열한다.
- Runtime의 `internal`은 `InternalsVisibleTo`로 `F1.Editor`, `F1.Tests.EditMode`, `F1.Tests.PlayMode`에만 연다.

## Domain의 Unity 비의존

`Assets/@Scripts/Gameplay`와 `Assets/@Scripts/Data`는 `UnityEngine`, `UnityEditor`를 참조하지 않는다.
asmdef를 나누지 않고, 시뮬 실행기(`Tools/Sim`)가 이 폴더의 소스를 Unity 없이 직접 컴파일하는 것으로 보장한다.
시뮬 실행기의 Build는 검증 체인에 들어 있다.

- 두 폴더의 코드는 Unity와 .NET SDK 양쪽에서 컴파일된다. C# 9 문법까지만 쓴다.
- JSON은 양쪽 모두 Newtonsoft Json.NET을 쓴다.

## Namespace

폴더를 그대로 따르지 않는다. Unity Type 이름과 겹치는 하위 Namespace를 만들지 않는다.

| Namespace | 폴더 |
|---|---|
| `F1.Core` | `@Scripts/Core/*` 전부 (하위 폴더별 Namespace 없음) |
| `F1.Data` | `@Scripts/Data/*` |
| `F1.Save` | `@Scripts/Save/*` |
| `F1.Gameplay` | `@Scripts/Gameplay/*` |
| `F1.Flow` | `@Scripts/Flow/*` (Application 계층. `UnityEngine.Application`과 겹치지 않게 `Application`이라는 Namespace를 쓰지 않는다) |
| `F1.UI` | `@Scripts/UI/*` |
| `F1.Editor.Setup`, `F1.Editor.Data` | `@Scripts/Editor/*` |
| `F1.Tests` | `@Tests/*` |

`F1.Editor` Namespace 안에서는 `UnityEditor.Editor` Type을 전체 이름으로 쓴다.

## Package

`manifest.json`과 `packages-lock.json`을 항상 함께 확인하고 같은 Commit에 넣는다.

| Package | 용도 |
|---|---|
| `com.unity.render-pipelines.universal` | URP 2D |
| `com.unity.ugui` | uGUI와 TextMeshPro |
| `com.unity.inputsystem` | 입력 (Active Input Handling은 Input System) |
| `com.unity.test-framework` | EditMode/PlayMode Test |
| `com.unity.2d.sprite` | Sprite Import와 편집 |
| `com.unity.ide.rider`, `com.unity.ide.visualstudio` | IDE 연동 |
| `com.unity.addressables` | Resource Load. 2.x 계열(2.11.2)로 고정한다. 3.x, 4.x는 필요가 생길 때 검토한다 |
| `com.unity.nuget.newtonsoft-json` | Save와 Static Data JSON (3.2.2) |
| `com.unity.localization` | UI String Table와 Locale (1.5.13). Addressables 위에서 동작한다 |

- Template이 넣었지만 쓰지 않아 제거한 Package: `2d.animation`, `2d.aseprite`, `2d.psdimporter`, `2d.spriteshape`, `2d.tilemap`,
  `2d.tilemap.extras`, `2d.tooling`, `collab-proxy`, `multiplayer.center`, `timeline`, `visualscripting`.
  필요해지면 그때 다시 넣는다.
- 내장 모듈(`com.unity.modules.*`)은 Template 그대로 둔다.
- TMP Essential Resources는 `com.unity.ugui` 안의 unitypackage를 풀어 `Assets/TextMesh Pro`에 넣었다
  (Editor 메뉴 Window > TextMeshPro > Import TMP Essential Resources와 같은 내용).
- UI는 uGUI + TextMeshPro다. legacy `Text`를 쓰지 않는다.

## Validation

- Runtime asmdef가 하나인가? 금지 방향 Reference가 없는가?
- `Gameplay`, `Data` 폴더에 `UnityEngine`/`UnityEditor` 참조가 없는가? (시뮬 실행기 Build로 확인)
- Package를 바꿨다면 `manifest.json`과 `packages-lock.json`이 함께 바뀌었는가?

## Deferred and Forbidden

- Deferred: DOTween 같은 Vendor Tween, Remote Addressables.
- Forbidden: 미래 가능성만으로 Package 설치, Runtime asmdef 분할, 범용 DI/Reactive Framework.
