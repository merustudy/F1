# 06. Localization

UI 문구와 Game Data 문구의 Localization, Locale, Font를 소유한다. Package는 `com.unity.localization`이다.

## 두 파이프라인 (합치지 않는다)

공유하는 것은 `LocalePolicy` 하나뿐이다.

```text
UI Static Text (버튼, 라벨, 로그 문장, 에러 문구)
UI_StaticText.csv -> LocalizationSetup.Sync -> F1_UI_Static String Table Collection
-> Locale별 String Table -> LocalizeStringEvent 또는 UiStrings.Get -> Text

Game Data 이름 (직업, 아이템, 적, 던전, 용병 ...)
Game Data CSV의 Name.<locale> 열 -> StaticDataTransformer -> LocalizedText
-> Generated JSON -> DataManager -> 화면이 현재 Locale로 Resolve
```

나누는 이유: Game Data 문구는 그 행의 수치와 같은 CSV, 같은 diff에서 고쳐야 하고, Domain과 시뮬이 Unity Localization을 몰라야 한다.

## Locale 정책

- 코드는 BCP-47 문자열이다: `ko-KR`, `en-US`. 기본과 Fallback은 `ko-KR`.
- `F1.Data.LocalePolicy`가 유일한 출처다. 설정 검증, 언어 선택 UI, 변환기의 Locale 열 검증, Localization 검사가 모두 이것을 읽는다.
- Save에는 Locale 코드 문자열만 저장한다. Enum, Index를 저장하지 않는다.
- 번역과 Font가 준비되기 전에 `SupportedCodes`에 Locale을 추가하지 않는다.
- `SupportedCodes`와 실제 Locale Asset이 다르면 Editor 검사와 Boot가 실패한다.

## 물리 경로

```text
Assets/@Localization
├─ Source/UI_StaticText.csv
├─ Settings/LocalizationSettings.asset
├─ Locales/Locale_ko-KR.asset, Locale_en-US.asset
└─ Tables/F1_UI_Static Shared Data.asset, F1_UI_Static_ko-KR.asset, F1_UI_Static_en-US.asset
```

Collection은 하나다(`F1_UI_Static`). 화면별로 나누지 않는다.

## UI 문구: CSV가 Source

```csv
Key,Shared Comments,ko-KR,en-US
Title.NewRun,Starts a fresh run.,새 런,New Run
Lobby.Day,{0} = current day. {1} = total days.,{0}일차 / {1}일,Day {0} / {1}
```

- Key 형식은 `<Area>.<Element>[.<State>]`, 조각마다 PascalCase다. 표시 문구, 경로, 순번을 Key로 쓰지 않는다.
- Key로만 참조한다. `Id` 열을 두지 않는다. Key를 바꾸는 것은 삭제하고 새로 넣는 것이다.
- String Table을 Inspector에서 고치지 않는다. CSV를 고치고 Sync한다.
- Import는 명시적이다(`F1/Localization/Sync UI Strings`, 체인의 setup). AssetPostprocessor 자동 Import를 만들지 않는다.
- Sync는 CSV와 Table을 똑같이 만든다: 없는 Key를 넣고, 다른 값을 고치고, CSV에 없는 Key를 지운다.
- 값을 넣을 자리는 `{0}`, `{1}`로 적는다. `{`가 든 문구는 Smart String으로 표시된다. 자리의 뜻은 Shared Comments에 적는다.
- 빈 값과 `TODO`, `TBD`, `[MISSING]`은 Sync가 거부한다.

### 코드에서 쓰는 법

- Key는 `F1.UI.UiKeys`의 상수로만 쓴다. 문자열을 코드에 직접 쓰지 않는다. `UiKeys`와 CSV의 Key 집합이 같은지 Test가 확인한다.
- Prefab의 고정 라벨: `LocalizeStringEvent`가 Table Entry를 참조한다. UI Setup 코드가 붙인다. Locale이 바뀌면 스스로 갱신된다.
- 실행 중에 만드는 문구(값이 들어가는 라벨, 전투 로그): `UiStrings.Get(key, args)`. `LocaleChanged`를 받으면 다시 만든다.
- 번역 결과 문자열을 비교하거나 계산하거나 저장하지 않는다. 번역과 값을 코드에서 `+`로 잇지 않는다.
- 없는 Key는 `[Missing:<Key>]`로 보인다. 빈 문자열로 숨기지 않는다.

## Game Data 문구: `LocalizedText`

- Unity Type을 모르는 순수 Value다. `Resolve(localeCode)`는 현재 Locale, 기본 Locale, `[Missing]` 순으로 돌려준다.
- Definition과 `LocalizedText`는 `Managers`, `SettingManager`, Unity Localization을 참조하지 않는다. 화면이 현재 Locale 코드를 넘긴다.
- 변환기가 `Name.*` Header 집합이 `LocalePolicy.SupportedCodes`와 같은지 검사한다(`05_STATIC_DATA.md`).

## Runtime Locale

`LocalizationManager`를 만들지 않는다.

- `SettingManager`: Locale 코드의 주인. `ChangeLocaleAsync`가 변경 명령이고, 저장에 성공한 뒤 `LocaleChanged`를 알린다.
- `UnityLocaleAdapter`: Unity Localization API를 만지는 작은 Type이다. Manager가 아니다. 초기화, Locale Asset 확인, `SelectedLocale` 적용.

```text
지원 Locale 검증 -> Runtime Locale 적용(Table Preload) -> settings.json 저장 -> LocaleCode 확정 -> LocaleChanged
```

저장에 실패하면 Runtime Locale을 이전 것으로 되돌린다.

### 시작할 때

- 설정이 없으면(첫 실행): OS 언어가 지원 Locale이면 그것, 아니면 `ko-KR`. `PlayerPrefs`에 Locale을 저장하지 않는다.
- 설정이 있으면: 저장된 코드를 검증하고 적용한다. 무효면 `ko-KR`로 고쳐 저장한다.
- Package의 Startup Selector는 `ko-KR` 고정 하나만 둔다. 실제 Locale은 Boot가 설정에서 읽어 적용한다.
- String Table은 Preload한다(선택한 Locale과 그 Fallback). 준비된 뒤에 Main UI를 보인다. 그래서 `UiStrings.Get`은 기다리지 않는다.
- **Boot 화면은 Localization에 의존하지 않는다.** 고정 영어 문구와 Latin Font(LiberationSans)를 직접 쓴다.

## Font

```text
Assets/@Fonts/Source/Pretendard/   # ttf, LICENSE.txt, README.md(출처와 SHA-256)
Assets/@Fonts/TMP/                 # TMP Font Asset (생성물)
```

- UI는 TextMeshPro와 Pretendard를 쓴다. Latin과 한글을 한 Family가 가져서 Fallback 사슬이 필요 없다.
- Player는 OS Font에 의존하지 않는다. License가 배포를 허용하는 Font만 쓴다.
- TMP Font Asset은 **Static Atlas**다. `FontSetup.Sync`가 Corpus의 글자만 구워 넣는다. Corpus는 ASCII, `UI_StaticText.csv`의 모든 문구,
  Static Data의 모든 `LocalizedText`다.
- Corpus가 바뀌면 setup이 Atlas를 다시 굽는다. 같으면 건드리지 않는다. Font Asset을 손으로 고치지 않는다.
- Corpus의 글자가 Font Asset에 전부 있는지 EditMode Test가 확인한다. 화면에 나오는 문구는 전부 CSV에서 오므로 빠지는 글자가 없다.
- Locale별 Font 교체는 하지 않는다.
- Font Asset은 LFS가 아니라 일반 Git에 둔다. Atlas가 텍스트로 저장되고 대부분 빈칸이라 잘 압축된다. LFS 용량은 그림과 소리에 쓴다.
- CSV에 넣지 않은 글자(코드에 직접 쓴 기호 등)는 화면에 나오지 않는다. 기호가 필요하면 CSV 문구에 넣는다.

## Authoring 순서

```text
UI_StaticText.csv 또는 Game Data CSV 수정
-> Tools/chain.sh setup (변환, String Table Sync, Font Atlas, Addressables)
-> EditMode Test (CSV와 Table 일치, UiKeys 일치, Glyph Coverage)
-> CSV + Table + Font Asset + .meta를 같은 Commit에
```

Player Build는 validate-only다. Build 중에 Import, Table 수정, Font 생성을 하지 않는다.

## Locale 추가 절차

```text
번역 준비 -> Font가 그 글자를 가지는지 확인 -> LocalePolicy 추가 -> UI CSV 열 추가 -> Game Data CSV 열 추가
-> setup (Locale Asset, Table, Atlas) -> Test
```

## Validation

- `LocalizationSetup.FindProblems`와 `FontSetup.FindProblems`가 비어 있는가? (EditMode Test)
- 코드에 표시 문구가 직접 들어가지 않았는가? `UiKeys`에 없는 Key를 쓰지 않았는가?
- Boot 화면이 Localization 없이 동작하는가?

## Deferred and Forbidden

- Deferred: Locale별 Font, 음성, Asset Table, 복수형 같은 Smart Format 확장.
- Forbidden: `LocalizationManager`, AssetPostprocessor 자동 Import, 두 파이프라인 합치기, Locale Index/Enum 저장,
  준비 안 된 Locale 노출, legacy `Text`, OS Font 의존.
