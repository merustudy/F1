# Round 41 — 아이템 칸의 단계 표시: 등급 배지 삭제, 합친 단계를 더 멋있게

목업이다. 호출 없음. 16단계(맞추기)에 들어가며 사용자가 UI 개선을 먼저 하기로 했다.

## 지시

2026-10-06 사용자: "일단 ui 개선 먼저 할게. 아이템 칸에 등급 점수 동그라미로 표시되어있는데 삭제. 조합으로 상위 등급 표기를 더 멋있게 하고싶어.
더 바자르나 백팩배틀즈 등 유사게임은 이것을 어떤 장식으로 구현했는지 알아보고 우리에게 적용할 수 있는 안을 제안해줘", "위 사안을 검토 후 보고. 승인 후 구현".

- 등급 배지: 파티 쪽(노드 맵·보상) 칸의 왼쪽 아래 놋쇠 원(2026-10-03 목업 A안, Design/10 §5, Architecture/12 "파티 쪽"). 전투 칸에는 원래 없다.
  빼면 등급은 아이템 제목("단검 · 은 · 등급 8")과 설명 줄에만 남는다.
- 단계 표시: 지금은 Round 35 A(칸 안쪽 3에 단계 색의 평면 띠 4 + 안쪽 가는 선). 동은 아무 표시 없음. 이것을 더 멋있게.

## 레퍼런스 (작품의 자산은 쓰지 않는다. 특징만 글로 읽는다)

웹에서 확인한 것은 규칙 쪽이다. 모습의 세부는 페이지에 적혀 있지 않아 기억으로 적었고 그렇게 표시한다.

| 게임 | 올리는 방식 | 표시 (확인한 것 / 기억) |
|---|---|---|
| The Bazaar | 동·은·금·다이아(+전설). 같은 단계 둘을 사면 합쳐져 한 단계 위, 또는 사건으로 올림 | 확인: 단계는 색으로 구별하고 전설은 "빛나는 주황 테", 상인의 단계도 "초상의 틀"로 본다. 기억: 카드의 **틀 전체가 단계의 금속색**(구리·은·금·하늘빛)이고 올리면 틀이 바뀐다. 아이템 크기 소·중·대는 카드의 폭 |
| Backpack Battles | 희귀도 다섯(일반·희귀·영웅·전설·신성 + 고유). 레시피로 다른 아이템을 만든다(올리는 것이 아니라 바꾸는 것) | 확인: 희귀도의 목록. 기억: 희귀도는 이름의 색과 상점 카드의 색 테/빛으로, 가방 안의 아이템에는 틀이 없다. 레시피의 결과는 새 그림 |
| Teamfight Tactics | 같은 챔피언 셋 → 별 하나 위(1·2·3성, 세트에 따라 4성) | 확인: "모델 아래에 별", 1성 동색·2성 은색·3성 금색. 기억: 3성에 금빛 반짝임, 합칠 때 터지는 빛 |
| Super Auto Pets | 같은 펫을 겹치면 경험치, 2·3레벨 | 확인: 머리 위의 노란 경험치 막대 |
| Darkest Dungeon (기억) | 장신구의 희귀도 | 아이콘의 **틀 색**(회·초록·파랑·노랑·주황…)과 같은 색의 이름 |
| Slay the Spire (기억) | 카드 강화 한 번 | 이름이 초록 + "+" |
| Monster Train (기억) | 카드에 강화석 | 카드 아래에 **박힌 보석 아이콘** |
| Hearthstone (기억) | 황금 카드 | **움직이는 금 틀** (단계가 아니라 사치) |

출처: [The Bazaar Wiki: Items](https://thebazaar.wiki.gg/wiki/Items), [TV Tropes: The Bazaar](https://tvtropes.org/pmwiki/pmwiki.php/VideoGame/TheBazaar), [Mobalytics: The Bazaar Guide](https://mobalytics.gg/the-bazaar/guides/strong-start),
[Backpack Battles Wiki: Rarity](https://backpackbattles.wiki.gg/wiki/Rarity), [TFT Ninja: Star Levels](https://tft.ninja/guides/game-mechanics/champions/star-levels), [LoL Wiki: Champion (TFT)](https://leagueoflegends.fandom.com/wiki/Champion_(Teamfight_Tactics)),
[Super Auto Pets Wiki: Experience](https://superautopets.fandom.com/wiki/Experience).

읽은 것: 합쳐서 오르는 게임은 두 길이다. **틀이 재질이 된다**(The Bazaar: 단계 = 틀의 금속) 또는 **별을 센다**(TFT: 몇 번 합쳤는가). 우리의 디아블로 컨셉(검은 돌, 그을린 쇠, 낡은 금, 뼈색)에는 금속 틀이 그대로 어울리고,
별은 "합친 횟수"를 한눈에 센다. Backpack Battles는 합치기가 아니라 레시피라 결과물이 새 그림이고, 우리와는 길이 다르다.

## 안 (`mock-compare.png`: 파티 쪽과 전투, `mock-detail.png`: 2배 확대와 "합치기 → 은")

스테이징: 로언(1열) 롱소드 동, 단검 은(+1), 버클러 금(+1), 약초 주머니 다이아 / 카이 롱소드 금 / 미라 지팡이 은. 전투는 로언 은, 카이 금, 미라 다이아. 동은 어느 안에서나 지금 그대로(표시 없음). 세 안 모두 등급 배지가 없다.

| 안 | 모양 | 장단 |
|---|---|---|
| 지금 | 등급 배지 + 평면 띠 4 | 띠가 평면이라 "색 테두리" 이상으로 읽히지 않는다 |
| **1. 금속 테 (권장)** | 칸의 선 바로 안쪽에 **단계의 금속으로 된 틀**(6): 위·왼쪽이 밝고 아래·오른쪽이 어두운 경사, 바깥 먹선과 안쪽 가는 선, **네 모서리에 징**. 은은 강철, 금은 낡은 금. **다이아는 수정**: 하늘빛 띠에 흰 결(facet)이 가로지르고 모서리는 보석 | The Bazaar와 같은 어법(틀 = 단계), 디아블로 컨셉의 재질(쇠·금) 그대로. 표가 늘지 않아 칸이 깨끗하다. 전투의 어두운 칸 위에서도 틀이 선다. 합칠 칸은 다음 단계의 틀로 알린다. 은과 동의 차이가 "틀이 있다/없다"라 분명하다 |
| 2. 별 표 | 가는 테는 그대로 두고, 배지 자리(왼쪽 아래)에 **먹색 표에 별**: 은 ★, 금 ★★, 다이아 ★★★(별과 표의 테는 단계의 색). 오른쪽 위의 피로 표 "+1"과 쌍 | TFT처럼 몇 번 합쳤는지 센다. 색을 못 가려도 읽힌다. 다만 칸 양 모서리에 표가 둘(+1과 별)이라 번잡해지고, 멋보다 정보 쪽이다 |
| 3. 금속 테 + 별 표 | 1과 2를 합친 것 | 가장 풍성하지만 한 칸에 틀·별·+1 셋이 겹친다. 다이아 셋째 별이 아이콘의 손잡이를 덮는다 |

## 권장: 안 1

- 사용자가 바란 것은 "더 멋있게"다. 금속 틀은 장식이 늘어나는 것이 아니라 **지금 있는 띠가 재질을 얻는 것**이라 칸이 깨끗한 채로 좋아진다. 레퍼런스에서 가장 가까운 The Bazaar의 어법이기도 하다.
- 별(안 2·3)은 단계가 넷뿐이고 색·재질이 이미 단계를 말하므로 중복이다. 플레이에서 은·금이 안 가려진다고 느끼면 그때 별을 더하는 것이 순서다.
- 구현하며 바꿀 것: 목업의 경사는 1배에서 약하다. 실제 조각은 밝은 이음선을 더 세게 둔다(2배로 그린 조각이라 여유가 있다).

## 목업 2: 아이콘의 외곽선 (사용자 제안, 2026-10-06)

사용자: "테 말고 아이템의 추가 외곽선 색으로 표시해볼까? 등급이 커질수록 외곽선도 더 굵어지고 한번 목업 제공해줘" → `mock_outline.py` → `mock2-compare.png`(파티 쪽과 전투), `mock2-detail.png`(2배, 합칠 칸), `mock2-shapes.png`(모양이 다른 아이콘 넷).
외곽선은 아이콘의 먹선 **바깥**에 단계 색으로 두른 실루엣이다(아이콘의 알파를 원 둘레로 옮겨 합친 것. 런타임에도 같은 방법으로 그릴 수 있다). 동은 없음, 등급 배지 없음.

| 안 | 굵기 (은 · 금 · 다이아, 화면 px) | 메모 |
|---|---|---|
| **A (권장)** | 2 · 3 · 4 | 다이아 4가 칸의 위아래 여백(5)에 꼭 들어간다 |
| B | 2 · 4 · 6 | 더 굵다. 다이아 6은 긴 아이콘에서 칸의 선에 닿을 듯하다 |
| C | 2 · 3 · 4 + 바깥 빛(같은 색, 3px 번짐, 55%) | 1배에서는 빛이 거의 안 보이고 2배에서 후광이 된다 |

읽은 것:
- 장점: 단계가 **아이템 자체에** 붙어 "이 아이템이 올랐다"로 읽힌다(틀은 칸이 바뀐 것처럼 읽힌다). 칸은 깨끗하다. 굵기의 사다리가 자연스러운 진행이다. 합칠 칸은 다음 단계의 외곽선을 미리 둘러 알릴 수 있다.
- 단점: **전투**에서는 충전되지 않은 부분의 어둠이 아이콘과 함께 외곽선도 덮어, 충전된 부분에서만 보인다(Round 35의 가는 테는 어둠 위에 있어 늘 보였다). 은(#8C9CB2)은 뼈색 위에서 옅다 → 조금 짙은 강철색(예 #7A8AA0)이 낫다. 아이콘이 없는 아이템(지금 출고 데이터에는 없음)은 이름의 색으로 대신해야 한다.
- 구현(권장): 아이콘 뒤에 같은 Sprite의 **실루엣 Image** 하나를 두고, 작은 UI 셰이더(색은 꼭짓점 색, 알파는 텍스처의 알파)와 메시 효과(사각형을 반지름 r의 원 둘레 16곳 + r/2의 8곳으로 복제)로 두른다. 색과 굵기는 View가 단계로 정한다. 새 Asset은 셰이더와 재질 하나씩.
  대안: 아이콘마다 단계별 그림을 미리 그려 두는 것(20 × 3 = 60장, 주소 `item/<key>/<tier>`). 코드는 단순하지만 그림이 늘고 캔버스에 여백을 더해야 한다.

## 목업 3: 단계의 색 (2026-10-07)

사용자: "은 금 다이아가 잘못된 것 같아. 특히 다이아 색이 잘 안어울려. 3단계를 나눌 색상을 다시 제안해줘(예. 동 은 금색)". 틀은 2안·외곽선은 A안으로 두고 **색을 먼저 확정**한 뒤 그 색으로 목업을 보고 고르기로 했다.
은은 짙게(예), 전투의 어둠에 가려지는 것은 그대로 둔다. → `mock_palette.py` → `mock3-colors.png`: 팔레트마다 외곽선 A의 세 칸(뼈색), 그 아래 제목의 단계 글과 보상 카드의 띠(어두운 패널), 전투의 충전 중 칸 셋.

지금 색이 어긋나는 까닭: 그림체는 평면·낮은 채도(Design/10)인데 은 #8C9CB2·금 #E2A21E·다이아 #2EC4E8는 채도가 높고, 다이아의 하늘빛은 보호막(`Shield` #8FD3F4, `RimShield`)의 파랑과 겹친다.
쓸 수 없는 색상: 보라(피로의 연보라), 초록(`Good`: 출발·노드), 주황(화상), 빨강(피·빈사). 남는 것은 금속의 회색·금색, 흰 백금, 깊은 청록이다.

| 팔레트 | 은 · 금 · 다이아 (표시 / 글) | 읽은 것 |
|---|---|---|
| 지금 | #8C9CB2 · #E2A21E · #2EC4E8 / #D5DEEA · #F7C84A · #6FE3F8 | 채도가 높다. 다이아가 네온 같다 |
| P1 금속 셋 | 강철 #7F8B9B · 낡은 금 #D4A232 · 백금 #DCE6EA / #C6CFD9 · #F0C85A · #F1F7F9 | 모두 금속이라 통일된다. 백금은 뼈색 위에서 아이콘의 먹선 덕에 읽히지만, **글 #F1F7F9가 제목의 글색(#EBEBE6)과 같아져** 패널에서 단계가 안 보인다 |
| **P2 금속 둘 + 깊은 보석 (권장)** | 강철 #7F8B9B · 낡은 금 #D4A232 · 깊은 청록 #3E7F96 / #C6CFD9 · #F0C85A · #7FC4D8 | 낮은 채도로 팔레트에 들어오고, 회색·따뜻한 금·차가운 청록이 서로 멀어 밝기로도 색상으로도 가려진다. 이름(다이아)과 색(푸른 보석)이 맞는다. 보호막의 하늘빛보다 훨씬 짙다 |
| P3 구리 · 은 · 금 | #A8683A · #9AA7B8 · #D4A232 / #D59A66 · #D3DBE4 · #F0C85A | 사용자의 예시 그대로. TFT의 별(동·은·금)과 같은 셈. 다만 **이름을 바꿔야 한다**: 기본 단계는 이름 없음(표시 없음), 오른 셋이 동·은·금. 이름을 두고 색만 쓰면 '은'이 구리색이 되어 어긋난다 |

- 은은 요청대로 짙어졌다(#8C9CB2 → #7F8B9B, 푸른 기를 뺐다). 금은 놋쇠(`Brass` #B8944E)보다 밝게 두어 고른 칸(놋쇠 채움)과 겹치지 않는다.
- 금의 글 색은 각성(강인·집중)의 금빛 `UiPalette.Virtue`와 같은 값이다(12_UI "피로도와 붕괴의 상태"). 금을 바꾸면 각성도 따라가게 한다(권장. 하나의 금).
- P3을 고르면 Design/02 §4의 "동·은·금·다이아"와 `ItemTier`의 이름, 문구 `Item.Bronze~Diamond`, Architecture/07·08·12, Design/03·08의 글이 바뀐다(저장 파일의 값은 순서라 그대로). 색만 바꾸는 P1·P2보다 큰 변경이다.

## 판정 1 (2026-10-07): 색은 P3, 이름도 바꾼다

사용자: "P3로 반영.(추후 코드 구현 시 이름도 다 바꾸는 걸로) 테두리와 외곽선 목업 제공해줘".
→ 기본 단계는 표시 없음, 오른 셋이 **동 #A8683A · 은 #9AA7B8 · 금 #D4A232**(글 #D59A66 · #D3DBE4 · #F0C85A). 구현할 때 단계의 이름을 모두 바꾼다(아래 "구현").

## 목업 4: P3 색으로 두 후보 (`mock_final.py` → `mock4-compare.png`, `mock4-detail.png`)

| 후보 | 모양 | 읽은 것 |
|---|---|---|
| 테두리 2안 | Round 35의 가는 테(단계 색) + 왼쪽 아래 먹색 표에 별 ★(동)·★★(은)·★★★(금) | 별이 몇 번 올랐는지 센다(TFT). 전투의 어둠 위에 있어 늘 보인다. 다만 "+1"과 별 표가 양 모서리에 서서 칸이 번잡하고, 은의 가는 테는 뼈색 위에서 옅다 |
| **외곽선 A안 (권장)** | 아이콘의 먹선 바깥에 단계 색의 외곽선 2·3·4 | 표시가 아이템 자체에 붙어 "이 아이템이 올랐다"로 읽힌다. 구리·은·금의 색과 굵기의 사다리가 함께 세 걸음을 말해 별이 없어도 센다. 칸은 "+1"만 남아 깨끗하다. 전투에서는 충전되지 않은 어둠에 함께 덮인다(그대로 두기로 함) |

## 목업 5: 외곽선 + 별 (사용자 지시, 2026-10-07. `mock_final.py` → `mock5-compare.png`, `mock5-detail.png`)

사용자: "외곽선 + 왼쪽아래 별 표시 목업 제공해줘" → 외곽선 A(2·3·4)에 테두리 2안의 별 표(왼쪽 아래 먹색 알약, 테는 단계 색, 별 ★·★★·★★★)를 더했다. 비교로 테두리 2안과 외곽선 A안을 같은 시트에 두었다.
- 읽은 것: 외곽선이 "이 아이템이 올랐다"를, 별이 "몇 번"을 말한다. 전투에서 외곽선이 어둠에 덮여도 별 표는 어둠 위에 있어 단계가 늘 읽힌다(외곽선 A안만의 약점을 별이 메운다).
  칸에는 "+1"과 별 표가 양 모서리에 서지만 테가 없어 테두리 2안보다 덜 번잡하다. 금의 ★★★는 긴 아이콘의 손잡이 끝을 조금 덮는다(지금의 등급 배지도 그랬다).

## 판정 2 (2026-10-07): 외곽선 + 별, 기본 단계의 이름은 일반 — 구현

사용자: "1. 이 안으로 결정 2. 권장대로 구현" → 외곽선 A(2·3·4) + 왼쪽 아래 별 표, 색 P3, 단계의 이름 **일반·동·은·금**(기본은 글에만 "일반"), 등급 배지 삭제.

## 구현 (2026-10-07)

- **이름**: `ItemTier` Common·Bronze·Silver·Gold(값의 순서 그대로). `BalanceData` `TierBronzePercent`·`TierSilverPercent`·`TierGoldPercent`, `DungeonData` `BronzeFloor`·`SilverFloor`·`GoldFloor`(CSV 머리글과 변환기, Generated 다시),
  시뮬 보고 "common, bronze, silver, gold", 문구 `Item.Common`(일반/Common. 제목에는 적지 않음: `Item.TitlePlain` "{0} · 등급 {1}", `Item.MergeHintPlain`)·`Bronze`·`Silver`·`Gold`(`Item.Diamond` 삭제), `UiText.ItemTitle`·`MergeHint`.
  **저장**: `run.json` 버전 7(`From6To7`: Bronze → Common, Silver → Bronze, Gold → Silver, Diamond → Gold. 값은 이름이라 바꿔야 했다). `From4To5`는 5의 이름 "Bronze"를 그대로 쓴다(`Version5Bronze`).
  Test(`TierRulesTests`, `RunSaveMapperTests`(+ `Migrate_From6_RenamesTheTiers`), `CampFlowTests`, `StaticDataTransformerTests`, TestCsv·TestData)와 문서(Design/00·02·03·08·10, Architecture/01·07·08·12·13)의 이름.
- **색**: `UiPalette.TierBronze` #A8683A·`TierSilver` #9AA7B8·`TierGold` #D4A232(`TierMark(tier)`, 일반은 투명), 글 `TierCommonText` #C9C2B0·`TierBronzeText` #D59A66·`TierSilverText` #D3DBE4·`TierGoldText` #F0C85A(`TierText`). 각성의 `Virtue`는 새 금의 글 색을 따른다.
- **등급 배지 삭제**: `UiPrefabSetup.PartySide.BuildItemSlot`의 `KitBadge`와 `ItemSlotView`의 `_badge`·`_grade`·`Grade`, `Kit`의 `GradeBadge*`·`KitBadge`. Test는 배지 대신 `TierShown`을 본다.
- **외곽선**: 아이콘 뒤의 실루엣 Image(같은 Sprite, `Assets/@Prefabs/UI/Silhouette.mat` ← 셰이더 `Assets/@Shaders/Silhouette.shader`: 꼭짓점 색에 그림의 알파, UI/Default의 클리핑과 premultiplied 블렌딩)에
  `SilhouetteOutline`(BaseMeshEffect: 반지름 r의 원 둘레 16곳 + r/2의 8곳으로 메시 복제). 재질은 `UiPrefabSetup.SilhouetteMaterial()`이 없으면 만든다(생성물, Prefab이 참조). 굵기는 `TierStyle.Outline`(2·3·4).
  전투에서는 `ItemUnder`(충전의 금빛) 다음, 아이콘 앞에 있어 어둠 아래다. 적의 아이콘처럼 좌우가 뒤집히고 발동의 튐을 따라간다(`BattleItemView.ScaleIcon`).
- **별 표**: 그린 조각 `tier_tag`(먹 알약, 흰 테 → 단계 색)와 `star`(흰 별, 먹 외곽선 → 단계의 글 색), `TierStyle.Stars`(1·2·3)와 치수(높이 20, 여백 6, 별 12, 간격 13 → 폭 24·37·50). `Kit.BuildTierTag`가 파티 쪽과 전투의 칸에 같은 것을 둔다(전투에서는 어둠 위).
  합칠 칸(`합치기 → 동`)은 다음 단계의 외곽선과 별을 미리 두르고 글은 표에서 6 띄운다(`MergeMarkLift`). 아이콘이 없는 아이템은 이름의 글이 단계 색이다.
- View의 질의(Test용): `ItemSlotView`·`BattleItemView`의 `TierShown`(ItemTier?), `Stars`, `OutlineColor`; `ItemSlotView.OutlineThickness`. `TierStyleTests`(EditMode) 셋.
- 조각: `draw_pieces.py` → `Assets/@Art/UI/Frame/tier_tag.png`(64x40, Border 20), `Assets/@Art/UI/Icon/star.png`(24x24). `tier_rim.png`은 지웠다.

## 검증 (2026-10-07)

- `dotnet build Tools/Sim` 오류 0, `transform`(BalanceData·DungeonData.json 다시)·`validate` OK. 시뮬 3,000회 64.0% / 0.35 — 이름만 바뀌고 수치 그대로.
- 체인 1: setup OK, sim OK, EditMode 717/723(실패 여섯: `RunSaveMapper`의 포션 보상 검증이 옛 `ItemTier.Bronze`(= 기본)로 남아 있던 것 → `Common`), PlayMode 73/73(9 skipped).
  체인 2(editmode): **723/723**. 체인 4(playmode, 고친 코드로): **73/73**(9 skipped). 마지막 setup·editmode(Stamp)는 Handoff `b16-tuning.md` "Verification".
- 스크린샷 `20261007-r41`(9/9, PNG 63): `game/ko_35_map_tiers_implemented.png`(은 ★★·금 ★★★의 외곽선, "합치기 → 동"의 동 ★과 동 외곽선, 제목 "단검 · 등급 8", 안내 "같은 단검 위에 놓으면 합쳐서 동 하나가 됩니다."),
  `ko_35_cells_zoom_implemented.png`(2배), `ko_34_camp_mend_implemented.png`("롱소드 일반 → 동"), `ko_07_reward_implemented.png`(일반 보상 카드에 단계 없음). 전투의 모습은 `ko_09_boss_battle_implemented.png`(맨 뒤 구성원의 동 무기).

## 판정할 것 (모두 정해졌다)

1. 등급 배지 삭제: 확정(지시). 등급은 제목과 설명 줄에만 남는다.
2. 단계 표시: **외곽선 + 별**(목업 5). 색은 **P3**(목업 3), 이름은 일반·동·은·금. 은은 짙게(예), 전투의 어둠에 가려지는 외곽선은 그대로 둔다.
3. 동(지금의 일반)의 표시: 없음(Round 35의 결정 그대로).
4. 기본 단계의 이름: **일반**(`Common`). 아이템 제목에는 적지 않는다.

## 고르면 (구현. 안 1 기준 — 목업 1의 기록)

- 조각: `Archive/41-tier-marks/draw_frames.py`가 `Assets/@Art/UI/Frame/tier_silver.png`·`tier_gold.png`·`tier_diamond.png`를 2배로 그린다(9-slice. 모서리 징이 Border 안에 들어가 늘어나지 않는다. 다이아의 결은 늘이지 않고 **타일**로 깐다: `UiArt.Piece(tiled)`).
  `tier_rim.png`(+`.meta`)은 지운다. `UiArt`에 셋을 올리고 `TierRim`을 뺀다.
- 화면: `ItemSlotView`·`BattleItemView`의 `_tierRim`이 색이 아니라 **단계의 Sprite**를 보인다(Builder가 셋을 참조로 넣음. 색은 흰색). Test가 보는 `TierRim`(색)은 `TierShown`(단계)으로.
  합칠 칸의 표시는 다음 단계의 틀. `UiPalette.Tier*`(테 색)는 보상 카드의 띠(`RewardOptionView.Stripe`)와 글에 남는다.
- 등급 배지 삭제: `UiPrefabSetup.PartySide.BuildItemSlot`의 `KitBadge`와 `_badge`·`_grade`, `ItemSlotView.Grade`, `Kit`의 `GradeBadgeSize`·`GradeBadgeInset`, `KitBadge`(쓰는 곳이 없어지면 함께).
  `UiFlowTests`의 배지 확인(1336·1343행) → 배지가 없음과 제목의 등급으로.
- 문서: Design/10 §5(파티 쪽 칸의 등급 배지 【확정】을 이 결정으로 바꿈), Architecture/12 "파티 쪽"·"아이템의 아이콘"·"UI의 그림"·Test, 13 "후처리 (`frame`, `glyph`)"의 그린 조각 목록. Roadmap(사후 검토 대기: 구현에서 정한 세부), Handoff `b16-tuning.md`.
- 체인과 스크린샷(`_35_map_tiers`, `_07_reward`, 전투에 단계가 있는 장면은 Test가 세운다).
