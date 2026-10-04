# Round 20 — 전투 준비 자세의 표정: 웃지 않는 얼굴 — A 진지함 채택, 직업 여섯 연결됨

사용자 지시 (2026-10-04, Round 19의 두 번째 판정과 함께):

- 추가 검토사항. 현재 게임 분위기가 밝고 명랑한 분위기가 아니라서 전투 준비자세에서 웃고 있는 것보다는 진지하고 약간 심각한 느낌이 좋을 것 같은데? 필요시 api 사용하여 테스트 이미지 제공하여 비교
- 검토 후 보고

게임에는 넣지 않았다. `Assets`, 스타일 문서, 소재는 그대로다.

## 지금

- 기획(Design/10 §2 "얼굴")이 "자신 있는 큰 웃음·비웃음"을 정해 두었고, 스타일 문서 §4가 "a wide charming mouth (a big confident grin, a cocky half-smile, a sly smirk, a gentle smile)"를 요구한다.
- 직업 여섯의 소재가 모두 웃는다: 발키리 "huge confident grin", 기사 "cocky half-smile", 대마법사 "sly knowing smile", 마검사 "cocky smirk", 주교 "gentle closed-mouth smile", 성기사 "serene confident smile".
- 무대는 다크 판타지 쪽이다: 디아블로 컨셉 UI(Round 14), 촛불 하나의 빛과 어두운 가장자리(Round 16), 노란 눈의 몬스터(Round 17). 웃는 용병이 이 무대에서 가볍게 보인다는 것이 지시의 요지다.

## 시험

- 방법: Round 19와 같다. 확정 원본(`Archive/12-roar-style/approved/character/valkyrie.raw.png`)을 기준 그림으로 그대로 붙이고 "같은 그림, **표정만** 바꾼다"(`STYLE_RUNTIME-face.md`: §1·§2는 지금 문서, §2에 웃는 얼굴을 금지로 더함,
  §3 자세·크기·자리·머리·옷·무기·색과 얼굴 생김새를 그대로, §4 같은 자세의 구도). 캔버스는 원본과 같은 1024x1024. 소재는 표정마다 한 파일(`serious.csv`, `grim.csv`): 정체 목록 + 같은 준비 자세 + 표정.
- 두 안(지시의 "진지하고 약간 심각한"을 두 세기로):

| 안 | 표정 (소재) |
|---|---|
| A 진지함 | serious and determined, no smile: 입을 굳게 다문 일자, 다문 턱, 내리고 조금 모은 눈썹, 가늘게 뜬 차분한 눈. 눈은 적 쪽 |
| B 약간 심각함 | grim and tense, no smile: 처진 입꼬리, 살짝 벌린 입술 뒤로 악문 이, 미간의 깊은 주름, 굳고 경계하는 눈. 눈은 적 쪽 |

```text
.venv/bin/python ArtPipeline/tools/gen_image.py --type character --key valkyrie --name valkyrie_serious \
  --style ArtPipeline/Archive/20-serious-face/STYLE_RUNTIME-face.md --roster ArtPipeline/Archive/20-serious-face/serious.csv \
  --reference ArtPipeline/Archive/12-roar-style/approved/character/valkyrie.raw.png
(B는 --name valkyrie_grim --roster .../grim.csv)
.venv/bin/python ArtPipeline/Archive/20-serious-face/review_face.py     # review-face.png
```

## 결과

| 안 | 비용 | 그려진 것 |
|---|---|---|
| A 진지함 `valkyrie_serious` | $0.027 | 입을 다물어 웃음이 사라지고, 눈썹을 내려 눈을 가늘게 뜬 결연한 얼굴. 차분하고 단단하다 |
| B 약간 심각함 `valkyrie_grim` | $0.027 | 미간을 찌푸리고 이를 악문 얼굴. 긴장과 분노 쪽으로 읽힌다. 게임 크기에서도 흰 이가 보여 표정이 가장 잘 읽힌다 |

- 두 안 모두 얼굴 생김새(눈·눈썹 모양·코·주근깨·귀·땋은 가닥), 머리, 옷과 장식, 도끼, 자세, 색이 지금 그대로다. 게임이 맞추는 크기도 같다(키 616 → 624, 폭에 맞춤).
- 리뷰 시트 `review-face.png`: 4층 보스전 목업(Round 19)에서 아스트리드의 그림만 바꾼 게임 크기 / 같은 배율의 얼굴 2배 / 전신.
- 후보는 `candidates/`(생성 그대로 `*.raw.png`, 게임처럼 맞춘 `*.png`).

## 권장

- **A 진지함**을 전투 준비 자세의 기본으로 권한다. "진지하고 약간 심각한"에 가장 가깝고, 웃음이 빠지면서도 화난 얼굴은 아니라 오래 보는 대기 그림으로 무난하다.
- B의 악문 이와 찌푸림은 분노·고통에 가까워, 대기보다 **공격·피격 자세**의 표정(Round 19)과 어울린다. 대기에서 B를 쓰면 피격 자세(이를 악문 찡그림)와 차이가 줄어든다.
- 같은 방향이면 공격 자세의 "joyful battle cry"(즐거운 함성)도 "fierce war cry"(사나운 함성)로 바꾸는 것이 맞다.

## 채택하면 할 일 (제안, 승인 전)

1. 기획 먼저: Design/10 §2 "얼굴"의 "자신 있는 큰 웃음·비웃음"을 고른 표정으로 바꾼다(용병. 몬스터의 "우스꽝스럽게 위협적인" 얼굴을 함께 바꿀지는 따로 정한다).
2. 스타일 문서 §4의 얼굴 문구와 직업 여섯의 소재 표정을 바꾼다(주교·성기사는 "온화한 미소" 대신 "차분하고 엄숙한" 같은 결).
3. 직업 여섯을 이번과 같은 방법(확정 원본 + 표정만)으로 다시 그린다: 호출 6회, 약 $0.16. 자세·옷·크기가 그대로라 맞추기와 화면은 바뀌지 않는다. 얼굴(`cutface.py`)도 다시 자른다.
4. Round 19의 자세 시험을 넓힐 때 공격 자세의 표정을 같은 결로 적는다.

## 호출

| 무엇 | 호출 | 비용 |
|---|---|---|
| A 진지함 `valkyrie_serious` (1024x1024, medium) | 1 | $0.027 |
| B 약간 심각함 `valkyrie_grim` (1024x1024, medium) | 1 | $0.027 |
| 직업 다섯 `<job>_serious` (채택 뒤) | 5 | $0.13 |
| 합계 | 7 | 약 $0.18 |

누계 176회, 약 $4.38(상한 $10. 이어서 Round 21의 무덤 셋까지 179회, 약 $4.47). 장부 `../calls.csv`.

## 판정

- 2026-10-04 (사용자): "권장안 반영" — **A 진지함** 채택. 몬스터는 "'우스꽝스럽게 위협적인' 얼굴" 그대로. "내 승인 후 연결".
- 2026-10-04 (사용자): 직업 여섯 "연결" → 아래 "연결".

## 채택 뒤 (2026-10-04)

- 기획 먼저: Design/10 §2 "얼굴"에 【확정】(용병은 진지하고 결연하다, 웃지 않는다. 직업마다 결이 다르다), §3에 【확정】(몬스터 얼굴은 그대로).
- 스타일 문서 §4의 얼굴 문구: 웃는 입 대신 "웃지 않는 진지한 입(굳게 다문, 다문 턱, 얇게 다문). 어두운 던전에서 싸우려는 용병은 결연하고 엄하고 경계하거나 침울하다".
- 소재 `Rosters/character.csv` 여섯의 표정: 기사 엄한 찡그림과 다문 입, 발키리 결연함, 주교 엄숙함(조용한 일자 입술), 성기사 무겁고 결연함, 대마법사 집중(다문 입술), 마검사 차가움(얇은 일자 입).
- 다시 그림(호출 5회, 약 $0.13): 기사·주교·성기사·대마법사·마검사를 확정 원본 + `STYLE_RUNTIME-face2.md`(v1에서 §3의 "the freckles"를 "any freckles, stubble or beard it has"로. 주근깨 없는 캐릭터에 그려 넣지 않게) + 지금의 소재로.
  발키리는 시험의 A(`valkyrie_serious`)를 쓴다. 여섯 모두 자세·옷·무기·얼굴 생김새가 그대로이고, 게임이 맞추는 키가 거의 같다(기사 626, 주교 806, 성기사 693, 대마법사 806, 마검사 726, 발키리 624).
- 비교 시트 `review-six.png`(`review_six.py`: 직업마다 지금 / 진지함의 얼굴 2배와 전신). 후보는 `candidates/`.

```text
.venv/bin/python ArtPipeline/tools/gen_image.py --type character --key <job> --name <job>_serious \
  --style ArtPipeline/Archive/20-serious-face/STYLE_RUNTIME-face2.md \
  --reference ArtPipeline/Archive/12-roar-style/approved/character/<job>.raw.png
.venv/bin/python ArtPipeline/Archive/20-serious-face/review_six.py      # review-six.png
```

## 연결 (2026-10-04, 사용자 "연결". 호출 없음)

1. 이전 그림을 `before/`에 두었다(직업 여섯의 `Assets` 그림과 기준 그림 시트). 파이프라인의 이전 출력은 `output/character/old-smile/`(저장소 밖).
2. `output/character/<job>.png`·`.raw.png`를 새 그림으로 바꾸고 `Assets/@Art/Unit/Job/<job>.png` 여섯에 복사했다. 새 확정 원본은 `approved/character/<job>.raw.png`(다음 표정·자세 시험의 기준 그림).
3. 얼굴: `cutface.py --type character` → `Assets/@Art/Face/Job` 여섯. 보정값(`FaceDx`, `FaceDy`)은 그대로다: 자세가 같아 눈의 자리가 이전과 1px 안에서 같고, 자른 틀도 같다.
4. 기준 그림 시트 `References/Character/style_ref_roster.png`를 새 발키리·기사·대마법사로 다시 합성했다(`make_reference_sheet.py`: 이전 시트에서 각 그림의 자리와 배율을 읽어 같은 바닥선·가운데·배율로). 모든 타입이 붙이는 기준 그림이라, 다음 생성부터 웃지 않는 얼굴을 본다.
5. 확인: `Tools/chain.sh` — setup OK(그림 열두 장 다시 Import, `.meta`·생성물은 그대로), sim OK, EditMode 616/616, PlayMode 47/47(`[Explicit]` 스크린샷 5개 제외).
   `Tools/screenshots.sh` 36장(5/5): 전투(`_05`)·노드 맵(`_04`)·보스전(`_09`)·전진 뒤(`_15`, 돌진 중인 아스트리드의 명패가 열에 그대로)·빈사(`_18`)·영어(`en_05`)에서 여섯 모두 웃지 않는 얼굴이다. 여섯 장은 `game/`.
   데이터·코드·Prefab은 바뀌지 않았다(그림 파일만).

```text
.venv/bin/python ArtPipeline/tools/cutface.py --type character
.venv/bin/python ArtPipeline/Archive/20-serious-face/make_reference_sheet.py
```
