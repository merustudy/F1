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

## Deferred and Forbidden

- Deferred: CI, 성능 Test, Player Build 자동 검증.
- Forbidden: exit code만으로 통과 판정, Test를 위한 MonoBehaviour화, 실패를 Warning으로 넘기기.
