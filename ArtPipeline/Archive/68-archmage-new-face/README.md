# Round 68 — 대마법사: 지금 그림체의 새 얼굴 (흔한 미인형 특징) (2026-10-15)

시험 라운드다. **지금의 그림체와 게임의 그림은 바꾸지 않는다**(G19 그대로).

## 지시

- 사용자가 실제 인물의 사진 한 장을 주며: "서브컬처를 제외한 기존 그림체로 해당인물을 토대로 다시 마법사 이미지 생성".
- 답: 알아볼 수 있는 실제 인물을 섹시한 차림의 상업 게임 캐릭터로 쓰는 일(초상권)과 그 사진을 외부 생성 API에 보내는 일이라, 그 인물을 토대로 그리거나 닮게 하지 않는다고 답하고, 사진에 보이는 **흔한 분위기만 글로** 옮긴 새 얼굴을 제안했다.
- 사용자: "케리커쳐에다가 우리 그림체로 하면 괜찮을 것 같은데" → "얼굴의 특징 콧대 높이, 눈 등을 최대한 구체적으로 살려서 생성" → **"최대한 특징을 옮기돼 특정인을 직접 그리는게 아닌 수준으로 해줘"**.
- 그렇게 했다: 아래 목록 수준의 흔한 미인형 특징만 쓰고, 그 사람에게 더 가까워지게 다듬지 않았다. **사진은 붙이지도 저장소에 넣지도 않았다.** 프롬프트에 "not a portrait of any real person"을 적었다.

## 방법

- 세트: Round 67 `sub60/`(지금 §1~§4 + §2 노출 금지 셋, 기준 그림은 확정 시트, 모자 없음, 발키리 비율, 자두·청록 옷, 지금 지팡이)에서 **얼굴만** 바꿈. 서브컬처 얼굴(`Face` 열)은 쓰지 않는다.
- §4 "Face": 우리 캐리커처 얼굴("caricatured but attractive in our style") + 흔한 미인형 특징 — 갸름한 달걀형과 부드럽게 좁아지는 턱선·작은 턱, 쌍꺼풀이 있는 크고 긴 아몬드 눈(짙은 갈색 홍채), 눈꼬리를 살짝 뺀 가는 아이라인, 결이 곧고 부드러운 눈썹,
  **곧고 높은 콧대를 깨끗한 선 하나로, 작은 코끝, 콧구멍·콧볼 없음**, 가운데가 짙은 두 톤의 장밋빛 도톰한 입술, 볼 위쪽과 콧등을 가로지르는 평면 홍조, 작은 은 고리 귀걸이, 웃지 않되 찡그리지 않는 차분하고 조금 무심한 눈빛.
- §2에 더함: 콧구멍·콧볼·넓은 코·매부리코·각진 턱·짙은 립스틱. 머리: 가운데 가르마의 길고 짙은 갈흑색 웨이브(밝은 갈색 결 몇 줄).

```text
.venv/bin/python ArtPipeline/tools/gen_image.py --type character --key archmage --name archmage_newface --style ArtPipeline/Archive/68-archmage-new-face/STYLE_RUNTIME.md --roster ArtPipeline/Archive/68-archmage-new-face/character.csv
.venv/bin/python ArtPipeline/tools/gen_image.py --type character --key archmage --name archmage_newface_h75 --roster ArtPipeline/Archive/68-archmage-new-face/character-h75.csv --refit
.venv/bin/python ArtPipeline/Archive/68-archmage-new-face/mock_newface.py
```

- 호출 1회, $0.033(그림 누계 262회, 약 $6.90 / $10). 반투명 3.1%.
- 게임 안의 높이: 이 얼굴은 우리 캐리커처의 비율이라 눈~턱으로는 발키리(눈이 크고 낮음)와 견줄 수 없어, Round 67처럼 **턱~발바닥을 발키리(539)와 같게** 맞춤 → `Height` **75**(서는 키 672, 발키리 705). 이마선~턱은 발키리의 약 0.94배.
- `mock-newface.png`: 얼굴(서브컬쳐_60과 새 얼굴), 게임 배율로 선 여섯 직업과 두 판, 여성 용병의 머리(게임 배율), 광산 무대.

## 결과 (관찰)

- **그림체: 지금 무리와 같다.** 굵은 손 잉크선, 고무 같은 곱슬 덩어리, 캐리커처 얼굴 — 발키리·주교·지금 대마법사와 한 게임으로 읽힌다(서브컬쳐_60은 혼자 다름).
- **얼굴:** 작은 은 귀걸이, 홍조, 콧볼 없는 곧은 코, 짙은 갈색 눈은 들어갔다. 다만 눈썹을 내린 진지한 표정(우리 그림체의 기본)이 남았고, 입술이 짙은 편이며, 눈이 "긴 아몬드"보다는 우리 캐리커처의 큰 눈에 가깝다.
- **머리:** 가운데 가르마의 부드러운 웨이브보다 크고 풍성한 곱슬로 나왔다(우리 그림체의 "big rubbery clumps").
- **비율·옷·지팡이:** Round 67 다시 그림과 같다. 게임 배율에서 머리 크기가 여성 용병들과 비슷하다.
- 무대: 머리가 짙어 위쪽이 어둡지만 자두·청록 옷으로 몸은 읽힌다.

## 판정할 것 (모두 권장안을 적었다)

| # | 질문 | 선택지 | 권장 |
|---|---|---|---|
| 1 | 대마법사의 얼굴 | a 서브컬쳐_60(Round 67) / b 새 얼굴(이 판) / c 지금 게임의 대마법사 | **b** — 무리와 한 그림체로 읽히고, 판정 2(여성 셋에 서브컬처를 같이?)를 열지 않아도 된다 |
| 2 | b를 고르면 다듬기(호출 1회) | 그대로 / 눈썹을 내리지 않게·입술을 옅게·머리를 가운데 가르마의 부드러운 웨이브로 | 다듬기 — 지시한 특징 가운데 덜 들어간 셋만 문구를 세게. 특정인에 가깝게 하는 다듬기는 하지 않는다 |
| 3 | 확정하면 | 대기 그림 연결(`Rosters/character.csv`의 대마법사 행을 이 소재·`Height` 75로) + 공격·피격·붕괴·각성 넷 다시 그림(호출 4회) | 1·2 뒤에 |

호출은 지시가 있을 때만 한다. 게임에 연결하는 것도 승인 뒤에 한다.

## 판정 → 2차 (2026-10-15)

사용자: **"1. 조금 더 가린 옷, 뒤 망토 없는 옷 스타일, 재생성 2. 진행"**(2 = 위 판정할 것 2의 다듬기). 판정할 것 1(새 얼굴)은 이 지시로 b를 고른 것으로 읽었다.

- 한 장에 함께 넣음(`try2/`):
  - 옷(권장안으로 정함, 사후 검토): 짙은 자두색 드레스에 은 테 — 끈으로 묶은 코르셋 몸판이 하트형의 얌전한 목선과 가는 어깨끈으로 엉덩이까지 덮고(배를 드러내지 않음), 허벅지 중간까지 오는 퍼진 치마의 한쪽 트임에 짙은 청록 속치마. 사슬 허리띠와 물약 둘, 긴 장갑, 허벅지 장화는 그대로. **망토·뒤 자락 없음**(§2에 "cape").
  - 얼굴 다듬기: 눈썹은 수평으로 편하게(내리거나 모으지 않음), 입술은 피부보다 한 톤 짙은 옅은 장밋빛, 웃지도 찡그리지도 않음. §2에 빨간 입술·찡그림·좁은 곱슬. 머리는 가운데 가르마, 정수리는 매끈하고 아래로 크고 느슨한 웨이브.

```text
.venv/bin/python ArtPipeline/tools/gen_image.py --type character --key archmage --name archmage_newface2 --style ArtPipeline/Archive/68-archmage-new-face/try2/STYLE_RUNTIME.md --roster ArtPipeline/Archive/68-archmage-new-face/try2/character.csv
.venv/bin/python ArtPipeline/tools/gen_image.py --type character --key archmage --name archmage_newface2_h75 --roster ArtPipeline/Archive/68-archmage-new-face/try2/character-h75.csv --refit
.venv/bin/python ArtPipeline/Archive/68-archmage-new-face/try2/mock_try2.py
```

호출 1회, $0.033(그림 누계 263회, 약 $6.93 / $10). 반투명 3.2%. `Height` 75(턱~바닥 539에 맞춘 값 76과 거의 같아 1차와 같게), 서는 키 672, 이마선~턱은 발키리의 약 1.07배. `try2/mock-try2.png`.

### 2차의 결과 (관찰)

- **옷: 지시대로.** 몸판이 허리를 덮고 치마가 허벅지 중간까지 와 1차보다 가렸다. 어깨·목선·허벅지 위쪽은 드러나 섹시함은 남았다. 망토와 뒤 자락이 없다. 트임 안의 청록 속치마는 작게만 보인다.
- **얼굴:** 입술이 옅어졌고(1차의 짙은 색에서), 곱슬이 크고 느슨한 웨이브로 부드러워졌다. **눈썹은 여전히 조금 기울어** 진지한 표정이 남았다(우리 그림체의 기본 표정이 이긴다). 가운데 가르마는 분명하지 않다.
- 장화는 무릎께에 접힌 단이 하나 더 있어 겹쳐 신은 듯 보인다(Round 66 3차와 같은 일).
- **무대: 1차보다 어둡다.** 청록 자락이 빠지고 자두·회보라·짙은 머리만 남아 광산에서 실루엣이 어둡게 묻힌다. 1차는 청록 자락이 실루엣을 읽히게 했다.

### 판정할 것 (2차)

| # | 질문 | 선택지 | 권장 |
|---|---|---|---|
| 1 | 이 그림 | 확정 후보 / 고칠 곳 지시 | 사용자의 눈 |
| 2 | 무대에서의 어두움 | 그대로 / 밝은 면을 늘림(호출 1회): 은 테를 넓히고, 드레스를 한 톤 밝은 자두색으로, 청록 속치마를 더 보이게(망토 없이) | **밝은 면을 늘림** — 게임 크기의 어두운 광산에서 실루엣이 읽혀야 한다(다른 용병은 크림·빨강·은이 있다) |
| 3 | 눈썹 | 그대로(우리 그림체의 진지함) / 한 번 더 세게 | 그대로 — 두 번 적어도 남는 것은 그림체의 결이다. 꼭 바꿔야 하면 2와 같은 한 장에서 |
| 4 | 확정하면 | 대기 그림 연결(`Height` 75) + 자세 넷 다시 그림(호출 4회) | 1·2 뒤에 |

## 3차: 무대에서 읽히게 (2026-10-15)

- 사용자가 같은 실제 인물의 사진에 연필 스케치 효과를 입힌 그림을 주며 "업로드 사진 기반 마법사 이미지 생성" → 같은 이유로 그 그림을 붙이거나 그 얼굴을 토대로 하지 않는다고 답하고 갈래 셋을 제안(2차 이어 다듬기 권장 / 새 얼굴의 취향을 글로 / 권리가 있는 비실존 캐릭터 그림을 기준으로) → 사용자 **"현재 그림체로"** → 권장 갈래(2차 이어 다듬기)로 진행.
- `try3/`: 문서는 2차 그대로(얼굴 그대로) + §2에 접힌 장화 단·겹쳐 신은 장화. 소재: 드레스를 짙은 자두에서 **한 톤 밝은 모브 자두**로, **넓고 밝은 은 테**(목선·몸판 가장자리·단), 치마 앞이 갈라져 **청록 속치마가 넓게** 보임, 장갑·장화는 슬레이트 회보라, **장화는 굽부터 허벅지까지 한 장(접힘·두 번째 단 없음)**. 망토 없음.

```text
.venv/bin/python ArtPipeline/tools/gen_image.py --type character --key archmage --name archmage_newface3 --style ArtPipeline/Archive/68-archmage-new-face/try3/STYLE_RUNTIME.md --roster ArtPipeline/Archive/68-archmage-new-face/try3/character.csv
.venv/bin/python ArtPipeline/tools/gen_image.py --type character --key archmage --name archmage_newface3_h75 --roster ArtPipeline/Archive/68-archmage-new-face/try3/character-h75.csv --refit
.venv/bin/python ArtPipeline/Archive/68-archmage-new-face/try3/mock_try3.py
```

호출 1회, $0.033(그림 누계 264회, 약 $6.96 / $10). 반투명 3.0%. `Height` 75(턱~바닥에 맞춘 값 74.5), 서는 키 672, 이마선~턱은 발키리의 약 1.03배. `try3/mock-try3.png`(2차와 3차).

### 3차의 결과 (관찰)

- **밝기:** 모브 자두 드레스, 넓은 은 테, 앞의 청록 속치마로 2차보다 무대에서 실루엣이 읽힌다. 다른 용병(크림·빨강·은)만큼 밝지는 않다 — 짙은 머리와 회보라 장갑·장화가 큰 면이다.
- **장화:** 굽부터 허벅지까지 한 장으로 매끈해졌다(겹친 단 없음).
- **옷:** 2차의 가린 정도와 같다(몸판이 허리를 덮음, 허벅지 중간의 치마, 망토 없음).
- **얼굴:** 2차와 같은 결. 눈썹은 조금 기울고 입술은 장밋빛(2차보다 조금 짙게 나옴). 머리는 밝은 갈색 결이 늘어난 느슨한 웨이브.

### 판정할 것 (3차)

| # | 질문 | 선택지 | 권장 |
|---|---|---|---|
| 1 | 이 그림 | 확정 후보 / 고칠 곳 지시 | **확정 후보** — 지시한 옷·망토·장화가 다 들어갔고, 무대에서 읽히며, 무리와 한 그림체다 |
| 2 | 확정하면 | 대기 그림을 게임에 연결(`Rosters/character.csv`의 대마법사 행을 이 소재·`Height` 75로, 원본을 `output/character/archmage.raw.png`로) + 공격·피격·붕괴·각성 넷을 새 대기를 보고 다시 그림(호출 4회, 발키리와 같은 길) + 체인·스크린샷 | 확정 뒤 한 번에 |
| 3 | 더 밝게 | 그대로 / 장갑·장화를 밝은 은회색으로(호출 1회) | 그대로 — 게임에서 보고 정해도 된다 |

## 4차: 사용자가 준 가상 인물의 얼굴 특징 (2026-10-15)

- 사용자가 실사풍 인물 이미지를 주며 "이 이미지 얼굴을 기반으로 다시 이미지 생성" → 실제 인물인지 물음 → 사용자 **"가상 인물"**(AI로 만든 가상 인물로 확인). 그 이미지는 붙이지 않고(실사 사진은 그림체까지 끌고 감, Round 63) 얼굴 특징을 글로 옮겼다. 이미지는 저장소에 넣지 않았다.
- `try4/`: 3차에서 §4 "Face"와 머리·장신구만 바꿈 — 둥근 달걀형과 도톰한 볼·작고 둥근 턱, 크고 순한 짙은 갈색 눈(속쌍꺼풀, 자연스러운 속눈썹, 눈 밑 애교살), 곧고 자연스러운 짙은 눈썹(수평), 둥근 코끝의 작은 곧은 코(콧볼 없음), 옅은 자연 분홍의 도톰한 입술, 맑은 피부와 옅은 분홍 홍조.
  표정은 Design/10 §2의 【확정】대로 웃지 않음(사진의 은은한 미소 대신 다문 입, 차분하고 부드러운 눈빛). 머리는 긴 검은 레이어드에 볼을 감싸는 옆머리, 가르마는 가운데에서 조금 비켜. 귀걸이를 빼고 가는 금 목걸이에 작은 네잎 클로버(사진에 있던 것).

```text
.venv/bin/python ArtPipeline/tools/gen_image.py --type character --key archmage --name archmage_newface4 --style ArtPipeline/Archive/68-archmage-new-face/try4/STYLE_RUNTIME.md --roster ArtPipeline/Archive/68-archmage-new-face/try4/character.csv
.venv/bin/python ArtPipeline/tools/gen_image.py --type character --key archmage --name archmage_newface4_h78 --roster ArtPipeline/Archive/68-archmage-new-face/try4/character-h78.csv --refit
.venv/bin/python ArtPipeline/Archive/68-archmage-new-face/try4/mock_try4.py
```

호출 1회, $0.033(그림 누계 265회, 약 $7.00 / $10). 반투명 3.2%. 턱~바닥을 발키리(539)와 같게 → `Height` **78**(서는 키 705 = 발키리), 이마선~턱은 발키리의 약 1.05배. `try4/mock-try4.png`(3차와 4차).

### 4차의 결과 (관찰)

- **얼굴:** 볼이 도톰하고 둥글어졌고, 짙은 갈색의 크고 동그란 눈, 분홍 입술, 홍조가 들어갔다 — 3차보다 어리고 부드러운 얼굴. 다만 우리 그림체의 진한 아이라인과 **조금 내린 눈썹**이 남아 사진의 순한 인상보다는 또렷하고 진지하다. 입은 조금 벌어진 듯 보인다.
- **머리:** 긴 검은 레이어드는 들어갔지만 **볼을 감싸는 옆머리**는 분명하지 않고 바람에 날리는 듯 풍성하다.
- 금 클로버 목걸이는 들어갔다(게임 크기에서는 점 하나).
- **옷:** 3차와 같은 꼴인데 드레스 색이 모브 자두에서 **먼지 낀 모브 갈색** 쪽으로 조금 바뀌었다.
- 무대: 검은 머리가 3차의 갈색보다 어둡다. 몸은 3차와 비슷하게 읽힌다.

### 판정할 것 (4차)

| # | 질문 | 선택지 | 권장 |
|---|---|---|---|
| 1 | 대마법사의 얼굴 | 3차 / 4차 | 사용자의 눈. 지시(가상 인물의 얼굴)에는 4차 |
| 2 | 4차를 고르면 다듬기(호출 1회) | 그대로 / 덜 들어간 것만 세게: 눈썹을 내리지 않음, 입을 다묾, 볼을 감싸는 옆머리, 드레스는 3차의 모브 자두로 | 다듬기 — 이 넷은 문구가 지켜지지 않은 곳이다 |
| 3 | 확정하면 | 대기 그림 연결(`Height` 78) + 자세 넷 다시 그림(호출 4회) + 체인·스크린샷 | 1·2 뒤에 |

## 5차: 가상 인물의 이미지를 얼굴 기준으로 붙임 (2026-10-15)

사용자: **"얼굴의 구체적 특징이 하나도 반영 안되어 있어. 그전 마법사 이미지는 무관하게 다시 이미지 생성해줘"**.

- 원인: 4차까지는 특징을 글로만 옮겨, 우리 §4의 캐리커처 얼굴과 확정 시트가 얼굴을 정했다.
- 고친 길: 사용자가 AI로 만든 가상 인물의 이미지(사용자 확인)를 **둘째 기준 그림**으로 붙인다. 그 이미지는 저장소 밖 `ArtPipeline/output/reference/archmage_virtual_face.jpg`(Git 제외)에 두었다.
  - `tools/gen_image.py`에 시험용 선택지 `--extra-reference`(타입의 기준 그림 뒤에 한 장 더, 여러 번 가능)를 더했다. 쓰임은 시험 문서의 §3이 적는다. Architecture/13의 도구 줄.
  - `try5/STYLE_RUNTIME.md`: 지금 §1~§4에서 §3을 "첫째(확정 시트)는 그림체만, 둘째(가상 인물)는 얼굴과 머리만 — 같은 여자로 알아보게, 실사(빛·피부 질감·광택·옷·자세·배경)는 버림"으로, §4 "Face"를 "둘째 이미지의 얼굴을 우리 그림체로, 캐리커처로 바꾸지 않음"(둥근 달걀형·도톰한 볼·작고 둥근 턱, 크고 순한 짙은 갈색 눈과 속쌍꺼풀·애교살, 곧은 눈썹, 콧볼 없는 작은 코, 옅은 분홍의 도톰한 입술, 웃지 않음)으로, §2에 사진 같은 얼굴·피부 질감·콧구멍·콧볼·찡그림·짙은 립스틱·망토와 노출 금지 셋.
  - 소재: 앞 판들과 무관하게 새로 적되 승인된 옷(3차의 모브 자두 드레스·은 테·청록 속치마·망토 없음·매끈한 허벅지 장화), 지금 지팡이, 발키리 비율, 클로버 목걸이는 그대로.

```text
.venv/bin/python ArtPipeline/tools/gen_image.py --type character --key archmage --name archmage_newface5 --style ArtPipeline/Archive/68-archmage-new-face/try5/STYLE_RUNTIME.md --roster ArtPipeline/Archive/68-archmage-new-face/try5/character.csv --extra-reference ArtPipeline/output/reference/archmage_virtual_face.jpg
.venv/bin/python ArtPipeline/tools/gen_image.py --type character --key archmage --name archmage_newface5_h76 --roster ArtPipeline/Archive/68-archmage-new-face/try5/character-h76.csv --refit
.venv/bin/python ArtPipeline/Archive/68-archmage-new-face/try5/mock_try5.py
```

호출 1회, $0.046(이미지 둘이라 입력이 큼. 그림 누계 266회, 약 $7.04 / $10). 반투명 3.3%. 턱~바닥을 발키리와 같게 → `Height` **76**(서는 키 681), 이마선~턱은 발키리의 약 1.1배. `try5/mock-try5.png`(4차와 5차). 가상 인물 이미지와 나란히 놓은 비교는 저장소 밖에만 만들었다.

### 5차의 결과 (관찰)

- **얼굴이 이미지 쪽으로 왔다:** 둥글고 도톰한 볼과 작고 둥근 턱, 순한 눈빛, 다문 도톰한 분홍 입술, 볼을 감싸는 긴 검은 레이어드 머리. 4차의 진한 캐리커처(내린 눈썹, 벌어진 입)는 빠졌다.
- **남은 차이:** 눈은 여전히 우리 그림체의 굵은 아이라인 눈이다(이미지는 아이라인이 옅고 눈꼬리가 순하게 내려감). 3/4 옆모습이라 정면 사진의 이마 앞머리(가르마의 커튼 앞머리)는 잘 보이지 않는다.
- **그림체:** 실사로 끌려가지 않고 지금 무리와 같다(§3의 "실사는 버림"이 지켜짐).
- 옷·지팡이·장화·목걸이는 지시대로. 키는 4차보다 조금 작고 머리는 조금 크다.

### 판정할 것 (5차)

| # | 질문 | 선택지 | 권장 |
|---|---|---|---|
| 1 | 얼굴 | 확정 후보 / 더 가깝게 다듬기 | 사용자의 눈 |
| 2 | 더 가깝게 하면(호출 1회) | 같은 둘째 기준 그림으로 눈만 세게: 아이라인을 옅게, 눈꼬리를 순하게 내려, 커튼 앞머리가 보이게 | 다듬기 — 남은 차이가 눈과 앞머리뿐이다 |
| 3 | 확정하면 | 대기 그림 연결(`Height` 76) + 자세 넷 다시 그림(호출 4회, 자세는 이 대기 그림을 붙이므로 얼굴이 따라옴) + 체인·스크린샷 | 1·2 뒤에 |

## 6차: 얼굴 크기·비율 재조정, 마법사 로브 (2026-10-15)

사용자: **"얼굴 크기 재조정, 전체 비율 재조정, 옷 마법사 로브로 교체"**.

- 읽은 것(권장안, 사후 검토): 얼굴은 여성 용병과 같은 크기로(5차는 게임 안에서 이마선~턱이 발키리의 약 1.11배), 비율은 그 얼굴로 키도 발키리와 같게(이마선~발바닥 ÷ 이마선~턱: 발키리 약 7.0, 5차 6.4 → 몸을 조금 길게, 소재 "약 6등신, 시트의 큰 머리보다 조금 작게"), 옷은 마법사 로브.
- `try6/`: 문서는 5차 그대로(두 기준 그림). 소재: 모브 자두색의 몸에 맞는 발목 길이 로브(얌전한 V 목선, 넓은 종 소매, 목선·소맷부리·단의 넓은 은 테, 소매와 앞트임에 보이는 청록 안감, 사슬 허리띠와 물약 둘로 잘록한 허리를 조임, 무릎부터 앞이 열려 다리가 조금 보임), 낮은 굽의 회보라 발목 장화, 망토·두건 없음. 얼굴·머리·목걸이·지팡이는 그대로.

```text
.venv/bin/python ArtPipeline/tools/gen_image.py --type character --key archmage --name archmage_newface6 --style ArtPipeline/Archive/68-archmage-new-face/try6/STYLE_RUNTIME.md --roster ArtPipeline/Archive/68-archmage-new-face/try6/character.csv --extra-reference ArtPipeline/output/reference/archmage_virtual_face.jpg
.venv/bin/python ArtPipeline/tools/gen_image.py --type character --key archmage --name archmage_newface6_h71 --roster ArtPipeline/Archive/68-archmage-new-face/try6/character-h71.csv --refit
.venv/bin/python ArtPipeline/tools/gen_image.py --type character --key archmage --name archmage_newface6_h75 --roster ArtPipeline/Archive/68-archmage-new-face/try6/character-h75.csv --refit
.venv/bin/python ArtPipeline/Archive/68-archmage-new-face/try6/mock_try6.py
```

호출 1회, $0.045(그림 누계 267회, 약 $7.09 / $10). 반투명 3.2%. 원본에서 이마선~턱 139, 이마선~발바닥 939(6.8배, 발키리 7.0배).

| 맞춤 | `Height` | 서는 키 | 얼굴(이마선~턱, 발키리 = 1) | 이마선~바닥(발키리 = 1) |
|---|---|---|---|---|
| 얼굴을 발키리와 같게 | 71 | 642 | 약 1.00 | 약 0.97 |
| 조금 크게 | 75 | 678 | 약 1.05 | 약 1.02 |

`try6/mock-try6.png`: 5차와 6차의 얼굴, 게임 배율(6차는 71·75 둘), 머리, 광산 무대.

### 6차의 결과 (관찰)

- **옷: 마법사 로브로 바뀌었다.** 모브 자두색 로브, 넓은 은 테, 소매와 앞트임의 청록 안감, 사슬 허리띠와 물약, 앞트임으로 보이는 다리, 낮은 굽 발목 장화. 망토·두건 없음. 무대에서 은 테와 청록 안감으로 실루엣이 읽힌다.
- **얼굴:** 5차와 같은 가상 인물의 얼굴(둥근 볼, 순한 눈, 도톰한 입술, 볼을 감싸는 검은 옆머리). 게임 안의 크기는 맞춤에 따라 위 표.
- **비율:** 몸이 5차보다 조금 길어져 얼굴과 키의 비가 발키리와 거의 같다. 다만 다른 용병은 머리숱이 크고 몸이 다부져, **71이면 무리에서 작고 가늘게** 보인다. 75면 키는 무리와 비슷하고 얼굴은 5% 크다.

### 판정할 것 (6차)

| # | 질문 | 선택지 | 권장 |
|---|---|---|---|
| 1 | 이 그림 | 확정 후보 / 고칠 곳 지시 | 사용자의 눈 |
| 2 | 게임 안의 크기(호출 없음) | `Height` 71(얼굴 = 발키리) / 75(키 ≈ 무리, 얼굴 1.05) | **75** — Design/10 §2 "여성의 머리는 서로 비슷한 크기"의 범위 안에서 무리와 키가 맞는다 |
| 3 | 확정하면 | 대기 그림 연결 + 자세 넷 다시 그림(호출 4회) + 체인·스크린샷 | 1·2 뒤에 |

## 확정과 연결 (2026-10-15)

사용자: **"1. 확정 2. 75 3. 진행"**.

### 대기 그림

- `Rosters/character.csv`의 대마법사 행을 6차의 소재로(얼굴·머리·로브·장화·목걸이·지팡이를 한 문장으로, `Height` 75, 몸은 저장한 체형 대신 글, `FaceDx/Dy` 빈 값).
  6차는 두 기준 그림(확정 시트 + 가상 인물)으로 그린 것이라, 같은 행으로 다시 그려도 같은 얼굴이 나오지 않는다 — 대기는 다시 그리지 않고 **6차 원본을 그대로 확정**했다(Round 64 발키리와 같은 길).
- 6차 원본을 `output/character/archmage.raw.png`로 놓고 `--refit`(키 672). 지금 쓰던 산출물은 `output/_before-r68-archmage/`(저장소 밖)과 Git에 있다.

### 자세 넷 (호출 4회)

- 소재: `Rosters/attack.csv`·`hit.csv`의 대마법사 행과 `state/broken.csv`·`resolute.csv`(Round 40의 소재에서 생김새 문장만 새 모습으로 바꾸고 자세 문장은 그대로. "long body"·"grey hair"·"the purple gone grey"를 새 모습에 맞게 고침).
- 그리기: `generate_poses.sh`(Round 64의 것과 같은 길: 공격·피격은 자세 타입, 붕괴·각성은 Round 38의 상태 자세 문서, 끝 문장은 Round 40의 "같은 얼굴·머리 크기·비율"). 자세의 기준 그림은 새 대기 원본이다(`archmage.reference.png`).
- 호출 4회, 약 $0.12(그림 누계 271회, 약 $7.21 / $10).

### 맞추기 (머리 크기)

새 대마법사에는 금 장식이 없어(`fit_pose`의 원판 0개) 배율은 머리로 잰 값을 적었다(Round 40의 규칙): 귀~코끝의 가로, 눈~턱, 두 눈 사이(두 눈이 다 보일 때)를 대기 원본과 견줌.

| 자세 | 귀~코끝 | 눈~턱 | 두 눈 사이 | 쓴 배율 |
|---|---|---|---|---|
| 공격 | 1.09 | 1.14 | 1.117 | **1.11** (`Rosters/attack.csv` Scale) |
| 피격 | 0.89 | 0.875 | (한 눈을 감음) | **0.89** (`Rosters/hit.csv` Scale) |
| 붕괴 | (귀 가림) | 0.94 (고개 숙임) | 0.80 (크게 뜬 눈) | **0.92** |
| 각성 | 0.89 | 0.95 | 0.925 | **0.92** |

```text
.venv/bin/python ArtPipeline/tools/fit_pose.py --type attack --type hit --key archmage
.venv/bin/python ArtPipeline/Archive/40-state-poses/fit_state_poses.py --jobs archmage --scale archmage_broken=0.92 --scale archmage_resolute=0.92
.venv/bin/python ArtPipeline/Archive/68-archmage-new-face/review_poses.py
```

- 색 맞춤은 `fit_pose`대로(붕괴는 일부러 어둡게 그린 색을 대기 쪽으로 옮김: ΔE 12.2 → 3.5, Round 38·64와 같은 길).
- `review-poses.png`: 자세 캔버스 같은 배율(위는 지금 쓰던 그림 — 대기는 바뀐 `Height` 75로 서서 옛 자세보다 작게 보인다, 아래가 새 그림). `review-heads.png`: 얼굴을 가운데에 25px 격자 — 새 줄의 다섯이 같은 머리 크기다.
- 확정 원본은 `approved/`(대기·공격·피격·붕괴·각성의 raw).

### 게임에 연결한 파일 (같은 경로, .meta 그대로)

`Assets/@Art/Unit/Job/archmage.png`, `Assets/@Art/Pose/Job/archmage_attack.png`, `_hit.png`, `_broken.png`, `_resolute.png`.

### 권장안으로 정한 세부 (사후 검토)

- 대기는 6차를 그대로 확정(다시 그리지 않음), `Height` 75.
- 자세의 배율을 머리로 잼(1.11 / 0.89 / 0.92 / 0.92).
- 확정 시트(`References/Character/style_ref_roster.png`)의 대마법사는 옛 그림 그대로 둠 — 시트는 다른 직업·몬스터를 그릴 때의 그림체 기준이다. 새 대마법사로 시트를 다시 합성할지는 지시가 있을 때.
- 가상 인물 이미지는 저장소 밖(`output/reference/`)에만 두고, 얼굴을 그 이미지로 다시 그릴 일이 생기면 그것을 `--extra-reference`로 다시 붙인다.

### 검증

- 체인 `20261015-155216` **CHAIN OK**(setup, EditMode 845/845, PlayMode 83/92, 9 skipped: 스크린샷). 시뮬은 돌리지 않았다(그림과 그림 소재만 바뀜).
- 스크린샷 `20261015-r68` 9/9(PNG 86). 새 대마법사가 나오는 장면: `game/impl_ko_05_battle.png`(공격 자세), `impl_ko_06_battle_won.png`(대기), `impl_ko_09_boss_battle.png`(대기, 보스전). 자세 장면(`_23`·`_24`·`_38`·`_39`)의 Test 파티에는 대마법사가 없어 피격·붕괴·각성은 `review-poses.png`로 본다.
- `git diff --check` 깨끗. `Assets`에서 바뀐 것은 그림 다섯뿐(.meta 그대로). 남은 `Assets/InitTestScene*` 없음.

## 공격 자세의 팔 다시 그림 (2026-10-15)

사용자: **"공격시 팔 하나가 없네. 재생성"**.

- 검토: 1차 공격 자세는 지팡이를 쥔 팔 하나만 그려지고(소재의 "다른 손을 펼쳐 앞으로"가 빠짐), 지팡이 아래 끝이 가슴을 지나갔다.
- 2차: Round 64 도끼처럼 그림 속 방향을 적은 `--extra`(`attack-extra.txt`): 두 팔을 모두 그림, 앞팔은 오른쪽으로 뻗어 지팡이의 가운데 아래를 쥠, 지팡이 아래 끝은 앞손 바로 뒤 엉덩이 옆에서 멈추고 몸을 가로지르지 않음, 다른 팔도 지팡이 아래 옆으로 뻗고 손바닥을 적에게 펼침. 호출 1회, $0.031(그림 누계 272회, 약 $7.24 / $10).
- 결과: 두 팔이 다 나왔다(지팡이를 쥔 손, 그 아래로 펼친 손과 종 소매). 지팡이의 아래 끝은 몸 앞 허리께까지 온다(몸 앞에 든 모양).
- 맞추기: 자동 눈 재기가 벌린 입을 눈으로 잡아(2.66) 손으로 쟀다 — 두 눈 사이 약 1.02, 귀~코끝 1.07 → `Rosters/attack.csv` Scale **1.05**(1차의 1.11에서). `review-heads.png`에서 대기와 같은 머리.
- 1차는 `approved/attack-sources/attack_try1.raw.png`와 `output/_before-r68-archmage/attack-try1/`(저장소 밖), 2차 원본은 `attack_try2.raw.png`와 `approved/archmage_attack.raw.png`. 게임의 `archmage_attack.png`를 2차로 바꿈.

```text
.venv/bin/python ArtPipeline/tools/gen_image.py --type attack --key archmage --tail "<generate_poses.sh의 TAIL>" --extra "$(cat ArtPipeline/Archive/68-archmage-new-face/attack-extra.txt)"
.venv/bin/python ArtPipeline/tools/fit_pose.py --type attack --key archmage
```

### 공격 자세의 크기 (2026-10-15)

사용자: **"스크린샷 보니 공격 모션시 너무 케릭터 크기가 줄어든 것 같은데 이것도 확인해줘"**.

- 맞았다. 자세와 상관없이 크기가 같아야 할 **물약 병**을 게임 배율의 캔버스(2016x1008)에서 쟀다: 1차 공격(Scale 1.11)은 대기의 **약 0.90~0.92배**(빨강 물약 17x13 대 19x14, 파랑 높이 21 대 24). 1차는 모델이 머리를 몸보다 크게 그려 머리로 잰 1.11이 몸을 줄였다.
- 2차 공격(Scale 1.05)은 빨강 0.97, 파랑 1.00배 — 머리(`review-heads.png`)와 물약이 둘 다 대기와 같다. 런지 자세라 전체 키는 대기보다 약 5% 낮게 선다(다리를 벌림, 의도한 자세).
- 다른 자세: 피격·각성의 빨강 물약 1.0배. 붕괴는 일부러 어둡게 그려 물약을 잴 수 없고, 머리는 대기와 같다.
- 다음에 머리와 몸의 비가 대기와 다르게 그려진 자세를 맞출 때는 머리 하나만이 아니라 물약 같은 단단한 소품도 함께 잰다.
- 검증: 체인 `20261015-161348` **CHAIN OK**(setup, EditMode 845/845, PlayMode 83/92), 스크린샷 `20261015-r68b` 9/9(PNG 86). 게임 안의 비교 `game/attack-size-compare.png`(대기 / 1차 / 2차). 이 판의 `_05` 장면은 무대가 조금 확대된 순간에 찍혀(옆의 주교도 커짐) 장면끼리의 크기 비교는 어림이고, 크기의 판단은 위 캔버스의 잼으로 한다. 남은 `Assets/InitTestScene*` 없음.

### 공격 자세 3차: 두 손을 떼어 놓음 (2026-10-15)

사용자: **"크기는 맞는데 양손이 이상하게 붙었어. 다시 생성"**.

- 2차는 지팡이를 쥔 손과 펼친 손이 같은 자리에서 나와 겹쳐 보였다.
- 3차(권장안으로 정함, 사후 검토): 두 팔을 반대 방향으로 — 앞팔은 오른쪽으로 뻗어 지팡이를 쥐고(아래 끝은 그 손 바로 뒤에서 멈춤), 다른 팔은 지팡이에서 떨어져 **뒤쪽 왼쪽 위로** 젖히고 손을 펼침. 두 손 사이에 몸통 전체. `--extra`는 `attack-extra-3.txt`. 호출 1회, $0.031(그림 누계 273회, 약 $7.27 / $10).
- 결과: 지팡이를 앞으로 겨누고 다른 손은 뒤로 활짝 — 두 손이 또렷이 떨어졌다. 지팡이가 몸을 가로지르지 않는다.
- 크기: 이번에도 모델이 머리를 몸보다 크게 그렸다(원본에서 두 눈 사이 1.09배, 물약 병 0.96~1.0배). 머리만 맞추면 1차처럼 몸이 작아지고 몸만 맞추면 머리가 약 12% 커져, **Scale 1.03**으로 가운데를 잡았다(게임에서 머리 약 1.06배, 몸 약 0.94~0.97배).
- 2차는 `approved/attack-sources/attack_try2.raw.png`와 `output/_before-r68-archmage/attack-try2/`, 3차 원본은 `attack_try3.raw.png`와 `approved/archmage_attack.raw.png`. 게임의 `archmage_attack.png`를 3차로 바꿈.
- 검증: 스크린샷 `20261015-r68c` 9/9(PNG 86). 게임 안의 비교 `game/attack3-compare.png`(대기 / 2차 / 3차): 두 손이 떨어져 읽히고 크기는 대기와 비슷하다. `review-attack-hands.png`(2차와 3차 원본).
- 체인: `20261015-163228` setup·EditMode 845/845 OK, **PlayMode 1 실패**(`Battle_PartyUnitsShowFatiguePips_AndABreakdownIsAMomentOnTheStage`: 붕괴 뒤 0.25초 사이 캡션 셋이 밀려 붕괴의 줄이 빠짐). PlayMode만 다시 `20261015-164805` **1 실패, 다른 Test**(`Battle_ACriticalHit_…`: 적이 먼저 치명타를 냄). 둘 다 새 런의 무작위 시드에 기대는 Test로 그림과 무관하다(같은 코드에 그림만 다른 앞의 두 체인 `155216`·`161348`은 모두 통과). 리팩토링 검토의 **R10**(PlayMode 새 런의 시드 고정)이 고칠 일이다. 남은 `Assets/InitTestScene*` 없음.

### 자세가 바닥 아래로 내려감 (2026-10-15)

사용자: **"지금 공격 모션은 너무 밑으로 내려가는데 확인해줘"**.

- 원인: `fit_pose`가 자세의 뒷발을 **부츠의 가죽색**(확정 원본 맨 아래의 따뜻한 갈색)으로 찾는데, 새 대마법사의 부츠는 회보라라 그 규칙이 **지팡이의 갈색 나무**(114, 78, 54)를 잡았다. 그래서 지팡이 아래쪽을 발바닥으로 보고 바닥선에 맞춰, 진짜 부츠가 바닥 아래로 들어갔다:
  공격 3차는 원본에서 30px(맞춘 캔버스 약 16px, 1080p 화면 약 5px) 아래, 1차는 +40까지. 피격·붕괴·각성도 5~16px(원본) 내려가 있었다. 옛 대마법사는 갈색 가죽 부츠라 드러나지 않았다.
- 고침: `Rosters/character.csv`에 **Boots** 열(몬스터의 `Feet`와 같은 길, "r g b")을 더하고 대마법사에 부츠의 색 `86 72 78`(대기·공격 원본 맨 아래에서 잰 회보라)을 적었다. `fit_pose.approved_place`가 그 값이 있으면 규칙 대신 쓴다. 다른 다섯 직업은 빈 값이라 전과 같은 색을 쓴다(확인).
- 다시 맞춘 결과(호출 없음): 공격 +20 → **+4**, 피격 +4, 붕괴 +7, 각성 +11(앞발이 원근으로 조금 낮게 그려진 만큼). 배율은 그대로(1.03 / 0.89 / 0.92 / 0.92). 잘못 맞춘 판은 `output/_before-r68-archmage/fit-wrong-boots/`(저장소 밖).
- 남는 차이(규칙대로): 게임의 대기 그림은 맨 아래 점(대마법사는 앞발)을 바닥에 두고, 자세는 뒷발을 바닥에 둔다. 그 차이는 직업마다 캔버스 5~17px(기사 17, 대마법사 12)로 모든 직업이 같고, 런지 자세는 머리가 내려가는 것이 의도다. 이 규칙은 바꾸지 않았다.
- Architecture/13 "후처리 (pose)"에 부츠의 색 줄, Roster 열 목록에 `Boots`.
- 검증: 체인 `20261015-171030` **CHAIN OK**(setup, EditMode 845/845, PlayMode 83/92, 9 skipped — 이번 판은 시드 의존 Test도 통과), 스크린샷 `20261015-r68e` 9/9. 게임 안의 비교 `game/attack-floor-compare.png`(대기 / 고치기 전 / 지금): 공격의 부츠가 1080p 화면에서 약 10px 올라와 대기와 같은 바닥에 선다. 남은 `Assets/InitTestScene*` 없음.
