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
| `BalanceData.csv` | 규칙의 상수 (Key, Value). 02~04 문서의 `코드 글꼴` 이름이 Key다. 17단계: 상점의 `ShopSlots`·`ShopRefreshBase`·`ShopRefreshStep`(Round 56: 포션의 `ShopPotionChancePercent`), 코인의 `CoinsPerEnemy`·`CoinsPerFloor`·`EliteCoinPercent`. 18단계: 전리품의 `DropCount`·`EliteDropCount`(보상의 `RewardChoices`는 뺐다) |
| `JobData.csv` | 직업: 이름, 최대 HP, 보드의 칸 수, 기본 무기와 등급, 패시브(자리 조건 포함)와 그 설명문, 권장 열 |
| `ItemData.csv` | 아이템: 분류(무기 장비·방어 장비·공격 아이템·지원 아이템·기타 아이템, 02 §4), 크기(칸), 쿨다운, 쓸 수 있는 자리(앞이나 뒤에서 N번째까지), 효과(타입, 타깃, 깊이, 계수), 상점 가중치 `ShopWeight`(18단계에 보상 가중치에서 이름을 바꿨다. 0이면 상점에 나오지 않음), 상점의 값 `Price`(일반 기준. 0이면 팔지 않음, 17단계). 적의 아이템도 같은 척도다(02 §4). 20단계: 근접 무기 `Melee`(true/false, 무기 장비만 true), 별 칸 `Stars`(돌리지 않은 모양의 왼쪽 위 칸에서 잰 "x:y"를 +로 이음, 아이템 둘레의 칸만), 별의 피해 `StarDamage`(별에 놓인 근접 무기의 피해 +N, 일반 기준이고 단계마다 N씩 더. 별이 있으면 1 이상). 효과가 없는 아이템은 별이 있고 쿨다운이 0이다(발동하지 않음) |
| `PotionData.csv` | 포션: 효과와 크기, 상점 가중치 `ShopWeight`(18단계), 병 아이콘, 상점의 값 `Price`(17단계) |
| `EnemyData.csv` | 적: 레벨, 최대 HP, 아이템과 등급(적의 힘. 쓰러뜨리면 그 아이템이 던전의 등급으로 떨어진다: 03 §5), 그림과 그림의 크기 배율(보스는 더 크게) |
| `EnemyGroupData.csv` | 적 무리: 던전, 나오는 층, 보스 여부, 정예 여부(Slice B), 서는 순서(앞에서부터, 넷까지) |
| `AffinityData.csv` | 지역 속성: 적 쿨다운 수정 |
| `DungeonData.csv` | 던전: 속성, 층 수, 맵 너비, 일수, 보상 등급, 시작 포션, 정예·야영지가 나오는 층과 확률과 야영지 층, 깊은 층의 적, 보상의 단계가 시작하는 층(Slice B), 상점이 나오는 층과 확률 `ShopMinFloor`·`ShopChancePercent`(17단계) (피로도 비용은 Slice B에서 없앴다) |
| `MercenaryData.csv` | 시작 로스터: 이름, 직업 |
| `FatigueStateData.csv` | 피로의 붕괴 판정이 주는 상태(Slice B): 종류(고통·각성), 쿨다운·받는 회복·빈사 사망 확률의 변화, 이름과 설명 |

- 【확정】 `items`와 `relics`는 반드시 별도 분류 (06_Relic_System §4). 유물은 Slice A 밖이라 파일이 없다.
- 위 3절의 "확인된 값" 가운데 Slice A가 쓰는 것은 CSV의 출발값으로 넣었다. 시뮬 결과로 바뀌면 CSV가 기준이다.
