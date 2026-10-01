# 05. Static Data

CSV, 변환, Generated JSON, `DataManager`를 소유한다. 열의 뜻과 값은 소유하지 않는다(기획: `Docs/Design`, 값: CSV).

## Pipeline

```text
CSV (사람이 편집, Source of Truth)
-> StaticDataTransformer (검증 + 변환. Unity Editor와 Tools/Sim 양쪽에서 같은 코드가 돈다)
-> JSON (Generated, Git 관리, 사람이 편집하지 않음)
-> Addressables (data/app/<name>)
-> ResourceManager (TextAsset Load)
-> DataManager (StaticDataLoader로 Deserialize + 재검증)
-> StaticData (Dictionary<string, T>)
```

- Runtime은 CSV를 Parse하지 않는다. 게임과 시뮬은 같은 Generated JSON을 같은 `StaticDataLoader`로 읽는다.
- Static Data를 ScriptableObject 중심으로 대체하지 않는다.
- Static Data, Runtime State, Save DTO는 서로 다른 Type이고 **문자열 id**로만 연결한다.

## 식별자

- 모든 Definition의 기본 키는 `Id` 열이다. 영어 snake_case 문자열(`^[a-z][a-z0-9]*(_[a-z0-9]+)*$`)이고 Definition 안에서 유일하다.
- 정수 Id를 따로 두지 않는다. 다른 Definition을 가리킬 때도, Save가 가리킬 때도 이 문자열을 쓴다.
- 조회는 `DataTable<T>`(안에서 `Dictionary<string, T>`)다. 없는 id를 조회하면 예외다. 조용한 기본값이 없다.
- 규칙이 Definition을 순회할 때는 항상 id 순서(`DataTable.Ordered`)로 한다. Dictionary의 순서에 결과가 달려서는 안 된다.

## 물리 경로

```text
Assets/@Data
├─ Source/<Definition>Data.csv
└─ Generated/<Definition>Data.json
```

수명은 전부 앱 수명이다(`data/app/<name>`, Boot에서 Load). 수명이 다른 Data가 실제로 생기면 그때 나눈다.
파일 목록은 `F1.Data.StaticDataFiles`가 가진다. 변환, Load, Addressables Entry가 모두 이 목록을 읽는다.

## Definition 단위

- Definition 하나 = CSV 하나 = Type 하나 = JSON 하나. 거대한 단일 파일을 만들지 않는다.
- Slice 범위 안의, 규칙이 확정된 Definition만 만든다.
- Definition Type은 불변이고 **생성자가 자기 규칙을 검증**한다. Transformer와 Loader는 실제 Type을 만들어 그 규칙을 그대로 건다.
- 아이템과 유물은 서로 다른 Definition, 다른 CSV, 다른 Type이다. 공용 Type으로 합치지 않는다.

## 코드 위치

| 위치 | 내용 | Unity 의존 |
|---|---|---|
| `Assets/@Scripts/Data` | Definition, `StaticDataJson`, `StaticData`, `StaticDataLoader`, `StaticDataFiles`, `LocalizedText`, `LocalePolicy` | 없음 |
| `Assets/@Scripts/Editor/Data/Csv` | `CsvTable`, `CsvRow` | 없음 |
| `Assets/@Scripts/Editor/Data/Transform` | `StaticDataTransformer`, Definition별 Mapper(`DefinitionMappers.cs`), `StaticDataFileStore` | 없음 |
| `Assets/@Scripts/Editor/Data/DataTransformMenu.cs` | Menu와 Batch 진입점 | 있음 |
| `Assets/@Scripts/Core/Data/DataManager.cs` | `ResourceManager`로 Load | 있음 |
| `Tools/Sim` | 위의 Unity 의존 없는 폴더를 그대로 컴파일하는 .NET 콘솔 | 없음 |

Mapper는 Definition마다 Type 하나다. 파일이 커지면 Definition별 파일로 나눈다. Reflection으로 Header를 Field에 자동 연결하지 않는다.
Definition은 JSON으로 직접 직렬화한다(Property 순서는 Attribute로 고정). Load는 Definition의 생성자를 거치므로 같은 검증이 다시 걸린다.

## CSV 형식

```csv
Id,Name.ko-KR,Name.en-US,Level,MaxHp,Items
goblin_shaman,고블린 주술사,Goblin Shaman,5,60,hex_spit:10+mending_chant:12
```

- UTF-8, Header Row 필수. **Header 이름으로 Mapping**하고 열 순서에 의존하지 않는다.
- quoted field, escaped quote(`""`), 필드 안 개행을 처리하는 Parser를 쓴다. `Split(',')` 금지.
- Localization 열은 `<Field>.<LocaleCode>`다. Locale 집합은 `LocalePolicy.SupportedCodes`와 정확히 같아야 한다.
- 다른 Definition 참조는 그 Definition의 `Id` 문자열이다.
- 목록 값은 `+`로 잇는다. 아이템과 등급의 목록은 `item_id:grade`를 `+`로 잇는다. 정수의 목록도 같다(쓸 수 있는 열 `1+2`).
  순서에 뜻이 있는 목록은 적힌 순서를 그대로 쓴다(적 무리의 `Enemies`는 앞에서부터 서는 순서다).
- 다른 열의 값에 따라 비워 두는 칸은 Mapper가 Optional로 읽고, 채워야 하는지는 Definition 생성자가 검증한다
  (효과의 `Reach`는 앞이나 뒤에서 세는 타깃에만 있다).
- `BalanceData.csv`만 `Key,Value` 형식이다. Key는 PascalCase 상수 이름이고 전부 필수다. 모르는 Key는 에러다.
- 모르는 Header는 에러다(오타를 조용히 넘기지 않는다).

## 형 변환

- Required/Optional과 Default를 Mapper에 명시한다. 누락을 CLR Default로 바꾸지 않는다.
- 정수: Invariant Culture, 전체 문자열 소비, 범위 검증. 앞뒤 공백, `+` 부호, 천 단위 구분자 금지.
- bool: `true`/`false`만. Enum: 정의된 이름만.
- Gameplay 수치에 소수를 쓰지 않는다. 비율은 정수(백분율이나 천분율)로 적는다.

## Validation

행 단위 형식 검증 -> 전체 Parse 후 Dataset과 Cross-reference 검증. 하나라도 실패하면 전체가 실패한다.

- Required Header/Value 누락, 열 수 불일치, 모르는 Header
- 형식 오류, 범위 초과, 미정의 Enum
- Duplicate Id, 참조 대상 Id 없음
- Locale Header가 `LocalePolicy.SupportedCodes`와 불일치, 빈 문구

에러 메시지는 Source 파일, Row, Header, 원인을 담는다. Warning을 찍고 계속 가지 않는다.
`StaticDataLoader`도 Load 때 같은 생성자 검증과 Dataset 검증을 다시 건다.

## Generated JSON

```json
{
  "SchemaVersion": 1,
  "Items": []
}
```

- Item은 `Id` 오름차순(Ordinal). Property 순서, 들여쓰기(2칸), Newline(`\n`), Encoding(UTF-8, BOM 없음)을 고정해 **같은 입력 -> 같은 byte**.
- Timestamp, Machine Path 같은 비결정 값을 넣지 않는다.
- Git ignore 대상이 아니다. Source와 같은 Commit에 넣는다.
- Load는 엄격하다. 모르는 Property나 빠진 Property는 에러다.

## Atomic Transform

```text
Source 전부 읽기 -> Parse/Map -> 검증 -> 결과를 메모리에 생성 -> 전체 성공 확인 -> Generated 교체
```

실패하면 기존 Generated를 그대로 둔다. 내용이 같은 파일은 다시 쓰지 않는다.

## 실행

```bash
Tools/chain.sh setup                 # Unity Batch: 변환 + Addressables 동기화
dotnet run --project Tools/Sim -- transform    # Unity 없이 변환 (시뮬 우선 루프)
dotnet run --project Tools/Sim -- validate     # Generated가 Source와 같은지, Load가 되는지 확인
```

- Unity 메뉴: `F1/Data/Transform Static Data`. 끝에 `F1_DATA_TRANSFORM_DONE` 로그.
- Player Build는 validate-only다. Build 중에 Source나 Generated를 바꾸지 않는다.
- stale 검사: Source에서 다시 만든 결과와 Git의 Generated가 다르면 EditMode Test가 실패한다.

## 새 Definition 추가 절차

```text
1. Data/Definitions/<Name>Data.cs (불변 Type + 생성자 검증 + JSON Attribute)
2. StaticDataFiles에 Entry, StaticData에 Table과 Cross-reference, StaticDataLoader의 Load와 Serialize
3. @Data/Source/<Name>Data.csv
4. Editor/Data/Transform에 Mapper, StaticDataTransformer에 연결
5. EditMode Test (TestData, TestCsv에도 추가)
6. Tools/chain.sh (변환 -> Addressables 동기화 -> Test)
```

Addressables Entry는 `StaticDataFiles`에서 자동으로 나온다.

## Validation 체크리스트

- 새 상수가 CSV에 있는가? 코드에 밸런스 숫자가 없는가?
- Generated JSON을 손으로 고치지 않았는가? Source와 같은 Commit인가?
- `Data`, `Editor/Data/Csv`, `Editor/Data/Transform`에 `UnityEngine`/`UnityEditor` 참조가 없는가? (`Tools/Sim` Build로 확인)
- 새 Definition에 누락, 중복 Id, 참조 실패 Test가 있는가?

## Deferred and Forbidden

- Deferred: 수명별 Data 분리, Build 시 validate-only Hook, 스프레드시트 연동.
- Forbidden: Runtime CSV Parsing, ScriptableObject 중심 Static Data, 거대한 단일 GameData, Reflection Data Engine,
  Generated JSON 수동 편집 또는 Git 제외, 정수 Id 병행, 아이템과 유물의 공용 Type.
