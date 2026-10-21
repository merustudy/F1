# Slice D 43단계: 맞추기와 직접 플레이 (1차)

Snapshot: 2026-10-21 (아침: 46단계 뒤의 자리로 판정 열 개 보고 → 47단계(직업군 확대)가 먼저 → 47 뒤 재측정 → 사용자 "43단계 맞추기의 재조정 및 구현 후 새 직업 넷의 그림과 아이콘 둘 이미지 생성 수행(직업 넷 이미지는 대표 이미지만 우선 생성)" → 레버 셋 구현·시뮬·체인, Round 93 생성 → 10-22 Round 93·94의 판정과 연결 → 세션 정리: 상한 $20, 커밋 넷 + 정리 커밋, 푸시)

## Goal
직업 기본값·성장·스탯의 %·특성·방어구·깊이 아이템·개정된 스탯·장비 요구·어휘 아이템의 값이 띠 안에 선다(광산: 새 파티 클리어 50~65%, 죽음 0.3~0.4, 붕괴 30~50%; 런: 04 §8). 직접 플레이(사용자)와 Acceptance 1~9. 끝나면 Slice D 완료의 선언은 사용자의 몫.

## State
- 1차 맞추기 구현 끝(2026-10-21): `BalanceData.csv` `StatBaseline` 10 → 7, `PowerBaseline` 5 → 4, `CritFatigueTaken` 15 → 12. 그 밖의 데이터는 그대로(군주 HP 364, 어휘 아이템의 값·가중치, HP 점당, 광산 복리, 묶음의 문턱, 요구값). 시뮬 전후 08 §54, 전체 체인 `20261021-184221` **CHAIN OK**. **1차 맞추기 완료.** 커밋 `2e6014a`(BalanceData)·`5da270e`(기획·Architecture, 47단계와 함께).
- 판정 열셋의 기록: 00 "43단계의 판정". 레버 스윕의 방법과 읽은 것: 08 §54 "방법"(복사본의 출력은 Git 밖).
- Round 93(새 직업 넷의 대표 그림, 저주의 서·낡은 류트의 아이콘): 생성 6회 약 $0.17 → 사용자 판정 1차(도적: 칼 든 모습이 어색 → 재생성, 궁수: 조준 자세라 공격 모션 → 백업하고 준비 자세로 재생성) → 소재 둘을 고쳐 재생성 2회 약 $0.06(장부 누계 377회 약 $10.08 — 상한 $10을 넘김, 새 상한은 사용자가), → 사용자 "자세는 2차, 얼굴은 1차로" → 자세 타입(`hit` + 1차 원본의 기준 그림, `ready-pose.csv`)으로 2회 약 $0.06 더(누계 379회 약 $10.14) → 3차 `rogue_v3`·`archer_v3`(1차의 얼굴, 2차의 준비 자세; 리뷰 시트 `review-figures-v3.png`). → 사용자 "도적 단검을 캐릭터 기준 오른손으로" → 같은 길로 1회(누계 380회 약 $10.17) → 4차 `rogue_v4`(먼 손) → 사용자 "반대 손으로" → 1회(누계 381회 약 $10.20) → 5차 `rogue_v5`(앞쪽 손) → 사용자 "뒤쪽 손으로" → 1회(누계 382회 약 $10.23) → 6차 `rogue_v6`(뒤로 뺀 손이나 날이 반대쪽) → 날을 적 쪽으로 1회 더(누계 383회 약 $10.26) → 7차 `rogue_v7`(뒤쪽 손, 날이 적을 향함) → 사용자 "뒷손을 자연스럽게" → 7차를 기준으로 1회(누계 384회 약 $10.29) → 8차 `rogue_v8`. 권장: 8차 도적·3차 궁수·1차 흑마법사·악사·아이콘 둘 확정 → 2026-10-22 사용자 **"도적 전투 준비 이미지를 5차 때 이미지로 변경"** → 도적의 확정 원본은 **5차** `rogue_v5`(호출 없음, README 판정 7차). 궁수·흑마법사·악사·아이콘 둘은 **판정 대기**.
- **Round 94**(사용자 "붕괴 사진은 피격 사진으로 대체하고 공격모션, 피격모션, 각성 모션 이미지 생성" — 새 직업 넷, 도적은 8차): 공격 4·피격 4·각성 4 = 12회 약 $0.36(장부 누계 396회 약 $10.65). 맞추기는 금 장식이 도적에게만 맞아 나머지는 머리 크기의 `Scale`(README의 표). 리뷰 시트 `review-attack-hit.png`·`review-poses.png`. 권장 열두 장 확정. → 2026-10-22 사용자 **"도적 피격 및 각성 이미지도 앞손에 칼을 든 상태로 이미지 재생성(현재 전투 준비 이미지 기반으로)"** → 5차를 기준으로 피격·각성 2회 약 $0.06(누계 398회 약 $10.71; 1차는 `candidates/*_v1`), 장식 배율 0.969·0.935, 권장 둘 다 확정(`review-rogue-hands.png`). 열두 장 **판정 대기**. 연결은 Round 93의 대표 그림·아이콘과 함께(붕괴 자리에 피격 그림을 복사). `ArtPipeline/Archive/94-job-poses/README.md`., 연결은 승인 뒤(자세 넷은 다음 라운드 — README "연결"의 권장: 대표 그림과 자세를 함께 연결). `ArtPipeline/Archive/93-job-figures/README.md`.
- **Round 93·94 연결**(2026-10-22 사용자 **"연결 및 구현"**): `Assets/@Art/Unit/Job/{rogue,archer,occultist,minstrel}.png`(도적 5차·궁수 3차·흑마법사·악사 1차), `Assets/@Art/Pose/Job/<id>_attack|_hit|_broken|_resolute.png` 열여섯(붕괴 = 피격의 복사), `Assets/@Art/Item/cursed_tome.png`·`lute.png`(`trim_items.py`), `JobData.csv`의 `Figure` → `unit/job/<id>` 넷, `ItemData.csv`의 `Icon` → `item/cursed-tome`·`item/lute`, Generated JSON(`transform`·`validate` OK). `ShippedDataTests`: 아이콘 예외 둘 삭제, 직업 넷의 `Figure` 단언, 마검사의 그림을 쓰는 직업 없음. 마검사의 그림 다섯(`spellblade`·자세 넷)은 아무 직업도 가리키지 않아 `Assets`에서 지우고(`.meta`도) `ArtPipeline/Archive/93-job-figures/removed-spellblade/`에 남겼다 — Addressables의 Entry는 setup의 `AddressablesSetup.Sync`가 지운다. 승인 원본 `Archive/93-job-figures/approved/`(그림 넷·아이콘 둘과 원본)·`94-job-poses/approved/`(열두 장과 원본). 체인: 아래 Verification(`20261022-063327` CHAIN OK).

## State (커밋)
- 2026-10-22 세션 정리: `5da270e` 기획·Architecture(47단계 + 43단계 1차), `7e36c3a` 47단계 구현·Test·데이터, `2e6014a` 43단계 1차 BalanceData, `f4eb9b3` Round 93·94 생성·연결(Assets/@Art·Addressables·Archive 93·94·Rosters·장부·스크린샷 캡처), 정리 커밋(Roadmap·CLAUDE.md·Handoff). 푸시 origin/feature/slice-a. `ArtPipeline/Archive/73-abyss/rejected/`는 Round 73의 규칙대로 Git에 올리지 않는다.

## Done
- 데이터: `BalanceData.csv` 셋, Generated `BalanceData.json`.
- 문서: 05 §2(기준 7·4), 02 §4(세기 기준 4, 요구값 5 그대로), 04 §3(치명타 피로 12), 08 §12(광산 띠에 새 파티 클리어 50~65%)·§54(전후), 00(결정 줄, "43단계의 판정" 표), Architecture/09(구현 순서 12의 1차), CLAUDE.md §9, Roadmap(43단계 절·요약·날짜 표·사후 검토 대기).
- 코드 변경 없음(43단계). Round 93·94 연결의 변경은 Test 하나(`ShippedDataTests`)와 데이터·그림.

## Open
- 끝난 날(균형형 36일)이 띠 40~60 아래 — 파티가 세져 런이 빨리 끝난다. 04 §8의 띠가 "직접 플레이의 속도를 본 뒤"로 열려 있어 사후 검토.
- 직접 플레이(사용자): 새 직업 넷(역마차에서 온다), 새 아이템 29와 칩, 요구 줄, 7·4 기준의 카드 수치. 그 뒤 2차 맞추기.
- (닫힘 2026-10-22) Round 93·94의 판정과 연결.
- (닫힘 2026-10-22) 유료 호출 상한: Round 93·94로 누계 398회 약 $10.71이 옛 상한 $10을 넘었고(모두 사용자의 지시), 사용자가 새 상한을 **$20**으로 정했다(Roadmap "유료 호출"). 남은 것 약 $9.29.

## Verification
- `transform`(BalanceData.json) → `validate` OK(8 jobs, 58 items, 9 enemies).
- 시뮬 전후(08 §54, 같은 시드, 저장소 빌드 — 전은 47단계의 자리): 광산 25.8 → **57.2%**, 죽음 0.63 → 0.38, 붕괴 76 → 39%; 런 균형형 26.2 → **68.5%**(한도 6.7, 끝난 날 36일, 열림 93%), 안전형 23.7 → **67.6%**(결전행 18%); 심연 새 파티 0.0 → 0.6%. 저장소의 출력은 복사본 `b43_364`와 byte 단위로 같다.
- 전체 체인 `20261021-184221` **CHAIN OK**: setup OK, sim OK(8 jobs, 58 items, 9 enemies), EditMode **1094/1094**, PlayMode **97/109**(건너뜀 12는 그래픽 스크린샷 Test). `Assets/InitTestScene*` 없음, `git diff --check` 깨끗. 화면 변경 없음(카드의 수치만 7·4 기준으로 달라진다).

- Round 93·94 연결(2026-10-22): 전체 체인 `20261022-063327` **CHAIN OK**: setup OK(새 PNG 22의 Import와 `.meta`, Addressables Entry +22/−5), sim OK(8 jobs, 58 items, 9 enemies), EditMode **1094/1094**, PlayMode **97/109**(건너뜀 12는 그래픽 스크린샷 Test). `Assets/InitTestScene*` 없음, `git diff --check` 깨끗. 스크린샷 대조(`Tools/screenshots.sh`의 fixture에 더한 캡처 `UiScreenshotTests.TheNewJobs_Korean` — 역마차의 후보를 넷으로 꾸며 고용하고 1~4열에 세운 뒤: `ko_82_expedition_party_new_jobs`(원정 파티 창: 도적·궁수·흑마법사·악사가 바닥선에 서고 머리 크기가 같다, 목록의 썸네일도 새 그림), `ko_83_battle_new_jobs`(1층 전투: 넷이 고블린과 같은 바닥선에, 보드에 류트·저주의 서·활·단검의 아이콘), `ko_84_battle_new_jobs_attack_pose`(도적의 공격 자세: 앞손의 단검을 곧게 찌름), `ko_85_battle_new_jobs_hit_pose`(궁수의 피격 자세). 전체 fixture `20261022-065736` 12/12(기존 캡처는 시작 로스터라 새 직업이 없다), 새 캡처만 `20261022-newjobs` 1/1. 사본 `Archive/94-job-poses/impl-*.png` 넷). 권장안(기록): 이 캡처는 fixture에 남긴다 — 다음 그림 라운드(특성에 맞춘 그림 등)에서도 새 직업 넷을 한 번에 본다.
- 정리 체인 `20261022-070904` **CHAIN OK**(fixture 캡처를 더한 뒤: setup·sim OK, EditMode 1094/1094, PlayMode 97/110 — 건너뜀 13은 스크린샷 Test). `Assets/InitTestScene*` 없음, `git diff --check` 깨끗.

## Next Action (제안)
- 직접 플레이(사용자): 새 직업 넷이 자기 그림으로 역마차·막사·전투에 선다(공격·피격·붕괴(=피격)·각성). 눈으로 볼 것: 머리 크기가 다른 직업과 같은가, 자세의 뒷발이 바닥선에 서는가, 저주의 서·류트의 아이콘이 상점·보드에 보이는가.
- (끝) 세션 정리 2026-10-22(위 "State (커밋)").
- 다음 세션의 시작: `CLAUDE.md → Docs/Roadmap.md(맨 위 요약, 날짜 표, 43단계 절, 47단계 절, 사후 검토 대기) → Docs/Workstreams/active/d43-tuning.md` 순서로 읽고 상태를 복구한 뒤, 사용자의 직접 플레이 기록(`Tools/playlog.py`)을 읽고 **43단계 2차 맞추기**(끝난 날의 띠, 어휘 아이템의 값, 플레이 피드백)를 보고한다. 그림 라운드는 요청 때만(상한 $20, 남은 것 약 $9.29).
- 직접 플레이 → 2차 맞추기(끝난 날의 띠, 어휘 아이템의 값은 플레이 뒤).
