# 08. Gameplay Domain

Domain 코드의 구조, 결정론을 지키는 방법, 상태 Type, 시뮬 실행기를 소유한다.
게임 규칙의 내용은 소유하지 않는다. 규칙은 `Docs/Design`(02 전투, 03 던전, 04 로비), 수치는 `Assets/@Data/Source/*.csv`다.

## 위치와 경계

```text
Assets/@Scripts/Gameplay        Namespace F1.Gameplay. 순수 C#. Unity, Managers, Save DTO를 참조하지 않는다
├─ Random       Pcg32, RngStream, SeedDeriver
├─ Battle       BattleEngine, BattleSetup, BattleUnit, BattleEvent, BattleInput, BattleLog, Formation
├─ Expedition   ExpeditionState, ExpeditionRules, ItemBoard, NodeMap, MapGenerator
└─ Run          RunState, RunRules, SettlementReport
```

- Domain은 `F1.Data`의 Definition(`StaticData`)만 입력으로 받는다. 파일, Addressables, 시각, 난수원을 스스로 구하지 않는다.
- `Tools/Sim`이 이 폴더를 Unity 없이 그대로 컴파일한다. Unity 참조가 생기면 체인의 sim 단계가 깨진다.
- C# 9 문법까지만 쓴다.

## 세 수명

| 수명 | 상태 | 규칙 | 끝나면 |
|---|---|---|---|
| 런 | `RunState`: 시드, 날짜, 로스터(피로도), 파티, 원정 횟수, 클리어 기록 | `RunRules` | 용병이 하나도 없으면 `IsOver` |
| 원정 | `ExpeditionState`: 던전, 시드, 맵, 구성원(HP, 보드), 인벤토리, 포션, 단계 | `ExpeditionRules` | `RunRules.Settle`로 런에 반영하고 버린다 |
| 전투 | `BattleEngine` 안의 유닛, 쿨다운, 포션, 이벤트 로그, 입력 기록 | `BattleEngine` | `ExpeditionRules.CompleteBattle`로 원정에 반영하고 버린다 |

- 상태 Type은 Plain 객체다. 규칙은 상태를 인자로 받는 static 함수(`RunRules`, `ExpeditionRules`)와 `BattleEngine`에만 있다.
- 상태를 바꾸는 것은 규칙 코드뿐이다. Application 계층과 UI는 규칙 함수를 부르고 상태를 읽기만 한다.
- 허용되지 않는 명령은 예외다. 부르는 쪽이 먼저 확인한다(`CanDepart`, `CanPlaceInParty`, `CanMoveToRow`, `CanUsePotion`, `CanRetreat`, `AvailableNodes`).
- 아이템(`EquippedItem`, 원정 수명)과 유물은 다른 Type이다. 유물은 Slice A에 없고, 유물을 가정하는 코드도 없다.

## 자리 (열)

규칙은 `Docs/Design/02_Combat_System.md` §2가 소유한다. 여기는 그 규칙을 코드 어디에 두는지만 정한다.

- 열은 정수다(1이 상대와 가장 가깝다). 한 쪽은 **한 열에 한 유닛씩 선 한 줄**이다. 열의 수(`BattleRows.Count`)와
  "한 열에 하나"는 상수가 아니라 **구조**다: 공격 범위를 "몇 번째까지"로 세는 것, 적 무리가 순서 있는 목록인 것,
  전투 화면이 열마다 카드 한 장을 놓는 것이 모두 그것을 전제한다.
- "앞 열이 비면 뒤 열이 전진한다"는 규칙은 `Formation` 한 곳에 있다: 줄이 맞는지(`Problem`), 빈 열 메우기(`CloseGaps`),
  새로 들어갈 수 있는 열(`CanJoin`), 자리 맞바꾸기(`CanMove`, `Move`). 서 있는 유닛의 열 목록만 받는 순수 함수다.
- 부르는 곳은 셋이다. 어느 상태도 빈 열을 사이에 둔 줄을 담지 않는다.

| 곳 | 하는 일 |
|---|---|
| `RunRules` | 로비 파티: 넣기와 자리 바꾸기(`PlaceInParty`), 빼기(`RemoveFromParty`), 파티 검증(`PartyProblem`), 귀환 정산에서 죽은 용병이 빠진 뒤 메우기 |
| `ExpeditionRules` | 출발할 때 검증, 전투 사이의 자리 바꾸기(`MoveToRow`), 전투가 끝나면 전투 유닛의 자리를 그대로 가져오기 |
| `BattleEngine` | Setup 검증, 전투 중의 전진(`AdvanceBehind`) |

- 전투 중의 전진은 죽음을 처리하는 코드(`Kill`) 안에서 일어난다. 죽은 유닛 뒤의 유닛을 한 칸씩 옮기고
  `RowsAdvanced`를 로그에 남긴다. 유닛의 `Index`(유닛 순서)는 바뀌지 않고 `Row`만 바뀐다.
- 줄에서 세는 자리는 `RowSpan`(`F1.Data`: 기준 `Front`/`Back`과 깊이) 하나다. 아이템을 쓸 수 있는 자리(`ItemData.Rows`)와 패시브의
  자리 조건(`PassiveSpec.Rows`)이 같은 Type이고, 판정은 `Contains(row, 살아 있는 줄의 길이)` 한 함수다. 공격 범위와 같은 어휘다.
- 아이템을 쓸 수 있는지(`BattleItemState.Active`)는 그 쪽에서 누가 죽을 때마다 **살아 있는 전원**을 다시 본다(`RefreshSide`):
  자리가 바뀐 유닛뿐 아니라 줄이 짧아져 자리가 달라진 유닛도 있기 때문이다. 쓸 수 있게 된 아이템은 그 시각부터 쿨다운을 새로 채운다.
  줄은 짧아지기만 하므로 전투 중에 꺼지는 일은 없다(코드는 일반적으로 쓰되 Test가 그 성질을 고정한다).
- 타깃은 효과 하나마다 적용 직전에 한 번 고른다(`ResolveTargets`). 그래서 효과 도중의 전진은 그 효과의 대상을 바꾸지 않는다.
- 공격 범위는 효과의 타깃(`EnemyFront`, `EnemyBack`)과 깊이(`Reach`)다. 살아 있는 유닛은 늘 1열부터 빈 열 없이 서 있으므로
  "앞에서 N번째까지"는 1~N열, "뒤에서 N번째까지"는 맨 뒤에서 N열이다. 그 안의 살아 있는 유닛만 고르니 빈 열을 치는 일이 없다.

## 아이템 보드와 인벤토리

규칙은 `Docs/Design/02_Combat_System.md` §4와 `03_Dungeon_Structure.md` §5가 소유한다.

- 보드는 아이템(`EquippedItem`)의 순서 있는 목록과 칸 수(`ExpeditionMember.ItemSlots`, 직업에서 온다)다. 아이템은 크기(`ItemData.Size`)만큼
  칸을 앞에서부터 빈 칸 없이 차지한다. 칸과 목록 사이의 계산(차지한 칸 수, 어느 칸에 어느 아이템이 있는지, 넣을 수 있는지, 넣기)은
  `ItemBoard` 한 곳에 있다. 목록과 칸 수만 받는 순수 함수다.
- 인벤토리는 `ExpeditionState.Inventory`(목록)다. 보드에서 밀려나거나 빼낸 아이템이 들어가고, 전투 Setup에는 들어가지 않는다.
  칸 수는 `BalanceData.InventoryCells`이고 아이템은 보드처럼 크기만큼 차지한다(`ItemBoard.UsedCells`). 남은 칸은 `ExpeditionRules.FreeInventoryCells`.
- 명령은 `ExpeditionRules`에 있다: 보상을 보드에 넣기(`TakeItemReward`)와 인벤토리로 받기(`TakeItemRewardToInventory`), 보드 사이 옮기기와
  바꾸기(`MoveItem`), 인벤토리로 빼기(`MoveToInventory`), 인벤토리에서 넣기(`PlaceFromInventory`). 각각 `Can...` 질의가 있고,
  화면은 칸마다 물어서 누를 수 있는지 정한다. 자리(칸)는 화면이 고르는 것이고 규칙은 Domain이 계산한다.
- 인벤토리로 가는 아이템(받는 보상, 빼는 아이템, 찬 자리에 넣어 밀려나는 아이템)은 남은 칸에 들어갈 때만 허용한다. 인벤토리에서 보드의 찬 자리로
  넣을 때는 나가는 아이템의 칸을 더해 센다. "칸에 아이템이 있어 집을 수 있는가"(`CanPickItem`)는 어디로 갈 수 있는가와 따로 묻는다:
  보드 사이의 옮기기는 인벤토리가 차도 된다.
- 전투의 `BattleUnitSetup.Items`는 보드 그대로의 목록(빈 칸 없음)이고 `ItemSlots`는 화면이 빈 칸을 그리기 위한 칸 수다
  (적은 가진 아이템의 크기 합). `BattleItemState.SlotIndex`는 보드의 순서이고 발동 순서다.

## 전투: 결정론

전투 결과는 `BattleSetup`(시드 포함)과 입력 기록(`BattleInput` 목록)만의 함수다.

- 시간은 정수 밀리초다. `BattleEngine`은 다음 사건의 시각으로 건너뛴다. `AdvanceTo(t)`를 잘게 여러 번 부르든 한 번에 부르든 결과가 같다.
- 같은 시각의 순서는 코드 한 곳(`ProcessAutomaticEvents`)에 고정돼 있다: 화상 틱 -> 아이템(아군, 유닛 순서, 칸 순서) -> 폭풍 틱.
  플레이어 입력은 `AdvanceTo`가 끝난 뒤의 현재 시각에 적용되므로, 그 시각의 자동 사건 뒤에 온다.
- 난수는 `Pcg32`뿐이다. 전투 스트림(사망 판정)과 입력 스트림(후퇴 판정)을 나눠, 후퇴 시도가 사망 판정을 바꾸지 않는다.
- 수치는 정수다. 효과 크기, 쿨다운 수정의 반올림은 한 함수에서만 계산한다.
- 입력은 받아들여졌을 때만 기록한다(`TryUsePotion`, `TryRetreat`). `BattleEngine.Replay(setup, inputs, untilMs)`가 같은 전투를 다시 만든다.
- 결과 계산에 `UnityEngine.Random`, `System.Random`, `DateTime`, `Time.deltaTime`, 연출 완료 시점을 쓰지 않는다.

## 이벤트 로그

- `BattleEngine.Events`는 일어난 일을 시각과 함께 순서대로 담는다(`BattleEvent`: Kind, Source, Target, Id, A, B, C).
- 표현 계층은 이 로그와 유닛 상태를 읽어 그린다. 표현은 결과를 바꾸지 못한다.
- "왜 죽었는지"는 로그로 설명한다: `DogEntered`, `GraceBroken`, `DeathRolled`(확률, 굴림, 결과), `Died`.
- 자리가 바뀐 것도 로그에 있다: `RowsAdvanced`(어느 쪽의 몇 열이 비었는지). 표현 계층은 유닛의 `Row`를 읽어 그 자리에 그린다.
- `BattleLog.Hash`는 로그의 요약값이다. 결정론 Test와 이어하기 검증이 비교한다.

## 시드

```text
런 시드 -> 원정 시드 = Derive(런 시드, "expedition", 원정 순번)
원정 시드 -> 맵 난수 = Derive(원정 시드, "map", 0)
          -> 보상 난수 = Derive(원정 시드, "reward", 노드 id)
          -> 전투 시드 = Derive(원정 시드, "battle", 노드 id)
```

런 시드는 Application 계층이 새 런을 만들 때 넣어 준다. Domain은 시드를 만들지 않는다.
`ExpeditionRules.BuildBattleSetup`은 상태만의 함수라서, 저장된 원정을 이어도 같은 전투가 다시 만들어진다.

## 데이터와 규칙의 경계

- 상수는 전부 `BalanceData`, 콘텐츠는 Definition에서 온다. Domain 코드에 밸런스 숫자가 없다.
  열의 수와 "한 열에 하나"는 상수가 아니라 구조다("자리 (열)").
- 패시브, 아이템 효과, 지역 속성은 데이터가 "언제, 조건, 무엇을, 누구에게, 얼마"를 적고, 코드는 그 어휘(Enum)만 구현한다.
  새 어휘가 필요하면 Enum, Definition 생성자의 검증, `BattleEngine`의 처리, Test를 함께 넣는다.
- Definition 생성자가 거르는 조합(`PassiveSpec`, `ItemEffect`)은 `BattleEngine`이 구현한 조합과 같아야 한다.

## 시뮬 실행기 (`Tools/Sim`)

```bash
dotnet run --project Tools/Sim -- battle     --group <id> --party a,b,c --policy none|balanced|safe --runs n --seed n
dotnet run --project Tools/Sim -- expedition --dungeon <id> --party a,b,c --policy ... --runs n --seed n
dotnet run --project Tools/Sim -- formations --dungeon <id> --party a,b,c --policy ... --runs n --seed n
dotnet run --project Tools/Sim -- trace      --group <id> --party a,b,c --policy ... --seed n
```

- 파티는 용병 id나 직업 id로 적는다. 모두에게 열 번호를 붙이면 그 열에 세운다(`knight:1,spellblade:2,bishop:3,archmage:4`).
  붙이지 않으면 직업의 권장 열 순서로 앞에서부터 세운다.
- `formations`는 같은 파티를 가능한 모든 순서로 세워 같은 시드로 돌린다. 자리가 결과를 얼마나 바꾸는지 본다.

- 게임과 같은 Generated JSON을 `StaticDataLoader`로 읽고, 같은 Domain 코드를 돌린다.
- 정책(`SimPolicy`)은 플레이어 입력을 대신한다. 게임 규칙이 아니라 시뮬 도구의 일부이고, 게임과 같은 API(`TryUsePotion`, `TryRetreat`,
  `ExpeditionRules`의 명령)만 쓴다.
- 밸런스 작업 순서: CSV 수정 -> `transform` -> 시뮬(변경 전후 같은 시드) -> 수치 보고 -> 확정된 것만 기획문서 갱신.
- 시뮬의 통계는 결정의 근거다. Test의 통과 조건으로 쓰지 않는다.

## Test

- 규칙 Test는 출고 CSV가 아니라 Test용 작은 데이터(`TestData`)를 쓴다. 밸런스를 바꿔도 Test가 깨지지 않는다.
- 규칙 하나에 Test 하나를 둔다. 기획문서의 규칙을 바꾸면 그 Test를 같이 바꾼다.
- 결정론 Test: 같은 Setup과 입력의 반복 실행, 진행 간격과 무관함, `Replay`와 실제 진행의 일치. 전진이 일어나는 전투도 포함한다.
- 자리 규칙은 `FormationTests`가, 전투 중의 전진과 아이템이 켜지는 것은 `BattleEngineTests`의 "Advancing" 묶음이 고정한다.
  보드와 인벤토리는 `ItemBoardTests`와 `ExpeditionRulesTests`의 "Rewards and the item board" 묶음이 고정한다.
- 출고 데이터 감사: 모든 던전의 맵과 전투 Setup이 만들어지고, 모든 적 무리와의 전투가 끝난다(`ShippedDataTests`).

## Validation

- `Gameplay` 폴더에 `UnityEngine`/`UnityEditor` using이 없는가? (`Tools/chain.sh sim`)
- 새 규칙이 `Docs/Design`에서 【확정】이고 Test가 있는가?
- 새 상수가 `BalanceData`나 Definition에 있는가? 코드에 숫자로 들어가지 않았는가?
- 자리를 바꾸는 새 코드가 `Formation`을 거치는가? 빈 열을 사이에 둔 줄이 상태에 남지 않는가?
- 보드를 바꾸는 새 코드가 `ItemBoard`를 거치는가? 칸 수를 넘는 보드가 상태에 남지 않는가?
- 결과에 영향을 주는 새 난수 사용이 시드에서 파생한 `Pcg32`인가?

## Deferred and Forbidden

- Deferred: 유물, 성장과 전직, 경제, 나머지 지역 속성과 노드 종류, 최종 보스, 런의 승패, 100일 전체 시뮬.
- Forbidden: Domain에서 Unity/Managers/Save DTO 참조, 결과 계산에 부동소수점이나 시드 밖 난수 사용, 표현 계층이 상태를 바꾸기,
  규칙 없이 추가한 효과 어휘, 시뮬 통계를 Test 통과 조건으로 쓰기.
