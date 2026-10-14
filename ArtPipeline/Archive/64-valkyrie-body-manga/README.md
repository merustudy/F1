# Round 64 — 발키리: 더 섹시한 체형 셋, 실사 + 체인소맨 만화풍 섞음 (2026-10-14)

시험 라운드다. **지금의 그림체와 게임의 그림은 바꾸지 않는다**(G19 그대로). 시험 세트는 `body/`와 `manga/`에 두고 `gen_image.py`의 `--style`, `--roster`, `--reference`로 돌렸다.

## 지시

사용자(Round 63을 본 뒤): "이 스타일은 안어울리네. 현재 우리 그림체 기반에서 발키리에서 더 섹시한 체형으로 테스트 3장 뽑아주고, 그림체 섞음(실사랑 더 가깝게, 만화 체인소맨 스타일 강하게)으로도 테스트 이미지 만들어줘".

- 체형 시험: 세 장을 **서로 다른 방향**으로 했다(고를 수 있게). 얼굴·옷·도끼·자세·그림체는 지금 그대로, 몸만 바꾼다.
- 섞음 시험: 장 수가 없어 **한 장**(Round 63과 같은 단위)으로 했다.

## 방법

두 시험의 기준 그림은 모두 지금 게임의 발키리(`output/character/valkyrie.raw.png`)다. Round 63에서 기준 그림이 그리는 방식까지 끌고 가는 것을 봤으므로 §3(기준 그림 규칙)을 서로 반대로 적었다.

| 시험 | 스타일 문서 | §3 기준 그림 규칙 | 크기 |
|---|---|---|---|
| 체형 (`body/`) | 지금 `STYLE_RUNTIME.md`의 §1·§2·§4를 옮기고 셋만 바꿈: §2에 노출 금지 셋(nudity, see-through clothing, underwear), §4의 "Proportions and build" 한 줄(몸은 소재대로, 머리 크기는 그대로, 섹시하고 곡선이 분명한 체형), §3 | **그리는 방식·얼굴·옷·도끼·자세는 지키고 몸만 바꿔라** | 1024x1024 |
| 섞음 (`manga/`) | 새로 씀: 실사에 가까운 해부와 약 7등신, 거친 펜 선·해칭·먹 덩어리, 무표정하고 지친 눈, 낮은 채도의 평면 색. 작품·작가 이름은 프롬프트에 넣지 않음 | **누구인지만 가져오고 그리는 방식(카툰 비율·큰 머리·카툰 얼굴·고른 선)은 버려라** | 1024x1536 (7등신이라 세로) |

체형 셋의 소재(`body/character-a/b/c.csv`):

| 안 | 체형 | 옷의 변화 |
|---|---|---|
| A 모래시계 글래머 | 약 4.5등신, 둥글고 풍만한 가슴, 아주 잘록한 허리, 넓은 엉덩이와 굵은 허벅지, 어깨·팔은 덜 우람하게(위가 무겁지 않게) | 없음 |
| B 탄탄한 운동선수형 | 약 4.5등신, 풍만한 가슴, 선 몇 개로 그린 복근의 맨 허리, 둥근 엉덩이, 탄탄한 허벅지·종아리 | 상의를 가슴 밑까지 짧게(복근을 보이려고) |
| C 긴 다리 핀업 | 약 5등신, 긴 다리, 풍만한 가슴, 잘록한 허리, 곡선의 엉덩이, 가는 팔 | 없음 |

돌린 명령 (저장소 루트에서. X는 a·b·c):

```text
.venv/bin/python ArtPipeline/tools/gen_image.py --type character --key valkyrie --name valkyrie_body_X --style ArtPipeline/Archive/64-valkyrie-body-manga/body/STYLE_RUNTIME.md --roster ArtPipeline/Archive/64-valkyrie-body-manga/body/character-X.csv --reference ArtPipeline/output/character/valkyrie.raw.png
.venv/bin/python ArtPipeline/tools/gen_image.py --type character --key valkyrie --name valkyrie_manga --size 1024x1536 --style ArtPipeline/Archive/64-valkyrie-body-manga/manga/STYLE_RUNTIME.md --roster ArtPipeline/Archive/64-valkyrie-body-manga/manga/character.csv --reference ArtPipeline/output/character/valkyrie.raw.png
.venv/bin/python ArtPipeline/Archive/64-valkyrie-body-manga/mock_compare.py
```

| 이름 | 추정 비용 | 캔버스 위 키 (지금 발키리 630, 기사 632, 목표 806) | 반투명 |
|---|---|---|---|
| `valkyrie_body_a` | $0.028 | 705 | 2.5% |
| `valkyrie_body_b` | $0.028 | 662 | 2.4% |
| `valkyrie_body_c` | $0.028 | 732 | 2.5% |
| `valkyrie_manga` | $0.025 | 812 (세로 캔버스라 목표대로) | 2.7% |

합계 4회, 약 $0.11(그림 누계 약 $6.26 / $10). 원본과 맞춘 그림은 `body/`·`manga/`에 보관했다(`output/character/`에도 있음).

- `mock-body.png`: 지금과 A·B·C를 같은 높이로 크게, 그 아래 광산 무대(게임 크기의 절반, 기사·고블린 약탈자와 함께). 무대에서는 캔버스 전체를 게임의 그림 자리(300px)로 그려 게임에서의 크기 그대로 보인다.
- `mock-manga.png`: 지금과 섞음을 크게, 머리 2배, 광산 무대.

## 결과 (관찰)

### 체형 셋

- 셋 모두 **지금 그림체와 얼굴, 옷, 도끼, 자세를 지켰다.** Round 63과 달리 바꾸라고 한 것(몸)만 바뀌었다.
- **A 모래시계:** 어깨와 팔이 가늘어지고 허리가 잘록해져 위가 무겁던 몸이 모래시계가 됐다. 변화는 셋 가운데 가장 작다.
- **B 운동선수형:** 짧아진 상의 아래 복근이 보이는 맨 허리. 몸의 선이 가장 분명하게 드러나고 "전사"의 느낌이 가장 강하다.
- **C 긴 다리 핀업:** 다리가 길어지고 몸이 가늘어 가장 "핀업"에 가깝다. 다만 우리 그림체의 큰 머리·과장과 조금 덜 맞고(다른 용병들보다 날씬), 무대에서 가장 크게 선다.
- 무대 위에서 셋 모두 지금처럼 읽힌다. 키는 지금(630)보다 5~16% 크다.

### 실사 + 만화풍

- **실사 쪽으로는 크게 갔다:** 약 7등신, 실제 여성의 얼굴과 근육, 무표정하고 차가운 눈, 펜 해칭, 낮은 채도의 색. 정체(금발과 땋은 가닥, 망토, 도끼, 자세)는 지켰다.
- **체인소맨다움은 절반쯤이다:** 펜 해칭과 실사 해부는 들어갔지만, 그 만화의 거칠고 빠른 선과 굵은 먹 덩어리 대신 **촘촘하고 정돈된 판타지 펜화**에 가깝게 나왔다.
- **무대에서 다른 유닛과 어긋난다:** 굵은 외곽선이 없어 배경에 묻히고, 가는 해칭은 게임 크기에서 뭉개진다. 옆의 카툰 기사·고블린과는 다른 게임의 캐릭터로 보이고, 혼자 머리가 작고 키가 크다. 이 방향을 쓰려면 모든 유닛을 같이 바꿔야 한다.

## 판정할 것 (모두 권장안을 적었다)

| # | 질문 | 선택지 | 권장 |
|---|---|---|---|
| 1 | 체형 | A 모래시계 / B 운동선수형 / C 긴 다리 핀업 / 지금 그대로 / 섞어서(예: C의 다리 + B의 허리) | **B** — 몸의 선이 가장 분명하고 전사다움이 남으며, 다른 용병의 과장된 비율과 맞는다. C는 더 섹시하지만 혼자 날씬해 무리에서 튄다 |
| 2 | 채택하면 함께 다시 그릴 것 | 발키리의 대기·공격·피격·붕괴·각성 자세 다섯(호출 5회, 자세는 고른 대기 그림을 보고 그림) | 고른 체형을 대기 그림으로 확정한 뒤 다섯을 한 번에 |
| 3 | 여성 용병 둘(주교·대마법사)도 | 같은 방향으로 시험 / 발키리만 | 발키리를 정한 뒤 판단(발키리만 바뀌면 혼자 튈 수 있음) |
| 4 | 실사 + 만화풍 | a 접는다 / b 더 거칠게(선은 굵고 빠르게, 해칭은 줄이고 먹 덩어리를 크게, 굵은 외곽선) 한 장 더 / c 이 정도로 다른 유닛 하나(기사)를 그려 무리로 본다 | **a** — 게임 크기에서 해칭이 뭉개지고 지금 무리와 다른 게임처럼 보인다. 전환하면 그림 전부(호출 약 70~90회)와 G19를 다시 연다. 더 보고 싶다면 b |

호출은 지시가 있을 때만 한다. 그림을 게임에 연결하는 것도 승인 뒤에 한다(지금 게임의 발키리는 그대로).

## 판정 (2026-10-14)

사용자: **"현재 a, b형 체형 모두 가능하게 저장(나중에 여러 체형의 캐릭터 생성 예정). 일단 지금의 발키리는 a형 체형으로 변경 및 다시 그리기 수행"**.

- 판정 1: **A**(모래시계)를 발키리에. B(운동선수형)는 쓰지 않고 저장만.
- 판정 2: 발키리의 자세를 다시 그린다(사용자 "다시 그리기 수행").
- 판정 3(주교·대마법사)과 4(실사 + 만화풍)는 답이 없어 그대로 둔다. 4는 권장대로 더 하지 않는다.

## 구현

### 체형을 저장하는 법 (권장안으로 정함)

던전 컨셉과 같은 길로 했다: 문구는 스타일 문서 한 곳에, 고르는 것은 소재의 열.

| 어디 | 무엇 |
|---|---|
| `STYLE_RUNTIME.md` | `## Body: hourglass`(A), `## Body: athletic`(B) 절. 시험의 소재 문구에서 몸만 떼어 낸 것(약 4.5등신, 머리는 큰 그대로). 머리의 형식 계약에 한 줄, §4의 "follow the subject" → "follow the subject and its body type" |
| `tools/gen_image.py` | 소재의 `Body` 열을 읽어 그 절의 불릿을 Subject 다음에 "Body type:"으로 붙인다. `character`·`attack`·`hit`만 받고, 없는 이름은 호출 전에 실패한다. `--style` 시험 문서를 써도 체형은 `STYLE_RUNTIME.md`에서 읽는다 |
| `Rosters/character.csv`, `attack.csv`, `hit.csv` | `Body` 열을 더함. 발키리는 `hourglass`, Subject에서 몸 문구("brawny, top-heavy … short sturdy legs")를 뺌. 다른 행은 빈 값 |
| `Docs/Design/10` §2, `Docs/Architecture/13` | 결정(【확정】)과 규칙 |

확인: 발키리 세 타입의 dry-run에 "Body type:"이 붙고, 기사에는 붙지 않으며, 없는 이름(`slender`)은 "있는 체형: hourglass, athletic"과 함께 실패한다.

### 발키리를 다시 그림

| 그림 | 어떻게 | 호출 |
|---|---|---|
| 대기 | 시험 A(`body/valkyrie_body_a.raw.png`)를 그대로 확정 원본(`output/character/valkyrie.raw.png`)으로 놓고 `--refit`(사용자가 본 그 그림. 맞춘 그림이 시험 A와 같은 파일). 다시 그리면 얼굴이 흔들릴 수 있어 그리지 않았다 | 0 |
| 공격, 피격 | `--type attack`·`--type hit`, `--key valkyrie`(새 소재 + `Body`), 새 대기 원본을 기준 그림으로. 끝 문장은 Round 40의 것(같은 얼굴·머리 크기·비율) | 2 |
| 붕괴, 각성 | Round 38의 길: `--type hit --name valkyrie_broken`(`_resolute`) `--style Archive/38-breakdown-fx/STYLE_RUNTIME-statepose.md --roster state/<state>.csv`(Round 38의 소재에서 몸 문구를 `Body`로), 같은 끝 문장 | 2 |

명령은 `generate_poses.sh`, 지금 쓰던 산출물은 `output/_before-r64-hourglass/`(저장소 밖)과 Git에 있다.

맞추기(`tools/fit_pose.py`, `Archive/40-state-poses/fit_state_poses.py`):

| 자세 | 금 장식의 배율 | 머리로 본 것 | 쓴 배율 |
|---|---|---|---|
| 공격 | 1.026 | 리뷰 시트에서 대기와 같음 | 1.026 (장식) |
| 피격 | 0.926 | 원본에서 머리가 대기의 약 0.9배(눈 사이 47 대 50) | 0.926 (장식) |
| 붕괴 | 0.880 | 원본에서 머리가 대기의 1.04~1.10배(눈 사이 52 대 50, 눈~턱 70 대 68). 장식으로 맞추면 머리가 약 18% 커짐 — Round 40과 같은 일 | **1.05** (머리) |
| 각성 | 1.033 | 눈 사이 거리(`measure_eyes`)로 0.974 | **0.97** (머리) |

- `review-poses.png`: 지금(위)과 새것(아래)의 대기·공격·피격·붕괴·각성을 자세 캔버스의 같은 배율로(게임에서의 크기와 자리, 바닥선).
- `review-heads.png`: 머리를 같은 배율과 25px 격자로. 자리는 손으로 읽어 지금 줄의 피격은 머리가 칸 밖이다(새 줄만 본다).
- 확정 원본: 대기는 `body/valkyrie_body_a.raw.png`, 자세 넷은 `approved/`(`--refit`·`fit_pose`로 다시 맞출 수 있게).

게임에 연결한 파일(같은 경로, .meta 그대로): `Assets/@Art/Unit/Job/valkyrie.png`, `Assets/@Art/Pose/Job/valkyrie_attack.png`, `_hit.png`, `_broken.png`, `_resolute.png`.
아이템 `greataxe`의 아이콘은 그대로다(도끼는 바뀌지 않았다).

### 권장안으로 정한 세부 (사후 검토)

- 체형의 이름(`hourglass`, `athletic`)과 문구(시험 소재에서 몸만 뗌, 약 4.5등신), 고르는 열 `Body`, 시험 문서도 저장한 체형을 쓰는 것.
- 대기 그림은 다시 그리지 않고 시험 A를 그대로 확정.
- 자세 넷에 Round 40의 끝 문장(같은 얼굴·머리 크기)을 붙인 것, 붕괴·각성의 배율을 머리로 잡은 것(1.05, 0.97).
- 붕괴의 색 맞춤은 Round 38처럼 했다(일부러 어둡게 그린 색을 대기 쪽으로 반쯤 되돌림: ΔE 11.2 → 5.8).
- 대기 그림의 키는 699(지금 630)로 무대에서 약 11% 크게 선다. `Height`는 그대로 90.

### 검증

- 체인 `20261014-133731` **CHAIN OK**(setup, EditMode 845/845, PlayMode 83/92, 9 skipped: 스크린샷). 시뮬은 돌리지 않았다(그림만 바뀜).
- 스크린샷 9/9(PNG 86). 게임 안의 새 발키리: `game/impl_ko_39_battle_state.png`(대기), `impl_ko_23_battle_attack_pose.png`(공격), `impl_ko_38_battle_breakdown.png`(붕괴), 모은 것 `impl-game.png`.
- `git diff --check` 깨끗. `Assets`에서 바뀐 것은 그림 다섯뿐(.meta 그대로).

## 공격 자세의 도끼 다시 그림 (2026-10-14)

사용자: **"모래시계 체형 공격시 도끼날 부분이 이상한데 검토 후 이미지 재생성해줘"**.

- 검토: 1차(게임에 넣었던 것)의 도끼는 날이 머리쇠 아래로 갈고리처럼 늘어지고 굽은 날이 **앞(오른쪽)**을 봤다. 내리찍기라면 날은 자루에 직각으로 나와 **바닥**을 봐야 한다(대기 그림의 도끼를 그대로 돌린 모양, 이전 체형의 확정 공격 자세가 그랬다). 소재의 "the haft and the axe head in one straight line"이 날까지 자루 방향으로 그리게 했다고 본다.
- 2차: `--extra`로 "날은 자루에 직각, 날이 바닥을 봄, 갈고리처럼 굽지 않음"을 더했으나 같은 잘못.
- 3차: 그림 속 방향으로 적은 `--extra`("머리쇠는 자루의 오른쪽 아래 끝, 날은 **왼쪽 아래**(앞발 쪽)로 직각, 굽은 날은 왼쪽 아래의 바닥을 봄, 망치 모양 등은 **오른쪽 위**")로 바로 섰다. 배율 1.013(장식), 리뷰 시트에서 머리는 대기와 같다.
- 피격·붕괴·각성의 도끼는 다시 봤고 날이 자루에 직각이라 그대로 둔다.
- 호출 2회(2차, 3차) 약 $0.06. 1·2차는 `output/_before-r64-hourglass/attack-try1`, `attack-try2`(저장소 밖). `approved/valkyrie_attack.raw.png`와 게임의 `valkyrie_attack.png`를 3차로 바꿨다.
- 비교: `review-attack-axe.png`(1차·2차·3차·이전 체형의 확정 자세, 도끼 확대).
- 다음에 도끼를 내리찍는 자세를 그릴 때: 날의 방향은 "아래로·바닥을 향해"보다 그림 속 방향(왼쪽 아래·오른쪽 위)으로 적는다. 소재의 그 문장(`Rosters/attack.csv`)은 이번 판정과 함께 고칠지 정한다(지금은 그대로, `--extra`로 재현).

3차의 `--extra` (재현용):

```text
Correction for the axe, in the directions of the picture: the haft runs from her hands down to the lower right, and the axe head sits at its lower right end. The haft passes straight through the head. The broad grey crescent blade sticks out of the head toward the LOWER LEFT, back toward her front boot, at a right angle to the haft, and its long curved cutting edge faces down and to the left, toward the floor, so that the edge leads the chop. The short grey hammer-like back sticks out of the head the other way, toward the UPPER RIGHT, and the end of the haft pokes out just past the head. The blade does not point straight down, does not face right and is never bent like a hook or a sickle.
```

`--tail`은 위 "발키리를 다시 그림"과 같다(`generate_poses.sh`의 TAIL).

## 공격 자세의 얼굴: 3차 + 2차 얼굴 (2026-10-14)

사용자: **"도끼는 현재 만족. 얼굴은 2차 얼굴로 해줘. 이번에 생성한 그림 얼굴이 미묘하게 다르네"**.

- 호출 없이 합성했다(`compose_attack_face.py`). 다시 생성하면 얼굴이 또 조금씩 바뀌므로 2차의 얼굴을 그대로 옮기는 편이 지시에 맞다.
- 바탕은 3차(도끼), 얼굴은 2차. 반대(2차에 3차 도끼)는 3차의 날이 2차의 앞발 부츠를 덮어 하지 않았다.
- 2차를 두 눈의 흰자로 3차에 맞추면(배율 1.006, 2.4° 돌림) 눈·눈썹·귀·앞머리·머리 윤곽이 겹치고 입과 턱만 다르다(3차가 입을 더 벌려 턱이 약 10px 낮음).
  코 아래까지는 머리카락을 지나는 다각형 안을 통째로 2차로, 그 아래는 2차의 얼굴 윤곽선 안만 2차로(경계가 2차의 잉크선 위에 놓임), 3차의 더 긴 턱이 남는 곳은 2차의 목(망토 깃과 머리 한 가닥)으로 덮었다. 실루엣은 3차 그대로.
- 원본: `approved/attack-sources/attack_try2.raw.png`, `attack_try3.raw.png`(스크립트가 읽음), 결과 `approved/valkyrie_attack.raw.png`. 맞추기는 `tools/fit_pose.py`(배율 1.013).
- 비교: `review-attack-face.png`(2차·3차·합성의 전신과 얼굴).
- 거친 시도 둘(다각형 하나로 자름: 목선이 두 겹으로 번짐 / 얼굴 윤곽만 자르고 턱 띠를 피부색으로 칠함: 눈썹이 흐려지고 띠에 점 자국)은 버렸다.
- 검증: 체인 `20261014-153953` **CHAIN OK**(setup, EditMode 845/845, PlayMode 83/92, 9 skipped), 스크린샷 9/9(PNG 86). 게임 안의 공격 장면 `game/impl_ko_23_battle_attack_pose.png`, 확대 `impl-attack-game.png`. 남은 임시 장면 파일 없음.
