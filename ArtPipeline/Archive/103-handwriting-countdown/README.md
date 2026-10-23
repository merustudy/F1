# Round 103: 단장의 손글씨와 로비의 "일식까지 N주"

2026-10-23. 55단계의 남은 것(Design/33 §11의 4) 중 둘이다. 유료 호출 없음.

## 지시

사용자 "3. 손글씨 서체: 단장의 혼잣말용입니다. 지금은 일반 서체입니다. / 4. 로비 머리의 "일식까지 N주": 지금은 "N주차 / 99주"로 나옵니다. — 적용".

둘 다 눈으로 정하는 변경이라 목업을 먼저 보인다. 바탕은 55단계 스크린샷 `20261023-d55b`이고, 글만 지우고 다시 썼다(`mock_round103.py`).

## 1. 단장의 혼잣말 서체 (`mock-font-sheet.png`)

Design/33 §9: 단장의 혼잣말은 손글씨 서체, 남의 말(따옴표 안)은 인쇄체. 지금은 둘 다 Pretendard이고 색으로만 가른다(Architecture/06 "Font"는 이것을 Deferred로 두었다).

| 안 | 서체 | 느낌 |
|---|---|---|
| 지금 | Pretendard | 깔끔하지만 단장의 목소리가 따로 없다 |
| **A (권장)** | 나눔손글씨 펜 (Nanum Pen Script, SIL OFL 1.1) | 펜으로 쓴 일기. 자막 크기에서 잘 읽히고 영어 글자도 손글씨다 |
| B | 나눔손글씨 붓 (Nanum Brush Script, SIL OFL 1.1) | 더 거칠고 어둡다. 작은 크기에서는 획이 뭉개져 덜 읽힌다 |

- 따옴표 안의 말(왕, 판관)은 셋 다 그대로 Pretendard + 옅은 금색이다. 한 줄 안에서 두 서체가 섞인다(#3의 첫 줄).
- 손글씨는 같은 크기에서 작아 보여 A는 1.38배, B는 1.30배로 맞췄다(줄 높이 45 그대로).
- 목업은 macOS에 들어 있는 같은 서체(`NanumScript.ttc`)로 그렸다. 게임에는 공개된 원본(google/fonts의 `ofl/nanumpenscript/`)을 라이선스 파일과 함께 넣는다.

## 2. 로비 머리줄 (`mock-countdown-sheet.png`)

Design/33 §7의 권장안은 "위쪽 표시는 남은 주를 센다("일식까지 87주")"다. 첫 주는 99, 99째 주는 1이다(`StoryRules.WeeksLeft`).

| 안 | 머리줄 (첫 주) |
|---|---|
| 지금 | 1주차 / 99주 |
| **A (권장)** | 일식까지 99주 |
| B | 일식까지 99주 · 1주차 |
| C | 일식까지 99주 (아래 작게 1주차 / 99주) |

A의 이유: 인트로 끝의 띠("일식까지 99주")와 같은 말이고, 남은 시간이 한눈에 보인다. 지난 주는 남은 주에서 바로 나온다.

## 3. A를 고르면 함께 바뀌는 곳 (`mock-countdown-follow-sheet.png`)

머리줄만 남은 주로 바꾸면 다른 화면의 "N주차"와 어긋난다. A는 화면 글의 기준을 남은 주로 맞춘다.

| 키 | 지금 | A |
|---|---|---|
| `Lobby.Day` (로비·원정 머리줄) | 1주차 / 99주 | 일식까지 99주 |
| `Lobby.DayFinalStand` (99주가 지난 뒤) | 100주차 / 99주 — 최종 결전 | 일식의 날 — 최종 결전 |
| `Expedition.Days`, `Expedition.PartyDungeon` | 1주 (→ 2주차) | 1주 (→ 일식까지 98주). 돌아오는 때가 일식의 날이면 "(→ 일식의 날)" |
| `Settle.Days` | 1주가 지나 2주차가 되었습니다 | 1주가 지나 일식까지 98주 남았습니다. 0이면 "1주가 지나 일식의 날이 되었습니다" |
| `Settle.NoDays` | 일찍 돌아와 시간이 흐르지 않았습니다 (1주차) | 일찍 돌아와 시간이 흐르지 않았습니다 (일식까지 99주) |
| `Expedition.FinalStandHint` | 99주를 넘기면 심연의 보스와 강제로 싸운다 | 일식의 날에는 심연의 보스와 강제로 싸운다 |
| `Lobby.DayValue` (결과 창의 "끝난 주") | 13주차 | 그대로 (지난 기록이라 몇째 주가 맞다) |

영어도 같은 틀이다("{0} weeks to the eclipse", "The day of the eclipse — the final stand").

## 권장

1 A(나눔손글씨 펜), 2 A(남은 주만), 3 위 표대로. 새 단계 없이 55단계의 이어서(Round 103)로 한다.

## 승인 뒤 할 일

- 서체: google/fonts에서 `NanumPenScript-Regular.ttf`(약 3.2MB)와 `OFL.txt`(약 4.5KB)를 받아 `Assets/@Fonts/Source/NanumPenScript/`에 README(출처와 SHA-256)와 함께 둔다. `FontSetup`이 두 번째 TMP Font Asset(Static Atlas, Corpus는 인트로의 혼잣말 줄, 없는 글자는 Pretendard로 Fallback)을 굽는다. `IntroScreen`은 혼잣말을 손글씨로, 따옴표 안을 Pretendard로 쓴다(한 줄 안의 서체 바꿈). 줄 크기의 배율은 서체의 데이터가 아니라 화면의 값이다.
- 남은 주: 위 표의 문구(`UI_StaticText.csv`)와 머리줄·원정·정산의 인자. 규칙은 그대로다(`TotalDays` 99, 저장 그대로).
- 문서: Architecture/06 "Font"(서체 둘), 12(머리줄), Design/33 §9·§11의 4, 00 "Round 103의 판정", Roadmap. Test와 스크린샷 대조.

## 판정

2026-10-23 사용자 "1. 지금 유지 / 2. 권장안 / 3. 권장안".

- 1: 지금 유지. 손글씨 서체는 넣지 않는다(내려받기 없음). 단장과 남의 말은 Pretendard에 색으로 가른다.
- 2: A, 남은 주만("일식까지 N주"). 로비와 원정의 머리줄.
- 3: 위 표대로. 다만 머리줄은 새 문구 대신 로비의 띠가 쓰던 `Story.WeeksLeft`를 함께 쓰고(Architecture/06에 적어 둔 계획) `Lobby.Day`는 지웠다. 원정 화면의 기간 칸(378px)에서 영어가 잘리지 않게, 남은 주 0("일식의 날")은 끼워 넣는 말이 아니라 따로 둔 키(`Expedition.DaysEclipse`·`PartyDungeonEclipse`, `Settle.DaysEclipse`·`NoDaysEclipse`)로 쓰고, 영어의 원정 줄은 짧게 "(→ 98 left)"로 했다.
