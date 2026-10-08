# Slice B 16단계: 맞추기

Snapshot: 2026-10-07 (18단계 뒤)

## Goal

Slice B의 Acceptance 1~8(Architecture/09 "Slice B")이 참이다: 원정 하나가 x1로 15~30분이고 그 안에 긴장의 곡선이 있으며(정예, 야영지, 쌓이는 피로),
시뮬 수치가 받아들일 만한 띠에 있고, 직접 플레이로 재미가 확인된다. 조정은 시뮬 우선 워크플로(Design/09 §4)로 하고, 확정된 것만 Design/08 §12와 해당 절에 적는다.
끝나면 Slice B 완료다(CLAUDE.md §9, Roadmap).

## State

- 2026-10-06 시작: 기준값 측정, 세 걸음(기준 잡기 → 직접 플레이 → 맞추기와 마감)과 결정 질문 Q1~Q6의 설계 제안. 10-07 Round 41(사용자 "UI 개선 먼저")을 구현·커밋·푸시했다.
- 2026-10-07 **설계 승인**: 사용자 **"Q5 화상은 피로 없음, 폭풍은 틱마다 피로. 나머지는 권고안"**(Roadmap "16단계의 승인"에 Q1~Q6의 답).
- 승인대로 구현했다(아래 Done): 플레이 기록(Q1 b), 틱의 피로(Q5), 그리고 Q5로 띠 밖까지 내려간 붕괴 비율을 되돌리는 피격 2·처치 5(권장안 J, 사후 검토).
- 2026-10-07 세션 정리(사용자 "지금 유지하자. 세션 정리"): 주제별 여섯 커밋(규칙과 데이터 / 플레이 기록 / 화면: 아이템 카드 / 그림: Round 42·43 / Unity 설정 / Roadmap과 Handoff)을 `origin/feature/slice-a`에 푸시했다. 다음 걸음은 **사용자의 직접 플레이**(x1로 원정 하나를 끝까지).
- 2026-10-07 Round 44의 판정 **"권장안 구현"** → **17단계**를 열어 상점 노드와 지역 코인을 구현하고 세션 정리에서 커밋·푸시했다(`b17-shop.md`가 함께 active다). 16단계의 맞추기와 마감은 그 뒤에, 상점이 있는 데이터로 한다. 직접 플레이의 체크리스트에 상점을 더한다(아래).
- 2026-10-07 사용자가 "16·17단계 이어서, 플레이 기록과 메모로 맞추기와 마감(17단계의 수치도 함께)"을 지시했으나 **플레이 기록은 없었다**(`~/Library/Application Support/funitup/F1/Logs/`가 없다 — 앱으로 원정을 하지 않았다). 메모의 내용은 새 제안(몬스터의 전리품)이어서 **Round 45 → 18단계**(`b18-loot.md`)로 구현했고, 맞추기의 수치는 그 데이터로 다시 잡았다(Design/08 §14): 붕괴가 난 원정이 10%로 내려가 처치 `FatigueOnKill`을 5에서 3(15단계의 값)으로 되돌렸다 → balanced 66.1% / 0.30, 붕괴 42.5%. J의 "피격 2·처치 5"는 "피격 2·처치 3"이 됐다.
  직접 플레이와 마감은 그대로 남았다(아래 Open). 체크리스트에 전리품을 더했다.
- 2026-10-07 사용자가 검토 항목을 더했다: **상점 노드와 지역 코인**("전투 선택지에서 상점 신설 검토 — 지역 코인(런-전투에만 누적, 끝나면 초기화), 아이템 형태 + 우클릭 툴팁, 누적 증가하는 새로고침. 유사 게임 검토 후 목업")
  → **Round 44**: 레퍼런스(Slay the Spire, Hades, Balatro, Brotato, Backpack Battles, Super Auto Pets)와 목업 여섯(`ArtPipeline/Archive/44-shop-node/`: 노드와 표식, 창 A/B/C, 사기 ㄱ/ㄴ, 새로고침, 코인의 자리). 호출 없음.
  권장: 지역 코인은 원정 안에서만(귀환 정산에 소멸), 표식 A(코인 더미), 창 A(지도 위, 보드 칸 모양의 물건 넷), 사기 ㄴ(좌클릭 고르기 + 카드, 칸에 놓을 때 삼. 우클릭은 들이지 않음), 새로고침은 시작값 + 증가(나가면 초기화),
  **범위는 17단계(16단계 뒤, Slice B에 더함)**. 판정할 것 여섯은 README 44. **판정 대기**. 플레이 기록은 아직 없다(`Logs/`가 없음).
- 2026-10-07 사용자가 검토 항목을 더했다: **아이템 클릭 툴팁**(Backpack Battles처럼, 아무 곳이나 누르면 닫힘) → **Round 42** 목업 다섯과 README(`ArtPipeline/Archive/42-item-tooltip/`). 권장 안 1(먹색 판 + 놋쇠 선) + 자리 P1(파티 쪽은 패널 위, 전투는 보드 옆) + 동작(고르기 그대로 + 카드, 다음 클릭에 카드만 닫힘) → 사용자 **"권장안 구현"** → 구현했다(아래 Done). 호출 없음.

## Done

- **Q1 (b) 플레이 기록**: `PlayLog`(`Core/Bootstrap`, 순수 C#: 한 줄 = 시각·화면·상태, 파일 `play-<시작 시각>.log`, 실패하면 조용히 멈춤), `UIManager.ScreenShown`(화면이 `Open`된 뒤),
  `AppRoot`의 배선(저장 루트 옆 `Logs/`. Test는 임시 저장 루트 안. `Boot` 줄 + 화면마다 `phase= day= floor=<선 층>/<층 수> node= speed=`), `BattleScreen.PreferredSpeedPercent`,
  집계 `Tools/playlog.py`, `PlayLogTests`(EditMode 3). 문서: Architecture/01(폴더), 02(AppRoot), 10("플레이 기록"), 12(UIManager).
- **Q5 틱의 피로**: `BattleEngine.ApplyDamage`가 원인 `burn`이면 `FatigueOnHit`를 올리지 않는다(빈사의 피격 셈, 화상 틱으로 든 빈사의 피로는 그대로). 폭풍 틱은 그대로 올린다.
  `FatigueBreakdownTests.ABurnTick_IsAHitThatTiresNobody_AStormTickTires`. 문서: Design/02 §5, 04 §3(【확정】 사용자 결정), 00 "Slice B" 표, Architecture/08.
- **시뮬 전후**(시드 1, 세 정책 3,000회 + 여섯 파티 1,000회. 상세는 Design/08 §12): 화상 틱을 빼니 balanced 클리어 64.0 → 64.8%, 사망 0.35 → 0.34(그대로)인데
  **붕괴가 난 원정 39.1 → 6.4%**, 고통 0.39 → 0.06, 각성 0.13 → 0.02 — Q2의 띠(30~50%) 밖. 레버 스윕 열셋 뒤 **피격 `FatigueOnHit` 2, 처치 `FatigueOnKill` 5(J)**로 되돌렸다:
  64.3% / 0.35, 붕괴 34.3%, 고통 0.38·각성 0.12·쓰러짐 0.00, 생환자 피로 평균 34.8, 보스전 89.4% — 15단계의 자리. 데이터 `BalanceData.csv` 두 줄과 Generated. 대안 K(진입 5)는 §12.
  J는 피격이 많은 파티(paladin, spellblade, archmage, bishop)의 붕괴를 54 → 75%로 올린다(클리어 62.8 → 60.0%). 사후 검토 대상.
- Q2(지금 값을 띠로), Q3 (가), Q4·Q6 "플레이 뒤"는 Roadmap "16단계의 승인"과 Design/00 "Slice B"에 기록. 권장안으로 정한 세부는 Roadmap "사후 검토 대기"(16단계의 권장안 세부).
- Roadmap: "16단계의 승인", 16단계 절, 머리와 날짜 표. 이 Handoff.
- **Round 42 아이템 카드**(권장안 구현): `ItemTooltipView`(Views. 먹색 판·놋쇠 선·단계 띠·꼭지, 글은 `UiText.ItemTitle`·새 `ItemCard`·`MergeHint`, 높이는 레이아웃이 정함), `TooltipPlacement`(순수 함수: `Above`는 패널 위 8·열 가운데·화면 끝 12·꼭지, `Beside`는 보드 옆 10·패널 안), `PointerPress`(Input System의 `Mouse`·`Touchscreen` 누름을 `Poll`, Test는 `Simulate`. `F1.Runtime.asmdef`에 `Unity.InputSystem`),
  `PartySideView`(`_tooltip`·`_boardPanel`, 고른 뒤 `ShowTooltip`, `Update`의 누름에 `Hide`, `ClearSelection`·`Open`에 `Hide`), `BattleItemView.Button`·`SetClickable`, `BattleBoardView.ItemClicked`·`SetItemsClickable`, `BattleScreen`(`OnItemClicked`: 아군 왼쪽·적 오른쪽, 포션을 들면 칸의 raycast를 끔, 결과 창에 닫힘),
  Builder `UiPrefabSetup.BuildItemTooltip`(Kit)과 파티 쪽·전투의 배선, 전투 칸의 Button(소리 없음, 틴트 없음). Test: `TooltipPlacementTests`(EditMode 4), `UiFlowTests` 둘, `UiTestUtil.Click`이 누름을 흉내 내고 `PressTheBackground`, 스크린샷 `_40_battle_item_card`(`_08_map_item_selected`에도 카드).
  문서: Architecture/12 "툴팁: 아이템 카드"(조작 줄, Test, Deferred에서 뺌), 03(asmdef 참조), Roadmap(16단계, 사후 검토 대기 "Round 42의 세부"), Round 42 README "판정·구현·검증".
- **목업 6 → 안 A 구현**(사용자 "전투화면 아이템 툴팁에도 화살표" → "권장안 구현"): 전투 카드의 보드 쪽 옆면에 꼭지(누른 칸의 가운데 높이), 카드가 화면 끝에 밀려 자기 보드를 덮게 되면 보드의 반대쪽으로. `TooltipPlacement.Beside`(out `notchY`·`cardIsLeftOfCell`), `ItemTooltipView.Notch`(None·Bottom·Left·Right)·`NotchAt`,
  Builder `NotchClip`(아래·왼쪽·오른쪽 셋, 한 번에 하나). Test: `TooltipPlacementTests`(5: 반대쪽 넘김, 꼭지의 y), PlayMode 둘의 꼭지 검사. 문서: Architecture/12 "툴팁"(전투의 자리·꼭지), Roadmap 사후 검토 대기, README 42 "목업 6"·"판정 2".
- **Round 43 양초 불꽃**(사용자 "스프라이트 4~6장 애니메이션, 외곽선 너무 두꺼움"): 검토와 목업(MP4·정지·프레임 시트, 안 다섯. 권장 A3) → 사용자 **"안 C로, 외곽선은 권장안"** → `candle_flame`의 `Outline` 6 → 2로 원본에서 다시 맞춤(호출 없음). 코드·Prefab 변경 없음. 문서: Architecture/12·13, Design/10, Roadmap(사후 검토 대기 "Round 43의 세부"), README 43.

| Acceptance (Architecture/09 "Slice B") | 어떻게 확인하나 |
|---|---|
| 1, 3, 4, 5, 6 (15층 + 보스·야영지, 합치기와 금 상한, 피로의 쌓임과 표시, 100·200과 결정론, 이어하기) | EditMode·PlayMode Test(12~15단계) ✔ |
| 2. x1로 원정 하나가 15~30분 | **직접 플레이로 잰다**(플레이 기록) |
| 7. 시뮬이 긴 원정의 수치를 보고 | `Tools/Sim expedition` ✔ (Design/08 §12) |
| 8. 밸런스 상수가 코드에 없음, 체인 통과 | 체인(아래 Verification). 16단계 끝에 다시 |

## Open

### 사용자의 직접 플레이 (다음 걸음)

- 앱을 켜고 **x1**로 원정 하나를 끝까지(귀환 정산까지). 플레이 기록은 저절로 `~/Library/Application Support/funitup/F1/Logs/play-<시작 시각>.log`에 쌓인다(앱을 켤 때마다 파일 하나).
- 끝나면 `python3 Tools/playlog.py`(가장 새 파일)가 화면별 분·횟수·비율과 시간순 줄을 보인다. 그 출력과 아래 체크리스트의 메모를 세션에 준다.
  야영지 창과 인벤토리 팝업은 노드 맵의 시간에 든다.
- 체크리스트(미뤄 둔 "직접 플레이로 볼 것". 출처는 괄호):
  - 길이와 곡선: 15~30분인가, 어디서 늘어지나(노드 맵·보상·보드 정리), 정예·야영지가 긴장을 만드나, 보스전이 절정인가 (Architecture/09 목표 1). 배속은 어디서 쓰고 싶어지나.
  - 아이템의 성장: 합치기와 정비가 성장으로 읽히나, 일반(기본 단계) 보상이 하찮게 읽히나(Q6), 정비 중 인벤토리 팝업이 닫히고 옮기기가 쉬는 것이 불편한가 (b14 Open).
  - 피로: 칸 열 개가 읽히나, 피격 2로 피로가 너무 빨리 차나(붕괴는 원정의 34%), 붕괴·각성·쓰러짐의 연출(느림 0.8초·줌 1.2·먹 튐·빛살·머리 위의 글)이 심심하거나 과한가,
    야영지에서 쉬기와 정비가 고민되나 (Architecture/09 목표 3).
  - 연출의 손맛(a-art-restyle Open): 결정타 0.5초의 느림·어둠·줌과 한 전투에 여러 번일 때의 피로, 발밑 표시의 읽힘, 자세가 돌아올 때 튀는지, 쿨다운 빛에서 아이콘이 읽히는지, 촛불 빛과 폭풍의 꺼짐, 보스 1.5배.
  - 소리(a10-sound Open): 음악의 크기(효과음의 0.6배), x2·x4에서 소리가 몰리는지, 반복되는 피격이 거슬리는지, 로비 곡의 이음매.
  - 적 정보 숨김이 답답한가(Q4). 단계의 외곽선·별(Round 41)이 전투에서 읽히나.
  - 상점(17단계): 창과 타일이 읽히나, 값과 코인의 느낌(남는가, 모자라는가), 새로고침을 쓰고 싶어지나, 카드가 창 아래에 뜨는 것이 불편한가, 결과 창의 "지역 코인 +N"이 보이나. 포션이 상점에서만 나오는 것이 모자란가.
  - 전리품(18단계, Round 47부터 전투 화면에서): 이긴 뒤 띠(승리·전사자·코인·전리품 N)가 읽히나, 바닥의 드랍이 눈에 띄나(작은 아이콘, 빛, 이름 판), 드랍을 고르고 왼쪽 보드에 놓는 흐름이 자연스러운가, 우클릭 카드가 손에 익나(좌클릭이 카드를 열지 않는 것이 어색한가), [계속]으로 두고 가는 것이 분명한가, 드랍 하나(정예 둘)가 적게 느껴지나, 쥐 송곳니·치유의 부적이 물건으로 읽히나.

### 그 뒤: 맞추기와 마감

- 메모와 기록으로 조정 대상을 정한다 → 데이터 패치 → 시뮬(전후 같은 시드, 3,000회 + 여섯 파티 1,000회) → 수치 보고 → 확정된 것만 문서(Design/08 §12, 바뀐 규칙은 02·03·04 먼저)
  → 체인 → Roadmap·Handoff → Slice B 완료 보고. 커밋은 지시가 있을 때.
- 사후 검토 질문: 피격 2·처치 3(18단계의 자리. Design/08 §14) 그대로인가, 진입으로 옮기는가(E4 진입 4는 붕괴 28%), 보스전 92.7%(띠의 위 끝)를 내릴 것인가. Q3의 로비 보조 표기("기본 무기: 앞에서 2번째까지")는 목업으로.
- 짧으면 레버는 `DungeonData.Floors`·노드 폭·적 HP(전투 한 판의 길이). 전투 시간 합은 x1로 약 2.5분(시뮬 153초)뿐이라 15분은 조작 시간이 채운다.

### 그 밖의 대기

- **Round 46 오른쪽 열**(2026-10-08 사용자: "물약칸은 전투 위치로, 지도·상점·보상의 고르는 칸을 세로로 넓게, 설명·인벤 버튼 패널을 절반으로"): 목업 여섯과 비교 둘(`ArtPipeline/Archive/46-right-column/`). 권장 A(보드 판은 보드 아래만) + S1(상점 타일에 효과까지, 카드 없음) + L2(전리품을 상점 타일로). "아이템 선택하는 칸"은 오른쪽 위의 판으로 읽었다. 호출 없음.
  판정: 사용자 **"일단 지금 새로만든 화면 반영"** → 권장안 구현(README 46 "구현"·"검증", Architecture/12, Roadmap "Round 46의 권장안 세부"). 체인 OK(EditMode 751/751, PlayMode 77/86, 9 skipped), 스크린샷 `20261008-r46c` 9/9. 커밋 안 함.
  체인 중 한 번 `OneLap_…`이 기존 버그로 실패했다(맞지 않고 죽은 용병의 사망 문장이 `Potion ''`을 찾음, `BattleLogText.Source`). 따로 고칠 작업으로 남겼다. 직접 플레이의 체크리스트에 더한다: 높아진 지도(일곱 층 반)가 길 고르기에 도움이 되나, 절반 패널의 두 줄이 모자라지 않나, 상점·전리품 타일의 사실이 읽히나.
- Round 42 목업 7(사용자 "안 3 방향으로 재검토": 단계별 명패): 비교 목업 셋을 보인 뒤 사용자 **"지금 유지하자"** → 안 1 그대로. 기록은 README 42 "목업 7"·"판정 3".
- 목업 7의 명패 스타일은 **유니크 아이템**(사용자 2026-10-07: 금 아이템 조합으로 만들고 전투당 하나만 소유)의 툴팁에 쓰기로 하고 보관했다: Design/02 §4 【검토】, Architecture/09 Deferred·12 "툴팁" 보관, README 42 "보관". 규칙은 Slice C에서 정한다.
- **Round 44 상점 노드와 지역 코인**: 2026-10-07 사용자 **"권장안 구현"** → 17단계를 열어 구현했다(`b17-shop.md`). 판정할 것 여섯과 권장안, 구현의 기록은 `ArtPipeline/Archive/44-shop-node/README.md`.
- Round 42의 사후 검토: 카드가 열린 동안 설명 줄에 같은 사실이 두 곳에 있는 것(설명 줄을 안내만으로 줄일지), 전투의 카드 클릭 소리, 카드가 용병 그림을 덮는 느낌 — 직접 플레이에서 본다.
- **결정타 분리(R2·R3)**(a10 Open): 연출을 바꾸는 라운드에서 한다. 16단계에서 연출을 바꾸지 않으면 하지 않는다(권장).
- Round 37의 붕괴·각성 초상은 B안에 자리가 없어 **보류** 그대로.
- Unity가 만든 추적되지 않은 파일 넷은 2026-10-07 세션 정리에서 권장안대로 정리했다(Addressables Profile 설정과 `ScriptableBuildPipeline.json`은 저장소에, `OSX/`·`OSX.meta`는 `.gitignore`. 사후 검토).
- Roadmap "사후 검토 대기" 표의 항목은 플레이 중 눈에 띄면 그때 올린다.

## Verification

- 체인 1(`20261007-032022`, `setup sim editmode`, J 전): setup OK(새 .cs 둘의 .meta 생성), sim OK, EditMode **727/727**(`PlayLogTests` 3, `ABurnTick_IsAHitThatTiresNobody_AStormTickTires` 포함).
- 체인 2(`20261007-032913`, `sim editmode playmode`, J 데이터): sim OK, EditMode **727/727**, PlayMode **73/73**(9 skipped). `Assets/InitTestScene*` 없음.
- 체인 3(Round 42. `20261007-090545`, `setup sim editmode`): setup OK(Prefab 셋과 Stamp 다시, 새 .cs 넷의 .meta), sim OK, EditMode 730/731 — 실패 하나는 `TooltipPlacementTests`의 기대값 오류(화면 안의 칸은 꼭지 clamp에 닿지 않음) → Test를 고침.
  체인 4(`20261007-090718`, `editmode playmode`): EditMode **731/731**(`TooltipPlacementTests` 4 포함), PlayMode **75/75**(9 skipped. 새 둘: 파티 쪽 카드, 전투 카드). 그 전 체인(`20261007-090513`)은 `BattleScreen.Tooltip`이 빠져 컴파일 오류 → 더함.
- 스크린샷 `20261007-r42`에서 적의 카드에 피로 줄이 보여 뺐다(`withFatigue`). 체인 5(`20261007-092254`, `editmode playmode`): EditMode 731/731, PlayMode 74/75 — `Battle_AnEnemyTheStormKills_IsNoKillMoment`가 긴 전투의 **붕괴**(피격 피로 2) 느림을 결정타로 읽음 → 붕괴의 순간이면 느림 검사를 건너뛰게 고침(처음에는 같은 모양의 x4 Test에 들어가 다시 옮김). 체인 6(`20261007-094933`, playmode): **75/75**(9 skipped). `Assets/InitTestScene*` 없음.
- 스크린샷 `20261007-r42c`: 9/9, PNG 65(`_40_battle_item_card` 둘이 늘었다). `ArtPipeline/Archive/42-item-tooltip/game/`에 `ko_08`·`ko_40`의 구현 장면. Render 설정은 그대로. 죽인 실행이 남긴 `Assets/InitTestScene*`은 지웠다.
- 목업 6 안 A(옆 꼭지) 구현의 체인 7(`20261007-133735`, `setup sim editmode`): setup OK(Prefab 셋 다시), sim OK, EditMode 731/732 — 실패는 새 Test의 기대값 오류(높이 900의 카드에서 꼭지 y는 clamp에 닿지 않음) → 고침. 체인 8(`20261007-133822`, `editmode playmode`): EditMode **732/732**, PlayMode **75/75**(9 skipped).
  스크린샷 `20261007-r42d`: 9/9, PNG 65. `game/ko_40_battle_item_card_implemented.png`를 옆 꼭지 장면으로 바꿨다. `Assets/InitTestScene*` 없음, Render 설정 그대로.
- Round 43 안 C(불꽃 외곽선 2)의 체인: setup·sim·editmode OK(EditMode 732/732), playmode(`20261007-152844`) **75/75**. 그림 한 장만 바뀌어 Test는 그대로다. 스크린샷 `20261007-r43`은 아래.
  스크린샷 `20261007-r43`: 9/9, PNG 65. `ArtPipeline/Archive/43-candle-flame/game/`에 양초 전후(4배)와 첫 전투 장면. Render 설정 그대로.
- 시뮬(출고 → Q5 → J, balanced 3,000회, 시드 1): 클리어 64.0 / 64.8 / 64.3%, 사망 0.35 / 0.34 / 0.35, 붕괴가 난 원정 39.1 / 6.4 / 34.3%, 쓰러짐 0.01 / 0.00 / 0.00, 보스전 89.9 / 90.1 / 89.4%.
  여섯 파티(J): 70.0 / 60.0 / 41.3 / 23.1 / 2.1 / 0.0%(15단계 70.3 / 62.7 / 42.3 / 23.3 / 1.6 / 0.0). 전체 표와 스윕은 Design/08 §12.
- `Tools/playlog.py`를 견본 로그로 돌려 집계를 확인했다. 플레이 기록의 배선은 PlayMode의 모든 Boot가 지난다(임시 루트 안의 `Logs/`).
- `git diff --check` 깨끗. 데이터 변경은 `BalanceData.csv`의 두 줄(`FatigueOnHit` 2, `FatigueOnKill` 5)과 Generated뿐.

## Next Action (제안)

- 사용자: **직접 플레이**(x1, 원정 하나를 끝까지). 끝나면 `python3 Tools/playlog.py`의 출력과 체크리스트의 메모를 세션에.
- 세션: 플레이 기록과 메모가 오면 "맞추기와 마감"으로(17·18단계의 수치도 함께. 지금 데이터로 시뮬 전후). 18단계는 2026-10-08 세션 정리에서 다섯 커밋으로 올렸고, 같은 날 Round 46(오른쪽 열)·47(전투 화면의 전리품, 우클릭 카드)도 다섯 커밋으로 올렸다(`b18-loot.md`, README 46·47).
