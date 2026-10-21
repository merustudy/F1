# 08. Gameplay Domain

Domain 코드의 구조, 결정론을 지키는 방법, 상태 Type, 시뮬 실행기를 소유한다.
게임 규칙의 내용은 소유하지 않는다. 규칙은 `Docs/Design`(02 전투, 03 던전, 04 로비), 수치는 `Assets/@Data/Source/*.csv`다.

## 위치와 경계

```text
Assets/@Scripts/Gameplay        Namespace F1.Gameplay. 순수 C#. Unity, Managers, Save DTO를 참조하지 않는다
├─ Random       Pcg32, RngStream, SeedDeriver
├─ Battle       BattleEngine, BattleSetup, BattleUnit, BattleEvent, BattleInput, BattleLog, Formation
├─ Expedition   ExpeditionState, ExpeditionRules, ItemBoard, StarRules, FatigueRules, NodeMap, MapGenerator
└─ Run          RunState, RunRules, SettlementReport
```

- Domain은 `F1.Data`의 Definition(`StaticData`)만 입력으로 받는다. 파일, Addressables, 시각, 난수원을 스스로 구하지 않는다.
- `Tools/Sim`이 이 폴더를 Unity 없이 그대로 컴파일한다. Unity 참조가 생기면 체인의 sim 단계가 깨진다.
- C# 9 문법까지만 쓴다.

## 세 수명

| 수명 | 상태 | 규칙 | 끝나면 |
|---|---|---|---|
| 런 | `RunState`: 시드, 날짜, 로스터(피로도), 파티, 원정 횟수, 클리어 기록, 결과(`Outcome`·`DefeatCause`) | `RunRules` | 최종 던전을 클리어하면 승리, 최종 결전에서 지거나 사망 한도에 닿거나 용병이 하나도 없으면 패배(`IsOver` = 결과가 진행 중이 아님) |
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
  인벤토리로 빼기(`MoveToInventory(구성원, X, Y)`: 첫 빈 자리로; `MoveToInventoryAt(구성원, X, Y, Placement)`: 고른 빈 자리로, 또는 같은 아이템 위에 합치기(2026-10-18)), 인벤토리 안에서 옮기기(`MoveInInventory(번호, Placement)`: 빈 칸에만(바꾸기 없음), 또는 같은 아이템 위에 합치기),
  인벤토리에서 넣기(`PlaceFromInventory(번호, 구성원, Placement)`). 각각 `Can...` 질의가 있고 화면은 그림자를 그리려고 묻는다. **가방은 놓으면 옮기지 못한다**(Round 79, 2026-10-17: `CanPickBag`·`CanMoveBag`·`MoveBag`을 지웠다. 시작 가방의 규칙이 모든 가방의 규칙).
  버튼과 밀려남으로 인벤토리에 들어가는 길(전리품·상점의 "인벤토리에 넣기", "인벤토리로", 밀려난 아이템)은 모두 `FindRoom`의 자리에 놓고, 자리가 없으면 막힌다.
  아이템은 그 아이템이 덮은 어느 칸으로도 가리킨다. 가방은 놓는 것(`CanPlaceBag`: 전리품·상점에서 틀의 빈 자리에)뿐이다.
- 놓기는 한 곳(`CanPut`·`Put`)이다: 자리가 모두 가방의 칸이고, 겹치는 아이템이 없거나 **하나**면 놓인다. 하나면 그것이 인벤토리의 첫 빈 자리로 간다(자리가 있을 때. 인벤토리에서 넣을 때는 나가는 아이템의 자리를 빈 것으로 친다). 둘 이상이면 안 된다.
  옮기는 아이템 자신은 겹침에서 빠진다. 같은 아이템의 왼쪽 위 칸에 왼쪽 위를 맞추면(`MergesAt`) 놓는 대신 합친다(아래 "단계와 합치기").
- 바닥의 드랍과 상점의 물건(`ItemOffer`)을 놓는 몸도 한 곳이다(2026-10-20 R17): 보드에 `CanPlaceOffer`·`PlaceOffer`(가방은 틀에, 아이템은 합치기 아니면 `Put`), 인벤토리의 첫 자리에 `OfferHasRoomInInventory`·`PlaceOfferInInventory`,
  인벤토리의 고른 자리에 `CanPlaceOfferInInventoryAt`·`PlaceOfferInInventoryAt`(또는 합치기), 합침의 질의 `OfferMergesAt`·`OfferMergesInInventoryAt`. 전리품의 `TakeLoot…`·`LootMerges…`는 그 위에 자리(`DropAt`)와 `Pick`을,
  상점의 `BuyTo…`·`ShopMerges…`는 코인(`AffordableAt`)과 `Pay`를 얹은 얇은 껍질이다. 놓는 규칙이 바뀌면 이 한 곳만 바뀐다.
- 가방을 옮기면 그 안에 온전히 든 아이템이 가방 안의 자리를 지키며 함께 가고, 돌리면 함께 돈다(가방의 상자 안에서 시계 방향으로: (x, y) → (높이 − y − h, x)). 가방은 틀 안의 다른 가방이 없는 자리에만 놓인다. 시작 가방은 옮기지 않는다.
- **★(별, 20단계)**: 별의 계산은 `StarRules` 한 곳에 있다(순수 C#). 아이템의 별 칸(`ItemData.Stars`, 돌리지 않은 모양의 왼쪽 위 칸에서 잰 칸)은 아이템과 함께 돈다:
  시계 방향으로 한 번 돌리면 (x, y) → (높이 − 1 − y, x)(높이는 돌리기 전 모양의 것. 가방째 돌리기와 같은 셈). `StarSquares(아이템, 자리)`는 보드의 칸이고, `Marks(보드, 아이템, 자리, 뺄 아이템)`은
  가방 안의 별 칸마다 켜졌는지(그 칸의 아이템이 근접 무기인지)다(화면이 그린다). `DamageOn(보드, 무기)`는 그 근접 무기가 받는 피해 +N의 합이다: 별 아이템마다 그 별 칸 가운데 하나라도 무기가 덮으면
  그 아이템의 `EquippedItem.StarDamage`(데이터의 `StarDamage` × (단계 + 1): 일반 1배, 동 2배, 은 3배, 금 4배)를 **한 번** 더한다(한 아이템은 다른 아이템 하나의 별을 하나만 채운다).
  `Lit(보드, 별 아이템)`은 그 아이템의 켜진 별 칸과 그 무기들이다(카드의 "걸린 ★"). 별은 같은 보드 안에서만 세고, 적의 보드에는 별 아이템이 없다.
- 전투의 `BattleUnitSetup.Items`는 보드의 아이템을 **읽는 순서**로 늘어놓은 목록이고 `BattleItemState.SlotIndex`는 그 순서(발동 순서)다. `BattleUnitSetup.Layout`(`BoardLayout`)은 화면이 그리려는 자리(아이템마다의 `Placement`와 가방)이고 규칙은 쓰지 않는다.
  `BattleUnitSetup.StarDamage`는 같은 순서의 피해 +N이다(`ExpeditionRules`가 전투를 세울 때 `StarRules.DamageInReadingOrder`로 채운다. 적과 Test는 null = 모두 0). 엔진은 `BattleItemState.StarDamage`로 갖고,
  무기 장비의 피해 효과에 "무기 피해 +%" 패시브를 곱하기 **전에** 더한다. 발동하지 않는 아이템(`ItemData.IsPassive`)은 늘 활성이 아니어서 쿨다운이 돌지 않고 발동 이벤트도 없다. 별은 전투를 세울 때 정해지므로 결정론과 이어하기는 그대로다.
  적은 시작 가방에 아이템을 읽는 차례의 처음 들어가는 자리로 놓고, 안 들어가면 가방을 아래로 늘린 `BoardLayout.Packed`다(2026-10-19 Round 86. 화면만 읽고 엔진은 읽지 않는다 — 적의 아이템은 데이터의 차례대로 돈다. 배치가 없는 Test의 Setup은 `Stacked`).

## 피로

규칙은 `Docs/Design/04_Lobby_100Day_Economy.md` §3이 소유한다. 피로도는 0에서 쌓이는 정수이고 0~`MaxFatigue` 안에 머문다.

- 셈은 `FatigueRules` 한 곳에 있다: 장비가 피로를 내는지(`CostsFatigue`: 무기 장비나 방어 장비이고 기본 무기도 유니크도 아님 — 유니크는 2026-10-19), 아이템 하나와 보드의 비용,
  전투에 들어갈 때의 비용(`BattleEntryCost` = `FatigueBattleEntry` + 장비), 범위 안에 묶는 더하기(`Add`). 화면도 같은 함수로 비용을 보인다.
- 기본 무기는 아이템 인스턴스의 표시다(`EquippedItem.IsBase`). `ExpeditionRules.Create`가 직업의 무기에만 붙이고, 보드 사이로 옮겨도 남는다.
  전리품과 상점의 물건, 인벤토리에서 꺼낸 아이템은 기본 무기가 아니다. 합친 것은 둘 중 하나가 기본 무기면 기본 무기다(2026-10-19, `MergeInto`).
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
  빈사의 사망 확률은 `FatigueRules.DogDeathChancePercent`가 더해 0~100에 묶는다(`DogHit`이 굴리고, 전투 화면의 상태 줄이 같은 값을 보인다). 고통이 풀리는 세 자리(전투의 처치, 야영지의 쉬기, 로비의 쉬는 날)는 모두 `FatigueRules.AfflictionEnds`·`StateAfter`로 묻는다.

## 긴 원정: 노드 종류와 깊은 층

규칙은 `Docs/Design/03_Dungeon_Structure.md` §1·§4가 소유한다. 수치는 `DungeonData`와 `BalanceData`에 있다.

- 노드 종류는 `MapNodeKind`(전투, 정예, 야영지, 보스, 상점(17단계))다. 싸우는 노드(`MapNode.IsFought`)만 적 무리를 갖는다. 야영지와 상점에는 `EnemyGroupId`가 없다.
- `MapGenerator`가 노드마다 맵 난수로 종류를 뽑는다: 야영지 층(`CampFloor`)은 모두 야영지, 상점 층(`ShopFloor`, 2026-10-19)은 모두 상점(난수를 쓰지 않음), 그 밖에서 정예가 설 수 있으면(`DungeonData.EliteCanStandOn`) 정예 확률,
  아니면 야영지가 설 수 있으면(`CampCanStandOn`) 야영지 확률, 아니면 상점이 설 수 있으면(`ShopCanStandOn`: 1층과 야영지 층이 아닌 `ShopMinFloor`부터) 상점 확률, 나머지는 전투다.
  자격이 없는 층에서는 난수를 쓰지 않으므로 정예·야영지·상점이 없는 데이터의 맵은 전과 같다.
  정예 노드는 정예 무리(`EnemyGroupData.IsElite`, `StaticData.ElitesFor`)에서, 전투 노드는 정예가 아닌 무리에서 고른다. 정예가 설 수 있는 층마다 정예 무리가 있어야 한다(`StaticData`의 검증).
- 야영지: `ExpeditionRules.EnterCamp`가 야영지 노드로 옮겨 `ExpeditionPhase.AtCamp`로 둔다(피로를 더하지 않는다). 그동안 고를 노드는 없고(`AvailableNodes`가 빈다) 보드와 자리는 바꿀 수 있다.
  `RestAtCamp`는 살아 있는 구성원마다 HP를 최대의 `CampHealPercent`%만큼(최대를 넘지 않게) 올리고 피로를 `CampFatigueRelief`만큼 내린 뒤(`FatigueRules.Add`) `ChoosingNode`로 돌린다. 정비는 14단계다.
- 깊은 층의 적: `BuildBattleSetup(…, floor)`은 보스가 아닌 무리의 적을 1층보다 한 층 깊어질 때마다 최대 HP `EnemyHpPerFloorPercent`%, 아이템의 세기 `EnemyPowerPerFloorPercent`%만큼 올린다(22단계: 그 전에는 아이템 등급).
  HP는 **복리**다(2026-10-17, Design/03 §1): `ExpeditionRules.DeeperMaxHp`가 층마다 `×(100 + p)/100`를 정수(0.01 단위)로 거듭한다 — 29층 아래에서 5%는 ×4.1(더하기였다면 ×2.45). 아이템의 세기는 더하기 그대로. 정수 연산이라 결정론을 지킨다.
  보스 무리도 제 노드의 층만큼 받는다(2026-10-17. 그 전에는 데이터 그대로였다). 최종 결전의 지도는 보스 하나가 1층이라 보정이 없다. 층은 노드의 것이라 이어하기도 같은 Setup을 만든다.
  **깊은 층의 아이템**(2026-10-20, Design/03 §4): `EnemySetup`은 적의 아이템을 `EnemyData.ItemsAt(층)`으로 읽는다 — 기본 아이템 뒤에 그 층까지의 깊이 묶음(`EnemyData.DeepItems`, 누적)을 데이터의 차례대로 더한 목록이고, 페이즈 묶음도 `ItemsAt(페이즈, 층)`으로 같이 더한다. 묶음의 세기에도 `EnemyPowerPerFloorPercent`의 더하기가 걸린다. **보스 무리**(`EnemyGroupData.IsBoss`)는 `carriedFloor` 1로 기본 아이템만 든다(HP의 깊이는 그대로). 전투의 발동 순서와 `BoardLayout.Packed`는 그 목록을 그대로 쓴다(틀 안에 드는지는 데이터 검사가 지킨다: `05_STATIC_DATA.md`).
- **최종 보스의 페이즈**(28단계, Design/02 §9): `EnemyData.Phases`는 `BattleUnitSetup.Phases`(`BattlePhaseSetup`: 문턱 %, 그 묶음의 `EquippedItem` 목록, 화면이 그릴 `Layout`)로 들어간다(깊은 층의 세기 배율은 묶음에도 같이 건다. 페이즈가 없는 적은 빈 목록, 아군은 null). 엔진은 피해를 적은 뒤(`ApplyDamage`의 `Damaged` 다음, 살아 있을 때) HP가 다음 문턱에 닿았으면 `EnterPhasesReached`로 그 묶음을 들게 한다: 아이템 상태를 새로 만들고(서는 자리로 `Works`를 다시 판정) 쿨다운은 그 순간부터(`NextFireMs = TimeMs + 쿨다운`), 한 타로 여러 문턱을 지나면 차례로 하나씩, 페이즈마다 `PhaseChanged`(Target 보스, A 페이즈 번호, B 문턱 %, C 남은 HP)를 적는다. `BattleUnit.Phase`·`Items`·`EquippedNow`·`Layout`이 지금의 묶음이다. 개에 든 적은 페이즈에 들지 않는다. 입력 기록의 재생은 같은 Setup으로 같은 페이즈를 지난다(결정론).

## 전리품 (Slice B 18단계)

규칙은 `Docs/Design/03_Dungeon_Structure.md` §5가 소유한다. 수치는 `BalanceData`(`DropCount`, `EliteDropCount`, `DropTierKeepPercent`)와 `DungeonData`(단계 `ItemTierAt`: 상점의 물건과 같은 것. 22단계부터 등급은 없다)에 있다.

- 일반 아이템 드랍(2026-10-17): `DropLoot`은 드랍 자리마다 던전의 `GeneralDropChancePercent`를 굴려 `DropWeight`로 뽑은 일반 아이템(그 층의 단계)을 두고, 아니면 적의 아이템을 둔다(적의 것이 다 떨어지면 일반, 가중치 있는 아이템이 없으면 전과 같이 굴리지 않고 적의 것만 — 난수열이 그대로다).
- `ExpeditionRules.CompleteBattle`이 이긴 전투(보스 아님)의 전리품을 `DropLoot(노드)`로 뽑아 `ExpeditionState.Loot`에 두고 `ExpeditionPhase.PickingLoot`로 둔다: 그 무리의 적들이 **그 층에서** 든 아이템(`EnemyData.ItemsAt(노드의 층)`: 기본 아이템과 깊이 묶음, 2026-10-20)을 모두 모아
  `DropCount`개(정예 `EliteDropCount`개)를 전리품 스트림(`RngStream.Loot`, `Derive(원정 시드, "loot", 노드 id)`)으로 겹치지 않게 뽑는다. 드랍(`ItemOffer`)의 단계는 층의 것(정예 한 단계 위)이고, 일반 위의 단계면 아이템마다 `DropTierKeepPercent`%로 그대로·아니면 한 단계 아래다(2026-10-19, `DropTier`: 따로 파생한 `Derive(원정 시드, "loot-tier", 노드 id)`의 전리품 스트림이라 무엇이 떨어지는지는 바뀌지 않는다). 수치는 아이템 데이터의 값 그대로다(22단계부터 등급은 없다) — 적이 들었던 세기(`ItemGrant.PowerPercent`)가 아니다.
- `Loot`은 드랍마다 한 자리이고 주운 자리는 null이다. 명령: `TakeLoot(자리, 구성원, Placement)`(위 "놓기"와 같은 규칙. 가방이면 틀에), `TakeLootToInventory(자리)`(아이템만), `TakeAllLootToInventory`(2026-10-17: 남은 것 가운데 인벤토리에 자리가 있는 아이템을 놓인 차례대로 모두, 각각 첫 빈 자리에; 가방과 자리 없는 것은 남고, 다 넣으면 끝남. 넣은 수를 돌려줌), `LeaveLoot`. 질의: `DropAt`, `CanTakeLoot`, `CanTakeLootToInventory`, `CanTakeAllLootToInventory`, `LootMergesAt(자리, 구성원, X, Y)`.
- 정예의 둘째 드랍은 `EliteBagPercent`%로 가방이다(`DrawBag`: `BagData.LootWeight`. 같은 전리품 스트림). 드랍(`ItemOffer`)의 종류는 `OfferKind.Item`·`Bag`이다.
  마지막 드랍을 주우면 `ChoosingNode`로 돌아간다(`Pick` → `EndLoot`). 포션은 떨어지지 않는다(`ItemOffer`의 포션은 상점의 것).
- 적의 아이템은 용병의 아이템과 같은 척도로 적는다(`Docs/Design/02_Combat_System.md` §4): 아이템의 값은 떨어지는 것의 값이고 적의 힘은 세기 %(`ItemGrant.PowerPercent`, 22단계: 그 전에는 등급)가 말한다. 그래서 떨어진 것이 상점의 물건과 비슷한 힘이다.

## 상점과 지역 코인 (Slice B 17단계)

규칙은 `Docs/Design/03_Dungeon_Structure.md` §1·§5가 소유한다. 수치는 `BalanceData`(`ShopSlots`, `ShopRefreshBase`, `ShopRefreshStep`, `ShopPotionChancePercent`, `CoinsPerEnemy`, `CoinsPerFloor`, `EliteCoinPercent`),
`DungeonData`(`ShopMinFloor`, `ShopChancePercent`), `ItemData`·`PotionData`의 `Price`에 있다.

- 코인은 `ExpeditionState.Coins`(정수)다. 런 상태에는 없다. `ExpeditionRules.CompleteBattle`이 이긴 전투의 코인(`CoinsFor(노드)`: 적마다 + 층마다, 정예는 배, 보스와 싸우지 않는 노드는 0)을 더한다.
  원정이 끝나면 `ExpeditionState`와 함께 버려진다 — 다만 **클리어**면 정산(`RunRules.Settle`)이 먼저 남은 코인과 `ItemCoinsFor`(살아 돌아온 구성원의 보드와 인벤토리의 팔 수 있는 아이템의 `SellPriceOf` 합)를 `CoinsPerGold`로 나눠 금에 더한다(2026-10-17: `SettlementReport.CoinsFromItems`·`CoinsConverted`·`GoldFromCoins`, `GoldEarned`에 포함). 후퇴·전멸은 0.
- 상점은 `ExpeditionState.Shop`(`ShopState`: 물건 `Stock`은 `ItemOffer`의 목록이고 판 자리는 null, `Refreshes`)이고 `ExpeditionPhase.AtShop` 동안만 있다.
  `EnterShop`이 상점 노드로 옮겨 물건을 뽑고(`DrawStock`), `LeaveShop`이 버리고 `ChoosingNode`로 돌린다. 피로는 더하지 않는다. 상점에서도 보드와 자리는 바꿀 수 있다(`IsBetweenBattles`).
- 물건(`Stock`)은 **물건 다음에 포션**이다(Round 56). 물건은 `DrawOffers`가 뽑는다(상점 가중치 `ShopWeight`가 있고 값이 있는 아이템과 가방, 겹침 없음, `ShopGoodsCount` = `ShopSlots`나 후보 수 중 작은 것).
  물건의 후보(`IsShopCandidate`, 2026-10-17): 가중치와 값이 있는 아이템 가운데 **직업의 기본 무기는 살아 있는 파티원의 직업 것만**(`PartyWeaponIds`). `ShopGoodsCount(data, state)`도 같은 후보를 센다.
  포션은 `DrawPotions`가 상점마다 한 번 뽑는다(종류마다 `ShopPotionChancePercent`, 하나도 없으면 가중치로 하나: 늘 하나 이상. 빈 포션 칸과 상관없이). 단계는 그 층의 것(`DungeonData.ItemTierAt`: 전리품과 같은 셈, 정예의 한 단계는 없다)이다.
  난수는 `RngStream.Shop`(전리품 스트림과 다름)을 `Derive(원정 시드, "shop", 노드 id)`로 열어 포션을 먼저 뽑고, 새로고침 k번째의 물건은 그 뒤를 k+1번 뽑은 마지막이다(저장한 횟수로 다시 만든다).
  새로고침은 물건 자리만 새로 채우고 포션 자리(판 것은 null 그대로)는 둔다(`RefreshShop`).
- 값은 `PriceOf(물건)` 한 곳에서 센다: 아이템은 `Price` × `BalanceData.TierPercent(단계)` / 100, 포션과 가방은 `Price`. 새로고침의 비용은 `RefreshCost` = `ShopRefreshBase` + `ShopRefreshStep` × `Refreshes`.
- 사는 명령은 전리품의 것과 짝이고 몸이 같다(위 "격자 보드"의 `CanPlaceOffer`·`PlaceOffer`·`PlaceOfferInInventory`·`PlaceOfferInInventoryAt`): `BuyToBoard(자리, 구성원, Placement)`(합쳐지면 합치고, 아니면 놓기. 가방은 틀에), `BuyToInventory`(아이템만), `BuyPotion`. 각각 `Can...`이 코인(`AffordableAt`: 코인이 닿는 물건, `CanAfford`)과 자리를 묻고, 산 자리는 null(`Pay`)이 된다.
  `RefreshShop`은 비용을 내고 모든 자리를 다시 뽑는다. `OfferAt`·`ShopMergesAt`은 화면이 묻는 질의다.
- **팔기**(Round 74, 2026-10-17): `SellPriceOf(EquippedItem)` = `Price` × 단계 % × `SellPercent` / 100, 적어도 1. `CanSellBoardItem(구성원, X, Y)`·`SellBoardItem`(보드에서 빼고 코인을 더함, 판 값을 돌려줌), `CanSellInventoryItem(X, Y)`·`SellInventoryItem`. `AtShop`에서만, 기본 무기(`IsBase`)와 유니크는 안 된다. 가방은 아이템이 아니라 팔리지 않는다. 판 아이템은 사라진다.

## 단계와 합치기 (Slice B 14단계)

규칙은 `Docs/Design/02_Combat_System.md` §4와 `03_Dungeon_Structure.md` §1·§5가 소유한다. 수치는 `BalanceData`와 `DungeonData`에 있다.

- 단계는 `ItemTier`(일반·1성·2성·3성 = `Common`·`Tier1`·`Tier2`·`Tier3`. 2026-10-15 Round 69에 일반·동·은·금에서, 2026-10-07 Round 41에 동·은·금·다이아에서 이름을 바꿨다: 값의 순서는 같다. `F1.Data`)이고 아이템 인스턴스(`EquippedItem.Tier`)와 드랍과 상점의 물건(`ItemOffer.Tier`)가 갖는다. 등급은 22단계에 뺐다(`EquippedItem.PowerPercent`: 용병의 것은 100, 적의 것은 그 적의 세기).
- 효과 크기는 한 곳에서 센다(22단계): `EquippedItem.Min`·`Max`(피해의 최소·최대)와 `Magnitude`(범위가 없는 효과) = `ItemEffect.Scaled(값, BalanceData.TierPercent(단계), 세기 %)`(0.01 단위 정수, 내림, 1 이상). 전투와 화면이 같은 함수를 쓴다.
- 단계는 `DungeonData.ItemTierAt(층, 정예)`가 정하고 `ExpeditionRules`가 전리품과 상점의 물건에 싣는다.
- 합치기는 `ExpeditionRules.CanMerge`(같은 아이템, 같은 단계, 금 아래, 서로 다른 인스턴스. 기본 무기도 합친다 — 2026-10-19)다. 보드에 놓는 명령
  (`MoveItem`, `PlaceFromInventory`, `TakeLoot`, `BuyToBoard`)은 놓을 자리의 왼쪽 위 칸이 합쳐질 아이템의 왼쪽 위 칸이면 합친다(`MergesAt`): 그 아이템이 그 자리·방향 그대로 한 단계 위(`TierUp`)가 되고, 놓은 것은 원래 목록에서 빠진다.
  각 `Can...`은 합쳐질 때 칸과 인벤토리의 여유를 묻지 않는다. 합치기는 따로 된 명령이 없다(놓는 것이 합치기다).
  **인벤토리에 놓는 명령도 같다**(2026-10-18 사용자 "인벤창에서도 합치기 가능하게": `MoveInInventory`, `MoveToInventoryAt`, `TakeLootToInventoryAt`, `BuyToInventoryAt`): 놓을 자리의 왼쪽 위 칸을 덮은 인벤토리의 아이템(`InventoryGrid.ItemAt`)이 합쳐질 것이면 그 자리에서 `TierUp`한다.
  질의는 `MergesInInventoryAt(아이템, X, Y)`·`LootMergesInInventoryAt(칸, X, Y)`·`ShopMergesInInventoryAt(칸, X, Y)`이고, `HasMergeTarget`은 보드와 인벤토리를 함께 본다. 자기 자신에는 합쳐지지 않는다(`CanMerge`의 같은 인스턴스 검사).
- 정비는 `ExpeditionRules.CanUpgradeAtCamp`·`UpgradeAtCamp(구성원, X, Y)`다: 야영지에서 살아 있는 구성원 보드의 아이템 하나(덮은 칸 어디로든)를 `EquippedItem.TierUp`(기본 무기는 기본 무기로)하고 `ChoosingNode`로 돌린다.

## 담금질과 유니크 (Slice B 23단계)

- 규칙은 `ForgeRules` 한 곳에 있다(순수 C#. `Docs/Design/02_Combat_System.md` §4 "담금질"). 레시피는 데이터(`RecipeData`: 결과·바탕과 단계·촉매와 단계)다.
- `ForgeRules.Options(data, state)`: 살아 있는 구성원의 보드에서 읽는 순서로, 바탕이 그 단계이고 `StarRules.Sources`가 주는 ★ 아이템 가운데 촉매가 그 단계인 것이 있으며, 결과를 바탕의 자리(`Placement` 그대로)에 두었을 때 가방 위이고 바탕·촉매 말고 아무것도 덮지 않으면 하나. 파티가 유니크를 가지면(`PartyHasUnique`: 보드와 인벤토리) 없다.
- `ForgeAtCamp(data, state, 구성원, X, Y)`(단계 `AtCamp`에서만): 촉매를 보드에서 빼고, 바탕의 `EquippedItem`을 결과 아이템으로 바꾼다(`ForgeRules.Result`: 기본 무기 여부·세기 % 그대로, 단계는 `Common` — 유니크에는 성이 없다), 단계를 `ChoosingNode`로(쉬기·정비 없음). 피로는 들지 않는다(사용자 결정).
- 유니크 아이템(`ItemData.Unique`)은 상점·전리품에 없고(`ShopWeight`·`Price` 0, 데이터 검사) 합치기 대상이 없고 정비 대상도 아니다(`CanUpgradeAtCamp`가 거른다: 성이 없다). 저장은 유니크가 `Common`일 때만 유효하다(`RunSaveMapper`). 효과가 있는 아이템도 ★을 가질 수 있다(글라디우스: 발동하는 무기이면서 자기 ★로 옆의 근접 무기를 벼린다). `StarRules`는 ★ 아이템이 발동하는지 묻지 않는다.
- 시뮬(`Tools/Sim`): 상점에서 레시피의 촉매를 먼저 사서 바탕을 든 용병의 ★ 자리에 두고, 야영지에서 레시피가 갖춰졌고 아무도 아프지 않으면 담금질한다. 보고에 담금질 /원정과 유니크를 가지고 끝난 원정.

## 런의 끝 (Slice C 25단계)

규칙은 `Docs/Design/04_Lobby_100Day_Economy.md` §2·§4·§8, 최종 던전은 `03_Dungeon_Structure.md` §1. 모두 `RunRules`다.

- **최종 결전**(`IsFinalStandDue`): 날이 `TotalDays`를 넘으면(101일째부터) 쉬기가 안 되고(`CanRest`), 출발은 최종 던전(`DungeonData.IsFinal`)으로만 된다(`CanDepart`: 다른 던전은 `FinalStandDue`). 그 출발은 `ExpeditionRules.Create(finalStand: true)`로, 지도가 보스 노드 하나(`MapGenerator.Generate(bossOnly)`, 시드를 쓰지 않음)이고 `ExpeditionState.FinalStand`가 참이다. 해금되지 않은 최종 던전도 최종 결전에는 열린다.
- **해금**(`IsUnlocked`): `DungeonData.UnlockDungeonId`가 없으면 처음부터 열려 있고, 있으면 그 던전의 클리어 수가 `UnlockClears` 이상일 때 열린다(아니면 `CanDepart`가 `Locked`).
- **끝**(`Settle` 끝의 `CloseTheRun`): 최종 던전을 클리어하면 `Victory`. 아니면 최종 결전이었으면 `FinalStandLost`, 로스터가 비었으면 `Wipeout`, 전사자가 `DeathLimit` 이상이면 `DeathLimit`로 `Defeat`. 기한을 넘긴 것 자체는 끝이 아니다. `SettlementReport`가 결과와 까닭을 담는다.
- 최종 던전은 데이터가 하나까지 가진다(`StaticData.FinalDungeon`, 검증). 25단계의 행 `abyss`는 임시다(Roadmap "임시 조치": 28단계가 Abyss Lord와 호위로 바꾼다).

## 경제·고용·시설 (Slice C 26단계)

규칙은 `Docs/Design/04_Lobby_100Day_Economy.md` §3(시설)·§5(금)·§6(고용과 해고)·§9(정산)가 소유한다. 모두 `RunRules`다. 수치는 `BalanceData`(`GoldPerFloor`·`GoldClear`·`HirePrice`·`RecruitCount`·`RosterLimit`·`TreatmentPrice`·`TreatmentRelief`)에 있다.

- **금**(`RunState.Gold`): 새 런은 0. 들어오는 곳은 `Settle`뿐이다(`GoldFor`): 도달한 층(`FloorsReached`: 끝난 노드의 층, 보스 노드는 마지막 지도 층으로 셈, 최종 결전은 0) × `GoldPerFloor` + 클리어면 `GoldClear`. `SettlementReport.GoldEarned`·`FloorsReached`·`GoldAfter`가 정산 화면에 간다. 클리어와 보스 처치는 같은 사건이라 `GoldBoss`는 두지 않았다. 나가는 곳은 고용과 치료뿐이다.
- **후보**(`RunState.Recruits`, `RecruitState`: 이름 id와 직업): `DrawRecruits`가 새 런과 끝나지 않은 런의 `Settle` 끝에서 새로 뽑는다. 이름 풀은 `MercenaryData.csv`의 `Starting`이 거짓인 행(`StaticData.RecruitPool`. 시작 로스터는 `StartingMercenaries`)이고, 로스터·전사자·해고된 이름은 뽑지 않는다(풀이 모자라면 적게). 직업은 모든 직업 가운데 균등. 난수는 `Derive(런 시드, "recruit", 원정 순번)`의 `RngStream.Recruit`라 같은 런은 같은 후보다.
- **고용**(`CanHire`·`Hire` → `HireCheck`): 런이 끝나지 않았고, 로스터가 `RosterLimit` 아래이고, 금이 `HirePrice` 이상이면 그 후보가 피로 0·고통 없음으로 로스터에 들고 후보에서 빠진다. 고용된 용병의 id는 이름 풀의 id이고 직업은 상태(`MercenaryState.JobId`)가 가진다 — 그래서 전투 Setup은 `BattleUnitSetup.JobId`를 구성원에게서 받고, 화면은 데이터의 직업이 아니라 그것으로 그림을 고른다.
- **해고**(`CanDismiss`·`Dismiss`): 비용 없음. 파티에서 빼고(뒤가 전진) 로스터에서 지워 `RunState.Dismissed`에 적는다(다시 뽑히지 않음). 마지막 한 명은 해고할 수 없다.
- **치료**(`CanTreat`·`Treat` → `TreatCheck`): 금이 `TreatmentPrice` 이상이고 피로가 0보다 크면 `TreatmentPrice`를 내고 피로가 `TreatmentRelief`만큼 내려간다(`FatigueRules.Add`, 0 아래로는 안 감). 날은 가지 않는다. 고통은 문턱 아래로 내려가면 쉬는 날과 같이 풀린다(`StateAfter`).
- 고용·해고·치료는 기한을 넘긴 로비(최종 결전 상태)에서도 된다. 쉬기만 안 된다(04 §2).
- `BeginExpedition`은 `ExpeditionCount`와 함께 던전별 횟수(`RunState.ExpeditionsByDungeon`)를 센다(결과 창의 "원정 N회 (광산 a, 심연 b)").

## 성장: 레벨과 경험치 (Slice C 27단계)

규칙은 `Docs/Design/05_Mercenary_Growth.md` §2가 소유한다. `LevelRules`와 `RunRules.Settle`이다. 사다리는 `LevelData.csv`(`StaticData.Levels`·`MaxLevel`·`LevelAt`), 경험치의 양은 `BalanceData`(`ExpPerFloor`·`ExpClear`).

- `MercenaryState.Level`(1부터)·`Exp`(다음 레벨까지 쌓인 것. 마지막 레벨은 0). 새 런의 로스터와 고용된 용병은 레벨 1. 죽은 용병의 레벨은 함께 사라진다.
- `Settle`: 살아 돌아온 파티원 모두 같은 경험치(`LevelRules.ExpFor`: 도달한 층 × `ExpPerFloor` + 클리어 `ExpClear`)를 받고(`Gain`: `ExpToNext`를 채울 때마다 한 레벨씩, 남은 것은 이월, 마지막 레벨에서는 0), `SettlementReport.ExpEarned`·`LevelUps`(새 레벨에 닿은 용병)가 정산 화면에 간다.
- ~~레벨은 최대 HP를 올린다~~ (Slice D 34단계에 레벨의 HP 보너스를 지웠다: 레벨은 스탯의 성장으로 HP에 닿는다 — 아래 "편차와 성장"). `ExpeditionRules.Create`가 `LevelRules.MaxHpOf`(직업 최대 HP × (100 + 체력 %)/100)로 구성원의 `MaxHp`·`Hp`를 세운다. 전투 Setup은 구성원의 값을 그대로 쓴다. 피로의 150 문턱과 무기 세기는 레벨이 바꾸지 않는다(HP 대 무기 세기의 측정은 `Docs/Design/08_Simulation_Report.md` §25).

## 건물과 건설 (Slice C 30단계, Round 78)

규칙은 `Docs/Design/04_Lobby_100Day_Economy.md` §7(건물의 단계와 건설)이 소유한다. `BuildingRules`와 `RunRules`다. 사다리는 `BuildingData.csv`(`StaticData.Buildings`·`MaxBuildingLevel`·`BuildingAt`), 기본값은 `BalanceData`(`RosterLimit`·`RecruitCount`·`TreatmentRelief`).
건물은 **셋**(`BuildingId.All`: 막사·쉼터·역마차)이다. 성문은 건물이 아니다(2026-10-18 사용자 "성문은 업그레이드 없음(원정)": 로비의 그림과 단추일 뿐이고 레벨·공사·효과·잠금이 없다).

- 상태: `RunState.BuildingLevels`(건물 id → 레벨. 없으면 1)와 `Constructions`(`ConstructionState`: 건물·짓는 레벨·끝나는 날, 건물마다 하나). `BuildingRules.LevelOf`·`ConstructionOf`·`DaysLeft`(끝나는 날 − 오늘, 최소 1).
- 업그레이드(`CanUpgrade`·`Upgrade` → `UpgradeCheck`): 런이 끝나지 않았고 기한을 넘기지 않았고(`FinalStandDue`), 다음 레벨이 있고(`MaxLevel`), 공사 중이 아니고(`UnderConstruction`), 금이 `Price` 이상이면 금을 내고 `FinishDay = 오늘 + Days`의 공사를 더한다. 동시에 몇 채든.
- 완성: 날이 가는 곳은 `RunRules.PassDays`(정산과 쉬기)뿐이고, 그 끝에서 `BuildingRules.Complete`가 끝나는 날에 닿은 공사를 레벨로 바꾼다. 끝난 건물은 `Rest`의 반환값과 `SettlementReport.BuildingsCompleted`로 돌아온다.
- 잠금(`IsLocked`): 공사 중이면 잠긴다 — `CanHire`는 `CoachLocked`, `CanTreat`는 `InnLocked`, `CanDismiss`는 거짓. 출발은 잠기지 않는다(성문은 짓지 않는다). 쉬기는 잠기지 않는다(날이 가는 길). 기한을 넘기면(`IsFinalStandDue`) 날이 가지 않으므로 잠금이 없고 짓지도 못한다.
- 효과: `RosterLimit`·`RecruitCount`·`TreatmentRelief`(기본값 + 그 레벨의 `Bonus`). `DrawRecruits`·`CanHire`·`Treat`·저장 검증이 이 값을 읽는다. 밸런스 상수는 데이터에만 있다. (성문의 정산 금 %는 2026-10-18에 뺐다.)
- 시뮬 `run`의 정책(`--build 0|1`): 고용·치료 뒤 금이 "다음 레벨의 값 + 고용비" 이상이면 가장 싼 다음 레벨부터(같으면 역마차 → 막사 → 쉼터). 전후는 `Docs/Design/08_Simulation_Report.md` §34·§35.
- Test: `BuildingRulesTests`(시작 레벨, 값과 공사, 쉬기·정산의 완성, 동시 건설과 금, 잠금 셋과 쉬기·출발, 기한 뒤, 효과 셋, 같은 시드), `RunSaveMapperTests`(17, 15 → 16, 16 → 17: 성문을 버림, 17의 성문은 거름), `StaticDataTransformerTests`·`ShippedDataTests`(사다리).

## 스탯 (Slice D 32단계 → 2026-10-20 스탯 개정, 41단계)

규칙은 `Docs/Design/05_Mercenary_Growth.md` §2(스탯 여섯·기준값·직업 기본값·체력의 HP)와 `02_Combat_System.md` §4(세기·쿨타임·치명타)·§5(명중·회피)·§6(빈사), `04_Lobby_100Day_Economy.md` §3(인내·각성)이 소유한다. 경위는 `Docs/Design/17_Stat_Revision_Proposal.md`. 수치는 `JobData`의 스탯 열과 `BalanceData`다.

- Type(`F1.Data`): `Stat`(`Str`·`Dex`·`Con`·`Int`·`Wis`·`End` — 2026-10-20에 `Spi`가 `Wis`가 됐다)과 `StatBlock`(불변, 여섯 정수, `this[Stat]`). 직업의 기본값은 `JobData.Stats`(출고 CSV는 여섯 열이 필수, 값은 1~8). **null이면 모두 `StatBaseline`**으로 센다(Test 데이터의 직업). 직업에 HP 열은 없다(2026-10-20).
  아이템은 **물리/마법** `ItemData.School`(`ItemSchool`: 무기 장비·공격 아이템은 비우면 `Physical`, 방어 장비는 선택, 지원·기타는 없음 — 생성자가 거절)을 가지고, 세기의 축 `ItemData.ScalesWith`는 거기서 **파생**한다(물리 무기 → `Str`, 마법 무기 → `Int`, 공격·지원 아이템 → `Wis`, 그 밖은 null). `QuickenedBy`는 쿨타임의 축(물리 장비 → `Dex`, 마법 장비 → `Wis`, 그 밖은 null). `CritDamagePercent`(아이템·특성)는 물리 치명타 배수의 %p다.
- **기준값은 효과마다**(`BalanceData`): 세기·쿨타임 `PowerBaseline`(5), 명중·치명타·회피·각성·빈사 `StatBaseline`(10), 오르는 피로 `FatigueBaseline`(5), HP는 0부터의 합. 범위 `StatMin`~`StatMax`(1~30). 검사: 세 기준이 범위 안, 가장 낮은 스탯의 세기 ≥ 1%, 가장 높은 스탯의 쿨타임 ≥ 1%, `HpBandLow` < `HpBandMid`.
- 규칙은 `StatRules` 한 곳이다: `BaseOf`(직업의 기본값, 없으면 기준값 여섯), `PowerPercent`(100 + `StatPowerPercentPerPoint` × (축 스탯 − `PowerBaseline`)), `CooldownPercent`(100 − `CooldownPercentPerPoint` × (`QuickenedBy` 스탯 − `PowerBaseline`), 최소 1; 축이 없거나 적이면 100)와 `CooldownMs`(반올림, 최소 `MinCooldownMs`), `MaxHpOf(balance, 체력)`(1~`HpBandLow`점은 `HpPerConPointLow`, `HpBandMid`점까지 `HpPerConPointMid`, 그 위 `HpPerConPointHigh`의 합), `Scale`(곱하고 내림, 최소 1)과 `ScaleStacks`(반올림, 최소 1), 그리고 아래 "판정의 스탯"의 굴림들.
- 상태: `MercenaryState.Stats`·`RecruitState.Stats`(새 런의 로스터와 뽑힌 후보는 `BaseOf`에 후보의 편차), 고용은 후보의 것을 그대로. `PartyMember.Stats`(null이면 `BaseOf`) → `ExpeditionMember.Stats` → `BattleUnitSetup.Stats`(적은 null). 원정 동안 바뀌지 않는다.
- 최대 HP: `LevelRules.MaxHpOf(data, jobId, stats, traits)` = `StatRules.MaxHpOf(체력)` × (100 + 특성의 %) / 100(내림, 최소 1). 직업은 이름뿐이다. 레벨은 체력의 성장으로만 HP에 닿는다(아래 "편차와 성장").
- 세기: `BattleEngine.Activate`가 주인의 `Stats`와 아이템의 `ScalesWith`로 `PowerPercent`를 구해 곱한다 — 피해는 대상마다 **치명타 뒤, 무기 피해 % 패시브 앞**(굴림 → 별 → 치명타 → 스탯 → 패시브), 회복·보호막은 `Scale`, 화상은 `ScaleStacks`. 적(`Stats` null)과 축 없는 아이템(방어 장비·기타)은 그대로.
- 쿨타임(2026-10-20): `BattleEngine.CreateUnit`과 페이즈 진입이 아이템마다 `EffectiveCooldown(기본, 지역 permille, StatRules.CooldownPercent)` = 기본 × (1000 + permille) / 1000 × % / 100을 1ms로 반올림(최소 `MinCooldownMs`)해 `BattleItemState.CooldownMs`로 둔다 — 한 번, 전투를 세울 때. 피로 상태의 %(`CooldownOf`)는 그 위에 곱한다. 적은 permille만(스탯 없음). 지역 속성이 없으면 카드의 `StatRules.CooldownMs`와 같은 수다.
- 화면의 셈(`UiText`의 카드)은 같은 `StatRules`를 부른다. 표시만 하고 상태를 바꾸지 않는다.
- Test: `StatRulesTests`(기준·위·아래의 %, 아이템의 학파와 축, 쿨타임 %와 ms, HP 구간, 치명타 배수, `BaseOf`, 런에서 전투까지, 밸런스·데이터의 거절), `StatBattleTests`(힘의 피해, 치명타 뒤 패시브 앞, 지혜의 회복·보호막·화상과 지식의 회복, 쿨타임의 단축, 적과 축 없는 아이템, 같은 Setup의 같은 로그), `LevelRulesTests`·`TraitRulesTests`의 HP.

### 판정의 스탯 (Slice D 33단계 → 2026-10-20 스탯 개정)

규칙은 `Docs/Design/02_Combat_System.md` §4(치명타)·§5(명중·회피·빗나감)·§6(빈사), `04_Lobby_100Day_Economy.md` §3(인내·각성)이 소유한다. 수치는 `BalanceData`(`HitBasePercent`·`HitPerDexPoint`·`DodgePerDexPoint`·`HitMinPercent`·`HitMaxPercent`·`CritPerDexPoint`·`CritPerWisPoint`·`PhysicalCritDamagePercent`·`MagicCritDamagePercent`·`DogDeathPercentPerEndPoint`·`VirtuePerEndPoint`·`FatiguePercentPerEndPoint`·`FatigueBaseline`)와 `EnemyData`의 `Accuracy`·`Dodge`다.

- 확률은 **만분율**(0.01%p = 1)이다. `HitPerDexPoint`·`DodgePerDexPoint`·`CritPerDexPoint`·`CritPerWisPoint`는 0.01 단위 Key(CSV "1")라 그 값이 곧 만분율의 1점이다. 굴림은 `NextInt(10000)`. 빈사와 각성은 1%p 단위다.
- 빗나갈 수 있는 것(`StatRules.CanMiss`)은 **물리**(`School` Physical) 무기 장비와 공격 아이템의 `Damage`·`Burn`뿐이다. **마법은 언제나 맞는다**(굴리지 않는다 — 난수열도 쓰지 않는다). 회복·보호막·패시브·화상 틱·폭풍은 전처럼 빗나가지 않는다. `Activate`가 **대상마다** 그 효과를 적용하기 전에 굴린다.
- 명중(`StatRules.HitChance`): `HitBasePercent`×100 + 공격자의 정확 − 대상의 회피를 `HitMinPercent`~`HitMaxPercent`(×100)로 자른다. 정확(`Accuracy`)은 용병이면 (**민첩** − `StatBaseline`) × `HitPerDexPoint` + 특성의 명중 %p×100(물리에만 닿으니 특성의 명중도 물리뿐), 적이면 `Accuracy`×100. 회피(`Dodge`)는 용병이면 (민첩 − 기준) × `DodgePerDexPoint` + 특성의 회피 %p×100, 적이면 `Dodge`×100. 확실한 명중(100%)은 굴리지 않는다.
- 치명타(`StatRules.CritChance`): (아이템의 `CritChancePercent` + 주인의 패시브 %p + 특성의 치명타 %p − 대상의 `CritTakenPercent`)×100 + 학파의 스탯 — 물리 무기는 (민첩 − 기준) × `CritPerDexPoint`, 마법 무기는 (**지혜** − 기준) × `CritPerWisPoint` — 를 0~10000으로 자른다. 값이 0인 무기는 굴리지 않는다. 배수(`StatRules.CritDamagePercent`): 물리는 `PhysicalCritDamagePercent` + 무기의 `CritDamagePercent` + 특성의 `CritDamagePercent`(`TraitBonus`), 마법은 `MagicCritDamagePercent`. `RollCritical`이 피해에 곱한다(회복·보호막은 치명타가 없다).
  패시브 %p(36단계, 방랑자)는 `StatRules.CritBonusPercent(패시브, 아이템)`: 치명타 확률이 있는 **무기 장비**에만.
  회피의 패시브 %p(47단계, 도적 `DodgePercent`)는 `StatRules.DodgeBonusPercent(패시브)`: `StatRules.Dodge`가 특성의 %p 옆에 더한다(`BattleEngine.RollHit`이 대상의 패시브를 넘김). 회피가 그렇듯 물리 공격에만(마법은 굴리지 않는다).
- 빈사의 사망 확률(`FatigueRules.DogDeathChancePercent`): 기본값 + 피로 상태의 %p − (**인내** − 기준) × `DogDeathPercentPerEndPoint` + 특성의 %p, 0~100.
- 각성(`StatRules.VirtueChancePercent`): `VirtueChancePercent` + (**인내** − 기준) × `VirtuePerEndPoint` + 특성의 %p, 0~100. `BrokeDown`의 B가 그 확률이다.
- 인내(`StatRules.FatigueGain`): 오르는 피로(양수)만 (100 − `FatiguePercentPerEndPoint` × (인내 − **`FatigueBaseline`**) + 특성의 %)%로 곱하고 반올림한다(0 아래 없음). 전투 안은 `ChangeFatigue` 한 곳, 전투에 들어가는 비용은 `ExpeditionRules.BeginBattle`이다.
- 저장: 24(`Docs/Architecture/07_SAVE.md`)가 `Spi`를 `Wis`로 읽고 스탯을 새 척도로 접는다. 진행 중 전투를 이으면 지금 규칙으로 재현한다(`Diverged`).
- Test: `StatJudgementTests`(민첩의 명중·회피, 물리만 빗나감과 마법의 명중, 빗나감의 무효, 대상마다의 굴림, 학파별 치명타, 방랑자의 패시브, 도적의 회피 패시브(47단계), 인내의 빈사·각성, 인내의 피로), `ArmorTests`(받는 치명타), `TraitRulesTests`.

### 편차와 성장 (Slice D 34단계)

규칙은 `Docs/Design/04_Lobby_100Day_Economy.md` §6(후보의 편차)과 `05_Mercenary_Growth.md` §2(레벨업의 굴림)가 소유한다. 수치는 `BalanceData.RecruitStatSpread`와 `JobData`의 성장 등급(`JobData.Growth`, `StatBlock`: 스탯마다 0~200%. null이면 성장 없음 — Test 데이터)이다.

- 후보(`RunRules.DrawRecruits`): 이름 → 직업 → 스탯마다 `NextInt(2 × spread + 1) − spread`를 같은 `Recruit` 스트림에서 차례로(힘 … 인내) 굴려 직업 기본값에 더하고 `StatMin`~`StatMax`로 자른다. 시작 로스터(`NewRun`)는 편차가 없다.
- 성장(`LevelRules.Grow`): 한 레벨마다 스탯마다 등급 ÷ 100을 더하고 나머지 %가 0보다 크면 `NextInt(100)` < 나머지로 1을 더 준다(`StatMax`에서 멈춤).
- **레벨업은 막사에서**(35단계): `Settle`은 살아 돌아온 파티원에게 경험치만 준다(`LevelRules.Gain`: 쌓기만, `ExpToNext`를 넘어도 버리지 않음, 마지막 레벨은 0). `LevelRules.CanLevelUp`(마지막 레벨 아님 + 경험치 ≥ `ExpToNext`), `LevelUp(data, 용병, 런 시드)`: 경험치 − `ExpToNext`, 레벨 + 1(마지막이면 경험치 0), 스트림 `new Pcg32(Derive(런 시드, "growth:" + 용병 id, 오른 레벨), RngStream.Growth)`로 한 레벨을 굴려 오른 스탯을 돌려준다 — 누르는 차례·시각과 무관하고 다시 불러와도 같다(굴림과 함께 저장). `RunRules.CanLevelUp`(런이 이어짐 + 로스터에 있음, 막사의 공사는 막지 않음)·`LevelUp`.
- `SettlementReport.ReadyToLevelUp`(생환자의 차례): 그 정산 뒤 올릴 수 있는 생환자(전에 미뤄 둔 것도). 정산 화면의 한 줄이 읽는다. 시뮬의 정책은 정산 직후 로스터의 차례로 올릴 수 있는 만큼 모두 올린다.
- 최대 HP는 `LevelRules.MaxHpOf(data, jobId, stats)` = 직업 HP × (100 + 체력 %) / 100. 레벨은 HP를 바꾸지 않는다(`LevelData.MaxHpBonusPercent` 삭제).
- Test: `GrowthTests`(후보 편차의 범위·같은 시드·시작 로스터 없음, 등급의 확정분과 나머지 굴림, 여러 레벨, `StatMax`, 등급 없는 직업, HP에 레벨이 없음, 35단계: 정산은 굴리지 않고 막사의 굴림은 누르는 차례와 무관), `LevelRulesTests`(쌓기·한 레벨씩·이월·마지막 레벨, 정산의 `ReadyToLevelUp`), `RunSaveMapperTests`(18 → 19의 기대 성장, 20의 넘는 경험치와 막사 레벨업의 저장).

### 특성 (Slice D 37단계)

규칙은 `Docs/Design/05_Mercenary_Growth.md` §6이 소유한다(Design/16 판정). 수치는 `TraitData`의 효과 열과 `BalanceData`(`RecruitTraitWeight0`~`2`).

- Type: `TraitData`(`F1.Data`, 불변: 종류·장점/약점·가중치·반대·효과 일곱, `Clashes`)와 `TraitBonus`(`F1.Gameplay`, 효과의 합. `None`은 0). 규칙은 `TraitRules` 한 곳이다: `BonusOf(data, ids)`(합), `CanTake(data, held, trait)`(두 번 없음, 충돌 없음), `DrawInnate(data, rng)`.
- 상태: `MercenaryState.Traits`·`RecruitState.Traits`(id의 목록, 얻은 차례). 새 런의 로스터는 `MercenaryData.Traits`(데이터, 굴리지 않음), 후보는 `DrawInnate`, 고용은 후보의 것을 가져간다. `PartyMember.Traits` → `ExpeditionMember.Traits`(원정 동안 그대로) → `BattleUnitSetup.Traits`(`TraitBonus`, 적은 null).
- 후보의 굴림(`RunRules.DrawRecruits`): 이름 → 직업 → 스탯 → **특성**, 같은 `Recruit` 스트림. `DrawInnate`는 개수를 가중치 셋으로(`NextInt(합)`) 고르고, 하나씩 아직 가질 수 있는 타고난 특성 가운데 `Weight`로 고른다(id 차례). **고를 특성이 없거나 1·2개의 가중치가 0이면 굴리지 않는다** — 특성이 없는 데이터(Test)는 전처럼 뽑는다.
- 식의 항(각 식은 그 항을 더하고 원래의 자름을 그대로 한다): 명중 `StatRules.Accuracy`(+ `HitPercent`×100, 용병만), 회피 `Dodge`(+ `DodgePercent`×100), 치명타 `CritChance`(+ `CritPercent`, 치명타가 있는 아이템만), 각성 `VirtueChancePercent`(+ `VirtuePercent`), 오르는 피로 `FatigueGain`(인내의 %에 `FatiguePercent`를 더함), 빈사 `FatigueRules.DogDeathChancePercent`(+ `DogDeathPercent`), 최대 HP `LevelRules.MaxHpOf`(체력의 %에 `MaxHpPercent`를 더함, 1% 아래 없음). 전투와 화면(아이템 카드의 명중·치명타, 전투 칸의 빈사 확률)이 같은 함수를 부른다.
- 직업 특성은 직업 패시브(02 §7) 그대로다. 화면만 특성으로 보인다(`JobData.PassiveName`).
- 결정론: 특성은 Setup의 입력이고 굴림은 런 시드의 스트림이다. 저장 22가 목록을 가진다(Architecture/07).
- Test: `TraitRulesTests`(합, 두 번·충돌, 굴리지 않는 경우와 스트림, 개수의 가중치·같은 시드·모든 타고난 특성, 식의 항 일곱, 전투 로그의 명중·빈사 확률, 데이터 → 로스터·후보·고용·원정·Setup), `StaticDataValidationTests`(반대·시작 로스터의 검증, 정의의 범위), `ShippedDataTests`(첫 묶음 여섯과 짝, 시작 로스터, 직업 특성의 이름), `RunSaveMapperTests`(22의 저장·검증·21 → 22).

### 얻은 특성 (Slice D 38단계)

규칙은 `Docs/Design/05_Mercenary_Growth.md` §6(38단계 판정)이 소유한다. 수치는 `TraitData`(`Source`, `Weight`, 효과 열)와 `BalanceData`(`TraitChance*Percent`).

- Type: `TraitSource`(`F1.Data`: `Clear`·`Retreat`·`DeathsDoor`·`Affliction`·`Virtue`, 굴리는 차례)와 `TraitData.Source`(얻은 특성만). 규칙은 `TraitRules`에 더했다: `SourcesOf(data, 결과, 구성원)`(살아 돌아온 구성원이 만난 계기 — 클리어 또는 후퇴, 빈사 표시, 끝날 때의 상태가 고통이면 고통·각성이면 각성; 죽은 이는 없음), `ChancePercent(balance, 계기)`, `AcquiredStream(런 시드, 용병 id, 원정 수)`, `DrawAcquired(data, 가진 것, 계기들, rng)`.
- 굴림(`DrawAcquired`): 계기마다 그 계기의 얻은 특성 가운데 가질 수 있는 것(가진 것과 이 굴림이 준 것에 대해 `CanTake`, 가중치 0 초과, id 차례)을 모으고, **없거나 확률이 0이면 굴리지 않는다**. 있으면 `NextInt(100) < 확률`, 맞으면 `NextInt(가중치 합)`으로 하나. 계기마다 하나까지.
- 빈사 표시: `ExpeditionMember.DeathsDoor`. `ExpeditionRules.CompleteBattle`이 전투 로그의 `DogEntered`의 대상을 켠다(한 번 켜지면 원정 동안 그대로; 0 HP로 다음 전투를 시작한 이는 앞 전투에서 이미 켜졌다). 저장 23이 가진다(Architecture/07).
- 정산(`RunRules.Settle`): 살아 돌아온 구성원마다 경험치 다음에 `DrawAcquired(로스터의 특성, SourcesOf, AcquiredStream(런 시드, id, 런의 ExpeditionCount))` — 얻은 것을 로스터의 `Traits` 끝에 더하고 `SettlementReport.NewTraits`(`AcquiredTraits`: 용병 id와 특성 id, 생환자의 차례)에 담는다. 런을 끝내는 정산도 똑같다.
- 결정론: 스트림은 런 시드에서 용병과 원정 수로 파생하므로 정산의 차례, 막사의 레벨업, 다른 스트림과 무관하다. 정산과 같은 저장에 들어가 이어하기로 되돌릴 수 없다.
- 시뮬: `run`의 보고에 계기를 만난 생환자 수와 계기마다 얻은 수(런마다), 끝에 산 용병의 얻은 장점·약점. 정책은 그대로(Design/08 §45).
- Test: `AcquiredTraitTests`(계기와 그 차례·죽은 이, 계기마다 하나·가진 것과 막는 짝은 제외·굴리지 않는 경우와 스트림, 확률과 가중치의 비율·같은 시드, 정산이 로스터와 보고에 남김·용병의 스트림·죽은 이 없음, 확률 0·후퇴의 특성, `CompleteBattle`의 빈사 표시), `ShippedDataTests`(얻은 일곱의 계기·극성·효과·확률), `StaticDataValidationTests`(계기 열의 검증), `StaticDataTransformerTests`(`Source` 열), `RunSaveMapperTests`(23의 빈사 표시·22 → 23).

### 방어구 (Slice D 39단계)

규칙은 `Docs/Design/02_Combat_System.md` §3·§4 "방어구"가 소유한다. 수치는 `ItemData`(효과의 보호막, `Trigger`, `CritTakenPercent`).

- 전투 시작 때 발동하는 아이템(`ItemTrigger.BattleStart`): `BattleEngine`의 생성자가 `BattleStarted`, 들어온 피로의 판정, 직업의 전투 시작 패시브(아군·적) 다음에 `ApplyBattleStartItems`(아군 먼저, 유닛 순서, 칸 순서)로 한 번 `Activate`한다(`ItemActivated` 로그, 보호막은 `ShieldGained`). 그 아이템은 쓸 수 있는 자리에서 `Active`이지만 돌지 않는다(`BattleItemState.Cycles`가 거짓: `NextItemMs`·`FireItems`가 건너뜀). 난수를 쓰지 않는다(보호막).
- 받는 치명타(`CritTakenPercent`): `RollCritical`이 대상의 `Active`인 아이템의 값을 더해(`CritTakenPercent(target)`) `StatRules.CritChance`의 마지막 인자로 뺀다(0 아래 없음). 굴림은 무기의 확률이 0이 아니면 늘 뽑으므로 피해 스트림의 차례는 투구와 무관하다. 단계는 곱하지 않는다. 카드(대상이 없음)는 0으로 셈한다.
- 피로: 방어 장비라 `FatigueRules.CostsFatigue`가 그대로 하나에 `FatigueEquipment`를 낸다.
- 시뮬: 정책은 그대로(상점·전리품을 "쓸 수 있는 자리의 첫 용병"에게). `expedition`의 보고에 원정 끝 보드의 방어구 수와 1열의 것(Design/08 §46).
- Test: `ArmorTests`(전투 시작의 한 번·패시브 다음·돌지 않음·자리 밖은 발동 없음·단계의 보호막, 받는 치명타의 뺌·0 아래 없음·굴림은 그대로 뽑음, 장비 피로, 정의의 검사 — `Trigger`와 `CritTakenPercent`), `StaticDataTransformerTests`(두 열), `ShippedDataTests`(철 갑옷·철 투구).

### 장비 요구 스탯 (Slice D 42단계)

규칙은 `Docs/Design/02_Combat_System.md` §4 "장비 요구 스탯"과 `03_Dungeon_Structure.md` §5, 후보의 보장은 `04_Lobby_100Day_Economy.md` §6, 직업 기본값의 보장은 `05_Mercenary_Growth.md` §2가 소유한다. 수치는 `ItemData`의 `ReqStat`·`ReqValue`다.

- Type: `ItemData.ReqStat`(`Stat?`)·`ReqValue`·`HasRequirement`. 판정은 `StatRules.MeetsRequirement(주인의 스탯, 아이템)` 한 식이다: 요구가 없거나, 스탯이 없거나(적, 주인 없는 카드), 그 스탯이 요구값 이상이면 참. 화면도 같은 식을 부른다.
- 보드의 질의는 `ExpeditionRules.MeetsRequirement(state, 구성원, 아이템)`(구성원의 `Stats`. `Create`가 모든 구성원에게 스탯을 준다)이고, 보드에 놓는 모든 길의 맨 앞에서 거절한다: `CanPlaceOffer`(전리품·상점, R17의 한 몸), `CanPlaceItem`, `CanMoveItem`(같은 보드에 다시 놓기 포함), `CanPlaceFromInventory`.
  합침의 질의(`OfferMergesAt`·`MergesAt`)에도 같은 판정이 들어 그림자와 명령이 어긋나지 않는다. 인벤토리의 질의(`…InInventoryAt`)와 `CanTakeLootToInventory`·`CanBuyToInventory`·`CanMoveToInventory`는 묻지 않는다.
- 놓을 때만 본다: `ExpeditionMember.Stats`는 원정 동안 바뀌지 않고 성장은 오르기만 하므로 보드의 아이템은 유효하다. 전투 엔진은 요구를 모른다(`BattleUnitSetup`에 요구가 없다). 원정 출발의 기본 무기(`ItemBoard.Starting`)와 담금질의 결과(`ForgeRules`)는 명령이 아니라 Setup이라 묻지 않는다 —
  그래서 데이터 검사(직업 기본값 ≥ 기본 무기의 요구, 레시피의 결과 ≤ 바탕의 요구)와 아래의 보장이 그 자리를 지킨다.
- 보장: `StatRules.MeetingBaseWeapon(data, 직업, 스탯)`은 그 직업 기본 무기의 요구 스탯을 요구값까지 올린 스탯이다(다른 스탯은 그대로, 난수 없음). `RunRules.DrawRecruits`가 편차 다음에 쓰고(뒤의 특성 굴림은 같은 수를 뽑는다), 저장 25(`RunSaveMigrator.From24To25`)가 옛 로스터·후보에 쓴다. 시작 로스터는 직업 기본값이라 데이터 검사로 충분하다.
- 시뮬: 정책(`Tools/Sim`의 `SimPolicy`)은 요구를 채우는 구성원에게만 놓는다(`MemberWithRoomFor`·`MemberWithStarRoomFor`·`FindMergeTarget`). `expedition`의 보고에 "요구로 보드에 못 놓은 드랍"(어느 구성원은 자리가 있었으나 요구를 채우는 구성원이 없던 것)을 더했다. 전후는 Design/08 §49.
- Test: `ItemRequirementTests`(식, 다섯 길의 거절과 허용, 합침의 질의, 요구 없는 아이템, 적, 후보의 올림과 난수, `MeetingBaseWeapon`), `DefinitionTests`(짝·분류), `StaticDataValidationTests`(직업 기본값·범위·레시피), `StaticDataTransformerTests`(열 둘), `ShippedDataTests`(모든 장비의 요구, 기본 무기 5와 주 스탯), `RunSaveMapperTests`(25).

### 효과 어휘 (Slice D 44단계)

규칙은 `Docs/Design/02_Combat_System.md` §3·§4·§5(효과 타입 열, 틱의 차례)와 `04_Lobby_100Day_Economy.md` §3(틱·가시의 피로 없음)이 소유한다. 검토와 판정은 `Docs/Design/18_Effect_Vocabulary_Proposal.md`와 00_INDEX "Design/18의 판정". 수치는 `ItemData`의 효과 값, `BalanceData`의 `BleedDamagePerStack`·`RegenHealPerStack`. (흡혈은 2026-10-21 사용자 "흡혈은 삭제"로 뺐다.)

- Type: `EffectKind`에 `Bleed`·`Regen`·`Thorns`·`Cleanse`·`Stagger`·`Charge`, `PotionEffect.Cleanse`. `BattleUnit`의 `Bleed`·`Regen`(중첩)·`Thorns`(0.01 단위). 모두 전투 안의 상태라 저장하지 않는다(07).
- `Activate`: 중첩 효과(`ItemEffect.CountsStacks`)는 `ScaleStacks`, 나머지(경직·충전의 ms 포함)는 `Scale`로 스탯을 곱한다. 출혈·경직은 물리면 명중을 굴린다(`StatRules.CanMiss`). 피해 갈래의 끝: 피해 → 치명타의 피로 → 무기 적중 패시브 → **가시**(근접 무기이고 대상이 서 있으면 `ApplyDamage(공격자, 대상의 가시, 원인 thorns)`; 공격자가 쓰러지면 `Activate`를 그 자리에서 끝낸다).
- 틱: `Ticks`가 `Tick.Burn`·`Bleed`·`Regen`의 차례로 `TickUnits`를 아군·적에 돌린다. 출혈 틱은 `ApplyDamage(…, throughShield: true)`로 보호막을 건너고, 재생 틱은 `ApplyHeal`이다. `BattleEvent.IsTickCause`(화상·출혈·가시)는 `ApplyDamage`에서 피격의 피로를 막는다.
- `ApplyHeal`: 크기가 0보다 크면 출혈을 0으로 하고 `Cleansed`(A 0, B 멎은 중첩, 원인은 그 회복의 원인)를 남긴다. 빈사 탈출은 그대로다.
- `Cleanse`(아이템·포션): 화상·출혈에서 각각 N까지 뺀다. `Stagger`: 대상의 `Active && Cycles` 아이템의 `NextFireMs += N`. `Charge`: `NextFireMs = max(TimeMs + 1, NextFireMs − N)`, 발동한 아이템 자신은 건너뛴다. 모두 그 자리에서 끝나 상태·만료가 없다.
- 포션(46단계, Design/19): `PotionEffect.Regen`은 `AddRegen`(원인은 포션 id, Source 없음 — 로그의 원인 글은 포션 이름)이다. 정화 포션은 `Cleanse` 그대로. `TryUsePotion`의 갈래 넷.
- 이벤트: `BleedApplied`·`RegenApplied`·`ThornsGained`·`Cleansed`·`Staggered`·`Charged`; 원인 상수 `CauseBleed`·`CauseRegen`·`CauseThorns`(Source는 None). `Kill`이 세 상태를 0으로 한다.
- 화면: 카드의 효과 줄(`UiText.Effect`·`EffectValue`: 중첩, 초, 수), 전투 로그의 줄과 원인 글(`BattleLogText`). 유닛 위의 상태 표시와 연출은 목업 뒤(Architecture/12).
- 시뮬: `BattleStats`에 어휘의 줄(출혈·재생 중첩, 가시의 피해, 정화·경직·충전 수 — 쓰인 전투가 있을 때만 출력), `trace`의 상세. 정책은 그대로(분류로 놓는다).
- Test: `EffectVocabularyTests`(규칙마다, 틱의 차례, 정의·변환·포션의 검사, 어휘 전부를 쓴 전투의 로그 재현). 어휘를 쓰는 출고 아이템이 아직 없어 출고 데이터의 시뮬은 전후가 같다(08 §50).
- **2차, 일곱 상태**(Slice D 45단계, Docs/Design/02 §5 "일곱 상태", Design/18 §10): `EffectKind`의 `Freeze`·`Blind`·`Aim`·`Keen`·`Fury`·`Haste`·`Vulnerable`(`ItemEffect.IsState`), `BattleUnit`의 중첩 일곱(`StacksOf`·`SetStacks`). `AddState`가 쌓고 `StateApplied`(B = 종류)를 남긴다. `Tick.States`가 재생 다음에 유닛마다 −1(로그 없음). 끼는 자리: `CooldownOf`(피로 상태 % + 빙결 − 가속, `EffectiveCooldown`의 바닥 그대로), `RollHit`(공격자의 조준 − 암흑, 만분율), `RollCritical`(예리는 확률의 보너스에, 격분은 배수에), `Activate`의 피해 갈래(대상의 취약: 스탯·무기 피해 % 다음에 곱함). `Cleanse`는 `HarmfulStates`(빙결·암흑·취약)도 N씩(C에 합).
  **대상의 편 제한 없음**(2026-10-21 사용자): `ItemEffect`의 대상 검사를 지웠다. 엔진의 `Lands(owner, target, item, effect)` 하나가 "자기 편이면 언제나, 상대편이면 `CanMiss`일 때 `RollHit`"를 정하고(`StatRules.CanMiss`는 `ItemEffect.Hurts` 종류의 물리), 피해 갈래의 `foe`가 치명타·무기 적중 패시브·가시를 상대편으로 묶는다. 시뮬 `BattleStats`에 상태의 줄(파티가 건 것 / 파티에 걸린 것). Test `SecondVocabularyTests`.

## 전투: 결정론

전투 결과는 `BattleSetup`(시드 포함)과 입력 기록(`BattleInput` 목록)만의 함수다.

- 시간은 정수 밀리초다. `BattleEngine`은 다음 사건의 시각으로 건너뛴다. `AdvanceTo(t)`를 잘게 여러 번 부르든 한 번에 부르든 결과가 같다.
- 같은 시각의 순서는 코드 한 곳(`ProcessAutomaticEvents`)에 고정돼 있다: 틱(`Ticks`: 모든 유닛의 화상 -> 출혈 -> 재생 -> 일곱 상태의 감쇠, 각각 아군 먼저. 44·45단계) -> 아이템(아군, 유닛 순서, 칸 순서) -> 폭풍 틱.
  0ms의 시작은 생성자가 처리한다: `BattleStarted` -> 들어온 피로의 판정 -> 전투 시작 패시브(아군, 적) -> 전투 시작 아이템(아군, 적. 39단계).
  플레이어 입력은 `AdvanceTo`가 끝난 뒤의 현재 시각에 적용되므로, 그 시각의 자동 사건 뒤에 온다.
- 난수는 `Pcg32`뿐이다. 전투 스트림(사망 판정)과 입력 스트림(후퇴 판정)을 나눠, 후퇴 시도가 사망 판정을 바꾸지 않는다.
  **피해 스트림**(`RngStream.Damage`, 22단계)은 피해의 굴림과 치명타를 뽑는다: 피해 효과마다 공격에 한 번 `NextInt(최소, 최대)`, 그 뒤 맞는 대상마다 치명타의 `NextInt(100)`(무기의 확률이 0이면 뽑지 않음).
- 수치는 정수다. HP·피해·회복·보호막·폭풍은 0.01을 1로 센다(`F1.Data.Centi`, 22단계). 효과 크기, 쿨다운 수정의 반올림은 한 함수에서만 계산한다.
- 피해의 차례(22단계, `Activate`): 굴림 → 무기면 별의 피해(+) → 대상마다 치명타(`× CritDamagePercent`%, `CriticalHit` 이벤트를 `Damaged` 바로 앞에) → 무기 피해 % 패시브 → `ApplyDamage`.
  치명타의 피로는 `ApplyDamage` 뒤에 붙인다: 맞은 용병 `CritFatigueTaken`, 그 동료 `…TakenAllies`, 넣은 용병 `CritFatigueDealt`(덜어 냄), 그 동료 `…DealtAllies`(원인 `crit_taken`·`ally_crit_taken`·`crit_dealt`·`ally_crit_dealt`).
- 피격의 최소(22단계, `ApplyDamage`): 보호막을 뚫은 피해가 `HitMinDamage` 미만이고 그 피해가 HP를 0으로 만들지 않으면 HP만 줄고 피격으로 세지 않는다(피로·빈사의 사망 판정 없음). 화상 틱은 중첩 × `BurnDamagePerStack`이다.
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
       -> 후보 난수 = Derive(런 시드, "recruit", 원정 순번) (26단계: 역마차의 후보. 34단계 스탯의 편차, 37단계 타고난 특성도)
       -> 얻은 특성 난수 = Derive(런 시드, "trait:" + 용병 id, 런의 원정 수) (38단계: 정산의 계기, `RngStream.Trait`)
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
  새 어휘가 필요하면 Enum, Definition 생성자의 검증, `BattleEngine`의 처리, Test를 함께 넣는다. 값이 0.01 단위인지(`05_STATIC_DATA.md`)와 화면의 글(`UiText`, `12_UI.md` "수의 표시")도 함께 정한다.
- Definition 생성자가 거르는 조합(`PassiveSpec`, `ItemEffect`)은 `BattleEngine`이 구현한 조합과 같아야 한다.

## 시뮬 실행기 (`Tools/Sim`)

```bash
dotnet run --project Tools/Sim -- battle     --group <id> --party a,b,c --policy none|balanced|safe --runs n --seed n
dotnet run --project Tools/Sim -- expedition --dungeon <id> --party a,b,c --policy ... --runs n --seed n
dotnet run --project Tools/Sim -- run        --policy ... --runs n --seed n --rest-above 60 --bench 2 --treat-above 100
dotnet run --project Tools/Sim -- formations --dungeon <id> --party a,b,c --policy ... --runs n --seed n
dotnet run --project Tools/Sim -- trace      --group <id> --party a,b,c --policy ... --seed n
dotnet run --project Tools/Sim -- map        --dungeon <id> --seed n
dotnet run --project Tools/Sim -- items
```

- 파티는 용병 id나 직업 id로 적는다. 직업 id는 아직 파티에 없는 그 직업의 시작 용병, 없으면(36단계: 방랑자는 역마차에서만 온다) 이름 풀의 첫 이름을 그 직업으로 쓴다. 기본 파티는 `warrior,ranger,priest,mage`. 모두에게 열 번호를 붙이면 그 열에 세운다(`rowan:1,kai:2,ella:3,mira:4`).
  붙이지 않으면 직업의 권장 열 순서로 앞에서부터 세운다.
- `formations`는 같은 파티를 가능한 모든 순서로 세워 같은 시드로 돌린다. 자리가 결과를 얼마나 바꾸는지 본다.
- `items`는 아이템마다 일반 단계의 피해 범위, 한 대상의 평균, 맞히는 수, 칸·초당 피해를 적는다(22단계의 크기 규칙). `trace`는 0.01 단위의 값(HP, 피해, 회복, 보호막, 폭풍)을 수로 적는다.
- `map`은 원정 시드 하나의 맵을 노드마다 한 줄로 적는다(id, 층, 열, 종류, 무리, 다음 노드). `expedition`의 보고는 원정마다 정예·야영지를 들른 수, 전투 시간(x1),
  합치기(전리품과 상점에서 바로 합친 것과 정리의 합치기)와 정비의 수, 끝날 때 보드의 단계별 아이템 수, 붕괴(고통·각성)와 쓰러짐의 수와 붕괴가 난 원정의 비율, 전리품(떨어진 수, 주운 수), 상점(들른 수, 산 것, 새로고침, 받은·쓴·남은 코인)을 함께 적는다.
  정책의 합치기와 야영지의 선택은 `Docs/Design/08_Simulation_Report.md` §10·§11, 상점의 선택(`SimPolicy.ShopAt`: 포션 둘까지, 합쳐지는 것, 자리가 맞는 것을 사고, 쓸 것이 없으면 둘까지 새로고침)은 §13, 전투의 포션(`UsePotionIfNeeded`, 46단계: 치유 → 빈사의 방벽 → 치유가 없을 때의 재생 → 해로운 중첩이 넷 이상인 구성원의 정화)과 자기 버프 아이템의 자리(`MemberWithRoomFor`, 46단계: 자신에게 예리·격분을 거는 아이템은 치명타 확률이 가장 높은 무기를 든 구성원에게)는 §52,
  전리품(`SimPolicy.PickLoot`: 드랍마다 합쳐지는 것, 자리가 맞는 구성원의 빈 칸, 인벤토리의 차례로 줍고 나머지는 둔다)은 §14, 격자의 자리(`ItemBoard.FindRoom`, 가방은 가방 칸이 가장 적은 용병에게. 밀어내기·가방 옮기기는 하지 않는다)와 가방의 수는 §15.

- `run`(25·26단계)은 런 하나를 끝까지 돌린다. 로비 정책(시뮬 도구이지 규칙이 아니다): 로스터가 `PartySize + bench` 아래이고 금이 되면 첫 후보를 고용(해고는 하지 않음), 파티는 피로가 낮은 넷을 직업의 권장 열 차례로,
  파티원의 피로가 `treat-above` 이상이면 금이 되는 동안 치료, 그래도 파티의 피로 평균이 `rest-above` 이상이면 쉬기, 던전은 최종 던전이 열렸으면 그것, 기한을 넘기면 최종 결전. 보고는 승리·패배의 까닭·일수·원정·쉬기·전사자·해금과 금(번 것, 고용·치료에 쓴 것, 남은 것, 끝의 로스터). `Docs/Design/08_Simulation_Report.md` §23·§24.
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
  스탯(32단계: 척도, 세기, 체력의 HP, 상태와 저장)은 위 "스탯"의 Test가 고정한다.
  런의 끝(해금, 최종 결전, 승패)은 `RunEndTests`·`RunEndFlowTests`, 금·후보·고용·해고·치료(26단계)는 `LobbyEconomyTests`·`LobbyEconomyFlowTests`, 레벨과 경험치(27단계: 사다리, 오르기, HP 보너스, 정산의 경험치)는 `LevelRulesTests`가 고정한다.
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

- Deferred: 유물, 전직(Slice E 이후. 레벨·경험치·HP 보너스는 27단계에, 스탯은 Slice D 32단계부터 들어왔다), 일당과 로비 상점(Slice D. 금·고용·시설은 26단계에, 지역 코인은 17단계에 들어왔다), 나머지 지역 속성과 노드 종류(이벤트·보물), 최종 보스는 28단계에 들어왔다(위 "최종 보스의 페이즈").
- Forbidden: Domain에서 Unity/Managers/Save DTO 참조, 결과 계산에 부동소수점이나 시드 밖 난수 사용, 표현 계층이 상태를 바꾸기,
  규칙 없이 추가한 효과 어휘, 시뮬 통계를 Test 통과 조건으로 쓰기.
