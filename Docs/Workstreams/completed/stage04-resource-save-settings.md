# Stage 04 — ResourceManager, Addressables, Save, Settings

Snapshot: 2026-09-30 (완료)

## Goal

Resource를 Scope 단위로 읽고 놓는 통로, Atomic한 Save I/O, 설정 파일이 있고 Boot가 이들을 초기화한다.

## Done

- Package: `com.unity.addressables` 2.11.2, `com.unity.nuget.newtonsoft-json` 3.2.2.
- Owner 문서: `Docs/Architecture/04_RESOURCES_ADDRESSABLES.md`, `07_SAVE.md`.
- `Assets/@Scripts/Core/Resource`: `ResourceManager`, `ResourceScope`, `LogicalAddress`.
- `Assets/@Scripts/Save`: `SaveManager`, `Storage/SaveStorage`, `Models/SettingsData`.
- `Assets/@Scripts/Core/Setting/SettingManager`, `Assets/@Scripts/Data/Localization/LocalePolicy`.
- `Assets/@Scripts/Editor/Setup/AddressablesSetup`: Entry 목록을 설정에 동기화하고 `FindProblems`로 검사한다. 체인의 setup이 부른다.
- `AppRoot`: Boot 단계 BOOT-03(Save 저장소), BOOT-04(설정), BOOT-05(Resource). `SaveRootOverride`로 Test가 임시 폴더를 쓴다.
- `Assets/AddressableAssetsData`: Package가 만든 설정. Label `scope-app`, `scope-lobby`, `scope-expedition`.

## 결정 (권장안, 일괄 승인 범위)

- G15: Scope는 `App`, `Lobby`, `Expedition`. 기획의 "런"은 Scope가 아니고 Static Data는 `App`에 둔다.
- G7 일부: 파일은 `settings.json`과 `run.json`(8단계), 슬롯 하나.
- Addressables는 2.x 계열로 고정했다. 레지스트리의 최신은 4.1.0이지만 C1에서 같은 Unity와 검증된 계열이 2.x다.
- 등록되지 않은 Address는 에러 로그 없이 `ResourceLoadException`으로만 알린다(Load 전에 Location을 먼저 확인).
- 설정의 기본 Locale은 `ko-KR`. 첫 실행은 OS 언어가 한국어나 영어면 그것을 쓴다.
- Application 계층의 Namespace는 `F1.Flow`다. `Application`이라는 Namespace는 `UnityEngine.Application`을 가린다.

## Verification

- `Tools/chain.sh`: setup OK, EditMode 62/62, PlayMode 11/11.
- Entry가 아직 없어 실제 Asset을 Load하는 Test는 5단계의 첫 Data Entry와 함께 넣는다.

## 알아둘 것

- `LocalePolicy`는 시뮬 실행기도 컴파일하므로 `Data` 폴더(순수 C#)에 있다.
- Save를 `.bak`으로 복구할 때 Primary만 다시 쓴다. 손상된 Primary가 정상 `.bak`을 덮지 않게 하기 위해서다.
