# Slice D 35단계: 막사의 레벨업

Snapshot: 2026-10-18 (완료. 커밋 안 함)

## Goal
레벨업을 정산에서 막사의 클릭으로 옮긴다(Design/15 판정, Round 82 안 A). 정산은 경험치만 쌓고 "레벨 업 가능"을 한 줄로 알리며, 막사 줄의 [▲ 레벨 업]이 한 레벨과 직업의 굴림을 하고 저장한다. 로비 띠의 ▲와 막사 이름표의 ▲N이 알린다. 범위는 Architecture/09 "Slice D" 구현 순서 4.

## State
- 2026-10-18 사용자 "세드릭 삭제, 카이는 전사로 / 옛 저장은 삭제 / 나머지는 권장안 구현"으로 시작했다. 커밋 안 함(31~34단계의 변경도 작업 트리에 함께 있다).

## Done
- Domain: `LevelRules.Gain`(쌓기만, 할 수 있는지 돌려줌), `CanLevelUp`, `LevelUp(data, 용병, 런 시드)`(시드 `Derive(시드, "growth:" + id, 오른 레벨)`), `RunRules.CanLevelUp`·`LevelUp`, `Settle`의 `ReadyToLevelUp`. `StatBlock.Plus`.
- Application: `RunManager.CanLevelUp`·`LevelUp`(저장 한 번, `RequireAtHome`).
- 저장 20: 경험치 검증은 0 이상과 마지막 레벨의 0. 19 → 20은 버전만.
- 화면: `RosterEntryView`(버튼, 금빛 글·막대 192, +N, 놋쇠 테두리), `UiText.StatValue`·`ChipLevel`, `BuildingView.ShowCount`, `LobbyScreen`(`_grown`은 창이 닫히면 비움, `heal` 소리), `SettlementScreen.ShowReady`, 빌더(버튼 1348,20 148x52·조용히, 테두리, 칩의 레벨 칸 100..166, 이름표의 알약 70x34, 정산의 `Ready` 줄 금빛), `UiPalette.LevelUp`·`LevelUpText`, 문구.
- 시뮬: 정책이 정산 직후 모두 올림. 08 §39.
- Test: `LevelRulesTests`·`GrowthTests`·`RunSaveMapperTests`(20), PlayMode `Lobby_Barracks_LevelsUpWhoseExperienceMeetsTheLevel_…`, 한 바퀴의 정산 줄, 스크린샷 `_72`·`_73`·`_74`.
- 문서: Design/15 판정, 05 §1·§2, 02 §7, 04 §5, 07 §4, 00, Architecture/07·08·09·11·12·14, CLAUDE.md §9, Roadmap.

## Open
- 런이 띠 위(균형형 89.7%, 안전형 88.7%): 37단계.
- 36단계(직업 재편): 방랑자 그림은 이미지 API 호출이 필요하다 — 사용자의 요청을 받아서.

## Verification
- `dotnet build Tools/Sim` 오류 0.
- 체인 `20261018-191509` CHAIN OK(EditMode 995/995, PlayMode 93/104, 건너뜀 11은 그래픽 스크린샷 Test).
- 스크린샷 `20261018-levelup35`를 Round 82 목업과 대조: 테두리의 좌우가 목록의 마스크에 잘림(→ 줄 안쪽의 이미지 넷), "+N"이 값을 왼쪽으로 밂(→ 투명한 앞 글), 공사 중인 막사의 문구가 버튼과 어긋남(→ `Lobby.BarracksLockedNote`). `20261018-193801` OK.
- `20261018-levelup35b`에서 오른 값이 통째로 사라짐: TMP의 `</alpha>`는 닫히지 않는다(→ `<alpha=#FF>`). `20261018-levelup35c`에서 목업과 같음, 체인 `20261018-200426` CHAIN OK.

## Next Action
- 36단계(직업 재편)는 시작 지시를 받은 뒤에. 방랑자 그림은 이미지 API 호출이라 사용자의 요청이 필요하다(그 전에는 Design/15 §4-3의 대안).
