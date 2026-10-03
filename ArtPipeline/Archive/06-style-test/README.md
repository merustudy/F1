# Round 06 — 그림체 시험 (`character`)

새 그림체를 시험하는 라운드다. **지금의 그림체는 바꾸지 않는다.** 시험 세트(같은 형식의 스타일 문서, 소재, 기준 그림)를 이 폴더 아래에 따로 두고
`gen_image.py`의 `--style`, `--roster`, `--reference`로 돌린다(`13_ART_PIPELINE.md` "스타일 문서"). 호출은 전부 `gpt-image-2.5-sunburst`, `1024x1024`, `medium`,
투명 배경, `--extra` 없음. 장부는 `../calls.csv`.

## 사용자 지시 (2026-10-03)

1. "현재 그림체 일단 백업 해놓고 새로운 그림체를 테스트 해보고 싶어. 현재 업로드한 그림을 테스트 버전으로 놓고 이를 기준으로 직업 2개 이미지 생성해줘." -> 시험 1.
2. "이번 테스트 세트는 폐기. 다른 테스트 버전 준비. 백업 원본이미지에서 어두운 느낌 부여(다크 판타지) 테스트 이미지 1개 생성." -> 시험 1 폐기, 시험 2.

## 백업 (`before/`)

지금 그림체의 파일을 복사했다. 원본은 제자리에 그대로 있고 시험은 그것을 바꾸지 않는다. Git 커밋 `32e47fe`에도 같은 내용이 있다.

| 파일 | 원본 |
|---|---|
| `before/STYLE_RUNTIME.md` | `ArtPipeline/STYLE_RUNTIME.md` (모든 타입의 규칙) |
| `before/character.csv` | `ArtPipeline/Rosters/character.csv` |
| `before/style_ref_mercenary.jpg` | `ArtPipeline/References/Character/style_ref_mercenary.jpg` (시험 2의 기준 그림) |

승인된 그림(`01-characters/approved`, `02-enemies/approved` 등)과 게임의 그림(`Assets/@Art`)은 시험과 무관하게 그대로다.

## 시험 1 — chibi (2026-10-03) — 폐기

사용자가 올린 기준 그림(약 2.5등신의 치비, 굵은 검은 외곽선, 맑고 밝은 평면 색, 눈동자와 하이라이트가 있는 큰 눈)으로 `character`만 있는 시험용 스타일 문서를 쓰고,
지금 소재에서 그림체에 딸린 문구만 뺀 소재로 **기사(`knight_chibi`)와 대마법사(`archmage_chibi`)**를 그렸다(권장안: 첫 라운드의 시험 2장과 같은 직업). 호출 2회, $0.060, 경고 없음.
리뷰 시트에서 본 것: 기사의 얼굴이 기준 그림의 소년과 닮음(소재의 "짧은 갈색 머리"), 여성의 체형이 드러나지 않음, 화면 300px 자리에서 머리 약 120px, 외곽선이 검정, 밝은 색과 어두운 배경·UI의 대비.

**판정 (사용자, 같은 날): "이번 테스트 세트는 폐기".** 시험 세트(`chibi/`: 올린 그림, 스타일 문서, 소재), 후보 둘, 리뷰 시트를 지웠다. 장부의 두 줄은 남는다.

## 시험 2 — dark (2026-10-03) — 판정 대기

백업한 원본 기준 그림의 캐릭터(발키리)를 **그리는 방식(선, 비율, 평면 채색, 얼굴)은 그대로 두고 어두운 느낌(다크 판타지)만 입혀** 다시 그렸다.
결과가 마음에 들면 그것이 새 그림체의 기준 그림 후보다.

| 파일 | 내용 |
|---|---|
| `dark/STYLE_RUNTIME.md` | 시험용 스타일 문서. `character`만 있다. §1에 어두운 색(근검정, 숯, 찬 회색, 마른 피 빛 붉은색, 크림 대신 뼈색·재색), 넓고 경계가 분명한 그림자(눈두덩, 머리 밑, 망토 안, 다리), 낡음(긁힘, 해진 천, 그을린 쇠, 때 묻은 붕대, 지친 얼굴. 피와 상처는 금지). §3은 "같은 캐릭터를 그대로 다시 그리되 색·그림자·낡음·분위기만 바꾼다" |
| `dark/character.csv` | 소재는 `valkyrie` 하나: 원본의 캐릭터를 어두운 색으로 적은 것. `Height` 90, `Flip` false |
| 기준 그림 | `before/style_ref_mercenary.jpg`를 그대로 붙였다(왼쪽을 본다). 그림도 왼쪽을 보므로 원본과 같은 방향으로 견준다. 게임에 넣을 때는 뒤집는다(`--refit`, 호출 없음) |
| `dark/valkyrie_dark.raw.png`, `valkyrie_dark.png` | 생성한 원본과 맞춘 그림(672x896) |

돌린 명령 (ArtPipeline 에서):

```text
.venv/bin/python tools/gen_image.py --type character --key valkyrie --name valkyrie_dark --style Archive/06-style-test/dark/STYLE_RUNTIME.md --roster Archive/06-style-test/dark/character.csv --reference Archive/06-style-test/before/style_ref_mercenary.jpg
.venv/bin/python tools/review_sheet.py --type character --names valkyrie_dark,valkyrie --reference Archive/06-style-test/before/style_ref_mercenary.jpg --out Archive/06-style-test/review-dark-vs-original.png
```

| Key | 산출물 | 추정 비용 | 결과 |
|---|---|---|---|
| `valkyrie` | `output/character/valkyrie_dark.png` (보관: `dark/`) | $0.031 | 같은 캐릭터, 같은 자세와 방향, 같은 비율과 얼굴(눈동자 없는 흰 눈). 머리는 재빛 금발, 망토는 마른 피 빛 붉은색에 그을린 회색 털과 해진 가장자리, 상의는 뼈색, 치마는 검정, 도끼는 검게 그을린 쇠, 붕대는 때가 묻었다. 경고 없음 |

리뷰 시트 `review-dark-vs-original.png`: 위 띠는 게임의 표시 크기(300px)로 게임 배경색 위에, 아래 띠는 원본 기준 그림, 시험 그림, 지금 게임의 발키리(원본을 오려 내 뒤집은 것).

### 판정할 때 볼 것

1. **어두운 느낌의 정도.** 색은 뚜렷이 어두워졌고 장비는 낡았다. 더 어둡게(또는 덜) 가려면 §1의 색과 그림자 문구를 고쳐 1회 재생성한다.
2. **면에 얼룩과 질감이 생겼다.** "낡고 때 묻은"을 모델이 얼룩진 면으로 풀었다. 금지 목록의 texture/grain과 어긋나고, 지금 그림체의 평면 채색과도 다르다.
   이 질감이 다크 판타지 느낌에 보탬이 되는지, 아니면 평면 채색을 지키고 색만 어둡게 할지가 판정 거리다(후자면 재생성에서 얼룩을 더 강하게 금지한다).
3. **치마의 금색 문양이 사라졌다.** 소재에 문양을 적지 않아서다. 필요하면 소재에 더한다.
4. **게임 배경 위에서 덜 도드라진다.** 남색 배경(리뷰 시트 위 띠)과 어두운 광산 배경 위에서 지금 그림보다 가라앉는다. 채택하면 명패·틀의 색이나 배경의 밝기를 함께 본다.
5. **채택 시 범위.** 선·비율·얼굴은 같으므로 지금 스타일 문서의 색·그림자·분위기 문구만 바꾸고, 이 그림을 새 기준 그림으로 삼아 직업 6(발키리는 이 그림)과 적 5를 다시 그린다.
   아이템 21, UI 8, 배경 1은 지금도 낮은 채도라 판정 때 정한다(어두운 쪽으로 맞추면 다시 그린다).

## 판정

- 시험 1 (chibi): **폐기** (2026-10-03, 사용자).
- 시험 2 (dark): (대기)

## 되돌리기와 채택

- 시험을 버리면: 바꾼 것이 없으므로 되돌릴 것도 없다. 시험 세트를 지우고 README에 기록만 남긴다. `output/character/*_dark.png`를 치운다.
- 채택하면: `Docs/Design/10_Art_Direction.md`의 G9를 먼저 고친다 -> 확정된 그림을 `References/Character/`의 새 기준 그림으로 두고 `ArtPipeline/STYLE_RUNTIME.md`의
  색·그림자·분위기 문구를 고친다(이전 문서는 `Archive/<style-name>/`. `13_ART_PIPELINE.md` "스타일 문서") -> 종류마다 승인 라운드 -> 배선.

## 호출

| 때 | 호출 | 추정 비용 |
|---|---|---|
| 시험 1 (`knight_chibi`, `archmage_chibi`) — 폐기 | 2 | $0.060 |
| 시험 2 (`valkyrie_dark`) | 1 | $0.031 |
| 누계 (모든 라운드) | 50 | 약 $1.21 (상한 $10) |
