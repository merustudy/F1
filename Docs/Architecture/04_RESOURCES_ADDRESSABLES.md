# 04. Resources and Addressables

`ResourceManager`, Logical Address, Group/Label/Scope를 소유한다.

## 통로

```text
Game Code -> Managers.Resource -> ResourceManager -> Addressables
```

- 일반 코드는 Addressables API와 `AsyncOperationHandle`을 만지지 않는다. `ResourceManager`가 Handle을 소유한다.
- Local Addressables만 쓴다. Remote/CDN은 요구가 생길 때.
- Load 실패는 명확히 실패한다. 자동 Placeholder 대체가 없다.
- Localization Package가 만든 Group과 Handle은 Package에 맡긴다.

## Logical Address

물리 경로가 아닌 소문자 kebab 경로다. 파일을 옮겨도 Address가 같으면 코드가 바뀌지 않는다.

```text
data/app/<name>
ui/app/<name>        ui/lobby/<name>        ui/expedition/<name>
unit/job/<key>       unit/enemy/<key>          # 유닛의 전신 그림. key는 Data Id의 `_`를 `-`로 바꾼 것
pose/job/<key>-attack  pose/job/<key>-hit      # 용병의 공격·피격 자세. 전신 그림이 확정되면 그려 더하므로 전신 그림이 있는 직업마다 있다 (ArtAddress.PoseOf)
pose/enemy/<key>-attack  pose/enemy/<key>-hit  # 몬스터의 공격·피격 자세. 용병처럼 전신 그림이 있는 적마다 있다 (2026-10-05)
background/dungeon/<key>                       # 던전의 배경
item/<key>                                     # 아이템의 아이콘
potion/<key>                                   # 포션의 병 아이콘
sound/sfx/<key>                                # 효과음 (14_SOUND.md. SoundCatalog가 가리킨다)
sound/bgm/<key>                                # 배경음
<domain>/<category>/<key>                      # 그 밖의 것이 들어올 때
```

형식: 소문자, 숫자, `-`로 된 조각을 `/`로 둘 이상 잇는다. `ResourceManager`가 형식을 검사한다.

## Scope

Scope는 "언제 필요하고 언제 버려도 되는가"로 정한다. Entry마다 Scope Label이 정확히 하나다.

| Scope | Label | 수명 | 담는 것 |
|---|---|---|---|
| `App` | `scope-app` | 앱이 떠 있는 동안 | Static Data, 어느 화면에서나 쓰는 UI, 소리(효과음과 배경음. `14_SOUND.md`) |
| `Lobby` | `scope-lobby` | 로비 화면이 떠 있는 동안 | 로비 UI |
| `Expedition` | `scope-expedition` | 원정 출발부터 귀환까지 | 노드 맵, 전투, 보상, 결과 UI, 유닛의 그림, 던전의 배경 |

- 기획의 "런"(100일 전체)은 Scope가 아니다. 런의 Static Data는 작아서 `App`에 둔다.
- 여러 Scope가 쓰는 Asset은 더 긴 수명의 Scope로 올린다.
- 개별 Release가 아니라 **Scope Release**가 기본이다.

## ResourceManager API

```text
InitializeAsync()
BeginScope(scope) / IsScopeOpen(scope)
LoadAsync<T>(address, scope)                 # 같은 Scope에서 같은 Address는 한 번만 Load하고 같은 Asset을 돌려준다
InstantiateAsync(address, parent, scope)
ReleaseInstance(instance)
ReleaseScope(scope)                          # 그 Scope의 Asset Handle과 Instance를 전부 놓는다
```

- 열리지 않은 Scope에 Load하면 예외다. 이미 열린 Scope를 또 열어도, 닫힌 Scope를 Release해도 예외다.
- 등록되지 않은 Address는 `ResourceLoadException`이다. 에러 로그 없이 예외로만 알린다.
- Scene이 Unload되면 그 Scene에 있던 Instance는 Scene과 함께 사라지고 Addressables가 그 Handle을 스스로 놓는다.
  `ResourceManager`는 이미 놓인 Handle을 다시 놓지 않는다.

## Group과 Entry

- Group은 책임별 소수다: `F1-Data`, `F1-UI`, `F1-Art`, `F1-Audio`. Scope마다 Group을 만들지 않는다. Group은 첫 Entry가 생길 때 만든다.
- Entry는 Inspector에서 등록하지 않는다. `F1.Editor.Setup.AddressablesSetup`의 Entry 목록에 `(assetPath, address, scope)` 한 줄을 넣고
  Sync한다. Sync는 멱등하고 체인의 setup 단계가 부른다.
- Sync는 목록에 없는 Entry를 `F1-*` Group에서 지운다.
- Data Entry는 `StaticDataFiles`에서, 화면 Prefab Entry는 `ScreenCatalog`에서, 소리 Entry는 `SoundCatalog`에서, 그림 Entry는 Static Data의 `Figure`, `Background`, `Icon` 값에서 자동으로 나온다
  (`ArtSetup`. 파일의 자리는 Address에서 정해진다: `unit/enemy/goblin-raider` -> `Assets/@Art/Unit/Enemy/goblin_raider.png`,
  `item/herb-pouch` -> `Assets/@Art/Item/herb_pouch.png`). 손으로 적는 목록이 아니다.
- `AddressablesSetup.FindProblems`가 검사한다: Asset 파일 존재, Address 형식과 중복, Scope Label이 정확히 하나, 목록에 없는 Entry.
  EditMode Test가 이 검사를 부른다.
- UI의 그림(`Assets/@Art/UI`)은 Entry가 아니다. 화면 Prefab이 Sprite를 직접 가리키고 Prefab과 함께 읽힌다(`12_UI.md` "UI의 그림").
- Editor의 Play Mode Script는 "Use Asset Database"다.

## Validation

- 새 Asset이 Entry 목록에 있고 `FindProblems`가 비어 있는가?
- 일반 코드에 `UnityEngine.AddressableAssets` using이 없는가? (`ResourceManager`와 Editor Setup만 쓴다)
- Scope를 연 코드가 그 Scope를 닫는가?

## Deferred and Forbidden

- Deferred: Remote Addressables, Content Update, Pool.
- Forbidden: Handle을 일반 코드에 노출, `Resources.Load`로 Runtime Asset 읽기, 시작 시 전체 Preload, Inspector로만 등록한 Entry.
