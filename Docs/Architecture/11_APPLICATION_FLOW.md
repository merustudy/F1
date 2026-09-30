# 11. Application Flow

Application 계층(`RunManager`, `ExpeditionManager`)이 하는 일, 명령, 상태를 확정하는 순서, 게임 단계를 소유한다.
규칙의 계산은 Domain(`08_GAMEPLAY_DOMAIN.md`), 저장 형식과 I/O는 Save(`07_SAVE.md`), 화면은 UI가 소유한다.

## 위치와 역할

```text
Assets/@Scripts/Flow            Namespace F1.Flow
├─ RunManager          런 상태의 주인. 새 런, 로비 명령, 귀환 정산 적용
├─ ExpeditionManager   원정, 전투 세션, 정산 보고의 주인
├─ BattleSession       진행 중이거나 막 끝난 전투 하나 (BattleEngine과 그 노드)
├─ BattleClock         화면의 Frame 시간을 전투 밀리초로 바꾼다 (배속, 일시정지)
└─ GamePhase           지금 어느 단계인가
```

Application 계층이 하는 일은 넷이다.

1. 명령이 지금 단계에서 허용되는지 확인한다.
2. Domain의 규칙 함수를 부른다. 규칙을 스스로 계산하지 않는다.
3. 바뀐 상태를 확정한다(저장은 `07_SAVE.md`).
4. 결과를 돌려준다.

- 상태 객체(`RunState`, `ExpeditionState`, `BattleEngine`)를 바꾸는 것은 Domain 규칙 코드뿐이다. Application 계층은 규칙 함수를 부르고,
  UI는 상태를 읽기만 한다.
- UI는 `StaticData`와 규칙 함수를 직접 부르지 않아도 되게, 필요한 질의(`CanDepart`, `AvailableNodes`, `CanSetRow` 등)를 Manager가 내놓는다.
- Application 계층은 `UnityEngine`의 시간, Scene, GameObject를 모른다. EditMode Test로 루프 전체를 돌린다.

## 주인

| 상태 | 주인 | 수명 |
|---|---|---|
| `RunState` | `RunManager` | 새 런부터 다음 새 런까지 |
| `ExpeditionState` | `ExpeditionManager` | 출발부터 원정이 끝날 때까지 |
| `BattleSession` | `ExpeditionManager` | 노드에 들어갈 때부터 화면이 전투를 닫을 때까지 |
| `SettlementReport` | `ExpeditionManager` | 원정이 끝날 때부터 화면이 확인할 때까지 |

`ExpeditionManager`는 `RunManager`에 의존한다. 반대 방향은 없다. `RunManager`는 원정이 진행 중인지만 안다(`IsAway`).

## 게임 단계

`ExpeditionManager.Phase`가 상태에서 계산한다. 따로 저장하는 값이 아니다.

| `GamePhase` | 조건 | 할 수 있는 명령 |
|---|---|---|
| `Lobby` | 원정도, 전투도, 확인할 보고도 없다 | 파티 정하기, 쉬기, 출발 |
| `NodeMap` | 원정 중이고 노드를 고를 차례 | 노드 들어가기, 아이템 옮기기, 열 바꾸기 |
| `Battle` | 전투 세션이 있다(끝났어도 닫기 전까지) | 전투 진행, 포션, 후퇴, 닫기 |
| `Reward` | 원정 중이고 보상을 고를 차례 | 보상 받기, 넘기기, 아이템 옮기기, 열 바꾸기 |
| `Settlement` | 확인하지 않은 정산 보고가 있다 | 확인 |

- 런이 없는 상태(`RunManager.HasRun`이 거짓)는 단계가 아니다. 화면이 타이틀을 보인다.
- 런이 끝났는지(`RunState.IsOver`)는 `Lobby` 안의 상태다. 로비가 종료 안내와 새 런을 보인다.
- 단계에 맞지 않는 명령은 예외다. 화면은 단계를 보고 명령을 낸다.

## 전투 진행

```text
화면의 Frame -> BattleClock.Step(deltaSeconds) -> ExpeditionManager.AdvanceBattle(ms) -> BattleEngine.AdvanceTo
```

- 전투 시간을 얼마나 나아가게 할지는 화면이 정한다(배속, 일시정지). 결과는 Setup과 입력 기록만의 함수이므로
  Frame 간격과 배속은 결과를 바꾸지 않는다.
- 입력(`TryUsePotion`, `TryRetreat`)은 현재 전투 시각에 적용된다. 일시정지 중에도 입력할 수 있다.
- 전투가 끝나면 **그 호출 안에서** `ExpeditionRules.CompleteBattle`을 적용한다. 연출이나 화면의 확인을 기다리지 않는다.
- `BattleSession`은 화면이 `CloseBattle`을 부를 때까지 남는다. 화면이 결과와 로그를 보여 주기 위한 것이고, 그 사이 상태는 이미 확정돼 있다.

## 귀환 정산

- 원정이 끝나는 순간(클리어, 전멸, 후퇴) 같은 호출 안에서 `RunRules.Settle`을 적용하고 `ExpeditionState`를 버린다.
- `SettlementReport`는 화면에 보이기 위해 남긴다. 확인(`AcknowledgeReport`)하면 버린다. 저장하지 않는다.

## 새 런

- 시드는 `AppRoot`가 넣어 준 시드 공급자에서 온다(OS 난수). Test는 고정값을 넣는다. Domain과 Application은 시드를 만들지 않는다.
- 새 런은 진행 중인 원정, 전투, 보고를 모두 버린다. `RunManager.RunStarted`를 `ExpeditionManager`가 듣는다.

## 알림

- 명령은 직접 호출이고, 부른 화면이 돌아온 뒤 상태를 읽어 다시 그린다.
- Typed Event는 `RunStarted` 하나다. 같은 사실을 여러 곳이 들어야 할 때만 늘린다.

## Validation

- 새 명령이 단계를 확인하는가? 규칙을 Domain이 계산하는가?
- 전투 종료와 귀환 정산이 화면의 확인 없이 적용되는가?
- 루프 한 바퀴 Test(`GameLoopTests`)가 통과하는가?
- `Flow` 폴더에 `UnityEngine.Time`, Scene, GameObject 참조가 없는가?

## Deferred and Forbidden

- Deferred: 원정 도중 타이틀로 나가기, 전투 되감기와 관전, 여러 원정 동시 진행.
- Forbidden: Application 계층에서 규칙 계산, 연출 완료를 상태 확정의 조건으로 쓰기, UI가 상태 객체를 직접 바꾸기,
  `RunManager`가 `ExpeditionManager`를 참조하기.
