# Slice D 32단계: 스탯의 뼈대와 세기

Snapshot: 2026-10-18 (완료: 구현·시뮬·체인 OK·목업 대조. 커밋 안 함. 다음은 33단계 — 시작 지시 대기)

## Goal
용병마다 스탯 여섯이 있고(직업 기본값), 힘·민첩·지식·정신력이 아이템의 세기를, 체력이 최대 HP를 데이터의 식으로 바꾼다. 저장 18이 이어 가고, 시뮬이 같은 시드로 전후를 잰다. 화면은 Round 81의 판정대로 막사·역마차·파티 팝업·아이템 카드에 스탯을 보인다. 범위는 Architecture/09 "Slice D" 구현 순서 1.

## State
- 2026-10-18 사용자 "권장안 구현"으로 Round 81(일곱 모두 권장안)을 판정받고 32단계를 시작해 구현했다. 체인 `20261018-110116` CHAIN OK(setup·sim OK, EditMode 975/975, PlayMode 91/102 — 건너뜀 11은 그래픽이 있어야 하는 스크린샷 Test), 그래픽 스크린샷 `20261018-stats32c`(11/11, PNG 121장).
- 커밋 안 함(지시가 있을 때만). 작업 트리에 31단계 문서 변경도 함께 있다.

## Done
- 데이터: `Stat`·`StatBlock`(F1.Data), `JobData.Stats`(null이면 기준값), `ItemData.ScalesWith`(Str·Dex·Int·Spi만, 효과가 있는 아이템만), `BalanceData`의 `StatBaseline`·`StatMin`·`StatMax`·`StatPowerPercentPerPoint`·`HpPercentPerConPoint`와 검증, 데이터셋의 직업 스탯 범위 검사, CSV(JobData 열 여섯, ItemData `ScalesWith`, BalanceData 다섯), `CsvRow.OptionalEnum`, Generated JSON.
- Domain: `StatRules`(BaseOf·PowerPercent·HpPercent·Scale·ScaleStacks), `MercenaryState`·`RecruitState`·`PartyMember`·`ExpeditionMember`·`BattleUnitSetup`의 `Stats`, `LevelRules.MaxHpOf`(체력 % + 레벨 %), `BattleEngine.Activate`(피해는 치명타 뒤·패시브 앞, 회복·보호막 내림, 화상 반올림), `RunRules`(새 런·후보·고용·출발).
- 저장 18: `StatsRecord`(용병·후보), `From17To18`(직업 기본값), 범위 검증, 구성원은 로스터의 스탯.
- 시뮬: 원정 보고의 파티 스탯 줄, 런 보고의 끝 평균 스탯 줄. 전후(HEAD 복사본)와 세기만·HP만: Design/08 §36.
- 화면: `RosterEntryView`(열 버튼·[제외] 삭제, 칸 여섯), `CandidateView`(스탯 줄, 높이 150), `PartyRowSlotView`(2×3), `PartyListRowView`(포인터 들고 남) + 새 `StatTooltipView`, `ItemTooltipView`·`UiText`(주인의 스탯으로 셈한 값, 꼬리 `Item.StatScale`), 빌더(Lobby·Expedition), 문구(`Stat.*`, `Item.StatScale`, `Expedition.StatTooltip`; `Lobby.Remove` 삭제).
- Test: 새 `StatRulesTests`·`StatBattleTests`, `RunSaveMapperTests`(18의 왕복, 17 → 18, 범위 밖·빠짐 다섯), `StaticDataTransformerTests`(열과 축), `ShippedDataTests`(직업 스탯·기본 무기의 축), PlayMode(막사의 칸 여섯 Test가 열 버튼 Test를 대신함, 역마차의 스탯 줄과 고용, 팝업의 2×3과 툴팁, 카드 단정 둘, 파티 채우기 도우미).
- 문서: Design/00·02·04·05·07·08 §36, Architecture/05·07·08·12, Roadmap(Round 81의 승인, 32단계, 사후 검토), Round 81 README(판정됨), Handoff d31 → completed.

## Open
- 시뮬이 띠 밖(균형형 94.3%, 안전형 91.3%): 권장은 33단계 뒤에 다시 재고 35단계에서 맞추기(08 §36). 사용자 판단 대기는 아님(사후 검토).
- 목업 대조는 끝났다(Round 81 README "구현"): 첫 대조에서 빠진 툴팁 줄의 파란 테두리를 더했다.

## Verification
- `dotnet run --project Tools/Sim -- validate` OK. 체인 `20261018-103344`(EditMode 974/975: 제 Test의 예외 Type) → `20261018-103435`(EditMode 975/975, PlayMode 91/102) → 테두리를 더한 뒤 `20261018-110116` CHAIN OK(setup·sim·EditMode 975/975·PlayMode 91/102).
- 스크린샷 `20261018-stats32c`: `ko_57_lobby_barracks`·`ko_58_lobby_coach`·`ko_66_expedition_party`·`ko_70_expedition_stat_tooltip`(새)·`ko_47_map_star_card`를 목업과 대조.
- 시뮬(08 §36): HEAD 복사본의 전이 §35의 후와 같음(79.7%)을 확인한 뒤 같은 시드로 후·세기만·HP만을 쟀다. `git diff --check` 깨끗함.

## Next Action (제안)
- 사용자의 33단계 시작 지시를 받으면: 이 Handoff를 completed로 옮기고 `d33-…`을 만든 뒤 명중·회피(02 §5)·치명타 + 민첩·빈사 − 체력·각성 + 정신력·인내의 피로(04 §3)와 빗나감의 표시(Round 81 판정 6)를 구현하고, 같은 시드로 08 §36의 띠를 다시 잰다.
