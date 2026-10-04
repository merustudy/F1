# 전투 UI 연출 라운드 — 사건의 표현, 폭풍 시계와 자막, 질감 틀

Snapshot: 2026-10-03 저녁 (연출 1·2·3차는 커밋·푸시됨, 판정 대기. 그 뒤 세로 열 라운드(V안)를 구현해 체인 통과, 같은 날 세션 정리에서 커밋·푸시). 2026-10-04 세션 정리에서 `completed/`로 옮겼다. 남은 판정은 Roadmap "사후 검토 대기"가 가진다.

## Goal

전투 화면이 일어난 일을 그 순간에 보여 준다(떠오르는 숫자, 번쩍임과 돌진, 발동 칸, 사망 잔상, 흔들림), 폭풍이 다가오는 것이 보인다(시계, 어두워짐, 번개),
UI의 틀에 재질과 장식이 있다. 결과·저장·결정론은 그대로다(연출은 로그를 읽기만 한다).

## State

- 사용자 지시 "전투 UI가 너무 심심해" → 조사 보고(`ArtPipeline/Archive/09-battle-ui-feel/README.md` §1~§5) → "모두 너의 권장안으로 1,2,3차 진행 후 보고. 필요시 이미지 API 사용"(일괄 승인).
- 1·2차(코드)와 3차(틀 여섯 생성, 호출 6회 약 $0.14)를 구현했다. 체인 통과. 판정은 보고 뒤 사용자가 한다. 전의 틀은 `Archive/09-battle-ui-feel/frames-before/`에 있다.
- 결정의 기록: Roadmap "전투 UI 연출 라운드", Design/10 §5, Architecture/09(표현 범위에 연출), 12("연출", "UI의 그림", "전투 화면"), 13(틀의 결·tint·타일, `ui_piece`).

## Done

- 새 코드: `UI/BattlePresenter.cs`(로그 → 연출), `UI/Views/BattleFxLayer.cs`(떠오르는 글자 풀, 잔상, 번개, 비네트 둘, 흔들림), `UI/Views/FloatingTextView.cs`, `UI/Views/UiBarGhost.cs`.
- 바꾼 View: `FigureView`(그림자는 Builder, 숨쉬기, 번쩍임, 움직임 오프셋, `ArtImage`), `BattleUnitView`(돌진·밀림·걷기, 빈사 맥동, `FigureRect`·`FigureArt`), `BattleItemView.Pulse`(놋쇠빛 번쩍임과 튀어오름),
  `BattleBoardView.ItemOfSlot`, `UiBar.Ratio`·`Fill`, `BattleLogText.Caption`, `BattleScreen`(헤더 제목, 시계, 자막, Fx, 걷기, 연출 재생 순서: 재생 → 배치).
- Builder: `UiPrefabSetup.Battle`(헤더의 제목, 패널 가운데의 다이얼·고리·시간·폭풍 줄·자막 셋, Fx 층과 그 조각들, 그림자, 칸의 번쩍임, 떠오르는 글자 템플릿), `Kit`(`KitBar`의 ghost, `Skin`의 타일),
  `UiArt`(`Dial`, `Ring`, `Vignette`, `Piece.Tiled`, `IsTiled`). 문구 CSV `Battle.Title`과 `Fx.*` 열한 개, `UiKeys.Fx`.
- 그림: `Assets/@Art/UI/Frame/{panel,plate_*,slot,slot_selected,button,bag,dial}.png`(새로 생성·변형), `Icon/{ring,vignette}.png`(도형). Prefab 셋과 Stamp, Font Atlas, String Table.
- 파이프라인: `STYLE_RUNTIME.md` §14·§15·§17 개정과 §22, `gen_image.py`(`ui_piece` 타입, `Flat` 열, `tint_fill`, `fit_ui(flatten)`), `ui_variants.py`(`Mode`), Roster 셋, `Archive/09-battle-ui-feel/`(README, 진단 그림, 리뷰 시트, 원본, `draw_pieces.py`).
- Test: PlayMode `Battle_PlaysWhatHappens_AsRisingNumbersCaptionsAndTheStormRing`.

## 세로 열 라운드 (V안, 2026-10-03 저녁)

- 지시: "아이템을 바자르 같은 느낌으로(쿨타임을 세로로, 칸 크기, 세로로 더 늘리기, 목업)" → "바자르는 가로 배열. 우리는 세로 버전으로. UI에서도 세로(열 순서대로) 배열. 더 나은 버전이 있으면 그걸로 목업"
  → 목업 셋(지금 / V / B 권장)과 1:1 상세 시트, 맵 둘(`ArtPipeline/Archive/10-bazaar-cells/`) → **"V안으로 쿨타임은 왼쪽에서 오른쪽으로 가게 구현"**.
- 바꾼 것: `BattleItemView`(칸 180x50, `CellGapY` 4, `BoardHeight`), `BattleBoardView`(얼굴·틀·배지·거울 배치를 지우고 칸과 가방만. 버튼의 그래픽은 가방), `BattleScreen`(`_partyBoardColumns`·`_enemyBoardColumns`를
  무대 Column과 같은 x에 놓는 `LayoutColumns`, `CreateBoard`에 얼굴 없음), `PartyColumnView`(`_board`, `BoardHeight`, 얼굴 없음), `PartySideView.LayoutColumns`(패널 열도 놓음), `ItemSlotView.SetHeight`.
  Builder: `Battle`(패널 620/460, `BuildBoardColumn`, `BuildBattleBoard`는 세로 Layout과 가방, 무대 206, 시계 160·폭풍 줄 180, 자막을 헤더로, 제목 왼쪽), `Kit`(아이콘 여백 8/5, 가방 좌우 4, `BuildPotionStrip(x)`),
  `PartySide`(포션 x 980, `PartyRightTop` 190, `PartyFieldTop` = 패널 - 12 - 444, `BuildPartyBoard`), `NodeMap`(맵 190부터), `Reward`(카드 132, 첫 줄에 종류·제목·동작). Prefab 셋과 Stamp 재생성.
  파이프라인: `gen_image.py`의 `ITEM_CELLS`(328x80, 328x188, 328x296), `review_sheet.py`(칸 180x50·간격 4, 세로 쌓임), 스타일 §19~§21. 문서: Architecture/12·13, Design/10 §5, 00.
  Test: `UiFlowTests`의 보드 검사 넷(얼굴·틀 검사 삭제, `EnemyBoard<row>` column, `BoardHeight`, 보드의 부모 = `<Side>Board<row>`).
- 세부(권장안, 사후 검토): 적의 칸도 왼쪽→오른쪽(사용자 결정 "왼쪽에서 오른쪽으로"를 양쪽에 적용), 얼굴을 쓰지 않음(그림과 읽기는 남김), 가방 좌우 4, 폭풍 줄 글자 18, 보상 카드의 배치, 팝업 190.

## Open

- 사용자의 판정: 연출의 세기(숫자 크기, 흔들림, 번쩍임), 시계와 자막의 자리, 새 틀의 모습. 되돌릴 것은 `frames-before/`로 복사하면 된다.
- 소리(10단계)는 없다. 연출의 리듬에 소리가 붙으면 세기를 다시 본다.
- 전투 밖의 연출(화면 전환, 로비·정산)은 Deferred 그대로다.

## Verification

- 세로 열 라운드 `Tools/chain.sh`: setup OK, sim OK, EditMode 616/616, PlayMode 44/44(`[Explicit]` 스크린샷 4개 제외). `git diff --check` 깨끗. 두 번째 setup에서 Prefab 셋과 Stamp의 해시가 같다. `Tools/screenshots.sh` 34장(4/4): 전투(`_05`)에 유닛마다 무대 Column 바로 아래의 세로 열(다섯 칸, 적은 한 칸), 헤더 가운데의 자막 세 줄과 왼쪽의 제목, 두 1열 사이의 시계 160. 전진 뒤(`_15`)에 죽은 적의 열이 비고 주술사의 두 칸이 쌓임. 빈사(`_18`)도 같음. 노드 맵(`_04`)·보상(`_07`)·팝업(`_17`)에 오른쪽 위의 포션 띠, 164까지 올라선 파티, 패널의 세로 열(등급 배지), 414의 맵과 132의 보상 카드 셋, 190부터의 팝업. 영어(`en_05`, `en_07`)도 제목·자막·카드 글이 들어간다. 다섯 장은 `ArtPipeline/Archive/10-bazaar-cells/game/`.
- `Tools/chain.sh`: setup OK, sim OK, EditMode 616/616, PlayMode 44/44(`[Explicit]` 스크린샷 4개 제외). `git diff --check` 깨끗. ProjectSettings, Packages, Scene은 그대로.
- 두 번째 setup에서 Prefab 셋과 Stamp의 해시가 같다.
- `Tools/screenshots.sh` 34장(4/4): 전투(`_05`)에 떠오르는 "-13"·"쓰러짐", 맞은 적의 붉은 번쩍임, HP 막대의 붉은 잔상, 발동한 칸의 놋쇠빛, 패널 가운데의 다이얼·고리·시간·폭풍 줄·자막 세 줄,
  헤더의 "버려진 광산 · 1층", 리벳이 늘어나지 않은 타일 패널. 빈사(`_18`)에 큰 "빈사!"와 붉은 비네트, 맥동하는 붉은 명패. 전진 뒤(`_15`)에 쓰러진 감독관의 잔상과 "화상 +3".
  노드 맵(`_04`)·보상(`_07`)에 새 틀(징 박힌 명패, 리벳 칸, 박음질 가방)과 발밑 그림자. 다섯 장은 `ArtPipeline/Archive/09-battle-ui-feel/game/`.
  (Test가 한 Frame에 긴 구간을 보내므로 같은 자리의 글자 여럿이 한꺼번에 떠 조금 겹친다. 실제 진행에서는 사건 사이에 시간이 있다.)

## 알아둘 것

- 연출은 `BattleScreen.Render`가 `_presenter.Play()`를 `Place()`보다 먼저 부른다. 순서를 바꾸면 사망 잔상이 자리를 잃는다.
- Test가 한 Frame에 긴 구간을 보내면 마지막 300ms의 이벤트만 재생된다(`BattlePresenter.RecentMs`).
- 틀을 다시 그릴 때: 결을 남기려면 Roster의 `Flat`을 no로, 그 변형은 `Mode`를 tint로. 가장자리에 반복 무늬가 있으면 `UiArt`에서 `tiled: true`.
- `draw_pieces.py`의 산출물은 `output/ui_placeholder/`다(`output/ui_piece/`는 생성물의 자리라 겹치면 호출이 거부된다).

## Next Action (제안)

0. V안을 Unity에서 직접 플레이해 본다(세로 열, 헤더의 자막, 노드 맵·보상의 포션 자리와 카드).
1. 사용자가 보고를 보고 판정한다: 그대로 둘 것, 세기를 바꿀 것, 되돌릴 틀(`frames-before/`에서 복사). 바꾸면 체인과 스크린샷으로 확인하고 커밋한다.
2. Unity에서 직접 플레이해 움직임의 리듬을 본다(스크린샷은 한 순간만 보여 준다. 옛 저장은 이어지지 않으니 새 런).
3. 그 뒤 남은 것: 포션 아이콘, 10단계(소리, G11), 11단계(플레이테스트). (아이콘 재조절은 V안으로 필요 없어졌다.)
