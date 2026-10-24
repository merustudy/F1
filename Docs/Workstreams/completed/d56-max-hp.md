# Slice D 56단계: 최대 체력과 살기의 부적

Snapshot: 2026-10-24. 사용자의 플레이 피드백 셋("상점 등에서 최대 체력 아이템 장착/해제시 바로 ui에 적용되게 …" · "최대 체력 아이템 1/3로 너프(너무 쉬어짐)" · "목걸이 예리, 격분 너무 강함(방어구 처럼 최초 1회 적용하거나 쿨타임을 늘리는 방향)") → 제안서 `Docs/Design/38_Max_Hp_And_Bloodlust_Proposal.md`(판정 아홉) → "권장안 구현".

## State
- 플레이 기록 `play-20261024-122615`(Round 104가 든 판): 광산 원정 9분 51초, 전투 14번 6.4분, 보스전 21초. 시험 금화 다섯 번.
- 규칙(02 §4의 2026-10-24 줄): 원정이 지니는 체력(`ExpeditionMember.Hp`·`MaxHp`)은 아이템 없는 값 그대로다. 원정의 화면은 보드의 최대 체력 아이템을 더한 값을 보인다 — 전투가 시작될 때의 값과 같다. 최대 체력 효과의 대상은 자신뿐이다.
- 데이터: `ItemData.csv`의 가죽 허리띠 +8, 수호자의 목걸이 +4, 살기의 부적(`Trigger` BattleStart, `CooldownMs` 0, 예리 12·격분 12). Generated `ItemData.json`.
- 코드:
  - Domain: 새 `MaxHpRules.Scale`(엔진의 `RaiseMaxHp`·`ExpeditionRules.HpAfterBattle`·`ShownHp`가 같이 씀), `ExpeditionRules.ShownHp`(보드를 읽는 차례·줄에서 일하는 전투 시작 아이템·크기·주인의 스탯 % — 엔진과 같은 셈), `MemberHp`(`ExpeditionState.cs`), `ItemData`의 검사(최대 체력은 `Self`만).
  - Flow: `ExpeditionManager.ShownHp`.
  - UI: `PartyColumnView.Show`의 `MemberHp hp`(`PartySideView`가 넘김)·`HpLine`, `BattleUnitView.ShowHp`(이긴 뒤 원정의 값)·`HpLine`, `BattleScreen.RefreshLoot`가 아군 막대에 넘김, `UiText.CooldownFact`의 "장착 중 적용"(`Item.WhileEquipped`, 한·영).
- Test: EditMode `ShownHpTests`(전투의 시작 값과 같음, 서른 번 넣고 빼도 체력 그대로, 죽은 구성원), `EffectVocabularyTests.MaxHp_RaisesItsOwnersOwnOnly`, `ShippedDataTests`(값 800·400, `TheBloodlustCharm_GivesKeenAndFuryOnce_WhenTheBattleStarts`). PlayMode `UiFlowTests.PartySide_AMaximumHpItem_ShowsOnTheHpBarAtOnce_AndTakenOffGivesTheSameHpBack`·`Loot_AfterTheWin_ThePartysBarsShowTheExpeditionsHp_AndABeltPutOnShowsAtOnce`.
- 저장: 형식 그대로(런 28). 전투 규칙·결정론 그대로(시뮬 출력이 코드 변경 전후 같음).
- 문서: Design/38 §7, 02 §4, 00 "Design/38의 판정"·목록, 08 §63, Architecture/05·08·09·12, CLAUDE.md §9, Roadmap 56단계(43단계 절의 한 줄, 날짜 표).
- **이어서: 이긴 전투의 회복을 지움**(사용자 "전투에서 이기면 살아남은 용병이 최대 체력의 10%를 회복하는 규칙-삭제"): `BalanceData.csv`·`BalanceData.cs`의 `PostBattleHealPercent`, `ExpeditionRules.CompleteBattle`(산 구성원은 HP 최소 1만 — 03 §6), Test(`TestData`·`TestCsv`의 키, `ExpeditionRulesTests.CompleteBattle_OnVictory_CarriesHpOver_HealingNothing_AndDropsLoot`), 문서 02 §12·03 §6·00·08 §63·Architecture/08·12·Design/38 §7. 이긴 직후 막대는 오르지 않는다.
- 커밋: 세션 정리 2026-10-24 — 기획·Architecture `c35fc59` / 구현 `5a06fc5` / Roadmap·Handoff, 푸시.

## Verification
- 시뮬 08 §63: 저장소의 출력은 데이터만 바꾼 복사본(scratchpad `r105/`)의 `hp13_bs12`와 바이트 단위로 같다(광산 시드 1). 코드를 바꾼 뒤에도 같다.
- 전체 체인 `20261024-131847` **CHAIN OK**: setup·sim OK, EditMode 1159/1159, PlayMode 105/122(건너뜀 17은 스크린샷 Test). `Assets/InitTestScene*` 없음.
  - setup이 새 문구 하나로 String Table 셋, 폰트 아틀라스(`Pretendard-Medium SDF`), `UiPrefabStamp.txt`를 다시 만들었다(Prefab은 그대로).
- 회복을 지운 뒤 전체 체인 `20261024-145231` **CHAIN OK**: EditMode 1159/1159, PlayMode 105/122. 저장소의 시뮬 출력은 복사본 `noheal`과 바이트 단위로 같다.
- 스크린샷은 찍지 않았다: 배치는 그대로이고 숫자와 카드의 한 줄만 바뀐다(판정 3). PlayMode Test가 파티 열·줍기 막대의 글과 카드의 줄을 확인한다.

## Open
- 사후 검토: 판정 아홉의 권장안(특히 부적의 값 12), 빈사로 이긴 용병의 최소 HP(03 §6 그대로), 회복을 지운 뒤 광산 52.0%(띠의 아래 끝)·죽음 0.55(띠 위).
- 같은 꼴로 쌓이는 광전사의 도끼(격분 5/3.6초)·깃발(조준 8/6초)은 그대로 — 다음 플레이에서 본다.
- 시뮬의 균형형은 모으고 합치는 빌드의 힘을 보지 못한다(08 §63 읽은 것 1).

## Next Action
- 직접 플레이: 상점에서 허리띠를 놓고 뺄 때의 파티 열, 줍기의 HP 막대, 부적이 전투 시작 차례(Round 104)의 한 걸음으로 보이는지, 원정 시간.
