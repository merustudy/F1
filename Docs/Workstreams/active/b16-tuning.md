# Slice B 16단계: 맞추기

Snapshot: 2026-10-06

## Goal

Slice B의 Acceptance 1~8(Architecture/09 "Slice B")이 참이다: 원정 하나가 x1로 15~30분이고 그 안에 긴장의 곡선이 있으며(정예, 야영지, 쌓이는 피로),
시뮬 수치가 받아들일 만한 띠에 있고, 직접 플레이로 재미가 확인된다. 조정은 시뮬 우선 워크플로(Design/09 §4)로 하고, 확정된 것만 Design/08 §12와 해당 절에 적는다.
끝나면 Slice B 완료다(CLAUDE.md §9, Roadmap).

## State

- 2026-10-06 시작(사용자 "16단계(맞추기) 시작 … 설계를 먼저 보여줘"). 필수 문서를 읽고 **기준값**을 쟀다(아래 Verification). 세 걸음과 결정 질문 여섯의 설계를 제안했고
  **사용자 승인 대기**다. 코드·데이터·기획 문서는 바꾸지 않았다.
- 화면은 `~/Library/Caches/F1/screenshots/20261006-b16a`(9/9, PNG 63)로 뽑아 노드 맵·전투·보상을 보였다.
- 2026-10-07 세션 정리에서 Round 41과 16단계의 시작을 주제별 다섯 커밋(규칙과 데이터 / 화면 / Architecture와 기획 / 그림 / Roadmap과 Handoff)으로 올리고 `origin/feature/slice-a`에 푸시했다.
- 사용자가 설계 승인에 앞서 **UI 개선을 먼저** 하기로 했다("아이템 칸의 등급 점수 동그라미 삭제, 조합으로 오른 단계의 표기를 더 멋있게. 유사 게임의 장식을 알아보고 안을 제안", "검토 후 보고, 승인 후 구현").
  → **Round 41**(`ArtPipeline/Archive/41-tier-marks/README.md`): 레퍼런스(The Bazaar 틀 = 단계, TFT 별, Backpack Battles 레시피)와 목업 세 안(금속 테 / 별 표 / 둘 다, 권장 1)을 보고했다. 사용자가 "테 말고 아이템의 추가 외곽선 색으로, 등급이 커질수록 굵게"를 제안해 **목업 2**(외곽선 2·3·4 / 2·4·6 / +빛)를 냈다. 2026-10-07 사용자: 색이 어긋난다(특히 다이아) → **목업 3**(색 팔레트 P1 금속 셋 / P2 금속 둘 + 깊은 청록, 권장 / P3 구리·은·금 = 이름도 바꿈). 정한 것: 틀은 2안, 외곽선은 A안으로 두고 색을 먼저 확정한 뒤 그 색의 목업으로 고른다. 은은 짙게, 전투의 어둠에 가려지는 것은 그대로. 색은 **P3**로 판정(기본은 표시 없음, 오른 셋이 동·은·금. 구현 때 `ItemTier`와 데이터 키·문구·문서의 이름을 모두 바꾼다) → **목업 4**(P3 색의 테두리 2안 / 외곽선 A안, 권장 A) → 사용자 "외곽선 + 왼쪽아래 별 표시" → **목업 5**(외곽선 + 별, 비교 포함) → 판정 **"이 안으로 결정, 권장대로, 구현"**(기본 단계의 이름은 "일반"). **구현했다**(아래 Done). 호출 없음.

## Done

- Roadmap 16단계를 "진행 중"으로. 이 Handoff.
- **Round 41 구현**(2026-10-07. 상세는 `ArtPipeline/Archive/41-tier-marks/README.md` "구현"):
  - 단계의 이름 **일반·동·은·금**: `ItemTier` Common·Bronze·Silver·Gold(값의 순서 그대로), `BalanceData`의 `TierBronzePercent`·`TierSilverPercent`·`TierGoldPercent`, `DungeonData`의 `BronzeFloor`·`SilverFloor`·`GoldFloor`(CSV 머리글, 변환기, Generated JSON 다시),
    시뮬 보고의 단계 이름, 문구(`Item.Common~Gold`, `Item.TitlePlain`, `Item.MergeHintPlain`; `Item.Diamond` 삭제), `UiText.ItemTitle`(일반은 단계를 적지 않음)·`MergeHint`. 저장 `run.json` **버전 7**(`From6To7`: 6의 Bronze·Silver·Gold·Diamond → Common·Bronze·Silver·Gold), `From4To5`는 5의 이름 "Bronze"를 그대로 둔다.
  - 색 P3: `UiPalette.TierBronze` #A8683A·`TierSilver` #9AA7B8·`TierGold` #D4A232(`TierMark`), 글 `TierCommonText`·`TierBronzeText`·`TierSilverText`·`TierGoldText`(`TierText`), `Virtue`는 새 금의 글 색.
  - 등급 배지 삭제(`KitBadge`, `GradeBadge*`, `ItemSlotView.Grade`·`_badge`·`_grade`).
  - 외곽선: `Assets/@Shaders/Silhouette.shader`(UI/Default에서 텍스처 색을 뺀 것), 재질 `Assets/@Prefabs/UI/Silhouette.mat`(`UiPrefabSetup.SilhouetteMaterial`이 없으면 만듦), `SilhouetteOutline`(BaseMeshEffect, 16 + 8 복제), `TierStyle`(굵기 2·3·4, 별 1·2·3, 표의 치수), 아이콘 뒤의 실루엣 Image(`Kit.BuildOutline`. 전투에서는 충전의 금빛 다음·아이콘 앞).
  - 별 표: 조각 `tier_tag`(64x40, Border 20)·`star`(24x24, `draw_pieces.py`), `Kit.BuildTierTag`(파티 쪽·전투 공통, 전투에서는 어둠 위), `tier_rim` 삭제. 합칠 칸은 다음 단계의 외곽선과 별, 글은 표에서 6 띄움.
  - View: `ItemSlotView`·`BattleItemView`의 `TierShown`·`Stars`·`OutlineColor`(+ `OutlineThickness`), `RewardOptionView.Stripe`는 `TierMark`. Test: `UiFlowTests`의 단계·합치기·그림 Test를 외곽선과 별로, `TierStyleTests`(EditMode 3), `RunSaveMapperTests.Migrate_From6_RenamesTheTiers`.
  - 문서: Design/00("Slice B" 표에 결정 한 줄)·02 §4·03 §1·§5·08 §10·§11(새 이름)·10 §5, Architecture/01(`@Shaders`, 재질)·07(버전 7)·08·12("단계", 배지 삭제, Test)·13(조각). Roadmap 16단계와 사후 검토 대기 표.

## Open

### 설계 제안 (승인 대기)

세 걸음이다. 2는 사용자가, 1·3은 세션이 한다.

1. **기준 잡기**(했다): 출고 데이터의 시뮬 기준값(Verification). Acceptance 1·3~8은 이미 Test·시뮬·체인이 덮고(아래 표), 2(15~30분)와 "재미"만 직접 플레이가 답한다.
2. **직접 플레이**(사용자, x1로 원정 하나를 끝까지): 시간을 재고(Q1) 체크리스트(아래)를 보며 메모를 남긴다. 전투 시간 합은 x1로 약 2.5분(시뮬 153초)뿐이라
   15분은 노드 고르기·보상·보드 정리의 조작 시간이 채운다. 짧으면 레버는 `DungeonData.Floors`·노드 폭·적 HP(전투 한 판의 길이)다.
3. **맞추기와 마감**: 플레이 메모와 Q2~Q6의 답으로 조정 대상을 정한다 → 데이터 패치 → 시뮬(전후 같은 시드, 3,000회 + 여섯 파티 1,000회) → 수치 보고 → 확정된 것만 문서
   (Design/08 §12 "16단계", 바뀐 규칙은 Design/02·03·04 먼저) → 체인 → Roadmap·Handoff → Slice B 완료 보고. 커밋은 지시가 있을 때.

| Acceptance (Architecture/09 "Slice B") | 어떻게 확인하나 |
|---|---|
| 1. 15층 + 보스, 정예·야영지, 쉬기/정비 | PlayMode `NodeMap_*`, `Camp_*`(13·14단계) ✔ |
| 2. x1로 원정 하나가 15~30분 | **직접 플레이로 잰다**(Q1) |
| 3. 합치기, 금 상한(이름 변경 전 다이아), 깊은 층·정예의 단계 | EditMode 14단계 Test, PlayMode `Reward_*`·`PartySide_*` ✔ |
| 4. 피로가 쌓이고 로비에서 내려가며 원정 사이에 남음, 장비 피로의 표시 | EditMode 12단계 Test, PlayMode `PartySide_EquipmentShowsItsFatigue_*` ✔ |
| 5. 100 붕괴 판정, 200 쓰러짐, 결정론 | EditMode `FatigueBreakdownTests`, 결정론 Test ✔ |
| 6. 어느 시점에 끝내도 이어짐(피로·상태·단계·지도) | `UiSaveTests`, `RunSaveMapperTests`(저장 버전 6) ✔ |
| 7. 시뮬이 긴 원정의 수치를 보고 | `Tools/Sim expedition`(오늘 돌림) ✔ |
| 8. 밸런스 상수가 코드에 없음, 체인 통과 | 체인 11 통과(15단계). 16단계 끝에 다시 |

결정 질문과 권장안:

| # | 질문 | 선택지 | 권장 |
|---|---|---|---|
| Q1 | 원정 시간을 어떻게 재나 | (a) 손 스톱워치 (b) **플레이 기록**: 화면이 열릴 때마다 한 줄(시각, 화면, 층, 단계)을 저장소 밖 `<persistentDataPath>/Logs/`에 쓰고 세션이 단계별 시간을 집계 (c) 정산 화면에 원정 시간 표시(출발 시각을 run.json에 저장해야 함) | **(b)**. 총 시간만이 아니라 전투·노드 맵·보상·야영지에 쓴 시간이 갈라져 보여 무엇을 늘릴지 정할 수 있다. 코드는 UI 한 곳과 작은 helper, Test 하나. 첫 플레이를 바로 하려면 (a)로 시작해도 된다 |
| Q2 | 원정 하나의 목표 수치 띠(기획에 【확정】 목표가 없다. 옛 "100일 런 82%/75%"는 런 전체의 것) | 띠를 정하거나, 지금 값을 기준으로 두거나 | **지금 값을 Slice B의 기준으로**: balanced 클리어 60~70%·사망 0.3~0.4/원정·전멸 ≈ 0, 붕괴가 난 원정 30~50%, 쓰러짐 ≤ 0.02/원정, 보스전 승리 85~92%. 플레이의 느낌이 다를 때만 레버를 쓴다 |
| Q3 | 조합 절벽(여섯 가운데 넷: 70 → 63 → 41 → 23 → 1.5 → 0%. 뿌리는 뒤 열에서 무기를 쓰는 직업이 둘뿐이고 2열의 기둥이 마검사의 화상인 것. Design/08 §7, 2026-10-01 "나머지는 플레이테스트 뒤") | (가) Slice B에서는 두고 Slice C(고용·성장·전직)에서 푼다 (나) 뒤 열용 보상 아이템을 더 둔다(새 아이템·아이콘) (다) 근접 무기의 자리를 넓힌다(예: `greataxe` `front:1` → `front:2`. 시뮬 전후) (라) 지팡이 직업을 더 둔다(그림 비용) | **(가)**. 절벽은 "누가 죽었는가"에 무게를 주는 영구 손실의 결과이고, 로스터를 바꾸는 답은 Slice C의 영역이다. 다만 로비는 지금 네 열 버튼이 모두 켜져 어느 열에서 기본 무기가 안 나가는지 알 길이 없다. 작은 보조(직업 줄에 "기본 무기: 앞에서 2번째까지")를 플레이 뒤 목업으로 정하자고 제안 |
| Q4 | 적 정보 숨김(지금 전부 숨김. Design/03 §1: 수만·종류만·싸워 본 무리만은 플레이테스트 뒤) | 그대로 / 수만 / 종류만 / 싸워 본 무리만 | **플레이 뒤 판단**, 기본은 그대로 |
| Q5 | 화상·폭풍 틱도 피격으로 피로를 올림(Design/02 §5의 정의. 15단계 Open) | 그대로 / 틱은 피로 없음(규칙 변경: Design/02 §5·04 §3 먼저, 시뮬 전후) | **플레이 뒤 판단**. 시뮬에서 피격 1이 가장 센 레버였고 틱이 그 일부다. 피로가 너무 빨리 찬다고 느끼면 먼저 뺄 후보 |
| Q6 | 동 보상이 작게 읽힘: 등급 8이 층과 무관해 보상 카드가 "자신의 HP 4 회복", "맨 앞 적에게 피해 3"으로 읽힌다(기본 무기는 피해 11) | 그대로(단계가 힘을 준다) / `ItemGradeBase` 올림(시뮬 전후) / 설명에 단계가 오르면 커진다는 안내 | **플레이 뒤 판단**. 수치는 시뮬이 괜찮다고 하지만, 보상이 하찮게 읽히면 "아이템의 성장"(Slice B 목표 2)이 안 읽힌다 |

플레이 체크리스트(그동안 미뤄 둔 "직접 플레이로 볼 것"을 모았다. 출처는 괄호):

- 길이와 곡선: 15~30분인가, 어디서 늘어지나(노드 맵·보상·보드 정리), 정예·야영지가 긴장을 만드나, 보스전이 절정인가 (Architecture/09 목표 1).
- 아이템의 성장: 합치기와 정비가 성장으로 읽히나, 동 보상이 하찮게 읽히나(Q6), 정비 중 인벤토리 팝업이 닫히고 옮기기가 쉬는 것이 불편한가 (b14 Open).
- 피로: 칸 열 개가 읽히나, 붕괴·각성·쓰러짐의 연출(결의 시험 B안: 느림 0.8초·줌 1.2·먹 튐·빛살·머리 위의 글)이 심심하거나 과한가, 틱으로 너무 빨리 차나(Q5), 야영지에서 쉬기와 정비가 고민되나 (Architecture/09 목표 3).
- 연출의 손맛(a-art-restyle Open): 결정타 0.5초의 느림·어둠·줌과 한 전투에 여러 번일 때의 피로, 발밑 표시의 읽힘, 자세가 돌아올 때 튀는지, 쿨다운 빛에서 아이콘이 읽히는지, 촛불 빛과 폭풍의 꺼짐, 보스 1.5배.
- 소리(a10-sound Open): 음악의 크기(효과음의 0.6배), x2·x4에서 소리가 몰리는지, 반복되는 피격이 거슬리는지, 로비 곡의 이음매.
- 적 정보 숨김이 답답한가(Q4). 배속은 어디서 쓰게 되나(x1만으로 15분이 되나).

### 그 밖의 대기

- **결정타 분리(R2·R3)**(a10 Open): 결정타·쓰러짐의 연출을 바꾸는 라운드에서 한다. 16단계에서 연출을 바꾸지 않으면 하지 않는다(권장).
- Round 37의 붕괴·각성 초상은 B안에 자리가 없어 **보류** 그대로.
- Unity가 만든 추적되지 않은 파일 넷(`Assets/AddressableAssetsData/OSX.meta`, `ProfileDataSourceSettings.asset`(+`.meta`), `ProjectSettings/ScriptableBuildPipeline.json`):
  스크린샷·체인이 돌 때마다 다시 생긴다. 권장은 Addressables의 Profile 설정과 `ScriptableBuildPipeline.json`은 저장소에 넣고(프로젝트 설정), 빌드 산출물 폴더의 `OSX.meta`는 `.gitignore`에 더하는 것. 세션 정리 때 정한다.
- Roadmap "사후 검토 대기" 표의 항목은 플레이 중 눈에 띄면 그때 올린다.

## Verification

- 시뮬 기준값(출고 데이터, `dotnet run --project Tools/Sim -c Release -- expedition`, 시드 1, 기본 파티 3,000회):
  balanced **64.0% / 0.35**(후퇴 35.9%, 전멸 0.1%), safe 56.6% / 0.32, none 81.1% / 1.33. balanced의 세부: 이긴 전투 12.17, 보드 9.93개(동 5.04·은 1.81·금 1.58·다이아 1.50), 합치기 3.41·정비 0.89, 정예 1.13·야영지 1.59,
  붕괴가 난 원정 39.1%(고통 0.39·각성 0.13·쓰러짐 0.01/원정), 생환자 피로 평균 34.5·최고 199, 100 이상인 채 4.4%·고통인 채 4.2%, 보스전 승리 89.9%, 전투 시간 x1 153.3초.
  Design/08 §11(64.6% / 0.35)과 같은 자리다(차이는 Round 39의 마검사 검). 여섯 파티(balanced, 1,000회): 69.6 / 62.8 / 41.3 / 23.3 / 1.5 / 0.0% — §11과 같은 자리.
- 스크린샷 `20261006-b16a`: 9/9, PNG 63. Render 설정은 바뀌지 않았다(`git status`는 시작 때의 추적되지 않은 파일 넷뿐).
- **Round 41의 체인**(2026-10-07): 체인 1(`20261007-013050`): setup OK(Prefab 셋 다시, 실루엣 재질 생성, 문구 표, 조각 둘의 Import), sim OK(변환·검증), EditMode 717/723 — 실패 여섯은 모두 `RunSaveMapper`의
  포션 보상 검증이 옛 이름(`ItemTier.Bronze` = 기본)으로 남아 있던 것(이름 바꾸기에서 빠진 파일 하나) → `Common`으로 고침. PlayMode 73/73(9 skipped. 고치기 전 코드지만 UI는 같다).
  체인 2(`20261007-014108`, editmode만): **723/723**(`TierStyleTests` 3, `Migrate_From6_RenamesTheTiers` 포함). 스크린샷 `20261007-r41`: 9/9, PNG 63 — `_35_map_tiers`에 은 ★★·금 ★★★의 외곽선과 "합치기 → 동"(동 ★, 동 외곽선),
  `_34_camp_mend`에 "롱소드 일반 → 동", `_07_reward`에 "불씨 플라스크 · 등급 8"(일반은 단계를 적지 않음). 보스전의 단계는 1열이 30초 전에 쓰러져 안 보여 스크린샷 Test가 맨 뒤 구성원의 무기를 동으로 세우게 고쳤다.
  스크린샷 `20261007-r41b`: 9/9, PNG 63 — `_09_boss_battle`에 미라의 지팡이 칸이 충전된 부분에서 구리 외곽선, 어둠 위에 ★ 표(`ArtPipeline/Archive/41-tier-marks/game/ko_09_boss_battle_implemented.png`). 그 밖의 장면은 전과 같다.
  체인 4(`20261007-014903`, playmode만, 고친 코드로): **73/73**(9 skipped). 체인 5(`20261007-015917`, setup·editmode: Builder 주석을 고쳐 Stamp를 다시 맞춤): setup OK, **723/723**. `Assets/InitTestScene*` 없음.
- 시뮬(출고 데이터, 시드 1, 3,000회): 64.0% / 0.35 — 이름만 바뀌었고 수치는 그대로(보고의 단계 이름 common·bronze·silver·gold).
- `git diff --check` 깨끗.

## Next Action (제안)

- Round 41은 구현과 검증이 끝났다. 세션 정리 때 주제별로 커밋한다(이름 바꾸기와 저장 7 / 외곽선·별 표와 배지 삭제 / 문서·목업).
- 그다음 16단계 설계(위 "설계 제안")의 승인과 Q1~Q6의 답을 받는다. (b)면 플레이 기록을 넣고 체인을 돌린 뒤 플레이를 부탁한다. 플레이 메모가 오면 3(맞추기와 마감)으로.
