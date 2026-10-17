# 아이템 · 용병 데이터 설계 (재구성 v0-recon)

## 1. 원칙
- 【확정】 모든 상수는 **데이터 파일에서만**. 코드 하드코딩 금지
- 【확정】 (2026-09-30) 원천은 **CSV**다. 게임과 시뮬은 CSV에서 생성한 JSON을 읽는다. 파이프라인 구조는 Docs/Architecture 가 정한다.
- 【확정】 식별자는 **영어 id**
- `prototype_data.json`과 `build_data.py`는 없다 (2026-09-30). 이전 기록의 "JSON에서만"은 위 문장으로 대체한다.

## 2. 확인된 id

| 분류 | id |
|---|---|
| 전직 | knight, valkyrie, bishop, paladin, archmage, spellblade |
| 지역 속성 | swift, hardened, regen, warded, armored |
| 최종 보스 | Abyss Lord (id 표기 【원본확인】) |

## 3. 확인된 값

| 항목 | 값 |
|---|---|
| 짧은 던전 피로도 비용 | 30 (Slice B에서 없앴다: 피로는 원정 안에서 쌓인다, 04 §3) |
| 피로도 임계 | 100 / 150 / 200 (Slice B: 100 붕괴 판정, 200 쓰러짐, 150은 비움. 04 §3) |
| swift | −8% |
| hardened | −15% |
| 보스 B / C 레벨 | 9 / 13 |
| 최종 보스 레벨 / HP | 14 / 420 |
| 최종 보스 페이즈 기준 | HP 75% / 50% / 25% |
| Hell 사망 한도 | 15 |
| bishop 패시브 보호막 | +10 |
| spellblade 화상 | +3 |
| spellblade 기본 무기 | 롱소드 grade 12 (2026-10-06: 기사와 같은 검으로 통일. 소드 아이템은 뺐다) |
| spellblade / paladin STR 요구 | 9 / 11 |

## 4. 스키마
- 원본 JSON은 없다 (2026-09-30). CSV의 파일 단위와 열의 형식은 Docs/Architecture 의 Static Data 문서가 정한다.
- 【확정】 (2026-09-30 일괄 승인) Slice A의 데이터 분류. 분류마다 CSV 하나다 (`Assets/@Data/Source`).

| 파일 | 내용 |
|---|---|
| `BalanceData.csv` | 규칙의 상수 (Key, Value). 02~04 문서의 `코드 글꼴` 이름이 Key다. 17단계: 상점의 `ShopSlots`·`ShopRefreshBase`·`ShopRefreshStep`(Round 56: 포션의 `ShopPotionChancePercent`), 코인의 `CoinsPerEnemy`·`CoinsPerFloor`·`EliteCoinPercent`. 18단계: 전리품의 `DropCount`·`EliteDropCount`(보상의 `RewardChoices`는 뺐다). 25단계: `DeathLimit`. 26단계: 금의 `GoldPerFloor`·`GoldClear`, 역마차의 `HirePrice`·`RecruitCount`·`RosterLimit`, 쉼터의 `TreatmentPrice`·`TreatmentRelief`(04 §3·§5·§6). 27단계: 경험치의 `ExpPerFloor`·`ExpClear`(05 §2) |
| `JobData.csv` | 직업: 이름, 최대 HP, 보드의 칸 수, 기본 무기와 등급, 패시브(자리 조건 포함)와 그 설명문, 권장 열 |
| `ItemData.csv` | 아이템(23단계부터 `Unique` 열: 유니크는 상점·전리품에 없다): 분류(무기 장비·방어 장비·공격 아이템·지원 아이템·기타 아이템, 02 §4), 크기(칸), 쿨다운, 쓸 수 있는 자리(앞이나 뒤에서 N번째까지), 효과(타입, 타깃, 깊이, 계수), 상점 가중치 `ShopWeight`(18단계에 보상 가중치에서 이름을 바꿨다. 0이면 상점에 나오지 않음), 상점의 값 `Price`(일반 기준. 0이면 팔지 않음, 17단계). 적의 아이템도 같은 척도다(02 §4). 20단계: 근접 무기 `Melee`(true/false, 무기 장비만 true), 별 칸 `Stars`(돌리지 않은 모양의 왼쪽 위 칸에서 잰 "x:y"를 +로 이음, 아이템 둘레의 칸만), 별의 피해 `StarDamage`(별에 놓인 근접 무기의 피해 +N, 일반 기준이고 단계마다 N씩 더. 별이 있으면 1 이상). 효과가 없는 아이템은 별이 있고 쿨다운이 0이다(발동하지 않음) |
| `RecipeData.csv` | 담금질의 레시피(23단계): 결과(유니크 아이템), 바탕 아이템과 단계, 촉매(★ 아이템)와 단계. 이름 열은 없다 | `Id,Result,BaseItem,BaseTier,Catalyst,CatalystTier` |
| `PotionData.csv` | 포션: 효과와 크기, 상점 가중치 `ShopWeight`(18단계), 병 아이콘, 상점의 값 `Price`(17단계) |
| `EnemyData.csv` | 적: 레벨, 최대 HP, 아이템과 등급(적의 힘. 쓰러뜨리면 그 아이템이 던전의 등급으로 떨어진다: 03 §5), 그림과 그림의 크기 배율(보스는 더 크게), 페이즈(28단계, 선택 열 `Phases`: HP 문턱마다 바꿔 드는 아이템 묶음. 02 §9) |
| `EnemyGroupData.csv` | 적 무리: 던전, 나오는 층, 보스 여부, 정예 여부(Slice B), 서는 순서(앞에서부터, 넷까지) |
| `AffinityData.csv` | 지역 속성: 적 쿨다운 수정 |
| `DungeonData.csv` | 던전: 속성, 층 수, 맵 너비, 일수, 보상 등급, 시작 포션, 정예·야영지가 나오는 층과 확률과 야영지 층, 깊은 층의 적, 보상의 단계가 시작하는 층(Slice B), 상점이 나오는 층과 확률 `ShopMinFloor`·`ShopChancePercent`(17단계) (피로도 비용은 Slice B에서 없앴다), 최종 던전과 해금(25단계: `IsFinal`, `UnlockDungeonId`, `UnlockClears`), 원정 지도의 표식 자리(Round 71 E2: `MapX`, `MapY`) |
| `MercenaryData.csv` | 시작 로스터(`Starting` 참: 이름, 직업)와 역마차의 **이름 풀**(26단계. `Starting` 거짓: 이름만, `JobId` 비움 — 후보를 뽑을 때 직업을 굴린다). 고용된 용병의 id는 풀의 id이고 직업은 런의 상태가 가진다 |
| `FatigueStateData.csv` | 피로의 붕괴 판정이 주는 상태(Slice B): 종류(고통·각성), 쿨다운·받는 회복·빈사 사망 확률의 변화, 이름과 설명 |
| `LevelData.csv` | 레벨의 사다리(27단계, 05 §2): `Level`(1부터), `ExpToNext`(다음 레벨까지의 경험치, 마지막은 0), `MaxHpBonusPercent`. 이름 열이 없다. 경험치의 양은 `BalanceData`의 `ExpPerFloor`·`ExpClear` |

- 【확정】 `items`와 `relics`는 반드시 별도 분류 (06_Relic_System §4). 유물은 Slice A 밖이라 파일이 없다.
- (Slice C) 더할 데이터는 그 단계에서 만든다(미리 만들지 않는다): 런의 끝·금·고용·시설의 상수는 `BalanceData`(25·26단계 — 넣었다), 고용 후보의 이름 풀(26단계: `MercenaryData`의 `Starting` 열로 정했다 — 이름 하나가 곧 용병 id라 저장·이름 표시·전투 Setup이 한 표로 된다), `LevelData.csv`(27단계: 넣었다 — 레벨, 필요 경험치, 최대 HP 보너스 %), Abyss Lord·호위·페이즈 묶음(28단계: 넣었다 — `EnemyData`의 선택 열 `Phases`(`75=item:100+item:100|50=…`), `EnemyGroupData`의 심연 무리 셋, `ItemData`의 군주 아이템 셋. 임시 무리 둘은 지웠다). 적이 떨어뜨리는 아이템의 `Price`(2026-10-17 Round 74, 팔 값을 위해: 쥐 송곳니 2, 녹슨 칼·조잡한 활 6, 등불 지팡이·치유의 노래 8, 감독관의 포효 12·망치 14, 심연의 울부짖음·손아귀 12·식칼 16. `ShopWeight` 0이라 상점에는 나오지 않는다), `BalanceData` `SellPercent` 50, `CoinsPerGold` 10(클리어의 코인 → 금, 2026-10-17). 기본 무기의 `Price` 16·24와 `ShopWeight` 3(파티의 것만 상점에: 03 §5), `ItemData`의 선택 열 `DropWeight`(일반 아이템 드랍의 가중치), `DungeonData`의 선택 열 `GeneralDropChancePercent`(광산 30, 심연 0). 25단계가 미리 넣은 것: `BalanceData` `DeathLimit`, `DungeonData`의 선택 열 `IsFinal`·`UnlockDungeonId`·`UnlockClears`와 임시 행 `abyss`, `EnemyGroupData`의 임시 무리 둘(03 §1).
- 위 3절의 "확인된 값" 가운데 Slice A가 쓰는 것은 CSV의 출발값으로 넣었다. 시뮬 결과로 바뀌면 CSV가 기준이다.
