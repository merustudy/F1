# Slice D 55단계: 인트로와 리넷의 대화

Snapshot: 2026-10-24 오후, 56단계(최대 체력과 살기의 부적, 이긴 전투의 회복 삭제) 세션 정리 뒤 (커밋·푸시 — `c35fc59` 기획·Architecture / `5a06fc5` 구현 / Roadmap·Handoff). 그 전: 2026-10-24 낮, Round 104(전투의 플레이 피드백 셋) 세션 정리 뒤 (커밋·푸시 — `c9ff1e4` 목업·판정·Architecture / `3be4d2c` 구현 / Roadmap·Handoff). 그 전: 2026-10-24 아침, 세션 정리 뒤 (Round 103과 4판의 인트로 줄: 커밋·푸시 — `96e13db` 기획·Architecture·목업 / `c7b3003` 구현 / `e529b77` 4판의 줄 / Roadmap·Handoff). 10-24 직접 플레이: 광산 원정 13분 42초, 클리어. 그 전: 2026-10-23 밤 세션 정리 뒤 (구현·체인·스크린샷 대조 끝. 커밋·푸시 — 기획·Architecture `893fcf4` / 구현 `62119e0` / Round 101 `81d13a8` / Roadmap·Handoff). 그 전: 같은 날 저녁 설계와 Round 101 목업 → 사용자 **"다른 세션 인트로 목업이 더 마음에 들어서 그걸로 변경. 나머지는 권장안 구현"**.

## Goal
Design/33 「검은 백합」의 인트로(§9)와 리넷의 대화 그림(Round 99)을 게임에 넣는다. 이야기가 바뀌면 Design/33을 먼저 고치고 그다음 **데이터만** 고친다(코드에 이야기의 글·이름·장면 차례·시간·카메라·그림 주소·화자·표정이 없다).

## State
- 판정: 인트로의 연출은 Round 98 움직임 목업(`ArtPipeline/Archive/98-intro-storyboard/make_motion.py`, 다른 세션이 만든 것), 나머지 열넷은 권장안. 판정 9(#3 줄 나누기)는 Round 98의 따옴표 색칠로 필요 없어져 줄 그대로(사후 검토). 기록: 00 "Round 101의 판정", Round 101 README "판정".
- 기획: Design/33 §9 공통 규칙(처음 본편·그 뒤 짧은 판, 클릭 다음·Esc 건너뛰기, Round 98 연출, 데이터가 가짐), 새 §12 리넷의 대화(대사 여섯 【확정】 (권장안)), §11의 4(넣은 것과 남은 것).
- 데이터: `Assets/@Data/Source/StorySceneData.csv`(장면 15, 카메라는 Round 98 `CAMERA`, 짧은 판은 `SHORT_SECONDS` — 본편 133초·짧은 판 78.5초 = 띠 포함), `NpcData.csv`, `DialogueData.csv`, `DialogueLineData.csv`와 Generated JSON 넷.
- 코드:
  - Data: `Definitions/StoryData.cs`(`StorySceneData`·`StoryCamera`·`StoryEffect`·`StoryTransition`·`NpcData`·`NpcPortrait`·`DialogueData`·`DialogueTrigger`·`DialogueLineData`), `StaticData`(표 넷, `ScenesOf`·`DialoguesInOrder`·`LinesOf`, `CheckStory`), `StaticDataFiles`·`StaticDataLoader`, 매퍼 넷(`DefinitionMappers.cs`), `CsvRow`의 새 읽기 다섯.
  - Domain: `RunState.SeenDialogues`, `Gameplay/Run/StoryRules.cs`(`DueDialogue`·`MarkSeen`·`LastHeard`·`WeeksLeft`).
  - Flow/Save: `RunManager.DueDialogue`·`LastHeardDialogue`·`WeeksLeft`·`MarkDialogueSeen`, 런 28(`RunRecord.SeenDialogues`, 매퍼 검증, `From27To28`은 모두 들은 것으로), 설정 3(`SettingsData.IntroSeen`, `SettingManager.MarkIntroSeen`).
  - 자원: `ResourceScope.Story`(`scope-story`), `UIManager`가 Story Scope를 놓음, `ArtSetup`(장면 그림 Scope Story·배경 정책, NPC 그림·대화 그림 Scope Lobby, 새 정책 Portrait).
  - UI: `ScreenId.Intro`(`ui/story/intro-screen`), `IntroScreen`, `IntroTimeline`(순수, Round 98을 옮김), `StoryHandoff`, `StoryKeys`, `Views/DialogueView`·`StoryBannerView`·`TownNpcView`, `LobbyScreen`(띠·대화·마을의 리넷, `StoryShown`, 결과 창의 [새 런]도 인트로로), `TitleScreen`([인트로 보기], 새 런 → 인트로), `UiPalette`의 이야기 색, `UiKeys.Story` 넷과 `Title.Intro`, 빌더 `UiPrefabSetup.Story.cs`·Lobby·Title.
- 그림(호출 없음): `Assets/@Art/Story/Intro/*.png` 열넷(02판 콘티의 생성 원본 — `v02/scenes/`의 JPG와 평균 1~2단계 차이로 같은 파일), `Unit/Npc/linette.png`(`linette_ingame_dhair`의 맞춘 그림), `Portrait/Npc/linette_{neutral,cold,smile}.png`(Round 99 approved의 8b·7·8).
- 문서: Owner 01·04·05·06·07·08·09·11·12·13·14의 "55단계" 절, CLAUDE.md §9, Roadmap 55단계.
- Test: EditMode `StoryDataTests`·`StoryRulesTests`·`StoryFlowTests`·`IntroTimelineTests`, `ShippedDataTests`(이야기), `SettingManagerTests`(설정 3 둘), `ArtSetupTests`(NPC는 자세 없음, Scope Story·Lobby), `RunSaveMapperTests`(버전). PlayMode `UiStoryTests` 셋, 기존 [새 런]은 `UiTestUtil.NewRunToLobby`·`PassTheIntro`, fixture는 `LobbyScreen.StoryShown = false`. 스크린샷 `TheStory_Korean`(`ko_93`~`ko_102`).
- 다른 세션의 미커밋 작업(건드리지 않음): `98-intro-storyboard/README.md`(움직임 목업 절), `make_motion.py`, `motion/`, `Archive/calls.csv`, `Docs/Workstreams/active/d-story-review.md`, `73-abyss/rejected/`, `99-linette/refs/`. 세션 전부터 바뀐 설정 셋(URP 둘·ProjectSettings).

- **Round 103**(2026-10-23 밤, 사용자 "1. 지금 유지 / 2. 권장안 / 3. 권장안", `ArtPipeline/Archive/103-handwriting-countdown/`): 손글씨 서체는 넣지 않음(Pretendard에 색, 내려받기 없음). 로비·원정 머리줄 "일식까지 N주"(`Story.WeeksLeft`, `Lobby.Day` 지움), 기한 뒤 "일식의 날 — 최종 결전", 원정의 기간 줄·파티 팝업 "(→ 일식까지 N주)", 정산 "N주가 지나 일식까지 M주 남았습니다", 원정 안내 "일식의 날에는 …", 남은 주 0은 `Expedition.DaysEclipse`·`PartyDungeonEclipse`·`Settle.DaysEclipse`·`NoDaysEclipse`. `StoryRules.WeeksLeftOn`(Test 하나 더). 결과 창의 "끝난 주"는 "N주차" 그대로. 문서: Design/33 §7·§9·§11의 4, 04 §2, 00 "Round 103의 판정", Architecture/06·12.
- **4판의 인트로 줄**(같은 밤, 스토리 세션 F1_스토리의 메시지 — 33 §9 4판과 대조): `StorySceneData.csv`의 #6 둘째 줄, #8 두 줄, #12 세 줄(새 첫 줄 "보물은 그 아이와 버려진 마을 하나를 사자 바닥이 났다.", 짧은 판 `2+3`). 그림·차례·시간·카메라는 그대로.

## Verification
- Round 103과 4판의 줄: 체인 `20261023-212324`(setup·sim OK, EditMode 1150/1150, PlayMode 실패 하나 — 원정 안내의 옛 문구 단언을 고침) → `20261023-214957` PlayMode OK → 데이터 뒤 `20261023-221628` **CHAIN OK**(EditMode 1150/1150, PlayMode 101/117, 건너뜀 16은 스크린샷). 스크린샷 `20261023-r103`.
- 전체 체인 `20261023-180510`: setup·sim OK, EditMode 1146/1149(실패 셋: ArtSetup의 NPC 자세 가정, 마이그레이션 버전 단언, Test CSV의 던전 id — 고침), PlayMode 101/117(건너뜀 16은 스크린샷).
- EditMode 다시 `20261023-183256` **1149/1149**. 스크린샷 `20261023-d55`(16/16).
- 스크린샷 대조: #3이 Round 98 목업 23초 프레임과 같은 구도·자막·금빛 따옴표(안내 글만 판정 3의 조작), 타이틀은 Round 101 목업과 같음, 대화 안 A·마을의 리넷 안 B는 목업대로. 고친 것 둘: 로비 띠의 위 띠가 반투명이라 머리줄 날짜가 비침 → 불투명 100px, 대화 대사가 세로 가운데 → 위쪽 맞춤. 그 뒤 체인 `20261023-184100` **setup·PlayMode OK**(PlayMode 101/117, 건너뜀 16은 스크린샷), 스크린샷 `20261023-d55b`(16/16)로 두 곳을 다시 대조했다. `Assets/InitTestScene*` 없음, 설정 셋은 세션 전과 같은 diff.

## Open
- 사후 검토: 판정의 권장안 열넷과 판정 9의 해석, 대사 여섯.
- 남은 것(Design/33 §11의 4): 본 그림 라운드(콘티 → 본 그림, 같은 주소; #3·#5 백합 문장 → 금빛 해, #7-2·#10 붕대 끝의 방울, #10의 옷), 소리(인트로 곡·장면 효과음 — `SoundNote`), 마을의 이름, 왕궁 장면과 끝, NPC 생성 타입(`npc`·`portrait`)을 `gen_image.py`에.

## Next Action
- **Round 104**(2026-10-24, 10-24 플레이의 피드백 셋 → 사용자 "2. … 살짝 커졌다가 원래 크기로 돌아오는 이펙트로 적용 / 나머지 권고안 적용 구현", `ArtPipeline/Archive/104-battle-feedback/`, 00 "Round 104의 판정", Architecture/12 "전투 화면"): 경직의 별(`StunStarsView`·`ArtTop`), 전투 시작의 차례(`BattleOpening`, 시계 0.0초에 멈춤, 걸음마다 배지·칩이 오름, 뒤 걸음의 글은 앞 글 위 — `BattleFxLayer.Float`의 `overRising`), 효과 칩·배지의 우클릭 카드(`EffectTooltipView`, 문구 `EffectCard.*`). 화면만, 규칙·시뮬·저장 그대로. 체인 `20261024-091911` CHAIN OK, PlayMode `20261024-103937` 103/120, setup·EditMode `20261024-115549` 1154/1154, 스크린샷 `20261024-r104c`(ko_80·103·104·105). 커밋 `c9ff1e4`·`3be4d2c`. 사후 검토: 판정의 권장안 스물하나, 별의 가로 자리(그림 자리의 가운데 — 왼쪽을 보는 쥐는 머리와 등 사이).
- 직접 플레이(사용자): 새 런의 인트로(본편 → 두 번째부터 짧은 판), 로비의 띠, 리넷의 대화와 표정, 마을의 리넷, 타이틀의 [인트로 보기]. 플레이 기록이 생기면 `Tools/playlog.py`로 먼저 읽는다.
- 다음 걸음 후보(지시 때): 본 그림 라운드(유료), 소리 라운드, 마을의 이름.
- 직접 플레이 `play-20261024-074251`(Round 103·4판 줄이 든 판): 인트로 본편 2분 7초, 광산 원정 13분 42초(노드 20·전투 13, 전투 9.2분·노드 맵 4.5분), 클리어 1·2주차. 원정 15~30분의 목표에 조금 못 미친다. 사용자 피드백은 아직 없다.
- 커밋하지 않은 것(스토리 세션의 작업): Design/33 4판의 문서(§2~§11의 4판 줄과 §11의 4 "4판의 인트로 줄 … → 옮겼다" 줄), 37, 00의 37 줄, Round 98 README·`make_storyboard.py`·`make_motion.py`·`motion/`, `102-marianne/`, `calls.csv`, `d-story-review.md`, `73-abyss/rejected/`, `99-linette/refs/`. 세션 전부터 바뀐 설정 셋(URP 둘·ProjectSettings).
- 세션 정리의 체인: EditMode `20261023-194852` 1149/1149(마지막 코드 변경 뒤), setup·PlayMode `20261023-184100`.
- 다른 세션의 미커밋 작업은 커밋하지 않았다: Design/33 §4 마리안 줄(Round 102), `102-marianne/`, Round 98 README·`make_motion.py`·`motion/`, `calls.csv`, `d-story-review.md`.
- **56단계**(2026-10-24, 최대 체력과 살기의 부적, Design/38 → 사용자 "권장안 구현"): `completed/d56-max-hp.md`. 같은 날 사용자 지시로 이긴 전투의 회복(10%)을 지움(광산 52.0%·죽음 0.55, 보정 없음). 체인 `20261024-145231` CHAIN OK. 커밋 `c35fc59`·`5a06fc5`. 직접 플레이에서 볼 곳: 상점에서 허리띠를 놓고 뺄 때의 파티 열, 줍기의 HP 막대, 부적의 전투 시작 한 걸음, 원정 시간.
- **다음 세션의 시작**: `CLAUDE.md → Docs/Roadmap.md(맨 위 요약, 날짜 표, 43·56·57단계 절) → Docs/Workstreams/active/d55-story-intro.md → Docs/Workstreams/completed/d57-trait-expansion.md → Docs/Design/39_Trait_Expansion_Proposal.md(§9)` 순서로 읽고 상태를 복구한다. 새 직접 플레이 기록이 있으면 `Tools/playlog.py`로 먼저 읽는다. 열린 판정은 없다. 57단계(체격·성격·경험·붕괴 특성과 새 효과, 이름의 성별로 고르는 그림, 전사의 거인·왜소와 여자 전사, 거인 138%)와 56단계·Round 104를 플레이로 확인할 곳, 사후 검토 거리(Design/39 판정 열여덟 — 특히 동료의 죽음 30%·체격의 가중치·거인 138%, 특성의 동료 피로에 전투 로그 줄 없음, Design/38 판정 아홉, Round 104의 스물하나, Round 101의 열넷과 판정 9), 남은 것(58단계 훈련 — 막사의 목업 먼저, 다른 직업의 체격·성별 그림, 본 그림 라운드, 소리, 마을의 이름, 왕궁 장면과 끝)과 유료 호출 누계(477회 약 $13.58 / 상한 $20)를 요약하고 지시를 기다린다.
