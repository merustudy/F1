# Slice D 51단계: 효과 규칙의 개정 (+ Round 97 금화 테스트 버튼)

Snapshot: 2026-10-23. 지시의 흐름은 다음과 같다.

- 사용자 "가속, 재생, 암흑 등 효과 중첩이 너무 강해서 너프 필요(특히 가속은 상한을 100으로)"와 "로비 화면에서 금화 테스트 버튼" → Design/34와 Round 97 목업.
- "쿨다운의 1%를 줄이는 것으로 하자", "금화 테스트는 권고안으로 먼저 구현. 가속도 1%로 먼저 구현 후 다른 효과들 보고" → 구현.
- "상한은 두지 않는 걸로 적용", 값 일곱의 검토, 화상·출혈의 검토와 효과의 순위.
- "새규칙으로하고 아이템의 효과를 조정하자 … 최대 체력 아이템 추가, 방어구 방어력 추가로" → Design/35.
- 판정 "1. 아이템 및 포션 … 9. 권고안 수행" → 구현.

## Goal
효과 중첩을 너프한다. 일곱 상태의 값을 낮추고, 화상·출혈·재생은 1/초로 하되 아이템의 중첩을 절반으로 한다. 센 것(얼음 플라스크)은 줄이고 약한 것은 올린다. 최대 체력 아이템을 넣고 방어구를 키운다. 광산은 띠 안에 두고, 런 띠는 43단계 2차에서 맞춘다.

## State
- 결정: 00 "Design/34의 판정 (1차)"·"Design/35의 판정"·"Round 97의 판정". 기획은 02 §4·§5, Design/34·35. 시뮬은 08 §57·§58. Owner 문서는 Architecture/08·09·10·12, CLAUDE.md §9.
- 데이터:
  - `BalanceData`: 가속 1, 암흑·빙결·조준·예리 1, 격분·취약 5, 화상·출혈·재생 1.00.
  - `ItemData`: 중첩 절반 열, 얼음 플라스크 800, 방어구 ×1.5, 깃발 8·살기의 부적 8·8·약초 주머니 2.00·로브 5.46·철 투구 4.36, 새 `leather_belt`(+6)·`guardian_amulet`(+4)(아이콘 없음 = 이름 Placeholder).
  - `PotionData`: 재생 포션 4. Generated JSON 셋.
- 코드:
  - `EffectKind.MaxHp`(`ItemData`: 전투 시작 아이템만), `BattleUnit.MaxHpBonus`·`MaxHp`, `BattleEngine.RaiseMaxHp`·`MaxHpRaised`.
  - `ApplyHeal`: 재생 틱이 아닌 회복은 출혈 1. `ExpeditionRules.HpAfterBattle`.
  - 화면: `UiText`·`BattleLogText`·`BattlePresenter`, 문구 셋. 시뮬 `trace`.
  - Round 97: `RunRules.GrantGold`, `RunManager.GrantTestGold`(개발 빌드만), `PlayLog.EventPrefix`와 `AppRoot`의 `!TestGold`, `Tools/playlog.py`의 사건 줄, `UiPrefabSetup.Lobby`·`LobbyScreen`·`UiPalette`·문구.
- Test:
  - `EffectVocabularyTests`: 출혈 셋을 고쳤고, `MaxHp_…` 둘을 더했다.
  - `ShippedDataTests`: 아이템 60, 아이콘 예외 둘, 재생 포션 4, `TheMaximumHpItems_…`.
  - `RunManagerTests.GrantTestGold_…` 둘, `UiFlowTests.Lobby_TestGoldButton_…`, 스크린샷 `TheTestGold_Korean`.
- 저장: 형식 그대로(27).
- 커밋: 세션 정리: 2026-10-23 커밋·푸시 — 기획·Architecture `eb78941` / 구현 `29b58be` / Round 97 `3029de2` / Roadmap·Handoff.

## Verification
- `transform` → `validate` OK(8 jobs, 60 items, 9 enemies).
- 가속 1%의 체인 `20261023-082256` CHAIN OK, 스크린샷 `20261023-r97`. 기록은 `active/d43-tuning.md`의 Verification과 README 97.
- 51단계 시뮬 전후: 08 §58.
- 51단계 전체 체인 `20261023-114133`: setup·sim OK, PlayMode 98/113(건너뜀 15는 스크린샷 Test), EditMode 1116/1118. 실패 둘은 새 Test의 기대값이었다.
  - 출혈의 첫 틱은 줄기 전의 5중첩으로 5를 깎는다.
  - 최대 체력 아이템이 방어구의 요구 축에 체력을 더한다.
  - 고친 뒤 EditMode `20261023-120602` **1118/1118**(CHAIN OK). `Assets/InitTestScene*` 없음, `git diff --check` 깨끗.
- 화면 배치는 바뀌지 않아 스크린샷 라운드는 하지 않았다(카드의 수와 로그·띄움 글만).

## Open
- 원정당 죽음 0.47(띠 0.3~0.4 위), 붕괴 47%(위 가장자리): 적의 화상·출혈 1/초(판정 4 "그대로")의 몫. 사후 검토.
- 깃발·로브·최대 체력 아이템은 하나씩 빼기로 여전히 약하다(장비 피로·명중 상한). 사후 검토.
- 최대 체력 아이템 둘의 아이콘은 그림 라운드(요청 때)다.
- 43단계 2차: 이 위에서 권장 **군주 620 + 광산 7일 + 심연 4일**(여섯 시드: 균형형 67.0%·전멸 22.4·한도 9.4·47.4일, 안전형 77.9%·결전행 16.9). 판정 대기. `active/d43-tuning.md`.

## Next Action
- 이 단계는 끝났다. 다음은 43단계 2차의 판정(`active/d43-tuning.md`)과 직접 플레이.
