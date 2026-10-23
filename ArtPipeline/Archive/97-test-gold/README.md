# Round 97 — 로비의 금화 테스트 버튼

- 지시 (2026-10-23 사용자): "로비 화면에서 금화 테스트 버튼 만들어 클릭시 100 골드씩 받을 수 있게"
- 유료 호출: 없음. 목업은 게임 스크린샷(`game/ko_12_lobby_after.png`, 체인 `20261022-d50c`) 위에 도형과 글자로 그렸다.
- 다시 그리기: `.venv/bin/python ArtPipeline/Archive/97-test-gold/mock_round97.py`

## 목업

- `mock-gold-button.png`: 지금 화면과 버튼 자리 세 안.
  - **A 머리줄의 금 옆**(권장)
  - B 아래 띠의 [원정 화면으로] 왼쪽
  - C 마을 그림의 왼쪽 위
- `mock-gold-button-header.png`: 머리줄을 원래 크기로 본 지금과 A.
- 버튼의 색은 게임의 버튼이 쓰지 않는 자주색 판에 분홍 테다. 한눈에 개발 도구로 읽히게 했다.

## 판정할 것 (권장안은 첫째)

| # | 무엇 | 선택지 |
|---|---|---|
| 1 | 자리 | **A 머리줄, 금 옆**(누르면 바로 옆의 숫자가 오른다. 금 글자의 칸은 1340~1620 → 1480~1620으로 줄인다. 그래도 "금 99999"까지 들어간다) / B 아래 띠 / C 마을 그림 위 |
| 2 | 누가 보나 | **Unity 에디터와 개발 빌드(Development Build)에서만**(정식 빌드에는 버튼도 명령도 없다) / 늘 보임 / 설정의 숨은 스위치 |
| 3 | 언제 되나 | **로비에서 런이 진행 중일 때 언제나**(최종 결전의 로비 포함, 결과 창 뒤는 안 됨) / 공사 잠금처럼 막히는 때를 따름 |
| 4 | 금의 양 | **100, 코드의 상수**(테스트 도구라 게임 밸런스 값이 아니며 정식 빌드에 없음) / `BalanceData`의 키 |
| 5 | 저장 | **다른 로비 명령처럼 바로 저장**(`RunManager`: 바꾸고 → 저장 → 화면) / 저장하지 않음(이어하기에서 사라짐) |
| 6 | 기록 | **플레이 기록에 한 줄 "TestGold +100"**(`Tools/playlog.py`가 보여 준다. 받은 금이 맞추기 판단을 흐리지 않게) / 남기지 않음 |
| 7 | 글 | **"금 +100 (테스트)"**, String Table의 ko-KR·en-US("Gold +100 (test)") / 다른 글 |
| 8 | 스크린샷 | **스크린샷 fixture에서는 숨김**(에디터라 보이지만 기존 캡처가 다 바뀌지 않게) / 그대로 보임 |

## 구현할 것 (승인 뒤)

- `RunManager.GrantTestGold()`: 로비에 있는지 확인하고, 금을 더하고, 저장한다. 전체를 `#if UNITY_EDITOR || DEVELOPMENT_BUILD`로 감싼다.
- `UiPrefabSetup.Lobby`: 버튼 `TestGold`를 만든다.
- `LobbyScreen`: 버튼을 연결하고, 개발용이 아닌 빌드에서는 끈다. 플레이 기록에 한 줄을 남긴다.
- `UiPalette`: 테스트 색 둘.
- 문구 키 하나.
- Test: EditMode는 금 +100과 저장을, PlayMode는 버튼을 누르면 금 글자가 오르는 것을 본다.

## 판정

2026-10-23 사용자: "금화 테스트는 권고안으로 먼저 구현". 위의 여덟 모두 권장안으로 정했다(사후 검토).

## 구현 (2026-10-23)

- `RunRules.GrantGold`: 런이 진행 중일 때 양수만 더한다.
- `RunManager.GrantTestGold`·`CanGrantTestGold`·`TestGoldGranted`·`TestGoldAmount` 100: `#if UNITY_EDITOR || DEVELOPMENT_BUILD`로 감쌌다.
- `AppRoot`: `TestGoldGranted`에 묶어 플레이 기록에 `!TestGold` 줄을 쓴다(`PlayLog.EventPrefix`). `Tools/playlog.py`는 사건 줄을 화면의 시간에서 빼고 따로 센다.
- `UiPrefabSetup.Lobby`: `TestGold` 버튼(1290,26 — 176x48)을 만들었고, 금 글자의 칸을 1480~1620으로 줄였다.
- `LobbyScreen`: 버튼을 연결했다. `TestToolsShown`이 꺼져 있거나 런이 끝나면 숨긴다.
- `UiPalette.TestTool`·`TestToolRim`, 문구 `Lobby.TestGold`.
- Test:
  - EditMode `RunManagerTests.GrantTestGold_AddsTheTestGold_SavesIt_AndTellsAfterTheSave`·`GrantTestGold_IsClosedAwayAndOnceTheRunIsOver`
  - PlayMode `UiFlowTests.Lobby_TestGoldButton_AddsAHundredGold_SavesIt_AndWritesAnEventLine`
  - 스크린샷 `TheTestGold_Korean`(`ko_91_lobby_test_gold`, `ko_92_lobby_test_gold_after`). fixture의 나머지 캡처는 버튼을 숨긴다.

## 검증

- 전체 체인 `20261023-082256` **CHAIN OK**: setup·sim OK, EditMode 1115/1115, PlayMode 98/113(건너뜀 15는 스크린샷 Test).
- 스크린샷 `20261023-r97`(`TheTestGold_Korean` 1/1)의 사본은 `ko_91_lobby_test_gold.png`·`ko_92_lobby_test_gold_after.png`다.
- 목업과의 대조: `impl-compare-header.png`. 위의 둘이 목업(지금·A), 아래 둘이 구현(누르기 전·두 번 누른 뒤)이다. 맞는 것은 다음과 같다.
  - 자리: 금의 왼쪽, 같은 높이.
  - 자주색 판과 분홍 테.
  - 글 "금 +100 (테스트)".
  - 금이 오른쪽 맞춤이라 숫자가 늘면 왼쪽으로 자란다(금 0 → 금 200).
