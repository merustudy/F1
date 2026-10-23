# Slice D 54단계: 조기 퇴각의 패널티

Snapshot: 2026-10-23. 사용자 "일찍 퇴각시 보상 및 경험치, 긍정적 후천적 특성은 “조기에 퇴각하여 아무것도 얻을 수 없었습니다“로 하여 얻지 못하게 하며, 부정적 특성은 얻는 걸로 하자.(조기 퇴각 패널티) 검토 후 보고" → 제안서 `Docs/Design/36_Early_Retreat_Penalty_Proposal.md`(판정 일곱) → "권장안 구현"("구현").

## State
- 규칙(04 §9, 05 §6): 53단계의 조기 퇴각(`RunRules.IsEarlyRetreat`: 후퇴이고 1/3 전, 광산은 6층까지)은 금·경험치·장점을 얻지 못한다. 약점·죽음·피로는 남는다.
- 코드:
  - `RunRules.Settle`: `early`면 `ExpEarned`·`GoldEarned`가 0이다. `DrawAcquired`가 굴린 것에서 `TraitPolarity.Positive`를 버린다(같은 스트림·같은 굴림이라 약점의 확률은 보통 후퇴와 같다).
  - `SettlementScreen`: 후퇴이고 `DaysPassed` 0이면 금 줄에 `Settle.EarlyRetreat`를 보통 글자색(`UiPalette.Text`)으로 쓰고, 경험치 줄은 비운다. 아니면 금빛(`DiabloGold`)으로 되돌린다.
  - `UiKeys`, `UI_StaticText.csv`("조기에 퇴각하여 아무것도 얻을 수 없었습니다" / "The early retreat brought back nothing").
- Test:
  - `AcquiredTraitTests.Settle_AnEarlyRetreat_KeepsTheNegativeTraitsRolled_AndThrowsThePositiveOnesAway`.
  - `RunRulesTests`의 조기 퇴각 Test: 금·경험치 0.
  - PlayMode `UiFlowTests.StagedEarlyRetreat`(본 흐름의 정산에서).
  - 스크린샷 fixture `UiScreenshotTests.StagedNewTraits(..., early: true)` → `_86_settlement_early_retreat`.
- 권장안(사후 검토): 판정 2~7, 그리고 문구의 보통 글자색.
- 저장: 형식 그대로(27).
- 커밋: 세션 정리: 2026-10-23 커밋·푸시 — 기획·Architecture `5f015f3` / 구현 `0393552` / Round 100 `26cd0c2` / Roadmap·Handoff.
- 문서: Design/36 §6, 00 "Design/36의 판정", 04 §9, 05 §6, 08 §62, Architecture/08·09·12, CLAUDE.md §9, Roadmap 54단계(날짜 표·사후 검토 대기).

## Verification
- 시뮬 08 §62: 저장소의 출력은 복사본(`scratchpad/simcopy/`, `F1_EARLY_PENALTY=1`)과 바이트 단위로 같다.
- 전체 체인 `20261023-152805` **CHAIN OK**: setup·sim OK, EditMode 1121/1121, PlayMode 98/113(건너뜀 15는 스크린샷 Test). `Assets/InitTestScene*` 없음.
  - setup이 새 문구 둘로 String Table 셋, 폰트 아틀라스(`Pretendard-Medium SDF`), `UiPrefabStamp.txt`를 다시 만들었다.
- 스크린샷 `20261023-d54`(`EveryScreen_Korean` 1/1). `ko_86_settlement_early_retreat`를 `ko_77_settlement_new_traits`와 대조했다.
  - 같은 것: 제목 "후퇴", 줄의 자리.
  - 달라진 것: 날 줄 "일찍 돌아와 시간이 흐르지 않았습니다 (2주차)", 금 줄 자리의 문구(보통 글자색), 경험치 줄은 빈칸 한 줄, 레벨 업 줄은 없음, 약점 칩 둘(패배의 기억·흉터).
  - 넘치거나 잘린 글은 없다. 빈 경험치 줄이 문구와 특성 줄 사이에 한 줄의 틈으로 남는다(판정 4 "비움"대로).

## Open
- 후퇴 단추의 경고("지금 후퇴하면 아무것도 얻지 못함")는 다음 화면 라운드에서 목업 먼저(판정 5).
- 런의 전멸 +4~8%p. 런 띠는 심연과 함께 나중에(사용자).

## Next Action
- 이 단계는 끝났다. 다음은 직접 플레이(`active/d43-tuning.md`).
