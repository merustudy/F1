# Slice D 46단계: 아이템 목록

Snapshot: 2026-10-21 (상태 복구 → 유사 게임의 목록 구조 조사 → Design/19 제안 → 사용자 "권장안 구현" → 46단계 구현·시뮬·체인 OK → Round 92 아이콘 33 생성·재생성 넷·마름쇠 → 투척 단검 → "승인"으로 연결·체인 OK·스크린샷. 세션 정리: 2026-10-21 주제별 커밋·푸시, 이 Handoff는 completed로)

## Goal
어휘 열셋(출혈·재생·가시·정화·경직·충전, 빙결·암흑·조준·예리·격분·가속·취약)이 모두 아이템에 실려 상점·전리품·적의 깊은 층 묶음에서 나오고, 직업 다섯이 저마다 새로 들 것이 생기며(방어구의 요구가 네 축에), 정화·재생 포션과 가방 둘, 직업마다의 유니크가 데이터로 선다. 엔진 변경은 포션의 재생 갈래뿐(Design/19 §4·§10, 02 §4 "아이템 목록"·§10, 03 §4·§5).

## State
- 2026-10-21 상태 복구(44·45단계 완료·`dbc693d` 푸시) → 사용자 지시 "아이템 목록 제안 … 【제안】 … 판정할 것을 번호로 … 승인을 기다려" → 조사(별도 에이전트: 백팩 배틀즈·백팩브롤·템빨용사의 목록 구조) → **Design/19**(판정할 것 열여섯, 모두 권장안을 적음) → 사용자 **"권장안 구현"** → **46단계** 시작.
- 구현 끝, 전체 체인 `20261021-121331` CHAIN OK(Verification). **46단계 완료.** 아이콘은 Round 92(아래 Open)로 같은 날 연결, 체인 `20261021-150110` CHAIN OK. 세션 정리(2026-10-21): 기획·Architecture `de850af` / 46단계 구현 `8030455` / 화면 `c58583a` / Round 92 `638885c` / Roadmap·Handoff 커밋, 푸시.

## Done
- 기획: Design/19 헤더(판정됨), 00 "Design/19의 판정" 표와 문서 목록 행, 02 §4 "아이템 목록"(새 아이템 29의 표와 셈의 규칙)·§10(포션 넷), 03 §4(묶음 넷)·§5(상점·드랍 풀, 가방 둘), 07 §4(행·`Regen`). CLAUDE.md §9(32~46, 46단계).
- Architecture: 05(46단계 줄: 열 변경 없음, `Regen`, 수, 빈 `Icon`), 08(포션의 재생, 정책의 포션·자기 버프), 09(Slice D 표 행 "아이템 목록", 구현 순서 15, Acceptance 5-8, 제목 32~46).
- 데이터: `ItemData.csv` +29(무기 8·공격 4·지원 6·방어 5·적 2·유니크 4 → 56), `RecipeData.csv` +4(→ 5), `PotionData.csv` +2(정화 `Cleanse` 3·재생 `Regen` 8 → 4), `BagData.csv` +2(등짐 2×2 16·큰 배낭 3×2 24 → 5), `EnemyData.csv`의 `DeepItems` 넷(쥐 11 `bristle_hide`, 약탈자 21 `goblin_sawblade`, 궁수 21 `ash_pouch`, 주술 대가 21 `hex_bone`). 새 행의 `Icon`은 비어 있다(아이콘 라운드까지).
- 코드: `DataEnums.PotionEffect.Regen`, `PotionMapper`(`Cleanse`·`Regen`은 정수), `BattleEngine.TryUsePotion`의 재생 갈래(`AddRegen`, 원인은 포션 id), `UiKeys.Potion.Regen`·`UI_StaticText.csv`의 `Potion.Regen`·`UiText.PotionDetails`, `PotionSlotView`의 이름 라벨(`_label`, 아이콘이 없을 때만 — `UiPrefabSetup.Kit.BuildPotionSlot`)·`ItemSlotView.ShowPotion(potion, icon)`의 이름, `Tools/Sim` `SimPolicy`(`UsePotionIfNeeded`: 치유 → 빈사의 방벽 → 재생; `CleanseIfNeeded`: 해로운 중첩 넷 이상인 구성원에게 정화; `MembersInOrderFor`: 자신에게 예리·격분을 거는 아이템은 치명타 확률이 가장 높은 무기를 든 구성원 먼저).
- Roadmap: 맨 위 요약, 날짜 표, "Design/19의 판정" 절, 사후 검토 대기 46단계 줄, 46단계 절.

## Open
- **Round 92 아이콘**(2026-10-21 사용자 "현재 만들어진 아이템 아이콘 생성 후 보고. 승인 후 연결"): 소재 33행(`Rosters/item.csv` 29·`bag.csv` 2·`ui_icon.csv` 2), `Archive/92-item-icons/generate.sh`로 33회 생성(약 $0.78, 장부 누계 364회 약 $9.74 — 총 상한 $10, 남은 것 약 $0.26). 후보 `candidates/` 33, 리뷰 시트 둘, README의 읽은 것과 권장(확정 29, 재생성 3: 그늘 지팡이·살기의 부적·가죽 갑옷(미늘창 누출), 마름쇠는 선택) → 사용자 "재생성 3 / 선택 1 — 재생성 후 보고. 승인 후 연결" → 넷을 1회씩 다시 그림(`_v2`, 4회 약 $0.09, 누계 368회 약 $9.83, 남은 것 약 $0.17). 2차 넷 모두 방향대로 나와 권장 "2차 확정" → 사용자 "마름쇠 대신 다른 아이템으로 변경" → 후보 셋 중 권장 **투척 단검**(`throwing_knives`)을 "권장안 반영"으로: `ItemData.csv`의 행 교체(자리·수치 그대로), Generated `ItemData.json`, 02 §4·Design/19·00 판정 16, 소재 행, 생성 1회(누계 369회 약 $9.85). → 사용자 **"승인"** → 33 전부 배선: 아이템 29(`Assets/@Art/Item`, `trim_items.py`, 유니크 넷의 `_glow`, `ItemData.Icon`), 가방 둘(`Assets/@Art/Bag`, `BagData.Icon`), 포션 둘(`Assets/@Art/Potion`, `PotionData.Icon`), Generated JSON, `ShippedDataTests`의 그림 단언, 문서(02 §4, 03 §5, 10 §5, Architecture/05·13, README 92 판정 3). 라운드의 호출 38회 약 $0.90, 누계 369회 약 $9.85. 체인은 Verification. 승인된 것만 배선: 아이템은 `trim_items.py` → `Assets/@Art/Item/<id>.png` + `ItemData.Icon`(유니크 넷은 `glow_items.py`), 가방은 `Assets/@Art/Bag/<id>.png` + `BagData.Icon`, 포션은 `Assets/@Art/Potion/<id>.png` + `PotionData.Icon`, 변환, 체인.
- **43단계 맞추기와 직접 플레이**: 10-20의 판정 열 개는 승인 대기. 46단계 뒤의 자리는 08 §52(광산 27.5%, 런 15~31% — 띠 아래; 광산의 죽음 0.60·붕괴 72.7%는 띠 위; 최종 던전이 열리는 런 73%이나 심연은 그대로 — 다음 벽). 레버: 어휘 아이템의 값·가중치, 깊은 층 묶음의 세기, 심연.
- 2차로 남긴 것: 곡도·화약 폭탄(Design/19 §6), 가속 포션, 둘째 ★ 아이템(엔진), 효과 있는 가방, 조건 발동 아이템.
- 권장안 세부(사후 검토): Roadmap "사후 검토 대기"의 46단계 줄. 구현에서 정한 것: 포션 칸·상인 조각의 이름 Placeholder, 시뮬 정책의 정화(해로운 중첩 넷 이상)·재생(치유가 없을 때)·자기 버프의 자리(치명타 확률이 가장 높은 무기).

## Verification
- `dotnet run --project Tools/Sim -c Release -- transform`(BagData·EnemyData·ItemData·PotionData·RecipeData.json 다시 씀) → `validate` **OK**(5 jobs, 56 items, 9 enemies). 시뮬 빌드 경고 0·오류 0(정책의 변경 포함).
- 시뮬 전후(08 §52, 같은 시드, 저장소 빌드 — 전은 `dbc693d`, 후는 46단계): 광산 원정 1,000 클리어 3.4 → **27.5%**(죽음 0.80 → 0.60/원정, 21층 승리 61.7 → 83.7%, 방어구 0.77 → 1.35, 새로고침 0.36 → 1.57), 심연 500 **byte 단위 같음**, 런 300 균형형 4.0·6.7·6.7 → **17.7·15.3·20.7%**, 안전형 11.7·9.3·10.0 → **31.0·27.3·26.0%**(최종 던전이 열린 런 15 → 73%, 심연 시도당 클리어는 그대로 4~5%). 어휘의 줄이 처음으로 찍힌다(전투마다 암흑 13.5·재생 10.3·출혈 6.4·가속 6.0·조준 5.3·취약 4.5·빙결 3.2, 가시 1.71, 정화 1.18·경직 2.10·충전 0.87). 출력은 scratchpad `sim-before46`·`sim-after46`.
- 전체 체인 `20261021-115412`: setup OK(문구 표의 `Potion.Regen`, 글꼴 atlas, BattleScreen·NodeMapScreen Prefab의 포션 칸 라벨, 도장), sim OK, EditMode **1087/1087**(새 Test 넷: 재생 포션의 틱과 지혈, 재생 포션의 변환, 유니크 넷, 46단계의 목록), **PlayMode 96/109 — 1 실패**: `UiFlowTests.Shop_ItsWindowOpensOverTheMap_…`가 상점의 첫 물건을 앞줄 용병의 보드에 놓는데, 풀이 커져 그 물건이 앞줄 용병이 요구를 못 채우는 장비였다(Expected Fits, was Refused — 42단계의 규칙). Test의 선택 조건에 "앞줄 용병이 요구를 채우는 것"을 더해 고쳤다(규칙은 그대로).
- 고친 뒤 전체 체인 `20261021-121331` **CHAIN OK**: setup OK, sim OK, EditMode 1087/1087, PlayMode **97/109**(건너뜀 12는 그래픽 스크린샷 Test). `Assets/InitTestScene*` 없음, `git diff --check` 깨끗.
- Round 92(아이콘 33 배선, 마름쇠 → 투척 단검) 뒤 전체 체인: `20261021-142817`·`145033`은 PlayMode의 상점 Test 하나가 격자 행 수의 고정 단언(8행, 가방은 8행)에서 멈춤 — 풀이 커져 그 시드의 상점이 9행을 쓴 것(설계대로 행을 더함). Test를 규칙대로 고쳐 `20261021-150110` **CHAIN OK**(setup OK — 새 PNG 33 Import, sim OK, EditMode 1087/1087, PlayMode 97/109). `Assets/InitTestScene*` 없음, `git diff --check` 깨끗.
- 스크린샷 `20261021-152336`(12/12, Render 설정 변경 없음): 고정 시드의 4층 상점에 석궁(2×2)·곡괭이·저주의 뼈(2×1)·깃발(1×3 세로 조각)과 재생 포션이 선다 — 조각이 잘린 아이콘을 비율대로 채우고 이름 Placeholder는 없다(`ArtPipeline/Archive/92-item-icons/impl-shop.png`).

## Next Action (제안)
- 46단계와 Round 92는 체인까지 닫혔다(마지막 `20261021-150110`). 다음 세션: **직접 플레이**(새 아이템 29와 상태 줄의 칩이 비로소 전투에 뜬다 — 플레이 기록은 `Tools/playlog.py`) → **43단계 맞추기**(10-20의 판정 열 개는 승인 대기; 46단계 뒤의 자리는 08 §52 — 광산 27.5%, 런 15~31%, 심연이 다음 벽; 레버에 어휘 아이템의 값·가중치와 묶음의 세기가 더해짐).
- 유료 호출 누계 약 $9.85 / 상한 $10: 다음 그림 라운드(2차 아이템·가속 포션 등) 전에 **상한을 올릴지** 사용자가 정한다.
- 2차로 미룬 것: 곡도·화약 폭탄(CSV 한 줄 + 아이콘), 가속 포션(작은 코드), 둘째 ★ 아이템(엔진), 효과 있는 가방, 조건 발동 아이템(Design/19 §9).
