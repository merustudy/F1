# Round 28 — 고블린 주술사의 무기와 공격 자세, 감독관 망치의 그리는 순서 — 확정, 구현됨 (몬스터 자세의 게임 연결 포함)

사용자 지시 (2026-10-05, Round 27의 판정):

- "고블린 주술사에게 저주의 침 대신 무기 장비류(생성)를 새로 만들어주고 주고 공격 이미지 하나 생성"
- "감독관 망치 내릴때 이미지가 용병 뒤쪽이 아닌 앞쪽으로 배치(망치 가려짐)"
- 이어서: "나머지는 만족. 일단 주술사 공격 이미지 내가 확인 후 최종 구현 요청 할게"

그래서 이번에는 **주술사의 공격 자세 한 장만** 그렸다(호출 1회). 새 무기의 데이터·아이콘과 몬스터 자세의 게임 연결은 최종 구현 요청 때 한다(아래 "최종 구현 때"). `Assets`, 데이터, 화면 코드, `tools/`는 그대로다.

## 새 무기 (권장안, 최종 구현 때 넣는다)

- **등불 지팡이**(`lantern_staff`, Lantern Staff): 주술사가 이미 든 굽은 지팡이(끝의 고리에 녹슨 양철 등불)를 무기 장비로 삼는다. 그림이 바뀌지 않고, 아이콘은 그 지팡이를 그린다(Roster의 `Reference`가 주술사의 확정 그림: "유닛이 든 무기의 아이콘은 그 유닛의 그림과 같은 모습").
- 수치는 저주의 침 그대로(분류만 공격 아이템 → 무기 장비): 1칸, 쿨다운 3800, 뒤에서 3열까지, 화상(앞의 적 1) 30, 보상 가중치 0(적만). 저주의 침의 효과가 화상이라 등불과 어울린다. 바꾸기 전후의 시뮬(같은 시드)로 전투가 같은지 확인한다.
- 주술사의 아이템: `hex_spit:10+mending_chant:12` → `lantern_staff:10+mending_chant:12`. 저주의 침은 주술사만 들어서 쓰는 곳이 없어진다: 데이터에서 빼고 아이콘과 소재 줄을 Archive로 옮기는 것을 권장(남겨 둘 수도 있다).

## 공격 자세 (호출 1회, 약 $0.031)

- 규칙은 Round 27과 같다: 시험 문서 `../27-monster-poses/STYLE_RUNTIME-motion.md`, 기준 그림 `../27-monster-poses/references/attack-goblin_shaman.png`(확정 원본을 오른쪽 3분의 1에), 소재 `attack.csv`(Round 27의 정체 목록 + 자세).
- 자세: 왼쪽으로 런지, 양손으로 굽은 지팡이를 머리 위에서 내리쳐 끝의 고리와 등불이 무릎 높이로 앞에 내려옴, 등불이 고리에서 흔들림, 귀·수염·목도리·누더기 옷이 뒤로 날림, 불·빛·효과 없음. 표정은 이 없는 입을 크게 벌린 낄낄대는 비명.
- 맞추기 `fit_shaman_attack.py`(Round 27의 도구로): 디딘 뒷발 1.065, 밀랍 1.101, 플라스크 1.044 → **1.065**, 색은 그린 그대로(맞추면 ΔE 2.7 → 6.3으로 나빠져서), 자세 캔버스 x 478..1300.
- 결과: 같은 주술사(모자와 양초, 수염, 목도리, 뼈 목걸이, 플라스크, 천, 맨발), 눈동자 없는 노란 눈, 머리는 대기·피격과 같은 크기. 리뷰 시트 `review-shaman.png`(대기 / 새 공격 / 확정된 피격).

```text
.venv/bin/python ArtPipeline/tools/gen_image.py --type enemy --key goblin_shaman --name goblin_shaman_attack \
  --style ArtPipeline/Archive/27-monster-poses/STYLE_RUNTIME-motion.md --roster ArtPipeline/Archive/28-shaman-weapon/attack.csv \
  --reference ArtPipeline/Archive/27-monster-poses/references/attack-goblin_shaman.png --size 1536x1024
.venv/bin/python ArtPipeline/Archive/28-shaman-weapon/fit_shaman_attack.py    # candidates/goblin_shaman_attack_wide.png, fit.txt, heads.json
.venv/bin/python ArtPipeline/Archive/28-shaman-weapon/review_shaman.py        # review-shaman.png
.venv/bin/python ArtPipeline/Archive/28-shaman-weapon/mock_shaman_overseer.py # mock-steps.png, mock-poses.mp4
```

## 감독관 망치의 그리는 순서

- **게임은 이미 적 1열을 파티 1열 위에 그린다**: 전장의 열은 맨 뒤 열부터 1열까지, 같은 열에서는 파티 다음에 적을 만든다(`UiPrefabSetup.Battle`의 Field). 그래서 감독관이 망치를 내리면 게임에서는 망치가 용병 앞에 보인다.
  Round 27의 목업은 반대로 아스트리드를 맨 위에 그렸다(Round 19 장면의 순서). 이번 목업은 게임의 순서로 그린다.
- 그 순서에서는 거꾸로 **용병의 무기가 적에게 가려질 수 있다**(지금 용병의 공격 자세는 적에게 닿지 않아 가려지는 장면이 없다: Round 23의 게임 스크린샷 `ko_23`). 권장(최종 구현 때): **공격하는 동안(돌진 + 0.05초) 공격하는 유닛을 맨 앞에** 그린다(양쪽 모두. 그 유닛의 열을 잠깐 마지막 자식으로). 목업은 이 규칙으로 그렸다.
- 본 것: 150%의 감독관이 망치를 내리면 망치머리가 아스트리드의 명패 오른쪽 위를 잠깐 덮는다(명패는 유닛과 함께 그려진다). 명패를 늘 그림 위에 둘지는 최종 구현 때 정할 것.
- 목업 `mock-poses.mp4`(17.6초: 주술사와 감독관, 보통 속도 2회 + 2배 느리게 1회), 세 순간 `mock-steps.png`. Round 27의 장면(게임의 조명, 게임식 그리기, 감독관 150%, 적 1열 명패 없음).

## 판정할 것

1. 주술사의 공격 자세가 같은 주술사로 보이는가, 지팡이로 내리치는 자세와 표정이 맞는가, 크기가 대기와 같은가.
2. 새 무기 "등불 지팡이"(이름, 저주의 침의 수치 그대로, 저주의 침을 데이터에서 뺌).
3. 그리는 순서: 공격하는 유닛을 맨 앞에(양쪽), 명패가 덮일 때.

## 판정 (2026-10-05, 사용자)

**"1.2.확인 / 3. 잠깐 덮이는 거는 괜찮은듯 / 구현 시작"** — 주술사의 공격 자세 확정, 새 무기(등불 지팡이, 저주의 침의 수치 그대로, 저주의 침 삭제) 확정, 공격하는 그림이 명패를 잠깐 덮는 것은 괜찮다. 아래 순서로 구현을 시작했다.

## 구현 1 — 기획과 데이터 (2026-10-05)

- 기획 먼저: Design/02 §4 【확정】(주술사는 등불 지팡이, 수치 그대로, 저주의 침 삭제), Design/10 §5(공격 자세 확정, 명패가 잠깐 덮이는 것은 괜찮다), Design/08에 메모, Architecture/05의 예시 줄.
- 데이터: `ItemData.csv`의 `hex_spit` 줄을 `lantern_staff,등불 지팡이,Lantern Staff,Weapon,1,3800,back:3,Burn,EnemyFront,1,30,,,,,0,`로(Icon은 아이콘 승인 뒤), `EnemyData.csv` 주술사 `lantern_staff:10+mending_chant:12`, 생성 JSON(`Tools/Sim transform`), `ShippedDataTests`(등불 지팡이는 무기 장비).
- 시뮬(바꾸기 전과 뒤, 같은 명령): 원정 `--dungeon abandoned_mine --runs 200 --seed 7`을 파티 둘(기사·마검사·주교·대마법사 / 성기사·발키리·주교·대마법사)로 — **출력이 바이트까지 같다**.
  전투 기록 `trace`(시드 7, `raider_warband`·`overseer_guard`)는 아이템 이름만 다르고 나머지가 같다(등불 지팡이가 8·14회 발동). 적에게는 무기 패시브가 없어 분류가 바뀌어도 전투는 같다.
- 저주의 침의 아이콘은 `Assets/@Art/Item/`에서 빼고(아무도 가리키지 않으면 검사가 잡는다) `removed/hex_spit.png`에 두었다. `Rosters/item.csv`의 저주의 침 줄 → 등불 지팡이 줄(Reference = 주술사의 확정 그림).
- 체인(`Tools/chain.sh`): setup OK(Addressables에서 `item/hex-spit`이 빠지고, 글꼴 아틀라스를 다시 만들어 '침'이 빠짐: 368 → 367자), sim OK, EditMode 625/625, PlayMode 50/51 →
  `UiFlowTests.Battle_WhenARowEmpties_TheCardsBehindMoveForward_AndTheDeadAreNamed`가 아이콘 없는 아이템(등불 지팡이)의 아이콘 색을 `Single(image.sprite == Icon)`으로 찾다가 여럿에 걸렸다(주석은 아이콘 없는 경우를 다룬다고 했다).
  아이콘이 있을 때만 아이콘 색을 보게 고친 뒤 `chain.sh playmode` 51/51(`[Explicit]` 스크린샷 7개 제외).

## 구현 2 — 아이콘 (승인, 연결됨)

- `gen_image.py --type item --key lantern_staff`(호출 1회, $0.015): 주술사의 확정 그림을 붙여 그 지팡이를 1칸 띠(1536x512 → 328x100, 폭의 86%)로. 눕힌 굽은 지팡이, 꼭대기 근처의 천 조각, 갈고리 끝의 녹슨 양철 등불.
- 리뷰 시트 `review-icon.png`(실제 칸에 넣어 화면 크기·2배, 다른 적 아이템 셋과 함께).
- 판정(2026-10-05, 사용자 "권장안 반영"): 승인. `Assets/@Art/Item/lantern_staff.png`, `ItemData.csv`의 `Icon` = `item/lantern-staff`, 변환. 확정 원본은 `approved/item/lantern_staff.raw.png`.

## 게임 연결 설계안 — 몬스터의 공격·피격 자세 (승인: "권장안 반영", 결정은 A)

용병의 설계안(Round 23, 승인됨)을 몬스터에 그대로 넓힌다. 화면의 움직임 코드는 이미 편과 상관없이 자세를 보인다(`BattlePresenter`가 무기의 돌진에 공격 자세, 밀림에 피격 자세를 부르고, 번쩍임은 피격 자세가 있으면 절반, `FigureView`는 자세에도 보스의 배율을 쓴다). 그래서 바뀌는 것은 그림을 넘기는 곳과 그리는 순서다.

### 1. 언제 어느 자세 (용병과 같다)

- 공격 자세: 무기 장비가 발동해 돌진하는 동안 + 0.05초. 몬스터의 무기 장비: 녹슨 칼, 조잡한 활, 물어뜯기, 감독관의 망치·포효, 이제 주술사의 등불 지팡이. 치유 주문(지원)은 맥동 그대로.
- 피격 자세: 유닛의 공격에 맞아 밀리는 동안 + 0.05초, 붉은 번쩍임은 절반. 화상·폭풍은 밀림이 없어 자세도 없다.
- 보스(150%)는 자세도 150%(바닥선에서). 쓰러지면 지금처럼 잔상(몬스터는 무덤 없음).

### 2. 그림 파일과 주소 (데이터 열 없음)

- 주소는 용병처럼 그림(`Figure`)에서: `unit/enemy/goblin-raider` → `pose/enemy/goblin-raider-attack`, `-hit`(`ArtAddress.PoseOf` 그대로, `EnemyData.AttackPose`·`HitPose`를 `JobData`처럼).
- 파일 `Assets/@Art/Pose/Enemy/<id>_attack.png`, `<id>_hit.png` 열 장 = 확정한 넓은 캔버스(2016x1008) 그대로: 약탈자는 Round 26, 쥐·궁수·감독관은 Round 27, 주술사의 공격은 Round 28·피격은 Round 27.
- Import는 자세의 정책(MaxSize 2048, 용병과 같은 선명도). 열 장 약 27MB(밉맵 포함). Addressables는 F1-Art·Expedition, `ExpeditionArt`가 함께 읽는다. 그림이 있는 적은 두 자세가 다 있어야 한다(`ArtSetup.FindProblems`).

### 3. 화면

- `BattleScreen`이 적도 `Bind`에 두 자세를 넘긴다(`ExpeditionArt.AttackPoseOfEnemy`·`HitPoseOfEnemy`). `BattlePresenter`·`BattleUnitView`·`FigureView`는 그대로.
- **결정 — 그리는 순서.** 지금 게임은 열을 맨 뒤 열부터 1열까지, 같은 열에서는 파티 다음에 적을 만든다(`UiPrefabSetup.Battle`): 적 1열이 파티 1열 위, 같은 편에서는 앞 열이 뒤 열 위.
  그래서 감독관의 망치는 이미 용병 앞에 보이지만, **뒤 열에서 공격하는 유닛의 무기는 앞 열 유닛에게 가려진다**(적 2열 주술사의 지팡이가 1열 약탈자 뒤로, 용병 3·4열 주교·대마법사의 지팡이가 1·2열 뒤로).
  - **A (권장)**: 공격하는 동안(돌진 + 0.05초) 그 유닛의 열을 맨 앞에 그리고 끝나면 원래 순서로. 양쪽, 모든 열. `BattleScreen`이 열의 순서를 정한다(기본 순서 + 공격 중인 열을 마지막에).
  - B: 지금 순서 그대로(감독관의 망치는 이미 앞). 뒤 열의 무기는 가려진 채.
- 명패: 공격하는 그림이 상대의 명패를 잠깐 덮는 것은 괜찮다(사용자 판정). 명패를 따로 올리지 않는다.

### 4. 파이프라인 (다음 몬스터도 같은 규칙으로)

- 스타일 문서에 몬스터의 자세 절 둘(Round 27 시험 문서의 §5·§6과 금지 추가분: 왼쪽, 눈동자 없는 노란 눈, 발·짐승의 발·부츠, 날아가는 화살 금지, 장비 그대로).
- `gen_image.py`에 타입 `enemy_attack`·`enemy_hit`(데이터 `EnemyData`, 기준 그림은 눈동자를 지운 확정 원본을 공격은 오른쪽 3분의 1·피격은 가운데), 소재 `Rosters/enemy_attack.csv`·`enemy_hit.csv`(정체 목록 + 자세 + 잰 배율 `Scale`).
- `tools/fit_pose.py`에 몬스터(맨 오른쪽 발과 발의 색, 발 안 잉크선 잇기, 배율은 소재의 `Scale`: 몬스터마다 잰 부위가 달라 Round 26~28에서 잰 값을 적는다), `tools/review_pose.py`에 몬스터. 열 장은 다시 그리지 않고 옮긴다(호출 없음). 도구로 다시 맞춘 것이 시험의 것과 같은지 본다.

### 5. 확인

- EditMode: 적의 자세 주소, `ArtSetup.FindProblems`(적의 자세 파일과 Import 정책), `AddressablesSetup.FindProblems`.
- PlayMode: 적이 무기로 돌진하는 동안 공격 자세·끝나면 대기 그림, 맞아 밀리는 동안 피격 자세와 절반 번쩍임, 보스의 자세가 150%, (A면) 공격하는 동안 그 열이 맨 앞이고 끝나면 원래 순서.
- 스크린샷: 적의 공격하는 순간과 맞는 순간(감독관 포함). 체인. 문서: Design/10 §5, Architecture/01·04·05·12·13.

## 구현 3 — 몬스터 자세의 게임 연결 (2026-10-05, 사용자 "권장안 반영")

- 데이터·주소: `EnemyData.AttackPose`·`HitPose`(`ArtAddress.PoseOf`, `Figure`에서. 열 없음), `ExpeditionArt`(적의 두 자세를 함께 읽음, `AttackPoseOfEnemy`·`HitPoseOfEnemy`), `ArtSetup`(적의 자세 Entry, 자세의 Import 정책).
- 화면: `BattleScreen.CreateUnit`이 적에게도 두 자세를 `Bind`(움직임 코드는 그대로: 무기의 돌진에 공격 자세, 밀림에 피격 자세와 절반 번쩍임, 보스는 자세도 150%).
  **공격하는 유닛을 맨 앞에(A)**: `BattleUnitView.Attacking`(돌진과 0.05초 더. 무기 장비만이 아니라 돌진하는 모든 아이템), `BattleScreen.ArrangeColumns`(매 Frame: Field의 원래 순서에 공격 중인 유닛의 Column을 마지막으로. 쓰러진 유닛은 세지 않는다).
- 그림: `Assets/@Art/Pose/Enemy/<id>_attack.png`, `_hit.png` 열 장 — 정식 도구로 다시 맞춘 것이 Round 26~28의 확정 후보와 픽셀까지 같다.
- 파이프라인: 스타일 문서 §27·§28과 Generation Types의 두 행, `gen_image.py`의 타입 `enemy_attack`·`enemy_hit`(기준 그림은 공격이 오른쪽 3분의 1: Round 26·27의 기준 그림과 픽셀까지 같음, 던전 컨셉은 붙이지 않음),
  `Rosters/enemy_attack.csv`·`enemy_hit.csv`(시험의 소재 + 잰 배율 `Scale` 소수 여섯째 자리), `Rosters/enemy.csv`에 `Feet`(감독관 `76 52 44`), `tools/fit_pose.py`의 몬스터(맨 오른쪽 발, 발의 색, 잉크선 잇기, 소재의 배율), `tools/review_pose.py --enemies`.
  원본은 `output/enemy_attack/`, `output/enemy_hit/`에(저장소 밖. 기록은 각 라운드의 `candidates/`).
- Test: EditMode `DefinitionTests.Poses_…`(적의 자세 주소), `ArtSetupTests`(적의 자세 경로, 그림마다 두 자세). PlayMode `Battle_AnEnemyShowsItsAttackPoseWhileItsWeaponLunges_AndItsHitPoseWhileItRecoils`(새),
  `Battle_AnAttackerIsDrawnInFrontOfEveryone_WhileItAttacks`(새), 용병의 자세 Test에서 "적은 자세가 없다"를 뺌. `UiTestUtil.EnterTheBossBattle`(보스전 시작에서 멈춤. `ReachAnEnemyAdvanceInTheBossBattle`이 그것을 부른다).
  스크린샷 `MonsterPoses_Korean`: `ko_26_battle_boss_attack_pose`(150% 감독관의 망치가 용병 앞), `ko_27_battle_back_row_attack`(3열 주술사의 지팡이가 2열 약탈자 앞), `ko_28_battle_enemy_hit_pose`. `game/`.
- 체인(`Tools/chain.sh`): setup OK(자세 열 장과 아이콘 Import, Addressables), sim OK, EditMode 626/626, PlayMode 53/53(`[Explicit]` 스크린샷 8개 제외). `Tools/screenshots.sh` 44장(8/8). `git diff --check` 깨끗.

## 최종 구현 때 (사용자 "최종 구현 요청" 뒤) — 위 구현 1~3으로 했다

1. 기획: Design/02 §4(분류), 07(ItemData), 03의 적 구성(주술사의 아이템), 10 §5(그리는 순서, 몬스터 자세의 연결).
2. 데이터: `ItemData.csv`에 `lantern_staff`, `EnemyData.csv`의 주술사, (권장) `hex_spit` 삭제, 생성 JSON, `ShippedDataTests`. 바꾸기 전후 시뮬(같은 시드).
3. 아이콘: `Rosters/item.csv`에 `lantern_staff`(Reference = 주술사의 확정 그림)로 생성(호출 1회) → 리뷰 → 승인 뒤 `Assets/@Art/Item/lantern_staff.png`와 `Icon`.
4. 몬스터 자세의 게임 연결(설계안을 먼저): 파이프라인 타입, 주소 `pose/enemy/<id>-attack|hit`, 적의 돌진·밀림의 자세, 공격하는 유닛을 맨 앞에, 보스 150%. 체인과 스크린샷.

## 호출

- 공격 자세 1회(약 $0.031), 아이콘 1회(약 $0.015). 누계 204회, 약 $5.20(상한 $10). 장부 `../calls.csv`.

## MP4 목업 (2026-10-05)

이 라운드의 MP4 목업은 사용자 지시("목업 mp4는 모두 삭제 해줘")로 지웠다. 휴지통의 `F1-mockup-mp4-20261005/28-shaman-weapon/`에 옮겨 두었다. 다시 보려면 `mock_shaman_overseer.py`로 만든다(호출 없음). Git에는 올린 적이 없고, 앞으로 만드는 MP4도 올리지 않는다(`.gitignore`).
