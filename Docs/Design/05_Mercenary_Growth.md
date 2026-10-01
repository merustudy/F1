# 용병 성장 설계 (v0-recon, 2026-09-30 재정의)

> `【확정】 (일괄)`은 2026-09-30 일괄 승인으로 다시 정한 Slice A 규칙이다. 수치는 `Assets/@Data/Source/*.csv`가 가진다.

## 1. 원형과 전직
- 【확정】 기본 원형 3종: 전사 / 성직자 / 마법사
- 【확정】 전직 6종: `knight`, `berserker`, `bishop`, `paladin`, `archmage`, `spellblade`
- 【해석】 계보: 전사 → knight / berserker, 성직자 → bishop / paladin, 마법사 → archmage / spellblade
- 레퍼런스: 우리들의 모험가 길드 (성장·전직)
- 【확정】 (일괄) Slice A에는 성장과 전직이 없다. 용병은 처음부터 전직 직업 하나를 가지고, 바뀌지 않는다.
- 【확정】 (일괄) Slice A의 시작 로스터는 전직 6종이 하나씩이다 (`MercenaryData.csv`).

## 2. 레벨과 스탯
- 【확정】 레벨과 STR 스탯 존재
- 【확정】 마법사 Lv8 기대 STR = 9 (spellblade 요구치의 근거)
- 【확정】 (일괄) Slice A의 용병 스탯은 직업이 정한다(`JobData.csv`): 최대 HP, 아이템 칸 수, 기본 무기와 등급, 패시브, 권장 열.
  레벨과 STR은 Slice A에서 쓰지 않는다.
- 【확정】 (일괄) **권장 열**은 시뮬 정책이 자리를 정할 때 쓰는 기본값일 뿐이다. 조건 판정은 실제 위치로 한다.
- 【원본확인】 최대 레벨, 경험치 곡선, STR 외 스탯 목록, 성장률 (Slice A 밖)

## 3. 전직 요구치 (Slice A 밖)

| 직업 | 요구치 | 근거 |
|---|---|---|
| spellblade | STR 9 | 마법사 Lv8 기대 STR에 맞춤 【확정】 |
| paladin | STR 11 | 하향 조정 【확정】 |
| knight / berserker / bishop / archmage | 【원본확인】 | |

- 【해석】 전직 시점은 Lv8 전후

## 4. 직업 패시브
→ 02_Combat_System §7

## 5. 영구 사망
- 【확정】 퍼마데스 (Darkest Dungeon 레퍼런스)
- 【확정】 (일괄) 죽은 용병은 귀환 정산에서 로스터에서 지워지고 돌아오지 않는다. 저장과 이어하기로 되돌릴 수 없다.
- 사망 한도 → 04_Lobby_100Day_Economy §4
