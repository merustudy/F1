# Slice D 44단계: 효과 어휘

Snapshot: 2026-10-21 (44단계 구현 뒤 2026-10-21 사용자 "흡혈은 삭제"로 흡혈을 뺐고, Round 90의 상태 아이콘 셋을 확정·배선했다. 이어서 45단계(2차 어휘 일곱, 대상의 편 제한 없음)를 구현했고 Round 91의 상태 아이콘 일곱을 "연결"로 확정·배선했다. 체인은 아래 Verification. 세션 정리: 2026-10-21 주제별 커밋 다섯·푸시, 이 Handoff는 completed로)

## Goal
(45단계, 2026-10-21) 2차 어휘 일곱(빙결·암흑·조준·예리·격분·가속·취약)이 중첩-감쇠의 상태로 들어오고, 어느 효과든 적·아군 어느 편에나 걸 수 있다(Design/18 §10, 02 §4·§5). 상태 아이콘 일곱은 Round 91로 생성해 승인 뒤 칩과 함께.

(44단계) 전투의 효과 어휘가 넷(피해·회복·보호막·화상)에서 열로 는다: **출혈·재생·가시·정화·경직·충전**(Design/02 §4·§5, Design/18 판정. 무기의 흡혈도 넣었다가 2026-10-21 사용자 "흡혈은 삭제"로 뺐다). 규칙은 결정론 안에서(정수 ms, 새 난수 없음, 시간제 상태 없음) 작게 들어가고, 어휘를 쓰는 아이템은 아직 없어 출고 데이터의 전투는 그대로다(08 §50). 유닛 위의 상태 표시와 연출은 목업 라운드 뒤(판정 11).

## State
- 2026-10-20 상태 복구(42단계까지 완료·`827efb1` 푸시) → 43단계 첫 걸음 보고(판정 열, 승인 대기) → 사용자 "아이템 목록 … 제안" → 조사 중 사용자 "효과(화상, 가시 등) 정하고 아이템 정하는 걸로. 잠깐 미루자" → 사용자 "백팩 배틀즈, 템빨용사, 백팩브롤 등의 효과 검토 … 유사·유명 게임도" → **Design/18**(효과 어휘 검토, 판정할 것 열둘) → 사용자 **"권장안 구현"** → 새 **44단계**로 구현(판정 12는 "구현" 지시로 어휘를 먼저, 43단계의 맞추기는 그 위에서).
- 구현 끝, 전체 체인 `20261021-001806` CHAIN OK(Verification). 44단계 완료. 커밋은 2026-10-21 세션 정리에서(아래 Next Action의 머리).
- 2026-10-21 사용자 "흡혈은 삭제" → `ItemData.LifestealPercent`(정의·검사·JSON 속성)·변환 열·엔진의 흡혈 갈래·`CauseLifesteal`·문구 둘(`Item.Lifesteal`·`Log.SourceLifesteal`)·흡혈 사실·Test를 지우고 문서를 고쳤다. Generated `ItemData.json` 다시 씀(속성 빠짐). 체인은 Verification의 셋째 줄.
- 2026-10-21 사용자 "출혈·재생,가시에 대한 아이콘 생성. 연결은 내 승인 후" → **Round 90**(`ArtPipeline/Archive/90-status-icons/`): `ui_icon` 셋 생성(3회, 약 $0.086), 후보와 칩 크기 견본 보고 → 사용자 "가시만 한개나 두개만 나오게(가지 없이)(흰색) 재생성" → 가시의 소재를 바꿔 1회 재생성(`thorns_v2`) → 사용자 "너무 얇아. 이전에 가지에 달린 가시 느낌으로 재생성" → 굵은 가지의 가시 둘로 1회(`thorns_v3`) → 사용자 "해당 가시를 크게하고 가지는 제거하여 재생성" → 가지 없는 굵은 가시 둘로 1회(`thorns_v4`), 출혈·재생은 그대로 → 사용자 **"이걸로 가자 연결"** → 셋 확정·배선(아이콘 셋, `UiArt`, 상태 줄의 칩 셋과 문구 셋, 팔레트 셋). 체인은 Verification의 넷째 줄. 연결은 승인 뒤(상태 칩의 자리는 목업 라운드와 함께).
- 2026-10-21 사용자 "그 외에도 빙결 … 암흑 … 미정(명중↑) … 미정(치명타 확률↑) … 미정(치명타 배율↑) … 그 외 네가 추천하는 다른 효과들 검토 후 보고. 승인 후 구현" → **Design/18 §10**(2차 어휘 검토, 판정할 것 열) → "2차 효과 검토 결과를 정리해서 보고해줘" → 사용자 **"가속, 취약 넣는 방향으로 구현 착수 / 모든 효과는 적/아군 서로 가리지 않고 적용될 수 있게 구현"** → **45단계**로 구현(범위·구현은 Roadmap 45단계 절). **Round 91**(`ArtPipeline/Archive/91-state-icons/`): 상태 아이콘 일곱 생성(7회) → 사용자 "예리는 다른 아이콘 없을까? 가속은 노란색 번개 모양으로 재생성. 승인 후 연결"·"암흑은 흑먼지 스타일로 재생성. 승인 후 연결" → 예리(날의 반짝임)·가속(노란 번개)·암흑(검은 먼지 구름)을 1회씩 재생성(Round 91 누계 10회 약 $0.29) → 사용자 **"연결"** → 일곱 확정·배선(아이콘 일곱, `UiArt`, 상태 줄의 칩 일곱 — 가시 다음에 빙결·암흑·조준·예리·격분·가속·취약, 문구 일곱, 팔레트 일곱 — 승인된 견본의 색, `BattleUnitView`의 보임, Architecture/12·13, Design/10, README 판정 2). 체인은 Verification의 마지막 줄들.

## Done
- 기획: Design/02 §3(틱의 차례: 화상 → 출혈 → 재생), §4(효과 타입 열, 대상 규칙, 값의 단위, 흡혈 열, 【원본확인】 "그 밖의 효과 타입" 닫음), §5("효과 어휘": 출혈·재생·가시·정화·경직·충전의 규칙), §8(regen·warded·armored 닫음), 04 §3(틱·가시의 피로 없음), 07(열·Key·포션), 00 "Design/18의 판정"(열둘), 18 헤더(판정됨).
- 데이터: `BalanceData.csv`의 `BleedDamagePerStack`·`RegenHealPerStack`(0.27, 0.01 단위). `ItemData.csv`·`PotionData.csv`는 행 변경 없음(어휘를 쓰는 아이템은 아이템 목록 제안 뒤). Generated `BalanceData.json`·`ItemData.json` 다시 만듦(`LifestealPercent` 0이 모든 아이템에).
- Data: `DataEnums`(`EffectKind` + `Bleed`·`Regen`·`Thorns`·`Cleanse`·`Stagger`·`Charge`, `PotionEffect.Cleanse`), `ItemData`(`ItemEffect`의 대상 검사 확장·`CountsStacks`·`CountsWhole`), `BalanceData`(Key 둘), `DefinitionMappers`(`ReadEffect`는 중첩·ms를 정수로, `PotionMapper`의 `Cleanse`는 정수). 흡혈 열은 2026-10-21에 뺐다.
- Domain: `BattleUnit.Bleed`·`Regen`·`Thorns`; `BattleTypes`(이벤트 여섯, 원인 `bleed`·`regen`·`thorns`, `IsTickCause`); `StatRules.CanMiss`(출혈·경직); `BattleEngine` — `Activate`의 여섯 갈래와 스탯 곱(중첩은 `ScaleStacks`), 피해 갈래 끝의 가시(가시에 쓰러진 공격자는 그 자리에서 끝), `Ticks`(`Tick.Burn`·`Bleed`·`Regen`, 각각 아군 먼저; 폭풍은 그대로), `ApplyDamage(throughShield)`와 틱 원인의 피로 없음, `ApplyHeal`의 지혈(`Cleansed` A 0·B 멎은 중첩), `Cleanse`·`Stagger`·`Charge`(바닥 다음 ms, 자기 자신 제외), 포션 `Cleanse`, `Kill`의 초기화.
- 화면(글만, 판정 11): `UiKeys`(Effect 여섯·Potion.Cleanse·Log 여섯·Log.Source 셋), `UI_StaticText.csv` 열다섯 행, `UiText.EffectKey`·`EffectValue`(중첩·초·수)·`PotionLine`, `BattleLogText`(원인 글, 이벤트 줄; 아무것도 빠지지 않은 정화는 줄 없음). `BattlePresenter`·`BattleUnitView`는 그대로(피해·회복 숫자만 뜬다).
- 시뮬: `Tools/Sim` `BattleStats`(어휘의 줄 — 쓰인 전투가 있을 때만), `trace`의 상세.
- Test: EditMode 새 `Gameplay/EffectVocabularyTests`(14: 출혈의 보호막 건너기·피로 없음·감소, 지혈, 물리의 명중 굴림, 재생과 패시브, 틱의 차례와 재생의 지혈, 가시의 반격·보호막·원거리 없음, 쓰러진 가시·가시에 쓰러진 공격자, 정화 아이템·포션, 경직, 충전의 바닥·자기 제외, 어휘 전부의 로그 재현, 정의·변환·포션·Key 검사), `TestData.BalanceEntries`와 `TestCsv.Balance`의 Key 둘.
- 문서: Architecture/05(열·Key·검사), 08("효과 어휘" 절, 차례), 09(Slice D 표 행, 구현 순서 13, Acceptance 5-6, 32~44), 12(어휘의 줄, 상태 표시는 목업 뒤), CLAUDE.md §9(32~44, 44 구현), Roadmap(맨 위 요약, 날짜 표, 사후 검토 대기, 43단계 줄, 44단계 절).

## Open
- 어휘를 쓰는 아이템이 없다: **아이템 목록 제안**(미룸)이 다음이다 — Design/18 §7의 밑그림(출혈 단검·발톱, 재생 약초·향로, 가시 방패·징 갑옷·가시 쥐, 정화 물주머니·성수, 경직 둔기·냉기 지팡이·얼음 플라스크, 충전 북·깃발, 흡혈 심연 무기, 유니크의 비틀기), 조사 출처는 메모리 `reference-item-games-survey`와 Design/18 §9.
- Round 90의 아이콘 셋은 확정·배선했다(상태 줄의 칩 셋: 화상 다음에 출혈·재생·가시, 견본의 색). 떠오르는 글·소리(연출)는 목업 라운드 뒤. 어휘를 쓰는 아이템이 없어 지금 전투에서는 칩이 뜨지 않는다 — 아이템 목록 뒤 직접 플레이에서 본다.
- Round 91의 상태 아이콘 일곱(빙결·암흑·조준·예리·격분·가속·취약. 예리는 날의 반짝임, 가속은 노란 번개, 암흑은 검은 먼지 구름으로 2차 — 사용자 지시)은 "연결"로 확정·배선했다(상태 줄의 칩 일곱: 가시 다음에 그 차례, 승인된 견본의 색). 어휘를 쓰는 아이템이 없어 지금 전투에서는 칩이 뜨지 않는다 — 아이템 목록 뒤 직접 플레이에서 본다. 3차로 남긴 것: 아이템 패시브, 관통, 처형, 적의 피로 공격, 피로 포션, 약화, 보호막 부수기, 회복 감소.
- 43단계 맞추기의 판정 열(상태 복구 보고)은 여전히 승인 대기. 44단계는 출고 수치를 바꾸지 않았다.
- 권장안 세부(사후 검토): Roadmap "사후 검토 대기"의 44단계 줄.

## Verification
- `dotnet build Tools/Sim -c Release` 경고 0·오류 0. `transform`(BalanceData.json·ItemData.json 다시 씀) → `validate` OK(5 jobs, 27 items, 9 enemies).
- 시뮬 전후(08 §50): 변경 전(`827efb1` + 조사만)의 Release 빌드로 광산 원정 1,000(균형형, 시드 1), 심연 원정 500, 런 300 균형형·안전형 시드 1·2·3을 scratchpad `sim-before`에, 변경 후 빌드로 같은 것을 `sim-after`에 돌려 `cmp`: 여덟 파일 모두 **byte 단위로 같다**(어휘를 쓰는 아이템이 없고, 틱의 차례를 화상 뒤에 덧붙였으며, `LifestealPercent` 0은 아무 일도 하지 않는다).
- 전체 체인 `20261020-235830`: setup OK, sim OK, PlayMode 97/109(건너뜀 12는 그래픽 스크린샷 Test), **EditMode 1074 중 38 실패** — 37은 Test용 Balance CSV 고정값(`TestCsv.Balance`)에 새 Key 둘이 없어 변환·적재 Test가 "key 'BleedDamagePerStack' is missing"으로 넘어진 것, 1은 경직 Test의 기대값(서리의 둘째 발동 3초가 3.5초의 발동을 4초로 미는 것을 빠뜨림). 둘을 고쳤다(고정값에 Key 둘, 기대값 1000·2500·4000).
- 전체 체인 `20261021-001806` **CHAIN OK**: setup OK(문구 표에 새 키 열일곱, 글꼴 atlas·Prefab 도장 갱신), sim OK, EditMode **1074/1074**, PlayMode 97/109(건너뜀 12는 그래픽 스크린샷 Test). `Assets/InitTestScene*` 없음. `git diff --check` 깨끗. 스크린샷은 뽑지 않았다(화면은 글만 바뀌었다).
- 흡혈을 뺀 뒤(2026-10-21) `dotnet build Tools/Sim -c Release` 경고 0·오류 0, `transform`(ItemData.json에서 속성 빠짐) → `validate` OK. 전체 체인 `20261021-070901` **CHAIN OK**: setup OK(문구 둘 빠짐), sim OK, EditMode **1072/1072**(흡혈 Test 둘이 빠져 1074 → 1072), PlayMode 97/109. `Assets/InitTestScene*` 없음, `git diff --check` 깨끗. Round 90의 아이콘은 배선하지 않았으므로 이 체인에 들어 있지 않다.
- Round 90 배선 뒤 전체 체인 `20261021-080131`: **setup FAILED** — `UI_StaticText.csv(267)`의 `Battle.Bleed` 행 설명 칸에 따옴표 없는 쉼표가 있어 열이 다섯으로 읽혔다(그래서 문구 표·글꼴·Prefab이 갱신되지 않아 EditMode의 Setup 검사 6개와 PlayMode의 UI 흐름 33개가 함께 넘어졌다: 생성물이 코드보다 오래됨, 새 아이콘의 Import 정책 미적용). 행을 따옴표로 고치고 다시 돌렸다: `20261021-081955` **CHAIN OK**(setup OK — 아이콘 셋 Import·문구 셋·Prefab 도장, sim OK, EditMode 1072/1072, PlayMode 97/109). 체인이 도는 동안 45단계의 편집이 시작돼 PlayMode는 그 일부를 안고 돌았을 수 있다 — 45단계의 전체 체인이 다시 확인한다.
- 45단계(2026-10-21): `dotnet build Tools/Sim -c Release` 경고 0·오류 0, `transform`(BalanceData.json) → `validate` OK. 시뮬 전후(08 §51): `sim-before`(§50의 기준)와 45단계 빌드의 `sim-after45` 여덟 파일이 `cmp`로 **byte 단위 같음**. 전체 체인은 아래 줄에.
- 45단계 전체 체인 `20261021-084518`: setup OK(문구 표의 새 키, Prefab 도장), sim OK, PlayMode 97/109(건너뜀 12는 그래픽 스크린샷 Test), **EditMode 1083 중 4 실패** — 넷 모두 새 `SecondVocabularyTests`의 기대값 오류였고 엔진은 설계대로였다: ① 빙결 Test가 서리(쿨다운 1.5초)의 둘째 발동(3초)이 중첩을 다시 쌓는 것을 빠뜨림 → 서리를 3.5초에 한 번만 떨어지게(쿨다운 3.5초, 6.4초까지) 하고 기대 발동을 1000·2000·3000·4000·5200·6300으로(중첩이 쿨다운 도중에 떨어져도 그 쿨다운은 그대로임을 함께 보임), ② 예리 Test의 첫 타 기대 1500은 150의 오타, ③ 취약 Test의 공격이 세기 1(피해 1)로 만들어져 110이 나올 수 없었음 → 세기 100, ④ 편 제한 없음 Test가 자기에게 건 빙결 둘이 자기 칼의 쿨다운도 늦추는 것(베기 1000·2100·3100)을 빠뜨렸고 `StateApplied` 수 2는 "한 번, 중첩 2"의 오기. 넷을 고치고 EditMode만 다시: `20261021-091059` **1083/1083**. 이어서 전체 체인 `20261021-091140` **CHAIN OK**(setup OK, sim OK, EditMode 1083/1083, PlayMode 97/109). `Assets/InitTestScene*` 없음, `git diff --check` 깨끗. Round 91의 아이콘은 배선하지 않았으므로 이 체인에 들어 있지 않다.
- Round 91 배선 뒤(사용자 "연결") 전체 체인 `20261021-095621` **CHAIN OK**: setup OK(아이콘 일곱 Import와 `.meta`, 문구 일곱, 글꼴 atlas·Prefab 도장), sim OK, EditMode 1083/1083, PlayMode 97/109(건너뜀 12는 그래픽 스크린샷 Test). `Assets/InitTestScene*` 없음, `git diff --check` 깨끗. 스크린샷은 뽑지 않았다(어휘를 쓰는 아이템이 없어 출고 전투에 칩이 뜨지 않는다. Round 90과 같음).

## Next Action (제안)
- 44·45단계와 Round 90·91은 체인까지 닫혔다(마지막 `20261021-095621`). 세션 정리(2026-10-21 사용자 "세션 정리"): 주제별 커밋 다섯 — 기획·Architecture 문서 / 44·45단계 구현(Data·Domain·변환·BalanceData·Generated JSON·시뮬·Test — 두 단계가 같은 파일을 고쳐 한 커밋) / 효과 어휘의 화면(글·상태 줄의 칩 열·아이콘 열·Unity 생성물) / Round 90·91 기록(소재 행·Archive·장부) / Roadmap·Handoff — 와 푸시(해시는 Roadmap 날짜 표).
- **다음 세션의 첫 일: 아이템 목록 제안**(사용자 2026-10-21 "다음 세션 아이템 목록 제안 예정"). 이 어휘(출혈·재생·가시·정화·경직·충전과 빙결·암흑·조준·예리·격분·가속·취약)로 인벤토리 아이템 목록(무기·공격·방어·지원, 유니크·포션·가방 포함)을 【제안】으로 낸다 — 밑그림 Design/18 §7, 조사 출처 Design/18 §9와 메모리 `reference-item-games-survey`, 미룬 경위 메모리 `project-item-catalog-order`, 지금 데이터 `ItemData.csv`(27)·`PotionData.csv`·`BagData.csv`, 요구 스탯(02 §4)·직업 다섯·깊은 층 묶음(03 §4)·적의 아이템과 맞물리게. 판정할 것을 번호로, 항목마다 권장안을 분명히. 승인 전 데이터 변경 없음. 승인 뒤 CSV → 변환 → 시뮬 전후(같은 시드) → 체인. 아이템의 아이콘은 승인 뒤 라운드로(유료 호출은 지시 때만).
- 그 뒤 **43단계 맞추기와 직접 플레이**(판정 열 10개는 10-20 보고, 승인 대기 — 아이템 목록이 들어간 뒤 다시 잼). 어휘를 쓰는 아이템이 들어오면 상태 줄의 칩 열이 비로소 전투에 뜬다 — 직접 플레이에서 본다.
- 커밋하지 않고 둔 것: `ArtPipeline/Archive/73-abyss/rejected/`(2026-10-16 Round 73의 거절된 군주 1차 둘. 이 세션의 것이 아니어서 그대로).
