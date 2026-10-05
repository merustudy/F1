# 01. Project Structure

Asset의 물리 경로, `@` Prefix, Folder 생성 시점을 소유한다.

## 저장소 최상위

```text
<repo>
├─ CLAUDE.md                 # 최상위 Authority
├─ Docs
│  ├─ Architecture           # Owner 문서 (번호는 읽는 순서일 뿐 우선순위가 아니다)
│  ├─ Design                 # 기획 입력
│  ├─ StarterKit             # 구조 제안 (규칙 아님)
│  ├─ Workstreams            # Handoff: README.md, active/, completed/
│  └─ Roadmap.md
├─ Assets
├─ Packages                  # manifest.json + packages-lock.json (둘 다 Git)
├─ ProjectSettings
├─ Tools                     # Unity 밖 도구: chain.sh(검증 체인), screenshots.sh(화면 PNG), Sim(시뮬 실행기)
├─ ArtPipeline               # 그림 생성: 스타일 문서, 기준 그림, Roster, 스크립트, 라운드 기록 (13_ART_PIPELINE.md)
├─ .venv                     # ArtPipeline의 Python 가상환경 (gitignore)
└─ .gitignore / .gitattributes
```

`Tools`와 `ArtPipeline`은 `Assets` 밖에 둔다. Unity가 Import하지 않고 `.meta`도 생기지 않는다.

## Assets

프로젝트가 직접 작성·관리하는 최상위 폴더에만 `@` Prefix를 붙인다.

```text
Assets
├─ @Scripts
├─ @Scenes          # Boot.unity (index 0), Main.unity (index 1)
├─ @Tests           # EditMode/, PlayMode/
├─ @Data            # Source/*.csv, Generated/*.json
├─ @Localization    # Source/UI_StaticText.csv, Settings/, Locales/, Tables/
├─ @Fonts           # Source/<Family>/ (ttf + LICENSE + README), TMP/
├─ @Prefabs         # UI/ (화면 Prefab. Setup 코드가 만든 생성물)
├─ @Art             # Unit/Job, Unit/Enemy, Face/Job, Face/Enemy, Pose/Job, Pose/Enemy, Background/Dungeon, Item, UI/Frame, UI/Icon (승인된 그림. ArtPipeline에서 온다 -> 13_ART_PIPELINE.md)
│
├─ AddressableAssetsData   # Package가 만든 경로 그대로
├─ Settings                # URP Template 설정
├─ TextMesh Pro            # TMP Essential Resources
└─ DefaultVolumeProfile.asset, UniversalRenderPipelineGlobalSettings.asset, InputSystem_Actions.inputactions
```

- 위 Tree는 논리 구조다. 폴더는 첫 실제 파일이 생기는 변경에서 만든다. `@Audio`는 소리 단계에서 생긴다.
- Package, Template, Vendor가 만든 Asset에는 `@` 규칙을 강제하지 않고 옮기지 않는다.
- `Assets/Resources`는 프로젝트 Runtime Asset 저장소로 쓰지 않는다. Runtime Asset은 Addressables로만 읽는다.
  (`TextMesh Pro/Resources`는 TMP가 요구하는 경로라 예외다.)
- `StreamingAssets`는 만들지 않는다.

## @Scripts

폴더는 Assembly 경계가 아니다. Assembly와 Namespace는 `03_ASSEMBLY_PACKAGES.md`가 소유한다.

```text
Assets/@Scripts
├─ Core
│  ├─ Bootstrap      # AppRoot, BootstrapView, MainSceneRoot, BootStep, InitializationState
│  ├─ Manager        # Managers (Service Container)
│  ├─ Resource       # ResourceManager, ResourceScope
│  ├─ Scene          # SceneManagerEx
│  └─ Setting        # SettingManager, UnityLocaleAdapter
├─ Data              # Definitions/, Json/, Localization/ (LocalePolicy, LocalizedText)
├─ Save              # Models/, Storage/
├─ Gameplay          # 순수 C# Domain
├─ Flow              # Application 계층: RunManager, ExpeditionManager, BattleSession, BattleClock
├─ UI                # UIManager, UIScreen, Screens/, Views/, UiKeys, UiStrings, UiText, UiPalette
└─ Editor            # F1.Editor asmdef
   ├─ Setup          # ProjectSetup, SceneSetup, AddressablesSetup, ArtSetup, LocalizationSetup, FontSetup
   │  └─ Ui          # UiBuild, UiPrefabSetup.* (화면 Prefab Builder. 이 폴더의 Hash가 Prefab Stamp다)
   └─ Data           # CSV Parser, DataTransformer
```

Asset 설정은 Inspector에서 손으로 하지 않는다. `Editor/Setup`의 멱등한 static 메서드로 만들고 Menu와 Batch(`-executeMethod`) 양쪽에서 부른다.

## 물리 경로와 Address

물리 경로와 Addressables Address는 별개다. 코드는 Logical Address만 안다(`04_RESOURCES_ADDRESSABLES.md`).

## Validation

- 직접 관리하는 최상위 Asset 폴더가 `@` Prefix인가?
- 첫 실제 파일 없이 만든 폴더, `.gitkeep`, Placeholder 파일이 없는가?
- Package/Template/Vendor 경로를 옮기지 않았는가?
- Asset 이동·삭제 시 `.meta`가 함께 처리됐는가?

## Deferred and Forbidden

- Deferred: `@Audio`(소리 단계).
- Forbidden: 미래용 빈 폴더, `Assets/Resources`를 Runtime 저장소로 쓰기, Inspector 수작업으로만 재현되는 Asset 설정.
