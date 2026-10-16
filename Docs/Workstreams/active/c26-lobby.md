# Slice C 26단계: 로비 구조와 경제·고용 — 원정 화면 E2, 마을 로비, 금·고용·해고·치료

Snapshot: 2026-10-16 (사용자 "권장안대로 구현": Round 72 판정과 26단계의 나머지를 구현. 체인 OK, 스크린샷을 목업과 대조해 고침. 커밋은 지시가 있을 때)

## Goal

Architecture/09 "Slice C" 구현 순서 2가 참이다: 로비 화면(마을 그림 위의 구역 막사·역마차·쉼터)과 원정 화면(던전 고르기), 금화와 정산 보상, 역마차의 고용·해고, 피로 시설, 시뮬의 로비 정책. 첫 조각 원정 화면 E2(사용자 "E2 구현")에 이어 나머지를 한 번에 구현했다.

## State

- 2026-10-16 Round 71 다섯째: 사용자 **"E2 구현"** → 원정 화면 E2(이전 Snapshot. 아래 Done의 "원정 화면").
- 2026-10-16 사용자 **"권장안대로 구현"**: Round 72(결과 창·최종 결전 상태)는 목업대로 판정(README 72 "판정"), 26단계의 나머지(마을 로비, 금, 역마차, 해고, 피로 시설, 시뮬 정책)를 권장안으로 구현했다. 권장안으로 정한 세부는 Roadmap "사후 검토 대기"의 "26단계의 권장안 세부".
- 26단계는 완료. 다음은 27단계(성장) — 시작 지시를 받은 뒤.

## Done

### 원정 화면 E2 (첫 조각)
- 화면: `ScreenId.Expedition`(`ui/lobby/expedition-screen`, Lobby Scope), `ExpeditionScreen`(지도·표식·정보·파티·출발·로비로. `PrepareAsync`가 지도를 Lobby Scope로 읽음), `DungeonMarkerView`, Builder `UiPrefabSetup.Expedition.cs`. 그림 `ArtAddress.ExpeditionMap`, `Assets/@Art/Background/Expedition/map.png`. 데이터 `DungeonData.MapX`·`MapY`. 문구 `Expedition.*`. Test `Expedition_ShowsEveryDungeonOnTheMap_…`, `UiTestUtil.DepartFromLobby`, 스크린샷 `_56_expedition`. 세부는 README 71 "다섯째 지시".
- 26단계 후반에 더한 것: 헤더의 금(`Frame/Header/Gold`), 정보의 여섯째 줄 보상(`Expedition.Reward` "층마다 금 2 · 클리어 금 20").

### 마을 로비 (Round 71 안 1, Round 72)
- `LobbyScreen`을 마을로 다시 만들었다: `Frame/Town`(그림 1920x1280, 위 0px부터. `ArtAddress.LobbyTown` = `background/lobby/town`, `Assets/@Art/Background/Lobby/town.png`, `ArtSetup.Files`의 Lobby Scope Entry), 건물 넷 `BuildingView`(`Frame/Barracks`·`Inn`·`Coach`·`Gate`: 보이지 않는 단추, 빛 11%와 놋쇠 테 3의 Wash, 돌 판 이름표 280x50(둘째 줄이면 78), `IPointerEnter/Exit`와 Test용 `Hover`·`Leave`), 말풍선 `Frame/Bubble`(건물 위 40), 헤더(날짜·금·타이틀로), 띠 `Frame/Strip`(칩 `RosterChipView` 176x102: 이름·열 "1열"·직업 · 상태·칸 열 개; "로스터 n/8 · 사망 n/15"; [원정 화면으로]/[최종 결전으로] `Frame/Strip/Depart`), 그늘 `Frame/Shade`(헤더 아래 67%), 창 셋(`BarracksWindow` 1840x910: `RosterEntryView` 1792x92 여덟 줄 + [내보내기], 아래 요약 줄 / `InnWindow` 1200x860: [쉬기], `PatientView` 줄, 안내 / `CoachWindow` 980x560: 요약, `CandidateView` 줄(고칠 때마다 다시 만듦), "후보가 없습니다", 안내), 결과 창 `RunOverWindow` 900x500(제목, 줄 넷, [새 런]).
- Builder `UiPrefabSetup.Lobby.cs`(`BuildBuilding`·`Window`·`RunOverRow`·`BuildRosterChip`·`BuildPatient`·`BuildCandidate`·`BuildRosterEntry`). 핫스팟·이름표 자리는 README 71 넷째 지시의 값.
- 최종 결전 상태: 헤더 `Lobby.DayFinalStand`(금빛), 성문 `Lobby.FinalStand`+`FinalStandSub`, 쉼터 `InnClosed`, 띠 `ToFinalStand`. 쉬기 꺼짐, 고용·해고·치료는 됨.
- 정산 화면에 금 줄(`Settle.Gold` "얻은 금 +N (M층 도달) · 보유 금 K").

### 금·역마차·해고·치료 (Domain·Application·저장)
- 데이터: `BalanceData` `GoldPerFloor` 2·`GoldClear` 20·`HirePrice` 30·`RecruitCount` 3·`RosterLimit` 8·`TreatmentPrice` 20·`TreatmentRelief` 40(검증: `RosterLimit ≥ PartySize`). `MercenaryData.csv`에 `Starting` 열(시작 로스터 여섯은 참, 이름 풀 마흔은 거짓·`JobId` 비움). `StaticData.StartingMercenaries`·`RecruitPool`과 검증(시작 로스터 ≤ 상한, 풀 ≥ `RecruitCount`). Mapper `OptionalId("JobId")`·`Bool("Starting")`. Generated 둘.
- Domain: `RunState.Gold`·`Recruits`(`RecruitState`)·`Dismissed`·`ExpeditionsByDungeon`; `RunRules.DrawRecruits`(새 런과 정산 끝, `Derive(시드, "recruit", 원정 순번)`의 `RngStream.Recruit`)·`CanHire`/`Hire`(`HireCheck`)·`CanDismiss`/`Dismiss`·`CanTreat`/`Treat`(`TreatCheck`)·`FloorsReached`·`GoldFor`; `Settle`이 금을 더하고 `SettlementReport.GoldEarned`·`FloorsReached`·`GoldAfter`; `BeginExpedition`이 던전별 횟수를 셈. `BattleUnitSetup.JobId`(구성원의 직업; `ExpeditionArt`의 자세 조회가 직업 기준 `…OfJob`로).
- Application: `RunManager.CanHire`·`Hire`·`CanDismiss`·`Dismiss`·`CanTreat`·`Treat`(집에 있을 때만, 끝에서 저장).
- 저장 14: `RunRecord.Gold`·`Recruits`·`Dismissed`·`Expeditions`, `From13To14`(금 0·후보 없음·해고 없음·횟수 모름), 검증(음수 금, 상한 넘는 로스터, 후보의 수·이름·직업·겹침, 해고의 겹침, 횟수).
- 시뮬: `Simulations.Run`의 로비 정책 `--bench`(여섯까지 고용)·`--treat-above`(100 이상 치료), 금의 보고. 결과 Design/08 §24(300런: 승리 11% → 54%, 전멸 89 → 46%, 금 164 벌어 102 고용·12 치료, 끝의 로스터 3.0).
- 문구: `Lobby.*` 쉰셋(일곱을 지우고 서른여섯을 더함), `Expedition.RewardLabel`·`Reward`, `Settle.Gold`.

### Test
- EditMode: `LobbyEconomyTests`(열셋: 금과 층, 최종 결전의 금, 던전별 횟수, 후보의 뽑기와 결정론, 정산의 새 후보, 고용, 상한, 고용된 용병의 직업과 다시 뽑히지 않음, 풀이 모자랄 때, 해고와 마지막 한 명, 해고된 이름, 치료, 기한 뒤의 고용·해고·치료), `LobbyEconomyFlowTests`(둘: 관리자·저장·재시작, 막힌 저장), `RunSaveMapperTests`(13 → 14 변환, 왕복, 깨진 런 열여섯), `RunRulesTests`(새 런의 금·후보), `DefinitionTests`(`MercenaryData`), `StaticDataValidationTests`(상한·풀), `StaticDataTransformerTests`(`Starting`), TestData(이름 풀 다섯·상수)·TestCsv.
- PlayMode: `Lobby_TheTown_LightsABuildingUnderThePointer_OpensItsWindows_HiresTreatsAndDismisses_AndSaysFinalStandPastTheLastDay`(새), 로비 Test 다섯을 마을로(결과 창의 줄 넷, 막사의 줄과 띠의 칩), 원정 화면 Test에 보상 줄과 금, `UiTestUtil.OpenBarracks`·`CloseQuarter`·`RosterEntries`·`FillParty`·`RestAtTheInn`·`DepartFromLobby`, 스크린샷 `_61_lobby_hover`·`_57_lobby_barracks`·`_58_lobby_coach`·`_59_lobby_inn`·`_60_lobby_final_stand`·`_62_run_victory`(`_13_run_over`는 사망 한도의 패배로).
- 문서: Architecture/04(Lobby Scope), 07(14), 08("경제·고용·시설", 시드, 시뮬 run), 11(명령, 정산), 12("로비: 마을", 원정 화면, 정산, Test), 13(Unity 배선: 마을), Design/00·04·07 §4·08 §24, README 72 "판정", Roadmap(26단계 완료, 사후 검토 "26단계의 권장안 세부").

## Open

- 사후 검토: Roadmap "26단계의 권장안 세부"(금의 식과 출발값, `GoldBoss` 없음, 새 런의 후보, 이름 풀의 방식, 내보내기 확인 없음, 창과 띠의 크기, 시뮬 정책)와 README 71 "다섯째 지시"의 원정 화면 세부.
- 수치의 띠는 29단계(24단계 판정 12). 고용이 큰 레버(승리 11% → 54%)라 고용비·금은 27·28단계 뒤 함께 본다.
- 띠와 막사 줄의 레벨·경험치 자리는 27단계가 채운다.

## Verification

- `transform`·`validate` OK(BalanceData·MercenaryData). 시뮬 Design/08 §24(300런 다섯 변형).
- 체인 `20261016-212938`(setup·sim OK, EditMode 897/897, PlayMode 86/96 — 건너뜀 10은 그래픽이 있어야 하는 스크린샷 Test), 그래픽 스크린샷 `20261016-214301`(10/10, PNG 107장).
  그 앞의 체인 넷: `204329`(Test의 컴파일 오류 하나), `204838`(EditMode 5·PlayMode 50 — 창을 연 직후의 클릭이 그늘·창의 Raycast에 닿지 않아 `OpenQuarter`·`FillParty`를 Redraw를 기다리는 코루틴으로), `205810`(PlayMode 1 — 내보낸 줄의 Destroy가 그 프레임에 남음), `211210`(playmode만, OK).
- 스크린샷을 목업과 요소별로 대조했다(Round 71 `mock-town-td-1`·`mock-town-painted-enter`·`mock-compare-quarters`, Round 72 `mock-compare-run-end`): 마을·이름표 넷·말풍선(역마차 "후보 3명 · 고용 금 30")·띠(칩·"로스터 6/8 · 사망 0/15"·[원정 화면으로])·막사(줄·열 버튼·제외·내보내기·요약 줄)·역마차(후보 셋·[고용 금 30]·안내)·쉼터([쉬기]·줄·[치료]·안내)·최종 결전 상태(헤더·성문·쉼터·띠)·결과 창(제목·줄 넷·[새 런]) 모두 있음.
  대조로 고친 것: 건물의 빛의 테를 Outline에서 네 변의 그림으로(Outline은 반투명 사각형을 덮어 버린다), 영어 치료 단추의 문구를 줄임("Treat · 20 gold · -40"). 목업과 다른 채로 둔 것(사후 검토): 띠의 칩이 176 폭(여덟이 들어가게; 목업 232)이고 레벨 없음(27단계), 역마차의 후보가 열(목업은 칸 셋), 막사의 줄 92(목업 116)와 영어 패시브 문구의 말줄임.

## Next Action (제안)

- 사용자: 26단계의 결과를 보고 세션 정리(주제별 커밋: 데이터·Domain·저장 / 시뮬 / 화면과 Test / 문서)를 지시하거나, 27단계(성장)의 시작을 지시한다.
- 세션: 27단계는 Design/05 §2(레벨·경험치·HP)를 읽고 `LevelData.csv`와 띠·막사 줄의 레벨 자리부터.
