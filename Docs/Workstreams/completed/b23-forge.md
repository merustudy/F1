# Slice B 23단계: 담금질과 첫 유니크 (글라디우스)

Snapshot: 2026-10-16 (구현·체인·스크린샷 통과. 후속 2: 금 테 → 아이콘 외곽선의 금빛 2px, 숨쉬기 없음. 후속 3: 유니크에는 성이 없다. 세션 정리에서 주제별 4묶음으로 커밋·푸시) — 2026-10-16 Slice B 완료로 닫음. 직접 플레이·사후 검토는 Slice C의 입력. `completed/`로 옮김.

## Goal

Architecture/09 "Slice B" Acceptance 15가 참이다: 3성 롱소드의 위나 아래에 3성 숫돌이 ★로 켜진 채 야영지에 들어가면 셋째 카드 "담금질"이 서고, 담금질하면 롱소드 자리에 3성 글라디우스(기본 무기 그대로)가 들어가고
숫돌은 사라지며 파티는 쉬지 않고 다음 층으로 간다. 글라디우스는 맨 앞 적 둘을 치고 자기 ★에 놓인 근접 무기를 +4 벼린다. 파티에 유니크는 하나다. 저장 형식은 그대로다.

## State

- 2026-10-16 Round 70(`ArtPipeline/Archive/70-unique-gladius/README.md`): 사용자 "유니크는 예제로 롱소드 3성 + 숫돌 3성 + 숫돌 3성으로 만드는 글라디우스" → 검토(G18·【검토】 유니크, 시뮬 측정: 지시의 레시피는 0/3,000, 롱소드 + 숫돌 둘 켜짐 0.1%), 목업 `mock-forge.png`, 판정할 것 열둘
  → 판정 1(숫돌 하나, 야영지, 숫돌 + 쉬기 포기, 맞히는 적 +1과 자신의 ★, 가중치 16, 금 테, A + C, 명패 머리, 생성 1회, 23단계, G18 결정) + "3성 + 3성으로 변경 검토" → 재검토(그 레시피도 0: 야영지 수가 막는다) + 모양 목업 `mock-gladius-stars.png`
  → 판정 2 **"3성 + 3성 그대로 / 모양 (a) 3×1 / 수치 권장안 / 숫돌 가격 1(임시)"** → 구현(Roadmap "23단계의 승인").
- 구현했다(아래 Done). 검증은 아래 Verification.
- 2026-10-16 후속(README 70 "후속 1·2"): 사용자 "디아블로2의 아이템 뒤 빛" → 검토(D2에는 없음; D3·D4·PoE) + 목업 → "금테는 삭제, 빛을 넣는데 외곽선이 전체적으로 해당 빛으로 숨쉬기" → 세 길 검토(글로우 그림 / 실루엣 복사 / 부드러운 테)와 목업 → "폭 2px·3px 목업, 나머지 권고안" → "숨쉬기를 지우고 1px" → **"2px로 확정. 숨쉬기 제거. 구현"** → 구현(README 70 "구현 (후속 2)").
- 2026-10-16 후속 3(사용자 "유니크는 성이 따로 없음(마름모 표시도 삭제). 단일 아이템이라 어차피 합쳐지지 않음"): `ForgeRules.Result`가 `Common`으로, 수치는 데이터 고정(12-20, ★ 4), `CanUpgradeAtCamp`가 유니크를 거름, 저장은 `Common`만, 카드 "유니크", 눈금 없음(README 70 "후속 3").

- 2026-10-16 **Slice B 완료로 닫음**(사용자 결정 2): 직접 플레이 없이 닫았다. 숫돌 값 1(임시)은 그대로 두고 Roadmap "임시 조치"로 Slice C에 넘긴다(복귀는 직접 플레이 뒤). 아래 Open은 Slice C의 입력이다.

## Done

- 기획: Design/02 §4(【확정】 담금질과 유니크, 글라디우스), 03 §1(셋째 선택)·§5(숫돌 값 1 임시·가중치 16), 00(Slice B 표), 07(`RecipeData.csv`, `Unique`), 08 §22(시뮬). CLAUDE.md §9. Roadmap("23단계의 승인", "임시 조치", G18 결정됨, 23단계 절).
- Architecture: 05(`RecipeData.csv`·`Unique`), 07(유니크 둘 이상 무효), 08("담금질과 유니크": `ForgeRules`), 09(범위·순서 12·Acceptance 15·Deferred), 11(`ForgeAtCamp`와 질의), 12("담금질", 유니크의 금 테, 유니크의 카드), 13(글라디우스 아이콘, `unique_rim`).
- 데이터: `RecipeData.csv`(글라디우스 ← 롱소드 Tier3 + 숫돌 Tier3), `ItemData.csv`의 `Unique` 열(선택)과 `gladius` 행(3×1, 근접, 3.0초, front:2, `EnemyFront` Reach 2 "3-5", 치명타 10, ★ `1:-1+1:1` `StarDamage` 1, 값·가중치 0), 숫돌 `Price` 1(임시)·`ShopWeight` 16, Generated 둘.
- 코드: `RecipeData`(정의·매퍼 `RecipeMapper`·`StaticDataFiles.Recipe`·`StaticData.Recipes`와 검사), `ItemData.Unique`(검사: 값·가중치 0), `CsvRow.OptionalBool`, `ForgeRules`(`PartyHasUnique`·`Options`·`OptionAt`·`CanForgeAtCamp`·`ForgeAtCamp`), `RunSaveMapper`(유니크 ≤ 1),
  `ExpeditionManager`(`ForgeOptions`·`CanForgeAtCamp`·`ForgeOptionAt`·`ForgeAtCamp`), 시뮬(`Policies`: 촉매 먼저 사기·담금질, `Simulations`: Forges·UniqueAtEnd 보고).
  화면: 문구 `Map.Forge*` 여섯·`Item.Unique*` 셋·`Item.Recipe`, `UiPalette.Unique`·`ForgeCard`·`ForgeCardText`, `CampCards`(카드 둘·셋의 자리), `NodeMapScreen`(셋째 카드, 담금질 걸음 `_forging`, `RefreshForgeCard`·`RefreshForgePlate`·`OnForge`·`ChooseForgeItem`·`OnForgeConfirm`), Builder(카드 셋·담금질 걸음, 카드 글은 늘어남),
  `UiText.TitleColor`·`RecipeHint`·`ForgeBody`·사실 "유니크"·`TooltipLines`의 유니크 줄, `ItemTooltipView`(명패 머리 `_plate`·`_plateTitle`·`_uniqueLine`, `PlateShown`), Builder(카드의 명패). **후속 2**: 유니크의 금 테(`unique_rim`·`BuildUniqueRim`·`UiArt.UniqueRim`·`draw_pieces.py`)를 빼고 **아이콘 외곽선의 금빛** — `ArtAddress.GlowOf`·`ItemData.GlowIcon`(`[JsonIgnore]`), `ArtSetup`·`ExpeditionArt.GlowOfItem`, `GridGeometry.GlowSize`·`LayGlow`, `ItemSlotView._glow`(`ShowGlow`·`UniqueGlowShown`)·`BattleItemView._glowArt`·`_glow`(마스크 밖, 흔들림 같이), `ShowItem`·`Bind`의 `glow`. 숨쉬기 없음.
- 그림: `Assets/@Art/Item/gladius.png`(생성 1회 $0.015, `Rosters/item.csv`의 행, 원본 `Archive/70-unique-gladius/approved/`, 자름). 후속 2: `gladius_glow.png`(356×120)를 `ArtPipeline/tools/glow_items.py`가 아이콘에서 만든다(화면 2px 넓힘·흐림, 금 + 밝은 심을 구움. 호출 없음, 다시 돌려도 같다).
- Test: EditMode `ForgeRulesTests`(다섯), `RunSaveMapperTests.Read_RefusesAPartyHoldingTwoUniqueItems`, `ShippedDataTests.TheGladius_…`, `StaticDataTransformerTests.Transform_RefusesARecipe_…`, `TestData`(`Item`의 melee·unique·stars, `gladius_t`, `Recipes`), `TestCsv.Recipes`. PlayMode `Camp_Forge_…`, 스크린샷 `_52_camp_forge`·`_53_camp_forge_step`·`_54_map_unique_card`. 후속 2: EditMode `GridGeometryTests.AUniquesGlow_…`, `ShippedDataTests`(GlowIcon), `ArtSetupTests`(`item/gladius_glow`); PlayMode의 단언은 `UniqueGlowShown`.

## Open

- **레시피는 닿기 어렵다**(시뮬 0/3,000: 기본 무기는 정비로만 오르고 야영지는 원정에 1.5번. 사용자가 알고 3성 + 3성을 지켰다). 직접 플레이에서 글라디우스를 만들 수 있는지, 만들고 싶어지는지 본다. 레버(야영지 수, 정비의 폭, 정예 처치의 기본 무기 단계)는 플레이 뒤.
- **임시 조치**: 숫돌 값 1(Roadmap "임시 조치"). 되돌릴 때 `ItemData.csv`·Design/03 §5·Roadmap을 함께 고친다.
- 사후 검토(Roadmap "23단계의 권장안 세부"): 글라디우스의 수치(3-5 × 단계, 치명타 10%, 자신의 ★ +1 × 단계), 카드 셋의 폭(170), 담금질 걸음의 첫 레시피 자동 선택, 바닥 이름판·전투 카드의 유니크 금 제목, 시뮬 정책이 촉매를 먼저 사는 것(클리어 +2.5%p의 일부).
- 둘째 유니크·둘째 레시피(지역 촉매)는 Slice C.

## Verification

- `dotnet build Tools/Sim`, `transform`, `validate` OK(아이템 22, 레시피 1).
- 시뮬(Design/08 §22): balanced 66.4% / 0.26, 보스전 92.3%, safe 60.9%, none 79.7% / 1.32 — 띠 안(보스전은 위 끝). 담금질 0.00/원정.
- 체인: 전체 `20261016-074627`·`075127`은 Test의 컴파일 오류(Test 코드 셋)로 실패 → 고친 뒤 `20261016-08`대: EditMode는 Test 데이터의 선택 열(`CsvRow.OptionalBool`이 없는 열을 허용)과 Test 셋(예외 형, 근접 무기 목록에 글라디우스, 레시피 CSV의 id)을 고친 뒤 **`20261016-081832` 855/855**(새 Test 아홉), PlayMode **`20261016-080508` 84/93**(9 skipped, inconclusive 0. 새 `Camp_Forge_…` 통과; 담금질 걸음에서 미리 고른 바탕의 사실이 설명 줄에 드는 것을 Test에 맞춤).
- 스크린샷 `20261016-081843` 9/9(PNG 92: `_52_camp_forge`·`_53_camp_forge_step`·`_54_map_unique_card`).
- **후속 2**(금빛 외곽선): `glow_items.py --check` same. 체인 **`20261016-142756`** setup·sim OK, EditMode **856/856**(새 `GridGeometryTests.AUniquesGlow_…`), PlayMode **84/93**(9 skipped). 스크린샷 **`20261016-144106`** 9/9. `game/impl-compare.png`의 넷째 행(글라디우스 조각 2배 | 목업 2px): 같다. 앞선 두 체인의 실패(Test의 `Bind` 인자, 주소의 `_` → `-`)는 README 70 "검증 (후속 2)".
- **후속 3**(성 없음): `transform`·`validate` OK. 체인 **`20261016-150615`** setup·sim OK, PlayMode **84/94**(10 skipped: 스크린샷 장면 `_55` 포함), EditMode **`20261016-152213` 857/857**(새 `Read_RefusesAUniqueItemAtATier`; 그 전 한 번은 단언의 단위로 실패). 스크린샷 **`20261016-151905`** 10/10(PNG 93, 새 `ko_55_battle_unique`). `game/impl-compare.png` 다섯 행 모두 목업과 같다(눈금만 후속 3으로 없어짐). 확정 목업과 요소마다 대조(`ArtPipeline/Archive/70-unique-gladius/game/impl-compare.png`): 카드 셋(어두운 금의 담금질 카드, 레시피 글), 담금질 걸음(판: "롱소드 3성 + 숫돌 3성 → 글라디우스 3성", 결과의 효과 줄, [돌아가기]·[담금질하기]), 유니크의 카드(금선 명패에 이름, "유니크 · 3성", 사실 줄의 "유니크", ★ 줄 "걸린 ★ 1/2 — 단검"), 조각의 금 테와 3성 눈금 — 목업과 같다. `Assets/InitTestScene*` 없음, `git diff --check` 깨끗.

## Next Action (제안)

- 2026-10-16 Slice B를 완료로 닫았다. 아래의 직접 플레이와 사후 검토는 Slice C의 입력이다.
- 후속 3까지 체인·스크린샷·목업 대조를 마치고 커밋했다(금 테는 기록에 남지 않았다). 남은 것은 사용자의 직접 플레이(x1, 원정 하나: 숫돌 값 1로 글라디우스를 노려 본다)와 그 뒤의 사후 검토(Open).
- 사용자: 직접 플레이(x1, 원정 하나)에서 숫돌 값 1로 글라디우스를 노려 본다.
