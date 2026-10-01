# F1 Roadmap

단계, 결정 관문, 진행 상태를 소유한다. 규칙은 [CLAUDE.md](../CLAUDE.md)가, 상세는 Owner 문서가 소유한다.
이 문서는 Authority가 아니다.

- 갱신: 2026-10-01 (1~8단계 완료. 그 뒤 Slice A를 세 번 개정했다. 개정 1: 열 1~4열, 한 열에 한 유닛, 4인 파티, 전투 화면 개편(커밋·푸시됨).
  개정 2: 쓸 수 있는 자리를 앞이나 뒤에서 세는 상대 범위로, 아이템 크기(1~3칸), 원정 인벤토리. 개정 3: 노드 맵·보상 화면의 파티를 전투와 같은 열로,
  적 정보 숨김, 포션 자리 통일, 인벤토리 팝업. 개정 4: 인벤토리 10칸. 개정 2~4는 구현, 검증, 커밋이 끝났고 사후 검토를 기다린다.
  그 뒤 개정 1~4의 코드 리뷰에서 나온 5건을 반영했다(커밋·푸시됨) — 규칙과 수치는 그대로. 상세는 Handoff "리뷰 반영")
- 상태 값: 대기 / 진행 / 완료
- 단계 번호는 고정이다. CLAUDE.md와 대화가 번호로 가리킨다.
- 모든 단계는 CLAUDE.md §10 "단계 절차"를 따른다. 한 번에 한 단계만, 다음 단계는 시작 지시를 받은 뒤에.
- 문서 위치 표기: `Design/NN` = `Docs/Design/NN_*.md`, `Kit/NN` = `Docs/StarterKit/NN_*.md`, `Architecture/NN` = `Docs/Architecture/NN_*.md`.

## 일괄 승인 (2026-09-30)

사용자가 Slice A 완료(8단계)까지 "권장안대로 모두 구현"을 승인했다. 이 범위에서는 CLAUDE.md §10 단계 절차의 3, 6, 7을 아래로 대체한다.

- 3~8단계는 단계별 승인 없이 이어서 진행한다.
- 미결 결정 관문은 권장안으로 닫고, "결정됨" 표와 Owner 문서, 기획문서에 기록해 사후에 검토받는다.
  기획문서에는 `【확정】 (2026-09-30 일괄 승인)`으로 적는다. 사용자가 바꾸면 기획문서를 먼저 고친다.
- 단계가 끝날 때마다 주제별로 로컬 커밋한다. 푸시는 하지 않는다. 작업 브랜치는 `feature/slice-a`다.
- 9단계부터는 다시 단계별 승인을 받는다.

### 열 개정의 승인 (2026-09-30 ~ 10-01)

Slice A를 플레이해 본 사용자가 전열/후열을 없애고 열을 다시 정하라고 했다. 지시는 세 번이었다.

1. 1열/2열/3열로: 한 열에 한 명, 앞 열이 비면 전진, 여럿을 맞히는 공격 범위를 지금 추가. 나머지는 "권장안대로 구현".
2. 이어서 4열로: "4열로 확장", "몬스터도 1열에 1몬스터", 공격 범위는 "뒤에서 몇 번째까지" 또는 "앞에서 몇 번째까지".
   빠진 부분은 "잘못된 부분이 있다면 네 권장안으로 구현".
3. 다음 날 "용병도 4열까지 확장 구현": 파티가 4인까지다(`PartySize` 4). 적은 4인 파티에 맞춰 권장안으로 다시 맞췄다.

- 이 개정 범위 안에서는 단계 절차의 3(설계 승인)을 위 지시로 대체했다. 사용자가 직접 정한 것은 기획문서에 `【확정】 (열 개정)`,
  권장안으로 정한 세부는 `【확정】 (열 개정 권장안)`으로 적고 아래 "사후 검토 대기"에 올렸다.
- 개정분은 2026-10-01 사용자 지시로 주제별로 커밋하고 `origin/feature/slice-a`에 푸시했다(단계 절차 6).

### 개정 2의 승인 (2026-10-01)

열 개정의 사후 검토 보고(조합 절벽의 선택지 "다"의 상세, 아이템 크기의 선택지 A~G)에 사용자가 답했다.

1. 조합 절벽: 선택지 "다"(쓸 수 있는 자리를 앞이나 뒤에서 센다)를 채택하고, **패시브의 자리 조건까지** 같은 셈법으로 바꾼다.
2. 아이템 여러 칸: 채택. 자리가 모자랄 때(C)는 제시한 선택지 대신 **원정 인벤토리**를 두고 보드에서 밀려난 아이템이 거기로 빠지는 방식을 새로 정했다.
   그 외(A1 크기 1~3과 칸 수 늘림, B2 목록+용량 보드, D, E1, F, G)는 "권장안 반영".
3. 플레이테스트는 지금 할 수 없으므로 추후.

- 이 개정 범위 안에서도 단계 절차의 3을 위 지시로 대체했다. 사용자가 직접 정한 것은 `【확정】 (개정 2)`, 권장안으로 정한 세부는
  `【확정】 (개정 2 권장안)`으로 적고 아래 "사후 검토 대기"에 올렸다(목록: Design/00 "개정 2").
- 커밋됨 (2026-10-01 세션 정리. 개정 2~4를 문서 / 데이터·Domain·저장·시뮬·Test / 화면 / Roadmap·Handoff의 네 커밋으로).

### 개정 3의 승인 (2026-10-01, 목업으로 결정)

전투와 보드 화면의 장비 표시가 달라 보인다는 지적에서 시작해, 목업 여섯 장으로 다음을 정했다(단계 절차 3을 목업 승인으로 대체).

1. "C안": 노드 맵과 보상 화면의 파티를 **전투와 같은 열**(4열→1열, 그림 자리, 정보 카드, 그 아래 세로 칸)로 왼쪽에 둔다. 전투 화면은 그대로.
   (A안 "전투의 장비를 하단 패널에 가로로"는 보류. 사용자는 "어떤 캐릭터가 어떤 공격을 하는지"가 C안에서 더 분명하다고 봤다.)
2. 노드를 고를 때 **적 무리를 미리 보여 주지 않는다**(기획 【확정】, Design/03 §1).
3. 포션은 세 화면에서 **전투와 같은 자리**(헤더 아래 왼쪽). 인벤토리는 줄 대신 **"인벤토리 보기" 버튼과 세로 팝업**. "모두 권장안대로".

- 권장안으로 정한 세부: 팝업이 오른쪽을 전부 덮는 것, 노드 맵에도 같은 포션 자리와 팝업, 죽은 용병의 열은 비워 두는 것, "인벤토리로" 버튼은 파티 아래.
  아래 "사후 검토 대기"에 올렸다. 화면 규칙은 Architecture/12가 소유한다.
- 커밋됨 (2026-10-01 세션 정리).

### 개정 4의 승인 (2026-10-01)

사용자가 "인벤토리는 아이템칸 10칸으로 제한하자"로 정했다. 개정 2의 권장안 "용량 제한 없음"을 바꾼 것이다.

- 【확정】 (개정 4): 인벤토리는 10칸. 데이터 `BalanceData.InventoryCells`(Design/03 §5).
- 권장안으로 정한 세부(아래 "사후 검토 대기"): 칸은 아이템의 크기만큼 센다(10개가 아니라 10칸), 들어갈 자리가 없으면 받기·빼기·밀려나기가 모두 막힘,
  팝업 제목에 쓴 칸/전체 칸, 저장 버전은 그대로(넘치면 무효). 목록: Design/00 "개정 4".
- 커밋됨 (2026-10-01 세션 정리).

## 사후 검토 대기 (일괄 승인으로 정한 것)

일괄 승인 범위(3~8단계)와 열 개정에서 권장안으로 정한 결정이다. 하나씩 승인받은 것이 아니므로 사용자가 검토하고 바꿀 수 있다.
바꿀 때는 아래 위치의 문서를 먼저 고친다.

| 무엇 | 어디에 적혀 있나 |
|---|---|
| Slice A의 게임 규칙 전부 (전투, 원정, 로비, 귀환 정산) | Design/00 "일괄 승인으로 정한 규칙"과 그 표가 가리키는 절 |
| 콘텐츠와 수치 (직업, 아이템, 적, 던전, 용병, 상수) | `Assets/@Data/Source/*.csv`, 시뮬 결과는 Design/08 §5 |
| 화면 구성과 조작 방식 | Architecture/12, `Docs/Workstreams/completed/stage07d-ui.md` "결정" |
| 저장 시점, 이어하기 방식, 저장 실패 정책 | Architecture/07 "run.json" |
| Font(Pretendard), Package(Addressables 2.x, Localization, Newtonsoft) | Architecture/03, 06 |
| 구조 결정 (Manager 의존, Scope, 단계 계산, Prefab을 코드로 생성) | Architecture/02, 04, 11, 12 |
| 열 개정의 세부 규칙 (깊이보다 적이 적을 때, 전진이 적용되는 곳과 순서, 자리 바꾸기, 로비의 넣기와 빼기) | Design/00 "열 개정"의 "권장안으로 정한 세부" (쓸 수 있는 열의 셈법과 패시브의 열은 개정 2가 대체했다) |
| 열 개정의 콘텐츠와 수치 (아이템의 범위, 새 아이템 `spear`와 `halberd`, 4인 파티에 맞춘 적 무리의 구성, 보스 HP) | `Assets/@Data/Source/*.csv`, 시뮬 결과는 Design/08 §6(3인), §7(4인) |
| 열 개정의 화면 (열 버튼, 앞으로와 뒤로, 전투의 열마다 한 장씩 서는 카드, 죽은 유닛의 카드를 치우는 것) | Architecture/12 "열을 보여 주는 방식", "전투 화면" |
| 열의 수와 "한 열에 한 유닛"을 데이터가 아니라 구조로 둔 것 | Architecture/08 "자리 (열)" |
| 규칙만 바뀌고 DTO의 모양이 같을 때는 저장 버전을 올리지 않는 것 | Architecture/07 "Schema Version" |
| 개정 2의 세부 규칙 ("뒤에서 N번째까지"를 살아 있는 줄로 세는 것, 죽음마다 전원 재판정, 패시브 판정 시점, 보드가 순서 있는 목록인 것, 넣고 빼는 규칙) | Design/00 "개정 2"의 "권장안으로 정한 세부" |
| 개정 2의 콘텐츠와 수치 (보드 4칸, 아이템 크기와 계수, 적 궁수의 활 계수 90) | `Assets/@Data/Source/*.csv`, 시뮬 결과는 Design/08 §8 |
| 개정 2의 화면 (보드의 칸과 크기, 인벤토리 줄, 누르고 누르기 조작, 보상의 "인벤토리에 넣기") | Architecture/12 "아이템 보드와 인벤토리를 보여 주는 방식" |
| 저장 버전 3과 2 → 3 변환(옛 보드의 빈 칸을 빼고 빈 인벤토리를 더한다) | Architecture/07 "Schema Version" |
| 개정 3의 세부 (인벤토리 팝업이 오른쪽을 전부 덮음, 노드 맵의 포션 자리와 팝업, 죽은 용병의 열을 비움, "인벤토리로" 버튼 자리, 열 순서 4열→1열) | Architecture/12 "파티 쪽" |
| 적 정보를 숨기는 정도 (지금은 전부 숨김. 수만, 종류만, 싸워 본 무리만은 플레이테스트 뒤) | Design/03 §1 |
| 개정 4의 세부 (인벤토리의 칸을 아이템 크기로 세는 것, 자리가 없을 때 막히는 명령, 팝업 제목, 저장 검증, 상수의 하한) | Design/00 "개정 4", Design/03 §5, Architecture/12 "인벤토리 팝업" |
| 조합 절벽의 나머지 선택지 (무기의 자리, 뒤 열용 보상, 지팡이 직업) — 플레이테스트 뒤 | Design/08 §7, §8 |

화면은 `Tools/screenshots.sh`로 PNG를 뽑아 볼 수 있다. 직접 플레이하려면 Unity에서 `Assets/@Scenes/Boot.unity`를 열고 Play한다.

## 진행 순서

```text
1 -> 2 -> 3 -> 4 -> 5 -> 7a -> 7b -> 6 -> 7c -> 7d -> 8 -> 9 -> 10 -> 11
```

- 7단계는 넷(7a 규칙 명세, 7b Domain과 시뮬, 7c Application, 7d UI)으로 나눠 각각 승인받는다.
- 7a와 7b를 6단계(언어)보다 먼저 한다. 규칙을 다시 정하므로 시뮬이 빨리 있어야 하고, 순수 C# Domain은 언어 작업에 의존하지 않는다.
- 3, 4, 5, 6단계는 기획 내용에 의존하지 않는다. 기획이 바뀌어도 다시 하지 않는다.

## 결정 관문 — 미결

"늦어도" 열의 시점 전에 정한다. 정해지면 내용을 해당 Owner 문서(또는 CLAUDE.md)로 옮기고 아래 "결정됨" 표로 옮긴다.

| # | 미결 결정 | 근거 문서 위치 | 늦어도 |
|---|---|---|---|
| G9 | 그림체와 컨셉 방향 (기존 안은 미채택) | Design/10 | 9단계 시작 전 |
| G11 | 소리 방향 | 기획문서에 없음, Kit/05 "승인 라운드" | 10단계 시작 전 |
| G17 | 게임 제목(`productName`)과 Bundle Identifier. Save 경로가 여기서 나온다. 바꾸면 그 전의 Save를 찾지 못한다 | 현재값 `F1`, `com.funitup.f1` (`ProjectSetup`), Architecture/07 | 출시 전 |

## 결정 관문 — 결정됨 (2026-09-30)

| # | 결정 | 기록 위치 |
|---|---|---|
| G0 | 원본 자료(기획 v0.5.x, `sim.py`, `build_data.py`, `prototype_data.json`, `lobby_sim`)는 없다. 규칙을 다시 정하고 시뮬을 새로 만든다. 기존 수치와 시뮬 결론은 재검증 전까지 출발값과 목표다 | Design/00 |
| G1 | CSV 원천 -> DataTransformer -> Generated JSON (Kit/02의 파이프라인). 하위 결정은 G14 | CLAUDE.md §3, Design/09 §3 |
| G2 | C# Domain이 기준 구현이다. 시뮬은 같은 Domain 코드를 Unity 밖에서 돌린다 | CLAUDE.md §3·§5, Design/09 §2 |
| G3 | 포션 슬롯과 후퇴를 도입하고 Slice A에 넣는다. 세부는 G16 | Design/02 §10 |
| G4 | Slice A = 핵심 루프 한 바퀴 + 저장/이어하기 | Architecture/09 |
| G5 | Locale은 `ko-KR`(기본/Fallback)과 `en-US` | CLAUDE.md §3 |
| G6 | Application 계층 Manager는 `RunManager`(런)와 `ExpeditionManager`(원정과 전투) | CLAUDE.md §4, Architecture/02 |
| G7 | 상태를 바꾸는 명령마다 저장하고, 확정된 손실은 이어하기로 되돌릴 수 없다. 파일은 `settings.json`과 `run.json`, 슬롯은 하나. 전투 도중은 입력 기록과 확정 시각으로 재현한다. 저장 실패는 다음 변경을 막는다 (저장 시점과 실패 정책은 일괄 승인으로 정함) | CLAUDE.md §6, Architecture/07 |
| G8 | 결정론 세부: 정수 밀리초, 고정된 처리 순서, 용도별 PCG32 스트림, 정수 수치, 이벤트 로그 재생 | Design/09 §5, Design/02 §3 |
| G9 | 다크 중세 흉상, 대규모 로스터, 소영주·공성전은 채택하지 않는다 | Design/10 |
| G10 | Git LFS를 쓴다. TextMeshPro 기본 리소스(ttf, png)가 3단계에 들어오므로 3단계 시작 때 설정했다 | `.gitattributes` |
| G12 | Prefix는 `F1` | CLAUDE.md |
| G13 | 대상 OS는 Windows와 macOS. `companyName`은 `funitup`. 기준 해상도 1920x1080(16:9), 기본은 전체 화면 창, 창 크기 조절 가능 | `ProjectSetup`, Architecture/02 |
| G14 | 식별자는 영어 snake_case 문자열 `Id` 하나(정수 Id 없음), 조회는 `Dictionary<string, T>`. 변환 코드는 Unity 비의존이고 `Tools/Sim`이 같은 코드로 돌린다 | Architecture/05 |
| G15 | Resource Scope는 `App`, `Lobby`, `Expedition`. 기획의 "런"은 Scope가 아니다 | Architecture/04 |
| G16 | Slice A의 규칙을 권장안으로 정했다. 목록은 Design/00 "일괄 승인으로 정한 규칙" (사후 검토 대상) | Design/02~05, 07, 09 |

## 단계

완료 기준은 초안이다. 각 단계의 설계 승인 때 확정한다.

### 1. CLAUDE.md와 git 설정 — 상태: 완료

- 산출: root `CLAUDE.md`, 이 문서, `.gitignore`/`.gitattributes` 병합, `Docs/Workstreams`, 결정 사항의 기획문서 반영.

### 2. Slice 범위 문서 — 상태: 완료

- 산출: `Docs/Architecture/09_VERTICAL_SLICE.md`. Slice A의 구간별 범위, 구현 순서, Acceptance, Deferred, 규칙 정의가 필요한 항목.
- 권장안(성장·경제·유물·기한 판정 제외, 로비 행동은 쉬기 하나)대로 승인됐다.

### 3. 기반 — 상태: 완료

- 산출: Boot/Main Scene(`SceneSetup`이 코드로 생성), `AppRoot`, `Managers`, `SceneManagerEx`, asmdef 4개, EditMode Test 4개,
  PlayMode Test 3개, `Tools/chain.sh`, `ProjectSetup`(Player 설정). Owner 문서 Architecture/01, 02, 03, 10.
- 함께 한 것: Template의 쓰지 않는 Package 11개 제거, TMP Essential Resources 반입, Git LFS 설정, `SampleScene` 삭제.
- Domain의 Unity 비의존은 asmdef 분리가 아니라 시뮬 실행기가 소스를 직접 컴파일하는 것으로 보장한다(Architecture/03). 실행기는 7b에서 만든다.

### 4. ResourceManager + Addressables + Save/Settings — 상태: 완료

- 산출: Addressables 2.11.2와 Newtonsoft JSON 3.2.2 도입, `ResourceManager`(Logical Address, Scope), `AddressablesSetup`(Entry 목록 동기화와 검사),
  `SaveManager`와 `SaveStorage`(Atomic Write, 한 세대 Backup, 손상 복구), `SettingManager`와 `settings.json`, `LocalePolicy`.
  Owner 문서 Architecture/04, 07.
- Boot 단계 추가: Save 저장소 초기화, 설정 Load, Resource 초기화. 실패하면 그 단계의 Error Code를 보이고 멈춘다.
- Test는 임시 Save Root를 쓴다.

### 5. 데이터 — 상태: 완료

- 산출: CSV -> `StaticDataTransformer` -> Generated JSON -> Addressables -> `DataManager` -> `StaticData`를 `JobData.csv` 하나로 관통.
  `CsvTable`/`CsvRow`(엄격한 Parser와 형 변환), `StaticDataLoader`, `StaticDataFiles`, `LocalizedText`. Owner 문서 Architecture/05.
- `Tools/Sim`(.NET 콘솔): Unity 없이 `transform`, `validate`. 순수 C# 폴더를 직접 컴파일하므로 그 폴더에 Unity 참조가 생기면 Build가 깨진다.
- 체인에 `sim` 단계 추가. Boot 단계 BOOT-06(Static Data Load) 추가.
- 첫 데이터는 직업 6종의 id와 이름이다. 나머지 열과 Definition은 규칙 명세(7a) 뒤에 7b에서 만든다.

### 6. 언어 — 상태: 완료

- 산출: Localization 1.5.13 도입, `UI_StaticText.csv` -> `LocalizationSetup.Sync` -> `F1_UI_Static` String Table(두 Locale),
  `UiKeys`와 `UiStrings`, `UnityLocaleAdapter`, `SettingManager.ChangeLocaleAsync`(Runtime 적용 -> 저장 -> 알림, 실패하면 되돌림),
  Pretendard Medium(SIL OFL 1.1)과 `FontSetup`이 굽는 Static Atlas. Owner 문서 Architecture/06.
- Boot 단계 추가: BOOT-07(Localization 초기화), BOOT-08(저장된 Locale 적용). Boot 화면은 Localization 없이 동작한다.
- 결정: UI String CSV에 `Id` 열을 두지 않는다(Key로만 참조). Font Atlas는 화면에 나올 글자만 굽고, 글자 집합이 바뀔 때만 다시 굽는다.
- Test: CSV와 Table 일치, `UiKeys`와 CSV Key 일치, Font Family와 Glyph Coverage, Locale 전환과 저장.

### 7. 게임 루프 (Slice A)

범위와 Acceptance는 Architecture/09가 소유한다. 그림은 도형 Placeholder다.
7d가 끝나기 전에는 유료 이미지/소리 API를 호출하지 않는다.

#### 7a. 규칙 명세 — 상태: 완료

- 산출: Slice A에 필요한 규칙을 권장안으로 정해 `Docs/Design` 02, 03, 04, 05, 07, 09에 `【확정】 (일괄)`로 적었다.
  검토용 목록은 Design/00 "일괄 승인으로 정한 규칙".
- 수치와 콘텐츠는 문서가 아니라 CSV에 둔다(7b에서 넣는다). 전부 출발값이고 시뮬로 조정한다.

#### 7b. Domain과 시뮬 — 상태: 완료

- 산출: Slice A Static Data 9종(CSV와 Generated JSON), 순수 C# Domain(`BattleEngine`, `ExpeditionRules`, `RunRules`, `Pcg32`),
  `Tools/Sim`의 `battle`, `expedition`, `trace` 명령과 입력 정책 셋. Owner 문서 Architecture/08.
- 결정론: 같은 Setup과 입력의 반복 실행, 진행 간격과 무관함, 재현(Replay)이 Test로 고정됐다.
- 시뮬로 출발값을 잡았다. 기본 파티(knight, bishop, spellblade)의 짧은 던전 클리어율은 balanced 정책에서 81.7%, 원정당 사망 0.22명.
  결과와 관찰(화상 절벽 포함)은 Design/08 §5.

#### 7c. Application — 상태: 완료

- 산출: `Assets/@Scripts/Flow`의 `RunManager`(새 런, 로비 명령, 귀환 정산 적용), `ExpeditionManager`(출발, 노드, 전투 세션, 보상, 보드, 정산 보고),
  `BattleSession`, `BattleClock`(배속과 일시정지), `GamePhase`. Owner 문서 Architecture/11.
- 전투가 끝나면 그 호출 안에서 원정에 반영하고, 원정이 끝나면 그 호출 안에서 귀환 정산까지 적용한다. 화면의 확인을 기다리지 않는다.
- Test: UI 없이 루프 한 바퀴와 두 번째 출발, 전멸과 후퇴, 런 종료, Frame 길이와 무관한 결과(`GameLoopTests`).
  출고 데이터로 Boot 뒤 원정 한 번을 끝까지 돌리는 PlayMode Test.

#### 7d. UI — 상태: 완료

- 산출: `UIManager`, 화면 여섯(타이틀, 로비, 노드 맵, 전투, 보상, 정산)과 View, 화면 Prefab을 만드는 Builder(`Editor/Setup/Ui`),
  Addressables Entry, UI 문구 128개(두 Locale), `Tools/screenshots.sh`. Owner 문서 Architecture/12.
- 그림은 전부 색 사각형이다. 유료 생성 API는 쓰지 않았다.
- 화면 구성은 목업 선택 없이 권장안으로 정했다(일괄 승인). `Tools/screenshots.sh`로 뽑은 PNG로 사후 검토한다.
- Acceptance(저장 7번 제외): 1~6, 8~12를 Test와 스크린샷으로 확인했다. 사람이 직접 플레이한 확인은 아직 없다(11단계).
- 함께 한 것: 직업 패시브 설명문(`JobData.csv`의 `PassiveText`), 이벤트 로그에서 사망 원인을 읽는 `BattleLog.PartyDeaths`,
  Scene과 함께 사라진 Instance의 Handle 처리(`ResourceManager`), Canvas Scaler를 Expand로 바꾸고 Scene 재생성.

### 8. 저장/이어하기 — 상태: 완료

- 산출: `run.json`(런 + 원정 + 전투 기록), `RunSaveData` DTO, `RunSaveMapper`(변환과 검증), `RunSaveMigrator`(Schema Version 골격),
  명령마다 저장, Boot 단계 BOOT-09(런 Load와 원정 복원), 전투 도중 이어하기(`BattleEngine.Replay`), 저장 실패 시 막힘과 재시도,
  저장 실패 Overlay, 타이틀의 Save 상태 알림. Owner 문서 Architecture/07에 상세 추가.
- Acceptance 7: 어느 단계에서 끝내도 이어진다. 저장된 사망과 실패한 후퇴는 다시 시작해도 그대로다(Test로 고정).
- 저장 실패 정책, 읽을 수 없는 파일, 옛 Backup으로 복구, 버전이 다른 파일의 거부가 Test로 고정됐다.
- 남긴 것: 전투 시간의 주기적 저장(하지 않는다. 확정되지 않은 구간은 다시 진행한다), 변조 방지, 여러 슬롯.

### Slice A 개정: 열 (1열~4열, 한 열에 한 유닛, 4인 파티) — 상태: 완료 (커밋됨, 사후 검토 대기)

8단계 뒤에 사용자 요청으로 한 개정이다. 번호가 붙은 단계가 아니고, 1~8단계의 산출물을 고쳤다.

- 규칙: 양쪽 모두 1열~4열이고 한 열에 한 유닛이다. 파티는 4인까지라 넷이면 1~4열에 선다. 앞 열이 비면 뒤 열이 한 칸씩 전진한다.
  아이템마다 쓸 수 있는 열이 있고, 적을 향한 공격 범위는 "앞에서 N번째까지", "뒤에서 N번째까지", 전부다 (Design/02 §2, §4).
- 산출: `Formation`(자리 규칙), `BattleEngine`의 전진과 아이템의 시작·멈춤, 타깃 `EnemyFront`·`EnemyBack`과 깊이(`Reach`),
  데이터의 열(`ItemData.Rows`, `PassiveRows`, 적 무리의 `Enemies`), 로비의 열 버튼과 보드의 앞으로·뒤로, 전투 화면의 열마다 한 장씩 서는 카드,
  `Tools/Sim`의 `formations` 명령. `run.json`은 Schema Version 2이고 1은 읽지 않는다.
- 시뮬: 기본 파티(knight, spellblade, bishop, archmage 순으로 1~4열)는 80.7% / 사망 0.27으로 개정 전과 같은 자리다
  (적 무리를 4인 파티에 맞춰 채웠다). 자리와 조합이 결과를 크게 가른다. 수치와 선택지는 Design/08 §7.
- 화면(2026-10-01 사용자 지시): 전투 중에는 로그를 숨기고, 끝난 뒤 결과 Panel의 "로그 보기"로 전체 로그를 본다.
- 화면(2026-10-01 목업 C안 승인): 헤더 왼쪽에 후퇴, 그 아래 왼쪽에 포션, 상단 여백은 배경 그림 자리, 유닛은 바닥선 위의 전신 그림 자리와
  정보 카드(아이템 보드: 세로 칸, 도형 아이콘, 최대 6칸). 로그 박스와 아래 조작 Panel은 없앴다. Architecture/12 "전투 화면".
- 검증: 체인 통과. EditMode와 PlayMode의 수는 Handoff에 있다.
- 남은 것: 사람이 직접 하는 플레이 확인. 조합 절벽과 아이템 크기는 개정 2로 이어졌다.
- Handoff: `Docs/Workstreams/completed/a-rows.md`

### Slice A 개정 2: 상대 범위, 아이템 크기, 인벤토리 — 상태: 완료

개정 1의 사후 검토에서 사용자가 정한 개정이다("개정 2의 승인"). 번호가 붙은 단계가 아니고, 1~8단계와 개정 1의 산출물을 고쳤다.

- 규칙: 아이템을 쓸 수 있는 자리와 패시브의 자리 조건을 공격 범위처럼 **앞이나 뒤에서 세어** 적는다(`front:N`, `back:N`, `all`).
  "뒤에서 N번째까지"는 살아 있는 줄로 세므로 전투 중에 자리를 잃는 일이 없다(Design/02 §4, §7). 아이템은 크기(1~3칸)만큼 보드를 차지하고,
  보드에서 밀려난 아이템은 **원정 인벤토리**로 간다(Design/03 §5).
- 산출: `RowSpan`(Data), `ItemBoard`(Domain), `ExpeditionRules`의 보드·인벤토리 명령과 `Can...` 질의, `BattleEngine.RefreshSide`,
  `run.json` Schema Version 3과 `From2To3`, 파티 보드의 칸과 인벤토리 줄, 보상 화면의 "인벤토리에 넣기", 전투 카드의 크기만큼 칸,
  문구 10개, `Tools/Sim` 정책의 인벤토리 사용. 데이터: 보드 4칸, `longbow`·`spear` 2칸, `halberd` 3칸, `crude_bow` 90.
- 시뮬: 기본 파티 balanced 81.2% / 사망 0.26(개정 전 80.7% / 0.27). 하락분은 전부 상대 범위에서 왔고 적 궁수의 활 계수 100 → 90으로 맞췄다.
  아이템 크기와 인벤토리는 짧은 던전에서는 중립이다. 조합 절벽은 예측대로 그대로다. 수치와 선택지는 Design/08 §8.
- 검증: 체인 통과. EditMode와 PlayMode의 수는 Handoff에 있다.
- 남은 것: 사람이 직접 하는 플레이 확인(개정 1과 같이 추후), 조합 절벽의 나머지 선택지(플레이테스트 뒤), 큰 아이템과 인벤토리의 값은 긴 던전에서.
- Handoff: `Docs/Workstreams/active/a-rows-items.md`

### Slice A 개정 3: 화면 통일(C안)과 적 정보 숨김 — 상태: 완료

개정 2 직후 사용자가 목업으로 정한 개정이다("개정 3의 승인"). 화면만 바뀌고 규칙은 하나(적 정보 숨김)만 바뀐다.

- 산출: `PartySideView`·`PartyColumnView`·`InventoryEntryView`(옛 `PartyBoardView`·`BoardMemberView`를 대체), 노드 맵과 보상 화면의 Builder,
  전투 Builder의 그림 자리 공용화(`BuildFigure`), 문구(적 정보 안내, 인벤토리 보기·닫기·제목·안내), Design/03 §1, Architecture/12.
- 검증: 체인 통과. PlayMode의 파티 쪽 Test 둘(앞으로·뒤로, 인벤토리 팝업)을 새 화면으로 다시 썼다. 수는 Handoff에 있다.
- Handoff: `Docs/Workstreams/active/a-rows-items.md` ("개정 3" 절)

### Slice A 개정 4: 인벤토리 용량 — 상태: 완료

개정 3 직후 사용자가 정한 개정이다("개정 4의 승인"). 상수 하나와 그 상수를 지키는 규칙이다.

- 규칙: 인벤토리는 `InventoryCells`칸(10)이고 아이템은 크기만큼 차지한다. 들어갈 자리가 없는 아이템은 인벤토리로 갈 수 없다(Design/03 §5).
- 산출: `BalanceData.InventoryCells`, `ExpeditionRules.FreeInventoryCells`·`CanTakeRewardToInventory`·`CanPickItem`과 인벤토리로 가는 명령의 자리 검사,
  저장 검증 하나, 시뮬 정책(자리가 없으면 넘김), 팝업 제목(쓴 칸/전체 칸)과 꺼지는 버튼. Design/03 §5·00·01, Architecture/07·08·11·12.
- 시뮬: Slice A의 짧은 던전에서는 용량에 닿지 않는다(Design/08 §8 "상태").
- 검증: 체인 통과. 수는 Handoff에 있다.
- Handoff: `Docs/Workstreams/active/a-rows-items.md` ("개정 4" 절)

### 9. 그림 — 상태: 대기

- 범위: `ArtPipeline` 세팅(가상환경, 키체인, 스타일 문서, 레퍼런스), 스모크 1회, 첫 승인 라운드, 승인분 배선(Kit/04).
- 완료 기준: 스타일 문서가 승인됐다. 승인된 그림만 `Assets`에 배선됐고 체인을 통과한다.
- 결정할 것: G9, 그릴 타입 목록, 예산.
- 선행: 7단계 완료.

### 10. 소리 — 상태: 대기

- 범위: 효과음·배경음 생성 파이프라인, 승인 라운드, `SoundManager` 배선(Kit/05).
- 완료 기준: 승인된 소리만 배선됐다. Import 정책이 Setup 코드로 고정됐다. 체인 통과.
- 결정할 것: G11, 예산.

### 11. 플레이테스트 -> 수정 -> Slice B — 상태: 대기

- 범위: Slice A 플레이테스트, 수정, Slice B 범위 문서.
- 완료 기준: Slice B 범위 문서가 승인됐다.
- 결정할 것: Slice B 범위(Architecture/09의 Deferred에서 고른다).
- 사용자 작업: Windows에서 확인하려면 첫 Windows 빌드 전에 Unity Hub에서 Windows Build Support 모듈을 추가한다.
