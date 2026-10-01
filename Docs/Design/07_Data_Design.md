# 아이템 · 용병 데이터 설계 (재구성 v0-recon)

## 1. 원칙
- 【확정】 모든 상수는 **데이터 파일에서만**. 코드 하드코딩 금지
- 【확정】 (2026-09-30) 원천은 **CSV**다. 게임과 시뮬은 CSV에서 생성한 JSON을 읽는다. 파이프라인 구조는 Docs/Architecture 가 정한다.
- 【확정】 식별자는 **영어 id**
- `prototype_data.json`과 `build_data.py`는 없다 (2026-09-30). 이전 기록의 "JSON에서만"은 위 문장으로 대체한다.

## 2. 확인된 id

| 분류 | id |
|---|---|
| 전직 | knight, berserker, bishop, paladin, archmage, spellblade |
| 지역 속성 | swift, hardened, regen, warded, armored |
| 최종 보스 | Abyss Lord (id 표기 【원본확인】) |

## 3. 확인된 값

| 항목 | 값 |
|---|---|
| 짧은 던전 피로도 비용 | 30 |
| 피로도 임계 | 100 / 150 / 200 |
| swift | −8% |
| hardened | −15% |
| 보스 B / C 레벨 | 9 / 13 |
| 최종 보스 레벨 / HP | 14 / 420 |
| 최종 보스 페이즈 기준 | HP 75% / 50% / 25% |
| Hell 사망 한도 | 15 |
| bishop 패시브 보호막 | +10 |
| spellblade 화상 | +3 |
| spellblade 기본 무기 | 소드 grade 12 |
| spellblade / paladin STR 요구 | 9 / 11 |

## 4. 스키마
- 원본 JSON은 없다 (2026-09-30). CSV의 파일 단위와 열의 형식은 Docs/Architecture 의 Static Data 문서가 정한다.
- 【확정】 (2026-09-30 일괄 승인) Slice A의 데이터 분류. 분류마다 CSV 하나다 (`Assets/@Data/Source`).

| 파일 | 내용 |
|---|---|
| `BalanceData.csv` | 규칙의 상수 (Key, Value). 02~04 문서의 `코드 글꼴` 이름이 Key다 |
| `JobData.csv` | 직업: 이름, 최대 HP, 보드의 칸 수, 기본 무기와 등급, 패시브(자리 조건 포함)와 그 설명문, 권장 열 |
| `ItemData.csv` | 아이템: 분류, 크기(칸), 쿨다운, 쓸 수 있는 자리(앞이나 뒤에서 N번째까지), 효과(타입, 타깃, 깊이, 계수), 보상 가중치 |
| `PotionData.csv` | 포션: 효과와 크기, 보상 가중치 |
| `EnemyData.csv` | 적: 레벨, 최대 HP, 아이템과 등급 |
| `EnemyGroupData.csv` | 적 무리: 던전, 나오는 층, 보스 여부, 서는 순서(앞에서부터, 넷까지) |
| `AffinityData.csv` | 지역 속성: 적 쿨다운 수정 |
| `DungeonData.csv` | 던전: 속성, 층 수, 맵 너비, 피로도 비용, 일수, 보상 등급, 시작 포션 |
| `MercenaryData.csv` | 시작 로스터: 이름, 직업 |

- 【확정】 `items`와 `relics`는 반드시 별도 분류 (06_Relic_System §4). 유물은 Slice A 밖이라 파일이 없다.
- 위 3절의 "확인된 값" 가운데 Slice A가 쓰는 것은 CSV의 출발값으로 넣었다. 시뮬 결과로 바뀌면 CSV가 기준이다.
