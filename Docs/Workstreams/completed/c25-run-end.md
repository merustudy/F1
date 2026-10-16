# Slice C 25단계: 런의 끝

Snapshot: 2026-10-16 (구현·시뮬·체인 OK. Round 72의 판정은 26단계의 입력. 세션 정리로 커밋·푸시함)

## Goal

Architecture/09 "Slice C" 구현 순서 1이 참이다: 100일을 넘기면 강제 최종 결전(최종 던전의 보스 하나), 최종 던전을 클리어하면 승리, 최종 결전의 패배·사망 한도·전멸이면 패배, 결과가 로비·정산에 보이고 새 런을 시작할 수 있다. 저장 13(11 이하는 읽지 않음). 시뮬 `run`이 런을 끝까지 돌려 보고한다.

## State

- 2026-10-16 사용자 "원정화면도 이미지 api를 이용해 생성해 목업 제공 / 25단계 시작": Round 71 다섯째(지도 생성 1회, E1·E2 목업 — 판정 대기) 뒤 25단계를 열었다. 24단계의 판정(Roadmap "24단계의 승인")이 설계이고, 빈 곳은 권장안으로 채워 기록했다(Roadmap "사후 검토 대기", "임시 조치").
- 구현했다(아래 Done). 시뮬은 Design/08 §23. 체인은 아래 Verification.
- **임시 조치**(28단계가 바꾼다): 최종 던전 행 `abyss`(5층·폭 1~2·3일·swift·치유 2·3층부터 1성·해금 광산 2회)와 무리 둘(광산의 적 재사용: `abyss_watch` 약탈자 둘·궁수·주술사, 보스 `abyss_lord_stand_in` 감독관·약탈자·주술사). 25단계의 규칙을 끝까지 돌려 보기 위한 자리 표시다.

## Done

- 데이터: `BalanceData.csv` `DeathLimit` 15, `DungeonData.csv`의 선택 열 `IsFinal`·`UnlockDungeonId`·`UnlockClears`와 임시 행 `abyss`, `EnemyGroupData.csv`의 임시 무리 둘, Generated 셋. `CsvRow.OptionalId`, `OptionalInt`가 없는 열을 허용.
- Domain: `RunOutcome`·`DefeatCause`, `RunState.Outcome`·`DefeatCause`(`IsOver`는 계산), `DepartCheck.FinalStandDue`·`Locked`, `RunRules.IsFinalStandDue`·`IsUnlocked`·`CanRest`·`CanDepart`·`BeginExpedition(finalStand)`·`Settle`의 `CloseTheRun`, `SettlementReport.Outcome`·`DefeatCause`; `ExpeditionState.FinalStand`, `ExpeditionRules.Create(finalStand)`, `MapGenerator.Generate(bossOnly)`; `DungeonData.IsFinal`·`UnlockDungeonId`·`UnlockClears`, `StaticData.FinalDungeon`과 검증(최종 던전 하나까지, 해금 던전의 id).
- Application: `RunManager.CanRest`·`IsFinalStandDue`·`IsUnlocked`.
- 저장 13: `RunRecord.Outcome`·`DefeatCause`(`IsOver` 뺌), `ExpeditionRecord.FinalStand`, `From12To13`, `OldestReadableVersion` 12(2~11의 변환 약 400줄 삭제), 검증 넷.
- 화면: 로비(`Dungeon`은 최종 결전이면 최종 던전, 아니면 첫 일반 던전; 안내 줄 "최종 결전" 금빛; 쉬기 `CanRest`; 결과 판 제목 `_runOverTitle`이 까닭을 말함 — Builder의 `RunOverTitle`을 Label로), 정산 화면의 마지막 줄(`RunOverText`), 문구 일곱(`Lobby.FinalStandDue`·`Victory`·`DefeatDeathLimit`·`DefeatFinalStand`, `Settle.Victory`·`DeathLimit`·`FinalStandLost`).
- 시뮬: `Tools/Sim run`(`Simulations.Run`: 로비 정책 — 피로 낮은 넷·권장 열, 피로 평균 `--rest-above`(60) 이상이면 쉬기, 최종 던전이 열렸으면 그것, 기한을 넘기면 최종 결전; `PlayExpedition`을 `RunExpeditions`에서 뺌), 보고(승리·패배의 까닭·일수·원정·쉬기·전사자·해금).
- Test: EditMode `RunEndTests`(일곱: 해금, 기한 뒤의 최종 결전과 보스 하나의 지도, 최종 던전 클리어의 승리, 최종 결전의 승리·패배, 사망 한도, 전멸), `RunEndFlowTests`(셋: 관리자와 저장을 지나는 최종 결전과 승리, 해금 뒤의 클리어, 치명적인 최종 결전의 패배), `RunSaveMapperTests`(12 → 13, 깨진 결과 넷; 2~11의 변환 Test 열을 지움), TestData의 최종 던전 `pit`(1층 + 보스, 동굴 1회 클리어로 해금). PlayMode는 `IsOver` 꾸밈을 결과로.
- 문서: Architecture/07(13, 11 이하 거부, 변환의 경위는 Git), 08("런의 끝"), 11(로비의 명령, 정산이 런을 끝냄), 12(로비·정산 화면), Design/03 §1(임시 행), 04 §2·§4·§8(구현), 07 §4, 08 §23, 00 현황. 목업 `ArtPipeline/Archive/72-run-end/`(마을 위의 결과 창과 최종 결전 상태, 판정 대기).

## Open

- Round 71 다섯째(원정 화면 E1/E2)와 Round 72(결과 창·최종 결전 상태)의 판정. 26단계가 구현한다.
- 사후 검토(Roadmap "25단계의 권장안 세부"): 최종 결전의 세부(101일째부터, 쉬기 불가, 보스 하나), 사망 한도 15(고용 뒤 다시: 지금은 전멸이 먼저), 시뮬 로비 정책의 값(쉬기 60), 임시 심연의 수치.
- 28단계: `abyss`를 Abyss Lord와 호위로(임시 조치를 지움). 26단계: 로비·원정 화면, 금·고용·시설, 마을 배경의 배선.

## Verification

- `transform`·`validate` OK(던전 둘). 시뮬(Design/08 §23): 300런 승리 11.0%, 전멸 88.7%, 끝난 날 44.5, 원정 7.5; 임시 심연 원정 클리어 33.8%.
- 체인: 첫 실행 `20261016-174939`은 Test의 컴파일 오류(옛 변환 Test를 지우며 도우미 `Inv`까지 지움), 둘째 `20261016-181841`은 Test 데이터 둘(`TestCsv`의 `DeathLimit`, 던전을 바꿔 끼운 Test의 pit 적 무리)과 E2의 미완 → 셋 다 고친 뒤 체인 `20261016-192628`(setup·sim OK, EditMode 863/863, PlayMode 85/95 — 건너뜀 10은 그래픽이 있어야 하는 스크린샷 Test), 그래픽 스크린샷 `20261016-193944`(10/10, PNG 95장). 25단계의 Test 열셋이 모두 지난다.

## Next Action (제안)

- 사용자: Round 72(결과 창·최종 결전 상태)의 판정 — 26단계가 마을 로비와 함께 구현한다(Round 71 다섯째는 "E2 구현"으로 판정됨).
- 세션: 체인 결과를 적고 세션 정리(커밋은 지시가 있을 때: 24단계·Round 71·25단계를 주제별로).
