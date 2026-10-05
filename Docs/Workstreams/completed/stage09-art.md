# 9단계 — 그림

Snapshot: 2026-10-03 (9단계는 완료: 그림 열하나를 게임에 연결했고 체인을 통과했다. 그 뒤의 그림은 배경 -> UI -> 아이템 순서로 하기로 했고, 광산 배경이 확정(잠정)됐다. 그 배경을 전투 화면에 깔고 전투의 캐릭터 간격을 승인된 목업(B안)대로 고쳤다(체인 통과). 이어서 전투 UI의 그림 여덟 장을 생성했고, 목업이 승인돼 전투 화면과 노드 맵·보상의 파티 쪽에 넣었다(체인 통과). 그 뒤 아이템의 아이콘 스물한 개를 아이템 칸의 모양에 맞춰 그렸고, 사용자가 전부 확정해 전투 화면의 칸에 넣었고, 파티 쪽 칸도 목업 A안(아이콘과 등급 배지)으로 넣었다(체인 통과). 2026-10-03 세션 정리에서 전부 커밋하고 푸시했다. 그 뒤 새 그림체를 시험했다: 치비(직업 둘)는 폐기, 다크 판타지 1장은 보류. 이어서 전투와 노드 맵·보상의 아이템 칸을 하단 보드 패널로 옮겼다(목업 A안 승인, 구현, 체인 통과). 띠 아이콘 셋은 판정 대기. 같은 날 두 번째 세션 정리에서 커밋하고 푸시했다)

## Goal

스타일 문서가 승인됐다. 승인된 그림만 `Assets`에 배선됐고 체인을 통과한다(`Docs/Roadmap.md` 9단계).

## State

- 끝났다. 직업 여섯과 버려진 광산의 적 다섯의 전신 그림이 전투, 노드 맵, 보상 화면에 선다. 체인 통과, 스크린샷으로 확인.
- 승인과 결정의 기록: Roadmap "9단계의 승인". 사용자가 그림마다 판정했고, 연결은 승인받은 Architecture/13 "Unity 배선"대로 했다.
- 직업 광전사를 발키리(`valkyrie`)로, 그 용병을 아스트리드(`astrid`)로 바꿨다(`Docs/Design/05_Mercenary_Growth.md` §1).
- 유료 호출 15회, 약 $0.44(상한 $10). 장부는 `ArtPipeline/Archive/calls.csv`.
- **커밋됐다** (2026-10-03 세션 정리. 9단계와 그 뒤의 그림 라운드를 네 커밋으로: 그림 파이프라인 / 그림의 데이터와 연결 / 화면 / Roadmap·Handoff).
  같은 날 두 번째 세션 정리에서 그림체 시험과 보드 패널을 네 커밋으로 더 올렸다(그림 파이프라인 / 그림체 시험 기록 / 화면과 얼굴 / Roadmap·Handoff).
  기준 그림은 저장소에 넣었다(사용자가 직접 만든 그림이라는 전제. 저장소가 공개다). 승인된 원본(`ArtPipeline/Archive`)도 저장소에 있다(약 80MB).
- 이 단계와 무관한 변경(사용자의 Unity Editor가 바꾼 설정 파일들. 아래 "알아둘 것")은 되돌렸고 커밋하지 않았다.

## 9단계 뒤의 그림 라운드

- **순서 (사용자, 2026-10-02): 배경 -> UI -> 아이템.**
- 아이템의 아이콘은 처음에 **보류**됐다(2026-10-02). `item` 타입을 넣고 정사각 아이콘으로 시험 2개(`longsword`, `ember_flask`)를 생성했는데, 사용자가 방향을 정했다:
  장비칸에서 이름 글자를 빼고 아이콘을 장비칸에 가로로 넣는다. 다만 **아이템 UI의 크기가 정해진 뒤, 가장 마지막**에 그린다. 그 시험은 `Archive/03-items/v1`에 있다.
  전투 UI에서 칸의 크기가 정해진 뒤 다시 시작했다(아래 "아이템의 아이콘 — 판정 대기").
- 배경: 필요한 배경을 검토했다. 지금 화면에서 배경이 넓게 보이는 곳은 타이틀, 정산, 전투의 무대, 보상과 노드 맵의 파티 쪽이고 로비는 패널이 거의 다 가린다.
  필요한 곳은 셋(던전, 타이틀, 용병단 본부)이다.
- 사용자 지시: "현재 그림체를 기준으로 '슬레이 더 스파이어'와 '다키스트 던전' 배경 구성을 참고하여 광산 배경 생성."
  `background` 타입을 넣었다(`STYLE_RUNTIME.md` §10~§13과 `Dungeon: abandoned_mine`의 `### Place`, `Rosters/background.csv`, `gen_image.py`의 타입별 Style Rule·Forbidden·크기와 `scene` 후처리).
  두 게임의 이름은 프롬프트에 넣지 않고 구성만 풀어 적었다. 버려진 광산의 배경 한 장(2304x1536)을 생성해 전투 화면 목업 둘과 함께 보냈다.
  3:2로 여유 있게 그렸고 발 높이는 Roster의 `FloorLine`(57%)이다. 기록과 원본은 `ArtPipeline/Archive/04-backgrounds`(README, `approved/`).
- **판정 (2026-10-02, 사용자)**: ① 광산 배경 확정(임시, 추후 변경 가능) ② 캐릭터 뒤의 옅은 색 틀은 뺀다 ③ 다음 배경(타이틀, 본부)은 보류
  ④ 전투 관련 UI를 만들기 전에 캐릭터의 간격 조절을 먼저: "다들 거리가 너무 일정해", Darkest Dungeon을 참고해 같은 편끼리는 조금 가깝게, 가운데와 양 끝은 약간 여백.
  "전투 배경만 적용 후 간격 조절 목업 이미지 제공. 내 승인 후 구현." `Design/10` §4에 적었다.
- 간격 조절 목업: 세션 scratchpad의 스크립트가 수치로 다시 그린 전투 화면이다(실제 스크린샷과 같은 상태를 그려 맞는지 대조했다). 지금, A, B, C를 나란히 제시했다.
  1920 기준, 같은 편의 캐릭터 간격 / 두 1열 Column 사이의 틈 / 양 끝의 여백: 그 전 232.5 / 40 / 20, A 205 / 140 / 80, **B 190 / 180 / 120**, C 175 / 220 / 160.
  카드를 제자리에 두고 그림만 모으는 안은 그림이 제 카드에서 최대 79px 어긋나 권하지 않았다. 목업은 보관하지 않는다(`ArtPipeline/output/review/battle-spacing-*.png`).
- **승인 (2026-10-02, 사용자): "권고안대로 진행"** — B안과 함께 제시한 것 전부: 카드가 캐릭터 아래에서 함께 좁아짐(222.5 -> 180), 그림은 제 크기로 겹쳐 서고 앞 열이 위,
  전장을 가로지르는 바닥선을 뺌, 노드 맵과 보상의 파티도 같은 간격, 배경은 전투 화면에만, 넘치는 이름은 글자를 줄임.
- **구현했다** (배경을 게임에 넣는 것과 간격을 함께. 아래 "Done"의 "9단계 뒤").
- **전투 UI — 승인돼 게임에 넣었다.**
  - 사용자 지시: "전투 관련 Ui 관련 이미지 생성 - 유사 유명 게임 검토 후 최선안 적용", "현재 그림체 스타일도 반영해서 생성". 목업을 본 뒤 "권장안 반영 구현".
  - 검토: Darkest Dungeon, Slay the Spire, The Bazaar, Backpack Battles의 전투 화면(Steam 스토어의 스크린샷을 열어 봤다). 가져온 것과 확정된 배치는 `ArtPipeline/Archive/05-ui/README.md`.
  - 배치(1920 기준): 그림 자리(262~562) 아래에 **명패** 180x92(y 568. 열 번호 배지, 이름 20px, HP 막대와 숫자, 상태 줄), 그 아래 **아이템 칸** 180x60을 간격 4로 세로로(y 666부터).
    큰 색 카드와 머리 위의 열 이름이 없다. 헤더는 높이 84의 패널, 포션 띠와 안내문 판. 편의 색·빈사·포션 대상은 명패의 그림으로 보인다.
    파티 쪽(노드 맵, 보상): 그림 자리 186~486, 명패 492, "앞으로/뒤로" 590, 칸 636부터(6칸이면 1016까지), 안내 줄 1024.
  - 파이프라인: `ui_frame`·`ui_icon` 타입(`STYLE_RUNTIME.md` §14~§18, `Rosters/ui_frame.csv`·`ui_icon.csv`의 `Size`·`Outline`), `gen_image.py`의 `fit_ui`
    (오려 내 `Size`로 맞추고 유닛의 외곽선 색으로 고른 띠를 두른다. 틀은 늘이고 바탕색을 한 색으로 편다. 아이콘은 비율을 지킨다),
    `tools/ui_variants.py`와 `Rosters/ui_variant.csv`(한 틀에서 색만 다른 Sprite를 만든다).
  - 그림: 원본 여덟은 `Archive/05-ui/approved`. 게임의 Sprite는 `Assets/@Art/UI/Frame`(`panel`, `plate_party`·`_enemy`·`_danger`·`_target`·`_label`, `slot`, `slot_selected`, `button`)과
    `Assets/@Art/UI/Icon`(`shield`, `burn`, `deaths_door`, `storm`).
  - 목업은 세션 scratchpad의 스크립트가 그렸다. 산출물은 `Archive/05-ui`의 `mock-battle.png`, `mock-compare.png`, `review-kit.png`.
- **아이템의 아이콘 — 확정돼 전투와 파티 쪽에 들어갔다 (2026-10-03).**
  - 사용자 지시: "아이템 아이콘 다시 생성 / 현재 아이템 칸에 맞추어 생성 / 2칸 이상의 아이템은 그 크기에 맞추어 생성", 이어서 "내 승인 후 연결".
  - 파이프라인: `item`의 후처리를 `icon`(256x256 정사각)에서 **`cell`**로 바꿨다. 칸의 수는 `ItemData.Size`에서 읽는다(`read_item_cells`).
    `ITEM_CELLS`: 1칸은 `1536x512`로 생성해 320x88로, 2칸은 `1536x1024` -> 320x216, 3칸은 `1024x1024` -> 320x344(칸에서 테를 뺀 자리의 2배). 둘레의 띠 4px.
    `STYLE_RUNTIME.md` §7은 크기와 상관없는 공통 규칙으로 줄이고 칸 수마다 구도의 절을 뒀다(§19 띠 — 긴 것은 눕힌다, §20 가로 사각형 — 비스듬히, §21 정사각 — 대각선).
    `Rosters/item.csv`는 `ItemData`의 스물한 개 전부다(`Key`, `Subject`, `Reference`). `review_sheet.py --type item`은 아이콘을 실제 칸(게임의 `slot` 그림)에 넣어 보여 준다.
    다른 타입의 프롬프트가 한 글자도 바뀌지 않은 것을 확인했다(스무 줄, 바꾸기 전의 스타일 문서와 대조).
  - 생성: 칸 크기별로 넷(`longsword`, `ember_flask`, `spear`, `halberd`)을 먼저 그려 칸에 맞는지 본 뒤 나머지 열일곱을 이었다. 호출 21회, 약 $0.39, 실패와 재생성 없음.
    산출물은 `ArtPipeline/output/item/<Key>.png`와 `.raw.png`(gitignore). **`approved/`로 옮기지 않았고 `Assets`에 넣지 않았다.**
  - 제시한 것: `Archive/03-items/review-icons.png`(실제 칸에), `mock-battle.png`(4 대 4), `mock-battle-boss.png`(감독관과 쥐). 목업은 세션 scratchpad의 스크립트가 게임의 Sprite로 그렸다.
  - 판정할 때 볼 것(README): ① 감독관의 포효가 붉은 피부다(감독관은 초록. 권장: 감독관의 그림을 붙여 1회 다시) ② 작은 것 넷(플라스크, 물어뜯기, 버클러, 포효)은 눕지 않고 가운데에 서 있다(권장: 그대로)
    ③ 적만 쓰는 능력 넷의 소재는 내가 골랐다 ④ 소드는 짙은 진홍 날이라 덜 도드라진다.
  - **판정 (사용자): "확정 및 연결"** — 스물한 개 전부(포효의 붉은 머리도 그대로. 재생성 없음)와 목업의 전투 칸 방식. 원본을 `Archive/03-items/approved`로 옮겼다.
  - 연결(아래 "Done"의 "9단계 뒤 (아이템의 아이콘)"): 데이터의 `Icon` 열 -> `Assets/@Art/Item/<Id>.png` -> Entry와 Import 정책은 `ArtSetup` -> `ExpeditionArt`가 읽어 둠 -> `BattleItemView`가 칸에 놓는다.
    적의 칸은 Scale -1로 뒤집고, 쓸 수 없는 아이템의 아이콘은 `UiPalette.IconDim`을 곱한다. 아이콘이 없는 아이템은 이름을 적는다(Placeholder).
  - **파티 쪽 칸(노드 맵, 보상)**: 세 안의 목업(`Archive/03-items/mock-party-cells.png`)에서 사용자가 A안을 골랐다("권장안대로"): 전투와 같은 아이콘, 등급은 칸 왼쪽 아래의 놋쇠 배지.
    `ItemSlotView.Show(item, icon, selected, interactable)`가 아이콘과 배지를 보여 주고 글자를 감춘다. 아이콘이 없는 아이템은 이름과 등급 글자, 빈 칸은 "빈 칸".
    `PartyColumnView.Show`가 `ExpeditionArt`를 받아 직업의 그림과 아이템의 아이콘을 스스로 고른다. 배지는 명패의 열 번호 배지와 같은 조각(`KitBadge`. 24px, 왼쪽 아래에서 5px).
- 남은 것(요청이 있을 때): 포션의 아이콘. 인벤토리 팝업과 보상 목록의 아이콘은 그 UI를 그릴 때. 타이틀과 본부의 배경은 보류.
  노드 맵·보상 화면의 오른쪽과 타이틀·로비·정산은 도형 UI 그대로다.
- **그림체 시험 (2026-10-03).** 사용자 지시: "현재 그림체 일단 백업 해놓고 새로운 그림체를 테스트".
  - 지금의 그림체는 바꾸지 않았다. 스타일 문서, 직업 소재, 기준 그림을 `Archive/06-style-test/before/`에 복사했다(Git `32e47fe`에도 있다).
  - `gen_image.py`에 `--style`, `--roster`, `--reference`(그대로 붙임)를, `review_sheet.py`에 `--reference`를 더했다. 플래그 없이 돌리면 전과 같다(dry-run으로 확인).
  - 시험 1 (치비) — **폐기** (사용자): 올린 그림을 기준 그림으로 기사와 대마법사를 그렸다(호출 2회, 약 $0.06). 시험 세트와 그림, 리뷰 시트는 지웠고 README에 기록만 남겼다.
  - 시험 2 (다크 판타지) — **판정 대기**: 사용자 지시 "백업 원본이미지에서 어두운 느낌 부여(다크 판타지) 테스트 이미지 1개 생성". `Archive/06-style-test/dark/`에
    `character`만 있는 시험용 `STYLE_RUNTIME.md`(같은 캐릭터를 같은 방식으로 다시 그리되 색·그림자·낡음·분위기만 어둡게)와 소재(`valkyrie` 하나). 기준 그림은 `before/`의 원본을 그대로(왼쪽을 본다, `Flip` false).
    후보 `valkyrie_dark`(호출 1회, 약 $0.03, 경고 없음). 원본과 맞춘 그림은 `dark/`에, 리뷰 시트는 `review-dark-vs-original.png`. 판정할 때 볼 것은 README.
    사용자가 **보류**했다. 세트는 그대로 둔다.
  - 시험 3 (Round 11, 같은 날 저녁) — **실패, 처음부터 다시** (사용자): 지시 "컨셉 이미지 변경을 시도. 캐릭터부터. 현재 스타일 백업 후 새 컨셉 테스트". 백업 `Archive/11-concept-test/before/`(06 뒤에 바뀐 것까지).
    C1 전달서("Cute Clean Cartoon", 사용자 작성)로 3장 → 그림체 변형 5장 → 큐트/지금 섞기 4장(60/40 선택) → 60/40의 얼굴 5장 → 미국 TV 카툰 1장, 그리고 지시를 잘못 읽은 화풍 5장. 호출 23회, 약 $0.57.
    세트·그림·리뷰 시트를 지우고 README(기록, 방법의 교훈)와 `before/`, 전달서 `C1_STYLE_TRANSFER.md`만 남겼다. 지금의 그림체·Assets·코드는 그대로.
- **전투 UI 개정: 보드 패널 (2026-10-03) — 구현됨, 커밋됨.** 사용자 지시: 아이템 칸을 가로로, 다키스트 던전처럼 하단 패널에 캐릭터마다(열 순서) 가로 칸, 오른쪽은 몬스터, 양 끝에 얼굴.
  목업 넷(지금, A, B, C)을 보고 **"A안으로 구현 진행. 노드 맵·보상 화면은 별도 목업 제공"**.
  - 화면: `BattleBoardView`(새 View), `BattleUnitView`에서 칸을 뺌, `BattleItemView`의 가로 칸(`CellWidth`, `CellGapX`, `BoardWidth`. 세로의 `CellGap`·`BoardHeight`는 파티 쪽이 그대로 쓴다),
    `BattleScreen`이 무대의 View와 패널의 줄을 함께 만들고 옮긴다(`Place<T>`), Builder의 패널·줄 여덟·`BoardTemplate`·`PotionTargetButton`.
  - 그림: `ArtAddress.FaceOf`, `JobData.Face`·`EnemyData.Face`(`[JsonIgnore]`. Generated JSON은 그대로), `ExpeditionArt`의 얼굴, `ArtSetup`의 얼굴 Entry와 정책, `Assets/@Art/Face/<Job|Enemy>/<Id>.png` 열하나.
  - 파이프라인: `tools/cutface.py`, Roster의 `FaceDx`·`FaceDy`, `gen_image.py`의 `ITEM_CELLS`, `STYLE_RUNTIME.md` §20·§21(띠), `review_sheet.py`의 가로 칸.
    띠 아이콘 셋(`longbow`, `spear`, `halberd`)을 생성했다(호출 3회). 미늘창은 폭의 39%만 채운다. **판정 대기, `Assets`에 넣지 않았다**(세로 모양의 맞춘 그림은 `output/item/old-vertical/`).
  - Test: `DefinitionTests.Face_...`, `ArtSetupTests`(얼굴의 경로와 Entry), PlayMode 전투 Test 넷이 패널의 줄을 본다.
  - 문서: Architecture/12("전투 화면", "아이템의 아이콘", "유닛의 그림", Test), 13("후처리 (`cell`)", "얼굴", "Unity 배선", Test), 04, 01, Design/10 §5·§6, Roadmap, `Archive/07-battle-panel/README.md`.
  - 파티 쪽(노드 맵, 보상): 별도 목업(`Archive/07-battle-panel/mock-party-compare.png`: 지금 / A안 / B안)에서 사용자가 **A안**을 택해 구현했다.
    `PartyColumnView`가 무대의 Column과 패널의 줄(`_line`, `_face`, `_slotParent`)을 함께 보여 준다, `ItemSlotView.SetWidth`, `PartySideView.LayoutColumns`가 줄도 감춘다.
    Builder `PartySide.cs`(무대 262, 패널·줄·설명 줄·"인벤토리로", 팝업 높이 640), `NodeMap.cs`(맵 630, 노드 정보와 버튼을 패널로), `Reward.cs`(안내와 버튼을 패널로).
    Test의 버튼 경로가 `Frame/BoardPanel/...`로 바뀌었다(Enter, InventoryToggle, Skip, RewardToInventory). 그림 Test에 파티 줄의 얼굴.
  - 스크린샷 34장(`Tools/screenshots.sh`): 전투(`_05`, `_15`, `_18`)의 패널이 목업과 같다. 둘은 `Archive/07-battle-panel/game/`에 두었다.
- 누계: 호출 137회, 약 $3.37(상한 $10). (연출 3차까지 59회 $1.41, Round 11 컨셉 시험 23회 $0.57, Round 12 그림체 전환 55회 $1.39. 전환의 기록은 `completed/a-art-restyle.md`)

## Done

- Owner 문서: `13_ART_PIPELINE.md`(새 문서. `CLAUDE.md` §2, §8에 등록), `01`(`ArtPipeline`, `.venv`, `@Art`), `04`(`unit/...` Address, `F1-Art`, Entry의 출처),
  `05`(`Figure` 열), `09`(표현의 범위), `12`("유닛의 그림", `PrepareAsync`).
- 기획: `Design/10`(G9, 그림체, 몬스터와 던전 컨셉), `05` §1(발키리), `02`·`07`·`08`·`00`(id).
- `ArtPipeline`: `STYLE_RUNTIME.md`(공통 규칙, `character`와 `enemy`의 구도와 기준 그림 문구, `Dungeon: abandoned_mine`), `Rosters/character.csv`·`enemy.csv`,
  `tools/gen_image.py`·`run_roster.py`·`review_sheet.py`·`cutout.py`, `References/Character/style_ref_mercenary.jpg`,
  `Archive/calls.csv`, `Archive/01-characters`·`02-enemies`(README, 리뷰 시트, `approved`의 원본, 이전 판 `v1`~`v3`). `.venv`(gitignore).
- 그림: `Assets/@Art/Unit/Job/{knight,valkyrie,bishop,paladin,archmage,spellblade}.png`,
  `Assets/@Art/Unit/Enemy/{cave_rat,goblin_raider,goblin_archer,goblin_shaman,mine_overseer}.png` (672x896, 같은 바닥선).
- Data: `JobData.Figure`, `EnemyData.Figure`(`FigureKey`), Mapper의 `Figure` 열, CSV 둘과 Generated JSON. `MercenaryData.csv`(astrid).
- Editor: `ArtSetup`(Address -> 파일, Entry, Import 정책, `FindProblems`), `AddressablesSetup.Art.cs`(Group `F1-Art`, Scope Expedition), `ProjectSetup`이 부른다.
- UI: `UnitFigures`(Static Data가 이름 붙인 그림을 Expedition Scope에 읽는다), `FigureView`(그림 또는 Placeholder), `UIScreen.PrepareAsync`와
  `UIManager.ShowAsync`(읽는 동안 새 화면을 감춘다), `BattleScreen`·`NodeMapScreen`·`RewardScreen`, `BattleUnitView`·`PartyColumnView`·`PartySideView`,
  Builder의 `BuildFigure`(Placeholder 묶음과 그림 Image). 생성물: Prefab 셋과 Stamp, Font Atlas(새 글자), Addressables 설정과 Group.
- Test: `ArtSetupTests`(새 파일), `DefinitionTests.Figure_...`, `StaticDataTransformerTests`(Figure 열), `TestCsv`,
  PlayMode `UiFlowTests.Figures_ShowTheArtTheDataNames_...`, `BattleEngineTests`의 이름.

9단계 뒤 (배경과 간격):

- Owner 문서: `12`("유닛의 그림", "파티 쪽", "전투 화면", Test), `13`("후처리 (`scene`)", "Unity 배선"), `04`(`background/dungeon/<key>`), `05`(`Background` 열), `01`, `09`(표현 범위에 전투 배경).
  기획: `Design/10` §4(배경과 전투 무대).
- `ArtPipeline`: `background` 타입(`STYLE_RUNTIME.md` §10~§13, `Dungeon`의 `### Place`), `Rosters/background.csv`(`FloorLine`),
  `gen_image.py`의 `scene` 후처리(모든 배경을 같은 캔버스 2304x1536, 같은 바닥선 57%로 맞춘다), `Archive/04-backgrounds`(README, `approved`).
- 그림: `Assets/@Art/Background/Dungeon/abandoned_mine.png`(2304x1536, 불투명).
- Data: `DungeonData.Background`(선택 열), `ArtAddress`(`FigureKey`를 대신한다. `Figure`와 `Background`의 값 검사), Mapper, `DungeonData.csv`와 Generated JSON.
- Editor: `ArtSetup`이 던전의 배경도 Entry로 내고 종류별 Import 정책을 쓴다(배경: 불투명, Mipmap 없음, MaxSize 4096).
- UI: `FieldLayout.SideGap` 180, 전장의 상자(`BattleFieldLeft` 120, `BattleFieldWidth` 1680), `BuildFigure`(색 틀 없음, 그림은 225x300으로 가운데 아래에, 전투에서는 Column 폭의 투명한 클릭 자리),
  Column을 뒤 열부터 만든다, 전투 화면의 `Background` Image와 `BattleScreen.PrepareAsync`(던전의 배경을 읽는다), 바닥선 삭제(전투),
  `UiBuild.ShrinkToFit`(유닛 이름 28 -> 최소 20, "앞으로/뒤로" 20 -> 최소 14), `ItemSlotView`(이름 아래에 등급, 한 Text의 두 줄). 생성물: Prefab 셋과 Stamp, Addressables Group.
- Test: `FieldLayoutTests`(새 파일), `DefinitionTests.Background_...`, `StaticDataTransformerTests`(Background 열, 빈 칸), `ArtSetupTests`(배경의 경로와 Entry),
  PlayMode `UiFlowTests.Battle_IsFoughtInFrontOfTheBackgroundTheDungeonNames`, 그림 Test에 "그림은 제 크기로 서고 그림 자리가 클릭을 받는다"를 더했다.

9단계 뒤 (전투 UI):

- Owner 문서: `12`("UI의 그림"(새 절), "파티 쪽", "전투 화면", Test), `13`("후처리 (`frame`, `glyph`)", 색 변형, "Unity 배선"의 UI), `01`, `04`(UI 그림은 Entry가 아니다), `09`(표현 범위).
  기획: `Design/10` §5(전투 UI, 아이템 칸 180x60).
- Editor: `UiArt`(UI 그림의 이름과 Border), `ArtSetup`(종류별 Import 정책을 한 구조로. UI는 PPU 200과 Border), `ProjectSetup`(그림을 화면 Prefab보다 먼저),
  `UiPrefabSetup.Kit.cs`(틀, 색을 입히는 버튼, 틀 안의 막대, 명패, 포션 띠), `UiPrefabSetup.Battle.cs`와 `PartySide.cs`의 새 배치.
- UI: `BattleUnitView`(명패의 그림을 편과 상태로 고른다. 열 번호, 상태 줄의 칩), `BattleItemView`(칸이 쿨다운. 칸 60, 간격 4. 도형 아이콘 삭제),
  `PotionSlotView`·`ItemSlotView`(보통과 고른 것의 Sprite), `BattleScreen`(열 이름 삭제), `PartySideView`(바닥선 삭제), `UiPalette`(`Brass`, `Ink`, `Gauge`의 색. `Icon` 삭제).
  문구: `Battle.Hp`·`Board.Hp`(`{0}/{1}`), `Battle.Shield`·`Battle.Burn`(`{0}`), `Battle.DogGrace`·`Battle.DogRolling`(짧게). 생성물: Prefab 셋과 Stamp, String Table.
- Test: PlayMode `Battle_AUnitAtDeathsDoor_ShowsItOnItsPlate`(새), 포션 Test에 명패의 그림(아군, 적, 포션 대상), 그림 Test에 열 번호 배지.
  스크린샷 `ko_18_battle_deaths_door`(`UiScreenshotTests.DeathsDoor_Korean`), 빈사를 운에 맡기지 않는 `UiTestUtil.ReachDeathsDoorInTheFirstBattle`.

9단계 뒤 (아이템의 아이콘):

- Owner 문서: `13`(타입 표의 `item`, "후처리 (`cell`)", "Unity 배선"의 아이템, Import 정책, Test), `12`("아이템의 아이콘"(새 절), "전투 화면", "파티 쪽"의 보류 표시, Test, Deferred),
  `05`(`Icon` 열), `04`(`item/<key>`), `01`(`@Art/Item`), `09`(표현 범위). 기획: `Design/10` §5(칸에 맞춰 그린다 — 사용자 지시. 스물한 개와 전투 칸의 방식 — 확정), §6(파티 쪽 칸의 【제안】).
- `ArtPipeline`: `STYLE_RUNTIME.md` §7, §19~§21, `Rosters/item.csv`(스물한 줄), `gen_image.py`(`cell` 후처리, `ITEM_CELLS`, `read_item_cells`, `fit_ui`의 채움 비율),
  `review_sheet.py`(아이템 모드: 실제 칸, Roster의 순서), `Archive/03-items`(README, 리뷰 시트, 전투 목업 둘, 파티 쪽 칸의 목업, `approved`의 원본 스물한 장, `v1`).
- 그림: `Assets/@Art/Item/<Id>.png` 스물한 장(320x88, 320x216, 320x344).
- Data: `ItemData.Icon`(선택. `ArtAddress.Optional`), Mapper의 `Icon` 열(필수 Header), `ItemData.csv`와 Generated JSON.
- Editor: `ArtSetup`(아이템의 Entry와 `Icon` 정책 = UI 아이콘의 정책), `UiPrefabSetup.Battle.cs`의 `BuildBattleItem`(칸 안의 `ItemIcon` Image. 여백은 `Kit.cs`의 `ItemIconMarginX`·`Y`),
  `Kit.cs`의 `KitBadge`(명패의 열 번호와 칸의 등급이 같은 배지를 쓴다. `GradeBadgeSize`·`Inset`), `PartySide.cs`의 `BuildItemSlot`(아이콘 Image와 등급 배지).
- UI: `UnitFigures` -> `ExpeditionArt`(`OfItem`을 더했다. 파일을 옮겨 GUID는 그대로), `BattleItemView`(`_icon`, `Bind(item, cells, icon, mirrored)`, `Icon`·`Mirrored` 속성),
  `BattleUnitView`(`Bind(unit, figure, art)`), `ItemSlotView`(`_icon`, `_badge`, `_grade`. `Show(item, icon, selected, interactable)`, `Icon`·`Grade` 속성),
  `PartyColumnView.Show(..., ExpeditionArt art, ...)`, `BattleScreen`·`NodeMapScreen`·`RewardScreen`·`PartySideView`(`_art`), `UiPalette.IconDim`. 생성물: Prefab 셋과 Stamp, Addressables Group.
- Test: `DefinitionTests.Icon_...`, `StaticDataTransformerTests`(Icon 열, 빈 칸), `TestCsv`(Items에 `Icon`), `ArtSetupTests`(아이템의 경로와 Entry),
  PlayMode 그림 Test에 "전투의 칸이 데이터의 아이콘을 보여 주고 이름을 감춘다, 적의 칸은 뒤집힌다, 아이콘이 없으면 이름; 파티 쪽의 칸은 아이콘과 등급 배지, 빈 칸은 글자만",
  전진 Test에 흐려진 아이콘의 색.

## 결정

사용자가 정한 것: 기준 그림과 그 스타일 / 시험 2장 먼저 / 예산 상한 $10, 그림은 직업에, 기준 그림은 저장소에 / 기준 그림에 맞춘 비율과 장식 /
용병은 전부 오른쪽 / 기준 그림의 캐릭터를 직업의 그림으로 / 여성 캐릭터는 기준 그림만큼 몸매가 드러나게, 대마법사는 다리 굵기를 원본만큼, 약간 슬렌더하게 /
광전사의 직업명을 바이킹 또는 발키리로, 용병 이름도 변경, id는 권장안대로 / 몬스터는 던전마다 컨셉 / 그림 열하나의 확정.

권장안으로 정한 것 (검토 대상. Roadmap "사후 검토 대기"):

- 직업명 **발키리**와 용병 이름 **아스트리드**. 그림이 직업에 붙고 그 그림이 여성 한 명이라 이름이 그림과 어긋나지 않는다.
- 모델 `gpt-image-2.5-sunburst`(설계안의 `gpt-image-2`는 투명 배경을 거절한다). 기준 그림을 직업에는 좌우로 뒤집어, 적에는 그대로 붙인다.
- 직업별 소재(성별, 옷차림, 무기), 몬스터의 크기(용병 90에 고블린 68, 쥐 40, 보스 97)와 소재의 세부.
- 발키리의 그림은 기준 그림을 다시 그리지 않고 오려 낸 것이다(`cutout.py`).
- 연결의 세부:
  - 그림은 데이터의 `Figure`(Logical Address)가 가리킨다. 빈 값이면 Placeholder다. Entry와 파일의 자리는 그 값에서 나온다.
  - Import 정책: Sprite(Single), Mipmap, Full Rect, MaxSize 1024, 높은 품질의 압축.
  - 화면은 열리기 전에 Static Data의 그림 **전부**(열하나)를 Expedition Scope에 읽는다. 화면마다 쓰는 것만 고르지 않았다: 수가 적고, Scope가 한 번 읽은 것을 갖고 있다.
  - (그림 뒤의 옅은 편 색은 배경을 깔면서 사용자가 빼기로 정했다. 빈사와 포션 대상은 카드의 색이 보여 준다.)
  - 보스(`mine_overseer`)도 다른 유닛과 같은 크기의 자리에 선다. 폭에 걸려 키가 용병과 비슷하다.

9단계 뒤에 사용자가 정한 것: 그림의 순서(배경 -> UI -> 아이템) / 아이템 칸은 이름 없이 아이콘을 가로로, 그리는 것은 UI 크기가 정해진 뒤 /
배경은 지금의 그림체로, 두 레퍼런스 게임의 구성을 참고 / 광산 배경 확정(잠정) / 캐릭터 뒤의 색 틀을 뺌 / 다른 배경은 보류 /
전투 UI보다 먼저 캐릭터 간격(같은 편끼리 가깝게, 가운데와 양 끝에 여백) / 목업 B안과 거기 딸린 것 /
전투 UI는 유사한 유명 게임을 검토한 최선안으로, 지금의 그림체로 / 목업의 권장안과 아이템 칸 180x60 /
아이템의 아이콘은 지금의 칸에 맞춰, 2칸 이상은 그 크기에 맞춰 그린다 / 아이콘의 연결은 승인 뒤에.

9단계 뒤에 권장안으로 정한 것 (검토 대상. Roadmap "사후 검토 대기"):

- 배경은 데이터가 가리킨다(`DungeonData.Background`, Address `background/dungeon/<key>`). 빈 값이면 배경색 그대로다.
- 모든 배경이 같은 캔버스(3:2)와 같은 바닥선(높이의 57%)을 쓴다. 화면은 배경마다 자리를 맞추지 않고 Frame의 폭으로 고정된 자리에 놓는다
  (바닥선이 그림 자리의 아래 끝에 온다). 화면이 16:9가 아니면 Frame 밖은 다른 UI처럼 배경색이다.
- 배경의 Import 정책(불투명, Mipmap 없음, MaxSize 4096).
- Slice A의 표현 범위에 전투 화면의 던전 배경을 넣었다(Architecture/09).
- 그림을 눌렀을 때 클릭을 받는 폭은 Column이다(그림이 옆으로 넘어간 부분은 옆 유닛의 것).
- 파티 쪽 아이템 칸을 이름과 등급 두 줄로 바꿨다(좁아진 Column에 한 줄로 들어가지 않는다). 글자를 줄이는 곳과 하한(이름 20, "앞으로/뒤로" 14).
- 노드 맵과 보상 화면에는 배경이 없어서 파티의 바닥선을 그대로 뒀다. 파티 아래의 안내 줄과 "인벤토리로"는 제자리다(왼쪽 절반을 쓴다).

## Open

- 사후 검토(Roadmap "사후 검토 대기"의 9단계 항목들).
- 그 전의 저장 파일(`run.json`)은 `bran`과 `berserker`를 가리켜 읽을 수 없다. 타이틀이 알리고 새 런으로 시작한다.
- 화면의 그림 자리는 높이 300이라 캐릭터의 머리가 40~45px로 보인다. 자리를 키울지, 보스를 크게 세울지는 플레이해 보고 정한다.
- 포션의 아이콘, 타이틀과 본부의 배경, 전투 말고의 화면의 UI 그림은 그리지 않았다. 그릴 때 종류마다 스타일을 승인받는다(Design/10 §6). 다른 던전의 컨셉과 배경도 그 던전을 만들 때.
- 배경 위에서 열 이름 글자와 포션 안내문이 덜 또렷하다. UI 단계에서 본다.
- 화면이 16:9보다 넓으면 배경의 양옆이 배경색으로 남는다(다른 UI도 Frame 안에만 있다).
- `ArtPipeline/Archive/01-characters/v1`~`v3`의 소재 목록에는 옛 Key(`berserker`)가 남아 있다(그때의 기록).
- 개정 2~4의 사후 검토와 플레이테스트는 그대로 열려 있다(`a-rows-items.md`, Roadmap "사후 검토 대기").

## Verification

- `Tools/chain.sh` (노드 맵·보상의 패널까지 넣은 뒤, 2026-10-03): setup OK, sim OK, EditMode 616/616, PlayMode 43/43(`[Explicit]` 스크린샷 4개 제외).
  (전투의 패널만 넣었을 때도 같은 수로 통과했고, 그때 두 번째 setup은 바뀐 파일이 없었다. 스크린샷 34장, 4/4 통과. 실행이 ProjectSettings나 Render 설정을 바꾸지 않았다.)
- `Tools/chain.sh` (전투 UI를 넣은 뒤): setup OK, sim OK, EditMode 611/611, PlayMode 43/43(`[Explicit]` 스크린샷 4개 제외). 체인의 setup이 두 번째 setup이었고 바뀐 파일이 없었다.
  (배경과 간격을 넣었을 때는 PlayMode 42/42, 9단계를 끝냈을 때는 EditMode 596/596, PlayMode 41/41.)
- `Tools/screenshots.sh` 34장 (전투 UI를 넣은 뒤): 전투(`_05`)가 승인된 목업과 같다. 빈사(`ko_18`)에서 붉은 명패와 해골과 남은 유예, 전진 뒤(`ko_15`)에서 보호막과 화상의 아이콘과 숫자,
  쓸 수 없는 아이템의 흐린 이름, 결과 창(`_06`)과 로그 보기(`_16`)의 패널과 버튼, 노드 맵(`_04`)과 보상(`_07`)의 명패·이동 버튼·두 줄 칸. 영어에서 "Forward"가 줄어 들어간다.
- `Tools/screenshots.sh` 33장 (배경과 간격을 넣은 뒤): 전투(`_05`, `_15`)가 승인된 목업 B안과 같다 — 광산 배경, 같은 편끼리 모인 그림, 가운데와 양 끝의 여백, 색 틀과 바닥선 없음.
  노드 맵(`_04`, `_17`)과 보상(`_07`)의 파티가 같은 자리에 서고 아이템 칸이 두 줄이다. 영어(`en_05`, `en_07`)에서 "Goblin Raider"와 "Forward"가 줄어 들어간다.
- `Tools/chain.sh` (아이템 아이콘을 전투와 파티 쪽에 넣은 뒤): setup OK, sim OK, EditMode 613/613, PlayMode 43/43(`[Explicit]` 스크린샷 4개 제외).
  두 번째 setup은 바뀐 파일이 없었다(522개의 해시 비교). (전투에만 넣었을 때도 같은 수로 통과했다.)
- `Tools/screenshots.sh` 34장: 전투(`_05`)의 칸에 아이콘이 쿨다운 채움 위에 놓이고 적의 칸은 뒤집혀 있다. 전진 뒤(`ko_15`) 4열로 간 소드의 아이콘이 흐리고 채움이 비어 있다.
  노드 맵(`_08`)과 보상(`_07`)의 파티 쪽 칸에 같은 아이콘과 왼쪽 아래의 등급 배지("10", "12")가 있고, 고른 칸은 놋쇠색이며, 빈 칸은 "빈 칸" 글자다. 영어도 같다.
- 아이템의 아이콘을 그렸을 때: `--dry-run`의 프롬프트(칸 수마다), 다른 타입의 프롬프트가 그대로인 것, 스물한 개가 캔버스 가장자리에서 잘리지 않은 것과
  가장자리에 밝은 테두리가 없는 것(픽셀로 쟀다), 리뷰 시트와 목업.
- `scene` 후처리: 발 높이를 45~70%로 그린 가짜 그림이 전부 57%로 맞춰지고, 확정된 광산 배경은 한 픽셀도 바뀌지 않는 것을 확인했다.
- 시뮬(같은 시드, 바꾸기 전과 뒤): 기본 파티 3,000회는 출력이 글자 그대로 같다(81.2% / 0.26). 발키리를 넣은 파티 1,000회는 이름만 바꾸면 같다(68.4% / 0.43).
- `Tools/screenshots.sh` 33장: 전투(`_05`)에 용병 넷과 동굴 쥐, 보스전(`_09`)에 광산 감독관, 노드 맵(`_04`)과 보상의 파티 열에 직업 그림, 로비(`_03`)에 "아스트리드 / 발키리".
  그림이 바닥선에 서고 열 안에 들어간다. 글자가 깨지지 않는다(Font Atlas를 다시 구웠다).
- `gen_image.py --dry-run`, 후처리의 가짜 그림 확인, `cutout.py`의 가장자리 확인은 그림을 만들 때 했다(라운드 README).
- `git diff --check` 깨끗. `Assets/InitTestScene*` 없음.

## 알아둘 것

- 그림을 더 그릴 때: `.venv/bin/python ArtPipeline/tools/gen_image.py --type <character|enemy|background> --key <Key>`. 호출 전 `--dry-run`. 같은 이름의 산출물이 있으면 호출하지 않는다.
  확정된 그림을 `Assets/@Art/Unit/<Job|Enemy>/<Id>.png`에 복사하고 CSV의 `Figure`를 채운 뒤 체인을 돌린다. Entry와 Import 설정은 setup이 만든다.
  던전의 배경은 `Assets/@Art/Background/Dungeon/<Id>.png`와 `DungeonData.csv`의 `Background`다. 그린 뒤 발 높이를 재서 Roster의 `FloorLine`에 적고 `--refit`한다.
- 그림체를 시험할 때: 지금의 문서·Roster·기준 그림은 두고, `Archive/<round>/<style-name>/`에 같은 형식의 시험 세트를 둔 뒤
  `gen_image.py --type character --key <Key> --name <Key>_<style-name> --style <문서> --roster <소재> --reference <기준 그림>`. `--reference`는 뒤집지 않는다.
  시트는 `review_sheet.py --names <시험>,<지금> --reference <시험의 기준 그림>`. 보기: `Archive/06-style-test/README.md`.
- 배경의 바닥선(57%)과 캔버스 비율(3:2)은 두 곳에 있다: `gen_image.py`의 `SCENE_FLOOR`·`SCENE_CANVAS`와 `UiPrefabSetup.Battle.cs`의 `BattleBackgroundFloor`·`BattleBackgroundHeight`. 함께 고친다.
- 전투 UI를 고칠 때: 그림 자리의 아래 끝(전장 위에서 300, 화면에서 562)이 배경의 바닥선과 맞물려 있다. 그림 자리를 옮기면 배경도 따라간다(`BattleFloorY`).
- UI 그림을 더하거나 바꿀 때: `gen_image.py --type ui_frame|ui_icon` -> (틀이면) `Rosters/ui_variant.csv`에 줄을 넣고 `ui_variants.py` -> `Assets/@Art/UI/<Frame|Icon>`에 복사
  -> `UiArt`에 이름(틀은 Border)을 적는다 -> 체인. 색만 바꾸려면 `ui_variant.csv`의 `Fill`만 고치고 다시 만든다(호출 없음).
- 아이템의 아이콘을 그릴 때: `gen_image.py --type item --key <ItemData.Id>`(칸의 수는 데이터에서 읽는다). 여럿이면 `run_roster.py --type item --max-calls N [--only a,b]`.
  본 뒤 `review_sheet.py --type item`. 칸의 크기는 두 곳에 있다: `gen_image.py`의 `ITEM_CELLS`(와 `review_sheet.py`의 `ITEM_CELL`)와 `BattleItemView.CellHeight`·`CellGap`. 함께 고친다.
- 빈사일 때 명패의 상태 줄에는 빈사만 보인다(보호막과 화상은 감춘다). 한 줄에 다 들어가지 않아서다.
- `ArtPipeline/output`은 gitignore다. 확정된 원본은 `Archive/<round>/approved`에 있다. 맞춘 그림은 원본과 Roster의 `Height`·`Flip`으로 다시 만들 수 있다(`--refit`).
- 키는 키체인에서 `security`가 읽는다. 값은 출력하지 않는다.
- **이 단계와 무관한 변경** (사용자의 Unity Editor가 만든 것. 세션 정리 때 되돌렸고 커밋하지 않았다. 추적되지 않는 생성물은 작업 폴더에 남아 있다):
  `Assets/Settings/UniversalRP.asset`, `Assets/UniversalRenderPipelineGlobalSettings.asset`, `ProjectSettings/ProjectSettings.asset`,
  `ProjectSettings/ScriptableBuildPipeline.json`, `Assets/AddressableAssetsData/OSX.meta`, `ProfileDataSourceSettings.asset`(와 `.meta`).
  `Assets/AddressableAssetsData/AddressableAssetSettings.asset`의 변경(새 Group)은 이 단계의 것이다.
- 사용자가 자리에 없어 Unity Editor를 이쪽에서 껐다. 종료 요청이 보조 프로세스로 먼저 가서 `Temp`가 지워졌고, 본체는 종료 도중 멈춰 프로세스를 끝냈다.
  잃은 것은 창 배치 저장뿐이다.

## Next Action (제안)

1. ~~띠 아이콘 셋의 판정~~ — 2026-10-03 개정 5(칸 100x72, `Archive/08-panel-cells`)로 띠 아이콘은 쓰지 않게 됐다. 아이템의 크기와 아이콘은 사용자가 따로 재조절한다(미정).
2. 다크 판타지 시험(보류 중)의 판정(`Archive/06-style-test/README.md` "판정할 때 볼 것"). 버리면 바꿀 것이 없다. 채택하면 Design/10의 G9를 먼저 고치고, 확정된 그림을 새 기준 그림으로
   삼아(`References/Character/`) 지금 문서의 색·그림자·분위기 문구를 고친 뒤, 종류마다 승인 라운드로 다시 그려 배선한다(선·비율은 같으니 범위는 판정 때 정한다). 그 전까지 게임은 지금 그림체다.
3. 사용자가 Unity에서 직접 플레이해 배경, 간격, 보드 패널, 아이콘이 들어간 전투와 노드 맵을 본다(`Assets/@Scenes/Boot.unity`. 옛 저장은 이어지지 않으니 새 런).
4. 사후 검토(Roadmap "사후 검토 대기")와 개정 2~4의 사후 검토(`completed/a-rows-items.md`).
5. 그림을 더 그릴 때는 이 문서의 "알아둘 것"과 `Docs/Architecture/13_ART_PIPELINE.md`를 따른다. 포션의 아이콘이 다음 후보다. 10단계(소리)와 11단계(플레이테스트와 Slice B)는 대기.
