# Round 22 — 성기사의 머리 크기: 재발 방지와 다시 그림 — 연결됨

사용자 지시 (2026-10-04, 무덤 2안-B를 구현하고 "용병 이미지 재확인"을 하던 중):

- 성기사 얼굴이 다른 캐릭터들에 비해 작아서 이상해보여. 재방방지 대책을 세우고, 성기사 이미지 재생성

재발 방지의 규칙·소재·검사를 넣고, 다시 그린 성기사를 사용자 판정("연결") 뒤 게임에 넣었다(아래 "연결").

## 잰 것

맞춘 캔버스(672x896, 게임이 그림 자리 225x300에 그대로 줄여 보인다)에서 눈(가까운 눈의 흰자 가운데)에서 턱(수염이 있으면 수염 끝)까지:

| 용병 | 눈에서 턱 | 그림의 키(맞추기) | 소재의 등신 |
|---|---|---|---|
| 로언 · 기사 | 77 | 626 | 3.5 |
| 카이 · 마검사 | 68 | 726 | 4.5 |
| 엘라 · 주교 | 56 | 806 | 3.5 (주교관 포함) |
| 미라 · 대마법사 | 55 | 806 | 5 |
| 아스트리드 · 발키리 | 51 | 624 | 4 |
| **세드릭 · 성기사 (지금)** | **41** | 693 | **5, "긴 다리"** |
| 세드릭 · 성기사 (다시 그림) | 81 | 665 | 4 |

성기사의 얼굴은 가장 작은 발키리보다 20% 작고 기사의 절반 남짓이다. 원본에서 머리는 키의 약 7분의 1이다(영웅 비율).

## 원인

1. **소재**: `Rosters/character.csv`의 성기사만 "a tall, broad and heroic male paladin about five heads tall … long sturdy legs". 모델은 "heroic"과 "huge chest"를 작은 머리의 영웅 비율로 그렸다.
2. **맞추기**: 모든 그림을 같은 키(캔버스 높이의 90%)로 맞추고, 폭이 넘으면 폭에 맞춰 더 줄인다. 성기사는 철퇴를 치켜들고 방패를 들고 망토가 날리는 넓은 자세라 폭에 맞춰 693px로 줄었다. 5등신에 이 축소가 겹쳐 머리가 가장 작아졌다.
   대마법사도 5등신이지만 폭이 좁아 806px로 서서 얼굴이 범위 안이다.
3. **검사**: 리뷰 시트가 후보끼리, 또는 그 그림의 전과 후(Round 20 `review-six.png`)만 맞대 보였다. 용병끼리 같은 배율로 머리를 비교한 적이 없어 Round 12(확정), 일괄 승인, Round 20(표정)을 지나도록 걸러지지 않았다.
   스타일 문서 §4는 "between three and five heads tall"만 말하고 머리 크기의 하한이 없었다.

## 재발 방지 (넣은 것)

1. **기획** (Design/10 §2 "체형"): 【확정】 용병의 머리는 서로 비슷한 크기다. 체형의 차이는 몸통·팔다리·폭으로 내고 머리를 작게 그려 키나 덩치를 내지 않는다.
2. **스타일 문서** (`STYLE_RUNTIME.md` §4): "always with a big head: the head is as big as the heads of the characters on the reference sheet. A tall, broad or heroic build is shown by the body, never by a smaller head."
3. **소재** (`Rosters/character.csv`의 `paladin`): "a broad and heroic male paladin about four heads tall with a big head, … short sturdy legs". 그려 온 방패와 가시 철퇴, 크림색 겉옷의 금 문장도 적었다(소재가 확정 그림을 말하게).
4. **검사** (`tools/review_sheet.py --type character`): 위 띠에 지금 게임의 용병 전원(`Assets/@Art/Unit/Job`)을 후보와 한 줄로 세우고, 맨 아래에 **머리 줄**을 더했다:
   모두를 같은 배율(캔버스 2배)로 가까운 눈의 높이에 맞춰 세우고, 턱이 들어가야 할 띠(눈 아래, `FACE_BAND`)를 초록으로 칠한다. 눈의 흰자를 찾지 못하면 그렇게 적고 머리 위에서 맞춘다.
   띠는 처음에 하나(50~85px)였고, 사용자 판정(아래)으로 남녀를 나눴다: **남성 65~85px, 여성 48~62px**. 성별은 소재의 새 열 `Gender`가 말한다.
5. **절차** (Architecture/13 "승인 라운드"·"Validation"): 용병의 리뷰 시트는 용병 전원과 한 줄로 본다. 턱이 띠 밖이면 판정 전에 보고하고 원인을 고친다. 자세·표정 시험의 그림도 같은 띠로 본다(다음 공격·피격 자세에도).

띠는 지금 용병들에서 정했다: 남성 68~81(다시 그린 성기사 81, 수염 끝까지), 여성 51~56. 화면에서는 남성 약 22~28px, 여성 약 16~21px다.

## 다시 그림

- 방법: Round 20과 같다. 성기사의 확정 원본(`../20-serious-face/approved/character/paladin.raw.png`)을 기준 그림으로 그대로 붙이고, 시험 문서 `STYLE_RUNTIME-head.md`로 **비율만** 바꿨다:
  §1·§2는 지금 문서(§2 끝에 작은 머리·영웅 비율·긴 다리·다시 디자인을 금지로 더함), §3 "이 캐릭터의 머리가 우리 게임에는 너무 작다. 약 4등신의 땅딸막한 캐리커처 영웅만큼 머리를 키우고 몸통과 다리를 줄인다. 얼굴·수염·머리·갑옷·겉옷·문장·망토·허리띠·철퇴·방패·색·자세·손맛은 그대로",
  §4 구도(머리는 머리카락 위에서 수염 끝까지 키의 약 4분의 1). 소재는 고친 `Rosters/character.csv`의 `paladin`.

```text
.venv/bin/python ArtPipeline/tools/gen_image.py --type character --key paladin --name paladin_head \
  --style ArtPipeline/Archive/22-paladin-head/STYLE_RUNTIME-head.md \
  --reference ArtPipeline/Archive/20-serious-face/approved/character/paladin.raw.png
.venv/bin/python ArtPipeline/tools/review_sheet.py --type character --names paladin_head --out ArtPipeline/Archive/22-paladin-head/review-paladin.png
.venv/bin/python ArtPipeline/Archive/22-paladin-head/compare_paladin.py      # compare-paladin.png
```

- 결과(`paladin_head`, $0.027): 같은 인물이다. 얼굴 생김새(굵은 눈썹, 곧은 코, 수염, 짧은 금발), 갑옷과 어깨의 금 원판, 크림색 겉옷의 금 문장, 망토, 갈색 허리띠와 둥근 버클, 가시 철퇴, 방패, 부츠의 버클, 넓은 자세가 그대로다.
  머리가 커지고 몸통·다리가 짧아졌다. 폭은 그대로라 맞추기가 폭에 맞춰 키 665px로 세운다(전 693). 눈에서 수염 끝까지 81px로 기사(77)와 비슷하고 띠 안이다.
- 비교 시트 `compare-paladin.png`: 게임 크기의 용병 여섯 줄(지금 / 다시 그림), 머리 줄(같은 배율, 눈높이, 띠), 두 성기사의 전신. 파이프라인의 리뷰 시트 `review-paladin.png`(위 띠에 용병 전원, 아래에 머리 줄).
- 후보는 `candidates/`(생성 그대로 `paladin_head.raw.png`, 게임처럼 맞춘 `paladin_head.png`).

## 승인되면 연결

1. 이전 그림을 `before/`에(게임의 `Assets/@Art/Unit/Job/paladin.png`와 얼굴), `output/character/paladin.png`·`.raw.png`를 새 그림으로(이전 것은 `output/character/old-small-head/`).
2. `Assets/@Art/Unit/Job/paladin.png`에 복사, 확정 원본을 `approved/character/paladin.raw.png`로(다음 공격·피격 자세의 기준 그림).
3. 얼굴: `cutface.py --type character --only paladin` → `Assets/@Art/Face/Job/paladin.png`. 철퇴가 머리 위에 있어 보정값(`FaceDx` 170, `FaceDy` 20)을 새 그림으로 다시 잰다.
4. 기준 그림 시트(`References/Character/style_ref_roster.png`: 발키리·기사·대마법사)는 성기사가 없어 그대로다. 아이템 `mace`의 아이콘은 성기사의 그림을 보고 그렸지만 철퇴가 같아 그대로다.
5. 확인: 체인, 스크린샷(성기사가 서는 장면: `ko_21`·`ko_22`).

## 호출

| 무엇 | 호출 | 비용 |
|---|---|---|
| 성기사 다시 그림 `paladin_head` (1024x1024, medium) | 1 | $0.027 |

누계 180회, 약 $4.50(상한 $10). 장부 `../calls.csv`.

## 판정

- 2026-10-04 (사용자): "규칙: 머리 크기는 남성, 여성 살짝 다르게(여성이 소폭 작게) 하고 서로 비슷한 크기로 한다(비슷한 크기는 지금 정도의 범위 내라면 될듯)", "1. 연결", "2. 생성(이전 발키리 모션 생성과 같은 규칙으로 진행)".
  → 띠를 남녀로 나눴다(위 "재발 방지" 4). 새 성기사를 연결했다(아래 "연결"). 공격·피격 자세는 Round 23(`../23-motion-six/`).

## 연결 (2026-10-04, 사용자 "1. 연결". 호출 없음)

1. 이전 그림을 `before/`에(`paladin.png` 전신, `paladin_face.png` 얼굴). 파이프라인의 이전 출력은 `output/character/old-small-head/`(저장소 밖).
2. `output/character/paladin.png`·`.raw.png`를 새 그림으로 바꾸고 `Assets/@Art/Unit/Job/paladin.png`에 복사했다. 확정 원본은 `approved/character/paladin.raw.png`(공격·피격 자세의 기준 그림, Round 23).
3. 얼굴: 보정값 `FaceDx`·`FaceDy`를 170·20 → **130·68**로 다시 쟀다(맨 위의 철퇴에서 머리 가운데로. 기사처럼 머리가 틀을 채운다). `cutface.py --type character --only paladin` → `Assets/@Art/Face/Job/paladin.png`.
4. 확인: `Tools/chain.sh` — setup OK(그림 둘 다시 Import), sim OK, EditMode 616/616, PlayMode 49/49(`[Explicit]` 스크린샷 제외). `Tools/screenshots.sh` 38장(6/6):
   `ko_21`·`ko_22`에서 세드릭의 머리가 엘라·카이와 비슷한 크기로 보인다. 두 장은 `game/`.

