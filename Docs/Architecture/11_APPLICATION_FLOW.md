# 11. Application Flow

Application 계층(`RunManager`, `ExpeditionManager`)이 하는 일, 명령, 상태를 확정하는 순서, 게임 단계를 소유한다.
규칙의 계산은 Domain(`08_GAMEPLAY_DOMAIN.md`), 저장 형식과 I/O는 Save(`07_SAVE.md`), 화면은 UI가 소유한다.

## 위치와 역할

```text
Assets/@Scripts/Flow            Namespace F1.Flow
├─ RunManager          런 상태와 run.json의 주인. 새 런, Load, 로비 명령, 귀환 정산 적용, 저장
├─ ExpeditionManager   원정, 전투 세션, 정산 보고의 주인
├─ RunSaveMapper       Save DTO와 상태 사이의 변환과 검증. RunSaveMigrator는 Schema Version
├─ BattleSession       진행 중이거나 막 끝난 전투 하나 (BattleEngine과 그 노드)
├─ BattleClock         화면의 Frame 시간을 전투 밀리초로 바꾼다 (배속, 일시정지, 결정타 동안의 느림)
└─ GamePhase           지금 어느 단계인가
```

Application 계층이 하는 일은 넷이다.

1. 명령이 지금 단계에서 허용되는지 확인한다.
2. Domain의 규칙 함수를 부른다. 규칙을 스스로 계산하지 않는다.
3. 바뀐 상태를 확정한다(저장은 `07_SAVE.md`).
4. 결과를 돌려준다.

- 상태 객체(`RunState`, `ExpeditionState`, `BattleEngine`)를 바꾸는 것은 Domain 규칙 코드뿐이다. Application 계층은 규칙 함수를 부르고,
  UI는 상태를 읽기만 한다.
- UI는 `StaticData`와 규칙 함수를 직접 부르지 않아도 되게, 필요한 질의(`CanDepart`, `CanPlaceInParty`, `AvailableNodes`, `CanMoveToRow`,
  `CanTakeLoot`, `CanTakeLootToInventory`, `CanPickItem`, `CanMoveItem`, `CanMoveToInventory`, `CanPlaceFromInventory`, `CanPickBag`, `CanMoveBag`, `MergesAt` 등)를 Manager가 내놓는다.
- 보드의 명령과 질의(19단계, 격자)는 아이템을 그 아이템이 덮은 칸(구성원, X, Y)으로 가리키고 놓을 자리를 `Placement`(왼쪽 위 칸과 돌린 횟수)로 받는다:
  `MoveItem(구성원, X, Y, 구성원, Placement)`, `MoveToInventory(구성원, X, Y)`, `PlaceFromInventory(번호, 구성원, Placement)`, `TakeLoot(자리, 구성원, Placement)`, `BuyToBoard(자리, 구성원, Placement)`, `MoveBag(구성원, X, Y, 구성원, Placement)`,
  `UpgradeAtCamp(구성원, X, Y)`. 합침의 질의는 왼쪽 위 칸(`MergesAt`·`LootMergesAt`·`ShopMergesAt`(…, 구성원, X, Y)). 돌리기와 그림자는 화면의 일이고 Manager는 놓인 자리만 받는다.
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
| `Lobby` | 원정도, 전투도, 확인할 보고도 없다 | 파티에 넣기와 빼기, 자리 바꾸기, 쉬기, 출발(피로도는 막지 않는다) |
| `NodeMap` | 원정 중이고 노드를 고를 차례 | 노드 들어가기(전투 노드는 `Battle`로, 야영지 노드는 `Camp`로), 아이템과 가방 옮기기(보드 사이, 보드와 인벤토리 사이: `MoveToInventory`·`MoveToInventoryAt`·`PlaceFromInventory`, 인벤토리 격자 안: `MoveInInventory`. Round 49), 자리 바꾸기 |
| `Camp` | 원정 중이고 야영지에 있다(`ExpeditionPhase.AtCamp`) | 쉬기(`RestAtCamp`)나 정비(`UpgradeAtCamp`. 둘 다 그 뒤 `NodeMap`), 아이템과 가방 옮기기, 자리 바꾸기 |
| `Shop` | 원정 중이고 상점에 있다(`ExpeditionPhase.AtShop`, 17단계) | 사기(`BuyToBoard`·`BuyToInventory`·`BuyToInventoryAt`(인벤토리의 한 자리, Round 55)·`BuyPotion`), 새로고침(`RefreshShop`), 나가기(`LeaveShop`. 그 뒤 `NodeMap`), 아이템 옮기기, 자리 바꾸기. 질의는 `ShopStock`·`PriceOf`·`RefreshCost`·`CanAfford`·`CanBuy...`·`ShopMergesAt`·`CanRefreshShop` |
| `Battle` | 전투 세션이 있다(끝났어도 닫기 전까지) | 전투 진행, 포션, 후퇴, 닫기. **이긴 뒤**(`LootOpen`: 끝난 전투가 열려 있고 원정이 `PickingLoot`. Round 47)에는 `Loot`의 명령도 모두 된다(전리품은 전투 화면에서 줍는다). 전리품을 다 주웠거나 없어서 원정이 `ChoosingNode`인데 이긴 전투가 아직 화면에 있으면(`WonBattleOnShow`, Round 52) 보드·인벤토리의 명령이 [계속](닫기)까지 된다 |
| `Loot` | 원정 중이고 이긴 전투의 전리품이 놓여 있는데 전투 세션은 없다(`ExpeditionPhase.PickingLoot`. 앱을 닫았다 연 경우뿐이다) | 줍기(`TakeLoot`·`TakeLootToInventory`·`TakeLootToInventoryAt`(인벤토리의 한 자리, Round 55). 하나마다, 마지막 것을 주우면 `NodeMap`), 두고 가기(`LeaveLoot`. 그 뒤 `NodeMap`), 아이템 옮기기, 자리 바꾸기. 질의는 `CanTakeLoot`·`CanTakeLootToInventory`·`CanTakeLootToInventoryAt`·`LootMergesAt`, 놓인 것은 `BattleLoot`. 화면은 전투 화면의 "이긴 뒤" 모습(`12_UI.md`) |
| `Settlement` | 확인하지 않은 정산 보고가 있다 | 확인 |

- 런이 없는 상태(`RunManager.HasRun`이 거짓)는 단계가 아니다. 화면이 타이틀을 보인다.
- 런이 끝났는지(`RunState.IsOver`)는 `Lobby` 안의 상태다. 로비가 종료 안내와 새 런을 보인다.
- 단계에 맞지 않는 명령은 예외다. 화면은 단계를 보고 명령을 낸다.
- 로비의 파티는 한 명씩 바꾼다: `RunManager.PlaceInParty`(파티의 맨 뒤 다음 열에 넣거나, 파티 안에서 자리를 맞바꾸거나)와 `RemoveFromParty`.
  원정 중의 자리 바꾸기는 `ExpeditionManager.MoveToRow`다. 어느 열이 가능한지는 Domain의 자리 규칙이 정한다(`08_GAMEPLAY_DOMAIN.md` "자리 (열)").

## 전투 진행

- 노드에 들어가는 명령(`EnterNode`)은 그 호출 안에서 살아 있는 구성원의 피로를 올리고(전투에 들어가는 비용, `08_GAMEPLAY_DOMAIN.md` "피로")
  전투를 세운 뒤 한 번에 저장한다. 이어하기는 저장된 피로로 같은 전투를 다시 만들 뿐 다시 더하지 않는다.
- 야영지 노드면 `EnterNode`는 전투를 세우지 않고 야영지에 들어가(`ExpeditionRules.EnterCamp`) 저장한다. 피로는 오르지 않는다.
  `RestAtCamp`가 쉬기를, `UpgradeAtCamp(구성원, X, Y)`가 정비를 적용하고 저장한다(`08_GAMEPLAY_DOMAIN.md` "긴 원정", "단계와 합치기"). 야영지의 화면은 노드 맵이다(`12_UI.md` "노드 맵의 오른쪽").
- 상점 노드면 `EnterNode`는 상점에 들어가 물건을 뽑고(`ExpeditionRules.EnterShop`) 저장한다. 상점의 명령마다 끝에서 저장하고, `LeaveShop`이 `NodeMap`으로 돌린다. 상점의 화면도 노드 맵이다(`12_UI.md` "상점").
  이긴 전투가 가져온 코인은 `CompleteBattle` 안에서 원정에 더해지고, 결과 창을 위해 `BattleCoins`가 그 전투의 것을 말한다(보스와 진 전투는 0).
  이긴 전투의 전리품도 `CompleteBattle` 안에서 뽑혀 놓이고(18단계), `BattleLoot`가 아직 놓인 것을 말한다(보스와 진 전투는 없음). 전리품은 **그 전투 화면에서** 줍는다(Round 47):
  `LootOpen`인 동안 전리품의 명령이 되고, 화면의 [계속]은 남은 것을 두고(`LeaveLoot`) 전투를 닫는다(`CloseBattle`). 마지막 것을 주우면 원정은 노드 고르기로 가지만 화면은 [계속]까지 머문다.

```text
화면의 Frame -> BattleClock.Step(deltaSeconds) -> ExpeditionManager.AdvanceBattle(ms) -> BattleEngine.AdvanceTo
```

- 전투 시간을 얼마나 나아가게 할지는 화면이 정한다(배속, 일시정지, 결정타 동안 잠깐 느리게: `BattleClock.SlowPercent`. `12_UI.md` "연출"). 결과는 Setup과 입력 기록만의 함수이므로
  Frame 간격과 배속은 결과를 바꾸지 않는다.
- 입력(`TryUsePotion`, `TryRetreat`)은 현재 전투 시각에 적용된다. 일시정지 중에도 입력할 수 있다.
- 전투가 끝나면 **그 호출 안에서** `ExpeditionRules.CompleteBattle`을 적용한다. 연출이나 화면의 확인을 기다리지 않는다.
- `BattleSession`은 화면이 `CloseBattle`을 부를 때까지 남는다. 화면이 결과와 로그를 보여 주기 위한 것이고, 그 사이 상태는 이미 확정돼 있다.

## 귀환 정산

- 원정이 끝나는 순간(클리어, 전멸, 후퇴) 같은 호출 안에서 `RunRules.Settle`을 적용하고 `ExpeditionState`를 버린다.
- `SettlementReport`는 화면에 보이기 위해 남긴다. 확인(`AcknowledgeReport`)하면 버린다. 저장하지 않는다.
  살아 돌아온 용병마다 원정에서 쌓인 피로도를 담는다(`SurvivorFatigue`); 그 값이 로스터에 남는다.

## 확정과 저장

```text
명령 검증(단계, 막힘 여부) -> Domain 규칙으로 상태 변경 -> Snapshot -> run.json 저장 -> 돌아감 -> 화면이 다시 그림
```

- 상태를 바꾸는 명령은 모두 끝에서 저장한다. `ExpeditionManager`는 자기 부분을 DTO로 만들어 `RunManager.Save`에 넘긴다.
  `RunManager`만 파일을 쓴다. 저장 시점의 목록과 파일 내용은 `07_SAVE.md`가 소유한다.
- 저장에 실패해도 명령은 예외 없이 돌아온다(상태는 이미 바뀌었다). 대신 `RunManager.IsSaveBlocked`가 켜지고,
  그동안 상태를 바꾸는 명령은 전부 예외이며 전투 시간도 흐르지 않는다. `RetrySave`가 같은 Snapshot을 다시 쓴다.
- 전투 시간이 흐르는 것만으로는 저장하지 않는다. 입력이 받아들여졌을 때, 아군이 빈사가 되거나 죽었을 때, 전투가 끝났을 때 저장한다.

## 이어하기

- Boot에서 `RunManager.Load`가 `run.json`을 읽고 검증한다. 이어서 `ExpeditionManager.Restore`가 원정을 넘겨받고,
  전투 중이었다면 `BattleEngine.Replay`로 확정 시각의 전투를 다시 만든다.
- 그 뒤의 단계(`Phase`)는 평소와 같이 상태에서 계산된다. 이어하기를 위한 별도 상태가 없다.
- `LastResume`은 전투가 정확히 재현됐는지(`Exact`), 규칙이나 데이터가 바뀌어 달라졌는지(`Diverged`), 입력 기록이 성립하지 않아
  처음부터 다시 하는지(`Restarted`)를 말한다.

## 새 런

- 시드는 `AppRoot`가 넣어 준 시드 공급자에서 온다(OS 난수). Test는 고정값을 넣는다. Domain과 Application은 시드를 만들지 않는다.
- 새 런은 진행 중인 원정, 전투, 보고를 모두 버린다. `RunManager.RunStarted`를 `ExpeditionManager`가 듣는다.

## 알림

- 명령은 직접 호출이고, 부른 화면이 돌아온 뒤 상태를 읽어 다시 그린다.
- Typed Event는 둘이다. `RunStarted`(새 런이 진행 중이던 것을 대체했다)와 `SaveBlockedChanged`(저장이 막혔거나 풀렸다).
  같은 사실을 여러 곳이 들어야 할 때만 늘린다.

## Validation

- 새 명령이 단계를 확인하는가? 규칙을 Domain이 계산하는가?
- 상태를 바꾸는 새 명령이 막힘을 확인하고(`RequireWritable`) 끝에서 저장하는가(`Commit`)?
- 전투 종료와 귀환 정산이 화면의 확인 없이 적용되는가?
- 루프 한 바퀴 Test(`GameLoopTests`)가 통과하는가?
- `Flow` 폴더에 `UnityEngine.Time`, Scene, GameObject 참조가 없는가?

## Deferred and Forbidden

- Deferred: 원정 도중 타이틀로 나가기, 전투 되감기와 관전, 여러 원정 동시 진행.
- Forbidden: Application 계층에서 규칙 계산, 연출 완료를 상태 확정의 조건으로 쓰기, UI가 상태 객체를 직접 바꾸기,
  `RunManager`가 `ExpeditionManager`를 참조하기.
