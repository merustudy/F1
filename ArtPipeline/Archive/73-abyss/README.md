# Round 73 — 심연의 군주와 호위, 심연의 배경과 아이템 (Slice C 28단계)

27~29단계의 일괄 승인("네 권장안에 따라 … 필요시 이미지 api 사용", Roadmap "27~29단계의 일괄 승인") 아래, 28단계가 최종 던전의 그림을 그린다.
판정은 권장안으로 하고 여기에 적는다(사후 검토). 상한: **호출 12회**. 누계는 `Archive/calls.csv`.

## 지시(범위)

- Design/02 §9(Abyss Lord, 스켈레톤 호위, 3 페이즈), 03 §1(최종 던전 심연), 10 §3(던전마다 컨셉)·§6(다른 던전의 컨셉은 그 던전을 만들 때 정함 — 여기서 권장안으로).
- 그릴 것: 몬스터 둘(`abyss_lord`, `skeleton_guard`: 전신 + 공격·피격 자세), 배경 하나(`abyss`), 아이템 아이콘 셋(`abyss_cleaver`, `abyss_howl`, `abyss_grasp`).

## 컨셉 (권장안)

**심연**: 죽은 숲 아래의 검은 구덩이. 시체를 버리던 곳이고 아래의 무엇이 그들을 깨웠다. 몬스터는 뼈처럼 희고 재처럼 잿빛이며 무덤 흙을 뒤집어썼다. 눈은 **흐린 병든 연두** 한 가지(광산의 등불 노랑과 가른다). 옛 무덤의 장비 하나(녹슨 철 두개골 투구, 갈라진 둥근 방패, 해진 수의, 철 목걸이). 군주는 뿔 달린 철 왕관과 썩은 망토. 강조색: 뼈 흰색, 재 회색, 녹 갈색, 병든 연두.
장소: 검은 돌의 넓은 동굴 바닥, 뒷벽의 뼈 더미, 위에서 늘어진 녹슨 쇠사슬과 검은 뿌리, 바닥 틈의 연두 빛. 문구는 `STYLE_RUNTIME.md`의 `## Dungeon: abyss`.

## 소재

- `Rosters/enemy.csv`: `skeleton_guard`(키 72, 녹슨 칼과 갈라진 둥근 방패), `abyss_lord`(키 97, 뿔 왕관·썩은 망토·거대한 식칼). `Rosters/background.csv`: `abyss`(바닥선 57). `Rosters/item.csv`: 식칼(기준 그림은 군주의 확정 그림), 울부짖음, 손아귀.
- 자세의 소재는 확정 그림을 보고 적는다(`Rosters/enemy_attack.csv`·`enemy_hit.csv`, 배율 `Scale`은 눈 사이의 거리로 잰다 — 아래).

## 진행 (2026-10-16)

호출 **11회** / 상한 12 (`Archive/calls.csv` 22:27~22:38, 약 $0.33). 판정은 모두 권장안이다(일괄 승인 — 사후 검토 대상).

| 종류 | Key | 호출 | 판정 | 비고 |
|---|---|---|---|---|
| enemy | `skeleton_guard` | 1 | 승인 | 큰 두개골·리벳 띠 투구·해진 잿빛 수의·녹슨 칼·갈라진 둥근 방패, 연두 눈. 키 653px(목표 645) |
| enemy | `abyss_lord` | 2 | 2차 승인 | 1차(`rejected/abyss_lord_v1`, Round 71의 옆 시점 후보처럼 Git에 올리지 않음)는 식칼을 옆으로 치켜든 넓은 자세라 폭에 걸려 616px(목표 869)로 줄었다 — 호위(645px)보다 작아진다. 소재를 "좁고 곧게 선, 식칼을 가슴 앞에 세워 든" 자세로 고쳐 다시 그렸다: 869px(목표와 같음) |
| background | `abyss` | 1 | 승인 | 검은 돌 동굴, 뒷벽의 뼈 더미·비석·쇠사슬·찢긴 깃발, 연두 불꽃 횃불, 바닥 틈의 연두 빛. 발 높이 57% = 바닥선(자르지 않음) |
| item | `abyss_howl`, `abyss_grasp`, `abyss_cleaver` | 3 | 승인 | 식칼은 확정 군주 그림을 기준 그림으로. 다듬기(`trim_items.py`) 뒤 156x100·200x100·328x88 |
| enemy_attack | `skeleton_guard`, `abyss_lord` | 2 | 승인 | 왼쪽으로 런지, 칼·식칼을 앞발 앞 바닥 아래까지 내리침 |
| enemy_hit | `skeleton_guard`, `abyss_lord` | 2 | 승인 | 뒤로 밀리며 앞발이 들림, 한 눈을 감음. 무기는 가슴 앞에 |

리뷰 시트: `review-enemy.png`(전투 크기와 원본), `review-background.png`, `review-item.png`(칸 안), `review-pose.png`(대기·공격·피격을 한 배율로).

**자세의 배율(`Scale`)**: 자세가 바뀌어도 크기가 그대로인 부위로 쟀다(`measure_scale.py`, 확정 원본 대 자세 원본).
- 머리에 두른 고리는 머리가 돌아도 폭이 지름이다: 호위의 투구(돔 137/146, 띠 210/221)와 칼의 길이(501/548) → 공격 **0.938**. 피격은 머리가 젖혀져 투구가 쪼개져 잡히지 않아 리벳(45/37.5 = 1.20)·방패의 세로(271/226 = 1.20)·가로(241/226 = 1.07)와 시트의 머리 크기로 **1.15**.
- 군주는 목걸이의 폭(141 대 144·145) → 공격 **0.979**, 피격 **0.972**. 자세의 머리는 확정 그림보다 크게 그려졌다(왕관 폭 1.45배·1.27배): 몸(목걸이·발·갈비)에 맞추고 머리는 표정의 과장으로 두었다.
- 눈의 면적은 표정(크게 뜸·감음)과 가림(군주의 먼 눈)에 흔들려 쓰지 않았다.

**배선**: `Assets/@Art/Unit/Enemy/{skeleton_guard,abyss_lord}.png`, `Assets/@Art/Pose/Enemy/{skeleton_guard,abyss_lord}_{attack,hit}.png`, `Assets/@Art/Background/Dungeon/abyss.png`, `Assets/@Art/Item/abyss_{cleaver,howl,grasp}.png`; `EnemyData.csv`의 `Figure`(`unit/enemy/skeleton-guard`·`unit/enemy/abyss-lord`, 군주 `FigureScale` 150), `DungeonData.csv`의 `Background`(`background/dungeon/abyss`), `ItemData.csv`의 `Icon`(`item/abyss-*`). 자세의 주소는 `Figure`에서 따라 나온다.

**도구의 메모(사후 검토)**: `trim_items.py`가 `gladius_glow.png`(유니크의 빛, `glow_items.py`의 산출물)까지 잘라 356x120 → 348x112로 바꿨다 — 되돌리고(`git checkout`) 도구가 `*_glow.png`를 건너뛰게 고쳤다. `run_roster.py`는 자세(`enemy_attack`·`enemy_hit`)를 "실패"로 센다 — 맞춘 파일은 `fit_pose.py`가 따로 만들기 때문이다. 호출과 원본 저장은 됐다. 자세의 Roster에 `Scale`은 생성 전에 1.0으로 두고 생성 뒤에 재서 적는다.
