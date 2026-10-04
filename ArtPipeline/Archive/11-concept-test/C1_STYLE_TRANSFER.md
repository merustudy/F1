# 아트 스타일 전달서: C1 "Cute Clean Cartoon" → 전신 캐릭터

출처는 C1 프로젝트의 `ArtPipeline/STYLE_GUIDE.md`·`STYLE_RUNTIME.md`와 실제 출고 아트다(2026-10-03 기준).
C1은 머리만 든 캐릭터 심볼, 아이템, 몬스터 머리 아이콘만 만들었다. **[전신]** 표시가 붙은 규칙은 그 규칙을
전신으로 옮긴 제안값이므로 첫 시험 시트를 보고 조정한다. 설명은 한국어, 이미지 모델에 넣는 문구는 영어다.

## 0. 이 문서를 받은 에이전트가 할 일 (C1의 작업 방식)

1. 이 문서를 저장소의 스타일 문서로 저장한다(예: `Art/STYLE.md`). 생성 스크립트는 실행할 때 이 문서의 영어 블록을
   읽어 프롬프트를 조립한다. 규칙을 코드에 따로 복사해 두 벌로 만들지 않는다.
2. 이미지 생성 API는 사용자 지시 없이 호출하지 않는다(크레딧). 첫 라운드는 체형이 다른 3장 정도로 시험하고,
   사용자 승인 뒤에 양산한다.
3. 사용자가 본 후보 이미지는 덮어쓰지 않는다. 라운드마다 파일 접두어를 바꿔 보관한다.

## 1. 한 줄 정의

귀엽고 단순한 2D 카툰 게임 아트. 낙서처럼 손으로 그린 가늘고 고르지 않은 검정 잉크선, 고무처럼 처지고 기우는
둥근 덩어리, 작은 점 눈, 밝고 선명한 평면 채색. 하이라이트, 그라데이션, 질감, 배경이 없다.

## 2. 방향

| 축 | 위치 |
|---|---|
| 귀여움 ↔ 멋짐 | 귀여움 90% |
| 단순 ↔ 상세 | 단순 90% |
| 카툰 ↔ 사실 | 카툰 100% |
| 명랑 ↔ 차분 | 명랑 85% |
| 평면 ↔ 입체 | 평면 90% |
| 정돈 ↔ 낙서 | 낙서 쪽 75% (정돈된 벡터 마스코트가 아니다) |

- 피할 오독 셋: "카툰"을 애니풍 미소녀로 읽지 않는다(눈은 작은 점, 반짝임·속눈썹 없음). "귀엽다"를 장식 많음으로
  읽지 않는다(장식 하나). 광택·반사·하이라이트를 넣지 않는다.
- 프롬프트에 작품·스튜디오·작가·캐릭터 이름이나 "in the style of"를 쓰지 않는다. 원하는 느낌은 특징을 글로 풀어
  쓴다: 고무처럼 처지는 형태, 국수처럼 휘는 팔다리, 반사광 없는 점 눈, 평면 채색, 가늘고 고르지 않은 잉크선.

## 3. 형태

- 원, 타원, 캡슐, 둥근 사각형에서 출발한다. 비율은 통통하게, 물건은 실물보다 짧고 둥글게.
- 손으로 그린 느낌: 조금 처지고 기울고 불룩하며 좌우가 정확히 맞지 않는다. 완벽한 원과 기계적으로 매끈한 호를
  쓰지 않는다. 기울임은 15~20도이거나 없다.
- 실루엣은 하나로 이어진 덩어리다. 안쪽의 작은 디테일을 그리지 않는다: 머리카락 결, 귀 안쪽 선, 박음질, 리벳,
  나뭇결, 비늘 줄, 털 뭉치, 볼 홍조.
- 무늬는 큰 점이나 줄 몇 개까지다. 캔버스의 6%보다 작은 반복 무늬는 그리지 않는다.

## 4. 선

- 순수 검정 `#000000` 한 색. 완전 불투명, 닫힌 윤곽, 둥근 선 끝.
- 벡터 획이 아니라 잉크선이다. 획마다 굵기가 눈에 띄게 다르고 윤곽이 지나가며 살짝 흔들린다. 스케치선, 끊긴 선,
  이중선, 색 외곽선은 쓰지 않는다.
- 굵기(C1 출고 아트 실측): 피사체 긴 변의 약 1%(0.8~1.6%), 1024 캔버스에서 7~14px. "크기에 비해 다소 가는" 선이다.
  내부 선은 외곽선의 50~70%.
- [전신] 외곽선은 캐릭터 키의 약 1~1.5%(1536 캔버스에서 키 1300px이면 13~20px).

## 5. 색

- 한 에셋에 색상(hue) 2~4개 + 검정 선. [전신] 피부·머리·옷 때문에 꼭 필요할 때만 5개, 색 영역은 10개 이하.
- 주 채움은 채도 70~100%(60% 미만 금지), 명도 60~100%. 맞닿은 채움은 명도가 20% 이상 차이 난다.
- 금지: 탁한 색, 회색 섞인 색, 파스텔, 형광 네온, 금속 광택 그라데이션.
- 피부는 평면 Peach 또는 Tan이다(종족 컨셉이면 초록·보라·흰색도 쓴다).
- 프롬프트에는 색 이름(`sun yellow`, `grape purple`)을, 후처리와 UI 코드에는 hex를 쓴다.
- 화면 바탕색과 같은 색을 주 채움으로 쓰지 않는다(C1은 크림 `#F8F7EE` 바탕).

| Role | Base | Shadow | Light | 용도 |
|---|---|---|---|---|
| Ink Black | `#000000` | | | 선, 눈 |
| Paper Cream | `#F8F7EE` | `#EAE5D3` | `#FFFFFF` | C1 UI 패널·바탕 |
| Paper White | `#FFFFFF` | `#DCDCDC` | | 흰 채움 |
| Sun Yellow | `#F8D018` | `#C9A400` | `#FFE96A` | 왕관, 별 |
| Berry Red | `#D81818` | `#A01010` | `#F05050` | 버섯, 보석, 붉은 머리 |
| Orange | `#E88038` | `#B85A20` | `#F6A868` | 주황 머리, 불꽃 |
| Grape Purple | `#8030B8` | `#562080` | `#B87AE0` | 물약, 책, 마녀 모자 |
| Royal Blue | `#0058C0` | `#003C88` | `#4A8CE8` | 방패, 파란 옷 |
| Gem Cyan | `#18C0F8` | `#0E8CC0` | `#7FE0FF` | 수정, 얼음 |
| Leaf Green | `#68A040` | `#46702A` | `#9CD070` | 잎, 초록 머리 |
| Coin Gold | `#F2B824` | `#B8860B` | `#FFD966` | 열쇠, 반지, 테 |
| Wood Brown | `#984810` | `#602800` | `#C07030` | 상자, 활, 손잡이 |
| Cocoa Hair | `#603820` | `#3E2312` | `#8A5A3C` | 갈색 머리 |
| Bubblegum Pink | `#F090B8` | `#C86490` | `#F7BAD4` | 분홍 머리, 꽃 |
| Peach Skin | `#F8E0C8` | `#E2B896` | | 밝은 피부 |
| Tan Skin | `#E8C090` | `#C69A64` | | 짙은 피부 |
| Steel Grey | `#C8C8D0` | `#8C8C98` | `#ECECF0` | 검, 투구 |
| Char Black | `#282830` | `#181820` | `#4A4A58` | 검은 머리, 폭탄 |

Light 값은 밝은 채움색으로만 쓴다. 하이라이트용이 아니다.

## 6. 명암

- 평면이다. 채움마다 Base 하나. 형태가 안 읽힐 때만 경계가 딱 떨어지는 Shadow 톤 하나를 더한다(그 채움 면적의
  20~35% 이하).
- 하이라이트는 넣지 않는다(흰 점, 광택, 반사 모두). 광원은 좌상단이므로 Shadow는 그 반대쪽에 둔다.
- 그라데이션, 소프트 브러시, 에어브러시, 림라이트, 글로우, 바닥 그림자, 드롭 섀도, 비네트를 에셋에 굽지 않는다.
  그림자가 필요하면 게임 엔진에서 준다.

## 7. 얼굴 (C1 규칙 그대로)

- 눈: 작은 검정 점 두 개(살짝 세로로 긴 타원)를 얼굴 아래쪽에 넓게 둔다. 하나가 살짝 크거나 높다. 흰자, 반사광,
  속눈썹, 눈꺼풀은 없다.
- 입: 작은 곡선 하나, 또는 안에 평평한 혀만 있는 열린 입. 코는 없거나 점 하나. 볼터치는 없다.
- 표정: 기본은 미소나 무표정. 컨셉이 요구할 때만 한 요소(처진 눈, 찌푸린 눈썹, 가면)로 바꾼다.
- 정체는 얼굴이 아니라 머리색 + 옷 색 + 장식 실루엣으로 구분한다. 같은 로스터 안에서 이 셋이 겹치지 않게 한다.

## 8. [전신] 전신 규칙 (C1에 없던 규칙, 제안값)

- 비율: 기본 2.5등신(머리가 키의 약 40%), 범위 2~3등신. 한 로스터 안에서는 하나로 고정한다.
- 머리: 크고 둥근 덩어리 하나(C1 캐릭터 머리 그대로).
- 몸통: 콩, 캡슐, 종 모양의 단순한 덩어리 하나. 허리선, 근육, 가슴 표현이 없다.
- 팔다리: 굵기가 일정하고 부드럽게 휘는 국수 같은 팔다리(머리 폭의 약 12~18%). 팔꿈치, 무릎, 근육이 없다.
- 손은 둥근 벙어리장갑(엄지 + 덩어리)이나 손가락 셋~넷의 둥근 손, 손톱과 주름이 없다. 발은 작은 둥근 타원이나
  단순한 신발 덩어리다.
- 옷: 큰 평면 색 블록 2~3개(상의·하의, 또는 로브 하나 + 신발). 단추, 주름, 솔기, 박음질, 작은 무늬가 없다.
- 장식: 머리 장식 최대 1개 + 손에 든 소품 최대 1개(C1 머리 심볼은 장식 1개; 전신은 소품 하나를 더 허용).
- 포즈: 정면이나 얕은 3/4으로 서 있는 기본 자세. 몸 전체가 한쪽으로 살짝 기울거나 휘어도 된다. 강한 원근, 단축,
  측면, 뒤돌아보기, 격한 액션은 쓰지 않는다. 팔다리가 몸통을 가려 실루엣이 뭉개지지 않게 한다.
- 몬스터 전신: 몸은 단순한 블롭이나 콩 하나 + 정체를 알리는 특징 최대 둘(뿔, 귀, 외눈, 꼬리, 날개, 지느러미, 갓).
  뿔·귀·더듬이는 단단한 원뿔이 아니라 휘는 국수다. 귀엽고 호기심 있거나 조금 얼빠진 정도까지이며 무섭거나 고어한
  표현은 금지다(피, 상처, 부패, 사실적인 이빨 없음; 작은 송곳니 한 쌍까지).
- 구도: 세로 캔버스 1024×1536. 머리끝부터 발끝까지 캔버스 높이의 80~88%, 가로 중앙(중심 오차 3% 이내), 사방 여백,
  발이 잘리지 않는다. 발밑에 땅이나 그림자가 없다. 네 모서리 알파 0.
- 가독성: 게임에서 실제로 보일 크기(예: 키 256px, 128px)로 줄여 실제 게임 바탕 위에서 누구인지 읽히는지 본다.

## 9. 생성 설정 (C1 값)

- 모델 `gpt-image-2`(OpenAI Images API). 레퍼런스를 붙이면 `images.edit(image=<레퍼런스 1장>)`, 없으면
  `images.generate()`. 레퍼런스는 요청당 1장이다.
- `quality="medium"`, `background="transparent"`. 크기는 C1이 `1024x1024`, [전신]은 `1024x1536`.
- C1의 스타일 레퍼런스는 몬스터 머리 11칸 시트(1168×784)다. 같은 Mac이면
  `/Users/funitup/Projects/C1/ArtPipeline/References/Monsters/monster_icon_master.png`를 이 프로젝트의 레퍼런스
  폴더로 복사해 쓴다. 승인된 첫 전신 시트를 레퍼런스로 바꿀지는 사용자가 정한다(사용자 승인 없이 생성물을
  레퍼런스로 올리지 않는다).
- 모델은 크기 지시를 잘 지키지 않는다(C1에서 "70~80%"를 주었는데 94%로 꽉 채워 왔다). 후처리로 피사체 상자(알파 > 8)를
  재서 목표 비율로 다시 배치하고, 네 모서리 알파 0, 중심 오차, 외곽선이 순수 검정 근처인지를 자동 검사한다.
- 생성물 옆에 같은 이름의 `.json`을 남긴다: asset, type, model, prompt 전문, size, quality, background, references,
  스타일 문서 commit, created_at.
- API 키는 환경변수가 아니라 OS 키체인에서 읽어 클라이언트에만 넘긴다(C1 방식).

## 10. 프롬프트 조립

순서: [1] QUICK RULE → [2] SUBJECT → [3] TYPE → [4] AVOID → [5] REFERENCE(레퍼런스를 붙일 때만). 블록 사이는 빈 줄.
한 프롬프트 안에 한국어를 섞지 않는다.

[1] QUICK RULE (C1 원문, Readability 한 줄만 [전신]용으로 바꿈)

```text
Style: cute simple 2D cartoon game art, cheerful and friendly, hand drawn and a
little wonky rather than a clean vector look.
Shapes: big simple rounded shapes, low detail, chubby friendly proportions, front view,
drawn loose, leaning or sagging a little and never mirror-perfect.
Linework: thin pure black outline on every shape, inked by hand so its weight varies
visibly and the contour wavers as it goes, closed contours, no sketchy or broken lines.
Color: flat solid fills in bright clear saturated colors, two to four colors per
subject. No gradients, no muted or pastel tones, no neon.
Shading: flat color, no highlight at all, and at most one hard-edged darker tone where
a form would be unreadable without it. Light from the upper left.
Readability: one subject, centered, big and simple enough to stay clear when the whole
figure is 256 pixels tall.
Background: fully transparent. No scenery, no frame, no grid, no drop shadow, no glow.
Never: realistic, 3D render, painterly, soft brush shading, texture, grain, text,
watermark.
```

[2] SUBJECT

```text
Subject: <SUBJECT>. Draw it as a single full-body character.
```

[3] TYPE: 캐릭터 전신

```text
Composition: one cute cartoon character shown full body, from the top of the head to
the feet, standing in a relaxed neutral pose, front view or a shallow three-quarter.
Chubby proportions about two and a half heads tall: one big round head, a small simple
bean-shaped body, and thin bendy noodle arms and legs of even thickness with no elbows,
knees or muscles. Round mitten hands with no fingernails, small rounded feet or plain
boots. Draw it loose, wonky and a little silly like a quick doodle, nothing like a
polished vector mascot: soft rubbery shapes that lean and sag, the whole figure leaning
a little to one side, no mirror symmetry anywhere, every contour wavering slightly as
it goes. Two small black dot eyes set wide and low on the face, one a touch bigger or
higher than the other, no white or catchlight in the eyes, no eyelashes, a tiny curved
mouth, no nose or a single dot, no blush. Clothes as two or three big flat color
blocks with no buttons, folds, seams, stitching or small patterns. At most one
headwear or accessory and at most one held item. Draw nothing small inside the forms:
no strand lines in the hair, no inner ear lines, no wrinkles. Flat fills behind a thin
black ink outline that is visibly uneven in weight, no gloss, no gradient, no soft
shading, no white highlight, and a darker tone only where a form would be unreadable
without one. The figure is centered and fills 80 to 88 percent of the canvas height
with clear margin on all four sides, the feet not cropped, nothing under the feet.
Never add text, background, ground, frame or a second character. Output a transparent
PNG with all four corner pixels at alpha 0.
```

[3] TYPE: 몬스터 전신

```text
Composition: one cute fantasy monster shown full body, front view or a shallow
three-quarter. One simple readable mass: a plain blob or bean body, short stubby legs
or none, and at most two features that name it, such as horns, ears, a single eye, a
tail, small wings, a fin or a cap. Draw it loose, wonky and a little silly, nothing
like a polished vector mascot: soft rubbery shapes that bend and sag, the whole body
leaning or bulging to one side, any horn, ear, arm, leg or antenna a thin bendy noodle
rather than a solid tapered cone, no mirror symmetry anywhere. Do not add fur tufts,
scale rows, feather layers, inner ear lines, cheek blush or any other small detail.
Two small solid black dot eyes set wide apart and low on the head, one a touch bigger
or higher than the other, no catchlight, no eyelashes, no eyelids; one plain mouth
that is a short curved line or an open shape with only a flat tongue inside; one pair
of small fangs at most. Friendly, curious or a bit dopey, never frightening or gory:
no blood, no wounds, no rot, no realistic teeth. Flat bright fills behind a thin black
ink outline that is visibly uneven in weight, no gloss, no gradient, no shading, no
white highlight; at most one simple pattern of a few large spots or stripes. The
monster is centered and fills 80 to 88 percent of the canvas height with clear margin
on all four sides, nothing cropped, nothing under it. Never add a background, scenery,
a ground shadow, a frame, a grid, text or a second creature. Output a transparent PNG
with all four corner pixels at alpha 0.
```

[4] AVOID (C1 원문 + [전신] 다섯 낱말)

```text
avoid: realistic, photorealistic, 3D render, CGI, painterly, watercolor, oil painting,
soft shading, gradients, airbrush, texture, grain, noise, muted colors, pastel, neon
glow, chrome, sparkles, drop shadow, ground shadow, scenery, background, grid, frame,
border box, text, letters, numbers, watermark, cropped subject, extra objects, tiny
details, realistic anatomy, muscles, detailed fingers, dynamic action pose,
foreshortening
```

[5] REFERENCE (몬스터 시트를 붙일 때)

```text
Use the attached image only as a style reference for line weight, shape
simplification, color range, shading amount and rendering. It is a sheet of separate
monster heads, not a scene: do not copy any creature shown in it, do not reproduce its
grid, its cell borders or its white background, and do not draw only a head. Draw the
described subject as a new, original, standalone full-body figure on a transparent
background.
```

## 11. SUBJECT 쓰는 법

- 영어 3~12단어, 명사 중심, 형용사 2개 이하. 색은 팔레트 이름으로 쓴다.
- 캐릭터: 머리색 + 옷 색 + 장식이나 소품 하나. 몬스터: 생물 종류 + 정체를 알리는 특징 한두 개.
- 좋음: `a girl with curly orange hair, a grape purple robe and a pointed witch hat`
- 좋음: `a stout old man with a white beard, a royal blue tunic and a wooden staff`
- 좋음: `a one-eyed purple blob monster with two small tusks and stubby legs`
- 나쁨: `epic legendary hero in a dynamic battle pose, highly detailed, 8k` (장식·원근·디테일이 생긴다), 고유명사·작가명·작품명.

## 12. 승인 전 점검 (C1 기준)

- 실제 표시 크기와 실제 바탕 위에서 본다(C1은 크림 `#F8F7EE`와 흰색 위에서 128px·66px, 몬스터 아이콘은 96px).
- 흰색이나 옅은 소재가 밝은 바탕에 묻으면 하이라이트를 더하지 말고 진한 앵커(가죽 끈, 진한 테)를 넣는다.
  불투명 픽셀의 평균 밝기를 바탕 밝기(크림 244)와 비교한다(C1 설인 가죽: 219 → 끈을 넣어 190).
- 같은 로스터 안에서 주 색상과 실루엣 특징이 겹치지 않는지 본다.
- 자동 검사: 네 모서리 알파 0, 피사체 크기 비율, 중심 오차 3% 이내, 외곽선이 순수 검정 근처.

## 부록: C1의 원래 타입 (머리 아이콘·아이템이 필요할 때)

- `character_symbol`: 머리만(얼굴 + 머리카락 + 장식 하나, 턱에서 자름, 목·몸·손 없음), 긴 변 70~80%, 사방 여백 10%.
- `item_symbol`: 아이템 하나만 중앙, 얼굴·손·캐릭터 없음, 큰 특징 하나, 색 영역 7개 이하, 긴 변 70~80%.
- `monster_icon`: 몬스터 머리(목이나 어깨 조금까지), 특징 최대 둘, 긴 변 75~90%.
- UI 패널: 크림 `#F8F7EE` 둥근 사각형 + 검정 잉크선(짧은 변의 2.5~3%), 모서리 반경 짧은 변의 10~12%, 안쪽은 완전
  평면(9-slice), 글자·아이콘을 굽지 않는다. 눌림은 채움 `#EAE5D3`, 비활성은 외곽선 `#8C8C98`(후처리).
