# Slice B 22단계: 피해의 범위와 수의 크기

Snapshot: 2026-10-11 (구현, 치명타 표시 "A 빨강", 세션 정리에서 커밋·푸시)

## Goal

Architecture/09 "Slice B" Acceptance 14가 참이다: 롱소드의 카드가 "피해 2-4"이고 전투의 피해는 그 사이에서 공격마다 굴린 값이다(같은 시드와 입력이면 같은 결과).
무기의 치명타는 맞는 대상마다 굴리고 굴린 피해의 1.5배이며, 맞은 용병과 동료를 지치게, 넣은 용병과 동료를 덜 지치게 한다. 화면의 수는 정수(반올림, 살아 있으면 HP 1 이상)다.
0.50 미만으로 보호막을 뚫은 피해는 피격이 아니다. 시뮬 수치가 16단계의 목표 띠 안이다. 옛 저장(버전 10)은 HP의 몫을 지켜 이어진다.

## State

- 2026-10-11 Round 60(`ArtPipeline/Archive/60-damage-range/README.md`): 사용자 "최소 - 최대 데미지 / 롱소드 피해 2-4 기준으로 체력·피해 재조정(크기 반영) / 소수 계산·화면은 정수 / 검토 후 보고. 승인 후 구현"
  → 저장소 밖 복사본의 시험 구현과 시뮬, 판정할 것 열하나 → 1차 판정(무기마다 폭, 공격마다 굴림·대상마다 치명타, 9 재설명, 10 반올림, 11 재검토, 단검 1-2)
  → 2차 검토(소수의 재설명, 폭 셋, 치명타의 조사와 피로, 보스 레버, HP 반올림) → **"D. 치명타 크기는 범위 내에서 결정된 피해의 1.5배로 / 그 외 전부 권고안 반영"**(Roadmap "22단계의 승인") → 22단계를 열어 구현했다.
- 치명타의 떠오르는 수: 목업 셋(안 A 금빛 + "치명타" 권장 / B 빨강 + ! / C 금빛 숫자만) → 사용자 "치명타 모습 목업 다시 제공해줘"(실제 화면 위 `mock-crit-floats-v2.png`와 MP4) → "A에 색만 빨강으로 다시 목업 제공"(`mock-crit-red.png`·MP4)
  → **"요청안으로 구현"**: 피해의 빨강 1.6배 "-5!"와 위의 작은 "치명타", 1.4초(`BattleFxLayer.FloatCritical`, `BattlePresenter`가 `CriticalHit` 다음의 피해를 그렇게 띄움). PlayMode Test와 스크린샷 `_51_battle_critical`.

## Done

- 기획: Design/02 §4·§5·§13, 03 §4·§5, 04 §3, 09 §5, 00 "Slice B" 표, 08 §21(시뮬). CLAUDE.md §9. Architecture/05(0.01 값과 열), 07(저장 11), 08(피해 스트림·치명타·피격의 최소), 09(범위·순서 11·Acceptance 14), 12("수의 표시").
- 데이터: `ItemData.csv`의 `Effect1Value`·`Effect2Value`("2-4", "2.73", 화상 중첩)와 `CritChancePercent`, `JobData.csv`의 `WeaponGrade` 뺌과 HP·보호막(0.01), `EnemyData.csv`의 HP와 `item:세기%`, `DungeonData.csv`의 `ItemGradeBase`·`ItemGradePerFloor` 뺌과 `EnemyPowerPerFloorPercent`, `PotionData.csv` 14·11,
  `BalanceData.csv`의 폭풍 0.55·`BurnDamagePerStack` 0.27·`HitMinDamage` 0.5·`CritDamagePercent` 150·치명타의 피로 넷(9·2·3·1), Generated.
- 코드: `Centi`(읽기·반올림·글), `ItemEffect`(`Min`·`Max`·`Scaled`), `ItemData.CritChancePercent`, `ItemGrant.PowerPercent`, `BalanceData`(0.01 Key, `IsCenti`, 새 Key), `CsvRow.Centi`·`CentiRange`, 매퍼,
  `EquippedItem`(등급 없음, `PowerPercent`, `Min`·`Max`), `RngStream.Damage`, `BattleEngine`(굴림·치명타·`CriticalHit`·치명타의 피로·피격의 최소·화상), `ItemOffer`(등급 없음), `ExpeditionRules`(등급 없는 드랍·상점·합치기, 적의 세기),
  저장 11(`From10To11`, 등급 필드 뺌), 화면(`UiText.Amount`·`Hp`·`Range`·`EffectValue`, 카드의 범위와 치명타 %, 제목의 등급 뺌, 전투의 수·HP·보호막·폭풍, 로그의 치명타 줄·치명타를 맞은 피로 줄, 정비의 바뀜 줄), 문구 넷과 뺀 셋, 시뮬(`items` 명령, 치명타 수).
- Test: EditMode `DamageRangeTests`(일곱), `NumberDisplayTests`(셋), `DefinitionTests`(범위·치명타·`Centi`·`Scaled`), 저장 10 → 11, 데이터 Test의 새 열, 옛 Test는 "등급 × 계수"를 "세기 % × 값"으로 옮겨 정수 계산을 그대로 지킴(`TestData`의 기본 무기 10, 흐름 Test의 강한 칼 `StrongBlade`).

## Open

- 사후 검토(Roadmap "22단계의 권장안 세부"): 치명타를 맞은 피로 +9(다키스트 던전 +10), 보스 HP +10%, 폭의 배정, 직업·적 HP의 정수 반올림.
- 직접 플레이: 롱소드 2-4의 수가 읽히는지(작은 무기의 "1", "1-2"), 치명타가 손에 느껴지는지, 치명타의 피로(+9)가 무거운지, 보스전.
- 시뮬의 손(`SimPolicy`)은 아이템을 고를 때 평균을 보지 않는다(지금처럼 자리·합치기). 범위·치명타를 보는 정책은 하지 않았다.

## Verification

- 시뮬(Design/08 §21): balanced 63.9% / 0.28, 붕괴 46.6%, 보스전 91.8%, safe 57.7% / 0.27, none 79.7% / 1.32, 여섯 파티 68.0 · 62.6 · 48.6 · 35.7 · 6.6 · 0.1 — 띠 안. `Tools/Sim items`: 던전의 무기 0.33, 단검 0.50.
- 체인 `20261011-140655` **CHAIN OK**(setup, sim, EditMode 846/846, PlayMode 82/91, 9 skipped). 앞선 실패: 옛 Test의 등급(컴파일), EditMode 13(데이터 Test의 옛 열, 기본 무기의 세기), PlayMode 4(`Regrade`가 등급 없이 아무 일도 안 함, HP 100000이 1000) — 모두 Test 쪽을 고침.
- 치명타 표시 뒤 체인 `20261011-154912` **CHAIN OK**(EditMode 846/846, PlayMode 83/92), 스크린샷 `20261011-s22c` 9/9(PNG 86, `_51_battle_critical`이 목업 "A 빨강"과 같음).
- 스크린샷 `20261011-s22b` 9/9(PNG 85), 목업과 대조해 고친 것: 카드의 사실 줄이 낱말 가운데서 끊김(`<nobr>`), 로그의 "피해 0"(그 줄을 쓰지 않음). 장면은 `ArtPipeline/Archive/60-damage-range/game/impl_*`.

## Next Action (제안)

- 사용자: 직접 플레이(x1, 원정 하나). 치명타의 빨강이 큰 피해와 갈리는지(글이 갈라 줌)도 본다.
