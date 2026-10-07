# 14. Sound

소리를 만들고, 승인받고, Unity에 넣고, 재생하는 길을 소유한다: `SoundPipeline` 폴더, 소리 스타일 문서, 생성 스크립트, 승인 라운드,
승인된 소리의 배선과 검사, `SoundManager`의 재생 규칙, 화면이 소리를 내는 자리.
소리의 방향은 `Docs/Design/12_Sound_Direction.md`, Manager의 책임과 Boot 순서는 `02_BOOTSTRAP_MANAGERS.md`, Address·Group·Scope는 `04_RESOURCES_ADDRESSABLES.md`,
설정 파일은 `07_SAVE.md`, 화면의 배치는 `12_UI.md`가 소유한다.

## 원칙

- 소리는 유료 API(ElevenLabs의 효과음과 음악)로 만든다. **요청받았을 때만 호출한다.** 한 번의 생성이 호출 한 번이고, 실행마다 호출 수의 상한(`--max-calls`)을 적는다.
- 호출 전에 실패할 수 있는 것은 전부 먼저 실패시킨다: 파일, 스타일 문서, 소재 목록 -> 그다음 키 -> 그다음 호출.
- 사람이 들어 보고 승인한 소리만 `Assets`에 들어간다.
- 스타일 문구는 문서 한 곳(`SoundPipeline/STYLE_SOUND.md`)에 있고 스크립트가 실행할 때 읽는다. 스크립트에 복사하지 않는다.
  게임·스튜디오·작곡가의 이름과 "in the style of"를 문구에 넣지 않는다.
- API 키는 macOS 로그인 키체인에만 둔다(서비스 이름 `ELEVENLABS_API_KEY`). 환경 변수, `.env`, 출력, 로그, 예외 메시지에 넣지 않는다. 키를 등록하는 명령은 사용자가 직접 실행한다.
- **소리는 연출이다.** 전투 결과, 저장, 명령, Bootstrap 성공의 조건이 아니다(CLAUDE.md §3). Domain, Application(`RunManager`, `ExpeditionManager`), 시뮬은 소리를 모른다.

## 구조

```text
SoundPipeline                  # 저장소 root. Unity가 Import하지 않는다
├─ STYLE_SOUND.md              # 스타일: 효과음의 공통 꼬리, 배경음의 공통 positive·negative. 스크립트가 읽는다
├─ Rosters
│  ├─ sfx.csv                  # Key, Subject(재료 + 동작 + 질감), Seconds(0.5 이상)
│  └─ bgm.csv                  # Key, Label, Bpm, Styles, Seconds
├─ Archive
│  ├─ calls.csv                # 유료 호출의 장부: 시각, 종류, Key, 후보, 요청 길이, 추정 비용
│  └─ <round>/                 # README.md(후보, 판정, 문구, 호출), 목업과 그 스크립트, 승인된 배경음의 원본(다시 자를 때)
├─ tools
│  ├─ soundlib.py              # 공통: 경로, 스타일 문서와 Roster 읽기, 키, 장부
│  ├─ gen_sfx.py               # 효과음 생성과 후처리. Key와 --variants, --max-calls(필수). --dry-run은 호출하지 않는다
│  ├─ gen_bgm.py               # 배경음 생성. --seconds로 시험(30초)과 전체 길이. 같다
│  ├─ loop_bgm.py              # 확정한 곡을 끊김 없이 반복되는 wav로 자른다(아래 "후처리"). 호출하지 않는다
│  ├─ playlist.py              # 후보의 재생 목록(output/index.html). 곡은 가장 작은 후보의 크기로 재생한다. 호출하지 않는다
│  ├─ measure.py               # 후보를 잰다: 길이, RMS·피크, 밝기, 어택, 감쇠. 들어 보지 않고 고를 때 쓴다. 호출하지 않는다
│  └─ usage.py                 # 계정의 플랜과 이번 기간의 크레딧 사용량. 생성하지 않고 비용이 없다 (키에 계정 읽기 권한이 있을 때)
└─ output/                     # 생성 직후 산출물과 후보 (gitignore)
```

- `.venv`는 ArtPipeline과 같이 쓴다. `elevenlabs` 2.70.0을 더했다(C1과 같은 판). 곡은 wav로 두므로 mp3 인코더는 없다. 디코드는 macOS의 `afconvert`.
- 스크립트는 `Docs/StarterKit/tools/gen_sfx.py`, `gen_bgm.py`를 이 프로젝트에 맞게 줄인 것이다(C1의 항목과 문구는 가져오지 않는다).
- 폴더는 첫 실제 파일과 함께 만든다. 거절된 후보는 `output/`에만 남고 Git에 올리지 않는다.

## 문구

- 효과음: 소재(재료 + 동작 + 질감. 예: "small wooden latch clicking shut, a crisp clack") + 공통 꼬리(방향, `dry`, 짧은 울림, `no music`) + 길이 힌트(`very short`, `short`).
- 배경음: 단일 청크의 composition plan. 곡의 positive(BPM은 숫자로) + 공통 positive(악기만, 반복되는 고른 흐름, 고조와 끝맺음 없음) + 공통 negative(밝음, 장난감, 보컬, 큰 오케스트라, 페이드아웃).
  `negative_styles`로 원치 않는 팔레트를 뺄 수 있는 길이 이것뿐이다(Kit/05).

## 후처리

```text
효과음  PCM 44.1kHz 수신 -> 모노로 접기 -> 앞뒤 무음 자르기(-60dB, 앞 5ms·뒤 30ms 여유) -> 피크 -6dBFS -> 16bit 모노 wav
배경음  mp3 수신(후보는 그대로 듣는다) -> 확정하면 loop_bgm.py: wav로 풀기 -> 마디 길이 측정(요청한 BPM의 ±10%)
        -> 처음 4초와 가장 닮은 악구(4마디) 경계에서 자르기(꼬리 1.5초는 쓰지 않음) -> 자른 뒤의 소리를 처음 60ms에 섞기
        -> RMS -20dBFS(피크 -1dBFS 아래) -> 16bit 스테레오 wav
```

- **효과음의 PCM은 문서와 달리 스테레오로 온다.** 모노로 읽으면 절반 속도, 한 옥타브 아래가 된다. 두 샘플씩 평균한다.
- **배경음은 끝을 페이드아웃한다**(negative에 넣어도). 그래서 확정한 후보를 다시 만들지 않고 그 후보를 악구 단위로 잘라 반복시킨다(다시 만들면 다른 곡이 된다).
  마디의 한가운데에서 처음으로 돌아가면 박이 튀므로 4마디의 배수에서 자른다. `--seam`이 이음매(끝 4초 + 처음 4초)를 따로 써서 들어 볼 수 있다.
- 곡은 wav로 둔다. Unity가 Vorbis로 한 번만 압축하고, mp3의 앞뒤 여백이 반복을 끊지 않는다.
- 효과음의 기음과 배경음의 조성이 어울리는지 본다.

## 승인 라운드

```text
1. 소재: Roster의 문구를 먼저 보여 준다 (호출 없음)
2. 상한: 라운드를 시작할 때 호출 수와 비용의 상한을 정한다. 닿으면 멈추고 보고한다
3. 생성: 효과음은 Key마다 후보 셋(A, B, C), 배경음은 30초 후보
4. 재생 목록: 후보를 HTML 한 장에 놓고 로컬 서버(127.0.0.1)로 연다(file://로는 소리가 나지 않았다). 끝나면 서버를 끈다
5. 판정: Key마다 "확정 A", "재생성 - 방향", "소재 교체"
6. 배경음은 확정한 후보의 문구로 전체 길이를 한 번 다시 만든다
7. 승인분만 배선하고 검증 체인을 한 번 돌린다
8. Archive/<round>/README.md: 후보, 판정, 받아들인 문구, 거절 사유, 호출 수와 비용
```

- 판정 없이 호출하지 않는다. 실패한 호출도 장부에 한 번으로 센다.
- **사용자가 판정을 맡기면**(2026-10-05 Round 03 "알아서 권장안 연결") 잴 수 있는 기준으로 고른다(`measure.py`). 반쯤 넘게 짧거나 거의 들리지 않는 후보는 뺀다.
  짧은 효과음(1초 이하)은 피크와 RMS의 차이가 가장 큰 것(또렷하고 마름), 긴 효과음은 밝기가 가장 낮은 것(어둡고 무거움),
  곡은 이음매가 처음과 0.85 이상 닮은 것 가운데 가장 어두운 것(없으면 가장 닮은 것). 이 규칙은 사용자가 직접 고른 Round 02의 넷과 모두 맞았다.
  고른 이유와 수치는 그 라운드의 README에 두고, 모든 후보를 판정용 페이지에 두어 사용자가 들어 보고 바꿀 수 있게 한다(바꾸면 파일만 바꾼다. 호출 없음).
- 라운드 폴더는 `NN-<이름>`이다(`01-design`: 음량 버튼의 목업, `02-test`: 시험 라운드).
- 유료 호출의 상한은 `Docs/Roadmap.md`의 그 단계에 적는다. 누계는 `Archive/calls.csv`가 가진다.

## Unity 배선

```text
효과음
1. wav   -> Assets/@Audio/Sfx/<key>.wav   (16bit 모노 44.1kHz)
2. 목록  -> SoundCatalog 의 효과음 한 줄 (Address "sound/sfx/<key>")
3. Entry -> SoundCatalog에서 자동으로 나온다 (Group F1-Audio, Scope App)

배경음
1. wav   -> Assets/@Audio/Bgm/<key>.wav   (loop_bgm.py의 산출물: 끊김 없이 반복되는 16bit 스테레오 44.1kHz)
2. 목록  -> SoundCatalog 의 곡 한 줄 (Address "sound/bgm/<key>")
3. Entry -> SoundCatalog에서 자동으로 나온다 (Group F1-Audio, Scope App)
```

- 소리는 CSV가 아니라 코드의 목록(`SoundCatalog`)이 가리킨다. 소리는 데이터의 행이 아니라 사건과 화면에 붙고,
  아이템의 소리는 데이터에 이미 있는 분류(`ItemData.Category`)로 고른다. 아이템마다 다른 소리를 둘 때 데이터 열을 더한다(Deferred).
- 화면이 낼 수 있는 소리는 `SoundEffect`(기획의 효과음 스물여섯)와 `MusicTrack`(곡 셋)이 모두 가진다. `SoundCatalog`에는 **승인된 것만** 있고,
  목록에 없는 소리는 나지 않는다(`Figure`가 빈 유닛이 그림 없이 서는 것과 같다. 목록에 있는데 파일이 없으면 검사와 Boot가 실패한다).
  새 소리가 승인되면 파일을 제자리에 두고 목록에 한 줄을 더한다. 코드는 바뀌지 않는다. 소리의 Key는 이름의 kebab case이고 `Rosters`의 Key와 같다.
- **Scope는 모두 `App`이다.** 효과음은 어느 화면에서나 나고 다 합쳐도 작다(PCM으로 약 2~3MB). 배경음은 스트리밍이라 곡 전체가 메모리에 올라오지 않는다.
  그래서 Boot가 한 번 읽고 앱이 끝날 때까지 둔다. 원정의 그림처럼 큰 것은 여전히 그 Scope에서 읽는다.
- Import 정책은 Setup 코드 한 곳(`AudioSetup`)이 강제하고 체인의 setup 단계가 부른다. Inspector에서 고치지 않는다.
  - 효과음: Force To Mono, Load Type Decompress On Load, 압축 ADPCM.
  - 배경음: Load Type Streaming, 압축 Vorbis(품질 70), Load In Background.
- Git: wav와 mp3는 이미 LFS다(`.gitattributes`).

## 재생 (`SoundManager`)

- `SoundManager`는 순수 C# 클래스다(`Assets/@Scripts/Core/Sound`). 생성자로 `ResourceManager`(Clip을 읽는다), `SettingManager`(음량), `SoundOutput`을 받는다.
  `SoundOutput`은 AppRoot가 자기 아래에 만드는 MonoBehaviour이고 AudioSource를 가진다: 음악 둘(곡을 바꿀 때 겹쳐 넘긴다), 효과음 여덟.
  Manager 목록과 의존 방향은 CLAUDE.md §4 그대로다. 다른 Manager를 `Managers.X`로 찾지 않는다.

```text
LoadAsync()             # Boot: SoundCatalog의 소리를 App Scope로 읽는다
PlayEffect(effect)      # 효과음 한 번
PlayMusic(track)        # 곡을 바꾼다. 같은 곡이 흐르고 있으면 아무것도 하지 않는다(화면이 열릴 때마다 불러도 된다)
```

- **겹침**: 같은 효과음이 60ms 안에 또 오면 내지 않는다. 비어 있는 효과음 AudioSource가 없으면 가장 먼저 시작한 것을 끊는다.
  x2·x4 배속에서 사건이 몰려도 이 둘이 소리를 묶는다.
- **높낮이**: 효과음마다 ±3% 안에서 흔든다. `UnityEngine.Random`을 쓴다(연출에만 쓰는 난수라 전투의 결정론과 무관하다).
- **시간**: 소리는 실제 시간으로 난다. 결정타가 `Time.timeScale`을 0.25로 낮춰도 소리의 높낮이와 길이는 그대로이고, 결정타의 소리가 그 0.5초를 채운다.
  전투를 멈춰도 이미 난 소리는 끝까지 나고 곡은 그대로 흐른다(멈춘 동안에는 사건이 없으니 새 소리도 없다).
- **곡 바꾸기**: 0.5초에 걸쳐 앞 곡을 줄이고 새 곡을 키운다(실제 시간).
- **음량**: 음악과 효과음의 크기는 `SettingManager`가 갖고 저장한다(0~100의 정수). 화면은 켬 100, 작게 30, 끔 0만 쓴다(30은 귀에 대략 절반 크기,
  `VolumeLevels`). 고르는 자리는 타이틀의 설정 줄이다(`12_UI.md` "타이틀"). 음악은 효과음 아래에 깔리게 설정 크기의 0.6배로 난다(`SoundManager.MusicLevel`).
  `SoundManager`는 처음에 그 값을 읽고, 저장에 성공한 뒤의 알림(`SettingManager.VolumeChanged`)을 받아 AudioSource에 적용한다.

## 소리를 내는 자리

화면이 낸다. 화면은 `Managers.Sound`로 부른다(그림을 `Managers.Resource`로 읽는 것과 같다). Manager와 Domain은 소리를 부르지 않는다.

전투는 `BattlePresenter`가 이벤트를 재생하는 그 자리에서 낸다. 300ms보다 오래된 이벤트를 건너뛰는 규칙(`12_UI.md` "연출")을 그대로 따르므로,
이어하기로 들어온 전투가 지난 소리를 한꺼번에 내지 않는다.

| 사건 | 소리 |
|---|---|
| `ItemActivated` | 아이템 분류의 소리: 무기, 방어, 공격, 지원 |
| `Damaged` (유닛의 타격) | 피격. 잃은 HP가 최대 HP의 20% 이상이면(큰 숫자가 뜨는 때) 큰 피격. 잃은 HP 없이 보호막이 다 막았으면 막힘 |
| `Damaged` (화상, 폭풍) | 없음. 숫자만 뜬다. 폭풍은 번개가 소리를 낸다 |
| `Healed`, `ShieldGained`, `BurnApplied` | 회복, 보호막, 불붙음 |
| `DogEntered` | 빈사 |
| `BrokeDown` (2026-10-06 Round 36) | 고통이면 빈사의 소리, 각성이면 버텼다의 소리. 붕괴의 소리는 아직 만들지 않았다(만들 때 바꾼다) |
| `Collapsed`, `FatigueStateEnded`, `FatigueChanged` | 없음. 쓰러짐 뒤에 오는 빈사나 사망이 낸다 |
| `DeathRolled` (살았다) | 버텼다 |
| `Died` | 적: 쓰러짐(결정타면 결정타의 소리 하나). 용병: 용병의 죽음 |
| `PotionUsed` | 포션 |
| `RetreatAttempted` | 후퇴 성공 / 후퇴 실패 |
| `StormTicked` | 번개 |
| 폭풍이 와서 양초가 꺼질 때 (`CandleView`) | 양초가 꺼짐 |
| 결과 창이 뜰 때 | 승리 / 패배 |

| 화면 | 소리 |
|---|---|
| 버튼을 누름 | 버튼. `UiBuild.MakeButton`이 버튼마다 붙이는 `ButtonSound`가 낸다. 자기 소리가 있는 버튼(출발, 노드에 들어가기, 두 "인벤토리로")은 Builder가 조용히 만든다(`UiBuild.Silence`) |
| 로비의 출발 | 원정 출발 |
| 노드 맵에서 노드에 들어감 | 노드 이동 |
| 보상 받기(보드, 인벤토리, 포션), 보드·인벤토리 사이 옮기기(`PartySideView`) | 아이템 넣기. 칸을 눌러 고르는 클릭은 버튼 소리 |
| 상점에서 사기(보드, 인벤토리, 포션)(`NodeMapScreen`, 17단계) | 아이템 넣기. 물건의 타일을 눌러 고르는 클릭과 새로고침·나가기는 버튼 소리. 상점의 소리(코인)는 아직 만들지 않았다 |

- 곡은 화면이 열릴 때 그 화면의 곡을 부른다: 타이틀·로비·귀환 정산은 로비 곡, 노드 맵·보상·전투는 던전 곡, 보스 노드의 전투는 보스 곡.

## Boot

- 새 단계 `LoadSounds`(오류 코드 `BOOT-13`)가 `LoadStaticData` 뒤, `LoadRun` 앞에서 `SoundManager.LoadAsync()`를 부른다.
  실패하면 다른 단계처럼 멈춘다. 빠진 소리를 소리 없음으로 바꾸지 않는다.
- 소리 장치가 없어도(체인의 Batch) AudioSource는 오류 없이 돌고 소리만 나지 않는다. Test는 실제 소리가 아니라 `SoundManager`가 낸 기록을 본다.

## Test와 검사

- EditMode: `AudioSetup.FindProblems`가 비어 있다(`SoundCatalog`의 소리마다 파일이 있고 Import 정책이 맞다. `Assets/@Audio`에 아무도 가리키지 않는 파일이 없다).
  `AddressablesSetup.FindProblems`가 소리 Entry를 포함해 비어 있다(`SoundCatalogTests`, `AudioSetupTests`). 소리마다 `Rosters`의 행이 있다. 출고 데이터의 아이템 분류마다 발동 소리가 있다(`ItemSoundTests`).
  음량 단계(`VolumeLevels`), 설정 파일의 1 -> 2 변환과 음량의 범위, 언어를 바꿔도 음량이 남는 것, 저장 실패(`SettingManagerTests`).
- PlayMode(`SoundManager`가 화면이 청한 소리 `Asked`와 실제로 낸 소리 `Played`를 기록한다): 같은 효과음이 60ms 안에 두 번이면 한 번 나고 목록에 없는 소리는 나지 않는다.
  타이틀의 음악·효과음 버튼이 켬 -> 작게 -> 끔으로 바뀌고 소리가 곧 따르며 앱을 다시 켜도 남는다. 저장에 실패하면 바뀌지 않고 알린다.
  곡이 화면을 따른다(타이틀·로비는 로비 곡이라 아직 조용하고, 노드 맵부터 던전 곡이 스트리밍으로 흐르고, 보스전은 보스 곡). 출발은 클릭 없이 출발 소리.
  전투가 피격·무기 발동·결정타·용병의 죽음·양초가 꺼짐·번개의 소리를 청한다(`UiFlowTests`).
- 소리의 느낌은 자동 Test로 판정하지 않는다. 재생 목록과 직접 플레이로 듣는다.

## Validation

- 요청받지 않은 호출이 없었는가? 상한 안인가? 장부에 적혔는가?
- 승인되지 않은 소리가 `Assets`에 없는가? 키가 출력, 로그, 파일에 없는가? 이름이 문구에 없는가?
- 새 소리의 파일, `SoundCatalog`, Entry, Import 정책이 서로 맞는가?
- Domain과 Manager가 소리를 부르지 않는가? 소리가 결과·저장·명령의 조건이 아닌가?

## Deferred and Forbidden

- Deferred: 아이템마다·적마다 다른 소리(데이터 열), 던전마다 다른 곡, 환경음, 전투 밖의 연출 소리(화면 전환, 로비·정산의 연출), 설정 화면과 음량 슬라이더, 음성(대사, 내레이션).
- Forbidden: 요청 없는 생성, 판정 없는 재생성, 승인 전 배선, 키를 환경 변수나 파일에 두기, 스타일 문구를 스크립트에 복사하기, 이름을 문구에 넣기,
  Inspector로 Import 설정 고치기, 소리의 끝을 명령·저장의 조건으로 쓰기, Domain이나 Application에서 소리 부르기, 범용 Audio·Event Framework, 소리용 Pool Manager.
