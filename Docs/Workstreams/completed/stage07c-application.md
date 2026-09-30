# Stage 07c — Application 계층

Snapshot: 2026-09-30 (완료)

## Goal

화면 없이 Test만으로 핵심 루프(로비 -> 원정 -> 귀환 정산 -> 로비 -> 다시 출발)를 돌릴 수 있다.

## Done

- `Assets/@Scripts/Flow` (Namespace `F1.Flow`)
  - `RunManager`: `StartNewRun`, `SetParty`, `PartyProblem`, `Rest`, `CanDepart`, `IsAway`, 이벤트 `RunStarted`.
    `BeginExpedition`과 `Settle`은 `internal`이고 `ExpeditionManager`만 부른다.
  - `ExpeditionManager`: `Depart`, `AvailableNodes`, `EnterNode`, `AdvanceBattle`, `TryUsePotion`, `TryRetreat`, `CloseBattle`,
    `TakeItemReward`, `TakePotionReward`, `SkipReward`, `SwapItems`, `CanSetRow`, `SetRow`, `AcknowledgeReport`, `Phase`.
  - `BattleSession`(엔진과 노드), `BattleClock`(Frame 시간 -> 전투 밀리초), `GamePhase`.
- `Managers`와 `ManagerSet`에 `Run`, `Expedition` 추가. `AppRoot`가 만들고, 새 런 시드는 OS 난수에서 온다.
- `DataManager`에 이미 Load된 데이터를 받는 `internal` 생성자(Addressables 없이 Test하기 위한 것).
- Owner 문서: `Docs/Architecture/11_APPLICATION_FLOW.md` (CLAUDE.md §2, §8에 등록).

## 결정 (권장안, 일괄 승인 범위)

- 전투 종료와 원정 종료는 일어난 호출 안에서 바로 상태에 반영한다. 귀환 정산도 원정이 끝나는 순간 적용한다.
  화면은 그 뒤에 결과(`BattleSession`, `SettlementReport`)를 보여 주기만 한다.
- 정산 보고는 저장하지 않는다. 확인 전에 앱을 끄면 다음에는 로비에서 시작한다.
- 타이틀은 게임 단계가 아니다. 런이 없을 때 화면이 보이는 것이다. 런 종료(용병단 전멸)는 로비 안의 상태다.
- 전투 시간은 화면이 준다. 일시정지 중에도 포션과 후퇴를 입력할 수 있다.
- Typed Event는 `RunStarted` 하나다. 화면은 명령을 내고 돌아온 뒤 상태를 읽어 다시 그린다.

## Verification

- `Tools/chain.sh`: setup OK, sim OK, EditMode 367/367, PlayMode 21/21.

## 알아둘 것

- 8단계(저장)에서 명령마다 "확정 -> 저장"이 들어간다. 저장의 주인은 `RunManager` 하나로 하고, 원정과 전투의 기록은 `ExpeditionManager`가
  넘겨주는 방식을 권한다(파일 하나를 원자적으로 쓰기 위해). 그러면 CLAUDE.md §4의 `ExpeditionManager -> SaveManager`는 필요 없어진다.
- Test 도구: `Assets/@Tests/EditMode/Flow/FlowTestKit.cs`. `StrongParty`(항상 이기는 데이터)와 `DeadlyEnemies`(항상 지는 데이터).
