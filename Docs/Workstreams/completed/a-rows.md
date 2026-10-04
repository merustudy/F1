# Slice A 개정 — 열 (1열~4열, 한 열에 한 유닛, 4인 파티)

Snapshot: 2026-10-01 (완료. 사후 검토에서 조합 절벽은 "다"를 채택해 개정 2로 이어졌다: `completed/a-rows-items.md`)

## Goal

전열/후열 두 열을 없애고 열을 다시 정한다: 양쪽 모두 1열~4열, 한 열에 한 유닛, 앞 열이 비면 뒤 열이 전진. 파티는 4인까지다.
아이템마다 쓸 수 있는 열이 있고, 공격 범위는 앞이나 뒤에서 세어 몇 번째까지 맞는가로 정한다.
Slice A의 루프, 저장, 화면, 시뮬이 새 규칙으로 그대로 돈다.

## State

- 구현, 문서, 검증이 끝났고 2026-10-01 사용자 지시로 `feature/slice-a`에 주제별로 커밋하고 `origin`에 푸시했다.
- 개정은 세 번 있었다(9-30에 3열, 이어서 4열. 10-01에 파티도 4인). 셋이 합쳐진 최종 상태만 커밋됐다(3열이던 중간 상태는 남기지 않았다).
- 같은 날 전투 화면도 두 번 바꿨다: 로그 숨김과 로그 보기, 그리고 목업 C안대로의 개편. 둘 다 같은 커밋 묶음에 있다.
- 승인 범위와 사후 검토 목록: `Docs/Roadmap.md` "열 개정의 승인", "사후 검토 대기".

## 결정

사용자가 정한 것 (2026-09-30, 기획문서의 `【확정】 (열 개정)`):

1. 열은 1열부터 4열까지다.
2. 한 열에 한 유닛이다. 파티도 적 무리도 그렇다.
3. 앞 열이 비면 뒤 열이 한 칸씩 전진한다.
4. 공격 범위는 "앞에서 몇 번째까지" 또는 "뒤에서 몇 번째까지"다.
5. 옛 저장 파일(전열/후열 때의 것)은 읽지 않는다.
6. 용병도 4열까지 선다: 파티는 4인까지다(`PartySize` 4). (2026-10-01 "용병도 4열까지 확장 구현")

"나머지는 권장안대로", "잘못된 부분이 있다면 권장안으로 구현"으로 맡겨져 권장안으로 정한 것 (`【확정】 (열 개정 권장안)`, 검토 대상).
목록은 `Docs/Design/00_INDEX.md` "열 개정". 그중 지시문만으로는 정해지지 않아 판단이 들어간 것:

- **"용병도 4열까지"를 파티 4인으로 읽었다.** 전진 규칙 때문에 세 명은 늘 1~3열에 서므로, 용병이 4열에 서려면 파티가 넷이어야 한다.
  `PartySize`(데이터) 3 → 4. 최소 인원(`MinPartySize` 1)은 그대로다.
- **적을 4인 파티에 맞췄다.** `PartySize`만 바꾸면 기본 파티가 99.7%라서, 적 무리를 채웠다(약탈자 순찰 셋, 매복 넷, 보스 무리는 감독관 뒤에
  약탈자와 주술사). 보스 HP는 300 그대로. 재 본 다른 안과 수치는 Design/08 §7.
- **쓸 수 있는 열은 서 있는 열의 번호로 센다.** 앞/뒤에서 세는 것은 맞는 쪽뿐이다. 쓰는 쪽도 "앞에서 몇 번째"로 세는 안은
  Design/08 §6의 선택지 "다"로 남겼다.
- 살아 있는 적이 깊이보다 적으면 있는 만큼만 맞힌다. 적 전부를 맞히는 범위(`EnemyAll`)는 깊이와 따로 둔다.
- 열의 수와 "한 열에 하나"는 `BalanceData`가 아니라 구조다(`BattleRows.Count`, 정원 Key 없음).
- `run.json`의 Schema Version은 2 그대로다. 4열로 바꾼 것은 DTO의 모양을 바꾸지 않는다(Architecture/07 "Schema Version").
- 콘텐츠: 적 무리를 한 열에 하나로 다시 짰고(넷까지), 새 보상 아이템 `spear`, `halberd`, `longbow`는 뒤에서 2번째까지, 보스 HP 300.

## Done

- 기획: Design/02 §2·§3·§4·§7, 03 §4·§5·§6, 04 §6, 05 §2, 06 §3, 07 §4, 09 §1-6, 01 §6, 00("열 개정"), 08 §6(시뮬). CLAUDE.md §5-5.
- Data: `BattleRows`(열 번호, `Count` 4), `ItemData.Rows`, `ItemEffect.Reach`, `TargetMode.EnemyFront`·`EnemyBack`,
  `PassiveSpec.Rows`와 `PassiveCondition.InRows`, `EnemyGroupData.Enemies`(앞에서부터의 목록), `CsvRow.IntList`.
- Domain: `Gameplay/Battle/Formation.cs`(자리 규칙), `BattleEngine`(전진 `AdvanceBehind`, 아이템 `RefreshItems`, `ResolveTargets`, `RowsAdvanced`),
  `RunRules`(`PlaceInParty`, `RemoveFromParty`), `ExpeditionRules`(`MoveToRow`, 전투 뒤 자리 유지).
- Flow와 Save: `RunManager.PlaceInParty`/`RemoveFromParty`, `ExpeditionManager.MoveToRow`, `RunSaveData` Schema Version 2(열은 번호),
  `RunSaveMigrator.OldestReadableVersion = 2`.
- UI: 로비의 열 버튼(파티가 서는 열만 보인다)과 제외, 보드의 앞으로·뒤로와 열 순서 정렬(카드를 낮춰 넷이 들어간다), 노드 정보의 적 목록(앞에서부터),
  전투 화면(아군 4열 + 적 4열, 열마다 카드 한 장). 문구(`UI_StaticText.csv`), 직업 패시브 설명문. 화면 Prefab을 다시 만들었다.
- 전투 로그(2026-10-01 지시): 전투 중에는 숨기고, 결과 Panel의 "로그 보기"가 전체 로그를 스크롤로 연다(`BattleScreen.BuildLog`,
  `UiBuild.VerticalScroll`).
- 전투 화면 개편(2026-10-01 목업 C안 승인): 헤더 왼쪽 후퇴, 그 아래 포션, 상단 여백, 바닥선 위의 전신 그림 자리(도형)와 정보 카드,
  아이템 보드(세로 칸 48px, 도형 아이콘, 빈 칸 표시, 최대 6칸). 로그 박스와 아래 조작 Panel 제거. `UiPrefabSetup.Battle.cs`, `BattleUnitView`, `BattleItemView`.
- 시뮬: `Tools/Sim`의 `formations` 명령, 파티에 `:1`~`:4`로 자리 지정, 통계에 아군 전진 횟수. 기본 파티는 넷이다.
- Owner 문서: Architecture/05, 07, 08("자리 (열)"), 09, 11, 12("열을 보여 주는 방식", "전투 화면").

## Open

- **조합 절벽** (Design/08 §7): 4인 파티에서 더 가파르다. 뒤 열에 설 수 있는 직업이 bishop과 archmage뿐이라 쓸 만한 파티는 둘을 다 넣은
  세 가지(70~83%)이고, 하나가 죽으면 40% 아래다. 3인 파티는 이제 12%다. 선택지는 §6의 가~라 그대로이고 결정 대기다.
- 100일 루프(피로도, 여섯 가운데 넷을 번갈아 보내는 것)는 시뮬 도구가 없어 재지 않았다.
- 옛 저장 파일(Schema Version 1)은 읽지 않는다. 개정 전에 플레이하던 런이 있으면 타이틀이 읽을 수 없다고 알리고 새 런만 받는다.
- 3열이던 중간 상태로 저장한 파일(Version 2)이 있다면 읽힌다. 진행 중이던 전투는 지금 규칙으로 다시 재현된다
  (Architecture/07 "이어하기"의 `Diverged`, `Restarted`). 사용자의 실제 Save 폴더는 확인하지 않았다(건드리지 않는다).
- 사람이 직접 플레이한 확인은 아직 없다.
- 큰 아이템이 여러 칸을 차지하는 것은 화면만 준비됐다(`BattleUnitView.CellsPerItem` 1). 아이템 크기 데이터(`ItemData`에 `Size`)와 보드·보상 규칙은 기획 결정 뒤에 넣는다.
- 그림 자리와 아이콘은 도형 Placeholder다. 9단계(그림)에서 바뀐다.

## Verification

- `Tools/chain.sh`: setup OK, sim OK, EditMode 529/529, PlayMode 39/39(`[Explicit]` 스크린샷 3개 제외). setup을 두 번 돌려도 파일이 바뀌지 않는다.
  ProjectSettings, Packages, Scene은 바뀌지 않았다.
- 새 Test: `FormationTests`, `BattleEngineTests`의 "Advancing" 묶음과 범위(`Reach`) 타깃, 데이터의 열과 깊이 검증, 자리 명령
  (`RunRulesTests`, `ExpeditionRulesTests`), 저장(열 번호, 옛 버전 거부), UI(로비 열 버튼, 보드 앞으로·뒤로, 전투에서 열이 비었을 때의 카드).
- `Tools/screenshots.sh`: 31장. 로비의 열 버튼 넷, 보드의 카드 넷, 전투 화면(헤더의 후퇴, 포션, 상단 여백, 그림 자리와 카드, 로그 보기), `ko_15_battle_after_advance`(적의 전진 직후).
- 시뮬(3,000회, 시드 1): 기본 파티 knight·spellblade·bishop·archmage(1~4열) balanced 80.7% / 사망 0.27. 3인이던 때 80.2% / 0.20,
  전열/후열 때 81.7% / 0.22. 전체 표와 재 본 안은 Design/08 §7(4인), §6(3인).

## 알아둘 것

- 자리를 바꾸는 코드는 `Formation`을 거친다. 어느 상태(로비 파티, 원정 구성원, 전투 Setup)도 빈 열을 사이에 두지 않는다.
- 전투 중에 바뀌는 것은 유닛의 `Row`와 아이템의 `Active`다. `Index`(유닛 순서, 입력의 `PartyIndex`)는 바뀌지 않는다.
- 파티의 열에 딸린 화면 요소는 Prefab에 `BattleRows.Count`만큼 있고 `PartySize`가 그보다 적으면 화면이 넘는 것을 감춘다(지금은 같아서 전부 보인다).
  전투 화면의 Column 자리와 폭은 화면이 열 때 정한다(`BattleScreen.LayoutColumns`). 파티 보드의 카드는 넷이 들어가게 98px다.
- 전투 화면은 죽은 유닛의 카드를 치운다. 죽은 유닛의 `Row`는 죽은 자리 그대로라서 카드 위치에 쓰지 않는다.
- PlayMode의 전진 Test는 운에 맡기지 않으려고 1열 용병의 무기 등급과 HP를 직접 올려 보스전까지 간다(`UiTestUtil.ReachAnEnemyAdvanceInTheBossBattle`).
- 아이템이나 문구를 더하면 Font Atlas가 다시 구워진다(글자 집합이 바뀌므로). `Tools/chain.sh setup`이 한다.
- 데이터를 바꿔 보는 시뮬은 저장소를 건드리지 않고 할 수 있다: `Assets/@Data`를 다른 폴더에 복사해 고치고
  `transform --project-root <그 폴더>`, `expedition --project-root <그 폴더>`.

## Next Action (제안)

1. 사용자가 Unity에서 직접 플레이하고(`Assets/@Scenes/Boot.unity`) 새 전투 화면과 4인 파티를 본다.
2. 조합 절벽의 선택지(Design/08 §7)와 아이템 크기(여러 칸) 규칙을 정한다.
3. 사후 검토가 끝나면 이 문서를 `completed/`로 옮기고, 9단계(그림)는 시작 지시를 받은 뒤 시작한다.
