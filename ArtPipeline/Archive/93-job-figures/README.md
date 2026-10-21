# Round 93 — 47단계 직업군 확대의 그림: 도적·궁수·흑마법사·악사의 대표 그림, 저주의 서·낡은 류트의 아이콘 (2026-10-21~22) — 생성, 판정 일곱 번, 연결 완료

지시: 2026-10-21 사용자 **"43단계 맞추기의 재조정 및 구현 후 새 직업 넷의 그림과 아이콘 둘 이미지 생성 수행(직업 넷 이미지는 대표 이미지만 우선 생성)"**.
47단계(Design/20 판정 12·13: 그림은 Round 93, 상한을 올린 뒤 요청 때)의 라운드다. **대표 그림(대기 자세)만** 그렸고 공격·피격·붕괴·각성 자세는 대표 그림이 확정된 뒤 한 라운드 더 돈다(Architecture/13 "승인 라운드" 9). 연결(배선)은 승인 뒤에만 한다.

## 범위와 상한

- 직업 넷(`character` 타입, 1024x1024 → 672x896 맞춤)과 아이템 둘(`item` 타입, 3×1 띠 1536x512 → 328x100).
- 호출 상한 **6**(항목당 한 번). 라운드 전의 장부 누계 369회 약 $9.85(Roadmap의 총 상한 $10). 생성 6회 약 **$0.17** → 누계 **375회 약 $10.02** — **총 상한 $10을 약 $0.02 넘겼다**(사용자의 생성 지시로. 새 상한은 사용자가 정한다; 재생성·자세 라운드는 그 뒤).
- 기준 그림: 타입의 것(`References/Character/style_ref_roster.png`, 아이템도 같은 시트). 체형은 저장한 것(도적 `athletic`, 악사 `hourglass`), 남성 둘은 소재가 체형을 적는다.
- 실행: `zsh ArtPipeline/Archive/93-job-figures/generate.sh`(`generate.log`). 있는 산출물은 건너뛴다.

## 소재

`ArtPipeline/Rosters/character.csv`에 더한 네 행과 `item.csv`에 더한 두 행. 요지(원문은 Roster):

| Key | 이름 | 타입·크기 | Gender·Body·Height | Subject (요지) |
|---|---|---|---|---|
| `rogue` | 도적 | character | 여·athletic·86 | 짧고 거친 검은 머리, 뒤로 젖힌 회색 두건, 가죽 조끼와 반 망토, 주머니 벨트, 단검(짧은 강철 날·작은 날밑·가죽 손잡이)을 낮게 거꾸로 쥐고 왼손을 편 웅크린 준비 자세; 날카롭고 경계하는 눈, 다문 입 |
| `archer` | 궁수 | character | 남·(소재: 마르고 팔다리가 긴 4.5등신)·88 | 짧은 갈색 머리와 짧은 수염, 두건 내린 짙은 녹색 저킨, 가슴끈·팔보호대, 회색 바지, 등의 화살통, 장궁을 세워 반쯤 당기고 화살을 먹인 채 오른쪽을 겨눔; 차분하고 가늘게 뜬 눈, 굳게 다문 입 |
| `occultist` | 흑마법사 | character | 남·(소재: 키 크고 수척한 5등신)·92 | 턱을 지나는 검은 생머리, 창백한 피부와 눈 밑 그늘, 높은 깃의 너덜한 보라-검정 로브, 뼈 부적 목걸이, 펼친 저주의 서(보라빛 연기)를 왼손에, 구부린 오른손을 적에게; 움푹한 차가운 눈, 긴 매부리코, 굳은 입 |
| `minstrel` | 악사 | character | 여·hourglass·88 | 긴 적갈색 곱슬머리, 깃털 하나 꽂은 녹색 모자, 크림색 블라우스 위 녹색 더블릿, 넓은 벨트와 띠, 무릎길이 적갈색 치마, 류트를 앞에 안고 왼손은 목·오른손은 현, 한 발 앞으로; 크고 또렷한 눈, 주근깨, 굳게 다문 입 |
| `cursed_tome` | 저주의 서 | item 3×1 | — | 보라-검정 가죽 장정의 두꺼운 책, 철 모서리 장식, 뼈색 눈 문장, 등을 왼쪽으로 눕힘, 책장 가장자리에 옅은 보라 |
| `lute` | 낡은 류트 | item 3×1 | — | 배 모양 나무 울림통, 둥근 울림구멍, 줄감개 달린 짧은 목, 줄 넷, 긁힌 자국, 목을 왼쪽으로 눕힘 |

## 생성 (2026-10-21 18:40~18:42)

- `generate.sh` → 6회, 실패 0. 직업 넷 1024x1024 약 $0.032씩, 아이템 둘 1536x512 약 $0.021씩.
- 맞춤(`figure`): 궁수 788/788px·악사 788/788px은 목표 키 그대로. **도적 660/771px, 흑마법사 758/824px은 폭이 캔버스를 넘어 폭에 맞춰 줄어 키가 목표보다 작다**(웅크린 넓은 자세와 뻗은 팔). 화면에서는 각각 마검사(같은 웅크림)·대마법사만 하다(리뷰 시트의 첫 줄).
- 아이템: 저주의 서는 3×1 조각의 폭 45%(책은 정사각에 가까워 띠를 다 채우지 못함), 류트는 75%.
- 산출물: 원본 `ArtPipeline/output/<type>/<key>.raw.png`, 맞춘 그림 `<key>.png`(gitignore). 맞춘 후보를 `candidates/`에 복사했다(6).
- 리뷰 시트: `review-figures.png`(지금 게임의 다섯과 후보 넷을 화면 크기로 한 줄에, 기준 시트와 후보 원본, 머리 줄에 얼굴 띠), `review-items.png`(아이콘 둘을 3×1 조각에 1배·2배, 지금의 3×1 기본 무기·지팡이 옆에). `review_sheet.py`가 R18(2026-10-20)로 지워진 `UiPalette.Party`·`Enemy`를 읽지 못해 `Button`·`Danger`로 대신하게 고쳤다(호출 없음).

## 후보 — 읽은 것과 권장

| Key | 읽은 것 | 권장 |
|---|---|---|
| `rogue` (1차) | 소재대로이나 **단검이 적의 반대쪽을 향하고 손이 손잡이를 감싸지 않음**(사용자 "어색해"). 넓은 자세라 키가 목표의 86% | 재생성 → 2차 `rogue_v2` **확정 권장** (아래 "판정 1차") |
| `archer` (1차) | 소재대로이나 **활을 당겨 겨눈 조준 자세**라 공격 모션으로 읽힘(사용자). 백업 `archer_v1` | 재생성 → 2차 `archer_v2` **확정 권장** (아래 "판정 1차") |
| `occultist` | 소재대로(수척한 긴 체형·늘어진 검은 머리·보라-검정 로브·뼈 부적·펼친 책의 보라 연기·구부린 손), 그림자 진 눈. 뻗은 팔로 키가 목표의 92% | **확정** |
| `minstrel` | 소재대로(깃털 모자·적갈색 곱슬머리·녹색 더블릿·류트를 치는 손·앞으로 디딘 발), 진지한 표정, 주근깨. 키 목표 그대로 | **확정** |
| `cursed_tome` | 눈 문장의 두꺼운 보라 책, 철 모서리, 책장의 옅은 보라. 조각의 폭 45%라 1배에서 작게 보인다 | **확정**. 더 크게 보이려면 재생성(펼친 책을 길게 눕혀) — 선택 |
| `lute` | 배 모양 울림통·줄감개·줄 넷·긁힌 자국, 띠를 75% 채움 | **확정** |

## 판정 1차 (2026-10-21 사용자 "도적 칼 들고 있는 부분이 어색해. 확인 후 재생성 / 궁수 활 들고 있는 모습이 공격 모션 같아.(일단 백업) 공격 준비 자세로 재생성")

- 확인: 도적 1차는 단검이 **적의 반대쪽(왼쪽 아래)**을 향한 채 주먹이 손잡이를 감싸지 않아 어색했다(소재의 "거꾸로 쥔" 단검을 모델이 거꾸로 돌렸다). 궁수 1차는 활을 반쯤 당기고 화살을 겨눈 **조준 자세**였다(소재가 그렇게 적었다) — 공격 자세 라운드의 모습이지 대기 자세가 아니다.
- 백업: 1차 둘은 `candidates/rogue_v1.png`·`archer_v1.png`(원본 `*_v1.raw.png`)로 보관했다. 궁수 1차는 뒤의 공격 자세 라운드에서 방향의 참고로 쓸 수 있다(그 라운드는 확정 대기 그림을 붙여 새로 그린다).
- 소재를 고쳤다(`Rosters/character.csv`의 두 행, 나머지 문구는 그대로): 도적 — "a firm forward grip in her right hand, her fist closed around the grip with the crossguard just above it and the blade pointing forward and a little up toward her enemy on the right at chest height … light-footed ready stance with the knees slightly bent"; 궁수 — "holding his wooden longbow upright at his left side … with the string slack and straight, his right hand resting a single arrow against the string without drawing it … braced, alert ready stance".
- 재생성 한 번씩(`--name rogue_v2`·`archer_v2`, `--extra`로 손잡이를 감싼 주먹과 오른쪽을 향한 날 / 당기지 않은 활을 강조): 2회 약 $0.064 → 장부 누계 **377회 약 $10.08**.
- 2차 후보: `candidates/rogue_v2.png`(키 771/771 — 자세를 좁혀 목표 키 그대로. 단검을 바로 쥐고 날이 오른쪽을 향한다), `candidates/archer_v2.png`(788/788 — 활을 세워 들고 줄은 늘어진 채, 화살은 줄에 대고만 있다). 리뷰 시트 `review-figures-v2.png`(얼굴 띠 안).
- 권장: 둘 다 **확정**(2차). 흑마법사·악사·아이콘 둘은 1차 그대로 확정 권장.

## 판정 2차 (2026-10-21 사용자 "자세는 마음에 드는데 얼굴은 이전 얼굴이 마음에 들어. 이전 얼굴을 유지한 상태에서 지금 자세로 재생성" → "도적, 궁수에 대해서 진행")

- 길: 얼굴을 지키며 자세만 바꾸는 것은 **자세 타입**의 일이다(§25·§26: 붙인 확정 그림의 같은 인물, 자세·표정만). `--type hit --roster Archive/93-job-figures/ready-pose.csv --name <key>_v3`로 1차 원본(`output/character/<key>.raw.png` = v1)을 자세 캔버스에 놓은 기준 그림을 붙이고, 소재는 "the very same rogue/archer as the attached image: the same face …, now standing in [2차의 준비 자세]"로 적었다(`ready-pose.csv`, Body는 character 행과 같게). 나온 원본(1536x1024)을 `output/character/<key>_v3.raw.png`로 옮겨 `--type character --refit`으로 전신 그림(672x896)에 맞췄다(호출 없음).
- 호출 2회 약 $0.058 → 장부 누계 **379회 약 $10.14**.
- 3차 후보: `candidates/rogue_v3.png` — 1차의 얼굴·머리·옷·귀걸이 그대로, 단검을 바로 쥐고 날이 오른쪽 가슴 높이를 향한다. 자세가 다시 넓어 키는 목표의 85%(657/771 — 1차와 같은 정도, 마검사만 하다). `candidates/archer_v3.png` — 1차의 얼굴(짙은 수염)·옷 그대로, 활을 세워 들고 줄은 늘어진 채 화살은 줄에 대고만 있다. 키 목표 그대로(788/788). 리뷰 시트 `review-figures-v3.png`(얼굴 띠 안).
- 권장: 둘 다 **확정**(3차). 도적의 키를 더 세우고 싶으면 같은 길로 "좁은 자세"를 한 번 더(선택).

## 판정 3차 (2026-10-21 사용자 "도적 단검을 케릭터 기준 오른손으로 들도록 재생성")

- 3차의 도적은 단검을 보는 이에 가까운 손(캐릭터의 왼손)에 들었다. `ready-pose.csv`의 도적 행에 "RIGHT hand — in this three-quarter view facing right that is the far hand … the right arm reaching forward past her body … her LEFT hand, the near hand closest to the viewer, is empty and raised open"을 적고 `--extra`로 오른손잡이임을 한 번 더 말해 같은 길(자세 타입 + 1차 원본의 기준 그림)로 1회(약 $0.030 → 장부 누계 **380회 약 $10.17**) → `rogue_v4`.
- 4차 후보 `candidates/rogue_v4.png`: 단검이 **캐릭터의 오른손**(먼 손)에 있고 오른팔이 몸 앞을 지나 날이 가슴 높이에서 적을 향한다. 왼손(가까운 손)은 비어 펼쳐져 있다. 얼굴·머리·옷은 1차 그대로. 자세가 넓어 키는 목표의 80%(619/771 — 화면에서 다른 직업보다 조금 작다). 리뷰 시트 `review-figures-v4.png`(얼굴 띠 안).
- 권장: **확정**(4차). 키가 작은 것이 마음에 걸리면 같은 길로 "다리를 좁히고 팔을 조금 접은" 자세를 한 번 더(선택, 약 $0.03).

## 판정 4차 (2026-10-21 사용자 "도적 단검 반대 손으로 쥐게 해줘. 재생성")

- 4차(먼 손)의 반대, 곧 **보는 이에게 가까운 손(앞쪽 팔)**으로. `ready-pose.csv`의 도적 행을 "the hand NEAREST the viewer — the arm in the foreground, in front of her body … the far one behind her body is empty and raised open"으로 고치고 `--extra`로 앞쪽 손임을 한 번 더 말해 같은 길로 1회(약 $0.030 → 장부 누계 **381회 약 $10.20**) → `rogue_v5`.
- 5차 후보 `candidates/rogue_v5.png`: 단검이 **앞쪽 손**에 있고 먼 손은 비어 있다. 얼굴·옷은 1차 그대로, 자세는 2차 이후의 준비 자세. 키는 목표의 88%(675/771). 리뷰 시트 `review-figures-v5.png`.
- 권장: **확정**(5차).

## 판정 5차 (2026-10-21 사용자 "도적 단검 뒤쪽 손으로 쥐게 해줘. 재생성")

- "뒤쪽 손"을 자세에서 뒤로 뺀 손(그림의 왼쪽)으로 읽었다: 앞으로 뻗은 손은 비우고 단검은 뒤로 뺀 손에, 날은 적을 향해. `ready-pose.csv`의 도적 행을 "her leading hand … empty and open, reaching forward, while the dagger is in her TRAILING hand, the one held back behind her body on the left side of the picture, that arm bent at her side … the blade pointing forward toward the enemy, ready to strike"으로 고치고 같은 길로 1회(약 $0.030 → 장부 누계 **382회 약 $10.23**) → `rogue_v6`.
- 6차 후보 `candidates/rogue_v6.png`: 뒤로 뺀 손에 단검, 앞 손은 비어 있다 — 그러나 **날이 적의 반대쪽(왼쪽)**을 향해 1차의 어색함에 가깝다. 소재의 "blade pointing forward toward the enemy"를 모델이 지키지 않았다.
- 같은 소재에 `--extra`로 "역수 그립: 날이 팔뚝을 따라 오른쪽(적)을 향한다, 왼쪽으로 향하지 않는다"를 더해 1회 더(약 $0.030 → 장부 누계 **383회 약 $10.26**) → 7차 `candidates/rogue_v7.png`: 뒤쪽 손에 쥔 채 날이 적을 향한다. 키는 목표의 95%(731/771). 리뷰 시트 `review-figures-v7.png`(6·7차 나란히).
- 권장: **7차 확정**(뒤쪽 손 + 날이 적을 향함). 6차는 날의 방향 때문에 권하지 않는다.

## 판정 6차 (2026-10-21 사용자 "현재 도적 이미지에서 조금 더 뒷손을 자연스럽게 재생성")

- 7차를 기준 그림으로 붙여(`--reference output/character/rogue_v7.png`: 자세 타입에서 `--reference`는 확정 원본 대신 그대로 붙는다) 자세는 그대로 두고 뒷팔·뒷손만 편하게(팔꿈치를 굽혀 몸 가까이, 손목은 곧게, 허리 옆에서 역수로 날이 팔뚝을 따라 적 쪽) 1회(약 $0.024 → 장부 누계 **384회 약 $10.29**) → 8차 `candidates/rogue_v8.png`. 리뷰 시트 `review-figures-v8.png`(7·8차 나란히).

## 판정 7차 (2026-10-22 사용자 "도적 전투 준비 이미지를 5차 때 이미지로 변경")

- 호출 없음. 도적의 확정 원본을 8차에서 **5차**(`candidates/rogue_v5` — 단검이 앞쪽 손에, 먼 손은 비어 있음, 얼굴·옷은 1차 그대로)로 바꿨다: `output/character/rogue.raw.png`·`rogue.png` = 5차. 8차는 `candidates/rogue_v8`에 남는다.
- Round 94의 자세 셋은 8차를 기준 그림으로 그렸으나 5차와 같은 인물·옷·단검이고, 공격 자세는 앞쪽(앞으로 뻗은) 손에 단검을 쥐어 5차의 손과 같다. 피격·각성의 단검은 몸 가까이 있어 어느 쪽과도 어긋나지 않는다 — 자세를 다시 그릴 필요는 없다(Round 94의 리뷰 시트 둘을 5차로 다시 만들었다).
- **도적의 대표 그림은 5차로 확정**(사용자 판정).

## 판정 (2026-10-22 사용자 "연결 및 구현" — 판정은 아래 번호의 뒤에, 실행은 "연결 실행")

1. 직업 넷: 확정 / 재생성(방향) / 소재 교체. 도적은 5차로 확정됨(판정 7차). 궁수(3차)·흑마법사·악사(1차)는 권장 확정.
2. 아이콘 둘: 확정 / 재생성. 권장 둘 다 확정(저주의 서는 선택).
3. 유료 호출의 새 상한(지금 누계 약 $10.02 / 옛 상한 $10). 재생성과 자세 라운드(넷 × 넷, 약 $0.5)는 상한을 올린 뒤.
4. (→ Round 94로 돌아 함께 연결했다) 연결 뒤 자세 라운드(공격·피격·붕괴·각성)를 돌릴지: 권장 "대표 그림 확정 뒤 한 번에"(Architecture/13 승인 라운드 9. 그 전에는 자세도 임시 그림의 것 — `JobData.Figure`가 가리키는 그림의 자세 넷이 따라오므로, 대표 그림만 새것이면 공격·피격 자세는 마검사·대마법사·주교의 것이 섞여 보인다 → 자세 라운드 전까지는 **대표 그림만 바꾸지 않고** 임시 그림을 그대로 두는 것도 선택이다).

## 연결 (승인 뒤의 계획)

- 직업: `ArtPipeline/output/character/<key>.png` → `Assets/@Art/Unit/Job/<key>.png`(+ `.meta`, Addressables `unit/job/<key>`), `JobData.csv`의 `Figure` → `unit/job/<key>`. 자세 넷(`Assets/@Art/Pose/Job/<key>_attack|_hit|_broken|_resolute.png`)은 자세 라운드 뒤 — 그 전에는 `JobData.AttackPose`가 `unit/job/<key>`에서 끌어낸 주소에 그림이 없으므로 **대표 그림과 자세를 같은 라운드에서 함께 연결**하거나(권장), 임시 그림을 유지한다.
- 아이템: `output/item/<key>.png` → `trim_items.py` → `Assets/@Art/Item/<key>.png`, `ItemData.csv`의 `Icon` → `item/<kebab>`, `ShippedDataTests`의 아이콘 예외 둘을 지움.
- 체인 한 번.

## 연결 실행 (2026-10-22 사용자 "연결 및 구현")

- 판정: 1 직업 넷 **확정**(도적 5차 `rogue_v5`, 궁수 3차 `archer_v3`, 흑마법사·악사 1차), 2 아이콘 둘 **확정**, 3 새 상한은 **$20**(2026-10-22 사용자 결정; 누계 398회 약 $10.71), 4 자세는 Round 94로 돌아 대표 그림과 함께 연결.
- 승인 원본: `approved/<key>.png`(맞춘 것)·`<key>.raw.png` — 도적은 5차, 궁수는 3차의 복사.
- 배선: `output/character/<key>.png` → `Assets/@Art/Unit/Job/<key>.png` 넷, `JobData.csv`의 `Figure` → `unit/job/<key>`(도적·궁수 `unit/job/spellblade`에서, 흑마법사 `unit/job/archmage`에서, 악사 `unit/job/bishop`에서). `output/item/<key>.png` → `Assets/@Art/Item/<key>.png` 둘(`trim_items.py`: 저주의 서·류트를 그린 상자로 자름), `ItemData.csv`의 `Icon` → `item/cursed-tome`·`item/lute`. Generated JSON(`transform`), `validate` OK(8 jobs, 58 items, 9 enemies). `ShippedDataTests`: 아이콘 예외 둘(`waitingForTheirRound`) 삭제, 직업 넷의 `Figure` 단언과 "마검사의 그림을 쓰는 직업 없음".
- 마검사(`spellblade`)의 그림 다섯(대표·공격·피격·붕괴·각성)은 이제 아무 직업도 가리키지 않아(`ArtSetup.FindProblems`의 "아무도 가리키지 않는 PNG") `Assets`에서 `.meta`와 함께 지우고 `removed-spellblade/`에 남겼다(36단계의 성기사와 같은 길). Addressables의 Entry 다섯은 체인의 setup(`AddressablesSetup.Sync`)이 지운다. `Rosters/character.csv`의 마검사 행은 그대로(참고 그림).
- 자세 열두 + 붕괴 자리의 피격 복사 넷은 Round 94의 README "연결 실행".
- 스크린샷 대조는 Round 94의 README "연결 실행"(`impl-expedition-party.png`: 대표 그림 넷과 썸네일, `impl-battle.png`: 보드의 저주의 서·류트 아이콘).
- 전체 체인 `20261022-063327` **CHAIN OK**: setup OK(새 PNG 22의 Import와 `.meta`, Addressables Entry +22/−5), sim OK(8 jobs, 58 items, 9 enemies), EditMode **1094/1094**, PlayMode **97/109**(건너뜀 12는 그래픽 스크린샷 Test). `Assets/InitTestScene*` 없음, `git diff --check` 깨끗.
