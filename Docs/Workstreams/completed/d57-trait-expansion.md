# Slice D 57단계: 특성의 확장

Snapshot: 2026-10-24 저녁. 사용자 "특성 추가 검토 …" → 제안서 `Docs/Design/39_Trait_Expansion_Proposal.md`(판정 열여덟) → "전사 거인, 왜소 한개씩 생성 …" → Round 105(`ArtPipeline/Archive/105-warrior-builds/`) → "이전 그림으로 목표 키" → "공격모션, 피격 모션만 생성 …" → **"게임에 연결"** · **"특성도 구현"** → 여자 전사·그림 고르기·거인의 콧볼 → **"세션 정리"**: 커밋 `2b388ef`(기획·Architecture)·`f0feb29`(구현)·`fab6ff4`(Round 105 그림), 푸시. 완료.

## Goal
Design/39의 권장안으로 체격·성격·경험·붕괴 특성과 새 효과를 넣고, 전사의 거인·왜소 그림을 게임에 연결한다. 훈련은 58단계(막사의 목업 뒤).

## State
- 판정: 열여덟 모두 권장안(사후 검토). 지시의 체격과 그림의 연결은 사용자의 것. 기록: Design/39 §9, 00 "Design/39의 판정", 05 §6(2026-10-24 줄), 04 §3·§6, 10 §2, 08 §64.
- 데이터: `TraitData.csv` 서른일곱(새 스물넷, 허약은 체격으로) + 열 `Build`·`Rare`·`Figure`·`FigureScale`·`SourceState`·`Str`~`End`·`GrowthPercent`·`DamagePercent`·`HealTakenPercent`·`AllyFatigueAtStart`. `BalanceData`의 `BuildWeightNone` 50·`TraitChanceAllyDiedPercent` 30. Generated JSON 둘.
- 코드:
  - Data: `TraitData`(새 열과 검사, 체격 둘의 `Clashes`, `ShiftsStats`), `TraitSource.AllyDied`, `BalanceData` 키 둘, `StaticData.BuildTraits`·`InnateTraits`(체격 뺌)·`SourceState` 검사, `ArtAddress.BuildFigure`, `TraitMapper`.
  - Domain: `TraitRules.DrawBuild`·`BuildOf`·`Shifted`·`ShiftOf`, `DrawInnate(…, held)`, `SourcesOf(…, allyDied)`, `DrawAcquired(…, stateId)`, `TraitBonus`의 넷, `RunRules`(후보: 스탯 → 체격 → 더함 → 요구 바닥 → 타고난; 시작 로스터의 이동; 정산의 동료 죽음·상태), `LevelRules.Grow`(성장 %), `BattleEngine`(주는 피해·받는 회복·동료 피로 `BattleEvent.FatigueTrait`).
  - UI: `ExpeditionArt`(체격의 그림·자세·배율, `FigureAddressOf`·`ScaleOf`·`BuildAddresses`), `BattleScreen`·`PartyColumnView`·`PartyRowSlotView`·`ExpeditionScreen`(용병의 그림과 배율), `UiText.TraitEffect`(스탯 먼저, 치명타 배수·성장·주는 피해·받는 회복·동료 피로)·`TraitChips`(드문 특성, 동료를 잃고), `UiKeys.Trait` 여덟과 `UI_StaticText.csv`.
  - Editor: `ArtSetup`(직업 × 체격의 그림·자세 등록).
- 그림: `Assets/@Art/Unit/Job/knight_{giant,small}.png`, `Assets/@Art/Pose/Job/knight_{giant,small}_{attack,hit,resolute,broken}.png`(공통 캔버스, 거인은 Height 69·화면 138%, 왜소 Height 62). 각성·붕괴는 대기 그림(사용자). 유료 호출 Round 105 8회 약 $0.25 → 장부 461회 약 $13.05(상한 $20). 이어서 여자 전사 셋(보통·거인·왜소의 대기 그림, 3회 약 $0.10; 사용자 "더 여성성 부각"으로 거인·왜소 v2 2회 → 466회 약 $13.21) — 게임에는 연결하지 않음(용병에게 성별이 없고 직업마다 그림 하나: 고르는 규칙이 판정 거리).
- 문서: Design/39 §9, 00, 04, 05, 08 §64, 10 §2, Architecture/05·08·09·12·13, CLAUDE.md §9, Roadmap 57단계.
- Test: EditMode `TraitExpansionTests`(새), `StaticDataValidationTests`(57단계 검사), `ShippedDataTests`(57단계의 값, 타고난 목록), `StaticDataTransformerTests`(새 열), `TestCsv`·`TestData`(열과 키). PlayMode `UiFlowTests.Builds_AGiantAndASmallWarrior_…`, 후보의 특성 수 단언, 스크린샷 `TheBuilds_Korean`(`ko_106`~`ko_110`).
- 다른 세션의 미커밋 작업(건드리지 않음): Design/33·37, 00의 37 줄, Round 98·102, `calls.csv`(이 세션의 8줄도 같은 파일), `d-story-review.md`, `73-abyss/rejected/`, `99-linette/refs/`. 세션 전부터 바뀐 설정 셋.

- **이어서: 여자 전사와 마검사, 그림을 고르는 규칙**(사용자 "이전 마검사 이미지도 전사로 편입 / 전사를 갑옷 … 자세를 꼭 동일하게 한정하지 말것 / 여자 전사 거인 및 왜소 다시 생성(더 여성성 부각)" → 판정 "이름의 성별로" · 왜소 v2 "이대로 확정"):
  - 데이터: `MercenaryData.csv`의 `Gender`(마흔다섯, 여 스물일곱)·`Figure`(카이 `unit/job/spellblade`), `JobData.csv`의 `FiguresMale`·`FiguresFemale`(전사만). Generated JSON 둘.
  - 코드: `Gender` enum, `JobData.FiguresMale`·`FiguresFemale`·`FiguresFor`·`HasFigure`, `MercenaryData.Gender`·`Figure`, `CsvRow.TextList`, `StaticData`의 시작 그림 검사, `RunRules.DrawFigure`(후보의 마지막 굴림)·`Hire`·`NewRun`, `MercenaryState`·`RecruitState`·`PartyMember`·`ExpeditionMember`의 `Figure`, 저장 29(`RunSaveMapper.ToFigure`, `RunSaveMigrator` 28 → 29), `ExpeditionArt.FiguresOf`·`BaseFigureOf`·`FigureAddressOf(…, figure, …)`, `BattleScreen.MemberOf`, `PartyColumnView`·`ExpeditionScreen`, `ArtSetup`(성별별 그림과 그 체격).
  - 그림: `spellblade` 다섯, `swordswoman`·`_giant`·`_small`과 자세 넷씩(이어서 확정본과 공격·피격으로 바꿈).
  - Test: EditMode `TraitExpansionTests`(그림의 굴림·고용, 바탕 그림의 체격), `StaticDataValidationTests`(그림 목록·시작 그림), `ShippedDataTests`(전사의 그림·성별), `StaticDataTransformerTests`·`TestCsv`(새 열), `RunSaveMapperTests`(그림의 저장·모르는 그림, 버전 29). PlayMode `Builds_AWarriorStandsInItsFigure_…`, 직업 그림을 가정하던 셋을 용병의 그림으로, 스크린샷 `TheBuilds_Korean`(기사 거인·마검사·여자 전사 거인·왜소).
  - 시뮬: 원정 그대로, 런은 후보의 굴림 하나만큼 흔들림(균형형 57.1%, 안전형 73.9%).

## Verification
- 시뮬 08 §64: 광산·심연 원정은 전과 바이트 단위로 같다. 런 균형형 54.9 → 56.7%, 안전형 73.3 → 72.5%(시드의 흔들림 안). 전은 HEAD 코드의 scratchpad 복사본(`base57/`).
- 전체 체인 `20261024-192521`: setup·sim OK, EditMode 1172/1173(새 Test `ATraitsGrowth_…`가 범위 밖 값 −200을 써서 실패 — −100으로 고침), PlayMode 106/124(건너뜀 18은 스크린샷, 새 `Builds_…` 통과). EditMode 다시 `20261024-195522` **1173/1173**. `Assets/InitTestScene*` 없음.
- 스크린샷 `20261024-d57`(`TheBuilds_Korean`만, 5/5): 전투는 목업 `mock-battle.png` 안 A와 같은 자리·크기(거인이 왜소의 검 위로), 카드의 "타고난 특성 · 장점 · 드문 특성"·"힘 +2 · 민첩 -2 · 체력 +2", 노드 맵·파티 팝업도 같은 배율. 설정 셋은 찍기 전과 같은 해시(세션 전부터의 diff 그대로). 스크린샷의 로언 스탯은 무대에 특성만 넣은 것이라 이동이 없다(실제 후보는 더한 값).

- 이어서(그림 고르기): 전체 체인 `20261024-201133` setup·sim OK, EditMode 1177/1178(`StoryFlowTests.Migrate_From27_HasHeardEveryDialogue`가 버전 28을 단언 — 지금 버전으로 고침), PlayMode 106/124. EditMode 다시 `20261024-204339` **1178/1178**. 스크린샷 `20261024-d57b`(`TheBuilds_Korean` 5/5): 로언 기사 거인·카이 마검사·요한나 여자 전사 거인·그레타 여자 전사 왜소(뒷열의 롱소드가 어두운 것은 앞 두 열 무기의 꺼짐 표시). 설정 셋은 해시 그대로, `Assets/InitTestScene*` 없음.

- 이어서(여자 전사 확정): 보통 v3·거인 v3·왜소 v4와 공격·피격 여섯(각성·붕괴는 대기 그림 — 사용자 "이제 별말 없으면 대체"), 기사의 롱소드를 붙여 그리는 규칙(Design/10 §2, Round 105 README 판정 7~9). 체인 `20261024-220928` setup·EditMode 1178/1178, PlayMode `Builds_…`·`TheBuilds_Korean` 통과(`20261024-d57c`). 유료 호출 누계 477회 약 $13.58.

- 이어서(거인의 콧볼): 사용자 "전사 여자 거인 이미지 얼굴에서 콧볼 부분만 제거" → "콧구멍은 남겨줘" → "연결". 대기·공격·피격 원본의 콧볼 선만 살색으로 칠한 `knightf_giant_v3k`(호출 없음, Round 105 README 판정 10차)를 `swordswoman_giant` 다섯 자리에. PlayMode `Builds_…`·`TheBuilds_Korean` 2/2(`20261024-d57d`).

## Open
- 사후 검토: 판정 열여덟의 권장안(특히 동료의 죽음 30%, 체격의 가중치, 거인 138%), 전투 화면의 목업 안 A(판정은 사용자), 체격의 스탯을 뽑을 때 더하는 구현, 특성의 동료 피로에 전투 로그 줄이 없음.
- 58단계 훈련: 막사의 목업 → 저장 29 → 화면 → 시뮬 정책.
- 다른 직업의 거인·왜소 그림(요청 때, 직업마다 약 $0.19), 각성·붕괴 자세(지금 대기 그림).

## Next Action
- 다음 세션의 시작은 `active/d55-story-intro.md`의 "다음 세션의 시작".
- 직접 플레이: 역마차의 체격(드문 거인), 거인·왜소 전사의 전투·노드 맵·파티 팝업, 특성 카드의 새 줄, 정산의 "동료를 잃고".
