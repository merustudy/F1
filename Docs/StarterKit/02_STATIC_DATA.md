# 02. Static Data

C1 `Docs/Architecture/05_STATIC_DATA.md`에서 게임 고유 열 설명을 뺀 구조다.

## Pipeline

```text
CSV (사람이 편집, Source of Truth)
-> DataTransformer (Editor-only, 검증 + 변환)
-> JSON (Generated, Git 관리, 사람이 편집하지 않음)
-> Addressables (Logical Address: data/app/<name>, data/run/<name>)
-> ResourceManager (TextAsset Load, Handle 소유)
-> DataManager (Deserialize + Runtime 재검증)
-> Dictionary<int, T>
```

- Runtime은 CSV를 Parse하지 않는다.
- Static Data를 ScriptableObject 중심으로 대체하지 않는다. (CSV는 diff가 읽히고, 에이전트와 스프레드시트가 둘 다 편집할 수 있다.)
- Static Data / Runtime State / Save DTO는 서로 다른 Type이며 `TemplateId` 같은 정수 ID로만 연결한다.

## 물리 경로

```text
Assets/@Data
├─ Source/<Definition>Data.csv
└─ Generated
   ├─ App/<Definition>Data.json     # 앱 수명 (Bootstrap에서 Load)
   └─ Run/<Definition>Data.json     # 한 판(세션) 수명 (시작/이어하기 준비에서 Load, 종료 시 Release)
```

App/Run 분류는 크기가 아니라 **언제 필요하고 언제 버려도 되는가**로 정한다. 새 게임에 "판" 개념이 없으면
`App` 하나로 시작하고, 수명이 다른 Data가 실제로 생길 때 나눈다. 전체를 시작 시 Blanket Preload하지 않는다.

## Definition 단위

- 거대한 단일 GameData 파일을 만들지 않는다. **Definition 하나 = CSV 하나 = Type 하나 = JSON 하나.**
- 콘텐츠가 구체화된 Definition만 만든다.
- Definition Type은 불변이고 **생성자가 자기 규칙을 검증**한다. Transformer는 행마다 실제 Type을 만들어
  그 규칙을 그대로 건다(규칙을 Transformer에 복사하지 않는다).

## CSV 형식

```csv
RelicId,Key,Trigger,EffectKind,Magnitude,Price,Name.en-US,Name.ko-KR,Description.en-US,Description.ko-KR,IconResourceKey
1,fairy_dust,OnSymbolActivated,AddChip,5,6,Fairy Dust,요정 가루,Adds 5 attack ...,점수 내는 ...,slot/relic/fairy-dust
```

- UTF-8, Header Row 필수. **Header 이름으로 Mapping**하고 열 순서에 의존하지 않는다.
- quoted field / escaped quote / 필드 안 개행을 처리하는 진짜 CSV Parser를 쓴다. `Split(',')` 금지.
- `<Id>` 열: 0보다 큰 정수, Definition 안에서 Unique. `Key` 열: snake_case 안정 식별자(아트·로그·도구용).
- `IconResourceKey` 같은 Asset 참조는 Logical Address 문자열(`<domain>/<category>/<kebab-key>`)이다.
- Localization 열은 `Name.<LocaleCode>`, `Description.<LocaleCode>` (03 참고).
- 목록 값은 열 안에서 쉼표가 아닌 구분자(`+`, `/`)를 쓰면 quoting이 줄어든다.
- 새 열은 **끝에, 선택 열로** 붙이면 기존 도구가 깨지지 않는다.

> C1의 교훈: 감사 Test와 Unity 밖 시뮬레이터가 CSV를 naive하게(`Split(',')`, 열 index) 읽는 바람에
> "이름·설명에 쉼표 금지", "끝 열을 늘리면 세 군데 index 수정" 같은 제약이 생겼다. 새 프로젝트에서는
> **보조 도구도 같은 Parser 코드나 Generated JSON을 읽게** 해서 이 제약을 만들지 않는다.

## Explicit Mapping과 형 변환

DataTransformer는 Reflection으로 Header를 Field에 자동 연결하는 범용 Serializer가 **아니다**.
Definition별로 작은 명시적 Mapper를 둔다.

- Required/Optional과 Default를 Mapper에 의도적으로 적는다. 누락을 CLR Default로 조용히 바꾸지 않는다.
- `int`/`long`: Invariant Culture, 전체 문자열 소비, 범위·음수 허용 여부 검증.
- `bool`: 승인된 값(`true`/`false`)만. 애매한 Truthy 변환 금지.
- Enum: 정의된 이름만. 미정의 값 거부.
- 빈 값: Optional로 명시된 Field만.
- Whitespace, 천 단위 구분자, Locale 의존 소수 해석 금지.
- 소수 배율이 필요하면 float 대신 **Scale 정수**를 검토한다(C1: `×4.5` → `4500`, Scale 1000). 결정적 계산이 필요 없는 게임이면 생략.

C1 파일 구성(참고): `CsvTable.cs`(Parser), `CsvValueParser.cs`(형 변환 + 에러 Context), `DataTransformer.cs`(Mapper·검증·출력).
C1에서는 `DataTransformer.cs`가 1,400줄 넘게 자랐다. 새 프로젝트는 처음부터 Definition별 partial/파일로 나눈다.

## Validation (조용히 넘어가지 않는다)

행 단위 형식 검증 → 전체 Parse 후 Dataset·Cross-reference 검증.

실패 조건:

- Required Header/Value 누락, 열 수 불일치
- 형식 오류, Range/Overflow, 미정의 Enum
- Duplicate ID
- 참조 대상 ID 없음, 참조 대상의 타입/용도 불일치
- App Data가 Run-only Definition에 의존하는 수명 위반
- Game Data Locale Header가 `LocalePolicy.SupportedCodes`와 불일치

에러 메시지는 **Source Path, Row, Header, Definition, 원인**을 담는다. Warning 찍고 계속 가지 않는다.

`DataManager`도 Load 때 Duplicate Key와 핵심 Cross-reference를 다시 검사해 명확히 실패한다(Generated가
손상되거나 stale일 때의 방어선).

## Generated JSON

```json
{
  "SchemaVersion": 1,
  "Items": []
}
```

- Item은 Primary ID 오름차순. Property 순서, Newline, Encoding을 고정해 **같은 입력 → 같은 byte**.
- Timestamp, Machine Path, GUID 같은 비결정 값을 넣지 않는다.
- Newtonsoft Json.NET (`com.unity.nuget.newtonsoft-json`).
- Git ignore 대상이 아니다. Source와 같은 Commit에 넣는다.

## Atomic Transform

```text
Source 전부 읽기 -> Parse/Map -> Type/ID/Cross-reference 검증
-> 임시 결과 생성 -> 전체 성공 확인 -> Generated 교체
```

일부 Definition만 새로 써진 상태를 성공으로 남기지 않는다. 실패하면 기존 Generated를 보존한다.

## Editor Tool과 Batch

```csharp
[MenuItem("<P>/Data/Transform Static Data")]
public static void TransformMenu()   // 끝에 "<P>_DATA_TRANSFORM_DONE" 로그
```

```bash
"$UNITY" -batchmode -quit -nographics -projectPath . -executeMethod <P>.Editor.Data.DataTransformer.TransformMenu -logFile "$LOG"
```

- Transform / Validate-only / stale check를 분리할 수 있다.
- **Player Build는 validate-only**다. Build 중 CSV/JSON/Addressables Entry를 자동 수정하지 않는다.
- stale 검사: Source에서 재생성한 결과와 tracked Generated가 다르면 Test 실패.

## DataManager 책임

한다: `ResourceManager`로 JSON TextAsset Load, Deserialize, 검증, `Dictionary<int, T>` 조회, App/Run 수명 구분.
하지 않는다: CSV Parse, Addressables Handle 소유, Runtime/Save State 소유, 자동 Content Fallback, UI String Table 관리.

## 새 Definition 추가 절차

```text
1. Definitions/<Name>Data.cs (불변 Type + 생성자 검증)
2. @Data/Source/<Name>Data.csv
3. DataTransformer에 Mapper + Cross-reference
4. Json Record, DataManager Load/Dictionary
5. AddressablesSetup의 DataEntries에 (assetPath, "data/<app|run>/<kebab>", "scope-<app|run>") 한 줄
6. EditMode Test: Header 순서 변경, 누락, 중복 ID, 참조 실패, byte-stable
7. Transform -> Addressables Sync -> Test 체인
```

## EditMode Test 후보

- Header 순서 변경 / Required Header 누락
- quoted comma / newline / escaped quote
- 숫자·Enum·ID Parse와 Overflow
- Duplicate ID, Cross-reference 실패
- 같은 입력의 byte-stable JSON
- 전체 실패 시 기존 Generated 보존
- tracked Generated stale 검출
- (권장) **출고 Data 전수 감사**: 모든 행이 Load되고, 모든 `IconResourceKey`가 Addressables Entry와 실제 파일을 가진다

## 금지

- Runtime CSV Parsing, ScriptableObject 중심 Static Data
- 거대한 단일 GameData/Mapper, Reflection Data Engine
- Generated JSON 수동 편집 또는 Git 제외
- Build 중 Source/Generated 자동 변경
