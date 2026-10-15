# Round 69 — 아이템 단계의 이름(일반·동·은·금)과 표시 방식 (2026-10-15, 검토)

목업이다. 호출 없음, 게임의 그림·코드 변경 없음. 직접 플레이 전의 UI 검토.

## 지시

2026-10-15 사용자: "아이템 등급에 따른 금/은/동 이라는 표현 개선과 표시방식 개선안을 먼저 수행하자. 유사 게임 및 네 생각 검토 후 개선안 제안해줘".

- "등급"은 지금의 **단계**(일반·동·은·금, Design/02 §4)로 읽었다. 등급(grade, 효과 크기의 수)은 22단계에 없앴다.

## 지금

- 규칙(Design/02 §4): 아이템은 **일반**으로 나오고 같은 아이템·같은 단계 둘을 합치면 한 단계 위(동 → 은 → 금). 야영지의 정비도 한 단계 올린다. 깊은 층의 전리품·상점 물건은 처음부터 위 단계로 나온다(`BronzeFloor`·`SilverFloor`·`GoldFloor`, 정예는 한 단계 위).
  효과 크기는 단계의 퍼센트(100·200·300·400)다. 시뮬: 합치기 3.27/원정, 15층 야영지에 닿은 원정의 95%가 금을 가진다(README 59 "재검토").
- 이름의 경위: 처음 The Bazaar의 동·은·금·다이아 → Round 41(2026-10-07)에 사용자가 색 P3(구리·은·금)를 고르며 **일반·동·은·금**으로.
- 표시(Architecture/12 "단계"):
  - 조각의 **왼쪽 아래 별** 12px: 동 ★, 은 ★★, 금 ★★★. 색은 Round 49부터 **디아블로의 희귀도 색**(`UiPalette.Rarity`: 동 매직 파랑, 은 레어 노랑, 금 유니크 금). 전투 보드에도.
  - 제목 "이름 · 단계"(`Item.TooltipTitle`): 툴팁·바닥 이름판·전투 카드의 제목은 희귀도 색, 단계 글자는 `TierText`(구리·은·금).
  - 정비 창 "롱소드  일반 → 동", 합치기 표시 "합치기 → 동", 안내 "같은 단검·동 위에 놓으면 합쳐서 은 하나가 됩니다."
  - Round 41의 아이콘 외곽선은 Round 49부터 그리지 않는다(부품만 남음: 리팩토링 R12. Design/10 §5 126행은 아직 외곽선이라 적혀 있어 낡았다).

## 레퍼런스 (작품의 자산은 쓰지 않는다. 특징만 글로)

웹에서 확인한 것은 "확인", 페이지에 없어 기억으로 적은 것은 "기억"으로 표시한다.

| 게임 | 무엇을 올리나 | 이름 | 표시 |
|---|---|---|---|
| The Bazaar | **같은 아이템 둘 → 한 단계 위**, 레벨 보상·사건으로도 | 확인: 네 "quality" bronze·silver·gold·diamond, 다이아는 더 못 올림 | 확인: 올릴 수 있는 아이템에 "위로 흐르는 갈매기표(chevron)" 오버레이. 기억: 카드의 틀이 단계의 금속색 |
| Teamfight Tactics | **같은 챔피언 셋 → 별 하나 위**(1·2·3성, 세트에 따라 4성) | 확인: "1-star / 2-star / 3-star" | 확인: 2성은 "silver unit", 3성은 "gold unit"(tracker.gg). 모델 아래의 별(Round 41에서 확인) |
| Super Auto Pets | 같은 펫 겹치기 → 경험치, 2·3레벨 | 확인: Level 1·2·3 | 확인: 머리 위 경험치 막대(Round 41) |
| Slay the Spire | 카드 강화 한 번(Searing Blow만 거듭) | 확인: 이름 끝의 **"+"** | 확인: 이름이 **초록** |
| Backpack Battles | 레시피로 **다른** 아이템을 만듦(올리는 것이 아님). 희귀도 다섯 + 유니크 | 확인: Common·Rare·Epic·Legendary·Godly | 기억: 이름 색과 상점 카드의 테. 가방 안의 아이템에는 틀 없음 |
| Brotato | 희귀도(다른 아이템) | 확인: **Tier 1~4, 글에서는 "Tier I"**(로마 숫자) | 기억: 회·파랑·보라·빨강 테 |
| Diablo 2 | 품질(다른 아이템) | Normal·Magic·Rare·Set·Unique | 확인: Set 초록·Unique 금. 기억: Normal 흰·Magic 파랑·Rare 노랑 |
| Darkest Dungeon | 장신구 희귀도(다른 아이템). 대장간의 무기·방어구는 레벨 1~5 | 확인: 희귀도는 "색과 설명으로 구별" | 기억: 영웅 화면의 무기·방어구 레벨은 작은 눈금 |
| Hades | 축복 희귀도 | 확인: Common·Rare·Epic·Heroic | 확인: 흰·파랑·보라·빨강 |
| 메이플스토리 / 메이플스토리2 | 장비 강화 | 확인: 스타포스 **"N성"** / 인챈트 **"N단계"** | — (기억: 옛 주문서 시대의 "+N") |

출처: [The Bazaar Wiki: Items](https://thebazaar.wiki.gg/wiki/Items), [Mobalytics: Level-up rewards](https://mobalytics.gg/the-bazaar/guides/level-up-rewards), [LoL Wiki: TFT Champion](https://wiki.leagueoflegends.com/en-us/TFT:Champion),
[TRN: How to upgrade champions in TFT](https://tracker.gg/checkpoint/articles/how-to-upgrade-champions-in-teamfight-tactics-tft), [Super Auto Pets Wiki: Experience](https://superautopets.wiki.gg/wiki/Experience), [Slay the Spire Wiki: Upgrade](https://slaythespire.wiki.gg/wiki/Upgrade),
[Backpack Battles Wiki: Rarity](https://backpack-battles.fandom.com/wiki/Rarity), [Brotato Wiki: Items](https://brotato.wiki.spellsandguns.com/Items), [PureDiablo: Set](https://www.purediablo.com/d2wiki/Set), [Darkest Dungeon Wiki: Trinkets](https://darkestdungeon.wiki.gg/wiki/Trinkets_(Darkest_Dungeon)),
[Hades Wiki (Fextralife): Boons](https://hades.wiki.fextralife.com/Thunder+Dash), [메이플스토리 스타포스 안내](https://maplestory.nexon.com/news/update/358?page=44), [메이플스토리2 인챈트 소식](https://www.gamemeca.com/view.php?gid=927150).

## 읽은 것

레퍼런스는 두 갈래다.

1. **같은 것을 겹쳐 올리는** 게임은 **센다**: TFT는 성(별), Super Auto Pets는 레벨, Slay the Spire는 "+", 메이플은 성·단계. 이름이 "몇 번 올랐나"를 그대로 말한다.
2. **다른 아이템의 품질·희귀도**를 가르는 게임은 **말과 색**을 쓴다: 디아블로의 매직·레어·유니크, Hades·Brotato·백팩 배틀즈의 색 사다리.

The Bazaar만 합치기 게임이면서 금속의 말(동·은·금·다이아)을 쓰고, 그것도 "quality"라 부르며 상점이 은 아이템을 바로 판다. 우리는 거기서 이름을 받아 왔다. 그런데 22단계에 등급을 없앤 뒤 우리의 단계는 **1갈래**(같은 것을 합친 횟수, 셋까지)이고, 이름은 2갈래의 말이다. 사용자가 느낀 어긋남은 여기서 온다고 본다.

지금의 이름과 표시에서 찾은 문제(`mock-marks.png`의 "지금" 줄):

| # | 문제 | 어디서 |
|---|---|---|
| 1 | **금속의 말은 재료로 읽힌다.** "단검 · 은"은 은으로 만든 단검(판타지의 은 무기), "금 롱소드"는 금칼처럼 읽힌다. 한 단계 위라는 뜻은 배워야 안다 | 툴팁·카드의 제목, 안내 문장 |
| 2 | **메달의 서열이 거꾸로 느껴진다.** 첫 상승이 "동"(꼴찌 메달)이고, "일반 → 동"은 올라간 느낌이 약하다. 일반은 금속도 아니라 사다리가 섞여 있다 | 정비 창, 합치기 표시 |
| 3 | **말과 색이 어긋난다.** 말은 동(구리)·은·금인데 조각의 별과 제목은 Round 49의 디아블로 희귀도 색(동 = **파랑** ★, 은 = **노랑** ★★). 정비 창·합치기 표시는 구리·은·금 글. 한 뜻에 팔레트 둘 | 조각의 별, 툴팁 제목 vs 정비 창 |
| 4 | **★이 겹친다.** 20단계의 숫돌 ★(칸 위의 큰 별, 켜짐·빈 별)과 단계의 ★(조각 모서리의 작은 별)이 같은 보드에 같은 글자다. 카드도 "(★ 숫돌 +1)"과 "은 ★★"이 한 장에 있다 | 보드(숫돌을 들거나 가리킬 때), 카드 |
| 5 | **금은 특별하지 않다.** 코인, 놋쇠 테, 켜진 별, 각성의 글, 디아블로의 금 제목이 모두 금빛이다. "금 단계"의 색이 UI 어디에나 있다 | 전체 |
| 6 | **작다.** 12px 별이 아이콘 아래에 겹쳐 1배에서 잘 안 읽힌다(동의 파란 별 하나는 남색 바탕에 묻힌다) | 보드, 전투 보드 |

바뀌지 않는 것: 규칙(단계 넷, 합치기, 정비, 층의 단계), 데이터, 저장 형식, 시뮬. 이름과 표시만의 문제다.

## 안 1: 이름 (`mock-words.png` — 툴팁 제목, 카드·이름판의 제목, 정비 창, 합치기 표시와 안내, 네 단계 나란히)

| 안 | 네 단계 | 제목 | 읽은 것 |
|---|---|---|---|
| 지금 | 일반·동·은·금 | 단검 · 은 | 위 문제 1·2·3·5 |
| **1. 로마 숫자 (권장)** | I·II·III·IV (I은 제목에 안 적음) | **단검 III** | 센다(TFT·Brotato의 어법). 재료로 읽히지 않고, 색이 없어도 읽히며, 금속색은 보조로 남길 수 있다. "단검 2"는 개수로 읽히지만 "단검 II"는 판(版)·단계로 읽힌다(FF의 파이어 II, Brotato의 Tier I). 위아래가 한눈에 보여 "합치기 → III"이 "무엇이 되나"를 바로 말한다 |
| 2. 강화 +N | 일반·+1·+2·+3 | 단검 +2 | 한국 게임의 가장 익숙한 말. 다만 조각의 **오른쪽 위 "+1"(장비 피로)**과 카드의 "★ 숫돌 +1"(피해)에 "+N"이 이미 둘 있어 한 조각에 세 가지 +N이 선다. 피로 표를 바꾸지 않는 한 보드에서 겹친다 |
| 3. N성 | 1성·2성·3성·4성 | 단검 3성 | TFT·메이플의 어법이라 익숙하고 센다. 다만 별 글자가 **숫돌의 ★**과 겹친다(문제 4가 말에까지 번진다). 백팩 배틀즈의 ★을 그대로 쓰기로 한 Round 51과 부딪힌다 |
| 4. 품질의 말 | 일반·상급·특급·명품 (예) | 단검 특급 | 분위기는 있으나 **세지 못한다**(특급 > 상급을 배워야 함). 셋뿐이라 배울 수는 있지만 "합쳐서 무엇이 되나"가 숫자보다 느리다. 담금질·벼림·정비는 야영지·G18이 이미 쓰는 말이라 못 쓴다 |

- 권장 1의 세부: **일반은 "I"이되 제목에는 적지 않는다**(지금 "일반"을 안 적는 것과 같다. "단검"은 I, "단검 II"부터 적음). 정비 창만 "롱소드  I → II"로 둘 다 적는다. 합치기 안내는 "같은 단검 II 위에 놓으면 합쳐서 단검 III 하나가 됩니다."
- 영어도 같다: "Dagger III", "Longsword  I → II", "Merge → III".

## 안 2: 조각의 표 (`mock-marks.png` — 노드 맵의 보드 네 줄(지금·A·B·C), 2배 상세, 전투 보드. 숫돌에 마우스를 올린 상태로 ★ 겹침을 함께)

스테이징: 로언 — 롱소드 II, 단검 III(+1), 숫돌(가리킨 상태: 롱소드 아래의 ★ 켜짐, 아래 칸은 빈 별), 약초 주머니 IV(+1). 카이 — 롱소드 I, 단검 II(든 단검이 합쳐질 조각: "합치기 → III"), 버클러 III. 색은 모두 금속 사다리(P3) 하나.

| 안 | 모양 | 읽은 것 |
|---|---|---|
| 지금 | 왼쪽 아래 ★ 1~3, 디아블로 희귀도 색 | 숫돌의 큰 금 ★ 옆에 작은 파랑·노랑 ★이 서서 같은 글자가 두 뜻이다. 동의 파란 별 하나는 1배에서 묻힌다 |
| **A. 로마 숫자 표 (권장)** | 왼쪽 아래 먹색 표(테는 단계의 금속색)에 **II·III·IV**(금속색 글, 12px) | 제목과 같은 글자라 **읽은 것과 보는 것이 같다**. 1배에서 읽히고 전투 보드(어둠 위, 작게)에서도 읽힌다. 별이 없어져 숫돌의 ★이 보드의 유일한 별이 된다. 표가 아이콘 아래를 조금 덮는 것은 지금 별과 같다. 이름 안 2·3을 고르면 같은 표에 "+2"·"3성"을 적으면 된다 |
| B. 눈금 ◆ | 왼쪽 아래 작은 마름모 1~3개(금속색) | 센다, 글자 없음, ★과 안 겹침. 다만 **작아서** 1배에서 구리 하나가 남색에 묻히고 전투에서는 점이다. 세려면 눈을 가까이 대야 한다 |
| C. 조각의 금속 테 | 조각 둘레 2px의 금속 테(The Bazaar의 틀) | 가장 멋있고 조각이 깨끗하다. 다만 **색만의 표시**라 구리(II)와 금(IV)이 한눈에 갈리지 않고, 고른 조각(밝은 남색)·밀려날 조각(어두운 금 바탕)·초록·빨강 그림자와 겹치며, 전투의 어두운 아이콘에 테만 밝다. 말(II·III·IV)은 어차피 제목에 있어야 하므로 A의 보조 장식으로 더하는 것은 된다(A + C) |

## 권장

- 이름 **안 1**(로마 숫자), 표 **안 A**(숫자 표), 색은 **금속 사다리 하나**(`TierText` 구리·은·금. 툴팁·바닥 이름판·전투 카드 제목의 디아블로 희귀도 색 `UiPalette.Rarity`는 뺀다 — 동의 파란 제목이 툴팁의 파란 효과 줄과 같은 색이기도 하다).
  A + C(테까지)는 직접 플레이 뒤 "더 멋있게"가 필요하면 그때.
- 코드·데이터·저장의 이름(`ItemTier.Common·Bronze·Silver·Gold`, `TierBronzePercent`, `BronzeFloor`, run.json의 `"Bronze"`, 시뮬 보고의 bronze/silver/gold)은 **그대로** 둔다: 내부 식별자이고 바꾸면 저장 12(변환)와 CSV 머리글·시뮬·문서가 함께 움직인다. 화면의 말만 바꾼다(문구 네 줄과 제목 형식).
  Round 41 때는 이름이 뜻까지 바뀌어(동 → 일반) 코드도 바꿨지만, 이번은 같은 네 단계를 다른 말로 부르는 것이라 식별자를 둘 이유가 있다.

## 구현하면 (승인 뒤. 지금은 하지 않았다)

- 문구(`UI_StaticText.csv`): `Item.Common` I / `Bronze` II / `Silver` III / `Gold` IV(en 같음), `Item.TooltipTitle` "{0} {1}", `Item.MergeHint` "같은 {0} {1} 위에 놓으면 합쳐서 {0} {2} 하나가 됩니다.", `MergeHintPlain` "같은 {0} 위에 놓으면 합쳐서 {0} {1} 하나가 됩니다.", `Board.MergeInto` 그대로.
- 코드: `TierStyle`(별 수 → 표의 글과 치수), `ItemSlotView.ShowTier`(별 Image 셋 → 표의 TMP 글 하나), Builder `BuildItemSlot`(표 안의 글, 별 셋 삭제), `UiPalette.Rarity` 삭제 → `ItemTooltipView`·`LootDropView`·`UiText.TooltipLines`·`ItemSlotView._text`는 `TierText`. Prefab Stamp.
- Test: PlayMode의 `Stars`·`StarColor` 검사(UiFlowTests 1368~1407) → 표의 글, EditMode의 제목·안내 Test. 스크린샷 `_35_map_tiers`·`_08`·`_40`·`_49`로 대조.
- 문서: Design/02 §4(이름 줄에 【확정】 추가), Design/10 §5 126행(낡은 외곽선 줄 → 숫자 표), Architecture/12 "단계"·"툴팁"·"인벤토리 팝업"의 희귀도 색 줄, Roadmap(사후 검토 "Round 69의 세부"), Handoff.
- **R12가 이 라운드의 계기다**(b16 "리팩토링 검토": 꺼진 단계 외곽선 부품 `SilhouetteOutline`·`TierStyle.Outline`·`ItemSlotView`의 외곽선 필드·`UiPalette.TierMark`의 외곽선 용도·툴팁 띠): 첫 커밋으로 한다.
- 순서: R10(작업 트리에 있음, `UiFlowTests`를 같이 고친 상태) → R12 → Round 69. 호출 없음, 저장 형식 그대로.

## 판정할 것

| # | 질문 | 선택지 | 권장 |
|---|---|---|---|
| 1 | 단계의 이름 | 1 로마 숫자 / 2 +N / 3 N성 / 4 품질의 말 / 지금 유지 | **1** |
| 2 | 일반(I)의 표기 | 제목에서 뺌, 정비 창만 "I → II" / 늘 "단검 I"까지 적음 | **제목에서 뺌** |
| 3 | 조각의 표 | A 숫자 표 / B 눈금 / C 금속 테 / A + C | **A** |
| 4 | 색 | 금속 사다리 하나(툴팁·이름판의 디아블로 희귀도 색을 뺌) / 디아블로 색 유지(Round 49 V3) | **금속 하나** |
| 5 | 코드·데이터·저장의 식별자 | 그대로(화면 말만) / 코드까지 바꿈(저장 12) | **그대로** |
| 6 | R12를 첫 커밋으로 | 예 / 나중에 | **예** |
| 7 | 언제 | 지금 구현 / 직접 플레이 뒤 | **지금**(플레이에서 새 표를 함께 본다) |

## 판정 (2026-10-15)

사용자: **"1. n성 - 일반 1성 2성 3성 / 2. 제목에서 뺌 / 3. B안 - 외곽선 및 도형의 크기는 현재 별과 동일하게, 색상(1성 - 현재 '동'의 파랑, 2성-은색, 3성-현재의 '은' 노랑색) / 4. 디아블로 색 유지 / 5. 코드까지 바꿈 / 6. 예 / 7. 지금구현"**, 이어서 **"구현 후 목업 제공해줘"**.

- 1 이름: **일반·1성·2성·3성**(권장 1 로마 숫자 대신 안 3의 변형: 일반은 "1성"이 아니라 일반). 2: 제목에서 일반을 뺌(권장안). 3 표: **B 눈금**, 크기와 외곽선은 별(12px, 먹 테)과 같게, 색은 1성 파랑·2성 은·3성 노랑(권장 A 숫자 표 대신).
  4 색: 디아블로 색 유지. 5: **코드·데이터·저장까지**(권장 "화면의 말만" 대신). 6 R12 첫 커밋(권장안). 7 지금 구현(권장안).
- 판정대로 다시 그린 목업: `mock-chosen.png`(`mock_chosen.py`).
- 빈 곳을 권장안으로 정한 것(사후 검토, Roadmap "Round 69의 세부"):
  - **한 팔레트**: 눈금의 색이 2성 은·3성 노랑으로 정해졌으므로 제목·정비 창·합치기 표시·툴팁·바닥 이름판의 글도 같은 색(`UiPalette.Rarity`: 1성 `#7070FF`·2성 `#D3DBE4`·3성 `#FFFF6E`). "디아블로 색 유지"는 "희귀도 색을 글에 그대로"로 읽었고, Round 49의 유니크 금 제목(`#C7B377`)은 3성이 노랑이 되어 뺐다.
    말과 표가 다른 색이면 Round 69의 문제 3이 되살아나서다. 글을 Round 49의 파랑·노랑·금으로 두고 싶으면 `Rarity`의 두 값만 되돌리면 된다.
  - 2성의 은색은 Round 41 P3의 은 글 색 `#D3DBE4`(남색 바탕과 검은 툴팁 양쪽에서 읽힘).
  - 코드 이름은 `ItemTier.Common`·`Tier1`·`Tier2`·`Tier3`(숫돌 ★의 `StarRules`·`StarDamage`·`GridStarView`와 안 겹치게 Star를 피함). CSV 키 `Tier1Percent`~`Tier3Percent`, `Tier1Floor`~`Tier3Floor`. 저장은 **12**(`From11To12`: 아이템·전리품·상점 물건의 이름만). 시뮬 보고는 "common, 1-star, 2-star, 3-star".
  - 영어는 1-star·2-star·3-star("Dagger 2-star"). 제목 형식은 "{0} {1}"(가운뎃점 없음). 안내는 "같은 단검 1성 위에 놓으면 합쳐서 2성 하나가 됩니다."
  - `UiPalette.TierText`(구리·은·금 글 색)를 뺐다. 각성의 금빛 `Virtue`는 제 값 그대로다(그 전에는 금의 글 색과 같은 값이었다).

## 구현 (2026-10-15)

- 먼저 **R12**(별도 커밋 `3ce9129`): Round 49부터 꺼져 있던 외곽선 부품(`SilhouetteOutline`, Builder의 `BuildOutline`, 두 View의 외곽선 Image·`OutlineColor`, `TierStyle.Outline`, `UiPalette.TierMark`·`TierBronze/Silver/Gold`, 툴팁 띠)을 뺐다. 실루엣 셰이더·재질은 발동 섬광과 무대 실루엣이 써서 그대로.
- 데이터: `DataEnums.ItemTier` Common·Tier1·Tier2·Tier3, `BalanceData` `Tier1Percent`~, `DungeonData` `Tier1Floor`~(매퍼·CSV 머리글·Generated), `BattleTypes`·`ExpeditionRules`의 마지막 단계 검사.
- 저장: `RunSaveData.CurrentSchemaVersion` 12, `RunSaveMigrator.From11To12`(보드·인벤토리 아이템, 전리품·상점 물건의 `Tier` 이름), `From6To7`은 7의 이름을 글자 그대로 쓴다(그 전에는 `nameof`).
- 화면: 문구 `Item.Tier1`~`Tier3`(1성·2성·3성 / 1-star~), `Item.TooltipTitle` "{0} {1}", `Item.MergeHint`; `UiKeys`·`UiText.TierName`·`TierWord`(색은 `Rarity`); `UiPalette.Rarity` 새 값과 `TierText` 삭제;
  `TierStyle.Pips`·`PipSize`·`PipPitch`·`MostPips`; `ItemSlotView`·`BattleItemView`의 `_pips`·`Pips`·`PipColor`; Builder `BuildTierTag`가 `UiArt.TierPip`로 눈금 셋; 새 조각 `Assets/@Art/UI/Icon/tier_pip.png`(`draw_pieces.py`, 24x24 흰 마름모에 먹 외곽선 — 별과 같은 캔버스·테).
- 시뮬: 보고의 단계 이름. Test: EditMode의 이름(`TierRulesTests`·`StarRulesTests`·`ShopRulesTests`·`ExpeditionRulesTests`·`CampFlowTests`·`StaticDataTransformerTests`·`TestCsv`·`TestData`), `TierStyleTests`(눈금), `RunSaveMapperTests`(`Migrate_From6`의 기대값이 12의 이름, 새 `Migrate_From11_RenamesTheTiers_OnItemsAndOffers`), PlayMode `UiFlowTests`(`Pips`·`PipColor`)·`UiScreenshotTests`.
- 문서: Design/02 §4(【확정】 Round 69)·03 §1·§5·00·08·10 §5, Architecture/07(저장 12)·08·09·12("단계")·13(`tier_pip`), Roadmap("Round 69의 승인", "Round 69의 세부"), Handoff.
- 호출 없음. 규칙·수치는 그대로다(시뮬의 수는 바뀌지 않는다: 이름만).

## 검증 (2026-10-15)

- 체인 `20261015-230612`(전체): setup OK(Generated 둘·String Table 셋·Prefab 둘·새 조각 `tier_pip`의 Import), sim OK(이름만 바뀌어 수는 그대로), EditMode 846/847 — 실패 하나는 새 Test `Migrate_From11_…`의 꾸밈 오류(그 판의 인벤토리에 주운 전리품이 있어 `[0]`이 더한 단검이 아니었고, 더한 단검이 (0,0)에서 겹침) → 변수로 잡고 빈 칸 (7,2)에 둠, PlayMode **83/92**(9 skipped, inconclusive 0 — 눈금·색·문구의 Test 통과).
  EditMode 다시 `20261015-231902`(꾸밈 하나 더 고침) → `20261015-232214` **847/847**.
- 스크린샷 `20261015-231913` 9/9(PNG 86). 확정 목업(`mock-chosen.png`)과 요소마다 대조(`game/impl-compare.png`):
  - 조각의 눈금: 2성 ◆◆ 은, 3성 ◆◆◆ 노랑, 크기와 먹 테가 별과 같다(1배와 3배 확대). 전투 보드의 1성 ◆ 파랑(보스전의 뒤 열 무기). 보드에 남은 별은 숫돌의 ★뿐이다.
  - 정비 창 "롱소드  일반 → 1성"(1성 파랑). 합치기 표시: 조각의 "합치기 → 1성"은 파랑인데 **든 아이템의 그림자 라벨**은 흰 글이었다(Round 48부터 그랬고 목업은 색을 칠했다) → `BoardHand`의 라벨도 `TierWord`로 색을 칠했다(PlayMode `GhostLabel` Test 갱신). PlayMode와 스크린샷을 다시 돌린 결과는 아래.
  - 단계가 있는 아이템의 제목(툴팁·카드·이름판)은 스크린샷 장면에 없다(꾸민 아이템이 모두 일반). PlayMode `Loot_AfterTheWin_…`이 바닥 이름판의 제목에 `TierWord`가 드는 것을 검사한다.
- 그림자 라벨의 색을 칠한 뒤: PlayMode `20261015-232357` **83/92**(`GhostLabel` Test 갱신), 스크린샷 `20261015-233559` 9/9 — "합치기 → 1성"의 1성이 파랑(`game/impl-compare.png`는 이 장면). `Assets/InitTestScene*` 없음, `git diff --check` 깨끗.
