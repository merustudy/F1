# 02 시험 라운드: 방향 A의 첫 소리 (2026-10-05)

G11(어두운 카툰)이 소리로 맞는지 보는 시험이다. 들어 보고 판정한 뒤 나머지(효과음 스물셋, 곡 셋의 전체 길이)를 다시 지시받아 만든다.

## 지시와 상한

- "모두 권장안대로"(Roadmap "10단계의 승인" 5): 효과음 셋 × 후보 셋 = 9회, 던전 곡 30초 × 셋 = 3회. 상한 12회, 추정 $0.3 아래.
- 고른 소리: 가장 자주 날 소리(피격), 가장 무거워야 할 소리(용병의 죽음), 화면의 소리(버튼). 곡은 가장 오래 들을 던전 곡.

## 문구

- 효과음 = `Rosters/sfx.csv`의 Subject + `STYLE_SOUND.md` §1 꼬리 + 길이 힌트(0.8초 이하 `very short`, 1.5초 이하 `short`). 후보 셋은 같은 문구를 세 번 부른 것이다(API에 시드가 없다).
  - 피격(0.5초): a blunt weapon striking a leather-armored body, a heavy dull thud with a small crunch
  - 용병의 죽음(2.0초): a fallen hero, a heavy body falling on cold stone, then one low distant iron bell toll fading out
  - 버튼(0.5초): a small iron latch clicking shut, a crisp short click with a faint wooden knock
- 던전 곡 = 단일 청크의 composition plan(`music_v2_5`, mp3 44.1kHz 192kbps): `[Abandoned mine]`, 92 BPM, creeping minor groove, plucked double bass, low clarinet and bassoon counter line,
  dry hand percussion and wooden clicks, mischievous but ominous, steady mid energy + `STYLE_SOUND.md` §3. negative는 §4.

## 결과

| 후보 | 길이 | 비고 |
|---|---|---|
| 피격 A, B, C | 0.48초씩 | 피크 -6dBFS로 맞춤. RMS -24.9 / -19.5 / -17.9dB |
| 용병의 죽음 A, B, C | 1.55 / 1.53 / 1.78초 | 2.0초를 요청했고 뒤의 무음을 잘랐다 |
| 버튼 A, B, C | 0.29 / 0.28 / 0.44초 | |
| 던전 A, B, C | 30초씩 | 서로 다른 곡. 음량이 크게 다르다(RMS 약 -24 / -21 / -15dB). 끝은 마지막 1초 안에서만 줄어든다(A) |

- 판정용 페이지: 사용자의 비공개 페이지 https://claude.ai/artifact/GJRdPV9YCb8iTktQ59UAD2 (소리 파일 열둘을 함께 올렸다. 서버 없이 열리고, 고른 것을 판정 문장으로 만들어 복사한다).
- 로컬 재생 목록 `output/index.html`(`tools/playlist.py`)은 곡 셋을 가장 작은 곡의 크기로 재생한다(B -2.8dB, C -7.5dB. 파일은 그대로). 큰 소리가 좋게 들리는 착시를 막는다.
- 후보 파일은 `output/`에만 있다(Git에 올리지 않음). 확정한 것만 다음 단계에서 쓴다.

## 호출

- 12회 모두 성공. 장부(`Archive/calls.csv`)의 추정 약 $0.24(효과음 9회 $0.016, 곡 3회 $0.225. API 요금표의 분당 단가로 셈).
- 실제 사용량은 읽지 못했다: 이 키에는 계정 정보를 읽는 권한이 없다(`tools/usage.py`가 HTTP 401). 생성은 된다. 사용량은 ElevenLabs 웹의 계정 화면에서 본다.

## 판정

사용자(2026-10-05): "모두 a로 구현" — 피격 A, 용병의 죽음 A, 버튼 A, 던전 곡 A. 방향 A(어두운 카툰)가 소리로도 맞다.

## 연결

| 소리 | 파일 | 가공 |
|---|---|---|
| 피격 | `Assets/@Audio/Sfx/hit.wav` | 후보 그대로(0.48초, 피크 -6dBFS) |
| 용병의 죽음 | `Assets/@Audio/Sfx/mercenary-death.wav` | 후보 그대로(1.55초) |
| 버튼 | `Assets/@Audio/Sfx/button.wav` | 후보 그대로(0.29초) |
| 던전 곡 | `Assets/@Audio/Bgm/dungeon.wav` | `loop_bgm.py dungeon_A_30s.mp3 --bpm 92`: 측정 92.0 BPM(마디 2.610초), 8마디 20.87초에서 잘라 반복, 처음과 닮은 정도 0.93, +2.5dB, RMS -20.0, 피크 -3.4dBFS |

- 곡은 다시 만들지 않고 승인한 30초 후보를 잘라 반복시켰다(같은 문구로 다시 만들면 다른 곡이 된다). 원본은 `approved/dungeon_A_30s.mp3`(다시 자를 때).
- 처음에는 요청한 92 BPM의 마디로 10마디(26.08초)를 골랐지만, 4마디 악구의 한가운데에서 처음으로 돌아가게 된다. 박자를 마디 단위로 재고(92.0) 악구(4마디) 경계에서만 자르게 고쳤다.
  8마디가 4초 비교로도 가장 닮았다(0.93).
- 이음매만 들어 보는 파일: `output/loop/dungeon_seam.wav`(끝 4초 + 처음 4초. Git에 올리지 않음).
- 게임: `SoundCatalog`에 넷이 올라갔고 나머지 효과음 스물셋과 로비·보스 곡은 목록에 없어 아직 나지 않는다(타이틀·로비·정산과 보스전은 조용하다).
