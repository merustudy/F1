# Stage 03 — 기반

Snapshot: 2026-09-30 (완료)

## Goal

Boot에서 Main으로 진입하는 뼈대, `Managers` 컨테이너, asmdef, 첫 Test, 검증 체인이 있다.

## Done

- Owner 문서: `Docs/Architecture/01_PROJECT_STRUCTURE.md`, `02_BOOTSTRAP_MANAGERS.md`, `03_ASSEMBLY_PACKAGES.md`, `10_TESTING_VALIDATION.md`.
- Runtime(`Assets/@Scripts/Core`): `AppRoot`, `BootstrapView`, `MainSceneRoot`, `BootStep`, `InitializationState`, `Managers`, `SceneManagerEx`.
- Editor(`Assets/@Scripts/Editor/Setup`): `ProjectSetup`(체인의 setup 진입점, Player 설정), `SceneSetup`(Boot/Main을 코드로 생성), `UiBuild`.
- Scene: `Assets/@Scenes/Boot.unity`, `Main.unity`. Build Settings 0, 1번. Template의 `SampleScene` 삭제.
- Test: EditMode `ManagersTests`(4), PlayMode `BootFlowTests`(3).
- `Tools/chain.sh`: setup, editmode, playmode. 결과는 XML로 판정.
- Package: Template의 쓰지 않는 11개 제거(목록은 Architecture/03).
- TMP Essential Resources를 `Assets/TextMesh Pro`에 반입(`com.unity.ugui` 안의 unitypackage를 풀어서).
- Git LFS: `git-lfs` 설치, `.gitattributes`에 이미지·오디오·Font 패턴.
- Player 설정: `companyName` = `funitup`, 창 크기 조절 가능.

## 결정 (권장안, 일괄 승인 범위)

- G6: Application 계층 Manager는 `RunManager`, `ExpeditionManager`.
- G13: 기준 해상도 1920x1080, 기본 전체 화면 창, 창 크기 조절 가능.
- Domain의 Unity 비의존은 시뮬 실행기의 직접 컴파일로 보장한다(asmdef는 나누지 않는다).
- Namespace는 폴더를 그대로 따르지 않는다. `Core` 아래는 전부 `F1.Core`.
- Editor에서 Main을 직접 Play하면 `MainSceneRoot`가 Boot로 되돌린다.

## Verification

- `Tools/chain.sh`: setup OK, EditMode 4/4, PlayMode 3/3.
- `git diff --check` 깨끗함.

## 알아둘 것

- Unity Batch 한 번이 20~30초다. Unity Editor가 이 프로젝트를 열고 있으면 체인이 시작하지 않는다.
- Scene을 바꾸려면 `SceneSetup`을 고치고 메뉴 `F1/Setup/Rebuild Boot And Main Scenes`를 돌린다. 체인의 setup은 Scene이 없을 때만 만든다.
- `Assets/TextMesh Pro`에는 HDRP용 Shader Graph도 들어 있다(Vendor 구성 그대로).
