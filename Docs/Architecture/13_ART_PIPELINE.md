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
├─ Rosters/<type>.csv          # 무엇을 그리는가: Key, Subject. 전신 그림은 Height, Flip(적은 Dungeon도), 아이템은 Reference, 배경은 Dungeon, FloorLine, UI는 Size, Outline. `ui_variant.csv`는 틀의 색 변형(Key, Source, Fill)
├─ Archive
│  ├─ calls.csv                # 유료 호출의 장부: 시각, 대상, 모델, 사용량, 추정 비용
│  └─ <round>/                 # README.md, 리뷰 시트, 승인된 원본
├─ tools
│  ├─ gen_image.py             # 한 번 실행 = 호출 한 번. --dry-run, --refit 은 호출하지 않는다
│  ├─ run_roster.py            # Roster의 빠진 항목을 하나씩 돌린다. --max-calls 가 필수다
│  ├─ cutout.py                # 단색 배경의 그림에서 캐릭터를 오려 낸다. 호출하지 않는다
│  ├─ ui_variants.py           # 맞춘 틀에서 게임에 넣을 Sprite를 만든다: 바탕색을 바꾼 변형. 호출하지 않는다
│  └─ review_sheet.py          # 리뷰 시트. 호출하지 않는다
└─ output/                     # 생성 직후 산출물 (gitignore)
.venv/                         # Python 가상환경 (gitignore). openai, pillow
```

폴더는 첫 실제 파일과 함께 만든다.

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
- 그림체를 바꿀 때는 이전 문서를 `Archive/<style-name>/`으로 옮기고 새로 쓴다.

## 타입

타입은 실제로 그릴 때 더한다.

| 타입 | 그리는 것 | Roster (`Key`) | 후처리 |
|---|---|---|---|
| `character` | 직업의 전신 그림. 오른쪽을 본다 | `Rosters/character.csv` (`JobData.Id`) | `figure` |
| `enemy` | 적의 전신 그림. 왼쪽을 본다 | `Rosters/enemy.csv` (`EnemyData.Id`, `Dungeon`은 `DungeonData.Id`) | `figure` |
| `item` | 아이템의 아이콘. 그 아이템이 차지하는 칸의 모양으로 그린다 | `Rosters/item.csv` (`ItemData.Id`. 칸의 수는 `ItemData.Size`) | `cell` |
| `background` | 화면의 배경. 불투명, 인물 없음 | `Rosters/background.csv` (던전의 배경은 `Dungeon`이 `DungeonData.Id`) | `scene` |
| `ui_frame` | UI의 틀: 패널, 명패, 칸, 버튼. 화면에서 늘여 쓰는 빈 사각형 | `Rosters/ui_frame.csv` (쓰임새의 이름) | `frame` |
| `ui_icon` | UI의 작은 기호: 상태 아이콘 | `Rosters/ui_icon.csv` (쓰임새의 이름) | `glyph` |

- 호출: `images.edit`에 기준 그림을 붙인다. 모델 `gpt-image-2.5-sunburst`, `1024x1024`, `medium`, 투명 배경, PNG.
  모양이 정사각이 아닌 것(배경, UI의 틀, 아이템)은 그 모양에 가까운 캔버스로 생성한다.
  모델은 투명 배경을 지원하는 것이어야 한다(`gpt-image-2`는 투명 배경 요청을 거절한다).
- 기준 그림은 타입마다 정해 둔 것만 붙인다. 승인된 우리 그림이 생기면 그것을 기준 그림으로 바꾼다.
- 파티 쪽 그림(직업)은 화면의 오른쪽, 곧 적 쪽을 본다. 적은 왼쪽을 본다. 모델은 방향을 문구보다 기준 그림에서 따르므로,
  왼쪽을 보는 기준 그림을 직업에는 좌우를 뒤집어, 적에는 그대로 붙인다.
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
- `Flip`이 참이면 좌우를 뒤집는다. 반대쪽을 보고 나온 그림은 다시 생성하지 않고 뒤집는다.
- 생성한 원본(`<name>.raw.png`)을 먼저 저장하고 맞춘 그림(`<name>.png`)을 따로 둔다. `Height`나 `Flip`을 고친 뒤에는
  `--refit`으로 원본에서 다시 맞춘다(호출 없음).
- 호출마다 사용량(토큰)과 추정 비용을 출력하고 `Archive/calls.csv`에 적는다.

## 후처리 (`cell`)

아이템의 아이콘은 그 아이템이 차지하는 칸의 모양으로 그린다. 칸의 수는 Game Data(`ItemData.Size`)에서 읽는다. Roster에 다시 적지 않는다.

| 칸의 수 | 화면의 칸 | 생성 캔버스 | 맞춘 캔버스 | 구도 (스타일 문서의 그 칸 수의 절) |
|---|---|---|---|---|
| 1 | 180x60 | `1536x512` | 320x88 | 가로로 긴 띠. 긴 것은 눕힌다 |
| 2 | 180x124 | `1536x1024` | 320x216 | 가로가 긴 사각형. 긴 것은 비스듬히 놓는다 |
| 3 | 180x188 | `1024x1024` | 320x344 | 정사각에 가까운 사각형. 긴 것은 대각선으로 놓는다 |

- 맞춘 캔버스는 칸에서 테(좌우 10, 위아래 8)를 뺀 자리의 2배다. 화면은 아이콘을 칸의 가운데에 그 크기로 놓는다.
- 조각을 Alpha로 오려 내 비율을 지킨 채 캔버스에 가장 크게 맞추고 가운데에 놓는다. 둘레에 고른 띠(4px, `UI_LINE`)를 두른다(`glyph`와 같은 맞추기다).
- 프롬프트의 구도는 아이템의 공통 규칙에 그 칸 수의 절을 더한 것이다. 긴 것은 손잡이가 왼쪽, 끝이 오른쪽이다.
- 칸의 크기(`12_UI.md` "전투 화면")가 바뀌면 이 표와 `ITEM_CELLS`를 같이 고치고, 비율이 달라진 칸 수의 아이콘은 다시 그린다.
- 리뷰 시트(`review_sheet.py --type item`)는 아이콘을 실제 칸에 넣어 만든다: 화면 크기(쿨다운이 절반 찬 것과 다 찬 것)와 그 2배.

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

## 승인 라운드

```text
1. 소재: Roster의 Subject를 먼저 보여 준다 (호출 없음)
2. 상한: 라운드를 시작할 때 호출 수의 상한을 정한다. 닿으면 멈추고 보고한다
3. 생성: 항목당 한 번
4. 리뷰 시트: 한 장에 [실제 표시 크기, 게임 배경색 위 / 기준 그림과 후보 원본]
5. 판정: 항목별로 "확정", "재생성 - 방향", "소재 교체"
6. 재생성은 요청된 항목만 한 번씩. 뒤집기, 크기 맞추기는 로컬에서 한다 (호출 없음)
7. 승인분만 배선하고 검증 체인을 한 번 돌린다
8. Archive/<round>/README.md: 후보, 판정, 받아들인 문구, 거절 사유, 호출 수와 사용량
```

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
1. PNG   -> Assets/@Art/UI/Frame/<Key>.png (ui_variant의 Key), Assets/@Art/UI/Icon/<Key>.png
2. 목록  -> UiArt 에 이름(틀은 Border도)을 적는다. 데이터가 가리키지 않는다
3. Entry -> 없다. 화면 Prefab이 Sprite를 직접 가리키고 Prefab과 함께 읽힌다
```

- 그림은 **직업**과 **적**에 붙는다. 용병은 자기 직업의 그림으로 보인다.
- `Figure`가 빈 값이면 그 유닛은 도형 Placeholder로 남는다. 그림이 없다는 것은 데이터가 말한다. Load 실패를 Placeholder로 바꾸지 않는다.
- 배경은 **던전**에 붙고, 전투 화면이 깐다. `Background`가 빈 값이면 그 던전의 전투는 배경색 그대로다.
- 아이콘은 **아이템**에 붙고, 아이템 칸이 보여 준다(`12_UI.md` "전투 화면", "파티 쪽"). `Icon`이 빈 값이면 그 아이템의 칸에는 이름이 적힌다.
- 맞춘 그림(`output/<type>/<Key>.png`)을 그 자리에 복사하는 것이 배선의 시작이다. 승인된 것만 복사한다.
- Import 정책은 Setup 코드 한 곳(`ArtSetup`)이 강제한다. 전신 그림: Sprite(Single), `alphaIsTransparency`, Mipmap, Full Rect, MaxSize 1024, 높은 품질의 압축.
  배경: 같되 불투명(`alphaIsTransparency` 없음), Mipmap 없음(화면에 거의 제 크기로 깔린다), MaxSize 4096(캔버스 2304x1536을 줄이지 않는다).
  UI: 전신 그림과 같되 Pixels Per Unit 200(화면 크기의 2배로 그려져 있다), MaxSize 512, 틀에는 `UiArt`의 Border.
  아이템의 아이콘: UI의 아이콘과 같다(화면 크기의 2배, Border 없음).
  정책과 다르면 다시 Import한다. Inspector에서 고치지 않는다. 체인의 setup 단계가 부른다.
- 화면은 열리기 전에 쓸 그림을 `ResourceManager`로 읽어 둔다. Addressables를 직접 부르지 않는다(`12_UI.md` "유닛의 그림", "전투 화면").

## Test와 검사

- EditMode: `ArtSetup.FindProblems`가 비어 있다(`Figure`, `Background`, `Icon`이 가리키는 파일, `UiArt`의 파일이 있고 Import 정책이 맞다. `Assets/@Art`에 아무도 가리키지 않는 PNG가 없다).
  `AddressablesSetup.FindProblems`가 비어 있다.
- PlayMode: `Figure`가 있는 유닛은 그림을, 없는 유닛은 Placeholder를 보여 준다. 전투 화면이 던전의 배경을 깐다. 아이템 칸이 `Icon`의 그림을 보여 준다.
- 눈으로: `Tools/screenshots.sh`.
- 스크립트는 `--dry-run`으로 호출 없이 파일 검사와 프롬프트 조립까지 확인한다.

## Validation

- 요청받지 않은 호출이 없었는가? 호출 상한 안인가? 호출이 장부에 적혔는가?
- 승인되지 않은 그림이 `Assets`에 없는가?
- 스타일 문구가 스크립트에 복사돼 있지 않은가? 키가 출력, 로그, 파일에 없는가?
- 새 그림의 파일, `Figure`나 `Background`나 `Icon`, Entry, Import 정책이 서로 맞는가?

## Deferred and Forbidden

- Deferred: 원정 화면 말고의 UI 그림, 던전 말고의 배경(타이틀, 본부)과 그 배선, 포션의 아이콘, 노드 아이콘, 애니메이션과 연출, 유닛의 색만 다른 변형, 소리.
- Forbidden: 요청 없는 생성, 판정 없는 재생성, 승인 전 배선, 키를 환경 변수나 파일에 두기, 스타일 문구를 스크립트에 복사하기,
  작가나 작품의 이름을 프롬프트에 넣기, 기준 그림의 캐릭터를 승인 없이 다시 그리기, Inspector로 Import 설정 고치기.
