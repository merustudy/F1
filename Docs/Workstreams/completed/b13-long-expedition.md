# Slice B 13단계: 긴 원정

Snapshot: 2026-10-06

## Goal

원정이 지도 15층 + 보스(16층)가 되고, 정예 노드(더 강한 무리)와 야영지 노드(쉬기)가 생긴다. 깊은 층의 적이 강해진다.
16층 지도와 야영지의 화면이 승인받은 목업대로 있고, 시뮬로 출발값을 잡는다. 보드는 5칸 그대로(사용자 "D").
범위와 순서: Architecture/09 "Slice B", 규칙: Design/03 §1·§4.

## State

- 목업 Round 34의 판정(사용자 "1. A 2. B 3. 권장은 / 구현"): 지도 A(위로 스크롤), 야영지 B(지도 위의 창), 표식은 그린 도형. 구현했다.
- 출고 데이터를 15층 출발값으로 바꿨다. 시뮬은 Design/08 §9.
- 커밋 안 함(12단계와 함께 세션 정리에서). 브랜치 `feature/slice-a`.

## Done

- Domain: `MapNodeKind.Elite`·`Camp`, `MapNode.IsFought`, `MapGenerator.DrawKind`, `ExpeditionPhase.AtCamp`, `ExpeditionRules.EnterCamp`·`RestAtCamp`, 야영지에서의 전투 거부,
  `BuildBattleSetup(…, floor)`의 깊은 층 HP·등급. 데이터 Type: `DungeonData`의 새 열 일곱과 `EliteCanStandOn`·`CampCanStandOn`, `EnemyGroupData.IsElite`, `StaticData.ElitesFor`와 검증,
  `BalanceData.CampHealPercent`·`CampFatigueRelief`. 변환기.
- Application: `GamePhase.Camp`, `ExpeditionManager.EnterNode`(야영지)·`RestAtCamp`. 저장: `run.json` 4의 `AtCamp`(검증). 시뮬: 정예·야영지·전투 시간 보고, `map` 명령.
- 데이터: `DungeonData.csv`(15층 출발값), `EnemyData.csv`(고블린 고참·주술 대가, 감독관 HP 400), `EnemyGroupData.csv`(정예 둘, 층 범위, 보스 무리). Generated JSON.
- 화면: `node_elite`·`node_camp`(`Archive/34-long-map/draw_icons.py`), `UiArt`, `MapNodeView`(종류별 표식), `UiPrefabSetup.NodeMap`(스크롤 뷰, 야영지 창, 바뀌는 안내 줄과 버튼),
  `NodeMapScreen`(층 사이 88.5, 지금 층으로 스크롤, 종류의 이름과 안내, 야영지 창, 쉬기), 문구 10개.
- Test: EditMode `LongExpeditionTests`, `CampFlowTests`, `ShippedDataTests`(야영지 건너뜀, 층마다의 Setup). PlayMode `NodeMap_TheLongMapScrollsUp…`, `NodeMap_EveryNodeShowsTheMarkerAndNameOfItsKind…`,
  `Camp_ItsWindowOpensOverTheMap…`, `UiSaveTests.Continue_AfterTheAppWasClosedAtACamp…`, 맵을 걷는 도우미 `UiTestUtil.GoIntoTheFirstNode`(야영지에서 쉼). 스크린샷 `_31_map_deep`, `_32_map_camp`, `_33_camp`.
- 문서: Design/00 "Slice B", 03 §1·§4, 07, 08 §9. Architecture/07(저장 시점, 검증, 버전), 08 "긴 원정", 11(단계 표, 야영지), 12("노드 맵의 오른쪽", Test), 13(그린 표식). Round 34 README.

## Open

- 원정 한 번의 실제 길이(15~30분)는 16단계에서 직접 플레이로 잰다. 전투 시간만 x1로 154초다.
- 조합 절벽이 커졌다(약한 조합 0.1~1.1%). 16단계에서 다시 본다(Design/08 §9).

## Verification

- `dotnet build Tools/Sim` 오류 0, `transform`·`validate` OK(직업 6, 아이템 21, 적 7). 지도 400개의 분포는 Design/08 §9와 같다(6~13층 정예 약 15%, 야영지 약 13%).
- 시뮬(시드 1): 기본 파티 balanced 3,000회 69.1% / 0.31, safe 57.5% / 0.29, none 76.9% / 1.40. 레버와 여섯 파티는 Design/08 §9.
- 체인 1(03:50, `~/Library/Caches/F1/chain/20261006-035048`): setup OK, sim OK, EditMode 678/678, PlayMode 66/66(`[Explicit]` 8개 제외, 469초 — 전에는 387초. 16층을 걷는 Test).
- 체인 2(지도의 여백과 스크롤 자리를 고친 뒤): setup·EditMode 678/678, PlayMode 66/66(04:07, `20261006-040759`). `git diff --check` 깨끗, `Assets/InitTestScene*` 없음.
- 스크린샷 53장(8/8): `20261006-b13`(처음), `20261006-b13b`(고친 뒤). 고친 것: 1층 노드의 이름이 바닥에 잘림, 깊은 층에서 위 끝에 다음 층 이름의 조각이 걸림.
  지금: 시작 지도는 1~3층과 4층 노드의 아래 절반, 깊은 지도는 지금 층 아래로 지나온 층이 조금, 위 끝은 노드의 한가운데. 야영지 창은 목업 B와 같다(영어도 들어간다).

## Next Action (제안)

- 14단계(단계와 합치기): 단계와 효과 크기, 보상의 단계(정예는 한 단계 위), 합치기, 야영지의 정비(창의 두 번째 카드), 단계의 표시(목업), 시뮬. Handoff는 `b14-tiers-merge.md`.
