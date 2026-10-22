# Slice D 50단계: 유니크의 조합 (+ 49단계의 화면)

Snapshot: 2026-10-22 밤 (사용자 "기존 담금질로 유니크 아이템을 만들었던걸 백팩배틀즈 방식으로 바꾸자. 전투 중에 3성 조합끼리 인접해 있으면 전투 후 이펙트와 함께 유니크가 되는 방향도 같이 검토" → Design/29 → 판정 "3. 하나의 촉매 혹은 아이템을 기반으로 여러 조합이 준비되는 경우 이펙트로 연결된 하나만 / 6. 해당 유니크 아이템은 하나로 제한하며 다른 유니크 아이템은 가능 / 나머지 권장안" → Round 96 목업 → 1차 "1. 잇는 선으로(각 아이템 중심점끼리) / 3. 금빛 고리 제거, 합쳐질 때 금빛이 퍼지는 다른 이펙트 제공(합쳐지기 전에는 따로 없다가) / 4. 막음 / 나머지 권고안. 목업 제공. 내 승인 후 구현" → 2차 목업 → **"권장안 반영"** → 구현)

## Goal
야영지의 담금질 대신 백팩 배틀즈 방식: 같은 보드에서 레시피의 바탕(3성)과 촉매(3성)가 변을 맞대고 있으면 보스가 아닌 전투를 이긴 순간 유니크가 된다. 보드에는 잇는 선, 이긴 뒤에는 금빛 번짐과 띠의 줄, 그동안 조작을 막는다. 49단계의 화면(사망 문장, 코인 더미)을 함께.

## State
- 결정: 00 "Design/29의 판정", 02 §4 "유니크의 조합", 03 §1(야영지 둘), Round 96 README(1·2차 판정). Architecture/08·09·12·14, CLAUDE.md §9.
- Domain: `ForgeRules`(다시 씀 — `Links`: 읽는 차례, 조합될 짝 먼저, 하나의 재료는 하나의 짝, 이미 가진 유니크와 이번에 만들 유니크는 다시 잇지 않음, 단계 모자란 짝은 회색; `ForgeAfterBattle`; `PartyHas`; `Touches`), `ForgeLink`·`UniqueForged`, `ExpeditionState.LastForged`(저장 안 함), `ExpeditionRules.CompleteBattle`(코인 다음, 전리품 전). 야영지의 `ForgeAtCamp`·`Options`·`OptionAt`·`CanForgeAtCamp`·`PartyHasUnique` 삭제.
- Flow: `ExpeditionManager.ForgeLinks()`·`LastForged`, 담금질 명령 넷 삭제.
- 화면: `LinkLines`(선 4px·점 10px·검은 테, 금빛/회색), `UniqueMergeView`·`DiscGraphic`(V1 금빛 번짐), `GridBoardView`(`links`, `PieceLayer`·`PieceTemplate`·`Links`), `PartyBoardView`(그 용병의 선), `BattleBoardView.ShowLinks`(전투 중), `BattleScreen`(이긴 뒤의 조합·소리 `ItemPlace` 0.45초·띠 아래 `BandUnique` 0.95초부터·막힘 `MergeLock` 1.3초·코인 더미 비행 0.4/1.1초부터 0.5초·띠 코인의 맥동), `NodeMapScreen`과 `UiPrefabSetup.NodeMap`의 담금질 삭제(야영지 카드 둘), `UiPrefabSetup.Battle`(`BandUnique`·`CoinPile`), `UiPalette`(`LinkReady`·`LinkShort`·`LinkRim`·`MergeGold`·`UniquePlate`, `ForgeCard*` 삭제), 문구(`Battle.UniqueMade`·`Battle.CoinPile` 새로, `Item.Recipe`·`Item.UniqueLine`·`Death.AfterGrace` 고침, `Map.Forge*` 여섯 삭제).
- 시뮬: 정책의 야영지 담금질 삭제, 보고 "uniques made after won battles". 광산 1,000(시드 1): 클리어 64.8%, 유니크로 끝나는 원정 0.7%.
- Test: EditMode `ForgeRulesTests` 다시 씀(맞닿음 셋·대각선·회색·숫돌 하나에 바탕 둘·조합될 짝 먼저·유니크마다 하나·같은 유니크 둘·조합의 결과·이긴 전투), PlayMode `Win_ARecipeSideBySide_MergesIntoTheUnique_WithTheLineTheGoldAndTheLock`(야영지 담금질 Test를 대신함), 스크린샷 `ko_52_camp_recipe_link`·`TheUniqueMerge_Korean`(`ko_86`~`ko_90`), `UiTestUtil.CampForge` 삭제.
- 저장: 형식 그대로(27). 조합은 전투를 끝내는 명령 안.
- 커밋(2026-10-22 세션 정리, 49단계와 함께): 기획 `9d9ef95`, 구현 `b2a036e`, 화면 `9351cfb`, Round 96 `3b51edc`, 푸시 origin/feature/slice-a.

## Verification
- 체인: `20261022-202838`은 컴파일 실패(같은 이름의 보조 함수 `Smooth`) → `20261022-202926`은 setup 실패(지운 담금질 문구 하나가 여러 줄 칸이라 꼬리 두 줄이 남음 — 지움, 이 체인은 멈춤) → `20261022-203042` **CHAIN OK**(EditMode 1113/1113, PlayMode 97/111 — 건너뜀 14는 스크린샷 Test).
- 스크린샷 `20261022-d50`(14/14, Unity 설정 변화 없음)을 Round 96 목업과 대조: 선·야영지 카드 둘·코인 더미·금빛 번짐·끝·전리품의 흐림은 맞음. 어긋남 둘을 고침 — (1) 막힘 동안 단추를 꺼서 반투명 → 로그 보기 뒤의 해골 더미가 비침: 끄지 않고 검은 덮개 `LockShade`(50%)를 얹음(`BattleScreen.ButtonsShaded`, PlayMode 단언도 그에 맞춤), (2) 띠 아래 판이 올리브빛(`Outline`의 금빛이 90% 판 밑에 깔림): 테와 채움이 겹치지 않는 `RimmedPlate`로. 기록은 README 96 "구현"·Architecture/12.
- 고친 뒤 체인 `20261022-210802` CHAIN OK → 스크린샷 `20261022-d50b`(14/14): 단추 덮개는 목업대로, 판은 그려지지 않음(설정 코드가 `CanvasRenderer`를 붙이지 않음 — 런타임에 만드는 `LinkLines`·`DiscGraphic`은 붙여 둠) → 붙임 → 마지막 체인 `20261022-213852` **CHAIN OK**(setup·sim OK, EditMode 1113/1113, PlayMode 97/111 — 건너뜀 14는 스크린샷 Test) → 스크린샷 `20261022-d50c`(14/14, Unity 설정 변화 없음): 판(짙은 바탕·금빛 테 2px·모서리 6)과 막힘 동안의 단추(불투명하게 어둡고 글자도 흐림, 해골이 비치지 않음)가 목업과 맞음.

## Open
- 43단계 2차(런 띠): 출발점 군주 600 + 광산 7일.
- 새 직업 셋의 레시피(유니크 아이템·아이콘)는 다음(Design/29 판정 10).

## Next Action
- 이 단계는 끝났다. 사후 검토는 Roadmap "사후 검토 대기"의 50단계 줄. 다음은 43단계 2차(`active/d43-tuning.md`), 새 직업 셋의 레시피는 그 뒤(Design/29 판정 10).
