# 그림체 전환과 UI 라운드 (Round 12~31)

Snapshot: 2026-10-05 — 마지막 지시 "이대로 두자. 세션 정리"(얼굴 빼기의 이유를 듣고 그대로 두기로). 그 전 "1.2.3. 모두 권장안대로"(리팩토링 검토의 정리 R1, 얼굴 빼기, 문서 줄이기).
세 번째 세션 정리에서 Round 31과 이것들을 주제별로 커밋하고 `origin/feature/slice-a`에 푸시했다. 라운드마다의 상세(지시·목업·판정·구현·검증)는
`ArtPipeline/Archive/<폴더>/README.md`와 Git(커밋 `dcfaff4`의 이 파일, 줄이기 전)에 있다. 이 파일은 2026-10-05에 Template으로 줄였다.
2026-10-05 10단계(소리)를 시작하며 `completed/`로 옮겼다. 남은 미결과 "직접 플레이로 볼 것"은 이 파일의 Open과 Roadmap "9단계 뒤의 그림 라운드"의 "남은 것"이 가진다.

## Goal

캐릭터와 몬스터의 그림체가 사용자가 확정한 새 그림체(손으로 그린 흔들리는 잉크선과 낙서 획, 부푼 과장, 직업마다 다른 체형, 매력적인 캐리커처 얼굴, 시선은 적 쪽)로 바뀌고,
색은 지금의 평면·낮은 채도 그대로다. 확정된 그림만 `Assets`에 들어가고 체인이 통과한다. 직업 하나에 스프라이트 여럿을 둘 수 있는 데이터 모양이 정해진다.

## State

- 그림체 전환(Round 12)과 그 뒤의 그림·UI 라운드 13~31을 모두 구현했고 체인을 통과했다. 라운드의 목록과 결과는 Roadmap "9단계 뒤의 그림 라운드"의 표.
- Goal 가운데 남은 것: 스프라이트 여럿의 데이터 모양(【미결】, Open).
- 2026-10-05 리팩토링 검토의 권장안(정리 R1, 얼굴 빼기, 문서 줄이기)을 했다. 얼굴 빼기는 사용자가 이유를 듣고 "이대로 두자"로 확인했다. 체인·스크린샷 통과, 커밋·푸시됨.
- 유료 호출 누계 212회, 약 $5.40(상한 $10, 장부 `ArtPipeline/Archive/calls.csv`). Round 30 뒤로 호출 없음.
- 다음 단계는 Roadmap의 10(소리, G11 먼저). 시작 지시를 받은 뒤에.

## Done

| Round | 무엇 | 기록 (`ArtPipeline/Archive/`) |
|---|---|---|
| 12 | 그림체 전환과 일괄 승인의 재생성(직업 셋, 적 다섯, 배경, UI, 아이템 스물한 개), 플레이 피드백(외곽선, 보스 150%) | `12-roar-style` |
| 13·14 | UI 꾸미기(야영지 장비) → 디아블로 컨셉 UI(타들어 가는 양초 `CandleView`, 뼈색 칸, 돌 패널, 비네트) | `13-ui-decor`, `14-ui-diablo` |
| 15 | 개정 6(최대 7칸, 칸 180x60) | `15-seven-cells` |
| 16·17·18 | 촛불 빛(반원), 외곽선 1/2과 몬스터 눈동자, 쿨다운 빛(촛불 금빛) | `16-candle-light`, `17-outline-pupils`, `18-cooldown-light` |
| 19~22 | 공격·피격 모션 시험과 정보칸 제자리, 진지한 표정, 쓰러진 용병의 무덤(2안-B), 성기사의 머리와 머리 크기의 띠 | `19-motion-test`, `20-serious-face`, `21-grave`, `22-paladin-head` |
| 23·24·25 | 용병 여섯의 공격·피격 자세와 연결, 아이템 분류 다섯과 발동할 때의 움직임, 외곽선 2/3 | `23-motion-six`, `24-support-motion`, `25-outline-thin` |
| 26~28 | 몬스터의 공격·피격 자세와 연결, 주술사의 등불 지팡이, 공격하는 유닛을 맨 앞에 | `26-monster-motion`, `27-monster-poses`, `28-shaman-weapon` |
| 29·30 | 유닛의 발밑 표시(명패 없음)와 보드 머리 띠, 적의 결정타(0.5초 25%, 어둠, 줌 1.1배) | `29-plates`, `30-kill-moment` |
| 31 | 그림의 압축: 자세의 파일 2048x1024(BC7), `ImportPolicy.Compressed`. 그림 묶음 28.6 → 17.9MB, 자세의 메모리 227 → 59MB | `31-texture-compression` |
| — | 리팩토링 검토, 정리(R1), 얼굴 빼기, 문서 줄이기 | 아래 절 |

## 리팩토링 검토 (2026-10-05)

- 검토: "다 한 다음 코드 리펙토링 검토 수행해줘(지금 한번 할 필요가 있는지)" → 큰 리팩토링은 필요 없다. Architecture 불변조건 위반이 없고(Gameplay에 Unity 참조 없음,
  Manager 조회·EventBus 없음), Domain은 10/01 뒤로 바뀌지 않았고 Test와 시뮬이 덮는다(EditMode 1초, PlayMode 약 4분). 몰린 곳은 `BattleScreen`(349 → 1028줄, 6일):
  결정타 약 250줄이 아홉 군데에 흩어졌고 `Render`·`ArrangeColumns`의 붙잡기와 그리는 순서를 Round 21·28·30이 매번 함께 고쳤다.
- 판정: "1.2.3. 모두 권장안대로". 얼굴 빼기는 지난 보고에 권장안을 적지 않아 작업을 시작할 때 밝혔고, 사용자가 이유를 듣고 "이대로 두자". 한 것:
  - **정리 R1**(동작 변화 없음): 참조 없는 `UiBuild.Grid`, `UiText.Row`, `UiPalette.PartyTarget`, `PartySideView.Columns` 삭제. 낡은 주석(`BattleUnitView`·`FigureView`의 자세는 몬스터도,
    `CandleView`는 `Time.time`이라 결정타 동안 느려짐, `PartySideView`의 명패). 이름 `PartyColumnView._info`(전 `_card`), `UiPrefabSetup.Kit.MarksGap`(전 `PlateGap`),
    Test `Battle_WhenARowEmpties_TheUnitsBehindMoveForward_AndTheDeadAreNamed`, 번역 메모 둘(`Board.Hp`, `Battle.Hp`). `UiPalette.Party`·`Enemy`·`Line`은 Python 도구가 이름으로 읽어 남겼다.
  - **얼굴 빼기**(권장안: 화면에 쓰지 않는데 원정마다 읽었다): 그림 열하나(`Assets/@Art/Face`), `ArtAddress.FaceOf`, `JobData`·`EnemyData`의 `Face`, `ExpeditionArt`의 읽어 두기와 `FaceOf*`,
    `ArtSetup`의 `Face` 정책, Test 셋, Architecture/01·04·05·12·13. `cutface.py`와 `Rosters`의 `FaceDx`·`FaceDy`, Archive의 원본은 남겼다(되살리는 법은 Architecture/13 "얼굴").
  - **문서 줄이기**: Roadmap 174 → 69KB(날짜별 갱신 기록과 라운드 절을 표로), 이 Handoff 77KB → Template(200줄 아래).
- 남긴 것(권장): **R2·R3**는 다음에 전투의 쓰러짐·결정타·소리를 고치는 라운드의 첫 커밋으로 — 결정타를 작은 클래스(`UI/KillMoment.cs`)로 옮기고(지켜야 할 순서:
  Play → Place → Arrange → RenderKillMoment, PlayMode Test가 덮음), 적이 쓰러지는 연출을 한 곳으로(결정타에 떨림이 없는 차이는 유지). R4(선택): `BattlePresenter.KillingBlowAt`을
  `BattleLog`로, EditMode Test와 함께. R5(`BuildBattle` 229줄 나누기)·R6(속도 버튼 수 3, 포션 띠, 열 폭 식의 중복)은 그 줄을 고칠 때만. 하지 않음: Domain 나누기, `gen_image.py` 재구성, 콜백을 위한 인터페이스·이벤트.

## Open

- **직접 플레이로 볼 것**(모두 구현됨. 움직임은 스크린샷으로 다 볼 수 없다). 바꿀 때 고치는 곳:
  - 결정타(R30): 0.5초의 느림과 어둠·줌, 한 전투에 여러 번일 때의 피로, x2에서의 길이 — `BattleScreen`의 `Kill*` 상수. 결정타 동안 `Time.timeScale`이 0.25이고 화면이 닫히면 `OnDisable`이 되돌린다.
  - 발밑 표시(R29): 판 없는 글의 읽힘(외곽선 0.3), 머리 띠의 이름(14)과 몬스터의 붉은 글, 48 내린 무대·46 내린 파티 — `UiPrefabSetup.Kit`의 `Marks*`·`BoardHead*`, `Battle`의 `BattleFieldTop`.
  - 자세(R23·R28)와 지원의 맥동(R24): 자세가 보이는 시간, 대기 그림으로 돌아올 때 튀는지, 절반 번쩍임, 뒤 열 공격의 맨 앞 그리기 — `BattleUnitView.PoseExtra`, `FigureView`의 `Pulse*`, `UiPrefabSetup.Battle`의 `PulseGlow*`.
  - 외곽선 2/3(R25): 어두운 옷·밝은 바닥에서 배경과 구별되는지, 보스(150%)의 인상 — `gen_image.py`의 `FIGURE_OUTLINE`(전신 `--refit`, 자세 `fit_pose.py`. 호출 없음).
  - 쿨다운 빛(R18): 어둠 85%에서 아이콘이 읽히는지, 금빛 22%와 앞머리 — `BattleItemView.ChargeShade`, `UiPrefabSetup.Kit`의 `ChargeTint`·`Front*`.
  - 촛불 빛(R16)과 양초(R14): 빛의 흔들림, 폭풍에 꺼지는 0.4초, 세기 — `UiPrefabSetup.Battle`의 `Light*`, `CandleView`의 상수. 폭풍은 전투 시작 후 `StormStartMs`.
  - 새 그림체의 간격·겹침·키(190 간격에서 넓은 자세가 더 겹친다), 보스의 1.5배(`EnemyData.FigureScale`, 권고안), 디아블로 UI의 세부(비네트, 빈 칸 알파, 양초 크기).
    아이템·UI·배경은 선만 바꾸고 색·크기·구조는 그대로라, 어울리지 않는 곳이 보이면 그 타입만 다시 그린다.
- **스프라이트 여럿【미결】**(Goal에 남은 것): 권장안은 용병의 `Figure` 선택 열(`MercenaryData`. 비면 직업의 그림)과 소재의 `Variant` 열. 설계안을 보여 준 뒤 Design/05·07,
  Architecture/05·12·13, 데이터·코드를 바꾼다. 세트(`roar`, `charm`, `pretty`)는 `Archive/12-roar-style`에 변형용으로 있다. 용병을 크게 늘리면 전신 캔버스를 2의 거듭제곱으로 하는 것(Round 31의 안 D)을 함께 정한다.
- **키와 폭【미결】**: 넓은 자세(양손 무기)는 3:4 캔버스에서 키가 14~24% 작다. 지금은 체형 차이로 둔다(권고안). 사용자가 화면을 보고 캔버스·그림 자리를 넓힐지 정한다.
- **남은 무압축과 또렷함**(Round 31): 전신(11장, 메모리 약 35MB)·UI·아이콘·포션은 무압축이다. Mipmap을 끈 그림이 지금보다 훨씬 또렷하다(`Archive/31-texture-compression/compare-sheet.png`):
  지금은 1080p에서 1/4 해상도의 Mipmap을 키워 그린다. 바꿀지는 움직임 목업(MP4)으로 본다. 에디터의 품질 단계가 Very Low이고 빌드는 Ultra다(1080p에서는 같고, 4K에서 에디터·스크린샷이 더 흐리다).
- **개정 6의 아이콘**: 칸이 60으로 높아져 아이콘의 위아래 여백이 는다. 작아 보이면 스물한 개를 새 캔버스(328x100·224·348)로 다시 그린다(호출 21회, 약 $0.5. 요청 시).
- **Deferred UI 틀**: 인벤토리 팝업·보상 카드·타이틀·로비·정산의 틀은 도형 그대로다. 요청이 있을 때 같은 컨셉으로 그린다.
- 되돌리기: 이전 그림체의 그림은 `Archive/01-characters/approved`, `02-enemies/approved`, `03-items/approved`, `04-backgrounds`, `05-ui`, `09-battle-ui-feel`에 있다.

## Verification

- 정리(R1)와 얼굴 빼기(2026-10-05): `Tools/chain.sh` setup OK(Addressables `F1-Art`에서 얼굴 항목 열하나가 빠짐, `NodeMapScreen`·`RewardScreen` Prefab은 `_card` → `_info`만,
  `F1_UI_Static Shared Data`는 번역 메모 둘만, 스탬프), sim OK, EditMode 625/625(얼굴 Test 셋을 지움), PlayMode 56/56(`[Explicit]` 스크린샷 8개 제외). `Tools/screenshots.sh` 45장(8/8): 움직임이 없는 화면 열 장(타이틀, 로비, 정산 등)은 Round 31 때와 픽셀까지 같고, 노드 맵·보상의 파티 쪽(이름을 바꾼 `_info`)이 그대로 보인다. `git diff --check` 깨끗.
- 그림의 압축(Round 31, 2026-10-05): 바꾸기 전의 도구로 다시 맞춘 자세 22장이 게임 파일과 픽셀까지 같았고, 새 2048x1024 22장은 게임 파일의 64/63배(LANCZOS)와 픽셀까지 같다.
  체인 통과(EditMode 628/628, PlayMode 56/56), 스크린샷 45장(8/8), Addressables 묶음 `f1-art` 28.57 → 17.88MB(복제본 빌드).
- 앞선 라운드는 모두 체인과 스크린샷을 통과했다(각 라운드 README의 검증, Git `dcfaff4`의 이 파일).

## 알아둘 것

- 자세처럼 얼굴 일관성이 필요한 재생성은 포스터가 아니라 **그 캐릭터의 확정 그림**을 `--reference`로 붙이고 시험 문서(§3 "같은 캐릭터, 자세만")로 돌린다. 자세는 `fit_pose.py`로 맞춘다.
- 소재는 체형(등신 포함) → 머리·옷·무기 → 자세 → 얼굴의 순서로 적으면 모델이 잘 따른다. 세트 폴더마다 스타일 문서와 소재가 있어 그 변형으로 더 그릴 수 있다(`--style`, `--roster`, `--name <key>_<set>`).
- 올린 포스터는 저작권이 있는 그림이라 저장소에 넣지 않았다. 생성은 우리 그림의 시트를 기준 그림으로 쓴다.
- 새 런의 시드는 OS에서 와서 첫 전투의 적이 찍을 때마다 바뀐다. 같은 장면을 비교하려면 같은 시드로 찍는다(`Archive/29-plates/run_lowered_shots.sh`).
- 커밋하지 않을 것(세션 정리 때마다): Unity가 만든 `Assets/AddressableAssetsData/OSX.meta`, `ProfileDataSourceSettings.asset(.meta)`, `ProjectSettings/ScriptableBuildPipeline.json`.
  목업 MP4는 `.gitignore`가 거른다(다시 만들 때는 그 라운드의 목업 스크립트).

## Next Action (제안)

- 사용자가 Unity에서 새 런으로 직접 플레이하며 Open의 "직접 플레이로 볼 것"을 본다. 바꿀 것이 있으면 위의 고치는 곳에서 고치고 체인·스크린샷으로 확인한다.
- Roadmap "사후 검토 대기"를 검토한다(가장 최근: Round 26~31, 정리와 얼굴 빼기, 문서 줄이기).
- 스프라이트 여럿의 설계안을 보여 주고 승인받은 뒤 구현한다. 키와 폭을 정한다.
- 다음에 전투의 쓰러짐·결정타·소리를 고치는 라운드의 첫 커밋으로 R2·R3(결정타 분리)를 한다.
