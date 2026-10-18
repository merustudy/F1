# Slice D 33단계: 판정의 스탯

Snapshot: 2026-10-18 (완료: 구현·시뮬·체인 OK·스크린샷 대조. 커밋 안 함. 다음은 34단계 — 시작 지시 대기)

## Goal
명중·회피(무기의 스탯·대상의 민첩·적의 `Accuracy`·`Dodge`)로 공격이 빗나갈 수 있고 빗나감이 글·자막·로그로 보이며, 치명타 + 민첩, 빈사 − 체력, 각성 + 정신력, 오르는 피로 − 인내가 데이터의 식으로 작동한다. 카드가 명중과 민첩의 치명타를 보인다. 시뮬이 같은 시드로 전후를 잰다. 범위는 Architecture/09 "Slice D" 구현 순서 2.

## State
- 2026-10-18 사용자 "다음 단계 진행"으로 시작해 구현했다. 체인 OK. 커밋 안 함(32단계와 31단계 문서도 작업 트리에 함께 있다).

## Done
- 데이터: `BalanceData`의 `HitBasePercent`·`HitMinPercent`·`HitMaxPercent`·`HitPerStatPoint`·`DodgePerDexPoint`·`CritPerDexPoint`(0.01 단위 셋)·`DogDeathPercentPerConPoint`·`VirtuePerSpiPoint`·`FatiguePercentPerEndPoint`와 검증, `EnemyData`의 `Accuracy`·`Dodge`(선택 열, −100..100; 쥐 회피 10, 감독관·군주 명중 10).
- Domain: `StatRules`(CanMiss·Accuracy·Dodge·HitChance·CritChance·VirtueChancePercent·DeathChanceShift·FatigueGain), `FatigueRules.DogDeathChancePercent`(체력), `BattleUnitSetup.Accuracy`·`Dodge`, 엔진(대상마다 `RollHit` → `Missed`, 만분율의 치명타, 빈사·각성의 스탯, `ChangeFatigue`의 인내), `ExpeditionRules.BeginBattle`(진입 비용의 인내), 적 Setup의 값.
- 화면: `BattlePresenter`(빗나감의 회색 글, 소리 없음), `BattleLogText`(`Log.Missed`), `UiText`(카드의 명중·치명타, `Percent`), `ItemTooltipView`·`BattleScreen`(적 카드의 `Accuracy`), `BattleUnitView`(빈사 확률의 체력), 문구 `Item.Hit`·`Log.Missed`·`Fx.Miss`.
- 시뮬: 전투 통계의 빗나감 줄. 08 §37(적 0과 적의 값).
- Test: 새 `StatJudgementTests`, `DamageRangeTests`의 치명타 확률 단위, Test 데이터의 키(명중 100%: 확실한 명중은 굴리지 않아 기존 Test의 난수가 그대로).
- 문서: Design/02 §5·07·08 §37, Architecture/05·08·12·14, Roadmap(33단계, 사후 검토).

## Open
- 런 균형형 86.7%(띠 65~80% 위): 35단계의 레버(08 §37). 사후 검토.
- 스크린샷 확인은 끝났다: `ko_71_battle_miss`(연출만 세움)와 `ko_47_map_star_card`(치명타 4.5% 붉게, 명중 82% 녹색).

## Verification
- `dotnet run --project Tools/Sim -- validate` OK. 시뮬은 저장소 데이터로 다시 돌려 변형과 같은 수치를 확인했다.
- 체인 `20261018-115955`: EditMode 986/987(제 Test가 민첩 20의 회피를 빠뜨림), PlayMode 1 실패(보스전에서 "아군이 아무도 죽지 않는다"는 가정 — 빗나감 뒤 아군이 죽을 수 있어 "죽은 아군은 로그가 이름을 적는다"로 고침) → `20261018-143643` CHAIN OK(EditMode 987/987, PlayMode 91/102) → 빗나감 로그 Test를 더해 `20261018-145904` PlayMode 92/103. 건너뜀 11은 그래픽 스크린샷 Test. 스크린샷 `20261018-stats33` 11/11.

## Next Action (제안)
- 34단계 시작 지시를 받으면: 이 Handoff를 completed로 옮기고 `d34-…`를 만든 뒤 역마차의 편차 ±2(`Recruit` 스트림)·후보의 ▲▼, 레벨업의 굴림(성장 등급 여섯, `Growth` 스트림)과 정산의 성장 줄, `LevelData.MaxHpBonusPercent` 삭제(옛 저장의 레벨 처리 포함)를 구현하고 같은 시드로 잰다.
