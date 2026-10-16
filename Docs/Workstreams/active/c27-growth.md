# Slice C 27단계: 성장 — 레벨·경험치·최대 HP 보너스

Snapshot: 2026-10-16 (27~29단계 일괄 승인 아래 권장안으로 구현. 체인은 아래 Verification)

## Goal

Architecture/09 "Slice C" 구현 순서 3이 참이다: 원정에서 살아 돌아온 파티원이 경험치를 받고 레벨이 오르면 최대 HP가 데이터만큼 는다. 로스터 줄·띠·후보·원정 화면의 칸이 레벨을 보이고, 시뮬이 HP 대 무기 세기를 보고한다.

## State

- 2026-10-16 사용자 "이어서 … 슬라이스 완성을 위한 나머지 절차를 네 권장안에 따라 완성할 때까지 알아서 진행(필요시 이미지 api 사용)" → Roadmap "27~29단계의 일괄 승인". 27단계를 권장안으로 구현했다.
- 권장안으로 정한 세부는 Roadmap "사후 검토 대기"의 "27단계의 권장안 세부"와 Design/05 §2.
- 다음은 28단계(최종 던전과 최종 보스): 같은 일괄 승인으로 이어서.

## Done

- 데이터: `LevelData.csv`(새 정의, `Level`·`ExpToNext`·`MaxHpBonusPercent`, 이름 열 없음: 1→2 15, 2→3 25, 3→4 40, 4→5 60, 5→6 90, 6 마지막; 보너스 0/10/20/30/40/50%), `LevelData` 정의와 `StaticData.Levels`·`MaxLevel`·`LevelAt`(사다리 검사: 1부터 빈틈없이, 마지막만 0, 1레벨은 0%, 내려가지 않음), `StaticDataFiles.Level`, Loader·Transformer(`LevelMapper`), `BalanceData` `ExpPerFloor` 1·`ExpClear` 5, Generated 둘.
- Domain: `MercenaryState.Level`·`Exp`, `PartyMember.Level`, `ExpeditionRules.Create`가 `LevelRules.MaxHpOf`로 구성원의 최대 HP를 세움, `LevelRules`(`MaxHpOf`·`ExpFor`·`Gain`·`ExpToNext`), `RunRules.Settle`이 살아 돌아온 파티원에게 같은 경험치를 주고 `SettlementReport.ExpEarned`·`LevelUps`, 새 런·고용은 레벨 1.
- 저장 15: `MercenaryRecord.Level`·`Exp`, `From14To15`(모두 1·0), 검증(사다리 안, 경험치는 `ExpToNext` 아래·마지막은 0).
- 화면: 띠의 칩에 "Lv N"(열 표시는 직업 줄 오른쪽으로), 막사 줄의 레벨 블록(`RosterEntryView.ShowLevel`: "Lv 2"·"경험치 4/25"·막대, 마지막 레벨은 "경험치 최대"), 후보의 "Lv 1 · 피로도 0/200", 원정 화면 칸의 "Lv N", 정산 화면의 "경험치 +N · 레벨 업: 로언 Lv 2". 문구 `Lobby.Level`·`Exp`·`ExpMax`, `Settle.Exp`·`LevelUps`·`LevelUp`.
- 시뮬: `run`의 레벨 보고(레벨 업, 끝의 평균·최고 레벨, 마지막 레벨에 닿은 런). HP 대 무기 세기는 scratchpad 복사본의 환경 변수 변형으로 쟀다(Design/08 §25: 62.3% 대 63.0%, HP형 유지).
- Test: EditMode `LevelRulesTests`(다섯: 사다리, 오르기와 이월과 마지막, HP 보너스와 출발·전투 Setup, 정산의 경험치와 레벨 업과 죽은 용병, 고용·새 런의 레벨 1), `RunSaveMapperTests`(14 → 15, 왕복, 깨진 런 다섯), `StaticDataTransformerTests`(사다리 매핑과 거부 일곱), TestData·TestCsv의 사다리 셋. PlayMode `Lobby_ShowsTheLevelAndExperience_OnTheChipAndTheBarracksLine`(새), 원정 화면 칸의 "Lv 1", 마을 Test의 후보·칩 레벨, `OneLap`의 정산 경험치 줄; 스크린샷은 한 용병을 2레벨로 꾸며 찍음.
- 문서: Architecture/05(`LevelData.csv`), 07(15), 08("성장"), 11(정산), 12(칩·줄·후보·칸·정산), Design/00·05 §2·07 §4·08 §25, Roadmap.

## Open

- 사후 검토: 사다리와 경험치의 출발값, HP형 유지(무기형과 승리 같음), 레벨 표시의 자리(목업 Round 71의 막사 블록을 따름).
- 6레벨에 닿는 런이 없다(끝의 최고 레벨 3쯤): 사다리의 위쪽 값은 29단계의 띠와 함께.

## Verification

- `transform`·`validate` OK(BalanceData·LevelData). 시뮬 Design/08 §25.
- 체인 `20261016-220641`(setup·sim OK, EditMode 910/910, PlayMode 87/97 — 건너뜀 10은 그래픽이 있어야 하는 스크린샷 Test). 그래픽 스크린샷 `20261016-222039`(10/10, PNG 107장): 막사 줄의 레벨 블록(목업 `mock-town-painted-enter`의 자리: "Lv 2 · 경험치 4/25"와 막대), 띠의 칩 "Lv 2", 후보의 "Lv 1 · 피로도 0/200", 원정 칸의 "Lv 1", 정산의 "경험치 +N"을 봤다.

## Next Action (제안)

- 세션: 체인과 스크린샷(막사의 레벨 블록을 목업 `mock-town-painted-enter`와 대조) 뒤 커밋하고 28단계로.
