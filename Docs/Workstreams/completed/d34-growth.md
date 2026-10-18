# Slice D 34단계: 후보의 편차와 성장

Snapshot: 2026-10-18 (완료. 커밋 안 함)

## Goal
역마차의 후보가 직업 기본값에서 ±`RecruitStatSpread`만큼 다르고 줄에 ▲▼로 보이며, 레벨업이 직업의 성장 등급대로 스탯을 굴리고 정산 화면이 성장 줄로 적는다. 레벨의 HP 보너스 열이 없고, 저장 19가 옛 레벨 용병에게 기대 성장을 준다. 시뮬이 같은 시드로 잰다. 범위는 Architecture/09 "Slice D" 구현 순서 3.

## State
- 2026-10-18 사용자 "다음단계 진행"으로 시작해 구현하고 끝냈다. 커밋 안 함(31~33단계의 변경도 작업 트리에 함께 있다).

## Done
- 데이터: `JobData.Growth`(필수 열 `GrowthStr`…`GrowthEnd`, 0..200; 05 §2의 출발값), `BalanceData.RecruitStatSpread` 2, `LevelData`에서 `MaxHpBonusPercent` 삭제(정의·변환·검사·CSV), `RngStream.Growth`.
- Domain: `RunRules.DrawRecruits`의 편차(`Spread`), `LevelRules.Grow`·`ExpectedGrowth`·`MaxHpOf(data, job, stats)`, `Settle`의 성장(`SettlementReport.LevelUpGains`), `ExpeditionRules.Create`의 HP.
- 저장 19: `From18To19`(레벨 L 용병에게 (L − 1) × 등급 ÷ 100 반올림).
- 화면: `UiText.StatColumns`(기본값 대비 ▲▼)·`GrowthLine`, `LobbyScreen`(후보 줄), `SettlementScreen`(성장 줄, 패널·아래 줄의 이동), 빌더(성장 글 상자와 참조), 문구 넷.
- Test: 새 `GrowthTests`, `RunSaveMapperTests`(18 → 19), `LevelRulesTests`·`StatRulesTests`·`StaticDataTransformerTests`(HP·레벨·변환), Test 데이터(편차 0, 레벨 정의, CSV의 등급 열), PlayMode(역마차의 편차와 ▲▼, 한 바퀴의 성장 줄).
- 시뮬: 08 §38(편차 0·등급 절반 변형 포함).
- 스크린샷: 새 `_72_settlement_growth`(보고서를 꾸민 한 장, 끝나면 되돌림). 목업 대조로 "Lv"의 색(흰색 → 뼈색: 글 상자의 색)과 줄 간격(29 → 34: `lineSpacing` 21, 첫 줄 y 544)을 고쳤다.

## Open
- 런이 띠 위(균형형 90.0%, 안전형 92.0%): 35단계에서 세기 %와 성장 등급을 함께 맞춘다(08 §38). 사후 검토.

## Verification
- `dotnet run --project Tools/Sim -- validate` OK.
- 체인 `20261018-154449`: EditMode 992/994(저장 버전 18 → 19의 단언, 변환 Test의 헤더 치환이 성장 열로 빗나감 — 둘 다 고침), PlayMode 92/103.
- `20261018-160243` EditMode 994/994 → `20261018-161310` CHAIN OK → 성장 줄의 간격을 고친 뒤 `20261018-163521` CHAIN OK(EditMode 994/994, PlayMode 92/103, 건너뜀 11은 그래픽 스크린샷 Test).
- 스크린샷 `20261018-stats34d`(124장): 역마차의 ▲▼와 정산의 성장 줄을 Round 81 목업과 대조(`impl-coach-spread.png`·`impl-settlement-growth.png`).

## Next Action (제안)
- 35단계(맞추기와 직접 플레이)는 시작 지시를 받은 뒤에. 레버는 세기 %(`StatPowerPercentPerPoint`)와 성장 등급(08 §38의 권장).
