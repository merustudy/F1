# Stage 05 — 데이터 파이프라인

Snapshot: 2026-09-30 (완료)

## Goal

CSV에서 시작해 게임과 시뮬이 같은 Generated JSON을 읽는 파이프라인이 첫 데이터 파일 하나로 끝까지 통한다.

## Done

- Owner 문서: `Docs/Architecture/05_STATIC_DATA.md`.
- 순수 C#(`Assets/@Scripts/Data`): `JobData`, `LocalizedText`, `LocalePolicy`, `DataId`, `StaticData`, `StaticDataFiles`,
  `StaticDataLoader`, `Json/StaticDataJson`, `Json/JobJson`.
- 순수 C#(`Assets/@Scripts/Editor/Data`): `Csv/CsvTable`, `Csv/CsvRow`, `Transform/StaticDataTransformer`, `Transform/JobMapper`,
  `Transform/StaticDataFileStore`.
- Unity 의존: `Editor/Data/DataTransformMenu`(메뉴 `F1/Data/Transform Static Data`), `Core/Data/DataManager`,
  `Editor/Setup/AddressablesSetup.Data`(Entry는 `StaticDataFiles`에서 자동).
- 데이터: `Assets/@Data/Source/JobData.csv` -> `Assets/@Data/Generated/JobData.json` (직업 6종의 id와 이름).
- `Tools/Sim`: `F1.Sim.csproj`, `src/Program.cs`. 명령 `transform`, `validate`.
- 체인: `sim` 단계. `ProjectSetup`이 변환 뒤 Addressables를 동기화한다.
- Boot: BOOT-06 Static Data Load.

## 결정 (권장안, 일괄 승인 범위)

- G14: 식별자는 영어 snake_case 문자열 하나. 정수 Id 없음. 조회는 `Dictionary<string, T>`, 없는 id는 예외.
- 변환 코드는 Editor asmdef 안에 있지만 Unity를 참조하지 않는다. `Tools/Sim`이 같은 파일을 컴파일한다.
- `Tools/Sim`은 NuGet 없이 Unity Package Cache의 `Newtonsoft.Json.dll`을 참조한다. 게임과 같은 JSON 라이브러리를 쓰기 위해서다.
  Unity로 프로젝트를 한 번 열어야(또는 `Tools/chain.sh setup`) Build된다.
- 직업의 한국어 이름(기사, 광전사, 주교, 성기사, 대마법사, 마검사)은 기획문서에 없어 새로 붙였다. `JobData.csv`에서 바꿀 수 있다.

## Verification

- `Tools/chain.sh`: setup OK, sim OK, EditMode 153/153, PlayMode 14/14.

## 알아둘 것

- 데이터를 고친 뒤 Unity 없이 확인하려면 `dotnet run --project Tools/Sim -- transform`.
- Generated JSON은 손으로 고치지 않는다. Source와 다르면 EditMode Test(`ShippedDataTests`)와 체인의 sim 단계가 실패한다.
- 새 Definition을 넣는 절차는 Architecture/05 "새 Definition 추가 절차".
