# Slice D 42단계: 장비 요구 스탯

Snapshot: 2026-10-20 (규칙·데이터·저장 25·시뮬·Test·문서와 화면(Round 89 "권장안 구현")까지 구현. 체인 `20261020-191647` CHAIN OK. 2026-10-20 세션 정리에서 주제별 커밋·푸시: 기획·Architecture `b471318` / 구현 `24c60e1` / Round 89 화면 `dbc5113` / Roadmap·Handoff)

## Goal
무기 장비와 방어 장비가 저마다 요구 스탯과 요구값을 가지고(Design/02 §4 "장비 요구 스탯"), 미달인 용병의 보드에는 어느 길로도 놓이지 않는다. 모든 용병이 자기 기본 무기를 든다(직업 기본값·역마차의 후보·저장 25). 카드가 요구를 적고 거절 그림자가 까닭을 말한다(Round 89 뒤). 시뮬이 전후를 같은 시드로 보고한다(08 §49).

## State
- 2026-10-20 상태 복구(41단계까지 완료·`ad83ec3` 푸시) → 사용자 지시 "장비 요구 스탯의 설계를 … 보고하고 … 승인을 기다려" + 추가 검토 사항(기본 장비 요구 5, 전사는 역마차에서 힘 5 이상 보장) → 설계 보고(판정할 것 아홉) → 판정 **"3. 모든 무기 장비와 방어 장비(버클러, 철 투구 포함) / 나머지 권장안 구현"** → 새 42단계로 구현(맞추기와 직접 플레이는 43).
- 규칙·데이터·저장·시뮬·Test·문서를 구현하고 전체 체인 CHAIN OK(`20261020-181229`) 뒤 Round 89 목업(`ArtPipeline/Archive/89-item-requirement/`)을 올렸고, 같은 날 사용자 **"권장안 구현"**(A안 사실 줄의 마지막·주인 없는 카드는 파티 기준 색·거절 그림자의 글)으로 화면을 구현했다. 체인은 아래. 42단계 완료(이 Handoff는 completed로).

## Done
- 기획: Design/02 §4 "장비 요구 스탯"(규칙·기본 무기 5·주 스탯·출발값 표), 03 §5(놓기 조건), 04 §6(후보의 올림), 05 §2(직업 기본값이 기본 무기의 요구를 채움, Slice E 목록에서 뺌), 07 §4(열), 00 "Slice D"(판정 표 아홉).
- 데이터: `ItemData.csv`의 선택 열 `ReqStat`·`ReqValue` — 무기·방어 장비 스물둘 모두(기본 무기 다섯은 직업의 주 스탯 5: 롱소드·전투도끼 힘, 장궁 민첩, 화염 지팡이 지식, 치유의 지팡이 지혜; 메이스·창·버클러·철 갑옷·글라디우스 힘 5, 단검 민첩 5, 미늘창·감독관의 망치·심연의 식칼 힘 7, 손아귀 힘 6, 등불 지팡이 지식 5, 포효·울부짖음 지식 7, 철 투구·쥐 발톱·녹슨 칼 힘 3, 조잡한 활 민첩 3). Generated `ItemData.json` 다시 만듦.
- Data: `ItemData.ReqStat`(`Stat?`)·`ReqValue`·`HasRequirement`(짝, 무기·방어 장비만), `ItemMapper`의 선택 열, `StaticData.CheckReferences`의 검사 셋(범위, 직업 기본값 ≥ 기본 무기의 요구 — 스탯 없는 직업은 `StatBaseline`, 레시피의 결과 ≤ 바탕).
- Domain: `StatRules.MeetsRequirement(스탯, 아이템)`·`MeetingBaseWeapon(data, 직업, 스탯)`, `ExpeditionRules.MeetsRequirement(state, 구성원, 아이템)`를 `CanPlaceOffer`·`CanPlaceItem`·`CanMoveItem`(같은 보드에 다시 놓기 포함)·`CanPlaceFromInventory`와 합침의 질의 `OfferMergesAt`·`MergesAt`에, `RunRules.DrawRecruits`의 올림(편차 다음, 난수 없음).
- 저장: 25 — `RunSaveData.CurrentSchemaVersion`, `RunSaveMigrator.From24To25`(로스터·후보의 그 스탯을 요구값까지, `LiftToBaseWeapon`). 모양은 24 그대로.
- 시뮬: `Tools/Sim` `Policies`(`MemberWithRoomFor`·`MemberWithStarRoomFor`·`FindMergeTarget`이 요구를 채우는 구성원만, `PickLoot`의 `keptOffByRequirement`), `Simulations`(보고 "kept off a board by a stat requirement").
- Test: EditMode 새 `ItemRequirementTests`(9), `DefinitionTests`(짝·분류), `StaticDataValidationTests`(직업 기본값·범위·레시피), `StaticDataTransformerTests`(열 둘), `ShippedDataTests`(모든 장비의 요구, 기본 무기 5와 주 스탯, 글라디우스 = 롱소드), `RunSaveMapperTests`(24 → 25, 버전 단언은 `CurrentSchemaVersion`). `TestData.Item`의 `reqStat`·`reqValue`. PlayMode `UiFlowTests` 여섯의 시나리오를 규칙에 맞춤(녹슨 칼, `RowOfAWielder`).
- 화면(Round 89 "권장안 구현"): 문구 `Item.Requires`·`Board.GhostRequires`(`UI_StaticText.csv`, `UiKeys`), 카드의 마지막 사실 줄 `UiText.RequirementFact`(주인이 있으면 주인, 없으면 `PartyWields` — 원정의 산 구성원 가운데 하나라도 채우는지; 적의 카드는 `ownerIsEnemy`로 흰색 — `ItemTooltipView.ShowBeside`·`UiText.ItemCard`·`BattleScreen`), 거절 그림자의 글(`BoardHand.GhostFor` → `ExpeditionManager.MeetsRequirement`), 그림자의 글은 모두 흰 글(`GridBoardView`). PlayMode `Requirement_TheCardSaysWhatTheItemAsks_AndTheGhostSaysWhyABoardRefusesIt`.
- 문서: Architecture/05(열·검사), 07(저장 25), 08("장비 요구 스탯"), 09(Slice D 표·구현 순서 11 → 42·12 → 43·Acceptance 5-5), 12("장비 요구 스탯": 구현한 화면), CLAUDE.md §9(32~43, Round 89), Design/00 판정 표 8, Roadmap(42 완료·43 대기, 맨 위 요약, 날짜 표, 사후 검토 대기), Round 89 README(판정·구현).

## Open
- Round 89는 판정·구현 끝. 스크린샷은 뽑지 않았다(글줄은 PlayMode Test가 지킨다). 사용자 확인은 직접 플레이에서(43단계).
- 그림자의 글이 모두 흰 글이 된 것(거절 글이 처음 생겨, 붉은 글의 옛 규칙을 목업대로 통일)은 권장안 세부(사후 검토).
- 시뮬 수치: 08 §49(아래). 요구로 마법사·사제가 물리 드랍을 못 들어 띠 밖의 수치가 더 내려간다 — 43단계의 입력.
- 미달인 용병이 든 아이템(옛 저장): 집어 들면 제자리에 다시 놓지 못하고 인벤토리로 간다(의도한 동작, 02 §4·Architecture/07).
- 방랑자 그림(Round 83)·특성 아이콘은 이미지 API 요청 시. 최대 피로(41단계 판정 4)는 정신붕괴 개정과 함께.

## Verification
- `dotnet build Tools/Sim -c Release` 경고 0·오류 0. `transform`(ItemData.json 다시 씀) → `validate` OK(직업 기본값이 기본 무기의 요구를 채움).
- 시뮬 전후(08 §49): 저장소 빌드, 같은 시드(광산 원정 1,000 균형형 시드 1, 런 300 균형형·안전형 시드 1·2·3, 심연 500). "전"은 `ad83ec3`의 빌드(§48의 후와 같은 수). 광산 2.9 → 3.4%(못 놓은 드랍 0.44/원정, 끝 보드의 방어구 1.12 → 0.77), 균형형 4.3·4.0·6.3 → 4.0·6.7·6.7%, 안전형 8.0·12.0·11.0 → 11.7·9.3·10.0%, 심연 0.0 → 0.0%: 시드의 흔들림 안. 출력은 scratchpad `sim-before`·`sim-after`(Git 밖).
- 체인 `20261020-174334` editmode: 1057 중 1052 통과, 실패 5는 저장 버전을 24로 고정한 옛 단언(`RunSaveMapperTests`) → `CurrentSchemaVersion`으로 고침. 전체 체인 `20261020-175057`: EditMode 1057/1057, PlayMode 108 중 90 통과·6 실패 — 모두 `UiFlowTests`가 단검(민첩 5)·기본 무기를 요구를 못 채우는 용병(사제·전사의 민첩 4)의 보드에 놓던 시나리오 → 2×1 무기를 녹슨 칼(힘 3)로, 무기를 옮기는 보드를 요구를 채우는 용병의 것으로(`RowOfAWielder`) 고침.
- 전체 체인 `20261020-181229` **CHAIN OK**(규칙까지): setup OK, sim OK, EditMode 1057/1057, PlayMode 96/108(건너뜀 12는 그래픽 스크린샷 Test).
- 화면까지 넣은 전체 체인 `20261020-191647` **CHAIN OK**: setup OK(문구 표에 새 키 둘), sim OK, EditMode 1057/1057, PlayMode 97/109(새 Test 하나 포함, 건너뜀 12는 그래픽 스크린샷 Test). `Assets/InitTestScene*` 없음. `git diff --check` 깨끗. 스크린샷은 뽑지 않았다.

## Next Action (제안)
- 세션 정리(사용자 지시 때): 주제별 커밋 — 기획·Architecture 문서 / 42단계 구현(데이터·Data·Domain·저장 25·시뮬·Test)과 Unity 생성물 / Round 89(화면·목업 기록) / Roadmap·Handoff — 와 푸시.
- 그 뒤 **43단계 맞추기와 직접 플레이**(시작 지시 대기): 08 §48의 스윕 묶음(판정 기준 7·HP 점당 7·광산 복리 4%·세기 기준 4)에 §49의 요구 효과를 더해 띠 안으로, 둘째 묶음의 문턱 21 → 26층, 요구값(올리면 마법사·사제의 보드가 빔)도 레버. 직접 플레이에서 요구 줄·그림자의 글·역마차의 보장을 본다.
