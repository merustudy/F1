# 01. Project Structure

C1 `Docs/Architecture/01_PROJECT_STRUCTURE.md`의 재사용 가능한 부분이다.

## 저장소 최상위

```text
<repo>
├─ CLAUDE.md                 # 최상위 Authority (CLAUDE.template.md에서 시작)
├─ Docs
│  ├─ Architecture           # 영역별 Owner 문서 (00_..., 01_..., 번호는 읽는 순서일 뿐 우선순위가 아니다)
│  └─ Workstreams            # 기능별 Handoff: README.md, active/, completed/   (06 참고)
├─ Assets                    # 아래 "Assets"
├─ Packages                  # manifest.json + packages-lock.json (둘 다 Git)
├─ ProjectSettings
├─ ArtPipeline               # 이미지·소리 생성 (Unity 밖, 04·05 참고)
│  ├─ STYLE_GUIDE.md         # 그림체의 근거·예외 (사람이 읽는 전체본)
│  ├─ STYLE_RUNTIME.md       # 생성 스크립트가 실행 시 읽는 경량 규칙
│  ├─ References/<Type>/     # 타입별 스타일 레퍼런스 시트
│  ├─ Rosters/*.csv          # 무엇을 그릴지: Id, Key, Type, Subject
│  ├─ Archive/<round>/       # 라운드별 README + samples (폐기 스타일, 후보, 결정 기록)
│  ├─ tools/*.py             # gen_image / gen_sfx / gen_bgm / 로컬 후처리
│  └─ output/                # 생성 직후 산출물 (gitignore)
├─ Tools                     # Unity 밖 보조 도구 (밸런스 시뮬레이터, 데이터 생성기 등). 필요할 때만
├─ .venv                     # Python 가상환경 (gitignore)
├─ .gitignore / .gitattributes
└─ .claude/skills, skills-lock.json   # 에이전트 스킬 (C1은 ElevenLabs music / sound-effects)
```

`ArtPipeline`과 `Tools`는 `Assets` 밖에 둔다. Unity가 Import하지 않고 `.meta`도 생기지 않는다.
승인된 산출물만 `Assets/@Art`, `Assets/@Audio`로 복사한다.

## Assets

프로젝트가 **직접 작성·관리하는** 최상위 폴더에만 `@` Prefix를 붙인다. Project 창에서 위로 정렬되고
Package/Vendor 폴더와 한눈에 구분된다.

```text
Assets
├─ @Scripts
├─ @Scenes          # Boot.unity (index 0), Main.unity (index 1)
├─ @Prefabs
├─ @Data            # Source/*.csv, Generated/{App,Run}/*.json
├─ @Localization    # Source/UI_StaticText.csv, Settings/, Locales/, Tables/
├─ @Art
├─ @Audio           # SFX/*.wav, BGM/*.mp3
├─ @Fonts           # Source/<Family>/ (ttf + LICENSE + README), TMP/
├─ @Tests           # EditMode/, PlayMode/
│
├─ AddressableAssetsData   # Package가 만든 경로 그대로 (옮기지 않는다)
├─ Plugins                 # Native/Special Plugin, DOTween 등 설치 도구가 관리하는 경로
├─ Resources               # Vendor가 요구하는 작은 Settings만 (예: DOTweenSettings.asset)
├─ ThirdParty              # 일반 외부 Asset (실제로 들어올 때 생성)
└─ Settings                # URP 등 Template/Package 설정
```

규칙:

- Package가 만든 Asset과 Vendor Asset에는 `@` 규칙을 강제하지 않는다. 옮기면 Update 경로가 깨진다.
- `Assets/Resources`는 프로젝트 Runtime Asset 저장소로 쓰지 않는다. Runtime Asset은 Addressables로만 읽는다.
- `StreamingAssets`는 초기에는 만들지 않는다.
- Template의 `SampleScene`은 Boot/Main이 Build Settings에 등록되고 Player 진입이 검증된 뒤 `.meta`와 함께 지운다.

## @Scripts

폴더는 **Assembly 경계가 아니다.** Runtime은 단일 `<P>.Runtime` asmdef 하나다.

```text
Assets/@Scripts
├─ Core
│  ├─ Bootstrap      # AppRoot, BootstrapView, MainSceneRoot, InitializationState
│  ├─ Manager        # Managers (Service Container)
│  ├─ Resource       # ResourceManager, ResourceScope, LogicalAddress
│  ├─ Scene          # SceneManagerEx
│  ├─ Audio          # SoundManager
│  └─ Setting        # SettingManager, LocalePolicy, UnityLocaleAdapter
├─ Data
│  ├─ Definitions    # Definition별 Type (불변, 생성자에서 규칙 검증)
│  ├─ Json           # JSON Record/Wrapper
│  └─ Localization   # LocalizedText
├─ Save
│  ├─ Models         # Save DTO
│  ├─ Migration
│  └─ Storage        # Atomic Write, Backup
├─ Gameplay          # 순수 C# Domain. 하위 폴더는 게임마다 다르다
├─ UI
├─ Utils
└─ Editor            # <P>.Editor asmdef
   ├─ Data           # DataTransformer, CsvTable, CsvValueParser, AddressablesSetup, LocalizationSetup
   ├─ Art            # Sprite Import 설정, Prefab Setup
   ├─ Audio
   └─ UI             # UI Prefab Setup
```

`Editor/*Setup.cs`가 C1의 핵심 습관이다. Addressables Entry, String Table Import, Sprite Import 설정,
UI Prefab의 Font 지정을 **Inspector에서 손으로 하지 않고** 멱등한 static 메서드로 만들어 Menu와
Batch(`-executeMethod`) 양쪽에서 부른다. 그래서 에이전트가 Editor를 열지 않고도 Asset 배선을 끝낼 수 있다.

## @Art / @Audio 예시 (C1 실제)

```text
@Art/<Domain>/<Category>/<key>.png      # 예: @Art/Slot/Relics/fairy_dust.png, @Art/UI/Tree/...
@Audio/SFX/<kebab-name>.wav             # 44.1kHz mono 16bit
@Audio/BGM/<kebab-name>.mp3             # 44.1kHz stereo, Streaming
```

물리 경로와 Addressables Address는 별개다. 코드는 `slot/relic/fairy-dust` 같은 Logical Address만 안다
(02, 06 참고). 파일을 옮겨도 Address가 같으면 코드가 바뀌지 않는다.

## 폴더 생성 규율

- 첫 실제 C#/Asset/Test가 생기는 변경에서 필요한 상위 폴더만 만든다.
- 위 Tree는 **최종 논리 구조**다. 문서에 적혀 있다는 이유로 빈 폴더, `.gitkeep`, Placeholder 파일을 만들지 않는다.
- Test 폴더와 asmdef도 첫 Test와 함께 만든다.
- Asset 이동·삭제는 `.meta`와 Reference를 함께 검증한다.

## Git

- `tools/gitignore.example`: Unity 표준 + `.venv/`, `/ArtPipeline/output/`, `.env`, Vendor Demo 폴더.
- `tools/gitattributes.example`: `.unity/.prefab/.asset/.meta/.mat/.anim` 등에 `-whitespace`를 걸어
  `git diff --check`가 손으로 쓴 텍스트에만 의미를 갖게 한다.
- Generated JSON, Localization Asset, 승인된 PNG/오디오, `.meta`는 모두 Git으로 관리한다.
- 큰 바이너리가 많아질 것 같으면 **처음부터** Git LFS를 검토한다(C1은 쓰지 않았고 `ArtPipeline`의 보관용 PNG 148장이 일반 Git History에 있다).

## Validation 체크리스트

- 직접 관리 최상위 Asset 폴더가 `@` Prefix인가?
- 첫 실제 파일 없이 만든 폴더가 없는가?
- Scene/Data/Localization/Font/Test 경로가 이 문서와 같은가?
- Package/Vendor 경로를 임의로 옮기지 않았는가?
- Asset 이동·삭제 시 `.meta`가 함께 처리됐는가?
