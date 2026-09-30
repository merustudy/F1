# 용병 성장 설계 (재구성 v0-recon)

## 1. 원형과 전직
- 【확정】 기본 원형 3종: 전사 / 성직자 / 마법사
- 【확정】 전직 6종 (패시브 구현 완료): `knight`, `berserker`, `bishop`, `paladin`, `archmage`, `spellblade`
- 【해석】 계보: 전사 → knight / berserker, 성직자 → bishop / paladin, 마법사 → archmage / spellblade
- 레퍼런스: 우리들의 모험가 길드 (성장·전직)

## 2. 레벨과 스탯
- 【확정】 레벨과 STR 스탯 존재
- 【확정】 마법사 Lv8 기대 STR = 9 (spellblade 요구치의 근거)
- 【원본확인】 최대 레벨, 경험치 곡선, STR 외 스탯 목록, 성장률

## 3. 전직 요구치

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
- 사망 한도 → 04_Lobby_100Day_Economy §4
