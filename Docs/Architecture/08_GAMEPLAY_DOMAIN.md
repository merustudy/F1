# 08. Gameplay Domain

Domain 코드의 구조, 결정론을 지키는 방법, 상태 Type, 시뮬 실행기를 소유한다.
게임 규칙의 내용은 소유하지 않는다. 규칙은 `Docs/Design`(02 전투, 03 던전, 04 로비), 수치는 `Assets/@Data/Source/*.csv`다.

## 위치와 경계

```text
Assets/@Scripts/Gameplay        Namespace F1.Gameplay. 순수 C#. Unity, Managers, Save DTO를 참조하지 않는다
├─ Random       Pcg32, RngStream, SeedDeriver
├─ Battle       BattleEngine, BattleSetup, BattleUnit, BattleEvent, BattleInput, BattleLog, Formation
├─ Expedition   ExpeditionState, ExpeditionRules, ItemBoard, FatigueRules, NodeMap, MapGenerator
└─ Run          RunState, RunRules, SettlementReport
```

- Domain은 `F1.Data`의 Definition(`StaticData`)만 입력으로 받는다. 파일, Addressables, 시각, 난수원을 스스로 구하지 않는다.
- `Tools/Sim`이 이 폴더를 Unity 없이 그대로 컴파일한다. Unity 참조가 생기면 체인의 sim 단계가 깨진다.
- C# 9 문법까지만 쓴다.

## 세 수명

| 수명 | 상태 | 규칙 | 끝나면 |
|---|---|---|---|
| 런 | `RunState`: 시드, 날짜, 로스터(피로도), 파티, 원정 횟수, 클리어 기록 | `RunRules` | 용병이 하나도 없으면 `IsOver` |
| 원정 | `ExpeditionState`: 던전, 시드, 맵, 구성원(HP, 보드, 피로도), 인벤토리, 포션, 단계 | `ExpeditionRules` | `RunRules.Settle`로 런에 반영하고 버린다 |
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

## 아이템 보드와 인벤토리 (격자: Slice B 19단계)

규칙은 `Docs/Design/02_Combat_System.md` §4와 `03_Dungeon_Structure.md` §5가 소유한다. 틀의 크기는 구조 상수 `BoardFrame`(3×8, `F1.Data`)이고 가방과 아이템의 모양은 Static Data(`BagData`, `ItemData.Width`·`Height`)다.

- 자리는 `Placement`(왼쪽 위 칸 X·Y와 시계 방향으로 돌린 횟수 Turns 0~3)다. 돌린 횟수가 홀수면 가로와 세로가 바뀐다(`WidthOf`·`HeightOf`).
- 구성원의 보드는 `ExpeditionMember.Board`(`ItemBoard`)이고 가방(`BoardBag`: `BagData`와 자리)과 아이템(`BoardItem`: `EquippedItem`과 자리)의 목록이다. 첫 가방은 시작 가방이고 (0,0)에 돌리지 않은 채로 있다.
  칸의 계산은 `ItemBoard` 한 곳에 있다: 그 칸의 아이템·가방(`ItemAt`·`BagAt`), 모두 가방의 칸인지(`OnBags`), 겹치는 아이템(`ItemsUnder`), 가방이 틀 안에 다른 가방 없이 놓이는지(`FreeOfBags`),
  가방 안에 온전히 든 아이템(`ItemsIn`)과 걸친 아이템(`HasItemAcross`), 읽는 순서(`InReadingOrder`: 왼쪽 위 칸의 Y, 그다음 X), 첫 빈 자리(`FindRoom`: 읽는 순서로, 돌리지 않은 모양 먼저 · `FindBagRoom`). `Starting`이 시작 보드를 만든다.
- 인벤토리는 `ExpeditionState.Inventory`(`InventoryGrid`, Round 49: 디아블로 2의 격자)다. 크기는 `BalanceData.InventoryWidth`·`InventoryHeight`(10×3)이고, 아이템은 보드처럼 자리(`BoardItem`: `Placement`)를 갖고 서로 겹치지 않는다.
  칸의 계산은 `InventoryGrid` 한 곳에 있다: 그 칸의 아이템(`ItemAt`), 격자 안에 다른 아이템 없이 놓이는지(`IsFree`, 하나를 빼고 셀 수 있다), 첫 빈 자리(`FindRoom`: 돌리지 않은 모양으로 읽는 순서의 첫 자리, 없으면 돌린 모양으로),
  자리가 있는지(`HasRoomFor`), 쓴 칸(`UsedSquares`). 목록으로 읽으면 들어온 차례의 `EquippedItem`이고(인벤토리 번호 = `Items`의 번호), `Add(아이템)`은 첫 빈 자리에 놓는다. 가방은 들어가지 않는다.
- 명령은 `ExpeditionRules`에 있고 자리를 받는다: 전리품을 보드에 넣기(`TakeLoot(자리, 구성원, Placement)`)와 인벤토리로 줍기(`TakeLootToInventory`), 옮기기(`MoveItem(구성원, X, Y, 구성원, Placement)`: 같은 보드 안, 보드 사이),
  인벤토리로 빼기(`MoveToInventory(구성원, X, Y)`: 첫 빈 자리로; `MoveToInventoryAt(구성원, X, Y, Placement)`: 고른 빈 자리로), 인벤토리 안에서 옮기기(`MoveInInventory(번호, Placement)`: 빈 칸에만, 바꾸기 없음),
  인벤토리에서 넣기(`PlaceFromInventory(번호, 구성원, Placement)`), 가방 옮기기(`MoveBag(구성원, X, Y, 구성원, Placement)`). 각각 `Can...` 질의가 있고 화면은 그림자를 그리려고 묻는다.
  버튼과 밀려남으로 인벤토리에 들어가는 길(전리품·상점의 "인벤토리에 넣기", "인벤토리로", 밀려난 아이템)은 모두 `FindRoom`의 자리에 놓고, 자리가 없으면 막힌다.
  아이템은 그 아이템이 덮은 어느 칸으로도 가리킨다. 가방은 그 가방의 칸으로 가리킨다: 집는 것(`CanPickBag`)은 아이템이 없는 칸으로만(아이템이 있는 칸은 그 아이템을 집는다), 옮기는 것(`CanMoveBag`·`MoveBag`)은 어느 칸으로도. 둘 다 시작 가방이 아니고 걸친 아이템이 없어야 한다.
- 놓기는 한 곳(`CanPut`·`Put`)이다: 자리가 모두 가방의 칸이고, 겹치는 아이템이 없거나 **하나**면 놓인다. 하나면 그것이 인벤토리의 첫 빈 자리로 간다(자리가 있을 때. 인벤토리에서 넣을 때는 나가는 아이템의 자리를 빈 것으로 친다). 둘 이상이면 안 된다.
  옮기는 아이템 자신은 겹침에서 빠진다. 같은 아이템의 왼쪽 위 칸에 왼쪽 위를 맞추면(`MergesAt`) 놓는 대신 합친다(아래 "단계와 합치기").
- 가방을 옮기면 그 안에 온전히 든 아이템이 가방 안의 자리를 지키며 함께 가고, 돌리면 함께 돈다(가방의 상자 안에서 시계 방향으로: (x, y) → (높이 − y − h, x)). 가방은 틀 안의 다른 가방이 없는 자리에만 놓인다. 시작 가방은 옮기지 않는다.
- **★(별, 20단계)**: 별의 계산은 `StarRules` 한 곳에 있다(순수 C#). 아이템의 별 칸(`ItemData.Stars`, 돌리지 않은 모양의 왼쪽 위 칸에서 잰 칸)은 아이템과 함께 돈다:
  시계 방향으로 한 번 돌리면 (x, y) → (높이 − 1 − y, x)(높이는 돌리기 전 모양의 것. 가방째 돌리기와 같은 셈). `StarSquares(아이템, 자리)`는 보드의 칸이고, `Marks(보드, 아이템, 자리, 뺄 아이템)`은
  가방 안의 별 칸마다 켜졌는지(그 칸의 아이템이 근접 무기인지)다(화면이 그린다). `DamageOn(보드, 무기)`는 그 근접 무기가 받는 피해 +N의 합이다: 별 아이템마다 그 별 칸 가운데 하나라도 무기가 덮으면
  그 아이템의 `EquippedItem.StarDamage`(데이터의 `StarDamage` × (단계 + 1): 일반 1배, 동 2배, 은 3배, 금 4배)를 **한 번** 더한다(한 아이템은 다른 아이템 하나의 별을 하나만 채운다).
  `Lit(보드, 별 아이템)`은 그 아이템의 켜진 별 칸과 그 무기들이다(카드의 "걸린 ★"). 별은 같은 보드 안에서만 세고, 적의 보드에는 별 아이템이 없다.
- 전투의 `BattleUnitSetup.Items`는 보드의 아이템을 **읽는 순서**로 늘어놓은 목록이고 `BattleItemState.SlotIndex`는 그 순서(발동 순서)다. `BattleUnitSetup.Layout`(`BoardLayout`)은 화면이 그리려는 자리(아이템마다의 `Placement`와 가방)이고 규칙은 쓰지 않는다.
  `BattleUnitSetup.StarDamage`는 같은 순서의 피해 +N이다(`ExpeditionRules`가 전투를 세울 때 `StarRules.DamageInReadingOrder`로 채운다. 적과 Test는 null = 모두 0). 엔진은 `BattleItemState.StarDamage`로 갖고,
  무기 장비의 피해 효과에 "무기 피해 +%" 패시브를 곱하기 **전에** 더한다. 발동하지 않는 아이템(`ItemData.IsPassive`)은 늘 활성이 아니어서 쿨다운이 돌지 않고 발동 이벤트도 없다. 별은 전투를 세울 때 정해지므로 결정론과 이어하기는 그대로다.
  적은 가방 없이 아이템을 한 줄씩 아래로 놓은 `BoardLayout.Stacked`다.

## 피로

규칙은 `Docs/Design/04_Lobby_100Day_Economy.md` §3이 소유한다. 피로도는 0에서 쌓이는 정수이고 0~`MaxFatigue` 안에 머문다.

- 셈은 `FatigueRules` 한 곳에 있다: 장비가 피로를 내는지(`CostsFatigue`: 무기 장비나 방어 장비이고 기본 무기가 아님), 아이템 하나와 보드의 비용,
  전투에 들어갈 때의 비용(`BattleEntryCost` = `FatigueBattleEntry` + 장비), 범위 안에 묶는 더하기(`Add`). 화면도 같은 함수로 비용을 보인다.
- 기본 무기는 아이템 인스턴스의 표시다(`EquippedItem.IsBase`). `ExpeditionRules.Create`가 직업의 무기에만 붙이고, 보드 사이로 옮겨도 남는다.
  전리품과 상점의 물건, 인벤토리에서 꺼낸 아이템은 기본 무기가 아니다.
- 수명: 로스터의 `MercenaryState.Fatigue` → 출발할 때 `PartyMember`로 원정의 `ExpeditionMember.Fatigue`에 → 원정 동안 쌓임 → `RunRules.Settle`이
  살아 돌아온 구성원의 값을 로스터에 쓴다. 원정에 나가지 않은 날(`PassDays`)에는 로스터의 값이 내려간다.
- 쌓이는 곳(12단계): `ExpeditionRules.BeginBattle`이 전투를 세우기 전에 살아 있는 구성원마다 전투에 들어가는 비용을 더한다. 그래서
  `BuildBattleSetup`은 그대로 상태만의 함수이고, 이어하기는 이미 더한 값으로 같은 전투를 다시 만든다(두 번 더하지 않는다).
- 전투 안의 피로(15단계): `BattleUnitSetup.Fatigue`·`FatigueState`로 들어가 `BattleUnit.Fatigue`·`State`가 되고, 전투가 끝나면 `CompleteBattle`이 구성원에 되쓴다(`ExpeditionMember.StateId`).
  엔진은 파티 유닛의 피로만 움직인다(`ChangeFatigue`: 피격·빈사·동료의 빈사와 죽음·처치의 덜어 냄, 이벤트 `FatigueChanged`. 화상 틱의 피해는 피격이지만 피로를 올리지 않고 폭풍 틱은 올린다 — `ApplyDamage`가 원인 `burn`을 가른다, Design/04 §3) 그리고 움직일 때마다 판정한다(`ResolveFatigue`):
  `MaxFatigue`면 쓰러짐(`Collapse`, 이벤트 `Collapsed`: 빈사로, 이미 빈사면 죽음), `FatigueBreakdown` 이상이고 상태가 없으면 붕괴 판정(`BreakDown`, 이벤트 `BrokeDown`),
  상태가 고통이고 문턱 아래면 풀림(`FatigueStateEnded`). 전투가 시작할 때 들어온 피로를 같은 함수로 먼저 판정한다(`BattleStarted` 뒤, 패시브 앞).
- 붕괴 판정의 난수는 `RngStream.Fatigue`(사망 판정의 `Battle` 스트림과 다름)다: 각성인지, 어느 상태인지 두 번 뽑는다. 상태의 목록은 `BattleSetup.FatigueStates`(데이터의 id 순)에서 온다.
- 상태의 효과는 `FatigueStateData`의 세 수치다: 쿨다운은 `CooldownOf(유닛, 아이템)`이 발동 때마다 센다(들어온 상태는 첫 쿨다운부터), 받는 회복은 `ApplyHeal`에서 `FatigueRules.Scaled`,
  빈사의 사망 확률은 `DogHit`에서 더해 0~100에 묶는다. 고통이 풀리는 세 자리(전투의 처치, 야영지의 쉬기, 로비의 쉬는 날)는 모두 `FatigueRules.AfflictionEnds`·`StateAfter`로 묻는다.

## 긴 원정: 노드 종류와 깊은 층

규칙은 `Docs/Design/03_Dungeon_Structure.md` §1·§4가 소유한다. 수치는 `DungeonData`와 `BalanceData`에 있다.

- 노드 종류는 `MapNodeKind`(전투, 정예, 야영지, 보스, 상점(17단계))다. 싸우는 노드(`MapNode.IsFought`)만 적 무리를 갖는다. 야영지와 상점에는 `EnemyGroupId`가 없다.
- `MapGenerator`가 노드마다 맵 난수로 종류를 뽑는다: 야영지 층(`CampFloor`)은 모두 야영지, 그 밖에서 정예가 설 수 있으면(`DungeonData.EliteCanStandOn`) 정예 확률,
  아니면 야영지가 설 수 있으면(`CampCanStandOn`) 야영지 확률, 아니면 상점이 설 수 있으면(`ShopCanStandOn`: 1층과 야영지 층이 아닌 `ShopMinFloor`부터) 상점 확률, 나머지는 전투다.
  자격이 없는 층에서는 난수를 쓰지 않으므로 정예·야영지·상점이 없는 데이터의 맵은 전과 같다.
  정예 노드는 정예 무리(`EnemyGroupData.IsElite`, `StaticData.ElitesFor`)에서, 전투 노드는 정예가 아닌 무리에서 고른다. 정예가 설 수 있는 층마다 정예 무리가 있어야 한다(`StaticData`의 검증).
- 야영지: `ExpeditionRules.EnterCamp`가 야영지 노드로 옮겨 `ExpeditionPhase.AtCamp`로 둔다(피로를 더하지 않는다). 그동안 고를 노드는 없고(`AvailableNodes`가 빈다) 보드와 자리는 바꿀 수 있다.
  `RestAtCamp`는 살아 있는 구성원마다 HP를 최대의 `CampHealPercent`%만큼(최대를 넘지 않게) 올리고 피로를 `CampFatigueRelief`만큼 내린 뒤(`FatigueRules.Add`) `ChoosingNode`로 돌린다. 정비는 14단계다.
- 깊은 층의 적: `BuildBattleSetup(…, floor)`은 보스가 아닌 무리의 적을 1층보다 한 층 깊어질 때마다 최대 HP `EnemyHpPerFloorPercent`%, 아이템 등급 `EnemyGradePerFloor`만큼 올린다.
  보스 무리는 데이터 그대로다. 층은 노드의 것이라 이어하기도 같은 Setup을 만든다.

## 전리품 (Slice B 18단계)

규칙은 `Docs/Design/03_Dungeon_Structure.md` §5가 소유한다. 수치는 `BalanceData`(`DropCount`, `EliteDropCount`)와 `DungeonData`(등급 `ItemGradeAt`, 단계 `ItemTierAt`: 상점의 물건과 같은 것)에 있다.

- `ExpeditionRules.CompleteBattle`이 이긴 전투(보스 아님)의 전리품을 `DropLoot(노드)`로 뽑아 `ExpeditionState.Loot`에 두고 `ExpeditionPhase.PickingLoot`로 둔다: 그 무리의 적들이 든 아이템(`EnemyData.Items`)을 모두 모아
  `DropCount`개(정예 `EliteDropCount`개)를 전리품 스트림(`RngStream.Loot`, `Derive(원정 시드, "loot", 노드 id)`)으로 겹치지 않게 뽑는다. 드랍(`ItemOffer`)의 등급은 던전의 것, 단계는 층의 것(정예 한 단계 위)이다 — 적이 들었던 등급이 아니다.
- `Loot`은 드랍마다 한 자리이고 주운 자리는 null이다. 명령: `TakeLoot(자리, 구성원, Placement)`(위 "놓기"와 같은 규칙. 가방이면 틀에), `TakeLootToInventory(자리)`(아이템만), `LeaveLoot`. 질의: `DropAt`, `CanTakeLoot`, `CanTakeLootToInventory`, `LootMergesAt(자리, 구성원, X, Y)`.
- 정예의 둘째 드랍은 `EliteBagPercent`%로 가방이다(`DrawBag`: `BagData.LootWeight`. 같은 전리품 스트림). 드랍(`ItemOffer`)의 종류는 `OfferKind.Item`·`Bag`이다.
  마지막 드랍을 주우면 `ChoosingNode`로 돌아간다(`Pick` → `EndLoot`). 포션은 떨어지지 않는다(`ItemOffer`의 포션은 상점의 것).
- 적의 아이템은 용병의 아이템과 같은 척도로 적는다(`Docs/Design/02_Combat_System.md` §4): 계수는 우리 아이템의 자리이고 적의 힘은 등급이 말한다. 그래서 던전의 등급으로 떨어진 것이 상점의 물건과 비슷한 힘이다.

## 상점과 지역 코인 (Slice B 17단계)

규칙은 `Docs/Design/03_Dungeon_Structure.md` §1·§5가 소유한다. 수치는 `BalanceData`(`ShopSlots`, `ShopRefreshBase`, `ShopRefreshStep`, `ShopPotionChancePercent`, `CoinsPerEnemy`, `CoinsPerFloor`, `EliteCoinPercent`),
`DungeonData`(`ShopMinFloor`, `ShopChancePercent`), `ItemData`·`PotionData`의 `Price`에 있다.

- 코인은 `ExpeditionState.Coins`(정수)다. 런 상태에는 없다. `ExpeditionRules.CompleteBattle`이 이긴 전투의 코인(`CoinsFor(노드)`: 적마다 + 층마다, 정예는 배, 보스와 싸우지 않는 노드는 0)을 더한다.
  원정이 끝나면 `ExpeditionState`와 함께 버려진다(정산은 코인을 모른다).
- 상점은 `ExpeditionState.Shop`(`ShopState`: 물건 `Stock`은 `ItemOffer`의 목록이고 판 자리는 null, `Refreshes`)이고 `ExpeditionPhase.AtShop` 동안만 있다.
  `EnterShop`이 상점 노드로 옮겨 물건을 뽑고(`DrawStock`), `LeaveShop`이 버리고 `ChoosingNode`로 돌린다. 피로는 더하지 않는다. 상점에서도 보드와 자리는 바꿀 수 있다(`IsBetweenBattles`).
- 물건(`Stock`)은 **물건 다음에 포션**이다(Round 56). 물건은 `DrawOffers`가 뽑는다(상점 가중치 `ShopWeight`가 있고 값이 있는 아이템과 가방, 겹침 없음, `ShopGoodsCount` = `ShopSlots`나 후보 수 중 작은 것).
  포션은 `DrawPotions`가 상점마다 한 번 뽑는다(종류마다 `ShopPotionChancePercent`, 하나도 없으면 가중치로 하나: 늘 하나 이상. 빈 포션 칸과 상관없이). 단계·등급은 그 층의 것(`DungeonData.ItemTierAt`·`ItemGradeAt`: 전리품과 같은 셈)이다.
  난수는 `RngStream.Shop`(전리품 스트림과 다름)을 `Derive(원정 시드, "shop", 노드 id)`로 열어 포션을 먼저 뽑고, 새로고침 k번째의 물건은 그 뒤를 k+1번 뽑은 마지막이다(저장한 횟수로 다시 만든다).
  새로고침은 물건 자리만 새로 채우고 포션 자리(판 것은 null 그대로)는 둔다(`RefreshShop`).
- 값은 `PriceOf(물건)` 한 곳에서 센다: 아이템은 `Price` × `BalanceData.TierPercent(단계)` / 100, 포션과 가방은 `Price`. 새로고침의 비용은 `RefreshCost` = `ShopRefreshBase` + `ShopRefreshStep` × `Refreshes`.
- 사는 명령은 전리품의 것과 짝이다: `BuyToBoard(자리, 구성원, Placement)`(합쳐지면 합치고, 아니면 놓기. 가방은 틀에), `BuyToInventory`(아이템만), `BuyPotion`. 각각 `Can...`이 코인(`CanAfford`)과 자리를 묻고, 산 자리는 null(`Pay`)이 된다.
  `RefreshShop`은 비용을 내고 모든 자리를 다시 뽑는다. `OfferAt`·`ShopMergesAt`은 화면이 묻는 질의다.

## 단계와 합치기 (Slice B 14단계)

규칙은 `Docs/Design/02_Combat_System.md` §4와 `03_Dungeon_Structure.md` §1·§5가 소유한다. 수치는 `BalanceData`와 `DungeonData`에 있다.

- 단계는 `ItemTier`(일반·동·은·금. 2026-10-07 Round 41에 동·은·금·다이아에서 이름을 바꿨다: 값의 순서는 같다. `F1.Data`)이고 아이템 인스턴스(`EquippedItem.Tier`)와 드랍과 상점의 물건(`ItemOffer.Tier`)가 갖는다. 등급은 그대로 있다.
- 효과 크기는 한 곳에서 센다: `EquippedItem.Magnitude(balance, effect)` = `ItemEffect.MagnitudeAt(등급, BalanceData.TierPercent(단계))`(정수, 내림, 1 이상). 전투와 화면이 같은 함수를 쓴다.
- 단계는 `DungeonData.ItemTierAt(층, 정예)`가 정하고 `ExpeditionRules`가 전리품과 상점의 물건에 싣는다.
- 합치기는 `ExpeditionRules.CanMerge`(같은 아이템, 같은 단계, 금 아래, 둘 다 기본 무기가 아님, 서로 다른 인스턴스)다. 보드에 놓는 명령
  (`MoveItem`, `PlaceFromInventory`, `TakeLoot`, `BuyToBoard`)은 놓을 자리의 왼쪽 위 칸이 합쳐질 아이템의 왼쪽 위 칸이면 합친다(`MergesAt`): 그 아이템이 그 자리·방향 그대로 한 단계 위(높은 등급)가 되고, 놓은 것은 원래 목록에서 빠진다.
  각 `Can...`은 합쳐질 때 칸과 인벤토리의 여유를 묻지 않는다. 합치기는 따로 된 명령이 없다(놓는 것이 합치기다).
- 정비는 `ExpeditionRules.CanUpgradeAtCamp`·`UpgradeAtCamp(구성원, X, Y)`다: 야영지에서 살아 있는 구성원 보드의 아이템 하나(덮은 칸 어디로든)를 `EquippedItem.TierUp`(기본 무기는 기본 무기로)하고 `ChoosingNode`로 돌린다.

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
          -> 전리품 난수 = Derive(원정 시드, "loot", 노드 id)
          -> 상점 난수 = Derive(원정 시드, "shop", 노드 id)
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
dotnet run --project Tools/Sim -- map        --dungeon <id> --seed n
```

- 파티는 용병 id나 직업 id로 적는다. 모두에게 열 번호를 붙이면 그 열에 세운다(`knight:1,spellblade:2,bishop:3,archmage:4`).
  붙이지 않으면 직업의 권장 열 순서로 앞에서부터 세운다.
- `formations`는 같은 파티를 가능한 모든 순서로 세워 같은 시드로 돌린다. 자리가 결과를 얼마나 바꾸는지 본다.
- `map`은 원정 시드 하나의 맵을 노드마다 한 줄로 적는다(id, 층, 열, 종류, 무리, 다음 노드). `expedition`의 보고는 원정마다 정예·야영지를 들른 수, 전투 시간(x1),
  합치기와 정비의 수, 끝날 때 보드의 단계별 아이템 수, 붕괴(고통·각성)와 쓰러짐의 수와 붕괴가 난 원정의 비율, 전리품(떨어진 수, 주운 수), 상점(들른 수, 산 것, 새로고침, 받은·쓴·남은 코인)을 함께 적는다.
  정책의 합치기와 야영지의 선택은 `Docs/Design/08_Simulation_Report.md` §10·§11, 상점의 선택(`SimPolicy.ShopAt`: 포션 둘까지, 합쳐지는 것, 자리가 맞는 것을 사고, 쓸 것이 없으면 둘까지 새로고침)은 §13,
  전리품(`SimPolicy.PickLoot`: 드랍마다 합쳐지는 것, 자리가 맞는 구성원의 빈 칸, 인벤토리의 차례로 줍고 나머지는 둔다)은 §14, 격자의 자리(`ItemBoard.FindRoom`, 가방은 가방 칸이 가장 적은 용병에게. 밀어내기·가방 옮기기는 하지 않는다)와 가방의 수는 §15.

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
  격자 보드(자리, 겹침, 읽는 순서, 빈 자리 찾기, 화면용 `BoardLayout`)는 `ItemBoardTests`, 놓기·밀어내기·돌리기·합치기 자리·가방의 명령과 인벤토리는 `ExpeditionRulesTests`의 격자 묶음이 고정한다(Test의 보드 도우미는 `TestBoards`).
  피로(장비의 비용, 기본 무기, 전투에 들어갈 때 쌓임, 범위)는 `FatigueRulesTests`, 로스터와 정산은 `RunRulesTests`가 고정한다.
  긴 원정(노드 종류의 배치, 정예 무리, 야영지와 쉬기, 깊은 층의 적, 데이터 검증)은 `LongExpeditionTests`, 야영지의 명령과 저장은 `CampFlowTests`가 고정한다.
  단계(효과 크기, 전리품의 단계, 데이터 검증), 합치기, 정비는 `TierRulesTests`가 고정한다.
  전투 안의 피로(사건, 판정, 상태의 효과, 쓰러짐, 되쓰기, 야영지·정산·쉬는 날의 풀림, 데이터 검증)는 `FatigueBreakdownTests`가 고정한다.
  상점과 지역 코인(상점 노드의 배치, 물건과 값, 사기, 새로고침, 나가기, 전투의 코인, 데이터 검증)은 `ShopRulesTests`, 상점의 명령과 저장은 `ShopFlowTests`가 고정한다.
- 출고 데이터 감사: 모든 던전의 맵과 싸우는 노드마다 그 층의 전투 Setup이 만들어지고, 모든 적 무리와의 전투가 끝난다(`ShippedDataTests`).

## Validation

- `Gameplay` 폴더에 `UnityEngine`/`UnityEditor` using이 없는가? (`Tools/chain.sh sim`)
- 새 규칙이 `Docs/Design`에서 【확정】이고 Test가 있는가?
- 새 상수가 `BalanceData`나 Definition에 있는가? 코드에 숫자로 들어가지 않았는가?
- 자리를 바꾸는 새 코드가 `Formation`을 거치는가? 빈 열을 사이에 둔 줄이 상태에 남지 않는가?
- 보드를 바꾸는 새 코드가 `ExpeditionRules`의 놓기(`CanPut`·`Put`)와 `ItemBoard`의 계산을 거치는가? 가방 밖이나 겹친 아이템, 틀 밖이나 겹친 가방이 상태에 남지 않는가?
- 피로를 바꾸는 새 코드가 `FatigueRules.Add`를 거치는가? 0~`MaxFatigue` 밖의 값이 상태에 남지 않는가?
- 결과에 영향을 주는 새 난수 사용이 시드에서 파생한 `Pcg32`인가?

## Deferred and Forbidden

- Deferred: 유물, 성장과 전직, 로비의 경제(지역 코인은 17단계에 들어왔다), 나머지 지역 속성과 노드 종류(이벤트·보물), 최종 보스, 런의 승패, 100일 전체 시뮬.
- Forbidden: Domain에서 Unity/Managers/Save DTO 참조, 결과 계산에 부동소수점이나 시드 밖 난수 사용, 표현 계층이 상태를 바꾸기,
  규칙 없이 추가한 효과 어휘, 시뮬 통계를 Test 통과 조건으로 쓰기.
