# 13. Art Pipeline

그림을 만들고, 승인받고, Unity에 넣는 길을 소유한다: `ArtPipeline` 폴더, 스타일 문서, 생성 스크립트, 승인 라운드, 승인된 그림의 배선과 검사.
그림체의 방향은 `Docs/Design/10_Art_Direction.md`, 물리 경로는 `01_PROJECT_STRUCTURE.md`, Address·Group·Scope는 `04_RESOURCES_ADDRESSABLES.md`,
CSV 열은 `05_STATIC_DATA.md`, 화면이 그림을 보여 주는 방식은 `12_UI.md`가 소유한다.

## 원칙

- 그림은 유료 API로 만든다. **요청받았을 때만 호출한다.** 한 번 실행에 호출은 한 번이다.
- 호출 전에 실패할 수 있는 것은 전부 먼저 실패시킨다: 파일, 스타일 문서의 형식, Roster, 기준 그림의 포맷 -> 그다음 키 -> 그다음 호출.
- 사람이 승인한 그림만 `Assets`에 들어간다.
- 스타일 규칙은 문서 한 곳에 있고 스크립트가 실행할 때 읽는다. 스크립트에 스타일 문구를 복사하지 않는다.
- API 키는 macOS 로그인 키체인에만 둔다(서비스 이름 `OPENAI_API_KEY`). 환경 변수, `.env`, 출력, 로그, 예외 메시지에 넣지 않는다.
  키를 등록하는 명령은 사용자가 직접 실행한다.

## 구조

```text
ArtPipeline                    # 저장소 root. Unity가 Import하지 않는다
├─ STYLE_RUNTIME.md            # 스타일 규칙. 스크립트가 읽어 프롬프트를 조립한다
├─ References/<Type>/          # 타입별 기준 그림. 그리는 방식만 빌린다
├─ Rosters/<type>.csv          # 무엇을 그리는가: Key, Subject. 전신 그림은 Height, Flip(적은 Dungeon도)과 얼굴의 보정 FaceDx, FaceDy(용병은 Gender, 양손에 각각 장비를 들면 Hands도, 적은 자세를 맞출 발의 색 Feet: 밝은 색 규칙이 발을 찾지 못할 때), 자세(attack, hit)는 Scale(잰 배율을 덮어쓸 때. enemy_attack, enemy_hit는 늘), 소품은 Height, Flip, 아이템은 Reference, 배경은 Dungeon, FloorLine, UI는 Size, Outline. `ui_variant.csv`는 틀의 색 변형(Key, Source, Fill)
├─ Archive
│  ├─ calls.csv                # 유료 호출의 장부: 시각, 대상, 모델, 사용량, 추정 비용
│  └─ <round>/                 # README.md, 리뷰 시트, 승인된 원본, 목업과 그 스크립트 (움직임 목업의 MP4는 gitignore)
├─ tools
│  ├─ gen_image.py             # 한 번 실행 = 호출 한 번. --dry-run, --refit 은 호출하지 않는다. --style, --roster, --reference, --tail(타입의 꼬리 문장을 바꾼다: 초상 시험) 은 그림체 시험용, --size 는 넓은 자세를 그릴 전신 그림의 캔버스(1536x1024 등)
│  ├─ run_roster.py            # Roster의 빠진 항목을 하나씩 돌린다. --max-calls 가 필수다
│  ├─ cutout.py                # 단색 배경의 그림에서 캐릭터를 오려 낸다. 호출하지 않는다
│  ├─ cutface.py               # 확정된 전신 그림에서 얼굴을 잘라 낸다 (지금 게임은 얼굴을 쓰지 않는다. 아래 "얼굴"). 호출하지 않는다
│  ├─ ui_variants.py           # 맞춘 틀에서 게임에 넣을 Sprite를 만든다: 바탕색을 바꾼 변형. 호출하지 않는다
│  ├─ fit_pose.py              # 용병과 몬스터의 공격·피격 자세를 확정 그림과 같은 크기·자리의 자세 캔버스(2016x1008)에 세우고 2048x1024 파일로 쓴다. 호출하지 않는다
│  ├─ review_pose.py           # 자세의 리뷰 시트: 용병마다 대기/공격/피격, 양손 장비의 손(--enemies 는 몬스터). 호출하지 않는다
│  └─ review_sheet.py          # 리뷰 시트. 호출하지 않는다. 용병(character)은 지금 게임의 용병 전원과 한 줄에 세우고 머리를 같은 배율로 맞대 본다(FACE_BAND, 성별은 Roster의 Gender)
└─ output/                     # 생성 직후 산출물 (gitignore)
.venv/                         # Python 가상환경 (gitignore). openai, pillow
```

폴더는 첫 실제 파일과 함께 만든다.

움직임 목업의 MP4는 Git에 올리지 않는다(`.gitignore`의 `/ArtPipeline/Archive/**/*.mp4`). 판정과 결과는 README와 스크린샷(PNG)이 갖고,
MP4는 그 라운드의 목업 스크립트가 다시 만든다(호출 없음). 게임에 넣는 영상이 생기면 `Assets` 아래에서 LFS로 올라간다(`.gitattributes`).

## 스타일 문서

- 운영 문서는 `STYLE_RUNTIME.md` 하나다. 방향과 결정은 `Docs/Design/10_Art_Direction.md`, 라운드의 경위는 `Archive`가 가진다.
- 스크립트가 파싱하는 형식: 번호 섹션의 제목은 `## <번호>. <제목>`. 공통 규칙, 금지, 기준 그림 문구 섹션에는 ```` ```text ```` 블록이 하나씩 있고,
  타입 섹션은 `- ` 불릿이다. 던전 컨셉 섹션의 제목은 `## Dungeon: <DungeonData.Id>`이고, 그 안의 `### Creatures`(적)와 `### Place`(배경) 아래가 `- ` 불릿이다.
  필요한 섹션이 비면 호출 전에 실패한다.
- 배경은 불투명한 장면이라 오려 낸 그림(전신 그림, 아이콘)과 공통 규칙, 금지를 따로 가진다.
  UI도 따로 가진다: 인물이 아니라 정면에서 본 납작한 조각이고, 원근과 입체감을 금지해야 하기 때문이다.
- 공통 규칙에는 어느 타입에나 맞는 것(선, 색, 채색, 장식의 정도, 배경)만 둔다. 비율과 얼굴처럼 그리는 대상에 딸린 규칙은 타입 섹션에 둔다.
  기준 그림 문구도 타입마다 따로 있다(사람의 기준 그림에서 몬스터가 빌릴 것은 다르다).
- 프롬프트는 공통 규칙 -> Subject -> 타입 섹션 -> (적이면) 던전 컨셉 -> 금지 블록 -> 타입의 기준 그림 문구 순으로 조립한다.
- 작가, 스튜디오, 작품의 이름과 "in the style of"를 프롬프트에 넣지 않는다.
- 그림체를 바꿀 때는 이전 문서를 `Archive/<style-name>/`으로 옮기고 새로 쓴다. (2026-10-04: 이전 그림체는 `Archive/flat-v1/`. 그림체 전환의 경위는 `Archive/12-roar-style/README.md`.)
- 그림체를 **시험**할 때는 지금의 문서, Roster, 기준 그림을 건드리지 않는다. 같은 형식의 시험 세트(스타일 문서, 소재, 기준 그림)를 `Archive/<round>/<style-name>/`에 두고
  `gen_image.py --style <문서> --roster <소재> --reference <기준 그림>`으로 돌린다. `--reference`는 뒤집지 않고 그대로 붙이므로 피사체가 볼 방향을 보는 그림을 준다.
  산출물의 이름은 `--name <Key>_<style-name>`으로 달리한다. 리뷰 시트는 `review_sheet.py --reference`로 시험의 기준 그림을 보인다.
  채택되면 그 세트를 제자리(`STYLE_RUNTIME.md`, `Rosters`, `References`)로 옮기고 이전 문서는 위 규칙대로 Archive로 옮긴다.

## 타입

타입은 실제로 그릴 때 더한다.

| 타입 | 그리는 것 | Roster (`Key`) | 후처리 |
|---|---|---|---|
| `character` | 직업의 전신 그림. 오른쪽을 본다 | `Rosters/character.csv` (`JobData.Id`) | `figure` |
| `enemy` | 적의 전신 그림. 왼쪽을 본다 | `Rosters/enemy.csv` (`EnemyData.Id`, `Dungeon`은 `DungeonData.Id`) | `figure` |
| `item` | 아이템의 아이콘. 그 아이템이 차지하는 칸의 모양으로 그린다 | `Rosters/item.csv` (`ItemData.Id`. 칸의 수는 `ItemData.Size`) | `cell` |
| `background` | 화면의 배경. 불투명, 인물 없음 | `Rosters/background.csv` (던전의 배경은 `Dungeon`이 `DungeonData.Id`) | `scene` |
| `ui_frame` | UI의 틀: 패널, 제목의 명패, 보드의 머리 띠, 칸, 버튼, 가방. 화면에서 늘여 쓰는 빈 사각형 | `Rosters/ui_frame.csv` (쓰임새의 이름. `Flat`이 no면 그린 결을 남긴다) | `frame` |
| `ui_piece` | UI의 장식 조각: 양초의 조각, 해골 더미, 보드 머리의 금 징(전에는 폭풍 시계의 다이얼). 늘이지 않고 통째로 보인다 | `Rosters/ui_piece.csv` (쓰임새의 이름) | `glyph` |
| `ui_icon` | UI의 작은 기호: 상태 아이콘 | `Rosters/ui_icon.csv` (쓰임새의 이름) | `glyph` |
| `prop` | 무대 바닥에 서는 소품: 쓰러진 용병의 무덤(2026-10-04 Round 21, C 채택). 전신 그림처럼 바닥선에 세운다 | `Rosters/prop.csv` (쓰임새의 이름. 데이터가 가리키지 않는다) | `figure` |
| `attack`, `hit` | 용병의 공격 자세와 피격 자세(Round 19·23). 확정 그림을 보고 같은 인물로, 자세와 표정만 바꾼다 | `Rosters/attack.csv`, `Rosters/hit.csv` (`JobData.Id`) | `pose` (`tools/fit_pose.py`) |
| `enemy_attack`, `enemy_hit` | 몬스터의 공격 자세와 피격 자세(2026-10-05 Round 26~28). 확정 그림을 보고 같은 몬스터로, 자세와 표정만. 왼쪽을 보고 눈동자 없는 눈 그대로 | `Rosters/enemy_attack.csv`, `Rosters/enemy_hit.csv` (`EnemyData.Id`, 잰 배율 `Scale`) | `pose` (`tools/fit_pose.py`) |

- **공격·피격 자세**는 용병의 기본 그림이 확정될 때마다 더한다(규칙, Design/10 §2. Round 23에서 정식 타입이 됐다): `gen_image.py --type attack|hit --key <job>`.
  기준 그림은 스크립트가 만든다: 그 직업의 확정 원본(`output/character/<key>.raw.png`)을 1536x1024에 키 700으로 앉혀 발바닥을 높이의 4분의 3에 두고(그 아래는 무기가 내려갈 빈 바닥), 공격은 왼쪽 3분의 1에, 피격은 가운데에(`output/<type>/<key>.reference.png`).
  스타일 문서 §25(자세의 구도: 같은 인물, 손, 바닥선 아래로 내려가는 무기, 웃음·작은 머리 금지)와 §26(기준 그림 규칙). 표정은 소재가 적는다(공격은 사나운 함성, 피격은 한 눈을 감은 찡그림).
  생성은 원본만 남기고, 맞추기는 `tools/fit_pose.py`가 한다(아래 "후처리 (`pose`)"). 리뷰 시트는 `tools/review_pose.py`.
  - **양손에 각각 다른 장비를 들면 손을 지킨다**(2026-10-04, Design/10 §5. Round 23의 성기사 공격이 철퇴와 방패를 바꿔 들었다. 한 손에만 무기를 든 용병은 손이 바뀌어도 된다: 사용자 판정, 마검사 공격은 그대로): 그런 용병의 소재는 장비마다 든 손을 적는다(대기 그림에서 본 대로. 오른쪽을 보는 4분의 3 측면에서 앞(보는 사람의 왼쪽)의 팔이 오른팔).
    소재가 그 손과 어긋나는 움직임을 시키지 않는다(먼 팔의 방패를 "왼쪽 뒤로" 보내라는 문장이 손을 바꾸게 했다). 시험 문서는 §3에 "양손에 각각 다른 것을 들면 각 손은 그것을 그대로 든다", §4에 손의 절, 금지에 무기와 방패가 손을 바꾸는 것을 둔다.
  - **자세 그림의 점검**(판정에 내기 전에 모두 본다): 같은 인물인가, 양손에 각각 장비를 든 용병은 각 손이 대기 그림과 같은가, 머리가 대기와 같은 크기인가, 디딘 뒷발과 바닥선, 무기가 잘리지 않았는가, 표정이 소재대로인가. 리뷰 시트는 그런 용병의 든 손을 적어 세 그림(대기·공격·피격)을 맞대 보게 한다.
- **붕괴·각성의 자세**(2026-10-06 Round 38 "B", Design/10 §5): 붕괴의 순간 용병의 그림이 공격 자세처럼 **상태의 자세**로 바뀌었다가 돌아온다(`12_UI.md` "연출"). 그리는 길은 피격 자세의 것이다:
  `gen_image.py --type hit --key <job> --name <job>_broken|<job>_resolute --style Archive/38-breakdown-fx/STYLE_RUNTIME-statepose.md --roster Archive/38-breakdown-fx/broken.csv|resolute.csv`(§25를 "제자리에 선 상태의 자세"로, 색의 기분을 더한 시험 문서. 소재는 그 라운드의 것),
  맞추기는 `Archive/38-breakdown-fx/fit_state_poses.py`(`tools/fit_pose.py`를 그 직업의 확정 원본에 대고 돌린다). 파일은 `Assets/@Art/Pose/Job/<Id>_broken.png`, `<Id>_resolute.png`(2048x1024), 주소는 `pose/job/<kebab-id>-broken`, `-resolute`(`ArtAddress.Broken`·`Resolute`, `JobData.BrokenPose`·`ResolutePose`).
  **직업마다 따로 그리는 선택 그림**이다(발키리는 Round 38, 나머지 다섯은 Round 40에 확정 — `Archive/40-state-poses/`: 소재 `broken.csv`·`resolute.csv`, 맞추기 `fit_state_poses.py --scale`. 지금은 여섯 직업 모두 있다): `ArtSetup.Files`는 파일이 있는 것만 Entry로 만들고, `ExpeditionArt`는 `ResourceManager.ExistsAsync`로 등록된 것만 읽으며, 없는 직업은 그 순간 대기 그림 그대로다. 정식 타입(`broken`·`resolute`)으로 올리고 선택을 거두는 일은 Slice C의 후보다(Round 40 권장안: 생성 도구·스타일 문서·로스터·`fit_pose` 네 곳을 바꾸는 일).
- **몬스터의 공격·피격 자세**(2026-10-05, Design/10 §5. Round 26~28의 시험이 확정되어 정식 타입이 됐다): `gen_image.py --type enemy_attack|enemy_hit --key <enemy>`. 용병의 것을 왼쪽을 보는 몬스터로 돌린 것이다:
  기준 그림은 그 몬스터의 확정 원본(`output/enemy/<key>.raw.png`, 눈동자를 지운 것)을 같은 캔버스에 앉혀 공격은 **오른쪽** 3분의 1에(왼쪽으로 돌진할 자리), 피격은 가운데에. 스타일 문서 §27(구도: 왼쪽, 눈동자 없는 한 가지 색의 눈, 발·짐승의 발·부츠가 바닥선에, 장비 그대로, 날아가는 화살 없음, 우스꽝스럽게 위협적인 얼굴)과 §28(기준 그림 규칙).
  던전 컨셉은 붙이지 않는다(확정 그림이 이미 지니고, 자세는 그 장비를 그대로 지킨다. 던전 절의 "장비 하나를 걸친다"는 장비를 바꾸게 할 수 있었다). 소재는 정체 목록(확정 그림에서 읽은 체형·장비·무기, 눈의 색) + 자세 + 표정(공격은 사나운 비명·포효, 피격은 한 눈을 감은 찡그림).
  공격 자세는 무기 장비가 있는 몬스터에 그린다(공격 자세는 무기 장비의 돌진에만 보인다: Design/10 §5).
- `prop`의 타입 섹션(§23)은 지금 하나뿐인 무덤에 맞춰 적혀 있다(시험 문서의 것을 그대로 옮겨 확정한 무덤을 같은 문구로 다시 그릴 수 있다). 다른 소품을 그릴 때 무덤에 딸린 문장을 소재로 옮긴다.
- 호출: `images.edit`에 기준 그림을 붙인다. 모델 `gpt-image-2.5-sunburst`, `1024x1024`, `medium`, 투명 배경, PNG.
  모양이 정사각이 아닌 것(배경, UI의 틀, 아이템)은 그 모양에 가까운 캔버스로 생성한다.
  모델은 투명 배경을 지원하는 것이어야 한다(`gpt-image-2`는 투명 배경 요청을 거절한다).
- 기준 그림은 타입마다 정해 둔 것만 붙인다. 승인된 우리 그림이 생기면 그것을 기준 그림으로 바꾼다. (2026-10-04: 모든 타입이 확정한 직업 셋의 시트 `References/Character/style_ref_roster.png`를 붙인다. 적만 뒤집는다. 이전 기준 그림은 `Archive/flat-v1/`. 같은 날 용병의 표정을 진지하게 바꾼 뒤 시트도 새 확정 그림으로 같은 자리·배율에 다시 합성했다: `Archive/20-serious-face/make_reference_sheet.py`, 이전 시트는 그 `before/`.)
- 파티 쪽 그림(직업)은 화면의 오른쪽, 곧 적 쪽을 보고 눈도 그쪽을 본다. 적은 왼쪽을 본다(적의 눈에는 눈동자가 없어 머리와 몸이 방향을 말한다. 2026-10-04 Design/10 §3). 모델은 방향을 문구보다 기준 그림에서 따르므로,
  오른쪽을 보는 기준 그림(확정한 직업 셋의 시트 `References/Character/style_ref_roster.png`, 2026-10-04)을 직업에는 그대로, 적에는 좌우를 뒤집어 붙인다.
  이전 기준 그림(`style_ref_mercenary.jpg`, 왼쪽을 본다)은 아이템·배경·UI 타입이 계속 쓴다.
- 체형(등신 포함), 얼굴, 자세는 Roster의 `Subject`가 캐릭터마다 적는다(2026-10-04 그림체: 캐릭터마다 다른 체형과 표정이 핵심이다). 스타일 문서는 손맛과 공통 규칙만 갖는다.
  다만 용병의 머리는 늘 크다(스타일 문서 §4 "always with a big head"). 키가 크거나 덩치가 큰 체형도 머리를 줄여 내지 않는다(Design/10 §2. 2026-10-04 성기사).
- **몬스터는 던전마다 컨셉이 있다.** 던전의 컨셉(눈의 색, 몸의 표시, 장비, 강조색)은 스타일 문서에 던전마다 한 절로 적고,
  그 던전의 적을 그릴 때마다 같은 문구가 붙는다. 그래서 한 던전의 몬스터는 서로 닮고 다른 던전과 구별된다.
- 아이템이 어느 유닛이 든 것이면 Roster의 `Reference`에 그 유닛의 확정된 그림을 적는다. 그 그림을 기준 그림으로 붙여 그 유닛이 든 것과 같은 모습으로 그린다.
  그렇게 해서 아이콘과 전신 그림 속의 무기가 같은 물건으로 보인다.
- Roster의 `Key`(와 적의 `Dungeon`)는 Game Data의 `Id`여야 한다. 스크립트가 호출 전에 `Assets/@Data/Source`의 CSV와 맞춰 본다.
- 기준 그림에서는 그리는 방식(선, 비율, 색, 채색)만 빌린다. 기준 그림을 그대로 그림으로 쓰기로 승인받았으면 다시 그리지 않고
  배경만 걷어 낸다(`cutout.py`). 그 결과가 그 Key의 원본이고, 맞추기는 생성한 그림과 같다.
- Roster의 `Subject`는 영어이고 명사 중심이다. 어떻게 보이는지는 스타일 문서가 맡는다. 재생성은 같은 문구에 수정(`--extra`)을 더해 재현한다.
- 이미 산출물이 있는 이름으로는 다시 호출하지 않는다. 다른 후보는 `--name`으로 이름을 달리한다.

## 후처리 (`figure`)

구도는 모델에 맡기지 않고 산술로 맞춘다. 모든 전신 그림이 같은 캔버스, 같은 바닥선을 쓰므로 화면은 그림마다 자리를 맞출 필요가 없다.

- 피사체의 경계는 Alpha로만 잰다.
- 3:4 캔버스(672x896)의 아래 가운데에 세운다. 발이 캔버스 아래의 바닥선에 닿는다.
  캔버스는 생성한 그림(1024)보다 작다. 그래서 맞추기는 늘 줄이기이고, `Height`가 같은 그림은 같은 키로 선다.
- 키는 Roster의 `Height`(캔버스 높이에 대한 %)로 맞춘다. 폭이 캔버스를 넘으면 폭에 맞춰 더 줄인다.
- 줄이기만 한다. 키우지 않는다. 모자라면 경고를 내고 판정에 맡긴다. 생성 캔버스의 가장자리에 닿은 그림(잘렸을 수 있다)도 경고한다.
- 맞춘 뒤 실루엣 둘레에 고른 어두운 띠(`FIGURE_OUTLINE` 1.35px, 색은 UI의 선 `UI_LINE`, 바깥 가장자리 흐림 0.8)를 두른다. 2026-10-04 플레이 피드백 "용병·몬스터의 외곽선을 더 굵게, 배경과 구별되게"로 8px를 넣었고, 같은 날 검토 뒤 "외곽선 1/2 적용"으로 4px,
  같은 날 밤 Round 25의 목업 뒤 "권장안으로 변경 구현"으로 1.35px: 그린 잉크선과 띠를 합친 외곽선이 4px 때의 약 2/3(화면에서 평균 약 2.2px).
  정수가 아닌 폭은 이웃한 두 정수 폭의 띠를 섞는다(`ring_alpha`. 캔버스가 화면에서 약 1/3이라 잉크의 양이 곧 폭이다). 그림은 바뀌지 않고 `--refit`으로 다시 두를 수 있다. 옆 여백 12는 띠보다 넓어 캔버스 밖으로 나가지 않는다.
- **적의 눈동자** (2026-10-04, Design/10 §3): 몬스터의 눈은 눈동자 없이 한 가지 노랑으로 채운다. 새 적은 스타일 문서(§5 구도, §6 기준 그림 규칙, 던전 절)와 소재대로 처음부터 그렇게 그린다.
  그 전에 확정한 다섯은 원본(`<name>.raw.png`)에서 눈동자만 지웠다: `Archive/17-outline-pupils/remove_pupils.py`가 확정 원본(`Archive/12-roar-style/approved/enemy/`)을 읽어 눈마다 정한 자리(노란 면에 갇힌 눈동자는 구멍, 테두리에 붙은 것은 눈의 다각형)를 둘레의 흰자 색으로 칠하고 `output/enemy/`에 쓴다(호출 없음).
  `--refit`은 `output/enemy/`의 고친 원본에서 다시 맞춘다. 고친 원본은 `Archive/17-outline-pupils/approved/enemy/`에도 둔다(`output/`은 저장소 밖). 다시 생성한 적에는 이 편집이 필요 없다(눈마다 자리를 손으로 정한 것이라 다른 그림에는 쓸 수 없다).
- 폭이 캔버스(3:4)를 넘는 그림(넓은 자세, 옆으로 뻗은 무기)은 폭에 맞춰 더 줄어 키가 목표보다 작게 선다. 직업마다 `Height`를 따로 두거나 캔버스를 넓히는 것은 그림체 전환의 【미결】이다(Design/10 §2).
- `Flip`이 참이면 좌우를 뒤집는다. 반대쪽을 보고 나온 그림은 다시 생성하지 않고 뒤집는다.
- 생성한 원본(`<name>.raw.png`)을 먼저 저장하고 맞춘 그림(`<name>.png`)을 따로 둔다. `Height`나 `Flip`을 고친 뒤에는
  `--refit`으로 원본에서 다시 맞춘다(호출 없음).
- 호출마다 사용량(토큰)과 추정 비용을 출력하고 `Archive/calls.csv`에 적는다.

## 후처리 (`pose`)

자세는 확정 그림을 대신해 잠깐 보이므로, 그 그림과 같은 크기·자리에 세운다(`tools/fit_pose.py`. Round 19·23의 규칙).

- 캔버스: 그림 캔버스(672x896)를 가운데에 둔 **2016x1008**(양옆으로 672씩, 바닥선 아래로 112). 바닥선은 그림 캔버스와 같은 높이다. 화면은 그림 자리(225x300)를 가운데에 둔 675x338로 그린다(`12_UI.md` "유닛의 그림").
- 파일: 캔버스를 64/63배 한 **2048x1024**(두 변이 2의 거듭제곱. 아래 "Import 정책"의 압축). 화면은 캔버스의 비율(2:1, 바닥선 112/1008)로만 그리므로 자리가 그대로다. 맞추기와 리뷰는 2016x1008에서 하고 쓸 때만 늘린다(`fit_pose.texture`).
- 배율: 자세가 바뀌어도 크기가 그대로인 부위로 잰다. 둥근 금 장식(걸쇠, 리벳, 버클, 징)의 긴 지름을 확정 원본과 자세에서 재어 큰 것 넷의 중앙값의 비. 금 장식이 없거나 가려져 맞지 않으면 소재의 `Scale`로 덮어쓴다
  (Round 23: 대마법사는 지팡이의 수정, 마검사는 부츠의 네모 버클, 성기사 피격은 허리 버클과 머리로 잰 값). 리뷰 시트에서 머리가 대기 그림과 같은 크기로 보이는지 본다.
  모델이 장식과 무기를 몸보다 작게 그리면(Round 40의 상태 자세: 장식 0.7~0.85배, 머리 0.85~1.0배) **머리 크기**가 기준이다 — 자세는 그 유닛의 머리와 몸이 그대로 보여야 하므로 장식의 배율은 버리고, 두 눈 사이의 거리와 격자 시트로 잰 머리 크기를 `Scale`로 적는다(`Archive/40-state-poses/README.md` "맞추기").
- 기준 크기: 확정 그림을 게임의 그림(`Assets/@Art/Unit/Job/<Id>.png`)과 같은 크기로 세운다 — `Rosters/character.csv`의 **Height**로 맞추고 키우지 않는다(`fit_figure`와 같은 규칙. `approved_place`). Round 40까지는 늘 90으로 세워 Height 79인 마검사(Round 39)의 자세가 게임 안에서 13.7% 컸다.
- 자리: 디딘 뒷발(부츠 가죽색의 맨 왼쪽 무리)을 확정 그림의 뒷발 자리에 두고, 그 발바닥이 바닥선이다. 돌진에서도 밀림에서도 뒷발은 땅에 있다. 앞발은 들릴 수 있고 무기는 바닥선 아래로 내려갈 수 있다.
- 색: 확정 원본의 주요 색(14색으로 줄인 팔레트)마다 평균의 차이만큼 옮기되, 가장 큰 차이를 줄일 때만 쓴다(CIE76 ΔE). 잉크와 흰색은 그대로.
- 둘레의 띠는 전신 그림과 같다(`FIGURE_OUTLINE`, `UI_LINE`, `ring_alpha`). 결과는 `output/<type>/<key>.png`, 캔버스 밖으로 나가면 경고한다.
- **몬스터**(`enemy_attack`, `enemy_hit`): 뒷발은 **맨 오른쪽 발**이다. 발의 색은 확정 원본의 맨 아래 12분의 1에서 가장 흔한 밝은 색(그 띠에 어두운 발톱뿐이면 띠를 넓힌다), 그 규칙이 다른 것을 잡으면 `Rosters/enemy.csv`의 `Feet`(감독관의 어두운 부츠).
  뒷발 = 그림 오른쪽 절반의 바닥(앞에서 바닥선 아래로 내려오는 무기를 피한다)에서 위로 6분의 1 안의, 그 색의 맨 오른쪽 덩어리. 발 안의 잉크선(발가락, 부츠의 주름)은 그 색의 마스크를 조금 키워 잇는다.
  배율은 소재의 `Scale`이다(늘 적는다): 금 장식이 없고, 잰 부위가 몬스터마다 다르다(얼굴, 디딘 뒷발, 칼날·모자·양초·코·귀·플라스크를 손으로 고른 점에서 채워 잰 면적비의 중앙값. `Archive/26-monster-motion/`, `27-monster-poses/`, `28-shaman-weapon/`).
  자리는 게임 그림의 자리(`fit_figure`: 소재의 `Height`, 폭에 맞춤)에서 뒷발을 뒷발에. 색 맞춤과 띠는 용병과 같다. Round 26~28의 열 장을 이 도구로 다시 맞추면 확정한 후보와 픽셀까지 같다.

## 후처리 (`cell`)

아이템의 아이콘은 그 아이템이 차지하는 칸의 모양으로 그린다. 보드 패널에서 한 아이템의 칸은 유닛의 열 아래에 **위아래로 쌓이므로**(`12_UI.md` "전투 화면". 2026-10-03 목업 V안)
칸이 많을수록 더 높은 모양이다. 칸의 수는 Game Data(`ItemData.Size`)에서 읽는다. Roster에 다시 적지 않는다.

| 칸의 수 | 칸 (화면) | 아이콘 자리 (테 좌우 8, 위아래 5 안쪽) | 생성 캔버스 | 맞춘 캔버스 | 구도 (스타일 문서의 그 칸 수의 절) |
|---|---|---|---|---|---|
| 1 | 180x60 | 164x50 | `1536x512` | 328x100 | 가로로 긴 띠(약 3.3:1). 긴 것은 눕힌다 |
| 2 | 180x122 | 164x112 | `1536x1024` | 328x224 | 가로가 조금 긴 사각형(약 3:2). 긴 것은 왼쪽 아래에서 오른쪽 위로 비스듬히 |
| 3 | 180x184 | 164x174 | `1024x1024` | 328x348 | 정사각에 가까운 사각형(세로가 조금 길다, 약 15:16). 긴 것은 비스듬히 |

- 맞춘 캔버스는 아이콘 자리의 2배다. 화면은 아이콘을 칸의 가운데에 비율을 지켜 그 자리에 맞춰 놓는다.
- 조각을 Alpha로 오려 내 비율을 지킨 채 캔버스에 가장 크게 맞추고 가운데에 놓는다. 둘레에 고른 띠(4px, `UI_LINE`)를 두른다(`glyph`와 같은 맞추기다).
  캔버스보다 길쭉한 그림은 한쪽에 맞춰 줄어드므로 캔버스의 일부만 채운다. 폭의 비율은 실행이 출력한다.
- 프롬프트의 구도는 아이템의 공통 규칙에 그 칸 수의 절을 더한 것이다. 긴 것은 손잡이가 왼쪽(아래), 끝이 오른쪽(위)이다.
- 칸의 크기(`12_UI.md` "전투 화면")가 바뀌면 이 표와 `ITEM_CELLS`, `review_sheet.py`의 `ITEM_CELL`을 같이 고치고, 비율이 달라진 칸 수의 아이콘은 다시 그린다.
- 아이콘 스물한 개는 2026-10-04 그림체 전환 때 그때의 칸(180x50·간격 4)의 캔버스 328x80, 328x188, 328x296에 그렸다(그 전의 것은 180x60을 쌓은 칸의 캔버스 320x88, 320x216, 320x344. `Archive/03-items/approved`).
  같은 날 개정 6이 칸을 180x60·간격 2로 바꿨지만 **다시 그리지 않았다**(사용자 결정: 유료 호출은 요청 때만. 화면은 폭에 맞춰 놓으므로 크기는 같고 위아래 여백만 는다 — `12_UI.md` "아이템의 아이콘").
  이 표는 다음에 그리는 아이콘의 캔버스다. 스물한 개를 다시 그리는 것은 요청이 있을 때 한 번에 한다(호출 21회).
- 리뷰 시트(`review_sheet.py --type item`)는 아이콘을 실제 칸에 넣어 만든다: 화면 크기(쿨다운이 절반 찬 것과 다 찬 것)와 그 2배.

## 얼굴 (`cutface.py`)

**게임은 지금 얼굴을 쓰지 않는다.** 전투의 보드 패널의 얼굴은 V안(2026-10-03, `12_UI.md` "전투 화면")에서 빠졌고, 2026-10-05에 그림과 배선(주소, 읽어 두기, Import 검사)도 게임에서 뺐다.
도구는 남긴다. 얼굴을 쓰는 화면이 생기면 따로 그리지 않고 **확정된 전신 그림에서 다시 잘라 낸다**(호출 없음).

- 원본은 게임이 보여 주는 확정된 그림(`Assets/@Art/Unit`)이다. 후보에서 자르지 않는다.
- 자르기는 산술이다: 정사각의 한 변은 그림 높이(Alpha 경계)의 27%, 그림의 맨 위 조각(머리)의 가운데에 맞추고, 모자나 무기가 머리 위로 솟은 그림은 Roster의 `FaceDx`, `FaceDy`(그림의 px)로 옮긴다.
  옛 보드 패널의 얼굴 틀(72) 안쪽(60)의 2배인 120x120으로 저장한다. 리뷰 시트(`output/review/faces.png`)가 모든 얼굴을 편의 색 위에 화면 크기와 2배로 보인다.
- 돌리는 법: `cutface.py --type character --type enemy [--only key]` -> `output/face/<Key>.png`. 되살리는 배선(`ArtAddress.FaceOf`, `JobData`·`EnemyData`의 `Face`,
  `ArtSetup`의 `Face` 정책, `ExpeditionArt`의 읽어 두기, 주소 `face/<job|enemy>/<key>`, 파일 `Assets/@Art/Face/<Job|Enemy>/<Id>.png`)은 Git의 2026-10-05 이전 상태에 있다.
  `Archive/08-panel-cells/mock_panel_cells.py`는 `Assets/@Art/Face`를 읽으므로, 다시 돌리려면 잘라 둔 얼굴을 그 자리에 둔다.

## 후처리 (`scene`)

모든 배경이 같은 캔버스, 같은 바닥선을 쓴다. 그래서 화면은 배경마다 자리를 맞출 필요가 없다(전신 그림과 같은 생각이다).

- 캔버스는 화면(16:9)보다 세로가 긴 3:2(`2304x1536`)다: UI의 바닥선이 움직여도 그림을 올리거나 내려 맞출 여유가 있다.
- **바닥선은 캔버스 높이의 57%**다(위에서부터. `SCENE_FLOOR`). 유닛의 발이 서는 높이다. 화면은 이 선을 전장의 그림 자리 아래 끝에 맞춘다(`12_UI.md` "전투 화면").
- 그린 그림에서 발이 서는 높이를 재서 Roster의 `FloorLine`(그림 높이에 대한 %)에 적는다(처음 그릴 때는 바닥선의 값을 적어 둔다). 바닥선과 같으면 그린 대로 둔다.
  다르면 그 높이가 바닥선에 오도록 캔버스 비율의 창을 잘라 캔버스 크기로 맞춘다(`--refit`. 호출 없음).
- 구성은 옆에서 본 무대다(스타일 문서의 배경 섹션). 인물을 그리지 않는다.

## 후처리 (`frame`, `glyph`)

UI의 조각은 전부 같은 굵기의 외곽선을 갖는다. 모델이 그린 선의 굵기에 맡기지 않고 산술로 두른다.

- 조각을 Alpha로 오려 내 Roster의 `Size`(화면에 쓰는 크기의 2배)로 맞춘다. 흐린 그림자나 번짐은 버린다.
- `frame`은 `Size`에 꽉 차게 늘인다(화면에서 다시 늘여 쓴다). 생성할 때의 캔버스는 `Size`의 비율에 가장 가까운 것을 쓴다.
  `glyph`는 비율을 지키고 가운데에 놓는다.
- 그 둘레에 `Outline`(px)만큼의 고른 띠를 두른다. 띠의 색은 유닛의 외곽선 색으로 고정이다(`UI_LINE`).
- 틀의 바탕은 한 색으로 편다(평면 채색). 모델이 남긴 옅은 얼룩을 없앤다. 같은 색조의 뚜렷이 더 어둡거나 밝은 띠(버튼의 아래쪽 띠, 칸의 테)는 그 단을 지킨다.
- **색 변형**: 한 틀의 색만 다른 것(편의 색, 고른 상태, 색을 입혀 쓰는 흰 바탕)은 다시 생성하지 않고 그 틀에서 바탕색을 바꿔 만든다(`ui_variants.py`. 호출 없음).
  무엇을 어떤 색으로 만드는지는 `Rosters/ui_variant.csv`(`Key`, `Source`, `Fill`)가 가진다. `Fill`이 비면 그 틀의 색 그대로다.
  게임에 들어가는 틀은 전부 이 변형이다(`output/ui_variant/<Key>.png`).
- 화면에서 늘일 때 늘어나지 않는 가장자리(9-slice의 Border)는 화면의 일이라 Unity 쪽이 갖는다(`UiArt`. `12_UI.md` "UI의 그림").
- (2026-10-03 연출 3차) 틀은 **질감과 장식이 있는 그림**으로 다시 생성한다(스타일 문서 §14·§15·§17: 가죽·쇠·놋쇠, 리벳, 모서리 장식. 평면 채색과 굵은 외곽선은 그대로).
  결(두 톤의 평면 무늬)을 남길 틀은 Roster의 `Flat`을 no로 두면 `frame` 후처리가 바탕색을 펴지 않는다(`panel`, `bag`). 색 변형은 `Rosters/ui_variant.csv`의 `Mode`로 정한다:
  `flatten`(바탕을 한 색으로 펴며 바꾼다. 명패·칸·버튼)과 `tint`(결을 남긴 채 바탕의 색을 바꾼다. `tint_fill`). 전의 틀은 `Archive/09-battle-ui-feel/frames-before/`에 있다.
- `ui_piece`(§22)는 늘이지 않는 장식 조각이다. `glyph`처럼 비율을 지켜 캔버스의 84%에 맞추므로, 화면 크기의 2배를 0.84로 나눈 `Size`를 적는다(다이얼 200 → 476).
- (2026-10-04) 틀의 변형(`ui_variant.csv`)은 Sprite의 이름과 생성한 틀을 떼어 놓는다. 그래서 그림체나 재질을 바꿀 때 Unity 쪽 이름은 그대로다. 디아블로 컨셉(`Archive/14-ui-diablo/`)에서는 `panel`·`table`·`tablet`이 `stone_panel`에서, `plate_*`가 `gothic_plate`에 편의 색을 `tint`로, `bag`이 `iron_inventory`, `belt`가 `belt_iron`, `trough`가 `iron_slot`, `potion_slot`(과 금색 `_selected`)이 `iron_pocket`, `button`이 `button_iron`을 흰색으로 편 것이다.
  유닛의 명패 `plate_party`·`plate_enemy`·`plate_danger`·`plate_target`은 2026-10-05 명패를 없애며 지웠다(`plate_label`은 남는다). 그때 더한 **보드의 머리 띠** `name_tag`는 `iron_tag`를 색 그대로(`tint`) 쓴 것이고,
  그 왼쪽 끝의 **금 징** `gold_stud`는 늘이지 않는 `ui_piece`다. 둘 다 Round 29의 견본(`tag29_slim`, `badge29_a_stud`)을 사용자가 목업으로 승인해 같은 소재로 `Rosters/ui_frame.csv`·`ui_piece.csv`에 올렸다(`Archive/29-plates/`).
  **도형으로 그린 조각**은 변형이 아니라 `Archive/<round>/draw_pieces.py`가 `output/ui_placeholder/`에 그린 것을 복사한다: 장식 없는 뼈색 칸 `slot`·`slot_selected`, 양초의 녹은 윗면 `candle_top`, 연기 `smoke`, 쇠 사슬 `chain`(14), 빛 `glow`(13), 비네트 `vignette`와 양초의 빛 `candle_dark`·`candle_warm`(16. 09가 그린 비네트는 밝기 경사가 거꾸로였다), 아이템 쿨다운의 경사 `charge_ramp`(18. 왼쪽이 투명하고 오른쪽이 불투명한 흰 띠). 야영지 장비(13)의 변형 매핑은 그 README에 남아 있다.
  장비 피로의 표 `fatigue_tag`(32. 자주 바탕에 연보라 테, 끝이 둥근 64x40. Border 20이라 옆으로만 늘어난다)는 `Archive/32-equipment-fatigue/draw_tag.py`가 `Assets/@Art/UI/Frame`에 바로 그린다.
  지도의 정예와 야영지 표식 `node_elite`(전투의 검을 붉은 마름모 위에)와 `node_camp`(장작 둘과 불꽃)(34. 96x96, 화면의 2배)는 `Archive/34-long-map/draw_icons.py`가 `Assets/@Art/UI/Icon`에 바로 그린다.
  붕괴의 순간의 **효과 조각** `ink_burst`(먹 튐: 검정에 붉은 보라 핏줄)와 `light_burst`(빛살: 크림 원판과 금빛 빛살)(38. 1024 원본을 512로)는 `Archive/38-breakdown-fx`의 시험 문서(`STYLE_RUNTIME-fx.md`: `ui_piece`의 자리에 "무대 위의 효과 조각")와 소재(`fx.csv`)로 생성한 것이다.
  무대에서 유닛 뒤에 상태 색의 `glow`와 함께 선다(`12_UI.md` "연출"). 사용자가 먹 튐 2차(붉은 보라 먹)보다 1차를 골랐고 둘 다 0.8배(416·352)로 쓴다.
  아이템 칸의 단계 표 `tier_tag`(41. 먹색 알약에 흰 테 3, 64x40, Border 20이라 옆으로만 늘어난다. 화면이 단계의 색으로 물들인다)와 그 별 `star`(41. 흰 별에 먹 외곽선, 24x24. 화면이 단계의 글 색으로 물들인다)는
  `Archive/41-tier-marks/draw_pieces.py`가 `Assets/@Art/UI`에 바로 그린다. Round 35의 단계 테 `tier_rim`은 Round 41이 뺐다. 단계의 외곽선은 조각이 아니라 아이콘 자신의 실루엣이다(`12_UI.md` "단계").
  양초의 불꽃 `candle_flame`은 `Outline` **2**다(2026-10-07 Round 43, 사용자 "안 C". 다른 `ui_piece`의 6과 다르다). 생성 원본에서 `gen_image.py --type ui_piece --key candle_flame --refit`으로 다시 맞췄다(호출 없음).
  양초의 빛처럼 화면에서 크게 늘이는 부드러운 경사는 2배로 그리지 않는다(UI의 MaxSize 512 안에서 512x256. 늘여도 경사는 매끄럽다).

## 승인 라운드

```text
1. 소재: Roster의 Subject를 먼저 보여 준다 (호출 없음)
2. 상한: 라운드를 시작할 때 호출 수의 상한을 정한다. 닿으면 멈추고 보고한다
3. 생성: 항목당 한 번
4. 리뷰 시트: 한 장에 [실제 표시 크기, 게임 배경색 위 / 기준 그림과 후보 원본]. 용병은 지금 게임의 용병 전원과 한 줄에, 머리는 같은 배율로
5. 판정: 항목별로 "확정", "재생성 - 방향", "소재 교체"
6. 재생성은 요청된 항목만 한 번씩. 뒤집기, 크기 맞추기는 로컬에서 한다 (호출 없음)
7. 승인분만 배선하고 검증 체인을 한 번 돌린다
8. Archive/<round>/README.md: 후보, 판정, 받아들인 문구, 거절 사유, 호출 수와 사용량
9. 용병은 기본 그림(전투 준비 자세)이 확정되면 공격 자세와 피격 자세를 더한다(Design/10 §2): 같은 승인 라운드를 그 둘로 한 번 더 돈다.
   몬스터도 같다(Design/10 §5, 2026-10-05): 공격 자세는 무기 장비가 있는 몬스터에
```

- **용병의 얼굴 크기**(2026-10-04, 성기사의 작은 머리가 세 라운드를 지나도록 걸러지지 않았다: 리뷰 시트가 후보끼리나 그 그림의 전과 후만 맞대 보였다):
  맞춘 캔버스(672x896)에서 눈에서 턱(수염이 있으면 수염 끝)까지가 **남성 65~85px, 여성 48~62px** 안이다(`review_sheet.py`의 `FACE_BAND`. 여성이 조금 작다: Design/10 §2.
  지금 남성 68~81(다시 그린 성기사 81, 전의 성기사 41), 여성 51~56. 성별은 `Rosters/character.csv`의 `Gender`).
  리뷰 시트의 머리 줄이 지금 게임의 용병 전원과 후보를 눈높이에 맞춰 같은 배율로 세우고 이 띠를 칠한다. 턱이 띠 밖이면 판정 전에 보고하고 원인(소재의 등신, 넓은 자세가 폭에 맞춰 줄어드는 것)을 고친다.
  자세·표정 시험(Round 19·20처럼 확정 원본을 붙여 다시 그린 것)도 같은 띠로 본다. 눈의 흰자를 찾지 못하면 시트가 그렇게 적고 머리 위에서 맞춘다.
- 판정 없이 호출하지 않는다.
- 러너는 파일이 생겼는지로 성공을 판정하고, 이미 있는 항목은 건너뛴다. 실패한 실행도 호출 한 번으로 센다.
- 유료 호출의 총 상한은 `Docs/Roadmap.md`의 그 단계에 적는다. 누계는 `Archive/calls.csv`가 가진다.

## Unity 배선

승인된 그림 한 장이 들어가는 길은 항상 같다.

```text
유닛의 전신 그림
1. PNG   -> Assets/@Art/Unit/<Job|Enemy>/<Id>.png
2. CSV   -> JobData.csv / EnemyData.csv 의 Figure = "unit/<job|enemy>/<kebab-id>"
3. Entry -> CSV의 Figure에서 자동으로 나온다 (Group F1-Art, Scope Expedition)

던전의 배경
1. PNG   -> Assets/@Art/Background/Dungeon/<Id>.png
2. CSV   -> DungeonData.csv 의 Background = "background/dungeon/<kebab-id>"
3. Entry -> CSV의 Background에서 자동으로 나온다 (Group F1-Art, Scope Expedition)

아이템의 아이콘
1. PNG   -> Assets/@Art/Item/<Id>.png (맞춘 그림 그대로: 칸 수에 따라 320x88, 320x216, 320x344)
2. CSV   -> ItemData.csv 의 Icon = "item/<kebab-id>"
3. Entry -> CSV의 Icon에서 자동으로 나온다 (Group F1-Art, Scope Expedition)

UI의 틀과 아이콘
1. PNG   -> Assets/@Art/UI/Frame/<Key>.png (ui_variant의 Key, 늘이지 않는 ui_piece도), Assets/@Art/UI/Icon/<Key>.png
2. 목록  -> UiArt 에 이름(틀은 Border도)을 적는다. 데이터가 가리키지 않는다
3. Entry -> 없다. 화면 Prefab이 Sprite를 직접 가리키고 Prefab과 함께 읽힌다

무대의 소품 (쓰러진 용병의 무덤. 모든 용병이 같이 쓴다)
1. PNG   -> Assets/@Art/UI/Frame/<Key>.png (prop의 맞춘 그림 그대로, 672x896: 전신 그림의 캔버스와 바닥선)
2. 목록  -> UiArt 에 이름을 적는다. 데이터가 가리키지 않는다
3. Entry -> 없다. 화면 Prefab이 Sprite를 직접 가리킨다 (Import 정책은 UI의 것이라 MaxSize 512에 맞춰 384x512로 줄어든다. 그림 자리 225x300의 1.7배)

용병의 공격·피격 자세 (전신 그림이 확정되면 더한다)
1. PNG   -> Assets/@Art/Pose/Job/<Id>_attack.png, <Id>_hit.png (fit_pose.py의 산출물 그대로, 2048x1024)
2. CSV   -> 없다. Figure가 있는 직업마다 "pose/job/<kebab-id>-attack", "-hit"가 따라 나온다 (ArtAddress.PoseOf)
3. Entry -> Figure에서 자동으로 나온다 (Group F1-Art, Scope Expedition)

용병의 붕괴·각성 자세 (2026-10-06 Round 38. 직업마다 따로 그린다: 지금은 발키리만)
1. PNG   -> Assets/@Art/Pose/Job/<Id>_broken.png, <Id>_resolute.png (fit_pose.py의 산출물 그대로, 2048x1024)
2. CSV   -> 없다. Figure가 있는 직업마다 "pose/job/<kebab-id>-broken", "-resolute"가 따라 나온다 (JobData.BrokenPose/ResolutePose)
3. Entry -> 파일이 있는 직업만 (ArtSetup.Files. Group F1-Art, Scope Expedition. ExpeditionArt가 ResourceManager.ExistsAsync로 묻고 읽는다)

몬스터의 공격·피격 자세 (전신 그림이 확정되면 더한다. 2026-10-05)
1. PNG   -> Assets/@Art/Pose/Enemy/<Id>_attack.png, <Id>_hit.png (fit_pose.py의 산출물 그대로, 2048x1024)
2. CSV   -> 없다. Figure가 있는 적마다 "pose/enemy/<kebab-id>-attack", "-hit"가 따라 나온다 (ArtAddress.PoseOf, EnemyData.AttackPose/HitPose)
3. Entry -> Figure에서 자동으로 나온다 (Group F1-Art, Scope Expedition. Import 정책은 용병의 자세와 같다)

포션의 병 아이콘
1. PNG   -> Assets/@Art/Potion/<Id>.png (ui_icon으로 생성한 96x96)
2. CSV   -> PotionData.csv 의 Icon = "potion/<kebab-id>"
3. Entry -> CSV의 Icon에서 자동으로 나온다 (Group F1-Art, Scope Expedition. Import 정책은 UI 아이콘과 같다)
```

- 그림은 **직업**과 **적**에 붙는다. 용병은 자기 직업의 그림으로 보인다.
- UI의 틀 가운데 **가방**(`Frame/bag`. 보드 패널에서 유닛의 칸 뒤에 깔린다)은 생성한 그림이 아니라 **도형 Placeholder**다: `Archive/08-panel-cells/draw_bag.py`가
  목업에서 승인된 모양(가죽판, 어두운 테, 안쪽 솔기)을 128x128의 9-slice로 그린다(호출 없음. Border 40). 그림으로 바꿀 때는 `ui_frame`으로 생성해 같은 자리에 둔다.
- `Figure`가 빈 값이면 그 유닛은 도형 Placeholder로 남는다. 그림이 없다는 것은 데이터가 말한다. Load 실패를 Placeholder로 바꾸지 않는다.
  `Figure`가 있는데 자세 파일이 없으면 `ArtSetup.FindProblems`가 잡는다.
- 배경은 **던전**에 붙고, 전투 화면이 깐다. `Background`가 빈 값이면 그 던전의 전투는 배경색 그대로다.
- 아이콘은 **아이템**에 붙고, 아이템 칸이 보여 준다(`12_UI.md` "전투 화면", "파티 쪽"). `Icon`이 빈 값이면 그 아이템의 칸에는 이름이 적힌다.
- 맞춘 그림(`output/<type>/<Key>.png`)을 그 자리에 복사하는 것이 배선의 시작이다. 승인된 것만 복사한다.
- Import 정책은 Setup 코드 한 곳(`ArtSetup`)이 강제한다. 전신 그림: Sprite(Single), `alphaIsTransparency`, Mipmap, Full Rect, MaxSize 1024, 높은 품질의 압축.
  배경: 같되 불투명(`alphaIsTransparency` 없음), Mipmap 없음(화면에 거의 제 크기로 깔린다), MaxSize 4096(캔버스 2304x1536을 줄이지 않는다).
  UI: 전신 그림과 같되 Pixels Per Unit 200(화면 크기의 2배로 그려져 있다), MaxSize 512, 틀에는 `UiArt`의 Border.
  자세: 전신 그림과 같되 MaxSize 2048(2048x1024를 줄이지 않아 전신 그림과 같은 선명도. 2026-10-04 "권장안 반영").
  압축: 모두 높은 품질의 압축(BC7)을 요청하지만 Unity는 Mipmap이 있는 텍스처를 두 변이 모두 2의 거듭제곱일 때만 압축하고, 다른 크기는 요청과 상관없이 무압축(RGBA32, 메모리 4배)으로 둔다.
  그래서 자세의 파일은 2048x1024이고 압축된다. 배경은 Mipmap이 없어 압축된다. 전신 그림(3:4)·UI·아이템 아이콘·포션은 무압축이다(2026-10-05, `Archive/31-texture-compression`).
  압축돼야 하는 종류(자세, 배경)가 무압축으로 Import되면 `ArtSetup.FindProblems`가 잡는다(`ImportPolicy.Compressed`).
  아이템의 아이콘: UI의 아이콘과 같다(화면 크기의 2배, Border 없음).
  정책과 다르면 다시 Import한다. Inspector에서 고치지 않는다. 체인의 setup 단계가 부른다.
- 화면은 열리기 전에 쓸 그림을 `ResourceManager`로 읽어 둔다. Addressables를 직접 부르지 않는다(`12_UI.md` "유닛의 그림", "전투 화면").

## Test와 검사

- EditMode: `ArtSetup.FindProblems`가 비어 있다(`Figure`와 직업과 적의 두 자세, `Background`, `Icon`이 가리키는 파일, `UiArt`의 파일이 있고 Import 정책이 맞다. `Assets/@Art`에 아무도 가리키지 않는 PNG가 없다).
  `AddressablesSetup.FindProblems`가 비어 있다.
- PlayMode: `Figure`가 있는 유닛은 그림을, 없는 유닛은 Placeholder를 보여 준다. 전투 화면이 던전의 배경을 깐다. 아이템 칸이 `Icon`의 그림을 보여 준다.
- 눈으로: `Tools/screenshots.sh`.
- 스크립트는 `--dry-run`으로 호출 없이 파일 검사와 프롬프트 조립까지 확인한다.

## Validation

- 요청받지 않은 호출이 없었는가? 호출 상한 안인가? 호출이 장부에 적혔는가?
- 승인되지 않은 그림이 `Assets`에 없는가?
- 스타일 문구가 스크립트에 복사돼 있지 않은가? 키가 출력, 로그, 파일에 없는가?
- 새 그림의 파일, `Figure`나 `Background`나 `Icon`, Entry, Import 정책이 서로 맞는가?
- 새 용병(과 그 자세·표정)의 머리가 지금 용병들과 같은 띠(`FACE_BAND`) 안인가?

## Deferred and Forbidden

- Deferred: 원정 화면 말고의 UI 그림, 가방의 그림(지금은 도형), 아이템 아이콘을 새 칸의 캔버스로 다시 그리는 것(지금 것이 맞아 들어가므로 필요할 때), 던전 말고의 배경(타이틀, 본부)과 그 배선, 포션의 아이콘, 노드 아이콘,
  여러 장의 애니메이션, 유닛의 색만 다른 변형. 소리는 `14_SOUND.md`가 소유한다.
- Forbidden: 요청 없는 생성, 판정 없는 재생성, 승인 전 배선, 키를 환경 변수나 파일에 두기, 스타일 문구를 스크립트에 복사하기,
  작가나 작품의 이름을 프롬프트에 넣기, 기준 그림의 캐릭터를 승인 없이 다시 그리기, Inspector로 Import 설정 고치기.
