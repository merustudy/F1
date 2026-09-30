# F1 Roadmap

단계, 결정 관문, 진행 상태를 소유한다. 규칙은 [CLAUDE.md](../CLAUDE.md)가, 상세는 Owner 문서가 소유한다.
이 문서는 Authority가 아니다.

- 갱신: 2026-09-30 (6단계까지와 7a, 7b 완료. 7c, 7d, 8단계를 일괄 승인으로 진행 중)
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
| G7 | 런과 원정의 저장 시점, 이어하기의 상세 (방향과 파일 구성은 결정됨) | Architecture/07, Architecture/09 Acceptance 7 | 8단계 |
| G9 | 그림체와 컨셉 방향 (기존 안은 미채택) | Design/10 | 9단계 시작 전 |
| G11 | 소리 방향 | 기획문서에 없음, Kit/05 "승인 라운드" | 10단계 시작 전 |
| G17 | 게임 제목(`productName`). Save 경로가 여기서 나온다 | ProjectSettings 현재값 `F1` | 출시 전 |

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
| G7 | 방향: 상태가 확정될 때마다 저장하고, 확정된 손실은 이어하기로 되돌릴 수 없다. 파일은 `settings.json`과 `run.json`, 슬롯은 하나 | CLAUDE.md §6, Architecture/07 |
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

### 8. 저장/이어하기 — 상태: 대기

- 범위: 런 상태와 원정 진행 상태의 저장, 이어하기. Owner 문서는 Save 영역에 추가.
- 완료 기준: Architecture/09의 Acceptance 7번. 저장 실패 시 정책이 Test로 고정됐다. Schema Version과 Migration 골격이 있다.
- 결정할 것: G7.

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
