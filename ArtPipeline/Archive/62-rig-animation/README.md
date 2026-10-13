# Round 62 — 움직임: 스파인·유니티 애니메이션, 리그와 무기 바꾸기, 클로드 코드로 되는 것 (2026-10-13)

검토의 기록이다. 호출 없음, 목업 없음, 코드·데이터·그림 변경 없음. G19(지금 그림체 유지) 바로 뒤의 질문이다.

## 지시

사용자: "오리지날 스타일 유지하는 걸로. 추가 질문사항 — 스파인이나 유니티 애니메이션을 통해 움직임 재현 가능한지 검토 / 리그 애니메이션, 무기 이미지 바꾸기 등 가능한지 검토" → "클로드 코드를 통해 해당 것들이 구현 가능한지".

## 방법

- 조사 둘(웹, 2026-10-13): ① Unity 2D Animation·PSD Importer·2D IK(Unity 6000.3), Spine의 2026년 라이선스와 spine-unity(SkeletonGraphic), 대안(DragonBones/LoongBones, Rive, Live2D) ② 한 장 그림에서 자동 리깅·부위 나누기·프레임 생성(Animated Drawings, SpineFactory, Qwen-Image-Layered, See-through, anysplit, 영상 모델), 인디의 컷아웃 파이프라인과 품, 무기 바꾸기의 전제, 레퍼런스 게임의 방식.
- 우리 쪽 사실: 전투 유닛은 **uGUI Canvas의 `Image`**(`FigureView`) 위의 한 장 그림(672x896)이고, 자세는 그림 바꿔치기(대기·공격·피격 — `fit_pose.py`가 대기 그림에 맞춰 세움) + `BattleUnitView.Update`의 돌진·밀림·걷기·숨쉬기·맥동이다. Tween 패키지 없음, 2D Animation 패키지 없음(`com.unity.2d.sprite`만). 그림 자리 Prefab은 코드(`UiPrefabSetup.BuildFigure`)가 만든다. 무기는 그림에 **그려 넣어져** 있다(아이템 아이콘은 따로, 격자용으로 눕힌 그림).

## 1. 사실 (조사)

### 리그는 "부위가 나뉜 그림"을 전제한다

- Unity: 캐릭터는 "팔다리와 부위를 **층으로 나눈** 기본 자세"로 준비하고(.psb, .psd는 Importer를 바꿔 씀) PSD Importer가 층 → 스프라이트 → 리그 Prefab을 만든다. 한 장 그림에도 뼈·메시를 넣을 수 있지만(Skinning Editor: Auto Geometry·Auto Weights), 한 메시는 부위의 **앞뒤 순서를 바꾸지 못하고 겹친 뒤의 픽셀이 없다** → 흔들림·숨쉬기·기울임·찌그러짐·짧은 돌진까지이고, 팔이 몸을 가로지르는 휘두르기는 안 된다([Preparing artwork](https://docs.unity3d.com/Packages/com.unity.2d.animation@13.0/manual/PreparingArtwork.html), [Sprite Skin](https://docs.unity3d.com/Packages/com.unity.2d.animation@13.0/manual/SpriteSkin.html), Unity 샘플 Boris·Plunkah).
- Spine: 자르는 지침 — "움직이는 부위마다 한 층, **가려진 부분까지 다 그림**(머리 뒤의 머리카락 전부), 관절은 둥글게, 팔다리는 곧게"([Spine 블로그](https://en.esotericsoftware.com/blog/How-to-cut-your-assets-for-animation)). 완성된 한 장 그림은 **잘라 내고 가려진 곳을 그려 넣어야** 한다([GameFromScratch](https://gamefromscratch.com/an-introduction-to-spine-a-2d-bone-based-animation-system/)).
- 품(출처는 의견·견적): 초보자의 단순 리그 2~5시간, 복잡하면 며칠([drawphics](https://drawphics.com/how-to-rig-a-2d-character/)); 프리랜서 리그 4~8시간·시급 $35, 리그 + 걷기·뛰기 $200([Spine 포럼](https://esotericsoftware.com/forum/d/13065), [포럼](https://esotericsoftware.com/forum/d/2557)); **애니메이션 시간이 리깅보다 훨씬 크다**(걷기 8시간, 특수 동작 셋 24시간: [포럼](https://it.esotericsoftware.com/forum/d/22986-spine-animation)); 한 인디는 일주일 배운 뒤 캐릭터 하나에 하루([IndieDB](https://www.indiedb.com/features/animating-2d-characters-with-a-3d-animation-workflow)).

### 한 장 그림을 자동으로 리그로 바꾸는 도구는 아직 "초안"이다

- Meta Animated Drawings(2023): 사람 모양 한 장 → 관절 추정 → 모션 캡처(BVH) 재생. 출력은 **영상·GIF**(부위·리그 아님), 든 물건과 옷이 자세 추정을 깨뜨리고, 저장소는 2025-09 보관(읽기 전용)([논문](https://arxiv.org/abs/2303.12741), [README](https://github.com/facebookresearch/AnimatedDrawings)).
- 층 나누기 AI: Qwen-Image-Layered(2025-12, [arXiv](https://arxiv.org/abs/2512.15603)), See-through(2026-02, 애니메 캐릭터 → PSD 23층; 실사용기 — 96GB GPU, 옆머리가 뒷머리에 합쳐지고 신발이 빠짐, "PSD 초안 생성기"([lilting.ch](https://lilting.ch/en/articles/see-through-anime-layer-decomposition))), anysplit(제작사 주장, 독립 평가 없음), Layer.ai(머리·몸통·팔다리만, 뼈 없음).
- 자동 리그: SpineFactory(비공개 베타), spine-animation-ai(비상업 라이선스), Esoteric(Spine) 자체는 없음. Unity도 평면 PNG의 자동 리그는 없다.
- 프레임 생성: 이미지 모델의 스프라이트 시트는 자세 반복·좌우 혼동([OpenAI 포럼](https://community.openai.com/t/developing-sprite-sheets-with-gpt-image-2/1379831)), 영상 모델은 몇 초 뒤 흐트러짐·떨림([GitHub](https://github.com/3DStreet/3dstreet/issues/1934)). 출하한 인디 사례는 찾지 못함.

### 무기 바꾸기는 리그의 기본 기능이고, 전제는 "무기가 별도 부위"다

- Unity: Sprite Library의 카테고리(예: Weapon)와 라벨(Saber/Bat/Blade/Axe), Sprite Resolver가 런타임에 바꿈. 바꾸는 스프라이트는 **같은 뼈 구조**여야 함([Sprite Swap](https://docs.unity3d.com/Packages/com.unity.2d.animation@13.0/manual/SpriteSwapIntro.html), [SL-Asset](https://docs.unity3d.com/Packages/com.unity.2d.animation@13.0/manual/SL-Asset.html)).
- Spine: 손 뼈의 슬롯에 attachment, `SetAttachment`/스킨; 양손 무기는 무기 뼈 + 손의 transform constraint + IK(Pro)([블로그](https://eu.esotericsoftware.com/blog/Holding-a-two-handed-weapon)).
- 전제(두 도구 공통): 무기가 **제 층**이고, **쥔 손이 무기와 겹치지 않거나 손도 별도 부위**이며, 무기마다 **같은 그립 피벗**. 우리 그림은 무기가 손과 함께 그려져 있어 **잘라 내고 손을 다시 그리거나, 빈손으로 다시 생성**해야 한다.

### 우리 무대(uGUI)와 도구

| | Unity 2D Animation 13.0.6 (6000.3 동봉, 미설치) | Spine 4.3 + spine-unity |
|---|---|---|
| Canvas(uGUI)에서 | **안 됨** — Sprite Skin은 Sprite Renderer 전용. 우회: 유닛마다 카메라 → RenderTexture → RawImage(비쌈), 또는 무대를 월드 스프라이트로 바꾸고 Screen Space–Camera 캔버스의 Plane Distance로 겹침 정리([Unity 토론](https://discussions.unity.com/t/2d-animation-in-ui/940219), [Canvas](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/class-Canvas.html)) | **됨** — `SkeletonGraphic`(MaskableGraphic, 기본은 텍스처 하나, 전용 셰이더)([main components](https://esotericsoftware.com/spine-unity-main-components)) |
| 값 | 무료. 패키지 둘 설치(2D Animation, PSD Importer — Authority 결정) | Essential $69(메시 변형 없음) / Professional $379, 1인 1라이선스, 런타임도 라이선스 필요([구매](https://esotericsoftware.com/spine-purchase), [라이선스](https://esotericsoftware.com/spine-editor-license)) |
| 메시 변형·IK | 있음(Auto Geometry·Auto Weights, Limb/CCD/FABRIK) | Pro에만 메시. IK·constraint |
| 부위 들이기 | .psb(.psd는 Importer 바꿔), 낱장 PNG는 수작업(일괄 리깅 없음) | PSD 직접 임포트(4.2.10부터), 태그 층 이름 |
| 편집기 작업 | Skinning Editor에서 뼈·가중치(GUI) | 편집기 전체(GUI) |
| 대안 | DragonBones → LoongBones(유료 전환, 혼란), Rive Unity 0.5.x(RenderTexture 기반, 전투 캐릭터 사례 없음), Live2D(표정·물리에 강함, 전투 적합성 미확인) | |

레퍼런스: 다키스트 던전은 Spine 2.1.27의 컷아웃(뼈 적음, "뻣뻣하지만 표현력 있음"), 슬레이 더 스파이어 1도 Spine이지만 "애니메이션은 신경 쓰지 않기로"했고 2는 전담 애니메이터를 뒀다([PCGamesN](https://www.pcgamesn.com/slay-the-spire-2/art-direction)). 백팩 배틀즈는 방식을 밝힌 자료가 없다.

## 2. 클로드 코드로 되는 것 / 사람 손이 드는 것

| 일 | 클로드 코드 | 사람 |
|---|---|---|
| 부위 계층·절차 애니메이션(대기 흔들림·예비 동작·휘두르기·피격·쓰러짐)을 **코드로**(uGUI, 패키지 없음, `Update`의 Frame 시간) | **됨** — 지금 `BattleUnitView`·`FigureView`의 틀 그대로. 클립은 코드·데이터(키프레임 표) | 움직임의 판정(MP4 목업) |
| 무기 바꾸기의 로직(데이터 → 든 무기 그림 + 그립 피벗 + 각도, 양손 자리) | **됨** | 무기마다 든 그림의 판정 |
| Prefab·Builder·데이터 열·Test·스크린샷 검사 | **됨**(`UiPrefabSetup`, CSV, 체인) | — |
| Unity 2D Animation: 패키지 설치, PSB 임포트 설정, Sprite Library·Resolver, AnimationClip을 스크립트로 생성, 무대 구조 변경 | **됨**(에디터 스크립트, batch) | **Skinning Editor의 뼈·가중치**(GUI, 20~30분/캐릭터), 패키지·구조 변경의 승인 |
| Spine | 런타임 배선·교체 로직·(원하면) skeleton JSON을 스크립트로 생성 | **편집기 작업 전부**(리깅·애니메이션), 라이선스 구매 |
| **부위 나누기와 가려진 곳 그리기** | 반자동 도구는 만들 수 있다(알파·색으로 무기 마스크 뽑기, 손 마스크 틀, 자른 조각의 피벗 검사 시트) — 결과는 초안 | **다듬기·그려 넣기**(2~5시간/캐릭터 × 유닛 11) 또는 외주, 또는 **빈손 재생성**의 승인 라운드(호출) |
| 자동 리깅 AI | 지금은 쓰지 않음(초안 수준·베타·비상업 라이선스) | — |

요약: **코드 쪽은 전부 된다. 안 되는 것은 GUI 편집기(Skinning Editor·Spine)와 그림의 손질이다.** 그래서 "클로드 코드만으로 가장 멀리 가는 길"은 패키지 없는 코드 리그(안 2)와 무기 층(안 1)이다.

## 3. 선택지

| 안 | 무엇 | 움직임 | 무기 바꾸기 | 사람 손 | 클로드 코드 |
|---|---|---|---|---|---|
| **0 지금 틀 보강** | 한 장 그림에 예비 동작(찌그러짐·기울임), 휘두름의 잔상·스미어, 피격의 비틀림을 코드로 더함 | 조금(fake. "obvious look"의 위험) | 없음 | 없음 | 전부 |
| **1 무기 층 + 손 피벗** | 자세 그림(대기·공격·피격 × 11)을 **빈손**으로 두고(재생성 또는 손질), **든 무기 그림**(무기마다 하나, 같은 그립 피벗)을 자세마다의 손 자리·각도 데이터로 얹는다. 쥔 주먹은 자세에서 오려 무기 위에 덮는다(몸 → 무기 → 주먹) | 지금과 같음(자세 바꿔치기) | **됨**(기본 무기의 담금질·단계, 보드의 무기 등) | 빈손 자세의 승인 라운드(호출 약 33 + 무기 20 ≈ $1), 주먹 마스크 확인 | 코드·데이터·Builder·검사·목업 |
| **2 코드 컷아웃 리그** | 부위 5~7(몸통+머리, 뒤팔, 앞 위팔, 앞 아래팔+손, 무기, 망토·머리카락)을 uGUI 계층으로, 절차 애니메이션(대기·예비·휘두름·피격·쓰러짐) + 무기 부위 교체. 패키지 없음 | **종이 인형**(다키스트 던전 1에 가까움). 메시 변형 없음. 그린 공격 자세의 극적 실루엣(뻗은 검, 날리는 망토)은 리그로 다시 잡아야 한다 | 됨 | **부위 나누기 + 가려진 곳**(2~5시간/캐릭터 × 11, 또는 외주, 또는 부위 시트 생성 실험) | 코드 전부, 클립 전부(절차), MP4 목업 |
| 3 Unity 2D Animation | 2의 부위에 뼈·메시·IK, Sprite Swap | 부드러운 변형 | 됨 | 2의 손 + Skinning Editor(20~30분/캐릭터) + **무대를 SpriteRenderer로 바꾸거나 RenderTexture**(12_UI 구조 변경) + 패키지 승인 | 에디터 스크립트·클립 생성·배선 |
| 4 Spine | 편집기에서 리깅·애니메이션, `SkeletonGraphic`으로 Canvas 그대로 | 가장 좋음(Pro면 메시) | 됨(attachment) | 2의 손 + **편집기 학습·작업 전부** + $69/$379 | 런타임 배선·교체 로직 |

- 1은 2·3·4와 **겹치지 않고 먼저** 할 수 있다(무기가 별도 층이 되는 것은 리그의 전제이기도 하다). 1을 하면 2~4로 갈 때 무기 부위는 이미 있다.
- 2·3·4의 공통 전제는 **부위 나누기**다. 누가 어떻게 나누느냐가 진짜 결정이다(아래 판정 2).
- 0은 어느 안과도 함께 간다.

## 4. 판정 (냉정하게)

1. **스파인·유니티로 움직임을 재현할 수 있다. 단, 조건이 둘이다**: 그림을 부위로 나누는 사람의 손(한 장에 메시만 넣는 것은 숨쉬기·기울임까지), 그리고 우리 무대가 uGUI라 Unity 2D Animation은 무대 구조를 바꿔야 하고 Spine은 라이선스가 든다. 둘 다 편집기의 GUI 작업이 있어 클로드 코드가 대신하지 못한다.
2. **리그와 무기 바꾸기는 표준 기능이고, 우리 그림의 걸림돌은 "무기가 손과 함께 그려져 있다"는 것**이다. 그 걸림돌은 리그 없이도 풀 수 있다(안 1).
3. **클로드 코드로**: 코드·데이터·Builder·검사·목업은 전부, 편집기와 그림 손질은 아니다. 패키지 없이 uGUI 안에서 종이 인형 리그(안 2)까지는 코드만으로 간다.
4. 리그의 값은 "움직임"보다 **"애니메이션을 만드는 시간"**이 크다(출처). 절차 애니메이션(코드)은 그 시간을 줄이지만 손맛은 사람의 키프레임보다 못하다. 다키스트 던전처럼 뻣뻣해도 FX·카메라·소리로 받치는 것이 현실적인 목표다.
5. 유닛 11, 자세 33장의 지금 세트는 "그린 자세"의 품질이 높다. 리그로 바꾸면 그 자세는 **참고 자세**가 되고 리그가 그것을 다시 만들어야 한다 — 바꾸는 것이지 더하는 것이 아니다.

## 5. 권장

**안 1(무기 층 + 손 피벗)을 먼저, 안 0(지금 틀 보강)을 함께. 리그(안 2 → 필요하면 3)는 "기사 한 명 시험"으로 결정한다.**

- 안 1은 사용자가 물은 "무기 이미지 바꾸기"를 가장 싸게 주고(호출 ≈ $1, 코드는 클로드), 지금의 자세 바꿔치기와 호환되며, 뒤의 리그에도 그대로 쓰인다.
- 리그는 부위 나누기의 손이 들어가므로, **기사 하나**를 시험으로: 부위를 나누고(판정 2의 방법으로) 코드 리그로 대기·휘두름·피격을 만들어 MP4로 지금 자세와 나란히 본다. 만족하면 나머지 열 유닛, 메시 변형이 아쉬우면 안 3.
- Spine은 사용자가 **편집기에서 직접 애니메이션을 만들 뜻**이 있을 때의 길이다(그때는 Canvas에 바로 들어가고 $69면 컷아웃은 된다).

## 6. 판정할 것 (모두 권장안을 적었다)

| # | 질문 | 선택지 | 권장 |
|---|---|---|---|
| 1 | 먼저 할 것 | a 안 1(무기 층) / b 안 2(코드 리그) 시험 / c 안 3 / d 안 4 Spine / e 안 0만 / f 보류 | **a + 안 0**, 그다음 b 시험 |
| 2 | 부위 나누기의 방법(리그 시험에서) | a 사용자가 Krita 등으로 자르고 가려진 곳을 그림(기사 2~5시간) / b 외주 / c 클로드의 반자동 마스크 초안 + 사용자 다듬기 / d 이미지 모델로 부위 시트 생성(호출, 신뢰도 낮음) | **c**(초안은 코드, 다듬기는 사람). d는 실험으로만 |
| 3 | 안 1의 빈손 자세 | a 이미지 모델로 재생성(33장, 승인 라운드) / b 지금 그림에서 무기를 지우고 손을 그려 넣음(사람) | **a** — 호출 약 33회 ≈ $1. 지시가 있을 때 |
| 4 | 든 무기 그림 | a 무기마다 하나(같은 그립 피벗·같은 각도 규칙, 20장) / b 직업마다 따로 | **a** |
| 5 | 리그의 도구(시험 뒤) | a 코드 리그(uGUI, 패키지 없음) / b Unity 2D Animation(무대 구조 변경) / c Spine | **a**로 시작. b는 변형이 꼭 필요할 때, c는 직접 애니메이션할 때 |
| 6 | 시점 | a 직접 플레이·16단계 마감 뒤 / b 지금 | **a**. 안 1의 데이터·코드 설계는 그 사이에 할 수 있다 |

- 승인되면: Design/10 §5(무기 층·손 피벗, 리그의 방향), Architecture/12("유닛의 그림"에 무기 층, 리그 시험), 13(빈손 자세·든 무기 타입과 후처리), 05(데이터 열: 자세의 손 자리·각도, 아이템의 든 무기 그림), Roadmap 결정 관문 **G20**(움직임의 방식).
- 기록: 이 README, Roadmap, `b16-tuning.md`.

## 추가 질문 (2026-10-13): "이미지 호출이나 편집으로 네가 부위를 분리해 주고 사람 손 없이 진행이 불가능한가? 스파인 라이선스는 있고, 유니티 애니메이션을 써도 된다"

답: **"사람 손 0"은 보장할 수 없지만, 사람의 일을 "승인과 예외 처리"로 줄이는 자동 분리 파이프라인은 만들 수 있다.** 지금의 승인 라운드와 같은 모양이다(생성 → 코드 검사 → 리뷰 시트 → 사용자 판정 → 실패한 것만 다시). 시험 한 캐릭터로 확인한다.

쓸 수 있는 수단 셋(모두 클로드 코드가 돌린다):

1. **생성 호출로 부위를 뽑는다** — 확정 그림을 기준 그림으로 붙이고 "같은 캐릭터, 같은 자세, **같은 자리와 크기**, 이 부위만(몸통+머리 / 뒤팔 / 앞 위팔 / 앞 아래팔+손 / 무기 / 망토·머리카락), 가려졌던 부분까지 온전히" — 부위마다 한 장(같은 캔버스라 정렬이 저절로 되고 가려진 곳을 모델이 그린다). 대안은 한 장의 **분해 시트**(부위를 흩어 그림) + 코드가 자른 뒤 원본에 맞춰 자리를 찾는 것.
2. **편집 호출(마스크 인페인트)** — 원본에서 무기 영역을 마스크로 지우고 "빈손으로 쥔 주먹"을 그리게 하기(안 1의 빈손 자세), 무기만 남긴 조각에서 손가락이 가리던 손잡이를 채우기, 뒤팔의 가려진 부분 채우기.
3. **코드(Pillow)** — 우리 그림은 **잉크선**으로 부위가 갈리므로 잉크선으로 둘러싸인 평면색 영역을 나누고(flood fill), 거친 다각형(비전 모델이 주거나 규칙으로)을 영역에 맞춰 마스크로 굳힌다. 자른 조각의 피벗(관절 자리)·겹침 순서·원본과의 겹침률을 검사해 리뷰 시트로 보인다. 정렬이 흔들리면 원본과의 겹침으로 되맞춘다.

불확실한 것(시험이 답한다): 모델이 부위를 **같은 자리·같은 크기**로 그리는가(드리프트), 가려진 부분의 품질과 스타일 일치, 손과 무기의 경계, 망토·머리카락처럼 덩어리가 아닌 부위.

스파인 라이선스가 있으면 길이 달라진다: spine-unity의 `SkeletonGraphic`이 **Canvas에 바로** 들어가므로 무대 구조를 바꾸지 않아도 되고, 스켈레톤(JSON + atlas)과 애니메이션은 **코드로 생성**할 수 있어 편집기는 다듬을 때만 열면 된다(안 쓸 수도 있다). 판은 **에디션**이다 — Professional이면 메시 변형(천·머리카락의 출렁임)이 되고 Essential이면 컷아웃만이다. Unity 2D Animation은 무대를 SpriteRenderer로 옮겨야 해서 지금은 둘째다. spine-unity는 패키지 추가라 Authority 확인(CLAUDE.md §10)이 필요하다.

### 시험의 설계 (기사 하나, 지시가 있을 때)

| 걸음 | 무엇 | 호출 |
|---|---|---|
| 1 | 부위별 생성(수단 1): 몸통+머리, 뒤팔, 앞 위팔, 앞 아래팔+손, 무기, 망토 — 각 1~2회 | 6~12 |
| 2 | 빈손 자세(수단 2): 대기·공격·피격의 무기 영역을 마스크로 지우고 주먹으로 | 3~6 |
| 3 | 코드: 자르기·정렬·피벗·겹침 검사, 리뷰 시트(원본 위에 조각을 겹쳐 차이를 붉게) | 0 |
| 4 | 코드 리그로 대기·휘두름·피격을 만들어 MP4(지금 자세 옆에) — Spine이면 JSON으로도 | 0 |
| 5 | 사용자 판정: 쓸 만한가, 어느 부위가 실패했나 → 그 부위만 다시(수단 2로 보완) | 실패분 |

합계 약 10~20회(≈ $0.3~0.8). 성공 기준: 조각을 원래 자리에 겹치면 원본과의 차이가 잉크선 두께 안, 가려진 부분이 자연스럽고 같은 손맛, 무기가 손과 깨끗이 갈림. 실패하면 **안 1(무기 층)만** 자동으로 하고 리그의 부위는 사람이 다듬는 길로 돌아간다.

판정할 것(추가):

| # | 질문 | 선택지 | 권장 |
|---|---|---|---|
| 7 | 자동 분리 시험 | a 기사 하나로 시험(호출 10~20) / b 하지 않음 | **a** |
| 8 | 리그의 런타임 | a Spine(라이선스 있음, Canvas 그대로, 코드 생성 JSON) / b 코드 리그(패키지 없음) / c Unity 2D Animation(무대 변경) | **a**(Professional이면 더욱) — 에디션 확인 뒤. b는 시험의 MP4용으로 어차피 만든다 |

### 2026-10-13: 사용자 "Spine Professional 라이선스를 가지고 있다. 내가 설치해야 하나? 이거면 사람 손 0으로 가능해질까?"

- 이 Mac에 Spine 편집기는 **없다**(`/Applications`, `~/Library/Application Support`에 없음). 편집기는 제안한 파이프라인(코드가 JSON·atlas를 생성 → spine-unity가 읽음)에 **필수가 아니다**. 있으면 좋은 점: CLI로 텍스처 패킹·데이터 검증, 사용자가 다듬고 싶을 때. 설치와 라이선스 로그인은 사용자가 한다(두 대까지). spine-unity 런타임과 URP 셰이더는 Unity 프로젝트에 클로드가 넣는다(패키지 추가 → Authority 확인).
- Professional이 없애는 것: 편집기 작업(코드 생성으로 대체), 무대 구조 변경(`SkeletonGraphic`), 메시 변형의 제한(천·머리카락). **남는 것은 부위 분리의 품질**(이미지 모델의 능력)이고 Spine은 그것을 바꾸지 않는다 → 시험이 답한다. 현실적인 목표는 "사람 = 판정만".
- `gen_image.py`는 `images.edit`(기준 그림 첨부)를 쓰고 있어 **마스크 인페인트는 `mask` 인자를 더하는 작은 코드 변경**으로 된다(시험의 일부).

### 판정 (2026-10-13)

사용자: **"일단 이것도 열린 결정으로 남겨줘. 내가 나중에 요청할께"** → G20은 미결(Roadmap "결정 관문 — 미결"). 요청이 오면 위 "시험의 설계"(기사 하나, 호출 10~20)부터 시작한다. 그 전에는 호출·패키지 추가·코드 변경을 하지 않는다.

## 출처 (조사 둘이 인용한 것 가운데 이 문서가 쓴 것)

- Unity: docs.unity3d.com/Packages/com.unity.2d.animation@13.0 (PreparingArtwork, SpriteSkin, SkinEdToolsShortcuts, SpriteSwapIntro, SL-Asset, 2DIK, Examples, changelog), com.unity.2d.psdimporter@12.0 (index, properties), com.unity.ugui@2.0 (class-Canvas, UICanvas), discussions.unity.com/t/2d-animation-in-ui/940219, discussions.unity.com/t/how-can-i-rig-a-character-from-sliced-multiple-png-files/919063.
- Spine: esotericsoftware.com (spine-purchase, spine-editor-license, spine-unity, spine-unity-installation, spine-unity-main-components, spine-unity-rendering, spine-meshes, spine-import-psd, blog/How-to-cut-your-assets-for-animation, blog/Holding-a-two-handed-weapon, forum 13065·2557·22986·12640), gamefromscratch.com(Spine 소개), drawphics.com(리그 품), indiedb.com(일주일 학습).
- 자동 리깅·층 나누기: arxiv.org/abs/2303.12741, github.com/facebookresearch/AnimatedDrawings, arxiv.org/abs/2512.15603, github.com/shitagaki-lab/see-through, lilting.ch(See-through 실사용), spinefactory.com, github.com/GenielabsOpenSource/spine-animation-ai, layer.ai, esotericsoftware.com/forum/d/30533(anysplit), community.openai.com/t/developing-sprite-sheets-with-gpt-image-2/1379831, github.com/3DStreet/3dstreet/issues/1934.
- 레퍼런스 게임: steamcommunity.com/groups/dd-workshop/discussions/0/135514800409348324(다키스트 던전 Spine 2.1.27), pcgamesn.com/slay-the-spire-2/art-direction, github.com/daviscook477/BaseMod/wiki/Custom-Characters.
- 대안: forum.defold.com(LoongBones), rive.app/docs(Unity runtime), live2d.com(comparison, sdk/license), retrostylegames.com/blog/unity-2d-animation-vs-spine.
