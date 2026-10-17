# Slice C 29단계: 맞추기와 직접 플레이 — 100일 런의 띠, 데이터 조정, Acceptance

Snapshot: 2026-10-16 (27~29단계 일괄 승인 아래 맞추기를 권장안으로. 직접 플레이는 사용자의 몫. 체인은 아래 Verification)

## Goal

Architecture/09 "Slice C" 구현 순서 5가 참이다: 100일 런의 띠가 정해져 있고 시뮬 `run`의 수치가 그 안이며, Acceptance 1~6·8이 확인됐다. 7(직접 플레이로 런 하나를 끝까지)은 사용자가 한다.

## State

- 2026-10-16 사용자 "이어서 … 슬라이스 완성을 위한 나머지 절차를 네 권장안에 따라 완성할 때까지 알아서 진행(필요시 이미지 api 사용)" → Roadmap "27~29단계의 일괄 승인": 띠도 권장안으로 정해 적는다.
- 띠와 조정은 Design/04 §6·§8, 08 §27, Roadmap "사후 검토 대기"의 "29단계의 권장안 세부".
- **사용자의 몫(남김)**: 직접 플레이(Acceptance 7: 런 하나를 끝까지, `completed/b16-tuning.md`의 체크리스트, 원정 15~30분), 그 피드백에 따른 조정, 숫돌 값의 복귀(임시 조치: "복귀는 직접 플레이 뒤"), Slice C 완료의 선언(CLAUDE.md §9·Architecture/09의 상태 줄 갱신).

## Done

- 2026-10-17 사용자 "보스도 깊이 보정 적용"과 "용병 단순 피격시에는 피로+0(치명타만 피로 추가 적용)": `ExpeditionRules.FloorsBelowTheFirst`가 보스에도 층을 줌(최종 결전은 1층), `BalanceData.csv` `FatigueOnHit` 0(치명타 피로 `CritFatigueTaken` 9는 그대로), `LongExpeditionTests`의 보스 줄, PlayMode의 붕괴 연출 Test는 피격 대신 **동료의 빈사**(1 HP의 미끼를 1열에, 발키리는 2열)로 다시 무대를 꾸밈. 전후와 치명타 피로의 후보는 Design/08 §30.
- 2026-10-17 사용자 "층당 적 hp 점차적 추가 증가 검토" → "권장안 반영": `ExpeditionRules.DeeperMaxHp`(층마다 ×(100+p)/100를 정수로 거듭), `DungeonData.csv` 광산 `EnemyHpPerFloorPercent` 5, `EnemyData.csv` 군주 364·호위 42, Generated, `LongExpeditionTests`(복리 단위 Test와 깊은 층 Test). 검토·전후·남은 결정(보스의 깊이 보정)은 Design/08 §29. 복사본 시뮬은 scratchpad의 `simcopy`(Git에 올리지 않음).
- 2026-10-17 사용자 결정 "깊이 ×2 + 피해 ×0.67 + 피격 1 + 폭풍 70초로 구현"(원정 하나 15~30분): `DungeonData.csv` 광산 30층(정예·야영지 11, 상점 7, 단계 9·20, 야영지 층 30, 층당 HP 2%), `EnemyGroupData.csv` 광산 무리의 층 범위 두 배, `ItemData.csv` 모든 피해 ×0.67(글라디우스 8.04-13.4, 별 +4 유지), `BalanceData.csv` `FatigueOnHit` 1·`StormStartMs` 70000, Generated 넷, `ShippedDataTests`의 글라디우스 값. 전후와 검토한 안은 Design/08 §28. 판이 쉬워졌다(런 92%): 되돌릴 레버를 재어 둠(08 §28 "다시 맞추기"), 고르는 것은 사용자.

- 측정(Design/08 §27): 균형형 300런 시드 1·2·3과 1,000런(52.7~61.3%, 1,000런 54.9%), 안전형(63.7%, 최종 결전 12%). 패배는 거의 전부 고용할 금이 모자란 전멸(41%), 기한과 사망 한도에는 닿지 않음.
- 레버 스윕(저장소 밖 복사본 `--project-root`, 시드 1·300런): 속도(해금 3·4회, 광산 6·7일, 쉬기 2일, 심연 5일, 합친 둘) — 기한의 런은 3% 이하, 승리 56~61%, 전사자만 늚; 경제(고용비 20, 층의 금 3, 클리어의 금 30, 합침, 정책의 일곱 고용·80 치료) — 고용비 20이 77%, 층의 금 3이 73%, 합치면 81%지만 쉬기 0.9·사망 한도 13%.
- 채택: **고용비 30 → 20**(`BalanceData.csv`, Generated). 시드 셋 73~77%, 1,000런 72.7%, 안전형 76%(최종 결전 15%). 저장소 데이터로 다시 돌린 시드 1이 복사본과 같음(76.7%).
- 띠(Design/04 §8, 권장안): 균형형 승리 65~80%, 전멸 ≤ 25%, 사망 한도 ≤ 12%, 끝난 날 40~60일, 최종 던전 열림 ≥ 75%; 안전형 승리 65~80%, 최종 결전 5~25%.
- Test: PlayMode `UiFlowTests.Lobby_TheTown_…`의 고용비(30 → 20, 금 80·60). 시험 데이터(`TestData`·`TestCsv`)의 30은 그대로(시험용).
- Acceptance 점검표: Roadmap 29.
- 문서: Design/00·04 §5·§6·§8·08 §27, Roadmap(29, 사후 검토, 진행), c28 Handoff를 completed로.

## Open

- 사용자: 직접 플레이 → 피드백 조정 → 숫돌 1 → 8 복귀(Roadmap "임시 조치") → Slice C 완료 선언.
- 기한(100일)은 균형형 시뮬에서 닿지 않는다(1% 아래). 플레이의 속도를 본 뒤 속도 레버(08 §27)를 다시 본다.
- 사후 검토: 띠의 값, 고용비 20, 옛 결론 82%/75%를 띠 안의 목표로 둔 것.

## Verification

- `transform`·`validate` OK. 시뮬 Design/08 §27.
- 체인 `20261016-233306`(고용비 20의 데이터와 로비 Test: setup·sim OK, EditMode **917/917**, PlayMode **87/98** — 건너뜀 11은 그래픽이 있어야 하는 스크린샷 Test).
- 2026-10-17 깊이 ×2의 데이터: 체인 `20261017-073612`(setup·sim OK, EditMode **917/917**, PlayMode **87/98**), 그래픽 스크린샷 `~/Library/Caches/F1/screenshots/20261017-074500-depth`(11/11, PNG 109장: 30층 지도의 스크롤, 전투의 "폭풍까지 70.0초").
- 2026-10-17 복리와 심연 HP: 체인 `20261017-084458`(setup·sim OK, EditMode **918/918** — `DeeperMaxHp` 단위 Test를 더함, PlayMode **87/98**). 화면은 바뀌지 않아 스크린샷은 다시 찍지 않았다. 시뮬은 `dotnet build Tools/Sim` 뒤에 다시 잰 값(08 §29).
- 2026-10-17 보스의 깊이 보정과 피격 피로 0: 체인 `20261017-092453`(setup·sim OK, EditMode **918/918**, PlayMode **87/98**), 그래픽 스크린샷 `~/Library/Caches/F1/screenshots/20261017-093500-boss-hit`(11/11, PNG 109장: 붕괴 연출 `ko_38`이 새 무대 — 2열의 발키리가 "강인", 1열의 미끼가 빈사 — 에서 찍힘). 치명타 피로 15/3(데이터만): 체인 `20261017-094417`(setup·sim OK, EditMode 918/918, PlayMode 87/98).

## Next Action (제안)

- 세션: 체인 뒤 커밋·푸시. 다음 세션은 사용자의 직접 플레이 기록(`Logs/`)을 읽어 맞추기와 Slice C 마감.
