# 10. Testing and Validation

Test, 검증 체인, 검증 보고, Definition of Done을 소유한다.

## Test 피라미드

```text
많음  순수 C# Domain/Data Test        (EditMode Runner)
중간  Editor/Storage/Data 통합         (EditMode)
적음  Scene/MonoBehaviour/Addressables/Localization Runtime   (PlayMode)
별도  Presentation 수동 점검           (레이아웃, 타이밍, 손맛 — 자동화하지 않는다)
```

- Scene, Frame, GameObject가 필요 없으면 PlayMode를 쓰지 않는다. Test하려고 순수 로직을 MonoBehaviour로 만들지 않는다.
- 이름: `<Subject>Tests` / `<Behavior>_When<Condition>_<Expected>`.
- PlayMode Test는 실제 Save 폴더를 쓰지 않는다. Test 전용 Save Root를 쓴다.
- PlayMode Test는 끝날 때 `AppRoot`를 파괴하고 `Managers`를 비운다.
- PlayMode Test의 새 런은 고정 시드(`BootTestUtil.RunSeed`, `AppRoot.NewRunSeedOverride`)로 시작한다. 지도·상점·전리품·전투가 매번 같아서 Test가 운에 기대지 않는다(실제 게임의 새 런은 OS 난수).
  그 시드의 지도에 기대는 전제(상점이 있다, 첫 전투가 떨어뜨린다)는 `Assume`이 아니라 `Assert`로 쓰고, 이겨야 하는 Test는 승리를 꾸민다(`UiTestUtil.StageChampion`).
  데이터를 바꾼 뒤 그 전제가 깨지면 시드를 바꾸지 말고 먼저 이유를 본다. 시드를 바꿀 때는 새 시드의 지도가 전제를 만족하는지 확인한다.
- 시뮬의 통계(승률 등)는 Test의 통과 조건이 아니다. 결정론 Test(같은 시드와 입력 -> 같은 결과)와 구분한다.

## 검증 체인

`Tools/chain.sh`가 Unity를 Batch로 돌린다. Editor를 열지 않고 Setup, 변환, Test를 끝낸다.

```bash
Tools/chain.sh            # 전체
Tools/chain.sh editmode   # 단계 이름을 주면 그 단계만
```

| 단계 | 하는 일 | 성공 판정 |
|---|---|---|
| `setup` | `F1.Editor.Setup.ProjectSetup.ApplyMenu` (Player 설정, Scene, Data 변환, UI String Table, Font Atlas, 화면 Prefab, Addressables 동기화) | Log의 `F1_PROJECT_SETUP_DONE` |
| `sim` | `Tools/Sim` Build(순수 C# 폴더가 Unity 없이 컴파일되는지)와 `validate`(Generated가 Source와 같고 Load되는지) | 둘 다 성공 |
| `editmode` | EditMode Test | 결과 XML |
| `playmode` | PlayMode Test | 결과 XML |

`setup` 안의 세부 동기화는 그 영역이 구현되는 Roadmap 단계에서 추가된다.

규칙:

- Test 결과는 exit code가 아니라 **결과 XML**로 판정한다. XML이 없으면 Compile Error로 보고 Log의 `error CS` 줄을 보인다.
- 결론을 내지 못한 Test(`Assume`이 맞지 않은 것. 예: 피로 상한이 200이 아닌 데이터)는 실패가 아니지만, 보고는 그 수(`inconclusive=N`)와 이름을 따로 보인다. 그 라운드가 고친 Test가 Inconclusive면 다시 돌린다.
- Log와 결과 XML은 저장소 밖(`~/Library/Caches/F1/chain/<시각>/`)에 둔다.
- 이 프로젝트를 연 Unity Editor가 있으면 체인은 시작하지 않는다.
- Setup 메서드는 멱등하다. 끝에 고정 Log Token을 찍는다.
- Player Build는 validate-only다. Build 중에 Source, Generated, Addressables Entry, Table을 바꾸지 않는다.
- 체인이 끝나면 `Assets/InitTestScene*`가 남지 않았는지 확인한다.
- 화면을 눈으로 볼 때는 `Tools/screenshots.sh`를 쓴다. 모든 화면을 두 Locale로 PNG로 뽑아 저장소 밖에 둔다.
  그래픽 장치가 필요해서 체인에는 넣지 않는다(`[Explicit]` Test). 끝나면 Render 설정 Asset이 바뀌지 않았는지 스크립트가 확인한다.

## 작업 후 확인 (Definition of Done)

1. 관련 Test가 있고 체인이 통과한다.
2. `git diff --check`가 깨끗하다.
3. `git status`에 의도하지 않은 변경(특히 ProjectSettings, Scene, `.meta`)이 없다.
4. 관련 Owner 문서, Handoff, Roadmap 상태를 고쳤다.

검증 보고에는 돌린 단계, Test 수(통과/실패), 돌리지 못한 것과 이유를 적는다.

## Validation

- 새 규칙이나 버그 수정에 Test가 있는가?
- PlayMode Test가 실제 Save 폴더를 건드리지 않는가?
- 체인 결과를 XML로 확인했는가?

## 플레이 기록 (16단계)

직접 플레이의 시간을 화면별로 나눠 보기 위한 진단 기록이다. 게임 규칙, 저장, Test의 판정에 쓰지 않는다.

- `PlayLog`(`@Scripts/Core/Bootstrap`)가 쓴다. `AppRoot`가 만들어 `UIManager.ScreenShown`에 묶는다: 화면이 열릴 때마다 한 줄
  (시각, 화면, 상태 `phase= day= floor=<선 층>/<층 수> node= speed=`)이고, 부팅 때 `Boot` 한 줄(Locale, 저장된 런의 상태)이다. 열 사이는 탭이다.
- 파일: `<persistentDataPath>/Logs/play-<시작 시각>.log`. 앱을 켤 때마다 하나이고 첫 줄을 쓸 때 만든다.
  macOS에서는 `~/Library/Application Support/funitup/F1/Logs/`. 저장 폴더(`Saves/`)와 Git 밖이다. PlayMode Test는 임시 저장 루트 안의 `Logs/`에 써서 루트와 함께 지워진다.
- 쓰기에 실패하면 조용히 멈춘다(게임에 영향 없음). Test: `PlayLogTests`(EditMode).
- **사건 줄**(Round 97): 이름이 `!`(`PlayLog.EventPrefix`)로 시작하는 줄은 화면이 아니라 사건이다. 지금은 금화 테스트 버튼의 `!TestGold`(`+100 gold=<뒤의 금>` 다음에 화면 줄과 같은 상태)뿐이다.
  `RunManager.TestGoldGranted`(저장이 성공한 뒤)에 `AppRoot`가 묶는다(에디터·개발 빌드만). 집계는 사건 줄을 화면의 시간에서 빼고 "events: TestGold xN"으로 따로 센다.
- 집계: `Tools/playlog.py [파일]`(없으면 가장 새 파일) — 화면별 분·횟수·비율, 처음과 끝, 시간순 줄. 야영지 창과 인벤토리 팝업은 화면이 아니라 노드 맵의 시간에 든다.

## Deferred and Forbidden

- Deferred: CI, 성능 Test, Player Build 자동 검증.
- Forbidden: exit code만으로 통과 판정, Test를 위한 MonoBehaviour화, 실패를 Warning으로 넘기기.
