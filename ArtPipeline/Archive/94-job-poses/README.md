# Round 94 — 47단계 직업 넷의 자세: 공격·피격·각성 (2026-10-21~22) — 생성, 판정 1차(도적의 피격·각성 재생성), 연결 완료

지시: 2026-10-21 사용자 **"용병 사진에 대해 붕괴 사진은 피격 사진으로 대체하고 공격모션, 피격모션, 각성 모션 이미지 생성"** → "여기서 용병이란 이번에 새로 만든 4명의 용병사진이야. 도적인 최신 사진으로 수행".
Round 93의 대표 그림(도적 8차 `rogue_v8` — 2026-10-22 사용자 판정으로 대표 그림은 **5차** `rogue_v5`로 바뀜, 같은 인물·옷·단검이라 자세는 그대로 둔다; 궁수 3차 `archer_v3`, 흑마법사·악사 1차)을 확정 원본으로 삼아(`output/character/<key>.raw.png`에 둠) 자세 셋을 그린다. **붕괴 자세는 그리지 않고 피격 자세를 쓴다**(사용자 결정: 배선 때 `<Id>_broken.png`에 피격 그림을 복사). 연결은 승인 뒤에만 한다.

## 범위와 상한

- 직업 넷 × 자세 셋 = **12회**(공격 `attack`·피격 `hit`은 정식 타입, 각성은 Round 40의 길: `--type hit --name <key>_resolute --style Archive/38-breakdown-fx/STYLE_RUNTIME-statepose.md --roster Archive/94-job-poses/resolute.csv --tail "…같은 얼굴·머리 크기…"`). 1536x1024, 약 $0.03씩 → 약 $0.36.
- 라운드 전의 장부 누계 384회 약 $10.29(총 상한 $10은 Round 93에서 넘겼다 — 새 상한은 사용자가 정한다).
- 기준 그림: 그 직업의 확정 원본을 자세 캔버스(1536x1024)에 키 700으로 앉힌 것(`output/<type>/<key>.reference.png`: 공격은 왼쪽 3분의 1, 피격·각성은 가운데).
- 실행: `zsh ArtPipeline/Archive/94-job-poses/generate.sh`(`generate.log`). 있는 원본은 건너뛴다.

## 소재

`ArtPipeline/Rosters/attack.csv`·`hit.csv`에 더한 네 행(인물 묘사는 Round 93의 확정 그림에서 읽은 것, 자세 문장은 발키리·기사의 문법), 각성은 `resolute.csv`(Round 40의 문법: 제자리에 서서 가슴을 펴고 턱을 들며 무기를 어깨 옆에 세움, 따뜻한 색과 금빛 테).

| 직업 (Body) | 공격 | 피격 | 각성 |
|---|---|---|---|
| 도적 (athletic) | 오른쪽으로 돌진하며 단검을 가슴 높이로 곧게 찌름, 다른 손은 뒤로, 함성 | 오른쪽에서 맞아 뒤로 젖혀짐, 단검을 몸에 붙임, 한 눈 감은 찡그림 | 단검을 어깨 옆에 세움, 주먹 |
| 궁수 | 활을 뺨까지 당겨 쏘는 순간, 활은 머리보다 높지 않게 | 뒤로 젖혀짐, 활은 몸에 붙이고 화살은 오른손에 | 활을 어깨 옆에 세움, 주먹 |
| 흑마법사 | 펼친 책에서 보라 연기, 구부린 손을 적에게 뻗음, 으르렁 | 뒤로 젖혀짐, 책을 가슴에 붙이고 오른손은 위로 | 닫은 책을 가슴에 세워 들고 주먹을 어깨 옆에 |
| 악사 (hourglass) | 류트를 가슴 높이로 들고 현을 친 뒤 오른손을 활짝, 전투의 노래 | 뒤로 젖혀짐, 두 손으로 류트를 몸에 붙임 | 류트를 목을 잡아 어깨 옆에 세움, 주먹 |

## 생성 (2026-10-21 21:27~21:32)

- `generate.sh` → 12회, 실패 0, 약 $0.36(1536x1024 한 번에 약 $0.03). 장부 누계 **396회 약 $10.65**(총 상한 $10은 Round 93에서 넘김 — 2026-10-22 사용자가 새 상한을 $20으로 정했다).
- 원본 `output/attack/<key>.raw.png`, `output/hit/<key>.raw.png`, `output/hit/<key>_resolute.raw.png`(gitignore). 열두 장 모두 같은 인물·옷·무기로 자세와 표정만 바뀌었다. 복사본은 `candidates/`(맞춘 그림 열두 장과 원본 열두 장).

## 맞추기 (`fit_pose.py`, `40-state-poses/fit_state_poses.py`)

- 배율의 규칙(금 장식의 지름)은 도적에게만 맞았다(공격 0.956, 피격 0.904, 각성 0.927 → 각성은 머리 크기로 0.86). 궁수·흑마법사는 금 장식이 없어 Roster의 `Scale`로, 악사는 류트의 둥근 몸통과 울림구멍을 장식으로 잘못 재어(공격 1.52, 각성 1.24) `Scale`로 덮어썼다. 배율은 먼저 얼굴 상자의 비(머리의 피부 덩어리)로 어림한 뒤 한 배율의 리뷰 시트(`review_poses.py`: 대기·공격·피격·각성을 같은 배율로, 바닥선을 그림)에서 머리 크기를 맞대 보며 두 번 고쳤다.

  | | 도적 | 궁수 | 흑마법사 | 악사 |
  |---|---|---|---|---|
  | 공격 (`attack.csv` Scale) | 장식 0.956 | 1.06 | 1.04 | 1.10 |
  | 피격 (`hit.csv` Scale) | 장식 0.904 → 판정 1차의 재생성은 장식 0.969 | 1.07 | 0.95 | 1.00 |
  | 각성 (`fit_state_poses.py --scale`) | 0.86 → 판정 1차의 재생성은 장식 0.935 | 0.97 | 0.95 | 1.00 |

- 뒷발은 바닥선에 섰고(+3~+55px의 무기·발이 바닥선 아래로: 규칙 안), 색 맞춤의 ΔE는 1.0~3.1(악사의 각성만 12.8 "as drawn": 금빛 테와 따뜻한 색을 그대로 둠 — Round 40과 같다). 결과 `output/attack/<key>.png`, `output/hit/<key>.png`, `output/hit/<key>_resolute.png`(2048x1024).
- 리뷰 시트: `review-attack-hit.png`(`tools/review_pose.py`: 대기·공격·피격 셋을 맞대 봄), `review-poses.png`(한 배율의 넷째 열까지). 2026-10-22 도적의 대표 그림이 5차로 바뀌어 둘 다 다시 만들었다(대기 열만 바뀜).

## 판정 1차 (2026-10-22 사용자 "도적 피격 및 각성 이미지도 앞손에 칼을 든 상태로 이미지 재생성(현재 전투 준비 이미지 기반으로)")

- 대표 그림이 5차(단검이 앞쪽 손)로 바뀐 뒤의 지시. 1차의 피격·각성은 8차(뒤쪽 손)를 기준으로 그려 피격의 단검이 뒤쪽 손에, 각성의 단검이 뒤쪽 손에 있었다 → `candidates/rogue_hit_v1.*`·`rogue_resolute_v1.*`로 남기고 둘을 다시 그렸다(`regen-rogue.sh`, `regen-rogue.log`).
- 기준 그림은 `pose_reference`가 매번 `output/character/rogue.raw.png`(= 5차)에서 다시 만든다. 소재: `Rosters/hit.csv` 도적 행의 손 문장을 "the hand NEAREST the viewer — the arm in the foreground, in front of her body, the very same hand that holds it in the attached image — gripped and pulled in close to her chest … her other hand, the far one behind her body, is empty and flung open"으로, `resolute.csv` 도적 행을 "… that arm bent at the elbow to hold the dagger upright beside her shoulder … her other hand, the far one behind her body, a clenched fist"으로 고치고 `--extra`로 앞쪽 손임을 한 번 더 말했다.
- 2회 약 $0.06 → 장부 누계 **398회 약 $10.71**.
- 맞추기: 이번에는 금 장식의 규칙이 둘 다 맞았다 — 피격 **0.969**(장식 41·32 ↔ 39·37), 각성 **0.935**(36·35 ↔ 39·37). 한 배율의 시트(`review-rogue-hands.png`: 대기·전·후를 두 줄로)에서 머리 크기가 대기와 같아 그대로 뒀다(1차의 각성은 머리 크기로 0.86까지 줄였었다). ΔE 1.6·2.6, 바닥선 아래 +4·+4.
- 후보: 피격 — 뒤로 젖혀져 한 눈을 감고 이를 악문 채, 단검은 **앞쪽 손**에 쥐어 가슴 앞으로 당기고(날은 적 쪽) 먼 손은 펼쳐 뒤로 흔들린다. 각성 — 가슴을 펴고 서서 **앞쪽 손**의 팔꿈치를 굽혀 단검을 어깨 옆에 곧게 세웠고 먼 손은 허리 옆의 주먹, 금빛 테. 둘 다 5차와 같은 얼굴·옷·단검.
- 권장: 둘 다 **확정**(대기 5차·공격·피격·각성 넷 모두 앞쪽 손). 리뷰 시트 셋(`review-poses.png`, `review-attack-hit.png`, `review-rogue-hands.png`)을 다시 만들었다.

## 후보 — 읽은 것과 권장

| 직업 | 공격 | 피격 (= 붕괴) | 각성 | 권장 |
|---|---|---|---|---|
| 도적 | 오른쪽으로 깊게 돌진해 단검을 곧게 찌름, 함성. 같은 얼굴·옷, 단검은 앞쪽 손(5차 대표 그림과 같은 손) | (판정 1차에서 다시 그림) 뒤로 젖혀져 한 눈을 감고 이를 악묾, 앞쪽 손의 단검을 가슴 앞으로 당김 | (판정 1차에서 다시 그림) 가슴을 펴고 앞쪽 손의 단검을 어깨 옆에 세움, 금빛 테 | 셋 다 **확정** |
| 궁수 | 활을 뺨까지 당겨 쏘는 순간, 활이 머리보다 높지 않음 | 뒤로 젖혀짐, 활은 몸에·화살은 손에 | 활을 세워 들고 주먹, 금빛 테 | 셋 다 **확정** |
| 흑마법사 | 펼친 책의 보라 연기, 구부린 손을 적에게, 으르렁 | 뒤로 젖혀져 책을 가슴에, 손은 위로 | 닫은 책을 가슴에, 주먹을 어깨 옆에 | 셋 다 **확정** |
| 악사 | 류트를 든 채 현을 치고 오른손을 활짝, 노래하는 입 | 뒤로 젖혀져 두 손으로 류트를 껴안음 | 류트를 머리 위로 세워 들고 주먹 | 셋 다 **확정** |

- 머리 크기는 네 열이 같은 배율에서 맞는다(두 번의 조정 뒤). 무기·장식은 Round 40처럼 자세에서 조금 작게 그려졌다(1초 남짓 보이는 자세라 머리와 몸을 맞췄다).


## 판정 (2026-10-22 사용자 "연결 및 구현": 열두 장 확정 — 실행은 "연결 실행")

1. 열두 장: 확정 / 재생성(방향) / 소재 교체. 자세 그림의 점검(Architecture/13): 같은 인물인가, 머리가 대기와 같은 크기인가, 뒷발과 바닥선, 무기가 잘리지 않았는가, 표정.
2. 연결(승인 뒤): 대표 그림 넷(`Assets/@Art/Unit/Job/<Id>.png`, `JobData.Figure`), 자세 열두 장 + 붕괴 자리에 피격의 복사 넷(`Assets/@Art/Pose/Job/<Id>_attack|_hit|_broken|_resolute.png`), 아이콘 둘(`Assets/@Art/Item/`, `ItemData.Icon`, Test의 예외 삭제), 체인 한 번.

## 연결 실행 (2026-10-22 사용자 "연결 및 구현")

- 판정: 열두 장 **확정**(도적의 피격·각성은 판정 1차의 재생성분 — 앞손에 단검).
- 승인 원본: `approved/<job>_attack|_hit|_resolute.png`(맞춘 것)·`.raw.png` 열두 쌍. 거절(대체됨): `candidates/rogue_hit_v1.*`·`rogue_resolute_v1.*`(8차 기준).
- 배선: `output/attack/<job>.png` → `Assets/@Art/Pose/Job/<job>_attack.png`, `output/hit/<job>.png` → `<job>_hit.png` **그리고 `<job>_broken.png`**(사용자 결정: 붕괴 사진은 피격 사진으로 대체 — 같은 파일의 복사), `output/hit/<job>_resolute.png` → `<job>_resolute.png`. 열여섯 장 모두 2048x1024. `JobData`가 `Figure`(`unit/job/<job>`)에서 네 자세의 주소를 끌어내므로 CSV의 자세 열은 없다; 붕괴·각성의 Entry는 파일이 있어 `ArtSetup.Files`가 넣는다.
- 대표 그림·아이콘·Test·마검사 그림의 제거는 Round 93의 README "연결 실행".
- 스크린샷 대조(`Tools/screenshots.sh`의 fixture에 더한 캡처 `UiScreenshotTests.TheNewJobs_Korean` — 역마차의 후보를 넷으로 꾸며 고용하고 1~4열에 세운 뒤: `ko_82_expedition_party_new_jobs`(원정 파티 창: 도적·궁수·흑마법사·악사가 바닥선에 서고 머리 크기가 같다, 목록의 썸네일도 새 그림), `ko_83_battle_new_jobs`(1층 전투: 넷이 고블린과 같은 바닥선에, 보드에 류트·저주의 서·활·단검의 아이콘), `ko_84_battle_new_jobs_attack_pose`(도적의 공격 자세: 앞손의 단검을 곧게 찌름), `ko_85_battle_new_jobs_hit_pose`(궁수의 피격 자세). 전체 fixture `20261022-065736` 12/12(기존 캡처는 시작 로스터라 새 직업이 없다), 새 캡처만 `20261022-newjobs` 1/1. 사본 `Archive/94-job-poses/impl-*.png` 넷).
- 전체 체인 `20261022-063327` **CHAIN OK**: setup OK(새 PNG 22의 Import와 `.meta`, Addressables Entry +22/−5), sim OK(8 jobs, 58 items, 9 enemies), EditMode **1094/1094**, PlayMode **97/109**(건너뜀 12는 그래픽 스크린샷 Test). `Assets/InitTestScene*` 없음, `git diff --check` 깨끗.
