# Slice D 36단계: 직업 재편

Snapshot: 2026-10-18 (완료. 커밋 안 함)

## Goal
직업을 다섯(전사 롱소드·바이킹 전투도끼·방랑자 장궁·마법사 화염 지팡이·사제 치유의 지팡이)으로 바꾼다(Design/15 판정, 05 §1). 성기사·마검사를 지우고, 시작 로스터는 다섯(카이 전사, 세드릭 삭제), 옛 저장은 읽지 않는다. 범위는 Architecture/09 "Slice D" 구현 순서 5.

## State
- 2026-10-18 사용자 "직업 재편" → "수행"으로 시작했다. 방랑자 그림은 이미지 API 허락이 없어 마검사 그림을 임시로 쓴다. 커밋 안 함(31~35단계의 변경도 작업 트리에 함께 있다).

## Done
- 데이터: `JobData.csv` 다섯(넷은 옛 행의 값과 그림, 방랑자 새로), `MercenaryData.csv` 시작 다섯, 장궁 상점 가중치 3·드랍 0. Generated 다시 만듦(validate OK: 5 jobs).
- Domain: `PassiveEffect.CritChancePercent`(`Always`·`None`·`Self`만), `StatRules.CritBonusPercent`, `CritChance(…, bonusPercent)`, `BattleEngine.RollCritical`.
- 화면: 아이템 카드의 치명타가 주인의 패시브를 셈(`UiText.ItemCard`·`ItemTooltipView`의 `ownerPassive`, `BattleScreen`의 두 곳). `ItemSlotView.Rim`(Test용).
- 저장 21: `RunSaveMigrator`는 범위만 검사(`OldestReadableVersion` 21), 12~20의 변환 삭제, `LevelRules.ExpectedGrowth` 삭제.
- 그림: 성기사 다섯 장(몸·공격·피격·붕괴·각성)을 `Assets`에서 지우고 `ArtPipeline/Archive/82-barracks-levelup/removed-paladin/`에, `Rosters`의 성기사 행 삭제, 메이스 아이콘의 참고 그림 경로를 사본으로.
- 시뮬: `ParseParty`(직업 id → 안 쓴 시작 용병, 없으면 이름 풀), 기본 파티 `warrior,ranger,priest,mage`. 08 §40.
- Test: `ShippedDataTests`(다섯 직업·로스터·장궁 2×2·드랍 목록), `StatJudgementTests`(방랑자 패시브), 저장 변환 Test 여덟 삭제·버전 21, PlayMode(직업 id, 로스터 수, 자세 이름은 그림의 키로, 숫돌 Test는 근접 무기의 열 `FlatRow(melee: true)`, 상점의 가방은 테두리).
- 문서: Design/02 §4·§7, 03 §5, 05 §1·§2, 08 §40, Architecture/05·07·08·13, Roadmap.

## Open
- 방랑자 그림(Round 83): 이미지 API 호출은 사용자의 요청으로. 그 전에는 마검사의 그림.
- 런이 띠 위(균형형 85.0%, 안전형 88.0%), 심연의 새 파티가 약함: 37단계.

## Verification
- `dotnet run --project Tools/Sim -- validate` OK(5 jobs). 체인 `20261018-204228`: EditMode 987/988(새 Test의 공격 아이템 치명타 — 데이터가 막는 경우라 단언을 뺌), PlayMode 89/104(실패 넷: 자세 이름·로스터 7/8·숫돌의 열·상점의 가방 — 모두 Test의 가정, 고침). 고친 뒤 `20261018-210059` CHAIN OK(EditMode 988/988, PlayMode 93/104, 건너뜀 11은 그래픽 스크린샷 Test).
- 스크린샷 `20261018-jobs36`: 막사의 다섯 줄(바이킹·사제·전사 둘·마법사), 역마차의 방랑자 후보와 패시브 글, 전투의 전사 둘(기사의 그림). `impl-jobs-barracks.png`·`impl-jobs-coach.png`(Round 82 폴더).

## Next Action
- 37단계(맞추기와 직접 플레이)는 시작 지시를 받은 뒤에. 방랑자 그림(Round 83)은 이미지 API 허락이 있으면.
