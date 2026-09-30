# 05. Audio Generation Pipeline

ElevenLabs로 효과음(Sound Effects API)과 배경음(Music API)을 만드는 구조다. 스크립트 원본:
[`tools/gen_sfx.py`](tools/gen_sfx.py), [`tools/gen_bgm.py`](tools/gen_bgm.py).

## 환경

```bash
.venv/bin/pip install elevenlabs
```

C1 기준 `elevenlabs` 2.70.0. 키는 이미지와 같은 방식으로 로그인 키체인에 둔다(서비스 이름 `ELEVENLABS_API_KEY`).

```bash
security add-generic-password -s ELEVENLABS_API_KEY -a "$USER" -w
```

선택: 에이전트 스킬(프롬프트 작성 요령·API 레퍼런스). `.claude/skills/`, `.agents/skills/`, `skills-lock.json`이 생긴다(Node 필요).

```bash
npx skills add elevenlabs/skills --skill sound-effects --skill music
```

## 효과음 (`gen_sfx.py`)

```bash
.venv/bin/python ArtPipeline/tools/gen_sfx.py <out-dir> [name ...]
```

이름을 주면 그 항목만 다시 만든다.

```python
STYLE = ", cute clean cartoon fantasy game, toy-like, dry, no reverb, no music"   # 모든 문구의 공통 꼬리

ITEMS = [   # name, prompt, seconds
    ("hit-attack", "Short warm wooden xylophone 'tok' hit with a tiny bright coin ding on top" + STYLE + ", very short", 0.6),
    ("shop-buy",   "Coins dropped into a leather purse with a bright little chime, purchase made" + STYLE + ", short", 0.8),
]

client.text_to_sound_effects.convert(
    text=text, duration_seconds=seconds, prompt_influence=0.6, output_format="pcm_44100")
```

후처리(순수 Python `array` + `wave`, 외부 도구 없음):

```text
PCM 수신 -> 스테레오를 모노로 접기 -> 뒤쪽 무음 트림(-60 dB, 30ms pad) -> 피크 -6 dBFS 정규화 -> 16bit mono 44.1kHz wav
```

함정:

- **`pcm_44100`은 문서와 달리 스테레오 인터리브로 온다.** 모노로 읽으면 절반 속도·한 옥타브 아래가 된다. 반드시 두 샘플씩 평균한다.
- `duration_seconds` 최소는 0.5 (미만은 422).
- 3회 재시도, 실패 메시지에는 예외 타입만 남긴다(키가 섞이지 않게).
- `ThreadPoolExecutor(max_workers=3)`로 동시 3개.

문구 요령(C1에서 잘 먹힌 형태): **재료 + 동작 + 질감**("small wooden latch clicking shut, a crisp 'clack' with a tiny metal pin") +
공통 `STYLE` + 길이 힌트(`very short` / `short`). 공통 꼬리의 `dry, no reverb, no music`이 게임 효과음다운 결과를 만든다.

## 배경음 (`gen_bgm.py`)

```bash
.venv/bin/python ArtPipeline/tools/gen_bgm.py <out-dir> [name ...]
```

```python
SHARED_POSITIVE = ["<장르> game soundtrack", "instrumental only", "loopable steady groove",
                   "no build-up", "no ending drop", "dry room production"]
SHARED_NEGATIVE = ["vocals", "singing", "epic orchestral swell", "fade out", ...]   # 빼고 싶은 악기·분위기

ITEMS = [   # name, section label, positive styles, seconds
    ("main-bgm", "[Lobby]", ["76 BPM", "dark lo-fi hip hop beat", "dusty slow drums", ...], 60),
]

plan = {"chunks": [{"text": label, "duration_ms": seconds * 1000,
                    "positive_styles": positive + SHARED_POSITIVE,
                    "negative_styles": SHARED_NEGATIVE,
                    "context_adherence": "high"}]}

client.music.compose(composition_plan=plan, model_id="music_v2_5", output_format="mp3_44100_128")
```

- **단일 청크 composition plan**을 직접 쓴다. `negative_styles`로 원치 않는 팔레트를 확실히 뺄 수 있는 유일한 경로였다.
  plan 생성 API는 30초도 여러 청크로 쪼개므로 루프 곡에는 쓰지 않는다.
- `composition_plan`과 `music_length_ms`를 같이 주면 에러다.
- positive에 **BPM을 숫자로** 넣는다. 나중에 마디 경계 트림의 기준이 된다.
- 비용 절감 2단계: **30초 후보 여러 개 → 확정한 것만 전체 길이**로 재생성.

### 루프 후처리 (필수)

전체 길이 곡은 `"fade out"`을 negative에 넣어도 **끝을 페이드아웃**한다(C1: 마지막 3~5초 무음 → 루프 구멍).

```text
mp3 -> wav 디코드 -> 초당 RMS 프로파일로 꼬리 확인 -> BPM 기준 마지막 온전한 마디 경계에서 자르기 -> 음량 맞추기 -> mp3 재인코드
```

ffmpeg 없이 한 방법(macOS):

```bash
afconvert -f WAVE -d LEI16@44100 in.mp3 out.wav
```

- 측정: 순수 Python(`wave` + `array`)으로 RMS/피크, 초당 프로파일. 곡 사이 음량 차이도 여기서 잰다(C1: 로비가 4.5 dB 커서 −4 dB).
- mp3 재인코드: `lameenc`(pip). C1에서는 `.venv`가 아니라 시스템 Python에 `--user`로 설치해 썼다. 새 프로젝트는 `.venv`에 넣고 트림 스크립트를 `ArtPipeline/tools/`에 **커밋**한다(C1은 scratchpad에서만 돌려 남아 있지 않다).
- 효과음의 기음과 배경음의 조성이 맞는지도 본다(C1: 세 곡 D단조 계열, 타격음 기음 C).

## 승인 라운드

```text
1. 방향 제시: 글로 방향 2~3개 (예: A 어두운 카툰 판타지 / B 앰비언트 / C 로파이) -> 사용자 선택
2. 후보 생성: 효과음은 종류당 A/B/C, 배경음은 30초
3. 재생 목록: HTML 한 장에 <audio> 나열 -> 사용자가 듣고 "3 A, 5 B" 식으로 판정
4. 확정본만 Assets/@Audio/ 로 복사 (같은 파일명으로 덮어쓰면 코드·Address·.meta 불변)
5. 검증 체인 1회
6. ArtPipeline/Archive/<round>/README.md 에 후보 설명과 결정 기록
```

- 재생 목록을 `file://`로 열면 오디오가 안 나왔다. 후보 폴더에서 로컬 서버를 띄운다(끝나면 종료).

```bash
python3 -m http.server 8765 --bind 127.0.0.1
```

- "안 들린다"는 시스템 음소거일 수 있다: `osascript -e 'output muted of (get volume settings)'`.
- 후보는 wav로 들려줘도 된다(인코더 없이 승인 가능).

## Unity 배선

```text
Assets/@Audio/SFX/<kebab-name>.wav   -> Address "audio/app/<kebab-name>"   (scope-app: 항상 메모리에)
Assets/@Audio/BGM/<kebab-name>.mp3   -> Address "audio/<main|run>/<kebab-name>"  (그 Scope가 열릴 때 Load)
```

1. 파일 복사
2. `AddressablesSetup.AudioEntries`에 `(assetPath, address, scopeLabel)` 한 줄
3. `SoundManager`의 Address 상수 목록에 추가 (C1은 Test가 효과음 **개수**를 고정해 누락을 잡는다)
4. 체인

Import 설정:

- SFX: wav, mono, Decompress On Load (기본)
- BGM: mp3, stereo, **`loadType: Streaming`**. C1은 `.meta`의 `loadType: 2`를 직접 고쳤고 재임포트에도 유지됐다. 새 프로젝트는 `AudioImporter` 정책을 Editor Setup 코드로 강제하는 편이 낫다(Sprite와 같은 방식).
- 첫 Import가 Compile Error로 중단되면 `.meta`가 guid 두 줄만 남는다. 다시 Import한 뒤 설정한다.

`SoundManager` 표면(C1):

```csharp
void PlayEffect(string address, float pitch = 1f);   // 미리 Load된 Clip을 OneShot
void PlayMusic(string address);                      // 같은 곡이거나 아직 Load 전이면 무시 (그래서 화면 Refresh가 매번 불러도 된다)
```

함정과 요령:

- 효과음 `AudioSource`가 하나면 **pitch가 재생 중인 OneShot 전부에 걸린다.** 음마다 pitch를 달리할 거면 Source를 나누거나 Pool을 둔다.
- 같은 소리를 연속으로 낼 때 pitch를 조금씩 올리면(C1: 줄당 +6%, 5음 음계 체인) 파일 하나로 상승감을 만든다.
- 크기 단계는 파일을 늘리지 말고 **겹쳐 쌓는다**: 기본음 / +sparkle / +thump+coin.
- 명령의 성공/거절 소리를 한 곳에서 낸다: `Execute(cmd, doneAddress)` → 성공이면 `doneAddress`, 실패면 `refuse`.
- 음악 전환 시점은 Gameplay 확정이 아니라 **화면이 그 상태를 보여주는 순간**에 맞춘다(보스 곡은 보스 팝업과 함께).
- 볼륨은 `SettingManager`가 소유하고 `SoundManager`가 읽는다. 결과에 영향 없는 pitch 흔들기에만 `UnityEngine.Random`을 허용한다.

## 절차적 효과음 (대안)

C1에는 API 이전에 Editor에서 파형을 합성해 wav를 쓰는 `Editor/Audio/SfxGenerator.cs` + `WavWriter.cs`도 있다.
UI 클릭 같은 단순음은 API 없이 이걸로 충분하고 비용이 0이다. 초기 Placeholder 용도로 권장한다.
