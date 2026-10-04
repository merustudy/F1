# 그림체 전환과 UI 라운드 (Round 12~17) — 포스터의 손맛 + 평면 색, 디아블로 컨셉 UI, 아이템 칸 개정 6, 촛불 빛, 외곽선 1/2과 눈동자

Snapshot: 2026-10-04 밤 (그 뒤 Round 16 촛불 빛(목업 B안 "b안으로 구현")과 Round 17 외곽선 1/2·몬스터 눈동자(검토 뒤 "외곽선 1/2 적용, 눈동자는 a안으로 구현")를 구현해 체인 통과. 같은 날 밤 두 번째 세션 정리에서 다섯 커밋(기획 / 화면 / 촛불 빛 기록 / 그림 파이프라인과 그림 / Roadmap·Handoff)으로 올리고 푸시했다. 개정 6: 아이템 칸 최대 7칸·칸 180x60(B안)을 구현해 체인 통과. 같은 날 밤 세션 정리에서 네 커밋으로 올리고 푸시했다. 그 전의 그림체 전환(12), 플레이 피드백, 포션 칸, UI 라운드(13 → 14 디아블로 컨셉)는 같은 날 저녁 네 커밋으로 올리고 푸시했다. 사후 검토 대기)

## Goal

캐릭터와 몬스터의 그림체가 사용자가 확정한 새 그림체(손으로 그린 흔들리는 잉크선과 낙서 획, 부푼 과장, 직업마다 다른 체형, 매력적인 캐리커처 얼굴, 시선은 적 쪽)로 바뀌고,
색은 지금의 평면·낮은 채도 그대로다. 확정된 그림만 `Assets`에 들어가고 체인이 통과한다. 직업 하나에 스프라이트 여럿을 둘 수 있는 데이터 모양이 정해진다.

## State

- Round 11(C1 큐트 전달서로 시작한 다섯 시험)은 사용자가 실패로 폐기했다(`ArtPipeline/Archive/11-concept-test/README.md`. 세트는 지웠고 기록·백업·전달서만 남겼다).
- Round 12: 올린 포스터의 손맛을 글로 풀고 평면 색을 유지한 시험 → 채택 → 체형·얼굴 다른 2장 → 미형 3장(기본으로 쓰지 않음) → 캐리커처+미형(`charm`) → 시선 규칙(`charm2`) → 자세 개선(`*_charm_v2`) → **확정**.
  현재 버전 셋은 `Archive/12-roar-style/selected/`(발키리·기사·대마법사. 둘은 양손 무기 준비 자세, 대마법사는 전방을 보며 주문). 세 세트(`roar`, `charm`, `pretty`)는 스프라이트 변형용으로 세트째 남겼다.
- 전환의 문서 작업을 했다(아래 Done). 이어서 사용자의 **일괄 승인**("1~5를 진행. 결정할 사안은 권고안으로", 자는 동안)으로 직업 셋, 적 다섯, 얼굴 열하나, 배경 1, UI 열(변형 열), 아이템 스물한 개를 생성해 `Assets/@Art`에 전부 연결했다.
  호출 누계는 UI 라운드까지 166회, 약 $4.12(상한 $10). 권고안으로 정한 것은 Roadmap "사후 검토 대기"와 `Archive/12-roar-style/README.md` "연결"에 있다.
- 이어서 플레이 피드백(외곽선, 보스 배율), UI 꾸미기(Round 13, 야영지 장비 → Round 14 디아블로 컨셉으로 대체. 13의 그림은 Assets에서 빠지고 Archive에만 있다), 포션 칸 정사각을 구현했다(아래 절).
- 2026-10-04 세션 정리에서 네 커밋(그림 파이프라인·그림 / 데이터 / 화면·UI / Roadmap·Handoff)으로 올리고 `origin/feature/slice-a`에 푸시했다. 끝난 Handoff 둘(`a-rows-items`, `a-battle-feel`)은 `completed/`로 옮겼다.
- 그 뒤 **개정 6**(아이템 칸 최대 7칸, 칸 180x60·간격 2, 목업 B안)을 구현했다(아래 절). 같은 날 밤 세션 정리에서 네 커밋(기획 / 화면 / 그림 파이프라인·기록 / Roadmap·Handoff)으로 올리고 푸시했다.
- 그 뒤 **촛불 빛**(Round 16, 목업 B안)과 **외곽선 1/2·몬스터 눈동자**(Round 17)를 구현했다(아래 절). 같은 날 밤 두 번째 세션 정리에서 다섯 커밋(기획 / 화면 / 촛불 빛 기록 / 그림 파이프라인과 그림 / Roadmap·Handoff)으로 올리고 푸시했다.
- 올린 포스터는 저작권이 있는 그림이라 저장소에 넣지 않았다. 시험은 이 세션의 올린 파일을 `--reference`로 붙였다. 앞으로의 생성은 우리 그림의 시트를 기준 그림으로 쓰므로 포스터가 필요 없다.

## Done

- 기획: Design/10 개정(머리말 【확정】 2026-10-04, §1 기준 그림, §2 그림체·체형·얼굴·시선·자세·"스프라이트 여럿", §3 몬스터, §7 기록). Roadmap: G9 개정, 갱신 로그, "그림체 전환 (Round 12)" 절.
- 스타일 문서 `ArtPipeline/STYLE_RUNTIME.md`: 머리말, Generation Types의 `character`·`enemy` 행, §1~§6 교체(§7~§22와 던전 컨셉은 그대로). 이전 문서·소재는 `Archive/flat-v1/`.
- 기준 그림 `References/Character/style_ref_roster.png`(확정 셋의 시트, 흰 바탕, 오른쪽을 본다. Pillow로 합성). `style_ref_mercenary.jpg`는 처음엔 아이템·배경·UI가 계속 썼고, 일괄 승인의 연결에서 `Archive/flat-v1/`로 옮겼다(모든 타입이 시트를 쓴다).
- 소재 `Rosters/character.csv`: 여섯 직업의 체형·얼굴·자세 문구. 발키리·기사·대마법사는 확정 그림의 문구, 주교·성기사·마검사는 **제안**(승인 전. 호출 없음).
- 스크립트: `gen_image.py`의 `character`(기준 그림 시트, 뒤집지 않음)·`enemy`(시트를 뒤집어 붙임), `review_sheet.py`의 기준 그림. `--dry-run`으로 직업·적·아이템이 조립되는 것을 확인했다.
- Architecture/13: 기준 그림과 뒤집기, 소재가 체형·얼굴·자세를 갖는 것, 폭이 넓은 그림의 키, Archive 위치.
- 기록: `Archive/12-roar-style/README.md`(지시, 분석, 세트, 현재 버전, 시험 1~7, 판정, 호출).
- 일괄 승인의 연결(2026-10-04): 스타일 문서 §7·§8·§10·§13·§14·§18을 새 손맛·새 기준 그림으로(모든 타입이 `style_ref_roster.png`. `gen_image.py` TYPES. 이전 기준 그림은 `Archive/flat-v1/`),
  `Rosters/character.csv`(여섯 확정, `FaceDx/FaceDy`), `enemy.csv`(새 소재, 보정값), `Assets/@Art/Unit/Job`(6)·`Unit/Enemy`(5)·`Face`(11)·`Background/Dungeon`(1)·`UI/Frame`(11)·`UI/Icon`(4)·`Item`(21) 교체.
  확정 원본과 그때의 문서·소재는 `Archive/12-roar-style/approved/`, 리뷰 시트는 라운드 폴더에. `output/<type>/old-flat-v1/`에 이전 출력(gitignore). Design/10 §2·§3·§4·§5, Architecture/13(기준 그림, 아이템 캔버스), Roadmap.

## 플레이 피드백 (2026-10-04)

- ① 외곽선 굵게: `gen_image.py`의 `fit_figure`가 맞춘 뒤 실루엣 둘레에 `FIGURE_OUTLINE`(8px, `UI_LINE` 색, 가장자리 1px 부드럽게) 띠를 두른다. 열한 장을 `--refit`으로 다시 맞춰 `Assets`에 복사했다(호출 없음). 전후 비교 `Archive/12-roar-style/outline-before-after.png`.
- ② 보스 크게: 감독관의 소재를 큰 머리·턱으로 바꿔 재생성(호출 1회, 약 $0.03. 전의 것은 `output/enemy/old-flat-v1/mine_overseer_smallhead*`). `EnemyData.FigureScale`(백분율, 50..300, 기본 100) 열을 더했다: Definition(검증, JSON Order 7), `EnemyMapper`, `TestCsv`·`StaticDataTransformerTests`, `EnemyData.csv`(감독관 150), Generated JSON 재생성.
  화면: `ExpeditionArt.ScaleOfEnemy`, `FigureView.SetScale`(그림 Image의 크기만 배율, 발은 바닥선 그대로), `BattleUnitView.Bind(unit, figure, scale)`, `BattleScreen.CreateUnit`. 겹침 허용, 2칸 규칙 없음(권고안).
  문서: Design/10 §2·§3, 07; Architecture/05, 12, 13.

## UI 꾸미기 (Round 13, 야영지 장비, 2026-10-04)

- 지시 "UI가 너무 심심하다. 유사 게임 검토 → 목업 → 승인 후 구현" → 여섯 게임 검토, 세 방향 목업(`Archive/13-ui-decor/mock_ui_decor.py`, 지금 전투 스크린샷 위에 합성, 샘플 조각 셋 생성 $0.08) → **A 야영지 장비** → 하단 패널 대비 피드백 → 네 안 → **A3 캔버스 공구 말이** 적용(권고안).
- 생성: 조각 12회(약 $0.33) + 샘플 재사용 둘 + 도형 둘(`draw_pieces.py`: 사슬, 빛). 변형은 `ui_variant.csv`가 Sprite 이름과 출처를 떼어 놓는다(`bag`←`roll`, `slot`←`pocket`, `trough`←전 `slot`, `plate_*`←`plate_wood` tint).
- 배선: `UiArt`(Table, Belt, Parchment, Trough, Sign, Lantern, MapRoll, Dice, Chain, Glow, NodeBattle, NodeBoss), `UiPrefabSetup.Kit`(HP 홈, 벨트, 포션 칸의 병과 잉크 글, 빈 칸 70%), `Battle`(간판과 제목, 자막 왼쪽, 탁자, 사슬, 등불·빛·주사위·지도),
  `PartySide`(탁자, 오른쪽 어두운 판, 주사위, 잉크 글), `NodeMap`(가죽 헤더·간판, 양피지, 잉크 길, 원 노드와 표식), `Reward`(가죽 헤더·간판). View: `PotionSlotView.Show(potion, icon, …)`, `MapNodeView.Show(label, boss, …)`, `ItemSlotView` 잉크 글.
  데이터: `PotionData.Icon`(Order 6, AllowNull), `PotionMapper`, `PotionData.csv`(`potion/healing-potion`, `potion/barrier-potion`), Generated JSON, `TestCsv`·`StaticDataTransformerTests`. `ExpeditionArt.OfPotion`, `ArtSetup`(포션 Entry·Import). `UiPalette.InkText/InkTextDim`.
  그림: `Assets/@Art/UI/Frame`(table, belt, parchment, plate_*, slot, slot_selected, trough, bag, sign, lantern, map_roll, dice), `UI/Icon`(node_battle, node_boss, chain, glow), `Assets/@Art/Potion`(healing_potion, barrier_potion).
- 문서: Architecture/12 "야영지 장비", 13(배선·변형), 04(`item/`, `potion/`), 05; Design/07, 10 §5; Roadmap; `Archive/13-ui-decor/README.md`.
- 간판의 자리: 제목은 판의 가운데(`SignTitleTop` 52), 노드 맵·보상의 간판은 헤더 왼쪽(`PartySignX` 420. 가운데는 포션 띠와 겹친다).
- 포션 칸(사용자 지시 2026-10-04 "치유 포션 칸을 정사각형으로, 아이콘만, 설명 글 삭제. 클릭 시 글이 나타나고 사용 시 소멸"): 칸은 64 정사각 주머니에 병만(`BuildPotionSlot`, 띠 232), `PotionSlotView`에서 이름·효과 글을 뺐다.
  전투: 누른 동안만 안내 판에 "이름 · 효과: 쓸 아군을 누르세요"(`Battle.PotionArmed`의 인자 둘), 아니면 판을 숨김(`Battle.PotionHint`·`PotionWait` 문구와 키 삭제. 대기 중 안내는 흐린 칸이 대신한다 — 권고안). 노드 맵·보상: 칸을 누르면 상세 줄에 "이름 — 효과"(`_selectedPotion`), 다른 칸을 누르거나 다시 누르면 사라진다.
- 체인·스크린샷: 아래 Verification.

## 디아블로 컨셉 (Round 14, 2026-10-04 오후)

- 지시 "디아블로 컨셉을 준용해서 꾸며볼래? 이미지 API 사용, 목업 우선" → 특징 분석(검은 돌·쇠, 핏빛, 금, 뼈, 구슬, 벨트, 촛불, 비네트. 작품의 그림은 쓰지 않음) → 샘플 5 + 도형 목업 두 안(`mock_ui_diablo.py`) → D2 → 혈액 구슬이 체력으로 읽힌다는 피드백 → 시계 대안 넷(`mock_clock.py`) → S4 양초 "같이 적용" + 칸 단순화 요청 → 칸 세 안(`mock_slots.py`) → I2.
- 구현: 조각 8회(`iron_pocket`, `iron_inventory`, `belt_iron`, `button_iron`, `candle_holder`, `candle_body`, `candle_flame`, `skulls`) + 샘플 셋 + 도형 다섯(`draw_pieces.py`: `slot`, `slot_selected`, `candle_top`, `smoke`, `chain`). `ui_variant.csv` 재매핑(Architecture/13).
  코드: `UiPalette`(`Blood`, `BloodLight`, `Gauge` 마른 피), `UiArt`(Tablet, PotionSlot(+Selected), Candle*, Smoke, Skulls. Sign·Lantern·MapRoll·Dice·Dial·Ring·Parchment 삭제), `Kit`(패널 여유 10, HP 핏빛·잔상, 쇠 포션 주머니, `BuildScreenVignette`),
  `Battle`(쇠 명패 제목, 사슬·해골, `BuildCandle`, 시간·폭풍 줄을 촛대 아래, 비네트), `PartySide`(사슬·해골·비네트), `NodeMap`(쇠 명패, 돌판, 금선, 뼈색 이름), `Reward`(쇠 명패), `MapNodeView`(뼈색 글), **`CandleView`**(새), `BattleScreen`(`_candle`, `StormProgress`, `Candle`, `RenderClock`), Test(`...AndTheStormCandle`).
  그림: `Assets/@Art/UI/Frame`(panel, table, tablet, belt, plate_*, slot, slot_selected, trough, potion_slot, potion_slot_selected, button, bag, candle_holder, candle_body, candle_flame, skulls), `Icon`(candle_top, smoke, chain + 전의 glow·vignette·상태·표식). 뺀 것: sign, lantern, map_roll, dice, dial, parchment, ring.
- 스크린샷 검토에서 고친 것(권고안): 노드 맵·보상의 누를 수 없는 칸이 공통 버튼의 흐림(55%, 알파 60%)으로 쇠 패널에 가라앉아 승인한 목업(`mock-D2-bone-map.png`)보다 어두웠다 → 보드 칸의 버튼만 `QuietCellTint` 80% 불투명(`Kit`, `PartySide.BuildItemSlot`). 누를 수 있는 칸은 그대로 밝은 뼈색이다.
- 문서: Architecture/12 "디아블로 컨셉"(야영지 장비 절을 대체), 13; Design/10 §5; Roadmap; `Archive/14-ui-diablo/README.md`.
- 체인·스크린샷: 아래 Verification.

## 아이템 칸 개정 6 (Round 15, 2026-10-04 밤)

- 지시 "현재 아이템 칸을 최대 7칸으로 줄이고 아이템 1칸의 세로길이를 좀 길게. 밑의 UI 높이는 유지. 목업 이미지 제공" → 지금 전투 스크린샷 위에 게임의 Sprite와 아이콘으로 네 안(`Archive/15-seven-cells/mock_seven_cells.py`: 지금 / A 58·간격 4 / B 60·간격 2 / C 56·간격 6. 패널 460 그대로) → **"b안으로 적용"**(권장은 A).
- 기획 먼저: Design/02 §4(【확정】 최대 8 → 7, 개정 6), 00(현황 줄, "개정 6" 절, 개정 2·5 표의 화살표), 10 §5(【확정】 (개정 6), V안 끝의 화살표).
- 코드: `JobData.MaxItemSlots` 7(직업의 `ItemSlots`와 적이 든 아이템 수·칸 합의 검증 상한. `DefinitionTests`는 상수를 쓰므로 그대로 통과), `BattleItemView.CellHeight` 60·`CellGapY` 2, `UiPrefabSetup.Battle.BoardColumnsTop` 14(7칸 432 = 460 − 28). 세 화면의 Prefab과 Stamp는 setup이 재생성했다(열 428 → 432, 칸 50 → 60, 간격 4 → 2, 열 위 16 → 14).
  `PartySide`·노드 맵·보상은 `BattleItemView`의 상수와 `JobData.MaxItemSlots`를 쓰므로 따라왔다. 패널 460과 그에 묶인 자리(맵, 보상 카드, 팝업)는 그대로.
- 그림 파이프라인: `gen_image.py`의 `ITEM_CELLS` 캔버스 328x100·224·348(다음 아이콘부터), `review_sheet.py`의 `ITEM_CELL` (180, 60)·`ITEM_CELL_GAP` 2, `STYLE_RUNTIME.md` §19~§21의 비율 문구(약 3.3:1, 3:2, 15:16). **아이콘 스물한 개는 다시 그리지 않았다**(호출 없음. 폭에 맞아 크기는 그대로, 1·2·3칸에서 위아래로 10·18·26이 남는다).
- 문서: Architecture/12 "아이템의 아이콘"·"전투 화면", 13 "후처리 (`cell`)", Roadmap(갱신 로그, "Slice A 개정 6", "사후 검토 대기"의 V안 행과 개정 6 행), `Archive/15-seven-cells/README.md`(안, 판정, 구현).
- 체인·스크린샷: 아래 Verification.

## 촛불 빛 (Round 16, 2026-10-04 밤)

- 지시 "전투 배경이 촛불을 기준으로 반원 형태로 환하고 점점 멀어질수록 어둡게(다키스트 던전처럼). 관련 목업 제공" → 지금 스크린샷 위에 게임의 배경과 양초 조각으로 빛을 합성한 네 안(`Archive/16-candle-light/mock_candle_light.py`: 지금 / A 은은한 빛 / B 반원의 빛 / C 좁혀 오는 어둠)과 B의 시간 흐름 → **"b안으로 구현"**(권장안).
- 발견: 화면 비네트 조각이 뒤집혀 가운데가 가장 어두웠다(빈사의 붉은 빛, 폭풍의 어스름도 같은 조각). B안 목업이 바로잡은 상태라 함께 고쳤다. 빛을 양초에 묶기와 어스름 삭제도 B안 목업에 담겨 있어 권고안으로 함께 했다(사후 검토).
- 조각(호출 없음): `draw_pieces.py`가 바로잡은 `vignette`, `candle_dark`(반원의 어둠, 512x256), `candle_warm`(따뜻한 빛)을 그려 `Assets/@Art/UI/Icon`에. 따뜻한 빛은 원 안의 픽셀을 올림으로 칠했다(`.meta`의 Sprite 영역 기록은 모든 조각이 알파 경계로 잘려 있지만, 양초 몸통의 자리로 보아 게임은 그림 전체를 그린다).
- 코드: `UiArt.CandleDark`·`CandleWarm`, `UiPrefabSetup.Battle`(배경 다음의 `StageLight`(RectMask2D) → `CandleLight` 아래 `LightDark`·`LightWarm`, 그리고 `LightOut`. 상수 `LightReach` 800·`LightWide` 1.35·`LightWarmReach` 620·`LightDarkness` 0.88·`LightWarmth` 0.16, 색 `CandleLight`. Fx의 `StormVignette` 삭제),
  `CandleView`(빛의 점을 불꽃 가운데에, `LightPull` 0.2·`LightBreath` 0.012·`LightFlicker` 0.08·`LightOutSeconds` 0.4, 시험용 `Darkness`·`DarknessAtFlame`·`LightReach`·`LightPosition`), `BattleFxLayer`(`SetStorm`·`StormDarkness` 삭제), `BattleScreen`(그 호출 삭제, 겹친 summary 정리).
- Test: `Battle_StageIsLitByTheCandle_UntilTheStormPutsItOut`(새. `UiTestUtil.EnterALongFirstBattle`: 파티의 아이템을 비우고 HP 100000이라 폭풍까지 이어진다), `Battle_PlaysWhatHappens...`에서 어스름 검사 삭제, 스크린샷 `TheStorm_Korean`(`ko_19_battle_storm_near`, `ko_20_battle_storm`).
- 문서: Design/10 §4, Architecture/12("UI의 그림", "디아블로 컨셉"의 양초·**무대의 빛 = 양초**·비네트, "전투 화면"의 패널 가운데(남아 있던 다이얼·고리 설명을 양초로), "연출"의 폭풍·Fx 층, Test), 13(도형 조각), Roadmap, `Archive/16-candle-light/README.md`, 09의 `draw_pieces.py`에 메모.

## 외곽선 1/2과 몬스터 눈동자 (Round 17, 2026-10-04 밤)

- 지시 "용병 및 몬스터의 외곽선을 1/2로 줄이는 방안 검토. 몬스터 눈의 눈동자는 지우는 방안 검토. 검토 후 보고" → 미리보기 넷(`Archive/17-outline-pupils/`: 촛불 빛 무대 위 합성 줄, 2배, 머리 4배, 보스. 호출 없음)과 눈동자의 두 안 → **"외곽선 1/2 적용, 눈동자는 a안으로 구현"**(권장안).
- 외곽선: `gen_image.py`의 `FIGURE_OUTLINE` 8 → 4(화면에서 약 2.7 → 1.3px, 보스 4 → 2px). 저장된 원본에서 다시 맞추면 Assets와 네 채널이 같다는 것을 먼저 확인했다.
- 눈동자(안 A): `remove_pupils.py`가 확정 원본(`Archive/12-roar-style/approved/enemy/`)을 읽어 눈마다 정한 자리를 둘레의 흰자 색으로 칠하고 `output/enemy/`와 `Archive/17-outline-pupils/approved/enemy/`에 쓴다. 갇힌 눈동자(약탈자 오른눈, 주술사 큰 눈)는 구멍, 테두리에 붙은 것은 눈의 다각형(피부에서 선 두께 안쪽은 보존), 궁수(흰자가 피부만큼 옅음)는 눈동자 다각형. 몬스터마다 원본의 149~639픽셀만 바뀐다.
- 그림: 열한 장 `--refit` → `Assets/@Art/Unit`(검토의 미리보기와 네 채널이 같다), 얼굴 열하나를 `cutface.py`로 다시 잘라 `Assets/@Art/Face`(자리는 전과 같음, 화면에 쓰이지 않음).
- 규칙: 스타일 문서 §2 공용 금지 목록에서 "dot eyes, blank eyes without pupils"를 빼고 §4 용병에 "눈동자는 늘 있다", §5 몬스터는 "눈동자 없는 한 가지 색의 눈, 방향은 머리와 몸", §6 기준 그림 규칙은 "용병 시트의 눈동자를 따르지 않는다", 광산 절은 "한 가지 흐린 등불 노랑, 눈동자 없음". 소재 다섯의 눈 문구. `--dry-run`으로 두 프롬프트를 확인했다.
- 문서: Design/10 §2·§3, Architecture/13("방향", "후처리 (figure)"), Roadmap, `Archive/17-outline-pupils/README.md`. 미리보기 스크립트는 몬스터를 확정 원본에서 읽게 고쳤다.

## Open

- **외곽선 1/2과 노란 눈**은 플레이로 본다: 얇아진 띠(화면에서 약 1.3px)로 뒤 열·밝은 바닥 위의 유닛이 배경과 구별되는지, 눈동자 없는 몬스터의 인상. 띠는 `gen_image.py`의 `FIGURE_OUTLINE` 하나로 `--refit`해 바꾼다(호출 없음).
- **촛불 빛**은 플레이로 본다: 빛의 흔들림, 폭풍에 꺼지는 0.4초, 세기(어둠 88%, 따뜻한 빛 16%), 화면 비네트가 가장자리로 간 노드 맵·보상의 인상. 조절은 `UiPrefabSetup.Battle`의 `Light*`와 `CandleView`의 상수로 한다.
- **개정 6의 아이콘**: 칸이 60으로 높아져 아이콘(폭에 맞음) 위아래의 여백이 는다. 플레이에서 작아 보이면 스물한 개를 새 캔버스(328x100·224·348)로 다시 그린다(호출 21회, 약 $0.5. 요청 시).
- 양초의 흔들림·가늘어짐·꺼짐과 연기는 스크린샷으로 볼 수 없다. 사용자가 직접 플레이해 본다(폭풍은 전투 시작 후 `StormStartMs`).
- 디아블로 컨셉의 세부(비네트 세기, 빈 칸 알파, 양초 크기, 소품 자리)는 플레이로 보고 조절한다. 인벤토리 팝업·보상 카드·타이틀·로비·정산의 틀은 도형 그대로(Deferred).

- **스프라이트 여럿**【미결】: 권장안은 용병의 `Figure` 선택 열(`MercenaryData`. 비면 직업의 그림)과 소재의 `Variant` 열(Key + Variant로 한 줄). 설계안을 보여 준 뒤 Design/05·07, Architecture/05·12·13, 데이터·코드를 바꾼다. 세트(`roar`, `charm`, `pretty`)는 변형용으로 남아 있다.
- 보스의 배율 150은 권고안이다. 전투 화면(보스 층)을 보고 조절한다(데이터 한 칸).
- **키와 폭**【미결】: 넓은 자세(양손 무기)는 3:4 캔버스에서 키가 14~24% 작다(발키리 616, 기사 625, 성기사 695, 마검사 720, 감독관 715 / 806·869). 지금은 체형 차이로 두었다(권고안). 사용자가 전투 화면을 보고 캔버스·그림 자리를 넓힐지 정한다.
- **UI 틀의 경고**: Round 12의 panel·button에 "그린 비율과 맞춘 비율이 15% 넘게 다르다"가 있었다(화면에서 눌림은 보이지 않았다). Round 14에서 둘 다 새 조각(`stone_panel`, `button_iron`)으로 바뀌었다.
- **얼굴**은 지금 화면에 쓰이지 않는다(V안). 보정값은 격자와 Alpha 계산으로 맞췄다. 얼굴을 다시 쓰게 되면 `output/review/faces.png`로 다시 본다.
- 사용자가 직접 플레이해 새 그림의 간격·겹침·키를 본다(전투 화면의 190 간격에서 넓은 자세가 옆 그림과 더 겹친다).
- 아이템·UI·배경은 선만 바꾸고 색·크기·구조는 그대로다. 어울리지 않는 곳이 보이면 그 타입만 다시 그린다.
- 이전 그림체의 그림은 `Archive/01-characters/approved`, `02-enemies/approved`, `03-items/approved`, `04-backgrounds`, `05-ui`, `09-battle-ui-feel`에 남아 있다(되돌리기 가능).

## Verification

- 외곽선 1/2·눈동자 `Tools/chain.sh`(2026-10-04 밤): setup OK(그림 PNG 스물두 장을 다시 Import. `.meta`는 그대로), sim OK, EditMode 616/616, PlayMode 45/45(`[Explicit]` 스크린샷 5개 제외). `git diff --check` 깨끗.
  `Tools/screenshots.sh` 36장(5/5): 전투(`_05`, `en_05`)·보스전(`_09`, `_15`)·빈사(`_18`)·폭풍(`_19`, `_20`)에서 그림 둘레의 띠가 얇아졌고 몬스터의 눈이 눈동자 없이 노랗다. 용병의 눈동자는 그대로다.
  스크린샷 실행이 더 바꾼 설정 파일은 없다(사용자의 Unity 세션이 바꿔 둔 설정 셋은 그대로 둠). 여덟 장은 `Archive/17-outline-pupils/game/`, 전후 비교는 `before-after.png`(같은 적이 나온 전투끼리).
- 촛불 빛 `Tools/chain.sh`(2026-10-04 밤, 두 번. 따뜻한 빛 조각을 다시 그린 뒤 한 번 더): setup OK(`BattleScreen` Prefab·Stamp 재생성, 새 조각 둘의 `.meta`는 다른 UI 아이콘과 같은 정책), sim OK, EditMode 616/616, PlayMode 45/45(`[Explicit]` 스크린샷 5개 제외. 새 빛 Test 포함). `git diff --check` 깨끗.
  `Tools/screenshots.sh` 36장(5/5. 폭풍 장면 둘을 더했다): 전투(`_05`)에 양초를 중심으로 한 따뜻한 반원과 어두운 가장자리·위쪽, 유닛·명패는 그대로. 폭풍 5초 전(`ko_19`)은 양초가 짧고 빛이 낮고 작다. 폭풍(`ko_20`)은 불이 꺼지고 연기가 오르며 무대 배경 전체가 어둡다.
  빈사(`_18`)와 보스(`_09`, 아군 빈사)의 붉은 빛은 무대 가장자리에, 노드 맵(`_04`)·보상(`_07`)은 화면 가장자리만 어둡다. 스크린샷 실행이 바꾼 설정 파일은 없다. 여덟 장은 `Archive/16-candle-light/game/`, 전후 비교는 `before-after.png`(새 런마다 지도가 달라 같은 적이 나온 영어 화면과 짝지었다).
- 개정 6 `Tools/chain.sh`(2026-10-04 밤, 상수 셋을 바꾼 뒤): setup OK(Prefab 셋·Stamp 재생성: 열 428 → 432, 칸 50 → 60, 간격 4 → 2, 열 위 16 → 14), sim OK(6 jobs, 21 items, 5 enemies), EditMode 616/616, PlayMode 44/44(`[Explicit]` 스크린샷 4개 제외). `git diff --check` 깨끗.
  PlayMode의 보드 Test 넷은 `BattleItemView.BoardHeight`로 높이를 보므로 고치지 않고 통과했다. `DefinitionTests`의 상한 Test도 상수를 쓴다. 밸런스 시뮬은 돌리지 않았다(`MaxItemSlots`는 검증 상한이라 결과가 같다).
  `Tools/screenshots.sh` 34장(4/4): 전투(`_05`)의 네 아군 열에 60 높이의 칸 다섯이 간격 2로 쌓이고 가방이 그만큼 길어졌다(아이콘은 같은 크기, 위아래 여백만 늘었다). 적의 칸, 양초, 사슬·해골은 그대로. 노드 맵(`_04`)·보상(`_07`)의 파티 쪽 칸과 "빈 칸" 글, 등급 배지, 인벤토리 팝업(`_17`), 보스(`_09`), 빈사(`_18`), 영어(`en_05`)도 정상.
  스크린샷 실행이 바꾼 프로젝트·렌더 설정은 없었다. 일곱 장은 `Archive/15-seven-cells/game/`, 전후 비교는 `before-after.png`.
- `Tools/chain.sh` (2026-10-04, 새 그림 전부를 연결한 뒤): setup OK, sim OK(6 jobs, 21 items, 5 enemies), EditMode 616/616, PlayMode 44/44(`[Explicit]` 스크린샷 4개 제외). `git diff --check` 깨끗.
  setup이 바꾼 생성물은 없다(Prefab, Stamp, Generated JSON, `.meta`의 Import 정책 그대로). Unity 쪽 코드 변경 없음.
- 플레이 피드백 뒤 `Tools/chain.sh`(2026-10-04 아침, 외곽 띠 열한 장·큰 머리 감독관·`FigureScale` 열과 화면 코드): setup OK, sim OK, EditMode 616/616, PlayMode 44/44(스크린샷 4개 제외). `git diff --check` 깨끗.
  setup이 바꾼 생성물 없음(Generated JSON은 `transform`이 미리 재생성). 에디터가 열려 있으면 체인이 서므로 사용자가 닫은 뒤 돌렸다.
  `Tools/screenshots.sh` 34장(4/4): 전투(`_05`)의 모든 유닛에 굵은 외곽 띠, 보스 층(`_09`)의 감독관이 1.5배로 서서 큰 머리와 치켜든 망치가 읽힌다(발은 바닥선, 명패 자리 그대로). `Archive/12-roar-style/game/`의 `_05`·`_09`·`_15`를 새것으로 바꿨다.
- UI 꾸미기 `Tools/chain.sh`(2026-10-04 낮): setup OK, sim OK, EditMode 616/616, PlayMode 44/44(스크린샷 4개 제외). `git diff --check` 깨끗. 처음 한 번 EditMode가 1건 실패했다(`StaticDataFileStoreTests`의 포션 CSV 고정값이 `Icon` 열을 몰랐다) → 고정값에 열을 더해 통과.
  setup이 바꾼 것: Prefab 셋과 Stamp, `AddressableAssetSettings.asset`(포션 병 Entry 둘), `Assets/@Art/Potion`·새 UI 그림의 `.meta`(Import 정책). `Tools/screenshots.sh` 34장(4/4): 전투(`_05`)에 판자 탁자, 공구 말이와 주머니, 벨트의 병, 칠한 나무 명패, 간판의 제목과 왼쪽의 자막, 사슬의 시계, 등불·주사위·지도;
  노드 맵(`_04`)에 양피지 위의 표식 노드와 잉크 길, 왼쪽의 간판, 오른쪽 판; 보상(`_07`), 보스(`_09`), 인벤토리 팝업(`_17`), 빈사(`_18`), 영어(`en_05`)도 정상. 일곱 장은 `Archive/13-ui-decor/game/`, 전후 비교는 `before-after.png`.
- 포션 칸 변경 뒤 `Tools/chain.sh`: setup OK(Prefab 셋·Stamp, String Table 셋 재생성: `Battle.PotionHint`·`PotionWait` 삭제와 `PotionArmed`의 인자 둘), sim OK, EditMode 616/616, PlayMode 44/44. `Tools/screenshots.sh` 34장(4/4): 전투(`_05`)의 정사각 포션 칸 셋(병 둘, 빈 칸 흐림)과 누른 포션의 안내 판 "치유 포션 · HP 50 회복: 쓸 아군을 누르세요",
  노드 맵·보상의 띠도 병만. 스크린샷 실행이 렌더·프로젝트 설정 셋을 다시 썼기에 커밋 상태로 되돌렸다. `game/`의 일곱 장과 `before-after.png`를 새것으로.
- 디아블로 컨셉 `Tools/chain.sh`(2026-10-04 오후, 칸 틴트를 고친 뒤 한 번 더. 두 번 모두): setup OK(Prefab 셋·Stamp 재생성, 새 UI 그림의 `.meta`), sim OK, EditMode 616/616, PlayMode 44/44(스크린샷 4개 제외. 양초 Test 포함). `git diff --check` 깨끗.
  `Tools/screenshots.sh` 34장(4/4): 전투(`_05`)에 돌 패널과 쇠 명패 제목, 핏빛 HP, 쇠 벨트의 주머니, 유닛마다 쇠 인벤토리 패널의 뼈색 격자 칸, 가운데의 양초(불꽃·빛·촛대, 아래의 시간과 폭풍 줄), 양끝 사슬과 해골 더미;
  노드 맵(`_04`)에 돌판의 금선 길과 표식 노드, 쇠 명패 제목, 왼쪽 사슬; 보상(`_07`), 보스(`_09`), 팝업(`_17`), 빈사(`_18`), 영어(`en_05`)도 정상. 스크린샷 실행이 다시 쓴 렌더·프로젝트 설정 셋은 커밋 상태로 되돌렸다. 일곱 장은 `Archive/14-ui-diablo/game/`, 전후 비교 `before-after.png`.
- `gen_image.py --dry-run`: 직업(시트 그대로), 적(시트를 뒤집어), 배경, UI 틀, 아이템(든 유닛의 새 그림) 모두 조립 통과.
- `Tools/screenshots.sh` 34장(4/4): 전투(`_05`)에 새 배경(버팀목·등불·광차·레일, 손으로 그린 선), 새 직업 넷(대마법사·주교·마검사·기사, 전투 준비 자세, 오른쪽을 봄)과 쥐 셋, 새 명패·칸·버튼·다이얼, 칸 안의 새 아이콘(지팡이 둘, 검 둘, 쥐 앞니).
  보스 전투(`_09`)의 결과 창과 패널, 노드 맵(`_04`)·보상(`_07`)의 파티 열 넷과 세로 칸의 아이콘·등급 배지, 전진 뒤(`_15`)와 빈사(`_18`)도 정상. 영어 화면도 같다. 여섯 장은 `Archive/12-roar-style/game/`.
  관찰: 넓은 자세의 그림이 옆 열과 조금 더 겹치지만 앞 열이 위에 그려져 읽힌다. 틀의 "비율 15%" 경고(panel, button)는 화면에서 눌림이 보이지 않는다. 발동한 칸의 놋쇠빛 번쩍임이 아이콘을 덮는 것은 전과 같은 연출이다.
- 그림의 확인은 리뷰 시트로 했다(`Archive/12-roar-style/`의 `jobs-six.png`, `enemies-five.png`, `faces-new.png`, `items-new.png`, `ui-new.png`, `background-new.png`).

## 알아둘 것

- 얼굴 일관성이 필요한 재생성(자세 변경)은 포스터가 아니라 **그 캐릭터의 확정 그림**을 `--reference`로 붙이고 `Archive/12-roar-style/charm/STYLE_RUNTIME-pose.md`(§3이 "같은 캐릭터, 자세만")로 돌린다. 발키리의 준비 자세가 이렇게 나왔다.
- 소재가 길다. 체형(등신 포함) → 머리·옷·무기 → 자세 → 얼굴의 순서로 적으면 모델이 잘 따른다.
- 세트 폴더마다 스타일 문서와 소재가 있어 그 변형으로 더 그릴 수 있다(`--style`, `--roster`). 산출물 이름은 `--name <key>_<set>`.

## Next Action (제안)

0. 사용자가 Unity에서 새 런으로 직접 플레이한다: 촛불 빛(반원의 세기, 양초와 함께 내려가고 폭풍에 꺼지는 것), 새 칸(180x60, 간격 2)의 인상과 아이콘의 여백, 새 그림체의 인상과 간격·겹침·키, 보스의 1.5배, 디아블로 UI(돌 패널, 쇠 명패, 뼈색 칸, 비네트 세기)와 양초의 흔들림·가늘어짐·꺼짐·연기(폭풍은 전투 시작 후 `StormStartMs`).
   조절은 데이터 한 칸(`EnemyData.FigureScale`)이나 `UiPrefabSetup.Kit`·`Battle`의 상수로 한다. 옛 저장은 그림만 바뀌어 그대로 이어지지만 새 런이 보기 좋다.
1. Roadmap "사후 검토 대기"의 Round 12~14, 개정 6, Round 16 항목(권고안으로 정한 세부)을 사용자가 검토한다. 바꾸면 체인·스크린샷으로 확인하고 커밋한다.
2. 스프라이트 여럿의 설계안(용병의 `Figure` 열 + 소재의 `Variant` 열)을 보여 주고 승인받은 뒤 구현한다.
3. 키와 폭【미결】: 넓은 자세의 캔버스·그림 자리를 넓힐지 사용자가 정한다.
4. Deferred UI 틀(인벤토리 팝업, 보상 카드, 타이틀·로비·정산)은 요청이 있을 때 같은 컨셉으로 그린다.
