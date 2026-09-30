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
- 원본 JSON은 없다 (2026-09-30). 스키마는 재정의 대상이고, CSV의 파일 단위와 열은 Docs/Architecture 의 Static Data 문서가 정한다.
- 【제안】 데이터 분류 초안 (CSV에서는 분류마다 파일 하나가 된다):
  `items`, `jobs`, `enemies`, `bosses`, `relics`, `affinities`, `dungeons`, `balance`
  - `items`와 `relics`는 반드시 별도 분류 (06_Relic_System §4)
