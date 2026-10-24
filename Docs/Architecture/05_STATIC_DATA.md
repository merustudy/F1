# 05. Static Data

CSV, 변환, Generated JSON, `DataManager`를 소유한다. 열의 뜻과 값은 소유하지 않는다(기획: `Docs/Design`, 값: CSV).

## Pipeline

```text
CSV (사람이 편집, Source of Truth)
-> StaticDataTransformer (검증 + 변환. Unity Editor와 Tools/Sim 양쪽에서 같은 코드가 돈다)
-> JSON (Generated, Git 관리, 사람이 편집하지 않음)
-> Addressables (data/app/<name>)
-> ResourceManager (TextAsset Load)
-> DataManager (StaticDataLoader로 Deserialize + 재검증)
-> StaticData (Dictionary<string, T>)
```

- Runtime은 CSV를 Parse하지 않는다. 게임과 시뮬은 같은 Generated JSON을 같은 `StaticDataLoader`로 읽는다.
- Static Data를 ScriptableObject 중심으로 대체하지 않는다.
- Static Data, Runtime State, Save DTO는 서로 다른 Type이고 **문자열 id**로만 연결한다.

## 식별자

- 모든 Definition의 기본 키는 `Id` 열이다. 영어 snake_case 문자열(`^[a-z][a-z0-9]*(_[a-z0-9]+)*$`)이고 Definition 안에서 유일하다.
- 정수 Id를 따로 두지 않는다. 다른 Definition을 가리킬 때도, Save가 가리킬 때도 이 문자열을 쓴다.
- 조회는 `DataTable<T>`(안에서 `Dictionary<string, T>`)다. 없는 id를 조회하면 예외다. 조용한 기본값이 없다.
- 규칙이 Definition을 순회할 때는 항상 id 순서(`DataTable.Ordered`)로 한다. Dictionary의 순서에 결과가 달려서는 안 된다.

## 물리 경로

```text
Assets/@Data
├─ Source/<Definition>Data.csv
└─ Generated/<Definition>Data.json
```

수명은 전부 앱 수명이다(`data/app/<name>`, Boot에서 Load). 수명이 다른 Data가 실제로 생기면 그때 나눈다.
파일 목록은 `F1.Data.StaticDataFiles`가 가진다. 변환, Load, Addressables Entry가 모두 이 목록을 읽는다.

## Definition 단위

- Definition 하나 = CSV 하나 = Type 하나 = JSON 하나. 거대한 단일 파일을 만들지 않는다.
- Slice 범위 안의, 규칙이 확정된 Definition만 만든다.
- Definition Type은 불변이고 **생성자가 자기 규칙을 검증**한다. Transformer와 Loader는 실제 Type을 만들어 그 규칙을 그대로 건다.
- 아이템과 유물은 서로 다른 Definition, 다른 CSV, 다른 Type이다. 공용 Type으로 합치지 않는다.

## 코드 위치

| 위치 | 내용 | Unity 의존 |
|---|---|---|
| `Assets/@Scripts/Data` | Definition, `StaticDataJson`, `StaticData`, `StaticDataLoader`, `StaticDataFiles`, `LocalizedText`, `LocalePolicy` | 없음 |
| `Assets/@Scripts/Editor/Data/Csv` | `CsvTable`, `CsvRow` | 없음 |
| `Assets/@Scripts/Editor/Data/Transform` | `StaticDataTransformer`, Definition별 Mapper(`DefinitionMappers.cs`), `StaticDataFileStore` | 없음 |
| `Assets/@Scripts/Editor/Data/DataTransformMenu.cs` | Menu와 Batch 진입점 | 있음 |
| `Assets/@Scripts/Core/Data/DataManager.cs` | `ResourceManager`로 Load | 있음 |
| `Tools/Sim` | 위의 Unity 의존 없는 폴더를 그대로 컴파일하는 .NET 콘솔 | 없음 |

Mapper는 Definition마다 Type 하나다. 파일이 커지면 Definition별 파일로 나눈다. Reflection으로 Header를 Field에 자동 연결하지 않는다.
Definition은 JSON으로 직접 직렬화한다(Property 순서는 Attribute로 고정). Load는 Definition의 생성자를 거치므로 같은 검증이 다시 걸린다.

## CSV 형식

```csv
Id,Name.ko-KR,Name.en-US,Level,MaxHp,Items,Figure,FigureScale,Phases
goblin_shaman,고블린 주술사,Goblin Shaman,5,60,lantern_staff:10+mending_chant:12,unit/enemy/goblin-shaman,100,
```

- UTF-8, Header Row 필수. **Header 이름으로 Mapping**하고 열 순서에 의존하지 않는다.
- quoted field, escaped quote(`""`), 필드 안 개행을 처리하는 Parser를 쓴다. `Split(',')` 금지.
- Localization 열은 `<Field>.<LocaleCode>`다. Locale 집합은 `LocalePolicy.SupportedCodes`와 정확히 같아야 한다.
- 다른 Definition 참조는 그 Definition의 `Id` 문자열이다.
- 목록 값은 `+`로 잇는다. 적의 아이템 목록은 `item_id:세기 퍼센트`를 `+`로 잇는다(22단계: 그 전에는 `item_id:grade`).
  순서에 뜻이 있는 목록은 적힌 순서를 그대로 쓴다(적 무리의 `Enemies`는 앞에서부터 서는 순서다).
- 줄에서 세는 자리(아이템의 `Rows`, 직업의 `PassiveRows`)는 `front:N`, `back:N`, `all`로 적는다(`RowSpan`. 구문은 그 Type이 가진다).
- 다른 열의 값에 따라 비워 두는 칸은 Mapper가 Optional로 읽고, 채워야 하는지는 Definition 생성자가 검증한다
  (효과의 `Reach`는 앞이나 뒤에서 세는 타깃에만 있다).
- 그림을 가리키는 열(`JobData`와 `EnemyData`의 `Figure`, `DungeonData`의 `Background`, `ItemData`와 `PotionData`의 `Icon`)에는 그 그림의 Logical Address를 적는다(`04_RESOURCES_ADDRESSABLES.md`).
  공격·피격 자세는 열이 없다: `Figure`에서 주소가 따라 나온다(`JobData`·`EnemyData`의 `AttackPose`, `HitPose`. `ArtAddress`).
- `ItemData.Category`는 아이템의 분류 다섯이다: `Weapon`(무기 장비), `Armor`(방어 장비), `Attack`(공격 아이템), `Support`(지원 아이템: 회복·버프), `Other`(기타 아이템) (Design/02 §4).
  규칙은 `Weapon`인지만 본다(무기 패시브는 무기 장비의 피해에만). 전투 화면은 분류로 주인을 움직인다(`12_UI.md` "연출").
  비워 두면 그 유닛이나 던전, 아이템은 그림이 없다. Data는 이 값을 글자 그대로 나른다. Address의 형식과 파일이 맞는지는 Editor Setup이 검사한다(`13_ART_PIPELINE.md`).
- 48단계(2026-10-22, Design/04 §3): `FatigueStateData.csv`의 선택 열 여섯 `HitPercent`·`DodgePercent`·`CritPercent`(%p, −100~100), `DamagePercent`·`FatigueGainPercent`(%, −100~200), `AllyFatigueAtStart`(±N, −100~100) — 열이 없거나 비면 0. 상태는 아홉 수 가운데 하나는 바꿔야 한다. `BalanceData`의 `AfflictionRecovery`(정신 붕괴가 풀리는 피로, `FatigueBreakdown` 아래)와 `OverworkCooldownPercent`(과로의 쿨다운 %, 0~100).
- 49단계(2026-10-22, Design/26·03 §5): `BalanceData`의 `DropOwnedPercent`(같은 아이템 드랍의 %, 0~100). 데이터의 값만 바뀐 것(광산 19층과 층의 열, 무리 범위, `DeepItems`, 드랍 2·3, 코인·금·경험치, `DogGraceMs` 0, 진입 피로 3)은 열을 바꾸지 않는다.
- `BalanceData`의 `SellPercent`(Round 74, 2026-10-17)는 상점이 파티의 아이템을 사 주는 값의 비율(살 값의 %)이다. `CoinsPerGold`(2026-10-17)는 클리어한 원정의 지역 코인(남은 것 + 아이템의 파는 값)이 금 1이 되는 코인 수다.
- `ItemData`의 선택 열 `DropWeight`(2026-10-17. 헤더가 없거나 비면 0)는 이긴 전투가 적의 아이템 대신 떨어뜨리는 **일반 아이템**의 가중치다. 유니크는 0이어야 한다. `DungeonData`의 선택 열 `GeneralDropChancePercent`(0..100, 기본 0)는 그 던전의 드랍 자리마다 일반 아이템이 될 확률이다. 직업의 기본 무기는 `ShopWeight`·`Price`가 있어도 그 직업이 파티에 있을 때만 상점에 선다(규칙은 `ExpeditionRules`). `ItemData`의 `Price`는 상점에서 살 값이자 팔 값의 밑이고, `ShopWeight`가 0이면 값이 있어도 상점에 나오지 않는다(적의 아이템).
- `DungeonData`의 `EnemyHpPerFloorPercent`는 1층보다 한 층 깊어질 때마다 적의 최대 HP에 **곱하는** 비율이다(복리, 2026-10-17. 0이면 자람 없음. `08_GAMEPLAY_DOMAIN.md` "깊은 층의 적"). `EnemyPowerPerFloorPercent`는 층마다 더하는 아이템 세기 %다.
- `EnemyData`의 `FigureScale`은 그림을 그리는 크기의 백분율이다(100이 보통, 보스는 더 크게. 50..300). 표현만 바꾸고 규칙과 자리는 바꾸지 않는다(`12_UI.md` "유닛의 그림").
- `EnemyData`의 `Phases`(28단계, 선택 열. 헤더가 없거나 비면 페이즈 없음)는 `문턱%=item_id:세기+item_id:세기|문턱%=…`다: 적의 HP가 그 %에 닿으면 그 묶음으로 바꿔 든다(`EnemyPhase`. Design/02 §9). 문턱은 1..99에서 내려가는 차례, 묶음은 아이템 하나 이상이고 높이 합은 틀의 높이 이하, 아이템은 있는 것이어야 한다(Dataset 검증).
- `EnemyData`의 `DeepItems`(2026-10-20, 선택 열. 헤더가 없거나 비면 묶음 없음)는 `층=item_id:세기+item_id:세기|층=…`다: 지도 층이 그 층 이상이면 그 묶음을 기본 아이템 뒤에 **더해** 든다(`EnemyDepthItems`, 누적. Design/03 §4). 층은 2 이상에서 오르는 차례(`EnemyData`의 검사), 묶음은 아이템 하나 이상 여덟 이하, 아이템은 있는 것이어야 하고(`StaticData.CheckReferences`), 기본 아이템(과 페이즈마다의 아이템)에 그 층까지의 묶음을 더한 것이 틀 안에 들어가야 한다(`CheckEnemyBag`). 어느 층에서 무엇을 드는지는 `EnemyData.ItemsAt(층)`·`ItemsAt(페이즈, 층)`이 답한다(`08_GAMEPLAY_DOMAIN.md` "깊은 층의 적"). 매퍼는 `CsvRow.DepthList`.
  `BalanceData.FinalBossLevel`은 최종 보스의 것이다: 그 레벨은 최종 던전(`DungeonData.IsFinal`)의 보스 무리에 선 적만 가질 수 있고, 그보다 높은 레벨의 적은 없다(Dataset 검증. CLAUDE.md §5-7).
- `ItemData.csv`와 `PotionData.csv`의 `Price`는 상점의 값이다(일반 기준의 정수. 0이면 상점에 나오지 않는다. 17단계). `ShopWeight`는 상점에 나올 상대 확률이다(18단계에 `RewardWeight`에서 이름을 바꿨다: 전투는 적이 든 것을 떨어뜨린다). `DungeonData.csv`의 `ShopMinFloor`·`ShopChancePercent`는 정예·야영지의 열과 같은 방식이다.
- `DungeonData.csv`의 선택 열(열이 없거나 비면 기본값): `IsFinal`(최종 던전, 하나까지)·`UnlockDungeonId`·`UnlockClears`(해금. Slice C 25단계), `MapX`·`MapY`(원정 지도에서 표식이 서는 자리, 그림 폭·높이의 %. Round 71 E2). 선택 열은 `CsvRow.OptionalBool`·`OptionalId`·`OptionalInt`로 읽고 Mapper가 `RequireHeaders`의 둘째 인자로 등록한다. `BalanceData`의 `DeathLimit`도 25단계.
- `ItemData.csv`의 `Width`·`Height`는 격자에서 아이템이 차지하는 가로·세로 칸이다(19단계. 한 변 1..`BoardFrame.MaxSide` 3. 그 전의 `Size`를 대신했다). `JobData.csv`에는 칸 수가 없다(보드는 가방이 만든다).
- `ItemData.csv`의 20단계 열(`Docs/Design/02_Combat_System.md` §4의 ★): `Melee`(근접 무기. `true`/`false`, `Weapon`만 `true`), `Stars`(별 칸: 돌리지 않은 모양의 왼쪽 위 칸에서 잰 `x:y`를 `+`로 이음,
  비우면 별이 없다), `StarDamage`(별에 놓인 근접 무기의 피해 +N, 일반 기준. 비우면 0). 정의(`ItemData`)의 검사: 별 칸은 모양 밖이고 모양에서 한 칸 안(대각선 포함)이며 겹치지 않는다,
  별이 있으면 `StarDamage` 1 이상이고 없으면 0이다. **효과가 없는 아이템**(Effect1을 모두 비움)은 발동하지 않는다(`IsPassive`): 별이 있어야 하고 `CooldownMs`는 0이다. 효과가 있으면 지금처럼 `CooldownMs` 100 이상.
  별 칸은 `StarSquare`(X·Y)의 목록으로 Generated JSON에 들어간다.
- `ItemData.csv`의 39단계 선택 열(`Docs/Design/02_Combat_System.md` §4 "방어구"): **`Trigger`**(`ItemTrigger`: 비면 `Cooldown`, `BattleStart`면 전투 시작 때 한 번 — 효과가 있어야 하고 `CooldownMs`는 0), **`CritTakenPercent`**(정수 %p 0..100, 비면 0: 그 주인을 노린 무기의 치명타 확률에서 뺌 — `Armor`만 0보다 큼). 정의(`ItemData`)가 둘을 검사한다. `ItemData.Cycles`는 쿨다운으로 도는 아이템(효과가 있고 `Trigger`가 `Cooldown`)이다. 첫 둘은 철 갑옷(`iron_armor`)·철 투구(`iron_helm`)이고 `Icon`이 비어 있다(그림이 없으면 조각에 이름).
  최대 체력 효과(`MaxHp`, Design/35)는 `BattleStart` 아이템에만, 대상은 **`Self`만**이다(56단계, Design/38 판정 1: 원정의 화면이 주인의 보드만 읽어 그 값을 보인다). 정의가 둘을 검사한다.
- `BagData.csv`(19단계)는 가방이다: `Width`·`Height`(1..3), `Start`(시작 가방: 정확히 하나, 값·가중치 0), `Price`(상점의 값, 0이면 팔지 않음), `ShopWeight`(상점에 나올 상대 확률), `LootWeight`(정예의 가방 전리품으로 뽑힐 상대 확률), `Icon`(선택, Round 79: 가방의 그림 `bag/<kebab-id>`. 비면 우물).
  틀의 크기(`BoardFrame` 3×8)는 데이터가 아니라 구조 상수다(저장 파일과 화면의 자리가 그것에 묶인다). Dataset 검증: 시작 가방이 정확히 하나, 직업의 기본 무기가 시작 가방에 돌리지 않고 들어감, 적 아이템의 높이 합이 틀의 높이(8) 이하.
- `RecipeData.csv`(23단계)는 담금질의 레시피다: `Result`(유니크 아이템의 id), `BaseItem`·`BaseTier`, `Catalyst`·`CatalystTier`(단계는 `ItemTier`의 이름: `Common`·`Tier1`·`Tier2`·`Tier3`). 이름 열이 없는 첫 정의다(`RecipeMapper`는 `Id`부터 직접 요구한다). 검사: 결과·바탕·촉매가 아이템에 있고, 결과는 `Unique`, 촉매는 ★이 있다.
  `ItemData.csv`의 `Unique` 열(선택, 비면 false)은 유니크 아이템이다: `ShopWeight`·`Price`가 0이어야 한다.
- `MercenaryData.csv`(26단계)의 `Starting`(참/거짓): 참이면 시작 로스터(`JobId` 필수), 거짓이면 역마차의 이름 풀(`JobId` 비움 — 후보를 뽑을 때 직업을 굴린다). `StaticData.StartingMercenaries`·`RecruitPool`.
- `BuildingData.csv`(30단계)는 마을 건물의 레벨 사다리다: `Id`(`BuildingId`의 셋: `barracks`·`inn`·`coach` — 성문은 건물이 아니다(2026-10-18 사용자: 업그레이드 없음). 이름 열이 없는 정의, 이름표는 UI 문구), `Level`(건물마다 1부터 빈틈없이), `Price`·`Days`(업그레이드의 금과 공사 일수. 1레벨은 0, 그 뒤는 0보다 큼), `Bonus`(그 레벨이 `BalanceData`의 기본값에 더하는 것: 막사 `RosterLimit`, 역마차 `RecruitCount`, 쉼터 `TreatmentRelief`. 1레벨은 0, 내려가지 않음). `StaticData.Buildings`·`MaxBuildingLevel`·`BuildingAt`이 검사한 사다리를 준다(`BuildingMapper`).
- `LevelData.csv`(27단계)는 레벨의 사다리다: `Level`(1부터 빈틈없이), `ExpToNext`(다음 레벨까지의 경험치. 마지막 레벨만 0). `MaxHpBonusPercent`는 Slice D 34단계에 지웠다(레벨은 스탯의 성장으로 HP에 닿는다). `Id`와 이름 열이 없다(`LevelMapper`). `StaticData.Levels`·`MaxLevel`·`LevelAt`이 검사한 사다리를 준다.
- **스탯**(Slice D 32단계, `Docs/Design/05_Mercenary_Growth.md` §2): `JobData.csv`의 **`Str`·`Dex`·`Con`·`Int`·`Spi`·`End`**(정수, 필수 열)는 직업의 기본값이다(`StatBlock`. 범위 `StatMin`~`StatMax`는 Dataset 검증). `ItemData.csv`의 선택 열 **`ScalesWith`**(비면 없음)는 그 아이템의 세기를 키우는 스탯이다: `Str`·`Dex`·`Int`·`Spi`(`Stat`의 이름. 기획의 `str`·`dex`·`int`·`spi`)만 되고, 효과가 없는 아이템은 비워 둔다.
  `BalanceData`의 `StatBaseline`(기준값)·`StatMin`·`StatMax`(범위)·`StatPowerPercentPerPoint`(세기 %/pt)·`HpPercentPerConPoint`(체력의 HP %/pt): `StatMin` ≤ `StatBaseline` ≤ `StatMax`이고, 가장 낮은 스탯의 세기·HP %가 1 이상이어야 한다(검증).
  → 41단계(2026-10-20 스탯 개정, `Docs/Design/17_Stat_Revision_Proposal.md`): `JobData.csv`의 **`MaxHp` 열은 없고**(체력이 HP를 준다) 스탯 열은 **`Str`·`Dex`·`Con`·`Int`·`Wis`·`End`**·**`GrowthWis`**(옛 `Spi`·`GrowthSpi`), 출고 값은 1~8. `ItemData.csv`의 `ScalesWith`는 없고 선택 열 **`School`**(`Physical`·`Magic`: 무기 장비·공격 아이템은 비우면 물리, 방어 장비는 선택, 지원·기타에 적으면 거절)과 **`CritDamagePercent`**(0..500, 치명타 확률이 있는 물리 무기만)가 있다 — 세기의 축(`ItemData.ScalesWith`)과 쿨타임의 축(`QuickenedBy`)은 분류와 학파에서 파생한다(`08_GAMEPLAY_DOMAIN.md` "스탯"). `TraitData.csv`의 **`CritDamagePercent`**(0..500). `BalanceData`의 키: `PowerBaseline`·`FatigueBaseline`(둘 다 `StatMin`~`StatMax` 안), `CooldownPercentPerPoint`(가장 높은 스탯의 쿨타임 ≥ 1%), `HpPerConPointLow`·`Mid`·`High`(0.01 단위)와 `HpBandLow` < `HpBandMid`, `HitPerDexPoint`(옛 `HitPerStatPoint`)·`CritPerWisPoint`(0.01 단위), `DogDeathPercentPerEndPoint`(옛 `…ConPoint`), `VirtuePerEndPoint`(옛 `VirtuePerSpiPoint`), `PhysicalCritDamagePercent`·`MagicCritDamagePercent`(옛 `CritDamagePercent`); `HpPercentPerConPoint`는 없다.
  → 42단계(장비 요구 스탯, `Docs/Design/02_Combat_System.md` §4 "장비 요구 스탯"): `ItemData.csv`의 선택 열 **`ReqStat`**(`Stat`의 이름 `Str`·`Dex`·`Con`·`Int`·`Wis`·`End`)·**`ReqValue`**(정수). 정의(`ItemData`)의 검사: 둘은 함께 있거나 함께 비고(`HasRequirement`), 무기 장비·방어 장비만 가진다(공격·지원·기타에 적으면 거절). Dataset 검증(`StaticData.CheckReferences`): `ReqValue`는 `StatMin`~`StatMax` 안,
  **직업의 기본값이 자기 기본 무기의 요구를 채움**(스탯이 없는 직업은 `StatBaseline`으로 센다 — 모든 용병이 기본 무기를 들어야 하므로), 레시피의 결과는 바탕보다 많이(다른 스탯을) 요구하지 못함(담금질은 결과를 바탕의 자리에 묻지 않고 넣는다). 매퍼는 `OptionalEnum<Stat>`·`OptionalInt`. 출고 데이터는 모든 무기·방어 장비가 요구를 가진다(`ShippedDataTests`).
  → 44단계(효과 어휘, `Docs/Design/18_Effect_Vocabulary_Proposal.md`, 02 §4·§5): `Effect*Kind`에 **`Bleed`·`Regen`·`Thorns`·`Cleanse`·`Stagger`·`Charge`**. 값(`Effect*Value`)은 화상·출혈·재생·정화가 중첩 수(정수), 경직·충전이 밀리초(정수), 가시가 0.01 단위(`ItemEffect.CountsWhole`·`CountsStacks`가 가른다). ~~정의(`ItemEffect`)의 검사: 해로운 효과는 적 대상, 이로운 효과는 아군 대상.~~ → 45단계(2026-10-21 사용자): **대상의 편 검사는 없다**(어느 종류든 어느 대상이든). 45단계의 효과 종류 **`Freeze`·`Blind`·`Aim`·`Keen`·`Fury`·`Haste`·`Vulnerable`**(중첩, 정수 — `ItemEffect.IsState`; 해로운 종류는 `ItemEffect.Hurts`)와 `BalanceData`의 정수 Key 일곱 `FreezePercentPerStack`·`BlindPercentPerStack`·`AimPercentPerStack`·`KeenPercentPerStack`·`FuryPercentPerStack`(0..500)·`HastePercentPerStack`·`VulnerablePercentPerStack`(0..100). `PotionData.Effect`에 `Cleanse`(그때 `Magnitude`는 중첩 수, 정수 — `PotionMapper`). `BalanceData`의 0.01 단위 Key에 **`BleedDamagePerStack`·`RegenHealPerStack`**(1 이상).
  → 46단계(아이템 목록, `Docs/Design/19_Item_Catalog_Proposal.md`, 02 §4·§10, 03 §4·§5): 열 변경 없음 — 데이터 행만. `PotionData.Effect`에 **`Regen`**(그때도 `Magnitude`는 중첩 수, 정수 — `PotionMapper`는 `Cleanse`·`Regen`을 `Int`로 읽는다). 출고 데이터: 아이템 56(유니크 5, 적만 드는 것 11, 1×3의 깃발), 레시피 5(기본 무기 다섯의 3성 + 숫돌 3성), 포션 4, 가방 5(시작 하나). 새 아이템·포션·가방의 `Icon`은 Round 92(같은 날)에 채웠다(`item/<kebab-id>`·`bag/<kebab-id>`·`potion/<kebab-id>`, 유니크 넷은 `-glow`도). 그 전에는 비어 있었고 조각과 포션 칸에 이름이 섰다(`12_UI.md`: 아이콘이 없는 물건의 길은 남아 있다). `ShippedDataTests`가 수·묶음·레시피·그림을 단언한다.
  37단계(`Docs/Design/05_Mercenary_Growth.md` §6): 새 정의 **`TraitData.csv`**(`TraitData`) — `Id`, `Name.*`, `Kind`(`Innate`·`Acquired`), `Polarity`(`Positive`·`Negative`), `Weight`(정수 0..1000, 후보가 타고난 특성을 뽑는 가중치), `Excludes`(선택 id: 함께 갖지 못하는 반대 특성, 어느 쪽이 적어도 막힘 — `TraitData.Clashes`), 효과 열 일곱 `HitPercent`·`DodgePercent`·`CritPercent`·`DogDeathPercent`·`VirtuePercent`(%p)·`FatiguePercent`·`MaxHpPercent`(%)(정수 −100..100, **빈칸은 0**, 하나는 0이 아니어야 함). `StaticData.Traits`·`InnateTraits`(타고난 것 가운데 가중치 0 초과, id 차례). `MercenaryData.csv`의 필수 열 **`Traits`**(`+`로 이은 id, 시작 로스터만 — 이름 풀의 행은 비움, Dataset 검증: 있는 특성·두 번 없음·충돌 없음). `JobData.csv`의 필수 열 **`PassiveName.*`**(직업 패시브를 특성으로 보일 때의 짧은 이름. 패시브가 없으면 비움). `BalanceData`의 **`RecruitTraitWeight0`·`RecruitTraitWeight1`·`RecruitTraitWeight2`**(0..1000, 합이 0보다 큼: 후보가 타고나는 특성 0·1·2개의 가중치). 직업 특성의 데이터는 `JobData`의 패시브 열 그대로다(Design/16 §8의 권장안). 아이템 ≠ 유물처럼 특성도 따로인 Definition이다.
  38단계: `TraitData.csv`의 필수 열 **`Source`**(얻은 특성을 남기는 계기 `TraitSource` — `Clear`·`Retreat`·`DeathsDoor`·`Affliction`·`Virtue`; 얻은 특성은 반드시 있고 타고난 특성은 빈칸이어야 함 — `TraitData`의 생성자가 막는다). `Weight`는 얻은 특성에서는 같은 계기의 특성 가운데 고르는 가중치다. 얻은 일곱(05 §6). `BalanceData`의 **`TraitChanceClearPercent`·`TraitChanceRetreatPercent`·`TraitChanceDeathsDoorPercent`·`TraitChanceAfflictionPercent`·`TraitChanceVirtuePercent`**(0..100: 계기마다 특성을 남길 확률).
  36단계: `JobData.csv`의 행은 다섯(`warrior`·`viking`·`ranger`·`mage`·`priest`, `Docs/Design/05_Mercenary_Growth.md` §1)이고 옛 여섯의 id는 없다. `PassiveEffect`에 **`CritChancePercent`**(정수 %p, `Always`·`None`·`Self`만: `PassiveSpec`의 조합 검사). `MercenaryData.csv`의 시작 로스터는 다섯(카이 `warrior`, 세드릭 없음). 장궁(`longbow`)은 방랑자의 기본 무기라 다른 기본 무기처럼 `ShopWeight` 3·`DropWeight` 0이고, 2×2 그대로다(기본 무기 3×1의 예외 둘째, 전투도끼 3×2와 함께).
  47단계(직업군 확대, `Docs/Design/05_Mercenary_Growth.md` §1, Design/20): `JobData.csv`의 행은 **여덟**(`warrior`·`viking`·`rogue`·`archer`·`mage`·`occultist`·`priest`·`minstrel`. `ranger`는 `archer`로: 저장 26이 옛 id를 옮긴다). `PassiveEffect`에 **`DodgePercent`**(정수 %p, `Always`·`None`·`Self`만: `PassiveSpec`이 거른다). `ItemData.csv`에 기본 무기 둘 `cursed_tome`(저주의 서)·`lute`(낡은 류트) — 열 변경 없음, `Icon`은 Round 93까지 비움(화면은 이름). 단검 `dagger`의 `ShopWeight` 10 → 3, `DropWeight` 10 → 0(기본 무기의 규칙). 데이터 검사는 그대로(기본 무기가 시작 배낭에 들고 직업 기본값이 요구를 채움).
  34단계: `JobData.csv`의 **`GrowthStr`·`GrowthDex`·`GrowthCon`·`GrowthInt`·`GrowthSpi`·`GrowthEnd`**(정수 %, 필수 열, 0..200 — `JobData.Growth`), `BalanceData`의 `RecruitStatSpread`(0..10). `LevelData.csv`에는 `MaxHpBonusPercent`가 없다(`Level`·`ExpToNext`).
  33단계: `HitBasePercent`·`HitMinPercent`·`HitMaxPercent`(정수 %, `HitMinPercent` ≤ `HitBasePercent` ≤ `HitMaxPercent`), `HitPerStatPoint`·`DodgePerDexPoint`·`CritPerDexPoint`(**0.01 단위**: "1"·"0.5" — 코드는 만분율로 쓴다), `DogDeathPercentPerConPoint`·`VirtuePerSpiPoint`·`FatiguePercentPerEndPoint`(정수 %p·%). `EnemyData.csv`의 선택 열 **`Accuracy`·`Dodge`**(정수 %p, 비면 0, −100..100)는 스탯이 없는 적의 명중과 회피다.
- **22단계의 수**(`Docs/Design/02_Combat_System.md` §4·§13): HP·피해·회복·보호막·폭풍은 **0.01 단위**다. CSV는 소수 둘째 자리까지 적고("38", "5.45") Generated JSON과 코드는 0.01을 1로 세는 정수다(`Centi`: 38 → 3800).
  - `ItemData.csv`의 효과 값은 `Effect1Value`·`Effect2Value`(그 전의 `…Power` 퍼센트를 대신함): 피해는 "최소-최대"("2-4", "1.5-2.5"), 회복·보호막은 값 하나("2.73"), 화상은 중첩 수(정수). 정의(`ItemEffect`)는 `Min`·`Max`를 갖고, 피해 밖은 둘이 같다.
  - `CritChancePercent`(0..100, 비우면 0): 피해 효과가 있는 `Weapon`만 0보다 크다. `StarDamage`는 0.01 단위("1"은 100).
  - 등급은 없다: `JobData.WeaponGrade`, `DungeonData.ItemGradeBase`·`ItemGradePerFloor`를 뺐고 `EnemyGradePerFloor`는 `EnemyPowerPerFloorPercent`다. `JobData.MaxHp`·`EnemyData.MaxHp`·`PotionData.Magnitude`와 패시브의 보호막(`PassiveMagnitude`, 효과가 `Shield`일 때)은 0.01 단위다.
  - `BalanceData`의 0.01 단위 Key(`StormBaseDamage`, `StormGrowth`, `BurnDamagePerStack`, `BleedDamagePerStack`, `RegenHealPerStack`, `HitMinDamage`)는 소수로 적는다(`BalanceData.IsCenti`). 나머지 Key는 정수다.
- **이야기**(Slice D 55단계, 2026-10-23 Round 101 판정 — 인트로의 연출은 Round 98 움직임 목업. 이야기의 기준은 `Docs/Design/33_Black_Lily_Story_Summary.md`): 이야기의 글·이름·장면 차례·시간·카메라·그림 주소·화자·표정은 모두 아래 넷의 데이터에 있고 코드에는 없다. 장면을 더하고 빼고, 자막·대사를 고치고, 표정을 더하는 일은 CSV(와 그림)만 고친다. Domain의 `StoryRules`가 대화의 차례만 읽고, 시뮬은 읽지 않지만 같은 길(변환·검증·Generated JSON)을 탄다.
  - **`StorySceneData.csv`**(`StorySceneData`): 장면 하나가 한 행. `Id`, `Sequence`(장면 묶음의 id, 지금은 `intro` — 인트로 화면이 이 id를 읽는다), `Order`(묶음 안의 차례, 정수. JSON은 Id 차례라 차례는 이 열이 가진다. 10씩 띄워 사이에 넣는다), `Image`(그림의 Logical Address `story/<sequence>/<key>`. **비면 런의 첫 로비 위의 띠**이고 묶음의 마지막 하나만 될 수 있다), `DurationMs`(본편, 1 이상), `ShortDurationMs`(짧은 판, 0이면 뺌), `ShortLines`(짧은 판의 줄: `all` 또는 줄 번호를 `+`로. 뺀 장면은 비움 — JSON에는 번호 목록), `CameraFrom`·`CameraTo`(`x:y:zoom` — 보는 곳의 가운데를 그림의 천분율로, 배율을 %로(100 = 그림의 폭 전부, 100..400). 그림이 있으면 필수, 띠는 비움 — `StoryCamera`), `Effect`(`StoryEffect`: 비우면 `None`, `Shake` 흔들림, `Heartbeat` 심장 박동의 어둠), `Transition`(`StoryTransition`: `Cross` 1초 겹쳐 넘김, `Black` 끝 1초에 어둠으로 가라앉고 다음이 1.6초에 떠오름), `Countdown`(참이면 이 장면에 "일식까지 N주" — 글은 UI 문구, N은 규칙), 줄 셋 `Line1`~`Line3`의 `.<locale>`(`LocalizedText`, 앞에서부터 채움. 따옴표 “…” 안이 남의 말이고 화면이 금색으로 칠한다 — 목소리 열은 없다), `SoundNote`(소리 메모, 번역 없는 글. 런타임은 읽지 않는다).
    검사: 묶음 안의 `Order`가 겹치지 않음, 줄은 빈틈없이 셋까지(Design/33 §9 "한 화면에 세 줄"), 줄에 `<`·`>` 없음(화면이 rich text로 그림), `ShortLines`의 번호가 있는 줄을 차례로, `ShortDurationMs` 0이면 `ShortLines` 비움, 그림이 없는 장면은 묶음의 마지막이고 카메라·효과가 없음.
  - **`NpcData.csv`**(`NpcData`): `Id`(`linette`), `Name.*`, `Figure`(마을에 서는 전신 그림 `unit/npc/<key>`, 선택), `Portraits`(`표정:주소`를 `+`로: `neutral:portrait/npc/linette-neutral+cold:…+smile:…` — `NpcPortrait`. 표정은 snake_case id이고 Enum이 아니다: 표정을 더하면 이 칸과 그림만 고친다. 같은 표정 두 번 없음).
  - **`DialogueData.csv`**(`DialogueData`): 대화 하나가 한 행. `Id`, `Order`(대화의 차례: 먼저 나올 것, 마을의 리넷이 되풀이할 마지막), `Trigger`(`DialogueTrigger`: `RunStart` 런의 첫 로비 — 인트로 뒤, `DungeonUnlocked` 그 던전이 열린 뒤의 첫 로비), `Dungeon`(`DungeonUnlocked`일 때만. 있는 던전이고 처음부터 열린 던전이 아니어야 함). 대화마다 대사가 하나 이상, `Order`가 겹치지 않음.
  - **`DialogueLineData.csv`**(`DialogueLineData`): 대사 하나가 한 행. `Id`, `Dialogue`(있는 대화), `Order`(대화 안의 차례, 겹치지 않음), `Speaker`(있는 NPC), `Expression`(그 NPC의 `Portraits`에 있는 표정), `Text.*`.
  - `StaticData.ScenesOf(sequence)`·`DialoguesInOrder`·`LinesOf(dialogue)`가 차례대로 준다. 검사는 `StaticData.CheckStory`. 그림의 주소는 다른 그림 열처럼 Editor Setup이 파일과 맞춰 본다(`13_ART_PIPELINE.md`). 매퍼의 새 읽기: `CsvRow.OptionalLocalized`·`OptionalText`·`OptionalCamera`·`LineNumbers`·`PortraitList`.
- `BalanceData.csv`만 `Key,Value` 형식이다. Key는 PascalCase 상수 이름이고 전부 필수다. 모르는 Key는 에러다.
- 모르는 Header는 에러다(오타를 조용히 넘기지 않는다).

## 형 변환

- Required/Optional과 Default를 Mapper에 명시한다. 누락을 CLR Default로 바꾸지 않는다.
- 정수: Invariant Culture, 전체 문자열 소비, 범위 검증. 앞뒤 공백, `+` 부호, 천 단위 구분자 금지.
- bool: `true`/`false`만. Enum: 정의된 이름만.
- Gameplay 수치에 소수(float)를 쓰지 않는다. 비율은 정수(백분율이나 천분율)로 적는다. 22단계의 0.01 단위 값만 CSV에 소수 둘째 자리까지 적고, 읽을 때 정수(0.01 = 1)가 된다: `+`·지수·셋째 자리는 에러다.

## Validation

행 단위 형식 검증 -> 전체 Parse 후 Dataset과 Cross-reference 검증. 하나라도 실패하면 전체가 실패한다.

- Required Header/Value 누락, 열 수 불일치, 모르는 Header
- 형식 오류, 범위 초과, 미정의 Enum
- Duplicate Id, 참조 대상 Id 없음
- Locale Header가 `LocalePolicy.SupportedCodes`와 불일치, 빈 문구

에러 메시지는 Source 파일, Row, Header, 원인을 담는다. Warning을 찍고 계속 가지 않는다.
`StaticDataLoader`도 Load 때 같은 생성자 검증과 Dataset 검증을 다시 건다.

## Generated JSON

```json
{
  "SchemaVersion": 1,
  "Items": []
}
```

- Item은 `Id` 오름차순(Ordinal). Property 순서, 들여쓰기(2칸), Newline(`\n`), Encoding(UTF-8, BOM 없음)을 고정해 **같은 입력 -> 같은 byte**.
- Timestamp, Machine Path 같은 비결정 값을 넣지 않는다.
- Git ignore 대상이 아니다. Source와 같은 Commit에 넣는다.
- Load는 엄격하다. 모르는 Property나 빠진 Property는 에러다.

## Atomic Transform

```text
Source 전부 읽기 -> Parse/Map -> 검증 -> 결과를 메모리에 생성 -> 전체 성공 확인 -> Generated 교체
```

실패하면 기존 Generated를 그대로 둔다. 내용이 같은 파일은 다시 쓰지 않는다.

## 실행

```bash
Tools/chain.sh setup                 # Unity Batch: 변환 + Addressables 동기화
dotnet run --project Tools/Sim -- transform    # Unity 없이 변환 (시뮬 우선 루프)
dotnet run --project Tools/Sim -- validate     # Generated가 Source와 같은지, Load가 되는지 확인
```

- Unity 메뉴: `F1/Data/Transform Static Data`. 끝에 `F1_DATA_TRANSFORM_DONE` 로그.
- Player Build는 validate-only다. Build 중에 Source나 Generated를 바꾸지 않는다.
- stale 검사: Source에서 다시 만든 결과와 Git의 Generated가 다르면 EditMode Test가 실패한다.

## 새 Definition 추가 절차

```text
1. Data/Definitions/<Name>Data.cs (불변 Type + 생성자 검증 + JSON Attribute)
2. StaticDataFiles에 Entry, StaticData에 Table과 Cross-reference, StaticDataLoader의 Load와 Serialize
3. @Data/Source/<Name>Data.csv
4. Editor/Data/Transform에 Mapper, StaticDataTransformer에 연결
5. EditMode Test (TestData, TestCsv에도 추가)
6. Tools/chain.sh (변환 -> Addressables 동기화 -> Test)
```

Addressables Entry는 `StaticDataFiles`에서 자동으로 나온다.

## Validation 체크리스트

- 새 상수가 CSV에 있는가? 코드에 밸런스 숫자가 없는가?
- Generated JSON을 손으로 고치지 않았는가? Source와 같은 Commit인가?
- `Data`, `Editor/Data/Csv`, `Editor/Data/Transform`에 `UnityEngine`/`UnityEditor` 참조가 없는가? (`Tools/Sim` Build로 확인)
- 새 Definition에 누락, 중복 Id, 참조 실패 Test가 있는가?

## Deferred and Forbidden

- Deferred: 수명별 Data 분리, Build 시 validate-only Hook, 스프레드시트 연동.
- Forbidden: Runtime CSV Parsing, ScriptableObject 중심 Static Data, 거대한 단일 GameData, Reflection Data Engine,
  Generated JSON 수동 편집 또는 Git 제외, 정수 Id 병행, 아이템과 유물의 공용 Type.
