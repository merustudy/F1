# Slice C 26단계: 로비 구조와 경제·고용 — 첫 조각: 원정 화면 E2

Snapshot: 2026-10-16 (원정 화면 E2 구현, 체인 OK, 스크린샷을 목업과 대조해 고침. 세션 정리로 커밋·푸시함)

## Goal

Architecture/09 "Slice C" 구현 순서 2가 참이다: 로비 화면(마을 그림 위의 구역 막사·역마차·쉼터)과 원정 화면(던전 고르기), 금화와 정산 보상, 역마차의 고용·해고, 피로 시설, 시뮬의 로비 정책. 이 Handoff는 사용자 "E2 구현"(2026-10-16)으로 연 첫 조각, 원정 화면부터 적는다.

## State

- 2026-10-16 Round 71 다섯째: 지도 생성 1회 → 사용자 **"E2 구현"**(파티는 막사에서, 원정 화면은 지도·던전 정보·보기만·출발). 승인 원본 `ArtPipeline/Archive/71-lobby-expedition/approved/expedition_map.png`.
- 원정 화면을 구현하고 검증했다(아래 Done·Verification). 25단계의 Test 재실행도 같은 체인이 덮는다(`c25-run-end.md`는 completed로).
- 남은 26단계: 마을 로비(Round 71 안 1 + 생성 그림 `lobby_town_topdown`의 배선), 결과 창·최종 결전 상태(Round 72, 판정 대기), 금화·정산 보상, 역마차(고용·해고), 피로 시설, 시뮬의 로비 정책 — 다음 지시로.

## Done

- 화면: `ScreenId.Expedition`(`ui/lobby/expedition-screen`, Lobby Scope — `AddressablesSetup.Ui`가 자동 등록), `ExpeditionScreen`(지도·표식·정보·파티·출발·로비로. `PrepareAsync`가 지도를 Lobby Scope로 읽음), `DungeonMarkerView`(Template 복제, `PlaceAt`, `Show`), Builder `UiPrefabSetup.Expedition.cs`(`BuildExpedition`·`BuildDungeonMarker`·`Disc`), `UiPrefabSetup.Build`에 등록.
- 로비: [원정 출발] → [원정 화면으로](`Lobby.Depart`의 문구만, 단추는 그대로 `Frame/Expedition/Depart`; 파티가 비었거나 런이 끝났으면 꺼짐), `OnDepart`가 원정 화면을 연다. 보통 버튼 소리(`UiBuild.Silence`를 뺌). 출발과 출발 소리는 원정 화면의 `Frame/Bottom/Depart`.
- 그림: `ArtAddress.ExpeditionMap`, `Assets/@Art/Background/Expedition/map.png`(2304x1536), `ArtSetup.Files`의 Scene 정책·Lobby Scope Entry, `ArtSetupTests`의 경로 사례.
- 데이터: `DungeonData.MapX`·`MapY`(선택 열, 0~100 %. 광산 38·30, 심연 54·13 — 10은 머리띠에 가렸다), Mapper, Generated, TestData(cave 40·30, pit 55·10).
- 스크린샷을 목업과 대조해 고친 것(README 71 "다섯째 지시"): 명패 폭 300과 "심연 (최종 던전)", 정보 패널의 왼쪽 이름·오른쪽 값 줄 다섯과 안내 두 줄(`Expedition.UnlockHint`는 "이 던전의 클리어가 여는 던전"으로 뜻을 바꿈, `FinalStandHint` 새로), 시작 포션 "치유 포션 ×2", 파티 칸 `ExpeditionPartyCardView`(이름, `UiText.JobLine`, 피로 칸 열 개 — 로비 Builder의 `FatiguePipRow`를 함께 씀). 문구 `Expedition.*`는 스물하나(`MemberDetail` 지움).
- 문구 `Expedition.*` 열넷(`UiKeys.Expedition`, `UI_StaticText.csv`).
- Test: PlayMode `Expedition_ShowsEveryDungeonOnTheMap_KeepsTheLockedOneClosed_AndDepartsToTheChosenOne`, `UiTestUtil.DepartFromLobby`(로비의 출발을 누르고 노드 맵을 기다리던 열세 곳: Flow 9·Save 3·Screenshot 1. Flow의 `DepartToTheMap`도 이를 쓴다), 음악 Test(로비의 단추는 버튼 소리, 원정 화면의 출발은 출발 소리), `ArtSetupTests`(지도만 Lobby Scope), 스크린샷 `_56_expedition`(ko·en, 랩의 `_03` 다음).
- 문서: Architecture/04(Lobby Scope), 05(선택 열), 12("원정 화면", 화면 표), 13(Unity 배선: 원정 지도), 14(출발 소리의 자리), Design/04 §7, 07 §4, 00, README 71 "다섯째 지시", Roadmap 24·26단계.

## Open

- Round 72(결과 창·최종 결전 상태)의 판정 — 마을 로비와 함께 구현.
- 사후 검토(README 71 "다섯째 지시"의 권장안 세부): 표식의 모양, 본부 표식의 자리, 패널의 90% 바탕, 처음 고르는 던전, 파티 칸에 레벨 없음(27단계).
- 로비가 보이는 던전 정보(이름·속성·일수·클리어)는 첫 일반 던전의 것 그대로다(마을 로비로 바꿀 때 사라진다).

## Verification

- `transform`·`validate` OK(`MapX`·`MapY`). 체인 `20261016-192628`(setup·sim OK, EditMode 863/863, PlayMode 85/95 — 건너뜀 10은 그래픽이 있어야 하는 스크린샷 Test), 그래픽 스크린샷 `20261016-193944`(10/10, PNG 95장). 원정 화면 `_56_expedition`(ko·en)을 목업 `mock-expedition-painted-E2.png`와 요소별로 대조했다: 머리띠(금화는 26단계의 경제), 표식 둘(심연은 자물쇠 그림 대신 흐린 디스크, 명패는 디스크 아래), 본부, 정보 줄 다섯(보상 줄은 경제와 함께)과 안내 두 줄, 파티 칸(레벨은 27단계), 상태 글과 [출발]. 로비의 피로 칸(`FatiguePipRow`로 옮김)은 `_03_lobby_party`에서 그대로다.

## Next Action (제안)

- 세션: 사용자가 Round 72를 판정하면 마을 로비(안 1)·결과 창·최종 결전 상태를 구현하고, 금화·역마차·시설·시뮬 정책으로 간다.
