# 그림체 전환과 UI 라운드 (Round 12~25) — 포스터의 손맛 + 평면 색, 디아블로 컨셉 UI, 아이템 칸 개정 6, 촛불 빛, 외곽선 1/2과 눈동자, 쿨다운 빛, 공격·피격 모션, 진지한 표정, 무덤, 성기사의 머리, 공격·피격 자세와 아이템 분류, 외곽선 2/3

Snapshot: 2026-10-04 밤 (마지막: 열네 번째 판정 "권장안으로 변경 구현" — **외곽선 2/3 구현**(띠 4 → 1.35px, 그림 스물세 장·얼굴 열하나 다시 맞춤, 호출 없음). 그 전: 열세 번째 지시 — **캐릭터 외곽선 2/3·1/2 목업**(Round 25, 띠만 좁혀 다시 맞춤, 게임 스크린샷과 발키리 동작 MP4). 그 전: 열두 번째 판정 "승인" — **공격·피격 자세의 게임 연결(Round 23) + 아이템 분류 다섯과 발동할 때의 움직임(Round 24) 구현**(체인·스크린샷 통과, 호출 없음). 그 전: 열한 번째 판정 — 아이템 분류 다섯(무기 장비·방어 장비·공격 아이템·지원 아이템·기타), 공격 자세는 무기 장비만, 방어·공격·기타는 A 돌진, 지원은 C 맥동과 빛 → 두 번째 검토(Round 24 README). 그 전: 열 번째 판정 — 치유의 지팡이는 무기, 선명도 MaxSize 2048. **Round 23 설계안 + Round 24의 승인 뒤 구현**. 그 전: 아홉 번째 판정 "모두 맞음 승인" — **공격·피격 자세 열두 장 확정**, 새 규칙(용병의 기본 그림이 확정되면 공격·피격 자세를 더한다). 그 전: 여덟 번째 판정 — 성기사 공격을 손을 고쳐 다시 그림, 손의 규칙은 양손에 각각 장비를 든 경우에만. 그 전: 일곱 번째 판정 — 머리 띠를 남녀로, **성기사 연결**(체인·스크린샷 통과), **용병 여섯의 공격·피격 자세 생성**(Round 23, 호출 12회, 판정 대기). 그 전: 재발 방지를 넣고 성기사를 다시 그림(Round 22). 그 전: 여섯 번째 판정 "권장안으로 모두 반영" — **무덤 2안-B 구현**(체인·스크린샷 통과). 다음 절차는 사용자가 정했다: 용병 이미지 재확인 → 캐릭터들의 공격·피격 이미지 생성 → 승인 후 구현. 그 전: 진지한 표정 여섯 연결, 정보칸 제자리 구현, Round 19 모션 시험. 모두 2026-10-05 세션 정리에서 커밋·푸시됨)

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
- 그 뒤 **쿨다운 빛**(Round 18, 목업 B안 촛불 금빛 + 전투의 빈 칸 어둡게)을 구현했다(아래 절). 같은 날 밤 세 번째 세션 정리에서 네 커밋(기획 / 화면 / 그림 파이프라인·기록 / Roadmap·Handoff)으로 올리고 푸시했다.
- 그 뒤 Round 19~25(공격·피격 모션 시험, 진지한 표정, 무덤, 성기사의 머리, 공격·피격 자세 열두 장과 연결, 아이템 분류 다섯과 움직임, 외곽선 2/3)를 진행했다(아래 절).
  2026-10-05 세션 정리에서 여섯 커밋(기획 / 데이터 / 그림 파이프라인과 그림 / 화면 / 기록 / Roadmap·Handoff)으로 올리고 푸시했다. 목업 MP4를 올리려고 `.gitattributes`에 `*.mp4`를 LFS로 더했다.
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

## 쿨다운 빛 (Round 18, 2026-10-04 밤)

- 지시 "쿨타임 표시: 어두운 상태에서 시작 > 밝아지면서 완전 밝아지면 동작 / 붉은색에서 다른 색으로(여러 권고안) / 가리는 것이 칸에 꽉 차게(더 바자르·백팩 배틀즈 참고). 검토 후 보고" → 두 게임 확인(위키·Steam 스크린샷·상점 영상. 출처는 README) →
  목업(`Archive/18-cooldown-light/mock_cooldown_light.py`: 지금 스크린샷의 칸을 선형 색공간으로 다시 그림, "지금"의 재현 오차 채널당 1~2) 지금 / A 뼈빛 / **B 촛불 금빛(권장)** / C 영혼 푸른빛 / D 비전 보라, 한 주기, GIF, 빈 칸 → **"1. 권장 2. 어둡게 3. 권장안. 구현"**.
- 기획 먼저: Design/10 §5(10-03 아이콘 항목에 화살표, 새 【확정】 "쿨다운은 빛으로": 사용자가 고른 것과 승인한 세부를 나눠 적음).
- 코드: `UiPalette`(`Gauge` 삭제, `ChargeDark`·`ChargeLight`·`ChargeEdge`), `BattleItemView`(`UiBar` 대신 `_light`·`_dark`·`_darkEdge`·`_glow`·`_front`. `ShowCharge`가 충전만큼 금빛의 끝과 어둠의 시작과 앞머리를 옮김, `Pulse`가 어둠을 0으로 했다가 번쩍임이 사라지는 만큼 돌려놓음. 시험용 `Charge`·`DarkFrom`·`LitTo`·`ShowsFront`·`Darkness`, 상수 `Rim` 2·`ChargeShade` 0.85),
  `UiPrefabSetup.Battle`(`BuildBattleItem`: 칸 = `slot`, `ItemUnder`(금빛) → 이름·아이콘 → `ItemOver`(어둠·부드러운 끝·빛·선) → 번쩍임(안쪽 2). `BuildEmptyCell`에 `EmptyCellDark`), `Kit`(`CooldownAlpha` → `ChargeTint` 0.22·`FrontGlowAlpha` 0.45·`FrontLineAlpha` 0.9, `Tinted`), `UiArt.ChargeRamp`.
- 조각(호출 없음): `Archive/18-cooldown-light/draw_pieces.py`의 `charge_ramp`(64x8, 흰색, 왼쪽 투명 → 오른쪽 불투명, 제곱 경사) → `Assets/@Art/UI/Icon`. 앞머리의 빛(금빛)과 어둠의 부드러운 끝(어둠)에 같이 쓴다.
- `ArtPipeline/tools/review_sheet.py`: 팔레트 확인 목록에서 쓰지 않던 `Gauge`를 뺐다. 목업 GIF(6.8MB)를 올리려고 `.gitattributes`에 `*.gif`를 LFS로 더했다.
- Test: `Battle_ItemCells_StartDark_AndLightUpFromTheLeftAsTheyCharge`(새). 문서: Architecture/12("UI의 그림", "디아블로 컨셉"의 색·보드·**쿨다운 = 빛**, "아이템의 아이콘", "전투 화면", "연출", Test), 13(도형 조각), Roadmap, `Archive/18-cooldown-light/README.md`.

## 공격·피격 모션 시험 (Round 19, 2026-10-04 밤)

- 지시 "전투시 공격 모션과 피격 모션을 만들어 공격 및 피격시 적용해 보려해. 우선 테스트로 현재 발키리 이미지 기반 공격, 피격 모션 한개 씩 생성(이미지의 일관성을 절대적으로 유지되도록)" → 모션 = 행동마다 자세 한 장을 지금의 돌진·밀림 동안 바꿔 끼우는 것(권장안, 다키스트 던전 방식)으로 시험했다.
- 생성(호출 2회, 약 $0.05): 확정 원본을 `--reference`로, 시험 세트 `Archive/19-motion-test/STYLE_RUNTIME-motion.md`(§3 "같은 캐릭터, 자세와 표정만")와 소재 `attack.csv`·`hit.csv`(정체 목록 + 자세), `--size 1536x1024`(이번에 `gen_image.py`에 더함, Architecture/13). `input_fidelity=high`는 모델이 거절(400, 비용 없음)해 뺐다.
- 맞추기(호출 없음): `fit_motion.py` — 배율(공격 1.12, 피격 1.07: 금 장식 지름·자루 굵기로 잼), 디딘 뒷발 기준, 두 배 폭 캔버스 1344x896, 색 맞춤(ΔE ≤ 1.3, `color_drift.py`). 리뷰 시트 `review-motion.png`(`review_motion.py`), 목업 `mock-*.png`·`mock-motion.gif`(`mock_motion.py`: ko_15 위에 무대 조명 모형, 배경 차이 평균 0.5~1.1).
- 후보는 `candidates/`(생성 그대로, 색 맞춤, 넓은 캔버스). `Assets`·데이터·화면 코드는 그대로다.
- 첫 판정: ① 같은 인물 **확정** ② 공격은 "도끼가 바닥에서 꺾였어 … 하단부에 여백, 충분히 아래로(필요시 규칙 변경)" ③ "GIF 움직임 없음" ④ 번쩍임 "권장안대로"(피격 자세가 있는 유닛은 절반) ⑤ "대기".
- 공격 2차(호출 1회, $0.030): `make_reference.py` → `reference-floor.png`(확정 원본을 1536x1024에 0.76배, 발바닥 높이의 3/4), `STYLE_RUNTIME-motion2.md`(§3 자리 그대로, §4 무기는 바닥선 아래로·전부 보임), `attack2.csv`.
  맞추기 규칙 변경: 바닥선 = 발바닥(가장 낮은 점이 아님), 자세 캔버스 1344x1008(바닥선 아래 112), 색 맞춤은 나아질 때만(공격 2차는 생성 그대로 ΔE ≤ 2.1). 배율 1.05. 도끼날이 바닥선 아래 54px(캔버스).
- 두 번째 판정: 공격 2차 "충분히 내려왔어", "움직임도 적당해" — **확정**. 정보칸 "같이 움직이는게 맞는가? 그대로 있는게 좋지 않을까? 검토" → `mock_plates.py`(호출 없음): `mock-plates.mp4`(위 지금 / 아래 제자리), `mock-plates.png`.
  권장: 돌진·밀림 동안 명패는 열에 그대로, 걸어가기(전진)는 함께(채택하면 `BattleUnitView`가 명패에 걸어가기의 이동만 준다). `mock_motion.Scene.frame`에 `plates_follow`, `idle`(Round 20이 씀)을 더했다.
- 커밋할 때: `mock-motion.mp4`·`mock-plates.mp4`(각 약 7MB)를 위해 `.gitattributes`에 `*.mp4`를 LFS로 더한다(Round 18의 `*.gif`처럼). `mock-motion.html`(3.3MB, 그림을 담은 글 파일)은 그대로 둔다.

## 표정 시험 (Round 20, 2026-10-04 밤)

- 지시 "게임 분위기가 밝고 명랑하지 않으니 전투 준비자세에서 웃는 것보다 진지하고 약간 심각한 느낌. 필요시 api로 테스트 이미지 비교. 검토 후 보고".
- 확정 원본을 `--reference`로, 시험 세트 `Archive/20-serious-face/STYLE_RUNTIME-face.md`(§2에 웃는 얼굴 금지, §3 같은 그림·표정만)와 소재 `serious.csv`(A 진지함)·`grim.csv`(B 약간 심각함), 1024x1024. 호출 2회, 약 $0.05.
- 비교 시트 `review-face.png`(`review_face.py`: Round 19의 전투 목업에 그림만 바꿔 게임 크기 / 같은 배율의 얼굴 2배 / 전신). 후보는 `candidates/`. 권장 A(B는 분노·고통 쪽이라 피격 자세와 겹친다).
- 채택하면: Design/10 §2 → 스타일 문서 §4·소재 여섯의 표정 → 직업 여섯을 같은 방법으로(호출 6회, 약 $0.16) → 얼굴 다시 자르기. 공격 자세의 함성은 "fierce". 몬스터 표정은 따로.
- GIF는 정상(브라우저 재생 확인), 앱 미리보기가 첫 장만 보인다 → `mock-motion.mp4`(`encode_mp4.swift`: macOS AVFoundation, 보통 3회 + 4배 느리게)와 `mock-motion.html`(1x·0.5x·0.25x, 한 프레임씩)을 더했다. 목업의 피격 번쩍임은 절반.

## 표정 채택과 무덤 제안 (세 번째 판정, 2026-10-04 밤)

- 판정: "1.2.권장안 반영 3. '우스꽝스럽게 위협적인' 얼굴 / 추가 제안: 캐릭터들이 죽을 경우 무덤(십자표시) 이미지(공통 사용), 관련 이미지 제안 / 내 승인 후 연결".
- 정보칸 제자리(구현): `BattleUnitView.Update`가 그림에는 모든 이동을, 명패에는 걸어가기만 준다(`PlateRect` 추가). Test `Battle_APlateStaysInItsColumn_WhileItsFigureLungesOrRecoils`(배치 실행은 프레임이 짧아 시간으로 기다린다). Design/10 §5, Architecture/12 "연출".
- 표정 A(채택): Design/10 §2·§3, `STYLE_RUNTIME.md` §4 얼굴 문구, `Rosters/character.csv` 여섯의 표정. 직업 다섯을 `Archive/20-serious-face/STYLE_RUNTIME-face2.md` + 확정 원본으로(호출 5회). 발키리는 시험의 A.
  비교 `review-six.png`. **연결은 승인 뒤**: `output/character/<job>_serious.png` → `Assets/@Art/Unit/Job/<job>.png`, 원본을 라운드의 `approved/`로, `cutface.py`로 얼굴, 체인·스크린샷(README "승인되면 연결").
- 무덤(Round 21, 제안): `gen_image.py`에 타입 `prop`(figure 맞추기, 데이터 Id 없음)을 더했다. 시험 문서 `Archive/21-grave/STYLE_RUNTIME-grave.md`(§23·§24), `grave.csv`, 세 안(호출 3회). 권장 B 돌 묘비.
  규칙: 죽음으로 비게 된 열(살아 있는 줄 바로 뒤)에 무덤 + 이름만 남긴 어두운 명패, 노드 맵·보상도(`ExpeditionMember.Alive`), 몬스터는 잔상만. 목업 `mock_grave.py`(Round 19의 `Scene` 위에 파티 넷을 다시 세움. 파티 열은 FieldLayout 셈보다 4px 오른쪽).

- 네 번째 판정: "1. 연결 2. C 3. 무덤 규칙 — 몬스터 제외(다르게 처리 예정), 나머지는 목업 확인 후. 1안 권고안 / 2안 잠깐 나타났다 소멸, gif로".
  연결: 이전 그림을 `Archive/20-serious-face/before/`에, 새 그림 여섯을 `Assets/@Art/Unit/Job`과 `output/character/<job>.png`(이전 출력은 `old-smile/`)에, 확정 원본을 `approved/character/`에, 얼굴 여섯(보정값 그대로), 기준 그림 시트 다시 합성(`make_reference_sheet.py`).
  무덤 목업: `Archive/21-grave/mock_grave_motion.py` → `mock-grave-1.gif`·`-2.gif`(+MP4, steps PNG). Round 19·21의 목업 스크립트는 스크린샷의 인물을 `before/`로 찾고 지금 그림을 그린다(`mock_motion.SHOT_ART`).

## 무덤 2안-B 구현 (여섯 번째 판정, 2026-10-04 밤)

- 판정: "Gif 파일이 안움직여. 2안-b를 mp4로 제공해줘" → `mock-grave-2b.mp4`. "권장안으로 모두 반영 — 이후 진행 예정 절차: 용병 이미지 재확인. 캐릭터들에 대한 공격 및 피격 이미지 생성. 내 승인 후 구현".
- 기획: Design/10 §5 【확정】(바로 무덤, 0.7초 + 0.15초를 배속으로 나눔, 기다리는 동안의 일, 판정은 그대로, 몬스터 제외), Design/02 §2 "전진"에 화면 쪽 메모.
- 파이프라인: 타입 `prop` 정식(`STYLE_RUNTIME.md` Generation Types 행과 §23·§24, `Rosters/prop.csv`의 `grave`), `output/prop/grave.png`(`--refit`이 게임의 그림과 바이트까지 같음), 확정 원본 `Archive/21-grave/approved/prop/grave.raw.png`. Architecture/13.
- 그림: `Assets/@Art/UI/Frame/grave.png`(`UiArt.Grave`. 계획의 `Unit/grave.png` 대신 UI 조각: Address 없이 Prefab이 직접, UI Import 정책). Builder가 `BattleScreen._grave`에 넣는다.
- 화면: `BattlePresenter`(용병의 `Died` → `partyFell`, 적은 `Ghost`), `BattleScreen`(`OnPartyFell` → `BattleFxLayer.Grave`, `GraveHold` 0.7·`GraveVanish` 0.15를 배속으로 나눔. `Render`가 `_fx.GravesLeft > 0`이면 파티의 `Place`를 `hold`),
  `BattleFxLayer`(`Grave`, `GravesLeft`, `GhostsShown`. 무덤은 발에서 세우고 시간은 나타난 Frame부터), `BattleUnitView`(`StandIn`: 배지는 화면이 옮길 때). Architecture/12 "연출".
- Test: `Battle_AFallenMercenary_TurnsIntoAGrave_AndThoseBehindWaitUntilItHasGone`, `Battle_AGraveGoesSooner_AtAHigherSpeed`, 스크린샷 `AGrave_Korean`(`ko_21`, `ko_22`). `UiTestUtil.EnterAFirstBattleWhereRow1Falls`, `AdvanceUntilFallen`.

## 성기사의 머리 (Round 22, 2026-10-04 밤)

- 지시: "성기사 얼굴이 다른 캐릭터들에 비해 작아서 이상해보여. 재방방지 대책을 세우고, 성기사 이미지 재생성".
- 잰 것(맞춘 캔버스, 눈에서 턱·수염 끝): 기사 77, 마검사 68, 주교 56, 대마법사 55, 발키리 51, 성기사 41 → 다시 그림 81. 원인: 소재 "5등신·긴 다리", 넓은 자세의 폭 맞춤(693px), 용병끼리 머리를 맞대 보지 않은 리뷰.
- 재발 방지: Design/10 §2 【확정】, `STYLE_RUNTIME.md` §4 "always with a big head …", `Rosters/character.csv`의 `paladin`(약 4등신, 큰 머리, 짧은 다리, 방패·가시 철퇴), `tools/review_sheet.py`(character: 지금 게임의 용병 전원과 한 줄, 머리 줄과 `FACE_BAND` 남성 65~85px·여성 48~62px(성별은 소재의 `Gender`), `near_eye`), Architecture/13.
- 다시 그림: `gen_image.py --type character --key paladin --name paladin_head --style Archive/22-paladin-head/STYLE_RUNTIME-head.md --reference Archive/20-serious-face/approved/character/paladin.raw.png`(호출 1회, $0.027). 후보 `candidates/`, 비교 `compare-paladin.png`, 리뷰 시트 `review-paladin.png`.
- 일곱 번째 판정: "규칙: 머리 크기는 남성, 여성 살짝 다르게(여성이 소폭 작게) 하고 서로 비슷한 크기로(지금 정도의 범위)", "1. 연결", "2. 생성(이전 발키리 모션 생성과 같은 규칙으로)".
  띠를 남녀로(남성 65~85px, 여성 48~62px. `Rosters/character.csv`에 `Gender` 열, `review_sheet.face_band`). 연결: 이전 그림·얼굴을 `before/`, `Assets`의 그림, 확정 원본 `approved/`, 얼굴 보정값 130·68, 체인·스크린샷(`game/`).

## 공격·피격 자세 (Round 23, 2026-10-04 밤)

- 세트 `ArtPipeline/Archive/23-motion-six/`: `STYLE_RUNTIME-motion.md`(Round 19 v2 + 지금 §1·§2), `attack.csv`·`hit.csv`(직업마다 정체 목록 + 자세 + 표정), `make_references.py` → `references/`(발 아래 여백, 공격은 왼쪽 3분의 1·피격은 가운데).
- 생성: 열두 장 `output/character/<key>_<attack|hit>.raw.png`(호출 12회). Round 19의 발키리 산출물은 `output/character/round19/`로 옮겼다(`mock_poses.py`가 Round 19 장면을 만들 때 그쪽을 읽는다).
- `fit_poses.py`: 배율(금 원판 큰 넷의 중앙값 비, `RIGID`는 대마법사 수정, `SCALE`은 마검사 부츠 버클 0.93·1.0과 성기사 피격 0.92), 디딘 뒷발(부츠 가죽색의 맨 왼쪽 무리)과 그 발바닥, 팔레트 색 맞춤(나아질 때만), 넓은 캔버스 2016x1008. `fit.txt`.
- `review_poses.py` → `review-poses.png`, `mock_poses.py` → `mock-poses.mp4`(Round 19 장면의 1열을 직업마다, 1열 명패는 뺌)와 `mock-steps.png`. 후보 `candidates/`(raw와 wide).
- 아홉 번째 판정 "모두 맞음 승인"으로 열두 장 확정. 게임 연결 설계안은 README "게임 연결 설계안"(결정 1은 Round 24의 분류로, 결정 2는 MaxSize 2048).
- 여덟 번째 판정 "성기사 공격시 무기를 든 손이 반대로 바꼈어. 해당 그림만 다시 그려주고 재방방지 대책 마련해줘":
  시험 문서 v2 `STYLE_RUNTIME-motion2.md`(손을 지킴), 소재 v2 `attack2.csv`(든 손을 적음, 성기사 공격은 오른팔로 내리치고 방패는 몸 앞), `paladin_attack2`(호출 1회). `fit_poses.DRAWN`이 첫 그림 대신 쓰게 하고 리뷰·목업도 따른다.
  `review_poses.HANDS`가 양손에 각각 장비를 든 용병(성기사)의 든 손을 적는다. Design/10 §5 【확정】, Architecture/13 "타입"에 손과 자세 그림의 점검 항목. 마검사 공격도 먼 팔로 찔렀지만 사용자 판정으로 그대로 둔다
  ("양손에 장비를 각각 들고 있는 경우에만 해당 규칙을 적용 … 마검사 처럼 한손으로 무기 들고 있는 경우는 … 일단 현행 유지"). 소재 v2(`attack2.csv`·`hit2.csv`)는 성기사 행만 v1과 다르다.
- 연결(열두 번째 판정 "승인", README "연결"): 그림 `Assets/@Art/Pose/Job/<id>_attack|hit.png`(성기사 공격은 `paladin_attack2`), 주소 `ArtAddress.PoseOf`(`pose/job/<id>-attack|hit`, `Figure`에서), `JobData.AttackPose`·`HitPose`,
  `ArtSetup`의 `Pose` 정책(전신 그림과 같되 MaxSize 2048), `ExpeditionArt.AttackPoseOfMercenary`·`HitPoseOfMercenary`.
  화면: `FigureView.ShowPose`(그림 자리를 3배 폭·1.125배 높이로, 피벗은 자세 캔버스의 바닥선 112/1008)·`ShowFigure`, `BattleUnitView.Lunge(withPose)`·`Recoil`(자세를 `PoseExtra` 0.05초 더), `HasHitPose`(번쩍임 절반), `BattleScreen`이 파티를 `Bind`할 때 자세를 넘김.
  파이프라인: 타입 `attack`·`hit`(`gen_image.py`의 `TYPES`, 기준 그림 `pose_reference`가 확정 원본으로 만듦, 생성은 원본만), `tools/fit_pose.py`(소재의 `Scale`이 잰 배율을 덮어씀), `tools/review_pose.py`, 스타일 문서 §25·§26, `Rosters/attack.csv`·`hit.csv`.
  열두 장의 원본을 `output/attack/`, `output/hit/`로 옮겨 다시 맞추니 열 장은 바이트까지 같고 대마법사 둘은 1px 달라 도구의 산출물로 바꿨다.

## 아이템 분류와 발동할 때의 움직임 (Round 24, 2026-10-04 밤)

- 세트 `ArtPipeline/Archive/24-support-motion/`: 첫 검토(치유의 지팡이를 무기로, 지원 아이템의 움직임 세 안 A 돌진 / B 솟구침 / C 맥동과 빛, `mock_support.py` → `mock-support.mp4`·`mock-support-steps.png`), 두 번째 검토(분류 다섯).
- 열한 번째 판정: 분류 다섯(무기 장비·방어 장비·공격 아이템·지원 아이템(회복·버프)·기타 아이템), 치유의 지팡이는 무기 장비·버클러는 방어 장비, 공격 자세는 무기 장비만, 방어·공격·기타는 A, 지원은 C. 열두 번째 판정 "승인".
- 기획: Design/02 §4(분류 다섯 【확정】, 무기 패시브는 무기 장비의 피해에만), Design/07(ItemData), Design/10 §5(분류마다 움직임).
- 데이터: `ItemCategory { Weapon, Support, Armor, Attack, Other }`(`DataEnums.cs`), `ItemData.csv` 네 칸(치유의 지팡이 → `Weapon`, 버클러 → `Armor`, 불씨 플라스크·저주의 침 → `Attack`), 생성 JSON. Architecture/05.
  시뮬: `Tools/Sim`의 원정을 시드 7, 200회 × 파티 둘(기사·마검사·주교·대마법사 / 성기사·발키리·주교·대마법사)로 바꾸기 전후에 돌려 출력이 같다.
- 문구: `Item.Weapon`·`Item.Armor`·`Item.Attack`·`Item.Support`·`Item.Other`(`UI_StaticText.csv`, String Table, `UiKeys.Item`), `UiText.CategoryKey`.
- 화면: `BattlePresenter.Motion`·`MotionOf`(무기 장비 → `Strike`, 지원 → `Pulse`, 나머지 → `Lunge`), `BattleUnitView.Pulse` → `FigureView.Pulse`(`PulseSwell` 6%, `PulseOut` 0.1초, `PulseLength` 0.3초, 빛 `PulseLight` 60%에서 사라짐).
  빛 조각은 Builder가 그림 자리마다 그림 뒤에 둔다(`UiPrefabSetup.Battle.BuildFigure`의 `<name>Glow`: `UiArt.Glow`, `ChargeEdge`, 300px, 가슴 높이 `PulseGlowHeight` 0.55). Architecture/12 "연출".
- Test: EditMode `ItemMotionTests`, `ShippedDataTests.ShippedItems_AreClassedAsDecided`, `DefinitionTests`(자세 주소), `ArtSetupTests`(자세의 경로·Entry). PlayMode `Battle_AMercenaryShowsItsAttackPoseWhileItsWeaponLunges_AndItsHitPoseWhileItRecoils`, `Battle_ASupportItemPulsesItsOwner_WithALightBehind`(`UiTestUtil`의 `UntilFigure`).
  스크린샷 `PosesAndPulse_Korean`(`ko_23_battle_attack_pose`, `ko_24_battle_hit_pose`, `ko_25_battle_support_pulse`).

## 캐릭터 외곽선 2/3·1/2 (Round 25, 2026-10-04 밤)

- 지시: "전체적으로 캐릭터 외곽선 굵기를 2/3, 1/2로 얇게 만든 전투 목업 화면을 보여줘", "발키리 기준으로 각 외각선에 대한 동작 화면 목업도 제공해주고".
- `Archive/25-outline-thin/thin_outline.py`: 확정 원본을 `FIGURE_OUTLINE` 0으로 맞춘 뒤 소수 폭의 띠(이웃한 두 정수 띠의 섞음, `ring_alpha`)를 두른다. 자세는 `fit_pose.fit_pose`의 `ring`·`place`를 잠깐 바꿔 끼워 같은 띠로.
  띠 4는 Assets 스물세 장과 바이트까지 같다. 두께 측정 `thickness`. 산출물 `ArtPipeline/output/outline/<2-3|1-2>/`(Git 밖).
- `run_shots.sh <scratch>`: 프로젝트(Assets, Packages, ProjectSettings, Library)를 `cp -c`로 복제해 그림을 바꿔 끼우고 스크린샷 Test를 돌린다(작업 트리는 그대로, 한 안에 약 75초). Addressables는 Asset Database 모드라 바꾼 파일을 읽는다.
  1층 전투의 적은 실행마다 새로 정해진다(2/3 안에서는 쥐). 그래서 비교는 4층 전투(ko_15)와 1층의 파티만. `compare_shots.py` → `compare-battle.png`, `compare-zoom.png`, `game/`.
- 열네 번째 판정 "권장안으로 변경 구현"(2/3 안): Design/10 §2 【확정】, `gen_image.py`의 `FIGURE_OUTLINE` 1.35와 `ring_alpha`(정수 폭은 이전과 같고 사이의 폭은 두 띠의 섞음. `fit_pose.ring`도 같은 것), Architecture/13.
  이전 그림·자세·얼굴은 `Archive/25-outline-thin/before/`. 전신 열한 장 `--refit`, 자세 열두 장 `fit_pose.py`(목업의 2/3 그림과 바이트까지 같음), 얼굴 열하나 `cutface.py`(이전 그림에서 자르면 이전 얼굴과 같다, 11/11).
  이 폴더의 `thin_outline.py`·`mock_outline_motion.py`는 "지금"을 `before/`에서 읽는다.
- `mock_outline_motion.py`: Round 23의 `Six` 장면에 안마다의 그림(아스트리드 세 장, 세드릭, 약탈자, 주술사), 그림은 `drawn()`(밉맵 2단계 + Bilinear: 게임과의 차이 8.0, LANCZOS 16.4, 1단계 11.2). `mock-motion.mp4`, `mock-motion-steps.png`.

## Open

- **외곽선 2/3**(Round 25)은 플레이로 본다: 얇아진 외곽선으로 유닛이 배경과 구별되는지(어두운 옷의 몬스터, 밝은 바닥 위의 발), 보스(150%)의 인상. 조절은 `FIGURE_OUTLINE` 하나로 한다:
  전신 `gen_image.py --refit`, 자세 `tools/fit_pose.py`, 얼굴 `tools/cutface.py`를 다시 돌려 Assets에 넣는다(호출 없음). 1/2 안은 0.4.
  참고: 게임은 그림을 밉맵 2단계(1/4 해상도)에서 읽어 키워 그려 파일보다 부드럽다(Bilinear 필터, 자리 225x300에 텍스처 672x896). 선을 또렷하게 하려면 필터나 밉맵 설정을 따로 검토할 수 있다(지금은 그대로).

- **공격·피격 자세와 지원의 맥동**은 플레이로 본다: 자세가 보이는 0.35초(돌진)·0.31초(밀림)가 짧거나 길지 않은지, 자세에서 대기 그림으로 돌아올 때 튀지 않는지, 피격의 절반 번쩍임,
  맥동의 부풂(6%)과 빛의 세기(60%), 주교가 치유의 지팡이로 돌진하는 인상. 조절은 `BattleUnitView.PoseExtra`, `FigureView`의 `Pulse*`, `UiPrefabSetup.Battle`의 `PulseGlow*`로 한다.

- **쿨다운 빛**은 플레이로 본다: 어둠 85%에서 아이콘이 읽히는지, 보드 전체가 어두워진 인상, 금빛의 세기(22%), 앞머리 빛과 선, 번쩍인 뒤 어둠이 돌아오는 0.3초. 조절은 `BattleItemView`의 `ChargeShade`와 `UiPrefabSetup.Kit`의 `ChargeTint`·`Front*`로 한다.
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

- 외곽선 2/3(Round 25) `Tools/chain.sh`(2026-10-04 밤): setup OK(그림 서른넷 다시 Import: 전신 열하나, 자세 열둘, 얼굴 열하나), sim OK, EditMode 625/625, PlayMode 51/51(`[Explicit]` 스크린샷 7개 제외).
  `Tools/screenshots.sh` 41장(7/7): 4층 전투·1층 전투의 얇아진 외곽선(`Archive/25-outline-thin/game/implemented_*.png`, `before-after.png`). `git diff --check` 깨끗.
  목업의 게임 스크린샷(프로젝트 복제, 두 안 모두 7/7·41장)은 같은 폴더의 `game/`·`compare-*.png`.
- 공격·피격 자세 연결 + 아이템 분류(Round 23·24) `Tools/chain.sh`(2026-10-04 밤): setup OK(자세 열두 장 Import, 화면 Prefab 셋의 빛 조각, 스탬프), sim OK, EditMode 625/625, PlayMode 51/51(`[Explicit]` 스크린샷 7개 제외).
  `Tools/screenshots.sh` 41장(7/7): `ko_23`(아스트리드의 공격 자세), `ko_24`(세드릭의 피격 자세), `ko_25`(엘라의 맥동과 빛), 아이템 설명의 "무기 장비"(`ko_08`·`ko_17`). `Archive/23-motion-six/game/`, `24-support-motion/game/`.
  데이터 변경 전후의 시뮬(시드 7, 원정 200회 × 파티 둘) 출력이 같다. `git diff --check` 깨끗.
- 성기사 연결 `Tools/chain.sh`(2026-10-04 밤): setup OK(그림 둘 다시 Import), sim OK, EditMode 616/616, PlayMode 49/49. `Tools/screenshots.sh` 38장(6/6), `ko_21`·`ko_22`의 세드릭(`Archive/22-paladin-head/game/`).
- 무덤 2안-B `Tools/chain.sh`(2026-10-04 밤): setup OK(`grave.png.meta`, `BattleScreen.prefab`의 `_grave`, 스탬프), sim OK, EditMode 616/616, PlayMode 49/49(`[Explicit]` 스크린샷 제외). `Tools/screenshots.sh` 38장(6/6):
  `ko_21_battle_grave`(1열의 무덤, 뒤의 셋은 4·3·2열의 Column·배지·보드에서 기다림), `ko_22_battle_after_grave`(1~3열로 옮겨 감). 두 장은 `Archive/21-grave/game/`.
- 진지한 표정 연결 `Tools/chain.sh`(2026-10-04 밤): setup OK(그림 열두 장 다시 Import), sim OK, EditMode 616/616, PlayMode 47/47. `Tools/screenshots.sh` 36장(5/5): 여섯 모두 웃지 않는 얼굴, `_15`에서 돌진 중인 아스트리드의 명패가 열에 그대로. 여섯 장은 `Archive/20-serious-face/game/`.
- 정보칸 제자리 `Tools/chain.sh`(2026-10-04 밤): setup OK(바뀐 생성물 없음), sim OK, EditMode 616/616, PlayMode 46/47 — 새 Test가 프레임 수로 기다려 실패 → 시간으로 고친 뒤 `chain.sh playmode` 47/47(`[Explicit]` 스크린샷 5개 제외). `git diff --check` 깨끗.
  스크린샷은 돌리지 않았다(움직임이라 정지 화면에 보이지 않는다. 목업 `Archive/19-motion-test/mock-plates.mp4`).
- 앞선 라운드는 모두 체인 통과(상세는 Git의 이 파일과 각 라운드 README): 쿨다운 빛 PlayMode 46/46·스크린샷 36장, 외곽선 1/2·눈동자 45/45·36장, 촛불 빛 45/45·36장, 개정 6 44/44·34장,
  새 그림 연결·플레이 피드백·UI 꾸미기·포션 칸·디아블로 컨셉 각각 44/44·34장. 스크린샷은 각 라운드의 `game/`과 `before-after.png`.
- Round 19~21의 그림은 리뷰 시트와 목업으로 확인했다(각 라운드 폴더). `gen_image.py --dry-run`으로 `--size`와 타입 `prop`의 조립을 확인했다.

## 알아둘 것

- 얼굴 일관성이 필요한 재생성(자세 변경)은 포스터가 아니라 **그 캐릭터의 확정 그림**을 `--reference`로 붙이고 `Archive/12-roar-style/charm/STYLE_RUNTIME-pose.md`(§3이 "같은 캐릭터, 자세만")로 돌린다. 발키리의 준비 자세가 이렇게 나왔다.
- 소재가 길다. 체형(등신 포함) → 머리·옷·무기 → 자세 → 얼굴의 순서로 적으면 모델이 잘 따른다.
- 세트 폴더마다 스타일 문서와 소재가 있어 그 변형으로 더 그릴 수 있다(`--style`, `--roster`). 산출물 이름은 `--name <key>_<set>`.

## Next Action (제안)

- 사용자가 새 외곽선(2/3)을 플레이로 본다(Open의 항목).
- 사용자가 공격·피격 자세와 지원의 맥동을 플레이로 본다(Open의 항목). Roadmap "사후 검토 대기"의 Round 23·24 구현 항목을 검토한다.
- 커밋하지 않을 것(세션 정리 때마다): Unity가 만든 `Assets/AddressableAssetsData/OSX.meta`, `ProfileDataSourceSettings.asset(.meta)`, `ProjectSettings/ScriptableBuildPipeline.json`.
0. 사용자가 Unity에서 새 런으로 직접 플레이한다: 쿨다운 빛(어둠 85%에서 아이콘이 읽히는지, 금빛과 앞머리, 번쩍인 뒤 어둠이 돌아오는 것), 촛불 빛(반원의 세기, 양초와 함께 내려가고 폭풍에 꺼지는 것), 새 칸(180x60, 간격 2)의 인상과 아이콘의 여백, 새 그림체의 인상과 간격·겹침·키, 보스의 1.5배, 디아블로 UI(돌 패널, 쇠 명패, 뼈색 칸, 비네트 세기)와 양초의 흔들림·가늘어짐·꺼짐·연기(폭풍은 전투 시작 후 `StormStartMs`).
   조절은 데이터 한 칸(`EnemyData.FigureScale`)이나 `UiPrefabSetup.Kit`·`Battle`의 상수로 한다. 옛 저장은 그림만 바뀌어 그대로 이어지지만 새 런이 보기 좋다.
1. Roadmap "사후 검토 대기"의 Round 12~14, 개정 6, Round 16~18, Round 21~24 항목(권고안으로 정한 세부)을 사용자가 검토한다. 바꾸면 체인·스크린샷으로 확인하고 커밋한다.
2. 스프라이트 여럿의 설계안(용병의 `Figure` 열 + 소재의 `Variant` 열)을 보여 주고 승인받은 뒤 구현한다.
3. 키와 폭【미결】: 넓은 자세의 캔버스·그림 자리를 넓힐지 사용자가 정한다.
4. Deferred UI 틀(인벤토리 팝업, 보상 카드, 타이틀·로비·정산)은 요청이 있을 때 같은 컨셉으로 그린다.
