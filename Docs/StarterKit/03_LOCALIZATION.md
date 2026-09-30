# 03. Localization

C1 `Docs/Architecture/06_LOCALIZATION.md`의 재사용 가능한 부분이다. Package: `com.unity.localization` (C1은 1.5.12).

## 두 파이프라인 (합치지 않는다)

공유하는 것은 `LocaleCode` 정책 하나뿐이다.

```text
UI Static Text (버튼, 라벨, 에러 문구)
UI_StaticText.csv -> Unity Localization CSV Import -> <P>_UI_Static String Table Collection
-> Locale별 String Table -> LocalizeStringEvent -> Text

Game Data 이름/설명 (아이템, 몬스터, 스킬 ...)
Game Data CSV의 Name.<locale> / Description.<locale> 열 -> DataTransformer -> LocalizedText
-> Generated JSON -> DataManager -> Presentation이 Resolve
```

나누는 이유: Game Data 문구는 그 행의 수치와 같이 편집·리뷰되어야 하고(같은 CSV, 같은 diff), Domain이
Unity Localization Type을 몰라야 순수 C# Test가 된다. UI 문구는 Prefab Reference와 Smart String이 필요하다.

## Locale 정책

- BCP-47 canonical 코드 문자열: `en-US`, `ko-KR`. 기본/Fallback은 `en-US`.
- Save에는 **`LocaleCode` 문자열**만 저장한다. Enum, Dropdown Index, `AvailableLocales` Index 금지.
- 작은 순수 C# `LocalePolicy` 하나가 Source다.

```csharp
public static class LocalePolicy
{
    public const string DefaultCode = "en-US";
    public static readonly IReadOnlyList<string> SupportedCodes = new[] { "en-US", "ko-KR" };
}
```

사용처: Setting 검증, 언어 선택 UI, Unity Locale Adapter, **DataTransformer의 Locale 열 검증**, Editor Validation.
번역·Font·Asset이 준비되기 전에 `SupportedCodes`에 추가하지 않는다. `SupportedCodes`(정책)와
`AvailableLocales`(실제 Asset)의 일치를 Bootstrap/Editor에서 검사한다.

## 물리 경로

```text
Assets/@Localization
├─ Source/UI_StaticText.csv
├─ Settings/LocalizationSettings.asset
├─ Locales/Locale_en-US.asset, Locale_ko-KR.asset
└─ Tables/<P>_UI_Static Shared Data.asset, <P>_UI_Static_en-US.asset, <P>_UI_Static_ko-KR.asset
```

Collection은 하나로 시작한다. 화면별로 미리 나누지 않는다.

## UI Static: CSV가 Source

```csv
Key,Shared Comments,en-US,ko-KR
Main.Title,Main menu title.,SLOT QUEST,슬롯 원정
Main.NewRun,Starts a fresh run.,NEW RUN,새 게임
```

- Key 형식: `<Area>.<Element>[.<ActionOrState>]`, PascalCase. 예: `Main.NewRun`, `Settings.Language`, `Common.Confirm`, `Error.DataLoadFailed`.
- 표시 Text, Prefab 경로, 순번, LocaleCode를 Key로 쓰지 않는다. Text가 바뀌어도 Key는 유지. 삭제된 Key를 다른 뜻으로 재사용하지 않는다.
- String Table Inspector에서 직접 고쳐 두 번째 Source를 만들지 않는다.
- Import는 **명시적**이다. AssetPostprocessor 자동 Import를 만들지 않는다(Import Loop와 예상 밖 Asset 변경을 막는다).

C1 구현: `Editor/Data/LocalizationSetup.cs`

```csharp
[MenuItem("<P>/Localization/Sync UI Strings")]
public static void SyncMenu()   // Settings/Locale/Collection 보장 -> CSV Merge Import -> "<P>_LOCALIZATION_SYNC_DONE"
```

Batch로 부를 수 있어서 에이전트가 CSV만 고치고 체인을 돌리면 Table이 따라온다.

> 주의: C1 Architecture 문서는 `Key,Id,...` 열로 Unity Entry Id를 CSV에 보존하라고 하지만 실제 C1 CSV에는
> `Id` 열이 없다(`Key,Shared Comments,en-US,ko-KR`). Prefab이 Entry Id로 Reference하는 구조에서 Key Rename을
> 안전하게 하려면 새 프로젝트에서는 **처음부터 `Id` 열을 둘지 결정**한다. Key로만 Reference하고 Rename을
> "삭제 + 추가"로 취급하면 Id 열 없이도 된다.

### Binding 규칙

- 정적 UI: Prefab/Scene의 `LocalizeStringEvent` → Table Entry Reference. `label.text = "New Run"` 같은 하드코딩 금지.
- Runtime 값이 들어가면 Smart String: `Floor {floor}`, `Score: {score}`. 번역과 값을 코드 `+`로 잇지 않는다. Argument 의미는 Shared Comment에 적는다.
- Localization 결과 문자열을 계산·비교·Save에 되돌리지 않는다.
- 숫자 표시 형식은 별도 Formatter가 만들고 그 문자열을 Argument로 넘긴다.

### Missing 처리

- Build 실패 값: Entry 누락, 빈 값, `TODO`/`TBD`/`[MISSING]`.
- Runtime 최후 방어: 현재 Locale → `en-US` → `[Missing:<Key>]`. 빈 문자열로 숨기지 않는다.

## Game Data: `LocalizedText`

Unity Type을 모르는 순수 Value다(C1 `Assets/@Scripts/Data/Localization/LocalizedText.cs`).

```csharp
public sealed class LocalizedText
{
    public const string MissingMarker = "[Missing]";
    readonly IReadOnlyDictionary<string, string> _values;   // LocaleCode -> text

    public LocalizedText(IReadOnlyDictionary<string, string> values) { ... }

    /// current locale -> default locale -> explicit missing marker
    public string Resolve(string localeCode) { ... }
    public bool Has(string localeCode) { ... }
}
```

- Definition과 `LocalizedText`는 `Managers`, `SettingManager`, `LocalizationSettings`를 참조하지 않는다.
- Presentation이 현재 LocaleCode를 넘겨 Resolve한다.
- DataTransformer가 `Name.*`/`Description.*` Header 집합이 `LocalePolicy.SupportedCodes`와 같은지 검사한다.

## Runtime Locale 변경

`LocalizationManager`를 만들지 않는다.

- `SettingManager`: LocaleCode 소유, 변경 Command, `settings.json` 저장, `LocaleChanged` Typed Event.
- 작은 `UnityLocaleAdapter`: Unity Localization API 접근(Locale Asset Lookup, `SelectedLocale` 적용, Table 준비 확인). Manager가 아니다.

```text
Supported 검증 -> Locale Asset 존재 -> Table/Font 준비 -> settings.json + Runtime Locale 일관 변경
-> SettingManager.LocaleCode 확정 -> LocaleChanged
```

실패하면 이전 Locale/Settings로 복구한다. 갱신 경로는 둘:

- String Table UI: Unity의 `LocalizeStringEvent`가 스스로 갱신
- `LocalizedText` UI: `LocaleChanged`를 받고 다시 Resolve

### 시작 시

- Settings 없음(첫 실행): System Locale Selector → 지원 안 하면 `en-US`. `PlayerPrefs Locale Selector`는 쓰지 않는다(설정 저장소는 `settings.json` 하나).
- Settings 있음: 저장된 Code 검증 → 유효하면 적용, 무효(빈 값·미지원)면 `en-US`로 Fallback하고 Settings를 정상화. 대소문자 차이(`KO-kr`)만 보정한다.
- 선택 Locale의 Table을 Preload하고 준비된 뒤 Main UI를 보인다.
- **Boot 화면은 Localization에 의존하지 않는다.** 최소 Latin Font를 직접 참조하고, Localization 자체가 실패하면 고정 영어 문구 + 안정적인 Error Code를 보인다.

## Font

```text
Assets/@Fonts/Source/<Family>/   # ttf, LICENSE, README(재생성 절차 + 입력 SHA-256)
Assets/@Fonts/TMP/               # TMP Font Asset (TMP를 쓸 때)
```

- Production Player는 OS Font에 의존하지 않는다. 배포·변형이 License상 허용된 Font만 쓴다.
- UI + Game Data 전체 Corpus의 Glyph Coverage를 검증한다.
- Locale별 Font Swap은 초기 범위가 아니다.

### C1에서 측정한 함정 (legacy `Text`/`Font`)

`TrueTypeFontImporter.fallbackFontReferences`에 한글 Font를 걸어도 **Editor가 OS Font로 한글을 그렸다**
(한글 Font 없는 Player에서만 깨진다). 그래서 C1은 Latin(Poppins ExtraBold) + 한글(Noto Sans KR Black)을
`tools/merge_ui_font.py`(fontTools)로 **한 파일로 합쳐** 쓴다. Test는 "한글이 그려지는가"가 아니라
"Font File이 그 Family인가"를 단언한다.

새 프로젝트 권장: **처음부터 TextMeshPro**. TMP는 Font Asset 단위 Fallback List를 실제로 지원하므로
Latin Primary → Hangul Fallback 구조가 성립하고 병합이 필요 없다. C1은 legacy `Text`로 시작해 전환 비용이 남았다.
legacy `Text`를 쓸 거면 병합 스크립트를 그대로 가져간다.

## Authoring 순서

```text
UI_StaticText.csv 수정
-> Sync(Merge Import)  [Menu 또는 Batch]
-> Required Locale / Key 검증
-> CSV와 Table의 semantic 비교 (byte 비교 아님)
-> Font Glyph Coverage
-> git diff -> CSV + Localization Asset + .meta Commit
```

Player Build는 validate-only다. Build 중 Import/Table 수정/Font 생성을 하지 않는다.

## Locale 추가 절차

```text
번역 준비 -> Font/License 준비 -> LocalePolicy 추가 -> UI CSV 열 추가 -> Game Data CSV 열 추가
-> Locale Asset + String Table 생성 -> Glyph 검증 -> Sync + Transform -> Test -> Supported 등록
```

## Test 후보

EditMode: Supported Code, 중복/무효 Key, Missing Translation, CSV/Table 불일치, Game Data Locale Header, Font Family.
PlayMode: 첫 실행 System Locale와 미지원 Fallback, Saved Locale 우선/무효 Fallback, Runtime 변경, Save 실패 시 일관성, 두 갱신 경로.

## 금지

- `LocalizationManager`, Custom Import Framework, AssetPostprocessor 자동 Import
- 두 파이프라인 합치기
- Locale Index/Enum 저장
- 준비 안 된 Locale Asset 노출
