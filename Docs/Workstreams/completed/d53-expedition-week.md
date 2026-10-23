# Slice D 53단계: 원정의 기간 (원정 한 번 1주)

Snapshot: 2026-10-23. 사용자 "광산/심연 원정 기간은 무엇을 뜻해? 우리 원정은 1일 소요 아니였나?" → 답(원정 한 번이 99주 달력을 넘기는 주. 1이었던 적은 없고, 1은 쉬기) → "원정 한번을 1주로 변경하고(원정 진행 1/3 전 조기 포기시에는 미소요), 나중에 던전을 추가할 예정이니 심연 던전 밸런스는 나중에 맞추어 보자".

## State
- 데이터:
  - `DungeonData` `abandoned_mine.DurationDays` 7 → 1, `abyss.DurationDays` 4 → 1.
  - `BalanceData` 새 키 `FreeRetreatPercent` 33(0~100). Generated JSON 둘.
- 코드:
  - `RunRules.DaysFor`·`IsEarlyRetreat`: 후퇴이고, 최종 결전이 아니고, 파티가 선 층 × 100 < (층 + 1) × 33이면 0주다. 아니면 `DurationDays`다.
  - `Settle`의 `DaysPassed`와 `PassDays`가 이 값을 쓴다.
  - `BalanceData.FreeRetreatPercent`.
  - 정산 화면: `SettlementScreen`은 0주면 `Settle.NoDays`("일찍 돌아와 시간이 흐르지 않았습니다 ({0}주차)" / "Back early: no time passed (week {0})")를 쓴다. `UiKeys`, `UI_StaticText.csv`.
- Test:
  - `RunRulesTests.Settle_AnEarlyRetreatPassesNoDays_ALaterOneAndAWipeTheirDays`.
  - 시험 데이터(`TestCsv`·`TestData`)는 `FreeRetreatPercent` 0이다. 그래서 옛 Test의 날 셈은 그대로다.
- 권장안(사후 검토):
  - 1/3은 보스를 넣은 층 수로 잰다(광산 6층까지가 무료).
  - 무료는 후퇴만이다(전멸은 주를 씀).
  - 시간 말고는 정산이 그대로다(도달한 층의 금·경험치·특성·피로·죽음). 집에 남은 용병의 회복도 0이다.
  - 후퇴 단추의 무료 표시는 하지 않았다(목업 먼저).
- 저장: 형식 그대로(27). 정산 보고의 `DaysPassed`가 0일 수 있다.
- 커밋: 세션 정리: 2026-10-23 커밋·푸시 — 기획·Architecture `5f015f3` / 구현 `0393552` / Round 100 `26cd0c2` / Roadmap·Handoff.
- 문서: 04 §2·§9, 03 §2, 00 "53단계: 원정의 기간", 08 §61, Architecture/08·09, CLAUDE.md §9, Roadmap 53단계(날짜 표·사후 검토 대기).

## Verification
- `transform` → `validate` OK(8 jobs, 60 items, 9 enemies). 시뮬은 `dotnet build Tools/Sim -c Release` 뒤 저장소 빌드로 쟀다(08 §61).
- 전체 체인 `20261023-144622` **CHAIN OK**: setup·sim OK, EditMode 1120/1120, PlayMode 98/113(건너뜀 15는 스크린샷 Test). `Assets/InitTestScene*` 없음, `git diff --check` 깨끗.
- 화면 배치는 바뀌지 않았다(정산의 날 줄의 글만). 그래서 스크린샷 라운드는 하지 않았다.

## Open
- 런 띠 넷이 밖이다(08 §61): 균형형 64.5%(아래 가장자리), 전멸 27.5%, 끝난 주 10, 결전행 0%. 사용자 지시로 심연의 밸런스와 함께 던전이 더 들어온 뒤에 맞춘다. 끝난 주·결전행의 띠는 이 규칙에서 뜻이 달라져 그때 다시 정한다.
- 무료 후퇴를 노리는 반복(6층까지 돌고 돌아옴)은 시뮬이 재지 않는다. 피로가 막는다고 보지만 직접 플레이로 본다.
- 후퇴 단추에 "지금은 시간이 들지 않음"을 알리는 표시는 화면 라운드(요청 때)다.

## Next Action
- 이 단계는 끝났다. 다음은 직접 플레이(`active/d43-tuning.md`).
