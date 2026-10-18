# Slice D 37단계: 특성의 뼈대와 타고난 특성

Snapshot: 2026-10-19 (완료: 구현·시뮬·체인 OK·목업 대조. 2026-10-19 세션 정리에서 커밋·푸시)

## Goal
용병이 특성을 가진다(Design/16 판정, Design/05 §6): 직업 특성(직업 패시브를 그 이름으로), 타고난 특성(역마차 후보가 0~2개, 시작 로스터는 데이터). 특성이 판정 식에 항을 더하고, 막사·역마차·파티 팝업이 특성 줄과 카드로 보인다(Round 84 판정 "권장안 구현"). 범위는 Architecture/09 "Slice D" 구현 순서 6.

## State
- 2026-10-18 사용자 "권장안 구현"(Round 84 판정 아홉)으로 시작했다. 얻은 특성(정산·빈사·고통/각성)은 38단계, 맞추기는 39단계(옛 37).
- 2026-10-19 세션 정리에서 주제별로 커밋·푸시(Design/16 검토·판정의 기록, Round 84 목업과 함께).

## Done
- 데이터: `TraitData.csv`(타고난 여섯: 매의 눈·서투름 명중 ±5%p, 강골 빈사 −5%p, 허약 최대 HP −10%, 담대함·소심함 오르는 피로 ∓10%; 가중치 10; 짝 매의 눈/서투름·담대함/소심함), `MercenaryData.Traits`(로언 담대함, 아스트리드 강골·서투름, 엘라 허약, 미라 매의 눈·소심함, 카이 없음), `JobData.PassiveName`(선봉·광기·급소·후방 영창·축복), `BalanceData` `RecruitTraitWeight0`~`2`(30·50·20). Generated 다시 만듦.
- Domain: `TraitData`·`TraitKind`·`TraitPolarity`(Data), `TraitBonus`·`TraitRules`(`BonusOf`·`CanTake`·`DrawInnate`), `StatRules.Accuracy`·`Dodge`·`CritChance`·`VirtueChancePercent`·`FatigueGain`, `FatigueRules.DogDeathChancePercent`, `LevelRules.MaxHpOf`의 항(선택 인자 `TraitBonus`). `MercenaryState`·`RecruitState`·`PartyMember`·`ExpeditionMember`의 `Traits`, `BattleUnitSetup.Traits`. `RunRules.NewRun`·`DrawRecruits`(스탯 다음)·`Hire`, 원정의 최대 HP·진입 비용·전투 Setup.
- 저장 22: `MercenaryRecord`·`RecruitRecord`의 `Traits`, 검증(`ToTraits`: 있음·두 번 없음·충돌 없음), `From21To22`(빈 목록), 원정 구성원은 로스터의 것.
- 시뮬: 고용 정책(약점이 적은 후보 먼저, 같으면 장점이 많은), 보고의 특성 줄, 새 파티의 시작 로스터 특성. 08 §41.
- 화면: `TraitChip`(UI 데이터), `TraitChipBase`·`TraitChipView`(아이콘 + 이름)·`TraitIconView`(아이콘만)·`TraitMoreView`("+N"), `TraitLineView`, `TraitTooltipView`; `RosterEntryView`·`CandidateView`·`PartyListRowView`의 패시브 글 → 특성 줄, `PartyRowSlotView`의 아이콘 줄; 빌더 `UiPrefabSetup.Traits.cs`(창마다 카드 `Barracks`·`Coach`·`Party` + `TraitTooltip`), `UiBuild.SetFloat`; `UiText.TraitChips`·`MoreChip`·`TraitEffect`·`Signed`; 팔레트 `TraitJob`·`TraitStrength`·`TraitWeakness`·`TraitIconFill`·`TraitMore`; 문구 `Trait.*` 열다섯. 아이템 카드의 명중·치명타(전투 둘, 노드 맵 — 노드 맵은 패시브도), 전투 칸의 빈사 확률이 특성을 셈.
- Test: EditMode `TraitRulesTests`(8), `RunSaveMapperTests` 셋(22의 저장·검증·21 → 22), `ShippedDataTests` 둘, `StaticDataValidationTests` 둘, `StaticDataTransformerTests`(특성·직업 특성 이름·시작 로스터의 열), `TestCsv`·`TestData`(특성의 CSV와 Test 데이터, 가중치 키); PlayMode `Lobby_Barracks_AndTheStagecoach_ShowTheTraits_AndTheirCards`, 파티 팝업 Test의 목록 줄, 빈사 표시 Test의 기댓값(특성까지 셈), 스크린샷 `_75_barracks_trait_card`.
- 문서: Design/05 §6(구현 줄)·07 §4·08 §41·16 §8, Architecture/05·07·08·12, Roadmap(37단계 절, 사후 검토 대기의 "Round 84·37단계의 권장안 세부"), `ArtPipeline/Archive/84-trait-display/README.md`(판정됨).

- 2026-10-19 사용자 "특성은 마우스 우클릭시 툴팁으로 설명 나오게": 카드는 칩의 우클릭에 열리고 다음 누름에 닫힌다(`TraitChipBase.OnPointerClick`·`RightClickNow`, `TraitTooltipView.Update`의 `PointerPress`). PlayMode·스크린샷 Test를 우클릭으로.

## Open
- 38단계(얻은 특성)와 39단계(맞추기와 직접 플레이)는 시작 지시를 받은 뒤에.
- 특성 아이콘의 그림(18장)과 방랑자 그림(Round 83)은 이미지 API를 요청받으면. 그림 호출 누계 약 $8.41 / 상한 $10.
- 균형형 런의 시드 흔들림이 크다(시드 1·2·3이 75.7~85.0%): 39단계는 시드 여럿이나 1,000런으로.

## Verification
- `dotnet run --project Tools/Sim -- validate` OK. 시뮬 전후는 08 §41.
- 체인 `20261018-233410`: EditMode 941/1003(실패 62: 변환 Test의 CSV 묶음에 새 열·파일이 없음 60, 시작 로스터의 기댓값 차례 1, 프리팹 검사 — 빈 참조 1), PlayMode 93/105(실패 1: 빈사 표시의 기댓값이 특성을 셈하지 않음). 모두 고쳤다(칩을 셋으로 나눔 등). `20261018-235233`은 Test 한 줄의 컴파일 오류(튜플의 null).
- 체인 `20261018-235342` CHAIN OK(EditMode 1003/1003, PlayMode 94/105 — 건너뜀 11은 그래픽 스크린샷 Test). 스크린샷 `20261019-traits37`을 Round 84 목업과 대조: 카드의 판·테·꼭지가 목업과 다름(→ 놋쇠), 역마차·목록의 이름이 비고 무대 카드의 아이콘이 직업 글을 덮음(→ `TextWidth.Of`, PlayMode에 그 둘의 단언).
- 체인 `20261019-001729` CHAIN OK(EditMode 1003/1003, PlayMode 94/105), 스크린샷 `20261019-traits37b`(130장)이 목업과 같음 — `impl-*.png`(Round 84 폴더). en-US도 줄에 들어감(Iron Bones·Rear Casting).
- 우클릭으로 바꾼 뒤 체인 `20261019-080400` CHAIN OK(EditMode 1003/1003, PlayMode 94/105), 스크린샷 `20261019-traits37c`: 우클릭으로 연 카드 장면(`_75`)이 그 전과 같은 그림.

## Next Action (제안)
- 38단계(얻은 특성: 정산의 계기 셋, 확률의 데이터, 정산의 "새 특성" 줄 — Round 84 목업 `mock-settlement.png`)는 시작 지시를 받아서.
