# 10단계: 소리

2026-10-06 사용자 "Slice A는 여기서 완료 및 세션 정리"로 10단계를 완료해 `completed/`로 옮겼다. 남은 사후 검토(듣기)는 Open, 다음 단계는 Roadmap 11(새 Slice B의 범위).

Snapshot: 2026-10-05 — 마지막 지시 "남은 것도 생성후 알아서 권장안 연결". 나머지(효과음 스물셋, 로비·보스 곡)를 만들고 잴 수 있는 기준으로 골라 모두 연결했다.
그 전: "모두 a로 구현"(Round 02의 넷), "모두 권장안대로"(G11 = A 어두운 카툰, 설계안, 음량 버튼 안 2, 시험 라운드), "html로 제공해줘"(판정용 페이지).

## Goal

G11(소리 방향)이 정해져 `Docs/Design/12_Sound_Direction.md`에 【확정】으로 있다(됨). 사용자가 들어 보고 승인한 소리(효과음 스물여섯, 곡 셋)만 게임에 연결되고,
Import 정책이 Setup 코드로 고정되고, 타이틀의 설정 줄에서 음악·효과음의 크기를 바꿀 수 있고, 체인을 통과한다.

## State

- **모든 소리가 연결됐다**: 효과음 스물여섯과 곡 셋이 `SoundCatalog`에 있다. Round 02의 넷은 사용자가, 나머지는 권장 기준으로 골랐다(`SoundPipeline/Archive/03-rest/README.md`).
  남은 것은 사용자의 듣기 확인(사후 검토)이다. 바꾸면 그 파일만 바꾼다(호출 없음).
- 체인 통과(아래). 커밋 안 함. 그림 작업 Handoff는 `completed/a-art-restyle.md`.
- 판정용 페이지(사용자의 비공개 페이지, 버전 3): https://claude.ai/artifact/GJRdPV9YCb8iTktQ59UAD2 — 모든 후보, 고른 것, 세 곡의 반복과 이음매.
- 유료 호출(소리) 누계 87회, 추정 $1.28(상한 $10). 키는 생성만 되고 계정 사용량 읽기 권한이 없다(`usage.py` HTTP 401).

## Done

- 문서: Design/12 【확정】, Architecture/14 등록(CLAUDE.md §2·§8), 01·02·04·07·09·12·13, Design/00, Roadmap("10단계의 승인", G11 결정됨, 사후 검토 둘).
- `SoundPipeline`: 스타일 문서, 소재(`Rosters/sfx.csv` 스물여섯, `bgm.csv` 셋), 도구(`soundlib`, `gen_sfx`, `gen_bgm`, `playlist`, `usage`, `loop_bgm`, `measure`), 장부,
  `Archive/01-design`(목업, 안 2), `Archive/02-test`(시험, 판정, 연결, 곡의 원본), `Archive/03-rest`(나머지, 고르는 규칙 `pick.py`와 수치 `picks.json`, 곡의 원본).
- 게임: `Core/Sound`(`SoundCatalog`·`SoundEffect`·`MusicTrack`, `SoundManager`, `SoundOutput`), `VolumeLevels`, `SettingManager`의 음량과 `VolumeChanged`, `SettingsData` 버전 2, `ManagerSet.Sound`,
  Boot `LoadSounds`(BOOT-13), `AudioSetup`(Import 정책, 검사)과 `AddressablesSetup.Audio`(Group `F1-Audio`, App Scope), `ButtonSound`(모든 버튼의 클릭, `UiBuild.Silence`),
  타이틀의 설정 줄(언어·음악·효과음)과 문구 여섯, 화면마다의 곡, 출발·노드·아이템 넣기의 소리, `BattlePresenter`의 사건 소리(`EffectOf` 분류별), 양초가 꺼짐, 승리·패배.
- 소리 파일: `Assets/@Audio/Sfx/<key>.wav` 스물여섯, `Assets/@Audio/Bgm/lobby.wav`(B, 8마디 26.67초), `dungeon.wav`(A, 8마디 20.87초), `boss.wav`(B, 8마디 17.13초). 모두 13MB(LFS).
- 칸 클릭: 파티 쪽 칸과 보상 카드를 조용히 하고 누른 결과대로 넣기나 클릭 한 소리(`PartySideView`, `RewardScreen`, Test가 확인).

## Open

- **듣기 확인**(사용자): 판정용 페이지. 먼저 로비 곡의 이음매(닮은 정도 0.72, 튀면 다른 후보나 마디로), 그다음 세 곡의 반복, 권장으로 고른 효과음 스물셋.
  바꾸라면 그 후보의 파일을 `Assets/@Audio`에 복사하고 체인(호출 없음).
- **결정타 분리(R2·R3)**: 설계안의 "첫 커밋"이었지만 하지 않았다. 소리가 `BattleScreen`의 결정타 코드를 건드리지 않아서다(사건 → 소리는 `BattlePresenter`). 결정타·쓰러짐의 연출을 바꾸는 라운드에서 한다.
- **직접 플레이로 들을 것**: 음악의 크기(효과음의 0.6배), 효과음의 높낮이 흔들기(±3%), x2·x4에서 소리가 몰리는지, 전투 중 반복되는 피격이 거슬리는지. 고치는 곳은 `SoundManager`의 상수.

## Verification

- 모든 소리를 연결한 뒤의 체인(2026-10-05 22:30, `~/Library/Caches/F1/chain/20261005-223040`): setup OK, sim OK, EditMode 656/656, PlayMode 60/60(`[Explicit]` 8개 제외).
  목록이 모두 찼다는 것(`SoundCatalogTests`), 곡이 화면마다 바뀌는 것(`Music_FollowsTheScreens`), 칸을 눌러 넣으면 클릭 없이 넣기 하나(인벤토리 팝업 Test)를 확인했다. 화면은 바뀌지 않아 스크린샷은 다시 찍지 않았다.
- 체인(2026-10-05 21:53, `~/Library/Caches/F1/chain/20261005-215340`): setup OK(Prefab·문구·폰트 아틀라스·Addressables `F1-Audio`), sim OK,
  EditMode 656/656(새 31: 소리 목록, 오디오 Setup, 아이템 소리, 음량과 설정 1 -> 2, ManagerSet), PlayMode 60/60(`[Explicit]` 스크린샷 8개 제외. 새 4: 타이틀의 음량 버튼과 다시 켜기,
  곡이 화면을 따름, 같은 소리 60ms·목록에 없는 소리, 음량 저장 실패. 전투 Test들이 피격·무기·결정타·용병의 죽음·양초·번개의 소리를 확인).
- `git diff --check` 깨끗, ProjectSettings·Scene 변경 없음, `Assets/InitTestScene*` 없음.
- 스크린샷 45장(8/8, `~/Library/Caches/F1/screenshots/20261005-215847`): 타이틀이 목업 안 2와 같다(두 Locale 모두 설정 줄의 글이 칸 안).
  시드와 상관없는 로비 빈 화면은 영어가 이전과 픽셀까지 같고 한국어는 폰트 아틀라스를 다시 만든 만큼(약 200픽셀, 밝기 2 이내)만 다르다. 나머지는 런의 시드와 Round 31·R1 뒤라 비교하지 않았다.

## Next Action (제안)

1. 사용자의 듣기 확인을 받으면 바꿀 것을 바꾼다(파일만, 호출 없음). 다음 단계는 11(새 Slice B: 전투 시스템 완성의 범위)이고 시작 지시를 받은 뒤에.
2. (2026-10-06 세션 정리에서 주제별로 커밋하고 `origin/feature/slice-a`에 푸시했다.)
