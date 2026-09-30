# Stage 07d — UI

Snapshot: 2026-09-30 (완료)

## Goal

버튼만 눌러서 핵심 루프를 한 바퀴 돌 수 있다. 화면만 보고 용병이 왜 죽었는지 알 수 있다. 문구가 두 Locale로 나온다.

## Done

- Runtime (`Assets/@Scripts/UI`): `UIManager`, `UIScreen`, `ScreenId`와 `ScreenCatalog`, 화면 여섯(`Screens/`), View(`Views/`),
  문구 도구(`UiText`, `BattleLogText`), `UiPalette`. `Managers.UI`, Boot 단계 BOOT-12(타이틀 표시).
- Builder (`Assets/@Scripts/Editor/Setup/Ui`): `UiBuild`, `UiPrefabSetup.*`. 산출물은 `Assets/@Prefabs/UI/*.prefab`과 `UiPrefabStamp.txt`.
- 문구: `UI_StaticText.csv` 128 Key와 `UiKeys`. Font Atlas 322자.
- Domain: `BattleEngine.NextStormDamage`, `BattleLog.PartyDeaths`(사망 원인). Data: `JobData.PassiveText`.
- `ResourceManager`: Scene Unload로 이미 놓인 Instance Handle을 다시 놓지 않는다.
- `UiBuild.Canvas`의 Scaler를 Expand로 바꾸고 Boot, Main Scene을 다시 만들었다.
- Test: EditMode 378, PlayMode 31(+ 스크린샷 2개는 `[Explicit]`). `Tools/screenshots.sh`.
- Owner 문서: `Docs/Architecture/12_UI.md` (CLAUDE.md §2, §8에 등록).

## 결정 (권장안, 일괄 승인 범위) — 사후 검토 대상

- 화면 구성 전부(배치, 색, 크기). 목업으로 고르는 절차를 건너뛰었다.
- 전투: 시작하자마자 x1로 흐른다. 배속은 x1, x2, x4와 정지. 정지 중에도 포션과 후퇴를 쓸 수 있다.
  고른 배속은 앱을 끌 때까지 다음 전투에도 이어진다.
- 포션: 포션을 누르고 아군을 누른다. 후퇴: 버튼 하나, 성공 확률을 버튼에 적는다.
- 노드 맵: 노드를 누르면 그 노드의 적(이름, HP)이 보이고, "전투 시작"으로 들어간다. 갈 곳이 하나뿐이면 미리 골라 둔다.
- 보상: 아이템은 고른 뒤 넣을 칸을 누른다. 포션은 한 번 눌러 받는다.
- 전투 결과 Panel이 사망마다 한 줄로 이유를 적는다. 폭풍 피해는 유닛별 줄 대신 "폭풍이 모두에게 피해 n" 한 줄만 로그에 남긴다.
- 로비: 던전은 데이터의 첫 던전 하나를 보인다(Slice A에는 하나뿐이다). 타이틀로 나가는 버튼이 있다.
- 타이틀에 종료 버튼이 있다. 새 런이 진행 중인 런을 덮어쓸 때는 먼저 묻는다.
- 직업 패시브 설명문을 데이터(`JobData.csv`)에 넣었다. 조사가 숫자에 붙지 않게 문장을 썼다.

## Verification

- `Tools/chain.sh`: setup OK, sim OK, EditMode 378/378, PlayMode 31/31(스크린샷 2개 제외).
- setup을 연달아 돌려도 파일이 바뀌지 않는다(Prefab 포함). Prefab을 강제로 다시 만들어도 byte가 같다.
- `Tools/screenshots.sh`: 두 Locale 26장. 글자 넘침과 가림을 눈으로 확인하고 고쳤다.
- 하지 못한 것: 사람이 마우스로 직접 플레이하는 확인. Test의 클릭은 Pointer Raycast로 버튼이 가려지지 않았는지까지만 본다.

## 알아둘 것

- 화면을 고치려면 Builder를 고치고 `Tools/chain.sh setup`. Prefab을 Unity에서 직접 고치면 다음 setup이 덮어쓴다.
- 한 Prefab 안에서 Object 이름이 겹치면 Builder가 실패한다(다시 만들 때 파일이 바뀌는 것을 막기 위해서다).
- 새 문구: CSV에 행 추가 -> `UiKeys`에 상수 추가 -> setup(Table, Font Atlas). 자리(`{0}`)가 있는 Key는 고정 라벨로 쓸 수 없다.
- 보상 아이템의 수치가 기본 무기보다 많이 약하다(등급 8, 계수가 낮다). 플레이테스트에서 볼 지점이다.
