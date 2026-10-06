# Slice B 15단계: 피로의 판정

Snapshot: 2026-10-06

## Goal

피로가 전투 안에서도 쌓이고(피격, 빈사, 동료의 빈사와 죽음; 처치는 덜어 냄), 100에 닿으면 붕괴 판정(고통 또는 각성), 200이면 쓰러진다. 고통·각성은 데이터가 정하는 상태이고
전투 수치(쿨다운, 받는 회복, 빈사의 사망 확률)를 바꾼다. 파티 쪽·전투·로비·정산이 피로와 상태를 보인다(목업). 시뮬로 출발값을 잡고 절벽을 점검한다.
범위와 순서: Architecture/09 "Slice B", 규칙: Design/04 §3.

## State

- 규칙·데이터(`FatigueStateData.csv`)·저장(버전 6)·시뮬, 그리고 화면까지 됐다. 화면은 목업 Round 36(`ArtPipeline/Archive/36-fatigue-states/README.md`)의 판정
  **"1. C 2. B 3. 4. 권장"**(피로는 HP 막대 아래의 칸 열 개, 붕괴·각성은 무대를 가로지르는 띠, 로비·정산은 목업대로, 상태의 효과는 발밑의 상태 이름을 누르면 설명 줄에)대로다.
- 연출은 Round 38의 판정(B안 결의 시험)대로 구현됐다(아래 Open). 띠는 뺐고 Round 37의 초상은 보류. Round 39로 마검사의 검이 기사의 롱소드로 통일됐다.
- **Round 40**(`ArtPipeline/Archive/40-state-poses/README.md`): 사용자 "나머지 용병 다섯의 붕괴 각성 자세 만들자(캐릭터 일관성 유지_얼굴 크기 등)" → 열 자세를 그려 머리 크기로 맞춘 리뷰 시트를 냈다. 판정 "모두 확정 구현" → `Assets/@Art/Pose/Job/<job>_broken|resolute.png`에 배선했다(여섯 직업 모두). 체인 11: `20261006-220529`: setup·sim OK, EditMode 719/719, PlayMode 73/73(9 skipped). `ArtSetup`이 열 파일을 `F1-Art`의 Entry로 등록하고 Import 정책 `Pose`를 적용했다.
- 2026-10-06 세션 정리에서 12~15단계와 Round 32~40을 주제별로 커밋하고 푸시했다. 브랜치 `feature/slice-a`. 단계가 끝나 `completed/`로 옮겼다.

## Done

- 데이터: 새 정의 `FatigueStateData`(종류 고통·각성, 쿨다운%·받는 회복%·빈사 사망 확률%p, 이름·설명; 검증 범위와 "무언가는 바꿔야"), `StaticDataFiles.FatigueState`(주소 `data/app/fatigue-state`),
  `StaticData.FatigueStates`·`FatigueStatesOf`(검증: 고통과 각성이 하나씩은), 변환기·로더, `FatigueStateData.csv`(공포·절망·무모, 집중·강인).
  `BalanceData`: `FatigueOnHit`·`FatigueOnDog`·`FatigueOnAllyDog`·`FatigueOnAllyDeath`·`FatigueOnKill`·`VirtueChancePercent`·`VirtueFatigue`(검증: 붕괴 문턱 아래), `FatigueBattleEntry` 3 → 2.
- Domain: `RngStream.Fatigue`, `BattleUnitSetup.Fatigue`·`FatigueState`, `BattleSetup.FatigueStates`, `BattleUnit.Fatigue`·`State`, 이벤트 `FatigueChanged`(원인은 `BattleEvent.Fatigue*` 상수)·`BrokeDown`·`Collapsed`·`FatigueStateEnded`,
  엔진의 `ChangeFatigue`·`ResolveFatigue`·`BreakDown`·`Collapse`와 상태의 효과(`CooldownOf`, 받는 회복, 사망 확률), 시작할 때의 판정. `ExpeditionMember.StateId`, `PartyMember.AfflictionId`, `MercenaryState.AfflictionId`,
  `FatigueRules.AfflictionEnds`·`StateAfter`·`Scaled`, `ExpeditionRules`(생성·Setup·되쓰기·야영지 풀림), `RunRules`(출발·정산·쉬는 날 풀림), `SettlementReport.SurvivorStates`.
- 저장: `run.json` 버전 6(`MercenaryRecord.Affliction`, `MemberRecord.State`), `From5To6`, 검증(`RequireState`).
- 시뮬: 전투·원정 보고에 붕괴(고통·각성)·쓰러짐·붕괴가 난 원정·문턱 이상의 생환자·고통인 채 돌아온 생환자. 정책은 문턱 이상인 용병이 있으면 야영지에서 쉰다.
- 화면(Round 36의 판정대로. 세부는 그 README "구현"과 Architecture/12 "피로도와 붕괴의 상태"·"연출"):
  - 발밑 표시(`UiPrefabSetup.Kit.BuildMarks`): HP 막대 아래 피로 칸 열 개(높이 4, 사이 2), 상태 줄 32, 높이 46 → 50. 채움은 셋이 같이 쓰는 `FatiguePips.Show`(로비도 이것으로).
  - 파티 쪽(`PartyColumnView`·`PartySideView`): 칸과 "직업 · 상태"(`UiText.JobLine`), 상태 줄의 투명 버튼 → 설명 줄에 "공포: 아이템이 25% 느리게 돈다"(`UiText.FatigueStateDetail`).
  - 전투(`BattleUnitView`): 아군만 칸, 상태 줄 끝에 상태 이름(`UnitFatigueState`). `BattleFxLayer.Banner`: 무대의 막(0.7)과 띠(310, 84, 선 2, 글 40, 상태의 색, 그림 자리 `BannerArt` 64 비움), 0.15·1.2·0.3초(배속으로 나눔. `BattlePresenter`에 `pace`).
    `BattlePresenter`: `BrokeDown`(띠, 유닛 위의 상태 이름, 소리는 고통 = 빈사·각성 = 버텼다), `Collapsed`(띠만). `BattleLogText`: 붕괴·각성·쓰러짐·풀림의 줄, 피로의 변화는 빈사·동료 빈사·동료 사망·각성만.
  - 로비(`RosterEntryView`·`UiPrefabSetup.Lobby`): "피로도 128/200 · 공포", 패시브 줄 오른쪽 끝에 효과(패시브 글 590 → 380). 정산: "로언 128 (공포)".
  - 색 `UiPalette.Virtue`(= `TierGoldText`)·`FatigueState(kind)`, 문구 16개(`Lobby.FatigueState`, `Board.JobState`·`StateDetail`, `Settle.SurvivorState`, `Fx.BrokeDown`·`Virtue`·`Collapsed`, `Log.*` 8개).
- Test: EditMode `FatigueBreakdownTests`(16개 + 정산 보고의 상태), `RunSaveMapperTests`, 변환기 Test, `RunRulesTests`(`SurvivorStates`), TestData·TestCsv.
  PlayMode `PartySide_FatigueShowsAsPipsUnderTheHpBar_AndAStateIsNamedAndExplainedOnClick`, `Battle_PartyUnitsShowFatiguePips_AndABreakdownIsABannerAcrossTheStage`(`UiTestUtil.ReachABreakdownInTheFirstBattle`),
  `Lobby_AMercenaryThatCameHomeAfflicted_IsNamedWithItsStateAndWhatItDoes`. 스크린샷 `_36_map_state`, `_37_lobby_affliction`, `ko_38_battle_breakdown`.
- 그림: Round 37 — 발키리의 붕괴·각성 초상(`output/character/valkyrie_afflicted.raw.png`, 다시 그린 `valkyrie_afflicted2.raw.png`, `valkyrie_virtue.raw.png`. 호출 3회). `gen_image.py --tail`(시험용 꼬리 문장). 리뷰 시트 `review-busts.png`·`review-game.png`.
  Round 38 — 효과 조각 `output/ui_piece/fx38_ink_burst.raw.png`·`fx38_light_burst.raw.png`·`fx38_ink_burst2.raw.png`(3회), 상태의 자세 `output/hit/valkyrie_broken.raw.png`·`valkyrie_resolute.raw.png`(2회), 목업 `mock_breakdown_fx.py` → `mock-steps.png`·`mock-steps2.png`·`review-poses.png`, `mock-breakdown.mp4`·`mock-breakdown2.mp4`(Git에 올리지 않음). 누계 약 $5.63 / $10.
- 문서: Design/00 "Slice B", 04 §3(판정의 세부, 출발값, 상태 다섯, 쓰러짐), 07. Architecture/07(버전 6), 08(전투 안의 피로), 12("피로도와 붕괴의 상태", "파티 쪽", "전투 화면", "연출", Test), 13(`--tail`), 14(소리의 자리). 목업 Round 36(판정과 구현), Round 37.

## Open

- **Round 38 구현됨**(판정 3 "1~4. 구현 5. 대기"): 붕괴·쓰러짐의 순간은 결정타의 틀(`BattleScreen.StageMoment`: 0.8초 25%, 어둠 0.87, 줌 1.2, 0.25초에 걷힘), 상태의 자세(`BattleUnitView.HoldPose`·`Shiver`, 발키리의 `valkyrie_broken`·`_resolute`),
  효과 조각(`UiArt.InkBurst`·`LightBurst`)과 빛(`BattleFxLayer.Burst`), 머리 위의 글(`Word`). 띠(`Banner`)와 `Fx.BrokeDown`·`Virtue`는 뺐다. 상태 자세는 직업마다 따로 그리는 선택 그림: `JobData.BrokenPose`·`ResolutePose`, `ArtSetup.Files`(있는 파일만), `ExpeditionArt.OptionalAddresses`, `ResourceManager.ExistsAsync`.
- 초상(Round 37)은 B안에 자리가 없어 보류. 나머지 용병 다섯의 상태 자세는 **Round 40**에서 그렸다(아래).
- **Round 39**(사용자 "마검사 무기가 빨간색이라 너무 강해 보여 … 기사와 같은 검으로 통일", "전투 준비 자세만 먼저, 승인 뒤 나머지"): 데이터는 바꿨다(`JobData` 마검사 `longsword` 12, `sword` 아이템·아이콘·로스터 줄 삭제, Design/02·07). 전투 준비 자세를 롱소드로 다시 그렸다(후보 넷: 1차 가는 날, 2차 두꺼우나 긴 날, 3차 짧은 날, 4차 = 3차의 날을 로컬에서 기사의 길이로. 호출 3회, 누계 약 $5.73) → 판정 "4차, 크기는 1차로" → **구현됨**: 로스터 Height 79, `Assets/@Art/Unit/Job/spellblade.png`, 소재(character·attack·hit)를 롱소드로, 공격·피격 자세 재생성(2회, 공격의 날 끝 −80 로컬)과 배선. 라운드 호출 5회, 누계 약 $5.79. 체인 9(`20261006-202707`): setup·sim OK, EditMode 719/719, PlayMode 72/73 — `GameFlowTests.AfterBoot_AnExpeditionOverTheShippedDataRunsToItsSettlement`가 멈췄다: 그 Test의 단계 switch에 13단계의 야영지(`GamePhase.Camp`)가 없어 파티가 야영지 층까지 살아남으면 무한 루프였다(런 시드가 매번 달라 그동안은 그 전에 끝났다). `RestAtCamp`를 더해 고침. 체인 10(PlayMode만 다시, `20261006-203816`): 73/73. 스크린샷(`20261006-b15f`): 9/9, PNG 63. `ko_05_battle.png`의 카이(2열)가 기사와 같은 롱소드를 들고 있고 `ko_24_battle_hit_pose.png`의 피격 자세도 같은 검이다. 사본은 `ArtPipeline/Archive/39-spellblade-sword/game/`.
- **Round 40**(사용자 "나머지 용병 다섯의 붕괴 각성 자세 만들자(캐릭터 일관성 유지_얼굴 크기 등)"): 기사·주교·성기사·대마법사·마검사의 붕괴·각성 자세 열 장(10회, 누계 약 $6.10). 소재는 `hit.csv`의 인물 묘사 + 발키리 문법의 자세 문장, 꼬리 문장에 같은 얼굴·머리 크기. 모델이 장식·무기를 몸보다 작게 그려(장식 0.7~0.85, 머리 0.85~1.0) 금 장식 대신 **머리 크기**로 배율을 잡았다(`fit_state_poses.py --scale`, 두 눈 거리와 격자 시트. 권장안, 사후 검토 대기 표). 리뷰 시트 `review-states.png`·`review-heads.png`. 판정 **"모두 확정 구현"** → 열 파일을 `Assets/@Art/Pose/Job/`에 배선(코드 변경 없음: 선택 메커니즘 그대로), Architecture/13·Design/10 §5 갱신.
  고친 것: `fit_pose.approved_place`가 Height 90을 가정해 Round 39의 마검사(Height 79) 공격·피격 자세가 게임 안에서 13.7% 컸다 → 로스터의 Height를 읽게 고치고 두 자세를 다시 맞춰 배선(`spellblade_attack.png`·`spellblade_hit.png`). 스크린샷 `20261006-b15g`: 9/9, PNG 63. `ko_24_battle_hit_pose.png`에서 카이의 피격 자세가 대기 그림과 같은 크기로 섰다(고치기 전에는 머리가 주교의 주교관 높이까지 올라왔다). 비교 `game-spellblade-hit-before-after.png`. 체인 11(열 자세의 배선과 함께): `20261006-220529`: setup·sim OK, EditMode 719/719, PlayMode 73/73(9 skipped). `ArtSetup`이 열 파일을 `F1-Art`의 Entry로 등록하고 Import 정책 `Pose`를 적용했다.
- 붕괴·쓰러짐의 소리는 빈사·버텼다의 것을 빌려 쓴다(14_SOUND). 새 소리는 요청 시.
- 화상·폭풍의 틱도 피격이라 피로를 올린다(02 §5의 정의대로). 틱이 많은 전투에서 피로가 빠르게 오를 수 있다. 16단계의 플레이에서 다시 본다.
- 조합 절벽은 15단계 전과 비슷하다(아래). 16단계.

## Verification

- `dotnet build Tools/Sim` 오류 0, `transform`·`validate` OK(`FatigueStateData.json` 새로).
- 스윕(스크래치 복사본, balanced, 1,000회, 시드 1): 출발값 피격 2·빈사 10·동료 3/15·처치 2에서는 모든 원정에 붕괴가 나고(100%) 쓰러짐 0.71/원정, 클리어 45%.
  채택한 피격 1·처치 3·빈사 5·동료 빈사 3·동료 사망 10·진입 2: 클리어 66.5% / 0.32(14단계와 같음), 붕괴가 난 원정 41%, 고통 0.40·각성 0.15·쓰러짐 0.01/원정, 문턱 이상의 생환자 4.5%.
- 시뮬(출고 데이터, 시드 1): balanced 3,000회 64.6% / 0.35(14단계 66.0% / 0.33), safe 54.8% / 0.30, none 81.5% / 1.28. 붕괴가 난 원정 40.9%, 고통 0.41·각성 0.14·쓰러짐 0.01/원정,
  생환자의 피로 평균 34.2·최고 198, 100 이상인 채 4.5%, 고통인 채 4.2%. 보스전 89.1%. 여섯 파티는 14단계와 같은 자리(Design/08 §11).
- 체인 1(규칙을 넣은 뒤): setup OK, EditMode 714/719 — 실패 다섯: 최대치에서 다시 쓰러지는 규칙(엔진이 값이 안 바뀌면 판정을 건너뛰었다 → 고침), 전투 안의 피로가 생겨 산수가 달라진 기존 Test 넷
  (TestData의 전투 피로 사건 값을 0으로 두고 `FatigueBreakdownTests`만 켜도록 고침).
- 체인 2(고친 뒤, `20261006-093421`): setup OK, sim OK, EditMode 719/719, PlayMode 70/70.
- 체인 3(화면을 넣은 뒤, `20261006-101506`): setup OK(Prefab 다시 만듦, 문구 표), sim OK, EditMode 719/719, PlayMode 72/73 — 실패 하나: 붕괴 Test의 띠 확인. 문턱 1 아래로 세워 붕괴가 시계를 멈추기 전의 Frame에 날 수 있었다
  → 6피격 아래로 세우고 멈춘 뒤에 붕괴가 없음을 확인하게 고침.
- 체인 4(PlayMode만 다시, `20261006-102749`): PlayMode 73/73(`[Explicit]` 9개 제외).
- 스크린샷 1(`~/Library/Caches/F1/screenshots/20261006-b15`): 9/9, PNG 62. 로비의 "피로도 128/200 · 공포"가 줄을 바꿔 글 상자를 넓혔다(330~610, 한 줄).
- 체인 5(로비를 고친 뒤, `20261006-104058`): setup OK, sim OK, EditMode 719/719, PlayMode 73/73. 스크린샷 2(`20261006-b15b`): 9/9, PNG 62. 로비의 글이 한 줄이다. 구현 뒤의 그림은 `ArtPipeline/Archive/36-fatigue-states/game/*_implemented.png`.
- 체인 6(Round 38의 연출을 넣은 뒤, `20261006-143943`): setup OK(상태 자세·조각의 Import와 Entry, Prefab), sim OK, EditMode 719/719, PlayMode 73/73(`[Explicit]` 9개 제외. 첫 실행에 통과).
  스크린샷 3(`20261006-b15d`): 9/9 — 머리 위의 글이 0.25초에 아직 안 보였다(전투의 시간 배율로 흘렀다) → 자세·떨림·조각·글을 실제 시간으로 고침.
- 체인 7(PlayMode만 다시, `20261006-145341`): 73/73.
- 체인 8(Round 39의 데이터 변경 뒤, `20261006-162831`): setup OK, sim OK(아이템 20), EditMode 719/719, PlayMode 73/73. 스크린샷 4(`20261006-b15e`): 9/9, PNG 63. 0.25초의 장면에 어둠·줌·붕괴 자세·먹 튐과 빛·머리 위의 "공포 / 아스트리드"가 모두 보인다. 구현 뒤의 그림은 `ArtPipeline/Archive/38-breakdown-fx/game/*_implemented.png`.
- `git diff --check` 깨끗.

## Next Action (제안)

- 16단계(맞추기)의 시작 지시를 기다린다. 세션 정리 때 12~15단계와 Round 32~40을 주제별로 커밋한다.
